namespace SpaceSails.Core.Tests;

/// <summary>
/// #239 · THE SENSOR JOBS DID NOT WEAR THEIR STATES, AND THE SCHEDULE COULD NOT HAVE TOLD THEM.
///
/// <para>Owner, after hunting the roadster with the wrong instrument: <i>"the sensor jobs should display
/// running, waiting, stopped states… I was using the general scan to try to find the Car. Let's make it
/// clearer what scan job is actually running."</i> Tonight's cost, in his own words: a 77% manual sweep
/// silently held the scope while the aimed Roadster fix — the one job that finds the car — waited invisible
/// beneath it. And minutes after filing, the exact failure mode: he watched the CORRECT aimed job at
/// <i>"● 0%"</i> and read it as <i>"maybe it is waiting to start."</i></para>
///
/// <para><b>The cause is here, not on the desk.</b> <see cref="TelescopeSchedule.Advance"/> returned its
/// <see cref="CompletedPass"/> list to the caller and retained <b>nothing</b>. So the schedule itself could
/// not answer "which job is the glass on", "has this one had its look yet" or "what just finished" — and the
/// desk, having no fact to read, inferred a state from a percentage. A percentage cannot tell RUNNING at 0%
/// from WAITING, which is precisely the mistake that was made.</para>
///
/// <para>These guards drive the real carousel and ask it for the state at each point. Every one of the four
/// states is reached, and each is reached in a standing the others are not — a test in which two states
/// happen to coincide would be green on a schedule that could not tell them apart.</para>
/// </summary>
public class SensorTasksWearTheirStatesTests
{
    private static SensorTask Track(string id) => SensorTask.TrackUpdate(id, id);

    private static SensorTask AreaScan(double x, string label) =>
        SensorTask.AreaScan(new Vector2d(x, 0), 1e10, label);

    private const double PassSeconds = 100;

    private static double FixedDuration(SensorTask _) => PassSeconds;

