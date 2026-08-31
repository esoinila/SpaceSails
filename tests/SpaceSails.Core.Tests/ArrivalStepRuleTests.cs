using System.Globalization;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #952/#955/#950 — <b>THE ARRIVE STEP'S VALID/INVALID BIT, and the sentence it owes the captain.</b>
/// The owner, iterating a Mars plan with the ±p / ±d / ±h buttons: <i>"the cherry on top of the cake is
/// missing when I cannot add the step to the end of the plan to orbit Mars here… It should have some bit
/// valid / invalid step, so that if we ruin it mid flight it would say so in the list and as a pop-up
/// that we no longer have safely ending flight plan."</i>
///
/// <para>Two laws are gated here. First, <b>the bit is the sim's own bit</b> — this repo's named bug class
/// is "the sim does one thing while a sentence reports another", so the tests below do not merely check
/// that the rule is self-consistent: they build real ship states and assert the badge agrees with
/// <see cref="OrbitRule.WindowOpen"/> and <see cref="DockRule.InEnvelope"/>, at the boundary, where the
/// two rules' comparison SENSES genuinely differ. Second, <b>the alarm is a one-shot on the transition</b>
/// — a plan that was good and got ruined wakes the captain once, and a plan that was never good does not
/// nag.</para>
/// </summary>
public class ArrivalStepRuleTests
{
    private const double Hill = 6.0e8;   // a roomy planet-ish Hill radius; capture range floors at 3e9 anyway
    private const double Day = 86400.0;

    private static ArrivalStepRule.ArrivalCheck Orbit(double distance, double relSpeed) =>
        ArrivalStepRule.Check(ArrivalStepRule.ArrivalKind.Orbit, "Mars", distance, relSpeed, Hill);

    private static ArrivalStepRule.ArrivalCheck Dock(double distance, double relSpeed) =>
        ArrivalStepRule.Check(ArrivalStepRule.ArrivalKind.Dock, "The Rusty Roadstead", distance, relSpeed, 0);

    // ===== The thresholds are BORROWED, never invented =====

    [Fact]
    public void TheGates_AreTheVeryConstantsTheFlightObeys()
    {
        // If this test ever needs new numbers written into it, the arrive step has started lying.
        Assert.Equal(OrbitRule.CaptureRange(Hill), ArrivalStepRule.DistanceLimit(ArrivalStepRule.ArrivalKind.Orbit, Hill));
        Assert.Equal(OrbitRule.MaxRelativeSpeed, ArrivalStepRule.SpeedLimit(ArrivalStepRule.ArrivalKind.Orbit));
        Assert.Equal(DockRule.EnvelopeMeters, ArrivalStepRule.DistanceLimit(ArrivalStepRule.ArrivalKind.Dock, Hill));
        Assert.Equal(DockRule.MatchSpeed, ArrivalStepRule.SpeedLimit(ArrivalStepRule.ArrivalKind.Dock));

        // And the dock gate ignores the Hill radius entirely — a μ=0 station has none.
        Assert.Equal(
            ArrivalStepRule.DistanceLimit(ArrivalStepRule.ArrivalKind.Dock, 0),
            ArrivalStepRule.DistanceLimit(ArrivalStepRule.ArrivalKind.Dock, 9.9e12));
    }

    // ===== A step that is VALID =====

    [Fact]
    public void AnArrivalWellInsideBothGates_IsValid_AndSaysSoWithTheNumbers()
    {
        // The owner's Mars pass: 64,951 km, comfortably slow.
        ArrivalStepRule.ArrivalCheck c = Orbit(distance: 6.4951e7, relSpeed: 3_100);

        Assert.True(c.Valid);
        Assert.True(c.DistanceOk);
        Assert.True(c.SpeedOk);
        Assert.Equal("✓", ArrivalStepRule.Badge(c));
        Assert.Equal(0, c.DistanceShortfall);
        Assert.Equal(0, c.SpeedShortfall);

        string verdict = ArrivalStepRule.Verdict(c);
        Assert.StartsWith("✓ orbit at Mars", verdict, StringComparison.Ordinal);
        Assert.Contains("64951 km", verdict, StringComparison.Ordinal);   // the pass itself
        Assert.Contains("3.1 km/s", verdict, StringComparison.Ordinal);   // and the speed
        Assert.Contains(ArrivalStepRule.FormatDistance(OrbitRule.CaptureRange(Hill)), verdict, StringComparison.Ordinal);
        Assert.Contains("5.0 km/s", verdict, StringComparison.Ordinal);   // OrbitRule.MaxRelativeSpeed
    }

