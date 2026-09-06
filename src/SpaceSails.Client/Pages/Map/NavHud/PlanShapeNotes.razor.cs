using Microsoft.AspNetCore.Components;

namespace SpaceSails.Client.Pages;

// PlanShapeNotes — the code-behind for PlanShapeNotes.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class PlanShapeNotes
{
    [Parameter] public bool NavLockedByDock { get; set; }
    [Parameter] public bool PlanListIsEmpty { get; set; }
    [Parameter] public Func<string?> PlanShapeWarningLine { get; set; } = default!;

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
