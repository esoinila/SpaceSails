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
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// CaptainsRemoteButton — the code-behind for CaptainsRemoteButton.razor.
//
// #1107 · A COMPONENT'S MEMBERS LIVE IN A .cs FILE, because the razor generator's output is NOT
// ANALYSED. Every line that lives inside an `@code { … }` block is invisible to the .NET analysers this
// repo runs with `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The
// moment the same characters sit in a `.cs` file beside the component, the analysers see them.
public partial class CaptainsRemoteButton
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature. That is the
    // whole trick of this refactor: it is what let the markup above move out of Map.razor without a single
    // character of it changing, and what lets the suite's source guards read it through MapMarkup exactly
    // as they read it when it lived in the page.
    //
    // SilentRunning.PanelTitle is not a parameter and must not become one: it is a constant on Core, read
    // by name in the moved markup exactly as it was read in the page.

    /// <summary>wired STRAIGHT to `@onclick`, so it crosses as an EventCallback and the PAGE is the
    /// receiver of the press (#1135 clause 2). The press sets `_showCaptainsRemote`, which is the flag the
    /// page's own `@if` reads to put CaptainsRemotePanel up.</summary>
    [Parameter] public EventCallback OpenCaptainsRemote { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
