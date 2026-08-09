using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #603 · THE SATCHEL. Owner: <i>"we should have some option to try use those at the locked doors... maybe
/// we need like on-site-carried-items inventory ... The captains ledger has the ship stuff but we should
/// have something similar on foot."</i>
/// </summary>
public sealed class TheSatchelTests
{
    private static Satchel.Item Card(string body, int band) =>
        new(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(body, band).Id);

    [Fact]
    public void ItIsAPocketAndNotAWarehouse()
    {
        // Capped so what is in it stays legible at a glance. The moment it needs sorting it has stopped
        // being a satchel and become inventory management, which is a different game and not this one.
        //
        // #688 · The cap that matters is now the one on BULK, and the ballast has to be bulky to meet it —
        // paper rides in its own bigger sleeve, so twelve manifests no longer fill anything.
        IReadOnlyList<Satchel.Item> bag = [];
        for (int i = 0; i < Satchel.PocketCapacity + 6; i++)
        {
            bag = Satchel.Add(bag, new Satchel.Item(Satchel.Kind.Relic, $"pallet-{i}"));
        }

        Assert.Equal(Satchel.PocketCapacity, bag.Count);
        Assert.True(Satchel.IsFull(bag, Satchel.Kind.Relic));

        // And it refuses politely rather than silently dropping the LAST thing you found, which would be
        // the crueller failure — you would never know what you had lost.
        Assert.Contains(bag, i => i.Id == "pallet-0");
    }

    [Fact]
    public void OnlyRoundsSTACK()
    {
        // Owner: "sometimes we find like 6 rounds ... and take them". Six is one thing you are carrying, so
        // ammunition must not eat the pocket a round at a time. A second copy of somebody's file, on the
        // other hand, is still one file to you.
        IReadOnlyList<Satchel.Item> bag = [];
        bag = Satchel.Add(bag, new Satchel.Item(Satchel.Kind.Rounds, "lab-2stage", 6));
        bag = Satchel.Add(bag, new Satchel.Item(Satchel.Kind.Rounds, "lab-2stage", 4));
        Assert.Single(bag);
        Assert.Equal(10, Satchel.CountOf(bag, Satchel.Kind.Rounds, "lab-2stage"));

        bag = Satchel.Add(bag, Card("luna", 1));
        bag = Satchel.Add(bag, Card("luna", 1));
        Assert.Equal(1, Satchel.CountOf(bag, Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard("luna", 1).Id));
    }

    [Fact]
    public void SpendingTakesTheRightAmountAndNoMore()
    {
        IReadOnlyList<Satchel.Item> bag = Satchel.Add([], new Satchel.Item(Satchel.Kind.Rounds, "lab-2stage", 6));
        bag = Satchel.Remove(bag, Satchel.Kind.Rounds, "lab-2stage", 4);
        Assert.Equal(2, Satchel.CountOf(bag, Satchel.Kind.Rounds, "lab-2stage"));

        bag = Satchel.Remove(bag, Satchel.Kind.Rounds, "lab-2stage", 5);   // more than is there
        Assert.Empty(bag);

        // Taking something that was never in there is not an error, it is nothing.
        Assert.Empty(Satchel.Remove(bag, Satchel.Kind.Paper, "nope"));
    }

    [Fact]
    public void ItSurvivesBeingWrittenDownAndReadBack()
    {
        // The save carries the FACT and never the words — the prose is a seeded property of the world and
        // would go stale the day the words changed. So this round trip IS the persistence contract.
        var all = new List<Satchel.Item>
        {
            Card("luna", 1),
            new(Satchel.Kind.Paper, "hive:luna:-3:7"),
            new(Satchel.Kind.Rounds, "lab-2stage", 6),
            new(Satchel.Kind.Dirt, "hive:dirt:titan:-9:2"),
        };

        foreach (Satchel.Item item in all)
        {
            Assert.True(Satchel.Item.TryParse(item.Stored, out Satchel.Item back), item.Stored);
            Assert.Equal(item, back);
        }

        // An edited or future save is dropped, not thrown over.
        // Including the ambiguity that broke the first cut: an id full of colons must survive intact.
        Assert.True(Satchel.Item.TryParse("1:1:hive:luna:-3:7", out Satchel.Item colons));
        Assert.Equal("hive:luna:-3:7", colons.Id);

        foreach (string junk in new[] { null!, "", "9:1:x", "notanint:1:x", "0:1:", "2:0:x", "2:-1:x", ":", "1:x" })
        {
            Assert.False(Satchel.Item.TryParse(junk, out _), $"'{junk}' parsed as an item.");
        }
    }

