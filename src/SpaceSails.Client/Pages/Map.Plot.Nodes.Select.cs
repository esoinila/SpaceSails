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
/// #251 · PICKING, DELETING AND FIRING NODES — selecting a node (or a body) at a click, deleting and sorting,
/// a node's thrust direction, and accounting for the nodes that fired.
///
/// <para>Split out of <c>Map.Plot.Nodes.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. <c>_selectedPlanNode</c> stays in the opening file with every other
/// field of the family.</para>
/// </summary>
public partial class Map
{
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

    /// <summary>#167 - which way a retired node PUSHED her, in world space, for the flame off her stern.
    /// A Vector node burns along its own world heading (ManeuverPlan's X-Pilot burn, 0 deg = +X, CCW); a
    /// Factor node scales the velocity's magnitude without turning it, so it pushes along her track and a
    /// Decelerate pushes back down it. Direction only - BurnFired never reads the length.</summary>
    private Vector2d ThrustDirectionOf(PlanNode node)
    {
        if (node.Mode == BurnMode.Vector)
        {
            double radians = node.HeadingDegrees * Math.PI / 180.0;
            return new Vector2d(Math.Cos(radians), Math.Sin(radians));
        }

        return node.Action == ManeuverAction.Accelerate ? _ship.Velocity : -_ship.Velocity;
    }

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
            // #167 BURN KIND 2/9 - THE PLOTTED NODE. Retiring a node was pure book-keeping: the integrator
            // had already applied the impulse inside Simulator.Step and this loop only settled the mass, so
            // the one burn the captain PLANNED was the one burn that showed him nothing. Each node is its
            // own burn and gets its own flame; nodes are minutes apart in any plan a hand wrote.
            BurnFired(node.Pulses, ThrustDirectionOf(node));
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
