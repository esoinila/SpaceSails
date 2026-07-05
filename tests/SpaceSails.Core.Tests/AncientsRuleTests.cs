namespace SpaceSails.Core.Tests;

/// <summary>M28 (Sunday PR-D): pyramid satellites and the Ancients' auto-plot.</summary>
public class AncientsRuleTests
{
    [Fact]
    public void Pyramids_AreDeterministic_AndDeliberatelyUnKeplerian()
    {
        // Same time, same place — twice.
        Assert.Equal(AncientsRule.PyramidPosition(0, 1e6), AncientsRule.PyramidPosition(0, 1e6));
        Assert.Equal(AncientsRule.PyramidPosition(1, 5e7), AncientsRule.PyramidPosition(1, 5e7));

        // Pyramid 0 circles 2.3 AU in 200 days; Kepler demands ~3.5 years there. The ancients
        // don't obey — the orbit closes (returns to start) after exactly its stated period.
        Vector2d start = AncientsRule.PyramidPosition(0, 0);
        Vector2d afterPeriod = AncientsRule.PyramidPosition(0, 200 * 86400.0);
        Assert.True((start - afterPeriod).Length < 1, "the pyramid's orbit must close on its own period");

        double keplerPeriod = Math.Tau * Math.Sqrt(Math.Pow(3.44e11, 3) / 1.32712440018e20);
        Assert.True(keplerPeriod > 2 * 200 * 86400.0,
            "the stated period must be impossibly fast for that radius — that's the tell");
    }

    [Fact]
    public void Pyramids_RevealOnlyClose_IgnoringSensorRules()
    {
        double t = 12 * 86400.0;
        Vector2d pyramid = AncientsRule.PyramidPosition(0, t);

        Vector2d far = pyramid + new Vector2d(AncientsRule.RevealRangeMeters * 2, 0);
        Vector2d near = pyramid + new Vector2d(AncientsRule.RevealRangeMeters * 0.5, 0);
        Vector2d close = pyramid + new Vector2d(AncientsRule.GrantRangeMeters * 0.5, 0);

        Assert.False(AncientsRule.Revealed(0, far, t));
        Assert.True(AncientsRule.Revealed(0, near, t));
        Assert.False(AncientsRule.InGrantRange(0, near, t));
        Assert.True(AncientsRule.InGrantRange(0, close, t));
    }

    [Fact]
    public void AutoPlot_LaysACourse_ThatBeatsCoasting()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        Vector2d earthPos = ephemeris.Position("earth", 0);
        Vector2d earthVel = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
        // Free-sailing near Earth, destination Mars — the classic first voyage.
        var ship = new ShipState(earthPos + new Vector2d(2e9, 0), earthVel, 0);

        AncientsRule.AutoPlotResult? result = AncientsRule.AutoPlot(ephemeris, ship, "mars");

        Assert.NotNull(result);
        // Coasting control: closest approach to Mars with no burns at all.
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        IReadOnlyList<TrajectorySample> coast = simulator.ProjectAdaptive(
            ship, null, 730 * 86400.0, maxTimeStep: 86400, maxSamples: 800);
        double coastMiss = double.MaxValue;
        foreach (TrajectorySample sample in coast)
        {
            coastMiss = Math.Min(coastMiss, (ephemeris.Position("mars", sample.SimTime) - sample.Position).Length);
        }

        Assert.True(result.Value.MissDistance < coastMiss / 2,
            $"the ancient pilot ({result.Value.MissDistance:E2} m) must at least halve the coasting miss ({coastMiss:E2} m)");
        Assert.True(result.Value.MissDistance < 1.5e10,
            $"and put the ship within striking distance of Mars (got {result.Value.MissDistance:E2} m)");
        Assert.NotEmpty(result.Value.Plan.Nodes);
    }
}
