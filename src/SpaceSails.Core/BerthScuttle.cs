using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #525 · <b>BLOWING HER AT A STATION BERTH IS ITS OWN SCENE, AND ITS OWN CRIME.</b>
///
/// <para><see cref="ShipScuttle"/> answers one question at zero — <i>was the captain aboard her</i> — and the
/// two answers are a death and a beginning. Both of those were written for the dark, where the only witnesses
/// are the people who were chasing her and their reason to care goes up with the hull. <b>A berth is not the
/// dark.</b> She is clamped to somebody else's collar, three kilometres of somebody else's ring is on the
/// other side of that hatch, and the ninety seconds are not private: the charges' PA is the port's PA.</para>
///
/// <para>Canon (Fable, 2026-09-05), and every line of it is implemented in exactly one place:</para>
/// <list type="number">
/// <item><b>Arming is public.</b> The port's own voice, once, on the pulse — <see cref="PaCall"/>, naming
/// the slot and nobody else.</item>
/// <item><b>The roster empties the neighbouring slots</b> — <see cref="CollarCleared"/>, walked round the
/// ring <see cref="DockRoster"/> already draws, <b>for once with a reason on the record</b>
/// (<see cref="Why"/>). #1092's reassignment is the same act performed by people who do not know why; this
/// one has a cause, and the cause is filed.</item>
/// <item><b>The concourse leaves</b> through the leaves that do not open for the captain (#731's egress).</item>
/// <item><b>The station files it</b> — the wire at once, and the port's operator remembers it at the top of
/// their meter (<see cref="Charge"/>).</item>
/// <item><b>He is not clear.</b> A castaway in the dark is worth the fuel to nobody. A castaway on a
/// concourse is standing on the ground of the people he did it to.</item>
/// </list>
///
/// <para><b>WHAT IS NOT DECIDED HERE.</b> Whether Nebula Mutual pays out on a hull whose captain turned the
/// keys himself is the owner's open question on #525, and this lane answers it by not touching it:
/// <see cref="InsuranceRule.ApplyToRebirth"/> is exactly what it was, and the castaway at a berth is handed
/// the same rustbucket the castaway in the dark is handed. The fine print is the best seam the Nebula arc has
/// been given and it is not spent here.</para>
///
/// <para><b>Two authored sentences and no others.</b> <see cref="PaCall"/>'s and
/// <see cref="ThePortHasYourName"/>'s, both verbatim from the canon pass, both swept by
/// <see cref="AllProse"/>. §8's reserved word is absent from both, and <b>the port never names who called it
/// in</b> — the PA says the berth and the announcement and nothing about a person, which is the whole of why
/// it is frightening.</para>
/// </summary>
public static class BerthScuttle
{
    // ── WHERE SHE IS WHEN THE KEYS TURN ─────────────────────────────────────────────────────────────────

    /// <summary>#525 · <b>The one question that makes this a different scene.</b> Is she clamped to a port's
    /// collar right now?
    ///
    /// <para>Deliberately not "is he ashore" and not "is there an interior": a hull tied up at an outpost's
    /// one collar is at a berth, and a captain sitting at his own nav board with the gangway mated is at a
    /// berth too. A scuttle in open space — every case where this is false — is unchanged, to the bit.</para>
    /// </summary>
    /// <param name="dockedHavenId">The port she is clamped to, or null.</param>
    /// <param name="onWreck">The captain is aboard a DERELICT, whose own console runs
    /// <see cref="Scuttle"/>'s clock and has nothing to do with her charges.</param>
    public static bool AtABerth(string? dockedHavenId, bool onWreck) =>
        !onWreck && !string.IsNullOrEmpty(dockedHavenId);

    /// <summary>#525 · <b>What the plate on the frame says.</b> <see cref="DockRoster"/> counts slots from
    /// zero because a bearing round a ring does; no harbour in history has painted BERTH 0 on anything. One
    /// conversion, in one place, so the PA and any plate that ever reads a slot cannot come to two
    /// opinions.</summary>
    public static int BerthNumber(int slot) => slot + 1;

    // ── 1 · THE PORT'S OWN VOICE ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #525 · <b>THE PORT'S PA, ONCE, ON THE PULSE.</b> Authored verbatim in the canon pass and the first of
    /// this lane's two sentences.
    ///
    /// <para>It is flat and procedural for <see cref="Scuttle.PaCall"/>'s reason, said about a working
    /// harbour instead of a dead hull: an overload announcement is a safety system, not a courtesy. And it
    /// <b>names the berth and nothing else</b> — not the ship, not her captain, not who declared it. The
    /// concourse is told there is a reactor running away with itself in slot nine. Working out who is
    /// standing in slot nine is left to the concourse, which is exactly how a port would do it and exactly
    /// how a crowd would.</para>
    /// </summary>
    public static string PaCall(int berthNumber) =>
        $"Berth {berthNumber}: reactor overload declared. Clear the collar. This is not a drill.";

