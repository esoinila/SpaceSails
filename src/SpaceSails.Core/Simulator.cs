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
public sealed partial class Simulator
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

    /// <summary>
    /// #264 — the sanity bound above which one live step is split into equal substeps so a close, fast
    /// pass stays energy-honest. The fixed 1 s step is accurate on a cruise, but at a deep-well periapsis
    /// the point-mass acceleration is large and <c>a·dt</c> can be a real fraction of the speed; a single
    /// semi-implicit Euler step then drifts orbital energy (the owner's Uranus incident shed km/s on pure
    /// integration error, masquerading as aerobraking). Split whenever <c>a·dt &gt; 0.05·|v|</c> — a 5 %
    /// velocity kick in one step. Away from every mass <c>a·dt</c> is negligible, so the guard never fires
    /// and the flight is bit-identical to <see cref="Step"/>: only a genuine close approach pays.
    /// </summary>
    public const double MaxVelocityChangeFraction = 0.05;

    /// <summary>Hard cap on live substeps so a graze can never stall a frame (#264).</summary>
    public const int MaxLiveSubsteps = 64;

    /// <summary>
    /// One live step, adaptively substepped near a mass so a fast close pass keeps its energy (#264).
    /// Identical to <see cref="Step"/> — same result to the bit — whenever the close-approach sanity
    /// bound (<see cref="MaxVelocityChangeFraction"/>) is not exceeded, which is the entire solar cruise;
    /// only a deep, fast periapsis drives the substep count up (capped at <see cref="MaxLiveSubsteps"/>).
    /// This is the live game's honest replacement for a raw fixed step; a minimum-radius clamp was
    /// rejected on purpose (it changes the physics), and the surface itself is enforced separately by
    /// <see cref="SurfaceImpact"/>, so nothing here ever integrates the interior.
    /// </summary>
    public ShipState StepGuarded(ShipState state, ManeuverPlan? plan = null) => StepGuarded(state, plan, TimeStep);

    /// <inheritdoc cref="StepGuarded(ShipState, ManeuverPlan?)"/>
    public ShipState StepGuarded(ShipState state, ManeuverPlan? plan, double dt)
    {
        int substeps = LiveSubstepCount(state.Position, state.Velocity, state.SimTime, dt);
        if (substeps <= 1)
        {
            return StepBy(state, plan, dt);
        }

        double sub = dt / substeps;
        for (int i = 0; i < substeps; i++)
        {
            state = StepBy(state, plan, sub);
        }

        return state;
    }

    /// <summary>
    /// How many equal substeps a step of <paramref name="dt"/> needs to keep the velocity kick under the
    /// sanity bound: 1 (no split) whenever the point-mass acceleration is gentle — everywhere but a close,
    /// fast pass — and <c>ceil(a·dt / bound)</c> capped at <see cref="MaxLiveSubsteps"/> otherwise. The
    /// bound has an absolute 1 m/s floor so a near-stationary ship deep in a well still splits sanely.
    /// </summary>
    public int LiveSubstepCount(Vector2d position, Vector2d velocity, double simTime, double dt)
    {
        double accel = GravitationalAcceleration(position, simTime).Length;
        if (accel <= 0.0)
        {
            return 1;
        }

        double bound = Math.Max(velocity.Length * MaxVelocityChangeFraction, 1.0);
        double ratio = accel * dt / bound;
        return ratio <= 1.0 ? 1 : Math.Min(MaxLiveSubsteps, (int)Math.Ceiling(ratio));
    }

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

        velocity += acceleration * dt;

        // Atmospheric drag: nothing in a vacuum scenario (the array is empty, the block is skipped),
        // and exactly zero — no touch to `velocity` — whenever the ship is outside every shell, so an
        // atmosphere-bearing world flies bit-identically to a vacuum one above the shells (Lab 22's
        // sacred regression gate). Applied SEMI-IMPLICITLY (issue #153): quadratic drag is stiff, and
        // the adaptive projection can step tens of seconds at a time. An explicit `v += a_drag·dt`
        // overshoots — deep in a dense shell a_drag·dt dwarfs the speed, so the velocity reverses and
        // amplifies every step, flinging the PLOTTED trajectory into a spurious multi-AU ray (the live
        // 1 s sim never hit it). The closed form v_rel ← v_rel / (1 + c·|v_rel|·dt) is unconditionally
        // dissipative (the denominator is ≥ 1, so |v_rel| can never grow or flip), exact for pure
        // quadratic drag, and agrees with the old explicit step to first order — so shallow fine-step
        // skims are unchanged while a coarse deep plunge just sheds its relative speed instead of
        // exploding. Drag can only ever remove energy; this makes the integrator obey that at any dt.
        if (_atmosphereBodies.Length > 0)
        {
            velocity = ApplyStableDrag(state.Position, velocity, state.SimTime, dt);
        }

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
    /// #161 · <see cref="Run"/>, WITH SOMEWHERE FOR THE CALLER TO BREATHE — the same run, handed back every
    /// <paramref name="stepsPerSlice"/> steps, the LAST element being the answer <see cref="Run"/> returns.
    ///
    /// <para><b>Why it is not a loop of shorter Runs.</b> The traffic planner's catch-up integration — the
    /// 20–70 days a mid-flight hauler has already been flying — is the longest single block left in the boot
    /// after #1114, at 1.8–2.1 s on the interpreted payload. The obvious fix is to call <see cref="Run"/>
    /// twice for half the duration each; the obvious fix is wrong, because the end time would then be
    /// computed off an ACCUMULATED <c>SimTime</c> rather than the original one, and a last-bit difference
    /// there decides whether the loop takes one more step. One step at this timestep is two hours of a
    /// hauler's flight, and this repository's fingerprint guards would call that a different sky.</para>
    ///
    /// <para><b>So it is the same loop.</b> The same <c>endTime</c>, computed once from the same state; the
    /// same condition; the same <see cref="Step"/> calls in the same order. The only thing that has been
    /// added is a yield — and an iterator suspends and resumes on values it already holds, so the run is
    /// byte-identical by construction rather than by argument. <c>TheSameRunHoweverItIsSliced</c> holds it
    /// anyway, at every slice size, because "by construction" is exactly the claim that stays true until
    /// somebody moves a line.</para>
    /// </summary>
    public IEnumerable<ShipState> RunSliceBySlice(
        ShipState state, double durationSeconds, ManeuverPlan? plan, int stepsPerSlice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepsPerSlice);

        return Slices(state, durationSeconds, plan, stepsPerSlice);

        IEnumerable<ShipState> Slices(ShipState state, double durationSeconds, ManeuverPlan? plan, int stepsPerSlice)
        {
            double endTime = state.SimTime + durationSeconds;
            int stepsSinceTheLastBreath = 0;
            while (state.SimTime < endTime)
            {
                state = Step(state, plan);
                if (++stepsSinceTheLastBreath == stepsPerSlice)
                {
                    stepsSinceTheLastBreath = 0;
                    yield return state;
                }
            }

            // The answer, always — even when the run happened to end exactly on a slice boundary and the
            // caller has just been handed this very state. A caller taking the LAST element is then reading
            // the same thing Run would have returned, with no special case of its own to get wrong.
            yield return state;
        }
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
}
