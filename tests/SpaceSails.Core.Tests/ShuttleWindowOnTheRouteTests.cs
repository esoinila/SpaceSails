namespace SpaceSails.Core.Tests;

/// <summary>
/// #955 NAV-2 · THE WINDOW THAT COMES BACK, AND THE WINDOW THAT READS THE ROUTE.
///
/// <para>The owner's corner case (2026-08-23, verbatim): <b>"docked at a Jupiter/Saturn haven the shuttle
/// windows to the moons are PERIODIC by default … so a window that closes while you are ashore must NOT
/// mark the team dead/stranded when it will reopen on its own; and it is the case of a window lost and
/// regained without any plotted course."</b> Before this lane the away clock only survived a berth by
/// IGNORING the moon's motion (geometry = ∞ while clamped); the honest geometry would have read
/// <c>Lost</c> the instant Ganymede swung a hop and a half off The Red Eye.</para>
///
/// <para>And the payoff half: with an armed plan the ship flies on, so the window is the span of the
/// PLOTTED PATH inside a shuttle hop of the site, and RETURN BY is that egress minus the ride home.</para>
///
/// <para>Everything here plays the SHIPPING sol scenario — The Red Eye is a real berth around a real
/// Jupiter and Ganymede is a real moon on a real rail — so a tuning change to any of them is felt here
/// rather than in a browser.</para>
/// </summary>
public class ShuttleWindowOnTheRouteTests
{
    private const string Berth = "red-eye";
    private const string Moon = "ganymede";

    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    private static CelestialBody Body(ICelestialEphemeris eph, string id) =>
        eph.Bodies.First(b => b.Id == id);

    /// <summary>The honest berth↔moon separation at an absolute sim time — no ship, no plot, just the two
    /// rails around Jupiter. This is exactly what the docked branch of Map.Expedition now reads.</summary>
    private static double Separation(ICelestialEphemeris eph, double simTime) =>
        (eph.Position(Moon, simTime) - eph.Position(Berth, simTime)).Length;

    // ── 1 · THE LAW: Closed vs Lost ────────────────────────────────────────────────────────────────

    [Fact]
    public void OutOfReach_IsClosedWhenItReopens_AndLostOnlyWhenItDoesNot()
    {
        double past = 1.4 * ExpeditionWindow.RangeMeters;
        const double opening = 2_500.0;
        double critical = ExpeditionWindow.DefaultCriticalSeconds;

        // The very same geometry reads two different ways, and the ONLY difference is whether the future
        // brings it back. That is the whole of the owner's ruling.
        Assert.Equal(WindowStatus.Closed, ExpeditionWindow.Classify(past, opening, critical, secondsUntilReopen: 7_200));
        Assert.Equal(WindowStatus.Lost, ExpeditionWindow.Classify(past, opening, critical, secondsUntilReopen: null));

        // Inside reach the reopen is beside the point — the old readings are untouched.
        double half = 0.5 * ExpeditionWindow.RangeMeters;
        Assert.Equal(WindowStatus.Holding, ExpeditionWindow.Classify(half, 0.0, critical, 60.0));
        Assert.Equal(WindowStatus.Ticking, ExpeditionWindow.Classify(half, opening, critical, 60.0));

        // And the three-argument overload every older caller uses still means "ask nothing about the future".
        Assert.Equal(WindowStatus.Lost, ExpeditionWindow.Classify(past, opening, critical));
    }

    [Fact]
    public void SecondsUntilReopen_FindsTheCrossing_AndGivesUpAtTheHorizon()
    {
        // A gap that closes linearly: out of reach now, back inside in exactly 1000 s.
        double start = ExpeditionWindow.RangeMeters * 1.5;
        double closing = (start - ExpeditionWindow.RangeMeters) / 1_000.0;
        double? found = ExpeditionWindow.SecondsUntilReopen(t => start - (closing * t), horizonSeconds: 5_000);
        Assert.NotNull(found);
        Assert.Equal(1_000.0, found!.Value, ExpeditionWindow.ReopenPrecisionSeconds);

        // The same gap with a horizon that stops short: no reopening to promise.
        Assert.Null(ExpeditionWindow.SecondsUntilReopen(t => start - (closing * t), horizonSeconds: 500));

        // Already in reach — the caller asked an easy question and gets zero, never a scan.
        Assert.Equal(0.0, ExpeditionWindow.SecondsUntilReopen(_ => 1.0, horizonSeconds: 5_000));
    }

