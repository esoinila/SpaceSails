using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #614 · THE ITEMS THAT EARN A CARD. Owner: <i>"we could have gen-AI images of plotwise important items…
/// maybe they say something about what door they open."</i>
///
/// <para>The interesting half is the second one, and it is a trap. An item that names its lock is a quest
/// marker, and this facility is built on the opposite law — so these tests are mostly about what the cards
/// are NOT allowed to say.</para>
/// </summary>
public class TheThingsWorthLookingAtTests
{
    private const string Here = "luna";

    [Fact]
    public void MostOfWhatYouCarryIsOrdinary_AndGetsNoCard()
    {
        // A game where every object earns a full-screen card has no objects that matter. Paper has its own
        // reader (#603) and a file on somebody is leverage, not a display piece.
        Assert.Null(CarriedObject.Card(new Satchel.Item(Satchel.Kind.Paper, "hive:luna:-3:2"), Here));
        Assert.Null(CarriedObject.Card(new Satchel.Item(Satchel.Kind.Dirt, "hive:luna:-3:2"), Here));

        // Issue ball is the round you always have. It is not plot.
        Assert.Null(CarriedObject.Card(new Satchel.Item(Satchel.Kind.Rounds, Ammunition.Issue.Id, 6), Here));

        // And the two-stage IS.
        Assert.NotNull(CarriedObject.Card(new Satchel.Item(Satchel.Kind.Rounds, Ammunition.LabTwoStage.Id, 4), Here));
    }

