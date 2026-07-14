using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Tuesday plan PR-A: the optional <c>"hidden"</c> flag on a scenario body (Expanse rules —
/// a body on its rail that the charts don't show until an intel-fed scan reveals it).
/// </summary>
public class HiddenBodyTests
{
    [Fact]
    public void Hidden_DefaultsToFalse_WhenAbsent()
    {
        const string json = """
        {
          "name": "Tiny",
          "bodies": [
            { "id": "sun", "name": "Sun", "mu": 1.327e20, "bodyRadiusM": 6.9e8 }
          ]
        }
        """;

        BodyDefinition sun = ScenarioLoader.Parse(json).Bodies.Single();
        Assert.False(sun.Hidden);
    }

    [Fact]
    public void Hidden_RoundTrips_WhenSetTrue()
    {
        const string json = """
        {
          "name": "Tiny",
          "bodies": [
            { "id": "sun", "name": "Sun", "mu": 1.327e20, "bodyRadiusM": 6.9e8 },
            { "id": "ghost", "name": "Ghost", "parentId": "sun", "mu": 0, "bodyRadiusM": 3,
              "orbitRadiusM": 1.7e11, "orbitPeriodS": 3.82e7, "initialPhaseRad": 2.5,
              "kind": "station", "hidden": true }
          ]
        }
        """;

        ScenarioDefinition scenario = ScenarioLoader.Parse(json);
        Assert.False(scenario.Bodies.First(b => b.Id == "sun").Hidden);
        Assert.True(scenario.Bodies.First(b => b.Id == "ghost").Hidden);
    }

    [Fact]
    public void SolScenario_MarksTheDerelictRoadsterHidden()
    {
        ScenarioDefinition sol = SimulatorTests.LoadSol();
        BodyDefinition roadster = sol.Bodies.First(b => b.Id == "derelict-roadster");

        Assert.True(roadster.Hidden);
        // Every other Sol body stays plainly on the charts.
        Assert.All(sol.Bodies.Where(b => b.Id != "derelict-roadster"), b => Assert.False(b.Hidden));
    }
}
