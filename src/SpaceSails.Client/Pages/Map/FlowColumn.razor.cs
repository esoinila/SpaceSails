using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ArriveStep = SpaceSails.Client.Pages.Map.ArriveStep;
using CargoManifestEntry = SpaceSails.Client.Pages.Map.CargoManifestEntry;
using DestPassInfo = SpaceSails.Client.Pages.Map.DestPassInfo;
using FlightEditorKind = SpaceSails.Client.Pages.Map.FlightEditorKind;
using FrameOption = SpaceSails.Client.Pages.Map.FrameOption;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using OrbitAssistInfo = SpaceSails.Client.Pages.Map.OrbitAssistInfo;
using PlanNode = SpaceSails.Client.Pages.Map.PlanNode;
using Quest = SpaceSails.Client.Pages.Map.Quest;
using ScopeIntel = SpaceSails.Client.Pages.Map.ScopeIntel;
using ShipBot = SpaceSails.Client.Pages.Map.ShipBot;
using SkimGauge = SpaceSails.Client.Pages.Map.SkimGauge;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SurfaceExcursion = SpaceSails.Client.Pages.Map.SurfaceExcursion;

namespace SpaceSails.Client.Pages;

// FlowColumn — the code-behind for FlowColumn.razor.
//
// #251 · the column owns the page's whole in-flow layout: the masthead box at its head, the Nav HUD and
// the desks in its greedy middle, and the two boxes that take their own measured height off the foot of
// the window. Its members live in a .cs file beside the component because the razor generator's output is
// NOT ANALYSED: a finding inside an `@code { … }` block is a finding nobody is ever shown.
public partial class FlowColumn
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME — a field
    // keeps its underscore, a method arrives as a delegate with its own signature — and is handed straight
    // on to the child of the column that read it from the page before the cut. That is the whole trick of
    // this refactor: it is what let the column and its five children move out of Map.razor without a single
    // character of them changing, and what lets the suite's source guards read them through MapMarkup
    // exactly as they read them when they lived in the page. A parameter that needs saying more than that
    // says it on its own line.

    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public bool _activeRadar { get; set; }
    [Parameter] public string? _activeThreadId { get; set; }
    [Parameter] public int _ancientCharges { get; set; }
    [Parameter] public int _armedBudgetPulses { get; set; }
    [Parameter] public string? _armedOrbitBodyId { get; set; }
    [Parameter] public string? _armedTransferSummary { get; set; }
    [Parameter] public ArriveStep? _arrive { get; set; }
    [Parameter] public int _bannerRowOffset { get; set; }
    /// <summary>the page's `_burnAngleAbsolute = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<bool> _burnAngleAbsoluteSet { get; set; } = default!;
    /// <summary>the page's `bool _burnAngleAbsolute` — the burn editor's aim toggle WRITES it, so it crosses as a pair.</summary>
    [Parameter] public bool _burnAngleAbsoluteValue { get; set; }
    [Parameter] public CacheLedger _caches { get; set; } = default!;
    [Parameter] public Camera _camera { get; set; } = default!;
    /// <summary>the page's `_captainTab = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<SpaceSails.Client.Pages.Stations.Captain.CaptainView> _captainTabSet { get; set; } = default!;
    /// <summary>the page's own captain-desk tab — the desk WRITES it, so it crosses as a pair.</summary>
    [Parameter] public SpaceSails.Client.Pages.Stations.Captain.CaptainView _captainTabValue { get; set; } = default!;
    [Parameter] public bool _captureEngaged { get; set; }
    [Parameter] public double _captureProgress { get; set; }
    [Parameter] public double _captureRequiredSeconds { get; set; }
    [Parameter] public string? _captureTargetCallsign { get; set; }
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _cargoValue { get; set; }
    [Parameter] public ClosestApproach.Pass? _closestPass { get; set; }
    [Parameter] public string? _commsActionMessage { get; set; }
    [Parameter] public string? _commsHailAnswer { get; set; }
    /// <summary>the page's `_commsSelectedId = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<string?> _commsSelectedIdSet { get; set; } = default!;
    /// <summary>the page's selected comms ship — the Comms desk WRITES it, so it crosses as a pair.</summary>
    [Parameter] public string? _commsSelectedIdValue { get; set; }
    [Parameter] public List<SpaceSails.Client.Pages.Stations.TrackingPost.CourseOpportunity> _courseOpportunities { get; set; } = default!;
    [Parameter] public bool _crashNoteCopied { get; set; }
    /// <summary>the page's `_credits = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<int> _creditsSet { get; set; } = default!;
    /// <summary>the page's `int _credits` — the Trade desk WRITES it and the Nav HUD READS it, so it crosses as a pair and both ends of the column see the private property below.</summary>
    [Parameter] public int _creditsValue { get; set; }
    [Parameter] public bool _dcNextActionCleared { get; set; }
    [Parameter] public bool _dcStandingOrder { get; set; }
    /// <summary>read by a GATE that moved with the column (`@if (_worldReady && !_deckMode)`), not by any child of it.</summary>
    [Parameter] public bool _deckMode { get; set; }
    [Parameter] public string? _destinationBodyId { get; set; }
    [Parameter] public ClosestApproach.Pass? _destinationPass { get; set; }
    [Parameter] public string? _disarmConfirmBodyId { get; set; }
    [Parameter] public DockAffordance _dockAffordance { get; set; } = default!;
    [Parameter] public string? _dockBodyId { get; set; }
    [Parameter] public bool _docked { get; set; }
    [Parameter] public string? _dockedHavenId { get; set; }
    [Parameter] public string? _dockReadyStatus { get; set; }
    [Parameter] public int _effectiveWarp { get; set; }
    [Parameter] public ICelestialEphemeris? _ephemeris { get; set; }
    [Parameter] public double _fireAimOffsetSeconds { get; set; }
    [Parameter] public double _fireAtSimTime { get; set; }
    [Parameter] public bool _fireAtWill { get; set; }
    [Parameter] public string? _fireBlockedBy { get; set; }
    [Parameter] public double _fireDispersionMeters { get; set; }
    [Parameter] public OrdnanceKind _fireKind { get; set; } = default!;
    [Parameter] public FireControl.Solution? _fireSolution { get; set; }
    [Parameter] public string? _fireTip { get; set; }
    [Parameter] public bool _followDest { get; set; }
    [Parameter] public bool _hasNetJammer { get; set; }
    [Parameter] public HeatState _heat { get; set; } = default!;
    [Parameter] public int _holdLevel { get; set; }
    [Parameter] public string _horizonChoice { get; set; } = default!;
    [Parameter] public IntelLedger _intelLedger { get; set; } = default!;
    [Parameter] public InterceptEstimate.Result? _intercept { get; set; }
    [Parameter] public string? _interestTargetId { get; set; }
    [Parameter] public int _keepTrimPulsesPerDay { get; set; }
    /// <summary>the page's `_localSpace = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<SpaceSails.Client.Pages.Stations.LocalSpace?> _localSpaceSet { get; set; } = default!;
    /// <summary>the page's captured `LocalSpace` — `@ref` is an assignment the compiler writes, so it crosses as a pair.</summary>
    [Parameter] public SpaceSails.Client.Pages.Stations.LocalSpace? _localSpaceValue { get; set; }
    [Parameter] public string? _localTradeMessage { get; set; }
    [Parameter] public double _localTradeProgress { get; set; }
    [Parameter] public string? _localTradeTargetId { get; set; }
    [Parameter] public string? _longHaulClearanceBlock { get; set; }
    [Parameter] public LongHaul.Departure? _longHaulDeparture { get; set; }
    [Parameter] public LongHaul.Reach? _longHaulReach { get; set; }
    [Parameter] public int _massLevel { get; set; }
    [Parameter] public int _missileAmmo { get; set; }
    [Parameter] public ShipMission _mission { get; set; } = default!;
    [Parameter] public MissionOptions _missionOptions { get; set; } = default!;
    [Parameter] public Vector2d _nearestBodyPosition { get; set; } = default!;
    [Parameter] public Vector2d _nearestBodyVelocity { get; set; } = default!;
    [Parameter] public CelestialBody? _nearestHaven { get; set; }
    [Parameter] public bool _openCaptainToTutorials { get; set; }
    [Parameter] public FlightEditorKind _openEditor { get; set; } = default!;
    [Parameter] public string? _orbitedBodyId { get; set; }
    [Parameter] public bool _orbitKept { get; set; }
    [Parameter] public bool _peekMap { get; set; }
    [Parameter] public bool _pinned { get; set; }
    [Parameter] public List<PlanNode> _planNodes { get; set; } = default!;
    [Parameter] public PlasmaEnvironment? _plasma { get; set; }
    [Parameter] public string? _plotFrameBodyId { get; set; }
    [Parameter] public string? _plunderOpportunityTargetId { get; set; }
    [Parameter] public PulseSlot _pulse { get; set; } = default!;
    [Parameter] public int _reactionMassPulses { get; set; }
    /// <summary>the page's `_renameDraft = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<string> _renameDraftSet { get; set; } = default!;
    /// <summary>the page's rename draft — the captain's berth-rename field WRITES it, so it crosses as a pair.</summary>
    [Parameter] public string _renameDraftValue { get; set; } = default!;
    [Parameter] public string? _renamingThreadId { get; set; }
    [Parameter] public int _revealedIterations { get; set; }
    [Parameter] public string _scenarioName { get; set; } = default!;
    [Parameter] public List<ScopeIntel> _scopeIntel { get; set; } = default!;
    /// <summary>the page's `_scrubOffsetSeconds = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<double> _scrubOffsetSecondsSet { get; set; } = default!;
    /// <summary>the page's `double _scrubOffsetSeconds` — the plot scrub `@bind`s it, so it crosses as a pair.</summary>
    [Parameter] public double _scrubOffsetSecondsValue { get; set; }
    [Parameter] public PlanNode? _selectedPlanNode { get; set; }
    [Parameter] public int _sensorLevel { get; set; }
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public ShipAlerts _shipAlerts { get; set; } = default!;
    [Parameter] public string? _shipAuthorized { get; set; }
    [Parameter] public List<ShipBot> _shipBots { get; set; } = default!;
    [Parameter] public bool _shotAuthorized { get; set; }
    [Parameter] public bool _showTutorial { get; set; }
    [Parameter] public double _skimAltKm { get; set; }
    [Parameter] public string? _skimFailure { get; set; }
    [Parameter] public ClosestApproach.Pass? _skimmablePass { get; set; }
    [Parameter] public SkimGauge? _skimResult { get; set; }
    [Parameter] public bool _skimSolving { get; set; }
    [Parameter] public bool _skipActive { get; set; }
    [Parameter] public WarpSkip.NextEvent _skipNext { get; set; } = default!;
    [Parameter] public double _skipTargetEpoch { get; set; }
    [Parameter] public string _skipTargetLabel { get; set; } = default!;
    [Parameter] public ClosestApproach.Pass? _slingablePass { get; set; }
    [Parameter] public string? _slingFailure { get; set; }
    [Parameter] public double _slingPassRadii { get; set; }
    [Parameter] public SlingPlanner.Result? _slingResult { get; set; }
    [Parameter] public SlingPlanner.PassSide _slingSide { get; set; } = default!;
    [Parameter] public bool _slingSolving { get; set; }
    [Parameter] public int _slugAmmo { get; set; }
    /// <summary>read by the GATE that moved with the column — `@if (_storyPlate is { } flash)`, which is where the plate's own `flash` comes from.</summary>
    [Parameter] public (StoryBeats.Beat Beat, string? Subject, double UntilSimTime)? _storyPlate { get; set; }
    /// <summary>read by a GATE that moved with the column (`@if (_surface is null)`), not by any child of it.</summary>
    [Parameter] public SurfaceExcursion? _surface { get; set; }
    [Parameter] public int _telescopeLevel { get; set; }
    /// <summary>the page's `_trackingPost = value`, handed on so the write survives the second move.</summary>
    [Parameter] public Action<SpaceSails.Client.Pages.Stations.TrackingPost?> _trackingPostSet { get; set; } = default!;
    /// <summary>the page's captured `TrackingPost` — `@ref` is an assignment the compiler writes, so it crosses as a pair, and the War Room reads the same private property.</summary>
    [Parameter] public SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPostValue { get; set; }
    [Parameter] public TransponderMode _transponderMode { get; set; } = default!;
    [Parameter] public bool _weaponsTight { get; set; }
    [Parameter] public int _workingStopsSinceShoreLeave { get; set; }
    /// <summary>read by a GATE that moved with the column (`@if (_worldReady && !_deckMode)`), not by any child of it.</summary>
    [Parameter] public bool _worldReady { get; set; }
}
