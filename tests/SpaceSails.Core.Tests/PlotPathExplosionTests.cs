using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Regression gates for issue #153: the plotted trajectory of a single ±10% Factor node exploded
/// into a straight ~185 AU ray. Root cause was NOT a re-fired plan node (a Pulses=1 node fires
/// exactly once — see <see cref="SingleFactorNode_FiresExactlyOnce"/>) but stiff atmospheric drag
/// integrated with an explicit Euler step: the projection's coarse adaptive step (tens of seconds
/// near a moon) drove <c>v += a_drag·dt</c> past the ship's speed deep in Titan's dense shell,
/// reversing and amplifying the velocity every step. <see cref="Simulator.ApplyStableDrag"/> now
/// relaxes the relative velocity semi-implicitly, which can never add energy at any dt. The live
/// 1 s sim never hit the blow-up, matching the owner's report that the leg flew normally.
/// </summary>
public class PlotPathExplosionTests
{
    private const double SaturnMu = 3.7931187e16;
    private const double TitanMu = 8.9781e12;
    private const double EnceladusMu = 7.211e9;
    private const double Day = 86400.0;
    private const double Au = 1.495978707e11;

    private static string FindRepoFile(string rel)
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, rel);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException(rel);
    }

    private static (ICelestialEphemeris Eph, Simulator Sim) MakeSaturnSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("saturn", "Saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
            new CelestialBody("titan", "Titan", "saturn", TitanMu, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
            new CelestialBody("enceladus", "Enceladus", "saturn", EnceladusMu, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, IsHaven: true),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 1.0));
    }

    // A ship in a circular orbit ~17,400 km from Titan, carrying Titan's rail velocity. `sense` = +1
    // prograde, -1 retrograde (the retrograde case is what grazes Titan's shell after the burn).
    private static ShipState TitanOrbit(ICelestialEphemeris eph, double t0, int sense)
    {
        Vector2d titanPos = eph.Position("titan", t0);
        Vector2d titanVel = TransferMath.BodyVelocity(eph, "titan", t0);
        const double r = 1.7358e7; // ~17,358 km (the owner's screenshot)
        Vector2d outward = (titanPos - eph.Position("saturn", t0)).Normalized();
        double vCirc = Math.Sqrt(TitanMu / r);
        var tangential = new Vector2d(-outward.Y, outward.X) * sense;
        return new ShipState(titanPos + outward * r, titanVel + tangential * vCirc, t0);
    }

    private static ManeuverPlan OneProgradePulse(double simTime) =>
        new([new ManeuverNode(simTime + 12 * 3600, ManeuverAction.Accelerate, Pulses: 1, Percent: 10.0)]);

    // Max implied speed between consecutive projected samples, as a multiple of the initial speed.
    private static double MaxImpliedSpeedRatio(ShipState ship, IReadOnlyList<TrajectorySample> samples)
    {
        double initial = ship.Velocity.Length;
        double maxImplied = 0;
        for (int i = 1; i < samples.Count; i++)
        {
            double dt = samples[i].SimTime - samples[i - 1].SimTime;
            if (dt <= 0)
            {
                continue;
            }
            maxImplied = Math.Max(maxImplied, (samples[i].Position - samples[i - 1].Position).Length / dt);
        }
        return maxImplied / initial;
    }

    [Fact]
    public void SingleFactorNode_ProjectedFiveDays_DoesNotExplode()
    {
        var (eph, sim) = MakeSaturnSystem();
        ShipState ship = TitanOrbit(eph, t0: 0, sense: 1);
        IReadOnlyList<TrajectorySample> samples =
            sim.ProjectAdaptive(ship, OneProgradePulse(ship.SimTime), 5 * Day, maxTimeStep: 3 * 3600, maxSamples: 8000);

        // One 1.1x pulse plus orbital dynamics; nothing should approach a runaway.
        Assert.True(MaxImpliedSpeedRatio(ship, samples) < 1.5,
            $"projected samples exploded ({MaxImpliedSpeedRatio(ship, samples):F1}x initial speed)");
    }

    [Fact]
    public void SingleFactorNode_FiresExactlyOnce()
    {
        var (eph, sim) = MakeSaturnSystem();
        ShipState ship = TitanOrbit(eph, t0: 0, sense: 1);
        ManeuverPlan plan = OneProgradePulse(ship.SimTime);

        ShipState before = sim.RunAdaptive(ship, 12 * 3600 - 60, plan);
        ShipState after = sim.RunAdaptive(ship, 12 * 3600 + 60, plan);

        // A Pulses=1 node scales the speed by a single 1.1x factor — never twice, never dozens of times.
        Assert.InRange(after.Velocity.Length / before.Velocity.Length, 1.0, 1.21);
    }

    // The headline gate: the owner's exact geometry (full sol.json, retrograde Titan orbit at the
    // phase that grazes Titan's atmosphere after the prograde burn). Before the fix this projection
    // ran away to ~160 AU; after it stays local (the ship aerobrakes, it does not fling off).
    [Fact]
    public void FullSol_RetrogradeTitanGraze_ProjectionStaysLocal()
    {
        var scenario = ScenarioLoader.LoadFile(FindRepoFile(Path.Combine("scenarios", "sol.json")));
        var eph = CircularOrbitEphemeris.FromScenario(scenario);
        var sim = new Simulator(eph, timeStepSeconds: 1.0);

        double t0 = 44 * (1.377648e6 / 48.0); // the exploding Titan phase
        ShipState ship = TitanOrbit(eph, t0, sense: -1);
        IReadOnlyList<TrajectorySample> samples =
            sim.ProjectAdaptive(ship, OneProgradePulse(t0), 5 * Day, maxTimeStep: 3 * 3600, maxSamples: 8000);

        double spreadAu = 0;
        foreach (TrajectorySample s in samples)
        {
            spreadAu = Math.Max(spreadAu, (s.Position - ship.Position).Length / Au);
        }

        // 5 days at ~7-16 km/s spans well under a hundredth of an AU. The pre-fix ray was ~160 AU.
        Assert.True(spreadAu < 0.2, $"projection ran away to {spreadAu:F2} AU (drag-instability regression)");
    }

    // Sweep every Titan phase and both orbit senses so no future physics tweak reopens the runaway.
    [Fact]
    public void FullSol_AllTitanPhasesAndSenses_NeverRunAway()
    {
        var scenario = ScenarioLoader.LoadFile(FindRepoFile(Path.Combine("scenarios", "sol.json")));
        var eph = CircularOrbitEphemeris.FromScenario(scenario);
        var sim = new Simulator(eph, timeStepSeconds: 1.0);
        double titanPeriod = 1.377648e6;

        double worst = 0;
        for (int k = 0; k < 48; k++)
        {
            double t0 = k * (titanPeriod / 48.0);
            foreach (int sense in new[] { 1, -1 })
            {
                ShipState ship = TitanOrbit(eph, t0, sense);
                IReadOnlyList<TrajectorySample> samples =
                    sim.ProjectAdaptive(ship, OneProgradePulse(t0), 5 * Day, maxTimeStep: 3 * 3600, maxSamples: 8000);
                foreach (TrajectorySample s in samples)
                {
                    worst = Math.Max(worst, (s.Position - ship.Position).Length / Au);
                }
            }
        }

        Assert.True(worst < 0.2, $"a Titan phase/sense still runs the projection away to {worst:F2} AU");
    }

    // The Core invariant that makes the fix correct at ANY step size: atmospheric drag can only remove
    // relative kinetic energy. Even a deliberately huge, deep, dense-atmosphere step must never speed
    // the ship up relative to the body — the explicit scheme did exactly that (it reversed v_rel).
    [Fact]
    public void StableDrag_NeverIncreasesRelativeSpeed_EvenForAnEnormousStep()
    {
        // Titan-at-origin so the relative frame is clean; a ship sitting deep in the shell at speed.
        var eph = new CircularOrbitEphemeris(
            [new CelestialBody("titan", "Titan", null, TitanMu, 2.575e6, 0, 0, 0,
                Atmosphere: new Atmosphere(RefDensity: 5.3, ScaleHeight: 4.0e4, TopAltitude: 3.0e5))]);
        var sim = new Simulator(eph, timeStepSeconds: 1.0);

        var position = new Vector2d(2.575e6 + 3.0e4, 0); // 30 km altitude, deep in the dense shell
        var velocity = new Vector2d(0, 4000);            // 4 km/s across the flow

        foreach (double dt in new[] { 0.5, 5.0, 60.0, 3600.0 })
        {
            Vector2d after = sim.ApplyStableDrag(position, velocity, simTime: 0, dt);
            Assert.True(after.Length <= velocity.Length + 1e-9,
                $"drag increased speed at dt={dt}s: {velocity.Length:F1} -> {after.Length:F1} m/s");
        }
    }
}
