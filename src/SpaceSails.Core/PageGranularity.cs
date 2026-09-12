using System;

namespace SpaceSails.Core;

/// <summary>
/// #798 item 2 · PAGE GRANULARITY — rip out the worst page, toss the rest.
///
/// <para>Owner, second refinement on the disposal tradecraft (2026-08-09): <i>"a compromising FILE is not
/// uniform; the owner wants to 'rip out the most compromising evidence and toss the rest inconspicuously.'
/// So a multi-page document can be SPLIT: keep/destroy per page — pocket the one damning sheet (small,
/// hideable, the photograph already in the book) and bin the innocent bulk, which is ALSO cover (a file in
/// the bin that reads boring explains itself; a missing file explains nothing). The processing UI (#784) is
/// where the split happens — seated, with air."</i></para>
///
/// <h3>The whole idea, in one sentence</h3>
/// <para><b>A file in the bin explains itself; a missing file explains nothing.</b> #798's first cut gave
/// the captain one lever — destroy the document or keep it — and both ends of that lever are loud. Tear up
/// a personnel file and the folder is gone off a shelf somebody checks; carry it and it is in your coat at
/// the gate. The split is the third answer, and it is the one an actual professional takes: the file goes in
/// the bin looking exactly like a file anyone would bin, minus the one sheet that was ever worth anything,
/// which is now folded twice in a pocket.</para>
///
/// <h3>What is a page, here</h3>
/// <para>Nothing in this game has ever modelled a document as pages, and this file does not start storing
/// them either. <see cref="PagesIn"/> and <see cref="WorstPageOf"/> are <b>rolled off the document's own
/// id</b>, exactly the way <see cref="FieldClue.Title"/>, <see cref="FieldClue.Document"/> and
/// <see cref="FieldClue.CertaintyOf"/> already are — so the page count is a property of the sheet rather
/// than a number a save carries, the same sheet is the same length in every session, and a vault written by
/// an older build needs no migration to have known it all along.</para>
///
/// <h3>And the split leaves NO state either</h3>
/// <para>A split paper becomes two papers whose ids carry the fact: <see cref="TornPageTag"/> on the sheet,
/// <see cref="TornRestTag"/> on the bulk. Both still name the document they came out of
/// (<see cref="SourceOf"/>), which is what lets the title, the body and the certainty stay the DOCUMENT's
/// rather than being re-rolled into two different papers — and it is what makes <b>"has this been split?"</b>
/// a question about the thing in your hand rather than a set somebody has to remember to save. There is no
/// register of split documents anywhere in this game and there must never be one: a second place holding
/// this fact is this repo's fifth named bug class with a folder in its hand.</para>
///
/// <para>Pure and deterministic, like everything else in Core: seeded off <see cref="DiceRule"/>, never
/// <c>Random</c>, never a clock. It has never heard of a satchel, a table or a bin.</para>
/// </summary>
public static class PageGranularity
{
    // ── HOW LONG A DOCUMENT IS ───────────────────────────────────────────────────────────────────────────

    /// <summary>The longest a seeded document gets. Four sheets is a folder you would have to sit down with,
    /// and it is deliberately small: the split is a piece of tradecraft, not an inventory game about
    /// paper.</summary>
    public const int LongestDocument = 4;

    /// <summary>
    /// HOW MANY SHEETS THIS DOCUMENT IS.
    ///
    /// <para>Weighted so a third of the paper in this game is a single sheet with nothing to split — the
    /// verb has to be a property of the document a captain discovers, rather than a button that is always
    /// there. Rolled on the house die off the document's own id, so the same sheet is the same length in
    /// every session and on every machine.</para>
    ///
    /// <para><b>An AUTHORED sheet is one sheet</b> (<see cref="FieldClue.IsAuthored"/>). The arc wrote those
    /// as a page — a rate schedule, the five out of the underground's designated rooms, the sheet with the
    /// lift code on it — and a captain tearing "page 3 of 4" out of a document whose every word was composed
    /// as one page would be the sim doing one thing while the prose said another, which is this repo's third
    /// named bug class. It is also what keeps the split from ever touching the sheets other systems look for
    /// BY ID: the code paper is read out of the sleeve by <c>LiftCode.PaperIn</c>, and a split id is not the
    /// id it is looking for.</para>
    /// </summary>
    public static int PagesIn(string paperId)
    {
        ArgumentNullException.ThrowIfNull(paperId);

        string source = SourceOf(paperId);
        if (FieldClue.IsAuthored(source))
        {
            return 1;
        }

        return DiceRule.Roll(DiceRule.Seed($"pages:count:{source}"), 6).Face switch
        {
            1 or 2 => 1,
            3 or 4 => 2,
            5 => 3,
            _ => LongestDocument,
        };
    }

