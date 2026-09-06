using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// LiftPanel — the code-behind for LiftPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of LiftPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class LiftPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public string? _liftOutcome { get; set; }
    [Parameter] public Action CloseLiftPanel { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public EventCallback LiftPadClear { get; set; }
    [Parameter] public string LiftPadDisplay { get; set; } = default!;
    [Parameter] public bool LiftPadIsDark { get; set; }
    [Parameter] public Action<string> LiftPadPush { get; set; } = default!;
    [Parameter] public string? LiftPadSaid { get; set; }
    [Parameter] public Action<UndergroundComplex.LiftStop> LiftPadSubmit { get; set; } = default!;
    [Parameter] public Func<string> LiftPanelLine { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<UndergroundComplex.LiftStop>> LiftStops { get; set; } = default!;

    /// <summary>#719 slice 2 · Whether the car has been stopped under this captain. The page's own question
    /// (Map.Surface.Break.cs), asked here rather than inferred from an empty <see cref="LiftStops"/> — a
    /// panel that read "no rows" as "stopped" would paint the plate over any future silence, and the panel
    /// is not the place that knows what a silence means.</summary>
    [Parameter] public bool TheCarIsStopped { get; set; }
    [Parameter] public Action<UndergroundComplex.LiftStop> PressLiftButton { get; set; } = default!;
    [Parameter] public SurfaceExcursion liftEx { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
