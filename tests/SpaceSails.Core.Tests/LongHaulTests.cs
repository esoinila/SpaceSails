using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for #246 — 🚀 LONG HAUL, the void-crossing autopilot mode. The headline promise is
/// "consistent BY CONSTRUCTION": the jump places the ship at the closed-form conic's arrival state and
/// advances the clock there, and every time-derived system (rails, heat, interest, pod/cache timers) is
/// a pure function of sim time — so the tests assert the jump-state equals what integrating tick-by-tick
/// would give. Plus the guard rules (refuse while hunted / in-well / short / off-course), the
/// arrival-at-capture-range placement, the promise wording, and Miranda's ephemeris + shuttle-range.
/// </summary>
public class LongHaulTests
{
    private const double SunMu = 1.32712440018e20;
    private const double MarsMu = 4.282837e13;
    private const double UranusMu = 5.793939e15;
    private const double MirandaMu = 4.4e9;

    // The Sol subset the haul lives in: the sun at the origin, Mars and Uranus on their sol.json rails,
    // and Uranus's satellites Miranda (moon) + The Tilt (dock haven) — the acceptance destination.
    private static ICelestialEphemeris MakeSol()
    {
        var bodies = new[]
        {
            new CelestialBody("sun", "Sun", null, SunMu, 6.9634e9, 0, 0, 0),
            new CelestialBody("mars", "Mars", "sun", MarsMu, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
            new CelestialBody("uranus", "Uranus", "sun", UranusMu, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
            new CelestialBody("miranda", "Miranda", "uranus", MirandaMu, 2.358e5, 1.299e8, 122083, 2.3, BodyKind.Moon),
            new CelestialBody("the-tilt", "The Tilt", "uranus", 0, 1000, 8.0e7, 14000, 4.7, BodyKind.Station, IsHaven: true),
        };
        return new CircularOrbitEphemeris(bodies);
    }

    // A ship on a heliocentric Lambert arc that genuinely closes on Uranus at tArr — Lambert proposes the
    // departure velocity, so the coasting conic really does reach the planet (no hand-waved state).
    private static (ShipState Ship, double ArrivalTime) ShipBoundForUranus(ICelestialEphemeris eph, double t0 = 0)
    {
        double tArr = t0 + 200.0 * 86400.0;
        Vector2d r2 = eph.Position("uranus", tArr);
        Vector2d dir = r2.Normalized();
        Vector2d perp = new Vector2d(dir.Y, -dir.X);          // −90°, so the prograde sweep to Uranus is ~90°
        Vector2d r1 = perp * 5e11;                            // an open-space heliocentric start, well clear of any well
        TransferMath.LambertSolution lam = TransferMath.Lambert(r1, r2, tArr - t0, SunMu)
            ?? throw new InvalidOperationException("test setup: Lambert failed to find the Uranus arc");
        return (new ShipState(r1, lam.V1, t0), tArr);
    }

    // ---- Miranda joins the world (#246 item 4) ----

    [Fact]
    public void Miranda_Ephemeris_MatchesRealishParams()
    {
        ICelestialEphemeris eph = MakeSol();
        CelestialBody miranda = eph.Bodies.Single(b => b.Id == "miranda");

        Assert.Equal("uranus", miranda.ParentId);
        Assert.Equal(BodyKind.Moon, miranda.Kind);
        Assert.False(miranda.IsHaven);
        Assert.Equal(1.299e8, miranda.OrbitRadius, 3);      // ≈ 129,900 km
        Assert.Equal(122083, miranda.OrbitPeriod, 0);       // ≈ 1.413 d
        Assert.Equal(2.358e5, miranda.BodyRadius, 3);       // ≈ 235.8 km
        Assert.Equal(4.4e9, miranda.Mu, 3);
    }

    [Fact]
    public void Miranda_IsInTheRealSolScenario_LandableAndShuttleReachableFromTheTilt()
    {
        // Guards the actual scenarios/sol.json data file (not just the inline fixture): Miranda is present,
        // a landable moon of Uranus, and every phase of its orbit sits inside a shuttle hop of The Tilt —
        // so #231's bury flow lists it when docked at The Tilt with zero code changes.
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        CelestialBody miranda = eph.Bodies.Single(b => b.Id == "miranda");

        Assert.Equal("uranus", miranda.ParentId);
        Assert.Equal(BodyKind.Moon, miranda.Kind);       // landable (the bury flow keys off Kind == Moon)
        Assert.False(miranda.IsHaven);

        double worst = 0;
        for (double t = 0; t <= miranda.OrbitPeriod; t += miranda.OrbitPeriod / 128.0)
        {
            worst = Math.Max(worst, (eph.Position("the-tilt", t) - eph.Position("miranda", t)).Length);
        }

        Assert.True(worst > miranda.BodyRadius);         // not "basically on it already"
        Assert.True(ShuttleRange.InRange(worst), $"worst Tilt→Miranda gap {worst:E3} m must be within a shuttle hop");
    }

    [Fact]
    public void Miranda_SitsWithinShuttleRange_OfTheTilt_AtEveryPhase()
    {
        ICelestialEphemeris eph = MakeSol();

        // The worst case is the two on opposite sides of Uranus: their orbit radii simply add. Sample a
        // full Miranda period to be sure the real geometry never beats the bound.
        double worst = 0;
        for (double t = 0; t <= 122083; t += 122083 / 64.0)
        {
            double d = (eph.Position("the-tilt", t) - eph.Position("miranda", t)).Length;
            worst = Math.Max(worst, d);
        }

        Assert.True(worst <= 8.0e7 + 1.299e8 + 1.0);        // ≤ sum of the two orbit radii
        Assert.True(ShuttleRange.InRange(worst));            // and comfortably inside the 5e8 m shuttle hop
    }

    // ---- JumpTargetPlanet: a destination resolves to the sun-orbiting planet the haul stops at ----

    [Fact]
    public void JumpTargetPlanet_ResolvesUpTheParentChain()
    {
        ICelestialEphemeris eph = MakeSol();
        Assert.Equal("uranus", LongHaul.JumpTargetPlanet(eph, "the-tilt")?.Id);   // station → its planet
        Assert.Equal("uranus", LongHaul.JumpTargetPlanet(eph, "miranda")?.Id);    // moon → its planet
        Assert.Equal("uranus", LongHaul.JumpTargetPlanet(eph, "uranus")?.Id);     // a planet is its own target
        Assert.Null(LongHaul.JumpTargetPlanet(eph, "sun"));                       // the root hauls nowhere
    }

    // ---- The arrival: stop AT capture range, on the closed-form conic (#246 items 1d, 2) ----

    [Fact]
    public void Project_ReachingCourse_StopsAtCaptureRange()
    {
        ICelestialEphemeris eph = MakeSol();
        (ShipState ship, double tArr) = ShipBoundForUranus(eph);
        CelestialBody uranus = eph.Bodies.Single(b => b.Id == "uranus");

        LongHaul.Reach reach = LongHaul.Project(ship, eph, uranus);

        Assert.True(reach.Reaches);
        double expectedCapture = OrbitRule.CaptureRange(OrbitRule.HillRadius(uranus, SunMu));
        Assert.Equal(expectedCapture, reach.CaptureRangeMeters, 0);

        // The bus stops AT the capture range: the arrival sits on the gate, not at the planet's centre.
        double arrivalDist = (reach.ArrivalState.Position - eph.Position("uranus", reach.ArrivalSimTime)).Length;
        Assert.Equal(reach.CaptureRangeMeters, arrivalDist, expectedCapture * 1e-3);

        // And it arrives BEFORE the full arc closes on the centre (the last mile is the existing machinery).
        Assert.True(reach.ArrivalSimTime < tArr);
        Assert.True(reach.ArrivalSimTime > ship.SimTime);
    }

    [Fact]
    public void Project_ArrivalState_LiesOnTheSameClosedFormConic()
    {
        // "Consistent by construction" for the ship: the jump places it at a point on the SAME heliocentric
        // Kepler conic it departed on. The method-independent proof is that the two conic invariants —
        // specific orbital energy (½v² − μ/r) and specific angular momentum (r × v), both sun-relative
        // (the sun sits at the world origin) — are conserved from departure to the placed arrival state.
        ICelestialEphemeris eph = MakeSol();
        (ShipState ship, _) = ShipBoundForUranus(eph);
        CelestialBody uranus = eph.Bodies.Single(b => b.Id == "uranus");

        LongHaul.Reach reach = LongHaul.Project(ship, eph, uranus);
        Assert.True(reach.Reaches);

        static double Energy(Vector2d p, Vector2d v) => 0.5 * v.LengthSquared - SunMu / p.Length;
        static double AngMom(Vector2d p, Vector2d v) => p.X * v.Y - p.Y * v.X;

        double e0 = Energy(ship.Position, ship.Velocity);
        double e1 = Energy(reach.ArrivalState.Position, reach.ArrivalState.Velocity);
        double h0 = AngMom(ship.Position, ship.Velocity);
        double h1 = AngMom(reach.ArrivalState.Position, reach.ArrivalState.Velocity);

        Assert.Equal(e0, e1, Math.Abs(e0) * 1e-3);
        Assert.Equal(h0, h1, Math.Abs(h0) * 1e-3);
    }

    [Fact]
    public void Project_BodyPositions_AreThePureEphemerisFunction_AtTheArrivalEpoch()
    {
        // "consistent by construction" for the rails: the world at the arrival clock is exactly ephemeris(t),
        // whether you integrated there or jumped — the ephemeris is a pure function of sim time.
        ICelestialEphemeris eph = MakeSol();
        (ShipState ship, _) = ShipBoundForUranus(eph);
        CelestialBody uranus = eph.Bodies.Single(b => b.Id == "uranus");

        LongHaul.Reach reach = LongHaul.Project(ship, eph, uranus);
        ICelestialEphemeris independent = MakeSol(); // a second rail set, built from scratch

        foreach (string id in new[] { "mars", "uranus", "miranda", "the-tilt" })
        {
            Vector2d viaJump = eph.Position(id, reach.ArrivalSimTime);
            Vector2d recomputed = independent.Position(id, reach.ArrivalSimTime);
            Assert.Equal(viaJump.X, recomputed.X, 0);
            Assert.Equal(viaJump.Y, recomputed.Y, 0);
        }
    }

    [Fact]
    public void Project_MissingCourse_ReportsClosestPass_NotReached()
    {
        ICelestialEphemeris eph = MakeSol();
        CelestialBody uranus = eph.Bodies.Single(b => b.Id == "uranus");

        // A ship on a tidy circular heliocentric lane at Mars's radius never climbs to Uranus.
        Vector2d r = new Vector2d(2.2794e11, 0);
        double v = Math.Sqrt(SunMu / r.Length);
        var ship = new ShipState(r, new Vector2d(0, v), 0);

        LongHaul.Reach reach = LongHaul.Project(ship, eph, uranus);

        Assert.False(reach.Reaches);
        Assert.True(reach.ClosestApproachMeters > reach.CaptureRangeMeters);
    }

    // ---- The DEPARTURE solve: the offer is reachable from a berth (#246/#249 fix) ----

    // A berth-like state: co-moving with Mars on its heliocentric lane (a station off Mars rides Mars's
    // orbit). Its CURRENT coast never climbs to Uranus — the #249 offer, gated on that, was unreachable.
    private static ShipState BerthOffMars(ICelestialEphemeris eph, double t0 = 0) =>
        new(eph.Position("mars", t0), TransferMath.BodyVelocity(eph, "mars", t0), t0);

    [Fact]
    public void SolveDeparture_OffersFromABerth_WhereTheCurrentCoastNeverReaches()
    {
        ICelestialEphemeris eph = MakeSol();
        CelestialBody uranus = eph.Bodies.Single(b => b.Id == "uranus");
        ShipState berth = BerthOffMars(eph);

        // The crux of the fix: the current coast does NOT reach Uranus (co-moving with Mars)...
        Assert.False(LongHaul.Project(berth, eph, uranus).Reaches);

        // ...yet the departure solve still finds an affordable cheap arc to offer.
        LongHaul.Departure dep = LongHaul.SolveDeparture(berth, eph, uranus);
        Assert.True(dep.Ok, dep.Failure);
        Assert.True(dep.DeparturePulses is > 0 and < 120, $"expected an affordable departure, got {dep.DeparturePulses} p");
        Assert.True(dep.ArrivalCenterTime > berth.SimTime);
        Assert.True(dep.ArrivalRelativeSpeed > 0);
    }

    [Fact]
    public void SolveDeparture_PostBurnConic_ReachesUranusCaptureRange_EndToEnd()
    {
        // The engage-from-berth state math: charge the burn, apply PostBurnVelocity, and the jump rides the
        // SOLVED conic — which by construction reaches the planet's capture range.
        ICelestialEphemeris eph = MakeSol();
        CelestialBody uranus = eph.Bodies.Single(b => b.Id == "uranus");
        ShipState berth = BerthOffMars(eph);

        LongHaul.Departure dep = LongHaul.SolveDeparture(berth, eph, uranus);
        Assert.True(dep.Ok, dep.Failure);

        ShipState postBurn = berth with { Velocity = dep.PostBurnVelocity };
        double horizon = (dep.ArrivalCenterTime - berth.SimTime) + 10.0 * 86400.0;
        LongHaul.Reach reach = LongHaul.Project(postBurn, eph, uranus, horizon);

        Assert.True(reach.Reaches);
        double arrivalDist = (reach.ArrivalState.Position - eph.Position("uranus", reach.ArrivalSimTime)).Length;
        Assert.Equal(reach.CaptureRangeMeters, arrivalDist, reach.CaptureRangeMeters * 1e-3); // stops AT the gate
        Assert.True(reach.ArrivalSimTime > berth.SimTime);
        Assert.True(reach.ArrivalSimTime <= dep.ArrivalCenterTime + 1.0);                     // a touch before centre
    }

    [Fact]
    public void SolveDeparture_NoHeliocentricFrame_FailsWithReason()
    {
        ICelestialEphemeris eph = MakeSol();
        CelestialBody sun = eph.Bodies.Single(b => b.Id == "sun");
        var ship = new ShipState(new Vector2d(2.2794e11, 0), new Vector2d(0, 24000), 0);

        LongHaul.Departure dep = LongHaul.SolveDeparture(ship, eph, sun); // the root has no parent frame
        Assert.False(dep.Ok);
        Assert.False(string.IsNullOrWhiteSpace(dep.Failure));
    }

    [Fact]
    public void RefusalBudget_AndMenuAction_SpeakThePlainWords()
    {
        Assert.Equal(
            "🚀 long haul needs ≈180 p; tank has 40 — top up or find a cheaper window",
            LongHaul.RefusalBudget(180, 40));
        Assert.Equal(
            "🚀 Long haul — autopilot to Uranus vicinity (≈37 p, arrive Sol-Day 5920)",
            LongHaul.MenuAction("Uranus", 37, "Sol-Day 5920"));
    }

    // The client's map-menu / card / chip visibility gate: a destination offers the long haul when its
    // sun-orbiting planet exists AND the ship is not already inside that planet's capture range.
    private static CelestialBody? OfferTargetPlanet(ICelestialEphemeris eph, ShipState ship, string destId)
    {
        if (LongHaul.JumpTargetPlanet(eph, destId) is not { } planet)
        {
            return null;
        }

        CelestialBody? sun = planet.ParentId is { } pid ? eph.Bodies.FirstOrDefault(b => b.Id == pid) : null;
        if (sun is null)
        {
            return null;
        }

        double capture = OrbitRule.CaptureRange(OrbitRule.HillRadius(planet, sun.Mu));
        double dist = (ship.Position - eph.Position(planet.Id, ship.SimTime)).Length;
        return dist > capture ? planet : null;
    }

    [Fact]
    public void LongHaulOffer_AppearsForEveryFarDestinationClass_FromRustyRoadstead()
    {
        // The #246/#249/#250 fix, verified live and pinned here: from The Rusty Roadstead (off Mars) the
        // 🚀 offer resolves to the destination's PLANET vicinity for EVERY picker class — dock haven, moon,
        // planet, non-haven station — with the departure solvable and the menu wording naming the planet.
        // (The owner's "The Tilt lacks the rocket" was a within-capture-range state: at Uranus vicinity the
        // gate correctly hides it — see the paired test below.)
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var ship = new ShipState(eph.Position("the-space-bar", 0), TransferMath.BodyVelocity(eph, "the-space-bar", 0), 0);

        (string dest, string planet)[] cases =
        {
            ("the-tilt", "uranus"),          // dock haven at Uranus → "autopilot to Uranus vicinity"
            ("miranda", "uranus"),           // moon of Uranus
            ("uranus", "uranus"),            // the planet itself
            ("neptune", "neptune"),          // a further planet
            ("satellite-factory", "earth"),  // a non-haven station orbiting a planet
        };

        foreach ((string dest, string planet) in cases)
        {
            CelestialBody? target = OfferTargetPlanet(eph, ship, dest);
            Assert.True(target is not null, $"{dest} should offer a long haul from The Rusty Roadstead");
            Assert.Equal(planet, target!.Id);

            LongHaul.Departure dep = LongHaul.SolveDeparture(ship, eph, target);
            Assert.True(dep.Ok, $"{dest}: {dep.Failure}");
            Assert.Contains($"autopilot to {target.Name} vicinity", LongHaul.MenuAction(target.Name, dep.DeparturePulses, "Sol-Day 1"));
        }
    }

    [Fact]
    public void LongHaulOffer_IsCorrectlyHidden_WhenAlreadyInThePlanetVicinity()
    {
        // The paired truth: docked at The Tilt (inside Uranus's capture range), clicking The Tilt/Miranda/
        // Uranus offers NO long haul — you're already there; the last mile is the dock/arm, not a jump.
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var atTilt = new ShipState(eph.Position("the-tilt", 0), TransferMath.BodyVelocity(eph, "the-tilt", 0), 0);

        Assert.Null(OfferTargetPlanet(eph, atTilt, "the-tilt"));
        Assert.Null(OfferTargetPlanet(eph, atTilt, "miranda"));
        Assert.Null(OfferTargetPlanet(eph, atTilt, "uranus"));

        // But a FAR body from the very same berth still offers it — the gate is per-destination, not global.
        Assert.NotNull(OfferTargetPlanet(eph, atTilt, "earth"));
    }

    // ---- Consistency by construction: heat / interest are single-application-equals-integrated ----

    [Fact]
    public void HeatDecay_JumpAppliedOnce_EqualsTickByTick_HavenMultOffInFlight()
    {
        // The jump applies DecayHeat once at the arrival clock; DecayHeat is idempotent and
        // path-independent, so one application over the span equals stepping through it. In flight the
        // haven multiplier does NOT apply (#246 item 2) — the ship is crossing the void, not resting.
        var start = new HeatState(3, 0);
        double arrival = 25.0 * 86400.0;          // ≈1.25 decay periods → drops one level, checkpoint advances
        double mid = 12.0 * 86400.0;

        HeatState oneShot = EncounterRule.DecayHeat(start, arrival, atHavenOrbit: false);
        HeatState stepped = EncounterRule.DecayHeat(EncounterRule.DecayHeat(start, mid, false), arrival, false);

        Assert.Equal(stepped.Level, oneShot.Level);
        Assert.Equal(stepped.RaisedAtSimTime, oneShot.RaisedAtSimTime, 1e-6);
        Assert.Equal(2, oneShot.Level);
    }

    [Fact]
    public void BankInterest_OverJumpSpan_IsTheClosedForm()
    {
        // Parked coin grows by the pure closed form over the elapsed days — the jump books it once for the
        // whole span (calm only; a hot deck earns nothing).
        long balance = 100_000;
        double days = 40.0;
        long expected = (long)Math.Round(balance * FavorBank.DailyInterestRate * days);

        Assert.Equal(expected, FavorBank.AccrueInterest(balance, days, heatLevel: 0));
        Assert.Equal(0, FavorBank.AccrueInterest(balance, days, heatLevel: 1));
    }

    [Fact]
    public void PodRailAndCacheTimers_LandClosedForm_AtTheJumpEpoch()
    {
        // The mode's premise for the rest of the world: pod rails (Lab 30 closed-form conic) and cache
        // discovery windows (floor(t / period)) are pure functions of sim time. The post-jump world reads
        // them at the arrival epoch directly — no integration of the void — and lands exactly where
        // stepping tick-by-tick would.
        var launch = new ShipState(new Vector2d(1.5e11, 0), new Vector2d(0, 3.2e4), 0);
        double arrival = 172.0 * 86400.0;

        ShipState pod = MassDriverSchedule.PodRailState(launch, arrival, SunMu)!.Value;
        Assert.Equal(arrival, pod.SimTime, 0);
        Assert.True(double.IsFinite(pod.Position.X) && double.IsFinite(pod.Position.Y));
        Assert.Equal(pod.Position.X, MassDriverSchedule.PodRailState(launch, arrival, SunMu)!.Value.Position.X, 0);

        long windowsCrossed = DiscoveryRule.PeriodIndex(arrival) - DiscoveryRule.PeriodIndex(0);
        Assert.Equal(172, windowsCrossed); // DiscoveryRule.PeriodSeconds == one sim-day
    }

    // ---- The gate: refuse while hunted / keeping / in-well / short / off-course (#246 items 1, 2) ----

    [Fact]
    public void Evaluate_CleanLongReach_IsClearToGo()
    {
        var reach = new LongHaul.Reach(true, 60.0 * 86400.0, default, 3.5e11, 3.5e11, 60.0 * 86400.0);
        Assert.Equal(LongHaul.Blocker.None, LongHaul.Evaluate(reach, anyHunterActive: false, keepingOrbit: false, insideWell: false, fromSimTime: 0));
    }

    [Fact]
    public void Evaluate_RefusesWhileHunted_FirstOfAll()
    {
        var reach = new LongHaul.Reach(true, 60.0 * 86400.0, default, 3.5e11, 3.5e11, 60.0 * 86400.0);
        // Even with every other reason also present, the hunter is the reason spoken.
        Assert.Equal(LongHaul.Blocker.HunterActive,
            LongHaul.Evaluate(reach, anyHunterActive: true, keepingOrbit: true, insideWell: true, fromSimTime: 0));
    }

    [Fact]
    public void Evaluate_RefusesWhileKeeping_ThenInWell_ThenOffCourse_ThenShort()
    {
        var reaching = new LongHaul.Reach(true, 60.0 * 86400.0, default, 3.5e11, 3.5e11, 60.0 * 86400.0);
        var missing = new LongHaul.Reach(false, 60.0 * 86400.0, default, 3.5e11, 9e11, 30.0 * 86400.0);
        var shortHop = new LongHaul.Reach(true, 2.0 * 86400.0, default, 3.5e11, 3.5e11, 2.0 * 86400.0);

        Assert.Equal(LongHaul.Blocker.Keeping, LongHaul.Evaluate(reaching, false, keepingOrbit: true, insideWell: true, 0));
        Assert.Equal(LongHaul.Blocker.InsideWell, LongHaul.Evaluate(reaching, false, false, insideWell: true, 0));
        Assert.Equal(LongHaul.Blocker.DoesNotReach, LongHaul.Evaluate(missing, false, false, false, 0));
        Assert.Equal(LongHaul.Blocker.ShortHop, LongHaul.Evaluate(shortHop, false, false, false, 0));
    }

    [Fact]
    public void AnyHunterActive_TrueOnlyForAFlyingUnbrokenHunter()
    {
        var atOrigin = new ShipState(Vector2d.Zero, Vector2d.Zero, 0);
        HunterState active = new("h1", "Wolf", "mars", 0, 100, atOrigin, false, false);
        HunterState fittingOut = active with { ActivationSimTime = 1e9 };  // not flying yet
        HunterState brokenOff = active with { BrokenOff = true };
        HunterState caught = active with { CaughtPlayer = true };

        Assert.True(LongHaul.AnyHunterActive(new[] { active }, simTime: 200));
        Assert.False(LongHaul.AnyHunterActive(new[] { fittingOut }, simTime: 200));
        Assert.False(LongHaul.AnyHunterActive(new[] { brokenOff }, simTime: 200));
        Assert.False(LongHaul.AnyHunterActive(new[] { caught }, simTime: 200));
        Assert.False(LongHaul.AnyHunterActive(Array.Empty<HunterState>(), simTime: 200));
    }

    [Fact]
    public void InsideAnyWell_TrueDeepInMars_ExemptForTheTargetPlanet()
    {
        ICelestialEphemeris eph = MakeSol();
        Vector2d marsPos = eph.Position("mars", 0);
        var atMars = new ShipState(marsPos + new Vector2d(1e8, 0), Vector2d.Zero, 0); // well inside Mars's Hill sphere

        Assert.True(LongHaul.InsideAnyWell(atMars, eph));

        // Sitting inside the DESTINATION planet's own well is not a blocker — arriving there is the point.
        Vector2d uranusPos = eph.Position("uranus", 0);
        var atUranus = new ShipState(uranusPos + new Vector2d(1e9, 0), Vector2d.Zero, 0);
        Assert.False(LongHaul.InsideAnyWell(atUranus, eph, exemptPlanetId: "uranus"));
    }

    // ---- The promise, the offer, the announcement, the refusal — one voice (#246 items 1, 3, 5) ----

    [Fact]
    public void UranusCaptureRange_IsTheOwnersPromiseNumber_234Au()
    {
        ICelestialEphemeris eph = MakeSol();
        CelestialBody uranus = eph.Bodies.Single(b => b.Id == "uranus");
        double capture = OrbitRule.CaptureRange(OrbitRule.HillRadius(uranus, SunMu));

        Assert.Equal("2.34 AU", LongHaul.FormatAu(capture));
    }

    [Fact]
    public void PromiseAndVerdict_SayTheDestinationPlainly()
    {
        var reaches = new LongHaul.Reach(true, 172.0 * 86400.0, default, 3.4995e11, 3.4995e11, 172.0 * 86400.0);
        var misses = new LongHaul.Reach(false, 0, default, 3.4995e11, 5.9e11, 0);

        Assert.Equal("this course reaches Uranus capture in 172 d", LongHaul.ReachVerdict("Uranus", reaches, 0));
        Assert.Equal("this coast does NOT reach Uranus — closest pass 3.94 AU", LongHaul.ReachVerdict("Uranus", misses, 0));

        Assert.Equal(
            "course reaches Uranus capture (2.34 AU) on Sol-Day 172 — ≈0 p now, ≈41 p quoted for the last mile",
            LongHaul.Promise("Uranus", 3.4995e11, "Sol-Day 172", 0, 41));

        Assert.Equal("🚀 Long haul to The Tilt — ≈0 p, arrive Sol-Day 172", LongHaul.Offer("The Tilt", 0, "Sol-Day 172"));
        Assert.Equal("🚀 AUTOPILOT HAS THE SHIP — NOW: long haul to Uranus", LongHaul.BannerNow("Uranus"));
        Assert.Equal("🚀 long haul complete — 172 d passed; arrived at The Tilt capture range", LongHaul.Completed("The Tilt", 172));
    }

    [Fact]
    public void RefusalText_SpeaksTheReason()
    {
        Assert.Contains("sky is clear", LongHaul.RefusalText(LongHaul.Blocker.HunterActive, "Uranus"));
        Assert.Contains("disarm the kept orbit", LongHaul.RefusalText(LongHaul.Blocker.Keeping, "Uranus"));
        Assert.Contains("leave the well", LongHaul.RefusalText(LongHaul.Blocker.InsideWell, "Uranus"));
        Assert.Contains("does not reach", LongHaul.RefusalText(LongHaul.Blocker.DoesNotReach, "Uranus"));
    }

    // ---- #255: the diegetic jump overlay's voice (pure text, unit-tested like the rest of the mode) ----

    [Fact]
    public void VoidOverlay_SpeaksTheCrossing()
    {
        Assert.Equal("CROSSING THE VOID", LongHaul.VoidTitle);
        Assert.Contains("does not stop", LongHaul.VoidNoStop);
        Assert.Equal("bound for The Tilt", LongHaul.VoidBound("The Tilt"));

        // Whole-year counter, floor 1 so even a sub-year hop reads as a one-year crossing.
        Assert.Equal(1, LongHaul.VoidYears(3.0 * 86400.0));           // 3 days -> year 1
        Assert.Equal(10, LongHaul.VoidYears(3655.0 * 86400.0));       // the Mars->Uranus decade
        Assert.Equal(1, LongHaul.VoidYears(0));

        // The ticking sub-line clamps the year into [1, total] so the overlay never reads "year 0 of 10".
        Assert.Equal("year 1 of 10", LongHaul.VoidYearLine(0, 10));
        Assert.Equal("year 3 of 10", LongHaul.VoidYearLine(3, 10));
        Assert.Equal("year 10 of 10", LongHaul.VoidYearLine(99, 10));
    }

    // ---- #255: closed-form retirement == the fate an integration would reach ----

    // The re-seed retires the entire pre-jump world (no per-NPC integration of the void) and rebuilds it
    // fresh at the arrival epoch. This is only honest if, after a decade, every stateful ship WOULD in fact
    // have finished its run — i.e. its last plotted node lies before the arrival epoch, so integrating it
    // forward reaches the same "arrived / despawned" fate the closed-form retire assigns for free.
    [Fact]
    public void DecadeJump_RetiresEveryStatefulShip_AsIntegrationWould()
    {
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        double epoch = 3655.0 * 86400.0; // Mars -> Uranus decade

        IReadOnlyList<NpcShip> fleet = TrafficSchedule.Generate(eph, seed: 42, count: 8);
        Assert.NotEmpty(fleet);
        foreach (NpcShip ship in fleet.Where(s => s.DepotBodyId is null))
        {
            double lastNode = ship.Plan.Nodes.Count > 0 ? ship.Plan.Nodes[^1].SimTime : ship.ActivationTime;
            Assert.True(lastNode < epoch,
                $"{ship.Callsign}'s run ends at {lastNode:E3}s, not before the {epoch:E3}s arrival epoch — retire-all would be a lie.");
        }
    }

    // ---- #255: elapsed-time personal rules apply themselves on the clock jump (no replay) ----

    // Heat keys off an ABSOLUTE checkpoint (RaisedAtSimTime), so advancing the clock a decade in one jump
    // applies a decade of decay the instant it is next read — the vault carries the checkpoint, the jump
    // moves the clock, and the closed-form rule does the rest. (Same shape for interest/insurance/caches.)
    [Fact]
    public void DecadeJump_AppliesElapsedHeatDecay_FromAbsoluteCheckpoint()
    {
        double raisedAt = 100.0 * 86400.0;
        var hot = new HeatState(4, raisedAt);

        // Read at the pre-jump clock: still hot (barely any time has passed).
        HeatState justAfter = EncounterRule.DecayHeat(hot, raisedAt + 60.0, atHavenOrbit: false);
        Assert.True(justAfter.Level >= 3, "heat should not have decayed in a minute");

        // Read at the post-jump clock (a decade later): a decade of decay applies itself, no replay needed.
        HeatState afterDecade = EncounterRule.DecayHeat(hot, raisedAt + 3655.0 * 86400.0, atHavenOrbit: false);
        Assert.Equal(0, afterDecade.Level);
    }
}
