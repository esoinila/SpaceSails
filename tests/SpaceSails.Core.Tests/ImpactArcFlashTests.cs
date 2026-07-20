namespace SpaceSails.Core.Tests;

/// <summary>
/// #395 — LAB 35, THE PUSH THAT ISN'T A PUSH (the EU arc-flash extension). Pins the game-canon
/// Electric-Universe impact-flash model: the owner's arc-melter reference power (500 A × 22 kV ≈ 11 MW),
/// the conductivity ordering (M-type metallic conducts, C-type carbonaceous poorest), and the flash
/// multiplier that makes the conductive M-type flash brightest over a pure-kinetic (vaporisation) baseline.
/// FLAGGED non-mainstream — this is the game's licence to be electric, not textbook physics.
/// </summary>
public class ImpactArcFlashTests
{
    [Fact]
    public void ReferenceArcPower_IsVoltsTimesAmps_AboutElevenMegawatts()
    {
        Assert.Equal(500.0 * 22_000.0, ImpactArcFlash.ReferenceArcPowerWatts, 1e-6);
        Assert.Equal(11.0, ImpactArcFlash.ReferenceArcPowerWatts / 1e6, 0.01);
    }

    [Fact]
    public void Conductivity_MetalHighest_CarbonaceousLowest()
    {
        Assert.True(ImpactArcFlash.ConductivityClass(RockComposition.MType)
            > ImpactArcFlash.ConductivityClass(RockComposition.SType));
        Assert.True(ImpactArcFlash.ConductivityClass(RockComposition.SType)
            > ImpactArcFlash.ConductivityClass(RockComposition.CType));
    }

    [Fact]
    public void ArcFlashMultiplier_StartsAtTheKineticBaseline_AndPeaksForMetal()
    {
        // Every rock flashes at least the pure-kinetic 1.0; the arc term only adds.
        Assert.True(ImpactArcFlash.ArcFlashMultiplier(RockComposition.CType) >= 1.0);
        Assert.True(ImpactArcFlash.ArcFlashMultiplier(RockComposition.MType)
            > ImpactArcFlash.ArcFlashMultiplier(RockComposition.SType));
        Assert.True(ImpactArcFlash.ArcFlashMultiplier(RockComposition.SType)
            > ImpactArcFlash.ArcFlashMultiplier(RockComposition.CType));
        // The fully-conductive M-type sits at 1 + ArcContribution·1.0 (README section F: 2.50).
        Assert.Equal(1.0 + ImpactArcFlash.ArcContribution, ImpactArcFlash.ArcFlashMultiplier(RockComposition.MType), 1e-9);
        Assert.Equal(2.50, ImpactArcFlash.ArcFlashMultiplier(RockComposition.MType), 1e-9);
    }
}
