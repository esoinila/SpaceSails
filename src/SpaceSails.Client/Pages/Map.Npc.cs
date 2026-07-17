using System.Globalization;
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
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Npc — the traffic: schedule wiring, StepNpcs, the refill that keeps the lanes busy, the
// sensor sweep and the prediction cone that paints where a contact is going. Carved off
// Map.razor for #251 — motion only.
public partial class Map
{
    private int _telescopeLevel;                       // MaxTracks: 1 + level, cap 4 (3 upgrades)
    private SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPost;
    private static readonly RgbaColor TrackedNpcColor = new(150, 255, 210);
    private static readonly RgbaColor TrackedNpcLastSeenColor = new(120, 200, 170, 180);
    private Simulator? _npcSimulator;

    // M5 additions — traffic & prediction
    private const double SensorSweepSimSeconds = 60;            // sensor cadence, sim seconds
    private const double NpcDespawnRadius = 1e10;               // matches RoutePlanner.ArrivalToleranceMeters
    private const double PredictionRefreshSimSeconds = 6 * 3600; // periodic cone re-solve
    private const double TrackedWindowSimSeconds = 2 * 3600;    // "Tracked" if seen within this
    private const int ConeTargetPoints = 300;                   // stride the cone down to ~this many points
    private const double ConeMaxHalfWidthMeters = 3e11;        // ~2 AU: stop drawing where prediction is meaningless

    private static readonly RgbaColor NpcColor = new(200, 120, 255);
    private static readonly RgbaColor NpcLastSeenColor = new(200, 120, 255, 90);
    private static readonly RgbaColor ConeCenterColor = new(150, 150, 220, 140);
    private static readonly RgbaColor ConeBoundaryColor = new(150, 150, 220, 80);

    private NpcState[] _npcStates = [];
    private string? _selectedTargetId;
    private SensorModel _sensor = SensorModel.Default;
    private bool _pinned;
    private ManeuverPlan? _pinnedPlan;
    private PredictedPath? _predictedPath;
    private bool _predictionDirty;
    private double _nextSweepSimTime;
    private double _nextPredictionSimTime;

    // Scratch buffers for the prediction cone polylines — reused each frame, no per-frame heap churn.
    private float[] _coneCenter = [];
    private float[] _coneUpper = [];
    private float[] _coneLower = [];

    // Live state of one NPC: its immutable schedule entry plus its evolving simulation state and
    // observation history. Mirrors PlanNode's role — mutable client-side wrapper over Core records.
    private sealed class NpcState
    {
        public required NpcShip Ship;
        public ShipState State;
        public bool Active;
        public bool Arrived;
        public bool Boarded;             // cargo taken; keeps flying but empty (M6)
        public bool CurrentlyObserved;   // seen in the most recent sweep
        public Observation? LastObservation;
        public int ObservationCount;
        public int CargoSoldToPlayer;    // units the player has BOUGHT (honest trade) — depletes the manifest
        public bool WarningShotFired;    // PR-7: heaves to (if compliant) — faster boarding
        public bool Bribed;              // PR-7: compliant AND no heat generated when robbed
        public bool Disabled;            // M28: slug through the sail — no more burns, drifts ballistic
    }
    private static readonly RgbaColor DisabledNpcColor = new(150, 150, 160);

    private string NpcName(string id) => FindNpc(id)?.Ship.Callsign ?? id;

    private const double IntelScanLeadSeconds = 12 * 3600;   // aim a touch ahead of "now" — a prediction, not a snapshot
    private const double WreckScanRadiusM = 4e10;            // generous box: covers the wreck's drift before the pass lands
    private static readonly RgbaColor ScanWedgeFillColor = new(120, 220, 255, 12);
    private static readonly RgbaColor ScanWedgeDoneFillColor = new(120, 220, 255, 32);
    private static readonly RgbaColor ScanWedgeEdgeColor = new(120, 220, 255, 80);
    private static readonly RgbaColor SearchRegionColor = new(255, 150, 100);

    private void DrawScanWedge()
    {
        if (_trackingPost?.CurrentScan is not { } scan)
        {
            return;
        }

        ScanJob job = scan.Job;
        (float shipX, float shipY) = _camera.WorldToScreen(_ship.Position);
        bool fullCircle = job.ArcWidthRad >= Math.Tau - 1e-9;

        // The whole aimed wedge faint, then the swept-so-far portion brighter — the sensors
        // chief literally watches the exposure fill in on the sky.
        DrawWedgePolygon(job.CenterBearingRad - job.ArcWidthRad / 2, job.ArcWidthRad,
            ScanWedgeFillColor, fullCircle, shipX, shipY);
        double sweptArc = job.ArcWidthRad * Math.Clamp(scan.Progress, 0, 1);
        if (sweptArc > 1e-6)
        {
            DrawWedgePolygon(job.CenterBearingRad - job.ArcWidthRad / 2, sweptArc,
                ScanWedgeDoneFillColor, fullCircle && scan.Progress >= 1, shipX, shipY);
        }

        _renderer!.DrawText(shipX, shipY + 26, $"📡 {scan.Label} · {(int)(scan.Progress * 100)}%",
            ScanWedgeEdgeColor with { A = 200 }, "11px sans-serif", TextAlign.Center);
    }

