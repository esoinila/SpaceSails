using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

// ScopeControls — the code-behind for ScopeControls.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of ScopeControls.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class ScopeControls
{
    // #251 · Every [Parameter] below is a member of TrackingPost under the member's own name.
    // A parameter that needs saying more than that says it on its own line.

    [Parameter] public double _centerBearingDeg { get; set; }
    [Parameter] public double _arcWidthDeg { get; set; }
    /// <summary>the desk's `ScanJob? _activeJob` — which of the two sweep buttons is shown.</summary>
    [Parameter] public ScanJob? _activeJob { get; set; }
    [Parameter] public bool _passiveWatch { get; set; }
    [Parameter] public bool RadarActive { get; set; }
    [Parameter] public TransponderMode Transponder { get; set; }
    // #251 · THESE FIVE ARE EventCallbacks AND NOT Actions, AND THE DIFFERENCE IS VISIBLE.
    //
    // The moved markup writes them bare — `@oninput="OnBearingInput"`, `@onclick="StartSweep"` — and a
    // bare method group would bind fine as an Action. But Blazor re-renders the component that HANDLES an
    // event, and with an Action that component is this surface: the desk's own `_activeJob` and
    // `_passiveWatch` would change and the button showing them would go on showing the old word until
    // Map's 200 ms HUD throttle came round. Razor compiles a method group passed to an EventCallback
    // parameter as `EventCallback.Factory.Create(this, …)` with `this` the DESK, so the desk is the
    // receiver, the desk re-renders, and the button changes on the press exactly as it did before the cut.
    [Parameter] public EventCallback<ChangeEventArgs> OnBearingInput { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnArcInput { get; set; }
    [Parameter] public EventCallback StartSweep { get; set; }
    [Parameter] public EventCallback StopSweep { get; set; }
    [Parameter] public EventCallback TogglePassiveWatch { get; set; }
    [Parameter] public Func<string> TransponderHint { get; set; } = default!;
    [Parameter] public EventCallback<bool> OnRadarToggle { get; set; }
    [Parameter] public EventCallback<TransponderMode> OnTransponderChange { get; set; }
}
