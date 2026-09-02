using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 11 · <b>THE LAST TWO SURFACES, AND THE KEY THE MENUS NEVER ANSWERED.</b>
///
/// <para>This is the TAIL of the OverlayShell migration (#998 · #1000 · #1001 · #1002 · #1005 · #1007 ·
/// #1008 · #1009 · #1010 · #1012). Two surfaces were left unexamined when wave 10 closed — the help
/// checklist and the dice tray — and after them the remainder is exactly the four surfaces the owner has
/// not unlocked, each with a written-down reason. Nothing is left that nobody has looked at.</para>
///
/// <list type="number">
/// <item><b>§1 · The help checklist.</b> The plainest migration in the client and the one that finally
/// merges an anchor with the card it wrapped: <c>.map-tutorial</c> and its <c>.card</c> were the same box,
/// and they are one element now — the shape #963's scope has worn since wave 2.</item>
/// <item><b>§2 · The dice tray</b>, and the pre-question wave 10 asked about it: this is the migration's
/// first crossing of a COMPONENT BOUNDARY. §2 measures what the crossing actually cost, which is two
/// <c>::deep</c>s in a stylesheet that is not <c>Map.razor.css</c>, a dead-rule guard taught to read a
/// second pair of files — and one pixel-height finding that wave 7 had already paid for once.</item>
/// <item><b>§3 · Escape closes the four click menus — FABLE'S RULING, WAVE 11.</b> #1012 found and
/// reported, without touching it, that the four <c>.map-body-menu</c> surfaces are the one family
/// <c>TryDismissTopOverlay</c> has never listed. §3 executes the ruling and proves it by TYPING THE
/// KEY.</item>
/// </list>
///
/// <h3>Why §3 types instead of calling</h3>
///
/// <para>The same discipline #992's law took for the mouse. A test that invoked <c>TryDismissTopOverlay</c>
/// by reflection would prove a chain of <c>if</c>s clears a field and nothing about whether the ESCAPE KEY
/// reaches that chain — and the key has six gates above it in <c>OnKeyDown</c> (the desk digits, the
/// shuttle run, Enter). So <see cref="DeskBench.TypeAsync"/> dispatches a real
/// <c>KeyboardEventArgs</c> at the <c>onkeydown</c> handler id the render tree wrote for
/// <c>.map-page</c>, and the road the key takes is the player's road.</para>
/// </summary>
[SlowGate] // #251 · 26 s over 13 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheChecklistAndTheTrayTakeTheShellAndEscapeClosesTheMenusTests
{
    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  1 · The help checklist
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE CHECKLIST IS A SHELL CARD, WEARING THE PAGE'S OWN DRESS, AND ITS ✕ ENDS IT.
    ///
    /// <para>Two elements became one, and that is the half of this worth a guard. The anchor
    /// <c>.map-tutorial</c> (bottom-left, 22 rem, <c>pointer-events: auto</c>) wrapped a
    /// <c>.card bg-dark bg-opacity-75 text-light</c> that filled it exactly, with nothing between them —
    /// so the migration merges them onto the shell's root, which is the shape the scope one card along has
    /// worn since wave 2. What is asserted is that BOTH class lists survived the merge: lose the first and
    /// the checklist falls out of its corner, lose the second and it loses its frame and its ground.</para>
    /// </summary>
    [Fact]
    public async Task TheHelpChecklistIsAShellCardWhoseCrossEndsIt()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        DeskBench.Painted.Node card = await RaiseTheChecklist(bench);

        Assert.True(card.HasClass("overlay-shell") && card.HasClass("overlay-shell-card"),
            "the help checklist is not the shell's Card frame. A name on the left and a way out on the "
            + "right IS the Card frame; a Bare one would leave the ✕ loose at the end of the lesson steps.");
        Assert.True(card.HasClass("map-tutorial"),
            "the checklist has lost its anchor class. `::deep .map-tutorial` is the ONLY rule it has — "
            + "position, corner, 22 rem width and pointer-events all live there — so without it the card "
            + "falls into the document flow at the top of the Nav column.");
        foreach (string dress in new[] { "card", "bg-dark", "bg-opacity-75", "text-light" })
        {
            Assert.True(card.HasClass(dress),
                $"the checklist has lost `{dress}`. That is the half of the merge that used to be a "
                + "separate div inside the anchor — the frame, the ground and the ink.");
        }

        DeskBench.Painted.Node head = card.Children.FirstOrDefault(n => n.HasClass("overlay-shell-head") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException("the checklist has no shell head row.");
        Assert.True(head.HasClass("card-header") && head.HasClass("py-1"),
            "the checklist's head has lost the page's own `card-header py-1` — the rule that gives it its "
            + "divider and its tighter padding. The shell draws the row; the page still dresses it.");

        DeskBench.Painted.Node name = head.Descendants().First(n => n.HasClass("overlay-shell-title"));
        Assert.True(name.HasClass("fw-bold") && name.HasClass("small"), "the lesson's name has lost its dress.");
        Assert.True(name.Spoken.Length > 0,
            "the checklist's head is nameless. It names the lesson being run, or `Ship's articles` once "
            + "every lesson is behind you — and a progress tracker that does not say what it is tracking is "
            + "the head doing nothing.");

        DeskBench.Painted.Node body = card.Children.FirstOrDefault(n => n.HasClass("overlay-shell-body") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException("the checklist has no shell body.");
        Assert.True(body.HasClass("card-body") && body.HasClass("p-2") && body.HasClass("small"),
            "the checklist's body has lost the page's own `card-body p-2 small`.");

        DeskBench.Painted.Node cross = head.Descendants()
            .FirstOrDefault(n => n.HasClass("overlay-shell-dismiss") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException("the checklist's ✕ is not the shell's.");
        Assert.True(cross.HasClass("btn-close") && cross.HasClass("btn-close-white"),
            "the checklist's ✕ has lost its dress — Bootstrap's own glyph plate, and nothing else draws it.");
        Assert.Equal("Close the lesson checklist", cross.Attributes.GetValueOrDefault("title"));
        Assert.Equal("Close", cross.Attributes.GetValueOrDefault("aria-label"));

        await bench.PressAsync(cross.Handlers["onclick"]);
        Assert.Null(TheChecklist(await bench.RenderAsync()));
        Assert.False((bool)bench.Peek("_showTutorial")!,
            "the checklist's ✕ took the card off the screen without clearing `_showTutorial`. The toolbar "
            + "switch reads that same field, so the two controls would immediately disagree about whether "
            + "the checklist is showing.");
    }

    /// <summary>
    /// …AND THE ONE RULE THAT REACHES ITS ROOT FOLLOWED IT.
    ///
    /// <para>#996's bug class, asked by name of the element this wave newly handed to the shell. Named here
    /// with the sentence about what losing it costs, so the day it is reverted the failure says what was
    /// lost rather than only that a selector moved. The general form is guarded next door
    /// (<c>EveryRuleWhoseTargetTheShellDrawsIsWrittenWithDeep</c>).</para>
    /// </summary>
    [Fact]
    public void TheRuleForTheChecklistsAnchorIsWrittenWithDeep()
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Pages", "Map.razor.css")));

        var reaching = Selectors(css)
            .Where(one => one.TrimEnd().EndsWith(".map-tutorial", StringComparison.Ordinal))
            .ToList();

        Assert.True(reaching.Count > 0,
            "no rule in Map.razor.css targets `.map-tutorial` at all. Every declaration the checklist has "
            + "for where it sits and how wide it is lives in that one block, so a checklist that has lost "
            + "it is worth knowing about even if nothing here is dead.");

        var bare = reaching.Where(one => !one.TrimStart().StartsWith("::deep", StringComparison.Ordinal)).ToList();

        Assert.True(bare.Count == 0,
            $"{bare.Count} rule(s) target `.map-tutorial` without `::deep`:\n  - {string.Join("\n  - ", bare)}"
            + "\n\nOverlayShell draws this root now, so it carries the SHELL's scope attribute and not the "
            + "page's: each of these compiles to `.map-tutorial[b-map]` and matches nothing — present, "
            + "correct and dead (#996). The checklist then loses its corner, its 22 rem and its "
            + "`pointer-events: auto` in one go and falls into the Nav column's flow.");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  2 · The dice tray — the migration's first component boundary
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE TRAY IS A BARE SHELL AND ITS WAY OUT IS THE CARD'S LAST DIRECT CHILD.
    ///
    /// <para>#1012 flagged this one as "the first migration crossing a component boundary" and asked
    /// whether that meant anything beyond the mechanical. It does not, and this guard is where the answer
    /// is written: the tray's shape is #996's story-plate idiom exactly — no head, the way out last — and
    /// the boundary changed which STYLESHEET the <c>::deep</c>s go in and nothing else. Nothing threaded,
    /// no parameter added, no rule moved out of the component that owns it.</para>
    ///
    /// <para><c>Last direct child</c> is asserted rather than assumed because it is load-bearing: a Bare
    /// shell puts its dismiss after <c>ChildContent</c>, and the tray's own <c>.dice-tray-detail</c> sets
    /// the margin that stands the button off the prose above it.</para>
    /// </summary>
    [Fact]
    public async Task TheDiceTrayIsABareShellWhoseWayOutIsItsLastDirectChild()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        DeskBench.Painted.Node card = await RaiseTheTray(bench);

        Assert.True(card.HasClass("overlay-shell") && card.HasClass("overlay-shell-bare"),
            "the dice tray's card is not the shell's Bare frame. It has no head row — the 🎲 cap is a line "
            + "of the tray's own prose, not a title with a ✕ opposite it — so Card would draw it a head "
            + "row it never had.");

        Assert.True(card.Children.Any(n => n.HasClass("dice-tray-cap")),
            "the tray has lost its cap line, which is the only thing on it that says which table cast the "
            + "dice.");

        DeskBench.Painted.Node last = card.Children.Last(n => !n.Hidden);
        Assert.True(last.HasClass("overlay-shell-dismiss"),
            "the tray does not end with its way out. Under a Bare frame the dismiss IS the card's last "
            + "direct child (#996), and the detail line above it sets the margin that stands it off the "
            + "prose — anything drawn after it would sit between the two.");
        Assert.True(last.HasClass("dice-tray-close"),
            "the tray's way out has lost `dice-tray-close`, which is its whole appearance: the gold "
            + "outline, the courier face and the hover invert. Without it the crude TTRPG tray ends in a "
            + "browser default button.");
        Assert.Equal("let the dice lie", last.Name);

        await bench.PressAsync(last.Handlers["onclick"]);
        Assert.Null(TheTray(await bench.RenderAsync()));
        Assert.Null(bench.Peek("_diceTrayEvent"));
    }

    /// <summary>
    /// THE CARD STILL SWALLOWS ITS OWN CLICKS, AND THE BACKDROP STILL ENDS THE TRAY.
    ///
    /// <para>Two halves of one design that the migration had to carry across intact. The backdrop is
    /// <c>pointer-events: none</c> and closes on a click; the card is <c>pointer-events: auto</c> and
    /// stops the click going through. A shell that let its own clicks bubble would close the tray under
    /// the captain's hand the moment he clicked the dice to read them — which is exactly what
    /// <c>StopClicks</c> is for, and it is a PARAMETER now rather than a hand-typed
    /// <c>@@onclick:stopPropagation</c>.</para>
    ///
    /// <para>The stop is read off the markup as typed, because a swallowed click leaves no trace in a
    /// render tree: <c>stopPropagation</c> is a flag the renderer hands to the JS side, not an attribute a
    /// walk can see. The backdrop half IS pressed.</para>
    /// </summary>
    [Fact]
    public async Task TheTrayStillSwallowsItsOwnClicksAndItsBackdropStillEndsIt()
    {
        string tray = File.ReadAllText(Path.Combine(ClientSource(), "Components", "DiceTray.razor"));
        Assert.Contains("StopClicks=\"true\"", tray, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick:stopPropagation", tray, StringComparison.Ordinal);

        using DeskBench bench = await DeskBench.BootAsync(Docked);
        _ = await RaiseTheTray(bench);

        DeskBench.Painted.Node backdrop = (await bench.RenderAsync()).Root.Descendants()
            .First(n => n.HasClass("dice-tray") && !n.HasClass("dice-tray-card"));
        Assert.True(backdrop.Handlers.ContainsKey("onclick"),
            "the tray's backdrop no longer closes on a click. It is the page's own element — the shell "
            + "draws the card inside it, never the click-catcher around it — and clicking off a reveal is "
            + "one of the two ways this tray has always been dismissed.");

        await bench.PressAsync(backdrop.Handlers["onclick"]);
        Assert.Null(TheTray(await bench.RenderAsync()));
    }

    /// <summary>
    /// THE TRAY'S OWN STYLESHEET LEARNED <c>::deep</c>, AND THIS IS THE WHOLE COST OF THE BOUNDARY.
    ///
    /// <para>The identical bug class as every wave before it, in a file that is not <c>Map.razor.css</c>.
    /// Blazor scopes a stylesheet to the component that RENDERED the element, and that is true of a child
    /// component's own sheet exactly as it is of a page's: the moment OverlayShell draws
    /// <c>.dice-tray-card</c>, <c>.dice-tray-card[b-dicetray]</c> matches nothing. <c>::deep</c> reaches it
    /// again through the backdrop, which this component still draws — and the backdrop being the page's is
    /// what makes the idiom available here at all.</para>
    /// </summary>
    [Theory]
    [InlineData(".dice-tray-card", "the tray's whole frame — the gold border, the black ground, the 30rem "
        + "cap, and the `pointer-events: auto` that makes it the one element in a `pointer-events: none` "
        + "backdrop that can catch a click at all")]
    [InlineData(".dice-tray-close", "the way out's entire appearance: the gold outline, the courier face, "
        + "the hover invert and the height it is drawn with")]
    public void TheRulesForWhatTheShellDrawsInTheTrayAreWrittenWithDeep(string target, string what)
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Components", "DiceTray.razor.css")));

        var reaching = Selectors(css)
            .Where(one => Regex.IsMatch(one, $@"{Regex.Escape(target)}(\s|:|$)")
                          || one.TrimEnd().EndsWith(target, StringComparison.Ordinal))
            .ToList();

        Assert.True(reaching.Count > 0,
            $"no rule in DiceTray.razor.css targets `{target}` at all — it is {what}.");

        var bare = reaching.Where(one => !one.TrimStart().StartsWith("::deep", StringComparison.Ordinal)).ToList();

        Assert.True(bare.Count == 0,
            $"{bare.Count} rule(s) target `{target}` without `::deep`:\n  - {string.Join("\n  - ", bare)}"
            + $"\n\nOverlayShell draws that element now, so it carries the shell's scope attribute and not "
            + "this component's: each of these compiles to a selector that matches nothing — present, "
            + $"correct and dead (#996). It is {what}.");
    }

    /// <summary>
    /// …AND THE WAY OUT KEEPS THE HEIGHT IT WAS DRAWN WITH — <b>wave 7's eight pixels, found again.</b>
    ///
    /// <para>The one non-mechanical thing the boundary turned up, and it is not about the boundary at all.
    /// OverlayShell's stylesheet puts <c>line-height: 1</c> on every way out that is not a Bootstrap
    /// <c>.btn</c>. Wave 7 measured what that costs a WORDED button — eight pixels off <i>…wake up</i>, off
    /// the shelf, off <i>Close hatch</i> — and answered it by carving <c>.btn</c> out, because a <c>.btn</c>
    /// already carries a line-height of its own and the honest fix was for the shell to stop overwriting
    /// it.</para>
    ///
    /// <para><i>let the dice lie</i> is the first worded way out in this client that is NOT a <c>.btn</c>,
    /// so it falls straight back into the rule wave 7 wrote the carve-out for, and it would have come out
    /// shorter than it was drawn. The tray states the height it always had; one class beats a
    /// <c>:where()</c> of none.</para>
    ///
    /// <para><b>TWO pixels, measured in a real Chrome rather than guessed:</b> with the statement the
    /// button is <b>24 px</b> tall (<c>line-height: normal</c>); with it removed the shell's rule computes
    /// <c>line-height: 12.48px</c> and the button is <b>22 px</b>. Smaller than wave 7's eight because the
    /// tray's face is <c>0.78rem</c> against a Bootstrap button's, and that is exactly why this guard
    /// exists: two pixels on one button is a thing no reviewer and no screenshot would ever catch, and it
    /// is still the migration moving something it promised not to move.</para>
    /// </summary>
    [Fact]
    public void TheTraysWayOutStatesTheHeightItWasAlwaysDrawnWith()
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Components", "DiceTray.razor.css")));

        Match rule = Regex.Match(css, @"::deep\s+\.dice-tray-close\s*\{(?<body>[^{}]*)\}");
        Assert.True(rule.Success, "DiceTray.razor.css no longer has a `::deep .dice-tray-close` block.");
        Assert.Matches(@"line-height\s*:\s*normal", rule.Groups["body"].Value);

        // …and the rule it is answering is still the one that would take the pixels: if the shell ever
        // stops setting a line-height on a non-.btn way out, this statement becomes a leftover rather than
        // a fix, and somebody should be told which of the two to remove.
        string shell = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "Components", "OverlayShell.razor.css")));
        Assert.Contains("overlay-shell-dismiss", shell, StringComparison.Ordinal);
        Assert.Matches(@":where\(:not\(\.btn\)\)\s*\{\s*line-height:\s*1;?\s*\}", shell);
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  3 · Fable's ruling, wave 11 — Escape closes the four click menus
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ESCAPE CLOSES EACH CLICK MENU, AND DOES NOT DO THE OTHER THING IT USED TO DO.
    ///
    /// <para>Four surfaces, one key, typed at the page. Each menu is raised with its three siblings put
    /// DOWN first — they share <c>.map-body-menu</c>, and a guard that read "the first one on the screen"
    /// would report one menu under another's name, which is this repository's first named bug class in a
    /// test.</para>
    ///
    /// <para><b>The second assertion is the bug, and it is worse than a key doing nothing.</b> Escape's
    /// fall-through in <c>OnKeyDown</c> is <c>SwitchDesk(Nav)</c>, so before this wave the key MOVED THE
    /// CAPTAIN TO A DIFFERENT DESK and left the menu's gate set. Which bad ending he got depended on which
    /// menu it was, and the red proof for this guard shows both: the chooser, the body menu and the contact
    /// menu draw on Nav as well, so they FOLLOWED him there wearing the inline anchor of a click made on
    /// another desk's map; the open-sky menu draws on Sensors only, so it vanished off the glass with
    /// <c>_skyMenuWorld</c> still holding a point and came back the moment he returned. Hence the desk is
    /// asserted unchanged — the half a "did the menu close?" test would have missed, and the half that says
    /// the key was not merely inert.</para>
    /// </summary>
    [Theory]
    [InlineData("the pick-candidate chooser", "_pickMenu")]
    [InlineData("the body menu", "_bodyMenuBody")]
    [InlineData("the contact menu", "_shipMenuId")]
    [InlineData("the open-sky menu", "_skyMenuWorld")]
    public async Task EscapeClosesEachClickMenuAndLeavesTheCaptainWhereHeWas(string what, string gate)
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        _ = await TheClickMenusTakeTheShellAndTheContractTakesItsFootTests.Raise(bench, what);

        ShipDesk standingAt = bench.ActiveDesk;
        Assert.NotNull(bench.Peek(gate));

        await PressEscape(bench);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.Null(TheClickMenusTakeTheShellAndTheContractTakesItsFootTests.TheMenu(after));
        Assert.Null(bench.Peek(gate));

        Assert.Equal(standingAt, bench.ActiveDesk);
    }

    /// <summary>
    /// THE ORDERING CASE — <b>a menu over a card takes the key first, and the card underneath is left
    /// alone.</b>
    ///
    /// <para>The dossier is the right card to ask this of: it is the one surface a click menu genuinely
    /// stacks over (the contact menu is raised BY a click on the same contact whose file is open), it is
    /// #960's tucking card rather than a modal, and — said out loud rather than smuggled — <b>it is not in
    /// the cancel chain either</b>. That is deliberately unchanged here. This wave was asked to seat the
    /// four menus in the law, not to re-rule the tucking instruments, and the dossier surviving the press
    /// is what makes the assertion mean something: the key took the menu and nothing else moved.</para>
    /// </summary>
    [Fact]
    public async Task EscapeTakesTheMenuAndLeavesTheDossierUnderItStanding()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying + "&target=collector");

        DeskBench.Painted.Node? file = TheCard(await bench.RenderAsync(), "map-dossier");
        Assert.True(file is not null,
            "?target=collector booted no dossier, so there is nothing for a menu to stack over and this "
            + "guard would be measuring one surface instead of two — the fifth named bug class.");

        _ = await TheClickMenusTakeTheShellAndTheContractTakesItsFootTests.Raise(bench, "the body menu");
        Assert.NotNull(TheCard(await bench.RenderAsync(), "map-dossier"));

        await PressEscape(bench);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.Null(TheClickMenusTakeTheShellAndTheContractTakesItsFootTests.TheMenu(after));
        Assert.True(TheCard(after, "map-dossier") is not null,
            "Escape took the menu AND the dossier under it. The cancel key peels ONE layer — that is the "
            + "whole discipline of TryDismissTopOverlay's chain of early returns — and a captain who "
            + "pressed it to shut a menu he opened by accident has just lost the file he was reading.");
    }

    /// <summary>
    /// THE LAW ITSELF, READ AS TYPED: ALL FOUR MENUS ARE IN THE CANCEL CHAIN, LAST, IN PAINT ORDER — AND
    /// NONE OF THEM IS IN THE CONFIRM CHAIN.
    ///
    /// <para>The behaviour is proved above by typing; this is the RULING, which is a different claim. Three
    /// things are held:</para>
    ///
    /// <list type="bullet">
    /// <item><b>All four.</b> The family is one mechanism now (#1012) and a law that listed three of it
    /// would be the inconsistency this wave exists to end, one menu smaller.</item>
    /// <item><b>Last, and in reverse paint order.</b> A click menu is the least modal thing in the chain —
    /// a list hanging off a spot on the map, with everything else drawn over it — and among the four,
    /// "topmost" can only honestly mean the one painted last. The sky menu is written last in Map.razor.
    /// </item>
    /// <item><b>Not in the confirm chain.</b> Enter answers only cards that ask nothing, and a menu is a
    /// list of things the captain MAY do. A key that picked one of them for him is the exact thing that
    /// chain refuses to do — and its absence is a feature that has to be defended, because it looks like an
    /// omission.</item>
    /// </list>
    /// </summary>
    [Fact]
    public void EveryClickMenuIsInTheCancelKeyLawInPaintOrderAndNoneIsInTheConfirmChain()
    {
        string cancel = File.ReadAllText(Path.Combine(ClientSource(), "Pages", "Map.Sim.Cancel.cs"));

        int esc = cancel.IndexOf("private bool TryDismissTopOverlay()", StringComparison.Ordinal);
        int yes = cancel.IndexOf("private bool TryConfirmTopOverlay()", StringComparison.Ordinal);
        Assert.True(esc > 0 && yes > esc, "the two keyboard chains are not both in Map.Sim.Cancel.cs.");

        string escChain = cancel[esc..yes];
        string yesChain = cancel[yes..];

        // Reverse paint order: the sky menu is written LAST in Map.razor, so it is drawn on top and peeled
        // first. Named with the closer each one calls, so a line that peeled a menu by writing its field
        // instead of calling the house closer the ✕ calls does not count.
        string[] inOrder =
        [
            "if (_skyMenuWorld is not null) { CloseSkyMenu(); return true; }",
            "if (_shipMenuId is not null) { CloseShipMenu(); return true; }",
            "if (_bodyMenuBody is not null) { CloseBodyMenu(); return true; }",
            "if (_pickMenu is not null) { ClosePickMenu(); return true; }",
        ];

        int at = 0;
        foreach (string line in inOrder)
        {
            int found = escChain.IndexOf(line, StringComparison.Ordinal);
            Assert.True(found >= 0,
                $"the cancel key does not reach a click menu: `{line}` is not in TryDismissTopOverlay.\n\n"
                + "Fable's ruling, wave 11: the four .map-body-menu surfaces are one mechanism (#1012) and "
                + "every other card in this client obeys the cancel key. A family that ignores it is "
                + "#351's own complaint — the owner's, verbatim, \"No way to close this dialog? Where is "
                + "cancel?\" — one family over. Worse than nothing happening: Escape's fall-through is "
                + "SwitchDesk(Nav), so the key moves the captain to another desk and the menu follows him "
                + "there.");
            Assert.True(found > at,
                $"`{line}` is out of order in the cancel chain. The four menus are peeled in REVERSE PAINT "
                + "ORDER — the sky menu is written last in Map.razor and therefore drawn on top — so that "
                + "\"topmost first\" means one thing in this file and not two.");
            at = found;
        }

        // …and they are the LAST thing the chain tries.
        string tail = escChain[at..];
        Assert.DoesNotContain("return true;", tail[(tail.IndexOf('\n') + 1)..], StringComparison.Ordinal);

        foreach (string gate in new[] { "_skyMenuWorld", "_shipMenuId", "_bodyMenuBody", "_pickMenu" })
        {
            Assert.DoesNotContain(gate, yesChain, StringComparison.Ordinal);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  Plumbing
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    // Spelled exactly as the dismissibility law spells them, so a surface driven here and the same surface
    // driven there can never be standing in two different places.
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";
    private const string FreeFlying = "/map?start=wreck";

    /// <summary>
    /// TYPE ESCAPE AT THE PAGE, through the handler the render tree wrote for <c>.map-page</c>.
    ///
    /// <para><b>The arming poke, said out loud.</b> <c>OnKeyDown</c>'s first act is #338's gesture unlock:
    /// the first key of a session calls <c>RendererInterop.ArmAudio()</c>, which is a <c>[JSImport]</c> and
    /// therefore reaches for a browser this bench does not have. Off-browser it throws, and the throw takes
    /// the whole handler with it — so the FIRST key a bench types is swallowed by the same documented gate
    /// <c>TrackingPost</c>'s canvas has always arrived through. The flag is set here rather than paying for
    /// it with a warm-up keystroke, because a test whose subject is "which card the key reaches" should not
    /// be quietly measuring one press behind.</para>
    /// </summary>
    private static async Task PressEscape(DeskBench bench)
    {
        bench.Poke("_audioArmed", true);
        ulong keyboard = DeskBench.TheKeyboard(await bench.RenderAsync());
        Assert.True(keyboard != 0,
            "the page draws no `.map-page` element with an onkeydown handler on it. That div is where every "
            + "key in this game lands (Map.razor:14), and a page without one is a page that has gone deaf — "
            + "which is worth failing over rather than skipping.");

        await bench.TypeAsync(keyboard, "Escape");
    }

    /// <summary>The checklist, raised the way the dismissibility register raises it — same desk, same gate,
    /// so the two can never be asking about a card in two different places.</summary>
    private static async Task<DeskBench.Painted.Node> RaiseTheChecklist(DeskBench bench)
    {
        await bench.SwitchAsync(ShipDesk.Nav);
        bench.Poke("_showTutorial", true);

        return TheChecklist(await bench.RenderAsync())
               ?? throw new Xunit.Sdk.XunitException(
                   "raising the checklist drew nothing wearing `.map-tutorial`. The gate this bench sets "
                   + "and the gate the markup reads have come apart, or the card is no longer drawn inside "
                   + "the Nav desk's own column.");
    }

    /// <summary>The tray, raised through the SHIPPING seam every dice-scripted system uses. Not a poke at
    /// <c>_diceTrayEvent</c>: <c>RaiseDiceEvent</c> is the one entry #305 wrote for exactly this, and a seam
    /// that stopped raising the tray should fail in a guard rather than nowhere.</summary>
    private static async Task<DeskBench.Painted.Node> RaiseTheTray(DeskBench bench)
    {
        bench.Call("RaiseDiceEvent", ARoll());

        return TheTray(await bench.RenderAsync())
               ?? throw new Xunit.Sdk.XunitException(
                   "RaiseDiceEvent drew nothing wearing `.dice-tray-card`. Either the shared seam has "
                   + "stopped reaching the tray or the <DiceTray> component has left the page.");
    }

    /// <summary>One cast of the dice with a named modifier on it, so the tray has a math line to spell as
    /// well as faces to paint.</summary>
    internal static DiceEvent ARoll() =>
        new("AEROBRAKE", "2D6", [4, 5], [new DiceModifier("load", -2)], 7,
            "The haze took a bite and gave it back.", "One pass saved; the sail held.");

    private static DeskBench.Painted.Node? TheChecklist(DeskBench.Painted painted) =>
        TheCard(painted, "map-tutorial");

    private static DeskBench.Painted.Node? TheTray(DeskBench.Painted painted) =>
        TheCard(painted, "dice-tray-card");

    private static DeskBench.Painted.Node? TheCard(DeskBench.Painted painted, string root) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(root) && !n.Hidden);

    private static IEnumerable<string> Selectors(string css) =>
        Regex.Matches(css, @"([^{}]+)\{[^{}]*\}")
            .SelectMany(rule => rule.Groups[1].Value.Split(','))
            .Select(one => one.Trim())
            .Where(one => one.Length > 0);

    private static string WithoutComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", " ", RegexOptions.Singleline);

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
