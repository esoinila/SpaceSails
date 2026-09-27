using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · WHETHER ANYBODY COMES, AND WHO — the dice for a watch, the nobody-came lines as said, every line
/// of prose for the sweeps, and the one who comes over.
///
/// <para>Split out of <c>SittingAlone.cs</c> under #251 as a pure move: two runs of the base file, no
/// member renamed, re-scoped or re-ordered. The six <c>NobodyCame…</c> pools between the two runs are
/// <c>static readonly</c> and stay in the opening file in their original order (#1163).</para>
/// </summary>
public static partial class SittingAlone
{
    // ── WHETHER ANYBODY COMES ─────────────────────────────────────────────────────────────────────────
    //
    // FIFTH-BUG-CLASS WARNING, paid up front: a threshold that selects everything, or nothing, is a guard
    // that asserts nothing. The die is DiceRule's own d20 and the threshold below is derived from the hall's
    // own WatchFill — 0.15 on the small watch, 0.95 in the middle of the day — so on EVERY watch the game
    // has, both answers are reachable, and a guard measures that against real rolls rather than trusting the
    // arithmetic in this comment.

    /// <summary>How many faces the approach is rolled on. The house d20, like everything else.</summary>
    public const int Faces = DiceRule.D20;

    /// <summary>How many of those faces bring somebody over when the hall is FULL. Roughly two beats in
    /// five at the heaving watch, and the fraction scales down with how many people are in the room, so a
    /// dead hall is a dead hall. FLAGGED for the owner's tuning — this number is the whole tempo of waiting.
    /// </summary>
    public const int FacesWhenPacked = 8;

    /// <summary>How full the hall is on this watch. <see cref="CanteenRegulars.WatchFill"/>'s own number and
    /// never a second one — the room the captain is looking at is the room that decides whether anybody has
    /// a reason to cross it.</summary>
    public static double Fill(long watch)
    {
        IReadOnlyList<double> bill = CanteenRegulars.WatchFill;
        return bill[(int)(((watch % bill.Count) + bill.Count) % bill.Count)];
    }

    /// <summary>How many faces of the d20 bring somebody over on THIS watch. At least one on every watch —
    /// a room with people in it is never a room where nobody can possibly walk up — and it is a floor rather
    /// than a rounding, so the emptiest shift stays the emptiest shift.</summary>
    public static int FacesThatBringSomebody(long watch) =>
        Math.Clamp((int)Math.Round(FacesWhenPacked * Fill(watch), MidpointRounding.AwayFromZero), 1, Faces);

    /// <summary>
    /// #757 · DOES ANYBODY CROSS THE ROOM THIS BEAT?
    ///
    /// <para>Seeded on (site, floor, table, watch, beat) and nothing else, so the same wait at the same table
    /// on the same shift is the same answer — a captain cannot re-press their way into company, and a test
    /// can walk beats to reach either outcome instead of mocking a die.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="tableIndex">Which top — Core's own ordinal, never a pair of doubles.</param>
    /// <param name="watch">The shift, frozen when the floor was drawn (#709).</param>
    /// <param name="beat">How many times you have waited at this table this sitting, from zero.</param>
    /// <param name="quiet">#751 · Whether this top is in a CABINET — a room the hall cannot see into. Then
    /// NOBODY comes, ever, and that is not a gap in the content: waiting at a table is a choice to be
    /// findable, and a cabinet is the room you take when you have chosen the opposite. The one law states
    /// itself twice, once in each direction.</param>
    public static bool SomebodyComes(
        string bodyId, int level, int tableIndex, long watch, int beat, bool quiet = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (quiet)
        {
            return false;
        }
        DiceRoll roll = DiceRule.Roll(
            DiceRule.Seed($"canteen:approach:{bodyId}:{tableIndex}", level, watch, beat), Faces);
        return roll.Face <= FacesThatBringSomebody(watch);
    }

    /// <summary>#1040 · The silence at the counter, beat by beat — same shape as
    /// <see cref="NobodyCameAboard"/> and deliberately a second call rather than a third flag on it: two
    /// bools in one signature is a room described by arithmetic.</summary>
    /// <param name="beat">How many times you have waited on this stool this sitting, from zero.</param>
    public static string NobodyCameAtYourOwnCounter(int beat) =>
        NobodyCameShipCounter[
            (int)(((beat % NobodyCameShipCounter.Count) + NobodyCameShipCounter.Count)
                  % NobodyCameShipCounter.Count)];

