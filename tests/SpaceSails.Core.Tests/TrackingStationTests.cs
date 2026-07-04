namespace SpaceSails.Core.Tests;

public class TrackingStationTests
{
    private const double Day = 86400;

    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    // ---- TelescopeModel range envelope ----

    [Fact]
    public void Range_Sunward_IsAboutEightPercent()
    {
        var telescope = new TelescopeModel();
        var shipPosition = new Vector2d(1e11, 0); // sun at origin, so sunward look direction is -X
        Vector2d lookAtSun = new Vector2d(-1, 0);

        double range = telescope.Range(shipPosition, lookAtSun);

        Assert.Equal(telescope.BaseRange * TelescopeModel.SunwardRangeFactor, range, 3);
    }

    [Fact]
    public void Range_AntiSunward_IsFullBaseRange()
    {
        var telescope = new TelescopeModel();
        var shipPosition = new Vector2d(1e11, 0);
        Vector2d lookAwayFromSun = new Vector2d(1, 0); // away from the sun

        double range = telescope.Range(shipPosition, lookAwayFromSun);

        Assert.Equal(telescope.BaseRange, range, 3);
    }

    [Fact]
    public void Range_IsMonotonic_FromSunwardToAntiSunward()
    {
        var telescope = new TelescopeModel();
        var shipPosition = new Vector2d(1e11, 0);

        double previous = double.NegativeInfinity;
        for (int i = 0; i <= 16; i++)
        {
            // Sweep the look direction from straight-at-the-sun (-X) to straight-away (+X)
            // through the upper half-plane, φ = i/16 * π.
            double phi = Math.PI * i / 16.0;
            var look = new Vector2d(-Math.Cos(phi), Math.Sin(phi));
            double range = telescope.Range(shipPosition, look);

            Assert.True(range >= previous - 1e-6, $"Range should be monotonic in φ; step {i} went {previous:E3} -> {range:E3}");
            previous = range;
        }

        Assert.InRange(previous, telescope.BaseRange * 0.999, telescope.BaseRange * 1.001);
    }

    // ---- Sweep detection ----

    [Fact]
    public void TryDetect_FindsShip_InsideWedgeAndRange()
    {
        var telescope = new TelescopeModel(baseRangeMeters: 1e10);
        var job = new ScanJob(CenterBearingRad: 0, ArcWidthRad: 30 * Math.PI / 180);
        var observer = Vector2d.Zero;
        var target = new ShipState(new Vector2d(5e9, 0), Vector2d.Zero, 0); // dead ahead, in range

        bool found = TrackingStation.TryDetect(telescope, job, observer, "t1", target, sweepCompleteTime: 100, out Observation obs);

        Assert.True(found);
        Assert.Equal("t1", obs.TargetId);
        Assert.Equal(100, obs.SimTime);
        Assert.Equal(target.Position, obs.Position);
    }

    [Fact]
    public void TryDetect_Misses_ShipOutsideWedge()
    {
        var telescope = new TelescopeModel(baseRangeMeters: 1e10);
        var job = new ScanJob(CenterBearingRad: 0, ArcWidthRad: 10 * Math.PI / 180);
        var observer = Vector2d.Zero;
        // 90 degrees off the wedge center, well within range otherwise.
        var target = new ShipState(new Vector2d(0, 5e9), Vector2d.Zero, 0);

        bool found = TrackingStation.TryDetect(telescope, job, observer, "t2", target, 100, out _);

        Assert.False(found);
    }

    [Fact]
    public void TryDetect_Misses_ShipBeyondRange()
    {
        var telescope = new TelescopeModel(baseRangeMeters: 1e10);
        var job = new ScanJob(CenterBearingRad: 0, ArcWidthRad: 30 * Math.PI / 180);
        var observer = Vector2d.Zero;
        // Dead ahead but far beyond the (already sun-dimmed) range.
        var target = new ShipState(new Vector2d(5e11, 0), Vector2d.Zero, 0);

        bool found = TrackingStation.TryDetect(telescope, job, observer, "t3", target, 100, out _);

        Assert.False(found);
    }

    [Fact]
    public void Sweep_ReturnsOnlyDetectedCandidates()
    {
        var telescope = new TelescopeModel(baseRangeMeters: 1e10);
        var job = new ScanJob(CenterBearingRad: 0, ArcWidthRad: 20 * Math.PI / 180);
        var observer = Vector2d.Zero;

        var candidates = new (string Id, ShipState State)[]
        {
            ("inside", new ShipState(new Vector2d(4e9, 0), Vector2d.Zero, 0)),
            ("wrong-bearing", new ShipState(new Vector2d(0, 4e9), Vector2d.Zero, 0)),
            ("too-far", new ShipState(new Vector2d(5e11, 0), Vector2d.Zero, 0)),
        };

        IReadOnlyList<Observation> found = TrackingStation.Sweep(telescope, job, observer, candidates, sweepCompleteTime: 50);

        Assert.Single(found);
        Assert.Equal("inside", found[0].TargetId);
    }

