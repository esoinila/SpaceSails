using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #653 slice 2 · THE ROUTE TABLE, PINNED AGAINST THE QUESTIONS IT REPLACED. The client used to route by body id with
/// <c>Derelict.TryParseWreckId</c> / <c>StationAboard.TryParseStationId</c> and a floor test, in an order; the table
/// must give the same answer for every id and floor those chains were ever asked, edge ids included.
/// </summary>
public sealed class SiteRouteTests
{
    public static readonly string[] Ids =
    [
        "luna", "phobos", "the-clinker", "secret-lab-site", "dev-station-6",
        "wreck-", "wreck-a", "wreck-cold-harvest", "station-", "station-dev-station-6", "station-x",
        "xwreck-a", "xstation-a", "Wreck-a", "STATION-a", "", "roadster",
    ];

    public static readonly int[] Floors = [-3, -1, 0, 1];

    /// <summary>The routing exactly as the frame's deck builder asked it before the table: a floor below the surface
    /// first, then the station, then the wreck, else the ground.</summary>
    private static SiteRoute.Kind TheOldChain(string id, int floor)
    {
        if (floor < 0)
        {
            return SiteRoute.Kind.Hive;
        }

        if (StationAboard.TryParseStationId(id, out _))
        {
            return SiteRoute.Kind.Station;
        }

        return Derelict.TryParseWreckId(id, out _) ? SiteRoute.Kind.Wreck : SiteRoute.Kind.Ground;
    }

    [Fact]
    public void TheTableAnswersEveryIdAndFloorTheOldChainWas()
    {
        foreach (string id in Ids)
        {
            foreach (int floor in Floors)
            {
                Assert.Equal(TheOldChain(id, floor), SiteRoute.Of(id, floor));
            }

            Assert.Equal(StationAboard.TryParseStationId(id, out _), SiteRoute.IsStation(id));
            Assert.Equal(Derelict.TryParseWreckId(id, out _), SiteRoute.IsWreck(id));
            Assert.Equal(
                Derelict.TryParseWreckId(id, out _) || StationAboard.TryParseStationId(id, out _), SiteRoute.IsHull(id));
            Assert.Equal(
                StationAboard.TryParseStationId(id, out string expected) ? expected : null, SiteRoute.StationIdOf(id));
        }
    }

    [Fact]
    public void ANullBodyIsAGroundAndAHullIsNeverBoth()
    {
        Assert.Equal(SiteRoute.Kind.Ground, SiteRoute.Of(null, 0));
        Assert.False(SiteRoute.IsHull(null));
        Assert.Null(SiteRoute.StationIdOf(null));

        foreach (string id in Ids)
        {
            Assert.False(SiteRoute.IsStation(id) && SiteRoute.IsWreck(id), id);
        }
    }

    [Fact]
    public void TheRealPrefixesRouteAndTheBarePrefixesDoNot()
    {
        Assert.Equal(SiteRoute.Kind.Station, SiteRoute.Of(StationAboard.BodyIdFor("dev-station-6"), 0));
        Assert.Equal(SiteRoute.Kind.Wreck, SiteRoute.Of(Derelict.BodyIdFor("cold-harvest"), 0));
        Assert.Equal(SiteRoute.Kind.Ground, SiteRoute.Of(StationAboard.BodyIdPrefix, 0));
        Assert.Equal(SiteRoute.Kind.Ground, SiteRoute.Of(Derelict.BodyIdPrefix, 0));
        Assert.Equal(SiteRoute.Kind.Hive, SiteRoute.Of(StationAboard.BodyIdFor("dev-station-6"), -1));
    }
}
