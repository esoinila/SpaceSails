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
        if (_arrive is null)
        {
            return;
        }

        // Arming says "this is where we're going" — and it must say it about THIS step, not the panel's
        // last thought. ToggleArmedInsertion sets the destination from the body id it is handed.
        ToggleArmedInsertion(_arrive.BodyId);
    }

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
