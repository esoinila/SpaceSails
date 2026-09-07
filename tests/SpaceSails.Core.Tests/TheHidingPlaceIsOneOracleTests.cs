namespace SpaceSails.Core.Tests;

/// <summary>
/// #455 · <b>DEEPER IS SAFER, AND THE PROMISE AND THE DICE ARE ONE ARITHMETIC.</b>
///
/// <para>Owner, live 2026-07-27: <i>"venturing deeper exposes the player to more reevers, but it makes the
/// buried place also equally more safe"</i>; <i>"buried beats dropped, by a lot"</i>; <i>"even one left on
/// surface might be quite safe thanks to reevers as watch dogs"</i>. The issue's governing law:
/// <b>the same distance that makes the walk dangerous is what makes the cache safe.</b></para>
///
/// <h3>What is actually at risk here, and what these tests are shaped against</h3>
/// <para>A safety promise is the exact shape of this repository's named bug class — one truth with two
/// reporters. The line shown when a captain buries a chest and the roll thrown at that chest three days
/// later are, structurally, an invitation to compute the same thing twice and let them drift. So the guards
/// below are not "does the number look right"; they are:</para>
/// <list type="number">
/// <item><see cref="TheThreeTermsAreThreeDIFFERENTPlaces"/> — the vacuity anchor. A deep-buried chest on bad
/// ground beats a pad-side dropped one by a MARGIN THE TEST NAMES, and identical inputs tie exactly. A
/// constant oracle fails the first half; a randomised one fails the second.</item>
/// <item><see cref="EveryTermMovesTheOddsOnItsOwn"/> — no term is decoration. Each of the three, moved
/// alone with the other two pinned, strictly changes the odds.</item>
/// <item><see cref="TheRungNeverPromisesMoreThanTheOddsDeliver"/> — the monotone sweep, over the whole
/// input lattice: as any term improves the odds never rise and the rung never falls, and the three bands
/// are strictly ordered by odds. This is what makes the word on the card mean the number on the die.</item>
/// <item><see cref="TheBuryTimeReadIsTheThresholdTheReturnRollUses"/> — the two reporters, held against
/// each other at every point of the lattice.</item>
/// </list>
///
/// <h3>Red proof (watched, quoted in the pull request)</h3>
/// <list type="bullet">
/// <item>Make <c>CacheSafety.CarryCredit</c> return 0 — <see cref="TheThreeTermsAreThreeDIFFERENTPlaces"/>
/// and <see cref="EveryTermMovesTheOddsOnItsOwn"/> fail naming the carry.</item>
/// <item>Give a dropped chest the shovel's premium instead of the open-ground penalty —
/// <see cref="ANoOneEverCallsAChestLyingInTheOpenBuried"/> fails on a chest that reads "Considered" while
/// lying on the regolith, and <see cref="EveryTermMovesTheOddsOnItsOwn"/> fails on the shovel.</item>
/// <item>Give the return roll its own threshold (the old <c>DiscoveryChanceFor(reeverLevel)</c>) while the
/// bury line keeps the full read — <see cref="TheBuryTimeReadIsTheThresholdTheReturnRollUses"/> fails on the
/// first carried chest, which is exactly the drift this issue exists to close.</item>
/// </list>
/// </summary>
public class TheHidingPlaceIsOneOracleTests
{
    // The two ends of the whole design, as a captain would describe them.
    private const double DeepCarryDu = 210.0;  // out at the deep commitment anchor and past it
    private const int FullPack = 3;            // the standing watchdog level a bad ground carries

