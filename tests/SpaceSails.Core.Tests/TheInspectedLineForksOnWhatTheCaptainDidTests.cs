using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#1397 · The Inspected death's seeded line never tells a motionless captain he did not stand still.
/// Swept over the seed space (a one-sample guard would pass with the fork removed: only one of the three lines
/// ever said "did not"). <b>RED</b> by dropping the <c>heldStill</c> branch in <c>DeathNarration.Line</c>.</summary>
public sealed class TheInspectedLineForksOnWhatTheCaptainDidTests
{
    [Fact]
    public void NoSeedTellsAMotionlessCaptainHeDidNot()
    {
        bool sawStoodStill = false;
        for (ulong seed = 0; seed < 30; seed++)
        {
            string held = DeathNarration.Line(DeathCause.Inspected, DeathPlace.Derelict, seed, "the Tilt", heldStill: true);
            Assert.DoesNotContain("and did not", held);
            sawStoodStill |= held.Contains("stood still");
        }
        Assert.True(sawStoodStill, "the fork never produced its own line: the guard asserts nothing");

        bool sawDidNot = false;
        for (ulong seed = 0; seed < 30; seed++)
        {
            sawDidNot |= DeathNarration.Line(DeathCause.Inspected, DeathPlace.Derelict, seed, "the Tilt").Contains("and did not");
        }
        Assert.True(sawDidNot, "the control line (a captain who moved) no longer says he did not");
    }

    [Fact]
    public void HeldStillIsAMatterOfAStepNotOfATwitch()
    {
        Assert.True(InspectionTeam.HeldStill(5, 5, 5.2, 5.2));
        Assert.False(InspectionTeam.HeldStill(5, 5, 9, 5));
    }
}
