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
        // PulseHold's own law, unchanged and re-asked here because this funnel is now its biggest caller:
        // the sentence that survives the card is the sentence that would have been on screen had no card
        // been raised.
        SpaceSails.Client.Pages.Map map = Boot(CanvasId);
        Set(map, "_showSatchel", true);

        Say(map, "THE WHOLE POINT OF THE FEATURE.", PulseRank.Climax);
        Say(map, TheBeat, PulseRank.Beat);

        Set(map, "_showSatchel", false);
        Frame(map);

        Assert.Equal("THE WHOLE POINT OF THE FEATURE.", OnScreen(map));
    }

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
