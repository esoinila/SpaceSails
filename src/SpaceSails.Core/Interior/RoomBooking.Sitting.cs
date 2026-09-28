using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #251 · WHO SITS OPPOSITE AND WHAT IS SAID — the delegation, the lines across the table, and the two
/// scenes.
///
/// <para>Split out of <c>RoomBooking.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>.</para>
/// </summary>
public static partial class RoomBooking
{
    // ── WHO IS SITTING OPPOSITE ───────────────────────────────────────────────────────────────────────
    //
    // A DELEGATION IS SOMEBODY WHO WAS ALREADY IN THE BUILDING. Owner's grammar for the room: you book it and
    // you put something to somebody across a table. So the party opposite is drawn from the hall's own rota
    // (CanteenRegulars) off the watch you booked ON — a room booked on a shift with nobody in the canteen is
    // a room with nobody in it, and being told that in an eighty-seat building IS the event (#757's law).
    //
    // Deterministic and re-derivable: the pick is a pure function of (site, floor, watch) and the rota the
    // deck was drawn from, so nothing about who is opposite has to be remembered anywhere. That is what lets
    // the booking be five integers in a book.

    /// <summary>Which of the people in this hall walked over. Null when the hall is empty this watch, which
    /// is a reachable and deliberate outcome — see <see cref="RoomIsYoursAndEmptyLine"/>.</summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="watch">The shift, frozen (#709).</param>
    /// <param name="rota">Who is in this hall on it — <see cref="CanteenRegulars.Sitting"/>'s own list,
    /// handed over rather than re-derived, so the person across the table is a person the captain could have
    /// walked up to instead.</param>
    public static CanteenRegulars.Seated? TheDelegation(
        string bodyId, int level, long watch, IReadOnlyList<CanteenRegulars.Seated>? rota)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (rota is null || rota.Count == 0)
        {
            return null;
        }

        // TWO THINGS HAVE TO BE TRUE AND THEY ARE DIFFERENT KINDS OF TRUE — the counter's own stool law
        // (TheStools.SomebodyTurns) said back at a table. Somebody has to be in this hall on this shift (the
        // room), and they have to have actually come up (the dice). THE ODDS ARE THE HALL'S OWN —
        // SittingAlone.FacesThatBringSomebody, unchanged and uncopied, because "does this room have anybody
        // for you tonight" is one question however you asked it — and the DIE is the booking's, so a room
        // taken on a busy shift is likelier to have somebody across the table and a dead one likelier not.
        //
        // Both answers are the feature. A room you paid for with nobody in it is #757's own law arriving
        // where it costs the most: nothing happening is an OUTCOME here (RoomIsYoursAndEmptyLine), and being
        // told so after handing over the coin is the whole of what this building is like.
        DiceRoll came = DiceRule.Roll(
            DiceRule.Seed($"hive:booking:came:{bodyId}", level, watch), SittingAlone.Faces);
        if (came.Face > SittingAlone.FacesThatBringSomebody(watch))
        {
            return null;
        }

