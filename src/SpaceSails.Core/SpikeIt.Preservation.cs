using System;
using System.Collections.Generic;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

// Subject: #1202 slice 4 · CHARGED TO PRESERVATION — the wire family meets #1074. The receipt paper a paid spike
// leaves in the satchel, the port rag's line the cycle after a SPIKED window, and her absence from her bar seat
// and her one line on coming back. Consts and methods only: every
// static field of SpikeIt stays in SpikeIt.cs (the static-partial-split law).
public static partial class SpikeIt
{
    // ── THE LINES (verbatim, #1202 slice 4 · CHARGED TO PRESERVATION · Fable, 2026-09-29 night) ─────────

    /// <summary>#1202 slice 4 · The receipt's title, as the field book reads it away from the desk.</summary>
    public const string ReceiptTitle = "A line item, one entry";

    /// <summary>#1202 slice 4 · …and its document. No amount, no signature, no department but the cost centre —
    /// #1074 beat 3's idiom, and the cost centre is the only tell.</summary>
    public const string ReceiptDocument = "Editorial services, one item. Charged to Preservation.";

    /// <summary>#1202 slice 4 · The port rag, the cycle after a SPIKED window, once.</summary>
    public const string BylineMissingLine =
        "A stringer's byline is missing from the cycle. The floor has not noticed; the floor never reads bylines.";

    /// <summary>#1202 slice 4 · Her return, once, the first time the captain is across from her after her
    /// absence. About the pages, never the man.</summary>
    public const string ReturnLine =
        "Somebody read my pages before the window. Somebody paid for that. I file on the cycler window whether I am "
        + "back or not.";

    /// <summary>#1202 slice 4 · What a regular says, asked about her empty seat while she is away — #1074 beat 4's
    /// own sentence (<see cref="CareerCost.ColleagueLine"/>), reused and never a second copy of it. Not in this
    /// family's <see cref="AllProse"/>: beat 4 owns it.</summary>
    public static string TheRegularsAnswer => CareerCost.ColleagueLine;

    // ── THE RECEIPT ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The receipt's satchel id: this, a colon, and the body the spiked story was about — the fact the
    /// book files it under, carried by the id rather than stored as prose (the <see cref="SwapId"/> shape).</summary>
    public const string ReceiptId = "spike-receipt";

    /// <summary>The glyph the book files the receipt under: the building's own paper glyph.</summary>
    public const string ReceiptGlyph = "📋";

    /// <summary>Is this paper the receipt?</summary>
    public static bool IsTheReceipt(string? paperId) =>
        paperId is not null && paperId.StartsWith(ReceiptId + ":", StringComparison.Ordinal);

    /// <summary>The receipt for a story about this body.</summary>
    public static Satchel.Item TheReceipt(string bodyName) => new(Satchel.Kind.Paper, $"{ReceiptId}:{bodyName}");

    /// <summary>The body a receipt is about, rebuilt from its id; empty for any other paper.</summary>
    public static string ReceiptBody(string? paperId) =>
        IsTheReceipt(paperId) ? paperId![(ReceiptId.Length + 1)..] : "";

    /// <summary>
    /// <b>DOES THE OFFICE PAY WITH PAPER?</b> On a payout, and only on one: SPIKED (the full purse) and ALTERED
    /// (half). LATE pays nothing, so nothing is itemised; and nothing lands before the window has decided.
    /// </summary>
    public static bool ReceiptLands(Outcome outcome) => outcome is Outcome.Spiked or Outcome.Altered;

    /// <summary>
    /// What the book files the receipt under: <i>the Authority</i> and the story's body — #1074 beat 3's own
    /// door (<see cref="MoneyTrail.SubjectsFor"/>), so the receipt stacks under the same office as the rail, the
    /// rota and the pour, and under the ground the story was about.
    /// </summary>
    public static string ReceiptSubjects(string bodyName) => MoneyTrail.SubjectsFor(bodyName);

    // ── THE RAG ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>DOES THE RAG NOTICE THE HOLE?</b> Only after a SPIKED window: an ALTERED story ran under her byline and
    /// LATE ran her own, so the byline is not missing from either cycle. Port rags only, and the cycle after —
    /// the floor's own clock (<see cref="CarryThePress.FloorAt"/>), the day the floor would have had its opinion.
    /// </summary>
    public static bool TheRagNoticesTheHole(Outcome outcome) => outcome == Outcome.Spiked;

    // ── HER ABSENCE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Watches her bar seat stands empty after a SPIKED window, at the least and the most.</summary>
    public const int AwayAtLeastWatches = 3;
    public const int AwayAtMostWatches = 6;

    /// <summary>How many watches she is away after a SPIKED window — seeded off her contract, so the same on
    /// every machine and after a reload.</summary>
    public static int AwayFor(string questId) =>
        AwayAtLeastWatches
        + DiceRule.Roll(DiceRule.Seed($"spike:away:{questId}"), AwayAtMostWatches - AwayAtLeastWatches + 1).Face - 1;

    /// <summary>The first watch she is back: the window's own watch plus <see cref="AwayFor"/>. The absence is the
    /// watches from the window up to, and not including, this one. It rides the contract line as one watch
    /// index (<see cref="CarryThePress.Passage.Back"/>).</summary>
    public static long BackOnWatch(string questId, double storyAt) =>
        PatronRota.WatchIndex(storyAt) + AwayFor(questId);

    /// <summary><b>IS SHE AWAY ON THIS WATCH?</b> Only after a SPIKED window, and only before the watch she is
    /// back. Nothing is greyed and nothing is said: her chair is simply empty.</summary>
    public static bool IsAway(Outcome outcome, long? back, long watch) =>
        outcome == Outcome.Spiked && back is { } b && watch < b;

    /// <summary><b>IS HER RETURN LINE DUE?</b> After a SPIKED window, from the watch she is back, and never once
    /// it has been told. The line itself is <see cref="ReturnLine"/>.</summary>
    public static bool HerReturnIsDue(Outcome outcome, long? back, bool told, long watch) =>
        !told && outcome == Outcome.Spiked && back is { } b && watch >= b;

    /// <summary>
    /// <b>WHO IS ASKED ABOUT HER SEAT.</b> The regular sitting nearest her empty chair — her neighbour, the one
    /// a captain looking at the chair would turn to. Ties go to the name first in ordinal order, so the answer is
    /// one person and the same one every time. Null when nobody is sitting.
    /// </summary>
    public static string? WhoIsAsked(IEnumerable<(string Name, double X, double Y)> seated, double chairX, double chairY)
    {
        string? who = null;
        double best = double.MaxValue;
        foreach ((string name, double x, double y) in seated)
        {
            double d = ((x - chairX) * (x - chairX)) + ((y - chairY) * (y - chairY));
            if (d < best || (d == best && who is not null && string.CompareOrdinal(name, who) < 0))
            {
                who = name;
                best = d;
            }
        }

        return who;
    }

    /// <summary>The slice's four strings, for <see cref="AllProse"/>.</summary>
    private static IEnumerable<string> PreservationProse()
    {
        yield return ReceiptTitle;
        yield return ReceiptDocument;
        yield return BylineMissingLine;
        yield return ReturnLine;
    }
}
