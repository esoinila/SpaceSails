using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SpaceSails.Client.Components;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 · <b>THE OVERLAY SHELL IS ONE MECHANISM.</b>
///
/// <para>#992's audit found the minimise-into-a-tile gesture implemented TWICE — #963's scope and #960's
/// dossier — with no shared field, no shared markup and no shared CSS between them. Two implementations of
/// one idea drift, and the drift is invisible until somebody fixes a bug in one of them. So the gesture is
/// a component now, and this file asks the component the questions the two hand-rolled versions were only
/// ever asked by eye.</para>
///
/// <para><b>Everything here presses.</b> The bench dispatches at the handler id the render tree wrote,
/// through the renderer's own event channel — the click a player's mouse makes. A test that called
/// <c>PressTheDismiss</c> by reflection would prove the shell has a method with that name and nothing at
/// all about whether the ✕ is wired to it, which is the exact shape of bug #992 exists to catch.</para>
///
/// <para><b>What is NOT asserted here, said out loud:</b> pixels. There is no layout in a render tree.
/// Where a stacked shell actually lands is <c>SpaceSails.UiGate</c>'s question and the browser walk's; what
/// this file can prove is that the two shells are given DIFFERENT geometry to land in, which is the half of
/// the stacking rule that a markup mistake can break.</para>
/// </summary>
public sealed class TheOverlayShellIsOneMechanismTests
{
    // ── The three ways out ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ACloseShellDrawsAWayOutAndPressingItTakesTheSurfaceDown()
    {
        bool closed = false;
        using ShellBench bench = ShellBench.Mount(Shell(
            ("class", "test-plate"),
            ("Frame", OverlayFrame.Bare),
            ("Dismiss", OverlayDismiss.Close),
            ("DismissFace", "✕"),
            ("DismissClass", "test-plate-close"),
            ("OnClose", EventCallback.Factory.Create(new object(), () => closed = true))));

        DeskBench.Painted painted = await bench.RenderAsync();

        // The page's own class survives the shell: #992's completeness guard reads `class="…"` out of the
        // markup as typed, and a surface that renamed itself through a parameter would vanish from it.
        Assert.NotNull(ShellBench.Wearing(painted, "test-plate"));

        DeskBench.Painted.Node? way = ShellBench.Control(painted, "✕");
        Assert.True(way is not null, "a Close shell drew no ✕ at all — the shape the owner's ruling forbids.");
        Assert.True(way!.HasClass("test-plate-close"),
            "the page named a class for its ✕ and the shell did not put it on: the page's own CSS for that "
            + "button reaches nothing.");

        await bench.PressAsync(way.Handlers["onclick"]);
        Assert.True(closed, "the ✕ is drawn and pressing it reaches nothing. That is a control that LOOKS "
                            + "like a way out and is not one.");
        Assert.Empty(bench.Escaped);
    }

    [Fact]
    public async Task AMinimizeShellDrawsATileAndNoCrossUnlessTheSurfaceAlsoAsksForOne()
    {
        using ShellBench plain = ShellBench.Mount(Scope());
        DeskBench.Painted painted = await plain.RenderAsync();

        Assert.NotNull(ShellBench.Control(painted, "–"));
        Assert.Null(ShellBench.Control(painted, "✕"));

        // …and the dossier's shape: the same shell, plus a close, because tucking and closing are not the
        // same verb — one keeps the target and the other drops it.
        bool closed = false;
        using ShellBench both = ShellBench.Mount(Shell(
            ("class", "test-dossier"),
            ("Dismiss", OverlayDismiss.Minimize),
            ("TileGlyph", "📖"), ("TileLabel", "Victoria I"), ("TileClass", "test-dossier-tile"),
            ("DismissFace", "–"),
            ("CloseFace", "✕"),
            ("OnClose", EventCallback.Factory.Create(new object(), () => closed = true))));

        DeskBench.Painted two = await both.RenderAsync();
        Assert.NotNull(ShellBench.Control(two, "–"));
        DeskBench.Painted.Node cross = ShellBench.Control(two, "✕")!;

        await both.PressAsync(cross.Handlers["onclick"]);
        Assert.True(closed, "the dossier's ✕ is not the same verb as its – and must reach the page's own "
                            + "close. It reached nothing.");
    }

