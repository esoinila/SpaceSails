using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #600 · THE CAR ONLY WENT DOWN. Owner, on B1: <i>"looks like the elevator only takes me down... how do I
/// get back to the surface with it :-D Am I marooned in a secret lab underground now :-D ?"</i>
///
/// <para>He was not, but only by luck: the lift's single action always descended, and the car returned to
/// the surface solely when pressed at the bottom of the band. Getting out of B2 on a twenty-floor site meant
/// riding eighteen floors DEEPER first, through dead air, on the tank.</para>
///
/// <para><b>Why nothing caught it.</b> <c>YouCanWalkTheHiveTests</c> floods every floor with A\* and proves
/// the lift console is reachable — it has never asked what pressing it DOES. A reachability audit walks
/// geometry; this was a state machine. These tests are that missing half, and they are the reason the button
/// list lives in Core instead of in a razor file.</para>
/// </summary>
public sealed class TheLiftPanelTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static IReadOnlyList<UndergroundComplex.LiftStop> Panel(string body, int level, params string[] cards) =>
        UndergroundComplex.LiftPanel(body, level, cards);

    [Fact]
    public void FromEVERYFloorTheSurfaceIsOneButtonAway()
    {
        // THE bug, stated as the law it broke. From anywhere in any facility, leaving must never require
        // travelling further in first.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                IReadOnlyList<UndergroundComplex.LiftStop> panel = Panel(body, level);

                UndergroundComplex.LiftStop surface = Assert.Single(panel, s => s.Level == 0);
                Assert.Null(surface.Refusal);
                Assert.False(surface.IsCurrent);
                Assert.Equal("SURFACE", surface.Name);
            }
        }
    }

    [Fact]
    public void TheWayOutIsNeverDOWNWARDS()
    {
        // The specific cruelty of the old behaviour: the only working button took you further from the air.
        // Every floor must offer at least one destination that is strictly SHALLOWER than where you stand.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                bool up = Panel(body, level).Any(s => s.Refusal is null && !s.IsCurrent && s.Level > level);
                Assert.True(up, $"{body} B{-level}: nothing on the panel goes UP.");
            }
        }
    }

    [Fact]
    public void TheCarServesItsOwnBandAndNoFurther()
    {
        // #585's law survives the panel: one car, one band. The floors it offers are its own band's, plus
        // the surface, plus at most ONE gated button for the shaft below.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                int band = UndergroundComplex.BandOf(level);
                foreach (UndergroundComplex.LiftStop stop in Panel(body, level).Where(s => s.Level < 0))
                {
                    int stopBand = UndergroundComplex.BandOf(stop.Level);
                    Assert.True(stopBand == band || stopBand == band + 1,
                        $"{body} B{-level}: the panel offers B{-stop.Level}, which is neither this car's " +
                        "band nor the one shaft below it.");

                    // Anything outside this car's own band is the OTHER shaft, and is a single entry.
                    if (stopBand != band)
                    {
                        Assert.Equal(UndergroundComplex.BandTop(band + 1), stop.Level);
                    }
                }
            }
        }
    }

    [Fact]
    public void EveryFloorItOffersIsAFloorThatEXISTS()
    {
        // A button to a floor nobody dug would ride the captain into rock — the gap under a site whose
        // listed depth stops mid-band (#592) is exactly such a place.
        foreach (string body in Bodies)
        {
            var real = UndergroundComplex.FloorsOf(body).ToHashSet();
            foreach (int level in real)
            {
                foreach (UndergroundComplex.LiftStop stop in Panel(body, level, $"{body}#1", $"{body}#2",
                             $"{body}#3", $"{body}#4", $"{body}#5", $"{body}#6"))
                {
                    if (stop.Level < 0)
                    {
                        Assert.Contains(stop.Level, real);
                    }
                }
            }
        }
    }

    [Fact]
    public void ThePanelNeverOffersTheFloorItIsSTANDINGOn()
    {
        // It is shown — you need to know where you are — but it is never a destination.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.LiftStop here =
                    Assert.Single(Panel(body, level), s => s.IsCurrent);
                Assert.Equal(level, here.Level);
            }
        }
    }

    [Fact]
    public void TheShaftBelowIsPRESENTAndRefusesRatherThanBeingAbsent()
    {
        // #590 · a button that is there and says why beats a button that is not there: from the captain's
        // side an absent control and a broken one look identical, and this ground has shipped that once.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                int next = UndergroundComplex.BandOf(level) + 1;
                if (!UndergroundComplex.SiteHasBand(body, next))
                {
                    continue;
                }
                // The unlisted band is the deliberate exception — see the silence test below.
                if (UndergroundComplex.IsUnlisted(body, UndergroundComplex.BandTop(next)))
                {
                    continue;
                }

                // #411 · And so is the head office, for the opposite reason. A branch office's card opens
                // exactly one band; the head office asks for nothing, on any floor, because a hull that is
                // on the board is expected and the building has never had another kind of visitor. That
                // half is asserted — with this half beside it, so neither can quietly become the other —
                // in TheHeadOfficeTests.TheHeadOfficeLiftNeverAsksForACardAndABranchOfficeAlwaysDoes.
                if (UndergroundComplex.IsHeadOffice(body))
                {
                    continue;
                }

                UndergroundComplex.LiftStop gated = Assert.Single(
                    Panel(body, level), s => UndergroundComplex.BandOf(s.Level) == next && s.Level < 0);
                Assert.NotNull(gated.Refusal);

                // And with the card in hand it opens.
                UndergroundComplex.LiftStop opened = Assert.Single(
                    Panel(body, level, new UndergroundComplex.AuthorityCard(body, next).Id),
                    s => UndergroundComplex.BandOf(s.Level) == next && s.Level < 0);
                Assert.Null(opened.Refusal);
            }
        }
    }

    [Fact]
    public void ThePanelKEEPSTheUnlistedBandsSecret()
    {
        // #592 · the hardest interaction in this feature. On the last floor the building admits to, the panel
        // must look EXACTLY like the panel at the true bottom of an ordinary site — because a refusal that
        // names a shaft would announce the secret in the one sentence it cannot survive.
        foreach (string body in Bodies.Where(UndergroundComplex.HasUnlistedBand))
        {
            int listedBottom = UndergroundComplex.DepthOf(body);
            int hidden = UndergroundComplex.UnlistedBandOf(body);

            // Carrying nothing: the panel says nothing at all about what is under it.
            IReadOnlyList<UndergroundComplex.LiftStop> quiet = Panel(body, listedBottom);
            Assert.DoesNotContain(quiet, s => UndergroundComplex.BandOf(s.Level) == hidden && s.Level < 0);
            foreach (UndergroundComplex.LiftStop stop in quiet)
            {
                Assert.Null(stop.Refusal);   // not even a sealed button to wonder about
            }

            // Carrying the card, it is simply there — you already know, so there is nothing left to keep.
            IReadOnlyList<UndergroundComplex.LiftStop> known =
                Panel(body, listedBottom, new UndergroundComplex.AuthorityCard(body, hidden).Id);
            UndergroundComplex.LiftStop way = Assert.Single(
                known, s => UndergroundComplex.BandOf(s.Level) == hidden && s.Level < 0);
            Assert.Null(way.Refusal);
            Assert.Equal(UndergroundComplex.BandTop(hidden), way.Level);
        }
    }

    [Fact]
    public void ANOTHERSitesCardOpensNothingHere()
    {
        // The wallet is not a skeleton key — the identity carries the body (#590).
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                int next = UndergroundComplex.BandOf(level) + 1;
                if (!UndergroundComplex.SiteHasBand(body, next)
                    || UndergroundComplex.IsUnlisted(body, UndergroundComplex.BandTop(next))
                    // #411 · the head office has no gate to be the wrong key for — see the test named above.
                    || UndergroundComplex.IsHeadOffice(body))
                {
                    continue;
                }
                UndergroundComplex.LiftStop stop = Assert.Single(
                    Panel(body, level, new UndergroundComplex.AuthorityCard("somewhere-else", next).Id),
                    s => UndergroundComplex.BandOf(s.Level) == next && s.Level < 0);
                Assert.NotNull(stop.Refusal);
            }
        }
    }

    /// <summary>Every floor a car can be pressed from, the surface included — the panel is offered there too,
    /// and a press from the shed is a press.</summary>
    private static IEnumerable<int> PressableFrom(string body) =>
        UndergroundComplex.FloorsOf(body).Append(0);

    [Fact]
    public void AGatedRowNAMESTheCardThatWillOpenItBeforeTheRide()
    {
        // #689 · the "needed" half of the story, told by the panel itself. Owner, having played the whole
        // loop: "It was locked until I got it ... there was no story point about it being needed or used."
        // The refusal already names cards (#590); this is its positive twin, and it is Core's to decide so
        // that the razor cannot invent a promise the gate will not keep.
        int gatesSeen = 0;
        foreach (string body in Bodies)
        {
            foreach (int level in PressableFrom(body))
            {
                int next = UndergroundComplex.BandOf(Math.Min(level, -1)) + 1;
                if (!UndergroundComplex.SiteHasBand(body, next))
                {
                    continue;
                }

                // #411 · The head office asks for nothing on any floor, so there is no gate here to promise
                // anything — not even while the captain is carrying this site's own card. The absence IS the
                // rank difference, and a row saying "the gate will read it" would spend it.
                var card = new UndergroundComplex.AuthorityCard(body, next);
                if (UndergroundComplex.IsHeadOffice(body))
                {
                    Assert.All(Panel(body, level, card.Id), s => Assert.Null(s.OpenedBy));
                    continue;
                }

                // Carrying nothing, or carrying another site's paper: the panel promises nothing.
                Assert.All(Panel(body, level), s => Assert.Null(s.OpenedBy));
                Assert.All(
                    Panel(body, level, new UndergroundComplex.AuthorityCard("somewhere-else", next).Id),
                    s => Assert.Null(s.OpenedBy));

                // Carrying it: exactly one row says so, it is the gated one, and it says the card's own
                // title rather than a sentence some other file wrote about it.
                UndergroundComplex.LiftStop gate =
                    Assert.Single(Panel(body, level, card.Id), s => s.OpenedBy is not null);
                Assert.Equal(UndergroundComplex.BandTop(next), gate.Level);
                Assert.Null(gate.Refusal);
                Assert.Equal(UndergroundComplex.CardTitle(card), gate.OpenedBy);
                gatesSeen++;
            }
        }

        // A world where nothing is gated would let every assertion above pass by never running: the fifth
        // named bug class, where the guard is right and the world cannot tell pass from fail.
        Assert.True(gatesSeen > 20, $"only {gatesSeen} gated rows were examined — this seed set has stopped " +
            "containing the case the test is about.");
    }

    [Fact]
    public void WhichGateARideGoesThroughIsAFactAboutTheSTOPAndNotAboutTheFloorPressedFrom()
    {
        // #689 · the beat used to be decided by `BandOf(min(Floor,-1)) + 1` — the band under the floor the
        // captain was STANDING on — which is a different question from the one that matters: does this trip
        // cross a gate, and did a card open it. Asked of the stop, the answer is the same from every floor
        // the button appears on, and it is NO wherever no card was read.
        int ridesSeen = 0;
        foreach (string body in Bodies)
        {
            foreach (int level in PressableFrom(body))
            {
                int next = UndergroundComplex.BandOf(Math.Min(level, -1)) + 1;
                if (!UndergroundComplex.SiteHasBand(body, next))
                {
                    continue;
                }
                int shaftHead = UndergroundComplex.BandTop(next);
                string cardId = new UndergroundComplex.AuthorityCard(body, next).Id;

                if (UndergroundComplex.IsHeadOffice(body))
                {
                    continue;   // its own fact, below — the head office answers a different law
                }

                // Holding the card, EVERY floor this button appears on earns it — the shallow press too.
                UndergroundComplex.AuthorityCard? opened =
                    UndergroundComplex.GateOpenedByRidingTo(body, level, shaftHead, [cardId]);
                Assert.True(opened is not null,
                    $"{body}: pressed from B{-level}, the ride to B{-shaftHead} goes through the gate and " +
                    "earns nothing. The beat belongs to the STOP; it must not depend on which floor of the " +
                    "band the captain happened to be standing on.");
                Assert.Equal(next, opened!.Value.Band);
                Assert.Equal(body, opened.Value.BodyId);

                // Not holding it, nothing opened — whatever the arithmetic about bands says. This is the
                // half that used to be true by accident: the client returned early on a refusal, so a
                // derivation that never looked at the wallet still behaved. A rule that is right only
                // because of where it is called from is a rule waiting for its second caller.
                Assert.True(
                    UndergroundComplex.GateOpenedByRidingTo(body, level, shaftHead, []) is null,
                    $"{body}: a captain carrying nothing at all rode from B{-level} to B{-shaftHead} and " +
                    "the game says a gate read their card.");

                // And an ordinary trip inside this car's own band is not a gate crossing.
                foreach (UndergroundComplex.LiftStop stop in
                         Panel(body, level, cardId).Where(s => s.Level != shaftHead))
                {
                    Assert.True(
                        UndergroundComplex.GateOpenedByRidingTo(body, level, stop.Level, [cardId]) is null,
                        $"{body}: the ordinary trip from B{-level} to {stop.Level} claims a gate.");
                }
                ridesSeen++;
            }
        }

        Assert.True(ridesSeen > 20, $"only {ridesSeen} gate crossings were examined — this seed set has " +
            "stopped containing the case the test is about.");
    }

    [Fact]
    public void TheHEADOfficeNeverNarratesACardBeingREADBecauseNothingReadsOne()
    {
        // #411 · The head office asks the captain for nothing on any floor: the gate is simply ABSENT, and
        // the absence is the rank difference. The beat's old arithmetic never asked whether a card was
        // involved — only which band was under the floor pressed from — so riding HQ's own shafts said "its
        // gate reads the card without hesitating" and named a countersignature the captain was not carrying.
        // A sentence about an object the sim never handed over: the third named bug class, in the prose.
        string hq = KaamosLore.IceMoonBodyId;
        foreach (int level in PressableFrom(hq))
        {
            int next = UndergroundComplex.BandOf(Math.Min(level, -1)) + 1;
            if (!UndergroundComplex.SiteHasBand(hq, next))
            {
                continue;
            }
            string cardId = new UndergroundComplex.AuthorityCard(hq, next).Id;
            Assert.True(
                UndergroundComplex.GateOpenedByRidingTo(
                    hq, level, UndergroundComplex.BandTop(next), [cardId]) is null,
                $"the head office narrated a gate reading a card on the ride from B{-level} to " +
                $"B{-UndergroundComplex.BandTop(next)}. There is no gate here to read one (#411).");
        }
    }

    [Fact]
    public void ThePanelSaysWhichFloorsCostAir()
    {
        // Depth is paid for in air, and the panel is where that is decided — so it must state it rather than
        // leave the captain to remember which floors hold pressure.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.LiftStop stop in Panel(body, level))
                {
                    Assert.Equal(
                        stop.Level >= 0 || UndergroundComplex.HoldsPressure(stop.Level),
                        stop.Pressurised);
                }
            }
        }
    }

    [Fact]
    public void ADepthIsAFACTAndTheSurfaceIsZero()
    {
        // #600 · owner: "we can use seriously large numbers there :-D ... or depths (in meters)". B4 is an
        // index; −76 m is where you are standing, and it is the number that makes the walk back up mean
        // something.
        Assert.Equal(0, UndergroundComplex.MetresDown(0));
        Assert.Equal(UndergroundComplex.OverburdenMetres, UndergroundComplex.MetresDown(-1));
        Assert.Equal("SURFACE", UndergroundComplex.DepthPaint(0));

        // Strictly increasing with depth, and deep enough at the bottom to be worth painting on a wall.
        double prev = UndergroundComplex.MetresDown(-1);
        for (int level = -2; level >= UndergroundComplex.DeepestPossibleFloor; level--)
        {
            double here = UndergroundComplex.MetresDown(level);
            Assert.True(here > prev, $"B{-level} is not deeper than B{-level - 1}.");
            prev = here;
        }
        Assert.True(UndergroundComplex.MetresDown(UndergroundComplex.DeepestPossibleFloor) > 300,
            "the deepest site in the game is less than 300 m down — that is not a hole worth telling people about.");

        // The paint reads as a depth, not as a floor index.
        Assert.Contains(" m", UndergroundComplex.DepthPaint(-4), StringComparison.Ordinal);
    }
}
