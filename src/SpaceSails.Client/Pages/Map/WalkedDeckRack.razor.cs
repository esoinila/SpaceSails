using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using DesignateTarget = SpaceSails.Client.Pages.Map.DesignateTarget;
using PumpRun = SpaceSails.Client.Pages.Map.PumpRun;
using SurfaceBot = SpaceSails.Client.Pages.Map.SurfaceBot;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;
using Sweeper = SpaceSails.Client.Pages.Map.Sweeper;

namespace SpaceSails.Client.Pages;

// WalkedDeckRack — the code-behind for WalkedDeckRack.razor.
//
// #251 · the rack owns the six corners of a walked deck: the captain's remote (button and panel), the lab
// door board, the lab alarm panel, the one strip every running clock shares, the captain chip, and the
// heat line that replaces the chip on someone else's ground. Its members live in a .cs file beside the
// component because the razor generator's output is NOT ANALYSED: a finding inside an `@code { … }` block
// is a finding nobody is ever shown.
public partial class WalkedDeckRack
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature. That is the whole trick
    // of this refactor: it is what let the gates and the invocations above move out of Map.razor without a
    // single character of them changing, and what lets the suite's source guards read them through
    // MapMarkup exactly as they read them when they lived in the page. A parameter that needs saying more
    // than that says it on its own line.

    [Parameter] public string? _alarmOutcome { get; set; }
    [Parameter] public bool _boatOrderedCold { get; set; }
    [Parameter] public double _boatWarmth { get; set; }
    [Parameter] public bool _deckMode { get; set; }
    [Parameter] public bool _designateOpen { get; set; }
    [Parameter] public List<DesignateTarget> _designateTargets { get; set; } = default!;
    [Parameter] public string? _designateUnit { get; set; }
    [Parameter] public string? _doorBoardOutcome { get; set; }
    [Parameter] public bool _hasVantarCard { get; set; }
    [Parameter] public LabSecurity.State _labAlarm { get; set; } = default!;
    [Parameter] public Dictionary<string, LockedDoor.State> _labDoors { get; set; } = default!;
    [Parameter] public DiceRoll? _labHackRoll { get; set; }
    [Parameter] public Dictionary<string, PumpRun> _pumps { get; set; } = default!;
    [Parameter] public string? _remoteOutcome { get; set; }
    [Parameter] public double? _scuttleSecondsLeft { get; set; }
    [Parameter] public double? _shipChargesSeconds { get; set; }
    [Parameter] public Dictionary<string, PumpRun> _shipPumps { get; set; } = default!;
    [Parameter] public bool _showAlarmPanel { get; set; }
    [Parameter] public bool _showCaptainsRemote { get; set; }
    [Parameter] public bool _showDoorBoard { get; set; }
    [Parameter] public bool _showSaveDrawer { get; set; }
    [Parameter] public bool _showStartPicker { get; set; }
    [Parameter] public bool _soundQuietly { get; set; }
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    [Parameter] public List<Sweeper> _sweepers { get; set; } = default!;
    [Parameter] public bool _weaponsTight { get; set; }
    [Parameter] public bool _worldReady { get; set; }
    [Parameter] public GameThreadInfo? ActiveThreadInfo { get; set; }
    [Parameter] public bool AnyRoomSoaking { get; set; }
    [Parameter] public bool AnySweeperOnTheCaptain { get; set; }
    [Parameter] public Func<string?> AwayWindowRemoteLine { get; set; } = default!;
    [Parameter] public Func<string> AwayWindowRemoteSubLine { get; set; } = default!;
    [Parameter] public EventCallback BackToTheSwitches { get; set; } = default!;
    [Parameter] public bool BoatClockWorthShowing { get; set; }
    [Parameter] public double BoatSecondsLeft { get; set; }
    [Parameter] public SilentRunning.BoatState BoatState { get; set; } = default!;
    [Parameter] public Func<string, string> CaptainInitial { get; set; } = default!;
    [Parameter] public Func<string, string> CaptainMonoColor { get; set; } = default!;
    [Parameter] public Action CloseAlarmPanel { get; set; } = default!;
    [Parameter] public Action CloseCaptainsRemote { get; set; } = default!;
    [Parameter] public Action CloseDoorBoard { get; set; } = default!;
    [Parameter] public Func<LabSecurity.Approach> CurrentApproach { get; set; } = default!;
    [Parameter] public double? CutSecondsLeft { get; set; }
    [Parameter] public int CutsLeftInTheCell { get; set; }
    [Parameter] public Func<List<SurfaceBot>> DesignatableGuns { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Action<DesignateTarget> FireOnTheLock { get; set; } = default!;
    [Parameter] public EventCallback HackTheAlarm { get; set; } = default!;
    [Parameter] public double? LabAlarmSecondsLeft { get; set; }
    [Parameter] public int LabHackStack { get; set; }
    [Parameter] public bool OnWreck { get; set; }
    [Parameter] public EventCallback OpenCaptainsRemote { get; set; } = default!;
    [Parameter] public EventCallback OpenDesignate { get; set; } = default!;
    [Parameter] public Action<string> PickTheGun { get; set; } = default!;
    [Parameter] public EventCallback RaiseTheClaimsDesk { get; set; } = default!;
    [Parameter] public IEnumerable<(string Room, double Seconds)> RoomsSoaking { get; set; } = default!;
    [Parameter] public string? SearchBandLine { get; set; }
    [Parameter] public EventCallback SendTheStanding { get; set; } = default!;
    [Parameter] public HullSounding.Method SoundingGear { get; set; } = default!;
    [Parameter] public double? SoundingSecondsLeft { get; set; }
    [Parameter] public bool SweepersAboard { get; set; }
    [Parameter] public Func<bool> TheRemoteReachesAKiosk { get; set; } = default!;
    [Parameter] public bool TheyRememberYouHere { get; set; }
    [Parameter] public EventCallback ToggleBoatPower { get; set; } = default!;
    [Parameter] public EventCallback ToggleQuietSearch { get; set; } = default!;
    [Parameter] public EventCallback ToggleWeaponsTight { get; set; } = default!;
    [Parameter] public double? WarmCutSecondsLeft { get; set; }
    [Parameter] public Action<string> WorkTheDoor { get; set; } = default!;
    [Parameter] public InspectionTeam.Awareness WorstSweeperState { get; set; } = default!;

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
