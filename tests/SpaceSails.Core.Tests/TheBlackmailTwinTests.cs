using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #233 · THE CAR WITH PHOTOGRAPHS IN IT — the rules half.
///
/// <para>Owner: <i>"I was thinking of trying the car mission (the other variant of this could be a set of
/// compromising data / photos between the seats)."</i> Canon pass, Fable, 2026-09-05.</para>
///
/// <para>Four laws live here, and each one is a thing that could quietly rot: <b>the ratio</b> (one car in
/// four, dealt off the booth's own seed and not a coin flipped per frame), <b>the object's identity</b> (the
/// glyph, the name, the pocket it rides in, the card it raises), <b>the fence's arithmetic</b> (more than the
/// contract, and DERIVED from the contract rather than typed beside it), and <b>the words</b> — the three
/// sentences this feature is allowed to say, character for character, and not a fourth.</para>
///
/// <para><b>Proven RED</b>, each guard, by reverting the thing it guards — see the note on each fact.</para>
/// </summary>
public class TheBlackmailTwinTests
{
    // The canon strings, retyped here on purpose. Two copies of a sentence in two files means a reword has
    // to be a deliberate act in both, rather than a typo in one (#573's shape).
    private const string TheName = "A data chip";
    private const string TheCard =
        "Photographs. Two people who are not supposed to know each other, and a timestamp that says they did.";
    private const string TheClient = "You didn't look. Say you didn't look.";
    private const string TheFence =
        "I know a buyer for that. He'd rather it never existed, which is the best price there is.";

    private const string TheReservedWord = "monolith";

    // ── THE RATIO ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>ONE CAR IN FOUR, measured over the space of booths a captain could actually take the job
    /// at. Not asserted off the constant — asserted off the DEAL, swept over four thousand distinct
    /// (watch, berth) pairs, because a roll that reads <c>OneInEvery</c> and then does something else with
    /// it is exactly the bug a test that only reads the constant cannot see.
    ///
    /// <para><b>Proven RED</b> two ways: the roll's <c>.Face == 1</c> widened to <c>&lt;= 2</c> (0.50, out
    /// of band), and the d-N replaced with the wallet always (0.00, and the both-outcomes guard below goes
    /// first, which is the point of it being there).</para></summary>
    [Fact]
    public void OneRoadsterInFour_CarriesTheChipInstead()
    {
        (int chips, int total) = SweepTheDeal();

        double share = (double)chips / total;
        double want = 1.0 / CompromisingChip.OneInEvery;

        Assert.Equal(4, CompromisingChip.OneInEvery);
        Assert.True(Math.Abs(share - want) < 0.02,
            $"the deal put the chip in {share:P2} of {total} cars; canon says one in "
            + $"{CompromisingChip.OneInEvery} ({want:P2}).");
    }

    /// <summary>THE WORLD CAN TELL PASS FROM FAIL. A sweep in which every car is the same car would satisfy
    /// nothing above by accident and everything by construction, so both outcomes are asserted to occur —
    /// this repo's fifth named bug class, written down as a fact.</summary>
    [Fact]
    public void TheSweepReallyDealsBothCars()
    {
        (int chips, int total) = SweepTheDeal();

        Assert.True(total > 1000, $"only {total} cars were dealt; the ratio above proves little.");
        Assert.True(chips > 0, "not one car in the sweep had the chip — the deal never fires.");
        Assert.True(chips < total, "every car in the sweep had the chip — the deal never declines.");
    }

    /// <summary>DEALT, NOT ROLLED. The same booth on the same watch deals the same car however many times it
    /// is asked — which is what lets a captain read an offer card without the car changing under him, and
    /// what makes the ratio above a property of the world rather than of the frame rate.
    ///
    /// <para><b>Proven RED</b> by seeding off a moving clock instead of the passed bucket.</para></summary>
    [Fact]
    public void TheSameBoothOnTheSameWatch_DealsTheSameCarEveryTime()
    {
        for (int bucket = 0; bucket < 50; bucket++)
        {
            bool first = CompromisingChip.BetweenTheSeats(bucket, 771);
            for (int again = 0; again < 5; again++)
            {
                Assert.Equal(first, CompromisingChip.BetweenTheSeats(bucket, 771));
            }
        }
    }

