using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Wednesday plan §3 PR-F, the owner's ruling ("People cannot be static furniture. They change place
/// and go behind locked doors or move"): an interior NPC's position must be a pure, deterministic
/// function of sim time (repo agreement §9) so the two renderers and the interaction gate all agree,
/// and so "talk to them, come back a slice later, they've moved" is testable without a browser.
/// </summary>
public class NpcScheduleTests
{
    private static NpcSchedule ThreePostRota(double dur = 100) => new("magpie", dur,
    [
        new NpcPost("BAR", 8, 72, 0, Present: true),
        new NpcPost("GONE", 0, 0, 0, Present: false),
        new NpcPost("BACK ROOM", -24, 31, 0, Present: true),
    ]);

    [Fact]
    public void Resolve_WalksTheRotaOnSliceBoundaries()
    {
        NpcSchedule s = ThreePostRota(dur: 100);

        Assert.Equal("BAR", s.Resolve(0).Location);
        Assert.Equal("BAR", s.Resolve(99.9).Location);
        Assert.Equal("GONE", s.Resolve(100).Location);
        Assert.Equal("BACK ROOM", s.Resolve(200).Location);
        // Cycles back round on the fourth slice.
        Assert.Equal("BAR", s.Resolve(300).Location);
        Assert.Equal("GONE", s.Resolve(450).Location);
    }

    [Fact]
    public void Resolve_GoneSlotIsNotPresent()
    {
        NpcSchedule s = ThreePostRota(dur: 100);

        Assert.True(s.Resolve(0).Present);       // at the bar, found
        Assert.False(s.Resolve(150).Present);    // stepped out — nothing for the deck to draw
        Assert.True(s.Resolve(250).Present);     // reappears in the back room
    }

    [Fact]
    public void Resolve_IsDeterministic_SameClockSameAnswer()
    {
        NpcSchedule a = ThreePostRota();
        NpcSchedule b = ThreePostRota();

        for (double t = 0; t < 1000; t += 37.5)
        {
            Assert.Equal(a.Resolve(t), b.Resolve(t));
        }
    }

    [Fact]
    public void SliceIndex_HandlesNegativeAndLargeTimes()
    {
        NpcSchedule s = ThreePostRota(dur: 100);

        Assert.Equal(0, s.SliceIndex(0));
        Assert.Equal(2, s.SliceIndex(250));
        Assert.InRange(s.SliceIndex(-50), 0, 2);      // never indexes out of range
        Assert.InRange(s.SliceIndex(1e9), 0, 2);
    }

    [Fact]
    public void PostAt_WrapsIntoRange()
    {
        NpcSchedule s = ThreePostRota();

        Assert.Equal("BAR", s.PostAt(0).Location);
        Assert.Equal("BACK ROOM", s.PostAt(2).Location);
        Assert.Equal("BAR", s.PostAt(3).Location);    // wraps
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_RejectsBadDuration(double dur)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NpcSchedule("x", dur,
            [new NpcPost("A", 0, 0, 0, true)]));
    }

    [Fact]
    public void Constructor_RejectsEmptyRota()
    {
        Assert.Throws<ArgumentException>(() => new NpcSchedule("x", 100, []));
    }
}
