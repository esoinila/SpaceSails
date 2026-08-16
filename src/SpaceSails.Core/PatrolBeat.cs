using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #804 · SOMEBODY IS WALKING THESE FLOORS, AND THE WALK IS A THING YOU CAN LEARN.
///
/// <para>Owner, 2026-08-09, filing the whole feature: <i>"the rotating guards on the lower more restricted
/// levels… Any how the guards using the A* to have a beat are missing."</i> And then the three sentences
/// that decide the shape of it:</para>
///
/// <list type="number">
/// <item><b>The verb is TIMING, not hiding.</b> <i>"ideally we could see them move and wait for them to pass
/// before we pass them."</i> A beat is only worth having if a captain can stand at the mouth of a rib,
/// watch a round go by, and step out behind it. That means the route is fixed for a watch, walkable, and
/// slower than the captain.</item>
/// <item><b>The knowing is asymmetric, in both directions.</b> <i>"Surely we should not know their movements
/// like 100 meters out and them need to see us like really close to register our existence. We need our
/// motion detector to warn us or that we hear a noise they make before they spot us."</i></item>
/// <item><b>Suspicion before pursuit.</b> <i>"a rolling guard has no reason to run after anyone just on
/// sight, they must suspect you do not belong there for some reason first."</i> So a sighting starts a
/// CHALLENGE — and that is still the DEFAULT and still the only thing a sighting on its own can ever
/// buy. See the #835 block below for the other branch, which the owner added later and deliberately,
/// and which no ambient round can reach.</item>
/// </list>
///
/// <h3>#835 · THE ONE LAW THAT WAS REVERSED, AND HOW MUCH OF IT SURVIVED</h3>
///
/// <para>This header used to say <i>"nothing in this file can start a chase"</i>. Owner, evening playtest
/// 2026-08-11: <i>"they need to catch us .... like reevers :-D we could use that code :-D"</i>. So there is
/// now a second branch — and it is a BRANCH, not a mood. <b>A guard who merely sees a captain still hails
/// and still walks over and still at worst walks you back to the car.</b> Pursuit needs one of exactly
/// three earned things (<see cref="Provocation"/>) and nothing else in the game can hand it one. The old
/// law is the default; the new branch is the exception; and being caught costs no health at all, ever
/// (<i>"just no damage by default :-D"</i>).</para>
///
/// <h3>What this is NOT</h3>
///
/// <para>It is not a detection meter and it is not a stealth level — #618's own canon constraint, written
/// before anybody had built this: <i>"The owner's ask is a cover that can blow, not a detection meter."</i>
/// There is no alert state, no alarm, no lockdown and no hunt. A guard who is satisfied walks on; a guard
/// who is not walks you back to the lift. The worst thing that happens on this floor is that somebody
/// writes your visit down.</para>
///
/// <h3>Where they are, and where they are deliberately not</h3>
///
/// <para><b>From the floor that breathes down to the last one the building admits to.</b> #709 put people on
/// B1 only and said why: the population falling to zero is what makes the descent a gradient. This does not
/// spend that. It walks the facility's WORKING floors — everything from the top pressurised floor
/// (<see cref="UndergroundComplex.TopPressurisedFloor"/>) to the last one the directory owns up to
/// (<c>DepthOf</c>) — which are exactly the floors that have a payroll to put somebody on.</para>
///
/// <para>#863 · <b>THE BAR FLOOR IS ONE OF THEM NOW.</b> It used to be the exception — the one working floor
/// nobody walked, on the grounds that a round would ask a room of contractors for passes under a plate
/// reading <c>NO PASS REQUIRED</c>. Owner, 2026-08-13: <i>"let's try to have round because the guard looks
/// kind of silly just standing in the middle of an aisle."</i> The plate is untouched and so is the descent:
/// what B1 gains is a man on a rota walking past people who are eating, which is the most ordinary thing in
/// the building and therefore the best possible floor to learn a round on.</para>
///
/// <para>Nobody patrols the unlisted band or the found halls. That is not an omission: the unlisted band is
/// the thing the clandestine operation was hiding <i>from its own staff</i> (§13.7), and a guard walking a
/// round down there would be the building telling on itself. Past the seam nothing belongs to anybody at
/// all. <b>The empty floors stay empty, and where the rounds stop is a fact a captain can read.</b></para>
///
/// <h3>The register</h3>
///
/// <para>A guard here is an EMPLOYEE. Bored, thorough, procedural, on a rota, halfway through a shift, and
/// wholly uninterested in you until the form says otherwise. Nothing one says, carries or does explains
/// anything (§13.8), and the closest any line comes to menace is a man being unhurried about paperwork.</para>
///
/// <para>Pure and deterministic, like everything else in Core: the beat is a function of (site, floor,
/// watch) and the floor plan, so the same round runs the same way every visit inside a shift and turns over
/// with the shift — which is how a captain learns one and how the building stays alive.</para>
/// </summary>
public static partial class PatrolBeat
{
    // ── WHICH FLOORS HAVE A ROUND ON THEM ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Does anybody walk a round on this floor?
    ///
    /// <para>Three clauses, and every one of them is somebody else's already-shipped ruling rather than a
    /// number typed here:</para>
    ///
    /// <list type="bullet">
    /// <item><b>Underground, and no shallower than the building breathes.</b>
    /// <see cref="UndergroundComplex.TopPressurisedFloor"/> is where the facility starts; the regolith over
    /// it is nobody's rota.</item>
    /// <item><b>Not deeper than the directory admits.</b> <c>DepthOf</c> is the building's account of
    /// itself, and a payroll stops where the account does.</item>
    /// <item><b>Never the head office.</b> #411's building has no gate, no shafts to card and a different
    /// fiction entirely; putting a contract guard in the wintering hall would be this file deciding
    /// something about a place it does not own.</item>
    /// </list>
    ///
    /// <h3>#863 · THE ONE CLAUSE THAT WAS REVERSED: B1 IS WALKED NOW</h3>
    ///
    /// <para>This used to read <c>level &gt;= bar</c> and the bullet above it used to say <i>below the
    /// bar</i>: the top pressurised floor — the one with the park, the canteen, the offices and the
    /// washrooms on it — was the one working floor in the building nobody walked, on the grounds that a
    /// round would ask a room of contractors for passes under a plate reading <c>NO PASS REQUIRED</c>.</para>
    ///
    /// <para><b>Owner ruling, 2026-08-13,</b> watching a man stand in a furnished aisle with nothing to do:
    /// <i>"let's try to have round because the guard looks kind of silly just standing in the middle of an
    /// aisle."</i> The plate is untouched and so is the argument behind it — a captain with no paper is
    /// still walked out and never harmed — but a guard with no beat is a statue, and a statue is worse
    /// storytelling than a man who wants to see your pass.</para>
    ///
    /// <para>And the cost that had been assumed was measured and is not there. Lab 45 (§C) flagged the
    /// irony first: the FURNITURE and the ROUNDS had never once stood on the same floor, so every
    /// heavy-floor sightline number in that lab was a what-if. Post-#860 — the WallIndex eye and the plan
    /// walked during the stand — the frame cost of a round on the densest floor in the game is
    /// near-nothing, so the reason to withhold it went and the reason to have it is on the screen.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor (negative; −1 is B1).</param>
    public static bool IsPatrolled(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (level >= 0 || UndergroundComplex.IsHeadOffice(bodyId))
        {
            return false;
        }

        // #863 · …and the top pressurised floor is INSIDE this now (`> bar`, not `>= bar`). The clause still
        // does its own work — a site whose upper floors do not breathe has no rota on them either — it just
        // no longer spends the floor the owner watched a guard stand still on.
        if (UndergroundComplex.TopPressurisedFloor(bodyId) is not { } bar || level > bar)
        {
            return false;
        }

        // Listed floors only. IsUnlisted/IsFound are the two bands the building denies having, and denying
        // a band is incompatible with rostering somebody to walk it.
        return level >= UndergroundComplex.DepthOf(bodyId);
    }

