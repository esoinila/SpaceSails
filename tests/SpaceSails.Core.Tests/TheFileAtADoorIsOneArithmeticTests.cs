using System;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #731 · ONE QUEUE, ONE SUM. Two crews in this game now file through a door one at a time — the hull sweep
/// team at a wreck's shuttle lock (#946) and the repo crew at their own boat's hatch — and until this lane the
/// arithmetic of <i>"the head stands a standoff off the leaf and everybody else a spacing further back"</i>
/// lived inline in one of them.
///
/// <para>A second copy of it is this repository's oldest bug class with a body standing in it: two files that
/// disagree by a body-width are two figures drawn inside one another. So the sum is
/// <see cref="Egress.PlaceInTheFile"/>, and these are the things it may never stop being true of.</para>
/// </summary>
public sealed class TheFileAtADoorIsOneArithmeticTests
{
    /// <summary>The head of the file stands exactly one door standoff off the leaf, on the side the queue is
    /// on. Not on it — a locked leaf has stone poured behind it and no route can end there.</summary>
    [Fact]
    public void TheHeadStandsOneStandoffOffTheLeaf()
    {
        (double x, double y) = Egress.PlaceInTheFile(10, 4, -1, 0, 0, InspectionTeam.FileSpacingDu);
        Assert.Equal(10 - Egress.DoorStandoffDu, x, 9);
        Assert.Equal(4, y, 9);
    }

    /// <summary>…and everybody behind them is one spacing further back, in the same direction, for ever.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public void EveryRankIsOneSpacingBehindTheOneInFront(int rank)
    {
        (double x, double y) = Egress.PlaceInTheFile(0, 0, 0, -1, rank, InspectionTeam.FileSpacingDu);
        Assert.Equal(0, x, 9);
        Assert.Equal(-(Egress.DoorStandoffDu + (rank * InspectionTeam.FileSpacingDu)), y, 9);

        if (rank == 0)
        {
            return;
        }

        (double px, double py) = Egress.PlaceInTheFile(0, 0, 0, -1, rank - 1, InspectionTeam.FileSpacingDu);
        double gap = Math.Sqrt(((x - px) * (x - px)) + ((y - py) * (y - py)));
        Assert.Equal(InspectionTeam.FileSpacingDu, gap, 9);
    }

    /// <summary>The direction is a DIRECTION and its length is not part of the answer. A caller handing a
    /// two-unit vector would otherwise silently double every spacing in the queue — which is a file drawn
    /// twice as long as the one the collision was checked against.</summary>
    [Fact]
    public void TheDirectionIsNormalisedAndNeverScalesTheQueue()
    {
        (double ax, double ay) = Egress.PlaceInTheFile(3, 3, -1, 0, 2, 3.0);
        (double bx, double by) = Egress.PlaceInTheFile(3, 3, -17.5, 0, 2, 3.0);
        Assert.Equal(ax, bx, 9);
        Assert.Equal(ay, by, 9);

        // …and a diagonal is a real unit of distance, not a unit per axis.
        (double dx, double dy) = Egress.PlaceInTheFile(0, 0, 1, 1, 0, 3.0);
        Assert.Equal(Egress.DoorStandoffDu, Math.Sqrt((dx * dx) + (dy * dy)), 9);
    }

