namespace SpaceSails.Core.Tests;

/// <summary>PR-BUSTED · The dice are the engine (owner §5.0). One deterministic d20 rule with named,
/// SHOWABLE modifiers; opposed checks; amount rolls; and a tiny encounter-script shape. Every
/// consequence system rolls on this — so it is pure, seedable, and never touches the clock.</summary>
public class DiceRuleTests
{
    [Fact]
    public void Roll_IsDeterministic_PerSeed()
    {
        Assert.Equal(DiceRule.Roll(12345).Face, DiceRule.Roll(12345).Face);
        Assert.Equal(DiceRule.Roll(12345).Total, DiceRule.Roll(12345).Total);
    }

    [Fact]
    public void Roll_FaceStaysWithinTheDie()
    {
        for (ulong seed = 0; seed < 500; seed++)
        {
            int face = DiceRule.Roll(seed, 20).Face;
            Assert.InRange(face, 1, 20);
        }
    }

    [Fact]
    public void DifferentSeeds_GiveAVariedDistribution()
    {
        var faces = new HashSet<int>();
        for (ulong seed = 0; seed < 200; seed++)
        {
            faces.Add(DiceRule.Roll(seed, 20).Face);
        }

        Assert.True(faces.Count > 10, "a d20 over 200 seeds should visit most faces");
    }

    [Fact]
    public void ModifierMath_Surfaces_InTotalAndDescription()
    {
        var mods = new List<DiceModifier> { new("Boarding-nets jammer", +2), new("Rattled", -1) };
        DiceRoll roll = DiceRule.Roll(42, 20, mods);

        Assert.Equal(1, roll.ModifierTotal);            // +2 −1
        Assert.Equal(roll.Face + 1, roll.Total);
        string math = roll.Describe();
        Assert.Contains("+2 (Boarding-nets jammer)", math);
        Assert.Contains("−1 (Rattled)", math);          // signed, named — the homage's whole point
        Assert.Contains($"= {roll.Total}", math);
    }

    [Fact]
    public void Opposed_TieGoesToTheDefender()
    {
        // Scan for a genuine tie and confirm the challenger does NOT win it.
        bool sawTie = false;
        for (ulong seed = 0; seed < 5000 && !sawTie; seed++)
        {
            OpposedRoll o = DiceRule.Opposed(seed);
            if (o.Margin == 0)
            {
                sawTie = true;
                Assert.False(o.ChallengerWins);
            }
        }

        Assert.True(sawTie, "a tie should occur within the scan");
    }

    [Fact]
    public void Opposed_Modifiers_TiltTheOdds()
    {
        int withHelp = 0, without = 0;
        var help = new List<DiceModifier> { new("big edge", +8) };
        for (ulong seed = 0; seed < 400; seed++)
        {
            if (DiceRule.Opposed(seed, help).ChallengerWins) withHelp++;
            if (DiceRule.Opposed(seed).ChallengerWins) without++;
        }

        Assert.True(withHelp > without, "a +8 helper should win more opposed checks");
    }

    [Fact]
    public void RollAmount_StaysInRange_ThenAddsModifiers()
    {
        var mods = new List<DiceModifier> { new("heat", +50) };
        DiceRoll r = DiceRule.RollAmount(7, 150, 400, mods);
        Assert.InRange(r.Face, 150, 400);
        Assert.Equal(r.Face + 50, r.Total);
    }

    [Fact]
    public void Seed_TagsGiveIndependentStreams()
    {
        // Same state, different purpose tags → uncorrelated seeds (bribe vs resist off one catch).
        Assert.NotEqual(DiceRule.Seed(99, "bribe"), DiceRule.Seed(99, "resist"));
        // ...and stable per (state, tag).
        Assert.Equal(DiceRule.Seed(99, "bribe"), DiceRule.Seed(99, "bribe"));
    }
}
