namespace SpaceSails.Core.Tests;

/// <summary>#210: one signed range-rate convention — positive closing (gap shrinking), negative
/// opening — so the Scope, the Nav contact line and the war-room tracks all say whether we are
/// getting closer, matching the tracked-target card.</summary>
public class RelativeMotionTests
{
    [Fact]
    public void ClosingSpeed_PositiveWhenApproaching()
    {
        // Other ship 1000 km ahead on +X; we chase at +5 km/s, it drifts at +2 km/s → gap shrinks 3 km/s.
        double c = RelativeMotion.ClosingSpeed(
            selfPos: new Vector2d(0, 0), selfVel: new Vector2d(5000, 0),
            otherPos: new Vector2d(1_000_000, 0), otherVel: new Vector2d(2000, 0));
        Assert.Equal(3000, c, 3);
        Assert.Equal("closing", RelativeMotion.ClosingWord(c));
    }

    [Fact]
    public void ClosingSpeed_NegativeWhenReceding()
    {
        // We fall back at +2 km/s while it runs at +5 km/s → gap grows: opening.
        double c = RelativeMotion.ClosingSpeed(
            new Vector2d(0, 0), new Vector2d(2000, 0),
            new Vector2d(1_000_000, 0), new Vector2d(5000, 0));
        Assert.Equal(-3000, c, 3);
        Assert.Equal("opening", RelativeMotion.ClosingWord(c));
    }

    [Fact]
    public void ClosingSpeed_PurelyLateralPassReadsZero()
    {
        // Relative velocity perpendicular to the line of sight — no range change.
        double c = RelativeMotion.ClosingSpeed(
            new Vector2d(0, 0), new Vector2d(0, 4000),
            new Vector2d(1_000_000, 0), new Vector2d(0, 0));
        Assert.Equal(0, c, 6);
    }

    [Fact]
    public void ClosingSpeed_ZeroRangeIsSafe()
    {
        double c = RelativeMotion.ClosingSpeed(
            new Vector2d(0, 0), new Vector2d(1000, 0),
            new Vector2d(0, 0), new Vector2d(0, 0));
        Assert.Equal(0, c);
    }

    [Fact]
    public void WordedAfter_SignAfterNumber_ForTheScope()
    {
        Assert.Equal("12.7 km/s closing", RelativeMotion.WordedAfter(12_700));
        Assert.Equal("12.7 km/s opening", RelativeMotion.WordedAfter(-12_700));
        // Steady range reads "closing" (the >= 0 tie), never a bare magnitude.
        Assert.Equal("0.0 km/s closing", RelativeMotion.WordedAfter(0));
    }

    [Fact]
    public void WordedBefore_SignBeforeNumber_ForTheContactLine()
    {
        Assert.Equal("closing 12.7 km/s", RelativeMotion.WordedBefore(12_700));
        Assert.Equal("opening 12.7 km/s", RelativeMotion.WordedBefore(-12_700));
        Assert.Equal("closing 5.28 km/s", RelativeMotion.WordedBefore(5_280, "F2"));
    }
}
