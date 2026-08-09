using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #784 · THE SEATED STRIP — what the HUD says while you are in a chair, and the CUSTOMER LINE it says it in.
///
/// <para>Owner, live 2026-08-08 night, at a taken table with the panel up and the scrim eating the park
/// behind it: <i>"the seated frame docks, it does not dim… the hall stays lit, the A* walkers stay visible,
/// the park stays green. The full card returns only for conversations."</i> And, naming the instrument the
/// strip is written in: <i>"maybe the HUD could have place for our current state as customer of the
/// restaurant, like we have really superb UI for the air use."</i></para>
///
/// <h3>The air gauge's register, borrowed on purpose</h3>
///
/// <para><see cref="SuitAir.Readout"/> is the shape: a LABEL, a FIGURE, and then a clause that says where the
/// figure came from. It is the most trusted line in the game because every number on it is the number the
/// mechanic is actually using. This line is built to that discipline and to nothing else — four clauses, one
/// per question a seated player has:</para>
///
/// <list type="number">
/// <item><b>Do I still hold the seat?</b> — the seat's own label.</item>
/// <item><b>Is the drink still a drink?</b> — the pour window the short rest reads
/// (<c>APourInFrontOfYou</c>), in minutes, and <see cref="ShortRest.DrinkNerveMultiplier"/> is what it is
/// worth.</item>
/// <item><b>How much rest is banked against the ceiling?</b> — <see cref="ShortRest"/>'s own per-watch
/// counter and <see cref="ShortRest.NervePipCapPerWatch"/>, not a second tally.</item>
/// <item><b>What watch is the room keeping?</b> — <see cref="SittingAlone.Fill"/>, the same number the
/// approach odds are scaled by, so "the hall is heaving" and "somebody is more likely to come over" are one
/// fact said twice.</item>
/// </list>
///
/// <h3>Why it cannot disagree with the rest engine</h3>
///
/// <para>Every figure is an ARGUMENT. This file owns no clock, no ledger and no dice; the caller hands it the
/// values the mechanics are running on and this decides only the words. That is #740's law (the DEAD AIR card
/// quoting a stopwatch nobody else was reading) pre-empted by construction rather than by care: there is no
/// second copy of any of these numbers for this line to drift away from.</para>
/// </summary>
public static class SeatedHud
{
    /// <summary>The strip's own mark. Same chair the plates wear (<see cref="SittingAlone.Glyph"/>), because
    /// it is the same posture.</summary>
    public const string Glyph = SittingAlone.Glyph;

    /// <summary>Which seat the captain is in. The exposure ladder in one enum, ordered from the private end
    /// to the public one, exactly as the owner priced it: <i>"cabinet (guaranteed) &gt; empty park bench
    /// (conditional) &gt; hall table &gt; bar desk."</i></summary>
    public enum Seat
    {
        /// <summary>A table behind a cabinet door. Nobody comes (<see cref="SittingAlone.SomebodyComes"/>
        /// returns false for a quiet top) and nobody sees.</summary>
        Cabinet,

        /// <summary>A steel bench in the park. Private only while the whole bench is yours (#793).</summary>
        ParkBench,

        /// <summary>An open top in the hall. The middle rung: people cross the room to it.</summary>
        HallTable,

        /// <summary>A stool at the counter, under the keep's eye and the neighbour's elbow (#789).</summary>
        BarStool,
    }

    /// <summary>What the strip calls the seat you are in. Plain words (#782/#783), and the ones the plates
    /// already use where a plate exists — the cabinet and the bench are the two this file has to name itself,
    /// because neither has ever had a panel of its own.</summary>
    public static string SeatLabel(Seat seat) => seat switch
    {
        Seat.Cabinet => "A CABINET TABLE",
        Seat.ParkBench => "A PARK BENCH",
        Seat.BarStool => "THE BAR DESK",
        _ => "YOUR OWN TABLE",
    };

    // ── THE FOUR CLAUSES ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The drink clause. <paramref name="pourSecondsLeft"/> is null when there is no bought pour in
    /// front of the captain — which is a fact about the counter and the clock and never about this line.
    ///
    /// <para>Its own minutes and deliberately NOT <see cref="SuitAir.Clock"/>: a glass and a tank are two
    /// budgets, and putting one through the other's converter is exactly the shape #740 was filed on.</para>
    /// </summary>
    public static string PourClause(double? pourSecondsLeft)
    {
        if (pourSecondsLeft is not { } left || left <= 0)
        {
            return "NO POUR — nothing bought, and the rest comes back at the plain rate";
        }
        string clock = left < 60 ? "under a minute" : $"{Math.Ceiling(left / 60.0):F0}m";
        return $"POUR in hand, {clock} of cold left — the rest lands {ShortRest.DrinkNerveMultiplier}× as fast";
    }

