using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #243 · <b>ONE INSTRUMENT, AND IT READS THE GATE'S OWN NUMBERS.</b>
///
/// <para>Owner's distillation: <i>"trying to be dockable, or capture etc — we always have the duo of
/// relative speed and distance, and some criteria for those."</i> So the conditions strip is one gauge with
/// swappable limits, and the single way it can be WRONG is by quoting a limit that is not the one the sim
/// enforces — the "one law, two typings" bug class, in the one surface whose entire job is to tell the
/// captain what the machinery will do.</para>
///
/// <para><b>Every claim below is made by CALLING the enforcing code, never by typing its constant.</b> The
/// gate table's numbers are DISCOVERED here — each criterion's limit is found by bisecting the boundary of
/// the strip's own chip and then compared against the boundary of the predicate the flight runs on
/// (<see cref="CaptureRule.IsInWindow"/>, <see cref="DockRule.InEnvelope"/>,
/// <see cref="ArrivalStepRule.Check"/>, <see cref="ShuttleRange.InRange"/>). A test that typed "5e8" would
/// pass on a strip that had drifted with the constant it was copied from; this one cannot.</para>
///
/// <para><b>Proven able to fail</b> — see the red-proof table in the pull request: each guard was watched
/// red against a deliberately broken strip (a limit re-typed, a trend sign flipped, a chip's predicate
/// swapped for a hand-written comparison) before it was allowed to stand.</para>
/// </summary>
public class TheGateIsAlwaysTheSameDuoTests
{
    // ── The worlds the gates are asked about ─────────────────────────────────────────────────────────

    private static CelestialBody Station(double radius = 5000) =>
        new("venus-haven", "Venus Haven", "venus", 0, radius, 1.08e11, 1.94e7, 0, BodyKind.Station, IsHaven: true);

    private static CelestialBody Moon() =>
        new("europa", "Europa", "jupiter", 3.2e12, 1.56e6, 6.71e8, 3.07e5, 0, BodyKind.Moon);

    /// <summary>Two hulls on one line: the target at rest at the origin, the ship <paramref name="range"/>
    /// out along +X closing at <paramref name="rel"/>. Range and relative speed are then exactly the two
    /// arguments, and the sign of the closing speed is exactly the sign of <paramref name="rel"/>.</summary>
    private static (ShipState Player, ShipState Target) Pair(double range, double rel) =>
        (new ShipState(new Vector2d(range, 0), new Vector2d(-rel, 0), 0),
         new ShipState(Vector2d.Zero, Vector2d.Zero, 0));

    private static (ShipState Ship, DockHaven Haven) DockFrame(double range, double rel)
    {
        var ship = new ShipState(new Vector2d(range, 0), new Vector2d(-rel, 0), 0);
        return (ship, new DockHaven(Station(), Vector2d.Zero, Vector2d.Zero, IsFocus: true));
    }

    private static DockAffordance Affordance(double range, double rel, int pulses = 100_000)
    {
        (ShipState ship, DockHaven haven) = DockFrame(range, rel);
        return DockAffordanceRule.Evaluate(ship, new[] { haven }, pulses, wasLatched: false);
    }