    /// <summary>Is there anything in this document to split? Two sheets is the whole of the requirement, and
    /// it is asked of <see cref="PagesIn"/> rather than spelled at a call site, because "a document with
    /// more than one page in it" is a law and not an arithmetic anybody should be retyping.</summary>
    public static bool IsMultiPage(string paperId) => PagesIn(paperId) >= 2;

    /// <summary>
    /// WHICH SHEET IS THE ONE WORTH TEARING OUT — the most compromising page, 1-based.
    ///
    /// <para>Its own seed stream, so the worst page and the page count never move together: a document that
    /// grew a page would otherwise quietly shift which sheet was damning, and the two facts have nothing to
    /// do with each other. Always inside the document (<see cref="DiceRule.Roll"/> over the page count), so
    /// there is no such thing as the worst page of a sheet that is not there.</para>
    ///
    /// <para>A single-page document's worst page is page one, and that is not a special case: on a one-sheet
    /// document the worst page IS the document, which is exactly why there is nothing to split.</para>
    /// </summary>
    public static int WorstPageOf(string paperId)
    {
        int pages = PagesIn(paperId);
        return DiceRule.Roll(DiceRule.Seed($"pages:worst:{SourceOf(paperId)}"), pages).Face;
    }

    // ── WHAT A SPLIT DOCUMENT IS ─────────────────────────────────────────────────────────────────────────

    /// <summary>What is left of a document after the worst page came out of it — the folder that goes in the
    /// bin. One less than <see cref="PagesIn"/> and never negative, because a one-sheet document is never
    /// split in the first place.</summary>
    public static int BulkPagesIn(string paperId) => Math.Max(0, PagesIn(paperId) - 1);

    /// <summary>Which part of a document a satchel row is.</summary>
    public enum Part
    {
        /// <summary>The document, entire — every sheet of it, still together.</summary>
        Whole,

        /// <summary>The one page that was worth keeping. Folded twice, and the book already has its gist —
        /// which is the precondition of the split and not a hope about it.</summary>
        TheSheet,

        /// <summary>What is left: the folder anyone would be bored to find, on its way to a bin.</summary>
        TheBulk,
    }

    /// <summary>The mark a torn-out sheet's id wears. Deliberately unmistakable and deliberately not a word
    /// any generator in this game has ever put in front of an id (they read <c>hive:&lt;body&gt;:&lt;level&gt;:&lt;n&gt;</c>
    /// and their kin), because <see cref="PartOf"/> reads the front of an id and a collision there would
    /// silently reinterpret a whole document as half of one.</summary>
    public const string TornPageTag = "torn-page:";

    /// <summary>…and the mark the bulk wears, for the same reason.</summary>
    public const string TornRestTag = "torn-rest:";

    /// <summary>Which part of a document this id names.</summary>
    public static Part PartOf(string? id) =>
        id is null ? Part.Whole
        : id.StartsWith(TornPageTag, StringComparison.Ordinal) ? Part.TheSheet
        : id.StartsWith(TornRestTag, StringComparison.Ordinal) ? Part.TheBulk
        : Part.Whole;

    /// <summary>
    /// THE DOCUMENT AN ID CAME OUT OF — the same id back for anything that was never split.
    ///
    /// <para>This is the one function that keeps a split from becoming two new documents. The title, the
    /// body, the certainty and the field book's own register are all rolled or keyed off the document's id,
    /// and every one of them asks this first: a page torn out of a pay sheet is still that pay sheet's page,
    /// and a folder with a sheet missing is still that folder. Re-rolling either off its new id would hand
    /// the captain a shipping manifest that came apart into two unrelated documents.</para>
    /// </summary>
    public static string SourceOf(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return PartOf(id) switch
        {
            Part.TheSheet => id[TornPageTag.Length..],
            Part.TheBulk => id[TornRestTag.Length..],
            _ => id,
        };
    }

    /// <summary>The id the torn-out sheet carries.</summary>
    public static string SheetIdOf(string paperId) => TornPageTag + SourceOf(paperId);

    /// <summary>…and the id the bulk carries.</summary>
    public static string BulkIdOf(string paperId) => TornRestTag + SourceOf(paperId);