    /// <summary>
    /// THE 986-DAY READING. Found by opening the shuttle board at The Red Eye and looking: with Ganymede a
    /// quarter of a hop away the row said "window closes in 986 d 18 h". Nothing had thrown — the berth and
    /// the moon were near closest approach, where the range-rate is momentarily ZERO, and
    /// <see cref="ExpeditionWindow.TimeLeftInRangeSeconds"/> is a straight line divided by that rate. A
    /// drifting rock really does recede in a straight line; a moon on a rail does not. This pins the measured
    /// answer against the extrapolated one on the real geometry.
    /// </summary>
    [Fact]
    public void SecondsUntilClose_MeasuresTheGeometry_WhereTheRangeRateWouldLie()
    {
        ICelestialEphemeris eph = Sol();

        // t=0 in the shipping scenario: The Red Eye and Ganymede share an initial phase, so they are at
        // their closest and the rate through the line between them is ~nothing.
        double distance = Separation(eph, 0);
        double rate = Separation(eph, 1.0) - distance;
        Assert.True(distance < ExpeditionWindow.RangeMeters, "the bench wants them in reach at t=0");
        double extrapolated = ExpeditionWindow.TimeLeftInRangeSeconds(distance, rate);

        double? measured = ExpeditionWindow.SecondsUntilClose(
            dt => Separation(eph, dt), ExpeditionWindow.MaxReopenHorizonSeconds);
        Assert.NotNull(measured);

        // The window really shuts inside a few days — and the straight line said years.
        Assert.InRange(measured!.Value, 3_600.0, 10.0 * 86400.0);
        Assert.True(extrapolated > 30.0 * 86400.0,
            $"the bench's whole point is that the extrapolation is wild here; it read {extrapolated / 86400:F0} d");

        // The measured instant is the crossing, to the second: out of reach just after, in reach just before.
        Assert.True(Separation(eph, measured.Value) >= ExpeditionWindow.RangeMeters);
        Assert.True(Separation(eph, measured.Value - 60.0) < ExpeditionWindow.RangeMeters);

        // A pair that never leaves reach has no close time at all — that is a window that HOLDS.
        Assert.Null(ExpeditionWindow.SecondsUntilClose(_ => 1.0, ExpeditionWindow.MaxReopenHorizonSeconds));

        // And out of reach already is an easy question, answered with zero rather than a scan.
        Assert.Equal(0.0, ExpeditionWindow.SecondsUntilClose(
            _ => 2 * ExpeditionWindow.RangeMeters, ExpeditionWindow.MaxReopenHorizonSeconds));
    }

    [Fact]
    public void ClassifyClock_ReadsTheSameLawOffAMeasuredClock()
    {
        double half = 0.5 * ExpeditionWindow.RangeMeters;
        double critical = ExpeditionWindow.DefaultCriticalSeconds;

        Assert.Equal(WindowStatus.Holding,
            ExpeditionWindow.ClassifyClock(half, double.PositiveInfinity, critical, null));
        Assert.Equal(WindowStatus.Ticking, ExpeditionWindow.ClassifyClock(half, 10_000, critical, null));
        Assert.Equal(WindowStatus.Critical, ExpeditionWindow.ClassifyClock(half, critical - 1, critical, null));
        Assert.Equal(WindowStatus.Closed,
            ExpeditionWindow.ClassifyClock(2 * ExpeditionWindow.RangeMeters, 0, critical, 900));
        Assert.Equal(WindowStatus.Lost,
            ExpeditionWindow.ClassifyClock(2 * ExpeditionWindow.RangeMeters, 0, critical, null));
    }

