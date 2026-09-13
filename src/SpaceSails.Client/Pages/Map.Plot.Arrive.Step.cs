using System.Globalization;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Plot.Arrive (#251 split; the header note lives in Map.Plot.Arrive.cs) —
// THE BUTTONS, and the pulse estimate they quote.
//
// The owner's north star for the whole unified nav list is that "the sub-panel of a step is WHERE
// the related buttons live", so this is that sub-panel's own code: what the add button says, what it
// adds, what makes sure a step exists for a body, and what removes, clears, opens and arms one.
//
// A pure move out of Map.Plot.Arrive.cs under #251 — no member renamed, re-scoped or re-ordered, and
// not one field moved.
public partial class Map
{
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
    /// about to add before he adds it (#950 — the old chip named a body he never chose).
    /// <para>#949 · The FACE is Core's (<see cref="ArrivalStepRule.AddAtScrubButton"/>) and only the
    /// bracketed body is this method's, so the help card can print the same button without retyping it.
    /// It was the same expression written out twice here; now it is written nowhere.</para></summary>
    private string ArriveButtonLabel(ArrivalStepRule.ArrivalKind kind) =>
        ArriveCandidate(kind) is { } pass
            ? $"{ArrivalStepRule.AddAtScrubButton(kind)} ({pass.BodyName})"
            : ArrivalStepRule.AddAtScrubButton(kind);

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

        ReachTheArrivalWithTheRibbon();

        ArrivalStepRule.ArrivalCheck? check = ArriveCheck();
        _arrive.LastValid = check?.Valid;
        _arrive.LastPassWasOffTheRibbon = ArriveRibbonIsTooShort();
        ShowPulseMessage(check is { } c
            ? $"Plan ends at {pass.BodyName}. {ArrivalStepRule.Verdict(c)}"
            : ArriveRibbonTooShortLine() is { } shortLine
            ? $"Plan ends at {pass.BodyName}. {shortLine}"
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
        // #952: a row laid by an ARM gets the same reach as one laid by the button — the plan gained an
        // ending either way, and the course has to be long enough to show it.
        ReachTheArrivalWithTheRibbon();
        // Seed the transition watch from the arrival as it stands, so an arm made into an already-poor
        // geometry does not immediately pop the "you ruined the plan" alarm at the captain who just armed
        // it — the row's ✗ is the honest surface for that (ArrivalStepRule.ShouldWarn).
        _arrive.LastValid = ArriveCheck()?.Valid;
        _arrive.LastPassWasOffTheRibbon = ArriveRibbonIsTooShort();
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
}
