using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Npc — THE TRAFFIC, AND THE TICK THAT MOVES IT. Carved off Map.razor for #251 (motion only) and
// split by concern at 1,376 lines; the other four partials are Map.Npc.Tasking/Dossier/Draw/Selection
// and every one of them reads the fields declared here. This file is what a contact IS — `NpcState`, one
// mutable client-side wrapper per scheduled hull, mirroring PlanNode's role over Core's records — and the
// four things done to the whole sky each tick: `StepNpcs` integrates everybody who is flying by now,
// `SweepSensors` is the honour-system visibility pass (the server enforces it from M9), `RefillTraffic`
// keeps the owner's law that the sky must never empty, and `UpdatePrediction`/`BuildPinnedPlan` re-solve
// the pinned contact's brake-at hypothesis when the cone has gone stale.
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
    public sealed class NpcState
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
        public bool Broke;               // #534: she has already made her one break for open water
    }
    private static readonly RgbaColor DisabledNpcColor = new(150, 150, 160);

    private string NpcName(string id) => FindNpc(id)?.Ship.Callsign ?? id;

    private void StepNpcs()
    {
        // #534 · Before anybody is integrated: a hull that is not a merchant, with the captain inside her
        // boarding envelope, puts a break into her own plan. Honest traffic never reaches this (Map.Npc.QShip).
        LetTheMaskedHullsRun();

        foreach (NpcState npc in _npcStates)
        {
            if (npc.Arrived)
            {
                continue;
            }

            if (!SheIsOnHerRouteByNow(npc))
            {
                continue;
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
                // #264: NPCs are deliberately NOT impact-enforced. They fly scheduled trader/hunter
                // routes that never dive under a surface (a plan that did would despawn on arrival, not
                // impact), and there is no death/re-birth flow for an NPC to enter. SurfaceImpact is
                // built step-robust precisely so the PLAYER's detection can't be tunnelled by a coarse
                // step; the 60 s NPC cadence needs no crossing check because nothing here can strike.
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
        // #973 L4 · …and the one hull this world has to grow for itself is berthed before the scope looks at
        // anything. Lazy off the universe's id (see EnsureTheOldShipIsBerthed) and a single string compare
        // once she is there, so the sweep pays nothing for her after the first pass.
        EnsureTheOldShipIsBerthed();

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
        KeepTheLessonsPreyInTheWorld(); // #351 — …and a running lesson's own prey is part of that sky
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
}