    private void DrawWedgePolygon(double startBearing, double arc, RgbaColor fill, bool fullCircle,
        float shipX, float shipY)
    {
        const int arcSteps = 28;
        Span<float> points = stackalloc float[(arcSteps + 2) * 2];
        int w = 0;
        if (!fullCircle)
        {
            points[w++] = shipX;
            points[w++] = shipY;
        }

        for (int i = 0; i <= arcSteps; i++)
        {
            double bearing = startBearing + arc * i / arcSteps;
            Vector2d look = new(Math.Cos(bearing), Math.Sin(bearing));
            double range = _trackingPost!.TelescopeRangeAlong(look);
            (float x, float y) = _camera.WorldToScreen(_ship.Position + look * range);
            points[w++] = x;
            points[w++] = y;
        }

        _renderer!.DrawPolygon(points[..w], fill, ScanWedgeEdgeColor, 1f);
    }

    private void DrawLostSearchRegions()
    {
        if (_trackingPost is null)
        {
            return;
        }

        foreach (LostTrack lost in _trackingPost.LostTrackEntries)
        {
            Vector2d center = _trackingPost.LostCenter(lost);
            (float cx, float cy) = _camera.WorldToScreen(center);
            float radiusPx = (float)Math.Max(8, lost.SearchRadius(SimTime) / _camera.MetersPerPixel);
            byte pulse = (byte)(120 + 60 * Math.Sin(_frameNowMs / 250.0));
            RgbaColor stroke = SearchRegionColor with { A = pulse };
            _renderer!.DrawCircle(cx, cy, radiusPx, SearchRegionColor with { A = 10 }, stroke, 1.5f);
            _renderer.DrawText(cx, cy - radiusPx - 6, $"🔍 lost — {ContactCallsign(lost.ShipId)}",
                stroke, "11px sans-serif", TextAlign.Center);
        }
    }

