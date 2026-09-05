// #251 item 1 · THE TRACKING POST'S CODE-BEHIND, ON ITS OWN FILE.
//
// THE DESK ITSELF: what the tracking post IS — the two projections Map.razor hands it,
// every [Parameter] the page binds, the instruments it owns (telescope, ledger, schedule,
// lost-track board), and the parameter tick that turns the ship's clock into telescope work.
//
// PURE MOTION: the members below are the members that stood in TrackingPost.razor's @code block,
// character for character, at the same indentation and in the same order. Nothing was renamed,
// resignatured or reordered — the whole change is which file the lines sit in. The desk is one
// `partial class TrackingPost`; the razor file keeps the markup and nothing else. (Map.razor's own
// code came out into 155 `Map.*.cs` partials in 2026-07 for exactly this reason; the tracking post
// is the desk that never got the same treatment, which is how it reached 1,473 lines.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

public partial class TrackingPost
{
    /// <summary>A candidate ship the tracking post can sweep for — a thin projection of
    /// Map.razor's own NPC state so this component never needs to know its private type.
    /// <see cref="IsPod"/>/<see cref="CargoDetail"/> (PR-12) let the scope wall draw the right
    /// silhouette and detail line for a tracked target, same as Map.razor's own scope inset.</summary>
    public readonly record struct TrackingCandidate(string Id, string Callsign, ShipState State, bool IsPod = false, string? CargoDetail = null, bool IsThreat = false);

    /// <summary>M29: a known contact whose predicted coast the plotted course passes near —
    /// the target-of-opportunity row (the cover-story seed: fly an innocent course, watch who
    /// drifts conveniently close along it).</summary>
    public readonly record struct CourseOpportunity(
        string Id, string Callsign, double MinDistance, double MinSimTime, bool Tracked);

