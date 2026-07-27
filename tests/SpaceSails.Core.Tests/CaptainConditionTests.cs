namespace SpaceSails.Core.Tests;

/// <summary>
/// #453 · The exchange. Owner, live 2026-07-27: <i>"player health could be like 5 reever hits but the reever
/// sphere must touch the player sphere when a hit is received. Player should have some melee blocking
/// ability. Dice throw. We should narrate what happens to the player."</i>
/// </summary>
public class CaptainConditionTests
{
    [Fact]
    public void FiveBlowsAndYouAreDown_NotFour_NotSix()
    {
        Assert.Equal(5, CaptainCondition.MaxHits);
        Assert.False(CaptainCondition.IsDown(4));
        Assert.True(CaptainCondition.IsDown(5));
        Assert.True(CaptainCondition.IsDown(9)); // never negative-health nonsense
    }

    [Fact]
    public void ContactIsTheTwoBodiesTOUCHING_WiderThanMerelyBeingReached()
    {
        // The owner's rule, and it is deliberately NOT the catch radius: reaching you is one thing, putting
        // a hand on you is another. Two 0.7 bodies touch at 1.4.
        Assert.Equal(1.4, CaptainCondition.TouchDistance, 6);
        // #469: ONE contact law. The catch radius used to be 1.2 — tighter than the sum of the two body
        // radii — so being "caught" required the bodies to visibly overlap while a swing landed at a true
        // touch. Everything that keys off being reached now reads the same number.
        Assert.Equal(ReeverChase.CatchRadius, CaptainCondition.TouchDistance, 6);
    }

    [Fact]
    public void SteadyHandsBlockFarBetterThanShotNerves()
    {
        // Nerve is the spine of the block: the same die, the same swing, two different captains. This is
        // what finally gives the #317 gauge a mechanical consequence instead of only a voice.
        int steadyBlocks = 0, ruinedBlocks = 0;
        for (ulong i = 0; i < 400; i++)
        {
            ulong seed = DiceRule.Seed(i, "swing");
            if (CaptainCondition.Resolve(CaptainCondition.BlockRoll(seed, NerveModel.Max, false, 1))
                == CaptainCondition.Exchange.Blocked)
            {
                steadyBlocks++;
            }
            if (CaptainCondition.Resolve(CaptainCondition.BlockRoll(seed, NerveModel.Min, false, 1))
                == CaptainCondition.Exchange.Blocked)
            {
                ruinedBlocks++;
            }
        }
        Assert.True(steadyBlocks > ruinedBlocks + 80,
            $"steady {steadyBlocks} vs shot {ruinedBlocks} — nerve must visibly decide the block");
    }

    [Fact]
    public void HandsFullOfChest_BlocksWorse_SoDroppingItIsARealChoice()
    {
        // The G key (drop the chest and sprint) should be a genuine decision under a swarm, not just a
        // speed toggle — a captain hugging a strongbox is a captain not defending themselves.
        int free = 0, laden = 0;
        for (ulong i = 0; i < 400; i++)
        {
            ulong seed = DiceRule.Seed(i, "swing");
            if (CaptainCondition.Resolve(CaptainCondition.BlockRoll(seed, 60, false, 1))
                == CaptainCondition.Exchange.Blocked) { free++; }
            if (CaptainCondition.Resolve(CaptainCondition.BlockRoll(seed, 60, true, 1))
                == CaptainCondition.Exchange.Blocked) { laden++; }
        }
        Assert.True(free > laden, $"free hands {free} must beat full hands {laden}");
    }

    [Fact]
    public void BeingSwarmedStacksAgainstYou()
    {
        int alone = 0, mobbed = 0;
        for (ulong i = 0; i < 400; i++)
        {
            ulong seed = DiceRule.Seed(i, "swing");
            if (CaptainCondition.Resolve(CaptainCondition.BlockRoll(seed, 60, false, 1))
                == CaptainCondition.Exchange.Blocked) { alone++; }
            if (CaptainCondition.Resolve(CaptainCondition.BlockRoll(seed, 60, false, 3))
                == CaptainCondition.Exchange.Blocked) { mobbed++; }
        }
        Assert.True(alone > mobbed, $"one-on-one {alone} must beat being mobbed {mobbed}");
    }

    [Fact]
    public void TheRollShowsItsMath_BecauseTheHomageIsWatchingTheNumbersAddUp()
    {
        // #305's whole point: the player must be able to SEE why they were opened up. Every penalty is a
        // named line, never an invisible fudge.
        DiceRoll roll = CaptainCondition.BlockRoll(DiceRule.Seed(7, "swing"), NerveModel.Max, true, 3);
        Assert.Contains(roll.Modifiers, m => m.Label.Contains("steady", System.StringComparison.OrdinalIgnoreCase));
        Assert.Contains(roll.Modifiers, m => m.Label.Contains("hands full", System.StringComparison.OrdinalIgnoreCase));
        Assert.Contains(roll.Modifiers, m => m.Label.Contains("swarmed", System.StringComparison.OrdinalIgnoreCase));
        Assert.All(roll.Modifiers, m => Assert.False(string.IsNullOrWhiteSpace(m.Label)));
    }

    [Fact]
    public void Deterministic_TheSameSwingAlwaysLandsTheSameWay()
    {
        ulong seed = DiceRule.Seed(99, "swing:3");
        DiceRoll a = CaptainCondition.BlockRoll(seed, 55, false, 2);
        DiceRoll b = CaptainCondition.BlockRoll(seed, 55, false, 2);
        Assert.Equal(a.Total, b.Total);
        Assert.Equal(CaptainCondition.Resolve(a), CaptainCondition.Resolve(b));
        Assert.Equal(CaptainCondition.HitLine(seed), CaptainCondition.HitLine(seed));
    }

    [Fact]
    public void EveryNarrationLine_IsRealProse_AndTheTwoPoolsNeverBleed()
    {
        // A block must read as costly and a hit must read as bloody — if the pools ever merged, being hurt
        // would stop meaning anything.
        for (ulong i = 0; i < 60; i++)
        {
            ulong seed = DiceRule.Seed(i, "swing");
            string block = CaptainCondition.BlockLine(seed);
            string hit = CaptainCondition.HitLine(seed);
            Assert.True(block.Length > 30, $"a thin block line: \"{block}\"");
            Assert.True(hit.Length > 30, $"a thin hit line: \"{hit}\"");
            Assert.NotEqual(block, hit);
        }
    }

    [Fact]
    public void TheMarkerNamesEveryStateDownToTheLastBlow()
    {
        Assert.Equal("unmarked", CaptainCondition.Readout(0));
        Assert.Equal("one more will do it", CaptainCondition.Readout(4));
        Assert.Equal("down", CaptainCondition.Readout(5));
        for (int i = 0; i <= CaptainCondition.MaxHits; i++)
        {
            Assert.False(string.IsNullOrWhiteSpace(CaptainCondition.Readout(i)));
        }
    }
}