    private string ContactCallsign(string shipId)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == shipId)
            {
                return npc.Ship.Callsign;
            }
        }

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == shipId)
            {
                return hunter.Callsign;
            }
        }

        return shipId;
    }

    private Vector2d? ContactPosition(string shipId)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == shipId)
            {
                return npc.State.Position;
            }
        }

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == shipId)
            {
                return hunter.State.Position;
            }
        }

        return null;
    }

    private void TrackShipFromMenu(string id)
    {
        NpcState? npc = FindNpc(id);
        if (npc is null || _trackingPost is null)
        {
            return;
        }

        if (!npc.CurrentlyObserved)
        {
            ShowPulseMessage("No live contact — the telescope needs a sweep fix or laser ranging first");
        }
        else if (_trackingPost.ApplyObservation(new Observation(id, SimTime, npc.State.Position, npc.State.Velocity)))
        {
            ShowPulseMessage($"{npc.Ship.Callsign} on the telescope ledger — fix sharpened 📡");
        }
        else
        {
            ShowPulseMessage("Telescopes full — drop a track on the Sensors desk first");
        }

        CloseShipMenu();
    }

    /// <summary>What a scan would cost in telescope time — shown before the player commits.</summary>
    private string ScanCostText(Vector2d center, double radius) => CostText(SensorTaskGeometry.Duration(
        SensorTask.AreaScan(center, radius, "probe"),
        SensorTaskGeometry.WedgeToward(_ship.Position, center, radius), 1 + 0.5 * _telescopeLevel));

    private string SweepCostText(ScanJob job) => CostText(
        Math.Max(SensorTaskGeometry.MinPassSeconds, job.DurationSeconds) / (1 + 0.5 * _telescopeLevel));

    private static string CostText(double seconds) =>
        seconds < 3600 ? $"≈ {seconds / 60:F0} min telescope time" : $"≈ {seconds / 3600:F1} h telescope time";

    private string SkyScanLabel(Vector2d point) =>
        $"sky scan · {FormatDistance((point - _ship.Position).Length)} out";

    private void ScanAreaFromMenu(Vector2d center, double radius, string label)
    {
        if (_trackingPost is null)
        {
            return;
        }

        ShowPulseMessage(_trackingPost.EnqueueTask(SensorTask.AreaScan(center, radius, label))
            ? $"🔭 Queued: {label}"
            : "That patch is already on the sensor tasks queue");
        CloseShipMenu();
        CloseBodyMenu();
        CloseSkyMenu();
    }

    // Tuesday plan PR-A (the reveal): a scan finished and swept some disc of sky. If a hidden body's
    // TRUE position at the scan's completion instant fell inside that disc, the scope resolved it —
    // chart it. It must be a completed pass, not merely a scheduled one (this fires from the tracking
    // post's HandlePass, which only runs when a pass actually lands).
    private void OnAreaScanCovered(SpaceSails.Client.Pages.Stations.TrackingPost.CompletedAreaScan scan)
    {
        if (_ephemeris is null)
        {
            return;
        }
        foreach (string id in _hiddenBodyIds)
        {
            if (_revealedBodyIds.Contains(id))
            {
                continue;
            }
            Vector2d truePos = _ephemeris.Position(id, scan.CompleteTime);
            if ((truePos - scan.Center).Length <= scan.Radius)
            {
                RevealBody(id, WreckRevealMessage(id));
                _scopeIntel.RemoveAll(si => si.BodyId == id);
            }
        }
    }

    private void SweepCorridorFromMenu(CorridorRegion lane, bool standing)
    {
        if (_trackingPost is null)
        {
            return;
        }

        SensorTask task = SensorTask.CorridorSweep(lane.AId, lane.BId,
            standing ? $"{lane.PairName} lane watch" : $"{lane.PairName} lane sweep", recurring: standing);
        ShowPulseMessage(_trackingPost.EnqueueTask(task)
            ? (standing ? $"🔁 Standing watch on the {lane.Name}" : $"📡 Sweeping the {lane.Name}")
            : $"The {lane.Name} is already on the queue");
        CloseCorridorMenu();
        CloseSkyMenu();
    }

    // M29 (the cover-story seed): every KNOWN contact whose predicted coast the current plotted
    // course happens to pass near — the Sensors desk reads these as targets of opportunity.
    // Fly an innocent course; see who drifts conveniently close along it.
    private readonly List<SpaceSails.Client.Pages.Stations.TrackingPost.CourseOpportunity> _courseOpportunities = [];

    private void UpdateCourseOpportunities()
    {
        _courseOpportunities.Clear();
        if (_simulator is null || _samples.Count < 2)
        {
            return;
        }

        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Active || npc.Arrived || npc.Disabled)
            {
                continue;
            }

            // Honest intel only: the course scan sees what the sensors see.
            bool tracked = _trackingPost is not null && _trackingPost.TryGetTrack(npc.Ship.Id, out _);
            if (!npc.CurrentlyObserved && !tracked)
            {
                continue;
            }

            double horizon = _samples[^1].SimTime - npc.State.SimTime;
            if (horizon <= 0)
            {
                continue;
            }

            IReadOnlyList<TrajectorySample> theirs = _simulator.ProjectAdaptive(npc.State, null, horizon, maxSamples: 400);
            if (InterceptEstimate.Against(_samples, theirs, CaptureRule.CaptureRadiusMeters) is { } pass)
            {
                _courseOpportunities.Add(new SpaceSails.Client.Pages.Stations.TrackingPost.CourseOpportunity(
                    npc.Ship.Id, npc.Ship.Callsign, pass.MinDistance, pass.MinSimTime, tracked));
            }
        }

        _courseOpportunities.Sort((a, b) => a.MinDistance.CompareTo(b.MinDistance));
        if (_courseOpportunities.Count > 8)
        {
            _courseOpportunities.RemoveRange(8, _courseOpportunities.Count - 8);
        }
    }

    private void CloseDossier()
    {
        // Closing the book stands down both selection layers it can be showing.
        if (_interestTargetId is not null)
        {
            SetInterestTarget(_interestTargetId); // toggle off
        }

        if (_selectedTargetId is not null)
        {
            SelectTarget(_selectedTargetId); // toggle off
        }

        StateHasChanged();
    }

    private readonly record struct DossierInfo(
        string Name, string Detail, string StatusLine,
        double Distance, double RelSpeed, double Closing,
        double? TrackQuality, bool InDriverReach, string FireLine,
        bool IsPrey, int BoardReady);

    private DossierInfo? DossierFor(string id)
    {
        Vector2d position, velocity;
        string name, detail;
        string statusLine;
        bool isPrey = false;
        if (FindNpc(id) is { Active: true, Arrived: false } npc)
        {
            isPrey = true;
            name = npc.Ship.Callsign;
            detail = npc.Ship.IsPod ? "· cargo pod" : $"· {npc.Ship.CargoClass} ({npc.Ship.CargoUnits}u)";
            if (npc.CurrentlyObserved)
            {
                position = npc.State.Position;
                velocity = npc.State.Velocity;
                statusLine = npc.Disabled ? "⚠ adrift — sail holed, easy prey" : "👁 live sensor contact";
            }
            else if (npc.LastObservation is { } lastObs)
            {
                position = lastObs.Position;
                velocity = lastObs.Velocity;
                statusLine = $"👻 last seen {FormatDuration(Math.Max(0, SimTime - lastObs.SimTime))} ago — dead-reckoned";
            }
            else
            {
                return null; // never observed: no honest dossier to show
            }
        }
        else
        {
            HunterState? found = null;
            foreach (HunterState hunter in _hunters)
            {
                if (hunter.Id == id) { found = hunter; break; }
            }

            if (found is not { } h)
            {
                return null;
            }

            (name, detail) = (h.Callsign, "· hired muscle 🐺");
            position = h.State.Position;
            velocity = h.State.Velocity;
            statusLine = h.BrokenOff ? "broke off the hunt" : "⚠ hunting US";
        }

        double distance = (position - _ship.Position).Length;
        double relSpeed = (velocity - _ship.Velocity).Length;
        // #210: one voice — the signed range-rate through the shared Core helper.
        double closing = RelativeMotion.ClosingSpeed(_ship.Position, _ship.Velocity, position, velocity);

        double? quality = null;
        if (_trackingPost is not null && _trackingPost.TryGetTrack(id, out TrackedTarget track))
        {
            quality = track.EffectiveQuality(SimTime);
        }

        // The driver's practical reach: max charge times the longest aim lead the gun deck offers.
        double reach = MaxMuzzleSpeed * 86400;
        bool inReach = distance <= reach;
        string fireLine = inReach
            ? "🎖 inside the driver's reach — a firing solution is on the table (war room)"
            : $"driver reach ≈ {FormatDistance(reach)} — close {FormatDistance(distance - reach)} more for a firing solution";

        // The HONEST autosteal criteria (owner: "the box should say the requirements — close enough
        // but too much speed difference"). The encounter-window clock above is DISTANCE-only and
        // coast-assumed; the real CaptureRule.IsInWindow needs BOTH within 5e8 m AND under 5 km/s
        // relative. BoardReady: 2 = both met (boardable), 1 = in range but too fast, 0 = out of range.
        // The popup renders the two checks explicitly from Distance/RelSpeed so nothing is implied.
        int boardReady = 0;
        if (isPrey)
        {
            bool within = distance <= CaptureRule.CaptureRadiusMeters;
            bool slow = relSpeed <= CaptureRule.MaxRelativeSpeed;
            boardReady = within && slow ? 2 : within ? 1 : 0;
        }

        return new DossierInfo(name, detail, statusLine, distance, relSpeed, closing, quality, inReach, fireLine, isPrey, boardReady);
    }

    // ---- M5: traffic, sensors, prediction ----

    // Keep every active NPC in lockstep with the player: after the player's stepping loop, catch
    // each NPC up to the player's SimTime. NPCs use Core's fixed NpcTimeStep (60 s), not the
    // player's dt=1 s — 8 NPCs × 10000 fine steps per frame at max warp brought interpreted WASM
    // to ~1 fps, and NPC accuracy needs are meters-scale at dt=60. The ≤59 s overshoot past the
    // player's SimTime is subpixel at map zoom. Also heals a mid-frame activation (the ship
    // starts from InitialState and immediately catches up).
    private void StepNpcs()
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Arrived)
            {
                continue;
            }

            if (!npc.Active)
            {
                if (_ship.SimTime < npc.Ship.ActivationTime)
                {
                    continue;
                }

                npc.Active = true;
                npc.State = npc.Ship.InitialState;
            }

            if (npc.Ship.DepotBodyId is not null)
            {
                // Depots ride rails: pure function of sim time, no integration, no drift.
                // And they never "arrive" — a depot's destination is its own host body, and
                // every inner-system depot orbits well inside NpcDespawnRadius, so letting the
                // despawn check below see one killed it on the first tick (the bug behind the
                // empty starting sky: Earth's depot died at birth while Jupiter's survived).
                npc.State = TrafficSchedule.DepotState(
                    npc.Ship.Id, npc.Ship.DepotBodyId, npc.Ship.DepotOrbitRadius, npc.Ship.DepotPhase,
                    _ephemeris!, _ship.SimTime);
                continue;
            }

            // #255 — the freeze class: if the world clock has leapt an epoch ahead of this mover (a long-haul
            // jump, a far-epoch vault resume, or a ?simhours boot cheat left it seeded near epoch 0 while
            // SimTime is years on), do NOT integrate the void — that is millions of 60 s Steps and a
            // hard-frozen tab. ReseedWorldForJump already retires movers on the jump/resume paths; this is the
            // last-line guard so no future path can ever grind. The mover belonged to the world we left, so
            // retire it exactly as the re-seed would and let RefillTraffic repopulate at the current epoch.
            // Depots never reach here (rails, closed-form, handled above); honest warp never opens a gap this
            // wide in one frame (see TrafficSchedule.NpcMaxCatchUpSeconds).
            if (TrafficSchedule.IsCatchUpStale(_ship.SimTime - npc.State.SimTime))
            {
                npc.Arrived = true;
                continue;
            }

            while (npc.State.SimTime < _ship.SimTime)
            {
                // M28: a slug through the sail ends all burns — the hulk drifts ballistic.
                npc.State = _npcSimulator!.Step(npc.State, npc.Disabled ? null : npc.Ship.Plan);
            }

            // Despawn: past its last planned node and parked within tolerance of the destination body.
            IReadOnlyList<ManeuverNode> nodes = npc.Ship.Plan.Nodes;
            double lastNodeTime = nodes.Count > 0 ? nodes[^1].SimTime : npc.Ship.ActivationTime;
            if (npc.State.SimTime >= lastNodeTime)
            {
                Vector2d destination = _ephemeris!.Position(npc.Ship.DestinationId, npc.State.SimTime);
                if ((npc.State.Position - destination).Length <= NpcDespawnRadius)
                {
                    npc.Arrived = true;
                }
            }
        }
    }

    // Sensor sweep: honor-system client-side filtering (server enforces it from M9). Each active,
    // non-arrived NPC is either currently visible or falls back to its dim last-seen marker.
    private void SweepSensors()
    {
        foreach (NpcState npc in _npcStates)
        {
            bool wasObserved = npc.CurrentlyObserved;
            npc.CurrentlyObserved = false;
            if (!npc.Active || npc.Arrived)
            {
                continue;
            }

            // M27: active radar — exact returns inside its range, sun glare and dark hulls be
            // damned. The passive model still runs first (it can see farther).
            bool seen = _sensor.TryObserve(_ship.Position, npc.Ship.Id, npc.State, _ship.SimTime, out Observation obs);
            if (!seen && _activeRadar && RadarRule.InRange(_ship.Position, npc.State.Position))
            {
                obs = new Observation(npc.Ship.Id, _ship.SimTime, npc.State.Position, npc.State.Velocity);
                seen = true;
            }

            // The living sky (owner): honest traffic runs LIT, and a transponder is a radio
            // broadcast — heard across AU, no optics needed. This is why the map is never
            // "empty space": every civilian beacon paints its ship. Off-the-books haulers
            // stay exactly as dark as their hulls.
            if (!seen && npc.Ship.PublishesTimetable
                && (npc.State.Position - _ship.Position).Length <= TransponderRule.CivilianBeaconRangeMeters)
            {
                obs = new Observation(npc.Ship.Id, _ship.SimTime, npc.State.Position, npc.State.Velocity);
                seen = true;
            }

            if (seen)
            {
                npc.LastObservation = obs;
                npc.ObservationCount++;
                npc.CurrentlyObserved = true;
                if (npc.Ship.Id == _selectedTargetId)
                {
                    // A fresh contact resets Δt — the cone snaps tight ("tightens as you shadow").
                    _predictionDirty = true;
                }
            }
            else if (wasObserved && (npc.Ship.Id == _interestTargetId || npc.Ship.Id == _selectedTargetId))
            {
                // LOST CONTACT is big news (owner): the target we were shadowing just fell off our
                // live fix. Sound off, and the ship does all it can to re-acquire — force the
                // telescope onto a LostSearch for her right now, on top of standing passive watch.
                // Meanwhile she stays on the map dead-reckoned (DrawNpcs), never a silent vanish.
                ShowPulseMessage($"⚠ LOST CONTACT — {npc.Ship.Callsign} off our fix; scopes re-acquiring, dead-reckoning her track");
                RendererInterop.PlayCue("miss");
                _trackingPost?.ForceReacquire(npc.Ship.Id);
            }
        }

        RefillTraffic();
    }

    // ---- The world keeps living (owner: the sky must never empty) ----

    private const int MinLiveTraffic = 6;
    private int _trafficWave;
    private double _lastRefillCheckSimTime;

    /// <summary>When enough ships have arrived/despawned that the sky is thinning, plan a
    /// fresh deterministic wave relative to NOW — new mid-flight haulers and scheduled
    /// departures, plus a couple of pods so the milk run never dries up.</summary>
    private void RefillTraffic()
    {
        if (_ephemeris is null || SimTime - _lastRefillCheckSimTime < 3600)
        {
            return;
        }

        _lastRefillCheckSimTime = SimTime;
        int live = 0;
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.DepotBodyId is null && !npc.Arrived)
            {
                live++;
            }
        }

        if (live >= MinLiveTraffic)
        {
            return;
        }

        _trafficWave++;
        IReadOnlyList<NpcShip> ships = TrafficSchedule.GenerateWave(
            _ephemeris, seed: 42UL + (ulong)_trafficWave * 1000, count: 6, SimTime, _trafficWave);
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePodsWave(
            _ephemeris, seed: 43UL + (ulong)_trafficWave * 1000, count: 2, SimTime, _trafficWave);
        _npcStates = _npcStates
            .Concat(ships.Concat(pods).Select(s => new NpcState { Ship = s }))
            .ToArray();
    }

    // Re-solve the prediction cone for the selected target on a fresh observation, a pin change, or
    // the periodic refresh. Cheap between solves — the cached path is redrawn each frame.
    private void UpdatePrediction()
    {
        NpcState? npc = _selectedTargetId is null ? null : FindNpc(_selectedTargetId);
        if (npc?.LastObservation is not { } obs)
        {
            _predictedPath = null;
            _pinnedPlan = null;
            return;
        }

        if (_ship.SimTime >= _nextPredictionSimTime)
        {
            _predictionDirty = true;
        }

        if (!_predictionDirty && _predictedPath is not null)
        {
            return;
        }

        _pinnedPlan = _pinned ? BuildPinnedPlan(npc, obs) : null;
        _predictedPath = PathPredictor.Predict(_ephemeris!, obs, _pinnedPlan, CurrentPlotHorizonSeconds, npc.Ship.ManeuverBudget);
        _nextPredictionSimTime = _ship.SimTime + PredictionRefreshSimSeconds;
        _predictionDirty = false;
    }

    private ManeuverPlan BuildPinnedPlan(NpcState npc, Observation obs)
    {
        double horizon = Math.Max(60 * DaySeconds, npc.Ship.EstimatedArrivalTime - obs.SimTime + 30 * DaySeconds);
        return PathPredictor.BrakeAtHypothesis(_ephemeris!, obs, npc.Ship.DestinationId, horizon);
    }

    // NPC markers: solid dot + callsign only while currently observed; otherwise a dim hollow marker
    // at the last known position for any ship ever seen.
    private void DrawNpcs()
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Arrived)
            {
                continue;
            }

            // 🗺 Layers: hidden classes neither draw nor answer clicks (the picker checks too).
            if (npc.Ship.DepotBodyId is not null ? !LayerVisible("depots") : !LayerVisible("traffic"))
            {
                continue;
            }

            // Tracking-post emphasis (vision ¶14): a ship on the ledger gets a brighter marker
            // plus a small ring sized off the ledger's own uncertainty growth (no separate
            // PathPredictor solve here — just the same half-width formula, cheap enough to draw
            // every frame for a handful of tracked contacts).
            TrackedTarget track = default;
            bool tracked = _trackingPost is not null && _trackingPost.TryGetTrack(npc.Ship.Id, out track);

            // PR-5: a subtle ring for anything co-orbiting the body the ship is currently bound to
            // (the same "local space" set CommerceRule.ContactsAt would list) — the proximity
            // affordance made visible on the map itself, not just in the Local Space panel.
            bool coOrbiting = _orbitedBodyId is not null && (npc.Ship.DepotBodyId == _orbitedBodyId
                || (npc.Ship.DepotBodyId is null
                    && (npc.State.Position - _orbitedBodyPosition).LengthSquared <= _orbitedBodyHillRadius * _orbitedBodyHillRadius));

            if (npc.Active && npc.CurrentlyObserved)
            {
                (float sx, float sy) = _camera.WorldToScreen(npc.State.Position);
                RgbaColor color = npc.Disabled ? DisabledNpcColor : tracked ? TrackedNpcColor : NpcColor;
                _renderer!.DrawCircle(sx, sy, 4f, color, color);
                if (npc.Disabled)
                {
                    // M28: a holed sail reads as a hulk — gray dot, small broken ring.
                    _renderer!.DrawCircle(sx, sy, 7f, null, DisabledNpcColor with { A = 140 }, 1f);
                    _renderer!.DrawText(sx + 6, sy + 8, "adrift", DisabledNpcColor);
                }
                if (tracked)
                {
                    double dt = Math.Max(0, SimTime - track.LastObservation.SimTime);
                    double uncertainty = (PredictedPath.BaseHalfWidthMeters + PredictedPath.VelocitySigma * dt)
                        * track.UncertaintyScale(SimTime);
                    float ringPx = (float)Math.Clamp(uncertainty / _camera.MetersPerPixel, 6, 40);
                    _renderer!.DrawCircle(sx, sy, ringPx, null, TrackedNpcColor, 1.5f);
                }

                if (coOrbiting)
                {
                    _renderer!.DrawCircle(sx, sy, 7f, null, LocalContactRingColor, 1f);
                }

                _renderer!.DrawText(sx + 8, sy - 6, npc.Ship.Callsign, color);
            }
            else if (npc.LastObservation is { } obs)
            {
                // Anything we've EVER detected stays ON the map, dead-reckoned from its last fix to
                // NOW and LABELLED — a contact you've seen must never silently blink out between
                // sweeps (owner: "I always want to see all ships... never hide one unless there's
                // true interference to visibility, and surely not at this close"). Only a ship we've
                // genuinely never detected stays dark (that IS the interference). It's a coast
                // estimate, not a live fix — dim colour + a "no live fix" tag, plus a growing
                // uncertainty ring for ledger-tracked contacts (whose drift we actually model) — and
                // the sensors keep working to re-acquire it (passive watch / LostSearch).
                double dt = Math.Max(0, SimTime - obs.SimTime);
                Vector2d reckoned = obs.Position + obs.Velocity * dt;
                (float sx, float sy) = _camera.WorldToScreen(reckoned);
                RgbaColor c = tracked ? TrackedNpcLastSeenColor : NpcLastSeenColor;
                _renderer!.DrawCircle(sx, sy, 4f, null, c);
                if (tracked)
                {
                    double uncertainty = (PredictedPath.BaseHalfWidthMeters + PredictedPath.VelocitySigma * dt)
                        * track.UncertaintyScale(SimTime);
                    float ringPx = (float)Math.Clamp(uncertainty / _camera.MetersPerPixel, 6, 40);
                    _renderer!.DrawCircle(sx, sy, ringPx, null, c with { A = 120 }, 1f);
                }
                _renderer!.DrawText(sx + 8, sy - 6, $"{npc.Ship.Callsign} · no live fix", c);
            }
        }
    }

    // The prediction cone: center line plus two boundaries offset ±HalfWidthAt(t) perpendicular to the
    // local path direction. Drawn from the current sim time forward so the near end visibly widens
    // while the target is unobserved (Δt grows) and snaps tight on the next contact.
    private void DrawPredictionCone()
    {
        PredictedPath? path = _predictedPath;
        if (path is null)
        {
            return;
        }

        IReadOnlyList<TrajectorySample> s = path.Samples;
        if (s.Count < 2)
        {
            return;
        }

        int start = 0;
        while (start < s.Count - 1 && s[start].SimTime < _ship.SimTime)
        {
            start++;
        }

        int remaining = s.Count - start;
        if (remaining < 2)
        {
            return;
        }

        int stride = Math.Max(1, remaining / ConeTargetPoints);
        int maxPoints = remaining / stride + 2;
        int maxFloats = maxPoints * 2;
        if (_coneCenter.Length < maxFloats)
        {
            _coneCenter = new float[maxFloats];
            _coneUpper = new float[maxFloats];
            _coneLower = new float[maxFloats];
        }

        int w = 0;
        void Emit(int i)
        {
            TrajectorySample sample = s[i];
            int prev = Math.Max(0, i - stride);
            int next = Math.Min(s.Count - 1, i + stride);
            Vector2d dir = (s[next].Position - s[prev].Position).Normalized();
            Vector2d perp = new(-dir.Y, dir.X);
            double halfWidth = path.HalfWidthAt(sample.SimTime);

            (float cx, float cy) = _camera.WorldToScreen(PlotFrame(sample.Position, sample.SimTime));
            (float ux, float uy) = _camera.WorldToScreen(PlotFrame(sample.Position + perp * halfWidth, sample.SimTime));
            (float lx, float ly) = _camera.WorldToScreen(PlotFrame(sample.Position - perp * halfWidth, sample.SimTime));
            _coneCenter[w] = cx; _coneUpper[w] = ux; _coneLower[w] = lx;
            _coneCenter[w + 1] = cy; _coneUpper[w + 1] = uy; _coneLower[w + 1] = ly;
            w += 2;
        }

        // #145 — in a Hill-sphere frame the cone is clipped to the same local-timescale window as the
        // ship ribbon, so a co-moving NPC track doesn't coil around the giant either. Sun frame: no cap.
        double coneCutoff = FrameDisplayWindowSeconds() is { } win ? _ship.SimTime + win : double.PositiveInfinity;

        int last = start;
        for (int i = start; i < s.Count; i += stride)
        {
            // Beyond ~2 AU of uncertainty the cone means "could be anywhere" — drawing it just
            // fans giant lines across the map. Truncate the whole track there.
            if (path.HalfWidthAt(s[i].SimTime) > ConeMaxHalfWidthMeters || s[i].SimTime > coneCutoff)
            {
                break;
            }

            Emit(i);
            last = i;
        }
        if (last != s.Count - 1 && s[^1].SimTime <= coneCutoff && path.HalfWidthAt(s[^1].SimTime) <= ConeMaxHalfWidthMeters)
        {
            Emit(s.Count - 1);
        }

        if (w < 4)
        {
            return;
        }

        _renderer!.DrawPolyline(_coneUpper.AsSpan(0, w), ConeBoundaryColor);
        _renderer!.DrawPolyline(_coneLower.AsSpan(0, w), ConeBoundaryColor);
        _renderer!.DrawPolyline(_coneCenter.AsSpan(0, w), ConeCenterColor);
    }

    private NpcState? FindNpc(string id)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == id)
            {
                return npc;
            }
        }

        return null;
    }

    private void SelectTarget(string id)
    {
        _selectedTargetId = _selectedTargetId == id ? null : id;
        _pinned = false;
        _pinnedPlan = null;
        _predictedPath = null;
        _predictionDirty = true;
        _captureProgress = 0;

        // Tutorial step 1: selecting a Luna pod.
        if (_selectedTargetId is not null && FindNpc(_selectedTargetId) is { Ship.IsPod: true })
        {
            AdvanceTutorial(0);
        }

        // Second hunt, step 1: singling out the stubborn He3 freighter.
        if (_selectedTargetId == TrafficSchedule.StarterFreighterId)
        {
            AdvanceTutorial(StepSelectFreighter);
        }
    }

    private void TogglePin()
    {
        _pinned = !_pinned;
        _predictionDirty = true;
    }

    private void RebuildSensor()
    {
        double range = SensorModel.Default.RangeMeters * Math.Pow(1.4, _sensorLevel);
        _sensor = new SensorModel(range, SensorModel.Default.GlareHalfAngleRad, SensorModel.Default.GlareRangeFactor);
    }

    // The selected target, but only when it carries an observation (the pin toggle needs one).
    private NpcState? SelectedTrackedTarget()
    {
        NpcState? npc = _selectedTargetId is null ? null : FindNpc(_selectedTargetId);
        return npc?.LastObservation is not null ? npc : null;
    }

    // Secretive haulers (He3 out of pirate country) never hit the public board, but they're
    // still out there — a quiet nod that the outer reaches don't run on the same rules (PR-3).
    private int OffBooksCount => _npcStates.Count(n => !n.Ship.PublishesTimetable);

    private string StatusLabel(NpcState npc)
    {
        if (npc.Arrived)
        {
            return "Arrived";
        }
        if (npc.Boarded)
        {
            return "Boarded";
        }
        if (!npc.Active)
        {
            return "Scheduled";
        }
        if (npc.LastObservation is not { } obs)
        {
            return "En route";
        }

        return _ship.SimTime - obs.SimTime <= TrackedWindowSimSeconds ? "Tracked" : "Lost";
    }

    private string RouteLabel(NpcShip ship) => $"{BodyName(ship.OriginId)}→{BodyName(ship.DestinationId)}";

    // SATURDAY-ANCHOR: methods — parallel lanes append their station methods directly below

    // Thin, read-only projection of the live NPC list for the tracking post — it never sees
    // Map.razor's private NpcState, only ids/callsigns/current physical state to sweep against.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate> TrackingCandidates()
    {
        var candidates = new List<SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate>(_npcStates.Length + _hunters.Count);
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Active && !npc.Arrived)
            {
                candidates.Add(new SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate(
                    npc.Ship.Id, npc.Ship.Callsign, npc.State,
                    npc.Ship.IsPod, $"cargo: {npc.Ship.CargoClass} ({npc.Ship.CargoUnits}u)"));
            }
        }

        // M27: hunters are sweepable and trackable too — THE targets the telescope should mind.
        foreach (HunterState hunter in _hunters)
        {
            candidates.Add(new SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate(
                hunter.Id, hunter.Callsign, hunter.State, IsThreat: true, CargoDetail: "hired muscle"));
        }

        return candidates;
    }

    // ---- M27: the eyes of the ship — active radar, interest target, intercept clock ----

    private bool _activeRadar;

    private void SetActiveRadar(bool on)
    {
        _activeRadar = on;
        if (on)
        {
            // The ping is loud: every ship in earshot learns exactly where we are.
            foreach (NpcState npc in _npcStates)
            {
                if (npc.Active && !npc.Arrived && RadarRule.HearsPing(_ship.Position, npc.State.Position))
                {
                    _trackingPost?.MarkAware(npc.Ship.Id);
                }
            }

            ShowPulseMessage("Active radar ON — exact returns close in; everyone in earshot hears us 📡");
        }
        else
        {
            ShowPulseMessage("Active radar off — back to passive silence");
        }

        StateHasChanged();
    }

    private void ZoomSensorsBackdrop(double factor) =>
        _camera.ZoomBy(factor, _viewportWidth / 2.0, _viewportHeight / 2.0);

    // M27: the sensor room screens the data; the war room consumes it (owner).
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack> WarRoomSensorTracks()
    {
        var tracks = new List<SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack>();
        if (_trackingPost is null)
        {
            return tracks;
        }

        foreach (TrackedTarget entry in _trackingPost.Entries)
        {
            string callsign = entry.ShipId;
            ShipState state = new(entry.LastObservation.Position, entry.LastObservation.Velocity, SimTime);
            bool isThreat = false;
            foreach (NpcState npc in _npcStates)
            {
                if (npc.Ship.Id == entry.ShipId) { callsign = npc.Ship.Callsign; state = npc.State; break; }
            }

            foreach (HunterState hunter in _hunters)
            {
                if (hunter.Id == entry.ShipId) { callsign = hunter.Callsign; state = hunter.State; isThreat = true; break; }
            }

            tracks.Add(new SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack(
                entry.ShipId, callsign, state, entry.EffectiveQuality(SimTime), isThreat));
        }

        return tracks;
    }

    // The ledger's "→ dossier" link (by ship id): switch to Comms and select that contact's detail.
    private void OpenDossierFromLedger(string shipId)
    {
        SwitchDesk(ShipDesk.Comms);
        SelectCommsShip(shipId);
    }

    private void LaserRangeTarget(string shipId)
    {
        if (_trackingPost is null)
        {
            return;
        }

        NpcState? npc = null;
        foreach (NpcState candidate in _npcStates)
        {
            if (candidate.Ship.Id == shipId) { npc = candidate; break; }
        }

        if (npc is null)
        {
            return;
        }

        (Observation obs, PingEvent _) = ActiveSensors.LaserRange(shipId, _ship.Position, npc.State.Position, npc.State.Velocity, SimTime);
        _trackingPost.ApplyObservation(obs);
        _trackingPost.MarkAware(shipId);
        ShowPulseMessage($"Laser ranged {npc.Ship.Callsign} — exact fix, but you're lit up ⚠");
    }
}
