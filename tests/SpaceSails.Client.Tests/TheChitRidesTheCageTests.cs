using System;
using System.Collections.Generic;
using System.IO;
using SpaceSails.Core;

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
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    private static string TheRide() => Between(
        Pages("Map.Surface.Hive.cs"),
        "private void RideTheLiftTo(",
        "private (double X, double Y) SecretLabHeadSpot(");

    /// <summary>The one ride the chit opens, found on a real site rather than typed in: standing on the
    /// first band with the paper in the satchel, the panel grows a row for the shaft below it.</summary>
    private static (string Body, int From, UndergroundComplex.LiftStop Stop) TheCageRide()
    {
        Satchel.Item[] carried = [CanteenTable.Chit(underAnotherName: false)];
        foreach (string body in new[]
                 {
                     "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "miranda",
                     "triton", "the-clinker", "secret-lab-site", "secret-lab-site-unlisted",
                 })
        {
            foreach (int from in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.LiftStop stop in
                         UndergroundComplex.LiftPanel(body, from, [], carried))
                {
                    if (stop.OpenedByChit)
                    {
                        return (body, from, stop);
                    }
                }
            }
        }

        throw new InvalidOperationException("no site in the sweep offers a row the day-labour chit opens.");
    }

    [Fact]
    public void TheGateBeatIsSaidWhenTheDoorsOPENAndNeverOnTheFrameTheCarLeaves()
    {
        // (c) · #689's lesson, applied to the second paper before it can be learned twice. PressLiftButton
        // closes the panel and immediately rebuilds the floor; anything said between those two events is
        // said to a screen that is being replaced.
        string press = Between(
            Pages("Map.Surface.Hive.cs"), "private void PressLiftButton(", "private void CloseLiftPanel(");
        Assert.True(!press.Contains("ChitGate", StringComparison.Ordinal),
            "PressLiftButton says the chit's gate line itself — the frame the panel closes and the floor " +
            "transition begins, which is exactly where the owner never saw the card's (#689/#752).");

        string ride = TheRide();
        Assert.True(ride.Contains("UndergroundComplex.ArrivalBeat.ChitGate", StringComparison.Ordinal),
            "the arrival never handles the chit's gate beat — the job the #748 scene was hired for still " +
            "stops one door short (#752).");

        // …and it no longer has to be written LAST, which is what this guard used to pin. #693 replaced that
        // discipline with a law: the arrival's sayings are composed in Core with a rank, and the one pulse
        // slot refuses a lesser line rather than trusting every future author to write below the air line.
        Assert.True(!ride.Contains("CanteenTable.ChitGateLine", StringComparison.Ordinal),
            "the ride says the chit's line itself again — the arrival's sayings are composed once, in Core " +
            "(UndergroundComplex.ArrivalSayings), so that no call site has to know what the call sites " +
            "after it are going to say (#693).");
    }

    [Fact]
    public void TheChitsBeatOutranksTheRoutineAirLineWhereverItIsWritten()
    {
        // #693 · The ORDERING this file used to assert, as the law that replaced it. Asked of the shipping
        // composition and the shipping slot, so it stays true however the list is later reordered.
        (string body, int from, UndergroundComplex.LiftStop stop) = TheCageRide();
        IReadOnlyList<UndergroundComplex.Saying> sayings = UndergroundComplex.ArrivalSayings(
            body, from, stop.Level, new UndergroundComplex.ArrivalMemory(WasUnderground: true), stop, [],
            [CanteenTable.Chit(underAnotherName: false)]);

        UndergroundComplex.Saying chit = Assert.Single(
            sayings, s => s.Beat == UndergroundComplex.ArrivalBeat.ChitGate);
        Assert.Equal(CanteenTable.ChitGateLine, chit.Text);

        PulseSlot slot = PulseSlot.Empty;
        foreach (UndergroundComplex.Saying s in sayings)
        {
            slot = slot.Write(s.Text, s.Rank, 0.0);
        }
        Assert.NotEqual(UndergroundComplex.PressurisedLine, slot.Message);
    }

    [Fact]
    public void TheRideAsksTHESTOPWhichPaperOpenedItAndNotTheWallet()
    {
        // Core decides, the razor draws (#600). The arrival reads the pressed BUTTON's own verdict — a rule
        // that re-asked "am I carrying a chit?" would narrate the gate on every trip a chit-carrying captain
        // took, including the ones the gate had nothing to do with. Asserted as behaviour: the same captain,
        // the same paper, an ordinary trip inside one band, and the gate has nothing to say.
        (string body, int from, UndergroundComplex.LiftStop stop) = TheCageRide();
        Satchel.Item[] carried = [CanteenTable.Chit(underAnotherName: false)];

        foreach (UndergroundComplex.LiftStop ordinary in
                 UndergroundComplex.LiftPanel(body, from, [], carried))
        {
            if (ordinary.OpenedByChit || ordinary.IsCurrent || ordinary.Refusal is not null
                || ordinary.Level >= 0)
            {
                continue;
            }

            Assert.DoesNotContain(
                UndergroundComplex.ArrivalSayings(
                    body, from, ordinary.Level,
                    new UndergroundComplex.ArrivalMemory(WasUnderground: true), ordinary, [], carried),
                s => s.Beat == UndergroundComplex.ArrivalBeat.ChitGate);
        }

        // …and the ride hands the pressed stop over rather than re-deriving which trip it was.
        Assert.True(TheRide().Contains("via, AuthorityCardIds(), _satchel", StringComparison.Ordinal),
            "the arrival no longer passes the pressed stop to the composition — whatever it reads instead " +
            "is a second opinion about which gate the trip crossed (#752).");
        Assert.True(stop.OpenedByChit);
    }

    [Fact]
    public void TheGistIsFiledWithTheBeatAndBOTHHappenOnce()
    {
        // (d) · The book gets what the paper turned out to be worth, once. A beat that files on every trip
        // turns the field book into a lift log.
        string ride = TheRide();
        Assert.True(ride.Contains("CanteenTable.ChitGateGist", StringComparison.Ordinal),
            "the arrival never files the chit's gist — the book keeps the beat and not the reading (#752).");

        string beat = Between(
            ride, "case UndergroundComplex.ArrivalBeat.ChitGate:", "case UndergroundComplex.ArrivalBeat.Seam:");
        Assert.True(beat.Contains("ex.ChitGateBeatShown = true", StringComparison.Ordinal),
            "the chit's arrival never latches ChitGateBeatShown — it would say itself and file itself on " +
            "every ride down the cage (#752).");
        Assert.True(
            ride.Contains("ChitBeatSpent: ex.ChitGateBeatShown", StringComparison.Ordinal),
            "the composition is never told whether the beat has been spent: a flag that is written and " +
            "never read is not a once (#752/#693).");
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
