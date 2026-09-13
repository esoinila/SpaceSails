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
//
// #251 · AND NOW IT IS FOUR FILES. At 759 lines the step was a hair under the 800-line aim and the
// next thing the arrival learns would have tripped it. This file keeps the ArriveStep record itself,
// everything the family remembers between frames, and the two questions the row is built on: what
// "+ Add orbit at scrub" would add (the candidate pass) and what the step's live verdict is. It also
// keeps the one-shot wake-up call and the glance line, which are the row's own voice.
//
// The other three are each a CONTIGUOUS run of the base file, in the order the base laid them out:
//
//   .Ribbon  #952 — how long the plotted course actually is, and whether it reaches the plan's own
//            ending; the leading approach a picture opens on, and the reach that fixes a short one
//   .Step    the pulse estimate and THE BUTTONS: add, ensure, remove, clear, toggle the editor, arm
//   .Arm     #969's ruling — the arrival armed THEN and not only NOW: the near range, the is-it-a-THEN
//            question, the plotted state at the pass, and the rehearsal that arms off it
//
// Not one field moved and no member is renamed, re-scoped or re-ordered; the family declares no
// static field at all, and the two `static` members that moved are METHODS.
public partial class Map
{
    /// <summary>
    /// The plan's terminal step. Deliberately holds only the IDENTITY of the arrival (which body, orbit or
    /// dock) — never a frozen copy of the pass. The geometry is re-read off the live plotted path every
    /// reprojection, which is exactly what makes the ✓/✗ bit flip when a mid-flight edit or a missed burn
    /// ruins the plan (the owner's ask) instead of standing there stale and green.
    /// </summary>
    public sealed class ArriveStep
    {
        public required string BodyId { get; init; }
        public required ArrivalStepRule.ArrivalKind Kind { get; init; }

        /// <summary>The last evaluated validity, for the one-shot transition alarm. Null until judged.</summary>
        public bool? LastValid { get; set; }

        /// <summary>#952 — did the last sweep find this arrival's pass sitting on the ribbon's own edge (a
        /// course too short to reach it)? Kept for the same reason <see cref="LastValid"/> is: the moment it
        /// stops being true is a TRANSITION, and the transition is what lets the auto path length settle
        /// back onto the encounter it just found instead of re-deciding every 300 ms. Null until judged.</summary>
        public bool? LastPassWasOffTheRibbon { get; set; }
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

            // #1042 — AND NOT A BODY THE RIBBON MERELY BEGINS AT. With the scrub at zero such a pass sits at
            // delta-zero from the captain's finger and wins this pick outright over every real encounter
            // later on the line, which is how the button came to read "+ Add orbit at scrub (Neptune)" to a
            // ship thirty AU away and opening. See PassIsOnlyTheRibbonsBeginning: the row's ✓/✗ is untouched.
            if (PassIsOnlyTheRibbonsBeginning(pass))
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
        // used to draw is gone. #1042: the same front-edge test applies to it, or the fallback would hand
        // back the very offer the loop above just refused.
        if (best is null
            && kind == ArrivalStepRule.ArrivalKind.Orbit
            && _armablePass is { } armable
            && !PassIsOnlyTheRibbonsBeginning(armable))
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

    /// <summary>The arrive step's verdict, or null when there is no step, no projection yet — or (#952) when
    /// the plotted course stops short of the body and the "pass" the sweep returned is only the end of the
    /// ribbon. Null is already the whole UI's word for "cannot judge this", so the fabricated ✗ simply stops
    /// being spoken; <see cref="ArriveRibbonIsTooShort"/> supplies the sentence that replaces it.</summary>
    private ArrivalStepRule.ArrivalCheck? ArriveCheck() =>
        _arrive is null || ArriveRibbonIsTooShort() ? null : CheckArrival(_arrive.Kind, _arrive.BodyId);

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

        // #952 — THE COURSE IS TOO SHORT TO JUDGE THIS ARRIVAL. Not a verdict, and above all not an alarm:
        // the pass the sweep returned is the ribbon's own edge. The moment it stops being the edge — the
        // reach for the cap found the real encounter — is a TRANSITION, and on that one frame the auto path
        // length is asked to run again so it can settle back onto the encounter instead of holding the cap.
        bool offTheEnd = ArriveRibbonIsTooShort();
        if (_arrive.LastPassWasOffTheRibbon == true && !offTheEnd && _horizonChoice == "auto")
        {
            _horizonDirty = true;
        }

        _arrive.LastPassWasOffTheRibbon = offTheEnd;

        if (ArriveCheck() is not { } check)
        {
            return; // no projection yet, or a course too short to judge — judge nothing rather than cry wolf
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
        // #952: a course that stops short of the body has NOTHING here to quote — the sweep's pass is where
        // the ribbon ended, and the distance, the price and the countdown are all read off it. Every one of
        // them goes to an em dash together, or the row would keep three fabricated numbers on the glance line
        // while its own sentence underneath says it has not judged anything.
        bool tooShort = ArriveRibbonIsTooShort();
        string est = tooShort ? "—" : ArriveEstPulses(step.Kind, step.BodyId).ToString(CultureInfo.InvariantCulture);
        string when = !tooShort && ArrivePassFor(step.BodyId) is { } pass && pass.SimTime > SimTime
            ? $"in {FormatDuration(pass.SimTime - SimTime)}"
            : tooShort ? "—" : "now";
        string dist = !tooShort && ArrivePassFor(step.BodyId) is { } p2 ? FormatDistance(p2.Distance) : "—";
        return $"{(step.Kind == ArrivalStepRule.ArrivalKind.Dock ? "⚓" : "🛰")} {verb} {body} · pass {dist} · ≈{est} p · {when}";
    }
}
