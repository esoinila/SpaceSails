using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — THE CAST OFF AS A STEP
// (#955 NAV-1): the two rows that let a flight plan begin at the berth.
//
// Owner's test story for the unified nav list (2026-08-23): "plan while docked, then the plan starts with an
// undock step recorded topmost in the nav-burn list, then safe-harbour out-thrust to clear the vicinity of
// the station, then the actual burns, then the autopilot approach step, then the dock step."
//
// #965 gave the plan its END (the ARRIVE step) and #969 made arming that end a PLAN-TIME promise — but both
// of them start from wherever the ship already is, and a clamped ship's nav is locked (RejectNavWhileDocked),
// so the one place a captain actually plans a voyage from — the bar stool at his own berth — was the one
// place he could not. This file is the other end of the list: ⚓ Undock and 🚀 Clear the harbour, laid in one
// press at the top of the plan, shaped like every other row, and flown by the same loop that fires burns.
//
// THE SHAPE OF THE FIX, in one paragraph: the clearance is a real plotted node (BurnMode.Vector, aimed
// straight out along the berthing arm, sized by Core's CastOffRule off DockRule's own envelope), so the
// existing executor — Simulator + ManeuverPlan — fires it with no new machinery and the plotted ribbon
// already draws the course it produces. The undock is the one step that cannot be a burn, because it changes
// which BRANCH the frame loop takes: while she is clamped the integrator never runs and the plan is never
// applied. So it is landed on exactly, in ConsumeTheAccumulator, the same way #146 lands on a transfer burn
// epoch — and from the instant it fires the rest of the plan is an ordinary flight.
public partial class Map
{
    /// <summary>The gap between the clamp letting go and the out-thrust firing: the same one minute the
    /// plan's own node floor uses, so the two rows read as one act while still being two steps the loop can
    /// land on separately.</summary>
    private const double CastOffGapSeconds = 60;

    // ===== Composing the pair =====

    /// <summary>
    /// <b>⚓ + Cast off</b> — one press, two rows. A cast-off that leaves the ship drifting in the harbour's
    /// traffic is not a cast-off, so the clamp release and the out-thrust are laid together and always in
    /// that order; the captain can still remove either afterwards, and the list will say what he has left.
    ///
    /// <para>They go to the TOP: at row 1 when the plan is empty or the ship is clamped (the story's
    /// "recorded topmost"), which for a clamped ship is the only honest place — nothing in the plan can
    /// happen before the clamp lets go.</para>
    ///
    /// <para>#989 · <b>AND THEY GO AT THE SCRUB.</b> Owner, docked at The Red Eye with the scrub 33 h out:
    /// <i>"Cast off time says in zero hours here even though the scrub is in 33 hours?"</i> He is SCHEDULING
    /// a departure, not requesting one now — so the undock takes the scrub epoch, by the same
    /// <c>Math.Max(floor(scrub), NodeEpochFloor())</c> clamp "+ Add burn at scrub" uses, and the clearance
    /// keeps its gap behind it. A scrub sitting at now still departs immediately: that reading became the
    /// special case instead of the rule.</para>
    /// </summary>
    private void AddCastOffAtTop()
    {
        if (_dockedHavenId is not { } haven)
        {
            ShowPulseMessage("⚓ Nothing to cast off from — she's already under way.");
            return;
        }

        // The undock sits AT THE SCRUB — the captain's finger on the plan — clamped to the plan's own floor
        // (one minute out, the floor every other node-timing path in this file honours) so a scrub in the
        // past schedules a departure now rather than refusing. The clearance keeps one floor-width behind it,
        // so the loop has a step in which to notice the clamp is gone before the burn is asked to fire.
        double undockAt = Math.Max(Math.Floor(ScrubTime), NodeEpochFloor());
        double clearAt = undockAt + CastOffGapSeconds;

        // #989 · ONE PAIR, EVER — and refused by the plan's own GRAMMAR, not by a button's private opinion.
        // The owner: "2 cast-off in sequence sounds kind of silly — we should have some logic check." The
        // check is CastOffRule.CheckShape, asked here about the plan this press WOULD make; the very same
        // function judges the board every cadence (RefreshPlanShapeValidity), so the refusal and the alarm
        // can never hold two opinions about what a legal plan looks like.
        CastOffRule.PlanShapeFault fault = CastOffRule.CheckShape(
            LiveStepKinds(alsoAt: [(undockAt, PlanStepKind.Undock), (clearAt, PlanStepKind.ClearHarbour)]),
            clamped: true);
        if (fault != CastOffRule.PlanShapeFault.None)
        {
            ShowPulseMessage(CastOffRule.ShapeComplaint(fault));
            return;
        }

        var undock = new PlanNode
        {
            Kind = PlanStepKind.Undock,
            HavenId = haven,
            SimTime = undockAt,
            Pulses = 0,                 // the arm costs nothing; the berth's shove is free
            Mode = BurnMode.Vector,
        };

        var clear = new PlanNode
        {
            Kind = PlanStepKind.ClearHarbour,
            HavenId = haven,
            SimTime = clearAt,
            Mode = BurnMode.Vector,
            Percent = CastOffRule.PulsePercent,
            Pulses = 1,
        };

        _planNodes.Insert(0, undock);
        _planNodes.Insert(1, clear);
        ResizeClearance(clear);         // sizes AND aims it off the live harbour
        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();

        _openEditor = FlightEditorKind.Burn;   // the freshly added step opens its own editor (PR-D2 idiom)
        _selectedPlanNode = clear;

        ShowPulseMessage(
            $"⚓ Cast off laid at the top of the plan — the clamp lets go at {BodyName(haven)} "
            + $"{(undockAt - SimTime <= CastOffGapSeconds ? "now" : $"in {FormatDuration(undockAt - SimTime)}")}, "
            + $"then ≈{clear.Pulses} p carries her clear of the harbour.");
    }

