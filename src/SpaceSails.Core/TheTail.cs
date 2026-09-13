using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1062 slice 1 · <b>THE TAIL</b> — following somebody who must not notice you.
///
/// <para>Owner, 2026-09-01, verbatim: <i>"following one of the customers covertly without them noticing us
/// would be classic spy / detective stuff :-D Maybe file that as plot possibility of some kind :-D … or
/// trying to lose a tail our selves :-D"</i></para>
///
/// <para>#1062 filed the whole idea away and said what it would be made of: <i>"the tail is nothing but
/// those two mechanics pointed at each other"</i> — the walker floor and the notice ladder. This file is
/// the first slice of it, built because #1199 needs somewhere to tail somebody TO. The mirror half (losing
/// the tail that is behind YOU) already has its seam in <see cref="FootTail"/> and is untouched here.</para>
///
/// <h3>What this file is, and what it deliberately is not</h3>
/// <list type="bullet">
/// <item><b>Not a pathfinder.</b> The person of interest walks on the one walker the game already has
/// (<see cref="Interior.NpcWalk"/> over <c>AutoWalk</c>'s route). Nothing here plots anything.</item>
/// <item><b>Not a roll.</b> "Have they noticed you" is priced by the observation roll the game already
/// casts for being seen at a distance with walls in the way — <see cref="ReeverObservation"/>, #436's own
/// rule, called and never re-derived. Its modifier stack is already the right stack: close is worse,
/// standing still is cover, moving is a gift. A second sightline arithmetic would be two rules disagreeing
/// about one corridor.</item>
/// <item><b>Not a sightline.</b> The walls are <c>SurfaceCollision.HasLineOfSight</c>'s, asked by the
/// caller, as everywhere else. Geometry is permission to roll, never knowledge.</item>
/// <item><b>Not a scene.</b> A noticed tail raises no card, says no line and pulses nothing. The person
/// stops, turns, and waits until you pass. The inference IS the telling (#1062: <i>"a noticed tail doesn't
/// confront you, they simply stop going where they were going"</i>).</item>
/// </list>
///
/// <para>Pure and deterministic, seeded off the one shared <see cref="DiceRule"/>.</para>
/// </summary>
public static class TheTail
{
    /// <summary>
    /// #1062 · <b>WHO THE PERSON OF INTEREST IS AT THIS HAVEN</b> — one of the bar's own named regulars,
    /// deterministic per universe, and never a name this file invents.
    ///
    /// <para><b>Why a regular and not a watcher's man.</b> #1074's first law is binding: <i>"the enforcer is
    /// always an OFFICE, never a name … the game never mints a villain official."</i> There is no named
    /// watcher in this game and there is not going to be one, so the only cast a tail may draw from is the
    /// cast the room already has — <see cref="Interior.PatronRota.Roster"/>, the four the bar consoles, the
    /// contact sheets and the quest givers are all keyed on. That is the better answer anyway: a stranger
    /// who vanishes is a plot device, and somebody the captain has bought a drink for is a problem.</para>
    ///
    /// <para><b>How the one is chosen.</b> The regular the rota is MOST LIKELY to have in this room — the
    /// highest <see cref="Interior.PatronRota.Affinity"/> at this station — with ties broken by a seeded
    /// roll on the body id. Highest affinity and not a free roll, because the tail is worth nothing if the
    /// person it names is hardly ever in the room to be followed: the world already publishes who haunts
    /// which port, and picking anybody else would be this file having an opinion the rota has already
    /// settled. Pure in the body id, so the same universe always names the same person and two captains
    /// comparing notes agree.</para>
    /// </summary>
    public static string ThePersonOfInterest(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        IReadOnlyList<string> roster = Interior.PatronRota.Roster;
        string chosen = roster[0];
        double best = double.NegativeInfinity;
        foreach (string regular in roster)
        {
            double weight = Interior.PatronRota.Affinity(regular, bodyId);
            if (weight > best)
            {
                best = weight;
                chosen = regular;
                continue;
            }

            // A tie is broken by the world and not by the order somebody typed the roster in — otherwise the
            // first name in a list would be the person of interest at every port with no published bias, and
            // a cast of four would read as a cast of one.
            if (weight == best
                && DiceRule.Roll(DiceRule.Seed($"tail:tiebreak:{bodyId}:{regular}"), DiceRule.D20).Face
                   > DiceRule.Roll(DiceRule.Seed($"tail:tiebreak:{bodyId}:{chosen}"), DiceRule.D20).Face)
            {
                chosen = regular;
            }
        }

        return chosen;
    }