    [Fact]
    public void SynodicPeriod_IsTheHonestHorizonForABerthAndAMoon()
    {
        ICelestialEphemeris eph = Sol();
        double synodic = ExpeditionWindow.SynodicPeriodSeconds(
            Body(eph, Berth).OrbitPeriod, Body(eph, Moon).OrbitPeriod);

        // The Red Eye laps Ganymede about once every seventeen days.
        Assert.InRange(synodic, 1.40e6, 1.60e6);

        // Two hulls in formation never come back, because they never left.
        Assert.True(double.IsPositiveInfinity(ExpeditionWindow.SynodicPeriodSeconds(4.37e5, 4.37e5)));

        // A retrograde rail (Triton's negative period) is read by magnitude, not swallowed.
        Assert.True(ExpeditionWindow.SynodicPeriodSeconds(-5.0777e5, 1.0e6) > 0);
    }

    // ── 2 · THE CORNER CASE: a Jupiter berth, a moon site, one full close-and-reopen ────────────────

    [Fact]
    public void DockedAtAJupiterBerth_TheMoonWindowClosesAndReopens_AndNobodyIsEverLost()
    {
        ICelestialEphemeris eph = Sol();
        double synodic = ExpeditionWindow.SynodicPeriodSeconds(
            Body(eph, Berth).OrbitPeriod, Body(eph, Moon).OrbitPeriod);

        const double walkStep = 7_200.0;   // two sim-hours across a seventeen-day cycle
        int sawInReach = 0, sawClosed = 0;

        for (double now = 0; now <= synodic; now += walkStep)
        {
            double distance = Separation(eph, now);
            double rate = (Separation(eph, now + 1.0) - distance);   // per second

            // The horizon a BERTH scans: one full turn of the relative geometry. No plotted course exists.
            double? reopen = distance >= ExpeditionWindow.RangeMeters
                ? ExpeditionWindow.SecondsUntilReopen(dt => Separation(eph, now + dt), synodic)
                : null;

            WindowStatus status = ExpeditionWindow.Classify(
                distance, rate, ExpeditionWindow.DefaultCriticalSeconds, reopen);

            Assert.NotEqual(WindowStatus.Lost, status);   // THE RULING: never dead at a berth it will come back to
            if (status == WindowStatus.Closed) { sawClosed++; } else { sawInReach++; }
        }

        // The world must actually be able to tell pass from fail: the cycle has to CLOSE and to REOPEN,
        // or "never Lost" would be a green number asked of nothing.
        Assert.True(sawClosed > 0, "the moon never left shuttle reach — this berth cannot test the ruling");
        Assert.True(sawInReach > 0, "the moon never came back into reach — this berth cannot test the ruling");
    }

    [Fact]
    public void ThePredictedReopen_IsWithinAMinuteOfTheGeometry()
    {
        ICelestialEphemeris eph = Sol();
        double synodic = ExpeditionWindow.SynodicPeriodSeconds(
            Body(eph, Berth).OrbitPeriod, Body(eph, Moon).OrbitPeriod);

        // The first moment the window is shut — the instant a captain ashore starts wanting a number.
        double closedAt = 0;
        while (Separation(eph, closedAt) < ExpeditionWindow.RangeMeters)
        {
            closedAt += 3_600.0;
        }

        double? reopen = ExpeditionWindow.SecondsUntilReopen(dt => Separation(eph, closedAt + dt), synodic);
        Assert.NotNull(reopen);
        double r = reopen!.Value;

        // At the promised instant the shuttle can cross…
        Assert.True(Separation(eph, closedAt + r) <= ExpeditionWindow.RangeMeters,
            "the window was promised open and is not");

        // …and a minute earlier it could not. That is "within a minute of the geometry", both ways.
        Assert.True(Separation(eph, closedAt + r - 60.0) > ExpeditionWindow.RangeMeters,
            "the promise was more than a minute early");
    }

