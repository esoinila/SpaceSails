namespace SpaceSails.Core.Tests;

/// <summary>PR-BUSTED · The catch (owner ruling §5). Confiscation math (heat-scaled coin share, all
/// hot cargo, the minimum-take fallback, the mercy floors), the dice bribe, the resist ladder, and
/// the brain-backup resurrection kit — never game over.</summary>
public class BustedRuleTests
{
    private static List<BustedRule.CargoLot> Hold(params (string cls, int units, int hot)[] lots)
    {
        var list = new List<BustedRule.CargoLot>();
        foreach ((string cls, int units, int hot) in lots)
        {
            list.Add(new BustedRule.CargoLot(cls, units, hot));
        }

        return list;
    }

    [Fact]
    public void CoinFraction_ClimbsWithHeat_PerTheOwnerLadder()
    {
        Assert.Equal(0.20, BustedRule.CoinFraction(1));
        Assert.Equal(0.35, BustedRule.CoinFraction(2));
        Assert.Equal(0.50, BustedRule.CoinFraction(3));
    }

    [Fact]
    public void Confiscate_TakesTheHeatShareOfCoin_AndAllHotCargo()
    {
        // Heat 2 → 35% of 2000 cr = 700, and every hot unit in full.
        BustedRule.Confiscation c = BustedRule.Confiscate(
            heat: 2, coin: 2000, Hold(("He3", 6, 6), ("Ice", 4, 0)), seed: 1);

        Assert.Equal(700, c.CoinTaken);
        Assert.Equal(1300, c.CoinLeft);
        // He3 hot lot taken in full; the clean Ice is untouched (the purse covered the floor).
        Assert.Contains(c.Seizures, s => s is { CargoClass: "He3", Units: 6, Hot: true });
        Assert.DoesNotContain(c.Seizures, s => s.CargoClass == "Ice");
        Assert.Equal(6 * CargoMarket.UnitValue("He3"), c.CargoValueTaken);
        Assert.False(c.UsedMinimumTake);
    }

    [Fact]
    public void Confiscate_MercyFloor_NeverTakesTheLastBerthFee()
    {
        // Only 120 cr aboard: the mercy floor leaves 100, so at most 20 can go however high the share.
        BustedRule.Confiscation c = BustedRule.Confiscate(heat: 3, coin: 120, Hold(("He3", 2, 2)), seed: 3);
        Assert.True(c.CoinLeft >= BustedRule.MinBerthFeeCr);
        Assert.Equal(20, c.CoinTaken);
    }

    [Fact]
    public void Confiscate_MinimumTake_GrabsCleanCargo_WhenThePurseIsSquirreledAway()
    {
        // Coin all banked/buried (0 aboard) and no hot cargo — the law still takes a dice-rolled
        // minimum from clean cargo (richest class first), so it never leaves empty-handed.
        BustedRule.Confiscation c = BustedRule.Confiscate(
            heat: 2, coin: 0, Hold(("He3", 8, 0), ("Ice", 8, 0)), seed: 5);

        Assert.True(c.UsedMinimumTake);
        Assert.Equal(0, c.CoinTaken);
        Assert.NotEmpty(c.Seizures);
        Assert.All(c.Seizures, s => Assert.False(s.Hot));   // clean cargo
        Assert.Equal("He3", c.Seizures[0].CargoClass);      // richest first — the law takes value
    }

    [Fact]
    public void Confiscate_Harsher_TakesMore_ThanAPlainSubmission()
    {
        var hold = Hold(("He3", 10, 4), ("Alloys", 10, 0));
        BustedRule.Confiscation plain = BustedRule.Confiscate(2, 2000, hold, seed: 9, harsher: false);
        BustedRule.Confiscation harsh = BustedRule.Confiscate(2, 2000, hold, seed: 9, harsher: true);

        Assert.True(harsh.CoinTaken > plain.CoinTaken, "a steeper coin fraction");
        Assert.True(harsh.CargoValueTaken > plain.CargoValueTaken, "plus an extra clean-cargo cut");
    }

    [Fact]
    public void Confiscate_IsDeterministic_PerSeed()
    {
        var hold = Hold(("He3", 3, 0), ("Ice", 5, 0));
        BustedRule.Confiscation a = BustedRule.Confiscate(2, 40, hold, seed: 77);
        BustedRule.Confiscation b = BustedRule.Confiscate(2, 40, hold, seed: 77);
        Assert.Equal(a.CoinTaken, b.CoinTaken);
        Assert.Equal(a.CargoValueTaken, b.CargoValueTaken);
    }

    [Fact]
    public void BribeDemand_ScalesWithHeat_AndShowsItsMath()
    {
        DiceRoll h1 = BustedRule.BribeDemand(1, seed: 4);
        DiceRoll h3 = BustedRule.BribeDemand(3, seed: 4);
        Assert.InRange(h1.Total, 150, 400);
        Assert.InRange(h3.Total, 800, 1500);
        Assert.True(h3.Total > h1.Total, "heat 3 patrols cost more to buy off");
    }

    [Fact]
    public void BribeDemand_ModifiersLowerTheAsk_AndAreNamed()
    {
        var haggle = new List<DiceModifier> { new("silver tongue", -120) };
        DiceRoll with = BustedRule.BribeDemand(2, 4, haggle);
        DiceRoll without = BustedRule.BribeDemand(2, 4);
        Assert.Equal(without.Total - 120, with.Total);
        Assert.Contains("silver tongue", with.Describe());
    }

    [Fact]
    public void ResistCheck_HeatStiffensTheCollector_HelpersAidThePlayer()
    {
        int lowHeatWins = 0, highHeatWins = 0, helpedWins = 0;
        var jammer = new List<DiceModifier> { new("Boarding-nets jammer", +2) };
        for (ulong seed = 0; seed < 400; seed++)
        {
            if (BustedRule.ResistCheck(1, seed).ChallengerWins) lowHeatWins++;
            if (BustedRule.ResistCheck(3, seed).ChallengerWins) highHeatWins++;
            if (BustedRule.ResistCheck(1, seed, jammer).ChallengerWins) helpedWins++;
        }

        Assert.True(lowHeatWins > highHeatWins, "resisting is harder at higher heat");
        Assert.True(helpedWins > lowHeatWins, "a purchased helper improves the odds");
    }

    [Fact]
    public void Resurrect_IsAStarterGradeRustbucket_AtTheMercyFloor()
    {
        BustedRule.ResurrectionKit kit = BustedRule.Resurrect(mercyFloorPulses: 60);

        Assert.Equal(BustedRule.InsuranceCredits, kit.Credits);
        Assert.Equal(60, kit.ReactionMassPulses);       // tank at the reach-a-pump floor, not stranded
        Assert.Equal(BustedRule.StarterSlugAmmo, kit.SlugAmmo);
        Assert.Equal(0, kit.MassLevel);                  // every upgrade reset to base
        Assert.Equal(0, kit.SensorLevel);
        Assert.Equal(0, kit.HoldLevel);
        Assert.Equal(0, kit.TelescopeLevel);
    }

    [Fact]
    public void ExposurePhrase_NamesTheShare_ForTheParrot()
    {
        Assert.Contains("fifth", BustedRule.ExposurePhrase(1));
        Assert.Contains("THIRD", BustedRule.ExposurePhrase(2));
        Assert.Contains("HALF", BustedRule.ExposurePhrase(3));
    }
}
