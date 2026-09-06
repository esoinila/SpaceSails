using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using SpaceSails.Core;
using TrackedTarget = SpaceSails.Core.TrackedTarget;
using TrackingCandidate = SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate;

namespace SpaceSails.Client.Pages.Stations;

// TrackedTargetCards — the code-behind for TrackedTargetCards.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of TrackedTargetCards.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class TrackedTargetCards
{
    // #251 · Every [Parameter] below is a member of TrackingPost under the member's own name.
    // A parameter that needs saying more than that says it on its own line.

    [Parameter] public TrackedTargetLedger _ledger { get; set; } = default!;
    [Parameter] public TelescopeSchedule _schedule { get; set; } = default!;
    /// <summary>the desk's `HashSet&lt;string&gt; _aware` (PR-6: who knows they have been pinged) — the SAME
    /// set, not a copy, so the ⚠ beside a callsign is the desk's own bookkeeping and not a snapshot.</summary>
    [Parameter] public HashSet<string> _aware { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Vector2d ShipPosition { get; set; }
    [Parameter] public Vector2d ShipVelocity { get; set; }
    [Parameter] public double ShipCharge { get; set; }
    [Parameter] public Func<List<TrackedTarget>> OrderedTracks { get; set; } = default!;
    [Parameter] public Func<string, TrackingCandidate?> FindCandidate { get; set; } = default!;
    [Parameter] public Func<string, string> CardCanvasId { get; set; } = default!;
    [Parameter] public Func<SightAdvantage, string> SightLine { get; set; } = default!;
    [Parameter] public Func<SightAdvantage, string?> BeaconLine { get; set; } = default!;
    [Parameter] public Func<double, string> FormatWallDistance { get; set; } = default!;
    [Parameter] public EventCallback<string> OnSetInterest { get; set; }
    [Parameter] public Action<string> ConfirmNow { get; set; } = default!;
    [Parameter] public Action<string> Drop { get; set; } = default!;
}