    [Fact]
    public void ARockThatLeavesForGood_IsStillLost()
    {
        // The #370 expedition rock: seeded half a hop off and drifting straight out at DriftSpeedMps. It
        // is on no rail and nothing brings it back — the negative that keeps Closed honest.
        double start = ExpeditionSite.SpawnFraction * ExpeditionWindow.RangeMeters;
        double DriftingAway(double dt) => start + (ExpeditionSite.DriftSpeedMps * dt);

        // Wind it forward until the gap is genuinely past the edge, then ask.
        double now = (ExpeditionWindow.RangeMeters - start) / ExpeditionSite.DriftSpeedMps * 2.0;
        double distance = DriftingAway(now);
        Assert.True(distance > ExpeditionWindow.RangeMeters);

        double? reopen = ExpeditionWindow.SecondsUntilReopen(
            dt => DriftingAway(now + dt), ExpeditionWindow.MaxReopenHorizonSeconds);
        Assert.Null(reopen);
        Assert.Equal(WindowStatus.Lost, ExpeditionWindow.Classify(
            distance, ExpeditionSite.DriftSpeedMps, ExpeditionWindow.DefaultCriticalSeconds, reopen));
    }

    // ── 3 · THE PLOTTED ROUTE: the window is the span of the path inside a hop, minus the ride home ──

    private static IReadOnlyList<RouteShuttleWindow.RouteSample> Ramp(params (double T, double D)[] pairs) =>
        [.. pairs.Select(p => new RouteShuttleWindow.RouteSample(p.T, p.D))];

    [Fact]
    public void RouteWindow_IsTheInRangeSpanOfTheSamples_MinusTheHop()
    {
        double range = ShuttleRange.RangeMeters;

        // A pass: far, in, in, far. The edges are interpolated, so they land ON the range boundary rather
        // than on whichever sample happened to be nearest it.
        var samples = Ramp(
            (0, 2 * range), (100, range * 0.5), (200, range * 0.5), (300, 2 * range));

        IReadOnlyList<RouteShuttleWindow.Window> windows = RouteShuttleWindow.Along(samples);
        RouteShuttleWindow.Window w = Assert.Single(windows);

        Assert.Equal(66.666, w.OpensSimTime, 0.01);    // crossing between t=0 (2R) and t=100 (0.5R)
        Assert.Equal(233.333, w.ClosesSimTime, 0.01);  // crossing between t=200 (0.5R) and t=300 (2R)

        // RETURN BY is the EGRESS minus the ride home, and the ride home at the egress is a full-reach
        // crossing — the widest gap the boat will ever be asked to fly.
        Assert.Equal(w.ClosesSimTime - ShuttleRange.TravelSeconds(range), w.ReturnBySimTime, 1e-6);

        // This particular pass is far too brief to be worth offering: the crossing eats it whole.
        Assert.False(w.IsUsable);
    }

    [Fact]
    public void RouteWindow_TwoDisjointSpans_AreTheIngressAndTheEgressWindows()
    {
        double range = ShuttleRange.RangeMeters;
        double hop = ShuttleRange.TravelSeconds(range);

        // The owner's sensitive visit: the plotted path passes the site, leaves, and comes back round.
        var samples = Ramp(
            (0, 2 * range), (1e5, 0.2 * range), (2e5, 0.2 * range), (3e5, 2 * range),
            (4e5, 2 * range), (5e5, 0.2 * range), (6e5, 0.2 * range), (7e5, 2 * range));

        IReadOnlyList<RouteShuttleWindow.Window> windows = RouteShuttleWindow.Along(samples);
        Assert.Equal(2, windows.Count);

        // Two real windows, in path order, and each with its own RETURN BY. They do not overlap: an
        // ingress and an egress, not one long window with a hole punched in it.
        Assert.True(windows[0].ClosesSimTime < windows[1].OpensSimTime);
        foreach (RouteShuttleWindow.Window w in windows)
        {
            Assert.Equal(w.ClosesSimTime - hop, w.ReturnBySimTime, 1e-6);
            Assert.True(w.IsUsable, "a 1.7-day span should leave time on the ground after the ride home");
        }
    }

