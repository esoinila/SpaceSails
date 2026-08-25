using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 6 · <b>THE SHELL AND THE START-PICKER FAMILY.</b>
///
/// <para>One root class, <c>.start-picker</c>, and four surfaces on it — the only door a player meets before
/// the game starts, and the one they meet again from the death card (#951 gave every death panel a
/// <i>📖 Load a saved voyage</i>):</para>
///
/// <list type="bullet">
/// <item><b>the front door</b> — the boot's own picker: Continue, the berths, the dev starts, the captains'
/// roster;</item>
/// <item><b>the logbook</b> — the same surface again from the captain's desk, in game;</item>
/// <item><b>the bank sheet</b> — #948's title-and-note page, for banking a moment, exporting one, or
/// rewriting the page of a bank already on the shelf;</item>
/// <item><b>the import consent</b> — #310's ask before a file becomes the live game.</item>
/// </list>
///
/// <para><b>Two of the four migrate and two do not, and that split is the point of this file.</b> The bank
/// sheet and the import consent are decisions: no ✕, because every answer each of them offers is itself a
/// close — the critical-decision exception, and it is proved here by PRESSING all five answers rather than
/// taken from a parameter. The front door and the logbook are ONE element with one class attribute drawn
/// twice, and neither of the shell's three shapes fits it: the logbook's way out is the third button of a
/// three-button row (a <c>Bare</c> shell's dismiss is the card's LAST DIRECT CHILD, which would lift it out
/// of that row), and the front door has no way out at all because there is nothing behind it to go back to.
/// They are named below with that reason, and the two guards at the bottom of this file pin the reason
/// itself — a straggler whose stated reason has quietly stopped being true is worse than no reason.</para>
///
/// <para>The dismissibility law (<see cref="EveryPopUpCanBeDismissedTests"/>) asks whether a surface can be
/// got rid of at all, and it already drives this family — including the sibling reset its own register row
/// documents, because the logbook's <i>⤓ bank here</i> raises the bank sheet on the same root class. What a
/// MIGRATION can break is narrower, and this file asks that instead.</para>
/// </summary>
public sealed class TheShellOwnsTheStartPickerFamilyTests
{
    /// <summary>The family's one root. <c>save-surface</c> and <c>bank-sheet</c> ride the same element as
    /// modifiers, so naming them here would count one surface three times — #992's own lesson about
    /// <c>rep-backdrop</c>, in this file's idiom.</summary>
    private static readonly string[] TheStartPickerRoots = ["start-picker"];

    // ── Read off the markup as typed ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY START-PICKER SURFACE IS DRAWN THROUGH THE SHELL, OR NAMED HERE WITH ITS REASON.
    ///
    /// <para>Read as TYPED, in #992's own idiom and for its reason: the alias law keeps this root a lowercase
    /// <c>class="…"</c> attribute precisely so a guard that reads the markup can still see it. A fifth
    /// save-surface typed as a plain <c>&lt;div&gt;</c> fails here with its file, its line and its class
    /// list.</para>
    ///
    /// <para><b>Keyed on the class list rather than on a line number</b> (wave 4's lesson), and it fails the
    /// other way too — a straggler that HAS been migrated must leave the list. Razor comments are skipped,
    /// because this page's own migration comments quote the markup they are about.</para>
    ///
    /// <para>The third assertion is this family's own shape: every one of its shells is a
    /// <c>ByDecision</c>. A <c>Close</c> here would draw a ✕ as the LAST DIRECT CHILD of a save sheet —
    /// below the answer row, in a family that has never had one, with no rule in Map.razor.css written for
    /// it. If a sheet ever really does earn a ✕, this is where somebody says so out loud.</para>
    /// </summary>
    [Fact]
    public void EveryStartPickerSurfaceIsDrawnThroughTheShell()
    {
        var handRolled = new List<(string Where, string Classes)>();
        var wrongShape = new List<string>();

        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);
            string shortName = Path.GetFileName(file);
            bool inComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool opens = line.Contains("@*", StringComparison.Ordinal);
                bool closes = line.Contains("*@", StringComparison.Ordinal);
                bool commented = inComment || opens;
                inComment = inComment ? !closes : (opens && !closes);

