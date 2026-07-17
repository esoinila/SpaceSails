namespace SpaceSails.Core.Tests;

/// <summary>#172 — "⏭ skip to next event". The pure next-event selection, the eased warp ramp, and
/// the long-coast advert edge, exercised across the flight states the live control faces: armed cruise,
/// a burn pending, a kept orbit, and nothing armed (disabled).</summary>
public class WarpSkipTests
{
    private static WarpSkip.EventKind K(WarpSkip.NextEvent e) => e.Kind;

    // ---- Resolve: the soonest strictly-future event across states ----

    [Fact]
    public void Resolve_NothingArmed_FindsNoEvent_SoTheControlIsDisabled()
    {
        WarpSkip.NextEvent e = WarpSkip.Resolve(1000, new[]
        {
            new WarpSkip.Candidate(null, WarpSkip.EventKind.Burn),
            new WarpSkip.Candidate(null, WarpSkip.EventKind.Arrival),
        });

        Assert.False(e.Found);
        Assert.Equal(WarpSkip.EventKind.None, e.Kind);
    }

    [Fact]
    public void Resolve_BurnPendingBeforeArrival_PicksTheBurn()
    {
        // Armed cruise with a departure burn still ahead of the far arrival: the burn is NEXT.
        WarpSkip.NextEvent e = WarpSkip.Resolve(0, new[]
        {
            new WarpSkip.Candidate(3_600, WarpSkip.EventKind.Burn),
            new WarpSkip.Candidate(500_000, WarpSkip.EventKind.Arrival),
            new WarpSkip.Candidate(500_000, WarpSkip.EventKind.PlanEnd),
        });

        Assert.True(e.Found);
        Assert.Equal(WarpSkip.EventKind.Burn, e.Kind);
        Assert.Equal(3_600, e.Epoch);
    }

    [Fact]
    public void Resolve_ArmedCruiseNoBurnsLeft_PicksTheArrival()
    {
        // The departure burns have fired; the only thing ahead is the far arrival window.
        WarpSkip.NextEvent e = WarpSkip.Resolve(0, new[]
        {
            new WarpSkip.Candidate(null, WarpSkip.EventKind.Burn),
            new WarpSkip.Candidate(9_000_000, WarpSkip.EventKind.Arrival),
            new WarpSkip.Candidate(9_000_000, WarpSkip.EventKind.PlanEnd),
        });

        Assert.True(e.Found);
        Assert.Equal(WarpSkip.EventKind.Arrival, e.Kind);
        Assert.Equal(9_000_000, e.Epoch);
    }

    [Fact]
    public void Resolve_KeptOrbit_PicksTheNextTrim()
    {
        WarpSkip.NextEvent e = WarpSkip.Resolve(100, new[]
        {
            new WarpSkip.Candidate(100 + 12_345, WarpSkip.EventKind.KeepTrim),
        });

        Assert.True(e.Found);
        Assert.Equal(WarpSkip.EventKind.KeepTrim, e.Kind);
        Assert.Equal(100 + 12_345, e.Epoch);
    }

    [Fact]
    public void Resolve_IgnoresEventsAtOrBeforeNow()
    {
        // A burn epoch already reached (== now) is not a future event to skip to; the arrival wins.
        WarpSkip.NextEvent e = WarpSkip.Resolve(1_000, new[]
        {
            new WarpSkip.Candidate(1_000, WarpSkip.EventKind.Burn),       // already here
            new WarpSkip.Candidate(500, WarpSkip.EventKind.Burn),          // in the past
            new WarpSkip.Candidate(4_000, WarpSkip.EventKind.Arrival),
        });

        Assert.True(e.Found);
        Assert.Equal(WarpSkip.EventKind.Arrival, e.Kind);
        Assert.Equal(4_000, e.Epoch);
    }

    // ---- SkipWarp: crank far, ease into an un-guarded epoch, never exceed the ceiling ----

    [Fact]
    public void SkipWarp_CranksToTheCeiling_WhenTheTargetIsFar()
    {
        Assert.Equal(10_000, WarpSkip.SkipWarp(5_000_000, 10_000));
        // At exactly the ease window edge it is still full warp.
        Assert.Equal(10_000, WarpSkip.SkipWarp(WarpSkip.EaseWindowSeconds, 10_000));
    }

    [Fact]
    public void SkipWarp_EasesTowardOne_AsTheEpochNears()
    {
        int far = WarpSkip.SkipWarp(WarpSkip.EaseWindowSeconds, 10_000);
        int mid = WarpSkip.SkipWarp(WarpSkip.EaseWindowSeconds / 2, 10_000);
        int near = WarpSkip.SkipWarp(10, 10_000);

        Assert.True(mid < far, "warp should be lower halfway into the ease window");
        Assert.True(near < mid, "warp should keep dropping as the epoch nears");
        Assert.True(near >= 1, "warp never drops below 1×");
    }

