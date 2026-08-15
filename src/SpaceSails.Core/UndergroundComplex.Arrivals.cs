using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>#585 · The card the first descent earns. Owner: "I think we need to gen AI pop-up about
    /// finding the elevator" — and he is right that it is the beat of the whole feature: the moment a moon
    /// stops being a field with things on it and becomes a lid.</summary>
    public const string DescentArtUrl = "art/the-descent.jpg";

    public const string DescentCardLabel = "🛗 THE SHAFT";

    /// <summary>What the card says beside the picture. Scale, and the cost of digging it — never a word about
    /// what it was for.</summary>
    public const string DescentCard =
        "The gate rattles down and the car starts, and it does not stop starting.\n\n" +
        "Service lamps go past in the wall at first, then a rhythm, and you find you have been counting " +
        "them and have lost count. The shaft is LINED. Somebody cut this out of a moon and then finished " +
        "it: poured walls, bolted rails, lamps on a circuit that is somehow still live.\n\n" +
        "Nobody does this quietly. A hole this deep is surveyed, funded, staffed and inspected; it has " +
        "invoices, and a schedule, and a name on a form somewhere. And yet the only thing above it is a " +
        "shed with a maintenance plate, on a moon with no register entry, on nobody's chart.\n\n" +
        "The car keeps going down. You have time to think about that, and you would rather not.";

    // ── #411 · AND THE OTHER ONE. The first descent at the head office is not the same beat as the first
    //    descent at a branch office, and giving it the same card would be the loudest missed opportunity in
    //    the arc: the whole ruling is that a captain who has crawled a Hive should recognise the rank on
    //    sight. So the establishing shot is its own, and it is built out of the same four things the Hive's
    //    is — a shaft, a directory, a lobby, a floor — with every one of them answered differently.
    //
    //    Discipline, harder here than anywhere: EVIDENCE, then stop. The card may say the lamps come up
    //    ahead of the car. It may not say who turned them on.

    public const string HeadOfficeArrivalArtUrl = "art/kaamos-head-office.jpg";

    public const string HeadOfficeArrivalLabel = "🧊 THE HEAD OFFICE";

    /// <summary>The first descent at the head office, said once. Four paragraphs, and not one of them tells
    /// the captain what any of it means.</summary>
    public const string HeadOfficeArrivalCard =
        "The car does not go down a shaft so much as down a BUILDING.\n\n" +
        "Service lamps go past in the wall at first, the way they do everywhere. Then the shaft opens out " +
        "and the lamps stop being service lamps: they are lobby lighting, warm and even, and they come up " +
        "ahead of the car and go down behind it.\n\n" +
        "The doors part on a floor built to receive people. Stone facing around the lift surround. A bench. " +
        "A rack for coats with nothing on it. And a directory beside the doors that lists TWENTY-FOUR floors " +
        "— all of them, none of them abbreviated, none of them missing.\n\n" +
        "There is no dust on the floor. Not undisturbed dust. None.";

    /// <summary>Which establishing card this building's first descent earns — asked in one place so the two
    /// can never be shown for the wrong building.</summary>
    public static (string Label, string ArtUrl, string Card) FirstDescentCard(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHeadOffice(bodyId)
            ? (HeadOfficeArrivalLabel, HeadOfficeArrivalArtUrl, HeadOfficeArrivalCard)
            : (DescentCardLabel, DescentArtUrl, DescentCard);
    }

    // ── #411 · THE THREE FLOORS WITH A BEAT ON THEM ──────────────────────────────────────────────────
    //
    // Every one of these is EVIDENCE and stops. Between them they say that somebody set an enormous thing
    // going, that nobody ever stopped it, and that it is still going. Not one of them says what it is, who
    // is doing it, or what any of it means — canon holds hardest exactly here, because this is the deepest
    // and most tempting room in the game.

    /// <summary>B12 · the sheet in the folder. Per #614's law a card may say WHAT and never WHERE: this
    /// names an instruction and an unused countersignature block, and no place at all.</summary>
    public const string StandingOrderLine =
        "📋 One countersigned sheet in a folder with nothing else in it: the runs are to continue UNTIL " +
        "COUNTERMANDED. Underneath, a countersignature block — ruled, printed, and never used. The folder has " +
        "been opened often enough to wear the crease through, and closed again every time.";

    public const string WinteringHallArtUrl = "art/kaamos-wintering-hall.jpg";

    public const string WinteringHallLabel = "❄❄ FORTY-ONE";

    /// <summary>B23. The room this arc was written for, and the only card in the game that is allowed to do
    /// arithmetic — because counting is a thing a captain does with their own eyes, and the count is the
    /// whole beat. It still never says whose the last one is.</summary>
    public const string WinteringHallCard =
        "The floor is one room, and the room does not end where the lamps do.\n\n" +
        "Four rows of ten, and every one of them is MADE. Not stripped, not stacked, not sheeted over for a " +
        "shutdown: made. The blanket turned back at the same angle on all forty. The pillow squared. Along " +
        "one side the wall is glass a hand thick and behind it there is black water going down further than " +
        "the lamps reach.\n\n" +
        "At the end of the fourth row, apart from the others by the width of a walkway, there is one more. " +
        "Turned back at the same angle. Squared.\n\n" +
        "Forty-one.";

    /// <summary>Said on the pulse line as well, because the card is dismissed and the log is not.</summary>
    public const string WinteringHallLine =
        "❄ You count them twice, from the far end the second time, because the first answer was not the one " +
        "you expected.";

    /// <summary>Why the nerve goes. Deliberately does not state the arithmetic — the captain has just done it.</summary>
    public const string WinteringHallShockReason =
        "a room that has been kept made up for a very long time, and the count in it";

    public const string BerthOfficeArtUrl = "art/kaamos-berth-office.jpg";

    public const string BerthOfficeLabel = "❄ ONE LINE STILL LIT";

    /// <summary>B24. The last floor, and the smallest room on it. It is the only untidy room in the building
    /// — and it is untidy with its own output, which is the tidiest possible reason.</summary>
    public const string BerthOfficeCard =
        "One console, one board, one line lit.\n\n" +
        "It is the only room in this building that is not immaculate, and it is knee-deep. The log has been " +
        "printing continuously and folding itself onto the floor, and nobody has emptied it, because " +
        "emptying it is not a thing anybody ever wrote down.\n\n" +
        "The entries are a requisition against a berth at Ringside Exchange, filed on every cycler window, " +
        "on the tick. The acknowledgement column beside them is blank for so far back that the form has " +
        "changed twice inside the drift.\n\n" +
        "The newest sheet is still warm. Under it, queued and dated, is the next one.";

    /// <summary>What the lift says as it starts down. The one beat of scale before any of the plan is drawn.</summary>
    public const string DescendingLine =
        "🛗 The car takes a moment to decide you are allowed, and then it drops. It keeps dropping. Whatever " +
        "this was, nobody dug it in an afternoon and nobody paid for it out of pocket.";

    /// <summary>#592 · Said ONCE, on stepping out onto the first floor the building never admitted to.
    ///
    /// <para>The whole beat of the feature, and the hardest place in the game to hold the canon line. It may
    /// say that the operation upstairs was enormous, funded, staffed and inspected, and that this was under
    /// it, and that the people who worked upstairs did not know. It may not say what it was for. The captain
    /// gets the arithmetic and never the answer — and if they want one, the files are in the rooms and the
    /// files are about PEOPLE.</para></summary>
    public static string UnlistedArrivalLine(int floorsAbove, Kind above, Kind here) =>
        $"🕳 The doors part on a floor that is not on the plan in the lobby.\n\n" +
        $"{floorsAbove} storeys of {TitleOf(above).TrimStart('▣', ' ').ToLowerInvariant()} over your head — " +
        "surveyed, funded, staffed, inspected, invoiced. Every one of those floors had a number and a " +
        "department and a plate beside the lift. This one has a lift and no plate.\n\n" +
        $"And the doors down here do not read like the doors up there. They read like " +
        $"{TitleOf(here).TrimStart('▣', ' ').ToLowerInvariant()}.\n\n" +
        "Somebody dug a second shaft, off the directory, to serve four floors that the people working " +
        "upstairs went home every night without knowing were under them. That is not secrecy from an enemy. " +
        "That is secrecy from your own staff, and it costs more.";

    /// <summary>What a floor with no plate calls itself when the captain looks for a name.</summary>
    public const string UnlistedFloorLine =
        "🕳 No plate by the lift, no department, no number painted anywhere. The building has floors it " +
        "does not count, and you are standing on one.";

    // ── #693 · WHAT THE DOORS OPENING HAS TO SAY, AND IN WHAT ORDER OF IMPORTANCE ────────────────────────
    //
    // An arrival on a floor can have five things to say at once: the car dropped, the plan has no such floor,
    // the air is good or gone, a gate read a paper, the pour stopped. The HUD's pulse has ONE slot, and until
    // #693 the rule was "last write wins" — so which of the five a player actually read was decided by the
    // order three separate blocks in a razor file happened to be written in. Three of those blocks carried a
    // comment explaining that they were deliberately last. #592's climax — the first words on a floor that
    // does not exist, the biggest sentence in the feature — was not one of them, and had been eaten by the
    // routine pressurisation line since the day it shipped.
    //
    // So the arrival composes here, once, with a RANK on each saying, and PulseSlot's law picks the winner.
    // The list is still in narrative order, because the BOOK keeps every one of them and reads top to bottom;
    // but nothing depends on that order any more, which is the whole point and is guarded as such (the house
    // bug class: a list built by appending is not a list in order).
    //
    // What stays in the client: the cards, the nerve, the flags, the save. Those are effects on a world Core
    // does not have. This is the SAYING, and a saying is prose with a rank on it.

    /// <summary>#693 · Which of the arrival's sayings this is, so the client can hang the effects that belong
    /// to it — a card, a nerve shock, a flag — off the one list rather than re-deciding the conditions.</summary>
    public enum ArrivalBeat
    {
        /// <summary>The car left the surface and kept going. The one beat of scale before any plan is drawn.</summary>
        Descending,

        /// <summary>#592 · The doors part on a floor the lobby's plan does not have.</summary>
        Unlisted,

        /// <summary>#609 · The first dead floor of an excursion — the one that also stops the world with a
        /// card, because whether you can breathe here is not a toast.</summary>
        DeadAirFirst,

        /// <summary>#609 · Every later floor's air line, whichever way it goes.</summary>
        Air,

        /// <summary>#689 · A gate read the countersignature card and the car went deeper than this shaft was
        /// ever dug to.</summary>
        CardAccepted,

        /// <summary>#752 · A gate read the day-labour chit, which is a tired man with a list and not an
        /// office that stopped answering its post.</summary>
        ChitGate,

        /// <summary>#677 · The pour stopped at a line. Said in the shaft, on the way.</summary>
        Seam,

        /// <summary>#677 · The first gallery. Four sentences, three of them measurable and the fourth an
        /// absence.</summary>
        Found,
    }

    /// <summary>#693 · One thing this arrival has to say, what to file it under, and how much it matters.</summary>
    /// <param name="Beat">Which saying it is (the client hangs its effects off this).</param>
    /// <param name="Text">The prose, ready to say and to file.</param>
    /// <param name="Glyph">The book's own column mark for it.</param>
    /// <param name="Rank">Who wins the one pulse slot. See <see cref="PulseRank"/>.</param>
    /// <param name="Gate">The card the gate read, on <see cref="ArrivalBeat.CardAccepted"/> and nowhere
    /// else. Carried on the saying so the caller can mark the shaft as narrated without spelling out a
    /// second time which band a ride ended in — that arithmetic has already been done once, above.</param>
    public readonly record struct Saying(
        ArrivalBeat Beat, string Text, string Glyph, PulseRank Rank, AuthorityCard? Gate = null);

    /// <summary>#693 · What this excursion has already heard, so a beat that is said once is said once.
    ///
    /// <para>Every field is a fact the client already keeps on its <c>SurfaceExcursion</c>; passing them in
    /// rather than re-deriving them keeps the once-ness and the SAYING in one place, which is the thing that
    /// had drifted.</para></summary>
    /// <param name="WasUnderground">Whether the car started below the surface.</param>
    /// <param name="FirstSightOfThisFloor">Whether this excursion has stood on this floor before.</param>
    /// <param name="VacuumWarned">Whether the dead-air card has already been spent this excursion.</param>
    /// <param name="UnlistedSeen">Whether #592's climax has already been said this excursion.</param>
    /// <param name="ChitBeatSpent">Whether the chit's gate has already been narrated this excursion.</param>
    /// <param name="SeamCrossed">Whether the pour's edge has already been narrated this excursion.</param>
    /// <param name="FoundSeen">Whether the first gallery has already been narrated this excursion.</param>
    /// <param name="ShaftsNarrated">Which bands' gates have already told their card story this excursion.</param>
    public readonly record struct ArrivalMemory(
        bool WasUnderground = false,
        bool FirstSightOfThisFloor = true,
        bool VacuumWarned = false,
        bool UnlistedSeen = false,
        bool ChitBeatSpent = false,
        bool SeamCrossed = false,
        bool FoundSeen = false,
        IReadOnlyCollection<int>? ShaftsNarrated = null);

    /// <summary>
    /// #693 · EVERYTHING THIS ARRIVAL HAS TO SAY, ranked, in narrative order.
    ///
    /// <para>The caller says all of them — the book keeps every one — and lets <see cref="PulseSlot"/> decide
    /// which is on the screen. A caller that reorders this list must get the same line on screen; that is the
    /// law, and it is what makes the ordering comments that used to live in the client unnecessary.</para>
    ///
    /// <para>Empty on a ride to the surface: coming up is the shed, the moon and what you are carrying out of
    /// it, which is one line the client composes out of the satchel it owns.</para>
    /// </summary>
    /// <param name="via">The button that was pressed, when a button was pressed. Only a ride through the
    /// panel can cross a gate — the dev floor cheat rides the same car and has no gate to cross.</param>
    public static IReadOnlyList<Saying> ArrivalSayings(
        string bodyId, int fromLevel, int toLevel, ArrivalMemory memory, LiftStop? via,
        IReadOnlyCollection<string> heldCardIds, IReadOnlyList<Satchel.Item>? carried = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(heldCardIds);

        var said = new List<Saying>();
        if (toLevel >= 0)
        {
            return said;
        }

        if (!memory.WasUnderground)
        {
            said.Add(new(ArrivalBeat.Descending, DescendingLine, "\U0001F6C3", PulseRank.Beat));
        }

        // #592 · THE FLOOR THAT IS NOT ON THE PLAN, and the reason this function exists. It says the operation
        // upstairs was enormous, funded, staffed and inspected, and that this was under it, and that the
        // people upstairs did not know. It does not say what it was for. CLIMAX, and it has never once won
        // the slot it was written for.
        if (IsUnlisted(bodyId, toLevel) && !memory.UnlistedSeen)
        {
            said.Add(new(
                ArrivalBeat.Unlisted,
                UnlistedArrivalLine(-DepthOf(bodyId), KindFor(bodyId), KindOn(bodyId, toLevel)),
                "\U0001F573",
                PulseRank.Climax));
        }

        // #677 · NEITHER AIR LINE IS SAID PAST THE SEAM. The pressurised line describes plant and somebody's
        // account decades after the last invoice; said in a gallery it would explain, in one breath, the one
        // thing that must never be explained. The authored arrival line states it once and after that the
        // gauge and the plate answer, which is what instruments are for.
        if (memory.FirstSightOfThisFloor && !IsFound(bodyId, toLevel))
        {
            bool pressurised = HoldsPressure(bodyId, toLevel);
            if (!pressurised && !memory.VacuumWarned)
            {
                // #609 · The first one is a BEAT and it comes with a card: depth is priced in air, and the
                // owner suffocated on B2 finding that out from a toast that had already faded.
                said.Add(new(ArrivalBeat.DeadAirFirst, DeadAirLine, "🫁", PulseRank.Beat));
            }
            else
            {
                // …and every one after it is the weather. Status, which is what it has always been and what
                // it was never allowed to stand on top of.
                said.Add(new(
                    ArrivalBeat.Air, pressurised ? PressurisedLine : DeadAirLine, "🫁",
                    PulseRank.Status));
            }
        }

        // #689 · THE CARD'S FINEST HOUR. A fact about the RIDE rather than about the floor, said once per
        // shaft per excursion, and the beat the owner filed an issue about never seeing.
        if (via is not null
            && GateOpenedByRidingTo(bodyId, fromLevel, toLevel, heldCardIds, carried) is { } opened
            && !(memory.ShaftsNarrated?.Contains(opened.Band) ?? false))
        {
            said.Add(new(
                ArrivalBeat.CardAccepted, CardAcceptedLine(opened), "🎫", PulseRank.Beat, opened));
        }

        // #752 · …and the other paper's arrival, which is the job finishing. A gate that reads a timesheet
        // and waves you through is the least frightening thing this building has done.
        if (via is { OpenedByChit: true } && !memory.ChitBeatSpent)
        {
            said.Add(new(
                ArrivalBeat.ChitGate, CanteenTable.ChitGateLine, CanteenTable.ChitGlyph, PulseRank.Beat));
        }

        // #677 · The seam first, because it happens in the shaft, on the way; then the arrival, because it
        // happens when the doors open. Both authored verbatim, both CLIMAX. Two climaxes in one arrival is
        // allowed and settled by the ordinary tie-break — among equals the last written wins — which is
        // exactly the reading order the owner wrote them in.
        if (IsFound(bodyId, toLevel))
        {
            if (!memory.SeamCrossed)
            {
                said.Add(new(ArrivalBeat.Seam, SeamLine, "\U0001F573", PulseRank.Climax));
            }
            if (!memory.FoundSeen)
            {
                said.Add(new(ArrivalBeat.Found, FoundArrivalLine, "\U0001F573", PulseRank.Climax));
            }
        }

        return said;
    }
}
