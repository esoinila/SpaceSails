using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #798 item 2 · PAGE GRANULARITY — the arithmetic of tearing one sheet out of a file, and the four laws
/// that decide when a captain may.
///
/// <para>Owner, refining the disposal tradecraft (2026-08-09): <i>"a compromising FILE is not uniform… rip
/// out the most compromising evidence and toss the rest inconspicuously. So a multi-page document can be
/// SPLIT: keep/destroy per page — pocket the one damning sheet (small, hideable, the photograph already in
/// the book) and bin the innocent bulk, which is ALSO cover (a file in the bin that reads boring explains
/// itself; a missing file explains nothing). The processing UI (#784) is where the split happens — seated,
/// with air."</i></para>
///
/// <h3>Why the nets are wide</h3>
/// <para>Every fact about a document in this game is rolled off its id, so an assertion made about one paper
/// is an assertion about one roll. A hundred and twenty ids per law is what makes the difference between
/// "this document happens to have three pages" and "no document anywhere has a worst page that is not in
/// it" — and several guards below assert that the net itself is VARIED (both single- and multi-page
/// documents in it, every page count reachable), because a world in which every paper is one sheet cannot
/// tell a working split from a dead one. That is this repo's fifth named bug class, aimed at its own
/// bench.</para>
/// </summary>
public sealed class ThePageWorthTearingOutTests
{
    /// <summary>A wide net of ordinary seeded papers — nothing authored, so every one of them is a document
    /// the dice composed and the page count is a real roll.</summary>
    private static IEnumerable<string> ManyPapers() =>
        Enumerable.Range(0, 240).Select(i => $"hive:doc:pages-{i}");

    /// <summary>#798 item 2, second cut · …and a net of dossier-shaped ids, out of the same generator's
    /// grammar. Every fact about a document here is rolled off its id and an id is an opaque string, so this
    /// net proves nothing new about the DICE — what it proves is that the laws below are stated over the ids
    /// a <see cref="Satchel.Kind.Dirt"/> row actually wears rather than over paper's alone.</summary>
    private static IEnumerable<string> ManyDossiers() =>
        Enumerable.Range(0, 240).Select(i => $"hive:dirt:pages-{i}");

    /// <summary>
    /// #798 item 2, second cut · THE KINDS THAT COME APART — asked of Core, never typed out.
    ///
    /// <para>A document, in this game, is whatever <see cref="RipAndBin.IsEvidence"/> says it is: the pair
    /// the field book has a gist for, the pair the bin's picker offers, and now the pair the scissors are
    /// live on. Sweeping the enum through that predicate rather than writing <c>Paper, Dirt</c> is what
    /// makes a third kind of document arriving tomorrow swept by every law in this file without anybody
    /// remembering to come back here.</para>
    /// </summary>
    private static IEnumerable<Satchel.Kind> EvidenceKinds() =>
        Enum.GetValues<Satchel.Kind>().Where(RipAndBin.IsEvidence);

    /// <summary>Anti-vacuity for every sweep below: a "both kinds" loop over one kind proves half of what it
    /// claims, and a loop over none passes in silence. Written once and asserted in each sweep.</summary>
    private static void TheNetHasBothKindsOfDocument()
    {
        List<Satchel.Kind> kinds = [.. EvidenceKinds()];
        Assert.True(kinds.Count >= 2,
            $"this build calls {kinds.Count} kind(s) evidence — every sweep in this file that claims to "
            + "cover both kinds of document is being stated over a world with only one of them.");
        Assert.Contains(Satchel.Kind.Paper, kinds);
        Assert.Contains(Satchel.Kind.Dirt, kinds);
    }

    // ── (a) A DOCUMENT HAS PAGES, AND THE SAME ONES EVERY TIME ───────────────────────────────────────

    /// <summary>
    /// THE PAGE COUNT IS A PROPERTY OF THE DOCUMENT, NOT OF THE MOMENT IT WAS ASKED FOR.
    ///
    /// <para>Core is pure and deterministic (the night shift's law 6, and this file's whole licence to roll
    /// anything at all): the same id gives the same length and the same worst page on every call, in every
    /// session, on every machine — which is what lets a save carry an id and nothing else.</para>
    ///
    /// <para>And the net is proved VARIED in the same breath: every page count this build can produce has to
    /// be reachable, or the laws below would be stated over a world that only ever hands out one answer.</para>
    ///
    /// <para><b>Proven RED</b> by script-replacing the seed with <c>DiceRule.Seed($"pages:count:{paperId}:
    /// {Environment.TickCount}")</c> — a clock in a Core roll, the exact thing law 6 forbids:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 2
    /// Actual:   4
    ///   hive:doc:pages-0 changed its length between two asks.
    /// </code>
    /// </summary>
    [Fact]
    public void A_DOCUMENT_IsTheSameLengthEveryTimeItIsAsked()
    {
        var seen = new HashSet<int>();
        foreach (string id in ManyPapers())
        {
            int pages = PageGranularity.PagesIn(id);
            int worst = PageGranularity.WorstPageOf(id);
            seen.Add(pages);

            for (int again = 0; again < 3; again++)
            {
                Assert.True(pages == PageGranularity.PagesIn(id),
                    $"{id} changed its length between two asks.");
                Assert.True(worst == PageGranularity.WorstPageOf(id),
                    $"{id} changed which of its pages was the worst between two asks.");
            }
        }

        for (int pages = 1; pages <= PageGranularity.LongestDocument; pages++)
        {
            Assert.True(seen.Contains(pages),
                $"no document in a net of 240 is {pages} page(s) long — the laws in this file are being "
                + "stated over a world that cannot produce the case they are about.");
        }
    }

