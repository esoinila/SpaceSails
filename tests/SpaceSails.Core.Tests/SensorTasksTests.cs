namespace SpaceSails.Core.Tests;

public class SensorTasksTests
{
    private static SensorTask Track(string id) => SensorTask.TrackUpdate(id, id);

    private static double FixedDuration(SensorTask _) => 100;

    // ---- Carousel order & timing ----

    [Fact]
    public void Advance_RunsQueueInOrder_AndWrapsAround()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Enqueue(Track("b"));
        schedule.Enqueue(Track("c"));

        IReadOnlyList<CompletedPass> passes = schedule.Advance(450, FixedDuration);

        Assert.Equal(4, passes.Count);
        Assert.Equal(["track:a", "track:b", "track:c", "track:a"], passes.Select(p => p.Task.Id));
        Assert.Equal(100, passes[0].CompleteTime);
        Assert.Equal(400, passes[3].CompleteTime);
    }

    [Fact]
    public void Advance_InSmallSteps_EmitsTheSamePasses_AsOneBigStep()
    {
        var one = new TelescopeSchedule();
        var many = new TelescopeSchedule();
        foreach (TelescopeSchedule s in new[] { one, many })
        {
            s.Enqueue(Track("a"));
            s.Enqueue(Track("b"));
        }

        var bigStep = one.Advance(1000, FixedDuration).ToList();
        var smallSteps = new List<CompletedPass>();
        for (double t = 50; t <= 1000; t += 50)
        {
            smallSteps.AddRange(many.Advance(t, FixedDuration));
        }

        Assert.Equal(bigStep, smallSteps);
    }

    [Fact]
    public void OneShotTask_LeavesTheQueue_AfterItsPass()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Enqueue(SensorTask.AreaScan(new Vector2d(3e11, 0), 1e10, "patch"));

        schedule.Advance(350, FixedDuration); // a, area, a — area ran once

        Assert.Equal(["track:a"], schedule.Queue.Select(t => t.Id));
        IReadOnlyList<CompletedPass> next = schedule.Advance(650, FixedDuration);
        Assert.All(next, p => Assert.Equal("track:a", p.Task.Id));
    }

    [Fact]
    public void ActiveProgress_ReportsTheRunningPassFraction()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));

        schedule.Advance(50, FixedDuration); // mid-pass

        Assert.NotNull(schedule.Active);
        Assert.Equal("track:a", schedule.Active!.Id);
        Assert.Equal(0.5, schedule.ActiveProgress(50), 3);
        Assert.Equal(100, schedule.ActiveCompleteTime);
    }

    [Fact]
    public void EmptyQueue_Idles_AndNewTaskStartsAtCurrentClock()
    {
        var schedule = new TelescopeSchedule();
        Assert.Empty(schedule.Advance(500, FixedDuration));

        schedule.Enqueue(Track("late"));
        IReadOnlyList<CompletedPass> passes = schedule.Advance(650, FixedDuration);

        Assert.Single(passes);
        Assert.Equal(500, passes[0].StartTime); // idle time is never back-filled
        Assert.Equal(600, passes[0].CompleteTime);
    }

    // ---- Priority operations ----

    [Fact]
    public void PrioritizeNext_JumpsTheQueue_WithoutKillingTheActivePass()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Enqueue(Track("b"));
        schedule.Enqueue(Track("c"));

        schedule.Advance(50, FixedDuration); // "a" is mid-pass
        schedule.PrioritizeNext("track:c");
        IReadOnlyList<CompletedPass> passes = schedule.Advance(300, FixedDuration);

        Assert.Equal(["track:a", "track:c", "track:b"], passes.Select(p => p.Task.Id));
    }

    [Fact]
    public void MoveUp_ChangesCarouselOrder()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Enqueue(Track("b"));
        Assert.True(schedule.MoveUp("track:b"));

        IReadOnlyList<CompletedPass> passes = schedule.Advance(200, FixedDuration);

        Assert.Equal(["track:b", "track:a"], passes.Select(p => p.Task.Id));
    }

    [Fact]
    public void Enqueue_RefusesDuplicateIds()
    {
        var schedule = new TelescopeSchedule();
        Assert.True(schedule.Enqueue(Track("a")));
        Assert.False(schedule.Enqueue(Track("a")));
        Assert.Single(schedule.Queue);
    }

    [Fact]
    public void Remove_CancelsTheActivePass_WithoutEmittingIt()
    {
        var schedule = new TelescopeSchedule();
        schedule.Enqueue(Track("a"));
        schedule.Enqueue(Track("b"));

        schedule.Advance(50, FixedDuration); // "a" mid-pass
        Assert.True(schedule.Remove("track:a"));
        IReadOnlyList<CompletedPass> passes = schedule.Advance(250, FixedDuration);

        Assert.All(passes, p => Assert.Equal("track:b", p.Task.Id));
    }

    // ---- Custody math: one instrument means real gaps ----

    [Fact]
    public void CrowdedQueue_MeansLongerRevisits_ThanASingleTrack()
    {
        double PassSeconds(SensorTask t) => SensorTaskGeometry.TrackPassSeconds;

        var solo = new TelescopeSchedule();
        solo.Enqueue(Track("prey"));
        var crowded = new TelescopeSchedule();
        crowded.Enqueue(Track("prey"));
        crowded.Enqueue(Track("other1"));
        crowded.Enqueue(Track("other2"));
        crowded.Enqueue(SensorTask.CorridorSweep("earth", "mars", "watch", recurring: true));

        double horizon = 8 * 3600;
        int soloPasses = solo.Advance(horizon, PassSeconds).Count(p => p.Task.Id == "track:prey");
        int crowdedPasses = crowded.Advance(horizon, PassSeconds).Count(p => p.Task.Id == "track:prey");

        Assert.True(soloPasses >= 4 * crowdedPasses,
            $"one telescope over four tasks must revisit far less often ({soloPasses} vs {crowdedPasses})");
    }

    // ---- Geometry pricing ----

    [Fact]
    public void WedgeToward_CoversTheDisc_AndFullCircleFromInside()
    {
        var ship = Vector2d.Zero;
        var center = new Vector2d(1e11, 0);

        ScanJob wedge = SensorTaskGeometry.WedgeToward(ship, center, 1e10);
        Assert.Equal(0, wedge.CenterBearingRad, 6);
        Assert.Equal(2 * Math.Asin(0.1), wedge.ArcWidthRad, 6);

        ScanJob inside = SensorTaskGeometry.WedgeToward(center, center, 1e10);
        Assert.Equal(Math.Tau, inside.ArcWidthRad, 6);
    }

    [Fact]
    public void Duration_WiderScansCostMore_AndUpgradesSpeedEverythingUp()
    {
        SensorTask area = SensorTask.AreaScan(new Vector2d(1e11, 0), 1, "patch");
        ScanJob narrow = new(0, 4.0 * Math.PI / 180);
        ScanJob wide = new(0, 90.0 * Math.PI / 180);

        double narrowSeconds = SensorTaskGeometry.Duration(area, narrow);
        double wideSeconds = SensorTaskGeometry.Duration(area, wide);
        Assert.True(wideSeconds > narrowSeconds);
        Assert.Equal(SensorTaskGeometry.MinPassSeconds, narrowSeconds); // floored: slew + settle

        Assert.Equal(wideSeconds / 2, SensorTaskGeometry.Duration(area, wide, speedFactor: 2), 6);

        SensorTask trackTask = SensorTask.TrackUpdate("x", "x");
        Assert.Equal(SensorTaskGeometry.TrackPassSeconds, SensorTaskGeometry.Duration(trackTask, narrow));
    }
}
