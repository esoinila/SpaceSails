using System;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Round 2 / U06 (#1420) · two Core pins. U06-F03: the plate card's floor count must be the building's own
/// (the pulse right after it said "7 storeys" under a card that said "twenty"). U06-F09: the big red O2
/// counter must go out while the tank is not drawing.
/// </summary>
public sealed class TheRound2U06CorePinsTests
{
    private static readonly string[] Words =
    [
        "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven",
        "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
        "twenty", "twenty-one", "twenty-two", "twenty-three", "twenty-four",
    ];

    [Fact]
    public void ThePlateCardCountsTheFloorsTheBuildingReallyListsOnEverySite()
    {
        int sawNotTwenty = 0;
        for (int i = 0; i < 300; i++)
        {
            string body = $"rock-{i}";
            int listed = -UndergroundComplex.DepthOf(body);
            string card = UndergroundComplex.UnlistedLobbyCardFor(body);
            Assert.Contains($"Above you, {Words[listed]} floors file and grade", card, StringComparison.Ordinal);
            if (listed != 20)
            {
                sawNotTwenty++;
                Assert.DoesNotContain("twenty floors", card, StringComparison.Ordinal);
            }
        }

        Assert.True(sawNotTwenty > 20, "the sweep never met a shallow site, so it proved nothing.");
    }

    [Fact]
    public void ATwentyFloorListingReadsTheAuthoredCardByteForByte()
    {
        for (int i = 0; i < 600; i++)
        {
            string body = $"rock-{i}";
            if (UndergroundComplex.DepthOf(body) == -20)
            {
                Assert.Equal(UndergroundComplex.UnlistedLobbyCard, UndergroundComplex.UnlistedLobbyCardFor(body));
                return;
            }
        }

        Assert.Fail("no twenty-floor site in the sweep.");
    }

    [Fact]
    public void TheRedClockIsOutWhenTheTankIsNotDrawingAndOnWhenItIs()
    {
        double onTheReserve = SuitAir.ReserveSeconds / 2;
        Assert.True(SuitAir.CountdownShows(onTheReserve, 10, SuitAir.Supply.Tanks));
        Assert.False(SuitAir.CountdownShows(onTheReserve, 10, SuitAir.Supply.Room));
        Assert.False(SuitAir.CountdownShows(onTheReserve, 10, SuitAir.Supply.Ship));
        Assert.False(SuitAir.CountdownShows(SuitAir.TankSeconds, 0, SuitAir.Supply.Tanks));
    }
}
