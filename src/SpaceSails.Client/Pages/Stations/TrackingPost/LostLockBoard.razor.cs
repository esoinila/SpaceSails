using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using TrackingCandidate = SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate;

namespace SpaceSails.Client.Pages.Stations;

// LostLockBoard — the code-behind for LostLockBoard.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of LostLockBoard.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class LostLockBoard
{
    // #251 · Every [Parameter] below is a member of TrackingPost under the member's own name.
    // A parameter that needs saying more than that says it on its own line.

    [Parameter] public LostTrackLedger _lostTracks { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Func<string, TrackingCandidate?> FindCandidate { get; set; } = default!;
    [Parameter] public Func<double, string> FormatWallDistance { get; set; } = default!;
    [Parameter] public Action<string> PrioritizeSearch { get; set; } = default!;
}