    /// <summary>…AND IT IS ITS OWN STREAM. The hand-off address is rolled off the same two numbers one line
    /// up in <c>MakeFetchOffer</c>; if the two shared a seed tag, which car you got and where you took it
    /// would move together for ever after, and nobody would notice for a year.</summary>
    [Fact]
    public void TheDealDoesNotMoveWithTheHandOffAddress()
    {
        int agreements = 0;
        int seen = 0;
        for (int bucket = 0; bucket < 400; bucket++)
        {
            bool chip = CompromisingChip.BetweenTheSeats(bucket, 4242);
            bool address = DiceRule.Roll(
                DiceRule.Seed("mission-range", bucket, 4242), CompromisingChip.OneInEvery).Face == 1;
            seen++;
            if (chip == address)
            {
                agreements++;
            }
        }

        // Two independent d4s agree about 5/8 of the time. Lockstep would be 100%.
        Assert.True(agreements < seen, "the deal and the address agreed on every single booth — one stream.");
    }

    // ── THE OBJECT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>WHAT IT IS: the canon glyph and name, and the pocket the satchel's own words put it in — a
    /// file on somebody, which is what leverage is and why no new <c>Kind</c> was appended for it.</summary>
    [Fact]
    public void TheChipIsAFileOnSomebody_ByGlyphAndByName()
    {
        Assert.Equal("💾", CompromisingChip.Glyph);
        Assert.Equal(TheName, CompromisingChip.Name);

        Satchel.Item chip = CompromisingChip.Found();
        Assert.Equal(Satchel.Kind.Dirt, chip.Kind);
        Assert.Equal(CompromisingChip.FindId, chip.Id);
        Assert.True(CompromisingChip.IsTheChip(chip));

        // …and it is not just "any Dirt": another file on somebody is not this one.
        Assert.False(CompromisingChip.IsTheChip(new Satchel.Item(Satchel.Kind.Dirt, "somebody-else")));
        Assert.False(CompromisingChip.IsTheChip(new Satchel.Item(Satchel.Kind.Paper, CompromisingChip.FindId)));
    }

    /// <summary>IT HAS A CARD, AND THE CARD IS THE CANON LINE. Asked of <see cref="CarriedObject.Card"/> —
    /// the shipping dispatch every 🔍 press goes through — rather than of the constant, because a card that
    /// exists in Core and has no arm in the switch is a look button that never lights.
    ///
    /// <para><b>Proven RED</b> by removing the <c>Kind.Dirt when IsTheChip</c> arm: the chip falls to the
    /// default and the card is null.</para></summary>
    [Fact]
    public void LookingAtIt_RaisesTheCanonCardThroughTheShippingDispatch()
    {
        CarriedObject.Reveal? card = CarriedObject.Card(CompromisingChip.Found(), "the-space-bar");

        Assert.NotNull(card);
        Assert.Equal(TheCard, card!.Value.Story);
        Assert.Equal(CompromisingChip.CardLabel, card.Value.Label);
        Assert.Contains(CompromisingChip.Glyph, card.Value.Label, StringComparison.Ordinal);

        // Caption-only, deliberately: a card of the photographs would name the two people the client is
        // paying a stranger not to be able to name.
        Assert.Equal(string.Empty, card.Value.ArtUrl);

        // …and a plain file on somebody still has no card, so "it has one" means something.
        Assert.Null(CarriedObject.Card(new Satchel.Item(Satchel.Kind.Dirt, "somebody-else"), "the-space-bar"));
    }

