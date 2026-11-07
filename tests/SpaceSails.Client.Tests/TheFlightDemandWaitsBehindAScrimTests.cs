using System;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1476 item 2 · The collector's demand is a card the world raises, so in FLIGHT it waits behind a scrim that is
/// up and goes up on the first frame the glass is clear (the flight frame pumps the scrim queue). The deaths stay
/// immediate. Proven RED by (a) routing <c>TheDemandGoesUp</c> straight to <c>TheDemandIsRaised</c> (the wait test
/// goes red), and (b) removing the <c>PumpTheScrimQueue()</c> line from <c>OnTick</c> (the release test goes red).
/// </summary>
public sealed class TheFlightDemandWaitsBehindAScrimTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static Pages.Map InFlight()
    {
        Pages.Map map = CastawayBench.Boot("flight-demand-scrim");
        CastawayBench.Set(map, "_deckMode", false);
        return map;
    }

    private static void ATellingIsUp(Pages.Map map, bool up)
    {
        object? card = up ? (StoryBeats.Beat.CollectorHail, (string?)null, (string?)null) : null;
        CastawayBench.Set(map, "_storyCard", card);
    }

    private static void TheCollectorCatchesHer(Pages.Map map)
    {
        var ship = (ShipState)CastawayBench.Read(map, "_ship")!;
        var hunter = new HunterState(
            Id: "collector-1", Callsign: "The Debt Collector", OriginBodyId: "earth",
            SpawnedAtSimTime: 0, ActivationSimTime: 0, State: ship, CaughtPlayer: true, BrokenOff: false);
        CastawayBench.Invoke(map, "TheDemandGoesUp", hunter, null);
    }

    [Fact]
    public void AFlightDemandRaisedWhileAStoryCardIsUpWaitsAndIsRaisedWhenTheCardCloses()
    {
        Pages.Map map = InFlight();
        ATellingIsUp(map, true);

        TheCollectorCatchesHer(map);
        Assert.Null(CastawayBench.Read(map, "_busted"));          // held behind the card, not stacked on it

        CastawayBench.Frame(map);                                  // the card is still up: still waiting
        Assert.Null(CastawayBench.Read(map, "_busted"));

        ATellingIsUp(map, false);                                  // the captain closes it
        CastawayBench.Frame(map);
        Assert.NotNull(CastawayBench.Read(map, "_busted"));        // the very next flight frame raises it
    }

    [Fact]
    public void AFlightDemandWithTheGlassClearGoesUpAtOnce()
    {
        Pages.Map map = InFlight();
        TheCollectorCatchesHer(map);
        Assert.NotNull(CastawayBench.Read(map, "_busted"));
    }

    [Fact]
    public void AnImpactDeathIsNeverHeldBehindAScrim()
    {
        Pages.Map map = InFlight();
        ATellingIsUp(map, true);
        var ship = (ShipState)CastawayBench.Read(map, "_ship")!;
        CastawayBench.Invoke(map, "TriggerImpact", new SurfaceImpact.Crossing("earth", "Earth", 1.0, 0.0, ship.Position));
        Assert.NotNull(CastawayBench.Read(map, "_busted"));
    }
}
