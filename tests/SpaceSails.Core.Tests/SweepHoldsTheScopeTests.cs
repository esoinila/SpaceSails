namespace SpaceSails.Core.Tests;

/// <summary>
/// #238 item 3 / #239 item 2 · <b>SWEEP HOLDS THE SCOPE.</b>
///
/// <para>Owner, filing it out of his own car hunt: <i>"when a manual sweep occupies the telescope while an
/// AIMED quest task waits queued, the Sensor tasks panel should say so ('sweep holds the scope — Roadster fix
/// waits') — the owner ran a 77% manual sweep unaware the real instrument was queued beneath it."</i></para>
///
/// <para>#239 bought four honest words for WHERE a job stands, and every one of them was true that night. The
/// thing that cost him the hunt is that a manual sweep is not a task at all — it takes the instrument out from
/// under the carousel — so nothing in the list was RUNNING and nothing was wrong. This sentence is the fact
/// those four words cannot carry: that what is happening is IN THE WAY of what he asked for.</para>
///
/// <para>Two halves, and they fail differently. <b>The predicate</b> (<see cref="ScopeHold.WaiterName"/>) is
/// where a job becomes quest-critical, and it is derived from the TARGET — a SensorTask carries no quest link
/// and this fix did not add one. <b>The composition</b> (<see cref="ScopeHold.LineFor"/>) is where three
/// conditions have to hold at once, and each of the three is proved able to withhold the line on its own,
/// because a guard that only ever saw the true case would pass on a method that returned the sentence
/// always.</para>
/// </summary>
public class SweepHoldsTheScopeTests
{
    private static readonly Vector2d RoadsterAt = new(2.1e11, 3e10);

    private const double ScanRadius = 4e10;

    /// <summary>The scan the 🔭 intel button actually queues: a generous disc around where the wreck should be
    /// a touch from now, labelled off the intel card's headline. The label is deliberately NOT the owner's
    /// sentence — that difference is what the roadster fork exists for.</summary>
    private static SensorTask TheRoadsterFix(Vector2d center) =>
        SensorTask.AreaScan(center, ScanRadius, "intel fix · 🔭 Roadster orbit fix");

    private static QuestScopeTarget TheRoadster(Vector2d at) =>
        new(Derelict.RoadsterBodyId, at);

    private static ScopeHold.Work Holder(string name) =>
        new(name, SensorTaskState.Running, QuestCritical: false);

    // ── THE PREDICATE: how a job is known to belong to a quest ─────────────────────────────────────────

    /// <summary>
    /// THE AIMED SCAN IS THE ONE WITH HER INSIDE IT. The quest link is derived from the disc containing the
    /// live target — the same containment test the reveal already makes of a completed pass — so the same
    /// task, aimed a whole scan-radius further out, is not quest-critical at all. Both cases in one test on
    /// purpose: a predicate that answered "yes" to everything passes the first assertion.
    /// </summary>
    [Fact]
    public void AScanWithALiveQuestTargetInsideItIsQuestCriticalAndOneAimedElsewhereIsNot()
    {
        QuestScopeTarget[] live = [TheRoadster(RoadsterAt)];

        Assert.Equal(
            Derelict.RoadsterScopeJobName,
            ScopeHold.WaiterName(TheRoadsterFix(RoadsterAt), live));

        // Two radii away: she cannot be in this disc, and no amount of "it is the roadster scan" may say so.
        var elsewhere = new Vector2d(RoadsterAt.X + 2 * ScanRadius, RoadsterAt.Y);
        Assert.Null(ScopeHold.WaiterName(TheRoadsterFix(elsewhere), live));
    }

    /// <summary>
    /// A DIRECTED PASS MATCHES BY ID, and wears its own label. The generic half of the owner's template: the
    /// name in the sentence is the job's OWN existing display name, not a second string invented for the line.
    /// </summary>
    [Fact]
    public void ACustodyPassOnAQuestsMarkIsQuestCriticalAndKeepsItsOwnName()
    {
        QuestScopeTarget[] live = [new("mark-7", new Vector2d(9e11, 0))];

        SensorTask onHer = SensorTask.TrackUpdate("mark-7", "GOLDEN HIND");
        Assert.Equal("GOLDEN HIND", ScopeHold.WaiterName(onHer, live));

        SensorTask onSomebodyElse = SensorTask.TrackUpdate("freighter-2", "KESTREL");
        Assert.Null(ScopeHold.WaiterName(onSomebodyElse, live));

        // A cold case for the mark is the same aim by another name, and counts.
        Assert.Equal("search — GOLDEN HIND",
            ScopeHold.WaiterName(SensorTask.LostSearch("mark-7", "search — GOLDEN HIND"), live));
    }

    /// <summary>
    /// A LANE WATCH IS AIMED AT NOBODY. A corridor sweep surveys a trade lane; the mark drifting through it is
    /// a coincidence, not an order. Proved with the target placed at the sweep's own anchors, so the only thing
    /// keeping this null is the KIND — a predicate that fell back on geometry for every task would go red here.
    /// </summary>
    [Fact]
    public void AStandingCorridorWatchIsNeverQuestCritical()
    {
        QuestScopeTarget[] live = [TheRoadster(Vector2d.Zero)];
        SensorTask lane = SensorTask.CorridorSweep("earth", "mars", "Earth–Mars lane watch", recurring: true);

        Assert.Null(ScopeHold.WaiterName(lane, live));
    }

