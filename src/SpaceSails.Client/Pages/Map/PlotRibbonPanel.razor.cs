using Microsoft.AspNetCore.Components;
using DestPassInfo = SpaceSails.Client.Pages.Map.DestPassInfo;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// PlotRibbonPanel — the code-behind for PlotRibbonPanel.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of PlotRibbonPanel.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class PlotRibbonPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public string? _armedOrbitBodyId { get; set; }
    /// <summary>the page's `string? _destinationBodyId`, never null here: the `@if (PlotMode &&
    /// _destinationBodyId is not null)` that stays in the page is what decides this card renders at all,
    /// and that flow narrowing does not cross a component boundary.</summary>
    [Parameter] public string _destinationBodyId { get; set; } = default!;
    [Parameter] public ClosestApproach.Pass? _destinationPass { get; set; }
    [Parameter] public string? _longHaulClearanceBlock { get; set; }
    [Parameter] public LongHaul.Departure? _longHaulDeparture { get; set; }
    [Parameter] public LongHaul.Reach? _longHaulReach { get; set; }
    [Parameter] public Func<string?, string> ArmMenuHint { get; set; } = default!;
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public Func<ClosestApproach.Pass, DestPassInfo?> DestinationPassInfo { get; set; } = default!;
    [Parameter] public string DockNavLockTip { get; set; } = default!;
    [Parameter] public EventCallback EngageLongHaul { get; set; }
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<string?, HarborClass> HarborClassOf { get; set; } = default!;
    [Parameter] public Func<CelestialBody, double> LongHaulCaptureRange { get; set; } = default!;
    [Parameter] public Func<int> LongHaulLastMilePulses { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure?, CelestialBody, string?, string?> LongHaulOfferBlock { get; set; } = default!;
    [Parameter] public Func<string?, CelestialBody?> LongHaulTargetPlanet { get; set; } = default!;
    [Parameter] public bool NavLockedByDock { get; set; }
    [Parameter] public Action<ClosestApproach.Pass> ScrubToDestinationPass { get; set; } = default!;
    [Parameter] public Action<string?> SetDestination { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Action<string> ToggleArmedInsertion { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
