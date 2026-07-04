namespace SpaceSails.Core.Tests;

/// <summary>
/// M25: the armed orbit autopilot — "point at it and throttle" done by the ship's systems.
/// Threading the bare Hill sphere with prograde-only pulses proved impossible in playtests,
/// so inside a generous capture range the autopilot burns onto a straight fall, trims when
/// the sun's tide bends it off, and inserts once safely deep inside the Hill sphere.
/// </summary>
public class OrbitAutopilotTests
{
    [Fact]
    public void CaptureRange_HasFloor_ForNeedleSizedHillSpheres()
    {
        // Mercury-class Hill sphere (~2e8 m): the floor dominates.
        Assert.Equal(OrbitRule.CaptureRangeFloorMeters, OrbitRule.CaptureRange(2e8));
        // Earth-class (~1.5e9 m): 5 Hill radii dominate.
        Assert.Equal(5 * 1.5e9, OrbitRule.CaptureRange(1.5e9));
    }

    [Fact]
    public void ClosingSpeed_PositiveWhenApproaching_NegativeWhenReceding()
    {
        var bodyPos = new Vector2d(1e9, 0);
        var bodyVel = Vector2d.Zero;
        var approaching = new ShipState(Vector2d.Zero, new Vector2d(3000, 0), 0);
        var receding = new ShipState(Vector2d.Zero, new Vector2d(-3000, 0), 0);

        Assert.Equal(3000, OrbitRule.ClosingSpeed(approaching, bodyPos, bodyVel), precision: 6);
        Assert.Equal(-3000, OrbitRule.ClosingSpeed(receding, bodyPos, bodyVel), precision: 6);
    }

    [Fact]
    public void ApproachVelocity_FallsStraightAtTheBody_AtTheSafeSpeed()
    {
        var bodyPos = new Vector2d(5e9, 0);
        var bodyVel = new Vector2d(0, 30000);
        var ship = new ShipState(Vector2d.Zero, new Vector2d(12000, 20000), 0);

        Vector2d v = OrbitRule.ApproachVelocity(ship, bodyPos, bodyVel);

        Vector2d rel = v - bodyVel;
        Assert.Equal(OrbitRule.MaxRelativeSpeed * OrbitRule.ApproachSpeedFraction, rel.Length, precision: 6);
        Assert.True(rel.X > 0 && Math.Abs(rel.Y) < 1e-6, "Approach must point straight at the body.");
    }

    [Fact]
    public void Autopilot_DoesNothing_BeyondCaptureRange()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        CelestialBody earth = ephemeris.Bodies.First(b => b.Id == "earth");
        CelestialBody sun = ephemeris.Bodies.First(b => b.Id == "sun");
        double hill = OrbitRule.HillRadius(earth, sun.Mu);

        Vector2d earthPos = ephemeris.Position("earth", 0);
        Vector2d earthVel = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        var far = new ShipState(earthPos + new Vector2d(10 * hill, 0), earthVel + new Vector2d(0, 20000), 0);

        Assert.Equal(OrbitRule.AutopilotAction.None,
            OrbitRule.AutopilotDecision(far, earthPos, earthVel, earth, hill));
    }

    [Fact]
    public void Autopilot_CapturesFromFourHillRadii_ArrivingFastAndOffAxis()
    {
        // The owner's actual failure: got within map-visible range of the planet but far too
        // fast and not aimed at the needle. Armed autopilot must take it from here — approach
        // burn(s), tidal trims, and an insertion deep enough inside the Hill sphere to hold.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        CelestialBody earth = ephemeris.Bodies.First(b => b.Id == "earth");
        CelestialBody sun = ephemeris.Bodies.First(b => b.Id == "sun");
        double hill = OrbitRule.HillRadius(earth, sun.Mu);

        Vector2d earthPos0 = ephemeris.Position("earth", 0);
        Vector2d earthVel0 = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        // 4 Hill radii out, 12 km/s relative, pointed sideways — hand-flown this sails past.
        var ship = new ShipState(
            earthPos0 + new Vector2d(4 * hill, 0),
            earthVel0 + new Vector2d(-2000, 12000), 0);

        int approachBurns = 0, pulses = 0;
        bool inserted = false;
        double day = 86400;
        for (double t = 0; t < 90 * day && !inserted; t += 60)
        {
            Vector2d bodyPos = ephemeris.Position("earth", ship.SimTime);
            double h = 1.0;
            Vector2d bodyVel = (ephemeris.Position("earth", ship.SimTime + h) - ephemeris.Position("earth", ship.SimTime - h)) / (2 * h);
            switch (OrbitRule.AutopilotDecision(ship, bodyPos, bodyVel, earth, hill))
            {
                case OrbitRule.AutopilotAction.Approach:
                    pulses += OrbitRule.ApproachPulseCost(ship, bodyPos, bodyVel);
                    ship = OrbitRule.Approach(ship, bodyPos, bodyVel);
                    approachBurns++;
                    break;
                case OrbitRule.AutopilotAction.Insert:
                    pulses += OrbitRule.PulseCost(ship, bodyPos, bodyVel, earth);
                    ship = OrbitRule.Insert(ship, bodyPos, bodyVel, earth);
                    Assert.True(OrbitRule.IsBound(ship, bodyPos, bodyVel, earth, hill));
                    // Deep enough that the sun's tide won't strip the parking orbit.
                    Assert.True((ship.Position - bodyPos).Length < hill * OrbitRule.AutopilotInsertHillFraction * 1.01);
                    inserted = true;
                    continue;
            }

            ship = simulator.Step(ship);
        }

        Assert.True(inserted, "Autopilot failed to capture within 90 days.");
        Assert.InRange(approachBurns, 1, 10);
        Assert.InRange(pulses, 1, 220); // fits the 250-pulse tank the player starts with

        // And the parking orbit holds: distance to Earth stays inside the Hill sphere for 10 days.
        for (int i = 0; i < 10; i++)
        {
            ship = simulator.Run(ship, day);
            double d = (ship.Position - ephemeris.Position("earth", ship.SimTime)).Length;
            Assert.True(d < hill, $"Parked orbit drifted out of the Hill sphere on day {i + 1} ({d:E2} m).");
        }
    }
}
