using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #798 item 2 · RIP OUT THE WORST PAGE, TOSS THE REST — the split, at the table where the case is worked.
///
/// <para>Owner, refining the disposal tradecraft the same evening he asked for the bin (2026-08-09):
/// <i>"a compromising FILE is not uniform… rip out the most compromising evidence and toss the rest
/// inconspicuously. So a multi-page document can be SPLIT: keep/destroy per page — pocket the one damning
/// sheet (small, hideable, the photograph already in the book) and bin the innocent bulk, which is ALSO
/// cover (a file in the bin that reads boring explains itself; a missing file explains nothing). The
/// processing UI (#784) is where the split happens — seated, with air."</i></para>
///
/// <h3>What was missing, and it was the middle of the loop</h3>
/// <para>#798's first cut gave the loop its last rung — <b>sit, dig, book, BIN</b> — and the bin took the
/// document ENTIRE. That is one lever with two loud ends: carry a personnel file through a gate, or tear the
/// whole thing up and leave a folder-shaped hole on a shelf somebody checks. The professional answer is
/// neither, and it is the one the owner described: the file goes in looking exactly like a file anybody
/// would bin, minus the one sheet that was ever worth anything.</para>
///
/// <h3>What this file decides, and what it does not</h3>
/// <para>Core (<see cref="PageGranularity"/>) owns how long a document is, which of its sheets is the worst,
/// what the two halves are called and when the verb is live. What is left for a client is the two facts only
/// a running world has: <b>whether this captain is in a seat they may spread the case on</b> and
/// <b>whether the book already has this document</b> — and the act itself, which is three lines on one
/// list.</para>
///
/// <h3>The one law</h3>
/// <para><b>NOTHING ANYWHERE REMEMBERS THAT A DOCUMENT WAS SPLIT.</b> The two halves carry ids that say what
/// they are, so the sleeve IS the record: there is no set on this page, nothing new in the vault, and no
/// second place that could come to disagree about whether a file has already come apart. That is not
/// housekeeping — it is what makes "a document comes apart once" enforceable after the bulk has been binned
/// and the sheet is the only half left in the world.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// #798 item 2 · IS THE SPLIT OFFERED ON THIS ROW?
    ///
    /// <para>Core's one answer (<see cref="PageGranularity.CanSplit"/>), asked with the client's two facts.
    /// Drawn only where it APPLIES rather than greyed out (#212/#603), which is the rule every other verb on
    /// this row already follows: on a single-sheet paper, on a sheet nothing has been dug out of, or on half
    /// a document that has already come apart, this is not a refusal — it is a verb about something
    /// else.</para>
    ///
    /// <para>The seat is the one clause a captain can meet with the control already in front of them, and it
    /// is the clause <see cref="SplitTheDocument"/> says OUT LOUD: the spread only draws at a table, but the
    /// chair opposite can fill while the papers are out (#784's own drama beat), and a control that vanished
    /// mid-sitting would teach nothing about why.</para>
    /// </summary>
    private bool SplitIsOffered(Core.Satchel.Item item) =>
        PageGranularity.CanSplit(
            item.Kind, item.Id, AlreadyWrittenUp(item), seatedForTheSpread: SpreadRefusal is null);

    /// <summary>#798 item 2 · What the control says before it is pressed — the price of a press is known
    /// before the press (#696's discipline, one control over). It is the act's own sentence, because this
    /// verb has exactly one outcome and no bet in it: what you will be holding afterwards is both halves of
    /// what the line describes.</summary>
    private static string SplitHint(Core.Satchel.Item item) =>
        $"{PageGranularity.SplitLine} ({PageGranularity.PagesIn(item.Id)} pages)";

    /// <summary>
    /// #798 item 2 · THE ACT — the worst page comes out, and what is left is a folder.
    ///
    /// <para>#1016 · No ground gate, deliberately: case-work verbs are SEAT-tied and never place-tied
    /// (owner ruling 2026-08-30), and this is the same evening's work as the dig one file over. What the bin
    /// keeps is its own gate, because a bucket IS a fixture in a room — and that gate is asked where the
    /// bulk is finally binned rather than here.</para>
    ///
    /// <para>ONE list is touched. <c>_fieldNotes</c>, <c>_caseThreads</c> and the seated register are not
    /// mentioned in this method and must never be: the book already holds this document (it is the
    /// precondition of the verb), and the register keys on the DOCUMENT rather than on the row
    /// (<c>WrittenUpKey</c>), so both halves are still in the book in the captain's own hand the moment the
    /// paper comes apart. Nothing goes to the ground either — #615's Leave is a different verb — and nothing
    /// is destroyed here at all: the bulk is a thing you are carrying until a bin takes it.</para>
    /// </summary>
    private void SplitTheDocument(Core.Satchel.Item item)
    {
        // The seat, said out loud (#603/#680): the one gate a captain can meet with the control in front of
        // them. Every other clause of CanSplit takes the control away instead of refusing, so a press that
        // gets past this line and still fails the law is a stale render and not a decision — it does
        // nothing, and there is nothing about it worth saying.
        if (SpreadRefusal is { } refusal)
        {
            SayItWhereTheyAreLooking(refusal);
            return;
        }
        if (!SplitIsOffered(item))
        {
            return;
        }

        var sheet = item with { Id = PageGranularity.SheetIdOf(item.Id) };
        var bulk = item with { Id = PageGranularity.BulkIdOf(item.Id) };

        // ── THE SLEEVE HAS TO HAVE ROOM FOR THE HALF THAT DID NOT FIT BEFORE ────────────────────────────
        //
        // A split is the one act in this game that turns one row into two, so it is the one act that can be
        // refused by the sleeve's own arithmetic — and Satchel.Add refuses POLITELY (it hands back the list
        // unchanged), which is exactly how #678's haul path destroyed a find for a year. So the room is
        // asked BEFORE anything moves, on the list as it will actually stand: the document is out of it, the
        // sheet is in, and the question is whether the folder still goes. The refusal is the sleeve's own
        // sentence, which already names what would fix it.
        System.Collections.Generic.IReadOnlyList<Core.Satchel.Item> less =
            Core.Satchel.Remove(_satchel, item.Kind, item.Id, item.Count);
        System.Collections.Generic.IReadOnlyList<Core.Satchel.Item> withSheet =
            Core.Satchel.Add(less, sheet);
        if (!Core.Satchel.CanTake(less, sheet) || !Core.Satchel.CanTake(withSheet, bulk))
        {
            SayItWhereTheyAreLooking(Core.Satchel.SpaceLine(_satchel));
            return;
        }

        _satchel = [.. Core.Satchel.Add(withSheet, bulk)];

        // Said where the captain is actually looking (#680/#736) — the spread stays OPEN through this, so a
        // line sent to the HUD would be in the DOM and under the backdrop's blur. Nothing is FILED: the book
        // was told what this document said when it was dug, and a second entry saying the paper came apart
        // in your hands would be the captain writing down their own tradecraft.
        SayItWhereTheyAreLooking(PageGranularity.SplitLine);
        RequestVaultSave();
    }
}
