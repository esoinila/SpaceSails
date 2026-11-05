using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// The death-and-writs lane (#1397, #1398, #1391, #1392, #1429): regression guards for the new-life reset and the
/// ground writ. Each class of guard names the fix it goes RED without.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDeathWritsTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static Pages.Map ABench()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        return map;
    }

    internal static object? Call(Pages.Map map, string name, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(name, Hidden) ?? throw new InvalidOperationException(name)).Invoke(map, args);

    internal static object? Field(Pages.Map map, string name) =>
        (typeof(Pages.Map).GetField(name, Hidden) ?? throw new InvalidOperationException(name)).GetValue(map);

    internal static void SetField(Pages.Map map, string name, object? value) =>
        (typeof(Pages.Map).GetField(name, Hidden) ?? throw new InvalidOperationException(name)).SetValue(map, value);

    /// <summary>#1429 · the new-life reset clears the cards by field, so the old life's held lines are dropped
    /// rather than released into the new life's pulse. RED with CloseViewObject()/CloseStoryCard() restored in
    /// ForgetThePageSessionRiders (they call ReleaseHeldSayings, which pulses the held line).</summary>
    [Fact]
    public void ANewLifeDropsTheOldLifesHeldLinesInsteadOfPulsingThem()
    {
        Pages.Map map = ABench();
        Call(map, "HoldAndFile", "A line from the dead life.", "x", PulseRank.Status);
        Assert.True(((PulseHold)Field(map, "_held")!).Any);

        Call(map, "ForgetThePageSessionRiders");

        Assert.False(((PulseHold)Field(map, "_held")!).Any);
        Assert.Null(((PulseSlot)Field(map, "_pulse")!).Message);
    }

    // ── #1391 · THE WRIT IS NOT SERVED THROUGH ROCK ─────────────────────────────────────────────────

    private static Pages.Map OnTheGroundWithACrewLanded(int floor)
    {
        Pages.Map map = ABench();
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Any)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Any)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody("luna", "Luna", "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        exType.GetProperty("CollectorCallsign")!.SetValue(ex, "BAILIFF");
        exType.GetProperty("CollectorsComing")!.SetValue(ex, true);
        exType.GetProperty("CollectorsEtaSeconds")!.SetValue(ex, 0.0);
        exType.GetProperty("CollectorShelterNoted")!.SetValue(ex, true);
        exType.GetProperty("CollectorsHailed")!.SetValue(ex, true);
        SetField(map, "_surface", ex);
        SetField(map, "_deckMode", true);
        SetField(map, "_heat", new HeatState(3, 0.0));
        Call(map, "RebuildSurfaceDeck");
        Call(map, "StepCollectors", 0.1);   // the boat sets down
        var crew = (System.Collections.IList)Field(map, "_collectors")!;
        Assert.True(crew.Count > 0, "no crew landed; the bench has nothing to drive");
        exType.GetProperty("Floor")!.SetValue(ex, floor);
        // the captain's floor-plan coordinates happen to coincide with a collector's regolith coordinates
        object c0 = crew[0]!;
        SetField(map, "_avatarX", (double)c0.GetType().GetField("X")!.GetValue(c0)!);
        SetField(map, "_avatarY", (double)c0.GetType().GetField("Y")!.GetValue(c0)!);
        return map;
    }

    /// <summary>#1391 · A captain on B1 whose floor-plan coordinates coincide with a collector's regolith
    /// coordinates is NOT served a writ: the compare is between two different coordinate systems. The control
    /// (the same placement on the regolith) IS served, so the world can tell pass from fail. <b>RED</b> by removing
    /// the <c>ex.Floor &lt; 0</c> return in StepCollectors.</summary>
    [Fact]
    public void ACrewOnTheRegolithServesNoWritOnACaptainInTheRock()
    {
        Pages.Map control = OnTheGroundWithACrewLanded(floor: 0);
        Call(control, "StepCollectors", 0.1);
        Assert.NotNull(Field(control, "_busted"));

        Pages.Map below = OnTheGroundWithACrewLanded(floor: -1);
        for (int i = 0; i < 30; i++)
        {
            Call(below, "StepCollectors", 0.1);
        }
        Assert.Null(Field(below, "_busted"));
    }
}
