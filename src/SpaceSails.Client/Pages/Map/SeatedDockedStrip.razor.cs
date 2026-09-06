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
using TableTalk = SpaceSails.Client.Pages.Map.TableTalk;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// SeatedDockedStrip — the code-behind for SeatedDockedStrip.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of SeatedDockedStrip.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class SeatedDockedStrip
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public List<Core.Satchel.Item> _satchel { get; set; } = default!;
    [Parameter] public bool _seatedNewsOpen { get; set; }
    [Parameter] public bool ACabinetLeafToWork { get; set; }
    [Parameter] public string CabinetLeafHint { get; set; } = default!;
    [Parameter] public string CabinetLeafLabel { get; set; } = default!;
    [Parameter] public bool CanSpreadTheCaseHere { get; set; }
    [Parameter] public EventCallback OpenTheSpread { get; set; }
    [Parameter] public Func<double?> ProcessingFraction { get; set; } = default!;
    [Parameter] public Func<string?> ProcessingUnderway { get; set; } = default!;
    [Parameter] public Func<Core.Satchel.Item, string> SatchelLabel { get; set; } = default!;
    [Parameter] public Func<string?> SeatedCompanyLine { get; set; } = default!;
    [Parameter] public Func<string?> SeatedCustomerLine { get; set; } = default!;
    [Parameter] public Func<string?> SeatedOverheardLine { get; set; } = default!;
    [Parameter] public string SpreadDoorHint { get; set; } = default!;
    [Parameter] public string SpreadDoorLabel { get; set; } = default!;
    [Parameter] public Func<string, Task> TableMoveClicked { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> TableMoveIsUrged { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, bool> TableMoveOnOffer { get; set; } = default!;
    [Parameter] public Func<Encounter.Move, string> TableMoveRefusal { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Encounter.Move>> TableMovesOnTheTable { get; set; } = default!;
    [Parameter] public Action<Core.Satchel.Item> TableShow { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Core.Satchel.Item>> TableShowables { get; set; } = default!;
    [Parameter] public EventCallback ToggleSeatedNews { get; set; }
    [Parameter] public EventCallback WorkTheCabinetLeaf { get; set; }

    /// <summary>bound by the OUTER `@if (SeatedTable is { } tab)` that stays in the page</summary>
    [Parameter] public TableTalk tab { get; set; } = default!;
    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
