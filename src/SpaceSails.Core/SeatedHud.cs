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
        /// <summary>#821 · A WC cubicle with the catch over. The TOP of the ladder and the reason it grew a
        /// rung: smaller than a bench, more private than an office, and the one seat in the game whose
        /// privacy does not depend on anybody else's behaviour — the owner's own reading, <i>"half a bench
        /// is not a desk; a locked cubicle absolutely is"</i>.
        ///
        /// <para>It is FIRST because this enum is ordered private end to public end and says so, and because
        /// nothing in the game reads a <c>Seat</c> off a stored number: it is derived from the seat the
        /// captain is in, every frame, by <c>Map.Seated</c>.</para></summary>
        LockedCubicle,

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
        Seat.LockedCubicle => "A LOCKED CUBICLE",
        Seat.Cabinet => "A CABINET TABLE",
        Seat.ParkBench => "A PARK BENCH",
        Seat.BarStool => "THE BAR DESK",
        _ => "YOUR OWN TABLE",
    };

    /// <summary>#865 · What a top you are SHARING is called. The first clause of the customer line answers
    /// <i>do I still hold the seat</i>, and at a top with the weighbridge clerk's tray on the other half of
    /// it the honest answer is not <see cref="SeatLabel(Seat)"/>'s "YOUR OWN TABLE" — a strip that said so
    /// while somebody ate opposite would be the sentence and the room disagreeing, which is this project's
    /// third named bug class read at the width of one clause.</summary>
    public const string SharedTopLabel = "A TOP YOU ARE SHARING";

    /// <summary>#865 · …and the same question asked with the company in it. Only the two TABLE rungs can be
    /// shared this way — a bench's far end is <c>SharedSeat</c> and #793 already names it, and nobody shares
    /// a locked cubicle.</summary>
    public static string SeatLabel(Seat seat, bool shared) =>
        shared && seat is Seat.HallTable or Seat.Cabinet ? SharedTopLabel : SeatLabel(seat);

    // ── #865 · THE COMPANY, ON THE STRIP'S OWN LINES ──────────────────────────────────────────────────
    //
    // Owner, live at a weighbridge clerk's table and blinded by the card that used to come up: "what if I
    // just sit and eat here… now I am kind of blinded of the surrounding here because somebody else sits in
    // the same table" — and, sealing the shape of the fix: "It should somehow UI wise be same style as the
    // sitting alone case." So the company arrives as MORE LINES ON THE STRIP the captain already had, in the
    // same separator and the same register as every clause on it. Nothing here is a second vocabulary for
    // facts the card was already saying; the plate is the one the deck draws over them and the chairs are
    // the room's own numbers.

    /// <summary>#865 · How the strip counts the chairs — the fact that let you sit down here at all
    /// (#746/#739), said in the strip's own voice rather than the card's.</summary>
    public static string ChairsClause(int seats, int free)
    {
        int chairs = Math.Max(0, seats);
        int spare = Math.Clamp(free, 0, chairs);
        return spare == 1
            ? $"seats {chairs}, one chair free"
            : $"seats {chairs}, {spare} chairs free";
    }

    /// <summary>#865 · The mark on the company line. Two figures, because that is what has changed about the
    /// seat.</summary>
    public const string CompanyGlyph = "🧑‍🤝‍🧑";

    /// <summary>#865 · WHO IS SHARING THE TOP — the strip's company line, built from the plate the deck
    /// already draws over them and the room's own arithmetic.</summary>
    public static string CompanyLine(string? plate, string? setting, int seats, int free) =>
        string.Join(Join,
            $"{CompanyGlyph} {(plate ?? "").Trim()}",
            (setting ?? "").Trim(),
            ChairsClause(seats, free));

    /// <summary>#865 · Their one breath, quoted on the strip. A background patron has exactly one line dealt
    /// to them per watch (#751's bark) and it used to be reachable only by pressing Small talk inside a card
    /// that had eaten the room; on the strip it is simply OVERHEARD, which is what sitting down at somebody
    /// else's table in a canteen actually gets you.</summary>
    public static string OverheardLine(string? bark) => $"“{(bark ?? "").Trim()}”";

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
    /// <para>A cabinet is not a fill at all: the room is what you paid for, nobody comes, and the strip says
    /// the thing that is actually true about the room you are in rather than a crowd figure for a hall you
    /// cannot see.</para>
    ///
    /// <para>#758 · …AND WHICH OF THE TWO STAGES IT IS STANDING AT. This clause said <i>the door is shut</i>
    /// on every cabinet in the game, which was true while a cabinet came with its leaf dogged and stopped
    /// being true the day the curtain shipped. A strip reporting a shut door to a captain sitting behind
    /// cloth is a sentence and a sim disagreeing about one leaf — the third named bug class — so the stage
    /// is handed in, and it defaults to the state every cabinet in the building is in.</para></summary>
    public static string RoomClause(
        Seat seat, double watchFill, CabinetPrivacy.Stage cabinetStage = CabinetPrivacy.Stage.Curtain)
    {
        if (seat == Seat.Cabinet)
        {
            return cabinetStage == CabinetPrivacy.Stage.Door ? CabinetDoggedClause : CabinetCurtainClause;
        }

        // #821 · …AND A LOCKED CUBICLE IS NOT A ROOM YOU CAN BE CROSSED IN EITHER — but for a different
        // reason, and the strip says the true one. A cabinet's door was PAID for and nobody comes; this door
        // is a catch on a partition and the floor is still out there, walking. Reporting a crowd figure here
        // would be #740's fault; reporting the cabinet's line would be worse, because it would say the
        // captain was safe.
        if (seat == Seat.LockedCubicle)
        {
            return LockedCubicleClause;
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

    /// <summary>#821 · The room clause a LOCKED CUBICLE gets. The sentry's own law on the instrument row: it
    /// states what the catch has actually bought, which is a door and a delay and not a hiding place.</summary>
    public const string LockedCubicleClause =
        "the catch is over — the door buys time, and the floor is still out there";

    /// <summary>#758 · The room clause a cabinet gets AT STAGE ONE. Both halves are true at once and the
    /// second half is the one the mechanic lives in: nobody will walk in on you, and cloth is not steel.</summary>
    public const string CabinetCurtainClause =
        "the curtain is across — nobody is crossing the room to you, and cloth is not steel";

    /// <summary>…and AT STAGE TWO, which is the sentence the strip used to say about every cabinet in the
    /// building and now says only about the ones somebody decided about.</summary>
    public const string CabinetDoggedClause =
        "the door is dogged — nobody is crossing the room to you";

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
    /// <param name="sharedTop">#865 · Is somebody else at this top? Optional, and it changes exactly one
    /// clause — the seat's own name — because the whole ruling is that co-seating is the SAME strip with
    /// company in it and not a second instrument.</param>
    /// <param name="cabinetStage">#758 · Which stage the cabinet is standing at, ignored at every other
    /// seat. Appended and defaulted, so every existing caller still means the room it meant.</param>
    public static string CustomerLine(
        Seat seat, double? pourSecondsLeft, int pipsEasedThisWatch, int pipCap, double watchFill,
        bool sharedTop = false, CabinetPrivacy.Stage cabinetStage = CabinetPrivacy.Stage.Curtain) =>
        string.Join(Join,
            $"{Glyph} {SeatLabel(seat, sharedTop)}",
            PourClause(pourSecondsLeft),
            RestClause(pipsEasedThisWatch, pipCap),
            RoomClause(seat, watchFill, cabinetStage));

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
        yield return RoomClause(Seat.LockedCubicle, 0);
        yield return RoomClause(Seat.Cabinet, 0, CabinetPrivacy.Stage.Curtain);
        yield return RoomClause(Seat.Cabinet, 0, CabinetPrivacy.Stage.Door);
        yield return RoomClause(Seat.ParkBench, 0);
        yield return RoomClause(Seat.HallTable, 1);
        yield return RoomClause(Seat.HallTable, SittingAlone.BusyAt);
        yield return RoomClause(Seat.HallTable, 0);
        yield return SharedTopLabel;
        yield return ChairsClause(4, 1);
        yield return ChairsClause(4, 2);
        foreach (Seat s in Enum.GetValues<Seat>())
        {
            yield return SeatLabel(s);
        }
    }
}
