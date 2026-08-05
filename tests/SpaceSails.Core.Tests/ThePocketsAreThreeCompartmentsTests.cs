using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #688 · THE WALLET NEVER FILLS, AND LITTLE PAPERS GET A BIGGER SLEEVE. Owner, live on the deep site:
/// <i>"Oh I run out of space in the inventory. How do I get Bigger pockets ... lol... I find the good keycard
/// but my pockets are full and I can not pocket it. Lol, love it. Lets fix it. :-D"</i> — and, seconds later,
/// <i>"I think bigger pockets for little papers"</i>.
///
/// <para>One number governed everything a captain could carry, and it was a number chosen for BULK. A card is
/// flat. A sheet of paper is flat. Twelve was right for rounds, crates and relic paperwork and it was quietly
/// deciding that the best find in the game — the countersigned authority that opens the shaft under your feet
/// — could be refused because you were carrying eleven shipping manifests and a file on a harbourmaster.</para>
///
/// <para>So the satchel has three compartments, and only two of them can fill:</para>
///
/// <list type="bullet">
/// <item><b>The wallet</b> (<see cref="Satchel.Kind.Authority"/>) — never counts, never refuses. A card is
/// flat and it rides inside the satchel and weighs nothing worth mentioning.</item>
/// <item><b>The document sleeve</b> (<see cref="Satchel.Kind.Paper"/> + <see cref="Satchel.Kind.Dirt"/>, both
/// of them paper in the hand) — twenty-four.</item>
/// <item><b>The pockets</b> (everything else: rounds, relic paperwork, whatever comes next) — twelve, and it
/// keeps every one of its teeth.</item>
/// </list>
///
/// <para>Watched go RED against the single-cap satchel: the wallet test refused the card at twelve, the sleeve
/// test stopped at twelve papers, and the crowding test found thirty-six things in a pocket that held
/// twelve.</para>
/// </summary>
public sealed class ThePocketsAreThreeCompartmentsTests
{
    private static Satchel.Item Card(string body, int band) =>
        new(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(body, band).Id);

    /// <summary>A bulky thing — the kind the twelve was always about.</summary>
    private static Satchel.Item Bulky(int i) => new(Satchel.Kind.Relic, $"pallet-{i}");

    private static IReadOnlyList<Satchel.Item> Fill(
        IReadOnlyList<Satchel.Item> bag, int n, Func<int, Satchel.Item> make)
    {
        for (int i = 0; i < n; i++)
        {
            bag = Satchel.Add(bag, make(i));
        }
        return bag;
    }

    [Fact]
    public void TheWalletNeverFills()
    {
        // The owner's exact heartbreak, made impossible. A pocket stuffed to the brim with bulk AND a sleeve
        // stuffed with paper still takes the good keycard, because the good keycard is not in either of them.
        IReadOnlyList<Satchel.Item> bag = Fill([], Satchel.PocketCapacity, Bulky);
        bag = Fill(bag, Satchel.SleeveCapacity, i => new Satchel.Item(Satchel.Kind.Paper, $"paper-{i}"));

        foreach ((string body, int band) in new[] { ("luna", 1), ("titan", 0), ("miranda", 2), ("europa", 1) })
        {
            Satchel.Item card = Card(body, band);
            Assert.True(Satchel.CanTake(bag, card),
                $"the satchel refused {card.Id} — the wallet filled up, which is the whole of #688.");

            bag = Satchel.Add(bag, card);
            Assert.True(Satchel.CountOf(bag, Satchel.Kind.Authority, card.Id) > 0,
                "CanTake said yes and Add dropped it on the floor — one law, two functions, and they drifted.");
        }

        // And the cards did not eat anybody else's room on the way in: the pocket and the sleeve are exactly
        // as full as they were, which is the difference between "does not count" and "counted somewhere else".
        Assert.Equal(0, Satchel.SpaceLeft(bag, Satchel.Compartment.Pocket));
        Assert.Equal(0, Satchel.SpaceLeft(bag, Satchel.Compartment.Sleeve));
        Assert.Equal(Satchel.PocketCapacity + Satchel.SleeveCapacity + 4, bag.Count);
    }

