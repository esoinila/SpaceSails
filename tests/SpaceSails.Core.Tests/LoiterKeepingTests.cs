namespace SpaceSails.Core.Tests;

/// <summary>#488 · The loiter — the autopilot mode that keeps the ship at shuttle range while the away
/// team is inside a wreck. Lab 40 wrote this design: a co-orbital hold is FREE, so the kernel guards the
/// hand-off match rather than the tank. These pin that.</summary>
public class LoiterKeepingTests
{
    private const double JupiterMu = 1.26686534e17;
    private const double Rj = 6.9911e7;

    // ── The hand-off gate (the real precondition) ─────────────────────────────────────────────────────

    [Fact]
    public void SittingStillWithHerIsAMatch()
    {
        Assert.Equal(LoiterKeeping.MatchQuality.Matched, LoiterKeeping.Classify(0));
        Assert.Equal(LoiterKeeping.MatchQuality.Matched,
            LoiterKeeping.Classify(LoiterKeeping.MatchedRelativeSpeedMps));
    }

    [Fact]
    public void ASloppyHandOffIsLooseButStillAllowed()
    {
        // Lab 40: even a 100 m/s launch error still bought about a week everywhere. That is a warning,
        // not a refusal — the captain is allowed to be in a hurry.
        LoiterKeeping.MatchQuality q = LoiterKeeping.Classify(LoiterKeeping.LooseRelativeSpeedMps);

        Assert.Equal(LoiterKeeping.MatchQuality.Loose, q);
        Assert.True(LoiterKeeping.CanHold(q));
    }

    [Fact]
    public void PassingByIsRefused_BecauseThatIsHowYouStrandThem()
    {
        LoiterKeeping.MatchQuality q = LoiterKeeping.Classify(LoiterKeeping.LooseRelativeSpeedMps + 1);

        Assert.Equal(LoiterKeeping.MatchQuality.PassingBy, q);
        Assert.False(LoiterKeeping.CanHold(q));
    }

    [Fact]
    public void ClassificationIgnoresTheSignOfTheDrift()
    {
        Assert.Equal(LoiterKeeping.Classify(250), LoiterKeeping.Classify(-250));
    }

    [Fact]
    public void MatchingHerCostsExactlyTheRelativeVelocity()
    {
        // A hold IS matching her — so the Δv to start holding is the relative velocity, no more.
        var ship = new Vector2d(1000, 0);
        var wreck = new Vector2d(1300, 400);   // 500 m/s away

        int pulses = LoiterKeeping.PulsesToMatch(ship, wreck, shipSpeedMps: 30_000);

        Assert.Equal(OrbitRule.PulsesFor(500, 30_000), pulses);
        Assert.True(pulses > 0);
    }

    // ── The co-orbital hold (the whole point) ─────────────────────────────────────────────────────────

    [Fact]
    public void TheHoldStateSharesHerOrbit_DisplacedAlongTheTrack()
    {
        // Same speed and same distance from whatever she orbits ⇒ same period ⇒ they never phase apart.
        // Displacing RADIALLY is the expensive mistake this kernel exists to avoid.
        var wreck = new ShipState(new Vector2d(1.0e9, 0), new Vector2d(0, 20_000), 0);

        ShipState hold = LoiterKeeping.HoldStateFor(wreck, offsetMeters: 2.0e8, simTime: 0);

        Assert.Equal(wreck.Velocity, hold.Velocity);                       // matched, exactly
        Vector2d offset = hold.Position - wreck.Position;
        Assert.Equal(2.0e8, offset.Length, 1);                             // the asked-for separation
        // …and it lies ALONG her track, not across it: zero component perpendicular to her velocity.
        Vector2d along = wreck.Velocity.Normalized();
        double across = (offset - (along * offset.Length)).Length;
        Assert.True(across < 1.0, "the displacement must be along-track, or it is a different orbit");
    }

