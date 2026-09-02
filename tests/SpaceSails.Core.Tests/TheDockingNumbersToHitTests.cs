namespace SpaceSails.Core.Tests;

/// <summary>
/// #200 · <b>THE DOCKING NUMBERS TO HIT.</b> Owner, inbound to Venus Haven: <i>"I want to see CLEARLY how
/// close I am to docking distance and speed limits. Now it toggless me meaningles nearby targets here… Own
/// pop-up of the numbers to hit, just like in piracy-hold."</i>
///
/// <para>The flicker half of that issue is closed elsewhere (#966's nearest-slot hysteresis, #1033's
/// Hill-sphere deferral). What these gates hold is the panel itself: <see cref="DockFocus"/> composes the
/// same criterion box the piracy run already has, and it must be composed <b>out of the clamp's own
/// judgement</b> — the resolved <see cref="DockAffordance"/> and <see cref="DockRule"/>'s constants — so a
/// row can never be green about a limit the arm refuses.</para>
///
/// <para><b>Every claim here is paired against its own opposite</b> (the vacuity discipline): a compliant
/// approach must show every row INSIDE <i>and</i> actually clamp, and the hot approach must show the
/// offending row OUTSIDE <i>and</i> be refused. An empty panel would fail the first half of every pair, so
/// none of these can pass on a panel that draws nothing.</para>
/// </summary>
public class TheDockingNumbersToHitTests
{
    private static CelestialBody Station(string id = "venus-haven", double radius = 5000) =>
        new(id, id, "venus", 0, radius, 1.08e11, 1.94e7, 0, BodyKind.Station, IsHaven: true);

    // A haven at the origin drifting at driftMps along +X; the ship rangeMeters out along +X closing at
    // relMps. Range and rel are then exactly the arguments — the same frame DockAffordanceTests uses.
    private static (ShipState Ship, DockHaven Haven) Frame(
        double rangeMeters, double relMps, double driftMps = 0, bool isFocus = true)
    {
        CelestialBody station = Station();
        var havenPos = new Vector2d(0, 0);
        var havenVel = new Vector2d(driftMps, 0);
        var ship = new ShipState(new Vector2d(rangeMeters, 0), new Vector2d(driftMps + relMps, 0), 0);
        return (ship, new DockHaven(station, havenPos, havenVel, isFocus));
    }

    private static DockAffordance Evaluate(ShipState ship, DockHaven haven, int pulses = 250, bool latched = false) =>
        DockAffordanceRule.Evaluate(ship, new[] { haven }, pulses, latched);

    private static DockGateRow Row(IReadOnlyList<DockGateRow> rows, string label)
    {
        foreach (DockGateRow r in rows)
        {
            if (r.Label == label) { return r; }
        }

        throw new Xunit.Sdk.XunitException(
            $"the focus panel has no '{label}' row — it drew [{string.Join(", ", rows.Select(r => r.Label))}]");
    }

    // ---- THE VACUITY PAIR: a compliant approach, and the hot one beside it ----

    /// <summary>
    /// <b>The compliant approach: every row inside its gate — AND she clamps.</b> Half a pair. Without the
    /// second assertion this would pass on a panel that draws no rows at all, and without the rows it would
    /// be a docking test with nothing to say about #200.
    /// </summary>
    [Fact]
    public void ACompliantApproachShowsEveryRowInsideItsGate_AndTheClampTakes()
    {
        // 340,000 km out, 4.6 km/s rel — the owner's own frame from #212, squarely inside the envelope.
        var (ship, haven) = Frame(340_000_000, 4600);
        DockAffordance a = Evaluate(ship, haven);

        IReadOnlyList<DockGateRow> rows = DockFocus.Rows(a, pulsesAboard: 250);
        Assert.NotEmpty(rows);                       // the panel is not empty — the anti-vacuous half
        Assert.All(rows, r => Assert.True(r.Inside, $"'{r.Label}' reads {r.Reading} against {r.Gate}"));

        // …and the clamp agrees: this is the frame that docks.
        Assert.True(DockRule.InEnvelope(ship, haven.Position, haven.Velocity, haven.Body.BodyRadius));
        Assert.True(a.CanClampNow);
        Assert.Equal(DockPhase.Clamp, a.Phase);
        Assert.Equal("→ " + DockFocus.ClampNowLine, DockFocus.Verdict(a, 250));
    }

