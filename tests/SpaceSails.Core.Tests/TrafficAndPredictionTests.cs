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
            // In transit: already fallen inside its origin's orbit.
            double originRadius = ephemeris.Bodies.First(b => b.Id == ship.OriginId).OrbitRadius;
            Assert.True(ship.InitialState.Position.Length < originRadius,
                $"{ship.Callsign} should have left {ship.OriginId}'s orbit by t=0.");
        }

        var scheduled = ships.Where(s => s.DepartureTime >= 0).ToList();
        Assert.NotEmpty(scheduled);
        Assert.All(scheduled, s => Assert.InRange(s.DepartureTime, 3 * Day, 30 * Day));
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
        // relative to their planet.
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(ephemeris, seed: 44);

        int planets = ephemeris.Bodies.Count(b => b.ParentId == "sun");
        Assert.Equal(planets, depots.Count);

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
