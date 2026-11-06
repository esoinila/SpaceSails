using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1439 part 1 · A line written BETWEEN frames is stamped with the previous frame's clock, so in a throttled or
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

        // …and it then dwells its full length from that tick, and goes after it.
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
}
