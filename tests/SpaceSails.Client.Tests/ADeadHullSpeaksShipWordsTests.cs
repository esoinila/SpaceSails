using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// R1-F26 · REGOLITH WORDS ABOARD A DERELICT HULL. The boarding receipt said "probe the regolith … [E] the
/// kiosk, wander, or dig", the ambient shudder said "a tremor runs through the rock", and the nerve-band
/// pulse said "digging under threat" — aboard a dead ship with no ground, kiosk or digging. Driven on the
/// shipping page standing on a wreck body.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ADeadHullSpeaksShipWordsTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;
    private static readonly string[] GroundWords = ["regolith", "kiosk", "dig", "rock", "tremor"];

    private static Pages.Map OnAWreck()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        string id = Derelict.BodyIdFor("test-hull");
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(id, "Second Marriage", "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, new LandingSite(0, LandingSiteKind.WildPlain, "x", "", ""));

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_avatarX", 0.0);
        Set(map, "_avatarY", 0.0);
        Set(map, "_shudderSeed", 0xC0FFEE_1234UL);
        return map;
    }

    private static void Set(object target, string field, object? value) =>
        (typeof(Pages.Map).GetField(field, Hidden) ?? throw new InvalidOperationException(field)).SetValue(target, value);

    [Fact]
    public void AmbientShuddersAboardAWreckSpeakNoRockAndNoCrewButTheyStillConsumeTheirSlot()
    {
        MethodInfo fire = typeof(Pages.Map).GetMethod("FireShudder", Hidden)!;
        FieldInfo pulse = typeof(Pages.Map).GetField("_pulse", Hidden)!;
        for (int i = 0; i < 40; i++)
        {
            Pages.Map map = OnAWreck();
            Set(map, "_shudderIndex", i);
            fire.Invoke(map, [0.0]);
            var slot = (PulseSlot)pulse.GetValue(map)!;
            Assert.True(slot.Message is null, $"shudder {i} spoke aboard a dead hull: {slot.Message}");
            Assert.Equal(i + 1, (int)typeof(Pages.Map).GetField("_shudderIndex", Hidden)!.GetValue(map)!);
        }
    }

    [Fact]
    public void TheNerveBandPulseAndTheMatingReceiptDropTheGroundWordsOnAHull()
    {
        string drop = (string)typeof(Pages.Map).GetMethod("NerveBandDropLine", Hidden | BindingFlags.Static)!.Invoke(null, [true])!;
        string mated = (string)typeof(Pages.Map).GetMethod("MatedToAHullLine", Hidden | BindingFlags.Static)!
            .Invoke(null, ["Second Marriage", ""])!;
        foreach (string said in new[] { drop, mated })
        {
            foreach (string bad in GroundWords)
            {
                Assert.DoesNotContain(bad, said, StringComparison.OrdinalIgnoreCase);
            }
        }

        string ground = (string)typeof(Pages.Map).GetMethod("NerveBandDropLine", Hidden | BindingFlags.Static)!.Invoke(null, [false])!;
        Assert.Contains("digging under threat", ground);
    }

    /// <summary>R1-F11 � The page's own field question, at the call site: the spine across the sealed door is not
    /// the hold, the column's own square is.</summary>
    [Fact]
    public void ThePagesArchiveFieldStopsAtTheBulkhead()
    {
        Pages.Map map = OnAWreck();
        Set(map, "_archiveAboard", true);
        PropertyInfo field = typeof(Pages.Map).GetProperty("InArchiveField", Hidden)!;

        Set(map, "_avatarX", (double)WreckLayout.ArchiveStation.X);
        Set(map, "_avatarY", 0.0);
        Assert.False((bool)field.GetValue(map)!, "standing in the spine is not standing in the hold");

        Set(map, "_avatarY", (double)WreckLayout.ArchiveStation.Y);
        Assert.True((bool)field.GetValue(map)!);
    }
}
