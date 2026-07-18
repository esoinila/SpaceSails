namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-295 · The 2D6 Reevers — the free watchdogs of a buried stash. They roll on the ONE shared
/// <see cref="DiceRule"/> engine (as two d6), never touch the loot, and stop dead at the crew-only
/// shuttle door. These pin the watchdog economy the owner asked for on 2026-07-18.
/// </summary>
public class ReeverRaidTests
{
    [Fact]
    public void Roll_IsDeterministic_PerSeed()
    {
        ReeverRoll a = ReeverRaid.Roll(9876, watchdogLevel: 1);
        ReeverRoll b = ReeverRaid.Roll(9876, watchdogLevel: 1);
        Assert.Equal(a.Face1, b.Face1);
        Assert.Equal(a.Face2, b.Face2);
        Assert.Equal(a.Total, b.Total);
        Assert.Equal(a.Reevers, b.Reevers);
    }

    [Fact]
    public void Roll_IsTwoHonestD6()
    {
        for (ulong seed = 0; seed < 500; seed++)
        {
            ReeverRoll r = ReeverRaid.Roll(seed);
            Assert.InRange(r.Face1, 1, ReeverRaid.Faces);
            Assert.InRange(r.Face2, 1, ReeverRaid.Faces);
            Assert.InRange(r.Pips, 2, 12);
        }
    }

