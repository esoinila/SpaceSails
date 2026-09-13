using System.Globalization;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Plot.Arrive (#251 split; the header note lives in Map.Plot.Arrive.cs) —
// #969 · THE ARRIVAL ARMED *THEN*, NOT ONLY *NOW*.
//
// The owner's 2026-08-23 ruling in one sentence: three burns and one arrival, planned at the berth,
// and after that no steps needed. What was missing was never the STEP — it was the ARM, which
// rehearsed from the ship's state RIGHT NOW and so never reached a Mars nine months and three burns
// away. The fix is to rehearse from where the PLAN puts her, at the moment the plan puts her there:
// the very state the row's ✓/✗ is already judged on.
//
// A pure move out of Map.Plot.Arrive.cs under #251 — no member renamed, re-scoped or re-ordered, and
// not one field moved. The banner above ArmTheArrivalForItsPass travelled with it, because the
// ruling it quotes is the design record of these members and of nothing else.
public partial class Map
{
    // ===== #969 — THE ARRIVAL ARMED *THEN*, NOT ONLY *NOW* =====
    //
    // Owner ruling, 2026-08-23: "I just want the possibility to plan the trip from space to docked at one
    // go. So once the burns are planned right I can add the autopilot after the last burn to dock the ship
    // at plan time before the trip is even begun. … Say three burns and one autopilot to finish the trip to
    // Mars. After that no, absolutely no steps needed if the ship is not interfered with."
    //
    // What was missing was never the STEP (#952 built that) — it was the ARM. ToggleArmedInsertion settles
    // its promise by rehearsing the journey from the ship's state RIGHT NOW, ballistically: the plotted
    // burns are not in that flight, so from Earth the rehearsal of a Mars arrival nine months and three
    // burns away simply never reaches Mars, and the captain is told "can't verify a capture from here". The
    // fix is one sentence long: rehearse from where the PLAN puts her, at the moment the plan puts her
    // there — the very state the row's ✓/✗ is already judged on.

    /// <summary>The range at which the autopilot honestly has an arrival in its hands: the floor-free
    /// Hill-scaled capture range for an orbit, the clamp's envelope for a μ=0 berth. Floor-free on purpose —
    /// <see cref="OrbitRule.CaptureRangeFloorMeters"/> is three million km, which would call a ship "at the
    /// door" of a world it is still a season away from (the #146 lesson, borrowed).</summary>
    private static double ArrivalNearRange(CelestialBody body, double hillRadius) =>
        body.Kind == BodyKind.Station || body.Mu <= 0
            ? DockRule.EnvelopeMeters
            : OrbitRule.CaptureRangeHillRadii * hillRadius;

    /// <summary>The step's own near-range, resolved off the live ephemeris; 0 when there is nothing to
    /// resolve (which makes every predicate below fall back to "we are at the door").</summary>
    private double ArrivalNearRangeFor(string bodyId)
    {
        if (_ephemeris is null
            || BodyById(bodyId) is not { ParentId: not null } body
            || BodyById(body.ParentId) is not { } parent)
        {
            return 0;
        }
        return ArrivalNearRange(body, OrbitRule.HillRadius(body, parent.Mu));
    }

    /// <summary>Is this arrival a THEN — a pass still ahead, with the ship not yet near the body? The law
    /// itself is Core's (<see cref="ArrivalStepRule.ArrivalIsAThen"/>), shared with the hold that keeps the
    /// armed autopilot's hands off during the cruise, so the arm and the flight can never disagree about
    /// whether the arrival has come round.</summary>
    private bool ArriveIsAThen(ArriveStep step)
    {
        if (_ephemeris is null || _simulator is null || ArrivePassFor(step.BodyId) is not { } pass)
        {
            return false;
        }
        double distance = (_ship.Position - _ephemeris.Position(step.BodyId, SimTime)).Length;
        return ArrivalStepRule.ArrivalIsAThen(pass.SimTime, SimTime, distance, ArrivalNearRangeFor(step.BodyId));
    }

    /// <summary>The ship as the PLOT delivers her at this pass: the sampled position and velocity of the
    /// projected course — planned burns and all — at the pass epoch. This is the same path, read the same
    /// way, that <see cref="CheckArrival"/> already judges the row's ✓/✗ on, so the badge and the promise
    /// are two readings of one course and not two opinions about two ships.</summary>
    private ShipState PlottedStateAt(ClosestApproach.Pass pass) =>
        new(pass.ShipPosition, SampledVelocityAt(pass.SimTime), pass.SimTime);

    /// <summary>Reaction mass left AFTER every plotted burn still ahead has fired — what the arrival
    /// actually has to spend. Quoting the whole tank would be promising the arrival money the plan has
    /// already committed.</summary>
    private int PulsesLeftForTheArrival() => Math.Max(0, _reactionMassPulses - PlannedPulseTotal());

    /// <summary>
    /// <b>Arm the arrival for its own pass.</b> The promise is settled here, at plan time, against the state
    /// the plot delivers — and settled by FLYING it: <see cref="AutopilotRehearsal.Rehearse"/> from the pass
    /// state, priced at the #928 tenth, bought out of what the plan leaves in the tank minus the reserve
    /// floor. Deliverable ⇒ the step is ARMED and the plan is complete: the captain needs no further input
    /// unless something interferes. Not deliverable ⇒ #957's answer first (lay a braking step at the pass
    /// and re-fly the whole thing to prove it), and only if even that will not fly does the captain hear a
    /// refusal — in the arrive row's own numbers, never a shrug.
    /// </summary>
    private void ArmTheArrivalForItsPass(ArriveStep step)
    {
        // #955 NAV-1 · THE ONE CARVE-OUT IN THE NAV LOCK — AND IT IS NOT A LIVE NAV ACT. #969 refused this
        // from a berth for the same reason every other nav command is refused, and the owner's answer was the
        // step: "plan while docked, then the plan starts with an undock step recorded topmost". Arming a THEN
        // moves nothing — it is a promise about a pass months away, settled by rehearsing the plotted course —
        // so a clamped ship may make it PROVIDED the plan begins by casting her off. Without that first row
        // the promise would be rehearsed from a berth the ship never leaves, which is a promise about a voyage
        // that cannot happen. Every other nav act stays refused by the same guard, in the same ⚓ sentence.
        if (NavLockedByDock && !PlanBeginsWithCastOff)
        {
            ShowPulseMessage($"⚓ {DockNavLockTip} — {CastOffRule.ArmNeedsCastOff}.");
            return;
        }

        if (_ephemeris is null || _simulator is null || ArrivePassFor(step.BodyId) is not { } pass)
        {
            ShowPulseMessage("No plotted pass to arrive at yet — let the course settle first.");
            return;
        }

        string name = BodyName(step.BodyId);
        ShipState atThePass = PlottedStateAt(pass);
        int leftForArrival = PulsesLeftForTheArrival();
        int reserve = AutopilotRehearsal.ReservePulses(ReactionMassCapacity);
        int budget = Math.Max(0, leftForArrival - reserve);

        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            atThePass, _ephemeris, _simulator, step.BodyId, budget, capturePath: true);

        TransferPlanner.Schedule? schedule = null;
        string? summary = null;
        if (!r.Deliverable)
        {
            // #957 — DON'T COMPLAIN, BRAKE, and a plan-time arm gets the same courtesy: the correction is
            // searched AT THE PASS (that is where the leverage is for a trip that has not started) and
            // believed only if re-flying the whole arrival with it captures inside the same budget.
            CaptureBrake.Solution? braked = !r.BudgetExceeded
                ? CaptureBrake.Solve(atThePass, _ephemeris, _simulator, step.BodyId, budget,
                    burnEpoch: pass.SimTime, maxHorizonSeconds: AutopilotRehearsal.DefaultMaxHorizonSeconds,
                    capturePath: true)
                : null;

            if (braked is { } brake)
            {
                schedule = brake.Schedule;
                summary = CaptureBrake.StepLine(brake, name);
                r = brake.Rehearsal;
                ShowPulseMessage(CaptureBrake.AddedText(brake, name));
            }
            else
            {
                // The numbers, in the arrive row's own words — where the course puts her at that pass, how
                // fast, and against which thresholds — so the captain knows which way to iterate the burns.
                string why = r.BudgetExceeded || r.PulsesCharged > budget
                    ? $"the arrival needs ≈{r.PulsesCharged} p at that pass (incl. insertion), and the plan's "
                      + $"own burns leave only {leftForArrival} with {reserve} held in reserve"
                    : CheckArrival(step.Kind, step.BodyId) is { } snapshot
                        ? ArrivalStepRule.RefusalWhy(snapshot)
                        : "the plotted course never brings her near enough to take her";
                _autopilotStandDownReason = $"autopilot declines {name}: {why}. It won't strand you.";
                ResetAutopilotBudget();
                ShowPulseMessage($"🛰 {_autopilotStandDownReason}");
                return; // NOT armed — the plan-time promise keeps the same word the NOW arm keeps
            }
        }

        // #267 surface clearance, judged on the rehearsed line exactly as the NOW arm judges it: a promise
        // that captures within budget can still thread a body it passes, and that is a refusal too.
        if (SurfaceClearance.Check(r.Path, _ephemeris, step.BodyId) is { } clearance)
        {
            _autopilotStandDownReason = $"autopilot declines {name}: {SurfaceClearance.RefusalText(clearance)}.";
            ResetAutopilotBudget();
            ShowPulseMessage($"🛰 {_autopilotStandDownReason}");
            return;
        }

        // #286 moon-docked clearance, the same guard the NOW arm applies: the rehearsal stops at the
        // insertion, so it never sees the KEPT orbit that follows — and a kept orbit round a small moon can
        // sweep through the planet it circles beside.
        if (BodyById(step.BodyId) is { } moonTarget
            && MoonOrbitClearance.Solve(_ephemeris, moonTarget, pass.SimTime) is { } keptVerdict)
        {
            if (keptVerdict.NoSafeOrbit)
            {
                _autopilotStandDownReason = $"autopilot declines {name}: {MoonOrbitClearance.RefusalText(keptVerdict)}.";
                ResetAutopilotBudget();
                ShowPulseMessage($"🛰 {_autopilotStandDownReason}");
                return;
            }
            if (keptVerdict.Clamped)
            {
                ShowPulseMessage($"🛰 {MoonOrbitClearance.RefusalText(keptVerdict)}.");
            }
        }

        _armedBudgetPulses = r.PulsesCharged; // #928: quote the tenth, which is what the tank really loses
        _armedSpentPulses = 0;
        _armedTransferSchedule = schedule;    // the brake, if one was laid — it fires at the pass, as a step
        _armedTransferBurnsFired = 0;
        _armedTransferSummary = summary;
        _autopilotStandDownReason = null;
        _dockReadyStatus = null;
        _disarmConfirmBodyId = null;
        _armedOrbitBodyId = step.BodyId;
        _destinationBodyId = step.BodyId;     // arming says "this is where we're going"
        _armedArrivalPassSimTime = pass.SimTime;  // …and THIS is when. The hold reads it every tick.
        ResetApproachTracking();

        // #148/#196/#219 stay NULL on purpose while the promise is ahead: the intended path the autopilot
        // will fly does not exist yet — the line on the map is the captain's own plotted ribbon, and it is
        // the ribbon the collision alarm must keep judging for the whole cruise. Both are filled in from a
        // rehearsal at the real state the moment the arrival comes round (OpenTheArrivalWindow).
        _autopilotPlanPath = null;
        _autopilotPlanClosestPass = null;
        _autopilotPlanBodyClearance = null;

        // Friday §0: the park will be KEPT, so quote the trim budget honestly at arm time.
        _keepTrimPulsesPerDay = 0;
        if (BodyById(step.BodyId) is { Mu: > 0, ParentId: not null } target
            && BodyById(target.ParentId) is { } keepParent)
        {
            _keepTrimPulsesPerDay = OrbitKeepingTable.TrimPulsesPerDay(
                target, OrbitRule.HillRadius(target, keepParent.Mu), keepParent.Mu, target.OrbitRadius,
                TransferMath.BodyVelocity(_ephemeris, target.Id, pass.SimTime).Length);
        }

        string line = ArrivalStepRule.PlanIsComplete(
            BurnsStillAhead(), step.Kind, name, FormatDuration(pass.SimTime - SimTime), r.PulsesCharged);
        LogAutopilotEvent($"arrival armed at plan time — {line}");
        ShowPulseMessage($"🛰 {line}");
    }

    /// <summary>Plotted burns still to fire — the "three burns" half of the owner's sentence.</summary>
    private int BurnsStillAhead()
    {
        int n = 0;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && !node.Executed && node.SimTime > SimTime)
            {
                n++;
            }
        }
        return n;
    }

    /// <summary>The finished plan's one line, for the step's own sub-panel: what is left to do, and that
    /// nothing more is needed. Null unless this step is the armed plan-time promise.</summary>
    private string? ArrivePlanCompleteLine() =>
        _arrive is { } step && ArmedArrivalStillAhead && _armedOrbitBodyId == step.BodyId
            && ArrivePassFor(step.BodyId) is { } pass
            ? ArrivalStepRule.PlanIsComplete(
                BurnsStillAhead(), step.Kind, BodyName(step.BodyId),
                pass.SimTime > SimTime ? FormatDuration(pass.SimTime - SimTime) : "now", _armedBudgetPulses)
            : null;
}
