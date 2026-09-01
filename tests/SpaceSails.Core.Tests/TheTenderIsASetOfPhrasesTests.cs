namespace SpaceSails.Core.Tests;

/// <summary>
/// #1022 · <b>B-7V IS A SET OF PHRASES.</b> Owner, live (2026-08-30): <i>"The dialog, one imagines, is a set
/// of phrases to keep the customer talking :-D ... but there is something heart warming in those scenes at
/// the same time."</i>
///
/// <para>Everything he can say is authored and finite, so the only things that can break are the CHOOSING
/// laws — and every one of them is a law about a thing the player would notice and could never report
/// precisely: a beat that ends in the wrong voice, a rare thing that stopped being rare, a warning that
/// arrives one drink late, a counter that says the same sentence twice running.</para>
///
/// <para>Each guard below was proven RED by reverting the law it stands on; the revert is written beside
/// it.</para>
/// </summary>
public class TheTenderIsASetOfPhrasesTests
{
    /// <summary>A quiet sim moment to open sittings at — the seed's only clock.</summary>
    private const long Evening = 4_800;

    // ── THE CATALOGUE ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryWordHeHasIsOnTheSweep_AndNoneOfThemIsBlank()
    {
        var prose = TheTender.AllProse().ToList();

        // The plate, three openers, the rare slot, three pours, the threshold line, four idles, five in the
        // other register, and the one that follows them. Counted here so a line added to a pool without
        // being added to AllProse() — the way prose escapes a canon sweep — fails on the number.
        Assert.Equal(
            1 + TheTender.Openers.Count + 1 + TheTender.Pours.Count + 1
            + TheTender.Idles.Count + TheTender.Announcements.Count + 1,
            prose.Count);
        Assert.Equal(prose.Count, prose.Distinct().Count());
        Assert.All(prose, line => Assert.False(string.IsNullOrWhiteSpace(line)));

        // §13.8, the same list the canteen's regulars are held to. He is the chattiest fitting on the boat
        // and the one most able to say a thing that cannot be taken back.
        string[] forbidden = ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];
        foreach (string line in prose)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void ThePlateIsHis_AndTheCardCanFindIt()
    {
        Assert.Equal("🤖 B-7V · THE TENDER", TheTender.Plate);
        Assert.Contains(TheTender.Plate, TheTender.AllProse());
    }

    // ── (a) THE OTHER REGISTER NEVER STANDS ALONE ─────────────────────────────────────────────────────

    /// <summary>
    /// EVERY ANNOUNCEMENT IS FOLLOWED BY THE RECOVERY — swept over a thousand sittings' worth of beats,
    /// rolled honestly, so this reads whatever the die actually hands back rather than a case typed in here.
    ///
    /// <para><b>RED PROOF:</b> return <c>new Line(Announcements[i], string.Empty)</c> from
    /// <c>TryFlashback</c> — the shape a two-call design would fall into the first time a caller forgot the
    /// second call — and this fails on the very first flashback it finds.</para>
    /// </summary>
    [Fact]
    public void NothingHeSaysInTheOtherRegisterIsLeftHanging()
    {
        int found = 0;
        for (long moment = 0; moment < 1_000; moment++)
        {
            var sitting = new TheTender.Sitting();
            for (int beat = 1; beat <= 4; beat++)
            {
                TheTender.Line line = sitting.Open(Evening + moment, beat);
                if (!line.IsFlashback)
                {
                    Assert.Null(line.Announcement);
                    continue;
                }

                found++;
                Assert.Contains(line.Announcement!, TheTender.Announcements);
                Assert.Equal(TheTender.Recovery, line.Text);
            }
        }

        // Anti-vacuous: a sweep that never rolled one would pass over a channel that had stopped working.
        Assert.True(found > 100,
            $"only {found} flashbacks in a thousand sittings — either the roll has gone cold or this sweep "
            + "is proving nothing about the pairing.");
    }

