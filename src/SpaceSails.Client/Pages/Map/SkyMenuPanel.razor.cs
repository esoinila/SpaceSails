using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// SkyMenuPanel — the code-behind for SkyMenuPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of SkyMenuPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class SkyMenuPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public Func<double, double, int, (double X, double Y)> ClampMenu { get; set; } = default!;
    [Parameter] public Action CloseSkyMenu { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<(double X, double Y), string> MenuAnchorStyle { get; set; } = default!;
    [Parameter] public Func<Vector2d, CorridorRegion?> NearLaneFor { get; set; } = default!;
    [Parameter] public Action<Vector2d, double, string> ScanAreaFromMenu { get; set; } = default!;
    [Parameter] public Func<Vector2d, double, string> ScanCostText { get; set; } = default!;
    [Parameter] public Func<Vector2d, string> SkyScanLabel { get; set; } = default!;
    [Parameter] public Action<CorridorRegion, bool> OnSweepCorridorFromMenu { get; set; } = default!;
    private void SweepCorridorFromMenu(CorridorRegion lane, bool standing) => OnSweepCorridorFromMenu(lane, standing);
    [Parameter] public Vector2d skyPoint { get; set; } = default!;

    [Parameter] public double _skyMenuX { get; set; }
    [Parameter] public double _skyMenuY { get; set; }
    [Parameter] public double _skyMenuRadius { get; set; }
    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
