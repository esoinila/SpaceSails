using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// The barkeep behind every haven bar (#247). Covers the pure purchase math (debit + affordability +
/// receipt), that each walkable station has a named keep with its own house special (the Tilt cold and
/// blue, the Ringside with a ring in it), the deterministic rumor rotation, and the ContactLedger
/// goodwill seam that "buy a round for the room" leans on (kin #224) — including its vault round-trip.
/// </summary>
public class BarkeepTests
{
    // The station ids HavenInterior builds bars for; the-space-bar is the Rusty Roadstead's berth.
    private static readonly string[] WalkableStations =
        ["the-space-bar", "cinder-roost", "ringside-exchange", "the-tilt"];

    [Fact]
    public void EveryWalkableStation_HasANamedBarkeepWithAHouseSpecial()
    {
        foreach (string id in WalkableStations)
        {
            Barkeep? keep = Barkeeps.For(id);
            Assert.NotNull(keep);
            Assert.False(string.IsNullOrWhiteSpace(keep!.Name), $"{id} barkeep has no name");
            Assert.False(string.IsNullOrWhiteSpace(keep.DrinkName), $"{id} has no house special");
            Assert.False(string.IsNullOrWhiteSpace(keep.DrinkFlavor), $"{id} special has no flavor text");
            Assert.True(keep.DrinkPrice > 0, $"{id} drink must cost a few credits");
            Assert.True(keep.RoundPrice > keep.DrinkPrice, $"{id} a round must cost more than one glass");
            Assert.NotEmpty(keep.Rumors);
        }
    }

    [Fact]
    public void HouseSpecials_AreDistinctPerBar_WithTheOwnerRequestedFlavors()
    {
        var drinks = WalkableStations.Select(id => Barkeeps.For(id)!.DrinkName).ToList();
        Assert.Equal(drinks.Count, drinks.Distinct().Count()); // no two bars pour the same special

        // The Tilt serves something cold and blue.
        string tilt = Barkeeps.For("the-tilt")!.DrinkName + " " + Barkeeps.For("the-tilt")!.DrinkFlavor;
        Assert.Contains("blue", tilt, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cold", tilt, System.StringComparison.OrdinalIgnoreCase);

        // The Ringside serves something with a ring in it.
        string ringside = Barkeeps.For("ringside-exchange")!.DrinkName + " " + Barkeeps.For("ringside-exchange")!.DrinkFlavor;
        Assert.Contains("ring", ringside, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void For_UnknownBerth_HasNoBarkeep()
    {
        Assert.Null(Barkeeps.For("earth"));
        Assert.Null(Barkeeps.For("not-a-station"));
    }

    [Fact]
    public void PourHouseSpecial_DebitsExactPrice_WhenAffordable()
    {
        Barkeep keep = Barkeeps.For("the-space-bar")!;
        BarTab tab = keep.PourHouseSpecial(100);

        Assert.True(tab.Poured);
        Assert.Equal(keep.DrinkPrice, tab.Cost);
        Assert.Equal(100 - keep.DrinkPrice, tab.RemainingCredits);
        Assert.Contains(keep.DrinkName, tab.Line);
    }

    [Fact]
    public void PourHouseSpecial_Refuses_WhenPurseIsShort_AndTakesNothing()
    {
        Barkeep keep = Barkeeps.For("the-space-bar")!;
        BarTab tab = keep.PourHouseSpecial(keep.DrinkPrice - 1);

        Assert.False(tab.Poured);
        Assert.Equal(keep.DrinkPrice - 1, tab.RemainingCredits); // purse untouched
    }

    [Fact]
    public void BuyRound_CostsMoreThanADrink_AndRefusesWhenShort()
    {
        Barkeep keep = Barkeeps.For("cinder-roost")!;

        BarTab paid = keep.BuyRound(keep.RoundPrice);
        Assert.True(paid.Poured);
        Assert.Equal(0, paid.RemainingCredits);

        BarTab broke = keep.BuyRound(keep.RoundPrice - 1);
        Assert.False(broke.Poured);
        Assert.Equal(keep.RoundPrice - 1, broke.RemainingCredits);
    }

    [Fact]
    public void RumorAt_IsDeterministic_AndAlwaysInRange()
    {
        Barkeep keep = Barkeeps.For("ringside-exchange")!;

        // Same sim time -> same rumor (no wall clock, no RNG).
        Assert.Equal(keep.RumorAt(12345.0), keep.RumorAt(12345.0));

        // Every hour of a long day lands on a real, non-empty rumor line.
        for (double t = 0; t < 86400 * 3; t += 3600)
        {
            Assert.Contains(keep.RumorAt(t), keep.Rumors);
        }
    }

    [Fact]
    public void AddGoodwill_WarmsAContact_CreatingTheRecordOnFirstRound()
    {
        var ledger = new ContactLedger();

        ContactHistory after = ledger.AddGoodwill("madam-coil", "Madam Coil", 1);
        Assert.Equal(1, after.Goodwill);
        Assert.True(after.HasHistory); // a round stood is history, even with no job done

        ledger.AddGoodwill("madam-coil", "Madam Coil", 2);
        Assert.Equal(3, ledger.For("madam-coil").Goodwill);

        // Goodwill is not coin — it never touches the signed bank balance.
        Assert.Equal(0, ledger.For("madam-coil").CreditBalance);
    }

    [Fact]
    public void Goodwill_SurvivesTheVaultRoundTrip()
    {
        var ledger = new ContactLedger();
        ledger.AddGoodwill("one-eye-silas", "One-Eye Silas", 4);

        ContactsSection section = VaultMapper.ToSection(ledger);
        var restored = new ContactLedger();
        VaultMapper.Apply(section, restored);

        Assert.Equal(4, restored.For("one-eye-silas").Goodwill);
    }
}
