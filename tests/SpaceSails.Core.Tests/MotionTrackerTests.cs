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

    // ── #338 "The long ear": the tracker hears them long before you see them. ──

    [Fact]
    public void DetectionRange_IsAMultipleOfTheVisibleHalfExtent()
    {
        // The long ear reaches several times farther than the eye — the dread-gap made law.
        Assert.Equal(32.0 * MotionTracker.VisualRangeMultiple, MotionTracker.DetectionRange(32.0), 6);
        double mult = MotionTracker.VisualRangeMultiple;
        Assert.True(mult >= 3.0 && mult <= 5.0, "owner asked for order 3-5× the viewport half-extent");
        // A degenerate/zero viewport never yields a zero (or negative) reach.
        Assert.True(MotionTracker.DetectionRange(0) > 0);
    }

    [Fact]
    public void BlipIntensity_FirmsAsTheContactCloses()
    {
        const double det = 100.0;
        double near = MotionTracker.BlipIntensity(0, det);
        double mid = MotionTracker.BlipIntensity(50, det);
        double rim = MotionTracker.BlipIntensity(100, det);
        double beyond = MotionTracker.BlipIntensity(400, det);

        Assert.Equal(1.0, near, 6);                       // point-blank reads full-firm
        Assert.True(near > mid && mid > rim, "the fan must firm as a contact closes");
        Assert.Equal(MotionTracker.FaintFloor, rim, 6);   // on the rim it's the faint floor
        Assert.Equal(MotionTracker.FaintFloor, beyond, 6);// beyond the rim it holds the floor — never nothing
        Assert.True(MotionTracker.FaintFloor > 0, "an extreme contact still paints, only faint");
    }

    [Fact]
    public void DetectedMovingCount_CountsOnlyMoversWithinReach()
    {
        var entities = new[]
        {
            new MotionTracker.Entity(10, 0, -0.5, 0),   // near mover — heard
            new MotionTracker.Entity(90, 0, -0.5, 0),   // far mover, still inside 100 — heard
            new MotionTracker.Entity(200, 0, -0.5, 0),  // mover past the reach — NOT heard
            new MotionTracker.Entity(5, 0, 0, 0),       // point-blank but STILL — not a contact
        };

        Assert.Equal(2, MotionTracker.DetectedMovingCount(0, 0, entities, detectionRange: 100));
        Assert.Equal(0, MotionTracker.DetectedMovingCount(0, 0, entities, detectionRange: 1));
    }

    // ── #338 addendum · the first-contact chirp: edge-triggered on the 0→N transition, re-arming only
    //    after the fan has been genuinely clear for a while. ──

    [Fact]
    public void Chirp_FiresOnlyOnTheZeroToContactEdge()
    {
        MotionTracker.ChirpState s = MotionTracker.ChirpState.Fresh;

        // First contact of the excursion → chirp, then disarmed.
        (s, bool first) = MotionTracker.StepChirp(s, movingContacts: 1, dtSeconds: 0.1);
        Assert.True(first);
        Assert.False(s.Armed);

        // A lingering contact never re-chirps, no matter how many frames pass.
        for (int i = 0; i < 20; i++)
        {
            (s, bool again) = MotionTracker.StepChirp(s, movingContacts: 3, dtSeconds: 0.1);
            Assert.False(again);
        }
    }

    [Fact]
    public void Chirp_ReArmsOnlyAfterTheFanHasBeenClearForAWhile()
    {
        // Start disarmed, fan just went empty.
        var s = new MotionTracker.ChirpState(Armed: false, ClearSeconds: 0);

        // A brief blip of clear time (a contact flickering at extreme range) must NOT re-arm.
        (s, _) = MotionTracker.StepChirp(s, movingContacts: 0, dtSeconds: 1.0);
        (s, bool tooSoon) = MotionTracker.StepChirp(s, movingContacts: 2, dtSeconds: 0.1);
        Assert.False(tooSoon); // still within the hysteresis window — no re-chirp spam
        Assert.False(s.Armed);

        // Now let the fan stay genuinely clear past the re-arm window, then a fresh contact chirps again.
        s = new MotionTracker.ChirpState(Armed: false, ClearSeconds: 0);
        (s, _) = MotionTracker.StepChirp(s, movingContacts: 0, dtSeconds: MotionTracker.ChirpReArmSeconds + 0.5);
        Assert.True(s.Armed);
        (s, bool rearmed) = MotionTracker.StepChirp(s, movingContacts: 1, dtSeconds: 0.1);
        Assert.True(rearmed);
    }
}
