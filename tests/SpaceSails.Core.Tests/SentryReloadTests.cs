using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #531 · CARRY IT HOME AND IT FILLS. Owner: <i>"Carrying the autogun to our shuttle air-lock should reload it
/// ( might ve needed for big ship) 😎"</i> — the boat carries the belts, the bot carries only what it was last
/// given, and the price of a refill is the walk.
/// </summary>
public sealed class SentryReloadTests
{
    [Fact]
    public void ADrainedBotNeedsFillingAndAFullOneDoesNot()
    {
        Assert.True(SentryBot.NeedsFilling(0));
        Assert.True(SentryBot.NeedsFilling(SentryBot.MaxMagazine - 1));
        Assert.False(SentryBot.NeedsFilling(SentryBot.MaxMagazine));
    }

    /// <summary>The line carries BOTH numbers, because the whole point of walking back is seeing what the walk
    /// bought — and the readout is the same two-digit counter the bot wears in the field.</summary>
    [Fact]
    public void TheLockSaysWhatTheWalkBought()
    {
        string line = SentryBot.FilledLine("GATE-1", 7);

        Assert.Contains("GATE-1", line, StringComparison.Ordinal);
        Assert.Contains(SentryBot.Readout(7), line, StringComparison.Ordinal);
        Assert.Contains(SentryBot.Readout(SentryBot.MaxMagazine), line, StringComparison.Ordinal);
    }

    [Fact]
    public void AFullBotIsToldSoRatherThanSilentlyDoingNothing() =>
        Assert.Contains("already full", SentryBot.AlreadyFullLine("R-3B"), StringComparison.OrdinalIgnoreCase);
}