    // ── (b) THE ROOM IS LARGER ONCE ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// IT CANNOT HAPPEN TWICE IN ONE SITTING, even with the roll forced on every single beat — which is the
    /// dev cheat's own state, and therefore the loudest way to break this law by accident.
    ///
    /// <para><b>RED PROOF:</b> delete the <c>FlashbackSpent</c> early-out in <c>TryFlashback</c> and this
    /// fails at 20 flashbacks instead of one.</para>
    /// </summary>
    [Fact]
    public void TheRoomIsOnlyLargerOncePerSitting()
    {
        var sitting = new TheTender.Sitting();
        int flashbacks = 0;
        for (int beat = 1; beat <= 20; beat++)
        {
            TheTender.Line line = beat % 2 == 0
                ? sitting.Pour(Evening, beat, totNumber: 1, forceFlashback: true)
                : sitting.Open(Evening, beat, forceFlashback: true);
            if (line.IsFlashback)
            {
                flashbacks++;
            }
        }

        Assert.Equal(1, flashbacks);
        Assert.True(sitting.FlashbackSpent);

        // …and a NEW sitting is a new night: the once-ness belongs to the visit, not to the process.
        Assert.True(new TheTender.Sitting().Open(Evening, 1, forceFlashback: true).IsFlashback);
    }

    [Fact]
    public void TheCheatForcesTheRollAndNeverTheContent()
    {
        // #746's philosophy, one door along: which line he reaches for is still his own salted pick, so a
        // tester watches the beat a captain would get. Forced across many moments, every announcement in
        // the pool comes up.
        var reached = new HashSet<string>(StringComparer.Ordinal);
        for (long moment = 0; moment < 200; moment++)
        {
            TheTender.Line line = new TheTender.Sitting().Open(Evening + moment, 1, forceFlashback: true);
            Assert.True(line.IsFlashback);
            reached.Add(line.Announcement!);
        }

        Assert.Equal(TheTender.Announcements.Count, reached.Count);
    }

    [Fact]
    public void TheRollIsGenuinelyRare_AndItIsTheHouseDie()
    {
        // Rare on purpose: a channel that opened every visit would be a feature rather than a thing that
        // happens to him. Counted over the first beat of ten thousand sittings.
        int fired = 0;
        for (long moment = 0; moment < 10_000; moment++)
        {
            if (new TheTender.Sitting().Open(Evening + moment, 1).IsFlashback)
            {
                fired++;
            }
        }

        double rate = fired / 10_000.0;
        Assert.InRange(rate, 1.0 / 16, 1.0 / 8);

        // …and it is the shared engine folding the seed, not a random of its own.
        Assert.Equal(12, TheTender.FlashbackFaces);
        bool byHand = DiceRule.Roll(DiceRule.Seed("tender:flashback", Evening, 1), TheTender.FlashbackFaces).Face == 1;
        Assert.Equal(byHand, new TheTender.Sitting().Open(Evening, 1).IsFlashback);
    }

    // ── (c) THE THRESHOLD SPEAKS ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE POUR THAT CROSSES THE THRESHOLD SAYS THE THRESHOLD'S LINE, and every pour under it says one of
    /// the pour pool's. The threshold is read off <see cref="NerveModel.DrunkAt"/> — the game's one
    /// drunkenness law — rather than a number typed in here, so the two can never drift apart.
    ///
    /// <para><b>RED PROOF:</b> drop the <c>DrunkAt</c> fork out of <c>Pour</c> and the third tot reads a
    /// pour-pool line: <i>"Assert.Equal() Failure"</i> naming the ordinary line where the advice should
    /// be.</para>
    /// </summary>
    [Fact]
    public void TheSetAdvisesOnThePourThatCrossesTheThreshold()
    {
        var sitting = new TheTender.Sitting();

        for (int tot = 1; tot < NerveModel.DrunkTotCount; tot++)
        {
            TheTender.Line under = sitting.Pour(Evening, tot, tot);
            Assert.Contains(under.Text, TheTender.Pours);
            Assert.NotEqual(TheTender.LastCall, under.Text);
        }

        // …and the tot that DrunkAt calls drunk — the third, the one that makes the deck tilty and restores
        // nothing — is the one he says it on, and every tot after it.
        for (int tot = NerveModel.DrunkTotCount; tot <= NerveModel.DrunkTotCount + 3; tot++)
        {
            TheTender.Line over = sitting.Pour(Evening, tot, tot);
            Assert.Equal(TheTender.LastCall, over.Text);
            Assert.Null(over.Announcement);
        }
    }

