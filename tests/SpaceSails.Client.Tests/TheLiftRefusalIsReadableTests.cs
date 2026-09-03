using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #686 · UNREADABLE TEXT FROM THE LIFT PANEL — #680's disease, second organ. Owner, at the car panel on
/// the deep site: <i>"pressing locked floor on the lift panel — result text is not displayed so it can be
/// read. It is the same kind of bug as we had with the inventory item use. Let's fix this also the same
/// way."</i>
///
/// <para>A refusing button is PRESENT and says why (#600) — but it said why to the pulse HUD, under the
/// open panel's backdrop blur. Same law as <see cref="TheTryOutcomeIsReadableTests"/>: in the DOM is not
/// on the screen, and the one layer the backdrop cannot blur is the dialog's own subtree. The refusal is
/// still FILED (the book is the record); only the saying moves into the panel.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLiftRefusalIsReadableTests
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

    private static string Pages(string file) =>
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    [Fact]
    public void TheRefusalIsSaidInsideThePanelItself()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("@if (_showLiftPanel", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the lift panel block this guard knows how to find.");
        int end = razor.IndexOf("_lockedDoor is { }", start, StringComparison.Ordinal);
        Assert.True(end > start, "Map.razor's lift panel block no longer ends where this guard expects.");
        string panel = razor[start..end];

        Assert.True(panel.Contains("_liftOutcome", StringComparison.Ordinal),
            "the CAR PANEL never renders _liftOutcome — a refused floor answers somewhere the player is " +
            "not looking (#686).");
        Assert.True(panel.Contains("lift-outcome", StringComparison.Ordinal),
            "the CAR PANEL has no styled outcome row (#686).");
    }

    [Fact]
    public void APressedRefusalNeverRoutesItsLineToThePulseHud()
    {
        // The panel stays open on a refusal — that is what makes it a panel and not a slot machine — so
        // any line pulsed from inside PressLiftButton's refusal path plays behind the frosted glass. The
        // success path closes the panel first, and its lines (the card accepted, the ride) may pulse.
        string surface = Pages("Map.Surface.Hive.cs");
        int at = surface.IndexOf("private void PressLiftButton(", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.Surface.cs no longer has PressLiftButton where this guard can read it.");
        int panelCloses = surface.IndexOf("_showLiftPanel = false", at, StringComparison.Ordinal);
        Assert.True(panelCloses > at, "PressLiftButton no longer closes the panel — this guard needs re-reading.");
        string refusalPath = surface[at..panelCloses];

        Assert.True(refusalPath.Contains("_liftOutcome", StringComparison.Ordinal),
            "PressLiftButton's refusal path never stores the line for the panel — the answer goes back " +
            "behind the blur (#686).");
        Assert.True(!refusalPath.Contains("ShowPulseMessage", StringComparison.Ordinal),
            "PressLiftButton pulses while the panel is still open — the exact broken shape #686 was filed " +
            "on: in the DOM is not on the screen.");
    }

    [Fact]
    public void TheOutcomeRowIsStyledLikeThePanelItSitsIn()
    {
        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        Assert.True(css.Contains(".lift-outcome", StringComparison.Ordinal),
            "the lift panel's outcome row has no style of its own (#686).");
    }
}
