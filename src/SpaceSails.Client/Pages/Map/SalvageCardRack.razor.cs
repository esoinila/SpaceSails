using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using KioskBuy = SpaceSails.Client.Pages.Map.KioskBuy;
using LockedDoorLook = SpaceSails.Client.Pages.Map.LockedDoorLook;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;
using WreckLook = SpaceSails.Client.Pages.Map.WreckLook;

namespace SpaceSails.Client.Pages;

// SalvageCardRack — the code-behind for SalvageCardRack.razor.
//
// #251 · the rack owns what a salvage and a walked hull put in front of you: our own scuttle panel, the
// wreck look, the wreck choice, the wreck outcome, the kiosk card, the lift panel and the locked door.
// Its members live in a .cs file beside the component because the razor generator's output is NOT
// ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
public partial class SalvageCardRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _cargoValue { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public KioskBuy? _kioskCard { get; set; }
    [Parameter] public string? _liftOutcome { get; set; }
    [Parameter] public LockedDoorLook? _lockedDoor { get; set; }
    [Parameter] public List<Core.Satchel.Item> _satchel { get; set; } = default!;
    [Parameter] public double? _shipChargesSeconds { get; set; }
    [Parameter] public string? _shipScuttleMessage { get; set; }
    [Parameter] public ShipScuttle.SecondKey? _shipScuttleSecondKey { get; set; }
    [Parameter] public bool _shipScuttleWordGiven { get; set; }
    [Parameter] public bool _showLiftPanel { get; set; }
    [Parameter] public bool _showShipScuttlePanel { get; set; }
    [Parameter] public bool _showWreckChoice { get; set; }
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    [Parameter] public Derelict.Wreck? _wreck { get; set; }
    [Parameter] public string? _wreckContact { get; set; }
    [Parameter] public HashSet<string> _wreckExamined { get; set; } = default!;
    [Parameter] public WreckLook? _wreckLook { get; set; }
    [Parameter] public Derelict.SalvageOutcome? _wreckOutcome { get; set; }
    /// <summary>the page's `_wreckReported = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<Derelict.WreckCause?> _wreckReportedSet { get; set; } = default!;
    /// <summary>the page's `Derelict.WreckCause? _wreckReported` — and this markup WRITES it, so it crosses as a pair: the value in, the page's own setter out.</summary>
    [Parameter] public Derelict.WreckCause? _wreckReportedValue { get; set; }
    [Parameter] public EventCallback AskTheCrewForTheSecondKey { get; set; } = default!;
    [Parameter] public EventCallback BackTheKeysOut { get; set; } = default!;
    [Parameter] public bool CanFileWreckReport { get; set; }
    [Parameter] public Action CloseKioskCard { get; set; } = default!;
    [Parameter] public Action CloseLiftPanel { get; set; } = default!;
    [Parameter] public Action CloseLockedDoor { get; set; } = default!;
    [Parameter] public Action CloseShipScuttlePanel { get; set; } = default!;
    [Parameter] public Action CloseWreckChoice { get; set; } = default!;
    [Parameter] public Action CloseWreckLook { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Action DismissWreckOutcome { get; set; } = default!;
    [Parameter] public EventCallback GiveTheWordAgainstHer { get; set; } = default!;
    [Parameter] public EventCallback LiftPadClear { get; set; } = default!;
    [Parameter] public string LiftPadDisplay { get; set; } = default!;
    [Parameter] public bool LiftPadIsDark { get; set; }
    [Parameter] public Action<string> LiftPadPush { get; set; } = default!;
    [Parameter] public string? LiftPadSaid { get; set; }
    [Parameter] public Action<UndergroundComplex.LiftStop> LiftPadSubmit { get; set; } = default!;
    [Parameter] public Func<string> LiftPanelLine { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<UndergroundComplex.LiftStop>> LiftStops { get; set; } = default!;
    [Parameter] public EventCallback OpenSatchelAtTheDoor { get; set; } = default!;
    [Parameter] public Action<UndergroundComplex.LiftStop> PressLiftButton { get; set; } = default!;
    [Parameter] public Action<Derelict.SalvageChoice> ResolveWreck { get; set; } = default!;
    [Parameter] public bool TheCarIsStopped { get; set; }
    [Parameter] public string TheClauseNonAnswer { get; set; } = default!;
    [Parameter] public EventCallback TurnBothKeys { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Derelict.WreckCause>> WreckCandidateCauses { get; set; } = default!;

    /// <summary>#251 · the member's OWN NAME, so the moved markup still reads and assigns `_wreckReported`
    /// and the assignment still lands on the page (NoSurfaceSwallowsAWriteTests).</summary>
    private Derelict.WreckCause? _wreckReported { get => _wreckReportedValue; set { _wreckReportedValue = value; _wreckReportedSet(value); } }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
