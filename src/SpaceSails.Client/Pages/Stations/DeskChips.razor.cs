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
using SpaceSails.Client.Pages;

namespace SpaceSails.Client.Pages.Stations;

// DeskChips — the code-behind for DeskChips.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of DeskChips.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class DeskChips
{
    /// <summary>One desk's tightest objective summary — up to three short lines. Built by
    /// Map.razor from whatever state each desk's component already exposes (never raw stats
    /// dumps; see the addendum in docs/SaturdayPlan/StationDesks.md).</summary>
    public readonly record struct ChipData(ShipDesk Desk, string Icon, string Label, string Line1, string? Line2 = null, string? Line3 = null);

    [Parameter] public IReadOnlyList<ChipData> Chips { get; set; } = [];
    [Parameter] public EventCallback<ShipDesk> OnSelect { get; set; }

    private async Task OnChipKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e, ShipDesk desk)
    {
        if (e.Key is "Enter" or " ")
        {
            await OnSelect.InvokeAsync(desk);
        }
    }
}
