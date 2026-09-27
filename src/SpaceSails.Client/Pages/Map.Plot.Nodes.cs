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
}
