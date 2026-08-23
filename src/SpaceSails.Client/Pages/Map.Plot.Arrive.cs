using System.Globalization;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — THE ARRIVE STEP
// (#952/#955/#950/#957): the terminal step that finally ends the flight plan, "+ Add orbit at scrub" /
// "+ Add dock at scrub", its live valid/invalid bit, and the wake-up call when a good plan is ruined.
//
// This is PR-D1 of docs/WednesdayPlan/UnifiedNavListNotes.md landing whole. That doc's north star: the
// unit of planning is the TRIP — "from docked position to docked position, or orbit on another place" —
// one list, readable top to bottom, and "the sub-panel of a step is WHERE the related buttons live". So
// the arrival is a STEP like every other: a collapsed one-line row with a state pip and a caret, whose
// editor opens in place and owns Arm / Disarm / scrub-to-it / remove. Nothing about the arrival is a
// loose button on the HUD any more — the old "✈ Insert at X pass" chip that floated above the plan (and
// that, parked at Earth, offered to orbit EARTH — #950) is gone; this step replaced it.
//
// The LAW (valid/invalid, the sentence, the one-shot alarm) lives in Core (ArrivalStepRule) so it is
// unit-tested without a browser and so the row, the pilot banner and the autopilot's refusal all read one
// set of thresholds — the ones the sim itself obeys.
public partial class Map
{
    /// <summary>
    /// The plan's terminal step. Deliberately holds only the IDENTITY of the arrival (which body, orbit or
    /// dock) — never a frozen copy of the pass. The geometry is re-read off the live plotted path every
    /// reprojection, which is exactly what makes the ✓/✗ bit flip when a mid-flight edit or a missed burn
    /// ruins the plan (the owner's ask) instead of standing there stale and green.
    /// </summary>
    private sealed class ArriveStep
    {
        public required string BodyId { get; init; }
        public required ArrivalStepRule.ArrivalKind Kind { get; init; }

        /// <summary>The last evaluated validity, for the one-shot transition alarm. Null until judged.</summary>
        public bool? LastValid { get; set; }
    }

    private ArriveStep? _arrive;

    // The plotted path's closest pass by every body, kept from the pass cadence (Map.Sim.Tick) so the
    // arrive candidate can be re-picked against the LIVE scrub without re-scanning 8000 samples on a
    // slider drag. A handful of entries — one per body.
    private IReadOnlyList<ClosestApproach.Pass> _passes = [];

    // The wake-up call: set on the valid → invalid transition, cleared when the plan ends safely again.
    // Persistent (not a 1.5-s toast) because the whole point is a sleeping captain — #147's lesson.
    private string? _arriveAlarm;
    private bool _arriveAlarmDismissed;

    // ===== Candidates: what "+ Add orbit at scrub" / "+ Add dock at scrub" would add =====

    /// <summary>
    /// The pass this arrival would be built from: among the passes of the plotted course, the arrivable one
    /// NEAREST THE SCRUB. The scrub is the captain's finger on the plan — he drags it to the Mars encounter
    /// and presses the button — so the button means what its name says, and #950's "it suggests orbiting
    /// Earth" (the old chip picked the tightest pass anywhere on the path, which parked at Earth is Earth)
    /// cannot happen. The body is spelled out on the button too, so it is never a guess.
    /// </summary>
    private ClosestApproach.Pass? ArriveCandidate(ArrivalStepRule.ArrivalKind kind)
    {
        if (_ephemeris is null)
        {
            return null;
        }

        ClosestApproach.Pass? best = null;
        double bestDelta = double.MaxValue;
        double scrub = ScrubTime;
        foreach (ClosestApproach.Pass pass in _passes)
        {
            if (BodyById(pass.BodyId) is not { } body || !ArrivableAs(body, kind))
            {
                continue;
            }

            double delta = Math.Abs(pass.SimTime - scrub);
            if (delta < bestDelta)
            {
                (bestDelta, best) = (delta, pass);
            }
        }

        // The tick's own tightest-orbitable pick is the SAME fact, already computed (Map.Sim.Tick), and it
        // survives the frame between a reprojection and the next pass sweep — so the button is never dead
        // for a frame while the list is being rebuilt. It is data reuse, not a second surface: the chip it
        // used to draw is gone.
        if (best is null && kind == ArrivalStepRule.ArrivalKind.Orbit && _armablePass is { } armable)
        {
            return armable;
        }

        return best;
    }

