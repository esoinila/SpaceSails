using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #535 · <b>THE CODE THAT UNHAPPENS AN ENCOUNTER — the laws, in Core.</b>
///
/// <para>Four things have to be true of this object or it is not the object the issue describes: it lies
/// only on a hull that fought and it is rare; both spends CONSUME it; a burn takes one whole band off the
/// meter and never a number somebody typed; and the canon strings are the only prose in the feature.</para>
///
/// <para><b>Every guard here is written so the world can tell pass from fail</b> — this repository's fifth
/// named bug class. The rarity is measured over a corpus large enough that the neighbouring rate is outside
/// the band; the cause gate is checked against the nine causes it must refuse and not merely the one it
/// admits; the band is compared against the meter's own step so a retuned rung moves both sides together.</para>
/// </summary>
public sealed class TheBlackOpsKeyTests
{
    /// <summary>The corpus every roll in this file is measured over — the same deterministic sequence
    /// <see cref="Derelict.SeededWithCause"/> walks, so these are the hulls the game can actually deal.</summary>
    private static IReadOnlyList<Derelict.Wreck> TheFleet(int n = 4000) =>
        [.. Enumerable.Range(0, n).Select(i => Derelict.Seeded($"lost-{i}"))];

    // ── WHERE THEY LIE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT IS DEALT ON A HULL THAT FOUGHT AND ON NOTHING ELSE — never on a merchant, never on the boarded
    /// one.
    ///
    /// <para>Asked of every cause rather than of the eligible one, which is what lets this fail: with the
    /// gate removed the nine refused causes deal at the eligible rate, and this reports them by name.</para>
    /// </summary>
    [Fact]
    public void TheKeyLiesOnlyOnTheSweepsOwnKindOfHull()
    {
        var dealtOn = new Dictionary<Derelict.WreckCause, int>();
        foreach (Derelict.Wreck w in TheFleet())
        {
            if (BlackOpsKey.IsAboard(w.Id, w.Cause))
            {
                dealtOn[w.Cause] = dealtOn.GetValueOrDefault(w.Cause) + 1;
            }
        }

        Derelict.WreckCause[] wrong = [.. dealtOn.Keys.Where(c => c != Derelict.WreckCause.InsuranceJob)];

        Assert.True(wrong.Length == 0,
            "a black-ops key was dealt on " + string.Join(", ", wrong)
            + ". The canon says the sweep's own kind of hull and nothing else — never a merchant, and never "
            + "the one that was boarded and stripped, which IS the merchant read from the other end.");

        Assert.True(dealtOn.GetValueOrDefault(Derelict.WreckCause.InsuranceJob) > 0,
            "no key was dealt anywhere in four thousand hulls. A gate that refuses everything is not a gate, "
            + "and every other guard in this file would be green against it.");
    }

    /// <summary>The refused causes, named one at a time, so the failure says WHICH cause opened. Piracy is
    /// the arm somebody will reach for and it is the one the canon rules out.</summary>
    [Theory]
    [InlineData(Derelict.WreckCause.Piracy)]
    [InlineData(Derelict.WreckCause.DriveFailure)]
    [InlineData(Derelict.WreckCause.LifeSupportFailure)]
    [InlineData(Derelict.WreckCause.Infested)]
    [InlineData(Derelict.WreckCause.Mutiny)]
    [InlineData(Derelict.WreckCause.VentedByOneOfTheirOwn)]
    public void NoMerchantEverCarriesOne(Derelict.WreckCause cause)
    {
        Assert.False(BlackOpsKey.CauseMayCarryOne(cause));
        Assert.All(TheFleet(1200), w => Assert.False(BlackOpsKey.IsAboard(w.Id, cause)));
    }

