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
    [Parameter] public Action<AlertKind> AcknowledgeAlert { get; set; } = default!;
    [Parameter] public string ActiveCaptainName { get; set; } = default!;
    [Parameter] public Func<string, Quest?> ActiveCargoRunTo { get; set; } = default!;
    [Parameter] public Func<int> ActiveTutorialIndex { get; set; } = default!;
    [Parameter] public Action<ArrivalStepRule.ArrivalKind> AddArriveAtScrub { get; set; } = default!;
    [Parameter] public EventCallback AddBurnAtScrub { get; set; }
    [Parameter] public EventCallback AddCastOffAtTop { get; set; }
    [Parameter] public EventCallback AddSkimBurn { get; set; }
    [Parameter] public EventCallback AddSlingBurn { get; set; }
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DeskChips.ChipData>> AllStationChips { get; set; } = default!;
    [Parameter] public Action<PlanNode, int> ApplyPulses { get; set; } = default!;
    [Parameter] public double ArcChargeThreshold { get; set; }
    [Parameter] public EventCallback ArmArriveStep { get; set; }
    [Parameter] public bool ArmedArrivalStillAhead { get; set; }
    [Parameter] public double? ArmedInsertionSimTime { get; set; }
    [Parameter] public Action ArmFire { get; set; } = default!;
    [Parameter] public Func<string?, string> ArmMenuHint { get; set; } = default!;
    [Parameter] public Func<ArrivalStepRule.ArrivalKind, string> ArriveButtonLabel { get; set; } = default!;
    [Parameter] public Func<ArrivalStepRule.ArrivalKind, ClosestApproach.Pass?> ArriveCandidate { get; set; } = default!;
    [Parameter] public Func<ArrivalStepRule.ArrivalCheck?> ArriveCheck { get; set; } = default!;
    [Parameter] public bool ArriveCoversArmed { get; set; }
    [Parameter] public Func<ArriveStep, string> ArriveGlanceLine { get; set; } = default!;
    [Parameter] public Func<ArriveStep, bool> ArriveIsAThen { get; set; } = default!;
    [Parameter] public Func<string, ClosestApproach.Pass?> ArrivePassFor { get; set; } = default!;
    [Parameter] public Func<string?> ArrivePlanCompleteLine { get; set; } = default!;
    [Parameter] public Func<string?> ArriveRibbonTooShortLine { get; set; } = default!;
    [Parameter] public Action AuthorizeNextDamageControlAct { get; set; } = default!;
    [Parameter] public EventCallback AuthorizePlunder { get; set; }
    [Parameter] public Action AuthorizeShot { get; set; } = default!;
    [Parameter] public Action AutoAim { get; set; } = default!;
    [Parameter] public bool AutopilotFlyingApproach { get; set; }
    [Parameter] public bool AutopilotStoodDown { get; set; }
    [Parameter] public EventCallback BannerPageDown { get; set; }
    [Parameter] public EventCallback BannerPageUp { get; set; }
    [Parameter] public Action<string> BeginRenameCaptain { get; set; } = default!;
    [Parameter] public Func<string?> BestWireLenderId { get; set; } = default!;
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public Action<string> BribeShip { get; set; } = default!;
    [Parameter] public Func<PlanNode, string> BurnGlanceLine { get; set; } = default!;
    [Parameter] public Action<OrdnanceKind> BuyAmmo { get; set; } = default!;
    [Parameter] public Action<int> BuyFuel { get; set; } = default!;
    [Parameter] public EventCallback BuyNetJammer { get; set; }
    [Parameter] public Action BuyTheInspectorCard { get; set; } = default!;
    [Parameter] public Action<string> BuyUpgrade { get; set; } = default!;
    [Parameter] public EventCallback CallInFavorAtPump { get; set; }
    [Parameter] public Action CancelFiringSolution { get; set; } = default!;
    [Parameter] public Action CancelRenameCaptain { get; set; } = default!;
    [Parameter] public bool CanFollowDestination { get; set; }
    [Parameter] public Func<bool> CanWarnInterest { get; set; } = default!;
    [Parameter] public Func<CrashNote?> CaptainCrashNote { get; set; } = default!;
    [Parameter] public int CargoCapacity { get; set; }
    [Parameter] public Func<IReadOnlyList<CargoManifestEntry>> CargoManifest { get; set; } = default!;
    [Parameter] public Func<CelestialBody, string> CargoNextAction { get; set; } = default!;
    [Parameter] public Action CenterShipOnMap { get; set; } = default!;
    [Parameter] public Func<int?> ChipFencePrice { get; set; } = default!;
    [Parameter] public double CircularSpeedHere { get; set; }
    [Parameter] public Func<PlanNode, string> ClearanceEtaLine { get; set; } = default!;
    [Parameter] public Action CloseStoryPlate { get; set; } = default!;
    [Parameter] public Action CommitRenameCaptain { get; set; } = default!;
    [Parameter] public Func<NpcState, bool> CommsCanFence { get; set; } = default!;
    [Parameter] public Func<NpcState, int?> CommsFencePrice { get; set; } = default!;
    [Parameter] public Func<List<(string Label, List<NpcState> Members)>> CommsGroups { get; set; } = default!;
    [Parameter] public Action<NpcState> CommsHail { get; set; } = default!;
    [Parameter] public Action<NpcState> CommsLaserRange { get; set; } = default!;
    [Parameter] public Action<NpcState> CommsSellTrack { get; set; } = default!;
    [Parameter] public Func<NpcState, (string Label, string Css)> CommsStatusBadge { get; set; } = default!;
    [Parameter] public int CommsTickerAmbientDays { get; set; }
    [Parameter] public int CommsTickerItemCount { get; set; }
    [Parameter] public Action ComputeFiringSolution { get; set; } = default!;
    [Parameter] public Action CopyCrashNote { get; set; } = default!;
    [Parameter] public Func<CrewTemp.Voyage> CrewVoyage { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<string>> CrossingRows { get; set; } = default!;
    [Parameter] public double CurrentPlotHorizonSeconds { get; set; }
    [Parameter] public Func<bool> DarkWebCanTrade { get; set; } = default!;
    [Parameter] public Func<string> DarkWebDisabledReason { get; set; } = default!;
    [Parameter] public Func<double> DarkWebDistanceFromEarth { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip>> DarkWebMarketShips { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo>> DarkWebTrackedShips { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.WireContact>> DarkWebWireContacts { get; set; } = default!;
    [Parameter] public EventCallback DeclinePlunder { get; set; }
    [Parameter] public Action<PlanNode> DeleteNode { get; set; } = default!;
    [Parameter] public Func<NpcShip, string> DepartureLabel { get; set; } = default!;
    [Parameter] public Func<ShipDesk, string> DeskKeyLabel { get; set; } = default!;
    [Parameter] public Func<ShipDesk, string> DeskLabel { get; set; } = default!;
    [Parameter] public Func<string?> DestinationEta { get; set; } = default!;
    [Parameter] public Func<ClosestApproach.Pass, DestPassInfo?> DestinationPassInfo { get; set; } = default!;
    [Parameter] public Func<Action, Task> Dismiss { get; set; } = default!;
    [Parameter] public Action DismissCrashNote { get; set; } = default!;
    [Parameter] public Func<string?> DockedBodyName { get; set; } = default!;
    [Parameter] public bool DockFocusLive { get; set; }
    [Parameter] public double DockMatchSpeedMps { get; set; }
    [Parameter] public string DockNavLockTip { get; set; } = default!;
    [Parameter] public double DockReachMeters { get; set; }
    [Parameter] public Func<OrbitAssistInfo, string> DockStatusLine { get; set; } = default!;
    [Parameter] public Action DrawFirePlan { get; set; } = default!;
    [Parameter] public int EffectiveDockTankPulses { get; set; }
    [Parameter] public Func<OrbitAssistInfo, string> EmergencyCaptureTip { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo, string> EmergencyDescentTip { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo, string> EmergencyInsertionTip { get; set; } = default!;
    [Parameter] public Func<string> EmergencyUndockTip { get; set; } = default!;
    [Parameter] public EventCallback EngageLongHaul { get; set; }
    [Parameter] public EventCallback EnterOrbit { get; set; }
    [Parameter] public Func<string, NpcState?> FindNpc { get; set; } = default!;
    [Parameter] public bool FireLocked { get; set; }
    [Parameter] public Func<FireControl.Solution, string> FireTip { get; set; } = default!;
    [Parameter] public Action<string> FireWarningShot { get; set; } = default!;
    [Parameter] public Func<FlightPlanStatus> FlightNowNext { get; set; } = default!;
    [Parameter] public Func<int> FlightPlanCurrentStep { get; set; } = default!;
    [Parameter] public Func<int> FlightPlanStepCount { get; set; } = default!;
    [Parameter] public Func<string> FollowDestTip { get; set; } = default!;
    [Parameter] public bool FollowShip { get; set; }
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDuration { get; set; } = default!;
    [Parameter] public Func<double, string> FormatHorizon { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<double, string> FormatZoom { get; set; } = default!;
    [Parameter] public Func<List<FrameOption>> FrameOptions { get; set; } = default!;
    [Parameter] public Func<List<(string Label, List<CelestialBody> Members)>> FramePickerGroups { get; set; } = default!;
    [Parameter] public Func<string> FrameSpeedReadout { get; set; } = default!;
    [Parameter] public Func<string> FuelDirectionsLine { get; set; } = default!;
    [Parameter] public Func<string?, HarborClass> HarborClassOf { get; set; } = default!;
    [Parameter] public Func<double, double> HeadingAlongCourseAt { get; set; } = default!;
    [Parameter] public int HorizonSliderValue { get; set; }
    [Parameter] public Func<int?> InspectorCardPrice { get; set; } = default!;
    [Parameter] public Func<string, double> IntelStaleInDays { get; set; } = default!;
    [Parameter] public Func<double?> InterestDistanceNow { get; set; } = default!;
    [Parameter] public Func<string?> InterestTargetName { get; set; } = default!;
    [Parameter] public Func<CelestialBody, bool> IsDockableHaven { get; set; } = default!;
    [Parameter] public Func<bool> IsHiddenAtHaven { get; set; } = default!;
    [Parameter] public Func<Stations.Captain.AccountRow[]> LedgerAccounts { get; set; } = default!;
    [Parameter] public Func<Stations.Captain.CacheMapItem[]> LedgerMaps { get; set; } = default!;
    [Parameter] public Func<Stations.Captain.LedgerTip[]> LedgerTipsAsRemembered { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Stations.WarRoom.LiveRound>> LiveRounds { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<CommerceRule.LocalContact>> LocalContacts { get; set; } = default!;
    [Parameter] public string? LocalSpaceBodyId { get; set; }
    [Parameter] public string LongCoastAheadReadout { get; set; } = default!;
    [Parameter] public Func<CelestialBody, double> LongHaulCaptureRange { get; set; } = default!;
    [Parameter] public Func<int> LongHaulLastMilePulses { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure?, CelestialBody, string?, string?> LongHaulOfferBlock { get; set; } = default!;
    [Parameter] public Func<string?, CelestialBody?> LongHaulTargetPlanet { get; set; } = default!;
    [Parameter] public EventCallback MatchAndClamp { get; set; }
    [Parameter] public double MaxFireAimOffsetSeconds { get; set; }
    [Parameter] public double MaxMuzzleSpeed { get; set; }
    [Parameter] public int MaxNodePulses { get; set; }
    [Parameter] public int MinNodePulses { get; set; }
    [Parameter] public int MissilePulseCost { get; set; }
    [Parameter] public bool NavLockedByDock { get; set; }
    [Parameter] public Func<string> NearestReadoutName { get; set; } = default!;
    [Parameter] public int NetJammerPriceCr { get; set; }
    /// <summary>the page's own `NewsFeed(count, scope, salt)`, crossing as the LAMBDA the page already wrote, one level further up — no method group of it converts to this shape (the page's own optional parameters see to that).</summary>
    [Parameter] public Func<int, NewsWire.NewsScope, string?, IReadOnlyList<NewsWire.NewsItem>> NewsFeed { get; set; } = default!;
    [Parameter] public Func<PlanNode, NodeDirection, bool> NodeAimedAlong { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeDepartureEpoch { get; set; } = default!;
    [Parameter] public Action<PlanNode, int> NudgeNodeAim { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeNodeEpoch { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeNodePulses { get; set; } = default!;
    [Parameter] public int OffBooksCount { get; set; }
    [Parameter] public Action<AreaScanCoverage> OnAreaScanCovered { get; set; } = default!;
    [Parameter] public EventCallback<ChangeEventArgs> OnFramePicked { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnHorizonSliderInput { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnSkimAltInput { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnSlingRadiiInput { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnWarpSliderInput { get; set; }
    /// <summary>the page's own `OpenBank(contactId, viaWire)`, crossing as the lambda the page already wrote.</summary>
    [Parameter] public Action<string, bool> OpenBank { get; set; } = default!;
    [Parameter] public Action OpenDarkWebFromLedger { get; set; } = default!;
    [Parameter] public Action<string> OpenDossierFromLedger { get; set; } = default!;
    [Parameter] public Func<string?> OpenPlanEditorKey { get; set; } = default!;
    [Parameter] public Action OpenSaveDrawer { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo?> OrbitInfo { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo, string> OrbitStatusLine { get; set; } = default!;
    [Parameter] public bool Paused { get; set; }
    [Parameter] public bool PlanBeginsWithCastOff { get; set; }
    [Parameter] public bool PlanListIsEmpty { get; set; }
    [Parameter] public Func<double?> PlannedImpactEta { get; set; } = default!;
    [Parameter] public Func<int> PlannedPulseTotal { get; set; } = default!;
    [Parameter] public Func<double, string> PlanningPrimaryName { get; set; } = default!;
    [Parameter] public Func<string?> PlanShapeWarningLine { get; set; } = default!;
    [Parameter] public Func<PlanNode, string> PlanStepGlanceLine { get; set; } = default!;
    [Parameter] public Func<string> PlotButtonTip { get; set; } = default!;
    [Parameter] public Func<string> PlotFrameName { get; set; } = default!;
    [Parameter] public bool PlotMode { get; set; }
    [Parameter] public Action PointScopeForActiveFetch { get; set; } = default!;
    [Parameter] public Action<string> PointScopeFromLedger { get; set; } = default!;
    [Parameter] public Action<ScopeIntel> PointScopeWhereIntelSays { get; set; } = default!;
    [Parameter] public Func<Action, Task> PressAndRefocus { get; set; } = default!;
    [Parameter] public Func<int, int, long> PumpLoanPrincipal { get; set; } = default!;
    /// <summary>the page's own `PushNewsEvent(kind, subject, detail)`, crossing as the lambda the page already wrote.</summary>
    [Parameter] public Action<NewsWire.NewsEventKind, string, string?> PushNewsEvent { get; set; } = default!;
    [Parameter] public Func<Stations.Captain.QuestItem[]> QuestCards { get; set; } = default!;
    [Parameter] public int ReactionMassCapacity { get; set; }
    [Parameter] public Action<string> ReadTheGreyPage { get; set; } = default!;
    [Parameter] public EventCallback RemoveArriveStep { get; set; }
    [Parameter] public EventCallback RemoveTheCastOff { get; set; }
    [Parameter] public Action<KeyboardEventArgs> RenameKeyDown { get; set; } = default!;
    [Parameter] public Action ReopenStartPicker { get; set; } = default!;
    [Parameter] public EventCallback RestockSentries { get; set; }
    [Parameter] public Action<PlanNode> RetimeToScrub { get; set; } = default!;
    [Parameter] public Func<(string Text, bool Warn)?> RibbonHorizonNote { get; set; } = default!;
    [Parameter] public Func<NpcShip, string> RouteLabel { get; set; } = default!;
    [Parameter] public EventCallback RunSkimSolveAsync { get; set; }
    [Parameter] public EventCallback RunSlingSolveAsync { get; set; }
    [Parameter] public Action<bool, string> Say { get; set; } = default!;
    [Parameter] public Action ScanFiringWindows { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<TransferPlanner.BurnStep>> ScheduledAutopilotBurns { get; set; } = default!;
    [Parameter] public Func<TransferPlanner.BurnStep, string> ScheduledBurnGlanceLine { get; set; } = default!;
    [Parameter] public Func<string, ScopeIntel?> ScopeIntelById { get; set; } = default!;
    [Parameter] public double ScrubTime { get; set; }
    [Parameter] public EventCallback ScrubToArrive { get; set; }
    [Parameter] public Action<ClosestApproach.Pass> ScrubToDestinationPass { get; set; } = default!;
    [Parameter] public Action<string> SelectCommsShip { get; set; } = default!;
    [Parameter] public Func<NpcState?> SelectedCaptureTarget { get; set; } = default!;
    [Parameter] public Func<NpcState?> SelectedTrackedTarget { get; set; } = default!;
    [Parameter] public EventCallback SellCargo { get; set; }
    [Parameter] public Action SellTheChipToTheFence { get; set; } = default!;
    [Parameter] public Func<int> SentryRestockCost { get; set; } = default!;
    [Parameter] public Func<int> SentryRoundsMissing { get; set; } = default!;
    [Parameter] public Func<PlanNode, double> SeparationFromHarbour { get; set; } = default!;
    [Parameter] public Action<bool> SetActiveRadar { get; set; } = default!;
    [Parameter] public Action<string?> SetDestination { get; set; } = default!;
    [Parameter] public Action<double> SetFireAimOffset { get; set; } = default!;
    [Parameter] public Action<OrdnanceKind> SetFireKind { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetHeading { get; set; } = default!;
    [Parameter] public EventCallback SetHorizonAuto { get; set; }
    [Parameter] public Action<string> SetInterestTarget { get; set; } = default!;
    [Parameter] public Action<ShipMission> SetMission { get; set; } = default!;
    [Parameter] public Action<PlanNode, NodeDirection> SetNodeDirection { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetPercent { get; set; } = default!;
    [Parameter] public Action<string?> SetPlotFrame { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetPulses { get; set; } = default!;
    [Parameter] public Action<SlingPlanner.PassSide> SetSlingSide { get; set; } = default!;
    [Parameter] public Action<TransponderMode> SetTransponder { get; set; } = default!;
    [Parameter] public bool ShipBridgeAlive { get; set; }
    [Parameter] public bool ShowLongCoastAdvert { get; set; }
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Func<SkimGauge, string> SkimDepthLine { get; set; } = default!;
    [Parameter] public Func<string> SkimFinePrint { get; set; } = default!;
    [Parameter] public Func<SkimGauge, string> SkimOutcomeLine { get; set; } = default!;
    [Parameter] public Func<SkimGauge, string> SkimShedLine { get; set; } = default!;
    [Parameter] public Func<double> SkimShellTopKm { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingBurnLine { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingLeverLine { get; set; } = default!;
    [Parameter] public double SlingMaxRadii { get; set; }
    [Parameter] public double SlingMinRadii { get; set; }
    [Parameter] public Func<string> SlingNodeNoteNow { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingOutcomeLine { get; set; } = default!;
    [Parameter] public Func<SlingPlanner.Result, string> SlingPassLine { get; set; } = default!;
    [Parameter] public int SlugPulseCost { get; set; }
    [Parameter] public Action<string> StartLocalBuy { get; set; } = default!;
    [Parameter] public Action<string> StartLocalTrade { get; set; } = default!;
    [Parameter] public EventCallback StartSkip { get; set; }
    [Parameter] public Action<int> StartTutorial { get; set; } = default!;
    [Parameter] public Func<StoryBeats.Beat, string?, (string Title, string Art, string Caption)> StoryBeatCopy { get; set; } = default!;
    [Parameter] public Func<string?> StraightWindowText { get; set; } = default!;
    [Parameter] public Action<ShipDesk> SwitchDesk { get; set; } = default!;
    [Parameter] public Func<ShipDesk, Task> SwitchDeskFromClick { get; set; } = default!;
    [Parameter] public ShipDesk[] TabBarOrder { get; set; } = default!;
    [Parameter] public Action<string> ToggleArmedInsertion { get; set; } = default!;
    [Parameter] public EventCallback ToggleArriveEditor { get; set; }
    [Parameter] public Action<PlanNode> ToggleBurnEditor { get; set; } = default!;
    [Parameter] public Action ToggleDamageControlAuthority { get; set; } = default!;
    [Parameter] public EventCallback ToggleDock { get; set; }
    [Parameter] public Action ToggleFireAtWill { get; set; } = default!;
    [Parameter] public EventCallback ToggleFollow { get; set; }
    [Parameter] public EventCallback ToggleFollowDest { get; set; }
    [Parameter] public EventCallback ToggleInsertionEditor { get; set; }
    [Parameter] public Action ToggleNavHelp { get; set; } = default!;
    [Parameter] public EventCallback TogglePause { get; set; }
    [Parameter] public EventCallback TogglePeekMapFromClick { get; set; }
    [Parameter] public EventCallback TogglePin { get; set; }
    [Parameter] public EventCallback TogglePlotMode { get; set; }
    [Parameter] public EventCallback ToggleSkimPanel { get; set; }
    [Parameter] public EventCallback ToggleSkip { get; set; }
    [Parameter] public EventCallback ToggleSlingPanel { get; set; }
    [Parameter] public EventCallback ToggleTutorial { get; set; }
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate>> TrackingCandidates { get; set; } = default!;
    [Parameter] public Func<double, (string? FrameId, string FrameName, string DestinationName)?> TripFrameOffer { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Stations.Captain.TutorialItem>> TutorialCards { get; set; } = default!;
    [Parameter] public EventCallback Undock { get; set; }
    [Parameter] public Func<int, int> UpgradePrice { get; set; } = default!;
    [Parameter] public EventCallback UseAncientsPilot { get; set; }
    [Parameter] public Action<string> ViewMapFromLedger { get; set; } = default!;
    [Parameter] public int Warp { get; set; }
    [Parameter] public string WarpReadout { get; set; } = default!;
    [Parameter] public int WarpSliderValue { get; set; }
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.Contact>> WarRoomContacts { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.HunterContact>> WarRoomHunters { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack>> WarRoomSensorTracks { get; set; } = default!;
    [Parameter] public bool WeaponsAuthorized { get; set; }
    [Parameter] public Action<double> ZoomSensorsBackdrop { get; set; } = default!;
    /// <summary>the page's own `ZoomStep(zoomIn)`, crossing as the lambda the page already wrote.</summary>
    [Parameter] public Action<bool> ZoomStep { get; set; } = default!;

    // ── THE EIGHT MEMBERS THE COLUMN WRITES ──────────────────────────────────────────────────────────
    //
    // #251 · each keeps the member's OWN NAME, so the moved markup still reads and assigns it exactly as it
    // did in the page and the assignment still lands on the page (NoSurfaceSwallowsAWriteTests). Two of
    // them are read one-way further down the column as well — `_credits` by the Nav HUD, `_trackingPost` by
    // the War Room — and what they read is THIS, so both ends of the column see one value in one render.
    private bool _burnAngleAbsolute { get => _burnAngleAbsoluteValue; set { _burnAngleAbsoluteValue = value; _burnAngleAbsoluteSet(value); } }
    private SpaceSails.Client.Pages.Stations.Captain.CaptainView _captainTab { get => _captainTabValue; set { _captainTabValue = value; _captainTabSet(value); } }
    private string? _commsSelectedId { get => _commsSelectedIdValue; set { _commsSelectedIdValue = value; _commsSelectedIdSet(value); } }
    private int _credits { get => _creditsValue; set { _creditsValue = value; _creditsSet(value); } }
    private SpaceSails.Client.Pages.Stations.LocalSpace? _localSpace { get => _localSpaceValue; set { _localSpaceValue = value; _localSpaceSet(value); } }
    private string _renameDraft { get => _renameDraftValue; set { _renameDraftValue = value; _renameDraftSet(value); } }
    private double _scrubOffsetSeconds { get => _scrubOffsetSecondsValue; set { _scrubOffsetSecondsValue = value; _scrubOffsetSecondsSet(value); } }
    private SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPost { get => _trackingPostValue; set { _trackingPostValue = value; _trackingPostSet(value); } }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