    /// <summary>Can this body end a plan in this way? A dock haven takes the ⚓ (DockableHavens is the one
    /// registry); an orbit needs a real well with a parent — never the sun, never a μ=0 station.</summary>
    private static bool ArrivableAs(CelestialBody body, ArrivalStepRule.ArrivalKind kind) =>
        kind == ArrivalStepRule.ArrivalKind.Dock
            ? IsDockableHaven(body)
            : body.ParentId is not null && body.Kind != BodyKind.Station && body.Mu > 0;

    // ===== The step's live geometry and its verdict =====

    private ClosestApproach.Pass? ArrivePassFor(string bodyId)
    {
        foreach (ClosestApproach.Pass pass in _passes)
        {
            if (pass.BodyId == bodyId)
            {
                return pass;
            }
        }
        return null;
    }

    /// <summary>Judge an arrival at this body against the plotted course as it stands NOW. The thresholds
    /// come from Core (ArrivalStepRule), which reads them off OrbitRule / DockRule — the rules the flight
    /// obeys — so the row's sentence and the sim can never tell two stories.</summary>
    private ArrivalStepRule.ArrivalCheck? CheckArrival(ArrivalStepRule.ArrivalKind kind, string bodyId)
    {
        if (_ephemeris is null
            || ArrivePassFor(bodyId) is not { } pass
            || BodyById(bodyId) is not { ParentId: not null } body
            || BodyById(body.ParentId) is not { } parent)
        {
            return null;
        }

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        Vector2d shipVel = SampledVelocityAt(pass.SimTime);
        Vector2d bodyVel = PassBodyVelocity(bodyId, pass.SimTime);
        return ArrivalStepRule.Check(kind, body.Name, pass.Distance, (shipVel - bodyVel).Length, hill);
    }

    /// <summary>The arrive step's verdict, or null when there is no step (or no projection yet).</summary>
    private ArrivalStepRule.ArrivalCheck? ArriveCheck() =>
        _arrive is null ? null : CheckArrival(_arrive.Kind, _arrive.BodyId);

    private Vector2d PassBodyVelocity(string bodyId, double simTime)
    {
        const double h = 1.0;
        return (_ephemeris!.Position(bodyId, simTime + h) - _ephemeris.Position(bodyId, simTime - h)) / (2 * h);
    }

    /// <summary>The ≈pulses the arrival itself costs from that pass — the insertion burn for an orbit
    /// (OrbitRule.PulseCost, the same estimate the old chip quoted), or the match burn that sheds the
    /// excess above the clamp's speed for a dock. Priced with the one kernel, OrbitRule.PulsesFor.</summary>
    private int ArriveEstPulses(ArrivalStepRule.ArrivalKind kind, string bodyId)
    {
        if (_ephemeris is null || ArrivePassFor(bodyId) is not { } pass || BodyById(bodyId) is not { } body)
        {
            return 0;
        }

        Vector2d shipVel = SampledVelocityAt(pass.SimTime);
        Vector2d bodyVel = PassBodyVelocity(bodyId, pass.SimTime);
        if (kind == ArrivalStepRule.ArrivalKind.Dock)
        {
            double excess = Math.Max(0, (shipVel - bodyVel).Length - DockRule.MatchSpeed);
            return excess <= 0 ? 0 : OrbitRule.PulsesFor(excess, shipVel.Length);
        }

        var passState = new ShipState(pass.ShipPosition, shipVel, pass.SimTime);
        return OrbitRule.PulseCost(passState, _ephemeris.Position(bodyId, pass.SimTime), bodyVel, body);
    }

    // ===== The buttons =====

    /// <summary>The label on the compose button, with the body named so the captain can see what he is
    /// about to add before he adds it (#950 — the old chip named a body he never chose).</summary>
    private string ArriveButtonLabel(ArrivalStepRule.ArrivalKind kind) =>
        ArriveCandidate(kind) is { } pass
            ? $"{(kind == ArrivalStepRule.ArrivalKind.Dock ? "⚓" : "🛰")} + Add {ArrivalStepRule.Verb(kind)} at scrub ({pass.BodyName})"
            : $"{(kind == ArrivalStepRule.ArrivalKind.Dock ? "⚓" : "🛰")} + Add {ArrivalStepRule.Verb(kind)} at scrub";