    /// <summary>
    /// THE RARITY, MEASURED AND PINNED. Two rates, because they answer two questions: how often an eligible
    /// hull is carrying one (the constant's own promise) and how often a captain boarding a wreck at random
    /// meets one at all (what the object actually feels like).
    ///
    /// <para><b>Measured on this corpus, not derived:</b> 19.6 % of the hulls that may carry one, and 1.95 %
    /// of every wreck in the sky — about one in five, and about one in fifty-one. The bands are tight enough
    /// that the neighbouring constants fall well outside them (one in four is 25 %, one in six is 16.7 %), so
    /// a change to <see cref="BlackOpsKey.OneInEligibleHulls"/> that nobody re-measured goes red here rather
    /// than shipping quietly.</para>
    ///
    /// <para>It is also the guard that caught the roll being seeded through <c>string.GetHashCode</c>, which
    /// .NET randomises per process: the same two rates came back different on two consecutive runs.</para>
    /// </summary>
    [Fact]
    public void TheRarityIsTheOneThatWasMeasured()
    {
        IReadOnlyList<Derelict.Wreck> fleet = TheFleet();
        int eligible = fleet.Count(w => BlackOpsKey.CauseMayCarryOne(w.Cause));
        int carrying = fleet.Count(w => BlackOpsKey.IsAboard(w.Id, w.Cause));

        double ofEligible = (double)carrying / eligible;
        double ofEveryHull = (double)carrying / fleet.Count;

        Assert.InRange(ofEligible, 0.19, 0.205);   // 1 in 5 of the hulls that may carry one
        Assert.InRange(ofEveryHull, 0.017, 0.022); // ~1 in 51 of every wreck in the sky

        Assert.Equal(5, BlackOpsKey.OneInEligibleHulls);
    }

    /// <summary>A hull you left is the hull you come back to, and two hulls are not one answer. Seeded off
    /// her id in the archive node's own idiom, so a rumour about a ship is worth something.</summary>
    [Fact]
    public void TheHullIsTheRollAndTheRollDoesNotMove()
    {
        IReadOnlyList<Derelict.Wreck> eligible =
            [.. TheFleet().Where(w => BlackOpsKey.CauseMayCarryOne(w.Cause))];

        foreach (Derelict.Wreck w in eligible.Take(50))
        {
            Assert.Equal(BlackOpsKey.IsAboard(w.Id, w.Cause), BlackOpsKey.IsAboard(w.Id, w.Cause));
        }

        Assert.Contains(eligible, w => BlackOpsKey.IsAboard(w.Id, w.Cause));
        Assert.Contains(eligible, w => !BlackOpsKey.IsAboard(w.Id, w.Cause));
    }

    // ── IN THE POCKET, AND OUT OF IT ────────────────────────────────────────────────────────────────────

    /// <summary>Two keys off two hulls are two keys. The id is the hull for exactly this reason: only rounds
    /// stack, so a shared id would silently make the second find the first one.</summary>
    [Fact]
    public void TwoHullsAreTwoKeys()
    {
        IReadOnlyList<Satchel.Item> pocket = Satchel.Add(
            Satchel.Add([], BlackOpsKey.FoundOn("lost-1")), BlackOpsKey.FoundOn("lost-2"));

        Assert.Equal(2, BlackOpsKey.CountIn(pocket));
    }

    /// <summary>BOTH SPENDS CONSUME, and they consume through one call — a presentation and a burn can never
    /// come to disagree about what "spent" means.</summary>
    [Fact]
    public void ASpendTakesExactlyOneKeyAndLeavesTheOther()
    {
        IReadOnlyList<Satchel.Item> pocket = Satchel.Add(
            Satchel.Add([], BlackOpsKey.FoundOn("lost-1")), BlackOpsKey.FoundOn("lost-2"));

        Satchel.Item spent = BlackOpsKey.InThePocket(pocket)!.Value;
        IReadOnlyList<Satchel.Item> after = BlackOpsKey.Spend(pocket, spent);

        Assert.Equal(1, BlackOpsKey.CountIn(after));
        Assert.DoesNotContain(after, i => string.Equals(i.Id, spent.Id, StringComparison.Ordinal));
        Assert.Null(BlackOpsKey.InThePocket(BlackOpsKey.Spend(after, after[0])));
    }

