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

    [Fact]
    public void HunterBacksOff_CracksAWoodenLegJoke()
    {
        // A warned or holed wolf turning tail gets a peg-leg jab across the whole rotation.
        for (int i = 0; i < 3; i++)
        {
            string line = Parrot.Line(Parrot.Squawk.HunterBacksOff, i);
            Assert.True(
                line.Contains("oak", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("timber", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("wood", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("peg", StringComparison.OrdinalIgnoreCase),
                $"expected a wooden-leg jab, got: {line}");
        }
    }

    [Fact]
    public void SpaceBarBreak_PunsOnTheName()
    {
        Assert.Contains("Space Bar", Parrot.Line(Parrot.Squawk.SpaceBarBreak, 0));
    }

    [Fact]
    public void Plunder_NamesTheHaul_AcrossTheRotation()
    {
        // #202: the bird names what was just stolen — the haul phrase fills the {0} slot in every line.
        for (int i = 0; i < 3; i++)
        {
            string line = Parrot.Line(Parrot.Squawk.Plunder, i, "six crates of He3 out of the Larkspur");
            Assert.Contains("six crates of He3 out of the Larkspur", line);
        }
    }
}
