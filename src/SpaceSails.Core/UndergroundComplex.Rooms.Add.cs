using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · ADDING THE ROOMS ALONG A RIB — <c>AddRoomsAlong</c>, the one long method that lays them and
/// keeps the taken spans on both sides.
///
/// <para>Split out of <c>UndergroundComplex.Rooms.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field. It is one 322-line method and travels
/// whole.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>
    /// #822 · THE FIRE RECESS — how a chamber comes to have a second way out.
    ///
    /// <para>Owner's standing law, mid-build: <i>"no space except a small bedroom can only have one door …
    /// it's a fire hazard otherwise."</i> Every chamber down here was a box with a single leaf onto a
    /// dead-end cross corridor, which is the most literal trap the plan can draw, and there are ~1,500 of
    /// them.</para>
    ///
    /// <para><b>The idiom is the room module's own leftover.</b> The slots along a rib are pitched a few du
    /// further apart than a chamber is deep — <see cref="RoomCentresAlong"/> has always left that gap and
    /// nothing has ever stood in it. Sealed at the back, it becomes a recess: it opens straight onto the rib
    /// at its own width, and the chamber beside it takes a fire door through its own end wall into it. So
    /// the second exit is derived from the walls the room already had and not one coordinate is typed —
    /// and a captain leaves by a hole in a different wall from the one somebody is standing in.</para>
    ///
    /// <para><b>And where two open chambers share one recess it is a connecting door</b>, which is the
    /// reading that makes sense for a pair of offices off the same corridor: the recess takes a leaf from
    /// each room it touches, so two neighbours become a suite you can pass through. A LOCKED chamber takes
    /// none — it is not a space a captain can stand in, the fire code has nothing to say about it, and
    /// cutting it a fire door would throw away half of the owner's illusion of scale.</para>
    ///
    /// <para>The recess itself is circulation and not a room: it is an alcove off a corridor exactly as the
    /// lift's own alcove is, so it is not in <see cref="FloorPlan.Rooms"/> and the sweep does not ask it for
    /// two doors — though it has them.</para>
    /// </summary>
    /// <returns>The mouths it wants cut in each face of the rib, handed back rather than cut here, because
    /// the wall that carries them belongs to <see cref="RibFace"/> — #585's law, said about one more gap.
    /// </returns>
    private static (List<(double Lo, double Hi)> Minus, List<(double Lo, double Hi)> Plus) AddRoomsAlong(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways, List<LockedDoor> locked,
        List<Room> rooms, List<EnSuite> ensuites,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        string bodyId, int level, int rib, double x, double mouth, double far, bool down, double roomScale,
        int hallSide = 0)
    {
        double roomW = RoomWidthDu * roomScale, roomH = RoomHeightDu * roomScale;

        // #677 · Down here the rooms are the only thing that has to be different, and everything else about
        // them falls out of that: a gallery is not a room with a plate on it, so it has no plate, no lock and
        // no sign. A door that says CONSENT FILES on a floor nobody built would be the loudest lie in the
        // game — it would name a purpose, and a purpose implies somebody who had one.
        bool found = IsFound(bodyId, level);
        List<double> centres = RoomCentresAlong(mouth, far, down, roomScale);

        // #822 · WHAT WAS ALREADY STANDING WHEN THIS RIB STARTED. The recesses are carved after every room
        // on this rib is up, and they need to know the ground they are stepping into was free BEFORE this
        // rib laid anything — the ledger by then also holds this rib's own rooms and their cells, and a
        // recess is expected to sit against those. The ledger is still law (#585); this is the same law read
        // at the moment the recess is actually about to occupy new ground.
        int beforeThisRib = claimed.Count;

        // #822 · Every chamber this rib built, so the recess pass can see the column rather than one room at
        // a time: whether the slot beside it was built at all, and whether it is a room anybody can stand
        // in. Index 0 is the rib's minus side, 1 its plus side.
        var column = new (bool Built, bool Shut, double X1, double Y1, double X2, double Y2,
            double FaceX, double BackX, double Cx, double Cy, List<SurfaceLayout.Doorway> Ways)
            [2, centres.Count];

        LayTheColumn(walls, doorways, locked, rooms, ensuites, claimed, bodyId, level, rib, x, hallSide, roomW, roomH, found, centres, column);

        return CarveTheRecesses(walls, claimed, mouth, down, roomH, centres, column, beforeThisRib);
    }

    /// <summary>#251 · The first phase of <see cref="AddRoomsAlong"/>, extracted with every statement verbatim
    /// and in order: every chamber this rib builds, slot by slot and side by side, written into the column the
    /// recess pass reads.</summary>
    private static void LayTheColumn(List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways, List<LockedDoor> locked, List<Room> rooms, List<EnSuite> ensuites, List<(double X0, double Y0, double X1, double Y1)> claimed, string bodyId, int level, int rib, double x, int hallSide, double roomW, double roomH, bool found, List<double> centres, (bool Built, bool Shut, double X1, double Y1, double X2, double Y2, double FaceX, double BackX, double Cx, double Cy, List<SurfaceLayout.Doorway> Ways)[,] column)
    {
        for (int i = 0; i < centres.Count; i++)
        {
            double cy = centres[i];

            for (int side = -1; side <= 1; side += 2)
            {
                // #751 · The hall is standing on this column. Nothing is built here — no chamber walls, no
                // plate, no lock — and the rib's face above keeps its doorway at this very slot, which is
                // how the hall comes to have a door without ever cutting one.
                if (side == hallSide)
                {
                    // …but the doorway is PUBLISHED, in the same list as every other door down here, so the
                    // hall's entrances are drawn as the imported leaves they are and an audit can find them
                    // without knowing anything about halls.
                    double hallFaceX = x + (side * CorridorHalf);
                    if (!found)
                    {
                        doorways.Add(new SurfaceLayout.Doorway(
                            hallFaceX, cy - DoorHalf, hallFaceX, cy + DoorHalf));
                    }
                    continue;
                }

                string tag = $"hive:{level}:{rib}:{i}:{side}";
                double cx = x + (side * (CorridorHalf + (roomW / 2)));

                double x1 = cx - (roomW / 2), x2 = cx + (roomW / 2);
                double y1 = cy - (roomH / 2), y2 = cy + (roomH / 2);

                // #585: if this room would sit on something already standing, it is not built at all. An
                // empty patch of corridor is a facility with a gap in it; a room you can see and cannot enter
                // is a lie, and the audit reports it as one.
                bool clash = false;
                foreach ((double ax0, double ay0, double ax1, double ay1) in claimed)
                {
                    clash |= x1 < ax1 && x2 > ax0 && y1 < ay1 && y2 > ay0;
                }
                if (clash)
                {
                    continue;
                }
                string plate = found ? "" : SignFor(bodyId, level, tag);
                bool shut = !found && Frac(bodyId, tag + ":locked") < 0.5;

                // #822 · The two end walls are laid in the SECOND pass below, because whether either of them
                // carries a fire door is not a question about this room alone — it is a question about the
                // recess it shares with the slot beside it, and that slot has not been built yet. Everything
                // else about the room is decided here, in the order it always was, so the plates, the locks,
                // the cells and the pool's own numbering are untouched.

                // #707 · …and the back wall, which is the one that says whether anybody important sat here.
                //
                // ASKED BEFORE THIS ROOM CLAIMS ITS OWN GROUND, which is the whole of the ordering: the
                // claim boxes are inflated by 1.5 du on every side, so a cell hung on this room's own back
                // wall sits inside its PARENT'S keep-out and every single en-suite in the game refused
                // itself. (Watched happen: 202 floors, "1 principal room(s) and 0 en-suite(s)", with the
                // geometry perfectly correct.) The cell is checked against everything already standing and
                // the room is claimed immediately after, so nothing later can be laid on either of them.
                double backX = side < 0 ? x1 : x2;
                bool cell = AddEnSuite(
                    walls, ensuites, claimed, bodyId, level, plate, backX, cy, side, open: !shut);
                claimed.Add((x1 - 1.5, y1 - 1.5, x2 + 1.5, y2 + 1.5));

                // #801 · A ROOM THAT COULD NOT BE PLUMBED WAS NEVER A PRINCIPAL ROOM.
                //
                // #707 s law is that rank is readable in PLUMBING: a cell on a store room says nothing, and
                // a principal plate with no cell says the opposite of the thing it is there to say. The cell
                // is refused when the ground behind the room is already spoken for (the ledger is law,
                // #585), and until now that left the plate saying a rank the building could not back up.
                // It never fired on the shipped field — which is exactly why it was worth fixing the moment
                // a new passage moved by four du and it fired on four generated moons.
                //
                // The re-plate walks the SAME seeded list from the SAME seed, one step on, so it is the
                // floor s own vocabulary and not a special sign invented for the case.
                if (!cell && !found && IsPlumbed(bodyId, level) && IsPrincipalRoom(plate))
                {
                    plate = NotPrincipal(bodyId, level, tag);
                }

                // ── #818 · THE ONE ROOM THE LAW LETS OFF, AND IT HAS TO SAY SO ──────────────────────────
                //
                // Owner, stating the exception in the same breath as the law: "let's not have any empty
                // storage space unless the space is actually an empty storage."
                //
                // So a bare floor stops being a default and becomes a CLAIM — made on a store floor, by the
                // building, in its own stencil voice, on the one wall a captain reads before walking in.
                // Only on the floors that keep stock (ChamberFitting.StoresOn), only on rooms anybody can
                // walk into, and never over a principal plate: a room important enough to be plumbed is not
                // a room somebody emptied and forgot.
                if (!shut && !found && !IsPrincipalRoom(plate)
                    && ChamberFitting.StoresOn(ChamberFitting.DepartmentOn(bodyId, level))
                    && Frac(bodyId, tag + ":emptied") < ChamberFitting.EmptyStoreChance)
                {
                    plate = ChamberFitting.EmptyStorePlate;
                }

                if (!cell)
                {
                    walls.Add(new(backX, y1, backX, y2, true));
                }

                double faceX = side < 0 ? x2 : x1;
                walls.Add(new(faceX, y1, faceX, cy - DoorHalf, true));
                walls.Add(new(faceX, cy + DoorHalf, faceX, y2, true));

                var ways = new List<SurfaceLayout.Doorway>(2);
                if (shut)
                {
                    locked.Add(new(faceX, cy - DoorHalf, faceX, cy + DoorHalf, plate));
                }
                else
                {
                    // #677 · A GALLERY HAS NO DOOR IN IT, only a way through. Every doorway in this building
                    // is drawn as an IMPORTED leaf — the violet that means "this was flown here", which is
                    // the whole of #592's material language — so hanging one in a hall would say, in the one
                    // channel the game reserves for it, that somebody shipped it in and fitted it. The wall
                    // simply stops, and the gap is the gap the wall builder already left.
                    if (!found)
                    {
                        doorways.Add(new SurfaceLayout.Doorway(faceX, cy - DoorHalf, faceX, cy + DoorHalf));
                    }

                    // #822 · …and the gap is a WAY OUT whether or not a leaf was hung in it, which is the
                    // one place the fire code and the doorway list have to part company: the galleries
                    // publish no doorways at all and every one of their chambers is still a room a captain
                    // walks into and has to be able to walk out of. The list is the room's own and the
                    // recess pass below appends to it.
                    ways.Add(new SurfaceLayout.Doorway(faceX, cy - DoorHalf, faceX, cy + DoorHalf));
                    rooms.Add(new Room(x1, y1, x2, y2, plate, ways));
                }

                column[side < 0 ? 0 : 1, i] = (true, shut, x1, y1, x2, y2, faceX, backX, cx, cy, ways);
            }
        }
    }

    /// <summary>#251 · The second phase of <see cref="AddRoomsAlong"/>, extracted with every statement verbatim
    /// and in order: the recesses, and the end walls decided with them, one pass per face of the rib.</summary>
    private static (List<(double Lo, double Hi)> Minus, List<(double Lo, double Hi)> Plus) CarveTheRecesses(List<SurfaceLayout.Wall> walls, List<(double X0, double Y0, double X1, double Y1)> claimed, double mouth, bool down, double roomH, List<double> centres, (bool Built, bool Shut, double X1, double Y1, double X2, double Y2, double FaceX, double BackX, double Cx, double Cy, List<SurfaceLayout.Doorway> Ways)[,] column, int beforeThisRib)
    {
        // ── #822 · THE RECESSES, AND THE END WALLS THAT ARE DECIDED WITH THEM ────────────────────────────
        //
        // One pass per face of the rib, walking the column outward from the spine exactly as the slots were
        // laid. A room's recess is the gap on its MOUTH side — the one between it and the slot before it,
        // or between it and the rib's own mouth for the first room in the column. That side rather than the
        // blind end for one reason worth stating: it is the same gap for every room in the column, so the
        // rule is one sentence, and the recess a captain steps out of always puts them a pace nearer the
        // way home.
        var minusMouths = new List<(double Lo, double Hi)>();
        var plusMouths = new List<(double Lo, double Hi)>();
        double dir = down ? -1.0 : 1.0;

        for (int face = 0; face < 2; face++)
        {
            var cutNear = new bool[centres.Count];
            var cutFar = new bool[centres.Count];
            List<(double Lo, double Hi)> mouthsHere = face == 0 ? minusMouths : plusMouths;

            // #822 · AS FEW RECESSES AS THE LAW NEEDS, and they are taken from the BLIND END back toward the
            // spine. Every open chamber wants a second way; a recess between two of them gives it to BOTH,
            // so walking the column outward-in and skipping whoever is already served halves the number of
            // pockets cut into the rib — and the ones it drops are the pockets nearest the mouth, which is
            // the busiest ten du of corridor in the building. That is not tidiness: the escort walk went
            // from 56 s to 68 s against a 67.5 s canary the first time this cut one beside every room
            // (#833's bound, watched go red on luna B10 and B12 at 82% moving). Fewer holes in the wall a
            // guard walks down, and the same law.
            var wants = new bool[centres.Count];
            for (int i = 0; i < centres.Count; i++)
            {
                wants[i] = column[face, i].Built && !column[face, i].Shut;
            }

            for (int i = centres.Count - 1; i >= 0; i--)
            {
                (bool built, bool shut, double x1, _, double x2, _, _, double backX, _, double cy, _) =
                    column[face, i];
                if (!built || shut || !wants[i])
                {
                    continue;   // nothing to let out of, nobody in there, or already let out
                }

                double nearEdge = cy - (dir * roomH / 2.0);
                double limit = i > 0 ? centres[i - 1] + (dir * roomH / 2.0) : mouth;
                double available = Math.Abs(nearEdge - limit);
                double depth = Math.Min(available, 2 * DoorHalf);
                if (depth < FireRecessMinDu)
                {
                    continue;   // the module left no gap here. The sweep says so out loud rather than here.
                }

                double endY = nearEdge - (dir * depth);
                double lo = Math.Min(nearEdge, endY), hi = Math.Max(nearEdge, endY);

                // #585 · The ledger is law for a recess exactly as it is for a room. Read against what was
                // standing before this rib began — see the snapshot above.
                bool taken = false;
                for (int k = 0; k < beforeThisRib; k++)
                {
                    (double ax0, double ay0, double ax1, double ay1) = claimed[k];
                    taken |= x1 < ax1 && x2 > ax0 && lo < ay1 && hi > ay0;
                }
                if (taken)
                {
                    continue;
                }

                // Sealed at the back, open to the rib. The mouth goes back to the wall builder.
                walls.Add(new(backX, lo, backX, hi, true));
                mouthsHere.Add((lo, hi));
                cutNear[i] = true;
                wants[i] = false;

                // Its outer end is the neighbour's own end wall where the gap runs the whole way to it —
                // then there is nothing to build and the recess is SHARED, so that room takes a connecting
                // leaf into it too, if it is a room anybody can stand in. Where the module left more ground
                // than a recess needs, the recess ends in a wall of its own and the neighbour keeps a solid
                // one: a fire door onto six du of rock would be the drawn world lying again.
                bool sharesWithNeighbour = i > 0 && column[face, i - 1].Built
                    && Math.Abs(depth - available) < 1e-6;
                if (!sharesWithNeighbour)
                {
                    walls.Add(new(x1, endY, x2, endY, true));
                }
                else if (!column[face, i - 1].Shut)
                {
                    cutFar[i - 1] = true;
                    wants[i - 1] = false;   // served by the same recess — a shared one is a connecting door
                }

            }

            // …and NOW the end walls, each with the gap the pass above decided it carries.
            for (int i = 0; i < centres.Count; i++)
            {
                (bool built, _, double x1, _, double x2, _, _, _, double cx, double cy,
                    List<SurfaceLayout.Doorway> ways) = column[face, i];
                if (!built)
                {
                    continue;
                }

                double nearY = cy - (dir * roomH / 2.0), farY = cy + (dir * roomH / 2.0);
                EndWall(nearY, cutNear[i]);
                EndWall(farY, cutFar[i]);

                void EndWall(double y, bool cut)
                {
                    if (!cut)
                    {
                        walls.Add(new(x1, y, x2, y, true));
                        return;
                    }
                    // #822 · A FIRE RECESS IS A GAP AND NOT A LEAF, so nothing is added to the floor's
                    // doorway list — the wall simply stops, exactly as it stops at a rib's mouth, at the
                    // cabinets' party walls and everywhere in the galleries. Every doorway in this building
                    // is drawn as an IMPORTED leaf (#592's material language: somebody flew this here), and
                    // an egress opening is the one thing in a facility nobody fits a door to. It is a way
                    // out all the same, which is why <see cref="Room.Ways"/> and the doorway list are two
                    // different lists and this is the room's own.
                    walls.Add(new(x1, y, cx - DoorHalf, y, true));
                    walls.Add(new(cx + DoorHalf, y, x2, y, true));
                    ways.Add(new SurfaceLayout.Doorway(cx - DoorHalf, y, cx + DoorHalf, y));
                }

            }
        }

        return (minusMouths, plusMouths);
    }
}
