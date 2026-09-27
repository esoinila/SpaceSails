using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE CASE, THE PAY AND THE CHAIN OF CUSTODY (#417) — the case record, the two endings and what
/// each pays, and the record that clears the red herring.
///
/// <para>Split out of <c>FinderCase.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Its two fields are <c>const</c>s.</para>
/// </summary>
public static partial class FinderCase
{
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

        // ── THE THREE THE TRAIL ADDS ────────────────────────────────────────────────────────────────
        //
        // #741 v1's law, and it is a law about WHERE: a subject is the AUTHOR's, declared in Core beside
        // the sentence it describes, and the client may not mint one. So the three headings the trail
        // grows are minted here rather than in the page that files them — the guard
        // `TheThreadsPageIsInTheSatchelTests.TheFilingFunnelCarriesTheAuthorsSubjects` sweeps the whole
        // client tree for a `CaseSubjects.Person(` and reds on the first one.
        //
        // A HULL IS FILED AS A PLACE. Of the three kinds she is the least wrong one: a hull with a name on
        // her transponder is somewhere the captain can go alongside, and she is certainly not a person or
        // an office. Both callsigns are printed for the captain to read — the dossier draws them — which
        // is the condition Person carries and which Place is not even asked for.

        /// <summary>The case's own headings, plus the PERSON the captain actually spoke to. Minted through
        /// <see cref="CaseSubjects.Person"/>, which is a promise by the author that this name is printed on
        /// something the captain is reading: it is, on the plate over the regular's own chair.</summary>
        public string SubjectsWithTheWitness(string printedName) =>
            CaseSubjects.Line([.. Subjects, CaseSubjects.Person(printedName)]);

        /// <summary>…plus the hull he went and looked up.</summary>
        public string SubjectsWithTheHull => CaseSubjects.Line([.. Subjects, CaseSubjects.Place(HullCallsign)]);

        /// <summary>…and plus the one her own record clears.</summary>
        public string SubjectsWithTheHerring =>
            CaseSubjects.Line([.. Subjects, CaseSubjects.Place(HerringCallsign)]);
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
}
