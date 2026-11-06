using System;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Round 2 · U07 (Paavo) and U08 (Ash) · the boot-level claims of the findings fixed in the page itself. Each one boots
/// the real dev URL through <see cref="DeskBench"/> and reads what the page did, not what a helper says it would do.
/// </summary>
public sealed class RoundTwoU07U08Tests
{
    /// <summary>U07-F08 · the bar-case start stands the room's visitors off (Ilse Varga sat at the top seconds after
    /// boot and "Work the case" answered "Not with somebody sitting there"); an explicit lever still wins.</summary>
    [Theory]
    [InlineData("/map?barcase=1", false)]
    [InlineData("/map?barcase=1&finder=1", true)]
    [InlineData("/map?finder=1&barcase=1", true)]
    public async Task TheBarCaseStandsTheFinderOffUnlessAskedOtherwise(string url, bool finderAllowed)
    {
        using DeskBench bench = await DeskBench.BootAsync(url);
        bool? cheat = (bool?)bench.Peek("_finderCheat");
        Assert.Equal(finderAllowed, cheat == true);
        if (!finderAllowed)
        {
            Assert.False(cheat ?? true, "the bar-case boot left Ilse Varga free to cross the floor.");
            Assert.False((bool?)bench.Peek("_walkInCheat") ?? true);
            Assert.False((bool?)bench.Peek("_approachCheat") ?? true);
        }
    }

    /// <summary>U07-F09 · a seat-cheat boot has already been told the venue's rooms: no THE HALL / THE CABINET picture
    /// card stands between the tester and the seat under test.</summary>
    [Theory]
    [InlineData("/map?dock=selene-gate&spread=1")]
    [InlineData("/map?dock=selene-gate&rip=1")]
    [InlineData("/map?dock=selene-gate&threads=1")]
    public async Task ASeatCheatBootsWithTheVenuesPlaceCardsSpent(string url)
    {
        using DeskBench bench = await DeskBench.BootAsync(url);
        object ex = bench.Peek("_surface") ?? throw new Xunit.Sdk.XunitException($"{url} never reached the ground.");
        Type t = ex.GetType();
        Assert.True((bool)t.GetProperty("HiveCantinaHallShown")!.GetValue(ex)!, "THE HALL card is still due.");
        Assert.True((bool)t.GetProperty("HiveCabinetShown")!.GetValue(ex)!, "THE CABINET card is still due.");
    }

    /// <summary>U08-F04 · a staged ground death owns the screen: the first-ground lesson is not up over the death
    /// card, and the landing's pulses were dropped before it.</summary>
    [Fact]
    public async Task AStagedGroundDeathHasNoLessonOverIt()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?death=suffocated&dock=the-tilt&land=1");
        Assert.NotNull(bench.Peek("_busted"));
        Assert.False((bool)bench.Peek("_groundLessonOpen")!, "the first-ground lesson is up over the death card.");
    }

    /// <summary>U08-F07 · the staged collector catch is at the top of the ladder, so RESIST is the Bolivia (it was
    /// heat 2: one fixed roll, won on every boot).</summary>
    [Fact]
    public async Task TheStagedCollectorCatchResistsIntoTheBolivia()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?death=collector&dock=selene-gate");
        object busted = bench.Peek("_busted") ?? throw new Xunit.Sdk.XunitException("no catch was staged.");
        Assert.Equal(EncounterRule.MaxHeatLevel, (int)busted.GetType().GetProperty("Heat")!.GetValue(busted)!);
        await bench.RenderAsync();   // the page needs its render handle before a press can StateHasChanged
        bench.CallOnTheDispatcher("BustedResist");
        Assert.Equal("Bolivia", busted.GetType().GetProperty("Phase")!.GetValue(busted)!.ToString());
    }

    /// <summary>U08-F08 · the void death is never staged on a clamped ship.</summary>
    [Fact]
    public async Task TheVoidDeathIsNotStagedOnAClampedShip()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?death=void");
        Assert.NotNull(bench.Peek("_busted"));
        Assert.False(bench.Docked, "the void card stands over a HUD that says Docked.");
    }

    /// <summary>U08-F10 · a dead station has nobody to look up with: no crowd line is spoken there.</summary>
    [Fact]
    public async Task ADeadStationSaysNoCrowdShudderLine()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&station=1&land=1");
        Assert.True((bool)bench.Call("get_OnStation")!, "this boot never reached the station.");
        string[] crowd = [.. HullShudder.LinesFor(HullShudder.Setting.DeepSite, HullShudder.HavenRoom.Concourse)];
        bench.CallOnTheDispatcher("FireShudder", 0.0);
        Assert.DoesNotContain(crowd, line => bench.Pulse.Contains(line[..40], StringComparison.Ordinal));
        Assert.DoesNotContain("everyone", bench.Pulse, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>U08-F17 · on her own deck, clamped at a haven, the captain hears the SHIP pool, not the concourse's.</summary>
    [Fact]
    public async Task OnHerOwnDeckWhileDockedTheShudderIsTheShipsOwn()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=selene-gate");
        Assert.True(bench.Docked);
        Assert.False((bool)bench.Peek("_ashore")!, "the captain is not on her own deck in this boot.");
        string[] haven = [.. HullShudder.EveryHavenLine()];
        for (int i = 0; i < 3; i++)
        {
            bench.CallOnTheDispatcher("FireShudder", 0.0);
            Assert.DoesNotContain(haven, line => bench.Pulse.Contains(line[..40], StringComparison.Ordinal));
        }
    }
}
