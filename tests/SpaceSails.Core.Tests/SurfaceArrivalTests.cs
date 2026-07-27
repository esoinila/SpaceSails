namespace SpaceSails.Core.Tests;

/// <summary>
/// #461 · The landing is not an ambush. Owner, live 2026-07-27, jumped on the pad: <i>"That makes no sense…
/// how did they know the shuttle would land just there."</i> … <i>"I had to use a sentry to even get out the
/// door."</i> … and the law: <i>"The reevers only get excited when they spot a non-reever. So ship in itself
/// does not attract them. They expect it is their ship… There should be at minimum a timer of player getting
/// out of the ship before the first reever spots them. Also there should always be one un-paid-for sentry at
/// the door."</i> … <i>"inside the tube there is always an unlimited ammo sentry built in."</i>
/// </summary>
public class SurfaceArrivalTests
{
    [Fact]
    public void NothingMayNoticeYou_UntilTheGraceHasRun()
    {
        Assert.False(SurfaceArrival.CanBeSpotted(0));                                    // the moment you mate
        Assert.False(SurfaceArrival.CanBeSpotted(SurfaceArrival.SpotGraceSeconds - 0.1)); // still owed
        Assert.True(SurfaceArrival.CanBeSpotted(SurfaceArrival.SpotGraceSeconds));        // the ground wakes
        Assert.True(SurfaceArrival.CanBeSpotted(999));
    }

    [Fact]
    public void TheGraceIsLongEnoughToGetOutTheDoor_AndShortEnoughToStayFrightening()
    {
        // The whole "let me get out of the door" dial. Too short and the pad is an ambush again; too long
        // and the deep stops being dangerous, which is the other way to ruin it.
        Assert.InRange(SurfaceArrival.SpotGraceSeconds, 10, 45);
    }

    [Fact]
    public void TheCountdownIsReadable_SoThePlayerCanSeeTheQuietTheyWereGiven()
    {
        Assert.Equal(SurfaceArrival.SpotGraceSeconds, SurfaceArrival.GraceLeft(0), 6);
        Assert.Equal(0, SurfaceArrival.GraceLeft(SurfaceArrival.SpotGraceSeconds), 6);
        Assert.Equal(0, SurfaceArrival.GraceLeft(1e6), 6);
        // Monotonic — a grace that jittered would be worse than none.
        Assert.True(SurfaceArrival.GraceLeft(3) > SurfaceArrival.GraceLeft(9));
    }

    [Fact]
    public void GarbageTimesNeverGrantASilentAmbush()
    {
        // A NaN clock must not read as "grace expired" and quietly wake the whole field.
        Assert.False(SurfaceArrival.CanBeSpotted(double.NaN));
        Assert.Equal(0, SurfaceArrival.GraceLeft(double.NaN), 6);
    }

    [Fact]
    public void TheTubesOwnGun_IsFullyLoadedAndNotYours()
    {
        // "one un-paid-for sentry at the door" / "unlimited ammo sentry built in": it starts at a full
        // magazine and is identifiable, so the client can top it back up and keep it out of the captain's
        // retrieve/abandon ledger at liftoff.
        Assert.Equal(SentryBot.MaxMagazine, SurfaceArrival.DoorSentryRounds);
        Assert.True(SurfaceArrival.IsDoorSentry(SurfaceArrival.DoorSentryUnit));
        Assert.False(SurfaceArrival.IsDoorSentry("K-77"));
        Assert.False(SurfaceArrival.IsDoorSentry("R-3B"));
        Assert.False(SurfaceArrival.IsDoorSentry(""));
    }
}
