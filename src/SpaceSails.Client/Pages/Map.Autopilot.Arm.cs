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
/// #146/#147/#179 · ARMING, AND THE TWO RADII THE ARM IS MEASURED AGAINST — the whole feasibility
/// question, settled once at the press: rehearse the flight, price it at the tenth, refuse it out loud
/// if the tank cannot hold it, and take the second click before disarming.
///
/// <para>#179 is why disarm confirms: a single stray click on an armed body is what dropped the owner
/// off orbit. #286 is why the park has two radii — <c>KeptRadiusCap</c> is the widest park whose swept
/// circle still clears the moon's PARENT, and <c>KeptParkRadius</c> is the tide-stable park under it.
/// They are separate because the insertion gate wants the raw cap.</para>
///
/// <para>Split out of <c>Map.Autopilot.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
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

    /// <summary>#286 · THE PARK IS SPELLED ONCE. The kept-orbit radius is a single quantity with two
    /// readers — <see cref="CheckArmedInsertion"/> parks at it, <c>StationKeep</c> trims back to it — and
    /// for years each one built it from its own expression. They agreed; nothing made them agree, and a
    /// change to one would have silently split the radius the autopilot arrives at from the radius the
    /// keeper holds. Both now ask here, so the two sites agree by construction.
    ///
    /// <para><see cref="KeptRadiusCap"/> is #286's cap alone (the widest park whose swept circle still
    /// clears the moon's PARENT planet); <see cref="KeptParkRadius"/> is the tide-stable park under it.
    /// They are separate because the insertion gate wants the raw cap —
    /// <c>OrbitRule.AutopilotDecision</c> applies the same <c>Math.Min</c> itself.</para></summary>
    private double KeptRadiusCap(CelestialBody body, CelestialBody parent) =>
        OrbitRule.MaxKeptRadiusUnderParent(_ephemeris!.InstantaneousOrbitRadius(body.Id, SimTime), parent);

    /// <summary>#286 · The clamped park: the tide-stable radius, bounded by <see cref="KeptRadiusCap"/> so
    /// the circularized orbit clears the parent. Inert for every shipped moon (the tide-stable park is far
    /// tighter than the cap).</summary>
    private static double KeptParkRadius(CelestialBody body, double hill, double keptRadiusCap) =>
        Math.Min(OrbitRule.ParkingRadius(body, hill), keptRadiusCap);

    /// <summary>#1179 · THE SAME PARK, FOR A CALLER THAT HAS NO PARENT IN HAND. The autopilot's own halves
    /// have already looked the parent up by the time they want the park; the surfaces that merely SPEAK about
    /// it — the flight-plan board's holding line, the warp tier — have the body and its Hill radius and
    /// nothing else, and each of them used to reach past both helpers for a raw
    /// <c>OrbitRule.ParkingRadius</c> rather than write the lookup out again. That is how four unclamped
    /// quotes of a clamped quantity got written. So the lookup lives here, once, and there is still exactly
    /// one spelling of the park on the client.
    ///
    /// <para>A body with no parent — a planet, the sun — has no parent to clear, so it has no cap and the
    /// tide-stable park stands; that is the <c>AutopilotDecision</c> default (+∞) said again.</para></summary>
    private double KeptParkRadius(CelestialBody body, double hill) =>
        KeptParkRadius(body, hill, BodyById(body.ParentId) is { } parent
            ? KeptRadiusCap(body, parent)
            : double.PositiveInfinity);
}
