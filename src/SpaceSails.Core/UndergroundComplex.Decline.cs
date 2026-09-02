using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #1068 · CHANNEL ONE: SUBTRACTION, IN THE BUILDING ────────────────────────────────────────────────
    //
    // A door that opened yesterday does not open today. That is the whole of it, and everything below is
    // about making it happen without saying one word and without moving one other thing on the floor.
    //
    // IT IS A POST-PASS AND NOT A DECISION TAKEN DURING THE CARVE, deliberately. The obvious place for this
    // is beside `bool shut = ... Frac(bodyId, tag + ":locked") < 0.5` in AddRoomsAlong, and it is the wrong
    // place: a room that never opened is a room the amenity pass, the refuge pass and the room-centre pool
    // never saw, so shutting one THERE moves the canteen, moves the refuge, renumbers the searchable rooms
    // and re-seeds the furniture. The whole floor would come back different and the captain would have no
    // way to tell which of the forty changes was the one. The world declines POLITELY: it takes one leaf off
    // a building that is otherwise identical to the one he walked out of, down to the poster on the wall
    // beside it.
    //
    // WHAT THE PASS ACTUALLY DOES, in three lines:
    //   (1) the leaf leaves FloorPlan.Doorways, so the renderer stops drawing it as a way through;
    //   (2) an ordinary LockedDoor is appended in its place, wearing the plate the room already had — which
    //       is exactly what the client has drawn forty of on every floor since #585: a leaf drawn shut, a
    //       wall behind it, and a 🔒 sign console reading the plate. No new sign, no fault, no card, no line;
    //   (3) the room loses that way out of Room.Ways, because a way that is not a way is the named bug this
    //       ground has a table for — the drawn world saying one thing while the sim does another.
    // Nothing else on the plan is touched. The room keeps its box, its plate, its furniture, its centre and
    // its place in every index anybody has ever persisted.

    /// <summary>#1068 · <b>IS THIS THE FLOOR THE WORLD DECLINES ON?</b> The concourse — the block's own
    /// floor, round the park — and no other floor of any site in the game.
    ///
    /// <para><b>It is not chosen. It is the one floor of this building that has a door to spare.</b> Every
    /// other listed floor is ribs of two-way chambers: a corridor leaf and a fire recess, and a chamber that
    /// lost either of them would be a room with one way out — which breaks the owner's own standing law
    /// (#822: two ways out of everything but a booth) and would put the captain in a room somebody could
    /// shut. The concourse is the only floor whose rooms have three and four ways, because a suite round the
    /// green has street doors AND a gate onto the gravel, so one of them can go and every one of them is
    /// still a way home. <b>A captain is never shut out of anywhere; he is sent round the other way.</b>
    /// That is what "declines politely" means, said in geometry.</para>
    ///
    /// <para>It is also, mundanely, the right floor for it: the concourse is the one floor of a moon site a
    /// visitor walks across without being taken there, so a leaf that is shut this week is a leaf he had
    /// already walked through — which is the only way the subtraction can be noticed at all.</para></summary>
    public static bool DeclinesOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return PoliteDecline.On(bodyId) && HasParkBlock(bodyId, level);
    }

    /// <summary>#1068 · <b>WHICH LEAVES THE WORLD MAY TAKE, AND WHICH IT MAY NOT.</b> A candidate is a
    /// (room, way) pair, and it qualifies only if all of this holds — each clause a promise rather than a
    /// preference:
    ///
    /// <list type="number">
    /// <item><b>The room can spare it.</b> After the leaf goes the room still has
    /// <see cref="FireCodeMinExits"/> ways out. Not <see cref="Room.MeetsFireCode"/>, which would let a
    /// bedroom-small booth off with its LAST door and seal a captain into a WC — the law's own exemption is
    /// about how far you are from your one exit, and it has nothing to say about somebody taking it away. So
    /// this asks the harder question: the world only ever takes a THIRD door.</item>
    /// <item><b>It is a drawn leaf.</b> The way has to be in the floor's published doorways. A fire recess is
    /// a GAP and not a leaf (#822) — there is no door there to fail to open, and walling one would be a
    /// wall appearing out of nowhere rather than a lock that was always plausible.</item>
    /// <item><b>It is not the refuge and not an amenity.</b> The air rack is the one door on a dead floor
    /// that matters (#608) and the canteen is where the people are (#751). Asked by containment against the
    /// floor's own published lists, so a refuge that moves takes its exemption with it.</item>
    /// <item><b>The room has a plate.</b> The leaf wears the sign the room already had — no new sign is
    /// authored anywhere, which is the law, and a locked door with nothing written on it is the one thing in
    /// this building that would read as odd.</item>
    /// </list>
    ///
    /// <para>The lift's alcove, the specimen recess (#1063) and the found band's galleries cannot be reached
    /// from here at all: the first two are not rooms, and the third publishes no doorways.</para></summary>
    private static void Candidates(
        List<(int Room, int Way)> into,
        IReadOnlyList<Room> published,
        IReadOnlyList<SurfaceLayout.Doorway> doorways,
        IReadOnlyList<Refuge> refuges,
        IReadOnlyList<Amenity> amenities)
    {
        for (int r = 0; r < published.Count; r++)
        {
            Room room = published[r];
            if (room.Plate.Length == 0 || room.Ways.Count - 1 < FireCodeMinExits)
            {
                continue;
            }

            bool spokenFor = false;
            foreach (Refuge refuge in refuges)
            {
                spokenFor |= room.Contains(refuge.X, refuge.Y);
            }
            foreach (Amenity amenity in amenities)
            {
                spokenFor |= room.Contains(amenity.X, amenity.Y);
            }
            if (spokenFor)
            {
                continue;
            }

            for (int w = 0; w < room.Ways.Count; w++)
            {
                if (IndexOfLeaf(doorways, room.Ways[w]) >= 0)
                {
                    into.Add((r, w));
                }
            }
        }
    }

    /// <summary>#1068 · Take the one door, if this is the floor the world declines on. Called last of all in
    /// <see cref="Build(string, int, in SurfaceLayout.Field)"/> — after every placer has laid its work
    /// against a building whose doors were all open, which is what makes the subtraction one leaf rather
    /// than a different floor.
    ///
    /// <para>Does nothing at all on every floor of every site in a world where nobody has been past a seam a
    /// whole window ago, which is almost every world: <see cref="DeclinesOn"/> answers false before this
    /// method builds so much as a list.</para></summary>
    private static void DeclineOneDoor(
        string bodyId, int level,
        List<SurfaceLayout.Doorway> doorways,
        List<LockedDoor> locked,
        List<Room> published,
        IReadOnlyList<Refuge> refuges,
        IReadOnlyList<Amenity> amenities)
    {
        if (!DeclinesOn(bodyId, level))
        {
            return;
        }

        // The candidates, in the plan's own order — which is the carve's order, which is deterministic per
        // (body, level, field). A list built by walking the published rooms is the same list on every visit,
        // so the door the seed picks out of it is the same door on every visit.
        var candidates = new List<(int Room, int Way)>();
        Candidates(candidates, published, doorways, refuges, amenities);

        if (PoliteDecline.TakenDoor(bodyId, level, candidates.Count) is not { } pick)
        {
            return;
        }

        (int which, int way) = candidates[pick];
        Room room = published[which];
        SurfaceLayout.Doorway leaf = room.Ways[way];

        doorways.RemoveAt(IndexOfLeaf(doorways, leaf));

        // APPENDED, never inserted. Locked doors are addressed by index upstream (a sentry's shot-open lock,
        // a walker's chosen leaf), and a decline that renumbered them would silently move which door those
        // are about.
        locked.Add(new LockedDoor(leaf.X1, leaf.Y1, leaf.X2, leaf.Y2, room.Plate));

        var kept = new List<SurfaceLayout.Doorway>(room.Ways.Count - 1);
        for (int w = 0; w < room.Ways.Count; w++)
        {
            if (w != way)
            {
                kept.Add(room.Ways[w]);
            }
        }
        published[which] = room with { Ways = kept };
    }

    /// <summary>#1068 · Where this leaf is in the floor's doorway list, or −1. Segments are compared by their
    /// four numbers because that is what a leaf IS down here — the same identity
    /// <c>HiveInterior.LeafKey</c> uses to tell one door from another, so the plan and the renderer cannot
    /// come to two opinions about which hole in which wall this is.</summary>
    private static int IndexOfLeaf(
        IReadOnlyList<SurfaceLayout.Doorway> doorways, in SurfaceLayout.Doorway leaf)
    {
        for (int i = 0; i < doorways.Count; i++)
        {
            SurfaceLayout.Doorway d = doorways[i];
            if (Math.Abs(d.X1 - leaf.X1) < 1e-9 && Math.Abs(d.Y1 - leaf.Y1) < 1e-9
                && Math.Abs(d.X2 - leaf.X2) < 1e-9 && Math.Abs(d.Y2 - leaf.Y2) < 1e-9)
            {
                return i;
            }
        }
        return -1;
    }
}