    [Fact]
    public void LittlePapersGetTheirOwnBiggerSleeve()
    {
        // Owner: "I think bigger pockets for little papers." Twenty-four sheets, and the twenty-fifth is
        // refused — the sleeve has a limit too, because a satchel you never have to think about has stopped
        // being a satchel.
        IReadOnlyList<Satchel.Item> bag = Fill([], Satchel.SleeveCapacity + 6, i =>
            new Satchel.Item(i % 2 == 0 ? Satchel.Kind.Paper : Satchel.Kind.Dirt, $"sheet-{i}"));

        Assert.Equal(Satchel.SleeveCapacity, bag.Count);
        Assert.False(Satchel.CanTake(bag, new Satchel.Item(Satchel.Kind.Paper, "one-more")));
        Assert.False(Satchel.CanTake(bag, new Satchel.Item(Satchel.Kind.Dirt, "one-more")));

        // A file and a manifest share the sleeve — both are paper in the hand, and a satchel that let you
        // carry twenty-four of each while refusing a thirteenth round would be arithmetic, not a pocket.
        Assert.Equal(Satchel.Compartment.Sleeve, Satchel.CompartmentOf(Satchel.Kind.Paper));
        Assert.Equal(Satchel.Compartment.Sleeve, Satchel.CompartmentOf(Satchel.Kind.Dirt));

        // And the first sheet is still in there. It refuses politely; it never drops what you already had.
        Assert.Contains(bag, i => i.Id == "sheet-0");
    }

    [Fact]
    public void TheBulkyPocketKeepsItsTeeth()
    {
        // Twelve is still twelve for the things twelve was chosen for. This is the half of #688 that must NOT
        // move: the pressure that makes "leave it here" a decision lives here.
        IReadOnlyList<Satchel.Item> bag = Fill([], Satchel.PocketCapacity + 6, Bulky);

        Assert.Equal(Satchel.PocketCapacity, bag.Count);
        Assert.Equal(12, Satchel.PocketCapacity);
        Assert.False(Satchel.CanTake(bag, Bulky(99)));
        Assert.False(Satchel.CanTake(bag, new Satchel.Item(Satchel.Kind.Rounds, "lab-2stage", 6)));
        Assert.True(Satchel.IsFull(bag, Satchel.Kind.Relic));
        Assert.True(Satchel.IsFull(bag, Satchel.Kind.Rounds));

        // ...and being full of bulk says nothing at all about paper or about cards.
        Assert.False(Satchel.IsFull(bag, Satchel.Kind.Paper));
        Assert.False(Satchel.IsFull(bag, Satchel.Kind.Authority));
    }

    [Fact]
    public void PaperNeverCrowdsTheBulkAndBulkNeverCrowdsThePaper()
    {
        // The compartments are separate or they are not compartments. Twelve bulky things and twenty-four
        // sheets and a wallet, all at once, in one satchel — thirty-six rows the old single cap could not
        // have held three of.
        IReadOnlyList<Satchel.Item> bag = Fill([], Satchel.SleeveCapacity, i =>
            new Satchel.Item(Satchel.Kind.Paper, $"paper-{i}"));

        for (int i = 0; i < Satchel.PocketCapacity; i++)
        {
            Assert.True(Satchel.CanTake(bag, Bulky(i)),
                $"a sleeve full of paper refused bulky thing {i} — the compartments are sharing a number.");
            bag = Satchel.Add(bag, Bulky(i));
        }

        Assert.Equal(Satchel.PocketCapacity + Satchel.SleeveCapacity, bag.Count);
        Assert.Equal(Satchel.PocketCapacity, Satchel.Used(bag, Satchel.Compartment.Pocket));
        Assert.Equal(Satchel.SleeveCapacity, Satchel.Used(bag, Satchel.Compartment.Sleeve));
    }

