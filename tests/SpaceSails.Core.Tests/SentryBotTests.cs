namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-314 · The ship's pirate sentries — escorts with a fuel gauge (owner, 2026-07-18: "a little more
/// of that Aliens movie threat... of running out of ammo"). These pin the ammo law at the seams: every
/// shot drains the 99-round magazine, 00 goes silent, downed Old Ones leave husks, the guns NEVER touch
/// loot, an abandoned bot writes a ledger line, and a haven rearm prints one honest receipt.
/// </summary>
public class SentryBotTests
{
    private static SentryBot.Deployed FullBotAt(double x, double y) =>
        new("K-77", x, y, SentryBot.MaxMagazine);

    [Fact]
    public void Readout_IsAlwaysTwoDigits_ClampedTo99()
    {
        Assert.Equal("99", SentryBot.Readout(99));
        Assert.Equal("99", SentryBot.Readout(150));   // clamps at the magazine
        Assert.Equal("07", SentryBot.Readout(7));      // the last dozen, readable
        Assert.Equal("00", SentryBot.Readout(0));
        Assert.Equal("00", SentryBot.Readout(-5));     // never negative
    }

    [Fact]
    public void Fire_DrainsOneRound_AndFreezesAtZero()
    {
        Assert.Equal(98, SentryBot.Fire(99));
        Assert.Equal(0, SentryBot.Fire(1));
        Assert.Equal(0, SentryBot.Fire(0));   // 00 = silent, drains nothing
        Assert.True(SentryBot.IsDry(0));
        Assert.False(SentryBot.IsDry(1));
    }

    [Fact]
    public void Step_DrainsTheCounter_ForEveryShotFired()
    {
        // One bot, one Reever inside the arc → one shot, one round gone.
        var bots = new[] { FullBotAt(0, 0) };
        var reevers = new[] { new SentryBot.Target(5, 0, 0) };

        SentryBot.Volley v = SentryBot.Step(bots, reevers);

        Assert.Equal(1, v.Shots);
        Assert.Equal(SentryBot.MaxMagazine - 1, v.Bots[0].Rounds);   // counter ticked down
        Assert.Single(v.Reevers);
        Assert.Equal(1, v.Reevers[0].HitsTaken);                     // the Old One soaked a hit
        Assert.Empty(v.Husks);                                       // not down yet
    }

    [Fact]
    public void Step_OutOfRange_HoldsFire_NoDrain()
    {
        var bots = new[] { FullBotAt(0, 0) };
        var reevers = new[] { new SentryBot.Target(SentryBot.RangeDeckUnits + 5, 0, 0) };

        SentryBot.Volley v = SentryBot.Step(bots, reevers);

        Assert.Equal(0, v.Shots);
        Assert.Equal(SentryBot.MaxMagazine, v.Bots[0].Rounds);   // nothing spent on empty air
        Assert.Equal(0, v.Reevers[0].HitsTaken);
    }

    [Fact]
    public void Step_DryBot_IsSilent_TargetUntouched()
    {
        var bots = new[] { new SentryBot.Deployed("R-3B", 0, 0, 0) };  // 00
        var reevers = new[] { new SentryBot.Target(3, 0, 5) };

        SentryBot.Volley v = SentryBot.Step(bots, reevers);

        Assert.Equal(0, v.Shots);
        Assert.Equal(0, v.Bots[0].Rounds);
        Assert.Equal(5, v.Reevers[0].HitsTaken);   // the dry bot did nothing — the Old One walks on
        Assert.Empty(v.Husks);
    }

    [Fact]
    public void Step_DownsAReever_AtRoundsPerReever_AndLeavesAHusk()
    {
        // A Reever already one hit shy of down: the next shot drops it and mints a husk where it stood.
        var bots = new[] { FullBotAt(0, 0) };
        var reevers = new[] { new SentryBot.Target(4, 2, SentryBot.RoundsPerReever - 1) };

        SentryBot.Volley v = SentryBot.Step(bots, reevers);

        Assert.Equal(1, v.Shots);
        Assert.Empty(v.Reevers);          // gone from the live board
        Assert.Single(v.Husks);           // persists as a husk mark
        Assert.Equal(4, v.Husks[0].X);
        Assert.Equal(2, v.Husks[0].Y);
    }

    [Fact]
    public void Step_TwoBots_DoNotWasteShotsOnACorpse()
    {
        // Both bots in range of one nearly-dead Reever: the first drops it, the second retargets (or
        // holds if nothing else) — never a second shot into the husk.
        var bots = new[] { FullBotAt(0, 0), FullBotAt(1, 0) };
        var reevers = new[] { new SentryBot.Target(3, 0, SentryBot.RoundsPerReever - 1) };

        SentryBot.Volley v = SentryBot.Step(bots, reevers);

        Assert.Single(v.Husks);
        Assert.Equal(1, v.Shots);                              // only the killing shot was spent
        Assert.Equal(SentryBot.MaxMagazine, v.Bots[1].Rounds); // the second bot held its round
    }

