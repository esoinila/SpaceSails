using SpaceSails.Contracts;

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
    double DepotPhase = 0,
    // False = a secretive hauler (He3 out of pirate country — worldbuilding notes §4). The ship
    // still flies and is still visible to sensors that get it in range; it just never appears on
    // the public departures board. The hook F6/F7 (tracking + intel economy) need.
    bool PublishesTimetable = true)
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
///
/// De-Earth-centering (vision ¶8): a scenario can supply a <see cref="TrafficDefinition"/>
/// (routes + pod launchers) instead of relying on the hardcoded Sol tables below. When present,
/// <see cref="Generate"/>/<see cref="GeneratePods"/> read it (via the optional parameter, or via
/// <see cref="ICelestialEphemeris.Traffic"/> when the ephemeris carries one from its scenario);
/// when absent, both fall back to the original fixed tables — byte-identical to pre-PR-3
/// behavior, so scenarios without a traffic section (e.g. the Wheel of the World) are unaffected.
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

    // Central-space vs. outer-reaches split for scenario-driven routes: a route touching
    // anything past ~Mars's orbit counts as "long haul" (mid-flight ships spawned already deep
    // in transfer); everything inside stays "short" (scheduled departures). Threshold sits
    // between Mars (2.28e11 m) and Jupiter (7.79e11 m).
    private const double LongHaulThresholdMeters = 4e11;

    private static readonly string[] Callsigns =
        ["Meridian", "Kestrel", "Long Haul", "Aurora", "Tycho's Due", "Windlass", "Half Hitch", "Barnacle", "Sable", "Pelican"];

    // Pods are dumb ballistic compute-core canisters — the pirate's milk run and the tutorial's
    // first prey — so they get a prey's nicknames rather than "Pod-1", "Pod-2". Kept distinct from
    // the hauler Callsigns above so a name alone tells you which board a contact came off.
    private static readonly string[] PodCallsigns =
        ["Milk Run", "Windfall", "Ripe Plum", "Fat Goose", "Easy Keeping", "Tin Kettle", "Slow Coach", "Ferryman's Due", "Sitting Duck", "Loose Change"];

    // OG vs. reinforcements (owner: "I want to know which were the OGs and which are the new"):
    // the founding traffic present at world-load carries no tag; every later refill wave stamps
    // its ships and pods with "·N" (N = wave number), visible right in the callsign on the board,
    // the scope and the map label. The id is namespaced too ("npc-w2-…"/"pod-w2-…").
    private static string WaveTag(int wave) => wave == 0 ? "" : $" ·{wave}";

    private static string NpcId(string kind, int wave, int index) =>
        wave == 0 ? $"{kind}-{index}" : $"{kind}-w{wave}-{index}";

    private static readonly (string Origin, string Destination, string Cargo)[] LongRoutes =
        [("saturn", "mars", "He3"), ("saturn", "earth", "He3"), ("jupiter", "mars", "He3")];

    private static readonly (string Origin, string Destination, string Cargo)[] ShortRoutes =
        [("mars", "earth", "Machinery"), ("earth", "mars", "Ice"), ("venus", "earth", "Alloys")];

    private static readonly string[] FixedPodDestinations = ["mars", "venus"];

    public static IReadOnlyList<NpcShip> Generate(ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { Routes.Count: > 0 }
            ? GenerateFromScenario(ephemeris, seed, count, effective)
            : GenerateFromFixedTables(ephemeris, seed, count);
    }

    /// <summary>
    /// The world keeps living (owner, 2026-07-05: after every ship arrived the sky emptied —
    /// "THERE IS NOBODY IN SPACE"): a fresh wave of traffic planned relative to
    /// <paramref name="nowSimTime"/> — mid-flight ships already deep in transfer as of NOW,
    /// short runs departing over the following weeks — with ids namespaced by
    /// <paramref name="waveNumber"/> so waves never collide. Deterministic per (seed, wave).
    /// </summary>
    public static IReadOnlyList<NpcShip> GenerateWave(
        ICelestialEphemeris ephemeris, ulong seed, int count, double nowSimTime, int waveNumber, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { Routes.Count: > 0 }
            ? GenerateFromScenario(ephemeris, seed, count, effective, nowSimTime, waveNumber)
            : GenerateFromFixedTables(ephemeris, seed, count, nowSimTime, waveNumber);
    }

    /// <summary>A fresh pod wave, launching over the days after <paramref name="nowSimTime"/> —
    /// the milk run never dries up.</summary>
    public static IReadOnlyList<NpcShip> GeneratePodsWave(
        ICelestialEphemeris ephemeris, ulong seed, int count, double nowSimTime, int waveNumber, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { PodLaunchers.Count: > 0 }
            ? GeneratePodsFromScenario(ephemeris, seed, count, effective, nowSimTime, waveNumber)
            : GeneratePodsFromFixedLauncher(ephemeris, seed, count, nowSimTime, waveNumber);
    }

    private static IReadOnlyList<NpcShip> GenerateFromFixedTables(
        ICelestialEphemeris ephemeris, ulong seed, int count, double baseSimTime = 0, int wave = 0)
    {
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
            string id = NpcId("npc", wave, i);
            string callsign = Callsigns[i % Callsigns.Length] + WaveTag(wave);

            int cargoUnits = rng.NextInt(5, 21);
            if (isMidFlight)
            {
                // Plan from a virtual past departure, then declare the coarse catch-up state at
                // ~t=0 the ship's truth. Deterministic forward from there; remaining plan nodes
                // (mid-course evasive bursts, the arrival brake) still execute live.
                double lead = rng.NextDouble(20 * Day, 70 * Day);
                NpcRoute probe = RoutePlanner.PlanRoute(ephemeris, origin, destination, baseSimTime, personality, Clone(rng));
                double transfer = probe.EstimatedArrivalTime - baseSimTime;
                // The world does not wait for the player: the remaining lead can never exceed
                // the transfer itself, or a short hop would "spawn mid-flight" at a departure
                // time in the FUTURE and the starting sky would be empty.
                lead = Math.Min(lead, transfer * rng.NextDouble(0.3, 0.8));
                double virtualDeparture = baseSimTime - (transfer - lead);

                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, virtualDeparture, personality, rng);
                ShipState now = catchUpSim.Run(route.DepartureState, baseSimTime - virtualDeparture, route.Plan);
                ships.Add(new NpcShip(
                    id, callsign, cargo, origin, destination, personality,
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
            else
            {
                double departure = baseSimTime + Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, departure, personality, rng);
                ships.Add(new NpcShip(
                    id, callsign, cargo, origin, destination, personality,
                    departure, departure, route.DepartureState, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
        }

        return ships;
    }

    private static IReadOnlyList<NpcShip> GenerateFromScenario(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition traffic,
        double baseSimTime = 0, int wave = 0)
    {
        var rng = new DeterministicRandom(seed);
        var catchUpSim = new Simulator(ephemeris, CatchUpTimeStep);
        var ships = new List<NpcShip>(count);

        (List<RouteDefinition> longHaul, List<RouteDefinition> shortHaul) = SplitRoutesByDistance(ephemeris, traffic.Routes);
        IReadOnlyList<RouteDefinition> all = traffic.Routes;

        // The outer-system mid-flight cohort — unchanged from before (its draws feed the
        // secretive-hauler worldbuilding), so the long-haul route mix stays byte-identical.
        int midFlight = Math.Max(1, count * 6 / 10);
        // Owner (2026-07-06, the empty-sky screenshot): if EVERY mid-flight ship is long-haul it
        // spawns 3–9 AU out — past the 3 AU civilian-beacon range — so an inner-system start opens
        // on empty space. Seed one extra ship already en route on a SHORT inner route: it falls
        // through the inner system at t=0, inside beacon range and lit. It takes the first
        // otherwise-scheduled slot, so the long-haul draws above are untouched.
        int innerMidFlight = longHaul.Count > 0 && shortHaul.Count > 0 ? 1 : 0;
        for (int i = 0; i < count; i++)
        {
            bool isLongMidFlight = i < midFlight;
            bool isInnerMidFlight = i >= midFlight && i < midFlight + innerMidFlight;
            bool isMidFlight = isLongMidFlight || isInnerMidFlight;
            IReadOnlyList<RouteDefinition> pool = isLongMidFlight
                ? (longHaul.Count > 0 ? longHaul : all)
                : (shortHaul.Count > 0 ? shortHaul : all);
            RouteDefinition chosen = PickWeighted(pool, rng);

            // Route planning compares raw orbit radii one level deep (RoutePlanner), so a moon or
            // a station orbiting a moon/planet borrows its top-level parent for the burn-direction
            // and horizon math — the same shortcut the Luna pods have always used. The ship's
            // displayed origin/destination stay the scenario's own ids (board flavor + lore).
            string planFrom = PlanningBodyId(ephemeris, chosen.From);
            string planTo = PlanningBodyId(ephemeris, chosen.To);

            var personality = (RoutePersonality)rng.NextInt(0, 3);
            string id = NpcId("npc", wave, i);
            string callsign = Callsigns[i % Callsigns.Length] + WaveTag(wave);
            int cargoUnits = rng.NextInt(5, 21);

            if (isMidFlight)
            {
                double lead = rng.NextDouble(20 * Day, 70 * Day);
                NpcRoute probe = RoutePlanner.PlanRoute(ephemeris, planFrom, planTo, baseSimTime, personality, Clone(rng));
                double transfer = probe.EstimatedArrivalTime - baseSimTime;
                // Same clamp as the fixed tables: mid-flight means genuinely EN ROUTE as of the
                // wave base time, even when the scenario routes are short hops.
                lead = Math.Min(lead, transfer * rng.NextDouble(0.3, 0.8));
                double virtualDeparture = baseSimTime - (transfer - lead);

                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planFrom, planTo, virtualDeparture, personality, rng);
                ShipState now = catchUpSim.Run(route.DepartureState, baseSimTime - virtualDeparture, route.Plan);
                ships.Add(new NpcShip(
                    id, callsign, chosen.Cargo, chosen.From, chosen.To, personality,
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false,
                    PublishesTimetable: chosen.PublishesTimetable));
            }
            else
            {
                double departure = baseSimTime + Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planFrom, planTo, departure, personality, rng);
                ships.Add(new NpcShip(
                    id, callsign, chosen.Cargo, chosen.From, chosen.To, personality,
                    departure, departure, route.DepartureState, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false,
                    PublishesTimetable: chosen.PublishesTimetable));
            }
        }

        return ships;
    }

    private static (List<RouteDefinition> Long, List<RouteDefinition> Short) SplitRoutesByDistance(
        ICelestialEphemeris ephemeris, IReadOnlyList<RouteDefinition> routes)
    {
        var longHaul = new List<RouteDefinition>();
        var shortHaul = new List<RouteDefinition>();
        foreach (RouteDefinition route in routes)
        {
            double distance = Math.Max(DistanceFromOrigin(ephemeris, route.From), DistanceFromOrigin(ephemeris, route.To));
            (distance >= LongHaulThresholdMeters ? longHaul : shortHaul).Add(route);
        }

        return (longHaul, shortHaul);
    }

    private static double DistanceFromOrigin(ICelestialEphemeris ephemeris, string bodyId) => ephemeris.Position(bodyId, 0).Length;

    private static RouteDefinition PickWeighted(IReadOnlyList<RouteDefinition> routes, DeterministicRandom rng)
    {
        double total = 0;
        foreach (RouteDefinition r in routes)
        {
            total += Math.Max(0.0001, r.Weight);
        }

        double pick = rng.NextDouble() * total;
        double cumulative = 0;
        foreach (RouteDefinition r in routes)
        {
            cumulative += Math.Max(0.0001, r.Weight);
            if (pick <= cumulative)
            {
                return r;
            }
        }

        return routes[^1];
    }

    /// <summary>
    /// The body to hand <see cref="RoutePlanner.PlanRoute"/> for a given scenario id: itself if
    /// it's a direct child of the system root (a planet), otherwise its top-level ancestor one
    /// level under the root (a moon or a station orbiting a moon/planet borrows its planet's
    /// orbit — RoutePlanner's inward/outward and horizon math only compares raw orbit radii one
    /// level deep).
    /// </summary>
    private static string PlanningBodyId(ICelestialEphemeris ephemeris, string bodyId)
    {
        Dictionary<string, CelestialBody> byId = ephemeris.Bodies.ToDictionary(b => b.Id);
        string current = bodyId;
        while (byId.TryGetValue(current, out CelestialBody? body)
               && body.ParentId is { } parentId
               && byId.TryGetValue(parentId, out CelestialBody? parent)
               && parent.ParentId is not null)
        {
            current = parentId;
        }

        return current;
    }

    /// <summary>
    /// One plunderable cargo depot in orbit around every planet (M22, owner: "surely there is
    /// something to steal on every planet orbit"), plus one at every named station and pirate
    /// haven (vision ¶8: the outer reaches get their own bus stops too). Depots ride RAILS —
    /// their state is a pure function of sim time (host body position + circular offset), costing
    /// nothing to step and never drifting. Cargo flavor follows the worldbuilding: compute cores
    /// at Mercury, He3 in the outer system.
    /// </summary>
    public static IReadOnlyList<NpcShip> GenerateDepots(ICelestialEphemeris ephemeris, ulong seed)
    {
        var rng = new DeterministicRandom(seed);
        var depots = new List<NpcShip>();
        CelestialBody sun = ephemeris.Bodies.First(b => b.ParentId is null);

        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.ParentId == "sun" && body.Mu == 0)
            {
                continue; // a mass-less thing on a heliocentric orbit is a drifting fixture/wreck (e.g. the
                          // derelict roadster), not a real planet or a commerce port — no depot rides it
            }

            bool isPlanet = body.ParentId == "sun";
            bool isNotable = body.Kind == BodyKind.Station || body.IsHaven;
            if (!isPlanet && !isNotable)
            {
                continue; // ordinary moons share their planet's depot; only planets, stations and havens get their own
            }

            double radius;
            if (isPlanet)
            {
                double hill = body.OrbitRadius * Math.Pow(body.Mu / (3 * sun.Mu), 1.0 / 3.0);
                radius = Math.Max(body.BodyRadius * 8, hill * 0.25);
            }
            else
            {
                // Stations/havens are small POIs, not planets — no Hill-sphere math, just a marker
                // orbit comfortably clear of the body itself.
                radius = Math.Max(body.BodyRadius * 8, 2e6);
            }

            double phase = rng.NextDouble() * Math.PI * 2;
            string cargo = body.Id switch
            {
                "mercury" or "mercury-compute" => "Compute cores",
                "venus" => "Alloys",
                "earth" => "Machinery",
                "mars" => "Ice",
                _ when body.Kind == BodyKind.Station => "Machinery",
                _ => "He3", // outer moons and havens: the black-market goods everyone's really after
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

    /// <summary>
    /// Mass-driver launches: ballistic compute-core pods (worldbuilding notes §1). The driver
    /// imparts all Δv at launch, so the "burn" is folded into <c>InitialState</c> and the plan is
    /// empty — no engine, no future maneuvers, <c>ManeuverBudget = 0</c>: a pod's prediction cone
    /// never opens. The tutorial prey and the pirate's milk run. Reads a scenario's pod launchers
    /// when supplied (moon and station launch sites both — worldbuilding §3), else falls back to
    /// the original Luna-only table, byte-identical to pre-PR-3 behavior.
    /// </summary>
    public static IReadOnlyList<NpcShip> GeneratePods(ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { PodLaunchers.Count: > 0 }
            ? GeneratePodsFromScenario(ephemeris, seed, count, effective)
            : GeneratePodsFromFixedLauncher(ephemeris, seed, count);
    }

    /// <summary>Abeam offset of the starter pod from the player — a short intercept the player can
    /// close with one plotted burn; well inside sensor reach, outside the instant boarding window.</summary>
    public const double StarterPodStandoffMeters = 1.5e9;

    /// <summary>Cross-drift of the starter pod relative to the player — comfortably under
    /// <see cref="CaptureRule.MaxRelativeSpeed"/>, so closing the distance is the only task.</summary>
    public const double StarterPodDriftMetersPerSecond = 2000;

    /// <summary>
    /// A guaranteed-catchable tutorial pod, placed relative to the player's own start so the "first
    /// hunt" is always deliverable regardless of seed luck (docs/MondayPonder/UIUsabilityNotes.md:
    /// interplanetary traffic runs 80–160 km/s relative, far past the 5 km/s boarding limit — the
    /// only catch from a standing start is a ship still co-moving with a body you share). This is
    /// that ship: a compute-core canister just flung from Luna's driver, still drifting abeam in
    /// Earth's neighbourhood at a couple of km/s — a fresh departure caught before it built transfer
    /// speed. Pure function of the player state, so client and (future) server agree.
    /// </summary>
    public static NpcShip StarterPod(ShipState player)
    {
        // Offset perpendicular to the player's velocity: the pod sits abeam a short hop away and
        // drifts slowly further off, so the player must plot a real (but small) intercept to close.
        Vector2d along = player.Velocity.Normalized();
        var abeam = new Vector2d(-along.Y, along.X);
        Vector2d position = player.Position + abeam * StarterPodStandoffMeters;
        Vector2d velocity = player.Velocity + abeam * StarterPodDriftMetersPerSecond;
        var state = new ShipState(position, velocity, player.SimTime);

        return new NpcShip(
            StarterPodId, "Sitting Duck", "Compute cores", "luna", "venus",
            RoutePersonality.Economical, player.SimTime, player.SimTime, state,
            ManeuverPlan.Empty, player.SimTime + 60 * Day,
            CargoUnits: 5, ManeuverBudget: 0, IsPod: true);
    }

    /// <summary>Ship id of the first-hunt pod — a stable handle so the hunt picker can (re)seed her.</summary>
    public const string StarterPodId = "pod-starter";

    /// <summary>Ship id of the second-hunt freighter. Chosen so <see cref="EncounterRule.ComplianceOf"/>
    /// rolls <see cref="ComplianceState.Stubborn"/> at heat 0 — she never heaves to, so the gun is the
    /// only way to take her (docs/MondayPonder: the second hunt teaches firing).</summary>
    public const string StarterFreighterId = "freighter-lark-5";

    /// <summary>Standoff of the tutorial's stubborn He3 freighter — on the opposite beam from the
    /// starter pod so the two hunts don't stack, still inside sensor reach but a real intercept away.</summary>
    public const double StarterFreighterStandoffMeters = 1.2e9;

    /// <summary>Cross-drift of the stubborn freighter — matchable (under
    /// <see cref="CaptureRule.MaxRelativeSpeed"/>), so once her sail is holed she can always be
    /// boarded. The gun's job here is to stop her bolting, not to slow her down.</summary>
    public const double StarterFreighterDriftMetersPerSecond = 2500;

    /// <summary>She's rigged to bolt: this many days after start her drive spins up and she jinks off
    /// her matched course (a perpendicular X-Pilot burn). Hole her sail first and the burn never fires
    /// — she drifts, boardable. Dawdle and you'll have to run her down again.</summary>
    public const double StarterFreighterEvadeDelayDays = 2.0;

    /// <summary>He3 out of the moons — the prize the tutorial's payoff line already promises.</summary>
    public const int StarterFreighterCargoUnits = 12;

    /// <summary>
    /// The second hunt's prey (docs/MondayPonder/UIUsabilityNotes.md — "the gun tutorial"): a stubborn
    /// He3 hauler that will NOT heave to, so a warning shot only makes her call for muscle. She starts
    /// co-moving with the player (matchable, like <see cref="StarterPod"/>) but her plan carries an
    /// escape jink — a perpendicular X-Pilot burn that swings her off the player's matched velocity and
    /// breaks the boarding window. Holing her sail (<c>Disabled</c> → the plan stops stepping) cancels
    /// the jink and leaves her drifting, boardable: the gun is what makes an evasive ship catchable.
    /// A perpendicular Vector burn (not a prograde Factor bolt) is used on purpose so her *speed* stays
    /// matchable — a holed Lark is always recoverable, never a stuck tutorial. Pure function of the
    /// player state, so client and (future) server agree.
    /// </summary>
    public static NpcShip StarterFreighter(ShipState player)
    {
        Vector2d along = player.Velocity.Normalized();
        var abeam = new Vector2d(-along.Y, along.X);
        // Opposite beam from the starter pod so the two tutorial prey don't overlap on the map.
        Vector2d position = player.Position - abeam * StarterFreighterStandoffMeters;
        Vector2d velocity = player.Velocity - abeam * StarterFreighterDriftMetersPerSecond;
        var state = new ShipState(position, velocity, player.SimTime);

        // The escape jink: one X-Pilot pulse perpendicular to her course. She swings off the player's
        // matched velocity (rel speed jumps past the 5 km/s window) without the runaway prograde speed
        // a Factor burn would build — so she stays close enough to run down and hole even if she bolts.
        double headingRad = Math.Atan2(-abeam.Y, -abeam.X);
        var evade = new ManeuverPlan(
        [
            new ManeuverNode(
                player.SimTime + StarterFreighterEvadeDelayDays * Day,
                ManeuverAction.Accelerate, Pulses: 1, Percent: 20,
                Mode: BurnMode.Vector, HeadingDegrees: headingRad * 180.0 / Math.PI),
        ]);

        return new NpcShip(
            StarterFreighterId, "Nervous Lark", "He3", "titan", "earth",
            RoutePersonality.Economical, player.SimTime, player.SimTime, state,
            evade, player.SimTime + 60 * Day,
            CargoUnits: StarterFreighterCargoUnits, ManeuverBudget: 1.0, IsPod: false);
    }

    private static IReadOnlyList<NpcShip> GeneratePodsFromFixedLauncher(
        ICelestialEphemeris ephemeris, ulong seed, int count, double baseSimTime = 0, int wave = 0)
    {
        var rng = new DeterministicRandom(seed);
        var launchSim = new Simulator(ephemeris, CatchUpTimeStep);
        var pods = new List<NpcShip>(count);
        int midFlight = count / 2;

        for (int i = 0; i < count; i++)
        {
            string destination = rng.NextInt(0, 2) == 0 ? "mars" : "venus";
            string id = NpcId("pod", wave, i);
            string callsign = PodCallsigns[i % PodCallsigns.Length] + WaveTag(wave);
            pods.Add(i < midFlight
                ? MidFlightPod(ephemeris, launchSim, rng, "earth", destination, "luna", "Compute cores", baseSimTime, id, callsign)
                : ScheduledPod(ephemeris, launchSim, rng, "earth", destination, "luna", "Compute cores", baseSimTime, id, callsign));
        }

        return pods;
    }

    /// <summary>A pod scheduled to fire in the days after <paramref name="baseSimTime"/>: its
    /// <c>InitialState</c> is declared just past the mass driver's single launch burn (everything
    /// the driver gave it, nothing it can change — plan stays empty).</summary>
    private static NpcShip ScheduledPod(
        ICelestialEphemeris ephemeris, Simulator launchSim, DeterministicRandom rng,
        string planningOrigin, string destination, string originId, string cargo,
        double baseSimTime, string id, string callsign)
    {
        double departure = baseSimTime + Math.Floor(rng.NextDouble(0.5 * Day, 10 * Day));

        // Plan the route like a ship (one burst + coast; the arrival brake is dropped — the
        // customer catches the pod), then run just past the burst and declare that state the launch.
        NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planningOrigin, destination, departure, RoutePersonality.Economical, rng);
        ManeuverNode burn = route.Plan.Nodes[0];
        var launchPlan = new ManeuverPlan([burn]);
        ShipState launched = launchSim.Run(route.DepartureState, (burn.SimTime - departure) + CatchUpTimeStep, launchPlan);

        return new NpcShip(
            id, callsign, cargo, originId, destination,
            RoutePersonality.Economical, departure, launched.SimTime, launched,
            // 5 units × 400 cr = exactly the first upgrade (2000 cr): one clean milk run finishes
            // the tutorial. 4 units would dead-end it 400 credits short.
            ManeuverPlan.Empty, route.EstimatedArrivalTime,
            CargoUnits: 5, ManeuverBudget: 0, IsPod: true);
    }

    /// <summary>
    /// A pod that already fired 0.5–6 days before <paramref name="baseSimTime"/> and is still
    /// coasting out through the inner system as of NOW (owner, 2026-07-06 empty-sky fix): a lit,
    /// catchable contact the instant the board opens, instead of a sky that waits days for the
    /// next mass-driver firing. Ballistic after the single launch burn, exactly like a scheduled
    /// pod — only its clock is wound back.
    /// </summary>
    private static NpcShip MidFlightPod(
        ICelestialEphemeris ephemeris, Simulator launchSim, DeterministicRandom rng,
        string planningOrigin, string destination, string originId, string cargo,
        double baseSimTime, string id, string callsign)
    {
        double timeSinceLaunch = rng.NextDouble(0.5 * Day, 6 * Day);
        double virtualDeparture = baseSimTime - timeSinceLaunch;

        NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planningOrigin, destination, virtualDeparture, RoutePersonality.Economical, rng);
        ManeuverNode burn = route.Plan.Nodes[0];
        var launchPlan = new ManeuverPlan([burn]);
        ShipState now = launchSim.Run(route.DepartureState, baseSimTime - virtualDeparture, launchPlan);

        return new NpcShip(
            id, callsign, cargo, originId, destination,
            RoutePersonality.Economical, virtualDeparture, now.SimTime, now,
            ManeuverPlan.Empty, route.EstimatedArrivalTime,
            CargoUnits: 5, ManeuverBudget: 0, IsPod: true);
    }

    private static IReadOnlyList<NpcShip> GeneratePodsFromScenario(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition traffic,
        double baseSimTime = 0, int wave = 0)
    {
        var rng = new DeterministicRandom(seed);
        var launchSim = new Simulator(ephemeris, CatchUpTimeStep);
        var pods = new List<NpcShip>(count);
        IReadOnlyList<PodLauncherDefinition> launchers = traffic.PodLaunchers;

        // At least one pod is already coasting as of baseSimTime (owner, 2026-07-06 empty-sky fix):
        // the tutorial's named "Luna pod" prey must exist the instant the board opens, not fire
        // days later. The rest are scheduled firings so the milk run keeps replenishing.
        int midFlight = count / 2;
        PodLauncherDefinition? lunaLauncher = launchers.FirstOrDefault(l => l.Body == "luna");

        for (int i = 0; i < count; i++)
        {
            bool isMidFlight = i < midFlight;
            // Draw the launcher every iteration so the rng stream (and determinism) is identical
            // whether or not the Luna override below fires.
            PodLauncherDefinition picked = launchers[rng.NextInt(0, launchers.Count)];
            PodLauncherDefinition launcher = isMidFlight && lunaLauncher is not null ? lunaLauncher : picked;
            string planningOrigin = PlanningBodyId(ephemeris, launcher.Body);
            string destination = PickPodDestination(ephemeris, planningOrigin, rng);
            string id = NpcId("pod", wave, i);
            string callsign = PodCallsigns[i % PodCallsigns.Length] + WaveTag(wave);

            pods.Add(isMidFlight
                ? MidFlightPod(ephemeris, launchSim, rng, planningOrigin, destination, launcher.Body, launcher.Cargo, baseSimTime, id, callsign)
                : ScheduledPod(ephemeris, launchSim, rng, planningOrigin, destination, launcher.Body, launcher.Cargo, baseSimTime, id, callsign));
        }

        return pods;
    }

    private static string PickPodDestination(ICelestialEphemeris ephemeris, string planningOrigin, DeterministicRandom rng)
    {
        List<string> candidates = FixedPodDestinations.Concat(["earth"])
            .Where(id => id != planningOrigin && ephemeris.Bodies.Any(b => b.Id == id))
            .Distinct()
            .ToList();
        if (candidates.Count == 0)
        {
            candidates = [.. FixedPodDestinations];
        }

        return candidates[rng.NextInt(0, candidates.Count)];
    }

    // The probe plan and the real plan must consume identical random sequences so the schedule
    // stays deterministic regardless of how PlanRoute uses its rng internally.
    private static DeterministicRandom Clone(DeterministicRandom rng)
    {
        // Fork a child stream from the parent's next output; both sides remain deterministic.
        return new DeterministicRandom(rng.NextUInt64());
    }
}
