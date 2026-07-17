namespace SpaceSails.Core.Tests;

/// <summary>PR-BUSTED · Hot cargo (owner ruling §5.1). Cargo stolen under heat is stamped hot at
/// theft time; when heat cools to 0 the flags launder off. The confiscation reads this book.</summary>
public class HotCargoLedgerTests
{
    [Fact]
    public void Stamp_OnlyFlagsHot_WhenTheTheftWasUnderHeat()
    {
        var hot = new HotCargoLedger();
        hot.Stamp("He3", 6, heatAtTheft: 2);
        Assert.Equal(6, hot.HotUnits("He3"));

        // A clean-space theft (heat 0) stamps nothing — no crime on the books.
        hot.Stamp("Ice", 4, heatAtTheft: 0);
        Assert.Equal(0, hot.HotUnits("Ice"));
    }

    [Fact]
    public void Stamp_Accumulates_AcrossBoardings()
    {
        var hot = new HotCargoLedger();
        hot.Stamp("He3", 3, 1);
        hot.Stamp("He3", 2, 1);
        Assert.Equal(5, hot.HotUnits("He3"));
        Assert.Equal(5, hot.TotalHotUnits);
        Assert.True(hot.Any);
    }

    [Fact]
    public void Launder_ClearsEveryFlag_WhenHeatCools()
    {
        var hot = new HotCargoLedger();
        hot.Stamp("He3", 6, 3);
        hot.Stamp("Alloys", 2, 3);
        Assert.True(hot.Any);

        Assert.True(hot.Launder());
        Assert.False(hot.Any);
        Assert.Equal(0, hot.HotUnits("He3"));
        Assert.False(hot.Launder());    // idempotent — nothing left to clear
    }

    [Fact]
    public void BuildLots_ClampsHotToUnitsAboard_AndDropsEmptyClasses()
    {
        var hot = new HotCargoLedger();
        hot.Stamp("He3", 10, 2);        // flagged 10 hot...
        var cargo = new Dictionary<string, int> { ["He3"] = 6, ["Ice"] = 4, ["Alloys"] = 0 };

        IReadOnlyList<BustedRule.CargoLot> lots = hot.BuildLots(cargo);

        BustedRule.CargoLot he3 = lots.Single(l => l.CargoClass == "He3");
        Assert.Equal(6, he3.Units);
        Assert.Equal(6, he3.HotUnits);  // ...but only 6 are actually aboard — clamped
        Assert.Contains(lots, l => l.CargoClass == "Ice" && l.HotUnits == 0);
        Assert.DoesNotContain(lots, l => l.CargoClass == "Alloys"); // zero-unit class dropped
    }

    [Fact]
    public void ConfiscationReadsTheLedger_TakingExactlyTheHotUnits()
    {
        var hot = new HotCargoLedger();
        hot.Stamp("He3", 4, 2);
        var cargo = new Dictionary<string, int> { ["He3"] = 6 }; // 4 hot, 2 clean

        BustedRule.Confiscation c = BustedRule.Confiscate(2, 2000, hot.BuildLots(cargo), seed: 1);

        Assert.Contains(c.Seizures, s => s is { CargoClass: "He3", Units: 4, Hot: true });
        // the 2 clean He3 stay (the purse covered the floor)
        Assert.DoesNotContain(c.Seizures, s => !s.Hot);
    }
}