    /// <summary>
    /// Append the arrival to the end of the plan — "the cherry on top" (#952). One terminal step at a
    /// time: adding a second replaces the first, because a plan ends once. The step is added whether or
    /// not the pass is currently good; an INVALID arrival is the point — its row says, in numbers, how far
    /// the course is from ending safely, and the ±p / ±d / ±h buttons on the burn rows are how the captain
    /// closes the gap (the loop the owner asked for: "I wanted to iterate the path until I could add orbit
    /// mars step to the end of my plan").
    /// </summary>
    private void AddArriveAtScrub(ArrivalStepRule.ArrivalKind kind)
    {
        if (ArriveCandidate(kind) is not { } pass)
        {
            ShowPulseMessage("No body on this course to arrive at — scrub to a pass first.");
            return;
        }

        _arrive = new ArriveStep { BodyId = pass.BodyId, Kind = kind };
        _arriveAlarm = null;
        _arriveAlarmDismissed = false;
        _openEditor = FlightEditorKind.Arrive;   // a freshly added step opens its own editor (PR-D2 idiom)
        _selectedPlanNode = null;

        ArrivalStepRule.ArrivalCheck? check = ArriveCheck();
        _arrive.LastValid = check?.Valid;
        ShowPulseMessage(check is { } c
            ? $"Plan ends at {pass.BodyName}. {ArrivalStepRule.Verdict(c)}"
            : $"Plan ends at {pass.BodyName}.");
    }

    /// <summary>
    /// #957 — <b>ARMING IS A STEP, WHEREVER IT WAS PRESSED.</b> The owner, after the autopilot finally
    /// accepted The Rusty Roadstead: <i>"it accepted the autopilot but it does [not] add it as navigation
    /// step to the list."</i> Arming from the destination card, the body menu or the O-key used to leave
    /// the plan list saying nothing about where the trip ends. Now every accepted arm ends the plan at
    /// that body — one list, dock-to-dock, exactly the PR-D1 shape — and the row it creates is the same
    /// row "+ Add orbit at scrub" builds, with the same ✓/✗ bit and the same buttons.
    /// </summary>
    private void EnsureArriveStepFor(string bodyId)
    {
        if (_arrive is { } existing && existing.BodyId == bodyId)
        {
            return;
        }

        ArrivalStepRule.ArrivalKind kind = BodyById(bodyId) is { } body && IsDockableHaven(body)
            ? ArrivalStepRule.ArrivalKind.Dock
            : ArrivalStepRule.ArrivalKind.Orbit;
        _arrive = new ArriveStep { BodyId = bodyId, Kind = kind };
        _arriveAlarm = null;
        _arriveAlarmDismissed = false;
        // Seed the transition watch from the arrival as it stands, so an arm made into an already-poor
        // geometry does not immediately pop the "you ruined the plan" alarm at the captain who just armed
        // it — the row's ✗ is the honest surface for that (ArrivalStepRule.ShouldWarn).
        _arrive.LastValid = ArriveCheck()?.Valid;
    }

    private void RemoveArriveStep()
    {
        ClearArriveStep();
        ShowPulseMessage("Arrival step removed — the plan no longer ends anywhere.");
    }

    /// <summary>
    /// Take the arrival off the end of the plan, silently. Used by the row's ✖ remove (which speaks for
    /// itself) and by <c>ArrivedAt</c> — #962's "the voyage is over, the orders complete" hook, which the
    /// berth lane wired to the clamp and the cast-off as well as the orbital insert. A step whose voyage is
    /// FINISHED must come off the board: left standing it is a terminal step that will never fire again,
    /// and clamped on at its own berth it would even read ✓ VALID forever (distance ≈ 0, rel ≈ 0) — a green
    /// badge over a trip that is already behind you.
    /// </summary>
    private void ClearArriveStep()
    {
        _arrive = null;
        _arriveAlarm = null;
        _arriveAlarmDismissed = false;
        if (_openEditor == FlightEditorKind.Arrive)
        {
            _openEditor = FlightEditorKind.None;
        }
    }

    private void ToggleArriveEditor()
    {
        _openEditor = _openEditor == FlightEditorKind.Arrive ? FlightEditorKind.None : FlightEditorKind.Arrive;
        _selectedPlanNode = null;
    }