    [Fact]
    public void SkipWarp_NeverExceedsTheNeighborhoodCeiling()
    {
        // Near a body the caller passes a capped ceiling (e.g. 100×). Skip must ask for no more.
        Assert.True(WarpSkip.SkipWarp(5_000_000, 100) <= 100);
        Assert.Equal(100, WarpSkip.SkipWarp(5_000_000, 100));
    }

    [Fact]
    public void SkipWarp_AtTheEpoch_IsRealtime()
    {
        Assert.Equal(1, WarpSkip.SkipWarp(0, 10_000));
        Assert.Equal(1, WarpSkip.SkipWarp(WarpSkip.ArriveToleranceSeconds, 10_000));
    }

    [Fact]
    public void HasArrived_TrueOnlyWithinTolerance()
    {
        Assert.False(WarpSkip.HasArrived(nowSeconds: 900, targetEpoch: 1_000));
        Assert.True(WarpSkip.HasArrived(nowSeconds: 1_000, targetEpoch: 1_000));
        Assert.True(WarpSkip.HasArrived(nowSeconds: 1_200, targetEpoch: 1_000)); // overshot slightly
    }

    // ---- Long-coast advert edge: fires once per leg, re-arms on the next ----

    [Fact]
    public void LongCoast_FiresOnce_WhenTheLongLegBegins_ThenStaysQuiet()
    {
        var far = new WarpSkip.NextEvent(true, 5_000_000, WarpSkip.EventKind.Arrival);

        WarpSkip.LongCoastDecision first = WarpSkip.EvaluateLongCoast(
            WarpSkip.LongCoastState.Idle, skipActive: false, far, nowSeconds: 0,
            WarpSkip.LongCoastThresholdSeconds);
        Assert.True(first.Fire);

        // A tick later, same leg (epoch essentially unchanged) — no re-fire.
        var stillFar = far with { Epoch = 5_000_000 - 50 };
        WarpSkip.LongCoastDecision second = WarpSkip.EvaluateLongCoast(
            first.State, skipActive: false, stillFar, nowSeconds: 50,
            WarpSkip.LongCoastThresholdSeconds);
        Assert.False(second.Fire);
    }

    [Fact]
    public void LongCoast_ReArms_OnTheNextLeg()
    {
        var legOne = new WarpSkip.NextEvent(true, 5_000_000, WarpSkip.EventKind.Arrival);
        WarpSkip.LongCoastDecision first = WarpSkip.EvaluateLongCoast(
            WarpSkip.LongCoastState.Idle, skipActive: false, legOne, 0, WarpSkip.LongCoastThresholdSeconds);
        Assert.True(first.Fire);

        // A wholly new, further leg (a fresh event well past the leg tolerance) fires afresh.
        var legTwo = new WarpSkip.NextEvent(true, 20_000_000, WarpSkip.EventKind.Arrival);
        WarpSkip.LongCoastDecision second = WarpSkip.EvaluateLongCoast(
            first.State, skipActive: false, legTwo, 1_000, WarpSkip.LongCoastThresholdSeconds);
        Assert.True(second.Fire);
    }

    [Fact]
    public void LongCoast_Silent_WhenTheCoastIsShort_AndReArmsForLater()
    {
        var near = new WarpSkip.NextEvent(true, 3_600, WarpSkip.EventKind.Burn); // < 1 day
        WarpSkip.LongCoastDecision d = WarpSkip.EvaluateLongCoast(
            WarpSkip.LongCoastState.Idle, skipActive: false, near, 0, WarpSkip.LongCoastThresholdSeconds);

        Assert.False(d.Fire);
        Assert.False(d.State.Offered); // re-armed, so a genuine long leg later will still fire.
    }

    [Fact]
    public void LongCoast_Frozen_WhileSkipping()
    {
        var far = new WarpSkip.NextEvent(true, 5_000_000, WarpSkip.EventKind.Arrival);
        WarpSkip.LongCoastDecision d = WarpSkip.EvaluateLongCoast(
            WarpSkip.LongCoastState.Idle, skipActive: true, far, 0, WarpSkip.LongCoastThresholdSeconds);

        Assert.False(d.Fire); // no point advertising the ride you're already on.
    }

    [Fact]
    public void LongCoast_NothingArmed_NeverFires()
    {
        WarpSkip.LongCoastDecision d = WarpSkip.EvaluateLongCoast(
            WarpSkip.LongCoastState.Idle, skipActive: false, WarpSkip.NextEvent.None, 0,
            WarpSkip.LongCoastThresholdSeconds);

        Assert.False(d.Fire);
    }
}
