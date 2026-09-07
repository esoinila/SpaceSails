using Microsoft.AspNetCore.Components;
using DestPassInfo = SpaceSails.Client.Pages.Map.DestPassInfo;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ColumnFoot — the code-behind for ColumnFoot.razor.
//
// #251 · the foot owns the flow column's last two items: the story plate and the navigation-target panel,
// each of which takes its own measured height off the bottom of the window. Its members live in a .cs file
// beside the component because the razor generator's output is NOT ANALYSED: a finding inside an
// `@code { … }` block is a finding nobody is ever shown.
public partial class ColumnFoot
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is what let the
    // gates and the invocations move out of the column without a single character of them changing, and
    // what lets the suite's source guards read them through MapMarkup exactly as they read them when they
    // lived in the page. A parameter that needs saying more than that says it on its own line.

    /// <summary>read by the GATE the plot ribbon came with — `@if (_activeDesk == ShipDesk.Nav)`, the desk the destination panel belongs to.</summary>
    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public string? _armedOrbitBodyId { get; set; }
    [Parameter] public string? _destinationBodyId { get; set; }
    [Parameter] public ClosestApproach.Pass? _destinationPass { get; set; }
    [Parameter] public string? _longHaulClearanceBlock { get; set; }
    [Parameter] public LongHaul.Departure? _longHaulDeparture { get; set; }
    [Parameter] public LongHaul.Reach? _longHaulReach { get; set; }
    /// <summary>read by the GATE the plate came with — `@if (_storyPlate is { } flash)`, which is where the plate's own `flash` comes from.</summary>
    [Parameter] public (StoryBeats.Beat Beat, string? Subject, double UntilSimTime)? _storyPlate { get; set; }
    [Parameter] public Func<string?, string> ArmMenuHint { get; set; } = default!;
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public Action CloseStoryPlate { get; set; } = default!;
    [Parameter] public Func<ClosestApproach.Pass, DestPassInfo?> DestinationPassInfo { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public string DockNavLockTip { get; set; } = default!;
    [Parameter] public EventCallback EngageLongHaul { get; set; }
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<string?, HarborClass> HarborClassOf { get; set; } = default!;
    [Parameter] public Func<CelestialBody, double> LongHaulCaptureRange { get; set; } = default!;
    [Parameter] public Func<int> LongHaulLastMilePulses { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure?, CelestialBody, string?, string?> LongHaulOfferBlock { get; set; } = default!;
    [Parameter] public Func<string?, CelestialBody?> LongHaulTargetPlanet { get; set; } = default!;
    [Parameter] public bool NavLockedByDock { get; set; }
    /// <summary>read by the GATE the plot ribbon came with, together with `_destinationBodyId is not null`.</summary>
    [Parameter] public bool PlotMode { get; set; }
    [Parameter] public Action<ClosestApproach.Pass> ScrubToDestinationPass { get; set; } = default!;
    [Parameter] public Action<string?> SetDestination { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Func<StoryBeats.Beat, string?, (string Title, string Art, string Caption)> StoryBeatCopy { get; set; } = default!;
    [Parameter] public Action<string> ToggleArmedInsertion { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
