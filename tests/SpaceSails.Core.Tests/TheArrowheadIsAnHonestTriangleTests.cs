using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #933 · THE ARROWHEAD'S GEOMETRY, on its own. <see cref="VelocityArrow"/> is pure — three screen points out
/// of an angle and a pixel — so the shape can be held to its own promises without a canvas anywhere near it.
/// What the client does WITH these points (which frame the angle came from, that it is drawn while paused,
/// that it never scales with zoom) is held by <c>TheShipSaysWhichWaySheIsGoingTests</c> next door, against a
/// real map's command buffer.
/// </summary>
public sealed class TheArrowheadIsAnHonestTriangleTests
{
    private static float[] Head(double dirRad, float x = 100f, float y = 60f)
    {
        float[] xy = new float[6];
        VelocityArrow.Points(dirRad, x, y, xy);
        return xy;
    }

    private static double Side(float[] t, int p, int q) =>
        Math.Sqrt(Math.Pow(t[p * 2] - t[q * 2], 2) + Math.Pow(t[(p * 2) + 1] - t[(q * 2) + 1], 2));

    /// <summary>The angle AT THE TIP is the one Core names — whatever the two lengths are set to, because the
    /// half-width is solved from the FULL height rather than typed in beside it. Change
    /// <see cref="VelocityArrow.LengthPx"/> or <see cref="VelocityArrow.BaseBehindPx"/> and this still holds;
    /// hard-code a half-width and it stops holding the moment either moves.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-2.5)]
    [InlineData(3.14159)]
    public void TheApexIsTheApexAngle(double dirRad)
    {
        float[] t = Head(dirRad);
        double a = Side(t, 0, 1), b = Side(t, 0, 2), c = Side(t, 1, 2);
        double apex = Math.Acos(((a * a) + (b * b) - (c * c)) / (2 * a * b)) * 180.0 / Math.PI;

        // Four decimals: the points come out as float32 (that is what the canvas buffer takes), so a 19 px
        // side carries about a millionth of a pixel of rounding — which is four orders of magnitude finer
        // than the smallest wrong apex anybody could type, and still not a promise this can keep at six.
        Assert.Equal(VelocityArrow.ApexDegrees, apex, 4);
        Assert.Equal(a, b, 4);   // isosceles: the two sides off the tip are the same length
    }

    /// <summary>The tip stands <see cref="VelocityArrow.LengthPx"/> ahead of the marker along the given angle,
    /// and the base's midpoint <see cref="VelocityArrow.BaseBehindPx"/> behind it — so the triangle straddles
    /// the ship dot instead of floating off her bow.</summary>
    [Fact]
    public void TheTipLeadsAndTheBaseSitsJustBehindTheMarker()
    {
        const double dir = 0.7;
        float[] t = Head(dir, 100f, 60f);

        Assert.Equal(100 + (VelocityArrow.LengthPx * Math.Cos(dir)), t[0], 3);
        Assert.Equal(60 + (VelocityArrow.LengthPx * Math.Sin(dir)), t[1], 3);

        double midX = (t[2] + t[4]) / 2.0, midY = (t[3] + t[5]) / 2.0;
        Assert.Equal(100 - (VelocityArrow.BaseBehindPx * Math.Cos(dir)), midX, 3);
        Assert.Equal(60 - (VelocityArrow.BaseBehindPx * Math.Sin(dir)), midY, 3);

        // …and the axis really points where it was asked to: tip minus base midpoint, back to an angle.
        Assert.Equal(dir, Math.Atan2(t[1] - midY, t[0] - midX), 6);
    }

    /// <summary>Turn the angle and the triangle turns with it, rigidly: the same three side lengths at every
    /// bearing. A shape that got longer to the north-east would be a shape reporting a speed it was never
    /// given.</summary>
    [Fact]
    public void TurningTheAngleTurnsTheShapeAndNothingElse()
    {
        double[] sides = [Side(Head(0), 0, 1), Side(Head(0), 0, 2), Side(Head(0), 1, 2)];
        for (double dir = -3.0; dir < 3.0; dir += 0.37)
        {
            float[] t = Head(dir);
            Assert.Equal(sides[0], Side(t, 0, 1), 3);
            Assert.Equal(sides[1], Side(t, 0, 2), 3);
            Assert.Equal(sides[2], Side(t, 1, 2), 3);
        }
    }

    /// <summary>Pure: the same six floats for the same three arguments, every time, with no state behind
    /// them.</summary>
    [Fact]
    public void ItIsPure()
    {
        Assert.Equal(Head(1.2, 40f, 90f), Head(1.2, 40f, 90f));
        Assert.NotEqual(Head(1.2, 40f, 90f), Head(1.3, 40f, 90f));
    }

    /// <summary>The ring threshold: below it a ring, at and above it a dart — and a NaN speed (a velocity
    /// nobody could take an angle of) falls to the ring rather than to a spinning dart.</summary>
    [Fact]
    public void TheRingThresholdIsWhereItSays()
    {
        Assert.True(VelocityArrow.ShowsRing(0));
        Assert.True(VelocityArrow.ShowsRing(VelocityArrow.RingBelowMps - 1e-9));
        Assert.False(VelocityArrow.ShowsRing(VelocityArrow.RingBelowMps));
        Assert.False(VelocityArrow.ShowsRing(3000));
        Assert.True(VelocityArrow.ShowsRing(double.NaN));
    }

    /// <summary>Three points is six floats, and a span too short throws rather than handing back half a
    /// triangle for something to draw.</summary>
    [Fact]
    public void HalfATriangleIsNotAShape() =>
        Assert.Throws<ArgumentException>(() => VelocityArrow.Points(0, 0, 0, new float[5]));
}
