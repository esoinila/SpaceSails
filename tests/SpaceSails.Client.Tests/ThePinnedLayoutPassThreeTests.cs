using System;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// LAYOUT PASS 3 (playtest 2026-10-29 to 11-01) · the parrot's bubble, the guard stop's pinned choice row and the Nav
/// checklist's room. The behaviours are asserted on a live page; the CSS halves are source guards over the cascade
/// (the pixel numbers are in the PR, measured in a browser at 1024 x 700, 390 x 700 and 1440 x 900).
/// </summary>
public sealed class ThePinnedLayoutPassThreeTests
{
    private static string Cascade() =>
        MapMarkup.Read(Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));

    private static string Rule(string css, string selector)
    {
        Match m = Regex.Match(css, "(?m)^" + Regex.Escape(selector) + @"\s*\{(?<body>[^}]*)\}");
        Assert.True(m.Success, $"the cascade no longer has a `{selector}` rule this guard audits.");
        return m.Groups["body"].Value;
    }

    /// <summary>
    /// U02-F07 / U10-F03 · <b>A CLICK ON THE PARROT'S BUBBLE PUTS IT AWAY.</b> The bubble used to be
    /// <c>pointer-events: none</c>, so a click on it fell through to the canvas and click-to-walk marched the captain
    /// off; it is a control now, and a control that cannot be closed is against the house law.
    ///
    /// <para><b>Proven RED</b> by <c>DismissParrot</c> made a no-op: the squawk stays up.</para>
    /// </summary>
    [Fact]
    public void AClickOnTheBubbleClearsTheSquawk()
    {
        Pages.Map map = Boot("parrot-click");
        Set(map, "_parrotSquawk", "SQUAWK!");
        Invoke(map, "DismissParrot");
        Assert.Null(Read(map, "_parrotSquawk"));
    }

    /// <summary>The bubble is wired to that verb, takes the pointer, and no longer hangs beside the bird where it landed
    /// on the desk's controls and the plan's tiles.</summary>
    [Fact]
    public void TheBubbleIsAControlAndLeavesThePerch()
    {
        string razor = MapMarkup.Read(Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map", "ParrotAndDiceRack.razor"));
        int at = razor.IndexOf("class=\"parrot-bubble ", StringComparison.Ordinal);
        Assert.True(at >= 0, "the bubble is gone from the markup.");
        Assert.Contains("Dismiss(DismissParrot)", razor[at..(at + 260)], StringComparison.Ordinal);
        Assert.Contains("parrot-bubble-deck", razor[at..(at + 120)], StringComparison.Ordinal);

        // Live probe (PR body): at 390x700, 1024x700 and 1440x900 the strip under the bubble holds a pointer target on Nav
        // (phone), Comms, Galley, Trade, War and Captain, and only the canvas on the walked Deck. So ONLY the Deck's bubble
        // takes the pointer: every other desk keeps it a caption, and a click can never land on a button or a comms-node.
        string bubble = Rule(Cascade(), ".parrot-bubble");
        Assert.Contains("pointer-events: none", bubble, StringComparison.Ordinal);
        Assert.DoesNotContain("pointer-events: auto", bubble, StringComparison.Ordinal);
        Assert.Contains("pointer-events: auto", Rule(Cascade(), ".parrot-bubble-deck"), StringComparison.Ordinal);
        Assert.Contains("position: fixed", bubble, StringComparison.Ordinal);
        Assert.Contains("left: 3.5rem", bubble, StringComparison.Ordinal);
        Assert.Contains("bottom: 2.4rem", bubble, StringComparison.Ordinal);
        Assert.Contains("pointer-events: none", Rule(Cascade(), ".parrot-perch"), StringComparison.Ordinal);
    }

    /// <summary>
    /// U10-F01 · The guard stop's choice row is pinned one Close above the foot, with the card's own fill, so at
    /// 1024 x 700 the moves are on the glass beside Close (SAY NOTHING) rather than below the card's cap.
    ///
    /// <para><b>Proven RED</b> by the rule taken out: the row scrolls with the body and Close alone is visible.</para>
    /// </summary>
    [Fact]
    public void TheStopsChoiceRowIsPinnedAboveClose()
    {
        string row = Rule(Cascade(), ".view-object > .table-actions");
        Assert.Contains("position: sticky", row, StringComparison.Ordinal);
        Assert.Contains("bottom: 1.75rem", row, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--card-foot-fill", row, StringComparison.Ordinal);
    }

    /// <summary>
    /// U10-F06 · While the lesson checklist is open the Nav column leaves its room and the card scrolls inside a cap,
    /// gated to windows 900px wide and up so what a smaller window does is not decided here.
    ///
    /// <para><b>Proven RED</b> by the media block taken out: the checklist overprints the dock hint again.</para>
    /// </summary>
    [Fact]
    public void TheChecklistLeavesTheNavColumnItsRoom()
    {
        string css = Cascade();
        int at = css.IndexOf("@media (min-width: 900px) {", StringComparison.Ordinal);
        Assert.True(at >= 0, "the checklist's media block is gone.");
        string block = css[at..css.IndexOf("::deep .mission-celebration", at, StringComparison.Ordinal)];
        Assert.Contains("max-height: 10rem", block, StringComparison.Ordinal);
        Assert.Contains("overflow-y: auto", block, StringComparison.Ordinal);
        Assert.Contains(".map-page:has(.map-tutorial) .map-hud", block, StringComparison.Ordinal);
        Assert.Contains("margin-bottom: 11rem", block, StringComparison.Ordinal);
    }
}
