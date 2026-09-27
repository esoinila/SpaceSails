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
/// #251 · PLOTTING MODE AND THE PLAN — staling and reprojecting the future nodes, entering and leaving
/// plot mode, rebuilding the plan, the pulse total, and adding a burn at the scrub.
///
/// <para>Split out of <c>Map.Plot.Nodes.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
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
        double horizon = CurrentPlotHorizonSeconds;
        _samples = _simulator!.ProjectAdaptive(PlanStartState(), _plan, horizon, maxTimeStep: 3 * 3600, maxSamples: 8000);
        _nextProjectionSimTime = _ship.SimTime + ProjectionRefreshSimSeconds;
        _passDirty = true;
        _lastReprojectMs = _lastTimestampMs ?? 0;

        // #1042 — AND THE SCRUB CANNOT POINT PAST THE END OF THE WORLD IT SCRUBS. The scrub slider's max IS
        // this horizon (Map.razor), but the bound value was only ever written by a hand on the control, so
        // every way the line gets SHORTER left the two disagreeing: drag Path length down, let auto settle
        // back off the projection cap once #952's reach found the encounter, or let a bound orbit cap the
        // horizon at one revolution — and the scrub still stood at the old far end, quietly resolving through
        // SamplePositionAtTime's "past the end → the last sample" fallback. The ghost ship, the ETA, the
        // node-epoch floor and every "at scrub" button then all agreed on an hour that is no longer drawn.
        // Clamped HERE, against the very number the projection was just asked for, so the control and its
        // value cannot tell two stories about how long the course is.
        _scrubOffsetSeconds = Math.Clamp(_scrubOffsetSeconds, 0, horizon);
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
}
