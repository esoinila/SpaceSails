using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #417 slice 1 · <b>THE FINDER'S CASE</b> — one whole case, seeded, pure, and built out of things the world
/// was already dealing.
///
/// <para><b>Owner, 2026-07-20:</b> <i>"We can always add private detective type missions then… just not call
/// our detective Miller 🤭"</i>. So she is not Miller. She is <b>Ilse Varga</b>, ex-harbour police at Selene
/// Gate, freelance finder, a coat worse than Fess's and a drinking excuse always ready — Fable's canon pass
/// of 2026-09-05, and every sentence below is lifted from it character for character.</para>
///
/// <h3>The graph, and the one law that shapes it</h3>
///
/// <para><b>IT INVENTS NOTHING.</b> A client port, a witness, a site, two hulls and a confrontation berth —
/// and every one of them is handed IN, off the world the game is already running. <see cref="Build"/> takes
/// lists and returns null when the world it was handed cannot furnish a case, which is the honest answer and
/// the reason there is no fallback arm anywhere in this file: a case that invented a port to finish itself
/// would be a detective story about a place the captain can never fly to.</para>
///
/// <list type="bullet">
/// <item><b>The client</b> — Varga, at a bar table at a port the world publishes. Her hook names that port,
/// and it is the only place the hook's <c>{PORT}</c> is ever filled from.</item>
/// <item><b>Lead one, a witness</b> — one of the bar's own roving regulars (<see cref="PatronRota"/>), at the
/// port that rota actually favours them at. #414's rhythm, asked rather than re-invented.</item>
/// <item><b>Lead two, a paper</b> — a find on a real body's ground, clipped into the field book under this
/// case's subjects (#1052/#934).</item>
/// <item><b>Lead three, a hull under a former name</b> — an NPC hull out of the traffic, read off her own
/// <see cref="ShipHistory"/> ledger of names (#397).</item>
/// <item><b>The red herring</b> — a SECOND hull answering to the same former name, whose chain of custody
/// (#426) clears her: her papers are older than the story.</item>
/// <item><b>The confrontation</b> — a real slot on a real port's roster (#1092/<see cref="DockRoster"/>): the
/// berth next to the one that port has always given him.</item>
/// </list>
///
/// <h3>The laws the canon pass set, and where each is kept</h3>
///
/// <list type="bullet">
/// <item><b>The case never names the Authority or the watchers</b>, and <b>the man's crime is never
/// stated</b> — a finder finds, she does not judge. Both are properties of the eleven sentences below and
/// are swept by this feature's guard.</item>
/// <item><b>Varga recurs at most once per port.</b> Kept by the room (a visit fold, exactly as the salesman's
/// and the walk-in's are), because it is a fact about a berth and not about a case.</item>
/// <item><b>The reserved word</b> of <c>docs/worldbuilding-notes.md</c> §8 is absent.</item>
/// </list>
///
/// <para>Pure and deterministic like everything else in Core: the same thread and the same world deal the
/// same case, on every machine and across a reload.</para>
/// </summary>
public static partial class FinderCase
{
    // ── WHO SHE IS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where her book is kept in the contacts ledger. A person's row, not an outfit's — she is
    /// somebody you drank with, and the reputation this case pays lands here as goodwill.</summary>
    public const string ContactId = "finder-ilse-varga";

    /// <summary>Her name, as it goes in the book from the moment she says it.</summary>
    public const string DisplayName = "Ilse Varga";

    /// <summary>The plate the deck draws over her while she crosses the floor. Composed exactly as the
    /// salesman's and the walk-in's are (<see cref="WalkIn.Plate"/>) — a plate is what the room calls
    /// somebody, and the room does not know she is a case.</summary>
    public static string Plate => "◈ " + DisplayName.ToUpperInvariant();

    // ── WHAT SHE SAYS. Fable's canon pass, 2026-09-05, verbatim. ────────────────────────────────────────

    /// <summary>Her approach, said at a bar table before the captain can decide anything.</summary>
    public const string Approach =
        "Varga. I used to wear a badge at Selene Gate; now I find things for people who can't ask the badge. "
        + "You have a ship and no reputation to lose. Sit.";

    /// <summary>The hook, with the client port's own name in it. The brace is filled from
    /// <see cref="Case.ClientPortName"/> and from nowhere else.</summary>
    private const string HookLine =
        "A man walked off a hull at {0} and never walked into the concourse. The hull has had three names. "
        + "Find the fourth.";

    /// <summary>The hook as she says it here, at this port.</summary>
    public static string Hook(string portName) =>
        string.Format(CultureInfo.InvariantCulture, HookLine, portName);

    /// <summary>What she says the next time she is met, once the case is settled and paid.</summary>
    public const string Payoff = "Paid. Don't thank me — the next one is worse, and you'll take it.";

    // ── THE CASE'S OWN LINES ────────────────────────────────────────────────────────────────────────────

