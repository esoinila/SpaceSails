namespace SpaceSails.Core.Tests;

/// <summary>
/// #635 · THE ARC HAD NO FRONT DOOR — the guards for the one that was built.
///
/// <para>The finding, verbatim from the issue: <i>"The KAAMOS arc has no inciting hook. Nothing points a
/// player at Ringside Exchange's dedication plaque rather than any of the other six plates in the system;
/// the ledger's card only appears once a shard is already held. So the longest-prepared arc in the game is
/// invisible until a player trips over it by accident."</i></para>
///
/// <para>What is built is option 3, the mission-desk contract that bounces: a freight agent is holding a
/// docket the board keeps returning, and pays a small fee to have it filed under somebody else's hull. It
/// bounces off yours too. Four things have to be true of that, and each is a test here:</para>
///
/// <list type="number">
/// <item><b>It opens the arc without being part of it.</b> The bounce hands over no shard, moves no
/// threshold and opens no gate — but it raises the ledger card, which is the thing the arc never had.</item>
/// <item><b>It is a DOOR, not an event.</b> A hook that a captain meets one watch in a dozen is not a front
/// door; a hook that is in every room every watch is wallpaper. The rarity is measured against what the
/// generator actually produces, both ways — the fifth bug class ("a threshold that selects everything") is
/// exactly what an eyeballed one-in-N does here.</item>
/// <item><b>It states nothing.</b> A docket may say a berth is HELD and that a window is not open. It may
/// not say who is holding it. Canon is not softened by the fact that this is the arc's own front door — it
/// is hardened, because this is the sentence most captains will read first.</item>
/// <item><b>It survives a reload and dies with the universe.</b> The one-time edge, the vault round-trip,
/// and the per-thread wipe.</item>
/// </list>
///
/// <para><b>Proven RED.</b> Each of these was watched failing before it was trusted — see the PR body for
/// which line was reverted for which test.</para>
/// </summary>
public class TheKaamosFrontDoorTests
{
    /// <summary>Every bar a captain can actually dock at and stand in, in <c>sol.json</c>. The bounce is
    /// seeded per (bar, watch-day), so "can a captain find the door" is a question about these rooms and
    /// nothing else. Typed out rather than loaded because the guard is about the SEED, not the scenario —
    /// but each id is a real haven in the shipped scenario, checked by eye against the file.</summary>
    private static readonly string[] Bars =
    [
        "cinder-roost", "selene-gate", "the-space-bar", "red-eye",
        "ringside-exchange", "the-tilt", "the-deep",
    ];

    private static KaamosProgress Bounced()
    {
        var p = new KaamosProgress();
        Assert.True(p.MarkBerthFilingBounced());
        return p;
    }

    // ── 1 · The door opens the arc without being part of it ──────────────────────────────────────────────

