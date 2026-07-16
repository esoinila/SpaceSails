namespace SpaceSails.Core.Tests;

/// <summary>#201: the plotting panel's burn angle is ship-heading-relative by default — 0 ahead,
/// +90 starboard, −90 port — while the flown heading stays world-space. This maps the two.</summary>
public class BurnHeadingConventionTests
{
    [Fact]
    public void RelativeToWorld_ZeroIsStraightAhead()
    {
        Assert.Equal(30, BurnHeadingConvention.RelativeToWorld(shipHeadingDeg: 30, relativeDeg: 0), 6);
    }

    [Fact]
    public void RelativeToWorld_StarboardIsAClockwiseQuarterTurn()
    {
        // Nose along +X (heading 0): starboard (+90 relative) points to −Y in the world's CCW frame,
        // i.e. world 270° — the ship's right on a screen whose +Y runs down.
        Assert.Equal(270, BurnHeadingConvention.RelativeToWorld(0, 90), 6);
        // Port (−90 relative) is the opposite side, world 90°.
        Assert.Equal(90, BurnHeadingConvention.RelativeToWorld(0, -90), 6);
    }

    [Fact]
    public void WorldToRelative_ReadsSignedOffTheHeading()
    {
        Assert.Equal(0, BurnHeadingConvention.WorldToRelative(30, 30), 6);
        Assert.Equal(90, BurnHeadingConvention.WorldToRelative(0, 270), 6);   // starboard reads +90
        Assert.Equal(-90, BurnHeadingConvention.WorldToRelative(0, 90), 6);   // port reads −90
    }

    [Fact]
    public void WorldToRelative_WrapsToSignedHalfTurn_NeverA0To360Bearing()
    {
        double r = BurnHeadingConvention.WorldToRelative(10, 350);
        Assert.InRange(r, -180, 180);
        Assert.Equal(20, r, 6); // 10 − 350 = −340 → +20, not 20-vs-340 confusion
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(30, 45)]
    [InlineData(123, -90)]
    [InlineData(300, 90)]
    [InlineData(200, 179)]
    public void RelativeToWorld_And_Back_RoundTrips(double shipHeading, double relative)
    {
        double world = BurnHeadingConvention.RelativeToWorld(shipHeading, relative);
        Assert.InRange(world, 0, 360);
        Assert.Equal(relative, BurnHeadingConvention.WorldToRelative(shipHeading, world), 6);
    }
}