    /// <summary>
    /// THE WORST PAGE IS ALWAYS INSIDE THE DOCUMENT.
    ///
    /// <para>There is no such thing as page four of a three-page file, and a build that rolled one would put
    /// a citation on a satchel row that cannot be true — the third named bug class, in an inventory line.
    /// The two facts are rolled on separate streams on purpose (a document that grew a page must not quietly
    /// shift which sheet was damning), and this is the guard that keeps the second stream honest about the
    /// first.</para>
    ///
    /// <para><b>Proven RED</b> by script-changing <c>WorstPageOf</c> to roll over
    /// <c>PageGranularity.LongestDocument</c> instead of over the document's own length:</para>
    /// <code>
    /// hive:doc:pages-1 is 2 page(s) long and its worst page is page 4.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_WORST_PAGE_IsAlwaysOneOfTheDocumentsOwn()
    {
        int multiPage = 0;
        foreach (string id in ManyPapers())
        {
            int pages = PageGranularity.PagesIn(id);
            int worst = PageGranularity.WorstPageOf(id);
            Assert.True(worst >= 1 && worst <= pages,
                $"{id} is {pages} page(s) long and its worst page is page {worst}.");
            if (pages >= 2)
            {
                multiPage++;
            }
        }

        Assert.True(multiPage > 60,
            $"only {multiPage} of 240 documents have more than one page — a net this thin cannot state a "
            + "law about multi-page files.");
    }

    // ── (b) THE SPLIT'S ARITHMETIC ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// EXACTLY ONE SHEET IS KEPT, AND THE BULK CARRIES THE REST.
    ///
    /// <para>The owner's own sum: <i>"pocket the one damning sheet… and bin the innocent bulk."</i> One
    /// page out, every other page in the folder, nothing created and nothing lost — a split that dropped a
    /// page would be destroying evidence the captain never chose to destroy, and one that duplicated a page
    /// would put the same sheet in a pocket and a bin at once.</para>
    ///
    /// <para><b>Proven RED</b> by script-changing <c>BulkPagesIn</c> to <c>PagesIn(paperId)</c> — the folder
    /// keeping the sheet that was supposed to come out of it:</para>
    /// <code>
    /// hive:doc:pages-2: 1 sheet + 2 pages of bulk is not a 2-page document.
    /// </code>
    /// </summary>
    [Fact]
    public void A_SPLIT_KeepsOneSheetAndTheBulkCarriesEveryOtherPage()
    {
        int split = 0;
        foreach (string id in ManyPapers().Where(PageGranularity.IsMultiPage))
        {
            int pages = PageGranularity.PagesIn(id);
            int bulk = PageGranularity.BulkPagesIn(id);
            split++;

            Assert.True(1 + bulk == pages,
                $"{id}: 1 sheet + {bulk} pages of bulk is not a {pages}-page document.");
            Assert.True(bulk >= 1,
                $"{id}: the bulk of a {pages}-page document is {bulk} page(s) — there would be nothing to "
                + "put in a bin, and the cover is the whole point of the verb.");
        }

        Assert.True(split > 60, $"only {split} document(s) in the net can be split — this proves nothing.");
    }

    /// <summary>
    /// A ONE-SHEET DOCUMENT HAS NOTHING TO SPLIT, AND EVERYTHING ELSE ABOUT IT IS IRRELEVANT.
    ///
    /// <para>The page count is the clause the verb is NAMED for, so it is asserted against a world where
    /// every other clause is satisfied — seated, dug, whole, paper. A guard that refused a single sheet
    /// because it was also standing up would be agreeing with the wrong law.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting <c>&amp;&amp; IsMultiPage(paperId)</c> from
    /// <c>CanSplit</c>:</para>
    /// <code>
    /// hive:doc:pages-0 is one sheet and the game is offering to tear a page out of it.
    /// </code>
    /// </summary>
    [Fact]
    public void A_ONE_SHEET_DOCUMENT_IsNeverSplit()
    {
        TheNetHasBothKindsOfDocument();
        int single = 0, many = 0;

        // #798 item 2, second cut · Stated over BOTH kinds of document. A file on somebody is the object the
        // owner's sentence was actually about, and the page count it is refused on is the same roll off the
        // same id — so the law is one law and this is one loop, not two guards that could drift apart.
        foreach (Satchel.Kind kind in EvidenceKinds())
        {
            foreach (string id in ManyPapers().Concat(ManyDossiers()))
            {
                bool can = PageGranularity.CanSplit(
                    kind, id, alreadyInTheBook: true, seatedForTheSpread: true);

                if (PageGranularity.PagesIn(id) == 1)
                {
                    single++;
                    Assert.False(can,
                        $"{kind} {id} is one sheet and the game is offering to tear a page out of it.");
                }
                else
                {
                    many++;
                    Assert.True(can,
                        $"{kind} {id} is {PageGranularity.PagesIn(id)} pages, dug, whole and on a table, and "
                        + "the split is refused anyway.");
                }
            }
        }

        Assert.True(single > 40 && many > 60,
            $"the net is {single} single-sheet and {many} multi-page document(s) — both halves of this law "
            + "need a world that produces them.");
    }

