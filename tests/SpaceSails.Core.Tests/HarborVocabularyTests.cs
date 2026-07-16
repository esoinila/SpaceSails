namespace SpaceSails.Core.Tests;

/// <summary>#203 "one voice": the same armed arrival must read the same everywhere, and it must
/// branch on harbor class so a μ≤0 station never gets moon vocabulary ("orbit-insert at Cinder
/// Roost (0 km)") and a real orbit never loses it.</summary>
public class HarborVocabularyTests
{
    [Fact]
    public void ArrivalStep_Orbit_ReadsOrbitInsertWithAltitude()
    {
        Assert.Equal(
            "orbit-insert at Enceladus (alt 313 km)",
            HarborVocabulary.ArrivalStep(HarborClass.Orbit, "Enceladus", "alt 313 km"));
    }

    [Fact]
    public void ArrivalStep_Orbit_WithoutAltitude_FallsBackToBareBody()
    {
        Assert.Equal("orbit-insert at Titan", HarborVocabulary.ArrivalStep(HarborClass.Orbit, "Titan"));
        Assert.Equal("orbit-insert at Titan", HarborVocabulary.ArrivalStep(HarborClass.Orbit, "Titan", "  "));
    }

    [Fact]
    public void ArrivalStep_Dock_ReadsDockEnvelope_NeverOrbitOrZeroKm()
    {
        string step = HarborVocabulary.ArrivalStep(HarborClass.Dock, "Cinder Roost", "alt 0 km");
        Assert.Equal("dock envelope at Cinder Roost — slow to ≤8 km/s", step);
        // The station's ruling: no "orbit", no "insert", no phantom "(0 km)".
        Assert.DoesNotContain("orbit", step);
        Assert.DoesNotContain("insert", step);
        Assert.DoesNotContain("0 km", step);
    }

    [Fact]
    public void ArrivalStep_Dock_MatchCeiling_ComesFromDockRule()
    {
        // The ≤N km/s ceiling is DockRule.MatchSpeed, not a magic number in the string.
        Assert.Contains($"≤{DockRule.MatchSpeed / 1000:N0} km/s",
            HarborVocabulary.ArrivalStep(HarborClass.Dock, "Rusty's"));
    }

    [Fact]
    public void ArmAction_SaysWhatTheShipDoes_PerHarborClass()
    {
        Assert.Equal("✈ Autopilot: dock at Rusty's", HarborVocabulary.ArmAction(HarborClass.Dock, "Rusty's"));
        Assert.Equal("✈ Autopilot: orbit Titan", HarborVocabulary.ArmAction(HarborClass.Orbit, "Titan"));
    }

    [Fact]
    public void ArmedStepVerb_DockVsOrbit()
    {
        Assert.Equal("Dock at", HarborVocabulary.ArmedStepVerb(HarborClass.Dock));
        Assert.Equal("Insert at", HarborVocabulary.ArmedStepVerb(HarborClass.Orbit));
    }
}
