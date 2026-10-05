using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #653 slice 1 · THE DEAD STATION, BOOTED FROM ITS URL AND PLAYED — through the shipping component, the shipping
/// descent and the shipping [E] dispatch, never a hand-built deck.
///
/// <para><b>It plays what the dev start plays.</b> <c>?station=1&amp;land=1</c> hangs the station off the berth and
/// rides <c>?land=</c>'s own descent; the tests then walk the captain to a console and press the real key, and
/// judge the state AFTER the sequence (the clock, the satchel, the dock, the air) rather than the line the press
/// printed. Lines are asserted at the panel they are drawn on — the pulse the HUD reads and the log the
/// captain's desk reads — and every expectation about a line is built from Core's own producer, never from text
/// this file typed.</para>
///
/// <para>The dev start's station is <c>dev-station-6</c>, pinned by
/// <see cref="TheDevStationHasBothKindsOfWayIn"/> to have a lock to cycle (the Habitat) and a face to cut (the
/// Reactor) among its two severed arms — which is the whole reason it was chosen.</para>
/// </summary>
public sealed class TheDeadStationIsBoardedTests
{
    private const string Url = "/map?dock=the-tilt&station=1&land=1";
    private const string Id = "dev-station-6";

    private static object? Get(object instance, string member) =>
        instance.GetType().GetProperty(member, TestTree.AnythingAtAll)?.GetValue(instance)
        ?? instance.GetType().GetField(member, TestTree.AnythingAtAll)?.GetValue(instance);

    private static void Set(object instance, string member, object? value)
    {
        if (instance.GetType().GetProperty(member, TestTree.AnythingAtAll) is { } p)
        {
            p.SetValue(instance, value);
            return;
        }

        instance.GetType().GetField(member, TestTree.AnythingAtAll)!.SetValue(instance, value);
    }

    private static async Task<DeskBench> BootAsync()
    {
        DeskBench bench = await DeskBench.BootAsync(Url);
        await bench.RenderAsync();
        Assert.True(bench.OnSurface, "?station=1&land=1 did not put the away team on any ground at all.");
        return bench;
    }

    private static DeckPlan Deck(DeskBench bench) => (DeckPlan)bench.Peek("_deckPlan")!;

    private static object Excursion(DeskBench bench) => bench.Peek("_surface")!;

    private static StationWreck.ModuleId Dock(DeskBench bench) =>
        (StationWreck.ModuleId)Get(Get(Excursion(bench), "Station")!, "Dock")!;

    private static double SimTime(DeskBench bench) => (double)bench.Peek("SimTime")!;

    private static IEnumerable<string> Log(DeskBench bench) =>
        ((IEnumerable<(double SimTime, string Text)>)bench.Peek("_autopilotEvents")!).Select(e => e.Text);

    private static void StandAt(DeskBench bench, double x, double y)
    {
        bench.Poke("_avatarX", x);
        bench.Poke("_avatarY", y);
    }

    private static void Press(DeskBench bench) => bench.CallOnTheDispatcher("InteractAtConsole");

    private static DeckPlan.ConsoleSpot HopTo(DeskBench bench, StationWreck.ModuleId module)
    {
        StationWreck.Access arrival = StationAboard.AccessOf(Id, module);
        return Deck(bench).Consoles.Single(c =>
            c.Kind == DeckPlan.ConsoleKind.StationHop && c.Label.Contains(arrival.Name, StringComparison.Ordinal));
    }

    // ── The dev start ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheDevStationHasBothKindsOfWayIn()
    {
        IReadOnlyList<StationWreck.ModuleId> severed = StationWreck.SeveredTubes(Id);
        Assert.Contains(severed, a => StationAboard.AccessOf(Id, a).Kind == StationWreck.AccessKind.ServiceableLock);
        Assert.Contains(severed, a => StationAboard.AccessOf(Id, a).Kind == StationWreck.AccessKind.CutFace);
        Assert.Contains("station=1", DevStarts.All.Single(e => e.Label.StartsWith("Ledger Point", StringComparison.Ordinal)).Url);
    }

