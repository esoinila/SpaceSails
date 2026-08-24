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

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — plotting mode itself: staling the future, entering and leaving the mode, rebuilding the plan the sim flies, and every edit to a node — add, retime, delete, select, the ± factor and X-Pilot heading burns, and the fired-node accounting.
public partial class Map
{

    private void StaleFutureNodes()
    {
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && !node.Executed && node.SimTime > _ship.SimTime)
            {
                node.Stale = true;
            }
        }
        RebuildPlan();
        ReprojectTrajectory();
    }

    private void ReprojectTrajectory()
    {
        // #955 NAV-1 — THE DRAWN FUTURE STARTS WHERE THE PLAN STARTS. For a free-flying ship that is the ship
        // herself (PlanStartState returns _ship unchanged, so nothing about an ordinary plot moved); for a
        // clamped ship whose plan begins with a cast off it is the state the clamp is about to hand over —
        // the berth plus the berth's own shove. Everything judged off this ribbon (the passes, the arrival
        // row's OK/NOT bit, #969's arm-time rehearsal) is therefore computed FROM THE BERTH ONWARD, which is
        // the owner's "the plotted path starts with the cast-off + clearance" in one argument.
        _samples = _simulator!.ProjectAdaptive(PlanStartState(), _plan, CurrentPlotHorizonSeconds, maxTimeStep: 3 * 3600, maxSamples: 8000);
        _nextProjectionSimTime = _ship.SimTime + ProjectionRefreshSimSeconds;
        _passDirty = true;
        _lastReprojectMs = _lastTimestampMs ?? 0;
    }

    // ---- Plotting mode ----

    private void TogglePlotMode()
    {
        if (PlotMode)
        {
            ExitPlotMode();
        }
        else
        {
            EnterPlotMode();
        }
    }

    private void EnterPlotMode()
    {
        StopSkip(); // #172: plotting is the captain taking the helm — stop skipping (and don't save a
                    // cranked warp as _warpBeforePlot). StopSkip drops Warp to 1× before we snapshot it.
        _warpBeforePlot = Warp;
        PlotMode = true;
        Paused = true;
        _scrubOffsetSeconds = 0;
        ReprojectTrajectory();
    }

    private void ExitPlotMode()
    {
        PlotMode = false;
        Paused = false;
        Warp = _warpBeforePlot <= 0 ? 1 : _warpBeforePlot;
        ReprojectTrajectory();
    }

    // Rebuild the immutable plan the sim executes from the non-stale nodes. Past/executed nodes are
    // harmless to include (their firing window has passed), so the same plan serves projection too.
    private void RebuildPlan()
    {
        // #955 NAV-1: the UNDOCK row is a step, not an impulse — there is no delta-v in a clamp letting go, and
        // handing one to the maneuver plan would have the integrator "fire" a burn of zero pulses at the berth.
        // It is flown by the frame loop instead (RunTheCastOffStep), because it is the one step that changes
        // which branch the loop takes. The CLEARANCE row is a genuine Vector burn and goes in like any other,
        // which is exactly why it was built as one: the existing executor fires it and the existing projection
        // draws it, with no new machinery on either side.
        _plan = new ManeuverPlan(
            _planNodes.Where(n => !n.Stale && n.Kind != PlanStepKind.Undock)
                      .Select(n => new ManeuverNode(n.SimTime, n.Action, n.Pulses, Fine: false, Percent: n.Percent,
                                                    Mode: n.Mode, HeadingDegrees: n.HeadingDegrees)));
    }

    // Reaction-mass claimed by still-pending (non-stale, future) nodes.
    private int PlannedPulseTotal()
    {
        int total = 0;
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && node.SimTime > _ship.SimTime)
            {
                total += node.Pulses;
            }
        }

        return total;
    }

    private void AddBurnAtScrub()
    {
        // Never refuse over the scrub sitting in the past (owner: the control must never be
        // in a position that blocks the action) — clamp to one minute out and proceed.
        double t = Math.Max(Math.Floor(ScrubTime), NodeEpochFloor());
        if (PlannedPulseTotal() + 1 > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass");
            return;
        }

        // #838 · a burn is born in the vector view, aimed FORWARD in the ghost's frame at its own epoch —
        // which flies identically to the old ± Accelerate it replaces (a Vector pulse down the velocity
        // vector IS a Factor Accelerate; see ManeuverPlan) but is now a heading the four quick selects and
        // the free aim can both speak about.
        var newNode = new PlanNode { SimTime = t, Action = ManeuverAction.Accelerate, Pulses = 1, Mode = BurnMode.Vector };
        AimNode(newNode, NodeDirection.Forward);
        _planNodes.Add(newNode);
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();

        // PR-D2: a freshly added burn opens its own editor (accordion) so its controls are right there.
        _openEditor = FlightEditorKind.Burn;
        _selectedPlanNode = newNode;

        // Tutorial step 2: first plan node added while a pod is selected.
        if (_selectedTargetId is not null && FindNpc(_selectedTargetId) is { Ship.IsPod: true })
        {
            AdvanceTutorial(1);
        }
    }

    // #838 — SetAction (the planner's ± prograde/retrograde pair) is GONE with the ruling: nothing else
    // called it. A node's Action still rides along for the auto-plot path's Factor nodes and is what
    // EnsureVectorPlanning reads to convert one, but the planner no longer offers a control that sets it.

    // #838 · THE PLANNER SPEAKS VECTOR, ALWAYS. The ± factor burn is reflex flying's idiom and left the
    // maneuver-node planner with the owner's ruling; a node opened for editing is therefore converted to
    // the Vector burn that flies EXACTLY the same course — prograde for the old +, retrograde for the old
    // − (a Vector pulse along ±v scales the speed by the same Percent, proven in Core) — so nothing about
    // the plotted trajectory changes, only the language the panel edits it in. Legacy nodes reach the
    // planner from the auto-plot path, which still lays Factor nodes.
    private void EnsureVectorPlanning(PlanNode node)
    {
        // #955 NAV-1: a departure step is not a burn the planner aims. The clamp has no heading, and the
        // clearance's heading is the berthing arm's, re-solved by ResizeClearance — not something a quick
        // select may quietly overwrite.
        if (node.Kind != PlanStepKind.Burn || node.Mode == BurnMode.Vector)
        {
            return;
        }

        node.Mode = BurnMode.Vector;
        AimNode(node, node.Action == ManeuverAction.Accelerate ? NodeDirection.Forward : NodeDirection.Back);
        RebuildPlan();
        ReprojectTrajectory();
    }

    // #838 · a quick select: point this node's burn along one of the four trajectory-relative directions,
    // solved in the GHOST'S frame at the node's own epoch. Nothing is cached — re-time the node and press
    // again and the ribbon is re-read at the new time, because by then the course has moved on.
    private void SetNodeDirection(PlanNode node, NodeDirection direction)
    {
        node.Mode = BurnMode.Vector;
        AimNode(node, direction);
        RebuildPlan();
        ReprojectTrajectory();
    }

    // #926 · FLYING WITH THE MOUSE ALONE. Owner (2026-08-17, playing): "Let's add the plus and minus
    // buttons to the burn scrub angle … the vector rotation is good for flying with mouse alone, without
    // inputting … like ±5 degrees." Turn THIS node's aim five degrees, off whatever it points at now — so
    // pressing FORWARD then +5° lands five degrees off the ghost's prograde, and the ribbon re-solves the
    // way any heading edit does. Distinct from the reflex-flying idiom #916 sent out of this panel: that
    // one scaled the ship's speed by a factor, this one is an angle, in the vector view's own language.
    private void NudgeNodeAim(PlanNode node, int sign)
    {
        node.Mode = BurnMode.Vector;
        node.HeadingDegrees = NodeFrame.Nudge(node.HeadingDegrees, sign);
        RebuildPlan();
        ReprojectTrajectory();
    }

    // The aim itself, without the rebuild — one call into Core, handed the ghost's own state at the node.
    private void AimNode(PlanNode node, NodeDirection direction) =>
        node.HeadingDegrees = NodeFrame.HeadingAt(
            direction, _samples, node.SimTime,
            PlanningPrimaryPositionAt(node.SimTime), _ship.Position, _ship.Velocity);

    // Is this node currently pointed along that quick select? Lights the matching button — and lets every
    // button go dark the moment the node is re-timed and the frame it was solved in has moved.
    private bool NodeAimedAlong(PlanNode node, NodeDirection direction) =>
        node.Mode == BurnMode.Vector && NodeFrame.PointsAlong(
            node.HeadingDegrees,
            NodeFrame.HeadingAt(direction, _samples, node.SimTime,
                                PlanningPrimaryPositionAt(node.SimTime), _ship.Position, _ship.Velocity));

    // #838 · which body "up" and "down" are measured from at a node's epoch: the captain's chosen plot
    // frame when he has one (#135/#143 — the frame is what the drawn plot MEANS, so it is also what the
    // radials mean), otherwise the innermost body whose Hill sphere holds the ghost at that instant, and
    // the Sun when nothing does. Whichever it is, the panel names it, so "up" is never a guess.
    private string? PlanningPrimaryIdAt(double simTime)
    {
        if (_ephemeris is null)
        {
            return null;
        }
        if (_plotFrameBodyId is not null && _ephemeris.Bodies.Any(b => b.Id == _plotFrameBodyId))
        {
            return _plotFrameBodyId;
        }

        // #926 — the innermost-Hill law moved to Core (TripFrame.PrimaryAt), unchanged, because the trip
        // frame needs the SAME reading of where the ghost really is. One law, two callers.
        return TripFrame.PrimaryAt(SamplePositionAt(simTime), _ephemeris, simTime);
    }

    private Vector2d PlanningPrimaryPositionAt(double simTime)
    {
        string? id = PlanningPrimaryIdAt(simTime);
        return id is null || _ephemeris is null ? Vector2d.Zero : _ephemeris.Position(id, simTime);
    }

    private string PlanningPrimaryName(double simTime) =>
        PlanningPrimaryIdAt(simTime) is { } id ? BodyName(id) : "the Sun";

    // World-space heading (degrees, 0° = +X, CCW) of the projected velocity at a plotted time — the
    // ghost's prograde, which is also what the rel/abs angle field reads its zero from.
    private double HeadingAlongCourseAt(double simTime) => NodeFrame.Prograde(SampledVelocityAt(simTime));

    private void SetHeading(PlanNode node, ChangeEventArgs e)
    {
        string raw = (e.Value?.ToString() ?? string.Empty).Replace(',', '.');
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double deg))
        {
            // #201: the field is ship-relative by default (0 ahead, +90 starboard, −90 port). Map it
            // back to the world heading the physics burns along; absolute mode types the world angle direct.
            node.HeadingDegrees = _burnAngleAbsolute
                ? WrapDegrees(deg)
                : BurnHeadingConvention.RelativeToWorld(HeadingAlongCourseAt(node.SimTime), deg);
            RebuildPlan();
            ReprojectTrajectory();
        }
    }

    // #838 — NudgeHeading (the ±15° ↺/↻ pair) is GONE, not moved: nothing else in the game called it.
    // Reflex flying's plus/minus is a different control entirely — the live +/−/arrow keys in
    // Map.Sim.Keys, which scale the ship's velocity right now — and it is untouched by this issue.

    private static double WrapDegrees(double deg)
    {
        deg %= 360;
        return deg < 0 ? deg + 360 : deg;
    }

    private void SetPulses(PlanNode node, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            ApplyPulses(node, value);
        }
    }

    // #937 · THE ONE PLACE A BURN'S MAGNITUDE CHANGES. Owner (2026-08-18): "Now writing a new number there
    // I have to switch to another input to see what the effect of numeric change is." The answer is nudge
    // buttons that re-solve under the press — and the way to keep a button and a typed number telling the
    // truth about the same burn is to give them ONE act, not two. The typed field parses and calls here;
    // a nudge steps in Core and calls here; the clamp, the reaction-mass budget and the re-solve are
    // written once, so a button can never reach a magnitude the field would have refused.
    private void ApplyPulses(PlanNode node, int value)
    {
        value = Math.Clamp(value, MinNodePulses, MaxNodePulses);
        if (value == node.Pulses)
        {
            return;
        }

        // Budget check counts this node's new pulses in place of its old ones.
        int othersTotal = PlannedPulseTotal();
        if (!node.Stale && node.SimTime > _ship.SimTime)
        {
            othersTotal -= node.Pulses;
        }
        if (othersTotal + value > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass");
            return;
        }

        node.Pulses = value;
        RebuildPlan();
        ReprojectTrajectory();
    }

    // #937 · one press on a magnitude button: Core picks the step (1 pulse fine, 5 coarse) and clamps it
    // into the field's own bounds, then the SAME act the typed field uses applies it. No second solve path.
    private void NudgeNodePulses(PlanNode node, int sign, bool coarse) =>
        ApplyPulses(node, NodeFrame.NudgeMagnitude(node.Pulses, sign, coarse, MinNodePulses, MaxNodePulses));

    // #937 · one press on a time button: an hour, or a day, along the course. Never earlier than the floor
    // every other node-timing path in this file already honours (one minute out from now) — so the control
    // clamps rather than refusing, and the captain can lean on it. Re-sorting is how a node that overtakes
    // its neighbour is handled: a burn dragged past another burn is a legal plan, just a re-ordered one.
    private void NudgeNodeEpoch(PlanNode node, int sign, bool coarse)
    {
        node.SimTime = NodeFrame.NudgeEpoch(node.SimTime, sign, coarse, NodeEpochFloor());
        ResizeClearance(node);   // #955: a departure re-timed is a departure re-solved — the berth has moved
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
    }

    // The earliest instant a plotted node may sit at: one minute out from now. AddBurnAtScrub, the retime
    // button and the epoch nudges all read it here, so there is one floor rather than three copies of it.
    private double NodeEpochFloor() => Math.Floor(_ship.SimTime) + 60;

    // Re-time to the scrub time. Un-stales the node (plan §4: re-timing repairs it).
    private void RetimeToScrub(PlanNode node)
    {
        // Same clamp as AddBurnAtScrub: a past scrub re-times to one minute out, never errors.
        double t = Math.Max(Math.Floor(ScrubTime), NodeEpochFloor());

        // If it was stale/executed it re-enters the budget; check it fits.
        int othersTotal = PlannedPulseTotal();
        bool wasPending = !node.Stale && node.SimTime > _ship.SimTime;
        if (wasPending)
        {
            othersTotal -= node.Pulses;
        }
        if (othersTotal + node.Pulses > _reactionMassPulses)
        {
            ShowPulseMessage("Not enough reaction mass");
            return;
        }

        node.SimTime = t;
        node.Stale = false;
        node.Executed = false;
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
    }

    private void SetPercent(PlanNode node, ChangeEventArgs e)
    {
        // Accept either decimal separator: the field renders with an invariant '.', but a user on a
        // comma-locale keyboard will type ',' — normalize before the invariant parse.
        string raw = (e.Value?.ToString() ?? string.Empty).Replace(',', '.');
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double p))
        {
            node.Percent = Math.Clamp(p, 0.01, 50);
            RebuildPlan();
            ReprojectTrajectory();
        }
    }

    private PlanNode? _selectedPlanNode;

    // M24: planets are clickable too — a small menu offers "set destination", so the orbit
    // assist coaches the approach to the body the captain MEANS, not whichever is nearest.
    // Click a thrust node on the ribbon to select it: highlights its row and jumps the scrub
    // to its time (owner request, M16). Returns true when a node was hit.
    private bool TrySelectNodeAt(double clientX, double clientY)
    {
        if (!PlotMode || _planNodes.Count == 0 || _samples.Count == 0)
        {
            return false;
        }

        const double hitRadiusPx = 14;
        PlanNode? best = null;
        double bestSq = hitRadiusPx * hitRadiusPx;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Executed)
            {
                continue;
            }

            // #143 — hit-test against where the marker is actually DRAWN (frame-transformed), else a
            // non-Sun frame makes every ribbon-node click miss. DrawNodeMarkers uses the same PlotFrame.
            (float nx, float ny) = _camera.WorldToScreen(PlotFrame(SamplePositionAt(node.SimTime), node.SimTime));
            double dx = clientX - nx, dy = clientY - ny;
            double d = dx * dx + dy * dy;
            if (d < bestSq)
            {
                bestSq = d;
                best = node;
            }
        }

        if (best is null)
        {
            return false;
        }

        // PR-D2: a ribbon-node click selects AND opens that step's editor — the map and the list are two
        // views of one plan, resolving to the same _selectedPlanNode + accordion state.
        _selectedPlanNode = best;
        _openEditor = FlightEditorKind.Burn;
        _scrubOffsetSeconds = Math.Max(0, best.SimTime - _ship.SimTime);
        EnsureVectorPlanning(best);   // #838: the panel that just opened plans in the vector view
        return true;
    }

    private void DeleteNode(PlanNode node)
    {
        // #989: the two departure rows are ONE act — one press laid them, one press takes them away. Routed
        // here as well as at the button so no other caller can ever leave half a departure standing (an ⚓
        // with no clearance drops her into the harbour's traffic; a 🚀 with no ⚓ thrusts against a clamp
        // that never let go), which is the shape the owner's second #989 screenshot caught.
        if (node.Kind != PlanStepKind.Burn)
        {
            RemoveTheDeparturePair();
            return;
        }

        _planNodes.Remove(node);
        // PR-D2: if the deleted step was the open one, collapse the accordion so nothing dangles.
        if (ReferenceEquals(node, _selectedPlanNode))
        {
            _selectedPlanNode = null;
            if (_openEditor == FlightEditorKind.Burn)
            {
                _openEditor = FlightEditorKind.None;
            }
        }
        RebuildPlan();
        ReprojectTrajectory();
    }

    private void SortNodes() => _planNodes.Sort((a, b) => a.SimTime.CompareTo(b.SimTime));

    // After live stepping, settle mass for any node whose firing window has passed. The window rule
    // in Simulator.Step fires each node once; this mirrors that once for the mass budget/HUD.
    private void AccountForFiredNodes()
    {
        int firedPulses = 0;
        int heldByTheClamp = 0;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Executed || node.Stale || node.SimTime >= _ship.SimTime)
            {
                continue;
            }

            // #955 NAV-1: the UNDOCK row is not billed here and is not retired here — the frame loop flies it
            // (RunTheCastOffStep) at the exact epoch, because it is the step that unclamps her, and a row
            // marked "done" by the accountant that the loop never ran would be a plan reporting a cast-off
            // that never happened.
            if (node.Kind == PlanStepKind.Undock)
            {
                continue;
            }

            // #955 NAV-1 · A CLAMPED SHIP FIRES NOTHING. Plotting from the berth is now allowed, and while she
            // is clamped the frame takes the clock-only branch: the integrator never runs and the maneuver
            // plan is never applied. A burn whose epoch slid past under the clamp therefore did NOT fire, and
            // billing it would be exactly the green number never asked of the world. Strike it instead, and
            // say so — re-time it (or cast off first) and it flies.
            if (_dockedHavenId is not null)
            {
                node.Stale = true;
                heldByTheClamp++;
                continue;
            }

            node.Executed = true;
            firedPulses += node.Pulses;
        }

        if (heldByTheClamp > 0)
        {
            RebuildPlan();
            ShowPulseMessage($"⚓ {DockNavLockTip} — {heldByTheClamp} plotted burn{(heldByTheClamp == 1 ? "" : "s")} struck; nothing fires from a berth.");
        }

        if (firedPulses > 0)
        {
            _reactionMassPulses = Math.Max(0, _reactionMassPulses - firedPulses);
            ShowPulseMessage($"Plan: {firedPulses} pulse{(firedPulses == 1 ? "" : "s")} fired");
        }

        // Spent burns clean themselves off the plot card (owner request): once a node's time
        // is past it either fired (Executed) or never will (Stale) — either way it's history.
        int removed = _planNodes.RemoveAll(n => n.SimTime < _ship.SimTime && (n.Executed || n.Stale));
        if (removed > 0 && _selectedPlanNode is { } sel && !_planNodes.Contains(sel))
        {
            _selectedPlanNode = null;
            if (_openEditor == FlightEditorKind.Burn)
            {
                _openEditor = FlightEditorKind.None; // PR-D2: the open step fired/expired — collapse it
            }
        }
    }

}