                if (commented)
                {
                    continue;
                }

                foreach (Match attribute in Regex.Matches(line, "class=\"([^\"]*)\""))
                {
                    string list = attribute.Groups[1].Value;
                    string[] classes = list.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                    if (!classes.Intersect(TheStartPickerRoots, StringComparer.Ordinal).Any())
                    {
                        continue;
                    }

                    string tag = TagOwning(lines, i, attribute.Index);
                    if (!string.Equals(tag, "OverlayShell", StringComparison.Ordinal))
                    {
                        handRolled.Add(($"{shortName}:{i + 1}  <{tag} class=\"{list}\">", list));
                        continue;
                    }

                    // A shell: read THIS tag — the one whose attribute we are standing on, found by walking
                    // back from this line, never by searching the file for the class list (two shells in
                    // this family wear the same one, and the first hit is the hand-rolled straggler).
                    if (!TheTagAt(lines, i).Contains("OverlayDismiss.ByDecision", StringComparison.Ordinal))
                    {
                        wrongShape.Add($"{shortName}:{i + 1}  class=\"{list}\"");
                    }
                }
            }
        }

        var unexplained = handRolled
            .Where(found => !TheNamedStragglers.ContainsKey(Normalised(found.Classes)))
            .Select(found => found.Where)
            .ToList();

        Assert.True(unexplained.Count == 0,
            $"{unexplained.Count} start-picker surface(s) are NOT drawn through OverlayShell:\n  - "
            + string.Join("\n  - ", unexplained)
            + "\n\nThis family is the one door a player meets before the game starts and again from the "
            + "death card, and #970's tests read its markup closely. Give it a shell (#997 wave 6) — or "
            + "name it in TheNamedStragglers with the reason, which is the edit that makes somebody say why "
            + "out loud.");

        var stale = TheNamedStragglers.Keys
            .Where(named => !handRolled.Any(found => Normalised(found.Classes) == named))
            .ToList();

        Assert.True(stale.Count == 0,
            $"{stale.Count} straggler(s) are named here and are no longer hand-rolled: "
            + string.Join(" · ", stale)
            + ". Take them off the list — a written-down reason for a surface that has moved on is worse "
            + "than no reason at all.");

        Assert.True(wrongShape.Count == 0,
            $"{wrongShape.Count} start-picker shell(s) are not ByDecision:\n  - "
            + string.Join("\n  - ", wrongShape)
            + "\n\nA Close here would draw a ✕ as the LAST DIRECT CHILD of a save sheet — under the answer "
            + "row, in a family that has never carried one and has no rule written for it. The two sheets "
            + "on this root are decisions: every answer they offer is itself a close, which is what the "
            + "press guard in this file establishes. If a sheet has really earned a ✕, say so here first.");
    }

    /// <summary>
    /// THE STRAGGLER, BY NAME AND WITH THE REASON — one class attribute, two surfaces.
    ///
    /// <para>Keyed on the class list exactly as typed, because that list IS the surface's identity under the
    /// alias law and it is the one thing about it a refactor is not allowed to move.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> TheNamedStragglers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["start-picker save-surface"] =
                "the front door AND the in-game logbook: one element with one class attribute, drawn twice "
                + "off the SaveLoadSurface fragment, so they migrate together or not at all. Not at all, "
                + "and for two reasons that are facts about the markup rather than shortcuts. (1) THE "
                + "LOGBOOK'S WAY OUT IS THE THIRD BUTTON OF A ROW: its Close sits in .save-surface-foot "
                + "beside \"⬇ Export this moment\" and \"⬆ Import file\" — one flex row, three buttons — "
                + "and a Bare shell draws its dismiss as the card's LAST DIRECT CHILD, which is the whole "
                + "point of the frame and would lift Close out of that row onto a line of its own below the "
                + "foot. That is a control moving on the screen, which this migration does not do. (2) THE "
                + "FRONT DOOR HAS NO WAY OUT AT ALL, because there is nothing behind it to go back to — you "
                + "leave it by starting or loading a game. So neither of the other two shapes fits either: "
                + "a Close would draw a ✕ this door has never had, and a ByDecision would claim that every "
                + "control on it is itself a close, which is false on both surfaces (⬆ Import, 📥 file, 🗑, "
                + "the ▸ dev-starts chevron and the ✏ pencil all leave the surface standing). Both halves "
                + "of that reason are pinned to the render tree by the two guards at the foot of this file. "
                + "It gets a shell when the logbook's Close stops sharing a row, or when the shell learns "
                + "to hand a page's own foot the dismiss it draws.",
        };

    private static string Normalised(string classList) =>
        string.Join(' ', classList.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));

    // ── Read off what was actually drawn ──────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TWO SHEETS ARE THE SHELL'S, THEY OFFER NO WAY OUT BUT AN ANSWER, AND EVERY ANSWER IS ONE.
    ///
    /// <para>Five answers across the two sheets, each pressed on its own run of the theory, and the sheet has
    /// to be gone afterwards. That is the check that caught the rep's card (#998) and the collector's demand
    /// (#1001) both claiming an exception they had not earned, and it is the only honest way to hold a
    /// <c>ByDecision</c>: the claim is about BEHAVIOUR, and this repository has been burned by guards that
    /// took such claims from a list.</para>
    ///
    /// <para>The commit answers are pressed on the real road, not a stubbed one: the bank sheet banks into
    /// slot 1 through <c>SaveToSlot</c> (which carries its own try/catch for a storage that is not there off
    /// a browser) and the import consent runs <c>ApplyPendingImport</c> with nothing pending. What is under
    /// test is whether the sheet ends, and it has to end either way.</para>
    ///
    /// <para><b>Four of the five, and the fifth is named rather than faked.</b> The import consent's
    /// <i>💾 Bank current first, then import</i> BANKS before it imports, and banking walks
    /// <c>FirstFreeManualSlotId</c> → <c>Slots.Get</c> → <c>RendererInterop.VaultRead</c>, which is a
    /// <c>[JSImport]</c> and therefore DeskBench's own documented browser gate — the read throws off a
    /// browser, before <c>ApplyPendingImport</c> has cleared the gate, and the sheet is still up. That is
    /// the bench's horizon rather than a fault in the answer, so this file does not pretend to press it: the
    /// theory below asserts that all three of the consent's answers are drawn and wired, and the third one
    /// is pressed in a real browser and written up in the PR. Said out loud, in #1005's idiom, rather than
    /// quietly dropped.</para>
    /// </summary>
    [Theory]
    [InlineData("the bank sheet", "bank-sheet", "⤓ Bank it")]
    [InlineData("the bank sheet", "bank-sheet", "Cancel")]
    [InlineData("the import consent", "", "Replace now")]
    [InlineData("the import consent", "", "Cancel")]
    public async Task EachSheetOffersNoWayOutButAnAnswerAndEveryAnswerIsOne(
        string name, string modifier, string answer)
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        OnlyThisStartPickerSurface(bench, modifier.Length > 0 ? "_bankPrompt" : "_importConfirming");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheSurface(painted, modifier)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: its gate was raised and nothing wearing .start-picker came back. The driver and "
                + "the markup's gate have come apart; one of them has moved.");

        Assert.True(card.HasClass("overlay-shell"),
            $"{name}: the sheet is on the screen and it is not the shell's — #997 wave 6 put both sheets of "
            + "this family on OverlayShell, and this one has come back off it.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            $"{name}: the shell drew it as something other than Bare. Hosted's `display: contents` would "
            + "put a wrapper between this root and .save-surface-foot, and Card would wrap the page's own "
            + ".start-picker-title in an overlay-shell-head that no rule in this file is written against.");
        Assert.True(card.HasClass("start-picker") && card.HasClass("save-surface"),
            $"{name}: the sheet has lost its own class. The shell is the MECHANISM and .start-picker is the "
            + "IDENTITY — #992's completeness guard reads that name off the markup as typed, and the whole "
            + "of this surface's skin hangs off it.");

        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-dismiss"));

        // Every answer the sheet is supposed to offer is drawn and wired — including the one this bench
        // cannot press (see the note above), so its disappearance is caught here rather than nowhere.
        foreach (string offered in TheAnswersOf(modifier))
        {
            Assert.Contains(card.Descendants(),
                n => !n.Hidden && n.Handlers.ContainsKey("onclick")
                     && n.Name.Contains(offered, StringComparison.Ordinal));
        }

        DeskBench.Painted.Node press = card.Descendants()
            .FirstOrDefault(n => !n.Hidden
                                 && n.Handlers.ContainsKey("onclick")
                                 && n.Name.Contains(answer, StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException(
                $"{name} has no control reading \"{answer}\". Its answers ARE the sheet — a ByDecision "
                + "surface that has mislaid one of them is a surface with no way out.");

        await bench.PressAsync(press.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(TheSurface(after, modifier) is null,
            $"{name}: \"{answer}\" was pressed and the sheet is still on the screen. It carries no ✕ only "
            + "because every answer it offers was supposed to be one — give it a way out, or make the "
            + "answer end it.");
    }

    // ── The straggler's reason, pinned to the render tree ─────────────────────────────────────────────

    /// <summary>
    /// THE LOGBOOK'S WAY OUT IS STILL THE THIRD BUTTON OF A ROW — which is the whole reason it keeps it.
    ///
    /// <para>A straggler is only honest while its reason is true, and this one's reason is a fact about the
    /// DOM rather than about a sentence. So the fact is asserted: the foot is a row of three controls, the
    /// last of them is <i>Close</i>, and it closes the drawer. The day somebody makes Close the only thing
    /// in that foot, this fails and the surface is ready for its shell.</para>
    ///
    /// <para>It also pins what the straggler COSTS, so the cost is visible rather than forgotten: the
    /// hand-rolled classless wrapper carrying <c>@@onclick:stopPropagation</c> between the backdrop and the
    /// surface — the div <c>StopClicks="true"</c> exists to delete.</para>
    /// </summary>
    [Fact]
    public async Task TheLogbooksWayOutIsStillTheThirdButtonOfARow()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        OnlyThisStartPickerSurface(bench, "_showSaveDrawer");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheSurface(painted, "")
            ?? throw new Xunit.Sdk.XunitException(
                "the logbook: _showSaveDrawer was raised and nothing wearing .start-picker came back.");

        Assert.False(card.HasClass("overlay-shell"),
            "the logbook is drawn through OverlayShell now. Good — but then it is no longer a straggler: "
            + "take it out of TheNamedStragglers and give this guard the shape it actually has.");

        DeskBench.Painted.Node foot = card.Descendants()
            .FirstOrDefault(n => n.HasClass("save-surface-foot") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException("the logbook has no .save-surface-foot at all.");

        List<DeskBench.Painted.Node> row = foot.Children
            .Where(n => !n.Hidden && (n.Handlers.ContainsKey("onclick") || n.Element == "a"))
            .ToList();

        Assert.True(row.Count > 1,
            $"the logbook's foot is down to {row.Count} control(s). The ONLY reason this surface is a "
            + "straggler is that its Close shares a flex row with ⬇ Export and ⬆ Import, so a Bare shell's "
            + "last-direct-child dismiss would move it. If the row is gone, so is the reason: migrate it "
            + "(#997 wave 6) rather than leaving a written-down excuse that is no longer true.");

        DeskBench.Painted.Node close = row[^1];
        Assert.Equal("Close", close.Name);

        // …and the wrapper the straggler pays for: a classless div carrying the stopPropagation that
        // StopClicks="true" would have owned.
        DeskBench.Painted.Node backdrop = painted.Root.Descendants()
            .First(n => n.HasClass("start-picker-backdrop") && !n.Hidden);
        Assert.DoesNotContain(backdrop.Children, n => n.HasClass("start-picker"));

        await bench.PressAsync(close.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(TheSurface(after, "") is null,
            "\"Close\" was pressed on the logbook and the drawer is still on the screen.");
    }

    /// <summary>
    /// THE FRONT DOOR HAS NO WAY OUT, AND THAT IS NOT A FAULT — it is the other half of the straggler's
    /// reason. There is no game behind it to go back to; the captain leaves it by starting or loading one.
    ///
    /// <para>Which is exactly why <c>ByDecision</c> would be a lie about it rather than a description: the
    /// door carries controls that leave it standing (⬆ Import a save file, 📥 file, 🗑, the ▸ dev-starts
    /// chevron, the ✏ pencil), so "every answer it offers is itself a close" is false here. Asserted rather
    /// than reasoned about, because #1005's whole lesson is that a false ByDecision claim is the bug.</para>
    /// </summary>
    [Fact]
    public async Task TheFrontDoorOffersNoCloseAtAllAndNotEveryControlOnItIsOne()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        OnlyThisStartPickerSurface(bench, "_showStartPicker");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node door = TheSurface(painted, "")
            ?? throw new Xunit.Sdk.XunitException(
                "the front door: _showStartPicker was raised and nothing wearing .start-picker came back.");

        Assert.False(door.HasClass("overlay-shell"),
            "the front door is drawn through OverlayShell now — then say which of the three shapes it "
            + "took, and take it out of TheNamedStragglers.");

        Assert.DoesNotContain(door.SelfAndDescendants(), n => n.HasClass("overlay-shell-dismiss"));
        Assert.DoesNotContain(door.Descendants(),
            n => !n.Hidden && string.Equals(n.Name, "Close", StringComparison.Ordinal));

        // The controls that leave it standing — the half of the door that makes ByDecision false about it.
        DeskBench.Painted.Node chevron = door.Descendants()
            .FirstOrDefault(n => n.HasClass("start-picker-devstarts-head") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "the front door's ▸ dev-starts head is gone. It is one of the controls this door offers "
                + "that is NOT a close, and it is why the door cannot claim the critical-decision "
                + "exception. If every control on the door really does end it now, the door is a "
                + "ByDecision shell and this guard should say so instead.");

        await bench.PressAsync(chevron.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(TheSurface(after, "") is not null,
            "the ▸ dev-starts chevron closed the front door. That is a control changing what it does, not "
            + "a refactor — and if the door really has become all-answers, it has earned a ByDecision "
            + "shell and this guard is the wrong shape.");
    }

    // ── #735, and the card that had never been named in it ────────────────────────────────────────────

    /// <summary>
    /// EVERY CARD IN THE CELEBRATION FAMILY TAKES THE TALL-CARD CAP AND THE PINNED FOOT.
    ///
    /// <para>#735's block is deliberately a NAMED list rather than a glob — "a NEW modal card has to be added
    /// here deliberately, which is the moment somebody asks whether its way out is reachable" — and the cost
    /// of a named list is that a card can be left off it. One was: <b><c>.mission-celebration</c></b>, the
    /// contract-complete fanfare, alone of the six cards in its own backdrop family. It is a full-screen card
    /// with seven growing sections (the fanfare, the title, the giver, the thanks, the payment line, the
    /// parrot, the history) and it had neither the screen cap nor the pinned foot its five siblings have.
    /// Measured in a browser by #997 wave 5, flagged there, and fixed here.</para>
    ///
    /// <para>So the list gets a guard as well as a name. Each root has to appear in the cap group, in the
    /// pinned-foot group, and to name its own foot scrim — asked of the family rather than of the one card,
    /// so the NEXT card added to this backdrop is caught the same way.</para>
    /// </summary>
    [Theory]
    [InlineData("the Convergence band", "convergence-card")]
    [InlineData("the contract-complete fanfare", "mission-celebration")]
    [InlineData("the treasure map and the wreck's outcome", "treasure-map-card")]
    [InlineData("the research brief and the deflection storyboard", "expedition-brief-card")]
    [InlineData("the reveal", "expedition-reveal-card")]
    [InlineData("the tow offer and the plan alarm", "rescue-card")]
    public void EveryCardInTheCelebrationFamilyTakesTheTallCardCapAndThePinnedFoot(string name, string root)
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Pages", "Map.razor.css")));

        Assert.True(
            SelectorsOfTheRuleContaining(css, "max-height: min(92vh, 100%)")
                .Any(selector => NamesTheRoot(selector, root)),
            $"{name}: .{root} is not in #735's tall-card list. A full-screen card that outgrows the screen "
            + "with no cap does not scroll, and everything past the fold — including its way out — is "
            + "simply off the screen. That is the softlock #735 was written about, and the list is named "
            + "rather than globbed precisely so that leaving a card off it is a decision somebody makes.");

        Assert.True(
            SelectorsOfTheRuleContaining(css, "box-shadow: 0 12rem 0 12rem var(--card-foot-scrim")
                .Any(selector => NamesTheRoot(selector, root)),
            $"{name}: .{root}'s action row is not pinned. The cap alone is half the rule — a card that "
            + "scrolls with its button riding the prose still opens with the way on below the fold, which "
            + "is the same bug wearing the fix's clothes (#735).");

        Assert.True(
            SelectorsOfTheRuleContaining(css, "--card-foot-scrim:")
                .Any(selector => NamesTheRoot(selector, root)),
            $"{name}: .{root} names no --card-foot-scrim, so its pinned foot falls back to the family "
            + "default and reads as a visibly flatter strip over a card of another shade. Read the bottom "
            + "stop of its own background, alpha and all.");
    }

    /// <summary>Does this one selector target the named root — either bare or through <c>::deep</c>, and not
    /// as some longer class name that merely starts with it.</summary>
    private static bool NamesTheRoot(string selector, string root) =>
        Regex.IsMatch(selector.Trim(), $@"^(::deep\s+)?\.{Regex.Escape(root)}(\s*>.*)?$");

    /// <summary>Every selector in the selector list of every rule whose body contains
    /// <paramref name="declaration"/>.</summary>
    private static IEnumerable<string> SelectorsOfTheRuleContaining(string css, string declaration)
    {
        foreach (Match rule in Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}"))
        {
            if (rule.Groups[2].Value.Contains(declaration, StringComparison.Ordinal))
            {
                foreach (string selector in rule.Groups[1].Value.Split(','))
                {
                    yield return selector.Trim();
                }
            }
        }
    }

    private static string WithoutComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", " ", RegexOptions.Singleline);

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";

    /// <summary>Every answer each sheet offers, as a player reads them. A ByDecision surface's answers ARE
    /// its way out, so one going missing is a surface with no way out — and that has to fail even for the
    /// one answer this bench cannot press.</summary>
    private static string[] TheAnswersOf(string modifier) =>
        modifier == "bank-sheet"
            ? ["⤓ Bank it", "Cancel"]
            : ["Bank current first", "Replace now", "Cancel"];

    /// <summary>The four gates that all draw a <c>.start-picker-backdrop</c>, and the reason this reset is
    /// not optional: the register's own row says it out loud — one of the logbook's controls (<i>⤓ bank
    /// here</i>) raises a SIBLING on the same root class, so a guard that reads "the first .start-picker on
    /// the screen" without putting the other three down reads whichever one is earliest in the tree and
    /// reports it under another one's name. That is this repository's first named bug class, in a test.
    /// </summary>
    private static readonly string[] TheFamilyGates =
        ["_showStartPicker", "_showSaveDrawer", "_bankPrompt", "_importConfirming"];

    private static void OnlyThisStartPickerSurface(DeskBench bench, string gate)
    {
        bench.Poke("_showStartPicker", false);
        bench.Poke("_showSaveDrawer", false);
        bench.Poke("_bankPrompt", null);
        bench.Poke("_importConfirming", false);
        Assert.Contains(gate, TheFamilyGates);

        if (gate == "_bankPrompt")
        {
            // #948's sheet on its real road: bank the live moment into a manual berth. The kind is a private
            // nested enum, so it is read off the field's own type rather than named again here — a renamed
            // member fails loudly instead of quietly raising nothing.
            bench.Poke("_bankSlotId", "1");
            bench.Poke("_bankThreadId", "");
            bench.Poke("_bankTitle", "The wave-6 bench");
            bench.Poke("_bankNote", "Standing at the logbook with the sheet up.");
            bench.Poke(gate, TheBankKind("Bank"));
            return;
        }

        bench.Poke(gate, true);
    }

    private static object TheBankKind(string member)
    {
        FieldInfo field = typeof(Map).GetField("_bankPrompt",
                              BindingFlags.Instance | BindingFlags.NonPublic)
                          ?? throw new InvalidOperationException(
                              "Map has no _bankPrompt — #948's sheet has moved, and this bench cannot raise "
                              + "a surface whose gate it can no longer name.");

        Type kind = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        return Enum.Parse(kind, member);
    }

    /// <summary>The one surface of the family that is up: a <c>.start-picker</c> that is not hidden, and
    /// wearing <paramref name="modifier"/> if one was asked for.</summary>
    private static DeskBench.Painted.Node? TheSurface(DeskBench.Painted painted, string modifier) =>
        painted.Root.Descendants().FirstOrDefault(
            n => n.HasClass("start-picker") && !n.Hidden
                 && (modifier.Length == 0 || n.HasClass(modifier)));

    /// <summary>The element a <c>class="…"</c> belongs to: the nearest <c>&lt;</c> at or before it, looking
    /// back up the file when the attribute sits on its own line.</summary>
    private static string TagOwning(string[] lines, int line, int column)
    {
        string head = lines[line][..column];
        for (int back = line; back >= 0 && line - back < 8; back--)
        {
            int open = head.LastIndexOf('<');
            if (open >= 0)
            {
                Match named = Regex.Match(head[(open + 1)..], @"^[A-Za-z][\w.]*");
                return named.Success ? named.Value : "?";
            }

            if (head.Contains('>'))
            {
                break;   // the tag before this one closed: the attribute is loose text, not an element's
            }

            head = back > 0 ? lines[back - 1] : "";
        }

        return "?";
    }

    /// <summary>The whole <c>&lt;OverlayShell …&gt;</c> opening tag that the attribute on
    /// <paramref name="line"/> belongs to: back up the file to the line that opens it, then forward to the
    /// first <c>&gt;</c> outside a quoted value — so a lambda in <c>OnClose="() =&gt; …"</c> does not end the
    /// tag early, and the tag after this one can never be read in its place.</summary>
    private static string TheTagAt(string[] lines, int line)
    {
        int start = line;
        while (start >= 0 && !lines[start].Contains("<OverlayShell", StringComparison.Ordinal))
        {
            start--;
        }

        if (start < 0)
        {
            return "";
        }

        var tag = new System.Text.StringBuilder();
        bool quoted = false;
        for (int at = start; at < lines.Length; at++)
        {
            string text = lines[at];
            int from = at == start ? text.IndexOf("<OverlayShell", StringComparison.Ordinal) : 0;
            for (int column = from; column < text.Length; column++)
            {
                quoted ^= text[column] == '"';
                tag.Append(text[column]);
                if (!quoted && text[column] == '>')
                {
                    return tag.ToString();
                }
            }

            tag.Append('\n');
        }

        return tag.ToString();
    }

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(ClientSource(), "*.razor", SearchOption.AllDirectories);

    private static string ClientSource()
    {
        var at = new DirectoryInfo(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException(
            "src/SpaceSails.Client is not above the test binary — this guard reads the markup as typed and "
            + "cannot do its job without it.");
    }
}
