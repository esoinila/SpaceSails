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
// burns, and the station-keeping that holds a KEPT orbit. Lifted whole from Map.razor for #251.
//
// #251 · WHY THE FILE WAS CUT, and what "pure motion" is holding across the family. At 1,442 lines this
// was the longest hand-written file in `src/`, and it was the file that set the size gate's daylight
// (`NoSourceFileIsTooLongTests`, the line at 1,500): the next section anybody added here would have had to
// be shoved somewhere else first. So it is cut along the seams it already had — one subject per partial,
// every line moved VERBATIM, no rename, no signature change, no statement reordered inside a method. A
// `partial` is the same class, so the page's field roster is untouched by construction.
//
// THE FAMILY (1,442 lines → six files, largest 960):
//   · Map.Autopilot.cs               — the arm, the promise, and the loop that flies it: the rehearsal and
//                                      its refusal, arm and stand-down, the transfer burns, the approach
//                                      and the insertion, and the keeping that holds a KEPT orbit.
//   · Map.Autopilot.ParkWatch.cs     — which body the ship is bound to, and the #180 degradation alert.
//   · Map.Autopilot.OrbitAssist.cs   — the M20 panel's readout and its one line of approach coaching.
//   · Map.Autopilot.ArrivalWindow.cs — #957's refusal arithmetic and #969's promise coming round.
//   · Map.Autopilot.Ancients.cs      — the M28 pyramid grants and the course one charge buys.
//   · Map.Autopilot.FlightPlan.cs    — PR-D1's read-only derivations for the banner and the Nav header.
//
// WHAT STAYED HERE, and why it is not "the leftovers": five source guards read THIS PATH and assert
// literals in it — `TheArrivalEndsWhereTheErrandIsTests` (the `BodyKind.Station` fork of
// `CheckArmedInsertion`, sliced structurally down to the `#146 the moon run` comment that follows it),
// `TheTenthIsQuotedAndOnlyTheAutopilotsTests` (the refusal's numbers, and the five `_reactionMassPulses -=`
// debits IN ORDER — `charge`, `approachCharge`, `insertCharge`, `cost`, `oi.Cost`, which pins
// `ApplyTransferBurn`, `CheckArmedInsertion`, `StationKeep` and `EnterOrbit` to this file in that order),
// `TheWallsAreHungAndReadTests` (both `TheArrivalIsRemembered` arrival edges, counted), and
// `TheWreckHasItsOwnArrivalTests` (`AutopilotStandInEnvelope`, and the FABLE marker that must not be in
// it). Every seam below was chosen to leave those literals where their guard reads them, so not one guard
// was edited for the cut — the alternative is a guard whose world can no longer tell pass from fail.
public partial class Map
{

    // ---- M22: planned insertion — "it is part of flight planning" (owner) ----
    private string? _armedOrbitBodyId;

    // ---- #969 — ARMED *THEN*, NOT ONLY *NOW*. Owner ruling 2026-08-23: "I want dock / orbit option as
    // normal part, a step in the plan. Say three burns and one autopilot to finish the trip to Mars. After
    // that no, absolutely no steps needed if the ship is not interfered with."
    //
    // This ONE nullable is the whole difference between the two arms, and it is deliberately the smallest
    // thing that can tell them apart (no forked autopilot): the sim-time of the PASS the arm's promise was
    // rehearsed for. Null = the historic NOW arm — the ship is at the door and the loop below has the
    // controls from this instant. Set = a plan-time promise about a moment that has not come round yet, so
    // CheckArmedInsertion keeps its hands off entirely (ArrivalStepRule.ArrivalPromiseIsStillAhead) while
    // the captain's plotted burns fly her, and clears itself the instant the pass — or the body — arrives.
    // From then on it IS an ordinary armed arrival and the unchanged insertion/dock path finishes the trip.
    private double? _armedArrivalPassSimTime;