    /// <summary>
    /// <b>The hot approach: the offending row is outside, the others are not — AND the clamp refuses.</b>
    /// #761's whole ask, held as a law: when the door says no, the panel says which number said it. In range
    /// but 12 km/s of drift: the range row must stay green (that is not what refused her) and only the drift
    /// row may be red.
    /// </summary>
    [Fact]
    public void AHotApproachShowsTheOffendingRowOutside_AndTheClampRefuses()
    {
        var (ship, haven) = Frame(340_000_000, 12_000);
        DockAffordance a = Evaluate(ship, haven);

        IReadOnlyList<DockGateRow> rows = DockFocus.Rows(a, pulsesAboard: 250);
        Assert.NotEmpty(rows);
        Assert.True(Row(rows, "close enough").Inside);     // the range is NOT what refused her
        Assert.False(Row(rows, "drift matched").Inside);   // this is

        // …and the clamp agrees: this frame does not dock.
        Assert.False(DockRule.InEnvelope(ship, haven.Position, haven.Velocity, haven.Body.BodyRadius));
        Assert.False(a.CanClampNow);
        Assert.Equal("→ " + DockFocus.MatchClampLine, DockFocus.Verdict(a, 250));
    }

    /// <summary>
    /// <b>The refusal that needs a third number (#213/#761).</b> Hot AND unable to pay for the terminal
    /// match: the panel grows the "match burn" row, that row is the one outside its gate, and the verdict
    /// says what the burn costs against what is aboard. Paired with the affordable case beside it, so this
    /// cannot pass on a panel that shows the mass row always-red or never at all.
    /// </summary>
    [Fact]
    public void AnUnaffordableMatchGrowsTheMassRow_AndItIsTheRowThatRefuses()
    {
        var (ship, haven) = Frame(340_000_000, 12_000);

        // Affordable: the mass row is there (a match IS the ask) and it is inside its gate.
        DockAffordance rich = Evaluate(ship, haven, pulses: 250);
        Assert.Equal(DockPhase.MatchClamp, rich.Phase);
        Assert.True(Row(DockFocus.Rows(rich, 250), "match burn").Inside);

        // Broke: the SAME frame, one pulse aboard. The door refuses, and the mass row is the refusal.
        DockAffordance broke = Evaluate(ship, haven, pulses: 1);
        Assert.Equal(DockPhase.TooHot, broke.Phase);
        IReadOnlyList<DockGateRow> rows = DockFocus.Rows(broke, pulsesAboard: 1);
        Assert.True(Row(rows, "close enough").Inside);
        Assert.False(Row(rows, "drift matched").Inside);
        Assert.False(Row(rows, "match burn").Inside);
        Assert.Contains(broke.MatchPulses.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DockFocus.Verdict(broke, 1), StringComparison.Ordinal);
        Assert.Contains("unaffordable", DockFocus.Verdict(broke, 1), StringComparison.Ordinal);

        // The mass row exists ONLY while a match is the ask — the plain clamp has no burn to pay for.
        var (slow, haven2) = Frame(340_000_000, 4600);
        Assert.DoesNotContain(DockFocus.Rows(Evaluate(slow, haven2), 250), r => r.Label == "match burn");
    }

    // ---- ONE SOURCE: the gates are DockRule's, not a second typing ----

    /// <summary>
    /// <b>The row flips exactly where the clamp flips.</b> Driven off <see cref="DockRule.EnvelopeMeters"/>
    /// itself and measured a <i>hair</i> either side of it — not a comfortable half-envelope out, which is
    /// the trap: a panel that had quietly grown a looser gate of its own (600,000 km, say) would still get a
    /// generous "outside" case right and this would pass over the drift. A hair past the constant, only the
    /// constant answers.
    /// </summary>
    [Fact]
    public void TheRangeRowFlipsAtDockRulesOwnEnvelope_NotAtANumberOfItsOwn()
    {
        var (at, havenAt) = Frame(DockRule.EnvelopeMeters, 0);
        DockAffordance inside = Evaluate(at, havenAt);
        Assert.True(Row(DockFocus.Rows(inside, 250), "close enough").Inside);
        Assert.True(inside.CanClampNow);   // the arm agrees at the very same metre

        // A hair past the door — still the captain's focus, so the panel is up and coaching the range.
        var (past, havenPast) = Frame(DockRule.EnvelopeMeters * 1.0001, 0);
        DockAffordance outside = Evaluate(past, havenPast);
        Assert.False(Row(DockFocus.Rows(outside, 250), "close enough").Inside);
        Assert.False(outside.CanClampNow);
        Assert.Equal(DockPhase.Approach, outside.Phase);
    }

