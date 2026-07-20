namespace SpaceSails.Core.Tests;

/// <summary>
/// #395 — LAB 36, THE CANNONBALL. Pins the kinetic-impactor Core math the lab certifies: asteroid mass from
/// Zubrin type + size, the ejecta enhancement β (DART/Dimorphos measured β≈3.6 on the S-type, C over-delivers,
/// M under), the momentum transfer Δv = β·m·u/M, the along-track 3·Δv·t leverage, and the headline the
/// "sacrifice a slug" gig would read — how much WARNING a hull or a pod buys to clear Ringside.
/// </summary>
public class KineticImpactorTests
{
    private static readonly double SafeMiss = DeflectionGig.SafeMissMeters;
    private const double U = KineticImpactor.ReferenceClosingSpeed;
    private const double Year = 365.25 * 86400.0;

    // ── Type constants: density and β, reconciled with DART ──

    [Fact]
    public void BulkDensity_LightCToDenseM()
    {
        Assert.True(KineticImpactor.BulkDensity(RockComposition.CType)
            < KineticImpactor.BulkDensity(RockComposition.SType));
        Assert.True(KineticImpactor.BulkDensity(RockComposition.SType)
            < KineticImpactor.BulkDensity(RockComposition.MType));
    }

    [Fact]
    public void Beta_SType_IsTheDartMeasuredValue_AndCOverdeliversMUnder()
    {
        // DART/Dimorphos measured β≈3.6 on a real S-type — the S value is anchored to it.
        Assert.Equal(3.6, KineticImpactor.Beta(RockComposition.SType), 1e-9);
        // Owner's DART reconciliation: a loose C-type throws the biggest plume (over-delivers); dense M under.
        Assert.True(KineticImpactor.Beta(RockComposition.CType) > KineticImpactor.Beta(RockComposition.SType));
        Assert.True(KineticImpactor.Beta(RockComposition.MType) < KineticImpactor.Beta(RockComposition.SType));
    }

    [Fact]
    public void AsteroidMass_IsDensityTimesSphereVolume()
    {
        var s = new RockType(RockComposition.SType);
        double r = 140.0;
        double expected = 2700.0 * (4.0 / 3.0) * System.Math.PI * r * r * r;
        Assert.Equal(expected, KineticImpactor.AsteroidMass(s, r), 1e-3 * expected);
        // r³ scaling: 10× radius → 1000× mass.
        Assert.Equal(1000.0, KineticImpactor.AsteroidMass(s, 10.0 * r) / KineticImpactor.AsteroidMass(s, r), 1e-6);
    }

    // ── The momentum transfer and the along-track leverage ──

    [Fact]
    public void DeltaV_IsBetaMomentumOverMass()
    {
        double m = KineticImpactor.OldHullMassKg, mass = 3.0e10, beta = 3.6;
        Assert.Equal(beta * m * U / mass, KineticImpactor.DeltaV(m, U, mass, beta), 1e-12);
        Assert.Equal(0.0, KineticImpactor.DeltaV(m, U, 0.0, beta)); // guarded against a zero-mass rock
    }

    [Fact]
    public void AlongTrackMiss_IsThreeTimesDvTimesLead()
    {
        Assert.Equal(3.0, KineticImpactor.AlongTrackLeverage, 1e-12);
        Assert.Equal(3.0 * 0.1 * Year, KineticImpactor.AlongTrackMiss(0.1, Year), 1e-3);
        Assert.Equal(0.0, KineticImpactor.AlongTrackMiss(0.1, -5.0)); // negative lead floors at 0
    }

    [Fact]
    public void RequiredDeltaV_InvertsAlongTrackMiss()
    {
        double dv = KineticImpactor.RequiredDeltaV(SafeMiss, Year);
        Assert.Equal(SafeMiss, KineticImpactor.AlongTrackMiss(dv, Year), 1.0);
        Assert.True(double.IsPositiveInfinity(KineticImpactor.RequiredDeltaV(SafeMiss, 0.0)));
    }

    // ── The headline: how much warning a slug buys, and the required mass ──

    [Fact]
    public void OldHull_Deflects140mSType_WithAboutTwoAndAQuarterYears()
    {
        var s = new RockType(RockComposition.SType);
        double lead = KineticImpactor.RequiredLeadSeconds(s, 140.0, KineticImpactor.OldHullMassKg, SafeMiss, U);
        Assert.Equal(2.28, lead / Year, 0.05); // README section C headline
    }

    [Fact]
    public void LighterSlug_NeedsMoreWarning_ThanHeavierSlug()
    {
        var s = new RockType(RockComposition.SType);
        double pod = KineticImpactor.RequiredLeadSeconds(s, 140.0, KineticImpactor.CargoPodMassKg, SafeMiss, U);
        double hull = KineticImpactor.RequiredLeadSeconds(s, 140.0, KineticImpactor.OldHullMassKg, SafeMiss, U);
        Assert.True(pod > hull, "a lighter pod must be thrown earlier than a heavier hull");
        // The 10× heavier hull needs 10× less warning (lead ∝ 1/mass, since miss ∝ Δv ∝ mass).
        Assert.Equal(10.0, pod / hull, 0.01);
    }

    [Fact]
    public void RequiredImpactorMass_InvertsRequiredLead()
    {
        var s = new RockType(RockComposition.SType);
        double m = KineticImpactor.RequiredImpactorMass(s, 140.0, SafeMiss, Year, U);
        // That mass, thrown one year out, opens exactly SafeMiss.
        double dv = KineticImpactor.DeltaV(m, U, KineticImpactor.AsteroidMass(s, 140.0), KineticImpactor.Beta(RockComposition.SType));
        Assert.Equal(SafeMiss, KineticImpactor.AlongTrackMiss(dv, Year), 1.0);
    }

    [Fact]
    public void MetalIsTheHardestKineticTarget_NeedingMostWarning()
    {
        // Same size, same slug: M-type needs more warning than S than C (denser AND less ejecta).
        double c = KineticImpactor.RequiredLeadSeconds(new(RockComposition.CType), 140.0, KineticImpactor.OldHullMassKg, SafeMiss, U);
        double s = KineticImpactor.RequiredLeadSeconds(new(RockComposition.SType), 140.0, KineticImpactor.OldHullMassKg, SafeMiss, U);
        double m = KineticImpactor.RequiredLeadSeconds(new(RockComposition.MType), 140.0, KineticImpactor.OldHullMassKg, SafeMiss, U);
        Assert.True(c < s && s < m, $"C {c} < S {s} < M {m}");
    }
}
