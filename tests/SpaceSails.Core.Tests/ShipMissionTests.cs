using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-15, the captain's position (docs/SaturdayPlan/StationDesks.md addendum): the mission model
/// and its catalog of selectable options must be pure and deterministic (repo agreement §9).
/// </summary>
public class ShipMissionTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    private static ScenarioDefinition LoadWheel() =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "wheel.json"));

    [Fact]
    public void Describe_MatchesTheAddendumsExamples()
    {
        Assert.Equal("Free sailing", ShipMission.Default.Describe());
        Assert.Equal("Hunt: He3 haulers", new ShipMission(MissionKind.Hunt, TargetCargo: "He3").Describe());
        Assert.Equal(
            "Trade run: Earth → Mars",
            new ShipMission(MissionKind.TradeRun, OriginBodyId: "earth", DestinationBodyId: "mars").Describe());
        Assert.Equal("Lay low: Enceladus", new ShipMission(MissionKind.LayLow, HavenBodyId: "enceladus").Describe());
        Assert.Equal(
            "Survey: Saturn–Mars corridor",
            new ShipMission(MissionKind.Survey, CorridorA: "saturn", CorridorB: "mars").Describe());
    }

    [Fact]
    public void Describe_FlyTo_NamesTheDestination()
    {
        Assert.Equal("Make for: Mercury orbit",
            new ShipMission(MissionKind.FlyTo, DestinationBodyId: "mercury").Describe());
    }

    [Fact]
    public void Catalog_FlyTo_OffersOrbitableWorldsOnly()
    {
        MissionOptions catalog = MissionCatalog.Build(Sol());

        Assert.Contains(catalog.FlyTo, m => m.DestinationBodyId == "mercury");
        Assert.Contains(catalog.FlyTo, m => m.DestinationBodyId == "saturn");
        Assert.Contains(catalog.FlyTo, m => m.DestinationBodyId == "luna");
        // Not the sun (you already orbit it), not stations (no Hill sphere — dock instead).
        Assert.DoesNotContain(catalog.FlyTo, m => m.DestinationBodyId == "sun");
        Assert.All(catalog.FlyTo, m =>
        {
            CelestialBody body = Sol().Bodies.First(b => b.Id == m.DestinationBodyId);
            Assert.NotEqual(BodyKind.Station, body.Kind);
            Assert.NotNull(body.ParentId);
        });
    }

    [Fact]
    public void Describe_HumanizesHyphenatedBodyIds()
    {
        var mission = new ShipMission(MissionKind.LayLow, HavenBodyId: "ringside-exchange");
        Assert.Equal("Lay low: Ringside Exchange", mission.Describe());
    }

    [Fact]
    public void Catalog_Sol_ContainsExpectedHuntTradeAndSurveyOptions()
    {
        MissionOptions catalog = MissionCatalog.Build(Sol());

        // Distinct cargo classes from sol.json's routes: He3, Machinery, Ice, Alloys, Compute cores.
        Assert.Contains(catalog.Hunt, m => m.TargetCargo == "He3");
        Assert.Contains(catalog.Hunt, m => m.TargetCargo == "Machinery");
        Assert.Contains(catalog.Hunt, m => m.TargetCargo == "Ice");
        Assert.Contains(catalog.Hunt, m => m.TargetCargo == "Alloys");
        Assert.Contains(catalog.Hunt, m => m.TargetCargo == "Compute cores");
        // "He3" appears on four routes (saturn->mars, saturn->earth, jupiter->mars, titan->mars,
        // titan->earth is a fifth) but must collapse to one Hunt option.
        Assert.Single(catalog.Hunt, m => m.TargetCargo == "He3");

        Assert.Contains(catalog.TradeRuns, m => m.OriginBodyId == "saturn" && m.DestinationBodyId == "mars");
        Assert.Contains(catalog.TradeRuns, m => m.OriginBodyId == "earth" && m.DestinationBodyId == "mars");
        // earth->mercury-compute and mercury-compute->earth are distinct directed trade runs.
        Assert.Contains(catalog.TradeRuns, m => m.OriginBodyId == "earth" && m.DestinationBodyId == "mercury-compute");
        Assert.Contains(catalog.TradeRuns, m => m.OriginBodyId == "mercury-compute" && m.DestinationBodyId == "earth");

        // Havens in sol.json: Enceladus (moon) and Ringside Exchange (station) out at Saturn, plus
        // the inner grey-market docks Cinder Roost (Venus), The Space Bar (Mars) and The Tilt (Uranus),
        // and Selene Gate — the Luna-vicinity fuel port added for #157 (its haven flag closes Lab 28's
        // stranded-at-Luna gap and, like any haven, offers a lay-low berth).
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "enceladus");
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "ringside-exchange");
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "cinder-roost");
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "the-space-bar");
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "the-tilt");
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "selene-gate");
        // The #289 outer oases: a lay-low haven now rides Jupiter (The Red Eye) and Neptune (The Deep).
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "red-eye");
        Assert.Contains(catalog.LayLow, m => m.HavenBodyId == "the-deep");
        Assert.Equal(8, catalog.LayLow.Count);

        // Survey corridors collapse direction: saturn/mars from one route, earth/mercury-compute
        // from two routes (opposite directions) must still yield a single corridor.
        Assert.Contains(catalog.Survey, m => m.CorridorA == "mars" && m.CorridorB == "saturn");
        Assert.Single(catalog.Survey, m => new[] { m.CorridorA, m.CorridorB }.OrderBy(x => x).SequenceEqual(new[] { "earth", "mercury-compute" }.OrderBy(x => x)));
    }

    [Fact]
    public void Catalog_WheelScenario_WithoutTraffic_OffersOnlyLayLowIfAnyHavens()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadWheel());
        Assert.Null(ephemeris.Traffic);

        MissionOptions catalog = MissionCatalog.Build(ephemeris);

        Assert.Empty(catalog.Hunt);
        Assert.Empty(catalog.TradeRuns);
        Assert.Empty(catalog.Survey);
        // Lay low reads the body list directly, independent of the traffic section.
        Assert.All(catalog.LayLow, m => Assert.True(ephemeris.Bodies.First(b => b.Id == m.HavenBodyId).IsHaven));
    }

    [Fact]
    public void Catalog_IsDeterministic_AcrossRepeatedBuilds()
    {
        MissionOptions first = MissionCatalog.Build(Sol());
        MissionOptions second = MissionCatalog.Build(Sol());

        Assert.Equal(first.Hunt, second.Hunt);
        Assert.Equal(first.TradeRuns, second.TradeRuns);
        Assert.Equal(first.LayLow, second.LayLow);
        Assert.Equal(first.Survey, second.Survey);
    }

    [Fact]
    public void Catalog_PreservesFirstSeenOrder_FromScenarioRoutes()
    {
        MissionOptions catalog = MissionCatalog.Build(Sol());

        // sol.json lists He3 first (saturn->mars), so the Hunt option for He3 must come before
        // Machinery (mars->earth, listed third).
        int he3Index = IndexOfCargo(catalog.Hunt, "He3");
        int machineryIndex = IndexOfCargo(catalog.Hunt, "Machinery");
        Assert.True(he3Index >= 0 && machineryIndex >= 0 && he3Index < machineryIndex);
    }

    private static int IndexOfCargo(IReadOnlyList<ShipMission> options, string cargo)
    {
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].TargetCargo == cargo)
            {
                return i;
            }
        }

        return -1;
    }
}
