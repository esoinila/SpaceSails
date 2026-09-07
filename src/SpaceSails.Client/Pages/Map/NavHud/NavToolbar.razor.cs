using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// NavToolbar — the code-behind for NavToolbar.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class NavToolbar
{
    [Parameter] public string? _destinationBodyId { get; set; }
    [Parameter] public DockAffordance _dockAffordance { get; set; } = default!;
    [Parameter] public string? _dockedHavenId { get; set; }
    [Parameter] public bool _followDest { get; set; }
    [Parameter] public string? _longHaulClearanceBlock { get; set; }
    [Parameter] public LongHaul.Departure? _longHaulDeparture { get; set; }
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public bool _showTutorial { get; set; }
    [Parameter] public bool _skipActive { get; set; }
    [Parameter] public WarpSkip.NextEvent _skipNext { get; set; } = default!;
    [Parameter] public Func<int> ActiveTutorialIndex { get; set; } = default!;
    [Parameter] public bool CanFollowDestination { get; set; }
    [Parameter] public Func<string> EmergencyUndockTip { get; set; } = default!;
    [Parameter] public EventCallback EngageLongHaul { get; set; }
    [Parameter] public Func<string> FollowDestTip { get; set; } = default!;
    [Parameter] public bool FollowShip { get; set; }
    [Parameter] public string LongCoastAheadReadout { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure?, CelestialBody, string?, string?> LongHaulOfferBlock { get; set; } = default!;
    [Parameter] public Func<string?, CelestialBody?> LongHaulTargetPlanet { get; set; } = default!;
    [Parameter] public EventCallback MatchAndClamp { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnWarpSliderInput { get; set; }
    [Parameter] public bool Paused { get; set; }
    [Parameter] public Func<string> PlotButtonTip { get; set; } = default!;
    [Parameter] public bool PlotMode { get; set; }
    [Parameter] public Func<Action, Task> PressAndRefocus { get; set; } = default!;
    [Parameter] public bool ShowLongCoastAdvert { get; set; }
    [Parameter] public EventCallback StartSkip { get; set; }
    [Parameter] public EventCallback ToggleDock { get; set; }
    [Parameter] public EventCallback ToggleFollow { get; set; }
    [Parameter] public EventCallback ToggleFollowDest { get; set; }
    [Parameter] public Action ToggleNavHelp { get; set; } = default!;
    [Parameter] public EventCallback TogglePause { get; set; }
    [Parameter] public EventCallback TogglePlotMode { get; set; }
    [Parameter] public EventCallback ToggleSkip { get; set; }
    [Parameter] public EventCallback ToggleTutorial { get; set; }
    [Parameter] public EventCallback Undock { get; set; }
    [Parameter] public string WarpReadout { get; set; } = default!;
    [Parameter] public int WarpSliderValue { get; set; }
    [Parameter] public Action<bool> OnZoomStep { get; set; } = default!;
    private void ZoomStep(bool zoomIn) => OnZoomStep(zoomIn);

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
