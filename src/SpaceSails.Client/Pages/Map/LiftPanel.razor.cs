using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

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

    /// <summary>#1253 · What floor the car says it is on, under the title. It replaces the
    /// <c>SurfaceExcursion</c> this surface used to be handed — a whole excursion object, carried in for one
    /// integer, which is also exactly why this panel could not be drawn at a berth: <b>a haven has no
    /// excursion.</b> The page answers the one question the markup was really asking, and answers it for a
    /// moon and for a station out of the same method, so the sentence over the buttons and the buttons
    /// cannot come to two names for one floor.</summary>
    [Parameter] public Func<string> LiftPanelDepth { get; set; } = default!;

    /// <summary>
    /// #1280 · <b>IS THIS CAR IN A STATION RATHER THAN UNDER A MOON?</b> The one question that decides this
    /// panel's ROW SHAPE, asked once and answered by the page (<c>TheStationHasFloors</c>).
    ///
    /// <para>It is a MODE on the one surface and never a second panel. #1253 put a berth's three cars through
    /// this file precisely so the buttons a captain presses are one set of buttons; what it did not do is ask
    /// what a ROW is made of, so every haven row came out wearing the Hive's furniture — the regolith depth
    /// on the left and the dead-air column on the right. A moon's row has to carry both (how deep, and
    /// whether the trip is free); a station's row has neither fact in it, because a haven is two floors of
    /// one pressurised building and the button says the whole of what there is to say.</para>
    ///
    /// <para>What survives in a station is the plate and <i>◄ you are here</i>: what the floor is called, and
    /// which one you are standing on. That is the panel §0 of the testing links describes, and it is the
    /// panel with nothing on it that a station cannot mean.</para>
    /// </summary>
    [Parameter] public bool TheCarIsInAStation { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
