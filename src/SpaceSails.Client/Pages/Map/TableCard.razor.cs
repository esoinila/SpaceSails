using Microsoft.AspNetCore.Components;
using TableTalk = SpaceSails.Client.Pages.Map.TableTalk;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// TableCard — the code-behind for TableCard.razor.
//
// #1107 · A COMPONENT'S MEMBERS LIVE IN A .cs FILE, because the razor generator's output is NOT
// ANALYSED. Every line that lives inside an `@code { … }` block is invisible to the .NET analysers this
// repo runs with `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The
// moment the same characters sit in a `.cs` file beside the component, the analysers see them.
public partial class TableCard
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public int _credits { get; set; }
    [Parameter] public List<Core.Satchel.Item> _satchel { get; set; } = default!;
    [Parameter] public Action CloseTable { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SatchelLabel { get; set; } = default!;
    [Parameter] public Func<string, Task> TableMoveClicked { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> TableMoveIsUrged { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> TableMoveOnOffer { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, string> TableMoveRefusal { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Encounter.Move>> TableMovesOnTheTable { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> TableShow { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.Satchel.Item>> TableShowables { get; set; } = default!;

    /// <summary>bound by the OUTER `@if (SeatedTable is { } tab)` that stays in the page — the same
    /// variable, under the same name, that SeatedDockedStrip takes on the other side of the fork.</summary>
    [Parameter] public TableTalk tab { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
