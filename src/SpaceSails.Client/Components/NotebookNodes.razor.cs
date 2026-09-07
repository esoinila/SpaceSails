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
using SpaceSails.Core;

namespace SpaceSails.Client.Components;

// NotebookNodes — the code-behind for NotebookNodes.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of NotebookNodes.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class NotebookNodes
{
    /// <summary>The rows to draw, in the order Core put them.</summary>
    [Parameter] public IReadOnlyList<CaseThreads.Row> Rows { get; set; } = [];

    /// <summary>Whether the pen is out — which is what pressing a title MEANS, and nothing else.</summary>
    [Parameter] public bool PenInHand { get; set; }

    /// <summary>Whether the ground each entry came off is drawn on the node. True only in the case
    /// reading, where place is no longer the heading.</summary>
    [Parameter] public bool WithPlace { get; set; }

    /// <summary>Is this node open? Asked of the page, which keys it on the HANDLE — so a node stays open
    /// across the reorder a drawn line causes, and the reading does not close under the captain's eye.</summary>
    [Parameter] public Func<string, bool> IsOpen { get; set; } = _ => false;

    /// <summary>Is this the title already picked up, waiting for its other end?</summary>
    [Parameter] public Func<string, bool> IsHeld { get; set; } = _ => false;

    /// <summary>A title was pressed. What that means is the page's business.</summary>
    [Parameter] public EventCallback<string> OnTitle { get; set; }
}
