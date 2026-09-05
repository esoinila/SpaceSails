namespace SpaceSails.Core.Tests;

/// <summary>
/// #316 laws 1 (second half), 3 and 4 · <b>SOMEBODY ELSE HAS BEEN HERE, AND YOU CAN READ IT — AND SO CAN
/// THEY.</b>
///
/// <para>Owner, live 2026-07-18: <i>"If we find already shot Reevers at a site then we know that somebody
/// else has been there to hide, pick-up, search etc :-D It serves as a clue."</i> #1105 shipped the half the
/// captain writes himself; #1127 gave it the three ages. This file guards the half the clue is ABOUT — a
/// rival's visit leaving physical evidence — and the symmetry that makes it a design rather than a decal:
/// the captain's own firefight advertises his hoard.</para>
///
/// <h3>What is actually at risk, and what these guards are shaped against</h3>
/// <para>Evidence is the exact shape of two named bug classes in this repo at once. <b>A guard that cannot
/// tell pass from fail</b>: "a plundered site has husks" passes on a build that scatters husks over every
/// site, so every guard below is stated against a world that contains BOTH a robbed cache and an untouched
/// one and asks the two to differ. And <b>seeded decoration</b> — law 4: marks that are re-rolled each time
/// they are drawn look identical on one visit and betray themselves on the second, so determinism is asked
/// twice (same call twice, and across a save).</para>
///
/// <h3>Red proof (watched, quoted in the pull request)</h3>
/// <list type="bullet">
/// <item>Make <c>RivalVisit.HusksLeftBy</c> return a typed 2 — <see cref="TheHuskCountIsThePackTheRivalsMet"/>
/// fails naming the roll it disagreed with.</item>
/// <item>Stamp the marks with a "now" instead of the period's own moment —
/// <see cref="TheMarksAreDatedByTheDayTheRollLanded"/> fails on the age band.</item>
/// <item>Fold #316's signpost in with the other three terms (before the floor) —
/// <see cref="TheLoudStandBreachesTheFloorTheQuietDigHidesUnder"/> fails on the haunted deep chest, which is
/// the only case the term exists for.</item>
/// <item>Drop the husk count from the discovery watch's threshold —
/// <see cref="TheSignpostRisesWithTheFightAndIsZeroForAQuietDig"/> fails.</item>
/// </list>
/// </summary>
public class TheBattlefieldIsALedgerTests
{
    private const string Body = "phobos";
    private const string Salt = "site-a";
    private const double SpotX = -40.0;
    private const double SpotY = -180.0;

    /// <summary>A chest, buried deep and hidden, on ground carrying <paramref name="reeverLevel"/>
    /// watchdogs. Minted through the shipping mint so nothing here invents a cache shape.</summary>
    private static TreasureCache Chest(string id, int reeverLevel = 0) =>
        CacheMint.Bury(
            id, Body, mintIndex: 1, coin: 900, cargo: [], buriedSimTime: 0, owner: "you",
            playerOwned: true, reeverLevel: reeverLevel, digX: SpotX, digY: SpotY, siteIndex: 0,
            buried: true, padDistance: 200.0);

    /// <summary>The first discovery period on which this ground rouses at least <paramref name="atLeast"/>
    /// Old Ones — the search for a case, never an assumption that day 1 happens to be it.</summary>
    private static long DayThePackTurnsOut(string cacheId, int reeverLevel, int atLeast)
    {
        for (long p = 1; p < 4000; p++)
        {
            if (RivalVisit.WatchdogsMet(cacheId, reeverLevel, p).Reevers >= atLeast)
            {
                return p;
            }
        }
        throw new InvalidOperationException($"no day rouses {atLeast} on this ground");
    }

    /// <summary>…and the first that rouses NONE, so the quiet visit is a real case and not a hope.</summary>
    private static long DayTheGroundStaysQuiet(string cacheId, int reeverLevel)
    {
        for (long p = 1; p < 4000; p++)
        {
            if (RivalVisit.WatchdogsMet(cacheId, reeverLevel, p).Reevers == 0)
            {
                return p;
            }
        }
        throw new InvalidOperationException("this ground never stays quiet");
    }

