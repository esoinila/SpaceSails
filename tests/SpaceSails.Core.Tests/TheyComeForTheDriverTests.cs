using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #583 · THE REPO BOAT. Owner, 2026-08-01: <i>"but the heat should not target the ship when the player is
/// not on it but only target the captain... we could have some other shuttle land near ours on some sites ...
/// that would be the heat when we are on land or at a ship looting it"</i> — and the design in one line,
/// <b><i>"FBI does not arrest cars ... they look for the driver"</i></b>.
///
/// <para>#580 stopped the wolves catching an empty hull, which was correct and left heat meaning nothing
/// during the part of the game the captain actually occupies. These pin the other half.</para>
/// </summary>
public sealed class TheyComeForTheDriverTests
{
    [Fact]
    public void ACOLDCaptainIsNeverFollowedDown()
    {
        // The floor the whole feature rests on, and the one that must never drift: nobody watches a moon.
        // Without heat there is no writ, so there is no boat — the surface is between you and the Old Ones
        // and nothing else. If this ever goes true at heat 0 the game has quietly started punishing walking.
        for (ulong seed = 0; seed < 400; seed++)
        {
            Assert.False(CollectorLanding.WillFollowYouDown(0, seed));
            Assert.False(CollectorLanding.WillFollowYouDown(-3, seed));
        }
    }

    [Fact]
    public void AHotCaptainIsSOMETIMESFollowedDownAndTheOddsClimbWithTheGauge()
    {
        // Never certain, even at the top — a landing guaranteed at heat 3 makes every hot excursion the same
        // scene, where a landing that is LIKELY makes the walk down a wager. That is what the gauge is for.
        int[] followed = new int[5];
        for (ulong seed = 0; seed < 2_000; seed++)
        {
            for (int heat = 1; heat <= 4; heat++)
            {
                if (CollectorLanding.WillFollowYouDown(heat, seed))
                {
                    followed[heat]++;
                }
            }
        }

        for (int heat = 1; heat <= 4; heat++)
        {
            Assert.True(followed[heat] > 0, $"heat {heat}: they never come — the feature is dead there.");
            Assert.True(followed[heat] < 2_000, $"heat {heat}: they ALWAYS come — no wager left in it.");
        }

        Assert.True(followed[1] < followed[2], "heat 2 must be worse than heat 1.");
        Assert.True(followed[2] < followed[4], "heat 4 must be worse than heat 2.");
    }

    [Fact]
    public void TheBoatArrivesMIDMISSIONNeverAtTheHatch()
    {
        // Owner's own framing, from days before this existed: "it has to be mid mission (so that you don't
        // just bring more air) and time sensitive". Arriving at the door would be a wall in front of the
        // airlock and the player would learn to check the gauge before landing and never see the scene.
        for (ulong seed = 0; seed < 500; seed++)
        {
            for (int heat = 1; heat <= 4; heat++)
            {
                double eta = CollectorLanding.ArrivesAfterSeconds(heat, seed);
                Assert.True(eta >= 90, $"heat {heat}/seed {seed}: a boat at {eta:F0}s is at the hatch.");
            }
        }
    }

    [Fact]
    public void AHotterCaptainIsFoundFaster()
    {
        // They were being watched for.
        for (ulong seed = 0; seed < 200; seed++)
        {
            Assert.True(
                CollectorLanding.ArrivesAfterSeconds(4, seed) <= CollectorLanding.ArrivesAfterSeconds(1, seed),
                $"seed {seed}: heat 4 is found no faster than heat 1.");
        }
    }

    [Fact]
    public void TheyAreACrewNotAnArmy()
    {
        // The threat is that they are between you and the tube and do not tire, never that they outnumber
        // the regolith. Also the hard ceiling the client's droid buffer is sized against.
        for (int heat = 1; heat <= 12; heat++)
        {
            int party = CollectorLanding.PartySize(heat);
            Assert.InRange(party, 1, 4);
        }
    }

    [Fact]
    public void TheBoatSetsDownBesideTheTubeAndNeverOnIt()
    {
        // Near enough to be between you and the way home; never on the hatch, because a boat parked on the
        // door would end the excursion by geometry instead of by decision.
        const double tubeX = -7.0;
        for (ulong seed = 0; seed < 200; seed++)
        {
            double x = CollectorLanding.SetsDownX(tubeX, seed);
            Assert.Equal(CollectorLanding.SetsDownOffsetDu, Math.Abs(x - tubeX), 6);
        }

        // And not always the same side, or it is one picture forever.
        var sides = new HashSet<bool>();
        for (ulong seed = 0; seed < 50; seed++)
        {
            sides.Add(CollectorLanding.SetsDownX(tubeX, seed) < tubeX);
        }
        Assert.Equal(2, sides.Count);
    }

    [Fact]
    public void TheyWalkFasterThanAStrollAndSlowerThanASprint()
    {
        // The pace IS the play: you can open the gap by running and you lose it the moment you stop to work.
        // If they ever outpace a running captain the only counter left is luck.
        Assert.True(CollectorLanding.PaceDuPerSecond > SuitAir.Breathing.Walking * 3,
            "a crew slower than a stroll is scenery.");
        Assert.True(CollectorLanding.PaceDuPerSecond < 8.0,
            "a crew that outruns a sprint leaves the player nothing to do.");
    }