    /// <summary>A queue with no rank, no spacing or no direction is not a queue, and the answer is a throw
    /// rather than a body placed somewhere plausible.</summary>
    [Fact]
    public void NoneOfThoseThreeMayBeNonsense()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Egress.PlaceInTheFile(0, 0, 1, 0, -1, 3.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Egress.PlaceInTheFile(0, 0, 1, 0, 0, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Egress.PlaceInTheFile(0, 0, 1, 0, 0, -3.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Egress.PlaceInTheFile(0, 0, 0, 0, 0, 3.0));
    }

    /// <summary>
    /// AND THE SWEEP TEAM'S OWN NUMBERS DID NOT MOVE. #946's file runs back down a wreck's spine from
    /// <c>WreckLayout.ShuttleLockX</c> at <c>y = 0</c>, and it was a hand-written subtraction until this lane
    /// replaced it with the call above. Refactoring a shipped queue is exactly where a body quietly steps a
    /// spacing to the wrong side, so the old sum is written out here and compared.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheHullSweepTeamsFileIsTheSameNumberItAlwaysWas(int rank)
    {
        double asItWas =
            WreckLayout.ShuttleLockX - Egress.DoorStandoffDu - (rank * InspectionTeam.FileSpacingDu);
        (double x, double y) = Egress.PlaceInTheFile(
            WreckLayout.ShuttleLockX, 0, -1, 0, rank, InspectionTeam.FileSpacingDu);
        Assert.Equal(asItWas, x, 9);
        Assert.Equal(0, y, 9);
    }
}

/// <summary>
/// #731 · <b>THEIR BOAT, THEIR HATCH.</b> The issue's law — <i>the door opens for the NPC by their own
/// authority exactly where the captain's TRY would fail, and no line of dialog may explain it</i> — stated for
/// a repo crew's own airlock the way <c>WreckLayout.HeldAtLock</c> states it for a wreck's crew-only lock.
/// </summary>
public sealed class TheirBoatIsNotYoursTests
{
    private const double Radius = 0.7;   // DeckPlan.AvatarRadius; Core may not see the client's constant.

    private static double KeepOut => Radius + Egress.DoorStandoffDu;

    /// <summary>A captain who is nowhere near it is not moved at all. A hold that nudges everybody is
    /// furniture, and this one has to be invisible until it is the point.</summary>
    [Theory]
    [InlineData(40.0, 0.0)]
    [InlineData(0.0, 40.0)]
    [InlineData(-9999.0, -9999.0)]
    public void StandingClearOfItChangesNothing(double x, double y)
    {
        (double hx, double hy) = CollectorLanding.HeldOffTheirHatch(x, y, 0, 0, Radius);
        Assert.Equal(x, hx, 9);
        Assert.Equal(y, hy, 9);
    }

    /// <summary>Walking into it puts you back out of it — along the line you came in on, so the push has a
    /// direction a player can read rather than a jitter.</summary>
    [Theory]
    [InlineData(0.5, 0.0)]
    [InlineData(0.0, -1.0)]
    [InlineData(-0.3, 0.4)]
    [InlineData(1.0, 1.0)]
    public void WalkingOntoItPutsYouBackOffIt(double dx, double dy)
    {
        (double hx, double hy) = CollectorLanding.HeldOffTheirHatch(5 + dx, -9 + dy, 5, -9, Radius);
        double range = Math.Sqrt(((hx - 5) * (hx - 5)) + ((hy + 9) * (hy + 9)));
        Assert.Equal(KeepOut, range, 9);

        // Out along the way in, never sideways: the bearing is unchanged.
        double wasBearing = Math.Atan2(dy, dx);
        Assert.Equal(wasBearing, Math.Atan2(hy + 9, hx - 5), 6);
    }

    /// <summary>Standing precisely on it is the one case with no line to push along, and the answer is a
    /// FIXED direction — out on the side the queue is not on, so a captain is never put inside the file.</summary>
    [Fact]
    public void StandingExactlyOnItIsPushedOutTheSideTheQueueIsNotOn()
    {
        (double hx, double hy) = CollectorLanding.HeldOffTheirHatch(5, -9, 5, -9, Radius);
        Assert.Equal(5, hx, 9);
        Assert.Equal(-9 + KeepOut, hy, 9);
    }

    /// <summary>And nothing else is a ground crew. The POSITIVE half of this law cannot be asked here — an
    /// id built out of the same constant the recogniser reads is true by construction, and a guard handed a
    /// world that cannot tell pass from fail is a known bug class in this repository. It is asked on the
    /// client side instead, of the id the game actually mints when a writ is served
    /// (<c>TheRepoCrewGoesHomeTests.THE_IdTheGameMintsIsTheIdTheCodeLooksFor</c>).</summary>
    [Fact]
    public void NothingElseIsAGroundCrew()
    {
        Assert.False(CollectorLanding.IsAGroundCrew("wolf-3"));
        Assert.False(CollectorLanding.IsAGroundCrew("collector-ground"));
        Assert.False(CollectorLanding.IsAGroundCrew(" collector-ground:luna"));
        Assert.False(CollectorLanding.IsAGroundCrew(null));
        Assert.False(CollectorLanding.IsAGroundCrew(""));
    }
}
