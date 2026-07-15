using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

public class ScenarioLoaderTests
{
    private const string OneBody =
        """
        { "name": "T", "bodies": [ { "id": "sun", "name": "Sun", "mu": 1.3e20, "bodyRadiusM": 7e8 } ] }
        """;

    [Fact]
    public void Parse_AcceptsBoundEccentricity()
    {
        string json =
            """
            { "name": "T", "bodies": [
              { "id": "sun", "name": "Sun", "mu": 1.3e20, "bodyRadiusM": 7e8 },
              { "id": "comet", "name": "Comet", "parentId": "sun", "mu": 0, "orbitRadiusM": 2e11,
                "orbitPeriodS": 4e7, "eccentricity": 0.85, "argPeriapsisRad": 0.6 }
            ] }
            """;

        var scenario = ScenarioLoader.Parse(json);
        var comet = scenario.Bodies.Single(b => b.Id == "comet");
        Assert.Equal(0.85, comet.Eccentricity, 12);
        Assert.Equal(0.6, comet.ArgPeriapsisRad, 12);
    }

    [Fact]
    public void Parse_DefaultsEccentricityToZero()
    {
        var scenario = ScenarioLoader.Parse(OneBody);
        Assert.Equal(0.0, scenario.Bodies.Single().Eccentricity);
        Assert.Equal(0.0, scenario.Bodies.Single().ArgPeriapsisRad);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(-0.1)]
    public void Parse_RejectsUnboundEccentricity(double e)
    {
        string json =
            $$"""
            { "name": "T", "bodies": [
              { "id": "sun", "name": "Sun", "mu": 1.3e20, "bodyRadiusM": 7e8 },
              { "id": "x", "name": "X", "parentId": "sun", "mu": 0, "orbitRadiusM": 2e11,
                "orbitPeriodS": 4e7, "eccentricity": {{e.ToString(System.Globalization.CultureInfo.InvariantCulture)}} }
            ] }
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => ScenarioLoader.Parse(json));
        Assert.Contains("eccentricity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
