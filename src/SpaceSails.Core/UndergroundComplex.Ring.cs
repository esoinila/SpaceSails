using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: ONE ROOM ON THE RING, and one gate through it — the two placers the block's four bands are laid
// by. Lifted out of UndergroundComplex.Block.cs whole when #775 gave the block a second kind of floor to
// stand on: the block file is the LINES (where the bands are, how wide a room wants to be, which arithmetic
// decides how many) and this is the CARVE (four walls, the doors in them, the glass, and the furnisher).
// Not one line of either moved in the split, and the size gate is the reason the split happened at all.
public static partial class UndergroundComplex
{
    /// <summary>
    /// #821 · WHERE ON THE NEAR FRONTAGE THE PUBLIC WASHROOM STANDS, or NaN when this block cannot hold one.
    ///
    /// <para>The x of the room's own middle, so the laying loop can claim it with one comparison rather than
    /// counting rooms in the same nested order twice — a count taken in two places is two answers waiting to
    /// disagree, which is this file's own fourth named bug class.</para>
    ///
    /// <para>It walks the same segments, skips the same hall and splits with the same
    /// <see cref="RingRoomsIn"/> the laying loop does, and takes <b>the NARROWEST near suite that can still
    /// hold a terrace and a basin run</b> (<see cref="RingOffice.WashroomMinFrontageDu"/>). That is #775's
    /// amenity gradient one more time: a building does not give its best frontage to the WCs, and the widest
    /// suites on the block are the premium offices with the service tier in them. Ties go to the one nearest
    /// the middle of the block, and then to the LOWER x — so the answer never depends on the order a list
    /// happened to be built in.</para>
    /// </summary>
    private static double WashroomFrontageOn(in ParkBlock block, Hall? hall)
    {
        double middle = (block.X0 + block.X1) / 2.0;
        double best = double.NaN, bestWide = double.MaxValue, bestGap = double.MaxValue;

        foreach ((double lo, double hi) in RingNearSegments(block))
        {
            if (hall is { } venue && venue.X1 > lo + 0.001 && venue.X0 < hi - 0.001)
            {
                continue;
            }
            int n = RingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                double rx0 = lo + ((hi - lo) * k / n), rx1 = lo + ((hi - lo) * (k + 1) / n);
                double wide = rx1 - rx0;
                if (wide < RingOffice.WashroomMinFrontageDu)
                {
                    continue;
                }

                double at = (rx0 + rx1) / 2.0, gap = Math.Abs(at - middle);
                bool better = wide < bestWide - 0.001
                    || (wide < bestWide + 0.001
                        && (gap < bestGap - 0.001 || (gap < bestGap + 0.001 && at < best)));
                if (better)
                {
                    (best, bestWide, bestGap) = (at, Math.Min(wide, bestWide), gap);
                }
            }
        }

