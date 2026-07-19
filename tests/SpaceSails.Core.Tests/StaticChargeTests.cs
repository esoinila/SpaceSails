namespace SpaceSails.Core.Tests;

/// <summary>#369: the static-charge vent reads a rotating flavor pool — deterministic per vent,
/// same seed → same quip, on any machine.</summary>
public class StaticChargeTests
{
    [Fact]
    public void Pool_IsNonEmpty()
    {
        Assert.NotEmpty(StaticCharge.Lines);
    }

    [Fact]
    public void EveryLine_IsNonBlank_AndUnique()
    {
        foreach (string line in StaticCharge.Lines)
        {
            Assert.False(string.IsNullOrWhiteSpace(line), "A charge quip must not be blank.");
        }

        Assert.Equal(StaticCharge.Lines.Length, StaticCharge.Lines.Distinct().Count());
    }

    [Fact]
    public void LineFor_IsSeededDeterministic_AndRotates()
    {
        // Same seed → same words, every time.
        Assert.Equal(StaticCharge.LineFor(0), StaticCharge.LineFor(0));
        Assert.Equal(StaticCharge.Lines[0], StaticCharge.LineFor(0));

        // Consecutive seeds step through the pool.
        Assert.NotEqual(StaticCharge.LineFor(0), StaticCharge.LineFor(1));

        // The rotation wraps cleanly past the end.
        Assert.Equal(StaticCharge.LineFor(1), StaticCharge.LineFor(1 + StaticCharge.Lines.Length));
    }

    [Fact]
    public void LineFor_HandlesNegativeSeeds()
    {
        // A folded negative seed still lands inside the pool (no exception, valid line).
        Assert.Contains(StaticCharge.LineFor(-1), StaticCharge.Lines);
        Assert.Equal(StaticCharge.LineFor(-1), StaticCharge.LineFor(-1 + StaticCharge.Lines.Length));
    }
}
