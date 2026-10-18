using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Four small lies that were told in the dark, each pinned by the world that would have told it: an unlisted
/// band that rolled the head office, a code paper dealt for a shaft the panel will never draw, an arrival
/// beat naming a card the captain is not holding, and a rehearsal whose last sample was not the park.
/// Each guard was watched going RED with its fix reverted.
/// </summary>
public sealed class TheRoundOfFourFindingsTests
{
    private static IEnumerable<string> Sites(int n) => Enumerable.Range(0, n).Select(i => $"round-four-{i}");

    [Fact]
    public void AnUnlistedBandNeverRollsTheHeadOffice()
    {
        int unlisted = 0;
        foreach (string body in Sites(3000).Where(b => !UndergroundComplex.IsHeadOffice(b)))
        {
            if (!UndergroundComplex.HasUnlistedBand(body))
            {
                continue;
            }
            unlisted++;
            int level = UndergroundComplex.BandTop(UndergroundComplex.UnlistedBandOf(body));
            UndergroundComplex.Kind kind = UndergroundComplex.KindOn(body, level);
            Assert.NotEqual(UndergroundComplex.Kind.HeadOffice, kind);
            Assert.NotEqual(UndergroundComplex.KindFor(body), kind);
        }
        Assert.True(unlisted > 100, $"the sweep found only {unlisted} unlisted bands; the guard judged a thin world.");
    }

    [Fact]
    public void NoCodePaperIsDealtWhereBandOneIsTheHiddenOne()
    {
        int hidden = 0;
        foreach (string body in Sites(6000).Where(b => !UndergroundComplex.IsHeadOffice(b)))
        {
            bool bandOneIsHidden = UndergroundComplex.HasUnlistedBand(body)
                && UndergroundComplex.UnlistedBandOf(body) == UndergroundComplex.LiftCode.PadBand;
            if (bandOneIsHidden)
            {
                hidden++;
                Assert.Null(UndergroundComplex.LiftCode.PaperRoomFor(body));
            }
            if (UndergroundComplex.LiftCode.PaperRoomFor(body) is not null)
            {
                Assert.False(UndergroundComplex.IsUnlisted(body, UndergroundComplex.BandTop(UndergroundComplex.LiftCode.PadBand)));
            }
        }
        Assert.True(hidden > 0, "the sweep never met a site whose band 1 is the hidden band.");
    }

    [Fact]
    public void TheArrivalBeatNamesTheCardTheGateActuallyRead()
    {
        List<string> gated = [.. Sites(300).Where(b => UndergroundComplex.NextShaftBelow(b, -1) == 1)];
        string? a = null, b2 = null;
        foreach (IGrouping<string, string> outfit in gated.GroupBy(x => SiteOperator.Of(x).Id))
        {
            if (outfit.Count() >= 2) { a = outfit.ElementAt(0); b2 = outfit.ElementAt(1); break; }
        }
        Assert.NotNull(a);
        var held = new UndergroundComplex.AuthorityCard(a!, 1);
        var own = new UndergroundComplex.AuthorityCard(b2!, 1);

        HashSet<string> wallet = [held.Id];
        UndergroundComplex.LiftStop stop = Assert.Single(
            UndergroundComplex.LiftPanel(b2!, -1, wallet), s => UndergroundComplex.BandOf(s.Level) == 1 && s.Level < 0);
        Assert.Null(stop.Refusal);

        UndergroundComplex.AuthorityCard? opened = UndergroundComplex.GateOpenedByRidingTo(b2!, -1, stop.Level, wallet);
        Assert.Equal(held, opened);

        var said = UndergroundComplex.ArrivalSayings(
            b2!, -1, stop.Level, new UndergroundComplex.ArrivalMemory(WasUnderground: true), stop, wallet);
        UndergroundComplex.Saying beat = Assert.Single(said, s => s.Beat == UndergroundComplex.ArrivalBeat.CardAccepted);
        Assert.Equal(UndergroundComplex.StandingHonouredLine(held), beat.Text);
        Assert.Equal(held, beat.Gate);

        // The site's own card keeps its own sentence.
        HashSet<string> ownWallet = [own.Id];
        UndergroundComplex.LiftStop ownStop = Assert.Single(
            UndergroundComplex.LiftPanel(b2!, -1, ownWallet), s => UndergroundComplex.BandOf(s.Level) == 1 && s.Level < 0);
        Assert.Equal(own, UndergroundComplex.GateOpenedByRidingTo(b2!, -1, ownStop.Level, ownWallet));
        var ownSaid = UndergroundComplex.ArrivalSayings(
            b2!, -1, ownStop.Level, new UndergroundComplex.ArrivalMemory(WasUnderground: true), ownStop, ownWallet);
        Assert.Equal(UndergroundComplex.CardAcceptedLine(own),
            Assert.Single(ownSaid, s => s.Beat == UndergroundComplex.ArrivalBeat.CardAccepted).Text);
    }

    [Fact]
    public void ACappedPathStillEndsOnTheInsertionSample()
    {
        var eph = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        var sim = new Simulator(eph, timeStepSeconds: 60);
        Vector2d saturnVel = (eph.Position("saturn", 1.0) - eph.Position("saturn", -1.0)) / 2.0;
        Vector2d titanPos = eph.Position("titan", 0);
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d outward = (titanPos - saturnPos).Normalized();
        var ship = new ShipState(titanPos + outward * 2.4e9, saturnVel, 0);

        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(ship, eph, sim, "titan", 100000, capturePath: true);
        Assert.True(r.Deliverable, "the fixture must fly to a park.");
        Assert.Equal(4096, r.Path.Count);

        // path[^1] is the park, not the 4096th mid-approach sample: it sits inside the capture range's
        // inner reach, nowhere near the 3e9 m floor the approach began at.
        TrajectorySample last = r.Path[^1];
        double d = (last.Position - eph.Position("titan", last.SimTime)).Length;
        Assert.True(d < 1e9, $"the last sample is {d:E2} m from Titan: a mid-approach point, not the park.");
    }
}
