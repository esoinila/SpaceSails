using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #319 slice 1 · <b>BURY ANYTHING LIGHT.</b>
///
/// <para>Owner, live 2026-07-18: <i>"Let's add another options to hide onto the planet / site. Anything from
/// the inventory that is light enough … hiding evidence of our piracy without losing it. Maybe we steal
/// something too important to risk carrying it around, like a superchip prototype etc."</i></para>
///
/// <h3>What is actually at risk, and what these guards are shaped against</h3>
/// <list type="number">
/// <item><b>The weight rule growing a list of kinds.</b> The obvious implementation is a switch over
/// <c>Satchel.Kind</c>, and it is wrong in the maintenance direction rather than the arithmetic one: it
/// would stop covering the world the next time a kind is appended, silently, which is this repo's fifth
/// named bug class with a long fuse. <see cref="TheWeightRuleIsTheSatchelsOwnArithmeticAndNeverAListOfKinds"/>
/// sweeps the source for it and <see cref="NoKindIsPrivilegedAndNoKindIsRefused"/> drives every kind the
/// enum has today.</item>
/// <item><b>The buried thing getting a ledger of its own.</b> Clause 2 and 3 of the issue are a list of
/// promises the CHEST already keeps — persistence, rebirth, the same <see cref="CacheSafety"/> odds, the
/// same #316 forensics — and the only way to keep them exactly is to be the chest.
/// <see cref="TheReturnRollReadsTheSameThreeTermsForAThingAsForCoin"/> holds the odds half at every point of
/// the lattice, and <see cref="TheVaultCarriesWhatCameOutOfTheCoat"/> the persistence half.</item>
/// <item><b>A vacuous rule.</b> Everything the satchel can hold today costs one place or none, so
/// <c>IsLightEnough</c> asked of a shipping item can only say yes — a guard written only against items
/// would be green against a rule that returned <c>true</c>. So the rule is driven at the level it is
/// WRITTEN at (<see cref="ThereIsAThresholdAndItRefusesWhatIsOverIt"/>, a cost the world cannot build yet)
/// and the item overload is held to it by equivalence
/// (<see cref="AskingTheThingIsAskingWhatItCostsTheSatchel"/>).</item>
/// </list>
///
/// <h3>Red proof (watched, quoted in the pull request)</h3>
/// <list type="bullet">
/// <item>Rewrite the rule as <c>item.Kind is not Satchel.Kind.Tool</c> — the source sweep fails naming
/// <c>Kind</c>, and the kind drive fails on Tool.</item>
/// <item>Return <c>true</c> from <c>IsLightEnough(int)</c> — the threshold drive fails at cost 2.</item>
/// <item>Drop <c>DepositCount</c> out of <c>HasContents</c> — the deposit-only chest reads as not worth
/// digging up.</item>
/// <item>Mint the deposit with <c>buried: null, padDistance: null</c> — the terms guard fails on the rung,
/// because a hole that comes back reading Exposed is not the safe the captain paid for.</item>
/// <item>Write the deposit key unconditionally (drop the <c>Count: &gt; 0</c> test) — the legacy byte
/// guard fails on a <c>"deposit"</c> in a chest's bytes.</item>
/// </list>
/// </summary>
public class TheMoonIsASafetyDepositBoxTests
{
    private const string Body = "phobos";
    private const int RidgeCamp = 1;

    // The two ends of the safety lattice, as a captain would describe them (TheHidingPlaceIsOneOracle's own
    // anchors, borrowed deliberately so the two files are arguing about the same world).
    private const double DeepCarryDu = 210.0;
    private const int FullPack = 3;

