namespace SpaceSails.Core.Tests;

/// <summary>
/// #422 · THE NEBULA MUTUAL STORY PASS — the sentences held to the sim.
///
/// <para>Walking arc 2 end to end on 2026-08-02, the afternoon after the KAAMOS walk (#411/#634), turned up
/// the house's third and commonest bug class in the same place its sibling had it, and three more beside
/// it.</para>
///
/// <list type="bullet">
/// <item><b>The Captain's-ledger card counted down to the wrong number</b> — the exact twin of the KAAMOS
/// finding. It printed <i>"the shape isn't clear yet — N more shards to read it"</i> with N measured against
/// the whole intel pool (five) instead of <see cref="NebulaLore.IntelNeededToUnlock"/> (four), so it was
/// always exactly one shard pessimistic: a captain holding three was told two more, and one more opened the
/// contract.</item>
/// <item><b>The marquee card repeated itself.</b> The convergence reveal — the biggest card in the game —
/// closed on <i>"Every death you have died was a withdrawal. The same forty names, the same lucid dark."</i>,
/// two sentences copied verbatim out of the paragraph directly above them.</item>
/// <item><b>One button, two different acts.</b> The bar seam read <i>"▓ Ask about NEBULA"</i> for both of its
/// steps, including the one where nobody is asked anything — the capstone is your own gathered small print
/// answering itself on your own table.</item>
/// <item><b>A parenthesised instruction to the player</b> stood in for the poster's first-read tell:
/// <i>"(Read it closely — come back and look again.)"</i></item>
/// </list>
///
/// <para>The fix, as in the sibling arc, was to build those sentences where the predicates live, so a test
/// can ask the arc what it SAYS and compare it with what it DOES. Each test below was proven RED against the
/// shipped behaviour before the fix went in.</para>
///
/// <para><b>What arc 2 did NOT share</b> is the KAAMOS pass's second bug. That capstone's authored prose
/// named four specific shards although its gate takes any four of five; this capstone's prose names none.
/// <see cref="TheCapstonesOwnLoreNamesNoShard_SoNothingCanPasteAListOfPiecesNobodyFound"/> pins that it stays
/// that way — an invariant, not a repaired defect.</para>
/// </summary>
public class TheNebulaLedgerTellsTheTruthTests
{
    private const string KeyId = "policy-terms";

    private static IReadOnlyList<string> IntelIds => [.. NebulaLore.IntelFragments.Select(f => f.Id)];

    private static NebulaProgress Holding(params string[] ids)
    {
        var p = new NebulaProgress();
        foreach (string id in ids)
        {
            Assert.True(p.Assemble(id), $"{id} is not a real pool fragment");
        }
        return p;
    }

    // ── 1 · The countdown counts down to the GATE, not to the pool. ──

    [Fact]
    public void TheLedgerCountdownIsExactlyHowManyMoreShardsTheGateWANTS()
    {
        // The honest reading of the sentence: assemble the number of shards the card asks for and the
        // contract must become earnable; assemble one fewer and it must not. This is the assertion the old
        // pool-sized countdown fails — it asked for five-minus-held, which is one too many at every step.
        for (int held = 0; held < NebulaLore.IntelNeededToUnlock; held++)
        {
            var p = Holding([.. IntelIds.Take(held)]);
            int asked = NebulaLore.IntelStillNeeded(p);

            Assert.Contains($"{asked} more shard", NebulaLore.LedgerProgressLine(p));
            Assert.False(p.HasEnoughIntelToEarnTheContract, $"holding {held}, the gate should still be shut");

            // One short of what the card asked for: still shut.
            var short1 = Holding([.. IntelIds.Take(held)]);
            foreach (string id in IntelIds.Skip(held).Take(asked - 1))
            {
                short1.Assemble(id);
            }
            Assert.False(short1.HasEnoughIntelToEarnTheContract,
                $"holding {held}, the card asked for {asked}; {asked - 1} must NOT be enough");

            // Exactly what the card asked for: open.
            var exact = Holding([.. IntelIds.Take(held)]);
            foreach (string id in IntelIds.Skip(held).Take(asked))
            {
                exact.Assemble(id);
            }
            Assert.True(exact.HasEnoughIntelToEarnTheContract,
                $"holding {held}, the card asked for {asked} and {asked} must be enough");
        }
    }

