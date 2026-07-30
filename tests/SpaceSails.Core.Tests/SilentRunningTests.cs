using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #538 · RIG FOR SILENT RUNNING, AS LAWS. Owner: <i>"Shuttle should also power down to make it less of an
/// anomaly"</i>, <i>"Captain's remote"</i>, and — the part that makes it a decision — <i>"Warm up time is a cost
/// there 😎"</i>.
/// </summary>
public sealed class SilentRunningTests
{
    /// <summary>
    /// THE TRADE, AS ONE PREDICATE. A cold boat is not a ride, and a boat mid-warm-up is not a ride either — which
    /// is the whole cost of having hidden well. If this ever returned true while she was waking, going dark would
    /// be free and the scene it exists for would have no teeth.
    /// </summary>
    [Theory]
    [InlineData(false, 0.0, true)]     // warm and answering
    [InlineData(true, 0.0, false)]     // asleep
    [InlineData(false, 5.0, false)]    // waking, not there yet
    [InlineData(true, 5.0, false)]     // asleep AND waking is nonsense; still not a ride
    public void OnlyAWarmBoatFlies(bool poweredDown, double spinUpLeft, bool expected) =>
        Assert.Equal(expected, SilentRunning.ReadyToFly(poweredDown, spinUpLeft));

    /// <summary>The warm-up has to be long enough to be a commitment and short enough not to be a death sentence
    /// — roughly the walk from amidships to the lock, so a captain who has to run arrives at a boat that is still
    /// waking up.</summary>
    [Fact]
    public void TheWarmUpIsACommitmentAndNotASentence()
    {
        Assert.True(SilentRunning.SpinUpSeconds >= 10);
        Assert.True(SilentRunning.SpinUpSeconds <= 45);
    }

    /// <summary>The panel names both costs out loud, because a captain should be able to read the trade before
    /// committing to it rather than discovering it at the lock.</summary>
    [Fact]
    public void ThePanelAdmitsBothCostsBeforeYouCommit()
    {
        Assert.Contains("will not defend you", SilentRunning.WhatItCostsLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("will not fly you", SilentRunning.WhatItCostsLine, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>It is a CAPTAIN'S REMOTE, not a settings menu — the owner's own name for it, and the name is the
    /// design: a handheld thing thumbed behind a bulkhead.</summary>
    [Fact]
    public void ItIsAHandheldNotAMenu() =>
        Assert.Contains("CAPTAIN'S REMOTE", SilentRunning.PanelTitle, StringComparison.Ordinal);

    /// <summary>And the refusal quotes the time left, so the cost is a number rather than a mood.</summary>
    [Fact]
    public void TheRefusalQuotesWhatIsLeft() =>
        Assert.Contains(HullVenting.SoakLabel(12), SilentRunning.NotARideYetLine(12), StringComparison.Ordinal);
}
