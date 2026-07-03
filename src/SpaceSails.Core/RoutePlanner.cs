namespace SpaceSails.Core;

/// <summary>How an NPC captain flies. Shapes the burn structure the planner emits.</summary>
public enum RoutePersonality
{
    /// <summary>One modest early burn, then ballistic. Cheap, slow, easy to dead-reckon.</summary>
    Economical,

    /// <summary>Burns hard at departure. Expensive and fast; still predictable once seen.</summary>
    Fast,

    /// <summary>Splits its burn into seeded smaller bursts at odd times — dead-reckoning decays fast.</summary>
    Evasive,
}

/// <summary>A planned transfer: where the ship starts, what it will do, and when it should arrive.</summary>
public sealed record NpcRoute(ShipState DepartureState, ManeuverPlan Plan, double EstimatedArrivalTime, double EstimatedMissDistance);

/// <summary>
/// Deterministic NPC transfer planner. Propulsion is prograde-only ±10% pulses, so a transfer
/// is: a pulse burst at departure (decelerate to fall inward, accelerate to climb outward),
/// a long coast, and a brake burst at arrival. The burst size is picked by a small grid search
/// over the game's own <see cref="Simulator.ProjectAdaptive"/> — the planner can't disagree
/// with the sim that will fly the plan. Arrival tolerance is deliberately loose (NPCs despawn
/// within 1e10 m of the destination); that keeps the search to a handful of candidates.
/// </summary>
public static class RoutePlanner
{
    /// <summary>Matches the client's NPC despawn radius; also the planner's accept tolerance.</summary>
    public const double ArrivalToleranceMeters = 1e10;

    private const double BurnLeadSeconds = 3600;
    // The planner only needs 1e10 m accuracy, so it projects very coarsely: dt = 2 h keeps a
    // Saturn-transfer search around a few hundred thousand steps — startup-cheap even in WASM.
    private const double CoarseTimeStep = 7200;
    private const double MaxHorizonSeconds = 4e8; // ~12.7 years — covers a slow Saturn->inner fall

    public static NpcRoute PlanRoute(
        ICelestialEphemeris ephemeris,
        string originId,
        string destinationId,
        double departureTime,
        RoutePersonality personality,
        DeterministicRandom rng)
    {
        ShipState departure = DepartureState(ephemeris, originId, destinationId, departureTime);
        bool inward = OrbitRadiusOf(ephemeris, destinationId) < OrbitRadiusOf(ephemeris, originId);
        ManeuverAction burn = inward ? ManeuverAction.Decelerate : ManeuverAction.Accelerate;
        double horizon = TransferHorizon(ephemeris, originId);

        int[] candidates = personality == RoutePersonality.Fast ? [12, 16, 20] : [4, 6, 8, 10];

        var sim = new Simulator(ephemeris, CoarseTimeStep);
        int bestPulses = candidates[0];
        double bestMiss = double.MaxValue, bestArrival = departureTime + horizon;
        foreach (int pulses in candidates)
        {
            var plan = new ManeuverPlan([new ManeuverNode(departureTime + BurnLeadSeconds, burn, pulses)]);
            (double miss, double arrival) = ClosestApproach(sim, ephemeris, departure, plan, destinationId, horizon);
            // Fast captains take the earliest arrival that still hits; others take the cleanest hit.
            bool better = personality == RoutePersonality.Fast
                ? miss < ArrivalToleranceMeters && arrival < bestArrival
                : miss < bestMiss;
            if (better || bestMiss == double.MaxValue)
            {
                (bestPulses, bestMiss, bestArrival) = (pulses, miss, arrival);
            }
        }

        List<ManeuverNode> nodes = BuildBurnNodes(personality, burn, bestPulses, departureTime, bestArrival, rng);
        // Brake burst on arrival: reverse the departure logic so the ship roughly matches the
        // destination's orbit — and gives hypothesis pinning ("brakes at X") something true to pin.
        ManeuverAction brake = inward ? ManeuverAction.Accelerate : ManeuverAction.Decelerate;
        nodes.Add(new ManeuverNode(bestArrival, brake, bestPulses));

        var finalPlan = new ManeuverPlan(nodes);
        // Re-estimate only through the brake window: after braking the ship loiters near the
        // destination's orbit, and a full-horizon closest-approach would drift to some later
        // incidental pass instead of the actual arrival.
        double finalHorizon = Math.Min(horizon, bestArrival - departureTime + 30 * 86400);
        (double finalMiss, double finalArrival) = ClosestApproach(sim, ephemeris, departure, finalPlan, destinationId, finalHorizon);
        return new NpcRoute(departure, finalPlan, finalArrival, finalMiss);
    }

