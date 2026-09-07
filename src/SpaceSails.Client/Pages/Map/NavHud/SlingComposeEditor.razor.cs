using Microsoft.AspNetCore.Components;
using FlightEditorKind = SpaceSails.Client.Pages.Map.FlightEditorKind;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// SlingComposeEditor — the code-behind for SlingComposeEditor.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class SlingComposeEditor
{
    [Parameter] public FlightEditorKind _openEditor { get; set; } = default!;
    [Parameter] public ClosestApproach.Pass? _slingablePass { get; set; }
    [Parameter] public string? _slingFailure { get; set; }
    [Parameter] public double _slingPassRadii { get; set; }
    [Parameter] public SlingPlanner.Result? _slingResult { get; set; }
    [Parameter] public SlingPlanner.PassSide _slingSide { get; set; } = default!;
    [Parameter] public bool _slingSolving { get; set; }
    [Parameter] public EventCallback AddSlingBurn { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnSlingRadiiInput { get; set; }
    [Parameter] public EventCallback RunSlingSolveAsync { get; set; }
    [Parameter] public Action<SlingPlanner.PassSide> SetSlingSide { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingBurnLine { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingLeverLine { get; set; } = default!;
    [Parameter] public double SlingMaxRadii { get; set; }
    [Parameter] public double SlingMinRadii { get; set; }
    [Parameter] public Func<string> SlingNodeNoteNow { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingOutcomeLine { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingPassLine { get; set; } = default!;

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
