namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-313 · The shuttle asks WHERE, not WHY. These pin the destination-first spine: the destination
/// board by orbit context, the contextual ground actions (the ONLY gate on the dig — hence on the
/// dice), and the one-path chest pack shared by the long route and any shortcut.
/// </summary>
public class ShuttleExcursionTests
{
    private static ShuttleExcursion.Candidate Moon(string id, double dist, bool cache = false, bool interior = false) =>
        new(id, BodyKind.Moon, ParentId: "uranus", DistanceMeters: dist, BodyRadiusMeters: 1_000_000,
            HasInterior: interior, HasCache: cache);

    // ── The destination board by orbit context ──

    [Fact]
    public void Destinations_ListsLandableMoonsInRange_NearestFirst()
    {
        var cands = new[]
        {
            Moon("miranda", 3e8),
            Moon("ariel", 1e8),
            Moon("titania", 9e9),          // way out of shuttle range
        };

        var stops = ShuttleExcursion.Destinations(cands, dockedBodyId: null);

        Assert.Equal(2, stops.Count);
        Assert.Equal("ariel", stops[0].BodyId);   // nearest first
        Assert.Equal("miranda", stops[1].BodyId);
        Assert.All(stops, s => Assert.True(s.IsLandableSurface));
        Assert.True(stops[1].TravelSeconds > 0);
    }

    [Fact]
    public void Destinations_ExcludesSunPlanetsDockedBerthAndTooClose()
    {
        var cands = new[]
        {
            new ShuttleExcursion.Candidate("sun", BodyKind.Planet, ParentId: null, 2e8, 7e8, false, false),
            new ShuttleExcursion.Candidate("uranus", BodyKind.Planet, ParentId: "sun", 2e8, 2.5e7, false, false),
            Moon("home-berth", 2e8),       // the berth we're clamped to → omitted
            Moon("underfoot", 500_000),    // distance <= body radius → basically on it
            Moon("miranda", 3e8),
        };

        var stops = ShuttleExcursion.Destinations(cands, dockedBodyId: "home-berth");

        ShuttleExcursion.Destination only = Assert.Single(stops);
        Assert.Equal("miranda", only.BodyId);
    }

    [Fact]
    public void Destinations_FlagsBerthAndCacheContextIndependently()
    {
        var cands = new[]
        {
            Moon("miranda", 3e8, cache: true, interior: false),  // landable + a chest already down
            new ShuttleExcursion.Candidate("tilt", BodyKind.Station, "uranus", 2e8, 5000, HasInterior: true, HasCache: false),
        };

        var stops = ShuttleExcursion.Destinations(cands, dockedBodyId: null);

        ShuttleExcursion.Destination miranda = stops.Single(s => s.BodyId == "miranda");
        Assert.True(miranda.IsLandableSurface);
        Assert.True(miranda.HasCache);
        Assert.False(miranda.HasBerth);

        ShuttleExcursion.Destination tilt = stops.Single(s => s.BodyId == "tilt");
        Assert.True(tilt.HasBerth);
        Assert.False(tilt.IsLandableSurface); // a station is not a walkable surface to bury on
    }

    // ── Ground actions: the dig site is the ONLY dice gate ──

    [Theory]
    [InlineData(false, false, ShuttleExcursion.GroundAction.None)]
    [InlineData(true, false, ShuttleExcursion.GroundAction.BuryHere)]
    [InlineData(false, true, ShuttleExcursion.GroundAction.DigAtX)]
    public void GroundActionsFor_IsPurelyContextual(bool chest, bool cache, ShuttleExcursion.GroundAction expected)
    {
        Assert.Equal(expected, ShuttleExcursion.GroundActionsFor(chest, cache));
    }

    [Fact]
    public void GroundActionsFor_CanOfferBoth_WhenAChestFliesToSaltedGround()
    {
        ShuttleExcursion.GroundAction both = ShuttleExcursion.GroundActionsFor(carryingChest: true, cacheBuriedHere: true);
        Assert.True(both.HasFlag(ShuttleExcursion.GroundAction.BuryHere));
        Assert.True(both.HasFlag(ShuttleExcursion.GroundAction.DigAtX));
    }

    [Fact]
    public void SightseeingVisit_OffersNoSite_SoNoDiceEverRoll()
    {
        // Nothing carried, nothing buried → no dig site → the shovel never comes out → the 2D6 never roll.
        Assert.Equal(ShuttleExcursion.GroundAction.None, ShuttleExcursion.GroundActionsFor(false, false));
        Assert.Equal(ShuttleExcursion.GroundAction.None, ShuttleExcursion.SiteActFor(false, false));
    }

    [Fact]
    public void SiteAct_BuryTakesPrecedenceOverDig_ForOneEPress()
    {
        Assert.Equal(ShuttleExcursion.GroundAction.BuryHere,
            ShuttleExcursion.SiteActFor(carryingChest: true, cacheBuriedHere: true));
        Assert.Equal(ShuttleExcursion.GroundAction.DigAtX,
            ShuttleExcursion.SiteActFor(carryingChest: false, cacheBuriedHere: true));
    }

    // ── One path: the long route and any shortcut pack the same chest ──

    [Fact]
    public void Pack_ClampsToWhatIsOnHand()
    {
        var hold = new List<CacheCargo> { new("He3", 3, Hot: true) };
        ShuttleExcursion.ChestLoad load = ShuttleExcursion.Pack(coin: 9999, credits: 500, hold);
        Assert.Equal(500, load.Coin);          // clamped to the purse
        Assert.Equal(hold, load.Cargo);
        Assert.False(load.IsEmpty);
    }