    [Parameter, EditorRequired] public double SimTime { get; set; }
    [Parameter, EditorRequired] public Vector2d ShipPosition { get; set; }
    [Parameter, EditorRequired] public Vector2d ShipVelocity { get; set; }
    [Parameter] public double ShipCharge { get; set; }
    [Parameter] public ICelestialEphemeris? Ephemeris { get; set; }
    [Parameter] public IReadOnlyList<TrackingCandidate> Candidates { get; set; } = [];
    [Parameter] public int MaxTracks { get; set; } = 1;
    [Parameter] public double TelescopeSpeedFactor { get; set; } = 1;
    [Parameter] public bool Visible { get; set; }
    [Parameter] public bool FullScreen { get; set; }
    [Parameter] public bool RadarActive { get; set; }
    [Parameter] public TransponderMode Transponder { get; set; } = TransponderMode.On;
    [Parameter] public EventCallback<TransponderMode> OnTransponderChange { get; set; }
    [Parameter] public IReadOnlyList<CourseOpportunity> Opportunities { get; set; } = [];
    [Parameter] public EventCallback<string> OnSetInterest { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<bool> OnRadarToggle { get; set; }
    [Parameter] public EventCallback<double> OnZoomMap { get; set; }
    [Parameter] public EventCallback OnCenterShip { get; set; }

    /// <summary>Tuesday plan PR-A / #240: how much of a scan's sky the telescope has been over.</summary>
    [Parameter] public EventCallback<AreaScanCoverage> OnAreaScanCoverage { get; set; }

    private const double RosetteCenterPx = 100;
    private const double RosetteMaxRadiusPx = 82;
    private const int RosetteSteps = 48;

    // M27: ONE live telescope tile cycling the ledger (was PR-12's wall of four) — drawn off
    // the same shared render loop as Map.razor's scope inset.
    private const int CardScopeSizePx = 148;
    private static readonly RgbaColor WallTargetColor = new(200, 120, 255); // mirrors Map.razor's NpcColor

    // PR-D: one live ScopeView per tracked-target card (PR-12's scope wall reborn, per-card).
    private readonly Dictionary<string, ScopeView> _cardScopes = new();
    private bool _wallFrameTickSubscribed;

    // M27: the passive watch — the eyes are open by default. Whenever no sweep is running (and
    // the player hasn't switched the watch off), a full-circle survey starts by itself.
    private bool _passiveWatch = true;
    private bool _passiveJobRunning;

    private readonly TelescopeModel _telescope = new();
    private readonly TrackedTargetLedger _ledger = new();

    // SundaySecondPlan PR-B: the one instrument, scheduled. The task queue owns the telescope
    // whenever no manual sweep does; the passive watch fills only truly idle time. Custody
    // passes for every held track are standing tasks, so tracking many things means real gaps.
    private const ulong SkySeed = 20260705; // the populated sky's page (per-scenario knob later)
    private readonly TelescopeSchedule _schedule = new();
    private readonly LostTrackLedger _lostTracks = new();
    private readonly Dictionary<string, (double BuiltAt, Vector2d Center)> _lostCenters = new();
    private IReadOnlyList<CorridorRegion> _corridorRegions = [];
    private IReadOnlyList<Discovery> _recentFinds = [];
    private (string ShipId, DateTime WallTime)? _lastPassFlash;

    // PR-6 (dark space web): ships the player has laser-ranged. Purely a UI-side warning flag —
    // Core's ledger doesn't need to know about it (PR-7 will consume "aware" for real).
    private readonly HashSet<string> _aware = new();

    private double _centerBearingDeg;
    private double _arcWidthDeg = 30;
    private ScanJob? _activeJob;
    private double _sweepStartSimTime;
    private double _lastSimTime;
    private bool _hasLastSimTime;
    private string? _lastSweepMessage;
    private double _corridorsBuiltAtSimTime = double.NegativeInfinity;

    /// <summary>The Sensors chip's objective line (addendum, 2026-07-04 evening): what we're
    /// looking for right now, not raw stats — a sweep in progress, else the best-quality track,
    /// else nothing set.</summary>
    public string ObjectiveSummary(double simTime)
    {
        if (RadarActive)
        {
            return "RADAR ACTIVE — loud";
        }

        string? callsign = BestTrackCallsign(simTime);
        if (callsign is not null)
        {
            return $"Tracking {callsign}";
        }

        if (_activeJob is not null)
        {
            return _passiveJobRunning ? "Passive watch — scanning" : "Manual sweep running";
        }

        if (_schedule.Active is { } task)
        {
            return $"Telescope: {task.Label}";
        }

        return "No watch set";
    }

    private string? BestTrackCallsign(double simTime)
    {
        TrackedTarget? best = null;
        double bestQuality = -1;
        foreach (TrackedTarget entry in _ledger.Entries)
        {
            double quality = entry.EffectiveQuality(simTime);
            if (quality > bestQuality)
            {
                bestQuality = quality;
                best = entry;
            }
        }

        if (best is not { } target)
        {
            return null;
        }

        return FindCandidate(target.ShipId)?.Callsign ?? target.ShipId;
    }

    /// <summary>Exposed so Map.razor's NPC-draw pass can key emphasis and the tightened
    /// prediction cone off the ledger without either side reaching into the other's internals.</summary>
    public bool TryGetTrack(string shipId, out TrackedTarget track) => _ledger.TryGet(shipId, out track);

    /// <summary>Read-only view of every current track (PR-6: the dark web sells these).</summary>
    public IReadOnlyCollection<TrackedTarget> Entries => _ledger.Entries;

    /// <summary>
    /// Injects a confirmed observation from outside the sweep/confirm loop (PR-6: laser ranging).
    /// Reuses the exact same reconfirm path a passive sweep hit takes, so the prediction cone
    /// tightens through the one code path everything else already goes through — a perfect,
    /// zero-age observation resets the elapsed-time term PathPredictor grows its cone from.
    /// </summary>
    public bool ApplyObservation(Observation observation) => _ledger.Add(observation);

    /// <summary>Flags a target as aware it's been actively pinged (PR-6: laser ranging gives away
    /// your position). UI-only bookkeeping — see <see cref="_aware"/>.</summary>
    public void MarkAware(string shipId) => _aware.Add(shipId);

    protected override void OnParametersSet()
    {
        _ledger.MaxTracks = Math.Max(1, MaxTracks);

        bool timeAdvanced = _hasLastSimTime && SimTime > _lastSimTime;
        _lastSimTime = SimTime;
        _hasLastSimTime = true;

        // Corridor programs are a pure function of (bodies, ship position, time) — cheap to
        // recompute, but there's no reason to redo it every render tick either.
        // Corridor geometry is a pure function of (bodies, time) — cheap to recompute, but no
        // reason to redo it every render tick either.
        if (Ephemeris is not null && SimTime - _corridorsBuiltAtSimTime > 3600)
        {
            _corridorRegions = TradeCorridors.Regions(Ephemeris, SimTime);
            _corridorsBuiltAtSimTime = SimTime;
        }

        if (timeAdvanced && _activeJob is { } job)
        {
            double elapsed = SimTime - _sweepStartSimTime;
            if (elapsed >= job.DurationSeconds)
            {
                CompleteSweep(job);
            }
        }

        RunScheduledInstrument(timeAdvanced);

        // M27: the passive watch — restart the full-circle survey when the post AND the task
        // queue are idle (real tasks own the instrument otherwise).
        if (_passiveWatch && _activeJob is null && _schedule.Queue.Count == 0)
        {
            _activeJob = new ScanJob(0, Math.Tau);
            _sweepStartSimTime = SimTime;
            _passiveJobRunning = true;
        }

        HandleLostAndColdTracks();
    }
}
