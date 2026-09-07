using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #585/#822/#818 · THE ROOMS — the three passes of <see cref="Build"/> that lay them, publish them, and
/// put something on the floor of every one of them.
///
/// <para>They run in this order and it is not arbitrary. A room cannot be published before it is laid; the
/// takers (an amenity, a refuge) cannot choose before the pool is full; and nothing can be furnished
/// before every way out of it is cut, because a furnisher measures its clearances against holes. Each
/// pass's own comments carry the bug that taught the order.</para>
///
/// <para>#251 · Extracted from the 837-line <c>Build</c> as named passes, #1167's method. Every line of
/// every body is the line that was inline, in the order it was in. See
/// <c>UndergroundComplex.FloorPlan.cs</c>.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#585 · THE RIBS — cross corridors off the spine, with rooms flanking them, one pass per rib
    /// in the order the ribs were planned. The rooms are laid FIRST and hand back the mouths they cut, so
    /// the rib's own side walls stop either side of every door: two walls on one line, each correct on its
    /// own and neither aware of the other, is the shape of every expensive bug on this ground.</summary>
    private static void RunTheRibs(
        string bodyId, int level, in SurfaceLayout.Field field,
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways, List<LockedDoor> locked,
        List<Room> rooms, List<EnSuite> ensuites,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<(double X, bool Down)> ribXs, ParkBlock? blockOn, (int Rib, int Side)? hallSlot,
        double shaftY, double roomScale)
    {
        // ── THE RIBS. Cross corridors off the spine, with rooms flanking them.
        for (int i = 0; i < ribXs.Count; i++)
        {
            // #819 · There is no longer a test here for "is this entry actually an alcove". There cannot be
            // an alcove in this list: the cars hand their mouths to the wall sweep as spans of their own
            // width, and `ribXs` holds ribs. A skip kept out of caution would be a line asserting the
            // opposite of what the list above it says.
            (double x, bool down) = ribXs[i];
            if (blockOn is not null && down)
            {
                // #813 · The block's own gates. Their walls, their claim and the doorway at the end of them
                // were laid by CarveRing, because the walls either side of a gate ARE the party walls of the
                // rooms it runs between — one wall, laid once, by whichever placer owns both of its faces.
                // A second pass over them here is exactly the two-authors-one-line bug this file opens with
                // a table of.
                continue;
            }
            // #759 · The hall's rib runs longer than the rest, and this is the same call the carve made —
            // never a second answer. `hallRib` is also the one rib in the building whose far end is a WAY IN
            // rather than an end: the park is behind it.
            bool hallRib = hallSlot is { } onThis && onThis.Rib == i;
            (double mouth, double far) = RibReach(field, shaftY, down, hall: hallRib);

            // #585 · THE RIB'S OWN WALLS ARE CUT WHERE ROOMS OPEN OFF THEM. Owner: "a door is missing here
            // towards down", and his A* suggestion found it everywhere at once — 94 floors, not one room
            // reachable.
            //
            // The rooms cut a doorway in their OWN corridor-facing face, at x ± CorridorHalf. The rib's side
            // wall runs down that exact line. So every door in the building opened onto a wall: the plan drew
            // a facility and the collision field was a set of sealed boxes beside a sealed tube. Two walls on
            // one line, each correct on its own, and neither aware of the other — the same shape as every
            // expensive bug on this ground.
            // #822 · …and the rooms are laid FIRST now, because the face has one more kind of gap in it and
            // only the room placer knows where those fell. Nothing else about the order changes: the two
            // builders touch different walls, and the one wall they share is the one this hands over.
            (List<(double Lo, double Hi)> minusMouths, List<(double Lo, double Hi)> plusMouths) =
                AddRoomsAlong(
                    walls, doorways, locked, rooms, ensuites, claimed, bodyId, level, i, x, mouth, far, down,
                    roomScale, hallSlot is { } taken && taken.Rib == i ? taken.Side : 0);

            RibFace(walls, x - CorridorHalf, mouth, far, bodyId, level, i, -1, down, roomScale, minusMouths);
            RibFace(walls, x + CorridorHalf, mouth, far, bodyId, level, i, +1, down, roomScale, plusMouths);

            // The rib's far end. #585: it is ALWAYS closed — by a sealed door with a distance on it, or by a
            // plain wall. It was 40/60 before, and a corridor that simply stops in mid-air is the same
            // topology bug one level down ("a door is missing here towards down").
            //
            // #677 · NEVER a sealed mouth in the halls. `⟶ SECTOR 7 · 2.4 km` is a plate somebody stencilled,
            // and a stencil is a department, a survey and a decision about where somebody's authority stops.
            // Down here the passage simply ends in the same material as everything else, and the captain gets
            // no number to reason with — which is worse, and is the point.
            //
            // #759 · …EXCEPT THE ONE THAT IS THE PARK GATE. On the hall's rib, where a park was carved, the
            // corridor does not end: it opens, through a doorway cut to the same DoorHalf every other door
            // in the building is cut to, and that gap is the ONLY way a body gets into the park. (The wall
            // it shares with the hall is glass — an eye crosses it and nothing else does.) A sealed mouth
            // with a distance stencilled on it here would be a sign lying about a door you can see through.
            //
            // #775 · …AND IT IS NO LONGER THE ONLY ONE. Owner: "let's have multiple doors to the park — it
            // is a kind of place people like to walk through on their way." Every rib pointing the park's
            // way now runs the extra HallRibExtraDu that the hall's rib always ran and opens where it
            // arrives, so a route down one rib and up another crosses the green instead of going round it.
            // The extension is corridor, not room: the chambers were laid against `far` and nothing stands
            // in the sixteen du beyond it — that is the same unused band the park itself came out of.
            // #813 · Every rib that gets this far runs away from the block, so its far end is an end.
            // The one that used to be a way in is a gate through the ring now, and it never reaches here.
            if (!IsFound(bodyId, level) && Frac(bodyId, $"hive:{level}:rib-far:{i}") < 0.55)
            {
                double km = 0.8 + (Frac(bodyId, $"hive:{level}:rib-km:{i}") * 3.4);
                locked.Add(new(x - CorridorHalf, far, x + CorridorHalf, far,
                    SealedMouthSign(bodyId, i, km)));
            }
            walls.Add(new(x - CorridorHalf, far, x + CorridorHalf, far, true));

            // #751 · …and on the column the hall is standing on, no rooms at all. The rib's own face is
            // still built above (RibFace), with its doorway at every slot — those gaps ARE the hall's doors,
            // and they are the same gaps the corridor has because nothing ever cut a second set.
        }
    }

    /// <summary>#822 · WHAT THE FLOOR PUBLISHES AS A ROOM, AND IN WHICH ORDER THE TAKERS GET THEIR PICK —
    /// the ring's suites join the pool, the building as carved is taken, the amenities and then the refuge
    /// choose out of it, and the rooms nothing may ever take are appended after.
    ///
    /// <para>The order is the law here and every clause of it was paid for: a refuge chosen before the rib
    /// loop is an index that sometimes names nothing (#608), an amenity chosen after it could take the same
    /// room (#707), the park's back of house appended before either would let the building take back the one
    /// thing the garden exists to give (#801), and a sweep read off the pool after the takers had taken
    /// theirs would quietly stop asking about a canteen or a refuge at all (#822).</para></summary>
    private static (List<Room> Published, List<Amenity> Amenities, List<Refuge> Refuges) PublishTheRooms(
        string bodyId, int level, in SurfaceLayout.Field field, List<SurfaceLayout.Wall> walls,
        List<Room> rooms, List<EnSuite> ensuites, List<RingRoom> ring, List<MeetingRoom> meetings,
        HallSite? hallSite, double shaftX, double shaftY)
    {
        // #608 · LAST, because a refuge is taken out of the rooms this floor actually managed to build. Any
        // earlier and it would be a designated INDEX rather than a designated ROOM — and the claim ledger
        // above drops a room whenever one would sit on something already standing, so an index chosen before
        // the loop is an index that sometimes names nothing. That is exactly the shape of the bug KeyRoomFor
        // was written to avoid, and a safety regulation may not be the second thing in this file to trip
        // over it.
        // #707 · …and the amenities, out of the same pool and BEFORE the refuge, so the two can never take
        // the same room. They never compete in practice — an amenity is only ever plumbed on a floor that
        // holds pressure and a refuge is only ever carved on one that does not — but the order says so
        // rather than leaving it to be rediscovered.
        // #813 · …and the ring's own rooms join the pool BEFORE either of them, which is #775's amenity
        // gradient cashed out: a washroom on the park side of the block is a washroom with a window, and
        // "amenities follow rank" means the best room in the building is a candidate for the best of them.
        // The back of house is NOT in here — it goes in below, after both have chosen, for #801's reason.
        foreach (RingRoom room in ring)
        {
            if (room.Shut)
            {
                continue;   // #775 · not a space a captain can stand in. See CarveRing's back band.
            }
            if (room.Side != RingSide.Far)
            {
                // #818 · …carrying the furniture #817 already stood in it. The suites were furnished at the
                // carve and the sweep reads ONE list for the whole building, so what a ring room holds is
                // handed over here rather than looked up again through the park — a second reader of the
                // same fact is a second answer waiting to disagree with the first.
                rooms.Add(new Room(
                    room.X0, room.Y0, room.X1, room.Y1, room.Plate, room.WaysOut, RoomKind.RingSuite,
                    room.Furniture, room.Seats));
            }
        }

        // #822 · THE BUILDING AS CARVED, taken here — before the amenities and the refuge start REMOVING
        // rooms from the pool. A canteen is a chamber with a counter in it and a refuge is a chamber with a
        // tank in it: both are still rooms a captain stands in, both still have to have two ways out, and a
        // sweep read off the pool after they had taken theirs would quietly stop asking about them.
        var published = new List<Room>(rooms);

        List<Amenity> amenities = CarveAmenities(bodyId, level, rooms, walls, shaftX, shaftY, hallSite);
        List<Refuge> refuges = CarveRefuges(bodyId, level, rooms, field);

        // #801 · …and the park's back of house LAST of all, appended after both of those have chosen. They
        // are rooms — they hold what any room down here holds and the A* audit walks to every one of them —
        // but they are the garden's, and an amenity or a refuge carved out of one would be the building
        // taking back the thing this feature exists to give: somewhere on the far side of the green.
        // #775 · …asked of the RING and not of the park, which is what says whether one was carved at all.
        // The condition here used to be `park is not null`, and on a landscape floor — a block with a core
        // of meeting rooms in the middle of it rather than a garden — that would have left the whole far
        // band carved, doored, plated and never published: "34 doors were cut and only 28 of them lead
        // anywhere", the very sentence #813 recorded, one storey down.
        if (ring.Count > 0)
        {
            // #813 · …asked of the RING rather than of Park.Rooms, which is the #801 view of it and holds
            // only the ones with a door onto the gravel. The far band's two CORNER rooms stand past the end
            // of the park's own wall, so they have no gravel door and are not back rooms in #801's sense —
            // and reading the narrower list here left two rooms on every block floor with a door cut, a
            // plate hung and nothing behind them. Watched go red: "34 doors were cut and only 28 of them
            // lead anywhere."
            foreach (RingRoom room in ring)
            {
                if (room.Side == RingSide.Far && !room.Shut)
                {
                    var back = new Room(
                        room.X0, room.Y0, room.X1, room.Y1, room.Plate, room.WaysOut, RoomKind.RingSuite,
                        room.Furniture, room.Seats);
                    rooms.Add(back);
                    published.Add(back);
                }
            }
        }

        // #822 · …and the rooms that were never in the pool at all, because nothing may ever take them: the
        // hall, the cabinets down its outer wall, the WC cubicles in the block's washroom and the en-suite
        // cells hung off the principal chambers. The last three are the exemption the law is written with —
        // a booth and a cell are bedroom-small (see <see cref="FireCodeSmallRoomDu"/>) and keep their single
        // leaf, which is exactly what #821's catch is bolted to.
        if (hallSite is { } venue)
        {
            Hall theHall = venue.Hall;
            published.Add(new Room(
                theHall.X0, theHall.Y0, theHall.X1, theHall.Y1,
                AmenitySigns(bodyId, HallUseOn(bodyId, level)).Item1, theHall.Openings, RoomKind.Hall));
            foreach (Cabinet cab in theHall.Cabinets)
            {
                published.Add(new Room(
                    cab.X - cab.HalfW, cab.Y - cab.HalfH, cab.X + cab.HalfW, cab.Y + cab.HalfH,
                    cab.Plate, cab.Ways, RoomKind.Cabinet));
            }
        }
        foreach (RingRoom room in ring)
        {
            foreach (RingOffice.Stall cell in room.Cubicles)
            {
                published.Add(new Room(
                    cell.X0, cell.Y0, cell.X1, cell.Y1, cell.Plate, [cell.Door], RoomKind.Cubicle));
            }
        }

        // #775 · …and the meeting rooms in a landscape floor's core, out of the pool for the reason the
        // hall's cabinets are: they are the rooms a department books, not the rooms a canteen or a refuge
        // may be carved out of. Published so the fire code, the never-empty-floor sweep and the seat verb
        // all walk one list.
        foreach (MeetingRoom room in meetings)
        {
            published.Add(new Room(
                room.X0, room.Y0, room.X1, room.Y1, room.Plate, room.Ways, RoomKind.MeetingRoom,
                room.Furniture, room.Seats));
        }
        foreach (EnSuite cell in ensuites)
        {
            published.Add(new Room(
                cell.X - (EnSuiteDepth / 2.0), cell.Y - EnSuiteHalfHeight,
                cell.X + (EnSuiteDepth / 2.0), cell.Y + EnSuiteHalfHeight,
                cell.Of, cell.Ways, RoomKind.Cell));
        }
        return (published, amenities, refuges);
    }

    /// <summary>#818/#853/#864 · AND WHAT IS STANDING ON THE FLOOR OF EVERY ONE OF THEM — the chambers'
    /// furniture, the conference posters outside their doors and the incident board inside one of them.
    ///
    /// <para>Owner, generalising #817 past the ring: <i>"Same for labs etc spaces… they have chairs and
    /// desks and equipment … never ever empty floor."</i> HERE and not down in the room placer, because a
    /// furnisher that ran before the recesses and the fire doors were cut would be measuring its clearances
    /// against a wall with no holes in it yet. By this line every way out of every room is published and the
    /// furnisher can be handed the finished box.</para>
    ///
    /// <para>The solids go into the SAME wall list every other piece of furniture down here goes into, so
    /// one segment is both the drawing and the collision — and it happens BEFORE the bins, which measure
    /// their own clearance against every wall the floor ended up with and would otherwise fit a bin inside a
    /// fume hood.</para></summary>
    private static (IReadOnlyList<LabPosters.Poster> Posters, IncidentBoard.Board? Board) FurnishTheChambers(
        string bodyId, int level, List<SurfaceLayout.Wall> walls, List<EnSuite> ensuites,
        List<Room> published, double shaftX, double shaftY)
    {
        // ── #818 · AND WHAT IS STANDING ON THE FLOOR OF EVERY ONE OF THEM ────────────────────────────────
        //
        // Owner, generalising #817 past the ring: "Same for labs etc spaces… they have chairs and desks and
        // equipment … never ever empty floor."
        //
        // HERE, and not down in AddRoomsAlong, for the reason RingOffice is called last in the ring's carve:
        // a placer that ran before the recesses were cut would be measuring its clearances against a wall
        // with no holes in it yet, and #822's fire doors are the newest holes in this building. By this line
        // every way out of every room is published and the furnisher can be handed the finished box.
        //
        // The solids go into the SAME wall list every other piece of furniture down here goes into (the
        // park's raised beds, the en-suite's pan, the ring's desks) so one segment is both the drawing and
        // the collision — and it happens BEFORE the bins, which measure their own clearance against every
        // wall the floor ended up with and would otherwise fit a bin inside a fume hood.
        var cellLeaves = new List<SurfaceLayout.Doorway>(ensuites.Count);
        foreach (EnSuite cell in ensuites)
        {
            foreach (SurfaceLayout.Doorway leaf in cell.Ways)
            {
                // An en-suite's leaf is NOT a way out (a cell is a dead end, and the fire code says so), so
                // it is not in Room.Ways — and it is still a hole a body goes through, which is the only
                // thing the furnisher needs to know about it. Handed over as a floor-wide list rather than
                // matched to a parent: a leaf in somebody else's wall is too far away to claim any of this
                // room's line, so the conservative reading costs nothing and cannot mis-pair.
                cellLeaves.Add(leaf);
            }
        }

        string? department = ChamberFitting.DepartmentOn(bodyId, level);
        Kind trade = KindOn(bodyId, level);

        // …counted PER KIT as the floor is walked, which is what turns "a laboratories floor has fume hoods,
        // vacuum chambers and furnaces on it" from a probability into a fact. See ChamberFitting.Fit's
        // ordinal: a seeded pick left fifteen floors in the sweep short of one piece each.
        var dealt = new Dictionary<ChamberFitting.Kit, int>();
        for (int r = 0; r < published.Count; r++)
        {
            Room carved = published[r];
            if (carved.Kind != RoomKind.Chamber)
            {
                continue;   // the suites, the hall, its cabinets and the cells are furnished by their own
            }

            var holes = new List<SurfaceLayout.Doorway>(carved.Ways.Count + cellLeaves.Count);
            holes.AddRange(carved.Ways);
            holes.AddRange(cellLeaves);

            ChamberFitting.Kit trade0 = ChamberFitting.KitFor(carved.Plate, department, trade);
            int ordinal = dealt.TryGetValue(trade0, out int seen) ? seen : 0;
            dealt[trade0] = ordinal + 1;

            RingOffice.Furnishing kit = ChamberFitting.Fit(in carved, trade0, holes, ordinal);
            if (kit.Fixtures.Count == 0)
            {
                continue;   // a gallery, an empty store, or a room whose every wall is a doorway's clearance
            }

            published[r] = carved with { Fittings = kit.Fixtures, Chairs = kit.Chairs };
            foreach (SurfaceLayout.Wall solid in kit.Solids)
            {
                walls.Add(solid);
            }
        }

        // #853 · …and the conference posters, on the one department they are about. Hung off the chambers'
        // own doorways, so a floor whose rooms move takes its wall dressing with it.
        IReadOnlyList<LabPosters.Poster> posters =
            LabPosters.On(bodyId, level, published, shaftX, shaftY);

        // #864 · …and the incident board, on ONE lab chamber's own wall. A poster is signage for somebody
        // walking past a door; a safety board hangs INSIDE the room it is about, so it is placed by the
        // furnishing law (ChamberFitting's measured walls) rather than the signage one — handed every hole
        // on the floor, because an opening in somebody else's wall is too far away to claim any of the line
        // and the conservative list cannot mis-pair.
        var everyHole = new List<SurfaceLayout.Doorway>(cellLeaves);
        foreach (Room carved in published)
        {
            everyHole.AddRange(carved.Ways);
        }
        IncidentBoard.Board? board =
            IncidentBoard.On(bodyId, level, published, everyHole, shaftX, shaftY);
        return (posters, board);
    }
}
