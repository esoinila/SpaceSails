using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #678 · A FIND THE POCKET CANNOT TAKE MUST STAY IN THE ROOM. Owner, after a live playtest that found an
/// authority card announced and never delivered: <i>"we should have CI test that makes sure all picked items
/// that sound useful are put into the inventory or an option to keep them is offered. If refused the item
/// should stay where it was investigated last — not disappear like they do now, or seem to."</i>
///
/// <para>Two silent-drop shapes, one law. A Key room on the bottom band whose far-site fallback came up empty
/// still narrated a countersigned card into an empty hand; and a full pocket destroyed a find while the
/// sentence claimed it had taken it. Both are the third named bug class — the sim doing one thing while the
/// sentence reports another — and the second one is worse, because the room was consumed either way.</para>
///
/// <para>Stated once, and swept over every haul the generator can produce:</para>
///
/// <para><b>A pickup line may only be printed for something that actually went in, and what the pocket
/// cannot take is not consumed.</b></para>
/// </summary>
public sealed class ThePocketNeverLiesTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private const string Here = "luna";

    /// <summary>A pocket with no space left in it — every compartment that CAN fill, filled, and none of it
    /// the find.
    ///
    /// <para>#688 · It used to be twelve papers, back when one number governed everything. It has to be both
    /// compartments now, or the sweep below would hand a full-pocket case to a satchel with a whole empty
    /// sleeve in it and never once ask the question — this repo's fifth named bug class, a guard handed a
    /// world where the case it forbids cannot occur.</para></summary>
    private static IReadOnlyList<Satchel.Item> FullPocket()
    {
        IReadOnlyList<Satchel.Item> bag = [];
        for (int i = 0; i < Satchel.SleeveCapacity; i++)
        {
            bag = Satchel.Add(bag, new Satchel.Item(Satchel.Kind.Paper, $"ballast-{i}"));
        }
        for (int i = 0; i < Satchel.PocketCapacity; i++)
        {
            bag = Satchel.Add(bag, new Satchel.Item(Satchel.Kind.Relic, $"pallet-{i}"));
        }

        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            // The wallet is the exception and is meant to be: #688's whole point is that a card is never
            // refused, so a "full pocket" is full of everything else.
            //
            // #746 · Asked of the COMPARTMENT, not of one kind. The wallet holds two things now — an
            // authority and a day-labour chit — and naming a kind here made this guard a statement about
            // which cards happened to exist the day it was written rather than about the law it guards,
            // which is "the flat pocket never fills" and never was "Authority is special".
            Assert.Equal(
                Satchel.CompartmentOf(kind) != Satchel.Compartment.Wallet, Satchel.IsFull(bag, kind));
        }
        return bag;
    }

    /// <summary>Every shape a Key room's card can be in when the room is opened: the ordinary card for the
    /// band below, the #613 foreign card for another site, and NONE — the residual path that started this.</summary>
    private static IEnumerable<UndergroundComplex.AuthorityCard?> CardShapes()
    {
        yield return new UndergroundComplex.AuthorityCard(Here, 1);
        yield return new UndergroundComplex.AuthorityCard("titan", 0);
        yield return null;
    }

    /// <summary>#678 · THE LAW, asked of the REAL SATCHEL rather than of a flag.
    ///
    /// <para>"Into your pocket: …" is a claim about a possession. The only thing that makes it true is that
    /// <see cref="Satchel.Add"/> — the actual one, with the actual carried list — takes the item. A guard
    /// that only compared two fields of the outcome would have passed on the broken build, because the broken
    /// build was perfectly consistent with ITSELF: it announced the card, handed it to a satchel that refused
    /// it, and struck the room off anyway.</para></summary>
    private static void AssertTheLineAndThePocketAgree(
        UndergroundComplex.Pickup got, IReadOnlyList<Satchel.Item> pocket, string where)
    {
        bool claimed = got.Line.Contains("Into your pocket", StringComparison.OrdinalIgnoreCase);
        if (!claimed)
        {
            Assert.Null(got.Take);
            return;
        }

        Assert.NotNull(got.Take);
        Satchel.Item item = got.Take!.Value;

        // Strictly MORE than was there before: every find in this sweep is new to the pocket, so "it is in
        // there" would also be satisfied by a satchel that took nothing and happened to hold one already.
        Assert.True(
            Satchel.CountOf(Satchel.Add(pocket, item), item.Kind, item.Id) > Satchel.CountOf(pocket, item.Kind, item.Id),
            $"{where}: the line claims a pocket that the satchel refused to take it into.");

        // And a find that went in is a find the room has really given up.
        Assert.True(got.RoomEmptied, $"{where}: something went into the pocket out of a room still holding it.");
    }

    [Fact]
    public void APickupLineIsOnlyEverPrintedForSomethingThatWENTIN()
    {
        // The law, swept. "Into your pocket: …" is a claim about a possession, and the only thing that makes
        // it true is an item this call actually handed to the satchel.
        foreach (UndergroundComplex.Haul haul in Enum.GetValues<UndergroundComplex.Haul>())
        {
            foreach (UndergroundComplex.AuthorityCard? card in CardShapes())
            {
                foreach (IReadOnlyList<Satchel.Item> pocket in new[] { (IReadOnlyList<Satchel.Item>)[], FullPocket() })
                {
                    AssertTheLineAndThePocketAgree(
                        UndergroundComplex.WhatGoesInThePocket(haul, Here, card, "hive:luna:-3:2", pocket),
                        pocket, $"{haul} / card {card?.Id ?? "none"} / {pocket.Count} carried");
                }
            }
        }
    }

    [Fact]
    public void AFindThePocketCannotTakeSTAYSInTheRoom()
    {
        // Owner's own words: "If refused the item should stay where it was investigated last — not disappear
        // like they do now." So a refusal is the one outcome down here that leaves the world exactly as it
        // was: nothing taken, nothing said about a pocket, and the room NOT marked emptied.
        foreach (UndergroundComplex.Haul haul in Enum.GetValues<UndergroundComplex.Haul>())
        {
            foreach (UndergroundComplex.AuthorityCard? card in CardShapes())
            {
                const string find = "hive:luna:-3:2";
                UndergroundComplex.Pickup empty =
                    UndergroundComplex.WhatGoesInThePocket(haul, Here, card, find, []);
                UndergroundComplex.Pickup full =
                    UndergroundComplex.WhatGoesInThePocket(haul, Here, card, find, FullPocket());

                if (empty.Take is null)
                {
                    // Nothing to carry either way — a full pocket changes nothing about a stripped room.
                    Assert.Equal(empty, full);
                    Assert.True(full.RoomEmptied);
                    continue;
                }

                if (empty.Take!.Value.Kind == Satchel.Kind.Authority)
                {
                    // #688 · THE WALLET NEVER FILLS. A card is flat, so a full satchel is no reason to leave
                    // the best find in the game lying on a shelf — the room is emptied and the card is in the
                    // pocket exactly as it would have been on an empty walk. Owner: "I find the good keycard
                    // but my pockets are full and I can not pocket it. Lol, love it. Lets fix it. :-D"
                    Assert.Equal(empty, full);
                    Assert.True(full.RoomEmptied);
                    Assert.True(Satchel.CanTake(FullPocket(), full.Take!.Value));
                    continue;
                }

                Assert.Null(full.Take);
                Assert.False(full.RoomEmptied,
                    $"{haul} was consumed by a pocket that could not take it — the find is gone.");
                Assert.Contains("still be here", full.Line, StringComparison.OrdinalIgnoreCase);

                // And the room really does still have it: make space, search again, and the same find is
                // offered. This is the assertion the owner asked for in as many words.
                //
                // #688 · The space has to be made in the compartment the find rides in. Emptying a sheet out
                // of the sleeve does nothing for a relic on a pallet, and a guard that made the wrong kind of
                // room would be asserting against a satchel that still cannot take it.
                Satchel.Item want = empty.Take!.Value;
                IReadOnlyList<Satchel.Item> roomMade =
                    Satchel.CompartmentOf(want.Kind) == Satchel.Compartment.Sleeve
                        ? Satchel.Remove(FullPocket(), Satchel.Kind.Paper, "ballast-0")
                        : Satchel.Remove(FullPocket(), Satchel.Kind.Relic, "pallet-0");
                Assert.True(Satchel.CanTake(roomMade, want), $"{haul}: the room made was in the wrong pocket.");
                UndergroundComplex.Pickup again =
                    UndergroundComplex.WhatGoesInThePocket(haul, Here, card, find, roomMade);
                Assert.Equal(empty.Take, again.Take);
                Assert.True(again.RoomEmptied);
            }
        }
    }

    [Fact]
    public void SomethingYouAlreadyCarryAlwaysGoesIn_EvenAtCapacity()
    {
        // The capacity law is not "twelve rows or nothing": Add MERGES into a row that is already there, so
        // six more rounds of a kind you have fit in a full pocket. A CanTake that answered !IsFull would
        // refuse a find the satchel would happily have taken — and by this law, would leave it in the room
        // forever.
        IReadOnlyList<Satchel.Item> bag = FullPocket();

        Assert.True(Satchel.CanTake(bag, bag[3]));
        Assert.False(Satchel.CanTake(bag, new Satchel.Item(Satchel.Kind.Paper, "a-new-one")));

        // One law, two functions, and they must not drift: CanTake is true exactly when the thing ends up in
        // the pocket. Stated that way rather than as "the count went up", because for a unique thing you are
        // already carrying nothing goes up and it is still in there — Add's own rule ("a unique thing is
        // simply already yours"), and a guard that missed it would forbid a find the satchel accepts.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            // A satchel filled to the brim with ballast AND already carrying one of the thing under test, so
            // its own compartment is full and the merge case is live. Built this way on purpose: the obvious
            // version — fill it, then Add the item — is refused by that Add, so the "already carrying" case
            // never occurs and the test passes without ever asking the question. This repo's fifth bug class.
            //
            // #688 · Each compartment is left one short and then given the item under test, so the thing is
            // in there AND the compartment it rides in is full — for every kind, including the ones that now
            // ride in a different pocket from the ballast.
            IReadOnlyList<Satchel.Item> start = [];
            for (int i = 0; i < Satchel.SleeveCapacity - 1; i++)
            {
                start = Satchel.Add(start, new Satchel.Item(Satchel.Kind.Paper, $"ballast-{i}"));
            }
            for (int i = 0; i < Satchel.PocketCapacity - 1; i++)
            {
                start = Satchel.Add(start, new Satchel.Item(Satchel.Kind.Relic, $"pallet-{i}"));
            }
            start = Satchel.Add(start, new Satchel.Item(kind, "already", kind == Satchel.Kind.Rounds ? 6 : 1));

            // The wallet never fills, so for anything that rides in it this is a satchel that is full of
            // everything else and still has room — which is exactly the case the merge law has to survive.
            // #746 · Asked of the compartment, for the reason FullPocket's own copy of this line records.
            Assert.Equal(
                Satchel.CompartmentOf(kind) != Satchel.Compartment.Wallet, Satchel.IsFull(start, kind));
            Assert.True(Satchel.CountOf(start, kind, "already") > 0,
                $"the {kind} the case is about is not in the pocket — the test proves nothing.");

            foreach (string id in new[] { "already", "brand-new" })
            {
                var item = new Satchel.Item(kind, id, kind == Satchel.Kind.Rounds ? 6 : 1);
                bool inPocketAfter = Satchel.CountOf(Satchel.Add(start, item), kind, id) > 0;

                Assert.Equal(Satchel.CanTake(start, item), inPocketAfter);
            }
        }
    }

    [Fact]
    public void AKeyRoomThatMintedNoCardNarratesNoCardATALL()
    {
        // The residual path of #613's own fix, and the reason this issue exists. CardInRoom correctly returns
        // null on the bottom band; the client's fallback then mints a card for ANOTHER site — but only when a
        // lead is still available to name one. When it is not, nothing is minted, and the room used to go on
        // describing "an authority card, countersigned twice and still active" in a hand that was empty.
        //
        // So the room pays as an ordinary find instead: it says what is lying there, and what is lying there
        // is not a card.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                string nothing = UndergroundComplex.KeyLine(body, level, null);

                foreach (string claim in new[] { "authority card", "countersigned", "still active" })
                {
                    Assert.DoesNotContain(claim, nothing, StringComparison.OrdinalIgnoreCase);
                }

                // It is still a sentence about this room, and it is still deterministic.
                Assert.True(nothing.Length > 40, $"{body} B{-level}: the empty Key room barely says anything.");
                Assert.Equal(nothing, UndergroundComplex.KeyLine(body, level, null));

                // And the haul line goes through it, so no screen can route around the law.
                Assert.Equal(nothing, UndergroundComplex.HaulLine(UndergroundComplex.Haul.Key, body, level, 0, null));
            }
        }
    }

    [Fact]
    public void AKeyRoomThatDIDMintACardSaysWhichBuildingItIsFor()
    {
        // The other three quarters of the same branch. A card for this building promises the shaft under your
        // feet; a card for another one is #613's wallet, and the line has to be the one that matches what is
        // actually in the pocket — that is the whole of what went wrong here.
        var own = new UndergroundComplex.AuthorityCard(Here, 1);
        var foreign = new UndergroundComplex.AuthorityCard("titan", 0);

        string mine = UndergroundComplex.KeyLine(Here, -3, own);
        string theirs = UndergroundComplex.KeyLine(Here, -3, foreign);

        Assert.Contains("authority card", mine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(UndergroundComplex.CardTitle(own), mine, StringComparison.Ordinal);
        Assert.Contains("these floors", mine, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("authority card", theirs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not this one", theirs, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(mine, theirs);

        // And the pocket line agrees with the room's own sentence about whose building it is — one fact,
        // said twice, never two facts.
        UndergroundComplex.Pickup here = UndergroundComplex.WhatGoesInThePocket(
            UndergroundComplex.Haul.Key, Here, own, "hive:luna:-3:2", []);
        UndergroundComplex.Pickup elsewhere = UndergroundComplex.WhatGoesInThePocket(
            UndergroundComplex.Haul.Key, Here, foreign, "hive:luna:-3:2", []);

        Assert.DoesNotContain("not for this building", here.Line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not for this building", elsewhere.Line, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(own.Id, here.Take!.Value.Id);
        Assert.Equal(foreign.Id, elsewhere.Take!.Value.Id);
    }

    [Fact]
    public void EverySiteShapeIsSwept_IncludingTheOneThatBrokeIt()
    {
        // The harness half of the fifth bug class: a guard is only evidence if the world handed to it can
        // actually produce the case the guard forbids. The failing case was a BOTTOM BAND, which is why the
        // owner found it on a four-floor rock and not on a deep site — so the sweep has to contain shallow
        // sites, deep sites, bottom bands and the band nobody listed, and it has to be able to say so.
        int bottomBands = 0, unlisted = 0, deep = 0;

        foreach (string body in Bodies)
        {
            int trueBottom = UndergroundComplex.TrueDepthOf(body);
            int lastBand = UndergroundComplex.BandOf(trueBottom);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.AuthorityCard? card = UndergroundComplex.CardInRoom(body, level);
                if (card is null)
                {
                    bottomBands++;
                }
                if (UndergroundComplex.IsUnlisted(body, level))
                {
                    unlisted++;
                }
                if (UndergroundComplex.BandOf(level) > 0 && UndergroundComplex.BandOf(level) < lastBand)
                {
                    deep++;
                }

                // Every floor of every site: whatever the room holds, the sentence and the pocket agree —
                // on an empty pocket and on a full one, because the full one is where it went wrong.
                foreach (UndergroundComplex.Haul haul in Enum.GetValues<UndergroundComplex.Haul>())
                {
                    foreach (IReadOnlyList<Satchel.Item> pocket in
                        new[] { (IReadOnlyList<Satchel.Item>)[], FullPocket() })
                    {
                        AssertTheLineAndThePocketAgree(
                            UndergroundComplex.WhatGoesInThePocket(haul, body, card, $"hive:{body}:{level}:0", pocket),
                            pocket, $"{body} B{-level} / {haul} / {pocket.Count} carried");
                    }
                }
            }
        }

        Assert.True(bottomBands > 0, "no bottom band in the sweep — the case that broke this cannot occur");
        Assert.True(unlisted > 0, "no unlisted band in the sweep");
        Assert.True(deep > 0, "no floor in a middle band — the sweep is all shallow sites");
    }
}
