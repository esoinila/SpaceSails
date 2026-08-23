using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L3 · THE BOOK. L5a filled `_heldMemories` and had nowhere to draw them; this is the four things
// the client owes the sheets:
//
//   THE SHEETS      every held memory renders in NOTES as a sheet — the text, the mark (mine / his /
//                   hers / not anyone's), the tag (money / love), who handed it over, and the day.
//   THE STACKS      THREADS stacks them by the names they write down, beside the field book's own
//                   stacks and on the same page. The photograph's four faces are four threads on the
//                   day it is handed over, which is what L5a seeded them for.
//   THE TABLE       seated at the SPREAD, two papers laid together reconcile (`SpreadReconcile`), and
//                   three that are nobody's assemble a NEBULA shard.
//   THE STRAYS      the rare fourth thing a grey page can do (`Flashback.Strays`), filed here rather
//                   than at the filing line, because what arrives is a SHEET and this is the book.
//
// NOTHING HERE DECIDES ANYTHING. Which verdict a pair settles into is `SpreadReconcile.Lay`; which
// stray comes and whether one comes at all is `Flashback`; what any of it says is Fable's. The client's
// opinions in this lane are which page, which row, and when a table is cleared.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
public partial class Map
{
    // ── THE FILTER ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>#973 L3 · MONEY &amp; LOVE (owner ruling §12), as a filter on THREADS. Null is the whole
    /// book, which is where every open lands: a filter that survived the lid would be a captain opening
    /// their own book onto half of it and not being told why.</summary>
    private HeldMemory.Theory? _bookTag;

    /// <summary>Turn the filter. Putting the pen's held end down for the same reason
    /// <see cref="TheNotebookTurnsTo"/> does: half a gesture, said on a page that no longer shows the other
    /// title, is not a sentence.</summary>
    private void TheThreadsFilterTurnsTo(HeldMemory.Theory? tag)
    {
        _bookTag = tag;
        _penHoldingId = null;
    }

    /// <summary>The sheets THREADS is showing, stacked by the names they write down.</summary>
    private IReadOnlyList<HeldMemory.Stack> SheetStacks() => HeldMemory.Stacks(_heldMemories, _bookTag);

    /// <summary>Every sheet the NOTES page draws, newest first. Unfiltered: the filter is THREADS' question
    /// ("which theory does this stack serve"), and a notebook that hid pages would be #587's own sin.</summary>
    private IReadOnlyList<HeldMemory.Sheet> TheSheets() => HeldMemory.Filtered(_heldMemories);

    // ── THE TABLE ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What is lying on the SPREAD right now, in the order it was laid. Never persisted: a table
    /// is a gesture and not a possession, and a save that came back with two sheets half-compared would be
    /// remembering a thought rather than a fact.</summary>
    private readonly List<SpreadReconcile.Paper> _laid = [];

    /// <summary>What the table last said, or null before anything has been laid together.</summary>
    private SpreadReconcile.Result? _reconciled;

    /// <summary>The papers a seated captain may lay: every held memory, and every page of the Captain's
    /// ledger this captain has WON BACK — because a page still grey is a page nobody has read, and you
    /// cannot lay a thing you have not read.</summary>
    private IReadOnlyList<SpreadReconcile.Paper> LayablePapers()
    {
        var papers = new List<SpreadReconcile.Paper>();
        foreach (HeldMemory.Sheet sheet in _heldMemories)
        {
            papers.Add(SpreadReconcile.Paper.Of(sheet, HeldMemory.RowTitle(sheet)));
        }

        foreach (LedgerPage page in LedgerPagesForFiling())
        {
            FilingLine.Page standing = FilingLine.Standing(_filingBook, page.Id);
            if (standing.IsGrey)
            {
                continue;
            }

            papers.Add(SpreadReconcile.Paper.Of(page, standing, []));
        }

        return papers;
    }

