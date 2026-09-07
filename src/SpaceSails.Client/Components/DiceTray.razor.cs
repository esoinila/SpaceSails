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

// DiceTray — the code-behind for DiceTray.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of DiceTray.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class DiceTray
{
    /// <summary>The cast dice to show, or null when the tray is closed.</summary>
    [Parameter] public DiceEvent? Event { get; set; }

    /// <summary>Raised when the captain dismisses the tray (or clicks off it).</summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    private async Task Dismiss()
    {
        if (OnDismiss.HasDelegate)
        {
            await OnDismiss.InvokeAsync();
        }
    }
}
