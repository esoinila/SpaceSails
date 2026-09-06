using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using CargoManifestEntry = SpaceSails.Client.Pages.Map.CargoManifestEntry;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using ScopeIntel = SpaceSails.Client.Pages.Map.ScopeIntel;
using ShipBot = SpaceSails.Client.Pages.Map.ShipBot;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// DeskPanels — the code-behind for DeskPanels.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of DeskPanels.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class DeskPanels
{
    // ── WHAT THESE PARAMETERS ARE ────────────────────────────────────────────────────────────────────
    //
    // #251 item 1 · Every [Parameter] below carries a member of the page under THE MEMBER'S OWN NAME —
    // a field keeps its underscore, a method arrives as a delegate with its own signature, a variable the
    // page's `@if` binds arrives under the name the `@if` gave it. That is the whole trick of this
    // refactor: it is what let the markup above move out of Map.razor without a single character of it
    // changing, and what lets the suite's source guards read it through MapMarkup exactly as they read it
    // when it lived in the page. A parameter that needs saying more than that says it on its own line.

    [Parameter] public ShipDesk _activeDesk { get; set; } = default!;
    [Parameter] public string? _activeThreadId { get; set; }
    [Parameter] public CacheLedger _caches { get; set; } = default!;
    /// <summary>the page's `SpaceSails.Client.Pages.Stations.Captain.CaptainView _captainTab` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_captainTab` and the assignment still lands on the page.</summary>
    [Parameter] public SpaceSails.Client.Pages.Stations.Captain.CaptainView _captainTabValue { get; set; } = default!;
    /// <summary>The page's `_captainTab = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<SpaceSails.Client.Pages.Stations.Captain.CaptainView> _captainTabSet { get; set; } = default!;
    private SpaceSails.Client.Pages.Stations.Captain.CaptainView _captainTab { get => _captainTabValue; set { _captainTabValue = value; _captainTabSet(value); } }
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _cargoValue { get; set; }
    [Parameter] public string? _commsActionMessage { get; set; }
    [Parameter] public string? _commsHailAnswer { get; set; }
    /// <summary>the page's `string? _commsSelectedId` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_commsSelectedId` and the assignment still lands on the page.</summary>
    [Parameter] public string? _commsSelectedIdValue { get; set; }
    /// <summary>The page's `_commsSelectedId = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<string?> _commsSelectedIdSet { get; set; } = default!;
    private string? _commsSelectedId { get => _commsSelectedIdValue; set { _commsSelectedIdValue = value; _commsSelectedIdSet(value); } }
    [Parameter] public bool _crashNoteCopied { get; set; }
    /// <summary>the page's `int _credits` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_credits` and the assignment still lands on the page.</summary>
    [Parameter] public int _creditsValue { get; set; }
    /// <summary>The page's `_credits = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<int> _creditsSet { get; set; } = default!;
    private int _credits { get => _creditsValue; set { _creditsValue = value; _creditsSet(value); } }
    [Parameter] public bool _dcNextActionCleared { get; set; }
    [Parameter] public bool _dcStandingOrder { get; set; }
    [Parameter] public string? _dockBodyId { get; set; }
    [Parameter] public bool _docked { get; set; }
    [Parameter] public string? _dockedHavenId { get; set; }
    [Parameter] public ICelestialEphemeris? _ephemeris { get; set; }
    [Parameter] public double _fireAimOffsetSeconds { get; set; }
    [Parameter] public double _fireAtSimTime { get; set; }
    [Parameter] public bool _fireAtWill { get; set; }
    [Parameter] public string? _fireBlockedBy { get; set; }
    [Parameter] public double _fireDispersionMeters { get; set; }
    [Parameter] public OrdnanceKind _fireKind { get; set; } = default!;
    [Parameter] public FireControl.Solution? _fireSolution { get; set; }
    [Parameter] public string? _fireTip { get; set; }
    [Parameter] public bool _hasNetJammer { get; set; }
    [Parameter] public HeatState _heat { get; set; } = default!;
    [Parameter] public int _holdLevel { get; set; }
    [Parameter] public IntelLedger _intelLedger { get; set; } = default!;
    [Parameter] public InterceptEstimate.Result? _intercept { get; set; }
    [Parameter] public string? _interestTargetId { get; set; }
    /// <summary>the page's `SpaceSails.Client.Pages.Stations.LocalSpace? _localSpace` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_localSpace` and the assignment still lands on the page.</summary>
    [Parameter] public SpaceSails.Client.Pages.Stations.LocalSpace? _localSpaceValue { get; set; }
    /// <summary>The page's `_localSpace = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<SpaceSails.Client.Pages.Stations.LocalSpace?> _localSpaceSet { get; set; } = default!;
    private SpaceSails.Client.Pages.Stations.LocalSpace? _localSpace { get => _localSpaceValue; set { _localSpaceValue = value; _localSpaceSet(value); } }
    [Parameter] public string? _localTradeMessage { get; set; }
    [Parameter] public double _localTradeProgress { get; set; }
    [Parameter] public string? _localTradeTargetId { get; set; }
    [Parameter] public int _massLevel { get; set; }
    [Parameter] public int _missileAmmo { get; set; }
    [Parameter] public ShipMission _mission { get; set; } = default!;
    [Parameter] public MissionOptions _missionOptions { get; set; } = default!;
    [Parameter] public bool _openCaptainToTutorials { get; set; }
    [Parameter] public string? _orbitedBodyId { get; set; }
    [Parameter] public bool _pinned { get; set; }
    [Parameter] public int _reactionMassPulses { get; set; }
    /// <summary>the page's `string _renameDraft` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_renameDraft` and the assignment still lands on the page.</summary>
    [Parameter] public string _renameDraftValue { get; set; } = default!;
    /// <summary>The page's `_renameDraft = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<string> _renameDraftSet { get; set; } = default!;
    private string _renameDraft { get => _renameDraftValue; set { _renameDraftValue = value; _renameDraftSet(value); } }
    [Parameter] public string? _renamingThreadId { get; set; }
    [Parameter] public int _revealedIterations { get; set; }
    [Parameter] public List<ScopeIntel> _scopeIntel { get; set; } = default!;
    [Parameter] public int _sensorLevel { get; set; }
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public string? _shipAuthorized { get; set; }
    [Parameter] public List<ShipBot> _shipBots { get; set; } = default!;
    [Parameter] public bool _shotAuthorized { get; set; }
    [Parameter] public int _slugAmmo { get; set; }
    [Parameter] public int _telescopeLevel { get; set; }
    [Parameter] public SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPost { get; set; }
    [Parameter] public bool _weaponsTight { get; set; }
    [Parameter] public int _workingStopsSinceShoreLeave { get; set; }
    [Parameter] public string ActiveCaptainName { get; set; } = default!;
    [Parameter] public Func<int> ActiveTutorialIndex { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DeskChips.ChipData>> AllStationChips { get; set; } = default!;
    [Parameter] public Action ArmFire { get; set; } = default!;
    [Parameter] public Action AuthorizeNextDamageControlAct { get; set; } = default!;
    [Parameter] public Action AuthorizeShot { get; set; } = default!;
    [Parameter] public Action AutoAim { get; set; } = default!;
    [Parameter] public Action<string> BeginRenameCaptain { get; set; } = default!;
    [Parameter] public Func<string?> BestWireLenderId { get; set; } = default!;
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public Action<string> BribeShip { get; set; } = default!;
    [Parameter] public Action<OrdnanceKind> BuyAmmo { get; set; } = default!;
    [Parameter] public Action<int> BuyFuel { get; set; } = default!;
    [Parameter] public EventCallback BuyNetJammer { get; set; }
    [Parameter] public Action<string> BuyUpgrade { get; set; } = default!;
    [Parameter] public EventCallback CallInFavorAtPump { get; set; }
    [Parameter] public Action CancelFiringSolution { get; set; } = default!;
    [Parameter] public Action CancelRenameCaptain { get; set; } = default!;
    [Parameter] public Func<bool> CanWarnInterest { get; set; } = default!;
    [Parameter] public Func<CrashNote?> CaptainCrashNote { get; set; } = default!;
    [Parameter] public int CargoCapacity { get; set; }
    [Parameter] public Func<IReadOnlyList<CargoManifestEntry>> CargoManifest { get; set; } = default!;
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
    [Parameter] public Func<bool> DarkWebCanTrade { get; set; } = default!;
    [Parameter] public Func<string> DarkWebDisabledReason { get; set; } = default!;
    [Parameter] public Func<double> DarkWebDistanceFromEarth { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip>> DarkWebMarketShips { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo>> DarkWebTrackedShips { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.WireContact>> DarkWebWireContacts { get; set; } = default!;
    // #233 · The chip's row on the dark-web desk: what the buyer pays (null while the captain carries no
    // chip) and the press that sells it. Both are Map's — the desk does no arithmetic and keeps no state.
    [Parameter] public Func<int?> ChipFencePrice { get; set; } = default!;
    [Parameter] public Action SellTheChipToTheFence { get; set; } = default!;
    [Parameter] public Func<NpcShip, string> DepartureLabel { get; set; } = default!;
    [Parameter] public Action DismissCrashNote { get; set; } = default!;
    [Parameter] public Func<string?> DockedBodyName { get; set; } = default!;
    [Parameter] public Action DrawFirePlan { get; set; } = default!;
    [Parameter] public Func<string, NpcState?> FindNpc { get; set; } = default!;
    [Parameter] public bool FireLocked { get; set; }
    [Parameter] public Func<FireControl.Solution, string> FireTip { get; set; } = default!;
    [Parameter] public Action<string> FireWarningShot { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    [Parameter] public Func<string> FuelDirectionsLine { get; set; } = default!;
    [Parameter] public Func<string, double> IntelStaleInDays { get; set; } = default!;
    [Parameter] public Func<double?> InterestDistanceNow { get; set; } = default!;
    [Parameter] public Func<string?> InterestTargetName { get; set; } = default!;
    [Parameter] public Func<bool> IsHiddenAtHaven { get; set; } = default!;
    [Parameter] public Func<Stations.Captain.AccountRow[]> LedgerAccounts { get; set; } = default!;
    [Parameter] public Func<Stations.Captain.CacheMapItem[]> LedgerMaps { get; set; } = default!;
    [Parameter] public Func<Stations.Captain.LedgerTip[]> LedgerTipsAsRemembered { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<Stations.WarRoom.LiveRound>> LiveRounds { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<CommerceRule.LocalContact>> LocalContacts { get; set; } = default!;
    [Parameter] public string? LocalSpaceBodyId { get; set; }
    [Parameter] public double MaxFireAimOffsetSeconds { get; set; }
    [Parameter] public double MaxMuzzleSpeed { get; set; }
    [Parameter] public int MissilePulseCost { get; set; }
    [Parameter] public int NetJammerPriceCr { get; set; }
    [Parameter] public Func<int, NewsWire.NewsScope, string?, IReadOnlyList<NewsWire.NewsItem>> OnNewsFeed { get; set; } = default!;
    private IReadOnlyList<NewsWire.NewsItem> NewsFeed(int ambientCount,
        NewsWire.NewsScope scope = NewsWire.NewsScope.SystemWire, string? salt = null)
        => OnNewsFeed(ambientCount, scope, salt);
    [Parameter] public int OffBooksCount { get; set; }
    [Parameter] public Action<string, bool> OnOpenBank { get; set; } = default!;
    private void OpenBank(string contactId, bool viaWire) => OnOpenBank(contactId, viaWire);
    [Parameter] public Action OpenDarkWebFromLedger { get; set; } = default!;
    [Parameter] public Action<string> OpenDossierFromLedger { get; set; } = default!;
    [Parameter] public Action OpenSaveDrawer { get; set; } = default!;
    [Parameter] public Func<double?> PlannedImpactEta { get; set; } = default!;
    [Parameter] public Action PointScopeForActiveFetch { get; set; } = default!;
    [Parameter] public Action<string> PointScopeFromLedger { get; set; } = default!;
    [Parameter] public Action<ScopeIntel> PointScopeWhereIntelSays { get; set; } = default!;
    [Parameter] public Func<int, int, long> PumpLoanPrincipal { get; set; } = default!;
    [Parameter] public Action<NewsWire.NewsEventKind, string, string?> OnPushNewsEvent { get; set; } = default!;
    private void PushNewsEvent(NewsWire.NewsEventKind kind, string subject, string? detail = null)
        => OnPushNewsEvent(kind, subject, detail);
    [Parameter] public Func<Stations.Captain.QuestItem[]> QuestCards { get; set; } = default!;
    [Parameter] public int ReactionMassCapacity { get; set; }
    [Parameter] public Action<string> ReadTheGreyPage { get; set; } = default!;
    [Parameter] public Action<KeyboardEventArgs> RenameKeyDown { get; set; } = default!;
    [Parameter] public Action ReopenStartPicker { get; set; } = default!;
    [Parameter] public EventCallback RestockSentries { get; set; }
    [Parameter] public Func<NpcShip, string> RouteLabel { get; set; } = default!;
    [Parameter] public Action ScanFiringWindows { get; set; } = default!;
    [Parameter] public Func<string, ScopeIntel?> ScopeIntelById { get; set; } = default!;
    [Parameter] public Action<string> SelectCommsShip { get; set; } = default!;
    [Parameter] public Func<NpcState?> SelectedTrackedTarget { get; set; } = default!;
    [Parameter] public EventCallback SellCargo { get; set; }
    [Parameter] public Func<int> SentryRestockCost { get; set; } = default!;
    [Parameter] public Func<int> SentryRoundsMissing { get; set; } = default!;
    [Parameter] public Action<double> SetFireAimOffset { get; set; } = default!;
    [Parameter] public Action<OrdnanceKind> SetFireKind { get; set; } = default!;
    [Parameter] public Action<string> SetInterestTarget { get; set; } = default!;
    [Parameter] public Action<ShipMission> SetMission { get; set; } = default!;
    [Parameter] public bool ShipBridgeAlive { get; set; }
    [Parameter] public double SimTime { get; set; }
    [Parameter] public int SlugPulseCost { get; set; }
    [Parameter] public Action<string> StartLocalBuy { get; set; } = default!;
    [Parameter] public Action<string> StartLocalTrade { get; set; } = default!;
    [Parameter] public Action<int> StartTutorial { get; set; } = default!;
    [Parameter] public Func<string?> StraightWindowText { get; set; } = default!;
    [Parameter] public Action<ShipDesk> SwitchDesk { get; set; } = default!;
    [Parameter] public Func<ShipDesk, Task> SwitchDeskFromClick { get; set; } = default!;
    [Parameter] public Action ToggleDamageControlAuthority { get; set; } = default!;
    [Parameter] public Action ToggleFireAtWill { get; set; } = default!;
    [Parameter] public EventCallback TogglePin { get; set; }
    [Parameter] public Func<IReadOnlyList<Stations.Captain.TutorialItem>> TutorialCards { get; set; } = default!;
    [Parameter] public Func<int, int> UpgradePrice { get; set; } = default!;
    [Parameter] public Action<string> ViewMapFromLedger { get; set; } = default!;
    [Parameter] public int Warp { get; set; }
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.Contact>> WarRoomContacts { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.HunterContact>> WarRoomHunters { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack>> WarRoomSensorTracks { get; set; } = default!;
    [Parameter] public bool WeaponsAuthorized { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
