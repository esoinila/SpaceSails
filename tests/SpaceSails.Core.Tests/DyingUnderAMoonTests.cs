using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #609 · A DEATH UNDER A MOON IS NOT A DEATH ON ONE. Owner, having suffocated on B2 and been handed the
/// away-team card: <i>"now we have the suffocated on surface one :-D"</i>
///
/// <para>He was 150 m down in a poured corridor and the card told him about regolith, a suit and a long walk
/// back to the tube. Nothing in <see cref="DeathPlace"/> meant "underground", so every death in the Hive
/// fell through to the away team's — the sim knowing one thing and the SENTENCE reporting another, which is
/// a named bug class on this ground and the third card it has cost.</para>
///
/// <para>These pin the words, the picture and the causes, because prose is the one thing a geometry audit
/// can never check.</para>
/// </summary>
public sealed class DyingUnderAMoonTests
{
    private static readonly ulong[] Seeds = [1, 7, 42, 1009, 65537];

    [Fact]
    public void ItNeverBorrowsTheAwayTeamsWords()
    {
        // The bug, stated as the law it broke. A captain 150 m down has no regolith under them, no sky over
        // them, no suit-and-a-walk back to a tube — they have a corridor and a lift somebody has to call.
        string[] surfaceOnly = ["regolith", "the tube", "dust of {body}", "sky", "horizon", "shuttle"];

        foreach (ulong seed in Seeds)
        {
            foreach (DeathCause cause in Enum.GetValues<DeathCause>())
            {
                if (!DeathNarration.CanHappen(cause, DeathPlace.Underground))
                {
                    continue;
                }

                string line = DeathNarration.Line(cause, DeathPlace.Underground, seed, "Luna");
                foreach (string surface in surfaceOnly)
                {
                    Assert.DoesNotContain(surface, line, StringComparison.OrdinalIgnoreCase);
                }

                // And it is not silently the generic pool either — it says where it actually is.
                Assert.NotEqual(DeathNarration.Line(cause, seed, "Luna"), line);
            }
        }
    }

    [Fact]
    public void ItGetsItsOwnPICTURE()
    {
        // Owner asked for it by name: "let's make a died in a secret lab photo also :-D". The red-shirt card
        // is a figure on regolith with a sky over it, which is the one thing this death does not have.
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            string art = DeathNarration.ArtFile(cause, DeathPlace.Underground);
            Assert.Equal("death-underground.jpg", art);
            Assert.NotEqual(DeathNarration.ArtFile(cause, DeathPlace.LandingParty), art);
        }
    }

    [Fact]
    public void NoCollectorEverReachesYouDownThere()
    {
        // Same reasoning that already keeps them off a derelict (#574): a collector is a person who came for
        // you, and nobody is riding a lift 150 m down a shaft they would have to call a car to use.
        Assert.False(DeathNarration.CanHappen(DeathCause.Collector, DeathPlace.Underground));

        // Suffocation, on the other hand, is the death this place is FOR.
        Assert.True(DeathNarration.CanHappen(DeathCause.Suffocated, DeathPlace.Underground));
    }

    [Fact]
    public void TheTailDoesNotPromiseGroundThatIsNotThere()
    {
        // "The ground keeps what it is given" is a surface sentence about a body on regolith. Down here the
        // ground is a slab somebody poured, and the truth is worse and quieter.
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            string tail = DeathNarration.Tail(cause, DeathPlace.Underground);
            Assert.DoesNotContain("ground keeps", tail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ItStillExplainsNothing()
    {
        // Canon, at a moment when a card is allowed to be dramatic and would most like to be explanatory.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var text = new List<string>();
        foreach (ulong seed in Seeds)
        {
            foreach (DeathCause cause in Enum.GetValues<DeathCause>())
            {
                if (DeathNarration.CanHappen(cause, DeathPlace.Underground))
                {
                    text.Add(DeathNarration.Line(cause, DeathPlace.Underground, seed, "Luna"));
                    text.Add(DeathNarration.Tail(cause, DeathPlace.Underground));
                }
            }
        }

        foreach (string line in text)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TheDeadAirCardDoesTheARITHMETICForYou()
    {
        // Owner: "there should be a warning or something :-D" / "maybe pop-up about you have air or you are
        // in vacuum type ... it is vital info" / "on surface there are emergency shelters :-D"
        //
        // A warning that leaves the captain to work out their own margin is a warning delivered too late. So
        // the card names the rule, names the nearest breathable floor, and states the tank.
        foreach (int level in new[] { -2, -3, -6, -11, -23 })
        {
            string card = UndergroundComplex.VacuumCard(level, airSeconds: 754);

            // The rule itself, in words, because it is the only thing down here that can kill you.
            Assert.Contains("TOP FLOOR OF EVERY SHAFT BAND", card, StringComparison.Ordinal);

            // Where you are, and where the air is.
            Assert.Contains($"{UndergroundComplex.MetresDown(level):F0} m", card, StringComparison.Ordinal);
            int refuge = UndergroundComplex.BandTop(UndergroundComplex.BandOf(level));
            Assert.Contains(UndergroundComplex.NameOf(refuge), card, StringComparison.Ordinal);
            Assert.True(UndergroundComplex.HoldsPressure(refuge),
                $"B{-level}: the card points at B{-refuge} as the nearest air and it does not hold pressure.");

            // And the tank, so the margin is a number rather than a feeling.
            Assert.Contains("12 min", card, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheDeadAirCardIsHONESTAboutThereBeingNoShelter()
    {
        // The owner's comparison is the whole point: "on surface there are emergency shelters". Down here
        // there are none, and the card must say so rather than let a captain go looking for one — a search
        // for a refuge that does not exist is the most expensive possible way to spend a tank.
        string card = UndergroundComplex.VacuumCard(-2, 300);
        Assert.Contains("no shelters", card, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnAPressurisedFloorThereIsNothingToWarnAbout()
    {
        // The card is for the first DEAD floor. A band top is a refuge and gets the relief line instead —
        // that beat is the lie that makes the rest of the building work (#585) and must not be stepped on.
        foreach (int level in new[] { -1, -5, -9, -13 })
        {
            Assert.True(UndergroundComplex.HoldsPressure(level));
        }
        foreach (int level in new[] { -2, -3, -4, -6, -7, -8 })
        {
            Assert.False(UndergroundComplex.HoldsPressure(level));
        }
    }
}