    /// <summary>
    /// ALL FOUR STATES, AT ONE INSTANT, ON ONE SCHEDULE. The point of the chip is that the four are
    /// distinguishable; a schedule that returned the same answer for two of them would pass a test that only
    /// ever looked at one job.
    /// </summary>
    [Fact]
    public void EveryStateIsShownForATaskThatIsActuallyInIt()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));                  // will have run, and be waiting its next turn
        schedule.Enqueue(AreaScan(3e11, "the patch")); // a one-shot: will run, finish, and leave — DONE
        schedule.Enqueue(Track("b"));                  // will be the one on the glass
        schedule.Enqueue(Track("c"));                  // never reached yet — QUEUED

        // Two passes land (a, then the patch); the third is under way, half exposed.
        schedule.Advance(PassSeconds * 2.5, FixedDuration);

        Assert.Equal(SensorTaskState.Running, schedule.StateOf("track:b"));
        Assert.Equal(SensorTaskState.Waiting, schedule.StateOf("track:a"));
        Assert.Equal(SensorTaskState.Queued, schedule.StateOf("track:c"));
        Assert.Equal(SensorTaskState.Done, schedule.StateOf(AreaScan(3e11, "the patch").Id));

        // The four really are four different words on the chip.
        Assert.Equal(4, new[]
        {
            SensorTaskPlates.For(SensorTaskState.Running),
            SensorTaskPlates.For(SensorTaskState.Waiting),
            SensorTaskPlates.For(SensorTaskState.Queued),
            SensorTaskPlates.For(SensorTaskState.Done),
        }.Distinct().Count());

        // …and the running one is genuinely mid-exposure at a progress a percentage cannot separate from a
        // waiting job's nothing. This is the owner's "● 0%" case, held open so it cannot come back.
        Assert.Equal(0.5, schedule.ActiveProgress(PassSeconds * 2.5), 6);
    }

    /// <summary>
    /// RUNNING AT ZERO IS STILL RUNNING. The whole reason the chip is its own signal: at the instant a pass
    /// starts, the job holding the telescope and every job behind it show the same number.
    /// </summary>
    [Fact]
    public void AJobAtZeroPercentIsRunningAndTheOneBehindItIsNot()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Enqueue(Track("b"));

        schedule.Advance(0.0001, FixedDuration); // the glass has just swung onto 'a'

        Assert.Equal(0, (int)(schedule.ActiveProgress(0.0001) * 100));
        Assert.Equal(SensorTaskState.Running, schedule.StateOf("track:a"));
        Assert.Equal(SensorTaskState.Queued, schedule.StateOf("track:b"));
    }

    /// <summary>
    /// A COMPLETED PASS STAYS DONE AFTER THE TICK THAT COMPLETED IT. The bug's own shape: the pass existed
    /// for exactly the length of the list <c>Advance</c> returned, and the row vanished from the desk at the
    /// tick it finished — so a captain who looked away for a second could not tell a job that had done its
    /// work from one that was never ordered.
    /// </summary>
    [Fact]
    public void AFinishedOneShotIsStillDoneManyTicksLater()
    {
        var schedule = new TelescopeSchedule();
        SensorTask patch = AreaScan(3e11, "the patch");
        schedule.Enqueue(patch);
        schedule.Enqueue(Track("a"));

        Assert.Single(schedule.Advance(PassSeconds, FixedDuration));
        Assert.DoesNotContain(schedule.Queue, t => t.Id == patch.Id);
        Assert.Equal(SensorTaskState.Done, schedule.StateOf(patch.Id));

        // Twenty more passes of other work go by; the finished scan is still on the board.
        for (int i = 1; i <= 20; i++)
        {
            schedule.Advance(PassSeconds * (1 + i), FixedDuration);
        }

        Assert.Equal(SensorTaskState.Done, schedule.StateOf(patch.Id));
        Assert.Contains(schedule.RecentlyDone, p => p.Task.Id == patch.Id);

        // And what it says about the pass is the pass that actually ran, not a placeholder.
        CompletedPass done = schedule.RecentlyDone.First(p => p.Task.Id == patch.Id);
        Assert.Equal(0, done.StartTime, 6);
        Assert.Equal(PassSeconds, done.CompleteTime, 6);
    }

    /// <summary>
    /// A RECURRING JOB BETWEEN LOOKS IS NOT "DONE". It never leaves the carousel, so it must read WAITING —
    /// a DONE row for a standing custody pass would tell the captain the glass had finished with a contact
    /// it is going to look at again in ninety seconds.
    /// </summary>
    [Fact]
    public void AStandingPassIsNeverDoneWhileItIsStillInTheCarousel()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Enqueue(Track("b"));

        schedule.Advance(PassSeconds * 1.5, FixedDuration); // 'a' has had a look; 'b' is on the glass

        Assert.Equal(SensorTaskState.Waiting, schedule.StateOf("track:a"));
        Assert.DoesNotContain(schedule.RecentlyDone, p => p.Task.Id == "track:a");
    }

    /// <summary>
    /// A JOB THE SCHEDULE HAS NEVER HEARD OF HAS NO STATE. Answering QUEUED for an id nobody enqueued would
    /// be the instrument inventing a job — and a caller that got a state for anything it asked about could
    /// never notice it was asking about the wrong thing.
    /// </summary>
    [Fact]
    public void AnUnknownJobHasNoStateAtAll()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Advance(PassSeconds * 1.5, FixedDuration);

        Assert.Null(schedule.StateOf("track:nobody"));
    }

    /// <summary>
    /// AN ABANDONED PASS IS NOT A FINISHED ONE. <see cref="TelescopeSchedule.Interrupt"/> — a manual sweep
    /// taking the instrument — abandons the exposure and emits nothing; the retained history must agree, or
    /// the desk would print DONE over a look that never landed.
    /// </summary>
    [Fact]
    public void AnInterruptedExposureNeverBecomesDone()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));

        schedule.Advance(PassSeconds * 0.5, FixedDuration);
        Assert.Equal(SensorTaskState.Running, schedule.StateOf("track:a"));

        schedule.Interrupt(PassSeconds * 0.5);

        Assert.Equal(SensorTaskState.Queued, schedule.StateOf("track:a"));
        Assert.Empty(schedule.RecentlyDone);
    }

    /// <summary>
    /// THE HISTORY IS CAPPED AND CARRIES EACH JOB ONCE. Retained state that grows without bound is a leak
    /// wearing a feature's clothes, and a standing job that had run forty times must not put forty rows on
    /// a desk that shows eight.
    /// </summary>
    [Fact]
    public void TheKeptHistoryIsBoundedAndHoldsOneEntryPerJob()
    {
        var schedule = new TelescopeSchedule();
        for (int i = 0; i < TelescopeSchedule.FinishedDepth * 3; i++)
        {
            schedule.Enqueue(AreaScan(3e11 + (i * 1e10), $"patch {i}"));
        }
        schedule.Enqueue(Track("a")); // recurring: runs many times, and is one row while it is in the queue

        schedule.Advance(PassSeconds * 200, FixedDuration);

        var done = schedule.RecentlyDone.ToList();
        Assert.True(done.Count <= TelescopeSchedule.FinishedDepth,
            $"{done.Count} finished passes retained against a depth of {TelescopeSchedule.FinishedDepth}");
        Assert.Equal(done.Count, done.Select(p => p.Task.Id).Distinct().Count());

        // The standing pass ran dozens of times and is still in the carousel, so it is on no DONE row.
        Assert.DoesNotContain(done, p => p.Task.Id == "track:a");
        Assert.Equal(SensorTaskState.Waiting, schedule.StateOf("track:a"));
    }
}
