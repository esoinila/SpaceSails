namespace SpaceSails.Core.Tests;

public class ReferenceFrameTests
{
    // A stationary frame (body hasn't moved between sample time and now) is the identity — this is
    // the Sun/inertial special case the caller relies on to keep the default draw path unchanged.
    [Fact]
    public void CoMoving_BodyUnmoved_IsIdentity()
    {
        var sample = new Vector2d(3e11, -7e10);
        var body = new Vector2d(1.4e12, 9e11);

        Vector2d result = ReferenceFrame.CoMoving(sample, body, body);

        Assert.Equal(sample, result);
    }

    // The sample's offset from the frame body at the SAMPLE time is preserved, re-pinned at the
    // body's ANCHOR ("now") position: a point 5e8 m off the body maps to 5e8 m off the anchor.
    [Fact]
    public void CoMoving_PreservesOffset_RepinnedAtAnchor()
    {
        var offset = new Vector2d(5e8, -2e8);
        var bodyAtSample = new Vector2d(-1.2e12, 3.3e11);
        var anchor = new Vector2d(8.1e11, -9.9e11);
        Vector2d sample = bodyAtSample + offset;

        Vector2d result = ReferenceFrame.CoMoving(sample, bodyAtSample, anchor);

        Assert.Equal(anchor.X + offset.X, result.X, 3);
        Assert.Equal(anchor.Y + offset.Y, result.Y, 3);
    }

    // A ship glued to the body (zero offset at every sample time) collapses to a single point — the
    // anchor. This is why a co-moving path shows RELATIVE motion: shared motion cancels out.
    [Fact]
    public void CoMoving_SampleEqualsBody_CollapsesToAnchor()
    {
        var anchor = new Vector2d(2e12, 2e12);
        var bodyAtSampleA = new Vector2d(1e12, 0);
        var bodyAtSampleB = new Vector2d(0, 1e12);

        Vector2d a = ReferenceFrame.CoMoving(bodyAtSampleA, bodyAtSampleA, anchor);
        Vector2d b = ReferenceFrame.CoMoving(bodyAtSampleB, bodyAtSampleB, anchor);

        Assert.Equal(anchor, a);
        Assert.Equal(anchor, b);
    }

    // Real geometry: a ship co-moving with Titan, sampled a while later, should trace Titan's motion
    // RELATIVE to Saturn — not Saturn's ~10 km/s solar streak. In the Saturn frame the ship's plotted
    // point sits far from where the heliocentric sample lands, and near Saturn's current position.
    [Fact]
    public void CoMoving_SaturnFrame_RemovesSolarDrift()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        double now = 0;
        double later = 20 * 86400.0; // twenty days out — Saturn's solar drift dwarfs Titan's orbit by then

        Vector2d titanNow = ephemeris.Position("titan", now);
        Vector2d titanLater = ephemeris.Position("titan", later);
        Vector2d saturnNow = ephemeris.Position("saturn", now);
        Vector2d saturnLater = ephemeris.Position("saturn", later);

        // Ship sits 3000 km off Titan and co-moves with it: its future sample IS Titan-plus-offset.
        var offset = new Vector2d(3e6, 0);
        Vector2d shipSampleLater = titanLater + offset;

        Vector2d framed = ReferenceFrame.CoMoving(shipSampleLater, saturnLater, saturnNow);

        // Heliocentric, the 3-day sample has drifted with Saturn's solar orbit — far from Saturn-now.
        double helioDriftFromSaturnNow = (shipSampleLater - saturnNow).Length;
        // In the Saturn frame it's pinned to Titan's position relative to Saturn (± the 3000 km offset),
        // i.e. within roughly one Titan orbital radius of Saturn — the solar streak is gone.
        double framedDistFromSaturnNow = (framed - saturnNow).Length;
        double titanOrbitRadius = (titanNow - saturnNow).Length;

        Assert.True(framedDistFromSaturnNow < titanOrbitRadius + offset.Length + 1,
            "Saturn-framed sample should stay within Titan's orbit radius of Saturn, whatever the epoch");
        Assert.True(helioDriftFromSaturnNow > 4 * framedDistFromSaturnNow,
            "heliocentric sample should have drifted far past the bounded Saturn-framed one");
    }
}
