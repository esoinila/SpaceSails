using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;

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

        Assert.True(ride.Contains("UndergroundComplex.ArrivalBeat.CardAccepted", StringComparison.Ordinal),
            "the arrival never handles the card-accepted beat — the beat has nowhere left to land (#689).");

        // …and it no longer has to be written LAST of the arrival's sayings, which is what this guard used
        // to pin. That was a contract living in a comment: ShowPulseMessage kept one slot, the last write
        // won, and every future author had to know to write below the routine air line. #693 replaced it
        // with a law — the sayings are composed once in Core with a RANK, and the slot refuses a lesser
        // line wherever it is written.
        Assert.True(!ride.Contains("CardAcceptedLine", StringComparison.Ordinal),
            "the ride says the card-accepted line itself again, which puts the ordering discipline back " +
            "where #693 took a law out of it (UndergroundComplex.ArrivalSayings composes the arrival).");
    }

    [Fact]
    public void TheAcceptedBeatOutranksTheRoutineAirLineWhereverItIsWritten()
    {
        // #693 · The ordering above, as the law that replaced it, asked of the shipping composition and the
        // shipping slot. #692 had to ship "last of the arrival's sayings" as a comment plus an ordering test
        // because there was nothing else to say it in; this is the something else.
        int crossings = 0;
        foreach (string body in new[]
                 {
                     "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "miranda",
                     "triton", "the-clinker", "secret-lab-site", "secret-lab-site-unlisted",
                 })
        {
            var wallet = new HashSet<string>();
            for (int band = 0;
                 band <= UndergroundComplex.BandOf(UndergroundComplex.DeepestPossibleFloor);
                 band++)
            {
                if (UndergroundComplex.SiteHasBand(body, band))
                {
                    wallet.Add(new UndergroundComplex.AuthorityCard(body, band).Id);
                }
            }

            foreach (int from in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.LiftStop stop in
                         UndergroundComplex.LiftPanel(body, from, wallet))
                {
                    if (stop.OpenedBy is null || stop.OpenedByChit || stop.IsCurrent
                        || stop.Refusal is not null)
                    {
                        continue;
                    }

                    IReadOnlyList<UndergroundComplex.Saying> sayings = UndergroundComplex.ArrivalSayings(
                        body, from, stop.Level,
                        new UndergroundComplex.ArrivalMemory(WasUnderground: true), stop, wallet);
                    if (!sayings.Any(s => s.Beat == UndergroundComplex.ArrivalBeat.CardAccepted))
                    {
                        continue;
                    }

                    crossings++;
                    PulseSlot slot = PulseSlot.Empty;
                    foreach (UndergroundComplex.Saying s in sayings)
                    {
                        slot = slot.Write(s.Text, s.Rank, 0.0);
                    }

                    Assert.NotEqual(UndergroundComplex.PressurisedLine, slot.Message);
                    Assert.NotEqual(UndergroundComplex.DeadAirLine, slot.Message);
                }
            }
        }

        Assert.True(crossings > 5, $"only {crossings} carded crossing(s) in the sweep — this proves little.");
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