    [Fact]
    public async Task AByDecisionShellDrawsNoCrossAndEveryAnswerItOffers()
    {
        var taken = new List<string>();
        using ShellBench bench = ShellBench.Mount(Shell(
            ("class", "test-demand"),
            ("Dismiss", OverlayDismiss.ByDecision),
            ("Restages", true),
            ("Choices", (IReadOnlyList<OverlayShell.Choice>)
            [
                new OverlayShell.Choice("Pay the fine",
                    EventCallback.Factory.Create(new object(), () => taken.Add("pay"))),
                new OverlayShell.Choice("Run for it",
                    EventCallback.Factory.Create(new object(), () => taken.Add("run"))),
            ])));

        DeskBench.Painted painted = await bench.RenderAsync();

        Assert.Null(ShellBench.Control(painted, "✕"));
        Assert.NotNull(ShellBench.Control(painted, "Pay the fine"));
        Assert.NotNull(ShellBench.Control(painted, "Run for it"));

        await bench.PressAsync(ShellBench.Control(painted, "Run for it")!.Handlers["onclick"]);
        Assert.Equal(["run"], taken);
    }

    // ── The tile round-trip ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// MINIMISE, TILE, RESTORE — AND WHAT WAS IN THE WINDOW IS STILL IN IT.
    ///
    /// <para>This is the test the M26 fix would have wanted. #963's scope minimises by CLASS rather than by
    /// <c>@@if</c> because destroying the surface destroys the canvas inside it and leaves renderer.js
    /// holding a stale 2D context — the scope then goes permanently dark after any desk switch. The shell
    /// inherited that rule, and the way to prove it holds is not to look at the markup but to put something
    /// with a LIFE inside the shell and check it was never born twice.</para>
    /// </summary>
    [Fact]
    public async Task TheTileRoundTripKeepsWhatWasInTheWindow()
    {
        int[] born = [0];
        using ShellBench bench = ShellBench.Mount(Shell(
            ("class", "test-scope"),
            ("Dismiss", OverlayDismiss.Minimize),
            ("TileGlyph", "🔭"), ("TileLabel", "Ganymede"), ("TileClass", "test-scope-tile"),
            ("DismissFace", "–"),
            ("ChildContent", (RenderFragment)(builder =>
            {
                builder.OpenComponent<Keepsake>(0);
                builder.AddComponentParameter(1, nameof(Keepsake.Born), born);
                builder.CloseComponent();
            }))));

        DeskBench.Painted open = await bench.RenderAsync();
        Assert.NotNull(ShellBench.Wearing(open, "test-scope"));
        Assert.Null(ShellBench.Wearing(open, "test-scope-tile"));
        Assert.False(ShellBench.Wearing(open, "overlay-shell-body")!.Hidden);
        Assert.Equal(1, born[0]);

        await bench.PressAsync(ShellBench.Control(open, "–")!.Handlers["onclick"]);
        DeskBench.Painted tucked = await bench.RenderAsync();

        // Tucked: the tile is up, the surface wears the page's TILE name and not its open one…
        Assert.NotNull(ShellBench.Wearing(tucked, "test-scope-tile"));
        Assert.Null(ShellBench.Wearing(tucked, "test-scope"));

        // …and the body is still IN THE TREE, merely off the screen. This is the assertion that would fail
        // if somebody "tidied" the d-none into an @if, and the canvas would go dark a week later.
        DeskBench.Painted.Node body = ShellBench.Wearing(tucked, "overlay-shell-body")!;
        Assert.True(body.Hidden, "a tucked shell must HIDE its body, not drop it.");
        Assert.Equal(1, born[0]);

        DeskBench.Painted.Node tile = ShellBench.Control(tucked, "🔭 Ganymede")!;
        await bench.PressAsync(tile.Handlers["onclick"]);
        DeskBench.Painted back = await bench.RenderAsync();

        Assert.NotNull(ShellBench.Wearing(back, "test-scope"));
        Assert.False(ShellBench.Wearing(back, "overlay-shell-body")!.Hidden);
        Assert.True(born[0] == 1,
            $"what was in the window was born {born[0]} times across one minimise and one restore. The "
            + "shell destroyed and rebuilt its own contents, which is the M26 bug: a recreated canvas "
            + "leaves renderer.js holding a stale 2D context and the scope goes permanently dark.");
    }

