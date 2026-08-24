using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #955 NAV-1 · <b>CAST OFF IS A STEP — THE DOCKED END OF THE OWNER'S TEST STORY.</b>
///
/// <para>Owner's test story for the unified nav list (2026-08-23): <i>"plan while docked, then the plan starts
/// with an undock step recorded topmost in the nav-burn list, then safe-harbour out-thrust to clear the
/// vicinity of the station, then the actual burns, then the autopilot approach step, then the dock step."</i>
/// #965 built the arrival, #969 made arming it a plan-time promise — and both of them refused to be made from
/// a berth, which is the one place a captain actually plans a voyage from. This file plays the whole sentence
/// from the berth, once, against the shipping <c>scenarios/sol.json</c> and the shipping frame loop.</para>
///
/// <h3>What the bench flies</h3>
/// <para>She is CLAMPED at Selene Gate. The plan is built at the berth: ⚓ + Cast off lays the two departure
/// rows, then one transfer burn (solved with the game's own <see cref="LongHaul.SolveDeparture"/> from the
/// state the plotted course delivers three weeks out), then ⚓ arrive-dock at The Rusty Roadstead — Mars's own
/// berth, a quarter of a year and 1.5 AU away. The arrival is ARMED WHILE STILL CLAMPED, and then nothing
/// else is ever touched: the test spends the clock through <c>ConsumeTheAccumulator</c> /
/// <c>PinHerToTheDockAndDriftTheGhost</c> / <c>AccountForWhatTheStepsDid</c> — the real frame's own phases —
/// with no further input of any kind. The clamp lets go on the plan's word, the clearance thrust fires, she
/// leaves the harbour's reach, the transfer burn fires, the arrival comes round, and she ends clamped on at
/// the Roadstead with the steps retired off the board.</para>
///
/// <h3>RED PROOF (watched before this shipped)</h3>
/// <para>On the commit before the fix, <c>ArmTheArrivalForItsPass</c> opened with
/// <c>if (RejectNavWhileDocked()) return;</c> — so the very first assertion below fails: nothing arms, and
/// warping the same clock through leaves the ship sitting at Selene Gate for a year (there was no undock step
/// to run, and the clamped branch of the frame loop never applies a maneuver plan). Both halves of the red are
/// asserted here as consequences, not as a comment: the arm, and the ship's own position at the end.</para>
///
/// <h3>Anti-vacuity</h3>
/// <para><see cref="A_PLAN_WITHOUT_A_CAST_OFF_CannotBeArmedFromTheBerth_AndSaysSo"/> flies the SAME bench with
/// the two departure rows deleted: the arm is refused, in the ⚓ register the nav lock already speaks in, and
/// nothing is armed. So the carve-out is the cast-off's, not a hole in the lock.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCastOffIsAStepTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public TheCastOffIsAStepTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const double Day = 86400.0;

    private const string Berth = "selene-gate";
    private const string Destination = "the-space-bar";

    /// <summary>When the transfer burn is solved and fired: three weeks out, by which time the cast-off has
    /// carried her clear of Earth's well and a heliocentric departure solve is honest. (The bench ASSERTS
    /// that — see the escape check — rather than assuming it.)</summary>
    private const double TransferBurnSimTime = 20 * Day;

    /// <summary>The frame loop's own high-warp quantum (Map.Sim's <c>AdaptiveWarpQuantum</c>) — the finest
    /// bite the clock is spent in while she is clamped, so "when did the clamp let go" is a measurement.</summary>
    private const double AdaptiveQuantumSeconds = 60.0;

    // ── (1) THE SENTENCE, PLAYED FROM THE BERTH ────────────────────────────────────────────────────────

    /// <summary>
    /// CAST OFF · CLEAR THE HARBOUR · ONE BURN · ARRIVE AND DOCK — planned and armed at the berth, flown with
    /// no further input. This is the owner's docked-end story as an executable statement.
    /// </summary>
    [Fact]
    public void CLAMPED_AT_SELENE_GATE_ThePlanCastsHerOffAndDocksHerAtTheRoadstead_WithNoFurtherInput()
    {
        Pages.Map map = AShipClampedAtSeleneGateWithACastOffAndATransferToMars();

        // The plan reads, top to bottom, exactly as the story says it should.
        IReadOnlyList<object> steps = PlanNodes(map);
        Assert.Equal(3, steps.Count);
        Assert.Equal(PlanStepKind.Undock, KindOf(steps[0]));
        Assert.Equal(PlanStepKind.ClearHarbour, KindOf(steps[1]));
        Assert.Equal(PlanStepKind.Burn, KindOf(steps[2]));
        Assert.True((bool)Property(map, "PlanBeginsWithCastOff")!);

        // …and she really is clamped, and the destination really is a THEN, or this proves nothing.
        Assert.Equal(Berth, Get<string?>(map, "_dockedHavenId"));
        ClosestApproach.Pass pass = ThePassBy(map, Destination);
        Assert.True(pass.SimTime - Get<double>(map, "SimTime") > 200 * Day,
            "the arm must be made months before the encounter for this to be the feature under test.");
        Assert.True(DistanceTo(map, Destination) > 1e11, "she must be nowhere near the Roadstead yet.");

        AddTheArriveStep(map, pass, ArrivalStepRule.ArrivalKind.Dock);

        // THE FIX: on the commit before this, ArmTheArrivalForItsPass opened with RejectNavWhileDocked()
        // and this line left _armedOrbitBodyId null.
        Invoke(map, "ArmArriveStep");
        Assert.Equal(Destination, Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Null(Get<string?>(map, "_autopilotStandDownReason"));
        Assert.NotNull(Get<double?>(map, "_armedArrivalPassSimTime"));
        Assert.Equal(Berth, Get<string?>(map, "_dockedHavenId"));   // armed WITHOUT letting go of the clamp
        _out.WriteLine($"armed from the berth for the pass at {Get<double?>(map, "_armedArrivalPassSimTime")!.Value / Day:F1} d");

        // The banner says what she is doing — WAITING at the berth for a departure a minute out, which is
        // #989's reading of a plan that has not let go yet — and names the clearance below NOW. Not the
        // cast off twice: NOW is already carrying that row's countdown.
        AssertTheBannerSays(map, "docked at Selene Gate");
        AssertTheBannerSays(map, "the plan casts off in");
        AssertTheBannerSays(map, "clear the harbour");
        AssertTheBannerNamesTheCastOffOnce(map);

        int tankAtArm = Get<int>(map, "_reactionMassPulses");
        int clearancePulses = PulsesOf(steps[1]);
        Flight flight = WarpThroughWithNoFurtherInput(map, Destination, pass.SimTime + 30 * Day);
        _out.WriteLine($"flown: {flight}");

        // 1 · THE CLAMP LET GO AT PLAN START — on the plan's word, with nobody at the console.
        Assert.NotNull(flight.CastOffSimTime);
        Assert.True(flight.CastOffSimTime!.Value <= 2 * AdaptiveQuantumSeconds,
            $"the clamp must release at the plan's FIRST step (its epoch is {NodeEpochOf(steps[0])}s); "
            + $"the clock had already run to {flight.CastOffSimTime}s when she came free.");
        Assert.True((bool)FieldOf(steps[0], "Executed")!, "…and the undock row must retire itself when it runs.");

        // 2 · THE CLEARANCE FIRED, AND SHE LEFT THE HARBOUR'S REACH.
        Assert.True(flight.ClearanceFired, "the safe-harbour out-thrust must actually fire.");
        Assert.True(CastOffRule.Cleared(flight.MaxSeparationFromBerth),
            $"she must leave the harbour's reach ({CastOffRule.ClearRangeMeters:E2} m); "
            + $"she got {flight.MaxSeparationFromBerth:E2} m out.");

        // 3 · THE TRANSFER BURN FIRED, AND 4 · THE ARRIVAL FINISHED THE TRIP.
        Assert.True(flight.TransferFired, "the plotted transfer burn must fire once she is under way.");
        Assert.Equal(Destination, Get<string?>(map, "_dockedHavenId"));

        // 5 · THE STEPS RETIRED THEMSELVES — nothing is left standing on a board whose voyage is over.
        Assert.Empty(PlanNodes(map));
        Assert.Null(Get<object?>(map, "_arrive"));

        int tankAtBerth = Get<int>(map, "_reactionMassPulses");
        Assert.True(tankAtBerth > 0, "a planned trip must never strand the captain.");
        _out.WriteLine($"tank {tankAtArm} → {tankAtBerth} p (clearance quoted {clearancePulses} p)");
    }

    // ── (2) ANTI-VACUITY: THE LOCK IS STILL A LOCK ─────────────────────────────────────────────────────

    /// <summary>
    /// A PLAN THAT DOES NOT BEGIN AT THE BERTH CANNOT BE ARMED FROM ONE. The same bench with the two departure
    /// rows deleted: the arm is refused in the ⚓ register the nav lock already speaks in, nothing arms, and
    /// warping the same clock through leaves her exactly where a refused arm should — still tied up.
    /// </summary>
    [Fact]
    public void A_PLAN_WITHOUT_A_CAST_OFF_CannotBeArmedFromTheBerth_AndSaysSo()
    {
        Pages.Map map = AShipClampedAtSeleneGateWithACastOffAndATransferToMars();
        ClosestApproach.Pass pass = ThePassBy(map, Destination);
        AddTheArriveStep(map, pass, ArrivalStepRule.ArrivalKind.Dock);

        RemoveTheDepartureRows(map);
        Assert.False((bool)Property(map, "PlanBeginsWithCastOff")!);

        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "ArmArriveStep");

        Assert.Null(Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Null(Get<double?>(map, "_armedArrivalPassSimTime"));
        string? said = Get<PulseSlot>(map, "_pulse").Message;
        _out.WriteLine($"refusal: {said}");
        Assert.NotNull(said);
        Assert.Contains("⚓", said);
        Assert.Contains("clamped to the station", said);   // the SAME sentence every other nav act is refused with
        Assert.Contains("Cast off", said);                 // …and the one press that fixes it

        Flight flight = WarpThroughWithNoFurtherInput(map, Destination, 60 * Day);
        _out.WriteLine($"flown after the refusal: {flight}");
        Assert.Equal(Berth, Get<string?>(map, "_dockedHavenId"));
        Assert.Null(flight.CastOffSimTime);
    }

    /// <summary>
    /// THE CARVE-OUT IS THE CAST-OFF'S, NOT A HOLE IN THE LOCK. Plotting from a berth is now allowed, and a
    /// plan-time promise may be armed from one — but a LIVE nav act still cannot get through, even with a cast
    /// off standing at the top of the plan. The refused list stays refused.
    /// </summary>
    [Fact]
    public void A_LIVE_NAV_ACT_IsStillRefusedFromTheBerth_EvenWithACastOffInThePlan()
    {
        Pages.Map map = AShipClampedAtSeleneGateWithACastOffAndATransferToMars();
        Assert.True((bool)Property(map, "PlanBeginsWithCastOff")!);

        Assert.True((bool)Invoke(map, "RejectNavWhileDocked")!,
            "the lock itself must still refuse — the cast-off step is not a key to it.");

        // Arming the autopilot at a body HERE AND NOW (the historic NOW arm, and the O-key's) is a live act:
        // it would have the engines fire against the clamp this instant.
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "ToggleArmedInsertion", "mars");
        Assert.Null(Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Contains("clamped to the station", Get<PulseSlot>(map, "_pulse").Message ?? "");

        // …and so is circularizing into orbit by hand.
        Set(map, "_pulse", PulseSlot.Empty);
        Invoke(map, "EnterOrbit");
        Assert.False(Get<bool>(map, "_orbitKept"));
        Assert.Contains("clamped to the station", Get<PulseSlot>(map, "_pulse").Message ?? "");

        Assert.Equal(Berth, Get<string?>(map, "_dockedHavenId"));
    }

    // ── (3) GUARDS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NO STEP KIND WITHOUT ITS WORDS AND ITS EXECUTOR. Walks <see cref="PlanStepKind"/> and demands, for each
    /// one, a banner label, a glance line, and a place in the plan the ship actually flies: a Burn or a
    /// ClearHarbour reaches the maneuver plan the integrator applies, an Undock reaches the frame loop's
    /// cast-off branch. A kind that had neither would be a row the captain reads and the ship ignores.
    /// </summary>
    [Fact]
    public void EVERY_STEP_KIND_HasWordsAndAnExecutor()
    {
        Pages.Map map = AShipClampedAtSeleneGateWithACastOffAndATransferToMars();

        foreach (PlanStepKind kind in Enum.GetValues<PlanStepKind>())
        {
            object node = FirstNodeOfKind(map, kind);

            var label = (string)Invoke(map, "PlanStepLabel", node)!;
            var glance = (string)Invoke(map, "PlanStepGlanceLine", node)!;
            Assert.False(string.IsNullOrWhiteSpace(label), $"{kind} has no banner label");
            Assert.False(string.IsNullOrWhiteSpace(glance), $"{kind} has no glance line");
            _out.WriteLine($"{kind,-13} label=\"{label}\"  glance=\"{glance}\"");

            // The executor. Undock is the frame loop's (it changes which branch the loop takes); everything
            // else is the maneuver plan's, which is what makes the clearance a burn and not a special case.
            if (kind == PlanStepKind.Undock)
            {
                Assert.Same(node, Invoke(map, "NextCastOffStep"));
            }
            else
            {
                Assert.Contains(Get<ManeuverPlan>(map, "_plan").Nodes,
                    n => Math.Abs(n.SimTime - (double)FieldOf(node, "SimTime")!) < 0.5);
            }
        }
    }

    /// <summary>
    /// THE CLEARANCE IS SIZED BY THE HARBOUR, NOT BY A TYPED NUMBER. Pins the laid row against
    /// <see cref="DockRule"/> read straight from Core: the target range is the clamp's own envelope with the
    /// margin, the outbound speed is the share of the speed that same law calls matched, and the pulses are
    /// what <see cref="OrbitRule.PulsesFor"/> charges for the Δv the berth's shove leaves owing.
    /// </summary>
    [Fact]
    public void THE_CLEARANCE_BurnIsSizedOffDockRule()
    {
        Assert.Equal(DockRule.EnvelopeMeters * CastOffRule.MarginFactor, CastOffRule.ClearRangeMeters, 6);
        Assert.Equal(DockRule.MatchSpeed * CastOffRule.DepartureShareOfMatchSpeed, CastOffRule.OutboundSpeedMps, 6);

        Pages.Map map = AShipClampedAtSeleneGateWithACastOffAndATransferToMars();
        object clearance = FirstNodeOfKind(map, PlanStepKind.ClearHarbour);

        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        double at = (double)FieldOf(clearance, "SimTime")!;
        Vector2d havenVel = (ephemeris.Position(Berth, at + 1) - ephemeris.Position(Berth, at - 1)) / 2;

        // The berth's own shove is real speed and is not paid for twice — that is the second argument.
        int expected = CastOffRule.Pulses(havenVel.Length, UndockPush());
        Assert.Equal(expected, PulsesOf(clearance));
        Assert.True(expected > 0, "a clearance of nothing is not a clearance.");
        _out.WriteLine(
            $"clearance {expected} p: {CastOffRule.DeltaVMps(UndockPush()):F0} m/s owed of "
            + $"{CastOffRule.OutboundSpeedMps:F0} m/s outbound, clear at {CastOffRule.ClearRangeMeters:E2} m");

        // …and it points STRAIGHT OUT along the berthing arm, which is the way the clamp already pushed her.
        Vector2d outward = ephemeris.Position(Berth, at).Normalized();
        Assert.Equal(NodeFrame.Prograde(outward), (double)FieldOf(clearance, "HeadingDegrees")!, 3);
        Assert.Equal(CastOffRule.PulsePercent, (double)FieldOf(clearance, "Percent")!, 9);
    }

    /// <summary>
    /// A CLAMPED SHIP FIRES NOTHING. Plotting from the berth is allowed — but the frame's clamped branch never
    /// applies the maneuver plan, so a burn whose epoch slid past under the clamp did NOT fire, and billing it
    /// would be a green number never asked of the world. It is struck instead, and the tank is untouched.
    /// </summary>
    [Fact]
    public void A_BURN_THAT_PASSES_UNDER_THE_CLAMP_IsStruck_NotBilled()
    {
        Pages.Map map = AShipClampedAtSeleneGateWithACastOffAndATransferToMars();
        RemoveTheDepartureRows(map);              // nothing will let go of the clamp

        int tank = Get<int>(map, "_reactionMassPulses");
        object burn = FirstNodeOfKind(map, PlanStepKind.Burn);
        Assert.False((bool)FieldOf(burn, "Stale")!);

        WarpThroughWithNoFurtherInput(map, Destination, TransferBurnSimTime + 5 * Day);

        Assert.Equal(Berth, Get<string?>(map, "_dockedHavenId"));
        Assert.Equal(tank, Get<int>(map, "_reactionMassPulses"));
        Assert.True((bool)FieldOf(burn, "Stale")!, "a burn the clamp ate must be struck.");
        Assert.False((bool)FieldOf(burn, "Executed")!, "…and never marked as flown.");
    }

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

        // …so the row's own countdown reads the truth rather than "in 0 h".
        var glance = (string)Invoke(map, "PlanStepGlanceLine", undock)!;
        _out.WriteLine($"row: {glance}");
        Assert.Contains("in 1 d", glance);
        Assert.DoesNotContain("in 0 h", glance);

        // And the banner: she is tied up, the captain has her, and the plan lets go at its own hour.
        AssertTheBannerSays(map, "YOU HAVE THE SHIP");
        AssertTheBannerSays(map, "docked at Selene Gate");
        AssertTheBannerSays(map, "the plan casts off in 1 d");
        AssertTheBannerDoesNotSay(map, "casting off from");
        AssertTheBannerNamesTheCastOffOnce(map);
    }

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

    // ── The bench ──────────────────────────────────────────────────────────────────────────────────────

    private readonly record struct Flight(
        int Frames, double Days, double? CastOffSimTime, bool ClearanceFired, bool TransferFired,
        double MaxSeparationFromBerth, bool Docked)
    {
        public override string ToString() =>
            $"{Frames} frames, {Days:F1} d, cast off at {CastOffSimTime?.ToString("F0") ?? "never"}s, "
            + $"clearance={ClearanceFired} transfer={TransferFired} "
            + $"maxOffBerth={MaxSeparationFromBerth:E2} m, docked={Docked}";
    }

    /// <summary>
    /// SPEND THE CLOCK, TOUCH NOTHING ELSE. The three phases are the shipping frame's own, in the shipping
    /// order — the fixed-step loop that lands on the cast-off epoch and fires the plotted burns, the pin that
    /// keeps a berthed ship on her rail, and the accounting phase that bills fired nodes and runs
    /// <c>CheckArmedInsertion</c>. No key, no click, no arm, no re-plot.
    /// </summary>
    private Flight WarpThroughWithNoFurtherInput(Pages.Map map, string targetId, double untilSimTime)
    {
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        double start = Get<double>(map, "SimTime");
        double? castOffAt = null;
        double maxSeparation = 0;
        int frames = 0;

        while (Get<double>(map, "SimTime") < untilSimTime && frames < 4000)
        {
            if (Get<string?>(map, "_dockedHavenId") == targetId || Get<bool>(map, "_orbitKept"))
            {
                break; // arrived — the trip the plan promised is over
            }

            var ship = Get<ShipState>(map, "_ship");
            double simTime = Get<double>(map, "SimTime");
            Vector2d bodyPos = ephemeris.Position(targetId, simTime);
            Vector2d bodyVel = (ephemeris.Position(targetId, simTime + 1) - ephemeris.Position(targetId, simTime - 1)) / 2;
            double gap = (ship.Position - bodyPos).Length;
            double closing = Math.Max(1.0, (ship.Velocity - bodyVel).Length);
            // While a cast off is still pending, spend the clock in small bites: it is the only way to MEASURE
            // when the clamp let go rather than merely notice afterwards that it has. Everywhere else, the
            // frame's own warp discipline generalized — coarse across the void, tightening as the gap to the
            // target closes, which is what UpdateEffectiveWarp's near-body cap does live.
            double chunk = Invoke(map, "NextCastOffStep") is not null
                ? 2 * AdaptiveQuantumSeconds
                : Math.Clamp(gap / closing / 10.0, 60.0, 20000 * 60.0);

            Set(map, "_effectiveWarp", 10000);
            Set(map, "_simAccumulator", chunk);
            int steps = (int)Invoke(map, "ConsumeTheAccumulator", false)!;
            Invoke(map, "PinHerToTheDockAndDriftTheGhost");
            Invoke(map, "AccountForWhatTheStepsDid", steps);
            frames++;

            if (castOffAt is null && Get<string?>(map, "_dockedHavenId") is null)
            {
                castOffAt = Get<double>(map, "SimTime");
            }

            // Latch what actually fired off the nodes' own Executed flags, BEFORE the board prunes them
            // (clearing spent rows off the plan is itself part of the behaviour under test).
            _clearanceFired |= _clearanceNode is { } cn && (bool)FieldOf(cn, "Executed")!;
            _transferFired |= _transferNode is { } tn && (bool)FieldOf(tn, "Executed")!;

            if (castOffAt is not null)
            {
                double sep = (Get<ShipState>(map, "_ship").Position
                              - ephemeris.Position(Berth, Get<double>(map, "SimTime"))).Length;
                maxSeparation = Math.Max(maxSeparation, sep);
            }
        }

        double end = Get<double>(map, "SimTime");
        return new Flight(
            frames, (end - start) / Day, castOffAt,
            ClearanceFired: _clearanceFired, TransferFired: _transferFired,
            maxSeparation, Get<string?>(map, "_dockedHavenId") is not null);
    }

    // The two "did it actually fire" bits, latched off the nodes' own Executed flags before the list prunes
    // them (AccountForFiredNodes clears spent rows off the board, which is itself under test here).
    private bool _clearanceFired;
    private bool _transferFired;
    private object? _clearanceNode;
    private object? _transferNode;

    /// <summary>A ship CLAMPED at Selene Gate with an empty board and the scrub at now — the bar stool the
    /// owner plans his voyages from. Everything else in this file is built on top of it.</summary>
    private Pages.Map AShipClampedAtSeleneGate()
    {
        _clearanceFired = false;
        _transferFired = false;
        _clearanceNode = null;
        _transferNode = null;

        var map = new Pages.Map();
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_reactionMassPulses", 500);
        Set(map, "_horizonChoice", "400");   // the ribbon must reach the encounter it is being planned to

        // The berth, built the one way every berth in this game is built.
        ShipState berth = BerthState.CoMoving(ephemeris, Berth, 0, BerthState.BerthOffsetMeters);
        Set(map, "_ship", berth);
        Set(map, "SimTime", 0.0);
        Set(map, "_dockedHavenId", Berth);
        Set(map, "_dockOffset", berth.Position - ephemeris.Position(Berth, 0));
        return map;
    }

    /// <summary>
    /// A ship CLAMPED at Selene Gate with the owner's plan on the board: ⚓ + Cast off (the undock and the
    /// clearance), then one transfer burn solved with the game's own departure solver from the state the
    /// PLOTTED course delivers three weeks out — which is to say, from after the cast-off, exactly as the
    /// ribbon draws it. Nothing is armed.
    /// </summary>
    private Pages.Map AShipClampedAtSeleneGateWithACastOffAndATransferToMars()
    {
        Pages.Map map = AShipClampedAtSeleneGate();
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");

        // ⚓ + Cast off — the button, pressed, with the scrub where it starts: at NOW. (#989 keeps this
        // reading working — a scrub at now still departs immediately; it is the SCHEDULED departure that
        // stopped lying about its hour.)
        Invoke(map, "AddCastOffAtTop");
        Invoke(map, "ReprojectTrajectory");
        Invoke(map, "ReprojectThePassesOnTheirCadence", 1000.0);

        // She must genuinely be OUT of Earth's well by the time the transfer is solved, or a heliocentric
        // Lambert from inside it would be a lie and this bench would be proving nothing about the transfer.
        Vector2d atBurn = (Vector2d)Invoke(map, "SamplePositionAt", TransferBurnSimTime)!;
        double earthHill = OrbitRule.HillRadius(
            System.Linq.Enumerable.First(ephemeris.Bodies, b => b.Id == "earth"),
            System.Linq.Enumerable.First(ephemeris.Bodies, b => b.Id == "sun").Mu);
        double offEarth = (atBurn - ephemeris.Position("earth", TransferBurnSimTime)).Length;
        Assert.True(offEarth > earthHill,
            $"the cast-off must carry her clear of Earth's Hill sphere ({earthHill:E2} m) before the transfer "
            + $"is solved; she was {offEarth:E2} m out.");

        // The transfer burn, solved from where the PLOT puts her — cast-off, clearance and all.
        var atBurnVel = (Vector2d)Invoke(map, "SampledVelocityAt", TransferBurnSimTime)!;
        CelestialBody mars = System.Linq.Enumerable.First(ephemeris.Bodies, b => b.Id == "mars");
        LongHaul.Departure departure = LongHaul.SolveDeparture(
            new ShipState(atBurn, atBurnVel, TransferBurnSimTime), ephemeris, mars);
        Assert.True(departure.Ok, $"the bench's own departure solve must succeed: {departure.Failure}");

        Vector2d deltaV = departure.PostBurnVelocity - atBurnVel;
        AddPlottedVectorBurn(map, TransferBurnSimTime,
            deltaV.Length / atBurnVel.Length * 100.0, NodeFrame.Prograde(deltaV));

        Invoke(map, "RebuildPlan");
        Invoke(map, "ReprojectTrajectory");
        Invoke(map, "ReprojectThePassesOnTheirCadence", 2000.0);

        _clearanceNode = FirstNodeOfKind(map, PlanStepKind.ClearHarbour);
        _transferNode = FirstNodeOfKind(map, PlanStepKind.Burn);
        return map;
    }

    private static void AddPlottedVectorBurn(Pages.Map map, double simTime, double percent, double heading)
    {
        Type nodeType = typeof(Pages.Map).GetNestedType("PlanNode", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Map.PlanNode is gone — this bench has drifted.");
        object node = Activator.CreateInstance(nodeType, nonPublic: true)!;
        SetField(node, "Kind", PlanStepKind.Burn);
        SetField(node, "SimTime", simTime);
        SetField(node, "Action", ManeuverAction.Accelerate);
        SetField(node, "Pulses", 1);
        SetField(node, "Percent", percent);
        SetField(node, "Mode", BurnMode.Vector);
        SetField(node, "HeadingDegrees", heading);
        ((IList)Get<object>(map, "_planNodes")).Add(node);
    }

    private static void RemoveTheDepartureRows(Pages.Map map)
    {
        var nodes = (IList)Get<object>(map, "_planNodes");
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (KindOf(nodes[i]!) != PlanStepKind.Burn)
            {
                nodes.RemoveAt(i);
            }
        }
        Invoke(map, "RebuildPlan");
        Invoke(map, "ReprojectTrajectory");
        Invoke(map, "ReprojectThePassesOnTheirCadence", 3000.0);
    }

    private static ArrivalStepRule.ArrivalCheck? AddTheArriveStep(
        Pages.Map map, ClosestApproach.Pass pass, ArrivalStepRule.ArrivalKind kind)
    {
        Set(map, "_scrubOffsetSeconds", pass.SimTime - Get<ShipState>(map, "_ship").SimTime);
        Invoke(map, "AddArriveAtScrub", kind);
        Assert.NotNull(Get<object?>(map, "_arrive"));
        return (ArrivalStepRule.ArrivalCheck?)Invoke(map, "ArriveCheck");
    }

    private static ClosestApproach.Pass ThePassBy(Pages.Map map, string bodyId)
    {
        object? pass = Invoke(map, "ArrivePassFor", bodyId);
        Assert.True(pass is not null, $"the plotted course must have a pass by {bodyId} — this bench has drifted.");
        return (ClosestApproach.Pass)pass!;
    }

    private static double DistanceTo(Pages.Map map, string bodyId)
    {
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        var ship = Get<ShipState>(map, "_ship");
        return (ship.Position - ephemeris.Position(bodyId, ship.SimTime)).Length;
    }

    private static double UndockPush() =>
        (double)(typeof(Pages.Map).GetField("UndockPushMps", Hidden | BindingFlags.Static)
                 ?? throw new InvalidOperationException("Map.UndockPushMps is gone — this bench has drifted"))
            .GetRawConstantValue()!;

    private void AssertTheBannerSays(Pages.Map map, string phrase)
    {
        var status = (FlightPlanStatus)Invoke(map, "FlightNowNext")!;
        bool found = false;
        foreach (FlightPlanRow row in status.Rows)
        {
            if (row.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
            }
        }

        Assert.True(found,
            $"the banner should carry \"{phrase}\"; it had: "
            + string.Join(" | ", System.Linq.Enumerable.Select(status.Rows, r => r.Text)));
    }

    private void AssertTheBannerDoesNotSay(Pages.Map map, string phrase)
    {
        var status = (FlightPlanStatus)Invoke(map, "FlightNowNext")!;
        foreach (FlightPlanRow row in status.Rows)
        {
            Assert.False(row.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase),
                $"the banner must NOT carry \"{phrase}\"; it had: "
                + string.Join(" | ", System.Linq.Enumerable.Select(status.Rows, r => r.Text)));
        }
    }

    /// <summary>#989's second sighting as an assertion: the owner read <i>"NOW: casting off from The Red Eye
    /// in 0 h · NEXT: ⚓ cast off from The Red Eye in 0 h"</i> — one live row, spoken from two slots. No row
    /// of the banner may name the departure more than once, however many rows the banner has.</summary>
    private void AssertTheBannerNamesTheCastOffOnce(Pages.Map map)
    {
        var status = (FlightPlanStatus)Invoke(map, "FlightNowNext")!;
        int mentions = 0;
        foreach (FlightPlanRow row in status.Rows)
        {
            if (row.Text.Contains("cast", StringComparison.OrdinalIgnoreCase)
                && row.Text.Contains("off", StringComparison.OrdinalIgnoreCase))
            {
                mentions++;
            }
        }

        Assert.True(mentions == 1,
            $"the departure must be named exactly once in the banner, not {mentions} times: "
            + string.Join(" | ", System.Linq.Enumerable.Select(status.Rows, r => r.Text)));
    }

    // ── Reflection plumbing (the TheArrivalIsArmedThenNotOnlyNow / TheBerthEndsTheVoyage idiom) ─────────

    private static IReadOnlyList<object> PlanNodes(Pages.Map map)
    {
        var list = new List<object>();
        foreach (object? node in (IList)Get<object>(map, "_planNodes"))
        {
            list.Add(node!);
        }
        return list;
    }

    private static object FirstNodeOfKind(Pages.Map map, PlanStepKind kind)
    {
        foreach (object node in PlanNodes(map))
        {
            if (KindOf(node) == kind)
            {
                return node;
            }
        }

        throw new InvalidOperationException($"the bench's plan has no {kind} row — it has drifted");
    }

    private static PlanStepKind KindOf(object node) => (PlanStepKind)FieldOf(node, "Kind")!;

    private static int CountOfKind(Pages.Map map, PlanStepKind kind)
    {
        int n = 0;
        foreach (object node in PlanNodes(map))
        {
            if (KindOf(node) == kind)
            {
                n++;
            }
        }
        return n;
    }

    private static int PulsesOf(object node) => (int)FieldOf(node, "Pulses")!;

    private static double NodeEpochOf(object node) => (double)FieldOf(node, "SimTime")!;

    private static object? FieldOf(object node, string field) =>
        (node.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on PlanNode — this bench has drifted"))
        .GetValue(node);

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    private static string ScenarioPath(string file)
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("no scenarios/ directory above the test binary")
            : System.IO.Path.Combine(dir.FullName, "scenarios", file);
    }

    private static void Set(object o, string field, object? value) => SetField(o, field, value);

    private static void SetField(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string name) =>
        (T)(o.GetType().GetField(name, Hidden)
            ?? throw new InvalidOperationException($"no field {name} on Map — this bench has drifted"))
            .GetValue(o)!;

    private static object? Property(object o, string name) =>
        (o.GetType().GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"no property {name} on Map — this bench has drifted"))
        .GetValue(o);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}
