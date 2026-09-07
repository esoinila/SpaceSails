using Microsoft.AspNetCore.Components;
using PlanNode = SpaceSails.Client.Pages.Map.PlanNode;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// DepartureStepRow — the code-behind for DepartureStepRow.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class DepartureStepRow
{
    /// <summary>the plan step this row draws — the page's `@foreach (PlanNode node in _planNodes)` variable.</summary>
    [Parameter] public PlanNode node { get; set; } = default!;
    /// <summary>the row's state, computed by the page beside the `@foreach`.</summary>
    [Parameter] public FlightStepState _ds { get; set; } = default!;
    /// <summary>is this departure's editor the open one.</summary>
    [Parameter] public bool _dopen { get; set; }
    [Parameter] public PlanNode? _selectedPlanNode { get; set; }
    [Parameter] public Func<PlanNode, string> ClearanceEtaLine { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeDepartureEpoch { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeNodePulses { get; set; } = default!;
    [Parameter] public Func<PlanNode, string> PlanStepGlanceLine { get; set; } = default!;
    [Parameter] public EventCallback RemoveTheCastOff { get; set; }
    [Parameter] public Func<PlanNode, double> SeparationFromHarbour { get; set; } = default!;
    [Parameter] public Action<PlanNode> ToggleBurnEditor { get; set; } = default!;

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
