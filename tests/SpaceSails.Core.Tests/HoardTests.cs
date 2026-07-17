namespace SpaceSails.Core.Tests;

/// <summary>#223 — buried treasure. The cache round-trip (bury→dig gives back the exact contents),
/// deterministic map text (bearing/paces reproduce from the seed), the confiscation seam (buried is
/// off the ship), the symmetric discovery roll (deterministic per seed), and rumour-map dig.</summary>
public class HoardTests
{
    private static List<CacheCargo> Cargo(params (string cls, int units, bool hot)[] lines) =>
        lines.Select(l => new CacheCargo(l.cls, l.units, l.hot)).ToList();

    // ---- Cache round-trip: bury the exact coin + cargo, dig it back unchanged ----

    [Fact]
    public void BuryThenDig_ReturnsExactContents()
    {
        var ledger = new CacheLedger();
        var cargo = Cargo(("He3", 4, true), ("Ice", 2, false));

        TreasureCache buried = ledger.Bury("phobos", coin: 1200, cargo, simTime: 5000, owner: "you", playerOwned: true);

        Assert.True(ledger.HasCacheAt("phobos"));
        TreasureCache? dug = ledger.Dig(buried.Id);

        Assert.NotNull(dug);
        Assert.Equal(1200, dug!.Value.Coin);
        Assert.Equal(6, dug.Value.TotalCargoUnits);       // 4 + 2
        Assert.Equal(4, dug.Value.HotCargoUnits);         // only the He3 was hot
        Assert.Equal("phobos", dug.Value.BodyId);
        Assert.Equal("you", dug.Value.Owner);
        Assert.False(ledger.HasCacheAt("phobos"));        // gone from the ledger once dug
        Assert.Null(ledger.Dig(buried.Id));               // and not diggable twice
    }

    [Fact]
    public void Bury_MintsTheMonolithLandmark_OnPhobos()
    {
        var ledger = new CacheLedger();
        TreasureCache c = ledger.Bury("phobos", 100, Cargo(), 0, "you", true);
        Assert.Equal(Landmarks.PhobosMonolith.Name, c.LandmarkName);
        Assert.Contains("PHOBOS", c.Caption("Phobos"));
        Assert.Contains(c.LandmarkName, c.Caption("Phobos"));
    }

    [Fact]
    public void Bury_UnknownBody_FallsBackToALandingBeacon()
    {
        var ledger = new CacheLedger();
        TreasureCache c = ledger.Bury("some-rock", 100, Cargo(), 0, "you", true);
        Assert.Equal("the landing beacon", c.LandmarkName);
    }

    // ---- Map text determinism: same seed → same bearing/paces, always ----

    [Fact]
    public void MapText_IsDeterministic_ForASeed()
    {
        string seed = CacheMint.SeedKey("phobos", "you", 5000.0, mintIndex: 3);
        Assert.Equal(CacheMint.Bearing(seed), CacheMint.Bearing(seed));
        Assert.Equal(CacheMint.Paces(seed), CacheMint.Paces(seed));
    }

    [Fact]
    public void MapText_Paces_StayInBand_AndBearingIsFromTheCompass()
    {
        for (int i = 0; i < 200; i++)
        {
            string seed = CacheMint.SeedKey("phobos", "you", i * 137.0, i);
            int paces = CacheMint.Paces(seed);
            Assert.InRange(paces, CacheMint.MinPaces, CacheMint.MaxPaces);
            Assert.Contains(CacheMint.Bearing(seed), CacheMint.Bearings);
        }
    }

    [Fact]
    public void MapText_DiffersAcrossSeeds()
    {
        // Not a hard guarantee for any two seeds, but across a spread the mint must vary both fields.
        var bearings = new HashSet<string>();
        var paces = new HashSet<int>();
        for (int i = 0; i < 50; i++)
        {
            string seed = CacheMint.SeedKey("phobos", "you", 0, i);
            bearings.Add(CacheMint.Bearing(seed));
            paces.Add(CacheMint.Paces(seed));
        }
        Assert.True(bearings.Count > 1);
        Assert.True(paces.Count > 1);
    }

    // ---- The confiscation seam: buried is off the ship ----