    /// <summary>A shell that is hidden — off this desk — is hidden in BOTH states. A <c>d-none</c> written
    /// into the page's class attribute would be lost the moment the shell swapped to its tile name, and the
    /// tile would follow the captain onto every other desk.</summary>
    [Fact]
    public async Task AHiddenShellIsHiddenTuckedAsWellAsOpen()
    {
        using ShellBench bench = ShellBench.Mount(Shell(
            ("class", "test-scope"),
            ("Hidden", true),
            ("Dismiss", OverlayDismiss.Minimize),
            ("TileGlyph", "🔭"), ("TileLabel", "Ganymede"), ("TileClass", "test-scope-tile"),
            ("DismissFace", "–")));

        DeskBench.Painted open = await bench.RenderAsync();
        Assert.True(ShellBench.Wearing(open, "test-scope")!.Hidden);

        await bench.PressAsync(ShellBench.Control(open, "–")!.Handlers["onclick"]);
        DeskBench.Painted tucked = await bench.RenderAsync();

        Assert.True(ShellBench.Wearing(tucked, "test-scope-tile")!.Hidden,
            "the shell tucked itself onto a desk it is not on: the tile is on the screen where the window "
            + "was not.");
    }

    // ── Stacking ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TWO SHELLS AT ONE ANCHOR TAKE TURNS.
    ///
    /// <para>#960's sighting: the dossier lying across the navigation-target panel's text and half its
    /// buttons. The answer was never a z-index — the card underneath is one the captain is reading too — so
    /// a shell that declares itself stacked must be given DIFFERENT geometry from the one on the floor,
    /// which off-browser means a different class list and the lift rule to go with it.</para>
    /// </summary>
    [Fact]
    public async Task TwoShellsAtOneAnchorAreNotGivenTheSameBox()
    {
        using ShellBench bench = ShellBench.Mount(builder =>
        {
            builder.AddContent(0, Shell(("class", "test-dossier"), ("Stacked", false), ("Dismiss", OverlayDismiss.Close),
                ("DismissFace", "a"), ("OnClose", EventCallback.Factory.Create(new object(), () => { }))));
            builder.AddContent(1, Shell(("class", "test-dossier"), ("Stacked", true),
                ("StackedClass", "test-dossier-raised"), ("Dismiss", OverlayDismiss.Close),
                ("DismissFace", "b"), ("OnClose", EventCallback.Factory.Create(new object(), () => { }))));
        });

        DeskBench.Painted painted = await bench.RenderAsync();
        List<DeskBench.Painted.Node> both = painted.Root.Descendants()
            .Where(node => node.HasClass("test-dossier")).ToList();

        Assert.Equal(2, both.Count);
        Assert.False(both[0].HasClass("overlay-shell-stacked"));
        Assert.False(both[0].HasClass("test-dossier-raised"));
        Assert.True(both[1].HasClass("overlay-shell-stacked"),
            "the second shell at this anchor declared itself stacked and was drawn wearing exactly what the "
            + "first one wears. Two cards on one spot is #960's own screenshot.");
        Assert.True(both[1].HasClass("test-dossier-raised"),
            "the page named its own measured lift and the shell did not put it on, so the card rides on the "
            + "component's unmeasured default instead of the number somebody measured.");

        // And the rule that does the lifting is in the component's OWN stylesheet — the mechanism travels
        // with the mechanism. Read rather than assumed: a class with no rule behind it is #996's whole
        // lesson about markup and CSS that quietly stopped agreeing.
        string css = ShellCss();
        Assert.Contains(".overlay-shell-stacked", css);
        Assert.Contains("--overlay-shell-lift", css);
    }

    // ── The design audit ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A DECISION THAT DOES NOT END THE SURFACE IS REPORTED.
    ///
    /// <para>The critical-decision exception is a CLAIM: "no ✕ is needed, because every answer is itself a
    /// close." This repository has been burned by guards that took such a claim from a list, so the shell
    /// does not take it from a parameter either — it presses on, and if it is still being drawn after an
    /// answer was taken, it says so. Proved here by taking an answer that does nothing at all, which is the
    /// forbidden shape wearing the exception's clothes.</para>
    /// </summary>
    [Fact]
    public async Task AnAnswerThatLeavesTheModalUpIsReported()
    {
        var complaints = new List<string>();
        Action<string> was = OverlayShell.DesignFault;
        OverlayShell.DesignFault = complaints.Add;

        try
        {
            // The page answers the way a page does — it re-renders — and simply keeps the card up, which
            // is the whole of the fault. Written as the mirror image of the test below it: same shell,
            // same press, same re-render, and the ONE difference is whether the surface goes.
            ShellBench? host = null;
            host = ShellBench.Mount(Shell(
                ("class", "test-demand"),
                ("Dismiss", OverlayDismiss.ByDecision),
                ("Choices", (IReadOnlyList<OverlayShell.Choice>)
                [
                    new OverlayShell.Choice("Nod along",
                        EventCallback.Factory.Create(new object(), () => host!.Redraw())),
                ])));

            using ShellBench bench = host;
            DeskBench.Painted painted = await bench.RenderAsync();
            Assert.Empty(complaints);

            await bench.PressAsync(ShellBench.Control(painted, "Nod along")!.Handlers["onclick"]);
            await bench.RenderAsync();

            Assert.True(complaints.Count > 0,
                "an answer was taken on a modal that carries no ✕ — allowed only because every answer is "
                + "supposed to BE the close — and the surface was drawn again afterwards with nothing said.");
            Assert.Contains("still on the screen", complaints[0], StringComparison.Ordinal);
        }
        finally
        {
            OverlayShell.DesignFault = was;
        }
    }

