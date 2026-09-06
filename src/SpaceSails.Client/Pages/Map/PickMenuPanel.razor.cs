using Microsoft.AspNetCore.Components;
using PickCandidate = SpaceSails.Client.Pages.Map.PickCandidate;

namespace SpaceSails.Client.Pages;

// PickMenuPanel — the code-behind for PickMenuPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of PickMenuPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class PickMenuPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public Func<double, double, int, (double X, double Y)> ClampMenu { get; set; } = default!;
    [Parameter] public Action ClosePickMenu { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<(double X, double Y), string> MenuAnchorStyle { get; set; } = default!;
    [Parameter] public EventCallback OpenLayersFromPick { get; set; }
    [Parameter] public Action<PickCandidate> OpenPickCandidate { get; set; } = default!;
    [Parameter] public Func<PickCandidate, string> PickHint { get; set; } = default!;
    [Parameter] public List<PickCandidate> pickList { get; set; } = default!;

    [Parameter] public double _pickMenuX { get; set; }
    [Parameter] public double _pickMenuY { get; set; }
    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
