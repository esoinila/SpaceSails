using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1439 part 1 Â· A line written BETWEEN frames is stamped with the previous frame's clock, so in a throttled or
/// hidden tab (ticks seconds apart) the next tick expired a long line before any frame had painted it. The dwell
/// now starts from the first tick that meets the line. A line written before the clock started (a boot line) is
/// NOT re-based: it keeps the stamp it always had, which is what keeps the boot photographs' lines from lingering
/// (PR #1435, TheCountersPinnedFootPaintsItsOwnBoxTests).
/// RED PROOF: make <c>Seen</c> return <c>this</c> and the first test fails (the line is gone at the first tick).
/// </summary>
public sealed class APulseLivesFromTheFirstTickThatSeesItTests
{
    private const string Long = "A fifty-four character line that dwells a good while..";   // 54 chars: 2.43 s

    [Fact]
    public void ALineWrittenBetweenFramesSurvivesALateFirstTick()
    {
        Assert.Equal(54, Long.Length);
        const double previousFrame = 10_000;
        const double lateTick = previousFrame + 5_000;   // a throttled tab: ticks 5 s apart
        PulseSlot slot = PulseSlot.Empty.Write(Long, PulseRank.Status, previousFrame, awaitFirstTick: true);

        slot = slot.Seen(lateTick).Expire(lateTick);
        Assert.Equal(Long, slot.Message);

        // â€¦and it then dwells its full length from that tick, and goes after it.
        Assert.Equal(Long, slot.Expire(lateTick + PulseSlot.DwellFor(Long) - 1).Message);
        Assert.Null(slot.Expire(lateTick + PulseSlot.DwellFor(Long) + 1).Message);
    }

    [Fact]
    public void ALineSaidBeforeTheFirstFrameIsNotRebased()
    {
        // The boot lines: written with no clock yet (stamp 0), they expire at the first tick exactly as before.
        PulseSlot slot = PulseSlot.Empty.Write(Long, PulseRank.Status, 0, awaitFirstTick: false);
        Assert.Null(slot.Seen(3_000).Expire(3_000).Message);
    }

    [Fact]
    public void ALineSeenOnTheFrameItWasWrittenOnDwellsExactlyAsItAlwaysDid()
    {
        PulseSlot slot = PulseSlot.Empty.Write(Long, PulseRank.Status, 4_000, awaitFirstTick: true).Seen(4_000);
        Assert.Equal(4_000 + PulseSlot.DwellFor(Long), slot.ExpiresMs);
        // seeing it again later does not extend it
        Assert.Equal(slot.ExpiresMs, slot.Seen(9_000).ExpiresMs);
    }

    [Fact]
    public void ALineFromTheNextFrameWindowIsNotGluedOntoARebasedOne()
    {
        // #1439 gap 1 · "the same call" is when the line was SAID, not the hold window Seen rebases.
        PulseSlot slot = PulseSlot.Empty.Write("First.", PulseRank.Status, 10_000, awaitFirstTick: true).Seen(15_000);
        // Unrelated line raised at a stamp that happens to equal the REBASED hold window's start.
        PulseSlot next = slot.Also("Second.", PulseRank.Status, 15_000 - PulseSlot.MinDwellMs + PulseSlot.MinDwellMs);
        Assert.Equal("Second.", next.Message);
        // …while two lines raised in one call (same stamp) are still joined.
        PulseSlot pair = PulseSlot.Empty.Write("A.", PulseRank.Status, 20_000, true).Also("B.", PulseRank.Status, 20_000, true);
        Assert.Equal("A. B.", pair.Message);
    }

    [Fact]
    public void AHeldLineReleasedAtCardCloseIsNotExpiredBeforeItsFirstPaint()
    {
        // #1439 gap 2 · ReleaseInto in a throttled tab.
        var held = PulseHold.Empty.Hold("A held line that waited behind a card for a good while.", PulseRank.Beat, 1_000);
        (PulseSlot slot, PulseHold rest) = held.ReleaseInto(PulseSlot.Empty, 10_000, awaitFirstTick: true);
        Assert.False(rest.Any);
        Assert.NotNull(slot.Seen(16_000).Expire(16_000).Message);

        // …and a second held line does not overwrite one no frame has met.
        var two = PulseHold.Empty.Hold("One.", PulseRank.Beat, 1_000).Hold("Two.", PulseRank.Beat, 1_001);
        (PulseSlot s1, PulseHold h1) = two.ReleaseInto(PulseSlot.Empty, 10_000, awaitFirstTick: true);
        (PulseSlot s2, _) = h1.ReleaseInto(s1, 20_000, awaitFirstTick: true);
        Assert.Equal("One.", s2.Message);
    }
}
