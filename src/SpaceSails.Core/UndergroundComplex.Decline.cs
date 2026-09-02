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

    /// <summary>#1068 · <b>WHAT THE WORLD WILL NOT TAKE.</b> A door is eligible only if all of this holds,
    /// and each clause is a promise rather than a preference:
    ///
    /// <list type="number">
    /// <item><b>It is a chamber's own corridor leaf.</b> <see cref="RoomKind.Chamber"/> and
    /// <c>Ways[0]</c> — the module the whole building is made of, and the one hole in it that
    /// <see cref="AddRoomsAlong"/> cuts first. Not the hall, not a cabinet, not a ring suite, not a WC
    /// booth, not an en-suite cell: those are rooms with a purpose somebody would have to have had.</item>
    /// <item><b>The room can spare it.</b> After the leaf goes the room still satisfies the fire code
    /// (<see cref="Room.MeetsFireCode"/>) — it keeps a second way out, or it is bedroom-small. <b>The
    /// captain is never shut in and never shut out of the only route home.</b> A chamber is a dead end off a
    /// rib and the lift is up the spine, so no chamber leaf can be on the way to the lift at all; the guard
    /// proves that by driving the A* audit with the door shut rather than by trusting this sentence.</item>
    /// <item><b>It is not the refuge and not an amenity.</b> The air rack is the one door on a dead floor
    /// that matters (#608) and the canteen is where the people are (#751). Asked by containment against the
    /// floor's own published lists, so a refuge that moves takes its exemption with it.</item>
    /// <item><b>It has a plate.</b> The leaf wears the sign the room already had — no new sign is authored,
    /// which is the law, and a blank one would be a locked door with nothing on it, which is the one thing in
    /// this building that reads as odd.</item>
    /// </list>
    ///
    /// <para>The specimen recess (#1063) and the lift alcove are not rooms and cannot be reached from here
    /// at all; the found band publishes no doorways, so it is out before the first clause.</para></summary>
    private static bool CanBeSpared(
        in Room room, IReadOnlyList<Refuge> refuges, IReadOnlyList<Amenity> amenities)
    {
        if (room.Kind != RoomKind.Chamber || room.Ways.Count == 0 || room.Plate.Length == 0)
        {
            return false;
        }

        // The fire code asked of the room it would BECOME. Room.MeetsFireCode is the one sentence the carve
        // and the sweep already read, and this is that same sentence asked one leaf early.
        if (!(room.BedroomSmall || room.Ways.Count - 1 >= FireCodeMinExits))
        {
            return false;
        }

        foreach (Refuge r in refuges)
        {
            if (room.Contains(r.X, r.Y))
            {
                return false;
            }
        }
        foreach (Amenity a in amenities)
        {
            if (room.Contains(a.X, a.Y))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>#1068 · Take the one door, if this is the floor the world took it on. Called last of all in
    /// <see cref="Build(string, int, in SurfaceLayout.Field)"/> — after every placer has laid its work
    /// against a building whose doors were all open, which is what makes the subtraction one leaf rather
    /// than a different floor.
    ///
    /// <para>Does nothing at all on every floor of every site in a world where nobody has been past a seam a
    /// whole window ago, which is almost every world: <see cref="PoliteDecline.TakenDoor"/> answers null
    /// before this method builds so much as a list.</para></summary>
    private static void DeclineOneDoor(
        string bodyId, int level,
        List<SurfaceLayout.Doorway> doorways,
        List<LockedDoor> locked,
        List<Room> published,
        IReadOnlyList<Refuge> refuges,
        IReadOnlyList<Amenity> amenities)
    {
        if (PoliteDecline.FloorOn(bodyId) != level)
        {
            return;
        }

        // The candidates, in the plan's own order — which is the carve's order, which is deterministic per
        // (body, level, field). A list built by walking the published rooms is the same list on every visit,
        // so the door the seed picks out of it is the same door on every visit.
        var candidates = new List<int>();
        for (int r = 0; r < published.Count; r++)
        {
            if (CanBeSpared(published[r], refuges, amenities))
            {
                candidates.Add(r);
            }
        }

        if (PoliteDecline.TakenDoor(bodyId, level, candidates.Count) is not { } pick)
        {
            return;
        }

        Room room = published[candidates[pick]];
        SurfaceLayout.Doorway leaf = room.Ways[0];

        // The leaf has to actually be one of the floor's published doorways — it always is for a chamber on
        // a listed floor, and asking rather than asserting is what keeps this from ever half-applying: a
        // decline that removed a way from a room without shutting the drawn door would be the drawn world
        // and the sim disagreeing, which is the exact bug class the pass exists inside of.
        int inPlan = doorways.FindIndex(d => Same(d, leaf));
        if (inPlan < 0)
        {
            return;
        }

        doorways.RemoveAt(inPlan);

        // APPENDED, never inserted. Locked doors are addressed by index in more than one place upstream, and
        // a decline that renumbered them would silently move which door a sentry had already shot open.
        locked.Add(new LockedDoor(leaf.X1, leaf.Y1, leaf.X2, leaf.Y2, room.Plate));

        var kept = new List<SurfaceLayout.Doorway>(room.Ways.Count - 1);
        for (int w = 1; w < room.Ways.Count; w++)
        {
            kept.Add(room.Ways[w]);
        }
        published[candidates[pick]] = room with { Ways = kept };

        static bool Same(in SurfaceLayout.Doorway a, in SurfaceLayout.Doorway b) =>
            Math.Abs(a.X1 - b.X1) < 1e-9 && Math.Abs(a.Y1 - b.Y1) < 1e-9
            && Math.Abs(a.X2 - b.X2) < 1e-9 && Math.Abs(a.Y2 - b.Y2) < 1e-9;
    }
}