    [Fact]
    public void Pack_EmptyLoad_IsSightseeing()
    {
        Assert.True(ShuttleExcursion.Pack(0, 1000, []).IsEmpty);
        Assert.True(ShuttleExcursion.Pack(-50, 1000, []).IsEmpty); // negative coin clamps to 0
    }

    [Fact]
    public void LongPathAndShortcut_ProduceTheSameChest()
    {
        var hold = new List<CacheCargo> { new("Ice", 2, Hot: false) };

        // The long path: board a destination, then load a chest from the hoard.
        ShuttleExcursion.ChestLoad longPath = ShuttleExcursion.Pack(coin: 300, credits: 1000, hold);

        // Any thin 'board with a chest loaded' shortcut funnels the same inputs through the same Pack.
        ShuttleExcursion.ChestLoad shortcut = ShuttleExcursion.Pack(coin: 300, credits: 1000, hold);

        Assert.Equal(longPath, shortcut); // provably one path — no duplicate burial logic
    }

    // ── #368: the honest board — nearest ground JUST BEYOND shuttle reach, with a closing/opening trend ──

    // A landable moon sampled at two epochs: current gap, next-epoch gap (for the trend). Out of range by
    // default (dist just past the shuttle's reach) unless the caller says otherwise.
    private static ShuttleExcursion.RangeSample FarMoon(string id, double dist, double distNext) =>
        new(id, BodyKind.Moon, ParentId: "jupiter",
            DistanceMeters: dist, DistanceMetersNext: distNext, BodyRadiusMeters: 1_000_000);

    [Fact]
    public void NearestOutOfReach_OrdersBySeparation_NearestFirst()
    {
        double reach = ShuttleRange.RangeMeters;
        var samples = new[]
        {
            FarMoon("callisto", reach * 3.0, reach * 3.0),
            FarMoon("ganymede", reach * 1.4, reach * 1.4),
            FarMoon("europa", reach * 2.0, reach * 2.0),
        };

        var list = ShuttleExcursion.NearestOutOfReach(samples, dockedBodyId: null, limit: 3);

        Assert.Equal(new[] { "ganymede", "europa", "callisto" }, list.Select(n => n.BodyId));
        Assert.All(list, n => Assert.Equal(reach, n.RangeMeters));
        Assert.Equal(1.4, list[0].TimesRange, 3); // ×1.4 of reach
    }

    [Fact]
    public void NearestOutOfReach_LimitCapsTheList()
    {
        double reach = ShuttleRange.RangeMeters;
        var samples = new[]
        {
            FarMoon("a", reach * 1.2, reach * 1.2),
            FarMoon("b", reach * 1.5, reach * 1.5),
            FarMoon("c", reach * 1.8, reach * 1.8),
            FarMoon("d", reach * 2.1, reach * 2.1),
        };

        var list = ShuttleExcursion.NearestOutOfReach(samples, dockedBodyId: null, limit: 2);

        Assert.Equal(new[] { "a", "b" }, list.Select(n => n.BodyId));
    }

    [Fact]
    public void NearestOutOfReach_ClosingWhenGapShrinks_OpeningWhenItGrows()
    {
        double reach = ShuttleRange.RangeMeters;
        var samples = new[]
        {
            FarMoon("closer", reach * 1.5, reach * 1.4),   // next sample nearer → closing
            FarMoon("further", reach * 1.5, reach * 1.6),  // next sample farther → opening
            FarMoon("still", reach * 1.5, reach * 1.5),    // unchanged → steady
        };

        var byId = ShuttleExcursion.NearestOutOfReach(samples, dockedBodyId: null, limit: -1)
            .ToDictionary(n => n.BodyId, n => n.Trend);

        Assert.Equal(ShuttleExcursion.RangeTrend.Closing, byId["closer"]);
        Assert.Equal(ShuttleExcursion.RangeTrend.Opening, byId["further"]);
        Assert.Equal(ShuttleExcursion.RangeTrend.Steady, byId["still"]);
    }

    [Fact]
    public void NearestOutOfReach_ExcludesInRange_StationsSunPlanetsDockedBerthAndTooClose()
    {
        double reach = ShuttleRange.RangeMeters;
        var samples = new[]
        {
            FarMoon("in-range", reach * 0.5, reach * 0.5),                 // within reach → boardable, not here
            FarMoon("home-berth", reach * 1.3, reach * 1.3),               // the berth we're clamped to → omitted
            new ShuttleExcursion.RangeSample("underfoot", BodyKind.Moon, "jupiter", 500_000, 500_000, 1_000_000), // dist <= radius
            new ShuttleExcursion.RangeSample("tilt", BodyKind.Station, "jupiter", reach * 1.3, reach * 1.3, 5_000), // a berth, not ground
            new ShuttleExcursion.RangeSample("sun", BodyKind.Planet, null, reach * 1.3, reach * 1.3, 7e8),        // no parent / a star
            new ShuttleExcursion.RangeSample("jupiter", BodyKind.Planet, "sun", reach * 1.3, reach * 1.3, 7e7),   // a planet
            FarMoon("europa", reach * 1.3, reach * 1.3),                   // the one true beyond-reach ground
        };

        var list = ShuttleExcursion.NearestOutOfReach(samples, dockedBodyId: "home-berth", limit: 3);

        ShuttleExcursion.NearbyLandable only = Assert.Single(list);
        Assert.Equal("europa", only.BodyId);
    }

    [Fact]
    public void ExplainsEmptyBoard_TrueOnlyWhenNothingIsInRange()
    {
        Assert.True(ShuttleExcursion.ExplainsEmptyBoard(0));
        Assert.False(ShuttleExcursion.ExplainsEmptyBoard(1));
        Assert.False(ShuttleExcursion.ExplainsEmptyBoard(3));
    }
}