    /// <summary>#688's law, for the fourth time: the best find in the game is flat, so a full pocket can
    /// never refuse it.</summary>
    [Fact]
    public void AFullPocketStillTakesTheKey()
    {
        IReadOnlyList<Satchel.Item> stuffed = [];
        for (int i = 0; i < Satchel.PocketCapacity + 4; i++)
        {
            stuffed = Satchel.Add(stuffed, new Satchel.Item(Satchel.Kind.Relic, $"relic-{i}"));
        }

        Assert.Equal(Satchel.Compartment.Wallet, Satchel.CompartmentOf(Satchel.Kind.BlackOpsKey));
        Assert.True(Satchel.CanTake(stuffed, BlackOpsKey.FoundOn("lost-7")));
        Assert.Equal(1, BlackOpsKey.CountIn(Satchel.Add(stuffed, BlackOpsKey.FoundOn("lost-7"))));
    }

    /// <summary>It is never offered to a door. A shaft files nothing about you, so there is nothing there for
    /// a key to unfile — and #688's ruling is that doors suggest keys, which this one is not: it is a code
    /// spent on a PERSON who is about to write your name down.</summary>
    [Fact]
    public void ItIsNeverOfferedToADoor()
    {
        foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
        {
            if (SatchelTry.IsDoor(target))
            {
                Assert.False(SatchelTry.CanOffer(Satchel.Kind.BlackOpsKey, target));
            }
        }
    }

    /// <summary>THE VAULT ROUND-TRIP. A saved satchel stores the ordinal, so the kind was appended and never
    /// inserted — and a key written down comes back a key, off the same hull.</summary>
    [Fact]
    public void AKeyRidesTheVaultAndComesBackTheSameKey()
    {
        Satchel.Item key = BlackOpsKey.FoundOn("lost-42");
        Assert.True(Satchel.Item.TryParse(key.Stored, out Satchel.Item back));

        Assert.Equal(key, back);
        Assert.True(BlackOpsKey.IsTheKey(back));

        // Appended, never inserted: every kind that existed before this one keeps its ordinal.
        Assert.Equal((int)Satchel.Kind.Tool + 1, (int)Satchel.Kind.BlackOpsKey);
        Assert.Equal(0, (int)Satchel.Kind.Authority);
    }

    // ── BURNING ONE COLD ────────────────────────────────────────────────────────────────────────────────

    private const string AnOutfit = "meridian-works";

    private static ContactLedger BookWith(int heat, double at = 1000.0)
    {
        var book = new ContactLedger();
        IllegalHeat.Bank(book, new UndergroundComplex.HeatCharge(AnOutfit, heat), at);
        return book;
    }

    /// <summary>
    /// A BURN TAKES ONE WHOLE BAND, AND THE BAND IS THE METER'S OWN STEP.
    ///
    /// <para>Not a typed number: the drop is compared against <see cref="IllegalHeat.HeatPerRung"/>, the
    /// width the round's patience ladder is already divided by — so "drops one band" and "starts one rung
    /// lower at their gate" are the same sentence, and both sides move together the day the rung is
    /// retuned.</para>
    /// </summary>
    [Fact]
    public void ABurnDropsExactlyOneBand()
    {
        ContactLedger book = BookWith(IllegalHeat.Ceiling);
        int before = IllegalHeat.HeatAt(book, AnOutfit);
        int rungBefore = IllegalHeat.StartingRung(before);

        int erased = IllegalHeat.Scrub(book, AnOutfit, BlackOpsKey.ScrubReason);
        int after = IllegalHeat.HeatAt(book, AnOutfit);

        Assert.Equal(IllegalHeat.ABand, erased);
        Assert.Equal(IllegalHeat.HeatPerRung, before - after);
        Assert.Equal(rungBefore - 1, IllegalHeat.StartingRung(after));
    }

    /// <summary>A key burned over a thin file removes what is there and stops — owner: <i>"a key burned at
    /// heat 1 removes almost nothing and nobody notices."</i> There is no such thing as negative heat, and a
    /// clean book is untouched.</summary>
    [Fact]
    public void ABurnNeverGoesPastZero()
    {
        ContactLedger thin = BookWith(1);
        Assert.Equal(1, IllegalHeat.Scrub(thin, AnOutfit, BlackOpsKey.ScrubReason));
        Assert.Equal(0, IllegalHeat.HeatAt(thin, AnOutfit));

        var clean = new ContactLedger();
        Assert.Equal(0, IllegalHeat.Scrub(clean, "somebody-never-crossed", BlackOpsKey.ScrubReason));
        Assert.Equal(0, IllegalHeat.HeatAt(clean, "somebody-never-crossed"));
    }

