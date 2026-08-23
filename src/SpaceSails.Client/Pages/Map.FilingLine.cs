using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L1 · THE FILING LINE, WIRED. The law is Core's (`FilingLine`, `Flashback`); this is the four
// things the client owes it:
//
//   THE MARKING    at the succession seam, and only there — every dated row of the Captain's ledger
//                  dated after the last filing becomes a page you don't remember writing.
//   THE READING    a click on a grey row is a flashback attempt: one roll on the shared dice, once per
//                  page per life, and the three outcomes settled and told.
//   THE DRAWING    the grey rows come out of `LedgerTips()` marked, and a page that came back WRONG
//                  comes out with its one moved detail already in it, so the desk renders a lie it
//                  cannot tell from the truth.
//   THE KEEPING    the marks, the refusals and the hidden originals ride the vault, so a reload never
//                  re-greys a page the captain won back and never re-rolls one they lost.
//
// NOTHING HERE DECIDES ANYTHING. Which pages grey is `FilingLine.Mark`; what a roll settles into is
// `Flashback.Read`; which detail moves is `Flashback.Alter`. The client's opinions in this lane are
// exactly two, and both are about surfaces: a plate rather than a card, and the wake notice riding the
// succession block the death card already raises rather than a card of its own.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
public partial class Map
{
    /// <summary>Where every marked page of the Captain's ledger stands with the captain now reading it.
    /// Empty for a captain who has never died — which is the honest state of a book nobody has been
    /// re-filed against, and the state every pre-#973 save loads with.</summary>
    private IReadOnlyList<FilingLine.Page> _filingBook = [];

    /// <summary>Which captain this is on the thread — the retirements plus the one at the helm. The roll is
    /// salted with it, so the same grey page answers a later captain differently, and a refusal lifted by a
    /// rebirth is a genuinely new question rather than a re-roll of the old one.</summary>
    private int LifeNumber => (ActiveThreadInfo?.Retired.Count ?? 0) + 1;

    // ── THE MARKING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE REBIRTH READS THE POLICY AND CLOSES THE BOOK. Called once from the succession seam, off the same
    /// `_insurance` the clinic bill was computed from one screen earlier — so the receipt and the amnesia can
    /// never disagree about what the captain was carrying.
    /// </summary>
    /// <returns>The one line the wake card says about it.</returns>
    private string MarkTheBookAtTheFilingLine()
    {
        double line = FilingLine.At(_insurance);
        _filingBook = FilingLine.MarkTheBook(_filingBook, LedgerPagesForFiling(), line);
        return FilingLine.WakeNotice(line);
    }

    /// <summary>Every DATED row of the Captain's ledger, as the filing line sees it. Read off
    /// <c>LedgerTips()</c> rather than off the six books it is assembled from, so the pages that can grey and
    /// the rows that are drawn are the same list by construction and cannot drift apart.</summary>
    private IReadOnlyList<LedgerPage> LedgerPagesForFiling()
    {
        var pages = new List<LedgerPage>();
        foreach (Stations.Captain.LedgerTip tip in LedgerTips())
        {
            if (tip.EntryId is { Length: > 0 } id && tip.SimTime is { } at)
            {
                pages.Add(new LedgerPage(id, at, tip.Title, tip.Lines, tip.Provenance ?? "", tip.Filed));
            }
        }

        return pages;
    }

    // ── THE DRAWING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The Captain's ledger as THIS captain remembers it: the grey rows marked, and a row that came back
    /// wrong carrying its one moved detail. The desk renders what this returns and knows nothing about the
    /// filing — which is why a wrong page looks exactly like a right one from there.
    /// </summary>
    private Stations.Captain.LedgerTip[] LedgerTipsAsRemembered()
    {
        Stations.Captain.LedgerTip[] raw = LedgerTips();
        if (_filingBook.Count == 0)
        {
            return raw; // nobody has died yet; every page is yours
        }

        var told = new Stations.Captain.LedgerTip[raw.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            Stations.Captain.LedgerTip tip = raw[i];
            if (tip.EntryId is not { Length: > 0 } id)
            {
                told[i] = tip;
                continue;
            }

            FilingLine.Page standing = FilingLine.Standing(_filingBook, id);
            if (standing.WasAltered)
            {
                var page = new LedgerPage(id, tip.SimTime ?? 0, tip.Title, tip.Lines, tip.Provenance ?? "");
                LedgerPage moved = Flashback.Apply(
                    page, new Flashback.Alteration(standing.Which, standing.Original, standing.Substitute));
                tip = tip with
                {
                    Title = moved.Title,
                    Lines = moved.Lines,
                    Provenance = tip.Provenance is null ? null : moved.Provenance,
                };
            }

            told[i] = tip with { Grey = standing.IsGrey };
        }

        return told;
    }

