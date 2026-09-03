using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1074/#1063 · <b>WHAT FIVE PAPERS ARE, READ AWAY FROM THE ROOM THEY CAME OUT OF.</b>
///
/// <para>Every paper in this game is anonymous by design and is titled by a seeded roll off its find id
/// (<see cref="FieldClue.Title"/>, #613) — which is right for the six generic forms and wrong for the five
/// this arc actually wrote. A captain who picks up the rail's invoice and opens it in the sleeve is shown a
/// torn shipping manifest; the decision card over the line item's own sentence heads it <i>"pay sheet,
/// allowances"</i>. The sim holds one thing while the sentence reports another, which is the third named bug
/// class, and this is the table that closes it.</para>
///
/// <para><b>THE BOOK KEEPS NO OPINION</b> (#741's law). These are what the paper IS — the clerical noun and
/// one flat line of what is on the page — and never what it means. No line here says lead, clue, site,
/// anomaly or missing; the ledger's title says <i>three entries</i> and its body says one job cites no
/// instruction, and the reader does the rest. That is the same discipline the papers themselves keep in
/// <see cref="Burial"/>, <see cref="StopOrder"/> and <see cref="MoneyTrail"/>.</para>
///
/// <para><b>CANON.</b> All ten strings are authored in #1074's canon pass of 2026-09-03 and lifted character
/// for character. Nothing here is composed, and there is no eleventh string: a paper this arc has not
/// authored a head for keeps the seeded one it has always had.</para>
///
/// <para><b>WHY A TABLE OF ITS OWN.</b> The five papers come from three different beats and three different
/// types, and each of those types already ships a canon sweep that counts its own strings — a head folded
/// into <see cref="Burial.AllProse"/> would be counted as one of the burial's own documents, which it is
/// not. What joins these ten is the SEAM they are read through and not the beat that dealt them, so they
/// are listed where that seam can find them. Which find id is which paper is the building's question and
/// is answered by <see cref="UndergroundComplex.AuthoredPaperOf"/>, beside the room designations that
/// settle it.</para>
/// </summary>
public static class PaperHeads
{
    /// <summary>Which authored paper this is. Five named papers rather than a list index, for
    /// <see cref="MoneyTrail.Item"/>'s reason: a caller is choosing between five THINGS somebody wrote and
    /// not between five positions in an array somebody may reorder.</summary>
    public enum Paper
    {
        /// <summary>#1063 · The plant's maintenance ledger, on a ground somebody filled in.</summary>
        MaintenanceLedger,

        /// <summary>#1074 beat 1 · The plant's valve-book, on a ground whose working was closed.</summary>
        ValveBook,

        /// <summary>#1074 beat 3 · The remediation's line item.</summary>
        Pour,

        /// <summary>#1074 beat 3 · The perimeter rail's line item.</summary>
        Rail,

        /// <summary>#1074 beat 3 · The site watch's line item.</summary>
        Rota,
    }

    // ── THE MAINTENANCE LEDGER (#1063) ──────────────────────────────────────────────────────────────────

    /// <summary>#1074 · What the ledger is called. Authored, verbatim. <i>Three entries</i> is a count and
    /// not a verdict: it says how much paper there is, and the reader counts the citations.</summary>
    public const string LedgerTitle = "A maintenance ledger, three entries";

    /// <summary>#1074 · …and what is on it. Authored, verbatim. It states the ledger's own house style and
    /// then states the exception, in that order, and stops — which is exactly what a reader standing over
    /// the page can see for themselves and no more.</summary>
    public const string LedgerDocument =
        "Plant's book. Every job cites an instruction; one job cites none.";

    // ── THE VALVE-BOOK (#1074 beat 1) ───────────────────────────────────────────────────────────────────

    /// <summary>#1074 · What the valve-book is called. Authored, verbatim, and deliberately the ledger's
    /// sibling to the comma: they are the same clerical paper from the same department, and two different
    /// shapes of title would make one of them look like the interesting one.</summary>
    public const string ValveBookTitle = "A valve-book, three entries";

    /// <summary>#1074 · …and what is on it. Authored, verbatim. The tell here is the PREPOSITION rather than
    /// the absent number, and the line reports it as a fact of the page: an isolation per order, sat between
    /// two jobs per instruction.</summary>
    public const string ValveBookDocument =
        "Plant's book. An isolation per order, between two jobs per instruction.";

