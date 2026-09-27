namespace SpaceSails.Core;

/// <summary>
/// #251 · THE ADAPTIVE INTEGRATOR — <c>RunAdaptive</c> and <c>ProjectAdaptive</c>, the step that shortens
/// itself near a body.
///
/// <para>Split out of <c>Simulator.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field — the two readonly fields and the constructor that sets them stay
/// in the opening file.</para>
/// </summary>
public sealed partial class Simulator
{
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
}
