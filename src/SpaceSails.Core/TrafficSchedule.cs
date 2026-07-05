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
            ? GenerateFromScenario(ephemeris, seed, count, effective, nowSimTime, $"npc-w{waveNumber}")
            : GenerateFromFixedTables(ephemeris, seed, count, nowSimTime, $"npc-w{waveNumber}");
    }

    /// <summary>A fresh pod wave, launching over the days after <paramref name="nowSimTime"/> —
    /// the milk run never dries up.</summary>
    public static IReadOnlyList<NpcShip> GeneratePodsWave(
        ICelestialEphemeris ephemeris, ulong seed, int count, double nowSimTime, int waveNumber, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { PodLaunchers.Count: > 0 }
            ? GeneratePodsFromScenario(ephemeris, seed, count, effective, nowSimTime, $"pod-w{waveNumber}", $"Pod-{waveNumber}.")
            : GeneratePodsFromFixedLauncher(ephemeris, seed, count, nowSimTime, $"pod-w{waveNumber}", $"Pod-{waveNumber}.");
    }

    private static IReadOnlyList<NpcShip> GenerateFromFixedTables(
        ICelestialEphemeris ephemeris, ulong seed, int count, double baseSimTime = 0, string idPrefix = "npc")
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
            string callsign = Callsigns[i % Callsigns.Length];

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
                    $"{idPrefix}-{i}", callsign, cargo, origin, destination, personality,
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
            else
            {
                double departure = baseSimTime + Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, departure, personality, rng);
                ships.Add(new NpcShip(
                    $"{idPrefix}-{i}", callsign, cargo, origin, destination, personality,
                    departure, departure, route.DepartureState, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
        }

        return ships;
    }

    private static IReadOnlyList<NpcShip> GenerateFromScenario(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition traffic,
        double baseSimTime = 0, string idPrefix = "npc")
    {
        var rng = new DeterministicRandom(seed);
        var catchUpSim = new Simulator(ephemeris, CatchUpTimeStep);
        var ships = new List<NpcShip>(count);

        (List<RouteDefinition> longHaul, List<RouteDefinition> shortHaul) = SplitRoutesByDistance(ephemeris, traffic.Routes);
        IReadOnlyList<RouteDefinition> all = traffic.Routes;

        int midFlight = Math.Max(1, count * 6 / 10);
        for (int i = 0; i < count; i++)
        {
            bool isMidFlight = i < midFlight;
            IReadOnlyList<RouteDefinition> pool = isMidFlight
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
            string callsign = Callsigns[i % Callsigns.Length];
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
                    $"{idPrefix}-{i}", callsign, chosen.Cargo, chosen.From, chosen.To, personality,
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false,
                    PublishesTimetable: chosen.PublishesTimetable));
            }
            else
            {
                double departure = baseSimTime + Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planFrom, planTo, departure, personality, rng);
                ships.Add(new NpcShip(
                    $"{idPrefix}-{i}", callsign, chosen.Cargo, chosen.From, chosen.To, personality,
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

    private static IReadOnlyList<NpcShip> GeneratePodsFromFixedLauncher(
        ICelestialEphemeris ephemeris, ulong seed, int count,
        double baseSimTime = 0, string idPrefix = "pod", string callsignPrefix = "Pod-")
    {
        var rng = new DeterministicRandom(seed);
        var launchSim = new Simulator(ephemeris, CatchUpTimeStep);
        var pods = new List<NpcShip>(count);

        for (int i = 0; i < count; i++)
        {
            string destination = rng.NextInt(0, 2) == 0 ? "mars" : "venus";
            double departure = baseSimTime + Math.Floor(rng.NextDouble(0.5 * Day, 10 * Day));

            // Plan the route like a ship (one burst + coast; the arrival brake is dropped —
            // the customer catches the pod), then run just past the burst and declare that
            // state the launch: everything the driver gave it, nothing it can change.
            NpcRoute route = RoutePlanner.PlanRoute(ephemeris, "earth", destination, departure, RoutePersonality.Economical, rng);
            ManeuverNode burn = route.Plan.Nodes[0];
            var launchPlan = new ManeuverPlan([burn]);
            ShipState launched = launchSim.Run(route.DepartureState, (burn.SimTime - departure) + CatchUpTimeStep, launchPlan);

            pods.Add(new NpcShip(
                $"{idPrefix}-{i}", $"{callsignPrefix}{i + 1}", "Compute cores", "luna", destination,
                RoutePersonality.Economical, departure, launched.SimTime, launched,
                // 5 units × 400 cr = exactly the first upgrade (2000 cr): one clean milk run
                // finishes the tutorial. 4 units would dead-end it 400 credits short.
                ManeuverPlan.Empty, route.EstimatedArrivalTime,
                CargoUnits: 5, ManeuverBudget: 0, IsPod: true));
        }

        return pods;
    }

    private static IReadOnlyList<NpcShip> GeneratePodsFromScenario(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition traffic,
        double baseSimTime = 0, string idPrefix = "pod", string callsignPrefix = "Pod-")
    {
        var rng = new DeterministicRandom(seed);
        var launchSim = new Simulator(ephemeris, CatchUpTimeStep);
        var pods = new List<NpcShip>(count);
        IReadOnlyList<PodLauncherDefinition> launchers = traffic.PodLaunchers;

        for (int i = 0; i < count; i++)
        {
            PodLauncherDefinition launcher = launchers[rng.NextInt(0, launchers.Count)];
            string planningOrigin = PlanningBodyId(ephemeris, launcher.Body);
            string destination = PickPodDestination(ephemeris, planningOrigin, rng);
            double departure = baseSimTime + Math.Floor(rng.NextDouble(0.5 * Day, 10 * Day));

            NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planningOrigin, destination, departure, RoutePersonality.Economical, rng);
            ManeuverNode burn = route.Plan.Nodes[0];
            var launchPlan = new ManeuverPlan([burn]);
            ShipState launched = launchSim.Run(route.DepartureState, (burn.SimTime - departure) + CatchUpTimeStep, launchPlan);

            pods.Add(new NpcShip(
                $"{idPrefix}-{i}", $"{callsignPrefix}{i + 1}", launcher.Cargo, launcher.Body, destination,
                RoutePersonality.Economical, departure, launched.SimTime, launched,
                ManeuverPlan.Empty, route.EstimatedArrivalTime,
                CargoUnits: 5, ManeuverBudget: 0, IsPod: true));
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