    [Fact]
    public void TheCountdownIsSingularWhenOneShardIsLeft_AndGoneOnceTheShapeIsClear()
    {
        var one = Holding([.. IntelIds.Take(NebulaLore.IntelNeededToUnlock - 1)]);
        Assert.Equal(1, NebulaLore.IntelStillNeeded(one));
        Assert.Contains("1 more shard to read it", NebulaLore.LedgerProgressLine(one));
        Assert.DoesNotContain("1 more shards", NebulaLore.LedgerProgressLine(one));

        var enough = Holding([.. IntelIds.Take(NebulaLore.IntelNeededToUnlock)]);
        Assert.Equal(0, NebulaLore.IntelStillNeeded(enough));
        Assert.DoesNotContain("more shard", NebulaLore.LedgerProgressLine(enough));
        Assert.Contains("Enough of the small print to earn the contract", NebulaLore.LedgerProgressLine(enough));
    }

    [Fact]
    public void TheCountdownNeverGoesNegative_EvenHoldingTheWholePool()
    {
        var all = Holding([.. IntelIds]);
        Assert.Equal(0, NebulaLore.IntelStillNeeded(all));
        Assert.DoesNotContain("more shard", NebulaLore.LedgerProgressLine(all));
    }

    // ── 2 · The headline counts clauses of small print; the contract is not one of them. ──

    [Fact]
    public void TheHeadlineCountsIntelClauses_AndTheContractIsNeverOneOfThem()
    {
        var p = Holding([.. IntelIds.Take(NebulaLore.IntelNeededToUnlock)]);
        Assert.Contains($"{NebulaLore.IntelNeededToUnlock} of {NebulaLore.IntelFragments.Count()} clauses",
            NebulaLore.LedgerHeadline(p));
        Assert.DoesNotContain("the contract in hand", NebulaLore.LedgerHeadline(p));

        p.Assemble(KeyId);
        // Holding the contract must not inflate the clause count — it is the capstone, not a clause.
        Assert.Contains($"{NebulaLore.IntelNeededToUnlock} of {NebulaLore.IntelFragments.Count()} clauses",
            NebulaLore.LedgerHeadline(p));
        Assert.Contains("the contract in hand", NebulaLore.LedgerHeadline(p));
    }

    // ── 3 · The capstone credits nothing the captain never found (the KAAMOS bug arc 2 does NOT have). ──

    [Fact]
    public void TheCapstonesOwnLoreNamesNoShard_SoNothingCanPasteAListOfPiecesNobodyFound()
    {
        // The gate takes ANY four of five, and the bar seam offers the capstone the moment it opens — so any
        // fixed list of pieces baked into the capstone's prose would, on some legitimate arrival, credit a
        // shard the captain never found. This capstone's prose names none ("the pieces resolve into the
        // clause the sales voice skips"), and that is what keeps it honest. An invariant, not a repair.
        foreach (NebulaFragment f in NebulaLore.IntelFragments)
        {
            Assert.DoesNotContain(f.Title, NebulaLore.KeyFragment.Lore, StringComparison.OrdinalIgnoreCase);
        }

        // And it reads identically however the captain arrived, because there is nothing in it to vary.
        var viaPoster = Holding("rebirth-glitch", "fine-print", "adjuster-tell", "collector-writ");
        var viaClinic = Holding("rebirth-glitch", "adjuster-tell", "collector-writ", "clinic-ledger");
        viaPoster.Assemble(KeyId);
        viaClinic.Assemble(KeyId);
        Assert.True(viaPoster.KnowsTheTruth);
        Assert.True(viaClinic.KnowsTheTruth);
        Assert.Equal(NebulaLore.LedgerProgressLine(viaPoster), NebulaLore.LedgerProgressLine(viaClinic));
    }