    /// <summary>#1062 · This person's own stable seed for the look cadence — the body and the name folded
    /// together, and never their index in a walker list. The list of feet in a room is mutated under the sim
    /// (people land and are taken off it every frame), so an index is not an identity: seeding on one would
    /// re-cast looks that had already been taken, which is this project's fourth named bug class wearing a
    /// coat. <see cref="ReeverObservation.LookSeed"/>'s own reasoning, one room over.</summary>
    public static ulong SeedFor(string bodyId, string name)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(name);
        return DiceRule.Seed($"tail:{bodyId}:{name}", 0);
    }

    /// <summary>
    /// #1062 · <b>HAVE THEY NOTICED YOU?</b> — the whole of the notice question, and it is
    /// <see cref="ReeverObservation.Look"/> with a person in front of it instead of an Old One.
    ///
    /// <para>Nothing is re-derived here and nothing is tuned. #436's rule already prices exactly the
    /// question a tail asks — <i>seen, at this range, moving or still, with these walls between</i> — on a
    /// cadence (<see cref="ReeverObservation.LookIntervalSeconds"/>) that makes a dash across a gap
    /// genuinely different from standing in the open, which is the entire play of a tail. Re-stating it
    /// would be two rules about one corridor, and the day one of them is tuned they are two corridors.</para>
    ///
    /// <para><b>What the captain is DOING is <see cref="ReeverObservation.Doing.Nothing"/>, always.</b> The
    /// louder entries in that enum are all things a captain does on a moon in a vacuum suit — hauling a
    /// chest, digging, standing beside a firing sentry — and none of them is available to somebody walking
    /// a concourse. Passing one would be handing the roll a signal the world cannot produce here, which is
    /// this repository's fifth named bug class: a modifier nothing can exercise.</para>
    ///
    /// <para><b>Noticed latches, exactly as FIXED does.</b> Somebody who has clocked you does not un-clock
    /// you by arithmetic on the next frame; they stop, they turn, and they wait until you have gone past.
    /// The latch is the rule's, not this file's — an already-noticed person is handed back with no die
    /// cast.</para>
    /// </summary>
    /// <param name="hasLineOfSight">Whether the walls leave them anything to see. The caller's answer, from
    /// the one sightline oracle, and never re-decided here.</param>
    /// <param name="alreadyNoticed">Whether they have already clocked the captain on this walk.</param>
    /// <param name="personSeed">Their own stable seed — <see cref="SeedFor"/>.</param>
    /// <param name="simSeconds">The sim second the look is taken at.</param>
    /// <param name="lastLookIndex">The index carried out of the previous call.</param>
    /// <param name="rangeDu">How far behind them the captain is, in deck units.</param>
    /// <param name="captainSpeedDu">How fast the captain is moving, in deck units per second.</param>
    public static ReeverObservation.Glance Notice(
        bool hasLineOfSight,
        bool alreadyNoticed,
        ulong personSeed,
        double simSeconds,
        long lastLookIndex,
        double rangeDu,
        double captainSpeedDu) =>
        ReeverObservation.Look(
            hasLineOfSight, alreadyNoticed, personSeed, simSeconds, lastLookIndex,
            new ReeverObservation.View(rangeDu, captainSpeedDu, ReeverObservation.Doing.Nothing));

    /// <summary>#1062 · Has this look clocked you? The rule's own latch, read through one name so a caller
    /// never has to know that the eye that prices a corridor was written for a moon.</summary>
    public static bool HasNoticed(in ReeverObservation.Glance glance) => glance.IsFixed;

    /// <summary>#1062 · <b>THE TAIL AUTHORS NOTHING.</b> A noticed tail raises no card, says no line and
    /// files no note — the person simply stops going where they were going, and the captain is left to work
    /// out why. There is no prose in this file to sweep, which is the strongest form §13.8 comes in.</summary>
    public static IEnumerable<string> AllProse() => [];
}
