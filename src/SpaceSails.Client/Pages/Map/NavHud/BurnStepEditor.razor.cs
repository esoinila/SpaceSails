using Microsoft.AspNetCore.Components;
using PlanNode = SpaceSails.Client.Pages.Map.PlanNode;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// BurnStepEditor — the code-behind for BurnStepEditor.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class BurnStepEditor
{
    /// <summary>the plan step this editor edits — the page's `@foreach (PlanNode node in _planNodes)` variable.</summary>
    [Parameter] public PlanNode node { get; set; } = default!;
    /// <summary>the page's `bool _burnAngleAbsolute` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_burnAngleAbsolute` and the assignment still lands on the page.</summary>
    [Parameter] public bool _burnAngleAbsoluteValue { get; set; }
    /// <summary>The page's `_burnAngleAbsolute = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _burnAngleAbsoluteSet { get; set; } = default!;
    [Parameter] public Action<PlanNode> DeleteNode { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<double, double> HeadingAlongCourseAt { get; set; } = default!;
    [Parameter] public int MaxNodePulses { get; set; }
    [Parameter] public int MinNodePulses { get; set; }
    [Parameter] public Func<PlanNode, NodeDirection, bool> NodeAimedAlong { get; set; } = default!;
    [Parameter] public Action<PlanNode, int> NudgeNodeAim { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeNodeEpoch { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeNodePulses { get; set; } = default!;
    [Parameter] public Func<double, string> PlanningPrimaryName { get; set; } = default!;
    [Parameter] public Func<string> PlotFrameName { get; set; } = default!;
    [Parameter] public Action<PlanNode> RetimeToScrub { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetHeading { get; set; } = default!;
    [Parameter] public Action<PlanNode, NodeDirection> SetNodeDirection { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetPercent { get; set; } = default!;
    [Parameter] public Action<string?> SetPlotFrame { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetPulses { get; set; } = default!;
    [Parameter] public Func<double, (string? FrameId, string FrameName, string DestinationName)?> TripFrameOffer { get; set; } = default!;
    private bool _burnAngleAbsolute { get => _burnAngleAbsoluteValue; set { _burnAngleAbsoluteValue = value; _burnAngleAbsoluteSet(value); } }

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