    // ── HOW MANY, AND WHO ─────────────────────────────────────────────────────────────────────────────

    /// <summary>The most who ever walk one floor at once. Two is a rota with a gap in it that a captain can
    /// time; three is a cordon, and a cordon is the stealth level #618 ruled against. FLAGGED for the
    /// owner's tuning.</summary>
    public const int MostOnAFloor = 2;

    /// <summary>How many are on the round on this floor this watch — one or two, seeded off (site, floor,
    /// watch) and nothing else. A floor with one is a floor you can walk behind; a floor with two is a floor
    /// where the gap is somewhere else. Nothing announces which you have walked into.</summary>
    public static int GuardsOn(string bodyId, int level, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsPatrolled(bodyId, level)
            ? DiceRule.Roll(DiceRule.Seed($"hive:patrol:heads:{bodyId}:{level}", watch), MostOnAFloor).Face
            : 0;
    }

    /// <summary>What is drawn over a guard on the deck. The ROUND, not the person: a rota numbers rounds,
    /// and nobody down here is going to introduce themselves. Kept short because it is drawn in 8px
    /// monospace over a moving dot.</summary>
    public static string DeckName(int index) => $"PATROL {index + 1}";

    /// <summary>
    /// #793 · A GUARD, AS A BENCH SEES THEM — and the answer is always the same: NOT A TAIL.
    ///
    /// <para>A round is <see cref="BeatFor"/>'s stop list, a pure function of (site, floor, watch) and the
    /// floor plan. It is laid before the captain steps off the car and it does not read them, which is
    /// precisely <see cref="FootTail.Mover.OnAPublishedRound"/> — so #793's counter-surveillance question is
    /// answered <b>no</b> by every guard in the building by construction, rather than by a clause somebody
    /// remembered to write at a call site.</para>
    ///
    /// <para>That is a LAW and not a gap. A round that started following people would not be a round: the
    /// whole of this file is somebody doing a job on a rota, and a captain who sits down on a bench and
    /// watches a patrol keep its pace has learned something true about the building.</para>
    /// </summary>
    public static FootTail.Mover OnTheRound(int index, double x, double y) =>
        FootTail.OnARound(DeckName(index), x, y);

