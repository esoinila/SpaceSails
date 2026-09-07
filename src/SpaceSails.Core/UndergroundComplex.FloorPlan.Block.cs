using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #751/#775/#813 · WHERE THE BIG ROOM GOES, AND WHAT THE SPINE SAYS ABOUT IT — the four passes of
/// <see cref="Build"/> that decide how wide a chamber may grow, site the hall (on the block's near band,
/// or on a rib's room column on every other floor), and hang the doors the spine's own face was swept
/// around.
///
/// <para>The hall is FIRST because it is the only placer that cannot be refused: carved before the rib
/// loop and claimed immediately, so rooms, en-suites and refuges all see the box and step around it. The
/// alternative was to carve it last and delete the walls of whatever it had swallowed, which is the same
/// thing said in a way that can go wrong.</para>
///
/// <para>#251 · Extracted from the 837-line <c>Build</c> as named passes, #1167's method. Every line of
/// every body is the line that was inline, in the order it was in. The two siting passes take their
/// outputs by <c>ref</c> rather than returning them, so the moved lines are byte for byte the lines that
/// were inline. See <c>UndergroundComplex.FloorPlan.cs</c>.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#677 · HOW BIG THE CHAMBERS ARE ON THIS FLOOR, decided ONCE and handed to both builders —
    /// the wall builder and the room builder must be given the same number for the same reason they are
    /// already given the same centres function (#585): the doorway a room cuts and the gap its corridor
    /// leaves are one gap.</summary>
    private static double HowBigTheChambersAre(string bodyId, int level, List<Rib> ribList)
    {
        // ── #677 · HOW BIG THE CHAMBERS ARE ON THIS FLOOR, decided ONCE and handed to both builders.
        //
        // The wall builder and the room builder must be given the same number for the same reason they are
        // already given the same centres function (#585): the doorway a room cuts and the gap its corridor
        // leaves are one gap, and two copies of a scale would open a door onto a wall on every floor of every
        // hall in the game.
        //
        // CAPPED BY THE GROUND, not by a guess. Two ribs' facing room columns must not meet, so the widest a
        // chamber may grow is half the closest rib spacing this field actually produced, less the corridor it
        // opens off. Below that the claim ledger would simply drop rooms — correct, and silent, which is the
        // shape of bug this file's spec opens with a table of.
        double roomScale = RoomScaleOn(bodyId, level);
        if (roomScale > 1.0 && ribList.Count > 1)
        {
            double closest = double.MaxValue;
            for (int i = 1; i < ribList.Count; i++)
            {
                closest = Math.Min(closest, ribList[i].X - ribList[i - 1].X);
            }
            double widest = (closest / 2.0) - CorridorHalf;
            roomScale = Math.Min(roomScale, Math.Max(1.0, widest / RoomWidthDu));
        }
        return roomScale;
    }

    /// <summary>#775/#813 · THE BLOCK'S FLOOR — where the hall stands in the near band, the ring around the
    /// middle, and what is in the middle: a garden on the venue's floor, a core of meeting rooms on a
    /// landscape one.
    ///
    /// <para>In that order because the ring owns the middle's whole boundary: every wall it has is a room's
    /// glass or a gate's stub, so the middle is what is LEFT when the block has been built rather than a box
    /// with openings cut in it afterwards.</para>
    ///
    /// <para>Every output is <c>ref</c> rather than returned so the moved lines are the lines that were
    /// inline, unchanged — see this family's opening file for what the cut is and is not allowed to
    /// touch.</para></summary>
    private static void SiteTheBlock(
        string bodyId, int level, in ParkBlock block,
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<LockedDoor> locked, List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<(double Lo, double Hi)> hallSpineCuts,
        List<(double Lo, double Hi, double PlateX, string Plate)> ringSpineCuts,
        List<(double Lo, double Hi)> spineMouths, List<SurfaceLayout.Doorway> parkGates,
        double shaftX, double? serviceX, double left, double right, double roomScale,
        ref HallSite? hallSite, ref Park? park, ref List<RingRoom> ring,
        ref List<MeetingRoom> meetings, ref double hallSpineFaceY)
    {
        Comfort use = HallUseOn(bodyId, level);
        double mouth = block.SpineFaceY, far = block.Y1;

        // #775 · IS THERE A HALL ON THIS BLOCK AT ALL. On the one floor with a park there is, and it
        // takes the widest sub-segment of the near band. On a landscape floor there is not — the near
        // band is offices end to end — so the whole siting pass below is stepped over rather than run
        // against a canteen that is not on this floor.
        bool hasHall = HasParkBlock(bodyId, level);

        // ── WHICH SUB-SEGMENT OF THE BAND · #751's rule, unchanged, asked of the block's own list
        //    (RingNearSegments) rather than of the room slots down a rib: the best FLOOR wins and
        //    nearest-the-cage breaks the tie. The hall takes the whole of the segment it stands in
        //    unless what is left over would still make a room, and never leaves a strip too narrow to
        //    be one — the owner's "not unused" applied to the ground the biggest room in the building
        //    does not want.
        var wide = new List<Rib> { new(block.WestInnerX - CorridorHalf, true) };
        double wanted = !hasHall ? 0.0 : HallGround(
            bodyId, use, wide, 0, +1, mouth, far, shaftX, serviceX, left, right + 400.0, roomScale)
            is { } asked ? asked.Wanted : 0.0;

        double bestLo = double.NaN, bestW = 0, bestD2 = double.MaxValue;
        foreach ((double lo, double hi) in hasHall ? RingNearSegments(block) : [])
        {
            double span = hi - lo;
            double w = span <= wanted || span - wanted < RingRoomMinDu ? span : wanted;
            var pseudo = new List<Rib> { new(lo - CorridorHalf, true) };
            if (HallGround(
                    bodyId, use, pseudo, 0, +1, mouth, far, shaftX, serviceX,
                    left, lo + w + HallEdgePadDu, roomScale)
                is null)
            {
                continue;   // this segment will not take a hall, and that is a real answer
            }
            double d2 = ((lo + (w / 2.0)) - shaftX) * ((lo + (w / 2.0)) - shaftX);
            if (w > bestW + 0.5 || (w > bestW - 0.5 && d2 < bestD2))
            {
                (bestLo, bestW, bestD2) = (lo, Math.Max(w, bestW), d2);
            }
        }

        if (!double.IsNaN(bestLo))
        {
            var stand = new List<Rib> { new(bestLo - CorridorHalf, true) };
            hallSite = CarveHall(
                walls, glass, hallSpineCuts, bodyId, level, stand, 0, +1, mouth, far,
                shaftX, serviceX, left, bestLo + bestW + HallEdgePadDu, roomScale, glazed: true);
        }

        // #775 · THE BLOCK IS CARVED WHENEVER THERE IS A BLOCK — and, on the venue's floor, only if
        // its hall actually stood up. A ring round a middle with no hall in it is a landscape floor
        // (this issue); a ring round a middle whose HALL WAS REFUSED THE GROUND is a canteen floor with
        // no canteen on it, and that floor keeps the ordinary grid it has always had.
        if (!hasHall || hallSite is not null)
        {
            // #775 · WHICH FACE OF THE SPINE THE NEAR BAND'S FRONT DOORS ARE CUT IN — the carve's own
            // mouth, and never a second opinion about it. It is the whole near band's face, hall or no
            // hall: on a landscape floor every door in it belongs to an office.
            hallSpineFaceY = mouth;
            if (hallSite is { } built)
            {
                claimed.Add((
                    built.Hall.X0 - 1.5, built.Hall.Y0 - 1.5,
                    built.Hall.X1 + 1.5, built.Hall.Y1 + 1.5));
            }

            // ── #813 · THE RING, AND THEN WHAT IS IN THE MIDDLE OF IT ──────────────────────────────
            //
            // In this order because the ring owns the middle's whole boundary: every wall it has is a
            // room's glass or a gate's stub, so the middle is what is LEFT when the block has been
            // built rather than a box with openings cut in it afterwards. That was #813's law about a
            // garden and it is the same law about a core of meeting rooms.
            ring = CarveRing(
                walls, glass, doorways, labels, claimed, ringSpineCuts, spineMouths, parkGates, locked,
                bodyId, level, block, hallSite is { } withHall ? withHall.Hall : null);

            if (hallSite is { } theVenue)
            {
                park = CarvePark(
                    walls, bodyId, level, theVenue.Hall, theVenue.Glass!.Value, block, parkGates, ring);
            }
            else
            {
                // #775 · …and on a landscape floor, the meeting rooms. Owner: "lots of meeting rooms —
                // glass-box rooms off the open floor." See UndergroundComplex.Landscape.cs.
                meetings = CarveMeetingCore(
                    walls, glass, doorways, labels, claimed, bodyId, level, block);
            }
        }
    }

    /// <summary>#751 · EVERY OTHER FLOOR, UNCHANGED — the staff mess two hundred metres down stands on a
    /// rib's room column exactly as it has since #751, and there is no park behind it to glaze. Carved
    /// before the rib loop and claimed immediately, so everything after it steps around the box.</summary>
    private static void SiteTheHallOnARib(
        string bodyId, int level, List<Rib> ribList, in SurfaceLayout.Field field,
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<(double Lo, double Hi)> hallSpineCuts,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        double shaftX, double? serviceX, double shaftY, double left, double right, double roomScale,
        ref (int Rib, int Side)? hallSlot, ref HallSite? hallSite, ref double hallSpineFaceY)
    {
        // ── EVERY OTHER FLOOR, UNCHANGED · the staff mess two hundred metres down stands on a rib's
        //    room column exactly as it has since #751, and there is no park behind it to glaze.
        hallSlot = HallSlotFor(
            bodyId, level, ribList, field, shaftX, serviceX, shaftY, left, right, roomScale);
        if (hallSlot is { } slot)
        {
            (double hmouth, double hfar) = RibReach(field, shaftY, ribList[slot.Rib].Down, hall: true);
            hallSite = CarveHall(
                walls, glass, hallSpineCuts, bodyId, level, ribList, slot.Rib, slot.Side, hmouth, hfar,
                shaftX, serviceX, left, right, roomScale, glazed: false);
            if (hallSite is { } built)
            {
                hallSpineFaceY = hmouth;
                claimed.Add((
                    built.Hall.X0 - 1.5, built.Hall.Y0 - 1.5,
                    built.Hall.X1 + 1.5, built.Hall.Y1 + 1.5));
            }
            else
            {
                hallSlot = null;   // the ground would not take one. It keeps its ordinary canteen.
            }
        }
    }

    /// <summary>#775/#813 · THE FRONT DOORS, PUBLISHED FROM THE VERY LIST THE WALL WAS CUT FROM — #585's
    /// one-gap law with no room left for a second opinion: the spine's segments stop at these spans and the
    /// leaves are drawn across them, off one list, in one method. The freight shutter is hung here too,
    /// because a door that will not open is still a door.</summary>
    private static void HangTheFrontDoors(
        string bodyId, int level,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<LockedDoor> locked,
        List<(double Lo, double Hi)> hallSpineCuts,
        List<(double Lo, double Hi, double PlateX, string Plate)> ringSpineCuts,
        double hallSpineFaceY, double shaftX, double shaftY, HallSite? hallSite)
    {
        // #775 · THE FRONT DOORS, PUBLISHED FROM THE VERY LIST THE WALL WAS CUT FROM. #585's one-gap law
        // with no room left for a second opinion: the segments above stop at these spans and the leaves
        // below are drawn across them, off one list, in one method.
        //
        // …and nothing is drawn past the seam, exactly as AddRoomsAlong has it: a gallery has no door in it,
        // only a way through, so the gap is cut and no imported leaf is hung in it.
        if (!IsFound(bodyId, level))
        {
            // …and the plate goes on the CORRIDOR side of each of them, which is the whole point of the
            // feature: a walker on the spine is told what the wall beside them is before they have to
            // wonder. The first cut is the entrance (it was placed at the lift), the rest are the doors a
            // code put there and they say so.
            //
            // BESIDE the door and never over it, on the side the walker is coming from. A plate centred on
            // its own doorway is a plate with the captain standing on top of it the moment they arrive —
            // watched happen in the browser on the first boot of ?frontdoor=1, the dot sitting squarely on
            // the word CANTEEN — and a sign you have to step off to read is not signage.
            double plateY = hallSpineFaceY > shaftY ? hallSpineFaceY - 2.0 : hallSpineFaceY + 2.0;
            double aside = DoorHalf + 3.0;
            for (int d = 0; d < hallSpineCuts.Count; d++)
            {
                (double lo, double hi) = hallSpineCuts[d];
                doorways.Add(new(lo, hallSpineFaceY, hi, hallSpineFaceY));

                double cx = (lo + hi) / 2.0;
                double plateX = Math.Clamp(
                    cx + (cx > shaftX ? -aside : aside),
                    hallSite is { } signed ? signed.Hall.X0 : cx,
                    hallSite is { } bounded ? bounded.Hall.X1 : cx);
                labels.Add(new(plateX, plateY,
                    d == 0
                        ? HallEntrancePlate(bodyId, HallUseOn(bodyId, level))
                        : HallEgressPlate(d + 1)));
            }

            // #813 · …and the near band's other doors, out of the very same list the wall was swept from.
            // Their plates are the building's own vocabulary rather than the venue's, and they go on the
            // corridor side for the reason the hall's do: a walker on the spine is told what the wall beside
            // them is before they have to wonder.
            //
            // #817 · …and a room with three of them is stencilled ONCE. The carve hands the plate over with
            // the FIRST of a room's cuts and hands the rest over blank, so a landscape office that earned
            // more leaves does not also earn three copies of its own name across one wall.
            foreach ((double lo, double hi, double plateX, string plate) in ringSpineCuts)
            {
                doorways.Add(new(lo, hallSpineFaceY, hi, hallSpineFaceY));
                if (plate.Length > 0)
                {
                    labels.Add(new(plateX, plateY, plate));
                }
            }
        }

        // #775 · AND THE FREIGHT DOOR, HUNG. The shutter is a locked door like every other door down here
        // that will not open — drawn shut, walled behind, and its plate is what [E] reads — so the refusal
        // is TOLD in the grammar the building already speaks, and no new client idiom was invented for it.
        if (hallSite is { } withFreight && withFreight.Hall.Freight is { } hoist)
        {
            locked.Add(new(
                hoist.Shutter.X1, hoist.Shutter.Y1, hoist.Shutter.X2, hoist.Shutter.Y2, hoist.Plate));
            labels.Add(new(hoist.PlateX, hoist.PlateY, FreightSign));
        }
    }
}
