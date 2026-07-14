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
    private readonly PlasmaEnvironment? _environment;

    // The subset of bodies that carry an atmosphere, cached at construction. Empty for every
    // vacuum scenario, which is what keeps the no-atmosphere path exactly free: the drag block is
    // skipped wholesale when there is nothing to drag against (see StepBy).
    private readonly CelestialBody[] _atmosphereBodies;

    /// <summary>
    /// The ship's ballistic coefficient BC = m/(C_d·A) in kg/m^2 — the single aerobrake knob (PR-H).
    /// Drag deceleration is <c>a = −0.5·ρ·|v_rel|·v_rel / BC</c>, so a larger BC (heavier or slicker
    /// ship) brakes less. This one constant is what the game tunes; the labs measure the corridor it
    /// produces. Chosen at 120 kg/m^2 — a compact, capsule-like value that gives a readable Jupiter
    /// braking corridor and a believably narrow Apollo-return skip corridor with the sol.json shells.
    /// </summary>
    public const double BallisticCoefficient = 120.0;

    public Simulator(ICelestialEphemeris ephemeris, double timeStepSeconds, PlasmaEnvironment? environment = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeStepSeconds);
        _ephemeris = ephemeris;
        _environment = environment;
        _atmosphereBodies = [.. ephemeris.Bodies.Where(b => b.Atmosphere is not null)];
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
            // ApplyBurnsInWindow handles both burn modes; for a pure Factor plan it is exactly the
            // old velocity *= scale, so existing trajectories are bit-identical.
            velocity = plan.ApplyBurnsInWindow(velocity, state.SimTime, state.SimTime + dt);
        }

        Vector2d acceleration = GravitationalAcceleration(state.Position, state.SimTime);
        double charge = state.Charge;
        if (_environment is not null)
        {
            // Hull charge relaxes toward the local ambient level, then the stream force acts on
            // whatever charge the hull carries this step. Clamped exponential approach: stable
            // for any dt (ProjectAdaptive can step hours at a time).
            double ambient = _environment.AmbientCharge(state.Position, state.SimTime);
            double blend = Math.Min(1.0, dt / PlasmaEnvironment.EquilibrationTau);
            charge += (ambient - charge) * blend;
            acceleration += _environment.Acceleration(state.Position, charge, state.SimTime);
        }

        // Atmospheric drag: nothing in a vacuum scenario (the array is empty, the block is skipped),
        // and exactly zero — no touch to `acceleration` — whenever the ship is outside every shell.
        if (_atmosphereBodies.Length > 0)
        {
            Vector2d drag = DragAcceleration(state.Position, velocity, state.SimTime);
            if (drag.X != 0.0 || drag.Y != 0.0)
            {
                acceleration += drag;
            }
        }

        velocity += acceleration * dt;
        Vector2d position = state.Position + velocity * dt;

        return new ShipState(position, velocity, state.SimTime + dt, charge);
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
    /// <summary>
    /// Advance the ship by exactly <paramref name="durationSeconds"/> using the same
    /// dynamical-time adaptive stepping as <see cref="ProjectAdaptive"/> (and the same exact
    /// landing on plan-node times), returning only the final state. This is the live game's
    /// high-warp path (M19): at 10000× the fixed 1 s loop costs 10,000 gravity evaluations per
    /// real second, almost all of them wasted in deep space where the dynamical time is weeks.
    /// Deterministic: step sizes are a pure function of ship state, so equal quanta always
    /// produce identical results regardless of frame timing.
    /// </summary>
    public ShipState RunAdaptive(
        ShipState state,
        double durationSeconds,
        ManeuverPlan? plan = null,
        double minTimeStep = 1.0,
        double maxTimeStep = 3600.0,
        double dynamicalTimeFraction = 1.0 / 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationSeconds);

        double endTime = state.SimTime + durationSeconds;
        IReadOnlyList<ManeuverNode> nodes = plan?.Nodes ?? [];
        int nextNode = 0;
        while (nextNode < nodes.Count && nodes[nextNode].SimTime <= state.SimTime)
        {
            nextNode++;
        }

        while (state.SimTime < endTime)
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

            while (nextNode < nodes.Count && nodes[nextNode].SimTime <= state.SimTime)
            {
                nextNode++;
            }
        }

        return state;
    }

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

    /// <summary>
    /// Total atmospheric drag acceleration at a ship state (m/s^2), summed over every body whose
    /// shell the ship is currently inside. <c>a = −0.5·ρ(h)·|v_rel|·v_rel / BC</c>, where ρ(h) is the
    /// body's exponential density at the ship's altitude, v_rel is the ship's velocity minus the
    /// body's rail velocity (the shell translates with the body; its spin is ignored), and BC is
    /// <see cref="BallisticCoefficient"/>. Returns <see cref="Vector2d.Zero"/> outside every shell.
    /// </summary>
    public Vector2d DragAcceleration(Vector2d position, Vector2d velocity, double simTime)
    {
        Vector2d drag = Vector2d.Zero;

        foreach (CelestialBody body in _atmosphereBodies)
        {
            Vector2d bodyPosition = _ephemeris.Position(body.Id, simTime);
            double altitude = (position - bodyPosition).Length - body.BodyRadius;
            double density = body.Atmosphere!.DensityAt(altitude);
            if (density <= 0.0)
            {
                continue;
            }

            Vector2d vRel = velocity - BodyVelocity(body.Id, simTime);
            double speed = vRel.Length;
            if (speed == 0.0)
            {
                continue;
            }

            drag += vRel * (-0.5 * density * speed / BallisticCoefficient);
        }

        return drag;
    }

    // Rail velocity of a body by central finite difference of its analytic position — deterministic
    // (same pure Position function on both sides) and only ever evaluated for a body whose shell the
    // ship is actually inside, so it never touches the vacuum hot path.
    private Vector2d BodyVelocity(string bodyId, double simTime) =>
        (_ephemeris.Position(bodyId, simTime + 1.0) - _ephemeris.Position(bodyId, simTime - 1.0)) / 2.0;

    /// <summary>
    /// A queryable summary of an aerobrake pass: the peak drag deceleration reached (the damage-line
    /// input), the total Δv the atmosphere shed, the peak dynamic pressure, the deepest altitude
    /// touched, the exit speed, and which body's atmosphere dominated. This is the exact shape the
    /// game's corridor gauge (PR-I) consumes — "pulses saved" from <see cref="DeltaVShedMetersPerSecond"/>,
    /// the hull-damage threshold from <see cref="PeakDecelG"/>, the depth read-out from
    /// <see cref="MinAltitudeMeters"/>. A pass that never entered a shell reports all-zero with a
    /// null body id.
    /// </summary>
    public readonly record struct DragReport(
        double PeakDecelMetersPerSecondSquared,
        double DeltaVShedMetersPerSecond,
        double PeakDynamicPressurePascal,
        double MinAltitudeMeters,
        double ExitSpeedMetersPerSecond,
        string? DominantBodyId)
    {
        /// <summary>Peak drag deceleration expressed in standard gravities (÷ 9.80665 m/s^2).</summary>
        public double PeakDecelG => PeakDecelMetersPerSecondSquared / 9.80665;
    }

    /// <summary>
    /// Fly the ship exactly as <see cref="RunAdaptive"/> does — same dynamical-time stepping, same
    /// deterministic result — while measuring the aerobrake pass, and return both the final state and
    /// a <see cref="DragReport"/>. The report samples drag at each step's start state (the same inputs
    /// the integrator's own drag term uses), so the trajectory is byte-identical to a plain
    /// <see cref="RunAdaptive"/> and the report describes that very flight. Designed as the public API
    /// PR-I's corridor gauge calls to price a skim.
    /// </summary>
    public (ShipState Final, DragReport Report) RunAdaptiveWithDrag(
        ShipState state,
        double durationSeconds,
        ManeuverPlan? plan = null,
        double minTimeStep = 1.0,
        double maxTimeStep = 3600.0,
        double dynamicalTimeFraction = 1.0 / 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationSeconds);

        double endTime = state.SimTime + durationSeconds;
        IReadOnlyList<ManeuverNode> nodes = plan?.Nodes ?? [];
        int nextNode = 0;
        while (nextNode < nodes.Count && nodes[nextNode].SimTime <= state.SimTime)
        {
            nextNode++;
        }

        double peakDecel = 0.0, deltaVShed = 0.0, peakDynamicPressure = 0.0;
        double minAltitude = double.PositiveInfinity;
        string? dominantBody = null;

        while (state.SimTime < endTime)
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

            // Read drag at the same post-burn velocity StepBy will use this step, so the metrics
            // describe the flight the integrator actually produces.
            Vector2d preVelocity = state.Velocity;
            if (plan is not null)
            {
                preVelocity = plan.ApplyBurnsInWindow(preVelocity, state.SimTime, state.SimTime + dt);
            }

            AccumulateDragMetrics(
                state.Position, preVelocity, state.SimTime, dt,
                ref peakDecel, ref deltaVShed, ref peakDynamicPressure, ref minAltitude, ref dominantBody);

            state = StepBy(state, plan, dt);

            while (nextNode < nodes.Count && nodes[nextNode].SimTime <= state.SimTime)
            {
                nextNode++;
            }
        }

        return (state, new DragReport(
            peakDecel, deltaVShed, peakDynamicPressure,
            double.IsPositiveInfinity(minAltitude) ? double.NaN : minAltitude,
            state.Velocity.Length, dominantBody));
    }

    // Fold one step's drag into the running peak/shed/depth accumulators. Mirrors DragAcceleration's
    // per-body math so the numbers are the exact drag the integrator applied, plus the extra
    // diagnostics (dynamic pressure, altitude, dominant body) the gauge wants.
    private void AccumulateDragMetrics(
        Vector2d position, Vector2d velocity, double simTime, double dt,
        ref double peakDecel, ref double deltaVShed, ref double peakDynamicPressure,
        ref double minAltitude, ref string? dominantBody)
    {
        if (_atmosphereBodies.Length == 0)
        {
            return;
        }

        Vector2d totalDrag = Vector2d.Zero;
        double deepestActive = double.PositiveInfinity;
        double bestDensity = 0.0;
        string? bestBody = null;
        double bestQ = 0.0;

        foreach (CelestialBody body in _atmosphereBodies)
        {
            Vector2d bodyPosition = _ephemeris.Position(body.Id, simTime);
            double altitude = (position - bodyPosition).Length - body.BodyRadius;
            double density = body.Atmosphere!.DensityAt(altitude);
            if (density <= 0.0)
            {
                continue;
            }

            Vector2d vRel = velocity - BodyVelocity(body.Id, simTime);
            double speed = vRel.Length;
            totalDrag += vRel * (-0.5 * density * speed / BallisticCoefficient);

            if (altitude < deepestActive)
            {
                deepestActive = altitude;
            }

            double q = 0.5 * density * speed * speed;
            if (q > bestQ)
            {
                bestQ = q;
            }

            if (density > bestDensity)
            {
                bestDensity = density;
                bestBody = body.Id;
            }
        }

        double decel = totalDrag.Length;
        deltaVShed += decel * dt;
        if (decel > peakDecel)
        {
            peakDecel = decel;
            dominantBody = bestBody;
        }

        if (bestQ > peakDynamicPressure)
        {
            peakDynamicPressure = bestQ;
        }

        if (deepestActive < minAltitude)
        {
            minAltitude = deepestActive;
        }
    }
}
