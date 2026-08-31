using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #952 · <b>ITERATE THE PATH UNTIL THE ARRIVAL IS ON THE END OF IT.</b>
///
/// <para>Owner, filing #952 over a screenshot of a plotted Mars run: <i>"I really like those iterate buttons,
/// especially the time iterate is sweet fine tune opportunity, but now the cherry on top of the cake is
/// missing when I cannot add the step to the end of the plan to orbit Mars here. It feels incomplete."</i>
/// #965 built the button and #969 made arming it a plan-time promise. What neither of them noticed is that
/// the LOOP under the button had a hole in it.</para>
///
/// <h3>The hole</h3>
/// <para>The plotted course has a LENGTH — the Path-length slider — and on "auto" that length reached the
/// plan's furthest BURN plus ninety days. The arrival was a step in the list but was never part of "the plan's
/// furthest encounter", so a plan whose burns fire in the first three hours and whose Mars encounter is nine
/// and a half months out drew a ribbon that stopped at day 90: two hundred days short of the plan's own
/// ending.</para>
///
/// <para>That alone would only be a short picture. What made it a bug is the second half:
/// <see cref="ClosestApproach.Passes"/> reports a closest approach for EVERY body in the system whether or not
/// the ribbon goes near it, so for Mars it returned the LAST SAMPLE of the ribbon — 0.49 AU out, 11.3 km/s,
/// pinned to day 90. The arrive row judged that artefact and printed, in confident numbers,
/// <c>"✗ no orbit at Mars — 0.45 AU too far, 6.3 km/s too fast"</c> over a course that in truth arrives
/// <b>383,764 km from Mars at 2.3 km/s</b> — inside BOTH gates — on day 291.8. This repo's own named bug
/// class: the sim doing one thing while a sentence reports another. And it is precisely fatal to the loop the
/// owner is describing, because the shortfall points the wrong way: no amount of ±p / ±d / ±h will ever close
/// a gap that was never there.</para>
///
/// <h3>RED PROOF (watched before this shipped)</h3>
/// <para>On b71e1c5 — the commit before the fix — every fact below fails as follows. (1) the auto press
/// reads <c>✗ no orbit at Mars — pass 0.49 AU … (0.45 AU too far, 6.3 km/s too fast)</c> and the pass
/// epoch is 90.1 d, not 291.8 d. (2) a hand-shortened path length yields a full <c>ArrivalCheck</c> with
/// <c>Valid == false</c> instead of no verdict, and there is no such method as <c>ArriveRibbonTooShortLine</c>.
/// (3) dragging the path length short under a ✓ plan fires <c>BrokenPlanAlarm</c> — the "NOBODY IS FLYING THE
/// SHIP" pop-up — and drops warp to 1×, over nothing but a shortened view.</para>
///
/// <h3>Anti-vacuity</h3>
/// <para>(4) flies the SAME bench with three times the departure — a course that really does cross Mars's
/// orbit far too wide and too hot — and asserts the row still speaks a true ✗ with both gates quoted. Silencing
/// the fabricated refusal did not silence the honest one.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheIterateEndsWithTheArrivalTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public TheIterateEndsWithTheArrivalTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const double Day = 86400.0;

    /// <summary>The departure that reaches Mars — the very constants #969's bench pins, solved once with the
    /// game's own Lambert departure solver and split across two plotted Vector nodes. Shared on purpose: the
    /// two files must be talking about the same voyage.</summary>
    private const double DeparturePercentPerNode = 3.920151388528919;

    private const double DepartureHeadingDegrees = 248.18684551285557;

    /// <summary>Three times the departure: a course that genuinely cannot end at Mars — the anti-vacuous case.</summary>
    private const double TooHotFactor = 3.0;

    private const double FirstBurnSimTime = 1800.0;
    private const double SecondBurnSimTime = 3 * 3600.0;

    /// <summary>The encounter the plan really has, near enough to recognise it and loose enough to survive a
    /// world that drifts a little. #969's own bench flies to it and parks.</summary>
    private const double TheRealMarsPassDays = 291.8;

    // ── (1) THE PRESS TELLS THE TRUTH ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// ON AUTO PATH LENGTH, ONE PRESS PUTS THE ENCOUNTER ON THE LINE — and the row reads the real pass.
    /// This is the owner's loop closing: he plots the burns, presses "+ Add orbit at scrub (Mars)", and the
    /// answer he gets back is about his course, not about where the picture happened to stop.
    /// </summary>
    [Fact]
    public void THE_PRESS_ReachesTheEncounter_AndTheRowJudgesTheRealPass_NotTheEndOfTheRibbon()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode, pinnedPathLengthDays: null);

        // The bench starts exactly where the owner's did: auto, and far too short to see Mars at all.
        Assert.Equal("auto", Get<string>(map, "_horizonChoice"));
        double horizonBefore = Horizon(map);
        Assert.True(horizonBefore < 200 * Day,
            $"the bench must START with a ribbon short of the encounter, or it proves nothing: {horizonBefore / Day:F1} d");
        Assert.True(MarsPassDays(map) < 200,
            "…and Mars's 'closest pass' must START as the ribbon's own edge, which is the artefact under test.");
        _out.WriteLine($"before: ribbon {horizonBefore / Day:F1} d, Mars 'pass' at {MarsPassDays(map):F1} d");

        ScrubToTheEndOfTheLine(map);
        Assert.Contains("Mars", ButtonLabel(map, ArrivalStepRule.ArrivalKind.Orbit));
        Invoke(map, "AddArriveAtScrub", ArrivalStepRule.ArrivalKind.Orbit);

        // THE FIX, in one line: the plan's own ending is now on the ribbon, so the pass is the encounter.
        _out.WriteLine($"after:  ribbon {Horizon(map) / Day:F1} d, Mars pass at {MarsPassDays(map):F1} d");
        Assert.Equal(TheRealMarsPassDays, MarsPassDays(map), 1.0);
        Assert.True(Horizon(map) > TheRealMarsPassDays * Day,
            $"the auto ribbon must now reach past the plan's own ending; it is {Horizon(map) / Day:F1} d");

        // …and the verdict is the true one — inside BOTH gates, exactly the arrival #969 flies to and parks at.
        ArrivalStepRule.ArrivalCheck check = TheCheck(map);
        _out.WriteLine($"verdict: {ArrivalStepRule.Verdict(check)}");
        Assert.True(check.Valid,
            $"this course DOES end safely at Mars; the row must say so: {ArrivalStepRule.Verdict(check)}");
        Assert.False((bool)Invoke(map, "ArriveRibbonIsTooShort")!);
        Assert.Null(Invoke(map, "ArriveRibbonTooShortLine"));

        // The plan's furthest encounter now counts its own last step — the one fact the auto length reads.
        Assert.Equal(TheRealMarsPassDays, (double)Invoke(map, "PlanFurthestEpochSeconds")! / Day, 1.0);
    }

    /// <summary>
    /// AND IT STAYS PUT. The press converges in a bounded couple of turns; a further sweep and reprojection —
    /// the 300 ms cadence doing its ordinary work — must not walk the ribbon anywhere, or the panel would
    /// breathe in and out under the captain's finger.
    /// </summary>
    [Fact]
    public void THE_RIBBON_SettlesAndStaysSettled_AcrossFurtherSweeps()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode, pinnedPathLengthDays: null);
        ScrubToTheEndOfTheLine(map);
        Invoke(map, "AddArriveAtScrub", ArrivalStepRule.ArrivalKind.Orbit);

        double settled = Horizon(map);
        for (int sweep = 0; sweep < 4; sweep++)
        {
            Invoke(map, "ReprojectTrajectory");
            Sweep(map, 100000.0 + (sweep * 1000));
            Assert.Equal(settled / Day, Horizon(map) / Day, 1.0);
            Assert.Equal(TheRealMarsPassDays, MarsPassDays(map), 1.0);
            Assert.True(TheCheck(map).Valid);
        }

        _out.WriteLine($"settled at {settled / Day:F1} d and held it across four sweeps");
        Assert.False(Get<bool>(map, "_horizonDirty"),
            "a settled ribbon must stop asking to be re-projected, or the panel re-projects for ever.");
    }

    // ── (2) NOT JUDGED IS NOT INVALID ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CAPTAIN'S OWN FINGER ON PATH LENGTH IS NEVER OVERRULED — and a course too short to reach the body
    /// yields NO VERDICT, plus the sentence saying which control ends the wait. "Invalid" is a claim about a
    /// plan; this is a confession about the picture, and the two must never be printed in the same words.
    /// </summary>
    [Fact]
    public void A_HAND_PINNED_SHORT_PATH_GivesNoVerdictAtAll_AndSaysWhichControlFixesIt()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode, pinnedPathLengthDays: 120);
        ScrubToTheEndOfTheLine(map);
        Invoke(map, "AddArriveAtScrub", ArrivalStepRule.ArrivalKind.Orbit);

        // His length is his. Nothing reached for the cap behind his back.
        Assert.Equal("120", Get<string>(map, "_horizonChoice"));
        Assert.Equal(120.0, Horizon(map) / Day, 0.5);

        Assert.True((bool)Invoke(map, "ArriveRibbonIsTooShort")!);
        Assert.Null(Invoke(map, "ArriveCheck"));   // ← no fabricated ✗, and no ✓ either: no claim at all

        var line = (string?)Invoke(map, "ArriveRibbonTooShortLine");
        _out.WriteLine($"row says: {line}");
        Assert.NotNull(line);
        Assert.Contains("not judged", line!);
        Assert.Contains("Mars", line!);          // never a body he did not choose (#950)
        Assert.Contains("Path length", line!);   // …and the control that ends the wait, by name

        // ANTI-VACUITY: the same bench, same burns, one long-enough path — a real, positive verdict.
        Set(map, "_horizonChoice", "400");
        Invoke(map, "ReprojectTrajectory");
        Sweep(map, 100000.0);
        Assert.False((bool)Invoke(map, "ArriveRibbonIsTooShort")!);
        Assert.True(TheCheck(map).Valid,
            "with the path long enough, the very same course must read ✓ — otherwise the short case proves nothing.");
    }

    // ── (3) THE WAKE-UP CALL IS FOR A RUINED PLAN, NOT A SHORTENED VIEW ────────────────────────────────

    /// <summary>
    /// SHORTENING THE PICTURE MUST NOT WAKE THE CAPTAIN. The #952 alarm is deliberately loud — a pop-up he
    /// cannot miss, a persistent banner, a ledger receipt and warp slammed to 1× — because it means nobody is
    /// flying the ship. Dragging Path length down is not that. It is looking at less of the same plan, and on
    /// the old code it fired the alarm every time, because the shortened ribbon manufactured a ✗.
    /// </summary>
    [Fact]
    public void DRAGGING_THE_PATH_LENGTH_SHORT_DoesNotCryWolfAtASleepingCaptain()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode, pinnedPathLengthDays: null);
        ScrubToTheEndOfTheLine(map);
        Invoke(map, "AddArriveAtScrub", ArrivalStepRule.ArrivalKind.Orbit);
        Assert.True(TheCheck(map).Valid, "the bench must start from a plan that DOES end safely.");

        Set(map, "Warp", 10000);
        Set(map, "_arriveAlarm", null);

        // The captain drags Path length down to look at the near term — the plan is untouched.
        Set(map, "_horizonChoice", "60");
        Invoke(map, "ReprojectTrajectory");
        Sweep(map, 100000.0);

        _out.WriteLine($"alarm after shortening: {Get<string?>(map, "_arriveAlarm") ?? "(none)"}");
        Assert.Null(Get<string?>(map, "_arriveAlarm"));
        Assert.Equal(10000, Get<int>(map, "Warp"));
        Assert.True((bool)Invoke(map, "ArriveRibbonIsTooShort")!);

        // …and stretching it back finds the same good plan, with nothing having been cried over.
        Set(map, "_horizonChoice", "400");
        Invoke(map, "ReprojectTrajectory");
        Sweep(map, 200000.0);
        Assert.True(TheCheck(map).Valid);
        Assert.Null(Get<string?>(map, "_arriveAlarm"));
    }

    // ── (4) ANTI-VACUITY: THE HONEST ✗ STILL SPEAKS ───────────────────────────────────────────────────

    /// <summary>
    /// A COURSE THAT REALLY CANNOT END AT MARS IS STILL REFUSED, IN NUMBERS. Three times the departure crosses
    /// Mars's orbit millions of km wide and kilometres-per-second hot, on a ribbon long enough to see the whole
    /// thing — so the ✗ is about the plan, and both gates and the shortfall are quoted, which is what tells the
    /// captain which way to iterate.
    /// </summary>
    [Fact]
    public void A_COURSE_THAT_TRULY_MISSES_StillReadsAnHonestRefusal_WithBothGates()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(
            DeparturePercentPerNode * TooHotFactor, pinnedPathLengthDays: 400);
        ScrubToTheMarsPass(map);
        Invoke(map, "AddArriveAtScrub", ArrivalStepRule.ArrivalKind.Orbit);

        Assert.False((bool)Invoke(map, "ArriveRibbonIsTooShort")!,
            "the ribbon must be long enough that the ✗ is about the COURSE, not about the picture.");

        ArrivalStepRule.ArrivalCheck check = TheCheck(map);
        _out.WriteLine($"verdict: {ArrivalStepRule.Verdict(check)}");
        Assert.False(check.Valid);

        string verdict = ArrivalStepRule.Verdict(check);
        Assert.Contains("Mars", verdict);
        Assert.Contains(ArrivalStepRule.FormatDistance(check.DistanceLimit), verdict);
        Assert.Contains(ArrivalStepRule.FormatSpeed(check.SpeedLimit), verdict);
        Assert.NotEqual("within both gates", ArrivalStepRule.Shortfall(check));
    }

    // ── The bench (the #969 / TheBerthEndsTheVoyage reflection idiom) ──────────────────────────────────

    /// <summary>A ship standing in free space off Earth with two plotted Vector burns that take her past Mars
    /// — #969's bench, with one difference that is the whole point: Path length is left where the captain
    /// finds it (auto) unless a test pins it by hand.</summary>
    private static Pages.Map AShipOffEarthWithTwoBurnsPastMars(double percentPerNode, int? pinnedPathLengthDays)
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
        Set(map, "_ship", new ShipState(earthPos + earthPos.Normalized() * 1.0e10, earthVel, 0));
        Set(map, "SimTime", 0.0);
        Set(map, "_reactionMassPulses", 500);
        if (pinnedPathLengthDays is { } days)
        {
            Set(map, "_horizonChoice", days.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        AddPlottedVectorBurn(map, FirstBurnSimTime, percentPerNode, DepartureHeadingDegrees);
        AddPlottedVectorBurn(map, SecondBurnSimTime, percentPerNode, DepartureHeadingDegrees);
        Invoke(map, "RebuildPlan");
        Invoke(map, "ReprojectTrajectory");
        Sweep(map, 1000.0);
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

    /// <summary>The pass sweep on its own cadence — the shipping method, so the passes the row reads are the
    /// passes the game reads. The timestamp only has to clear the 300 ms gate.</summary>
    private static void Sweep(Pages.Map map, double timestampMs)
    {
        Set(map, "_passDirty", true);
        Set(map, "_lastReprojectMs", 0.0);
        Invoke(map, "ReprojectThePassesOnTheirCadence", timestampMs);
    }

    /// <summary>The captain's finger: drag the scrub to the far end of the line, which is where he is looking
    /// when the encounter he wants is off the end of it.</summary>
    private static void ScrubToTheEndOfTheLine(Pages.Map map) =>
        Set(map, "_scrubOffsetSeconds", Horizon(map));

    private static void ScrubToTheMarsPass(Pages.Map map) =>
        Set(map, "_scrubOffsetSeconds", MarsPass(map).SimTime - Get<ShipState>(map, "_ship").SimTime);

    private static double Horizon(Pages.Map map) => (double)Property(map, "CurrentPlotHorizonSeconds")!;

    private static ClosestApproach.Pass MarsPass(Pages.Map map) =>
        (ClosestApproach.Pass)(Invoke(map, "ArrivePassFor", "mars")
            ?? throw new InvalidOperationException("no Mars pass on the plotted course — this bench has drifted"));

    private static double MarsPassDays(Pages.Map map) => MarsPass(map).SimTime / Day;

    private static string ButtonLabel(Pages.Map map, ArrivalStepRule.ArrivalKind kind) =>
        (string)Invoke(map, "ArriveButtonLabel", kind)!;

    private static ArrivalStepRule.ArrivalCheck TheCheck(Pages.Map map) =>
        Invoke(map, "ArriveCheck") is ArrivalStepRule.ArrivalCheck c
            ? c
            : throw new InvalidOperationException("the arrival gave no verdict where this test needs one");

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
