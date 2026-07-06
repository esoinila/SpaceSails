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
    public void OrbitInsertion_BindsAndHolds()
    {
        // Arrive 1e8 m over Earth at 1.2 km/s relative: the window is open, the burn costs a
        // sane number of pulses, and after insertion the integrator holds the orbit — Earth
        // distance stays within a few percent for a full orbital period.
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0);
        CelestialBody earth = ephemeris.Bodies.First(b => b.Id == "earth");
        CelestialBody sun = ephemeris.Bodies.First(b => b.Id == "sun");
        double hill = OrbitRule.HillRadius(earth, sun.Mu);
        Assert.InRange(hill, 1.3e9, 1.7e9); // the textbook 1.5 M km

        Vector2d earthPos = ephemeris.Position("earth", 0);
        Vector2d earthVel = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        var arrival = new ShipState(earthPos + new Vector2d(1e8, 0), earthVel + new Vector2d(0, 1200), 0);

        Assert.True(OrbitRule.WindowOpen(arrival, earthPos, earthVel, earth, hill));
        int cost = OrbitRule.PulseCost(arrival, earthPos, earthVel, earth);
        Assert.InRange(cost, 1, 12);

        ShipState orbiting = OrbitRule.Insert(arrival, earthPos, earthVel, earth);
        Assert.True(OrbitRule.IsBound(orbiting, earthPos, earthVel, earth, hill));

        double vCirc = OrbitRule.CircularSpeed(earth, 1e8);
        double period = 2 * Math.PI * 1e8 / vCirc;
        ShipState s = orbiting;
        double dMin = double.MaxValue, dMax = 0;
        for (int i = 0; i < 24; i++)
        {
            s = simulator.Run(s, period / 24);
            double d = (s.Position - ephemeris.Position("earth", s.SimTime)).Length;
            dMin = Math.Min(dMin, d);
            dMax = Math.Max(dMax, d);
        }

        Assert.InRange(dMin / 1e8, 0.95, 1.05);
        Assert.InRange(dMax / 1e8, 0.95, 1.05);
    }

    [Fact]
    public void RunAdaptive_MatchesFixedStepWithinTolerance()
    {
        // The live high-warp path (60 s adaptive quanta) must agree with the historic fixed
        // 1 s integration over a 30-day Earth-vicinity cruise with a mid-course burn.
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0);
        Vector2d earth0 = ephemeris.Position("earth", 0);
        Vector2d v0 = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        var start = new ShipState(earth0 + earth0.Normalized() * 5e9, v0, 0);
        var plan = new ManeuverPlan([new ManeuverNode(10 * Day + 12345.6, ManeuverAction.Accelerate, 3)]);

        ShipState fixedStep = simulator.Run(start, 30 * Day, plan);

        ShipState adaptive = start;
        for (int i = 0; i < 30 * Day / 60; i++)
        {
            adaptive = simulator.RunAdaptive(adaptive, 60, plan);
        }

        Assert.Equal(fixedStep.SimTime, adaptive.SimTime, precision: 6);
        double posError = (fixedStep.Position - adaptive.Position).Length / (fixedStep.Position - earth0).Length;
        Assert.True(posError < 1e-3, $"relative divergence {posError:E2}");
        Assert.Equal(fixedStep.Velocity.Length, adaptive.Velocity.Length, fixedStep.Velocity.Length * 1e-4);
    }

    [Fact]
    public void RunAdaptive_IsFrameTimingInvariant()
    {
        // Equal quanta must yield bit-identical results however the caller groups its frames.
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadSol());
        var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0);
        Vector2d earth0 = ephemeris.Position("earth", 0);
        Vector2d v0 = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        var start = new ShipState(earth0 + earth0.Normalized() * 5e9, v0, 0);

        ShipState a = start, b = start;
        for (int i = 0; i < 200; i++) { a = simulator.RunAdaptive(a, 60); }
        for (int i = 0; i < 200; i++) { b = simulator.RunAdaptive(b, 60); }

        Assert.Equal(a, b);
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

    [Fact]
    public void VectorBurn_StraightAhead_MatchesFactorAccelerate()
    {
        // An X-Pilot burn aimed exactly down the velocity vector must be indistinguishable from a
        // classic Factor Accelerate — the two modes agree on the prograde axis.
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var state = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);

        var factor = simulator.Step(state, new ManeuverPlan(
            [new ManeuverNode(SimTime: 0, ManeuverAction.Accelerate, Pulses: 2)]));
        var vector = simulator.Step(state, new ManeuverPlan(
            [new ManeuverNode(SimTime: 0, ManeuverAction.Accelerate, Pulses: 2, Mode: BurnMode.Vector, HeadingDegrees: 0)]));

        Assert.Equal(factor.Velocity.X, vector.Velocity.X, precision: 6);
        Assert.Equal(factor.Velocity.Y, vector.Velocity.Y, precision: 6);
    }

    [Fact]
    public void VectorBurn_AtNinetyDegrees_TurnsTheCourse()
    {
        // Ship coasting along +X; a single 10% X-Pilot pulse aimed at +Y adds cross-track Δv without
        // (to first order) touching the along-track speed. This is the pod-chasing "climb" burn.
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var state = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);

        state = simulator.Step(state, new ManeuverPlan(
            [new ManeuverNode(SimTime: 0, ManeuverAction.Accelerate, Pulses: 1, Mode: BurnMode.Vector, HeadingDegrees: 90)]));

        Assert.Equal(1000, state.Velocity.X, precision: 6);   // along-track untouched
        Assert.Equal(100, state.Velocity.Y, precision: 6);    // 10% of 1000, straight up
    }

    [Fact]
    public void VectorAndFactorNodes_CoexistInOnePlan()
    {
        // Both burn kinds live in the same list and fire in time order (MondayPonder requirement).
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 1);
        var state = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);
        var plan = new ManeuverPlan(
        [
            new ManeuverNode(SimTime: 0, ManeuverAction.Accelerate, Pulses: 1),                                  // Factor: 1000 -> 1100 on X
            new ManeuverNode(SimTime: 1, ManeuverAction.Accelerate, Pulses: 1, Mode: BurnMode.Vector, HeadingDegrees: 90), // Vector: +10% up = +110 on Y
        ]);

        state = simulator.Step(state, plan);   // fires the Factor node at t=0
        state = simulator.Step(state, plan);   // fires the Vector node at t=1

        Assert.Equal(1100, state.Velocity.X, precision: 6);
        Assert.Equal(110, state.Velocity.Y, precision: 6);    // 10% of the post-Factor speed 1100
    }

    [Fact]
    public void VectorBurn_IsDeterministic()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var state = new ShipState(Vector2d.Zero, new Vector2d(1000, 500), 0);
        var plan = new ManeuverPlan(
            [new ManeuverNode(SimTime: 120, ManeuverAction.Accelerate, Pulses: 3, Percent: 7.5, Mode: BurnMode.Vector, HeadingDegrees: 217.3)]);

        var a = simulator.Run(state, 600, plan);
        var b = simulator.Run(state, 600, plan);

        Assert.Equal(a, b);
    }

    [Fact]
    public void FactorBurn_CannotTurn_ButXPilotCan()
    {
        // The MondayPonder problem stated in physics: a Factor burn only scales the velocity
        // magnitude, so a ship coasting along +X can NEVER change heading with it — it cannot follow
        // a pod that climbs off that line. An X-Pilot (Vector) burn adds cross-track Δv and turns.
        var sim = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var start = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);

        var afterFactor = sim.Step(start, new ManeuverPlan(
            [new ManeuverNode(0, ManeuverAction.Accelerate, Pulses: 3)]));
        Assert.Equal(0, afterFactor.Velocity.Y, precision: 6);                  // still dead along +X

        var afterXPilot = sim.Step(start, new ManeuverPlan(
            [new ManeuverNode(0, ManeuverAction.Accelerate, Pulses: 3, Mode: BurnMode.Vector, HeadingDegrees: 90)]));
        Assert.True(afterXPilot.Velocity.Y > 0);                                // heading has turned upward
    }

    [Fact]
    public void XPilotBurn_LetsUsFollowAClimbingPod()
    {
        // Concrete chase: a pod runs alongside us in +X but also climbs in +Y (it "flew upward without
        // a gravity sling"). Coasting straight we stay pinned to the Y=0 line and it climbs away; a few
        // X-Pilot pulses onto its climb heading match its vertical rate and we hold station beside it.
        // Gravity-free so the test isolates heading control.
        var sim = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var podStart = new ShipState(Vector2d.Zero, new Vector2d(1000, 800), 0);
        var usStart = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);

        double horizon = 3600;
        Vector2d podLater = sim.Run(podStart, horizon).Position;

        // Us, coasting straight — no climb, so the whole 800 m/s vertical gap opens up.
        double missCoasting = (sim.Run(usStart, horizon).Position - podLater).Length;

        // Us, adding vertical Δv with an X-Pilot burn aimed at the pod's climb heading (90°).
        var chasePlan = new ManeuverPlan(
            [new ManeuverNode(0, ManeuverAction.Accelerate, Pulses: 8, Mode: BurnMode.Vector, HeadingDegrees: 90)]);
        double missChasing = (sim.Run(usStart, horizon, chasePlan).Position - podLater).Length;

        Assert.True(missChasing < missCoasting * 0.5,
            $"X-Pilot chase should more than halve the miss: coasting {missCoasting:F0} m, chasing {missChasing:F0} m");
    }

    internal static ScenarioDefinition LoadSol() =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json"));
}
