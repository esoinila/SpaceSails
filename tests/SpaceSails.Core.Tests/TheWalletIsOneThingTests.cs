using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #697 · THE WALLET IS ONE THING, AND IT CAN BE HELD UP ALL AT ONCE. Owner: <i>"Let's also add option to try
/// all ID cards ... by grouping them into a folder in the inventory."</i> — and, on the register the outcome
/// had to be written in: <i>"It is a little throw at the movie ... where he had this wallet with zillion
/// different contradictory IDs :-D"</i>
///
/// <para>The fold is the whole feature, and it is the part that can go quietly wrong. A captain who has worked
/// three sites carries several authorities that disagree about who they work for; pressing them all at a reader
/// used to be four presses producing four sentences, of which one was worth reading. What the fan must do is
/// pick that one.</para>
///
/// <para><b>Nothing here is new prose about cards.</b> Every sentence a fan can produce at a shaft gate is a
/// sentence a single try already produced — the wrong-shaft and wrong-site readings (#679/#683) are the best
/// storytelling the Hive has, and the fold RANKS them rather than restating them. The two lines that are new
/// belong to the doors that have no reader at all, where fanning a wallet has to answer once and never N
/// times.</para>
/// </summary>
public sealed class TheWalletIsOneThingTests
{
    private const string Site = "luna";

    /// <summary>The authority the gate in front of the captain is looking for.</summary>
    private static string Gate => new UndergroundComplex.AuthorityCard(Site, 2).Id;

    private static Satchel.Item Card(string body, int band) =>
        new(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(body, band).Id);

    /// <summary>A card this build cannot read — an edited save, or a future build's authority. It is the
    /// bottom rung of the ladder: it refuses, and it teaches nothing.</summary>
    private static Satchel.Item Unreadable => new(Satchel.Kind.Authority, "a-card-from-a-later-build");

    [Fact]
    public void AWorkingCardIsTheWholeAnswer_WhereverItSitsInTheWallet()
    {
        // The fold's first law, and the one the CALLER's state transitions rest on: if a card works, the
        // outcome IS that card's own outcome — the same sentence, the same Worked — so a fan can do exactly
        // what a single successful try does and there is never a second answer to keep in step with.
        //
        // Every order, because a wallet is not sorted and the card that opens the hole under your feet is as
        // likely to be the last one you found as the first.
        Satchel.Item works = Card(Site, 2);
        SatchelTry.Outcome alone = SatchelTry.Offer(works, SatchelTry.Target.ShaftGate, Gate);
        Assert.True(alone.Worked, "the fixture is wrong — the card for this gate does not open it.");

        List<Satchel.Item>[] wallets =
        [
            [works, Card("titan", 1), Card(Site, 3)],
            [Card("titan", 1), works, Card(Site, 3)],
            [Card("titan", 1), Card(Site, 3), works],
        ];

        foreach (List<Satchel.Item> wallet in wallets)
        {
            SatchelTry.Outcome fan = SatchelTry.OfferWallet(wallet, SatchelTry.Target.ShaftGate, Gate);

            Assert.True(fan.Worked,
                "the wallet holds the card this gate reads and the fan refused — a captain who owns the way " +
                "down was told they do not (#697).");
            Assert.Equal(alone.Line, fan.Line);
        }
    }