    [Fact]
    public void HoldingEveryClauseButNotTheContractIsNotKnowingTheTruth()
    {
        // The testing guide told a tester "at N ≥ 4 the one-time THE POLICY'S TRUE TERMS RESOLVE notice
        // fires". It does not: the gate is the capstone AND the intel, so ?nebula=4 and even ?nebula=5 fire
        // nothing and only ?nebula=all does. Pinned here so the sentence in the guide has an owner.
        var everyClause = Holding([.. IntelIds]);
        Assert.True(everyClause.HasEnoughIntelToEarnTheContract);
        Assert.False(everyClause.KnowsTheTruth);
        Assert.DoesNotContain("true terms resolve", NebulaLore.LedgerProgressLine(everyClause),
            StringComparison.OrdinalIgnoreCase);

        everyClause.Assemble(KeyId);
        Assert.True(everyClause.KnowsTheTruth);
        Assert.Equal(NebulaLore.TruthLedgerLine, NebulaLore.LedgerProgressLine(everyClause));
    }

    [Fact]
    public void TheLoudNoticeAndTheRestingLedgerLineTellOneStory()
    {
        // The pulse the world shouts on the edge and the line the ledger keeps afterwards are one fact, so
        // they live beside the predicate and say the same thing: filed under death, not insured against it.
        Assert.Contains("filed under it", NebulaLore.TruthNotice);
        Assert.Contains("filed under it", NebulaLore.TruthLedgerLine);
    }

    // ── 4 · Nothing the player reads talks about the build, or to the player. ──

    [Fact]
    public void NoAuthoredNebulaCopyTalksAboutTheBuildOrStageDirectsThePlayer()
    {
        // Two tells. A production note ("route pending" was the KAAMOS one) — and a parenthesised aside,
        // which is how an instruction to the PLAYER gets smuggled into a line spoken to the CAPTAIN. The
        // poster's first read used to end "(Read it closely — come back and look again.)". RED against it.
        string[] buildTells = ["pending", "TODO", "not implemented", "coming soon", "placeholder", "for now:"];

        var copy = new List<string>
        {
            NebulaLore.TruthNotice,
            NebulaLore.TruthLedgerLine,
            NebulaLore.PosterFirstReadLine,
            ArcConvergence.ConvergenceReveal,
            ArcConvergence.ConvergenceFoot,
        };
        copy.AddRange(NebulaLore.Fragments.Select(f => f.Lore));
        copy.AddRange(NebulaLore.Fragments.Select(f => f.Title));
        copy.AddRange([
            NebulaLore.BarSeamLabel("adjuster-tell"),
            NebulaLore.BarSeamLabel(KeyId),
            NebulaLore.BarSeamTitle("adjuster-tell"),
            NebulaLore.BarSeamTitle(KeyId),
        ]);

        // …and the ledger's state line in each of its three states.
        var fresh = Holding("rebirth-glitch");
        var ready = Holding([.. IntelIds.Take(NebulaLore.IntelNeededToUnlock)]);
        var known = Holding([.. IntelIds.Take(NebulaLore.IntelNeededToUnlock)]);
        known.Assemble(KeyId);
        copy.AddRange([
            NebulaLore.LedgerProgressLine(fresh),
            NebulaLore.LedgerProgressLine(ready),
            NebulaLore.LedgerProgressLine(known),
            NebulaLore.LedgerHeadline(known),
        ]);

        foreach (string line in copy)
        {
            foreach (string tell in buildTells)
            {
                Assert.False(line.Contains(tell, StringComparison.OrdinalIgnoreCase),
                    $"authored NEBULA copy talks about the build ('{tell}'): {line}");
            }

            Assert.False(line.Contains('('), $"authored NEBULA copy stage-directs the player: {line}");
        }
    }

