namespace SpaceSails.Core.Tests;

/// <summary>M28 (Sunday PR-B): slugs and missiles — ballistic, self-evaporating, hit-checked
/// with the closed-form segment minimum so nothing tunnels between steps.</summary>
public class OrdnanceTests
{
    private sealed class EmptySpace : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } = [];

        public Vector2d Position(string bodyId, double simTime) => Vector2d.Zero;
    }

    [Fact]
    public void Slug_FiredOnAFiringSolution_HitsACoastingTarget()
    {
        // The full PR-A + PR-B loop: solve the shot, fly the slug, detect the hit.
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        var shooter = new ShipState(Vector2d.Zero, new Vector2d(0, 200), 0);
        var target = new ShipState(new Vector2d(2e6, 1e6), new Vector2d(-150, 300), 0);

        double tHit = 3600;
        Vector2d predicted = target.Position + target.Velocity * tHit;
        FireControl.Solution solution = FireControl.Solve(simulator, shooter, 4000, predicted, tHit);
        Assert.True(solution.Converged);

        var slug = new ShipState(
            shooter.Position, shooter.Velocity + solution.LaunchDirection * solution.MuzzleSpeed, 0);
        ShipState prey = target;
        bool hit = false;
        for (int step = 0; step < 120 && !hit; step++)
        {
            ShipState slugNext = simulator.Run(slug, 60);
            ShipState preyNext = simulator.Run(prey, 60);
            hit = OrdnanceRule.StepHits(slug.Position, slugNext.Position, prey.Position, preyNext.Position);
            (slug, prey) = (slugNext, preyNext);
        }

        Assert.True(hit, "the solved slug must pass within the hit radius of the coasting target");
    }

    [Fact]
    public void Missile_CorrectsOntoAnEvadingTarget_ThatASlugWouldMiss()
    {
        var simulator = new Simulator(new EmptySpace(), timeStepSeconds: 60);
        // Head-on-ish geometry, then the target sidesteps 250 m/s at t=600 — enough to open
        // a miss of 250·3000 ≈ 7.5e5 m on a pure ballistic round.
        var round0 = new ShipState(Vector2d.Zero, new Vector2d(2000, 0), 0);
        var target0 = new ShipState(new Vector2d(7.2e6, 0), Vector2d.Zero, 0);

        static ShipState Evade(ShipState s, double t) =>
            t == 600 ? s with { Velocity = s.Velocity + new Vector2d(0, 250) } : s;

        // Ballistic control: misses.
        ShipState slug = round0, preyA = target0;
        bool slugHit = false;
        for (int step = 0; step < 80 && !slugHit; step++)
        {
            preyA = Evade(preyA, preyA.SimTime);
            ShipState slugNext = simulator.Run(slug, 60);
            ShipState preyNext = simulator.Run(preyA, 60);
            slugHit = OrdnanceRule.StepHits(slug.Position, slugNext.Position, preyA.Position, preyNext.Position);
            (slug, preyA) = (slugNext, preyNext);
        }

        Assert.False(slugHit, "the ballistic control round should miss the evading target");

        // The missile spends its correction budget and connects.
        ShipState missile = round0, preyB = target0;
        double budget = OrdnanceRule.MissileDeltaVBudget;
        bool missileHit = false;
        for (int step = 0; step < 80 && !missileHit; step++)
        {
            preyB = Evade(preyB, preyB.SimTime);
            (missile, double spent) = OrdnanceRule.Guide(missile, preyB, 60, budget);
            budget -= spent;
            ShipState missileNext = simulator.Run(missile, 60);
            ShipState preyNext = simulator.Run(preyB, 60);
            missileHit = OrdnanceRule.StepHits(missile.Position, missileNext.Position, preyB.Position, preyNext.Position);
            (missile, preyB) = (missileNext, preyNext);
        }

        Assert.True(missileHit, "the guided round must correct onto the evading target");
        Assert.True(budget >= 0, "corrections must never overspend the budget");
        Assert.True(budget < OrdnanceRule.MissileDeltaVBudget, "the evasion must have cost real Δv");
    }

    [Fact]
    public void Rounds_SelfEvaporate()
    {
        var slug = new OrdnanceRound("s1", OrdnanceKind.Slug, null, LaunchedAtSimTime: 1000);
        Assert.False(OrdnanceRule.Expired(slug, 1000 + OrdnanceRule.SlugLifetimeSeconds - 1));
        Assert.True(OrdnanceRule.Expired(slug, 1000 + OrdnanceRule.SlugLifetimeSeconds));

        var missile = new OrdnanceRound("m1", OrdnanceKind.Missile, "prey", LaunchedAtSimTime: 0);
        Assert.False(OrdnanceRule.Expired(missile, OrdnanceRule.SlugLifetimeSeconds));
        Assert.True(OrdnanceRule.Expired(missile, OrdnanceRule.MissileLifetimeSeconds));
    }

    [Fact]
    public void Guide_SpendsNothingWhenTheBudgetIsGone()
    {
        var missile = new ShipState(Vector2d.Zero, new Vector2d(1000, 0), 0);
        var target = new ShipState(new Vector2d(1e6, 5e5), Vector2d.Zero, 0);

        (ShipState unchanged, double spent) = OrdnanceRule.Guide(missile, target, 60, remainingBudget: 0);

        Assert.Equal(0, spent);
        Assert.Equal(missile, unchanged);
    }
}