    /// <summary>The drift row's twin of the same law, on <see cref="DockRule.MatchSpeed"/> — and measured
    /// the same hair past it, for the same reason.</summary>
    [Fact]
    public void TheDriftRowFlipsAtDockRulesOwnMatchSpeed_NotAtANumberOfItsOwn()
    {
        var (at, havenAt) = Frame(100_000_000, DockRule.MatchSpeed);
        DockAffordance inside = Evaluate(at, havenAt);
        Assert.True(Row(DockFocus.Rows(inside, 250), "drift matched").Inside);
        Assert.True(inside.CanClampNow);

        var (over, havenOver) = Frame(100_000_000, DockRule.MatchSpeed * 1.0001);
        DockAffordance outside = Evaluate(over, havenOver);
        Assert.False(Row(DockFocus.Rows(outside, 250), "drift matched").Inside);
        Assert.False(outside.CanClampNow);
    }

    /// <summary>The gates are QUOTED from the constants too, not just compared against them — the owner
    /// reads "≤ 500,000 km" and "≤ 8 km/s" off the panel, and both are computed from
    /// <see cref="DockRule"/>. A re-typed limit in the page would leave these strings behind.</summary>
    [Fact]
    public void TheGatesAreQuotedFromTheConstantsTheClampEnforces()
    {
        var (ship, haven) = Frame(340_000_000, 4600);
        IReadOnlyList<DockGateRow> rows = DockFocus.Rows(Evaluate(ship, haven), 250);

        string envelopeKm = (DockRule.EnvelopeMeters / 1000).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        string matchKmS = (DockRule.MatchSpeed / 1000).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains(envelopeKm, Row(rows, "close enough").Gate, StringComparison.Ordinal);
        Assert.Contains(matchKmS, Row(rows, "drift matched").Gate, StringComparison.Ordinal);
        Assert.Contains(envelopeKm, DockFocus.CoastCloserLine(), StringComparison.Ordinal);
    }

    // ---- THE OTHER HALF OF #200: it does not toggle to a meaningless neighbour ----

    /// <summary>
    /// <b>A haven merely drifting past raises nothing.</b> The owner's complaint was that the readout
    /// "toggless me meaningles nearby targets"; the panel's gate is the affordance's phase, so a haven that
    /// is neither the captain's focus nor within the clamp's reach is <see cref="DockPhase.None"/> and the
    /// panel is silent. Paired against the focused case, so "silent" cannot mean "silent always".
    /// </summary>
    [Fact]
    public void AHavenDriftingPastRaisesNothing_ButTheChosenBerthDoes()
    {
        // Far out, and NOT the destination/armed haven: nothing to say.
        var (ship, stranger) = Frame(DockRule.EnvelopeMeters * 40, 3000, isFocus: false);
        DockAffordance passing = Evaluate(ship, stranger);
        Assert.Equal(DockPhase.None, passing.Phase);
        Assert.False(DockFocus.IsLive(passing));
        Assert.Empty(DockFocus.Rows(passing, 250));
        Assert.Equal(string.Empty, DockFocus.Verdict(passing, 250));

        // The same geometry, but this berth is the one the captain chose: the panel is up and coaching.
        var (same, chosen) = Frame(DockRule.EnvelopeMeters * 40, 3000, isFocus: true);
        DockAffordance armed = Evaluate(same, chosen);
        Assert.True(DockFocus.IsLive(armed));
        Assert.NotEmpty(DockFocus.Rows(armed, 250));
        Assert.Equal("venus-haven", armed.HavenId);
    }

    /// <summary>The panel names the berth the ⚓ button names — #212's one truth, carried through. A nearer
    /// stranger must not steal the heading while the captain's own berth is armed.</summary>
    [Fact]
    public void ThePanelNamesTheBerthTheClampButtonNames()
    {
        var chosen = new DockHaven(Station("venus-haven"), new Vector2d(0, 0), new Vector2d(0, 0), IsFocus: true);
        var nearer = new DockHaven(Station("depot"), new Vector2d(2000, 0), new Vector2d(0, 0), IsFocus: false);
        var ship = new ShipState(new Vector2d(400_000_000, 0), new Vector2d(0, 0), 0);

        DockAffordance a = DockAffordanceRule.Evaluate(ship, new[] { nearer, chosen }, 250, false);
        Assert.Equal("venus-haven", a.HavenId);
        Assert.True(DockFocus.IsLive(a));
        Assert.NotEmpty(DockFocus.Rows(a, 250));
    }
}
