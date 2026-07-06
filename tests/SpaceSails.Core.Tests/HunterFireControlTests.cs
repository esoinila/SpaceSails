namespace SpaceSails.Core.Tests;

/// <summary>
/// The aim-solution fork for hunters (core-gravity review, 2026-07-06): a hunter flies the
/// pursuit law — no gravity, +0.5 m/s² toward the player every 60 s quantum — so the gun
/// deck must predict it by REPLAYING <see cref="EncounterRule.AdvanceHunter"/> against the
/// player's plotted course, never by dead-reckoning it through the gravity Simulator. The
/// gravity estimate is wrong twice over (adds a pull the hunter never feels, drops the
/// thrust it does apply): ½·a·τ² ≈ 13,000 km of aim error on a 2 h slug flight, against
/// OrdnanceRule's 5e5 m hit radius.
/// </summary>
public class HunterFireControlTests
{
    private const double SunMu = 1.32712440018e20;
    private const double EarthOrbitRadius = 1.496e11;

    private static CircularOrbitEphemeris SunOnly() =>
        new([new CelestialBody("sun", "Sun", null, SunMu, 6.9634e8, 0, 0, 0)]);

    private static HunterState ActiveHunter(Vector2d position, Vector2d velocity) =>
        new("hunter-t", "Test Wolf", "earth", SpawnedAtSimTime: 0, ActivationSimTime: 0,
            new ShipState(position, velocity, 0), CaughtPlayer: false, BrokenOff: false);

    private static List<TrajectorySample> LinearPath(Vector2d start, Vector2d velocity, double horizon, double dt)
    {
        var samples = new List<TrajectorySample>();
        for (double t = 0; t <= horizon + 1e-9; t += dt)
        {
            samples.Add(new TrajectorySample(t, start + velocity * t));
        }

        return samples;
    }

    [Fact]
    public void PredictHunterPath_ReplaysAdvanceHunter_Exactly()
    {
        // The predictor must be the SAME integrator the game runs, not a lookalike: replay
        // AdvanceHunter by hand at its own quanta against the true (linear) player motion and
        // demand the predicted track lands on the manual one.
        var playerVelocity = new Vector2d(1000, 0);
        List<TrajectorySample> playerPath = LinearPath(Vector2d.Zero, playerVelocity, 3 * 3600, 60);
        HunterState hunter = ActiveHunter(new Vector2d(6e8, 2e8), new Vector2d(-3000, 4000));

        IReadOnlyList<TrajectorySample> predicted =
            EncounterRule.PredictHunterPath(hunter, playerPath, 3 * 3600);

        HunterState manual = hunter;
        for (double stepTime = EncounterRule.HunterStepSeconds;
             stepTime <= 3 * 3600 + 1e-9 && !manual.CaughtPlayer && !manual.BrokenOff;
             stepTime += EncounterRule.HunterStepSeconds)
        {
            var player = new ShipState(playerVelocity * stepTime, playerVelocity, stepTime);
            manual = EncounterRule.AdvanceHunter(manual, player, stepTime);
        }

        Assert.Equal(3 * 3600, predicted[^1].SimTime, precision: 6);
        double deviation = (predicted[^1].Position - manual.State.Position).Length;
        Assert.True(deviation < 1e-3,
            $"the predictor must replay the live integrator bit-for-bit (off by {deviation:E2} m)");
    }

