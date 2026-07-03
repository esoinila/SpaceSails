using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SpaceSails.Contracts;
using SpaceSails.Core;

namespace SpaceSails.Server;

/// <summary>
/// The authoritative shared session (plan §M9): one system, 2–8 pirates, one sim time.
/// Owns the world state and the tick; the hub is a thin command shim over this.
///
/// Determinism note: the sim itself (Core Step calls) stays pure — wall clock only decides
/// *how much* sim time to advance, never *what happens* inside it. Warp is the minimum of all
/// connected players' requests: everyone must agree to skip time.
///
/// Hidden information stays hidden by omission: each player's broadcast is filtered through
/// the same SensorModel the single-player client uses, from their own ship's position. An
/// unobserved ship is not in the packet at all.
/// </summary>
public sealed class SessionHost : BackgroundService
{
    private const double TickSeconds = 0.1;
    private const int BroadcastEveryTicks = 2; // 5 Hz
    private const double PulseCooldownSeconds = 1.0;
    private const int ReactionMassCapacity = 250;
    private static readonly int[] AllowedWarps = [0, 1, 10, 100, 1000, 10000];

    public sealed class PlayerShip
    {
        public required string ConnectionId;
        public required string Callsign;
        public ShipState State;
        public int ReactionMass = ReactionMassCapacity;
        public int RequestedWarp = 1;
        public double LastPulseSimTime = double.MinValue;
        public double LastVentSimTime = double.MinValue;
        public ManeuverPlan Plan = ManeuverPlan.Empty;
        public double PlanMassAccountedThrough;
    }

    private sealed class NpcState
    {
        public required NpcShip Ship;
        public ShipState State;
        public bool Active;
    }

    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<SessionHost> _logger;
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<string, PlayerShip> _players = new();
    private readonly List<NpcState> _npcs = [];

    private CircularOrbitEphemeris _ephemeris = null!;
    private PlasmaEnvironment? _plasma;
    private Simulator _simulator = null!;
    private Simulator _npcSimulator = null!;
    private double _simTime;
    private double _accumulator;
    private int _effectiveWarp;

    public string ScenarioSlug { get; }

    public SessionHost(IHubContext<GameHub> hub, IConfiguration configuration, ILogger<SessionHost> logger)
    {
        _hub = hub;
        _logger = logger;
        ScenarioSlug = configuration["SpaceSails:Scenario"] ?? "sol";
    }

    public double SimTime { get { lock (_gate) { return _simTime; } } }

    // ---- Commands (called by the hub) ----

    public JoinResultDto Join(string connectionId, string callsign)
    {
        lock (_gate)
        {
            const double h = 1.0;
            Vector2d velocity = (_ephemeris.Position("earth", _simTime + h) - _ephemeris.Position("earth", _simTime - h)) / (2 * h);
            Vector2d earth = _ephemeris.Position("earth", _simTime);
            Vector2d position = earth + earth.Normalized() * 5e9;

            var ship = new PlayerShip
            {
                ConnectionId = connectionId,
                Callsign = Sanitize(callsign),
                State = new ShipState(position, velocity, _simTime),
            };
            _players[connectionId] = ship;
            _logger.LogInformation("{Callsign} joined ({Count} aboard)", ship.Callsign, _players.Count);

            return new JoinResultDto(
                connectionId, ScenarioSlug, _simTime,
                position.X, position.Y, velocity.X, velocity.Y, ship.ReactionMass);
        }
    }

    public void Leave(string connectionId)
    {
        if (_players.TryRemove(connectionId, out PlayerShip? ship))
        {
            _logger.LogInformation("{Callsign} left ({Count} aboard)", ship.Callsign, _players.Count);
        }
    }

    public void Pulse(string connectionId, bool accelerate)
    {
        lock (_gate)
        {
            if (!_players.TryGetValue(connectionId, out PlayerShip? ship)
                || ship.ReactionMass <= 0
                || ship.State.SimTime < ship.LastPulseSimTime + PulseCooldownSeconds)
            {
                return;
            }

            double factor = accelerate ? ManeuverPlan.AccelerateFactor : ManeuverPlan.DecelerateFactor;
            ship.State = ship.State with { Velocity = ship.State.Velocity * factor };
            ship.ReactionMass--;
            ship.LastPulseSimTime = ship.State.SimTime;

            // A live override invalidates the pending plan (single-player rule, enforced here).
            if (ship.Plan.Nodes.Any(n => n.SimTime > ship.State.SimTime))
            {
                ship.Plan = ManeuverPlan.Empty;
            }
        }
    }

    public void Vent(string connectionId)
    {
        lock (_gate)
        {
            if (_plasma is null
                || !_players.TryGetValue(connectionId, out PlayerShip? ship)
                || ship.State.SimTime < ship.LastVentSimTime + PulseCooldownSeconds)
            {
                return;
            }

            ship.State = ship.State with { Charge = ship.State.Charge * 0.5 };
            ship.LastVentSimTime = ship.State.SimTime;
        }
    }

    public void SetPlan(string connectionId, IReadOnlyList<PlanNodeDto> nodes)
    {
        lock (_gate)
        {
            if (!_players.TryGetValue(connectionId, out PlayerShip? ship))
            {
                return;
            }

            ship.Plan = new ManeuverPlan(nodes
                .Where(n => n.SimTime > _simTime)
                .Take(20)
                .Select(n => new ManeuverNode(
                    n.SimTime,
                    n.Accelerate ? ManeuverAction.Accelerate : ManeuverAction.Decelerate,
                    Math.Clamp(n.Pulses, 1, 20))));
            ship.PlanMassAccountedThrough = _simTime;
        }
    }

