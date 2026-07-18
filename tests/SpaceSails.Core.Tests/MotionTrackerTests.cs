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
}