    // ── 2 · THE COLLAR IS CLEARED ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #525 · <b>WHY THE NEIGHBOURS WERE MOVED — and the whole difference between this and #1092.</b>
    ///
    /// <para><see cref="QuietHands"/> reassigns a berth and publishes <b>no prose at all</b>: that channel's
    /// entire design is a harbour doing ordinary paperwork for reasons nobody in it could tell you. This is
    /// the same clerk, the same roster and the same act — and it is the opposite kind of event, because a
    /// reason went into the book with it. Carrying that reason as a value rather than as a sentence is what
    /// keeps this lane at two authored strings: the record says why, and the game never explains it.</para>
    /// </summary>
    public enum Why
    {
        /// <summary>A captain turned both keys against his own hull while she was clamped on. The only
        /// member, and it is an enum rather than a bool so the second reason a harbour ever files has
        /// somewhere to go.</summary>
        DeclaredOverload,
    }

    /// <summary>
    /// #525 · <b>THE SLOTS THE ROSTER EMPTIES.</b> The berths either side of his on the ring
    /// <see cref="DockRoster.BearingOf"/> draws — the collars close enough to share the blast, which is the
    /// only reading a dockmaster would ever take of the sentence <i>clear the collar</i>.
    ///
    /// <para><b>Never his own slot</b>, which is not a rounding detail: he is still tied up in it, and a
    /// roster that reassigned the ship declaring the overload away from the overload would be the feature
    /// silently doing nothing. <b>Sorted, and deduplicated</b> — at a two-berth port both sides are the same
    /// neighbour and the answer is one slot, not the same slot twice.</para>
    ///
    /// <para><b>Empty at a one-berth outpost</b>, and that is the correct answer rather than a gap, in
    /// <see cref="DockRoster.BerthsAtAnOutpost"/>'s own words: a place with one berth cannot reassign
    /// anybody. The PA still goes out. There is simply nobody in the next slot to move, because there is no
    /// next slot.</para>
    /// </summary>
    public static IReadOnlyList<int> CollarCleared(int berth, int berths)
    {
        if (berths <= 1)
        {
            return [];
        }

        int mine = (((berth % berths) + berths) % berths);
        int before = (mine + berths - 1) % berths;
        int after = (mine + 1) % berths;

        if (before == after)
        {
            return [before];
        }
        return before < after ? [before, after] : [after, before];
    }

    // ── 3 · WHAT THE PORT'S OPERATOR REMEMBERS ──────────────────────────────────────────────────────────

    /// <summary>
    /// #525 · <b>THE CROSSING, AND IT IS THE WHOLE METER.</b> Published in
    /// <see cref="UndergroundComplex.HeatCharge"/>'s one shape, through <see cref="IllegalHeat.Charge"/>, so
    /// this scene cannot come to its own opinion about what a charge is or who is owed it.
    ///
    /// <para>The weight is <see cref="IllegalHeat.Ceiling"/> and is never typed anywhere — see
    /// <see cref="IllegalHeat.Crossing.SheWentAtTheirBerth"/> for why the meter's own top is the only honest
    /// number here, and what it buys on their floor.</para>
    /// </summary>
    public static UndergroundComplex.HeatCharge Charge(string havenId)
    {
        ArgumentNullException.ThrowIfNull(havenId);
        return IllegalHeat.Charge(havenId, IllegalHeat.Crossing.SheWentAtTheirBerth);
    }

    /// <summary>
    /// #525 · <b>IS HE A FUGITIVE ON THEIR FLOOR?</b> Asked of the meter rather than of a flag, because the
    /// game already has this state and it is a paper trail rather than a mood.
    ///
    /// <para>The round keeps ONE ladder of patience (<see cref="PatrolBeat.EscortsAWatchAllows"/>) and heat
    /// starts you further up it (<see cref="IllegalHeat.StartingRung"/>). At the ceiling the rung is the last
    /// one, so the first thing the watch does about him is the thing it normally takes a whole evening of
    /// provocations to reach: the same men, the same mild procedure, arriving at the end of their patience
    /// before he has done anything. That is what "he is a fugitive on foot" means in the vocabulary this
    /// building already speaks, and it costs no second flag, no floor-wide alert
    /// (<c>PatrolBeat.Chase</c> refuses one by name) and no new field in the save — the outfit's book already
    /// rides the vault.</para>
    /// </summary>
    public static bool AFugitiveOnTheirFloor(int heatAtSite) =>
        IllegalHeat.StartingRung(heatAtSite) >= PatrolBeat.EscortsAWatchAllows;

    // ── 4 · THE SECOND LINE ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #525 · <b>THE PORT'S SECOND LINE</b>, on the card that already tells the castaway ending — under
    /// <see cref="ShipScuttle.CastawayLine"/>'s <i>"a man in a small cold boat who is worth the fuel to
    /// nobody"</i>, which at a berth is the one thing on that card that is not true.
    ///
    /// <para>Authored verbatim in the canon pass and the second of this lane's two sentences. Both halves of
    /// it are a fact the player can check: they have his name because he tied up under it, and they had his
    /// berth because the roster gave it to him. <b>Nobody informed on him.</b> That is the point, and it is
    /// why the sentence is in the past tense on the second clause: the berth is not there any more.</para>
    /// </summary>
    public const string ThePortHasYourName = "The port has your name. It had your berth.";

    // ── THE SWEEP ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#525 · Every string this scene publishes — two, and a reflection sweep in
    /// <c>TheBerthIsItsOwnSceneTests</c> fails on a third. The PA is quoted at a berth number so the sweep
    /// reads the same sentence the player does.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return PaCall(BerthNumber(0));
        yield return ThePortHasYourName;
    }
}
