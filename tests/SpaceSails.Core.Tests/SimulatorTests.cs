using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

public class SimulatorTests
{
    private const double SunMu = 1.32712440018e20;
    private const double EarthOrbitRadius = 1.496e11;
    private const double Day = 86400;

    private static CircularOrbitEphemeris SunOnly() =>
        new([new CelestialBody("sun", "Sun", null, SunMu, 6.9634e8, 0, 0, 0)]);

    private sealed class EmptySpace : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } = [];

        public Vector2d Position(string bodyId, double simTime) => Vector2d.Zero;
    }

    [Fact]
    public void CircularOrbit_StaysCircular_ForThirtyDays()
    {
        var simulator = new Simulator(SunOnly(), timeStepSeconds: 60);
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);

        state = simulator.Run(state, 30 * Day);

        double relativeDrift = Math.Abs(state.Position.Length - EarthOrbitRadius) / EarthOrbitRadius;
        Assert.True(relativeDrift < 0.005, $"Orbit radius drifted {relativeDrift:P3} after 30 days.");
    }

    [Fact]
    public void ShipAtRest_FallsSunward()
    {
        var simulator = new Simulator(SunOnly(), timeStepSeconds: 60);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), Vector2d.Zero, 0);

        state = simulator.Run(state, 10 * Day);

        Assert.True(state.Position.Length < EarthOrbitRadius, "A ship at rest must fall toward the Sun.");
        Assert.True(state.Velocity.X < 0, "Fall velocity must point sunward.");
    }

    [Fact]
    public void ManeuverNodes_ScaleVelocity_AtScheduledTime()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var plan = new ManeuverPlan([new ManeuverNode(SimTime: 120, ManeuverAction.Accelerate, Pulses: 2)]);
        var state = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);

        // The node at t=120 fires at the start of the step covering [120, 180) — step 3.
        state = simulator.Step(state, plan);
        state = simulator.Step(state, plan);
        Assert.Equal(1000, state.Velocity.X, precision: 9);

        state = simulator.Step(state, plan);
        Assert.Equal(1000 * 1.1 * 1.1, state.Velocity.X, precision: 9);

        state = simulator.Step(state, plan);
        Assert.Equal(1000 * 1.1 * 1.1, state.Velocity.X, precision: 9);
    }

    [Fact]
    public void DeceleratePulse_ReducesSpeed_TenPercent()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var plan = new ManeuverPlan([new ManeuverNode(SimTime: 0, ManeuverAction.Decelerate)]);
        var state = new ShipState(Vector2d.Zero, new Vector2d(600, 800), 0);

        state = simulator.Step(state, plan);

        Assert.Equal(1000 * 0.9, state.Velocity.Length, precision: 9);
    }

    [Fact]
    public void Project_MatchesStepByStepIntegration()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        var plan = new ManeuverPlan([new ManeuverNode(SimTime: 1800, ManeuverAction.Accelerate, Pulses: 3)]);
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var initial = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);

        IReadOnlyList<Vector2d> projected = simulator.Project(initial, plan, horizonSeconds: 6000);

        ShipState state = initial;
        Assert.Equal(state.Position, projected[0]);
        for (int i = 1; i < projected.Count; i++)
        {
            state = simulator.Step(state, plan);
            Assert.Equal(state.Position, projected[i]);
        }
    }

    [Fact]
    public void Replay_IsBitwiseDeterministic()
    {
        ShipState RunFresh()
        {
            var simulator = new Simulator(CircularOrbitEphemeris.FromScenario(LoadSol()), timeStepSeconds: 60);
            var plan = new ManeuverPlan([
                new ManeuverNode(SimTime: 3600, ManeuverAction.Accelerate, Pulses: 2),
                new ManeuverNode(SimTime: 5 * Day, ManeuverAction.Decelerate),
            ]);
            double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
            var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);
            return simulator.Run(state, 10 * Day, plan);
        }

        ShipState first = RunFresh();
        ShipState second = RunFresh();

        Assert.Equal(first, second); // bit-identical, not approximately equal
    }

    [Fact]
    public void ProjectAdaptive_IsDeterministic()
    {
        var simulator = new Simulator(CircularOrbitEphemeris.FromScenario(LoadSol()), timeStepSeconds: 1);
        var plan = new ManeuverPlan([new ManeuverNode(SimTime: 2 * Day, ManeuverAction.Accelerate, Pulses: 3)]);
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);

        IReadOnlyList<TrajectorySample> first = simulator.ProjectAdaptive(state, plan, 30 * Day);
        IReadOnlyList<TrajectorySample> second = simulator.ProjectAdaptive(state, plan, 30 * Day);

        Assert.True(first.SequenceEqual(second), "Adaptive projection must be bit-identical across runs.");
    }

    [Fact]
    public void ProjectAdaptive_LandsExactlyOnNodeTimes()
    {
        var simulator = new Simulator(SunOnly(), timeStepSeconds: 1);
        // Node times deliberately off any regular step grid.
        var plan = new ManeuverPlan([
            new ManeuverNode(SimTime: 98765, ManeuverAction.Accelerate),
            new ManeuverNode(SimTime: 3.3 * Day, ManeuverAction.Decelerate, Pulses: 2),
        ]);
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);

        IReadOnlyList<TrajectorySample> samples = simulator.ProjectAdaptive(state, plan, 10 * Day);

        Assert.Contains(samples, s => s.SimTime == 98765);
        Assert.Contains(samples, s => s.SimTime == 3.3 * Day);
    }

    [Fact]
    public void ProjectAdaptive_MatchesFixedStepTruth_ThroughAMidcourseBurn()
    {
        var ephemeris = SunOnly();
        var simulator = new Simulator(ephemeris, timeStepSeconds: 1);
        var plan = new ManeuverPlan([new ManeuverNode(SimTime: 5 * Day, ManeuverAction.Decelerate, Pulses: 2)]);
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);

        TrajectorySample projectedEnd = simulator.ProjectAdaptive(state, plan, 20 * Day)[^1];
        ShipState truthEnd = simulator.Run(state, 20 * Day, plan);

        Assert.Equal(20 * Day, projectedEnd.SimTime);
        double error = (projectedEnd.Position - truthEnd.Position).Length;
        // The planning line is a guide, not the sim: it must stay well inside the provisional
        // capture threshold (1e9 m) over a plotted transfer, not be bit-identical.
        Assert.True(error < 5e7, $"Adaptive projection diverged {error:E2} m from dt=1 truth after 20 days.");
    }

    [Fact]
    public void ProjectAdaptive_RefinesNearMass_CoarsensInDeepSpace()
    {
        var simulator = new Simulator(SunOnly(), timeStepSeconds: 1);
        double nearSunRadius = 1e10;
        var nearState = new ShipState(
            new Vector2d(nearSunRadius, 0), new Vector2d(0, Math.Sqrt(SunMu / nearSunRadius)), 0);
        var farState = new ShipState(
            new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, Math.Sqrt(SunMu / EarthOrbitRadius)), 0);

        IReadOnlyList<TrajectorySample> near = simulator.ProjectAdaptive(nearState, null, Day);
        IReadOnlyList<TrajectorySample> far = simulator.ProjectAdaptive(farState, null, Day);

        double nearStep = near[1].SimTime - near[0].SimTime;
        double farStep = far[1].SimTime - far[0].SimTime;
        Assert.True(nearStep < farStep, $"Near-mass step {nearStep}s should be finer than deep-space step {farStep}s.");
        Assert.Equal(3600, farStep); // deep space clamps to the coarse ceiling
    }

    [Fact]
    public void ProjectAdaptive_RespectsSampleCap()
    {
        var simulator = new Simulator(SunOnly(), timeStepSeconds: 1);
        double circularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circularSpeed), 0);

        IReadOnlyList<TrajectorySample> samples = simulator.ProjectAdaptive(state, null, 365 * Day, maxSamples: 16);

        Assert.True(samples.Count <= 16, $"Sample cap exceeded: {samples.Count}.");
    }


    [Fact]
    public void FractionalPercentNodes_ScaleExactly()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var plan = new ManeuverPlan([new ManeuverNode(SimTime: 0, ManeuverAction.Accelerate, Pulses: 2, Percent: 3.5)]);
        var state = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);

        state = simulator.Step(state, plan);

        Assert.Equal(1000 * 1.035 * 1.035, state.Velocity.X, precision: 9);
    }

    [Fact]
    public void FineManeuverNodes_ScaleByOnePercent()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var plan = new ManeuverPlan([new ManeuverNode(SimTime: 0, ManeuverAction.Accelerate, Pulses: 3, Fine: true)]);
        var state = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);

        state = simulator.Step(state, plan);

        Assert.Equal(1000 * 1.01 * 1.01 * 1.01, state.Velocity.X, precision: 9);
    }

    internal static ScenarioDefinition LoadSol() =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json"));
}