    [Fact]
    public void WithNothingThatWorks_TheMOSTINFORMATIVERefusalIsTheOneSaid()
    {
        // THE LADDER (#683), which is the reason this is a fold and not a loop. Three refusals are available
        // and they are not worth the same: a card for another shaft of THIS site tells the captain which floor
        // of the building they are standing in is worth going back for; a card for another site tells them the
        // wallet is for elsewhere; a card this build cannot read tells them nothing at all.
        //
        // RED-provable, and watched: a naive fold that says the FIRST refusal it meets passes every other
        // guard in this file and fails this one on the first ordering.
        var wallet = new List<Satchel.Item> { Unreadable, Card("titan", 1), Card(Site, 3) };
        string thisSite = SatchelTry.Offer(Card(Site, 3), SatchelTry.Target.ShaftGate, Gate).Line;
        string elsewhere = SatchelTry.Offer(Card("titan", 1), SatchelTry.Target.ShaftGate, Gate).Line;

        // Every ordering of the same three cards answers with the same sentence: the ladder ranks by what a
        // refusal TEACHES, and a pocket has no order worth respecting.
        foreach (List<Satchel.Item> order in Shuffles(wallet))
        {
            SatchelTry.Outcome fan = SatchelTry.OfferWallet(order, SatchelTry.Target.ShaftGate, Gate);
            Assert.False(fan.Worked);
            Assert.Equal(thisSite, fan.Line);
        }

        // And one rung down: with no card for this site at all, the foreign card beats the unreadable one.
        foreach (List<Satchel.Item> order in Shuffles([Unreadable, Card("titan", 1)]))
        {
            Assert.Equal(elsewhere,
                SatchelTry.OfferWallet(order, SatchelTry.Target.ShaftGate, Gate).Line);
        }

        // The nearest miss is NAMED — that is what makes the fan worth pressing rather than a slot handle.
        // Card(Site, 3) runs shaft 4 of this site, and the sentence says so (#679).
        Assert.Contains("4", thisSite, StringComparison.Ordinal);
        Assert.DoesNotContain(BodyNames.Designation("titan"), thisSite, StringComparison.Ordinal);
    }

    [Fact]
    public void AWalletOfONEAnswersExactlyAsThatOneCardDoes()
    {
        // A folder of one is bureaucracy about bureaucracy, and a sentence about fanning a wallet when one
        // card came out of it would be the third named bug class in a line of prose: the sim doing one thing
        // while the sentence reports another.
        foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
        {
            Satchel.Item one = Card("titan", 1);
            Assert.Equal(
                SatchelTry.Offer(one, target, Gate).Line,
                SatchelTry.OfferWallet([one], target, Gate).Line);
        }
    }

