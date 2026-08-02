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
                    || UndergroundComplex.IsUnlisted(body, UndergroundComplex.BandTop(next)))
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
