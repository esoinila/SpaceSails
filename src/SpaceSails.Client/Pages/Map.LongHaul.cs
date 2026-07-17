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

// Map.LongHaul — crossing the deep black: jump engage, the void cinematic, skip-to-event and
// the closed-form coast that computes the wait instead of slogging it. #251 filing, motion only.
public partial class Map
{

    // ===== #172 — the skip machinery. NextSkippableEvent reads the SAME truths the banner's NEXT row
    // reads (pending burns, the armed insertion/arrival, the plan's furthest encounter, the next keeping
    // trim) and lets WarpSkip pick the soonest. Nothing invented. =====

    // The soonest upcoming event the skip can fast-forward to, or NextEvent.None when nothing is armed
    // (→ the control is disabled). Candidates mirror PlanFurthestEpochSeconds / the FlightNowNext queue.
    private WarpSkip.NextEvent NextSkippableEvent()
    {
        double now = SimTime;
        var candidates = new List<WarpSkip.Candidate>(6);

        // The soonest pending burn — a plotted node OR the armed transfer schedule's next unfired burn.
        double? nextBurn = null;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && !node.Executed && node.SimTime > now)
            {
                nextBurn = nextBurn is { } b ? Math.Min(b, node.SimTime) : node.SimTime;
            }
        }
        if (_armedTransferSchedule is { } sch && _armedTransferBurnsFired < sch.Burns.Count)
        {
            double e = sch.Burns[_armedTransferBurnsFired].SimTime;
            if (e > now)
            {
                nextBurn = nextBurn is { } b ? Math.Min(b, e) : e;
            }
        }
        candidates.Add(new WarpSkip.Candidate(nextBurn, WarpSkip.EventKind.Burn));

        // The armed orbit-insert / dock arrival window — the destination pass, the transfer schedule's
        // arrival, and the rehearsed path's final sample all name the same instant from the one truth.
        candidates.Add(new WarpSkip.Candidate(ArmedInsertionSimTime, WarpSkip.EventKind.Arrival));
        if (_armedTransferSchedule is { } s2)
        {
            candidates.Add(new WarpSkip.Candidate(s2.ArrivalTime, WarpSkip.EventKind.Arrival));
        }
        if (_armedOrbitBodyId is not null && _autopilotPlanPath is { Count: > 0 } path)
        {
            candidates.Add(new WarpSkip.Candidate(path[^1].SimTime, WarpSkip.EventKind.Arrival));
        }

        // While the autopilot HOLDS a kept orbit, the next station-keeping trim is the next event.
        if (_orbitKept)
        {
            candidates.Add(new WarpSkip.Candidate(_keepNextCheckTime, WarpSkip.EventKind.KeepTrim));
        }

        // The plan's furthest encounter — the fallback when nothing sooner is queued (same source the
        // plot ribbon reaches to). Zero means "no plan", which Resolve treats as no candidate.
        double furthest = PlanFurthestEpochSeconds();
        if (furthest > 0)
        {
            candidates.Add(new WarpSkip.Candidate(now + furthest, WarpSkip.EventKind.PlanEnd));
        }

        return WarpSkip.Resolve(now, candidates);
    }

    // One-voice words for an event kind (the readout, the announcement, the advert).
    private string SkipEventLabel(WarpSkip.EventKind kind) => kind switch
    {
        WarpSkip.EventKind.Burn => "the next burn",
        WarpSkip.EventKind.Arrival => _armedOrbitBodyId is not null
            ? $"the {BodyName(_armedOrbitBodyId)} arrival window"
            : "the arrival window",
        WarpSkip.EventKind.KeepTrim => "the next orbit trim",
        WarpSkip.EventKind.SensorPass => "the sensor pass",
        WarpSkip.EventKind.PlanEnd => "the plan's end",
        _ => "the next event",
    };

    // The captain pressed ⏭: lock the target and engage skip mode. Disabled when nothing is armed.
    private async Task StartSkip()
    {
        WarpSkip.NextEvent next = NextSkippableEvent();
        _skipNext = next;
        if (!next.Found)
        {
            return;
        }

        // #261 — a JUMP-SCALE coast is never integrated: that is the #255/#257 freeze class through the
        // skip's side door (a 717 d arrival coast = ~62M fixed-1 s steps, a pinned tab for minutes). Above
        // the threshold, compute the void — advance the conic in closed form and re-seed like the long haul,
        // or (if the leg isn't a clean heliocentric ballistic coast) chunk the integration with awaited
        // yields so the tab always paints. Below the threshold the honest tick-by-tick warp-skip stands.
        if (WarpSkip.IsJumpScale(next.Epoch - SimTime) && await TrySkipByComputation(next))
        {
            return;
        }

        _skipActive = true;
        _skipTargetEpoch = next.Epoch;
        _skipTargetKind = next.Kind;
        _skipTargetLabel = SkipEventLabel(next.Kind);
        Paused = false;
        PlotMode = false;
        int commanded = WarpSkip.SkipWarp(next.Epoch - SimTime, MaxWarpLevel);
        Warp = commanded;
        _skipWarpCommanded = commanded;
        LogAutopilotEvent($"⏭ skip engaged — fast-forwarding to {_skipTargetLabel}");
    }

    // The ■ stop-skip press (captain's hand wins): drop to 1× and let go.
    private void StopSkip()
    {
        if (!_skipActive)
        {
            return;
        }

        _skipActive = false;
        Warp = 1;
        _effectiveWarp = 1;
        LogAutopilotEvent("⏭ skip stopped by the captain");
    }

    private async Task ToggleSkip()
    {
        if (_skipActive)
        {
            StopSkip();
        }
        else
        {
            await StartSkip();
        }
    }

    // Try to CONSUME the jump-scale coast without integrating it. Returns true when it owned the skip (the
    // caller then skips the honest warp path); false only if it could not run at all (no ephemeris / a jump
    // already in progress), leaving the honest warp-skip to take it.
    private async Task<bool> TrySkipByComputation(WarpSkip.NextEvent next)
    {
        // Docked: the berth pins the ship (HoldAtDock overrides the integrator), so the honest skip already
        // advances the clock ONLY — cheap, freeze-free, and it keeps the ship on the dock. Never integrate a
        // docked ship here: RunAdaptive would fling it off the mass-less station. Defer to the honest path.
        if (_ephemeris is null || _jumpInProgress || _dockedHavenId is not null)
        {
            return false;
        }

        double targetEpoch = next.Epoch;
        string label = SkipEventLabel(next.Kind);

        // Honest closed form needs a BALLISTIC leg (no impulse to fire mid-coast) AND open heliocentric
        // cruise (the Simulator is n-body — the sun-relative conic is the true motion only clear of every
        // well). Either miss → chunked integration with yields (still no freeze, and no lie).
        CelestialBody? sun = _ephemeris.Bodies.FirstOrDefault(b => b.ParentId is null && b.Mu > 0);
        bool ballistic = WarpSkip.IsBallisticLeg(SimTime, targetEpoch, UpcomingBurnEpochs());
        bool heliocentricCruise = sun is not null && !LongHaul.InsideAnyWell(_ship, _ephemeris);

        if (ballistic && heliocentricCruise)
        {
            await ConsumeCoastClosedForm(sun!, targetEpoch, label);
        }
        else
        {
            await ConsumeCoastChunked(targetEpoch, label);
        }

        return true;
    }

    // The CLOSED-FORM path: advance the ship's conic to the target epoch in one pure computation, re-seed
    // the world there, autosave, and play a short "coast consumed" beat. No integration touches the void.
    private async Task ConsumeCoastClosedForm(CelestialBody sun, double targetEpoch, string label)
    {
        int days = (int)Math.Round((targetEpoch - SimTime) / DaySeconds);

        // Compute the arrival state BEFORE any side effect (mirrors the long haul's commit order).
        ShipState arrival = LongHaul.PropagateHeliocentricTo(_ship, _ephemeris!, sun, targetEpoch);

        // #255 vault safety: commit the personal life NOW, so a tab death mid-beat loses only the skip.
        RequestVaultSave();
        FlushVaultSaveIfDirty();

        // Freeze the tick (OnTick returns early on _jumpInProgress) and raise the beat.
        _jumpInProgress = true;
        _coastSkipActive = true;
        _coastSkipDays = days;
        _coastSkipLabel = label;
        StateHasChanged();
        await Task.Delay(700); // a beat to read the state — the coast is gone, not ground

        // Commit atomically: re-seed the world at the target epoch (drops stale movers, keeps depot rails,
        // arms RefillTraffic — the same tested mechanism the long haul uses), place the ship, advance clock.
        ReseedWorldForJump(targetEpoch);
        _ship = arrival;
        SimTime = arrival.SimTime;

        StaleFutureNodes();
        _skipActive = false;
        _passDirty = true;
        _scrubOffsetSeconds = 0;

        _coastSkipActive = false;
        _jumpInProgress = false;

        ShowPulseMessage(WarpSkip.CoastConsumedAnnounce(days, label));
        LogAutopilotEvent(WarpSkip.CoastConsumedAnnounce(days, label));
        RequestVaultSave();
        ReprojectTrajectory();
        StateHasChanged();
    }

    // The FALLBACK path (a burn mid-leg, or in a planet's well where closed form would lie): still fast-
    // forward, but by INTEGRATING in awaited chunks that paint between them — never a dead frame (issue #261
    // option 1). Runs the same adaptive integrator the live loop uses, with the plan, so burns fire honestly;
    // lands exactly on the target epoch. The overlay ticks the remaining days down as it goes.
    private async Task ConsumeCoastChunked(double targetEpoch, string label)
    {
        const double ChunkSeconds = 6.0 * 3600.0; // 6 sim-hours reckoned per painted chunk

        _jumpInProgress = true;
        _coastSkipActive = true;
        _coastSkipLabel = label;

        while (SimTime < targetEpoch - WarpSkip.ArriveToleranceSeconds)
        {
            double span = Math.Min(ChunkSeconds, targetEpoch - SimTime);
            _ship = _simulator!.RunAdaptive(_ship, span, _plan);
            SimTime = _ship.SimTime;
            _coastSkipDays = (int)Math.Round((targetEpoch - SimTime) / DaySeconds);
            StateHasChanged();
            await Task.Yield(); // let the tab paint this chunk before the next
        }

        ReseedWorldForJump(targetEpoch);
        SimTime = _ship.SimTime;

        StaleFutureNodes();
        _skipActive = false;
        _passDirty = true;
        _scrubOffsetSeconds = 0;

        _coastSkipActive = false;
        _jumpInProgress = false;

        ShowPulseMessage($"⏭ arrived at: {label}");
        LogAutopilotEvent($"⏭ coast integrated in chunks — arrived at {label}");
        RequestVaultSave();
        ReprojectTrajectory();
        StateHasChanged();
    }

    // Interruptions win, always. Called by the interruption sites that do NOT already yank warp (a fuel
    // AMBER crossing, a boarding offer). The DriveSkip catch-all covers every site that DOES set Warp=1.
    private void EndSkipIfActive(string reason)
    {
        if (!_skipActive)
        {
            return;
        }

        _skipActive = false;
        Warp = 1;
        _effectiveWarp = 1;
        LogAutopilotEvent($"⏭ skip stopped — {reason}");
    }

    // Runs at the very top of every frame, BEFORE UpdateEffectiveWarp. Owns the Warp value while
    // skipping: arrive+announce at the target epoch, stop on any external warp write (a yank or the
    // helm), else crank toward the target eased in the final approach.
    private void DriveSkip()
    {
        if (!_skipActive)
        {
            return;
        }

        // Reached the target — announce and drop to realtime. Covers UN-guarded events (plan end, keep
        // trim) that no arrival guard stops, and guarded ones on the frame the guard lands.
        if (WarpSkip.HasArrived(SimTime, _skipTargetEpoch))
        {
            _skipActive = false;
            Warp = 1;
            _effectiveWarp = 1;
            ShowPulseMessage($"⏭ arrived at: {_skipTargetLabel}");
            LogAutopilotEvent($"⏭ skip arrived at {_skipTargetLabel}");
            return;
        }

        // Something changed warp out from under the skip since last frame — an interruption yank
        // (collision, fuel red, handback, arrival) or the captain's own hand. Stop; the yank site already
        // said WHY. This is the catch-all behind the explicit EndSkipIfActive cancels.
        if (Warp != _skipWarpCommanded)
        {
            _skipActive = false;
            LogAutopilotEvent("⏭ skip stopped — an event needs the captain (or the helm was touched)");
            return;
        }

        // Crank toward the target; UpdateEffectiveWarp still clamps this to the neighborhood ceiling.
        int commanded = WarpSkip.SkipWarp(_skipTargetEpoch - SimTime, MaxWarpLevel);
        Warp = commanded;
        _skipWarpCommanded = commanded;
        Paused = false;
    }

    // The long-coast advert edge (owner addition): squawk + log ONCE per long leg; re-arm on the next.
    // The clickable chip's presence is computed (ShowLongCoastAdvert); this only fires the once-per-leg
    // shout. Called after UpdateShipAlerts so an alert that just cancelled skip wins this same frame.
    private void EvaluateLongCoastAdvert(double nowMs)
    {
        _skipNext = NextSkippableEvent();
        WarpSkip.LongCoastDecision d = WarpSkip.EvaluateLongCoast(
            _longCoast, _skipActive, _skipNext, SimTime, WarpSkip.LongCoastThresholdSeconds);
        _longCoast = d.State;
        if (d.Fire)
        {
            SquawkNow(Parrot.Squawk.LongHaul, nowMs);
            LogAutopilotEvent(
                $"⏭ long coast ahead — {FormatDuration(_skipNext.Epoch - SimTime)} to {SkipEventLabel(_skipNext.Kind)}; " +
                "the coast is free, only time passes — hit ⏭ to skip it");
        }
    }

    // The advert chip shows whenever a long dead coast is available to skip and we aren't already on the
    // ride. A computed presence (not a flag) so it never sticks around stale.
    private bool ShowLongCoastAdvert =>
        !_skipActive && _skipNext.Found && (_skipNext.Epoch - SimTime) > WarpSkip.LongCoastThresholdSeconds;

    // Whole sim-days of coast still ahead, for the advert copy.
    private int LongCoastDays => (int)Math.Round((_skipNext.Epoch - SimTime) / DaySeconds);

    // ===== #246 🚀 LONG HAUL — the void is COMPUTED, not animated =====
    // Where #172's warp-skip INTEGRATES a coast you want to watch, the long haul JUMPS a void you don't:
    // it places the ship at the closed-form conic's arrival state (LongHaul.Project) and advances the
    // clock there. Everything time-derived (rails, heat, interest, pod/cache timers) is a pure function of
    // sim time, so the world is consistent by construction — the one non-pure actor, a hunter mid-chase,
    // is refused ("the sky is clear"), and the bus stops at the destination planet's capture range.

    // The last-mile pulse quote from the current course's destination pass, when the plot has one.
    private int LongHaulLastMilePulses() =>
        _destinationPass is { } dp && DestinationPassInfo(dp) is { } info ? info.EstPulses : 0;

    // A destination is a genuine long-haul target when its own sun-orbiting planet exists AND the ship is
    // not already inside that planet's capture range (there is a real void to cross). Drives the offer's
    // visibility on the map menu, the nav card, and the toolbar chip — never gated on the CURRENT coast
    // reaching, which from a berth it never does (#249 fix).
    private CelestialBody? LongHaulTargetPlanet(string? destBodyId)
    {
        if (_ephemeris is null || destBodyId is null || LongHaul.JumpTargetPlanet(_ephemeris, destBodyId) is not { } planet)
        {
            return null;
        }

        CelestialBody? sun = planet.ParentId is null ? null : _ephemeris.Bodies.FirstOrDefault(b => b.Id == planet.ParentId);
        if (sun is null)
        {
            return null;
        }

        double capture = OrbitRule.CaptureRange(OrbitRule.HillRadius(planet, sun.Mu));
        double dist = (_ship.Position - _ephemeris.Position(planet.Id, SimTime)).Length;
        return dist > capture ? planet : null; // already in the vicinity → nothing to haul
    }

    // The gate for the DEPARTURE-based offer: null = clear to engage; else the spoken reason (visible-but-
    // disabled, never hidden — #212). Order: hunter on the board, a kept orbit to disarm, the planner can't
    // solve, then the tank can't afford it. No InsideWell check — the departure burn IS the well-escape;
    // engaging from a berth is exactly the point (the undock is part of engaging).
    private string? LongHaulOfferBlock(LongHaul.Departure? departure, string planetName)
    {
        if (_hunters.Any(h => !h.BrokenOff && !h.CaughtPlayer))
        {
            return LongHaul.RefusalText(LongHaul.Blocker.HunterActive, planetName);
        }

        if (_orbitKept)
        {
            return LongHaul.RefusalText(LongHaul.Blocker.Keeping, planetName);
        }

        if (departure is not { Ok: true } dep)
        {
            return "🚀 " + (departure?.Failure ?? "no departure arc from here yet — give the plot a moment");
        }

        int reserve = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
        int budget = Math.Max(0, _reactionMassPulses - reserve);
        return dep.DeparturePulses > budget ? LongHaul.RefusalBudget(dep.DeparturePulses, _reactionMassPulses) : null;
    }

    // The destination planet's capture range (the void mode's stop), for the promise's "capture (X AU)".
    private double LongHaulCaptureRange(CelestialBody planet)
    {
        CelestialBody? sun = planet.ParentId is { } pid ? _ephemeris?.Bodies.FirstOrDefault(b => b.Id == pid) : null;
        return sun is null ? OrbitRule.CaptureRangeFloorMeters : OrbitRule.CaptureRange(OrbitRule.HillRadius(planet, sun.Mu));
    }

    // The nav-card 🚀 button — uses the reproject-computed departure for the current destination.
    private Task EngageLongHaul() => EngageLongHaulTo(_destinationBodyId, _longHaulPlanet, _longHaulDeparture);

    // The MAP CONTEXT-MENU primary entry (owner refinement): one click SETS the destination AND engages.
    // Solves the departure fresh from the current state so it works straight off a berth, no card-hunting.
    private async Task EngageLongHaulFromMenu(string bodyId)
    {
        CloseBodyMenu();
        SetDestination(bodyId);
        if (_ephemeris is null || LongHaul.JumpTargetPlanet(_ephemeris, bodyId) is not { } planet)
        {
            return;
        }

        await EngageLongHaulTo(bodyId, planet, LongHaul.SolveDeparture(_ship, _ephemeris, planet));
    }

    // Engage the haul (#246 solve → #255 crossing): refuse-with-reason if the gate is shut; else the solve
    // is in hand — charge the departure pulses, place the ship at the SOLVED conic's capture-range arrival
    // (reaches by construction), and advance the clock there. The void is NEVER integrated: instead the
    // world is RE-SEEDED at the arrival epoch with the same tested spawn/wave code a fresh boot uses (owner
    // 2026-07-17: "just use the mechanism we have for the different spawn points — the physics is preserved,
    // and that spawning code is tested to work"). Personal continuity (purse, heat, contacts, caches,
    // quests, insurance, upgrades) rides the live fields untouched; every time-derived personal system keys
    // off an ABSOLUTE sim-time checkpoint, so a decade of decay/accrual applies itself the moment the clock
    // jumps — no replay needed. The crossing runs as an awaited, painting cinematic so the tab never freezes.
    private async Task EngageLongHaulTo(string? destBodyId, CelestialBody? planet, LongHaul.Departure? departure)
    {
        if (_ephemeris is null || planet is null || _jumpInProgress)
        {
            return;
        }

        string destName = destBodyId is { } d ? BodyName(d) : planet.Name;
        string? block = LongHaulOfferBlock(departure, planet.Name);
        if (block is not null)
        {
            ShowPulseMessage(block);
            return;
        }

        LongHaul.Departure dep = departure!.Value;

        // Compute the jump BEFORE committing any side effect: ride the post-burn conic to the capture gate.
        ShipState postBurn = _ship with { Velocity = dep.PostBurnVelocity };
        double horizon = (dep.ArrivalCenterTime - postBurn.SimTime) + 30.0 * DaySeconds;
        LongHaul.Reach reach = LongHaul.Project(postBurn, _ephemeris, planet, horizon);
        if (!reach.Reaches)
        {
            ShowPulseMessage($"🚀 the solved arc slipped past {planet.Name} — re-plot and try the long haul again");
            return; // vanishingly unlikely: the post-burn conic targets the planet by construction
        }

        double crossingSeconds = reach.ElapsedSecondsFrom(postBurn.SimTime);
        double daysPassed = Math.Round(crossingSeconds / DaySeconds);
        double arrivalEpoch = reach.ArrivalSimTime;

        // Commit the departure: undock (part of engaging), charge the honest pulses, flip the banner.
        if (_dockedHavenId is not null)
        {
            Undock();
        }

        ShowPulseMessage(LongHaul.BannerNow(destName));
        // #268 pay-at-the-pump — CORRECT AS-IS, left alone. Unlike ⚓ Match & clamp, this charge IS the
        // burn firing: the departure burn genuinely fires HERE, at engage, before the void-crossing jump
        // (#249/#250's burns-then-jumps order — the post-burn conic above was computed FROM this Δv). So
        // taking the pulses now is charging as the burn executes, not billing a flight the ship hasn't flown.
        _reactionMassPulses = Math.Max(0, _reactionMassPulses - dep.DeparturePulses);

        // #255 VAULT SAFETY (pre-advance autosave): commit the personal life NOW, so a tab death mid-crossing
        // loses only the jump itself. The undock above already set a berth-resume near the departure.
        RequestVaultSave();
        FlushVaultSaveIfDirty();

        // Raise the diegetic overlay and FREEZE the tick (the re-seed owns the clock; nothing integrates).
        // The #246 bottle-pop squawk fires at engage; ESC/cancel is not offered — the overlay says so.
        _jumpTotalYears = LongHaul.VoidYears(crossingSeconds);
        _jumpYear = 0;
        _jumpDestName = destName;
        _jumpFlavor = VoidFlavor(_jumpTotalYears);
        _jumpActive = true;
        _jumpInProgress = true;
        SquawkNow(Parrot.Squawk.LongHaul, _frameNowMs, force: true);
        RendererInterop.PlayCue("voidjump");
        StateHasChanged();

        // Play the crossing as a short cinematic beat — the year counter ticks up over a couple of seconds
        // while the tab stays fully responsive (there is NOTHING heavy to compute: the world is deterministic
        // from sim time, so the jump is a clock advance + a cheap re-seed, not a decade of integration).
        await RunVoidCinematic();

        // COMMIT the crossing atomically: re-seed the world at the arrival epoch, place the ship at the
        // capture-range arrival, advance the clock.
        ReseedWorldForJump(arrivalEpoch);
        _ship = reach.ArrivalState;
        SimTime = reach.ArrivalSimTime;

        StaleFutureNodes();          // any pending burns are behind us now
        ResetAutopilotBudget();
        _autopilotStandDownReason = null;
        _skipActive = false;         // any warp-skip is moot now
        _passDirty = true;           // reproject from the arrival state
        _scrubOffsetSeconds = 0;

        _jumpInProgress = false;
        _jumpActive = false;

        // Announce the arrival, book the ledger line, and autosave the FAR side of the void.
        ShowPulseMessage(LongHaul.Completed(destName, (int)daysPassed));
        RendererInterop.PlayCue("board");
        PushNewsEvent(NewsWire.NewsEventKind.LongHaulComplete, destName, $"{(int)daysPassed} d crossed");
        RequestVaultSave();
        ReprojectTrajectory();
        StateHasChanged();
    }

    // #255 — re-seed the world at the arrival epoch WITHOUT integrating the void or freezing the tab. The
    // depots are pure rails (StepNpcs recomputes each from the new sim time — correct at any epoch, zero
    // cost), so they are kept as-is. Every transient stateful actor — mid-flight strangers, pods, a decoy
    // ghost, rounds in flight — belonged to the world we departed a decade ago and is dropped (a decade past
    // its last plotted node it has long arrived/expired anyway; see the DecadeJump_Retires… test). Fresh
    // ambient traffic repopulates AT the arrival epoch through the existing, tested RefillTraffic wave path
    // over the following sim-hour — deliberately NOT run here, because NPC route-planning is far too heavy to
    // do at the (frozen) engage moment on the interpreted-WASM build (measured: ~20-40 s per hauler). This is
    // the owner's architecture — reuse the tested spawn mechanism, physics preserved — with the planning cost
    // kept off the dramatic beat. Transient world reset is acceptable (the hunter-refusal law already bars
    // jumping out of a live chase).
    private void ReseedWorldForJump(double epoch)
    {
        _npcStates = _npcStates.Where(n => n.Ship.DepotBodyId is not null).ToArray();
        _lastRefillCheckSimTime = epoch; // let RefillTraffic repopulate movers over the next sim-hour of play
        _ordnance.Clear();               // rounds in flight belonged to the world we left
        _beaconGhost = null;             // any decoy ghost is a decade stale
        _pursuitTrail.Clear();
    }

    // The crossing cinematic: tick the year counter 1 → total over ~2.2 s of theater, then hold the full bar
    // a beat before the void lets out. Pure UI — the tab paints every frame (no heavy work runs during it).
    private async Task RunVoidCinematic()
    {
        int perTickMs = Math.Clamp(2200 / Math.Max(1, _jumpTotalYears), 90, 400);
        for (int year = 1; year <= _jumpTotalYears; year++)
        {
            _jumpYear = year;
            StateHasChanged();
            await Task.Delay(perTickMs);
        }

        await Task.Delay(260);
    }

    // The overlay's parrot-aging wink, scaled to how much void is being crossed (owner: "maybe the parrot
    // aging jokes"). Pure flavor; the bird gets saltier the longer the dark.
    private static string VoidFlavor(int years) => years switch
    {
        <= 1 => "🦜 the parrot barely blinks — a short dark",
        <= 3 => "🦜 the parrot preens through the quiet years",
        <= 6 => "🦜 the parrot has picked up two new swear words by now",
        _ => "🦜 the parrot is greying at the crest — mind the long years out here",
    };
    private bool _skipActive;                                     // the skip is fast-forwarding now
    private double _skipTargetEpoch;                              // sim-time it is aiming at
    private WarpSkip.EventKind _skipTargetKind;                   // what kind of event that is
    private string _skipTargetLabel = "";                         // one-voice words for the readout
    private int _skipWarpCommanded;                               // the warp DriveSkip last set (catch-all)
    private WarpSkip.NextEvent _skipNext = WarpSkip.NextEvent.None; // cached per frame for the button/chip
    private WarpSkip.LongCoastState _longCoast = WarpSkip.LongCoastState.Idle; // long-coast advert edge

    // #261 — the COMPUTED skip's beat: a jump-scale coast is reckoned (closed form) or chunked (yields),
    // never integrated into a frozen tab. Drives the "COAST CONSUMED" overlay while the void is consumed.
    private bool _coastSkipActive;
    private int _coastSkipDays;
    private string _coastSkipLabel = "";
    private LongHaul.Reach? _longHaulReach;           // #246: the CURRENT coast's reach — the manual-coast promise verdict ONLY
    private CelestialBody? _longHaulPlanet;           // #246: the destination's sun-orbiting planet, named in the promise/verdict
    private LongHaul.Departure? _longHaulDeparture;   // #246/#249 fix: the solved cheap departure the OFFER quotes and engages

    // #255 — the diegetic jump overlay + the re-seed guard. While _jumpInProgress the tick loop freezes
    // (OnTick returns early) so the world never integrates the void; _jumpActive drives the full-screen
    // "CROSSING THE VOID" overlay whose year counter ticks as the arrival-epoch fleet re-seeds.
    private bool _jumpActive;
    private bool _jumpInProgress;
    private int _jumpYear;
    private int _jumpTotalYears;
    private string _jumpDestName = "";
    private string _jumpFlavor = "";
}
