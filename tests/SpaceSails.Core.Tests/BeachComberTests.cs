namespace SpaceSails.Core.Tests;

/// <summary>
/// The beach-comber kit (owner rulings, Evening-wind playtest 2026-07-18): the D100 probe that decides
/// whether any surface square holds shallow treasure, is too hard to dig, or comes up empty. These pin
/// the owner's die — deterministic per (body, square), the three bands, and the modest-find shape — so
/// a swept grid never lies and a fishing expedition is honest luck, not an economy.
/// </summary>
public class BeachComberTests
{
    [Fact]
    public void Roll_IsDeterministic_PerBodyAndSquare()
    {
        Probe a = BeachComber.Roll("miranda", 4, -19);
        Probe b = BeachComber.Roll("miranda", 4, -19);
        Assert.Equal(a, b); // same throw, same find, forever — the swept grid's guarantee
    }

    [Fact]
    public void Roll_DiffersByBodyAndSquare()
    {
        // The die is keyed on both the body and the square: a spot on one moon is a fresh throw on another,
        // and a step over is a fresh square. (Not every neighbour differs — but the streams are independent,
        // so across a spread the faces move.)
        int bodyDiffers = 0, squareDiffers = 0;
        for (int i = 0; i < 60; i++)
        {
            if (BeachComber.Roll("miranda", i, 3).Roll != BeachComber.Roll("phobos", i, 3).Roll)
            {
                bodyDiffers++;
            }
            if (BeachComber.Roll("miranda", i, 3).Roll != BeachComber.Roll("miranda", i + 1, 3).Roll)
            {
                squareDiffers++;
            }
        }
        Assert.True(bodyDiffers > 40, $"body should reseed the throw (differed {bodyDiffers}/60)");
        Assert.True(squareDiffers > 40, $"square should reseed the throw (differed {squareDiffers}/60)");
    }

    [Fact]
    public void Roll_FaceIsAlwaysAHonestD100()
    {
        for (int x = -50; x < 50; x++)
        {
            Probe p = BeachComber.Roll("miranda", x, x * 2);
            Assert.InRange(p.Roll, 1, 100);
        }
    }

    [Fact]
    public void Bands_MatchTheRollFace()
    {
        // The outcome is exactly the band the face fell in — bedrock low, find high, nothing between.
        for (int x = -400; x < 400; x++)
        {
            Probe p = BeachComber.Roll("miranda", x, 7);
            BeachComber.Outcome expected =
                p.Roll <= BeachComber.TooHardMax ? BeachComber.Outcome.TooHard
                : p.Roll >= BeachComber.ShallowMin ? BeachComber.Outcome.Shallow
                : BeachComber.Outcome.Nothing;
            Assert.Equal(expected, p.Outcome);
        }
    }

    [Fact]
    public void Distribution_IsMostlyNothing_WithRareFindsAndSomeHardGround()
    {
        // Owner: "unlucky to find anything but still possible", "some surfaces may be too hard". Over a
        // wide sweep of squares the bands should land near their D100 widths — nothing dominant, finds rare.
        int nothing = 0, hard = 0, find = 0, total = 0;
        for (int x = -100; x < 100; x++)
        {
            for (int y = -100; y < 100; y++)
            {
                total++;
                switch (BeachComber.Roll("miranda", x, y).Outcome)
                {
                    case BeachComber.Outcome.Nothing: nothing++; break;
                    case BeachComber.Outcome.TooHard: hard++; break;
                    case BeachComber.Outcome.Shallow: find++; break;
                }
            }
        }
        // Nothing is the overwhelming case; finds are a thin band; bedrock a modest one. Loose bounds so
        // the test pins the SHAPE without being brittle to the exact face distribution.
        Assert.True(nothing > total * 0.75, $"nothing should dominate (was {nothing}/{total})");
        Assert.InRange(find, (int)(total * 0.01), (int)(total * 0.09));   // ~4% band
        Assert.InRange(hard, (int)(total * 0.06), (int)(total * 0.20));   // ~12% band
    }

    [Fact]
    public void HardGround_CarriesNoFind()
    {
        // A bedrock square never coughs up coin or scrap — the refusal is total.
        for (int x = -400; x < 400; x++)
        {
            Probe p = BeachComber.Roll("miranda", x, 11);
            if (p.IsTooHard)
            {
                Assert.Equal(0, p.FindCoin);
                Assert.Equal(0, p.FindScrapUnits);
                Assert.False(p.IsFind);
            }
        }
    }

    [Fact]
    public void ShallowFind_IsModest_AndDeterministic()
    {
        // Every find pays inside the modest coin band and at most a single scrap unit — luck, not a payday.
        int findsSeen = 0;
        for (int x = -400; x < 400; x++)
        {
            Probe p = BeachComber.Roll("miranda", x, 5);
            if (!p.IsFind)
            {
                continue;
            }
            findsSeen++;
            Assert.InRange(p.FindCoin, BeachComber.MinFindCoin, BeachComber.MaxFindCoin);
            Assert.InRange(p.FindScrapUnits, 0, 1);
            // The find replays identically (the swept mark and the payout must never drift).
            Assert.Equal(p, BeachComber.Roll("miranda", x, 5));
        }
        Assert.True(findsSeen > 0, "the sweep should have turned up at least one shallow find");
    }

    [Fact]
    public void SquareOf_And_SquareCenter_RoundTripIntoTheSameSquare()
    {
        // A world point maps to a square, and that square's centre maps back to the SAME square — so a
        // probe at your feet and its rendered checked mark always agree on which cell was swept.
        double[] xs = [-43.9, -12.0, -0.1, 0.0, 3.2, 14.7, 33.4];
        double[] ys = [-83.5, -70.0, -27.1, -20.4, -5.0];
        foreach (double x in xs)
        {
            foreach (double y in ys)
            {
                (int sx, int sy) = BeachComber.SquareOf(x, y);
                (double cx, double cy) = BeachComber.SquareCenter(sx, sy);
                Assert.Equal((sx, sy), BeachComber.SquareOf(cx, cy));
            }
        }
    }

    [Fact]
    public void SquareOf_FloorsCleanly_AcrossZero()
    {
        // Floor semantics: negative coordinates bucket without a gap or an overlap at the origin.
        Assert.Equal((-1, -1), BeachComber.SquareOf(-0.01, -0.01));
        Assert.Equal((0, 0), BeachComber.SquareOf(0.0, 0.0));
        Assert.Equal((0, 0), BeachComber.SquareOf(BeachComber.SquareSize - 0.001, BeachComber.SquareSize - 0.001));
        Assert.Equal((1, 1), BeachComber.SquareOf(BeachComber.SquareSize, BeachComber.SquareSize));
    }
}
