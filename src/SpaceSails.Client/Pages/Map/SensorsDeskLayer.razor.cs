using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// SensorsDeskLayer — the code-behind for SensorsDeskLayer.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of SensorsDeskLayer.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class SensorsDeskLayer
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public bool _activeRadar { get; set; }
    [Parameter] public List<SpaceSails.Client.Pages.Stations.TrackingPost.CourseOpportunity> _courseOpportunities { get; set; } = default!;
    [Parameter] public ICelestialEphemeris? _ephemeris { get; set; }
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public int _telescopeLevel { get; set; }
    /// <summary>the page's `SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPost` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_trackingPost` and the assignment still lands on the page.</summary>
    [Parameter] public SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPostValue { get; set; }
    /// <summary>The page's `_trackingPost = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<SpaceSails.Client.Pages.Stations.TrackingPost?> _trackingPostSet { get; set; } = default!;
    private SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPost { get => _trackingPostValue; set { _trackingPostValue = value; _trackingPostSet(value); } }
    [Parameter] public TransponderMode _transponderMode { get; set; } = default!;
    [Parameter] public Action CenterShipOnMap { get; set; } = default!;
    [Parameter] public Action<AreaScanCoverage> OnAreaScanCovered { get; set; } = default!;
    [Parameter] public Action<bool> SetActiveRadar { get; set; } = default!;
    [Parameter] public Action<string> SetInterestTarget { get; set; } = default!;
    [Parameter] public Action<TransponderMode> SetTransponder { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Func<ShipDesk, Task> SwitchDeskFromClick { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate>> TrackingCandidates { get; set; } = default!;
    [Parameter] public Action<double> ZoomSensorsBackdrop { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