    /// <summary>#969: the arm on the board is a plan-time promise whose pass has not come round yet — the
    /// autopilot is armed, and correctly doing nothing.</summary>
    private bool ArmedArrivalStillAhead => _armedArrivalPassSimTime is not null;

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
    private int _armedBudgetPulses;              // rehearsal-quoted CHARGED cost for the current arm (#928: the tenth)
    // #928 THE TENTH'S ACCUMULATOR, and it is this field: the RAW (un-economized) approach+insert pulses
    // burnt since this arm. Every autopilot burn asks AutopilotRehearsal.ChargeForBurn(_armedSpentPulses,
    // rawCost) what the TANK loses and adds the RAW cost here, so the flown total charged is exactly
    // ⌈raw/10⌉ — the same formula the arm-time estimate quotes. Ten one-pulse burns cost one pulse, not
    // ten and not zero. No new state was needed for the accumulator (#905's frame ledger stays pinned):
    // the running raw total IS the accumulator, and every arm resets it. (Station-keeping trims also add
    // here for the diagnostic count, but they are charged in FULL — the tenth is the price of an
    // approach, not of holding a park — and no approach burn can follow a kept orbit without a re-arm.)
    private int _armedSpentPulses;
    private string? _autopilotStandDownReason;   // persistent "you have the ship" line (decline/handback)
    private string? _dockReadyStatus;            // #155: persistent "in the envelope — hit ⚓ Dock" line, a station SUCCESS (never a #147 handback)
    private IReadOnlyList<TrajectorySample>? _autopilotPlanPath; // #148: the rehearsed INTENDED path
    // #196: the tightest pass of the rehearsed plan path, cached at arm time (the plan is fixed, so it
    // costs one MostSevere pass, not one per tick). The collision alarm judges THIS while armed — the
    // insert burn resolves the ballistic impact, so that impact is the plan working, not news. A plan
    // whose OWN path goes subsurface leaves this an Impact pass, and the alarm shouts red immediately.
    private ClosestApproach.Pass? _autopilotPlanClosestPass;
    // #962: the same rehearsed plan, cached per BODY — how close the plan's own path came to each world
    // it passed. The collision alarm above asks "does the plan hit anything"; the #180 park-degradation
    // watchdog asks the other question of the same numbers — "did the plan clear the body this ship is
    // BOUND to" — because an osculating conic is not the course of a ship the autopilot is still flying.
    // Cached with the path and the pass (the #219 one-arm law): all three live and die together.
    private IReadOnlyDictionary<string, double>? _autopilotPlanBodyClearance;

    // ---- #179: disarming the autopilot is what dropped the owner off orbit; confirm it once. The
    // first disarm click arms this pending flag (with a short expiry) and asks; a second click within
    // the window actually stands down. No browser confirm() — those hang automation. ----
    private string? _disarmConfirmBodyId;        // body whose disarm is pending a confirming second click
    private double _disarmConfirmExpiresMs;      // rAF-clock deadline for that second click
    private const double DisarmConfirmWindowMs = 4000;

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
        _armedArrivalPassSimTime = null; // #969: the plan-time promise dies with the arm that made it
        _autopilotPlanPath = null;
        _autopilotPlanClosestPass = null; // #196: plan gone — the alarm returns to the ballistic course
        _autopilotPlanBodyClearance = null; // #962: …and the park watchdog returns to the raw conic
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
        if (_ephemeris is null)
        {
            _autopilotPlanClosestPass = null;
            _autopilotPlanBodyClearance = null;
            return;
        }

        // One judged pass list, read two ways (#962): the worst of them is the collision alarm's plan
        // pass, and the per-body distances are what the park-degradation watchdog checks the bound body
        // against. Same scan, same arrival treatment, one arm-time cost.
        IReadOnlyList<ClosestApproach.Pass> passes = AutopilotRehearsal.PlanPasses(r, _ephemeris, bodyId);
        ClosestApproach.Pass? worst = null;
        var clearance = new Dictionary<string, double>(passes.Count, StringComparer.Ordinal);
        foreach (ClosestApproach.Pass pass in passes)
        {
            clearance[pass.BodyId] = pass.Distance;
            if (worst is null || pass.Severity < worst.Value.Severity)
            {
                worst = pass;
            }
        }

