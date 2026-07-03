namespace SpaceSails.Core;

/// <summary>
/// One NPC cargo ship: its public departures-board entry plus its (hidden) flight plan.
/// <see cref="ActivationTime"/> is when the ship enters the live sim with
/// <see cref="InitialState"/> as truth — for mid-flight ships that is ~t=0 with the ship
/// already deep in its transfer, not its historical departure.
/// </summary>
public sealed record NpcShip(
    string Id,
    string Callsign,
    string CargoClass,
    string OriginId,
    string DestinationId,
    RoutePersonality Personality,
    double DepartureTime,
    double ActivationTime,
    ShipState InitialState,
    ManeuverPlan Plan,
    double EstimatedArrivalTime,
    int CargoUnits,
    double ManeuverBudget,
    bool IsPod,
    string? DepotBodyId = null,
    double DepotOrbitRadius = 0,
    double DepotPhase = 0)
{
    /// <summary>Equivalent acceleration a pilot could plausibly hide between observations.
    /// Feeds the prediction cone; a mass-driver pod has no engine at all.</summary>
    public const double DefaultManeuverBudget = 0.3;
}

/// <summary>
/// Deterministic traffic generator: the same seed yields bit-identical schedules on client and
/// server. Physics note: prograde-only pulses mean outer-system transfers take sim-years, so
/// playable traffic is dominated by ships spawned mid-flight — already falling through the
/// inner system at t=0, 20–70 days from arrival — plus a few short inner-system runs departing
/// during the first weeks. The Saturn departure times on the board are honest history.
/// </summary>
public static class TrafficSchedule
{
    /// <summary>
    /// Fixed timestep for live NPC integration. Coarser than the player's dt=1 s because a
    /// frame at high warp must step every NPC (8 × 10000 dt=1 steps/frame froze interpreted
    /// WASM at ~1 fps), and NPC accuracy needs are meters-scale at dt=60. One shared constant
    /// so client and server (M9) integrate NPCs identically — determinism is law.
    /// </summary>
    public const double NpcTimeStep = 60;

    // Coarse on purpose: catch-up replays years of transfer at startup in interpreted WASM.
    // The resulting state is *declared* truth, so coarseness costs accuracy of the fiction, not
    // determinism of the sim.
    private const double CatchUpTimeStep = 7200;
    private const double Day = 86400;

    private static readonly string[] Callsigns =
        ["Meridian", "Kestrel", "Long Haul", "Aurora", "Tycho's Due", "Windlass", "Half Hitch", "Barnacle", "Sable", "Pelican"];

    private static readonly (string Origin, string Destination, string Cargo)[] LongRoutes =
        [("saturn", "mars", "He3"), ("saturn", "earth", "He3"), ("jupiter", "mars", "He3")];

    private static readonly (string Origin, string Destination, string Cargo)[] ShortRoutes =
        [("mars", "earth", "Machinery"), ("earth", "mars", "Ice"), ("venus", "earth", "Alloys")];

