using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;

namespace SpaceSails.Client.Pages;

// ChartMenusRack — the code-behind for ChartMenusRack.razor.
//
// #251 · the rack owns the three menus a click on the chart opens: the body menu, the ship menu and the
// open-sky menu. Its members live in a .cs file beside the component because the razor generator's output
// is NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
public partial class ChartMenusRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public string? _aerobrakeArmedBodyId { get; set; }
    [Parameter] public string? _armedOrbitBodyId { get; set; }
    [Parameter] public CelestialBody? _bodyMenuBody { get; set; }
    [Parameter] public double _bodyMenuX { get; set; }
    [Parameter] public double _bodyMenuY { get; set; }
    [Parameter] public string? _destinationBodyId { get; set; }
    [Parameter] public ICelestialEphemeris? _ephemeris { get; set; }
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public string? _shipMenuId { get; set; }
    [Parameter] public double _shipMenuX { get; set; }
    [Parameter] public double _shipMenuY { get; set; }
    [Parameter] public double _skyMenuRadius { get; set; }
    [Parameter] public Vector2d? _skyMenuWorld { get; set; }
    [Parameter] public double _skyMenuX { get; set; }
    [Parameter] public double _skyMenuY { get; set; }
    [Parameter] public Func<CelestialBody, Aerobrake.Quote?> AerobrakeMenuQuote { get; set; } = default!;
    [Parameter] public Func<string?, string> ArmMenuHint { get; set; } = default!;
    [Parameter] public Func<double, double, int, (double X, double Y)> ClampMenu { get; set; } = default!;
    [Parameter] public Action CloseBodyMenu { get; set; } = default!;
    [Parameter] public Action CloseShipMenu { get; set; } = default!;
    [Parameter] public Action CloseSkyMenu { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public string DockNavLockTip { get; set; } = default!;
    [Parameter] public Action<string> EngageAerobrakeFromMenu { get; set; } = default!;
    [Parameter] public Func<string, Task> EngageLongHaulFromMenu { get; set; } = default!;
    [Parameter] public Func<string, NpcState?> FindNpc { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<string?, HarborClass> HarborClassOf { get; set; } = default!;
    [Parameter] public Action<string> InterestFromMenu { get; set; } = default!;
    [Parameter] public Func<string?> KaamosCyclerBlock { get; set; } = default!;
    [Parameter] public Func<string> KaamosCyclerLabel { get; set; } = default!;
    [Parameter] public Func<string, bool> KaamosCyclerRowVisible { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure, CelestialBody, string?> LongHaulClearanceBlock { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure?, CelestialBody, string?, string?> LongHaulOfferBlock { get; set; } = default!;
    [Parameter] public Func<string?, CelestialBody?> LongHaulTargetPlanet { get; set; } = default!;
    [Parameter] public Func<(double X, double Y), string> MenuAnchorStyle { get; set; } = default!;
    [Parameter] public bool NavLockedByDock { get; set; }
    [Parameter] public Func<Vector2d, CorridorRegion?> NearLaneFor { get; set; } = default!;
    [Parameter] public EventCallback RideTheCyclerWindow { get; set; } = default!;
    [Parameter] public Action<Vector2d, double, string> ScanAreaFromMenu { get; set; } = default!;
    [Parameter] public Func<Vector2d, double, string> ScanCostText { get; set; } = default!;
    [Parameter] public Action<string?> SetDestination { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Func<Vector2d, string> SkyScanLabel { get; set; } = default!;
    [Parameter] public Action<CorridorRegion, bool> SweepCorridorFromMenu { get; set; } = default!;
    [Parameter] public Action<string> ToggleArmedInsertionFromMenu { get; set; } = default!;
    [Parameter] public Action<string> TrackShipFromMenu { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
