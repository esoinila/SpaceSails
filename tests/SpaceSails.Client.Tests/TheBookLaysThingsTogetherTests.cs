using System;
using System.IO;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 L3 · THE BOOK, ON THE SCREEN. Core decides what a stray is, what a verdict is, and what any of it
/// says (<c>TheBookHoldsWhatSomebodyRemembersTests</c> holds all of that). What is left is the half only the
/// client can get wrong, and it is the half this repo has paid for repeatedly: a rule that is true in the
/// model and unreachable, double-told, or spent at the wrong seam on the surface.
///
/// <para><b>Why these are source-shape guards.</b> This project has no component renderer, so every client
/// audit here reads the shipping markup and the shipping method bodies — which is also the honest shape for
/// the claims below, because all of them are about PLACEMENT and ROUTING rather than about a value.</para>
/// </summary>
public sealed class TheBookLaysThingsTogetherTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(params string[] file) =>
        MapMarkup.Read(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", "Pages", .. file]));

    /// <summary>One method body, from its signature to the next member at the same indent — the same cut the
    /// sibling client guards make, so a body read here is a body read there.</summary>
    private static string Method(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"the client no longer has `{signature}` — this guard cannot find what it audits.");
        int end = source.IndexOf("\n    }", start, StringComparison.Ordinal);
        Assert.True(end > start, $"`{signature}` does not close where this guard expects.");
        return source[start..end];
    }

    // ── THE SHEETS ARE DRAWN, WITH THEIR MARK AND THEIR TAG ──────────────────────────────────────────

    /// <summary>
    /// L5a filled the book and had nowhere to draw it: <c>_heldMemories</c> existed and the satchel showed
    /// none of it. The NOTES page renders a sheet's TEXT and the one line that says whose it is — and the
    /// byline is <c>HeldMemory.Sheet.BookLine</c> rather than a sentence built in the markup, because a
    /// second place assembling "mine · love · day 12" is a second place that has to agree with the first.
    /// </summary>
    [Fact]
    public void TheNotesPageDrawsASheetWithItsMarkAndItsTag()
    {
        string razor = Pages("Map.razor");

        Assert.Contains("TheSheets()", razor, StringComparison.Ordinal);
        Assert.Contains("held-sheet-text", razor, StringComparison.Ordinal);
        Assert.Contains("@sheet.Text", razor, StringComparison.Ordinal);
        Assert.Contains("@sheet.BookLine", razor, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.RowTitle(sheet)", razor, StringComparison.Ordinal);

        // …and the byline really does carry both axes, so this guard is about a line that says something.
        var sheet = new HeldMemory.Sheet(
            "x", HeldMemory.Mark.NotAnyones, HeldMemory.Theory.Love, "…", [], 0);
        Assert.Contains(HeldMemory.Label(HeldMemory.Mark.NotAnyones), sheet.BookLine, StringComparison.Ordinal);
        Assert.Contains(HeldMemory.Label(HeldMemory.Theory.Love), sheet.BookLine, StringComparison.Ordinal);
    }

    /// <summary>The money/love filter is on THREADS, it has all three positions, and it is wired to the ONE
    /// turn method — a second place setting <c>_bookTag</c> is a second place that has to remember to put
    /// the pen's held end down.</summary>
    [Fact]
    public void TheThreadsPageCarriesTheMoneyAndLoveFilter()
    {
        string razor = Pages("Map.razor");

        Assert.Contains("TheThreadsFilterTurnsTo(null)", razor, StringComparison.Ordinal);
        Assert.Contains("TheThreadsFilterTurnsTo(SpaceSails.Core.HeldMemory.Theory.Money)", razor, StringComparison.Ordinal);
        Assert.Contains("TheThreadsFilterTurnsTo(SpaceSails.Core.HeldMemory.Theory.Love)", razor, StringComparison.Ordinal);
        Assert.Contains("SheetStacks()", razor, StringComparison.Ordinal);

        // The filter reaches the STACKS through Core rather than being applied in the markup.
        string body = Method(Pages("Map.Book.cs"), "private IReadOnlyList<HeldMemory.Stack> SheetStacks()");
        Assert.Contains("HeldMemory.Stacks(_heldMemories, _bookTag)", body, StringComparison.Ordinal);
    }

    // ── THE SPREAD ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE RECONCILE IS SEATED-ONLY, and it is so by the law that was already there rather than by a new
    /// condition of its own. The markup for it lives inside the SPREAD page's own block, and the SPREAD tab
    /// is drawn only for a seated captain (#784) — so a guard that re-checked the posture here would be a
    /// second rule to keep in step with the first.
    /// </summary>
    [Fact]
    public void TheReconcileLivesOnTheSpreadPageWhichIsSeatedOnly()
    {
        string razor = Pages("Map.razor");

        int spread = razor.IndexOf("@if (_satchelPage == SatchelPage.Spread)", StringComparison.Ordinal);
        int notes = razor.IndexOf("else if (_satchelPage == SatchelPage.Notes)", StringComparison.Ordinal);
        int lay = razor.IndexOf("LayItOnTheSpread", StringComparison.Ordinal);

        Assert.True(spread >= 0 && notes > spread, "the satchel's page blocks are not where this guard expects.");
        Assert.InRange(lay, spread, notes);   // the lay row is INSIDE the spread block

        // The tab that reaches this page is still drawn only while seated — the law this leans on.
        Assert.Contains("@if (CaptainIsSeatedAnywhere)", razor, StringComparison.Ordinal);
    }

    /// <summary>Laying a pair reconciles through Core and applies BOTH halves of the result — the words and
    /// the effect. A table that printed a verdict without moving anything would be a mood, not a mechanic.</summary>
    [Fact]
    public void LayingAPairReconcilesThroughCoreAndWritesWhatItSays()
    {
        string body = Method(Pages("Map.Book.cs"), "private void LayItOnTheSpread(SpreadReconcile.Paper paper)");

        Assert.Contains("SpreadReconcile.Lay(_laid[^2], _laid[^1], AnAuthoredRevealFor)", body, StringComparison.Ordinal);
        Assert.Contains("ApplyTheReconcile(result)", body, StringComparison.Ordinal);
        Assert.Contains("SpreadReconcile.TheBleedAssembles(_laid)", body, StringComparison.Ordinal);
        Assert.Contains("_satchelOutcome = result.Line", body, StringComparison.Ordinal);

        // #680's law: a line said inside the satchel goes to the dialog, never to the HUD behind its blur.
        Assert.DoesNotContain("ShowPulseMessage(result.Line)", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE HIDDEN ORIGINAL COMES BACK AT THE ONE SEAM #974 KEPT IT FOR. The restore writes the ledger page
    /// clean AND marks a sheet under that id corrected — the two books, from one id, applied where each
    /// belongs. A verdict that only did one of them would leave the ledger still lying or the book still
    /// silent about having caught it.
    /// </summary>
    [Fact]
    public void ADisagreementPutsTheOriginalBackAndMarksTheMemoryCorrected()
    {
        string apply = Method(Pages("Map.Book.cs"), "private void ApplyTheReconcile(SpreadReconcile.Result result)");
        Assert.Contains("RestoreTheHiddenOriginal(result.CorrectedId)", apply, StringComparison.Ordinal);

        string restore = Method(Pages("Map.Book.cs"), "private void RestoreTheHiddenOriginal(string id)");
        Assert.Contains("FilingLine.Detail.None", restore, StringComparison.Ordinal);
        Assert.Contains("_filingBook = FilingLine.Put(", restore, StringComparison.Ordinal);
        Assert.Contains("Corrected = true", restore, StringComparison.Ordinal);

        // …and an AGREEMENT warms the memories rather than the ledger.
        Assert.Contains("Warmer(SpreadReconcile.MostConfidence)", apply, StringComparison.Ordinal);
        Assert.Contains("Naming(SpreadReconcile.NotAnyonesYet)", apply, StringComparison.Ordinal);
    }

    /// <summary>#973 · The authored-reveal hook exists, is wired into the one lay seam, and is a NAMED method
    /// rather than a missing argument — so #973 L5b's walk-in has a place to put its two cases without
    /// touching the lay path or the three general verdicts.</summary>
    [Fact]
    public void TheAuthoredRevealHookIsWiredAndNamedForTheLaneThatFillsIt()
    {
        string source = Pages("Map.Book.cs");

        Assert.Contains(
            "private SpreadReconcile.Result? AnAuthoredRevealFor(SpreadReconcile.Paper a, SpreadReconcile.Paper b)",
            source, StringComparison.Ordinal);
        Assert.Contains("SpreadReconcile.Lay(_laid[^2], _laid[^1], AnAuthoredRevealFor)", source, StringComparison.Ordinal);

        // …and the mark her note wears is already in the enum, so the lane that fills the hook adds nothing
        // to the book's vocabulary.
        Assert.Equal("hers", HeldMemory.Label(HeldMemory.Mark.Hers));
    }

    /// <summary>A grey page cannot be laid on the table. You cannot compare a page nobody has read, and a
    /// row that offered one would be the SPREAD showing the captain the contents of a page they were
    /// refused.</summary>
    [Fact]
    public void OnlyPagesTheCaptainHasWonBackAreLayable()
    {
        string body = Method(Pages("Map.Book.cs"), "private IReadOnlyList<SpreadReconcile.Paper> LayablePapers()");

        Assert.Contains("standing.IsGrey", body, StringComparison.Ordinal);
        Assert.Contains("continue;", body, StringComparison.Ordinal);
        Assert.Contains("SpreadReconcile.Paper.Of(page, standing", body, StringComparison.Ordinal);
    }

    /// <summary>The table is cleared by STANDING UP — the SPREAD is seated-only by law, so a laid pair that
    /// outlived the chair would be state the player could not see, reach, or put down. And it is cleared by
    /// a new voyage, because the papers are drawn out of two books that a new voyage empties.</summary>
    [Fact]
    public void StandingUpAndStartingOverBothClearTheTable()
    {
        Assert.Contains(
            "ClearTheSpread();",
            Method(Pages("Map.Seated.cs"), "private void StandUpFromTable()"),
            StringComparison.Ordinal);

        Assert.Contains(
            "ClearTheSpread();",
            Method(Pages("Map.OldCrew.cs"), "private void ForgetTheOldCrew()"),
            StringComparison.Ordinal);
    }

    // ── THE STRAYS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE STRAY IS ASKED INSIDE THE <i>NOTHING</i> ARM AND NOWHERE ELSE. That placement is the whole
    /// cadence: a rarity reachable from the good outcomes would take a recollection away from a captain who
    /// rolled one, and a rarity asked before the roll would not be an outcome at all.
    /// </summary>
    [Fact]
    public void AStrayIsOnlyEverAskedForInsteadOfNothing()
    {
        string source = Pages("Map.FilingLine.cs");
        string body = Method(source, "private void ReadTheGreyPage(string entryId)");

        int nothingArm = body.IndexOf("if (outcome == Flashback.Outcome.Nothing)", StringComparison.Ordinal);
        int asked = body.IndexOf("AStrayComesBackInstead(entryId)", StringComparison.Ordinal);

        Assert.True(nothingArm >= 0, "the filing line no longer has a nothing arm.");
        Assert.True(asked > nothingArm, "the stray is not asked for inside the nothing arm.");
        Assert.Equal(1, Occurrences(source, "AStrayComesBackInstead(entryId)"));

        // …and the ordinary "nothing" line is not said over it. Two sentences for one press is the one
        // thing a rarity must never do.
        Assert.Contains("if (!stray)", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE THREE CADENCES ARE CORE'S, ASKED HERE. The count of grey pages already read this life comes off
    /// the filing book (which is what makes "never the first read" survive a reload), the draw is without
    /// replacement against the stray sheets the book already holds, and the price is the pip and the beat a
    /// wrong page pays — because from the outside it is the same act.
    /// </summary>
    [Fact]
    public void TheStraysCadencesAndPriceAreAllReadOffCore()
    {
        string body = Method(Pages("Map.Book.cs"), "private bool AStrayComesBackInstead(string entryId)");

        Assert.Contains("FilingLine.GreyPagesReadThisLife(_filingBook)", body, StringComparison.Ordinal);
        Assert.Contains("Flashback.AStrayComesInstead(", body, StringComparison.Ordinal);
        Assert.Contains("Flashback.IsStrayId(held.Id)", body, StringComparison.Ordinal);
        Assert.Contains("Flashback.DrawStray(drawn, seed)", body, StringComparison.Ordinal);
        Assert.Contains("Flashback.StrayToast(text)", body, StringComparison.Ordinal);
        Assert.Contains("Flashback.StrayNervePips", body, StringComparison.Ordinal);
        Assert.Contains("StoryBeats.Beat.Flashback", body, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Mark.NotAnyones", body, StringComparison.Ordinal);
    }

    // ── THE SIGNING ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SHEET IS FILED ON THE SAME EDGE AS THE PLATE AND BEHIND THE SAME LATCH. L2 built a once-per-life
    /// latch for the signing plate; the sheet rides it rather than growing a second condition, so there is
    /// nothing for a later lane to forget to keep in step — and the text is
    /// <c>NebulaRep.SigningMemoryFor</c>, so the reborn captain's extra line cannot be assembled two ways.
    /// </summary>
    [Fact]
    public void TheSigningSheetIsFiledOnTheSameEdgeAsThePlate()
    {
        string rep = Method(Pages("Map.Rep.cs"), "private void TellHimYouAlreadyHaveOne()");

        int latch = rep.IndexOf("_repSigningToldInLife = CaptainsLife", StringComparison.Ordinal);
        int filed = rep.IndexOf("FileTheSigningSheet()", StringComparison.Ordinal);
        Assert.True(latch >= 0 && filed > latch, "the sheet is not filed inside the once-per-life latch.");

        string book = Method(Pages("Map.Book.cs"), "private void FileTheSigningSheet()");
        Assert.Contains("NebulaRep.SigningMemoryFor(RetiredCaptainCount)", book, StringComparison.Ordinal);
        Assert.Contains("NebulaRep.SigningMemoryId", book, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Mark.Mine", book, StringComparison.Ordinal);
        Assert.Contains("HeldMemory.Theory.Money", book, StringComparison.Ordinal);

        // …and a rebirth GROWS the sheet rather than replacing it with a blank one: what was already
        // earned on it — the confidence, the names it writes down — rides across.
        Assert.Contains("had?.Confidence ?? 0", book, StringComparison.Ordinal);
        Assert.Contains("had?.Threads ?? []", book, StringComparison.Ordinal);
    }

    // ── THE SHARD ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The lattice assembles through the arc's own seam (<c>TryAssembleNebula</c>) rather than
    /// through a second path — so the convergence check, the vault save and the once-per-universe rule all
    /// happen exactly as they do for every other shard. And it raises no plate, because
    /// <c>NebulaLore.PlateFor</c> has none for it: the SPREAD is its host.</summary>
    [Fact]
    public void TheBleedGoesThroughTheArcsOwnAssembleSeamAndRaisesNoPlate()
    {
        string body = Method(Pages("Map.Book.cs"), "private void TheBleedComesTogether()");

        Assert.Contains("TryAssembleNebula(SpreadReconcile.TheBleedId", body, StringComparison.Ordinal);
        Assert.Null(NebulaLore.PlateFor(SpreadReconcile.TheBleedId));
        Assert.NotNull(NebulaLore.ById(SpreadReconcile.TheBleedId));
    }

    // ── THE ARC'S LAW ────────────────────────────────────────────────────────────────────────────────

    /// <summary>The word the arc never says, swept over the markup this lane added. A literal typed into a
    /// razor file is the one place a canon sweep of Core would never look.</summary>
    [Fact]
    public void TheMarkupThisLaneAddedNamesNothing()
    {
        string razor = Pages("Map.razor");
        int from = razor.IndexOf("#973 L3 · THE RECONCILE", StringComparison.Ordinal);
        Assert.True(from >= 0, "the reconcile markup is gone.");

        string book = Pages("Map.Book.cs");
        foreach (string forbidden in new[] { "restore from", "clone", "cadaver" })
        {
            Assert.DoesNotContain(forbidden, razor, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(forbidden, book, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static int Occurrences(string haystack, string needle)
    {
        int count = 0;
        int at = 0;
        while ((at = haystack.IndexOf(needle, at, StringComparison.Ordinal)) >= 0)
        {
            count++;
            at += needle.Length;
        }

        return count;
    }
}