    // ── (c) THE SEATED-ONLY LAW, AND THE BOOK-FIRST LAW ──────────────────────────────────────────────

    /// <summary>
    /// THE SPLIT HAPPENS SEATED, AND NOWHERE ELSE.
    ///
    /// <para>Owner: <i>"the processing UI is where the split happens — seated, with air."</i> Taking a file
    /// apart is the table verb's own gesture: it wants a surface, both hands and a minute nobody is watching
    /// you through, and it is the same gate the dig and the reconcile are already under
    /// (<see cref="SeatedSpread.RefusalAt"/>, asked by the caller and never re-decided in Core).</para>
    ///
    /// <para>Stated over the SEAT LADDER itself rather than over a bare flag, so the law is asserted in the
    /// terms a player meets it in: a cubicle and a cabinet always, a hall table and a park bench only alone,
    /// a bar stool never.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting <c>&amp;&amp; seatedForTheSpread</c> from
    /// <c>CanSplit</c>:</para>
    /// <code>
    /// BarStool (alone: True): the case cannot be spread at this seat and the split is offered anyway.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_SPLIT_IsOfferedOnlyWhereTheCaseMayBeSpread()
    {
        TheNetHasBothKindsOfDocument();
        string paper = ManyPapers().First(PageGranularity.IsMultiPage);
        string dossier = ManyDossiers().First(PageGranularity.IsMultiPage);
        int allowed = 0, refused = 0;

        foreach (Satchel.Kind kind in EvidenceKinds())
        foreach (string id in new[] { paper, dossier })
        foreach (SeatedHud.Seat seat in Enum.GetValues<SeatedHud.Seat>())
        {
            foreach (bool alone in new[] { true, false })
            {
                bool maySpread = SeatedSpread.RefusalAt(seat, alone) is null;
                bool can = PageGranularity.CanSplit(
                    kind, id, alreadyInTheBook: true, seatedForTheSpread: maySpread);

                if (maySpread)
                {
                    allowed++;
                    Assert.True(can, $"{kind} {id} at {seat} (alone: {alone}): the case may be spread here "
                        + "and the split is refused anyway.");
                }
                else
                {
                    refused++;
                    Assert.False(can, $"{kind} {id} at {seat} (alone: {alone}): the case cannot be spread at "
                        + "this seat and the split is offered anyway.");
                }
            }
        }

        Assert.True(allowed > 0 && refused > 0,
            $"the seat ladder produced {allowed} allowed and {refused} refused seat(s) — a ladder that "
            + "answers one way everywhere cannot tell this law's two halves apart.");
    }

    /// <summary>
    /// THE BOOK GETS IT FIRST. A document nobody has dug is never taken apart.
    ///
    /// <para>#828's two-tier reading law, read forward: only the dig guarantees the book kept what the sheet
    /// had, and the bulk of a split is on its way to a bin. Splitting before the dig would throw away every
    /// page but one, unread, in a single press — which is precisely the loss that issue spent itself making
    /// impossible to do by accident.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting <c>&amp;&amp; alreadyInTheBook</c> from
    /// <c>CanSplit</c>:</para>
    /// <code>
    /// hive:doc:pages-2: nothing has been dug out of this document and the split is offered anyway.
    /// </code>
    /// </summary>
    [Fact]
    public void A_DOCUMENT_NOBODY_HAS_DUG_IsNeverTakenApart()
    {
        TheNetHasBothKindsOfDocument();
        int tried = 0;
        foreach (Satchel.Kind kind in EvidenceKinds())
        {
            foreach (string id in ManyPapers().Concat(ManyDossiers()).Where(PageGranularity.IsMultiPage))
            {
                tried++;
                Assert.False(
                    PageGranularity.CanSplit(
                        kind, id, alreadyInTheBook: false, seatedForTheSpread: true),
                    $"{kind} {id}: nothing has been dug out of this document and the split is offered "
                    + "anyway.");
            }
        }

        Assert.True(tried > 60, $"only {tried} document(s) were tried — this proves nothing.");
    }

