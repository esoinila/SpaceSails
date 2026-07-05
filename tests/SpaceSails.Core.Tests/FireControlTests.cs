namespace SpaceSails.Core.Tests;

/// <summary>
/// M28 (Sunday PR-A): the firing solution — the shooting method run on the real Simulator —
/// plus the two Lab-found honesty fixes it depends on: PathPredictor's post-burn cone
/// (Lab 08) and the fast-graze closed-form closest approach (Lab 06).
/// </summary>
public class FireControlTests
{
    private const double SunMu = 1.32712440018e20;
    private const double EarthOrbitRadius = 1.496e11;

    private static CircularOrbitEphemeris SunOnly() =>
        new([new CelestialBody("sun", "Sun", null, SunMu, 6.9634e8, 0, 0, 0)]);

    private sealed class EmptySpace : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } = [];

        public Vector2d Position(string bodyId, double simTime) => Vector2d.Zero;
    }

    [Fact]
    public void Solve_NoGravity_StationaryShooter_IsExactGeometry()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var shooter = new ShipState(Vector2d.Zero, Vector2d.Zero, 0);

        // The aim point needs exactly 1 km/s for an hour — the solver must find that charge.
        FireControl.Solution solution = FireControl.Solve(
            simulator, shooter, maxMuzzleSpeed: 4000, new Vector2d(3.6e6, 0), tHit: 3600);

        Assert.True(solution.Converged);
        Assert.True(solution.ExpectedMissMeters < 1e3, $"miss {solution.ExpectedMissMeters:E2}");
        Assert.Equal(0, solution.BearingRad, precision: 3);
        Assert.Equal(1000, solution.MuzzleSpeed, precision: 0);
    }

    [Fact]
    public void Solve_CrossSystemShot_MonthsOfGravity_Converges()
    {
        // The owner's ruling: shooting across the star system is a legitimate action. A months-
        // long transfer's launch bearing has nothing to do with the target's current sky
        // position, so this is the multi-start seed's acceptance test. The aim point is
        // manufactured to be reachable BY CONSTRUCTION: fly a known launch for 90 days, then
        // demand the solver rediscover a shot that hits where it ended up.
        var simulator = new Simulator(SunOnly(), timeStepSeconds: 60);
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var shooter = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);

        // A retrograde-ish 5 km/s launch falls sunward for 90 days — a genuinely curved,
        // cross-system arc (it ends nowhere near any straight line from the shooter).
        var knownLaunchDir = new Vector2d(-0.4, -0.9).Normalized();
        var round = new ShipState(shooter.Position, shooter.Velocity + knownLaunchDir * 5000, 0);
        double tHit = 90 * 86400.0;
        Vector2d aimPoint = simulator.RunAdaptive(round, tHit).Position;

        FireControl.Solution solution = FireControl.Solve(
            simulator, shooter, maxMuzzleSpeed: 8000, aimPoint, tHit);

        Assert.True(solution.Converged,
            $"90-day cross-system shot must converge (best miss {solution.ExpectedMissMeters:E2} m)");
        Assert.True(solution.ExpectedMissMeters < FireControl.ConvergedMissMeters);
    }

    [Fact]
    public void Solve_NoGravity_MovingShooter_LeadsTheShot()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        // Shooter drifting +Y at 500 m/s: to put the slug dead on +X the launch must cancel
        // the drift — the converged bearing points below the +X axis.
        var shooter = new ShipState(Vector2d.Zero, new Vector2d(0, 500), 0);

        FireControl.Solution solution = FireControl.Solve(
            simulator, shooter, maxMuzzleSpeed: 4000, new Vector2d(3.0e6, 0), tHit: 3600);

        Assert.True(solution.Converged);
        Assert.True(solution.BearingRad < -0.4, $"bearing {solution.BearingRad:F3} should lead against the drift");
        Assert.True(solution.ExpectedMissMeters < 1e3);
    }

    [Fact]
    public void Solve_SunGravity_ConvergesAndTheTraceShowsIt()
    {
        var ephemeris = SunOnly();
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        double circular = Math.Sqrt(SunMu / EarthOrbitRadius);
        var shooter = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circular), 0);

        // A point ahead on the orbit and 3e7 m anti-sunward, 6 h out: reachable at ~1.5 km/s,
        // but the sun bends the slug ~1.4e6 m over the flight — the straight-line first guess
        // misses by more than the tolerance and Newton has to work for it.
        double tHit = 6 * 3600;
        Vector2d target = shooter.Position + new Vector2d(3e7, circular * tHit * 0.98);

        FireControl.Solution solution = FireControl.Solve(simulator, shooter, 4000, target, tHit);

        Assert.True(solution.Converged, $"final miss {solution.ExpectedMissMeters:E2}");
        Assert.True(solution.Trace.Count >= 2, "the solve should take visible iterations");
        Assert.True(solution.Trace[^1].MissMeters < solution.Trace[0].MissMeters,
            "the trace must converge downward — the gun deck shows this");
        Assert.True(solution.ValiditySeconds >= 60,
            $"a sane solution keeps for at least a minute (got {solution.ValiditySeconds}s)");
    }

    [Fact]
    public void Solve_TargetBeyondTheDriversReach_ReportsInfeasible()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var shooter = new ShipState(Vector2d.Zero, Vector2d.Zero, 0);

        // Needs 6 km/s; the driver maxes at 2 km/s. Honest answer: best effort, not converged.
        FireControl.Solution solution = FireControl.Solve(
            simulator, shooter, maxMuzzleSpeed: 2000, new Vector2d(2.16e7, 0), tHit: 3600);

        Assert.False(solution.Converged);
        Assert.True(solution.ExpectedMissMeters > 1e6);
        Assert.Equal(0, solution.ValiditySeconds);
    }

    [Fact]
    public void PredictedCone_CoversAPostBurnTarget_Lab08Fix()
    {
        // Lab 08's dishonesty, reproduced: observe a cruiser, it fires a 2-pulse +10% burst
        // an hour later. The OLD cone (no impulse term) excluded the truth at 2 h and 6 h.
        var ephemeris = SunOnly();
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        double circular = Math.Sqrt(SunMu / EarthOrbitRadius);
        var observed = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circular), 0);
        var observation = new Observation("prey", 0, observed.Position, observed.Velocity);

        var burst = new ManeuverPlan([new ManeuverNode(3600, ManeuverAction.Accelerate, Pulses: 2)]);
        PredictedPath predicted = PathPredictor.Predict(ephemeris, observation, null, 6 * 3600);

        foreach (double dt in new[] { 2 * 3600.0, 6 * 3600.0 })
        {
            ShipState truth = simulator.RunAdaptive(observed, dt, burst);
            Vector2d deadReckoned = PositionAlong(predicted.Samples, dt);
            double deviation = (truth.Position - deadReckoned).Length;
            double cone = predicted.HalfWidthAt(dt);
            Assert.True(deviation < cone,
                $"at {dt / 3600:F0} h the truth ({deviation:E2} m off) must sit inside the cone ({cone:E2} m)");
        }
    }

    [Fact]
    public void PredictedCone_PodStaysNeedleThin()
    {
        // A mass-driver pod (budget 0) cannot burn: the Lab 08 fix must not fatten its cone.
        var observation = new Observation("pod", 0, new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, 30000));
        PredictedPath predicted = PathPredictor.Predict(SunOnly(), observation, null, 6 * 3600, maneuverBudget: 0);

        Assert.Equal(0, predicted.ImpulseBudget);
        double dt = 6 * 3600;
        Assert.Equal(
            PredictedPath.BaseHalfWidthMeters + PredictedPath.VelocitySigma * dt,
            predicted.HalfWidthAt(dt), precision: 3);
    }

    [Fact]
    public void Intercept_FastGrazeBetweenSamples_IsCaught_Lab06Fix()
    {
        // Two paths crossing at t=105, sampled only every 30 s with no sample near the
        // crossing: the old per-sample check reported a miss of ~300,000 km-scale units;
        // the closed-form segment minimum finds the graze.
        static List<TrajectorySample> Line(Vector2d start, Vector2d velocity, double t0, double t1, double dt)
        {
            var samples = new List<TrajectorySample>();
            for (double t = t0; t <= t1 + 1e-9; t += dt)
            {
                samples.Add(new TrajectorySample(t, start + velocity * (t - t0)));
            }

            return samples;
        }

        var ours = Line(new Vector2d(-1.05e6, 0), new Vector2d(1e4, 0), 0, 210, 30);
        var theirs = Line(new Vector2d(0, -1.05e6), new Vector2d(0, 1e4), 0, 210, 30);

        InterceptEstimate.Result? result = InterceptEstimate.Against(ours, theirs, thresholdMeters: 5e4);

        Assert.NotNull(result);
        Assert.True(result.Value.MinDistance < 1e3,
            $"the graze at t=105 must be caught exactly (got {result.Value.MinDistance:E2} m)");
        Assert.Equal(105, result.Value.MinSimTime, tolerance: 0.5);
        Assert.NotNull(result.Value.FirstWithinThresholdSimTime);
        // Separation is sqrt(2)·|t−105|·1e4 ≤ 5e4 from t ≈ 105 − 3.54 s.
        Assert.Equal(105 - 5e4 / Math.Sqrt(2) / 1e4, result.Value.FirstWithinThresholdSimTime!.Value, tolerance: 0.5);
    }

    [Fact]
    public void SegmentMin_ClosedForm_NoTunneling()
    {
        // Relative motion sweeping straight through the origin inside one step.
        (double min, double sMin, double? within) =
            InterceptEstimate.SegmentMin(new Vector2d(-1e6, 2e3), new Vector2d(1e6, 2e3), 1e4);

        Assert.Equal(2e3, min, precision: 6);
        Assert.Equal(0.5, sMin, precision: 6);
        Assert.NotNull(within);
    }

    private static Vector2d PositionAlong(IReadOnlyList<TrajectorySample> samples, double simTime)
    {
        for (int i = 0; i < samples.Count - 1; i++)
        {
            if (samples[i + 1].SimTime >= simTime)
            {
                TrajectorySample a = samples[i];
                TrajectorySample b = samples[i + 1];
                double span = b.SimTime - a.SimTime;
                double f = span > 0 ? (simTime - a.SimTime) / span : 0;
                return a.Position + (b.Position - a.Position) * f;
            }
        }

        return samples[^1].Position;
    }
}
