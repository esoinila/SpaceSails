using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #653 slice 2 · THE STATION'S TWO PER-VISIT LINES, PINNED BY CADENCE BEFORE THE MECHANISM MOVES. The lock's line
/// and the cut face's line are each told ONCE A VISIT — the visit being one excursion, from the boat's mating to the
/// way home — and again on the next boarding. Asked at the two panels the lines draw on (the log and the pulse),
/// through the shipping boarding, never of the set that latches them.
/// </summary>
public sealed class TheStationToldOnceCadenceTests
{
    private const string Url = "/map?dock=the-tilt&station=1&land=1";

    private static IEnumerable<string> Log(DeskBench bench) =>
        ((IEnumerable<(double SimTime, string Text)>)bench.Peek("_autopilotEvents")!).Select(e => e.Text);

    private static async Task<DeskBench> BootAsync()
    {
        DeskBench bench = await DeskBench.BootAsync(Url);
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);
        return bench;
    }

    /// <summary>Home through the boat's own lock, then the shipping boarding again — a second visit to the same station.</summary>
    private static async Task ReboardAsync(DeskBench bench)
    {
        DeckPlan.ConsoleSpot home = Assert.Single(
            ((DeckPlan)bench.Peek("_deckPlan")!).Consoles, c => c.Kind == DeckPlan.ConsoleKind.ShuttleAirlock);
        bench.Poke("_avatarX", (double)home.X);
        bench.Poke("_avatarY", (double)home.Y);
        bench.CallOnTheDispatcher("InteractAtConsole");
        Assert.False(bench.OnSurface, "the way home did not leave the station.");

        object stop = ((System.Collections.IEnumerable)bench.Call("ShuttleDestinationsInRange")!).Cast<object>()
            .Single(s => StationAboard.TryParseStationId(
                (string)s.GetType().GetProperty("Body")!.GetValue(s)!.GetType().GetProperty("Id")!
                    .GetValue(s.GetType().GetProperty("Body")!.GetValue(s))!, out _));
        await (Task)bench.CallOnTheDispatcher("BeginSurfaceExcursion", stop, ShuttleExcursion.Pack(0, 0, []), 0, null)!;
        Assert.True(bench.OnSurface);
    }

    [Fact]
    public async Task TheLockLineIsToldOncePerVisitAndAgainOnTheNextBoarding()
    {
        DeskBench bench = await BootAsync();
        Assert.Single(Log(bench), l => l == StationAboard.LockLine);

        await ReboardAsync(bench);

        // The second visit's crew lock is the first lock of THAT visit: told again. The first-standing line is the
        // book's, not the visit's, and stays at one.
        Assert.Equal(2, Log(bench).Count(l => l == StationAboard.LockLine));
        Assert.Single(Log(bench), l => l == StationAboard.FirstStandingLine);
    }

    [Fact]
    public async Task TheCutLineIsToldOncePerVisitAndAgainOnTheNextBoarding()
    {
        DeskBench bench = await BootAsync();
        object ex = bench.Peek("_surface")!;
        const string line = "the cut face's line, as the press would carry it";

        bench.CallOnTheDispatcher("TellOnce", ex, "cut", line);
        bench.CallOnTheDispatcher("TellOnce", ex, "cut", line);
        Assert.Single(Log(bench), l => l == line);
        Assert.Contains(line, bench.Pulse, StringComparison.Ordinal);

        // A different key is a different line; the lock's key is the lock's (spent by the boarding itself).
        bench.CallOnTheDispatcher("TellOnce", ex, "lock", StationAboard.LockLine);
        Assert.Single(Log(bench), l => l == StationAboard.LockLine);

        await ReboardAsync(bench);
        bench.Poke("_pulse", PulseSlot.Empty);
        bench.CallOnTheDispatcher("TellOnce", bench.Peek("_surface")!, "cut", line);
        Assert.Equal(2, Log(bench).Count(l => l == line));
    }
}
