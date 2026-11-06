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
    public void TheFirstGroundCardsFootIsNotUnderItsOwnPinnedButton()
    {
        string css = Src("Pages", "Map.razor.css");
        string rule = RuleAfter(css, "::deep .ground-lesson-card > .convergence-close {\n".Replace("\n", css.Contains("\r\n") ? "\r\n" : "\n") + "        position: static");
        Assert.Contains("position: static", rule);
        Assert.Contains("box-shadow: none", rule);
        // …and it comes AFTER the pinned-foot list that would otherwise win the tie on specificity.
        int list = css.IndexOf("::deep .convergence-card > .convergence-close,", StringComparison.Ordinal);
        int mine = css.IndexOf("::deep .ground-lesson-card > .convergence-close {", StringComparison.Ordinal);
        Assert.True(list >= 0 && mine > list, "the override must follow the pinned-foot list");
    }

    [Fact]
    public void ThePhoneMapControlsClearAThreeRowTabBar()
    {
        string css = Src("Pages", "Map.razor.css");
        int at = css.IndexOf("@media (max-width: 480px)", StringComparison.Ordinal);
        Assert.True(at >= 0, "a narrow-screen tier must move the map controls");
        string block = css[at..css.IndexOf("--map-control-top", at)];
        Assert.Contains(".map-page", css[at..(css.IndexOf("--map-control-top", at) + 40)]);
        Assert.True(block.Length > 0);
        string after = css[css.IndexOf("--map-control-top", at)..];
        double rem = double.Parse(after.Split(':')[1].Split("rem")[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(rem >= 7.0, $"three 1.9rem tab rows plus padding need >= 7rem, found {rem}rem");
        // Both floating controls and the masthead's floor still read the ONE variable.
        Assert.Contains("top: var(--map-control-top)", Src("Pages", "Map", "NavSearchPanel.razor.css"));
        Assert.Contains("var(--map-control-top)", Src("Pages", "Map", "NavSearchChips.razor.css"));
    }

    [Fact]
    public void TheDeskChipColumnStepsOutOfAPhonesWay()
    {
        string chips = Src("Pages", "Stations", "DeskChips.razor.css");
        int at = chips.IndexOf("@media (max-width: 600px)", StringComparison.Ordinal);
        Assert.True(at >= 0, "the chip column needs a narrow-screen rule");
        Assert.Contains("display: none", chips[at..]);
        Assert.Contains("--desk-chip-strip-clearance: 0.75rem", Src("Pages", "Map.razor.css"));
    }
}