    /// <summary>THE POCKET LETS GO. All three endings spend it, so a spend that quietly left it behind would
    /// hand the captain a chip he could sell twice.</summary>
    [Fact]
    public void SpendingIt_TakesItOutOfThePocketAndLeavesTheRest()
    {
        var other = new Satchel.Item(Satchel.Kind.Dirt, "somebody-else");
        IReadOnlyList<Satchel.Item> carried = Satchel.Add(Satchel.Add([], other), CompromisingChip.Found());
        Assert.NotNull(CompromisingChip.InThePocket(carried));

        IReadOnlyList<Satchel.Item> after = CompromisingChip.Spend(carried);

        Assert.Null(CompromisingChip.InThePocket(after));
        Assert.Contains(after, i => i.Id == other.Id);
    }

    // ── THE THREE ENDINGS' ARITHMETIC ─────────────────────────────────────────────────────────────────

    /// <summary>THE FENCE PAYS MORE, AND HE PAYS IT OUT OF THE CONTRACT'S OWN NUMBER. The premium is the
    /// dark web's own price for a certainty, taken through the market's own function — so repricing the
    /// market reprices the betrayal, and nobody has to remember that this file exists.
    ///
    /// <para>The last assertion is the one that matters: the premium GROWS with the contract. A typed bonus
    /// would pass "more than the contract" on every row and fail this, which is the whole difference between
    /// derived and hard-coded.</para>
    ///
    /// <para><b>Proven RED</b> by replacing the derivation with <c>pay + 1260</c> — the value it happens to
    /// take at the shipping 4,200 cr contract, which passes the first two rows and dies on the third.</para>
    /// </summary>
    [Fact]
    public void TheFencePaysMoreThanTheContract_DerivedFromTheContract()
    {
        foreach (int pay in new[] { 40, 900, 4200, 8400, 25_000 })
        {
            int price = CompromisingChip.FencePrice(pay);

            Assert.Equal(pay + IntelMarket.SellPrice(CompromisingChip.CertainAsPhotographs, pay), price);
            Assert.True(price > pay, $"the fence paid {price} for a {pay} cr contract — that is not more.");
        }

        // The shipping roadster contract, priced end to end, so the number a captain actually sees is pinned
        // as well as the rule that makes it.
        Assert.Equal(5460, CompromisingChip.FencePrice(4200));

        // The derivation holds all the way down, including where the premium rounds away to nothing — a
        // three-credit job is not a thing the fiction has, but a function that special-cased it would be a
        // second rule hiding under the first.
        Assert.Equal(1, CompromisingChip.FencePrice(1));

        // Proportional, not a flat tip: double the job, double the premium.
        int small = CompromisingChip.FencePrice(4200) - 4200;
        int large = CompromisingChip.FencePrice(8400) - 8400;
        Assert.Equal(small * 2, large);
    }

    /// <summary>ONE BAND, AND THE BAND IS #715's OWN STEP. Not a number this feature chose: the heat a sale
    /// costs is the rung the patrol ladder is already measured in, so a retuned ladder retunes this.</summary>
    [Fact]
    public void TheSaleCostsExactlyOneOfTheLaddersOwnBands()
    {
        Assert.Equal(IllegalHeat.HeatPerRung, IllegalHeat.ABand);
        Assert.True(IllegalHeat.ABand > 0, "a band of heat that costs nothing is not a consequence.");

        // One band really is one rung further up the ladder the round already keeps.
        Assert.Equal(IllegalHeat.StartingRung(0) + 1, IllegalHeat.StartingRung(IllegalHeat.ABand));
    }

