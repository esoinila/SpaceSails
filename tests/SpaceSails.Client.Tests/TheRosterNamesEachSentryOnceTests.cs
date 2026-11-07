using System.Collections;
using System.Reflection;
using SpaceSails.Client.Pages;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>R1-F46 · The sentry roster is two named units. Bringing a carried sentry home must never list a
/// unit twice ("K-77 and R-3B and R-3B ride escort" after K-77 was written off).</summary>
public class TheRosterNamesEachSentryOnceTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    [Fact]
    public void ABotComingHomeTwice_StillStandsInTheRosterOnce_AndTheMusterLineNamesItOnce()
    {
        var map = new Map();
        var roster = (IList)typeof(Map).GetField("_shipBots", Hidden)!.GetValue(map)!;
        roster.Clear();                                  // K-77 written off, R-3B is all that is left aboard
        MethodInfo home = typeof(Map).GetMethod("ReturnSentryToTheRoster", Hidden)!;

        home.Invoke(map, ["R-3B", 40]);
        home.Invoke(map, ["R-3B", 12]);

        Assert.Single(roster);
        string muster = (string)typeof(Map).GetProperty("BotMusterLine", Hidden)!.GetValue(map)!;
        Assert.Equal("R-3B rides escort by default", muster);
    }
}