    /// <summary>
    /// THE VACUITY ANCHOR, and it is stated first because everything below is worthless without it.
    ///
    /// <para>A chest carried out to the deep and buried on ground three Old Ones haunt must beat a chest
    /// dropped by the landing pad on quiet ground — by a margin this test NAMES, not merely by "less than
    /// or equal". And two identical hiding places must price identically, to the point, so the guard cannot
    /// be satisfied by an oracle that just returns something different every time.</para>
    /// </summary>
    [Fact]
    public void TheThreeTermsAreThreeDIFFERENTPlaces()
    {
        CacheSafetyRead best = CacheSafety.Read(DeepCarryDu, buried: true, FullPack);
        CacheSafetyRead worst = CacheSafety.Read(padDistanceDu: 0, buried: false, reeverLevel: 0);

        // The named margin: the best hiding place in the game is FIVE TIMES safer per day than the worst,
        // and at least four whole percentage points of daily odds separate them.
        Assert.True(worst.ChancePerMille >= best.ChancePerMille * 5,
            $"the worst hiding place ({worst.ChancePerMille}‰) must be at least 5× the odds of the best "
            + $"({best.ChancePerMille}‰) — depth, the shovel and the watchdogs are meant to be worth something");
        Assert.True(worst.ChancePerMille - best.ChancePerMille >= 40,
            $"only {worst.ChancePerMille - best.ChancePerMille}‰ separates the best hiding place from the worst");

        // …and the words match the ends they are at.
        Assert.Equal(CacheSafetyRung.Guarded, best.Rung);
        Assert.Equal(CacheSafetyRung.Exposed, worst.Rung);

        // The tie. Same three terms → the same read, to the point, every time.
        Assert.Equal(best, CacheSafety.Read(DeepCarryDu, buried: true, FullPack));
        Assert.Equal(worst, CacheSafety.Read(0, buried: false, 0));
    }

    /// <summary>NO TERM IS DECORATION. Each of the three moved ALONE, with the other two pinned, strictly
    /// changes the odds — so an oracle that quietly stopped reading one of its arguments cannot pass by
    /// leaning on the other two.</summary>
    [Fact]
    public void EveryTermMovesTheOddsOnItsOwn()
    {
        // 1 · The carry. Same shovel, same quiet ground; only the walk differs.
        int atThePad = CacheSafety.Read(0, buried: true, 0).ChancePerMille;
        int outThere = CacheSafety.Read(DeepCarryDu, buried: true, 0).ChancePerMille;
        Assert.True(outThere < atThePad,
            $"the carry bought nothing: {atThePad}‰ at the pad vs {outThere}‰ at the deep anchor");

        // 2 · The shovel. Same spot, same quiet ground; only whether a hole was dug.
        int dug = CacheSafety.Read(60, buried: true, 0).ChancePerMille;
        int dropped = CacheSafety.Read(60, buried: false, 0).ChancePerMille;
        Assert.True(dug < dropped,
            $"the shovel bought nothing: buried {dug}‰ vs dropped {dropped}‰ on the same ground");

        // 3 · The ground's own Reever weight (#295, the watchdogs). Same spot, same shovel.
        int quiet = CacheSafety.Read(60, buried: true, 0).ChancePerMille;
        int haunted = CacheSafety.Read(60, buried: true, 2).ChancePerMille;
        Assert.True(haunted < quiet,
            $"the watchdogs bought nothing: quiet ground {quiet}‰ vs haunted {haunted}‰");
    }

