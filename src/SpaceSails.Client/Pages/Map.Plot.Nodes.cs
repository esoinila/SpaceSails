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
        _samples = _simulator!.ProjectAdaptive(_ship, _plan, CurrentPlotHorizonSeconds, maxTimeStep: 3 * 3600, maxSamples: 8000);
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
        _plan = new ManeuverPlan(
            _planNodes.Where(n => !n.Stale)
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
        double t = Math.Max(Math.Floor(ScrubTime), Math.Floor(_ship.SimTime) + 60);
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
        if (node.Mode == BurnMode.Vector)
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

        Vector2d ghost = SamplePositionAt(simTime);
        string? best = null;
        double bestHill = double.MaxValue;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.ParentId is null || body.Kind == BodyKind.Station)
            {
                continue;
            }
            CelestialBody? parent = _ephemeris.Bodies.FirstOrDefault(b => b.Id == body.ParentId);
            if (parent is null)
            {
                continue;
            }
            double hill = OrbitRule.HillRadius(body, parent.Mu);
            if (hill <= 0 || hill >= bestHill)
            {
                continue;
            }
            if ((ghost - _ephemeris.Position(body.Id, simTime)).Length < hill)
            {
                (bestHill, best) = (hill, body.Id);
            }
        }
        return best;
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
        if (!int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            return;
        }

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

    // Re-time to the scrub time. Un-stales the node (plan §4: re-timing repairs it).
    private void RetimeToScrub(PlanNode node)
    {
        // Same clamp as AddBurnAtScrub: a past scrub re-times to one minute out, never errors.
        double t = Math.Max(Math.Floor(ScrubTime), Math.Floor(_ship.SimTime) + 60);

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
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Executed && !node.Stale && node.SimTime < _ship.SimTime)
            {
                node.Executed = true;
                firedPulses += node.Pulses;
            }
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
