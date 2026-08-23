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
    /// </summary>
    private void AddCastOffAtTop()
    {
        if (_dockedHavenId is not { } haven)
        {
            ShowPulseMessage("⚓ Nothing to cast off from — she's already under way.");
            return;
        }

        if (PlanBeginsWithCastOff)
        {
            ShowPulseMessage("⚓ The plan already starts with a cast off.");
            return;
        }

        // The undock sits at the plan's own floor — one minute out, the same floor every other node-timing
        // path in this file honours — and the clearance one floor-width behind it, so the loop has a step in
        // which to notice the clamp is gone before the burn is asked to fire.
        double undockAt = NodeEpochFloor();
        double clearAt = undockAt + CastOffGapSeconds;

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
            $"⚓ Cast off laid at the top of the plan — the clamp lets go at {BodyName(haven)}, "
            + $"then ≈{clear.Pulses} p carries her clear of the harbour.");
    }

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

    // ===== Reading the plan's own beginning =====

    /// <summary>The pending ⚓ Undock step, if the plan has one still ahead of it.</summary>
    private PlanNode? PendingUndockStep()
    {
        foreach (PlanNode node in _planNodes)
        {
            if (node.Kind == PlanStepKind.Undock && !node.Stale && !node.Executed)
            {
                return node;
            }
        }
        return null;
    }

    /// <summary>
    /// Does the plan BEGIN at the berth? The one predicate the clamped arm reads (#969's plan-time promise
    /// is allowed from a berth exactly when the plan casts her off first) and the one the banner reads.
    /// "Begins" is meant literally: the undock must be the first live step, because a burn plotted ahead of
    /// it is a burn the clamp will eat.
    /// </summary>
    private bool PlanBeginsWithCastOff
    {
        get
        {
            foreach (PlanNode node in _planNodes)
            {
                if (node.Stale || node.Executed)
                {
                    continue;
                }
                return node.Kind == PlanStepKind.Undock;
            }
            return false;
        }
    }

    /// <summary>
    /// <b>THE STATE THE PLAN STARTS FROM.</b> Ordinarily the ship as she is. When the plan begins with a cast
    /// off, the clamp is about to let go and the berth's own shove is part of the trip — so the plotted
    /// ribbon, the passes read off it, the arrival's ✓/✗ and #969's arm-time rehearsal are all computed FROM
    /// THE BERTH ONWARD, which is the owner's item 3 in one method. Without this the ribbon would draw a
    /// clamped ship's frozen berth state and the arrival would be judged on a voyage that never left.
    /// </summary>
    private ShipState PlanStartState()
    {
        if (_dockedHavenId is not { } haven || _ephemeris is null || PendingUndockStep() is null)
        {
            return _ship;
        }

        return ShovedOffTheClamp(_ship, _ephemeris.Position(haven, _ship.SimTime));
    }

    /// <summary>The haven a pending cast-off leaves from, for the rows and the banner.</summary>
    private string CastOffHavenName() =>
        PendingUndockStep()?.HavenId is { } id ? BodyName(id) : _havenName;

    // ===== The rows =====

    /// <summary>The collapsed glance line for a departure step — the same shape as
    /// <c>BurnGlanceLine</c> (kind · what it does · countdown) so the whole trip still reads top to bottom
    /// as one list. Composed in Core so the row, the banner and the desk chip cannot word it three ways.</summary>
    private string DepartureGlanceLine(PlanNode node)
    {
        string when = node.Executed ? "done"
            : node.Stale ? "struck"
            : node.SimTime <= SimTime ? "now"
            : $"in {FormatDuration(node.SimTime - SimTime)}";
        return CastOffRule.GlanceLine(node.Kind, HavenNameOf(node), node.Pulses, when);
    }

    private string HavenNameOf(PlanNode node) => node.HavenId is { } id ? BodyName(id) : "the berth";

    /// <summary>How far the harbour is behind her right now — what the clearance row explains itself
    /// with. Measured from the haven the step belongs to, live.</summary>
    private double SeparationFromHarbour(PlanNode node)
    {
        if (node.HavenId is null || _ephemeris is null)
        {
            return 0;
        }
        return (_ship.Position - _ephemeris.Position(node.HavenId, SimTime)).Length;
    }

    // ===== The executor =====

    /// <summary>
    /// The next ⚓ Undock step the loop must land on: pending, and due at or before this epoch is reached.
    /// Only the undock needs this — the clearance is an ordinary Vector node and the plan executor fires it
    /// like any other burn, which is exactly why it was built as one.
    /// </summary>
    private PlanNode? NextCastOffStep() => PendingUndockStep();

    /// <summary>
    /// <b>Run the cast-off.</b> The clamp lets go, on the plan's own word, with nobody at the console — the
    /// half of the owner's sentence that today's code cannot do at all. Everything about HOW she leaves is
    /// <see cref="Undock"/>'s, unchanged: the deck comes back aboard, the berth's shove is applied, the
    /// destination lock is healed. This adds only the two things a PLANNED cast-off owes: the step retires
    /// itself, and the pilot banner says who did it.
    /// </summary>
    private void RunTheCastOffStep(PlanNode step)
    {
        step.Executed = true;

        if (_dockedHavenId is null)
        {
            // Already free — a captain who cast off by hand before the plan got there. The step is simply
            // retired; it must never "undock" a flying ship (that is how a step kind grows a second meaning).
            return;
        }

        string haven = BodyName(_dockedHavenId);
        Undock();
        LogAutopilotEvent($"the plan cast her off from {haven}");
        ShowPulseMessage($"⚓ {CastOffRule.CastingOffNow(haven)}");
    }

    /// <summary>The NOW line while a clamped ship's plan is about to cast her off — the pilot banner's own
    /// words, so a captain who is nowhere near the Nav desk reads that the ship is leaving on her own.
    /// Null unless she really is clamped with a live cast-off ahead of her.</summary>
    private string? CastOffNowLine()
    {
        if (_dockedHavenId is null || PendingUndockStep() is not { } undock)
        {
            return null;
        }

        string line = CastOffRule.CastingOffNow(BodyName(_dockedHavenId));
        return undock.SimTime > SimTime
            ? $"{line} in {FormatDuration(undock.SimTime - SimTime)}"
            : line;
    }
}