    [Fact]
    public void TheTwoDiceDoNotCorrelate()
    {
        // Salted apart on a shared seed: the pair must differ on plenty of rolls (a single die reused
        // twice would make Face1 == Face2 every time).
        int differ = 0;
        for (ulong seed = 0; seed < 200; seed++)
        {
            ReeverRoll r = ReeverRaid.Roll(seed);
            if (r.Face1 != r.Face2)
            {
                differ++;
            }
        }

        Assert.True(differ > 120, "two salted d6 should mostly differ");
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(6, 0)]
    [InlineData(7, 1)]
    [InlineData(8, 1)]
    [InlineData(9, 2)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    [InlineData(12, 3)]
    public void ReeversFor_FollowsTheWatchdogBands(int total, int expected)
    {
        Assert.Equal(expected, ReeverRaid.ReeversFor(total));
        Assert.InRange(ReeverRaid.ReeversFor(total), 0, ReeverRaid.MaxReevers);
    }

    [Fact]
    public void WatchdogLevel_RaisesTheTotal_AndShowsItsMath()
    {
        ReeverRoll bare = ReeverRaid.Roll(4242, watchdogLevel: 0);
        ReeverRoll haunted = ReeverRaid.Roll(4242, watchdogLevel: 2);

        // Same seed → same natural faces; the level rides on top as a visible modifier.
        Assert.Equal(bare.Pips, haunted.Pips);
        Assert.Equal(bare.Pips + 2, haunted.Total);
        Assert.Contains("haunted ground", haunted.Describe());
        Assert.Contains("2d6:", haunted.Describe());
    }

    [Fact]
    public void HauntedGround_IsNeverSaferToRevisit()
    {
        // The watchdog modifier can only ever add hostiles, never remove them — a haunted stash is
        // strictly at least as dangerous to return to as an unhaunted one at the same seed.
        for (ulong seed = 0; seed < 300; seed++)
        {
            int bare = ReeverRaid.Roll(seed, watchdogLevel: 0).Reevers;
            int haunted = ReeverRaid.Roll(seed, watchdogLevel: 3).Reevers;
            Assert.True(haunted >= bare);
        }
    }

    [Fact]
    public void OverManySeeds_EveryPackSize_CanTurnOut()
    {
        var seen = new HashSet<int>();
        for (ulong seed = 0; seed < 400; seed++)
        {
            seen.Add(ReeverRaid.Roll(seed).Reevers);
        }

        Assert.Contains(0, seen);
        Assert.Contains(1, seen);
        Assert.Contains(2, seen);
        Assert.Contains(3, seen);
    }

    // ── The crew-only door law (ReeverChase) ──

    [Fact]
    public void Reever_Chases_TowardTheDigger()
    {
        // On the surface side (below the barrier), a Reever closes the gap toward the avatar.
        (double x, double y) = ReeverChase.Step(-20, -35, avatarX: 0, avatarY: -30, stepDistance: 1.0, barrierY: -10);
        double before = Math.Sqrt(20 * 20 + 5 * 5);
        double after = Math.Sqrt(x * x + (y + 30) * (y + 30));
        Assert.True(after < before, "the Reever should have closed on the digger");
    }

    [Fact]
    public void Reever_NeverCrossesTheCrewOnlyDoor()
    {
        // The digger has fled up the tube, deep into the ship (well above the barrier). Step the Reever
        // a thousand times: it may pile up at the threshold but must never pass it.
        const double barrierY = -10;
        double rx = -6, ry = -12; // just outside, on the surface
        for (int i = 0; i < 1000; i++)
        {
            (rx, ry) = ReeverChase.Step(rx, ry, avatarX: -6, avatarY: 8, stepDistance: 9.0 * 0.1, barrierY: barrierY);
            Assert.True(ry <= barrierY, $"a Reever reached y={ry}, past the crew-only door at {barrierY}");
        }

        // And having been penned at the door, it cannot be catching an avatar safe inside the ship.
        Assert.False(ReeverChase.Caught(rx, ry, -6, 8));
    }

    [Fact]
    public void Caught_OnlyWhenWithinReach()
    {
        Assert.True(ReeverChase.Caught(-6, -30, -6, -30));
        Assert.True(ReeverChase.Caught(-6, -30, -6, -30 + ReeverChase.CatchRadius));
        Assert.False(ReeverChase.Caught(-6, -30, -6, -30 + ReeverChase.CatchRadius + 1));
    }

    // ── The watchdog economy: presence hardens a stash against a rival's slow discovery roll ──

    [Fact]
    public void DiscoveryChance_ShrinksWithWatchdogPresence_FlooredNotZero()
    {
        Assert.Equal(DiscoveryRule.DiscoveryChancePercent, DiscoveryRule.DiscoveryChanceFor(0));
        Assert.Equal(DiscoveryRule.DiscoveryChancePercent - 1, DiscoveryRule.DiscoveryChanceFor(1));
        Assert.Equal(DiscoveryRule.MinDiscoveryChancePercent, DiscoveryRule.DiscoveryChanceFor(3));
        Assert.True(DiscoveryRule.DiscoveryChanceFor(99) >= DiscoveryRule.MinDiscoveryChancePercent);
    }

    [Fact]
    public void HauntedStash_IsFoundNoMoreOftenThanAnUnhauntedOne()
    {
        // Over a long span, a haunted cache is discovered on strictly fewer (or equal) periods.
        int bareHits = 0, hauntedHits = 0;
        for (long p = 1; p <= 5000; p++)
        {
            if (DiscoveryRule.IsDiscovered("cache-you-7", p, reeverLevel: 0)) bareHits++;
            if (DiscoveryRule.IsDiscovered("cache-you-7", p, reeverLevel: 3)) hauntedHits++;
        }

        Assert.True(hauntedHits <= bareHits, "watchdogs must not make a stash easier to find");
        Assert.True(bareHits > hauntedHits, "at these odds the discount should bite over 5000 rolls");
    }

    // ── Reevers never take loot (owner's core law) ──

    [Fact]
    public void Reevers_NeverTouchTheLoot()
    {
        var ledger = new CacheLedger();
        var cargo = new List<CacheCargo> { new("He3", 4, Hot: true), new("Ice", 2, Hot: false) };
        TreasureCache buried = ledger.Bury("miranda", coin: 1200, cargo, simTime: 100, owner: "you", playerOwned: true, reeverLevel: 3);

        // A full pack rouses on the dig — but the raid rule has no loot output at all; the chest is
        // untouched. Digging returns exactly what went in.
        ReeverRoll raid = ReeverRaid.Roll(DiceRule.Seed("reever-dig", 42), buried.ReeverLevel);
        Assert.True(raid.Total >= buried.ReeverLevel); // the watchdogs are real…

        TreasureCache? dug = ledger.Dig(buried.Id);
        Assert.NotNull(dug);
        Assert.Equal(1200, dug!.Value.Coin);
        Assert.Equal(6, dug.Value.TotalCargoUnits);
        Assert.Equal(4, dug.Value.HotCargoUnits);
        Assert.Equal(buried.ContentsLine(), dug.Value.ContentsLine());
    }

    // ── The watchdog level round-trips losslessly through the vault ──

    [Fact]
    public void ReeverLevel_SurvivesTheVaultRoundTrip()
    {
        var ledger = new CacheLedger();
        ledger.Bury("miranda", coin: 500, [], simTime: 10, owner: "you", playerOwned: true, reeverLevel: 2);

        CachesSection section = VaultMapper.ToSection(ledger);
        var restored = new CacheLedger();
        VaultMapper.Apply(section, restored);

        TreasureCache back = Assert.Single(restored.Caches);
        Assert.Equal(2, back.ReeverLevel);
    }

    [Fact]
    public void OlderCacheRecord_WithoutTheField_LoadsAsUnhaunted()
    {
        // A pre-#295 vault: a CacheRecord that never set ReeverLevel defaults it to 0, losslessly.
        var section = new CachesSection
        {
            NextMintIndex = 1,
            Caches = [new CacheRecord { Id = "cache-you-0", BodyId = "miranda", Owner = "you", PlayerOwned = true, Coin = 300 }],
        };
        var ledger = new CacheLedger();
        VaultMapper.Apply(section, ledger);

        Assert.Equal(0, Assert.Single(ledger.Caches).ReeverLevel);
    }
}
