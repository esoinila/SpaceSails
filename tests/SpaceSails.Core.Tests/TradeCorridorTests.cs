namespace SpaceSails.Core.Tests;

public class TradeCorridorTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    [Fact]
    public void Regions_OnePerPresentAnchorPair()
    {
        CircularOrbitEphemeris sol = Sol();
        int anchorsPresent = TradeCorridors.TradeAnchors.Count(id => sol.Bodies.Any(b => b.Id == id));

        IReadOnlyList<CorridorRegion> regions = TradeCorridors.Regions(sol, simTime: 0);

        Assert.Equal(anchorsPresent * (anchorsPresent - 1) / 2, regions.Count);
        Assert.All(regions, r => Assert.True(r.Radius >= TradeCorridors.MinLaneRadiusMeters));
    }

    [Fact]
    public void Regions_EndpointsSitOnTheBodies_AtTheQueriedTime()
    {
        CircularOrbitEphemeris sol = Sol();
        double t = 40 * 86400.0;

        CorridorRegion earthMars = TradeCorridors.Regions(sol, t)
            .Single(r => r is { AId: "earth", BId: "mars" } or { AId: "mars", BId: "earth" });

        Vector2d earth = sol.Position("earth", t);
        Assert.True((earthMars.A - earth).Length < 1 || (earthMars.B - earth).Length < 1);
    }

    [Fact]
    public void DistanceTo_AndContains_MeasureFromTheSegment()
    {
        var lane = new CorridorRegion("a", "b", "A–B",
            new Vector2d(0, 0), new Vector2d(1e11, 0), Radius: 1e10);

        Assert.Equal(0, lane.DistanceTo(new Vector2d(5e10, 0)), 3);
        Assert.Equal(5e9, lane.DistanceTo(new Vector2d(5e10, 5e9)), 3);
        Assert.True(lane.Contains(new Vector2d(5e10, 9e9)));
        Assert.False(lane.Contains(new Vector2d(5e10, 1.1e10)));
        // beyond the endpoint: distance measured to the endpoint, not the infinite line
        Assert.Equal(1e10, lane.DistanceTo(new Vector2d(1.1e11, 0)), 3);
    }

    [Fact]
    public void TryNearest_PicksTheClosestLane()
    {
        CircularOrbitEphemeris sol = Sol();
        IReadOnlyList<CorridorRegion> regions = TradeCorridors.Regions(sol, 0);
        CorridorRegion earthMars = regions
            .Single(r => r is { AId: "earth", BId: "mars" } or { AId: "mars", BId: "earth" });
        Vector2d nearTheLane = earthMars.Midpoint + new Vector2d(0, earthMars.Radius / 2);

        Assert.True(TradeCorridors.TryNearest(regions, nearTheLane, out CorridorRegion nearest, out double distance));

        Assert.Equal(earthMars.PairName, nearest.PairName);
        Assert.True(distance <= earthMars.Radius);
    }

    [Fact]
    public void SweepJobFor_WedgeCoversBothAnchors_FromTheShipsVantage()
    {
        CircularOrbitEphemeris sol = Sol();
        CorridorRegion lane = TradeCorridors.Regions(sol, 0)
            .Single(r => r is { AId: "earth", BId: "mars" } or { AId: "mars", BId: "earth" });
        var ship = new Vector2d(-2e11, -2e11); // well off the lane

        ScanJob job = TradeCorridors.SweepJobFor(lane, ship);

        double bearingA = TrackingStation.Bearing(lane.A - ship);
        double bearingB = TrackingStation.Bearing(lane.B - ship);
        Assert.True(TrackingStation.InArc(bearingA, job.CenterBearingRad, job.ArcWidthRad));
        Assert.True(TrackingStation.InArc(bearingB, job.CenterBearingRad, job.ArcWidthRad));
    }

    [Fact]
    public void SweepJobFor_MatchesTheCorridorWatchProgram_ForTheSamePair()
    {
        CircularOrbitEphemeris sol = Sol();
        var ship = new Vector2d(-2e11, -2e11);
        CorridorRegion lane = TradeCorridors.Regions(sol, 0)
            .Single(r => r is { AId: "earth", BId: "mars" } or { AId: "mars", BId: "earth" });

        ScanJob fromLane = TradeCorridors.SweepJobFor(lane, ship);
        ScanProgram program = ScanPrograms.BuildPrograms(sol, ship, 0)
            .Single(p => p.Name.Contains(lane.PairName));

        Assert.Equal(program.Job, fromLane);
    }
}
