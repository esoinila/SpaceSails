using System;
using System.Threading.Tasks;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U04-F09 (Fable ruling 2026-11-07) · ARRIVAL, THEN THE DOOR, THEN ANYTHING ELSE; NOTHING STACKS. The collector's
/// demand and the death cards wear the busted backdrop, so they are on the scrim census; the arbiter's line is a
/// FIFO of three whose head alone is released; and a DEATH never waits in it.
/// </summary>
[SlowGate]
public sealed class TheNothingStacksOnArrivalTests
{
    private const string SatAtABarTop = "/map?barcase=1";

    private static Task<DeskBench> Boot() => DeskBench.BootAsync(SatAtABarTop);

    private static bool Raised(DeskBench bench, string tag, Func<bool>? wanted = null)
    {
        Action raise = () => RaisedLog.Add(tag);
        return (bool)bench.Call("RaiseAScrimCard", raise, wanted ?? (() => true), null)!;
    }

    private static readonly System.Collections.Generic.List<string> RaisedLog = [];

    [Fact]
    public async Task ACardHeldBehindTheCollectorsDemandWaitsForItAndThenArrives()
    {
        using DeskBench bench = await Boot();
        RaisedLog.Clear();
        await bench.RenderAsync();
        bench.CallOnTheDispatcher("SheGoesWithHim");   // a death/demand card: _busted is up, wearing the busted backdrop
        Assert.True(bench.Field("_busted") is not null);

        Assert.False(Raised(bench, "door"), "the door-keeper's card went up over the busted card - two dims stacked.");
        Assert.Empty(RaisedLog);
        Assert.True(bench.Field("_scrimQueued") is not null);

        bench.Poke("_busted", null);
        bench.CallOnTheDispatcher("PumpTheScrimQueue");
        Assert.Equal(["door"], RaisedLog);
    }

    [Fact]
    public async Task TheLineIsAFifoOfThreeAndReleasesOnlyTheHeadPerClearGlass()
    {
        using DeskBench bench = await Boot();
        RaisedLog.Clear();
        bench.CallOnTheDispatcher("OpenGalleyCard");
        foreach (string tag in new[] { "a", "b", "c" })
        {
            Assert.False(Raised(bench, tag));
        }

        bench.CallOnTheDispatcher("CloseGalleyCard");
        bench.CallOnTheDispatcher("PumpTheScrimQueue");
        Assert.Equal(["a"], RaisedLog);   // the head alone; the raises here are bare, so the glass stays clear...
        bench.CallOnTheDispatcher("PumpTheScrimQueue");
        bench.CallOnTheDispatcher("PumpTheScrimQueue");
        Assert.Equal(["a", "b", "c"], RaisedLog);   // ...and one more per pump, in the order they arrived
        Assert.True(bench.Field("_scrimQueued") is null);
    }

    [Fact]
    public async Task HeadOnlyIsReleasedWhileTheGlassStaysBusy()
    {
        using DeskBench bench = await Boot();
        RaisedLog.Clear();
        bench.CallOnTheDispatcher("OpenGalleyCard");
        Raised(bench, "a");
        Raised(bench, "b");
        Raised(bench, "c");
        Raised(bench, "d");   // a fourth: the oldest goes

        bench.CallOnTheDispatcher("CloseGalleyCard");
        bench.CallOnTheDispatcher("PumpTheScrimQueue");
        // The head went up and nothing else did: "b" is first now ("a" was evicted).
        Assert.Equal(["b"], RaisedLog);
        Assert.True(bench.Field("_scrimQueued") is not null, "the other two were released together, or dropped.");
    }

    [Fact]
    public async Task ADeathNeverWaitsInTheQueueBehindAnotherCard()
    {
        using DeskBench bench = await Boot();
        await bench.RenderAsync();
        bench.CallOnTheDispatcher("OpenGalleyCard");
        Assert.True((bool)bench.Field("_galleyCardOpen")!);

        bench.CallOnTheDispatcher("SheGoesWithHim");
        Assert.True(bench.Field("_busted") is not null,
            "the death card was held behind the galley card - a death must raise the instant it happens.");
        Assert.True(bench.Field("_scrimQueued") is null, "a death was put in the line.");
    }

    /// <summary>The collectors' demand on foot and the door-keeper's card both go up through the arbiter; the
    /// death triggers do not (a death waits behind nothing).</summary>
    [Fact]
    public void TheDemandAndTheDoorGoThroughTheArbiterAndTheDeathsDoNot()
    {
        string pages = System.IO.Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages");
        string Read(string f) => System.IO.File.ReadAllText(System.IO.Path.Combine(pages, f));
        string repo = Read("Map.Surface.RepoBoat.cs");
        string writ = repo[repo.IndexOf("private void TheWritIsServed(", StringComparison.Ordinal)..repo.IndexOf("private void TheWritIsServedNow(", StringComparison.Ordinal)];
        Assert.Contains("RaiseAScrimCard(", writ, StringComparison.Ordinal);
        Assert.Contains("RaiseAScrimCard(", Read("Map.GateGuard.Card.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("RaiseAScrimCard(", Read("Map.ShipScuttleBoard.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("RaiseAScrimCard(", Read("Map.Combat.Busted.cs"), StringComparison.Ordinal);
    }
}
