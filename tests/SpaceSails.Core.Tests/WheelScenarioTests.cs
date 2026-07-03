using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

public class WheelScenarioTests
{
    private const double Day = 86400;

    private static ScenarioDefinition LoadWheel() =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "wheel.json"));

    [Fact]
    public void Wheel_Loads_WithSaturnHub_AndElectricLayer()
    {
        ScenarioDefinition wheel = LoadWheel();

        Assert.Equal("Wheel of the World", wheel.Name);
        Assert.True(wheel.ElectricUniverse);
        Assert.NotEmpty(wheel.Streams);
        foreach (string id in new[] { "venus", "earth", "mars" })
        {
            Assert.Equal("saturn", wheel.Bodies.First(b => b.Id == id).ParentId);
        }
        Assert.Equal("earth", wheel.Bodies.First(b => b.Id == "luna").ParentId);
    }

    [Fact]
    public void Spoke_StaysRigid_AsTheWheelTurns()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadWheel());

        foreach (double t in new[] { 0.0, 13 * Day, 47 * Day, 200 * Day, 1000 * Day })
        {
            Vector2d hub = ephemeris.Position("saturn", t);
            Vector2d venus = ephemeris.Position("venus", t) - hub;
            Vector2d earth = ephemeris.Position("earth", t) - hub;
            Vector2d mars = ephemeris.Position("mars", t) - hub;

            // Radii hold…
            Assert.Equal(2.5e10, venus.Length, tolerance: 1);
            Assert.Equal(4.0e10, earth.Length, tolerance: 1);
            Assert.Equal(5.5e10, mars.Length, tolerance: 1);

            // …and all three stay on one ray from the hub (cross products vanish, dots positive).
            Assert.Equal(0, Math.Abs(venus.X * earth.Y - venus.Y * earth.X) / (venus.Length * earth.Length), precision: 9);
            Assert.Equal(0, Math.Abs(venus.X * mars.Y - venus.Y * mars.X) / (venus.Length * mars.Length), precision: 9);
            Assert.True(venus.X * earth.X + venus.Y * earth.Y > 0, "Spoke bodies must be on the same side of the hub.");
        }
    }

    [Fact]
    public void Wheel_TrafficAndPods_Generate()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(LoadWheel());

        IReadOnlyList<NpcShip> ships = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(ephemeris, seed: 43, count: 3);

        Assert.Equal(8, ships.Count);
        Assert.Equal(3, pods.Count);
        // The generator must produce finite, sane states in the exotic geometry.
        foreach (NpcShip ship in ships.Concat(pods))
        {
            Assert.True(double.IsFinite(ship.InitialState.Position.X));
            Assert.True(double.IsFinite(ship.InitialState.Velocity.Length));
            Assert.True(ship.InitialState.Velocity.Length < 5e5, $"{ship.Callsign} launched at {ship.InitialState.Velocity.Length:E1} m/s.");
        }
    }
}
