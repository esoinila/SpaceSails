using System;
using System.IO;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1463 items 1, 2 and 4 · pure stylesheet facts the live-site pass found (the drill HUD, item 3, has a measured
/// fit test of its own). These read source because the rules are pure CSS; the browser pass after publish is the
/// pixel proof.
/// </summary>
public sealed class ThePhoneAndDesktopLayoutPassTests
{
    private static string Src(params string[] parts) =>
        File.ReadAllText(Path.Combine(SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client", Path.Combine(parts)));

    private static string RuleAfter(string css, string selector)
    {
        int at = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(at >= 0, $"the stylesheet must carry `{selector}`");
        return css[at..css.IndexOf('}', at)];
    }

    [Fact]
    public void TheFirstGroundCardsFootnoteRidesWithItsPinnedButton()
    {
        string map = Src("Pages", "Map.razor.css");
        // The button stays pinned (#735): nothing may take it out of the pinned-foot list or make it static.
        Assert.Contains("::deep .convergence-card > .convergence-close,", map);
        Assert.DoesNotContain("ground-lesson-card > .convergence-close {", map.ReplaceLineEndings("\n"));

        string own = Src("Pages", "Map", "GroundLessonCard.razor.css").ReplaceLineEndings("\n");
        string rule = RuleAfter(own, "::deep .ground-lesson-card > .ground-lesson-foot {");
        Assert.Contains("position: sticky", rule);
        Assert.Contains("bottom:", rule);
        Assert.Contains("background:", rule);   // the card's own fill, so prose slides under it and not through it
    }

    [Fact]
    public void ThePhoneMapControlsFollowTheRealTabBar()
    {
        string js = Src("wwwroot", "renderer.js");
        int at = js.IndexOf("keepTheMapControlsBelowTheTabBar()", StringComparison.Ordinal);
        Assert.True(at >= 0, "the tab-bar observer must exist");
        string fn = js[at..];
        Assert.Contains("ResizeObserver", fn);
        Assert.Contains("getBoundingClientRect().bottom", fn);
        Assert.Contains("--map-control-top", fn);
        Assert.Contains("--map-controls-on", fn);
        Assert.Contains("removeProperty('--map-control-top')", fn);   // wider than a phone the typed values stand

        string bar = Src("Pages", "Map", "ShipTabsStrip.razor.css");
        Assert.Contains("--map-controls-on", bar);   // the banner is pushed below the controls' strip
        Assert.Contains("top: var(--map-control-top)", Src("Pages", "Map", "NavSearchPanel.razor.css"));
        Assert.Contains("var(--map-control-top)", Src("Pages", "Map", "NavSearchChips.razor.css"));
    }

    [Fact]
    public void TheDeskChipColumnStepsAsideOnlyWhileACardIsOpenOnAPhone()
    {
        string map = Src("Pages", "Map.razor.css").ReplaceLineEndings("\n");
        int at = map.IndexOf(".map-page:has(.deck-offer-card)", StringComparison.Ordinal);
        Assert.True(at >= 0, "the chip column needs a card-open rule");
        Assert.Contains("@media (max-width: 600px)", map[..at][^200..]);
        Assert.Contains("--desk-chip-strip-clearance: 0.75rem", map[at..]);
        Assert.Contains("display: none", RuleAfter(map, ".map-page:has(.deck-offer-card) ::deep .desk-chip-strip"));

        // …and the chips themselves are never hidden by width alone: they carry each desk's status summary.
        string chips = Src("Pages", "Stations", "DeskChips.razor.css");
        Assert.DoesNotContain("display: none", chips);
    }
}
