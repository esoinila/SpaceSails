using System;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1476 item 2 · The collector's demand is a card the world raises, so in FLIGHT it waits behind a scrim that is
/// up and goes up on the first frame the glass is clear (the flight frame pumps the scrim queue). The deaths stay
/// immediate. Proven RED by (a) calling <c>TheDemandGoesUp</c> directly from <c>TheDemandWaitsItsTurn</c> (the wait test
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
        CastawayBench.Invoke(map, "TheDemandWaitsItsTurn", hunter, null);
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

    // ── the inspection's findings (#1479) ──────────────────────────────────────────────────────────────

    private static PendingWritRecord AWritAtTheBerth() =>
        new("The Debt Collector", "ringside-exchange", FiledAtSimTime: 100.0, HeatWhenFiled: 2, HunterId: "collector-1");

    /// <summary>D1 · A served writ is cleared and saved when its demand really goes up, not when it is queued: a
    /// save taken while the demand waits behind a card still carries the writ. RED: clear and save before the raise.</summary>
    [Fact]
    public void AServedWritIsStillOnTheFileWhileItsDemandWaitsAndIsClearedWhenItGoesUp()
    {
        Pages.Map map = InFlight();
        PendingWritRecord filed = AWritAtTheBerth();
        CastawayBench.Set(map, "_dockedHavenId", filed.HavenId);
        CastawayBench.Set(map, "_writPending", filed);
        ATellingIsUp(map, true);

        CastawayBench.Invoke(map, "TheWaitingWritIsServed");
        CastawayBench.Invoke(map, "TheWaitingWritIsServed");   // served again next frame: still one held, same key
        Assert.Null(CastawayBench.Read(map, "_busted"));
        Assert.Equal(filed, CastawayBench.Read(map, "_writPending"));
        var saved = (Vault)CastawayBench.Invoke(map, "BuildVault", "", "")!;
        Assert.Equal(filed, saved.Progress?.WritPending);       // a reload now keeps the writ
        Assert.Single((System.Collections.ICollection)CastawayBench.Read(map, "_scrimQueued")!);

        ATellingIsUp(map, false);
        CastawayBench.Frame(map);
        Assert.NotNull(CastawayBench.Read(map, "_busted"));
        Assert.Null(CastawayBench.Read(map, "_writPending"));
    }

    /// <summary>D2 · The terms are the catch's, not the showing's: heat raised and the clock run on behind a card
    /// leave the demand cut from the moment of the catch. RED: read the heat and the clock in
    /// <c>TheDemandGoesUp</c> again at show time.</summary>
    [Fact]
    public void TheDemandIsPricedAtTheCatchNotWhenTheCardClosesAndTheWarpComesDownAtOnce()
    {
        Pages.Map held = InFlight();
        ATellingIsUp(held, true);
        CastawayBench.Set(held, "Warp", 5);
        TheCollectorCatchesHer(held);
        Assert.Equal(1, (int)CastawayBench.Read(held, "Warp")!);
        object heatThen = CastawayBench.Read(held, "_heat")!;
        double t0 = (double)CastawayBench.Read(held, "SimTime")!;

        CastawayBench.Set(held, "_heat", EncounterRule.RaiseHeat((HeatState)heatThen, 4, t0));
        CastawayBench.Set(held, "SimTime", t0 + 86_400.0);
        ATellingIsUp(held, false);
        CastawayBench.Invoke(held, "PumpTheScrimQueue");
        object a = CastawayBench.Read(held, "_busted")!;

        Pages.Map straight = InFlight();
        CastawayBench.Set(straight, "SimTime", t0);
        TheCollectorCatchesHer(straight);
        object b = CastawayBench.Read(straight, "_busted")!;

        foreach (string term in new[] { "Seed", "Heat", "Bribe" })
        {
            Assert.Equal(b.GetType().GetProperty(term)!.GetValue(b), a.GetType().GetProperty(term)!.GetValue(a));
        }
    }

    /// <summary>D3 · A card held for the life being left is not owed to the one arriving. RED: drop the clear.</summary>
    [Fact]
    public void ANewLifeDoesNotInheritTheHeldCards()
    {
        Pages.Map map = InFlight();
        ATellingIsUp(map, true);
        TheCollectorCatchesHer(map);
        Assert.NotNull(CastawayBench.Read(map, "_scrimQueued"));
        CastawayBench.Invoke(map, "ForgetThePageSessionRiders");
        Assert.Null(CastawayBench.Read(map, "_scrimQueued"));
    }
}
