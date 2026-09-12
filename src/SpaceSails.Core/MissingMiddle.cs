using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1063 · THE MISSING MIDDLE — the note kind for a paper trail that is not there.
///
/// <para>The issue, in its own words: <i>"A slow honest raise leaves permits, complaints, invoices; a sudden
/// burial leaves rumor. The captain can hunt the paper trail that must exist under EITHER story — and finds
/// neither. That absence has a shape: an absence in the exact shape of the thing removed — and
/// finding-the-absence is filed as its own note kind."</i></para>
///
/// <h3>Why it is a KIND and not a sentence</h3>
/// <para>Every other line the field book keeps is a thing the captain <b>found</b>: a paper, a file, a wall,
/// a headline. This one is the opposite shape — it is what the captain went looking for and did not find —
/// and if it were filed behind the find glyph it would read as one more piece of paper in a stack of paper.
/// It carries its own glyph (<see cref="Glyph"/>, a dotted square: the outline of a thing that is not in
/// it), which is what a note kind IS in this book — <see cref="FieldNote.Glyph"/> is the only thing the
/// ledger card, the satchel's NOTES tab and the THREADS page all read to tell one sort of entry from
/// another. No enum was added and none is wanted: the issue's four clue tags are explicitly <i>"optional,
/// flagged, not required for the beat"</i>, and a taxonomy with one inhabitant is a table waiting to be
/// wrong.</para>
///
/// <h3>THE TRIGGER IS A THING THE CAPTAIN DOES</h3>
/// <para><b>Working the ledger</b>, on a ground the neighbours have filled in — the one paper the burial
/// leaves (<see cref="UndergroundComplex.MaintenanceLedgerRoomFor"/>), read in the room it is kept in. That
/// is the act: he has the job's only surviving record in his hands, he looks for the permit it must cite,
/// the complaint a raise this size must have drawn, the invoice somebody must have paid — and the ledger's
/// own numbering has already told him there was an instruction 2212 and that nothing on the paper names it.
/// Nothing announces the absence to him; he measures it, and the book keeps what he measured (§13.8 — the
/// game does not narrate, and a pop-up saying THE PAPER TRAIL IS MISSING would be the whole feature thrown
/// away to save a press).</para>
///
/// <para><b>ONCE PER GROUND.</b> A second identical line under one place would read as the book stuttering,
/// and the measurement is of a ground rather than of a room. The room is struck off when it is gone through,
/// so the ordinary game cannot ask twice — but a law that holds only because of somebody else's bookkeeping
/// is a law one refactor from not holding, so it is asked of the book itself.</para>
///
/// <para><b>THE BOOK NEVER LIES</b> (<see cref="Burial"/>'s own binding law). This adds an entry and removes
/// none: the ledger's own note, filed the moment the paper went into the pocket, is still there and still
/// says what it said. The absence is filed BESIDE the find and never instead of it — the find is what makes
/// the absence measurable, and a book that kept only the conclusion would be exactly the extraction #741
/// refuses.</para>
///
/// <para><b>Scully law (#672, binding).</b> The line is the captain's own hand and it concludes nothing. It
/// names three ordinary documents, says neither story accounts for them, and stops. §8's reserved word does
/// not appear and nothing here settles which reading of §10 is true — a swept list, like every prose-bearing
/// type in Core keeps (<see cref="AllProse"/>).</para>
/// </summary>
public static class MissingMiddle
{
    /// <summary>#1063 · <b>THE NOTE KIND.</b> A dotted square — an outline with nothing inside it, which is
    /// the only picture this beat has and the only one it needs. Not a glyph any other author in the game
    /// files under, so a page of the book can be read by sort and this entry is instantly not-a-paper.</summary>
    public const string Glyph = "⬚";

    /// <summary>#1063 · <b>THE LINE, IN THE CAPTAIN'S OWN HAND.</b> Authored (Fable, canon pass for slice 2),
    /// verbatim; no word of it is composed here and none may be added to it. It lists what a slow honest
    /// raise leaves, reports that neither story accounts for it, and ends on a measurement rather than on a
    /// conclusion — <i>the absence has a shape</i> is as far as this game will ever go, and going one word
    /// further is how the feature dies.</summary>
    public const string Line =
        "Looked for the paper a raise this size leaves: permit, complaint, invoice. Nothing under either "
        + "story. The absence has a shape, and I have measured it.";

