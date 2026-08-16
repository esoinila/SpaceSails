using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #793 · THE PARK BENCH IS A GUMSHOE MOVE — sit, see who else stops, and work the case if the bench is all
/// yours.
///
/// <para>Owner, from play in the new park (<i>"I love the park"</i>), in three parts:</para>
///
/// <list type="number">
/// <item><b>The benches take the sit verb.</b> <i>"E at a steel bench seats you… a park bench under a
/// painted sky is the best breather in the base."</i> #790 shipped them as labelled fixtures; this wires
/// them to the seat machinery #778/#788/#799 already built, short rest and all.</item>
/// <item><b>The bench is a COUNTER-SURVEILLANCE instrument.</b> <i>"it is a good gumshoe move to see if
/// anyone is following us by foot, as they would need to stop moving also."</i></item>
/// <item><b>The bench processes the case — IF it is all yours.</b> <i>"if we get the whole bench to
/// ourselves to have some privacy."</i></item>
/// </list>
///
/// <h3>The same panel, on purpose</h3>
///
/// <para>A bench does not get a seated frame of its own. Sitting down is one posture with one answer
/// (<c>Map.Seated.CaptainIsSeated</c>), one docked strip, one WAIT beat and one short-rest ledger, and a
/// second seated panel beside those would be this repo's first named bug class aimed at a chair. What is
/// genuinely different about a bench is three facts, and they are three flags on the sitting rather than
/// three copies of it: which rung of the exposure ladder it is, what the room says when nobody comes, and
/// what somebody ARRIVING does — because on a plank they sit down beside you and start no conversation
/// at all.</para>
///
/// <h3>What is deliberately NOT here</h3>
///
/// <para><b>A tailing NPC.</b> Nothing in this game follows the captain yet, and inventing a watcher to
/// demonstrate a bench would be shipping a feature to justify a fixture. What ships is the SEAM: Core's
/// <see cref="FootTail"/> question, asked for real on every beat spent on a bench, answered <i>no</i> today
/// by every mover that exists (#804's rounds are laid before the captain arrives and cannot be tails), with
/// the hold law and its drawing written and guarded from both directions.</para>
/// </summary>
public partial class Map
{
    // ── THE TAIL-CHECK ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #793 · EVERYBODY ON FOOT, as the bench sees them.
    ///
    /// <para>Today that is #804's rounds and nothing else — built through
    /// <see cref="PatrolBeat.OnTheRound"/>, which stamps them as walkers on a PUBLISHED ROUND and therefore
    /// as movers that can never be tails. That is not this file being polite about guards: their beat is a
    /// pure function of (site, floor, watch) drawn before the captain stepped off the car, and a route that
    /// existed before you did is not a route that is about you.</para>
    ///
    /// <para>The list is built here, per ask, rather than kept as a field: it is one small allocation on a
    /// key press and it cannot go stale, which a cached copy of everybody's position certainly would.</para>
    /// </summary>
    private IReadOnlyList<FootTail.Mover> MoversAfoot()
    {
        // #870 lane 6′a · TheRoundOnFoot is the patrol family's own name for the men walking this floor —
        // a read-only view of the one list, asked for here rather than reached into.
        IReadOnlyList<Guard> round = TheRoundOnFoot;
        var afoot = new List<FootTail.Mover>(round.Count);
        for (int i = 0; i < round.Count; i++)
        {
            afoot.Add(PatrolBeat.OnTheRound(i, round[i].X, round[i].Y));
        }
        return afoot;
    }

    /// <summary>#793 · Is anything actually following the captain right now? Core's one predicate, asked with
    /// the captain's own position and the same sight blockers the guards' markers are drawn through.</summary>
    private bool AnythingTailingYou =>
        FootTail.AnythingTailing(_avatarX, _avatarY, MoversAfoot(), SightBlockers());

    /// <summary>
    /// #793 · WHAT SITTING STILL SHOWED YOU — one sentence, and never a meter.
    ///
    /// <para>#618's law, one instrument along: <i>"a cover that can blow, not a detection meter."</i> Nothing
    /// counts, nothing fills, nothing turns red. You sat down, you looked down the walk, and the park either
    /// handed you something or it did not — and either way it is a sentence a player has to read and decide
    /// about, which is the whole of the gumshoe register.</para>
    /// </summary>
    private string TheTailReading() => FootTail.Reading(AnythingTailingYou);

    // -- #870 lane 6c · THE FORWARDERS, AND EACH ONE HAS A CALLER OUTSIDE THIS FAMILY ------------------
    //
    // The bench's verbs live on Seating now (Seating.Bench.cs). These two are the spellings the rest of the
    // page still asks for by name, and they are the whole list — measured rather than assumed: the arm in
    // Map.Deck.Interact.cs presses [E] at a bench, and the ?park=1 row in Map.Surface.Cheats.cs walks the
    // captain down the gravel to a free one. Everything else the bench does is reached from inside the seat.

    /// <inheritdoc cref="Seating.TryTakeBench"/>
    private bool TryTakeBench() => _seating.TryTakeBench();

    /// <inheritdoc cref="Seating.SitOnAFreeBenchIfAsked"/>
    private bool SitOnAFreeBenchIfAsked(in UndergroundComplex.Park green) =>
        _seating.SitOnAFreeBenchIfAsked(in green);
}
