using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1486 inspection follow-up · THE LOW-AIR LATCH CLEARS ON REFILL. U09-F03's room-air ease only eases a
/// captain whose air ran low (<c>AirSpellLow</c>); that latch has to come off when the tank is topped back
/// above the low line — at her tube (<c>StepSuitAir</c>) and at a shelter rack (<c>DrawFromRack</c>) — or the
/// room air would ease nerve for a captain who has long since been fine. Driven on the shipping page.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLowAirLatchClearsOnRefillTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;
    private const string Body = "luna";

    private static int ThePark => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException("no pressurised floor");

    private static (Pages.Map Map, object Ex) Bench(int floor, double avatarY)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);
        Field(map, "_surface", ex);
        Field(map, "_deckMode", true);
        Field(map, "_avatarX", 0.0);
        Field(map, "_avatarY", avatarY);
        return (map, ex);
    }

    private static void Field(object map, string name, object? v) =>
        typeof(Pages.Map).GetField(name, Hidden)!.SetValue(map, v);

    private static bool Latch(object ex) => (bool)ex.GetType().GetProperty("AirSpellLow")!.GetValue(ex)!;

    private static void Air(object ex, double seconds, bool latch)
    {
        ex.GetType().GetProperty("AirSeconds")!.SetValue(ex, seconds);
        ex.GetType().GetProperty("AirSpellLow")!.SetValue(ex, latch);
    }

    private static double AirOf(object ex) => (double)ex.GetType().GetProperty("AirSeconds")!.GetValue(ex)!;

    /// <summary>The nerve's own question about the room air, asked the way StepNerve asks it.</summary>
    private static bool FloorEases(object ex) => Latch(ex) || SuitAir.TankIsLow(AirOf(ex));

    [Fact]
    public void TheTubeRefillAboveThe35PercentLineClearsTheLatchAndTheFloorGivesNoEase()
    {
        (Pages.Map map, object ex) = Bench(floor: 0, avatarY: 1.0);
        double low = SuitAir.TankSeconds * SuitAir.LowAirFraction * 0.5;
        Air(ex, low, latch: true);
        Assert.True(FloorEases(ex), "the setup is not a low spell — this case would prove nothing.");

        var supply = typeof(Pages.Map).GetMethod("AirSupplyOf", Hidden)!.Invoke(map, [ex]);
        Assert.Equal(SuitAir.Supply.Ship, supply);

        MethodInfo step = typeof(Pages.Map).GetMethod("StepSuitAir", Hidden)!;
        for (int i = 0; i < 400 && AirOf(ex) <= SuitAir.TankSeconds * SuitAir.LowAirFraction + 1; i++)
        {
            step.Invoke(map, [1.0]);
        }

        Assert.True(AirOf(ex) > SuitAir.TankSeconds * SuitAir.LowAirFraction, "the tube never refilled the tank.");
        Assert.False(Latch(ex));
        Assert.False(FloorEases(ex));
        Assert.False(NervePipsEasesOnThePressurisedFloor(ex));
    }

    [Fact]
    public void TheRackRefillAboveThe35PercentLineClearsTheLatchAndTheFloorGivesNoEase()
    {
        (Pages.Map map, object ex) = Bench(ThePark, avatarY: -100.0);
        Air(ex, SuitAir.TankSeconds * SuitAir.LowAirFraction * 0.5, latch: true);
        Assert.True(FloorEases(ex));

        MethodInfo draw = typeof(Pages.Map).GetMethod("DrawFromRack", Hidden)!;
        for (int i = 0; i < 400 && AirOf(ex) <= SuitAir.TankSeconds * SuitAir.LowAirFraction + 1; i++)
        {
            draw.Invoke(map, [ex, SurfaceShelter.ReservoirSeconds, 1.0, 0.0]);
        }

        Assert.True(AirOf(ex) > SuitAir.TankSeconds * SuitAir.LowAirFraction, "the rack never refilled the tank.");
        Assert.False(Latch(ex));
        Assert.False(NervePipsEasesOnThePressurisedFloor(ex));
    }

    /// <summary>Hold a 2-pip captain safe on a pressurised floor for a long stay with this excursion's
    /// latch as the frame's AirWasLow, and report whether any pip came back.</summary>
    private static bool NervePipsEasesOnThePressurisedFloor(object ex)
    {
        double n = NervePips.FromPips(2);
        var clock = NervePips.Beats.Fresh;
        for (int i = 0; i < 20; i++)
        {
            var f = new NervePips.Frame(
                OnExcursion: true, OnRegolith: false, SeesMonolith: false, Stressors: default,
                FreshSightings: 0, Touched: false, DtSeconds: NervePips.AirlockBeatSeconds,
                SafeGroundHoldsPressure: true, AirWasLow: FloorEases(ex));
            NervePips.Step s = NervePips.Advance(n, false, clock, in f);
            n = s.Nerve;
            clock = s.Beats;
        }
        return NervePips.PipsOf(n) > 2;
    }
}