    [Fact]
    public void SixMoreRoundsOfAKindYouCarryStillGoInAtCapacity()
    {
        // #678's law, carried forward unchanged through the restructure: Add MERGES into the row that is
        // already there, so a full pocket is no obstacle to more of a thing you have. A CanTake that answered
        // "is the compartment full" would refuse a find the satchel would happily take — and by #615's law
        // would leave it in the room forever.
        IReadOnlyList<Satchel.Item> bag = Fill([], Satchel.PocketCapacity - 1, Bulky);
        bag = Satchel.Add(bag, new Satchel.Item(Satchel.Kind.Rounds, "lab-2stage", 6));

        Assert.True(Satchel.IsFull(bag, Satchel.Kind.Rounds));
        Assert.True(Satchel.CanTake(bag, new Satchel.Item(Satchel.Kind.Rounds, "lab-2stage", 6)));
        Assert.Equal(12, Satchel.CountOf(
            Satchel.Add(bag, new Satchel.Item(Satchel.Kind.Rounds, "lab-2stage", 6)),
            Satchel.Kind.Rounds, "lab-2stage"));

        // Every kind, both compartments: CanTake is true exactly when the thing ends up in the pocket.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            IReadOnlyList<Satchel.Item> start = Fill([], Satchel.PocketCapacity - 1, Bulky);
            start = Fill(start, Satchel.SleeveCapacity - 1, i => new Satchel.Item(Satchel.Kind.Paper, $"p-{i}"));
            start = Satchel.Add(start, new Satchel.Item(kind, "already", kind == Satchel.Kind.Rounds ? 6 : 1));

            Assert.True(Satchel.CountOf(start, kind, "already") > 0,
                $"the {kind} this case is about is not in the pocket — the test proves nothing.");

            foreach (string id in new[] { "already", "brand-new" })
            {
                var item = new Satchel.Item(kind, id, kind == Satchel.Kind.Rounds ? 6 : 1);
                bool inPocketAfter = Satchel.CountOf(Satchel.Add(start, item), kind, id) > 0;
                Assert.Equal(Satchel.CanTake(start, item), inPocketAfter);
            }
        }
    }

    [Fact]
    public void TheSpaceLeftLineNamesWhatItCountsAndNeverCountsCards()
    {
        // The old line said "N spaces left" from one number, and after #688 that number does not exist: what
        // is left depends on what you are about to pick up. A line that reports one figure for three
        // compartments is this repo's third named bug class in a subtitle — the sim doing one thing while a
        // sentence reports another.
        IReadOnlyList<Satchel.Item> bag = Fill([], 3, Bulky);
        bag = Fill(bag, 5, i => new Satchel.Item(Satchel.Kind.Paper, $"paper-{i}"));

        string said = Satchel.SpaceLine(bag);
        Assert.Contains((Satchel.SleeveCapacity - 5).ToString(), said, StringComparison.Ordinal);
        Assert.Contains((Satchel.PocketCapacity - 3).ToString(), said, StringComparison.Ordinal);
        Assert.Contains("paper", said, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pocket", said, StringComparison.OrdinalIgnoreCase);

        // And the wallet is invisible to it, however fat it gets. Stated as an identity rather than as "does
        // not contain the number 9", because the number of cards you hold is also a number of other things.
        IReadOnlyList<Satchel.Item> walletted = bag;
        foreach (string body in new[] { "luna", "titan", "miranda", "europa", "phobos", "triton" })
        {
            walletted = Satchel.Add(walletted, Card(body, 0));
            walletted = Satchel.Add(walletted, Card(body, 1));
        }

        Assert.True(walletted.Count > bag.Count + 6, "the wallet under test is not actually carrying cards.");
        Assert.Equal(Satchel.SpaceLine(bag), Satchel.SpaceLine(walletted));

        // Both ends of both compartments say something a captain can act on, and neither of them lies.
        Assert.Contains("full", Satchel.SpaceLine(
            Fill(Fill([], Satchel.SleeveCapacity, i => new Satchel.Item(Satchel.Kind.Paper, $"x-{i}")),
                Satchel.PocketCapacity, Bulky)), StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(Satchel.SpaceLine([])));
    }
}