    /// <summary>The whole point: a captain holding nothing but a returned filing HAS a KAAMOS card. Before
    /// #635 the ledger's only KAAMOS entry appeared strictly after a shard was in hand, so the one place in
    /// the game that says this arc exists appeared only to captains who had already started it.</summary>
    [Fact]
    public void ABouncedFilingAloneIsEnoughToPutTheArcInTheLedger()
    {
        KaamosProgress bounced = Bounced();

        Assert.Equal(KaamosLore.BounceHeadline, KaamosLore.LedgerHeadline(bounced));
        Assert.Equal(KaamosLore.BounceLedgerLine, KaamosLore.LedgerProgressLine(bounced));

        // And it must not read as a progress bar for a quest nobody has been given.
        Assert.DoesNotContain("0 of", KaamosLore.LedgerHeadline(bounced), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shard", KaamosLore.LedgerHeadline(bounced), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A captain who has never had a filing bounce and holds no shard still has no card — the door
    /// is a door, not a permanent fixture of the ledger.</summary>
    [Fact]
    public void AFreshCaptainHasNoArcAtAll()
    {
        var fresh = new KaamosProgress();
        Assert.False(fresh.BerthFilingBounced);
        Assert.Equal(0, fresh.Count);
    }

    /// <summary>The bounce credits NOTHING. It is not a sixth shard by another name: the intel count, the
    /// capstone gate and the reach are all exactly where they were. If this ever fails, the front door has
    /// quietly become a free step of the arc and every threshold in it has moved.</summary>
    [Fact]
    public void TheBounceIsNotAShardAndMovesNoThreshold()
    {
        var before = new KaamosProgress();
        int intelBefore = KaamosLore.IntelAssembled(before);
        bool gateBefore = KaamosLore.HasEnoughIntelToEarnTheKey(before);
        bool reachBefore = KaamosLore.CanReachEnceladus(before);

        KaamosProgress after = Bounced();

        Assert.Equal(intelBefore, KaamosLore.IntelAssembled(after));
        Assert.Equal(gateBefore, KaamosLore.HasEnoughIntelToEarnTheKey(after));
        Assert.Equal(reachBefore, KaamosLore.CanReachEnceladus(after));
        Assert.Equal(0, after.Count);
        Assert.Empty(after.AssembledIds);
    }

    /// <summary>Once a shard is in hand the ledger goes back to counting shards — the door's line is for
    /// the state where there is nothing else to say, and must not swallow the real arc's readout.</summary>
    [Fact]
    public void OnceAShardIsHeldTheLedgerCountsShardsAgain()
    {
        KaamosProgress p = Bounced();
        Assert.True(p.Assemble("listed-berth"));

        Assert.NotEqual(KaamosLore.BounceHeadline, KaamosLore.LedgerHeadline(p));
        Assert.NotEqual(KaamosLore.BounceLedgerLine, KaamosLore.LedgerProgressLine(p));
        Assert.Contains("1 of", KaamosLore.LedgerHeadline(p), StringComparison.Ordinal);
    }

    // ── 2 · It is a door, not an event ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// The rarity, measured against what the seed actually produces rather than chosen by eye — the fifth
    /// named bug class, whose type specimen is a threshold set to 34 du when the nearest real case is 34.2.
    ///
    /// <para>Two-sided on purpose, and it is the two-sidedness that makes it evidence: a bounce that never
    /// fires and a bounce that fires every watch would BOTH pass a one-sided check, and both are the bug.
    /// The claim is "a captain who drinks in the same bar for a week meets it, and does not meet it every
    /// single night".</para>
    /// </summary>
    [Fact]
    public void EveryBarOffersTheDoorWithinAWeekAndNoBarOffersItEveryNight()
    {
        const int week = 7;
        foreach (string bar in Bars)
        {
            int hits = 0;
            for (int day = 0; day < week; day++)
            {
                if (KaamosFind.BounceAtBar(bar, day))
                {
                    hits++;
                }
            }

            Assert.True(hits > 0, $"{bar}: the front door never opened in {week} watches — that is no door at all");
            Assert.True(hits < week, $"{bar}: the front door stood open on all {week} watches — that is wallpaper");
        }
    }

    /// <summary>Deterministic per (bar, watch): asking twice in the same room on the same watch answers the
    /// same, so walking away from the card and coming back does not re-roll the world.</summary>
    [Fact]
    public void TheSameBarOnTheSameWatchAlwaysAnswersTheSame()
    {
        foreach (string bar in Bars)
        {
            for (int day = 0; day < 12; day++)
            {
                Assert.Equal(KaamosFind.BounceAtBar(bar, day), KaamosFind.BounceAtBar(bar, day));
            }
        }
    }

    /// <summary>The dev seat, per <c>Map.Sim.cs</c>'s own rule that a scene nobody can reach on demand is a
    /// scene that ships broken. <c>/map?kaamos=bounce</c> forces the agent into whatever bar you walk into,
    /// including one that has no id at all.</summary>
    [Fact]
    public void TheForcedSeatOpensTheDoorAnywhere()
    {
        Assert.True(KaamosFind.BounceAtBar("some-bar-nobody-seeded", 0, forced: true));
        Assert.True(KaamosFind.BounceAtBar("", 99, forced: true));
        Assert.False(KaamosFind.BounceAtBar("", 99));
    }

    // ── 3 · It states nothing ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The docket says HELD and says a window is not open, because that is what a returned filing says. It
    /// may not say who is holding it.
    ///
    /// <para>The forbidden list is the writers' bible's §2 vocabulary — the truth this arc keeps by not
    /// answering, and the one the convergence card (#422) already spends too early. The front door is the
    /// sentence most captains will read FIRST, so it is held to the tightest version of the rule.</para>
    /// </summary>
    [Fact]
    public void TheFrontDoorSaysHeldAndNeverSaysWho()
    {
        string[] doorProse =
        [
            KaamosLore.BounceOfferTitle,
            KaamosLore.BounceOfferBlurb(KaamosFind.BounceFilingFee),
            KaamosLore.BounceReceipt(KaamosFind.BounceFilingFee),
            KaamosLore.BounceHeadline,
            KaamosLore.BounceLedgerLine,
            KaamosLore.BouncePlate.Title,
            KaamosLore.BouncePlate.Caption,
        ];

        // It has to do its ONE job: the word that makes a dead berth interesting.
        Assert.Contains(doorProse, s => s.Contains("HELD", StringComparison.Ordinal));

        string[] forbidden =
        [
            "forty", "Vantar", "wintering", "lucid", "awake", "one voice", "continuous",
            "KAAMOS", "polar night",
        ];
        foreach (string line in doorProse)
        {
            foreach (string word in forbidden)
            {
                Assert.False(line.Contains(word, StringComparison.OrdinalIgnoreCase),
                    $"the front door said \"{word}\" out loud: {line}");
            }
        }
    }

    /// <summary>A paid step never hides its price. The bar seam already paid for this lesson once, with a
    /// button that read "Ask about KAAMOS" and took 1,200 cr the instant it was clicked.</summary>
    [Fact]
    public void TheOfferNamesItsFeeOnItsOwnCard()
    {
        int fee = KaamosFind.BounceFilingFee;
        Assert.True(fee > 0, "a zero fee reads as 'On the house' on the offer card and the beat becomes a gift");
        Assert.Contains($"{fee:N0}", KaamosLore.BounceOfferBlurb(fee), StringComparison.Ordinal);
        Assert.Contains($"{fee:N0}", KaamosLore.BounceReceipt(fee), StringComparison.Ordinal);
    }

    /// <summary>The plate is keyed to no fragment — deliberately, since the bounce is not one — so nothing
    /// else can hold it to a painted file. This does.</summary>
    [Fact]
    public void TheFrontDoorsPlateIsNotSmuggledIntoTheFragmentPlates()
    {
        Assert.DoesNotContain(KaamosLore.AllPlates, kv => kv.Value.ArtFile == KaamosLore.BouncePlate.ArtFile);
        Assert.StartsWith("art/", KaamosLore.BouncePlate.ArtFile, StringComparison.Ordinal);
        Assert.EndsWith(".jpg", KaamosLore.BouncePlate.ArtFile, StringComparison.Ordinal);
    }

    // ── 4 · It survives a reload and dies with the universe ──────────────────────────────────────────────

    /// <summary>One-time edge: the second bounce is not a bounce. The client narrates on the true return,
    /// so a repeat that reported true would show the card again at the next counter.</summary>
    [Fact]
    public void MarkingTheBounceIsAOneTimeEdge()
    {
        var p = new KaamosProgress();
        Assert.True(p.MarkBerthFilingBounced());
        Assert.False(p.MarkBerthFilingBounced());
        Assert.True(p.BerthFilingBounced);
    }

    /// <summary>Round-trips through the vault section, and a pre-#635 save (no property, hence false) simply
    /// has no front door open — exactly as a captain who never met the agent.</summary>
    [Fact]
    public void TheBounceRoundTripsThroughTheVaultAndDefaultsClosed()
    {
        KaamosProgress saved = Bounced();
        Assert.True(saved.Assemble("cold-pod"));

        KaamosSection section = VaultMapper.ToSection(saved);
        Assert.True(section.BerthFilingBounced);

        var loaded = new KaamosProgress();
        VaultMapper.Apply(section, loaded);
        Assert.True(loaded.BerthFilingBounced);
        Assert.True(loaded.Has("cold-pod"));

        var old = new KaamosProgress();
        VaultMapper.Apply(new KaamosSection { AssembledFragmentIds = ["cold-pod"] }, old);
        Assert.False(old.BerthFilingBounced);
        Assert.True(old.Has("cold-pod"));
    }

    /// <summary>A new voyage is a new universe: the door closes with everything else.</summary>
    [Fact]
    public void ANewVoyageHasNeverHadAFilingComeBack()
    {
        KaamosProgress p = Bounced();
        Assert.True(p.Assemble("listed-berth"));

        p.Clear();

        Assert.False(p.BerthFilingBounced);
        Assert.Equal(0, p.Count);
    }
}
