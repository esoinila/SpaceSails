using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #689 · THE CARD'S FINEST HOUR HAPPENED OFF-SCREEN. Owner, having found the card, fed the gate and ridden
/// past the floor the building admits to: <i>"It was locked until I got it ... there was no story point
/// about it being needed or used. Let's tell that story somehow more clearly that it was used in the
/// elevator or somehow played a part in opening the most bottom floor."</i>
///
/// <para>The line was there. It was <c>ShowAndFile</c>d in <c>PressLiftButton</c> on the exact frame the
/// panel closes and the floor is torn down and rebuilt — the third organ of #680's disease: not under a
/// modal this time, under a scene change. Same family as <see cref="TheLiftRefusalIsReadableTests"/>, same
/// law: <b>in the DOM is not on the screen, and a line said while the world is changing is not said.</b></para>
///
/// <para>These are source-shape guards for the same reason that file's are: the wiring is a partial class in
/// a razor page, and the thing that must never come back is a saying in the wrong PLACE.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCardOpensTheBottomFloorTests
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
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    [Fact]
    public void TheAcceptedLineIsNEVERSaidOnTheFrameTheCarLeaves()
    {
        // PressLiftButton closes the panel and immediately rebuilds the floor. Anything said between those
        // two events is said to a screen that is being replaced.
        string press = Between(
            Pages("Map.Surface.cs"), "private void PressLiftButton(", "private void CloseLiftPanel(");

        Assert.True(!press.Contains("CardAcceptedLine", StringComparison.Ordinal),
            "PressLiftButton says the card-accepted line itself — the frame the panel closes and the floor " +
            "transition begins, which is exactly where the owner never saw it (#689).");
    }

    [Fact]
    public void TheAcceptedLineIsSaidWhenTheDoorsOPENOnTheNewFloor()
    {
        string ride = Between(
            Pages("Map.Surface.cs"),
            "private void RideTheLiftTo(",
            "private (double X, double Y) SecretLabHeadSpot(");

        Assert.True(ride.Contains("CardAcceptedLine", StringComparison.Ordinal),
            "the arrival never says the card-accepted line — the beat has nowhere left to land (#689).");

        // …and LAST of the arrival's sayings. ShowPulseMessage keeps ONE slot and the last write wins, so a
        // beat placed above the routine air line is a beat the air line erases. This is the whole reason the
        // owner filed the issue, expressed as an ordering.
        int air = ride.IndexOf("UndergroundComplex.PressurisedLine", StringComparison.Ordinal);
        int card = ride.IndexOf("CardAcceptedLine", StringComparison.Ordinal);
        Assert.True(air >= 0, "the arrival's air line has moved; this guard's ordering check is blind now.");
        Assert.True(card > air,
            "the card-accepted beat is said BEFORE the arrival's routine air line, which overwrites the one " +
            "pulse slot. The line would be filed and never seen — the bug, again (#689).");
    }

    [Fact]
    public void TheGatedRowSaysTheCardWillBeReadBeforeTheRide()
    {
        string panel = Between(Pages("Map.razor"), "@if (_showLiftPanel", "_lockedDoor is { }");

        Assert.True(panel.Contains("OpenedBy", StringComparison.Ordinal),
            "the CAR PANEL never reads LiftStop.OpenedBy — the row cannot say the card matters before the " +
            "ride, which is the half of #689 that happens BEFORE the doors close.");
        Assert.True(panel.Contains("the gate will read it", StringComparison.Ordinal),
            "the gated row never names what the held card will do (#689).");

        string css = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        Assert.True(css.Contains(".lift-stop-card", StringComparison.Ordinal),
            "the card's title has no style of its own on the button — an institutional string in a nowrap " +
            "air cell is a row that overflows its panel (#689).");
    }
}
