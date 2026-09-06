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

// NavHud — the code-behind for NavHud.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of NavHud.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class NavHud
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public bool _activeRadar { get; set; }
    [Parameter] public int _ancientCharges { get; set; }
    [Parameter] public int _armedBudgetPulses { get; set; }
    [Parameter] public string? _armedOrbitBodyId { get; set; }
    [Parameter] public string? _armedTransferSummary { get; set; }
    [Parameter] public ArriveStep? _arrive { get; set; }
    /// <summary>the page's `bool _burnAngleAbsolute` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_burnAngleAbsolute` and the assignment still lands on the page.</summary>
    [Parameter] public bool _burnAngleAbsoluteValue { get; set; }
    /// <summary>The page's `_burnAngleAbsolute = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<bool> _burnAngleAbsoluteSet { get; set; } = default!;
    private bool _burnAngleAbsolute { get => _burnAngleAbsoluteValue; set { _burnAngleAbsoluteValue = value; _burnAngleAbsoluteSet(value); } }
    [Parameter] public Camera _camera { get; set; } = default!;
    [Parameter] public bool _captureEngaged { get; set; }
    [Parameter] public double _captureProgress { get; set; }
    [Parameter] public double _captureRequiredSeconds { get; set; }
    [Parameter] public string? _captureTargetCallsign { get; set; }
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _cargoValue { get; set; }
    [Parameter] public ClosestApproach.Pass? _closestPass { get; set; }
    [Parameter] public int _credits { get; set; }
    [Parameter] public string? _destinationBodyId { get; set; }
    [Parameter] public string? _disarmConfirmBodyId { get; set; }
    [Parameter] public DockAffordance _dockAffordance { get; set; } = default!;
    [Parameter] public string? _dockedHavenId { get; set; }
    [Parameter] public int _effectiveWarp { get; set; }
    [Parameter] public bool _followDest { get; set; }
    [Parameter] public string _horizonChoice { get; set; } = default!;
    [Parameter] public int _keepTrimPulsesPerDay { get; set; }
    [Parameter] public string? _longHaulClearanceBlock { get; set; }
    [Parameter] public LongHaul.Departure? _longHaulDeparture { get; set; }
    [Parameter] public Vector2d _nearestBodyPosition { get; set; } = default!;
    [Parameter] public Vector2d _nearestBodyVelocity { get; set; } = default!;
    [Parameter] public CelestialBody? _nearestHaven { get; set; }
    [Parameter] public FlightEditorKind _openEditor { get; set; } = default!;
    [Parameter] public bool _orbitKept { get; set; }
    [Parameter] public List<PlanNode> _planNodes { get; set; } = default!;
    [Parameter] public PlasmaEnvironment? _plasma { get; set; }
    [Parameter] public string? _plotFrameBodyId { get; set; }
    [Parameter] public string? _plunderOpportunityTargetId { get; set; }
    [Parameter] public PulseSlot _pulse { get; set; } = default!;
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public string _scenarioName { get; set; } = default!;
    /// <summary>the page's `double _scrubOffsetSeconds` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_scrubOffsetSeconds` and the assignment still lands on the page.</summary>
    [Parameter] public double _scrubOffsetSecondsValue { get; set; }
    /// <summary>The page's `_scrubOffsetSeconds = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<double> _scrubOffsetSecondsSet { get; set; } = default!;
    private double _scrubOffsetSeconds { get => _scrubOffsetSecondsValue; set { _scrubOffsetSecondsValue = value; _scrubOffsetSecondsSet(value); } }
    [Parameter] public PlanNode? _selectedPlanNode { get; set; }
    [Parameter] public ShipState _ship { get; set; } = default!;
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
    [Parameter] public Func<string, Quest?> ActiveCargoRunTo { get; set; } = default!;
    [Parameter] public Func<int> ActiveTutorialIndex { get; set; } = default!;
    [Parameter] public Action<ArrivalStepRule.ArrivalKind> AddArriveAtScrub { get; set; } = default!;
    [Parameter] public EventCallback AddBurnAtScrub { get; set; }
    [Parameter] public EventCallback AddCastOffAtTop { get; set; }
    [Parameter] public EventCallback AddSkimBurn { get; set; }
    [Parameter] public EventCallback AddSlingBurn { get; set; }
    [Parameter] public Action<PlanNode, int> ApplyPulses { get; set; } = default!;
    [Parameter] public double ArcChargeThreshold { get; set; }
    [Parameter] public EventCallback ArmArriveStep { get; set; }
    [Parameter] public bool ArmedArrivalStillAhead { get; set; }
    [Parameter] public double? ArmedInsertionSimTime { get; set; }
    [Parameter] public Func<ArrivalStepRule.ArrivalKind, string> ArriveButtonLabel { get; set; } = default!;
    [Parameter] public Func<ArrivalStepRule.ArrivalKind, ClosestApproach.Pass?> ArriveCandidate { get; set; } = default!;
    [Parameter] public Func<ArrivalStepRule.ArrivalCheck?> ArriveCheck { get; set; } = default!;
    [Parameter] public bool ArriveCoversArmed { get; set; }
    [Parameter] public Func<ArriveStep, string> ArriveGlanceLine { get; set; } = default!;
    [Parameter] public Func<ArriveStep, bool> ArriveIsAThen { get; set; } = default!;
    [Parameter] public Func<string, ClosestApproach.Pass?> ArrivePassFor { get; set; } = default!;
    [Parameter] public Func<string?> ArrivePlanCompleteLine { get; set; } = default!;
    [Parameter] public Func<string?> ArriveRibbonTooShortLine { get; set; } = default!;
    [Parameter] public EventCallback AuthorizePlunder { get; set; }
    [Parameter] public bool AutopilotFlyingApproach { get; set; }
    [Parameter] public Action<NpcState> Board { get; set; } = default!;
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public Func<PlanNode, string> BurnGlanceLine { get; set; } = default!;
    [Parameter] public bool CanFollowDestination { get; set; }
    [Parameter] public int CargoCapacity { get; set; }
    [Parameter] public Func<CelestialBody, string> CargoNextAction { get; set; } = default!;
    [Parameter] public double CircularSpeedHere { get; set; }
    [Parameter] public Func<PlanNode, string> ClearanceEtaLine { get; set; } = default!;
    [Parameter] public double CurrentPlotHorizonSeconds { get; set; }
    [Parameter] public EventCallback DeclinePlunder { get; set; }
    [Parameter] public Action<PlanNode> DeleteNode { get; set; } = default!;
    [Parameter] public Func<string?> DestinationEta { get; set; } = default!;
    [Parameter] public bool DockFocusLive { get; set; }
    [Parameter] public double DockMatchSpeedMps { get; set; }
    [Parameter] public string DockNavLockTip { get; set; } = default!;
    [Parameter] public double DockReachMeters { get; set; }
    [Parameter] public Func<OrbitAssistInfo, string> DockStatusLine { get; set; } = default!;
    [Parameter] public int EffectiveDockTankPulses { get; set; }
    [Parameter] public Func<OrbitAssistInfo, string> EmergencyCaptureTip { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo, string> EmergencyDescentTip { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo, string> EmergencyInsertionTip { get; set; } = default!;
    [Parameter] public Func<string> EmergencyUndockTip { get; set; } = default!;
    [Parameter] public EventCallback EngageLongHaul { get; set; }
    [Parameter] public EventCallback EnterOrbit { get; set; }
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
    [Parameter] public Func<string?, HarborClass> HarborClassOf { get; set; } = default!;
    [Parameter] public Func<double, double> HeadingAlongCourseAt { get; set; } = default!;
    [Parameter] public int HorizonSliderValue { get; set; }
    [Parameter] public Func<CelestialBody, bool> IsDockableHaven { get; set; } = default!;
    [Parameter] public string LongCoastAheadReadout { get; set; } = default!;
    [Parameter] public Func<LongHaul.Departure?, CelestialBody, string?, string?> LongHaulOfferBlock { get; set; } = default!;
    [Parameter] public Func<string?, CelestialBody?> LongHaulTargetPlanet { get; set; } = default!;
    [Parameter] public EventCallback MatchAndClamp { get; set; }
    [Parameter] public int MaxNodePulses { get; set; }
    [Parameter] public int MinNodePulses { get; set; }
    [Parameter] public bool NavLockedByDock { get; set; }
    [Parameter] public Func<string> NearestReadoutName { get; set; } = default!;
    [Parameter] public Func<PlanNode, NodeDirection, bool> NodeAimedAlong { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeDepartureEpoch { get; set; } = default!;
    [Parameter] public Action<PlanNode, int> NudgeNodeAim { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeNodeEpoch { get; set; } = default!;
    [Parameter] public Action<PlanNode, int, bool> NudgeNodePulses { get; set; } = default!;
    [Parameter] public EventCallback<ChangeEventArgs> OnFramePicked { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnHorizonSliderInput { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnSkimAltInput { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnSlingRadiiInput { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnWarpSliderInput { get; set; }
    [Parameter] public Func<string?> OpenPlanEditorKey { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo?> OrbitInfo { get; set; } = default!;
    [Parameter] public Func<OrbitAssistInfo, string> OrbitStatusLine { get; set; } = default!;
    [Parameter] public bool Paused { get; set; }
    [Parameter] public bool PlanBeginsWithCastOff { get; set; }
    [Parameter] public bool PlanListIsEmpty { get; set; }
    [Parameter] public Func<int> PlannedPulseTotal { get; set; } = default!;
    [Parameter] public Func<double, string> PlanningPrimaryName { get; set; } = default!;
    [Parameter] public Func<string?> PlanShapeWarningLine { get; set; } = default!;
    [Parameter] public Func<PlanNode, string> PlanStepGlanceLine { get; set; } = default!;
    [Parameter] public Func<string> PlotButtonTip { get; set; } = default!;
    [Parameter] public Func<string> PlotFrameName { get; set; } = default!;
    [Parameter] public bool PlotMode { get; set; }
    [Parameter] public Func<Action, Task> PressAndRefocus { get; set; } = default!;
    [Parameter] public int ReactionMassCapacity { get; set; }
    [Parameter] public EventCallback RemoveArriveStep { get; set; }
    [Parameter] public EventCallback RemoveTheCastOff { get; set; }
    [Parameter] public Action<PlanNode> RetimeToScrub { get; set; } = default!;
    [Parameter] public Func<(string Text, bool Warn)?> RibbonHorizonNote { get; set; } = default!;
    [Parameter] public EventCallback RunSkimSolveAsync { get; set; }
    [Parameter] public EventCallback RunSlingSolveAsync { get; set; }
    [Parameter] public Action<bool, string> Say { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<TransferPlanner.BurnStep>> ScheduledAutopilotBurns { get; set; } = default!;
    [Parameter] public Func<TransferPlanner.BurnStep, string> ScheduledBurnGlanceLine { get; set; } = default!;
    [Parameter] public double ScrubTime { get; set; }
    [Parameter] public EventCallback ScrubToArrive { get; set; }
    [Parameter] public Func<NpcState?> SelectedCaptureTarget { get; set; } = default!;
    [Parameter] public Func<PlanNode, double> SeparationFromHarbour { get; set; } = default!;
    [Parameter] public Action<string?> SetDestination { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetHeading { get; set; } = default!;
    [Parameter] public EventCallback SetHorizonAuto { get; set; }
    [Parameter] public Action<PlanNode, NodeDirection> SetNodeDirection { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetPercent { get; set; } = default!;
    [Parameter] public Action<string?> SetPlotFrame { get; set; } = default!;
    [Parameter] public Action<PlanNode, ChangeEventArgs> SetPulses { get; set; } = default!;
    [Parameter] public Action<SlingPlanner.PassSide> SetSlingSide { get; set; } = default!;
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
    [Parameter] public EventCallback StartSkip { get; set; }
    [Parameter] public Action<string> ToggleArmedInsertion { get; set; } = default!;
    [Parameter] public EventCallback ToggleArriveEditor { get; set; }
    [Parameter] public Action<PlanNode> ToggleBurnEditor { get; set; } = default!;
    [Parameter] public EventCallback ToggleDock { get; set; }
    [Parameter] public EventCallback ToggleFollow { get; set; }
    [Parameter] public EventCallback ToggleFollowDest { get; set; }
    [Parameter] public EventCallback ToggleInsertionEditor { get; set; }
    [Parameter] public Action ToggleNavHelp { get; set; } = default!;
    [Parameter] public EventCallback TogglePause { get; set; }
    [Parameter] public EventCallback TogglePlotMode { get; set; }
    [Parameter] public EventCallback ToggleSkimPanel { get; set; }
    [Parameter] public EventCallback ToggleSkip { get; set; }
    [Parameter] public EventCallback ToggleSlingPanel { get; set; }
    [Parameter] public EventCallback ToggleTutorial { get; set; }
    [Parameter] public Func<double, (string? FrameId, string FrameName, string DestinationName)?> TripFrameOffer { get; set; } = default!;
    [Parameter] public EventCallback Undock { get; set; }
    [Parameter] public EventCallback UseAncientsPilot { get; set; }
    [Parameter] public int Warp { get; set; }
    [Parameter] public string WarpReadout { get; set; } = default!;
    [Parameter] public int WarpSliderValue { get; set; }
    [Parameter] public Action<bool> OnZoomStep { get; set; } = default!;
    private void ZoomStep(bool zoomIn) => OnZoomStep(zoomIn);

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
