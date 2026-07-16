namespace SpaceSails.Core.Tests;

/// <summary>#202 — the crime's receipt. A completed boarding books a loot line: what, units, worth,
/// off whom, where, when — the piracy twin of the honest payment receipts, priced through the same
/// fence market so the receipt and the credited cargo value agree.</summary>
public class LootRecordTests
{
    [Fact]
    public void ForHaul_PricesTheHaul_ThroughTheFenceMarket()
    {
        LootRecord loot = LootRecord.ForHaul("He3", units: 6, victimCallsign: "Larkspur", where: "Mars", simTime: 4200);

        Assert.Equal("He3", loot.CargoClass);
        Assert.Equal(6, loot.Units);
        Assert.Equal(6 * CargoMarket.UnitValue("He3"), loot.EstimatedWorth); // 6 × 1200 = 7200
        Assert.Equal("Larkspur", loot.VictimCallsign);
        Assert.Equal("Mars", loot.Where);
        Assert.Equal(4200, loot.SimTime);
    }

    [Fact]
    public void Describe_NamesWhatUnitsVictimWhereAndWorth()
    {
        string line = LootRecord.ForHaul("He3", 6, "Larkspur", "Mars", 0).Describe();

        Assert.Contains("6 units of He3", line);
        Assert.Contains("Larkspur", line);   // off whom
        Assert.Contains("Mars", line);       // where
        Assert.Contains("7,200 cr", line);   // est. worth
    }

    [Fact]
    public void Worth_TracksTheClass_MilkRunVsPrize()
    {
        // He3 is the prize; ice is a milk run — the receipt says so.
        LootRecord he3 = LootRecord.ForHaul("He3", 10, "A", "here", 0);
        LootRecord ice = LootRecord.ForHaul("Ice", 10, "B", "here", 0);
        Assert.True(he3.EstimatedWorth > ice.EstimatedWorth);
    }
}