        ulong seed = DiceRule.Seed($"hive:booking:delegation:{bodyId}", level, watch);
        return rota[(int)(seed % (ulong)rota.Count)];
    }

    // ── WHAT IS SAID ACROSS IT ────────────────────────────────────────────────────────────────────────

    /// <summary>Where you are, while you are in one.</summary>
    public const string Setting = "the long table in a booked negotiation room";

    /// <summary>Who the captain reads as on the strip when the room is theirs and empty.</summary>
    public const string EmptyRoomPlate = "🪟 A ROOM YOU PAID FOR";

    /// <summary>
    /// SITTING DOWN WITH NOBODY OPPOSITE. #783's law kept — the first clause confirms the state change — and
    /// the whole of what the room is worth when the rota was against you.
    ///
    /// <para><b>NEW PROSE, flagged for the owner to bless.</b></para>
    /// </summary>
    public const string RoomIsYoursAndEmptyLine =
        "You take the head of the long table in a room you have paid for, and nobody comes. The chairs down "
        + "both sides are pushed in square, the lamps over the green are still four in the afternoon, and "
        + "the whole of what you bought was twenty minutes of nobody being able to walk in on you.";

    /// <summary>…and sitting down with somebody across it. The plate is the room's own — one of the ten who
    /// were in the hall — and the sentence says nothing about why they agreed.</summary>
    /// <param name="plate">Who is opposite, at plate size.</param>
    public static string TheyAreSeatedOppositeLine(string plate) =>
        $"You take one side of the long table and {Downcased(plate)} takes the other, with the chair "
            + "pulled out and put back the way somebody does when they have done this before. Nobody says "
            + "what the twenty minutes are for.";

    /// <summary>A plate, said mid-sentence. The stencil is shouted because a stencil is; a sentence about a
    /// person is not, and the glyph is not a word.</summary>
    private static string Downcased(string plate)
    {
        string words = plate.Replace(CanteenRegulars.Glyph, "", StringComparison.Ordinal).Trim();
        return words.Length == 0 ? "somebody" : words.ToLowerInvariant();
    }

    /// <summary>The one deal move's id. Named here so no panel invents its own vocabulary for a move the
    /// design named — <see cref="TheStools.LabelOf"/>'s own discipline.</summary>
    public const string PutItToThem = "put-it-to-them";

    /// <summary>What the button says.</summary>
    public const string PutItToThemLabel = "Put it to them";

    /// <summary>
    /// WHAT PUTTING IT TO THEM IS. #746's papers-on-the-table grammar, in a room with a door on it: you have
    /// the spread out in front of you and you turn it round.
    ///
    /// <para>Nothing is granted and nothing is signed — v1 is deliberately the GESTURE (#760's standing and
    /// contract negotiation is v2, and is named as such on the issue). What it actually changes is that the
    /// dig at this table files under the person opposite (<see cref="DugAgainstThem"/>), which is #715's
    /// per-entity memory arriving as a consequence rather than as a meter.</para>
    ///
    /// <para><b>NEW PROSE, flagged for the owner to bless.</b></para>
    /// </summary>
    public const string PutItToThemLine =
        "You turn the papers round on the table and leave them there. Whoever is opposite reads the top "
        + "sheet the whole way down without touching it, and then reads it again, and does not put a hand "
        + "out. \"I'll take that as said,\" they say, which is not the same as taking it.";

    /// <summary>The scene the strip is holding when somebody is opposite. Asked by NAME rather than by
    /// counting buttons, so a client can tell the two rooms apart without a field of its own.</summary>
    public const string DelegationSceneId = "ring:booked:delegation";

    /// <summary>…and the same room with nobody in it.</summary>
    public const string EmptySceneId = "ring:booked:empty";

    /// <summary>Is this sitting one of the two the booked room opens?</summary>
    public static bool IsABookedSitting(string? sceneId) =>
        string.Equals(sceneId, DelegationSceneId, StringComparison.Ordinal)
        || string.Equals(sceneId, EmptySceneId, StringComparison.Ordinal);

    /// <summary>What the counterpart's book keeps of a dig done in front of them — the machine-readable half,
    /// under their own entity. A note about a case, filed under the person it was put to.</summary>
    public static string DugAgainstThem(int room, long watch) =>
        $"{BookTag}-worked:{room}:{watch}";

    /// <summary>
    /// #770 · THE BOOKED TABLE WITH SOMEBODY AT IT — an <see cref="Encounter.Scene"/> and therefore the SAME
    /// docked strip every other seat in this game runs on (#865).
    ///
    /// <para>The three social moves are <see cref="CanteenTable"/>'s own ids and not new ones: a delegation
    /// is people at a table, and small talk at a table is small talk at a table however much the room cost.
    /// What is this room's is the FOURTH button, and the fact that there is a door.</para>
    /// </summary>
    /// <param name="plate">Who is opposite.</param>
    public static Encounter.Scene TheDelegationTable(string plate) => new(
        DelegationSceneId,
        plate,
        Setting,
        TheyAreSeatedOppositeLine(plate),
        [
            new(CanteenTable.SmallTalk, CanteenTable.LabelOf(CanteenTable.SmallTalk)),
            new(CanteenTable.Round, CanteenTable.LabelOf(CanteenTable.Round),
                Encounter.Requirement.Credits, CanteenTable.RoundPrice),
            new(PutItToThem, PutItToThemLabel, Says: PutItToThemLine),
            new(CanteenTable.Leave, CanteenTable.LabelOf(CanteenTable.Leave), Says: CanteenTable.LeaveLine),
        ]);

    /// <summary>…and the same table with nobody across it. Wait and stand, the two moves every lone seat in
    /// the game has, on the same ids (<see cref="SittingAlone.Wait"/>/<see cref="SittingAlone.Stand"/>) so a
    /// saved game and a guard both keep working.</summary>
    public static Encounter.Scene TheEmptyRoom() => new(
        EmptySceneId,
        EmptyRoomPlate,
        Setting,
        RoomIsYoursAndEmptyLine,
        [
            new(SittingAlone.Wait, RingOffice.WaitLabel),
            new(SittingAlone.Stand, RingOffice.StandLabel, Says: RingOffice.StoodUpLine),
        ]);

    /// <summary>Every sentence this file can put on a screen, for the canon sweep. It walks EVERY watch, so
    /// both halves of the counter's forked note are seen — <see cref="CabinetPrivacy.AllProse"/>'s own
    /// anti-vacuous idiom.</summary>
    public static IEnumerable<string> AllProse()
    {
        for (long watch = 0; watch < CanteenRegulars.WatchFill.Count; watch++)
        {
            yield return WhoBookedNote(1, watch);
            yield return DoggedTheDoorNote(1, watch);
            yield return BookedPlate(watch, true);
            yield return BookedPlate(watch, false);
        }
        yield return BookLabel(true);
        yield return BookLabel(false);
        yield return BookHint(true);
        yield return BookHint(false);
        yield return NothingToBookLine;
        yield return NoRoomOfThatKindLine(true);
        yield return NoRoomOfThatKindLine(false);
        yield return ShortLine(Price);
        yield return AlreadyHeldLine(1);
        yield return TookItLine(1, true);
        yield return TookItLine(1, false);
        yield return Setting;
        yield return EmptyRoomPlate;
        yield return RoomIsYoursAndEmptyLine;
        yield return TheyAreSeatedOppositeLine("◈ A CARRIER, WAITING ON A SIGNATURE");
        yield return PutItToThemLabel;
        yield return PutItToThemLine;
        yield return BarkThatKnows(1);
    }
}
