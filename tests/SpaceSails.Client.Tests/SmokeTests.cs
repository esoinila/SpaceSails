using SpaceSails.Client.Rendering;

namespace SpaceSails.Client.Tests;

/// <summary>Can a test project even load the client assembly? Everything else here depends on it.</summary>
public sealed class SmokeTests
{
    [Fact]
    public void HerDeckIsReachableFromATest() => Assert.NotEmpty(DeckPlan.Ship.Consoles);
}
