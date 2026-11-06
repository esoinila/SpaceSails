using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>Fable's 2026-11-06 lines, the page-side half: #1387's skip label, #1380's greyed Board row, #1439's headings.</summary>
public sealed class TheFableLines1106ClientTests
{
    [Fact]
    public async Task ACoastSkipIsLabelledAsACoastAlongTheOrbit()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt");
        Assert.Equal("the end of the coast along this orbit", bench.Call("SkipEventLabel", WarpSkip.EventKind.PlanEnd));
        Assert.Equal("the next burn", bench.Call("SkipEventLabel", WarpSkip.EventKind.Burn));
    }

    [Fact]
    public async Task TheRoleIdLedgerHeadingsAreAuthoredNotTitleCased()
    {
        MethodInfo give = typeof(Map).GetMethod("GiverDisplay", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.Equal("The berth-holder", give.Invoke(null, ["kaamos-holder"]));
        Assert.Equal("The Nebula adjuster", give.Invoke(null, ["nebula-adjuster"]));
        Assert.Equal("Some-Body Else", give.Invoke(null, ["some-body else"]));
        await Task.CompletedTask;
    }

    private static async Task<DeskBench.Painted.Node> TheBoardRow(string url, string ground)
    {
        DeskBench bench = await DeskBench.BootAsync(url);
        bench.Poke("_deckMode", true);
        bench.Poke("_shuttleBayStops", bench.Call("ShuttleDestinationsInRange"));
        bench.Poke("_shuttleBayFarStops", bench.Call("NearestOutOfReachLandables", 3));
        DeskBench.Painted painted = await bench.RenderAsync();
        return painted.Root.Descendants()
            .Where(n => n.HasClass("deck-shuttle-row") && !n.HasClass("deck-shuttle-row-far"))
            .FirstOrDefault(n => n.Spoken.Contains(ground, StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException($"the board has no row for {ground}");
    }

    [Fact]
    public async Task AGroundTheBoatCannotFlyBackFromIsListedGreyedAndReasoned()
    {
        // The Tilt -> Miranda at the boot epoch: the ship is close, and parting faster than the boat can fly.
        DeskBench.Painted.Node row = await TheBoardRow("/map?dock=the-tilt", "Miranda");
        DeskBench.Painted.Node board = row.Descendants().First(n => n.Element == "button" && n.Spoken.Contains("Board for", StringComparison.Ordinal));
        Assert.True(board.Attributes.ContainsKey("disabled"), "the unreturnable ground's Board button must be disabled");
        Assert.Equal(ShuttleLink.OneWayDownLine, board.Attributes["title"]);
        DeskBench.Painted.Node why = row.Descendants().First(n => n.HasClass("deck-shuttle-oneway"));
        Assert.Equal(ShuttleLink.OneWayDownFace, why.Spoken);
    }

    [Fact]
    public async Task ACatchableGroundCanStillBeBoarded()
    {
        DeskBench.Painted.Node row = await TheBoardRow("/map?dock=the-tilt&simhours=20", "Miranda");
        DeskBench.Painted.Node board = row.Descendants().First(n => n.Element == "button" && n.Spoken.Contains("Board for", StringComparison.Ordinal));
        Assert.False(board.Attributes.ContainsKey("disabled") && board.Attributes["disabled"] is not null && board.Attributes["disabled"] != "False");
        Assert.DoesNotContain(row.Descendants(), n => n.HasClass("deck-shuttle-oneway"));
    }

    [Fact]
    public async Task ALongUndockedCoastSaysItArrivesNowhereExactlyOnce()
    {
        // An undocked ship with a plan ending 22 days out: the jump-scale branch takes this skip, and the
        // note must have been said before it did.
        using DeskBench bench = await DeskBench.BootAsync("/map?start=wreck");
        double now = (double)bench.Peek("SimTime")!;
        bench.Poke("_destinationPass", (ClosestApproach.Pass?)new ClosestApproach.Pass(
            "mars", "Mars", 3.4e6, 1e9, now + 22 * 86_400.0, default));
        Assert.Null(bench.Peek("_dockedHavenId"));

        await bench.RenderAsync();
        await (Task)bench.CallOnTheDispatcher("StartSkip")!;

        var events = (System.Collections.Generic.List<(double SimTime, string Text, bool Story)>)bench.Peek("_autopilotEvents")!;
        Assert.Equal(1, events.Count(e => e.Text.StartsWith("⏭ A coast, not a crossing", StringComparison.Ordinal)));
    }
}
