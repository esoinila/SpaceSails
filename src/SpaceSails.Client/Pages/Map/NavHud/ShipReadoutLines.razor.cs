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
using ArriveStep = SpaceSails.Client.Pages.Map.ArriveStep;
using FlightEditorKind = SpaceSails.Client.Pages.Map.FlightEditorKind;
using FrameOption = SpaceSails.Client.Pages.Map.FrameOption;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using OrbitAssistInfo = SpaceSails.Client.Pages.Map.OrbitAssistInfo;
using PlanNode = SpaceSails.Client.Pages.Map.PlanNode;
using Quest = SpaceSails.Client.Pages.Map.Quest;
using SkimGauge = SpaceSails.Client.Pages.Map.SkimGauge;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// ShipReadoutLines — the code-behind for ShipReadoutLines.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class ShipReadoutLines
{
    [Parameter] public Camera _camera { get; set; } = default!;
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _cargoValue { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public int _effectiveWarp { get; set; }
    [Parameter] public PlasmaEnvironment? _plasma { get; set; }
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public string _scenarioName { get; set; } = default!;
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public bool _skipActive { get; set; }
    [Parameter] public double _skipTargetEpoch { get; set; }
    [Parameter] public string _skipTargetLabel { get; set; } = default!;
    [Parameter] public int CargoCapacity { get; set; }
    [Parameter] public double CircularSpeedHere { get; set; }
    [Parameter] public Func<double, string> FormatDuration { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<double, string> FormatZoom { get; set; } = default!;
    [Parameter] public bool Paused { get; set; }
    [Parameter] public int ReactionMassCapacity { get; set; }
    [Parameter] public double SimTime { get; set; }
    [Parameter] public int Warp { get; set; }

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
