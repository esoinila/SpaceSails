using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using SpaceSails.Client.Rendering;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #612 · the air-supply crossing is said only where the tank starts or stops and never on the first tick of an
/// excursion. A death or fold-away ends an excursion without a liftoff, so the next boarding must forget the last
/// supply it noted, and the first tick must measure speed from where the captain stands.
/// </summary>
public sealed class TheAirCrossingIsNeverInheritedTests
{
    private const string Url = "/map?dock=the-tilt&station=1&land=1";

    [Fact]
    public async Task ABoardingForgetsTheSupplyTheLastExcursionNoted()
    {
        DeskBench bench = await DeskBench.BootAsync(Url);
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);

        bench.Poke("_airSupplyNoted", (SuitAir.Supply?)SuitAir.Supply.Tanks);   // the last life ended drawing
        DeckPlan.ConsoleSpot home = Assert.Single(
            ((DeckPlan)bench.Peek("_deckPlan")!).Consoles, c => c.Kind == DeckPlan.ConsoleKind.ShuttleAirlock);
        bench.Poke("_avatarX", (double)home.X);
        bench.Poke("_avatarY", (double)home.Y);
        bench.CallOnTheDispatcher("InteractAtConsole");
        Assert.False(bench.OnSurface);
        bench.Poke("_airSupplyNoted", (SuitAir.Supply?)SuitAir.Supply.Tanks);

        object stop = ((System.Collections.IEnumerable)bench.Call("ShuttleDestinationsInRange")!).Cast<object>()
            .Single(s => StationAboard.TryParseStationId(
                (string)s.GetType().GetProperty("Body")!.GetValue(s)!.GetType().GetProperty("Id")!
                    .GetValue(s.GetType().GetProperty("Body")!.GetValue(s))!, out _));
        await (Task)bench.CallOnTheDispatcher("BeginSurfaceExcursion", stop, ShuttleExcursion.Pack(0, 0, []), 0, null)!;
        Assert.True(bench.OnSurface);

        Assert.Null(bench.Peek("_airSupplyNoted"));
    }

    [Fact]
    public async Task TheFirstTickOfAnExcursionMeasuresFromWhereTheCaptainStands()
    {
        DeskBench bench = await DeskBench.BootAsync(Url);
        bench.Poke("_airSupplyNoted", null);
        bench.Poke("_airLastX", 9999.0);
        bench.Poke("_airLastY", -9999.0);
        bench.Poke("_avatarX", 12.0);
        bench.Poke("_avatarY", 34.0);

        bench.CallOnTheDispatcher("AnnounceAirSupply", SuitAir.Supply.Tanks, false);

        Assert.Equal(12.0, (double)bench.Peek("_airLastX")!);
        Assert.Equal(34.0, (double)bench.Peek("_airLastY")!);
    }
}
