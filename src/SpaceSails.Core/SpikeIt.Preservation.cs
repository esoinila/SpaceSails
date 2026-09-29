using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: #1202 slice 4 · CHARGED TO PRESERVATION — the wire family meets #1074. The receipt paper a paid spike
// leaves in the satchel, and the port rag's line the cycle after a SPIKED window. Consts and methods only: every
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

    /// <summary>The slice's three strings, for <see cref="AllProse"/>.</summary>
    private static IEnumerable<string> PreservationProse()
    {
        yield return ReceiptTitle;
        yield return ReceiptDocument;
        yield return BylineMissingLine;
    }
}
