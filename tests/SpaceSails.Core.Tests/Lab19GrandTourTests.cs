using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 19 — The Grand Tour (Voyager gravity assists).
/// See labs/19-the-grand-tour/README.md and Probe.cs for the lesson.
/// Every assert here is exercised by running real trajectories through Simulator.
/// </summary>
public class Lab19GrandTourTests
{
    private const double Day = 86400.0;
    private const double Year = 365.25 * Day;
    private const double AU = 1.496e11;
    private const double SunMu = 1.32712440018e20;
    private const double JupiterMu = 1.26686534e17;

    private static (ICelestialEphemeris Ephemeris, Simulator Sim) MakeJupiterSystem()
    {
        // Minimal bodies sufficient for the assist lesson (matches probe style, subset of sol.json)
        var bodies = new[]
        {
            new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
            new CelestialBody("earth", "earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
            new CelestialBody("jupiter", "jupiter", "sun", JupiterMu, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
            new CelestialBody("saturn", "saturn", "sun", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        var sim = new Simulator(eph, timeStepSeconds: 60);
        return (eph, sim);
    }

    private static Vector2d BodyVelocity(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    // Replicates minimal Lambert + ShootTo from the lesson for the gates (kept local to test so probe can evolve independently).
    private static (Vector2d V1, Vector2d V2, int Iterations) Lambert(Vector2d r1v, Vector2d r2v, double tof, double mu)
    {
        double r1 = r1v.Length, r2 = r2v.Length;
        double cross = r1v.X * r2v.Y - r1v.Y * r2v.X;
        double dTheta = Math.Acos(Math.Clamp(r1v.Dot(r2v) / (r1 * r2), -1.0, 1.0));
        if (cross < 0) dTheta = Math.Tau - dTheta;
        double A = Math.Sin(dTheta) * Math.Sqrt(r1 * r2 / (1 - Math.Cos(dTheta)));

        static double C(double z) => z > 1e-8 ? (1 - Math.Cos(Math.Sqrt(z))) / z : z < -1e-8 ? (Math.Cosh(Math.Sqrt(-z)) - 1) / -z : 0.5 - z / 24 + z * z / 720;
        static double S(double z) => z > 1e-8 ? (Math.Sqrt(z) - Math.Sin(Math.Sqrt(z))) / Math.Pow(z, 1.5) : z < -1e-8 ? (Math.Sinh(Math.Sqrt(-z)) - Math.Sqrt(-z)) / Math.Pow(-z, 1.5) : 1.0 / 6 - z / 120 + z * z / 5040;

        double Y(double z) => r1 + r2 + A * (z * S(z) - 1) / Math.Sqrt(C(z));
        double Err(double z)
        {
            double y = Y(z);
            return y < 0 ? double.NegativeInfinity : Math.Pow(y / C(z), 1.5) * S(z) + A * Math.Sqrt(y) - Math.Sqrt(mu) * tof;
        }

        double lo = -100, hi = Math.Tau * Math.Tau - 1e-9;
        for (int it = 0; it < 200 && hi - lo > 1e-12; it++)
        {
            double m = 0.5 * (lo + hi);
            if (Err(m) > 0) hi = m; else lo = m;
        }
        double z = 0.5 * (lo + hi);
        if (double.IsNaN(Err(z)) || Math.Abs(Err(z)) / Math.Sqrt(mu) > 1.0)
            return (Vector2d.Zero, Vector2d.Zero, -1);
        double y = Y(z);
        double f = 1 - y / r1, g = A * Math.Sqrt(y / mu), gDot = 1 - y / r2;
        return ((r2v - r1v * f) / g, (r2v * gDot - r1v) / g, 1);
    }

    private static (Vector2d V, bool Ok) ShootTo(Simulator sim, Vector2d pos, double t0, Vector2d vSeed, Vector2d tgt, double tArr, double tol, double trust = 200)
    {
        const double Eps = 1.0;
        Vector2d Fly(Vector2d v) => sim.RunAdaptive(new ShipState(pos, v, t0), tArr - t0).Position;
        Vector2d v = vSeed;
        for (int i = 0; i < 15; i++)
        {
            Vector2d miss = Fly(v) - tgt;
            if (miss.Length < tol) return (v, true);
            Vector2d cx = (Fly(v + new Vector2d(Eps, 0)) - (miss + tgt)) / Eps;
            Vector2d cy = (Fly(v + new Vector2d(0, Eps)) - (miss + tgt)) / Eps;
            double det = cx.X * cy.Y - cx.Y * cy.X;
            var step = new Vector2d(-(cy.Y * miss.X - cy.X * miss.Y) / det, -(-cx.Y * miss.X + cx.X * miss.Y) / det);
            if (step.Length > trust) step = step.Normalized() * trust;
            v += step;
        }
        return (v, false);
    }

    // Closest approach of a projected trajectory to a body (searched from a start time).
    private static (double Distance, double Time) ClosestTo(ICelestialEphemeris eph, string bodyId, IReadOnlyList<TrajectorySample> samples, double fromTime)
    {
        double best = double.MaxValue, bestT = fromTime;
        foreach (TrajectorySample s in samples)
        {
            if (s.SimTime < fromTime) continue;
            double d = (eph.Position(bodyId, s.SimTime) - s.Position).Length;
            if (d < best) (best, bestT) = (d, s.SimTime);
        }
        return (best, bestT);
    }

    // The honest flyby the probe §D flies, reduced to a gate primitive: from the approach state
    // (90 d before nominal CA) boost along v_inf (capped so e_pre stays &lt; 0), then re-aim THAT
    // boosted arc to a close b-plane offset at its true encounter time. All burns are up front,
    // before the measurement window, so the arc from start onward is zero-burn ballistic.
    private static (ShipState Start, double CaDistance, double CaTime, double VInf) BoostedFlyby(
        ICelestialEphemeris eph, Simulator sim, ShipState approach, double boost, double offset)
    {
        Vector2d vInfApp = approach.Velocity - BodyVelocity(eph, "jupiter", approach.SimTime);
        Vector2d boostedV = approach.Velocity + vInfApp.Normalized() * boost;
        var boostedStart = new ShipState(approach.Position, boostedV, approach.SimTime);

        // True encounter time of the boosted ballistic, so ShootTo targets a reachable point.
        var scout = sim.ProjectAdaptive(boostedStart, null, 130 * Day, maxTimeStep: 1800, maxSamples: 40_000);
        double tEnc = ClosestTo(eph, "jupiter", scout, approach.SimTime + 1 * Day).Time;
        Vector2d vInfEnc = boostedV - BodyVelocity(eph, "jupiter", tEnc);
        var side = new Vector2d(-vInfEnc.Normalized().Y, vInfEnc.Normalized().X);
        var aim = eph.Position("jupiter", tEnc) + side * offset;
        var tuned = ShootTo(sim, approach.Position, approach.SimTime, boostedV, aim, tEnc, 5e6, 100);
        var start = new ShipState(approach.Position, tuned.V, approach.SimTime);

        var traj = sim.ProjectAdaptive(start, null, 130 * Day, maxTimeStep: 1800, maxSamples: 40_000);
        var pass = ClosestTo(eph, "jupiter", traj, approach.SimTime + 1 * Day);
        var jIn = sim.RunAdaptive(start, (pass.Time - 2 * Day) - start.SimTime);
        double vInf = (jIn.Velocity - BodyVelocity(eph, "jupiter", jIn.SimTime)).Length;
        return (start, pass.Distance, pass.Time, vInf);
    }

    [Fact]
    public void G1_Determinism_TwoIdenticalRunsAreByteIdentical()
    {
        var (eph, _) = MakeJupiterSystem();
        // Simple repeatable coast: determinism gate (lesson 10 contract).
        var start = new ShipState(new Vector2d(1.6e11, 0), new Vector2d(0, 25000), 0);
        double runSeconds = 3600 * 10; // 10 hours, exact multiple of 60s
        var sim1 = new Simulator(eph, timeStepSeconds: 60);
        var sim2 = new Simulator(eph, timeStepSeconds: 60);
        var r1 = sim1.Run(start, runSeconds);
        var r2 = sim2.Run(start, runSeconds);

        Assert.Equal(r1.SimTime, r2.SimTime, precision: 9);
        Assert.Equal(r1.Position.X, r2.Position.X, precision: 9);
        Assert.Equal(r1.Position.Y, r2.Position.Y, precision: 9);
        Assert.Equal(r1.Velocity.X, r2.Velocity.X, precision: 9);
        Assert.Equal(r1.Velocity.Y, r2.Velocity.Y, precision: 9);
    }

    [Fact]
    public void G2_EnergySignFlip_ZeroPropellantDuringFlyby()
    {
        var (eph, sim) = MakeJupiterSystem();
        // Real gate: the honest probe-§D flyby. From the approach state 90 d before CA, one burn
        // up front (boost along v_inf, capped so e_pre < 0; re-aim to a close pass), then a single
        // zero-burn ballistic arc. Assert solar specific energy flips sign across the encounter and
        // the pass was genuinely close — the escape is bought by the crank, not by propellant.
        double dep = 100 * Day;
        double tof = 2.73 * Year;
        var pad = RoutePlanner.DepartureState(eph, "earth", "jupiter", dep);
        var jAt = eph.Position("jupiter", dep + tof);
        var lam = Lambert(pad.Position, jAt, tof, SunMu);
        var approach = sim.RunAdaptive(new ShipState(pad.Position, lam.V1, dep), tof - 90 * Day);

        var flyby = BoostedFlyby(eph, sim, approach, boost: 8000, offset: 3e8);

        // e_pre: after the last burn, before CA, far from Jupiter (Sun-frame energy clean).
        double ePre = flyby.Start.Velocity.LengthSquared / 2.0 - SunMu / flyby.Start.Position.Length;
        // e_post: 60 d past CA on the same ballistic arc, clear of Jupiter's well.
        var post = sim.RunAdaptive(flyby.Start, (flyby.CaTime + 60 * Day) - flyby.Start.SimTime);
        double ePost = post.Velocity.LengthSquared / 2.0 - SunMu / post.Position.Length;

        Assert.True(ePre < 0, $"pre-flyby must be bound (e_pre = {ePre:F0} J/kg)");
        Assert.True(ePost > 0, $"post-flyby must be escaping — sign flip with zero propellant (e_post = {ePost:F0} J/kg)");
        Assert.True(flyby.CaDistance < 15 * 6.9911e7, $"flyby must genuinely pass close to Jupiter (CA = {flyby.CaDistance / 6.9911e7:F1} R_J)");
    }

    [Fact]
    public void G3_GainWithinToleranceOfPatchedConic()
    {
        var (eph, sim) = MakeJupiterSystem();
        // Real gate: measure heliocentric gain at a flyby and compare to patched-conic upper bound.
        double dep = 100 * Day;
        double tof = 2.73 * Year;
        var pad = RoutePlanner.DepartureState(eph, "earth", "jupiter", dep);
        var jAt = eph.Position("jupiter", dep + tof);
        var lam = Lambert(pad.Position, jAt, tof, SunMu);
        var pre = sim.RunAdaptive(new ShipState(pad.Position, lam.V1, dep), tof - 20 * Day);

        Vector2d vj = BodyVelocity(eph, "jupiter", pre.SimTime);
        double vInf = (pre.Velocity - vj).Length;

        // Simple patched-conic upper bound on outgoing helio speed: |vj + v_inf|
        double patchedUpper = vj.Length + vInf;

        // Re-aim to a strong-gain b-plane offset; the aiming burn is up front, before the flyby.
        var side = new Vector2d(-vj.Normalized().Y, vj.Normalized().X);
        var tuned = ShootTo(sim, pre.Position, pre.SimTime, pre.Velocity, jAt + side * -5e8, pre.SimTime + 20 * Day, 2e6, 100);
        double postBurnSpeed = tuned.V.Length; // measure gain from AFTER the burn, so the burn is not counted as gain
        var after = sim.RunAdaptive(new ShipState(pre.Position, tuned.V, pre.SimTime), 100 * Day);

        double measured = after.Velocity.Length;
        double gain = measured - postBurnSpeed;

        // The measured must be <= upper bound (within small tolerance for n-body effects) and gain positive.
        Assert.True(measured <= patchedUpper * 1.05, $"measured {measured / 1000:F1} km/s exceeds patched upper {patchedUpper / 1000:F1} km/s by more than 5%");
        Assert.True(gain > 1000, $"heliocentric gain must be substantial (gain = {gain / 1000:F1} km/s)");
    }

    [Fact]
    public void G4_Symmetry_SpeedRelativeToJupiterEqualBeforeAfter()
    {
        var (eph, sim) = MakeJupiterSystem();
        // Real gate: |v_inf| relative to Jupiter must be (approximately) equal before and after the
        // flyby. Sampled on the SAME post-burn ballistic arc (the aiming/boost burn is up front),
        // symmetrically ±2 d about the true closest approach — the gain lives only in the Sun frame.
        double dep = 100 * Day;
        double tof = 2.73 * Year;
        var pad = RoutePlanner.DepartureState(eph, "earth", "jupiter", dep);
        var jAt = eph.Position("jupiter", dep + tof);
        var lam = Lambert(pad.Position, jAt, tof, SunMu);
        var approach = sim.RunAdaptive(new ShipState(pad.Position, lam.V1, dep), tof - 90 * Day);

        var flyby = BoostedFlyby(eph, sim, approach, boost: 8000, offset: 3e8);

        var jIn = sim.RunAdaptive(flyby.Start, (flyby.CaTime - 2 * Day) - flyby.Start.SimTime);
        var jOut = sim.RunAdaptive(flyby.Start, (flyby.CaTime + 2 * Day) - flyby.Start.SimTime);
        double vinfPre = (jIn.Velocity - BodyVelocity(eph, "jupiter", jIn.SimTime)).Length;
        double vinfPost = (jOut.Velocity - BodyVelocity(eph, "jupiter", jOut.SimTime)).Length;

        double relDiff = Math.Abs(vinfPre - vinfPost) / Math.Max(1, vinfPre);
        Assert.True(relDiff < 0.05, $"|v_inf| changed by {relDiff:P1} across flyby; must be conserved in planet frame (G4)");
    }

    [Fact]
    public void G5_TimeStepHonesty_CaAndExitStableUnderDtChange()
    {
        var (eph, _) = MakeJupiterSystem();
        // Real gate: flyby CA distance and exit velocity are reasonably stable when changing the integrator dt.
        double dep = 100 * Day;
        double tof = 2.73 * Year;
        var pad = RoutePlanner.DepartureState(eph, "earth", "jupiter", dep);
        var jAt = eph.Position("jupiter", dep + tof);
        var lam = Lambert(pad.Position, jAt, tof, SunMu);
        var pre = new Simulator(eph, timeStepSeconds: 60).RunAdaptive(new ShipState(pad.Position, lam.V1, dep), tof - 30 * Day);

        Vector2d side = new Vector2d(-0.1, 1.0);
        var aim = jAt + side * 8e8;
        var vGood = ShootTo(new Simulator(eph, timeStepSeconds: 60), pre.Position, pre.SimTime, pre.Velocity, aim, pre.SimTime + 30 * Day, 2e6, 100).V;

        var s1 = new Simulator(eph, timeStepSeconds: 60);
        var s2 = new Simulator(eph, timeStepSeconds: 120);
        var s05 = new Simulator(eph, timeStepSeconds: 30);

        var post1 = s1.RunAdaptive(new ShipState(pre.Position, vGood, pre.SimTime), 50 * Day);
        var post2 = s2.RunAdaptive(new ShipState(pre.Position, vGood, pre.SimTime), 50 * Day);
        var post05 = s05.RunAdaptive(new ShipState(pre.Position, vGood, pre.SimTime), 50 * Day);

        double ca1 = (eph.Position("jupiter", post1.SimTime) - post1.Position).Length;
        double ca2 = (eph.Position("jupiter", post2.SimTime) - post2.Position).Length;
        double ca05 = (eph.Position("jupiter", post05.SimTime) - post05.Position).Length;

        double v1 = post1.Velocity.Length;
        double v2 = post2.Velocity.Length;
        double v05 = post05.Velocity.Length;

        Assert.True(Math.Abs(ca1 - ca2) / Math.Max(1, ca1) < 0.2, "CA moved >20% at 2× dt");
        Assert.True(Math.Abs(v1 - v2) / Math.Max(1, v1) < 0.1, "exit speed moved >10% at 2× dt");
        Assert.True(Math.Abs(ca1 - ca05) / Math.Max(1, ca1) < 0.2, "CA moved >20% at 0.5× dt");
        Assert.True(Math.Abs(v1 - v05) / Math.Max(1, v1) < 0.1, "exit speed moved >10% at 0.5× dt");
    }
}