    /// <summary>
    /// A DOCUMENT COMES APART ONCE — and the law needs no register anywhere to hold it.
    ///
    /// <para>The two halves wear ids of their own (<see cref="PageGranularity.TornPageTag"/> /
    /// <see cref="PageGranularity.TornRestTag"/>), so "has this already been split?" is a question about the
    /// thing in the captain's hand. That is what makes the law survive the bulk being binned: the sheet is
    /// the only half left in the world and it still knows what it is.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting <c>&amp;&amp; PartOf(paperId) == Part.Whole</c> from
    /// <c>CanSplit</c>:</para>
    /// <code>
    /// hive:doc:pages-2: the torn-out sheet can be torn again — a document is coming apart twice.
    /// </code>
    /// </summary>
    [Fact]
    public void A_DOCUMENT_ComesApartOnceAndNothingHasToRememberThat()
    {
        TheNetHasBothKindsOfDocument();
        int tried = 0;
        foreach (string id in ManyPapers().Concat(ManyDossiers()).Where(PageGranularity.IsMultiPage))
        {
            string sheet = PageGranularity.SheetIdOf(id);
            string bulk = PageGranularity.BulkIdOf(id);
            tried++;

            Assert.Equal(PageGranularity.Part.Whole, PageGranularity.PartOf(id));
            Assert.Equal(PageGranularity.Part.TheSheet, PageGranularity.PartOf(sheet));
            Assert.Equal(PageGranularity.Part.TheBulk, PageGranularity.PartOf(bulk));
            Assert.Equal(id, PageGranularity.SourceOf(sheet));
            Assert.Equal(id, PageGranularity.SourceOf(bulk));
            Assert.Equal(id, PageGranularity.SourceOf(id));

            foreach (Satchel.Kind kind in EvidenceKinds())
            {
                Assert.False(
                    PageGranularity.CanSplit(
                        kind, sheet, alreadyInTheBook: true, seatedForTheSpread: true),
                    $"{kind} {id}: the torn-out sheet can be torn again — a document is coming apart twice.");
                Assert.False(
                    PageGranularity.CanSplit(
                        kind, bulk, alreadyInTheBook: true, seatedForTheSpread: true),
                    $"{kind} {id}: the bulk can be split again — a document is coming apart twice.");
            }
        }

        Assert.True(tried > 60, $"only {tried} document(s) were tried — this proves nothing.");
    }

    /// <summary>
    /// #798 item 2, second cut · ONLY A DOCUMENT COMES APART — and a document is whatever the field book
    /// has a gist for.
    ///
    /// <para>A handful of rounds has no pages and an authority is a door rather than evidence; a file on
    /// somebody is <i>exactly</i> the multi-page object the owner described (<i>"a compromising FILE is not
    /// uniform"</i>), and the first cut of this feature left it out for a reason about a missing title
    /// rather than a reason about the law. So the clause is <see cref="RipAndBin.IsEvidence"/> — the pair
    /// the book already keeps and the bin already takes — asked of Core rather than restated here, and
    /// swept over every kind the satchel has rather than over the ones somebody remembered.</para>
    ///
    /// <para><b>Proven RED</b> both ways. Reverting <c>CanSplit</c>'s first clause to
    /// <c>kind == Satchel.Kind.Paper</c> — the shipped behaviour this cut is widening — reddens the
    /// offered half:</para>
    /// <code>
    /// Dirt is a document the book keeps a gist for and the game refuses to tear a page out of it.
    /// </code>
    /// <para>…and replacing it with <c>true</c> reddens the withheld half on <c>Authority</c>.</para>
    /// </summary>
    [Fact]
    public void ONLY_EVIDENCE_ComesApart()
    {
        TheNetHasBothKindsOfDocument();
        string id = ManyPapers().First(PageGranularity.IsMultiPage);
        int offered = 0, withheld = 0;

        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            bool can = PageGranularity.CanSplit(
                kind, id, alreadyInTheBook: true, seatedForTheSpread: true);

            if (RipAndBin.IsEvidence(kind))
            {
                offered++;
                Assert.True(can,
                    $"{kind} is a document the book keeps a gist for and the game refuses to tear a page "
                    + "out of it.");
            }
            else
            {
                withheld++;
                Assert.False(can,
                    $"{kind} is not a document and the game is offering to tear a page out of it.");
            }

            // …and the shredder and the scissors are answering ONE question, not two that happen to agree
            // today. This is the line that would catch the day somebody hand-writes `Paper or Dirt` back
            // into either file.
            Assert.True(
                RipAndBin.IsEvidence(kind)
                    == (LeftBehind.GistOf(new Satchel.Item(kind, "split-guard-1"), "B1") is { Length: > 0 }),
                $"{kind}: the book and the shredder disagree about whether this is a document, and the "
                + "scissors are written over one of them.");
        }

