using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #869/#864 · THE PRESSES, DRIVEN — the desk edge, the memory buttons, and the board on the wall.
///
/// <para>Owner, 2026-08-13: <i>"it got up- and down buttons to move the table to work either with office
/// chair, Salli standing (lab) chair or by standing while using the table"</i>, <i>"love the gag of messing
/// with somebodys desks memorized height options :-D"</i>, and <i>"This lab has survived X days without
/// sarcasm on the wall."</i></para>
///
/// <h3>Why these guards are DRIVEN and not read</h3>
///
/// <para>Every fact below is asked of a REAL <see cref="Pages.Map"/> on a REAL generated floor: the deck is
/// the one the renderer builds, the captain is walked onto the console the pen drew, and the press goes
/// through <c>HandleDeckKey("e")</c> — the shipping key handler, the shipping dispatch. A guard that read the
/// source for the word <c>PressTheDeskEdge</c> would pass on a build where the arm had been deleted from the
/// switch, which is this repo's first named bug class.</para>
///
/// <para>…and the anti-vacuity clause that matters most here is the NEGATIVE one. This whole feature is
/// supposed to cost and pay NOTHING — no beat, no note, no coin, no thread — so the guards snapshot the
/// ledger either side of the press. A beat that fires a line and quietly banks a favour would satisfy every
/// positive assertion in this file and be the exact opposite of what the owner blessed.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDeskHumsAndRisesTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The first floor in the sweep that carries a sit-stand desk with both its controls on it —
    /// the generator's own floor, never a hand-typed room, because a guard handed a world it built itself
    /// cannot tell pass from fail.</summary>
    private static (string Body, int Level) AFloorWithADesk() =>
        FirstFloorWhere(deck =>
            deck.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.HiveDeskEdge)
            && deck.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.HiveDeskPresets));

    /// <summary>…and the first that carries the incident board.</summary>
    private static (string Body, int Level) AFloorWithTheBoard()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.Build(body, level, Field).TheBoard is not null)
                {
                    return (body, level);
                }
            }
        }
        throw new InvalidOperationException(
            "no site in the sweep hangs an incident board — #864 is not in this build at all.");
    }

    private static (string Body, int Level) FirstFloorWhere(Func<DeckPlan, bool> want)
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0);
                if (want(deck))
                {
                    return (body, level);
                }
            }
        }
        throw new InvalidOperationException(
            "no site in the sweep carries what this guard is about — it would be asserting about nothing.");
    }

    /// <summary>
    /// A live component on a real Hive floor, with nothing running but the deck. The one piece of theatre is
    /// the render handle — see <c>MustStandUpBeforeWalkingTests.OnTheFloor</c>, whose recipe this is.
    /// </summary>
    private static Pages.Map OnTheFloor(string body, int level)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the presses will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, level);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Walk the captain onto the first console of a kind, off the deck the game just built, so the
    /// press lands on the real thing.</summary>
    private static DeckPlan.ConsoleSpot StandAt(Pages.Map map, DeckPlan.ConsoleKind kind)
    {
        DeckPlan.ConsoleSpot[] spots = [.. ThePlan(map).Consoles.Where(c => c.Kind == kind)];
        Assert.True(spots.Length > 0,
            $"this floor carves no {kind} — the guard below would be asserting about a control that is not "
            + "there.");
        Set(map, "_avatarX", (double)spots[0].X);
        Set(map, "_avatarY", (double)spots[0].Y);
        return spots[0];
    }

    /// <summary>Press [E] where the captain is standing, through the shipping key handler.</summary>
    private static void PressE(Pages.Map map) =>
        Assert.True((bool)Invoke(map, "HandleDeckKey", "e")!, "the deck did not take the [E] key.");

    // ── THE LEDGER, SNAPSHOTTED ───────────────────────────────────────────────────────────────────────

    /// <summary>Everything this feature is forbidden to touch, in one object — so "it costs and pays
    /// nothing" is a comparison rather than four separate hopes.</summary>
    private sealed record Ledger(int Credits, int Notes, int Threads, int Satchel, int Kaamos, int Nebula)
    {
        public static Ledger Of(Pages.Map map) => new(
            (int)Must(map, "_credits")!,
            Count(Must(map, "_fieldNotes")),
            Count(Must(map, "_caseThreads")),
            Count(Must(map, "_satchel")),
            ((KaamosProgress)Must(map, "_kaamos")!).Count,
            ((NebulaProgress)Must(map, "_nebula")!).Count);

        private static int Count(object? list) =>
            list is System.Collections.ICollection c
                ? c.Count
                : throw new InvalidOperationException("that ledger field is not a list any more.");

        /// <summary>The field, or a failure — a snapshot of a name that has moved is a snapshot of nothing,
        /// and a guard that compares two nulls is the vacuous guard this house has a name for.</summary>
        private static object Must(Pages.Map map, string field) =>
            Get(map, field)
            ?? throw new InvalidOperationException(
                $"the component has no `{field}` — this ledger snapshot is reading a dead name.");
    }

    // ── (c) THE SIT-STAND BEAT ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #869 · [E] AT THE DESK EDGE RAISES IT, AND [E] AGAIN PUTS IT BACK — in that order, verbatim, and
    /// nothing else moves.
    ///
    /// <para><b>RED</b> by gutting the <c>HiveDeskEdge</c> arm of <c>InteractAtConsole</c>'s switch, which is
    /// the world of the base commit:</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    /// Expected: "🔼 The desk hums and rises to meet you. S"···
    /// Actual:   null
    /// </code>
    /// <para>…and <b>RED a second way</b> by making the press say <c>RaisedLine</c> every time (dropping the
    /// toggle), which is the honest mistake this beat invites:</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    ///            ↓ (pos 1)
    /// Expected: "🔽 It sinks back to chair height without "···
    /// Actual:   "🔼 The desk hums and rises to meet you. S"···
    /// </code>
    /// </summary>
    [Fact]
    public void TheDeskGoesUpAndThenBackDownAndCostsNothing()
    {
        (string body, int level) = AFloorWithADesk();
        Pages.Map map = OnTheFloor(body, level);
        StandAt(map, DeckPlan.ConsoleKind.HiveDeskEdge);

        Ledger before = Ledger.Of(map);

        PressE(map);
        Assert.Equal(SitStandDesk.RaisedLine, ThePulse(map));

        PressE(map);
        Assert.Equal(SitStandDesk.LoweredLine, ThePulse(map));

        // …and a third press starts the cycle again, because a motor is a motor.
        PressE(map);
        Assert.Equal(SitStandDesk.RaisedLine, ThePulse(map));

        // NOTHING ELSE MOVED. Not a coin, not a note, not a thread, not a thing in the satchel — and the
        // captain is standing exactly where they pressed, because raising a desk is not a place to be.
        Assert.Equal(before, Ledger.Of(map));
        Assert.True(Get(map, "_table") is null,
            "pressing the desk's own buttons sat somebody down — the chair is the sit, and this is not it.");
        Assert.True(Get(map, "_viewObject") is null,
            "raising a desk raised a card. This beat is one line and no furniture of its own.");
    }

    /// <summary>
    /// #869 · TWO DESKS ARE TWO DESKS. The raise is keyed per fitting, so walking to the next room and
    /// pressing does not report that somebody else's desk is coming back down.
    ///
    /// <para><b>RED</b> by keying the state on the floor alone (dropping the room and fitting from
    /// <c>DeskKey</c>) — the fourth named bug class in this house, one source consumed as if it were
    /// several:</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    ///            ↓ (pos 1)
    /// Expected: "🔼 The desk hums and rises to meet you. S"···
    /// Actual:   "🔽 It sinks back to chair height without "···
    /// </code>
    /// </summary>
    [Fact]
    public void TheDeskNextDoorIsADifferentDesk()
    {
        (string body, int level) = AFloorWithADesk();
        Pages.Map map = OnTheFloor(body, level);

        DeckPlan.ConsoleSpot[] edges =
            [.. ThePlan(map).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveDeskEdge)];
        Assert.True(edges.Length > 1,
            $"only {edges.Length} sit-stand desk(s) on this floor — this guard needs two to prove anything.");

        Set(map, "_avatarX", (double)edges[0].X);
        Set(map, "_avatarY", (double)edges[0].Y);
        PressE(map);
        Assert.Equal(SitStandDesk.RaisedLine, ThePulse(map));

        Set(map, "_avatarX", (double)edges[^1].X);
        Set(map, "_avatarY", (double)edges[^1].Y);
        PressE(map);
        Assert.Equal(SitStandDesk.RaisedLine, ThePulse(map));
    }

    // ── (d) THE READ, AND THEN THE PRANK ──────────────────────────────────────────────────────────────

    /// <summary>
    /// #869 · THE MEMORY BUTTONS — read them, then lean on them, and nothing anywhere is any different.
    ///
    /// <para>The read comes up as a CARD (the poster/monolith idiom, caption-only because there is no
    /// picture of a preset button); the prank is a line you walk away from. Both fire from the same console
    /// and the same key, and the order is the gumshoe's: look, then meddle.</para>
    ///
    /// <para><b>RED</b> two ways, and both land on the same sentence because both lose the same half.
    /// Gutting the <c>HiveDeskPresets</c> arm of the switch, and separately making every press the prank:</para>
    /// <code>
    /// System.InvalidOperationException : the desk's presets were pressed and no card came up — the read is
    /// the gumshoe half and it is the half that is missing.
    /// </code>
    /// </summary>
    [Fact]
    public void ThePresetsAreReadFirstAndLeanedOnAfterAndFileNothing()
    {
        (string body, int level) = AFloorWithADesk();
        Pages.Map map = OnTheFloor(body, level);
        StandAt(map, DeckPlan.ConsoleKind.HiveDeskPresets);

        Ledger before = Ledger.Of(map);

        // THE READ — a card, with the owner's own sentence on it and no picture at all.
        PressE(map);
        object card = Get(map, "_viewObject")
            ?? throw new InvalidOperationException(
                "the desk's presets were pressed and no card came up — the read is the gumshoe half and it "
                + "is the half that is missing.");
        Assert.Equal(SitStandDesk.ReadLine, (string?)Prop(card, "Caption"));
        Assert.True((string?)Prop(card, "ImageUrl") is null,
            "the presets came up with an art slot. There is no picture of a memory button.");

        // [E] again puts the card down, through the one door every road out of a card comes through.
        PressE(map);
        Assert.True(Get(map, "_viewObject") is null, "the card would not go away.");

        // THE PRANK — and it is a line, not a card, because it is something you do in passing.
        PressE(map);
        Assert.Equal(SitStandDesk.PrankLine, ThePulse(map));
        Assert.True(Get(map, "_viewObject") is null,
            "leaning on the buttons raised a card. Nothing about the prank is worth stopping for.");

        // …and again, as often as you like, because it costs and pays nothing.
        PressE(map);
        Assert.Equal(SitStandDesk.PrankLine, ThePulse(map));

        Assert.Equal(before, Ledger.Of(map));
    }

    // ── (b) THE BOARD ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #864 · THE BOARD IS ON THE FLOOR, IT SAYS WHAT IT SAYS, AND [E] GIVES THE FINE PRINT.
    ///
    /// <para><b>RED</b> by dropping the board's console out of <c>HiveInterior</c>:</para>
    /// <code>
    /// the floor that Core says carries an incident board draws no console for it.
    /// </code>
    /// <para>…and <b>RED a second way</b> by handing the card the TITLE instead of the fine print, which is
    /// the mistake a plate-and-card pair invites:</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    ///           ↓ (pos 0)
    /// Expected: "The zero is not printed. It is a card in "···
    /// Actual:   "🗓 THIS LABORATORY HAS OPERATED 0 DAYS WI"···
    /// </code>
    /// </summary>
    [Fact]
    public void TheBoardHangsOnTheDeckAndItsPressIsTheFinePrint()
    {
        (string body, int level) = AFloorWithTheBoard();
        Pages.Map map = OnTheFloor(body, level);

        IncidentBoard.Board board = UndergroundComplex.Build(body, level, Field).TheBoard!.Value;

        DeckPlan.ConsoleSpot spot = ThePlan(map).Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.ViewObject
                && string.Equals(c.Label, IncidentBoard.Title, StringComparison.Ordinal))
            .DefaultIfEmpty()
            .First();
        Assert.True(spot.Label is { Length: > 0 },
            "the floor that Core says carries an incident board draws no console for it.");
        Assert.Equal(board.X, spot.X, 3);
        Assert.Equal(board.Y, spot.Y, 3);

        Set(map, "_avatarX", (double)spot.X);
        Set(map, "_avatarY", (double)spot.Y);

        Ledger before = Ledger.Of(map);

        PressE(map);
        object card = Get(map, "_viewObject")
            ?? throw new InvalidOperationException("[E] at the incident board raised nothing at all.");
        Assert.Equal(IncidentBoard.Title, (string?)Prop(card, "Label"));
        Assert.Equal(IncidentBoard.FinePrint, (string?)Prop(card, "Caption"));
        Assert.True((string?)Prop(card, "ImageUrl") is null,
            "the board came up with an art slot. #864 is a text board and says so.");

        // …and nobody in-world finds it funny, which in this file means: nothing is recorded about it.
        Assert.Equal(before, Ledger.Of(map));
    }

    // ── (e) THE SADDLE AND THE CHAIR ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #869 · A LABORATORY'S SEATS SAY SADDLE STOOL AND AN ADMINISTRATION ROOM'S SAY OFFICE CHAIR — on the
    /// deck the pen actually draws, over every site in the sweep.
    ///
    /// <para>Core answers which (<c>ChamberFitting.SeatPlateFor</c>); this is the clause that says the
    /// renderer ASKED. <b>RED</b> by putting <c>ChamberFitting.StoolPlate</c> back on every chamber seat,
    /// which is the base commit:</para>
    /// <code>
    /// 177 seat(s) are labelled wrong:
    ///   luna B1 (ADMINISTRATION) — a seat says "🪑 A STOOL AT THE BENCH — SIT DOWN" in an administration chamber.
    ///   luna B2 (LABORATORIES) — a seat says "🪑 A STOOL AT THE BENCH — SIT DOWN" on a laboratories floor.
    /// </code>
    /// </summary>
    [Fact]
    public void TheLabSeatsSaySaddleAndTheOfficesSayChair()
    {
        var wrong = new List<string>();
        int labSeats = 0, officeSeats = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                string? department = ChamberFitting.DepartmentOn(body, level);
                bool labs = ChamberFitting.LabsOn(department);
                bool offices = department is "ADMINISTRATION";
                if (!labs && !offices)
                {
                    continue;
                }

                DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0);
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);

                // Only the chambers' own seats. The park block's suites are on their own plates (#817) and
                // are a different room in a different building.
                var chamberSeats = new HashSet<(float, float)>();
                foreach (UndergroundComplex.Room room in floor.TheRooms)
                {
                    if (room.Kind != UndergroundComplex.RoomKind.Chamber)
                    {
                        continue;
                    }
                    foreach (RingOffice.Chair seat in room.Seats)
                    {
                        chamberSeats.Add(((float)seat.X, (float)seat.Y));
                    }
                }

                foreach (DeckPlan.ConsoleSpot c in deck.Consoles)
                {
                    if (c.Kind != DeckPlan.ConsoleKind.HiveOfficeChair || !chamberSeats.Contains((c.X, c.Y)))
                    {
                        continue;
                    }

                    if (labs)
                    {
                        labSeats++;
                        if (!c.Label.Contains("SADDLE", StringComparison.Ordinal))
                        {
                            wrong.Add(
                                $"  {body} B{-level} ({department}) — a seat says \"{c.Label}\" on a "
                                + "laboratories floor.");
                        }
                    }
                    else
                    {
                        officeSeats++;
                        if (!string.Equals(c.Label, RingOffice.FreeChairPlate, StringComparison.Ordinal))
                        {
                            wrong.Add(
                                $"  {body} B{-level} ({department}) — a seat says \"{c.Label}\" in an "
                                + "administration chamber.");
                        }
                    }
                }
            }
        }

        Assert.True(labSeats > 50, $"only {labSeats} laboratory seat(s) drawn — this proved little.");
        Assert.True(officeSeats > 20, $"only {officeSeats} office seat(s) drawn — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} seat(s) are labelled wrong:{Environment.NewLine}"
            + string.Join(Environment.NewLine, wrong.Take(12)));
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static string? ThePulse(Pages.Map map) => ((PulseSlot)Get(map, "_pulse")!).Message;

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    private static object? Prop(object o, string name) =>
        o.GetType().GetProperty(name, Hidden)!.GetValue(o);

    /// <summary>#870 lane 6b - The five seat fields live on the page's <c>_seating</c> object now, so this
    /// follows them there (<see cref="SeatState"/>); every assertion below still asks for the state by the
    /// name it was written with.</summary>
    private static object? Get(object o, string field) =>
        SeatState.TryFollow(o, field, out object? seated)
            ? seated
            : o.GetType().GetField(field, Hidden)?.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }
}
