using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// NavSearchChips — the code-behind for NavSearchChips.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of NavSearchChips.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class NavSearchChips
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    /// <summary>the page's `bool _layersOpen` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_layersOpen` and the assignment still lands on the page.</summary>
    [Parameter] public bool _layersOpenValue { get; set; }
    /// <summary>The page's `_layersOpen = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _layersOpenSet { get; set; } = default!;
    private bool _layersOpen { get => _layersOpenValue; set { _layersOpenValue = value; _layersOpenSet(value); } }
    [Parameter] public HashSet<string> CollapsedLayerGroups { get; set; } = default!;
    [Parameter] public HashSet<string> HiddenLayers { get; set; } = default!;
    [Parameter] public Func<string, bool> LayerVisible { get; set; } = default!;
    [Parameter] public EventCallback ResetLayersToDeskDefaults { get; set; }
    [Parameter] public Action<string> ToggleLayer { get; set; } = default!;
    [Parameter] public Action<MapLayerTree.Group> ToggleLayerGroup { get; set; } = default!;
    [Parameter] public Action<string> ToggleLayerGroupCollapsed { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
