using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// #251 · Split from UndergroundComplex.Park.cs, moved verbatim: the park's carving (CarvePark) and the
// walk cut down to it (GardenWalkX). Every field of the park — the plates, the crops, the back rooms'
// list, the way sign — stays in UndergroundComplex.Park.cs (#1163).
public static partial class UndergroundComplex
{
    /// <summary>
    /// #759 · THE PARK, CARVED — the one room in the building that is not a box off a corridor.
    ///
    /// <para>Laid in the park's own two axes, the hall's discipline: <b>u</b> runs along the spine and
    /// <b>w</b> runs outward from the wall it shares with the hall. It owns three walls and half of a
    /// fourth: the far wall, the two ends, and the near wall in the segments left over once the corridor's
    /// gate and the hall's GLASS have been taken out of it. Neither of those two openings is cut here —
    /// the gate is the rib's own far end (the corridor stops being a dead end) and the glass is the hall's
    /// own far wall — which is #585's one-gap law said about a room that has two neighbours.</para>
    ///
    /// <para><b>The walk comes first and the planting is laid around it</b>, which is the whole reason a
    /// park drawn on a square grid can have a curve in it. The centre-line is a smooth line the length of
    /// the room; a bed is only laid where it clears that line by a walk's half-width and then some. Do it
    /// the other way — beds first, path threaded after — and the path is whatever the beds left, which is
    /// how a garden becomes a maze and how a guard stops being able to fail.</para>
    /// </summary>
    private static Park CarvePark(
        List<SurfaceLayout.Wall> walls, string bodyId, int level, in Hall hall, SurfaceLayout.Wall glass,
        in ParkBlock block, IReadOnlyList<SurfaceLayout.Doorway> gates, IReadOnlyList<RingRoom> ring)
    {
        // ── #813 · THE BOX IS THE BLOCK'S. Every one of the park's four walls belongs to a room or to a
        //    gate (CarveRing), so nothing here pours a single boundary segment: what is left for this method
        //    is the GROUND — the walk, the planting, the benches, the masts, and the one figure sitting at
        //    the far end of it.
        //
        //    That is a real narrowing and it is the point. The park used to own three of its own walls and
        //    half of a fourth, which meant the shape of the room and the shape of the fabric around it were
        //    two answers to one question; now the fabric IS the shape, and a wall the ring did not lay is a
        //    wall the park does not have.
        double x0 = block.X0, x1 = block.X1;
        double y0 = block.Y0, y1 = block.Y1;
        double depth = y1 - y0;
        double W(double w) => y1 - w;

        // WHICH GATE THE ROOM'S PLATE AND ITS DEV ROUTE ARE PINNED TO: the one on the hall's own side of
        // the green, nearest the bar, which is the gate a drinker walks out of. #775's rule, said about a
        // wall that now has six openings in it instead of two.
        SurfaceLayout.Doorway first = gates[0];
        double bestD = double.MaxValue;
        foreach (SurfaceLayout.Doorway g in gates)
        {
            if (Math.Abs(g.Y1 - y1) > 0.001 || Math.Abs(g.Y2 - y1) > 0.001)
            {
                continue;   // not on the near wall: a gate off the back street or one of the two ends
            }
            double d = Math.Abs(((g.X1 + g.X2) / 2.0) - ((hall.X0 + hall.X1) / 2.0));
            if (d < bestD)
            {
                (first, bestD) = (g, d);
            }
        }
        double gateX = (first.X1 + first.X2) / 2.0;

        // ── THE WALK · one long curve down the room, and a spur in from the gate.
        double uLo = x0 + ParkEdgeClearDu, uHi = x1 - ParkEdgeClearDu;
        double span = uHi - uLo;
        double mid = depth / 2.0;
        double amp = Math.Max(1.0, mid - ParkEdgeClearDu - ParkWalkHalfDu - 2.0);
        double Curve(double u) =>
            mid + (amp * Math.Sin(2 * Math.PI * ParkWalkBends * (u - uLo) / span));

        var walk = new List<(double X, double Y)>();
        double gateU = Math.Clamp(gateX, uLo, uHi);
        for (double w = 0; w < Curve(gateU); w += 1.5)
        {
            walk.Add((gateU, W(w)));      // in from the gate, until it meets the long walk
        }
        for (double u = uLo; u <= uHi + 0.001; u += 1.5)
        {
            walk.Add((u, W(Curve(u))));
        }

        // ── THE BEDS · a grid of raised boxes, minus every one that would stand on the walk.
        var beds = new List<GrowingBed>();
        double bu0 = uLo + ParkBedHalfWDu, bu1 = uHi - ParkBedHalfWDu;
        double bw0 = ParkEdgeClearDu + ParkBedHalfHDu, bw1 = depth - ParkEdgeClearDu - ParkBedHalfHDu;
        // #813 · The grid is a du tighter both ways than the band-shaped park's was. The green kept its area
        // when it became a block and it lost most of its LENGTH, and a planting pitch measured for a room
        // six times wider than it is deep leaves a garden with six beds in it — which is what the first
        // Manhattan carve produced, and the number a guard measures rather than a paragraph.
        int cols = Math.Max(1, (int)((bu1 - bu0) / ((2 * ParkBedHalfWDu) + 5)) + 1);
        int rows = Math.Max(1, (int)((bw1 - bw0) / ((2 * ParkBedHalfHDu) + 2)) + 1);
        double clearU = ParkBedHalfWDu + ParkWalkHalfDu + 1.0;
        double clearW = ParkBedHalfHDu + ParkWalkHalfDu + 1.0;

        // #813 · …and minus every one that would stand in front of a GATE. There are six of them now and
        // they arrive on all four sides, so "the ground in front of a door is clear" stopped being a thing
        // the old single gate got for free off the gate spur's own walk. A bed across a doorway is the
        // shape of bug this file keeps a table of: drawn correct, walked shut.
        var mouths = new List<(double X, double Y)>(gates.Count);
        foreach (SurfaceLayout.Doorway g in gates)
        {
            mouths.Add(((g.X1 + g.X2) / 2.0, (g.Y1 + g.Y2) / 2.0));
        }

        // …and the back of house's own doors onto the gravel (#801) are mouths in this wall exactly as the
        // gates are. They are not in `gates` — a Way is a way THROUGH the park and these are ways OUT of it
        // into a room, a distinction Park.BackDoors has kept since #801 — and leaving them out of THIS list
        // is what put a floodlight mast in front of two of them on every floor in the game. Watched go red.
        foreach (RingRoom room in ring)
        {
            if (room.Gate is { } gravel)
            {
                mouths.Add(((gravel.X1 + gravel.X2) / 2.0, (gravel.Y1 + gravel.Y2) / 2.0));
            }
        }

        for (int r = 0; r < rows; r++)
        {
            double bw = rows == 1 ? (bw0 + bw1) / 2.0 : bw0 + (r * (bw1 - bw0) / (rows - 1));
            for (int c = 0; c < cols; c++)
            {
                double bu = cols == 1 ? (bu0 + bu1) / 2.0 : bu0 + (c * (bu1 - bu0) / (cols - 1));

                // Would it stand on the gravel? Asked of the walk the room actually published, sample by
                // sample — never of the formula, which is the same discipline as measuring the tops rather
                // than reading the seat target.
                bool blocked = false;
                foreach ((double px, double py) in walk)
                {
                    double pw = Math.Abs(py - y1);
                    if (Math.Abs(px - bu) < clearU && Math.Abs(pw - bw) < clearW)
                    {
                        blocked = true;
                        break;
                    }
                }

                double by = W(bw);
                foreach ((double mx, double my) in mouths)
                {
                    blocked |= Math.Abs(mx - bu) < ParkBedHalfWDu + ParkWalkHalfDu + 1.0
                        && Math.Abs(my - by) < ParkBedHalfHDu + ParkWalkHalfDu + 1.0;
                }
                if (blocked)
                {
                    continue;
                }

                // ── #874 · AND IT IS SOLID, WHICH IS WHAT THE ART HAS ALWAYS SAID IT WAS.
                //
                //    It was four rails. Four rails round a box fourteen deck units by seven leave a
                //    12.6 × 5.6 du pocket of perfectly standable floor with no way in — 275 lattice squares
                //    a body fits on and no route on this floor can end in, nine times over, in every park
                //    in the game. Nobody could ever SEE that: the bed is DRAWN as a filled box and the
                //    owner's own complaint about it (#866) was that his finger would not take him there.
                //
                //    So it is laid with the one thing in this codebase that means SOLID —
                //    SurfaceLayout.AddSolidMass, #586's own answer to exactly this on the monolith, where a
                //    sealed cavity in a slab of stone read as 99 cells of ground nobody could reach. Same
                //    outline, to the coordinate; the inside simply stops being a place.
                SurfaceLayout.AddSolidMass(walls,
                    bu - ParkBedHalfWDu, by - ParkBedHalfHDu,
                    bu + ParkBedHalfWDu, by + ParkBedHalfHDu, true);

                beds.Add(new GrowingBed(
                    beds.Count + 1, bu, by, ParkBedHalfWDu, ParkBedHalfHDu,
                    ParkCrops[beds.Count % ParkCrops.Count]));
            }
        }

        // ── THE BENCHES · one at every bend, on the outside of it, where a bench goes.
        var benches = new List<(double X, double Y)>();
        for (int k = 0; k < 2 * ParkWalkBends; k++)
        {
            double u = uLo + (span * (0.25 + (k * 0.5)) / ParkWalkBends);
            double w = Curve(u);
            double off = w > mid ? ParkWalkHalfDu + 1.6 : -(ParkWalkHalfDu + 1.6);
            double by = W(w + off);
            benches.Add((u, by));
            walls.Add(new(u - ParkBenchHalfDu, by, u + ParkBenchHalfDu, by, true));
        }

        // ── THE MASTS · the artificial day, standing along the far pavement. The far wall used to be the
        //    edge of the world and is a row of shop fronts now, so a mast is STEPPED ASIDE where one would
        //    otherwise stand in front of somebody's door — nudged, never dropped: the artificial day is what
        //    makes an underground garden legible and a park with two lamps in it is a car park.
        var masts = new List<(double X, double Y)>();
        double mastW = depth - (ParkEdgeClearDu / 2.0);
        foreach (double at in ParkMastXs(x0, x1))
        {
            double my = W(mastW), u = at;
            foreach ((double mx, double gy) in mouths)
            {
                if (Math.Abs(gy - my) >= DoorHalf + ParkEdgeClearDu || Math.Abs(mx - u) >= DoorHalf + 2.0)
                {
                    continue;
                }
                double step = DoorHalf + 2.0 + 0.1;
                u = mx + (mx <= (x0 + x1) / 2.0 ? step : -step);
            }
            u = Math.Clamp(u, x0 + 1.0, x1 - 1.0);
            masts.Add((u, my));
            walls.Add(new(u - 0.6, my - 0.6, u + 0.6, my - 0.6, true));
            walls.Add(new(u - 0.6, my + 0.6, u + 0.6, my + 0.6, true));
            walls.Add(new(u - 0.6, my - 0.6, u - 0.6, my + 0.6, true));
            walls.Add(new(u + 0.6, my - 0.6, u + 0.6, my + 0.6, true));
        }

        // ── #801/#813 · THE BACK OF HOUSE, which is now the ring's FAR band. It is published in both
        //    shapes on purpose and it is ONE set of rooms: Park.Rooms is the view #801's own consumers were
        //    written against (a box, a door off the gravel, a plate) and Park.Frontage is the whole ring.
        //    A second carve for the second shape is the mirrored-constant bug with a record's clothes on.
        var back = new List<BackRoom>();
        foreach (RingRoom room in ring)
        {
            if (room.Side == RingSide.Far && room.Gate is { } gravel)
            {
                back.Add(new BackRoom(room.X0, room.Y0, room.X1, room.Y1, gravel, room.Plate));
            }
        }

        // The lone figure, on the bench furthest from the gate. Scenery: the owner's own "benches, the lone
        // figure, the curve that hides the far end", and nothing to press — a park that started offering
        // things would be a park that had noticed you.
        (double figX, double figY) = benches[0];
        foreach ((double bx, double by) in benches)
        {
            if (Math.Abs(bx - gateX) > Math.Abs(figX - gateX))
            {
                (figX, figY) = (bx, by);
            }
        }

        // #775 · Every gate as a published doorway, the hall's own FIRST — it is the one the room's plate
        // and its dev route are pinned to, and the order is the only thing that says which is which.
        var ordered = new List<SurfaceLayout.Doorway>(gates.Count) { first };
        foreach (SurfaceLayout.Doorway g in gates)
        {
            if (g != first)
            {
                ordered.Add(g);
            }
        }

        return new Park(
            x0, y0, x1, y1, walk, beds, benches, masts,
            first,
            glass,
            gateX, W(4.0), figX, figY,
            CanteenRegulars.StrangerPlates[
                (int)(Frac(bodyId, $"hive:{level}:park-figure") * CanteenRegulars.StrangerPlates.Count)
                    % CanteenRegulars.StrangerPlates.Count],
            ParkArtFor(bodyId, HallUseOn(bodyId, level)),
            ordered,
            back,
            ring);
    }