    [Fact]
    public void AnAuthorityCardSaysWhichSHAFTItRuns_AndNeverWhichMOON()
    {
        // THE WHOLE RULE, stated once. "Says what door it opens" was the ask; naming the moon would be a
        // quest marker and would hand the captain the inference the entire Hive is arranged around.
        //
        // The card may say: which shaft, of what kind of building, and whether it is this building.
        // The card may NOT say: where that building is, or anything a tracker could act on.
        string[] elsewhere = ["ganymede", "titan", "europa", "callisto", "triton"];

        foreach (string moon in elsewhere)
        {
            for (int band = 0; band < 4; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(moon, band);
                string runs = CarriedObject.WhatItRuns(card, Here);

                // It states the shaft, in the human numbering the panel and the card title both use.
                Assert.Contains($"shaft {band + 1}", runs, StringComparison.OrdinalIgnoreCase);

                // It never names the moon it belongs to — not by id, and the id is the only handle we have.
                Assert.DoesNotContain(moon, runs, StringComparison.OrdinalIgnoreCase);

                // And it is explicit that this is somewhere else, which is the line that turns a wallet of
                // foreign authorities (#613) into a reason to keep flying rather than an inventory oddity.
                Assert.Contains("not this one", runs, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void ACardForTHISBuildingSaysSo_BecauseThatIsTheOneThingWorthKnowingAtAGate()
    {
        var mine = new UndergroundComplex.AuthorityCard(Here, 1);
        string runs = CarriedObject.WhatItRuns(mine, Here);

        Assert.Contains("shaft 2", runs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("this building", runs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not this one", runs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoCardEverExplainsTheOldOnes()
    {
        // §13.8, and it holds hardest on the collar — the most tempting object in the game to explain them
        // with. Canon: the Old Ones are FAILED RESTORES, never stated in a card, never confirmed by a sensor.
        string[] stories =
        [
            CarriedObject.CollarStory,
            CarriedObject.PenetratorStory,
            CarriedObject.WhatItRuns(new UndergroundComplex.AuthorityCard("ganymede", 2), Here),
            UndergroundComplex.HaulLine(UndergroundComplex.Haul.Relic, Here, -20, 1),
        ];

        string[] forbidden =
        [
            "reever", "old one", "old ones", "restore", "backup", "clone", "subject",
            "creature", "specimen", "experiment", "kaamos", "minister",
        ];

        foreach (string story in stories)
        {
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, story, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TheCollarIsOnlyAMeasurement_AndTheGameNeverSaysWhatWoreIt()
    {
        // Owner: "kind of horror theme in a Lovecraft way ... like finding massive collar designed for
        // Cthulhus neck :D". Everything frightening about it is arithmetic — the size, and the WEAR.
        Assert.Contains("polish", CarriedObject.CollarStory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pallet", CarriedObject.CollarStory, StringComparison.OrdinalIgnoreCase);

        // It is not a thing you can lift, and the card must not pretend otherwise — what the captain has is
        // the record of it. A satchel that claimed to contain a three-metre alloy band would be the sim
        // saying one thing while the sentence said another, which is this repo's third named bug class.
        Assert.Contains("cannot carry it", CarriedObject.CollarStory, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheRelicRoomEXISTSOnEverySiteThatHasOne_AndOnNoSiteThatDoesNot()
    {
        // THE GUARD THAT MATTERS. A designated room is only a fix for the silent-absence bug if the room it
        // designates is actually generated — the Key room precedent (#592) exists precisely because a
        // seeded one-in-N object can be missing from a world FOREVER with every test still green.
        //
        // So: walk real sites, build the real floor, and check the index is in range.
        SurfaceLayout.Field field = SurfaceLayout.DefaultField;
        int withRelic = 0;

        foreach (string body in Bodies())
        {
            (int Level, int RoomIndex)? spot = UndergroundComplex.RelicRoomFor(body);

            if (!UndergroundComplex.HasUnlistedBand(body))
            {
                Assert.Null(spot);
                continue;
            }

            Assert.NotNull(spot);
            (int level, int room) = spot!.Value;
            withRelic++;

            // It is on the deepest floor of the band nobody listed — the payoff for reaching somewhere you
            // were not supposed to be able to reach.
            Assert.Equal(UndergroundComplex.TrueDepthOf(body), level);
            Assert.True(UndergroundComplex.IsUnlisted(body, level),
                $"{body}: the relic is on {level}, which is a floor the building admits to");

            // And the room is REAL — built from the SAME field the rest of the Hive suite uses.
            //
            // This assertion earned its keep twice over, and the second time it was about the test rather
            // than the game: written against an invented 78-du-wide field it reported zero rooms on every
            // floor of every site, listed and unlisted alike. A guard that fails on a field the game never
            // generates is not evidence of a bug, it is evidence of a bad harness — the same lesson the
            // client A* audit taught (a reachability test is only as honest as both its endpoints), and the
            // reason DefaultField exists.
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, field);
            Assert.True(floor.RoomCentres.Count > 0, $"{body}: the deepest hidden floor has no rooms at all");
            Assert.InRange(room, 0, floor.RoomCentres.Count - 1);

            // It does not collide with the way down — a room cannot hold two designated things.
            Assert.NotEqual(UndergroundComplex.KeyRoomFor(body), spot);

            // And searching it really does yield the relic rather than a roll.
            Assert.Equal(UndergroundComplex.Haul.Relic, UndergroundComplex.InRoom(body, level, room));
        }

        Assert.True(withRelic > 0, "no generated site had an unlisted band — the test proves nothing");
    }

    [Fact]
    public void ThereIsExactlyONERelicInAFacility()
    {
        // It is the payoff for the deepest floor of a hidden band. Two of them would make it scenery.
        SurfaceLayout.Field field = SurfaceLayout.DefaultField;

        foreach (string body in Bodies().Where(UndergroundComplex.HasUnlistedBand))
        {
            int found = 0;
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, field);
                for (int room = 0; room < floor.RoomCentres.Count; room++)
                {
                    if (UndergroundComplex.InRoom(body, level, room) == UndergroundComplex.Haul.Relic)
                    {
                        found++;
                    }
                }
            }

            Assert.Equal(1, found);
        }
    }

    [Fact]
    public void TheRelicKindWasAPPENDED_SoOldVaultsStillReadCorrectly()
    {
        // The ordinal is what a saved satchel stores. Inserting a kind in the middle would silently
        // reinterpret every item in every existing vault — a paper becoming a set of rounds on load.
        Assert.Equal(0, (int)Satchel.Kind.Authority);
        Assert.Equal(1, (int)Satchel.Kind.Paper);
        Assert.Equal(2, (int)Satchel.Kind.Rounds);
        Assert.Equal(3, (int)Satchel.Kind.Dirt);
        Assert.Equal(4, (int)Satchel.Kind.Relic);

        // And it round-trips like everything else.
        var item = new Satchel.Item(Satchel.Kind.Relic, "hive:luna:-20:1");
        Assert.True(Satchel.Item.TryParse(item.Stored, out Satchel.Item back));
        Assert.Equal(item, back);
    }

    /// <summary>A spread of real body ids, enough that some of them have a band nobody listed.</summary>
    private static IEnumerable<string> Bodies()
    {
        for (int i = 0; i < 120; i++)
        {
            yield return $"moon-{i}";
        }

        yield return "luna";
        yield return "ganymede";
        yield return "titan";
    }
}
