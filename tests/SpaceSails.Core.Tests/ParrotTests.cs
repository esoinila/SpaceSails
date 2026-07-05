namespace SpaceSails.Core.Tests;

/// <summary>M28 (Sunday PR-E): the parrot squawks deterministically — same event, same
/// counter, same words, on any machine.</summary>
public class ParrotTests
{
    [Fact]
    public void Lines_AreDeterministic_AndRotate()
    {
        Assert.Equal(Parrot.Line(Parrot.Squawk.FiringSolution, 0), Parrot.Line(Parrot.Squawk.FiringSolution, 0));
        Assert.Equal("FIRING SOLUTION, CAPTAIN!", Parrot.Line(Parrot.Squawk.FiringSolution, 0));
        Assert.NotEqual(Parrot.Line(Parrot.Squawk.FiringSolution, 0), Parrot.Line(Parrot.Squawk.FiringSolution, 1));
        // The rotation wraps.
        Assert.Equal(Parrot.Line(Parrot.Squawk.HunterNear, 1), Parrot.Line(Parrot.Squawk.HunterNear, 4));
    }

    [Fact]
    public void ImpactLines_NameTheBody()
    {
        Assert.Contains("Mars", Parrot.Line(Parrot.Squawk.Impact, 0, "Mars"));
        Assert.Contains("the Sun", Parrot.Line(Parrot.Squawk.Impact, 2, "the Sun"));
    }

    [Fact]
    public void EveryEventKind_HasLines()
    {
        foreach (Parrot.Squawk kind in Enum.GetValues<Parrot.Squawk>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Parrot.Line(kind, 0)));
        }
    }
}