    // ── THE THREE LINE ITEMS (#1074 beat 3) ─────────────────────────────────────────────────────────────
    //
    // One purchase, one head, one cost centre. The titles share a prefix — "A line item:" — because that is
    // what they are and because it is what makes the THREADS view (#934) legible the moment a captain has
    // clipped a second one: three rows under one heading, all naming the same budget line, and the book says
    // nothing over them.

    /// <summary>#1074 · The remediation's title. Authored, verbatim.</summary>
    public const string PourTitle = "A line item: remediation";

    /// <summary>#1074 · …and its one line. Authored, verbatim. The quantity is spelled the way the invoice
    /// spells it and the cost centre is the last word, which is where the invoice puts it.</summary>
    public const string PourDocument =
        "Three hundred tonnes into the lower galleries, charged to Preservation.";

    /// <summary>#1074 · The fence's title. Authored, verbatim.</summary>
    public const string RailTitle = "A line item: perimeter rail";

    /// <summary>#1074 · …and its one line. Authored, verbatim.</summary>
    public const string RailDocument = "Sixteen sections, charged to Preservation.";

    /// <summary>#1074 · The watch's title. Authored, verbatim.</summary>
    public const string RotaTitle = "A line item: site watch";

    /// <summary>#1074 · …and its one line. Authored, verbatim. <i>Continuous</i> survives the shortening,
    /// because it is the word the whole item turns on.</summary>
    public const string RotaDocument = "Two hands, continuous, charged to Preservation.";

    // ── THE TWO QUESTIONS THE SEAM ASKS ─────────────────────────────────────────────────────────────────

    /// <summary>What this paper is called, and the one place any caller may get it from. There is no default
    /// arm: a <see cref="Paper"/> nobody has authored a head for would be a document with an invented title
    /// on it, which is the one thing this table exists to prevent.</summary>
    public static string TitleOf(Paper paper) => paper switch
    {
        Paper.MaintenanceLedger => LedgerTitle,
        Paper.ValveBook => ValveBookTitle,
        Paper.Pour => PourTitle,
        Paper.Rail => RailTitle,
        Paper.Rota => RotaTitle,
        _ => throw new ArgumentOutOfRangeException(nameof(paper)),
    };

    /// <summary>…and what is on it. Returned ALONE, exactly as <see cref="HardcaseRep.ScheduleBody"/> is and
    /// for its reason: <see cref="FieldClue.Document"/>'s seeded tail is a GENERIC paper telling you how
    /// well it pins a place, and these five pin nothing — they are the paperwork of one job on the ground
    /// the captain is already standing on. The certainty behind them is untouched and still rolls, so the
    /// tracker, the sleeve's row and <see cref="FieldClue.Line"/> all say what they always said.</summary>
    public static string DocumentOf(Paper paper) => paper switch
    {
        Paper.MaintenanceLedger => LedgerDocument,
        Paper.ValveBook => ValveBookDocument,
        Paper.Pour => PourDocument,
        Paper.Rail => RailDocument,
        Paper.Rota => RotaDocument,
        _ => throw new ArgumentOutOfRangeException(nameof(paper)),
    };

    /// <summary>Every player-facing string this table publishes — ten, and they are the ten the canon pass
    /// authored. The same <c>AllProse</c> discipline every prose-bearing type in Core keeps, and the list
    /// the reserved-word sweep and the no-eleventh-string sweep both walk.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return LedgerTitle;
        yield return LedgerDocument;
        yield return ValveBookTitle;
        yield return ValveBookDocument;
        yield return PourTitle;
        yield return PourDocument;
        yield return RailTitle;
        yield return RailDocument;
        yield return RotaTitle;
        yield return RotaDocument;
    }

    /// <summary>The five, in the order the arc dealt them. A private-shaped constant published for the
    /// sweeps, which is the same arrangement <c>UndergroundComplex.TheItems</c> keeps: an
    /// <c>Enum.GetValues</c> on every row of every satchel draw would allocate for nothing.</summary>
    public static readonly Paper[] All =
        [Paper.MaintenanceLedger, Paper.ValveBook, Paper.Pour, Paper.Rail, Paper.Rota];
}
