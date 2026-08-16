using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>#707 · Hang a washroom cell off the back of a room, if the room is one that earned one and
    /// the ground behind it is free. Returns true when it built the cell AND the parent's back wall (with a
    /// doorway cut in it), so the caller knows not to build that wall itself.</summary>
    private static bool AddEnSuite(
        List<SurfaceLayout.Wall> walls, List<EnSuite> ensuites,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        string bodyId, int level, string plate, double backX, double cy, int side, bool open)
    {
        // The one pressure source, asked through the one plumbing question: a cell is for people out of their
        // suits AND for a building that had a wet stack to hang it off. The halls breathe and have neither
        // (#677) — a pan in a gallery would be the most explaining object in the game.
        if (!IsPlumbed(bodyId, level) || !IsPrincipalRoom(plate))
        {
            return false;
        }

        double outward = side < 0 ? -EnSuiteDepth : EnSuiteDepth;
        double farX = backX + outward;
        double cx0 = Math.Min(backX, farX), cx1 = Math.Max(backX, farX);
        double cy0 = cy - EnSuiteHalfHeight, cy1 = cy + EnSuiteHalfHeight;

        // #585 · Checked against the ledger BEFORE it is built, not only added to it afterwards. The room
        // columns either side of a rib are laid in x order and this cell reaches BACK toward a neighbour
        // that already exists, so a placer that only claims forward is a placer that can bury one.
        foreach ((double ax0, double ay0, double ax1, double ay1) in claimed)
        {
            if (cx0 < ax1 && cx1 > ax0 && cy0 < ay1 && cy1 > ay0)
            {
                return false;   // somebody is already standing on it. The room keeps its solid back wall.
            }
        }
        claimed.Add((cx0 - 1.5, cy0 - 1.5, cx1 + 1.5, cy1 + 1.5));

        // The parent's back wall, in two segments with the cell's doorway between them — the whole tell, in
        // one gap in one wall. The room is 12 du deep, so the segments run from its own corners.
        walls.Add(new(backX, cy - 6.0, backX, cy - DoorHalf, true));
        walls.Add(new(backX, cy + DoorHalf, backX, cy + 6.0, true));

        // …and the cell itself: two returns and an end wall.
        walls.Add(new(backX, cy0, farX, cy0, true));
        walls.Add(new(backX, cy1, farX, cy1, true));
        walls.Add(new(farX, cy0, farX, cy1, true));

        // The fixture. One pan against the end wall, which is all a private cell has room for and all it
        // needs to read as one on a plan.
        double basinX = backX + (outward * 0.76);
        walls.Add(new(basinX, cy + 1.0, basinX, cy + 3.2, true));

        ensuites.Add(new EnSuite(
            backX + (outward / 2.0), cy, plate, open,
            new SurfaceLayout.Doorway(backX, cy - DoorHalf, backX, cy + DoorHalf)));
        return true;
    }

    /// <summary>#707 · The amenity rooms, taken out of the rooms the floor had already built — the same
    /// discipline as <see cref="CarveRefuges"/>, and for the same three reasons: a room is already audited
    /// walkable from the lift, already has a door the captain can find, and already sits down a rib.
    ///
    /// <para><b>Nearest the car, which is the exact opposite of the refuge law and is right for the same
    /// reason.</b> A refuge earns its existence by being a detour (#608). A canteen earns its by being the
    /// first door off the lift: it is the room a haulier with a pallet and forty minutes actually used, and
    /// a bar you have to go looking for is not a bar anybody drank in on a shift. No dice — a building puts
    /// its catering by the car, every time, and a captain gets to learn that.</para>
    ///
    /// <para><b>And the washroom is beside the canteen</b>, for the reason a plumber would give: a building
    /// runs ONE wet stack and hangs everything that needs a drain off it. That is the same sentence as the
    /// en-suites only appearing on floors that breathe, which is why §13's amenity law is one rule and not
    /// three.</para></summary>
    private static List<Amenity> CarveAmenities(
        string bodyId, int level, List<Room> rooms,
        List<SurfaceLayout.Wall> walls, double shaftX, double shaftY, HallSite? hall)
    {
        var built = new List<Amenity>();
        bool top = TopPressurisedFloor(bodyId) == level;
        bool mess = StaffCanteenFloor(bodyId) == level;
        if (!top && !mess)
        {
            return built;
        }

        // #751 · THE HALL IS THE CANTEEN, where one was carved. It is not taken out of the room pool at all
        // — it is the ground the pool's own column stood on, claimed before any room was laid — so the only
        // thing left for this method to do on a hall floor is to give it its plate and (on the top floor)
        // find the washroom a wet stack away from it.
        Comfort hallUse = top ? Comfort.UpperCanteen : Comfort.StaffCanteen;
        if (hall is { } site)
        {
            (string hallPlate, string hallFixture) = AmenitySigns(bodyId, hallUse);
            built.Add(new Amenity(
                hallUse, site.X, site.Y, hallPlate, hallFixture, site.Tops, site.Hall));

            if (!top || rooms.Count == 0)
            {
                return built;
            }

            var near = new List<int>();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (!IsPrincipalRoom(rooms[i].Plate) && !ReservedRoom(bodyId, level, i))
                {
                    near.Add(i);
                }
            }
            if (near.Count == 0)
            {
                return built;
            }

            Nearest(near, rooms, site.X, site.Y);
            int washroom = near[0];
            Room wet = rooms[washroom];
            (double wx, double wy) = (wet.X, wet.Y);
            rooms.RemoveAt(washroom);
            (string wplate, string wfixture) = AmenitySigns(bodyId, Comfort.Washroom);
            built.Add(new Amenity(
                Comfort.Washroom, wx, wy, wplate, wfixture, Fitting(walls, Comfort.Washroom, wx, wy)));
            built.Sort((a, b) => a.Use.CompareTo(b.Use));
            return built;
        }

        if (rooms.Count == 0)
        {
            return built;
        }

        // #592/#614/#411 · The designated rooms, which may never be taken. The same reservation
        // CarveRefuges makes and for the same reason: a designated INDEX read off a list that a second
        // placer shortens is a feature silently dead on some worlds forever, with every test still green.
        // Candidates, nearest the car first. A principal room is never one: it already has its own
        // washroom, and a director's office is not where a building puts the vending machines.
        var pool = new List<int>();
        var anywhere = new List<int>();
        for (int i = 0; i < rooms.Count; i++)
        {
            if (ReservedRoom(bodyId, level, i))
            {
                continue;
            }
            anywhere.Add(i);
            if (!IsPrincipalRoom(rooms[i].Plate))
            {
                pool.Add(i);
            }
        }

        int need = top ? 2 : 1;
        List<int> from = pool.Count >= need ? pool : anywhere;
        if (from.Count < need)
        {
            return built;   // nothing left to give. The guards say this has never happened.
        }
        Nearest(from, rooms, shaftX, shaftY);

        // The canteen takes the nearest room to the car; the washroom takes the room nearest THE CANTEEN,
        // which is the wet stack rather than a second walk from the lift.
        int first = from[0];
        var taken = new List<(int Index, Comfort Use)>
        {
            (first, top ? Comfort.UpperCanteen : Comfort.StaffCanteen),
        };
        if (top)
        {
            from.RemoveAt(0);
            Nearest(from, rooms, rooms[first].X, rooms[first].Y);
            taken.Add((from[0], Comfort.Washroom));
        }

        // Highest index first, so removing one never renumbers another out from under us.
        taken.Sort((a, b) => b.Index.CompareTo(a.Index));
        foreach ((int index, Comfort use) in taken)
        {
            (double rx, double ry) = (rooms[index].X, rooms[index].Y);
            rooms.RemoveAt(index);
            (string plate, string fixtureName) = AmenitySigns(bodyId, use);
            built.Add(new Amenity(use, rx, ry, plate, fixtureName, Fitting(walls, use, rx, ry)));
        }

        // Back into the order the plates read in, so a floor's amenity list is canteen-then-washroom rather
        // than an artefact of the order they happened to be removed in.
        built.Sort((a, b) => a.Use.CompareTo(b.Use));
        return built;
    }

    /// <summary>
    /// #798 · THE BINS — somewhere to put a document you never want read again.
    ///
    /// <para>Owner, live in play: <i>"those trash cans are needed so we get rid of the processed materials
    /// without connecting them to us too clearly, like leaving them to the table."</i> The phase-two loop
    /// (sit, dig, book) ends by producing a liability, and until this method ran, the only places a captain
    /// could put one were their own pocket and the table they had just been sitting at.</para>
    ///
    /// <h3>Where they go, and why those three places</h3>
    /// <list type="bullet">
    /// <item><b>The canteen hall</b> — a <see cref="RipAndBin.Tier.SlopBin"/> at the far end of the counter,
    /// where trays go back, and a <see cref="RipAndBin.Tier.Chute"/> at the service end of the same room,
    /// beside the end of the band the goods hoist parks in. Both rungs of the ladder in the one room a
    /// captain spends an evening in, which is what makes bin CHOICE a choice.</item>
    /// <item><b>The park</b> — a slop-grade grounds bin beside the bench nearest the gate. Clippings,
    /// cartons and whatever the walk leaves behind: a bin with wet in it, in the open, in another room.</item>
    /// <item><b>Every floor that breathes</b> — a <see cref="RipAndBin.Tier.PaperBin"/> by the lift, which is
    /// the copier-corner bin of #775's amenity rule and the reason the offices band has one at all. It is
    /// also the WORST rung, and it is the one that is everywhere: the convenient answer and the wrong one,
    /// which is the whole shape of this feature's joke.</item>
    /// <item>#828 · <b>The premium suites</b> — a <see cref="RipAndBin.Tier.SecureDisposal"/> at the machine
    /// <see cref="RingOffice"/> stands at the end of a big suite's service strip. The BEST rung, and the one
    /// that is nowhere except behind the rank that pays for a kitchenette and two staff WCs: the ladder is
    /// priced like every other privacy instrument in this building.</item>
    /// </list>
    ///
    /// <h3>The one discipline</h3>
    /// <para><b>Nothing here is placed at a coordinate somebody liked.</b> Every anchor is derived from
    /// geometry the room already published, every candidate is then CHECKED against the floor's own finished
    /// walls and furniture, and a bin that cannot find clear floor is simply not fitted — the floor says so
    /// by having no bin rather than by having one inside a table. §13.15's rule, and the reason this method
    /// runs last: it is the only placer that needs to see the whole room.</para>
    ///
    /// <para>The solid box goes into <paramref name="walls"/>, which is what makes a bin drawn and collidable
    /// off ONE source — a second rectangle for the eye would be the drawn world and the simulated world
    /// disagreeing about a thing the captain walks into.</para>
    /// </summary>
    private static List<RipAndBin.Bin> CarveBins(
        string bodyId, int level,
        List<SurfaceLayout.Wall> walls,
        List<SurfaceLayout.Doorway> doorways,
        List<(double X, double Y)> roomCentres,
        List<Amenity> amenities,
        List<Refuge> refuges,
        List<Rib> ribs,
        Park? park,
        List<RingRoom> ring,
        double shaftX, double shaftY)
    {
        var bins = new List<RipAndBin.Bin>();

        // #775's tidy-spaces rule is what makes a bin worth having AND what makes it a bet: this building is
        // kept by professionals, and professionals empty bins in rooms that have air in them. A floor that
        // does not hold pressure gets none — not a rule about litter, but about who works down there.
        if (!HoldsPressure(bodyId, level))
        {
            return bins;
        }

        var segs = new List<SurfaceCollision.Segment>(walls.Count);
        foreach (SurfaceLayout.Wall w in walls)
        {
            segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
        }

        // Everything a bin may not be shoved against. Walls are handled by the collision field above; this
        // is the list of things that are NOT walls and still cannot be blocked — a doorway, a rib's mouth, a
        // console the [E] key answers, a chair somebody has to be able to get out of.
        var furniture = new List<(double X, double Y)>();
        foreach (SurfaceLayout.Doorway d in doorways)
        {
            furniture.Add(((d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0));
        }
        foreach (Rib rib in ribs)
        {
            furniture.Add((rib.X, shaftY - CorridorHalf));
            furniture.Add((rib.X, shaftY + CorridorHalf));
        }
        furniture.AddRange(roomCentres);
        foreach (Refuge r in refuges)
        {
            furniture.Add((r.X, r.Y));
        }
        foreach (Amenity a in amenities)
        {
            furniture.Add((a.X, a.Y));
            furniture.AddRange(a.Tables);
            if (a.Hall is { } hall)
            {
                furniture.AddRange(hall.StoolRow);
                foreach (Cabinet c in hall.Cabinets)
                {
                    furniture.Add(c.Table);
                }
                foreach (SurfaceLayout.Doorway d in hall.Openings)
                {
                    furniture.Add(((d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0));
                }
                if (hall.Freight is { } hoist)
                {
                    furniture.Add((hoist.PlateX, hoist.PlateY));
                }
            }
        }
        if (park is { } green)
        {
            furniture.AddRange(green.Walk);
            furniture.AddRange(green.Benches);
            furniture.AddRange(green.Masts);
        }

        // ── #828 · THE GOOD OFFICES · the secure disposal, read off the fixture the suite already stands ─
        //
        // Owner: "One thing the good offices would also have is a safe paper disposal trashes… a more secure
        // disposal than restaurant trash."
        //
        // FIRST, because it is the only rung on this ladder whose position is not this method's to choose:
        // RingOffice put the machine at the end of a premium suite's service strip, and a bin placed
        // anywhere else would be a second answer to where it stands. Nothing is stood up here — no box, no
        // clearance search — because the box is already in `walls` and was already checked by the furnishing
        // law (#818's no-fitting-in-a-doorway sweep). What is decided here is the one fact only this method
        // has: WHERE A CAPTAIN STANDS to use it, which is out of the machine's own face toward the room.
        foreach (RingRoom suite in ring)
        {
            foreach (RingOffice.Fixture kit in suite.Furniture)
            {
                if (kit.Kind != RingOffice.Fitting.SecureDisposal)
                {
                    continue;
                }

                double toX = (suite.X0 + suite.X1) / 2.0, toY = (suite.Y0 + suite.Y1) / 2.0;
                double dx = toX - kit.X, dy = toY - kit.Y;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 0.001)
                {
                    continue;   // a machine in the exact centre of its own suite has no "out of it"
                }

                // Out of the box along that bearing, then a standoff — the same StandOffDu every other bin
                // publishes, measured from the FACE rather than from the centre, because this box is six du
                // across and 2.2 du from its middle is still inside it.
                double ux = dx / len, uy = dy / len;
                double hx = (kit.X1 - kit.X0) / 2.0, hy = (kit.Y1 - kit.Y0) / 2.0;
                double outOfIt = double.MaxValue;
                if (Math.Abs(ux) > 1e-6)
                {
                    outOfIt = Math.Min(outOfIt, hx / Math.Abs(ux));
                }
                if (Math.Abs(uy) > 1e-6)
                {
                    outOfIt = Math.Min(outOfIt, hy / Math.Abs(uy));
                }

                double sx = kit.X + (ux * (outOfIt + RipAndBin.StandOffDu));
                double sy = kit.Y + (uy * (outOfIt + RipAndBin.StandOffDu));
                if (SurfaceCollision.Blocked(sx, sy, RipAndBin.StandClearDu, segs))
                {
                    continue;   // no floor to stand on: the suite says so by having no bin, per §13.15
                }

                // …and the BOX is published with it, because this rung is not a bucket. Everything that
                // measures a bin — the reach, the guards, anything that later asks what a captain is
                // standing at — reads the rectangle the suite actually stands rather than the 1.8 du
                // square the other three rungs are.
                bins.Add(new RipAndBin.Bin(
                    RipAndBin.Tier.SecureDisposal, kit.X, kit.Y, sx, sy, kit.Plate, hx, hy));
                furniture.Add((sx, sy));
            }
        }

        // ── THE HALL · the slop bin at the tray end of the counter, the chute at the service end ─────────
        foreach (Amenity a in amenities)
        {
            if (a.Hall is not { } h)
            {
                continue;
            }

            // THE HALL'S OWN TWO AXES, READ BACK OFF WHAT THE ROOM PUBLISHED. The carve laid this room in
            // (u, v) — u out from the rib's face, v in from the spine — and threw the frame away. It is
            // recoverable exactly, from two points the room hangs signs on: the PLATE is a quarter of the
            // way down the door wall at u = HallDoorAisleDu / 2, and the AMENITY's own spot is the middle
            // of the counter. Two signs, two signs' worth of arithmetic, and not one number typed here that
            // the room did not already say out loud.
            //
            // #827 · …and WHERE THE COUNTER'S LINE IS comes off the DESK now rather than being inferred
            // from the amenity's own standoff. It used to read `a.Y + HallEdgePadDu`, which is a second
            // opinion about the fixture's offset dressed up as arithmetic — and the moment #827 moved the
            // console onto the desk's face it would have walked both bins two du toward the bar. The desk
            // publishes its face; this reads it.
            int su = Math.Sign(a.X - h.PlateX);
            int sv = Math.Sign(a.Y - h.PlateY);
            if (su == 0 || sv == 0 || h.Desk is not { } bar)
            {
                continue;   // a hall that cannot say which way round it is gets no bins, and says so.
            }

            double faceX = su > 0 ? h.X0 : h.X1;
            double mouthY = sv > 0 ? h.Y0 : h.Y1;
            double U(double u) => faceX + (su * u);
            double V(double v) => mouthY + (sv * v);

            double counterV = (bar.FaceY0 - mouthY) * sv;
            double counterU1 = (bar.FaceX1 - faceX) * su;
            double midX = (h.X0 + h.X1) / 2.0, midY = (h.Y0 + h.Y1) / 2.0;

            // The chute first, because it is the better bet and a room with only one of the two should keep
            // the one worth walking to. Down the aisle inside the door wall — the service side of the room,
            // where the goods band ends — trying further in until the floor has room for it.
            var chuteAt = new List<(double X, double Y, double TowardX, double TowardY)>();
            var slopAt = new List<(double X, double Y, double TowardX, double TowardY)>();
            for (int step = 1; step <= 6; step++)
            {
                chuteAt.Add((U(HallDoorAisleDu - 0.6), V(counterV - (3.0 * step)), midX, midY));
                slopAt.Add((U(counterU1 - HallEdgePadDu), V(counterV - (3.0 * step)), midX, midY));
            }

            TakeTheBin(RipAndBin.Tier.Chute, chuteAt);
            TakeTheBin(RipAndBin.Tier.SlopBin, slopAt);
        }

        // ── THE PARK · a grounds bin beside the bench nearest the gate ───────────────────────────────────
        if (park is { Benches.Count: > 0 } grounds)
        {
            (double bx, double by) = grounds.Benches[0];
            foreach ((double x, double y) in grounds.Benches)
            {
                double was = ((bx - grounds.X) * (bx - grounds.X)) + ((by - grounds.Y) * (by - grounds.Y));
                double now = ((x - grounds.X) * (x - grounds.X)) + ((y - grounds.Y) * (y - grounds.Y));
                if (now < was)
                {
                    (bx, by) = (x, y);
                }
            }

            double pmx = (grounds.X0 + grounds.X1) / 2.0, pmy = (grounds.Y0 + grounds.Y1) / 2.0;
            var parkAt = new List<(double X, double Y, double TowardX, double TowardY)>();
            foreach (double off in new[] { 5.5, 7.0, 8.5, 10.0 })
            {
                parkAt.Add((bx + off, by, pmx, pmy));
                parkAt.Add((bx - off, by, pmx, pmy));
            }
            TakeTheBin(RipAndBin.Tier.SlopBin, parkAt);
        }

        // ── THE LIFT · the paper bin every floor that breathes has, and the worst rung of the ladder ─────
        var liftAt = new List<(double X, double Y, double TowardX, double TowardY)>();
        foreach (double along in new[] { 3.5, 7.0, 10.5, -3.5, -7.0, -10.5 })
        {
            double bx = shaftX + (along > 0 ? ShaftHalf + along : -ShaftHalf + along);
            liftAt.Add((bx, shaftY - CorridorHalf + 2.0, bx, shaftY));
            liftAt.Add((bx, shaftY + CorridorHalf - 2.0, bx, shaftY));
        }
        TakeTheBin(RipAndBin.Tier.PaperBin, liftAt);

        return bins;

        // ── THE FITTING ITSELF ───────────────────────────────────────────────────────────────────────────
        //
        // One local function, so every bin in the building is stood up by the same three tests and none of
        // the placers above can quietly relax one of them: clear of the walls, clear of the furniture, and
        // with somewhere to STAND that is clear of both and inside arm's reach. A candidate that fails any
        // of the three is skipped; a bin whose whole list fails is not fitted at all.
        void TakeTheBin(
            RipAndBin.Tier tier, IReadOnlyList<(double X, double Y, double TowardX, double TowardY)> anchors)
        {
            foreach ((double bx, double by, double tx, double ty) in anchors)
            {
                if (SurfaceCollision.Blocked(bx, by, RipAndBin.ClearDu, segs) || Crowded(bx, by))
                {
                    continue;
                }

                double dx = tx - bx, dy = ty - by;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 0.001)
                {
                    continue;
                }

                double sx = bx + (dx / len * RipAndBin.StandOffDu);
                double sy = by + (dy / len * RipAndBin.StandOffDu);
                if (SurfaceCollision.Blocked(sx, sy, RipAndBin.StandClearDu, segs))
                {
                    continue;
                }

                bins.Add(new RipAndBin.Bin(tier, bx, by, sx, sy, RipAndBin.PlateFor(tier)));

                // The box, in the ONE list that is both drawn and collided with. And into the collision
                // field this method is still reading, so the next bin cannot be stood inside this one.
                double h = RipAndBin.HalfDu;
                AddBox(bx, by, h);

                // …and the spot a captain stands on is furniture now, for the same reason a chair is: the
                // next bin may not be put where somebody has to stand to use this one.
                furniture.Add((sx, sy));
                return;
            }
        }

        // #874 · SOLID, not a box with a hole in it. A bin is 1.8 du on a side and the captain is 1.4 du
        // across, so four rails leave exactly ONE lattice square in the middle of it that a body fits on and
        // nothing on the floor can walk to. One square is not a room and it is the identical fault as the
        // park's beds and #586's monolith — a drawn solid the sim left hollow — so it is laid by the same
        // method, and the collision list this placer is still reading grows with it.
        void AddBox(double cx, double cy, double h)
        {
            int before = walls.Count;
            SurfaceLayout.AddSolidMass(walls, cx - h, cy - h, cx + h, cy + h, true);
            for (int i = before; i < walls.Count; i++)
            {
                SurfaceLayout.Wall w = walls[i];
                segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
            }
        }

        bool Crowded(double bx, double by)
        {
            foreach ((double fx, double fy) in furniture)
            {
                double dx = fx - bx, dy = fy - by;
                if ((dx * dx) + (dy * dy)
                    < RipAndBin.ClearOfFurnitureDu * RipAndBin.ClearOfFurnitureDu)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
