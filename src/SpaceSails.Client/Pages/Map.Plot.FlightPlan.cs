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

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — the flight-plan board: how many steps and which one is being flown, the glance line, the accordion, the NOW/next banner rows, and the burn epochs #261's skip test reads.
public partial class Map
{
    // #952 — THE PLAN ENDS SOMEWHERE, AND ONE ROW SAYS SO. When the arrive step names the very body the
    // autopilot is armed for, the two are ONE fact and must be ONE row (the owner's standing complaint about
    // parallel UIs for the same thing); the arrive row wins, because it is the one that carries the pass, the
    // ✓/✗ bit and the numbers. This predicate is the single place that decision is taken — the list, the
    // banner queue and the step counter all read it.
    private bool ArriveCoversArmed =>
        _arrive is not null && _armedOrbitBodyId is not null && _arrive.BodyId == _armedOrbitBodyId;

    // Live flight-plan steps: non-stale burns still on the board, the autopilot's own scheduled burns, the
    // armed insertion, and the arrival that ends the plan.
    private int FlightPlanStepCount()
    {
        int n = _armedOrbitBodyId is not null && !ArriveCoversArmed ? 1 : 0;
        if (_arrive is not null) n++;
        n += ScheduledAutopilotBurns().Count;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale) n++;
        }
        return n;
    }

    // #957 — THE AUTOPILOT'S OWN BURNS ARE STEPS TOO. The owner, after the autopilot finally accepted the
    // Roadstead: "it accepted the autopilot but it does [not] add it as navigation step to the list." The
    // armed transfer schedule (the #146 in-well arc, and since #957 the braking step laid instead of a
    // refusal) fires impulses at fixed epochs and had NO list presence at all — the plan the ship was
    // actually flying was partly invisible. These are the ones still ahead, in order.
    private IReadOnlyList<TransferPlanner.BurnStep> ScheduledAutopilotBurns()
    {
        if (_armedTransferSchedule is not { } schedule)
        {
            return [];
        }

        var ahead = new List<TransferPlanner.BurnStep>();
        for (int i = _armedTransferBurnsFired; i < schedule.Burns.Count; i++)
        {
            ahead.Add(schedule.Burns[i]);
        }
        return ahead;
    }

    // The glance line for one of those — priced with the same OrbitRule.PulsesFor kernel ApplyTransferBurn
    // spends with, so the row and the tank agree (#928: what it says is the CHARGED tenth).
    private string ScheduledBurnGlanceLine(TransferPlanner.BurnStep burn)
    {
        int raw = OrbitRule.PulsesFor(burn.DeltaV.Length, _ship.Velocity.Length);
        int charged = AutopilotRehearsal.ChargeForBurn(_armedSpentPulses, raw);
        string when = burn.SimTime <= SimTime ? "now" : $"in {FormatDuration(burn.SimTime - SimTime)}";
        return $"🛑 autopilot burn {ArrivalStepRule.FormatSpeed(burn.DeltaV.Length)} · ≈{charged} p · {when}";
    }

    // 1-based index of the step being worked now: the earliest pending burn, or the insertion when the
    // approach is being flown / no burns remain. (Executed burns are pruned from _planNodes, so the
    // count reflects what is still ahead rather than the original plan length.)
    private int FlightPlanCurrentStep()
    {
        int idx = 0;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Stale) continue;
            idx++;
            if (!node.Executed && node.SimTime > SimTime) return idx; // first pending burn
        }
        return Math.Max(1, idx + (_armedOrbitBodyId is not null ? 1 : 0));
    }

    // #955 NAV-1: the banner's label for a plotted step, whichever kind it is. TOTAL over PlanStepKind —
    // a step with no words is a step the captain cannot read, and TheCastOffIsAStepTests walks the enum to
    // prove none of them is missing one.
    private string PlanStepLabel(PlanNode node) => node.Kind switch
    {
        PlanStepKind.Burn => BurnStepLabel(node),
        PlanStepKind.Undock or PlanStepKind.ClearHarbour =>
            CastOffRule.StepLabel(node.Kind, HavenNameOf(node), node.Pulses),
        _ => throw new ArgumentOutOfRangeException(nameof(node), node.Kind, "no banner label for this step kind"),
    };

    // …and the collapsed glance line for the same row, by the same law.
    private string PlanStepGlanceLine(PlanNode node) => node.Kind switch
    {
        PlanStepKind.Burn => BurnGlanceLine(node),
        PlanStepKind.Undock or PlanStepKind.ClearHarbour => DepartureGlanceLine(node),
        _ => throw new ArgumentOutOfRangeException(nameof(node), node.Kind, "no glance line for this step kind"),
    };

    private static string BurnStepLabel(PlanNode node)
    {
        string dir = node.Mode == BurnMode.Vector ? "✚"
            : node.Action == ManeuverAction.Accelerate ? "▲" : "▼";
        return $"burn {dir} {node.Pulses} p";
    }

    // PR-D2: the glanceable collapsed line for a burn step — type + direction + countdown, so the whole
    // trip reads top to bottom without opening anything (GeminiUINotes.md: "type + target + countdown").
    private string BurnGlanceLine(PlanNode node)
    {
        string arrow = node.Mode == BurnMode.Vector ? "✚"
            : node.Action == ManeuverAction.Accelerate ? "▲" : "▼";
        // #838: a burn pointed along one of the four quick selects says so in words — the glance line
        // reads "▲ UP", not "+90° rel", for the aim the captain actually pressed. Free aim (the
        // exception) still quotes its angle in the panel's own convention.
        // #201: mirror the input's convention — ship-relative by default ("+90° rel"), absolute on toggle.
        string dir = node.Mode == BurnMode.Vector && QuickSelectOf(node) is { } quick
            ? NodeFrame.Label(quick)
            : node.Mode == BurnMode.Vector
            ? (_burnAngleAbsolute
                ? $"{node.HeadingDegrees.ToString("0", CultureInfo.InvariantCulture)}°"
                : $"{BurnHeadingConvention.WorldToRelative(HeadingAlongCourseAt(node.SimTime), node.HeadingDegrees).ToString("+0;-0;0", CultureInfo.InvariantCulture)}° rel")
            : node.Action == ManeuverAction.Accelerate ? "prograde" : "retrograde";
        string when = node.Executed ? "fired"
            : node.SimTime <= SimTime ? "now"
            : $"in {FormatDuration(node.SimTime - SimTime)}";
        return $"burn {arrow} {node.Pulses} p → {dir} · {when}";
    }

    // #838 — which quick select, if any, this node is currently pointed along (solved fresh in the ghost's
    // frame at the node's epoch, same as the buttons). null = free aim, the exception the vector view keeps.
    private NodeDirection? QuickSelectOf(PlanNode node)
    {
        foreach (NodeDirection direction in NodeFrame.QuickSelects)
        {
            if (NodeAimedAlong(node, direction))
            {
                return direction;
            }
        }
        return null;
    }

    // PR-D2: the flight-plan accordion. Clicking a step's line (or its ribbon node) expands its editor;
    // opening one collapses any other — exactly one editor is open at a time. _selectedPlanNode is the
    // burn's identity, shared by the list click and the map-node pick so both resolve to one selection.
    private void ToggleBurnEditor(PlanNode node)
    {
        if (_openEditor == FlightEditorKind.Burn && ReferenceEquals(_selectedPlanNode, node))
        {
            _openEditor = FlightEditorKind.None;
            _selectedPlanNode = null;
        }
        else
        {
            _openEditor = FlightEditorKind.Burn;
            _selectedPlanNode = node;
            EnsureVectorPlanning(node);   // #838: the planner's one surface is the vector view
        }
    }

    /// <summary>#992 · The accordion state the last paint already scrolled to. Compared rather than flagged
    /// ON PURPOSE: an editor opens from five places — this toggle, a fresh burn (<c>AddBurnAtScrub</c>), the
    /// cast-off pair, a click on a ribbon node, and an arrival — and a flag set at four of them is a flag
    /// somebody forgets at the fifth. The accordion holds exactly one open editor (PR-D2), so "which one is
    /// open" IS the whole state, and asking whether it changed cannot be forgotten anywhere.</summary>
    private FlightEditorKind _scrolledEditorKind = FlightEditorKind.None;
    private PlanNode? _scrolledEditorNode;

    /// <summary>#992 · Bring the open step's row inside the step list, once the editor is really in the DOM.
    ///
    /// <para>The list scrolls now (the panel is bound to the window), so a step opened near the bottom of a
    /// long plan can unfold below the LIST's fold — the owner's sighting one scroller further in. A selector
    /// rather than an ElementReference: exactly one row carries <c>map-plan-step-open</c> at a time, and the
    /// class the CSS already keys off is the one honest handle on it.</para></summary>
    private void BringOpenStepIntoViewIfAsked()
    {
        if (_openEditor == _scrolledEditorKind && ReferenceEquals(_selectedPlanNode, _scrolledEditorNode))
        {
            return;
        }

        _scrolledEditorKind = _openEditor;
        _scrolledEditorNode = _selectedPlanNode;
        if (_openEditor != FlightEditorKind.None)
        {
            RendererInterop.ScrollIntoView(".map-plan-step-open");
        }
    }

    // The NOW / next readout AND the full banner row list, built through the shared Core helper so the
    // banner, the Nav header, and the desk chips can never contradict (#159/#184). The queue below NOW
    // names every step still ahead top to bottom: each pending burn in time order, then the armed
    // orbit-insert — so the approach and the orbit step are SEPARATE, plain-language rows (#171/#173).
    private FlightPlanStatus FlightNowNext()
    {
        var steps = new List<FlightPlanStep>();

        // Pending burns, earliest first.
        var pending = new List<PlanNode>();
        foreach (PlanNode node in _planNodes)
        {
            if (node.Stale || node.Executed || node.SimTime <= SimTime) continue;
            // #989 · NO STEP IS SAID TWICE. The owner: "there is something wonky with deletion of cast off
            // events also… there are two in a row now" — NOW read "casting off from The Red Eye in 0 h" and
            // NEXT read "⚓ cast off from The Red Eye in 0 h". Not a stale queue (NOW is derived from THIS
            // list, and always was): the same live row, spoken from two slots. The row whose countdown NOW
            // is already carrying is dropped here, so NEXT becomes the clearance — which is the honest
            // answer to "what does she do after the clamp lets go".
            if (NowLineCarriesTheCastOff(node)) continue;
            pending.Add(node);
        }
        pending.Sort((a, b) => a.SimTime.CompareTo(b.SimTime));
        foreach (PlanNode node in pending)
        {
            steps.Add(new FlightPlanStep(PlanStepLabel(node), $"in {FormatDuration(node.SimTime - SimTime)}", FlightStepState.Planned));
        }

        // #957: the autopilot's own scheduled burns — the in-well transfer arc, or the braking step it laid
        // instead of refusing. They fire at fixed epochs whether or not anything names them, so they are
        // named.
        foreach (TransferPlanner.BurnStep burn in ScheduledAutopilotBurns())
        {
            steps.Add(new FlightPlanStep(
                ScheduledBurnGlanceLine(burn),
                burn.SimTime > SimTime ? $"in {FormatDuration(burn.SimTime - SimTime)}" : "now",
                FlightStepState.Armed));
        }

        // The armed orbit-insert — named in plain language ("will it orbit or crash?" → it says so),
        // with the parked altitude when we know it, and the insertion's Armed/Active step state. Only
        // while still FLYING to it — once the park is kept there is no insertion step pending (Friday §0).
        if (_armedOrbitBodyId is not null && !_orbitKept && !ArriveCoversArmed)
        {
            string eta = ArmedInsertionSimTime is { } t ? $"in {FormatDuration(t - SimTime)}" : "at window";
            // #204: a μ=0 station is never orbit-inserted — the plan's final step is the ⚓ dock. For an
            // honest errand the autopilot clamps itself ("⚓ auto-dock"); a hostile-flagged run keeps the
            // captain's-word grammar ("⚓ Dock — your call").
            string label = BodyById(_armedOrbitBodyId) is { } armed && IsDockableHaven(armed)
                ? (AutoDockHonest(armed)
                    ? $"⚓ auto-dock at {armed.Name}"
                    : $"⚓ Dock at {armed.Name} — your call in the envelope")
                : InsertStepLabel();
            steps.Add(new FlightPlanStep(
                label, eta, FlightPlanStatusBuilder.InsertionState(AutopilotFlyingApproach)));
        }

        // #952 THE CHERRY ON TOP: the arrival that ends the plan, carried into the banner queue with its
        // valid/invalid bit. An INVALID arrival speaks its whole sentence here — distance and relative
        // speed against the real thresholds — because the banner is where a captain who is not looking at
        // the Nav desk will read that his plan no longer ends safely.
        if (_arrive is { } arrive)
        {
            ArrivalStepRule.ArrivalCheck? check = ArriveCheck();
            // #969: an arrival ARMED AT PLAN TIME is the last step of a finished trip, and the banner says
            // so in those words — "🛰 arrive Mars — the autopilot inserts", "in 9 mo". The ✓/✗ verdict stays
            // the row's business; up here the captain who is nowhere near the Nav desk wants to read WHO
            // finishes the trip, and that nothing is owed until then.
            string label = ArriveCoversArmed && ArmedArrivalStillAhead
                ? ArrivalStepRule.ArmedThenLabel(arrive.Kind, BodyName(arrive.BodyId))
                : check is { } c
                ? (c.Valid
                    ? $"{(arrive.Kind == ArrivalStepRule.ArrivalKind.Dock ? "⚓" : "🛰")} {ArrivalStepRule.Verb(arrive.Kind)} at {BodyName(arrive.BodyId)} ✓"
                    : ArrivalStepRule.Verdict(c))
                : $"{(arrive.Kind == ArrivalStepRule.ArrivalKind.Dock ? "⚓" : "🛰")} {ArrivalStepRule.Verb(arrive.Kind)} at {BodyName(arrive.BodyId)}";
            string arriveEta = ArrivePassFor(arrive.BodyId) is { } ap && ap.SimTime > SimTime
                ? $"in {FormatDuration(ap.SimTime - SimTime)}"
                : "at the pass";
            steps.Add(new FlightPlanStep(
                label,
                arriveEta,
                ArriveCoversArmed
                    ? FlightPlanStatusBuilder.InsertionState(AutopilotFlyingApproach)
                    : FlightStepState.Planned));
        }

        // Friday §0 (owner ruling): the kept-orbit NOW line, composed HERE and fed through main's
        // #190 HoldingLine seam — ONE code path in the builder, which slots it below Docked and above
        // every flying phase: "🛰 AUTOPILOT HOLDS THE ORBIT — <body>, <alt>, trim ≈N p/day", never
        // "you have the ship".
        // #220 / #203: quote the PARK altitude (the held value the keeper trims back to,
        // ParkingRadius − surface, "alt N km"), NOT the instantaneous body-relative radius — the kept
        // orbit's forced eccentricity makes that raw radius oscillate, so the snapshot disagreed with
        // itself (holding said "278 km" while Nearest said "217 km"). The live wobble stays on the
        // Nearest line; the holding line states the steady park the autopilot is keeping.
        string? holdingLine = null;
        if (_orbitKept && _armedOrbitBodyId is not null && _ephemeris is not null)
        {
            // The steady park the keeper trims back to: ParkingRadius − surface, "alt N km" (#203). The
            // kept body IS the bound body while keeping, so its Hill radius is UpdateOrbitedBody's cached
            // one. If (defensively) that isn't available, fall back to the instantaneous radius so the
            // NOW line never vanishes — but the park altitude is the value that stays steady on this line.
            CelestialBody? keptBody = BodyById(_armedOrbitBodyId);
            double parkAlt = keptBody is not null && _orbitedBodyId == _armedOrbitBodyId && _orbitedBodyHillRadius > 0
                ? OrbitRule.ParkingRadius(keptBody, _orbitedBodyHillRadius) - keptBody.BodyRadius
                : 0;
            string alt = parkAlt > 0
                ? FormatAltitude(parkAlt)
                : FormatDistance((_ship.Position - _ephemeris.Position(_armedOrbitBodyId, SimTime)).Length);
            string trim = _keepTrimPulsesPerDay > 0 ? $", trim ≈{_keepTrimPulsesPerDay} p/day" : "";
            holdingLine = $"🛰 AUTOPILOT HOLDS THE ORBIT — {BodyName(_armedOrbitBodyId)}, {alt}{trim}";
        }

        return FlightPlanStatusBuilder.Build(new FlightPlanInputs(
            Docked: NavLockedByDock,
            DockedHavenName: _havenName,
            // #955 NAV-1: she is clamped, but the plan is about to let go — say THAT, not "docked".
            CastOffLine: CastOffNowLine(),
            AutopilotArmed: _armedOrbitBodyId is not null,
            AutopilotFlyingApproach: AutopilotFlyingApproach,
            AutopilotBodyName: _armedOrbitBodyId is null ? null : BodyName(_armedOrbitBodyId),
            NextStepLabel: null,
            NextStepEta: null,
            // #147: the persistent "you have the ship" reason — a decline or a loud handback — so the
            // now line survives high warp instead of a 1.5-s toast. Only when not armed (else the ship
            // is flying again and the armed line wins).
            HandbackReason: _armedOrbitBodyId is null ? _autopilotStandDownReason : null,
            // Friday §0 priority lane: the kept-orbit NOW line, or null while still flying / manual.
            HoldingLine: holdingLine,
            UpcomingSteps: steps.Count > 0 ? steps : null));
    }

    // The armed-arrival step's plain-language label — routed through the Core one-voice vocabulary
    // (#203) so a real orbit reads "orbit-insert at Enceladus (alt 313 km)" while a μ≤0 dock haven
    // reads "dock envelope at Cinder Roost — slow to ≤8 km/s" (no phantom orbit, no "(0 km)"). The
    // parked ALTITUDE above the surface (not the orbital radius) is the captain-facing number.
    private string InsertStepLabel()
    {
        HarborClass harbor = HarborClassOf(_armedOrbitBodyId);
        string body = BodyName(_armedOrbitBodyId!);
        string? altitude = null;
        if (harbor == HarborClass.Orbit && OrbitInfo() is { } oi && oi.Body.Id == _armedOrbitBodyId)
        {
            double parkAlt = OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius;
            if (parkAlt > 0)
            {
                altitude = FormatAltitude(parkAlt);
            }
        }
        return HarborVocabulary.ArrivalStep(harbor, body, altitude);
    }

    // ===== #261 — the COMPUTED skip: reckon a jump-scale coast, never grind it =====
    // The freeze is the near-body fixed-1 s regime during a long arrival coast (warp auto-drops, the loop
    // grinds ~86_400 gravity steps per day). The law is already ruled — the void is computed, not slogged.
    // The clean, tested path is the long haul's own machinery: for a BALLISTIC leg in OPEN heliocentric
    // cruise, advance the ship along its closed-form conic to the target epoch, re-seed the world there, and
    // say the state. Otherwise (a burn inside the leg, or the ship deep in a well where the heliocentric
    // conic is a lie against the n-body integrator) fall back to chunked integration that PAINTS between
    // chunks — never a dead frame. Correctness beats elegance: closed form only where it is honest.

    // The upcoming burn epochs the ballistic test reads — the same plotted-node + armed-transfer sources
    // NextSkippableEvent's Burn candidate is built from, so the two agree on where impulses fire.
    private IEnumerable<double> UpcomingBurnEpochs()
    {
        double now = SimTime;
        foreach (PlanNode node in _planNodes)
        {
            // #955 NAV-1: the UNDOCK row carries no impulse but it is still a decision point the skip must
            // not leap over — it is where a clamped ship becomes a flying one. Reported here with the burns
            // so the one list of "epochs that must not be skipped" stays one list.
            if (!node.Stale && !node.Executed && node.SimTime > now)
            {
                yield return node.SimTime;
            }
        }

        if (_armedTransferSchedule is { } sch)
        {
            for (int i = _armedTransferBurnsFired; i < sch.Burns.Count; i++)
            {
                yield return sch.Burns[i].SimTime;
            }
        }
    }
}
