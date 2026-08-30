using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1016 · THE CAPTAIN CAN SIT DOWN ON HIS OWN SHIP.
///
/// <para><b>Owner, on 7 Deck, looking at the room:</b> <i>"Why no table here to sit at?"</i>, then <i>"Why no
/// table in cabin either?"</i>, and then the ruling that names the lane — <i>"I expect to have a bar table
/// like this in this ships galley also.... feature complete."</i></para>
///
/// <para>Her cantina had drawn three tops since the deck plan was written and every one of them was
/// DRESSING: furniture with no console over it, so [E] there answered nothing at all — the same absence
/// #757 found in a Hive hall and #973 L0 found in a station bar, arriving last in the one room the player
/// owns. Her cabins had a bunk and a backdrop.</para>
///
/// <para>Three claims, driven on a real page through the verb [E] actually dispatches to: the room offers
/// the chair, sitting in it is a real sitting (the ship's own key, the ship's own words, the body snapped
/// onto a square the STONE chose), and the berth's desk is the one seat aboard that is behind a door.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShipHasSeatsAboardTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    // ── THE ROOM OFFERS THE CHAIR ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HER CANTINA PUBLISHES TOPS THE CAPTAIN CAN TAKE, and every one of them is ON a top the pen draws.
    ///
    /// <para>The console the press lands on and the table the player can see have to be one piece of
    /// furniture — §13.15, and this ship's own named bug: two numbers for one fixture is how four console
    /// collisions shipped on her. So the seats are asserted to sit exactly on entries of
    /// <see cref="DeckPlan.Tables"/> rather than at coordinates this test knows.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the publishing loop out of <c>DeckPlan.BuildShip</c>: the count
    /// falls to zero and the assert names the room.</para>
    /// </summary>
    [Fact]
    public void HerCantinaPublishesTopsTheCaptainCanTake()
    {
        DeckPlan ship = DeckPlan.Ship;
        DeckPlan.ConsoleSpot[] seats =
            [.. ship.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.BarTop)];

        Assert.True(seats.Length > 0,
            "7 Deck publishes no takeable top at all — the cantina is back to being dressing, which is the "
            + "state the owner filed #1016 about.");

        foreach (DeckPlan.ConsoleSpot seat in seats)
        {
            Assert.Contains(ship.Tables,
                t => Math.Abs(t.X - seat.X) < 1e-6 && Math.Abs(t.Y - seat.Y) < 1e-6);
        }
    }

    /// <summary>
    /// …AND THE GALLEY DESK KEEPS ITS OWN PRESS. The CANTINA console switches to the galley card and must go
    /// on doing exactly that, so the top that stands 1.5 du under it does NOT grow a seat — the deck audit's
    /// own label law (<see cref="DeckPlan.LabelClearance"/>) decides which tops do, asked of the console list
    /// itself rather than by hand.
    ///
    /// <para>Anti-vacuous in both directions: at least one top IS refused (or the law is selecting
    /// everything), and the cantina console still answers from on top of itself.</para>
    /// </summary>
    [Fact]
    public void TheGalleyDeskKeepsItsPressAndTheTopUnderItGetsNoSeat()
    {
        DeckPlan ship = DeckPlan.Ship;
        DeckPlan.ConsoleSpot galley =
            ship.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.Cantina);

        DeckPlan.ConsoleSpot? answering = ship.NearestConsoleSpot(galley.X, galley.Y);
        Assert.NotNull(answering);
        Assert.Equal(DeckPlan.ConsoleKind.Cantina, answering!.Value.Kind);

        int refused = ship.Tables.Count(t =>
            !ship.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.BarTop
                                    && Math.Abs(c.X - t.X) < 1e-6 && Math.Abs(c.Y - t.Y) < 1e-6));
        Assert.True(refused > 0,
            "every top got a seat, so the label law is selecting everything and this guard proves nothing.");

        foreach (DeckPlan.TableTop top in ship.Tables)
        {
            bool takeable = ship.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.BarTop
                                                   && Math.Abs(c.X - top.X) < 1e-6
                                                   && Math.Abs(c.Y - top.Y) < 1e-6);
            double dx = top.X - galley.X, dy = top.Y - galley.Y;
            bool clear = Math.Sqrt((dx * dx) + (dy * dy)) >= DeckPlan.LabelClearance;
            Assert.Equal(clear, takeable);
        }
    }

    // ── SITTING AT ONE IS A REAL SITTING ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// [E] AT A TOP IN HER CANTINA SITS THE CAPTAIN DOWN — the ship's own key, the ship's own words, and the
    /// body on the square the stone chose.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>ShipDesk</c>/<c>BarTop</c> fall-through from
    /// <c>TheBarTopUnderfoot</c>: the press answers null and the captain stays on his feet.</para>
    /// </summary>
    [Fact]
    public void PressingATopInHerCantinaOpensASitting()
    {
        Pages.Map map = Aboard();
        Assert.Null(Seated(map));

        DeckPlan.ConsoleSpot top = SeatConsoles(map).First();
        StandAt(map, top.X, top.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, "a top in her own cantina refused [E].");

        object seat = Seated(map)!;
        Assert.True((bool)Get(seat, "Solo")!);
        Assert.True((bool)Get(seat, "Joined")!, "there is nobody aboard to ask — the table is simply taken.");
        Assert.True((bool)Get(seat, "Aboard")!);
        Assert.False((bool)Get(seat, "Quiet")!, "a cantina is a room with a counter and a window in it.");
        Assert.False((bool)Get(seat, "TheyCameToYou")!);
        Assert.Equal(SittingAlone.OwnTablePlate, (string)Get(seat, "Plate")!);

        // …AND IT SAYS WHERE IT IS. The strip's company clause is built out of the scene's own Setting, and
        // the canteen's constant would announce a captain sitting on his own boat as being in a mining hall.
        string setting = (string)Get(Get(seat, "Scene")!, "Setting")!;
        Assert.Equal(SittingAlone.ShipCantinaSetting, setting);
        Assert.NotEqual(SittingAlone.Setting, setting);

        // The key is the SHIP's own, and it must never collide with a berth's or a Hive floor's.
        string key = (string)Get(seat, "Key")!;
        Assert.StartsWith("ship:cantina:", key, StringComparison.Ordinal);

        // #820's snap: the body is ON the chair, at the place the ROOM sounded, never one measured here.
        var deck = (DeckPlan)Field(map, "_deckPlan")!;
        DeckReachability.Point chair = HavenInterior.BesideATop(
            new DeckReachability.Point(top.X, top.Y), DeckPlan.AvatarRadius, deck.CollisionField)!.Value;
        Assert.Equal(chair.X, (double)Field(map, "_avatarX")!, 6);
        Assert.Equal(chair.Y, (double)Field(map, "_avatarY")!, 6);

        // …and standing up closes it exactly as it does at every other seat in the game.
        Assert.True((bool)Invoke(map, "StandUpBeforeWalking")!);
        Assert.Null(Seated(map));
    }

    /// <summary>A second press at the same top is CONSUMED and changes nothing — [E] is not how you stand up
    /// (#680: re-opening would wipe the line somebody is in the middle of reading).</summary>
    [Fact]
    public void PressingAgainDoesNotReopenTheSitting()
    {
        Pages.Map map = SatInTheCantina();
        object first = Seated(map)!;
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!);
        Assert.Same(first, Seated(map));
    }

    // ── THE DESK IN THE BERTH ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE DESK IN CABIN 1 SEATS THE CAPTAIN ON THE CABINET RUNG — the one seat aboard behind a door, and
    /// therefore the one where the case may be spread unconditionally.
    ///
    /// <para><b>Proven RED</b> by handing the desk <c>Quiet: false</c> in <c>Map.ShipSeats.cs</c>: the rung
    /// falls back to <c>HallTable</c> and the spread is refused in a room with a door on it.</para>
    /// </summary>
    [Fact]
    public void TheDeskInTheCabinSeatsYouOnTheCabinetRung()
    {
        Pages.Map map = Aboard();
        DeckPlan.ConsoleSpot desk = TheDesk(map);
        StandAt(map, desk.X, desk.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, "the desk in CABIN 1 refused [E].");

        object seat = Seated(map)!;
        Assert.Equal("ship:cabin:desk", (string)Get(seat, "Key")!);
        Assert.True((bool)Get(seat, "Quiet")!);
        Assert.True((bool)Get(seat, "Aboard")!);
        Assert.Equal(SittingAlone.OwnDeskPlate, (string)Get(seat, "Plate")!);
        Assert.Equal(SittingAlone.ShipCabinSetting, (string)Get(Get(seat, "Scene")!, "Setting")!);
        Assert.Equal(0, (int)Get(seat, "Cabinet")!);

        Assert.Equal(SeatedHud.Seat.Cabinet, (SeatedHud.Seat?)Invoke(map, "get_SeatedIn"));
        Assert.True((bool)Invoke(map, "get_CanSpreadTheCaseHere")!,
            "a room with a door a step away is where the papers come out.");

        // The body is on the square the STONE chose, and the desk is in the berth it says it is in.
        Assert.Equal(ShipLayout.DeskCabin,
            ShipLayout.CompartmentAt((double)Field(map, "_avatarX")!, (double)Field(map, "_avatarY")!));
    }

    /// <summary>
    /// THE BUNK AND THE DESK DO NOT FIGHT OVER [E] — from the chair the desk answers, from the berth's
    /// middle the bunk does.
    ///
    /// <para>This is the whole reason <see cref="ShipLayout.CabinDeskStation"/> is in a CORNER: the chair is
    /// sounded one body-width off the fixture on the first side the stone allows, and in the corner the hull
    /// and the divider refuse three of the four. A captain who sat down to work and pressed [E] into a
    /// night's sleep is exactly the crowding the owner has caught by eye twice on this ship.</para>
    /// </summary>
    [Fact]
    public void TheBunkAndTheDeskDoNotFightOverTheKey()
    {
        Pages.Map map = Aboard();
        var deck = (DeckPlan)Field(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot desk = TheDesk(map);
        DeckPlan.ConsoleSpot bunk = deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.Bunk);

        DeckReachability.Point chair = HavenInterior.BesideATop(
            new DeckReachability.Point(desk.X, desk.Y), DeckPlan.AvatarRadius, deck.CollisionField)!.Value;

        Assert.Equal(DeckPlan.ConsoleKind.ShipDesk,
            deck.NearestConsoleSpot(chair.X, chair.Y)!.Value.Kind);
        Assert.Equal(DeckPlan.ConsoleKind.Bunk,
            deck.NearestConsoleSpot(bunk.X, bunk.Y)!.Value.Kind);
    }

    // ── AND NOBODY IS EVER GOING TO COME ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WAIT BEAT SAYS ITS LINE, IN THE BOAT'S OWN WORDS, AND NOBODY EVER ARRIVES.
    ///
    /// <para>Sitting alone ashore is a choice to be FINDABLE and the room may send somebody over. Her crew is
    /// three droids on a fixed patrol, so aboard the answer is always the silence — and the silence is the
    /// ship's (<see cref="SittingAlone.NobodyCameAboard"/>) rather than an eighty-seat hall's. Both lines of
    /// the pool are walked, so a pool that had lost a line would redden.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>t.Aboard</c> arm from the wait beat's ladder: the boat
    /// answers with the hall's trays and eighty chairs.</para>
    ///
    /// <para><b>And an honest note about the OTHER clause.</b> <c>!t.Aboard</c> on the approach cannot be
    /// shown red today, because the clause beside it (<c>ex is not null</c>) already refuses at a seat with
    /// no excursion behind it, and the ship's two are exactly those. It is written all the same, and this
    /// assertion stands over it, for the reason the office rung's own flag is written: the law is <i>nobody
    /// crosses the floor of your own boat</i>, and a law that holds only because of a second clause's
    /// accident is the shape of a bug rather than of a rule.</para>
    /// </summary>
    [Fact]
    public void NobodyEverComesAboardAndTheWaitSaysSoInTheBoatsOwnWords()
    {
        Pages.Map map = SatInTheCantina();

        for (int beat = 0; beat < SittingAlone.NobodyCameShipCantina.Count + 1; beat++)
        {
            Wait(map);
            object seat = Seated(map)!;
            Assert.Equal(SittingAlone.NobodyCameAboard(cabin: false, beat), (string)Get(seat, "Outcome")!);
            Assert.True((bool)Get(seat, "Solo")!, "somebody took the chair opposite on a boat with no crew.");
            Assert.False((bool)Get(seat, "TheyCameToYou")!);
        }

        // …and the berth's desk hears the same silence through a door, which is a different sentence.
        Pages.Map berth = SatAtTheDesk();
        Wait(berth);
        Assert.Equal(SittingAlone.NobodyCameAboard(cabin: true, 0), (string)Get(Seated(berth)!, "Outcome")!);
        Assert.NotEqual(
            SittingAlone.NobodyCameAboard(cabin: false, 0), SittingAlone.NobodyCameAboard(cabin: true, 0));
    }

    /// <summary>
    /// AND THE BUTTONS ARE LIVE. A seat with no <c>SurfaceExcursion</c> under it used to render its whole
    /// move row DISABLED — the gate opened on the excursion, so #973 L5b's bar top shipped a strip you could
    /// sit at and not press, and the ship's seats would have inherited it. A control that is drawn and does
    /// nothing is #603's founding complaint.
    ///
    /// <para><b>Proven RED</b> by putting <c>_host.Surface is { } ex &amp;&amp;</c> back on the front of
    /// <c>TableMoveOnOffer</c>.</para>
    /// </summary>
    [Fact]
    public void EveryMoveOnTheShipsOwnStripIsPressable()
    {
        Pages.Map map = SatInTheCantina();
        var moves = (IReadOnlyList<Encounter.Move>)Invoke(map, "TableMovesOnTheTable")!;

        Assert.Contains(moves, m => m.Id == SittingAlone.Wait);
        Assert.Contains(moves, m => m.Id == SittingAlone.Stand);
        foreach (Encounter.Move move in moves)
        {
            Assert.True((bool)Invoke(map, "TableMoveOnOffer", move)!,
                $"`{move.Id}` is drawn on the ship's own strip and cannot be pressed.");
        }
    }

    // ── AND THE CASE COMES OUT AT ONE OF THEM ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>WORK THE CASE, AT A TOP IN HER OWN CANTINA — the two halves of #1016 meeting.</b>
    ///
    /// <para>This lane built the SEATS; <c>fix/1016-work-the-case-anywhere</c> (#1017) took the case-work
    /// verbs off the ground so a sitting with no <c>SurfaceExcursion</c> under it can spread the satchel,
    /// dig a sheet and have the book keep it. Neither half is worth much without the other, and the seam
    /// between two lanes is exactly where a feature ships broken — so this drives the whole thing at a top
    /// on the captain's own boat: the spread opens, the dig takes its seconds, and the book files the entry
    /// <b>under the boat</b> rather than under a moon or a station.</para>
    ///
    /// <para>It fails on an INSTANT write as well as on a dead one: half a document in, the strip has a bar
    /// to draw and the book is still empty.</para>
    /// </summary>
    [Fact]
    public void WorkTheCaseDigsASheetAtATopInHerOwnCantina()
    {
        Pages.Map map = SatInTheCantina();
        Invoke(map, "SeedTheSpreadFinds");

        // The door is open at this seat, and it says so rather than being quietly missing (#603).
        Assert.Null(Invoke(map, "get_SpreadRefusal"));
        Assert.True((bool)Invoke(map, "get_CanSpreadTheCaseHere")!);
        Assert.Contains((List<Satchel.Item>)Field(map, "_satchel")!, i => i.Kind == Satchel.Kind.Paper);

        Invoke(map, "OpenTheSpread");
        Assert.True((bool)Field(map, "_showSatchel")!, "the spread did not open at a top on her own deck.");

        var paper = new Satchel.Item(Satchel.Kind.Paper, "spread-demo-1");
        Assert.True((bool)Invoke(map, "CanWriteUp", paper)!);
        Invoke(map, "WriteItUp", paper);

        // Nothing is spent on the press: an interruption has to have something to undo (#696).
        Assert.NotNull(Field(map, "_processing"));
        Assert.Empty((List<FieldNote>)Field(map, "_fieldNotes")!);

        Invoke(map, "StepProcessing", Processing.SecondsPerDocument / 2);
        double half = (double)Invoke(map, "ProcessingFraction")!;
        Assert.True(half is > 0.3 and < 0.7,
            $"half a document in, the strip's bar reads {half:F2} — the clock is not the document's clock.");
        Assert.Empty((List<FieldNote>)Field(map, "_fieldNotes")!);

        Invoke(map, "StepProcessing", Processing.SecondsPerDocument);
        Assert.Null(Field(map, "_processing"));

        // THE ENTRY, in the one book, filed under the room the captain is actually sitting in — which on
        // this deck is a boat and not a berth, a hall or a moon.
        FieldNote wrote = Assert.Single((List<FieldNote>)Field(map, "_fieldNotes")!);
        Assert.Contains(FieldNotes.YourOwnBoat, wrote.Place, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("regolith", wrote.Text, StringComparison.OrdinalIgnoreCase);
    }

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live page standing on her own deck, with nothing running but the deck itself. The render
    /// handle is the same piece of theatre every seat bench in this repository uses.</summary>
    private static Pages.Map Aboard()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Set(map, "_deckMode", true);
        Set(map, "_deckPlan", DeckPlan.Ship);
        return map;
    }

    private static Pages.Map SatInTheCantina()
    {
        Pages.Map map = Aboard();
        DeckPlan.ConsoleSpot top = SeatConsoles(map).First();
        StandAt(map, top.X, top.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!);
        return map;
    }

    private static Pages.Map SatAtTheDesk()
    {
        Pages.Map map = Aboard();
        DeckPlan.ConsoleSpot desk = TheDesk(map);
        StandAt(map, desk.X, desk.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!);
        return map;
    }

    /// <summary>One press of SIT A WHILE, through the very handler the strip's button is wired to.</summary>
    private static void Wait(Pages.Map map) =>
        ((Task)Invoke(map, "TableMoveClicked", SittingAlone.Wait)!).GetAwaiter().GetResult();

    private static IEnumerable<DeckPlan.ConsoleSpot> SeatConsoles(Pages.Map map) =>
        ((DeckPlan)Field(map, "_deckPlan")!).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.BarTop);

    private static DeckPlan.ConsoleSpot TheDesk(Pages.Map map) =>
        ((DeckPlan)Field(map, "_deckPlan")!).Consoles
            .Single(c => c.Kind == DeckPlan.ConsoleKind.ShipDesk);

    private static void StandAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    private static object? Seated(Pages.Map map) => Invoke(map, "get_SeatedTable");

    // ── Reflection plumbing ──────────────────────────────────────────────────────────────────────────────

    private static object? Field(Pages.Map map, string name) =>
        (typeof(Pages.Map).GetField(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name."))
        .GetValue(map);

    private static void Set(Pages.Map map, string name, object? value)
    {
        if (typeof(Pages.Map).GetField(name, Hidden) is { } field)
        {
            field.SetValue(map, value);
            return;
        }

        (typeof(Pages.Map).GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}`.")).SetValue(map, value);
    }

    private static object? Get(object o, string name) =>
        (o.GetType().GetField(name, Hidden)?.GetValue(o))
        ?? (o.GetType().GetProperty(name, Hidden)
            ?? throw new InvalidOperationException($"{o.GetType().Name} has no `{name}`.")).GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name."))
        .Invoke(map, args);
}
