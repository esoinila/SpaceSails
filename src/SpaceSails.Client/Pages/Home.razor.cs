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

namespace SpaceSails.Client.Pages;

// Home — the code-behind for Home.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of Home.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class Home
{
    private static readonly (string Slug, string Title, string Tag, string Blurb)[] Scenarios =
    [
        ("sol", "Sol", "",
         "The real solar system, pure Newtonian. Nine worlds on rails, He3 haulers inbound from Saturn, " +
         "and Luna's mass drivers lobbing compute-core pods. Where every pirate learns the trade."),
    ];

    // Archived skies — kept for the labs that teach them, but no longer the maiden launch.
    // Collapsed into the "Archived scenarios" drawer on the front page (issue #258).
    private static readonly (string Slug, string Title, string Tag, string Blurb)[] Archived =
    [
        ("sol-eu", "Sol (Electric)", "EU ⚡",
         "The same sky, awake. Hull charge builds near the Sun and inside plasma streams; vent it or glow " +
         "on every sensor in the system. Ride the Saturn–Jupiter river — fast, bright, and watched."),
        ("wheel", "Wheel of the World", "EU ⚡",
         "The ancients' cosmology. Venus, Earth and Mars ride a rigid spoke around Saturn the hub. The " +
         "spoke is not gravity's work — but your ship still obeys Newton. Harder than it looks. It looks hard."),
        ("oops", "Sol (Miners' Folly)", "🌙",
         "The real Sol system, one accident later: a mining rig shorted its own capacitor and gave Luna " +
         "an unplanned +15% speed kick. Wider, more eccentric orbit, still bound, still Newtonian — fly it " +
         "and see. The wreck is still up there, and everyone calls it Miners' Folly now."),
    ];
}
