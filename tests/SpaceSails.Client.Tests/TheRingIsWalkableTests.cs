using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #813 · A CORRIDOR ON EVERY SIDE — the Manhattan ruling's third law, walked on the deck the boots
/// actually collide with.
///
/// <para>Owner, 2026-08-09: <i>"The central park needs to be in the center of all the other rooms… not on
/// the side"</i> and, deciding every number in the carve, <i>"make sure the park prime real estate is not
/// wasted and not unused, not on any side. It is the best real estate."</i> Core's own guards prove the
/// shape of that — the ring is complete, no du of the park's perimeter is rock, every room's door is on a
/// street. This file proves the part a shape cannot: that a BODY gets to every one of those rooms, from the
/// car, <b>without walking through another one of them</b>.</para>
///
/// <para>That last clause is the whole guard, and it is the difference between a block and a warren. A
/// floor where you reach the potting shed by crossing the cold room would satisfy every reachability audit
/// in this folder — every room is reached, every door is a door — and would be precisely the building the
/// ruling forbids. #600's lesson said the same thing about a lift: the A* audit proved you could REACH the
/// lift and never that it was a way HOME. "Reachable" is not the claim. "Reachable off a street" is.</para>
///
/// <para><b>How it is proved, in three moves that only mean anything together.</b> One deck per floor, one
/// flood per deck — the harness <see cref="TheParkIsWalkableTests"/> uses, for the reason its own summary
/// gives: a hundred and forty rooms times twelve sites is a lot of separate A* searches over a field this
/// size, and <see cref="DeckReachability.Reachable"/> is the same lattice and the same standability
/// predicate as the walk.</para>
/// <list type="number">
/// <item><b>The honest floor.</b> Every ring room's centre is standable and on the flood from the car. If
/// it were not, everything below would be a statement about a room nobody can enter at all.</item>
/// <item><b>The floor with EVERY ring door poured shut</b> — every street door, every gravel door, and the
/// hall's own openings with them. The streets, the spine, the back street and the park are all still there;
/// the rooms are boxes. On that floor every room's own DOORSTEP — the spot a pace outside its door, on the
/// street side — must still be on the flood. That is "you can get to every door without entering a room",
/// which is the ruling, stated in the one unit a guard can measure.</item>
/// <item><b>And on that same sealed floor, no room's CENTRE may be on the flood.</b> Without this the
/// second move would pass on a building with no walls at all: it is the plug proving it is a plug.</item>
/// </list>
///
/// <para>Move (2) plus a door that is genuinely a way through — which
/// <see cref="TheCirculationIsWalkableTests"/> owns, per doorway, inside a box that leaves no way round —
/// is the sentence "car → street → this room's own door → this room", with no other room on the route.</para>
///
/// <para><b>Proven RED, twice, and deliberately at two different depths</b> — both by breaking Core's
/// <c>RingBox</c>, running this file, and pasting what actually came out.</para>
///
/// <para><b>(A) The door gap taken out of the block's two ENDS.</b> The west and east rooms keep their
/// walls, their glass and their published <c>Door</c>; only the gap in the street face goes, so the plan
/// still draws a violet leaf over poured concrete. That is the drawn-versus-simulated bug in its purest
/// form, and move (1) catches it:</para>
/// <code>
/// 48 ring room(s) are not reached off a street:
///   luna B1: ring room 11 (West) — REGISTERED OFFICE · GARDEN ASPECT — cannot be reached from the car at all.
///   luna B1: ring room 12 (West) — NEGOTIATION ROOM · BOOK AT THE COUNTER — cannot be reached from the car at all.
///   luna B1: ring room 13 (East) — SIGNATORY SUITE · TWO KEYS — cannot be reached from the car at all.
///   luna B1: ring room 14 (East) — SENIOR ROTA · GREEN SIDE — cannot be reached from the car at all.
///   phobos B1: ring room 11 (West) — RECEPTION · APPOINTMENTS HELD — cannot be reached from the car at all.
///   …
/// </code>
///
/// <para><b>(B) The end rooms' door moved off the street and into the PARTY WALL</b> — the same street
/// poured shut, and the doorway cut in the wall the room shares with its neighbour instead. That is a
/// building where you reach an office by walking through an office, which is the ruling's own third law
/// broken and the ONE failure the ordinary reachability audits in this folder cannot see. Move (2) names
/// it in the words it was written in:</para>
/// <code>
/// 72 ring room(s) are not reached off a street:
///   luna B1: ring room 11 (West) — REGISTERED OFFICE · GARDEN ASPECT — cannot be reached from the car at all.
///   luna B1: ring room 12 (West) — NEGOTIATION ROOM · BOOK AT THE COUNTER — cannot be reached from the car at all.
///   luna B1: ring room 13 (East) — SIGNATORY SUITE · TWO KEYS — cannot be reached from the car at all.
///   luna B1: ring room 14 (East) — SENIOR ROTA · GREEN SIDE — cannot be reached from the car at all.
///   luna B1: ring room 11 (West) — the doorstep at (-116.5,-255.7) is not reached from the car with the
///   ring's doors shut. The only way to that room is through another one.
///   luna B1: ring room 13 (East) — the doorstep at (106.5,-255.7) is not reached from the car with the
///   ring's doors shut. The only way to that room is through another one.
///   phobos B1: ring room 11 (West) — RECEPTION · APPOINTMENTS HELD — cannot be reached from the car at all.
///   …
/// </code>
///
/// <para>Both breaks were reverted byte-for-byte; Core is untouched by this file.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRingIsWalkableTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static (DeckReachability.Point Spawn, (double, double, double, double) Bounds) Walk()
    {
        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        return (new DeckReachability.Point(sx, sy),
            (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY));
    }

    /// <summary>
    /// Everywhere the captain can get to on this deck, as a lookup — ONE flood per deck rather than one A*
    /// per sample. Lifted whole from <see cref="TheParkIsWalkableTests"/> rather than reimplemented: two
    /// floods with two tolerances would be two different questions wearing one name.
    /// </summary>
    private sealed class Flood
    {
        private const double Step = DeckReachability.DefaultStep;
        private readonly Dictionary<(int, int), List<DeckReachability.Point>> _cells = [];

        public int Count { get; }

        public Flood(DeckPlan deck, DeckReachability.Point from, (double, double, double, double) bounds)
        {
            var reached = DeckReachability.Reachable(from, deck.CollisionField, DeckPlan.AvatarRadius, bounds);
            Count = reached.Count;
            foreach (DeckReachability.Point p in reached)
            {
                (int, int) key = ((int)Math.Floor(p.X / Step), (int)Math.Floor(p.Y / Step));
                if (!_cells.TryGetValue(key, out var bucket))
                {
                    _cells[key] = bucket = [];
                }
                bucket.Add(p);
            }
        }

        /// <summary>Is this spot on the flood? True when a walked lattice point lands within one grid step
        /// of it — the same tolerance <c>CanReach</c> itself reaches a goal with.</summary>
        public bool Holds(double x, double y)
        {
            int cx = (int)Math.Floor(x / Step), cy = (int)Math.Floor(y / Step);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!_cells.TryGetValue((cx + dx, cy + dy), out var bucket))
                    {
                        continue;
                    }
                    foreach (DeckReachability.Point p in bucket)
                    {
                        if (Math.Abs(p.X - x) <= Step && Math.Abs(p.Y - y) <= Step)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park,
        UndergroundComplex.FloorPlan Floor)> EveryBlock()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Park is { } park && park.Frontage.Count > 0)
                {
                    yield return (body, level, park, floor);
                }
            }
        }
    }

    /// <summary>A pace outside a room's door, on the STREET side of it — which way that is comes from the
    /// room and never from the side: the near band's doors face up at the spine and the far band's face
    /// down at the back street, and asking the room is what makes one function do all four.</summary>
    private static (double X, double Y) Doorstep(in UndergroundComplex.RingRoom room, double pace)
    {
        SurfaceLayout.Doorway d = room.Door;
        double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
        bool horizontal = Math.Abs(d.Y1 - d.Y2) < 0.001;
        return horizontal
            ? (mx, my + (Math.Sign(my - room.Y) * pace))
            : (mx + (Math.Sign(mx - room.X) * pace), my);
    }

    /// <summary>Grow a poured plug ACROSS an opening, whichever way the opening runs. A pad on a fixed axis
    /// would lay a zero-width smear along the block's vertical doors and seal nothing at all while
    /// reporting that it had — #775's own lesson, restated about a wall with four orientations.</summary>
    private static DeckPlan.Wall Plug(SurfaceLayout.Doorway d)
    {
        bool horizontal = Math.Abs(d.Y1 - d.Y2) < 0.001;
        float px = horizontal ? 0f : 0.5f, py = horizontal ? 0.5f : 0f;
        return new DeckPlan.Wall(
            (float)d.X1 - px, (float)d.Y1 - py, (float)d.X2 + px, (float)d.Y2 + py, false, true);
    }

    // ── EVERY ROOM ON THE RING IS REACHED OFF A STREET ────────────────────────────────────────────────

    /// <summary>
    /// FROM THE CAR TO EVERY ROOM THAT FACES THE PARK, WITHOUT SETTING FOOT IN ANOTHER ONE. See the class
    /// summary for the three moves and for the captured red.
    /// </summary>
    [Fact]
    public void EveryRingRoomIsReachedFromTheCarWithoutCrossingAnother()
    {
        var bad = new List<string>();
        int floors = 0, walked = 0, sealedRooms = 0;

        (DeckReachability.Point spawn, var bounds) = Walk();

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryBlock())
        {
            DeckPlan honest = DeckFor(body, level);
            floors++;

            Assert.True(
                DeckReachability.Standable(spawn.X, spawn.Y, DeckPlan.AvatarRadius, honest.CollisionField),
                $"{body} B{-level}: the walk starts inside a wall — every verdict below would mean nothing.");

            var open = new Flood(honest, spawn, bounds);
            Assert.True(open.Count > 2000,
                $"{body} B{-level}: the flood only found {open.Count} standable squares on a whole floor — "
                + "it never left the lift, and every verdict below would be vacuous.");

            // ── (1) THE HONEST FLOOR. Every room on the ring is a room you can get into at all.
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                walked++;
                if (!DeckReachability.Standable(
                        room.X, room.Y, DeckPlan.AvatarRadius, honest.CollisionField))
                {
                    bad.Add($"  {body} B{-level}: ring room {room.Number} ({room.Side}) has no floor at its "
                        + $"own centre ({room.X:F0},{room.Y:F0}) — something was built through it.");
                }
                else if (!open.Holds(room.X, room.Y))
                {
                    bad.Add($"  {body} B{-level}: ring room {room.Number} ({room.Side}) — {room.Plate} — "
                        + "cannot be reached from the car at all.");
                }
            }

            // ── (2)/(3) THE FLOOR WITH THE WHOLE RING SHUT. Every ring door, every gravel door, and the
            //    hall's own openings — because the hall is one of the rooms round this park too, and a
            //    route that cut through the bar to reach a suite would be exactly the thing under test.
            //    Each opening is checked against the plan's own drawn list before it is plugged, so this
            //    cannot brick up a hole nobody built.
            var plugged = new List<DeckPlan.Wall>(honest.Walls);
            var shutDoors = new List<SurfaceLayout.Doorway>();
            //
            //    #817 · EVERY ONE OF A ROOM'S STREET DOORS, not just the first. The owner overrode the
            //    ring's "exactly one door" from inside a landscape office ("bigger spaces must have much
            //    more doors"), and a sealing experiment that plugged room.Door alone would have left two of
            //    the three leaves on a 48 du suite wide open — and then reported, in confident detail, that
            //    the ring is reached off its streets. The room's own published list, so a room that grows a
            //    door grows this plug with it.
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                foreach (SurfaceLayout.Doorway leaf in room.Doors)
                {
                    shutDoors.Add(leaf);
                }
                if (room.Gate is { } gravel)
                {
                    shutDoors.Add(gravel);
                }
            }
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Hall is { } hall)
                {
                    shutDoors.AddRange(hall.Openings);
                }
            }
            foreach (SurfaceLayout.Doorway d in shutDoors)
            {
                Assert.Contains(d, floor.Doorways);
                plugged.Add(Plug(d));
            }

            var sealedDeck = new DeckPlan(
                [.. plugged], honest.Consoles, honest.RoomLabels, honest.Backdrops,
                honest.SpawnX, honest.SpawnY, 0, (_, _) => { }, (_, _) => "sealed");
            var streets = new Flood(sealedDeck, spawn, bounds);

            // The street network survived the plug. If it had not, every doorstep below would be
            // unreachable for a reason that has nothing to do with the ring.
            Assert.True(streets.Holds(park.X, park.Y),
                $"{body} B{-level}: shutting the ring's doors also took the park — the plug is in the wrong "
                + "place and this experiment measures nothing.");

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                sealedRooms++;
                (double sx, double sy) = Doorstep(in room, DeckPlan.AvatarRadius + 1.5);

                if (!streets.Holds(sx, sy))
                {
                    bad.Add($"  {body} B{-level}: ring room {room.Number} ({room.Side}) — the doorstep at "
                        + $"({sx:F1},{sy:F1}) is not reached from the car with the ring's doors shut. The "
                        + "only way to that room is through another one.");
                }

                if (streets.Holds(room.X, room.Y))
                {
                    bad.Add($"  {body} B{-level}: ring room {room.Number} ({room.Side}) — poured shut, the "
                        + "room is STILL reachable; the plug proves nothing.");
                }
            }
        }

        // ANTI-VACUITY, and it is stated in three numbers because all three have gone quietly to zero in
        // this house before: floors that HAVE a block, rooms walked on the honest floor, and rooms whose
        // doorstep was checked against a sealed one. A guard that flooded no blocks would report success.
        Assert.True(floors >= 10, $"only {floors} floors carry a block — this proved little.");
        Assert.True(walked >= 100, $"only {walked} ring rooms were walked — this proved little.");
        Assert.True(sealedRooms >= 100,
            $"only {sealedRooms} ring rooms had their doorstep checked against a sealed ring — the half of "
            + "this guard that carries the ruling never ran.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} ring room(s) are not reached off a street:\n" + string.Join("\n", bad.Take(20)));
    }

    /// <summary>
    /// …AND EVERY ONE OF THEM IS PLATED WHERE ITS DOOR IS. The rooms are Core's, their stencils are Core's,
    /// and the deck's only job is to carry them — so this is the ONE-SOURCE half: every ring room's plate is
    /// on the plan, and no room on the ring is left anonymous.
    ///
    /// <para>It matters more here than it looks. A ring room's door is on a street and its glass is on the
    /// park, so the only thing that tells a captain in a corridor which of fourteen identical poured boxes
    /// they are standing outside is the stencil — and the amenity gradient (#775) is drawn in exactly that
    /// vocabulary: the rooms with the view are named for the view.</para>
    /// </summary>
    [Fact]
    public void EveryRingRoomIsPlatedOnTheDeck()
    {
        var bad = new List<string>();
        int plates = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryBlock())
        {
            DeckPlan deck = DeckFor(body, level);

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (string.IsNullOrEmpty(room.Plate))
                {
                    continue;   // past the seam nothing is signed, and that absence is #592's whole tell
                }
                plates++;
                if (!deck.RoomLabels.Any(l => l.Text == room.Plate)
                    && !deck.Consoles.Any(c => c.Label.Contains(room.Plate, StringComparison.Ordinal)))
                {
                    bad.Add($"  {body} B{-level}: ring room {room.Number} ({room.Side}) is stencilled "
                        + $"\"{room.Plate}\" and the deck says nothing at all — one of fourteen identical "
                        + "poured boxes with no way to tell it from the others.");
                }
            }
        }

        Assert.True(plates >= 100, $"only {plates} ring plates were read — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} ring room(s) are drawn without their stencil:\n" + string.Join("\n", bad.Take(20)));
    }
}