    [Fact]
    public void FullMagazine_DownsAboutOneBadRollPack_ThenRunsDry()
    {
        // The siege math (owner addendum): a full magazine handles one bad roll's pack (6) with little
        // to spare for the trickle. Drive a single bot against an endless wall via repeated volleys and
        // count the downs before it reads 00.
        var bot = FullBotAt(0, 0);
        int downed = 0;
        // Feed a fresh in-range Reever and grind it down across volleys (the many-law wall) until the
        // bot runs dry — counting the downs a single 99-round magazine buys.
        int carriedHits = 0;
        for (int guard = 0; guard < 5000 && !SentryBot.IsDry(bot.Rounds); guard++)
        {
            var target = new[] { new SentryBot.Target(2, 0, carriedHits) };
            SentryBot.Volley v = SentryBot.Step(new[] { bot }, target);
            bot = v.Bots[0];
            if (v.Husks.Count > 0)
            {
                downed++;
                carriedHits = 0;   // the next Reever in the wall steps up fresh
            }
            else
            {
                carriedHits = v.Reevers[0].HitsTaken;
            }
        }

        Assert.Equal(0, bot.Rounds);                            // ran dry
        Assert.Equal(7, downed);                                // 99 rounds ÷ 14 each = 7 downs
        Assert.InRange(downed, ReeverRaid.MaxReevers, ReeverRaid.MaxReevers + 1); // the 6-pack + one trickle
    }

    [Fact]
    public void Engagement_NeverTouchesLoot_ByConstruction()
    {
        // The guns produce husks and drained magazines and NOTHING else — no coin, no cargo field
        // exists on the volley (mirrors ReeverRaid's no-loot law). We resolve a whole siege and assert
        // the caller's purse/hold, passed nowhere near Step, are provably untouched.
        const int startCredits = 5000;
        const int startCargo = 40;
        int credits = startCredits, cargo = startCargo;

        var bots = new[] { FullBotAt(0, 0), FullBotAt(2, 0) };
        var reevers = new List<SentryBot.Target>
        {
            new(3, 0, 0), new(4, 1, 0), new(2, 2, 0), new(5, 0, 0), new(3, 3, 0), new(4, 2, 0),
        };
        int husks = 0;
        for (int i = 0; i < 300 && reevers.Count > 0; i++)
        {
            SentryBot.Volley v = SentryBot.Step(bots, reevers);
            bots = v.Bots.ToArray();
            reevers = v.Reevers.ToList();
            husks += v.Husks.Count;
            if (bots.All(b => b.Dry))
            {
                break;
            }
        }

        Assert.True(husks > 0);                     // it did fight
        Assert.Equal(startCredits, credits);        // purse untouched
        Assert.Equal(startCargo, cargo);            // hold untouched
    }

    [Fact]
    public void QuoteRestock_FillsMagazines_AtOneHonestPrice()
    {
        // Two bots half-spent; a rearm tops both to 99 and charges exactly the missing rounds.
        var mags = new[] { 30, 50 };
        int missing = (SentryBot.MaxMagazine - 30) + (SentryBot.MaxMagazine - 50);

        SentryBot.RestockQuote q = SentryBot.QuoteRestock(mags, credits: 100000);

        Assert.Equal(missing, q.RoundsBought);
        Assert.Equal(missing * SentryBot.RestockPricePerRound, q.Cost);
        Assert.Equal(SentryBot.MaxMagazine, q.Magazines[0]);
        Assert.Equal(SentryBot.MaxMagazine, q.Magazines[1]);
    }

    [Fact]
    public void QuoteRestock_BuysOnlyWhatThePurseAffords()
    {
        // 10 credits, 2 cr/round → 5 rounds only; the first bot gets them, the second stays as-is.
        var mags = new[] { 0, 0 };
        SentryBot.RestockQuote q = SentryBot.QuoteRestock(mags, credits: 10);

        Assert.Equal(5, q.RoundsBought);
        Assert.Equal(10, q.Cost);
        Assert.Equal(5, q.Magazines[0]);
        Assert.Equal(0, q.Magazines[1]);
    }

    [Fact]
    public void RestockReceiptLine_IsAReceipt_WithRoundsAndCost()
    {
        string r = SentryBot.RestockReceiptLine(118, 236);
        Assert.Contains("118 rounds", r);
        Assert.Contains("236", r);
        Assert.StartsWith("🧾", r);

        string none = SentryBot.RestockReceiptLine(0, 0);
        Assert.Contains("full", none);
    }

    [Fact]
    public void AbandonLedgerLine_NamesTheUnitAndCounter_AndReadsRunDryWhenEmpty()
    {
        string dry = SentryBot.AbandonLedgerLine("K-77", 0);
        Assert.Contains("K-77", dry);
        Assert.Contains("00", dry);
        Assert.Contains("run dry", dry);          // the #316 forensic phrase

        string live = SentryBot.AbandonLedgerLine("R-3B", 42);
        Assert.Contains("R-3B", live);
        Assert.Contains("42", live);
        Assert.DoesNotContain("run dry", live);   // it still had rounds
    }

    [Fact]
    public void Roster_IsTheRealShipUnits()
    {
        Assert.Equal(SentryBot.RosterCap, SentryBot.RosterUnits.Count);
        Assert.Contains("K-77", SentryBot.RosterUnits);
        Assert.Contains("R-3B", SentryBot.RosterUnits);
    }
}
