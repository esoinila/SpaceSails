using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Part of <see cref="HiveInterior"/> (the header note lives in HiveInterior.cs) — THE PASSES THAT BUILD
/// THE FLOOR ITSELF: the poured hull and its glazing, the one kept specimen, every doorway and the doors
/// that never open, the rooms still worth searching, and the refuges you go looking for when the air goes.
///
/// <para>#1164 · Each of these was a <c>// ── banner ──</c> section inside <see cref="FloorDeck"/> and is
/// now a named pass called from it IN THE SAME ORDER. The order is the deck: a wall added after a door is
/// a leaf with a partition behind it, and the pinned frame ledger reads these lists in the order they were
/// filled. Every accumulator is handed in explicitly — no pass reaches for a field and none declares one,
/// because a static field moved out of the opening file initialises in the order the compiler reads the
/// FILES rather than the order a reader sees (#1163's named bug class, which stood a station's furniture
/// on the hall floor with no warning at all).</para>
/// </summary>
public static partial class HiveInterior
{
    /// <summary>
    /// #585/#677 · POURS THE FLOOR'S OWN STRUCTURE — every wall Core cut, drawn in the made-thing ink or,
    /// past the seam, in the third idiom that belongs to no palette. Lays down: walls.
    /// </summary>
    private static void PourTheStructure(
        List<DeckPlan.Wall> walls, in UndergroundComplex.FloorPlan floor, bool pastTheSeam)
    {
        // The structure. Everything down here is MADE — poured, welded, bolted — so it draws in the ship's
        // own pressure-hull ink rather than in the body's stone (#589). That contrast is the point: you have
        // just left a world built out of what was under your boots and walked into something imported whole.
        foreach (SurfaceLayout.Wall w in floor.Walls)
        {
            // #585 · EVERY wall down here is hull-bright, not just the spine. Owner, standing in a corridor:
            // "this looks weird now... I am like outside the structure?" / "like in the regolith."
            //
            // He was reading the INK, and the ink was lying. Only the spine carried IsHull, so the ribs and
            // every room wall drew in the dim inner-line stroke — which is the same faint grey the surface
            // uses for rubble and scree. A captain who has just ridden a lift into a poured, powered,
            // still-lit facility was being shown regolith line-work and correctly concluded they were
            // outside.
            //
            // Nothing down here is rubble. It was cut, poured and bolted by people with a budget, so it draws
            // like the made thing it is — the brightest structure the game has shown since the ship.
            //
            // #677 · …and past the seam it is none of that. A gallery is not poured and it is not the moon's
            // rock either, so it takes the third idiom and, with it, no ink from the department livery this
            // plan may be carrying. The concrete stops at a line and the material changes, which is the whole
            // sentence the drawing is allowed to say about it.
            walls.Add(new(
                (float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2,
                false, IsHull: !pastTheSeam, IsSeamless: pastTheSeam));
        }
    }

    /// <summary>
    /// #759 · GLAZES THE OPENINGS Core keeps out of the wall list, as walls carrying IsWindow: the drawn half
    /// of a segment whose collided half is already poured. Lays down: walls.
    /// </summary>
    private static void GlazeTheOpenings(
        List<DeckPlan.Wall> walls, in UndergroundComplex.FloorPlan floor, bool pastTheSeam)
    {
        // ── #759 · THE GLAZING, PUT BACK INTO THE DECK AS WHAT IT IS ────────────────────────────────────
        //
        // Owner's pinned requirement for this room's own pictures: a) A VIEW TO THE PARK and b) A WINDOW
        // WALL BETWEEN. Core keeps these segments OUT of floor.Walls so nothing can draw them as poured
        // concrete; they come back in here as walls carrying IsWindow — the flag the ship's bridge glass and
        // her cantina's panoramic window have used since the deck was built, so the ink is the game's own
        // window ink and this file invents nothing.
        //
        // BOTH HALVES OFF ONE SEGMENT. It is a wall, so the collision field takes it and no body crosses it;
        // it is a window, so the eye does. Two segments on one line — one to draw, one to collide — is the
        // drawn-versus-simulated split this house has a name for.
        foreach (SurfaceLayout.Wall w in floor.Windows ?? [])
        {
            walls.Add(new(
                (float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2,
                IsWindow: true, IsHull: !pastTheSeam));
        }
    }

    /// <summary>
    /// #1063 · KEEPS THE ONE SPECIMEN a filled-in ground was allowed to keep — one old leaf at the back of a
    /// recess, drawn shut and impassable. Lays down: walls, doors.
    /// </summary>
    private static void KeepTheSpecimen(
        List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors, in UndergroundComplex.FloorPlan floor)
    {
        // ── #1063 · THE ONE SPECIMEN THE BURIAL KEEPS ───────────────────────────────────────────────────
        //
        // A short recess off the corridor on the listed bottom of a ground somebody filled in, with a single
        // old door at the back of it, kept for display. The recess itself is ordinary poured hull and came in
        // with `floor.Walls` above; THIS is the leaf, and it is the only segment on any listed floor in the
        // game drawn in the third idiom (#677's `IsSeamless`, which belongs to no palette and says who built
        // nothing). The material is the whole statement — the rag's own line is the same sentence and does
        // not know it: "the old kerbs make a handsome course of masonry in the new wall."
        //
        // BOTH HALVES OFF ONE SEGMENT, exactly as the glazing above and every locked door below: a leaf that
        // is drawn shut and a wall a body cannot cross. It has no sign console and no card, because #1063
        // authored no words for it and a caption would be the one helpful sentence that kills the beat.
        if (floor.TheSpecimen is { } kept)
        {
            walls.Add(new(
                (float)kept.X1, (float)kept.Y1, (float)kept.X2, (float)kept.Y2,
                IsWindow: false, IsHull: false, IsSeamless: true));
            doors.Add(new(
                (float)kept.X1, (float)kept.Y1, (float)kept.X2, (float)kept.Y2, Locked: true));
        }
    }

    /// <summary>
    /// #821 · ANSWERS WHICH CUBICLE LEAVES HAVE THE CATCH OVER, before a single doorway is hung — the set
    /// exists to be handed to <see cref="HangTheDoorways"/> and to nothing else. Lays down: nothing.
    /// </summary>
    private static HashSet<string> WhichLeavesAreShut(
        in UndergroundComplex.FloorPlan floor, int level, IReadOnlyCollection<string>? cubiclesShut)
    {
        // ── #821 · WHICH CUBICLE LEAVES HAVE THE CATCH OVER ────────────────────────────────────────────
        //
        // Worked out BEFORE the doorway loop, because a shut cubicle is a door that is drawn shut AND a wall
        // that a body cannot cross — the very idiom #585's never-opening doors already use, one scale down.
        // Keyed on the leaf's own geometry so the doorway list and the cubicle list cannot come to two
        // different opinions about which hole in which partition this is.
        var shut = new HashSet<string>(StringComparer.Ordinal);
        if (cubiclesShut is { Count: > 0 })
        {
            // #775 · The floor's own ring, not the park's view of it — a landscape floor's big suites have
            // service strips with cubicles in them too, and a lock that could only be found through a
            // garden would be a lock that silently stopped working two floors down.
            foreach (UndergroundComplex.RingRoom suite in floor.TheRing)
            {
                foreach (RingOffice.Stall cell in suite.Cubicles)
                {
                    if (cubiclesShut.Contains(CubicleKey(level, in cell)))
                    {
                        shut.Add(LeafKey(cell.Door));
                    }
                }
            }
        }

        return shut;
    }

    /// <summary>
    /// #770 · …AND WHICH STREET LEAVES BELONG TO A ROOM THE CAPTAIN HAS PAID FOR. The mirror of <see
    /// cref="WhichLeavesAreShut"/> and the doorway pass's other input. Lays down: nothing.
    /// </summary>
    private static HashSet<string> WhichLeavesAreBooked(
        in UndergroundComplex.FloorPlan floor, int level, long canteenWatch, RoomBooking.Booking? booked)
    {
        // ── #770 · …AND WHICH STREET LEAVES BELONG TO A ROOM THE CAPTAIN HAS PAID FOR ──────────────────
        //
        // The cubicle's own idiom one room up, with the one difference the feature is about: a booked leaf is
        // DRAWN dogged and is NOT walled. RoomBooking.HonoursTheDoor is the law — the door honours the
        // BOOKER and only until the watch turns — and the only body on this deck is the booker, so what a
        // captain sees is their own room shut against the corridor and open to them. Nothing is added to
        // floor.Walls, because a wall here would be the plan refusing the one person the room is for.
        var bookedLeaves = new HashSet<string>(StringComparer.Ordinal);
        if (booked is { } theRoom && floor.Park is { } bookedPark)
        {
            foreach (UndergroundComplex.RingRoom suite in bookedPark.Frontage)
            {
                if (!RoomBooking.HoldsThisRoom(in theRoom, level, suite.Number, canteenWatch))
                {
                    continue;
                }
                foreach (SurfaceLayout.Doorway leaf in suite.Doors)
                {
                    bookedLeaves.Add(LeafKey(leaf));
                }
            }
        }

        return bookedLeaves;
    }

    /// <summary>
    /// HANGS EVERY DOORWAY in one of the three states the two sets above decide between: dogged for the
    /// captain's own booked room, shut with a partition behind it, or the ordinary way through. Lays down:
    /// walls, doors.
    /// </summary>
    private static void HangTheDoorways(
        List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors, in UndergroundComplex.FloorPlan floor,
        HashSet<string> shut, HashSet<string> bookedLeaves)
    {
        foreach (SurfaceLayout.Doorway d in floor.Doorways)
        {
            if (bookedLeaves.Contains(LeafKey(d)))
            {
                doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Locked: true));
                continue;
            }
            if (shut.Contains(LeafKey(d)))
            {
                // Drawn as the shut door it is, with the partition behind it. #821's law is that this buys
                // TIME and not safety: what it buys is exactly this one segment, and the man outside is
                // still on the floor.
                doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Locked: true));
                walls.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, false, false));
                continue;
            }
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: true));
        }
    }

    /// <summary>
    /// #585/#803 · HANGS THE DOORS THAT NEVER OPEN — the illusion of scale, each with a real wall behind it
    /// and its sign still readable — and the ones a sentry has taken the hasp off. Lays down: walls, doors,
    /// consoles.
    /// </summary>
    private static void HangTheLockedDoors(
        List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors, List<DeckPlan.ConsoleSpot> consoles,
        in UndergroundComplex.FloorPlan floor, int level, IReadOnlyCollection<string>? locksShotOpen)
    {
        // #585 · THE DOORS THAT NEVER OPEN. The owner asked for these by name as the illusion of scale, and
        // they are drawn Locked — cold, always shut, with a real wall behind them — so nothing about them
        // ever hints that a way through exists. [E] reads the sign; that is all it will ever do.
        //
        // #803 · …unless somebody took the hasp off it with a sentry. That is a LOUD, deliberate, six-round
        // act performed with the captain's own handset, and it is the one thing in the game allowed to move
        // one of these — so the illusion is untouched by walking past forty of them and spent, one door at a
        // time, by a captain who decided a particular door was worth telling the whole floor about.
        foreach (UndergroundComplex.LockedDoor l in floor.Locked)
        {
            if (locksShotOpen is not null && locksShotOpen.Contains(LockKey(level, l)))
            {
                // No wall behind it now, and the leaf is drawn as the ordinary way-through it has become.
                // The plate stays readable — an affordance that vanishes when it is used is #212's shape —
                // and it wears the hole rather than the padlock so the deck says which of the two it is.
                doors.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, Imported: true));
                consoles.Add(new(DeckPlan.ConsoleKind.HiveSign,
                    (float)((l.X1 + l.X2) / 2), (float)((l.Y1 + l.Y2) / 2), $"{ShotOpenGlyph} {l.Sign}"));
                continue;
            }

            doors.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, Locked: true));
            walls.Add(new((float)l.X1, (float)l.Y1, (float)l.X2, (float)l.Y2, false, false));
            consoles.Add(new(DeckPlan.ConsoleKind.HiveSign,
                (float)((l.X1 + l.X2) / 2), (float)((l.Y1 + l.Y2) / 2), $"🔒 {l.Sign}"));
        }
    }

    /// <summary>
    /// #573 · OFFERS THE ROOMS THAT DO OPEN, minus the ones already emptied — which keep their walls and
    /// their door and simply stop offering anything. Lays down: consoles.
    /// </summary>
    private static void OfferTheRooms(
        List<DeckPlan.ConsoleSpot> consoles, in UndergroundComplex.FloorPlan floor, int level,
        IReadOnlyCollection<int> emptiedRooms)
    {
        // What is in the rooms that DO open. An emptied room keeps its walls and its door — it stays a place
        // you have been, the #573 law — and simply stops offering anything.
        for (int i = 0; i < floor.RoomCentres.Count; i++)
        {
            if (emptiedRooms.Contains(RoomKey(level, i)))
            {
                continue;
            }
            (double rx, double ry) = floor.RoomCentres[i];
            consoles.Add(new(DeckPlan.ConsoleKind.HiveHaul, (float)rx, (float)ry, "🔦 SEARCH THE ROOM"));
        }
    }

    /// <summary>
    /// #608 · MARKS THE REFUGES so they can be FOUND: the rack's own console, kept on all three states so a
    /// dead one can still say why it is dead, and the plate over the door at signage size. Lays down:
    /// consoles, labels.
    /// </summary>
    private static void MarkTheRefuges(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        in UndergroundComplex.FloorPlan floor)
    {
        // ── #608 · THE REFUGES, DRAWN TO BE FOUND ───────────────────────────────────────────────────────
        //
        // Owner, after suffocating on B2: "the rooms should have airlocks etc ... some havens :-D" / "on
        // surface there are emergency shelters :-D" / "there should be like at least one air replenish
        // station in each of the airless labs underground... for pure safety".
        //
        // The hardest thing about a dead floor is that every door on it looks like every other door, and one
        // of them is the only one that matters. On the surface a shelter is a DIFFERENT SHAPE of building
        // seen across open ground; down here every room is the same poured box off the same rib, so the
        // refuge has to be told rather than shown — a plate over its door, at the signage size the depth
        // plate uses, because this is the second question a captain asks after "which floor is this".
        //
        // The rack is a console like any other so the [E] verb is the surface's verb, and the room's own
        // doorway is already drawn Imported violet with every other door down here (#592's language: what
        // was flown in, versus what was cut out of the moon).
        foreach (UndergroundComplex.Refuge refuge in floor.Refuges)
        {
            // #608 · The console stays on all three states and the CAPTION is what changes. It has to stay:
            // the [E] on a dead refuge is the only thing that can say why it is dead, and #212's law is that
            // a refusal said out loud beats an affordance that quietly is not there. What it may not do is
            // go on advertising a RACK in a room whose inner door will not cycle — a prompt that names a
            // machine you cannot reach is the affordance-you-see differing from the affordance-you-get.
            consoles.Add(new(DeckPlan.ConsoleKind.HiveRefuge,
                (float)refuge.X, (float)refuge.Y,
                UndergroundComplex.RefugeStillHolds(refuge.State)
                    ? UndergroundComplex.RefugeTankLabel
                    : UndergroundComplex.RefugeFailedGlyph));
            labels.Add(((float)refuge.X, (float)(refuge.Y - UndergroundComplex.RefugeHalfHeight - 2.0),
                refuge.Sign));
        }
    }
}