    /// <summary>The largest x in [lo, hi] for which <paramref name="holds"/> is true, to a part in 2^60 —
    /// i.e. the boundary of a monotone predicate, FOUND rather than assumed. This is the whole mechanism of
    /// the gate table: ask the code where its edge is instead of writing the edge down twice.</summary>
    private static double EdgeOf(Func<double, bool> holds, double lo, double hi)
    {
        Assert.True(holds(lo), "the bisection's lower end is already outside the gate — nothing to find.");
        Assert.False(holds(hi), "the bisection's upper end is still inside the gate — the edge is not bracketed.");
        for (int i = 0; i < 200; i++)
        {
            double mid = (lo + hi) / 2;
            if (mid <= lo || mid >= hi)
            {
                break;
            }
            if (holds(mid))
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private static GateCriterion Chip(ConditionsReading reading, string label) =>
        reading.Criteria.FirstOrDefault(c => c.Label == label) is { Label.Length: > 0 } found
            ? found
            : throw new InvalidOperationException(
                $"#243 · the {reading.Kind} gate has no '{label}' chip — it draws "
                + string.Join(", ", reading.Criteria.Select(c => $"'{c.Label}'")));

    // ── THE GATE TABLE, discovered ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE BOARDING WINDOW.</b> The strip's two chips flip at exactly the two edges
    /// <see cref="CaptureRule.IsInWindow"/> flips at — the predicate the shuttles actually launch on. Both
    /// edges are bisected out of the two codes independently and compared; neither number is typed.
    /// </summary>
    [Fact]
    public void TheBoardingWindowsTwoEdgesAreTheEdgesTheShuttlesLaunchOn()
    {
        const double slow = 100;      // well inside the speed gate, so the range edge is the only one moving
        double stripRange = EdgeOf(
            d => Chip(ConditionsGate.Board("Kestrel", d, slow, slow), "close enough").Inside, 1e3, 1e12);
        double simRange = EdgeOf(
            d => { (ShipState p, ShipState t) = Pair(d, slow); return CaptureRule.IsInWindow(p, t); }, 1e3, 1e12);
        Assert.Equal(simRange, stripRange, 3);

        const double near = 1e6;      // well inside the range gate, so the speed edge is the only one moving
        double stripSpeed = EdgeOf(
            v => Chip(ConditionsGate.Board("Kestrel", near, v, v), "speed match").Inside, 0, 1e6);
        double simSpeed = EdgeOf(
            v => { (ShipState p, ShipState t) = Pair(near, v); return CaptureRule.IsInWindow(p, t); }, 0, 1e6);
        Assert.Equal(simSpeed, stripSpeed, 3);

        // …and the strip's verdict is the sim's verdict across the whole quadrant, not just at the corners.
        foreach (double d in new[] { 1e6, 4.9e8, 5e8, 5.1e8, 3e9 })
        {
            foreach (double v in new[] { 0.0, 4_999, 5_000, 5_001, 20_000 })
            {
                (ShipState p, ShipState t) = Pair(d, v);
                Assert.Equal(CaptureRule.IsInWindow(p, t), ConditionsGate.Board("Kestrel", d, v, v).AllInside);
            }
        }
    }

    /// <summary>
    /// <b>THE DOCK ENVELOPE.</b> The clamp's chips are #200's own rows — the strip borrows
    /// <see cref="DockFocus.Rows"/> rather than composing a second set — so the pair of them agrees with
    /// <see cref="DockRule.InEnvelope"/>, the test the arm itself makes, at and either side of both edges.
    /// </summary>
    [Fact]
    public void TheClampsTwoChipsAreTheArmsOwnTest()
    {
        CelestialBody station = Station();
        foreach (double d in new[] { 1e6, 4.9e8, 5e8, 5.1e8, 3e9 })
        {
            foreach (double v in new[] { 0.0, 7_999, 8_000, 8_001, 30_000 })
            {
                (ShipState ship, DockHaven haven) = DockFrame(d, v);
                bool arm = DockRule.InEnvelope(ship, haven.Position, haven.Velocity, station.BodyRadius);
                ConditionsReading strip = ConditionsGate.Dock(Affordance(d, v), 100_000, v);

                bool bothGates = Chip(strip, "close enough").Inside && Chip(strip, "drift matched").Inside;
                Assert.Equal(arm, bothGates);
            }
        }

        // The edges, bisected out of each side and compared — the gate table's two numbers, discovered.
        double stripRange = EdgeOf(d => Chip(ConditionsGate.Dock(Affordance(d, 100), 100_000, 100), "close enough").Inside, 1e4, 1e12);
        double armRange = EdgeOf(
            d => { (ShipState s, DockHaven h) = DockFrame(d, 100); return DockRule.InEnvelope(s, h.Position, h.Velocity, station.BodyRadius); },
            1e4, 1e12);
        Assert.Equal(armRange, stripRange, 3);

        double stripSpeed = EdgeOf(v => Chip(ConditionsGate.Dock(Affordance(1e6, v), 100_000, v), "drift matched").Inside, 0, 1e6);
        double armSpeed = EdgeOf(
            v => { (ShipState s, DockHaven h) = DockFrame(1e6, v); return DockRule.InEnvelope(s, h.Position, h.Velocity, station.BodyRadius); },
            0, 1e6);
        Assert.Equal(armSpeed, stripSpeed, 3);
    }

    /// <summary>
    /// <b>THE CAPTURE GATE OF AN ARMED ARRIVAL.</b> Same two numbers the #955 ARRIVE row wears its ✓/✗
    /// from, asked of <see cref="ArrivalStepRule.Check"/> — including the sense: the insertion window is
    /// STRICT where the clamp is inclusive, so a pass sitting exactly on the speed cap is red here and green
    /// at a clamp. A strip that borrowed the number but not the sense would be green over a pass the flight
    /// refuses, and that is what the exact-boundary row below is for.
    /// </summary>
    [Fact]
    public void TheOrbitCaptureGateIsTheArriveRowsOwnJudgement()
    {
        CelestialBody moon = Moon();
        double hill = OrbitRule.HillRadius(moon, 1.267e17);

        foreach (double d in new[] { 1e6, 1e9, 3e9, 3.1e9, 1e10 })
        {
            foreach (double v in new[] { 0.0, 4_999, 5_000, 5_001, 20_000 })
            {
                ArrivalStepRule.ArrivalCheck row =
                    ArrivalStepRule.Check(ArrivalStepRule.ArrivalKind.Orbit, moon.Name, d, v, hill);
                ConditionsReading strip = ConditionsGate.Orbit(moon.Name, d, v, hill, v);
                Assert.Equal(row.Valid, strip.AllInside);
                Assert.Equal(row.DistanceOk, Chip(strip, "close enough").Inside);
                Assert.Equal(row.SpeedOk, Chip(strip, "slow enough").Inside);
            }
        }

        // The strict sense, stated as its own row: exactly AT the cap the arrival is refused, so the chip is.
        double cap = OrbitRule.MaxRelativeSpeed;
        Assert.False(Chip(ConditionsGate.Orbit(moon.Name, 1e6, cap, hill, 0), "slow enough").Inside);
        Assert.True(Chip(ConditionsGate.Orbit(moon.Name, 1e6, cap - 1, hill, 0), "slow enough").Inside);

        // …and the distance edge, bisected: the strip's edge is the arrival's edge, which is capture range.
        double stripEdge = EdgeOf(d => Chip(ConditionsGate.Orbit(moon.Name, d, 100, hill, 0), "close enough").Inside, 1e4, 1e13);
        double rowEdge = EdgeOf(
            d => ArrivalStepRule.Check(ArrivalStepRule.ArrivalKind.Orbit, moon.Name, d, 100, hill).DistanceOk, 1e4, 1e13);
        Assert.Equal(rowEdge, stripEdge, 3);
    }

    /// <summary>
    /// <b>THE SHUTTLE HOP HAS ONE CRITERION, AND THE STRIP DRAWS ONE.</b> The hop's reach is
    /// <see cref="ShuttleRange.InRange"/>; there is no speed gate on a hop, and a strip that drew an empty
    /// second chip would be showing the captain a criterion nothing in the game enforces.
    /// </summary>
    [Fact]
    public void TheHopIsOneCriterionBecauseTheHopHasOneGate()
    {
        ConditionsReading hop = ConditionsGate.Hop("Phobos", 1e8, 0);
        Assert.Single(hop.Criteria);

        foreach (double d in new[] { 0.0, 1e8, 4.9e8, 5e8, 5.1e8, 1e11 })
        {
            Assert.Equal(ShuttleRange.InRange(d), ConditionsGate.Hop("Phobos", d, 0).AllInside);
        }

        double stripEdge = EdgeOf(d => ConditionsGate.Hop("Phobos", d, 0).AllInside, 0, 1e12);
        double ruleEdge = EdgeOf(ShuttleRange.InRange, 0, 1e12);
        Assert.Equal(ruleEdge, stripEdge, 3);
    }

    /// <summary>
    /// #243 · <b>THE WINDOW IS ITS OWN TWO HALVES.</b> <see cref="CaptureRule.IsInWindow"/> was rewritten in
    /// terms of the two hoisted predicates so the strip reads the launch's own code; this proves the rewrite
    /// changed nothing — the composed answer is the conjunction, at and either side of both edges, and the
    /// squared comparisons it replaced agree with it everywhere tried.
    /// </summary>
    [Fact]
    public void TheWindowIsItsOwnTwoHalves()
    {
        foreach (double d in new[] { 0.0, 1e6, 4.999e8, 5e8, 5.001e8, 1e10 })
        {
            foreach (double v in new[] { 0.0, 1_000, 4_999.9, 5_000, 5_000.1, 50_000 })
            {
                (ShipState p, ShipState t) = Pair(d, v);
                bool composed = CaptureRule.RangeInWindow(d) && CaptureRule.SpeedInWindow(v);
                bool squared = (p.Position - t.Position).LengthSquared
                                   <= CaptureRule.CaptureRadiusMeters * CaptureRule.CaptureRadiusMeters
                               && (p.Velocity - t.Velocity).LengthSquared
                                   <= CaptureRule.MaxRelativeSpeed * CaptureRule.MaxRelativeSpeed;

                Assert.Equal(composed, CaptureRule.IsInWindow(p, t));
                Assert.Equal(squared, CaptureRule.IsInWindow(p, t));
            }
        }
    }

    // ── THE TREND ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #243/#210 · <b>THE ARROW IS THE RANGE-RATE, AND IT POINTS THE WAY THE SHIP IS GOING.</b> Owner: <i>"Are
    /// we closing or distancing… a failing chip that is IMPROVING reads differently from one getting worse."</i>
    /// The rate is not a second measurement — it is <see cref="RelativeMotion.ClosingSpeed"/>, the one the
    /// Scope and the tracked-target cards already speak, so this ties the chip to that helper's SIGN rather
    /// than restating the convention.
    /// </summary>
    [Fact]
    public void TheRangeChipsArrowIsThatSignedRangeRate()
    {
        foreach ((double rel, ConditionTrend want) in new[]
                 {
                     (3_000.0, ConditionTrend.Improving),    // closing on her
                     (-3_000.0, ConditionTrend.Worsening),   // falling behind
                     (0.0, ConditionTrend.Steady),           // neither
                 })
        {
            (ShipState p, ShipState t) = Pair(1e8, rel);
            double closing = RelativeMotion.ClosingSpeed(p.Position, p.Velocity, t.Position, t.Velocity);

            // The helper and the world agree about which way this is: the sign IS the direction.
            Assert.Equal(Math.Sign(rel), Math.Sign(Math.Round(closing, 6)));

            GateCriterion range = Chip(ConditionsGate.Board("Kestrel", 1e8, Math.Abs(rel), closing), "close enough");
            Assert.Equal(want, range.Trend);
            Assert.Equal(
                want switch
                {
                    ConditionTrend.Improving => "▼",
                    ConditionTrend.Worsening => "▲",
                    _ => "",
                },
                ConditionsGate.TrendGlyph(range.Trend));
        }
    }

    /// <summary>
    /// <b>THE SPEED CHIP CARRIES NO ARROW, AND THAT IS THE POINT.</b> Nothing in the sim computes d|Δv|/dt,
    /// so the strip draws nothing there — <see cref="ConditionTrend.Unknown"/>, whose glyph is the empty
    /// string. An instrument that drew ▼ on a rate nobody measured would be the third named bug class by
    /// hand: a drawn shape reporting what the sim never worked out.
    /// </summary>
    [Fact]
    public void NothingMeasuresTheDriftsRateSoNoArrowIsDrawnOnIt()
    {
        GateCriterion drift = Chip(ConditionsGate.Board("Kestrel", 1e8, 9_000, 4_000), "speed match");
        Assert.Equal(ConditionTrend.Unknown, drift.Trend);
        Assert.Equal(string.Empty, ConditionsGate.TrendGlyph(drift.Trend));
        Assert.Equal(string.Empty, ConditionsGate.TrendWord(drift.Trend));

        // …while the range chip on the very same reading DOES carry one — so the absence above is a
        // decision and not a strip that has simply stopped computing trends.
        Assert.Equal(ConditionTrend.Improving, Chip(ConditionsGate.Board("Kestrel", 1e8, 9_000, 4_000), "close enough").Trend);
    }

    /// <summary>
    /// <b>STEADY IS THE GEOMETRY'S OWN ZERO, AND NOT A COMFORTABLE BAND AROUND IT.</b> This guard exists
    /// because the first cut of the trend was wrong in a way that LOOKED principled: a still-band scaled to
    /// the chip's limit. The clamp's limit is 5×10⁸ m and the speeds crossing it are ~10³ m/s, so a band of
    /// even a tenth of a percent of the limit per second swallowed every closing speed the game can make and
    /// the arrow never moved — an instrument that always says "steady" while the ship falls toward a berth.
    /// So the rows below assert at the scale the game actually produces: a kilometre a second MOVES.
    /// </summary>
    [Fact]
    public void SteadyIsTheGeometrysZeroAndARealClosingSpeedIsNever()
    {
        Assert.Equal(ConditionTrend.Improving, ConditionsGate.TrendOf(-1_000));
        Assert.Equal(ConditionTrend.Worsening, ConditionsGate.TrendOf(1_000));
        Assert.Equal(ConditionTrend.Steady, ConditionsGate.TrendOf(0));
        Assert.Equal(ConditionTrend.Unknown, ConditionsGate.TrendOf(null));
        Assert.Equal(ConditionTrend.Unknown, ConditionsGate.TrendOf(double.NaN));

        // The band is small enough that the slowest drift the instruments quote still reads as motion:
        // a millimetre a second is a thousand times the stillness cut-off.
        Assert.Equal(ConditionTrend.Improving, ConditionsGate.TrendOf(-1e-3));
        Assert.Equal(ConditionTrend.Steady, ConditionsGate.TrendOf(ConditionsGate.StillnessMps / 2));

        // …and the chip built on a live approach agrees, which is what stops this being a test of TrendOf
        // alone: closing at 1 km/s on a clamp, the range chip reads improving, not steady.
        Assert.Equal(
            ConditionTrend.Improving,
            Chip(ConditionsGate.Dock(Affordance(1e8, 1_000), 100_000, 1_000), "close enough").Trend);
    }

    // ── WHICH GATE SPEAKS ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The strip is ABSENT when no gate is live — the owner's complaint was a screen that has to be hunted
    /// across, and a permanently reserved empty rectangle is one more thing to hunt past. It is also absent
    /// for a "gate" with no criteria at all, which is what a dock affordance resolves to when nothing
    /// dockable is in play.
    /// </summary>
    [Fact]
    public void NoLiveGateMeansNoStripAtAll()
    {
        Assert.Null(ConditionsGate.Active(Array.Empty<ConditionsReading>()));
        Assert.Null(ConditionsGate.Active(new[] { ConditionsGate.Dock(DockAffordance.Hidden, 0, 0) }));
        Assert.Empty(ConditionsGate.Dock(DockAffordance.Hidden, 0, 0).Criteria);
    }

    /// <summary>When two gates are live at once the strip speaks the one closing fastest on the captain —
    /// the order of commitment, not of arrival in a list.</summary>
    [Fact]
    public void TheMostCommittingGateSpeaksFirst()
    {
        ConditionsReading board = ConditionsGate.Board("Kestrel", 1e8, 1_000, 0);
        ConditionsReading dock = ConditionsGate.Dock(Affordance(1e8, 1_000), 100_000, 0);
        ConditionsReading hop = ConditionsGate.Hop("Phobos", 1e8, 0);

        Assert.Equal(ConditionGateKind.Board, ConditionsGate.Active(new[] { hop, dock, board })!.Value.Kind);
        Assert.Equal(ConditionGateKind.Dock, ConditionsGate.Active(new[] { hop, dock })!.Value.Kind);
        Assert.Equal(ConditionGateKind.ShuttleHop, ConditionsGate.Active(new[] { hop })!.Value.Kind);

        // Order in the list must not decide it — the same three, shuffled, answer the same.
        Assert.Equal(ConditionGateKind.Board, ConditionsGate.Active(new[] { board, hop, dock })!.Value.Kind);
    }

    /// <summary>
    /// #243 · <b>THE SCOPE CORNER SAYS THE SAME THING THE STRIP DOES.</b> Owner: <i>"Mirror the strip's
    /// summary in the Scope corner (where the eyes are during an approach)."</i> A mirror that could differ
    /// from what it mirrors is worse than no mirror, so the summary is derived from the very criteria the
    /// strip draws: one mark per chip, in the strip's own order.
    /// </summary>
    [Fact]
    public void TheScopeSummaryIsTheStripsOwnChipsInTheStripsOwnOrder()
    {
        // In range, far too fast: the first mark passes and the second does not.
        ConditionsReading hot = ConditionsGate.Board("Kestrel", 1e8, 40_000, 0);
        Assert.Equal("🎯 ✓✗", ConditionsGate.ScopeSummary(hot));
        Assert.False(hot.AllInside);

        ConditionsReading good = ConditionsGate.Board("Kestrel", 1e8, 1_000, 0);
        Assert.Equal("🎯 ✓✓", ConditionsGate.ScopeSummary(good));
        Assert.True(good.AllInside);

        // Generic, not a pair of typed strings: every gate's summary is its own chips, whatever their count.
        foreach (ConditionsReading r in new[]
                 {
                     hot, good,
                     ConditionsGate.Dock(Affordance(1e8, 1_000), 100_000, 0),
                     ConditionsGate.Orbit("Europa", 1e9, 1_000, 1e9, 0),
                     ConditionsGate.Hop("Phobos", 1e8, 0),
                 })
        {
            string marks = ConditionsGate.ScopeSummary(r)[(ConditionsGate.Glyph(r.Kind).Length + 1)..];
            Assert.Equal(r.Criteria.Count, marks.Length);
            for (int i = 0; i < r.Criteria.Count; i++)
            {
                Assert.Equal(r.Criteria[i].Inside ? '✓' : '✗', marks[i]);
            }
        }
    }

    /// <summary>
    /// <b>ONE SOURCE — the clamp's chips ARE #200's rows.</b> Not "the same numbers": the same objects,
    /// converted field for field. The day somebody adds a fourth gate to the focus panel the strip grows it
    /// too, and the day somebody re-types one here this goes red.
    /// </summary>
    [Fact]
    public void TheClampsChipsAreTheFocusPanelsOwnRows()
    {
        // A hot approach inside the door, so the third (#213 match-burn) row is in play as well.
        DockAffordance hot = Affordance(1e8, 9_000, pulses: 100_000);
        IReadOnlyList<DockGateRow> rows = DockFocus.Rows(hot, 100_000);
        IReadOnlyList<GateCriterion> chips = ConditionsGate.Dock(hot, 100_000, 0).Criteria;

        Assert.Equal(3, rows.Count);
        Assert.Equal(rows.Count, chips.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            Assert.Equal(rows[i].Label, chips[i].Label);
            Assert.Equal(rows[i].Reading, chips[i].Reading);
            Assert.Equal(rows[i].Gate, chips[i].Gate);
            Assert.Equal(rows[i].Inside, chips[i].Inside);
        }
    }

    /// <summary>Every gate is formatted in one hand — the owner's complaint was that reading the criteria was
    /// SLOW, and four gates written in four unit styles is the slow version of this instrument.</summary>
    [Fact]
    public void EveryGateIsWrittenInTheSameUnits()
    {
        var everyGate = new List<ConditionsReading>
        {
            ConditionsGate.Board("Kestrel", 5e8, 5_000, 0),
            ConditionsGate.Dock(Affordance(5e8, 5_000), 100_000, 0),
            ConditionsGate.Orbit("Europa", 5e8, 5_000, 1e9, 0),
            ConditionsGate.Hop("Phobos", 5e8, 0),
        };

        foreach (ConditionsReading gate in everyGate)
        {
            Assert.NotEmpty(gate.Criteria);
            Assert.Contains(gate.TargetName, ConditionsGate.Title(gate), StringComparison.Ordinal);

            // The range criterion of every gate reads the SAME distance the same way…
            GateCriterion range = gate.Criteria[0];
            Assert.Equal(ConditionsGate.Km(5e8), range.Reading);
            Assert.StartsWith("≤ ", range.Gate, StringComparison.Ordinal);
        }

        // …and the speed criterion of the three gates that have one is likewise one hand, differing only in
        // the SENSE the sim enforces (strict at an insertion, inclusive at a clamp and a boarding).
        Assert.Equal(ConditionsGate.KmPerSecond(5_000), everyGate[0].Criteria[1].Reading);
        Assert.Equal(ConditionsGate.KmPerSecond(5_000), everyGate[1].Criteria[1].Reading);
        Assert.Equal(ConditionsGate.KmPerSecond(5_000), everyGate[2].Criteria[1].Reading);
        Assert.StartsWith("≤ ", everyGate[1].Criteria[1].Gate, StringComparison.Ordinal);
        Assert.StartsWith("< ", everyGate[2].Criteria[1].Gate, StringComparison.Ordinal);
    }
}