    /// <summary>THE HOLE LISTS IT. #223's chest takes a free-form manifest line; this is the one entry in
    /// the game that is not a traded class, it is listed by the chip's own canon name, and it is flagged HOT
    /// (#202's flag) because a chest of photographs is evidence whatever the captain's heat happens to be.
    ///
    /// <para><b>Proven RED</b> by flagging it cold: the chest stops counting a hot unit.</para></summary>
    [Fact]
    public void BuryingIt_PutsTheChipOnTheChestsManifestAndFlagsItHot()
    {
        CacheCargo line = CompromisingChip.Manifest();

        Assert.Equal(TheName, line.CargoClass);
        Assert.Equal(1, line.Units);
        Assert.True(line.Hot);

        // …and the chest that carries it really does read as carrying a hot unit.
        TreasureCache chest = CacheMint.Bury(
            "cache-you-0", "phobos", 0, coin: 0, cargo: [line],
            buriedSimTime: 0, owner: "you", playerOwned: true);
        Assert.Equal(1, chest.HotCargoUnits);
        Assert.Equal(1, chest.TotalCargoUnits);
    }

    // ── THE WORDS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>VERBATIM. The three sentences canon authored, character for character.</summary>
    [Fact]
    public void TheThreeSentences_AreExactlyWhatCanonAuthored()
    {
        Assert.Equal(TheCard, CompromisingChip.LookCardLine);
        Assert.Equal(TheClient, CompromisingChip.ClientLine);
        Assert.Equal(TheFence, CompromisingChip.FenceLine);
        Assert.Equal([TheCard, TheClient, TheFence], CompromisingChip.EveryLine().ToArray());
    }

    /// <summary>AND NOT A FOURTH. Read by reflection off the type rather than from a list here, because a
    /// list would be edited by the same hand that added the sentence. Three sentences and four names — the
    /// glyph, the item name, the satchel id and the seed tag — and a fifth name or a fourth sentence has to
    /// be argued for in a diff to this array, which is the point.
    ///
    /// <para><b>Proven RED</b> by adding a spare <c>public const string</c> to the type.</para></summary>
    [Fact]
    public void TheTypePublishesExactlyTheseStringsAndNoOthers()
    {
        string[] published = [.. typeof(CompromisingChip)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(s => s, StringComparer.Ordinal)];

        Assert.True(published.Length > 0,
            "the reflection sweep found no string constants on CompromisingChip at all — it is reading the "
            + "wrong type, and would then pass over any sentence anybody added.");

        string[] owned = [.. new[]
        {
            CompromisingChip.Glyph, TheName, TheCard, TheClient, TheFence,
            "roadster-data-chip", "chip-between-the-seats",
        }.OrderBy(s => s, StringComparer.Ordinal)];

        Assert.Equal(owned, published);
    }