        _autopilotPlanClosestPass = worst;
        _autopilotPlanBodyClearance = clearance;
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
        // #928: the autopilot flies at a tenth. The tank is charged ChargeForBurn against the raw ledger,
        // so the whole armed journey costs exactly the ⌈raw/10⌉ the rehearsal quoted at arm time.
        int charge = AutopilotRehearsal.ChargeForBurn(_armedSpentPulses, cost);
        int reserveFloor = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
        if (_reactionMassPulses - charge < reserveFloor)
        {
            AutopilotStandDown($"autopilot handed back mid-transfer to {BodyName(_armedOrbitBodyId)} — fuel plan broken (reserve floor reached before a departure burn)");
            return;
        }
        _ship = _ship with { Velocity = _ship.Velocity + deltaV };
        _reactionMassPulses -= charge;
        _armedSpentPulses += cost;
        _armedTransferBurnsFired++;
        // #167 BURN KIND 3/9 - THE SCHEDULED TRANSFER BURN, and the reason this lane exists at all: this one
        // fires ITSELF at its epoch, usually at four figures of warp, with no hand anywhere near the ship.
        // The flame is on the wall clock precisely so that this burn - whose whole sim-time existence is one
        // frame - is still on the glass for a second afterwards.
        BurnFired(charge, deltaV);
        StaleFutureNodes();
        ShowPulseMessage($"Transfer burn — riding the well toward {BodyName(_armedOrbitBodyId)} ({charge} p) 🛰");
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
        // #938 D3a / a live #212 breach: the branch that gets here asks BodyKind.Station, because μ=0 is
        // what makes a body unorbitable and the envelope its only arrival — that part is physics and stays.
        // What must NOT ride on BodyKind is the PROMISE. `hit ⚓ Dock to clamp on` names a button that
        // UpdateDockAffordance only ever offers for a DockableHavens.IsDockable body, and sol.json's
        // Derelict Roadster, Mercury Compute Farms and Highport Satellite Works are μ=0 stations with no
        // haven flag: the autopilot flew you to a wreck and told you to clamp onto it. The clamp clause is
        // now spoken only where a clamp exists; alongside a wreck the line stops at the truth.
        string clamp = IsDockableHaven(station) ? " — hit ⚓ Dock to clamp on" : "";
        _dockReadyStatus = $"🛰 in the dock envelope at {station.Name}{clamp}";
        LogAutopilotEvent($"autopilot delivered {station.Name} — matched inside the dock envelope{(clamp.Length > 0 ? "; hit ⚓ Dock to clamp on" : "")}");
        // #244 item 1 (canon pass, Fable, 2026-09-05) · …AND THE WRECK'S OWN SENTENCE. #1104 stopped the
        // autopilot promising a clamp here and left the arrival with nothing of its own to say, which is
        // what the owner walked into: "I think we dropped out of autopilot… did we miss the dock button
        // press while warping?" The line goes on the autopilot's OWN channel, beside the delivery it
        // belongs to, and it is said ONCE per arrival — a berth that repeats itself every tick is noise,
        // and the whole complaint was that the moment went unnoticed rather than unheard.
        if (Derelict.IsWreckBody(station.Id) && _pickupHailSaidAt != station.Id)
        {
            _pickupHailSaidAt = station.Id;
            LogAutopilotEvent(HarborVocabulary.PickupArrival);
        }
        Warp = 1;               // auto-drop so the arrival moment isn't missed at warp
        _effectiveWarp = 1;
        ShowPulseMessage(_dockReadyStatus);
    }

    /// <summary>#244 item 1 · The wreck this arrival has already been announced at, so the sentence is said
    /// once and not once per tick. Cleared when an insertion is armed (a fresh approach is a fresh arrival),
    /// which is what makes coming back to the same hull say it again.</summary>
    private string? _pickupHailSaidAt;

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
        _pickupHailSaidAt = null; // #244 item 1: a fresh approach is a fresh arrival, and it may say so again

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
                // #957 — DON'T COMPLAIN, BRAKE. Owner, having flown right up to The Rusty Roadstead and been
                // refused: "It should just add the necessary braking step on the plot path and not complain.
                // … nobody will ever play the 'let's fly next to it really quiet so autopilot will agree'."
                // So before the refusal stands, ask whether ONE braking burn — priced at the same tenth,
                // bought out of the same tank-minus-reserve budget, and PROVEN by re-flying the whole journey
                // through AutopilotRehearsal with it — turns the refusal into a promise. If it does, the burn
                // becomes a step the plan shows (Map.Plot.FlightPlan.ScheduledAutopilotBurns) and the arm
                // proceeds on the rehearsal that INCLUDES it, so nothing downstream is quoting a flight that
                // was never flown. A brake is only tried when no transfer schedule already rides this arm.
                CaptureBrake.Solution? braked = schedule is null && !r.BudgetExceeded
                    ? CaptureBrake.Solve(_ship, _ephemeris, _simulator, bodyId, budget,
                        burnEpoch: null, maxHorizonSeconds: BrakeSearchHorizon(bodyId), capturePath: true)
                    : null;

                if (braked is { } brake)
                {
                    schedule = brake.Schedule;
                    transferSummary = CaptureBrake.StepLine(brake, name);
                    plannerNote = null;
                    r = brake.Rehearsal;
                    ShowPulseMessage(CaptureBrake.AddedText(brake, name));
                }
                else
                {
                    // #928: quote the CHARGED number — what the tank will really lose at the autopilot's
                    // tenth — never the raw Δv count. The refusal's arithmetic must be the arithmetic the
                    // flight then performs, or "It won't strand you" is a sentence about a different ship.
                    // #957: and when the refusal is NOT about money, the why clause now carries the geometry
                    // — where the ship is, how fast, and against which thresholds — through the very same
                    // Core formatter the arrive step's ✗ row speaks with, plus the course's own closest pass
                    // so the captain has a moment to scrub to. "Can't verify a capture from here" survives
                    // only as the last-resort clause for a body with no arrival geometry to quote at all.
                    string why = r.BudgetExceeded || r.PulsesCharged > budget
                        ? $"needs ≈{r.PulsesCharged} p (incl. insertion), tank has {_reactionMassPulses} and keeps {reserve} in reserve"
                        : ArrivalCheckNow(bodyId) is { } snapshot
                            ? ArrivalStepRule.RefusalWhy(snapshot, NearestWindowNote(bodyId))
                            : "can't verify a capture from here — no clear window within range";
                    _autopilotStandDownReason = $"autopilot declines {name}: {why}. It won't strand you.";
                    ResetAutopilotBudget();
                    ShowPulseMessage($"🛰 {_autopilotStandDownReason}");
                    return; // NOT armed — the whole point of the promise
                }
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

            // #286 moon-docked clearance: the rehearsal's path ends at the insertion, so the #278 gate above
            // never sees the KEPT orbit that follows. A kept orbit around a moon can be geometrically bigger
            // than the moon's clearance from its parent — it would sweep through the planet the moon circles
            // beside. Judge the kept orbit itself: if the moon has no flyable park at all, refuse with the
            // reason (never thread the planet silently); if the standard park would breach the parent, note
            // that the autopilot will hold a tighter orbit. Inert for every shipped moon (the tide-stable
            // park clears the parent with a wide margin) — the guard for any future inner moon.
            if (target is not null
                && MoonOrbitClearance.Solve(_ephemeris, target, SimTime) is { } keptVerdict)
            {
                if (keptVerdict.NoSafeOrbit)
                {
                    _autopilotStandDownReason = $"autopilot declines {name}: {MoonOrbitClearance.RefusalText(keptVerdict)}.";
                    ResetAutopilotBudget();
                    ShowPulseMessage($"🛰 {_autopilotStandDownReason}");
                    return; // NOT armed — no kept orbit there clears the planet
                }
                if (keptVerdict.Clamped)
                {
                    ShowPulseMessage($"🛰 {MoonOrbitClearance.RefusalText(keptVerdict)}.");
                }
            }

            _armedBudgetPulses = r.PulsesCharged; // #928: the arm line and the panel quote the tenth
            _armedSpentPulses = 0;                // the raw ledger the tenth accumulates against
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
        EnsureArriveStepFor(bodyId);  // #957: …and the plan list SAYS so — the arrival is a step, always
        string trimQuote = _keepTrimPulsesPerDay > 0 ? $"; then holds the orbit, trim ≈{_keepTrimPulsesPerDay} p/day" : "";
        // #204: the arm-time quote names the final step. A μ=0 station ends at the ⚓ berth, not a park —
        // for an honest errand the autopilot auto-docks; a hostile-flagged run stands into the envelope
        // for the captain's ⚓ Dock.
        string arrival = BodyById(bodyId) is { } armedBody && IsDockableHaven(armedBody)
            ? (AutoDockHonest(armedBody)
                ? $"auto-dock at {BodyName(bodyId)}"
                : $"stand into the dock envelope at {BodyName(bodyId)} for your ⚓ Dock")
            : $"park at {BodyName(bodyId)}";
        // #928: "at the autopilot's tenth" is not decoration — it is why the quoted number is a tenth of
        // the Δv a hand would pay for the same arrival, and it is the number the tank is really charged.
        ShowPulseMessage($"Insertion armed — budgeted ≈{_armedBudgetPulses} p at the autopilot's tenth; the ship will {arrival} when the window opens{trimQuote} 🛰");
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
        // #286: the kept-orbit radius is bounded so the circularized park clears the moon's PARENT planet.
        // Inert for every shipped moon (the tide-stable park is far tighter than this cap); the guard that
        // an inner moon with a small Hill sphere can never be circled through the world beside it.
        double keptRadiusCap = OrbitRule.MaxKeptRadiusUnderParent(
            _ephemeris.InstantaneousOrbitRadius(body.Id, SimTime), parent);
        double keptPark = Math.Min(OrbitRule.ParkingRadius(body, hill), keptRadiusCap);

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
                OrbitKeeping.TrimPulseCost(_ship, bodyPos, bodyVel, body, keptPark)
                    <= _reactionMassPulses;
            return;
        }

        // #969 THE HOLD — an arm made at PLAN TIME is a promise about a pass that has not come round yet.
        // Until it does, the autopilot touches NOTHING: the captain's own plotted burns are flying the ship,
        // and every line below would be the autopilot flying its own approach instead of the plan (and the
        // convergence watchdog would stand it down for "not converging" on a trip that has not begun). The
        // hold lifts at the pass epoch OR the moment the ship is honestly near the body, whichever comes
        // first — after which this is an ordinary armed arrival and the unchanged insertion/dock path below
        // finishes the trip with no further input. That is the owner's "absolutely no steps needed".
        double distanceToTarget = (_ship.Position - bodyPos).Length;
        if (ArrivalStepRule.ArrivalPromiseIsStillAhead(
                _armedArrivalPassSimTime, SimTime, distanceToTarget, ArrivalNearRange(body, hill)))
        {
            return; // coast the plan — the arrival is still ahead
        }

        if (_armedArrivalPassSimTime is not null)
        {
            OpenTheArrivalWindow(body); // the promise has come round: the loop below has the ship now
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
            // #244 · THE ENVELOPE IS NOT THE DESTINATION. Owner, arrived at the roadster: "I think we
            // dropped out of autopilot… did we miss the dock button press while warping?" The autopilot had
            // SUCCEEDED — 499,721 km out, rel 4.0 — and for a WRECK that success is wrong: a fetch pickup is
            // proximity at a three-metre object, so half a million kilometres is a car-park in the next
            // county. DockRule.Arrived asks the clamp gate (DockableHavens.IsDockable, the predicate the ⚓
            // button itself obeys) rather than BodyKind or μ: a berth with an arm to throw is arrived at
            // when the arm can reach, and one without is arrived at where the errand actually happens.
            if (DockRule.Arrived(_ship, bodyPos, bodyVel, body))
            {
                AutopilotStandInEnvelope(body);
                return;
            }
            // Schedule done (or none available) but not yet matched/alongside — fall through to the legacy
            // Approach loop to match velocity and close the last stretch; its reserve guard stays intact.
            // For a non-clamp berth that stretch is now the last mile proper, flown by the same loop that
            // always closed the gap: nothing here burns differently, it simply stops later.
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

        switch (OrbitRule.AutopilotDecision(_ship, bodyPos, bodyVel, body, hill, keptRadiusCap))
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
                // #928 the tenth: the raw Δv price above is what a HAND would pay; the autopilot's own
                // charge comes from the accumulator, so the flown journey costs exactly the ⌈raw/10⌉ the
                // rehearsal quoted and the refusal arithmetic used.
                int approachCharge = AutopilotRehearsal.ChargeForBurn(_armedSpentPulses, approachCost);
                // The reserve floor (#146): the autopilot never burns the tank below the reserve it
                // promised to keep. If an approach burn would breach it, reality has diverged from the
                // rehearsed plan — only possible via something external (a manual burn, damage, a
                // dock) or a harder approach than rehearsed — so hand back LOUDLY instead of bleeding
                // the tank dry. The owner's ruling: never a silent drop.
                int reserveFloor = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
                if (_reactionMassPulses - approachCharge < reserveFloor)
                {
                    AutopilotStandDown($"autopilot handed back near {body.Name} — fuel plan broken (reserve floor reached; a manual burn, damage, or a harder approach than budgeted)");
                    return;
                }

                Vector2d beforeTheApproach = _ship.Velocity;
                _ship = OrbitRule.Approach(_ship, bodyPos, bodyVel, body, obstacle, hill);
                _reactionMassPulses -= approachCharge;
                _armedSpentPulses += approachCost;
                _approachBurnCount++;
                // #167 BURN KIND 4/9 - THE AUTOPILOT'S APPROACH BURN.
                BurnFired(approachCharge, _ship.Velocity - beforeTheApproach);
                StaleFutureNodes();
                ShowPulseMessage($"Approach burn — falling toward {body.Name} ({approachCharge} p) 🛰");
                return;

            case OrbitRule.AutopilotAction.Insert:
                int cost = OrbitRule.PulseCost(_ship, bodyPos, bodyVel, body);
                int insertCharge = AutopilotRehearsal.ChargeForBurn(_armedSpentPulses, cost); // #928 the tenth
                // The insertion is the arrival — it may dip into the reserve to complete the park.
                // Only an outright can't-afford-it (post-rehearsal, only via external divergence)
                // stands it down.
                if (insertCharge > _reactionMassPulses)
                {
                    AutopilotStandDown($"autopilot handed back at {body.Name} — insertion needs {insertCharge} p, only {_reactionMassPulses} left (fuel plan broken externally)");
                    return;
                }

                Vector2d beforeTheInsertion = _ship.Velocity;
                _ship = OrbitRule.Insert(_ship, bodyPos, bodyVel, body);
                _reactionMassPulses -= insertCharge;
                _armedSpentPulses += cost;
                // #167 BURN KIND 5/9 - THE AUTOPILOT'S INSERTION - the arrival, and the biggest burn in the
                // game. The pulse scaling is what makes it read as one against a hand's single trim.
                BurnFired(insertCharge, _ship.Velocity - beforeTheInsertion);
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
                _autopilotPlanBodyClearance = null; // #962: …and the park watchdog is the keeping rule's now
                _armedTransferSchedule = null;
                _armedTransferSummary = null;
                _armedTransferBurnsFired = 0;
                ResetApproachTracking();
                double park = keptPark; // #286: the clamped park the keeper trims back to (clears the parent)
                _orbitKept = true;
                _keepTrimPulsesPerDay = OrbitKeepingTable.TrimPulsesPerDay(
                    body, hill, parent.Mu, body.OrbitRadius, _ship.Velocity.Length);
                _keepNextCheckTime = SimTime + OrbitKeeping.TrimCadenceFraction * OrbitRule.LocalOrbitPeriod(park, body.Mu);
                _keepTrimsFired = 0;
                ArrivedAt(body.Id);
                TheArrivalIsRemembered(body.Id);   // #973 L4: a place can finish a page you don't remember writing
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

        // #286: trim back to the CLAMPED park (bounded so the kept orbit clears the parent), not the raw
        // tide-stable radius — otherwise a clamped orbit would be trimmed back out toward the planet.
        double park = Math.Min(
            OrbitRule.ParkingRadius(body, hill),
            OrbitRule.MaxKeptRadiusUnderParent(_ephemeris!.InstantaneousOrbitRadius(body.Id, SimTime), parent));
        if (SimTime < _keepNextCheckTime)
        {
            return; // between cadence points — let the reversible oscillation reverse itself
        }
        _keepNextCheckTime = SimTime + OrbitKeeping.TrimCadenceFraction * OrbitRule.LocalOrbitPeriod(park, body.Mu);

        if (!OrbitKeeping.NeedsTrim(_ship, bodyPos, bodyVel, body))
        {
            return; // still tight inside the tolerance — nothing to spend
        }

        // #928: a keeping trim is NOT economized. The tenth is the price of an APPROACH — the fine
        // trajectory a hand cannot fly — while the kept orbit's trims are quoted honestly and separately
        // from Lab 25's per-body table ("trim ≈N p/day") and spent in full, so OrbitHold's "holds for N
        // days" and the tank keep telling the same story (the Lab 25 law is untouched).
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

        Vector2d beforeTheTrim = _ship.Velocity;
        _ship = OrbitKeeping.Trim(_ship, bodyPos, bodyVel, body, park);
        _reactionMassPulses -= cost;
        _armedSpentPulses += cost;
        _keepTrimsFired++;
        // #167 BURN KIND 6/9 - THE STATION-KEEPING TRIM. Small, periodic, and the whole reason the banner
        // says AUTOPILOT HOLDS THE ORBIT - a held park that visibly spends a puff now and then is the
        // owner's ruling made legible without opening a panel.
        BurnFired(cost, _ship.Velocity - beforeTheTrim);
        StaleFutureNodes();
        ShowPulseMessage($"🛰 orbit trim at {body.Name} ({cost} p) — holding the park");
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
        Vector2d beforeTheOrbit = _ship.Velocity;
        _ship = OrbitRule.Insert(_ship, bodyPos, bodyVel, oi.Body);
        _reactionMassPulses -= oi.Cost;
        // #167 BURN KIND 7/9 - THE PANEL'S OWN ORBITAL INSERTION (the button / the `o` key). The `board`
        // cue below is the ARRIVAL's jingle and stays; this is the burn that got her there.
        BurnFired(oi.Cost, _ship.Velocity - beforeTheOrbit);
        ArrivedAt(oi.Body.Id);
        StaleFutureNodes();
        CompleteBoundCargoRunQuests(); // a parcel bound for this moon haven delivers on the park (#175)
        ShowPulseMessage($"Orbital insertion — bound to {oi.Body.Name} 🛰");
        TheArrivalIsRemembered(oi.Body.Id);   // #973 L4: …and the place may finish a grey page, said after the receipt
        RendererInterop.PlayCue("board");
    }

    private float[] _autopilotPlanScratch = [];

}