    [Fact]
    public async Task TheBootPutsTheAwayTeamInsideHerCrewLockOnHerOwnDeck()
    {
        DeskBench bench = await BootAsync();

        Assert.Equal(StationAboard.BodyIdFor(Id), Get(Get(Excursion(bench), "Stop")!, "Body") is { } body ? Get(body, "Id") : null);
        Assert.Equal(StationWreck.ModuleId.Hub, Dock(bench));

        DeckPlan deck = Deck(bench);
        Assert.Contains(deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.StationHop);
        Assert.Equal((deck.SpawnX, deck.SpawnY), ((double)bench.Peek("_avatarX")!, (double)bench.Peek("_avatarY")!));
        Assert.Equal(StationAboard.PlateOf(StationWreck.ModuleId.Hub), deck.RoomLabels.First(l => l.Text.StartsWith("HUB")).Text);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    [Fact]
    public async Task TheFirstStandingAndTheFieldBookAreToldOnceAndTheLockLineGoesToTheLog()
    {
        DeskBench bench = await BootAsync();

        Assert.Contains(StationAboard.FirstStandingLine, bench.Pulse, StringComparison.Ordinal);

        var notes = ((IEnumerable<FieldNote>)bench.Peek("_fieldNotes")!).Where(n => n.Text == StationAboard.FieldBookLine).ToArray();
        Assert.Single(notes);
        Assert.Equal("📍", notes[0].Glyph);

        Assert.Single(Log(bench), l => l == StationAboard.LockLine);

        // A second boarding with the book already holding the entry says neither again — the book is the latch.
        bench.CallOnTheDispatcher("ArriveAtTheStation");
        Assert.Single(((IEnumerable<FieldNote>)bench.Peek("_fieldNotes")!), n => n.Text == StationAboard.FieldBookLine);
        Assert.Single(Log(bench), l => l == StationAboard.FirstStandingLine);
        Assert.Single(Log(bench), l => l == StationAboard.LockLine);
    }

    /// <summary>The crew lock is a serviceable lock, and a locked door is TIME: boarding spends the crossing's
    /// seconds AND the lock's. Asked of the shipping boarding from a docked berth, so the delta is the whole of what
    /// the clock was charged.</summary>
    [Fact]
    public async Task TheBoardingPaysTheCrewLocksTimeOnTopOfTheCrossing()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&station=1");
        await bench.RenderAsync();
        Assert.False(bench.OnSurface, "the bench landed without being asked — this test would measure nothing.");

        object stop = ((System.Collections.IEnumerable)bench.Call("ShuttleDestinationsInRange")!).Cast<object>()
            .Single(s => StationAboard.TryParseStationId((string)Get(Get(s, "Body")!, "Id")!, out _));
        double crossing = (double)Get(stop, "TravelSeconds")!;
        double before = SimTime(bench);

        await (Task)bench.CallOnTheDispatcher("BeginSurfaceExcursion", stop, ShuttleExcursion.Pack(0, 0, []), 0, null)!;

        Assert.True(bench.OnSurface);
        Assert.Equal(before + crossing + StationAboard.LockCycleSeconds, SimTime(bench), 3);
    }

    // ── A tube that will not let you through ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task EveryEndOfEverySeveredTubeReadsCoresOwnBlockageLineOffThePressedSpot()
    {
        DeskBench bench = await BootAsync();
        DeckPlan.ConsoleSpot[] ends = [.. Deck(bench).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.StationTube)];
        Assert.Equal(2 * StationWreck.SeveredTubes(Id).Count, ends.Length);

