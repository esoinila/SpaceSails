using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for PR-G — the sling (<see cref="SlingPlanner"/>). Every assert flies real
/// trajectories through <see cref="Simulator"/> on a lab-19-style Jupiter system. The gates are
/// written the labs' way: they assert only CONVERGED results, with physical tolerances, and never
/// pin exact far-side positions (the flyby is a lever — Lab 20's cross-platform lesson).
/// </summary>
public class SlingPlannerTests
{
    private const double Day = 86400.0;
    private const double Year = 365.25 * Day;
    private const double AU = 1.496e11;
    private const double SunMu = 1.32712440018e20;
    private const double JupiterMu = 1.26686534e17;
    private const double RJ = 6.9911e7;

    private static (ICelestialEphemeris Ephemeris, Simulator Sim) MakeJupiterSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
            new CelestialBody("earth", "earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
            new CelestialBody("jupiter", "jupiter", "sun", JupiterMu, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
            new CelestialBody("saturn", "saturn", "sun", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    // Minimal Lambert, kept local (same convention as Lab19GrandTourTests) so the probe can evolve.
    private static (Vector2d V1, int Iterations) Lambert(Vector2d r1v, Vector2d r2v, double tof, double mu)
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
        if (double.IsNaN(Err(z)) || Math.Abs(Err(z)) / Math.Sqrt(mu) > 1.0) return (Vector2d.Zero, -1);
        double yv = Y(z);
        double f = 1 - yv / r1, g = A * Math.Sqrt(yv / mu);
        return ((r2v - r1v * f) / g, 1);
    }

    // A burn state on a real Earth->Jupiter approach that already passes close to Jupiter, ~40 d
    // before closest approach — the caller's "integrate the plan to the burn node" input.
    private static (ShipState Burn, double PassEpoch) ApproachBurnState(ICelestialEphemeris eph, Simulator sim)
    {
        double dep = 100 * Day, tof = 2.73 * Year;
        var pad = RoutePlanner.DepartureState(eph, "earth", "jupiter", dep);
        var jAt = eph.Position("jupiter", dep + tof);
        var lam = Lambert(pad.Position, jAt, tof, SunMu);
        var burn = sim.RunAdaptive(new ShipState(pad.Position, lam.V1, dep), tof - 40 * Day);
        return (burn, dep + tof);
    }

    [Fact]
    public void ConvergesToRequestedPassWithinTolerance()
    {
        var (eph, sim) = MakeJupiterSystem();
        var (burn, passEpoch) = ApproachBurnState(eph, sim);

        double requested = 12 * RJ;
        var req = new SlingPlanner.Request(burn, "jupiter", passEpoch, requested, SlingPlanner.PassSide.Lead);
        var result = SlingPlanner.Solve(sim, eph, req);

        Assert.True(result.Ok, $"solve must converge (failure: {result.Failure})");
        Assert.True(result.DeltaVMagnitude > 0, "an aiming burn must have been found");
        // Achieved pass must match the request within a sane tolerance (curvature + step size).
        Assert.InRange(result.AchievedPassDistance / requested, 0.85, 1.15);
        Assert.True(result.LeverGm > 0, "a flyby is a lever — the sensitivity number must be reported");
        Assert.True(result.PassEpoch > burn.SimTime, "the pass must be ahead of the burn");
    }

    [Fact]
    public void LeadGainsSpeed_TrailBrakes()
    {
        var (eph, sim) = MakeJupiterSystem();
        var (burn, passEpoch) = ApproachBurnState(eph, sim);
        double requested = 10 * RJ;

        var lead = SlingPlanner.Solve(sim, eph, new SlingPlanner.Request(burn, "jupiter", passEpoch, requested, SlingPlanner.PassSide.Lead));
        var trail = SlingPlanner.Solve(sim, eph, new SlingPlanner.Request(burn, "jupiter", passEpoch, requested, SlingPlanner.PassSide.Trail));

        Assert.True(lead.Ok, $"lead solve must converge ({lead.Failure})");
        Assert.True(trail.Ok, $"trail solve must converge ({trail.Failure})");
        // The crank's whole point: the two sides of the same pass gain vs shed heliocentric speed.
        Assert.True(lead.SpeedGain > trail.SpeedGain,
            $"lead ({lead.SpeedGain:F0} m/s) must gain more than trail ({trail.SpeedGain:F0} m/s)");
    }

    [Fact]
    public void ImpossibleRequest_FailsHonestly()
    {
        var (eph, sim) = MakeJupiterSystem();
        var (burn, passEpoch) = ApproachBurnState(eph, sim);

        // Demand a pass hundreds of Jupiter-radii out from a near-center approach: the sideways
        // redirect over ~40 d dwarfs the Δv cap, so the solver must refuse (not fake a solution).
        var req = new SlingPlanner.Request(burn, "jupiter", passEpoch, 800 * RJ, SlingPlanner.PassSide.Lead, MaxDeltaV: 500);
        var result = SlingPlanner.Solve(sim, eph, req);

        Assert.False(result.Ok, "an out-of-reach request must return Ok=false");
        Assert.False(string.IsNullOrEmpty(result.Failure), "a refusal must carry an honest reason");
        Assert.Equal(0, result.DeltaVMagnitude);
    }

    [Fact]
    public void Deterministic_TwoRunsIdentical()
    {
        var (eph1, sim1) = MakeJupiterSystem();
        var (eph2, sim2) = MakeJupiterSystem();
        var (burn1, pass1) = ApproachBurnState(eph1, sim1);
        var (burn2, pass2) = ApproachBurnState(eph2, sim2);

        var r1 = SlingPlanner.Solve(sim1, eph1, new SlingPlanner.Request(burn1, "jupiter", pass1, 12 * RJ, SlingPlanner.PassSide.Lead));
        var r2 = SlingPlanner.Solve(sim2, eph2, new SlingPlanner.Request(burn2, "jupiter", pass2, 12 * RJ, SlingPlanner.PassSide.Lead));

        Assert.True(r1.Ok && r2.Ok);
        Assert.Equal(r1.DeltaV.X, r2.DeltaV.X, precision: 6);
        Assert.Equal(r1.DeltaV.Y, r2.DeltaV.Y, precision: 6);
        Assert.Equal(r1.AchievedPassDistance, r2.AchievedPassDistance, precision: 3);
        Assert.Equal(r1.LeverGm, r2.LeverGm, precision: 6);
    }

    [Fact]
    public void SummarizeAtQuantizedDeltaV_MatchesTheFlownPass()
    {
        // The honesty rule behind "Add the burn": the desk quantizes Δv to whole pulses and
        // re-summarizes, so the numbers shown are the ones the plan will fly. Summarize at the
        // solved Δv must reproduce Solve's own pass distance.
        var (eph, sim) = MakeJupiterSystem();
        var (burn, passEpoch) = ApproachBurnState(eph, sim);
        var req = new SlingPlanner.Request(burn, "jupiter", passEpoch, 12 * RJ, SlingPlanner.PassSide.Lead);

        var solved = SlingPlanner.Solve(sim, eph, req);
        Assert.True(solved.Ok, $"solve must converge ({solved.Failure})");

        var summary = SlingPlanner.Summarize(sim, eph, req, solved.DeltaV);
        Assert.True(summary.Ok);
        Assert.Equal(solved.AchievedPassDistance, summary.AchievedPassDistance, precision: 3);
        Assert.Equal(solved.SpeedGain, summary.SpeedGain, precision: 3);
    }
}
