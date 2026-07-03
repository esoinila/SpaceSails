namespace SpaceSails.Core.Tests;

public class PiracyTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    [Fact]
    public void CaptureWindow_RequiresBothDistanceAndRelativeSpeed()
    {
        var player = new ShipState(new Vector2d(0, 0), new Vector2d(30000, 0), 0);

        var close = new ShipState(new Vector2d(5e7, 0), new Vector2d(30000 + 1500, 0), 0);
        var closeButFast = new ShipState(new Vector2d(5e7, 0), new Vector2d(30000 + 5000, 0), 0);
        var slowButFar = new ShipState(new Vector2d(5e8, 0), new Vector2d(30000, 0), 0);

        Assert.True(CaptureRule.IsInWindow(player, close));
        Assert.False(CaptureRule.IsInWindow(player, closeButFast));
        Assert.False(CaptureRule.IsInWindow(player, slowButFar));
    }

    [Fact]
    public void CargoMarket_He3_IsThePrize()
    {
        Assert.True(CargoMarket.UnitValue("He3") > 2 * CargoMarket.UnitValue("Compute cores"));
        Assert.True(CargoMarket.UnitValue("Compute cores") > CargoMarket.UnitValue("Ice"));
        Assert.Equal(50, CargoMarket.UnitValue("SomethingUnheardOf"));
    }

    [Fact]
    public void Pods_AreBallistic_AndDeterministic()
    {
        var ephemeris = Sol();

        IReadOnlyList<NpcShip> first = TrafficSchedule.GeneratePods(ephemeris, seed: 7, count: 3);
        IReadOnlyList<NpcShip> second = TrafficSchedule.GeneratePods(ephemeris, seed: 7, count: 3);

        Assert.Equal(3, first.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].InitialState, second[i].InitialState); // bit-identical

            NpcShip pod = first[i];
            Assert.True(pod.IsPod);
            Assert.Equal(0, pod.ManeuverBudget);
            Assert.Empty(pod.Plan.Nodes);           // mass driver: all delta-v at launch
            Assert.Equal("luna", pod.OriginId);
            Assert.Equal("Compute cores", pod.CargoClass);
            // 5 × 400 cr = exactly the first upgrade price: one pod finishes the tutorial.
            Assert.Equal(5, pod.CargoUnits);
            Assert.True(pod.ActivationTime > pod.DepartureTime); // launched, then coasting
        }
    }

    [Fact]
    public void Pod_LaunchState_IsEscapingEarthToward_ItsDestination()
    {
        var ephemeris = Sol();
        NpcShip pod = TrafficSchedule.GeneratePods(ephemeris, seed: 7, count: 1)[0];

        // The launch state must carry more than Earth's orbital velocity alone — the mass
        // driver's burn is folded in (outward transfers accelerate prograde).
        const double h = 1.0;
        Vector2d earthVelocity = (ephemeris.Position("earth", pod.ActivationTime + h)
                                - ephemeris.Position("earth", pod.ActivationTime - h)) / (2 * h);
        double relativeSpeed = (pod.InitialState.Velocity - earthVelocity).Length;
        Assert.True(relativeSpeed > 1000, $"Pod launch is only {relativeSpeed:F0} m/s relative to Earth.");
    }

    [Fact]
    public void PodPredictionCone_NeverOpens_PastMeasurementNoise()
    {
        var observation = new Observation("pod", 0, new Vector2d(1.5e11, 0), new Vector2d(0, 32000));
        var path = new PredictedPath(observation, [new TrajectorySample(0, new Vector2d(1.5e11, 0))], ManeuverBudget: 0);

        const double Day = 86400;
        double afterTenDays = path.HalfWidthAt(10 * Day);

        // Only base noise + velocity sigma: no quadratic maneuver term.
        Assert.Equal(PredictedPath.BaseHalfWidthMeters + PredictedPath.VelocitySigma * 10 * Day, afterTenDays);
        Assert.True(afterTenDays < 1e9, "A ballistic pod must stay predictable for weeks.");
    }

    [Fact]
    public void Luna_OrbitsEarth_InTheScenario()
    {
        var ephemeris = Sol();
        Vector2d luna = ephemeris.Position("luna", 0);
        Vector2d earth = ephemeris.Position("earth", 0);

        Assert.Equal(3.844e8, (luna - earth).Length, tolerance: 1e3);
    }
}
