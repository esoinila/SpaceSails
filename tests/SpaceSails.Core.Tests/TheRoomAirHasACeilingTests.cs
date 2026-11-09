using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// U09-F03 (#1423, owner ruling 2026-11-09) · THE ROOM AIR GIVES NERVE BACK ONLY AFTER LOW AIR, AND ONLY TO
/// ABOUT 70% (7 of 10 pips). The chair and the rest give the remainder.
/// </summary>
public sealed class TheRoomAirHasACeilingTests
{
    /// <summary>Hold a captain safe on a pressurised floor for <paramref name="beats"/> whole ease beats.</summary>
    private static (int Pips, int Events) Rest(int startPips, bool airWasLow, int beats)
    {
        double n = NervePips.FromPips(startPips);
        var clock = NervePips.Beats.Fresh;
        int events = 0;
        for (int i = 0; i < beats; i++)
        {
            var f = new NervePips.Frame(
                OnExcursion: true, OnRegolith: false, SeesMonolith: false, Stressors: default,
                FreshSightings: 0, Touched: false, DtSeconds: NervePips.AirlockBeatSeconds,
                SafeGroundHoldsPressure: true, AirWasLow: airWasLow);
            NervePips.Step s = NervePips.Advance(n, false, clock, in f);
            n = s.Nerve;
            clock = s.Beats;
            events += s.Events.Count;
        }
        return (NervePips.PipsOf(n), events);
    }

    [Fact]
    public void TheCapIsSevenPipsOfTen() => Assert.Equal(7, NervePips.RoomAirCapPips);

    [Fact]
    public void WithoutALowAirSpellTheFloorGivesNothing()
    {
        (int pips, int events) = Rest(startPips: 2, airWasLow: false, beats: 20);
        Assert.Equal(2, pips);
        Assert.Equal(0, events);
    }

    [Fact]
    public void AfterALowAirSpellTheFloorEasesToExactlyTheCapAndStops()
    {
        (int pips, int events) = Rest(startPips: 2, airWasLow: true, beats: 30);
        Assert.Equal(NervePips.RoomAirCapPips, pips);
        Assert.Equal(NervePips.RoomAirCapPips - 2, events);
    }

    [Fact]
    public void ALongFrameCannotEaseThePipsPastTheCapInOneStep()
    {
        // The in-loop break in Advance: a frame of three beats starting at six pips owes three eases, and
        // the second would carry the nerve to eight. It must stop at exactly the cap.
        double n = NervePips.FromPips(NervePips.RoomAirCapPips - 1);
        var f = new NervePips.Frame(
            OnExcursion: true, OnRegolith: false, SeesMonolith: false, Stressors: default,
            FreshSightings: 0, Touched: false, DtSeconds: 3 * NervePips.AirlockBeatSeconds,
            SafeGroundHoldsPressure: true, AirWasLow: true);
        NervePips.Step s = NervePips.Advance(n, false, NervePips.Beats.Fresh, in f);
        Assert.Equal(NervePips.RoomAirCapPips, NervePips.PipsOf(s.Nerve));
        Assert.Single(s.Events);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    public void AtOrAboveTheCapTheFloorTouchesNothing(int startPips)
    {
        (int pips, int events) = Rest(startPips, airWasLow: true, beats: 20);
        Assert.Equal(startPips, pips);
        Assert.Equal(0, events);
    }

    [Fact]
    public void TheShipsAndTheAirlocksEaseIsUnchanged()
    {
        // Not a pressurised floor: the plain ease still runs to full, with or without a low-air spell.
        double n = NervePips.FromPips(2);
        var clock = NervePips.Beats.Fresh;
        for (int i = 0; i < 30; i++)
        {
            var f = new NervePips.Frame(true, false, false, default, 0, false,
                NervePips.AirlockBeatSeconds, SafeGroundHoldsPressure: false, AirWasLow: false);
            NervePips.Step s = NervePips.Advance(n, false, clock, in f);
            n = s.Nerve;
            clock = s.Beats;
        }
        Assert.Equal(NervePips.MaxPips, NervePips.PipsOf(n));
    }

    [Fact]
    public void TheLowAirTestIsThePlainTankFraction()
    {
        Assert.True(SuitAir.TankIsLow(SuitAir.TankSeconds * SuitAir.LowAirFraction));
        Assert.False(SuitAir.TankIsLow(SuitAir.TankSeconds * SuitAir.LowAirFraction + 1));
    }

    /// <summary>The #1423 repro: ?nerve=low (2 pips), full air. The floor gives nothing, so the chair's rest
    /// has the whole climb to give.</summary>
    [Fact]
    public void TheReproFloorLeavesTheChairItsWholeClimb()
    {
        (int pips, _) = Rest(startPips: 2, airWasLow: false, beats: 30);
        Assert.Equal(2, pips);

        // ...and the chair, sat in over the watch, now visibly gives pips back from that same 2.
        int given = 0;
        for (int beat = 0; beat < 40; beat++)
        {
            given += ShortRest.Beat(beat, withDrink: false, given, 0, 0).NervePips;
        }
        Assert.True(given > 0, "the short rest gave the captain nothing back.");
        Assert.True(pips + given > pips);
    }
}
