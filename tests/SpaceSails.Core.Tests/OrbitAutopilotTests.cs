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

    // ---- Body avoidance (issues #127/#128): the owner's Saturn playtest flew THROUGH the planet ----

    // Two-body periapsis of a state relative to a body — the closest the ballistic fall would come.
    private static double Periapsis(Vector2d relPos, Vector2d relVel, double mu)
    {
        double r = relPos.Length;
        double v2 = relVel.LengthSquared;
        double energy = v2 / 2 - mu / r;
        double h = relPos.X * relVel.Y - relPos.Y * relVel.X; // specific angular momentum (scalar)
        double a = -mu / (2 * energy);
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (mu * mu)));
        return a * (1 - e); // valid for ellipse (a>0) and hyperbola (a<0, e>1) alike
    }

    [Fact]
    public void SafeApproach_AimsOffCenter_SoTheFocusedFallClearsThePlanetSurface()
    {
        // A straight fall aimed at Saturn's CENTER gravity-focuses into the disc (the bug). The
        // safe approach offsets the aim by the impact parameter that keeps the ballistic periapsis
        // at ~2 body radii — proven here by computing the two-body periapsis of the post-burn state.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        CelestialBody saturn = ephemeris.Bodies.First(b => b.Id == "saturn");
        Vector2d saturnPos = ephemeris.Position("saturn", 0);
        Vector2d saturnVel = (ephemeris.Position("saturn", 1.0) - ephemeris.Position("saturn", -1.0)) / 2.0;

        // Ship far out, dead-aimed at Saturn's centre, at rest in Saturn's frame — worst case.
        var ship = new ShipState(saturnPos + new Vector2d(3e11, 0), saturnVel, 0);

        // The NAIVE approach (aims at centre) would punch the surface: prove the failure first.
        Vector2d naive = OrbitRule.ApproachVelocity(ship, saturnPos, saturnVel);
        double naivePeriapsis = Periapsis(ship.Position - saturnPos, naive - saturnVel, saturn.Mu);
        Assert.True(naivePeriapsis < saturn.BodyRadius,
            $"Precondition: the naive centre-aimed fall should focus below the surface ({naivePeriapsis:E2} m).");

        // The SAFE approach arcs to a periapsis at or above the surface (target ~2 R).
        Vector2d safe = OrbitRule.SafeApproachVelocity(ship, saturnPos, saturnVel, saturn);
        double safePeriapsis = Periapsis(ship.Position - saturnPos, safe - saturnVel, saturn.Mu);
        Assert.True(safePeriapsis > saturn.BodyRadius,
            $"Safe approach must clear Saturn's surface — periapsis {safePeriapsis:E2} m vs R {saturn.BodyRadius:E2} m.");
        Assert.InRange(safePeriapsis, saturn.BodyRadius, saturn.BodyRadius * OrbitRule.ApproachSafeBodyRadii * 1.2);
    }

    [Fact]
    public void SafeApproach_ToAMoonBehindItsPlanet_HeadsAroundThePlanet_NotIntoIt()
    {
        // Enceladus (the owner's haven) sits ~4 Saturn-radii out. Approaching from the far side of
        // Saturn, a straight aim at Enceladus threads the planet. The safe approach heads for a
        // tangent that keeps Saturn clear — the heading's perpendicular miss of Saturn is ≥ safe R.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        CelestialBody saturn = ephemeris.Bodies.First(b => b.Id == "saturn");
        Vector2d saturnPos = ephemeris.Position("saturn", 0);
        Vector2d saturnVel = (ephemeris.Position("saturn", 1.0) - ephemeris.Position("saturn", -1.0)) / 2.0;
        CelestialBody moon = ephemeris.Bodies.First(b => b.Id == "enceladus");
        Vector2d moonPos = ephemeris.Position("enceladus", 0);
        Vector2d moonVel = (ephemeris.Position("enceladus", 1.0) - ephemeris.Position("enceladus", -1.0)) / 2.0;

        // Ship on the far side of Saturn from Enceladus, so the naive chord crosses the planet.
        Vector2d awayFromMoon = (saturnPos - moonPos).Normalized();
        var ship = new ShipState(saturnPos + awayFromMoon * 2.3e9, saturnVel, 0);
        double safeR = saturn.BodyRadius * OrbitRule.ParentSafeBodyRadii;
        var obstacle = new OrbitRule.ApproachObstacle(saturnPos, safeR);

        // Naive heading (aimed at the moon) skims Saturn's centre — confirm the hazard exists.
        Vector2d naiveHeading = (OrbitRule.ApproachVelocity(ship, moonPos, moonVel) - moonVel);
        Assert.True(PerpMiss(ship.Position, naiveHeading, saturnPos) < saturn.BodyRadius,
            "Precondition: the naive aim at the moon should pass through Saturn.");

        // Safe heading keeps Saturn's safe radius clear.
        Vector2d safeHeading = OrbitRule.SafeApproachVelocity(ship, moonPos, moonVel, moon, obstacle) - moonVel;
        double miss = PerpMiss(ship.Position, safeHeading, saturnPos);
        Assert.True(miss >= safeR * 0.999,
            $"Safe approach must round Saturn — heading misses the centre by {miss:E2} m, need ≥ {safeR:E2} m.");
    }

    // Perpendicular distance from a point to the forward ray (origin + t*dir, t≥0).
    private static double PerpMiss(Vector2d origin, Vector2d dir, Vector2d point)
    {
        Vector2d u = dir.Normalized();
        Vector2d w = point - origin;
        double along = w.Dot(u);
        if (along <= 0) return w.Length; // point is behind the heading
        return (w - u * along).Length;
    }

    [Fact]
    public void AutoOrbit_MoonBehindItsPlanet_NeverEntersThePlanet_AndStillCaptures()
    {
        // End to end: armed autopilot for Titan, started on the far side of Saturn. The approach
        // must round Saturn (never inside its body radius) and still park at Titan.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        CelestialBody saturn = ephemeris.Bodies.First(b => b.Id == "saturn");
        CelestialBody titan = ephemeris.Bodies.First(b => b.Id == "titan");
        double hill = OrbitRule.HillRadius(titan, saturn.Mu);

        Vector2d saturnPos0 = ephemeris.Position("saturn", 0);
        Vector2d titanPos0 = ephemeris.Position("titan", 0);
        Vector2d awayFromTitan = (saturnPos0 - titanPos0).Normalized();
        Vector2d saturnVel0 = (ephemeris.Position("saturn", 1.0) - ephemeris.Position("saturn", -1.0)) / 2.0;
        // 1.5e9 out the far side of Saturn, near rest in Saturn's frame — well inside Titan capture.
        var ship = new ShipState(saturnPos0 + awayFromTitan * 1.5e9, saturnVel0, 0);

        double minSaturn = double.MaxValue;
        bool inserted = false;
        double day = 86400;
        for (double t = 0; t < 120 * day && !inserted; t += 60)
        {
            Vector2d saturnPos = ephemeris.Position("saturn", ship.SimTime);
            minSaturn = Math.Min(minSaturn, (ship.Position - saturnPos).Length);

            Vector2d bodyPos = ephemeris.Position("titan", ship.SimTime);
            double h = 1.0;
            Vector2d bodyVel = (ephemeris.Position("titan", ship.SimTime + h) - ephemeris.Position("titan", ship.SimTime - h)) / (2 * h);
            var obstacle = new OrbitRule.ApproachObstacle(saturnPos, saturn.BodyRadius * OrbitRule.ParentSafeBodyRadii);

            switch (OrbitRule.AutopilotDecision(ship, bodyPos, bodyVel, titan, hill))
            {
                case OrbitRule.AutopilotAction.Approach:
                    ship = OrbitRule.Approach(ship, bodyPos, bodyVel, titan, obstacle);
                    break;
                case OrbitRule.AutopilotAction.Insert:
                    ship = OrbitRule.Insert(ship, bodyPos, bodyVel, titan);
                    Assert.True(OrbitRule.IsBound(ship, bodyPos, bodyVel, titan, hill));
                    inserted = true;
                    continue;
            }

            ship = simulator.Step(ship);
            minSaturn = Math.Min(minSaturn, (ship.Position - ephemeris.Position("saturn", ship.SimTime)).Length);
        }

        Assert.True(minSaturn > saturn.BodyRadius,
            $"Autopilot flew inside Saturn's surface ({minSaturn:E2} m < R {saturn.BodyRadius:E2} m).");
        Assert.True(inserted, "Autopilot failed to park at Titan within 120 days.");
    }

    [Fact]
    public void AutoOrbit_Planet_WithSafeApproach_CapturesWithoutDippingBelowTheSurface()
    {
        // The planet case with the safe (b-plane-offset) approach: it must still capture Earth AND
        // never bring the ship below Earth's surface before the insertion fires.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        CelestialBody earth = ephemeris.Bodies.First(b => b.Id == "earth");
        CelestialBody sun = ephemeris.Bodies.First(b => b.Id == "sun");
        double hill = OrbitRule.HillRadius(earth, sun.Mu);

        Vector2d earthPos0 = ephemeris.Position("earth", 0);
        Vector2d earthVel0 = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        var ship = new ShipState(
            earthPos0 + new Vector2d(4 * hill, 0),
            earthVel0 + new Vector2d(-2000, 12000), 0);

        double minEarth = double.MaxValue;
        bool inserted = false;
        double day = 86400;
        for (double t = 0; t < 90 * day && !inserted; t += 60)
        {
            Vector2d bodyPos = ephemeris.Position("earth", ship.SimTime);
            minEarth = Math.Min(minEarth, (ship.Position - bodyPos).Length);
            double h = 1.0;
            Vector2d bodyVel = (ephemeris.Position("earth", ship.SimTime + h) - ephemeris.Position("earth", ship.SimTime - h)) / (2 * h);
            switch (OrbitRule.AutopilotDecision(ship, bodyPos, bodyVel, earth, hill))
            {
                case OrbitRule.AutopilotAction.Approach:
                    ship = OrbitRule.Approach(ship, bodyPos, bodyVel, earth, null); // planet: no parent obstacle
                    break;
                case OrbitRule.AutopilotAction.Insert:
                    ship = OrbitRule.Insert(ship, bodyPos, bodyVel, earth);
                    inserted = true;
                    continue;
            }

            ship = simulator.Step(ship);
            minEarth = Math.Min(minEarth, (ship.Position - ephemeris.Position("earth", ship.SimTime)).Length);
        }

        Assert.True(inserted, "Safe-approach autopilot failed to capture Earth within 90 days.");
        Assert.True(minEarth > earth.BodyRadius,
            $"Autopilot dipped below Earth's surface ({minEarth:E2} m < R {earth.BodyRadius:E2} m).");
    }
}
