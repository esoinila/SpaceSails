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

// Map.Autopilot — the pilot's hands: rehearsal and promise, arm and stand-down, the transfer
// burns, and the station-keeping that holds a KEPT orbit. The ancients' pyramid pilot-grants
// ride here too. Lifted whole from Map.razor for #251.
public partial class Map
{

    // ===== PR-D1: the burn list read AS a flight plan (docs/WednesdayPlan/UnifiedNavListNotes.md) =====
    // Read-only derivation over existing state — NO flight-logic changes. Armed auto-orbit gains list
    // presence as a step; the owner's NOW/next status line and the step counter are derived once, here,
    // so the pilot banner and the Nav-desk header never disagree. FlightPlanStatusBuilder (Core, unit-
    // tested) owns the state + now/next decisions; this only feeds it facts already on screen elsewhere.

    // The autopilot is FLYING THE APPROACH (vs merely armed and waiting for the window) when it is armed
    // AND already within capture range — the same gate OrbitStatusLine reports as "flying the approach".
    private bool AutopilotFlyingApproach =>
        _armedOrbitBodyId is not null && !_orbitKept && OrbitInfo() is { Armed: true, InCaptureRange: true };

    // The armed insertion's time when the plotted destination pass pins it; null = "at window" (unknown).
    private double? ArmedInsertionSimTime =>
        _armedOrbitBodyId is not null && _destinationPass is { } dp
            && dp.BodyId == _armedOrbitBodyId && dp.SimTime > SimTime
            ? dp.SimTime : null;

    private void ToggleInsertionEditor()
    {
        _openEditor = _openEditor == FlightEditorKind.Insertion ? FlightEditorKind.None : FlightEditorKind.Insertion;
        _selectedPlanNode = null;
    }
    // The body the ship is gravitationally bound to right now (M20 orbit rules) — ground truth
    // for CommerceRule's "same orbit" case, independent of the orbit-assist UI's armed/nearest
    // framing. Cached alongside its position/Hill radius so DrawNpcs can ring-highlight co-orbiting
    // contacts without recomputing them.
    private string? _orbitedBodyId;
    private Vector2d _orbitedBodyPosition;
    private double _orbitedBodyHillRadius;

    // #265 — the period (s) of the ship's currently-achieved bound orbit about its dominant body, or null
    // when the ship is on a transfer/hyperbolic leg (not captured). Reads the SAME body + Hill the orbit
    // panel judges capture against (OrbitInfo), so "bound" here and the panel's "bound — parked" never
    // disagree. Cheap: a finite-difference body velocity and one energy/√ in OrbitRule.BoundOrbitPeriod.
    private double? BoundOrbitPeriodSeconds()
    {
        if (_ephemeris is null || OrbitInfo() is not { Bound: true } oi)
        {
            return null;
        }
        Vector2d pos = _ephemeris.Position(oi.Body.Id, SimTime);
        const double h = 1.0;
        Vector2d vel = (_ephemeris.Position(oi.Body.Id, SimTime + h) - _ephemeris.Position(oi.Body.Id, SimTime - h)) / (2 * h);
        return OrbitRule.BoundOrbitPeriod(_ship, pos, vel, oi.Body, oi.Hill);
    }

    // ---- M28 (Sunday PR-D): the Ancients' pilot — pyramid satellites & auto-plot charges ----
    private int _ancientCharges;
    private readonly double[] _ancientLastGrant =
        [double.NegativeInfinity, double.NegativeInfinity];
    private static readonly RgbaColor PyramidColor = new(255, 215, 120);

    /// <summary>Runs on the sensor cadence: a pyramid close enough to touch grants charges.</summary>
    private void CheckPyramids()
    {
        for (int i = 0; i < AncientsRule.PyramidCount; i++)
        {
            if (SimTime - _ancientLastGrant[i] < AncientsRule.GrantCooldownSeconds
                || !AncientsRule.InGrantRange(i, _ship.Position, SimTime))
            {
                continue;
            }

            _ancientLastGrant[i] = SimTime;
            _ancientCharges += AncientsRule.ChargesPerVisit;
            ShowPulseMessage($"◬ The pyramid regards you. {AncientsRule.ChargesPerVisit} plottings are granted.");
            RendererInterop.PlayCue("board");
            StateHasChanged();
        }
    }

