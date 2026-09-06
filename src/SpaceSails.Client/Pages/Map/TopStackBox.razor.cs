using Microsoft.AspNetCore.Components;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using SpaceSails.Core;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;

namespace SpaceSails.Client.Pages;

// TopStackBox — the code-behind for TopStackBox.razor.
//
// #251 · the box owns the head of the flow column: the desk tab bar and the pilot banner with the ship
// alert strip under it. Its members live in a .cs file beside the component because the razor generator's
// output is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
public partial class TopStackBox
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is what let the two
    // gates and the two invocations move out of the column without a single character of them changing, and
    // what lets the suite's source guards read them through MapMarkup exactly as they read them when they
    // lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public string? _armedOrbitBodyId { get; set; }
    [Parameter] public int _bannerRowOffset { get; set; }
    /// <summary>read by a GATE inside the box (`@if (_worldReady && !_deckMode)`), not by either child of it.</summary>
    [Parameter] public bool _deckMode { get; set; }
    [Parameter] public string? _dockReadyStatus { get; set; }
    [Parameter] public bool _peekMap { get; set; }
    [Parameter] public ShipAlerts _shipAlerts { get; set; } = default!;
    /// <summary>read by a GATE inside the box (`@if (_surface is null)`) — on a surface excursion the ship's tabs are not the captain's instruments (#330).</summary>
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    /// <summary>read by a GATE inside the box (`@if (_worldReady && !_deckMode)`), not by either child of it.</summary>
    [Parameter] public bool _worldReady { get; set; }
    [Parameter] public Action<AlertKind> AcknowledgeAlert { get; set; } = default!;
    [Parameter] public EventCallback AuthorizePlunder { get; set; }
    [Parameter] public bool AutopilotStoodDown { get; set; }
    [Parameter] public EventCallback BannerPageDown { get; set; }
    [Parameter] public EventCallback BannerPageUp { get; set; }
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public EventCallback DeclinePlunder { get; set; }
    [Parameter] public Func<ShipDesk, string> DeskKeyLabel { get; set; } = default!;
    [Parameter] public Func<ShipDesk, string> DeskLabel { get; set; } = default!;
    [Parameter] public Func<FlightPlanStatus> FlightNowNext { get; set; } = default!;
    [Parameter] public Func<ShipDesk, Task> SwitchDeskFromClick { get; set; } = default!;
    [Parameter] public ShipDesk[] TabBarOrder { get; set; } = default!;
    [Parameter] public EventCallback TogglePeekMapFromClick { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
