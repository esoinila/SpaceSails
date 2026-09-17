using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ConditionsStrip — the code-behind for ConditionsStrip.razor (#243).
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// One parameter, and it is a QUESTION rather than a state: the page answers "which gate is live, with what
// numbers" every time the strip draws, out of Core. There is no field here and none on the page either —
// the whole instrument is derived, which is why adding it moved no row of the frame fingerprint's sweep.
public partial class ConditionsStrip
{
    /// <summary>The one gate the strip speaks about this instant, or null — and null draws NOTHING, not an
    /// empty box (see the note in the markup).</summary>
    [Parameter] public Func<ConditionsReading?> ActiveConditions { get; set; } = default!;

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
