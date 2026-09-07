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
[SlowGate] // #251 · 39 s over 23 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed partial class TheCastOffIsAStepTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public TheCastOffIsAStepTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;
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
}
