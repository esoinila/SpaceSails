using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #751 · CAN YOU ACTUALLY WALK THE HALL? — the A* flood, pointed at eighty chairs and three shut rooms.
///
/// <para>#585's law, on new ground: <b>a room that exists on screen and cannot be reached is the same bug
/// class as a beacon over nothing.</b> A hall is thirty times the floor area of the module the audits were
/// written against, and it is the first room in the game with furniture INSIDE it — poured pillars, a bar
/// wall, and three enclosed boxes each with one door. Every one of those is a chance to seal a chair, and
/// none of it is visible to reasoning.</para>
///
/// <para>So this walks the REAL deck the captain's boots collide with, to every top the room offers and into
/// every cabinet — and then, because #600's lesson is that an audit is only worth what it can SEE,
/// it deliberately plugs a doorway and demands the audit go red.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHallIsWalkableTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level, long watch = 0) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], watch);

    private static (DeckReachability.Point Spawn, (double, double, double, double) Bounds) Walk()
    {
        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        return (new DeckReachability.Point(sx, sy),
            (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY));
    }

    /// <summary>Every hall on every site, with the amenity that carries it.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Amenity Room,
        UndergroundComplex.Hall Hall)> EveryHall()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.Amenity a in
                    UndergroundComplex.Build(body, level, Field).Amenities)
                {
                    if (a.Hall is { } hall)
                    {
                        yield return (body, level, a, hall);
                    }
                }
            }
        }
    }

    // ── (b)/(m) THE FLOOD ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY CHAIR IN EVERY HALL CAN BE WALKED TO FROM THE LIFT — the counter, all twenty tops, and the
    /// aisles between them.
    ///
    /// <para>Both ends of every walk are asserted STANDABLE first (#600's lesson: a reachability test is
    /// only as honest as its endpoints). A top buried in the counter would make <c>CanReach</c> return false
    /// and read as "the corridors are broken", which is a different and much less alarming bug.</para>
    /// </summary>
    [Fact]
    public void EverySeatInEveryHallIsReachableFromTheLift()
    {
        var bad = new List<string>();
        int tops = 0, halls = 0;

        (DeckReachability.Point spawn, var bounds) = Walk();

        foreach ((string body, int level, UndergroundComplex.Amenity room, _) in EveryHall())
        {
            DeckPlan deck = DeckFor(body, level);
            halls++;

            if (!DeckReachability.Standable(spawn.X, spawn.Y, DeckPlan.AvatarRadius, deck.CollisionField))
            {
                bad.Add($"  {body} B{-level}: the walk starts inside a wall — the verdict would mean nothing.");
                continue;
            }

            // The fixture console first: it is the one the floor-wide audit already targets, and it is where
            // ?tablescene=1 stands the captain down.
            foreach ((double px, double py, string what) in
                room.Tables.Select(t => (t.X, t.Y, "a top"))
                    .Append((room.X, room.Y, "THE COUNTER")))
            {
                tops++;
                if (!DeckReachability.Standable(px, py, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    bad.Add($"  {body} B{-level}: {what} at ({px:F0}, {py:F0}) is solid wall.");
                }
                else if (!DeckReachability.CanReach(
                    spawn, new DeckReachability.Point(px, py),
                    deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    bad.Add($"  {body} B{-level}: {what} at ({px:F0}, {py:F0}) cannot be reached from the lift.");
                }
            }
        }

        Assert.True(halls > 15, $"only {halls} halls were flooded — this proved little.");
        Assert.True(tops > 300, $"only {tops} places in halls were walked to — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} place(s) in a hall cannot be stood in or reached:\n"
            + string.Join("\n", bad.Take(20)));
    }

    /// <summary>
    /// (h) EVERY CABINET'S DOORWAY IS A DOORWAY. Three enclosed boxes with one gap each, off a wall the
    /// counter has its back to — the exact shape of #585's stranded room, and the reason this is measured
    /// rather than looked at.
    /// </summary>
    [Fact]
    public void EveryCabinetCanBeWalkedIntoFromTheLift()
    {
        var bad = new List<string>();
        int cabinets = 0;

        (DeckReachability.Point spawn, var bounds) = Walk();

        foreach ((string body, int level, _, UndergroundComplex.Hall hall) in EveryHall())
        {
            DeckPlan deck = DeckFor(body, level);

            foreach (UndergroundComplex.Cabinet cabinet in hall.Cabinets)
            {
                cabinets++;
                foreach ((double px, double py, string what) in new[]
                {
                    (cabinet.X, cabinet.Y, "the middle of the cabinet"),
                    (cabinet.Table.X, cabinet.Table.Y, "the cabinet's table"),
                })
                {
                    if (!DeckReachability.Standable(px, py, DeckPlan.AvatarRadius, deck.CollisionField))
                    {
                        bad.Add($"  {body} B{-level}: cabinet {cabinet.Number} — {what} is solid wall.");
                    }
                    else if (!DeckReachability.CanReach(
                        spawn, new DeckReachability.Point(px, py),
                        deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                    {
                        bad.Add(
                            $"  {body} B{-level}: cabinet {cabinet.Number} — {what} cannot be reached; "
                            + "its door is not a door.");
                    }
                }
            }
        }

        Assert.True(cabinets > 30, $"only {cabinets} cabinets were walked into — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} cabinet check(s) failed:\n{string.Join("\n", bad.Take(20))}");
    }

    // ── PROVE THE AUDITOR CAN SEE ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #751/#587 · THE AUDIT GOES RED WHEN A PILLAR IS PUT OVER A DOORWAY.
    ///
    /// <para>The house's fifth bug class is a guard handed a world that cannot tell pass from fail, and the
    /// two floods above are exactly the shape that catches it: they walk a room that has never once been
    /// broken. So this breaks it on purpose — a poured pillar laid across the hall's own doorway, which is
    /// the single most plausible thing to get wrong when a room grows furniture — and demands the same
    /// flood, over the same deck, report the tables it can no longer reach.</para>
    ///
    /// <para>It is the ONE gap the hall and the corridor share (#585), so plugging it is also a statement
    /// about that: if the hall had cut a second door of its own somewhere, this would stay green and the
    /// one-gap law would have been quietly broken.</para>
    /// </summary>
    [Fact]
    public void APillarLaidOverTheHallsDoorwayTakesTheAuditRED()
    {
        (DeckReachability.Point spawn, var bounds) = Walk();
        int proved = 0;

        foreach ((string body, int level, UndergroundComplex.Amenity room, UndergroundComplex.Hall hall)
            in EveryHall())
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            DeckPlan honest = DeckFor(body, level);

            // EVERY WAY INTO THE ROOM, ASKED OF THE ROOM.
            //
            // #775 · This used to pick doorways out of floor.Doorways by their x standing on one of the
            // hall's two vertical edges, which found every door a hall had for exactly as long as every
            // door a hall had was a gap in its rib's face. The front doors cut into the SPINE's face (#775)
            // stand on the box's other pair of edges and were silently missed — and a sealing experiment
            // that misses a door is an experiment that proves nothing while reporting that it proved
            // something, which is this house's fifth bug class wearing a red-proof's clothes.
            //
            // The hall publishes its openings now, and each one is checked against the plan's own drawn
            // list before it is plugged, so this cannot brick up a hole that is not actually a door.
            IReadOnlyList<SurfaceLayout.Doorway> doors = hall.Openings;
            if (doors.Count == 0)
            {
                continue;
            }
            foreach (SurfaceLayout.Doorway d in doors)
            {
                Assert.True(
                    floor.Doorways.Any(p => Math.Abs(p.X1 - d.X1) < 0.001 && Math.Abs(p.Y1 - d.Y1) < 0.001),
                    $"{body} B{-level}: the hall names a door at ({d.X1:F1},{d.Y1:F1}) that the plan does "
                    + "not draw — plugging it would prove nothing about the deck.");
            }

            // THE WORLD IS HONEST FIRST. If the tables were unreachable anyway, the red below would prove
            // nothing at all.
            var reachable = room.Tables
                .Where(t => DeckReachability.CanReach(
                    spawn, new DeckReachability.Point(t.X, t.Y),
                    honest.CollisionField, DeckPlan.AvatarRadius, bounds))
                .ToList();
            Assert.True(reachable.Count == room.Tables.Count,
                $"{body} B{-level}: the hall is already unwalkable — this experiment would prove nothing.");

            // …and now every one of its doors is a poured pillar. The plug is grown ACROSS the opening,
            // whichever way the opening runs — a fixed y-pad would have laid a zero-height smear along a
            // front door in the spine's face and sealed nothing at all.
            var plugged = new List<DeckPlan.Wall>(honest.Walls);
            foreach (SurfaceLayout.Doorway d in doors)
            {
                bool horizontal = Math.Abs(d.Y1 - d.Y2) < 0.001;
                float px = horizontal ? 0.5f : 0f, py = horizontal ? 0f : 0.5f;
                plugged.Add(new DeckPlan.Wall(
                    (float)d.X1 - px, (float)d.Y1 - py, (float)d.X2 + px, (float)d.Y2 + py, false, true));
            }

            var sealedDeck = new DeckPlan(
                [.. plugged], honest.Consoles, honest.RoomLabels, honest.Backdrops,
                honest.SpawnX, honest.SpawnY, 0, (_, _) => { }, (_, _) => "sealed");

            int stillReachable = room.Tables.Count(t => DeckReachability.CanReach(
                spawn, new DeckReachability.Point(t.X, t.Y),
                sealedDeck.CollisionField, DeckPlan.AvatarRadius, bounds));

            Assert.True(stillReachable == 0,
                $"{body} B{-level}: {stillReachable} of {room.Tables.Count} tops are STILL reachable with "
                + "every one of the hall's doorways poured shut. Either the audit cannot see a sealed room, "
                + "or the hall has a second way in that the corridor knows nothing about (#585).");

            proved++;
        }

        Assert.True(proved > 15,
            $"only {proved} halls could be deliberately sealed — the experiment was never run.");
    }

    // ── (c) ONE SOURCE, WITH THE HALL IN IT ───────────────────────────────────────────────────────────

    /// <summary>The drawn room and the pressed room are still one room now that it holds eighty: every top
    /// the deck draws — hall floor and cabinet alike — is a top Core's one call returned, and every patron
    /// dot stands on one of them.</summary>
    [Fact]
    public void ONESOURCE_TheDrawnHallIsCoresHall()
    {
        int tops = 0, people = 0, cabinetTops = 0;

        foreach ((string body, int level, UndergroundComplex.Amenity room, UndergroundComplex.Hall hall)
            in EveryHall())
        {
            for (long watch = 0; watch < 6; watch += 2)
            {
                DeckPlan deck = DeckFor(body, level, watch);

                var expected = CanteenRegulars.Tables(body, level, room, watch);
                var drawn = deck.Tables.Select(t => (t.X, t.Y)).ToList();
                var dots = deck.Consoles
                    .Where(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular)
                    .Select(c => (c.X, c.Y, c.Label))
                    .ToList();

                foreach (CanteenRegulars.TableSeat t in expected)
                {
                    Assert.Contains(((float)t.X, (float)t.Y), drawn);
                    if (t.Cabinet > 0)
                    {
                        cabinetTops++;
                        // A cabinet is empty in v1 — #731's walkers are the ones who will sit in one.
                        Assert.False(t.Taken, $"{body} B{-level}: somebody is in cabinet {t.Cabinet}.");
                    }
                    if (t.Plate is { } plate)
                    {
                        Assert.Contains(((float)t.X, (float)t.Y, plate), dots);
                        people++;
                    }
                }

                // Nobody is drawn anywhere but on a top the room actually has.
                foreach ((float dx, float dy, string _) in dots)
                {
                    Assert.Contains((dx, dy), drawn);
                }

                // …and every cabinet's plate is on the deck, once, where Core put the door.
                foreach (UndergroundComplex.Cabinet cabinet in hall.Cabinets)
                {
                    Assert.Contains(deck.RoomLabels, l =>
                        string.Equals(l.Text, cabinet.Plate, StringComparison.Ordinal));
                }

                tops += expected.Count;
            }
        }

        Assert.True(tops > 1000, $"only {tops} tops swept — this guard is not walking halls.");
        Assert.True(people > 300, $"only {people} people drawn — the hall is not full.");
        Assert.True(cabinetTops > 60, $"only {cabinetTops} cabinet tops drawn — this proved little.");
    }

    /// <summary>The board still hangs where a rota goes: by the door, on the hall's own floor, and clear of
    /// the furniture. Core says where — a renderer choosing a spot on a wall of a room it does not own is
    /// §13.15's own failure, and the #709 offsets were measured against a 15 x 12 room.</summary>
    [Fact]
    public void TheBoardHangsByTheHallsDoorAndNotInsideItsFurniture()
    {
        int boards = 0;

        (DeckReachability.Point spawn, var bounds) = Walk();

        foreach ((string body, int level, UndergroundComplex.Amenity room, UndergroundComplex.Hall hall)
            in EveryHall())
        {
            if (CanteenBoard.At(body, level, room) is not { } board)
            {
                continue;   // the mess has no board, and never had one
            }

            boards++;
            Assert.Equal((hall.BoardX, hall.BoardY), board);
            Assert.True(hall.Contains(board.X, board.Y),
                $"{body} B{-level}: the board hangs outside the hall.");

            DeckPlan deck = DeckFor(body, level);
            Assert.True(
                DeckReachability.Standable(board.X, board.Y, DeckPlan.AvatarRadius, deck.CollisionField)
                && DeckReachability.CanReach(
                    spawn, new DeckReachability.Point(board.X, board.Y),
                    deck.CollisionField, DeckPlan.AvatarRadius, bounds),
                $"{body} B{-level}: nobody can stand at the board to read it.");

            // …and it owns its own patch of floor: two du of clearance from every console on the deck,
            // which is the same law ConsoleCrowdingTests enforces on every ship and haven.
            foreach (DeckPlan.ConsoleSpot c in deck.Consoles)
            {
                double dx = c.X - board.X, dy = c.Y - board.Y;
                double d = Math.Sqrt((dx * dx) + (dy * dy));
                Assert.True(d < 0.01 || d >= 2.0,
                    $"{body} B{-level}: '{c.Label}' is {d:F2} du from the board — two labels, one smear.");
            }
        }

        Assert.True(boards > 8, $"only {boards} boards were checked — this proved little.");
    }

    /// <summary>
    /// #751 · THE CROWD COSTS NOTHING TO KEEP DRAWING — measured, not claimed.
    ///
    /// <para>WASM perf is a law here, and "eighty people in a room" is exactly the sentence that invites a
    /// per-frame cost. The design's answer is that a background patron is DATA: a plate, a bark and a chair,
    /// laid once when the floor's deck is welded. So the claim to check is that a heaving hall's deck is a
    /// FIXED shape — the same console count and the same allocation every time it is built, rather than a
    /// figure that creeps with each rebuild.</para>
    ///
    /// <para>Measured with the thread's own allocation counter over repeated builds rather than asserted in
    /// a comment. A tolerance is allowed because the runtime allocates for its own reasons; what would fail
    /// here is growth, which is the thing that matters.</para>
    /// </summary>
    [Fact]
    public void REBUILDING_AHeavingHallsDeckCostsTheSameEveryTime()
    {
        const string body = "luna";
        int level = UndergroundComplex.TopPressurisedFloor(body)!.Value;

        // Warm the generator's own statics and the JIT, so the first build's one-off cost is not measured.
        for (int i = 0; i < 3; i++)
        {
            _ = DeckFor(body, level, 2);
        }

        int consoles = DeckFor(body, level, 2).Consoles.Length;
        int walls = DeckFor(body, level, 2).Walls.Length;
        Assert.True(consoles > 20,
            $"only {consoles} consoles on a heaving hall's floor — this measured an empty room.");

        long first = Measure();
        long later = Measure();

        Assert.Equal(consoles, DeckFor(body, level, 2).Consoles.Length);
        Assert.Equal(walls, DeckFor(body, level, 2).Walls.Length);

        // Growth is the failure. A hall that leaked a list per rebuild would show up as a rising figure long
        // before a player felt it, which is the whole reason this is a number rather than a paragraph.
        Assert.True(later <= first + (first / 4),
            $"rebuilding the hall's deck cost {first} bytes for ten builds and then {later} — the crowd is "
            + "growing something per build.");

        static long Measure()
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10; i++)
            {
                _ = DeckFor("luna", UndergroundComplex.TopPressurisedFloor("luna")!.Value, 2);
            }
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }

    /// <summary>The hall's tops do not crowd each other into an illegible smear either. Same two du, and it
    /// is measured because the pitch is DERIVED — a hall on tight ground packs closer, and this is where it
    /// would stop being readable.</summary>
    [Fact]
    public void NoTwoPatronsInAHallShareADoorstep()
    {
        var smears = new StringBuilder();

        foreach ((string body, int level, _, _) in EveryHall())
        {
            DeckPlan deck = DeckFor(body, level, 2);   // the heaving watch, which is the worst case
            var dots = deck.Consoles
                .Where(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular)
                .ToList();

            for (int i = 0; i < dots.Count; i++)
            {
                for (int j = i + 1; j < dots.Count; j++)
                {
                    double dx = dots[i].X - dots[j].X, dy = dots[i].Y - dots[j].Y;
                    double d = Math.Sqrt((dx * dx) + (dy * dy));
                    if (d < 2.0)
                    {
                        smears.AppendLine(
                            $"  {body} B{-level}: '{dots[i].Label}' and '{dots[j].Label}' are {d:F2} du apart.");
                    }
                }
            }
        }

        Assert.True(smears.Length == 0, $"patrons drawn on top of each other:\n{smears}");
    }
}