    /// <summary>Is this paper already on the table? The row says so rather than being hidden — a paper that
    /// vanished from the list the moment you laid it would leave the captain looking for what they just
    /// picked up.</summary>
    private bool IsLaid(string id)
    {
        foreach (SpreadReconcile.Paper p in _laid)
        {
            if (string.Equals(p.Id, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Take everything off the table. The captain's own move, and the one the SPREAD makes for them
    /// when they stand up — a pair laid at one seat is not a pair laid at the next one.</summary>
    private void ClearTheSpread()
    {
        _laid.Clear();
        _reconciled = null;
    }

    /// <summary>
    /// LAY ONE DOWN. A second paper reconciles against the first; a third joins them, because the one
    /// argument this table can make needs three.
    ///
    /// <para>The order of the two effects below is the whole of the wiring: the RECONCILE happens on the
    /// pair (Core's law, two papers), and the LATTICE is asked of the whole table afterwards. A third stray
    /// therefore both reconciles with the second — <i>they agree</i> — and completes the set, which is
    /// exactly what the fiction says happens: they agree with each other, and that agreement is the shard.</para>
    /// </summary>
    private void LayItOnTheSpread(SpreadReconcile.Paper paper)
    {
        if (IsLaid(paper.Id))
        {
            return;
        }

        if (_laid.Count >= SpreadReconcile.StraysForTheBleed)
        {
            // The table holds three. A fourth is a new comparison, not a bigger one — so the table is
            // cleared and the new paper is the first thing on it, which is what pushing papers aside does.
            ClearTheSpread();
        }

        _laid.Add(paper);
        if (_laid.Count < 2)
        {
            _satchelOutcome = null;
            StateHasChanged();
            return;
        }

        SpreadReconcile.Result result = SpreadReconcile.Lay(_laid[^2], _laid[^1], AnAuthoredRevealFor);
        _reconciled = result;
        ApplyTheReconcile(result);

        if (SpreadReconcile.TheBleedAssembles(_laid))
        {
            TheBleedComesTogether();
        }

        _satchelOutcome = result.Line;
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>
    /// #973 · <b>THE AUTHORED REVEALS — the one seam a lane with two specific papers in mind plugs into.</b>
    /// Asked before any of the three general verdicts (<see cref="SpreadReconcile.Lay"/>), because a fact
    /// about a story cannot be derived from names and numbers.
    ///
    /// <para>Empty today, and deliberately a named method rather than a missing argument: #973 L5b's walk-in
    /// is the first lane with something to put here — <i>her note beside the fleet-day page or the job's
    /// first slip</i> (which finishes a line somebody left unfinished) and <i>her note beside any
    /// money-tagged old-crew slip</i> (which says out loud that two hands are one hand, and marks her note
    /// corrected). Both are built with <see cref="SpreadReconcile.Reveals"/>, which does the money/love
    /// counting so the reveal only has to carry its own words. The mark her note wears —
    /// <see cref="HeldMemory.Mark.Hers"/> — is already in the enum and needs nothing from that lane.</para>
    ///
    /// <para>A reveal that does not apply returns null and the table answers as it always does, so an empty
    /// hook and a missing hook are the same behaviour — which is what makes this safe to ship ahead of the
    /// lane that fills it.</para>
    /// </summary>
    private SpreadReconcile.Result? AnAuthoredRevealFor(SpreadReconcile.Paper a, SpreadReconcile.Paper b) =>
        null;

    /// <summary>
    /// WHAT THE VERDICT DOES TO THE TWO BOOKS. Every one of the three writes something, because a table that
    /// only ever printed a sentence would be a mood and not a mechanic.
    /// </summary>
    private void ApplyTheReconcile(SpreadReconcile.Result result)
    {
        switch (result.Verdict)
        {
            case SpreadReconcile.Verdict.Agree:
                foreach (string id in result.Corroborated)
                {
                    if (HeldMemory.Find(_heldMemories, id) is { } sheet)
                    {
                        _heldMemories = HeldMemory.Put(
                            _heldMemories, sheet.Warmer(SpreadReconcile.MostConfidence));
                    }
                }

                break;

            case SpreadReconcile.Verdict.Disagree:
                RestoreTheHiddenOriginal(result.CorrectedId);
                break;

            default:
                if (HeldMemory.Find(_heldMemories, result.LeadId) is { } lead)
                {
                    _heldMemories = HeldMemory.Put(
                        _heldMemories, lead.Naming(SpreadReconcile.NotAnyonesYet));
                }

                break;
        }

        LogAutopilotEvent($"🗂 {result.Line}  ({result.SecondQuestion})");
    }

    /// <summary>
    /// THE ORIGINAL COMES BACK. #974 kept it in the vault, unread by any surface, from the moment the page
    /// came back wrong — and this is the one surface it was kept for. The page stops carrying its moved
    /// detail, so the ledger row redraws as what it always said; and if the book holds a SHEET under that
    /// same id, the sheet is marked corrected, because a memory that has been caught and put right is not
    /// the same evidence it was an hour ago.
    /// </summary>
    private void RestoreTheHiddenOriginal(string id)
    {
        FilingLine.Page standing = FilingLine.Standing(_filingBook, id);
        if (standing.WasAltered)
        {
            _filingBook = FilingLine.Put(_filingBook, new FilingLine.Page(
                id, FilingLine.PageState.CameBack, FilingLine.Detail.None, "", ""));
        }

        if (HeldMemory.Find(_heldMemories, id) is { } sheet)
        {
            _heldMemories = HeldMemory.Put(_heldMemories, sheet with { Corrected = true });
        }
    }

    /// <summary>
    /// THREE THAT ARE NOBODY'S. The only NEBULA shard the captain assembles out of their own book, and it
    /// arrives with no plate at all: the SPREAD is its host, the three sheets are lying on it, and the
    /// player's eye is already there (<see cref="NebulaLore.PlateFor"/> returns null for it, so
    /// <see cref="TryAssembleNebula"/> raises nothing — the same rule the four hosted shards keep).
    /// </summary>
    private void TheBleedComesTogether() =>
        TryAssembleNebula(SpreadReconcile.TheBleedId, "▓ " + NebulaLore.ById(SpreadReconcile.TheBleedId)!.Lore);

    // ── THE STRAYS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A PAGE THAT WAS NEVER YOURS. Called from the filing line's <i>nothing</i> arm and nowhere else, so a
    /// stray can never take a recollection away from a captain who rolled one.
    ///
    /// <para>Everything about WHETHER and WHICH is Core's (<see cref="Flashback.AStrayComesInstead"/>,
    /// <see cref="Flashback.DrawStray"/>); the drawn set is the stray sheets already in the book, which is
    /// how "never the same one twice" persists without a second store. It costs the pip a wrong page costs
    /// and raises the same beat, because from the outside it is the same act: you sat down with a page you
    /// did not remember, and something came back.</para>
    /// </summary>
    /// <returns>True when a stray came, so the caller knows not to say <i>nothing</i>.</returns>
    private bool AStrayComesBackInstead(string entryId)
    {
        int readBefore = FilingLine.GreyPagesReadThisLife(_filingBook);
        if (!Flashback.AStrayComesInstead(_activeThreadId ?? "", entryId, LifeNumber, readBefore))
        {
            return false;
        }

        ulong seed = Flashback.SeedFor(_activeThreadId ?? "", entryId, LifeNumber);
        var drawn = new List<string>();
        foreach (HeldMemory.Sheet held in _heldMemories)
        {
            if (Flashback.IsStrayId(held.Id))
            {
                drawn.Add(held.Id);
            }
        }

        if (Flashback.DrawStray(drawn, seed) is not { } index)
        {
            return false;   // this thread has all six; the page stays somebody else's
        }

        string text = Flashback.Strays[index];
        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            Flashback.StrayId(index),
            HeldMemory.Mark.NotAnyones,
            // Every one of the six is about being SOMEBODY — barefoot in a corridor, turning to a name,
            // a hand teaching yours. None of them is about money, and the tag has to be true.
            HeldMemory.Theory.Love,
            text,
            [],
            SimTime));

        ShowPulseMessage(Flashback.StrayToast(text));
        LogAutopilotEvent($"{FilingLine.Mark} {Flashback.StrayToast(text)}");
        ApplyNerveShock(NervePips.PipUnit * Flashback.StrayNervePips, Flashback.StrayNerveLabel);
        RaiseStoryBeat(StoryBeats.Beat.Flashback, Flashback.StrayId(index));
        return true;
    }

    // ── THE SIGNING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE DAY YOU SIGNED, FILED. L2 raised the plate at the moment the captain says <i>I already have a
    /// policy</i> to the man holding the file, and the prose had nowhere to live (a plate carries a caption
    /// about the room, not a paragraph about the afternoon). It lives here: a held memory marked
    /// <i>mine</i>, tagged money, put in the book on the same edge the plate goes up.
    ///
    /// <para>After a rebirth the sheet GROWS a line rather than being replaced by a different sheet
    /// (<see cref="NebulaRep.SigningMemoryFor"/>) — the memory did not change, it acquired a sentence, which
    /// is the whole of what a rebirth does to a page you still have. <see cref="HeldMemory.Put"/> replaces
    /// under the same id, so the confidence a captain has already earned on it rides across.</para>
    /// </summary>
    private void FileTheSigningSheet()
    {
        string text = NebulaRep.SigningMemoryFor(RetiredCaptainCount);
        HeldMemory.Sheet? had = HeldMemory.Find(_heldMemories, NebulaRep.SigningMemoryId);
        if (had is { } already && string.Equals(already.Text, text, StringComparison.Ordinal))
        {
            return;   // same afternoon, same sentence — nothing to write
        }

        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            NebulaRep.SigningMemoryId,
            HeldMemory.Mark.Mine,
            HeldMemory.Theory.Money,
            text,
            had?.Threads ?? [],
            had?.SimTime ?? SimTime,
            Filed: false,
            HandedBy: had?.HandedBy ?? "",
            Confidence: had?.Confidence ?? 0,
            Corrected: had?.Corrected ?? false));

        RequestVaultSave();
    }
}
