using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

// SweepReadoutStrip — the code-behind for SweepReadoutStrip.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of SweepReadoutStrip.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class SweepReadoutStrip
{
    // #251 · Every [Parameter] below is a member of TrackingPost under the member's own name.
    // A parameter that needs saying more than that says it on its own line.

    /// <summary>the desk's `ScanJob? _activeJob` — the progress bar exists only while a sweep runs.</summary>
    [Parameter] public ScanJob? _activeJob { get; set; }
    [Parameter] public double SweepProgressPercent { get; set; }
    [Parameter] public string? _lastSweepMessage { get; set; }
}