    // ---- Sweep timing ----

    [Fact]
    public void FullSurvey_TakesSixHoursOfSimTime()
    {
        var job = new ScanJob(CenterBearingRad: 0, ArcWidthRad: Math.Tau);

        Assert.Equal(ScanJob.FullSurveySeconds, job.DurationSeconds, 3);
        Assert.Equal(6 * 3600, job.DurationSeconds, 3);
    }

    [Fact]
    public void SweepDuration_IsProportionalToArcWidth()
    {
        var halfSurvey = new ScanJob(0, Math.PI);
        var quarterSurvey = new ScanJob(0, Math.PI / 2);

        Assert.Equal(ScanJob.FullSurveySeconds / 2, halfSurvey.DurationSeconds, 3);
        Assert.Equal(ScanJob.FullSurveySeconds / 4, quarterSurvey.DurationSeconds, 3);
    }

    // ---- Ledger lifecycle ----

    [Fact]
    public void Ledger_Add_CreatesTrackWithInitialQuality()
    {
        var ledger = new TrackedTargetLedger(maxTracks: 2);
        var obs = new Observation("s1", 0, Vector2d.Zero, Vector2d.Zero);

        bool added = ledger.Add(obs);

        Assert.True(added);
        Assert.True(ledger.IsTracked("s1"));
        Assert.True(ledger.TryGet("s1", out TrackedTarget entry));
        Assert.Equal(TrackedTargetLedger.InitialQuality, entry.Quality);
    }

    [Fact]
    public void Ledger_MaxTracks_IsEnforced()
    {
        var ledger = new TrackedTargetLedger(maxTracks: 1);

        Assert.True(ledger.Add(new Observation("s1", 0, Vector2d.Zero, Vector2d.Zero)));
        Assert.False(ledger.Add(new Observation("s2", 0, new Vector2d(1, 0), Vector2d.Zero)));
        Assert.True(ledger.IsTracked("s1"));
        Assert.False(ledger.IsTracked("s2"));

        // Re-detecting the ship already tracked is always free — it's not a new slot.
        Assert.True(ledger.Add(new Observation("s1", 10, new Vector2d(2, 0), Vector2d.Zero)));
    }

    [Fact]
    public void Ledger_Decay_ReducesQuality_AndLosesTrackEventually()
    {
        var ledger = new TrackedTargetLedger(maxTracks: 1);
        ledger.Add(new Observation("s1", 0, Vector2d.Zero, Vector2d.Zero));
        ledger.TryGet("s1", out TrackedTarget fresh);

        // Still within the staleness horizon: no decay yet.
        double withinHorizon = TrackedTargetLedger.StalenessHorizonSeconds - Day;
        Assert.Equal(fresh.Quality, fresh.EffectiveQuality(withinHorizon));

        // Well past the horizon: quality has decayed.
        double wayStale = TrackedTargetLedger.StalenessHorizonSeconds + 3 * Day;
        Assert.True(fresh.EffectiveQuality(wayStale) < fresh.Quality);

        // Advancing the ledger that far should drop the track (initial quality is low, so a
        // modest number of stale days already crosses LostThreshold).
        ledger.AdvanceTime(wayStale);
        Assert.False(ledger.IsTracked("s1"));
    }

    [Fact]
    public void Ledger_Confirm_Succeeds_WhenShipIsWherePredicted()
    {
        var ephemeris = Sol();
        var ledger = new TrackedTargetLedger(maxTracks: 1);
        var telescope = new TelescopeModel(baseRangeMeters: 1e12); // generous range for this test
        var observerPosition = new Vector2d(2e11, 0);

        var initialState = new ShipState(new Vector2d(1.5e11, 0), new Vector2d(0, 2000), 0);
        var obs = new Observation("s1", 0, initialState.Position, initialState.Velocity);
        ledger.Add(obs);
        ledger.TryGet("s1", out TrackedTarget before);

        // Actual ship coasts ballistically — right where PathPredictor expects it.
        var simulator = new Simulator(ephemeris, timeStepSeconds: 60);
        ShipState actual = simulator.Run(initialState, 3600, ManeuverPlan.Empty);

        bool confirmed = ledger.TryConfirm("s1", ephemeris, telescope, observerPosition, actual, actual.SimTime);

        Assert.True(confirmed);
        Assert.True(ledger.TryGet("s1", out TrackedTarget after));
        Assert.True(after.Quality > before.Quality);
        Assert.Equal(actual.SimTime, after.LastConfirmedTime);
    }

