namespace SpaceSails.Core.Tests;

/// <summary>The 2026-07-18 playtest bug: a receipt filed seconds ago showed "logged 0d 16h 13m" —
/// the absolute sim clock read as an age. LedgerClock renders a TRUE age off the current sim time,
/// so a fresh line reads "just now", never "sixteen hours old".</summary>
public class LedgerClockTests
{
    [Fact]
    public void SecondsOld_ReadsJustNow_NotTheAbsoluteClock()
    {
        // The exact playtest case: now is deep in the run (T+16h13m), the receipt was cut seconds ago.
        double now = (16 * 3600) + (13 * 60) + 40;
        Assert.Equal("just now", LedgerClock.Age(now - 3, now));
    }

    [Fact]
    public void UnderAMinute_IsJustNow()
    {
        Assert.Equal("just now", LedgerClock.Age(0, 0));
        Assert.Equal("just now", LedgerClock.Age(0, 59));
    }

    [Fact]
    public void Minutes_Hours_Days_EachTierReads()
    {
        Assert.Equal("2m ago", LedgerClock.Age(0, 2 * 60));
        Assert.Equal("59m ago", LedgerClock.Age(0, 59 * 60));
        Assert.Equal("1h ago", LedgerClock.Age(0, 3600));
        Assert.Equal("23h ago", LedgerClock.Age(0, 23 * 3600));
        Assert.Equal("1d ago", LedgerClock.Age(0, 86400));
        Assert.Equal("5d ago", LedgerClock.Age(0, 5 * 86400));
    }

    [Fact]
    public void FutureOrSkewedStamp_ClampsToJustNow_NeverNegative()
    {
        Assert.Equal("just now", LedgerClock.Age(500, 100));
    }
}