    public static IReadOnlyList<NpcShip> Generate(ICelestialEphemeris ephemeris, ulong seed, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var rng = new DeterministicRandom(seed);
        var catchUpSim = new Simulator(ephemeris, CatchUpTimeStep);
        var ships = new List<NpcShip>(count);

        int midFlight = Math.Max(1, count * 6 / 10);
        for (int i = 0; i < count; i++)
        {
            bool isMidFlight = i < midFlight;
            (string origin, string destination, string cargo) = isMidFlight
                ? LongRoutes[rng.NextInt(0, LongRoutes.Length)]
                : ShortRoutes[rng.NextInt(0, ShortRoutes.Length)];
            var personality = (RoutePersonality)rng.NextInt(0, 3);
            string callsign = Callsigns[i % Callsigns.Length];

            int cargoUnits = rng.NextInt(5, 21);
            if (isMidFlight)
            {
                // Plan from a virtual past departure, then declare the coarse catch-up state at
                // ~t=0 the ship's truth. Deterministic forward from there; remaining plan nodes
                // (mid-course evasive bursts, the arrival brake) still execute live.
                double lead = rng.NextDouble(20 * Day, 70 * Day);
                NpcRoute probe = RoutePlanner.PlanRoute(ephemeris, origin, destination, 0, personality, Clone(rng));
                double transfer = probe.EstimatedArrivalTime;
                double virtualDeparture = -(transfer - lead);

                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, virtualDeparture, personality, rng);
                ShipState now = catchUpSim.Run(route.DepartureState, -virtualDeparture, route.Plan);
                ships.Add(new NpcShip(
                    $"npc-{i}", callsign, cargo, origin, destination, personality,
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
            else
            {
                double departure = Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, departure, personality, rng);
                ships.Add(new NpcShip(
                    $"npc-{i}", callsign, cargo, origin, destination, personality,
                    departure, departure, route.DepartureState, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
        }

        return ships;
    }

    /// <summary>
    /// Luna's mass-driver launches: ballistic compute-core pods (worldbuilding notes §1). The
    /// driver imparts all Δv at launch, so the "burn" is folded into <c>InitialState</c> and the
    /// plan is empty — no engine, no future maneuvers, <c>ManeuverBudget = 0</c>: a pod's
    /// prediction cone never opens. The tutorial prey and the pirate's milk run.
    /// </summary>
    /// <summary>
    /// One plunderable cargo depot in orbit around every planet (M22, owner: "surely there is
    /// something to steal on every planet orbit"). Depots ride RAILS — their state is a pure
    /// function of sim time (planet position + circular offset), costing nothing to step and
    /// never drifting. Cargo flavor follows the worldbuilding: compute cores at Mercury, He3
    /// in the outer system.
    /// </summary>
    public static IReadOnlyList<NpcShip> GenerateDepots(ICelestialEphemeris ephemeris, ulong seed)
    {
        var rng = new DeterministicRandom(seed);
        var depots = new List<NpcShip>();
        int n = 0;
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.ParentId != "sun")
            {
                continue; // planets only: moons share their planet's depot
            }

            CelestialBody sun = ephemeris.Bodies.First(b => b.ParentId is null);
            double hill = body.OrbitRadius * Math.Pow(body.Mu / (3 * sun.Mu), 1.0 / 3.0);
            double radius = Math.Max(body.BodyRadius * 8, hill * 0.25);
            double phase = rng.NextDouble() * Math.PI * 2;
            string cargo = body.Id switch
            {
                "mercury" => "Compute cores",
                "venus" => "Alloys",
                "earth" => "Machinery",
                "mars" => "Ice",
                _ => "He3",
            };

            depots.Add(new NpcShip(
                Id: $"depot-{body.Id}",
                Callsign: $"{body.Name} Depot",
                CargoClass: cargo,
                OriginId: body.Id,
                DestinationId: body.Id,
                Personality: RoutePersonality.Economical,
                DepartureTime: 0,
                ActivationTime: 0,
                InitialState: DepotState($"depot-{body.Id}", body.Id, radius, phase, ephemeris, 0),
                Plan: new ManeuverPlan([]),
                EstimatedArrivalTime: double.MaxValue,
                CargoUnits: 4,
                ManeuverBudget: 0,
                IsPod: false,
                DepotBodyId: body.Id,
                DepotOrbitRadius: radius,
                DepotPhase: phase));
            n++;
        }

        return depots;
    }

    /// <summary>Rails state of a depot at a given time: planet position plus circular orbit.</summary>
    public static ShipState DepotState(string id, string bodyId, double radius, double phase, ICelestialEphemeris ephemeris, double simTime)
    {
        CelestialBody body = ephemeris.Bodies.First(b => b.Id == bodyId);
        double angularRate = Math.Sqrt(body.Mu / (radius * radius * radius));
        double angle = phase + angularRate * simTime;
        Vector2d center = ephemeris.Position(bodyId, simTime);
        double h = 1.0;
        Vector2d centerVel = (ephemeris.Position(bodyId, simTime + h) - ephemeris.Position(bodyId, simTime - h)) / (2 * h);
        var offset = new Vector2d(Math.Cos(angle), Math.Sin(angle)) * radius;
        var tangent = new Vector2d(-Math.Sin(angle), Math.Cos(angle)) * (angularRate * radius);
        return new ShipState(center + offset, centerVel + tangent, simTime);
    }

    public static IReadOnlyList<NpcShip> GeneratePods(ICelestialEphemeris ephemeris, ulong seed, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var rng = new DeterministicRandom(seed);
        var launchSim = new Simulator(ephemeris, CatchUpTimeStep);
        var pods = new List<NpcShip>(count);

        for (int i = 0; i < count; i++)
        {
            string destination = rng.NextInt(0, 2) == 0 ? "mars" : "venus";
            double departure = Math.Floor(rng.NextDouble(0.5 * Day, 10 * Day));

            // Plan the route like a ship (one burst + coast; the arrival brake is dropped —
            // the customer catches the pod), then run just past the burst and declare that
            // state the launch: everything the driver gave it, nothing it can change.
            NpcRoute route = RoutePlanner.PlanRoute(ephemeris, "earth", destination, departure, RoutePersonality.Economical, rng);
            ManeuverNode burn = route.Plan.Nodes[0];
            var launchPlan = new ManeuverPlan([burn]);
            ShipState launched = launchSim.Run(route.DepartureState, (burn.SimTime - departure) + CatchUpTimeStep, launchPlan);

            pods.Add(new NpcShip(
                $"pod-{i}", $"Pod-{i + 1}", "Compute cores", "luna", destination,
                RoutePersonality.Economical, departure, launched.SimTime, launched,
                // 5 units × 400 cr = exactly the first upgrade (2000 cr): one clean milk run
                // finishes the tutorial. 4 units would dead-end it 400 credits short.
                ManeuverPlan.Empty, route.EstimatedArrivalTime,
                CargoUnits: 5, ManeuverBudget: 0, IsPod: true));
        }

        return pods;
    }

    // The probe plan and the real plan must consume identical random sequences so the schedule
    // stays deterministic regardless of how PlanRoute uses its rng internally.
    private static DeterministicRandom Clone(DeterministicRandom rng)
    {
        // Fork a child stream from the parent's next output; both sides remain deterministic.
        return new DeterministicRandom(rng.NextUInt64());
    }
}
