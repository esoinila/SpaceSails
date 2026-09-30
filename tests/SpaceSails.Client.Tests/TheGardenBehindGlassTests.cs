using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1332 B · <b>THE GARDEN BEHIND GLASS</b>, driven against the built deck and a live page. Owner, 2026-09-29:
/// <i>"the little garden on space ports could be used to produce salad, coffee etc. comforts for levels
/// sufficient for the restaurant."</i>
///
/// <para>Every claim is measured off <see cref="HavenInterior.DockedDeck"/> — the walls the pen draws and the
/// captain collides with, the labels it lays, the doors it hangs — rather than off the constants that built
/// them, so a guard cannot agree with whatever the builder did.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheGardenBehindGlassTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;

    /// <summary>Every haven with a bar — which the first guard below holds to being every haven there is.</summary>
    public static TheoryData<string> Gardens
    {
        get
        {
            var rows = new TheoryData<string>();
            foreach (string id in HavenInterior.InteriorBodyIds.Where(HavenInterior.HasGarden))
            {
                rows.Add(id);
            }

            return rows;
        }
    }

    private static DeckPlan Deck(string berth) => HavenInterior.DockedDeck(berth)!;

    private static (double X0, double Y0, double X1, double Y1) Box(string berth) =>
        HavenInterior.TheGardenBox(berth)!.Value;

    /// <summary>The whole complex with the garden's reach added, for the lattice sweeps.</summary>
    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds(string berth)
    {
        (double x0, _, _, _) = Box(berth);
        double west = Math.Min(x0, HavenInterior.TheGalleryBox(berth)?.X0 ?? x0);
        return (west - 4, -4, 40, 90);
    }

    // ── WHICH HAVENS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY HAVEN WITH A BAR HAS A GARDEN — AND ITS BOARD SAYS SO.</b> Swept over the whole catalogue: a
    /// haven with a keep behind its counter has the room, its plate on its concourse deck exactly once, and the
    /// menu line on its board; and all seven havens have a bar, so all seven have one.
    ///
    /// <para><b>Proven RED</b> by <c>HasGarden</c> answering false for The Deep: <c>the-deep has a bar and no
    /// garden</c> (and the Lobbies-style count 7 → 6).</para>
    /// </summary>
    [Fact]
    public void EveryHavenWithABarHasAGardenAndItsBoardSaysSo()
    {
        int gardens = 0;
        foreach (string berth in HavenInterior.InteriorBodyIds)
        {
            bool bar = Barkeeps.For(berth) is not null;
            Assert.True(bar == HavenInterior.HasGarden(berth), $"{berth} has a bar and no garden.");
            Assert.Equal(bar, HavenGarden.MenuLineAt(berth) is not null);
            if (!bar)
            {
                continue;
            }

            gardens++;
            Assert.Single(Deck(berth).RoomLabels, l => l.Text == HavenGarden.Plate);
        }

        Assert.Equal(7, gardens);
    }

    // ── THE WORDS ON THE ROOM ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PLATES, VERBATIM, AND THE BEDS IN THEIR ORDER FROM THE DOOR.</b> The room carries its one plate
    /// inside itself; each of the four beds carries its own plate on itself, once; and bed <c>i</c> is strictly
    /// further from THE door than bed <c>i − 1</c> — lettuce nearest, tomatoes furthest. Nothing else in the
    /// garden is written.
    ///
    /// <para><b>Proven RED</b> by laying the plates in reverse (<c>BedPlates[3 - i]</c> in
    /// <c>LayTheGarden</c>): <c>LETTUCE · 14 DAYS is not on the bed nearest the door</c>.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void ThePlatesAreVerbatimAndTheBedsRunInOrderFromTheDoor(string berth)
    {
        DeckPlan deck = Deck(berth);
        IReadOnlyList<(double X0, double Y0, double X1, double Y1)> beds = HavenInterior.TheGardenBedsAt(berth);
        Assert.Equal(HavenGarden.BedPlates.Count, beds.Count);

        DeckReachability.Point door = HavenInterior.TheGardenDoorsAt(berth)[0];
        double last = double.MinValue;
        for (int i = 0; i < beds.Count; i++)
        {
            (double x0, double y0, double x1, double y1) = beds[i];
            string plate = HavenGarden.BedPlates[i];
            (float lx, float ly, _) = Assert.Single(deck.RoomLabels, l => l.Text == plate);
            Assert.True(lx >= x0 && lx <= x1 && ly >= y0 && ly <= y1,
                $"{berth}: {plate} is not on the bed nearest the door but {i}.");

            double d = Math.Sqrt(Math.Pow(((x0 + x1) / 2) - door.X, 2) + Math.Pow(((y0 + y1) / 2) - door.Y, 2));
            Assert.True(d > last, $"{berth}: {plate} is not on the bed {i + 1}th from the door.");
            last = d;
        }

        // The room's plate is in the room, and it is the only other text in there.
        (float px, float py, _) = Assert.Single(deck.RoomLabels, l => l.Text == HavenGarden.Plate);
        Assert.True(HavenInterior.InTheGarden(berth, px, py), $"{berth}: the garden's plate is not in the garden.");
        string[] inside =
        [
            .. deck.RoomLabels.Where(l => HavenInterior.InTheGarden(berth, l.X, l.Y)).Select(l => l.Text),
        ];
        string[] canon = [HavenGarden.Plate, .. HavenGarden.BedPlates];
        Assert.Equal(canon.Order(StringComparer.Ordinal), inside.Order(StringComparer.Ordinal));

        // …and the room says its own name when the captain stands in it.
        Assert.Equal(HavenGarden.Plate, deck.Location(px, py));
    }

    /// <summary>
    /// <b>ONE LABEL PER FLOOR STILL HOLDS.</b> The garden is the concourse's room and its plate is a room plate
    /// on the concourse — the observation walk's own standing — never a label on the floor below: every lower
    /// level still carries exactly its one label, and the garden does not exist at level −1.
    ///
    /// <para><b>Proven RED</b> by <c>InTheGarden</c> dropping its level clause: the point in the garden's box
    /// reads as in the garden on the service level too.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void TheGardensPlateIsARoomPlateOnTheConcourseAndNotASecondFloorLabel(string berth)
    {
        DeckPlan below = HavenInterior.DockedDeck(berth, level: HavenLevels.ServiceLevel)!;
        Assert.Single(below.RoomLabels);
        Assert.DoesNotContain(below.RoomLabels, l => l.Text == HavenGarden.Plate);

        (float px, float py, _) = Assert.Single(Deck(berth).RoomLabels, l => l.Text == HavenGarden.Plate);
        Assert.True(HavenInterior.InTheGarden(berth, px, py, HavenLevels.Concourse));
        Assert.False(HavenInterior.InTheGarden(berth, px, py, HavenLevels.ServiceLevel));
    }

    // ── THE FIRE CODE AND THE WAY IN ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>TWO DOORS, AND EITHER ONE IS A WAY OUT.</b> Fire code #822: the garden is bigger than bedroom-small,
    /// so it needs <see cref="UndergroundComplex.FireCodeMinExits"/> ways out and no exemption. Every door the
    /// plan hangs within a body of the room's box is counted: exactly two, both unlocked, and they are the two
    /// the room publishes. Then each is WELDED SHUT in turn and the whole garden is swept again from the bar's
    /// threshold — it must still all be reachable through the other, which is what "two ways out" means.
    ///
    /// <para><b>Proven RED</b> by cutting the growers' face as a sealed berth (dropping edge 3 from
    /// <c>TheGardenOpensOnEdge</c>): <c>1 way(s) into the garden</c>, and with THE door welded the garden
    /// drops out of the reachable set whole.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void TheGardenHasTwoDoorsAndEitherOneIsAWayOut(string berth)
    {
        DeckPlan deck = Deck(berth);
        (double x0, double y0, double x1, double y1) = Box(berth);
        const double Reach = DeckPlan.AvatarRadius;

        var ways = new List<DeckPlan.Door>();
        foreach (DeckPlan.Door door in deck.Doors)
        {
            double mx = (door.X1 + door.X2) / 2.0, my = (door.Y1 + door.Y2) / 2.0;
            if (mx >= x0 - Reach && mx <= x1 + Reach && my >= y0 - Reach && my <= y1 + Reach
                && !IsTheBarsWideDoor(door))
            {
                ways.Add(door);
            }
        }

        Assert.True(ways.Count == UndergroundComplex.FireCodeMinExits,
            $"{berth}: {ways.Count} way(s) into the garden — the fire code wants {UndergroundComplex.FireCodeMinExits}.");
        Assert.All(ways, w => Assert.False(w.Locked, "a way out of the garden is a leaf nobody is refused at."));
        foreach (DeckReachability.Point published in HavenInterior.TheGardenDoorsAt(berth))
        {
            Assert.Contains(ways, w => Math.Abs(((w.X1 + w.X2) / 2.0) - published.X) < 1e-3
                                       && Math.Abs(((w.Y1 + w.Y2) / 2.0) - published.Y) < 1e-3);
        }

        // Each door alone is a way in and out: weld the other and sweep.
        foreach (DeckPlan.Door shut in ways)
        {
            var walls = new List<SurfaceCollision.Segment>(deck.CollisionField)
            {
                new(shut.X1, shut.Y1, shut.X2, shut.Y2),
            };
            (int standable, int missed) = Sweep(berth, walls);
            Assert.True(standable > 150, $"{berth}: only {standable} standable tiles in the garden.");
            Assert.True(missed == 0,
                $"{berth}: with the door at ({(shut.X1 + shut.X2) / 2:0.0}, {(shut.Y1 + shut.Y2) / 2:0.0}) shut, "
                + $"{missed} of {standable} garden tiles cannot be reached — that door was the only way out.");
        }
    }

    /// <summary>The bar's own wide door stands on the hall's north edge, beside the growers' face — it is the
    /// bar's, not the garden's, and is told apart by its span.</summary>
    private static bool IsTheBarsWideDoor(DeckPlan.Door d) =>
        Math.Abs(d.Y1 - d.Y2) < 1e-3 && Math.Abs(d.X2 - d.X1) > 6;

    /// <summary>
    /// <b>EVERY STANDABLE TILE OF THE GARDEN IS REACHABLE FROM THE CONCOURSE</b>, on the captain's own lattice
    /// — the beds, the plank and the glass leave no corner of the room that is floor you can see and not walk
    /// to (#600 with a lettuce in it).
    ///
    /// <para><b>Proven RED</b> by carving both doorways as sealed berths (the garden branch in
    /// <c>CutTheRing</c> removed): every tile of the room drops out of the set.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void EveryStandableTileOfTheGardenIsReachable(string berth)
    {
        (int standable, int missed) = Sweep(berth, Deck(berth).CollisionField);
        Assert.True(standable > 150, $"{berth}: only {standable} standable tiles in the garden — that is not a room.");
        Assert.True(missed == 0, $"{berth}: {missed} of {standable} standable garden tile(s) cannot be walked to.");
    }

    private static (int Standable, int Missed) Sweep(string berth, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        (double x, double y, _) = HavenInterior.BarThreshold;
        const double Step = 0.5;
        IReadOnlyCollection<DeckReachability.Point> reached = DeckReachability.Reachable(
            new DeckReachability.Point(x, y), walls, DeckPlan.AvatarRadius, Bounds(berth), Step);
        var got = new HashSet<(int, int)>(
            reached.Select(p => ((int)Math.Round(p.X / Step), (int)Math.Round(p.Y / Step))));

        (double x0, double y0, double x1, double y1) = Box(berth);
        int standable = 0, missed = 0;
        for (double px = x0; px <= x1 + 1e-9; px += Step)
        {
            for (double py = y0; py <= y1 + 1e-9; py += Step)
            {
                if (!HavenInterior.InTheGarden(berth, px, py)
                    || SurfaceCollision.Blocked(px, py, DeckPlan.AvatarRadius, walls))
                {
                    continue;
                }

                standable++;
                if (!got.Contains(((int)Math.Round(px / Step), (int)Math.Round(py / Step))))
                {
                    missed++;
                }
            }
        }

        return (standable, missed);
    }

    // ── THE GLASS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE GLASS IS THE GALLERY'S GLASS.</b> The room's west wall and its north wall — found on the deck by
    /// where they are — are drawn as windows (<c>IsWindow</c>, the pen's one way of saying glass) and still
    /// collide: a point on the far side of the west glass is not reachable from inside.
    ///
    /// <para><b>Proven RED</b> by laying the west wall as stone (<c>IsWindow: false</c>).</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void TheGlassIsGlassAndStillAWall(string berth)
    {
        DeckPlan deck = Deck(berth);
        (double x0, double y0, _, double y1) = Box(berth);

        DeckPlan.Wall west = Assert.Single(deck.Walls,
            w => Math.Abs(w.X1 - x0) < 1e-3 && Math.Abs(w.X2 - x0) < 1e-3);
        Assert.True(west.IsWindow, $"{berth}: the garden's west wall is not glass.");
        Assert.Equal(y1 - y0, Math.Abs(west.Y2 - west.Y1), 3);

        Assert.Contains(deck.Walls, w => w.IsWindow && Math.Abs(w.Y1 - y1) < 1e-3 && Math.Abs(w.Y2 - y1) < 1e-3
                                          && Math.Min(w.X1, w.X2) <= x0 + 1e-3);

        Assert.True(SurfaceCollision.Blocked(x0, (y0 + y1) / 2, DeckPlan.AvatarRadius, deck.CollisionField),
            $"{berth}: the west glass does not collide.");
    }

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE BENCH BY THE GLASS IS THE PARK BENCH'S SEAT.</b> [E] at it — through the deck's own dispatch,
    /// the <c>HiveBench</c> arm — opens a sitting that is a BENCH, whose scene is the park bench's two moves and
    /// no third (SIT A WHILE / Stand up, the park's own labels and ids), whose setting is the room's own plate,
    /// that snaps the captain ON the end he walked up to (#820), and that stands him back up on the room's side
    /// of the plank, on ground he can stand on. SIT A WHILE answers with the park's own silence.
    ///
    /// <para><b>Proven RED</b> by dropping the berth fall-through from <c>Seating.TryTakeBench</c>: the press
    /// answers nothing and the captain stays on his feet.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void TheBenchIsTheParkBenchsSeatWithItsTwoMovesAndItsSnap(string berth)
    {
        Pages.Map map = TheRideDownIsAWayBackTests.Ashore($"garden-bench-{berth}", berth);
        ParkBenches.Bench bench = HavenInterior.TheGardenBenchAt(berth)!.Value;

        // Walk up to the LEFT end's side, where a body can stand, and press.
        (double endX, double endY) = bench.End(ParkBenches.TakenEnd);
        DeckReachability.Point walkUp = HavenInterior.TheGardenBenchStepOff(endX);
        Set(map, "_avatarX", walkUp.X);
        Set(map, "_avatarY", walkUp.Y);
        Invoke(map, "InteractAtConsole");

        object seat = Invoke(map, "get_SeatedTable") ?? throw new Xunit.Sdk.XunitException(
            $"{berth}: [E] at the garden's bench did not sit the captain down.");
        Assert.True((bool)Get(seat, "Bench")!);
        Assert.False((bool)Get(seat, "SharedSeat")!, "nobody is ever on the garden's far end.");
        var scene = (Encounter.Scene)Get(seat, "Scene")!;
        Assert.Equal([SittingAlone.Wait, SittingAlone.Stand], scene.Moves.Select(m => m.Id));
        Assert.Equal([ParkBenches.WaitLabel, ParkBenches.StandLabel], scene.Moves.Select(m => m.Label));
        Assert.Equal(HavenGarden.Plate, scene.Setting);
        Assert.Equal(ParkBenches.OwnBenchPlate, (string)Get(seat, "Plate")!);
        Assert.StartsWith($"garden:{berth}:", (string)Get(seat, "Key")!, StringComparison.Ordinal);

        // #820 · ON the end he walked up to.
        Assert.Equal(endX, (double)Read(map, "_avatarX")!, 6);
        Assert.Equal(endY, (double)Read(map, "_avatarY")!, 6);

        // SIT A WHILE: the park's own silence, on the panel.
        object seating = Read(map, "_seating")!;
        seating.GetType().GetMethod("TableMove", Hidden)!.Invoke(seating, [SittingAlone.Wait]);
        string said = (string)Get(Invoke(map, "get_SeatedTable")!, "Outcome")!;
        Assert.Contains(ParkBenches.NobodyCameLines, l => said.StartsWith(l, StringComparison.Ordinal));

        // Stand up: off the plank, on the room's side, standing clear of the furniture.
        Assert.True((bool)Invoke(map, "StandUpBeforeWalking")!);
        Assert.Null(Invoke(map, "get_SeatedTable"));
        double sx = (double)Read(map, "_avatarX")!, sy = (double)Read(map, "_avatarY")!;
        Assert.True(sy < bench.Y, $"{berth}: stood up on the glass side of the plank.");
        Assert.False(SurfaceCollision.Blocked(sx, sy, DeckPlan.AvatarRadius,
            ((DeckPlan)Read(map, "_deckPlan")!).CollisionField), $"{berth}: stood up inside the furniture.");
        Assert.True(HavenInterior.InTheGarden(berth, sx, sy));
    }

    /// <summary>
    /// <b>NOTHING ANNOUNCES THE BENCH, AND THE BEDS ARE NOT CONSOLES.</b> Kosh: in the garden there is exactly
    /// one console, the bench's, wearing the park bench's own plate — no plate for the bench beside it, no
    /// press on any bed, nothing to take.
    ///
    /// <para><b>Proven RED</b> by hanging a ViewObject on the first bed.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void TheBenchIsTheOnlyPressInTheGarden(string berth)
    {
        DeckPlan deck = Deck(berth);
        DeckPlan.ConsoleSpot only = Assert.Single(deck.Consoles, c => HavenInterior.InTheGarden(berth, c.X, c.Y));
        Assert.Equal(DeckPlan.ConsoleKind.HiveBench, only.Kind);
        Assert.Equal(ParkBenches.FreeBenchPlate, only.Label);
        Assert.DoesNotContain(deck.RoomLabels, l => l.Text == UndergroundComplex.ParkBenchPlate);
    }

    // ── THE ONE LINE ─────────────────────────────────────────────────────────────────────────────────────

    private static string? InTheSlot(Pages.Map map) => ((PulseSlot)Read(map, "_pulse")!).Message;

    private static void StandInTheGarden(Pages.Map map, string berth)
    {
        ParkBenches.Bench bench = HavenInterior.TheGardenBenchAt(berth)!.Value;
        DeckReachability.Point spot = HavenInterior.TheGardenBenchStepOff(bench.X);
        Set(map, "_avatarX", spot.X);
        Set(map, "_avatarY", spot.Y);
    }

    private static void StandOnTheConcourse(Pages.Map map, string berth)
    {
        DeckReachability.Point outside = HavenInterior.TheGardenThresholdAt(berth)!.Value;
        Set(map, "_avatarX", outside.X);
        Set(map, "_avatarY", outside.Y);
    }

    private static void Frames(Pages.Map map, int n, bool freeTheSlot)
    {
        for (int frame = 0; frame < n; frame++)
        {
            if (freeTheSlot)
            {
                Set(map, "_pulse", PulseSlot.Empty);
            }

            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
        }
    }

    /// <summary>
    /// <b>THE FIRST TIME IN, TOLD ONCE AT EACH HAVEN — ON A FREE SLOT, AND FILED NOWHERE.</b> Fable canon,
    /// verbatim: <i>"Warm, wet, and quiet. Somebody comes here on purpose."</i>
    ///
    /// <list type="number">
    /// <item>On the concourse outside its door, frame after frame with the slot free: never.</item>
    /// <item>In, with a line still in the slot: it waits.</item>
    /// <item>The slot comes free: it is told, and the book has no entry for it.</item>
    /// <item>Out and in again, slot free every frame: never a second time at this haven.</item>
    /// </list>
    ///
    /// <para><b>Proven RED</b> by removing the <c>TellTheGardenOnce</c> call from <c>AdvanceBarWalkers</c> (never
    /// in the slot); by dropping its <c>_pulse.Message is not null</c> clause (wrote over a line being read);
    /// by dropping its <c>_toldOnceAtStation</c> clause (told again on the second visit).</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Gardens))]
    public void TheFirstVisitIsToldOnceOnAFreeSlotAndFiledNowhere(string berth)
    {
        Pages.Map map = TheRideDownIsAWayBackTests.Ashore($"garden-line-{berth}", berth);

        // 1 · outside, nothing.
        StandOnTheConcourse(map, berth);
        for (int frame = 0; frame < 30; frame++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
            Assert.NotEqual(HavenGarden.FirstVisitLine, InTheSlot(map));
        }

        // 2 · in, with somebody else's line still being read: it waits its turn.
        StandInTheGarden(map, berth);
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "ShowPulseMessage", "a line still being read", PulseRank.Status);
        Frames(map, 5, freeTheSlot: false);
        Assert.Equal("a line still being read", InTheSlot(map));

        // 3 · the slot comes free: told, and filed nowhere.
        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1, freeTheSlot: false);
        Assert.Equal(HavenGarden.FirstVisitLine, InTheSlot(map));
        Assert.DoesNotContain(
            (IEnumerable<FieldNote>)Read(map, "_fieldNotes")!,
            n => n.Text.Contains(HavenGarden.FirstVisitLine, StringComparison.Ordinal));

        // 4 · out, and in again: never a second time at this haven.
        StandOnTheConcourse(map, berth);
        Frames(map, 10, freeTheSlot: true);
        StandInTheGarden(map, berth);
        for (int frame = 0; frame < 30; frame++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);
            Assert.NotEqual(HavenGarden.FirstVisitLine, InTheSlot(map));
        }
    }

    /// <summary>
    /// <b>ONCE AT EACH HAVEN, NOT ONCE IN THE GAME — AND NOT SPENT BY THE RIDE DOWN.</b> The same page is told
    /// the line at one station, casts off, docks at another and walks in: told again there. And the first ride
    /// down, which shares the memory, is still owed at a station whose garden line has been told.
    ///
    /// <para><b>Proven RED</b> by keying the garden's memory on the berth alone (the first-ride key's shape):
    /// the ride down at the first station is never told.</para>
    /// </summary>
    [Fact]
    public void TheLineIsOwedAtEveryHavenAndTheRideDownIsStillOwedItsOwn()
    {
        string[] two = [.. HavenInterior.InteriorBodyIds.Where(HavenInterior.HasGarden).Take(2)];
        Assert.Equal(2, two.Length);

        Pages.Map map = TheRideDownIsAWayBackTests.Ashore("garden-line-twice", two[0]);
        StandInTheGarden(map, two[0]);
        Frames(map, 1, freeTheSlot: true);
        Assert.Equal(HavenGarden.FirstVisitLine, InTheSlot(map));

        // The ride down still owes its own line here.
        Assert.True((bool)Invoke(map, "RideTheHavenLiftTo", HavenLevels.ServiceLevel, 0)!);
        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1, freeTheSlot: false);
        Assert.Equal(HavenLevels.FirstRideLine, InTheSlot(map));
        Assert.True((bool)Invoke(map, "RideTheHavenLiftTo", HavenLevels.Concourse, 0)!);

        Invoke(map, "PullAvatarAboard");
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody next = sky.Bodies.First(b => b.Id == two[1]);
        Invoke(map, "ClampOntoHaven", next, sky.Position(two[1], (double)Read(map, "SimTime")!), null);
        Assert.Equal(two[1], (string?)Read(map, "_dockedHavenId"));
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!);
        StandInTheGarden(map, two[1]);
        Set(map, "_pulse", PulseSlot.Empty);
        Frames(map, 1, freeTheSlot: false);
        Assert.Equal(HavenGarden.FirstVisitLine, InTheSlot(map));
    }

    // ── THE BOARD AND THE DOOR A TESTER USES ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE MENU LINE IS ON THE BOARD'S CARD.</b> The board block of the bar's card draws Core's line for its
    /// counter (<see cref="HavenGarden.MenuLineAt"/>, which the Core suite holds to every board) — read off the
    /// card's own markup, inside the board and after the special.
    ///
    /// <para><b>Proven RED</b> by deleting the line's block from <c>BarMenuCard.razor</c>.</para>
    /// </summary>
    [Fact]
    public void TheMenuLineIsChalkedOnTheBoardsCard()
    {
        string card = File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map",
            "BarMenuCard.razor"));
        int board = card.IndexOf("<div class=\"bar-board\">", StringComparison.Ordinal);
        int special = card.IndexOf("@boardLine", board, StringComparison.Ordinal);
        int line = card.IndexOf("HavenGarden.MenuLineAt(keep.BodyId)", special, StringComparison.Ordinal);
        int foot = card.IndexOf("<div class=\"deck-offer-actions\">", board, StringComparison.Ordinal);
        Assert.True(board > 0 && special > board && line > special && line < foot,
            "the garden's menu line is not drawn on the board, under the special.");
    }

    /// <summary>
    /// <b>A TESTER STANDS AT THE GARDEN'S DOOR IN ONE URL.</b> Two havens at least carry a dev start
    /// (<c>?dock=&lt;haven&gt;&amp;ashore=1&amp;garden=1</c>); the boot's own parse reads it into that berth, ashore,
    /// with the garden asked for; and the stand puts the captain one pace outside THE door, on the concourse,
    /// on ground he can stand on — the step in is his.
    ///
    /// <para><b>Proven RED</b> by leaving the rows out of <c>DevStarts.All</c> (fewer than two), and by the
    /// parse not setting <c>GardenCheat</c>.</para>
    /// </summary>
    [Fact]
    public void AGardenIsOneDevStartAway()
    {
        string[] rows =
        [
            .. DevStarts.All.Select(e => e.Url)
                .Where(u => u.Contains("&garden=1", StringComparison.Ordinal)),
        ];
        Assert.True(rows.Length >= 2, $"only {rows.Length} dev start(s) stand at a garden's door.");

        foreach (string url in rows)
        {
            Pages.Map map = Boot($"garden-dev-{rows.ToList().IndexOf(url)}");
            object q = Invoke(map, "ReadEveryQueryKey", new Uri("https://localhost" + url))!;
            Invoke(map, "DefaultABerthForTheCheatsThatNeedOne", q);
            Assert.True((bool)q.GetType().GetField("GardenCheat", Hidden)!.GetValue(q)!);
            Assert.True((bool)q.GetType().GetField("AshoreCheat", Hidden)!.GetValue(q)!);
            string berth = (string)q.GetType().GetField("DockCheat", Hidden)!.GetValue(q)!;
            Assert.True(HavenInterior.HasGarden(berth), $"{url} docks at {berth}, which has no garden.");

            Pages.Map ashore = TheRideDownIsAWayBackTests.Ashore($"garden-door-{berth}", berth);
            Invoke(ashore, "StandAtTheGardensDoorIfAsked");
            double x = (double)Read(ashore, "_avatarX")!, y = (double)Read(ashore, "_avatarY")!;
            DeckReachability.Point door = HavenInterior.TheGardenDoorsAt(berth)[0];
            Assert.False(HavenInterior.InTheGarden(berth, x, y), $"{berth}: the dev start stands him inside.");
            Assert.True(Math.Sqrt(Math.Pow(x - door.X, 2) + Math.Pow(y - door.Y, 2)) < DeckPlan.InteractRadius,
                $"{berth}: the dev start does not stand him at the door.");
            Assert.False(SurfaceCollision.Blocked(x, y, DeckPlan.AvatarRadius,
                ((DeckPlan)Read(ashore, "_deckPlan")!).CollisionField));
        }
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
        {
            at = at.Parent;
        }

        return at?.FullName ?? throw new InvalidOperationException("no repo root above the test binaries");
    }
}
