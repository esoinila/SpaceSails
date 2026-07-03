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
    double EstimatedArrivalTime);

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
                    virtualDeparture, now.SimTime, now, route.Plan, route.EstimatedArrivalTime));
            }
            else
            {
                double departure = Math.Floor(rng.NextDouble(3 * Day, 30 * Day));
                NpcRoute route = RoutePlanner.PlanRoute(ephemeris, origin, destination, departure, personality, rng);
                ships.Add(new NpcShip(
                    $"npc-{i}", callsign, cargo, origin, destination, personality,
                    departure, departure, route.DepartureState, route.Plan, route.EstimatedArrivalTime));
            }
        }

        return ships;
    }

    // The probe plan and the real plan must consume identical random sequences so the schedule
    // stays deterministic regardless of how PlanRoute uses its rng internally.
    private static DeterministicRandom Clone(DeterministicRandom rng)
    {
        // Fork a child stream from the parent's next output; both sides remain deterministic.
        return new DeterministicRandom(rng.NextUInt64());
    }
}
