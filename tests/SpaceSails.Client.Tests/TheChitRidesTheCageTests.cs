using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #752 · THE CHIT'S OWN ARRIVAL, SAID WHERE IT CAN BE HEARD. The Hand's line is <i>"take this to the lift
/// and don't be clever near the counter"</i>; Core now makes the cage's gate read it, and this file is about
/// the half that is not arithmetic — WHERE the sentence lands.
///
/// <para>Same family, same law as <see cref="TheCardOpensTheBottomFloorTests"/> and #680 before it: <b>in the
/// DOM is not on the screen, and a line said while the world is changing is not said.</b> #689 shipped that
/// exact bug in this exact function once already — the beat was <c>ShowAndFile</c>d on the frame the panel
/// closed and the floor was torn down, and the owner played the whole loop without ever seeing it.</para>
///
/// <para>Source-shape guards, for the reason that file gives: the wiring is a partial class in a razor page,
/// and the thing that must never come back is a saying in the wrong PLACE.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheChitRidesTheCageTests
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

    private static string TheRide() => Between(
        Pages("Map.Surface.cs"),
        "private void RideTheLiftTo(",
        "private (double X, double Y) SecretLabHeadSpot(");

    [Fact]
    public void TheGateBeatIsSaidWhenTheDoorsOPENAndNeverOnTheFrameTheCarLeaves()
    {
        // (c) · #689's lesson, applied to the second paper before it can be learned twice. PressLiftButton
        // closes the panel and immediately rebuilds the floor; anything said between those two events is
        // said to a screen that is being replaced.
        string press = Between(
            Pages("Map.Surface.cs"), "private void PressLiftButton(", "private void CloseLiftPanel(");
        Assert.True(!press.Contains("ChitGateLine", StringComparison.Ordinal),
            "PressLiftButton says the chit's gate line itself — the frame the panel closes and the floor " +
            "transition begins, which is exactly where the owner never saw the card's (#689/#752).");

        string ride = TheRide();
        Assert.True(ride.Contains("CanteenTable.ChitGateLine", StringComparison.Ordinal),
            "the arrival never says the chit's gate line — the job the #748 scene was hired for still stops " +
            "one door short (#752).");

        // …and LAST, after the routine air line. ShowPulseMessage keeps ONE slot and the last write wins, so
        // a beat placed above "the air here is breathable" is a beat the air line erases: filed, and never
        // seen. That is the whole shape of #689's bug, expressed as an ordering.
        int air = ride.IndexOf("UndergroundComplex.PressurisedLine", StringComparison.Ordinal);
        int chit = ride.IndexOf("CanteenTable.ChitGateLine", StringComparison.Ordinal);
        Assert.True(air >= 0, "the arrival's air line has moved; this guard's ordering check is blind now.");
        Assert.True(chit > air,
            "the chit's gate beat is said BEFORE the arrival's routine air line, which overwrites the one " +
            "pulse slot (#752).");
    }

    [Fact]
    public void TheRideAsksTHESTOPWhichPaperOpenedItAndNotTheWallet()
    {
        // Core decides, the razor draws (#600). The arrival must read the pressed BUTTON's own verdict —
        // a client that re-asked "am I carrying a chit?" here would narrate the gate on every trip a
        // chit-carrying captain took, including the ones the gate had nothing to do with.
        string ride = TheRide();
        Assert.True(ride.Contains("OpenedByChit", StringComparison.Ordinal),
            "the arrival does not read the stop's own OpenedByChit — whatever it reads instead is a second " +
            "opinion about which gate the trip crossed (#752).");
    }

    [Fact]
    public void TheGistIsFiledWithTheBeatAndBOTHHappenOnce()
    {
        // (d) · The book gets what the paper turned out to be worth, once. A beat that files on every trip
        // turns the field book into a lift log.
        string ride = TheRide();
        Assert.True(ride.Contains("CanteenTable.ChitGateGist", StringComparison.Ordinal),
            "the arrival never files the chit's gist — the book keeps the beat and not the reading (#752).");

        string beat = Between(ride, "via is { OpenedByChit: true }", "#677 · AND THE TWO SENTENCES");
        Assert.True(beat.Contains("ex.ChitGateBeatShown = true", StringComparison.Ordinal),
            "the chit's arrival never latches ChitGateBeatShown — it would say itself and file itself on " +
            "every ride down the cage (#752).");
        Assert.True(
            ride.Contains("!ex.ChitGateBeatShown", StringComparison.Ordinal),
            "nothing tests ChitGateBeatShown before the beat: a flag that is written and never read is not " +
            "a once (#752).");
    }

    [Fact]
    public void TheCarPanelDrawsWHICHEVERPaperOpensTheRow()
    {
        // The affordance itself is #692's row, reused rather than re-cut: the panel asks LiftStop.OpenedBy
        // and prints it. If this file ever starts asking about a CARD specifically, the chit's row goes
        // quiet — a button that opens and does not say why is the "🔒 sealed" bug in a nicer coat.
        string panel = Between(Pages("Map.razor"), "@if (_showLiftPanel", "_lockedDoor is { }");

        Assert.True(panel.Contains("stop.OpenedBy is { } carriedCard", StringComparison.Ordinal),
            "the car panel no longer draws LiftStop.OpenedBy as the paper that opens the row (#692/#752).");
        Assert.True(!panel.Contains("AuthorityCard", StringComparison.Ordinal),
            "the car panel names the AUTHORITY CARD type — the row has stopped being about whatever paper " +
            "Core says opens it, and the day-labour chit is the paper that proves it (#752).");
    }
}