    /// <summary>The rest clause, in <see cref="ShortRest"/>'s own pips against
    /// <see cref="ShortRest.NervePipCapPerWatch"/>. When the ceiling is reached it says so, because that is
    /// the moment the mechanic stops paying and a strip that kept printing a figure would be lying by
    /// omission (#603).</summary>
    public static string RestClause(int pipsEasedThisWatch, int pipCap)
    {
        int eased = Math.Max(0, pipsEasedThisWatch);
        int cap = Math.Max(0, pipCap);
        return eased >= cap && cap > 0
            ? $"REST {eased}/{cap} pips — that is all a chair gives this watch"
            : $"REST {eased}/{cap} pips";
    }

    /// <summary>The room clause, off <see cref="SittingAlone.Fill"/> — the same fraction the approach roll is
    /// scaled by, so how full the hall reads and how likely somebody is to cross it are one fact.
    ///
    /// <para>A cabinet is not a fill at all: the door is what you paid for, nobody comes, and the strip says
    /// the thing that is actually true about the room you are in rather than a crowd figure for a hall you
    /// cannot see.</para></summary>
    public static string RoomClause(Seat seat, double watchFill)
    {
        if (seat == Seat.Cabinet)
        {
            return "the door is shut — nobody is crossing the room to you";
        }

        // #793 · …AND A BENCH IS NOT IN THE HALL AT ALL. Same law, the other way out: a strip that reported
        // "the hall is heaving" to a captain sitting on gravel behind a window wall would be quoting a crowd
        // figure for a room they cannot see, which is the exact fault this file was built to be incapable of
        // (#740). What is true on a bench is the thing the whole sit is FOR — the sight lines. Owner: "the
        // park's openness is the point: the same move at a hall table proves nothing."
        if (seat == Seat.ParkBench)
        {
            return OpenWalkClause;
        }

        double fill = Math.Clamp(watchFill, 0, 1);
        return fill >= HeavingAt ? "the hall is heaving"
            : fill >= SittingAlone.BusyAt ? "the hall is working"
            : "the hall is thin";
    }

    /// <summary>Where "heaving" starts. Above <see cref="SittingAlone.BusyAt"/> (0.40, which is where the
    /// room's own silence lines switch register) and below full, so all three words are reachable on the
    /// watches the game actually has — a threshold that selected everything, or nothing, would be a clause
    /// that says nothing (the fifth bug class). FLAGGED for the owner's tuning.</summary>
    public const double HeavingAt = 0.75;

    /// <summary>#793 · The room clause a PARK BENCH gets. It states the one fact that is true of the room
    /// you are actually in and that the sit is for: nothing crosses that gravel unseen, so anybody who is
    /// on the walk is on the walk in front of you.</summary>
    public const string OpenWalkClause =
        "the walk runs clear both ways — nobody crosses this gravel out of sight";

    // ── THE LINE ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the clauses are joined with. One separator, so the line reads as one instrument.</summary>
    public const string Join = " · ";

    /// <summary>
    /// #784 · THE CUSTOMER LINE — the seated strip's one instrument row.
    /// </summary>
    /// <param name="seat">Which seat is held.</param>
    /// <param name="pourSecondsLeft">Seconds left in the bought-pour window, or null for no pour. The
    /// caller's own <c>APourInFrontOfYou</c> window and never a second one.</param>
    /// <param name="pipsEasedThisWatch">Nerve pips this watch has already handed back — the excursion's
    /// ledger, the one <see cref="ShortRest.Beat"/> is asked against.</param>
    /// <param name="pipCap"><see cref="ShortRest.NervePipCapPerWatch"/>, passed in so a guard can watch the
    /// line move with the ceiling rather than trusting a constant it also reads.</param>
    /// <param name="watchFill"><see cref="SittingAlone.Fill"/> for the frozen watch.</param>
    public static string CustomerLine(
        Seat seat, double? pourSecondsLeft, int pipsEasedThisWatch, int pipCap, double watchFill) =>
        string.Join(Join,
            $"{Glyph} {SeatLabel(seat)}",
            PourClause(pourSecondsLeft),
            RestClause(pipsEasedThisWatch, pipCap),
            RoomClause(seat, watchFill));

    // ── THE STRIP'S OWN CHROME ────────────────────────────────────────────────────────────────────────

    /// <summary>What the strip is called for a screen reader, and the one place the frame's whole promise is
    /// written down: this is a HUD, not a card, and the room behind it is still running.</summary>
    public const string StripLabel = "Seated — the room is still running";

    /// <summary>Every sentence this file can put on a screen, for the canon sweep. The two parameterised
    /// clauses are listed in their plainest form.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return StripLabel;
        yield return PourClause(null);
        yield return PourClause(240);
        yield return RestClause(0, ShortRest.NervePipCapPerWatch);
        yield return RestClause(ShortRest.NervePipCapPerWatch, ShortRest.NervePipCapPerWatch);
        yield return RoomClause(Seat.Cabinet, 0);
        yield return RoomClause(Seat.ParkBench, 0);
        yield return RoomClause(Seat.HallTable, 1);
        yield return RoomClause(Seat.HallTable, SittingAlone.BusyAt);
        yield return RoomClause(Seat.HallTable, 0);
        foreach (Seat s in Enum.GetValues<Seat>())
        {
            yield return SeatLabel(s);
        }
    }
}
