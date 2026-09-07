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

/// <summary>
/// #172 · THE SKIP — fast-forwarding to the next thing that is actually going to happen, and the #261
/// COMPUTED coast that reckons a jump-scale wait (closed form, or chunked with yields) instead of
/// integrating it in a frozen tab.
///
/// <para><c>NextSkippableEvent</c> reads the SAME truths the banner's NEXT row reads — pending burns, the
/// armed insertion/arrival, the plan's furthest encounter, the next keeping trim — and lets
/// <c>WarpSkip</c> pick the soonest. Nothing is invented here.</para>
///
/// <para>Split out of <c>Map.LongHaul.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
/// What the skip REMEMBERS between frames is in <c>Map.LongHaul.State.cs</c>, where the base file kept
/// every field of this family in one run.</para>
/// </summary>
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
        // #989: the ⚓ cast off is lifted out of that pile into its own candidate. A departure scheduled a
        // day and a half out opens exactly the dead wait this control exists to eat ("⏭ Long coast ahead —
        // skip it" now offers the berth's own wait), and a captain sitting on the clamp is not waiting for
        // "the next burn" — the cast off spends nothing.
        double? nextBurn = null;
        double? nextCastOff = null;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Stale || node.Executed || node.SimTime <= now)
            {
                continue;
            }

            if (node.Kind == PlanStepKind.Undock)
            {
                nextCastOff = nextCastOff is { } c ? Math.Min(c, node.SimTime) : node.SimTime;
            }
            else
            {
                nextBurn = nextBurn is { } b ? Math.Min(b, node.SimTime) : node.SimTime;
            }
        }
        candidates.Add(new WarpSkip.Candidate(nextCastOff, WarpSkip.EventKind.CastOff));
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
        WarpSkip.EventKind.CastOff => _dockedHavenId is not null
            ? $"casting off from {BodyName(_dockedHavenId)}"
            : "the cast off",
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

    // How much coast is still ahead, for the advert copy — said in the SAME words the plan's own rows and
    // the pulse message below say it in (#989 clock). It used to round to whole sim-days on its own, which
    // meant the toolbar offered to skip "1 d" while the flight plan a finger away counted the very same
    // wait as "in 33 h". One clock, one sentence.
    private string LongCoastAheadReadout => FormatDuration(Math.Max(0, _skipNext.Epoch - SimTime));

    // ===== #246 🚀 LONG HAUL — the void is COMPUTED, not animated =====
    // Where #172's warp-skip INTEGRATES a coast you want to watch, the long haul JUMPS a void you don't:
    // it places the ship at the closed-form conic's arrival state (LongHaul.Project) and advances the
    // clock there. Everything time-derived (rails, heat, interest, pod/cache timers) is a pure function of
    // sim time, so the world is consistent by construction — the one non-pure actor, a hunter mid-chase,
    // is refused ("the sky is clear"), and the bus stops at the destination planet's capture range.

}
