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
    public ShipState Step(ShipState state, ManeuverPlan? plan = null) => StepBy(state, plan, TimeStep);

    private ShipState StepBy(ShipState state, ManeuverPlan? plan, double dt)
    {
        Vector2d velocity = state.Velocity;

        if (plan is not null)
        {
            double scale = plan.ScaleFactorInWindow(state.SimTime, state.SimTime + dt);
            if (scale != 1.0)
            {
                velocity *= scale;
            }
        }

        velocity += GravitationalAcceleration(state.Position, state.SimTime) * dt;
        Vector2d position = state.Position + velocity * dt;

        return new ShipState(position, velocity, state.SimTime + dt);
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

    /// <summary>
    /// Project the trajectory forward with an adaptive timestep: a fixed fraction of the local
    /// dynamical time <c>min over bodies of sqrt(d³/μ)</c>, clamped to
    /// [<paramref name="minTimeStep"/>, <paramref name="maxTimeStep"/>]. Coarse in deep space,
    /// fine near a mass — the planning line stays cheap on a cruise and honest through a flyby.
    /// Steps additionally land exactly on every plan node's SimTime, so a plotted burn executes
    /// in the projection at the same instant the live fixed-dt sim fires it (same window rule as
    /// <see cref="Step"/>; contiguous windows guarantee each node fires exactly once).
    ///
    /// Deterministic: the step size is a pure function of ship state and ephemeris. The classic
    /// closed-form alternative (universal-variable Kepler / patched conics) is rejected on
    /// purpose — it assumes one attracting body per arc and would disagree with the integrator
    /// exactly where the game happens: flybys.
    /// </summary>
    public IReadOnlyList<TrajectorySample> ProjectAdaptive(
        ShipState state,
        ManeuverPlan? plan,
        double horizonSeconds,
        double minTimeStep = 1.0,
        double maxTimeStep = 3600.0,
        double dynamicalTimeFraction = 1.0 / 64,
        int maxSamples = 8192)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(horizonSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minTimeStep);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTimeStep, minTimeStep);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dynamicalTimeFraction);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSamples, 2);

        double endTime = state.SimTime + horizonSeconds;
        var samples = new List<TrajectorySample>(256) { new(state.SimTime, state.Position) };

        IReadOnlyList<ManeuverNode> nodes = plan?.Nodes ?? [];
        int nextNode = 0;
        while (nextNode < nodes.Count && nodes[nextNode].SimTime <= state.SimTime)
        {
            nextNode++;
        }

        while (state.SimTime < endTime && samples.Count < maxSamples)
        {
            double dt = Math.Clamp(
                DynamicalTime(state.Position, state.SimTime) * dynamicalTimeFraction,
                minTimeStep,
                maxTimeStep);

            double boundary = nextNode < nodes.Count ? Math.Min(endTime, nodes[nextNode].SimTime) : endTime;
            if (state.SimTime + dt > boundary)
            {
                dt = boundary - state.SimTime;
            }

            state = StepBy(state, plan, dt);
            samples.Add(new TrajectorySample(state.SimTime, state.Position));

            while (nextNode < nodes.Count && nodes[nextNode].SimTime <= state.SimTime)
            {
                nextNode++;
            }
        }

        return samples;
    }

    /// <summary>
    /// Shortest orbital timescale <c>sqrt(d³/μ)</c> over all attracting bodies at this position —
    /// small next to a heavy body, huge in deep space. Governs the adaptive projection step.
    /// </summary>
    public double DynamicalTime(Vector2d position, double simTime)
    {
        double shortest = double.MaxValue;

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.Mu == 0)
            {
                continue;
            }

            double distance = (_ephemeris.Position(body.Id, simTime) - position).Length;
            double tau = Math.Sqrt(distance * distance * distance / body.Mu);
            if (tau < shortest)
            {
                shortest = tau;
            }
        }

        return shortest;
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
