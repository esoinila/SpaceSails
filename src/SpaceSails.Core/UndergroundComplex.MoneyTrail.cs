using System;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #1074 beat 3 · THE MONEY TRAIL, IN THE BUILDING ──────────────────────────────────────────────────
    //
    // Three papers, dealt by the idiom this building has dealt five authored papers with already: a
    // DESIGNATED room, and the reason is the reason it has been every time (KeyRoomFor's own argument). A
    // seeded one-in-nine paper is a paper that is silently absent forever on some worlds with nothing on
    // screen ever saying so, and a money trail with a hole in it is not a money trail.
    //
    // WHERE THEY ARE, AND WHY IT IS THE FLOOR THE FACILITY WORKS ON. These are ACCOUNTS — a purchase, a
    // quantity, a cost centre — which is the output of a person sitting at a desk with a pen, and #608's
    // ruling puts that person on a floor that breathes. TopPressurisedFloor is where this building keeps its
    // paperwork and it is where #1063's maintenance ledger is already kept; the two can never meet, because
    // a ground is stopped or buried and never both (StopOrder.TheOfficeGetsThisOne), and in any case that
    // one takes room 0 and these take rooms 1, 2 and 3.
    //
    // NOT ROOM 0, AND THAT IS A RULE RATHER THAN AN ARRANGEMENT: a paper in room 0 would be a paper the
    // first search on the floor is guaranteed to turn up, and a money trail that hands itself over on the
    // first press is a briefing. The narrowest works floor in the swept family carries FOURTEEN searchable
    // rooms — measured rather than remembered, and asserted by TheMoneyTrailTests to leave room for the
    // highest index this file names — so three designations still leave a captain a building to turn over.
    //
    // NOTHING ELSE CHANGES. There is no new haul face, no new pocket line, no card, no pulse of its own and
    // no beat: the room reads as operational paper because operational paper is exactly what it is, and the
    // only thing that is new is the SENTENCE on it and the two names the book files that sentence under.

    /// <summary>#1074 · The room on the works floor that holds the pour's line item. Room 1 — see the note
    /// above on why nothing here is ever room 0.</summary>
    public const int MoneyTrailPourRoom = 1;

    /// <summary>#1074 · …the rail's.</summary>
    public const int MoneyTrailRailRoom = 2;

    /// <summary>#1074 · …and the rota's.</summary>
    public const int MoneyTrailRotaRoom = 3;

    /// <summary>#1074 · WHICH ROOM ON THIS GROUND HOLDS ONE ITEM'S PAPER, or null where this ground never
    /// bought that thing.
    ///
    /// <para><b>The pour is dealt on any STOPPED ground</b> — the remediation is what closed the working, so
    /// a closed working is exactly the site that has one on its books. <b>The rail and the rota are dealt
    /// only on a PRESERVED one</b>, because you cannot invoice a perimeter rail for a site that has no
    /// perimeter and you cannot roster a watch on a fence nobody built (<see cref="MoneyTrail.NeedsTheFence"/>
    /// is where that rule is stated, beside the purchases it is about).</para>
    ///
    /// <para><b>A fenced site is asked BOTH questions and that is not redundant.</b> Official care only ever
    /// arrives on a working somebody already closed (<see cref="PreservationZone.Note"/>'s second condition),
    /// so on every world the game produces the two answers agree — but the registers are installed from a
    /// save by a caller that could hand over either list, and "no line item on a ground nobody stopped" is a
    /// law this beat has to keep against the world it is GIVEN rather than against the world it expects.
    /// Asking the stop register here costs one walk of a list that is almost always empty and makes the law
    /// true by construction.</para></summary>
    public static (int Level, int RoomIndex)? MoneyTrailRoomFor(string bodyId, MoneyTrail.Item item)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (!StopOrder.On(bodyId))
        {
            return null;
        }
        if (MoneyTrail.NeedsTheFence(item) && !PreservationZone.On(bodyId))
        {
            return null;
        }
        if (TopPressurisedFloor(bodyId) is not { } works)
        {
            return null;   // a site with no floor that breathes keeps no accounts, which is the honest answer
        }

        return (works, item switch
        {
            MoneyTrail.Item.Rail => MoneyTrailRailRoom,
            MoneyTrail.Item.Rota => MoneyTrailRotaRoom,
            _ => MoneyTrailPourRoom,
        });
    }

    /// <summary>#1074 · <b>WHAT LINE ITEM IS IN THIS ROOM</b>, or null where the room holds none — which is
    /// every room of every site in almost every world.
    ///
    /// <para>Asked of the room rather than answered by three separate lookups at the call sites, for
    /// <see cref="IsSealedWay"/>'s reason: the client meets a room as a level and an index and must never be
    /// the place that decides what one of them HOLDS.</para></summary>
    public static MoneyTrail.Item? MoneyTrailPaperIn(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        foreach (MoneyTrail.Item item in TheItems)
        {
            if (MoneyTrailRoomFor(bodyId, item) is { } at
                && at.Level == level && at.RoomIndex == roomIndex)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>#1074 · The three, in the order a site buys them: the concrete that closed the working, the
    /// fence that went round it a shift later, and the watch that keeps the fence. A private array rather
    /// than <c>Enum.GetValues</c>, which allocates on every room search on every floor.</summary>
    private static readonly MoneyTrail.Item[] TheItems =
        [MoneyTrail.Item.Pour, MoneyTrail.Item.Rail, MoneyTrail.Item.Rota];

    /// <summary>#1074 · What the captain reads in a room that holds one, behind the paper glyph every
    /// operational-paper line in this building already wears.
    ///
    /// <para><b>Composed exactly as <see cref="MaintenanceLedgerLine"/> and <see cref="PlantValveBookLine"/>
    /// are</b> — the building's own glyph and the authored sentence, and not one word besides. There is no
    /// framing, no "you find", no total and no remark: the whole paper is one line item, which is what a
    /// line item is.</para></summary>
    public static string MoneyTrailLine(MoneyTrail.Item item) => PaperGlyph + MoneyTrail.TextOf(item);

    /// <summary>#1074 · The glyph the building's papers are read behind — the same one
    /// <see cref="MaintenanceLedgerLine"/> and <see cref="PlantValveBookLine"/> already carry, named here
    /// once rather than typed a fourth time.</summary>
    private const string PaperGlyph = "\U0001F4CB ";

    /// <summary>#1074/#741 · What the BOOK files this room's find under, or the empty line for every other
    /// room in the game — which is what <c>FileNoteAbout</c> already means by "about nothing the game has
    /// named".
    ///
    /// <para>Core answers this and not the client, for <see cref="CaseSubjects"/>'s own law: a subject comes
    /// from the AUTHOR of the sentence, and the author of this sentence is this file.</para></summary>
    /// <param name="siteName">What the ground the captain is standing on is called, as the game prints
    /// it.</param>
    public static string MoneyTrailSubjectsFor(string bodyId, int level, int roomIndex, string? siteName)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return MoneyTrailPaperIn(bodyId, level, roomIndex) is null
            ? ""
            : MoneyTrail.SubjectsFor(siteName);
    }

    // #1074 · WHAT A LINE ITEM IS CALLED AWAY FROM ITS ROOM is answered now and is not answered here. The
    // canon pass of 2026-09-03 wrote a title and a one-line body for each of these three and for the two
    // clerical books beside them, and the five heads are listed together in PaperHeads because what joins
    // them is the SEAM they are read through — FieldClue.Title and FieldClue.Document, the sleeve's row, the
    // free glance and the line the book keeps about what was left behind — rather than the beat that dealt
    // them. Which find id is which paper is this building's question and AuthoredPaperOf answers it off the
    // three room designations already written above; nothing about a paper is composed in a second place.
}