    /// <summary>
    /// THE MONOTONE SWEEP. Over the whole input lattice — every carry from the pad to past the deep anchor,
    /// every provenance, every watchdog level — two things hold:
    ///
    /// <list type="number">
    /// <item>improving any single term never RAISES the odds and never LOWERS the rung;</item>
    /// <item>the three rungs are strictly ordered by odds: every Guarded chest is safer than every
    /// Considered one, which is safer than every Exposed one.</item>
    /// </list>
    ///
    /// <para>That second claim is the one that makes the word on the ledger row worth reading. Without it a
    /// card could say "Guarded" over odds a card saying "Exposed" would be glad of.</para>
    /// </summary>
    [Fact]
    public void TheRungNeverPromisesMoreThanTheOddsDeliver()
    {
        double[] carries = [0, 5, 12, 25, 40, 60, 90, 120, 150, 180, 205, 240, 400];
        bool?[] provenance = [false, null, true];   // worst → unrecorded → the shovel
        int[] packs = [0, 1, 2, 3, 4, 5, 6];

        var worstByRung = new Dictionary<CacheSafetyRung, int>();
        var bestByRung = new Dictionary<CacheSafetyRung, int>();

        foreach (bool? how in provenance)
        {
            foreach (int pack in packs)
            {
                int previous = int.MaxValue;
                foreach (double carry in carries)
                {
                    CacheSafetyRead read = CacheSafety.Read(carry, how, pack);

                    // (1a) a longer carry never costs safety
                    Assert.True(read.ChancePerMille <= previous,
                        $"carrying it further made it LESS safe at {carry} du (buried={how}, pack {pack})");
                    previous = read.ChancePerMille;

                    // (1b) the shovel never costs safety, at the same spot and ground
                    Assert.True(
                        CacheSafety.Read(carry, true, pack).ChancePerMille
                        <= CacheSafety.Read(carry, null, pack).ChancePerMille,
                        $"the shovel cost safety at {carry} du, pack {pack}");
                    Assert.True(
                        CacheSafety.Read(carry, null, pack).ChancePerMille
                        <= CacheSafety.Read(carry, false, pack).ChancePerMille,
                        $"lying in the open was safer than an unrecorded chest at {carry} du, pack {pack}");

                    // (1c) another Old One on the ground never costs safety
                    Assert.True(
                        CacheSafety.Read(carry, how, pack + 1).ChancePerMille <= read.ChancePerMille,
                        $"another watchdog made it LESS safe at {carry} du (buried={how}, pack {pack})");

                    worstByRung[read.Rung] = Math.Max(worstByRung.GetValueOrDefault(read.Rung, 0), read.ChancePerMille);
                    bestByRung[read.Rung] = Math.Min(bestByRung.GetValueOrDefault(read.Rung, int.MaxValue), read.ChancePerMille);
                }
            }
        }

        // All three rungs must actually occur, or "monotone" is a statement about one band.
        Assert.Equal(3, worstByRung.Count);

        // (2) strictly ordered bands: the WORST Guarded chest is still safer than the BEST Considered one,
        // and the worst Considered is still safer than the best Exposed.
        Assert.True(worstByRung[CacheSafetyRung.Guarded] < bestByRung[CacheSafetyRung.Considered],
            $"a Guarded chest ({worstByRung[CacheSafetyRung.Guarded]}‰) was no safer than a Considered one "
            + $"({bestByRung[CacheSafetyRung.Considered]}‰) — the word does not mean the number");
        Assert.True(worstByRung[CacheSafetyRung.Considered] < bestByRung[CacheSafetyRung.Exposed],
            $"a Considered chest ({worstByRung[CacheSafetyRung.Considered]}‰) was no safer than an Exposed one "
            + $"({bestByRung[CacheSafetyRung.Exposed]}‰)");
    }

    /// <summary>
    /// THE TWO REPORTERS, HELD AGAINST EACH OTHER. The number the bury line quotes IS the threshold the
    /// return-trip roll is compared against, at every point of the lattice — asserted through the shipping
    /// <see cref="DiscoveryRule.IsDiscovered(TreasureCache, long)"/> rather than by re-deriving it, because a
    /// test that recomputed the threshold would be a third reporter of the same truth.
    /// </summary>
    [Fact]
    public void TheBuryTimeReadIsTheThresholdTheReturnRollUses()
    {
        int checkedPoints = 0;
        foreach (double carry in new double[] { 0, 30, 90, 205 })
        {
            foreach (bool? how in new bool?[] { false, null, true })
            {
                foreach (int pack in new[] { 0, 1, 3 })
                {
                    TreasureCache chest = Chest("cache-you-0", how, carry, pack);
                    int quoted = chest.Safety.ChancePerMille;   // what the bury line told the captain

                    for (long day = 1; day <= 400; day++)
                    {
                        bool rolledFound = DiscoveryRule.Roll(chest.Id, day) <= quoted;
                        Assert.Equal(rolledFound, DiscoveryRule.IsDiscovered(chest, day));
                        checkedPoints++;
                    }
                }
            }
        }

        Assert.True(checkedPoints > 10_000, "the sweep did not actually sweep anything");
    }