    /// <summary>
    /// A SCRUB IS AN EDIT, NOT AN HOUR — the #938 audit row's own words, as arithmetic.
    ///
    /// <para>The cooling stamp does not move, so the hours a captain has or has not spent away from these
    /// people are exactly what they were. A scrub that touched the clock would be handing back time as well
    /// as pages, and the next <see cref="IllegalHeat.Cool"/> would quietly charge for it.</para>
    /// </summary>
    [Fact]
    public void AScrubDoesNotTouchTheCoolingClock()
    {
        const double banked = 1000.0;
        ContactLedger book = BookWith(IllegalHeat.Ceiling, banked);
        double stampBefore = book.For(IllegalHeat.LedgerId(AnOutfit)).HeatStampSimTime;

        IllegalHeat.Scrub(book, AnOutfit, BlackOpsKey.ScrubReason);
        Assert.Equal(stampBefore, book.For(IllegalHeat.LedgerId(AnOutfit)).HeatStampSimTime);

        // …and the hour that was already owed still cools, off the same stamp it was always going to.
        IllegalHeat.Cool(book, operatorIdUnderfoot: null, banked + IllegalHeat.CoolsOnePointEverySeconds);
        Assert.Equal(IllegalHeat.Ceiling - IllegalHeat.ABand - 1, IllegalHeat.HeatAt(book, AnOutfit));
    }

    /// <summary>It is owed to ONE outfit, like everything else in that book. Burning a key over one company's
    /// file says nothing to anybody else's — #715's whole law, and a scrub does not get to renegotiate it.</summary>
    [Fact]
    public void ABurnReachesOneOutfitAndNoOther()
    {
        var book = new ContactLedger();
        IllegalHeat.Bank(book, new UndergroundComplex.HeatCharge(AnOutfit, 8), 1000.0);
        IllegalHeat.Bank(book, new UndergroundComplex.HeatCharge("somebody-else", 8), 1000.0);

        IllegalHeat.Scrub(book, AnOutfit, BlackOpsKey.ScrubReason);

        Assert.Equal(8 - IllegalHeat.ABand, IllegalHeat.HeatAt(book, AnOutfit));
        Assert.Equal(8, IllegalHeat.HeatAt(book, "somebody-else"));
    }

    /// <summary>A caller that cannot say why it is deleting somebody's file does not get to delete it. The
    /// reason is what makes the row an EDIT in the ledger rather than an hour that passed.</summary>
    [Fact]
    public void AScrubWithNoReasonIsRefused()
    {
        ContactLedger book = BookWith(8);
        Assert.Throws<ArgumentException>(() => IllegalHeat.Scrub(book, AnOutfit, "  "));
        Assert.Equal(8, IllegalHeat.HeatAt(book, AnOutfit));
    }