    // ===== A step that fails on DISTANCE =====

    [Fact]
    public void AnArrivalThatMissesByDistanceAlone_IsInvalid_AndQuotesHowFar()
    {
        // Half an AU wide of a dock envelope that reaches 500,000 km, but drifting slowly.
        double distance = 0.5 * 1.495978707e11;
        ArrivalStepRule.ArrivalCheck c = Dock(distance, relSpeed: 2_000);

        Assert.False(c.Valid);
        Assert.False(c.DistanceOk);
        Assert.True(c.SpeedOk);
        Assert.Equal("✗", ArrivalStepRule.Badge(c));
        Assert.Equal(distance - DockRule.EnvelopeMeters, c.DistanceShortfall, 3);
        Assert.Equal(0, c.SpeedShortfall);

        string verdict = ArrivalStepRule.Verdict(c);
        Assert.StartsWith("✗ no dock at The Rusty Roadstead", verdict, StringComparison.Ordinal);
        Assert.Contains("need ≤ " + ArrivalStepRule.FormatDistance(DockRule.EnvelopeMeters), verdict, StringComparison.Ordinal);
        Assert.Contains("too far", verdict, StringComparison.Ordinal);
        Assert.DoesNotContain("too fast", verdict, StringComparison.Ordinal);
        // The speed clause is quoted anyway: the captain gets BOTH numbers, always.
        Assert.Contains("rel 2.0 km/s", verdict, StringComparison.Ordinal);
    }

    // ===== A step that fails on SPEED =====

    [Fact]
    public void AnArrivalThatIsOnlyTooFast_IsInvalid_AndQuotesHowMuchTooFast()
    {
        // Right in the berth's lap at 200,000 km, but tearing past at 12.4 km/s — the owner's #957 read.
        ArrivalStepRule.ArrivalCheck c = Dock(distance: 2.0e8, relSpeed: 12_400);

        Assert.False(c.Valid);
        Assert.True(c.DistanceOk);
        Assert.False(c.SpeedOk);
        Assert.Equal(0, c.DistanceShortfall);
        Assert.Equal(12_400 - DockRule.MatchSpeed, c.SpeedShortfall, 3);

        string verdict = ArrivalStepRule.Verdict(c);
        Assert.Contains("rel 12.4 km/s, need ≤ 8.0 km/s", verdict, StringComparison.Ordinal);
        Assert.Contains("4.4 km/s too fast", verdict, StringComparison.Ordinal);
        Assert.DoesNotContain("too far", verdict, StringComparison.Ordinal);
    }

    [Fact]
    public void AnArrivalThatFailsBothWays_SaysBoth()
    {
        ArrivalStepRule.ArrivalCheck c = Dock(distance: 3.6e9, relSpeed: 12_400);
        Assert.False(c.Valid);

        string shortfall = ArrivalStepRule.Shortfall(c);
        Assert.Contains("too far", shortfall, StringComparison.Ordinal);
        Assert.Contains("too fast", shortfall, StringComparison.Ordinal);
        Assert.Contains(shortfall, ArrivalStepRule.Verdict(c), StringComparison.Ordinal);
    }

    // ===== The badge agrees with the SIM, at the boundary, where the senses differ =====

    [Fact]
    public void AtTheSpeedBoundary_TheBadgeAgreesWithDockRule_Inclusive()
    {
        // DockRule.InEnvelope accepts relSpeed <= MatchSpeed. So must the badge — exactly, not nearly.
        var station = new CelestialBody("berth", "The Rusty Roadstead", "sun", 0, 1_000, 2e11, 3e7, 0, BodyKind.Station, IsHaven: true);
        var stationPos = new Vector2d(0, 0);
        var stationVel = new Vector2d(0, 0);
        var ship = new ShipState(new Vector2d(2.0e8, 0), new Vector2d(0, DockRule.MatchSpeed), 0);

        bool sim = DockRule.InEnvelope(ship, stationPos, stationVel, station.BodyRadius);
        bool badge = Dock(2.0e8, DockRule.MatchSpeed).Valid;
        Assert.True(sim);
        Assert.Equal(sim, badge);

        // One metre per second over and BOTH say no.
        var tooFast = ship with { Velocity = new Vector2d(0, DockRule.MatchSpeed + 1) };
        Assert.False(DockRule.InEnvelope(tooFast, stationPos, stationVel, station.BodyRadius));
        Assert.False(Dock(2.0e8, DockRule.MatchSpeed + 1).Valid);
    }