    /// <summary>THE PROMISE PAYS OUT OVER TIME, not merely on paper: a Guarded chest survives strictly
    /// more days than an Exposed one with the SAME id and the same die. Same roll stream, different
    /// threshold — which is the only honest way to compare two hiding places.</summary>
    [Fact]
    public void AGuardedChestOutlivesAnExposedOneOnTheSameDice()
    {
        TreasureCache guarded = Chest("cache-you-11", buried: true, DeepCarryDu, FullPack);
        TreasureCache exposed = Chest("cache-you-11", buried: false, padDistance: 0, reeverLevel: 0);

        Assert.Equal(CacheSafetyRung.Guarded, guarded.Safety.Rung);
        Assert.Equal(CacheSafetyRung.Exposed, exposed.Safety.Rung);

        int guardedFinds = 0, exposedFinds = 0;
        for (long day = 1; day <= 5000; day++)
        {
            if (DiscoveryRule.IsDiscovered(guarded, day)) guardedFinds++;
            if (DiscoveryRule.IsDiscovered(exposed, day)) exposedFinds++;
        }

        Assert.True(exposedFinds > guardedFinds * 3,
            $"over 5000 days the open chest was found {exposedFinds} times and the deep one {guardedFinds} — "
            + "the gap the whole issue is about did not show up in the dice");
    }

    /// <summary>NOBODY EVER CALLS A CHEST LYING IN THE OPEN "BURIED". The Considered rung's authored line
    /// says the word out loud, so no hiding place that never saw a shovel may land on it — at any carry, on
    /// any ground. (It may reach Guarded on ground a full pack haunts: that is owner rule 3, <i>"even one
    /// left on surface might be quite safe thanks to reevers as watch dogs"</i>.)</summary>
    [Fact]
    public void ANoOneEverCallsAChestLyingInTheOpenBuried()
    {
        bool everGuarded = false;
        for (int pack = 0; pack <= ReeverRaid.MaxReevers; pack++)
        {
            for (double carry = 0; carry <= 400; carry += 2.5)
            {
                CacheSafetyRead read = CacheSafety.Read(carry, buried: false, pack);
                Assert.True(read.Rung != CacheSafetyRung.Considered,
                    $"a chest lying in the open on {pack}-watchdog ground {carry} du out read \"Considered\", "
                    + "whose line claims it was buried");
                everGuarded |= read.Rung == CacheSafetyRung.Guarded;
            }
        }

        Assert.True(everGuarded,
            "bad enough ground never made a dropped chest Guarded — owner rule 3 is not paying out");
    }

    /// <summary>THE THREE LINES, VERBATIM (Fable canon pass on #455, 2026-09-02). These are the whole of
    /// what a player is told about a hiding place, so they are pinned character for character.</summary>
    [Fact]
    public void TheThreeAuthoredLinesAreTheAuthoredLines()
    {
        Assert.Equal("Exposed", CacheSafety.Word(CacheSafetyRung.Exposed));
        Assert.Equal("Considered", CacheSafety.Word(CacheSafetyRung.Considered));
        Assert.Equal("Guarded", CacheSafety.Word(CacheSafetyRung.Guarded));

        Assert.Equal(
            "It lies where anyone can see it, on ground anyone would walk.",
            CacheSafety.Line(CacheSafetyRung.Exposed));
        Assert.Equal(
            "Buried, off the paths. A patient rival could still read the disturbed ground.",
            CacheSafety.Line(CacheSafetyRung.Considered));
        Assert.Equal(
            "Nobody sane digs here. That is the whole of the safe.",
            CacheSafety.Line(CacheSafetyRung.Guarded));
    }

    /// <summary>THE THREE RUNGS LAND WHERE THE CANON SAYS THEY DO — the parentheticals from the ruling,
    /// each as the hiding place a captain would actually make.</summary>
    [Theory]
    // "dropped in the open / near the pad / quiet ground"
    [InlineData(0.0, false, 0, CacheSafetyRung.Exposed)]
    [InlineData(0.0, true, 0, CacheSafetyRung.Exposed)]
    // "buried with the shovel, off the paths"
    [InlineData(60.0, true, 0, CacheSafetyRung.Considered)]
    [InlineData(0.0, true, 1, CacheSafetyRung.Considered)]
    // "deep carry and/or bad ground"
    [InlineData(205.0, true, 0, CacheSafetyRung.Guarded)]
    [InlineData(20.0, true, 2, CacheSafetyRung.Guarded)]
    [InlineData(0.0, false, 3, CacheSafetyRung.Guarded)]
    public void TheRungsLandWhereTheRulingPutsThem(double carry, bool buried, int pack, CacheSafetyRung expected) =>
        Assert.Equal(expected, CacheSafety.Read(carry, buried, pack).Rung);

