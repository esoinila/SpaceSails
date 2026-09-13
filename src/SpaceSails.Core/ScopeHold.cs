namespace SpaceSails.Core;

/// <summary>
/// #238 item 3 · A LIVE QUEST'S TARGET, AS THE TELESCOPE CAN RECOGNISE IT. Two facts and no opinions: the
/// <see cref="Id"/> a directed pass names (the contact a custody pass or a cold-case search is aimed at) and
/// the <see cref="Position"/> a patch of sky either holds or does not.
///
/// <para>It is deliberately NOT a quest. A <see cref="SensorTask"/> carries no quest link — it never has —
/// and the audit behind this fix found none to reuse: the client answers "is this body still the thing the
/// contract is about" by pairing the quest's source body with <c>IsBodyHidden</c>, at four separate call
/// sites, and answers "is this contact the mark" with the quest's own target ship id. So the telescope is
/// handed the ANSWER rather than the question, which keeps the Sensors desk innocent of contracts (it has
/// never known what a quest is) and keeps this file pure.</para>
/// </summary>
public readonly record struct QuestScopeTarget(string Id, Vector2d Position);

/// <summary>
/// #238 item 3 / #239 item 2 · <b>SWEEP HOLDS THE SCOPE.</b>
///
/// <para>Owner, filing #238 out of his own car hunt: <i>"when a manual sweep occupies the telescope while an
/// AIMED quest task waits queued, the Sensor tasks panel should say so ('sweep holds the scope — Roadster fix
/// waits') — the owner ran a 77% manual sweep unaware the real instrument was queued beneath it."</i></para>
///
/// <para>#239 gave the desk four honest words for WHERE a job stands. They are each true and, together, still
/// silent about the one thing that cost the owner the hunt: a manual sweep is not in the task list at all, so
/// the instrument that is actually holding the glass is the one row the panel has never had. RUNNING and
/// WAITING beside each other say what is happening; they do not say that what is happening is in the way.
/// This sentence is that, and only that.</para>
///
/// <para><b>One sentence, one template, one place</b> (#203, one voice). The Sensor-tasks panel's running row
/// and the Sensors desk chip's second line are two surfaces on one fact, and the fact is composed here — so
/// the two cannot come to word it differently, which is the only failure this line has available to it.</para>
/// </summary>
public static class ScopeHold
{
    /// <summary>
    /// One piece of telescope work, as this sentence needs to see it: what it is called, where it stands, and
    /// whether a live quest is waiting on it. A shape rather than a <see cref="SensorTask"/> because the
    /// thing that usually holds the glass here is the MANUAL SWEEP, which is not a task and never was — it
    /// takes the instrument out from under the whole carousel (<c>TelescopeSchedule.Interrupt</c>), which is
    /// precisely why the panel could not see it.
    /// </summary>
    public readonly record struct Work(string Name, SensorTaskState State, bool QuestCritical);

    /// <summary>What the thing holding the glass is called when it is the captain's own hand on it. Not used
    /// in the sentence — the sentence names the WAITER — but named here so a caller building the holder's row
    /// is not inventing a word for it.</summary>
    public const string ManualSweepName = "manual sweep";

    /// <summary>
    /// The sentence, in the owner's words, once. <paramref name="waiterName"/> is the quest-critical job's own
    /// display name — the roadster's is <see cref="Derelict.RoadsterScopeJobName"/>, and everything else wears
    /// the label it already wears in the queue.
    /// </summary>
    public static string Line(string waiterName) => $"sweep holds the scope — {waiterName} waits behind it";

    /// <summary>
    /// #238 item 3 · <b>THE WHOLE PREDICATE.</b> The line, or null — and null is the answer nearly always,
    /// which is the point: this is not a status, it is a complaint, and it is only true when something is in
    /// the way of something that matters.
    ///
    /// <para>Three conditions, all of them necessary. Something must be <see cref="SensorTaskState.Running"/>
    /// (an idle telescope is nobody's obstacle); that holder must NOT itself be quest-critical (the aimed job
    /// running is the good case, and a panel that complained about it would be telling the captain his own
    /// scan was in his way); and something quest-critical must actually be behind it,
    /// <see cref="SensorTaskState.Queued"/> or <see cref="SensorTaskState.Waiting"/>. A
    /// <see cref="SensorTaskState.Done"/> row is history and waits for nothing.</para>
    ///
    /// <para>The FIRST quest-critical waiter is the one named, in the order the caller hands them over — which
    /// for the desk is carousel order, so the sentence names the job that comes back first.</para>
    /// </summary>
    public static string? LineFor(IEnumerable<Work> work)
    {
        Work? holder = null;
        Work? waiter = null;
        foreach (Work item in work)
        {
            if (item.State == SensorTaskState.Running)
            {
                holder ??= item;
            }
            else if (item.QuestCritical && item.State is SensorTaskState.Queued or SensorTaskState.Waiting)
            {
                waiter ??= item;
            }
        }

        return holder is { QuestCritical: false } && waiter is { } behind ? Line(behind.Name) : null;
    }

    /// <summary>
    /// #238 item 3 · <b>IS THIS JOB TIED TO A LIVE QUEST TARGET — AND IF SO, WHAT DOES THE LINE CALL IT?</b>
    /// Null means "not quest-critical"; the predicate and the name are one answer on purpose, because they are
    /// decided by the same thing (WHICH target it turned out to be aimed at) and two methods asking that twice
    /// is how a sentence comes to name a job it is not actually about.
    ///
    /// <para><b>Aimed, not merely adjacent.</b> A directed pass (<see cref="SensorTaskKind.TrackUpdate"/>, the
    /// captain's own one-shot look, and a <see cref="SensorTaskKind.LostSearch"/> cold case) names its contact,
    /// so it matches by id. An <see cref="SensorTaskKind.AreaScan"/> names a patch of sky, so it matches when
    /// the target is INSIDE the patch — the same disc test <c>OnAreaScanCovered</c> uses to decide the reveal,
    /// asked here before the pass instead of after it. A <see cref="SensorTaskKind.CorridorSweep"/> is a
    /// standing survey of a trade lane and is aimed at nobody: it never qualifies, and a lane watch that
    /// happened to contain the mark would be the sentence claiming an intention the order never had.</para>
    /// </summary>
    public static string? WaiterName(SensorTask task, IReadOnlyList<QuestScopeTarget> liveTargets)
    {
        foreach (QuestScopeTarget target in liveTargets)
        {
            bool aimed = task.Kind switch
            {
                SensorTaskKind.TrackUpdate or SensorTaskKind.LostSearch =>
                    string.Equals(task.TargetShipId, target.Id, StringComparison.Ordinal),
                SensorTaskKind.AreaScan =>
                    (target.Position - task.AreaCenter).Length <= task.AreaRadius,
                _ => false,
            };

            if (aimed)
            {
                // #238 · HER OWN NAME, the way the found-her beat already gives her one. The aimed scan's
                // label is the queue's ("intel fix · 🔭 Roadster orbit fix") and the owner's sentence is not:
                // he calls it "🔭 Roadster fix". The fork is on the TARGET, exactly as RevealMomentFor forks,
                // and not on the label — a sentence that read a display string to decide what a job IS would
                // be one authored word away from being wrong.
                return string.Equals(target.Id, Derelict.RoadsterBodyId, StringComparison.Ordinal)
                    ? Derelict.RoadsterScopeJobName
                    : task.Label;
            }
        }

        return null;
    }
}
