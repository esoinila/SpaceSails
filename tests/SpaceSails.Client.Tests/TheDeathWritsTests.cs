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
}