    [Fact]
    public void TheThresholdOutranksTheRareRoll_AndSpendsNothing()
    {
        // At the threshold the set has one thing to say and says it. The roll is not made, so the sitting
        // can still get its larger room later — a beat swallowed by a warning is not a beat spent.
        var sitting = new TheTender.Sitting();
        TheTender.Line advised = sitting.Pour(Evening, 1, NerveModel.DrunkTotCount, forceFlashback: true);

        Assert.Equal(TheTender.LastCall, advised.Text);
        Assert.False(advised.IsFlashback);
        Assert.False(sitting.FlashbackSpent);
        Assert.True(sitting.Open(Evening, 2, forceFlashback: true).IsFlashback);
    }

    // ── (d) HE DOES NOT SAY THE SAME THING TWICE RUNNING ──────────────────────────────────────────────

    /// <summary>
    /// #1006's law, one door along: within a sitting the pick is walked forward past anything already said,
    /// so a full turn of a pool is repeat-free. Asserted over thirty sittings, because a bare salted index
    /// happens to come out clean about one sitting in ten and a single-sitting guard would be a coin flip
    /// dressed as a law.
    ///
    /// <para><b>RED PROOF:</b> strip the collision walk and the <c>said</c> memory out of <c>Pick</c> — the
    /// hour-only shape #1006 was filed against — and this fails naming a sitting where he said the same
    /// thing twice in four looks. (28 of 30 sittings failed on the run that proved it.)</para>
    /// </summary>
    [Fact]
    public void ASecondLookInTheSameSitting_NeverReadsTheCardHeJustRead()
    {
        for (long moment = 0; moment < 30; moment++)
        {
            var sitting = new TheTender.Sitting();
            var idles = new List<string>();

            // Walk beats until a full turn of the idle pool has been heard. The greeting and, once in a
            // while, a beat in the other register are not idles and are simply not counted.
            for (int beat = 1; idles.Count < TheTender.Idles.Count && beat <= 40; beat++)
            {
                TheTender.Line line = sitting.Open(Evening + moment, beat);
                if (!line.IsFlashback && TheTender.Idles.Contains(line.Text))
                {
                    idles.Add(line.Text);
                }
            }

            Assert.Equal(TheTender.Idles.Count, idles.Count);
            Assert.Equal(TheTender.Idles.Count, idles.Distinct().Count());
        }
    }

    [Fact]
    public void APourAfterAPour_NeverRepeatsWhileThePoolLasts()
    {
        for (long moment = 0; moment < 30; moment++)
        {
            var sitting = new TheTender.Sitting();
            var poured = new List<string>();

            for (int beat = 1; poured.Count < TheTender.Pours.Count && beat <= 40; beat++)
            {
                TheTender.Line line = sitting.Pour(Evening + moment, beat, totNumber: 1);
                if (!line.IsFlashback)
                {
                    poured.Add(line.Text);
                }
            }

            Assert.Equal(TheTender.Pours.Count, poured.Count);
            Assert.Equal(TheTender.Pours.Count, poured.Distinct().Count());
        }
    }