    // ===== #989 — the plan's shape, read the one way =====

    /// <summary>
    /// The plan's LIVE step kinds, in flight order — the list <see cref="CastOffRule.CheckShape"/> judges.
    /// Struck and flown rows are left out on purpose: a departure already behind her is not a second
    /// departure, and a struck row is not a step. <paramref name="alsoAt"/> lets a press ask the law about
    /// the plan it WOULD make before it makes it, at the epochs it would use — which is why the button's
    /// refusal and the board's alarm are one rule and not two.
    /// </summary>
    private IReadOnlyList<PlanStepKind> LiveStepKinds(
        IReadOnlyList<(double SimTime, PlanStepKind Kind)>? alsoAt = null)
    {
        var live = new List<(double SimTime, PlanStepKind Kind)>(_planNodes.Count + 2);
        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && !node.Executed)
            {
                live.Add((node.SimTime, node.Kind));
            }
        }
        if (alsoAt is not null)
        {
            live.AddRange(alsoAt);
        }

        live.Sort((a, b) => a.SimTime.CompareTo(b.SimTime));
        var kinds = new List<PlanStepKind>(live.Count);
        foreach ((double _, PlanStepKind kind) in live)
        {
            kinds.Add(kind);
        }
        return kinds;
    }

    /// <summary>The fault standing on the board this instant, if any — the law asked about what IS.</summary>
    private CastOffRule.PlanShapeFault PlanShapeFaultNow() =>
        CastOffRule.CheckShape(LiveStepKinds(), clamped: _dockedHavenId is not null);

    // The one-shot wake-up call for a plan whose SHAPE went bad — the #965 machinery, verbatim, because a
    // plan that cannot be flown as written is the same emergency as a plan that no longer ends safely: the
    // captain may be asleep at warp and nothing else on screen would tell him. Latched on the transition
    // (ArrivalStepRule.ShouldWarn owns "warn once"), cleared and re-armed when the shape comes right.
    private string? _shapeAlarm;
    private bool _shapeAlarmDismissed;
    private bool? _shapeWasWellFormed;

    /// <summary>Judge the plan's shape on the pass cadence, beside the arrival's own verdict. A plan can
    /// reach a bad shape with nobody pressing anything — a burn dropped ahead of the clamp, a row re-timed
    /// past its neighbour, an old vault loaded — so the law is asked about the BOARD, every cadence, and not
    /// only about the presses that touch it.</summary>
    private void RefreshPlanShapeValidity()
    {
        CastOffRule.PlanShapeFault fault = PlanShapeFaultNow();
        bool wellFormed = fault == CastOffRule.PlanShapeFault.None;

        if (ArrivalStepRule.ShouldWarn(_shapeWasWellFormed, wellFormed))
        {
            _shapeAlarm = CastOffRule.ShapeComplaint(fault);
            _shapeAlarmDismissed = false;
            LogAutopilotEvent(_shapeAlarm);
            ShowPulseMessage(_shapeAlarm, PulseRank.Beat);
            Warp = 1;               // the same drop the arrival's alarm takes — never unseen at warp
            _effectiveWarp = 1;
        }

        if (wellFormed)
        {
            _shapeAlarm = null;
        }

        _shapeWasWellFormed = wellFormed;
    }

    // ===== The one wake-up call, two reasons =====
    //
    // #952 built the pop-up for a plan that no longer ENDS safely; #989 adds the plan that cannot be FLOWN
    // as written. They are the same emergency ("nobody is flying the ship") and they get the same shell —
    // one modal, whichever alarm is standing, so a second pop-up never has to invent its own z-band. The
    // shape speaks first: a plan the ship cannot obey at all outranks the question of where it ends.

    /// <summary>The alarm the modal is showing, or null when nothing is standing undismissed.</summary>
    private string? LoudPlanAlarm =>
        _shapeAlarm is { } shape && !_shapeAlarmDismissed ? shape
        : _arriveAlarm is { } arrive && !_arriveAlarmDismissed ? arrive
        : null;

    private string LoudPlanAlarmHail =>
        _shapeAlarm is not null && !_shapeAlarmDismissed
            ? "⚠ THE FLIGHT PLAN CANNOT BE FLOWN AS WRITTEN"
            : "⚠ THE FLIGHT PLAN NO LONGER ENDS SAFELY";

    private string LoudPlanAlarmQuote =>
        _shapeAlarm is not null && !_shapeAlarmDismissed
            ? "\"That plan's got its own knots in it, captain. She'd not get out of the berth.\""
            : "\"She's off the plan, captain. Nothing at the far end of this course but the dark.\"";

    /// <summary>Dismiss whichever alarm the modal is currently carrying — the shape's first, by the same
    /// priority the modal reads them in, so one press never silences an alarm the captain never saw.</summary>
    private void DismissLoudPlanAlarm()
    {
        if (_shapeAlarm is not null && !_shapeAlarmDismissed)
        {
            _shapeAlarmDismissed = true;
            return;
        }
        DismissArriveAlarm();
    }

    /// <summary>The plan-shape complaint the list shows under the steps while the shape is bad — the ✗ the
    /// captain reads at the desk, where the alarm is the one that wakes him. Null while the plan is sound.</summary>
    private string? PlanShapeWarningLine() =>
        PlanShapeFaultNow() is var fault && fault != CastOffRule.PlanShapeFault.None
            ? CastOffRule.ShapeComplaint(fault)
            : null;

    /// <summary>Size and aim a clearance node off the harbour it belongs to. Called when the row is laid,
    /// and again whenever it is re-timed, so a step dragged three days down the list is re-solved against
    /// where the berth will actually BE then — nothing about a departure is cached.</summary>
    private void ResizeClearance(PlanNode node)
    {
        if (node.Kind != PlanStepKind.ClearHarbour || node.HavenId is null || _ephemeris is null)
        {
            return;
        }

        (Vector2d havenPos, Vector2d havenVel) = HavenStateAt(node.HavenId, node.SimTime);

        // Straight away from the haven, along the berthing arm — the same outward direction BerthState lays
        // the berth on and Undock shoves her down, so the plan's first burn pushes the way the arm already
        // pointed her instead of turning her across her own departure.
        node.Mode = BurnMode.Vector;
        node.HeadingDegrees = NodeFrame.Prograde(OutwardFromHaven(havenPos));
        node.Percent = CastOffRule.PulsePercent;

        // #937's law, honoured: ApplyPulses is the ONE place in the planner that writes a node's magnitude,
        // so the clearance is clamped, charged against the tank and re-solved by exactly the arithmetic a
        // typed number and a ±p press go through. A departure that could reach a magnitude the field would
        // have refused would be a second solve path, which is how the two drift apart.
        ApplyPulses(node, Math.Max(MinNodePulses, CastOffRule.Pulses(havenVel.Length, UndockPushMps)));
    }

    /// <summary>The direction "away from the harbour": the berthing arm's own outward radial, which is the
    /// sun-outward line <see cref="BerthState.CoMoving"/> lays every berth on.</summary>
    private static Vector2d OutwardFromHaven(Vector2d havenPosition) =>
        havenPosition == Vector2d.Zero ? new Vector2d(1, 0) : havenPosition.Normalized();

    /// <summary>A haven's position and drift at an epoch — the central-difference pair every other
    /// body-velocity read in this page uses.</summary>
    private (Vector2d Position, Vector2d Velocity) HavenStateAt(string havenId, double simTime)
    {
        const double h = 1.0;
        Vector2d pos = _ephemeris!.Position(havenId, simTime);
        Vector2d vel = (_ephemeris.Position(havenId, simTime + h) - _ephemeris.Position(havenId, simTime - h)) / (2 * h);
        return (pos, vel);
    }
}
