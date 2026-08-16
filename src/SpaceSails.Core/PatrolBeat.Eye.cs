using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: what is known and by whom — #832's retired-cop law and the eye's edge that is not a cliff (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── WHAT IS KNOWN, AND BY WHOM ────────────────────────────────────────────────────────────────────
    //
    // Owner: "we should not know their movements like 100 meters out and them need to see us like really
    // close to register our existence."
    //
    // Four bands, and a captain meets them in this order as a round comes down a corridor:
    //
    //   1. THE FAN hears it, through the rock, as a smudge with a bearing on it. (#591's tracker, untouched:
    //      a guard walks, so a guard is a contact. Nothing here special-cases the instrument.)
    //   2. THE EAR hears it — boots, out of sight, unhurried. (EarshotDu, and deliberately NOT wall-aware:
    //      sound goes round corners, which is why the noise line is the warning and the marker is not.)
    //   3. THE EYE sees it — and only then is a marker drawn on the deck. (MarkerSightDu + line of sight.)
    //   4. THEY see YOU. (NoticeDu + line of sight — a third of the eye's reach, because they are doing a
    //      job and you are watching a corridor.)
    //
    // The asymmetry between 3 and 4 IS the feature. If they saw you the moment you saw them there would be
    // nothing to time, and "wait for them to pass" would not be a sentence anybody could act on.

    // ── #832 · THE RETIRED-COP LAW, which tunes every number in this block ────────────────────────────
    //
    // Owner, filing the whole detection stack: "Also the guard looks like retired cop, not ninja, let's make
    // them easy to detect :-D" — and then the consequence, stated as a rule: "A challenge should essentially
    // NEVER feel like an ambush. If a captain is surprised by a guard, the captain was not looking, and the
    // instruments can prove it."
    //
    // THE ASYMMETRY IS THE DESIGN. A payroll guard is the loudest thing on a floor: heavy tread, keys, a
    // radio, a man who has not needed to surprise anybody in twenty years. The Old Ones are the quiet ones,
    // and a guard being scary-quiet steals their register and wastes his own. So a guard gets the GENEROUS
    // end of every instrument here, and the difficulty of the stealth game lives in the round's coverage and
    // in the book's memory — never in the guard's stealth. (The one exception the owner carved is a class
    // that does not exist yet: "no more ninja guards :-D ... those only maybe at the most sensitive levels".
    // When it is built it is a SECOND class with its own numbers, not a nudge to these.)

    /// <summary>How far the captain's own eye reaches for a guard's marker to be drawn at all. A corridor's
    /// length: far enough to watch a round work, nowhere near the hundred metres the owner ruled out. The
    /// last stretch of it is a smear rather than a marker — see <see cref="SmearFromFraction"/>.</summary>
    public const double MarkerSightDu = 30.0;

    /// <summary>How close a guard has to be before they register that somebody is standing there. A third of
    /// the eye's reach, and the whole of the timing window. FLAGGED for the owner's tuning.</summary>
    public const double NoticeDu = 9.0;

    /// <summary>
    /// How far the boots carry — AS FAR AS THE EYE ITSELF, by the retired-cop law above.
    ///
    /// <para>It was 18 du, deliberately short of the marker, and that left a band nobody meant to build:
    /// between 18 and 30 du a guard on the other side of a wall was neither drawn nor heard, which is
    /// #832's <i>"at no range may a walking man be neither seen nor heard while the instruments claim to be
    /// working"</i>. Raised to the eye's own reach so the ladder closes: anything the eye could have shown
    /// you, the ear will tell you about when a wall is in the way.</para>
    ///
    /// <para>It does not overtake the marker, because <see cref="Heard"/> is silent about anybody you can
    /// actually see — the line is a warning about a corridor you cannot look into, never a narrator for one
    /// you are looking at.</para>
    /// </summary>
    public const double EarshotDu = MarkerSightDu;

    /// <summary>#830 · WHAT A GUARD RETURNS ON THE FAN WHEN HE IS NOT WALKING. Declared here, in Core, and
    /// read by the client's one accessor — so the register of a figure is a fact about the figure rather
    /// than a flag somebody remembered to pass. A man on a rota standing at a stop is the owner's own
    /// example of a thing that has NOT earned quiet: <i>"if the guard is still then it would be a blurry
    /// blob"</i>.</summary>
    public const MotionTracker.AtRest FanRegister = MotionTracker.AtRest.Restless;

    /// <summary>How long a captain stepping out of the car has before anybody looks up. Not the moon's
    /// twenty-second grace (<see cref="SurfaceArrival.SpotGraceSeconds"/> is about a hull setting down and
    /// a deep full of Old Ones) — this is a lift door opening on a working floor, and four seconds is the
    /// beat it takes to read the plate and decide.</summary>
    public const double OffTheCarSeconds = 4.0;

    /// <summary>May anybody notice the captain yet? False for the first moments on a floor, whatever is in
    /// front of them.</summary>
    public static bool CanBeNoticed(double secondsOnThisFloor) =>
        !double.IsNaN(secondsOnThisFloor) && secondsOnThisFloor >= OffTheCarSeconds;

    /// <summary>
    /// THE ONE LOOK, read in both directions.
    ///
    /// <para>Clear line of sight over the walls, and inside <paramref name="reachDu"/>. It is one function
    /// with a range parameter rather than two functions, because the drawn marker and the guard's notice are
    /// the same question asked twice and a second copy of it is how they end up disagreeing — which on this
    /// ground would mean a guard challenging a captain who cannot see them, or standing visible three metres
    /// away and never looking up.</para>
    ///
    /// <para>The wall law is <see cref="SurfaceCollision.HasLineOfSight"/>, the same one the captain, the
    /// pack and the sweep team all obey.</para>
    /// </summary>
    public static bool EyesOn(
        double fromX, double fromY, double toX, double toY, double reachDu,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double dx = toX - fromX, dy = toY - fromY;
        if ((dx * dx) + (dy * dy) > reachDu * reachDu)
        {
            return false;
        }
        return SurfaceCollision.HasLineOfSight(fromX, fromY, toX, toY, walls);
    }

    // ── #832 · THE EYE'S EDGE IS NOT A CLIFF ──────────────────────────────────────────────────────────
    //
    // Owner, watching a guard walk a straight, open corridor: "Now the guard just vanishes into thin air ..
    // that is like huge magic trick".
    //
    // MarkerSightDu was a hard cutoff, and the circle it cuts on is invisible to the player. Inside it, a
    // crisp marker with a round number over it; one deck unit outside, nothing at all, in open air, with no
    // wall to blame. Popping at a WALL reads as physics — that is what a wall is for, and it stays exactly
    // as it was. Popping in the open reads as the building cheating.
    //
    // A person does not vanish at a range; they stop being resolvable. So the last fifth of the eye's reach
    // is a DISTANT FIGURE: a silhouette with no plate and no round number on it — somebody is down there,
    // and you cannot yet say who or which. It is the same idiom as #830's fan blob and the tracker's own
    // smudge: when the instrument is unsure, it says so by drawing less, never by drawing nothing.

    /// <summary>#832 · WHAT THE CAPTAIN CAN MAKE OF A GUARD AT THIS RANGE. Ordered, so a test can assert
    /// that the ladder never skips a rung — the whole failure was a jump from the top of it to the
    /// bottom.</summary>
    public enum Sighting
    {
        /// <summary>Nothing on the deck. Past the eye's reach, or behind a wall.</summary>
        None = 0,

        /// <summary>A distant figure: the silhouette, without the plate or the round's number. You know
        /// somebody is at the far end of the corridor; you do not know which round it is.</summary>
        Smear = 1,

        /// <summary>The marker as it has always been drawn — body, facing and the round's number.</summary>
        Plain = 2,
    }

    /// <summary>#832 · Where along the eye's reach a marker stops being a marker and becomes a distant
    /// figure. The outer fifth, which is far enough down a corridor that "I cannot tell yet" is the truth
    /// and close enough that it is not most of the feature. FLAGGED for the owner's tuning — and by the
    /// retired-cop law it may only ever move OUTWARD for a guard, never in.</summary>
    public const double SmearFromFraction = 0.8;

    /// <summary>The range at which the figure starts to go, in deck units.</summary>
    public static double SmearFromDu => MarkerSightDu * SmearFromFraction;

    /// <summary>
    /// #832 · The one ranging question, answered in three rungs. Wall occlusion still cuts to
    /// <see cref="Sighting.None"/> instantly and without a smear tier: a body stepping behind shotcrete IS
    /// gone, that is physics, and softening it would take back the cover the whole timing game is played
    /// against.
    /// </summary>
    public static Sighting SightingFor(
        double captainX, double captainY, double guardX, double guardY,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        if (!EyesOn(captainX, captainY, guardX, guardY, MarkerSightDu, walls))
        {
            return Sighting.None;
        }
        double dx = guardX - captainX, dy = guardY - captainY;
        return (dx * dx) + (dy * dy) >= SmearFromDu * SmearFromDu ? Sighting.Smear : Sighting.Plain;
    }

    /// <summary>Is this guard on the captain's deck AT ALL? The captain's eye, at the eye's reach — the same
    /// one question <see cref="SightingFor"/> answers, so the marker and the smear can never be gated by two
    /// different predicates.</summary>
    public static bool DrawnFor(
        double captainX, double captainY, double guardX, double guardY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        SightingFor(captainX, captainY, guardX, guardY, walls) != Sighting.None;

    /// <summary>Has this guard registered that somebody is standing there? Their eye, at their own much
    /// shorter reach — the same predicate, the other way round.</summary>
    public static bool Notices(
        double guardX, double guardY, double captainX, double captainY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        EyesOn(guardX, guardY, captainX, captainY, NoticeDu, walls);
}
