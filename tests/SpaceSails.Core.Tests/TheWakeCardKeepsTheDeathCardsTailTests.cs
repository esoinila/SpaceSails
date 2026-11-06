using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>R1-F68 (#1413) · the wake card's WHAT HAPPENED ends the way the death card does. A scuttled
/// captain's derelict death says "Her log will not mention it." on both. Proven RED by pointing WakeTail
/// back at "" (the card dropped the tail).</summary>
public sealed class TheWakeCardKeepsTheDeathCardsTailTests
{
    [Fact]
    public void ADerelictDeathKeepsItsTailOnTheWakeCard()
    {
        foreach (DeathCause cause in System.Enum.GetValues<DeathCause>())
        {
            if (cause == DeathCause.Collector)
            {
                continue;
            }
            Assert.Equal(DeathNarration.Tail(cause, DeathPlace.Derelict), DeathNarration.WakeTail(cause, DeathPlace.Derelict));
        }
        Assert.Contains("Her log will not mention it.", DeathNarration.WakeTail(DeathCause.Joined, DeathPlace.Derelict));
    }

    [Fact]
    public void TheGunCameraFreezeFrameStaysOffTheWakeCard()
    {
        Assert.Equal("", DeathNarration.WakeTail(DeathCause.Collector, DeathPlace.Derelict));
        Assert.Equal("", DeathNarration.WakeTail(DeathCause.Collector, DeathPlace.LandingParty));
    }
}
