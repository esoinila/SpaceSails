using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Components;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 8 · <b>THE EIGHT PIXELS, AND THE ROW THE PAGE DREW.</b>
///
/// <para><b>The eight pixels.</b> The shell's chrome rule gave every way out <c>line-height: 1</c>. That is
/// exactly right for the bare ✕ glyph it was written for and eight pixels too short for a Bootstrap
/// <c>.btn</c> with words on it — a worded button's box is its line-height plus its padding, so overwriting
/// <c>--bs-btn-line-height</c> shrinks it. #1008 measured it in a browser (<i>…wake up</i> 38 px → 30 px,
/// the shelf 36 → 28, <i>Close hatch</i> 31 → 25), restored the two families wave 7 moved, and reported the
/// rest rather than fixing it under a refactor. Nineteen <c>.btn</c>-dressed dismisses across waves 1–6 were
/// wearing it quietly. Fable's ruling for wave 8: <b>this is a fidelity FIX, not a feel change</b> — the law
/// that forbids visible changes under cover of a refactor is the law that requires this one, because what
/// the captain gets back is the button he had before the shell touched it.</para>
///
/// <para>So the shell's own stylesheet excludes <c>.btn</c> from that one declaration, and the two local
/// restorations #1008 wrote are gone — one general rule instead of a list that the next wave would have had
/// to extend. This file's first guard is what keeps it: <b>no rule in the shell's stylesheet may set
/// line-height on an element that also wears <c>.btn</c></b>, read out of the file itself.</para>
///
/// <para><b>The row the page drew.</b> <c>WaysClass</c> (wave 7) taught the shell to draw its ways out into
/// a row the page NAMES. The logbook needed the other half: its <i>Close</i> shares
/// <c>.save-surface-foot</c> with <i>⬇ Export this moment</i> and <i>⬆ Import file</i>, which are not ways
/// out, and that row is drawn only once the reactor is warm. A shell that drew the row would have to know
/// both facts. <c>FootHost</c> hands the row the way out instead, and the logbook — the last half of wave
/// 6's joint straggler — is the shell's.</para>
///
/// <para><b>And the two surfaces that did NOT take <see cref="CappedScrollPanel"/>, with the reason pinned
/// rather than asserted in prose.</b> #1008 named <c>.map-readouts</c> and <c>.desk-content</c> as the
/// second consumer. Neither is one: the component is <i>a head that keeps its measured height and a body
/// that takes what is left and scrolls there</i>, and both of these are all body. The reason is a fact
/// about the markup, so it is read off the markup.</para>
/// </summary>
public sealed class TheWayOutKeepsItsOwnHeightAndTheLogbookTakesTheShellTests
{
    // ── 1 · The eight pixels ──────────────────────────────────────────────────────────────────────────

    /// <summary>The three classes the shell puts on a way out. Any of them may be dressed as a Bootstrap
    /// button by the page, and by wave 7 all three are.</summary>
    private static readonly string[] TheWaysOut =
        ["overlay-shell-dismiss", "overlay-shell-close", "overlay-shell-beside"];