    /// <summary>THE CLIENT NEVER SAYS WHOSE PHOTOGRAPHS, AND THE FENCE NEVER NAMES THE BUYER. Canon's two
    /// standing laws for this feature, asked of the sentences rather than assumed from them.</summary>
    [Fact]
    public void NeitherOfThemNamesAnybody()
    {
        // He speaks about looking and about saying, and about nobody.
        Assert.DoesNotContain("photograph", CompromisingChip.ClientLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wife", CompromisingChip.ClientLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("minister", CompromisingChip.ClientLine, StringComparison.OrdinalIgnoreCase);

        // He names an appetite. The buyer stays "a buyer" and "he", and never acquires a name, a company or
        // a rank — the three ways a sentence like this usually leaks one.
        Assert.Contains("a buyer", CompromisingChip.FenceLine, StringComparison.Ordinal);
        foreach (string leak in new[] { "Kaamos", "Nebula", "Minister", "Reever", "the Hive", "Gilt" })
        {
            Assert.DoesNotContain(leak, CompromisingChip.FenceLine, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>THE RESERVED WORD IS ABSENT (docs/worldbuilding-notes.md §8) — from every sentence, from
    /// every name, and from the source file itself, docblocks included.</summary>
    [Fact]
    public void NothingHereEverSpeaksTheReservedWord()
    {
        foreach (string line in CompromisingChip.EveryLine().Append(TheName).Append(CompromisingChip.RowLabel))
        {
            Assert.DoesNotContain(TheReservedWord, line, StringComparison.OrdinalIgnoreCase);
        }

        string source = File.ReadAllText(Path.Combine(CoreSource(), "CompromisingChip.cs"));
        Assert.DoesNotContain(TheReservedWord, source, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>ONE PUBLISHER. Each sentence is typed as a literal in exactly one source file, and that file
    /// is Core's — so the pickup, the counter, the desk and the hole cannot drift into four readings of the
    /// same line.</summary>
    [Fact]
    public void EachSentenceIsWrittenDownExactlyOnceInTheSource()
    {
        string src = Path.Combine(RepoRoot(), "src");

        foreach (string sentence in CompromisingChip.EveryLine())
        {
            var carriers = new List<string>();
            foreach (string file in Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file) is not (".cs" or ".razor")
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                if (File.ReadAllText(file).Contains($"\"{sentence}", StringComparison.Ordinal))
                {
                    carriers.Add(Path.GetFileName(file));
                }
            }

            Assert.True(carriers.Count == 1,
                $"\"{sentence}\" is typed in {carriers.Count} file(s) ({string.Join(", ", carriers)}).");
            Assert.Equal("CompromisingChip.cs", carriers[0]);
        }
    }

    // ── THE BIRD ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE OWNER'S OWN TWO LINES, verbatim, and each one is ONE line rather than a rotation: this is
    /// a two-beat gag with a punchline, and a set-up that rotates is a different joke every time.
    ///
    /// <para><b>Proven RED</b> by dropping a second line into either row — the rotation assertion catches
    /// it at counter 1.</para></summary>
    [Fact]
    public void TheParrotAsksWhereTheCarIsAndThenAnswersItself()
    {
        Assert.Equal("DUDE. WHERE. Is. The CAR?!", Parrot.Line(Parrot.Squawk.CarHunt, 0));
        Assert.Equal("You found your CAAAR!", Parrot.Line(Parrot.Squawk.CarFound, 0));

        for (int counter = 0; counter < 8; counter++)
        {
            Assert.Equal(Parrot.Line(Parrot.Squawk.CarHunt, 0), Parrot.Line(Parrot.Squawk.CarHunt, counter));
            Assert.Equal(Parrot.Line(Parrot.Squawk.CarFound, 0), Parrot.Line(Parrot.Squawk.CarFound, counter));
        }
    }

    /// <summary>APPENDED, NEVER INSERTED. <c>Parrot.Lines</c> is indexed by the enum ordinal, so a kind
    /// slipped into the middle would silently reassign every sentence below it to the wrong event. The two
    /// new kinds are the last two, and every kind that was already there still has words.</summary>
    [Fact]
    public void TheTwoNewSquawksAreTheLastTwoOrdinalsAndNobodyElseMoved()
    {
        Parrot.Squawk[] all = Enum.GetValues<Parrot.Squawk>();

        Assert.Equal(Parrot.Squawk.CarFound, all[^1]);
        Assert.Equal(Parrot.Squawk.CarHunt, all[^2]);
        Assert.Equal("FIRING SOLUTION, CAPTAIN!", Parrot.Line(Parrot.Squawk.FiringSolution, 0));

        foreach (Parrot.Squawk kind in all)
        {
            Assert.False(string.IsNullOrWhiteSpace(Parrot.Line(kind, 0)), $"{kind} has no words.");
        }
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every car a captain could be dealt across forty watches at a hundred berths — the two
    /// numbers a booth actually has, swept over the space they live in.</summary>
    private static (int Chips, int Total) SweepTheDeal()
    {
        int chips = 0;
        int total = 0;
        for (int bucket = 0; bucket < 40; bucket++)
        {
            for (int salt = 500; salt < 1500; salt += 10)
            {
                total++;
                if (CompromisingChip.BetweenTheSeats(bucket, salt))
                {
                    chips++;
                }
            }
        }

        return (chips, total);
    }

    private static string CoreSource() => Path.Combine(RepoRoot(), "src", "SpaceSails.Core");

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }
}
