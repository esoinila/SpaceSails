namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for <see cref="TransferMath"/> — the pure two-body kernels behind #146 / Lab 23.
/// These assert the MATH (no integrator): Lambert's arc is self-consistent, matches the Hohmann
/// closed form in the limit, and refuses degenerate/unreachable geometry with null rather than a
/// NaN or a silent guess. Tolerances are the labs' way: physical invariants at tight relative
/// tolerance, never pinned absolute coordinates.
/// </summary>
public class TransferMathTests
{
    private const double Mu = 3.7931187e16; // Saturn — the moon-run well

    private static Vector2d OnCircle(double radius, double angle) =>
        new(radius * Math.Cos(angle), radius * Math.Sin(angle));

    // Circular-orbit velocity (tangential, CCW) at a point on a circle of the given radius.
    private static Vector2d CircularVel(double radius, double angle, double mu)
    {
        double speed = Math.Sqrt(mu / radius);
        return new Vector2d(-Math.Sin(angle), Math.Cos(angle)) * speed;
    }

    [Theory]
    [InlineData(30)]
    [InlineData(170)]
    [InlineData(190)]
    [InlineData(350)]
    public void Lambert_OnACircle_RecoversCircularVelocityBothEnds(double degrees)
    {
        // A point coasting on a circular orbit and a second point Δθ ahead, given exactly the coast
        // time n·Δθ⁻¹, is a Lambert arc whose endpoint velocities ARE the circular velocity —
        // tangential, at the circular speed. The universal-variable solver must reproduce that to
        // machine-ish precision for angles on both sides of 180°.
        const double radius = 1.22183e9;
        double dTheta = degrees * Math.PI / 180;
        double meanMotion = Math.Sqrt(Mu / (radius * radius * radius));
        double tof = dTheta / meanMotion;

        Vector2d r1 = OnCircle(radius, 0);
        Vector2d r2 = OnCircle(radius, dTheta);
        var sol = TransferMath.Lambert(r1, r2, tof, Mu);

        Assert.NotNull(sol);
        Vector2d expectV1 = CircularVel(radius, 0, Mu);
        Vector2d expectV2 = CircularVel(radius, dTheta, Mu);
        double circularSpeed = Math.Sqrt(Mu / radius);

        Assert.Equal(expectV1.X, sol.Value.V1.X, circularSpeed * 1e-6);
        Assert.Equal(expectV1.Y, sol.Value.V1.Y, circularSpeed * 1e-6);
        Assert.Equal(expectV2.X, sol.Value.V2.X, circularSpeed * 1e-6);
        Assert.Equal(expectV2.Y, sol.Value.V2.Y, circularSpeed * 1e-6);
    }

    [Theory]
    [InlineData(60, 1.0)]
    [InlineData(120, 1.4)]
    [InlineData(150, 0.9)]
    [InlineData(200, 1.1)]
    [InlineData(90, 0.25)] // short time of flight → hyperbolic arc
    [InlineData(75, 0.18)] // deeper into the hyperbolic domain
    public void Lambert_ConservesEnergyAndAngularMomentum(double degrees, double tofFraction)
    {
        // Whatever arc Lambert returns, it is a two-body conic: specific orbital energy and specific
        // angular momentum are identical at both ends. Checked across elliptic AND hyperbolic cells
        // (short TOF) — the hyperbolic ones exercise the solver's expanding lower bracket.
        double r1len = 2.38037e8, r2len = 1.22183e9;
        double dTheta = degrees * Math.PI / 180;
        double circTof = TransferMath.Hohmann(r1len, r2len, Mu).TransferSeconds;
        double tof = tofFraction * circTof;

        Vector2d r1 = OnCircle(r1len, 0);
        Vector2d r2 = OnCircle(r2len, dTheta);
        var sol = TransferMath.Lambert(r1, r2, tof, Mu);
        Assert.NotNull(sol);

        double energy1 = sol.Value.V1.LengthSquared / 2 - Mu / r1len;
        double energy2 = sol.Value.V2.LengthSquared / 2 - Mu / r2len;
        double h1 = r1.X * sol.Value.V1.Y - r1.Y * sol.Value.V1.X;
        double h2 = r2.X * sol.Value.V2.Y - r2.Y * sol.Value.V2.X;

        Assert.Equal(energy1, energy2, Math.Abs(energy1) * 1e-9);
        Assert.Equal(h1, h2, Math.Abs(h1) * 1e-9);
    }

