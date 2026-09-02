using System;
using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #969 · <b>AUTOPILOT / DOCK / ORBIT <i>THEN</i>, NOT JUST <i>NOW</i>.</b>
///
/// <para>Owner ruling, 2026-08-23: <i>"I just want the possibility to plan the trip from space to docked at
/// one go. So once the burns are planned right I can add the autopilot after the last burn to dock the ship
/// at plan time before the trip is even begun. The point is that now we only have orbit now / dock now. I
/// want dock / orbit option as normal part, a step in the plan. Say three burns and one autopilot to finish
/// the trip to Mars. After that no, absolutely no steps needed if the ship is not interfered with."</i></para>
///
/// <h3>What this file does</h3>
/// <para>It plays that sentence, once, against the shipping <c>scenarios/sol.json</c> and the shipping frame
/// loop. A ship stands in free space off Earth with two plotted burns that put her past Mars nine and a half
/// months later. The ARRIVE step is added the way the button adds it, ARMED at plan time while she is still
/// 1.5 AU and 292 days from the encounter — and then <b>nothing else is ever touched</b>: the test spends the
/// clock through <c>ConsumeTheAccumulator</c> / <c>AccountForWhatTheStepsDid</c>, the real frame's own two
/// phases, with no further input of any kind, and asks at the end where the ship is. She is in a kept orbit
/// at Mars, the step has retired itself, and the tank still has fuel.</para>
///
/// <para>The chunk of clock each pass buys is sized off the closing geometry — coarse across the void, fine
/// as the gap shuts. That is the same thing the live frame's near-body warp cap does for a player (a frame at
/// 10,000× is ~166 sim-seconds and the cap tightens near a well); doing it here keeps a 292-day integration
/// affordable in CI without ever letting the ship skip past a decision point.</para>
///
/// <h3>RED PROOF (watched before this shipped)</h3>
/// <para>On 77eee74 — the commit before the fix — <c>ArmArriveStep</c> went straight to
/// <c>ToggleArmedInsertion</c>, whose promise is rehearsed BALLISTICALLY FROM THE SHIP'S PRESENT STATE. The
/// plotted burns are not in that flight, so the rehearsal simply never reaches Mars: measured directly,
/// <c>AutopilotRehearsal.Rehearse</c> from the ship's state at t=0 returns <c>Captured=false,
/// HorizonReached=true</c> after 120 days of coasting, and the arm is REFUSED. The positive test below fails
/// at its very first assertion (nothing is armed), and the ship coasts past Mars for ever.</para>
///
/// <h3>Anti-vacuity</h3>
/// <para>The negative case flies the SAME bench with a heavier pair of burns — a course that crosses Mars's
/// orbit 8.4 M km wide and 10.3 km/s hot, past both gates the flight itself obeys. The arm is refused, in
/// numbers, and warping the same clock through leaves the ship un-inserted. So the plan-time arm is not a
/// rubber stamp: it still only promises what it has flown.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 42 s over 4 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheArrivalIsArmedThenNotOnlyNowTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public TheArrivalIsArmedThenNotOnlyNowTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const double Day = 86400.0;

    /// <summary>The departure that reaches Mars, solved once with the game's own Lambert departure solver
    /// (<c>LongHaul.SolveDeparture</c>) from this bench's starting state and then split in half across two
    /// plotted Vector nodes — a captain's departure burn and its follow-up. Pinned as constants rather than
    /// re-searched per run so the test is deterministic and cheap; the bench ASSERTS the pass it produces
    /// (distance and relative speed against the real gates) before arming anything, so a world that drifts
    /// out from under these numbers fails loudly instead of quietly proving nothing.</summary>
    private const double DeparturePercentPerNode = 3.920151388528919;

    private const double DepartureHeadingDegrees = 248.18684551285557;

    /// <summary>Three times the departure: a course that crosses Mars's orbit far too wide and far too fast
    /// to be caught from — the anti-vacuous negative.</summary>
    private const double TooHotFactor = 3.0;

    private const double FirstBurnSimTime = 1800.0;
    private const double SecondBurnSimTime = 3 * 3600.0;

    // ── (1) THE SENTENCE, PLAYED ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TWO BURNS AND ONE AUTOPILOT, ARMED BEFORE THE TRIP BEGINS — and the ship arrives with no further
    /// input. This is the owner's ruling as an executable statement.
    /// </summary>
    [Fact]
    public void ARMED_AT_PLAN_TIME_TheShipFliesTheBurnsAndTheAutopilotParksHerAtMars_WithNoFurtherInput()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode);
        ClosestApproach.Pass pass = TheMarsPass(map);

        // The plan ENDS SAFELY before anything is armed — the row's own ✓ bit, on the real thresholds.
        ArrivalStepRule.ArrivalCheck check = AddTheArriveStep(map, pass, ArrivalStepRule.ArrivalKind.Orbit);
        _out.WriteLine($"plotted arrival: {ArrivalStepRule.Verdict(check)}  (pass at {pass.SimTime / Day:F1} d)");
        Assert.True(check.Valid, $"the bench's own plan must end safely, or it proves nothing: {ArrivalStepRule.Verdict(check)}");

        // …and she is nowhere near Mars yet: this is a THEN, not a NOW.
        double distanceAtArm = DistanceTo(map, "mars");
        Assert.True(distanceAtArm > 1e11,
            $"the arm must be made from far away to be the feature under test; she is {distanceAtArm:E2} m out.");
        Assert.True(pass.SimTime - Get<double>(map, "SimTime") > 200 * Day,
            "the arm must be made months before the encounter.");

        Invoke(map, "ArmArriveStep");

        // THE FIX: on 77eee74 this is null — the arm refuses ("can't verify a capture from here"), because
        // its rehearsal flies ballistically from HERE and never meets Mars at all.
        Assert.Equal("mars", Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Null(Get<string?>(map, "_autopilotStandDownReason"));
        double? armedFor = Get<double?>(map, "_armedArrivalPassSimTime");
        Assert.NotNull(armedFor);
        Assert.Equal(pass.SimTime, armedFor!.Value, 1.0);
        _out.WriteLine($"armed THEN for the pass at {armedFor.Value / Day:F2} d; quoted ≈{Get<int>(map, "_armedBudgetPulses")} p");

        // The banner names the arrival as the last step, and says who finishes the trip.
        AssertTheBannerNamesTheArrival(map, "the autopilot inserts");

        int tankAtArm = Get<int>(map, "_reactionMassPulses");
        Flight flight = WarpThroughWithNoFurtherInput(map, "mars", pass.SimTime + 30 * Day);
        _out.WriteLine($"flown: {flight}");

        Assert.True(Get<bool>(map, "_orbitKept"),
            $"she should be in a KEPT orbit at Mars with nothing more asked of the captain; {flight}");
        Assert.Equal("mars", Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Null(Get<string?>(map, "_autopilotStandDownReason"));

        // #964/#965: the voyage is over, so the terminal step comes off the board.
        Assert.Null(Get<object?>(map, "_arrive"));

        int tankAtPark = Get<int>(map, "_reactionMassPulses");
        Assert.True(tankAtPark > 0, "an armed trip must never strand the captain.");
        _out.WriteLine($"tank {tankAtArm} → {tankAtPark} p across the whole trip");
    }

    /// <summary>
    /// THE HOLD: through the whole cruise the autopilot fires NOTHING of its own. Only the captain's two
    /// plotted burns are spent before the arrival window opens — the ruling's "absolutely no steps needed",
    /// read from the tank rather than from a sentence. (Without the hold the armed loop would start firing
    /// approach burns from Earth, because Mars's capture range is five Hill radii wide and its floor is three
    /// million km: the ship is "in range" of the encounter before she has left.)
    /// </summary>
    [Fact]
    public void THE_HOLD_TheAutopilotSpendsNothingOfItsOwnUntilThePassComesRound()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode);
        ClosestApproach.Pass pass = TheMarsPass(map);
        AddTheArriveStep(map, pass, ArrivalStepRule.ArrivalKind.Orbit);
        Invoke(map, "ArmArriveStep");
        Assert.Equal("mars", Get<string?>(map, "_armedOrbitBodyId"));

        int tankAtArm = Get<int>(map, "_reactionMassPulses");
        int plottedPulses = PlannedPulsesAhead(map);

        // Stop a long way short of the pass — half way there.
        Flight flight = WarpThroughWithNoFurtherInput(map, "mars", pass.SimTime * 0.5);
        _out.WriteLine($"half way: {flight}; armedStillAhead={Property(map, "ArmedArrivalStillAhead")}");

        Assert.True((bool)Property(map, "ArmedArrivalStillAhead")!,
            "half way to Mars the promise is still ahead — the autopilot has nothing to do yet.");
        Assert.Equal("mars", Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Null(Get<string?>(map, "_autopilotStandDownReason"));
        Assert.False(Get<bool>(map, "AutopilotFlyingApproach", isProperty: true),
            "…and the banner must not claim she is flying an approach months out.");

        int spent = tankAtArm - Get<int>(map, "_reactionMassPulses");
        Assert.Equal(plottedPulses, spent);
    }

    // ── (2) ANTI-VACUITY: THE PLAN THAT CANNOT END, AND IS TOLD SO ─────────────────────────────────────

    /// <summary>
    /// A PLAN-TIME ARM IS STILL A PROMISE, NOT A RUBBER STAMP. The same two burns, three times as hard: the
    /// course crosses Mars's orbit 8.4 M km wide and 10.3 km/s hot — past BOTH gates the flight itself obeys.
    /// The arm is refused with those numbers in it, and warping the same clock through leaves the ship
    /// exactly where a refused arm should leave her: still flying, never inserted.
    /// </summary>
    [Fact]
    public void A_PASS_TOO_FAR_AND_TOO_FAST_IsRefusedWithTheNumbers_AndNothingEverInsertsHer()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode * TooHotFactor);
        ClosestApproach.Pass pass = TheMarsPass(map);

        ArrivalStepRule.ArrivalCheck check = AddTheArriveStep(map, pass, ArrivalStepRule.ArrivalKind.Orbit);
        _out.WriteLine($"plotted arrival: {ArrivalStepRule.Verdict(check)}");
        Assert.False(check.Valid, "the bench's hot plan must NOT end safely, or the negative proves nothing.");

        Invoke(map, "ArmArriveStep");

        Assert.Null(Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Null(Get<double?>(map, "_armedArrivalPassSimTime"));
        string? refusal = Get<string?>(map, "_autopilotStandDownReason");
        _out.WriteLine($"refusal: {refusal}");
        Assert.NotNull(refusal);
        Assert.Contains("declines Mars", refusal);
        Assert.Contains("It won't strand you", refusal);
        // The numbers, not a shrug: both gates quoted, and which way the course is wrong.
        Assert.Contains("too far", refusal);
        Assert.Contains("too fast", refusal);
        Assert.Contains(ArrivalStepRule.FormatDistance(check.DistanceLimit), refusal);
        Assert.Contains(ArrivalStepRule.FormatSpeed(check.SpeedLimit), refusal);

        Flight flight = WarpThroughWithNoFurtherInput(map, "mars", pass.SimTime + 30 * Day);
        _out.WriteLine($"flown after the refusal: {flight}");
        Assert.False(Get<bool>(map, "_orbitKept"), "a refused arm must never quietly insert her anyway.");
        Assert.Null(Get<string?>(map, "_dockedHavenId"));
        Assert.Null(Get<string?>(map, "_armedOrbitBodyId"));
    }

    // ── (3) THE OWNER'S OWN BERTH ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// "…TO DOCK THE SHIP AT PLAN TIME": the same two burns, but the plan's last step is the ⚓ at The Rusty
    /// Roadstead, Mars's own berth. Armed 292 days out and left alone, the ship arrives CLAMPED — the trip
    /// planned from space to docked at one go, which is the ruling's first sentence word for word.
    /// </summary>
    [Fact]
    public void ARMED_AT_PLAN_TIME_TheShipDocksHerselfAtTheRustyRoadstead_WithNoFurtherInput()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode);
        ClosestApproach.Pass pass = ThePassBy(map, "the-space-bar");

        AddTheArriveStep(map, pass, ArrivalStepRule.ArrivalKind.Dock);
        Assert.Equal("the-space-bar", ArriveBodyId(map));

        Invoke(map, "ArmArriveStep");
        Assert.Equal("the-space-bar", Get<string?>(map, "_armedOrbitBodyId"));
        Assert.NotNull(Get<double?>(map, "_armedArrivalPassSimTime"));
        AssertTheBannerNamesTheArrival(map, "the autopilot docks");

        Flight flight = WarpThroughWithNoFurtherInput(map, "the-space-bar", pass.SimTime + 30 * Day);
        _out.WriteLine($"flown to the berth: {flight}");

        Assert.Equal("the-space-bar", Get<string?>(map, "_dockedHavenId"));
        Assert.Null(Get<object?>(map, "_arrive"));
        Assert.True(Get<int>(map, "_reactionMassPulses") > 0);
    }

    // ── The bench ──────────────────────────────────────────────────────────────────────────────────────

    private readonly record struct Flight(int Frames, double Days, double FinalDistance, bool Parked, bool Docked)
    {
        public override string ToString() =>
            $"{Frames} frames, {Days:F1} d, ended {FinalDistance:E2} m out, parked={Parked} docked={Docked}";
    }

    /// <summary>
    /// SPEND THE CLOCK, TOUCH NOTHING ELSE. Both phases are the shipping frame's own — the fixed-step loop
    /// that integrates, fires the plotted burns and lands exactly on any scheduled autopilot burn epoch, and
    /// the accounting phase that bills the fired nodes and runs <c>CheckArmedInsertion</c>. No key, no click,
    /// no arm, no re-plot: exactly what the owner means by "no steps needed".
    /// </summary>
    private Flight WarpThroughWithNoFurtherInput(Pages.Map map, string targetId, double untilSimTime)
    {
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        double start = Get<double>(map, "SimTime");
        int frames = 0;

        while (Get<double>(map, "SimTime") < untilSimTime && frames < 4000)
        {
            if (Get<bool>(map, "_orbitKept") || Get<string?>(map, "_dockedHavenId") is not null)
            {
                break; // arrived — the trip the plan promised is over
            }

            // The frame's own warp discipline, generalized: never step so far that a decision point could
            // be skipped. Coarse across the void (the loop's own 20,000-quantum ceiling), tightening as the
            // gap to the target closes — which is what UpdateEffectiveWarp's near-body cap does live.
            var ship = Get<ShipState>(map, "_ship");
            double simTime = Get<double>(map, "SimTime");
            Vector2d bodyPos = ephemeris.Position(targetId, simTime);
            Vector2d bodyVel = (ephemeris.Position(targetId, simTime + 1) - ephemeris.Position(targetId, simTime - 1)) / 2;
            double gap = (ship.Position - bodyPos).Length;
            double closing = Math.Max(1.0, (ship.Velocity - bodyVel).Length);
            double chunk = Math.Clamp(gap / closing / 10.0, 60.0, 20000 * 60.0);

            Set(map, "_effectiveWarp", 10000);  // the adaptive-quantum regime the live loop uses at warp
            Set(map, "_simAccumulator", chunk);
            int steps = (int)Invoke(map, "ConsumeTheAccumulator", false)!;
            Invoke(map, "PinHerToTheDockAndDriftTheGhost");
            Invoke(map, "AccountForWhatTheStepsDid", steps);
            frames++;
        }

        double end = Get<double>(map, "SimTime");
        var finalShip = Get<ShipState>(map, "_ship");
        return new Flight(
            frames, (end - start) / Day,
            (finalShip.Position - ephemeris.Position(targetId, end)).Length,
            Get<bool>(map, "_orbitKept"),
            Get<string?>(map, "_dockedHavenId") is not null);
    }

    /// <summary>A ship standing in free space off Earth — outside Earth's Hill sphere, on a heliocentric
    /// coast — with TWO plotted Vector burns that take her past Mars. Nothing is armed and nothing is
    /// docked; the projection and the pass list are built through the page's own reprojection and pass
    /// cadence, so the numbers the arrive step reads are the numbers the shipping code reads.</summary>
    private static Pages.Map AShipOffEarthWithTwoBurnsPastMars(double percentPerNode)
    {
        var map = new Pages.Map();
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));

        Vector2d earthPos = ephemeris.Position("earth", 0);
        Vector2d earthVel = (ephemeris.Position("earth", 1) - ephemeris.Position("earth", -1)) / 2;
        // 10 M km out from Earth along the sun line — well outside Earth's ~1.5 M km Hill sphere, so she is
        // genuinely in free space and not "parked at Earth" (the #950 trap).
        Set(map, "_ship", new ShipState(earthPos + earthPos.Normalized() * 1.0e10, earthVel, 0));
        Set(map, "SimTime", 0.0);
        Set(map, "_reactionMassPulses", 500);
        Set(map, "_horizonChoice", "400");   // the ribbon must reach the encounter it is being planned to

        AddPlottedVectorBurn(map, FirstBurnSimTime, percentPerNode, DepartureHeadingDegrees);
        AddPlottedVectorBurn(map, SecondBurnSimTime, percentPerNode, DepartureHeadingDegrees);
        Invoke(map, "RebuildPlan");
        Invoke(map, "ReprojectTrajectory");
        Invoke(map, "ReprojectThePassesOnTheirCadence", 1000.0);
        return map;
    }

    private static void AddPlottedVectorBurn(Pages.Map map, double simTime, double percent, double heading)
    {
        Type nodeType = typeof(Pages.Map).GetNestedType("PlanNode", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Map.PlanNode is gone — this bench has drifted.");
        object node = Activator.CreateInstance(nodeType, nonPublic: true)!;
        SetField(node, "SimTime", simTime);
        SetField(node, "Action", ManeuverAction.Accelerate);
        SetField(node, "Pulses", 1);
        SetField(node, "Percent", percent);
        SetField(node, "Mode", BurnMode.Vector);
        SetField(node, "HeadingDegrees", heading);
        var nodes = (IList)Get<object>(map, "_planNodes");
        nodes.Add(node);
    }

    /// <summary>Add the arrive step the way the button adds it: scrub to the encounter, then press
    /// "+ Add orbit / dock at scrub". Returns the row's verdict as the page computes it.</summary>
    private static ArrivalStepRule.ArrivalCheck AddTheArriveStep(
        Pages.Map map, ClosestApproach.Pass pass, ArrivalStepRule.ArrivalKind kind)
    {
        Set(map, "_scrubOffsetSeconds", pass.SimTime - Get<ShipState>(map, "_ship").SimTime);
        Invoke(map, "AddArriveAtScrub", kind);
        Assert.NotNull(Get<object?>(map, "_arrive"));
        return (ArrivalStepRule.ArrivalCheck)Invoke(map, "ArriveCheck")!;
    }

    private static ClosestApproach.Pass TheMarsPass(Pages.Map map) => ThePassBy(map, "mars");

    private static ClosestApproach.Pass ThePassBy(Pages.Map map, string bodyId)
    {
        object? pass = Invoke(map, "ArrivePassFor", bodyId);
        Assert.True(pass is not null, $"the plotted course must have a pass by {bodyId} — this bench has drifted.");
        return (ClosestApproach.Pass)pass!;
    }

    private static string ArriveBodyId(Pages.Map map)
    {
        object step = Get<object>(map, "_arrive");
        return (string)step.GetType().GetProperty("BodyId", Hidden)!.GetValue(step)!;
    }

    private static double DistanceTo(Pages.Map map, string bodyId)
    {
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        var ship = Get<ShipState>(map, "_ship");
        return (ship.Position - ephemeris.Position(bodyId, ship.SimTime)).Length;
    }

    private static int PlannedPulsesAhead(Pages.Map map) => (int)Invoke(map, "PlannedPulseTotal")!;

    /// <summary>The NOW/NEXT banner queue must name the arrival as a step, in the words that say who
    /// finishes the trip — the captain reads this without opening the Nav desk.</summary>
    private static void AssertTheBannerNamesTheArrival(Pages.Map map, string expectedPhrase)
    {
        var status = (FlightPlanStatus)Invoke(map, "FlightNowNext")!;
        bool found = false;
        foreach (FlightPlanRow row in status.Rows)
        {
            if (row.Text.Contains(expectedPhrase, StringComparison.Ordinal))
            {
                found = true;
            }
        }
        Assert.True(found,
            $"the banner queue should carry the armed arrival as \"{expectedPhrase}\"; it had: "
            + string.Join(" | ", System.Linq.Enumerable.Select(status.Rows, r => r.Text)));
    }

    // ── Reflection plumbing (the TheBerthEndsTheVoyageTests / TheBrakeCardKnowsSheIsClamped idiom) ──────

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

    private static T Get<T>(object o, string name, bool isProperty = false)
    {
        if (isProperty)
        {
            return (T)Property(o, name)!;
        }

        return (T)(o.GetType().GetField(name, Hidden)
            ?? throw new InvalidOperationException($"no field {name} on Map — this bench has drifted"))
            .GetValue(o)!;
    }

    private static object? Property(object o, string name) =>
        (o.GetType().GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"no property {name} on Map — this bench has drifted"))
        .GetValue(o);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}
