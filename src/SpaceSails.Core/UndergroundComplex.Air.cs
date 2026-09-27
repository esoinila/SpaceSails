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
}
