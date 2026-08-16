using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: #833's approach, and the escort walked back to the car (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── #833 · THE APPROACH: HE COMES OVER, AND ONLY THEN DOES HE READ ────────────────────────────────
    //
    // Owner, filing the beat the challenge never had: "I think the guard should approach us when it does the
    // inspection." Until now the card went up on the frame Notices() fired — at up to NoticeDu, with the man
    // standing wherever the round had left him. A man reading your wallet from nine deck units off is
    // telepathy wearing a uniform.
    //
    // The choreography, and every rung of it is a fact this file owns rather than a client's timer:
    //
    //   1. NOTICE   — unchanged. He registers somebody standing there, at his own short reach.
    //   2. THE HAIL — he turns and says HailLine. One second of warning that the next thing is happening.
    //   3. THE WALK — he crosses to CardReachDu on his own A*/slide gait, live on the fan the whole way. The
    //                 captain's controls stay FREE: walking away is allowed, and is its own tell.
    //   4. THE READ — the card raises on ARRIVAL and nowhere else. Inspections happen where inspections
    //                 happen.

    /// <summary>#833 · How close a man has to be standing to take a pass out of your hand and look at it. An
    /// arm and a step: the distance two people talk at, and the distance the card may raise at. FLAGGED for
    /// the owner's tuning — and it may never be raised toward <see cref="NoticeDu"/> without the whole beat
    /// going back to being a magic trick.</summary>
    public const double CardReachDu = 2.0;

    /// <summary>#833 · Is he close enough to read it? The ONE question the card is gated on, asked here so
    /// the walk-up and the guard that proves it cannot be looking at two different numbers.</summary>
    public static bool AtCardReach(double guardX, double guardY, double captainX, double captainY)
    {
        double dx = captainX - guardX, dy = captainY - guardY;
        return (dx * dx) + (dy * dy) <= CardReachDu * CardReachDu;
    }

    /// <summary>#833 · How far the captain may get before a hail is abandoned. A stride or two past the reach
    /// he registered you at — a man who has said "hold on" does not give up because you took one step, and he
    /// does not follow you round the building either.
    ///
    /// <para>#835 · …the FIRST time. Walking away from a hail is still free, still allowed and still ends in
    /// <see cref="WalkedAwayLine"/>; it is doing it TWICE in one watch that is a
    /// <see cref="Provocation.WalkedAwayTwice"/>. This number is untouched by that: he still stops coming
    /// here, at the same range. What changed is what he does next.</para></summary>
    public const double GivesUpBeyondDu = NoticeDu + 4.0;

    /// <summary>#833 · The longest a walk-up may take before he thinks better of it. The belt on top of the
    /// braces: a captain circling a pillar out of arm's reach is walking away by any honest reading, and a
    /// guard must never be left crossing a corridor forever.</summary>
    public const double WalkUpSeconds = 20.0;

    /// <summary>#833 · Is he still coming? False the moment the captain is out past
    /// <see cref="GivesUpBeyondDu"/> or the walk-up has run past <see cref="WalkUpSeconds"/> — and the caller
    /// then simply puts him back on his round, which is the whole of the consequence in this phase.</summary>
    public static bool StillComing(
        double secondsWalkingUp, double guardX, double guardY, double captainX, double captainY)
    {
        if (double.IsNaN(secondsWalkingUp) || secondsWalkingUp > WalkUpSeconds)
        {
            return false;
        }
        double dx = captainX - guardX, dy = captainY - guardY;
        return (dx * dx) + (dy * dy) <= GivesUpBeyondDu * GivesUpBeyondDu;
    }

    /// <summary>#833 · How often a walk-up re-plans on the moving target the captain is. Not every frame: an
    /// A* is not free in WASM, and a man crossing a corridor does not re-decide his route sixty times a
    /// second either.</summary>
    public const double RePlanEverySeconds = 0.5;

    /// <summary>#833 · THE HAIL. Terse on purpose — the whole beat is the second of warning it buys, and a
    /// paragraph would spend that second. Nothing in it explains anything and nothing in it threatens.</summary>
    public const string HailLine =
        "👮 \"You there — hold on.\" He turns, tucks the clipboard under his arm, and starts walking over. " +
        "He is in no hurry at all.";

    /// <summary>#833 · What it looks like when a captain simply keeps walking. Allowed — the controls are
    /// never taken during the approach — and it is its own tell: the round does not follow, it writes.</summary>
    public const string WalkedAwayLine =
        "👮 He stops where he is, watches you go the length of the corridor, and writes something short on " +
        "the clipboard.";

    // ── #833 · THE ESCORT, WALKED ─────────────────────────────────────────────────────────────────────
    //
    // Owner, evening playtest 2026-08-11, four challenges on B2: "how did I jump to elevator there?" … "So
    // the guard walk me back to the car". The sentence said WALK and the sim did a placement — the
    // sentence-vs-sim bug class, verbatim, in the one feature whose entire register is procedure.
    //
    // So the numbers below exist to make EscortLine literally true: he plans a route to the car, the captain
    // is walked along at his shoulder through the captain's own collision, and neither of them is ever put
    // anywhere. Both are moving contacts on the fan the whole way, which is the one guaranteed long walk
    // beside a guard this game has.

    /// <summary>#833 · How far from him the captain is walked — half a pace back and half a pace to the
    /// side, which is where you end up beside somebody who is showing you out.</summary>
    public const double ShoulderDu = 1.3;

    /// <summary>#833 · How far the captain may lag before the guard WAITS. He is escorting, not racing: a man
    /// who walked off and left you would not be walking you anywhere. It is also what guarantees the pair
    /// arrive together rather than the escort ending with the captain still down the corridor.</summary>
    public const double TetherDu = 2.6;

    /// <summary>#833 · How much brisker than the guard the captain's own legs are worked so a lag closes
    /// rather than becoming permanent. Above one, and modest: this is somebody keeping up, not a tow.</summary>
    public const double CatchUpFactor = 1.6;

    /// <summary>#833 · Close enough to the car to BE at it — the end of the escort, measured on the captain,
    /// because the captain arriving is the thing the escort was about.</summary>
    public const double AtTheCarDu = 0.9;

    /// <summary>#833 · The whole escort's bound in seconds. Past it the walk is abandoned and the cut is
    /// ADMITTED (<see cref="EscortCutLine"/>) rather than narrated as a walk that did not happen.</summary>
    public const double EscortSecondsCap = 90.0;

    /// <summary>#833 · When the small talk lands, in seconds into the walk. Far enough in that it is
    /// something said on a walk rather than a line fired at a placement.</summary>
    public const double PumpsAfterSeconds = 2.5;

    /// <summary>#833 · The small talk that is the punishment's whole texture: a man so unbothered by you that
    /// he makes conversation about the plant while he walks you off his floor.</summary>
    public const string PumpsLine =
        "👮 \"They've had the pumps out on three since Tuesday,\" he says, to nobody in particular. \"Same " +
        "three.\"";

    /// <summary>#833 · The end of the walk, and the moment the captain has the controls back. A hand-back
    /// with no sentence on it is the sim doing something the prose never mentioned.</summary>
    public const string EscortDoneLine =
        "👮 The doors open on an empty car. He waits until you are in it, and then goes back to the round " +
        "without another word.";

    /// <summary>#833 · The one honest way to keep a jump-cut: SAY it is one. Used only when the ground
    /// refuses to give him a route to the car at all — the audit (§13.1) says that cannot happen on a floor
    /// this generator builds, and a guard is not the place to find out otherwise. The sentence may never
    /// claim a walk the sim did not take, so this one does not.</summary>
    public const string EscortCutLine =
        "👮 Next thing you know, you are at the lift. Whatever the walk back was, none of it stayed with you.";

    /// <summary>#833 · What he says to a captain who tries to steer while being walked off the floor. The
    /// controls ARE held for this stretch — the only stretch in the feature where they are — so the refusal
    /// has to be said, and it has to be said the way a man on a rota would say it.</summary>
    public const string EscortHeldLine = "👮 \"This way.\" He is walking you out, and he is walking you out.";

    /// <summary>Can the captain HEAR this one — close, and not visible? Range only, because sound goes round
    /// corners; and explicitly not when they are already drawn, because a line describing boots you can see
    /// is the picture and the sentence disagreeing.</summary>
    public static bool Heard(
        double captainX, double captainY, double guardX, double guardY,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double dx = guardX - captainX, dy = guardY - captainY;
        if ((dx * dx) + (dy * dy) > EarshotDu * EarshotDu)
        {
            return false;
        }
        return !DrawnFor(captainX, captainY, guardX, guardY, walls);
    }

    /// <summary>What a corridor you cannot see into sounds like when a round is in it. Said sparingly — it
    /// is a warning, not a narrator — and it names no direction, because the bearing is the fan's job and
    /// two instruments answering one question is how they come to disagree.</summary>
    public const string HeardLine =
        "👣 Boots on shotcrete, out of sight and in no hurry — the tread of somebody walking a line they " +
        "have walked all week.";
}