        foreach (DeckPlan.ConsoleSpot end in ends)
        {
            StationWreck.ModuleId arm = StationAboard.ArmOfTubeEnd(end.X, end.Y)!.Value;
            bench.Poke("_pulse", PulseSlot.Empty);   // so what the pulse says afterwards is what THIS press said
            StandAt(bench, end.X, end.Y);
            Press(bench);

            Assert.Contains(StationWreck.BlockageLine(Id, arm), bench.Pulse, StringComparison.Ordinal);
        }
    }

    // ── The boat ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The hop to the Habitat: a lock arm. The clock pays flight and lock, the captain is put down by
    /// Core's own square, the boat's dock moves with her — and the tank, the nerve and the magazine are
    /// exactly where they were.</summary>
    [Fact]
    public async Task AHopSpendsClockAndNothingElse_ARelocateIsNotARest()
    {
        DeskBench bench = await BootAsync();
        object ex = Excursion(bench);

        // Make every restorable thing partial, so a hop that topped any of them up would show.
        double budget = (double)Get(ex, "AirBudgetSeconds")!;
        double air = budget * 0.5;
        Set(ex, "AirSeconds", air);
        bench.Poke("_nerve", 40.0);
        object[] bots = [.. ((System.Collections.IEnumerable)Get(ex, "Bots")!).Cast<object>()];
        Assert.NotEmpty(bots);
        foreach (object b in bots) { Set(b, "Rounds", 3); }

        StationWreck.Access arrival = StationAboard.AccessOf(Id, StationWreck.ModuleId.Habitat);
        Assert.Equal(StationWreck.AccessKind.ServiceableLock, arrival.Kind);
        StationHop.Quote quoted = StationAboard.WithTheLock(
            StationHop.QuoteHop(Id, StationWreck.ModuleId.Hub, StationWreck.ModuleId.Habitat), arrival);

        DeckPlan.ConsoleSpot hop = HopTo(bench, StationWreck.ModuleId.Habitat);
        StandAt(bench, hop.X, hop.Y);
        double before = SimTime(bench);
        Press(bench);

        // The state AFTER the sequence.
        Assert.Equal(StationWreck.ModuleId.Habitat, Dock(bench));
        Assert.Equal(before + quoted.Seconds, SimTime(bench), 3);
        Assert.Equal((quoted.LandX, quoted.LandY), ((double)bench.Peek("_avatarX")!, (double)bench.Peek("_avatarY")!));
        Assert.Equal(StationWreck.ModuleId.Habitat, StationAboard.ModuleAt((double)bench.Peek("_avatarX")!, (double)bench.Peek("_avatarY")!)?.Id);

        Assert.Equal(air, (double)Get(ex, "AirSeconds")!, 6);
        Assert.Equal(40.0, (double)bench.Peek("_nerve")!, 6);
        Assert.All(bots, b => Assert.Equal(3, (int)Get(b, "Rounds")!));

        // The deck was rebuilt around the boat: her way home now stands at the Habitat's own lock, and what the
        // boat can reach is now everything the Habitat's walk group cannot.
        DeckPlan deck = Deck(bench);
        (double hx, double hy) = StationAboard.DockFixtureAt(arrival, 0);
        DeckPlan.ConsoleSpot home = Assert.Single(deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.ShuttleAirlock);
        Assert.Equal(((float)hx, (float)hy), (home.X, home.Y));
        Assert.Equal(
            StationHop.Destinations(Id, StationWreck.ModuleId.Habitat).Count,
            deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.StationHop));

        // The cast-off line is Core's own, with the honest total.
        Assert.Contains(StationHop.CastOffLine(quoted), Log(bench));
        Assert.Empty(bench.EscapedPastTheGate);
    }

    [Fact]
    public async Task TheLockLineIsToldOncePerVisitNoMatterHowManyLocksCycle()
    {
        DeskBench bench = await BootAsync();

        // Hop to the Habitat (a lock) and back to the hub (a lock): two more cycles after the boarding's own.
        DeckPlan.ConsoleSpot toHabitat = HopTo(bench, StationWreck.ModuleId.Habitat);
        StandAt(bench, toHabitat.X, toHabitat.Y);
        Press(bench);
        DeckPlan.ConsoleSpot back = HopTo(bench, StationWreck.ModuleId.Hub);
        StandAt(bench, back.X, back.Y);
        Press(bench);

        Assert.Equal(StationWreck.ModuleId.Hub, Dock(bench));
        Assert.Single(Log(bench), l => l == StationAboard.LockLine);
    }

    [Fact]
    public async Task AFaceWithNoLockIsRefusedWithoutACutterAndCostsNothing()
    {
        DeskBench bench = await BootAsync();
        object ex = Excursion(bench);
        Assert.DoesNotContain(
            ((IEnumerable<Satchel.Item>)bench.Peek("_satchel")!), HullCutter.IsTheCutter);

        DeckPlan.ConsoleSpot hop = HopTo(bench, StationWreck.ModuleId.Reactor);
        Assert.StartsWith("✂", hop.Label, StringComparison.Ordinal);
        StandAt(bench, hop.X, hop.Y);
        double before = SimTime(bench);
        Press(bench);

        StationHop.Quote refused = StationHop.QuoteHop(Id, StationWreck.ModuleId.Hub, StationWreck.ModuleId.Reactor);
        Assert.Equal(StationHop.Refusal.NeedsACut, refused.Refused);
        Assert.Contains(StationHop.RefusalLine(refused, StationWreck.ModuleOf(StationWreck.ModuleId.Hub).Name), bench.Pulse, StringComparison.Ordinal);
        Assert.Contains(HullCutter.NoCutterLine, Log(bench));
        Assert.Empty((HashSet<StationWreck.ModuleId>)Get(Get(ex, "Station")!, "Cuts")!);
        Assert.Equal(before, SimTime(bench), 6);
        Assert.Equal(StationWreck.ModuleId.Hub, Dock(bench));
    }

    /// <summary>The cut: a cell's worth of the captain's own #537 cutter, the cutter's own clock, a face that
    /// stays cut — and only then a flight.</summary>
    [Fact]
    public async Task ACutSpendsTheCaptainsOwnCutterAndOpensTheFaceForTheFlight()
    {
        DeskBench bench = await BootAsync();
        object ex = Excursion(bench);
        bench.Poke("_satchel", new List<Satchel.Item> { HullCutter.FreshRig });

        DeckPlan.ConsoleSpot hop = HopTo(bench, StationWreck.ModuleId.Reactor);
        StandAt(bench, hop.X, hop.Y);
        double before = SimTime(bench);
        Press(bench);

        // After the cut: one cut gone, the face recorded, the clock charged the cutter's seconds, the boat
        // still at the hub, and the console on the deck now names a flight instead of a cut.
        var satchel = (IReadOnlyList<Satchel.Item>)bench.Peek("_satchel")!;
        Assert.Equal(HullCutter.CutsPerCell - 1, HullCutter.CutsLeft(satchel));
        Assert.Contains(StationWreck.ModuleId.Reactor, (HashSet<StationWreck.ModuleId>)Get(Get(ex, "Station")!, "Cuts")!);
        Assert.Equal(before + HullCutter.CutSeconds, SimTime(bench), 3);
        Assert.Equal(StationWreck.ModuleId.Hub, Dock(bench));
        Assert.Contains(StationAboard.CutFaceLine, Log(bench));
        Assert.DoesNotContain(Deck(bench).Consoles, c => c.Label.StartsWith('✂'));

        // Now the flight is quoted, priced and flown — and it does not cut a second time.
        DeckPlan.ConsoleSpot fly = HopTo(bench, StationWreck.ModuleId.Reactor);
        Assert.StartsWith("🛸", fly.Label, StringComparison.Ordinal);
        StandAt(bench, fly.X, fly.Y);
        Press(bench);

        Assert.Equal(StationWreck.ModuleId.Reactor, Dock(bench));
        Assert.Equal(HullCutter.CutsPerCell - 1, HullCutter.CutsLeft((IReadOnlyList<Satchel.Item>)bench.Peek("_satchel")!));
        Assert.Single(Log(bench), l => l == StationAboard.CutFaceLine);
    }

    // ── What the HUD and the instruments say ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AboardTheStationTheTankRunsAwayFromTheBoatAndFillsAtItAndTheHudSaysSo()
    {
        DeskBench bench = await BootAsync();
        object ex = Excursion(bench);
        DeckPlan deck = Deck(bench);

        // At the dock: breathing the boat's air. In the middle of the drum, well away: the suit's own.
        StandAt(bench, deck.SpawnX, deck.SpawnY);
        Assert.Equal(SuitAir.Supply.Ship, bench.Call("AirSupplyOf", ex));
        StandAt(bench, 0, 0);
        Assert.NotEqual(SuitAir.Supply.Ship, bench.Call("AirSupplyOf", ex));

        // …and the gauge is handed the same answer, drawn, with no regolith fan on it.
        var hud = (DeckView.SurfaceHud)bench.Call("BuildSurfaceHud")!;
        Assert.False(hud.Instruments);
        Assert.Equal((double)Get(ex, "AirSeconds")!, hud.AirSeconds);
        Assert.Equal(bench.Call("AirSupplyOf", ex), hud.AirSupply);
        Assert.True(hud.AirDistanceHome > 0);
    }

    [Fact]
    public async Task NothingCrawlsUpOutOfHerDecks_NoTideAndNoPack()
    {
        DeskBench bench = await BootAsync();

        // Long enough to cross the arrival grace and several tide cadences on the regolith.
        for (int i = 0; i < 1500; i++)
        {
            bench.CallOnTheDispatcher("StepSurface", 0.1);
        }

        var reevers = (System.Collections.ICollection)bench.Peek("_reevers")!;
        Assert.Empty(reevers);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    [Fact]
    public async Task ABareDeckPressInTheFoundryDigsNothing()
    {
        DeskBench bench = await BootAsync();
        object ex = Excursion(bench);
        StationWreck.Module foundry = StationWreck.ModuleOf(StationWreck.ModuleId.Foundry);

        // A square of the Foundry's floor that sits inside the regolith's diggable band — the premise this guard
        // exists for. (The band starts at y = −27; the Foundry's centre is −28, so stand a little further in.)
        double x = foundry.CentreX, y = foundry.CentreY - 4;
        Assert.True(MoonSurface.IsDiggableGround(x, y, 0),
            "that square of the Foundry is no longer inside the regolith's diggable band — this guard has drifted.");

        StandAt(bench, x, y);
        Assert.Equal(DeckPlan.ConsoleKind.None, Deck(bench).NearestConsole(x, y));
        Press(bench);

        Assert.Null(Get(ex, "Channel"));
        Assert.Empty((System.Collections.IEnumerable)Get(ex, "Swept")!);
    }

    [Fact]
    public async Task TheWayHomeLeavesTheStation()
    {
        DeskBench bench = await BootAsync();
        DeckPlan deck = Deck(bench);
        DeckPlan.ConsoleSpot home = Assert.Single(deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.ShuttleAirlock);

        StandAt(bench, home.X, home.Y);
        Press(bench);

        Assert.False(bench.OnSurface, "the boat's own lock did not take the away team home.");
        Assert.Empty(bench.EscapedPastTheGate);
    }
}
