using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Components;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 · <b>THE CAPPED SCROLL PANEL HOLDS ITS HEAD.</b>
///
/// <para>The component is #993's arithmetic with a name on it, and #993's arithmetic answered a bug that
/// had been on the screen since the day it was written: <c>max-height: calc(100vh - 14rem)</c>, a constant
/// standing in for chrome that is 34 rem tall on the Nav desk. The cap resolved to 881 px against a 701 px
/// panel and never engaged once. So what this file guards is not "does it look right" — that is
/// <c>PlotPanelFitsTheWindowTests</c>' job, in a real browser at 1280×720, and it is unchanged — but the
/// two things a browser cannot tell you: that the head and the body are the elements the page named, and
/// that the arithmetic is still written down in the component's own stylesheet rather than guessed at
/// again somewhere else.</para>
/// </summary>
public sealed class TheCappedScrollPanelHoldsItsHeadTests
{
    [Fact]
    public async Task TheHeadIsThePanelsOwnChildrenAndTheBodyIsTheOneThatScrolls()
    {
        using ShellBench bench = ShellBench.Mount(Panel(hasContent: true));
        DeskBench.Painted painted = await bench.RenderAsync();

        DeskBench.Painted.Node panel = ShellBench.Wearing(painted, "capped-scroll")!;
        Assert.True(panel.HasClass("test-plot"),
            "the page's own class did not reach the panel, so every rule the page wrote for it is dead.");

        // The head rows are DIRECT children of the panel, with nothing wrapped around them. A wrapper here
        // would break every `.panel > *` rule any page has ever written against a panel of this shape —
        // .map-plot's own head rule among them.
        string[] children = panel.Children.Select(node => node.ClassList).ToArray();
        Assert.Contains("test-head-title", children);
        Assert.Contains("test-head-scrub", children);

        DeskBench.Painted.Node body = ShellBench.Wearing(painted, "capped-scroll-body")!;
        Assert.True(body.HasClass("test-plot-nodes"),
            "the page named the class its SCROLLING element must wear — the name its own browser-level "
            + "guard locates — and the component put it somewhere else.");
        Assert.Equal("test-row", Assert.Single(body.Children).ClassList);

        // …and the head is not inside the scroller. This is the whole point of the component: what is above
        // the plan keeps its measured height, and only the plan gives.
        Assert.DoesNotContain("test-head-title", body.Descendants().Select(node => node.ClassList));
    }

    /// <summary>
    /// AN EMPTY PLAN DRAWS NO EMPTY SCROLLER.
    ///
    /// <para>The body carries a floor — 6 rem for a flight plan, about two steps and a scrollbar — and a
    /// body element drawn on an empty plan would stand all of that floor under a sentence saying there is
    /// nothing to scroll. The empty state is a HEAD row for exactly that reason.</para>
    /// </summary>
    [Fact]
    public async Task AnEmptyPanelDrawsNoBodyAtAll()
    {
        using ShellBench bench = ShellBench.Mount(Panel(hasContent: false));
        DeskBench.Painted painted = await bench.RenderAsync();

        Assert.NotNull(ShellBench.Wearing(painted, "capped-scroll"));
        Assert.NotNull(ShellBench.Wearing(painted, "test-head-title"));
        Assert.Null(ShellBench.Wearing(painted, "capped-scroll-body"));
    }

    /// <summary>The floor travels as a custom property, so how short is too short stays the page's
    /// judgement while what to do about it stays the component's. Asserted on the STYLE the panel actually
    /// emitted, because a parameter nobody reads is a parameter that does nothing.</summary>
    [Fact]
    public async Task ThePagesFloorReachesTheStylesheet()
    {
        using ShellBench bench = ShellBench.Mount(Panel(hasContent: true, floor: "9rem"));
        DeskBench.Painted painted = await bench.RenderAsync();

        string style = ShellBench.Wearing(painted, "capped-scroll")!.Attributes.GetValueOrDefault("style") ?? "";
        Assert.Contains("--capped-scroll-floor: 9rem", style, StringComparison.Ordinal);

        string css = TheOverlayShellIsOneMechanismTests.PanelCss();
        Assert.Contains("min-height: var(--capped-scroll-floor", css);
    }

    /// <summary>
    /// #993'S ARITHMETIC IS STILL WRITTEN DOWN, AND IT IS WRITTEN DOWN HERE.
    ///
    /// <para>Six declarations, each measured for the Plotting panel and each load-bearing: the panel is the
    /// thing allowed to shrink, <c>min-height: 0</c> is what lets a flex item shrink below its content at
    /// all (without it a panel walks off the bottom of the screen — that IS #950), the panel's own overflow
    /// is the backstop, the body takes the remainder and scrolls, and a flick at the end of the list stays
    /// inside the list. A source-shape guard rather than a browser one because the browser guard already
    /// exists and passes; what this catches is somebody "tidying" one of these out of the file, which is
    /// how a cap that never fires gets written in the first place.</para>
    /// </summary>
    [Fact]
    public void TheComponentsStylesheetStillCarriesTheMeasurement()
    {
        string css = TheOverlayShellIsOneMechanismTests.PanelCss();

        foreach (string declaration in new[]
                 {
                     "flex: 0 1 auto", "min-height: 0", "overflow-y: auto",
                     "flex: 1 1 auto", "overscroll-behavior: contain",
                 })
        {
            Assert.True(css.Contains(declaration, StringComparison.Ordinal),
                $"`{declaration}` is gone from CappedScrollPanel.razor.css. Every line in that file was "
                + "measured for the Plotting panel at a real window size; dropping one is how #950 happened "
                + "— a cap that could not fire, on a panel nobody had measured.");
        }
    }

    private static RenderFragment Panel(bool hasContent, string floor = "6rem") => builder =>
    {
        builder.OpenComponent<CappedScrollPanel>(0);
        builder.AddComponentParameter(1, "class", "test-plot");
        builder.AddComponentParameter(2, nameof(CappedScrollPanel.BodyClass), "test-plot-nodes");
        builder.AddComponentParameter(3, nameof(CappedScrollPanel.BodyFloor), floor);
        builder.AddComponentParameter(4, nameof(CappedScrollPanel.HasContent), hasContent);
        builder.AddComponentParameter(5, nameof(CappedScrollPanel.Header), (RenderFragment)(head =>
        {
            head.OpenElement(0, "div");
            head.AddAttribute(1, "class", "test-head-title");
            head.AddContent(2, "Plotting");
            head.CloseElement();
            head.OpenElement(3, "div");
            head.AddAttribute(4, "class", "test-head-scrub");
            head.AddContent(5, "Scrub");
            head.CloseElement();
        }));
        builder.AddComponentParameter(6, nameof(CappedScrollPanel.ChildContent), (RenderFragment)(rows =>
        {
            rows.OpenElement(0, "div");
            rows.AddAttribute(1, "class", "test-row");
            rows.AddContent(2, "a burn");
            rows.CloseElement();
        }));
        builder.CloseComponent();
    };
}
