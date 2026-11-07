using System;
using System.Collections;
using System.Threading.Tasks;
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

    /// <summary>#1469 · The helper test above calls <c>ReturnSentryToTheRoster</c> directly, so it would not catch
    /// liftoff bypassing it. This one lifts off for real: a carried R-3B comes home while an R-3B already stands in
    /// the roster. RED: make <c>LiftOffFromSurface</c> add the carried bot with <c>_shipBots.Add</c> instead.</summary>
    [Fact]
    public async Task ACarriedSentryLiftingOffForRealStillStandsInTheRosterOnce()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);
        ADeadCaptainDoesNotBoardTests.BringHerAlongside(bench);
        var roster = (IList)bench.Peek("_shipBots")!;
        roster.Clear();
        Type shipBot = typeof(Map).GetNestedType("ShipBot")!;
        roster.Add(Activator.CreateInstance(shipBot, "R-3B", 40)!);

        object ex = bench.Peek("_surface")!;
        var carried = (IList)ex.GetType().GetProperty("Bots")!.GetValue(ex)!;
        carried.Clear();
        Type surfaceBot = typeof(Map).GetNestedType("SurfaceBot")!;
        object bot = Activator.CreateInstance(surfaceBot)!;
        surfaceBot.GetProperty("Unit")!.SetValue(bot, "R-3B");
        surfaceBot.GetProperty("Rounds")!.SetValue(bot, 12);
        surfaceBot.GetProperty("Deployed")!.SetValue(bot, false);
        carried.Add(bot);

        bench.CallOnTheDispatcher("LiftOffFromSurface");

        Assert.False(bench.OnSurface, "the bench never lifted off, so the roster proves nothing.");
        Assert.Single(roster);
        Assert.Equal("R-3B rides escort by default", (string)bench.Call("get_BotMusterLine")!);
    }
}
