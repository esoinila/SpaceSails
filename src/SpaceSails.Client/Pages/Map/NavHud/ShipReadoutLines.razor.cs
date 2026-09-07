using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ShipReadoutLines — the code-behind for ShipReadoutLines.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class ShipReadoutLines
{
    [Parameter] public Camera _camera { get; set; } = default!;
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _cargoValue { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public int _effectiveWarp { get; set; }
    [Parameter] public PlasmaEnvironment? _plasma { get; set; }
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public string _scenarioName { get; set; } = default!;
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public bool _skipActive { get; set; }
    [Parameter] public double _skipTargetEpoch { get; set; }
    [Parameter] public string _skipTargetLabel { get; set; } = default!;
    [Parameter] public int CargoCapacity { get; set; }
    [Parameter] public double CircularSpeedHere { get; set; }
    [Parameter] public Func<double, string> FormatDuration { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<double, string> FormatZoom { get; set; } = default!;
    [Parameter] public bool Paused { get; set; }
    [Parameter] public int ReactionMassCapacity { get; set; }
    [Parameter] public double SimTime { get; set; }
    [Parameter] public int Warp { get; set; }

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
