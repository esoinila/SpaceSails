using System;
using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U04-F29 (Fable ruling 2026-11-07, owner may veto) · ONE MAN ON B1. Posted: he stands at the door and no rota
/// round walks B1. OnRound: his short beat is B1's round. Absent: the #863 rota round walks B1. Driven through the
/// shipping page's own <c>SpawnPatrolFor</c> with the door-keeper set to each posting.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDoorKeeperIsTheOnlyManOnB1Tests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const string Body = "luna";
    private const long Watch = 7;

    private static int RoundOnB1(GateGuard.Posting posting)
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
        int bar = UndergroundComplex.TopPressurisedFloor(Body)!.Value;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, bar);
        exType.GetProperty("CanteenWatch")!.SetValue(ex, Watch);
        typeof(Pages.Map).GetField("_surface", Hidden)!.SetValue(map, ex);
        typeof(Pages.Map).GetField("_deckMode", Hidden)!.SetValue(map, true);
        typeof(Pages.Map).GetMethod("RebuildSurfaceDeck", Hidden)!.Invoke(map, null);

        // The door-keeper the rebuild met is replaced by the posting under test (what ?guard= does).
        object gate = Activator.CreateInstance(gateType, nonPublic: true)!;
        gateType.GetProperty("Ground")!.SetValue(gate, Body);
        gateType.GetProperty("Window")!.SetValue(gate, Watch);
        gateType.GetProperty("Visit")!.SetValue(gate, new GateGuard.Visit(posting, true));
        gateType.GetProperty("Floor")!.SetValue(gate, bar);
        gateType.GetProperty("Post")!.SetValue(gate, new DeckReachability.Point(0, 0));
        exType.GetProperty("Gate")!.SetValue(ex, gate);

        typeof(Pages.Map).GetMethod("SpawnPatrolFor", Hidden)!.Invoke(map, new[] { ex });
        object round = PatrolState.Round(map)!;
        return ((ICollection)round.GetType().GetProperty("Guards")!.GetValue(round)!).Count;
    }

    [Fact]
    public void ThePremiseHolds_B1IsPatrolledAndTheRotaWouldPutAManOnIt()
    {
        int bar = UndergroundComplex.TopPressurisedFloor(Body)!.Value;
        Assert.True(PatrolBeat.IsPatrolled(Body, bar));
        Assert.True(PatrolBeat.GuardsOn(Body, bar, Watch) >= 1);
    }

    [Fact]
    public void APostedManStandsAtTheDoorAndNoRotaRoundWalksB1() =>
        Assert.Equal(0, RoundOnB1(GateGuard.Posting.Posted));

    [Fact]
    public void AnOnRoundMansShortBeatIsB1sRoundSoNoRotaRoundWalksIt() =>
        Assert.Equal(0, RoundOnB1(GateGuard.Posting.OnRound));

    [Fact]
    public void AnAbsentWindowLeavesTheRotaRoundToWalkB1() =>
        Assert.True(RoundOnB1(GateGuard.Posting.Absent) >= 1);
}
