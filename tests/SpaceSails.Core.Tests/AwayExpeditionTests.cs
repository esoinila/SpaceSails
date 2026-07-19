namespace SpaceSails.Core.Tests;

/// <summary>
/// #370 · THE AWAY EXPEDITION. Pins the pure Core spine of the away-team gig: the seeded site SPAWN
/// (deterministic, in shuttle range, drifting outward), the hold-in-range WINDOW clock, the diced on-site
/// EVENT table (cadence + outcome bands + consequences) and the PAYOUT composition. Determinism is law in
/// Core, so every roll here is reproducible from the folded seed.
/// </summary>
public class AwayExpeditionTests
{
    // ── Site spawn determinism ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Spawn_IsDeterministic_ForTheSameSeedAndState()
    {
        var pos = new Vector2d(1.2e11, -3.4e10);
        var vel = new Vector2d(12_000, -4_000);

        SiteSpawn a = ExpeditionSite.Spawn(4242, pos, vel, ExpeditionFlavor.Science);
        SiteSpawn b = ExpeditionSite.Spawn(4242, pos, vel, ExpeditionFlavor.Science);

        Assert.Equal(a, b); // record equality — position, velocity, kind, name all identical
    }

    [Fact]
    public void Spawn_PlacesTheRockComfortablyInsideShuttleRange()
    {
        var pos = new Vector2d(0, 0);
        SiteSpawn s = ExpeditionSite.Spawn(7, pos, Vector2d.Zero, ExpeditionFlavor.MiningSurvey);

        double distance = (s.Position - pos).Length;
        Assert.True(ShuttleRange.InRange(distance), "site must spawn within one shuttle hop");
        Assert.Equal(ExpeditionSite.SpawnFraction * ShuttleRange.RangeMeters, distance, 1e-3);
        // A fresh gig is instantly reachable — that is the whole point of the cheat/test loop.
        Assert.True(distance < ShuttleRange.RangeMeters, "must leave head-room before the range edge");
    }

    [Fact]
    public void Spawn_DriftsStraightOutward_SoAnUntendedShipOpensTheGap()
    {
        var pos = new Vector2d(5e10, 5e10);
        var vel = new Vector2d(3_000, -2_000);
        SiteSpawn s = ExpeditionSite.Spawn(99, pos, vel, ExpeditionFlavor.Science);

        Vector2d relPos = s.Position - pos;
        Vector2d relVel = s.Velocity - vel; // strip the ship's own motion — the drift alone
        double rate = ExpeditionWindow.RangeRate(relPos, relVel);

        // Drift is straight out along the offset, so the range-rate is the full drift speed (opening).
        Assert.Equal(ExpeditionSite.DriftSpeedMps, rate, 1e-6);
        Assert.True(rate > 0, "the gap must open when the ship does not match course");
    }

    [Fact]
    public void Spawn_DrawsDifferentGround_AcrossSeeds()
    {
        // The kind is seeded, so across many seeds every authored scheme should appear at least once.
        var kinds = new HashSet<ExpeditionSiteKind>();
        for (ulong seed = 0; seed < 60; seed++)
        {
            kinds.Add(ExpeditionSite.Spawn(seed, Vector2d.Zero, Vector2d.Zero, ExpeditionFlavor.Science).Kind);
        }

        Assert.Equal(3, kinds.Count); // ruins, crashed hull, sealed tunnel all reachable
    }

    [Fact]
    public void BodyId_RoundTrips_TheSiteKind_AndRoutesTheGround()
    {
        SurfaceLayout.Field f = TestField();
        foreach (ExpeditionSiteKind kind in Enum.GetValues<ExpeditionSiteKind>())
        {
            string id = ExpeditionSite.BodyIdFor(kind);
            Assert.True(ExpeditionSite.TryParseKind(id, out ExpeditionSiteKind back));
            Assert.Equal(kind, back);

            // SurfaceLayout.For routes the kind-encoded id to the SAME ground ForExpedition builds.
            Assert.Equal(
                SurfaceLayout.WallHash(SurfaceLayout.ForExpedition(kind, f)),
                SurfaceLayout.WallHash(SurfaceLayout.For(id, f)));
        }

        // Ordinary body ids are not expedition rocks — the parse falls through untouched.
        Assert.False(ExpeditionSite.TryParseKind("miranda", out _));
        Assert.False(ExpeditionSite.TryParseKind("expedition-site", out _)); // the bare family prefix is not a site
        Assert.False(ExpeditionSite.TryParseKind(null, out _));
    }

    // ── The hold-in-range window / clock math ───────────────────────────────────────────────────────