    /// <summary>
    /// Arm (or disarm) the autopilot FOR THIS STEP'S BODY — the #950 fix at its root. The old surface
    /// armed whatever the nav panel currently pointed at, which while parked at Earth was Earth; this one
    /// can only ever arm the body the step names, at the pass the step is judged on.
    /// </summary>
    private void ArmArriveStep()
    {
        if (_arrive is not { } step)
        {
            return;
        }

        // Disarming is the same act wherever it is pressed — the #179 double-confirm lives in one place.
        // #969: an arm made for a pass still ahead is a PLAN-TIME promise, and it is rehearsed from the
        // state the plot delivers at that pass rather than from the ship's present state (see
        // ArmTheArrivalForItsPass). Everything else — the captain already at the door — is the historic NOW
        // arm, untouched, with its transfer planner and its #957 braking search behind it.
        if (_armedOrbitBodyId != step.BodyId && ArriveIsAThen(step))
        {
            ArmTheArrivalForItsPass(step);
            return;
        }

        // Arming says "this is where we're going" — and it must say it about THIS step, not the panel's
        // last thought. ToggleArmedInsertion sets the destination from the body id it is handed.
        ToggleArmedInsertion(step.BodyId);
    }

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
        if (RejectNavWhileDocked())
        {
            // Deliberately unchanged (#969 item 3): a clamped ship's nav is locked, so the plan-time arm is
            // refused for the same reason every other nav act is, and says so in the same sentence. Cast off
            // first; the arrive step and its numbers survive the undock, so the arm is one press away.
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

    /// <summary>Jump the scrub clock to the arrival's own pass — the step's own "scrub to it".</summary>
    private void ScrubToArrive()
    {
        if (_arrive is not null && ArrivePassFor(_arrive.BodyId) is { } pass)
        {
            _scrubOffsetSeconds = Math.Max(0, pass.SimTime - _ship.SimTime);
        }
    }

    // ===== The one-shot wake-up call =====

    /// <summary>
    /// Runs on the pass cadence, right after the passes are rebuilt. When a plan that ENDED SAFELY stops
    /// doing so — an edit, a missed burn, a sling that bent the course — the row flips to ✗ and the
    /// captain is woken ONCE: a pop-up he must see, a persistent banner that survives warp, a ledger
    /// receipt, and warp dropped to 1× so the ship is not still barrelling on at 10,000× while nobody
    /// flies her. Coming back to valid clears the banner and re-arms the alarm for next time.
    /// </summary>
    private void RefreshArriveValidity()
    {
        if (_arrive is null)
        {
            _arriveAlarm = null;
            return;
        }

        if (ArriveCheck() is not { } check)
        {
            return; // no projection yet — judge nothing rather than cry wolf
        }

        if (ArrivalStepRule.ShouldWarn(_arrive.LastValid, check.Valid))
        {
            _arriveAlarm = ArrivalStepRule.BrokenPlanAlarm(check);
            _arriveAlarmDismissed = false;
            LogAutopilotEvent(_arriveAlarm);
            ShowPulseMessage(_arriveAlarm, PulseRank.Beat);
            Warp = 1;               // the drop must not slip past unseen at warp (the #147 idiom)
            _effectiveWarp = 1;
        }

        if (check.Valid)
        {
            _arriveAlarm = null;
        }

        _arrive.LastValid = check.Valid;
    }

    private void DismissArriveAlarm() => _arriveAlarmDismissed = true;

    // ===== The glance line (mirrors BurnGlanceLine's shape so the rows read as one list) =====

    private string ArriveGlanceLine(ArriveStep step)
    {
        string verb = ArrivalStepRule.Verb(step.Kind);
        string body = BodyName(step.BodyId);
        int est = ArriveEstPulses(step.Kind, step.BodyId);
        string when = ArrivePassFor(step.BodyId) is { } pass && pass.SimTime > SimTime
            ? $"in {FormatDuration(pass.SimTime - SimTime)}"
            : "now";
        string dist = ArrivePassFor(step.BodyId) is { } p2 ? FormatDistance(p2.Distance) : "—";
        return $"{(step.Kind == ArrivalStepRule.ArrivalKind.Dock ? "⚓" : "🛰")} {verb} {body} · pass {dist} · ≈{est} p · {when}";
    }
}
