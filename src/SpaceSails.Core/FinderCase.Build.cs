using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #251 · BUILDING ONE (#417) — the fee band, <c>Build</c>, and the private finders under it: the two
/// hulls, the confrontation port and the witness.
///
/// <para>Split out of <c>FinderCase.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Its fields are <c>const</c>s.</para>
/// </summary>
public static partial class FinderCase
{
    // ── BUILDING ONE ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many berths a port must keep before a confrontation can happen there. Two, and it is not
    /// a knob: the reveal is <i>the transponder in the NEXT berth</i>, and a port with one collar has no next
    /// berth to point at (<see cref="DockRoster.BerthsAtAnOutpost"/> is one, and that is the correct
    /// behaviour rather than a gap).</summary>
    public const int BerthsAConfrontationNeeds = 2;

    /// <summary>The fee's band, in credits. Wide enough that two cases in one thread are not the same job
    /// twice, small enough that a finder's case is not a career. FLAGGED for owner tuning.</summary>
    public const int FeeFloorCredits = 600;

    /// <summary>…and the top of it.</summary>
    public const int FeeCeilingCredits = 1400;

    /// <summary>What the man aboard offers, as a multiple of the fee. He is buying a silence that is worth
    /// more to him than the finding is to her, and the number says so. FLAGGED.</summary>
    public const int BribeIsThisManyFees = 2;

    /// <summary>
    /// <b>DEAL ONE CASE, OR NONE.</b>
    ///
    /// <para>Null when the world handed in cannot furnish one: no port with a next berth, no ground, no
    /// regular, or — the interesting one — <b>no two hulls in the traffic that have ever answered to the same
    /// name</b>. That last is the case's own spine and it is not something this file may arrange: the former
    /// names are seeded off the hull ids (<see cref="ShipHistories.For"/>) and a universe whose traffic
    /// happens to share no name between two hulls is a universe with no case in it this watch.</para>
    /// </summary>
    /// <param name="threadId">The game thread's id — the per-universe seed.</param>
    /// <param name="clientPortId">Where Varga is sitting: the port whose bar the captain is in.</param>
    /// <param name="berths">Every dockable berth the scenario publishes, with the tier it has earned.</param>
    /// <param name="names">What the world calls each of those berths and each site body, by id.</param>
    /// <param name="hulls">The traffic, as this file reads it.</param>
    /// <param name="sites">The grounds a paper can be found on.</param>
    public static Case? Build(
        string threadId,
        string clientPortId,
        IReadOnlyList<OldCrew.Berth> berths,
        IReadOnlyDictionary<string, string> names,
        IReadOnlyList<Hull> hulls,
        IReadOnlyList<Site> sites)
    {
        ArgumentNullException.ThrowIfNull(threadId);
        ArgumentNullException.ThrowIfNull(clientPortId);
        ArgumentNullException.ThrowIfNull(berths);
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(hulls);
        ArgumentNullException.ThrowIfNull(sites);

        if (sites.Count == 0 || !Names(names, clientPortId, out string clientName))
        {
            return null;
        }

        if (TheTwoHulls(threadId, clientPortId, hulls) is not { } pair)
        {
            return null;
        }

        if (TheConfrontationPort(threadId, clientPortId, berths) is not { } berth)
        {
            return null;
        }

        if (TheWitness(threadId, clientPortId, berths) is not { } witness)
        {
            return null;
        }

        Site site = sites[(int)(DiceRule.Seed($"finder|site|{threadId}|{clientPortId}") % (ulong)sites.Count)];

        int fee = FeeFloorCredits + DiceRule.Roll(
            DiceRule.Seed($"finder|fee|{threadId}|{clientPortId}"),
            FeeCeilingCredits - FeeFloorCredits + 1).Face - 1;

        return new Case(
            ClientPortId: clientPortId,
            ClientPortName: clientName,
            WitnessId: witness.Regular,
            WitnessPortId: witness.PortId,
            PaperSiteBodyId: site.BodyId,
            PaperSiteName: site.Name,
            HullId: pair.Suspect.Id,
            HullCallsign: pair.Suspect.Callsign,
            FormerName: pair.Name,
            HerringHullId: pair.Cleared.Id,
            HerringCallsign: pair.Cleared.Callsign,
            BerthPortId: berth.PortId,
            BerthSlot: berth.Slot,
            PayCredits: fee,
            PayReputation: ReputationForFinding,
            BribeCredits: fee * BribeIsThisManyFees);
    }

    /// <summary>Two hulls and the name they share, with the record's verdict already applied — the SUSPECT is
    /// the one the paper does not clear.</summary>
    private readonly record struct Pair(string Name, Hull Suspect, Hull Cleared);