    // ── THE WEIGHT RULE ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONE WEIGHT RULE, AND IT IS THE SATCHEL'S OWN ARITHMETIC.</b> The issue's words: <i>"One weight
    /// threshold ('light enough to carry down the tube'), one honest rule."</i>
    ///
    /// <para>Read off the source rather than off behaviour, because the failure this guards against is not
    /// visible in behaviour TODAY — a kind list and the real rule agree on every object the game can build
    /// right now. What they disagree about is the FUTURE: a kind list stops covering the world the next time
    /// somebody appends to <c>Satchel.Kind</c>, and nothing would go red. So the claim is about the text.</para>
    ///
    /// <para>Comments are stripped first and the word is then forbidden outright: the prose in that file
    /// talks about kinds at length (it has to — it is explaining why there is no list), and a guard that
    /// could not tell an argument from a switch would have to be written loosely enough to miss the switch.</para>
    /// </summary>
    [Fact]
    public void TheWeightRuleIsTheSatchelsOwnArithmeticAndNeverAListOfKinds()
    {
        string file = File.ReadAllText(
            Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Core", "CacheDeposit.cs"));

        // Sliced between the source's own two fences — everything that DECIDES how heavy a thing is lives
        // between them. Below them the file goes on to ask WHICH ROW a pick is, which is the satchel's own
        // identity for a row (kind and id, exactly as Satchel.Remove matches one) and is not a weight.
        int opens = file.IndexOf("THE WEIGHT RULE BEGINS", StringComparison.Ordinal);
        int closes = file.IndexOf("THE WEIGHT RULE ENDS", StringComparison.Ordinal);
        Assert.True(opens >= 0 && closes > opens, "the weight rule's fences are gone — this guard has drifted.");

        string code = Decommented(file[opens..closes]);

        Assert.Contains("Satchel.SpaceCostOf", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Kind", code, StringComparison.Ordinal);

        // …and it is a NAMED threshold rather than a literal at the places that ask. Four times over this
        // project has paid for a fact transcribed at its call sites.
        Assert.Contains("HeaviestBuriableSpaceCost", code, StringComparison.Ordinal);
    }

    /// <summary>THE THRESHOLD IS REAL — driven at the level it is written at, over a cost the world cannot
    /// build an object for yet. This is the guard that stops the rule being <c>=&gt; true</c> with a
    /// docblock over it.</summary>
    [Fact]
    public void ThereIsAThresholdAndItRefusesWhatIsOverIt()
    {
        Assert.True(CacheDeposit.IsLightEnough(0));                                      // a page folded twice
        Assert.True(CacheDeposit.IsLightEnough(CacheDeposit.HeaviestBuriableSpaceCost)); // one place, and the last one

        for (int over = CacheDeposit.HeaviestBuriableSpaceCost + 1; over <= 6; over++)
        {
            Assert.False(CacheDeposit.IsLightEnough(over));
        }

        Assert.False(CacheDeposit.IsLightEnough(-1)); // not a lighter thing — a broken one
    }

    /// <summary>…AND THE THING IS ASKED BY ASKING WHAT IT COSTS. The equivalence, held over every object the
    /// world can actually produce, which is what ties the drivable rule above to the shipping one.</summary>
    [Fact]
    public void AskingTheThingIsAskingWhatItCostsTheSatchel()
    {
        foreach (Satchel.Item item in EveryShapeOfThingAPocketCanHold())
        {
            Assert.Equal(CacheDeposit.IsLightEnough(Satchel.SpaceCostOf(item)), CacheDeposit.IsLightEnough(item));
        }
    }

    /// <summary>NO KIND IS PRIVILEGED AND NO KIND IS REFUSED. Every kind the enum has, including any appended
    /// after this was written — the sweep is over <c>Enum.GetValues</c>, so a new kind joins the guard the
    /// day it joins the game rather than the day somebody remembers.</summary>
    [Fact]
    public void NoKindIsPrivilegedAndNoKindIsRefused()
    {
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            var item = new Satchel.Item(kind, $"a-plain-{kind}");
            Assert.True(CacheDeposit.IsLightEnough(item), $"{kind} is in a pocket and cannot go in a hole");
        }
    }

    /// <summary>THE CHOOSER'S OWN READ. What a pocket offers is what is light enough in it, in the pocket's
    /// own order — a chooser that reshuffled itself between two looks at one satchel would be a control that
    /// moves under the finger.</summary>
    [Fact]
    public void ThePocketOffersWhatIsLightEnoughInTheOrderItIsCarried()
    {
        List<Satchel.Item> pocket = [.. EveryShapeOfThingAPocketCanHold()];

        IReadOnlyList<Satchel.Item> offered = CacheDeposit.LightEnoughIn(pocket);

        Assert.Equal(pocket.Where(CacheDeposit.IsLightEnough).ToArray(), offered.ToArray());
        Assert.Empty(CacheDeposit.LightEnoughIn(null));
        Assert.Empty(CacheDeposit.LightEnoughIn([]));
    }

    /// <summary>THE PICK IS THE ROW AS IT IS NOW, not as it was picked at the shuttle door. A whole excursion
    /// happens in between: rounds get fired, papers get read, folders come apart, things get set down.</summary>
    [Fact]
    public void WhatGoesInTheHoleIsTheRowAsTheCoatHoldsItNow()
    {
        var picked = new Satchel.Item(Satchel.Kind.Rounds, "loose", 4);
        List<Satchel.Item> afterTwoWereFired = [new(Satchel.Kind.Rounds, "loose", 2)];

        Assert.True(CacheDeposit.StillCarried(afterTwoWereFired, picked));
        Assert.Equal(2, CacheDeposit.AsCarried(afterTwoWereFired, picked)!.Value.Count);

        // …and a thing that is gone is gone. No exception, no substitute, nothing said.
        Assert.False(CacheDeposit.StillCarried([], picked));
        Assert.Null(CacheDeposit.AsCarried([], picked));
        Assert.Null(CacheDeposit.AsCarried([new Satchel.Item(Satchel.Kind.Rounds, "other", 9)], picked));
    }

    /// <summary>THE PURE BUILDER IS WHERE THE PICK IS WEIGHED. Both boarding routes go through
    /// <see cref="ShuttleExcursion.Pack"/>, so it is the one place a thing too heavy for the tube can stop
    /// being a thing that can be buried — asked of the source, because no object this build can construct
    /// weighs enough to drive it.</summary>
    [Fact]
    public void ThePackIsWhereThePickIsWeighed()
    {
        string pack = BodyOf(
            Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Core", "ShuttleExcursion.cs"),
            "public static ChestLoad Pack(");

        Assert.Contains("CacheDeposit.IsLightEnough", pack, StringComparison.Ordinal);
    }

    /// <summary>A LOAD WITH NOTHING BUT A FILE IN IT IS A LOAD. The empty-sling prompt is about walking down
    /// with nothing to do, and burying evidence is the most deliberate errand in the game.</summary>
    [Fact]
    public void APickAloneIsNotAnEmptySling()
    {
        ShuttleExcursion.ChestLoad justAFile = ShuttleExcursion.Pack(
            coin: 0, credits: 5000, [], new Satchel.Item(Satchel.Kind.Dirt, "a-file"));

        Assert.False(justAFile.IsEmpty);
        Assert.True(ShuttleExcursion.Pack(0, 5000, []).IsEmpty);
    }

    // ── THE HOLE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A HOLE WITH NOTHING IN IT BUT A FILE ON SOMEBODY IS WORTH DIGGING UP. This is the read that
    /// tells a captain whether his own ✗ still means anything; a deposit-only chest answering "no" would be
    /// the game writing off the one thing the whole issue is about.
    ///
    /// <para>…and the manifest COUNTS it and never names it: a hoard line read over a captain's shoulder in
    /// a bar would otherwise be the one place in the game where burying evidence advertises evidence.</para></summary>
    [Fact]
    public void AHoleWithNothingButAFileInItIsWorthDiggingUp()
    {
        var ledger = new CacheLedger();
        TreasureCache hole = ledger.Bury(
            Body, coin: 0, [], simTime: 61234.5, owner: "you", playerOwned: true,
            reeverLevel: 0, digX: -6, digY: -232, siteIndex: RidgeCamp,
            buried: true, padDistance: DeepCarryDu,
            deposit: [new Satchel.Item(Satchel.Kind.Dirt, "roadster-data-chip")]);

        Assert.True(hole.HasContents);
        Assert.Equal(1, hole.DepositCount);
        Assert.Equal("1 thing from the satchel", hole.ContentsLine());
        Assert.DoesNotContain("roadster", hole.ContentsLine(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 thing from the satchel", ledger.HoardLine(), StringComparison.Ordinal);

        // …and beside coin and cargo it is one more part of one line, not a second sentence.
        TreasureCache mixed = ledger.Bury(
            Body, coin: 1200, [new CacheCargo("He3", 4, Hot: true)], 61234.5, "you", true,
            deposit: [new Satchel.Item(Satchel.Kind.Paper, "p1"), new Satchel.Item(Satchel.Kind.Paper, "p2")]);
        Assert.Equal("1,200 cr + 4 units (4 hot) + 2 things from the satchel", mixed.ContentsLine());
    }

    /// <summary>AN EMPTY PICK IS NO PICK. The mint stores null rather than an empty list, which is what keeps
    /// a chest a chest byte for byte in the vault.</summary>
    [Fact]
    public void AChestWithNothingOutOfTheCoatCarriesNoList()
    {
        TreasureCache chest = new CacheLedger().Bury(Body, 900, [], 1000, "you", true, deposit: []);

        Assert.Null(chest.Deposit);
        Assert.Equal(0, chest.DepositCount);
        Assert.Equal("900 cr", chest.ContentsLine());
    }

    /// <summary>
    /// <b>THE RETURN ROLL READS THE SAME THREE TERMS FOR A THING AS FOR COIN.</b> The issue: <i>"subject to
    /// the same CacheSafety.Read odds the chest gets (buried, pad distance, watchdog level — call the oracle,
    /// never restate it)."</i>
    ///
    /// <para>Held over the whole lattice rather than at one point, and held BOTH ways: the deposit-only hole
    /// reads exactly what the coin chest with the same terms reads, AND the oracle called directly with those
    /// three terms reads the same thing again. A deposit path that had quietly acquired a discount — or a
    /// mint that dropped the terms on the floor for a hole with no coin in it — fails here.</para>
    /// </summary>
    [Fact]
    public void TheReturnRollReadsTheSameThreeTermsForAThingAsForCoin()
    {
        foreach (double? carry in new double?[] { null, 0.0, 40.0, DeepCarryDu })
        {
            foreach (bool? shovel in new bool?[] { null, false, true })
            {
                for (int pack = 0; pack <= FullPack; pack++)
                {
                    var ledger = new CacheLedger();
                    TreasureCache coin = ledger.Bury(
                        Body, 1200, [], 61234.5, "you", true, pack, -6, -232, RidgeCamp, shovel, carry);
                    TreasureCache thing = ledger.Bury(
                        Body, 0, [], 61234.5, "you", true, pack, -6, -232, RidgeCamp, shovel, carry,
                        deposit: [new Satchel.Item(Satchel.Kind.Dirt, "a-file")]);

                    Assert.Equal(coin.Safety, thing.Safety);
                    Assert.Equal(CacheSafety.Read(carry, shovel, pack), thing.Safety);

                    // …and the husk term rides the same way, which is what makes #316's forensics one rule.
                    Assert.Equal(coin.SafetyWith(5), thing.SafetyWith(5));
                }
            }
        }
    }

    /// <summary>…AND THE DICE THEMSELVES. The watch's own scan, run day by day over a long span: two chests
    /// with the same terms and different contents are found on the same days or on none.</summary>
    [Fact]
    public void TheWatchLosesAThingExactlyWhenItWouldLoseTheCoin()
    {
        var ledger = new CacheLedger();
        TreasureCache coin = ledger.Bury(Body, 1200, [], 0, "you", true, 1, -6, -232, RidgeCamp, true, 40.0);
        TreasureCache thing = ledger.Bury(
            Body, 0, [], 0, "you", true, 1, -6, -232, RidgeCamp, true, 40.0,
            deposit: [new Satchel.Item(Satchel.Kind.Paper, "a-manifest")]);

        // The ids differ, so the two chests roll different dice — what must match is the THRESHOLD, which is
        // what the rung and the per-mille both are. Held against the oracle and against each other.
        Assert.Equal(coin.Safety.ChancePerMille, thing.Safety.ChancePerMille);

        // And the scan itself runs for a deposit-only hole rather than skipping it. Over 400 days at ~3% a
        // day a chest is lost with probability 1 - 0.97^400, which is not a coin flip: this is an assertion
        // about the watch being WIRED, and it goes red the moment a deposit-only cache is exempted.
        Assert.NotNull(DiscoveryRule.DiscoveredWithin(thing, 0, 400 * DiscoveryRule.PeriodSeconds));
    }

    /// <summary>DETERMINISM. Two identical burials of the same thing on the same ground at the same instant
    /// mint the same hole, contents and all — Core is pure and the hoard is a saved record.</summary>
    [Fact]
    public void TwoIdenticalBurialsMintTheSameHole()
    {
        static TreasureCache Mint() => CacheMint.Bury(
            "cache-you-7", Body, 7, 0, [], 61234.5, "you", true, 2, -6, -232, RidgeCamp, true, DeepCarryDu,
            [new Satchel.Item(Satchel.Kind.Dirt, "a-file")]);

        // Compared with the coat's list lifted out, because a record struct compares an IReadOnlyList by
        // REFERENCE and two mints build two lists — an equality that passed here would be measuring the
        // allocator, not the mint. The rows themselves are then compared as rows.
        Assert.Equal(Mint() with { Deposit = null }, Mint() with { Deposit = null });
        Assert.Equal(Mint().Deposit!.ToArray(), Mint().Deposit!.ToArray());
        Assert.Equal(Mint().Safety, Mint().Safety);
    }

    // ── THE VAULT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>WHAT CAME OUT OF THE COAT SURVIVES THE VAULT, through the SAME string a satchel row is saved
    /// as everywhere else in this game. Two spellings of one row is how a chest dug up after a reload comes
    /// back holding something the captain never buried.</summary>
    [Fact]
    public void TheVaultCarriesWhatCameOutOfTheCoat()
    {
        var ledger = new CacheLedger();
        Satchel.Item[] coat =
        [
            new(Satchel.Kind.Dirt, "roadster-data-chip"),
            new(Satchel.Kind.Rounds, "loose", 6),
        ];
        ledger.Bury(Body, 0, [], 61234.5, "you", true, 2, -6, -232, RidgeCamp, true, DeepCarryDu, coat);

        var restored = new CacheLedger();
        VaultMapper.Apply(VaultMapper.ToSection(ledger), restored);

        TreasureCache back = restored.Caches.Single();
        Assert.Equal(coat, back.Deposit!.ToArray());
        Assert.Equal(ledger.Caches.Single().Safety, back.Safety);
    }

    /// <summary>A CHEST WITH NOTHING OF HIS IN IT WRITES NO KEY. The digest is taken over the payload, so an
    /// extra <c>"deposit": null</c> per chest would change the checksum of every hoard ever saved and open an
    /// honest captain's voyage flying the 📛 tampered flag.</summary>
    [Fact]
    public void AnOrdinaryChestWritesNoDepositKeyAtAll()
    {
        var ledger = new CacheLedger();
        ledger.Bury(Body, 900, [new CacheCargo("He3", 3, Hot: false)], 61234.5, "you", true);

        string saved = VaultSerializer.Save(new Vault
        {
            SavedSimTime = 90000,
            Caches = VaultMapper.ToSection(ledger),
        });

        Assert.DoesNotContain("deposit", saved, StringComparison.OrdinalIgnoreCase);
        Assert.False(VaultSerializer.Load(saved).Tampered);
    }

    /// <summary>A ROW THIS BUILD CANNOT READ IS DROPPED, NOT THROWN OVER — the satchel section's own
    /// tolerance, because a mystery object is not worth losing a voyage for. And a hole whose every row is
    /// unreadable comes back as a hole with nothing of his in it rather than as an empty list, so a re-save
    /// writes no key.</summary>
    [Fact]
    public void AnUnreadableRowIsDroppedAndNeverLosesTheHole()
    {
        var section = new CachesSection
        {
            NextMintIndex = 1,
            Caches =
            [
                new CacheRecord
                {
                    Id = "cache-you-0", BodyId = Body, LandmarkName = "the monolith", Bearing = "spinward",
                    Paces = 40, Coin = 500, Cargo = [], BuriedSimTime = 12000, Owner = "you",
                    PlayerOwned = true, Deposit = ["not a row at all", "3:1:a-paper"],
                },
            ],
        };

        var ledger = new CacheLedger();
        VaultMapper.Apply(section, ledger);

        TreasureCache back = ledger.Caches.Single();
        Assert.Equal(1, back.DepositCount);
        Assert.Equal(500, back.Coin);
    }

    // ── WHAT IT SAYS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE TWO STRINGS, PINNED — the canon line character for character, and the plate. Reserved
    /// words swept off both: a register a captain tells himself while he digs is exactly the sort of
    /// sentence that starts explaining the ground it is being dug into.</summary>
    [Fact]
    public void TheRegisterIsTheAuthoredLineAndNamesNothingReserved()
    {
        Assert.Equal(
            "Not coin this time. Something that is safer in the ground than in the hold.",
            CacheDeposit.NotCoinThisTime);
        Assert.Equal("BURY A THING FROM THE SATCHEL", CacheDeposit.RowLabel);

        Assert.Equal(
            [CacheDeposit.RowLabel, CacheDeposit.NotCoinThisTime],
            CacheDeposit.AllProse().ToArray());

        string[] forbidden =
        [
            "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
            "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
        ];
        foreach (string text in CacheDeposit.AllProse().Append(CacheDeposit.ManifestLine(1))
                     .Append(CacheDeposit.ManifestLine(3)))
        {
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        // …and there is no third sentence hiding on the type that no canon grep can see.
        string[] published = [.. typeof(CacheDeposit)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)];
        Assert.Equal(CacheDeposit.AllProse().OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            published.OrderBy(s => s, StringComparer.Ordinal).ToArray());
    }

    /// <summary>THE MANIFEST COUNTS, IN ENGLISH. One thing is a thing; two are things.</summary>
    [Fact]
    public void TheManifestSaysOneThingAndTwoThings()
    {
        Assert.Equal("1 thing from the satchel", CacheDeposit.ManifestLine(1));
        Assert.Equal("2 things from the satchel", CacheDeposit.ManifestLine(2));
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One of everything a pocket can hold, including the one object whose weight is not one: the
    /// page torn out of a document (<see cref="Satchel.FoldedSheetSpace"/>), which the world builds through
    /// <see cref="PageGranularity"/> and no test may spell by hand.</summary>
    private static IEnumerable<Satchel.Item> EveryShapeOfThingAPocketCanHold()
    {
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            yield return new Satchel.Item(kind, $"a-plain-{kind}");
        }

        yield return new Satchel.Item(Satchel.Kind.Paper, PageGranularity.SheetIdOf("a-plain-Paper"));
        yield return new Satchel.Item(Satchel.Kind.Paper, PageGranularity.BulkIdOf("a-plain-Paper"));
    }

    /// <summary>The source with every comment cut out of it, so a guard about a SWITCH cannot be defeated —
    /// or tripped — by a paragraph arguing about switches.</summary>
    private static string Decommented(string source) =>
        Regex.Replace(Regex.Replace(source, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");

    /// <summary>One method's body out of a file, by its signature — the idiom this suite already uses to ask
    /// whether a shipping method really calls the thing a feature hangs off.</summary>
    private static string BodyOf(string path, string signature)
    {
        string source = File.ReadAllText(path);
        int at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{signature} has been renamed — this guard has drifted.");
        int ends = source.IndexOf("\n    }", at, StringComparison.Ordinal);
        Assert.True(ends > at, $"{signature} has no end — this guard has drifted.");
        return source[at..ends];
    }
}
