using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #793 · IS ANYBODY FOLLOWING YOU ON FOOT — the question a bench is for, and the seam that answers it.
///
/// <para>Owner, filing the park bench: <i>"it is a good gumshoe move to see if anyone is following us by
/// foot, as they would need to stop moving also."</i> That is the whole instrument. A captain who keeps
/// walking learns nothing, because everybody on a walk is walking. A captain who <b>sits down</b> takes the
/// motion out of the picture, and whatever is left moving is the park going about its evening — while a
/// figure that stops when you stop, twice, is an answer.</para>
///
/// <h3>The park's openness is the mechanic, not the scenery</h3>
///
/// <para>Owner, in the same breath: <i>"the same move at a hall table proves nothing — the hall gives
/// everyone a reason to sit."</i> So the reading is taken along a clear line of sight
/// (<see cref="PatrolBeat.EyesOn"/>, the game's one look) and never through a wall: a room full of chairs
/// and doorways answers the question with noise. The bench asks; the walls decide whether the answer is
/// worth anything.</para>
///
/// <h3>WHAT "TAILING" MEANS, stated so it cannot quietly grow</h3>
///
/// <para>This file will not <i>infer</i> a tail out of two positions and a heading. It could — and it would
/// be wrong: a guard walking the length of the corridor you happen to be walking is not following you, and
/// a detector that said so would fire on every round in the building and mean nothing (the fifth bug class,
/// pointed at a person). A tail is a fact about a mover's <b>route</b>, and the mover's owner is the only
/// one who has it:</para>
///
/// <list type="number">
/// <item><b><see cref="Mover.Tailing"/></b> — the mover's destination IS the captain. Declared by whoever
/// steers it, in the frame it is steered.</item>
/// <item><b><see cref="Mover.OnAPublishedRound"/></b> — the mover's route was drawn before the captain
/// arrived and does not read them (#804's beat is a function of site, floor and watch). You cannot be
/// following somebody down a line you were already walking, so this DISQUALIFIES, whatever clause one is
/// set to.</item>
/// </list>
///
/// <h3>Today the answer is no, and that is the honest shipped state</h3>
///
/// <para><b>Nothing in the game tails the captain yet.</b> The only movers with routes on these floors are
/// #804's patrol guards, and a round is the second clause's own example — <see cref="PatrolBeat.OnTheRound"/>
/// builds theirs and it can never be a tail. This file is the SEAM: the question exists, the bench asks it
/// every time it is sat on, the hold law is written, and the day a watcher is built it has one place to
/// declare itself rather than a bench growing a second opinion about who is behind you.</para>
///
/// <para>Pure: positions, walls and two bools in, one answer out. No clock, no world, no list of its own.</para>
/// </summary>
public static class FootTail
{
    /// <summary>
    /// Somebody on foot, as the bench sees them.
    /// </summary>
    /// <param name="Name">What they are called on the deck — for the reading and for a guard's message, never
    /// for a decision.</param>
    /// <param name="X">Where they are this frame.</param>
    /// <param name="Y">The same.</param>
    /// <param name="OnAPublishedRound">Their route was laid before the captain walked in and does not read
    /// them. A round, a rota, a walk to a fixed stop. DISQUALIFIES from tailing, by construction.</param>
    /// <param name="Tailing">Their destination is the CAPTAIN — declared by whoever steers them, never
    /// inferred from geometry by this file.</param>
    public readonly record struct Mover(
        string Name, double X, double Y, bool OnAPublishedRound = false, bool Tailing = false);

    /// <summary>A mover whose route the building drew: a beat, a rota, a walk to a fixed stop. The factory
    /// exists so that the disqualifying clause is set by the KIND of mover rather than remembered at each
    /// call site — a walker put on a published round can never be handed to this file as a tail.</summary>
    public static Mover OnARound(string name, double x, double y) =>
        new(name, x, y, OnAPublishedRound: true, Tailing: false);

    /// <summary>Is this one actually following the captain? Both clauses, and the round wins: a route that
    /// existed before you did is not a route that is about you.</summary>
    public static bool IsTailing(in Mover mover) => mover.Tailing && !mover.OnAPublishedRound;

    /// <summary>How far a foot-tail is legible from a bench. The captain's own eye and not a second one
    /// (<see cref="PatrolBeat.MarkerSightDu"/>): the range at which this game has decided a person is a
    /// person you can see, said once and read here. Two reaches for one pair of eyes is how an instrument
    /// comes to disagree with the picture it is drawn over.</summary>
    public const double LegibleDu = PatrolBeat.MarkerSightDu;

