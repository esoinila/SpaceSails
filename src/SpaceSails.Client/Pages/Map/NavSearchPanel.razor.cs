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
using NavSearchRow = SpaceSails.Client.Pages.Map.NavSearchRow;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// NavSearchPanel — the code-behind for NavSearchPanel.razor.
//
// #1107 · A COMPONENT'S MEMBERS LIVE IN A .cs FILE, because the razor generator's output is NOT
// ANALYSED. Every line that lives inside an `@code { … }` block is invisible to the .NET analysers this
// repo runs with `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The
// moment the same characters sit in a `.cs` file beside the component, the analysers see them.
public partial class NavSearchPanel
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    /// <summary>the page's `ElementReference _navSearchInput` — and `@ref` in the moved markup WRITES it,
    /// which is the third spelling of an assignment (the compiler writes this one, as it writes
    /// `@bind`'s). The page is the reader: FocusNavSearch awaits `_navSearchInput.FocusAsync()` when the
    /// captain presses `/`. One-way, the capture would land here and the page's field would stay a default
    /// ElementReference — a `/` that throws in the browser and nothing in the suite that could say so.</summary>
    [Parameter] public ElementReference _navSearchInputValue { get; set; }
    /// <summary>The page's `_navSearchInput = value`, handed down so the capture survives the move.</summary>
    [Parameter] public Action<ElementReference> _navSearchInputSet { get; set; } = default!;
    private ElementReference _navSearchInput { get => _navSearchInputValue; set { _navSearchInputValue = value; _navSearchInputSet(value); } }

    /// <summary>the page's `bool _navSearchOpen` — written by the input's `@onfocus`, so it crosses as a
    /// pair too: the value in, the page's own setter out.</summary>
    [Parameter] public bool _navSearchOpenValue { get; set; }
    /// <summary>The page's `_navSearchOpen = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _navSearchOpenSet { get; set; } = default!;
    private bool _navSearchOpen { get => _navSearchOpenValue; set { _navSearchOpenValue = value; _navSearchOpenSet(value); } }

    [Parameter] public int _navSearchIndex { get; set; }
    [Parameter] public string _navSearchQuery { get; set; } = default!;
    [Parameter] public List<NavSearchRow> _navSearchRows { get; set; } = default!;
    [Parameter] public Func<NavSearchRow, Task> JumpToSearchResult { get; set; } = default!;
    [Parameter] public Func<NavSearchRow, string> NavSearchRowTip { get; set; } = default!;

    /// <summary>wired STRAIGHT to `@oninput`, so it crosses as an EventCallback and the PAGE is the
    /// receiver of the press rather than this surface (#1135 clause 2).</summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnNavSearchInput { get; set; }
    /// <summary>wired STRAIGHT to `@onkeydown`, same reason.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnNavSearchKeyDown { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
