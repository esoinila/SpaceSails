using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U04-F12 (Fable ruling 2026-11-07) · THE ROUND NEVER STOPS ANYONE ON THE NO PASS REQUIRED FLOOR. Driven through the
/// shipping round: a guard with the captain squarely in his sight hails on B2, and does not on Canteen 1's floor.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRoundNeverStopsAnyoneInCanteen1Tests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const string Body = "luna";
    private const long Watch = 7;

    /// <summary>Stage a floor with an ABSENT door-keeper (so the rota round walks even B1), put the first guard's
    /// hail conditions all true, ask the round to stop anybody who sees the captain, and say whether it hailed.</summary>
    private static bool TheRoundHails(int level)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        Type gateType = typeof(Pages.Map).GetNestedType("ManAtTheDoor", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, level);
        exType.GetProperty("CanteenWatch")!.SetValue(ex, Watch);
        typeof(Pages.Map).GetField("_surface", Hidden)!.SetValue(map, ex);
        typeof(Pages.Map).GetField("_deckMode", Hidden)!.SetValue(map, true);
        typeof(Pages.Map).GetField("_patrolCheat", Hidden)?.SetValue(map, null);
        typeof(Pages.Map).GetMethod("RebuildSurfaceDeck", Hidden)!.Invoke(map, null);

        object gate = Activator.CreateInstance(gateType, nonPublic: true)!;
        gateType.GetProperty("Ground")!.SetValue(gate, Body);
        gateType.GetProperty("Window")!.SetValue(gate, Watch);
        gateType.GetProperty("Visit")!.SetValue(gate, new GateGuard.Visit(GateGuard.Posting.Absent, true));
        gateType.GetProperty("Floor")!.SetValue(gate, UndergroundComplex.TopPressurisedFloor(Body)!.Value);
        gateType.GetProperty("Post")!.SetValue(gate, new DeckReachability.Point(0, 0));
        exType.GetProperty("Gate")!.SetValue(ex, gate);
        typeof(Pages.Map).GetMethod("SpawnPatrolFor", Hidden)!.Invoke(map, new[] { ex });

        object round = PatrolState.Round(map)!;
        var guards = (IList)round.GetType().GetProperty("Guards")!.GetValue(round)!;
        Assert.True(guards.Count >= 1, $"no guard walks level {level} - the premise of the test is gone.");
        object g = guards[0]!;
        Type gt = g.GetType();
        double x = (double)(gt.GetField("X")?.GetValue(g) ?? gt.GetProperty("X")!.GetValue(g))!;
        double y = (double)(gt.GetField("Y")?.GetValue(g) ?? gt.GetProperty("Y")!.GetValue(g))!;
        gt.GetProperty("SinceStop")!.SetValue(g, PatrolBeat.AfterTheStopSeconds + 1.0);

        typeof(Pages.Map).GetField("_avatarX", Hidden)!.SetValue(map, x + 1.0);
        typeof(Pages.Map).GetField("_avatarY", Hidden)!.SetValue(map, y);
        round.GetType().GetProperty("FloorSeconds")!.SetValue(round, 1000.0);

        MethodInfo ask = round.GetType().GetMethod("StopTheRoundIfAnybodySeesYou", Hidden)!;
        ask.Invoke(round, new object?[] { new List<SurfaceCollision.Segment>(), new ContactLedger() });
        return (bool)gt.GetProperty("WalkingUp")!.GetValue(g)!;
    }

    [Fact]
    public void OnB2TheRoundHailsACaptainItSees() =>
        Assert.True(TheRoundHails(OtherPatrolledFloor()), "the premise: a guard in plain sight hails off the no-pass floor.");

    [Fact]
    public void OnCanteen1sFloorTheRoundNeverHailsAnyone() =>
        Assert.False(TheRoundHails(UndergroundComplex.TopPressurisedFloor(Body)!.Value));

    private static int OtherPatrolledFloor() =>
        Enumerable.Range(1, 14).Select(i => -i)
            .First(l => PatrolBeat.IsPatrolled(Body, l) && !UndergroundComplex.IsTheNoPassFloor(Body, l));
}
