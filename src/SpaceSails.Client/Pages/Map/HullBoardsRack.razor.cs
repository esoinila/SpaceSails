using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using ArchiveCard = SpaceSails.Client.Pages.Map.ArchiveCard;
using PumpRun = SpaceSails.Client.Pages.Map.PumpRun;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;

namespace SpaceSails.Client.Pages;

// HullBoardsRack — the code-behind for HullBoardsRack.razor.
//
// #251 · the rack owns the boards a captain inside a pressure hull can open, and the cards they raise: the
// scuttle panel and its epitaph, the life-sign read, the pressure door, the wreck's vent board, the
// archive vision, our own ship board and the charge board. Its members live in a .cs file beside the
// component because the razor generator's output is NOT ANALYSED: a finding inside an `@code { … }` block
// is a finding nobody is ever shown.
public partial class HullBoardsRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public ArchiveCard? _archiveCard { get; set; }
    [Parameter] public string? _chargeBoardMessage { get; set; }
    [Parameter] public bool _contactorOn { get; set; }
    [Parameter] public PlasmaEnvironment? _plasma { get; set; }
    [Parameter] public string? _pressureDoor { get; set; }
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public int _refillCharges { get; set; }
    [Parameter] public string? _scuttleEpitaph { get; set; }
    [Parameter] public bool _scuttleHeardIt { get; set; }
    [Parameter] public double? _scuttleSecondsLeft { get; set; }
    [Parameter] public List<string> _shipBoardLog { get; set; } = default!;
    [Parameter] public string? _shipBoardMessage { get; set; }
    [Parameter] public bool _shipPumpOrder { get; set; }
    [Parameter] public int _shipReserve { get; set; }
    [Parameter] public string? _shipSelected { get; set; }
    [Parameter] public bool _showChargeBoard { get; set; }
    [Parameter] public bool _showScuttlePanel { get; set; }
    [Parameter] public bool _showShipBoard { get; set; }
    [Parameter] public bool _showVentPanel { get; set; }
    [Parameter] public bool _spinePressurised { get; set; }
    [Parameter] public double _spineVacuumSeconds { get; set; }
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    [Parameter] public List<string> _ventLog { get; set; } = default!;
    [Parameter] public string? _ventMessage { get; set; }
    [Parameter] public string? _ventReadCard { get; set; }
    [Parameter] public Dictionary<string, (DiceRoll Roll, HullVenting.LifeSign Sign)> _ventReads { get; set; } = default!;
    [Parameter] public string? _ventSelected { get; set; }
    [Parameter] public Dictionary<string, HullVenting.Space> _ventSpaces { get; set; } = default!;
    [Parameter] public Derelict.Wreck? _wreck { get; set; }
    [Parameter] public EventCallback AbortTheOverload { get; set; } = default!;
    [Parameter] public Func<double> AmbientChargeNow { get; set; } = default!;
    [Parameter] public EventCallback ArmTheOverload { get; set; } = default!;
    [Parameter] public Func<string, IReadOnlyList<string>> AtmosphereAt { get; set; } = default!;
    [Parameter] public Func<string?> CaptainCompartment { get; set; } = default!;
    [Parameter] public Action CloseArchiveCard { get; set; } = default!;
    [Parameter] public Action CloseChargeBoard { get; set; } = default!;
    [Parameter] public Action ClosePressureDoorCard { get; set; } = default!;
    [Parameter] public Action CloseScuttleEpitaph { get; set; } = default!;
    [Parameter] public Action CloseScuttlePanel { get; set; } = default!;
    [Parameter] public Action CloseShipBoard { get; set; } = default!;
    [Parameter] public Action CloseVentPanel { get; set; } = default!;
    [Parameter] public Action CloseVentReadCard { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Func<double, string> Du { get; set; } = default!;
    [Parameter] public Action<string> EqualiseAtDoor { get; set; } = default!;
    [Parameter] public Func<int> FloodCost { get; set; } = default!;
    [Parameter] public EventCallback FloodTheShip { get; set; } = default!;
    [Parameter] public Action<string> GiveTheCaptainsWord { get; set; } = default!;
    [Parameter] public Func<double> HullChargeNow { get; set; } = default!;
    [Parameter] public EventCallback OrderWholeShipPumped { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<string>> PumpableRooms { get; set; } = default!;
    [Parameter] public EventCallback PumpEverySealedRoom { get; set; } = default!;
    [Parameter] public Func<string, PumpRun?> PumpOn { get; set; } = default!;
    [Parameter] public Action<string> ReadLifeSigns { get; set; } = default!;
    [Parameter] public Action<string> RefillCompartment { get; set; } = default!;
    [Parameter] public Action<string> RefillShipCompartment { get; set; } = default!;
    [Parameter] public Action<string> RescueSurvivor { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Scuttle.Method>> ScuttleMethods { get; set; } = default!;
    [Parameter] public EventCallback SealHerUp { get; set; } = default!;
    [Parameter] public EventCallback SealTheShip { get; set; } = default!;
    [Parameter] public Action<string> SelectShipSpace { get; set; } = default!;
    [Parameter] public Action<string> SelectVentSpace { get; set; } = default!;
    [Parameter] public Func<string, (string Text, string Class)> ShipAreaTag { get; set; } = default!;
    [Parameter] public Func<ShipAuthority.VentAuthority> ShipAuthorityNow { get; set; } = default!;
    [Parameter] public Func<string> ShipBreathingLine { get; set; } = default!;
    [Parameter] public Func<string?> ShipCompartment { get; set; } = default!;
    [Parameter] public bool ShipCorridorPressurised { get; set; }
    [Parameter] public Func<string> ShipCorridorTagSvg { get; set; } = default!;
    [Parameter] public string ShipHullOutline { get; set; } = default!;
    [Parameter] public Func<string, PumpRun?> ShipPumpOn { get; set; } = default!;
    [Parameter] public Func<string, HullVenting.Space> ShipSpaceNow { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<HullVenting.Space>> ShipSpacesNow { get; set; } = default!;
    [Parameter] public string ShipViewBox { get; set; } = default!;
    [Parameter] public Func<string, HullVenting.Space> SpaceNow { get; set; } = default!;
    [Parameter] public bool SpinePumpable { get; set; }
    [Parameter] public Func<(string Text, string Class)> SpineTag { get; set; } = default!;
    [Parameter] public Action<string> StartPumpDown { get; set; } = default!;
    [Parameter] public Action<string> StartShipPump { get; set; } = default!;
    [Parameter] public EventCallback StartSpinePump { get; set; } = default!;
    [Parameter] public Action<string> StopPump { get; set; } = default!;
    [Parameter] public Action<string> StopShipPump { get; set; } = default!;
    [Parameter] public EventCallback ToggleContactor { get; set; } = default!;
    [Parameter] public Action<string> ToggleSealAtHand { get; set; } = default!;
    [Parameter] public Action<string, bool> ToggleShipDoor { get; set; } = default!;
    [Parameter] public Action<string> ToggleVentDoor { get; set; } = default!;
    [Parameter] public EventCallback UnsealHer { get; set; } = default!;
    [Parameter] public EventCallback UnsealTheShip { get; set; } = default!;
    [Parameter] public Func<string, float, float, float, (string Text, string Class), float, string> VentAreaLabelSvg { get; set; } = default!;
    [Parameter] public Func<string, HullVenting.Space, float, (string Text, string Class)> VentAreaTag { get; set; } = default!;
    [Parameter] public EventCallback VentCharge { get; set; } = default!;
    [Parameter] public Action<string> VentCompartment { get; set; } = default!;
    [Parameter] public Func<float, float, float> VentDoorX { get; set; } = default!;
    [Parameter] public string VentHullOutline { get; set; } = default!;
    [Parameter] public Action<string> VentShipCompartment { get; set; } = default!;
    [Parameter] public string VentViewBox { get; set; } = default!;
    [Parameter] public EventCallback WithdrawTheCaptainsWord { get; set; } = default!;
    [Parameter] public double YearsOfVacuumSeconds { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
