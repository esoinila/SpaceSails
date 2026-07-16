namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for the ONE-truth dock affordance (#212 selection, #211 hysteresis latch, #213 match &amp;
/// clamp quote, #204 honest auto-dock). The toolbar ⚓ button and the envelope coaching line both read
/// <see cref="DockAffordanceRule.Evaluate"/>, so they can never disagree — the exact failure the owner
/// caught on final at The Rusty Roadstead, where a planet momentarily nearer than the station hid the
/// button while the line still said "hit ⚓ Dock".
/// </summary>
public class DockAffordanceTests
{
    private static CelestialBody Station(string id = "roadstead", double radius = 5000) =>
        new(id, id, "mars", 0, radius, 2.28e11, 5.9e7, 0, BodyKind.Station, IsHaven: true);

    // A haven at the origin drifting at driftMps along +X. The ship sits rangeMeters out along +X with a
    // relative speed relMps along +X — so range and rel are exactly the arguments, while the ship's
    // heliocentric speed (what the pulse kernel prices against) is drift + rel.
    private static (ShipState Ship, DockHaven Haven) Frame(
        double rangeMeters, double relMps, double driftMps = 0, bool isFocus = true, CelestialBody? body = null)
    {
        CelestialBody station = body ?? Station();
        var havenPos = new Vector2d(0, 0);
        var havenVel = new Vector2d(driftMps, 0);
        var ship = new ShipState(new Vector2d(rangeMeters, 0), new Vector2d(driftMps + relMps, 0), 0);
        return (ship, new DockHaven(station, havenPos, havenVel, isFocus));
    }

    // ---- #212: one truth — the button binds to the dockable haven, never the raw nearest body ----

    [Fact]
    public void InEnvelope_TheClampButtonExists_TheFrameThatHadNoButton()
    {
        // The owner's frame: 339,597 km out, rel 4.6 km/s — squarely inside the DockRule envelope.
        var (ship, haven) = Frame(339_597_000, 4600);
        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { haven }, availablePulses: 250, wasLatched: false);

        Assert.Equal(DockPhase.Clamp, a.Phase);
        Assert.True(a.ShowButton);      // the button MUST exist — the line said "hit ⚓ Dock"
        Assert.True(a.CanClampNow);
        Assert.Equal("roadstead", a.HavenId);
    }

    [Fact]
    public void Selection_IsFocusFirst_SoTheButtonReadsTheHavenTheLineReads()
    {
        // A NEARER dockable haven that is NOT the captain's focus must not steal the selection: the
        // envelope line reads the destination/armed haven first, and the button must read the same one.
        var focus = new DockHaven(Station("roadstead"), new Vector2d(0, 0), new Vector2d(0, 0), IsFocus: true);
        var nearer = new DockHaven(Station("depot"), new Vector2d(2000, 0), new Vector2d(0, 0), IsFocus: false);
        var ship = new ShipState(new Vector2d(400_000_000, 0), new Vector2d(0, 0), 0);

        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { nearer, focus }, 250, false);
        Assert.Equal("roadstead", a.HavenId); // focus wins over the nearer depot
    }

    [Fact]
    public void Selection_FallsBackToNearestDockable_WhenNothingIsFocused()
    {
        var far = new DockHaven(Station("far"), new Vector2d(0, 0), new Vector2d(0, 0), IsFocus: false);
        var near = new DockHaven(Station("near"), new Vector2d(9e8, 0), new Vector2d(0, 0), IsFocus: false);
        var ship = new ShipState(new Vector2d(1e9, 0), new Vector2d(0, 0), 0);

        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { far, near }, 250, false);
        Assert.Equal("near", a.HavenId); // |1e9 - 9e8| = 1e8 beats |1e9 - 0| = 1e9
    }

    [Fact]
    public void NoHavens_IsHidden()
    {
        var ship = new ShipState(new Vector2d(0, 0), new Vector2d(0, 0), 0);
        DockAffordance a = DockAffordanceRule.Evaluate(ship, System.Array.Empty<DockHaven>(), 250, false);
        Assert.Equal(DockPhase.None, a.Phase);
        Assert.False(a.ShowButton);
    }

    // ---- #211: hysteresis latch — no per-tick flicker as the orbiting station phases the rel speed ----

    [Fact]
    public void Latch_EntersAtMatchSpeed_HoldsAcrossTheWobble_ReleasesOnlyClearlyOut()
    {
        var body = Station();

        // Enter: rel just under the 8 km/s match speed inside the door → latched, plain clamp.
        var (s1, h1) = Frame(300_000_000, 7000);
        DockAffordance entered = DockAffordanceRule.Evaluate(s1, new[] { h1 }, 250, wasLatched: false);
        Assert.True(entered.Latched);
        Assert.Equal(DockPhase.Clamp, entered.Phase);

        // Wobble: the drift phases up to 9 km/s (over the 8 cap, under the 10 release). Latched holds —
        // the affordance stays (as the match offer), it does NOT blink out to None.
        var (s2, h2) = Frame(300_000_000, 9000);
        DockAffordance held = DockAffordanceRule.Evaluate(s2, new[] { h2 }, 250, wasLatched: true);
        Assert.True(held.Latched);
        Assert.True(held.ShowButton);
        Assert.Equal(DockPhase.MatchClamp, held.Phase);

        // Clearly out: rel past the 10 km/s release speed → latch drops.
        var (s3, h3) = Frame(300_000_000, 11000);
        DockAffordance released = DockAffordanceRule.Evaluate(s3, new[] { h3 }, 250, wasLatched: true);
        Assert.False(released.Latched);
    }

    [Fact]
    public void Latch_Releases_WhenRangeCoastsWellBeyondTheDoor()
    {
        // Latched, then the ship coasts past the envelope by more than the release margin → drops.
        double outside = DockRule.EnvelopeMeters * (DockAffordanceRule.ReleaseRangeFactor + 0.05);
        var (ship, haven) = Frame(outside, 3000);
        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { haven }, 250, wasLatched: true);
        Assert.False(a.Latched);
    }

    // ---- #213: match & clamp — quote priced with the SAME kernel the autopilot flies ----

    [Fact]
    public void MatchClamp_OfferedInRange_WhenHot_AndPricedWithTheApproachKernel()
    {
        // In the envelope RANGE but rel above the match speed, with plenty of drift so the heliocentric
        // speed (what the kernel prices against) is realistic.
        var (ship, haven) = Frame(300_000_000, 10_500, driftMps: 18_000);
        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { haven }, availablePulses: 250, wasLatched: false);

        Assert.Equal(DockPhase.MatchClamp, a.Phase);
        Assert.True(a.NeedsMatch);

        // The quote is EXACTLY the autopilot's terminal-match burn cost — same kernel, no parallel model.
        int kernel = OrbitRule.ApproachPulseCost(ship, haven.Position, haven.Velocity, haven.Body, null, 0);
        Assert.Equal(kernel, a.MatchPulses);
        Assert.True(a.MatchPulses > 0);
    }

    [Fact]
    public void TooHot_WhenTheMatchIsUnaffordable_RefusesWithoutClamping()
    {
        var (ship, haven) = Frame(300_000_000, 10_500, driftMps: 18_000);
        int quote = OrbitRule.ApproachPulseCost(ship, haven.Position, haven.Velocity, haven.Body, null, 0);

        // One pulse short of the quote → the door refuses with the numbers.
        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { haven }, availablePulses: quote - 1, wasLatched: false);
        Assert.Equal(DockPhase.TooHot, a.Phase);
        Assert.False(a.CanClampNow);
        Assert.False(a.NeedsMatch);
        Assert.True(a.ShowButton); // still shown — it teaches the refusal, not silence

        // Exactly the quote aboard → the offer stands.
        DockAffordance ok = DockAffordanceRule.Evaluate(ship, new[] { haven }, availablePulses: quote, wasLatched: false);
        Assert.Equal(DockPhase.MatchClamp, ok.Phase);
    }

    [Fact]
    public void MatchQuote_IsThePulseKernelArithmetic()
    {
        // The kernel: one pulse buys 1% of heliocentric speed as Δv (floor 1 m/s), rounded up, ≥ 1.
        Assert.Equal(1, OrbitRule.PulsesFor(deltaV: 5, currentSpeed: 1000));         // 5 / max(1,10) = 0.5 → 1
        Assert.Equal(57, OrbitRule.PulsesFor(deltaV: 10_500, currentSpeed: 18_500)); // 10500 / 185 = 56.76 → 57
    }

    [Fact]
    public void OutOfRange_FocusedHaven_CoachesApproach_NoButtonYet()
    {
        var (ship, haven) = Frame(9e8, 3000); // beyond the 5e8 envelope
        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { haven }, 250, false);
        Assert.Equal(DockPhase.Approach, a.Phase);
        Assert.False(a.ShowButton);
    }

    // ---- #204/#186: auto-dock only when the errand is honest ----

    [Fact]
    public void ShouldAutoDock_OnlyWhenDockHaven_AndNotHostileFlagged()
    {
        Assert.True(DockAffordanceRule.ShouldAutoDock(armedTargetIsDockHaven: true, hostileFlagged: false));
        Assert.False(DockAffordanceRule.ShouldAutoDock(armedTargetIsDockHaven: true, hostileFlagged: true));   // felony keeps the confirm
        Assert.False(DockAffordanceRule.ShouldAutoDock(armedTargetIsDockHaven: false, hostileFlagged: false)); // a moon parks, not docks
        Assert.False(DockAffordanceRule.ShouldAutoDock(armedTargetIsDockHaven: false, hostileFlagged: true));
    }
}