    [Fact]
    public void TheGreetingIsSaltedOnTheMoment_SoTheCounterIsNotOneStream()
    {
        // Different sittings reach for different openers — the salt is the moment and the beat, and without
        // it every night at the counter would open on the same sentence for ever.
        var reached = new HashSet<string>(StringComparer.Ordinal);
        for (long moment = 0; moment < 400; moment++)
        {
            TheTender.Line line = new TheTender.Sitting().Open(Evening + moment, 1);
            if (!line.IsFlashback)
            {
                reached.Add(line.Text);
            }
        }

        // All three of the ordinary openers, and the rare slot as well.
        Assert.All(TheTender.Openers, opener => Assert.Contains(opener, reached));
        Assert.Contains(TheTender.RareOpener, reached);
    }

    [Fact]
    public void TheRareSlotIsRare_AndTheOrdinaryThreeAreOrdinary()
    {
        int rare = 0;
        int ordinary = 0;
        for (long moment = 0; moment < 4_000; moment++)
        {
            TheTender.Line line = new TheTender.Sitting().Open(Evening + moment, 1);
            if (line.IsFlashback)
            {
                continue;
            }

            if (line.Text == TheTender.RareOpener)
            {
                rare++;
            }
            else
            {
                ordinary++;
            }
        }

        Assert.Equal(8, TheTender.RareOpenerFaces);
        Assert.InRange(rare / (double)(rare + ordinary), 1.0 / 12, 1.0 / 5);
    }

    // ── THE SITTING IS A VISIT, NOT A CARD-OPEN ───────────────────────────────────────────────────────

    [Fact]
    public void HeGreetsYouOnceAVisit_AndTalksToYouEveryTimeAfter()
    {
        // The set of phrases doing its job: come back and he does not start over, he keeps you talking.
        var sitting = new TheTender.Sitting();
        var said = new List<string>();
        for (int beat = 1; beat <= 6; beat++)
        {
            TheTender.Line line = sitting.Open(Evening, beat);
            if (!line.IsFlashback)
            {
                said.Add(line.Text);
            }
        }

        Assert.True(said[0] == TheTender.RareOpener || TheTender.Openers.Contains(said[0]));
        Assert.All(said.Skip(1), line => Assert.Contains(line, TheTender.Idles));
        Assert.True(sitting.Greeted);
    }

    [Fact]
    public void AFirstLookThatCameUpFlashback_StillGetsItsGreeting()
    {
        // The greeting is spent when it is GIVEN, not when the card first opens — otherwise a captain whose
        // very first look landed on the rare beat would never be greeted at all that visit.
        var sitting = new TheTender.Sitting();
        Assert.True(sitting.Open(Evening, 1, forceFlashback: true).IsFlashback);
        Assert.False(sitting.Greeted);

        TheTender.Line second = sitting.Open(Evening, 2);
        Assert.True(second.Text == TheTender.RareOpener || TheTender.Openers.Contains(second.Text));
        Assert.True(sitting.Greeted);
    }

    [Fact]
    public void TheVisitAndTheSpreeLapseOnTheSameClock()
    {
        // One number, in the drink law's own file, beside the threshold it feeds. The client starts a fresh
        // sitting on exactly the gap that starts a fresh tot count, so the tender's memory and the rum
        // ledger cannot disagree about whether the captain ever left the counter.
        Assert.Equal(90_000, NerveModel.SpreeGapMs);
    }

    [Fact]
    public void ThePickIsDeterministic_TheOneEngineAndNoPrivateRandom()
    {
        // Determinism is law in Core: same moment, same beat, same words — every time, no clock read.
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(
                new TheTender.Sitting().Open(Evening, 7).Text,
                new TheTender.Sitting().Open(Evening, 7).Text);
            Assert.Equal(
                new TheTender.Sitting().Pour(Evening, 7, totNumber: 1).Text,
                new TheTender.Sitting().Pour(Evening, 7, totNumber: 1).Text);
        }
    }
}
