using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

public class TrafficAndPredictionTests
{
    private const double Day = 86400;
    private const double SunMu = 1.32712440018e20;
    private const double EarthOrbitRadius = 1.496e11;

    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    [Fact]
    public void TrafficSchedule_IsDeterministic()
    {
        var ephemeris = Sol();

        IReadOnlyList<NpcShip> first = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);
        IReadOnlyList<NpcShip> second = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Id, second[i].Id);
            Assert.Equal(first[i].Callsign, second[i].Callsign);
            Assert.Equal(first[i].DepartureTime, second[i].DepartureTime);
            Assert.Equal(first[i].InitialState, second[i].InitialState); // bit-identical record struct
            Assert.True(first[i].Plan.Nodes.SequenceEqual(second[i].Plan.Nodes));
        }
    }

    // #255 — the long-haul re-seed leans on GenerateWave's existing "seed the world AT an epoch"
    // ability (owner's architecture: reuse the tested spawn mechanism instead of integrating the void).
    // Seeding at a future epoch T is deterministic and produces a world genuinely "at T" — mid-flight
    // ships are en route as of T, not t=0 — so ambient traffic repopulates correctly after a decade jump.
    [Fact]
    public void GenerateWave_SeedsTheWorldAtEpoch_Deterministically()
    {
        var ephemeris = Sol();
        double epoch = 3655.0 * 86400.0; // the Mars->Uranus decade the jump lands at

        IReadOnlyList<NpcShip> a = TrafficSchedule.GenerateWave(ephemeris, seed: 99, count: 8, epoch, waveNumber: 2);
        IReadOnlyList<NpcShip> b = TrafficSchedule.GenerateWave(ephemeris, seed: 99, count: 8, epoch, waveNumber: 2);
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Id, b[i].Id);
            Assert.Equal(a[i].InitialState, b[i].InitialState);
        }

        // Mid-flight ships (a virtual PAST departure) are active in the neighbourhood of T, not near 0 —
        // the world clock has moved to the arrival epoch.
        var midFlight = a.Where(s => s.DepartureTime < epoch).ToList();
        Assert.True(midFlight.Count >= 3, $"Expected several mid-flight ships at epoch, got {midFlight.Count}.");
        foreach (NpcShip ship in midFlight)
        {
            Assert.InRange(ship.ActivationTime, epoch - 1.0, epoch + 7300.0);
            Assert.Equal(ship.ActivationTime, ship.InitialState.SimTime);
        }
    }

    // #255 — the freeze class: the live StepNpcs catch-up guard. A gap of honest warp (a frame's worth,
    // even a full accumulator-clamped 5.6 h) must integrate; a gap of an epoch leap (the 8.3-year "the-tilt"
    // vault resume, the Mars->Uranus decade jump) must NOT — integrating years at the 60 s NpcTimeStep is
    // millions of Steps and a frozen tab, so the caller retires the mover instead. The boundary is pure and
    // lives in Core so it is unit-testable, not buried in Map.razor.
    [Fact]
    public void CatchUpGuard_IntegratesHonestWarp_ButRetiresAnEpochLeap()
    {
        // The widest HONEST per-frame catch-up: MaxStepsPerFrame(20000) × player dt(1 s) + one NpcTimeStep of
        // overshoot ~= 5.6 h. Well under the threshold — the live loop must integrate these, never drop a
        // ship mid-flight during ordinary warp.
        double widestHonestFrame = 20000.0 * 1.0 + TrafficSchedule.NpcTimeStep;
        Assert.False(TrafficSchedule.IsCatchUpStale(widestHonestFrame));
        Assert.False(TrafficSchedule.IsCatchUpStale(0));
        Assert.False(TrafficSchedule.IsCatchUpStale(TrafficSchedule.NpcMaxCatchUpSeconds)); // the boundary is inclusive-safe

        // Epoch leaps: the void a jump crosses / a far vault resumes at. These MUST be classed stale so the
        // mover is retired rather than integrated (the freeze the guard exists to prevent).
        Assert.True(TrafficSchedule.IsCatchUpStale(3655.0 * Day));       // Mars->Uranus decade jump
        Assert.True(TrafficSchedule.IsCatchUpStale(260_568_425.0));      // the owner's 8.3-year "the-tilt" save

        // The threshold sits far above honest warp yet far below any real void — no legitimate leg lands in
        // the gap between them (~128× the widest honest frame, ~40× below a single year).
        Assert.True(TrafficSchedule.NpcMaxCatchUpSeconds > 100.0 * widestHonestFrame);
        Assert.True(TrafficSchedule.NpcMaxCatchUpSeconds < 365.0 * Day);
    }

    [Fact]
    public void TrafficSchedule_MidFlightShips_AreActiveNearTimeZero()
    {
        var ephemeris = Sol();

        IReadOnlyList<NpcShip> ships = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);

        var midFlight = ships.Where(s => s.DepartureTime < 0).ToList();
        Assert.True(midFlight.Count >= 3, $"Expected several mid-flight ships, got {midFlight.Count}.");
        foreach (NpcShip ship in midFlight)
        {
            Assert.InRange(ship.ActivationTime, 0, 7300); // catch-up (dt=2h) lands just past t=0
            Assert.Equal(ship.ActivationTime, ship.InitialState.SimTime);
            // En route at t=0: genuinely out in open space, not parked on its origin body. (Since
            // the empty-sky fix also seeds the inner system, a mid-flight ship may now be inbound
            // OR outbound, so we no longer assume it has "fallen inside" its origin's orbit — only
            // that it has actually left the launch point.)
            double clearOfOrigin = (ship.InitialState.Position - ephemeris.Position(ship.OriginId, 0)).Length;
            Assert.True(clearOfOrigin > 1e9,
                $"{ship.Callsign} should have cleared {ship.OriginId} by t=0 (only {clearOfOrigin:E2} m away).");
        }

        var scheduled = ships.Where(s => s.DepartureTime >= 0).ToList();
        Assert.NotEmpty(scheduled);
        Assert.All(scheduled, s => Assert.InRange(s.DepartureTime, 3 * Day, 30 * Day));
    }

    [Fact]
    public void StarterPod_IsCatchableFromTheStandingStart()
    {
        var ephemeris = Sol();

        // The player's canonical start (Map.InitializeShipState): 5e9 m radially out from Earth,
        // co-moving with Earth's heliocentric orbit.
        Vector2d earth = ephemeris.Position("earth", 0);
        Vector2d earthVel = (ephemeris.Position("earth", 1) - ephemeris.Position("earth", -1)) / 2;
        var player = new ShipState(earth + earth.Normalized() * 5e9, earthVel, 0);

        NpcShip pod = TrafficSchedule.StarterPod(player);

        // A pod, worth exactly the first upgrade, activated and truthful at t=0.
        Assert.True(pod.IsPod);
        Assert.Equal(5, pod.CargoUnits);
        Assert.Equal(0, pod.InitialState.SimTime);

        // Relative speed is already inside the boarding limit — the only task is to close distance.
        double relSpeed = (player.Velocity - pod.InitialState.Velocity).Length;
        Assert.True(relSpeed < CaptureRule.MaxRelativeSpeed,
            $"Starter pod rel speed {relSpeed:F0} m/s must be under the {CaptureRule.MaxRelativeSpeed} m/s board limit.");

        // A real but short intercept: outside the instant window (so the player must plot a burn),
        // yet a small fraction of the 80–160 km/s fly-throughs' unreachable stand-off.
        double distance = (player.Position - pod.InitialState.Position).Length;
        Assert.InRange(distance, CaptureRule.CaptureRadiusMeters, 5e9);

        // And once the player has closed to point-blank at that same low rel speed, the window is open.
        var closed = new ShipState(pod.InitialState.Position, player.Velocity, 0);
        Assert.True(CaptureRule.IsInWindow(closed, pod.InitialState));
    }

    // The player's canonical start (Map.InitializeShipState): 5e9 m radially out from Earth,
    // co-moving with Earth's heliocentric orbit.
    private static ShipState StarterPlayer(CircularOrbitEphemeris ephemeris)
    {
        Vector2d earth = ephemeris.Position("earth", 0);
        Vector2d earthVel = (ephemeris.Position("earth", 1) - ephemeris.Position("earth", -1)) / 2;
        return new ShipState(earth + earth.Normalized() * 5e9, earthVel, 0);
    }

    [Fact]
    public void StarterFreighter_IsStubbornHe3PreyThatOnlyTheGunCanTake()
    {
        var ephemeris = Sol();
        ShipState player = StarterPlayer(ephemeris);

        NpcShip lark = TrafficSchedule.StarterFreighter(player);

        // A richer haul than the pod, and a crewed freighter (so warning shots / compliance apply).
        Assert.False(lark.IsPod);
        Assert.Equal("He3", lark.CargoClass);
        Assert.Equal(TrafficSchedule.StarterFreighterCargoUnits, lark.CargoUnits);
        Assert.Equal(0, lark.InitialState.SimTime);

        // She will NOT heave to — a warning shot only makes her call muscle, so the gun is the
        // only way to take her. If this ever regresses, the loop names an id that does roll stubborn.
        if (EncounterRule.ComplianceOf(lark, playerHeat: 0) != ComplianceState.Stubborn)
        {
            for (int i = 0; i < 2000; i++)
            {
                NpcShip probe = lark with { Id = $"freighter-lark-{i}" };
                if (EncounterRule.ComplianceOf(probe, 0) == ComplianceState.Stubborn)
                {
                    Assert.Fail($"'{TrafficSchedule.StarterFreighterId}' is not stubborn — use 'freighter-lark-{i}'.");
                }
            }
        }
        Assert.Equal(ComplianceState.Stubborn, EncounterRule.ComplianceOf(lark, 0));

        // Co-moving at t=0: matchable like the pod — closing distance is the only initial task.
        double rel0 = (player.Velocity - lark.InitialState.Velocity).Length;
        Assert.True(rel0 < CaptureRule.MaxRelativeSpeed,
            $"t=0 rel speed {rel0:F0} m/s must be under the {CaptureRule.MaxRelativeSpeed} m/s board limit.");

        // A real but short intercept away, well inside sensor reach — not a 160 km/s fly-through.
        double distance = (player.Position - lark.InitialState.Position).Length;
        Assert.InRange(distance, CaptureRule.CaptureRadiusMeters, 5e9);
    }

    [Fact]
    public void StarterFreighter_EscapeJink_BreaksTheWindow_ButHolingKeepsHerBoardable()
    {
        var ephemeris = Sol();
        ShipState player = StarterPlayer(ephemeris);
        NpcShip lark = TrafficSchedule.StarterFreighter(player);

        var sim = new Simulator(ephemeris, TrafficSchedule.NpcTimeStep);
        double past = lark.InitialState.SimTime
            + (TrafficSchedule.StarterFreighterEvadeDelayDays + 0.5) * Day;

        // Fly three ballistic-or-planned tracks from the shared start: the freighter with her escape
        // plan, the freighter with a HOLED sail (plan never steps — Map.razor steps a Disabled npc
        // with a null plan), and a player who matched her t=0 velocity and coasts.
        ShipState flown = lark.InitialState;
        ShipState holed = lark.InitialState;
        var matchedPlayer = new ShipState(player.Position, player.Velocity, player.SimTime);
        for (double t = lark.InitialState.SimTime; t < past; t += TrafficSchedule.NpcTimeStep)
        {
            flown = sim.Step(flown, lark.Plan);
            holed = sim.Step(holed, null);
            matchedPlayer = sim.Step(matchedPlayer, null);
        }

        // With her drive live she jinks off the matched course — the boarding window slams shut.
        double relFlown = (matchedPlayer.Velocity - flown.Velocity).Length;
        Assert.True(relFlown > CaptureRule.MaxRelativeSpeed,
            $"A live Lark should jink past the {CaptureRule.MaxRelativeSpeed} m/s window (rel {relFlown:F0} m/s).");

        // Hole her sail before the burn and she just drifts — still matchable, always recoverable.
        double relHoled = (matchedPlayer.Velocity - holed.Velocity).Length;
        Assert.True(relHoled < CaptureRule.MaxRelativeSpeed,
            $"A holed Lark must stay boardable (rel {relHoled:F0} m/s under the {CaptureRule.MaxRelativeSpeed} m/s limit).");
    }

    [Fact]
    public void TrafficSchedule_ShortRouteScenario_StillSpawnsShipsEnRouteAtTimeZero()
    {
        // The world does not wait for the player: even a scenario whose every route is a short
        // inner-system hop (transfer ≪ the 20–70 day mid-flight lead) must have ships already
        // flying at t=0. The unclamped lead used to put "mid-flight" departures in the FUTURE.
        var ephemeris = Sol();
        var traffic = new SpaceSails.Contracts.TrafficDefinition
        {
            Routes =
            [
                new SpaceSails.Contracts.RouteDefinition { From = "earth", To = "mars", Cargo = "Ice" },
                new SpaceSails.Contracts.RouteDefinition { From = "mars", To = "earth", Cargo = "Machinery" },
            ],
        };

        IReadOnlyList<NpcShip> ships = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8, traffic);

        var midFlight = ships.Where(s => s.DepartureTime < 0).ToList();
        Assert.True(midFlight.Count >= 4, $"Expected the mid-flight 60% to be genuinely en route, got {midFlight.Count}.");
        foreach (NpcShip ship in midFlight)
        {
            Assert.InRange(ship.ActivationTime, 0, 7300); // catch-up (dt=2h) lands just past t=0
        }
    }

    [Fact]
    public void SolEu_AtGenesis_ShowsLitPreyWithinBeaconRange_OfThePlayerStart()
    {
        // The empty-sky regression (owner, 2026-07-06 screenshot "ZERO ships"): opening sol-eu near
        // Earth must reveal mobile prey immediately, not depots-only for days. At least one lit,
        // timetable-publishing pod AND at least one lit ship-or-pod must be active within the first
        // catch-up step and inside the 3 AU civilian-beacon range of the player's start. Before the
        // fix, every pod launched 0.5–10 days out and every mid-flight hauler spawned past 3 AU.
        var scenario = ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "sol-eu.json"));
        var ephemeris = CircularOrbitEphemeris.FromScenario(scenario);

        Vector2d earth = ephemeris.Position("earth", 0);
        Vector2d playerStart = earth + earth.Normalized() * 5e9; // mirrors Map.InitializeShipState

        // The genesis call the client makes (Map.razor): pods first, then haulers.
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(ephemeris, seed: 43, count: 3);
        IReadOnlyList<NpcShip> traffic = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);

        bool LitInRangeAtGenesis(NpcShip s) =>
            s.PublishesTimetable
            && s.ActivationTime <= 7300 // active within the first NPC catch-up step, i.e. essentially now
            && (s.InitialState.Position - playerStart).Length <= TransponderRule.CivilianBeaconRangeMeters;

        Assert.Contains(pods, LitInRangeAtGenesis);                    // the Luna milk-run pod is right there
        Assert.Contains(pods.Concat(traffic), LitInRangeAtGenesis);   // and a lit contact on the board

        // And that first pod is the named "Luna pod" the tutorial sends the player after.
        NpcShip lunaPrey = pods.First(LitInRangeAtGenesis);
        Assert.Equal("luna", lunaPrey.OriginId);
        Assert.True(lunaPrey.IsPod);
    }

    [Fact]
    public void TrafficSchedule_GenerateWave_IsLiveRelativeToNow()
    {
        // The world keeps living: a refill wave planned at day 200 must be mid-flight or
        // scheduled relative to day 200, with wave-namespaced ids that can't collide.
        var ephemeris = Sol();
        double now = 200 * Day;

        IReadOnlyList<NpcShip> wave = TrafficSchedule.GenerateWave(ephemeris, seed: 7, count: 8, now, waveNumber: 3);

        Assert.All(wave, s => Assert.StartsWith("npc-w3-", s.Id));
        var midFlight = wave.Where(s => s.DepartureTime < now).ToList();
        Assert.True(midFlight.Count >= 3, $"expected mid-flight ships in the wave, got {midFlight.Count}");
        foreach (NpcShip ship in midFlight)
        {
            Assert.InRange(ship.ActivationTime, now, now + 7300); // catch-up lands just past NOW
        }

        var scheduled = wave.Where(s => s.DepartureTime >= now).ToList();
        Assert.All(scheduled, s => Assert.InRange(s.DepartureTime, now + 3 * Day, now + 30 * Day));

        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePodsWave(ephemeris, seed: 8, count: 2, now, waveNumber: 3);
        Assert.All(pods, p => Assert.StartsWith("pod-w3-", p.Id));
        // The milk run lives too: at least one pod is already coasting as of NOW, the rest are
        // freshly scheduled firings.
        var midPods = pods.Where(p => p.DepartureTime < now).ToList();
        Assert.NotEmpty(midPods);
        Assert.All(midPods, p => Assert.InRange(p.ActivationTime, now, now + 7300));
        var scheduledPods = pods.Where(p => p.DepartureTime >= now).ToList();
        Assert.All(scheduledPods, p => Assert.InRange(p.DepartureTime, now, now + 10 * Day));
    }

    [Fact]
    public void RoutePlanner_InnerSystemRoute_ReachesDestination()
    {
        var ephemeris = Sol();
        var rng = new DeterministicRandom(7);

        NpcRoute route = RoutePlanner.PlanRoute(ephemeris, "mars", "earth", 0, RoutePersonality.Economical, rng);

        Assert.True(route.EstimatedMissDistance < 5 * RoutePlanner.ArrivalToleranceMeters,
            $"Mars->Earth economical route missed by {route.EstimatedMissDistance:E2} m.");
        Assert.True(route.EstimatedArrivalTime > 0);
    }

    [Fact]
    public void SensorModel_RespectsRange_AndSunGlare()
    {
        var sensor = new SensorModel(rangeMeters: 5e10, glareHalfAngleRad: 20 * Math.PI / 180, glareRangeFactor: 0.25);
        Vector2d observer = new(1e11, 0); // sun is at the origin, so glare looks in -X

        var sunwardTarget = new ShipState(new Vector2d(7e10, 0), Vector2d.Zero, 0);   // 3e10 away, dead in the glare
        var antiSunTarget = new ShipState(new Vector2d(1.3e11, 0), Vector2d.Zero, 0); // 3e10 away, sun at its back
        var distantTarget = new ShipState(new Vector2d(1e11, 6e10), Vector2d.Zero, 0); // 6e10 away, off-glare

        Assert.False(sensor.TryObserve(observer, "a", sunwardTarget, 0, out _)); // glare cuts range to 1.25e10
        Assert.True(sensor.TryObserve(observer, "b", antiSunTarget, 0, out Observation obs));
        Assert.False(sensor.TryObserve(observer, "c", distantTarget, 0, out _)); // beyond plain range

        Assert.Equal("b", obs.TargetId);
        Assert.Equal(antiSunTarget.Position, obs.Position);
    }

    [Fact]
    public void PathPredictor_PinnedCorrectHypothesis_ConvergesToActualTrack()
    {
        var ephemeris = Sol();
        var truthSim = new Simulator(ephemeris, timeStepSeconds: 1.0);

        // A target on Earth's orbit that will brake hard at day 10.
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var target = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);
        var actualPlan = new ManeuverPlan([new ManeuverNode(10 * Day, ManeuverAction.Decelerate, 5)]);

        // Perfect observation at t=0, then predict 25 days out with and without the hypothesis.
        var observation = new Observation("t", 0, target.Position, target.Velocity);
        PredictedPath pinned = PathPredictor.Predict(ephemeris, observation, actualPlan, 25 * Day);
        PredictedPath ballistic = PathPredictor.Predict(ephemeris, observation, null, 25 * Day);

        ShipState actual = truthSim.Run(target, 25 * Day, actualPlan);
        double pinnedError = (pinned.Samples[^1].Position - actual.Position).Length;
        double ballisticError = (ballistic.Samples[^1].Position - actual.Position).Length;

        Assert.True(pinnedError < ballisticError / 20,
            $"Pinned {pinnedError:E2} m vs ballistic {ballisticError:E2} m — hypothesis should dominate.");
        Assert.True(pinnedError < 1e8, $"Pinned prediction off by {pinnedError:E2} m.");
    }

    [Fact]
    public void PredictedPath_UncertaintyGrows_WithTimeSinceObservation()
    {
        var observation = new Observation("t", 1000, Vector2d.Zero, Vector2d.Zero);
        var path = new PredictedPath(observation, [new TrajectorySample(1000, Vector2d.Zero)]);

        double atObservation = path.HalfWidthAt(1000);
        double afterHour = path.HalfWidthAt(1000 + 3600);
        double afterDay = path.HalfWidthAt(1000 + Day);

        Assert.Equal(PredictedPath.BaseHalfWidthMeters, atObservation);
        Assert.True(atObservation < afterHour && afterHour < afterDay);
        Assert.True(afterDay > 1e9, "A day of silence should open the cone past the capture threshold.");
    }

    [Fact]
    public void BrakeAtHypothesis_PlacesBrake_NearClosestApproach()
    {
        var ephemeris = Sol();
        var rng = new DeterministicRandom(7);
        NpcRoute route = RoutePlanner.PlanRoute(ephemeris, "mars", "earth", 0, RoutePersonality.Economical, rng);

        // Observe the ship shortly after its departure burn, then ask for the standard hunch.
        var sim = new Simulator(ephemeris, timeStepSeconds: 1.0);
        ShipState observed = sim.Run(route.DepartureState, 2 * 3600, route.Plan);
        var observation = new Observation("t", observed.SimTime, observed.Position, observed.Velocity);

        ManeuverPlan hypothesis = PathPredictor.BrakeAtHypothesis(
            ephemeris, observation, "earth", horizonSeconds: route.EstimatedArrivalTime * 1.2);

        ManeuverNode brake = Assert.Single(hypothesis.Nodes);
        Assert.Equal(ManeuverAction.Decelerate, brake.Action);
        // The hunch should land in the same era as the route's own arrival estimate.
        Assert.InRange(brake.SimTime, route.EstimatedArrivalTime * 0.5, route.EstimatedArrivalTime * 1.5);
    }

    [Fact]
    public void SaturnSail_FitsInOneNavDeskSitDown()
    {
        // The owner's route-plan-length metric: a single plotted plan must carry the ship all
        // the way to Saturn. Reference sail found by offline probe: accelerate 12 pulses at
        // day 82; it passes inside Saturn's 1e10 m port zone around day 278 — comfortably
        // within the client's 730-day plotting horizon, using the client's own coarse ribbon
        // settings (maxTimeStep 3 h).
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0);

        Vector2d earth0 = ephemeris.Position("earth", 0);
        Vector2d v0 = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        var start = new ShipState(earth0 + earth0.Normalized() * 5e9, v0, 0);
        var plan = new ManeuverPlan([new ManeuverNode(82 * Day, ManeuverAction.Accelerate, 12)]);

        IReadOnlyList<TrajectorySample> sail = simulator.ProjectAdaptive(
            start, plan, 730 * Day, maxTimeStep: 3 * 3600, maxSamples: 8000);

        double miss = double.MaxValue, when = 0;
        foreach (TrajectorySample sample in sail)
        {
            double d = (ephemeris.Position("saturn", sample.SimTime) - sample.Position).Length;
            if (d < miss) { (miss, when) = (d, sample.SimTime); }
        }

        Assert.True(miss < 1.5e10, $"The Saturn sail must reach the port zone; missed by {miss:E2} m.");
        Assert.InRange(when / Day, 200, 400);
        Assert.Equal(730 * Day, sail[^1].SimTime); // the horizon truly covers the voyage
    }

    [Fact]
    public void ClosestApproach_FlagsAVenusThreading()
    {
        // A ship placed on a line that passes 1000 km over Venus's cloud tops: the planner
        // must call Venus the most severe pass, and a path through the planet must say Impact.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var samples = new List<TrajectorySample>();
        for (int i = 0; i <= 100; i++)
        {
            double t = i * 1000.0;
            // Co-moving flyby: constant 1000 km of clearance sideways, sweeping past along-track.
            Vector2d offset = new(6.0518e6 + 1e6, (t - 50000) * 30);
            samples.Add(new TrajectorySample(t, ephemeris.Position("venus", t) + offset));
        }

        ClosestApproach.Pass? pass = ClosestApproach.MostSevere(samples, ephemeris);

        Assert.NotNull(pass);
        Assert.Equal("venus", pass.Value.BodyId);
        Assert.False(pass.Value.Impact);
        Assert.True(pass.Value.Severity is > 1.0 and < 2.0, $"severity {pass.Value.Severity}");

        // Now thread the planet itself.
        for (int i = 0; i < samples.Count; i++)
        {
            double t = i * 1000.0;
            samples[i] = new TrajectorySample(t, ephemeris.Position("venus", t) + new Vector2d(0, (t - 50000) * 30));
        }

        ClosestApproach.Pass? impact = ClosestApproach.MostSevere(samples, ephemeris);
        Assert.True(impact!.Value.Impact, "a path through Venus must be flagged as an impact");
    }

    [Fact]
    public void OrbitalDepots_OnePerPlanet_RideRailsBoardably()
    {
        // M22: something to steal on every planet orbit. Depots are rails entities — at any
        // time they sit exactly on their circular orbit, moving under the boarding speed limit
        // relative to their planet. PR-3: also one per named station and pirate haven — the
        // outer reaches get their own bus stops too.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(ephemeris, seed: 44);

        // One depot per qualifying body, computed the same way GenerateDepots decides: planets, named
        // stations and havens — but NOT mass-less sun-orbiting fixtures like the derelict roadster (which
        // is both sun-orbiting and kind==station, so it must not be counted twice, nor at all).
        int expected = ephemeris.Bodies.Count(b =>
            !(b.ParentId == "sun" && b.Mu == 0)
            && (b.ParentId == "sun" || b.Kind == BodyKind.Station || b.IsHaven));
        Assert.Equal(expected, depots.Count);

        foreach (NpcShip depot in depots)
        {
            foreach (double t in new[] { 0.0, 86400.0, 3.7e6 })
            {
                ShipState state = TrafficSchedule.DepotState(
                    depot.Id, depot.DepotBodyId!, depot.DepotOrbitRadius, depot.DepotPhase, ephemeris, t);
                Vector2d planetPos = ephemeris.Position(depot.DepotBodyId!, t);
                double d = (state.Position - planetPos).Length;
                Assert.InRange(d / depot.DepotOrbitRadius, 0.999, 1.001);

                Vector2d planetVel = (ephemeris.Position(depot.DepotBodyId!, t + 1) - ephemeris.Position(depot.DepotBodyId!, t - 1)) / 2.0;
                double relSpeed = (state.Velocity - planetVel).Length;
                Assert.True(relSpeed < CaptureRule.MaxRelativeSpeed, $"{depot.Callsign}: {relSpeed} m/s");
            }
        }
    }
}
