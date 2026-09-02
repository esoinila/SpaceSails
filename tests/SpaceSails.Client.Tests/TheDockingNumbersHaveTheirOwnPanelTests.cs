using System;
using System.IO;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #200 · <b>THE DOCKING NUMBERS HAVE THEIR OWN PANEL.</b> The law lives next door in Core
/// (<c>TheDockingNumbersToHitTests</c>): what these gates hold is the WIRING, because a panel composed
/// perfectly in <see cref="DockFocus"/> and never drawn is exactly the shape of a shipped bug this repo has
/// filed before.
///
/// <para><b>Source-shape guards, and why.</b> The claims are about where the panel SITS and what it is
/// allowed to know: in the flow of the Nav readouts (so it cannot fight the nav-target lines or the Plotting
/// panel for a pixel — geometry, not z-index), raised on the clamp affordance's own gate, and quoting no
/// threshold of its own. None of that can be read off a rendered frame. Each gate carries an anti-vacuous
/// half — a landmark that has nothing to do with docking must be found in the same file first — so a rename
/// or a move reddens these instead of quietly passing over nothing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDockingNumbersHaveTheirOwnPanelTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file)
    {
        string path = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file);
        Assert.True(File.Exists(path), $"{file} is not where this guard reads it ({path}).");
        string src = File.ReadAllText(path);
        Assert.True(src.Length > 400, $"{file} is suspiciously empty — this guard would be reading nothing.");
        return src;
    }

    /// <summary>The panel's own markup, sliced out of the shipping page so the gates below can speak about
    /// IT and not about the rest of a 7,000-line razor that legitimately quotes the dock constants
    /// elsewhere (the ⚓ hint on the Nearest line, the destination readout).</summary>
    private static string PanelBlock(string page)
    {
        const string opens = "@if (DockFocusLive)";
        int start = page.IndexOf(opens, StringComparison.Ordinal);
        Assert.True(start >= 0, "the #200 focus panel is no longer raised by `@if (DockFocusLive)` in Map.razor.");

        // It ends where the #954 Nearest readout begins — the toggling line the owner was reading instead.
        int end = page.IndexOf("Nearest: @NearestReadoutName()", start, StringComparison.Ordinal);
        Assert.True(end > start, "the focus panel no longer sits directly above the Nearest readout.");
        return page[start..end];
    }

    /// <summary>
    /// <b>The panel is on the glass, and it is the piracy box.</b> The owner asked for the numbers "just
    /// like in piracy-hold", and the model is the autosteal criterion box on a prey dossier — so that box's
    /// own heading is asserted here too: if it is renamed or retired, whoever does it is made to look at the
    /// panel that copied it.
    /// </summary>
    [Fact]
    public void ThePanelIsDrawnAndItMirrorsThePiracyCriterionBox()
    {
        string page = Pages("Map.razor");
        Assert.Contains("🎯 Autosteal needs BOTH:", page, StringComparison.Ordinal); // the model it mirrors

        string panel = PanelBlock(page);
        Assert.Contains("map-dock-focus", panel, StringComparison.Ordinal);
        Assert.Contains("DockFocus.Rows(_dockAffordance, EffectiveDockTankPulses)", panel, StringComparison.Ordinal);
        Assert.Contains("DockFocus.Verdict(_dockAffordance, EffectiveDockTankPulses)", panel, StringComparison.Ordinal);

        // Row-per-gate, reading against required, obviously green inside — the piracy idiom, element for
        // element: the tick/cross, the label, the reading, the gate.
        Assert.Contains("row.Inside ? \"text-success\" : \"text-warning\"", panel, StringComparison.Ordinal);
        Assert.Contains("@row.Label", panel, StringComparison.Ordinal);
        Assert.Contains("@row.Reading", panel, StringComparison.Ordinal);
        Assert.Contains("@row.Gate", panel, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>ONE SOURCE — the panel quotes no threshold of its own.</b> The named bug class: a number typed
    /// twice, once where it is enforced and once where it is displayed, drifting apart at leisure. The whole
    /// point of #200's panel is to tell the captain what the clamp will do, so the moment one of these
    /// appears inside its markup, the display has started arguing with the arm.
    /// </summary>
    [Fact]
    public void ThePanelReTypesNoThresholdOfItsOwn()
    {
        string panel = PanelBlock(Pages("Map.razor"));

        foreach (string typedTwice in new[]
                 {
                     "EnvelopeMeters", "MatchSpeed", "DockReachMeters", "DockMatchSpeedMps",
                     "500,000", "8000", "/ 1000", "_reactionMassPulses",
                 })
        {
            Assert.DoesNotContain(typedTwice, panel, StringComparison.Ordinal);
        }

        // …and the same file DOES quote them elsewhere, which is what makes the absence above meaningful
        // rather than a slice that happened to catch nothing.
        Assert.Contains("DockReachMeters", Pages("Map.razor"), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The gate it speaks on is the clamp's own.</b> Not a hand-rolled "is a haven nearby" — the
    /// affordance's phase, the same one-truth (#212) the ⚓ button reads, which is why a haven merely
    /// drifting past cannot raise it. And the tank it judges the match burn against is the tank the
    /// affordance itself was evaluated with (#268), quoted through one property rather than recomputed.
    /// </summary>
    [Fact]
    public void ThePanelInheritsTheClampsOwnGateAndTheClampsOwnTank()
    {
        string docking = Pages("Map.Docking.cs");
        Assert.Contains("private void UpdateDockAffordance()", docking, StringComparison.Ordinal); // the right file

        Assert.Contains("private bool DockFocusLive => DockFocus.IsLive(_dockAffordance);", docking, StringComparison.Ordinal);
        Assert.Contains("private int EffectiveDockTankPulses =>", docking, StringComparison.Ordinal);
        Assert.Contains(
            "DockAffordanceRule.Evaluate(_ship, havens, EffectiveDockTankPulses, _dockLatched)",
            docking, StringComparison.Ordinal);

        // The affordance is Hidden while clamped on (the 🚀 Undock button owns that moment), so the panel
        // puts itself away without a second rule about being docked.
        Assert.True(DockFocus.IsLive(new DockAffordance(DockPhase.Approach, "x", "X", 0, 0, 0, false)));
        Assert.False(DockFocus.IsLive(DockAffordance.Hidden));
    }

    /// <summary>
    /// <b>The coaching sentence has one copy.</b> The nav-target line and the panel's verdict describe the
    /// same approach, so they are the same words: <see cref="DockFocus"/> owns them and
    /// <c>DockStatusLine</c> returns them. A literal re-appearing in the page is the sentence starting to
    /// drift from the panel that quotes it.
    /// </summary>
    [Fact]
    public void TheCoachingSentencesAreSpokenFromOneCopy()
    {
        string docking = Pages("Map.Docking.cs");
        Assert.Contains("private string DockStatusLine(OrbitAssistInfo oi)", docking, StringComparison.Ordinal);

        Assert.Contains("DockFocus.ClampedOnLine", docking, StringComparison.Ordinal);
        Assert.Contains("DockFocus.CoastCloserLine()", docking, StringComparison.Ordinal);
        Assert.Contains("DockFocus.MatchClampLine", docking, StringComparison.Ordinal);
        Assert.Contains("DockFocus.ClampNowLine", docking, StringComparison.Ordinal);

        foreach (string typedTwice in new[]
                 {
                     "\"clamped on — lying low\"",
                     "\"alongside and matched",
                     "\"alongside but hot",
                     "coast within {",
                 })
        {
            Assert.DoesNotContain(typedTwice, docking, StringComparison.Ordinal);
        }

        // The words themselves are unchanged from what #213 shipped — this wave moved them, it did not
        // rewrite them, and the panel's verdict is built out of the very same string.
        Assert.Equal("alongside and matched — hit ⚓ Dock to clamp on", DockFocus.ClampNowLine);
        Assert.Equal(
            "→ " + DockFocus.MatchClampLine,
            DockFocus.Verdict(new DockAffordance(DockPhase.MatchClamp, "x", "X", 1e8, 12_000, 40, true), 250));
    }

    /// <summary>
    /// <b>Geometry, not z-index.</b> The panel lives in the FLOW of <c>.map-readouts</c> — the same in-flow
    /// housing the piracy run's own sections use — inside the window-bound column (#992). A box that claims
    /// real document height there cannot paint over the nav-target lines above it or the Plotting panel
    /// below it, and the readouts block's own <c>overflow-y: auto</c> means it scrolls only when the window
    /// is genuinely too short. So the panel must carry no positioning of its own, and the block it sits in
    /// must still be the one that caps itself.
    /// </summary>
    [Fact]
    public void ThePanelIsInTheFlowAndWinsNoStackingArgument()
    {
        string page = Pages("Map.razor");
        string panel = PanelBlock(page);

        foreach (string positioned in new[] { "z-index", "position:", "position-absolute", "position-fixed" })
        {
            Assert.DoesNotContain(positioned, panel, StringComparison.OrdinalIgnoreCase);
        }

        // It is inside the readouts block, above the Nearest line — PanelBlock already proved the second
        // half; this proves the first, by finding the block's opening ahead of the panel.
        int readouts = page.IndexOf("<div class=\"map-readouts", StringComparison.Ordinal);
        int panelAt = page.IndexOf("@if (DockFocusLive)", StringComparison.Ordinal);
        Assert.True(readouts >= 0 && readouts < panelAt, "the focus panel is no longer inside the Nav readouts block.");

        // …and that block still caps and scrolls itself, which is what makes "in the flow" a safe answer.
        string css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        int rule = css.IndexOf(".map-hud .map-readouts {", StringComparison.Ordinal);
        Assert.True(rule >= 0, "the .map-readouts sizing rule this panel relies on is gone.");
        Assert.Contains("overflow-y: auto", css[rule..(rule + 200)], StringComparison.Ordinal);
    }
}