    /// <summary>Is this deck name one of ours? The renderer's ink gate, and a single answer so a fourth kind
    /// of figure on the deck cannot be told apart by a string literal typed twice.</summary>
    public static bool IsGuardName(string? name) =>
        name is not null && name.StartsWith("PATROL ", StringComparison.Ordinal);

    /// <summary>
    /// Who is walking it — the plate that stands over them when the round stops at you.
    ///
    /// <para>The register test (#701, #709) applies at full strength: not one of them is interesting.
    /// A guard who read as a threat would turn the working floors into a stealth level, and a guard who read
    /// as a character would make the round a quest.</para>
    /// </summary>
    private static readonly string[] Plates =
    [
        "◈ A CONTRACT GUARD, WALKING THE ROUND",
        "◈ A SITE WARDEN, HALFWAY THROUGH A SHIFT",
        "◈ SOMEBODY ON THE SECURITY ROTA, NOT LOOKING FOR YOU",
        "◈ A NIGHT MAN DOING THE DAY MAN'S HOURS",
    ];

    /// <summary>How many plates are authored. Public so a guard can pin the catalog's size without reaching
    /// into it.</summary>
    public static int PlateCount => Plates.Length;

    /// <summary>Which of them this round is. Seeded on (site, floor, watch, index), so the face on the round
    /// turns over with the shift exactly as the canteen's does.</summary>
    public static string PlateOf(string bodyId, int level, long watch, int index)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return Plates[DiceRule.Roll(
            DiceRule.Seed($"hive:patrol:who:{bodyId}:{level}:{index}", watch), Plates.Length).Face - 1];
    }

    // ── THE BEAT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One stop on a round: where it is, and what the guard is standing at when they are there.
    /// The name is diagnostic — it is what a red audit prints — and never shown to a player.</summary>
    /// <param name="X">Deck units, in the surface's own coordinates.</param>
    /// <param name="Y">Deck units.</param>
    /// <param name="What">The thing at this stop: the car, a corridor mouth, a room.</param>
    /// <param name="Point">#831 · The watchclock station this stop SNAPPED to, or null when the floor had no
    /// wall to offer within <see cref="CheckpointReachDu"/>. Carried on the stop rather than kept in a list
    /// beside it because <see cref="BeatFor"/> reverses and rotates the round: a parallel list would be one
    /// shift away from a guard facing somebody else's plate.</param>
    public readonly record struct Stop(double X, double Y, string What, Checkpoint? Point = null);
}
