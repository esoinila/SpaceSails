using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-3, de-Earth-centering (vision par. 8): scenario-driven traffic, moons as real bodies,
/// stations/havens at the outer reaches, and secretive He3 haulers that never hit the board.
/// </summary>
public class OuterReachesTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    private static ScenarioDefinition LoadWheel() =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "wheel.json"));

    [Fact]
    public void ScenarioDrivenTraffic_IsDeterministic_AndProducesSecretiveHaulers()
    {
        var ephemeris = Sol();

        IReadOnlyList<NpcShip> first = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);
        IReadOnlyList<NpcShip> second = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Id, second[i].Id);
            Assert.Equal(first[i].OriginId, second[i].OriginId);
            Assert.Equal(first[i].DestinationId, second[i].DestinationId);
            Assert.Equal(first[i].PublishesTimetable, second[i].PublishesTimetable);
            Assert.Equal(first[i].InitialState, second[i].InitialState); // bit-identical
        }

        // Titan's He3 haulers (worldbuilding notes §4) keep their timetable to themselves.
        Assert.Contains(first, s => !s.PublishesTimetable && s.OriginId == "titan");
        Assert.Contains(first, s => s.PublishesTimetable); // central-space routes still publish
    }

    [Fact]
    public void ScenarioWithoutTrafficSection_FallsBackToFixedTables()
    {
        ScenarioDefinition wheel = LoadWheel();
        Assert.Null(wheel.Traffic);

        var ephemeris = CircularOrbitEphemeris.FromScenario(wheel);
        Assert.Null(ephemeris.Traffic);

        IReadOnlyList<NpcShip> ships = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(ephemeris, seed: 43, count: 3);

        // No traffic section anywhere in the Wheel scenario: every ship falls back to the
        // original hardcoded tables, so everyone still publishes a timetable.
        Assert.All(ships, s => Assert.True(s.PublishesTimetable));
        Assert.All(pods, p => Assert.Equal("luna", p.OriginId));
    }

    [Fact]
    public void UnpublishedShips_ExcludedFromBoardListing_ButStillSensorObservable()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> ships = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);

        List<NpcShip> unpublished = [.. ships.Where(s => !s.PublishesTimetable)];
        Assert.NotEmpty(unpublished);

        // The traffic-board panel's filter predicate (Map.razor): unpublished ships never qualify.
        Assert.All(unpublished, s => Assert.False(BoardListingPredicate(s)));
        Assert.All(ships.Where(s => s.PublishesTimetable), s => Assert.True(BoardListingPredicate(s)));

        // But a sensor sweep doesn't care about the board flag: an unpublished ship in range is
        // observed exactly like any other (worldbuilding notes §4/§5 — you have to go looking).
        NpcShip target = unpublished[0];
        Vector2d observer = target.InitialState.Position + new Vector2d(1e9, 0);
        bool observed = SensorModel.Default.TryObserve(
            observer, target.Id, target.InitialState, target.InitialState.SimTime, out Observation obs);

        Assert.True(observed);
        Assert.Equal(target.Id, obs.TargetId);
    }

    private static bool BoardListingPredicate(NpcShip ship) => ship.PublishesTimetable;

    [Fact]
    public void Moons_OrbitTheirParentPlanet_NotTheSun()
    {
        var ephemeris = Sol();

        foreach (double t in new[] { 0.0, 3e5, 1.5e7 })
        {
            Vector2d saturn = ephemeris.Position("saturn", t);
            Vector2d titan = ephemeris.Position("titan", t);
            Assert.InRange((titan - saturn).Length, 1.22183e9 * 0.999, 1.22183e9 * 1.001);

            Vector2d jupiter = ephemeris.Position("jupiter", t);
            Vector2d europa = ephemeris.Position("europa", t);
            Assert.InRange((europa - jupiter).Length, 6.709e8 * 0.999, 6.709e8 * 1.001);
        }
    }

    [Fact]
    public void Depots_ExistAtStationsAndHavens_ButNotOrdinaryMoons()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(ephemeris, seed: 44);

        Assert.Contains(depots, d => d.DepotBodyId == "mercury-compute");
        Assert.Contains(depots, d => d.DepotBodyId == "satellite-factory");
        Assert.Contains(depots, d => d.DepotBodyId == "ringside-exchange"); // station + haven
        Assert.Contains(depots, d => d.DepotBodyId == "enceladus");         // haven moon

        // Titan, Europa, Ganymede, Callisto and Luna are ordinary moons: no depot of their own,
        // they share their planet's.
        foreach (string moon in new[] { "titan", "europa", "ganymede", "callisto", "luna" })
        {
            Assert.DoesNotContain(depots, d => d.DepotBodyId == moon);
        }
    }
}
