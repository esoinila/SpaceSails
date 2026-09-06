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

// PlotScrubControls — the code-behind for PlotScrubControls.razor.
//
// #1107/#1140 · the members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
//
// Every [Parameter] below is a member of NavHud under THE MEMBER'S OWN NAME — a field keeps its
// underscore, a method arrives as a delegate with its own signature, a variable NavHud's `@if` or
// `@foreach` binds arrives under the name that binding gave it. That is the whole trick: it is what let
// the markup move out of NavHud.razor without a single character of it changing.
public partial class PlotScrubControls
{
    [Parameter] public ClosestApproach.Pass? _closestPass { get; set; }
    [Parameter] public string _horizonChoice { get; set; } = default!;
    /// <summary>the page's `double _scrubOffsetSeconds` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_scrubOffsetSeconds` and the assignment still lands on the page.</summary>
    [Parameter] public double _scrubOffsetSecondsValue { get; set; }
    /// <summary>The page's `_scrubOffsetSeconds = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<double> _scrubOffsetSecondsSet { get; set; } = default!;
    [Parameter] public double CurrentPlotHorizonSeconds { get; set; }
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatHorizon { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public int HorizonSliderValue { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnHorizonSliderInput { get; set; }
    [Parameter] public Func<(string Text, bool Warn)?> RibbonHorizonNote { get; set; } = default!;
    [Parameter] public double ScrubTime { get; set; }
    [Parameter] public EventCallback SetHorizonAuto { get; set; }
    private double _scrubOffsetSeconds { get => _scrubOffsetSecondsValue; set { _scrubOffsetSecondsValue = value; _scrubOffsetSecondsSet(value); } }

    // NavHud's own event dispatch, repeated: no automatic re-render per event. See the header.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
