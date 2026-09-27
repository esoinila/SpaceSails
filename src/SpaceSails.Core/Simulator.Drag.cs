namespace SpaceSails.Core;

/// <summary>
/// #251 · THE DRAG REPORT — the record, the adaptive run that fills it, and the metrics accumulated on
/// the way.
///
/// <para>Split out of <c>Simulator.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field.</para>
/// </summary>
public sealed partial class Simulator
{
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
