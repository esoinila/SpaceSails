namespace SpaceSails.Core;

/// <summary>
/// A predicted future track for a target: the center line dead-reckoned from an observation
/// (optionally under a pinned hypothesis plan), and an uncertainty half-width that grows with
/// time since that observation. Rendered as a cone on the map.
/// </summary>
public sealed record PredictedPath(Observation Source, IReadOnlyList<TrajectorySample> Samples)
{
    /// <summary>Instrument position/velocity noise at the moment of observation.</summary>
    public const double BaseHalfWidthMeters = 1e7;

    /// <summary>Velocity measurement uncertainty, meters/second.</summary>
    public const double VelocitySigma = 100;

    /// <summary>
    /// Plausible unobserved maneuvering, expressed as an equivalent acceleration: a ship
    /// pulsing ±10% (~3 km/s each) several times a day can shift ~27 km/s of Δv, i.e.
    /// ~0.3 m/s² averaged. The cone passes the 1e9 m capture threshold after ~a day dark.
    /// </summary>
    public const double ManeuverBudgetAcceleration = 0.3;

    /// <summary>Uncertainty half-width at a sim time at or after the source observation.</summary>
    public double HalfWidthAt(double simTime)
    {
        double dt = Math.Max(0, simTime - Source.SimTime);
        return BaseHalfWidthMeters + VelocitySigma * dt + 0.5 * ManeuverBudgetAcceleration * dt * dt;
    }
}

/// <summary>
/// Dead-reckoning predictor: integrate the observed state forward with the game's own
/// simulator — gravity is public knowledge; only the target's future burns are not. A pinned
/// hypothesis supplies those burns; if it is right, the center line converges to the target's
/// actual track (the M5 accept test).
/// </summary>
public static class PathPredictor
{
    public static PredictedPath Predict(
        ICelestialEphemeris ephemeris,
        Observation observation,
        ManeuverPlan? hypothesis,
        double horizonSeconds)
    {
        var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0);
        var state = new ShipState(observation.Position, observation.Velocity, observation.SimTime);
        IReadOnlyList<TrajectorySample> samples = simulator.ProjectAdaptive(
            state, hypothesis, horizonSeconds, maxSamples: SampleBudget(horizonSeconds));
        return new PredictedPath(observation, samples);
    }

    // ProjectAdaptive silently stops at maxSamples; a long-horizon prediction must budget for
    // its whole span at the coarse ceiling (1 h) or the far end of the track just vanishes.
    private static int SampleBudget(double horizonSeconds) =>
        Math.Max(2, (int)(horizonSeconds / 3600) + 16);

    /// <summary>
    /// The standard pirate hunch: "it brakes at its destination". Dead-reckons to the closest
    /// approach to <paramref name="destinationId"/> and hypothesizes a decelerate burst there.
    /// </summary>
    public static ManeuverPlan BrakeAtHypothesis(
        ICelestialEphemeris ephemeris,
        Observation observation,
        string destinationId,
        double horizonSeconds,
        int brakePulses = 10)
    {
        var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0);
        var state = new ShipState(observation.Position, observation.Velocity, observation.SimTime);
        IReadOnlyList<TrajectorySample> ballistic = simulator.ProjectAdaptive(
            state, null, horizonSeconds, maxSamples: SampleBudget(horizonSeconds));

        double bestDistance = double.MaxValue;
        double brakeTime = observation.SimTime + horizonSeconds;
        foreach (TrajectorySample sample in ballistic)
        {
            double d = (ephemeris.Position(destinationId, sample.SimTime) - sample.Position).Length;
            if (d < bestDistance)
            {
                (bestDistance, brakeTime) = (d, sample.SimTime);
            }
        }

        return new ManeuverPlan([new ManeuverNode(brakeTime, ManeuverAction.Decelerate, brakePulses)]);
    }
}
