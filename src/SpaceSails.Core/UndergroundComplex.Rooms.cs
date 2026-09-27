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
                MaintenanceLedgerRoomFor(bodyId),   // #1063 · …and so is the room the ledger is kept in
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
}
