using System.Reflection;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #426 · HER CHAIN OF OWNERS — the dread, and the discipline that keeps it dread.
///
/// <para><b>Owner, 2026-07-20, sailing through a storm:</b> <i>"The word 'storm' was spoken... makes one
/// think the ship's long chain of owners and maintenance organizations and insurers.. hope it all was
/// checked through the whole chain. 😅"</i></para>
///
/// <para>Fable's canon pass (2026-09-05) set three lines and four laws. These hold the laws: deterministic
/// per (hull, window); line 2 only for a hull that has worn another name; a hull with no chain says
/// nothing; and — the one that matters most on this ground — <b>every fact in the sentence is read off the
/// hull's own record and none of it is typed</b>. A guard that only checked the words would pass on a
/// version that hard-codes a yard, and hard-coding the yard is how a hull's plate and a hull's dread end up
/// naming two different builders in front of the player.</para>
/// </summary>
public class HerChainOfOwnersTests
{
    private const BindingFlags Constants = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>A hull with everything: a yard, a year, a former name, owners behind her.</summary>
    private static ShipHistory FullChain => new(
        Yard: "Ahti & Sons Shipwrights (the Rusty Roadstead, Mars orbit)",
        Year: 2291,
        FormerNames: ["ex-GILDED WAKE (sold for debt, name struck)"],
        OwnersDeep: 3,
        Condition: "The shine's long gone; the pedigree hasn't.");

    /// <summary>A hull with a yard record and no rename — line 2 must never be drawn for her.</summary>
    private static ShipHistory NeverRenamed => FullChain with { FormerNames = [], OwnersDeep = 0 };

    /// <summary>A brand-new hull with no chain at all: nobody before this owner, no former name, and no
    /// yard record. Fable's law: she says nothing.</summary>
    private static ShipHistory NoChain => new(
        Yard: "", Year: 0, FormerNames: [], OwnersDeep: 0, Condition: "");

    // A spread of window seeds — the caller folds hull and window into one of these.
    private static IEnumerable<ulong> Windows =>
        Enumerable.Range(0, 200).Select(i => DiceRule.Seed(0xC0FFEEUL, $"window:{i}"));

    // ── Determinism ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SameHullSameWindow_SaysTheSameThing_EveryTime()
    {
        foreach (ulong window in Windows)
        {
            string? a = ChainOfCustody.Line(FullChain, window);
            string? b = ChainOfCustody.Line(FullChain, window);
            Assert.Equal(a, b);
            Assert.Equal(ChainOfCustody.Which(FullChain, window), ChainOfCustody.Which(FullChain, window));
        }
    }

    [Fact]
    public void AcrossWindows_ShePicksUpEachOfTheThreeWorries()
    {
        // Not one canned line: a hull with a full chain can honestly speak all three, and over a spread of
        // windows she does. (Without this, a Which() that always returned candidate 0 would pass every
        // other guard in this file.)
        var seen = Windows.Select(w => ChainOfCustody.Which(FullChain, w)).Distinct().ToList();

        Assert.Contains(ChainOfCustody.Doubt.TheSurvey, seen);
        Assert.Contains(ChainOfCustody.Doubt.TheName, seen);
        Assert.Contains(ChainOfCustody.Doubt.TheYard, seen);
    }

    // ── Line 2 only with a former name ────────────────────────────────────────────────────────────────

    [Fact]
    public void TheNameLine_IsNeverDrawnForAHullThatNeverWoreAnother()
    {
        foreach (ulong window in Windows)
        {
            Assert.NotEqual(ChainOfCustody.Doubt.TheName, ChainOfCustody.Which(NeverRenamed, window));
        }
    }

    [Fact]
    public void TheNameLine_NamesTheNameSheActuallyWore_WithoutTheDossiersExPrefixOrFate()
    {
        ulong window = Windows.First(w => ChainOfCustody.Which(FullChain, w) == ChainOfCustody.Doubt.TheName);
        string line = ChainOfCustody.Line(FullChain, window)!;

        Assert.Contains("She was GILDED WAKE once", line, StringComparison.Ordinal);
        Assert.DoesNotContain("ex-", line, StringComparison.Ordinal);
        Assert.DoesNotContain("sold for debt", line, StringComparison.Ordinal); // the fate stays in the dossier
    }