    [Fact]
    public void EveryLineNamesTHEMAndNeverTheOldOnes()
    {
        // The whole point of a second hostile is that it is a DIFFERENT one. If the prose slips into Reever
        // language the player learns nothing from the words and the amber marks are just red marks.
        string callsign = CollectorLanding.CallsignFor(7);
        string[] said =
        [
            CollectorLanding.ArrivalLine(callsign),
            CollectorLanding.HailLine,
            CollectorLanding.ClosingLine,
            CollectorLanding.ContactLine(callsign),
            CollectorLanding.EscapedLine,
            CollectorLanding.ShelterIsNotSanctuaryLine,
        ];

        foreach (string line in said)
        {
            Assert.DoesNotContain("Old One", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Reever", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("shamble", line, StringComparison.OrdinalIgnoreCase);
            Assert.True(line.Length > 40, "a line this short cannot carry a scene.");
        }
    }

    [Fact]
    public void TheArrivalLineSaysOUTLOUDThatTheyFlewPastTheShip()
    {
        // This is the sentence that teaches the whole rule the owner asked for — that the ship was never the
        // target. If it ever stops saying so, the player has no way to learn it except by dying.
        string line = CollectorLanding.ArrivalLine(CollectorLanding.CallsignFor(3));
        Assert.Contains("for you", line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AShelterIsSaidPLAINLYNotToBeSanctuary()
    {
        // A refuge that silently fails to be one is the worst thing a survival game can do. The shelter stops
        // vacuum and the air drain (#573) and stops NOTHING about a writ — so the game says it in words the
        // first time, rather than letting the player find out by being taken inside one.
        Assert.Contains("wait", CollectorLanding.ShelterIsNotSanctuaryLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("air", CollectorLanding.ShelterIsNotSanctuaryLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EverythingHereIsDeterministic()
    {
        // Determinism is law in Core: same seed, same trip, every reload.
        for (ulong seed = 0; seed < 100; seed++)
        {
            Assert.Equal(
                CollectorLanding.WillFollowYouDown(2, seed), CollectorLanding.WillFollowYouDown(2, seed));
            Assert.Equal(
                CollectorLanding.ArrivesAfterSeconds(2, seed), CollectorLanding.ArrivesAfterSeconds(2, seed));
            Assert.Equal(CollectorLanding.CallsignFor(seed), CollectorLanding.CallsignFor(seed));
        }
    }
}

/// <summary>#583 · The death card for a captain taken on foot. The owner has already caught this class of
/// bug twice — <i>"there was no collector I was on site ... how can it say anything like that"</i>, and
/// <i>"the debt collector deaths should also only happen in those situations never in any other"</i>. Now
/// that a collector CAN be on a moon, the card has to know the difference between the two deaths rather than
/// simply being allowed to say the old thing in a new place.</summary>
public sealed class TakenOnFootTests
{
    [Fact]
    public void ACollectorCanNowTakeYouOnTheGroundButStillNeverOnAWreck()
    {
        Assert.True(DeathNarration.CanHappen(DeathCause.Collector, DeathPlace.OwnShip));
        Assert.True(DeathNarration.CanHappen(DeathCause.Collector, DeathPlace.LandingParty));

        // Boarding a wreck they are already inside is a different arrival and is not built (#584). Until it
        // is, a card must never claim it happened.
        Assert.False(DeathNarration.CanHappen(DeathCause.Collector, DeathPlace.Derelict));
    }

    [Fact]
    public void FlyingIntoAMOONIsStillOnlySomethingYouCanDoFromTheControls()
    {
        // The other half of the old rule, which must survive the change: you have to be at the helm to fly a
        // ship into a planet.
        Assert.True(DeathNarration.CanHappen(DeathCause.Impact, DeathPlace.OwnShip));
        Assert.False(DeathNarration.CanHappen(DeathCause.Impact, DeathPlace.LandingParty));
        Assert.False(DeathNarration.CanHappen(DeathCause.Impact, DeathPlace.Derelict));
    }

    [Fact]
    public void AGroundCatchNeverBorrowsTheSHIPSWords()
    {
        // #574's lesson, applied before it can bite: the ship pool is all boarding volleys and last stands at
        // the controls, and reading that over a captain walked down on regolith is the same borrowed prose.
        for (ulong seed = 0; seed < 60; seed++)
        {
            string line = DeathNarration.Line(
                DeathCause.Collector, DeathPlace.LandingParty, seed, "Miranda");

            Assert.DoesNotContain("volley", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("boarding", line, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Miranda", line);
        }
    }

    [Fact]
    public void AShipCatchSTILLGetsTheShipsWords()
    {
        // The new branch must not have stolen the old death's voice on the way past.
        bool sawAVolley = false;
        for (ulong seed = 0; seed < 60; seed++)
        {
            string line = DeathNarration.Line(
                DeathCause.Collector, DeathPlace.OwnShip, seed, "Miranda");
            sawAVolley |= line.Contains("volley", StringComparison.OrdinalIgnoreCase);
        }
        Assert.True(sawAVolley, "the ship's collector death lost its own language.");
    }

    [Fact]
    public void ACaptainTakenOnFootDiesInARedShirt()
    {
        // Owner: "the image should not show a ocean sailship in space", and "(can not resist the red shirt
        // always die nod to Star Trek the OG series ) :-D". A captain taken on a moon died on the GROUND in
        // a suit, whoever's hand it was — the place decides the picture, the cause decides the words.
        Assert.Equal("death-landing-party.jpg",
            DeathNarration.ArtFile(DeathCause.Collector, DeathPlace.LandingParty));

        // And a catch at the controls keeps the gun-camera still.
        Assert.Equal("busted-freeze-frame.jpg",
            DeathNarration.ArtFile(DeathCause.Collector, DeathPlace.OwnShip));
    }
}
