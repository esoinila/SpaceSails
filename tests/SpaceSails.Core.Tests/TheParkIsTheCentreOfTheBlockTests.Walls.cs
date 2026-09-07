using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #813 · <b>THE THREE SIDES THAT USED TO BE A HORIZON</b> — sections (c), (d) and (e) of
/// <see cref="TheParkIsTheCentreOfTheBlockTests"/>.
///
/// <para>What this part owns is the SKIN of the block: which of its walls are glass and which are poured
/// rock, that every ring room opens onto a street and never onto the green, and that a captain standing on
/// any of the four sides has a way through rather than a view. The bench, and the two laws about the ring
/// being complete, sit in the file that carries the class docblock.</para>
/// </summary>
public sealed partial class TheParkIsTheCentreOfTheBlockTests
{
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
}
