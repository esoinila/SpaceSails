namespace SpaceSails.Core.Tests;

/// <summary>
/// #264 — the integrator honesty half. Near a deep well a single fixed step drifts orbital energy (the
/// owner's Uranus "flower": km/s shed on integration error, not physics). StepGuarded substeps a close,
/// fast pass to keep it honest while staying bit-identical to a plain Step on the cruise, so only a real
/// close approach pays. A minimum-radius clamp was rejected (it changes the physics); the surface itself
/// is enforced separately by SurfaceImpact.
/// </summary>
public class SimulatorImpactGuardTests
{
    // A single central mass at the origin. Radius kept well inside the test periapsis so these energy
    // checks integrate an above-surface pass (the strike is SurfaceImpact's job, not StepGuarded's).
    private static CircularOrbitEphemeris CentralMass(double mu, double radius) =>
        new([new CelestialBody("p", "P", null, mu, radius, 0, 0, 0)]);

    private const double SunMu = 1.32712440018e20;
    private const double EarthOrbitRadius = 1.496e11;

    private static double SpecificEnergy(ShipState s, double mu) =>
        s.Velocity.LengthSquared / 2.0 - mu / s.Position.Length;

    [Fact]
    public void DeepCruise_NeedsNoSubstep()
    {
        // An Earth-like circular cruise: the acceleration is milli-g, a·dt is nothing against the speed,
        // so the guard leaves the step whole — the vast majority of every flight.
        var sim = new Simulator(CentralMass(SunMu, 6.9634e8), timeStepSeconds: 1.0);
        double v = Math.Sqrt(SunMu / EarthOrbitRadius);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, v), 0);

        Assert.Equal(1, sim.LiveSubstepCount(state.Position, state.Velocity, state.SimTime, 1.0));
    }

    [Fact]
    public void DeepCruise_StepGuarded_IsBitIdenticalToStep()
    {
        // Where the guard doesn't fire, StepGuarded must be exactly Step — no divergence from the
        // recorded cruise trajectories the labs gate on.
        var sim = new Simulator(CentralMass(SunMu, 6.9634e8), timeStepSeconds: 1.0);
        double v = Math.Sqrt(SunMu / EarthOrbitRadius);
        var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, v), 0);

        ShipState plain = sim.Step(state);
        ShipState guarded = sim.StepGuarded(state);

        Assert.Equal(plain.Position.X, guarded.Position.X);
        Assert.Equal(plain.Position.Y, guarded.Position.Y);
        Assert.Equal(plain.Velocity.X, guarded.Velocity.X);
        Assert.Equal(plain.Velocity.Y, guarded.Velocity.Y);
    }

    [Fact]
    public void FastDeepPeriapsis_Substeps()
    {
        // A fast, deep periapsis: a·dt is tens of times the sanity bound, so the guard splits the step.
        var sim = new Simulator(CentralMass(1e14, 1e5), timeStepSeconds: 200.0);
        var state = new ShipState(new Vector2d(1e6, 0), new Vector2d(0, 12000), 0);

        Assert.True(sim.LiveSubstepCount(state.Position, state.Velocity, state.SimTime, 200.0) > 1);
    }

    [Fact]
    public void FastDeepPeriapsis_StepGuarded_ConservesEnergyBetterThanStep()
    {
        // The heart of the fix: over one coarse 200 s step across periapsis, a single semi-implicit Euler
        // step sheds/gains energy on numerical error; the substepped guard holds far closer to the true
        // constant. This is the "aerobraking on floating point" the owner accidentally exploited, gone.
        const double mu = 1e14;
        var sim = new Simulator(CentralMass(mu, 1e5), timeStepSeconds: 200.0);
        var state = new ShipState(new Vector2d(1e6, 0), new Vector2d(0, 12000), 0);
        double e0 = SpecificEnergy(state, mu);

        double singleDrift = Math.Abs(SpecificEnergy(sim.Step(state), mu) - e0);
        double guardedDrift = Math.Abs(SpecificEnergy(sim.StepGuarded(state), mu) - e0);

        Assert.True(guardedDrift < singleDrift,
            $"Guarded energy drift {guardedDrift:E3} should beat the single-step drift {singleDrift:E3}.");
    }
}
