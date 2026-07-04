namespace SpaceSails.Core.Tests;

/// <summary>
/// M27: the mutual-visibility readout ("do we see them, or do they see us first") and the
/// close-range active radar. The telescope's double duty: watching targets AND minding our
/// own signature toward them.
/// </summary>
public class SensorAdvantageTests
{
    [Fact]
    public void Advantage_SunGlare_IsAsymmetric_TargetUpSunHidesFromUs()
    {
        // Them dead between us and the sun, 8e10 m out: our look is straight into the glare
        // (range ×0.25 = 2.5e10 m — blind); their look back at us is anti-sunward (full 1e11 m).
        var us = new ShipState(new Vector2d(1.0e11, 0), Vector2d.Zero, 0);
        var them = new ShipState(new Vector2d(2.0e10, 0), Vector2d.Zero, 0);

        SightAdvantage sight = SensorModel.Default.Advantage(us, them);

        Assert.True(sight.TheirRange > sight.OurRange, "The up-sun ship must win the eyes race.");
        Assert.False(sight.WeSeeThem);
        Assert.True(sight.TheySeeUs);
    }

    [Fact]
    public void Advantage_ChargedHull_GlowsInTheirRangeTerm()
    {
        // Side by side, perpendicular to the sun line — no glare either way. Dark hulls are
        // symmetric; charging OUR hull triples THEIR range on us and flips the edge.
        var us = new ShipState(new Vector2d(1.5e11, 0), Vector2d.Zero, 0);
        var themPos = new Vector2d(1.5e11, 5e10);

        SightAdvantage dark = SensorModel.Default.Advantage(us, new ShipState(themPos, Vector2d.Zero, 0));
        Assert.Equal(dark.OurRange, dark.TheirRange, precision: 3);
        Assert.Equal(0, dark.Edge, precision: 3);

        SightAdvantage glowing = SensorModel.Default.Advantage(
            us with { Charge = 1.0 }, new ShipState(themPos, Vector2d.Zero, 0));
        Assert.Equal(dark.TheirRange * (1 + SensorModel.ChargeGlowFactor), glowing.TheirRange, precision: 3);
        Assert.True(glowing.Edge < 0, "A glowing hull must lose the eyes race.");
        Assert.True(glowing.TheySeeUs);
    }

    [Fact]
    public void Radar_RangeAndLoudness()
    {
        var us = Vector2d.Zero;
        Assert.True(RadarRule.InRange(us, new Vector2d(4e9, 0)));
        Assert.False(RadarRule.InRange(us, new Vector2d(6e9, 0)));
        Assert.True(RadarRule.HearsPing(us, new Vector2d(4e10, 0)));
        Assert.False(RadarRule.HearsPing(us, new Vector2d(6e10, 0)));
    }
}
