using System;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>Fable ruling (c) · the underground low-air card never raises on a floor that holds air: there is
/// always a floor to climb, so "this floor" is not a sentence the cards can say.</summary>
public class TheDeadAirCardsNeverRaiseOnBreathingGroundTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TheUndergroundFaceRefusesToSayThisFloor(int floorsUp)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AirCard.HeadUnderground("Floor 0", floorsUp));
        Assert.Throws<ArgumentOutOfRangeException>(() => AirCard.For(true, "Floor 0", floorsUp));
    }

    [Fact]
    public void OneFloorUpStillReads()
    {
        Assert.Contains("1 floor up", AirCard.HeadUnderground("Floor 0", 1), StringComparison.Ordinal);
        Assert.DoesNotContain("The air is this floor", AirCard.HeadUnderground("Floor 0", 2), StringComparison.Ordinal);
    }

    [Fact]
    public void TheDeadAirSumsRefuseABreathingFloor()
    {
        // The top of every band holds air (refuge == level): the vacuum card has no business there.
        for (int level = -1; level >= -40; level--)
        {
            if (UndergroundComplex.HoldsPressure("miranda", level))
            {
                Assert.ThrowsAny<ArgumentException>(() => UndergroundComplex.VacuumCard("miranda", level, 600));
                return;
            }
        }
        Assert.Fail("no breathing floor found to test against.");
    }
}
