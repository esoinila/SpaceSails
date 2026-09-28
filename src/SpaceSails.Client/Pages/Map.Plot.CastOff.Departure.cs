using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · READING THE PLAN'S OWN BEGINNING, THE ROWS AND THE EXECUTOR — the pending undock and clearance
/// steps, removing the pair, nudging the departure, the plan's start state, the glance and ETA rows, and the
/// one step the executor runs.
///
/// <para>Split out of <c>Map.Plot.CastOff.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening file.
/// <c>RefreshPlanShapeValidity</c>, which ThePlayerIsToldTests names by this file, stays home.</para>
/// </summary>
public partial class Map
{
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

    /// <summary>The live 🚀 clearance row belonging to a departure, if it is still standing.</summary>
    private PlanNode? PendingClearanceStep()
    {
        foreach (PlanNode node in _planNodes)
        {
            if (node.Kind == PlanStepKind.ClearHarbour && !node.Stale && !node.Executed)
            {
                return node;
            }
        }
        return null;
    }

    /// <summary>
    /// #989 · <b>THE PAIR IS ONE ACT, AND IT COMES OFF AS ONE.</b> One press lays the clamp release and the
    /// out-thrust together because a cast-off that leaves her drifting in the traffic is not a cast-off — so
    /// the ✖ on either row takes both. Half a departure is not a plan a captain meant to have: an ⚓ with no
    /// clearance drops her into the harbour's traffic, and a 🚀 with no ⚓ is an out-thrust against a clamp
    /// that never let go. Returns the rows it actually removed, for the sentence the caller says.
    /// </summary>
    private int RemoveTheDeparturePair()
    {
        int removed = _planNodes.RemoveAll(n => n.Kind != PlanStepKind.Burn && !n.Executed);
        if (removed == 0)
        {
            return 0;
        }

        // Nothing may keep pointing at a row that is gone (the PR-D2 accordion idiom, said once).
        if (_selectedPlanNode is { } sel && sel.Kind != PlanStepKind.Burn)
        {
            _selectedPlanNode = null;
            if (_openEditor == FlightEditorKind.Burn)
            {
                _openEditor = FlightEditorKind.None;
            }
        }

        RebuildPlan();
        ReprojectTrajectory();
        return removed;
    }

    /// <summary>The ✖ on either departure row: the pair comes off together and the captain is told so, in
    /// one sentence, because a plan that quietly lost half a departure is exactly the #989 sighting.</summary>
    private void RemoveTheCastOff()
    {
        int removed = RemoveTheDeparturePair();
        ShowPulseMessage(removed > 1
            ? "⚓ Cast off removed — the clamp release and the clearance came off together; they are one act."
            : "⚓ Cast off removed.");
    }

