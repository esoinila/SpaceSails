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

namespace SpaceSails.Client.Pages.Stations;

// SensorTaskQueue — the code-behind for SensorTaskQueue.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of SensorTaskQueue.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class SensorTaskQueue
{
    // #251 · Every [Parameter] below is a member of TrackingPost under the member's own name.
    // A parameter that needs saying more than that says it on its own line.

    [Parameter] public TelescopeSchedule _schedule { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Func<SensorTaskKind, string> KindIcon { get; set; } = default!;
    [Parameter] public Func<SensorTaskState, string> StateChipClass { get; set; } = default!;
    [Parameter] public Func<SensorTask, string> TaskCostText { get; set; } = default!;
    [Parameter] public Action<string> AbandonSearch { get; set; } = default!;
}
