using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1199 · <b>"BEHIND THE COUNTER THE STAFF GO STILL" NEEDS A COUNTER.</b>
///
/// <para><b>What was played.</b> Inspector, live on Selene Gate (2026-09-18), tailing GILT-EYE out of THE
/// EARTHRISE BAR: the unexplained-signal line <i>"A distant tone climbs somewhere past the bulkheads, holds,
/// and cuts out. Behind the counter the staff go still as one and trade a single glance — then, wordlessly,
/// they carry on. The drinkers never look up."</i> fired with the captain out on the CONCOURSE.</para>
///
/// <para><b>Why it happened.</b> <c>StepSignal</c>'s gate was <c>_deckMode &amp;&amp; !_shuttleDescending
/// &amp;&amp; _surface is null</c> — its own comment called that "a populated interior only", which is the
/// ship's whole deck, a haven's concourse, its immigration hall and its observation walk as well as the bar.
/// Every one of the beat's six lines is made of bar furniture: a counter, a barkeep, a dock-hand, drinkers,
/// glasses being wiped. <b>This is #1215's class at ambient rank</b> — the same floor, the same month, and
/// the same shape: a beat raised from the far end of the game with no room in its hand.</para>
///
/// <para><b>The fix is #1222's predicate and not a second one</b> (<c>TheCaptainIsInTheDockedBar</c>), so a
/// room that moves moves for every beat in it at once. <b>No prose changed:</b> the lines are good and they
/// are about a bar; what was wrong was where they were said.</para>
///
/// <para>Asked from both sides, because a guard that only watched the concourse would be satisfied by a beat
/// that never fired anywhere at all.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ABarRoomLineNeedsTheBarRoomTests
{
    private const string CanvasId = "ambient-room-canvas";

    private static double TheBarsFloorY =>
        HavenInterior.BarBand(Port)?.FloorY
        ?? throw new InvalidOperationException($"{Port} draws no bar band, so there is no room to be in.");

    // ── THE LAW, FROM BOTH SIDES ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InTheBarTheBuzzerStillSounds()
    {
        // The anti-vacuous half, and it comes first: a beat that could not fire at all would make every
        // refusal below green about nothing.
        SpaceSails.Client.Pages.Map map = ADockedBar();
        StandHim(map, inTheBar: true);

        OneFrameOfTheSignalsClock(map, seconds: HullShudder.SignalMeanGapSeconds * 4);

        Assert.True((bool)Read(map, "_signalActive")!, "the buzzer never sounded in the room it is about.");
        Assert.Equal(1, (int)Read(map, "_signalIndex")!);   // …and the beat was SPENT, not merely armed
    }

    [Fact]
    public void OutOnTheConcourseItDoesNot()
    {
        SpaceSails.Client.Pages.Map map = ADockedBar();
        StandHim(map, inTheBar: false);

        OneFrameOfTheSignalsClock(map, seconds: HullShudder.SignalMeanGapSeconds * 4);

        Assert.False((bool)Read(map, "_signalActive")!,
            "a line about the staff behind the counter was said to a captain out on the concourse.");
    }

    [Fact]
    public void AndSteppingOutOfTheRoomRESETSTheClockRatherThanBankingIt()
    {
        // The half a "does it fire?" pair cannot reach. The gate's else-branch clears the schedule, so a
        // captain who walks the concourse for an hour and then comes back in does not walk into a buzzer
        // that has been saving itself up for him — which would be the room's rhythm keeping time in a room
        // the captain was not in.
        SpaceSails.Client.Pages.Map map = ADockedBar();
        StandHim(map, inTheBar: false);

        OneFrameOfTheSignalsClock(map, seconds: HullShudder.SignalMeanGapSeconds * 4);

        Assert.Equal(0.0, (double)Read(map, "_signalSeconds")!);
        Assert.Equal(0, (int)Read(map, "_signalIndex")!);
    }

    // ── THE AUDIT: WHICH AMBIENT POOLS NAME FURNITURE, AND WHICH ROOMS THEY ARE SCOPED TO ───────────────

    /// <summary>
    /// #1199 · <b>THE SIGNAL POOL IS THE BAR'S, AND IT SAYS SO IN EVERY LINE.</b>
    ///
    /// <para>The ruling asked for the other ambient pools to be audited for room-specific furniture and
    /// tabled. They are tabled in the PR body; this is the half of that audit a file can hold, because it is
    /// the half that can rot: every line of the pool this lane re-scoped must go on being about a bar, or the
    /// scope is wrong in the other direction and a beat has been taken away from rooms that wanted it.</para>
    ///
    /// <para><b>The shudder pools are NOT swept for this</b>, and deliberately. They are already scoped by
    /// <see cref="HullShudder.Setting"/> — #590 gave the regolith its own voice after a vacuum line spoke on a
    /// lawn, and #867 gave pressurised ground its own after the same bug one scale down — so each of those
    /// pools has a room and a guard of its own. The signal pool had no setting at all, which is why it is the
    /// one that needed a room.</para>
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryLineOfTheSignalPoolIsAboutABar(bool cold)
    {
        // The room's own PEOPLE and FIXTURES — measured off the pool rather than wished at it. "room" and
        // "bar" are deliberately NOT in this list: they are words any interior can say, and a sweep that
        // accepted them would pass a line that had been quietly rewritten to be about nowhere.
        string[] barFurniture =
            ["counter", "barkeep", "dock-hand", "drinkers", "glasses", "crew", "staff"];

        IReadOnlyList<string> pool = HullShudder.SignalLinesFor(cold);
        Assert.NotEmpty(pool);
        Assert.All(pool, line => Assert.True(
            barFurniture.Any(word => line.Contains(word, StringComparison.OrdinalIgnoreCase)),
            $"this line names no bar furniture, so scoping the pool to the bar has taken it away from "
            + $"somewhere it belonged:\n  {line}"));
    }

    // ── The room, and the two places to stand in it ──────────────────────────────────────────────────────

    private static SpaceSails.Client.Pages.Map ADockedBar()
    {
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);

        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        Assert.True(HavenInterior.HasInterior(Port), $"{Port} has no interior, so it has no bar.");
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        return map;
    }

    /// <summary>North of the bar's own south wall, or south of it — the one wall <c>InTheBar</c> reads, taken
    /// off the room the game draws. Both places are ASHORE, which is what makes the pair a fair test: the
    /// difference between them is the room and nothing else.</summary>
    private static void StandHim(SpaceSails.Client.Pages.Map map, bool inTheBar)
    {
        Set(map, "_avatarY", inTheBar ? TheBarsFloorY + 6.0 : TheBarsFloorY - 6.0);
        Invoke(map, "RefreshAshore");
        Assert.True((bool)Read(map, "_ashore")!, "the captain never got past the tube.");
    }

    /// <summary>Hand the page's own signal clock a single long frame — enough real seconds that the schedule
    /// is due however it jittered — through the shipping <c>StepSignal</c>, so what is under test is the road
    /// to the beat and not the beat's own door.</summary>
    private static void OneFrameOfTheSignalsClock(SpaceSails.Client.Pages.Map map, double seconds)
    {
        // The step clamps a frame to MaxShudderStepSeconds, so the clock is walked rather than jumped.
        for (double spent = 0; spent < seconds; spent += 0.25)
        {
            Invoke(map, "StepSignal", 0.25, 10_000.0 + spent);
            if ((bool)Read(map, "_signalActive")!)
            {
                return;
            }
        }
    }
}