    public void RequestWarp(string connectionId, int warp)
    {
        if (_players.TryGetValue(connectionId, out PlayerShip? ship))
        {
            ship.RequestedWarp = AllowedWarps.Contains(warp) ? warp : 1;
        }
    }

    // ---- The authoritative loop ----

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "scenarios", $"{ScenarioSlug}.json");
        ScenarioDefinition scenario = ScenarioLoader.LoadFile(path);
        _ephemeris = CircularOrbitEphemeris.FromScenario(scenario);
        _plasma = PlasmaEnvironment.FromScenario(scenario, _ephemeris);
        _simulator = new Simulator(_ephemeris, timeStepSeconds: 1.0, _plasma);
        _npcSimulator = new Simulator(_ephemeris, TrafficSchedule.NpcTimeStep);

        foreach (NpcShip ship in TrafficSchedule.Generate(_ephemeris, seed: 42, count: 8)
                     .Concat(TrafficSchedule.GeneratePods(_ephemeris, seed: 43, count: 3)))
        {
            _npcs.Add(new NpcState { Ship = ship });
        }

        _logger.LogInformation("Session up: scenario {Scenario}, {Npcs} NPCs", scenario.Name, _npcs.Count);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(TickSeconds));
        int tick = 0;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            Step();
            if (++tick % BroadcastEveryTicks == 0)
            {
                await BroadcastAsync(stoppingToken);
            }
        }
    }

    private void Step()
    {
        lock (_gate)
        {
            _effectiveWarp = _players.IsEmpty ? 0 : _players.Values.Min(p => p.RequestedWarp);
            _accumulator += TickSeconds * _effectiveWarp;

            // Bound one tick's work (native is fast; this guards a warp spike after a stall).
            _accumulator = Math.Min(_accumulator, 20000);

            int steps = (int)_accumulator;
            if (steps <= 0)
            {
                return;
            }

            _accumulator -= steps;
            double target = _simTime + steps;

            foreach (PlayerShip player in _players.Values)
            {
                while (player.State.SimTime < target)
                {
                    player.State = _simulator.Step(player.State, player.Plan);
                }

                // Debit reaction mass for plan nodes whose window has passed.
                foreach (ManeuverNode node in player.Plan.Nodes)
                {
                    if (node.SimTime > player.PlanMassAccountedThrough && node.SimTime <= player.State.SimTime)
                    {
                        player.ReactionMass = Math.Max(0, player.ReactionMass - node.Pulses);
                    }
                }
                player.PlanMassAccountedThrough = player.State.SimTime;
            }

            foreach (NpcState npc in _npcs)
            {
                if (!npc.Active)
                {
                    if (target < npc.Ship.ActivationTime)
                    {
                        continue;
                    }
                    npc.Active = true;
                    npc.State = npc.Ship.InitialState;
                }

                while (npc.State.SimTime < target)
                {
                    npc.State = _npcSimulator.Step(npc.State, npc.Ship.Plan);
                }
            }

            _simTime = target;
        }
    }

    private async Task BroadcastAsync(CancellationToken ct)
    {
        List<(string ConnectionId, WorldUpdateDto Update)> packets = [];
        lock (_gate)
        {
            foreach (PlayerShip player in _players.Values)
            {
                var contacts = new List<ContactDto>();

                foreach (NpcState npc in _npcs)
                {
                    if (npc.Active && SensorModel.Default.TryObserve(
                            player.State.Position, npc.Ship.Id, npc.State, _simTime, out _))
                    {
                        contacts.Add(new ContactDto(
                            npc.Ship.Id, npc.Ship.Callsign, npc.Ship.IsPod ? "pod" : "npc",
                            npc.State.Position.X, npc.State.Position.Y,
                            npc.State.Velocity.X, npc.State.Velocity.Y,
                            npc.State.Charge, npc.Ship.CargoClass));
                    }
                }

                foreach (PlayerShip other in _players.Values)
                {
                    if (!ReferenceEquals(other, player) && SensorModel.Default.TryObserve(
                            player.State.Position, other.ConnectionId, other.State, _simTime, out _))
                    {
                        contacts.Add(new ContactDto(
                            other.ConnectionId, other.Callsign, "player",
                            other.State.Position.X, other.State.Position.Y,
                            other.State.Velocity.X, other.State.Velocity.Y,
                            other.State.Charge, null));
                    }
                }

                packets.Add((player.ConnectionId, new WorldUpdateDto(
                    _simTime, _effectiveWarp, _players.Count,
                    player.State.Position.X, player.State.Position.Y,
                    player.State.Velocity.X, player.State.Velocity.Y,
                    player.State.Charge, player.ReactionMass, contacts)));
            }
        }

        foreach ((string connectionId, WorldUpdateDto update) in packets)
        {
            await _hub.Clients.Client(connectionId).SendAsync("Update", update, ct);
        }
    }

    private static string Sanitize(string callsign)
    {
        string trimmed = new([.. callsign.Trim().Where(c => !char.IsControl(c))]);
        return trimmed.Length switch
        {
            0 => "Anonymous",
            > 24 => trimmed[..24],
            _ => trimmed,
        };
    }
}
