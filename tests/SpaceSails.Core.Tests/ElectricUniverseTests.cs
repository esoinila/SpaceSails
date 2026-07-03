using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

public class ElectricUniverseTests
{
    private const double Day = 86400;

    private static ScenarioDefinition LoadSolEu() =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "sol-eu.json"));

    private static (CircularOrbitEphemeris Ephemeris, PlasmaEnvironment Plasma) SolEu()
    {
        ScenarioDefinition scenario = LoadSolEu();
        var ephemeris = CircularOrbitEphemeris.FromScenario(scenario);
        PlasmaEnvironment? plasma = PlasmaEnvironment.FromScenario(scenario, ephemeris);
        Assert.NotNull(plasma);
        return (ephemeris, plasma);
    }

    [Fact]
    public void NewtonianScenario_YieldsNoEnvironment_AndChargeStaysZero()
    {
        ScenarioDefinition sol = SimulatorTests.LoadSol();
        var ephemeris = CircularOrbitEphemeris.FromScenario(sol);

        Assert.Null(PlasmaEnvironment.FromScenario(sol, ephemeris));

        // Chargeless simulator: state after M7 must be bit-identical to before (Charge = 0).
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        Vector2d earth0 = ephemeris.Position("earth", 0);
        var state = simulator.Run(new ShipState(earth0 + new Vector2d(5e9, 0), new Vector2d(0, 30000), 0), 10 * Day);
        Assert.Equal(0, state.Charge);
    }

    [Fact]
    public void Charge_Equilibrates_TowardSolarHalo()
    {
        (CircularOrbitEphemeris ephemeris, PlasmaEnvironment plasma) = SolEu();
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60, plasma);

        // Circular orbit at Mercury's radius: ambient (5e10/5.791e10)^2 ≈ 0.745.
        double r = 5.791e10;
        double mu = 1.32712440018e20;
        var state = new ShipState(new Vector2d(r, 0), new Vector2d(0, Math.Sqrt(mu / r)), 0);

        state = simulator.Run(state, 12 * 3600); // 12 τ

        double expected = plasma.AmbientCharge(state.Position, state.SimTime);
        Assert.InRange(state.Charge, expected - 0.05, expected + 0.05);
        Assert.True(state.Charge > 0.6, $"Hull near Mercury should run hot, got {state.Charge:F2}.");
    }

    [Fact]
    public void Streams_Saturate_AndAccelerate_AlongTheRibbon()
    {
        (CircularOrbitEphemeris ephemeris, PlasmaEnvironment plasma) = SolEu();

        // Midpoint of the Saturn→Jupiter ribbon at t=0.
        Vector2d saturn = ephemeris.Position("saturn", 0);
        Vector2d jupiter = ephemeris.Position("jupiter", 0);
        Vector2d mid = (saturn + jupiter) * 0.5;

        Assert.Equal(1.0, plasma.AmbientCharge(mid, 0));

        Vector2d acceleration = plasma.Acceleration(mid, charge: 1.0, simTime: 0);
        Assert.Equal(PlasmaEnvironment.StreamAcceleration, acceleration.Length, precision: 6);

        Vector2d along = (jupiter - saturn).Normalized();
        double dot = acceleration.X * along.X + acceleration.Y * along.Y;
        Assert.True(dot > 0.999 * PlasmaEnvironment.StreamAcceleration, "Force must point along the ribbon.");

        // Uncharged ships feel nothing, far positions feel nothing.
        Assert.Equal(Vector2d.Zero, plasma.Acceleration(mid, 0, 0));
        Assert.Equal(Vector2d.Zero, plasma.Acceleration(new Vector2d(-2e12, -2e12), 1.0, 0));
    }

    [Fact]
    public void RidingTheStream_BeatsBallistic_SaturnToJupiter()
    {
        (CircularOrbitEphemeris ephemeris, PlasmaEnvironment plasma) = SolEu();
        var charged = new Simulator(ephemeris, timeStepSeconds: 3600, plasma);
        var ballistic = new Simulator(ephemeris, timeStepSeconds: 3600);

        // Depart Saturn co-moving, nudged toward Jupiter along the ribbon.
        Vector2d saturn0 = ephemeris.Position("saturn", 0);
        const double h = 1.0;
        Vector2d saturnVelocity = (ephemeris.Position("saturn", h) - ephemeris.Position("saturn", -h)) / (2 * h);
        var start = new ShipState(saturn0 + (ephemeris.Position("jupiter", 0) - saturn0).Normalized() * 4e10, saturnVelocity, 0);

        // Closest approach over the voyage: a rider gains hundreds of km/s down the ribbon and
        // overshoots (venting at the midpoint to coast is the intended skill), so endpoint
        // distance is meaningless — what matters is that the stream *reaches* Jupiter's
        // neighborhood months before a ballistic coast does.
        (double Miss, double When) ClosestApproach(Simulator sim, double days)
        {
            ShipState s = start;
            double miss = double.MaxValue, when = 0;
            while (s.SimTime < days * Day)
            {
                s = sim.Step(s);
                double d = (ephemeris.Position("jupiter", s.SimTime) - s.Position).Length;
                if (d < miss)
                {
                    (miss, when) = (d, s.SimTime);
                }
            }
            return (miss, when);
        }

        (double streamedMiss, double streamedWhen) = ClosestApproach(charged, 200);
        (double ballisticMiss, _) = ClosestApproach(ballistic, 200);
        Assert.True(streamedMiss < 1e11,
            $"A stream ride should pass close to Jupiter; got {streamedMiss:E2} m.");
        Assert.True(streamedMiss < ballisticMiss * 0.5,
            $"Stream {streamedMiss:E2} m vs ballistic {ballisticMiss:E2} m — the river should dominate.");
        Assert.True(streamedWhen < 150 * Day,
            $"The ride should arrive within ~5 months; closest at {streamedWhen / Day:F0} d.");
    }

    [Fact]
    public void ChargedHull_Glows_ThroughGlare_AndBeyondPassiveRange()
    {
        var sensor = new SensorModel(rangeMeters: 5e10, glareHalfAngleRad: 20 * Math.PI / 180, glareRangeFactor: 0.25);
        Vector2d observer = new(1e11, 0); // sun at origin: glare looks toward -X

        // Sunward target inside glare at 3e10: invisible cold (glare cuts range to 1.25e10)…
        var coldSunward = new ShipState(new Vector2d(7e10, 0), Vector2d.Zero, 0, Charge: 0);
        Assert.False(sensor.TryObserve(observer, "a", coldSunward, 0, out _));

        // …but a fully charged hull glows through the glare (range 1.25e10 + 2×5e10).
        var hotSunward = coldSunward with { Charge = 1.0 };
        Assert.True(sensor.TryObserve(observer, "b", hotSunward, 0, out _));

        // Off-glare, beyond passive range at 9e10: cold invisible, hot visible.
        var coldFar = new ShipState(new Vector2d(1e11, 9e10), Vector2d.Zero, 0, Charge: 0);
        var hotFar = coldFar with { Charge = 1.0 };
        Assert.False(sensor.TryObserve(observer, "c", coldFar, 0, out _));
        Assert.True(sensor.TryObserve(observer, "d", hotFar, 0, out _));
    }

    [Fact]
    public void EuReplay_IsBitwiseDeterministic()
    {
        ShipState RunFresh()
        {
            (CircularOrbitEphemeris ephemeris, PlasmaEnvironment plasma) = SolEu();
            var simulator = new Simulator(ephemeris, timeStepSeconds: 60, plasma);
            var plan = new ManeuverPlan([new ManeuverNode(SimTime: 3600, ManeuverAction.Accelerate, Pulses: 3)]);
            Vector2d saturn0 = ephemeris.Position("saturn", 0);
            var state = new ShipState(saturn0 + new Vector2d(4e10, 0), new Vector2d(0, 9000), 0);
            return simulator.Run(state, 30 * Day, plan);
        }

        Assert.Equal(RunFresh(), RunFresh()); // includes Charge — bit-identical
    }
}
