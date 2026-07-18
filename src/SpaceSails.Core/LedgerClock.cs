using System.Globalization;

namespace SpaceSails.Core;

/// <summary>How OLD a ledger receipt is, not what the wall clock read when it was filed. The
/// 2026-07-18 playtest caught the bug: a receipt cut seconds ago showed "logged 0d 16h 13m" —
/// the absolute sim clock formatted like a duration, so a brand-new line read as sixteen hours
/// stale. The captain's ledger wants an AGE ("logged just now", "logged 2h ago"), a relative read
/// off the current sim time. Pure text so it can be pinned by a test with no ship, no ephemeris.</summary>
public static class LedgerClock
{
    /// <summary>The receipt's age, relative to <paramref name="nowSimTime"/> — "just now" under a
    /// minute, then "Nm ago", "Nh ago", "Nd ago" as it recedes. A future or clock-skewed stamp
    /// (event ahead of now) clamps to "just now" rather than reading as negative age.</summary>
    public static string Age(double eventSimTime, double nowSimTime)
    {
        double seconds = Math.Max(0, nowSimTime - eventSimTime);
        if (seconds < 60)
        {
            return "just now";
        }

        long minutes = (long)(seconds / 60);
        if (minutes < 60)
        {
            return $"{minutes.ToString(CultureInfo.InvariantCulture)}m ago";
        }

        long hours = minutes / 60;
        if (hours < 24)
        {
            return $"{hours.ToString(CultureInfo.InvariantCulture)}h ago";
        }

        long days = hours / 24;
        return $"{days.ToString(CultureInfo.InvariantCulture)}d ago";
    }
}