    // ── A hull with no chain says nothing ─────────────────────────────────────────────────────────────

    [Fact]
    public void AHullWithNoChain_SaysNothingAtAll()
    {
        foreach (ulong window in Windows)
        {
            Assert.Equal(ChainOfCustody.Doubt.None, ChainOfCustody.Which(NoChain, window));
            Assert.Null(ChainOfCustody.Line(NoChain, window));
        }
    }

    [Fact]
    public void AHullWithNoYardRecord_NeverClaimsAYard()
    {
        // Renamed, but nobody kept her build paper: line 3 cannot be filled, so it is never drawn.
        ShipHistory noYard = FullChain with { Yard = "", Year = 0 };
        foreach (ulong window in Windows)
        {
            Assert.NotEqual(ChainOfCustody.Doubt.TheYard, ChainOfCustody.Which(noYard, window));
            Assert.NotNull(ChainOfCustody.Line(noYard, window)); // she still has a chain, and still speaks
        }
    }

    // ── The braces are filled from the RECORD, never typed ────────────────────────────────────────────

    [Fact]
    public void EveryFactInTheSentence_ComesOffTheHullsOwnRecord()
    {
        // The same three lines, spoken by two hulls with nothing in common, must differ exactly where the
        // record differs — which no amount of typed prose can fake.
        ShipHistory other = new(
            Yard: "Louhi Sunside Yards (Mercury, sunside)",
            Year: 2274,
            FormerNames: ["ex-SILVER PELICAN (a fire off Ceres, rebuilt)"],
            OwnersDeep: 5,
            Condition: "Tired plating, proud bones — the history is still in her frames.");

        foreach (ulong window in Windows)
        {
            switch (ChainOfCustody.Which(FullChain, window))
            {
                case ChainOfCustody.Doubt.TheYard:
                    Assert.Contains(FullChain.Yard, ChainOfCustody.Line(FullChain, window)!, StringComparison.Ordinal);
                    Assert.Contains("2291", ChainOfCustody.Line(FullChain, window)!, StringComparison.Ordinal);
                    Assert.Contains(other.Yard, ChainOfCustody.Line(other, window)!, StringComparison.Ordinal);
                    Assert.Contains("2274", ChainOfCustody.Line(other, window)!, StringComparison.Ordinal);
                    break;
                case ChainOfCustody.Doubt.TheName:
                    Assert.Contains("GILDED WAKE", ChainOfCustody.Line(FullChain, window)!, StringComparison.Ordinal);
                    Assert.Contains("SILVER PELICAN", ChainOfCustody.Line(other, window)!, StringComparison.Ordinal);
                    break;
                default:
                    break;
            }
        }
    }

