using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Round 2 / U05 (#1419) and U06 (#1420) · the client-side pins: the rack's gauge line, the door sentry in
/// the HUD, the bare add-on links, the outpost bearing, the red O2 clock and the back rooms' park plates.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRound2U05AndU06ClientPinsTests
{
    private const string Luna = "/map?dock=selene-gate&body=luna&site=0&land=1";

    private static Pages.Map.SurfaceExcursion AnExcursion() => new()
    {
        Stop = new Pages.Map.ShuttleStop(
            new CelestialBody("luna", "luna", "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false),
        RestoreHavenId = null,
        Site = new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""),
    };

    // ── U05-F03 · the rack never narrates a fill that did not happen ────────────────────────────────

    [Fact]
    public void ARackThatNeverFilledThisTankSaysItStaysShutNotThatItClickedOff()
    {
        Pages.Map.SurfaceExcursion ex = AnExcursion();   // a full tank, well past two thirds
        Assert.Equal(SurfaceShelter.AlreadyFullEnoughLine, Pages.Map.RackGaugeLine(ex, 100, pumpedHere: false));
    }

    [Fact]
    public void ARackThatDidFillThisTankStillSaysItClickedOffAtTwoThirds()
    {
        Pages.Map.SurfaceExcursion ex = AnExcursion();
        Assert.Equal(SurfaceShelter.PumpDoneLine, Pages.Map.RackGaugeLine(ex, 100, pumpedHere: true));
    }

    // ── U05-F05 · the tube's own door sentry is not a bot the captain set down ─────────────────────────

    [Fact]
    public async Task TheDoorSentryIsNotCountedAsASentryHolding()
    {
        DeskBench bench = await DeskBench.BootAsync(Luna);
        var ex = (Pages.Map.SurfaceExcursion)bench.Peek("_surface")!;
        Assert.Contains(ex.Bots, b => SurfaceArrival.IsDoorSentry(b.Unit));
        Assert.DoesNotContain(Captions(bench), c => c.Contains("sentry holding", StringComparison.Ordinal));

        ex.Bots.First(b => !SurfaceArrival.IsDoorSentry(b.Unit)).Deployed = true;
        Assert.Contains(Captions(bench), c => c.StartsWith("🤖 1 sentry holding", StringComparison.Ordinal));
    }

    private static List<string> Captions(DeskBench bench) =>
        (List<string>)bench.Call("BuildTrackerCaptions", bench.Peek("_surface"), 0)!;

    // ── U05-F09 · a bare add-on link lands instead of coasting near Earth ──────────────────────────────

    [Theory]
    [InlineData("/map?shelter=1")]
    [InlineData("/map?kit=1")]
    [InlineData("/map?outpost=1")]
    public async Task ABareAddOnLinkBootsOnTheGround(string url)
    {
        DeskBench bench = await DeskBench.BootAsync(url);
        Assert.True(bench.OnSurface, $"{url} booted coasting with the flag silently ignored.");
    }

    // ── U05-F10 · ?outpost=1 says where the hut is ─────────────────────────────────────────────────────

    [Fact]
    public async Task TheOutpostCheatNamesTheHutsBearing()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1&outpost=1");
        Assert.Contains("DEV ?outpost=1: the hut is on this ground", bench.Pulse, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 0, 30, 0, "30 du E")]
    [InlineData(0, 0, 0, -40, "40 du N")]
    [InlineData(0, 0, -20, 20, "28 du SW")]
    public void TheBearingReadsOffTheTwoPoints(double fx, double fy, double dx, double dy, string expected) =>
        Assert.Contains(expected, Pages.Map.OutpostCheatLine(fx, fy, dx, dy), StringComparison.Ordinal);

    // ── U06-F09 · no red clock over a tank that is stopped ─────────────────────────────────────────────

    [Fact]
    public async Task TheRedClockGoesOutOnAPressurisedFloorAndComesBackOnADeadOne()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&secretlab=deep&land=1&floor=1&air=90");
        var ex = (Pages.Map.SurfaceExcursion)bench.Peek("_surface")!;
        ex.AirSeconds = SuitAir.ReserveSeconds / 2;
        Assert.Null(Countdown(bench));          // B1 holds pressure: the tank is stopped

        DeskBench dead = await DeskBench.BootAsync("/map?dock=the-tilt&secretlab=deep&land=1&floor=2&air=90");
        var deadEx = (Pages.Map.SurfaceExcursion)dead.Peek("_surface")!;
        deadEx.AirSeconds = SuitAir.ReserveSeconds / 2;
        Assert.NotNull(Countdown(dead));        // B2 is vacuum: the same tank, drawing, is the problem
    }

    private static object? Countdown(DeskBench bench) =>
        bench.Call("BuildSurfaceHud")!.GetType().GetProperty("Countdown")!.GetValue(bench.Call("BuildSurfaceHud"));

    // ── U06-F08 · the back rooms are named on the gravel too ───────────────────────────────────────────

    [Fact]
    public void EveryBackRoomPlateStandsOnTheGravelBesideItsParkDoor()
    {
        int rooms = 0;
        foreach (string body in new[] { "luna", "phobos", "miranda", "triton", "europa", "titan", "the-clinker" })
        {
            if (UndergroundComplex.Build(body, -1, MoonSurface.ExpeditionField()).Park is not { } green)
            {
                continue;
            }
            foreach (UndergroundComplex.BackRoom room in green.Rooms)
            {
                rooms++;
                (float x, float y, string text) = HiveInterior.ParkSidePlate(in green, room);
                Assert.Equal(room.Plate, text);
                Assert.True(green.Contains(x, y), $"{body}: {text} stands off the gravel at {x},{y}.");
                double dx = x - ((room.Door.X1 + room.Door.X2) / 2), dy = y - ((room.Door.Y1 + room.Door.Y2) / 2);
                Assert.InRange(Math.Sqrt((dx * dx) + (dy * dy)), 2.0, 3.0);
            }
        }

        Assert.True(rooms > 0, "no body in the sweep has a park with back rooms, so nothing was proved.");
    }
}
