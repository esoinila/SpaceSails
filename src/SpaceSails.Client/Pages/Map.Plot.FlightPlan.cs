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
    // Live flight-plan steps: non-stale burns still on the board plus the armed insertion, if any.
    private int FlightPlanStepCount()
    {
        int n = _armedOrbitBodyId is not null ? 1 : 0;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale) n++;
        }
        return n;
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
            pending.Add(node);
        }
        pending.Sort((a, b) => a.SimTime.CompareTo(b.SimTime));
        foreach (PlanNode node in pending)
        {
            steps.Add(new FlightPlanStep(BurnStepLabel(node), $"in {FormatDuration(node.SimTime - SimTime)}", FlightStepState.Planned));
        }

        // The armed orbit-insert — named in plain language ("will it orbit or crash?" → it says so),
        // with the parked altitude when we know it, and the insertion's Armed/Active step state. Only
        // while still FLYING to it — once the park is kept there is no insertion step pending (Friday §0).
        if (_armedOrbitBodyId is not null && !_orbitKept)
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
