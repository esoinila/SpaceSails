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
using PumpRun = SpaceSails.Client.Pages.Map.PumpRun;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// VentPanel — the code-behind for VentPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of VentPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class VentPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public int _refillCharges { get; set; }
    [Parameter] public bool _shipPumpOrder { get; set; }
    [Parameter] public bool _spinePressurised { get; set; }
    [Parameter] public double _spineVacuumSeconds { get; set; }
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    [Parameter] public List<string> _ventLog { get; set; } = default!;
    [Parameter] public string? _ventMessage { get; set; }
    [Parameter] public Dictionary<string, (DiceRoll Roll, HullVenting.LifeSign Sign)> _ventReads { get; set; } = default!;
    [Parameter] public string? _ventSelected { get; set; }
    [Parameter] public Func<string, IReadOnlyList<string>> AtmosphereAt { get; set; } = default!;
    [Parameter] public Func<string?> CaptainCompartment { get; set; } = default!;
    [Parameter] public Action CloseVentPanel { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<double, string> Du { get; set; } = default!;
    [Parameter] public Func<int> FloodCost { get; set; } = default!;
    [Parameter] public EventCallback FloodTheShip { get; set; }
    [Parameter] public EventCallback OrderWholeShipPumped { get; set; }
    [Parameter] public Func<IReadOnlyList<string>> PumpableRooms { get; set; } = default!;
    [Parameter] public EventCallback PumpEverySealedRoom { get; set; }
    [Parameter] public Func<string, PumpRun?> PumpOn { get; set; } = default!;
    [Parameter] public Action<string> ReadLifeSigns { get; set; } = default!;
    [Parameter] public Action<string> RefillCompartment { get; set; } = default!;
    [Parameter] public Action<string> RescueSurvivor { get; set; } = default!;
    [Parameter] public EventCallback SealTheShip { get; set; }
    [Parameter] public Action<string> SelectVentSpace { get; set; } = default!;
    [Parameter] public Func<string, HullVenting.Space> SpaceNow { get; set; } = default!;
    [Parameter] public bool SpinePumpable { get; set; }
    [Parameter] public Func<(string Text, string Class)> SpineTag { get; set; } = default!;
    [Parameter] public Action<string> StartPumpDown { get; set; } = default!;
    [Parameter] public EventCallback StartSpinePump { get; set; }
    [Parameter] public Action<string> StopPump { get; set; } = default!;
    [Parameter] public Action<string> ToggleVentDoor { get; set; } = default!;
    [Parameter] public EventCallback UnsealTheShip { get; set; }
    [Parameter] public Func<string, float, float, float, (string Text, string Class), float, string> OnVentAreaLabelSvg { get; set; } = default!;
    private string VentAreaLabelSvg(string label, float cx, float cy, float roomWidth,
        (string Text, string Class) tag, float roomHeight = 0f)
        => OnVentAreaLabelSvg(label, cx, cy, roomWidth, tag, roomHeight);
    [Parameter] public Func<string, HullVenting.Space, float, (string Text, string Class)> VentAreaTag { get; set; } = default!;
    [Parameter] public Action<string> VentCompartment { get; set; } = default!;
    [Parameter] public Func<float, float, float> VentDoorX { get; set; } = default!;
    [Parameter] public string VentHullOutline { get; set; } = default!;
    [Parameter] public string VentViewBox { get; set; } = default!;
    [Parameter] public double YearsOfVacuumSeconds { get; set; }
    [Parameter] public Derelict.Wreck ventWreck { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