    [Fact]
    public void RangeRate_IsPositiveOpening_NegativeClosing_ZeroTangential()
    {
        var relPos = new Vector2d(1e8, 0);
        Assert.True(ExpeditionWindow.RangeRate(relPos, new Vector2d(500, 0)) > 0);   // moving away
        Assert.True(ExpeditionWindow.RangeRate(relPos, new Vector2d(-500, 0)) < 0);  // closing
        Assert.Equal(0.0, ExpeditionWindow.RangeRate(relPos, new Vector2d(0, 900)), 1e-9); // tangential
    }

    [Fact]
    public void TimeLeftInRange_TicksDownWhenOpening_HoldsWhenMatched()
    {
        double halfway = 0.5 * ExpeditionWindow.RangeMeters;

        // Opening at 2500 m/s from halfway to the edge → an honest, finite countdown.
        double left = ExpeditionWindow.TimeLeftInRangeSeconds(halfway, 2_500);
        Assert.Equal((ExpeditionWindow.RangeMeters - halfway) / 2_500, left, 1e-6);

        // Matched course (rate ≤ 0) → no clock at all.
        Assert.True(double.IsPositiveInfinity(ExpeditionWindow.TimeLeftInRangeSeconds(halfway, 0)));
        Assert.True(double.IsPositiveInfinity(ExpeditionWindow.TimeLeftInRangeSeconds(halfway, -800)));

        // Already past the edge → the window is lost (zero).
        Assert.Equal(0.0, ExpeditionWindow.TimeLeftInRangeSeconds(ExpeditionWindow.RangeMeters + 1, 2_500));
    }

    [Theory]
    [InlineData(0.5, 0.0, WindowStatus.Holding)]      // matched → holds
    [InlineData(0.5, -900.0, WindowStatus.Holding)]   // closing → holds
    [InlineData(0.5, 2_500.0, WindowStatus.Ticking)]  // opening with room → ticks
    [InlineData(0.9998, 2_500.0, WindowStatus.Critical)] // opening, almost out (~40 s) → last call
    [InlineData(1.01, 2_500.0, WindowStatus.Lost)]    // past the edge → stranded
    public void Classify_ReadsTheWindowStatus(double distanceFraction, double rate, WindowStatus expected)
    {
        double distance = distanceFraction * ExpeditionWindow.RangeMeters;
        Assert.Equal(expected, ExpeditionWindow.Classify(distance, rate, ExpeditionWindow.DefaultCriticalSeconds));
    }

    [Fact]
    public void OnSiteBudget_CountsDownAndFloorsAtZero()
    {
        Assert.Equal(200.0, ExpeditionWindow.OnSiteRemainingSeconds(300, 100), 1e-9);
        Assert.Equal(0.0, ExpeditionWindow.OnSiteRemainingSeconds(300, 400), 1e-9); // never negative
    }

    [Fact]
    public void EffectiveClock_TakesTheTighterOfBudgetAndGeometry()
    {
        // Budget 120 s left, geometry window 40 s → the geometry strands you first.
        Assert.Equal(40.0, ExpeditionWindow.EffectiveClockSeconds(120, 40), 1e-9);
        // Held geometry (infinite) → the contracted budget rules.
        Assert.Equal(120.0, ExpeditionWindow.EffectiveClockSeconds(120, double.PositiveInfinity), 1e-9);
    }

    // ── Event cadence + outcome bands ───────────────────────────────────────────────────────────────

    [Fact]
    public void EpisodesElapsed_IsOnePerCadence()
    {
        double c = AwayExpeditionEvents.EventCadenceSeconds;
        Assert.Equal(0, AwayExpeditionEvents.EpisodesElapsed(0));
        Assert.Equal(0, AwayExpeditionEvents.EpisodesElapsed(c - 0.01));
        Assert.Equal(1, AwayExpeditionEvents.EpisodesElapsed(c));
        Assert.Equal(3, AwayExpeditionEvents.EpisodesElapsed((3 * c) + 5));
    }