    [Fact]
    public void RouteWindow_StillInReachWhenTheSamplesRunOut_PricesTheRideHomeAtTheRealGap()
    {
        double range = ShuttleRange.RangeMeters;
        var samples = Ramp((0, 2 * range), (1e5, 0.25 * range), (2e5, 0.25 * range));

        RouteShuttleWindow.Window w = Assert.Single(RouteShuttleWindow.Along(samples));

        // The plan's horizon closed the window, not the geometry — so the boat is charged for the gap that
        // is actually standing there (a quarter hop), never for a full-reach crossing it never makes.
        Assert.Equal(2e5, w.ClosesSimTime, 1e-6);
        Assert.Equal(2e5 - ShuttleRange.TravelSeconds(0.25 * range), w.ReturnBySimTime, 1e-6);
    }

    [Fact]
    public void RouteWindow_APathThatNeverComesInsideAHop_OffersNothing()
    {
        double range = ShuttleRange.RangeMeters;
        Assert.Empty(RouteShuttleWindow.Along(Ramp((0, 3 * range), (1e5, 1.2 * range), (2e5, 4 * range))));
        Assert.Empty(RouteShuttleWindow.Along([]));
    }

    // ── 4 · THE REMOTE READS THE SAME NUMBER ────────────────────────────────────────────────────────

    [Fact]
    public void TheRemotesReturnBy_IsTheEgressMinusTheHop_AndTheSameLineServesTheAwaySiteHud()
    {
        double range = ShuttleRange.RangeMeters;
        var samples = Ramp((0, 2 * range), (1e5, 0.2 * range), (2e5, 0.2 * range), (3e5, 2 * range));
        RouteShuttleWindow.Window w = Assert.Single(RouteShuttleWindow.Along(samples));

        Assert.Equal(w.ClosesSimTime - ShuttleRange.TravelSeconds(range), w.ReturnBySimTime, 1e-6);

        string line = RouteShuttleWindow.RemoteLine(nowSimTime: 1.2e5, w.ReturnBySimTime, reopenSeconds: null);
        Assert.Contains("RETURN BY", line, StringComparison.Ordinal);
        Assert.Contains(RouteShuttleWindow.Stamp(w.ReturnBySimTime), line, StringComparison.Ordinal);
        Assert.Contains("no next window", line, StringComparison.Ordinal);

        // Ashore with the window shut but a reopening promised — the docked corner case, said out loud.
        string waiting = RouteShuttleWindow.RemoteLine(nowSimTime: 1.2e5, returnBySimTime: null, reopenSeconds: 9_000);
        Assert.Contains("NO RETURN WINDOW", waiting, StringComparison.Ordinal);
        Assert.Contains("next window in 2 h 30 m", waiting, StringComparison.Ordinal);

        // Nothing is flying on, but the window itself is running out — the line says which clock is which.
        string closing = RouteShuttleWindow.RemoteLine(
            nowSimTime: 1.2e5, returnBySimTime: null, reopenSeconds: null, secondsLeftInRange: 3 * 3600);
        Assert.Equal("🛸 WINDOW CLOSES IN 3 h 00 m", closing);

        // …and an open window with no reopening to name does not mutter "no next window" at the captain.
        Assert.DoesNotContain("no next window", closing, StringComparison.Ordinal);
    }

    [Fact]
    public void TheClockCopy_ReadsInTheHouseLadder()
    {
        Assert.Equal("under a minute", RouteShuttleWindow.In(30));
        Assert.Equal("45 m", RouteShuttleWindow.In(45 * 60));
        Assert.Equal("3 h 20 m", RouteShuttleWindow.In((3 * 3600) + (20 * 60)));
        Assert.Equal("2 d 06 h", RouteShuttleWindow.In((2 * 86400) + (6 * 3600)));
        Assert.Equal("under a minute", RouteShuttleWindow.In(-500));   // never negative

        Assert.Equal("0d 00:00", RouteShuttleWindow.Stamp(0));
        Assert.Equal("3d 07:41", RouteShuttleWindow.Stamp((3 * 86400) + (7 * 3600) + (41 * 60)));
    }
}