    /// <summary>
    /// THE SHELL'S STYLESHEET NEVER SETS <c>line-height</c> ON A BUTTON.
    ///
    /// <para>Stated as the law rather than as the fix: it is not "the selector says <c>:not(.btn)</c>" but
    /// "no rule here can reach a <c>.btn</c> with a line-height in it". A rule reaches one unless it says
    /// otherwise, so every line-height rule whose target names a way out has to carry the exclusion — which
    /// is what makes this guard survive the selector being rewritten and still fail if the exclusion is
    /// dropped.</para>
    ///
    /// <para><b>And the exclusion must weigh nothing.</b> <c>:not(.btn)</c> contributes the specificity of
    /// its argument — one whole class — so the obvious form would have compiled to a rule HEAVIER than
    /// <c>::deep .thing</c> in Map.razor.css, in a file whose one stated law is that nothing in it may win
    /// a tie. <c>:where(:not(.btn))</c> matches the same elements and weighs nothing. Both halves are asked
    /// for here, because the second one failing is the first one's fix quietly becoming a new bug.</para>
    /// </summary>
    [Fact]
    public void TheShellsStylesheetNeverSetsLineHeightOnAButton()
    {
        var offences = new List<string>();
        var guarded = 0;

        foreach ((string selector, string body) in RulesOf(TheShellsStylesheet()))
        {
            if (!Regex.IsMatch(body, @"(^|[;\s])line-height\s*:"))
            {
                continue;
            }

            if (!TheWaysOut.Any(way => selector.Contains(way, StringComparison.Ordinal)))
            {
                continue;
            }

            if (!selector.Contains(":not(.btn)", StringComparison.Ordinal))
            {
                offences.Add(
                    $"`{selector}` sets a line-height and does not exclude .btn — every worded way out it "
                    + "reaches loses the height Bootstrap gave it (roughly eight pixels on a btn, six on a "
                    + "btn-sm), which is #1008's finding coming back");
                continue;
            }

            if (!selector.Contains(":where(:not(.btn))", StringComparison.Ordinal))
            {
                offences.Add(
                    $"`{selector}` excludes .btn with a bare `:not(.btn)`, which contributes a whole class "
                    + "of specificity. Every rule in this file is deliberately weightless so the page wins "
                    + "in any bundle order; write `:where(:not(.btn))`, which matches the same elements "
                    + "and weighs nothing");
                continue;
            }

            guarded++;
        }

        Assert.True(offences.Count == 0,
            $"{offences.Count} rule(s) in OverlayShell.razor.css put a line-height on a way out that may be "
            + $"a Bootstrap button:\n  - {string.Join("\n  - ", offences)}\n\n#1008 measured what that costs "
            + "in a real browser: `…wake up` 38 px → 30 px, the shelf 36 → 28, `Close hatch` 31 → 25. A "
            + "refactor is not allowed to move a control, and this is the rule that stops it.");

        // A guard that scanned nothing would be green forever. There IS a line-height rule in this file and
        // it IS the guarded one — if somebody deletes it outright, that is a different change and this says
        // so rather than passing in silence.
        Assert.True(guarded == 1,
            $"{guarded} guarded line-height rule(s) in OverlayShell.razor.css, expected exactly one. The ✕ "
            + "glyph still wants `line-height: 1` — it is what keeps a bare glyph's box the size of the "
            + "glyph. If the rule has gone or grown a sibling, this guard is looking at a file it no longer "
            + "understands.");
    }

    /// <summary>
    /// …AND #1008'S TWO LOCAL RESTORATIONS ARE GONE, because the general rule covers them.
    ///
    /// <para>Wave 7 gave <c>.busted-close</c>, <c>.busted-logbook</c> and the hatch's dismiss their own
    /// <c>line-height: var(--bs-btn-line-height)</c> in Map.razor.css — the right fix for two families and
    /// the wrong shape for nineteen. Leaving them beside the general rule would be two rules saying the
    /// same thing, one of which nobody would ever think to delete.</para>
    /// </summary>
    [Fact]
    public void ThePageNoLongerHandsAnyWayOutItsButtonHeightBack()
    {
        string css = File.ReadAllText(Path.Combine(ClientSource(), "Pages", "Map.razor.css"));

        var restorations = RulesOf(css)
            .Where(rule => rule.Body.Contains("--bs-btn-line-height", StringComparison.Ordinal))
            .Select(rule => rule.Selector)
            .ToList();

        Assert.True(restorations.Count == 0,
            $"{restorations.Count} rule(s) in Map.razor.css hand a way out its Bootstrap line-height back:"
            + $"\n  - {string.Join("\n  - ", restorations)}\n\nThe shell's own stylesheet stopped taking it "
            + "away (#997 wave 8), so these restore something nothing removed. Two rules saying the same "
            + "thing is how one of them survives the day the other is wrong.");
    }