    // ── THE PROSE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every public string this feature publishes, gathered by reflection so a fifth one added
    /// tomorrow cannot hide from the two guards below.</summary>
    private static IReadOnlyList<(string Member, string Text)> EveryStringOnTheKey()
    {
        var found = new List<(string, string)>();
        foreach (FieldInfo f in typeof(BlackOpsKey).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.GetValue(null) is string s)
            {
                found.Add((f.Name, s));
            }
        }
        foreach (PropertyInfo p in typeof(BlackOpsKey)
                     .GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (p.PropertyType == typeof(string) && p.GetValue(null) is string s)
            {
                found.Add((p.Name, s));
            }
        }
        return found;
    }

    /// <summary>
    /// THE CANON STRINGS ARE THE ONLY PROSE IN THE FEATURE.
    ///
    /// <para>Canon pass, 2026-09-05: the name, the look-card line, the two verbs, the outcome plate and the
    /// burn line. Anything else this type publishes with a space in it is prose somebody authored without
    /// asking, which is the one thing slice 1 was told not to do.</para>
    /// </summary>
    [Fact]
    public void NothingIsAuthoredBesideTheCanon()
    {
        string[] canon =
        [
            BlackOpsKey.Name,
            BlackOpsKey.LookCardLine,
            BlackOpsKey.PresentVerb,
            BlackOpsKey.BurnVerb,
            BlackOpsKey.NoContactLoggedPlate,
            BlackOpsKey.BurnLine,
            BlackOpsKey.CardLabel,      // the canon name, shouted — the plate typography, not a second name
            BlackOpsKey.ScrubReason,    // a ledger reason, never rendered
        ];

        string[] stray =
        [
            .. EveryStringOnTheKey()
                .Where(s => s.Text.Contains(' ', StringComparison.Ordinal))
                .Where(s => !canon.Contains(s.Text, StringComparer.Ordinal))
                .Select(s => $"{s.Member} = \"{s.Text}\""),
        ];

        Assert.True(stray.Length == 0,
            "prose this feature was not given:\n  - " + string.Join("\n  - ", stray)
            + "\n\nSlice 1 authors nothing. If a beat wants a line, leave a `// FABLE: line needed — …` "
            + "marker and file it.");

        // …and the canon really is what the issue says it is, word for word.
        Assert.Equal("Black-ops key", BlackOpsKey.Name);
        Assert.Equal(
            "A code somebody paid a great deal to make sure nobody would ever read. It works once.",
            BlackOpsKey.LookCardLine);
        Assert.Equal("Present the key", BlackOpsKey.PresentVerb);
        Assert.Equal("Burn the key", BlackOpsKey.BurnVerb);
        Assert.Equal("NO CONTACT LOGGED", BlackOpsKey.NoContactLoggedPlate);
        Assert.Equal("Somewhere a file closes. The key is ash.", BlackOpsKey.BurnLine);
        Assert.Equal("🗝", BlackOpsKey.Glyph);
    }

    /// <summary>§8's reserved word and the fifteen beside it — and, because this object's own canon law is
    /// that it never explains who made it, every word an issuer would be named with.</summary>
    [Fact]
    public void TheKeySaysNothingAboutWhoMadeItOrWhatThisPlaceWasFor()
    {
        string[] forbidden =
        [
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
            // …and the issuer. "They belong to somebody" is the constraint; naming the somebody is the
            // thing that would make the object boring and the fiction cheap.
            "issued", "issuer", "navy", "military", "service", "agency", "bureau", "nebula", "mutual",
        ];

        foreach ((string member, string text) in EveryStringOnTheKey())
        {
            foreach (string bad in forbidden)
            {
                Assert.False(text.Contains(bad, StringComparison.OrdinalIgnoreCase),
                    $"BlackOpsKey.{member} says \"{bad}\". The key never explains who made it, and nothing "
                    + "in this feature settles what the ground was for.");
            }
        }
    }

    /// <summary>The look card is the object's own card and it claims no painting — #528's caption-only
    /// idiom, taken deliberately rather than by wiring a file that is not there and hiding it on error.</summary>
    [Fact]
    public void TheCardIsCaptionOnlyAndIsTheCanonLine()
    {
        CarriedObject.Reveal? card = CarriedObject.Card(BlackOpsKey.FoundOn("lost-3"), "luna");

        Assert.NotNull(card);
        Assert.Equal(string.Empty, card!.Value.ArtUrl);
        Assert.Equal(BlackOpsKey.LookCardLine, card.Value.Story);
        Assert.Contains(BlackOpsKey.Glyph, card.Value.Label, StringComparison.Ordinal);
    }

    // ── WHERE IT IS LYING ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The key's station is somewhere a captain can stand and is not on top of anything else — the
    /// same two questions the archive node's own station answers, asked on every cause because eligibility
    /// rules widen and geometry checked only where it is used breaks the day they do.</summary>
    [Fact]
    public void TheKeyStandsClearOfEveryOtherStationOnEveryCause()
    {
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            foreach ((string name, DeckReachability.Point at) in WreckLayout.Stations(cause))
            {
                double dx = at.X - WreckLayout.KeyStation.X;
                double dy = at.Y - WreckLayout.KeyStation.Y;
                Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) >= 3.0,
                    $"on {cause} the key is standing on {name}'s doorstep — two consoles sharing one square "
                    + "metre, which is how the scuttling panel came to hand out a nest.");
            }

            Assert.Contains(WreckLayout.StationsWithKey(cause), s => s.Name == "the black-ops key");
        }
    }
}
