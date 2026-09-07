using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #989 · <b>A DEPARTURE IS SCHEDULED, NOT REQUESTED</b> — sections (4) and (4b) of
/// <see cref="TheCastOffIsAStepTests"/>.
///
/// <para>What this part owns is the cast-off that sits at a scrub epoch rather than happening now: she stays
/// clamped until the epoch and then casts off and clears, the banner says she is waiting, the countdown is
/// quoted at the band it is in (and no wait a plan can hold is ever quoted as zero hours), a burn placed
/// before the cast-off breaks the plan's shape and wakes the captain once, the pair adds once however many
/// times it is pressed and comes off together, and the plotted course starts from the BERTH at the undock
/// epoch rather than from wherever she is now.</para>
/// </summary>
public sealed partial class TheCastOffIsAStepTests
{
    // ── (4) #989 — A DEPARTURE IS SCHEDULED, NOT REQUESTED ─────────────────────────────────────────────
    //
    // Owner, docked at The Red Eye at 324d 16h 02m with the scrub 33 h out (2026-08-22): "Cast off time says
    // in zero hours here even though the scrub is in 33 hours?" — both rows were stamped at NOW+1 min and
    // NOW+2 min, and the banner echoed "casting off from The Red Eye in 0 h" while the ship sat on the clamp.
    // Fable's ruling: the pair is placed AT THE SCRUB, like every other step added at scrub.

    /// <summary>
    /// (a) THE PAIR IS LAID WHERE THE FINGER IS. Scrub 33 h out, press ⚓ + Cast off: the clamp row takes the
    /// scrub's own epoch (the clearance keeping its minute behind it), the rows count the real wait, and the
    /// banner says she is WAITING at the berth — not "casting off in 0 h" while tied up.
    ///
    /// <para>RED on the commit before this fix: <c>AddCastOffAtTop</c> stamped <c>NodeEpochFloor()</c>, so the
    /// undock landed 60 s out however far the scrub had been dragged, and <c>CastOffNowLine</c> said "casting
    /// off … in 33 h" — the ship reporting an act it was not performing.</para>
    /// </summary>
    [Fact]
    public void SCHEDULED_33_HOURS_OUT_TheCastOffSitsAtTheScrub_AndTheBannerSaysSheIsWaiting()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        double scrubOut = 33 * 3600.0;
        Set(map, "_scrubOffsetSeconds", scrubOut);

        Invoke(map, "AddCastOffAtTop");

        object undock = FirstNodeOfKind(map, PlanStepKind.Undock);
        object clear = FirstNodeOfKind(map, PlanStepKind.ClearHarbour);
        _out.WriteLine($"scrub {scrubOut / 3600:F0} h → undock at {NodeEpochOf(undock) / 3600:F2} h, "
                       + $"clearance at {NodeEpochOf(clear) / 3600:F2} h");

        // THE FIX, stated: the clamp lets go at the scrub, and the pair keeps its own one-minute spacing.
        Assert.Equal(Math.Floor(scrubOut), NodeEpochOf(undock), 3);
        Assert.Equal(Math.Floor(scrubOut) + 60, NodeEpochOf(clear), 3);

        // …so the row's own countdown reads the truth rather than "in 0 h" — and, since the clock's own
        // bands were fixed (#989 clock), it reads it in the unit the captain typed: THIRTY-THREE HOURS, not
        // the "in 1 d" the old whole-days branch rounded his afternoon down to.
        var glance = (string)Invoke(map, "PlanStepGlanceLine", undock)!;
        _out.WriteLine($"row: {glance}");
        Assert.Contains("in 33 h", glance);
        Assert.DoesNotContain("in 0 h", glance);
        Assert.DoesNotContain("in 1 d", glance);

