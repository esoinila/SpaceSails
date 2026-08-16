using System;
using System.Collections.Generic;
using System.Reflection;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6b · WHERE THE SEAT'S STATE ACTUALLY IS.
///
/// <para>Several behaviour guards reach into a LIVE <see cref="Pages.Map"/> by reflection and ask it for
/// <c>_table</c> or <c>_stool</c> — <c>MustStandUpBeforeWalkingTests</c>, <c>CoSeatingIsAStripTests</c>,
/// <c>TheDeskHumsAndRisesTests</c>, <c>TheStallSaysSoTests</c>, <c>TwoGripsOneWalkTests</c>. They are the
/// strongest seat proof in the repository: they sit a captain down through the real handlers and then look
/// at the state, so a changed boolean anywhere in the family shows up as a red assertion rather than as a
/// source file that still reads plausibly.</para>
///
/// <para>6b moved those five fields onto <c>Map.Seating</c>, reached through the page's one <c>_seating</c>
/// field. <b>The LOOKUP follows the state; not one assertion moved.</b> That is the whole of this file, and
/// it lives in one place because five copies of "where the seat is now" is precisely the law-transcribed-at-
/// its-call-sites bug this repository has paid for four times.</para>
///
/// <para>It is deliberately NOT a fallback for any name: only the five are followed, and only when the page
/// really has a <c>_seating</c> on it. A guard asking for a field that has simply been deleted must still
/// fail loudly at its own helper, which is what keeps the anti-vacuous property those guards rely on.</para>
/// </summary>
public static class SeatState
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>The five, and what each is called on the seat object now. Raw name in, property name out.</summary>
    private static readonly Dictionary<string, string> Moved = new(StringComparer.Ordinal)
    {
        ["_table"] = "Table",
        ["_stool"] = "Stool",
        ["_standUpAsk"] = "StandUpAsk",
        ["_sitBeatOwedSeconds"] = "SitBeatOwedSeconds",
        ["_cubicleFloorKey"] = "CubicleFloorKey",
    };

    /// <summary>The seat object hanging off a live page, or null when this is not a page that has one.</summary>
    public static object? Seat(object o) =>
        o.GetType().GetField("_seating", Hidden)?.GetValue(o);

    /// <summary>
    /// Follow one of the five to where it now lives.
    /// </summary>
    /// <returns><c>true</c> only when <paramref name="name"/> is one of the five AND the page carries a seat
    /// object — in which case <paramref name="value"/> is the state a raw field read used to give. Anything
    /// else returns <c>false</c> and the caller's own lookup (and its own failure message) takes over.</returns>
    public static bool TryFollow(object o, string name, out object? value)
    {
        value = null;
        if (!Moved.TryGetValue(name, out string? property) || Seat(o) is not { } seat)
        {
            return false;
        }

        PropertyInfo? on = seat.GetType().GetProperty(property, Hidden | BindingFlags.Public);
        if (on is null)
        {
            throw new InvalidOperationException(
                $"#870 lane 6b · the seat object has no `{property}` — `{name}` was followed onto Seating " +
                "and is not there. Rename it in this table in the same commit; do not delete the row, " +
                "which would send every seat guard back to reading a dead name on Map.");
        }

        value = on.GetValue(seat);
        return true;
    }
}