    [Fact]
    public void Solve_AgainstThrustingHunter_PursuitAimHits_GravityAimStructurallyMisses()
    {
        // The money test: player on a circular 1 AU orbit, a collector 30,000 km off with
        // 4 km/s of crossing drift (never "caught": catch needs < 3 km/s relative), thrusting
        // the whole 2 h flight. Aim where the PURSUIT LAW says it will be, run the shooting
        // method, fly the slug through real gravity — and demand the round passes inside the
        // hit radius of the hunter flown by the game's own AdvanceHunter. Then show the old
        // gravity dead-reckon put the aim point tens of hit-radii off the truth.
        CircularOrbitEphemeris ephemeris = SunOnly();
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        double circular = Math.Sqrt(SunMu / EarthOrbitRadius);
        var player = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circular), 0);
        HunterState hunter = ActiveHunter(
            new Vector2d(EarthOrbitRadius, -3e7), new Vector2d(4000, circular));

        double tHit = 2 * 3600;
        IReadOnlyList<TrajectorySample> playerPath =
            simulator.ProjectAdaptive(player, null, 3 * 3600, maxTimeStep: 60);
        IReadOnlyList<TrajectorySample> pursuit =
            EncounterRule.PredictHunterPath(hunter, playerPath, tHit);
        Vector2d aim = pursuit[^1].Position;

        FireControl.Solution solution = FireControl.Solve(simulator, player, 8000, aim, tHit);
        Assert.True(solution.Converged, $"pursuit-aim solve must converge (miss {solution.ExpectedMissMeters:E2} m)");

        var slug = new ShipState(
            player.Position, player.Velocity + solution.LaunchDirection * solution.MuzzleSpeed, 0);
        IReadOnlyList<TrajectorySample> slugPath =
            simulator.ProjectAdaptive(slug, null, tHit, maxTimeStep: 60);
        InterceptEstimate.Result? closest =
            InterceptEstimate.Against(slugPath, pursuit, OrdnanceRule.HitRadiusMeters);

        Assert.NotNull(closest);
        Assert.True(closest.Value.MinDistance < OrdnanceRule.HitRadiusMeters,
            $"the slug must pass inside the {OrdnanceRule.HitRadiusMeters:E1} m hit radius " +
            $"of the pursuit-law hunter (closest {closest.Value.MinDistance:E2} m)");

        // The old model: dead-reckon the hunter through gravity as if it were a freighter.
        Vector2d gravityAim = simulator.RunAdaptive(
            new ShipState(hunter.State.Position, hunter.State.Velocity, 0), tHit).Position;
        double structuralError = (gravityAim - aim).Length;
        Assert.True(structuralError > 10 * OrdnanceRule.HitRadiusMeters,
            $"gravity dead-reckoning a thrusting hunter must be shown structurally hopeless " +
            $"(error {structuralError:E2} m ≈ {structuralError / OrdnanceRule.HitRadiusMeters:F0} hit radii)");
    }

    [Fact]
    public void PredictHunterPath_PeeledHunter_CoastsStraight_NotOnAGravityArc()
    {
        // A peeled (or sun-blinded, or fitting-out) hunter drifts LINEARLY — AdvanceHunter has
        // no gravity term by design (owner's call). Predicting even the coast through the
        // gravity simulator bends it ~1.4e6 m over 6 h at 1 AU: enough to blow the hit radius
        // with no thrust involved at all.
        CircularOrbitEphemeris ephemeris = SunOnly();
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        double circular = Math.Sqrt(SunMu / EarthOrbitRadius);
        var start = new ShipState(new Vector2d(EarthOrbitRadius, -3e7), new Vector2d(4000, circular), 0);
        HunterState peeled = ActiveHunter(start.Position, start.Velocity) with
        {
            PeeledUntilSimTime = double.MaxValue,
        };
        List<TrajectorySample> playerPath = LinearPath(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, circular), 6 * 3600, 3600);

        double horizon = 6 * 3600;
        IReadOnlyList<TrajectorySample> predicted =
            EncounterRule.PredictHunterPath(peeled, playerPath, horizon);

        Vector2d straight = start.Position + start.Velocity * horizon;
        Assert.True((predicted[^1].Position - straight).Length < 1.0,
            "a peeled hunter's predicted track must be the straight coast the game will fly");

        Vector2d gravityBent = simulator.RunAdaptive(start, horizon).Position;
        Assert.True((gravityBent - straight).Length > 5e5,
            "the gravity model demonstrably bends the coast past the hit radius — the fork matters even for a coasting hunter");
    }
}
