namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-313 · The surface motion tracker — the crude sweep that reads MOVEMENT, not presence. These pin
/// the honest-to-the-trope laws: a mover paints, a still contact is invisible, and the ping cadence
/// quickens as the nearest blip closes.
/// </summary>
public class MotionTrackerTests
{
    [Fact]
    public void MovingContact_Paints_WithBearingAndRange()
    {
        // A Reever due "east" (+x) of the captain, shambling.
        var e = new MotionTracker.Entity(X: 10, Y: 0, Vx: -1.0, Vy: 0);
        MotionTracker.Blip? blip = MotionTracker.Read(0, 0, in e);

        Assert.NotNull(blip);
        Assert.Equal(0.0, blip!.Value.Bearing, 3);   // atan2(0,10) = 0
        Assert.Equal(10.0, blip.Value.Range, 3);
    }

    [Fact]
    public void StillContact_IsInvisible()
    {
        var still = new MotionTracker.Entity(X: 3, Y: 4, Vx: 0, Vy: 0);
        Assert.Null(MotionTracker.Read(0, 0, in still));
        Assert.False(MotionTracker.IsMoving(0, 0));

        // Below the still-speed floor still reads as motionless (render jitter must not conjure a blip).
        Assert.False(MotionTracker.IsMoving(MotionTracker.StillSpeed * 0.5, 0));
        Assert.True(MotionTracker.IsMoving(MotionTracker.StillSpeed * 2, 0));
    }

    [Fact]
    public void Sweep_ReturnsOnlyMovers_NearestFirst()
    {
        var entities = new[]
        {
            new MotionTracker.Entity(30, 0, -0.5, 0),   // far mover
            new MotionTracker.Entity(5, 0, -0.5, 0),    // near mover
            new MotionTracker.Entity(2, 0, 0, 0),       // point-blank but STILL → dropped
        };

        var blips = MotionTracker.Sweep(0, 0, entities);

        Assert.Equal(2, blips.Count);
        Assert.Equal(5.0, blips[0].Range, 3);   // nearest first
        Assert.Equal(30.0, blips[1].Range, 3);
    }

    [Theory]
    [InlineData(null, MotionTracker.Cadence.Silent)]
    [InlineData(4.0, MotionTracker.Cadence.Imminent)]
    [InlineData(12.0, MotionTracker.Cadence.Closing)]
    [InlineData(40.0, MotionTracker.Cadence.Distant)]
    public void CadenceFor_BandsByNearestRange(double? nearest, MotionTracker.Cadence expected)
    {
        Assert.Equal(expected, MotionTracker.CadenceFor(nearest));
    }

    [Fact]
    public void Readout_SpeaksInHouseVoice()
    {
        Assert.Equal("movement — 40 du, closing", MotionTracker.Readout(40, closing: true));
        Assert.Equal("movement — 12 du, drifting", MotionTracker.Readout(12.4, closing: false));
        Assert.Equal("no movement — for now", MotionTracker.Readout(null, closing: false));
    }

    // ── #330 · the left-edge instrument column: the tracker seats under the SANITY plate, big, and
    //    stays fully on-screen (shrinking rather than clipping on a small viewport). ──

    private const double ColumnTop = 82.0;   // the SANITY plate's bottom + gap, as DeckView uses

    [Fact]
    public void TrackerAnchor_SeatsInTheLeftColumn_BelowTheSanityPlate()
    {
        (double cx, double cy) = MotionTracker.TrackerAnchor(1280, 800, radius: 116, ColumnTop);

        // Left edge: the disc hugs the left inset, well left of centre.
        Assert.True(cx < 1280 / 2.0, "the tracker must sit on the LEFT, not centre/right");
        Assert.Equal(18 + 116 + 6, cx, 3);   // leftInset + radius + disc pad

        // Below the plate: its caption (radius+18 above the centre) clears the column top.
        Assert.True(cy - (116 + 18) >= ColumnTop - 1e-6, "the tracker must sit BELOW the SANITY plate");
    }

    [Fact]
    public void TrackerAnchor_StaysInsideTheViewport_AtEverySize()
    {
        foreach ((int w, int h) in new[] { (1280, 800), (1024, 640), (768, 500), (480, 400), (360, 320) })
        {
            double r = MotionTracker.TrackerRadius(w, h, ColumnTop, desired: 116);
            (double cx, double cy) = MotionTracker.TrackerAnchor(w, h, r, ColumnTop);

            // The whole disc is within the viewport horizontally and vertically.
            Assert.True(cx - r >= 0, $"disc runs off the left at {w}x{h}");
            Assert.True(cx + r <= w, $"disc runs off the right at {w}x{h}");
            Assert.True(cy - r >= 0, $"disc runs off the top at {w}x{h}");
            Assert.True(cy + r <= h, $"disc runs off the bottom at {w}x{h}");
        }
    }

    [Fact]
    public void TrackerRadius_ShrinksOnSmallViewports_ButNeverBelowTheFloor_NorAboveDesired()
    {
        double big = MotionTracker.TrackerRadius(1280, 800, ColumnTop, desired: 116);
        double small = MotionTracker.TrackerRadius(360, 320, ColumnTop, desired: 116);

        Assert.Equal(116, big, 3);            // a roomy viewport gets the full desired size
        Assert.True(small < big, "a small viewport must shrink the disc");
        Assert.True(small >= 44, "but never collapse below the readable floor");
    }
}