    [Fact]
    public void AStationaryWreckStillYieldsAUsableHold()
    {
        // Degenerate but reachable: a wreck with no velocity in the working frame must not divide by zero.
        var wreck = new ShipState(new Vector2d(0, 0), Vector2d.Zero, 0);

        ShipState hold = LoiterKeeping.HoldStateFor(wreck, 1.0e8, 0);

        Assert.Equal(1.0e8, (hold.Position - wreck.Position).Length, 1);
    }

    [Fact]
    public void TheLeashIsHalfAShuttleHop_AndTheDeadBandOnlyTripsOutsideIt()
    {
        Assert.Equal(0.5 * ShuttleRange.RangeMeters, LoiterKeeping.HoldBandMeters, 1);

        Assert.False(LoiterKeeping.NeedsTrim(LoiterKeeping.HoldBandMeters - 1));
        Assert.False(LoiterKeeping.NeedsTrim(LoiterKeeping.HoldBandMeters));
        Assert.True(LoiterKeeping.NeedsTrim(LoiterKeeping.HoldBandMeters + 1));
    }

    [Fact]
    public void TheLeashSitsWellInsideTheShuttlesOwnReach()
    {
        // The away team's ride must never be one drift away from unreachable.
        Assert.True(LoiterKeeping.HoldBandMeters < ShuttleRange.RangeMeters);
    }

    // ── Where waiting is harder ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeepSpaceHasNothingToPhaseAgainst()
    {
        Assert.Equal(0.0, LoiterKeeping.ErrorGrowthRate(parentMu: 0, orbitRadiusMeters: 0));
        Assert.False(LoiterKeeping.IsDemanding(0, 0));
    }

    [Fact]
    public void CloseOverAGiantIsTheDemandingCase_AndFarOutIsNot()
    {
        // Lab 40's one failing regime was 3 Rj. Far out around the same giant is forgiving.
        Assert.True(LoiterKeeping.IsDemanding(JupiterMu, 3 * Rj));
        Assert.False(LoiterKeeping.IsDemanding(JupiterMu, 400 * Rj));
    }

    [Fact]
    public void ErrorGrowthFallsAsYouBackAway()
    {
        double close = LoiterKeeping.ErrorGrowthRate(JupiterMu, 3 * Rj);
        double far = LoiterKeeping.ErrorGrowthRate(JupiterMu, 30 * Rj);

        Assert.True(close > far, "a tighter orbit phases errors apart faster");
    }

    // ── The quote ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheOrdinaryQuoteReadsFree_BecauseItIs()
    {
        // Lab 40 measured ZERO pulses/day for a co-orbital hold in every regime. The promise is better
        // than the orbit keeper's "trim ≈27 p/day" (#193), and it is earned.
        LoiterKeeping.Quote q = LoiterKeeping.Assess(relativeSpeedMps: 0, parentMu: 0, orbitRadiusMeters: 0);

        Assert.True(q.CanHold);
        Assert.Contains("free", q.Line, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("will be here when you get back", q.Line);
        Assert.DoesNotContain("p/day", q.Line);
    }

    [Fact]
    public void ADemandingSiteIsCalledOut_ButStillHeld()
    {
        LoiterKeeping.Quote q = LoiterKeeping.Assess(0, JupiterMu, 3 * Rj);

        Assert.True(q.CanHold);
        Assert.True(q.Demanding);
        Assert.Contains("long chase", q.Line);
    }

    [Fact]
    public void RefusingSaysWhy_AndWhatToDoAboutIt()
    {
        LoiterKeeping.Quote q = LoiterKeeping.Assess(5_000, 0, 0);

        Assert.False(q.CanHold);
        Assert.Equal(LoiterKeeping.MatchQuality.PassingBy, q.Match);
        Assert.Contains("Match her first", q.Line);
    }

    [Fact]
    public void ALooseHoldAdmitsItIsLoose()
    {
        LoiterKeeping.Quote q = LoiterKeeping.Assess(80, 0, 0);

        Assert.True(q.CanHold);
        Assert.Equal(LoiterKeeping.MatchQuality.Loose, q.Match);
        Assert.Contains("days, not weeks", q.Line);
    }
}