    /// <summary>…and the same audit says nothing when the answer really does end it. Without this half, the
    /// one above would pass on an audit that complained about everything.</summary>
    [Fact]
    public async Task AnAnswerThatEndsTheModalIsNotReported()
    {
        var complaints = new List<string>();
        Action<string> was = OverlayShell.DesignFault;
        OverlayShell.DesignFault = complaints.Add;

        try
        {
            bool up = true;
            ShellBench? host = null;
            host = ShellBench.Mount(builder =>
            {
                if (!up)
                {
                    return;
                }

                builder.AddContent(0, Shell(
                    ("class", "test-demand"),
                    ("Dismiss", OverlayDismiss.ByDecision),
                    ("Choices", (IReadOnlyList<OverlayShell.Choice>)
                    [
                        new OverlayShell.Choice("Pay the fine", EventCallback.Factory.Create(
                            new object(),
                            () =>
                            {
                                // A page's own answer: drop the surface, re-render. The re-render is
                                // queued from INSIDE the handler, which is where Blazor puts a page's
                                // StateHasChanged too — so the card is disposed before its own paint.
                                up = false;
                                host!.Redraw();
                            })),
                    ])));
            });

            using ShellBench bench = host;
            DeskBench.Painted painted = await bench.RenderAsync();
            await bench.PressAsync(ShellBench.Control(painted, "Pay the fine")!.Handlers["onclick"]);
            await bench.RenderAsync();

            Assert.True(complaints.Count == 0,
                "the answer took the surface off the screen and the audit complained anyway: "
                + string.Join(" · ", complaints));
        }
        finally
        {
            OverlayShell.DesignFault = was;
        }
    }

    /// <summary>The shape audit, on the one fault that is not about behaviour: a ✕ with nothing behind it.
    /// It fires at PARAMETER time, before anybody has pressed anything, which is the point — the bug is
    /// visible in the markup and should not have to wait for a player to find it.</summary>
    [Fact]
    public async Task ACrossWithNothingBehindItIsReported()
    {
        var complaints = new List<string>();
        Action<string> was = OverlayShell.DesignFault;
        OverlayShell.DesignFault = complaints.Add;

        try
        {
            using ShellBench bench = ShellBench.Mount(Shell(
                ("class", "test-plate"), ("Title", "the long walk in"),
                ("Dismiss", OverlayDismiss.Close), ("DismissFace", "✕")));

            await bench.RenderAsync();

            Assert.True(complaints.Count > 0, "a Close shell was built with no OnClose and nothing said.");
            Assert.Contains("the long walk in", complaints[0], StringComparison.Ordinal);
        }
        finally
        {
            OverlayShell.DesignFault = was;
        }
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

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

    private static RenderFragment Scope() => Shell(
        ("class", "test-scope"),
        ("Dismiss", OverlayDismiss.Minimize),
        ("TileGlyph", "🔭"), ("TileLabel", "Ganymede"), ("TileClass", "test-scope-tile"),
        ("DismissFace", "–"));

    internal static string ShellCss() =>
        File.ReadAllText(Path.Combine(ClientSource(), "Components", "OverlayShell.razor.css"));

    internal static string PanelCss() =>
        File.ReadAllText(Path.Combine(ClientSource(), "Components", "CappedScrollPanel.razor.css"));

    internal static string ClientSource()
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

        throw new DirectoryNotFoundException("src/SpaceSails.Client is not above the test binary.");
    }

    /// <summary>Something with a LIFE, to be put inside a shell and counted. It reports how many times it
    /// was BUILT, so a shell that destroys its own contents on the way to a tile is caught by arithmetic
    /// rather than by reading the markup and hoping.</summary>
    private sealed class Keepsake : ComponentBase
    {
        [Parameter] public int[] Born { get; set; } = [0];

        protected override void OnInitialized() => Born[0]++;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "canvas");
            builder.AddAttribute(1, "class", "test-keepsake");
            builder.CloseElement();
        }
    }
}
