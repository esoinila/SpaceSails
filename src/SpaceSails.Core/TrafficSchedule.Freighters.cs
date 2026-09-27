using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE FREIGHTERS' WAVE, TABLE BY TABLE — the two walks that plan a wave of haulers (the fixed Sol
/// tables and a scenario's own routes), the route pickers they share, and the rng <c>Clone</c> their probe
/// plans draw from.
///
/// <para>Split out of <c>TrafficSchedule.cs</c> under #251 as a pure move: no member renamed, re-scoped or
/// re-ordered. Two runs of the base file — the walks, and <c>Clone</c> from the foot of it, which only these
/// walks call. Every <c>static readonly</c> table they read stays in the opening file, in its original
/// order (#1163).</para>
/// </summary>
public static partial class TrafficSchedule
{
    private static IEnumerable<TrafficStep> GenerateFromFixedTables(
        ICelestialEphemeris ephemeris, ulong seed, int count, double baseSimTime = 0, int wave = 0)
    {
        var rng = new DeterministicRandom(seed);
        var catchUpSim = new Simulator(ephemeris, CatchUpTimeStep);

        int midFlight = Math.Max(1, count * 6 / 10);
        for (int i = 0; i < count; i++)
        {
            bool isMidFlight = i < midFlight;
            (string origin, string destination, string cargo) = isMidFlight
                ? LongRoutes[rng.NextInt(0, LongRoutes.Length)]
                : ShortRoutes[rng.NextInt(0, ShortRoutes.Length)];
            var personality = (RoutePersonality)rng.NextInt(0, 3);
            string id = NpcId("npc", wave, i);
            string callsign = Callsigns[i % Callsigns.Count] + WaveTag(wave);

            int cargoUnits = rng.NextInt(5, 21);
            if (isMidFlight)
            {
                // Plan from a virtual past departure, then declare the coarse catch-up state at
                // ~t=0 the ship's truth. Deterministic forward from there; remaining plan nodes
                // (mid-course evasive bursts, the arrival brake) still execute live.
                double lead = rng.NextDouble(20 * Day, 70 * Day);
                NpcRoute probe = RoutePlanner.PlanRoute(ephemeris, origin, destination, baseSimTime, personality, Clone(rng));
                yield return new TrafficStep(i, count, null);   // #161 · the crossing is measured

                double transfer = probe.EstimatedArrivalTime - baseSimTime;
                // The world does not wait for the player: the remaining lead can never exceed
                // the transfer itself, or a short hop would "spawn mid-flight" at a departure
                // time in the FUTURE and the starting sky would be empty.
                lead = Math.Min(lead, transfer * rng.NextDouble(0.3, 0.8));
                double virtualDeparture = baseSimTime - (transfer - lead);

                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, virtualDeparture, personality, rng);
                yield return new TrafficStep(i, count, null);   // #161 · …and flown, from the departure it implies

                // #161 · …and the catch-up, which is the longest of the three, taken a slice at a time. The
                // LAST state is the answer (Simulator.RunSliceBySlice), so `now` simply keeps the newest.
                ShipState now = route.DepartureState;
                foreach (ShipState slice in catchUpSim.RunSliceBySlice(
                             route.DepartureState, baseSimTime - virtualDeparture, route.Plan, CatchUpStepsPerSlice))
                {
                    now = slice;
                    yield return new TrafficStep(i, count, null);
                }

                yield return new TrafficStep(i, count, new NpcShip(
                    id, callsign, cargo, origin, destination, personality,
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
            else
            {
                double departure = baseSimTime + Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, departure, personality, rng);
                yield return new TrafficStep(i, count, new NpcShip(
                    id, callsign, cargo, origin, destination, personality,
                    departure, departure, route.DepartureState, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false));
            }
        }
    }

    private static IEnumerable<TrafficStep> GenerateFromScenario(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition traffic,
        double baseSimTime = 0, int wave = 0)
    {
        var rng = new DeterministicRandom(seed);
        var catchUpSim = new Simulator(ephemeris, CatchUpTimeStep);

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
            string callsign = Callsigns[i % Callsigns.Count] + WaveTag(wave);
            int cargoUnits = rng.NextInt(5, 21);

            if (isMidFlight)
            {
                double lead = rng.NextDouble(20 * Day, 70 * Day);
                NpcRoute probe = RoutePlanner.PlanRoute(ephemeris, planFrom, planTo, baseSimTime, personality, Clone(rng));
                yield return new TrafficStep(i, count, null);   // #161 · the crossing is measured

                double transfer = probe.EstimatedArrivalTime - baseSimTime;
                // Same clamp as the fixed tables: mid-flight means genuinely EN ROUTE as of the
                // wave base time, even when the scenario routes are short hops.
                lead = Math.Min(lead, transfer * rng.NextDouble(0.3, 0.8));
                double virtualDeparture = baseSimTime - (transfer - lead);

                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planFrom, planTo, virtualDeparture, personality, rng);
                yield return new TrafficStep(i, count, null);   // #161 · …and flown, from the departure it implies

                // #161 · …and the catch-up, which is the longest of the three, taken a slice at a time. The
                // LAST state is the answer (Simulator.RunSliceBySlice), so `now` simply keeps the newest.
                ShipState now = route.DepartureState;
                foreach (ShipState slice in catchUpSim.RunSliceBySlice(
                             route.DepartureState, baseSimTime - virtualDeparture, route.Plan, CatchUpStepsPerSlice))
                {
                    now = slice;
                    yield return new TrafficStep(i, count, null);
                }

                yield return new TrafficStep(i, count, new NpcShip(
                    id, callsign, chosen.Cargo, chosen.From, chosen.To, personality,
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false,
                    PublishesTimetable: chosen.PublishesTimetable));
            }
            else
            {
                double departure = baseSimTime + Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planFrom, planTo, departure, personality, rng);
                yield return new TrafficStep(i, count, new NpcShip(
                    id, callsign, chosen.Cargo, chosen.From, chosen.To, personality,
                    departure, departure, route.DepartureState, route.Plan, route.EstimatedArrivalTime,
                    cargoUnits, NpcShip.DefaultManeuverBudget, IsPod: false,
                    PublishesTimetable: chosen.PublishesTimetable));
            }
        }
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

    // The probe plan and the real plan must consume identical random sequences so the schedule
    // stays deterministic regardless of how PlanRoute uses its rng internally.
    private static DeterministicRandom Clone(DeterministicRandom rng)
    {
        // Fork a child stream from the parent's next output; both sides remain deterministic.
        return new DeterministicRandom(rng.NextUInt64());
    }
}