    /// <summary>
    /// THE VACUITY ANCHOR. One ledger, two grounds: the site a rival plundered carries husks and a hole; the
    /// site nobody touched carries NOTHING. A build that seeds husks everywhere passes the first half and
    /// fails the second; a build that writes nothing at all fails the first.
    /// </summary>
    [Fact]
    public void ARobbedSiteCarriesTheFightAndAnUntouchedOneCarriesNothing()
    {
        var ground = new GroundMemory();
        TreasureCache robbed = Chest("cache-you-robbed", reeverLevel: 2);
        long day = DayThePackTurnsOut(robbed.Id, robbed.ReeverLevel, atLeast: 2);

        RivalVisit.Evidence left = RivalVisit.LeftBehind(robbed.Id, robbed.ReeverLevel, SpotX, SpotY, day);
        foreach (GroundMemory.Husk h in left.Husks)
        {
            ground.Remember(GroundMemory.HuskKey(Body, Salt, h));
        }
        foreach (GroundMemory.Scar s in left.Scars)
        {
            ground.Remember(GroundMemory.ScarKey(Body, Salt, s));
        }

        // The robbed ground: bodies, and a hole where the ✗ was.
        Assert.NotEmpty(ground.HusksAt(Body, Salt));
        Assert.Contains(ground.ScarsAt(Body, Salt), s => s.What == GroundMemory.ScarKind.Pit);
        Assert.Equal(SpotX, ground.ScarsAt(Body, Salt).First(s => s.What == GroundMemory.ScarKind.Pit).X, 2);
        Assert.Equal(SpotY, ground.ScarsAt(Body, Salt).First(s => s.What == GroundMemory.ScarKind.Pit).Y, 2);

        // The ground next door, on the SAME ledger, where nothing happened.
        Assert.Empty(ground.HusksAt(Body, "site-b"));
        Assert.Empty(ground.ScarsAt(Body, "site-b"));
        Assert.Empty(ground.HusksAt("miranda", Salt));
    }

    /// <summary>
    /// THE COUNT IS THE ROLL, NOT A NUMBER SOMEBODY TYPED. Every husk a rival visit leaves is one Old One the
    /// existing 2D6 table roused for that ground on that day — so the count moves with the roll across a long
    /// sweep, and the sweep is asserted to contain more than one answer (a constant would pass a bare
    /// equality against itself).
    /// </summary>
    [Fact]
    public void TheHuskCountIsThePackTheRivalsMet()
    {
        TreasureCache chest = Chest("cache-you-count", reeverLevel: 1);
        var seen = new HashSet<int>();

        for (long day = 1; day <= 300; day++)
        {
            ReeverRoll roll = RivalVisit.WatchdogsMet(chest.Id, chest.ReeverLevel, day);
            RivalVisit.Evidence left = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, day);

            Assert.Equal(ReeverRaid.ReeversFor(roll.Total), left.Husks.Count);
            seen.Add(left.Husks.Count);
        }