    [Fact]
    public void ACardOpensITSShaftAndNoOtherOnes()
    {
        // #590's law, now reachable by hand rather than only by the engine: the wallet is not a skeleton key.
        string wanted = new UndergroundComplex.AuthorityCard("luna", 2).Id;

        SatchelTry.Outcome ok = SatchelTry.Offer(Card("luna", 2), SatchelTry.Target.ShaftGate, wanted);
        Assert.True(ok.Worked);

        foreach (Satchel.Item wrong in new[] { Card("luna", 1), Card("titan", 2) })
        {
            SatchelTry.Outcome no = SatchelTry.Offer(wrong, SatchelTry.Target.ShaftGate, wanted);
            Assert.False(no.Worked);
            Assert.NotEmpty(no.Line);
        }
    }

    [Fact]
    public void ASEALEDWayRefusesEverythingAndNeverHints()
    {
        // #590 call 2, held from the other side. These doors exist to be walls with a world behind them, so
        // the refusal must be FINAL — it may not suggest that a different card, a code or a tool would do
        // it, because none would, and a captain sent off to look for one has been lied to.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            SatchelTry.Outcome no = SatchelTry.Offer(
                new Satchel.Item(kind, "whatever"), SatchelTry.Target.SealedWay);

            Assert.False(no.Worked);
            foreach (string hint in new[] { "another card", "the right", "elsewhere", "somewhere else" })
            {
                Assert.DoesNotContain(hint, no.Line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EVERYRefusalSaysWhy()
    {
        // The law the whole verb rests on. A control that does nothing and says nothing is indistinguishable
        // from a bug, and this ground has shipped that twice in a week — the lift that only went down, and
        // the return that put the captain in a wall.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
            {
                SatchelTry.Outcome o = SatchelTry.Offer(new Satchel.Item(kind, "x"), target, "someone-else#1");
                Assert.False(string.IsNullOrWhiteSpace(o.Line),
                    $"{kind} offered to {target} said nothing at all.");
                Assert.True(o.Line.Length > 20, $"{kind} at {target} is too terse to be a reason.");
            }
        }
    }

    [Fact]
    public void OnlyRoundsFeedADryGunAndOnlyPaperFeedsTheTracker()
    {
        // The kind decides what it can be offered TO. A satchel that let you try anything on anything would
        // be a puzzle box rather than a pocket.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            var item = new Satchel.Item(kind, "paper-1", kind == Satchel.Kind.Rounds ? 6 : 1);
            Assert.Equal(kind == Satchel.Kind.Rounds,
                SatchelTry.Offer(item, SatchelTry.Target.Sentry).Worked);
            Assert.Equal(kind == Satchel.Kind.Paper,
                SatchelTry.Offer(item, SatchelTry.Target.Tracker).Worked);
        }
    }

    [Fact]
    public void ACluesWorthIsAPropertyOfTheDOCUMENT()
    {
        // Owner: "we could have like stronger and stronger clues... most strong would put a dot about it on
        // the motion detector." So a paper is not "a lead" — it is a lead of some QUALITY, and that is
        // seeded off the document rather than rolled when it is read.
        for (int i = 0; i < 50; i++)
        {
            string id = $"paper-{i}";
            Assert.Equal(FieldClue.CertaintyOf(id), FieldClue.CertaintyOf(id));
        }

        // Strictly tighter as it gets better, and only the best one is a DOT.
        Assert.True(FieldClue.SpreadFor(FieldClue.Certainty.Vague)
            > FieldClue.SpreadFor(FieldClue.Certainty.Narrow));
        Assert.True(FieldClue.SpreadFor(FieldClue.Certainty.Narrow)
            > FieldClue.SpreadFor(FieldClue.Certainty.Exact));

        Assert.False(FieldClue.IsExact(FieldClue.Certainty.Vague));
        Assert.False(FieldClue.IsExact(FieldClue.Certainty.Narrow));
        Assert.True(FieldClue.IsExact(FieldClue.Certainty.Exact));

        // The vague end must stay genuinely vague. A wash you can cross in twenty paces is a dot wearing a
        // disguise, and it would hand back the search it was only meant to narrow.
        Assert.True(FieldClue.SpreadFor(FieldClue.Certainty.Vague) > 60);
    }

    [Fact]
    public void TheGoodCluesStayRARE()
    {
        // A field full of exact clues is a field with no searching left in it, and the search is the game.
        var seen = new Dictionary<FieldClue.Certainty, int>();
        const int Sample = 600;
        for (int i = 0; i < Sample; i++)
        {
            FieldClue.Certainty c = FieldClue.CertaintyOf($"probe-paper-{i}");
            seen[c] = seen.GetValueOrDefault(c) + 1;
        }

        double exact = seen.GetValueOrDefault(FieldClue.Certainty.Exact) / (double)Sample;
        double vague = seen.GetValueOrDefault(FieldClue.Certainty.Vague) / (double)Sample;

        Assert.InRange(exact, 0.08, 0.28);   // the one you tell people about
        Assert.InRange(vague, 0.35, 0.65);   // most paper is a mention
        Assert.Equal(3, seen.Count);         // and every tier actually occurs
    }

