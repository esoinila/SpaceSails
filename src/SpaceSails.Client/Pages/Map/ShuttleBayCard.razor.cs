using Microsoft.AspNetCore.Components;
using AwayWindow = SpaceSails.Client.Pages.Map.AwayWindow;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using RouteWindowRow = SpaceSails.Client.Pages.Map.RouteWindowRow;
using ShuttleFarStop = SpaceSails.Client.Pages.Map.ShuttleFarStop;
using ShuttleStop = SpaceSails.Client.Pages.Map.ShuttleStop;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ShuttleBayCard — the code-behind for ShuttleBayCard.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of ShuttleBayCard.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class ShuttleBayCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public List<ShuttleFarStop> _shuttleBayFarStops { get; set; } = default!;
    [Parameter] public Func<string, double?> AwayReopenSeconds { get; set; } = default!;
    [Parameter] public Func<AwayWindow, string> AwayWindowWord { get; set; } = default!;
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public Action CloseShuttleBayDoor { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDuration { get; set; } = default!;
    [Parameter] public Action<ShuttleStop> OpenBoardingPanel { get; set; } = default!;
    [Parameter] public Func<string, double?> RouteReturnBySimTime { get; set; } = default!;
    [Parameter] public Func<RouteWindowRow, string> RouteWindowRowText { get; set; } = default!;
    [Parameter] public Func<double, string> ShuttleReachText { get; set; } = default!;
    [Parameter] public Func<List<RouteWindowRow>> ShuttleRouteWindowRows { get; set; } = default!;
    [Parameter] public Func<double, string> ShuttleSeparationText { get; set; } = default!;
    [Parameter] public Func<ShuttleExcursion.RangeTrend, string> ShuttleTrendWord { get; set; } = default!;
    [Parameter] public Action<ShuttleStop> TakeShuttleTo { get; set; } = default!;
    [Parameter] public Func<string, AwayWindow> WindowOn { get; set; } = default!;
    [Parameter] public List<ShuttleStop> shuttleStops { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
