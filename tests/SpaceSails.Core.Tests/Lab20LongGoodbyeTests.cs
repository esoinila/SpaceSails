using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 20 — The Long Goodbye (the Grand Tour after the Saturn hand-off, coasted to a
/// fixed present). See labs/20-the-long-goodbye/README.md and Probe.cs for the lesson.
///
/// Every assert here is exercised by flying real trajectories through Simulator. The gates
/// reconstruct lesson 19's flown winner from its KNOWN parameters (depart day 6413, Earth→Jupiter
/// leg 3.4 yr, b-plane aim −1485 Mm — the values lesson 19's scan/sweep produce) rather than
/// re-running the full 20-year window scan, so the whole file stays well under a minute.
/// </summary>
public class Lab20LongGoodbyeTests
{
    private const double Day = 86400.0;
    private const double Year = 365.25 * Day;
    private const double AU = 1.496e11;
    private const double SunMu = 1.32712440018e20;
    private const double JupiterMu = 1.26686534e17;

    // The pinned present + display anchor the probe uses (Section C). NEVER DateTime.Now.
    private static readonly DateTimeOffset Present = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LaunchEpoch = new(1977, 8, 20, 14, 29, 0, TimeSpan.Zero);

    private static (ICelestialEphemeris Ephemeris, Simulator Sim) MakeOuterSystem()
    {
        // Identical constants/body table to the probe (9-body; Uranus/Neptune present for parity).
        var bodies = new[]
        {
            new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
            new CelestialBody("mercury", "mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
            new CelestialBody("venus", "venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
            new CelestialBody("earth", "earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
            new CelestialBody("mars", "mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
            new CelestialBody("jupiter", "jupiter", "sun", JupiterMu, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
            new CelestialBody("saturn", "saturn", "sun", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
            new CelestialBody("uranus", "uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
            new CelestialBody("neptune", "neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    private static Vector2d BodyVelocity(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    // Minimal Lambert + ShootTo + ClosestTo, kept local so the probe can evolve independently
    // (same convention as Lab19GrandTourTests).
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
        double yv = Y(z);
        double f = 1 - yv / r1, g = A * Math.Sqrt(yv / mu), gDot = 1 - yv / r2;
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

    // Reconstruct lesson 19's flown hand-off (post-TCM-2, ballistic) from its KNOWN winner
    // parameters, WITHOUT the full window scan/sweep. Returns the hand-off state, the Saturn
    // closest-approach time, and the arrival distance at Saturn.
    private static (ShipState Handoff, double SatCaTime, double ArriveDist) ReconstructHandoff(ICelestialEphemeris eph, Simulator sim)
    {
        // Crank geometry side, exactly as the probe derives it (from the day-100 scout launch).
        double dep0 = 100 * Day;
        double aT = (AU + 7.7857e11) / 2;
        double tofEJ = Math.PI * Math.Sqrt(aT * aT * aT / SunMu);
        var pad0 = RoutePlanner.DepartureState(eph, "earth", "jupiter", dep0);
        var jup0 = eph.Position("jupiter", dep0 + tofEJ);
        var seed0 = Lambert(pad0.Position, jup0, tofEJ, SunMu);
        var launch0 = ShootTo(sim, pad0.Position, dep0, seed0.V1, jup0, dep0 + tofEJ, 5e7, 500);
        var before0 = sim.RunAdaptive(new ShipState(pad0.Position, launch0.V, dep0), tofEJ - 90 * Day);
        var vInf0 = before0.Velocity - BodyVelocity(eph, "jupiter", before0.SimTime);
        var side = new Vector2d(-vInf0.Normalized().Y, vInf0.Normalized().X);

        // The known winner (lesson 19's scan/sweep result).
        double t0C = 6413 * Day;
        double bestTof = 3.4 * Year;
        var padC = RoutePlanner.DepartureState(eph, "earth", "jupiter", t0C);
        var jupArrC = eph.Position("jupiter", t0C + bestTof);
        var launchSeed = Lambert(padC.Position, jupArrC, bestTof, SunMu);
        var liftoff = ShootTo(sim, padC.Position, t0C, launchSeed.V1, jupArrC + side * -5e8, t0C + bestTof, 1e7, 500);
        var nearJ = sim.RunAdaptive(new ShipState(padC.Position, liftoff.V, t0C), bestTof - 90 * Day);

        // TCM-1 straight to the known best b-plane offset (−1485 Mm) — no sweep needed.
        var chosen = ShootTo(sim, nearJ.Position, nearJ.SimTime, nearJ.Velocity, jupArrC + side * -1485e6, t0C + bestTof, 2e6, 100);

        // Saturn closest-approach time of the post-TCM-1 arc (drives TCM-2's aim and arrival).
        var projJS = sim.ProjectAdaptive(new ShipState(nearJ.Position, chosen.V, nearJ.SimTime), null, 5.5 * Year, maxTimeStep: 7200, maxSamples: 48_000);
        double chosenT = ClosestTo(eph, "saturn", projJS, t0C + bestTof + 100 * Day).Time;

        // TCM-2 at Jupiter+150d, exactly as lesson 19: walk arrival to a +1 Gm Saturn offset.
        var pastJ = sim.RunAdaptive(new ShipState(nearJ.Position, chosen.V, nearJ.SimTime), 90 * Day + 150 * Day);
        var satAim = eph.Position("saturn", chosenT);
        satAim += satAim.Normalized() * 1e9;
        var tcm2 = ShootTo(sim, pastJ.Position, pastJ.SimTime, pastJ.Velocity, satAim, chosenT, 1e9, 300);

        var handoff = new ShipState(pastJ.Position, tcm2.V, pastJ.SimTime);
        var atSaturn = sim.RunAdaptive(handoff, chosenT - handoff.SimTime);
        double arriveDist = (atSaturn.Position - eph.Position("saturn", chosenT)).Length;
        return (handoff, chosenT, arriveDist);
    }

    // Coast the ballistic hand-off arc to the pinned present, using the probe's exact two-stage
    // recipe: fine step (3600) through the Saturn encounter, then the long deep-space coast (86400).
    private static ShipState CoastToPresent(Simulator sim, ShipState handoff, double satCaTime)
    {
        double t0C = 6413 * Day;
        double tPresent = t0C + (Present - LaunchEpoch).TotalSeconds;
        double tSeam = satCaTime + 200 * Day;
        var seam = sim.RunAdaptive(handoff, tSeam - handoff.SimTime, maxTimeStep: 3600);
        return sim.RunAdaptive(seam, tPresent - seam.SimTime, maxTimeStep: 86400);
    }

    [Fact]
    public void G1_Determinism_PresentStateIdenticalAcrossTwoRuns()
    {
        var (eph1, sim1) = MakeOuterSystem();
        var (eph2, sim2) = MakeOuterSystem();

        var h1 = ReconstructHandoff(eph1, sim1);
        var h2 = ReconstructHandoff(eph2, sim2);
        var now1 = CoastToPresent(sim1, h1.Handoff, h1.SatCaTime);
        var now2 = CoastToPresent(sim2, h2.Handoff, h2.SatCaTime);

        // The full chain — reconstruction and 49-year coast — must be byte-identical run to run.
        Assert.Equal(now1.SimTime, now2.SimTime, precision: 9);
        Assert.Equal(now1.Position.X, now2.Position.X, precision: 6);
        Assert.Equal(now1.Position.Y, now2.Position.Y, precision: 6);
        Assert.Equal(now1.Velocity.X, now2.Velocity.X, precision: 9);
        Assert.Equal(now1.Velocity.Y, now2.Velocity.Y, precision: 9);
    }

    [Fact]
    public void G2_PostSaturnEnergySign_MatchesSectionA_BoundZeroBurn()
    {
        var (eph, sim) = MakeOuterSystem();
        var h = ReconstructHandoff(eph, sim);

        // Section A measures solar specific energy ±120 d about the Saturn CA on the zero-burn arc.
        // Recompute here (don't parse stdout) and assert the sign the probe prints: both sides BOUND
        // (the distant, high-v_inf pass brakes rather than escapes — Section A's verdict).
        var before = sim.RunAdaptive(h.Handoff, (h.SatCaTime - 120 * Day) - h.Handoff.SimTime);
        var after = sim.RunAdaptive(h.Handoff, (h.SatCaTime + 120 * Day) - h.Handoff.SimTime);
        double ePre = before.Velocity.LengthSquared / 2.0 - SunMu / before.Position.Length;
        double ePost = after.Velocity.LengthSquared / 2.0 - SunMu / after.Position.Length;

        Assert.True(ePre < 0, $"pre-Saturn must be bound (e_pre = {ePre:F0} J/kg)");
        Assert.True(ePost < 0, $"post-Saturn must stay bound on the zero-burn pass (e_post = {ePost:F0} J/kg)");

        // And the bound arc's apoapsis must fall short of Uranus (19.2 AU) — the 'no second crank'
        // physics Section A/B report.
        double a = -SunMu / (2 * ePost);
        double h2 = Math.Abs(after.Position.X * after.Velocity.Y - after.Position.Y * after.Velocity.X);
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * ePost * h2 * h2 / (SunMu * SunMu)));
        double apoapsisAU = a * (1 + e) / AU;
        Assert.True(apoapsisAU < 2.87246e12 / AU, $"zero-burn arc must not reach Uranus's orbit (apoapsis {apoapsisAU:F1} AU)");
    }

    [Fact]
    public void G3_FlownSaturnPass_MatchesLesson19Winner()
    {
        var (eph, sim) = MakeOuterSystem();
        var h = ReconstructHandoff(eph, sim);

        // Same winner as lesson 19: the flown Saturn hand-off is 1.07 Gm at day 9499.
        Assert.Equal(1.07e9, h.ArriveDist, 0.08e9);          // within 80 Mm of lesson 19's 1.07 Gm
        Assert.Equal(9499.0, h.SatCaTime / Day, 3.0);        // Saturn closest pass on day 9499 (±3 d)
    }
}
