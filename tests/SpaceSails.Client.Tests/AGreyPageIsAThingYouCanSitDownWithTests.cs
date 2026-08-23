using System;
using System.IO;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 L1 · THE FILING LINE, ON THE SCREEN. Core decides which pages grey, what a roll settles into and
/// which detail a wrong recollection moves (<c>ThePagesYouDontRememberWritingTests</c> holds all of that).
/// What is left is the half only the client can get wrong, and it is the half this repo has paid for
/// repeatedly: a rule that is true in the model and invisible, unreachable or double-told on the surface.
///
/// <para><b>Why these are source-shape guards.</b> This project has no component renderer — every client
/// audit here reads the shipping markup and the shipping method bodies, which is also the honest shape for
/// the claims below, because all four are about PLACEMENT and ROUTING rather than about a value:
/// <list type="bullet">
/// <item>the mark and the label are on the row and come from Core's one copy of them, not from a glyph
/// somebody typed into the desk;</item>
/// <item>only a GREY row is clickable, and the row's own action buttons do not roll it by accident;</item>
/// <item>"nothing" gets no card — the seam is not spent on a memory that did not arrive;</item>
/// <item>the wake notice rides the succession block the death card already raises, because the brief said
/// reuse its surface and adding a twelfth panel to the wake would have been the easy way to be wrong.</item>
/// </list></para>
/// </summary>
public sealed class AGreyPageIsAThingYouCanSitDownWithTests
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
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", "Pages", .. file]));

    /// <summary>The Captain desk's "Tips, intel &amp; rumors" block, cut out of the ledger view. Cutting it is
    /// the point: a mark rendered somewhere else in this 880-line component is a mark on a different section.</summary>
    private static string TheTipsSection()
    {
        string razor = Pages("Stations", "Captain.razor");
        int start = razor.IndexOf("🔭 Tips, intel", StringComparison.Ordinal);
        Assert.True(start >= 0, "Captain.razor no longer has the tips section this guard knows how to find.");
        int end = razor.IndexOf("🗺 Treasure maps", start, StringComparison.Ordinal);
        Assert.True(end > start, "…and no longer has the treasure-map section that ends it.");
        return razor[start..end];
    }

    /// <summary>One method body, from its signature to the next member at the same indent — the same cut
    /// <c>TheOutcomeIsOnThePopUpTests</c> makes, so a body read here is a body read there.</summary>
    private static string Method(string source, string signature)
    {
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"the source no longer has `{signature}` where this guard can read it.");
        int end = source.IndexOf("\n    private ", at + 1, StringComparison.Ordinal);
        return source[at..(end > at ? end : source.Length)];
    }

    // ── The row ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A GREY ROW WEARS THE MARK AND SAYS WHAT THE MARK MEANS. The greying alone is not the deliverable:
    /// a dimmed row on an already-dark ledger is not a difference everybody can see, and it is not a
    /// difference anybody can NAME. So the ⟂ and the sentence ride the row itself.
    /// </summary>
    [Fact]
    public void AGreyRowIsDrawnWithTheMarkAndTheLabel()
    {
        string tips = TheTipsSection();

        Assert.Contains("captain-ledger-unremembered", tips, StringComparison.Ordinal);
        Assert.Contains("@GreyPageMark", tips, StringComparison.Ordinal);
        Assert.Contains("@GreyPageLabel", tips, StringComparison.Ordinal);

        // …and the greyed CLASS is applied off the row's own Grey flag rather than to the whole section.
        Assert.Contains("t.Grey ? \"captain-ledger-unremembered\"", tips, StringComparison.Ordinal);

        // …and the class it names is really styled, so "greyed" is a fact about pixels and not a class name
        // nobody ever wrote a rule for.
        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Stations", "Captain.razor.css"));
        Assert.Contains(".captain-ledger-unremembered {", css, StringComparison.Ordinal);
        Assert.Contains("grayscale", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE GLYPH AND THE SENTENCE HAVE ONE HOME. Both come down as parameters out of Core's own constants;
    /// the desk never types either of them. Two places writing one sentence is the drift this codebase keeps
    /// paying for, and a ⟂ typed into markup is a ⟂ that will not change when the mark does.
    /// </summary>
    [Fact]
    public void TheMarkAndTheLabelComeFromCoreAndAreNotTypedIntoTheDesk()
    {
        string map = Pages("Map.razor");
        Assert.Contains("GreyPageMark=\"@FilingLine.Mark\"", map, StringComparison.Ordinal);
        Assert.Contains("GreyPageLabel=\"@FilingLine.Label\"", map, StringComparison.Ordinal);

        string desk = Pages("Stations", "Captain.razor");
        Assert.DoesNotContain(FilingLine.Mark, desk, StringComparison.Ordinal);
        Assert.DoesNotContain(FilingLine.Label, desk, StringComparison.Ordinal);
    }

    /// <summary>
    /// ONLY A GREY ROW IS A THING YOU CAN SIT DOWN WITH, and the row's own action buttons do not roll it by
    /// accident. The second half is the bug this would otherwise have shipped: a scope tip is dated, so it can
    /// grey — and clicking its 🔭 button would have bubbled to the card and spent the page's one roll for the
    /// whole life on a press that was about the telescope.
    /// </summary>
    [Fact]
    public void OnlyAGreyRowRollsAndItsOwnButtonsDoNot()
    {
        string desk = Pages("Stations", "Captain.razor");
        string readIfGrey = Method(desk, "private Task ReadIfGrey(LedgerTip tip)");

        Assert.Contains("tip.Grey", readIfGrey, StringComparison.Ordinal);
        Assert.Contains("Task.CompletedTask", readIfGrey, StringComparison.Ordinal);

        string tips = TheTipsSection();
        int buttons = CountOf(tips, "<button");
        int stops = CountOf(tips, "@onclick:stopPropagation");
        Assert.True(buttons > 0, "the tips section has no buttons left — this guard is asking nothing.");
        Assert.Equal(buttons, stops);
    }

    /// <summary>The desk is handed the ledger AS THIS CAPTAIN REMEMBERS IT — the marked rows and the moved
    /// details — rather than the raw one. Passing <c>LedgerTips()</c> here would have rendered a perfect book
    /// with a filing line nobody could see.</summary>
    [Fact]
    public void TheDeskDrawsTheRememberedLedgerAndNotTheTrueOne()
    {
        string map = Pages("Map.razor");

        Assert.Contains("Tips=\"LedgerTipsAsRemembered()\"", map, StringComparison.Ordinal);
        Assert.Contains("OnReadGreyPage=\"ReadTheGreyPage\"", map, StringComparison.Ordinal);
        Assert.DoesNotContain("Tips=\"LedgerTips()\"", map, StringComparison.Ordinal);
    }

    // ── The press ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EXACTLY ONE OF THE THREE OUTCOMES, AND THE MATCHING SURFACE. The press writes one state, says one
    /// sentence, and raises the beat for the two that arrived — <b>"nothing" gets no card</b>, because a
    /// picture of a memory that did not come back is the story-card seam spent on an absence, which is the
    /// "too repetitive" half of #528's law arriving through the back door.
    /// </summary>
    [Fact]
    public void EachOutcomeWritesItsOwnStateAndOnlyTheTwoThatArrivedRaiseTheBeat()
    {
        string body = Method(Pages("Map.FilingLine.cs"), "private void ReadTheGreyPage(string entryId)");

        // one roll, once — the latch is asked before anything is spent
        Assert.Contains("MayBeRead", body, StringComparison.Ordinal);
        Assert.Contains("Flashback.Roll(", body, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(body, @"Flashback\.Roll\("));

        // the three states, each written by its own arm
        Assert.Contains("FilingLine.PageState.Refused", body, StringComparison.Ordinal);
        Assert.Contains("FilingLine.PageState.CameBackWrong", body, StringComparison.Ordinal);
        Assert.Contains("FilingLine.PageState.CameBack", body, StringComparison.Ordinal);

        // the beat, once, with the entry id as its subject — and after the Nothing arm has already returned
        int nothingArm = body.IndexOf("Outcome.Nothing", StringComparison.Ordinal);
        int beat = body.IndexOf("RaiseStoryBeat(StoryBeats.Beat.Flashback, entryId)", StringComparison.Ordinal);
        Assert.True(nothingArm >= 0 && beat > nothingArm,
                    "the flashback beat is raised with the entry id as its subject, below the arm that "
                    + "returns for `nothing` — a beat above it would put a card up for a memory that never came.");
        Assert.Equal(1, CountOf(body, @"RaiseStoryBeat\("));

        int returnInNothing = body.IndexOf("return;", nothingArm, StringComparison.Ordinal);
        Assert.True(returnInNothing > nothingArm && returnInNothing < beat,
                    "the `nothing` arm must return before the beat — otherwise every refusal gets a plate.");
    }

    /// <summary>
    /// A WRONG PAGE COSTS A PIP, AND IT IS THE ONLY NERVE THIS LANE SPENDS (#226's sanity seam; owner ruling
    /// 2026-08-23 §4). A page that came back whole must not be taxed, and a refusal must not be either —
    /// nothing happened, and nothing is not frightening.
    /// </summary>
    [Fact]
    public void OnlyAPageThatCameBackWrongCostsNerve()
    {
        string file = Pages("Map.FilingLine.cs");
        string body = Method(file, "private void ReadTheGreyPage(string entryId)");

        Assert.Equal(1, CountOf(file, @"ApplyNerveShock\("));

        int guard = body.IndexOf("if (wrong)", StringComparison.Ordinal);
        int shock = body.IndexOf("ApplyNerveShock(", StringComparison.Ordinal);
        Assert.True(guard >= 0 && shock > guard,
                    "the pip is spent inside the `wrong` arm and nowhere else.");

        Assert.Contains("Flashback.WrongPageNervePips", body, StringComparison.Ordinal);
        Assert.Contains("Flashback.WrongPageNerveLabel", body, StringComparison.Ordinal);
    }

    // ── The wake, and the keeping ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WAKE NOTICE RIDES A SURFACE THAT ALREADY EXISTS. One line, once per rebirth, inside the succession
    /// block the death card has raised since Evening wind #20 — not a twelfth panel on a wake that is already
    /// four panels long, and not a card of its own on top of a card.
    /// </summary>
    [Fact]
    public void TheWakeNoticeIsOneLineOnTheSuccessionBlockAndNotANewCard()
    {
        string map = Pages("Map.razor");

        int block = map.IndexOf("busted-succession-head", StringComparison.Ordinal);
        int close = map.IndexOf("busted-close-row", block, StringComparison.Ordinal);
        Assert.True(block >= 0 && close > block, "Map.razor no longer has the succession block on the wake card.");

        string succession = map[block..close];
        Assert.Contains("bust.FilingNotice", succession, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(map, @"bust\.FilingNotice"));

        // …and it is set once, at the succession seam, off Core's own sentence.
        string busted = Pages("Map.Combat.Busted.cs");
        Assert.Contains("b.FilingNotice = MarkTheBookAtTheFilingLine();", busted, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(busted, @"MarkTheBookAtTheFilingLine\(\)"));
    }

    /// <summary>
    /// THE MARKS, THE LATCH AND THE HIDDEN ORIGINALS RIDE THE VAULT — and a NEW voyage starts with none of
    /// them. The second half is the #563 lesson this file inherits: <c>ResetForNewGame</c>'s contract is that
    /// it is the exact inverse of <c>BuildVault</c>, and a book of grey rows carried into a fresh universe
    /// would grey the pages of a ledger that does not exist yet.
    /// </summary>
    [Fact]
    public void TheFilingRidesTheVaultAndANewVoyageStartsWithNothingMarked()
    {
        string vault = Pages("Map.Vault.cs");

        Assert.Contains("Filing = BuildFilingSection(),", vault, StringComparison.Ordinal);
        Assert.Contains("RestoreFilingSection(vault.Filing);", vault, StringComparison.Ordinal);
        Assert.Contains("_filingBook = [];", vault, StringComparison.Ordinal);
    }

    /// <summary>The rows that can grey are exactly the rows the ledger DATES. A row with no stamp — the
    /// standing note, the two arc readouts — carries no entry id, so the filing line can never take it away;
    /// and every row that does carry one carries its stamp too, or it could be marked and never unmarked.</summary>
    [Fact]
    public void OnlyADatedRowCanEverGrey()
    {
        string ledger = Pages("Map.Quests.Ledger.cs");
        int ids = CountOf(ledger, @"EntryId: ");
        int stamps = CountOf(ledger, @"SimTime: ");

        Assert.True(ids >= 5, $"only {ids} ledger rows carry an entry id — the filing line has almost nothing "
                              + "to take away, which is not the ledger this lane was written against.");
        Assert.Equal(ids, stamps);

        // …and the standing note, which is evergreen background, is not one of them.
        Assert.Contains("\"⚓ Ports come in twos\"", ledger, StringComparison.Ordinal);
        int standing = ledger.IndexOf("\"⚓ Ports come in twos\"", StringComparison.Ordinal);
        int endOfIt = ledger.IndexOf("));", standing, StringComparison.Ordinal);
        Assert.DoesNotContain("EntryId:", ledger[standing..endOfIt]);
    }

    /// <summary>How many times a pattern occurs in a blob. A local helper rather than a raw
    /// <c>Regex.Matches(...).Count</c> at each site, which xUnit's analyzer reads as a collection-size
    /// assertion and refuses.</summary>
    private static int CountOf(string haystack, string pattern) =>
        System.Text.RegularExpressions.Regex.Matches(haystack, pattern).Count;

    /// <summary>The flashback's canvas is one plate for every memory — a fixed painting, not a pool keyed by
    /// the ledger row, which would be a set nobody could ever finish painting.</summary>
    [Fact]
    public void TheFlashbackPlateIsOnePaintingForEveryMemory()
    {
        Assert.Equal("art/flashback.jpg", StoryBeats.ArtFile(StoryBeats.Beat.Flashback));
        Assert.Equal(
            StoryBeats.ArtFile(StoryBeats.Beat.Flashback),
            StoryBeats.ArtFile(StoryBeats.Beat.Flashback, "plunder:1"));
        Assert.Equal(StoryBeats.Presentation.Plate, StoryBeats.PresentationOf(StoryBeats.Beat.Flashback));
    }
}
