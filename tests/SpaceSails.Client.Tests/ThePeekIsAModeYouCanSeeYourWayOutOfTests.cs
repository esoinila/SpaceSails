using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1038 · <b>THE PEEK IS A MODE, AND THE POP-UP LAW REACHES IT.</b>
///
/// <para>Owner, with screenshots, after pressing 👁 Peek at the Captain's desk: <i>"I thing esc-key should end
/// the peek. Also peek button should remain visible."</i> And the question underneath it, which turned out to
/// be the worse half: <i>"Are the other buttons also there even though they are invisible during the
/// peek?"</i></para>
///
/// <h3>What was actually wrong, and why nothing caught it</h3>
///
/// <para><b>The latch.</b> Peek fades every overlay on the map page. The desk tab bar has been EXEMPT from that
/// fade since the day peek was written, for the obvious reason — the bar carries the 👁 button that turns peek
/// off — and the exemption was spelled as a child combinator: <c>.map-peek &gt; *:not(.map-canvas)</c>
/// <c>:not(.desk-tab-bar):not(.map-loading)</c>. Then #992 wrapped the top stack and the Nav HUD in
/// <c>.map-flowcolumn</c> so the window-height arithmetic could be measured instead of guessed, and the bar
/// stopped being a direct child of the page. A <c>&gt;</c> exemption that names a grandchild matches nothing:
/// the column faded as ONE box, at opacity 0, with the only labelled way out inside it. A layout change
/// repealed a law, silently, and every test in this repository stayed green — because the law had never been
/// written down as a test.</para>
///
/// <para><b>The blind controls.</b> <c>pointer-events: none</c> on the faded box did not reach the controls
/// inside it. <c>pointer-events</c> INHERITS, and an inherited value loses to a descendant's own declaration
/// however <c>!important</c> the ancestor was — and <c>.map-hud</c>'s toolbar, readouts, frame chip and plot
/// panel all declare <c>pointer-events: auto</c> on purpose, so the canvas stays grabbable through the gaps
/// between them. Opacity 0 is not a hit-test barrier either. So the answer to the owner's question is YES:
/// every HUD control was still there and still pressable while invisible, for as long as peek has existed. The
/// fix is <c>visibility: hidden</c>, which is inherited, which nothing in this client ever declares its way
/// back out of, and which takes a subtree out of hit-testing AND out of the tab order.</para>
///
/// <h3>The four guards, and what each one is for</h3>
///
/// <list type="number">
/// <item><b><see cref="EscapeEndsThePeek"/> — the owner's first ask, proved by TYPING THE KEY.</b> Not by
/// calling <c>TryDismissTopOverlay</c>: the key has six gates above it in <c>OnKeyDown</c>, and the failure
/// mode this repository knows best is a chain that works and a key that never reaches it.</item>
/// <item><b><see cref="EscapeCannotStartAPeek"/> — the one-way door.</b> Escape means <i>stop</i>; a cancel key
/// wired to the TOGGLE would blank the panels on a clear screen and look broken doing it.</item>
/// <item><b><see cref="ThePeekIsPeeledBeforeAnyCardUnderneathIt"/> — the precedence.</b> While peek is on,
/// every card in the cancel chain is invisible, so a press that peeled one would be spending a told-once beat
/// the captain never saw. That is #1027's bug shape exactly, and this pins the answer.</item>
/// <item><b><see cref="TheWayOutIsNeverInsideAnythingThePeekFades"/> — the latch itself, and the only one that
/// reads CSS.</b> It applies the SHIPPING <c>.map-peek</c> rule, parsed out of <c>app.css</c>, to the REAL
/// render tree, and walks the ancestor chain of the 👁 button asking whether any box on the way to it fades.
/// On the build the owner was looking at, <c>.map-flowcolumn</c> answers yes.</item>
/// </list>
///
/// <h3>Why guard 4 matches selectors instead of naming classes</h3>
///
/// <para>Because "the tab bar is exempt" was already written down — in the stylesheet, in English, in a comment
/// that was still true about its intent and false about its effect. A guard that asserted <c>.desk-tab-bar</c>
/// appears in a <c>:not()</c> would have passed on the broken build. The only question worth asking is the
/// browser's own: given this rule and this tree, is the way out inside something that goes to zero. So there is
/// a small selector matcher here, and it is fed the rule as shipped and the tree as drawn. The anti-vacuum half
/// matters as much: <see cref="TheWayOutIsNeverInsideAnythingThePeekFades"/> also asserts that the rule DOES
/// fade the Nav HUD, because a rule that matched nothing would pass the first half perfectly.</para>
///
/// <para><b>What is left to the browser.</b> Whether the surviving button is on the screen in PIXELS, and
/// whether a faded control still answers <c>document.elementFromPoint</c>, are questions about layout and
/// hit-testing that no off-browser bench can answer (#680's law — in the DOM is not on the screen). Those are
/// asserted in <c>SpaceSails.UiGate.ThePeekLeavesAWayOutTests</c>, on real pixels, through the player's own
/// keystroke.</para>
/// </summary>
public sealed class ThePeekIsAModeYouCanSeeYourWayOutOfTests
{
    // Spelled exactly as the dismissibility law and the cancel-chain law spell it, so a page driven here and
    // the same page driven there can never be standing in two different places.
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  1 · The key
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ESCAPE ENDS THE PEEK — and it ends ONLY the peek.
    ///
    /// <para>The second half is not decoration. Escape's fall-through in <c>OnKeyDown</c> is
    /// <c>SwitchDesk(Nav)</c>, so on the build the owner reported, pressing Escape while peeking at the
    /// Captain's desk did not do nothing: it left the panels blank AND moved him to another desk. The key
    /// did something, and then lied about it (#997 wave 11's finding, one mode over).</para>
    /// </summary>
    [Fact]
    public async Task EscapeEndsThePeek()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Captain);

        bench.Poke("_peekMap", true);
        Assert.True((bool)bench.Peek("_peekMap")!, "the bench could not put the page into a peek at all.");

        await PressEscape(bench);

        Assert.False(
            (bool)bench.Peek("_peekMap")!,
            "Escape did not end the peek. Owner, verbatim: \"I thing esc-key should end the peek.\" The peek "
            + "hides every panel in the game, so a captain who presses it and then reaches for the cancel key "
            + "— the key that ends every other mode in this client — is looking at a blank sky that will not "
            + "answer him. Add `if (_peekMap) { EndPeekMap(); return true; }` to the TOP of "
            + "TryDismissTopOverlay (#1038).");

        Assert.Equal(ShipDesk.Captain, bench.ActiveDesk);
    }

    /// <summary>
    /// AND IT CANNOT START ONE. Escape is the cancel key; wiring it to <c>TogglePeekMap</c> instead of
    /// <c>EndPeekMap</c> would make a stray press on a clear screen blank every panel — a key that means
    /// "stop" doing the thing it is supposed to stop.
    /// </summary>
    [Fact]
    public async Task EscapeCannotStartAPeek()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);

        Assert.False((bool)bench.Peek("_peekMap")!, "the page booted already peeking, which it must not.");

        await PressEscape(bench);

        Assert.False(
            (bool)bench.Peek("_peekMap")!,
            "Escape STARTED a peek. The cancel key may only ever end one — a press that blanks every panel "
            + "on a screen that had nothing open is the #603 class (a control that quietly does the wrong "
            + "thing), and the captain's next press of the same key would look like the game ignoring him. "
            + "The chain must call EndPeekMap, never TogglePeekMap (#1038).");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  2 · The precedence
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE PEEK IS PEELED FIRST, AND THE CARD UNDERNEATH IS LEFT STANDING.
    ///
    /// <para>#1027 moved the first-ground family to the head of the cancel chain because Escape over a VISIBLE
    /// ground lesson was peeling an INVISIBLE story card beneath it. Peek is that fact taken to its limit:
    /// while it is on, EVERY surface in the chain is at opacity 0 and visibility hidden, so any gate above the
    /// peek would be a blind dismissal — spending a told-once beat the captain never read, with no way for him
    /// to know it happened.</para>
    ///
    /// <para>Driven through <c>TryDismissTopOverlay</c> itself rather than the key, because the subject here is
    /// the ORDER of the chain and not whether the key reaches it — guard 1 already proved the key reaches it.
    /// Two gates are set, one press is spent, and which one it spent is the whole assertion.</para>
    /// </summary>
    [Fact]
    public async Task ThePeekIsPeeledBeforeAnyCardUnderneathIt()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);

        bench.Poke("_peekMap", true);
        bench.Poke("_galleyCardOpen", true);

        object? consumed = bench.CallOnTheDispatcher("TryDismissTopOverlay");
        Assert.True(consumed is true, "the cancel chain did not consume the press at all.");

        Assert.False(
            (bool)bench.Peek("_peekMap")!,
            "the cancel chain reached past the peek to a card underneath it. The peek is the only thing on "
            + "the glass — everything below it in this chain is invisible — so it takes the top of the chain "
            + "(#1038).");

        Assert.True(
            (bool)bench.Peek("_galleyCardOpen")!,
            "the first press peeled the GALLEY CARD, which the captain could not see, and left him looking "
            + "at the same blank sky. That is #1027's bug exactly, one mode over: a cancel key that spends "
            + "itself on a surface nobody can look at. The peek goes above every card in the chain (#1038).");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  3 · The latch
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE WAY OUT IS NEVER INSIDE ANYTHING THE PEEK FADES — asked of the shipping stylesheet and the drawn
    /// tree together, which is the only pair that can answer it.
    ///
    /// <para>Three assertions, and the middle one is the anti-vacuum. (a) The rule takes a subtree out of
    /// hit-testing and not merely out of sight. (b) The rule really does reach the Nav HUD — a rule that
    /// matched nothing would sail through (c). (c) No box on the path from the map page down to the 👁 button
    /// matches it. Reverted against the old stylesheet, (a) fails first; give it back its
    /// <c>visibility: hidden</c> and (c) fails naming <c>&lt;div class="map-flowcolumn"&gt;</c> as the box the
    /// way out was buried in.</para>
    /// </summary>
    [Fact]
    public async Task TheWayOutIsNeverInsideAnythingThePeekFades()
    {
        IReadOnlyList<string> fade = ThePeekFadeSelectors(out string declarations);

        Assert.True(fade.Count > 0,
            "app.css has no `.map-peek` fade rule at all — peek hides nothing, or it has moved somewhere this "
            + "law cannot read it.");

        // (a) THE OWNER'S QUESTION, AS A LAW. `pointer-events` inherits, and .map-hud's toolbar, readouts,
        //     frame chip and plot panel each declare `pointer-events: auto` so the canvas stays grabbable
        //     between them — a descendant's own declaration beats an inherited one no matter how !important
        //     the ancestor was. Opacity 0 is not a hit-test barrier either. `visibility: hidden` is the one
        //     declaration that takes the whole subtree out of hit-testing and out of the tab order, so the
        //     rule must carry it or the invisible buttons go back to answering blind clicks.
        Assert.True(
            Regex.IsMatch(declarations, @"visibility\s*:\s*hidden"),
            "the peek fade does not declare `visibility: hidden`. Owner: \"Are the other buttons also there "
            + "even though they are invisible during the peek?\" — with opacity and pointer-events alone the "
            + "answer is yes, they are, and they take clicks. `pointer-events: none !important` on the faded "
            + "box does NOT reach the controls in it: pointer-events INHERITS, and .map-hud .btn-toolbar / "
            + ".map-readouts / .map-frame / ::deep .map-plot all declare `pointer-events: auto` themselves, "
            + "which beats an inherited value however important the ancestor's was. A control nobody can see "
            + "and everybody can press is worse than a control that is missing (#1038).");

        using DeskBench bench = await DeskBench.BootAsync(Docked);
        await bench.SwitchAsync(ShipDesk.Nav);
        bench.Poke("_peekMap", true);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node page = painted.Root.SelfAndDescendants().FirstOrDefault(n => n.HasClass("map-page"))
            ?? throw new Xunit.Sdk.XunitException("the page drew no `.map-page` root.");

        Assert.True(page.HasClass("map-peek"),
            "`_peekMap` is set and the page root is not wearing `.map-peek` — the whole mode hangs off that "
            + "class, and the CSS this law reads would never engage.");

        // (b) THE ANTI-VACUUM. The rule has to BITE, or (c) below is a sentence about nothing. The Nav HUD is
        //     the block peek exists to get out of the way, and it is on the screen at every docked boot.
        //     Asked of the whole path rather than of the HUD box alone, on purpose: "the panels go away" is
        //     true whether the rule fades the HUD itself or a wrapper around it, and this half is only here
        //     to prove the rule is not inert. Which box it lands on is (c)'s business.
        List<DeskBench.Painted.Node> hud = PathTo(page, n => n.HasClass("map-hud"))
            ?? throw new Xunit.Sdk.XunitException(
                "the page drew no `.map-hud` — this law cannot tell a rule that fades the panels from one "
                + "that fades nothing without it.");
        Assert.True(
            Enumerable.Range(0, hud.Count).Any(at => fade.Any(selector => Matches(selector, hud, at))),
            "the peek fade rule reaches nothing on the way to the Nav HUD. Peek exists to take the panels "
            + "off the sky; a rule that leaves the biggest one standing is inert, and every other assertion "
            + "in this law would then be passing on a rule that does nothing at all (#1038).");

        // (c) THE LATCH. The 👁 button, and every box between it and the page.
        List<DeskBench.Painted.Node> path = PathTo(page, IsTheWayOut)
            ?? throw new Xunit.Sdk.XunitException(
                "the desk tab bar draws no 👁 peek button. That button is the labelled way out of the mode "
                + "(the ` hotkey and Escape are the two unlabelled ones), and the owner's ask was that it "
                + "REMAIN VISIBLE — a peek with no button at all is the same trap by a shorter road (#1038).");

        List<string> faded = [];
        for (int at = 0; at < path.Count; at++)
        {
            if (fade.Any(selector => Matches(selector, path, at)))
            {
                faded.Add(Describe(path[at]));
            }
        }

        Assert.True(
            faded.Count == 0,
            "the peek fades a box the 👁 way out lives inside: " + string.Join(" → ", faded) + ".\n\n"
            + "Opacity is a GROUP: nothing inside a box at opacity 0 can paint itself back, so a button in "
            + "there is gone however exempt it believes it is. That is the owner's report — \"peek button "
            + "should remain visible\" — and it is the pop-up law (#992, owner 2026-08-24) generalised to a "
            + "MODE: there may be no state the player cannot visibly leave.\n\n"
            + "It happened because the exemption was spelled as a DEPTH (`.map-peek > *:not(.desk-tab-bar)`) "
            + "and #992 put .map-flowcolumn between the page and the bar. Mark the wrappers "
            + "`peek-passthrough` and the bar `peek-keep`, and the fade walks through the layout instead of "
            + "guessing at it (#1038).");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════════
    //  Plumbing
    // ══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The 👁 control, recognised by the eye it wears rather than by the class list around it — the
    /// class list is the thing under test and a recogniser that read it would be marking its own homework.</summary>
    private static bool IsTheWayOut(DeskBench.Painted.Node node) =>
        node.Element == "button" && node.Spoken.Contains("👁", StringComparison.Ordinal);

    private static string Describe(DeskBench.Painted.Node node) =>
        $"<{node.Element} class=\"{node.ClassList}\">";

    /// <summary>Every node from <paramref name="from"/> down to the first node that answers
    /// <paramref name="wanted"/>, inclusive — the ancestor chain a CSS combinator walks.</summary>
    private static List<DeskBench.Painted.Node>? PathTo(
        DeskBench.Painted.Node from, Func<DeskBench.Painted.Node, bool> wanted)
    {
        if (wanted(from))
        {
            return [from];
        }

        foreach (DeskBench.Painted.Node child in from.Children)
        {
            if (PathTo(child, wanted) is { } deeper)
            {
                deeper.Insert(0, from);
                return deeper;
            }
        }

        return null;
    }

    // ── The stylesheet, as shipped ───────────────────────────────────────────────────────────────────

    /// <summary>The selectors of every rule in <c>app.css</c> whose subject is the peek fade, plus the
    /// declarations they carry. Read off the file rather than restated here, because a law that restated the
    /// rule would be asserting against its own copy of it.</summary>
    private static IReadOnlyList<string> ThePeekFadeSelectors(out string declarations)
    {
        string css = WithoutComments(File.ReadAllText(
            Path.Combine(ClientSource(), "wwwroot", "css", "app.css")));

        List<string> selectors = [];
        var body = new System.Text.StringBuilder();
        foreach (Match rule in Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}"))
        {
            string head = rule.Groups[1].Value;
            if (!head.Contains(".map-peek", StringComparison.Ordinal))
            {
                continue;
            }

            selectors.AddRange(head.Split(',').Select(one => one.Trim()).Where(one => one.Length > 0));
            body.Append(rule.Groups[2].Value).Append(';');
        }

        declarations = body.ToString();
        return selectors;
    }

    // ── A very small CSS matcher ─────────────────────────────────────────────────────────────────────
    //
    // Enough of a selector engine for the grammar the peek rule is written in and no more: compound
    // selectors of `*` and `.class` with `:not(.class)` on them, joined by the descendant and child
    // combinators. It exists so this law can ask the browser's question — given THIS rule and THIS tree,
    // does the box fade — instead of the much weaker question a class-name assertion asks.

    private readonly record struct Step(char Combinator, string[] Needs, string[] Refuses);

    private static Step[] Parse(string selector)
    {
        List<Step> steps = [];
        char combinator = ' ';
        foreach (string token in Regex.Replace(selector, @"\s*>\s*", " > ").Split(' ',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (token == ">")
            {
                combinator = '>';
                continue;
            }

            string[] refuses = Regex.Matches(token, @":not\(\.([A-Za-z0-9_-]+)\)")
                .Select(m => m.Groups[1].Value).ToArray();
            string bare = Regex.Replace(token, @":not\([^)]*\)", "");
            string[] needs = Regex.Matches(bare, @"\.([A-Za-z0-9_-]+)")
                .Select(m => m.Groups[1].Value).ToArray();

            steps.Add(new Step(combinator, needs, refuses));
            combinator = ' ';
        }

        return steps.ToArray();
    }

    /// <summary>Does <paramref name="selector"/> match <c>path[at]</c>, given that <c>path</c> is that node's
    /// own ancestor chain (outermost first)?</summary>
    private static bool Matches(string selector, IReadOnlyList<DeskBench.Painted.Node> path, int at)
    {
        Step[] steps = Parse(selector);
        return steps.Length > 0 && Matches(steps, steps.Length - 1, path, at);
    }

    private static bool Matches(
        Step[] steps, int step, IReadOnlyList<DeskBench.Painted.Node> path, int at)
    {
        if (step < 0 || at < 0)
        {
            return false;
        }

        DeskBench.Painted.Node node = path[at];
        if (steps[step].Needs.Any(cls => !node.HasClass(cls))
            || steps[step].Refuses.Any(node.HasClass))
        {
            return false;
        }

        if (step == 0)
        {
            return true;
        }

        if (steps[step].Combinator == '>')
        {
            return Matches(steps, step - 1, path, at - 1);
        }

        for (int up = at - 1; up >= 0; up--)
        {
            if (Matches(steps, step - 1, path, up))
            {
                return true;
            }
        }

        return false;
    }

    // ── Shared plumbing, spelled the way the neighbouring laws spell it ──────────────────────────────

    /// <summary>
    /// TYPE ESCAPE AT THE PAGE, through the handler the render tree wrote for <c>.map-page</c>.
    ///
    /// <para>The arming poke is the same one the cancel-chain law explains: <c>OnKeyDown</c>'s first act is
    /// #338's gesture unlock, a <c>[JSImport]</c> that reaches for a browser this bench has not got, so the
    /// FIRST key of a session is swallowed by that documented gate. A law about which mode a key reaches must
    /// not quietly be measuring one press behind.</para>
    /// </summary>
    private static async Task PressEscape(DeskBench bench)
    {
        bench.Poke("_audioArmed", true);
        ulong keyboard = DeskBench.TheKeyboard(await bench.RenderAsync());
        Assert.True(keyboard != 0,
            "the page draws no `.map-page` element with an onkeydown handler on it — the div every key in "
            + "this game lands on (Map.razor:14). A page without one has gone deaf.");

        await bench.TypeAsync(keyboard, "Escape");
    }

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
            "src/SpaceSails.Client is not above the test binary — this guard reads the stylesheet as shipped "
            + "and cannot do its job without it.");
    }
}