    /// <summary>A CHEST ALREADY IN THE GROUND KEEPS THE DEAL IT WAS BURIED UNDER. An unrecorded provenance
    /// prices at exactly #295's shipped ladder — 4%, 3%, 2%, 1% — so nothing a captain buried last week is
    /// re-priced under a rule invented after he walked away from it.</summary>
    [Fact]
    public void ALegacyChestKeepsTheOddsItWasBuriedUnder()
    {
        Assert.Equal(40, CacheSafety.Read(null, null, 0).ChancePerMille);
        Assert.Equal(30, CacheSafety.Read(null, null, 1).ChancePerMille);
        Assert.Equal(20, CacheSafety.Read(null, null, 2).ChancePerMille);
        Assert.Equal(10, CacheSafety.Read(null, null, 3).ChancePerMille);

        // …and the percent view every older caller still reads is the same answer divided by ten.
        Assert.Equal(4, DiscoveryRule.DiscoveryChanceFor(0));
        Assert.Equal(3, DiscoveryRule.DiscoveryChanceFor(1));
        Assert.Equal(2, DiscoveryRule.DiscoveryChanceFor(2));
        Assert.Equal(1, DiscoveryRule.DiscoveryChanceFor(3));
        Assert.Equal(1, DiscoveryRule.DiscoveryChanceFor(99));
    }

    /// <summary>THE CARRY IS MEASURED OFF THE FIELD, NOT OFF A LITERAL. The pad the walk is measured from,
    /// and the anchor that earns full credit, are read from Core's one field envelope — so a field that
    /// grows re-prices the walk instead of leaving this rule auditing a world that stopped existing (#573's
    /// lesson, and this repository's first named bug class).</summary>
    [Fact]
    public void TheCarryIsMeasuredOffTheFieldItself()
    {
        SurfaceLayout.Field f = SurfaceLayout.DefaultField;

        Assert.Equal(0, CacheSafety.PadDistanceOf(f.HomeX, f.LandingBandY), 6);
        // Standing up in the tube, above the band, is still "at the pad".
        Assert.Equal(0, CacheSafety.PadDistanceOf(f.HomeX, f.LandingBandY + 5), 6);
        // The deep commitment anchor is a full carry away, and it earns the full credit.
        Assert.Equal(CacheSafety.FullCarryDu, CacheSafety.PadDistanceOf(f.AnchorX, f.AnchorY), 6);
        Assert.Equal(CacheSafety.MaxCarryCreditPerMille, CacheSafety.CarryCredit(CacheSafety.FullCarryDu));
        Assert.Equal(0, CacheSafety.CarryCredit(0));

        // …and the anchor really is deep: this is a long walk, not a step off the pad.
        Assert.True(CacheSafety.FullCarryDu > 150,
            $"the full carry is only {CacheSafety.FullCarryDu:0.#} du — that is not a commitment");
    }