    // ── 2 · The row the page drew ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A PAGE-DRAWN FOOT REALLY DOES GET THE SHELL'S WAY OUT, AND THE SHELL ADDS NOTHING AROUND IT.
    ///
    /// <para>The mechanism in one render: the page's own element is the card's last child, the way out is
    /// INSIDE it beside whatever else the page put there, and no <c>.overlay-shell-ways</c> appears
    /// anywhere — because the row already exists and a second one would be the wrapper wave 7 measured the
    /// cost of. Pressed, too: a way out that is drawn somewhere else and wired to nothing is the exact
    /// quiet failure this lane is about.</para>
    /// </summary>
    [Fact]
    public async Task AWayOutHandedToAPageDrawnFootStandsInItAndIsWiredToTheVerb()
    {
        var closed = 0;
        RenderFragment<RenderFragment> foot = wayOut => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "test-plate-foot");
            builder.OpenElement(2, "button");
            builder.AddAttribute(3, "type", "button");
            builder.AddContent(4, "⬇ Export this moment");
            builder.CloseElement();
            builder.AddContent(5, wayOut);
            builder.CloseElement();
        };

        using ShellBench bench = ShellBench.Mount(Shell(
            ("class", "test-plate"), ("Frame", OverlayFrame.Bare), ("Dismiss", OverlayDismiss.Close),
            ("DismissFace", "Close"), ("DismissClass", "btn btn-sm btn-outline-info"),
            ("FootHost", foot),
            ("OnClose", EventCallback.Factory.Create(new object(), () => closed++))));

        DeskBench.Painted.Node card = ShellBench.Wearing(await bench.RenderAsync(), "test-plate")!;

        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-ways"));

        DeskBench.Painted.Node row = card.Children[^1];
        Assert.True(row.HasClass("test-plate-foot"),
            "the page's own foot is not the card's last child. FootHost draws the page's fragment where a "
            + "lone dismiss would have gone, so whatever the page wrapped its row in is what lands there.");

        Assert.True(row.Children.Count == 2,
            $"the page's foot came out with {row.Children.Count} control(s). It drew one of its own and "
            + "was handed one way out, so a row of two is the whole claim.");

        DeskBench.Painted.Node close = row.Children[^1];
        Assert.True(close.HasClass("overlay-shell-dismiss") && close.HasClass("btn-outline-info"),
            "the last thing in the page's row is not the shell's way out wearing the page's own dress.");

        await bench.PressAsync(close.Handlers["onclick"]);
        Assert.True(closed == 1,
            "the way out standing in the page's foot is not wired to the page's verb. A control that looks "
            + "like a way out and is not one is #992's own bug, one row further in.");
    }

    /// <summary>
    /// THE FOOT-HOST AUDIT FIRES, and it is proved by watching the sink rather than by trusting it would.
    ///
    /// <para>Three shapes, each of them the mechanism failing quietly. A foot host on a frame that is not
    /// <c>Bare</c> draws the way out somewhere the page did not put it (a <c>Card</c> keeps its dismiss in
    /// the head row; a <c>Hosted</c> shell has no box of its own at all). A foot host beside a
    /// <c>WaysClass</c> is two answers to one question, and the shell can only obey one — so the row the
    /// page named is silently never drawn. And a foot host on a <c>ByDecision</c> with nothing wired beside
    /// it is a row drawn one control short, because a decision surface has no dismiss to hand anybody.</para>
    /// </summary>
    [Theory]
    [InlineData("a Card that hands its way out to a page-drawn foot", "frame")]
    [InlineData("a foot host beside a named ways row", "both")]
    [InlineData("a decision surface with a foot and no way out to put in it", "empty")]
    public async Task AFootHostWithNothingToHostOrNowhereToSitIsReported(string name, string fault)
    {
        var complaints = new List<string>();
        Action<string> was = OverlayShell.DesignFault;
        OverlayShell.DesignFault = complaints.Add;

        RenderFragment<RenderFragment> foot = wayOut => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, wayOut);
            builder.CloseElement();
        };

        try
        {
            var parameters = new List<(string, object?)>
            {
                ("class", "test-plate"),
                ("Title", "the sheet"),
                ("FootHost", foot),
                ("Frame", fault == "frame" ? OverlayFrame.Card : OverlayFrame.Bare),
                ("DismissFace", "Close"),
            };

            if (fault == "both")
            {
                parameters.Add(("WaysClass", "test-plate-foot"));
            }

            if (fault == "empty")
            {
                parameters.Add(("Dismiss", OverlayDismiss.ByDecision));
                parameters.Add(("Choices", (IReadOnlyList<OverlayShell.Choice>)
                    [new OverlayShell.Choice("Replace now",
                         EventCallback.Factory.Create(new object(), () => { }))]));
            }
            else
            {
                parameters.Add(("OnClose", EventCallback.Factory.Create(new object(), () => { })));
            }

            using ShellBench bench = ShellBench.Mount(Shell([.. parameters]));
            await bench.RenderAsync();

            Assert.True(complaints.Count > 0,
                $"{name}: the shape audit said nothing. FootHost is the third way a page can tell the shell "
                + "where its way out goes, and an unaudited parameter is one whose misuse is invisible "
                + "until somebody looks at the screen — which is what every guard in this lane replaces.");
            Assert.Contains("the sheet", complaints[0], StringComparison.Ordinal);
        }
        finally
        {
            OverlayShell.DesignFault = was;
        }
    }

    // ── 3 · The two that did not take the capped scroll ───────────────────────────────────────────────

    /// <summary>
    /// THE READOUTS BLOCK AND THE DESK CONTENT ARE NAMED STRAGGLERS, AND THE REASON IS A FACT ABOUT THEM.
    ///
    /// <para><see cref="CappedScrollPanel"/> is one shape: a HEAD that keeps its measured height and ONE
    /// body that takes what is left and scrolls there. #1008 listed both of these as candidates for the
    /// second consumer; neither of them has a head.</para>
    ///
    /// <para><b>The readouts</b> are one block of read-at-a-glance lines that scroll TOGETHER — the
    /// scenario, the sim clock, warp, zoom, the ship's speed, the hold, whatever the nav target is doing.
    /// Nothing in it is pinned above the scroll and nothing is supposed to be. Putting it in the panel with
    /// an empty head would add a wrapper div and a <c>display: flex</c> to buy exactly nothing: the floor
    /// and the scroll it already has are the page's own <c>.map-hud .map-readouts</c> rule, which is
    /// heavier than anything the component brings and would go on winning. <i>Which line of it should stop
    /// scrolling away</i> is a real question and a good one — it is a DESIGN question for the owner, not
    /// something to change under a refactor that promised to move nothing.</para>
    ///
    /// <para><b>The desk content</b> is not a flex column at all: it is a fixed-height scrollport
    /// (<c>height: 90vh</c>, or the layer's <c>calc(100vh - clearance - 2rem)</c>) holding exactly one
    /// child — a whole desk that is <c>height: 100%</c> inside it. There is no remainder for a body to
    /// take, because there is no head taking anything first.</para>
    ///
    /// <para>Both facts are asserted rather than written down, so the day either surface grows a head this
    /// fails and says the straggler is ready.</para>
    /// </summary>
    [Fact]
    public void TheTwoCappedScrollCandidatesAreNamedStragglersAndTheirReasonIsPinned()
    {
        string css = WithoutComments(File.ReadAllText(Path.Combine(ClientSource(), "Pages", "Map.razor.css")));
        string razor = MapMarkup.Read(Path.Combine(ClientSource(), "Pages", "Map.razor"));

        // The readouts: the page's own rule owns the shrink, the floor and the scroll — one block, one
        // scroller, no head. If the panel is ever going to help here, this rule is what has to change first.
        string readouts = TheRuleFor(css, ".map-hud .map-readouts")
            ?? throw new Xunit.Sdk.XunitException(
                "`.map-hud .map-readouts` is gone from Map.razor.css. The straggler's reason hangs off that "
                + "rule — say what the readouts do now.");

        foreach (string owns in new[] { "flex: 0 8 auto", "min-height: 7rem", "overflow-y: auto" })
        {
            Assert.True(readouts.Contains(owns, StringComparison.Ordinal),
                $"the readouts block no longer says `{owns}`. #994 measured that block as the SECOND thing "
                + "on the Nav column allowed to give (the Plotting panel is the first), and the whole "
                + "reason it is not a CappedScrollPanel is that the shrink, the floor and the scroll are "
                + "one rule on one element with nothing pinned above it. If that has changed, the panel is "
                + "worth another look.");
        }

        Assert.False(razor.Contains("<CappedScrollPanel class=\"map-readouts", StringComparison.Ordinal),
            "the readouts took the panel. Good — then this straggler is no longer one: say which line of "
            + "them is the head that stops scrolling away, and take this guard off the list.");

        // The desk content: a fixed-height scrollport around one full-height child, not a head and a body.
        string deskContent = TheRuleFor(css, ".desk-content")
            ?? throw new Xunit.Sdk.XunitException("`.desk-content` is gone from Map.razor.css.");

        Assert.True(deskContent.Contains("height: 90vh", StringComparison.Ordinal)
                    && deskContent.Contains("overflow-y: auto", StringComparison.Ordinal),
            "`.desk-content` is no longer a fixed-height scrollport. That is the whole reason it cannot be "
            + "a CappedScrollPanel: the panel measures a remainder with flex, and this box has no head to "
            + "measure against — it is a window-sized frame around one desk that fills it.");

        Assert.False(razor.Contains("<CappedScrollPanel class=\"desk-content", StringComparison.Ordinal),
            "the desk content took the panel. Then say what its head is — every desk inside it is "
            + "`height: 100%`, and a panel with an empty head is a wrapper div with a name.");
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The shell's own stylesheet, read as typed. The whole point of the first guard is that the
    /// FILE is the law — a component under test could not tell us what its CSS says.</summary>
    private static string TheShellsStylesheet() =>
        File.ReadAllText(Path.Combine(ClientSource(), "Components", "OverlayShell.razor.css"));

    /// <summary>Every rule in a stylesheet as (selector, body), comments removed first so a brace or a
    /// declaration quoted in somebody's prose is not read as CSS — the idiom EveryCssRuleIsClosedTests
    /// established for this tree.</summary>
    private static IEnumerable<(string Selector, string Body)> RulesOf(string css)
    {
        foreach (Match rule in Regex.Matches(WithoutComments(css), @"([^{}]+)\{([^{}]*)\}"))
        {
            yield return (Flat(rule.Groups[1].Value), rule.Groups[2].Value);
        }
    }

    /// <summary>The body of the first rule whose selector list names <paramref name="selector"/> exactly.
    /// </summary>
    private static string? TheRuleFor(string css, string selector) =>
        RulesOf(css)
            .Where(rule => rule.Selector.Split(',').Any(one => Flat(one) == selector))
            .Select(rule => rule.Body)
            .FirstOrDefault();

    private static string WithoutComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", " ", RegexOptions.Singleline);

    private static string Flat(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>One <c>&lt;OverlayShell&gt;</c> with the parameters named — wave 7's own idiom, so a
    /// component test and the page ask the shell the same way.</summary>
    private static RenderFragment Shell(params (string Name, object? Value)[] parameters) => builder =>
    {
        builder.OpenComponent<OverlayShell>(0);
        int seq = 1;
        foreach ((string name, object? value) in parameters)
        {
            builder.AddComponentParameter(seq++, name, value);
        }

        builder.CloseComponent();
    };

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
            "src/SpaceSails.Client is not above the test binary — these guards read the markup and the "
            + "stylesheets as typed and cannot do their job without them.");
    }
}
