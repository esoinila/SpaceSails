using System.Collections;
using System.Collections.Generic;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// The page-session riders: state that lives on the Map page but is NOT a vault field, so the BuildVault audit
/// (#1374/#1376) could not see it. The live collectors, the news wire (whose hull-loss entries are claim receipts),
/// the hail answers, and the mission-moment latch and ledger all survived a New voyage and a load. One door each way:
/// <c>ResetLiveStateForNewGame</c> and <c>ApplyVault</c>.
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class ThePageSessionRidersDoNotOutliveTheLifeTests
{
    public static IEnumerable<object[]> Doors() => [["ResetLiveStateForNewGame"], ["ApplyVault"]];

    private static void TheOldLifeLeavesItsRiders(Pages.Map map)
    {
        ((IList)Read(map, "_hunters")!).Add(new HunterState(
            Id: "wolf", Callsign: "WOLF", OriginBodyId: "luna", SpawnedAtSimTime: 0, ActivationSimTime: 0,
            State: (ShipState)Read(map, "_ship")!, CaughtPlayer: false, BrokenOff: false));
        ((IList)Read(map, "_newsEvents")!).Add(new NewsWire.NewsEvent(NewsWire.NewsEventKind.HullLostAtABerth, 10, "OLD HULL"));
        ((IDictionary<string, string>)Read(map, "_hailAnswers")!)["old-ship"] = "an old life's reply";
        ((HashSet<string>)Read(map, "_momentsRaisedFor")!).Add("roadster");
        ((List<(double SimTime, string Text)>)Read(map, "_missionMomentLedger")!).Add((5, "found her"));
    }

    private static void Pass(Pages.Map map, string door)
    {
        if (door == "ApplyVault") { Invoke(map, door, new Vault()); }
        else { Invoke(map, door); }
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheLiveCollectorsDoNotRideIntoTheNextLife(string door)
    {
        Pages.Map map = Boot("rider-hunters-" + door);
        TheOldLifeLeavesItsRiders(map);
        Assert.Single((IList)Read(map, "_hunters")!);

        Pass(map, door);

        Assert.Empty((IList)Read(map, "_hunters")!);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheWireAndTheHailsDoNotRideIntoTheNextLife(string door)
    {
        Pages.Map map = Boot("rider-wire-" + door);
        TheOldLifeLeavesItsRiders(map);

        Pass(map, door);

        Assert.Empty((IList)Read(map, "_newsEvents")!);
        Assert.Empty((IDictionary<string, string>)Read(map, "_hailAnswers")!);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheMomentLatchAndLedgerDoNotRideIntoTheNextLife(string door)
    {
        Pages.Map map = Boot("rider-moments-" + door);
        TheOldLifeLeavesItsRiders(map);

        Pass(map, door);

        Assert.Empty((HashSet<string>)Read(map, "_momentsRaisedFor")!);
        Assert.Empty((List<(double SimTime, string Text)>)Read(map, "_missionMomentLedger")!);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheCardLineDoesNotRideIntoTheNextLife(string door)
    {
        Pages.Map map = Boot("rider-cards-" + door);
        Invoke(map, "RaiseStoryBeat", StoryBeats.Beat.CrewDeputation, null, null);
        Invoke(map, "RaiseStoryBeat", StoryBeats.Beat.ArchivePurged, null, null);
        ((IList)Read(map, "_deferredBeats")!).Add((StoryBeats.Beat.CrewDeputation, (string?)null, (string?)null));
        Assert.NotNull(Read(map, "_storyCard"));
        Assert.NotEmpty((IList)Read(map, "_storyWaiting")!);

        ((IList)Read(map, "_viewObjectWaiting")!).Add(SpotFor("OLD LIFE WAITING"));
        Set(map, "_viewObject", SpotFor("OLD LIFE ON THE GLASS"));
        Set(map, "_storyPlate", (StoryBeats.Beat.Flashback, (string?)"old life", 1e9));

        Pass(map, door);

        Assert.Null(Read(map, "_viewObject"));
        Assert.True(Read(map, "_storyPlate") is not ValueTuple<StoryBeats.Beat, string?, double> oldPlate || oldPlate.Item2 != "old life",
            "the old life's plate is still on the glass");
        Assert.Null(Read(map, "_storyCard"));
        Assert.Empty((IList)Read(map, "_storyWaiting")!);
        Assert.Empty((IList)Read(map, "_deferredBeats")!);
        Assert.Empty((IList)Read(map, "_viewObjectWaiting")!);
    }

    private static SpaceSails.Client.Rendering.DeckPlan.ConsoleSpot SpotFor(string label) =>
        new(SpaceSails.Client.Rendering.DeckPlan.ConsoleKind.ViewObject, 0f, 0f, label, "", "caption");
}