    [Fact]
    public void Ledger_Confirm_Fails_WhenShipStrayedFarFromPrediction()
    {
        var ephemeris = Sol();
        var ledger = new TrackedTargetLedger(maxTracks: 1);
        var telescope = new TelescopeModel(baseRangeMeters: 1e12);
        var observerPosition = new Vector2d(2e11, 0);

        var initialState = new ShipState(new Vector2d(1.5e11, 0), new Vector2d(0, 2000), 0);
        var obs = new Observation("s1", 0, initialState.Position, initialState.Velocity);
        ledger.Add(obs);

        // A wild position far from anything a ballistic coast would predict.
        var strayed = new ShipState(new Vector2d(-3e11, 4e11), Vector2d.Zero, 3600);

        bool confirmed = ledger.TryConfirm("s1", ephemeris, telescope, observerPosition, strayed, 3600);

        Assert.False(confirmed);
    }

    [Fact]
    public void Ledger_Drop_RemovesEntry()
    {
        var ledger = new TrackedTargetLedger(maxTracks: 1);
        ledger.Add(new Observation("s1", 0, Vector2d.Zero, Vector2d.Zero));

        Assert.True(ledger.Drop("s1"));
        Assert.False(ledger.IsTracked("s1"));
        Assert.False(ledger.Drop("s1")); // already gone
    }

    // ---- UncertaintyScale ----

    [Fact]
    public void UncertaintyScale_IsMonotonicallyTighter_WithHigherQuality()
    {
        double previous = double.PositiveInfinity;
        for (double quality = 0; quality <= 1.0; quality += 0.1)
        {
            var target = new TrackedTarget("s1", new Observation("s1", 0, Vector2d.Zero, Vector2d.Zero), 0, quality);
            double scale = target.UncertaintyScale(0); // no decay at t = LastConfirmedTime

            Assert.True(scale <= previous + 1e-9, $"UncertaintyScale should shrink as quality grows; quality {quality} gave {scale}, previous {previous}");
            previous = scale;
        }

        var zero = new TrackedTarget("s1", new Observation("s1", 0, Vector2d.Zero, Vector2d.Zero), 0, 0);
        var perfect = new TrackedTarget("s1", new Observation("s1", 0, Vector2d.Zero, Vector2d.Zero), 0, 1);
        Assert.Equal(1.0, zero.UncertaintyScale(0), 6);
        Assert.Equal(0.3, perfect.UncertaintyScale(0), 6);
    }

    // ---- Determinism ----

    [Fact]
    public void Sweep_And_Ledger_AreDeterministic_AcrossIdenticalRuns()
    {
        var telescope = new TelescopeModel(baseRangeMeters: 2e11);
        var job = new ScanJob(CenterBearingRad: 0.3, ArcWidthRad: 40 * Math.PI / 180);
        var observer = new Vector2d(1e11, 5e10);

        var candidates = new (string Id, ShipState State)[]
        {
            ("a", new ShipState(new Vector2d(1.3e11, 6e10), new Vector2d(100, -50), 0)),
            ("b", new ShipState(new Vector2d(-2e11, 1e11), Vector2d.Zero, 0)),
            ("c", new ShipState(new Vector2d(1.2e11, 5.5e10), Vector2d.Zero, 0)),
        };

        IReadOnlyList<Observation> first = TrackingStation.Sweep(telescope, job, observer, candidates, 1000);
        IReadOnlyList<Observation> second = TrackingStation.Sweep(telescope, job, observer, candidates, 1000);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }

        var ledgerA = new TrackedTargetLedger(maxTracks: 4);
        var ledgerB = new TrackedTargetLedger(maxTracks: 4);
        foreach (Observation obs in first)
        {
            Assert.Equal(ledgerA.Add(obs), ledgerB.Add(obs));
        }

        Assert.Equal(ledgerA.Entries.Count, ledgerB.Entries.Count);
        foreach (TrackedTarget entry in ledgerA.Entries)
        {
            Assert.True(ledgerB.TryGet(entry.ShipId, out TrackedTarget other));
            Assert.Equal(entry, other);
        }
    }

    // ---- Scan programs ----

    [Fact]
    public void ScanPrograms_BuildsCorridorWatch_ForPresentTradeAnchors()
    {
        var ephemeris = Sol();
        var shipPosition = ephemeris.Position("earth", 0) + new Vector2d(1e9, 0);

        IReadOnlyList<ScanProgram> programs = ScanPrograms.BuildPrograms(ephemeris, shipPosition, 0);

        Assert.NotEmpty(programs);
        Assert.Contains(programs, p => p.Name.Contains("Saturn") && p.Name.Contains("Mars"));
        Assert.Contains(programs, p => p.Name.Contains("corridor watch"));
        Assert.All(programs, p => Assert.True(p.Job.ArcWidthRad is > 0 and <= Math.Tau));
    }

    [Fact]
    public void ScanPrograms_ArePureFunctionOfInputs()
    {
        var ephemeris = Sol();
        var shipPosition = new Vector2d(1.4e11, 2e10);

        IReadOnlyList<ScanProgram> first = ScanPrograms.BuildPrograms(ephemeris, shipPosition, 12345);
        IReadOnlyList<ScanProgram> second = ScanPrograms.BuildPrograms(ephemeris, shipPosition, 12345);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }
}