        Assert.True(offered >= 2 && withheld >= 4,
            $"the enum produced {offered} evidence and {withheld} non-evidence kind(s) — a sweep whose "
            + "world answers one way everywhere proves nothing.");
    }

    /// <summary>
    /// #798 item 2, second cut · THE CHIP IS ONE OBJECT AND IS NEVER TORN IN HALF.
    ///
    /// <para>Widening the verb to <see cref="Satchel.Kind.Dirt"/> brought one named thing into its world
    /// that is not a dossier at all: what was between the seats of the roadster rides the satchel as a file
    /// on somebody because that is what it IS to a client, and it is a data chip with photographs on it. It
    /// has no pages. The arc wrote its one sentence, the row prints its canon name, and a captain offered
    /// the scissors on <i>page 2 of 3</i> of a chip would be the sim doing one thing while the row said
    /// another — this repo's third named bug class, in an inventory line, introduced by a widening.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting <c>|| CompromisingChip.IsTheFindId(source)</c> from
    /// <c>PagesIn</c>:</para>
    /// <code>
    /// the chip out of the roadster is 4 page(s) long.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_NAMED_CHIP_IsNeverTornInHalf()
    {
        Satchel.Item chip = CompromisingChip.Found();
        Assert.True(RipAndBin.IsEvidence(chip.Kind),
            "the chip is not evidence in this build, so this guard cannot tell a pass from a fail — the "
            + "clause it is about would refuse the split for a completely different reason.");

        Assert.True(PageGranularity.PagesIn(chip.Id) == 1,
            $"the chip out of the roadster is {PageGranularity.PagesIn(chip.Id)} page(s) long.");
        Assert.False(PageGranularity.IsMultiPage(chip.Id));
        Assert.False(
            PageGranularity.CanSplit(
                chip.Kind, chip.Id, alreadyInTheBook: true, seatedForTheSpread: true),
            "the game is offering to tear a page out of a data chip.");
        Assert.Equal("", PageGranularity.RowCitation(chip.Id));

        // …and the two spellings of "is this the chip" are one spelling, which is what keeps the id from
        // being held in two places that could come to disagree about it.
        Assert.True(CompromisingChip.IsTheFindId(chip.Id));
        Assert.False(CompromisingChip.IsTheFindId("hive:doc:pages-0"));
        Assert.False(CompromisingChip.IsTheFindId(null));
    }

    // ── (d) BOTH HALVES ARE STILL THE SAME DOCUMENT ──────────────────────────────────────────────────

    /// <summary>
    /// A PAGE TORN OUT OF A PAY SHEET IS STILL THAT PAY SHEET'S PAGE.
    ///
    /// <para>The title, the body and the certainty are all rolled off a paper's id, and the split gives both
    /// halves new ids. Without <see cref="PageGranularity.SourceOf"/> at the top of each of FieldClue's three
    /// readers, one press would turn a maintenance log into two unrelated documents — a satchel row claiming
    /// a shipping manifest that came out of a pay sheet, which is the third named bug class with scissors.
    /// </para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>paperId = PageGranularity.SourceOf(paperId);</c>
    /// line from <c>FieldClue.Title</c>:</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    /// Expected: "maintenance log, two hands"
    /// Actual:   "pay sheet, allowances"
    ///   hive:doc:pages-2: the torn-out sheet is not called what the document is called.
    /// </code>
    /// </summary>
    [Fact]
    public void BOTH_HALVES_AreStillTheDocumentTheyCameOutOf()
    {
        int tried = 0;

        // #798 item 2, second cut · Over BOTH nets. The three readers take an id and nothing else, so a
        // dossier's halves resolve through exactly the same SourceOf line — and that is the claim worth
        // stating, because it is the whole of why Kind.Dirt needed no reader of its own to come apart.
        foreach (string id in ManyPapers().Concat(ManyDossiers()).Where(PageGranularity.IsMultiPage))
        {
            tried++;
            foreach (string half in new[] { PageGranularity.SheetIdOf(id), PageGranularity.BulkIdOf(id) })
            {
                Assert.True(FieldClue.Title(id) == FieldClue.Title(half),
                    $"{id}: {PageGranularity.PartOf(half)} is not called what the document is called.");
                Assert.True(FieldClue.Document(id) == FieldClue.Document(half),
                    $"{id}: {PageGranularity.PartOf(half)} does not read as the document it came out of.");
                Assert.True(FieldClue.CertaintyOf(id) == FieldClue.CertaintyOf(half),
                    $"{id}: {PageGranularity.PartOf(half)} pins a place differently from the document it "
                    + "came out of.");
                Assert.True(PageGranularity.PagesIn(id) == PageGranularity.PagesIn(half),
                    $"{id}: {PageGranularity.PartOf(half)} thinks the document was a different length.");
            }

            // …and the ROWS can be told apart, which is the other half of it: one citation says which sheet,
            // the other says how much of the file, and a whole document still wears nothing at all.
            Assert.Equal("", PageGranularity.RowCitation(id));
            Assert.Contains(
                $"page {PageGranularity.WorstPageOf(id)} of {PageGranularity.PagesIn(id)}",
                PageGranularity.RowCitation(PageGranularity.SheetIdOf(id)), StringComparison.Ordinal);
            Assert.Contains(
                $"{PageGranularity.BulkPagesIn(id)} pages of {PageGranularity.PagesIn(id)}",
                PageGranularity.RowCitation(PageGranularity.BulkIdOf(id)), StringComparison.Ordinal);
        }

        Assert.True(tried > 60, $"only {tried} document(s) were tried — this proves nothing.");
    }

    /// <summary>
    /// A SHEET THE ARC WROTE IS ONE SHEET, and it never comes apart.
    ///
    /// <para>Every authored paper in this game was composed as a single page — a rate schedule, the five out
    /// of the underground's designated rooms, the sheet with the lift code on it — and two of them are read
    /// back out of the sleeve BY ID by systems that would not recognise a torn one. A captain tearing "page
    /// 3 of 4" out of a document whose every word was written as one page is the sim doing one thing while
    /// the prose says another.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>FieldClue.IsAuthored</c> branch from
    /// <c>PagesIn</c>:</para>
    /// <code>
    /// kolt-premium-schedule is a sheet the arc wrote and the game says it is 4 pages long.
    /// </code>
    /// </summary>
    [Fact]
    public void A_SHEET_THE_ARC_WROTE_IsOneSheet()
    {
        var authored = new List<string> { HardcaseRep.ScheduleFindId };
        foreach (string body in new[] { "luna", "miranda", "europa" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                for (int room = 0; room < 12; room++)
                {
                    string id = UndergroundComplex.FindId(body, level, room);
                    if (FieldClue.IsAuthored(id))
                    {
                        authored.Add(id);
                    }
                }
            }
        }

        Assert.True(authored.Count >= 3,
            $"only {authored.Count} authored sheet(s) were found in this build — the sweep has stopped "
            + "reaching the arc's own paper and this law is being stated over one hard-coded id.");

        foreach (string id in authored)
        {
            Assert.True(PageGranularity.PagesIn(id) == 1,
                $"{id} is a sheet the arc wrote and the game says it is {PageGranularity.PagesIn(id)} pages "
                + "long.");
            foreach (Satchel.Kind kind in EvidenceKinds())
            {
                Assert.False(
                    PageGranularity.CanSplit(
                        kind, id, alreadyInTheBook: true, seatedForTheSpread: true),
                    $"{id} is a sheet the arc wrote and the game is offering to tear a page out of it "
                    + $"as {kind}.");
            }
        }
    }

    // ── (e) THE BULK IN A BIN READS AS LESS THAN THE WHOLE FILE ──────────────────────────────────────

    /// <summary>
    /// A FILE IN THE BIN EXPLAINS ITSELF; A MISSING FILE EXPLAINS NOTHING.
    ///
    /// <para>Owner: <i>"bin the innocent bulk, which is ALSO cover."</i> The whole point of splitting rather
    /// than shredding is what whoever empties the bin makes of what is in it, so the difference has to be a
    /// law and not a flavour: on every rung that leaves anything at all, the bulk of a split file reads as
    /// LESS than a whole file somebody tore up — and on the rung that leaves nothing, both answer the same,
    /// because there is nothing in that drawer to read as anything.</para>
    ///
    /// <para>Stated over the ordinal ranking the enum publishes (worst first, like <c>Tier</c>'s own), and
    /// over the whole ladder rather than a sampled rung.</para>
    ///
    /// <para><b>Proven RED</b> by script-changing <c>LeftInTheBin</c> to ignore its second argument
    /// (<c>=> !LeavesSomethingToFind(tier) ? Nothing : AFileSomebodyGotRidOf</c>), which is the answer the
    /// ladder gave before this issue:</para>
    /// <code>
    /// PaperBin: the bulk of a split file reads exactly as suspicious as the whole file — the split buys
    /// the captain nothing at the bin, which is the entire point of it.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_BULK_OF_A_SPLIT_ReadsAsLessThanTheWholeFile()
    {
        int bets = 0;
        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            RipAndBin.WhatIsLeft whole = RipAndBin.LeftInTheBin(tier, bulkOfASplitFile: false);
            RipAndBin.WhatIsLeft bulk = RipAndBin.LeftInTheBin(tier, bulkOfASplitFile: true);

            if (!RipAndBin.LeavesSomethingToFind(tier))
            {
                Assert.True(whole == RipAndBin.WhatIsLeft.Nothing && bulk == RipAndBin.WhatIsLeft.Nothing,
                    $"{tier} leaves nothing to find and the ladder still has an opinion about what was left.");
                continue;
            }

            bets++;
            Assert.True(bulk > whole,
                $"{tier}: the bulk of a split file reads exactly as suspicious as the whole file — the split "
                + "buys the captain nothing at the bin, which is the entire point of it.");
            Assert.Equal(RipAndBin.WhatIsLeft.AFileSomebodyGotRidOf, whole);
            Assert.Equal(RipAndBin.WhatIsLeft.NothingMuch, bulk);
        }

        Assert.True(bets >= 3,
            $"only {bets} rung(s) of the ladder are a bet — this law needs the buckets it is about.");
    }

    /// <summary>
    /// …AND THE BOOK SAYS SO, IN THE ONE PLACE A LATER ARC CAN READ IT BACK.
    ///
    /// <para>The filed note is the only durable record of a disposal (#798's own ruling: not a meter, a
    /// line), so the difference the ladder above states has to reach it. The SHAPE does not change — bucket
    /// first, document after, one flat clause about what was left behind — because a later arc reads these
    /// back and a note that changed shape would be a second format to parse. What changes is the last
    /// clause, and at the secure rung it does not change at all: there is nothing left there to read as
    /// boring.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>boring</c> branch from
    /// <c>RipAndBin.DisposalNote</c>:</para>
    /// <code>
    /// SlopBin: the book's note for a split file's bulk is the note for a whole file torn up.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_FILED_NOTE_SaysWhichOfTheTwoThingsWentInTheBin()
    {
        const string what = "📋 inspection schedule, margin list — a mention";

        foreach (RipAndBin.Tier tier in RipAndBin.Ladder)
        {
            string whole = RipAndBin.DisposalNote(what, tier);
            string bulk = RipAndBin.DisposalNote(what, tier, boring: true);

            // The shape is the same either way: the bucket is named first and the document after it.
            foreach (string note in new[] { whole, bulk })
            {
                Assert.StartsWith(
                    tier == RipAndBin.Tier.SecureDisposal ? "Fed to " : "Torn up and put in ",
                    note, StringComparison.Ordinal);
                Assert.Contains(RipAndBin.TheBin(tier), note, StringComparison.Ordinal);
                Assert.Contains(what, note, StringComparison.Ordinal);
            }

            if (RipAndBin.LeavesSomethingToFind(tier))
            {
                Assert.True(whole != bulk,
                    $"{tier}: the book's note for a split file's bulk is the note for a whole file torn up.");
                Assert.Contains(PageGranularity.BulkDisposalClause, bulk, StringComparison.Ordinal);
                Assert.DoesNotContain(PageGranularity.BulkDisposalClause, whole, StringComparison.Ordinal);
            }
            else
            {
                Assert.True(whole == bulk,
                    $"{tier}: the machine destroyed the folder and the book still says there is a boring "
                    + "file in a bin — a note about a thing that is not there.");
            }
        }
    }

    // ── (f) THE TORN SHEET IS A CHEAPER CARRY ────────────────────────────────────────────────────────

    /// <summary>
    /// #798 item 2, second cut · THE SHEET COSTS THE SLEEVE LESS THAN THE FOLDER IT CAME OUT OF.
    ///
    /// <para>Owner: <i>"pocket the one damning sheet — SMALL, HIDEABLE — and bin the innocent bulk."</i>
    /// #1185 shipped the split with both halves costing exactly what the whole document cost and said so in
    /// its own judgement calls; a captain who did the professional thing walked away carrying as much as one
    /// who stuffed the folder in their coat, and the tradecraft bought nothing the arithmetic could see.
    /// </para>
    ///
    /// <para>Three claims, and all three are measured through <b>the satchel's own arithmetic</b> rather
    /// than asserted about a constant: the sheet costs <see cref="Satchel.FoldedSheetSpace"/>, that is
    /// STRICTLY less than what the folder costs, and the folder is full price — it is the folder, it is
    /// exactly as thick as it looks, and it is on its way to a bin. Over both kinds of document, because the
    /// cost is read off the ID and a dossier's sheet is folded the same way a pay sheet's is.</para>
    ///
    /// <para><b>Proven RED</b> by script-changing <c>Satchel.SpaceCostOf</c> to <c>=> 1</c> — the arithmetic
    /// as it stood before this cut:</para>
    /// <code>
    /// Assert.True() Failure
    ///   Paper hive:doc:pages-2: the torn sheet costs the sleeve 1 and the folder costs 1 — the split
    ///   buys the captain nothing they can carry, which is the whole of what item 2 is for.
    /// </code>
    /// </summary>
    [Fact]
    public void A_TORN_SHEET_CostsTheSleeveLessThanTheFolderItCameOutOf()
    {
        TheNetHasBothKindsOfDocument();
        Assert.True(Satchel.FoldedSheetSpace < 1,
            $"a folded sheet is priced at {Satchel.FoldedSheetSpace}, which is what every other thing in "
            + "the satchel costs — there is no cheaper carry here and this guard is about nothing.");

        int proved = 0;
        foreach (Satchel.Kind kind in EvidenceKinds())
        {
            Assert.Equal(Satchel.Compartment.Sleeve, Satchel.CompartmentOf(kind));

            foreach (string id in ManyPapers().Concat(ManyDossiers())
                         .Where(PageGranularity.IsMultiPage).Take(24))
            {
                var whole = new Satchel.Item(kind, id);
                var sheet = new Satchel.Item(kind, PageGranularity.SheetIdOf(id));
                var folder = new Satchel.Item(kind, PageGranularity.BulkIdOf(id));

                // Measured the way the sleeve measures: what one thing adds to Used, which is the number
                // every capacity question in this game is answered out of.
                int forSheet = Satchel.Used([sheet], Satchel.Compartment.Sleeve);
                int forFolder = Satchel.Used([folder], Satchel.Compartment.Sleeve);
                int forWhole = Satchel.Used([whole], Satchel.Compartment.Sleeve);
                proved++;

                Assert.True(forSheet == Satchel.FoldedSheetSpace,
                    $"{kind} {id}: the torn sheet takes {forSheet} of the sleeve and the one place that "
                    + $"says what a sheet weighs says {Satchel.FoldedSheetSpace}.");
                Assert.True(forSheet < forFolder,
                    $"{kind} {id}: the torn sheet costs the sleeve {forSheet} and the folder costs "
                    + $"{forFolder} — the split buys the captain nothing they can carry, which is the whole "
                    + "of what item 2 is for.");
                Assert.True(forFolder == forWhole,
                    $"{kind} {id}: the folder costs {forFolder} and the document it came out of cost "
                    + $"{forWhole} — the bulk is the folder and is exactly as thick as it looks.");
                Assert.Equal(Satchel.FoldedSheetSpace, Satchel.SpaceCostOf(sheet));
                Assert.Equal(1, Satchel.SpaceCostOf(whole));
            }
        }

        Assert.True(proved >= 24, $"only {proved} document(s) were priced — this proves nothing.");
    }

    /// <summary>
    /// …AND IT IS A CARRY AND NOT A NUMBER: A FULL SLEEVE TAKES THE SHEET AND REFUSES THE FOLDER.
    ///
    /// <para>The arithmetic above is worth nothing unless the two verbs a captain actually meets agree with
    /// it. <see cref="Satchel.CanTake"/> is the question every haul path asks before it consumes a find
    /// (#678's whole issue), and <see cref="Satchel.Add"/> is the one that refuses politely — so a sleeve
    /// packed to its cap is offered both halves, and what it does with them is the feature: the page folded
    /// twice goes in, the folder does not.</para>
    ///
    /// <para><b>Proven RED</b> by script-reverting <c>Add</c> and <c>CanTake</c> to ask
    /// <c>IsFull(list, item.Kind)</c> — the kind-shaped question they asked before this cut:</para>
    /// <code>
    /// a sleeve with no room left in it refused the one page the whole verb exists to hand the captain.
    /// </code>
    /// </summary>
    [Fact]
    public void A_FULL_SLEEVE_TakesTheSheetAndRefusesTheFolder()
    {
        string id = ManyPapers().First(PageGranularity.IsMultiPage);
        var sheet = new Satchel.Item(Satchel.Kind.Paper, PageGranularity.SheetIdOf(id));
        var folder = new Satchel.Item(Satchel.Kind.Paper, PageGranularity.BulkIdOf(id));

        IReadOnlyList<Satchel.Item> full = [];
        for (int i = 0; i < Satchel.SleeveCapacity; i++)
        {
            full = Satchel.Add(full, new Satchel.Item(Satchel.Kind.Paper, $"hive:doc:filler-{i}"));
        }

        Assert.True(full.Count == Satchel.SleeveCapacity,
            $"the bench built a sleeve of {full.Count} against a cap of {Satchel.SleeveCapacity} — it is "
            + "not full and this guard would pass on anything.");
        Assert.True(Satchel.IsFull(full, Satchel.Kind.Paper),
            "the sleeve is at its cap and the satchel does not think it is full.");
        Assert.Equal(0, Satchel.SpaceLeft(full, Satchel.Compartment.Sleeve));

        Assert.True(Satchel.CanTake(full, sheet),
            "a sleeve with no room left in it refused the one page the whole verb exists to hand the "
            + "captain.");
        Assert.False(Satchel.CanTake(full, folder),
            "a sleeve at its cap took another folder — the cap has stopped meaning anything.");

        Assert.Contains(Satchel.Add(full, sheet), i => i.Id == sheet.Id);
        Assert.DoesNotContain(Satchel.Add(full, folder), i => i.Id == folder.Id);

        // …and the sheet going in does not move the sleeve's own figure, which is what "costs nothing"
        // means when the subtraction is the one every other caller does.
        Assert.Equal(
            Satchel.Used(full, Satchel.Compartment.Sleeve),
            Satchel.Used(Satchel.Add(full, sheet), Satchel.Compartment.Sleeve));
    }

    /// <summary>Both of this feature's authored sentences reach the canon sweep — #709's discipline. A line
    /// nobody can sweep is a line nobody can check, and the two here are the only prose the split owns.</summary>
    [Fact]
    public void EVERY_AUTHORED_LINE_IsInTheCatalog()
    {
        List<string> mine = [.. PageGranularity.AllProse()];
        Assert.Contains(PageGranularity.SplitLine, mine);
        Assert.Contains(PageGranularity.BulkDisposalClause, mine);

        // …and the bulk's clause is reachable from the BIN's catalog too, because that is the ladder that
        // actually prints it.
        Assert.Contains(
            RipAndBin.AllProse(),
            line => line.Contains(PageGranularity.BulkDisposalClause, StringComparison.Ordinal));
    }
}