    /// <summary>#1063 · Is this room the act? The maintenance ledger's own room, which only exists on a
    /// ground that has been filled in — so this is <see cref="Burial.IsFilled"/> and the designation asked
    /// together, through the one function that already owns the answer. A second copy of "the ledger lives on
    /// the works floor" here would be the mirrored constant this ground keeps paying for.</summary>
    public static bool IsTheLedgerRoom(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UndergroundComplex.MaintenanceLedgerRoomFor(bodyId) is { } ledger
            && ledger.Level == level && ledger.RoomIndex == roomIndex;
    }

    /// <summary>#1063 · Has this ground's absence already been measured? Asked of the BOOK rather than of a
    /// flag, because the book is the only witness (<see cref="Burial"/>'s own law) and a second register of
    /// what the book contains would be a second source for one fact.
    ///
    /// <para>Matched on the glyph AND the place: the kind is what makes it this note, and the place is what
    /// makes it this ground. Text is not matched on, for <see cref="CaseSubjects"/>' reason — nothing in this
    /// game decides anything by reading a note's prose back.</para></summary>
    public static bool AlreadyMeasured(IReadOnlyList<FieldNote>? book, string? place)
    {
        if (book is null || place is null)
        {
            return false;
        }
        for (int i = 0; i < book.Count; i++)
        {
            if (string.Equals(book[i].Glyph, Glyph, StringComparison.Ordinal)
                && string.Equals(book[i].Place, place, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>#1063 · <b>DOES THE CAPTAIN WRITE THIS DOWN NOW?</b> The whole condition in one place, asked
    /// at the one seam every find in the building already goes through: the act happened (he worked the
    /// ledger, on a ground somebody filled in) and the book does not already carry this ground's
    /// measurement.</summary>
    public static bool ShouldBeWritten(
        string bodyId, int level, int roomIndex, IReadOnlyList<FieldNote>? book, string? place) =>
        IsTheLedgerRoom(bodyId, level, roomIndex) && !AlreadyMeasured(book, place);

    /// <summary>#1063/#741 · <b>WHAT THE ENTRY IS ABOUT, declared by the author that wrote it</b> — never
    /// worked out afterwards from the words (<see cref="CaseSubjects"/>' first law).
    ///
    /// <para>Two subjects and never a third, and both are already printed for the captain to read: the site's
    /// own operator, which is the letterhead the missing permits and invoices would have carried and the one
    /// the rag's clipping is already filed under (<see cref="Burial.RagOffice"/>, #1052), and the ground
    /// itself, named on the plate the shuttle sets you down under. So a captain who has clipped the rag's
    /// cheerful sentence and then measured the absence finds the two stacked under one heading — the world's
    /// own account of the job and his own, side by side, with the book saying nothing over them.</para>
    ///
    /// <para>No <see cref="CaseSubjects.Person"/> is minted, because the line prints nobody's name.</para>
    /// </summary>
    /// <param name="siteName">What the ground is called, as the game prints it. Empty and null are the same
    /// answer and drop the subject rather than minting a blank heading.</param>
    public static string SubjectsFor(string bodyId, string? siteName)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return CaseSubjects.Line(
            CaseSubjects.Office(Burial.RagOffice(bodyId)), CaseSubjects.Place(siteName ?? ""));
    }

    /// <summary>#1063 · The entry itself, minted where its words and its subjects are known. The client hands
    /// it to the book through the same one door every other note goes through.</summary>
    public static FieldNote Note(string bodyId, string place, double simTime, string? siteName)
    {
        ArgumentNullException.ThrowIfNull(place);
        return new FieldNote(Line, simTime, place, Glyph, SubjectsFor(bodyId, siteName));
    }

    /// <summary>#1063 · Every player-facing string this note kind publishes — one, and it is authored. The
    /// same <c>AllProse</c> discipline every prose-bearing type in Core keeps, and the list the reserved-word
    /// sweep walks.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Line;
    }
}
