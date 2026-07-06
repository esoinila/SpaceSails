namespace SpaceSails.Core.Tests;

public class EphemerisTests
{
    [Fact]
    public void SolScenario_LoadsAllBodies()
    {
        var scenario = SimulatorTests.LoadSol();

        Assert.Equal("Sol", scenario.Name);
        // Sun + 8 planets + Luna (M6) + outer moons, stations and havens (PR-3, vision par. 8):
        // Mercury Compute Farms, Highport Satellite Works, Europa/Ganymede/Callisto, Titan,
        // Enceladus, Ringside Exchange, plus the inner grey-market docks Cinder Roost (Venus),
        // The Space Bar (Mars) and The Tilt (Uranus).
        Assert.Equal(21, scenario.Bodies.Count);
        Assert.Contains(scenario.Bodies, b => b.Id == "saturn");
        Assert.Contains(scenario.Bodies, b => b.Id == "luna" && b.ParentId == "earth");
        Assert.Contains(scenario.Bodies, b => b.Id == "titan" && b.ParentId == "saturn" && b.Kind == "moon");
        Assert.Contains(scenario.Bodies, b => b.Id == "mercury-compute" && b.Kind == "station");
        Assert.Contains(scenario.Bodies, b => b.Id == "enceladus" && b.Haven);
    }

    [Fact]
    public void Earth_ReturnsToStart_AfterOnePeriod()
    {
        var ephemeris = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        double period = ephemeris.Bodies.Single(b => b.Id == "earth").OrbitPeriod;

        Vector2d start = ephemeris.Position("earth", 0);
        Vector2d afterOneYear = ephemeris.Position("earth", period);

        Assert.True((afterOneYear - start).Length < 1.0, "Earth must return to its epoch position after one period.");
    }

    [Fact]
    public void ParentChaining_OffsetsChildByParentPosition()
    {
        // A moon on a rotating parent — the mechanism the Wheel of the World scenario is built on.
        var ephemeris = new CircularOrbitEphemeris([
            new CelestialBody("center", "Center", null, 0, 0, 0, 0, 0),
            new CelestialBody("planet", "Planet", "center", 0, 0, OrbitRadius: 1000, OrbitPeriod: 400, InitialPhase: 0),
            new CelestialBody("moon", "Moon", "planet", 0, 0, OrbitRadius: 10, OrbitPeriod: 40, InitialPhase: 0),
        ]);

        // At t = 100 (quarter parent orbit) the planet is at (0, 1000); the moon has done
        // 2.5 of its own orbits, putting it at planet + (-10, 0).
        Vector2d moon = ephemeris.Position("moon", 100);

        Assert.Equal(-10, moon.X, precision: 6);
        Assert.Equal(1000, moon.Y, precision: 6);
    }

    [Fact]
    public void UnknownParent_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CircularOrbitEphemeris([
            new CelestialBody("orphan", "Orphan", "ghost", 0, 0, 1, 1, 0),
        ]));
    }
}