    /// <summary>
    /// #989 · <b>±d / ±h ON THE DEPARTURE MOVES THE WHOLE ACT.</b> The captain re-times WHEN he leaves, not
    /// when one of two rows fires: the undock takes the nudge through <see cref="NodeFrame.NudgeEpoch"/> —
    /// the same faces, the same floor, as every other step's time buttons — and the clearance rides along at
    /// its own gap behind, re-solved against where the berth will actually BE then.
    /// </summary>
    private void NudgeDepartureEpoch(PlanNode undock, int sign, bool coarse)
    {
        double moved = NodeFrame.NudgeEpoch(undock.SimTime, sign, coarse, NodeEpochFloor());
        double delta = moved - undock.SimTime;
        if (delta == 0)
        {
            return;
        }

        undock.SimTime = moved;
        if (PendingClearanceStep() is { } clear)
        {
            clear.SimTime += delta;      // the pair's internal spacing is the act's own shape — keep it
            ResizeClearance(clear);      // …and a departure re-timed is a departure re-solved
        }

        SortNodes();
        RebuildPlan();
        ReprojectTrajectory();
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
    ///
    /// <para>#989 · <b>AND FROM THE BERTH AS IT WILL BE AT THE UNDOCK EPOCH.</b> Once a departure can be
    /// SCHEDULED, "the berth" is not one place: a berth 33 h out has swung a long way round its body, and a
    /// course drawn from where it stands tonight is a course from a place the ship will never leave from.
    /// The state is therefore the berth pinned at the epoch — the same <c>havenPos + _dockOffset</c>, drift
    /// matched, that <see cref="HoldAtDock"/> pins her with every tick — plus the same shove
    /// <see cref="Undock"/> will really give her. One arithmetic for the drawn departure and the flown one;
    /// #969's arm-time rehearsal reads this course, so the promise is rehearsed from the right berth too.</para>
    /// </summary>
    private ShipState PlanStartState()
    {
        if (_dockedHavenId is not { } haven || _ephemeris is null || PendingUndockStep() is not { } undock)
        {
            return _ship;
        }

        double at = Math.Max(undock.SimTime, _ship.SimTime);
        (Vector2d havenPos, Vector2d havenVel) = HavenStateAt(haven, at);
        var atTheBerth = new ShipState(havenPos + _dockOffset, havenVel, at);
        return ShovedOffTheClamp(atTheBerth, havenPos);
    }

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

    /// <summary>The clearance row's honest clock: how long, at the speed the harbour's law set, until the
    /// berth is behind her from where she is standing now. Said in the same words every other countdown in
    /// the plan is said in.</summary>
    private string ClearanceEtaLine(PlanNode node)
    {
        double seconds = CastOffRule.SecondsToClear(SeparationFromHarbour(node));
        return seconds <= 0
            ? "the harbour is already behind her"
            : $"≈{FormatDuration(seconds)} to clear from here";
    }

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
    /// The ⚓ Undock step the frame loop must land on, or null when the plan has none pending. Only the
    /// undock needs this — the clearance is an ordinary Vector node and the plan executor fires it like any
    /// other burn, which is exactly why it was built as one.
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

    /// <summary>
    /// The NOW line while a clamped ship's plan holds a cast off — the pilot banner's own words, so a captain
    /// who is nowhere near the Nav desk reads what the ship is doing. Null unless she really is clamped with
    /// a live cast-off ahead of her.
    ///
    /// <para>#989 · <b>TWO STATES, NOT ONE.</b> Before the epoch she is WAITING: tied up, the captain has the
    /// ship, and the plan lets go at its own hour ("docked at The Red Eye · ⚓ the plan casts off in 33 h").
    /// At the epoch — and only then — the autopilot has her and she is CASTING OFF. Saying "casting off in
    /// 33 h" was one sentence trying to be both, which is how the owner's screenshot came to read "casting
    /// off … in 0 h" while the ship sat at her berth for another day and a half.</para>
    /// </summary>
    private string? CastOffNowLine()
    {
        if (_dockedHavenId is null || PendingUndockStep() is not { } undock)
        {
            return null;
        }

        string haven = BodyName(_dockedHavenId);
        return undock.SimTime > SimTime
            ? CastOffRule.WaitingAtTheBerth(haven, $"in {FormatDuration(undock.SimTime - SimTime)}")
            : CastOffRule.CastingOffNow(haven);
    }

    /// <summary>
    /// #989 · Is the NOW line already the cast-off's own countdown? Then the ⚓ row must NOT be named again
    /// one line below it. Owner, off the second screenshot: <i>"there is something wonky with deletion of
    /// cast off events also… there are two in a row now"</i> — NOW and NEXT were two readings of the SAME
    /// live row, and no delete was needed to produce them. The banner still derives from the live step list
    /// (there is no second queue to go stale); it simply never says one step twice.
    /// </summary>
    private bool NowLineCarriesTheCastOff(PlanNode node) =>
        node.Kind == PlanStepKind.Undock && _dockedHavenId is not null && ReferenceEquals(node, PendingUndockStep());
}
