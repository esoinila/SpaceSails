using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #813 · THE MANHATTAN RULING — the park is the middle of a block, and every side of it is somebody's
/// front wall.
///
/// <para>Owner, 2026-08-09 evening: <i>"The central park needs to be in the center of all the other rooms…
/// not on the side. Think of New York, is the park on one side or is it in the center?"</i> And the clause
/// every guard below is a measurement of: <i>"make sure the park prime real estate is not wasted and not
/// unused, not on any side. It is the best real estate."</i></para>
///
/// <para>#759 shipped the green as a BAND: the hall's glass on one long side and painted rock on the other
/// three. #801 put a row of doors in the far wall, which was the same complaint from inside — <i>"walking
/// through the park is fun, it should not be the edge."</i> This suite is the law that the other three
/// sides can never go back to being a horizon.</para>
///
/// <para>Every guard walks the REAL generator over the REAL field, and every one of them is stated against
/// a PUBLISHED list rather than a coordinate: the ring, the windows, the doorways, the gates. A law about a
/// list nobody keeps is a law nobody can fail, and a law measured off a number typed into the test is the
/// house's fifth bug class.</para>
/// </summary>
public sealed class TheParkIsTheCentreOfTheBlockTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every floor that has a block on it, with the four things a law here needs.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Hall Hall,
        UndergroundComplex.Park Park, UndergroundComplex.FloorPlan Floor)> EveryBlock()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || !UndergroundComplex.HasParkBlock(body, level))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            UndergroundComplex.Hall? hall = null;
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Hall is { } h && a.Use == UndergroundComplex.Comfort.UpperCanteen)
                {
                    hall = h;
                }
            }

            Assert.True(hall is not null, $"{body} B{-level}: the block has no hall on its near band.");
            Assert.True(floor.Park is not null, $"{body} B{-level}: the block has no park in the middle.");
            yield return (body, level, hall!.Value, floor.Park!.Value, floor);
        }
    }

    /// <summary>How much of <paramref name="want"/> is left uncovered by <paramref name="pieces"/>, as the
    /// gaps themselves. Interval arithmetic rather than a sampled sweep, because "not one du of frontage is
    /// wasted" is a claim about a measure and a sampler can only ever say "not at the places I looked".</summary>
    private static List<(double Lo, double Hi)> Gaps(
        (double Lo, double Hi) want, IEnumerable<(double Lo, double Hi)> pieces)
    {
        List<(double Lo, double Hi)> sorted = pieces
            .Where(p => p.Hi > want.Lo + 0.001 && p.Lo < want.Hi - 0.001)
            .OrderBy(p => p.Lo)
            .ToList();

        var gaps = new List<(double Lo, double Hi)>();
        double cursor = want.Lo;
        foreach ((double lo, double hi) in sorted)
        {
            if (lo > cursor + 0.01)
            {
                gaps.Add((cursor, lo));
            }
            cursor = Math.Max(cursor, hi);
        }
        if (cursor < want.Hi - 0.01)
        {
            gaps.Add((cursor, want.Hi));
        }
        return gaps;
    }

    /// <summary>The span a ring room presents to the park, in the axis its side runs along.</summary>
    private static (double Lo, double Hi) Frontage(in UndergroundComplex.RingRoom room) =>
        room.Side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far
            ? (room.X0, room.X1)
            : (room.Y0, room.Y1);

    /// <summary>Which of the park's four walls a gate is cut in, or null where it is somewhere else.</summary>
    private static UndergroundComplex.RingSide? GateSide(
        in UndergroundComplex.Park park, in SurfaceLayout.Doorway gate)
    {
        bool horizontal = Math.Abs(gate.Y1 - gate.Y2) < 0.001;
        if (horizontal && Math.Abs(gate.Y1 - park.Y1) < 0.001)
        {
            return UndergroundComplex.RingSide.Near;
        }
        if (horizontal && Math.Abs(gate.Y1 - park.Y0) < 0.001)
        {
            return UndergroundComplex.RingSide.Far;
        }
        if (!horizontal && Math.Abs(gate.X1 - park.X0) < 0.001)
        {
            return UndergroundComplex.RingSide.West;
        }
        if (!horizontal && Math.Abs(gate.X1 - park.X1) < 0.001)
        {
            return UndergroundComplex.RingSide.East;
        }
        return null;
    }

    /// <summary>The span a gate presents to the park, in the axis its side runs along — the DOORWAY itself,
    /// which is what a body walks through.</summary>
    private static (double Lo, double Hi) GateSpan(in SurfaceLayout.Doorway gate) =>
        Math.Abs(gate.Y1 - gate.Y2) < 0.001
            ? (Math.Min(gate.X1, gate.X2), Math.Max(gate.X1, gate.X2))
            : (Math.Min(gate.Y1, gate.Y2), Math.Max(gate.Y1, gate.Y2));

    /// <summary>The GROUND a gate stands on, which is wider than the gate: a spur is a corridor, and the
    /// two jambs either side of its doorway are the corridor's own walls arriving at the park. Frontage is
    /// measured against this rather than against <see cref="GateSpan"/>, because the rooms either side of a
    /// gate stop at the CORRIDOR and a law written against the doorway would call a doorjamb "wasted
    /// frontage" — which is a law failing on the thickness of a wall.</summary>
    private static (double Lo, double Hi) GateGround(in SurfaceLayout.Doorway gate)
    {
        (double lo, double hi) = GateSpan(gate);
        double mid = (lo + hi) / 2.0;
        return (mid - UndergroundComplex.CorridorHalf, mid + UndergroundComplex.CorridorHalf);
    }

    // ── (a) CENTRALITY ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PARK IS IN THE MIDDLE OF THE FABRIC — carved building on all four sides of it, and no side of it
    /// anywhere near the edge of the world.
    ///
    /// <para><b>Proven RED</b> by carving only the NEAR band — the far band's loop and the two ends' loop
    /// each given an empty list, which is the green as #759 had it: one glazed side and three that face
    /// whatever the field left:</para>
    /// <code>
    /// 156 park(s) are still on the side of the map:
    ///   luna B1: the FAR side of the park has nothing behind it — 203.0 du of frontage and 0 rooms.
    ///   luna B1: the WEST side of the park has nothing behind it — 45.0 du of frontage and 0 rooms.
    ///   luna B1: the EAST side of the park has nothing behind it — 45.0 du of frontage and 0 rooms.
    ///   phobos B1: the FAR side of the park has nothing behind it — 203.0 du of frontage and 0 rooms.
    /// </code>
    /// </summary>
    [Fact]
    public void ThereIsBuildingOnAllFourSidesOfIt()
    {
        var wrong = new List<string>();
        int blocks = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;

            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                int rooms = park.Frontage.Count(r => r.Side == side && r.HasView);
                bool hallHere = side == UndergroundComplex.RingSide.Near;
                if (rooms == 0 && !hallHere)
                {
                    double run = side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far
                        ? park.X1 - park.X0
                        : park.Y1 - park.Y0;
                    wrong.Add($"  {body} B{-level}: the {side.ToString().ToUpperInvariant()} side of the "
                        + $"park has nothing behind it — {run:F1} du of frontage and 0 rooms.");
                }
            }

            // …and the fabric on each side is REAL DEPTH, not a skin. A room a du deep against the edge of
            // the world would satisfy a count and satisfy nothing the owner asked for.
            (string Name, double Off)[] margins =
            [
                ("WEST", park.X0 - (Field.LeftX + SurfaceLayout.EdgeMargin)),
                ("EAST", (Field.RightX - SurfaceLayout.EdgeMargin) - park.X1),
                ("FAR", park.Y0 - (Field.BottomY + SurfaceLayout.EdgeMargin)),
                ("NEAR", (Field.LandingBandY - SurfaceLayout.EdgeMargin) - park.Y1),
            ];
            foreach ((string name, double off) in margins)
            {
                if (off < UndergroundComplex.RingRoomMinDu)
                {
                    wrong.Add($"  {body} B{-level}: the park's {name} wall stands {off:F1} du off the "
                        + "field's own edge.");
                }
            }

            // The hall is one of the ring's rooms and it is on the near band, which is what makes the near
            // side's frontage used even where no office stands.
            if (Math.Abs(hall.Y0 - park.Y1) > 0.001)
            {
                wrong.Add($"  {body} B{-level}: the hall ({hall.Y0:F1}) is not standing on the park's near "
                    + $"wall ({park.Y1:F1}).");
            }
            Assert.NotEmpty(floor.RoomCentres);
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) are still on the side of the map:\n" + string.Join("\n", wrong));
    }

    // ── (b) THE RING IS COMPLETE ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NOT ONE DU OF THE PARK'S PERIMETER IS WASTED. Every side of it, end to end, is either a room's front
    /// wall or a gate — the owner's "not unused, not on any side", in the one unit a guard can measure.
    ///
    /// <para><b>Proven RED</b> by laying the far band's rooms at <see cref="UndergroundComplex.RingRoomTargetDu"/>
    /// each from the start of their segment and letting the remainder be whatever it turned out to be —
    /// which is the tempting version of this carve, and the one that leaves a strip of the best frontage in
    /// the building facing nothing:</para>
    /// <code>
    /// 156 park(s) have frontage facing nothing:
    ///   luna B1: 8.8 du of the FAR wall (-66.5…-57.7) is neither a room nor a gate.
    ///   luna B1: 2.2 du of the FAR wall (-10.7…-8.5) is neither a room nor a gate.
    ///   luna B1: 18.0 du of the FAR wall (78.5…96.5) is neither a room nor a gate.
    ///   phobos B1: 8.8 du of the FAR wall (-66.5…-57.7) is neither a room nor a gate.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryDuOfTheFrontageIsARoomOrAGate()
    {
        var wrong = new List<string>();
        int blocks = 0, du = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;

            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                bool horizontal = side is UndergroundComplex.RingSide.Near
                    or UndergroundComplex.RingSide.Far;
                (double Lo, double Hi) wall = horizontal ? (park.X0, park.X1) : (park.Y0, park.Y1);

                var pieces = new List<(double Lo, double Hi)>();
                foreach (UndergroundComplex.RingRoom r in park.Frontage)
                {
                    if (r.Side == side && r.HasView)
                    {
                        pieces.Add(Frontage(r));
                    }
                }
                if (side == UndergroundComplex.RingSide.Near)
                {
                    pieces.Add((hall.X0, hall.X1));   // the hall's own glass, laid by the hall's carver
                }
                foreach (SurfaceLayout.Doorway g in park.Ways)
                {
                    if (GateSide(park, g) == side)
                    {
                        pieces.Add(GateGround(g));
                    }
                }

                du += (int)(wall.Hi - wall.Lo);
                foreach ((double lo, double hi) in Gaps(wall, pieces))
                {
                    wrong.Add($"  {body} B{-level}: {hi - lo:F1} du of the "
                        + $"{side.ToString().ToUpperInvariant()} wall ({lo:F1}…{hi:F1}) is neither a room "
                        + "nor a gate.");
                }
            }

            Assert.NotEmpty(floor.Windows!);
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(du > 20_000, $"only {du} du of frontage was walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) have frontage facing nothing:\n" + string.Join("\n", wrong));
    }

    // ── (c) THE VIEW IS GLASS, AND THE GLASS IS A WALL ────────────────────────────────────────────────

    /// <summary>
    /// EVERY ROOM WITH A VIEW HAS GLASS IN FRONT OF IT AND NOT A POURED WALL — the whole of what makes it a
    /// room with a view rather than a room with a wall.
    ///
    /// <para>Two halves, and the second is the one that has bitten this project: the glazing is published in
    /// <see cref="UndergroundComplex.FloorPlan.Windows"/> (so the deck draws it as glass and the collision
    /// treats it as wall — one segment, both jobs) AND there is no ordinary wall lying on the same line,
    /// which would be two segments where the eye reads one and a day later they disagree.</para>
    ///
    /// <para><b>Proven RED</b> by pouring the ring's view walls into <c>walls</c> instead of <c>glass</c>
    /// (<c>walls.Add(viewWall.Value)</c> in <c>RingBox</c>):</para>
    /// <code>
    /// 624 park(s) have a view of a wall:
    ///   luna B1: ring room 2 (NEAR) has 48.8 du of frontage and 0.0 du of glass in front of it.
    ///   luna B1: a poured wall (-106.5…-57.7) lies on the NEAR view line.
    ///   luna B1: ring room 3 (NEAR) has 42.2 du of frontage and 0.0 du of glass in front of it.
    ///   luna B1: a poured wall (-50.7…-8.5) lies on the NEAR view line.
    ///   luna B1: ring room 11 (WEST) has 19.0 du of frontage and 0.0 du of glass in front of it.
    ///   luna B1: a poured wall (-253.5…-234.5) lies on the WEST view line.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryViewWallIsGlassAndIsNotAlsoAPouredWall()
    {
        var wrong = new List<string>();
        int blocks = 0, views = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (!room.HasView)
                {
                    continue;
                }
                views++;

                bool horizontal = room.Side is UndergroundComplex.RingSide.Near
                    or UndergroundComplex.RingSide.Far;
                SurfaceLayout.Wall view = room.View!.Value;
                double line = horizontal ? view.Y1 : view.X1;
                (double Lo, double Hi) span = Frontage(room);

                // …the glazing, plus this room's own door in it where it has one (the back of house keeps
                // #801's way in off the gravel), covers the whole of its frontage.
                var pieces = new List<(double Lo, double Hi)>();
                foreach (SurfaceLayout.Wall w in floor.Windows!)
                {
                    bool onLine = horizontal
                        ? Math.Abs(w.Y1 - line) < 0.001 && Math.Abs(w.Y2 - line) < 0.001
                        : Math.Abs(w.X1 - line) < 0.001 && Math.Abs(w.X2 - line) < 0.001;
                    if (onLine)
                    {
                        pieces.Add(horizontal
                            ? (Math.Min(w.X1, w.X2), Math.Max(w.X1, w.X2))
                            : (Math.Min(w.Y1, w.Y2), Math.Max(w.Y1, w.Y2)));
                    }
                }
                if (room.Gate is { } gravel)
                {
                    pieces.Add(GateSpan(gravel));
                }

                double uncovered = Gaps(span, pieces).Sum(g => g.Hi - g.Lo);
                if (uncovered > 0.01)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                        + $"({room.Side.ToString().ToUpperInvariant()}) has {span.Hi - span.Lo:F1} du of "
                        + $"frontage and {(span.Hi - span.Lo) - uncovered:F1} du of glass in front of it.");
                }

                // …and no poured wall lies across it.
                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    bool onLine = horizontal
                        ? Math.Abs(w.Y1 - line) < 0.001 && Math.Abs(w.Y2 - line) < 0.001
                        : Math.Abs(w.X1 - line) < 0.001 && Math.Abs(w.X2 - line) < 0.001;
                    if (!onLine)
                    {
                        continue;
                    }
                    (double lo, double hi) = horizontal
                        ? (Math.Min(w.X1, w.X2), Math.Max(w.X1, w.X2))
                        : (Math.Min(w.Y1, w.Y2), Math.Max(w.Y1, w.Y2));
                    if (lo < span.Hi - 0.01 && hi > span.Lo + 0.01)
                    {
                        wrong.Add($"  {body} B{-level}: a poured wall ({lo:F1}…{hi:F1}) lies on the "
                            + $"{room.Side.ToString().ToUpperInvariant()} view line.");
                    }
                }
            }

            // The hall's own glass is still the hall's own far wall, unchanged since #759.
            Assert.Contains(floor.Windows!, w =>
                Math.Abs(w.Y1 - park.Y1) < 0.001
                && Math.Abs(Math.Min(w.X1, w.X2) - hall.X0) < 0.001
                && Math.Abs(Math.Max(w.X1, w.X2) - hall.X1) < 0.001);
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(views > 400, $"only {views} rooms with a view were checked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) have a view of a wall:\n" + string.Join("\n", wrong));
    }

    // ── (d) A CORRIDOR ON EVERY SIDE ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// NOBODY WALKS THROUGH AN OFFICE TO REACH AN OFFICE. Every ring room's door is on a STREET — the spine
    /// for the near band, the back street for the back of house, the two end streets for the ends — and it
    /// is on the room's own street-side wall, which is the far side of it from the green.
    ///
    /// <para>The A* half of this law (that the door leads to the car without crossing another room) is the
    /// client's, because pathfinding is the client's: <c>TheRingIsWalkableTests</c> owns it.</para>
    ///
    /// <para><b>Proven RED</b> by hanging the west and east bands' doors on the park side instead of the
    /// street side (swapping <c>streetLine</c> and <c>parkLine</c> in <c>RingBox</c>):</para>
    /// <code>
    /// 208 park(s) put a door where the view goes:
    ///   luna B1: ring room 11 (WEST) opens onto the park at -106.5, not onto its street.
    ///   luna B1: ring room 12 (WEST) opens onto the park at -106.5, not onto its street.
    ///   luna B1: ring room 13 (EAST) opens onto the park at 96.5, not onto its street.
    ///   luna B1: ring room 14 (EAST) opens onto the park at 96.5, not onto its street.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryRingRoomOpensOntoAStreetAndNeverOntoTheGreen()
    {
        var wrong = new List<string>();
        int blocks = 0, rooms = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;
            UndergroundComplex.ParkBlock block = UndergroundComplex.BlockOn(Field);

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                rooms++;
                SurfaceLayout.Doorway d = room.Door;
                double dx = (d.X1 + d.X2) / 2.0, dy = (d.Y1 + d.Y2) / 2.0;

                // The door stands on the wall of this room that is FURTHEST from the park.
                double want = room.Side switch
                {
                    UndergroundComplex.RingSide.Near => room.Y1,
                    UndergroundComplex.RingSide.Far => room.Y0,
                    UndergroundComplex.RingSide.West => room.X0,
                    _ => room.X1,
                };
                double got = room.Side is UndergroundComplex.RingSide.Near
                    or UndergroundComplex.RingSide.Far ? dy : dx;
                if (Math.Abs(got - want) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                        + $"({room.Side.ToString().ToUpperInvariant()}) opens onto the park at "
                        + $"{got:F1}, not onto its street.");
                }

                // …and the street it opens onto is one of the four, published as such.
                double streetAt = room.Side switch
                {
                    UndergroundComplex.RingSide.Near => block.SpineFaceY,
                    UndergroundComplex.RingSide.Far => block.BackStreetY1,
                    UndergroundComplex.RingSide.West => block.WestInnerX,
                    _ => block.EastInnerX,
                };
                if (Math.Abs(want - streetAt) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number}'s door at {want:F1} is not on "
                        + $"any of the block's four streets (wanted {streetAt:F1}).");
                }

                // …and the door is a door the floor published, so the deck hangs a leaf on it and the audit
                // can find it without being told anything about rings.
                if (!floor.Doorways.Contains(d))
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number}'s door is not in the floor's "
                        + "own list of doorways.");
                }

                // Exactly one street door. Two would be a room somebody could walk THROUGH.
                Assert.True(room.FloorDu2 > 0, $"{body} B{-level}: ring room {room.Number} has no floor.");
            }

            // The hall's front doors are on the spine, which is the near band's street: #775, unchanged.
            Assert.Contains(hall.Openings, o => Math.Abs(o.Y1 - block.SpineFaceY) < 0.001);
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(rooms > 500, $"only {rooms} ring rooms were checked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) put a door where the view goes:\n" + string.Join("\n", wrong));
    }

    // ── (e) A WAY IN ON EVERY SIDE ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE GATES ARE ONE PER SIDE, WHICH IS WHAT MAKES A CENTRAL PARK A CROSSING. #796 asked for two to
    /// five ways in; a park in the middle of a block gets them for free and gets them everywhere.
    ///
    /// <para><b>Proven RED</b> by dropping the two end gates (returning early from the west/east
    /// <c>RingGate</c> calls):</para>
    /// <code>
    /// 104 park(s) cannot be crossed:
    ///   luna B1: the WEST side of the park has no way in at all.
    ///   luna B1: the EAST side of the park has no way in at all.
    ///   phobos B1: the WEST side of the park has no way in at all.
    ///   phobos B1: the EAST side of the park has no way in at all.
    /// </code>
    /// </summary>
    [Fact]
    public void EverySideOfTheParkHasAWayThrough()
    {
        var wrong = new List<string>();
        int blocks = 0;

        foreach ((string body, int level, UndergroundComplex.Hall _, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;

            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                int ways = park.Ways.Count(g => GateSide(park, g) == side);
                if (ways == 0)
                {
                    wrong.Add($"  {body} B{-level}: the {side.ToString().ToUpperInvariant()} side of the "
                        + "park has no way in at all.");
                }
            }

            if (park.Ways.Count < 4)
            {
                wrong.Add($"  {body} B{-level}: {park.Ways.Count} gate(s) — a park with fewer than one per "
                    + "side is a room you visit rather than a place you cross.");
            }

            foreach (SurfaceLayout.Doorway g in park.Ways)
            {
                Assert.Contains(g, floor.Doorways);
                Assert.NotNull(GateSide(park, g));
            }

            // The one the plate and the dev route are pinned to is still on the hall's own side, and still
            // never cut through the glass: #759's rule, said about a wall with six openings in it.
            Assert.Equal(UndergroundComplex.RingSide.Near, GateSide(park, park.Gate));
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) cannot be crossed:\n" + string.Join("\n", wrong));
    }

    // ── (f) THE CAR GOES WHERE THE BUILDING IS THINNEST ───────────────────────────────────────────────

    /// <summary>
    /// THE RING'S DENSITY DECIDES WHICH END OF THE BLOCK THE GOODS CAR STANDS AT, and not the other way
    /// round — the owner's clause 4, and the one law here that had to be shown CHOOSING rather than merely
    /// agreeing with where the car already was.
    ///
    /// <para>So it is asked twice: once of the shipped ground, and once of a ground whose asymmetry runs the
    /// other way. <see cref="UndergroundComplex.RibColumnsOn"/> drops the rib column nearest the cage, and
    /// that dropped column is what makes one half of the block carry more unbroken frontage than the other.
    /// Move the cage and a DIFFERENT column is dropped — and the car has to move with it. A rule that
    /// answered "west" on both fields would be a rule that was not reading anything.</para>
    /// </summary>
    [Fact]
    public void TheGoodsCarStandsAtTheLessBuiltEndOfTheBlock()
    {
        // ── THE SHIPPED GROUND. The gates fall west of the park's middle, so the west half carries less
        //    unbroken frontage, so the car is at the west end.
        (double west, double east) = UndergroundComplex.RingFrontageOn(Field);
        Assert.True(west < east,
            $"the shipped field is symmetric ({west:F1} vs {east:F1}) — this law cannot be shown choosing.");

        UndergroundComplex.ParkBlock block = UndergroundComplex.BlockOn(Field);
        (double X, double Y)? car = UndergroundComplex.ServiceShaftAt(Field);
        Assert.NotNull(car);
        Assert.True(car!.Value.X < block.WestOuterX,
            $"the car stands at {car.Value.X:F1}, which is not outside the block's west street "
            + $"({block.WestOuterX:F1}) — the thinner end.");

        // …and it is hard against that street rather than adrift in the rock behind it.
        Assert.True(
            block.WestOuterX - car.Value.X
                < UndergroundComplex.ShaftHalf + UndergroundComplex.ShaftClearDu + 1.0,
            $"the car stands {block.WestOuterX - car.Value.X:F1} du off the street it serves.");

        // ── AND THE MIRROR. Move the cage so RibColumnsOn drops a different column: the gates now fall EAST
        //    of the middle, the east half is the thinner one, and the car must cross the building.
        SurfaceLayout.Field flipped = Field with { AnchorX = -94 };
        (double fw, double fe) = UndergroundComplex.RingFrontageOn(flipped);
        Assert.True(fe < fw,
            $"the mirrored ground did not flip the asymmetry ({fw:F1} vs {fe:F1}) — this proved nothing.");

        UndergroundComplex.ParkBlock flippedBlock = UndergroundComplex.BlockOn(flipped);
        (double X, double Y)? flippedCar = UndergroundComplex.ServiceShaftAt(flipped);
        Assert.NotNull(flippedCar);
        Assert.True(flippedCar!.Value.X > flippedBlock.EastOuterX,
            $"the ground's thin end moved and the car did not: it is still at {flippedCar.Value.X:F1}, "
            + $"west of the east street at {flippedBlock.EastOuterX:F1}.");

        // Whatever end it takes, it is still a car a captain cannot see from the other one (#801).
        Assert.True(
            Math.Abs(flippedCar.Value.X - UndergroundComplex.ShaftAt(flipped).X)
                >= UndergroundComplex.MinShaftSeparationOn(flipped));
    }

    // ── (g) THE BLOCK IS ALL OF ITS OWN SIDE ──────────────────────────────────────────────────────────

    /// <summary>
    /// NOTHING ELSE IS BUILT ON THE BLOCK'S SIDE OF THE SPINE. Every cross corridor on the block's floor
    /// either runs away from it into the ordinary grid, or IS one of its gates — because a rib running down
    /// anywhere else would arrive in the middle of somebody's office.
    ///
    /// <para>And the side it takes is the one the cage's alcove does not, every time: the alcove hangs off
    /// the spine's upper face, and the block would otherwise have to be carved around the captain's own way
    /// home.</para>
    /// </summary>
    [Fact]
    public void TheBlockOwnsItsWholeSideOfTheSpineAndNeverTheCagesSide()
    {
        var wrong = new List<string>();
        int blocks = 0;

        foreach ((string body, int level, UndergroundComplex.Hall _, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;
            UndergroundComplex.ParkBlock block = UndergroundComplex.BlockOn(Field);
            (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);

            // The cage's alcove is on the far side of the spine from the green, always.
            if (park.Y1 > shaftY)
            {
                wrong.Add($"  {body} B{-level}: the block is on the cage's own side of the spine.");
            }

            foreach (UndergroundComplex.Rib rib in floor.Ribs)
            {
                if (!rib.Down)
                {
                    continue;
                }
                if (!block.SpurXs.Any(sx => Math.Abs(sx - rib.X) < 0.001))
                {
                    wrong.Add($"  {body} B{-level}: a rib at x={rib.X:F1} runs into the block and is not "
                        + "one of its gates.");
                }
            }

            // …and the gates really are the columns the cross corridors stand on, so a gate and a corridor
            // can never disagree about where the crossings are.
            foreach (double sx in block.SpurXs)
            {
                Assert.Contains(floor.Ribs, r => Math.Abs(r.X - sx) < 0.001 && r.Down);
                Assert.True(Math.Abs(sx - shaftX) > UndergroundComplex.ShaftHalf,
                    $"{body} B{-level}: a gate is cut down the lift's own column.");
            }
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) build on the block's ground:\n" + string.Join("\n", wrong));
    }

    // ── (g1) THE SAME LAWS, ON GROUND THE BLOCK HAS NEVER SEEN ────────────────────────────────────────

    /// <summary>
    /// THE BLOCK IS A FUNCTION OF THE GROUND, SO IT IS ASKED OF MORE THAN ONE PIECE OF GROUND.
    ///
    /// <para>Every other guard in this file sweeps fifty-odd sites — and every one of them lays the SAME
    /// block, because the block's geometry is a pure function of the field and the game ships one field.
    /// That is right for the fiction (one company drawing, poured at every site: the branch offices differ
    /// in what is behind the doors, never in where the doors are) and it is a trap for a test suite. Fifty
    /// floors of identical geometry prove one floor of geometry fifty times.</para>
    ///
    /// <para>So the laws are asked again of grounds the generator has never been handed: narrower, deeper,
    /// shallower, and one with the cage moved far enough to change which rib column
    /// <see cref="UndergroundComplex.RibColumnsOn"/> drops — which moves the gates, which moves the
    /// segments, which moves every room on the ring. If the block only works on 310 × 260, this is where
    /// that is found out.</para>
    ///
    /// <para><b>Proven RED</b> by the same break as
    /// <see cref="EveryDuOfTheFrontageIsARoomOrAGate"/> — the far band laid at a fixed room width with the
    /// remainder left over. On the shipped field that leaves 8.8 du facing nothing; on the narrow field it
    /// leaves a different number, which is the point of asking twice.</para>
    /// </summary>
    [Fact]
    public void TheLawsHoldOnGroundTheGeneratorHasNeverBeenHanded()
    {
        (string Name, SurfaceLayout.Field Field)[] grounds =
        [
            ("the shipped field", Field),
            ("a narrower field", Field with { LeftX = -130, RightX = 120 }),
            ("a much narrower field", Field with { LeftX = -100, RightX = 90 }),
            ("a much wider field", Field with { LeftX = -220, RightX = 210 }),
            ("a deeper field", Field with { BottomY = -320 }),
            ("a shallower field", Field with { BottomY = -258 }),
            ("the cage moved west", Field with { AnchorX = -94 }),
        ];

        var wrong = new List<string>();
        var shapes = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string name, SurfaceLayout.Field ground) in grounds)
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build("luna", -1, ground);
            if (floor.Park is not { } park)
            {
                wrong.Add($"  {name}: no block was carved at all.");
                continue;
            }

            UndergroundComplex.Hall? hall = null;
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Hall is { } h && a.Use == UndergroundComplex.Comfort.UpperCanteen)
                {
                    hall = h;
                }
            }
            if (hall is not { } bar)
            {
                wrong.Add($"  {name}: a block with no hall on its near band.");
                continue;
            }

            // The shape this ground produced, so the guard can say out loud that it met more than one.
            shapes.Add($"{park.X1 - park.X0:F1}x{park.Y1 - park.Y0:F1}/{park.Frontage.Count}");

            // LAW 1 · a room on every side.
            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                if (side != UndergroundComplex.RingSide.Near
                    && !park.Frontage.Any(r => r.Side == side && r.HasView))
                {
                    wrong.Add($"  {name}: the {side.ToString().ToUpperInvariant()} side faces nothing.");
                }
            }

            // LAW 2 · not one du of the perimeter wasted.
            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                bool horizontal = side is UndergroundComplex.RingSide.Near
                    or UndergroundComplex.RingSide.Far;
                (double Lo, double Hi) wall = horizontal ? (park.X0, park.X1) : (park.Y0, park.Y1);

                var pieces = new List<(double Lo, double Hi)>();
                foreach (UndergroundComplex.RingRoom r in park.Frontage)
                {
                    if (r.Side == side && r.HasView)
                    {
                        pieces.Add(Frontage(r));
                    }
                }
                if (side == UndergroundComplex.RingSide.Near)
                {
                    pieces.Add((bar.X0, bar.X1));
                }
                foreach (SurfaceLayout.Doorway g in park.Ways)
                {
                    if (GateSide(park, g) == side)
                    {
                        pieces.Add(GateGround(g));
                    }
                }
                foreach ((double lo, double hi) in Gaps(wall, pieces))
                {
                    wrong.Add($"  {name}: {hi - lo:F1} du of the "
                        + $"{side.ToString().ToUpperInvariant()} wall ({lo:F1}…{hi:F1}) faces nothing.");
                }
            }

            // LAW 3 · every room opens onto a street, never onto the green.
            UndergroundComplex.ParkBlock made = UndergroundComplex.BlockOn(ground);
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                double want = room.Side switch
                {
                    UndergroundComplex.RingSide.Near => room.Y1,
                    UndergroundComplex.RingSide.Far => room.Y0,
                    UndergroundComplex.RingSide.West => room.X0,
                    _ => room.X1,
                };
                double street = room.Side switch
                {
                    UndergroundComplex.RingSide.Near => made.SpineFaceY,
                    UndergroundComplex.RingSide.Far => made.BackStreetY1,
                    UndergroundComplex.RingSide.West => made.WestInnerX,
                    _ => made.EastInnerX,
                };
                if (Math.Abs(want - street) > 0.001)
                {
                    wrong.Add($"  {name}: ring room {room.Number} does not open onto a street.");
                }
            }

            // LAW 4 · a way through every wall.
            foreach (UndergroundComplex.RingSide side in Enum.GetValues<UndergroundComplex.RingSide>())
            {
                if (!park.Ways.Any(g => GateSide(park, g) == side))
                {
                    wrong.Add($"  {name}: the {side.ToString().ToUpperInvariant()} wall has no gate.");
                }
            }

            // …and the green never runs off the end of the world it was carved in.
            if (park.X0 < ground.LeftX + SurfaceLayout.EdgeMargin
                || park.X1 > ground.RightX - SurfaceLayout.EdgeMargin
                || park.Y0 < ground.BottomY)
            {
                wrong.Add($"  {name}: the park ({park.X0:F1},{park.Y0:F1})-({park.X1:F1},{park.Y1:F1}) "
                    + "leaves the field.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} problem(s) on ground the generator has never been handed:\n"
            + string.Join("\n", wrong));

        // ANTI-VACUITY, and it is the whole reason this guard exists: the five grounds really did produce
        // five different buildings. A suite that swept a hundred sites and met one shape would pass this
        // file without ever having asked the carve a second question.
        Assert.True(shapes.Count >= 4,
            "the mutated grounds all produced the same block, so this proved no more than the sweep did: "
            + string.Join(", ", shapes.Order()));
    }

    // ── (g2) WHAT IS WRITTEN ON THE RING ──────────────────────────────────────────────────────────────

    /// <summary>
    /// §13.8 · THE ROOMS WITH THE VIEW SAY WHAT A ROOM IS AND NEVER WHAT THE PLACE IS FOR.
    ///
    /// <para>The block's own register (<see cref="UndergroundComplex.ParkViewPlates"/>) is the one piece of
    /// new prose the Manhattan ruling ships, and it is six lines. Every one of them names a booking, a
    /// signature or an appointment; none of them names the facility, its purpose, or anything a captain
    /// could take to an inspector. The nearest any of them comes to a sentence is #770's negotiation room,
    /// and all that plate says is where you book it.</para>
    ///
    /// <para>…and it is HUNG WHERE THE VIEW IS, which is the amenity gradient (#775) as signage: a corner
    /// room, which stands past the end of the park's own wall, wears the corridors' ordinary vocabulary
    /// instead. Read along one wall of the block and the rooms get better as the green comes into
    /// view.</para>
    /// </summary>
    [Fact]
    public void TheRingsOwnPlatesSayNothingTheBuildingWouldNotSay()
    {
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];
        foreach (string plate in UndergroundComplex.ParkViewPlates)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, plate, StringComparison.OrdinalIgnoreCase);
            }
            Assert.Equal(plate.ToUpperInvariant(), plate);
            Assert.False(plate.EndsWith('.'), $"\"{plate}\" is a sentence — a stencil is not.");
        }
        Assert.Equal(
            UndergroundComplex.ParkViewPlates.Count,
            UndergroundComplex.ParkViewPlates.Distinct(StringComparer.Ordinal).Count());

        // …and it is on the ring, on the rooms with the view, and NOT on the corners.
        int viewed = 0, cornered = 0, blocks = 0;
        foreach ((string body, int level, UndergroundComplex.Hall _, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan _) in EveryBlock())
        {
            blocks++;
            if (UndergroundComplex.IsFound(body, level))
            {
                continue;   // a gallery has no plates at all (#677), which is a different law
            }
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (room.Side == UndergroundComplex.RingSide.Far)
                {
                    Assert.Contains(room.Plate, UndergroundComplex.ParkBackPlates);
                    continue;
                }
                if (room.HasView)
                {
                    viewed++;
                    Assert.Contains(room.Plate, UndergroundComplex.ParkViewPlates);
                }
                else
                {
                    cornered++;
                    Assert.DoesNotContain(room.Plate, UndergroundComplex.ParkViewPlates);
                }
            }
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were read — this proved little.");
        Assert.True(viewed > 100, $"only {viewed} rooms with a view were read — this proved little.");
        Assert.True(cornered > 40, $"only {cornered} corner rooms were read — the gradient is untested.");
    }

    // ── (g3) A SIGN YOU HAVE TO STEP OFF TO READ IS NOT SIGNAGE ───────────────────────────────────────

    /// <summary>
    /// #775's LESSON, RE-LEARNED ON FOURTEEN ROOMS A FLOOR: no ring room's plate stands on its own doorway.
    ///
    /// <para>#775 paid for this once already, on the hall's own front doors: <i>"a plate centred on its own
    /// doorway is a plate with the captain standing on top of it the moment they arrive — watched happen in
    /// the browser on the first boot of <c>?frontdoor=1</c>, the dot sitting squarely on the word
    /// CANTEEN."</i> The block shipped the identical mistake on every room of the ring, and it was found the
    /// identical way: booting <c>?parkwalk=1</c> and reading the pixels, with the avatar sitting in the
    /// middle of PRIVILEGED RECORDS · READING ROOM.</para>
    ///
    /// <para>Which is the argument for this guard rather than for a careful placer. The geometry was right,
    /// every other law in this file was green, and the only instrument that could see it was a screenshot.
    /// A law that can only be checked by eye gets checked once; this one is checked on every floor.</para>
    ///
    /// <para><b>Proven RED</b> by putting the plate back on the door's own centre
    /// (<c>plateAt = mid</c> in <c>RingBox</c>):</para>
    /// <code>
    /// 728 plate(s) are standing in their own doorway:
    ///   luna B1: ring room 1 (NEAR) — CONSENT FILES — reads from 0.0 du off the middle of its own door.
    ///   luna B1: ring room 2 (NEAR) — SENIOR ROTA · GREEN SIDE — reads from 0.0 du off the middle of its
    ///     own door.
    ///   luna B1: ring room 3 (NEAR) — PRIVILEGED RECORDS · READING ROOM — reads from 0.0 du off the middle
    ///     of its own door.
    ///   luna B1: ring room 5 (FAR) — 🚿 WASH-DOWN — reads from 0.0 du off the middle of its own door.
    /// </code>
    ///
    /// <para>Ring room 3 is the one in the screenshot: PRIVILEGED RECORDS · READING ROOM, with the avatar
    /// sitting on the word RECORDS.</para>
    /// </summary>
    [Fact]
    public void NoRingRoomsPlateStandsInItsOwnDoorway()
    {
        var wrong = new List<string>();
        int blocks = 0, plates = 0;

        foreach ((string body, int level, UndergroundComplex.Hall _, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryBlock())
        {
            blocks++;
            if (UndergroundComplex.IsFound(body, level))
            {
                continue;   // a gallery hangs no plates at all (#677)
            }

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                bool horizontal = room.Side is UndergroundComplex.RingSide.Near
                    or UndergroundComplex.RingSide.Far;
                double doorAt = horizontal
                    ? (room.Door.X1 + room.Door.X2) / 2.0
                    : (room.Door.Y1 + room.Door.Y2) / 2.0;
                (double lo, double hi) = Frontage(room);

                // The label this room hung: its own text, standing within the room's own span and within a
                // few du of the wall its door is in. Matched off the published box rather than by index,
                // because plates repeat and an index would pair a room with somebody else's sign.
                bool found = false;
                foreach (SurfaceLayout.Landmark mark in floor.Labels)
                {
                    if (!string.Equals(mark.Label, room.Plate, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    double along = horizontal ? mark.X : mark.Y;
                    double across = horizontal ? mark.Y : mark.X;
                    double street = horizontal
                        ? (room.Side == UndergroundComplex.RingSide.Near ? room.Y1 : room.Y0)
                        : (room.Side == UndergroundComplex.RingSide.West ? room.X0 : room.X1);
                    if (along < lo - 0.5 || along > hi + 0.5 || Math.Abs(across - street) > 4.0)
                    {
                        continue;   // somebody else's plate that happens to read the same
                    }

                    found = true;
                    if (Math.Abs(along - doorAt) < UndergroundComplex.DoorHalf)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                            + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate} — reads from "
                            + $"{Math.Abs(along - doorAt):F1} du off the middle of its own door.");
                    }

                    // …and it stays on its OWN frontage. A plate stepped aside so far that it hangs over
                    // the neighbour's wall is a different way of being unreadable.
                    if (along < lo + 0.5 || along > hi - 0.5)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number}'s plate at {along:F1} is "
                            + $"outside its own frontage ({lo:F1}…{hi:F1}).");
                    }
                }

                if (!found)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                        + $"({room.Side.ToString().ToUpperInvariant()}) hangs no plate the deck draws.");
                }
                else
                {
                    plates++;
                }
            }
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(plates > 500, $"only {plates} plates were read — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} plate(s) are standing in their own doorway:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    // ── (h) THE ROOMS THE OWNER ASKED FOR ─────────────────────────────────────────────────────────────

    /// <summary>
    /// THEY ARE THE BEST ROOMS IN THE BUILDING, and the plan says so in floor area. A ring room with a view
    /// is several times an ordinary chamber; a CORNER room — one that stands past the end of the park's own
    /// wall and has nothing to look at — is the cheap one, which is #775's amenity gradient drawn on the
    /// deck rather than written in a sentence.
    /// </summary>
    [Fact]
    public void TheRoomsWithTheViewArePremiumAndTheCornersAreNot()
    {
        var wrong = new List<string>();
        int blocks = 0;
        double chamber = UndergroundComplex.RoomWidthDu * UndergroundComplex.RoomHeightDu;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan _) in EveryBlock())
        {
            blocks++;

            Assert.True(park.Frontage.Count >= 8,
                $"{body} B{-level}: {park.Frontage.Count} rooms on the whole ring.");

            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (room.FloorDu2 < chamber)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} is {room.FloorDu2:F0} du², "
                        + $"under the building's own chamber ({chamber:F0} du²).");
                }
                if (room.Plate.Length == 0 && !UndergroundComplex.IsFound(body, level))
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} has no plate on it.");
                }
            }

            // At least one corner room, and the corners have no view — the gradient exists.
            Assert.Contains(park.Frontage, r => !r.HasView);
            Assert.Contains(park.Frontage, r => r.HasView);

            // …and the park still dwarfs the hall that looks at it, which is #759's own law re-anchored.
            double hallFloor = (hall.X1 - hall.X0) * (hall.Y1 - hall.Y0);
            Assert.True(park.FloorDu2 >= 1.5 * hallFloor,
                $"{body} B{-level}: {park.FloorDu2:F0} du² of park behind {hallFloor:F0} du² of hall.");
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} ring room(s) are not what was asked for:\n" + string.Join("\n", wrong));
    }
}