    [Fact]
    public void TheFirstPosterReadStillTellsTheCaptainThereIsASecondOne()
    {
        // Honesty is not the thing being removed. `fine-print` is "the fine print, read TWICE" — a captain
        // who is never told there is a second read never gets the shard. The line must still send them back.
        Assert.Contains("Come back and read it", NebulaLore.PosterFirstReadLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("grey line", NebulaLore.PosterFirstReadLine, StringComparison.OrdinalIgnoreCase);
    }

    // ── 5 · The marquee card does not say the same thing twice. ──

    [Fact]
    public void TheConvergenceCardsFootIsNotACopyOfItsBody()
    {
        // The foot is the beat after the beat. It closed on two sentences lifted verbatim out of the
        // paragraph directly above it, so the biggest card in the game repeated itself to the player's face.
        Assert.False(string.IsNullOrWhiteSpace(ArcConvergence.ConvergenceFoot));

        foreach (string sentence in ArcConvergence.ConvergenceFoot.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            string s = sentence.Trim();
            if (s.Length < 12)
            {
                continue; // a fragment too short to be a repeat of anything
            }

            Assert.False(ArcConvergence.ConvergenceReveal.Contains(s, StringComparison.OrdinalIgnoreCase),
                $"the convergence card's foot repeats its own body: \"{s}\"");
        }
    }

    // ── 6 · One button, two different acts — and neither of them costs money. ──

    [Fact]
    public void TheTwoSeamStepsReadDifferently_AndNeitherWearsAPrice()
    {
        Assert.NotEqual(NebulaLore.BarSeamLabel("adjuster-tell"), NebulaLore.BarSeamLabel(KeyId));
        Assert.NotEqual(NebulaLore.BarSeamTitle("adjuster-tell"), NebulaLore.BarSeamTitle(KeyId));

        // Nobody is ASKED anything when your own gathered clauses answer each other on your own table.
        Assert.DoesNotContain("Ask", NebulaLore.BarSeamLabel(KeyId), StringComparison.Ordinal);
        Assert.Contains("Ask", NebulaLore.BarSeamLabel("adjuster-tell"), StringComparison.Ordinal);

        // Unlike the KAAMOS seam, no NEBULA step takes coin — so no NEBULA button may look like a purchase.
        foreach (string step in new[] { "adjuster-tell", KeyId })
        {
            Assert.DoesNotContain(" cr", NebulaLore.BarSeamLabel(step), StringComparison.Ordinal);
            Assert.DoesNotContain(" cr", NebulaLore.BarSeamTitle(step), StringComparison.Ordinal);
        }
    }

    // ── 7 · The dev seat (/map?nebula=adjuster). ──

    [Fact]
    public void TheDevSeatPutsTheAdjusterAtWhateverBarTheTesterDocksAt()
    {
        // One watch in five, at whichever bar you happen to walk into, is not a scene anyone can open on
        // purpose — and ?nebula=N only ever GRANTED the shard, which proves nothing about the scene.
        for (int day = 0; day < 20; day++)
        {
            Assert.True(NebulaFind.AdjusterAtBar("the-space-bar", day, forced: true));
            Assert.True(NebulaFind.AdjusterAtBar("ringside-exchange", day, forced: true));
            Assert.True(NebulaFind.AdjusterAtBar("selene-gate", day, forced: true));
        }

        // Unforced, the same bar-watches are mostly empty — otherwise the seat would be seating nobody.
        int seeded = Enumerable.Range(0, 20).Count(d => NebulaFind.AdjusterAtBar("the-space-bar", d));
        Assert.InRange(seeded, 0, 19);
    }

    [Fact]
    public void TheDevSeatChangesNothingWhenNobodyAsksForIt()
    {
        // A cheat that alters the unforced world is a cheat that invalidates every playtest run beside it.
        string[] bars = ["the-space-bar", "ringside-exchange", "the-tilt", "selene-gate"];
        foreach (string bar in bars)
        {
            for (int day = 0; day < 64; day++)
            {
                Assert.Equal(NebulaFind.AdjusterAtBar(bar, day), NebulaFind.AdjusterAtBar(bar, day, forced: false));
            }
        }

        // …and the adjuster is still genuinely rare, and an empty bar id still seats nobody.
        int hits = bars.Sum(bar => Enumerable.Range(0, 64).Count(d => NebulaFind.AdjusterAtBar(bar, d)));
        Assert.InRange(hits, 1, bars.Length * 64 / 2);
        Assert.False(NebulaFind.AdjusterAtBar("", 3));
    }
}
