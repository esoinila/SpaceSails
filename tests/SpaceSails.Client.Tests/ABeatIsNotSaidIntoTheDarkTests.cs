using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1214 · <b>A LINE THE CAPTAIN PAID FOR IS NOT DELIVERED INTO THE DARK.</b>
///
/// <para><b>What was played.</b> Seated at a top at Selene Gate with the man who came in after you in the
/// room, the tail's chair reading fired — the DOM had it, verbatim, <i>"From this chair you can see the
/// door. So can the man who came in after you, and he has not ordered."</i> — drawn in
/// <c>.deck-pulse-toast</c> at (400, 96), <b>underneath the finder card's backdrop at z 1320</b>. On the
/// glass it was a dim smear. The sit was the cost; the reading is the whole payoff; it was spent, drawn and
/// never read. Seen again the same session with an ambient line under the TWO GLASSES card.</para>
///
/// <h3>The audit, because reality differed from the machinery</h3>
///
/// <para>The holding gear already existed and had for a year: #768's <see cref="PulseHold"/>, and the page's
/// <c>ACardStopsTheWorld</c>. <b>Two things were wrong with it, and both had to move.</b></para>
///
/// <list type="number">
///   <item><b>Only events that opted in ever consulted it.</b> <c>HoldAndFile</c> / <c>HoldSaying</c> are for
///   an arrival that raises its OWN card. A beat the world raises somewhere else — the chair reading, the
///   later sighting — goes through <c>ShowPulseMessage</c>, which wrote straight into the slot.</item>
///   <item><b><c>ACardStopsTheWorld</c> named two of thirty-one scrims.</b> Its own docblock warned that a
///   copy of the raising conditions is "a second rule to keep in step"; it was one, and it had fallen behind
///   the page. The finder's card is <c>_finderCard</c> and was on neither side of that <c>||</c>.</item>
/// </list>
///
/// <para>The predicate is now #1052's census (<c>AScrimIsUp</c>), which <c>OnlyOneScrimAtATimeTests</c> keeps
/// honest by walking <c>Map.razor</c> itself — so this law covers a card typed tomorrow with no edit
/// anywhere. <b>The second half of this file is what proves the mirror is really gone:</b> it asserts the
/// hold under scrims that are neither of the two the old rule knew.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ABeatIsNotSaidIntoTheDarkTests
{
    private const string CanvasId = "beat-in-the-dark-canvas";
    private const string TheBeat = "From this chair you can see the door.";
    private const string TheWeather = "〜 A distant tone climbs somewhere past the bulkheads…";

    /// <summary>#1230 · the two sentences the ruling is about, in the game's own words rather than in two
    /// strings composed here. Both are spent ONCE by construction and neither writes a durable record, which
    /// is why the one the old hold threw away was gone with no trace in the save.</summary>
    private const string TheChairReading = TheTailBehindYou.FromThisChairLine;

    private const string TheLosingLine = TheTailBehindYou.LostLine;

    // ── THE REPORTED CASE ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnderTheFINDERSCardAPlotSignificantLineIsHeldAndThenSaid()
    {
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);

        Say(map, TheBeat, PulseRank.Beat);

        Assert.NotEqual(TheBeat, OnScreen(map));       // …not under the scrim, which is where it used to go
        Assert.True(SomethingIsHeld(map), "and not dropped either — the sit paid for it.");

        Invoke(map, "CloseTheFindersCard");
        Frame(map);

        Assert.Equal(TheBeat, OnScreen(map));
        Assert.False(SomethingIsHeld(map), "the hold must empty when it releases, or the line says itself twice.");
    }

    [Fact]
    public void AndItIsSaidExactlyONCE()
    {
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);
        Say(map, TheBeat, PulseRank.Beat);

        Invoke(map, "CloseTheFindersCard");
        Frame(map);
        Assert.Equal(TheBeat, OnScreen(map));

        // …and the frames after it do not say it AGAIN. A second release would write the line afresh, and a
        // fresh write is a fresh dwell — so the expiry is the thing to watch: unchanged means it was said
        // once and is now simply ageing, exactly like every other pulse in the game.
        double saidUntil = (double)Get(Read(map, "_pulse")!, "ExpiresMs")!;
        for (int frame = 0; frame < 5; frame++)
        {
            Frame(map);
        }

        Assert.Equal(TheBeat, OnScreen(map));
        Assert.Equal(saidUntil, (double)Get(Read(map, "_pulse")!, "ExpiresMs")!);
        Assert.False(SomethingIsHeld(map));
    }

    // ── AMBIENT KEEPS TODAY'S BEHAVIOUR ──────────────────────────────────────────────────────────────────

    [Fact]
    public void WeatherIsStillWrittenStraightIntoTheSlot()
    {
        // The law is about plot-significant lines and nothing else. Four hundred instrument, price and
        // refusal lines must not start queueing up behind an open satchel — weather is allowed to be missed,
        // which is what makes it weather (#693's own warning, from the other side).
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);

        Say(map, TheWeather, PulseRank.Status);

        Assert.Equal(TheWeather, OnScreen(map));
        Assert.False(SomethingIsHeld(map));
    }

    [Fact]
    public void AndWithNothingUpAPlotSignificantLineIsSaidOnTheSpot()
    {
        // The anti-vacuous half: a "fix" that held every beat forever would pass every assertion above.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);

        Say(map, TheBeat, PulseRank.Beat);

        Assert.Equal(TheBeat, OnScreen(map));
        Assert.False(SomethingIsHeld(map));
    }

    // ── THE MIRROR IS GONE ───────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("_showSatchel")]
    [InlineData("_galleyCardOpen")]
    [InlineData("_navHelpOpen")]
    [InlineData("_showShipBoard")]
    [InlineData("_showAlarmPanel")]
    [InlineData("_showCaptainsRemote")]
    public void EVERYScrimHoldsIt_NotJustTheTwoTheOldRuleKnew(string gate)
    {
        // Every one of these is a full-viewport backdrop in TheScrimCensus, and not one of them is
        // `_viewObject` or `_storyCard`. Before #1214 a beat raised under any of them went straight to a HUD
        // nobody could see.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        Set(map, gate, true);
        Assert.True(AScrimIsUp(map), $"{gate} is supposed to be a scrim, and the census does not think so.");

        Say(map, TheBeat, PulseRank.Beat);
        Assert.NotEqual(TheBeat, OnScreen(map));
        Assert.True(SomethingIsHeld(map));

        Set(map, gate, false);
        Frame(map);

        Assert.Equal(TheBeat, OnScreen(map));
    }

    [Fact]
    public void TheHeldWinnerIsChosenByRankAndNotByOrder()
    {
        // PulseHold's own law, re-asked here because this funnel is its biggest caller: the FIRST sentence
        // said when the glass clears is the sentence that would have been on screen had no card been raised.
        // #1230 · and it is the first of several now rather than the only one — see the queue guards below.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        Set(map, "_showSatchel", true);

        Say(map, "THE WHOLE POINT OF THE FEATURE.", PulseRank.Climax);
        Say(map, TheBeat, PulseRank.Beat);

        Set(map, "_showSatchel", false);
        Frame(map);

        Assert.Equal("THE WHOLE POINT OF THE FEATURE.", OnScreen(map));
    }

    // ── #1230 · AND THE HOLD IS A QUEUE ──────────────────────────────────────────────────────────────────

    [Fact]
    public void TWOBeatsRaisedSecondsApartUnderOneCardAreBOTHSaidInOrder()
    {
        // The ruling's case, driven through the page that actually holds them. Under the one-slot body the
        // second beat annihilated the first while the card was still up, and the captain — who had paid for
        // both — read one of the two. The card is open across both raisings and closes once, which is the
        // shape #1231 measured: the tail's chair reading and its losing line, nine seconds apart, under the
        // finder's pitch. Neither of them writes a durable record, so the one that lost was simply gone.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);

        Say(map, TheChairReading, PulseRank.Beat);
        RunFrames(map, 9.0);                                // the tail's own nine seconds, on real frames
        Say(map, TheLosingLine, PulseRank.Beat);

        Assert.Equal(2, HowManyAreWaiting(map));

        Invoke(map, "CloseTheFindersCard");
        Assert.Equal(
            [TheChairReading, TheLosingLine],
            WhatTheCaptainReads(map, [TheChairReading, TheLosingLine], seconds: 40.0));
    }

    [Fact]
    public void EachHeldLineGetsItsOwnFullDwellBeforeTheNextTakesTheSlot()
    {
        // "One at a time, each for its own full duration, the next only after the previous expires." So the
        // first line is still the one on screen for its whole dwell after the card closes, and the second has
        // not jumped it — a release that drained the hold in one frame would show only the last one.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);
        Say(map, TheChairReading, PulseRank.Beat);
        Say(map, TheLosingLine, PulseRank.Beat);

        Invoke(map, "CloseTheFindersCard");
        Frame(map);
        Assert.Equal(TheChairReading, OnScreen(map));

        // Up to a frame short of the first line's own dwell, the SECOND one has not taken the slot and is
        // still waiting its turn. (What is asserted is the second line's absence rather than the first line's
        // presence: the ship's weather may legitimately take the slot from a beat once its breath is up, and
        // that is #766's dwell law, not this one's business.)
        double until = (double)Get(Read(map, "_pulse")!, "ExpiresMs")!;
        while (Convert.ToDouble(Read(map, "_lastTimestampMs")) < until - (FrameSeconds * 1000))
        {
            Frame(map);
            Assert.NotEqual(TheLosingLine, OnScreen(map));
            Assert.True(SomethingIsHeld(map), "the second line left the queue before the first had its time.");
        }

        RunFrames(map, 1.0);
        Assert.Equal(TheLosingLine, OnScreen(map));
    }

    [Fact]
    public void TheSAMESentenceRaisedTwiceBehindOneCardIsSaidOnce()
    {
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);

        Say(map, TheChairReading, PulseRank.Beat);
        RunFrames(map, 2.0);
        Say(map, TheChairReading, PulseRank.Beat);

        Assert.Equal(1, HowManyAreWaiting(map));

        Invoke(map, "CloseTheFindersCard");
        Assert.Equal([TheChairReading], WhatTheCaptainReads(map, [TheChairReading], seconds: 40.0));
    }

    [Fact]
    public void WeatherRaisedBehindACardNeverJoinsTheQueueAtAll()
    {
        // Clause 4, at the funnel: the queue is for lines at the floor and above, and four hundred
        // instrument, price and refusal lines do not start piling up behind an open satchel. The weather
        // goes straight into the slot nobody can see, exactly as it always did, and is simply missed.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);

        Say(map, TheBeat, PulseRank.Beat);
        for (int i = 0; i < 20; i++)
        {
            Say(map, $"{TheWeather} {i}", PulseRank.Status);
        }

        Assert.Equal(1, HowManyAreWaiting(map));

        Invoke(map, "CloseTheFindersCard");
        Assert.Equal([TheBeat], WhatTheCaptainReads(map, [TheBeat], seconds: 40.0));
    }

    [Fact]
    public void EightBeatsBehindACardLeftOpenAllEveningAreALLSaid()
    {
        // The soft bound, at the page. Eight beats is a card the captain walked away from; not one of them
        // may be dropped on the way, and they come out in the order the evening raised them.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        RaiseTheFindersCard(map);

        var raised = new List<string>();
        for (int i = 0; i < PulseHold.TheBound; i++)
        {
            string line = $"🕵 Something once-only happened, number {i}.";
            raised.Add(line);
            Say(map, line, PulseRank.Beat);
            RunFrames(map, 1.0);
        }

        Assert.Equal(PulseHold.TheBound, HowManyAreWaiting(map));

        Invoke(map, "CloseTheFindersCard");
        Assert.Equal(raised, WhatTheCaptainReads(map, raised, seconds: 180.0));
    }

    /// <summary>Which of OUR sentences a captain who closes the card and then watches the HUD actually reads,
    /// in order, with a line still on screen not counted twice — the page's own frames, the page's own dwell,
    /// nothing set by hand.
    ///
    /// <para>The world keeps talking while he watches: <c>StepTheAmbientBeats</c> runs every frame and the
    /// ship's weather takes the slot between our lines exactly as it always has. That is the behaviour clause
    /// 4 preserves, so it is filtered out here rather than suppressed — a bench with the weather turned off
    /// would be proving the law in a world the game does not build.</para></summary>
    private static List<string> WhatTheCaptainReads(
        SpaceSails.Client.Pages.Map map, IReadOnlyCollection<string> ours, double seconds)
    {
        var read = new List<string>();
        for (int frame = 0; frame < (int)(seconds / FrameSeconds); frame++)
        {
            Frame(map);
            if (OnScreen(map) is { } line && ours.Contains(line) && (read.Count == 0 || read[^1] != line))
            {
                read.Add(line);
            }
        }

        Assert.False(SomethingIsHeld(map),
            $"{HowManyAreWaiting(map)} line(s) never made it onto the glass in {seconds} s.");
        return read;
    }

    private static int HowManyAreWaiting(SpaceSails.Client.Pages.Map map) =>
        (int)Get(Read(map, "_held")!, "Count")!;

    // ── How this file drives and reads the page ──────────────────────────────────────────────────────────

    private static void Say(SpaceSails.Client.Pages.Map map, string line, PulseRank rank) =>
        Invoke(map, "ShowPulseMessage", line, rank);

    private static string? OnScreen(SpaceSails.Client.Pages.Map map) =>
        (string?)Get(Read(map, "_pulse")!, "Message");

    private static bool SomethingIsHeld(SpaceSails.Client.Pages.Map map) =>
        (bool)Get(Read(map, "_held")!, "Any")!;

    private static bool AScrimIsUp(SpaceSails.Client.Pages.Map map) =>
        (bool)typeof(SpaceSails.Client.Pages.Map)
            .GetProperty("AScrimIsUp", Hidden)!.GetValue(map)!;

    /// <summary>The card the issue was filed about, in the page's own field — the gate the census reads is
    /// <c>_finderCard is { } finderAsks</c>, so what raises the scrim is the field being SET and nothing about
    /// the case inside it. This file is about the scrim; what Ilse Varga is asking for is
    /// <c>TheFinderBringsACaseTests</c>' business and is deliberately not restated here.</summary>
    private static void RaiseTheFindersCard(SpaceSails.Client.Pages.Map map)
    {
        Set(map, "_finderCard", (default(FinderCase.Case), false));
        Assert.True(AScrimIsUp(map), "the finder's card draws a full-viewport scrim and the census missed it.");
    }
}
