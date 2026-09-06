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
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// BodyMenuPanel — the code-behind for BodyMenuPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of BodyMenuPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class BodyMenuPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public string? _aerobrakeArmedBodyId { get; set; }
    [Parameter] public string? _armedOrbitBodyId { get; set; }
    [Parameter] public string? _destinationBodyId { get; set; }
    [Parameter] public ICelestialEphemeris? _ephemeris { get; set; }
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public Func<CelestialBody, Aerobrake.Quote?> AerobrakeMenuQuote { get; set; } = default!;
    [Parameter] public Func<string?, string> ArmMenuHint { get; set; } = default!;
    [Parameter] public Func<double, double, int, (double X, double Y)> ClampMenu { get; set; } = default!;
    [Parameter] public Action CloseBodyMenu { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public string DockNavLockTip { get; set; } = default!;
    [Parameter] public Action<string> EngageAerobrakeFromMenu { get; set; } = default!;
    [Parameter] public Func<string, Task> EngageLongHaulFromMenu { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<string?, HarborClass> HarborClassOf { get; set; } = default!;
    [Parameter] public Func<string?> KaamosCyclerBlock { get; set; } = default!;
    [Parameter] public Func<string> KaamosCyclerLabel { get; set; } = default!;
    [Parameter] public Func<string, bool> KaamosCyclerRowVisible { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure, CelestialBody, string?> LongHaulClearanceBlock { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure?, CelestialBody, string?, string?> LongHaulOfferBlock { get; set; } = default!;
    [Parameter] public Func<string?, CelestialBody?> LongHaulTargetPlanet { get; set; } = default!;
    [Parameter] public Func<(double X, double Y), string> MenuAnchorStyle { get; set; } = default!;
    [Parameter] public bool NavLockedByDock { get; set; }
    [Parameter] public EventCallback RideTheCyclerWindow { get; set; }
    [Parameter] public Action<Vector2d, double, string> ScanAreaFromMenu { get; set; } = default!;
    [Parameter] public Func<Vector2d, double, string> ScanCostText { get; set; } = default!;
    [Parameter] public Action<string?> SetDestination { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Action<string> ToggleArmedInsertionFromMenu { get; set; } = default!;
    [Parameter] public CelestialBody menuBody { get; set; } = default!;

    [Parameter] public double _bodyMenuX { get; set; }
    [Parameter] public double _bodyMenuY { get; set; }
    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
