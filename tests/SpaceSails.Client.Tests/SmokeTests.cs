using SpaceSails.Client.Rendering;

namespace SpaceSails.Client.Tests;

/// <summary>Can a test project even load the client assembly? Everything else here depends on it.</summary>
// The client assembly targets the browser; a test touching its API has to say so or CA1416 refuses the
// call site. Analyzer-only — xunit still runs it on the test host.
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SmokeTests
{
    [Fact]
    public void HerDeckIsReachableFromATest() => Assert.NotEmpty(DeckPlan.Ship.Consoles);
}
