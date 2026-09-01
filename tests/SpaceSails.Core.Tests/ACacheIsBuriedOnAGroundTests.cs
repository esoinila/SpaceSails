namespace SpaceSails.Core.Tests;

/// <summary>
/// #650 · A CACHE IS BURIED ON A GROUND, NOT ON A MOON.
///
/// <para>Found in the surface story-QA pass (#648): a chest stored its body and its real dug coordinates but
/// never WHICH of the body's landing sites the shovel went in at. Since #320 a body offers 2–4 seeded sites
/// and every one of them rebuilds the same local surface coordinate frame — so a chest buried out on the Wild
/// Plain drew its ✗, and dug back out, at the identical x/y on the Ridge Camp: ground the captain had never
/// walked, and quite possibly a spot the generator had put a rille or a hut on.</para>
///
/// <para>Owner ruling (2026-09-01), <b>Option A — a cache belongs to a SITE</b>: the site rides the cache as a
/// NULLABLE ordinal, so every save written before the field existed loads and re-saves as body-wide exactly as
/// today; the ✗ filters on it; and the map card — which the ledger row reads too — names the ground, so a
/// captain standing on the wrong site is told which one to go back to.</para>
/// </summary>
public class ACacheIsBuriedOnAGroundTests
{
    // Phobos offers four sites and site 2 IS "The Ridge Camp" — the ruling's own example ground. Site 0 is
    // always the Wild Plain on the body's canon layout, so these two are as far apart as two grounds get.
    private const string Body = "phobos";
    private const int WildPlain = 0;
    private const int RidgeCamp = 2;

    /// <summary>THE TWO GROUNDS ARE ACTUALLY DIFFERENT PLACES. Stated first and separately because everything
    /// below is worthless without it: a "the ✗ is not on the wrong site" assertion passes gloriously if the
    /// two sites are the same ground, or if neither exists. Named, seeded, and demonstrably distinct walls.</summary>
    [Fact]
    public void TheTwoGroundsAreDifferentGround()
    {
        LandingSite plain = LandingSites.At(Body, WildPlain);
        LandingSite ridge = LandingSites.At(Body, RidgeCamp);

        Assert.Equal("The Wild Plain", plain.Name);
        Assert.Equal("The Ridge Camp", ridge.Name);
        Assert.NotEqual(plain.LayoutSalt, ridge.LayoutSalt);

        // The mechanical difference, not just the label: the salts seed visibly different deck plans.
        long plainWalls = SurfaceLayout.WallHash(SurfaceLayout.For(Body, SurfaceLayout.DefaultField, plain.LayoutSalt));
        long ridgeWalls = SurfaceLayout.WallHash(SurfaceLayout.For(Body, SurfaceLayout.DefaultField, ridge.LayoutSalt));
        Assert.NotEqual(plainWalls, ridgeWalls);
    }

    /// <summary>THE BUG ITSELF. A chest dug on the Ridge Camp is diggable from the Ridge Camp and from NOWHERE
    /// ELSE on Phobos. Both directions asserted — present on the right ground, absent on the wrong one — so the
    /// guard cannot go green by finding nothing anywhere.</summary>
    [Fact]
    public void ACacheDugOnOneSiteIsFoundOnThatSiteAndNotOnTheOther()
    {
        var ledger = new CacheLedger();
        TreasureCache chest = ledger.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true,
            reeverLevel: 0, digX: 4.5, digY: -30.0, siteIndex: RidgeCamp);

        Assert.True(chest.HasSite);
        Assert.Equal(RidgeCamp, chest.SiteIndex);