    [Fact]
    public void Lambert_At179Degrees_MatchesHohmannClosedForm()
    {
        // Just shy of the 180° blind spot the Lambert arc IS (nearly) the Hohmann transfer — the
        // departure and arrival Δv must land within 2% of the closed form. This is the honesty
        // handshake between the engine and the teacher.
        double r1len = 2.38037e8, r2len = 1.22183e9;
        var hohmann = TransferMath.Hohmann(r1len, r2len, Mu);

        double dTheta = 179 * Math.PI / 180;
        Vector2d r1 = OnCircle(r1len, 0);
        Vector2d r2 = OnCircle(r2len, dTheta);
        var sol = TransferMath.Lambert(r1, r2, hohmann.TransferSeconds, Mu);
        Assert.NotNull(sol);

        double departDeltaV = (sol.Value.V1 - CircularVel(r1len, 0, Mu)).Length;
        double arriveDeltaV = (sol.Value.V2 - CircularVel(r2len, dTheta, Mu)).Length;

        Assert.Equal(hohmann.DepartDeltaV, departDeltaV, hohmann.DepartDeltaV * 0.02);
        Assert.Equal(hohmann.ArriveDeltaV, arriveDeltaV, hohmann.ArriveDeltaV * 0.02);
    }

    [Fact]
    public void Lambert_DegenerateGeometry_ReturnsNullNeverNaN()
    {
        double radius = 1.22183e9;
        Vector2d r1 = OnCircle(radius, 0);
        double tof = 0.5 * TransferMath.Hohmann(radius, radius * 1.5, Mu).TransferSeconds;

        // 0° transfer (arrival ray coincides with departure): singular, null.
        Assert.Null(TransferMath.Lambert(r1, OnCircle(radius, 0), tof, Mu));
        // 180° transfer (Lambert's classic blind spot, sin Δθ = 0): null.
        Assert.Null(TransferMath.Lambert(r1, OnCircle(radius, Math.PI), tof, Mu));
        // Absurdly short time of flight (below the deepest hyperbolic bracket): unreachable, null.
        Assert.Null(TransferMath.Lambert(r1, OnCircle(radius * 1.5, 1.2), 1e-3, Mu));
        // Bad inputs never throw or return NaN.
        Assert.Null(TransferMath.Lambert(r1, OnCircle(radius, 1.2), -100, Mu));
        Assert.Null(TransferMath.Lambert(r1, OnCircle(radius, 1.2), tof, -1));

        // Whatever a spread of times of flight returns, it is null OR finite — never NaN/Infinity.
        for (double frac = 0.05; frac < 3.0; frac += 0.19)
        {
            var sol = TransferMath.Lambert(r1, OnCircle(radius * 1.5, 1.2), frac * tof, Mu);
            if (sol is { } s)
            {
                Assert.True(double.IsFinite(s.V1.X) && double.IsFinite(s.V1.Y)
                    && double.IsFinite(s.V2.X) && double.IsFinite(s.V2.Y), $"finite velocities at frac {frac}");
            }
        }
    }

    [Fact]
    public void Hohmann_EnceladusToTitan_MatchesSpecNumbers()
    {
        // The spec's priced geometry (§0): Δv₁ ≈ 3.71 km/s, Δv₂ ≈ 2.39 km/s, TOF ≈ 3.68 d.
        var plan = TransferMath.Hohmann(2.38037e8, 1.22183e9, Mu);
        Assert.Equal(3.71, plan.DepartDeltaV / 1000, 0.05);
        Assert.Equal(2.39, plan.ArriveDeltaV / 1000, 0.05);
        Assert.Equal(3.68, plan.TransferSeconds / 86400, 0.05);
    }

    [Fact]
    public void SynodicPeriod_EnceladusTitan_IsAboutThirtySixHours()
    {
        double synodic = TransferMath.SynodicPeriod(1.183868e5, 1.377648e6);
        Assert.Equal(36.0, synodic / 3600, 1.0);
    }

    [Fact]
    public void SynodicPeriod_EqualPeriods_IsInfinite()
    {
        Assert.True(double.IsPositiveInfinity(TransferMath.SynodicPeriod(1e5, 1e5)));
    }

    [Fact]
    public void Lambert_ProgradeAndRetrograde_AreDistinctArcs()
    {
        // The same two points are reachable both ways round; the retrograde branch is a different,
        // usually pricier arc. Selecting it explicitly must change the answer, not infer from the
        // cross product alone.
        double r1len = 6.709e8, r2len = 1.0704e9;
        Vector2d r1 = OnCircle(r1len, 0);
        Vector2d r2 = OnCircle(r2len, 100 * Math.PI / 180);
        double tof = TransferMath.Hohmann(r1len, r2len, Mu).TransferSeconds;

        var pro = TransferMath.Lambert(r1, r2, tof, Mu, prograde: true);
        var retro = TransferMath.Lambert(r1, r2, tof, Mu, prograde: false);

        Assert.NotNull(pro);
        Assert.NotNull(retro);
        Assert.True((pro.Value.V1 - retro.Value.V1).Length > 1.0, "prograde and retrograde arcs must differ");
    }
}
