using Microsoft.AspNetCore.Components;
using PumpRun = SpaceSails.Client.Pages.Map.PumpRun;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ShipBoardPanel — the code-behind for ShipBoardPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of ShipBoardPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class ShipBoardPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public List<string> _shipBoardLog { get; set; } = default!;
    [Parameter] public string? _shipBoardMessage { get; set; }
    [Parameter] public int _shipReserve { get; set; }
    [Parameter] public string? _shipSelected { get; set; }
    [Parameter] public Action CloseShipBoard { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<double, string> Du { get; set; } = default!;
    [Parameter] public Action<string> GiveTheCaptainsWord { get; set; } = default!;
    [Parameter] public Action<string> RefillShipCompartment { get; set; } = default!;
    [Parameter] public EventCallback SealHerUp { get; set; }
    [Parameter] public Action<string> SelectShipSpace { get; set; } = default!;
    [Parameter] public Func<string, (string Text, string Class)> ShipAreaTag { get; set; } = default!;
    [Parameter] public Func<ShipAuthority.VentAuthority> ShipAuthorityNow { get; set; } = default!;
    [Parameter] public Func<string> ShipBreathingLine { get; set; } = default!;
    [Parameter] public Func<string?> ShipCompartment { get; set; } = default!;
    [Parameter] public bool ShipCorridorPressurised { get; set; }
    [Parameter] public Func<string> ShipCorridorTagSvg { get; set; } = default!;
    [Parameter] public string ShipHullOutline { get; set; } = default!;
    [Parameter] public Func<string, PumpRun?> ShipPumpOn { get; set; } = default!;
    [Parameter] public Func<string, HullVenting.Space> ShipSpaceNow { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<HullVenting.Space>> ShipSpacesNow { get; set; } = default!;
    [Parameter] public string ShipViewBox { get; set; } = default!;
    [Parameter] public Action<string> StartShipPump { get; set; } = default!;
    [Parameter] public Action<string> StopShipPump { get; set; } = default!;
    [Parameter] public Action<string, bool> ToggleShipDoor { get; set; } = default!;
    [Parameter] public EventCallback UnsealHer { get; set; }
    [Parameter] public Func<string, float, float, float, (string Text, string Class), float, string> OnVentAreaLabelSvg { get; set; } = default!;
    private string VentAreaLabelSvg(string label, float cx, float cy, float roomWidth,
        (string Text, string Class) tag, float roomHeight = 0f)
        => OnVentAreaLabelSvg(label, cx, cy, roomWidth, tag, roomHeight);
    [Parameter] public Action<string> VentShipCompartment { get; set; } = default!;
    [Parameter] public EventCallback WithdrawTheCaptainsWord { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