    [Fact]
    public void TheSEALEDWayAnswersONCE_HoweverManyCardsCameOutOfThePocket()
    {
        // #590 call 2, at the one press most likely to break it. Fanning six authorities at a door with no
        // reader must not print six refusals, and the one it does print may not name a card, a shaft or a
        // site — because naming any of them would hint that somewhere there is a card that opens this, and
        // there is not.
        var wallet = new List<Satchel.Item>();
        string[] bodies = ["luna", "titan", "europa", "miranda", "triton", "the-clinker"];
        foreach (string body in bodies)
        {
            wallet.Add(Card(body, 1));
            if (wallet.Count < 2)
            {
                continue;
            }

            SatchelTry.Outcome fan = SatchelTry.OfferWallet(wallet, SatchelTry.Target.SealedWay);
            Assert.False(fan.Worked, "a sealed way opened for a wallet (#590).");

            // ONE honest sentence. The body of the refusal appears exactly once however thick the wallet is.
            Assert.Equal(1, Occurrences(fan.Line, "There is no reader on this"));
            Assert.Contains("it is not waiting", fan.Line, StringComparison.OrdinalIgnoreCase);

            // And it is not one card's sentence with the rest of the wallet pretended away: the captain held
            // up all of them, and the line says a thing that is true of all of them. This is the register the
            // owner asked for — a reader working through several mutually incompatible authorities without
            // comment — and it is why a fold that simply returned the first refusal is not enough here.
            foreach (Satchel.Item card in wallet)
            {
                Assert.NotEqual(
                    SatchelTry.Offer(card, SatchelTry.Target.SealedWay).Line, fan.Line);
            }

            // And it hints at nothing, exactly as the single-card refusal hints at nothing.
            Assert.DoesNotContain("shaft", fan.Line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SECTOR", fan.Line, StringComparison.OrdinalIgnoreCase);
            foreach (string body2 in bodies)
            {
                Assert.DoesNotContain(BodyNames.Designation(body2), fan.Line, StringComparison.Ordinal);
            }
        }

        // The room door is the same call one size down: mechanical, shut, and not refusing you.
        SatchelTry.Outcome shut = SatchelTry.OfferWallet(wallet, SatchelTry.Target.RoomDoor);
        Assert.False(shut.Worked);
        Assert.Equal(1, Occurrences(shut.Line, "The lock is mechanical"));
        Assert.DoesNotContain("shaft", shut.Line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnEmptyWalletIsAnswered_AndTheSEALEDWayStillSaysNothingItDidNotSayBefore()
    {
        // Nothing to fan is a state the dialog never shows (no cards, no folder row), which is exactly why
        // Core has to answer it: a control that can be reached by a save, a keybind or a later caller and
        // says nothing is indistinguishable from a bug.
        //
        // At the two FINAL doors the answer is the door's OWN sentence, word for word — an empty wallet that
        // suddenly started discussing wallets at a sealed way would be hinting that a full one matters.
        var paper = new Satchel.Item(Satchel.Kind.Paper, "clue:something");

        foreach (SatchelTry.Target final in new[] { SatchelTry.Target.SealedWay, SatchelTry.Target.RoomDoor })
        {
            Assert.Equal(
                SatchelTry.Offer(paper, final).Line,
                SatchelTry.OfferWallet([], final).Line);
        }

        // At a gate it says the true thing: there is no authority in the wallet.
        SatchelTry.Outcome gate = SatchelTry.OfferWallet([], SatchelTry.Target.ShaftGate, Gate);
        Assert.False(gate.Worked);
        Assert.NotEqual("", gate.Line);
        Assert.Contains("wallet", gate.Line, StringComparison.OrdinalIgnoreCase);

        // A satchel of papers and rounds is an empty WALLET: the fold reads authorities and nothing else, so
        // a caller handing it the whole pocket can never get a paper narrated as a card.
        Assert.Equal(
            SatchelTry.OfferWallet([], SatchelTry.Target.ShaftGate, Gate).Line,
            SatchelTry.OfferWallet(
                [paper, new Satchel.Item(Satchel.Kind.Rounds, "ball", 6)],
                SatchelTry.Target.ShaftGate, Gate).Line);
    }

    [Fact]
    public void NoFanEverOpensAFINALDoor_AndNoFanEverExplainsWhatTheBuildingWasFor()
    {
        // §13.8 at the press most tempted to break it, and #590 call 2 alongside it. The fan is a new control
        // over old sentences and it inherits every silence they keep.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave", "subject"];

        var wallet = new List<Satchel.Item>
        {
            Card("luna", 0), Card("luna", 3), Card("titan", 2), Unreadable,
        };

        foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
        {
            SatchelTry.Outcome fan = SatchelTry.OfferWallet(wallet, target, Gate);

            if (target is SatchelTry.Target.SealedWay or SatchelTry.Target.RoomDoor)
            {
                Assert.False(fan.Worked, $"{target} opened for a fanned wallet (#590).");
            }

            Assert.NotEqual("", fan.Line);
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, fan.Line, StringComparison.OrdinalIgnoreCase);
            }
            Assert.DoesNotContain("SECTOR", fan.Line, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheFanIsDeterministic_LikeEverythingElseInCore()
    {
        // The same wallet at the same door twice is the same sentence. A fold that reached for a roll would
        // make TRY a slot handle, which is the exact thing #679 was filed to stop.
        var wallet = new List<Satchel.Item> { Card("titan", 1), Card(Site, 3), Unreadable };
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(
                SatchelTry.OfferWallet(wallet, SatchelTry.Target.ShaftGate, Gate).Line,
                SatchelTry.OfferWallet(wallet, SatchelTry.Target.ShaftGate, Gate).Line);
        }
    }

    /// <summary>Every ordering of a small wallet. The ladder is a fact about what a refusal teaches and must
    /// not be a fact about which pocket a card fell into.</summary>
    private static List<List<Satchel.Item>> Shuffles(List<Satchel.Item> of)
    {
        var all = new List<List<Satchel.Item>>();
        if (of.Count <= 1)
        {
            all.Add([.. of]);
            return all;
        }

        for (int i = 0; i < of.Count; i++)
        {
            var rest = new List<Satchel.Item>(of);
            rest.RemoveAt(i);
            foreach (List<Satchel.Item> tail in Shuffles(rest))
            {
                var one = new List<Satchel.Item> { of[i] };
                one.AddRange(tail);
                all.Add(one);
            }
        }
        return all;
    }

    private static int Occurrences(string line, string needle)
    {
        int n = 0;
        for (int at = line.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = line.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }
}
