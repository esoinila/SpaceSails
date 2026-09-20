using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #608 · THE REFUGES — A DEAD FLOOR IS A FLOOR OF SUIT-WORK, AND SUITS RUN OUT ─────────────────────
    //
    // Owner, in the order he said it, after suffocating on B2: "I thought there is air in the base?" ...
    // "there should be a warning or something :-D ... the rooms should have airlocks etc ... some havens
    // :-D" ... "like the basement is more dangerous than the surface now :-D" ... "on surface there are
    // emergency shelters :-D" ... "Still for safety there would need to be a couple of places with air lock
    // and air refilling, because otherwise the elevator being busy could kill employees, and those honest
    // criminal scientists are hard to recruit :-D" ... and finally, deciding it:
    //
    //     "there should be like at least one air replenish station in each of the airless labs
    //      underground... for pure safety"
    //
    // AT LEAST ONE, ON EVERY AIRLESS FLOOR. Not "most floors", not "a rare one" — a regulation, in-world and
    // in code, and RefugesAreOnEveryAirlessFloor walks every floor of every band on every body to say so.
    //
    // THE REASON IT IS RIGHT, which is the owner's and is better than the mechanic it costs. He also ruled
    // on why any floor down here is pressurised at all: "the thought about the dead floors is that it is
    // very difficult to work in the suit. So all work would happen out of it. So any room that would house
    // like office work would be pressurized by that constraint" — "like writing with a pen ... reading
    // documents etc.... that kind of thing would not happen at all in vacuum as a working environment" —
    // "or any kind of fine motor skill stuff".
    //
    // So an airless floor is not an ABANDONED floor. It is a floor of SUIT-WORK: storage, hauling, plant,
    // hard-vacuum process. It had people in it, in suits, all day, every day — and a building that staffs a
    // vacuum floor and gives its staff nowhere to go when a tank runs short is a building that is one busy
    // lift away from killing somebody. Whoever inspected this place made them pay for the refuge. That the
    // pressure vessels are still holding decades after the last invoice is the same sentence the surface
    // shelter tells (#573): somebody built this for a stranger and it outlasted them.
    //
    // WHAT IT DOES NOT DO IS CANCEL #585. Depth is still paid for in air, because a refuge is not a floor:
    //
    //   * it is NEVER beside the lift (MinRefugeDetourDu) — reaching one is a decision to detour, which is
    //     the verb #608 asked for: not "how long dare I stay" but "can I get from the car to the refuge to
    //     the room I want and back";
    //   * its rack is the SURFACE rack, law for law — SurfaceShelter.Produce/Transfer and the two-thirds
    //     ceiling somebody set on purpose for the next person through the door. More refuges buy RANGE,
    //     never independence, exactly as more shelters do;
    //   * it holds pressure and nothing else. There is no locker down here, no reload, no bunk.
    //
    // Canon holds: the plate says what the room is FOR and never what the building was for. A safety sign is
    // the one thing on this ground that is allowed to be plain — a captain who cannot find air is not being
    // teased (#573) — and it is still an inspectorate's sign, not an explanation.

    /// <summary>Half the breathable width of a refuge, in deck units — the room's own box, inset by the
    /// poured wall. <see cref="RefugeHolds"/> is the one place that reads it.</summary>
    public const double RefugeHalfWidth = 6.3;

    /// <summary>Half the breathable height of a refuge.</summary>
    public const double RefugeHalfHeight = 4.8;

    /// <summary>How far a refuge must stand from the lift before it counts as one worth having.
    ///
    /// <para>#608: <i>"Never on the way. If it sits beside the lift it is decoration; it earns its existence
    /// by being somewhere you have to decide to detour to."</i> Measured from the shaft, so this is the
    /// smallest walk a captain can ever be asked for — and it is a floor plan, so the real one is longer.</para>
    ///
    /// <para><b>Why 70 and not 34.</b> This shipped for an hour as 34, which was chosen by eye and was
    /// WORTHLESS: the nearest room to the shaft that this generator can produce, measured over 808 dead
    /// floors, is 34.2 du out — so every room on every floor qualified, the constraint selected nothing, and
    /// the guard that was supposed to enforce it passed happily on a build deliberately rigged to put the
    /// refuge in the closest room there is. That is the house rule this repo names out loud (revert the fix
    /// and watch the guard go RED), and it caught a threshold that meant nothing.</para>
    ///
    /// <para>At 70 it is twice the nearest possible room and still satisfiable on every floor the generator
    /// makes, so the detour is real AND the fallback below never has to fire.</para></summary>
    public const double MinRefugeDetourDu = 70.0;

    /// <summary>Is (<paramref name="x"/>, <paramref name="y"/>) inside the air of the refuge centred at
    /// (<paramref name="cx"/>, <paramref name="cy"/>)?
    ///
    /// <para><b>The one containment law</b>, so Core, the audit and the live suit cannot disagree about
    /// whether the captain is breathing. Rectangular rather than the shelter's inscribed ellipse
    /// (<c>SurfaceShelter.Contains</c>) for the one reason that matters: a shelter is a regolith drum and
    /// its corners are metres of piled dirt, while this is a POURED ROOM with square corners — an ellipse
    /// here would leave a captain standing plainly inside a sealed room watching their tank tick down, which
    /// is precisely the kind of instrument-disagrees-with-the-world lie this ground keeps paying for.</para></summary>
    public static bool RefugeHolds(double cx, double cy, double x, double y) =>
        Math.Abs(x - cx) <= RefugeHalfWidth && Math.Abs(y - cy) <= RefugeHalfHeight;

    // ── #608/#1149 · AND WHAT DECADES DID TO IT: ALMOST NOTHING ─────────────────────────────────────────
    //
    // The regulation above says what was BUILT, and it is not in question: every airless floor has a refuge
    // and the plan still marks it. What the plan does not carry is whether the thing still works — and the
    // owner ruled on that twice, the second time reversing the first.
    //
    // #608, in the same breath he asked for the refuges: "their state after decades is the story. The ones
    // that still hold are the ones somebody maintained; the ones that do not are the ones somebody stopped
    // being paid to." That shipped as #1087's seeded split — a fifth holding, and a maintenance line the
    // department either kept or lost.
    //
    // #1149, 2026-09-06, is the correction, and it is the world's answer rather than ours: "On a failed
    // floor the emergency station most probably still works decades or centuries after everything else
    // stopped — robustness and reliability were the metrics it was built to ... They almost never fail from
    // old age; something happened, and we tell it." Safety equipment is not a service a department buys. It
    // is a REGULATION, built to a spec written by people who assumed nobody would be maintaining it on the
    // day it mattered, and a fire extinguisher in an abandoned office block still discharges.
    //
    // So the room is a fact, the SEAL is still a story, and all three states survive with new causes — see
    // UndergroundComplex.Inspection.cs, which holds the whole of the new law and the reasoning for it:
    //
    //   HOLDING · the default, on every floor, whatever the department. Nothing is rolled for it.
    //   EMPTY   · not decay: somebody DREW on it (#573's reservoir idiom, a visitor's footprint), rare, and
    //             it comes back on the rack's own clock. It costs a captain TIME, never range.
    //   FAILED  · an EVENT and never age: at most one per site, on a minority of sites, never the first
    //             refuge a captain reaches, and told by a card with its own painting.
    //             Owner, on the fan (#604): "A refuge whose seal has failed must still paint, and must read
    //             as failed. Walking to one and finding it dead is a real beat; walking to one that was
    //             never marked is just a bad map."

    /// <summary>What a refuge's seal has done with the decades since anybody paid for it.</summary>
    public enum RefugeState
    {
        /// <summary>The door cycles, the room holds, and there is air in the rack.</summary>
        Holding,

        /// <summary>The door cycles and the room holds. The rack is drawn down: somebody was here before you,
        /// and it is coming back on its own clock.</summary>
        Empty,

        /// <summary>The seal went. The room is on the plan and holds nothing.</summary>
        Failed,
    }

    /// <summary>Is a pressure refuge marked on this floor's plan? The LAW, not a count taken off a built
    /// floor — the lift panel asks it about floors it has not generated and the card asks it about the one
    /// underfoot, and a panel that answered by building twenty floors would be a panel nobody presses twice.
    ///
    /// <para><c>EveryAirlessFloorHasARefuge</c> and <c>APressurisedFloorCarriesNoRefuge</c> are what keep
    /// this honest: they walk every floor of a hundred sites and check that the generator agrees with the
    /// sentence written here.</para></summary>
    public static bool RefugeOnThePlan(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return level < 0 && !HoldsPressure(bodyId, level);
    }

    /// <summary>What state the refuge on this floor is in — null where the plan marks none.
    ///
    /// <para>A fact about the FLOOR rather than about the room, which is why the client may ask it directly
    /// instead of carrying it down through the deck plan: there is one refuge per floor, its state is decided
    /// here, and one ask is one answer.</para>
    ///
    /// <para><b>#1149 · It holds, unless something happened to it.</b> No department is consulted and no coin
    /// is tossed over decay — the owner's ruling is that a refuge is built to a robustness spec and outlasts
    /// the building around it. One thing can still be true of it: somebody drew the rack down before you got
    /// to it (<see cref="SomebodyDrewTheRackDown"/>), which is a footprint and not age.</para>
    ///
    /// <para><b>#619 · AND IT IS NEVER <see cref="RefugeState.Failed"/>.</b> That is the law, written here
    /// because here is where every other surface in the game asks. The room that failed is a SECOND chamber
    /// on its floor (<see cref="CarveRefuges"/>), welded shut, and it is not what this sentence is about —
    /// so the panel row, the dead-air card, the suit and the lift can all go on believing that a floor
    /// carrying a refuge carries air behind a door that cycles, because it does. Ask
    /// <see cref="RefugeThatFailedIsOn"/> for the other room.</para></summary>
    public static RefugeState? StateOfTheRefugeOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!RefugeOnThePlan(bodyId, level))
        {
            return null;
        }
        return SomebodyDrewTheRackDown(bodyId, level) ? RefugeState.Empty : RefugeState.Holding;
    }

    /// <summary>Does the refuge on this floor hold pressure at all — the one question the suit asks. Empty
    /// still counts: the door cycles and the room holds, and that is the difference between a wait and a
    /// death.</summary>
    public static bool RefugeStillHolds(RefugeState state) => state != RefugeState.Failed;

    /// <summary>What is said once, at the door, by state. Two sentences for two worlds, and neither of them
    /// says what to do about it.
    ///
    /// <para>#619 · <b>There is no third sentence any more, and there cannot be one.</b> This carried a line
    /// for <see cref="RefugeState.Failed"/> — <i>"the seal went a long time ago"</i> — and it was said while
    /// the captain stood INSIDE the room. Under #619's law the one refuge in the game that failed is welded
    /// shut and nobody stands in it, so the sentence described a place no captain can be; and it named AGE,
    /// which is the single cause the owner's 2026-09-06 ruling took off the table. The card at that door is
    /// the whole of the telling (#761). This is asked only of a room a captain is standing in.</para></summary>
    public static string RefugeEntryLine(RefugeState state) =>
        state == RefugeState.Empty
            ? "🫁 The door cycles and the room holds. The fill line reads empty, and the tag on the valve is "
                + "dated years ago."
            : "🫁 The door cycles and the gauge climbs. A rack of bottles on the wall, and the meter on the "
                + "fill line still turns.";

    /// <summary>One pressure refuge: a room somebody kept the seals on, with an air cracker in it.</summary>
    /// <param name="State">#608 · What the decades did to it — <see cref="StateOfTheRefugeOn"/>, carried on
    /// the room so the renderer that draws the plate has the answer the suit is using.</param>
    /// <param name="DoorX">#619 · The midpoint of the way into it. Carried rather than worked out again,
    /// because the one welded room in the game is read at its DOOR and never in it — the plate hangs there,
    /// the mark on the fan points there and [E] is pressed there — and a renderer re-deriving which of this
    /// room's four walls had the hole in it would be a second author of geometry it does not own (§13.15).
    /// On a refuge you can walk into it is the same door, and nothing reads it.</param>
    public readonly record struct Refuge(
        double X, double Y, string Sign, RefugeState State = RefugeState.Holding,
        double DoorX = 0, double DoorY = 0)
    {
        /// <summary>Is the captain in its air? <see cref="RefugeHolds"/>, so there is only ever one answer.
        /// Geometry only — whether that air EXISTS is <see cref="State"/>'s business.</summary>
        public bool Contains(double x, double y) => RefugeHolds(X, Y, x, y);
    }

    /// <summary>What is stencilled beside a refuge door. An inspectorate's plate: a number, an occupancy and
    /// a date somebody stopped renewing — which is the whole story of this building told by a form.</summary>
    public static string RefugeSign(string bodyId, int level, int index)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ulong seed = DiceRule.Seed($"hive:refuge-sign:{bodyId}:{level}:{index}");
        int number = (int)(seed % 40) + 1;
        int occupancy = 4 + (int)((seed / 11) % 9);
        return $"🫁 PRESSURE REFUGE {number} · OCCUPANCY {occupancy} · KEEP CLEAR";
    }

    /// <summary>The refuges on a floor, taken out of the rooms it had already built.
    ///
    /// <para>A refuge IS one of the floor's rooms — three poured walls and a doorway cut in its corridor
    /// face — and that is deliberate rather than lazy. A room is already audited walkable from the lift
    /// (13.1), already has a door the captain can find, and already sits down a rib rather than on the
    /// spine. Inventing a second kind of chamber would be a second thing to keep reachable, and a refuge you
    /// cannot walk to is a refuge that does not exist.</para>
    ///
    /// <para>It stops being a haul room when it becomes one: a pressure vessel somebody maintained is not a
    /// drawer to turn over, and the air is what it pays.</para>
    ///
    /// <para>#619 · <b>AND ON ONE FLOOR OF ONE SITE IN FOUR, A SECOND ONE THAT FAILED — never instead of the
    /// first.</b> The owner's issue is exact about it: <i>"a SECOND refuge, on one floor, that failed … never
    /// the only one on its floor, so it can never kill anybody who trusted the instrument."</i> So the working
    /// refuge is carved FIRST, out of the same pool, by the same roll it has always used — nothing about a
    /// floor that has no story on it changes by a byte — and the failed one is taken afterwards, out of what
    /// is left. A captain who walks to the mark the fan paints as air finds air; the other room is a room
    /// they can choose to walk to, and it is welded shut.</para>
    ///
    /// <para>It is welded by the building's own lock seam (<see cref="LockedDoor"/>): every way out of that
    /// chamber becomes a leaf that never opens with a real wall behind it. That is the difference between a
    /// refuge that is out of service and a refuge that is merely empty, and it is the reason the air
    /// machinery does not have to be told anything — the room never joins the list of places that breathe,
    /// and the floor plan agrees with it.</para></summary>
    private static List<Refuge> CarveRefuges(
        string bodyId, int level, List<Room> rooms, List<LockedDoor> locked,
        in SurfaceLayout.Field field)
    {
        var refuges = new List<Refuge>();
        if (HoldsPressure(bodyId, level) || rooms.Count == 0)
        {
            return refuges;   // a pressurised floor IS the refuge — and every gallery is one (#677)
        }

        // #592 · The one room that may never be taken. On a site with a band nobody listed, room 0 of the
        // last listed floor is the card that reaches it (KeyRoomFor) — designated exactly because a rolled
        // index would sometimes miss and strand the whole feature forever. Turning it into a refuge would
        // do the same thing by a different route.
        int reserved = KeyRoomFor(bodyId) is { } key && key.Level == level ? key.RoomIndex : -1;

        var faraway = new List<int>();
        var anywhere = new List<int>();
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == reserved)
            {
                continue;
            }
            anywhere.Add(i);

            // #801 · A DETOUR FROM EVERY CAR, not from the cage. This measured one shaft, and the day the
            // building grew a second one at the other end of the corridor it went on passing while the
            // sentence it exists to protect died: a third of the refuges in the game were four steps from
            // the goods car. The guard found it (332 of 1130 floors); the fix is that the carve asks the
            // same list the guard does.
            bool far = true;
            foreach (Shaft car in ShaftsOn(field))
            {
                double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                far &= (dx * dx) + (dy * dy) >= MinRefugeDetourDu * MinRefugeDetourDu;
            }
            if (far)
            {
                faraway.Add(i);
            }
        }

        // The detour is the design, so it is preferred — but it is NOT allowed to cost the guarantee. On a
        // floor whose rooms all happen to crowd the shaft, a near refuge beats no refuge, every time: the
        // owner's line is "at least one ... for pure safety", and a safety regulation that a seed can talk
        // out of is not one.
        // #801 · …and when NOTHING qualifies, the fallback takes the FURTHEST room rather than a rolled one.
        // With two cars at opposite ends of the spine there are floors whose every chamber is inside the
        // detour of one car or the other, and on those the old fallback rolled a room at random — which on
        // the sweep put a refuge twenty-nine du from a car on floors that had a sixty-du one going spare.
        // A safety regulation a seed can talk out of is not one, and neither is one it can shrug at.
        List<int> pool = faraway;
        if (pool.Count == 0 && anywhere.Count > 0)
        {
            int best = anywhere[0];
            double bestNear = -1;
            foreach (int i in anywhere)
            {
                double near = double.MaxValue;
                foreach (Shaft car in ShaftsOn(field))
                {
                    double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                    near = Math.Min(near, (dx * dx) + (dy * dy));
                }
                if (near > bestNear)
                {
                    (best, bestNear) = (i, near);
                }
            }
            pool = [best];
        }
        if (pool.Count == 0)
        {
            return refuges;
        }

        int pick = pool[DiceRule.Roll(DiceRule.Seed($"hive:refuge:{bodyId}:{level}"), pool.Count).Face - 1];
        Room chosen = rooms[pick];
        rooms.RemoveAt(pick);
        (double doorX, double doorY) = WayIn(chosen);
        refuges.Add(new Refuge(
            chosen.X, chosen.Y, RefugeSign(bodyId, level, 0),
            // The seal is decided by the FLOOR, not by the carve, and asked here rather than worked out
            // again: the panel, the card, the tracker and the suit all read StateOfTheRefugeOn, and a room
            // that carried a second opinion about its own door is this repo's oldest and dearest bug.
            //
            // #619 · …and what that answer can no longer be is FAILED. The floor's own refuge holds or it is
            // dry; the room that failed is the extra one below, and it is the whole reason this method can
            // still promise that the mark a captain walks a tank toward is air.
            StateOfTheRefugeOn(bodyId, level) ?? RefugeState.Holding,
            doorX, doorY));

        // ── #619 · AND THE ONE THAT FAILED, WHICH IS AN EXTRA ROOM AND NEVER THE FLOOR'S OWN ────────────
        //
        // Second, out of what the first one left, and only where the site's story happened
        // (FailedRefugeFloorOf — one site in four, one floor of it). Taken from the same pool by the same
        // preference, so it is a detour like every other refuge in the building rather than a prop set down
        // beside the lift for the captain to trip over.
        //
        // The pool can be empty here and that is allowed: a floor the generator built with exactly one
        // takeable chamber keeps it as the working refuge and simply has no story on it. The law is
        // "never the only refuge on its floor", and the way to keep a law like that is to let the beat go
        // rather than to let the safety regulation go.
        if (!RefugeThatFailedIsOn(bodyId, level))
        {
            return refuges;
        }

        var left = new List<int>();
        foreach (int i in faraway.Count > 0 ? faraway : anywhere)
        {
            // The indices were taken before the first refuge came out of the list, so they are re-walked
            // against the list as it stands now rather than arithmetically shifted — an index adjusted by
            // hand is the shape of the bug KeyRoomFor and CarveRefuges were both written to avoid.
            int after = i < pick ? i : i - 1;
            if (i != pick && after >= 0 && after < rooms.Count)
            {
                left.Add(after);
            }
        }
        if (left.Count == 0)
        {
            return refuges;
        }

        int second = left[
            DiceRule.Roll(DiceRule.Seed($"hive:refuge-failed:{bodyId}:{level}"), left.Count).Face - 1];
        Room welded = rooms[second];
        rooms.RemoveAt(second);
        (double wx, double wy) = WayIn(welded);
        refuges.Add(new Refuge(
            welded.X, welded.Y, RefugeSign(bodyId, level, 1), RefugeState.Failed, wx, wy));

        // THE WELD ITSELF, in the building's own grammar. A LockedDoor is a leaf that never opens with a
        // real wall behind it, and that is exactly what a door welded from the inside is — so the air
        // machinery, the walkers, the Reevers, the A* audit and the renderer all learn about it from the one
        // list they already read, and none of them has to be told that this room is special.
        foreach (SurfaceLayout.Doorway way in welded.Ways)
        {
            locked.Add(new(way.X1, way.Y1, way.X2, way.Y2, RefugeFailedGlyph));
        }
        return refuges;
    }

    /// <summary>#619 · How far OUT of the room the welded refuge's press stands, in deck units.
    ///
    /// <para><b>It has to be out at all, and that is a bug this lane paid for.</b> The press first sat on
    /// the doorway's own midpoint — which is exactly where the weld goes — so on the two scenario floors
    /// that carry one, the A* audit found a console inside solid wall and a card that could never be read.
    /// The doorway is a WALL now; the captain stands in the corridor in front of it.</para>
    ///
    /// <para>Two du clears the avatar (<c>DeckPlan.AvatarRadius</c> = 0.7) with room to spare, stays well
    /// inside the interact reach (3.0), and stays inside the corridor's own half-width
    /// (<see cref="CorridorHalf"/> = 3.5) — so the spot is in the rib a captain is already walking down and
    /// never through it into whatever stands on the far side.</para></summary>
    public const double WeldedRefugeStandOffDu = 2.0;

    /// <summary>#619 · Where a captain stands to read a chamber's first way out: the doorway's midpoint,
    /// stepped <see cref="WeldedRefugeStandOffDu"/> back out of the room along the line from its centre.
    ///
    /// <para>On the welded refuge that is the only place the press CAN be, because the doorway itself is a
    /// wall. On a refuge whose door cycles nothing reads it — and it is computed all the same rather than
    /// left at zero, because a field that is a lie on most rows is a field the next hand reads off the wrong
    /// row.</para>
    ///
    /// <para>Falls back to the room's own centre for a chamber with no recorded doorway, which the generator
    /// does not produce and which is not worth a second kind of answer.</para></summary>
    private static (double X, double Y) WayIn(in Room room)
    {
        if (room.Ways.Count == 0)
        {
            return (room.X, room.Y);
        }

        double mx = (room.Ways[0].X1 + room.Ways[0].X2) / 2;
        double my = (room.Ways[0].Y1 + room.Ways[0].Y2) / 2;
        double dx = mx - room.X, dy = my - room.Y;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        return len < 1e-9
            ? (mx, my)
            : (mx + (dx / len * WeldedRefugeStandOffDu), my + (dy / len * WeldedRefugeStandOffDu));
    }

    /// <summary>What the console inside is called.</summary>
    public const string RefugeTankLabel = "🫁 REFUGE RACK";

    /// <summary>What the plate over the door says at signage size — short enough to read at a run, because
    /// that is how it will be read.
    ///
    /// <para>It names the ROOM, not the floor, and that word is load-bearing (#612). The plate by the lift
    /// is simultaneously shouting NO ATMOSPHERE about the level; a sign forty du away reading only AIR
    /// would be a second instrument appearing to contradict the first, which is the one thing #612 says is
    /// worse than saying nothing. <c>REFUGE ·</c> makes the scope of the claim part of the claim.</para></summary>
    public const string RefugeGlyph = "🫁 REFUGE · AIR";

    /// <summary>#619 · The plate on the one room in the game that is out of service. Authored canon
    /// (2026-09-20), verbatim: <c>REFUGE — OUT OF SERVICE — REPORTED</c>, behind the family's own glyph that
    /// every other string in this file already wears.
    ///
    /// <para>It is the INSPECTORATE'S VOICE and it is entirely functional — the register of a form, not of a
    /// story. Three flat words a clerk would use, and the third of them is the only one doing any work:
    /// <i>REPORTED</i> says a notice went somewhere, and says nothing whatever about what was reported, who
    /// read it, or whether anybody came. Canon §13.8 at the one door in the building where the temptation to
    /// explain is worst.</para>
    ///
    /// <para>It replaces the old failed plate, which was the stencil with the word AIR quietly absent
    /// (<c>PRESSURE REFUGE</c>). That was a good tell for a seal that had perished and a bad one for a room
    /// somebody shut on purpose: it read as neglect, and #619's whole point is that this did not fail from
    /// age. It is also the sign on the WELD — <see cref="CarveRefuges"/> hands it to every
    /// <see cref="LockedDoor"/> it lays across that chamber's ways — so the plate over the door and the plate
    /// on the door are one string and cannot come to two accounts of one room.</para></summary>
    public const string RefugeFailedGlyph = "🫁 REFUGE — OUT OF SERVICE — REPORTED";

    /// <summary>#619 · Is this lock the weld on the refuge that failed? Asked by the renderer, which lets
    /// that door keep its wall and its leaf and takes its CONSOLE for itself, and by the hasp rule, which
    /// refuses to let a sentry shoot it. One predicate, so neither of them re-types the plate.</summary>
    public static bool IsTheWeldedRefugePlate(string sign) =>
        string.Equals(sign, RefugeFailedGlyph, StringComparison.Ordinal);

    /// <summary>#619 · What the instrument column says about the grey ring while one is on the fan.
    /// Authored canon (2026-09-20), verbatim — <c>refuge · dark</c> — behind the refuge family's glyph.
    ///
    /// <para>Lower case and two words, because it is a LEGEND and not an affordance: every other line in that
    /// column teaches a key, and this one teaches an ink. <i>dark</i> is the word the instrument would use
    /// about a lamp that is not lit, which is what the captain is looking at, and it promises nothing at
    /// all.</para></summary>
    public const string RefugeDarkCaption = "🫁 refuge · dark";

    /// <summary>#938 · THE PLATE ON A ROOM THAT HOLDS AND HAS NOTHING IN IT. Authored for the one
    /// line-needed marker #608 shipped with (2026-09-03), in the stencil grammar the other two speak.
    ///
    /// <para>The bug it closes: <see cref="RefugeGlyphFor"/> read the plate off a two-way test, so
    /// <see cref="RefugeState.Empty"/> — thirty-nine per cent of them — wore <see cref="RefugeGlyph"/> and
    /// went on saying AIR at range. That is the #612 fault at the worst possible door: the one word a
    /// captain crosses a dead floor for, printed over a rack whose fill line is empty and whose valve tag is
    /// dated years ago. The room is not a lie — it holds, and shelter is worth the walk — but AIR is.</para>
    ///
    /// <para>DRY is the whole correction, and it is one word because the plate is read at a run. It keeps
    /// <c>REFUGE ·</c> so the scope of the claim stays part of the claim; it does not become a warning,
    /// because the room still works; and it is a word about the RACK, which is the only thing the decades
    /// took. A captain who has read AIR on one floor and DRY on this one knows the difference before the
    /// walk, which is the same service the failed plate does by dropping the word altogether.</para></summary>
    public const string RefugeDryGlyph = "🫁 REFUGE · DRY";

    /// <summary>Which plate a refuge in this state wears. One place, so the deck plan and the tracker cannot
    /// come to disagree about what the room claims — and now three plates for three states, because a
    /// two-way test could only ever tell the captain which of them the room was NOT.</summary>
    public static string RefugeGlyphFor(RefugeState state) => state switch
    {
        RefugeState.Failed => RefugeFailedGlyph,
        RefugeState.Empty => RefugeDryGlyph,
        _ => RefugeGlyph,
    };

    /// <summary>#608 · What the lift panel prints on a floor whose plan carries a refuge. It says a refuge is
    /// THERE and never what state it is in — the plan is a drawing made when the building was new, and no
    /// drawing knows which compressors are still turning. Finding that out is the walk.</summary>
    public const string RefugeRowTag = "REFUGE";

    // ── #609 · THE ONE THING YOU MUST NOT MISS ──────────────────────────────────────────────────────────
    //
    // Owner, after suffocating on B2: "I thought there is air in the base?" ... "there should be a warning
    // or something :-D" ... "maybe pop-up about you have air or you are in vacuum type ... it is vital info"
    // ... "like the basement is more dangerous than the surface now :-D" ... "on surface there are emergency
    // shelters :-D"
    //
    // He is right on every count, and the last two are the argument. The surface gives a captain a visible
    // building to run to; a dead floor gives them a number they have to have been told. The rule itself is
    // good and stays exactly as it is — the top of each shaft band holds pressure and the rest costs air —
    // but it was being announced in a pulse that fades in eight seconds, between one about bench hardware
    // and one about dust.
    //
    // So the first dead floor of an excursion stops the world and says it properly, WITH THE ARITHMETIC:
    // which floors have air, how far the nearest one is, and how long the tank has. After that the pulse
    // line is enough, because by then it is knowledge rather than news.

    public const string VacuumArtUrl = "art/the-dead-air.jpg";

    public const string VacuumCardLabel = "🫁 DEAD AIR";

    /// <summary>What the first dead floor says. It states the rule and does the sum — a warning that makes
    /// the captain work out their own margin is a warning delivered too late.
    ///
    /// <para>#740 · And it does the sum in the SUIT'S units, off <see cref="SuitAir.Clock"/>, because the
    /// card and the gauge are describing one tank and a captain compares them by eye.</para></summary>
    public static string VacuumCard(string bodyId, int level, double airSeconds)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int band = BandOf(level);
        int refuge = BandTop(band);          // the top of this band always holds pressure
        int floorsUp = -level - -refuge;     // how many floors between here and breathable

        // #740 · THE CARD READS THE GAUGE, it does not do its own sum. This used to format the raw play
        // budget as minutes and seconds — "you have 21 min 01 s" — while the HUD two seconds later on the
        // same floor said AIR 8h09. Both sentences were about the tank and both were honest about the number
        // they held; they were simply holding it in different units, because the card was the one surface in
        // the game that had never gone through SuitAir. A captain cannot be expected to know which of two
        // instruments is quoting the designer's stopwatch, so there is now one clock and the card asks for it.
        //
        // …and the sentence OWNS the quantity: it names the instrument the figure came off, so that a captain
        // who glances at their wrist a second later reads the same characters back, and so that the next hand
        // to edit this copy cannot quietly re-derive the number from something else.
        string margin = airSeconds > 0
            ? $"Your gauge reads {SuitAir.Clock(airSeconds)}, and that is the figure it will go on counting " +
              "down the whole way up."
            : "Your gauge is already reading empty, which is its own instruction.";

        string upstairs = floorsUp == 0
            ? "this floor"
            : $"{floorsUp} floor{(floorsUp == 1 ? "" : "s")} up";

        return
            "The doors part on nothing.\n\n" +
            "No pressure, no lights but yours, and the dust has not been disturbed since it settled. You " +
            $"are {MetresDown(level):F0} m under the regolith and your tank is now the clock.\n\n" +
            "THE RULE, because it is the only one down here that can kill you: the TOP FLOOR OF EVERY SHAFT " +
            "BAND holds pressure. Nothing else does. That is where the lobbies were, and the fans on those " +
            "floors are still turning on somebody's account.\n\n" +
            $"The nearest floor of air is {NameOf(bodyId, refuge)} — {upstairs}. {margin}\n\n" +
            // #608 · AND THE OTHER HALF, now that it is true. This card used to end "there are no shelters
            // down here", which was honest when it was written and is now the most dangerous sentence in the
            // game: a captain who believes it will ration a tank they did not have to ration. Owner: "there
            // should be like at least one air replenish station in each of the airless labs underground...
            // for pure safety". So the card says where the exception is, and says the two things about it
            // that decide whether it is any use — it is not beside the lift, and the instrument finds it.
            "There is a PRESSURE REFUGE on this floor. Every vacuum floor in this building has one: staff " +
            "worked these levels in suits all day, and somebody with a clipboard made the owners pay for " +
            "somewhere to go when a tank ran short. It is not beside the lift — it never is — and your " +
            "tracker paints it as a ring like any shelter on the surface.\n\n" +
            // #608 · AND THE SENTENCE THAT STOPS THE CARD PROMISING SOMETHING THE BUILDING CANNOT KEEP.
            // The paragraph above is about what was BUILT and every word of it is still true. What the card
            // may not do is let a captain read "somewhere to go when a tank ran short" as "air, forty du
            // that way" — because a quarter of these rooms have air, some hold and have nothing, and some
            // will not open at all. The plan is a drawing; the seal is decades of nobody paying for it.
            "The plan marks a refuge on this band. Whether it still holds is not on the plan.";
    }

    /// <summary>Said on stepping out on the top floor — the lie that makes the rest work.</summary>
    public const string PressurisedLine =
        "🫁 The doors part on warm air and standing lights. Your suit stops drawing and the readout holds. " +
        "Somewhere a fan is still turning, on somebody's account, decades after the last invoice.";

    /// <summary>And on every floor below it.</summary>
    public const string DeadAirLine =
        "🫁 The doors part on nothing. No pressure, no lights but yours, and the dust on the floor has not " +
        "been disturbed since it settled. Your tank starts counting again. From here down, depth costs air.";

    /// <summary>What a locked door says when the captain tries it. It never opens, and the game never pretends
    /// it might — a door that teases is a puzzle, and this is meant to be a WALL with a world behind it.
    ///
    /// <para>#1074 · <b>One door in the building answers with what is POSTED on it instead</b>, because one
    /// door in the building has something posted on it: the order at the seal. The plate is the heading and
    /// the sentence under it is the order, verbatim (<see cref="StopOrder.OrderLine"/>) — which is what a
    /// stop order IS, a piece of paper somebody stuck to a door, and it is the whole of what the world ever
    /// says about the closure. Nothing is composed here: the two strings are set side by side and no third
    /// sentence explains either of them.</para></summary>
    public static string LockedLine(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return StopOrder.IsPlate(sign)
            ? $"🔒 {sign}. {StopOrder.OrderLine}"
            : $"🔒 {sign}. The lock is not a lock you can argue with — it is a decision somebody made, and "
                + "it is still being enforced by a building whose owners stopped answering a long time ago.";
    }

    /// <summary>#600 · How far under the regolith the shed's floor a given level sits, in metres.
    ///
    /// <para>Owner: <i>"we can use seriously large numbers there :-D ... or depths (in meters)"</i>. He is
    /// right that the depth is the better number — <c>B4</c> is an index and <c>−76 m</c> is a fact about
    /// where you are standing, and it is the one that makes the walk back up mean something.</para>
    ///
    /// <para>The first floor is far down because the facility is BURIED — the shed on the surface is a lid
    /// over a shaft, and the descent card earns that ("service lamps go past in the wall at first, then a
    /// rhythm, and you find you have been counting them and have lost count"). After that a floor is a
    /// floor plus its slab, its services and the rock somebody left between levels.</para>
    ///
    /// <para>Owner, reading the paint on B1: <i>"also we could make it deeper like 150 meters :-D"</i> — and
    /// he is right, 40 m was a car park. The overburden is the number that has to sell the lid, because it
    /// is the whole ride down before the first door opens, and the descent card has always described a shaft
    /// long enough to lose count in. At 150 m it does.</para></summary>
    public const double OverburdenMetres = 150.0;

    /// <summary>Floor to floor, including the slab and the rock between.</summary>
    public const double MetresPerFloor = 12.0;

    /// <summary>Metres below the surface for a level. 0 on the surface, positive going down.</summary>
    public static double MetresDown(int level) =>
        level >= 0 ? 0 : OverburdenMetres + ((-level - 1) * MetresPerFloor);

    /// <summary>What is painted on the wall beside the lift, big enough to read on the way past.</summary>
    public static string DepthPaint(int level) =>
        level >= 0 ? "SURFACE" : $"−{MetresDown(level):F0} m";
}