    // ── WHEN IT MAY BE DONE ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// MAY THIS DOCUMENT BE SPLIT, HERE, NOW?
    ///
    /// <para>Four clauses and every one of them is the owner's: it is <b>paper</b> (a handful of rounds has
    /// no pages and an authority is a door, not evidence); it is <b>whole</b> (a document comes apart once —
    /// and because the parts wear their own ids, that law needs no state to enforce and cannot be defeated
    /// by binning one half); it is <b>already in the book</b> (the owner's own precondition: <i>"the
    /// photograph already in the book"</i> — tearing a document up before the dig throws away the bulk's
    /// pages unread, which #828's two-tier reading law spent a whole issue making impossible to do by
    /// accident); and it is <b>seated at a table you may spread the case on</b> (<i>"the processing UI is
    /// where the split happens — seated, with air"</i>).</para>
    ///
    /// <para><paramref name="seatedForTheSpread"/> is the caller's own fact about a seat, asked of
    /// <see cref="SeatedSpread.RefusalAt"/> and never re-decided here: what a seat is, and who is sitting at
    /// it, is a question about a room and this file does not have one.</para>
    /// </summary>
    public static bool CanSplit(
        Satchel.Kind kind, string paperId, bool alreadyInTheBook, bool seatedForTheSpread)
    {
        ArgumentNullException.ThrowIfNull(paperId);
        return kind == Satchel.Kind.Paper
            && PartOf(paperId) == Part.Whole
            && alreadyInTheBook
            && seatedForTheSpread
            && IsMultiPage(paperId);
    }

    // ── THE WORDS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The verb's glyph, and what the control on the spread wears.</summary>
    public const string Glyph = "✂";

    /// <summary>
    /// WHAT IS SAID AS IT COMES APART — once, when it happens, in the captain's own flat register.
    ///
    /// <para>Fable-authored for #798 item 2, and the only sentence this feature owns. It says both halves of
    /// the tradecraft in one breath: what you keep is small enough to go where the book goes, and what you
    /// are about to put in a bin is boring — which is the entire reason it is going in a bin rather than
    /// going missing.</para>
    /// </summary>
    public const string SplitLine =
        "One sheet, folded twice, goes where the book goes. What is left is a folder anyone would be bored "
        + "to find.";

    /// <summary>
    /// WHAT THE BOOK KEEPS WHEN THE BULK GOES IN A BIN — the flavour of
    /// <see cref="RipAndBin.DisposalNote"/> for a split file.
    ///
    /// <para>Fable-authored for #798 item 2. The ordinary note ends <i>"Nothing of it was left on the
    /// table"</i>, which is the captain saying they tidied up after themselves; this one is the captain
    /// saying the thing they left behind looks like nothing, which is a different and much better answer.
    /// It is the tradecraft written down where a later arc can read it back — see
    /// <see cref="RipAndBin.LeftInTheBin"/> for the ladder it belongs to.</para>
    /// </summary>
    public const string BulkDisposalClause =
        "A file about nothing much, in a bin, where files about nothing much go.";

    /// <summary>
    /// THE PAGE CITATION A SPLIT ROW WEARS after the document's own title, and the empty string for a
    /// document that is still whole.
    ///
    /// <para>It is a CITATION and not a sentence: the satchel row already says what the document is called
    /// (<see cref="FieldClue.Title"/>) and how well it pins a place, and what a captain holding two rows off
    /// one file needs is which of them is the page and which is the rest. Both halves of a split say how
    /// long the document was, because "page 3 of 4" and "3 pages of 4" are the same fact read from the two
    /// sides and a captain should not have to hold one row up against the other to work it out.</para>
    /// </summary>
    public static string RowCitation(string paperId)
    {
        ArgumentNullException.ThrowIfNull(paperId);
        int pages = PagesIn(paperId);
        return PartOf(paperId) switch
        {
            Part.TheSheet => $", page {WorstPageOf(paperId)} of {pages}",
            Part.TheBulk => $", {BulkPagesIn(paperId)} pages of {pages}",
            _ => "",
        };
    }

    /// <summary>Every authored sentence this feature owns, for the canon sweep — #709's discipline, and the
    /// reason a catalog is a method rather than a list that goes stale.</summary>
    public static System.Collections.Generic.IEnumerable<string> AllProse()
    {
        yield return SplitLine;
        yield return BulkDisposalClause;
    }
}
