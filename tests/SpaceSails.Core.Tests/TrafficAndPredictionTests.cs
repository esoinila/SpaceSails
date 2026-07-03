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
}