    private static List<ManeuverNode> BuildBurnNodes(
        RoutePersonality personality,
        ManeuverAction burn,
        int pulses,
        double departureTime,
        double arrivalTime,
        DeterministicRandom rng)
    {
        double burnTime = departureTime + BurnLeadSeconds;
        if (personality != RoutePersonality.Evasive || pulses < 2)
        {
            return [new ManeuverNode(burnTime, burn, pulses)];
        }

        // Evasive: same total impulse, but the second half fires at a seeded odd time in the
        // first 40% of the transfer. Between the bursts the ship is on neither the "did it
        // burn?" nor the "will it burn?" conic — dead-reckoning from a single sighting decays.
        int first = (pulses + 1) / 2;
        double split = departureTime + rng.NextDouble(0.10, 0.40) * (arrivalTime - departureTime);
        return
        [
            new ManeuverNode(burnTime, burn, first),
            new ManeuverNode(Math.Floor(split), burn, pulses - first),
        ];
    }

    private static (double Miss, double ArrivalTime) ClosestApproach(
        Simulator sim,
        ICelestialEphemeris ephemeris,
        ShipState state,
        ManeuverPlan plan,
        string destinationId,
        double horizon)
    {
        IReadOnlyList<TrajectorySample> samples = sim.ProjectAdaptive(
            state, plan, horizon, minTimeStep: CoarseTimeStep, maxTimeStep: CoarseTimeStep, maxSamples: 100_000);

        // Earliest pass inside tolerance wins — a captain takes the first arrival, not a
        // marginally closer conjunction years later. Falls back to the global minimum when
        // the route never gets inside tolerance (the caller can then reject or accept it).
        double miss = double.MaxValue, arrival = state.SimTime + horizon;
        foreach (TrajectorySample sample in samples)
        {
            double d = (ephemeris.Position(destinationId, sample.SimTime) - sample.Position).Length;
            if (d < miss)
            {
                (miss, arrival) = (d, sample.SimTime);
            }
            else if (miss < ArrivalToleranceMeters)
            {
                break; // past the local minimum of an in-tolerance approach
            }
        }

        return (miss, arrival);
    }

    /// <summary>Co-moving with the origin body, offset radially like the player spawn.</summary>
    public static ShipState DepartureState(ICelestialEphemeris ephemeris, string originId, string destinationId, double departureTime)
    {
        const double h = 1.0;
        Vector2d velocity = (ephemeris.Position(originId, departureTime + h)
                           - ephemeris.Position(originId, departureTime - h)) / (2 * h);
        Vector2d origin = ephemeris.Position(originId, departureTime);
        bool inward = OrbitRadiusOf(ephemeris, destinationId) < OrbitRadiusOf(ephemeris, originId);
        Vector2d offset = origin.Normalized() * (inward ? -5e9 : 5e9);
        return new ShipState(origin + offset, velocity, departureTime);
    }

    private static double OrbitRadiusOf(ICelestialEphemeris ephemeris, string bodyId)
    {
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Id == bodyId)
            {
                return body.OrbitRadius;
            }
        }

        throw new ArgumentException($"Unknown body '{bodyId}'.");
    }

    /// <summary>
    /// Generous projection horizon for a transfer from this origin: a fall from radius r takes
    /// on the order of its orbital period; double it for slow economical plans.
    /// </summary>
    private static double TransferHorizon(ICelestialEphemeris ephemeris, string originId)
    {
        double r = OrbitRadiusOf(ephemeris, originId);
        double mu = SunMu(ephemeris);
        return Math.Min(2 * Math.Tau * Math.Sqrt(r * r * r / mu), MaxHorizonSeconds);
    }

    private static double SunMu(ICelestialEphemeris ephemeris)
    {
        double best = 0;
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Mu > best)
            {
                best = body.Mu;
            }
        }

        return best;
    }
}
