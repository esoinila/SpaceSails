namespace SpaceSails.Core.Tests;

/// <summary>
/// #395 — LAB 38, THE ROCK THAT MINES ITSELF. Pins the mass-driver-on-the-rock Core math the lab certifies:
/// the rocket equation with the rock as its own tankage (Δv = v_ex·ln(M0/M_final)), the tiny mass FRACTION a
/// deflection needs (f = 1 − e^(−Δv/v_ex)) against the enormous absolute tonnage, the along-track Δv that ties
/// it to the cannonball, the run-clock that says a 1 km rock on a year's warning can't finish (the honest
/// negative), and the reactor power P = ṁ·½·v_ex².
/// </summary>
public class RockMassDriverTests
{
    private static readonly double SafeMiss = DeflectionGig.SafeMissMeters;
    private const double Vex = RockMassDriver.ExhaustVelocityMetersPerSecond;
    private const double Thru = RockMassDriver.ReferenceThroughputKgPerSecond;
    private const double Year = 365.25 * 86400.0;

    // ── The rocket equation, with the rock as tankage ──

    [Fact]
    public void DeltaV_IsVexTimesLnMassRatio()
    {
        // Throw off half the rock: Δv = v_ex·ln(2).
        Assert.Equal(Vex * System.Math.Log(2.0), RockMassDriver.DeltaV(Vex, 2.0, 1.0), 1e-9);
        Assert.Equal(0.0, RockMassDriver.DeltaV(Vex, 1.0, 0.0));   // guarded (zero final mass)
        Assert.Equal(0.0, RockMassDriver.DeltaV(Vex, 0.0, 0.0));   // guarded (zero initial mass)
    }

    [Fact]
    public void MassFractionFlung_IsOneMinusExp_AndInvertsDeltaV()
    {
        double dv = 0.3169; // the ~1-year-lead requirement
        double f = RockMassDriver.MassFractionFlung(dv, Vex);
        Assert.Equal(1.0 - System.Math.Exp(-dv / Vex), f, 1e-12);
        // Flinging that fraction gives back exactly dv (rocket equation round-trip).
        Assert.Equal(dv, RockMassDriver.DeltaV(Vex, 1.0, 1.0 - f), 1e-9);
        Assert.Equal(0.0, RockMassDriver.MassFractionFlung(-5.0, Vex)); // negative Δv floors at 0
        Assert.Equal(0.0, RockMassDriver.MassFractionFlung(dv, 0.0));   // guarded exhaust velocity
    }

    [Fact]
    public void SmallDeltaV_FractionIsNearlyLinear_InDvOverVex()
    {
        // For the tiny Δv a deflection needs, f ≈ Δv/v_ex to better than 0.1%.
        double dv = 0.3169;
        double f = RockMassDriver.MassFractionFlung(dv, Vex);
        Assert.Equal(dv / Vex, f, 1e-3 * (dv / Vex));
    }

    // ── The self-mine: tiny fraction, enormous mass, fixed by the warning not the rock ──

    [Fact]
    public void MassFractionToDeflect_IsIndependentOfRockTypeAndSize()
    {
        // The required Δv is set by the warning + miss, so the FRACTION flung is the same for every rock.
        double fracC = RockMassDriver.MassToDeflect(new(RockComposition.CType), 50.0, SafeMiss, Year, Vex)
            / KineticImpactor.AsteroidMass(new(RockComposition.CType), 50.0);
        double fracM = RockMassDriver.MassToDeflect(new(RockComposition.MType), 1000.0, SafeMiss, Year, Vex)
            / KineticImpactor.AsteroidMass(new(RockComposition.MType), 1000.0);
        Assert.Equal(fracC, fracM, 1e-12);
        // And it's a rounding error — about 0.0127 % at a year's warning (README section B).
        Assert.Equal(0.0127, fracC * 100.0, 0.0005);
    }

    [Fact]
    public void MassToFling_ScalesWithRockMass_ThoughFractionIsFixed()
    {
        // A 10× radius is 1000× the mass, so 1000× the tonnage flung (same fraction).
        var s = new RockType(RockComposition.SType);
        double small = RockMassDriver.MassToDeflect(s, 100.0, SafeMiss, Year, Vex);
        double big = RockMassDriver.MassToDeflect(s, 1000.0, SafeMiss, Year, Vex);
        Assert.Equal(1000.0, big / small, 1e-6);
    }

    [Fact]
    public void Reference140mSType_FlingsAboutFourThousandTonnes_AtOneYear()
    {
        double t = RockMassDriver.MassToDeflect(new(RockComposition.SType), 140.0, SafeMiss, Year, Vex);
        Assert.Equal(3.93e6, t, 0.05e6); // README section B: ~3,900 t
    }

    // ── The run clock: the honest negative ──

    [Fact]
    public void RunSeconds_IsMassOverThroughput_AndGuarded()
    {
        Assert.Equal(100.0, RockMassDriver.RunSeconds(2000.0, 20.0), 1e-9);
        Assert.True(double.IsPositiveInfinity(RockMassDriver.RunSeconds(2000.0, 0.0)));
    }

    [Fact]
    public void BigRock_ShortWarning_CannotFinishInTime()
    {
        // 1 km S-type with only a year's warning needs > a year of throwing — it can't finish (README C).
        double run = RockMassDriver.RunSecondsToDeflect(new(RockComposition.SType), 1000.0, SafeMiss, Year, Thru, Vex);
        Assert.True(run > Year, $"run {run / Year} yr should exceed the 1 yr lead");
        // Five years of warning cuts the required Δv (and mass, and run) fivefold — now it fits.
        double run5 = RockMassDriver.RunSecondsToDeflect(new(RockComposition.SType), 1000.0, SafeMiss, 5 * Year, Thru, Vex);
        Assert.True(run5 < 5 * Year, "with 5 yr warning the same rock finishes comfortably");
        Assert.Equal(5.0, run / run5, 0.01); // run ∝ required Δv ∝ 1/lead
    }

    [Fact]
    public void SmallRock_IsDaysOfThrowing()
    {
        double run = RockMassDriver.RunSecondsToDeflect(new(RockComposition.SType), 140.0, SafeMiss, Year, Thru, Vex);
        Assert.Equal(2.28, run / 86400.0, 0.05); // README section C: ~2.28 days
    }

    // ── The reactor bill ──

    [Fact]
    public void DriverPower_IsHalfMdotVexSquared()
    {
        Assert.Equal(0.5 * Thru * Vex * Vex, RockMassDriver.DriverPowerWatts(Thru, Vex), 1e-3);
        // The 20 kg/s reference rig draws 62.5 MW (README section D).
        Assert.Equal(62.5e6, RockMassDriver.DriverPowerWatts(Thru, Vex), 0.01e6);
        Assert.Equal(0.0, RockMassDriver.DriverPowerWatts(-5.0, Vex)); // negative throughput floors at 0
    }

    [Fact]
    public void FasterMuzzle_FlingsLessMass_ButCostsMorePower()
    {
        var s = new RockType(RockComposition.SType);
        double slow = RockMassDriver.MassToDeflect(s, 140.0, SafeMiss, Year, 2500.0);
        double fast = RockMassDriver.MassToDeflect(s, 140.0, SafeMiss, Year, 3200.0);
        Assert.True(fast < slow, "a faster muzzle flings less mass (f ≈ Δv/v_ex)");
        // But power grows as v_ex², so speed is not free.
        Assert.True(RockMassDriver.DriverPowerWatts(Thru, 3200.0) > RockMassDriver.DriverPowerWatts(Thru, 2500.0));
    }
}