    /// <summary>Spends a charge: the ancient pilot replaces the maneuver plan with a course
    /// to the current destination — the same Simulator-evaluated search that plans NPC
    /// routes, offered as scarce alien assistance. Manual flight stays the taught skill.</summary>
    private void UseAncientsPilot()
    {
        if (_ancientCharges <= 0 || _destinationBodyId is null || _ephemeris is null)
        {
            return;
        }

        ShowPulseMessage("◬ The ancient pilot considers the sky…");
        if (AncientsRule.AutoPlot(_ephemeris, _ship, _destinationBodyId) is not { } result)
        {
            return;
        }

        _ancientCharges--;
        _planNodes.Clear();
        foreach (ManeuverNode node in result.Plan.Nodes)
        {
            _planNodes.Add(new PlanNode { SimTime = node.SimTime, Action = node.Action, Pulses = node.Pulses });
        }

        RebuildPlan();
        ReprojectTrajectory();
        ShowPulseMessage($"◬ Course laid — closest approach {FormatDistance(result.MissDistance)} at {FormatSimTime(result.ClosestApproachSimTime)}. Mind HOW it flies.");
        StateHasChanged();
    }
    // The pilot's most-wanted number (owner, M16): the speed that holds a circular sun orbit
    // at the ship's CURRENT distance. Match it (tangentially) and you coast forever — the
    // difference between "matching the radius" and "matching the orbit".
    private double CircularSpeedHere
    {
        get
        {
            double r = _ship.Position.Length;
            if (r <= 0 || _ephemeris is null) return 0;
            double mu = 0;
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.ParentId is null && body.Mu > mu) mu = body.Mu;
            }
            return Math.Sqrt(mu / r);
        }
    }

    // ---- M22: planned insertion — "it is part of flight planning" (owner) ----
    private string? _armedOrbitBodyId;

    // #136 convergence watchdog: an armed approach that fires burns without ever beating its
    // closest-ever distance is going nowhere (bad geometry, or the old empty-window fuel trap).
    // Track burns and the best distance so the tick executor can stand the autopilot down and
    // preserve the remaining fuel instead of firing forever with no feedback (the owner's report).
    private int _approachBurnCount;
    private int _approachStalledBurns;
    private double _approachMinDistance = double.MaxValue;
    private const int AutopilotMaxStalledBurns = 6;

    // Fresh convergence tracking for a new (or ended) armed session.
    private void ResetApproachTracking()
    {
        _approachBurnCount = 0;
        _approachStalledBurns = 0;
        _approachMinDistance = double.MaxValue;
    }

    // ---- The autopilot's promise (issues #146/#147). The whole feasibility question is settled at
    // arm time by AutopilotRehearsal, which flies the armed journey in Core and prices it. An
    // affordable trip is armed with its quoted budget REMEMBERED (shown on the insertion step); an
    // un-affordable one is refused with the numbers and never armed. In flight the autopilot keeps a
    // reserve floor: it never burns the tank below it, so it is never stranded — and any stand-down
    // (reserve reached, watchdog, refusal) is LOUD: a persistent reason, a ledger receipt, warp
    // dropped to 1×. The owner's ruling: "dropping from autopilot should never happen when there was
    // nothing external to cause it." ----
    private int _armedBudgetPulses;              // rehearsal-quoted total cost for the current arm
    private int _armedSpentPulses;               // approach+insert pulses spent since this arm
    private string? _autopilotStandDownReason;   // persistent "you have the ship" line (decline/handback)
    private string? _dockReadyStatus;            // #155: persistent "in the envelope — hit ⚓ Dock" line, a station SUCCESS (never a #147 handback)
    private IReadOnlyList<TrajectorySample>? _autopilotPlanPath; // #148: the rehearsed INTENDED path
    // #196: the tightest pass of the rehearsed plan path, cached at arm time (the plan is fixed, so it
    // costs one MostSevere pass, not one per tick). The collision alarm judges THIS while armed — the
    // insert burn resolves the ballistic impact, so that impact is the plan working, not news. A plan
    // whose OWN path goes subsurface leaves this an Impact pass, and the alarm shouts red immediately.
    private ClosestApproach.Pass? _autopilotPlanClosestPass;

    // ---- #179: disarming the autopilot is what dropped the owner off orbit; confirm it once. The
    // first disarm click arms this pending flag (with a short expiry) and asks; a second click within
    // the window actually stands down. No browser confirm() — those hang automation. ----
    private string? _disarmConfirmBodyId;        // body whose disarm is pending a confirming second click
    private double _disarmConfirmExpiresMs;      // rAF-clock deadline for that second click
    private const double DisarmConfirmWindowMs = 4000;

    // ---- #180 orbit-degradation alert. Edge-triggered off OrbitRule.ParkStability for the bound
    // body: on a transition INTO the tide-chaotic band (amber) or a surface-grazing orbit (red) we
    // raise a persistent pilot-banner warning, drop warp to 1×, and log it; it clears when stability
    // returns. TODO(#166): migrate this into the ShipAlerts channel (+🦜) when that lands. ----
    private string? _orbitDegradeWarning;                 // persistent amber/red "orbit degrading" line
    private int _orbitDegradeSeverity;                    // 0 none · 1 TideRisk (amber) · 2 Subsurface (red)
    private OrbitRule.ParkStabilityVerdict _lastParkStability = OrbitRule.ParkStabilityVerdict.NotBound;
    private string? _parkStabilityBodyId;                 // body _lastParkStability refers to (fresh watch on change)

    // ---- Friday §0 (owner ruling): "armed auto-orbit ends in a KEPT orbit, not an achieved one."
    // When the autopilot inserts, it does NOT hand the ship back — it enters STATION-KEEPING for
    // _armedOrbitBodyId: holding the park with trim burns (OrbitKeeping, budgets from Lab 25) until
    // the captain deliberately disarms (the #179 double-confirm) or the tank runs dry (a LOUD
    // handback, after which the #180 degradation alert is the backstop). The status reads "AUTOPILOT
    // HOLDS THE ORBIT", never "you have the ship", expressed through FlightPlanStatus. ----
    private bool _orbitKept;                              // parked and now station-keeping _armedOrbitBodyId
    private int _keepTrimPulsesPerDay;                    // Lab 25 trim budget quoted at arm time / at park
    private double _keepNextCheckTime;                    // sim-time of the next trim-cadence check
    private int _keepTrimsFired;                          // trims spent this keeping session (diagnostic/ledger)
    // #220: the next trim is affordable — recomputed every tick keeping is active (CheckArmedInsertion's
    // kept branch). Read ONLY as `_orbitKept && _keepTrimFunded`, so a stale value after a disarm/handback
    // (when _orbitKept is already false) can never keep the collision alarm falsely trusting a dead keep.
    private bool _keepTrimFunded;

    // ---- #146 the moon run: the cached in-well transfer plan for the current arm. When arming rides a
    // cheap Lambert arc (TransferPlanner) instead of the legacy approach loop, the schedule's departure
    // burn(s) are fired EXACTLY at their epochs by the tick-advance split, and AutopilotDecision stays
    // muzzled until the arc is honestly near the target (CheckArmedInsertion's gate) so the giant's pull
    // can't restart the velocity-reset bleed straight through the cheap arc. ----
    private TransferPlanner.Schedule? _armedTransferSchedule; // null when this arm flies the legacy loop
    private string? _armedTransferSummary;                    // the planner's one-line quote for the status
    private int _armedTransferBurnsFired;                     // how many scheduled burns have already fired

    // The autopilot has stood down (refused to arm, or handed the ship back) and the captain has not
    // yet re-engaged — the single predicate every desk chip and the pilot banner read so none can
    // claim a mission the autopilot no longer flies (#147).
    private bool AutopilotStoodDown => _armedOrbitBodyId is null && _autopilotStandDownReason is not null;

    private void ResetAutopilotBudget()
    {
        _armedBudgetPulses = 0;
        _armedSpentPulses = 0;
        _autopilotPlanPath = null;
        _autopilotPlanClosestPass = null; // #196: plan gone — the alarm returns to the ballistic course
        _armedTransferSchedule = null;
        _armedTransferSummary = null;
        _armedTransferBurnsFired = 0;
        _orbitKept = false;
        _keepTrimPulsesPerDay = 0;
        _keepTrimsFired = 0;
    }

    // #219 one-arm semantics: the collision alarm's plan-trust is only sound if EVERY arm caches BOTH
    // the plan PATH (drawn as the #148 intended track, and the `armedWithPlan` gate in UpdateShipAlerts)
    // AND the plan's collision PASS, together — one drifting without the other is exactly the #196/#219
    // bug (a plan the ballistic alarm then judges raw). All arm entry points already funnel through the
    // single ToggleArmedInsertion — the destination card's Auto-orbit button, the nav-target panel's Arm
    // button, the body context menu (ToggleArmedInsertionFromMenu), the O-key, and the #183 out-of-band
    // manual press (EnterOrbit, when the current radius is tide-chaotic) — so this is the ONE place the
    // pair is set. The pass is the plan's ACHIEVED PARK, not its powered approach: a deliverable
    // rehearsal's coarse terminal coast grazes the surface a step before the insert lifts it back, and
    // that graze must NOT fire ROCKS AHEAD on a valid armed approach (AutopilotRehearsal.PlanCollisionPass).
    private void CachePlanForAlarm(string bodyId, AutopilotRehearsal.RehearsalResult r)
    {
        _autopilotPlanPath = r.Path;
        _autopilotPlanClosestPass = _ephemeris is null
            ? null
            : AutopilotRehearsal.PlanCollisionPass(r, _ephemeris, bodyId);
    }

    // #146: does this arm ride a cheap in-well transfer rather than the legacy approach loop? The target
    // is a moon of a moon-owning giant, the ship is free-flying INSIDE that giant's Hill sphere, and it
    // is still OUTSIDE the target's honest (floor-free) Hill-scaled capture range — the exact geometry
    // where OrbitRule.Approach re-sets the velocity every step and hemorrhages, and where the Lambert
    // planner rides the well cheaply instead.
    private bool ShouldPlanTransfer(CelestialBody target, out CelestialBody parent, out double targetHill)
    {
        parent = null!;
        targetHill = 0;
        if (_ephemeris is null || target.ParentId is null)
        {
            return false;
        }
        CelestialBody? p = _ephemeris.Bodies.FirstOrDefault(b => b.Id == target.ParentId);
        if (p is null || !_ephemeris.Bodies.Any(c => c.ParentId == p.Id && c.Kind == BodyKind.Moon))
        {
            return false; // parent must be a giant that owns moons
        }
        if (!ShipInsideHill(p))
        {
            return false; // must be free-flying in the well, not out in interplanetary space
        }
        targetHill = OrbitRule.HillRadius(target, p.Mu);
        double distance = (_ship.Position - _ephemeris.Position(target.Id, SimTime)).Length;
        if (distance <= OrbitRule.CaptureRangeHillRadii * targetHill)
        {
            return false; // already honestly near the moon — the terminal capture handles it directly
        }
        parent = p;
        return true;
    }

    // #146 split-advance executor: apply one scheduled transfer impulse from the ship's TRUE state at the
    // burn epoch (the tick loop advances exactly onto it), pricing it with the same OrbitRule.PulsesFor
    // the approach/insert burns spend and guarding the reserve floor. Can't afford it without breaching
    // the reserve → the #147 loud handback (reality diverged from the rehearsed plan, externally).
    private void ApplyTransferBurn(Vector2d deltaV)
    {
        if (_armedOrbitBodyId is null || _armedTransferSchedule is null)
        {
            return;
        }
        int cost = OrbitRule.PulsesFor(deltaV.Length, _ship.Velocity.Length);
        int reserveFloor = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
        if (_reactionMassPulses - cost < reserveFloor)
        {
            AutopilotStandDown($"autopilot handed back mid-transfer to {BodyName(_armedOrbitBodyId)} — fuel plan broken (reserve floor reached before a departure burn)");
            return;
        }
        _ship = _ship with { Velocity = _ship.Velocity + deltaV };
        _reactionMassPulses -= cost;
        _armedSpentPulses += cost;
        _armedTransferBurnsFired++;
        StaleFutureNodes();
        ShowPulseMessage($"Transfer burn — riding the well toward {BodyName(_armedOrbitBodyId)} ({cost} p) 🛰");
    }

    // The autopilot log — a Captain's-ledger receipt for every stand-down (newest first), projected
    // into the ledger's Tips section alongside the intel receipts (the established idiom).
    private readonly List<(double SimTime, string Text)> _autopilotEvents = [];
    private void LogAutopilotEvent(string text) => _autopilotEvents.Insert(0, (SimTime, text));

    // A loud stand-down: clear the arm, remember WHY (persistent, not a 1.5-s toast), file a ledger
    // receipt, and drop warp to 1× — an event worth interrupting for, the #139 deep-well philosophy.
    private void AutopilotStandDown(string reason)
    {
        _armedOrbitBodyId = null;
        ResetApproachTracking();
        ResetAutopilotBudget();
        _dockReadyStatus = null; // a loud handback replaces any prior "dock is ready" success line
        _autopilotStandDownReason = reason;
        LogAutopilotEvent(reason);
        Warp = 1;               // auto-drop: the drop must not slip past unseen at 10,000× warp
        _effectiveWarp = 1;
        ShowPulseMessage($"🛰 {reason}");
    }

    // #155 the last mile: the GRACEFUL station stand-down. When the armed target is a μ=0 station and the
    // rendezvous schedule has flown the ship into the dock envelope (matched and alongside), the autopilot
    // has SUCCEEDED — this is NOT the #147 loud handback. The tell in code: it never sets
    // _autopilotStandDownReason, so AutopilotStoodDown stays FALSE and no "you have the ship / here's why
    // it failed" surface lights up; instead it posts _dockReadyStatus, a success line. It clears the arm,
    // files a ledger receipt, and (like every stand-down) drops warp to 1× so the captain doesn't blow past
    // the berth at 10,000×. Docking stays the captain's ⚓ click — the autopilot never auto-clamps.
    private void AutopilotStandInEnvelope(CelestialBody station)
    {
        // #204: when the errand is honest (a friendly dock haven, nothing hostile-flagged), the ⚓ belongs
        // in the autopilot list — the ship completes the clamp itself, through the SAME path the manual
        // press and the #213 match use. The terminal match that brought it into the envelope already
        // fired above (the legacy Approach loop), so this is just the confirming clamp. Hostile-flagged
        // anything NEVER auto-docks (#186/#178): the captain's-word grammar stays, standing down into the
        // envelope for the manual ⚓ press.
        if (AutoDockHonest(station) && ResolveDockHaven(station.Id) is { } t)
        {
            LogAutopilotEvent($"autopilot delivered {station.Name} — matched in the dock envelope; auto-docking (honest errand)");
            Warp = 1; _effectiveWarp = 1;
            ClampOntoHaven(t.Body, t.Pos, $"🛰 auto-docked at {station.Name} —");
            return;
        }

        _armedOrbitBodyId = null;
        ResetApproachTracking();
        ResetAutopilotBudget();
        _autopilotStandDownReason = null; // SUCCESS — deliberately NOT a handback surface (#147 vs #155)
        _dockReadyStatus = $"🛰 in the dock envelope at {station.Name} — hit ⚓ Dock to clamp on";
        LogAutopilotEvent($"autopilot delivered {station.Name} — matched inside the dock envelope; hit ⚓ Dock to clamp on");
        Warp = 1;               // auto-drop so the arrival moment isn't missed at warp
        _effectiveWarp = 1;
        ShowPulseMessage(_dockReadyStatus);
    }

    // #204/#186: the autopilot completes the clamp itself only for an HONEST arrival — the armed
    // destination is a dock haven and nothing about the errand is hostile-flagged (no authorized plunder,
    // no plunder opportunity in play). A felony keeps the captain's-word grammar. Pure boundary lives in
    // Core (DockAffordanceRule.ShouldAutoDock) so it is unit-testable.
    private bool AutoDockHonest(CelestialBody station) =>
        DockAffordanceRule.ShouldAutoDock(
            IsDockableHaven(station),
            _plunderAuthorizedTargetId is not null || _plunderOpportunityTargetId is not null);

    private void ToggleArmedInsertionFromMenu(string bodyId)
    {
        ToggleArmedInsertion(bodyId);
        _bodyMenuBody = null;
        StateHasChanged();
    }

    private void ToggleArmedInsertion(string bodyId)
    {
        if (RejectNavWhileDocked())
        {
            return;
        }

        _dockReadyStatus = null; // a fresh arm/disarm supersedes any lingering "dock is ready" success line

        // Disarming (toggle off) the currently-armed body. #179: the autopilot is what keeps you on
        // orbit — losing it to a stray click stranded the owner, so confirm once. First click arms
        // the pending flag and asks; a second click within the window actually stands down.
        if (_armedOrbitBodyId == bodyId)
        {
            double nowMs = _lastTimestampMs ?? 0;
            if (_disarmConfirmBodyId != bodyId || nowMs > _disarmConfirmExpiresMs)
            {
                _disarmConfirmBodyId = bodyId;
                _disarmConfirmExpiresMs = nowMs + DisarmConfirmWindowMs;
                // Friday §0 / #179: disarming a KEPT orbit hands the ship back — the very act that
                // stranded the owner — so confirm it once, and say plainly what it costs.
                ShowPulseMessage(_orbitKept
                    ? $"Hand the ship back from the kept orbit at {BodyName(bodyId)}? The autopilot is holding it — click again to confirm."
                    : $"Disarm autopilot for {BodyName(bodyId)}? It keeps you on orbit — click again to confirm.");
                return;
            }

            _disarmConfirmBodyId = null;
            bool wasKept = _orbitKept;
            _armedOrbitBodyId = null;
            ResetApproachTracking();
            ResetAutopilotBudget();
            _autopilotStandDownReason = null; // the captain chose this — no "handed back" surface
            ShowPulseMessage(wasKept
                ? $"You have the ship — autopilot released the kept orbit at {BodyName(bodyId)}."
                : "Insertion disarmed");
            return;
        }

        _disarmConfirmBodyId = null; // arming a different body clears any pending disarm confirm

        // Arming: the promise (#146/#147). Rehearse the WHOLE journey — every approach burn plus the
        // insertion — before committing, so the ship is never armed into a trip it cannot finish. If
        // the rehearsed cost (plus a reserve floor) would outrun the tank, REFUSE with the numbers
        // rather than strand the captain mid-flight at warp.
        if (_ephemeris is not null && _simulator is not null)
        {
            int reserve = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
            int budget = Math.Max(0, _reactionMassPulses - reserve);
            string name = BodyName(bodyId);

            // #146 the moon run: when this is a free-flight-in-the-well hop to a giant's moon, quote the
            // cheap Lambert arc FIRST and rehearse WITH its schedule so the arm-time promise prices the
            // transfer, not the legacy hemorrhage. If the planner finds no window, fall back to the
            // legacy approach-loop rehearsal (never lose the old capability) and surface the planner's
            // reason as context on the status line — NOT as a refusal (the refuse/accept gate below on
            // r.Deliverable is unchanged: it still loudly refuses trips the rehearsal can't finish).
            TransferPlanner.Schedule? schedule = null;
            string? transferSummary = null;
            string? plannerNote = null;
            CelestialBody? target = _ephemeris.Bodies.FirstOrDefault(b => b.Id == bodyId);
            if (target is not null && ShouldPlanTransfer(target, out CelestialBody parentGiant, out _))
            {
                TransferPlanner.Result plan = TransferPlanner.Solve(
                    _simulator, _ephemeris, new TransferPlanner.Request(_ship, parentGiant.Id, bodyId, MaxWaitSeconds: 0));
                if (plan.Ok)
                {
                    schedule = plan.ToSchedule();
                    // #155: quote the winner's one-line summary; when the rendezvous priced a trade table
                    // (cheaper-vs-sooner), tack on "(+N other windows)" so the captain knows more lanes
                    // exist. The full table UI is #159/D2 territory — here we only hint the count.
                    transferSummary = plan.Alternatives.Count > 1
                        ? $"{plan.Summary} (+{plan.Alternatives.Count - 1} other windows)"
                        : plan.Summary;
                }
                else
                {
                    plannerNote = plan.Failure;
                }
            }

            AutopilotRehearsal.RehearsalResult r =
                AutopilotRehearsal.Rehearse(_ship, _ephemeris, _simulator, bodyId, budget, capturePath: true, schedule: schedule);
            if (!r.Deliverable)
            {
                string why = r.BudgetExceeded || r.Pulses > budget
                    ? $"needs ≈{r.Pulses} p (incl. insertion), tank has {_reactionMassPulses} and keeps {reserve} in reserve"
                    : "can't verify a capture from here — no clear window within range";
                _autopilotStandDownReason = $"autopilot declines {name}: {why}. It won't strand you.";
                ResetAutopilotBudget();
                ShowPulseMessage($"🛰 {_autopilotStandDownReason}");
                return; // NOT armed — the whole point of the promise
            }

            // #267 surface clearance: a rehearsal can be Deliverable (reaches a bound park within budget)
            // yet its point-mass PATH still thread a body it passes — the solve doesn't know a planet is in
            // the way. Verify the rehearsed line clears every body, judging the TARGET from its achieved
            // park (the #229 lesson) so a valid arrival AT the moon never false-refuses; the target's parent
            // and any brushed-by planet are judged over the whole path. Reuses the rehearsal's own samples —
            // no re-flight. A threaded planet is refused with the reason, in the captain's voice.
            if (SurfaceClearance.Check(r.Path, _ephemeris, bodyId) is { } clearance)
            {
                _autopilotStandDownReason = $"autopilot declines {name}: {SurfaceClearance.RefusalText(clearance)}.";
                ResetAutopilotBudget();
                ShowPulseMessage($"🛰 {_autopilotStandDownReason}");
                return; // NOT armed — the line threads a body
            }

            _armedBudgetPulses = r.Pulses;
            _armedSpentPulses = 0;
            CachePlanForAlarm(bodyId, r); // #148/#196/#219: draw the intended path AND cache its alarm pass, together
            _armedTransferSchedule = schedule;
            _armedTransferBurnsFired = 0;
            _armedTransferSummary = transferSummary
                ?? (plannerNote is not null ? $"no cheap transfer ({plannerNote}); flying the direct approach" : null);
            _autopilotStandDownReason = null;

            // Friday §0: the park will be KEPT, so quote the trim budget at arm time (honest pricing —
            // "trim ≈N p/day" on top of the transfer). Lab 25's per-body table, priced at the target's
            // world (heliocentric) speed ≈ the parked ship's. Only for a real orbit-able moon (μ>0); a
            // μ=0 station is never orbit-kept.
            _keepTrimPulsesPerDay = 0;
            if (target is { Mu: > 0, ParentId: not null }
                && _ephemeris.Bodies.FirstOrDefault(b => b.Id == target.ParentId) is { } keepParent)
            {
                _keepTrimPulsesPerDay = OrbitKeepingTable.TrimPulsesPerDay(
                    target, OrbitRule.HillRadius(target, keepParent.Mu), keepParent.Mu, target.OrbitRadius,
                    TransferMath.BodyVelocity(_ephemeris, target.Id, SimTime).Length);
            }
        }

        _armedOrbitBodyId = bodyId;
        ResetApproachTracking(); // every arm starts convergence tracking clean
        _destinationBodyId = bodyId; // arming says "this is where we're going"
        string trimQuote = _keepTrimPulsesPerDay > 0 ? $"; then holds the orbit, trim ≈{_keepTrimPulsesPerDay} p/day" : "";
        // #204: the arm-time quote names the final step. A μ=0 station ends at the ⚓ berth, not a park —
        // for an honest errand the autopilot auto-docks; a hostile-flagged run stands into the envelope
        // for the captain's ⚓ Dock.
        string arrival = BodyById(bodyId) is { } armedBody && IsDockableHaven(armedBody)
            ? (AutoDockHonest(armedBody)
                ? $"auto-dock at {BodyName(bodyId)}"
                : $"stand into the dock envelope at {BodyName(bodyId)} for your ⚓ Dock")
            : $"park at {BodyName(bodyId)}";
        ShowPulseMessage($"Insertion armed — budgeted ≈{_armedBudgetPulses} p; the ship will {arrival} when the window opens{trimQuote} 🛰");
    }

    // M25: the armed autopilot. Inside capture range it flies the "point at it and throttle"
    // approach the owner asked for — an approach burn, tidal trim burns as needed, and the
    // insertion once safely deep in the Hill sphere. Every burn is Δv-priced in pulses.
    private void CheckArmedInsertion()
    {
        if (_armedOrbitBodyId is null || _ephemeris is null || Paused)
        {
            return;
        }

        CelestialBody? body = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == _armedOrbitBodyId) { body = candidate; break; }
        }
        if (body?.ParentId is null) { _armedOrbitBodyId = null; return; }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; break; }
        }
        if (parent is null) { _armedOrbitBodyId = null; return; }

        Vector2d bodyPos = _ephemeris.Position(body.Id, SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
        double hill = OrbitRule.HillRadius(body, parent.Mu);

        // Friday §0: once parked, the autopilot HOLDS the orbit — station-keeping owns the tick, not
        // the approach/insert loop below. It stays here until the captain disarms or the tank runs dry.
        if (_orbitKept)
        {
            StationKeep(body, parent, bodyPos, bodyVel, hill);
            // #220: does keeping still earn the collision alarm's trust this tick? StationKeep flips
            // _orbitKept off the instant keeping ends (unbound, or the tank can't afford the next trim);
            // if it still holds, recompute FUNDED from the post-trim state — the next trim's pulse cost
            // against the tank. Read as `_orbitKept && _keepTrimFunded` in UpdateShipAlerts, so a healthy
            // held park's subsurface between-trim dip is trusted, and a dry-tank keep shouts immediately.
            _keepTrimFunded = _orbitKept &&
                OrbitKeeping.TrimPulseCost(_ship, bodyPos, bodyVel, body, OrbitRule.ParkingRadius(body, hill))
                    <= _reactionMassPulses;
            return;
        }

        // #155 the last mile: a μ=0 station is never orbited — its capture is the dock envelope (DockRule),
        // mirroring the rehearsal. The rendezvous schedule's two burns fly the ship into the berth; burn 2
        // (fired in the tick-advance split at the rendezvous epoch) is what matches the station's drift, so
        // stay FULLY muzzled until every scheduled burn has fired — letting AutopilotDecision fire an
        // Approach before then would double-pay for that match. Once the schedule is done and the ship is
        // matched & alongside, stand down GRACEFULLY (AutopilotStandInEnvelope — a SUCCESS, not a #147
        // handback) and let the captain clamp on with ⚓ Dock. Insert can never fire on a μ=0 body, so if
        // the ship isn't in the envelope yet the legacy Approach loop below can only close the gap, never
        // falsely "orbit-capture" the station.
        if (body.Kind == BodyKind.Station)
        {
            if (_armedTransferSchedule is { } stSched && _armedTransferBurnsFired < stSched.Burns.Count)
            {
                return; // still flying the rendezvous — the split fires the burns; coast, decisions muzzled
            }
            if (DockRule.InEnvelope(_ship, bodyPos, bodyVel, body.BodyRadius))
            {
                AutopilotStandInEnvelope(body);
                return;
            }
            // Schedule done (or none available) but not yet matched/alongside — fall through to the legacy
            // Approach loop to match velocity and close the last stretch; its reserve guard stays intact.
        }

        // #146 the moon run: while a transfer schedule is still in flight, keep AutopilotDecision MUZZLED.
        // Titan's 3e9 m capture floor makes the ship "inside capture range" for the ENTIRE Enceladus→Titan
        // cruise, so consulting the decision now would return Approach at ~7 km/s rel and restart the
        // velocity-reset bleed straight through the cheap arc. Stay muzzled until the ship is honestly
        // near the target — within max(60 s, 1% TOF) of arrival, OR inside the honest (floor-free)
        // Hill-scaled capture range. The scheduled burns fire in the tick-advance split, not here; once
        // the gate opens the terminal capture below takes over unchanged.
        if (_armedTransferSchedule is { } sched)
        {
            double tof = sched.Burns.Count > 0 ? sched.ArrivalTime - sched.Burns[0].SimTime : 0;
            double gateTime = sched.ArrivalTime - Math.Max(60.0, 0.01 * tof);
            double honestRange = OrbitRule.CaptureRangeHillRadii * hill;
            double distTarget = (_ship.Position - bodyPos).Length;
            if (SimTime < gateTime && distTarget >= honestRange)
            {
                return; // the arc is still in flight — coast, do not touch AutopilotDecision
            }
        }

        // Auto-orbiting a MOON (its parent is itself a planet, not the sun): the parent is a solid
        // body the approach chord must not thread. Auto-orbiting a PLANET (parent = sun): no chord
        // obstacle — you never route around the sun — just the target's own b-plane offset applies.
        OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
            ? null
            : new OrbitRule.ApproachObstacle(
                _ephemeris.Position(parent.Id, SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

        switch (OrbitRule.AutopilotDecision(_ship, bodyPos, bodyVel, body, hill))
        {
            case OrbitRule.AutopilotAction.Approach:
                double distance = (_ship.Position - bodyPos).Length;
                // Convergence watchdog: a burn that beats our closest-ever pass is progress; a run of
                // burns that don't means the approach is stuck. Stand down and keep the fuel rather
                // than firing forever with no feedback (issue #136, the owner's live complaint).
                if (distance < _approachMinDistance * (1 - 1e-3))
                {
                    _approachMinDistance = distance;
                    _approachStalledBurns = 0;
                }
                else
                {
                    _approachStalledBurns++;
                }
                if (_approachStalledBurns >= AutopilotMaxStalledBurns)
                {
                    // Should be near-impossible now the arm-time rehearsal proves convergence, but if
                    // geometry drifts it still stands down LOUDLY (#147), not with a 1.5-s toast.
                    AutopilotStandDown($"autopilot handed back near {body.Name} — approach not converging after {_approachBurnCount} burns; fuel preserved");
                    return;
                }

                // Hill-aware approach (issue #136): the aim's safe periapsis and closing speed scale
                // to the body's well, so a deep moon like Enceladus actually reaches its capture band.
                int approachCost = OrbitRule.ApproachPulseCost(_ship, bodyPos, bodyVel, body, obstacle, hill);
                // The reserve floor (#146): the autopilot never burns the tank below the reserve it
                // promised to keep. If an approach burn would breach it, reality has diverged from the
                // rehearsed plan — only possible via something external (a manual burn, damage, a
                // dock) or a harder approach than rehearsed — so hand back LOUDLY instead of bleeding
                // the tank dry. The owner's ruling: never a silent drop.
                int reserveFloor = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
                if (_reactionMassPulses - approachCost < reserveFloor)
                {
                    AutopilotStandDown($"autopilot handed back near {body.Name} — fuel plan broken (reserve floor reached; a manual burn, damage, or a harder approach than budgeted)");
                    return;
                }

                _ship = OrbitRule.Approach(_ship, bodyPos, bodyVel, body, obstacle, hill);
                _reactionMassPulses -= approachCost;
                _armedSpentPulses += approachCost;
                _approachBurnCount++;
                StaleFutureNodes();
                ShowPulseMessage($"Approach burn — falling toward {body.Name} ({approachCost} p) 🛰");
                return;

            case OrbitRule.AutopilotAction.Insert:
                int cost = OrbitRule.PulseCost(_ship, bodyPos, bodyVel, body);
                // The insertion is the arrival — it may dip into the reserve to complete the park.
                // Only an outright can't-afford-it (post-rehearsal, only via external divergence)
                // stands it down.
                if (cost > _reactionMassPulses)
                {
                    AutopilotStandDown($"autopilot handed back at {body.Name} — insertion needs {cost} p, only {_reactionMassPulses} left (fuel plan broken externally)");
                    return;
                }

                _ship = OrbitRule.Insert(_ship, bodyPos, bodyVel, body);
                _reactionMassPulses -= cost;
                _armedSpentPulses += cost;
                // Friday §0 (owner ruling): "armed auto-orbit ends in a KEPT orbit, not an achieved
                // one." The park is NOT a handback — the autopilot stays in command and now STATION-KEEPS
                // the orbit (holds it with trims, priced from Lab 25). _armedOrbitBodyId stays set to the
                // kept body; _orbitKept flips keeping on. The transfer/approach machinery is done, so its
                // budget/schedule/plan-path clear, but keeping owns the ship until the captain disarms
                // (double-confirm) or the tank runs dry (a loud handback). Never "you have the ship" while
                // circling a moon by luck (#176/#184).
                _autopilotStandDownReason = null;
                _dockReadyStatus = null;
                _armedBudgetPulses = 0;
                _armedSpentPulses = 0;
                _autopilotPlanPath = null;
                _autopilotPlanClosestPass = null; // #196: park reached — the plan is consumed; ballistic alarm resumes
                _armedTransferSchedule = null;
                _armedTransferSummary = null;
                _armedTransferBurnsFired = 0;
                ResetApproachTracking();
                double park = OrbitRule.ParkingRadius(body, hill);
                _orbitKept = true;
                _keepTrimPulsesPerDay = OrbitKeepingTable.TrimPulsesPerDay(
                    body, hill, parent.Mu, body.OrbitRadius, _ship.Velocity.Length);
                _keepNextCheckTime = SimTime + OrbitKeeping.TrimCadenceFraction * OrbitRule.LocalOrbitPeriod(park, body.Mu);
                _keepTrimsFired = 0;
                ArrivedAt(body.Id);
                StaleFutureNodes();
                double parkedRadius = (_ship.Position - bodyPos).Length;
                string holds = $"🛰 AUTOPILOT HOLDS THE ORBIT — {body.Name}, {FormatDistance(parkedRadius)}, trim ≈{_keepTrimPulsesPerDay} p/day";
                LogAutopilotEvent($"autopilot parked at {body.Name} — now HOLDING the orbit at {FormatDistance(parkedRadius)}; trim ≈{_keepTrimPulsesPerDay} p/day until you disarm or the tank runs dry");
                Warp = 1;               // auto-drop so the arrival moment isn't blown past at warp
                _effectiveWarp = 1;
                CompleteBoundCargoRunQuests(); // a parcel bound for this moon haven delivers on the park (#175)
                ShowPulseMessage(holds);
                RendererInterop.PlayCue("board");
                return;
        }
    }

    // Friday §0: STATION-KEEPING — the autopilot holds the park with trim burns (OrbitKeeping, budgets
    // from Lab 25). Runs every tick while _orbitKept, but only CONSIDERS a trim once every quarter park
    // period (OrbitKeeping.TrimCadenceFraction) — riding the tide's reversible forced eccentricity
    // instead of fighting it every tick (the lab's treadmill). Two ways out, both loud: the captain
    // disarms (ToggleArmedInsertion's #179 double-confirm), or the tank can't afford the next trim —
    // a LOUD handback, after which the #180 degradation alert becomes the backstop as the orbit decays.
    private void StationKeep(CelestialBody body, CelestialBody parent, Vector2d bodyPos, Vector2d bodyVel, double hill)
    {
        // The orbit gone (an external burn flung it out, or it decayed unbound): keeping is over. Stand
        // down loudly; the #180 alert covers the decay from here.
        if (!OrbitRule.IsBound(_ship, bodyPos, bodyVel, body, hill))
        {
            _orbitKept = false;
            AutopilotStandDown($"autopilot lost the orbit at {body.Name} — no longer bound; you have the ship");
            return;
        }

        double park = OrbitRule.ParkingRadius(body, hill);
        if (SimTime < _keepNextCheckTime)
        {
            return; // between cadence points — let the reversible oscillation reverse itself
        }
        _keepNextCheckTime = SimTime + OrbitKeeping.TrimCadenceFraction * OrbitRule.LocalOrbitPeriod(park, body.Mu);

        if (!OrbitKeeping.NeedsTrim(_ship, bodyPos, bodyVel, body))
        {
            return; // still tight inside the tolerance — nothing to spend
        }

        int cost = OrbitKeeping.TrimPulseCost(_ship, bodyPos, bodyVel, body, park);
        // (b) the tank running dry — the LOUD handback (Friday §0). Reuse the escalating degradation
        // surface as the BACKSTOP, not the defense: keeping can no longer hold, so hand back loudly and
        // let the #180 alert shout amber/red as the orbit strips.
        if (cost > _reactionMassPulses)
        {
            _orbitKept = false;
            AutopilotStandDown(
                $"⚠ TANK DRY at {body.Name} — the autopilot can no longer hold the orbit ({cost} p needed, {_reactionMassPulses} left). It will now decay — you have the ship.");
            return;
        }

        _ship = OrbitKeeping.Trim(_ship, bodyPos, bodyVel, body, park);
        _reactionMassPulses -= cost;
        _armedSpentPulses += cost;
        _keepTrimsFired++;
        StaleFutureNodes();
        ShowPulseMessage($"🛰 orbit trim at {body.Name} ({cost} p) — holding the park");
    }

    // ---- M20: the bus stop in space ----
    private readonly record struct OrbitAssistInfo(
        CelestialBody Body, double Distance, double RelSpeed, double Hill, int Cost,
        bool WindowOpen, bool TooFast, bool CanEngage, bool IsDestination,
        double CaptureRange, bool InCaptureRange, bool Armed, int ApproachCost,
        bool Bound, bool RadiusInStableBand);

    private OrbitAssistInfo? OrbitInfo()
    {
        // The chosen destination owns the panel, then an armed target: "Orbit Earth?" while
        // sailing for Mars was exactly the confusion the owner reported. Nearest is the
        // fallback for players who haven't picked anywhere yet.
        string? focusId = _destinationBodyId ?? _armedOrbitBodyId;
        CelestialBody? preferred = null;
        if (focusId is not null && _ephemeris is not null)
        {
            foreach (CelestialBody candidate in _ephemeris.Bodies)
            {
                if (candidate.Id == focusId) { preferred = candidate; break; }
            }
        }

        if (preferred is not null && preferred.ParentId is not null)
        {
            Vector2d pos = _ephemeris!.Position(preferred.Id, SimTime);
            double h = 1.0;
            Vector2d vel = (_ephemeris.Position(preferred.Id, SimTime + h) - _ephemeris.Position(preferred.Id, SimTime - h)) / (2 * h);
            return BuildOrbitInfo(preferred, pos, vel);
        }

        if (_nearestBody is not CelestialBody body || body.ParentId is null || _ephemeris is null)
        {
            return null; // the sun is not a bus stop; you already orbit it
        }

        return BuildOrbitInfo(body, _nearestBodyPosition, _nearestBodyVelocity);
    }

    private OrbitAssistInfo? BuildOrbitInfo(CelestialBody body, Vector2d bodyPos, Vector2d bodyVel)
    {
        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris!.Bodies)
        {
            if (candidate.Id == body.ParentId) { parent = candidate; break; }
        }
        if (parent is null) return null;

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        double distance = (_ship.Position - bodyPos).Length;
        bool destination = _destinationBodyId == body.Id;
        bool focused = destination || _armedOrbitBodyId == body.Id;
        if (!focused && distance > Math.Max(OrbitRule.IndicatorRangeHillRadii * hill, 2e9))
        {
            return null;
        }

        double relSpeed = (_ship.Velocity - bodyVel).Length;
        bool open = OrbitRule.WindowOpen(_ship, bodyPos, bodyVel, body, hill);
        int cost = OrbitRule.PulseCost(_ship, bodyPos, bodyVel, body);
        bool bound = OrbitRule.IsBound(_ship, bodyPos, bodyVel, body, hill);
        double captureRange = OrbitRule.CaptureRange(hill);
        // #180: is the ship's CURRENT radius inside the tide-stable park band? The manual press
        // circularizes here, so this decides whether Enter-orbit parks now or hands to the autopilot.
        bool radiusInBand = OrbitRule.RadiusInStableBand(distance, body, hill);
        return new OrbitAssistInfo(body, distance, relSpeed, hill, cost,
            open && !bound, relSpeed >= OrbitRule.MaxRelativeSpeed,
            open && !bound && cost <= _reactionMassPulses, destination,
            captureRange, distance <= captureRange && !bound,
            _armedOrbitBodyId == body.Id,
            OrbitRule.ApproachPulseCost(_ship, bodyPos, bodyVel),
            bound, radiusInBand);
    }

    // One line of approach coaching: what to fix first, with a ballpark number on it.
    private string OrbitStatusLine(OrbitAssistInfo oi)
    {
        string inv(double v) => v.ToString("F1", CultureInfo.InvariantCulture);
        // #176: once bound the panel used to fall through to the "inside capture range" line and the
        // button greyed out mute — say plainly that we're already parked so nobody reads it as "off".
        if (oi.Bound) return $"bound — parked at {FormatDistance(oi.Distance)}";
        // #180: inside the window but above the tide-stable band — say the press won't park HERE.
        if (oi.WindowOpen)
            return oi.RadiusInStableBand
                ? "window OPEN"
                : "window open — this radius is tide-chaotic (Lab 16); autopilot parks you deeper";
        if (oi.Armed && oi.InCaptureRange)
            // #203: altitude above the surface, unit-labelled — the SAME number the banner's
            // "orbit-insert (alt N km)" row shows, never the raw orbital radius the panel used to quote.
            return $"autopilot flying the approach — insertion at ≈{FormatAltitude(OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius)}";
        if (oi.InCaptureRange) return "in capture range — auto-orbit can park you";
        // #153: once inside the capture range (e.g. already bound/orbiting) the gap goes NEGATIVE —
        // the old line printed "close in -2,982,642 km to capture range". Read it honestly instead:
        // report the distance to the body, not a nonsensical negative closing distance.
        string closing = oi.Distance < oi.CaptureRange
            ? $"inside capture range — {FormatDistance(oi.Distance)} from {oi.Body.Name}"
            : $"close in {FormatDistance(oi.Distance - oi.CaptureRange)} to capture range";
        return oi.TooFast
            ? $"{closing} (autopilot sheds the {inv((oi.RelSpeed - OrbitRule.MaxRelativeSpeed) / 1000)} km/s there)"
            : closing;
    }

    private void EnterOrbit()
    {
        if (RejectNavWhileDocked())
        {
            return;
        }

        // No silent no-ops (issue #136): if there is nothing to orbit, say so.
        if (_ephemeris is null || OrbitInfo() is not { } candidate)
        {
            ShowPulseMessage("No moon or planet in range to orbit — pick a destination (🎯) or coast closer.");
            return;
        }

        // Outside the open window the button (and O) toggles the autopilot instead: arm it
        // anywhere inside capture range and the ship flies the approach itself (M25).
        if (!candidate.CanEngage)
        {
            if (candidate.Armed)
            {
                if (candidate.InCaptureRange)
                {
                    // Arm-once (issue #136): the autopilot is already flying this approach. Report
                    // its status instead of re-firing a burn or dropping the arm mid-flight — it
                    // disarms itself on arrival, or the watchdog stands it down if it can't close.
                    ShowPulseMessage($"🛰 {candidate.Body.Name}: {OrbitStatusLine(candidate)}");
                }
                else
                {
                    ToggleArmedInsertion(candidate.Body.Id); // armed but not yet closing — a press stands it down
                }
                return;
            }
            if (candidate.InCaptureRange)
            {
                ToggleArmedInsertion(candidate.Body.Id); // in range — arm the approach
                return;
            }
            // In view but out of auto-orbit reach: say why the press did nothing, with the gap.
            ShowPulseMessage($"{candidate.Body.Name} is out of auto-orbit range — coast within {FormatDistance(candidate.CaptureRange)} (still {FormatDistance(candidate.Distance - candidate.CaptureRange)} to go).");
            return;
        }

        OrbitAssistInfo oi = candidate;

        // #180 moon-grade orbit: the window is open, but never SILENTLY circularize at a radius the
        // sun's tide will strip (the owner's ≈0.53-Hill Enceladus park, Lab 16). When the current
        // radius is outside the tide-stable band, hand the descent to the armed autopilot — the same
        // machinery that parks at the stable radius — instead of parking unstably here.
        if (!oi.RadiusInStableBand)
        {
            if (oi.Armed)
            {
                ShowPulseMessage($"🛰 {oi.Body.Name}: {OrbitStatusLine(oi)}");
            }
            else
            {
                ToggleArmedInsertion(oi.Body.Id); // descends to the tide-stable park (≈0.33 Hill)
            }
            return;
        }

        // Insert relative to the panel's own body — it can be an armed/destination target,
        // not necessarily the nearest one whose position the tick loop caches.
        Vector2d bodyPos = _ephemeris.Position(oi.Body.Id, SimTime);
        double h = 1.0;
        Vector2d bodyVel = (_ephemeris.Position(oi.Body.Id, SimTime + h) - _ephemeris.Position(oi.Body.Id, SimTime - h)) / (2 * h);
        _ship = OrbitRule.Insert(_ship, bodyPos, bodyVel, oi.Body);
        _reactionMassPulses -= oi.Cost;
        ArrivedAt(oi.Body.Id);
        StaleFutureNodes();
        CompleteBoundCargoRunQuests(); // a parcel bound for this moon haven delivers on the park (#175)
        ShowPulseMessage($"Orbital insertion — bound to {oi.Body.Name} 🛰");
        RendererInterop.PlayCue("board");
    }

    private float[] _autopilotPlanScratch = [];

    // Recomputed every frame from ground truth (OrbitRule.IsBound), same math UpdateEffectiveWarp
    // already relies on — not the orbit-assist UI's armed/nearest framing, which can point at a
    // different body than the one the ship is actually bound to.
    private void UpdateOrbitedBody()
    {
        _orbitedBodyId = null;
        CelestialBody? boundBody = null;
        Vector2d boundBodyPos = default, boundBodyVel = default;
        double boundHill = 0;
        if (_ephemeris is not null)
        {
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.ParentId is null)
                {
                    continue; // the sun: everyone "orbits" it, not a bus stop
                }

                CelestialBody? parent = null;
                foreach (CelestialBody candidate in _ephemeris.Bodies)
                {
                    if (candidate.Id == body.ParentId) { parent = candidate; break; }
                }
                if (parent is null)
                {
                    continue;
                }

                Vector2d bodyPos = _ephemeris.Position(body.Id, SimTime);
                const double h = 1.0;
                Vector2d bodyVel = (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
                double hill = OrbitRule.HillRadius(body, parent.Mu);
                if (OrbitRule.IsBound(_ship, bodyPos, bodyVel, body, hill))
                {
                    _orbitedBodyId = body.Id;
                    _orbitedBodyPosition = bodyPos;
                    _orbitedBodyHillRadius = hill;
                    boundBody = body;
                    boundBodyPos = bodyPos;
                    boundBodyVel = bodyVel;
                    boundHill = hill;
                    break;
                }
            }
        }

        // #180: watch the bound orbit's tide-stability every tick (cheap — one bound body at most)
        // and alert on the edge into decay. Ground truth, independent of the orbit-assist UI.
        UpdateParkStability(boundBody, boundBodyPos, boundBodyVel, boundHill);

        // PR-11 deviation: this used to auto-pop the (small, floating) Local Space panel on the
        // rising edge of a bind (vision par. 10). Now that Trade is a full-screen desk, yanking
        // the player's whole view there mid-flight would be jarring rather than helpful — the
        // Trade chip (TradeChip()) already updates live so the player notices the new contact,
        // and switching desks stays a deliberate action (number key / tab / chip click).
        // (The haven news + lesson advance moved to the "hidden at a haven" rising edge in
        // UpdateEncounters, so a mass-less dock — which never orbit-binds — triggers them too.)
    }

    // #180: edge-triggered orbit-degradation alert. Evaluate the bound body's ParkStability each
    // tick and fire ONLY on a transition into a risk verdict (TideRisk / Subsurface), or clear when
    // stability returns. Losing an orbit must never be discovered by looking — the owner's Enceladus
    // strand. TODO(#166): route this through the ShipAlerts channel (+🦜) when it lands.
    private void UpdateParkStability(CelestialBody? body, Vector2d bodyPos, Vector2d bodyVel, double hill)
    {
        OrbitRule.ParkStabilityVerdict verdict = body is null
            ? OrbitRule.ParkStabilityVerdict.NotBound
            : OrbitRule.ParkStability(_ship, bodyPos, bodyVel, body, hill);

        // Friday §0: while the autopilot is KEEPING the orbit, the #180 degradation alert is the
        // BACKSTOP, not the defense — it must stay silent for the forced-eccentricity brush that
        // keeping trims away every quarter period (at a deep well like Enceladus the apoapsis routinely
        // touches the band ceiling, TideRisk, between trims). So downgrade TideRisk to Stable while
        // kept. A true Subsurface (impact imminent) still shouts red even while keeping — the one
        // failure keeping should never mask.
        if (_orbitKept && verdict == OrbitRule.ParkStabilityVerdict.TideRisk)
        {
            verdict = OrbitRule.ParkStabilityVerdict.Stable;
        }

        bool IsRisk(OrbitRule.ParkStabilityVerdict v) =>
            v is OrbitRule.ParkStabilityVerdict.TideRisk or OrbitRule.ParkStabilityVerdict.Subsurface;

        // A change of bound body resets the watch — a fresh park is a fresh baseline. Warn straight
        // away if we arrive already in a risk state; otherwise clear any stale warning.
        if (body?.Id != _parkStabilityBodyId)
        {
            _parkStabilityBodyId = body?.Id;
            _lastParkStability = verdict;
            if (body is not null && IsRisk(verdict))
            {
                RaiseOrbitDegrade(body, bodyPos, bodyVel, verdict);
            }
            else
            {
                ClearOrbitDegrade();
            }
            return;
        }

        if (verdict == _lastParkStability)
        {
            return; // no transition — nothing to do (edge-triggered, not continuous)
        }

        if (IsRisk(verdict))
        {
            // Into a risk state, or an escalation/de-escalation between the two risk verdicts.
            if (body is not null)
            {
                RaiseOrbitDegrade(body, bodyPos, bodyVel, verdict);
            }
        }
        else if (IsRisk(_lastParkStability))
        {
            ClearOrbitDegrade(); // stability returned (or the ship left the well)
        }

        _lastParkStability = verdict;
    }

    private void RaiseOrbitDegrade(CelestialBody body, Vector2d bodyPos, Vector2d bodyVel, OrbitRule.ParkStabilityVerdict verdict)
    {
        bool subsurface = verdict == OrbitRule.ParkStabilityVerdict.Subsurface;
        string reason = subsurface
            ? "periapsis under the surface — impact coming"
            : "drifting past the tide-stable band (Lab 16) — it strips over hours";
        // Ballpark corrective-burn cost from the current state — a hint for the "re-park or leave"
        // choice; the orbit/autopilot button recomputes the exact bill when pressed.
        int reparkCost = OrbitRule.PulseCost(_ship, bodyPos, bodyVel, body);

        _orbitDegradeSeverity = subsurface ? 2 : 1;
        _orbitDegradeWarning = $"⚠ orbit degrading at {body.Name} — {reason}; re-park (≈{reparkCost} p) or leave";
        Warp = 1; // auto-drop so the decay isn't blown past at warp
        LogAutopilotEvent(_orbitDegradeWarning);
        ShowPulseMessage(_orbitDegradeWarning);

        // #166: the third founding alert now speaks through the shared channel too. Raise fires only on
        // the rising edge / an escalation to red — so the parrot squawks once per crossing, not per tick.
        if (_shipAlerts.Raise(AlertKind.OrbitDegrade, subsurface ? AlertSeverity.Red : AlertSeverity.Amber,
                _orbitDegradeWarning, SimTime))
        {
            SquawkNow(Parrot.Squawk.OrbitDecay, _lastTimestampMs ?? 0, force: true);
        }
    }

    private void ClearOrbitDegrade()
    {
        _shipAlerts.Clear(AlertKind.OrbitDegrade);

        if (_orbitDegradeWarning is null)
        {
            return;
        }

        LogAutopilotEvent("orbit stable again — tide-risk cleared");
        _orbitDegradeWarning = null;
        _orbitDegradeSeverity = 0;
    }
}