    [Fact]
    public void TheOnlyStringsInTheFile_AreTheThreeTemplates()
    {
        // THE REFLECTION SWEEP. A typed yard, a typed former name, a typed year — any fourth string
        // constant on this type — is the bug this whole design is trying not to have. Three, and no more.
        var literals = typeof(ChainOfCustody)
            .GetFields(Constants)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.Equal(3, literals.Count);

        // And none of them names a yard, a glory name or a fate out of the pools — the facts must arrive
        // through a brace, never through the template.
        string[] pools = [.. Pool("Yards"), .. Pool("FormerNamePool"), .. Pool("Fates")];
        foreach (string template in literals)
        {
            foreach (string authored in pools)
            {
                Assert.DoesNotContain(authored, template, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void NoLineLeavesABraceOnScreen()
    {
        foreach (ShipHistory hull in new[] { FullChain, NeverRenamed, ShipHistories.Hers })
        {
            foreach (ulong window in Windows)
            {
                string line = ChainOfCustody.Line(hull, window)!;
                Assert.DoesNotContain('{', line);
                Assert.DoesNotContain('}', line);
            }
        }
    }

    // ── The house laws ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheReservedWord_IsAbsent()
    {
        // docs/worldbuilding-notes.md §8: there is ONE monolith and the word is reserved. A mood line that
        // borrows it is a second claim on it.
        foreach (ulong window in Windows)
        {
            foreach (ShipHistory hull in new[] { FullChain, NeverRenamed, ShipHistories.Hers })
            {
                Assert.DoesNotContain("monolith", ChainOfCustody.Line(hull, window)!, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void NothingHereRollsAnythingOrHoldsADebt()
    {
        // The optional maintenance-debt hook is NOT this slice (the issue files it optional; the 2026-08-02
        // walk-through recommends against it). If a hidden per-hull number ever appears on this type, this
        // guard is where the decision gets re-made rather than drifted into.
        var numbers = typeof(ChainOfCustody)
            .GetFields(Constants)
            .Where(f => f.FieldType != typeof(string) && f.FieldType != typeof(ChainOfCustody.Doubt))
            .ToList();

        Assert.Empty(numbers);

        // …and the whole type is pure: nothing to mutate, nothing to schedule.
        Assert.All(
            typeof(ChainOfCustody).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
            f => Assert.Fail($"ChainOfCustody grew instance state: {f.Name}"));
    }

    // ── Her own hull, and the plate it is keyed to ────────────────────────────────────────────────────

    [Fact]
    public void HerOwnHull_ReadsHerBuildersPlate_NotARoll()
    {
        // #392's plate is canon; the seed fills in the rest around it (the 2026-08-02 recommendation).
        Assert.Equal(ShipHistories.KoskiAndDaughters, ShipHistories.Hers.Yard);
        Assert.Equal(Interior.Plaques.ShipLaidDownYear, ShipHistories.Hers.Year);
        Assert.True(ShipHistories.Hers.HasYardRecord);

        // And she is reachable the way every other hull is — by her id, which is her plate's id.
        Assert.Same(ShipHistories.Hers, ShipHistories.For(Interior.Plaques.Ship.Id));
        Assert.True(ShipHistories.IsHerOwnHull(Interior.Plaques.Ship.Id));
        Assert.False(ShipHistories.IsHerOwnHull("npc-0"));
    }

    [Fact]
    public void ThePlatesYear_IsTheYearOnThePlate()
    {
        // The number and the prose are two mirrors of one fact; this is the guard that keeps them one.
        Assert.Contains(
            Interior.Plaques.ShipLaidDownYear.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Interior.Plaques.Ship.Lore,
            StringComparison.Ordinal);
        Assert.Contains("Koski & Daughters", Interior.Plaques.Ship.Lore, StringComparison.Ordinal);
        Assert.StartsWith("Koski & Daughters", ShipHistories.KoskiAndDaughters, StringComparison.Ordinal);
    }

    [Fact]
    public void HerOwnHull_HasAChainToDoubt_SoTheDreadActuallyLands()
    {
        // The null path is a law, not the shipping case: she must have something to say in every window,
        // or #426 ships as dead code.
        foreach (ulong window in Windows)
        {
            Assert.NotEqual(ChainOfCustody.Doubt.None, ChainOfCustody.Which(ShipHistories.Hers, window));
            Assert.False(string.IsNullOrWhiteSpace(ChainOfCustody.Line(ShipHistories.Hers, window)));
        }
    }

    // ── #1151 · HER GLORY NAME, AND THE THIRD WORRY IT UNLOCKS ───────────────────────────────────────

    [Fact]
    public void HerOwnHull_DealsTheGloryNameHerPlateClaims_OffThePoolsAndNotOutOfAnAuthorsHead()
    {
        // Owner ruling, 2026-09-06: "she gets her glory name." Her plate says in prose that she had one —
        // "her name lit on every departures board from Selene Gate to the Roadstead" — and her seed's rename
        // count had come up zero, so the plate and the record disagreed. The COUNT comes off the plate now.
        Assert.NotNull(ShipHistories.Hers.GloryName);
        Assert.True(ShipHistories.Hers.HasFormerNames);
        Assert.Single(ShipHistories.Hers.FormerNames);

        // …and the NAME is still dealt, not typed: a name and a fate out of the pools every other hull is
        // dealt from. (A guard that only asked for non-null would pass on a typed-in glory name.)
        string entry = ShipHistories.Hers.FormerNames[0];
        Assert.Contains(Pool("FormerNamePool"), name => entry.Contains(name, StringComparison.Ordinal));
        string fate = Pool("Fates").First(f => entry.Contains(f, StringComparison.Ordinal));
        Assert.Equal($"ex-{ShipHistories.Hers.GloryName} ({fate})", entry);

        // Nothing else about her moved. Her plate is as specific about her owners ("sold on, and sold on
        // again") as it is about her name, and a rename may not quietly re-roll the rest of her record.
        Assert.Equal(ShipHistories.KoskiAndDaughters, ShipHistories.Hers.Yard);
        Assert.Equal(Interior.Plaques.ShipLaidDownYear, ShipHistories.Hers.Year);
        Assert.Equal(2, ShipHistories.Hers.OwnersDeep);
        Assert.True(
            ShipHistories.Hers.OwnersDeep >= ShipHistories.Hers.FormerNames.Count,
            "a rename is a re-registration: she cannot be fewer owners deep than the names she has worn.");
    }

    [Fact]
    public void HerOwnHull_NowCarriesTheNameWorryToo_AndItNamesHerOwnGloryName()
    {
        // THE WIDENING the ratchet was filed for. Her candidate set was two lines; it is three now, and the
        // third one says HER name — read off her record, never a pool name and never another hull's.
        var seen = Windows.Select(w => ChainOfCustody.Which(ShipHistories.Hers, w)).Distinct().ToList();
        Assert.Contains(ChainOfCustody.Doubt.TheSurvey, seen);
        Assert.Contains(ChainOfCustody.Doubt.TheName, seen);
        Assert.Contains(ChainOfCustody.Doubt.TheYard, seen);

        ulong window = Windows.First(w => ChainOfCustody.Which(ShipHistories.Hers, w) == ChainOfCustody.Doubt.TheName);
        string line = ChainOfCustody.Line(ShipHistories.Hers, window)!;
        Assert.Contains($"She was {ShipHistories.Hers.GloryName} once", line, StringComparison.Ordinal);
        Assert.DoesNotContain("ex-", line, StringComparison.Ordinal);
    }

    // ── #1151 · THE COVER-UP ON THE BULKHEAD ─────────────────────────────────────────────────────────

    [Fact]
    public void TheCoverUp_IsDealtPerHull_AndBothWaysComeUpAcrossTheHullsTheGameDeals()
    {
        // Owner ruling, 2026-09-06: the plate stays discoverable and the METHOD varies, dealt from her seed.
        // A deal that always answered the same way would be a house style with a switch in front of it, so
        // both arms are asserted to occur among the hulls the generator really deals.
        var dealt = Enumerable.Range(0, 400)
            .Select(i => ShipHistories.HowThePlateIsHidden(ShipHistories.For($"npc-{i}")))
            .ToList();

        Assert.Contains(ShipHistories.PlateConcealment.Plastered, dealt);
        Assert.Contains(ShipHistories.PlateConcealment.Bolted, dealt);
        Assert.Contains(ShipHistories.PlateConcealment.None, dealt); // maiden hulls, with nothing to cover

        // Deterministic (repo agreement §9).
        foreach (string id in new[] { "npc-3", "npc-17", Interior.Plaques.Ship.Id })
        {
            Assert.Equal(
                ShipHistories.HowThePlateIsHidden(ShipHistories.For(id)),
                ShipHistories.HowThePlateIsHidden(ShipHistories.For(id)));
        }
    }

    [Fact]
    public void TheCoverUp_FollowsTheNameItCovers_AndAMaidenHullHasNoneAtAll()
    {
        // The variant is dealt off the former-name entry, so the cover and the thing it covers can never
        // come from two different hulls: change her old name and the cover-up is re-dealt with it.
        Assert.Equal(ShipHistories.PlateConcealment.None, ShipHistories.HowThePlateIsHidden(NeverRenamed));
        Assert.Equal(ShipHistories.PlateConcealment.None, ShipHistories.HowThePlateIsHidden(NoChain));
        Assert.NotEqual(ShipHistories.PlateConcealment.None, ShipHistories.HowThePlateIsHidden(FullChain));

        var byName = Pool("FormerNamePool")
            .Select(name => ShipHistories.HowThePlateIsHidden(
                FullChain with { FormerNames = [$"ex-{name} (impounded, renamed)"] }))
            .Distinct()
            .ToList();
        Assert.Equal(2, byName.Count);
    }

    [Fact]
    public void ThePlateCard_CarriesOneCoverUpLine_ItsOwn_AndNeverTheNameUnderIt()
    {
        // Each arm shows its own line and only its own — the card is the captain's whole find, and two
        // cover-ups on one bulkhead would be the plate telling him it does not know what happened to it.
        ShipHistory plastered = FirstHullDealt(ShipHistories.PlateConcealment.Plastered);
        ShipHistory bolted = FirstHullDealt(ShipHistories.PlateConcealment.Bolted);

        string plasteredCard = Interior.Plaques.BuildersPlateLore(plastered);
        string boltedCard = Interior.Plaques.BuildersPlateLore(bolted);

        Assert.Contains(Interior.Plaques.PlateSkimmedWithFiller, plasteredCard, StringComparison.Ordinal);
        Assert.DoesNotContain(Interior.Plaques.PlateBoltedOverAnother, plasteredCard, StringComparison.Ordinal);
        Assert.Contains(Interior.Plaques.PlateBoltedOverAnother, boltedCard, StringComparison.Ordinal);
        Assert.DoesNotContain(Interior.Plaques.PlateSkimmedWithFiller, boltedCard, StringComparison.Ordinal);

        // The bronze itself is untouched underneath, and a hull with nothing to hide gets nothing appended.
        Assert.StartsWith(Interior.Plaques.Ship.Lore, plasteredCard, StringComparison.Ordinal);
        Assert.StartsWith(Interior.Plaques.Ship.Lore, boltedCard, StringComparison.Ordinal);
        Assert.Equal(Interior.Plaques.Ship.Lore, Interior.Plaques.BuildersPlateLore(NeverRenamed));

        // AND THE PLATE NEVER READS THE NAME OUT. Both lines say she had a name before this one; WHAT it was
        // is the claims counter's second press, and a plate that answered it would spend that scene.
        foreach (string line in new[] { Interior.Plaques.PlateSkimmedWithFiller, Interior.Plaques.PlateBoltedOverAnother })
        {
            foreach (string name in Pool("FormerNamePool"))
            {
                Assert.DoesNotContain(name, line, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain(ShipHistories.Hers.GloryName!, line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase); // §8's reserved word
        }
    }

    [Fact]
    public void HerOwnPlate_TellsTheCaptainSheWasRenamed_WithoutTellingHimTheName()
    {
        // The shipping case, on her own bulkhead: whichever arm her seed dealt her, the card carries it.
        string card = Interior.Plaques.BuildersPlateLore(ShipHistories.Hers);
        Assert.NotEqual(Interior.Plaques.Ship.Lore, card);
        Assert.DoesNotContain(ShipHistories.Hers.GloryName!, card, StringComparison.Ordinal);
    }

    /// <summary>The first hull the shipping generator deals with this cover-up — asked of the generator so
    /// the cards above are about hulls the game really has, and asserted to exist.</summary>
    private static ShipHistory FirstHullDealt(ShipHistories.PlateConcealment how)
    {
        for (int i = 0; i < 400; i++)
        {
            ShipHistory hull = ShipHistories.For($"npc-{i}");
            if (ShipHistories.HowThePlateIsHidden(hull) == how)
            {
                return hull;
            }
        }

        throw new InvalidOperationException($"the generator deals no hull in four hundred a {how} plate.");
    }

    private static string[] Pool(string field) =>
        (string[])(typeof(ShipHistories).GetField(field, Constants)
                   ?? throw new InvalidOperationException($"ShipHistories has no {field} pool."))
        .GetValue(null)!;
}