    /// <summary>NOTHING IS LIVE, NOTHING IS CRITICAL. The contract is handed in, the wreck is charted — the
    /// page hands down an empty list and the aimed scan goes back to being a patch of sky.</summary>
    [Fact]
    public void WithNoLiveQuestTargetsEvenTheRoadsterFixIsJustAScan()
    {
        Assert.Null(ScopeHold.WaiterName(TheRoadsterFix(RoadsterAt), []));
    }

    // ── THE SENTENCE: the owner's words, and the three conditions that withhold them ───────────────────

    /// <summary>
    /// THE OWNER'S LINE, FOR THE ROADSTER, VERBATIM. Written out as a literal here and nowhere else in the
    /// shipping code — this is the one place the sentence is typed twice on purpose, so that a template edited
    /// in Core has to come and argue with the owner's own words.
    /// </summary>
    [Fact]
    public void TheManualSweepHoldingTheGlassSaysSoInTheOwnersWords()
    {
        string? line = ScopeHold.LineFor(
        [
            Holder(ScopeHold.ManualSweepName),
            new(Derelict.RoadsterScopeJobName, SensorTaskState.Queued, QuestCritical: true),
        ]);

        Assert.Equal("sweep holds the scope — 🔭 Roadster fix waits behind it", line);
    }

    /// <summary>THE SAME SENTENCE FOR ANYBODY ELSE, with the job's own display name in it — the owner's
    /// template is one shape, not a roadster special case with a general fallback bolted on.</summary>
    [Fact]
    public void AnyOtherQuestCriticalWaiterGetsTheSameSentenceUnderItsOwnName()
    {
        string? line = ScopeHold.LineFor(
        [
            Holder(ScopeHold.ManualSweepName),
            new("GOLDEN HIND", SensorTaskState.Waiting, QuestCritical: true),
        ]);

        Assert.Equal("sweep holds the scope — GOLDEN HIND waits behind it", line);
    }

    /// <summary>
    /// THE AIMED JOB RUNNING IS THE GOOD CASE. The roadster fix has the glass and the manual sweep is behind
    /// it: nothing is in anybody's way, and a panel that complained here would be telling the captain his own
    /// scan was the obstacle. This is the condition a naive "is anything quest-critical queued" predicate gets
    /// wrong, which is why it is its own guard.
    /// </summary>
    [Fact]
    public void NothingIsSaidWhileTheQuestCriticalJobIsTheOneOnTheGlass()
    {
        Assert.Null(ScopeHold.LineFor(
        [
            new(Derelict.RoadsterScopeJobName, SensorTaskState.Running, QuestCritical: true),
            new("sky scan · 0.4 AU out", SensorTaskState.Queued, QuestCritical: false),
        ]));
    }

    /// <summary>NOTHING QUEUED THAT MATTERS, NOTHING TO SAY. A sweep holding the scope with only routine work
    /// behind it is a telescope doing its job.</summary>
    [Fact]
    public void NothingIsSaidWhenTheQueueBehindTheSweepIsRoutine()
    {
        Assert.Null(ScopeHold.LineFor(
        [
            Holder(ScopeHold.ManualSweepName),
            new("Earth–Mars lane watch", SensorTaskState.Waiting, QuestCritical: false),
            new("sky scan · 0.4 AU out", SensorTaskState.Queued, QuestCritical: false),
        ]));
    }

    /// <summary>
    /// AN IDLE TELESCOPE IS NOBODY'S OBSTACLE. The aimed job is queued and NOTHING is running — the schedule
    /// between passes, or the instant before the carousel picks it up. There is no holder, so there is no
    /// complaint to make about one.
    /// </summary>
    [Fact]
    public void NothingIsSaidWhenNothingHoldsTheGlassAtAll()
    {
        Assert.Null(ScopeHold.LineFor(
            [new(Derelict.RoadsterScopeJobName, SensorTaskState.Queued, QuestCritical: true)]));
    }

    /// <summary>A FINISHED PASS WAITS FOR NOTHING. A DONE row is history and stays on the desk dimmed (#239);
    /// it must never be read as something the sweep is standing on.</summary>
    [Fact]
    public void ADoneQuestCriticalRowIsNotAWaiter()
    {
        Assert.Null(ScopeHold.LineFor(
        [
            Holder(ScopeHold.ManualSweepName),
            new(Derelict.RoadsterScopeJobName, SensorTaskState.Done, QuestCritical: true),
        ]));
    }

    /// <summary>
    /// THE SENTENCE NAMES THE ONE THAT COMES BACK FIRST. Two quest-critical jobs behind the sweep: the line
    /// names the one earlier in carousel order, which is the one the captain will actually get. A composer
    /// that took the last would tell him to wait for the wrong thing.
    /// </summary>
    [Fact]
    public void TheFirstQuestCriticalWaiterInCarouselOrderIsTheOneNamed()
    {
        string? line = ScopeHold.LineFor(
        [
            Holder(ScopeHold.ManualSweepName),
            new(Derelict.RoadsterScopeJobName, SensorTaskState.Queued, QuestCritical: true),
            new("GOLDEN HIND", SensorTaskState.Waiting, QuestCritical: true),
        ]);

        Assert.Equal(ScopeHold.Line(Derelict.RoadsterScopeJobName), line);
    }
}
