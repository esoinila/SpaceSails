using System;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #719 slice 2 · THE MAINTENANCE BREAK ─────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-05: "what if the elevator needs maintenance break :-)" / "just stopping the elevator by
    // remote of radio message would stop all escape way too easy :-d" / "going up would use more air".
    //
    // THE ORDERING LAW IS MET AND THIS FILE IS WHERE IT IS SPENT. #1115 shipped the stair — a second means of
    // escape at the far blind end of every listed complex, a door on every declared floor, and a climb the
    // tank pays for — so the car may now be taken away. Until that shipped, one radio call would have ended
    // every escape in the game; a break added before a second exit is a softlock with atmosphere.
    //
    // THE TRIGGER IS A PERSON, NOT A DIE. There is no roll in this file and no new provocation beside it. The
    // challenge that has been on these floors since #804 already has failing outcomes — a wallet that cannot
    // answer, and a run — and #602's third miss on the keypad summons the same man to the same read. When one
    // of those goes against the captain, the man who is already down here says so into the radio he was
    // already carrying, and the car stops. Deterministic, because it is the OUTCOME and not a chance: a
    // captain who was read and refused knows exactly what they are paying for.
    //
    // AND IT MAY ONLY HAPPEN WHERE THE STAIR IS. ACallCanStopTheCarOn is HasStairOn with the wrecks taken
    // out, which is the ordering law written as arithmetic rather than as a promise: the one condition under
    // which the car may be stopped is the one under which a second way out is cut. The band nobody listed
    // (#592) and the halls nobody dug (#677) have no stair, so nothing can stop the car down there, and the
    // #600 scar — an audit proves you can REACH the lift, never that it is a way HOME — is respected by the
    // break being unable to reach a floor with one exit on it.

    /// <summary>
    /// #719 slice 2 · <b>WHAT THE PANEL SAYS INSTEAD OF ITS FLOORS.</b> The only new string this slice has,
    /// and it is a plate rather than a sentence for the reason every other plate down here is one: the
    /// building labels its own fabric and never narrates it.
    ///
    /// <para>Nothing on it explains who called it, and that is the whole of the register (§13.8). A captain
    /// who has just been read and refused, and who walks back to the car to find the floor list gone, is
    /// left to draw the one inference the game will never confirm for them. The word MAINTENANCE is the
    /// building's own lie and it is a perfectly ordinary one: this is what a stopped car says in every
    /// building anybody has ever stood in.</para>
    ///
    /// <para>It wears the cars' vocabulary on purpose — unlike <see cref="StairSign"/>, which may not — and
    /// for the identical reason. A plate that says CAR is a sign reporting the machine that IS there, and
    /// the machine is exactly what has stopped.</para>
    /// </summary>
    public const string CarStoppedPlate = "CAR STOPPED · MAINTENANCE";

    /// <summary>
    /// #719 slice 2 · <b>MAY A RADIO CALL STOP THE CAR ON THIS FLOOR AT ALL?</b> — asked before anything is
    /// stored, so that the ordering law is a condition rather than a convention.
    ///
    /// <para>It is <see cref="HasStairOn"/>, minus a hull. That equality is the point: the floors a break may
    /// happen on and the floors a second way out is cut on are ONE list, so a break can never be applied to a
    /// floor whose only exit is the thing it is taking away. Never on the surface (<c>level &lt; 0</c> is
    /// inside <see cref="HasStairOn"/>), never below the listed bottom, and never on a wreck — a hull has no
    /// complex, no cage, no rota and nobody to radio.</para>
    /// </summary>
    public static bool ACallCanStopTheCarOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return !Derelict.TryParseWreckId(bodyId, out _) && HasStairOn(bodyId, level);
    }

    // ── THE WAY HOME, ONCE THE FREE ONE IS GONE ─────────────────────────────────────────────────────────
    //
    // #573's oldest law, said about a second exit: the ring on the fan and the number on the readout must be
    // about ONE journey. So the spot below is published rather than typed at each instrument — the fan paints
    // it and the tank measures to it, off one function, and the day it moves they both move.

    /// <summary>#719 · The stair's door as the fan and the tank both see it — a pace clear of the spine on
    /// the pocket's own side, which is where a captain actually stands to press it. Null on ground that will
    /// not take a stair.</summary>
    public static (double X, double Y)? StairRingAt(in SurfaceLayout.Field field) =>
        StairOn(field) is { } stair ? (stair.Landing.X, stair.Landing.Y + CorridorHalf + 1.5) : null;

    //
    // #1115 left this as its one flagged judgement call: the stair's price is real and the readout did not
    // quote it, because underground the fan's HOME ring is the CAGE and TheWayBackIsAlwaysOnTheFanTests holds
    // the law that the ring and the readout measure ONE journey. Quoting a climb as "the walk home" while a
    // free ride is standing in the corridor would have been two instruments disagreeing about home.
    //
    // The break is what settles it, and it settles it the right way round. With the car stopped there is no
    // free ride to disagree with: the ring moves to the stair door, and the way home becomes the walk to that
    // door and then the climb. So the law holds BY the break rather than being broken by it, which is why the
    // arithmetic waited for this lane.
    //
    // TWO LEGS, ONE CURRENCY. The readout takes a DISTANCE and prices it at SuitAir.WalkHomeSeconds, so the
    // climb has to arrive here in deck units rather than in seconds — and a climb is not walked, it is
    // hauled, which is a different rate. So the climb leg is not re-derived here at all: it is
    // ClimbAirSeconds — the very seconds ClimbTheStairOut takes out of the tank, heavy-labour band and all —
    // run back through WalkHomeSeconds' own arithmetic into the units the readout speaks. The number the
    // suit quotes and the number the press charges are then the same number by construction, and there is no
    // second author of the climb's price to drift away from the first.

    /// <summary>
    /// #719 slice 2 · <b>THE WAY HOME WITH THE CAR STOPPED</b>, in deck units, from where the captain is
    /// standing: the walk across the floor to the stair's door, and then the climb itself — taken as
    /// <see cref="ClimbAirSeconds"/> and converted back into distance at <see cref="SuitAir.WalkSpeedDu"/>,
    /// so that pricing this whole distance at <see cref="SuitAir.WalkHomeSeconds"/> yields exactly the walk
    /// plus exactly what the press charges.
    ///
    /// <para>A DISTANCE and never a coordinate, exactly like the surface's own way home (#453: depth is not
    /// a danger gradient) — and a straight line to the door rather than a route, because that is what the
    /// fan's ring is too. The instrument that says WHICH WAY and the instrument that says WHAT IT COSTS are
    /// both answering about the same journey, and neither of them has ever claimed to know the corridors.
    /// </para>
    ///
    /// <para>Zero climb on the surface, and zero everywhere the stair is not cut, in which case this is the
    /// bare walk to the doorstep — but nothing may stop the car on such a floor
    /// (<see cref="ACallCanStopTheCarOn"/>), so that arm is a safety and not a case.</para>
    /// </summary>
    public static double WayOutByStairDu(
        in SurfaceLayout.Field field, int level, double fromX, double fromY)
    {
        double walk = 0.0;
        if (StairRingAt(field) is { } door)
        {
            double dx = door.X - fromX, dy = door.Y - fromY;
            walk = Math.Sqrt((dx * dx) + (dy * dy));
        }
        return walk + (ClimbAirSeconds(field, level) * SuitAir.WalkSpeedDu);
    }
}