    [Fact]
    public void BuriedHotUnits_CountsOnlyOurBuriedStolenCargo()
    {
        var ledger = new CacheLedger();
        ledger.Bury("phobos", 0, Cargo(("He3", 5, true), ("Ice", 3, false)), 0, "you", playerOwned: true);
        ledger.Bury("phobos", 0, Cargo(("Alloys", 2, true)), 0, "you", playerOwned: true);
        // A rival's cache we merely hold the map to is not OUR evidence.
        ledger.Learn(new TreasureCache("npc-1", "phobos", "the monolith", "spinward", 20, 0,
            Cargo(("He3", 9, true)), 0, "Old Vane", PlayerOwned: false));

        Assert.Equal(7, ledger.BuriedHotUnits);  // 5 + 2, not the rival's 9
    }

    // ---- Discovery roll: deterministic per seed, symmetric risk ----

    [Fact]
    public void DiscoveryRoll_IsDeterministic_PerCacheAndPeriod()
    {
        Assert.Equal(DiscoveryRule.Roll("cache-you-0", 12), DiscoveryRule.Roll("cache-you-0", 12));
        // Different period or cache generally changes the roll.
        Assert.NotEqual(DiscoveryRule.Roll("cache-you-0", 12), DiscoveryRule.Roll("cache-you-0", 13));
    }

    [Fact]
    public void DiscoveryRoll_StaysInD100()
    {
        for (long p = 0; p < 500; p++)
        {
            int r = DiscoveryRule.Roll("cache-you-7", p);
            Assert.InRange(r, 1, 100);
        }
    }

    [Fact]
    public void DiscoveredWithin_ScansTheWholeSkippedSpan_Deterministically()
    {
        // Find a cache id that DOES get discovered within a wide span, then prove the scan finds the
        // same period every time (a warp that skips days can't skip a roll).
        string hit = Enumerable.Range(0, 1000).Select(i => $"cache-you-{i}")
            .First(id => DiscoveryRule.DiscoveredWithin(id, lastCheckedPeriod: -1, nowSimTime: 400 * DiscoveryRule.PeriodSeconds) is not null);

        long? a = DiscoveryRule.DiscoveredWithin(hit, -1, 400 * DiscoveryRule.PeriodSeconds);
        long? b = DiscoveryRule.DiscoveredWithin(hit, -1, 400 * DiscoveryRule.PeriodSeconds);
        Assert.Equal(a, b);
        Assert.NotNull(a);

        // Freshly buried (no elapsed period) is never found on day zero.
        Assert.Null(DiscoveryRule.DiscoveredWithin(hit, lastCheckedPeriod: DiscoveryRule.PeriodIndex(0), nowSimTime: 0));
    }

    // ---- Rumour map: modest, dice-priced, and diggable through the same path ----

    [Fact]
    public void RumorMap_IsDeterministic_ModestAndPricedAsAFraction()
    {
        RumorMaps.Rumor r1 = RumorMaps.Generate("the-space-bar|day-3");
        RumorMaps.Rumor r2 = RumorMaps.Generate("the-space-bar|day-3");

        Assert.Equal(r1.Cache.Id, r2.Cache.Id);
        Assert.Equal(r1.PriceCredits, r2.PriceCredits);
        Assert.False(r1.Cache.PlayerOwned);                    // it's someone else's hoard
        Assert.True(r1.Cache.Coin is >= 300 and <= 1500);      // modest
        Assert.True(r1.PriceCredits < r1.Cache.Coin);          // a wager that usually clears
        Assert.True(r1.PriceCredits >= 100);
    }

    [Fact]
    public void RumorMap_DigsThroughTheSameLedgerPath()
    {
        var ledger = new CacheLedger();
        RumorMaps.Rumor r = RumorMaps.Generate("the-tilt|day-9");

        ledger.Learn(r.Cache);
        Assert.True(ledger.HasCacheAt(r.Cache.BodyId));

        TreasureCache? dug = ledger.Dig(r.Cache.Id);
        Assert.NotNull(dug);
        Assert.Equal(r.Cache.Coin, dug!.Value.Coin);
        Assert.Equal(0, ledger.BuriedHotUnits);   // a rival's cache never counts as our evidence
    }

    // ---- Contents line + caption for the ledger / card ----

    [Fact]
    public void ContentsLine_ReadsCoinAndCargoWithHotCount()
    {
        var c = new TreasureCache("id", "phobos", "the monolith", "anti-spinward", 40, 1200,
            Cargo(("He3", 4, true), ("Ice", 2, false)), 0, "you", true);
        string line = c.ContentsLine();
        Assert.Contains("1,200 cr", line);
        Assert.Contains("6 units", line);
        Assert.Contains("4 hot", line);   // the He3 (4u) was hot; the Ice (2u) was not
        Assert.Equal("40 paces anti-spinward of the monolith", c.BearingLine);
    }
}