        // And the banner: she is tied up, the captain has her, and the plan lets go at its own hour.
        AssertTheBannerSays(map, "YOU HAVE THE SHIP");
        AssertTheBannerSays(map, "docked at Selene Gate");
        AssertTheBannerSays(map, "the plan casts off in 33 h");
        AssertTheBannerDoesNotSay(map, "casting off from");
        AssertTheBannerNamesTheCastOffOnce(map);
    }

    // ── (4b) #989 clock — THE COUNTDOWN IS QUOTED AT THE BAND IT IS IN ────────────────────────────────
    //
    // The scheduling was only half of the owner's complaint; the other half was the DISPLAY. PR #990 laid
    // the cast-off at the scrub, but the row still read it through a formatter that knew two units: whole
    // hours under a day, whole days over it. So a plan laid one minute out said "in 0 h" — the clock
    // reporting nothing where something was about to happen — and the owner's 33 h scrub said "in 1 d".
    // These pin the ladder AND the surface it reaches, because a formatter nobody's row calls is not a fix.

    /// <summary>
    /// THE LADDER, unit by unit. Seconds under a minute, minutes under an hour, hours under two days,
    /// days-and-hours under a month, and the horizon's own idiom past that.
    ///
    /// <para>RED on the commit before this fix (watched, and the whole point of the pin): the old body was
    /// <c>seconds &lt; 86400 ? $"{seconds / 3600:F0} h" : FormatHorizon(seconds)</c> — two units and a
    /// rounding. Under a day everything became whole hours (45 s → "0 h", 60 s → "0 h", 42 m → "1 h"); over
    /// a day everything fell through to whole DAYS (33 h → "1 d", 47 h → "2 d", 3 d 7 h → "3 d"). Six of the
    /// eight rows below go red on that body; 1 h and 120 d are the two the ladder deliberately keeps, so the
    /// pin also says what did NOT change.</para>
    /// </summary>
    [Theory]
    [InlineData(45.0, "45 s")]
    [InlineData(60.0, "1 m")]              // the never-"0 h" case, at the exact epoch floor a plan uses
    [InlineData(42 * 60.0, "42 m")]
    [InlineData(3600.0, "1 h")]
    [InlineData(33 * 3600.0, "33 h")]      // the owner's own screenshot, in his own unit
    [InlineData(47 * 3600.0, "47 h")]      // still an "afternoon-and-a-bit", still hours
    [InlineData((3 * 86400.0) + (7 * 3600.0), "3 d 7 h")]
    [InlineData(120 * 86400.0, "120 d")]   // past a month the hour is noise; FormatHorizon takes over
    public void ThePlansClockIsQuotedAtTheBandItIsIn(double seconds, string expected)
    {
        string said = TheClockSays(seconds);
        _out.WriteLine($"{seconds:F0} s → \"{said}\"");
        Assert.Equal(expected, said);
    }

    /// <summary>
    /// AND NEVER "0 h", at any wait a plan can actually hold. The old body said "0 h" for everything under
    /// half an hour — which is precisely the band a freshly laid cast-off sits in.
    /// </summary>
    [Fact]
    public void NoWaitAPlanCanHoldIsEverQuotedAsZeroHours()
    {
        foreach (double seconds in new[] { 1.0, 30.0, 60.0, 90.0, 600.0, 1799.0, 2520.0, 3599.0 })
        {
            string said = TheClockSays(seconds);
            _out.WriteLine($"{seconds:F0} s → \"{said}\"");
            Assert.DoesNotContain("0 h", said);
            Assert.NotEqual("0 m", said);
        }
    }

    /// <summary>
    /// THE WIRE, not just the formatter: the shipping cast-off row and the shipping banner, on a real
    /// clamped ship, at the smallest wait the plan can hold — one minute. The row says "in 1 m" and the
    /// banner says she is waiting a minute; neither says "in 0 h". (This is the assertion that would have
    /// caught the owner's first screenshot, and it goes red on the old formatter with the fix's scheduling
    /// already in place — which is exactly the state PR #990 shipped.)
    /// </summary>
    [Fact]
    public void ONE_MINUTE_OUT_TheRowAndTheBannerSayAMinute_NotZeroHours()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        Set(map, "_scrubOffsetSeconds", 0.0);       // scrub at now → the plan's own 60 s floor

        Invoke(map, "AddCastOffAtTop");

        object undock = FirstNodeOfKind(map, PlanStepKind.Undock);
        Assert.Equal(60.0, NodeEpochOf(undock), 3);

        var glance = (string)Invoke(map, "PlanStepGlanceLine", undock)!;
        _out.WriteLine($"row: {glance}");
        Assert.Contains("in 1 m", glance);
        Assert.DoesNotContain("0 h", glance);

        AssertTheBannerSays(map, "the plan casts off in 1 m");
        AssertTheBannerDoesNotSay(map, "0 h");
    }

    /// <summary>Ask the shipping page's own duration clock — the one every plan row, banner countdown and
    /// cast-off line is worded through — rather than a copy of it living in the test.</summary>
    private static string TheClockSays(double seconds) =>
        (string)typeof(Pages.Map)
            .GetMethod("FormatDuration", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [seconds])!;

    /// <summary>
    /// (b) SHE WAITS, THEN SHE LEAVES — WITH NOBODY AT THE CONSOLE. The same scheduled departure, warped
    /// through with zero input: she is still clamped a day later, the clamp lets go AT the epoch (measured,
    /// within one of the frame loop's own quanta), and the clearance fires behind it.
    /// </summary>
    [Fact]
    public void SCHEDULED_33_HOURS_OUT_SheStaysClampedUntilTheEpoch_ThenCastsOffAndClears()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        double scrubOut = 33 * 3600.0;
        Set(map, "_scrubOffsetSeconds", scrubOut);
        Invoke(map, "AddCastOffAtTop");
        double epoch = NodeEpochOf(FirstNodeOfKind(map, PlanStepKind.Undock));
        _clearanceNode = FirstNodeOfKind(map, PlanStepKind.ClearHarbour);

        // Half a day in she must still be tied up — the wait is the feature, not a stall.
        WarpThroughWithNoFurtherInput(map, Destination, 12 * 3600.0);
        Assert.Equal(Berth, Get<string?>(map, "_dockedHavenId"));
        AssertTheBannerSays(map, "the plan casts off in");

        Flight flight = WarpThroughWithNoFurtherInput(map, Destination, epoch + 6 * 3600.0);
        _out.WriteLine($"flown: {flight}; epoch was {epoch:F0}s");

        Assert.NotNull(flight.CastOffSimTime);
        Assert.True(Math.Abs(flight.CastOffSimTime!.Value - epoch) <= 2 * AdaptiveQuantumSeconds,
            $"the clamp must let go AT the scheduled epoch ({epoch:F0}s); she came free at "
            + $"{flight.CastOffSimTime.Value:F0}s.");
        Assert.Null(Get<string?>(map, "_dockedHavenId"));
        Assert.True(flight.ClearanceFired, "the out-thrust must fire behind the clamp release.");
    }

    /// <summary>
    /// (c) A BURN CANNOT FIRE BEFORE THE CLAMP LETS GO. Scrub back inside a scheduled departure and drop a
    /// burn there: the plan's SHAPE goes bad, the captain is woken once in the rule's own words, and warp is
    /// dropped — the #965 machinery, reused whole.
    ///
    /// <para>RED before the fix: with the undock pinned 60 s out, no burn could ever precede it, and there
    /// was no shape law to break — the plan stood green with a burn the clamp would have eaten.</para>
    /// </summary>
    [Fact]
    public void A_BURN_PLACED_BEFORE_THE_CAST_OFF_BreaksThePlansShape_AndWakesTheCaptainOnce()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        Set(map, "_scrubOffsetSeconds", 33 * 3600.0);
        Invoke(map, "AddCastOffAtTop");

        // The plan is sound as laid — seed the one-shot watch on that, exactly as the arrival's does.
        Invoke(map, "RefreshPlanShapeValidity");
        Assert.Null(Get<string?>(map, "_shapeAlarm"));
        Assert.Null(Invoke(map, "PlanShapeWarningLine"));

        // Now scrub back inside the wait and add a burn there — a burn the clamp would eat.
        Set(map, "_scrubOffsetSeconds", 10 * 3600.0);
        Set(map, "Warp", 10000);
        Invoke(map, "AddBurnAtScrub");
        Assert.Equal(PlanStepKind.Burn, KindOf(PlanNodes(map)[0]));   // it really is ahead of the clamp

        Invoke(map, "RefreshPlanShapeValidity");

        var alarm = Get<string?>(map, "_shapeAlarm");
        _out.WriteLine($"alarm: {alarm}");
        Assert.NotNull(alarm);
        Assert.Contains("before the clamp lets go", alarm);
        Assert.Equal(CastOffRule.ShapeComplaint(CastOffRule.PlanShapeFault.CastOffNotFirst), alarm);
        Assert.Equal(alarm, Invoke(map, "PlanShapeWarningLine"));
        Assert.Equal(alarm, Property(map, "LoudPlanAlarm"));
        Assert.Equal(1, Get<int>(map, "Warp"));   // never unseen at warp — the #147 idiom

        // ONE shot: a second cadence with the same broken plan does not re-pop it.
        Set(map, "_shapeAlarm", null);
        Invoke(map, "RefreshPlanShapeValidity");
        Assert.Null(Get<string?>(map, "_shapeAlarm"));

        // …and taking the burn off puts the plan right again, which re-arms the alarm for next time.
        Invoke(map, "DeleteNode", PlanNodes(map)[0]);
        Invoke(map, "RefreshPlanShapeValidity");
        Assert.Null(Invoke(map, "PlanShapeWarningLine"));
    }

    /// <summary>
    /// (d) A SCRUB AT NOW STILL LEAVES NOW. The sighting's behaviour is not deleted, it is demoted to the
    /// special case it always should have been: press with the scrub where it starts and the clamp lets go
    /// at the plan's floor, a minute out, exactly as #955 shipped it.
    /// </summary>
    [Fact]
    public void A_SCRUB_AT_NOW_StillCastsOffImmediately()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        Invoke(map, "AddCastOffAtTop");

        object undock = FirstNodeOfKind(map, PlanStepKind.Undock);
        Assert.Equal(60.0, NodeEpochOf(undock), 3);   // the plan's own floor: one minute out
        Assert.Equal(120.0, NodeEpochOf(FirstNodeOfKind(map, PlanStepKind.ClearHarbour)), 3);

        // A scrub dragged into the PAST is the same case — the control clamps, it never refuses.
        Pages.Map past = AShipClampedAtSeleneGate();
        Set(past, "_scrubOffsetSeconds", -5000.0);
        Invoke(past, "AddCastOffAtTop");
        Assert.Equal(60.0, NodeEpochOf(FirstNodeOfKind(past, PlanStepKind.Undock)), 3);
    }

    /// <summary>
    /// (e) THE PAIR ADDS ONCE. Owner: <i>"2 cast-off in sequence sounds kind of silly — we should have some
    /// logic check."</i> Pressing ⚓ + Cast off again is a no-op with one line — and the refusal is the plan
    /// GRAMMAR's, not the button's private opinion, so the same law that refuses here is the one that judges
    /// a plan nobody pressed anything on.
    /// </summary>
    [Fact]
    public void THE_CAST_OFF_PAIR_AddsOnce_HoweverManyTimesItIsPressed()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        Set(map, "_scrubOffsetSeconds", 33 * 3600.0);
        Invoke(map, "AddCastOffAtTop");
        Assert.Equal(2, PlanNodes(map).Count);

        // Press again, at the same scrub and at a different one: nothing is added, and the captain is told.
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "AddCastOffAtTop");
        Assert.Equal(2, PlanNodes(map).Count);
        Assert.Equal(CastOffRule.ShapeComplaint(CastOffRule.PlanShapeFault.SecondCastOff),
                     Get<PulseSlot>(map, "_pulse").Message);

        Set(map, "_scrubOffsetSeconds", 60 * 3600.0);
        Invoke(map, "AddCastOffAtTop");
        Assert.Equal(2, PlanNodes(map).Count);
        Assert.Equal(1, CountOfKind(map, PlanStepKind.Undock));
        Assert.Equal(1, CountOfKind(map, PlanStepKind.ClearHarbour));

        // …and a departure laid BEHIND a burn is refused by the same law, in the words that say why.
        Pages.Map late = AShipClampedAtSeleneGate();
        Set(late, "_scrubOffsetSeconds", 10 * 3600.0);
        Invoke(late, "AddBurnAtScrub");
        Set(late, "_scrubOffsetSeconds", 33 * 3600.0);
        Set(late, "_pulse", PulseSlot.Empty);
        Invoke(late, "AddCastOffAtTop");
        Assert.Equal(0, CountOfKind(late, PlanStepKind.Undock));
        Assert.Equal(CastOffRule.ShapeComplaint(CastOffRule.PlanShapeFault.CastOffNotFirst),
                     Get<PulseSlot>(late, "_pulse").Message);
    }

    /// <summary>
    /// (f) REMOVING EITHER ROW REMOVES THE PAIR. One press laid both, one press takes both — half a
    /// departure is not a plan anybody meant to have, and it is what the owner's second screenshot found on
    /// the board. Proven from BOTH rows, and proven not to touch the burns around them.
    /// </summary>
    [Fact]
    public void REMOVING_EITHER_DEPARTURE_ROW_TakesThePairOffTogether()
    {
        foreach (PlanStepKind pressed in new[] { PlanStepKind.Undock, PlanStepKind.ClearHarbour })
        {
            Pages.Map map = AShipClampedAtSeleneGate();
            Set(map, "_scrubOffsetSeconds", 40 * 3600.0);
            Invoke(map, "AddBurnAtScrub");            // a burn AFTER the departure, which must survive
            Set(map, "_scrubOffsetSeconds", 33 * 3600.0);
            Invoke(map, "AddCastOffAtTop");
            Assert.Equal(3, PlanNodes(map).Count);

            Invoke(map, "DeleteNode", FirstNodeOfKind(map, pressed));

            _out.WriteLine($"pressed ✖ on {pressed}: {PlanNodes(map).Count} row(s) left");
            Assert.Equal(0, CountOfKind(map, PlanStepKind.Undock));
            Assert.Equal(0, CountOfKind(map, PlanStepKind.ClearHarbour));
            Assert.Equal(1, CountOfKind(map, PlanStepKind.Burn));   // the burn is not collateral

            // …and with the departure gone the banner names no departure at all.
            AssertTheBannerDoesNotSay(map, "cast off");
            AssertTheBannerDoesNotSay(map, "clear the harbour");
        }
    }

    /// <summary>
    /// (g) THE REHEARSAL LEAVES FROM THE BERTH AS IT WILL BE. A berth 33 h out has swung a long way round
    /// its body; the plotted course — which is what #969's plan-time arm rehearses, and what the arrival's
    /// ✓/✗ is judged on — must start from THERE, not from where the berth stands tonight.
    /// </summary>
    [Fact]
    public void THE_PLOTTED_COURSE_StartsFromTheBerthAtTheUndockEpoch_NotAtNow()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        double scrubOut = 33 * 3600.0;
        Set(map, "_scrubOffsetSeconds", scrubOut);
        Invoke(map, "AddCastOffAtTop");

        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        double epoch = NodeEpochOf(FirstNodeOfKind(map, PlanStepKind.Undock));
        var start = (ShipState)Invoke(map, "PlanStartState")!;

        Assert.Equal(epoch, start.SimTime, 3);
        double offBerthThen = (start.Position - ephemeris.Position(Berth, epoch)).Length;
        double offBerthNow = (start.Position - ephemeris.Position(Berth, 0)).Length;
        _out.WriteLine($"plan start {offBerthThen:E2} m off the berth at the epoch, {offBerthNow:E2} m off it at now");

        Assert.True(offBerthThen < BerthState.BerthOffsetMeters * 2,
            "the plan must start ON the berth as it will be at the epoch.");
        Assert.True(offBerthNow > 100 * offBerthThen,
            "…and the berth really does move in 33 h, or this test proves nothing.");
    }
}
