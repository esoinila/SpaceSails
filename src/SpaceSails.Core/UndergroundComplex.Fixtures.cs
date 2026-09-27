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
}