    [Fact]
    public void AtTheSpeedBoundary_TheBadgeAgreesWithOrbitRule_Strict()
    {
        // OrbitRule.WindowOpen is STRICT: relSpeed < MaxRelativeSpeed. A badge that borrowed the dock's
        // inclusive sense would stand green over a window the sim refuses to open. It does not.
        var planet = new CelestialBody("mars", "Mars", "sun", 4.2828e13, 3.39e6, 2.28e11, 5.94e7, 0);
        double hill = 1.0e9;
        var bodyPos = new Vector2d(0, 0);
        var bodyVel = new Vector2d(0, 0);
        var atTheLimit = new ShipState(new Vector2d(5.0e8, 0), new Vector2d(0, OrbitRule.MaxRelativeSpeed), 0);

        Assert.False(OrbitRule.WindowOpen(atTheLimit, bodyPos, bodyVel, planet, hill));
        Assert.False(ArrivalStepRule.SpeedWithin(ArrivalStepRule.ArrivalKind.Orbit, OrbitRule.MaxRelativeSpeed, OrbitRule.MaxRelativeSpeed));

        var justUnder = atTheLimit with { Velocity = new Vector2d(0, OrbitRule.MaxRelativeSpeed - 1) };
        Assert.True(OrbitRule.WindowOpen(justUnder, bodyPos, bodyVel, planet, hill));
        Assert.True(ArrivalStepRule.SpeedWithin(ArrivalStepRule.ArrivalKind.Orbit, OrbitRule.MaxRelativeSpeed - 1, OrbitRule.MaxRelativeSpeed));
    }

    [Fact]
    public void AtTheDistanceBoundary_TheCaptureRangeIsInclusive_LikeThePassTest()
    {
        // The client's PassIsOrbitable rejects on distance > captureRange, so exactly AT the range is armable.
        double range = OrbitRule.CaptureRange(Hill);
        Assert.True(Orbit(range, 1_000).Valid);
        Assert.False(Orbit(range + 1, 1_000).DistanceOk);
    }

    // ===== The mid-flight flip =====

    [Fact]
    public void TheAlarm_FiresOnlyOnTheTransitionFromAPlanThatEndedSafely()
    {
        // Ruined mid-flight: the one case the captain must be woken for.
        Assert.True(ArrivalStepRule.ShouldWarn(wasValid: true, nowValid: false));

        // Already broken, and still broken: no nagging, frame after frame.
        Assert.False(ArrivalStepRule.ShouldWarn(wasValid: false, nowValid: false));

        // Added broken (never yet judged): the row already says ✗ and the captain is looking at it.
        Assert.False(ArrivalStepRule.ShouldWarn(wasValid: null, nowValid: false));

        // Fixed, and staying fixed: nothing to say.
        Assert.False(ArrivalStepRule.ShouldWarn(wasValid: false, nowValid: true));
        Assert.False(ArrivalStepRule.ShouldWarn(wasValid: true, nowValid: true));
    }

    [Fact]
    public void TheAlarm_ReArms_SoASecondRuiningIsAlsoHeard()
    {
        // Walk one arrival's life: good → ruined (alarm) → still ruined (silence) → fixed → ruined (alarm).
        bool? last = null;
        var fired = new List<int>();
        bool[] timeline = [true, false, false, true, false];
        for (int i = 0; i < timeline.Length; i++)
        {
            if (ArrivalStepRule.ShouldWarn(last, timeline[i]))
            {
                fired.Add(i);
            }
            last = timeline[i];
        }

        Assert.Equal([1, 4], fired);
    }