    /// <summary>The head of the lead card, and the head of the entry it leaves in the field book.</summary>
    public const string LeadTitle = "A finder's case";

    /// <summary>…and the body of it. It states the shape of the case and refuses to state what it means,
    /// which is the field book's own frame law (#587).</summary>
    public const string LeadBody = "Three names on one hull, and a man who is none of them.";

    /// <summary>The red herring, at the moment her chain of custody clears her. A pulse: nothing is decided
    /// here, a door is closed.</summary>
    public const string HerringCleared = "Her papers are older than the story. Not this one.";

    /// <summary>The reveal, at the confrontation berth. The one plot-significant telling in this case, and
    /// the card is the telling (#761).</summary>
    public const string Reveal =
        "The fourth name is on the transponder in the next berth. He is aboard. He knows you are here.";

    /// <summary>The first verb.</summary>
    public const string TurnHimIn = "Turn him in";

    /// <summary>…and the second. Two, because there is nothing to bargain about.</summary>
    public const string TakeTheBribe = "Take the bribe";

    /// <summary>What happens when he is turned in.</summary>
    public const string AfterTurningIn = "Selene Gate sends a boat. Varga does not come to see it.";

    /// <summary>…and when the bribe is taken.</summary>
    public const string AfterTheBribe =
        "The account clears before he does. Varga will hear; she always does.";

