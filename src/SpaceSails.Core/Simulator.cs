namespace SpaceSails.Core;

/// <summary>
/// Fixed-timestep, deterministic ship integrator. Celestial bodies are on rails (see
/// <see cref="ICelestialEphemeris"/>); ships feel their point-mass gravity and execute
/// <see cref="ManeuverPlan"/>s. Semi-implicit Euler keeps orbits energy-stable.
///
/// Determinism is law here: no wall clock, no randomness, no environment-dependent math.
/// The same initial state, plan, and step count must produce bit-identical results on
/// client (WASM) and server.
/// </summary>
public sealed class Simulator
{
    private readonly ICelestialEphemeris _ephemeris;

    public Simulator(ICelestialEphemeris ephemeris, double timeStepSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeStepSeconds);
        _ephemeris = ephemeris;
        TimeStep = timeStepSeconds;
    }

    public double TimeStep { get; }

    /// <summary>Advance one fixed timestep. Maneuver nodes scheduled inside the step fire first.</summary>
    public ShipState Step(ShipState state, ManeuverPlan? plan = null)
    {
        Vector2d velocity = state.Velocity;

        if (plan is not null)
        {
            double scale = plan.ScaleFactorInWindow(state.SimTime, state.SimTime + TimeStep);
            if (scale != 1.0)
            {
                velocity *= scale;
            }
        }

        velocity += GravitationalAcceleration(state.Position, state.SimTime) * TimeStep;
        Vector2d position = state.Position + velocity * TimeStep;

        return new ShipState(position, velocity, state.SimTime + TimeStep);
    }

    /// <summary>Advance by whole steps until at least <paramref name="durationSeconds"/> has elapsed.</summary>
    public ShipState Run(ShipState state, double durationSeconds, ManeuverPlan? plan = null)
    {
        double endTime = state.SimTime + durationSeconds;
        while (state.SimTime < endTime)
        {
            state = Step(state, plan);
        }

        return state;
    }

    /// <summary>
    /// Project the trajectory forward as a polyline (used by plotting mode and trajectory ribbons).
    /// Runs the exact same integration as <see cref="Step"/>, sampling every
    /// <paramref name="sampleEverySteps"/> steps. The first point is the current position.
    /// </summary>
    public IReadOnlyList<Vector2d> Project(ShipState state, ManeuverPlan? plan, double horizonSeconds, int sampleEverySteps = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleEverySteps);

        int steps = (int)Math.Ceiling(horizonSeconds / TimeStep);
        var points = new List<Vector2d>(steps / sampleEverySteps + 2) { state.Position };

        for (int i = 1; i <= steps; i++)
        {
            state = Step(state, plan);
            if (i % sampleEverySteps == 0 || i == steps)
            {
                points.Add(state.Position);
            }
        }

        return points;
    }

    /// <summary>Sum of point-mass gravity from every body with nonzero Mu.</summary>
    public Vector2d GravitationalAcceleration(Vector2d position, double simTime)
    {
        Vector2d acceleration = Vector2d.Zero;

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.Mu == 0)
            {
                continue;
            }

            Vector2d toBody = _ephemeris.Position(body.Id, simTime) - position;
            double distanceSquared = toBody.LengthSquared;

            // Inside the physical body the point-mass model is meaningless (and singular);
            // collision handling is a later milestone, so just clamp the force off.
            if (distanceSquared < body.BodyRadius * body.BodyRadius || distanceSquared == 0)
            {
                continue;
            }

            double distance = Math.Sqrt(distanceSquared);
            acceleration += toBody * (body.Mu / (distanceSquared * distance));
        }

        return acceleration;
    }
}