    // ── THE READING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SITTING DOWN WITH A PAGE YOU DON'T REMEMBER WRITING. One roll on the shared dice, once per page per
    /// life; the three outcomes are Core's and so are the three sentences.
    ///
    /// <para>The only judgement made here is the honest degrade: a page with nothing on it a person could
    /// misremember one of (no number in its own words, and no attribution to move) cannot come back wrong,
    /// so it comes back whole and costs nothing. Faking a lie by inventing a detail the page never had would
    /// be a worse answer than admitting the page was too thin to lose anything.</para>
    /// </summary>
    private void ReadTheGreyPage(string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
        {
            return;
        }

        FilingLine.Page standing = FilingLine.Standing(_filingBook, entryId);
        if (!standing.MayBeRead)
        {
            return; // already yours, or already tried this life — the latch, and there is no second roll
        }

        DicePool roll = Flashback.Roll(_activeThreadId ?? "", entryId, LifeNumber, _insurance.Tier);
        Flashback.Outcome outcome = Flashback.Read(roll);

        if (outcome == Flashback.Outcome.Nothing)
        {
            // #973 L3 · …UNLESS SOMETHING ELSE CAME. The rare fourth thing a grey page does is hidden inside
            // the WORST outcome rather than beside the best one: once in four nothings, never on the first
            // grey page of a life, a page comes back that was never yours (`AStrayComesBackInstead`, and
            // every rule of it is Core's). The row is still Refused either way — what arrived was not this
            // page — and the stray says its own line, spends its own pip and raises its own plate.
            bool stray = AStrayComesBackInstead(entryId);

            _filingBook = FilingLine.Put(_filingBook, standing with { State = FilingLine.PageState.Refused });
            if (!stray)
            {
                ShowPulseMessage(Flashback.Toast(Flashback.Outcome.Nothing));
                // No beat and no card: nothing came back, and a picture of a memory that did not arrive would
                // be the seam being spent on an absence. The dice are still filed, so the ledger can show the
                // math.
                LogAutopilotEvent($"{FilingLine.Mark} {Flashback.Toast(Flashback.Outcome.Nothing)}  ({roll.Describe()})");
            }

            RequestVaultSave();
            StateHasChanged();
            return;
        }

        Flashback.Alteration lie = standing.WasAltered
            // A page that already came back wrong in an earlier life keeps the detail it was already
            // carrying. The lie was written into the book, and a new captain inherits the book — not the
            // truth, and not a second, different falsehood layered over the first.
            ? new Flashback.Alteration(standing.Which, standing.Original, standing.Substitute)
            : outcome == Flashback.Outcome.CameBackWrong
                ? Flashback.Alter(ThisPage(entryId), LedgerPagesForFiling(), Flashback.SeedFor(_activeThreadId ?? "", entryId, LifeNumber))
                : new Flashback.Alteration(FilingLine.Detail.None, "", "");

        bool wrong = lie.Which != FilingLine.Detail.None;
        // …and whether the moved detail is NEW. A page re-greyed by a later rebirth and won back again is
        // still carrying the lie an earlier captain took the pip for, and the row must go on saying so — but
        // charging the sanity seam a second time for the same falsehood would turn one wrong page into a
        // recurring tax on every death, which is the repeat-tax #480 ruled against in the nerve's own lane.
        bool freshLie = wrong && !standing.WasAltered;
        _filingBook = FilingLine.Put(_filingBook, new FilingLine.Page(
            entryId,
            wrong ? FilingLine.PageState.CameBackWrong : FilingLine.PageState.CameBack,
            lie.Which, lie.Original, lie.Substitute));

        Flashback.Outcome told = wrong ? Flashback.Outcome.CameBackWrong : Flashback.Outcome.CameBack;
        ShowPulseMessage(Flashback.Toast(told));

        if (freshLie)
        {
            // #226's sanity seam, and the only nerve this lane spends: a memory that is not how you remember
            // it costs a pip, filed under its own name so "what broke you?" can say so afterwards.
            ApplyNerveShock(NervePips.PipUnit * Flashback.WrongPageNervePips, Flashback.WrongPageNerveLabel);
        }

        LogAutopilotEvent($"{FilingLine.Mark} {Flashback.Toast(told)}  ({roll.Describe()})");
        RaiseStoryBeat(StoryBeats.Beat.Flashback, entryId);
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>This entry as it TRULY reads — the unaltered page the lie is written against. Empty when the
    /// row has left the ledger between the click and here, which the alteration handles by finding nothing to
    /// move.</summary>
    private LedgerPage ThisPage(string entryId)
    {
        foreach (LedgerPage page in LedgerPagesForFiling())
        {
            if (string.Equals(page.Id, entryId, StringComparison.Ordinal))
            {
                return page;
            }
        }

        return new LedgerPage(entryId, 0, "", [], "");
    }

    // ── THE KEEPING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The marks, as the vault stores them — opaque rows, the house idiom. Null when nothing is
    /// marked, so a captain who has never died writes no section at all.</summary>
    private FilingSection? BuildFilingSection() =>
        _filingBook.Count == 0
            ? null
            : new FilingSection { Pages = [.. _filingBook.Select(p => p.Stored)] };

    /// <summary>Read them back. A row this build cannot parse is dropped rather than thrown over — the same
    /// tolerance the satchel and the red lines get; a pre-#973 file simply has none, and a book with nothing
    /// marked is exactly what it was.</summary>
    private void RestoreFilingSection(FilingSection? section)
    {
        var book = new List<FilingLine.Page>();
        foreach (string stored in section?.Pages ?? [])
        {
            if (FilingLine.Page.TryParse(stored, out FilingLine.Page page))
            {
                book.Add(page);
            }
        }

        _filingBook = book;
    }
}
