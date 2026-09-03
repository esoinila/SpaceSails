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
/// <para><b>Three of the four are the shell's and one is not, and that split is the point of this file.</b>
/// The bank sheet and the import consent are decisions: no ✕, because every answer each of them offers is
/// itself a close — the critical-decision exception, proved here by PRESSING all five answers rather than
/// taken from a parameter. The logbook joined them in #997 wave 8: its way out is still the third button of
/// <c>.save-surface-foot</c>, beside ⬇ Export and ⬆ Import, and the shell now HANDS that row the dismiss
/// it draws (<c>FootHost</c>) instead of drawing a lone one below it. The front door alone stays the page's
/// own, and not because the shell cannot reach it — because it has no way out at all: there is nothing
/// behind it to go back to, which is Fable's wave-7 ruling that it is the game's threshold rather than a
/// pop-up. It is named below with that reason, and the guards at the bottom of this file pin the reason
/// itself — a straggler whose stated reason has quietly stopped being true is worse than no reason.</para>
///
/// <para>The dismissibility law (<see cref="EveryPopUpCanBeDismissedTests"/>) asks whether a surface can be
/// got rid of at all, and it already drives this family — including the sibling reset its own register row
/// documents, because the logbook's <i>⤓ bank here</i> raises the bank sheet on the same root class. What a
/// MIGRATION can break is narrower, and this file asks that instead.</para>
/// </summary>
[SlowGate] // #251 · 30 s over 13 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
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

                    // A shell: read THIS element — the one whose attribute we are standing on, found by
                    // walking back from this line, never by searching the file for the class list (two
                    // shells in this family wear the same one, and the first hit is the hand-rolled front
                    // door). The whole element and not just its opening tag, because the second shape this
                    // family is allowed to take is declared by a CHILD (<FootHost>) rather than by an
                    // attribute.
                    string element = TheElementAt(lines, i);
                    if (!element.Contains("OverlayDismiss.ByDecision", StringComparison.Ordinal)
                        && !element.Contains("<FootHost", StringComparison.Ordinal))
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
            $"{wrongShape.Count} start-picker shell(s) are neither a ByDecision nor a page-drawn foot:\n  - "
            + string.Join("\n  - ", wrongShape)
            + "\n\nA plain Close here would draw a ✕ as the LAST DIRECT CHILD of a save sheet — under the "
            + "answer row, in a family that has never carried one and has no rule written for it. This "
            + "family is allowed exactly two shapes. The two sheets are DECISIONS: every answer they offer "
            + "is itself a close, which is what the press guard in this file establishes. The logbook is a "
            + "Close whose way out the page's own `.save-surface-foot` HOSTS (#997 wave 8's FootHost), so "
            + "the button lands in the row it has always been in. If a third sheet has really earned a "
            + "loose ✕, say so here first.");
    }

    /// <summary>
    /// THE STRAGGLER, BY NAME AND WITH THE REASON — and by #997 wave 8 it is ONE surface rather than two.
    ///
    /// <para>Keyed on the class list exactly as typed, because that list IS the surface's identity under the
    /// alias law and it is the one thing about it a refactor is not allowed to move. Two elements in
    /// Map.razor wear this list now — the front door's <c>&lt;div&gt;</c> and the logbook's
    /// <c>&lt;OverlayShell&gt;</c> — and only the first of them is hand-rolled, which is what leaves this
    /// entry standing and its reason down to one half.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> TheNamedStragglers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["start-picker save-surface"] =
                "the FRONT DOOR, and only the front door: wave 6 named this key for two surfaces on the "
                + "grounds that they were one element drawn twice off the SaveLoadSurface fragment, and "
                + "#997 wave 8 took that reason away — the shared part is now SaveLoadInside, the logbook "
                + "has its own <OverlayShell> root, and its Close is the shell's dismiss standing in the "
                + "page's own `.save-surface-foot` (FootHost). What is left is the half that was never "
                + "about the markup: THE FRONT DOOR HAS NO WAY OUT AT ALL, because there is nothing behind "
                + "it to go back to — you leave it by starting or loading a game. So none of the shell's "
                + "three shapes fits it. A Close would draw a ✕ this door has never had; a Minimize would "
                + "tuck the threshold into a corner of a game that has not started; and a ByDecision would "
                + "claim that every control on it is itself a close, which is false (⬆ Import a save file, "
                + "📥 file, 🗑, the ▸ dev-starts chevron and the ✏ pencil all leave the door standing). "
                + "That is not a surface waiting for a shell — it is Fable's wave-7 ruling in markup: the "
                + "front door is the game's THRESHOLD and not a pop-up over play, which is why it sits in "
                + "EveryPopUpCanBeDismissedTests' NotPopUpsAndWhy rather than in its register. Pinned by "
                + "TheFrontDoorOffersNoCloseAtAllAndNotEveryControlOnItIsOne at the foot of this file.",
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
    /// THE LOGBOOK IS THE SHELL'S, AND ITS WAY OUT IS STILL THE THIRD BUTTON OF THE PAGE'S OWN ROW.
    ///
    /// <para>Wave 6 named this surface a straggler because a <c>Bare</c> shell draws its dismiss as the
    /// card's LAST DIRECT CHILD, and that would have lifted <i>Close</i> out of <c>.save-surface-foot</c>
    /// and dropped it on a line of its own below the row. #997 wave 8's <c>FootHost</c> is the answer to
    /// exactly that: the PAGE goes on drawing the row — its own element, its own scope attribute, and only
    /// once the reactor is warm — and the shell hands it the way out to put in it.</para>
    ///
    /// <para>So this guard asks for both halves at once, because either alone would be the migration
    /// failing quietly: the button is the SHELL'S (it wears <c>overlay-shell-dismiss</c>, so the audit and
    /// the dismissibility law both reach it) and it is still exactly where it was (the third and last
    /// control of a foot it shares with ⬇ Export and ⬆ Import, neither of which is a way out). And it is
    /// PRESSED, because a way out that does not close is the thing this whole lane exists to catch.</para>
    ///
    /// <para>It also asks for what the straggler used to COST: the classless wrapper carrying
    /// <c>@@onclick:stopPropagation</c> between the backdrop and the surface. <c>StopClicks="true"</c> owns
    /// that now, so the surface is the backdrop's own child — one div fewer, and the behaviour identical
    /// (a click on the sheet must not fall through to the backdrop, which closes the drawer).</para>
    /// </summary>
    [Fact]
    public async Task TheLogbookIsTheShellsAndItsWayOutIsStillTheThirdButtonOfTheRow()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        OnlyThisStartPickerSurface(bench, "_showSaveDrawer");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheSurface(painted, "")
            ?? throw new Xunit.Sdk.XunitException(
                "the logbook: _showSaveDrawer was raised and nothing wearing .start-picker came back.");

        Assert.True(card.HasClass("overlay-shell"),
            "the logbook is on the screen and it is not the shell's — #997 wave 8 put it on OverlayShell, "
            + "and it has come back off.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            "the logbook's shell drew it as something other than Bare. Hosted's `display: contents` would "
            + "put a wrapper between this root and .save-surface-foot, and Card would wrap the page's own "
            + ".start-picker-title in an overlay-shell-head that no rule in Map.razor.css is written for.");
        Assert.True(card.HasClass("start-picker") && card.HasClass("save-surface"),
            "the logbook has lost its own class. The shell is the MECHANISM and .start-picker is the "
            + "IDENTITY — #992's completeness guard reads that name off the markup as typed, and the whole "
            + "of this surface's skin hangs off it.");

        DeskBench.Painted.Node foot = card.Descendants()
            .FirstOrDefault(n => n.HasClass("save-surface-foot") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException("the logbook has no .save-surface-foot at all.");

        Assert.False(foot.HasClass("overlay-shell-ways"),
            "the logbook's foot is drawn by the SHELL now. That is the other half of wave 7's mechanism "
            + "(WaysClass) and it is the wrong half for this surface: the shell would have to know that "
            + "the row carries ⬇ Export and ⬆ Import, and that it exists only once _worldReady. FootHost "
            + "is what lets the page keep drawing its own row.");

        List<DeskBench.Painted.Node> row = foot.Children
            .Where(n => !n.Hidden && (n.Handlers.ContainsKey("onclick") || n.Element == "a"))
            .ToList();

        Assert.True(row.Count == 3,
            $"the logbook's foot is a row of {row.Count} control(s) and it has always been three: ⬇ Export "
            + "this moment, ⬆ Import file, and Close. If the row has really changed, say so here — the "
            + "whole point of hosting the shell's dismiss in it is that the row is the page's own.");

        DeskBench.Painted.Node close = row[^1];
        Assert.Equal("Close", close.Name);
        Assert.True(close.HasClass("overlay-shell-dismiss"),
            "the logbook's Close is the last button of the page's own row, but it is not the SHELL'S — it "
            + "has gone back to being a hand-typed button. Then the surface is a shell with a ✕ nobody "
            + "draws, and the audit that watches for a way out wired to nothing cannot see this one.");
        foreach (string dressed in new[] { "btn", "btn-sm", "btn-outline-info" })
        {
            Assert.True(close.HasClass(dressed),
                $"the logbook's Close has lost `{dressed}`. The shell owns the MECHANISM; the page's own "
                + "DismissClass owns how the button looks, and this migration does not repaint a control.");
        }

        // …and the wrapper the straggler used to pay for is gone: the surface is the backdrop's own child.
        DeskBench.Painted.Node backdrop = painted.Root.Descendants()
            .First(n => n.HasClass("start-picker-backdrop") && !n.Hidden);
        Assert.Contains(backdrop.Children, n => n.HasClass("start-picker"));

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
        string css = WithoutComments(MapStylesheet.Text);

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

    /// <summary>The whole <c>&lt;OverlayShell&gt;…&lt;/OverlayShell&gt;</c> the attribute on
    /// <paramref name="line"/> belongs to: from the line that opens the tag to the line that closes the
    /// element. Read whole rather than as a tag because one of the two shapes this family is allowed —
    /// a way out hosted by the page's own foot — is declared by a child element, not by an attribute.
    /// <para>Nothing in this client nests an OverlayShell inside another, so the first closing tag is this
    /// one's; if that ever stops being true, this reads short rather than long, which fails loudly.</para>
    /// </summary>
    private static string TheElementAt(string[] lines, int line)
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

        int end = start;
        while (end < lines.Length && !lines[end].Contains("</OverlayShell>", StringComparison.Ordinal))
        {
            end++;
        }

        return string.Join('\n', lines[start..Math.Min(end + 1, lines.Length)]);
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