    /// <summary>
    /// The first pair of hulls in the traffic that have ever answered to one name.
    ///
    /// <para><b>The order is fixed and the pass is exhaustive</b>, which is this repository's fourth named
    /// bug class stated as a rule: a list walked in whatever order it arrived in is not a list in a stable
    /// order, so the candidates are collected by a nested walk in the handed order and the SEED then picks
    /// among however many the world actually has. Picking the first match would make the case a fact about
    /// how the traffic generator happened to sort itself.</para>
    /// </summary>
    private static Pair? TheTwoHulls(string threadId, string clientPortId, IReadOnlyList<Hull> hulls)
    {
        var found = new List<Pair>();

        for (int i = 0; i < hulls.Count; i++)
        {
            IReadOnlyList<string> mine = hulls[i].History.BareFormerNames;
            if (mine.Count == 0)
            {
                continue;
            }

            for (int j = i + 1; j < hulls.Count; j++)
            {
                foreach (string name in mine)
                {
                    if (!Carries(hulls[j].History, name))
                    {
                        continue;
                    }

                    bool firstIsCleared = TheRecordClears(hulls[i], hulls[j]);
                    found.Add(firstIsCleared
                        ? new Pair(name, hulls[j], hulls[i])
                        : new Pair(name, hulls[i], hulls[j]));
                    break;   // one pair per two hulls; a second shared name is the same two ships
                }
            }
        }

        return found.Count == 0
            ? null
            : found[(int)(DiceRule.Seed($"finder|hulls|{threadId}|{clientPortId}") % (ulong)found.Count)];
    }

    /// <summary>Has this hull ever answered to that name? Read off her own ledger of names and never off a
    /// second list.</summary>
    private static bool Carries(ShipHistory history, string name)
    {
        foreach (string was in history.BareFormerNames)
        {
            if (string.Equals(was, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The port the confrontation happens at, and the slot the fourth name is tied up in.</summary>
    private readonly record struct Confrontation(string PortId, int Slot);

    /// <summary>
    /// A port with a roster big enough to have a NEXT berth in it, and the slot beside the one it has always
    /// given him.
    ///
    /// <para>The captain's own slot is <see cref="DockRoster.OrdinaryBerth"/> — stable per port, which is what
    /// makes "the next berth" a place a captain can actually walk to rather than a different berth every
    /// arrival. The fourth name is in the slot one round from it, and the modulo is why a captain berthed at
    /// the last slot of the ring still has a neighbour.</para>
    /// </summary>
    private static Confrontation? TheConfrontationPort(
        string threadId, string clientPortId, IReadOnlyList<OldCrew.Berth> berths)
    {
        var roomy = new List<OldCrew.Berth>();
        foreach (OldCrew.Berth b in berths)
        {
            if (DockRoster.BerthsAt(b.Tier) >= BerthsAConfrontationNeeds)
            {
                roomy.Add(b);
            }
        }

        if (roomy.Count == 0)
        {
            return null;
        }

        OldCrew.Berth at = roomy[
            (int)(DiceRule.Seed($"finder|berth|{threadId}|{clientPortId}") % (ulong)roomy.Count)];
        int slots = DockRoster.BerthsAt(at.Tier);
        return new Confrontation(at.Id, (DockRoster.OrdinaryBerth(at.Id, slots) + 1) % slots);
    }

    /// <summary>Which regular saw it, and the port they are actually to be found at.</summary>
    private readonly record struct Witness(string Regular, string PortId);

    /// <summary>
    /// A roving regular, and <b>the port their own rota favours them at</b>.
    ///
    /// <para>Not a port picked and a person posted to it: <see cref="PatronRota.Affinity"/> already knows
    /// where each of the four drinks (the Fixer haunts Cinder Roost, Gilt-Eye works Selene Gate), and asking
    /// it is the difference between a witness who is somewhere and a witness who is somewhere for a reason.
    /// Ties break on the port id so the answer is deterministic on a world where nobody is favoured
    /// anywhere.</para>
    /// </summary>
    private static Witness? TheWitness(
        string threadId, string clientPortId, IReadOnlyList<OldCrew.Berth> berths)
    {
        if (berths.Count == 0 || PatronRota.Roster.Count == 0)
        {
            return null;
        }

        string regular = PatronRota.Roster[
            (int)(DiceRule.Seed($"finder|witness|{threadId}|{clientPortId}") % (ulong)PatronRota.Roster.Count)];

        string bestPort = "";
        double best = double.NegativeInfinity;
        foreach (OldCrew.Berth b in berths)
        {
            double affinity = PatronRota.Affinity(regular, b.Id);
            if (affinity > best
                || (affinity == best && string.CompareOrdinal(b.Id, bestPort) < 0))
            {
                best = affinity;
                bestPort = b.Id;
            }
        }

        return bestPort.Length == 0 ? null : new Witness(regular, bestPort);
    }

    /// <summary>What the world calls this id, or false when it has no name for it — a case that filled its
    /// hook's brace with an id would be printing a database key at the player.</summary>
    private static bool Names(IReadOnlyDictionary<string, string> names, string id, out string name)
    {
        name = names.TryGetValue(id, out string? found) ? found ?? "" : "";
        return name.Length > 0;
    }
}
