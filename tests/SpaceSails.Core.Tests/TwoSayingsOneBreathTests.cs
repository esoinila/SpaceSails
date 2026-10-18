using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// THE SECOND LINE OF ONE BREATH IS SAID BESIDE THE FIRST, NOT OVER IT. Equal ranks overwrite, so a receipt and
/// the consequence it set going, written in one call, left only the consequence on the glass.
///
/// <para><b>RED</b> by making <c>PulseSlot.Also</c> a plain <c>Write</c> (the first line is gone), and by
/// making <c>IsPumpRunningLine</c> always true (the pump's warnings are wiped by the countdown).</para>
/// </summary>
public class TwoSayingsOneBreathTests
{
    [Fact]
    public void AlsoKeepsBothLinesWrittenAtTheSameInstant()
    {
        PulseSlot slot = PulseSlot.Empty.Write("first", PulseRank.Status, 1000.0)
            .Also("second", PulseRank.Status, 1000.0);
        Assert.Equal("first second", slot.Message);
    }

    [Fact]
    public void AlsoOverAnOlderLineIsJustAWrite()
    {
        PulseSlot slot = PulseSlot.Empty.Write("first", PulseRank.Status, 1000.0)
            .Also("second", PulseRank.Status, 5000.0);
        Assert.Equal("second", slot.Message);
    }

    [Fact]
    public void AlsoNeverLowersTheRankOfTheLineBesideIt()
    {
        PulseSlot slot = PulseSlot.Empty.Write("beat", PulseRank.Beat, 1000.0)
            .Also("weather", PulseRank.Status, 1000.0);
        Assert.Equal(PulseRank.Beat, slot.Rank);
        Assert.Equal("beat weather", slot.Message);
    }

    [Fact]
    public void ThePumpCountdownRefreshesOnlyOverItsOwnKind()
    {
        Assert.True(HullVenting.IsPumpRunningLine(null));
        Assert.True(HullVenting.IsPumpRunningLine(HullVenting.PumpRunningLine("BRIDGE", 30)));
        Assert.False(HullVenting.IsPumpRunningLine(HullVenting.PumpUnderfootLine("BRIDGE", 30)));
        string reaches = HullVenting.PumpReachesFurtherLine("BRIDGE", ["BRIDGE", "CREW SPACES"]);
        Assert.NotEmpty(reaches);
        Assert.False(HullVenting.IsPumpRunningLine(reaches));
    }
}