        return best;
    }

    /// <summary>#813 · One ring room: four walls, a door on the street, and — where it has any park in front
    /// of it — the glass that makes it one of these rather than a chamber.
    ///
    /// <para><paramref name="fromX"/>/<paramref name="fromY"/> is the STREET corner and
    /// <paramref name="toX"/>/<paramref name="toY"/> the PARK corner, so this one function lays a room on
    /// any of the four sides without ever asking which side it is on — the same trick <see cref="CarveHall"/>
    /// plays with its u and v.</para></summary>
    private static RingRoom RingBox(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<(double Lo, double Hi, double PlateX, string Plate)> spineDoors,
        string bodyId, int level, bool found, int number, RingSide side,
        double fromX, double fromY, double toX, double toY, in ParkBlock block, bool gate,
        string? plateOverride = null, bool landscape = false, bool labs = false,
        List<LockedDoor>? locked = null, bool shut = false)
    {
        bool horizontal = side is RingSide.Near or RingSide.Far;
        double x0 = Math.Min(fromX, toX), x1 = Math.Max(fromX, toX);
        double y0 = Math.Min(fromY, toY), y1 = Math.Max(fromY, toY);

        // WHERE THE PARK IS, and how much of this room's front wall actually looks at it. A corner room
        // stands past the end of the park's own wall, so it has none — which is the amenity gradient (#775)
        // as geometry: the rooms with the view are the ones with the view.
        double faceLo = horizontal ? x0 : y0, faceHi = horizontal ? x1 : y1;
        double parkLo = horizontal ? block.X0 : block.Y0, parkHi = horizontal ? block.X1 : block.Y1;
        bool view = faceLo >= parkLo - 0.001 && faceHi <= parkHi + 0.001;
        double mid = (faceLo + faceHi) / 2.0;

        // ── THE FOUR WALLS. The park-facing one is laid last because it is the one that is sometimes glass,
        //    sometimes glass with a door in it, and on a corner room simply concrete.
        double parkLine = horizontal ? (side == RingSide.Near ? y0 : y1) : (side == RingSide.West ? x1 : x0);
        double streetLine = horizontal ? (side == RingSide.Near ? y1 : y0) : (side == RingSide.West ? x0 : x1);

        // the two side walls, which are also the pier between this room and its neighbour
        if (horizontal)
        {
            walls.Add(new(x0, y0, x0, y1, true));
            walls.Add(new(x1, y0, x1, y1, true));
        }
        else
        {
            walls.Add(new(x0, y0, x1, y0, true));
            walls.Add(new(x0, y1, x1, y1, true));
        }

        // ── THE STREET DOORS. On the near band the street is the SPINE, and its face is poured by the spine
        //    builder from one sorted list of spans — so the gaps are handed over rather than cut here, which
        //    is #585's one-gap law said about the one wall this room does not own.
        //
        //    #817 · THERE ARE SEVERAL OF THEM NOW. Owner, live, from inside one of these: "Oh just one door
        //    in a landscape office?" … "bigger spaces must have much more doors." The count is the room's own
        //    frontage divided by DoorsForFrontage's ratio, with #822's fire-code floor under it — never a
        //    number typed here — and they are spaced evenly down the face, so a room with ONE door still puts
        //    it exactly where it has always been (the frontage's midpoint) and nothing about the single-door
        //    case moved.
        // #775 · …and ONE on a room that will not open. The door law is about how many ways a body has out
        // of a space it is standing in, and nobody is standing in this one: a shut room is a leaf with a
        // plate on it and a wall behind, which is the owner's own illusion of scale and the reason #822's
        // fire code has never had anything to say about a locked chamber either.
        int leaves = shut
            ? 1
            : DoorsForFrontage(faceHi - faceLo, hasAnotherWayOut: gate && view);
        var openings = new List<SurfaceLayout.Doorway>(leaves);
        var cuts = new List<double>(leaves);
        for (int d = 0; d < leaves; d++)
        {
            double at = faceLo + ((faceHi - faceLo) * (d + 0.5) / leaves);
            cuts.Add(at);
            openings.Add(horizontal
                ? new SurfaceLayout.Doorway(at - DoorHalf, streetLine, at + DoorHalf, streetLine)
                : new SurfaceLayout.Doorway(streetLine, at - DoorHalf, streetLine, at + DoorHalf));
        }
        SurfaceLayout.Doorway door = openings[0];

        // ── THE PLATE. A room with the view gets the block's own register (ParkViewPlates); a CORNER room
        //    gets the building's ordinary one, which is the amenity gradient said in signage rather than in
        //    a sentence. The back of house keeps #801's plates, because those rooms did not become premium
        //    by acquiring a street door.
        string plate = found ? "" : SignFor(bodyId, level, $"hive:{level}:ring:{(int)side}:{number}");
        if (view && side != RingSide.Far && !landscape)
        {
            plate = found
                ? ""
                : ParkViewPlates[
                    (number + (int)(Frac(bodyId, $"hive:{level}:ring-view") * ParkViewPlates.Count))
                        % ParkViewPlates.Count];
        }
        if (side == RingSide.Far && !landscape)
        {
            plate = found
                ? ""
                : ParkBackPlates[
                    (number + (int)(Frac(bodyId, $"hive:{level}:park-back") * ParkBackPlates.Count))
                        % ParkBackPlates.Count];
        }

        // ── #775 · A LANDSCAPE SUITE IS NOT SOMEBODY'S PRIVATE OFFICE, so it may not wear a plate that says
        //    it is. #707's law is that rank is readable in PLUMBING: a principal plate is a room somebody
        //    signed things in, and the building hangs a private washroom off every one of them on a floor
        //    that breathes. A ring suite has no en-suite — its service strip's WCs are the STAFF's (#817) —
        //    so a register that dealt it QUOTA OFFICE would be the tell saying the opposite of itself, and
        //    the guard that watches both directions of it went red on 22 floors saying exactly that.
        //
        //    The re-plate walks the SAME seeded list from the SAME seed, one step on, so the room keeps the
        //    building's own vocabulary rather than acquiring a sign invented for the occasion. It is the
        //    identical fallback AddRoomsAlong takes when a chamber's cell is refused (#801), asked here for
        //    the same reason and against the same floor predicate.
        if (landscape && !found && IsPlumbed(bodyId, level) && IsPrincipalRoom(plate))
        {
            plate = NotPrincipal(bodyId, level, $"hive:{level}:ring:{(int)side}:{number}");
        }

        // #821 · …and the one room the block gives to everybody. Applied LAST, over whichever plate the
        // register would have dealt it, and never on a found floor — a gallery has no plates at all (#677),
        // and a room past the seam with a stencil on it would be this file telling on the band the building
        // denies having.
        if (plateOverride is { Length: > 0 } given && !found)
        {
            plate = given;
        }

        // ── WHERE THE PLATE READS FROM · BESIDE the door and never over it.
        //
        // #775 learned this the expensive way on the hall's own front doors: "a plate centred on its own
        // doorway is a plate with the captain standing on top of it the moment they arrive — watched happen
        // in the browser on the first boot of ?frontdoor=1, the dot sitting squarely on the word CANTEEN."
        // The ring shipped the same mistake on fourteen rooms a floor and it was found the same way, in the
        // browser, on the first boot of ?parkwalk=1: PRIVILEGED RECORDS · READING ROOM with the avatar in
        // the middle of it. A sign you have to step off to read is not signage.
        //
        // Stepped along the room's own wall rather than out into the corridor, and clamped inside the
        // room's span so a narrow room's plate cannot wander onto its neighbour's frontage.
        //
        // #817 · …and it is beside the FIRST door only, however many the frontage earned. Two plates on one
        // room would be two answers to "which room is this", and the room's own signage is the only thing
        // telling fourteen identical poured boxes apart.
        double aside = DoorHalf + 3.0;
        double plateAt = Math.Clamp(cuts[0] + aside, faceLo + 1.5, faceHi - 1.5);

        if (side == RingSide.Near)
        {
            for (int d = 0; d < cuts.Count; d++)
            {
                spineDoors.Add((cuts[d] - DoorHalf, cuts[d] + DoorHalf, plateAt, d == 0 ? plate : ""));
            }
        }
        else
        {
            // #817 · ONE SORTED SWEEP WITH A CURSOR THAT MAY ONLY MOVE FORWARD (§13.2), because this wall
            // has several holes in it now. The cuts are generated in ascending order and the sweep is
            // written as if they were not, for #587's own reason: a wall built by a cursor over a list it
            // was handed in the wrong order is the bug this file keeps a monument to.
            double cursor = horizontal ? x0 : y0, end = horizontal ? x1 : y1;
            foreach (double at in cuts)
            {
                double near = Math.Max(cursor, at - DoorHalf);
                if (near > cursor)
                {
                    walls.Add(horizontal
                        ? new SurfaceLayout.Wall(cursor, streetLine, near, streetLine, true)
                        : new SurfaceLayout.Wall(streetLine, cursor, streetLine, near, true));
                }
                cursor = Math.Max(cursor, at + DoorHalf);
            }
            if (end > cursor)
            {
                walls.Add(horizontal
                    ? new SurfaceLayout.Wall(cursor, streetLine, end, streetLine, true)
                    : new SurfaceLayout.Wall(streetLine, cursor, streetLine, end, true));
            }

            if (!found)
            {
                // #775 · A SHUT ROOM'S LEAF IS A LOCKED DOOR AND NOT A DOORWAY, which is exactly the shape
                // the chambers have used since the beginning (AddRoomsAlong): the plate is what [E] reads,
                // the wall behind it is real, and nothing about the room is a space the audit has to reach.
                if (shut && locked is not null)
                {
                    SurfaceLayout.Doorway one = openings[0];
                    locked.Add(new LockedDoor(one.X1, one.Y1, one.X2, one.Y2, plate));
                }
                else
                {
                    foreach (SurfaceLayout.Doorway leaf in openings)
                    {
                        doorways.Add(leaf);
                    }
                }
                labels.Add(new(
                    horizontal ? plateAt : streetLine + (side == RingSide.West ? -2.5 : 2.5),
                    horizontal ? streetLine + (side == RingSide.Far ? -2.5 : 2.5) : plateAt,
                    plate));
            }
        }

        // ── THE PARK-FACING WALL. Glass where there is a park behind it; on the far band a door is cut in
        //    it and the glass is the rest of it, which is what a potting shed's front actually looks like.
        SurfaceLayout.Wall? viewWall = null;
        SurfaceLayout.Doorway? parkDoor = null;
        gate &= !shut;   // #775 · a room nobody may enter off the street is not a way onto the green either
        if (view)
        {
            viewWall = horizontal
                ? new SurfaceLayout.Wall(x0, parkLine, x1, parkLine, true)
                : new SurfaceLayout.Wall(parkLine, y0, parkLine, y1, true);
            if (gate)
            {
                parkDoor = horizontal
                    ? new SurfaceLayout.Doorway(mid - DoorHalf, parkLine, mid + DoorHalf, parkLine)
                    : new SurfaceLayout.Doorway(parkLine, mid - DoorHalf, parkLine, mid + DoorHalf);
                glass.Add(horizontal
                    ? new SurfaceLayout.Wall(x0, parkLine, mid - DoorHalf, parkLine, true)
                    : new SurfaceLayout.Wall(parkLine, y0, parkLine, mid - DoorHalf, true));
                glass.Add(horizontal
                    ? new SurfaceLayout.Wall(mid + DoorHalf, parkLine, x1, parkLine, true)
                    : new SurfaceLayout.Wall(parkLine, mid + DoorHalf, parkLine, y1, true));
                if (!found)
                {
                    doorways.Add(parkDoor.Value);
                }
            }
            else
            {
                glass.Add(viewWall.Value);
            }
        }
        else
        {
            walls.Add(horizontal
                ? new SurfaceLayout.Wall(x0, parkLine, x1, parkLine, true)
                : new SurfaceLayout.Wall(parkLine, y0, parkLine, y1, true));
        }

        claimed.Add((x0 - 1.5, y0 - 1.5, x1 + 1.5, y1 + 1.5));

        // ── #817 · AND WHAT IS ON THE FLOOR OF IT.
        //
        // Owner, live, standing in one of these on a bare deck: "It really needs tables … the cubicles etc
        // chairs maybe tables etc. It is way too empty." The room is furnished LAST, because a placer that
        // ran before the doors were cut would be measuring its clearances against a wall with no holes in
        // it — and it is furnished by RingOffice, which is handed the finished room and answers what is
        // standing in it. Nothing here decides where a desk goes.
        //
        // The solids go into the SAME wall list every other piece of furniture down here goes into (the
        // park's raised beds, the en-suite's pan) so one segment is both the drawing and the collision, and
        // the cubicles' leaves go into the same doorway list every door in the building is in — #821's lock
        // has to be able to find them without knowing what a WC is.
        // #775 · …and a SHUT room is furnished all the same, which is not an oversight. Its wall onto the
        // core is glass (the block's own fabric law: every du of the middle's perimeter that is not a gate
        // is a room's window), so a captain walking the promenade LOOKS INTO a department office they cannot
        // get into. An empty box behind that glass would be the plan admitting the room is scenery.
        var furnished = new RingRoom(
            number, x0, y0, x1, y1, side, door, viewWall, parkDoor, plate, openings);

        // #775 · WHAT IT IS DRESSED AS. Off the plate everywhere the plate says something — which is every
        // room the block round the park cuts — and told by the CARVE on a laboratories floor's back band,
        // because nothing on a door down there says LAB and the register would have to grow a word to say
        // it. See RingOffice.Fit's second overload for why that is the honest way round.
        RingOffice.Furnishing fit = labs && side == RingSide.Far
            ? RingOffice.Fit(in furnished, RingOffice.Dressing.LabHall)
            : RingOffice.Fit(in furnished);
        foreach (SurfaceLayout.Wall solid in fit.Solids)
        {
            walls.Add(solid);
        }
        if (!found && !shut)
        {
            foreach (SurfaceLayout.Doorway leaf in fit.Doors)
            {
                doorways.Add(leaf);
            }
        }

        return furnished with
        {
            Fittings = fit.Fixtures, Chairs = fit.Chairs, Cells = fit.Cells, Taps = fit.Taps,
            Shut = shut,
        };
    }

    /// <summary>#813 · One gate through the ring — a corridor's width of spur, and the park's own wall
    /// opened to a doorway where it arrives. The spur's two side walls ARE the party walls of the rooms
    /// either side of it, so a gate costs the ring a pier and never a room.</summary>
    private static void RingGate(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<SurfaceLayout.Doorway> gates, double at, double from, double to, bool vertical,
        IReadOnlyList<SurfaceLayout.Doorway>? abutting = null)
    {
        if (vertical)
        {
            // The two side walls, in the segments left between whatever opens off them. One sorted sweep
            // with a cursor that may only move forward (§13.2) — #587's own law, said about a corridor
            // whose neighbour is a room somebody else carved.
            foreach (int face in (int[])[-1, +1])
            {
                double wx = at + (face * CorridorHalf);
                var cuts = new List<(double Lo, double Hi)>();
                foreach (SurfaceLayout.Doorway o in abutting ?? [])
                {
                    if (Math.Abs(o.X1 - wx) < 0.001)
                    {
                        cuts.Add((Math.Min(o.Y1, o.Y2), Math.Max(o.Y1, o.Y2)));
                    }
                }
                cuts.Sort((a, b) => a.Lo.CompareTo(b.Lo));

                double lo = Math.Min(from, to), hi = Math.Max(from, to), cursor = lo;
                foreach ((double clo, double chi) in cuts)
                {
                    if (clo > cursor)
                    {
                        walls.Add(new(wx, cursor, wx, Math.Min(clo, hi), true));
                    }
                    cursor = Math.Max(cursor, chi);
                }
                if (cursor < hi)
                {
                    walls.Add(new(wx, cursor, wx, hi, true));
                }
            }
            walls.Add(new(at - CorridorHalf, to, at - DoorHalf, to, true));
            walls.Add(new(at + DoorHalf, to, at + CorridorHalf, to, true));
            var gate = new SurfaceLayout.Doorway(at - DoorHalf, to, at + DoorHalf, to);
            doorways.Add(gate);
            gates.Add(gate);
            claimed.Add((
                at - CorridorHalf - 1.5, Math.Min(from, to) - 1.5,
                at + CorridorHalf + 1.5, Math.Max(from, to) + 1.5));
        }
        else
        {
            walls.Add(new(from, at - CorridorHalf, to, at - CorridorHalf, true));
            walls.Add(new(from, at + CorridorHalf, to, at + CorridorHalf, true));
            walls.Add(new(to, at - CorridorHalf, to, at - DoorHalf, true));
            walls.Add(new(to, at + DoorHalf, to, at + CorridorHalf, true));
            var gate = new SurfaceLayout.Doorway(to, at - DoorHalf, to, at + DoorHalf);
            doorways.Add(gate);
            gates.Add(gate);
            claimed.Add((
                Math.Min(from, to) - 1.5, at - CorridorHalf - 1.5,
                Math.Max(from, to) + 1.5, at + CorridorHalf + 1.5));
        }
    }
}
