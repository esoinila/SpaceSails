namespace SpaceSails.Core.Tests;

public class LostTrackTests
{
    private const double Day = 86400;

    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    private static TrackedTarget StaleTrack(string id, Vector2d position, Vector2d velocity, double confirmedAt) =>
        new(id, new Observation(id, confirmedAt, position, velocity), confirmedAt, TrackedTargetLedger.InitialQuality);

    // ---- Ledger handoff: dropped tracks come back out ----

    [Fact]
    public void AdvanceTime_ReturnsTheDroppedEntries()
    {
        var ledger = new TrackedTargetLedger(maxTracks: 2);
        ledger.Add(new Observation("fresh", 0, new Vector2d(1e11, 0), Vector2d.Zero));
        ledger.Add(new Observation("stale", 0, new Vector2d(2e11, 0), Vector2d.Zero));
        ledger.TryGet("fresh", out _);

        // decay InitialQuality 0.4 to <= 0.05 needs ~1.75 stale days past the horizon
        double lostTime = TrackedTargetLedger.StalenessHorizonSeconds + 2 * Day;
        ledger.Add(new Observation("fresh", lostTime - 1, new Vector2d(1e11, 0), Vector2d.Zero)); // reconfirm keeps it
        IReadOnlyList<TrackedTarget> dropped = ledger.AdvanceTime(lostTime);

        Assert.Single(dropped);
        Assert.Equal("stale", dropped[0].ShipId);
        Assert.True(ledger.IsTracked("fresh"));
        Assert.False(ledger.IsTracked("stale"));
    }

    // ---- Search region growth & shrink ----

    [Fact]
    public void SearchRadius_GrowsFromTheConeWidth_AtLossTime()
    {
        var ledger = new LostTrackLedger();
        var dropped = StaleTrack("prey", new Vector2d(2e11, 0), new Vector2d(0, 20_000), confirmedAt: 0);
        double lostTime = 7 * Day;

        ledger.AddFrom(dropped, lostTime);
        Assert.True(ledger.TryGet("prey", out LostTrack lost));

        double atLoss = lost.SearchRadius(lostTime);
        double aDayLater = lost.SearchRadius(lostTime + Day);
        Assert.True(atLoss > PredictedPath.BaseHalfWidthMeters, "starts as wide as the cone that broke the lock");
        Assert.True(aDayLater > atLoss, "an unsearched region keeps growing");
    }

    [Fact]
    public void RecordSearchPass_ShrinksTheRegion()
    {
        var ledger = new LostTrackLedger();
        ledger.AddFrom(StaleTrack("prey", new Vector2d(2e11, 0), new Vector2d(0, 20_000), 0), 7 * Day);
        ledger.TryGet("prey", out LostTrack before);
        double searchTime = 7 * Day + Day;
        double beforeRadius = before.SearchRadius(searchTime);

        ledger.RecordSearchPass("prey", searchTime);

        ledger.TryGet("prey", out LostTrack after);
        Assert.Equal(beforeRadius * LostTrackLedger.ShrinkOnPass, after.SearchRadius(searchTime), 0);
    }

    [Fact]
    public void DropColdCases_LetsAnOldTrailGo()
    {
        var ledger = new LostTrackLedger();
        ledger.AddFrom(StaleTrack("prey", new Vector2d(2e11, 0), new Vector2d(0, 20_000), 0), 7 * Day);

        Assert.Empty(ledger.DropColdCases(7 * Day + 3600)); // an hour in: still searchable
        IReadOnlyList<LostTrack> cold = ledger.DropColdCases(7 * Day + 30 * Day);

        Assert.Single(cold);
        Assert.Equal("prey", cold[0].ShipId);
        Assert.False(ledger.IsLost("prey"));
    }

    // ---- Re-acquire ----

    [Fact]
    public void TryReacquire_FindsAHull_StillInsideTheRegion()
    {
        CircularOrbitEphemeris sol = Sol();
        var telescope = new TelescopeModel();
        var ledger = new LostTrackLedger();
        var observation = new Observation("prey", 0, new Vector2d(2e11, 0), new Vector2d(0, 15_000));
        ledger.AddFrom(new TrackedTarget("prey", observation, 0, TrackedTargetLedger.InitialQuality), 6 * 3600);
        ledger.TryGet("prey", out LostTrack lost);

        double now = 12 * 3600;
        Vector2d predicted = LostSearchRule.PredictedCenter(sol, lost, now);
        var stillThere = new ShipState(predicted + new Vector2d(1e7, 0), new Vector2d(0, 15_000), now);
        var observer = new Vector2d(1.5e11, 0);

        Assert.True(LostSearchRule.TryReacquire(sol, telescope, lost, observer, stillThere, now, out Observation obs));
        Assert.Equal("prey", obs.TargetId);
        Assert.Equal(stillThere.Position, obs.Position);
    }

    [Fact]
    public void TryReacquire_Misses_AHullThatBurnedOutOfTheRegion()
    {
        CircularOrbitEphemeris sol = Sol();
        var telescope = new TelescopeModel();
        var ledger = new LostTrackLedger();
        var observation = new Observation("prey", 0, new Vector2d(2e11, 0), new Vector2d(0, 15_000));
        ledger.AddFrom(new TrackedTarget("prey", observation, 0, TrackedTargetLedger.InitialQuality), 6 * 3600);
        ledger.TryGet("prey", out LostTrack lost);

        double now = 12 * 3600;
        Vector2d predicted = LostSearchRule.PredictedCenter(sol, lost, now);
        double radius = lost.SearchRadius(now);
        var farAway = new ShipState(predicted + new Vector2d(radius * 3, 0), new Vector2d(0, 15_000), now);

        Assert.False(LostSearchRule.TryReacquire(
            sol, telescope, lost, new Vector2d(1.5e11, 0), farAway, now, out _));
    }
}
