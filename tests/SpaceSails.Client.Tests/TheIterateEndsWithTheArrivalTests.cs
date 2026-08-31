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

    // ── (5) #1042 · THE OTHER EDGE: A PASS THE RIBBON MERELY *BEGINS* AT ───────────────────────────────

    /// <summary>
    /// <b>WITH THE SCRUB AT ZERO, THE BUTTON MUST NOT OFFER A BODY THE SHIP LEFT BEHIND BEFORE THE PLAN
    /// BEGAN.</b> #1041 fixed the END edge and left this one standing on purpose, but it left BOTH readings
    /// of a start-edge pass standing with it: "at closest approach NOW" and "opening from something whose
    /// closest approach was years ago" come out of <see cref="ClosestApproach.Passes"/> as the same artefact,
    /// pinned to the ribbon's first sample, and the compose button could not tell them apart. With the scrub
    /// at zero that artefact sits at delta-zero from the captain's finger and beats every real encounter on
    /// the line.
    ///
    /// <para>RED PROOF, watched on this bench before the fix: the orbit button read
    /// <c>🛰 + Add orbit at scrub (Uranus)</c> — 20.16 AU, pass pinned to day 0, closest approach 333 days
    /// in the past — and the dock button <c>⚓ + Add dock at scrub (The Tilt)</c>, same rock. On a 120 d line
    /// it read <c>(Neptune)</c>, 29.88 AU, closest approach 1761 days ago: the very body CI offered #1041's
    /// own gate.</para>
    /// </summary>
    [Fact]
    public void AT_SCRUB_ZERO_TheButtonOffersNoBodyTheShipLeftBehindBeforeThePlanBegan()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode, pinnedPathLengthDays: 5);
        Set(map, "_scrubOffsetSeconds", 0.0);

        // THE PREMISE, OUT LOUD. This sky really does pin Neptune's and Uranus's "closest pass" to the
        // ribbon's first sample while the ship opens from them — the state under test, or the bench proves
        // nothing (#1041's own lesson about betting on where the planets are).
        foreach (string leftBehind in new[] { "neptune", "uranus" })
        {
            ClosestApproach.Pass pass = PassFor(map, leftBehind);
            (double range, double rangeRate, double relSpeed) = Approach(map, leftBehind);
            double agoDays = range * rangeRate / (relSpeed * relSpeed) / Day;
            _out.WriteLine(
                $"{pass.BodyName}: pass at day {pass.SimTime / Day:F3}, {ArrivalStepRule.FormatDistance(pass.Distance)}, "
                + $"opening at {rangeRate / 1000:F1} km/s — closest approach was {agoDays:F0} d ago");
            Assert.Equal(0.0, pass.SimTime / Day, 0.01);
            Assert.True(agoDays > 100, $"{pass.BodyName} must be a body the ship left behind long before the plan.");
        }

        // THE FIX: neither of them is on the button any more, in either arrival's words.
        string orbit = ButtonLabel(map, ArrivalStepRule.ArrivalKind.Orbit);
        string dock = ButtonLabel(map, ArrivalStepRule.ArrivalKind.Dock);
        _out.WriteLine($"orbit button: {orbit}");
        _out.WriteLine($"dock  button: {dock}");
        foreach (string offer in new[] { orbit, dock })
        {
            Assert.DoesNotContain("Neptune", offer, StringComparison.Ordinal);
            Assert.DoesNotContain("Triton", offer, StringComparison.Ordinal);
            Assert.DoesNotContain("The Deep", offer, StringComparison.Ordinal);
            Assert.DoesNotContain("Uranus", offer, StringComparison.Ordinal);
            Assert.DoesNotContain("Miranda", offer, StringComparison.Ordinal);
            Assert.DoesNotContain("The Tilt", offer, StringComparison.Ordinal);
        }

        // …and the general law behind the two names, measured in this test's own arithmetic rather than
        // through the production predicate: whatever the button offers, either its pass is an encounter the
        // picture actually holds, or the ship is still at closest approach to it now.
        foreach (ArrivalStepRule.ArrivalKind kind in new[]
                 { ArrivalStepRule.ArrivalKind.Orbit, ArrivalStepRule.ArrivalKind.Dock })
        {
            if (Invoke(map, "ArriveCandidate", kind) is not ClosestApproach.Pass offered)
            {
                continue;
            }

            var samples = (IReadOnlyList<TrajectorySample>)Get<object>(map, "_samples");
            double step = samples[1].SimTime - samples[0].SimTime;
            if (offered.SimTime > samples[0].SimTime + step)
            {
                continue;   // an interior (or end-edge) pass: not this rule's business
            }

            (double range, double rangeRate, double relSpeed) = Approach(map, offered.BodyId);
            double secondsAgo = range * rangeRate / (relSpeed * relSpeed);
            Assert.True(
                secondsAgo <= step,
                $"the button offers {offered.BodyName} at the ribbon's first sample, but its closest approach "
                + $"was {secondsAgo / Day:F1} d before the plan begins — that is where the PICTURE starts, not "
                + "an encounter on this course.");
        }
    }

    /// <summary>
    /// <b>AND THE ✓ SURVIVES — THE ASYMMETRY IS THE POINT.</b> #1041 kept start-edge passes judged for one
    /// stated reason: <i>"silencing it would take the ✓ away from the captain coasting past the body he means
    /// to orbit."</i> So here is that captain: a ship abeam of Mars, 1 M km out, moving square across the
    /// line to it at 2 km/s — genuinely AT closest approach this instant, with the pass pinned to the ribbon's
    /// first sample exactly like Neptune's was. He must keep his offer AND his ✓.
    ///
    /// <para>Without this fact the fix would be a blanket mute and the ✓ would be gone, which is precisely
    /// what #1041 refused to do.</para>
    /// </summary>
    [Fact]
    public void A_CAPTAIN_AT_CLOSEST_APPROACH_NOW_KeepsHisOfferAndHisTick()
    {
        Pages.Map map = AShipAbeamOfMarsAtClosestApproach();
        Set(map, "_scrubOffsetSeconds", 0.0);

        // The premise: Mars's pass really is pinned to the ribbon's first sample — the same artefact shape.
        ClosestApproach.Pass mars = PassFor(map, "mars");
        (double range, double rangeRate, double relSpeed) = Approach(map, "mars");
        _out.WriteLine(
            $"Mars: pass at day {mars.SimTime / Day:F4}, {ArrivalStepRule.FormatDistance(mars.Distance)}, "
            + $"range rate {rangeRate:F1} m/s, rel {relSpeed / 1000:F2} km/s");
        Assert.Equal(0.0, mars.SimTime / Day, 0.01);
        Assert.False((bool)Invoke(map, "PassIsOnlyTheRibbonsBeginning", mars)!,
            "a ship AT closest approach now is not a picture that merely begins here.");

        // The offer stands, by name…
        Assert.Contains("Mars", ButtonLabel(map, ArrivalStepRule.ArrivalKind.Orbit), StringComparison.Ordinal);

        // …and so does the verdict it was protecting.
        Invoke(map, "AddArriveAtScrub", ArrivalStepRule.ArrivalKind.Orbit);
        ArrivalStepRule.ArrivalCheck check = TheCheck(map);
        _out.WriteLine($"verdict: {ArrivalStepRule.Verdict(check)}");
        Assert.True(check.Valid, $"the coasting captain's ✓ must survive #1042: {ArrivalStepRule.Verdict(check)}");
    }

    // ── (6) #1042 · THE SCRUB CANNOT POINT PAST THE END OF THE WORLD IT SCRUBS ─────────────────────────

    /// <summary>
    /// <b>SHORTEN THE LINE AND THE SCRUB COMES BACK ONTO IT.</b> The scrub slider's <c>max</c> IS the path
    /// length, but the bound value was only ever written by a hand on the control — so every way the line got
    /// SHORTER left a scrub standing past the end of the drawn world, resolving through
    /// <c>SamplePositionAtTime</c>'s "past the end → the last sample" fallback. The ghost ship, the scrub
    /// clock, the node-epoch floor and every "at scrub" button then all agreed on an hour that is not on the
    /// picture.
    ///
    /// <para>RED PROOF, watched on this bench before the fix: <i>"the scrub is at 400.0 d on a line 30.0 d
    /// long"</i> — a value more than thirteen times its own control's maximum, and the fact never even
    /// reached its auto half. (With the clamp in, that half goes 648.9 d → 90.1 d and takes the scrub with
    /// it, which is what the printed line below reports.)</para>
    /// </summary>
    [Fact]
    public void SHRINKING_THE_PATH_LENGTH_BringsTheScrubBackOntoTheLine()
    {
        Pages.Map map = AShipOffEarthWithTwoBurnsPastMars(DeparturePercentPerNode, pinnedPathLengthDays: 400);
        ScrubToTheEndOfTheLine(map);
        Assert.Equal(400.0, Get<double>(map, "_scrubOffsetSeconds") / Day, 0.5);

        // The captain drags Path length down to look at the near term.
        Set(map, "_horizonChoice", "30");
        Invoke(map, "ReprojectTrajectory");

        double scrub = Get<double>(map, "_scrubOffsetSeconds");
        _out.WriteLine($"after shrinking to {Horizon(map) / Day:F1} d the scrub reads {scrub / Day:F1} d");
        Assert.True(scrub <= Horizon(map) + 1e-6,
            $"the scrub is at {scrub / Day:F1} d on a line {Horizon(map) / Day:F1} d long — past the end of the "
            + "world it scrubs, and past its own slider's maximum.");

        // …and it points at an hour the picture actually holds, not at the fallback's last sample.
        var samples = (IReadOnlyList<TrajectorySample>)Get<object>(map, "_samples");
        double scrubTime = (double)Property(map, "ScrubTime")!;
        Assert.InRange(scrubTime, samples[0].SimTime, samples[^1].SimTime);

        // The same must hold when it is AUTO that shortens the line behind the captain's back — nobody
        // touched the scrub control at all. On auto the plan's own ending stretches the ribbon to the Mars
        // encounter (#952); take the arrival off the plan again and it snaps back to last burn + 90 d, and
        // a scrub left at the old far end would be standing three hundred days outside the picture.
        Set(map, "_horizonChoice", "auto");
        Invoke(map, "AddArriveAtScrub", ArrivalStepRule.ArrivalKind.Orbit);
        ScrubToTheEndOfTheLine(map);
        double reachedFor = Horizon(map);
        Assert.True(reachedFor / Day > 300, $"the bench must start from the long auto line: {reachedFor / Day:F1} d");

        Invoke(map, "RemoveArriveStep");
        Invoke(map, "ReprojectTrajectory");

        double after = Get<double>(map, "_scrubOffsetSeconds");
        _out.WriteLine(
            $"auto held {reachedFor / Day:F1} d, snapped back to {Horizon(map) / Day:F1} d, scrub {after / Day:F1} d");
        Assert.True(Horizon(map) < reachedFor - Day, "the auto line must genuinely have shrunk, or this proves nothing.");
        Assert.True(after <= Horizon(map) + 1e-6,
            $"auto snapping the line back must bring the scrub with it: scrub {after / Day:F1} d on a "
            + $"{Horizon(map) / Day:F1} d line.");
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

    /// <summary>
    /// #1042 · <b>THE CAPTAIN COASTING PAST THE BODY HE MEANS TO ORBIT.</b> A ship abeam of Mars — one
    /// million km out, well inside the capture range the arrival is judged against — with her whole relative
    /// velocity square across the line to it, so the range rate is exactly zero and closest approach is THIS
    /// INSTANT. The pass therefore pins to the ribbon's first sample, the same artefact shape Neptune's does,
    /// and telling the two apart is the entire #1042 fix.
    ///
    /// <para>2 km/s relative is inside <see cref="OrbitRule.MaxRelativeSpeed"/>, so the arrival reads ✓ —
    /// which is the ✓ #1041 refused to give up and this bench exists to keep.</para>
    /// </summary>
    private static Pages.Map AShipAbeamOfMarsAtClosestApproach()
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

        Vector2d marsPos = ephemeris.Position("mars", 0);
        Vector2d marsVel = (ephemeris.Position("mars", 1) - ephemeris.Position("mars", -1)) / 2;

        // Offset along Mars's own velocity, relative motion square across it: r·v = 0, exactly.
        Vector2d along = marsVel.Normalized();
        var across = new Vector2d(-along.Y, along.X);
        Set(map, "_ship", new ShipState(marsPos + (along * AbeamRangeMeters), marsVel + (across * AbeamRelSpeed), 0));
        Set(map, "SimTime", 0.0);
        Set(map, "_reactionMassPulses", 500);
        Set(map, "_horizonChoice", "5");

        Invoke(map, "RebuildPlan");
        Invoke(map, "ReprojectTrajectory");
        Sweep(map, 1000.0);
        return map;
    }

    /// <summary>One million km: comfortably inside Mars's capture range (5.42 M km), so the arrival is a ✓.</summary>
    private const double AbeamRangeMeters = 1.0e9;

    /// <summary>2 km/s — under <see cref="OrbitRule.MaxRelativeSpeed"/>, so the insertion window is open.</summary>
    private const double AbeamRelSpeed = 2000.0;

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

    /// <summary>#1042 — the pass the sweep is holding for a body.</summary>
    private static ClosestApproach.Pass PassFor(Pages.Map map, string bodyId) =>
        (ClosestApproach.Pass)(Invoke(map, "ArrivePassFor", bodyId)
            ?? throw new InvalidOperationException($"no {bodyId} pass on the plotted course — this bench has drifted"));

    /// <summary>#1042 — range, range rate and relative speed at the ribbon's FIRST sample, off the page's own
    /// reading of its own projection.</summary>
    private static (double Range, double RangeRate, double RelSpeed) Approach(Pages.Map map, string bodyId) =>
        ((double, double, double))(Invoke(map, "LeadingApproach", bodyId)
            ?? throw new InvalidOperationException($"no leading approach for {bodyId} — this bench has drifted"));

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