    /// <summary>#1016 · Which silence a beat aboard gets. No watch is consulted and that is not an omission:
    /// the ship's own rooms are not filled by a rota, so the ONLY thing that varies is which beat this is —
    /// a captain who waits twice is told two different things and the room does not loop.</summary>
    /// <param name="cabin">Whether this is the berth's desk rather than a cantina top.</param>
    /// <param name="beat">How many times you have waited at this seat this sitting, from zero.</param>
    public static string NobodyCameAboard(bool cabin, int beat)
    {
        IReadOnlyList<string> pool = cabin ? NobodyCameShipCabin : NobodyCameShipCantina;
        return pool[(int)(((beat % pool.Count) + pool.Count) % pool.Count)];
    }

    /// <summary>Every line in all three pools, for the canon grep. The guard walks THIS, so a line added tomorrow
    /// is checked tomorrow.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string s in NobodyCameBusy)
        {
            yield return s;
        }
        foreach (string s in NobodyCameQuiet)
        {
            yield return s;
        }
        foreach (string s in NobodyCameCabinet)
        {
            yield return s;
        }
        // #1016 · …and the ship's own two, walked by the same grep the hall's three are. A pool the sweep
        // cannot see is a pool that is checked by nobody, which is why this list exists at all.
        foreach (string s in NobodyCameShipCantina)
        {
            yield return s;
        }
        foreach (string s in NobodyCameShipCabin)
        {
            yield return s;
        }
        // #1040 · …and the counter's, walked by the same grep for the same reason.
        foreach (string s in NobodyCameShipCounter)
        {
            yield return s;
        }
        yield return ShipCantinaSetting;
        yield return ShipCabinSetting;
        yield return ShipCounterSetting;
        yield return OwnDeskPlate;
        yield return OwnStoolPlate;
        yield return FreeStoolPlate;
        yield return TookTheTableLine;
        yield return StoodUpLine;
        // #783 · the other register, checked by the same grep the wary one is.
        yield return SatDownLine;
        yield return RelaxedSitLine;
        yield return RelaxedSitDryLine;
        yield return TheDrinkLine;
        yield return TheDrinkAboardLine;
        yield return RelaxedSitAboardLine;
        yield return StoodUpRelaxedLine;
        yield return ApproachOpening;
        yield return WaveInLine;
        yield return WaveOffLine;
        yield return DrinkTakenLine;
        yield return DrinkDeclinedLine;
        yield return TheAskLine;
        yield return TheAskNote;
        yield return VisitorPlate;
        yield return FreeTablePlate;
        yield return OwnTablePlate;
    }

    /// <summary>
    /// #757 · WHAT NOTHING HAPPENING SOUNDS LIKE — the told outcome, so a wait that produced nobody reads as
    /// an answer rather than as a control that did not respond.
    /// </summary>
    /// <param name="watch">The shift, which decides which of the two silences this is.</param>
    /// <param name="beat">Which wait this was, so a captain who sits through four of them is told four
    /// different things and the room does not loop.</param>
    /// <param name="quiet">Whether this is a cabinet, where nobody was ever going to come.</param>
    public static string NobodyCame(long watch, int beat, bool quiet = false)
    {
        IReadOnlyList<string> pool = quiet
            ? NobodyCameCabinet
            : Fill(watch) >= BusyAt ? NobodyCameBusy : NobodyCameQuiet;
        return pool[(int)(((beat % pool.Count) + pool.Count) % pool.Count)];
    }

    // ── THE ONE WHO COMES OVER ────────────────────────────────────────────────────────────────────────
    //
    // She is a HAULIER with her coat still on, which is the register test (#701) surviving contact with a
    // quest-giver: she is not mysterious, she is not interesting, and she is not the plot. She is somebody
    // with an ordinary reason to cross a room, and every word she says is about her own family and her own
    // week. What is horrifying about it is a thing the game never states.
    //
    // AND SHE POINTS AT WHAT IS ALREADY BUILT. The Hand who has been here longer than the contract said
    // (#746) writes the names, the chit he writes them on rides the cage (#752), and the cage goes down.
    // Nothing new is promised by this scene that the game cannot already deliver.

    /// <summary>Who she reads as, at a glance, before she says anything.</summary>
    public const string VisitorPlate = "◈ A HAULIER WITH HER COAT STILL ON";

    /// <summary>What stopping at your table looks like from your side of it.</summary>
    public const string ApproachOpening =
        "Somebody has crossed the whole hall to stand at your table with her coat still on. \"Nobody's in " +
        "that one, are they.\"";

    /// <summary>The first rung: you pull the chair out, and she has a reason ready for why she should stay
    /// at it. The drink is offered by HER — which is the rung, and it is the offer this scene inverts.</summary>
    public const string WaveInLine =
        "She sits like somebody who has practised sitting down at strangers' tables. \"Let me get these " +
        "in. You don't have to drink it.\"";

    /// <summary>…or you do not, and it costs nothing. She goes, and the way she goes is the whole
    /// characterisation.</summary>
    public const string WaveOffLine =
        "\"Right. Fair enough.\" She goes back the way she came, and does not stop at anybody else's table " +
        "on the way.";

    /// <summary>The second rung, taken.</summary>
    public const string DrinkTakenLine =
        "Two glasses come over from the counter on her tab. She does not touch hers.";

    /// <summary>The second rung, declined — and declining is not a refusal of her, it is just an answer.</summary>
    public const string DrinkDeclinedLine =
        "\"Suit yourself.\" She folds her hands on the table, which is somehow worse.";

    /// <summary>
    /// THE THIRD RUNG — what she came over for, said plainly, the way somebody asks a stranger for a
    /// favour they have already rehearsed.
    ///
    /// <para>#761 · Told clearly. There is nothing to infer about what she wants; the inference is somewhere
    /// else entirely, and she does not know she is standing next to it.</para>
    /// </summary>
    public const string TheAskLine =
        "\"My brother took a down-contract here in the spring. The money still comes home every month, " +
        "regular as a clock, and there hasn't been a word with it since March. I can't get in the cage — " +
        "they know my face at the counter. You're new. Ask the hand who's been here longest. He's the one " +
        "who writes the names.\"";

    /// <summary>What the field book keeps of it. The book records what she said and what it points at, and
    /// never what it might mean.</summary>
    public const string TheAskNote =
        "A haulier's brother took a down-contract here in the spring. The money still comes home; he does " +
        "not write. She cannot get in the cage. The hand who has been here longest writes the names.";

    /// <summary>
    /// #757 · THE APPROACH, as an <see cref="Encounter.Scene"/> — the three-rung ladder.
    ///
    /// <para>Every rung is <see cref="Encounter.Requirement.ReplyToPriorMove"/>, so nothing is on the panel
    /// before the sentence it answers has been spoken (#749). Read down the list and the courtship is legible
    /// as data: the chair, then the drink, then the ask — and the ask needs the drink ANSWERED, either way,
    /// because turning a drink down is still having heard the offer.</para>
    /// </summary>
    public static Encounter.Scene TheVisitor() => new(
        "canteen:table:approach",
        VisitorPlate,
        Setting,
        ApproachOpening,
        [
            new(WaveIn, LabelOf(WaveIn), Says: WaveInLine),
            new(WaveOff, LabelOf(WaveOff), Says: WaveOffLine),

            new(LetThemBuy, LabelOf(LetThemBuy),
                Encounter.Requirement.ReplyToPriorMove, After: WaveIn, Says: DrinkTakenLine),
            new(NoDrink, LabelOf(NoDrink),
                Encounter.Requirement.ReplyToPriorMove, After: WaveIn, Says: DrinkDeclinedLine),

            // …and it is the ONE rung the field book keeps. The note rides the move (#757's addition to
            // Encounter.Move), so no client author has to remember which sentence was worth writing down.
            new(HearThemOut, LabelOf(HearThemOut),
                Encounter.Requirement.ReplyToPriorMove, After: LetThemBuy, OrAfter: NoDrink,
                Says: TheAskLine, Note: TheAskNote),

            new(Stand, LabelOf(Stand), Says: CanteenTable.LeaveLine),
        ]);
}
