using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Companion to labs/12-oops-at-the-moon: the playable aftermath scenario. Luna's rail is set to
/// the lesson's "mild" computed outcome (semi-major axis of the +15%-speed-kicked orbit,
/// approximated as circular since this engine's rails don't support eccentricity); a new haven
/// station orbits Luna as the site of the accident.
/// </summary>
public class OopsScenarioTests
{
    private static ScenarioDefinition LoadOops() =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "oops.json"));

    [Fact]
    public void Oops_Loads_WithWidenedLunaOrbit_AndMinersFolly()
    {
        ScenarioDefinition oops = LoadOops();

        Assert.Equal("Sol (Miners' Folly)", oops.Name);
        Assert.False(oops.ElectricUniverse);

        BodyDefinition luna = oops.Bodies.First(b => b.Id == "luna");
        Assert.Equal("earth", luna.ParentId);
        // Widened from sol.json's real 3.844e8 m -- the lesson's mild-severity semi-major axis.
        Assert.True(luna.OrbitRadiusM > 3.844e8, "Luna's orbit should be widened from the real value.");

        BodyDefinition folly = oops.Bodies.First(b => b.Id == "miners-folly");
        Assert.Equal("luna", folly.ParentId);
        Assert.True(folly.Haven);
        Assert.Equal("station", folly.Kind);
    }

    [Fact]
    public void Oops_Ephemeris_PlacesLunaFartherOut_ButStaysCircularAndConsistent()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadOops());

        Vector2d earth0 = ephemeris.Position("earth", 0);
        Vector2d luna0 = ephemeris.Position("luna", 0) - earth0;
        Assert.Equal(5.781566e8, luna0.Length, tolerance: 1e3);

        // A quarter period later the radius (a circular rail) must be unchanged.
        BodyDefinition lunaDef = LoadOops().Bodies.First(b => b.Id == "luna");
        Vector2d earthQuarter = ephemeris.Position("earth", lunaDef.OrbitPeriodS / 4);
        Vector2d lunaQuarter = ephemeris.Position("luna", lunaDef.OrbitPeriodS / 4) - earthQuarter;
        Assert.Equal(luna0.Length, lunaQuarter.Length, tolerance: 1);
    }

    [Fact]
    public void Oops_TrafficAndPods_Generate()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadOops());

        IReadOnlyList<NpcShip> ships = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(ephemeris, seed: 43, count: 3);
        IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(ephemeris, seed: 44);

        Assert.Equal(8, ships.Count);
        Assert.Equal(3, pods.Count);
        // Miners' Folly is a haven station, so it earns its own depot alongside every planet
        // and the other notable stations/havens -- the sol.json count (12) plus one.
        Assert.Equal(13, depots.Count);

        foreach (NpcShip ship in ships.Concat(pods))
        {
            Assert.True(double.IsFinite(ship.InitialState.Position.X));
            Assert.True(double.IsFinite(ship.InitialState.Velocity.Length));
        }
    }
}