    [Fact]
    public void TheAlarmText_CarriesTheVerdict_SoThePopUpIsNeverAShrug()
    {
        ArrivalStepRule.ArrivalCheck c = Dock(distance: 3.6e9, relSpeed: 12_400);
        string alarm = ArrivalStepRule.BrokenPlanAlarm(c);
        Assert.Contains("NO LONGER ENDS SAFELY", alarm, StringComparison.Ordinal);
        Assert.Contains(ArrivalStepRule.Verdict(c), alarm, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRefusalWhy_IsTheRowsOwnSentence_SoTheyCannotDisagree()
    {
        // #957: the autopilot's decline reuses the arrive row's formatter verbatim. If these ever drift
        // apart, the panel and the refusal are describing two different ships.
        ArrivalStepRule.ArrivalCheck c = Dock(distance: 3.6e9, relSpeed: 12_400);
        Assert.Equal(ArrivalStepRule.Verdict(c), ArrivalStepRule.RefusalWhy(c));

        // …and the optional trailing clause is appended, never swallowed: the refusal may name the
        // course's own closest pass so the captain has a moment to scrub to.
        const string note = "; this course's own closest pass is 3.60 M km at day 12";
        Assert.Equal(ArrivalStepRule.Verdict(c) + note, ArrivalStepRule.RefusalWhy(c, note));
    }

    // ===== #952 · a pass the ribbon never reached is not a pass =====

    /// <summary>
    /// THE EDGE OF THE PICTURE IS NOT AN ENCOUNTER. <c>ClosestApproach.Passes</c> answers for every body in
    /// the system whether or not the plotted course goes near it, so for a body the ribbon stops short of it
    /// returns the LAST SAMPLE. Judged, that artefact prints a confident ✗ with numbers over a plan that in
    /// truth arrives inside both gates — and sends the captain iterating the wrong way for ever. So the pass
    /// is recognised for what it is, using the projection's own spacing at that end as the tolerance rather
    /// than a number invented here.
    /// </summary>
    [Fact]
    public void APassAtTheRibbonsLastSample_IsTheEndOfThePicture_NotAnEncounter()
    {
        const double ribbonEnd = 90.1 * 86400;
        const double step = 3 * 3600;   // the projection's own maxTimeStep out in the deep

        // Pinned to the end — this is the artefact.
        Assert.True(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(ribbonEnd, ribbonEnd, step));

        // …and so is anything inside the final sample, which is the safe direction to be wrong in.
        Assert.True(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(ribbonEnd - step, ribbonEnd, step));
        Assert.True(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(ribbonEnd + 1, ribbonEnd, step));

        // A genuine encounter comfortably inside the line is a real pass and must be judged normally —
        // without this half the rule would refuse to judge anything at all.
        Assert.False(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(ribbonEnd - (2 * step), ribbonEnd, step));
        Assert.False(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(24.5 * 86400, ribbonEnd, step));

        // The tolerance is the caller's sample spacing, so a coarser ribbon shields a wider band — and a
        // negative gap (a caller handing the spacing back to front) can never invert the test.
        Assert.True(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(ribbonEnd - (2 * step), ribbonEnd, 4 * step));
        Assert.True(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(ribbonEnd - step, ribbonEnd, -step));
    }

    /// <summary>
    /// AND IT SAYS WHICH CONTROL ENDS THE WAIT. The owner likes the iterate buttons (#952: <i>"I really like
    /// those iterate buttons"</i>); the sentence therefore names the one he has his hand on rather than
    /// leaving him to guess why his arrival will not judge itself.
    /// </summary>
    [Fact]
    public void TheRibbonTooShortLine_NamesTheBody_TheLength_AndTheControl()
    {
        string line = ArrivalStepRule.RibbonTooShort("Mars", "90 d");
        Assert.Contains("not judged", line, StringComparison.Ordinal);
        Assert.Contains("Mars", line, StringComparison.Ordinal);
        Assert.Contains("90 d", line, StringComparison.Ordinal);
        Assert.Contains("Path length", line, StringComparison.Ordinal);
        Assert.Contains("auto", line, StringComparison.Ordinal);

        // It is NOT a verdict and must never read like one — no ✗, no threshold, no shortfall.
        Assert.DoesNotContain("✗", line, StringComparison.Ordinal);
        Assert.DoesNotContain("too far", line, StringComparison.Ordinal);
        Assert.DoesNotContain("need ≤", line, StringComparison.Ordinal);
    }

    // ===== The words themselves =====

    [Fact]
    public void TheVerbs_NameTheTwoWaysAPlanCanEnd()
    {
        Assert.Equal("orbit", ArrivalStepRule.Verb(ArrivalStepRule.ArrivalKind.Orbit));
        Assert.Equal("dock", ArrivalStepRule.Verb(ArrivalStepRule.ArrivalKind.Dock));
    }

    [Fact]
    public void Distances_ClimbThePanelsOwnLadder()
    {
        Assert.Equal("64951 km", ArrivalStepRule.FormatDistance(6.4951e7));
        Assert.Equal("3.60 M km", ArrivalStepRule.FormatDistance(3.6e9));
        Assert.Equal("0.50 AU", ArrivalStepRule.FormatDistance(0.5 * 1.495978707e11));
        Assert.Equal("12.4 km/s", ArrivalStepRule.FormatSpeed(12_400));
    }

    [Fact]
    public void EveryNumberIsInvariantCulture()
    {
        // InvariantGlobalization is on for the build, but the formatter says so explicitly rather than
        // relying on it — a decimal comma in a nav readout is a bug report waiting to happen.
        Assert.Contains(".", ArrivalStepRule.FormatSpeed(12_400), StringComparison.Ordinal);
        Assert.Equal(
            (3.6).ToString("F2", CultureInfo.InvariantCulture) + " M km",
            ArrivalStepRule.FormatDistance(3.6e9));
    }

    // ===== #969 — THE ARRIVAL ARMED *THEN*, NOT ONLY *NOW* =====
    //
    // Owner ruling 2026-08-23: "Say three burns and one autopilot to finish the trip to Mars. After that
    // no, absolutely no steps needed if the ship is not interfered with." Two predicates carry that: which
    // arm to make, and how long the armed autopilot must keep its hands off. They are ONE law on purpose —
    // an arm that thought the arrival was months away while the flight thought it was now would fire
    // approach burns straight through the captain's plan.

    [Fact]
    public void AN_ARRIVAL_IS_A_THEN_WhenThePassIsAheadAndTheShipIsNotYetNear()
    {
        const double near = 5.42e9;  // five Mars Hill radii — the floor-free capture range

        // Three burns and nine months out: the owner's case.
        Assert.True(ArrivalStepRule.ArrivalIsAThen(passSimTime: 292 * Day, simTime: 0, distanceNow: 1.6e11, nearRange: near));

        // Already at the door — the historic NOW arm, unchanged.
        Assert.False(ArrivalStepRule.ArrivalIsAThen(292 * Day, 0, distanceNow: 1e9, nearRange: near));

        // The pass is behind us: there is no "then" left to promise.
        Assert.False(ArrivalStepRule.ArrivalIsAThen(passSimTime: 10, simTime: 20, distanceNow: 1.6e11, nearRange: near));
    }

    [Fact]
    public void THE_HOLD_LiftsAtThePassOrAtTheDoor_WhicheverComesFirst()
    {
        const double near = 5.42e9;

        // Mid-cruise: armed, and correctly doing nothing.
        Assert.True(ArrivalStepRule.ArrivalPromiseIsStillAhead(292 * Day, simTime: 100 * Day, distanceNow: 9e10, nearRange: near));

        // The epoch arrives — the loop takes the ship.
        Assert.False(ArrivalStepRule.ArrivalPromiseIsStillAhead(292 * Day, simTime: 292 * Day, distanceNow: 9e10, nearRange: near));

        // …or she runs early and is honestly near the body first. The projection is only a projection, so
        // the geometry gets the final word (this is the half that stops a hold outliving the encounter).
        Assert.False(ArrivalStepRule.ArrivalPromiseIsStillAhead(292 * Day, simTime: 280 * Day, distanceNow: 1e9, nearRange: near));

        // A NOW arm pins no epoch, so it is NEVER held: the tick after arming, the autopilot has the ship.
        Assert.False(ArrivalStepRule.ArrivalPromiseIsStillAhead(null, simTime: 0, distanceNow: 1.6e11, nearRange: near));
    }

    [Fact]
    public void THE_FINISHED_PLAN_ReadsTheOwnersOwnSentenceBack()
    {
        string line = ArrivalStepRule.PlanIsComplete(
            burnsAhead: 3, ArrivalStepRule.ArrivalKind.Orbit, "Mars", "9 mo", chargedPulses: 3);

        Assert.Contains("3 burns", line, StringComparison.Ordinal);
        Assert.Contains("orbit Mars", line, StringComparison.Ordinal);
        Assert.Contains("9 mo", line, StringComparison.Ordinal);
        Assert.Contains("3 p", line, StringComparison.Ordinal);
        Assert.Contains("nothing more needed", line, StringComparison.Ordinal);

        // One burn is one burn, not "1 burns" — the plural is counted, not assumed.
        Assert.Contains("1 burn ", ArrivalStepRule.PlanIsComplete(1, ArrivalStepRule.ArrivalKind.Dock, "The Rusty Roadstead", "3 d", 6), StringComparison.Ordinal);
    }

    [Fact]
    public void THE_ARMED_THEN_LABEL_SaysWhoFinishesTheTrip()
    {
        Assert.Equal("🛰 arrive Mars — the autopilot inserts",
            ArrivalStepRule.ArmedThenLabel(ArrivalStepRule.ArrivalKind.Orbit, "Mars"));
        Assert.Equal("⚓ arrive at The Rusty Roadstead — the autopilot docks",
            ArrivalStepRule.ArmedThenLabel(ArrivalStepRule.ArrivalKind.Dock, "The Rusty Roadstead"));
    }
}
