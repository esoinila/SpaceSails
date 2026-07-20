namespace SpaceSails.Core.Tests;

/// <summary>
/// #395 — LAB 39, THE LONG KNIFE. Pins the standoff-laser-ablation Core math the lab certifies: the
/// diffraction spot (r ≈ d·λ/D) and the inverse-square intensity, the min-power-to-ablate that rises as d² and
/// the reach that follows, the momentum-coupling thrust F = C_m·P (independent of the rock), the feeble a = F/M,
/// and the headline continuous-tow clear time (miss = 1.5·a·T²) that makes the technique POWER-gated —
/// MW-class buys years, 100 MW buys months.
/// </summary>
public class LaserAblationTests
{
    private static readonly double SafeMiss = DeflectionGig.SafeMissMeters;
    private const double Pref = LaserAblation.ReferencePlatformPowerWatts;
    private const double Year = 365.25 * 86400.0;

    // ── The standoff budget: diffraction and inverse-square ──

    [Fact]
    public void SpotRadius_IsDLambdaOverAperture()
    {
        double d = 1.0e6;
        Assert.Equal(d * LaserAblation.WavelengthMeters / LaserAblation.ApertureMeters, LaserAblation.SpotRadius(d), 1e-15);
        Assert.Equal(0.0, LaserAblation.SpotRadius(0.0));
    }

    [Fact]
    public void SpotIntensity_FallsAsInverseSquareOfStandoff()
    {
        // Doubling the standoff quarters the intensity at fixed power (spot area ∝ d²).
        double i1 = LaserAblation.SpotIntensity(Pref, 1.0e5);
        double i2 = LaserAblation.SpotIntensity(Pref, 2.0e5);
        Assert.Equal(4.0, i1 / i2, 1e-9);
    }

    [Fact]
    public void MinPowerToAblate_RisesAsDistanceSquared()
    {
        double p1 = LaserAblation.MinPowerToAblate(1.0e5);
        double p2 = LaserAblation.MinPowerToAblate(2.0e5);
        Assert.Equal(4.0, p2 / p1, 1e-9);
        // At threshold the delivered intensity equals the ablation flux (definitional).
        Assert.Equal(LaserAblation.AblationThresholdWattsPerM2,
            LaserAblation.SpotIntensity(p1, 1.0e5), 1e-3 * LaserAblation.AblationThresholdWattsPerM2);
    }

    [Fact]
    public void MaxStandoff_InvertsMinPower_AndScalesWithSqrtPower()
    {
        double d = LaserAblation.MaxStandoff(Pref);
        // At that reach the min power to ablate equals the platform power (round-trip).
        Assert.Equal(Pref, LaserAblation.MinPowerToAblate(d), 1e-3 * Pref);
        // 100× the power reaches 10× as far (d ∝ √P).
        Assert.Equal(10.0, LaserAblation.MaxStandoff(100.0 * Pref) / d, 1e-6);
        // The 1 MW reference reaches ~1,683 km (README section A).
        Assert.Equal(1683.0, d / 1000.0, 5.0);
    }

    // ── The thrust: momentum coupling, independent of the rock ──

    [Fact]
    public void Thrust_IsCouplingTimesPower_AndSameForEveryRock()
    {
        Assert.Equal(LaserAblation.MomentumCouplingNewtonsPerWatt * Pref, LaserAblation.Thrust(Pref), 1e-12);
        // 50 N at 1 MW (README section B).
        Assert.Equal(50.0, LaserAblation.Thrust(Pref), 1e-9);
        Assert.Equal(0.0, LaserAblation.Thrust(-1.0)); // negative power floors at 0
    }

    [Fact]
    public void Acceleration_IsThrustOverMass_AndDenserRockAcceleratesLess()
    {
        var s = new RockType(RockComposition.SType);
        double m = KineticImpactor.AsteroidMass(s, 140.0);
        Assert.Equal(LaserAblation.Thrust(Pref) / m, LaserAblation.Acceleration(Pref, m), 1e-24);
        // Same size, same laser: a dense M-type accelerates slower than a light C-type.
        double aC = LaserAblation.Acceleration(Pref, KineticImpactor.AsteroidMass(new(RockComposition.CType), 140.0));
        double aM = LaserAblation.Acceleration(Pref, KineticImpactor.AsteroidMass(new(RockComposition.MType), 140.0));
        Assert.True(aC > aM);
        Assert.Equal(0.0, LaserAblation.Acceleration(Pref, 0.0)); // guarded
    }

    // ── The headline: power-gated clear time (continuous-tow leverage) ──

    [Fact]
    public void CumulativeDeltaV_IsAccelTimesTime()
    {
        var s = new RockType(RockComposition.SType);
        double m = KineticImpactor.AsteroidMass(s, 140.0);
        Assert.Equal(LaserAblation.Acceleration(Pref, m) * Year, LaserAblation.CumulativeDeltaV(Pref, m, Year), 1e-12);
        Assert.Equal(0.0, LaserAblation.CumulativeDeltaV(Pref, m, -5.0)); // negative burn floors at 0
    }

    [Fact]
    public void OneMegawatt_Clears140mRock_InAboutThreeAndAHalfYears()
    {
        double t = LaserAblation.RequiredBurnSeconds(new(RockComposition.SType), 140.0, Pref, SafeMiss);
        Assert.Equal(3.53, t / Year, 0.05); // README section C headline
    }

    [Fact]
    public void ClearTime_FallsAsOneOverSqrtPower()
    {
        var s = new RockType(RockComposition.SType);
        double t1 = LaserAblation.RequiredBurnSeconds(s, 140.0, Pref, SafeMiss);
        double t100 = LaserAblation.RequiredBurnSeconds(s, 140.0, 100.0 * Pref, SafeMiss);
        // 100× the power clears 10× faster (miss = 1.5·a·T², a ∝ P → T ∝ 1/√P).
        Assert.Equal(10.0, t1 / t100, 0.01);
        // The 100 MW upgrade brings the 140 m rock under half a year (README section C: ~0.35 yr).
        Assert.True(t100 / Year < 0.5, "100 MW clears a 140 m rock in months, not years");
    }

    [Fact]
    public void OneMegawattYearBurn_FallsShort_HundredMegawattClears()
    {
        // A 1-year continuous burn: 1 MW doesn't reach SafeMiss, 100 MW does (README section D).
        var s = new RockType(RockComposition.SType);
        double m = KineticImpactor.AsteroidMass(s, 140.0);
        double miss1 = GravityTractor.Miss(LaserAblation.Acceleration(Pref, m), Year);
        double miss100 = GravityTractor.Miss(LaserAblation.Acceleration(100.0 * Pref, m), Year);
        Assert.True(miss1 < SafeMiss, "1 MW over a year only grazes");
        Assert.True(miss100 >= SafeMiss, "100 MW over a year clears with margin");
    }
}