    /// <summary>Every player-facing sentence this case can put on a screen — the ten the canon pass authored
    /// plus the hook's template, which is a sentence with a port's name in the middle of it. The same
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps, and the list the reserved-word
    /// sweep and the no-twelfth-string sweep both walk.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Approach;
        yield return HookLine;
        yield return Payoff;
        yield return LeadTitle;
        yield return LeadBody;
        yield return HerringCleared;
        yield return Reveal;
        yield return TurnHimIn;
        yield return TakeTheBribe;
        yield return AfterTurningIn;
        yield return AfterTheBribe;
    }

    // ── WHAT THE WORLD HANDS IN ─────────────────────────────────────────────────────────────────────────

    /// <summary>One hull the traffic is actually flying, as this file needs to read her: her stable id, the
    /// name she answers to now, and her own service record. Handed in rather than looked up, for
    /// <see cref="OldCrew.Berth"/>'s reason — a test builds a world by hand, and a guard handed a world it
    /// invented itself cannot tell pass from fail.</summary>
    public readonly record struct Hull(string Id, string Callsign, ShipHistory History);

    /// <summary>One ground a paper can be found on: the body's id and the name the book will file the find
    /// under. Both are the world's own; nothing here composes either.</summary>
    public readonly record struct Site(string BodyId, string Name);

    // ── THE CASE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The whole graph, as data. Every id in it came out of a list the world handed in, which is the claim
    /// <c>TheCaseInventsNothing</c> holds this file to.
    /// </summary>
    /// <param name="ClientPortId">Where Varga is sitting, and the only port her hook ever names.</param>
    /// <param name="ClientPortName">…as the world spells it, for the hook's brace.</param>
    /// <param name="WitnessId">The roving regular who saw it — a name off <see cref="PatronRota.Roster"/>.</param>
    /// <param name="WitnessPortId">The port that regular's rota actually favours them at.</param>
    /// <param name="PaperSiteBodyId">The body whose ground holds the paper.</param>
    /// <param name="PaperSiteName">…as the world spells it.</param>
    /// <param name="HullId">The hull that answered to <paramref name="FormerName"/> and is still running.</param>
    /// <param name="HullCallsign">The name she answers to now.</param>
    /// <param name="FormerName">The name BOTH hulls have carried — the pivot of the whole case.</param>
    /// <param name="HerringHullId">The second hull that carried it, and whose record clears her.</param>
    /// <param name="HerringCallsign">…as she is called now.</param>
    /// <param name="BerthPortId">The port the confrontation happens at.</param>
    /// <param name="BerthSlot">The slot the fourth name is tied up in — the one next to the captain's.</param>
    /// <param name="PayCredits">What Varga pays for the finding.</param>
    /// <param name="PayReputation">…and the standing it earns with her.</param>
    /// <param name="BribeCredits">What the man aboard offers instead.</param>
    public readonly record struct Case(
        string ClientPortId,
        string ClientPortName,
        string WitnessId,
        string WitnessPortId,
        string PaperSiteBodyId,
        string PaperSiteName,
        string HullId,
        string HullCallsign,
        string FormerName,
        string HerringHullId,
        string HerringCallsign,
        string BerthPortId,
        int BerthSlot,
        int PayCredits,
        int PayReputation,
        int BribeCredits)
    {
        /// <summary>The hook as she says it at this port.</summary>
        public string TheHook => Hook(ClientPortName);

        /// <summary>What the field book files this case under: the finder, the port she asked at, and the
        /// ground the paper is on. Minted through <see cref="CaseSubjects.Person"/> only because the card the
        /// captain is reading PRINTS her name, which is that door's whole condition (#741).</summary>
        public IReadOnlyList<CaseSubjects.Subject> Subjects =>
        [
            CaseSubjects.Person(DisplayName),
            CaseSubjects.Place(ClientPortName),
            CaseSubjects.Place(PaperSiteName),
        ];

        /// <summary>…joined, for the note itself.</summary>
        public string SubjectLine => CaseSubjects.Line(Subjects);
    }

    // ── THE PAY ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How the captain settled it. There is no third arm: a finder finds, and what happens to the
    /// man is the captain's business and nobody else's.</summary>
    public enum Outcome
    {
        /// <summary>Nothing decided yet.</summary>
        Open = 0,

        /// <summary>Turned in. Standing up, and nothing owed to anybody.</summary>
        TurnedIn = 1,

        /// <summary>The bribe taken. Coin now, and a band of heat at this port's own doors.</summary>
        Bribed = 2,
    }

    /// <summary>What one settling actually moves: the purse, the standing with Varga, and what an outfit
    /// remembers. One record, so the arithmetic and the sentence can never come to two views of one
    /// evening.</summary>
    public readonly record struct Payment(int Credits, int Reputation, int HeatPoints);

    /// <summary>Standing the finding earns with her either way. Small: this is a working relationship, not a
    /// friendship, and she says so on the way out. FLAGGED for owner tuning.</summary>
    public const int ReputationForFinding = 2;

    /// <summary>…and the extra rung for handing the man over rather than selling the silence. FLAGGED.</summary>
    public const int ReputationForTurningHimIn = 1;

    /// <summary>
    /// <b>WHAT A SETTLING PAYS, AND WHAT IT COSTS.</b> Both arms pay Varga's fee and Varga's standing,
    /// because both arms are the finding done; the fork is what happens on top.
    ///
    /// <para><b>Turn him in</b> — one more rung of standing, and <b>heat unchanged</b>: nobody was crossed,
    /// nothing was filed. <b>Take the bribe</b> — the man's money on top of the fee, and <b>one whole band</b>
    /// of heat at the port he was tied up in. The band is <see cref="IllegalHeat.ABand"/> and is not a number
    /// typed here: it is the width the meter's own <see cref="IllegalHeat.StartingRung"/> divides by, so
    /// "a band" and "one rung warier at their gate" are the same sentence and cannot come apart the day the
    /// rung is retuned.</para>
    /// </summary>
    public static Payment PayFor(in Case c, Outcome outcome) => outcome switch
    {
        Outcome.TurnedIn => new Payment(
            c.PayCredits, c.PayReputation + ReputationForTurningHimIn, 0),
        Outcome.Bribed => new Payment(
            c.PayCredits + c.BribeCredits, c.PayReputation, IllegalHeat.ABand),
        _ => new Payment(0, 0, 0),
    };

    /// <summary>The line the captain reads when they have chosen. Nothing here says which was right.</summary>
    public static string OutcomeLine(Outcome outcome) => outcome switch
    {
        Outcome.TurnedIn => AfterTurningIn,
        Outcome.Bribed => AfterTheBribe,
        _ => "",
    };

    // ── THE CHAIN OF CUSTODY, WHICH IS THE WHOLE OF THE RED HERRING ─────────────────────────────────────

    /// <summary>
    /// <b>WHICH OF TWO HULLS CARRYING ONE NAME IS CLEARED BY HER OWN RECORD.</b>
    ///
    /// <para>Both answer to the same former name; only one of them can be the hull the man walked off. The
    /// question is settled by the paper and by nothing else — <b>her papers are older than the story</b> —
    /// so the hull laid down EARLIER is the one whose ownership of that name predates the case, and she is
    /// cleared. Ties go to the deeper chain of owners (a hull three owners back carried the name and gave it
    /// up long before a hull one owner back did), and a final tie to the id, so the answer is total,
    /// deterministic and never a coin.</para>
    ///
    /// <para>A hull with no yard record at all is never the cleared one: an absence of paper cannot clear
    /// anybody, and the whole line depends on there BEING a record older than the story.</para>
    /// </summary>
    /// <returns>True when <paramref name="hull"/> is the one the record clears.</returns>
    public static bool TheRecordClears(in Hull hull, in Hull against)
    {
        if (!hull.History.HasYardRecord)
        {
            return false;
        }

        if (!against.History.HasYardRecord)
        {
            return true;
        }

        if (hull.History.Year != against.History.Year)
        {
            return hull.History.Year < against.History.Year;
        }

        if (hull.History.OwnersDeep != against.History.OwnersDeep)
        {
            return hull.History.OwnersDeep > against.History.OwnersDeep;
        }

        return string.CompareOrdinal(hull.Id, against.Id) < 0;
    }

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
