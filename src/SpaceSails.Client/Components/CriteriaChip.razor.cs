using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Components;

// CriteriaChip — the code-behind for CriteriaChip.razor (#243).
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output is
// NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// There is no arithmetic in here on purpose. Everything this chip draws was decided in Core by
// ConditionsGate, off the gate's own enforcing code — the chip's whole job is to be the one place the
// decision is turned into pixels, so a second surface cannot draw the same criterion a different way.
public partial class CriteriaChip
{
    /// <summary>The criterion to draw, composed by <see cref="ConditionsGate"/> from the gate's own limits.</summary>
    [Parameter] public GateCriterion Criterion { get; set; }

    /// <summary>The trend arrow, or nothing at all when the sim measures no rate for this criterion —
    /// <see cref="ConditionsGate.TrendGlyph"/> is the one place that choice is made.</summary>
    private string Arrow =>
        ConditionsGate.TrendGlyph(Criterion.Trend) is { Length: > 0 } glyph ? " " + glyph : string.Empty;

    /// <summary>
    /// The hover line — the house standard for a hint: what the row MEANS, never a restatement of what it
    /// already says in numbers. A criterion with no measured rate carries the plain reading, because a
    /// tooltip promising a trend it cannot supply is worse than no tooltip.
    /// </summary>
    private string Hint =>
        ConditionsGate.TrendWord(Criterion.Trend) is { Length: > 0 } word
            ? $"{Criterion.Label}: {Criterion.Reading} against {Criterion.Gate} — {word}"
            : $"{Criterion.Label}: {Criterion.Reading} against {Criterion.Gate}";
}