    /// <summary>
    /// #775 · WHERE THE DEDICATED WALK DOWN TO THE PARK IS CUT, or null where this floor has no room for
    /// one.
    ///
    /// <para>Owner: <i>"multiple doors to the park"</i> — and the corridors that already reach it are the
    /// ribs pointing its way, which on a quarter of the shipped sites is exactly ONE (the hall's own). A
    /// park with one door is a cul-de-sac however many ribs happen to fall the right way, so the building
    /// gets a passage whose only job is that crossing: off the main corridor, straight down, into the
    /// green.</para>
    ///
    /// <para>It is placed at the point on the spine's park-side face FURTHEST from everything already
    /// standing on that side — the hall's box, the corridors that reach the park, and the lift alcove where
    /// the park is on the alcove's own face. Furthest rather than first-fit because the room columns either
    /// side of a rib are ground this passage would otherwise take: the claim ledger would drop them
    /// silently, and a floor quietly losing chambers is the shape of bug this file keeps a table of.</para>
    /// </summary>
    private static double? GardenWalkX(
        List<Rib> ribs, bool parkSide, in Hall hall, double shaftX, double? serviceX,
        double leftEnd, double rightEnd)
    {
        var keepOff = new List<(double Lo, double Hi)> { (hall.X0, hall.X1) };
        foreach (Rib r in ribs)
        {
            if (r.Down == parkSide)
            {
                keepOff.Add((r.X - CorridorHalf, r.X + CorridorHalf));
            }
        }
        if (!parkSide)
        {
            keepOff.Add((shaftX - ShaftHalf, shaftX + ShaftHalf));   // the alcove hangs off the top face
        }
        else if (serviceX is { } carX)
        {
            // #801 · …and on the other face the goods car stands, at the blind end — which is precisely
            // where this max-min search likes to land, because the emptiest x on a face is very often the
            // last one. Without this clause the walk down to the green and the second car would have been
            // cut into the same six du of wall.
            keepOff.Add((carX - ShaftHalf, carX + ShaftHalf));
        }

        double clear = CorridorHalf + 2.0;
        // …and it keeps a wall's worth of ground off the ends of the building, which the max-min search
        // would otherwise walk straight into: the emptiest x on this face is very often the last one.
        double lo = leftEnd + clear + CorridorHalf, hi = rightEnd - clear - CorridorHalf;
        double bestX = double.NaN, bestRoom = clear;

        for (double x = lo; x <= hi + 0.001; x += 1.0)
        {
            double room = double.MaxValue;
            foreach ((double a, double b) in keepOff)
            {
                room = Math.Min(room, x < a ? a - x : x > b ? x - b : 0.0);
            }
            if (room > bestRoom)
            {
                (bestRoom, bestX) = (room, x);
            }
        }
        return double.IsNaN(bestX) ? null : bestX;
    }
}
