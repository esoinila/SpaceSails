using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #784 phase two · SITTING IS NOT A CUTSCENE — the frame docks, the room keeps running, and the papers come
/// out on the table.
///
/// <para>Owner, live 2026-08-08 night, reading his own screenshot of a taken table — the card centred, the
/// backdrop dimming the whole hall AND the park behind its glass to near-black: <i>"I would like to be able
/// to see the NPCs move with A* and press I to open inventory and process the loot into my detective book.
/// Each item would have that digging-like progress bar… we are digging for info and understanding."</i> And
/// the frame law in one clause: <i>"the seated frame docks, it does not dim… the full card returns only for
/// conversations."</i></para>
///
/// <h3>The shape of the bug, and why the guards look like this</h3>
///
/// <para>The defect was a DOM element that should not exist. #780 shipped the mirror image of it — a menu
/// that had slid under a scrim, present in the markup and unreadable on the screen — and the lesson there
/// was that a guard which only asserts presence cannot see a thing being buried. Read in reverse: the guard
/// for a frame that must not dim has to assert the scrim's ABSENCE, in the branch that is actually rendered
/// while the captain is sitting there.</para>
/// </summary>
public sealed class SittingIsNotACutsceneTests
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

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    /// <summary>#870 · The sim page is nine partials by subject now, so "the sim" a guard reads over is all
    /// of them — exactly the text it read out of one file before the split.</summary>
    private static string Sim() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Sim*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>#870 · The surface page is fifteen partials by subject now, so "the surface" a guard
    /// counts over is all of them — exactly the text it read out of one file before the split.</summary>
    private static string Surface() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Surface*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>#870 · The deck page is seven partials by subject now, so "the deck" a guard reads over is
    /// all of them — exactly the text it read out of one file before the split.</summary>
    private static string Deck() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Deck*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    private static string Doc(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", name));

    /// <summary>The whole seated region of Map.razor — from the one guard that opens it to the satchel block
    /// that follows. Both branches are inside it, which is the point: the assertions below are about WHICH
    /// branch draws WHAT.</summary>
    private static string SeatedRegion()
    {
        string razor = Source("Pages", "Map.razor");
        int start = razor.IndexOf("@if (SeatedTable is { } tab)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the seated region this guard knows how to find.");
        int end = razor.IndexOf("@if (TheStandUpConfirmIsUp)", start, StringComparison.Ordinal);
        Assert.True(end > start, "the seated region no longer ends at the stand-up confirm.");
        return razor[start..end];
    }

    /// <summary>The DOCKED branch alone — everything between <c>@if (SeatedIsDocked)</c> and the
    /// <c>else</c> that raises the card.</summary>
    private static string DockedBranch()
    {
        string region = SeatedRegion();
        int start = region.IndexOf("@if (SeatedIsDocked)", StringComparison.Ordinal);
        Assert.True(start >= 0,
            "there is no docked branch at all — the seated frame is still a card in every state.");
        int end = region.IndexOf("\n    else", start, StringComparison.Ordinal);
        Assert.True(end > start, "the docked branch has no else beside it, so nothing raises the card.");
        return region[start..end];
    }

    // ── (a) THE FRAME DOCKS: A STRIP, AND NO SCRIM AT ALL ─────────────────────────────────────────────

    /// <summary>
    /// #784 · SEATED ALONE, THE HALL STAYS LIT. The docked branch draws the strip and draws NO backdrop —
    /// asserted as an absence, because a backdrop is exactly the kind of defect that is invisible to a guard
    /// which only checks that the right things are present.
    ///
    /// <para><b>Proven RED</b> by putting the card branch back: see the PR body for the run.</para>
    /// </summary>
    [Fact]
    public void SEATED_IDLE_DrawsTheStripAndNoBackdropAtAll()
    {
        string docked = DockedBranch();

        // The strip is there, and it is a HUD element rather than a card.
        Assert.Contains("class=\"seated-dock\"", docked, StringComparison.Ordinal);

        // …AND THE SCRIM IS NOT. Every backdrop class this stylesheet has, checked one at a time, so a
        // future card family cannot sneak a dimmer into the seated state under a new name.
        foreach (string scrim in new[]
        {
            "view-object-backdrop", "mission-celebration-backdrop", "convergence-backdrop",
            "start-picker-backdrop", "pin-backdrop", "rescue-backdrop", "busted-backdrop",
            "er-scrim",
        })
        {
            Assert.DoesNotContain(scrim, docked, StringComparison.Ordinal);
        }

        // Nor does it wear the modal card family's skin, which is the other way the dimming could come back:
        // .table-card drops its anchor precisely so a backdrop can centre it.
        Assert.DoesNotContain("table-card", docked, StringComparison.Ordinal);
        Assert.DoesNotContain("deck-offer-card", docked, StringComparison.Ordinal);

        // The CONVERSATION keeps its card, its backdrop, its picture and its prose — the frame changed and
        // #778/#787's content did not.
        string region = SeatedRegion();
        int card = region.IndexOf("\n    else", StringComparison.Ordinal);
        string conversation = region[card..];
        Assert.Contains("view-object-backdrop", conversation, StringComparison.Ordinal);
        Assert.Contains("deck-offer-card table-card", conversation, StringComparison.Ordinal);
        Assert.Contains("tab.ArtUrl", conversation, StringComparison.Ordinal);

        // And the fork is the state machine's, in one place, rather than a condition written into markup.
        // #865 · …and it forks on WHO CHOSE THIS SEAT and not on the chair opposite being empty. It read
        // `_table is { Solo: true }` until the owner ruled that joining a stranger's top keeps the room
        // visible: Solo is OCCUPANCY (the fact the privacy ladder reads) and the frame is a different
        // question with a different answer at exactly one table — the one you asked to sit at.
        string seated = Source("Pages", "Map.Seated.cs");
        Assert.Contains("private bool SeatedIsDocked => _table is { TheyCameToYou: false };", seated,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// #784 · THE STRIP IS PINNED TO THE FOOT OF THE DECK AND TAKES NO SCREEN. A strip tall enough to cover
    /// the tables would be the modal back in a low hat — the owner's whole complaint was that the panel
    /// stood between him and the room.
    /// </summary>
    [Fact]
    public void THE_STRIP_IsAHudAndNotACardInTheStylesheetEither()
    {
        string css = Source("Pages", "Map.razor.css");
        int at = css.IndexOf(".seated-dock {", StringComparison.Ordinal);
        Assert.True(at >= 0, "the docked strip has no rule of its own — it is inheriting a card's.");
        string rule = css[at..css.IndexOf('}', at)];

        // Pinned to the foot, bounded, and never full-screen.
        Assert.Contains("position: absolute", rule, StringComparison.Ordinal);
        Assert.Contains("bottom:", rule, StringComparison.Ordinal);
        Assert.Contains("max-height:", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("inset: 0", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("backdrop-filter", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("top: 0", rule, StringComparison.Ordinal);

        // #299 · Its layer is a BAND, never a hand-tuned number.
        Match z = Regex.Match(rule, @"z-index:\s*(?<expr>[^;]+);");
        Assert.True(z.Success, "the strip has no z-index, so its layer is whatever the DOM order gives it.");
        Assert.Contains("var(--z-", z.Groups["expr"].Value, StringComparison.Ordinal);
    }

    // ── (b, client half) THE CUSTOMER LINE READS THE MECHANIC'S OWN MEMBERS ───────────────────────────

    /// <summary>
    /// #784/#740 · THE STRIP CANNOT DISAGREE WITH THE REST ENGINE, BY CONSTRUCTION.
    ///
    /// <para>The pour window is ONE member — <c>PourSecondsLeft</c> — and the boolean the rest engine reads
    /// is derived from it. Written as two members they would be two clocks, and a strip saying "2m of cold
    /// left" on a beat the rest engine had already decided was dry is #740's fault with a chair under it.</para>
    /// </summary>
    [Fact]
    public void THE_CUSTOMER_LINE_ReadsTheSameMembersTheMechanicsRunOn()
    {
        string seated = Source("Pages", "Map.Seated.cs");

        // One window, and the flag is derived from it rather than measured beside it.
        Assert.Contains("private bool APourInFrontOfYou => PourSecondsLeft is not null;", seated,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(seated, @"DrinkInHandSeconds \* 1000\.0"));

        // The line itself hands Core the ledger, the ceiling and the fill — no arithmetic of its own.
        int at = seated.IndexOf("private string? SeatedCustomerLine()", StringComparison.Ordinal);
        Assert.True(at > 0, "there is no customer line.");
        string body = seated[at..(at + 900)];
        Assert.Contains("ex.RestPipsEased.TryGetValue(ex.CanteenWatch", body, StringComparison.Ordinal);
        Assert.Contains("ShortRest.NervePipCapPerWatch", body, StringComparison.Ordinal);
        Assert.Contains("SittingAlone.Fill(ex.CanteenWatch)", body, StringComparison.Ordinal);
        Assert.Contains("PourSecondsLeft", body, StringComparison.Ordinal);
        Assert.Contains("SeatedHud.CustomerLine(", body, StringComparison.Ordinal);

        // …and the strip draws THAT and never a sentence of its own.
        Assert.Contains("SeatedCustomerLine()", DockedBranch(), StringComparison.Ordinal);
    }

    // ── (c) [I] SEATED OPENS PROCESSING; STANDING IT DOES NOT ─────────────────────────────────────────

    /// <summary>
    /// #784 · THE I KEY IS THE DOOR TO THE SPREAD, AND POSTURE IS THE ONLY THING THAT FORKS IT.
    ///
    /// <para>Owner: <i>"press I to open inventory and process the loot into my detective book."</i> Standing,
    /// the key does exactly what it has always done. The fork lives at the one place an open happens, so a
    /// mouse cannot miss it — and the SPREAD tab is not drawn at all on your feet, because on your feet this
    /// is not a page being refused, it is a page that does not apply.</para>
    /// </summary>
    [Fact]
    public void THE_I_KEY_OpensTheSpreadSeatedAndThePocketStanding()
    {
        // The key is still one key, and it still reaches the satchel.
        string deck = Deck();
        Assert.Contains("case \"i\" or \"I\":", deck, StringComparison.Ordinal);
        Assert.Contains("ToggleSatchel();", deck, StringComparison.Ordinal);

        // The fork is at the OPEN, and it is posture and nothing else.
        string surface = Surface();
        Assert.Contains(
            "_satchelPage = CaptainIsSeatedAnywhere ? SatchelPage.Spread : SatchelPage.Carried;",
            surface, StringComparison.Ordinal);

        // The page exists as a real page and not as a mode on another one.
        Assert.Contains("Spread,", surface, StringComparison.Ordinal);

        // The tab follows the posture: no seat, no tab.
        string razor = Source("Pages", "Map.razor");
        int tab = razor.IndexOf("🗂 SPREAD", StringComparison.Ordinal);
        Assert.True(tab > 0, "the spread has no tab in the satchel.");
        int gate = razor.LastIndexOf("@if (CaptainIsSeatedAnywhere)", tab, StringComparison.Ordinal);
        Assert.True(gate > 0 && tab - gate < 700,
            "the spread tab is drawn under some condition other than being in a seat.");

        // …and the page's rows press the deliberate write, which is the only way into the dig.
        int page = razor.IndexOf("@if (_satchelPage == SatchelPage.Spread)", StringComparison.Ordinal);
        Assert.True(page > 0, "the satchel has no spread page.");
        string body = razor[page..razor.IndexOf("else if (_satchelPage == SatchelPage.Notes)", page,
            StringComparison.Ordinal)];
        // …through the seam that gives the keys back. A press on this page CLOSES the dialog (#696 shuts the
        // pocket so the deck comes back), and a click that closes a dialog leaves focus on a control that no
        // longer exists — after which the map takes no keys at all. Found by playing this build in a
        // browser: the dig ran, the strip said so, and [I] and WASD were both dead afterwards. Same bug #746
        // found after "Take your leave", same fix.
        Assert.Contains("SpreadDigClicked(dig)", body, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"() => WriteItUp(dig)\"", body, StringComparison.Ordinal);

        string seatedSrc = Source("Pages", "Map.Seated.cs");
        int clicked = seatedSrc.IndexOf("private async Task SpreadDigClicked(", StringComparison.Ordinal);
        Assert.True(clicked > 0, "the spread row has no way home for the mouse.");
        string clickedBody = seatedSrc[clicked..(clicked + 700)];
        Assert.Contains("WriteItUp(item);", clickedBody, StringComparison.Ordinal);
        Assert.Contains("if (!_showSatchel)", clickedBody, StringComparison.Ordinal);
        Assert.Contains("await RefocusMap();", clickedBody, StringComparison.Ordinal);
        Assert.Contains("SeatedSpread.SpreadBlurb", body, StringComparison.Ordinal);
        Assert.Contains("SeatedSpread.NothingToWorkLine", body, StringComparison.Ordinal);

        // #212/#603 · The control is never disabled and never hidden: the bar desk's refusal is a SENTENCE
        // you get by pressing, which is how the gumshoe rule gets learned.
        Assert.DoesNotContain("disabled=", body, StringComparison.Ordinal);
    }

    // ── (d) THE DIG TAKES ITS TIME, AND ITS PRODUCT LANDS IN THE BOOK ─────────────────────────────────

    /// <summary>
    /// #784/#696 · THE SEATED WRITE-UP RUNS ON THE ONE CHANNEL, DRAWS THE ONE BAR, AND CANNOT COMPLETE EARLY.
    ///
    /// <para>Owner: <i>"The inventory processing should have like a timer progress bar like the digging
    /// has."</i> Reuse, not a second progress language — so the assertions are about the SEAM: the hold that
    /// already exists, the precedence ladder that already picks the bar's winner, and the one predicate
    /// (<see cref="Processing.Done"/>) that decides what finished means.</para>
    ///
    /// <para>THE TIMER MUST BE ABLE TO FAIL. An instant completion — a <c>CompleteProcessing</c> reachable
    /// without either the clock being full or the QA cheat having switched the clock off — is the exact
    /// defect that would make every other assertion here pass over a feature that does nothing. Both call
    /// sites are pinned.</para>
    /// </summary>
    [Fact]
    public void THE_DIG_TakesItsTimeAndThenTheBookGainsTheEntry()
    {
        string seated = Source("Pages", "Map.Seated.cs");
        string surface = Surface();

        // ONE CHANNEL: the write starts #696's hold rather than a clock of its own.
        Assert.Contains("BeginProcessing(ex, Core.Processing.Work.Write, item, standing, null);", seated,
            StringComparison.Ordinal);
        Assert.DoesNotContain("_writeChannel", seated, StringComparison.Ordinal);
        Assert.DoesNotContain("SecondsPerWriteUp", seated + surface, StringComparison.Ordinal);

        // ONE BAR: the glyph is Core's per-work choice, so the mark over the captain's head cannot say
        // "photographing" while the sim writes into the book (#562).
        int glyph = surface.IndexOf("private static string SurfaceChannelGlyph(", StringComparison.Ordinal);
        Assert.True(glyph > 0);
        string ladder = surface[glyph..(glyph + 700)];
        Assert.Contains("Core.Processing.GlyphFor(hold.Work)", ladder, StringComparison.Ordinal);

        // THE CLOCK CAN FAIL. Every completion is behind the one predicate or behind the explicit
        // zero-length QA branch, and there is no third way to reach the far end.
        Assert.Equal(3, Regex.Matches(surface, @"CompleteProcessing\(").Count);
        int step = surface.IndexOf("private void StepProcessing(", StringComparison.Ordinal);
        string stepBody = surface[step..(step + 900)];
        int done = stepBody.IndexOf("Core.Processing.Done(hold.Elapsed, ProcessingSeconds)",
            StringComparison.Ordinal);
        int fires = stepBody.IndexOf("CompleteProcessing(ex, hold);", StringComparison.Ordinal);
        Assert.True(done > 0 && fires > done,
            "the hold completes without the clock being asked whether it is full.");
        Assert.Contains("hold.Elapsed += dtRealSeconds;", stepBody, StringComparison.Ordinal);

        int begin = surface.IndexOf("private void BeginProcessing(", StringComparison.Ordinal);
        string beginBody = surface[begin..step];
        int zero = beginBody.IndexOf("if (ProcessingSeconds <= 0)", StringComparison.Ordinal);
        int instant = beginBody.IndexOf("CompleteProcessing(ex, hold);", StringComparison.Ordinal);
        Assert.True(zero > 0 && instant > zero,
            "a hold completes at the moment it starts, with no cheat asked and no clock run.");

        // THE PRODUCT: the seated ending files the gist and RETURNS — the sheet is not set down, which is
        // the whole difference between a table and a photograph.
        // #828 · Cut at the member that FOLLOWS it, exactly as the landing below is cut, and no longer at a
        // character count: a third register (the secure disposal's watched beat) pushed the fall-through
        // past 1600 characters and the guard went red on a method it had no complaint about. A window a
        // comment can push an assertion out of is a guard about formatting.
        int complete = surface.IndexOf("private void CompleteProcessing(", StringComparison.Ordinal);
        int afterComplete = surface.IndexOf(
            "private void AbandonProcessing(", complete, StringComparison.Ordinal);
        Assert.True(afterComplete > complete, "the far end has no end this guard can find.");
        string completeBody = surface[complete..afterComplete];
        int write = completeBody.IndexOf("hold.Work == Core.Processing.Work.Write", StringComparison.Ordinal);
        int setDown = completeBody.IndexOf("SetItDown(ex, hold.Item", StringComparison.Ordinal);
        Assert.True(write > 0, "the far end has no arm for the seated register at all.");
        Assert.True(setDown > write, "the seated write-up falls through into the leave-it-behind ending.");
        Assert.Contains("TheWriteUpLands(ex, hold.Item, hold.Standing);", completeBody,
            StringComparison.Ordinal);

        // …and the entry goes into the ONE book, through the one seam, glyphed with the pen.
        // The method's own body, cut at the member that follows it rather than at a character count — a
        // guard whose window a comment can push an assertion out of is a guard about formatting.
        int lands = seated.IndexOf("private void TheWriteUpLands(", StringComparison.Ordinal);
        Assert.True(lands > 0, "the dig has no far end for the entry to land at.");
        int afterLanding = seated.IndexOf("private bool CanWriteUp(", lands, StringComparison.Ordinal);
        Assert.True(afterLanding > lands);
        string landing = seated[lands..afterLanding];
        Assert.Contains("FileNote(gist, SeatedPosture.WriteGlyph);", landing, StringComparison.Ordinal);
        // …and it asks for the KEPT disposition, which is the difference between a table and a photograph
        // said in the entry itself. Found in a browser: without it the strip promised the sleeve and then
        // quoted a book entry that said the sheet had been left on the floor.
        Assert.Contains("LeftBehind.GistOf(item, standing, kept: true)", landing, StringComparison.Ordinal);
        Assert.Contains("RequestVaultSave();", landing, StringComparison.Ordinal);

        // INTERRUPTIBLE, and honestly: standing up ends it out loud, in the seated register's own reason.
        string table = Source("Pages", "Map.Table.cs");
        int close = table.IndexOf("private void CloseTable()", StringComparison.Ordinal);
        string closeBody = table[close..(close + 900)];
        Assert.Contains("Core.Processing.Interruption.StoodUp", closeBody, StringComparison.Ordinal);
        Assert.True(
            closeBody.IndexOf("AbandonProcessing(", StringComparison.Ordinal)
                < closeBody.IndexOf("_table = null;", StringComparison.Ordinal),
            "the table is gone before the abandon line has a strip to land on.");

        // …and so does somebody taking the chair opposite, which is the privacy the spread was licensed by
        // being revoked by a person.
        int chair = table.IndexOf("private void SomebodyTakesTheChair(", StringComparison.Ordinal);
        string chairBody = table[chair..(chair + 900)];
        Assert.Contains("Core.Processing.Interruption.CompanyArrived", chairBody, StringComparison.Ordinal);
        Assert.True(
            chairBody.IndexOf("AbandonProcessing(", StringComparison.Ordinal)
                < chairBody.IndexOf("t.Solo = false;", StringComparison.Ordinal),
            "the privacy flag flips before the hold it licensed is ended.");
    }

    // ── (d2) THE DIG'S CLOCK IS DRAWN WHERE THE CAPTAIN IS ACTUALLY LOOKING ───────────────────────────

    /// <summary>
    /// #784/#782 · <b>THE BAR IS ON THE STRIP, AT THE STRIP'S WIDTH, AND ONLY WHILE A DIG RUNS.</b>
    ///
    /// <para>Owner playtest, live on the phase-2 build: <i>"the progress bar is kind of small there… it might
    /// be good to have it on the dialog… took me a while to notice it."</i> The rectangle on the deck rides
    /// the DeckView idiom and is honest at a glance from across the hall — but a seated captain's eyes are on
    /// the DOCKED STRIP, and a clock drawn where nobody is looking is #782's readability law failing at TIME
    /// rather than at type. A bar is a number drawn as a length; too thin to see is the same defect as too
    /// small to read.</para>
    ///
    /// <para>Three things, and the second is the one that can rot. It must be THERE during a dig, ABSENT
    /// otherwise (a permanent empty trough at the foot of the strip is furniture, and furniture is what the
    /// eye learns to skip), and it must be fed by <see cref="Processing.Fraction"/> — the same call the deck
    /// rectangle is fed by, because two arithmetics for one clock is this house's two-clocks class.</para>
    ///
    /// <para>The deck rectangle STAYS. The owner asked for a second read, not a move.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the bar from the strip, and again by hoisting it out of the
    /// <c>ProcessingUnderway</c> gate so it draws while nothing is being dug: see the PR body for both.</para>
    /// </summary>
    [Fact]
    public void THE_DIG_BAR_RidesTheStripAndOnlyWhileSomethingIsBeingDug()
    {
        string docked = DockedBranch();

        // ── it is in the strip's subtree, and inside the running-dig gate ──
        int gate = docked.IndexOf("@if (ProcessingUnderway() is { } dockedBusy)", StringComparison.Ordinal);
        Assert.True(gate > 0, "the strip no longer says anything about a running dig at all.");
        // The gate's own body, cut at ITS closing brace (the one at the strip's own indentation) rather
        // than at the next element — a window that ran past the brace would call a bar hoisted OUT of the
        // gate "inside" it, which is precisely the failure this test exists to see.
        int closes = docked.IndexOf("\n            }", gate, StringComparison.Ordinal);
        Assert.True(closes > gate, "the running-dig block no longer closes where this guard can see it.");
        string whileDigging = docked[gate..closes];

        Assert.Contains("class=\"seated-dock-dig\"", whileDigging, StringComparison.Ordinal);
        Assert.Contains("class=\"seated-dock-dig-fill\"", whileDigging, StringComparison.Ordinal);

        // …AND NOWHERE ELSE IN THE STRIP. Outside the gate there is no trough to sit empty.
        Assert.Single(Regex.Matches(docked, "seated-dock-dig\""));
        Assert.DoesNotContain("seated-dock-dig", docked[..gate], StringComparison.Ordinal);
        Assert.DoesNotContain("seated-dock-dig", docked[closes..], StringComparison.Ordinal);

        // …and it is the bar rather than a second sentence: a width, driven by a fraction.
        Assert.Contains("ProcessingFraction() is { } dockedDug", whileDigging, StringComparison.Ordinal);
        Assert.Contains("style=\"width:@((int)(dockedDug * 100))%\"", whileDigging, StringComparison.Ordinal);

        // ── ONE CLOCK. The strip's fraction and the deck rectangle's are the same Core call ──
        string surface = Surface();
        int frac = surface.IndexOf("private double? ProcessingFraction()", StringComparison.Ordinal);
        Assert.True(frac > 0, "the strip's bar has no fraction of its own to read.");
        string fracBody = surface[frac..(frac + 260)];
        Assert.Contains("Core.Processing.Fraction(dug.Elapsed, ProcessingSeconds)", fracBody,
            StringComparison.Ordinal);
        Assert.Contains("_surface is { Processing: { } dug }", fracBody, StringComparison.Ordinal);

        // THE DECK RECTANGLE STAYS. The owner asked for a second read, not for the first one to move —
        // "keep the deck rectangle (it's honest at a glance from afar)".
        Assert.Contains("Core.Processing.Fraction(paper.Elapsed, ProcessingSeconds)", surface,
            StringComparison.Ordinal);
        Assert.Contains("DigProgress:", surface, StringComparison.Ordinal);

        // ── (#782) AND IT IS BIG ENOUGH TO NOTICE. Full strip width, and a real height in rem ──
        string css = Source("Pages", "Map.razor.css");
        int at = css.IndexOf(".seated-dock-dig {", StringComparison.Ordinal);
        Assert.True(at >= 0, "the strip's bar has no rule of its own — it is whatever a default gives it.");
        string rule = css[at..css.IndexOf('}', at)];
        Assert.Contains("width: 100%", rule, StringComparison.Ordinal);

        Match height = Regex.Match(rule, @"height:\s*(?<n>[0-9.]+)rem");
        Assert.True(height.Success, "the bar's height is not stated in rem, so it does not scale with the text.");
        Assert.True(double.Parse(height.Groups["n"].Value, CultureInfo.InvariantCulture) >= 0.9,
            "the bar is back to being 'kind of small there' — #782 is a law about being noticed.");

        int fill = css.IndexOf(".seated-dock-dig-fill {", StringComparison.Ordinal);
        Assert.True(fill >= 0, "the fill has no rule, so a width with no colour behind it draws nothing.");
        Assert.Contains("background:", css[fill..css.IndexOf('}', fill)], StringComparison.Ordinal);
    }

    // ── (e, client half) THE GATE IS ASKED WITH THE ROOM'S OWN FACTS ──────────────────────────────────

    /// <summary>#784 · The privacy predicate is Core's, and the two facts it is asked with are the room's
    /// own: the cabinet bit Core already keeps on a top (<c>Quiet</c> — the same one
    /// <see cref="SittingAlone.SomebodyComes"/> refuses on) and #789's own neighbour law at the
    /// counter.</summary>
    [Fact]
    public void THE_PRIVACY_GATE_IsAskedWithTheRoomsOwnFacts()
    {
        string seated = Source("Pages", "Map.Seated.cs");

        Assert.Contains("SeatedSpread.CanSpreadTheCase(seat, SeatedAlone)", seated, StringComparison.Ordinal);
        Assert.Contains("t.Quiet ? SeatedHud.Seat.Cabinet : SeatedHud.Seat.HallTable", seated,
            StringComparison.Ordinal);
        Assert.Contains("Core.Interior.TheStools.HasNeighbour(", seated, StringComparison.Ordinal);

        // No client opinion about what a private seat IS — the enum decides and Core answers.
        Assert.DoesNotContain("seat == SeatedHud.Seat.BarStool ?", seated, StringComparison.Ordinal);
    }

    // ── ESC STAYS SANE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · Once the frame stopped dimming the room, "cancel" stopped being a way out of a card and became
    /// a press that silently spends the watch you sat for. So a DOCKED seat routes Esc into the same
    /// question W raises; a CONVERSATION keeps the old law, because leaving a table is free and always
    /// available.
    /// </summary>
    [Fact]
    public void ESC_AsksTheSameQuestionWDoesWhileSeatedAloneAndStillLeavesAConversation()
    {
        string sim = Source("Pages", "Map.Sim.Cancel.cs");
        int chain = sim.IndexOf("private bool TryDismissTopOverlay()", StringComparison.Ordinal);
        Assert.True(chain > 0);

        int confirm = sim.IndexOf("if (TheStandUpConfirmIsUp) { KeepYourSeat(); return true; }", chain,
            StringComparison.Ordinal);
        int docked = sim.IndexOf("if (SeatedIsDocked) { AskWhetherToStandUp(); return true; }", chain,
            StringComparison.Ordinal);
        int table = sim.IndexOf("if (CaptainIsSeated) { CloseTable(); return true; }", chain,
            StringComparison.Ordinal);

        Assert.True(confirm > 0 && docked > confirm,
            "Esc raises the stand-up question on top of the stand-up question.");
        Assert.True(docked < table,
            "Esc peels a docked seat before it asks — the cancel key spends the rest without asking.");
    }

    // ── THE DEMO ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #693's rule again: <i>a scene nobody can reach on demand is a scene that ships broken.</i> Owner's own
    /// ask for this one: <i>"we probably need a start point where we have things in our inventory we can
    /// process (when our HUD UI state is sitting down with enough privacy)."</i>
    /// </summary>
    [Fact]
    public void THE_DEMO_PutsATesterInAPrivateSeatWithSomethingToDig()
    {
        string sim = Sim();
        Assert.Contains("pair.StartsWith(\"spread=\"", sim, StringComparison.Ordinal);
        Assert.Contains("_spreadCheat = true;", sim, StringComparison.Ordinal);

        string table = Source("Pages", "Map.Table.cs");

        // A CABINET, asked of the room's own list — never a coordinate the cheat typed (§13.15).
        Assert.Contains("FirstFreeTop(ex, a, quietOnly: true)", table, StringComparison.Ordinal);
        Assert.Contains("CanteenRegulars.Tables(", table, StringComparison.Ordinal);

        // …sat down through the very handler [E] reaches, so the tester lands in the state a captain gets.
        int leg = table.IndexOf("if (_spreadCheat && FirstFreeTop(", StringComparison.Ordinal);
        Assert.True(leg > 0);
        string legBody = table[leg..(leg + 1400)];
        Assert.Contains("TryTakeTable();", legBody, StringComparison.Ordinal);
        Assert.Contains("SeedTheSpreadFinds();", legBody, StringComparison.Ordinal);

        // …with real finds, added through the one funnel every find in the game goes through.
        int seed = table.IndexOf("private void SeedTheSpreadFinds()", StringComparison.Ordinal);
        string seedBody = table[seed..(seed + 900)];
        Assert.Contains("Core.Satchel.Add(", seedBody, StringComparison.Ordinal);
        Assert.Contains("Core.Satchel.Kind.Paper", seedBody, StringComparison.Ordinal);
        Assert.Contains("Core.Satchel.Kind.Dirt", seedBody, StringComparison.Ordinal);

        // Both kinds seeded really are the kinds the field book has a gist for — a demo that put rounds in
        // the sleeve would boot a tester into an empty spread and prove nothing.
        foreach (Satchel.Kind kind in new[] { Satchel.Kind.Paper, Satchel.Kind.Dirt })
        {
            Assert.NotNull(LeftBehind.GistOf(new Satchel.Item(kind, "spread-demo"), "in the cabinet"));
        }

        // The row is in the front door's list and in the guide — the two places a tester looks.
        Assert.Contains(DevStarts.All, e => e.Url.Contains("spread=1", StringComparison.Ordinal));
        Assert.Contains("?spread=1", Doc("testing-guide.md"), StringComparison.Ordinal);
        Assert.Contains("spread=1", Doc("testing-links-the-hive.md"), StringComparison.Ordinal);
    }
}
