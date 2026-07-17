namespace SpaceSails.Core.Tests;

/// <summary>
/// The shared co-moving berth construction (#269): completing a clamp — like a docked start, a shuttle
/// arrival or a vault-resume boot — snaps the ship onto the haven's rail, not merely sets a flag where
/// the ship happened to float. These tests pin the construction against the real Sol ephemeris so the
/// 103,989 km "clamped on while diving at Uranus" bug can never return.
/// </summary>
public class BerthStateTests
{
    private static ICelestialEphemeris SolEphemeris() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    [Fact]
    public void CoMoving_PlacesShipAtBerthOffset_MatchingBodyDrift()
    {
        ICelestialEphemeris eph = SolEphemeris();
        const double t = 4.2e8; // a far-future epoch: the berth is deterministic from time
        Vector2d bodyPos = eph.Position("the-tilt", t);

        ShipState berth = BerthState.CoMoving(eph, "the-tilt", t, BerthState.BerthOffsetMeters);

        // Position sits a berth's width off the body, no further.
        double gap = (berth.Position - bodyPos).Length;
        Assert.Equal(BerthState.BerthOffsetMeters, gap, 1.0);

        // Velocity IS the body's own orbital drift (central-difference), so rel speed is ~zero.
        Vector2d bodyVel = (eph.Position("the-tilt", t + 1) - eph.Position("the-tilt", t - 1)) / 2;
        Assert.True((berth.Velocity - bodyVel).Length < 1e-3, "the berth must ride the body's exact drift");
        Assert.Equal(t, berth.SimTime);
    }

    [Fact]
    public void CoMoving_FromFarDivergentApproach_LandsInsideTheDockEnvelope()
    {
        // The #269 scenario: a ship 100,000+ km out on a conic diving past the haven. Before the fix the
        // clamp froze the arm at that separation; now completing it rebuilds the ship AT the berth.
        ICelestialEphemeris eph = SolEphemeris();
        const double t = 3.0e8;
        Vector2d tiltPos = eph.Position("the-tilt", t);
        Vector2d tiltVel = (eph.Position("the-tilt", t + 1) - eph.Position("the-tilt", t - 1)) / 2;

        ShipState berth = BerthState.CoMoving(eph, "the-tilt", t, BerthState.BerthOffsetMeters);

        // The rebuilt state is genuinely clampable: inside the envelope AND matched under the shear cap.
        Assert.True(DockRule.InEnvelope(berth, tiltPos, tiltVel, bodyRadius: 1000),
            "the co-moving berth must satisfy the very dock gate that granted it");
        Assert.True((berth.Position - tiltPos).Length <= DockRule.EnvelopeMeters);
        Assert.True((berth.Velocity - tiltVel).Length <= DockRule.MatchSpeed);
    }

    [Fact]
    public void CoMoving_PreservesCharge()
    {
        ICelestialEphemeris eph = SolEphemeris();
        ShipState berth = BerthState.CoMoving(eph, "the-tilt", 1.0e8, BerthState.BerthOffsetMeters, charge: 4200);
        Assert.Equal(4200, berth.Charge);
    }
}
