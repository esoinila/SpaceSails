using Microsoft.AspNetCore.Components;

namespace SpaceSails.Client.Pages;

// ScuttleEpitaphCard — the code-behind for ScuttleEpitaphCard.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of ScuttleEpitaphCard.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class ScuttleEpitaphCard
{
    /// <summary>The sentence the ship's end wrote, bound out of the page's `_scuttleEpitaph`.</summary>
    [Parameter] public string epitaph { get; set; } = "";

    /// <summary>Was anything still alive aboard when she went. Names the page's own field.</summary>
    [Parameter] public bool _scuttleHeardIt { get; set; }

    /// <summary>The page's `Dismiss(Action)` — the one way out every card in this game uses.</summary>
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;

    /// <summary>The page's `CloseScuttleEpitaph()`, handed on as the action `Dismiss` runs.</summary>
    [Parameter] public Action CloseScuttleEpitaph { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
