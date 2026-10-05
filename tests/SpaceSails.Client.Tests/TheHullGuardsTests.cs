using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #653 slice 2 · THE "NO REGOLITH HERE" GUARDS, PINNED THROUGH THE ROUTE TABLE'S CONSUMERS. A derelict and a dead
/// station are HULLS: no tide claws out of them, they show no tube-mouth beacon and no shelter, they hide no
/// clandestine lab, and the boarding card does not offer a regolith landing. Five guards used to each spell out
/// "wreck or station" for themselves; they now ask <see cref="SiteRoute.IsHull"/>. Each is asked here of the page that
/// shipped it, on a wreck, on a station and — as the control that lets the guard FAIL — on a real moon, where the
/// opposite answer is the whole point.
/// </summary>
public sealed class TheHullGuardsTests
{
    private const string Wreck = "/map?dock=the-tilt&wreck=drivefailure&land=1";
    private const string Station = "/map?dock=the-tilt&station=1&land=1";
    private const string Moon = "/map?dock=the-tilt&site=0&land=1";

    public static TheoryData<string> Hulls => new() { Wreck, Station };

    private static async Task<DeskBench> BootAsync(string url)
    {
        DeskBench bench = await DeskBench.BootAsync(url);
        await bench.RenderAsync();
        Assert.True(bench.OnSurface, $"{url} did not put the away team on any ground at all.");
        return bench;
    }

    private static string BodyId(object ex)
    {
        object stop = ex.GetType().GetProperty("Stop")!.GetValue(ex)!;
        object body = stop.GetType().GetProperty("Body")!.GetValue(stop)!;
        return (string)body.GetType().GetProperty("Id")!.GetValue(body)!;
    }

    [Theory]
    [MemberData(nameof(Hulls))]
    public async Task ARouteThatIsAHullRunsNoTideAndShowsNoBeaconAndHidesNoLab(string url)
    {
        DeskBench bench = await BootAsync(url);
        object ex = bench.Peek("_surface")!;
        Assert.True(SiteRoute.IsHull(BodyId(ex)));

        // The tide: stepped a long while, the clock it accrues on never runs.
        for (int i = 0; i < 50; i++)
        {
            bench.CallOnTheDispatcher("StepTide", 0.1);
        }

        Assert.Equal(0.0, (double)ex.GetType().GetProperty("TideSeconds")!.GetValue(ex)!);

        // The beacons: a hull has neither a tube mouth nor a shelter to point at.
        Assert.Empty((IEnumerable)bench.Call("BuildBeacons", ex)!);

        // The lab: even forced onto this very body, a hull hides none.
        bench.Poke("_secretLabForceBodyId", BodyId(ex));
        bench.CallOnTheDispatcher("ResolveSecretLab", ex);
        Assert.Null(ex.GetType().GetProperty("Lab")!.GetValue(ex));

        // The shelters: not on any tile near the captain.
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                var shelters = (IEnumerable)bench.Call("SheltersOnTile", ex, new SurfaceTiles.Address(x, y))!;
                Assert.Empty(shelters);
            }
        }
    }

    [Fact]
    public async Task AMoonIsTheControl_ItRunsATideShowsABeaconAndTakesALab()
    {
        DeskBench bench = await BootAsync(Moon);
        object ex = bench.Peek("_surface")!;
        Assert.False(SiteRoute.IsHull(BodyId(ex)));

        for (int i = 0; i < 50; i++)
        {
            bench.CallOnTheDispatcher("StepTide", 0.1);
        }

        // (the clock may have spawned its claw-outs and wrapped, so the proof is that it MOVED or spawned)
        double clock = (double)ex.GetType().GetProperty("TideSeconds")!.GetValue(ex)!;
        int spawned = (int)ex.GetType().GetProperty("TideSpawnIndex")!.GetValue(ex)!;
        Assert.True(clock > 0.0 || spawned > 0, "a moon's tide never ran — the hull guard would pass anything.");

        Assert.NotEmpty((IEnumerable)bench.Call("BuildBeacons", ex)!);

        bench.Poke("_secretLabForceBodyId", BodyId(ex));
        bench.CallOnTheDispatcher("ResolveSecretLab", ex);
        Assert.NotNull(ex.GetType().GetProperty("Lab")!.GetValue(ex));

        // The shelters: the control that lets the hull's emptiness FAIL. On the same tiles a hull is held to,
        // the moon must have at least one shelter, or the hull assertion would pass with the guard deleted.
        int sheltered = 0;
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                sheltered += ((IEnumerable)bench.Call("SheltersOnTile", ex, new SurfaceTiles.Address(x, y))!)
                    .Cast<object>().Count();
            }
        }

        Assert.True(sheltered > 0, "the moon has no shelter on the tiles the hull is held to — the shelter guard cannot fail here.");
    }

    [Fact]
    public async Task OnlyAHullBoardsAsAHull()
    {
        foreach ((string url, bool hull) in new[] { (Wreck, true), (Station, true), (Moon, false) })
        {
            DeskBench bench = await BootAsync(url);
            object ex = bench.Peek("_surface")!;
            bench.Poke("_boardTarget", ex.GetType().GetProperty("Stop")!.GetValue(ex));
            Assert.Equal(hull, (bool)bench.Call("get_BoardingAWreck")!);
        }
    }

    /// <summary>The deck builder routes by the id: each kind of place gets ITS deck — the derelict her evidence
    /// stations, the station her hops, a moon neither.</summary>
    [Fact]
    public async Task EachKindOfPlaceIsRoutedToItsOwnDeck()
    {
        foreach ((string url, bool evidence, bool hop) in new[] { (Wreck, true, false), (Station, false, true), (Moon, false, false) })
        {
            DeskBench bench = await BootAsync(url);
            var deck = (SpaceSails.Client.Rendering.DeckPlan)bench.Peek("_deckPlan")!;
            Assert.Equal(evidence, deck.Consoles.Any(c => c.Kind == SpaceSails.Client.Rendering.DeckPlan.ConsoleKind.WreckEvidence));
            Assert.Equal(hop, deck.Consoles.Any(c => c.Kind == SpaceSails.Client.Rendering.DeckPlan.ConsoleKind.StationHop));
        }
    }
}