    [Fact]
    public void Roll_IsDeterministic_ForTheSameSeed()
    {
        ulong seed = AwayExpeditionEvents.Seed(1000, ExpeditionSite.BodyId, 4);
        ExpeditionEpisode a = AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, 4);
        ExpeditionEpisode b = AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, 4);
        Assert.Equal(a.Outcome, b.Outcome);
        Assert.Equal(a.BonusCredits, b.BonusCredits);
        Assert.Equal(a.NerveHit, b.NerveHit);
        Assert.Equal(a.Event.Total, b.Event.Total);
    }

    [Fact]
    public void Roll_AllBandsShowTheirDiceAndTagTheSource()
    {
        // Sweep many ordinals; every episode carries a shown 2D6 and the EXPEDITION tag.
        for (int ord = 0; ord < 200; ord++)
        {
            ulong seed = AwayExpeditionEvents.Seed(500, "expedition-site", ord);
            ExpeditionEpisode ep = AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, ord);
            Assert.Equal(AwayExpeditionEvents.Source, ep.Event.Source);
            Assert.Equal(2, ep.Event.Faces.Count);          // 2D6 — the cast dice are shown
            Assert.False(string.IsNullOrWhiteSpace(ep.Event.Headline));
        }
    }

    [Fact]
    public void Roll_OverManyBeats_CoversEveryOutcomeBand()
    {
        var seen = new HashSet<ExpeditionOutcome>();
        for (int ord = 0; ord < 400; ord++)
        {
            ulong seed = AwayExpeditionEvents.Seed(12345, "expedition-site", ord);
            seen.Add(AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, ord).Outcome);
        }

        Assert.Contains(ExpeditionOutcome.Nothing, seen);
        Assert.Contains(ExpeditionOutcome.Discovery, seen);
        Assert.Contains(ExpeditionOutcome.NerveFray, seen);
        Assert.Contains(ExpeditionOutcome.ScientistBolts, seen);
        Assert.Contains(ExpeditionOutcome.Horror, seen);
    }

    [Fact]
    public void Roll_Consequences_MatchTheirBand()
    {
        for (int ord = 0; ord < 400; ord++)
        {
            ulong seed = AwayExpeditionEvents.Seed(777, "expedition-site", ord);
            ExpeditionEpisode ep = AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.MiningSurvey, ord);

            switch (ep.Outcome)
            {
                case ExpeditionOutcome.Discovery:
                case ExpeditionOutcome.MajorDiscovery:
                    Assert.True(ep.BonusCredits > 0, "a find pays a bonus");
                    Assert.Equal(0, ep.HostilePack);
                    Assert.False(ep.ScientistLost);
                    break;
                case ExpeditionOutcome.Horror:
                    Assert.True(ep.NerveHit > 0, "the horror costs nerve");
                    Assert.InRange(ep.HostilePack, 3, 5); // a LIMITED pack only — never the endless tide
                    break;
                case ExpeditionOutcome.ScientistBolts:
                    Assert.True(ep.NerveHit > 0);
                    Assert.Equal(0, ep.HostilePack); // a bolt is not a swarm
                    break;
                case ExpeditionOutcome.NerveFray:
                    Assert.True(ep.NerveHit > 0);
                    Assert.Equal(0, ep.BonusCredits);
                    break;
                case ExpeditionOutcome.Nothing:
                    Assert.Equal(0, ep.BonusCredits);
                    Assert.Equal(0.0, ep.NerveHit);
                    Assert.Equal(0, ep.HostilePack);
                    Assert.False(ep.ScientistLost);
                    break;
            }
        }
    }

    [Fact]
    public void Roll_HostilePack_IsAlwaysLimited_NeverAStream()
    {
        // No episode, in any flavor, ever rouses more than a small pack (the owner's "NO endless tide").
        for (int ord = 0; ord < 500; ord++)
        {
            ulong s1 = AwayExpeditionEvents.Seed(1, "expedition-site", ord);
            ulong s2 = AwayExpeditionEvents.Seed(2, "expedition-site", ord);
            Assert.True(AwayExpeditionEvents.Roll(s1, ExpeditionFlavor.Science, ord).HostilePack <= 5);
            Assert.True(AwayExpeditionEvents.Roll(s2, ExpeditionFlavor.MiningSurvey, ord).HostilePack <= 5);
        }
    }

    [Fact]
    public void Roll_MiningSurvey_SkewsWarier_ThanScience()
    {
        // The −1 "reads the rock warily" modifier tilts mining toward the low (scare) bands, so across the
        // same seeds it should suffer at least as many nerve-costing beats as the science team.
        int scienceScares = 0, miningScares = 0;
        for (int ord = 0; ord < 400; ord++)
        {
            ulong seed = AwayExpeditionEvents.Seed(20260719, "expedition-site", ord);
            if (AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.Science, ord).NerveHit > 0) scienceScares++;
            if (AwayExpeditionEvents.Roll(seed, ExpeditionFlavor.MiningSurvey, ord).NerveHit > 0) miningScares++;
        }

        Assert.True(miningScares >= scienceScares,
            $"mining ({miningScares}) should skew at least as scary as science ({scienceScares})");
    }

    // ── Payout composition ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reward_LocalGig_PaysAboutTheFatBase()
    {
        // A local rock (both radii ~1.5 AU) carries almost no haul premium → ≈ the base fee, no bonuses.
        double r = HaulReward.AstronomicalUnitMeters * 1.5;
        int pay = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, discoveryBonus: 0, scientistsLost: 0);
        Assert.Equal(HaulReward.WithFloor(ExpeditionReward.BaseFee, r, r), pay);
        Assert.True(pay >= ExpeditionReward.BaseFee);
    }

    [Fact]
    public void Reward_AddsDiscoveries_AndDocksLostScientists()
    {
        double r = HaulReward.AstronomicalUnitMeters * 1.5;
        int baseline = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, 0, 0);

        int withFind = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, discoveryBonus: 2500, scientistsLost: 0);
        Assert.Equal(baseline + 2500, withFind);

        int withLoss = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, discoveryBonus: 0, scientistsLost: 1);
        Assert.Equal(baseline - ExpeditionReward.PerScientistLostPenalty, withLoss);
    }

    [Fact]
    public void Reward_NeverPaysBelowTheFloor_HoweverBadlyItWent()
    {
        double r = HaulReward.AstronomicalUnitMeters * 1.5;
        int pay = ExpeditionReward.Total(ExpeditionReward.BaseFee, r, r, discoveryBonus: 0, scientistsLost: 99);
        Assert.Equal(ExpeditionReward.Floor, pay);
    }

    [Fact]
    public void Reward_FartherSurvey_EarnsTheDistanceOnTop()
    {
        double inner = HaulReward.AstronomicalUnitMeters * 1.5;   // struck at Mars-ish
        double outer = HaulReward.AstronomicalUnitMeters * 19.0;  // dragged to Uranus-ish
        int local = ExpeditionReward.Total(ExpeditionReward.BaseFee, inner, inner, 0, 0);
        int hauled = ExpeditionReward.Total(ExpeditionReward.BaseFee, inner, outer, 0, 0);
        Assert.True(hauled > local, "a survey dragged out earns the haul distance on top of the fat base");
    }

    // ── The special surface schemes ─────────────────────────────────────────────────────────────────

    private static SurfaceLayout.Field TestField() =>
        new(LeftX: -44, RightX: 34, TopY: -20, BottomY: -84, LandingBandY: -27, AnchorX: -6, AnchorY: -70);

    [Fact]
    public void ExpeditionSchemes_AreDistinct_FromEachOtherAndFromMiranda()
    {
        SurfaceLayout.Field f = TestField();
        long ruins = SurfaceLayout.WallHash(SurfaceLayout.ForExpedition(ExpeditionSiteKind.MysticalRuins, f));
        long hull = SurfaceLayout.WallHash(SurfaceLayout.ForExpedition(ExpeditionSiteKind.CrashedHull, f));
        long tunnel = SurfaceLayout.WallHash(SurfaceLayout.ForExpedition(ExpeditionSiteKind.SealedTunnel, f));
        long miranda = SurfaceLayout.WallHash(SurfaceLayout.For("miranda", f));

        var all = new[] { ruins, hull, tunnel, miranda };
        Assert.Equal(all.Length, all.Distinct().Count()); // every ground is its own layout
    }

    [Fact]
    public void ExpeditionSchemes_AreDeterministic_AndNameTheirGround()
    {
        SurfaceLayout.Field f = TestField();
        foreach (ExpeditionSiteKind kind in Enum.GetValues<ExpeditionSiteKind>())
        {
            SurfaceLayout.Plan a = SurfaceLayout.ForExpedition(kind, f);
            SurfaceLayout.Plan b = SurfaceLayout.ForExpedition(kind, f);
            Assert.Equal(SurfaceLayout.WallHash(a), SurfaceLayout.WallHash(b));
            Assert.NotEmpty(a.Walls);
            Assert.False(string.IsNullOrWhiteSpace(a.Scheme));
            Assert.NotEmpty(a.Landmarks);
        }
    }

    [Fact]
    public void ExpeditionSchemes_KeepTheEdgeLanesOpen_SoAWayDownAlwaysExists()
    {
        SurfaceLayout.Field f = TestField();
        foreach (ExpeditionSiteKind kind in Enum.GetValues<ExpeditionSiteKind>())
        {
            SurfaceLayout.Plan plan = SurfaceLayout.ForExpedition(kind, f);
            foreach (SurfaceLayout.Wall w in plan.Walls)
            {
                // No feature intrudes on the kept-open far-edge lanes (the pathability guarantee).
                Assert.True(w.X1 >= f.LeftX + SurfaceLayout.EdgeMargin - 1e-6, "wall crosses the left lane");
                Assert.True(w.X2 <= f.RightX - SurfaceLayout.EdgeMargin + 1e-6, "wall crosses the right lane");
            }
        }
    }

    // ── The accepted-mission record ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Plan_DescribesTheGig_InTheHouseVoice()
    {
        var science = new ExpeditionPlan(ExpeditionFlavor.Science, ExpeditionSiteKind.MysticalRuins,
            ExpeditionSite.BodyId, "The Drifter ruins", TeamSize: 4, BaseFee: 6000, AcceptedSimTime: 0);
        var mining = science with { Flavor = ExpeditionFlavor.MiningSurvey };

        Assert.Contains("scientists", science.Describe());
        Assert.Contains("The Drifter ruins", science.Describe());
        Assert.Contains("survey crew", mining.Describe());
    }
}
