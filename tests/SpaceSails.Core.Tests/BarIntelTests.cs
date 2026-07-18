namespace SpaceSails.Core.Tests;

/// <summary>
/// A round for the room loosens tongues (owner 2026-07-18): after you stand a round, each regular who
/// drank rolls — on their own initiative — whether to volunteer something. These pin the deterministic
/// laws: same seed → same tier; known contacts (goodwill-weighted) beat strangers; strangers give only
/// vague color; and the "overheard at the bar" book appends durably and stays capped.
/// </summary>
public class BarIntelTests
{
    [Fact]
    public void Volunteer_IsDeterministic_SameSeedSameTier()
    {
        ulong seed = DiceRule.Seed("round-tip:GILT-EYE", 4242);
        TipTier a = RoundTips.Volunteer(seed, goodwill: 4, known: true);
        TipTier b = RoundTips.Volunteer(seed, goodwill: 4, known: true);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Volunteer_Stranger_NeverBeatsVagueColor()
    {
        // A loosened stranger offers atmosphere at best — never a solid or choice tip, whatever the roll.
        for (long t = 0; t < 500; t++)
        {
            TipTier tier = RoundTips.Volunteer(DiceRule.Seed("stranger", t), goodwill: 0, known: false);
            Assert.True(tier is TipTier.None or TipTier.Vague, $"stranger produced {tier} at t={t}");
        }
    }

    [Fact]
    public void Volunteer_KnownContact_IsNeverWorseThanAStranger_OnTheSameRoll()
    {
        // Goodwill-weighting: for any given roll, a known (warm) contact volunteers material at least as
        // good as a stranger would — and across the sweep, strictly better material appears for the known.
        bool sawKnownBeatStranger = false;
        for (long t = 0; t < 500; t++)
        {
            ulong seed = DiceRule.Seed("weight", t);
            TipTier stranger = RoundTips.Volunteer(seed, goodwill: 0, known: false);
            TipTier known = RoundTips.Volunteer(seed, goodwill: 6, known: true);
            Assert.True(known >= stranger, $"known {known} < stranger {stranger} at t={t}");
            sawKnownBeatStranger |= known > stranger;
        }

        Assert.True(sawKnownBeatStranger, "a known contact should produce better material at least sometimes");
    }

    [Fact]
    public void Volunteer_HigherGoodwill_ShiftsQualityUpward()
    {
        // More goodwill → better tips on average (the warmth weight). Compare the tier distribution of a
        // cold acquaintance against a deep friend over the same seed sweep.
        int coldSolidPlus = 0, warmSolidPlus = 0;
        for (long t = 0; t < 600; t++)
        {
            ulong seed = DiceRule.Seed("goodwill-sweep", t);
            if (RoundTips.Volunteer(seed, goodwill: 0, known: true) >= TipTier.Solid)
            {
                coldSolidPlus++;
            }
            if (RoundTips.Volunteer(seed, goodwill: 6, known: true) >= TipTier.Solid)
            {
                warmSolidPlus++;
            }
        }

        Assert.True(warmSolidPlus > coldSolidPlus,
            $"a deep friend should yield more solid+ tips (warm={warmSolidPlus}, cold={coldSolidPlus})");
    }

    [Fact]
    public void OverheardLog_Append_KeepsOrder_AndIsPure()
    {
        IReadOnlyList<OverheardLine> a = OverheardLog.Append([], new OverheardLine("first", 1, "A", "BAR"));
        IReadOnlyList<OverheardLine> b = OverheardLog.Append(a, new OverheardLine("second", 2, "B", "BAR"));
        Assert.Single(a);                       // the input was not mutated
        Assert.Equal(2, b.Count);
        Assert.Equal("first", b[0].Text);       // oldest first
        Assert.Equal("second", b[1].Text);
    }

    [Fact]
    public void OverheardLog_Append_CapsToMostRecent()
    {
        IReadOnlyList<OverheardLine> log = [];
        for (int i = 0; i < OverheardLog.Cap + 10; i++)
        {
            log = OverheardLog.Append(log, new OverheardLine($"line {i}", i, "src", "BAR"));
        }

        Assert.Equal(OverheardLog.Cap, log.Count);
        Assert.Equal($"line {OverheardLog.Cap + 9}", log[^1].Text);        // newest kept
        Assert.Equal($"line 10", log[0].Text);                            // oldest trimmed off the front
    }

    // ── #347 rumor → ledger, GROUPED PER CONTACT (the bug: it never crossed into the ledger at all) ──

    [Fact]
    public void PerContact_EmptyBook_ProjectsNothing()
    {
        Assert.Empty(OverheardLog.PerContact(null));
        Assert.Empty(OverheardLog.PerContact([]));
    }

    [Fact]
    public void PerContact_GroupsBySource_NewestFirstWithinAndAcross()
    {
        // Coil spoke at t=1 and again at t=4; the barkeep at t=2. The freshest talk (Coil's t=4) heads the
        // section, Coil's own lines come back newest-first, and every line stays with who told you.
        IReadOnlyList<OverheardLine> log =
        [
            new OverheardLine("Coil: a soft price at the next berth", 1, "MADAM COIL", "THE CINDER LOUNGE"),
            new OverheardLine("Keep: quiet week for gossip", 2, "Ember Vance", "THE CINDER LOUNGE"),
            new OverheardLine("Coil: a ghost runs dark tonight", 4, "MADAM COIL", "THE CINDER LOUNGE"),
        ];

        IReadOnlyList<LedgerRumor> ledger = OverheardLog.PerContact(log);

        Assert.Equal(2, ledger.Count);
        Assert.Equal("MADAM COIL", ledger[0].Source);        // freshest source on top
        Assert.Equal("Ember Vance", ledger[1].Source);
        Assert.Equal(2, ledger[0].Lines.Count);
        Assert.Equal("Coil: a ghost runs dark tonight", ledger[0].Lines[0].Text); // newest first within
        Assert.Equal("Coil: a soft price at the next berth", ledger[0].Lines[1].Text);
        Assert.Equal(4, ledger[0].LatestSimTime);
        Assert.Equal("THE CINDER LOUNGE", ledger[0].LatestBar);
    }

    [Fact]
    public void PerContact_KeepsEveryLine_AndNamesABlankSourceHonestly()
    {
        IReadOnlyList<OverheardLine> log =
        [
            new OverheardLine("unattributed murmur", 1, "", "THE TILT BAR"),
        ];

        IReadOnlyList<LedgerRumor> ledger = OverheardLog.PerContact(log);
        Assert.Single(ledger);
        Assert.Equal("The bar", ledger[0].Source); // a sourceless line still lands, filed to the room
        Assert.Single(ledger[0].Lines);
    }
}
