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
}
