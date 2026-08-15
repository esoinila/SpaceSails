using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>#592/#614/#411 · Is this room index DESIGNATED — reserved for a find that must exist? The
    /// same reservation <see cref="CarveRefuges"/> makes and for the same reason: a designated INDEX read off
    /// a list that a second placer shortens is a feature silently dead on some worlds forever, with every
    /// test still green. Lifted out of <see cref="CarveAmenities"/> when #751 gave it a second caller.</summary>
    private static bool ReservedRoom(string bodyId, int level, int index)
    {
        foreach ((int Level, int RoomIndex)? designated in
            new (int, int)?[]
            {
                KeyRoomFor(bodyId), RelicRoomFor(bodyId), StandingOrderRoomFor(bodyId),
                FoundKeyRoomFor(bodyId),   // #677 · the way down to the halls is a designation too
            })
        {
            if (designated is { } d && d.Level == level && d.RoomIndex == index)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Sort room indices by how far they are from a point, ties broken by index — <c>List.Sort</c>
    /// is not stable, and a floor being the same floor every visit is law down here.</summary>
    private static void Nearest(
        List<int> which, List<Room> rooms, double px, double py)
    {
        which.Sort((a, b) =>
        {
            double da = Dist2(rooms[a], px, py), db = Dist2(rooms[b], px, py);
            int by = da.CompareTo(db);
            return by != 0 ? by : a.CompareTo(b);
        });

        static double Dist2(Room room, double px, double py)
        {
            double dx = room.X - px, dy = room.Y - py;
            return (dx * dx) + (dy * dy);
        }
    }

    /// <summary>#707 · WHAT IS BOLTED DOWN IN ONE OF THESE ROOMS — a counter, a run of cubicles, a bank of
    /// machines — returning the round tops that go on the floor with it.
    ///
    /// <para>The fixtures are WALLS, in the same list as everything else, so they collide: a bar you can
    /// walk through is a bar drawn ON a floor rather than one IN a room, and this ground has paid for the
    /// sim doing one thing while the picture said another three times in one afternoon. Every fixture is
    /// laid against the room's own back half, so the doorway, the middle of the room and the console in it
    /// are all left clear — a fixture that seals a room is #585's stranded room with better furniture.</para>
    ///
    /// <para>The tables are NOT walls. Round tops are drawn and never collided with anywhere in this game
    /// (the ship's cantina, a haven bar), and a captain barking their shins on a table on a floor with a
    /// tank running would be a cruelty nobody asked for.</para></summary>
    private static IReadOnlyList<(double X, double Y)> Fitting(
        List<SurfaceLayout.Wall> walls, Comfort use, double cx, double cy)
    {
        switch (use)
        {
            case Comfort.UpperCanteen:
                // The counter, and the service side behind it — the one part of any bar the customer never
                // stands in, closed off exactly the way it would be.
                walls.Add(new(cx - 5.0, cy + 3.6, cx + 5.0, cy + 3.6, true));
                walls.Add(new(cx - 5.0, cy + 3.6, cx - 5.0, cy + 6.0, true));
                walls.Add(new(cx + 5.0, cy + 3.6, cx + 5.0, cy + 6.0, true));
                return [(cx - 4.5, cy - 2.5), (cx, cy - 4.2), (cx + 4.5, cy - 2.5)];

            case Comfort.StaffCanteen:
                // Four machines against the back wall and nothing to lean on. The owner's whole point about
                // this room is what is NOT in it.
                foreach (double m in new[] { cx - 5.4, cx - 1.8, cx + 1.8, cx + 5.4 })
                {
                    walls.Add(new(m - 1.4, cy + 4.4, m + 1.4, cy + 4.4, true));
                    walls.Add(new(m - 1.4, cy + 4.4, m - 1.4, cy + 6.0, true));
                    walls.Add(new(m + 1.4, cy + 4.4, m + 1.4, cy + 6.0, true));
                }
                // Tables close together and facing each other, which is the other half of that design.
                return [(cx - 3.6, cy - 2.4), (cx, cy - 2.4), (cx + 3.6, cy - 2.4)];

            default:
                // Bathroom-grade, per the owner: a basin run along the back and three cubicle dividers. The
                // stalls have no fronts on the plan — a deck plan draws partitions, and a captain made to
                // path around three cubicle doors to reach a mirror is being charged for a joke.
                walls.Add(new(cx - 5.5, cy + 4.2, cx + 5.5, cy + 4.2, true));
                foreach (double d in new[] { cx - 4.0, cx, cx + 4.0 })
                {
                    walls.Add(new(d, cy - 6.0, d, cy - 2.6, true));
                }
                return [];
        }
    }

    /// <summary>Rooms down both sides of a rib. About half are locked — the owner's illusion of scale — and a
    /// locked one still gets its sign, because a door that says what is behind it and will not open is doing
    /// far more work than a blank one.</summary>
    /// <summary>#585 · Where the rooms sit along a rib. ONE function, called by the wall builder and by the
    /// room builder, because the doorway a room cuts and the gap its corridor leaves must be the same gap.
    /// They were computed twice and agreed about nothing.</summary>
    private static List<double> RoomCentresAlong(double mouth, double far, bool down, double roomScale)
    {
        double roomH = RoomHeightDu * roomScale;
        double span = Math.Abs(far - mouth);
        int count = Math.Max(1, (int)(span / (roomH + 3)) - 1);

        var ys = new List<double>(count);
        for (int i = 0; i < count; i++)
        {
            double along = (i + 1) * (span / (count + 1));
            ys.Add(down ? mouth - along : mouth + along);
        }
        return ys;
    }

    /// <summary>One side of a rib corridor, built as segments with a gap at every room door.
    ///
    /// <para>#822 · …and at every fire recess. Those spans are not worked out here: they are handed over by
    /// <see cref="AddRoomsAlong"/>, which is the placer that knows which slots were built and which of them
    /// hold a room anybody can stand in. #585's law is that the gap a room cuts and the gap its corridor
    /// leaves are ONE gap, and a second opinion here about where a recess fell would be that bug wearing a
    /// new coat.</para></summary>
    private static void RibFace(
        List<SurfaceLayout.Wall> walls, double x, double mouth, double far,
        string bodyId, int level, int rib, int side, bool down, double roomScale,
        IReadOnlyList<(double Lo, double Hi)>? recesses = null)
    {
        var doors = RoomCentresAlong(mouth, far, down, roomScale);
        double lo = Math.Min(mouth, far), hi = Math.Max(mouth, far);

        var cuts = new List<(double Lo, double Hi)>();
        foreach (double cy in doors)
        {
            cuts.Add((cy - DoorHalf, cy + DoorHalf));
        }
        if (recesses is not null)
        {
            cuts.AddRange(recesses);
        }
        cuts.Sort((a, b) => a.Lo.CompareTo(b.Lo));

        double cursor = lo;
        foreach ((double clo, double chi) in cuts)
        {
            if (chi <= lo || clo >= hi)
            {
                continue;
            }
            walls.Add(new(x, cursor, x, Math.Max(cursor, clo), true));
            cursor = Math.Min(hi, chi);
        }
        walls.Add(new(x, cursor, x, hi, true));
    }

    /// <summary>Half a doorway. Comfortably wider than the captain, and the ONE number both the room's own
    /// face and its corridor's wall are cut to.
    ///
    /// <para>#585: widened from 2.0. A 4 du gap is four captain-diameters and looked ample on paper, but the
    /// reachability flood walks a GRID — a gap narrower than a couple of grid steps can fail to be sampled at
    /// all, so a door that is open in the geometry is shut to anything that pathfinds. A facility corridor
    /// would have wide doors anyway; this is one of the happy cases where the honest fiction and the robust
    /// number are the same number.</para></summary>
    public const double DoorHalf = 3.2;

    /// <summary>#822 · The narrowest a fire recess may be and still be a way out. Half a doorway — the
    /// narrowest gap this building has ever asked a body to pass, and comfortably more than twice the
    /// captain's own width at the step the reachability lattice samples on.
    ///
    /// <para>It is a floor, not a target: the gap the room module leaves between two slots is wider than
    /// this everywhere the generator has ever laid one, and this exists so that a floor whose module leaves
    /// no useful gap gets NO recess rather than a slot too narrow to walk out of. The sweep then reports
    /// that chamber as the violation it is, which is the honest failure — a recess a body cannot enter
    /// would be a second exit only on the plan.</para></summary>
    public const double FireRecessMinDu = DoorHalf;

    /// <summary>#585/#677 · THE ROOM MODULE — how wide and how deep one room off a rib is, at the scale a
    /// facility builds at.
    ///
    /// <para>These were two <c>const</c>s inside <see cref="AddRoomsAlong"/> and a third inside
    /// <see cref="RoomCentresAlong"/>, which was exactly as safe as it sounds: the door a room cuts and the
    /// gap its corridor leaves are the SAME gap (#585's lesson), and the moment one floor in the game wanted
    /// bigger chambers there would have been two places to grow and one of them would have been missed. One
    /// module, published, and everything that scales it scales it once.</para></summary>
    public const double RoomWidthDu = 15.0;

    /// <summary>Room depth along its rib. See <see cref="RoomWidthDu"/>.</summary>
    public const double RoomHeightDu = 12.0;

    /// <summary>#677 · HOW MUCH BIGGER A GALLERY GETS PER FLOOR DOWN, and it is the one number the halls'
    /// geometry is allowed to state.
    ///
    /// <para>The whole game has taught the opposite: deeper is tighter, because a facility's cost per cubic
    /// metre goes up with every metre of overburden and the people paying for it knew that. Down here it
    /// inverts, and the renderer says so without one word of prose — <b>room scale increasing with depth</b>,
    /// which on a top-down plan is the only sentence a plan can speak. The four floors run 1.00, 1.10, 1.21,
    /// 1.33 of the module above, so the deepest gallery has getting on for twice the floor area of the first
    /// and about half as many chambers on it.</para>
    ///
    /// <para><b>Derived, never typed, and capped by the ground it is standing on.</b> Nothing here writes a
    /// room's dimensions: they are <see cref="RoomWidthDu"/>/<see cref="RoomHeightDu"/> — the facility's own
    /// module, the same one every floor above uses — taken to the power of how far into the band you are.
    /// And <see cref="Build"/> clamps the ratio against the actual rib spacing of the actual field, so the
    /// growth stops where two facing chambers would meet rather than at a number somebody guessed.</para></summary>
    public const double FoundGrowthPerFloor = 1.10;

    /// <summary>#677 · How much bigger than the module this floor's chambers are. 1.0 everywhere the building
    /// built itself; compounding with depth in the halls.</summary>
    public static double RoomScaleOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!IsFound(bodyId, level))
        {
            return 1.0;
        }
        return Math.Pow(FoundGrowthPerFloor, BandTop(FoundBandOf(bodyId)) - level);
    }

    /// <summary>#801 · The nearest plate in this floor s own register that is NOT a room somebody sat in.
    ///
    /// <para>Walked from the same seed <see cref="SignFor(string, int, string)"/> used, one step on, so a
    /// room that had to give up its rank keeps the building s vocabulary rather than acquiring a sign
    /// invented for the occasion. Empty only if a kind ever ships a register with nothing but principal
    /// plates in it, which is a thing a guard would notice long before a player did.</para></summary>
    private static string NotPrincipal(string bodyId, int level, string tag)
    {
        string[] signs = SignsFor(KindOn(bodyId, level));
        ulong seed = DiceRule.Seed($"hive-sign:{bodyId}:{tag}");
        for (int i = 1; i <= signs.Length; i++)
        {
            string candidate = signs[(int)((seed + (ulong)i) % (ulong)signs.Length)];
            if (!IsPrincipalRoom(candidate))
            {
                return candidate;
            }
        }
        return string.Empty;
    }

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