    /// <summary>
    /// IS ANYTHING TAILING THE CAPTAIN — the question the bench asks, in one call.
    ///
    /// <para>A tail counts only while it is in plain sight along an unbroken line: the reading is about what
    /// sitting still <i>reveals</i>, and something you cannot see has not been revealed by anything.</para>
    /// </summary>
    /// <param name="captainX">Where the captain is sitting.</param>
    /// <param name="captainY">The same.</param>
    /// <param name="afoot">Everybody on foot the caller knows about. Null or empty is a fine answer and the
    /// common one.</param>
    /// <param name="sight">The walls the look has to get past — the caller's own sight blockers.</param>
    public static bool AnythingTailing(
        double captainX, double captainY,
        IReadOnlyList<Mover>? afoot,
        IReadOnlyList<SurfaceCollision.Segment>? sight)
    {
        if (afoot is null)
        {
            return false;
        }
        foreach (Mover m in afoot)
        {
            if (InPlainSight(captainX, captainY, in m, sight) && IsTailing(in m))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Is this mover in the captain's plain sight from the bench? Range and line of sight, through
    /// the one look (<see cref="PatrolBeat.EyesOn"/>).</summary>
    public static bool InPlainSight(
        double captainX, double captainY, in Mover mover,
        IReadOnlyList<SurfaceCollision.Segment>? sight) =>
        PatrolBeat.EyesOn(captainX, captainY, mover.X, mover.Y, LegibleDu, sight);

    /// <summary>
    /// MUST THIS ONE HOLD? — the law the whole move is named after.
    ///
    /// <para>Owner: <i>"they would need to stop moving also."</i> A tail whose subject has sat down on a
    /// bench in the open has nowhere to be: walking on past you is the one thing a follower cannot do, so
    /// they stop, and the stop is the tell. It costs the captain a sit and it costs the tail their cover,
    /// which is the exchange.</para>
    ///
    /// <para>Both directions are in the one predicate on purpose. A captain on their FEET buys nothing —
    /// a tail walks while you walk — and a mover that is not a tail is never held, or the park would freeze
    /// every time anybody sat down and the tell would mean nothing (the fifth bug class again: a hold that
    /// selects everybody selects nobody).</para>
    /// </summary>
    /// <param name="captainIsSeatedInTheOpen">The captain is sitting on a bench, in a room with sight lines.
    /// The caller owns what that means; this owns what it BUYS.</param>
    /// <param name="captainX">Where they are sitting.</param>
    /// <param name="captainY">The same.</param>
    /// <param name="mover">The one being asked about.</param>
    /// <param name="sight">The walls between the two.</param>
    public static bool MustHold(
        bool captainIsSeatedInTheOpen, double captainX, double captainY, in Mover mover,
        IReadOnlyList<SurfaceCollision.Segment>? sight) =>
        captainIsSeatedInTheOpen
        && IsTailing(in mover)
        && InPlainSight(captainX, captainY, in mover, sight);

    /// <summary>Everybody who must hold, in the caller's own order — for the drawing, which has to mark
    /// each of them where they stopped.</summary>
    public static IReadOnlyList<Mover> Holding(
        bool captainIsSeatedInTheOpen, double captainX, double captainY,
        IReadOnlyList<Mover>? afoot,
        IReadOnlyList<SurfaceCollision.Segment>? sight)
    {
        if (afoot is null || !captainIsSeatedInTheOpen)
        {
            return [];
        }
        var held = new List<Mover>();
        foreach (Mover m in afoot)
        {
            if (MustHold(true, captainX, captainY, in m, sight))
            {
                held.Add(m);
            }
        }
        return held;
    }

    // ── WHAT THE BENCH SAYS ABOUT IT ──────────────────────────────────────────────────────────────────
    //
    // The reading is a SENTENCE and never a meter (#618's own constraint, one instrument along: "a cover
    // that can blow, not a detection meter"). Nothing counts, nothing fills, nothing goes red. You sat down,
    // you looked, and the park either handed you something or it did not.

    /// <summary>The clean reading, and it is deliberately not a reassurance: nobody stopping is evidence
    /// about this minute on this walk, which is all a gumshoe ever gets.
    ///
    /// <para>FOUND BY READING IT ON SCREEN. The first draft said <i>"two figures keep their pace"</i> — and
    /// the sim does not know how many figures are on that walk, or that there are any at all. A sentence
    /// that counts something nothing counts is #740's fault with a bench under it, and it would have read
    /// as a lie the first time somebody sat on an empty walk. This one owns exactly the fact the reading
    /// has: <b>nothing out there has stopped</b>.</para></summary>
    public const string NobodyStoppedLine =
        "Nobody stops when you do. The walk runs on past the beds and out the far gate with nothing on it "
        + "that has any reason to be standing still.";

    /// <summary>…and the reading that is worth the sit. It names WHAT WAS SEEN and draws no conclusion — the
    /// conclusion is the player's, which is the whole of the gumshoe register.</summary>
    public const string SomebodyStoppedLine =
        "Somebody down the walk stops when you stop. Not a bench, not a bed, not a plate to read — they are "
        + "simply standing on gravel, at the distance people stand at when they have run out of reasons to "
        + "be walking.";

    /// <summary>What the reading says this beat. One call so the two sentences can never be reached by two
    /// different questions.</summary>
    public static string Reading(bool anythingTailing) =>
        anythingTailing ? SomebodyStoppedLine : NobodyStoppedLine;

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return NobodyStoppedLine;
        yield return SomebodyStoppedLine;
    }
}