        // The table has four rungs (0 / 2 / 4 / 6); a real ground reaches several of them over 300 days.
        Assert.True(seen.Count > 1, $"the husk count never moved: only {string.Join(",", seen)}");
    }

    /// <summary>
    /// THE QUIET VISIT LEAVES A HOLE AND NO BODIES, and the dire one leaves the hardware. Both halves stated
    /// against real days found by search, so neither is a case the test wished into being.
    /// </summary>
    [Fact]
    public void ABloodlessRobberyLeavesTheHoleAloneAndOnlyTheDireOneCostsASentry()
    {
        TreasureCache chest = Chest("cache-you-dire", reeverLevel: 2);

        long quiet = DayTheGroundStaysQuiet(chest.Id, chest.ReeverLevel);
        RivalVisit.Evidence nothing = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, quiet);
        Assert.Empty(nothing.Husks);
        Assert.Null(nothing.DryBot);
        Assert.Single(nothing.Scars);                                   // the hole, and only the hole
        Assert.Equal(GroundMemory.ScarKind.Pit, nothing.Scars[0].What);

        long dire = DayThePackTurnsOut(chest.Id, chest.ReeverLevel, ReeverRaid.MaxReevers);
        RivalVisit.Evidence worst = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, dire);
        Assert.Equal(ReeverRaid.MaxReevers, worst.Husks.Count);
        Assert.NotNull(worst.DryBot);
        Assert.Equal(GroundMemory.ScarKind.DryBot, worst.DryBot!.Value.What);

        // A middling day costs them nothing but the bodies.
        long middling = DayThePackTurnsOut(chest.Id, chest.ReeverLevel, atLeast: 2);
        if (RivalVisit.WatchdogsMet(chest.Id, chest.ReeverLevel, middling).Reevers < ReeverRaid.MaxReevers)
        {
            Assert.Null(RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, middling).DryBot);
        }
    }

    /// <summary>
    /// THE MARKS ARE DATED BY THE DAY THE ROLL LANDED, NEVER BY WHEN THE CAPTAIN GOT BACK. A warp that skips
    /// a fortnight resolves the discovery on the day it happened; the scene a captain walks into must read
    /// the age the world actually has, or the whole clue is a lie about when he was beaten to it.
    /// </summary>
    [Fact]
    public void TheMarksAreDatedByTheDayTheRollLanded()
    {
        TreasureCache chest = Chest("cache-you-dated", reeverLevel: 2);
        long day = DayThePackTurnsOut(chest.Id, chest.ReeverLevel, atLeast: 2);
        RivalVisit.Evidence left = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, day);

        double moment = day * DiscoveryRule.PeriodSeconds;
        Assert.Equal(moment, left.AtSimTime);
        Assert.All(left.Husks, h => Assert.Equal(moment, h.FellAtSimTime));
        Assert.All(left.Scars, s => Assert.Equal(moment, s.AtSimTime));

        // …and it reads through the three bands the ground already speaks — the ONLY lines, #1127's.
        Assert.Equal("Still smoking.", GroundMemory.AgeLine(left.Husks[0], moment + 3600));
        Assert.Equal("Dusted over. Days old.",
            GroundMemory.AgeLine(left.Husks[0], moment + (3 * GroundMemory.DaySeconds)));
        Assert.Equal("Regolith-dusted. Weeks old.",
            GroundMemory.AgeLine(left.Husks[0], moment + (20 * GroundMemory.DaySeconds)));

        // The hole dates off the same clock and the same three sentences — one question, one reporter.
        Assert.Equal(
            GroundMemory.AgeLine(left.Husks[0], moment + (3 * GroundMemory.DaySeconds)),
            GroundMemory.AgeLine(left.Pit.AtSimTime, moment + (3 * GroundMemory.DaySeconds)));

        // A captain who comes home a fortnight late reads a fortnight, not a fresh kill.
        double home = moment + (14 * GroundMemory.DaySeconds);
        Assert.Equal("Regolith-dusted. Weeks old.", GroundMemory.AgeLine(left.Husks[0], home));
    }

    /// <summary>
    /// LAW 4 · DETERMINISM. Same chest, same spot, same day → the same marks in the same places, twice, and
    /// a different day is a different scene (so the guard cannot be satisfied by a constant).
    /// </summary>
    [Fact]
    public void TheSameResolutionLeavesTheSameSceneForEver()
    {
        TreasureCache chest = Chest("cache-you-determinism", reeverLevel: 1);
        long day = DayThePackTurnsOut(chest.Id, chest.ReeverLevel, atLeast: 2);

        RivalVisit.Evidence a = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, day);
        RivalVisit.Evidence b = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, day);

        Assert.Equal(
            a.Husks.Select(h => GroundMemory.HuskKey(Body, Salt, h)),
            b.Husks.Select(h => GroundMemory.HuskKey(Body, Salt, h)));
        Assert.Equal(
            a.Scars.Select(s => GroundMemory.ScarKey(Body, Salt, s)),
            b.Scars.Select(s => GroundMemory.ScarKey(Body, Salt, s)));

        long later = DayThePackTurnsOut2(chest.Id, chest.ReeverLevel, after: day);
        RivalVisit.Evidence c = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, later);
        Assert.NotEqual(
            a.Husks.Select(h => GroundMemory.HuskKey(Body, Salt, h)).ToArray(),
            c.Husks.Select(h => GroundMemory.HuskKey(Body, Salt, h)).ToArray());
    }

    private static long DayThePackTurnsOut2(string cacheId, int reeverLevel, long after)
    {
        for (long p = after + 1; p < 4000; p++)
        {
            if (RivalVisit.WatchdogsMet(cacheId, reeverLevel, p).Reevers >= 2)
            {
                return p;
            }
        }
        throw new InvalidOperationException("no second day rouses this ground");
    }

    /// <summary>
    /// THE SCENE SURVIVES THE FILE. A rival's visit that dies with the session is the #1105 bug re-shipped
    /// through the other door: the captain is in orbit when it happens and can only ever meet it on a later
    /// trip, so the vault is the ONLY way this feature reaches a player at all.
    /// </summary>
    [Fact]
    public void TheSceneRoundTripsTheVault()
    {
        var ship = new GroundMemory();
        TreasureCache chest = Chest("cache-you-vault", reeverLevel: 2);
        long day = DayThePackTurnsOut(chest.Id, chest.ReeverLevel, ReeverRaid.MaxReevers);
        RivalVisit.Evidence left = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, day);

        foreach (GroundMemory.Husk h in left.Husks)
        {
            ship.Remember(GroundMemory.HuskKey(Body, Salt, h));
        }
        foreach (GroundMemory.Scar s in left.Scars)
        {
            ship.Remember(GroundMemory.ScarKey(Body, Salt, s));
        }

        var saved = new Vault { Ground = new GroundSection { Changed = ship.Stored } };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(saved));
        GroundMemory back = GroundMemory.Restore(loaded.Ground?.Changed);

        Assert.Equal(
            ship.HusksAt(Body, Salt).Select(h => GroundMemory.HuskKey(Body, Salt, h)),
            back.HusksAt(Body, Salt).Select(h => GroundMemory.HuskKey(Body, Salt, h)));
        Assert.Equal(
            ship.ScarsAt(Body, Salt).Select(s => GroundMemory.ScarKey(Body, Salt, s)),
            back.ScarsAt(Body, Salt).Select(s => GroundMemory.ScarKey(Body, Salt, s)));

        // The dire case's hardware came home too, and it is still a bot rather than a body.
        Assert.Contains(back.ScarsAt(Body, Salt), s => s.What == GroundMemory.ScarKind.DryBot);
        Assert.DoesNotContain(back.HusksAt(Body, Salt), h => h.FellAtSimTime != left.AtSimTime);

        // A row this build cannot read is refused rather than guessed at — a file from a later build loads
        // as a captain who can see less, never as one who cannot load.
        Assert.False(GroundMemory.TryReadScarKey($"scar:{Body}:{Salt}:campfire:0_0:1.00_2.00@30", Body, Salt, out _));
        Assert.False(GroundMemory.TryReadScarKey(GroundMemory.HuskKey(Body, Salt, new GroundMemory.Husk(1, 2, 3)), Body, Salt, out _));
        Assert.False(GroundMemory.TryReadHuskKey(
            GroundMemory.ScarKey(Body, Salt, new GroundMemory.Scar(GroundMemory.ScarKind.Pit, 1, 2, 3)), Body, Salt, out _));
    }

    // ── #316 law 3 · THE SYMMETRY ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SIGNPOST RISES WITH THE FIGHT, AND THE QUIET DIG PAYS NOTHING FOR IT. Stated as a strict sweep so
    /// neither a dead term (never rises) nor a constant surcharge (rises for the quiet dig too) survives.
    /// </summary>
    [Fact]
    public void TheSignpostRisesWithTheFightAndIsZeroForAQuietDig()
    {
        CacheSafetyRead quiet = CacheSafety.Read(200.0, buried: true, reeverLevel: 0, huskCount: 0);
        Assert.Equal(0, quiet.BattleTerm);
        Assert.Equal(CacheSafety.Read(200.0, buried: true, 0), quiet);   // the world before this term

        int last = quiet.ChancePerMille;
        bool everRose = false;
        for (int husks = 1; husks <= ReeverRaid.MaxReevers; husks++)
        {
            CacheSafetyRead loud = CacheSafety.Read(200.0, buried: true, 0, husks);
            Assert.True(loud.BattleTerm > 0, $"{husks} bodies on the ground bought a rival nothing");
            Assert.True(loud.ChancePerMille >= last, "the signpost went the wrong way");
            everRose |= loud.ChancePerMille > last;
            last = loud.ChancePerMille;
        }
        Assert.True(everRose, "the odds never moved with the fight");

        // And it is capped by what the deepest carry buys — the loudest stand undoes the bravest walk and
        // no more. Read off the constants, never a typed number.
        Assert.Equal(CacheSafety.MaxBattleSignpostPerMille, CacheSafety.BattleSignpost(999));
        Assert.Equal(CacheSafety.MaxCarryCreditPerMille, CacheSafety.MaxBattleSignpostPerMille);

        // The percent view the legacy read speaks moves with it too — one oracle, still.
        Assert.True(DiscoveryRule.DiscoveryChanceFor(0, ReeverRaid.MaxReevers)
            > DiscoveryRule.DiscoveryChanceFor(0));
        Assert.Equal(DiscoveryRule.DiscoveryChancePercent, DiscoveryRule.DiscoveryChanceFor(0, 0));
    }

    /// <summary>
    /// THE CASE THE TERM EXISTS FOR, AND THE ONE A CARELESS BUILD LOSES. A stand happens where the pack
    /// turned out — which is haunted ground — which is already sitting on the discovery FLOOR. Folded in with
    /// the other three terms the signpost would be clamped away in exactly that case and the loudest
    /// afternoon in the game would move nothing. So: the deep, hidden, haunted chest is on the floor, and a
    /// full pack of husks around it is strictly worse than the floor. The ceiling still holds.
    /// </summary>
    [Fact]
    public void TheLoudStandBreachesTheFloorTheQuietDigHidesUnder()
    {
        CacheSafetyRead safest = CacheSafety.Read(220.0, buried: true, reeverLevel: 3);
        Assert.Equal(CacheSafety.MinChancePerMille, safest.ChancePerMille);   // the floor, as it always was
        Assert.Equal(CacheSafetyRung.Guarded, safest.Rung);

        CacheSafetyRead advertised = CacheSafety.Read(220.0, buried: true, 3, ReeverRaid.MaxReevers);
        Assert.True(advertised.ChancePerMille > safest.ChancePerMille,
            "the loudest stand in the game left the safest hole in the game exactly as safe");
        Assert.Equal(CacheSafety.MinChancePerMille + CacheSafety.MaxBattleSignpostPerMille,
            advertised.ChancePerMille);
        Assert.NotEqual(CacheSafetyRung.Guarded, advertised.Rung);   // the word the card says changed too

        // …and nothing breaches the ceiling.
        Assert.True(CacheSafety.Read(0, buried: false, 0, 999).ChancePerMille <= CacheSafety.MaxChancePerMille);
    }

    /// <summary>
    /// THE PROMISE AND THE DICE, STILL ONE ARITHMETIC — with the ground in the question. The bury line reads
    /// the chest against the ground it is going into; the return roll compares its d1000 against exactly
    /// that. This is #455's own guard, re-stated with the fourth term, because a term added to one reporter
    /// and not the other is precisely how this repo's third bug class ships.
    /// </summary>
    [Fact]
    public void TheBuryTimeReadIsStillTheThresholdTheReturnRollUses()
    {
        foreach (int husks in new[] { 0, 1, 4, ReeverRaid.MaxReevers })
        {
            TreasureCache chest = Chest($"cache-you-agree-{husks}", reeverLevel: 2);
            int quoted = chest.SafetyWith(husks).ChancePerMille;

            for (long day = 1; day <= 120; day++)
            {
                bool rolledFound = DiscoveryRule.Roll(chest.Id, day) <= quoted;
                Assert.Equal(rolledFound, DiscoveryRule.IsDiscovered(chest, day, husks));
            }
        }

        // A ground nobody has fought on is the chest exactly as it has always read.
        TreasureCache untouched = Chest("cache-you-legacy", reeverLevel: 2);
        Assert.Equal(untouched.Safety, untouched.SafetyWith(0));
    }

    /// <summary>
    /// AND THE WHOLE LOOP, END TO END, THE WAY A CAPTAIN MEETS IT: bury a chest on a ground he shot up, fly
    /// away, get beaten to it while he is gone, come home and read the scene off the file. Every step is a
    /// shipping call; nothing here re-implements the rules it is checking.
    /// </summary>
    [Fact]
    public void BuryLeaveAndComeHomeToARobbedSite()
    {
        // 1 · He made a stand here, and the ground kept it (#1105's half, still working).
        var ground = new GroundMemory();
        for (int i = 0; i < ReeverRaid.MaxReevers; i++)
        {
            ground.Remember(GroundMemory.HuskKey(Body, Salt, new GroundMemory.Husk(-30 + i, -170, 0)));
        }
        int standing = ground.HusksAt(Body, Salt).Count;
        Assert.Equal(ReeverRaid.MaxReevers, standing);

        // 2 · He buries under a rival's-eye read that already knows about the mess he left. The chest is
        //     SEARCHED FOR rather than assumed: one whose own discovery day is a day the ground fights back,
        //     because a scene with no bodies in it would let this guard pass on a build that writes none.
        var ledger = new CacheLedger();
        TreasureCache chest = default;
        long found = -1;
        for (int i = 0; i < 400 && found < 0; i++)
        {
            TreasureCache candidate = Chest($"cache-you-e2e-{i}", reeverLevel: 2);
            if (DiscoveryRule.DiscoveredWithin(candidate, 0, 400 * DiscoveryRule.PeriodSeconds, standing)
                is { } day
                && RivalVisit.WatchdogsMet(candidate.Id, candidate.ReeverLevel, day).Reevers > 0)
            {
                chest = candidate;
                found = day;
            }
        }
        Assert.True(found >= 0, "no chest in 400 was ever taken off a ground that fought back");
        ledger.Load(chest);
        Assert.True(chest.SafetyWith(standing).ChancePerMille > chest.Safety.ChancePerMille);

        // 3 · He is away, and the watch resolves the whole span he skipped in one pass.

        // 4 · The ground carries what happened, on the resolution's own clock.
        RivalVisit.Evidence left = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, SpotX, SpotY, found);
        Assert.NotEmpty(left.Husks);
        foreach (GroundMemory.Husk h in left.Husks)
        {
            ground.Remember(GroundMemory.HuskKey(Body, Salt, h));
        }
        foreach (GroundMemory.Scar s in left.Scars)
        {
            ground.Remember(GroundMemory.ScarKey(Body, Salt, s));
        }
        ledger.Remove(chest.Id);
        Assert.Empty(ledger.CachesAt(Body, 0));   // the ✗ is off the map: the chest is gone

        // 5 · Lift-off, a save, a load, and he walks back onto it a week after the fact.
        GroundMemory back = GroundMemory.Restore(
            VaultSerializer.Load(VaultSerializer.Save(
                new Vault { Ground = new GroundSection { Changed = ground.Stored } })).Ground?.Changed);

        double home = left.AtSimTime + (9 * GroundMemory.DaySeconds);
        Assert.Contains(back.ScarsAt(Body, Salt), s => s.What == GroundMemory.ScarKind.Pit);

        // The scene reads: his own stand is ancient, and the visit that took his chest is dated.
        GroundMemory.Husk theirs = back.HusksAt(Body, Salt)
            .OrderByDescending(h => h.FellAtSimTime).First();
        Assert.Equal(left.AtSimTime, theirs.FellAtSimTime);
        Assert.Equal("Regolith-dusted. Weeks old.", GroundMemory.AgeLine(theirs, home));
        Assert.Equal("Dusted over. Days old.",
            GroundMemory.AgeLine(theirs, left.AtSimTime + (2 * GroundMemory.DaySeconds)));
    }
}
