namespace SpaceSails.Core.Tests;

/// <summary>
/// The captain's drinking excuses (owner ruling 2026-07-19: "Captain needs excuse to drink :-D"). Pure
/// flavour, so the tests guard exactly what flavour must promise: a healthy, unique, never-blank pool and
/// a deterministic pick that a caller's seed always reproduces.
/// </summary>
public class DrinkExcusesTests
{
    [Fact]
    public void Pool_IsAHealthySize()
    {
        // The owner asked for ~12-16 lines so the shelf never feels thin.
        Assert.InRange(DrinkExcuses.Lines.Count, 12, 16);
    }

    [Fact]
    public void EveryLine_IsNonBlank()
    {
        Assert.All(DrinkExcuses.Lines, line => Assert.False(string.IsNullOrWhiteSpace(line)));
    }

    [Fact]
    public void EveryLine_IsUnique()
    {
        var distinct = new HashSet<string>(DrinkExcuses.Lines, System.StringComparer.Ordinal);
        Assert.Equal(DrinkExcuses.Lines.Count, distinct.Count);
    }

    [Fact]
    public void LineFor_IsDeterministic_SameSeedSameLine()
    {
        for (ulong seed = 0; seed < 64; seed++)
        {
            Assert.Equal(DrinkExcuses.LineFor(seed), DrinkExcuses.LineFor(seed));
        }
    }

    [Fact]
    public void LineFor_AlwaysReturnsAPoolLine()
    {
        // Sweep a wide seed span (incl. values past Count) — every pick is a real, in-pool line.
        for (ulong seed = 0; seed < 5000; seed++)
        {
            Assert.Contains(DrinkExcuses.LineFor(seed), DrinkExcuses.Lines);
        }
    }

    [Fact]
    public void LineFor_CoversEveryLine_AcrossContiguousSeeds()
    {
        // The pick is seed-modulo-Count, so Count contiguous seeds reach every shelf entry — no dead lines.
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        for (ulong seed = 0; seed < (ulong)DrinkExcuses.Lines.Count; seed++)
        {
            seen.Add(DrinkExcuses.LineFor(seed));
        }

        Assert.Equal(DrinkExcuses.Lines.Count, seen.Count);
    }

    [Fact]
    public void LineFor_RidesTheSameSeedFold_TheClientPoursWith()
    {
        // The client seeds each pour via DiceRule.Seed("drink-excuse", simTime, tot). Prove that fold is
        // stable end-to-end: the same sim moment + tot count always speaks the same excuse.
        ulong a = DiceRule.Seed("drink-excuse", 4200L, 1);
        ulong b = DiceRule.Seed("drink-excuse", 4200L, 1);
        Assert.Equal(DrinkExcuses.LineFor(a), DrinkExcuses.LineFor(b));
    }
}