        // On the ground it is under: there.
        Assert.Equal([chest.Id], ledger.CachesAt(Body, RidgeCamp).Select(c => c.Id));
        // On the other ground of the same moon: nothing. This is the ✗ that used to lie.
        Assert.Empty(ledger.CachesAt(Body, WildPlain));
        // And the body-wide read — the ledger's 🗺 section, the destination board's hint — still sees it,
        // because a captain in orbit is owed every chest on the moon.
        Assert.Equal([chest.Id], ledger.CachesAt(Body).Select(c => c.Id));
    }

    /// <summary>EVERY CHEST ALREADY IN THE GROUND KEEPS ITS OLD DEAL. A cache minted without a site (a save from
    /// before #650, a bought rumour map) answers for every ground on its body — the exact behaviour it has today.
    /// Nothing a captain buried last week becomes undiggable because we changed our mind about what a cache is.</summary>
    [Fact]
    public void ABodyWideCacheIsStillDiggableFromEveryGround()
    {
        var ledger = new CacheLedger();
        TreasureCache old = ledger.Bury(Body, coin: 500, [], simTime: 1000, owner: "you", playerOwned: true);

        Assert.False(old.HasSite);
        Assert.Null(old.SiteIndex);

        for (int site = 0; site < LandingSites.Count(Body); site++)
        {
            Assert.Equal([old.Id], ledger.CachesAt(Body, site).Select(c => c.Id));
        }
    }

    /// <summary>A site NEVER reaches across bodies. Same ordinal, different moon, still not our ground.</summary>
    [Fact]
    public void TheSiteNeverMakesACacheReachAcrossBodies()
    {
        var ledger = new CacheLedger();
        ledger.Bury("miranda", coin: 200, [], simTime: 900, owner: "you", playerOwned: true, siteIndex: RidgeCamp);

        Assert.Single(ledger.CachesAt("miranda", RidgeCamp));
        Assert.Empty(ledger.CachesAt(Body, RidgeCamp));
    }

    /// <summary>THE CARD NAMES THE GROUND (ruling 4), in the owner's own format: body · SITE — the existing card
    /// text. This string is read twice — on the full-screen map card and on the ledger's 🗺 row — which is how a
    /// captain who lands on the wrong site learns where to go back to (ruling 5).</summary>
    [Fact]
    public void TheMapCardNamesTheGroundItIsBuriedOn()
    {
        var ledger = new CacheLedger();
        TreasureCache chest = ledger.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true, siteIndex: RidgeCamp);

        Assert.Equal("The Ridge Camp", chest.SiteName);
        Assert.Equal(
            $"PHOBOS · THE RIDGE CAMP — from {chest.LandmarkName}, {chest.Paces} paces {chest.Bearing}",
            chest.Caption("Phobos"));
    }

    /// <summary>…and a body-wide chest keeps the ORIGINAL caption, with no stray separator where a site would be.
    /// The old text is not a fallback we tolerate; it is the honest caption for a chest that genuinely is buried
    /// on a moon rather than at a place.</summary>
    [Fact]
    public void ABodyWideCardKeepsTheOriginalCaption()
    {
        var ledger = new CacheLedger();
        TreasureCache old = ledger.Bury(Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true);

        Assert.Null(old.SiteName);
        Assert.Equal(
            $"PHOBOS — from {old.LandmarkName}, {old.Paces} paces {old.Bearing}",
            old.Caption("Phobos"));
        Assert.DoesNotContain(" · ", old.Caption("Phobos"), StringComparison.Ordinal);
    }

    /// <summary>The site survives the vault, or the whole thing is a lie the moment the captain saves.</summary>
    [Fact]
    public void TheGroundSurvivesTheVault()
    {
        var ledger = new CacheLedger();
        ledger.Bury(Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true,
            reeverLevel: 1, digX: 4.5, digY: -30.0, siteIndex: RidgeCamp);

        var restored = new CacheLedger();
        VaultMapper.Apply(VaultMapper.ToSection(ledger), restored);

        TreasureCache c = restored.Caches.Single();
        Assert.Equal(RidgeCamp, c.SiteIndex);
        Assert.Equal("The Ridge Camp", c.SiteName);
        Assert.Single(restored.CachesAt(Body, RidgeCamp));
        Assert.Empty(restored.CachesAt(Body, WildPlain));
    }

    /// <summary>A vault record written before the field existed loads with no site — and therefore body-wide.</summary>
    [Fact]
    public void AVaultRecordWithoutASiteLoadsBodyWide()
    {
        var section = new CachesSection
        {
            NextMintIndex = 1,
            Caches =
            [
                new CacheRecord
                {
                    Id = "cache-you-0", BodyId = Body, LandmarkName = "the monolith",
                    Bearing = "sunward", Paces = 40, Coin = 900, Owner = "you", PlayerOwned = true,
                    // SiteIndex deliberately unset — the old shape.
                },
            ],
        };

        var restored = new CacheLedger();
        VaultMapper.Apply(section, restored);

        TreasureCache c = restored.Caches.Single();
        Assert.Null(c.SiteIndex);
        Assert.False(c.HasSite);
        Assert.Single(restored.CachesAt(Body, WildPlain));
        Assert.Single(restored.CachesAt(Body, RidgeCamp));
    }

    /// <summary>
    /// THE SAVE-COMPAT PROOF, in bytes. <c>LegacyVault</c> below is not a hand-written fixture: it is the exact
    /// output of <see cref="VaultSerializer.Save"/> on the build immediately before #650, checksum and all. This
    /// build must load it and write it back CHARACTER FOR CHARACTER — same keys, same order, same digest.
    ///
    /// <para>That is a stronger claim than "the field defaults to null", and it is the reason
    /// <c>CacheRecord.SiteIndex</c> carries <c>JsonIgnore(WhenWritingNull)</c> while <c>DigX</c>/<c>DigY</c> do
    /// not: the checksum is taken over the payload, so an extra <c>"siteIndex": null</c> per chest would change
    /// the digest of every hoard ever saved and the game would open each one with the 📛 tampered marker on the
    /// captain's own honest voyage.</para>
    /// </summary>
    [Fact]
    public void ALegacyVaultRoundTripsByteForByte()
    {
        // The writer indents with the platform's newline, so the fixture is read in the platform's newline
        // too — the only normalization, and it is the checked-out file's line endings, not anything of ours.
        string legacy = LegacyVault.ReplaceLineEndings();

        Vault loaded = VaultSerializer.Load(legacy);

        Assert.False(loaded.Tampered);                       // the digest still validates as written
        Assert.NotNull(loaded.Caches);
        Assert.Equal(2, loaded.Caches!.Caches.Count);
        Assert.All(loaded.Caches.Caches, r => Assert.Null(r.SiteIndex));  // every legacy chest is body-wide

        string rewritten = VaultSerializer.Save(loaded);
        Assert.Equal(legacy, rewritten);
        Assert.DoesNotContain("siteIndex", rewritten, StringComparison.Ordinal);
    }

    // Captured from the build at 5e9ab05 (pre-#650): two chests, one ours with a real dug spot and haunted
    // ground, one a rumour map's. Verbatim — do not reformat, the whole point is the bytes.
    private const string LegacyVault = """
        {
          "version": 1,
          "savedSimTime": 90000,
          "sections": {
            "caches": {
              "nextMintIndex": 2,
              "lastCheckedPeriod": -1,
              "caches": [
                {
                  "id": "cache-npc-1",
                  "bodyId": "miranda",
                  "landmarkName": "the landing beacon",
                  "bearing": "spinward",
                  "paces": 17,
                  "coin": 400,
                  "cargo": [],
                  "buriedSimTime": 12000,
                  "owner": "Old Vane",
                  "playerOwned": false,
                  "reeverLevel": 0,
                  "digX": null,
                  "digY": null
                },
                {
                  "id": "cache-you-0",
                  "bodyId": "phobos",
                  "landmarkName": "the monolith",
                  "bearing": "up the ridgeline",
                  "paces": 25,
                  "coin": 1800,
                  "cargo": [
                    {
                      "cargoClass": "He3",
                      "units": 3,
                      "hot": true
                    }
                  ],
                  "buriedSimTime": 61234.5,
                  "owner": "you",
                  "playerOwned": true,
                  "reeverLevel": 2,
                  "digX": -6.5,
                  "digY": -71.25
                }
              ]
            }
          },
          "checksum": "5ae91fdcec199803750bf9f3ffc12e4ca61c7200e7147599581aa6f7f6ac78f0"
        }
        """;
}
