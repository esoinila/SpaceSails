using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#1357 - the one story-hull predicate: the charter hull and the old ship are in it, ordinary traffic is not.</summary>
public sealed class StoryHullsTests
{
    [Fact]
    public void TheCharterHullIsAStoryHull() =>
        Assert.True(StoryHulls.IsOne(ReturningShuttle.ShipIdFor("luna")));

    [Fact]
    public void TheOldShipIsAStoryHull() =>
        Assert.True(StoryHulls.IsOne(TheOldShip.ShipId));

    [Theory]
    [InlineData("npc-3")]
    [InlineData("depot-luna")]
    [InlineData("")]
    [InlineData(null)]
    public void OrdinaryTrafficIsNot(string? id) => Assert.False(StoryHulls.IsOne(id));
}
