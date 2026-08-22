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
}