    [Fact]
    public void APaperCanBeREADAgainAndAgain()
    {
        // Owner: "press I ... inventory opens... select paper and see what the clue is." / "it should be
        // viewable many times."
        //
        // The first cut consumed the paper on reading, which conflated LOOKING with DECIDING and broke the
        // field book's own law (#587: a find that is shown once is a find that is lost). The document is a
        // pure function of the paper, so it can be re-read forever and always says the same thing.
        foreach (int i in Enumerable.Range(0, 30))
        {
            string id = $"hive:luna:-3:{i}";
            string first = FieldClue.Document(id);

            Assert.False(string.IsNullOrWhiteSpace(first));
            Assert.Equal(first, FieldClue.Document(id));

            // And it tells you HOW GOOD it is, every time, without being spent to find out.
            Assert.Contains(
                FieldClue.CertaintyOf(id) switch
                {
                    FieldClue.Certainty.Vague => "Only named",
                    FieldClue.Certainty.Narrow => "a description",
                    _ => "POSITION",
                },
                first, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ADocumentNeverNamesTheMoonForYou()
    {
        // Working out WHERE is the act the player is paying for. A paper that printed the answer would hand
        // back the search it exists to narrow — the same reason a vague clue paints a wide wash rather than
        // a dot.
        string[] moons = ["luna", "phobos", "europa", "titan", "miranda", "callisto", "triton"];
        foreach (int i in Enumerable.Range(0, 40))
        {
            string doc = FieldClue.Document($"hive:luna:-3:{i}");
            foreach (string moon in moons)
            {
                Assert.DoesNotContain(moon, doc, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EveryPaperHasItsOwnShortTitle_SoAPocketfulIsNotSixIdenticalLines()
    {
        // #613 · Owner, holding several at once: "the operational papers could have individual short
        // titles ... now they look identical in inventory."
        //
        // Worse than cosmetic. Six satchel entries all reading "operational paper" is not an inventory, it
        // is a counter: you cannot tell which one you have read, which floor it came off, or whether the
        // seventh pickup got you anything you did not already have.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int floor = -1; floor >= -24; floor--)
        {
            for (int room = 0; room < 4; room++)
            {
                string title = FieldClue.Title($"hive:luna:{floor}:{room}");

                // Short enough to sit on one satchel row beside an emoji and a certainty.
                Assert.InRange(title.Length, 4, 40);

                // It is what the PAPERWORK calls itself. It must never do the player's thinking: the whole
                // ladder rests on the captain deciding a document is worth something, and a title that said
                // "lead" or "site" would make that decision for them.
                foreach (string tell in new[] { "lead", "clue", "site", "facility", "secret" })
                {
                    Assert.DoesNotContain(tell, title, StringComparison.OrdinalIgnoreCase);
                }
                seen.Add(title);
            }
        }

        // Genuinely several distinct ones across a site, not one string with the id glued on.
        Assert.True(seen.Count >= 5, $"only {seen.Count} distinct paper titles across 96 finds");
    }

    [Fact]
    public void ThePaperInYourPocketIsThePaperYouOpen()
    {
        // ONE SOURCE, consumed once. This repo's fourth named bug class is a single source read out of
        // order; the cheapest cousin of it is a single source ROLLED TWICE. Title and Document must not
        // each take their own roll, or a captain carries a "pay sheet" that opens as a shipping manifest.
        //
        // The pairing is checked through the visible strings, which is the only thing a player can see and
        // therefore the only thing worth pinning.
        (string Title, string InDoc)[] pairs =
        [
            ("movement order, third copy", "movement order"),
            ("supply requisition, countersigned", "supply requisition"),
            ("shipping manifest, torn", "shipping manifest"),
            ("maintenance log, two hands", "maintenance log"),
            ("inspection schedule, margin list", "inspection schedule"),
            ("pay sheet, allowances", "pay sheet"),
        ];

        for (int floor = -1; floor >= -24; floor--)
        {
            string id = $"hive:ganymede:{floor}:0";
            string title = FieldClue.Title(id);
            string doc = FieldClue.Document(id);

            (string Title, string InDoc) pair = Assert.Single(pairs, p => p.Title == title);
            Assert.Contains(pair.InDoc, doc, StringComparison.OrdinalIgnoreCase);
        }
    }
}