    /// <summary>THE TERMS SURVIVE THE VAULT, and a chest that never recorded them writes NOTHING — no
    /// <c>"buried": null</c>, no <c>"padDistance": null</c>. #650 learned this at byte 564 of a real legacy
    /// file: the checksum is taken over the payload, so one extra key per chest changes the digest of every
    /// hoard ever saved and hangs the 📛 tampered flag on an honest voyage. <c>ALegacyVaultRoundTripsByteForByte</c>
    /// holds the same line against a real captured file; this holds it against the writer directly.</summary>
    [Fact]
    public void TheNewTermsRideTheVaultAndALegacyChestWritesNeitherKey()
    {
        var ledger = new CacheLedger();
        ledger.Bury("phobos", 900, [], 40000, "you", playerOwned: true, reeverLevel: 1,
            digX: -6, digY: -200, siteIndex: 2, buried: true, padDistance: 173.5);

        var vault = new Vault { Caches = VaultMapper.ToSection(ledger) };
        string json = VaultSerializer.Save(vault);

        Assert.Contains("\"buried\": true", json, StringComparison.Ordinal);
        Assert.Contains("\"padDistance\": 173.5", json, StringComparison.Ordinal);

        var restored = new CacheLedger();
        VaultMapper.Apply(VaultSerializer.Load(json).Caches!, restored);
        TreasureCache back = restored.Caches.Single();
        Assert.Equal(true, back.Buried);
        Assert.Equal(173.5, back.PadDistance);
        Assert.Equal(CacheSafety.Read(173.5, true, 1), back.Safety);

        // …and the chest that never recorded them writes neither key at all.
        var old = new CacheLedger();
        old.Bury("phobos", 900, [], 40000, "Old Vane", playerOwned: false);
        string legacyJson = VaultSerializer.Save(new Vault { Caches = VaultMapper.ToSection(old) });

        Assert.DoesNotContain("\"buried\"", legacyJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"padDistance\"", legacyJson, StringComparison.Ordinal);
        Assert.False(VaultSerializer.Load(legacyJson).Tampered);
    }

    /// <summary>#455's rule 4 · A HOARD OUTLIVES ITS CAPTAIN. The vault is what carries a predecessor's
    /// cache across the insurance rebirth (#398), so this pins the whole record — the three safety terms
    /// included — through a save and a reload. The other half of the answer (that the rebirth itself never
    /// touches the ledger) is driven through the shipping <c>BustedResurrect</c> in the Client suite.</summary>
    [Fact]
    public void APredecessorsWellBuriedCacheSurvivesTheVaultIntact()
    {
        var ledger = new CacheLedger();
        TreasureCache deep = ledger.Bury(
            "phobos", 2400, [new CacheCargo("He3", 4, Hot: true)], 61234.5, "you", playerOwned: true,
            reeverLevel: 3, digX: -6, digY: -232, siteIndex: 1, buried: true, padDistance: 205.0);

        string json = VaultSerializer.Save(new Vault { Caches = VaultMapper.ToSection(ledger) });

        var newCaptain = new CacheLedger();
        VaultMapper.Apply(VaultSerializer.Load(json).Caches!, newCaptain);

        TreasureCache inherited = newCaptain.Caches.Single();
        // Field for field (the record's own equality would compare the cargo LIST BY REFERENCE, which is a
        // guard that fails for a reason that has nothing to do with the hoard).
        Assert.Equal(deep.Id, inherited.Id);
        Assert.Equal(deep.BodyId, inherited.BodyId);
        Assert.Equal(deep.Coin, inherited.Coin);
        Assert.Equal(deep.BuriedSimTime, inherited.BuriedSimTime);
        Assert.Equal(deep.ReeverLevel, inherited.ReeverLevel);
        Assert.Equal(deep.SiteIndex, inherited.SiteIndex);
        Assert.Equal(deep.DigX, inherited.DigX);
        Assert.Equal(deep.DigY, inherited.DigY);
        Assert.Equal(deep.Buried, inherited.Buried);
        Assert.Equal(deep.PadDistance, inherited.PadDistance);
        Assert.Equal(deep.Cargo.Select(c => (c.CargoClass, c.Units, c.Hot)),
                     inherited.Cargo.Select(c => (c.CargoClass, c.Units, c.Hot)));

        Assert.Equal(CacheSafetyRung.Guarded, inherited.Safety.Rung);   // and still the safe it was
        Assert.Equal(deep.Safety, inherited.Safety);
    }

    private static TreasureCache Chest(string id, bool? buried, double padDistance, int reeverLevel) =>
        new(id, "phobos", "the monolith", "sunward", 40, 900, [], 0, "you", PlayerOwned: true,
            ReeverLevel: reeverLevel, DigX: null, DigY: null, SiteIndex: null,
            Buried: buried, PadDistance: padDistance);
}
