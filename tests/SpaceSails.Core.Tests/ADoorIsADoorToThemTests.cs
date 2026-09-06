using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 · <b>A DOOR IS A DOOR TO THEM</b> — the pure half of the 2026-09-06 ruling, which is the half that
/// says where the numbers come from.
///
/// <para>The driven half lives in the Client suite (<c>AnOldOneOpensTheDoorTheSlowWayTests</c>), where a real
/// contact stands at a real leaf on the shipping wreck deck. What is pinned here is the arithmetic it is
/// stepped by, because a beat that is DERIVED and a beat that is TYPED are indistinguishable from the outside
/// on any one door — and the whole point of this lane is that nobody typed it.</para>
/// </summary>
public sealed class ADoorIsADoorToThemTests
{
    /// <summary>An ordinary unlocked leaf is a door: they open it. This is the case every other one here is
    /// a refusal of, so it is stated first and out loud.</summary>
    [Fact]
    public void AnOrdinaryLeafMayBeWorked()
    {
        Assert.True(ReeverDoor.MayWork(locked: false, interlocked: false, walledUp: false));
    }

    /// <summary>THE RESTRAINT. A locked leaf is a wall to them by choice — never opened, never broken. What
    /// they could do to it instead is never shown, which is the gramophone.</summary>
    [Fact]
    public void ALockedLeafIsNeverWorked()
    {
        Assert.False(ReeverDoor.MayWork(locked: true, interlocked: false, walledUp: false));
    }

    /// <summary>An airlock's leaf takes its turn by a law about the GROUP (#462); a leaf propped by an Old One
    /// could never take one. They are held at the crew-only lock before any tube anyway.</summary>
    [Fact]
    public void AnAirlocksLeafIsNeverWorked()
    {
        Assert.False(ReeverDoor.MayWork(locked: false, interlocked: true, walledUp: false));
    }

    /// <summary>#442's other direction: a leaf with a wall laid across its middle is a PICTURE of a door.
    /// Retracting it would open an image in front of stone that still stops them.</summary>
    [Fact]
    public void ALeafWithStoneAcrossItIsNeverWorked()
    {
        Assert.False(ReeverDoor.MayWork(locked: false, interlocked: false, walledUp: true));
    }

    /// <summary>
    /// THE BEAT IS THE LEAF'S OWN WIDTH AT THEIR OWN PACE — distance over speed, and nothing else. Pinned as
    /// a RELATION rather than a number: a leaf twice as wide takes twice as long, a pace twice as quick takes
    /// half as long. A typed constant cannot satisfy either.
    /// </summary>
    [Fact]
    public void TheBeatIsWidthOverPace_AndScalesWithBoth()
    {
        Assert.Equal(4.0 / 5.6, ReeverDoor.HaulSeconds(4.0, 5.6), 12);
        Assert.Equal(2 * ReeverDoor.HaulSeconds(4.0, 5.6), ReeverDoor.HaulSeconds(8.0, 5.6), 12);
        Assert.Equal(ReeverDoor.HaulSeconds(4.0, 5.6) / 2, ReeverDoor.HaulSeconds(4.0, 11.2), 12);
    }

    /// <summary>A leaf with nothing to travel, or a thing with no pace, has no beat. Honest rather than
    /// defensive — no plan hangs a zero-width door — but it must not divide by nothing.</summary>
    [Fact]
    public void ALeafWithNothingToTravelHasNoBeat()
    {
        Assert.Equal(0, ReeverDoor.HaulSeconds(0, 5.6));
        Assert.Equal(0, ReeverDoor.HaulSeconds(4.0, 0));
    }

    /// <summary>
    /// AN UNTOUCHED LEAF READS SHUT. Every door in the game is untouched on an ordinary frame, and the pen
    /// draws the slide off this number — so a version of it that answered "open" for a leaf with no beat
    /// recorded would have drawn every hatch in the game standing retracted.
    ///
    /// <para>It is not a hypothetical: the first draft did exactly that, and the locked-leaf guard in the
    /// Client suite caught it. This is the case that keeps it caught.</para>
    /// </summary>
    [Fact]
    public void AnUntouchedLeafIsShut()
    {
        Assert.Equal(0, ReeverDoor.Opening(hauledSeconds: 0, haulSeconds: 0));
        Assert.Equal(0, ReeverDoor.Opening(hauledSeconds: 0, haulSeconds: 0.7));
    }

    /// <summary>The drawn slide: half a beat is half over, and no frame length can overshoot the picture.</summary>
    [Fact]
    public void TheSlideIsTheBeatSpent_AndIsClampedBothEnds()
    {
        Assert.Equal(0.5, ReeverDoor.Opening(0.35, 0.7), 12);
        Assert.Equal(1.0, ReeverDoor.Opening(9.0, 0.7), 12);
        Assert.Equal(0.0, ReeverDoor.Opening(-1.0, 0.7), 12);
        Assert.True(ReeverDoor.Opened(0.7, 0.7));
        Assert.False(ReeverDoor.Opened(0.69, 0.7));
    }

    /// <summary>
    /// ARM'S LENGTH IS THE GAME'S OWN ARM'S LENGTH. The reach at which an Old One can lay hands on a leaf is
    /// two body radii — the same touching law <see cref="ReeverChase.CatchRadius"/> runs on, which is the only
    /// arm's length this game has ever had. Pinned against that constant so the two can never drift apart.
    /// </summary>
    [Fact]
    public void ItReachesADoorExactlyAsFarAsItReachesTheCaptain()
    {
        double radius = ReeverChase.CatchRadius / 2;   // the two bodies the catch law is the sum of
        Assert.Equal(ReeverChase.CatchRadius, ReeverDoor.Reach(radius), 12);
    }
}
