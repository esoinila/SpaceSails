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

// ArmedOrbitStepRow — the code-behind for ArmedOrbitStepRow.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class ArmedOrbitStepRow
{
    [Parameter] public int _armedBudgetPulses { get; set; }
    /// <summary>#1107 found-not-fixed #4, met a second time: FLOW NARROWING DOES NOT CROSS A COMPONENT
    /// BOUNDARY. NavHud's `@if (_armedOrbitBodyId is not null &amp;&amp; !ArriveCoversArmed)` is what made
    /// `string _armedId = _armedOrbitBodyId;` legal in the block below. The gate stays in the HUD — 63
    /// lines a ship not arriving anywhere never draws — so the compiler in here can no longer see the
    /// promise, and the parameter is declared `string` instead. The `@if` is what holds it.</summary>
    [Parameter] public string _armedOrbitBodyId { get; set; } = default!;
    [Parameter] public string? _armedTransferSummary { get; set; }
    [Parameter] public string? _disarmConfirmBodyId { get; set; }
    [Parameter] public int _keepTrimPulsesPerDay { get; set; }
    [Parameter] public FlightEditorKind _openEditor { get; set; } = default!;
    [Parameter] public bool _orbitKept { get; set; }
    [Parameter] public double? ArmedInsertionSimTime { get; set; }
    [Parameter] public bool AutopilotFlyingApproach { get; set; }
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public string DockNavLockTip { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<string?, HarborClass> HarborClassOf { get; set; } = default!;
    [Parameter] public bool NavLockedByDock { get; set; }
    [Parameter] public Action<string> ToggleArmedInsertion { get; set; } = default!;
    [Parameter] public EventCallback ToggleInsertionEditor { get; set; }

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
