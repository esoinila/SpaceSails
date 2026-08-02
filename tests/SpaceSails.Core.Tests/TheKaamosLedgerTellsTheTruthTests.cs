namespace SpaceSails.Core.Tests;

/// <summary>
/// #411 · THE KAAMOS STORY PASS — the sentences held to the sim.
///
/// <para>Walking PROJEKTI KAAMOS end to end on 2026-08-02 turned up the house's third and commonest bug
/// class three times over: <b>the sim did one thing and the sentence said another.</b></para>
///
/// <list type="bullet">
/// <item>The Captain's-ledger card counted down to the wrong number. It printed
/// <i>"the shape isn't clear yet — N more shards to see it"</i> with N measured against the whole intel pool
/// (five) instead of <see cref="KaamosLore.IntelNeededToUnlock"/> (four) — always exactly one shard
/// pessimistic. A captain holding three shards was told they needed two more; one more opened the
/// capstone.</item>
/// <item>The capstone credited pieces the captain had never found. The berth code's authored prose named
/// four shards by name — <i>"the held pod's cycler window, Vantar's dates, the holder's tick, the bought
/// coordinate"</i> — but the gate takes ANY four of five, and the bar seam offers the capstone BEFORE it
/// offers the coordinate. So the biggest sentence in the arc routinely told a captain that a coordinate
/// they had never bought was part of the answer.</item>
/// <item>The reach notice ended <i>"For now: route pending."</i> — a production note, out loud, in the
/// arc's loudest line.</item>
/// <item>The bar seam's button read <i>"🌑 Ask about KAAMOS"</i> for every step including the one that
/// takes 1,200 cr out of the purse the instant it is clicked, on a counter where every other button prints
/// its own price.</item>
/// </list>
///
/// <para>The fix was to build those sentences where the predicates live, so a test can ask the arc what it
/// SAYS and compare it with what it DOES. Each test below was proven RED against the shipped behaviour
/// before the fix went in.</para>
/// </summary>
public class TheKaamosLedgerTellsTheTruthTests
{
    private const string KeyId = "berth-code";

    private static IReadOnlyList<string> IntelIds => [.. KaamosLore.IntelFragments.Select(f => f.Id)];

    private static KaamosProgress Holding(params string[] ids)
    {
        var p = new KaamosProgress();
        foreach (string id in ids)
        {
            Assert.True(p.Assemble(id), $"{id} is not a real pool fragment");
        }
        return p;
    }

    /// <summary>Every subset of the intel pool of exactly <paramref name="size"/> ids, in canonical order.</summary>
    private static IEnumerable<string[]> Subsets(int size)
    {
        var ids = IntelIds;
        int n = ids.Count;
        for (int mask = 0; mask < (1 << n); mask++)
        {
            if (System.Numerics.BitOperations.PopCount((uint)mask) != size)
            {
                continue;
            }

            yield return [.. Enumerable.Range(0, n).Where(i => (mask & (1 << i)) != 0).Select(i => ids[i])];
        }
    }

    // ── 1 · The countdown counts down to the GATE. ──

    [Fact]
    public void TheLedgerCountdownIsExactlyHowManyMoreShardsTheGateWANTS()
    {
        // The honest reading of the sentence: assemble the number of shards the card asks for and the
        // capstone must become earnable; assemble one fewer and it must not. This is the assertion the old
        // pool-sized countdown fails — it asked for five-minus-held, which is one too many at every step.
        for (int held = 0; held < KaamosLore.IntelNeededToUnlock; held++)
        {
            var p = Holding([.. IntelIds.Take(held)]);
            int asked = KaamosLore.IntelStillNeeded(p);

            Assert.Contains($"{asked} more shard", KaamosLore.LedgerProgressLine(p));
            Assert.False(p.HasEnoughIntelToEarnTheKey, $"holding {held}, the gate should still be shut");

            // One short of what the card asked for: still shut.
            var short1 = Holding([.. IntelIds.Take(held)]);
            foreach (string id in IntelIds.Skip(held).Take(asked - 1))
            {
                short1.Assemble(id);
            }
            Assert.False(short1.HasEnoughIntelToEarnTheKey,
                $"holding {held}, the card asked for {asked}; {asked - 1} must NOT be enough");

            // Exactly what the card asked for: open.
            var exact = Holding([.. IntelIds.Take(held)]);
            foreach (string id in IntelIds.Skip(held).Take(asked))
            {
                exact.Assemble(id);
            }
            Assert.True(exact.HasEnoughIntelToEarnTheKey,
                $"holding {held}, the card asked for {asked} and {asked} must be enough");
        }
    }

    [Fact]
    public void TheCountdownIsSingularWhenOneShardIsLeft_AndGoneOnceTheShapeIsClear()
    {
        var one = Holding([.. IntelIds.Take(KaamosLore.IntelNeededToUnlock - 1)]);
        Assert.Equal(1, KaamosLore.IntelStillNeeded(one));
        Assert.Contains("1 more shard to see it", KaamosLore.LedgerProgressLine(one));
        Assert.DoesNotContain("1 more shards", KaamosLore.LedgerProgressLine(one));

        var enough = Holding([.. IntelIds.Take(KaamosLore.IntelNeededToUnlock)]);
        Assert.Equal(0, KaamosLore.IntelStillNeeded(enough));
        Assert.DoesNotContain("more shard", KaamosLore.LedgerProgressLine(enough));
        Assert.Contains("Enough intel to earn the berth-code", KaamosLore.LedgerProgressLine(enough));
    }

    [Fact]
    public void TheCountdownNeverGoesNegative_EvenHoldingTheWholePool()
    {
        var all = Holding([.. IntelIds]);
        Assert.Equal(0, KaamosLore.IntelStillNeeded(all));
    }

    // ── 2 · The capstone credits the shards actually in hand, and no others. ──

    [Fact]
    public void EveryIntelShardCarriesTheClauseTheCapstoneNamesItBy()
    {
        foreach (KaamosFragment f in KaamosLore.IntelFragments)
        {
            Assert.False(string.IsNullOrWhiteSpace(f.KeyClause),
                $"{f.Id} contributes nothing nameable to the berth code");
        }

        // The capstone is the thing derived, not one of the pieces derived from.
        Assert.Equal(string.Empty, KaamosLore.KeyFragment.KeyClause);
    }

    [Fact]
    public void TheCapstoneNamesEveryShardInHandAndNoShardOutOfIt()
    {
        // Every way a captain can legitimately arrive at the capstone: any IntelNeededToUnlock of the pool,
        // and the full sweep. The old fixed prose named four shards regardless and is RED on the first
        // subset that omits one of them.
        var arrivals = Subsets(KaamosLore.IntelNeededToUnlock).Concat(Subsets(KaamosLore.IntelFragments.Count()));

        foreach (string[] held in arrivals)
        {
            var p = Holding(held);
            p.Assemble(KeyId);
            Assert.True(p.CanReachEnceladus, "this subset should be a legitimate arrival at the capstone");

            string resolution = KaamosLore.KeyResolution(p);
            foreach (KaamosFragment f in KaamosLore.IntelFragments)
            {
                if (held.Contains(f.Id))
                {
                    Assert.Contains(f.KeyClause, resolution, StringComparison.Ordinal);
                }
                else
                {
                    Assert.DoesNotContain(f.KeyClause, resolution, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void TheLedgerRereadsTheCapstoneAsTheCaptainEarnedIt_AndOtherShardsAsAuthored()
    {
        var p = Holding("listed-berth", "cold-pod", "vantar-log", "holders-tell");
        p.Assemble(KeyId);

        // The capstone re-read in the ledger is the SAME sentence the bar seam gave — one source of truth.
        Assert.Equal(KaamosLore.KeyResolution(p), KaamosLore.LedgerLoreFor(KaamosLore.KeyFragment, p));
        Assert.DoesNotContain("the bought coordinate", KaamosLore.LedgerLoreFor(KaamosLore.KeyFragment, p));

        // An ordinary shard reads exactly as authored.
        KaamosFragment pod = KaamosLore.ById("cold-pod")!;
        Assert.Equal(pod.Lore, KaamosLore.LedgerLoreFor(pod, p));
    }

    [Fact]
    public void TheCapstonesOwnLoreNamesNoShard_SoNothingCanPasteAListOfPiecesNobodyFound()
    {
        // The authored capstone text is deliberately shard-free; the naming is the derivation's job.
        foreach (KaamosFragment f in KaamosLore.IntelFragments)
        {
            Assert.DoesNotContain(f.KeyClause, KaamosLore.KeyFragment.Lore, StringComparison.Ordinal);
        }
    }

    // ── 3 · The ledger headline counts intel, not the key. ──

    [Fact]
    public void TheHeadlineCountsIntelShards_AndTheKeyIsNeverOneOfThem()
    {
        var p = Holding([.. IntelIds.Take(KaamosLore.IntelNeededToUnlock)]);
        Assert.Contains($"{KaamosLore.IntelNeededToUnlock} of {KaamosLore.IntelFragments.Count()} shards", KaamosLore.LedgerHeadline(p));
        Assert.DoesNotContain("berth-code in hand", KaamosLore.LedgerHeadline(p));

        p.Assemble(KeyId);
        // Holding the key must not inflate the shard count — it is the capstone, not a shard.
        Assert.Contains($"{KaamosLore.IntelNeededToUnlock} of {KaamosLore.IntelFragments.Count()} shards", KaamosLore.LedgerHeadline(p));
        Assert.Contains("berth-code in hand", KaamosLore.LedgerHeadline(p));
    }

    // ── 4 · Nothing the player reads talks about the build. ──

    [Fact]
    public void NoAuthoredKaamosCopyTalksAboutWhatIsNotBuiltYet()
    {
        // "For now: route pending" is a production note wearing a parenthesis. The arc may say the window is
        // not open — that is fiction, and true — but it may never say the ROUTE is pending, because a berth
        // holder has never heard of a route lane. RED against the shipped reach notice.
        string[] tells = ["pending", "TODO", "not implemented", "coming soon", "placeholder", "for now:"];

        var copy = new List<string>
        {
            KaamosLore.ReachNotice,
            KaamosLore.ReachLedgerLine,
        };
        copy.AddRange(KaamosLore.Fragments.Select(f => f.Lore));
        copy.AddRange(KaamosLore.Fragments.Select(f => f.Title));
        copy.Add(KaamosLore.BarSeamLabel("holders-tell"));
        copy.Add(KaamosLore.BarSeamLabel("bought-coordinate"));
        copy.Add(KaamosLore.BarSeamLabel(KeyId));
        copy.Add(KaamosLore.BarSeamTitle("holders-tell"));
        copy.Add(KaamosLore.BarSeamTitle("bought-coordinate"));
        copy.Add(KaamosLore.BarSeamTitle(KeyId));

        // …and the ledger's state line in each of its three states.
        var fresh = Holding("listed-berth");
        var ready = Holding([.. IntelIds.Take(KaamosLore.IntelNeededToUnlock)]);
        var reached = Holding([.. IntelIds.Take(KaamosLore.IntelNeededToUnlock)]);
        reached.Assemble(KeyId);
        copy.AddRange([
            KaamosLore.LedgerProgressLine(fresh),
            KaamosLore.LedgerProgressLine(ready),
            KaamosLore.LedgerProgressLine(reached),
            KaamosLore.KeyResolution(reached),
        ]);

        foreach (string line in copy)
        {
            foreach (string tell in tells)
            {
                Assert.False(line.Contains(tell, StringComparison.OrdinalIgnoreCase),
                    $"authored KAAMOS copy talks about the build ('{tell}'): {line}");
            }
        }
    }

    [Fact]
    public void TheReachCopyStillSaysTheWindowIsNotOpen()
    {
        // Honesty is not the thing being removed: the captain must still be told there is nothing further to
        // do yet — in fiction, as a window that has not come round.
        Assert.Contains("not open yet", KaamosLore.ReachNotice);
        Assert.Contains("not yet open", KaamosLore.ReachLedgerLine);
        Assert.Equal(KaamosLore.ReachLedgerLine, KaamosLore.LedgerProgressLine(ReachedProgress()));
    }

    private static KaamosProgress ReachedProgress()
    {
        var p = Holding([.. IntelIds.Take(KaamosLore.IntelNeededToUnlock)]);
        p.Assemble(KeyId);
        return p;
    }

    // ── 5 · The button that spends money says so. ──

    [Fact]
    public void OnlyTheStepThatTakesCoinWearsAPrice()
    {
        string price = $"{KaamosFind.BoughtCoordinateCredits:N0} cr";

        Assert.Contains(price, KaamosLore.BarSeamLabel("bought-coordinate"));
        Assert.Contains(price, KaamosLore.BarSeamTitle("bought-coordinate"));

        // The tell and the capstone are free; a free step must never look like a purchase.
        foreach (string free in new[] { "holders-tell", KeyId })
        {
            Assert.DoesNotContain(" cr", KaamosLore.BarSeamLabel(free), StringComparison.Ordinal);
            Assert.DoesNotContain(" cr", KaamosLore.BarSeamTitle(free), StringComparison.Ordinal);
        }

        // And a paid step must never be disguised as a question — the three steps read differently.
        Assert.NotEqual(KaamosLore.BarSeamLabel("bought-coordinate"), KaamosLore.BarSeamLabel("holders-tell"));
        Assert.NotEqual(KaamosLore.BarSeamLabel(KeyId), KaamosLore.BarSeamLabel("holders-tell"));
    }

    // ── 6 · The two dev seats (/map?kaamos=pod, /map?kaamos=holder). ──

    [Fact]
    public void TheDevSeatsPutTheRareFindWhereTheTesterIsStanding()
    {
        // The pod is one seeded square in seventeen on one of seven outer moons; the holder drinks at a given
        // bar roughly one watch in four. Neither could be opened on purpose. Forced, they are here — even on
        // ground that carries no pods, because the point is to play the FIND, not to fly to a cold moon.
        Assert.False(KaamosFind.IsColdPodSquare("luna", 3, 4));
        Assert.True(KaamosFind.IsColdPodSquare("luna", 3, 4, forced: true));
        Assert.True(KaamosFind.IsColdPodSquare("europa", 11, 2, forced: true));

        // Whatever bar, whatever watch.
        for (int day = 0; day < 12; day++)
        {
            Assert.True(KaamosFind.HolderAtBar("the-space-bar", day, forced: true));
            Assert.True(KaamosFind.HolderAtBar("ringside-exchange", day, forced: true));
        }
    }

    [Fact]
    public void TheDevSeatsChangeNothingWhenNobodyAsksForThem()
    {
        // A cheat that alters the unforced world is a cheat that invalidates every playtest run beside it.
        for (int x = 0; x < 24; x++)
        {
            for (int y = 0; y < 24; y++)
            {
                Assert.Equal(KaamosFind.IsColdPodSquare("europa", x, y), KaamosFind.IsColdPodSquare("europa", x, y, forced: false));
                Assert.Equal(KaamosFind.IsColdPodSquare("luna", x, y), KaamosFind.IsColdPodSquare("luna", x, y, forced: false));
            }
        }

        for (int day = 0; day < 64; day++)
        {
            Assert.Equal(KaamosFind.HolderAtBar("the-tilt", day), KaamosFind.HolderAtBar("the-tilt", day, forced: false));
        }

        // …and the seeded pod is still genuinely rare and still refuses inner rocks entirely.
        int hits = 0;
        for (int x = 0; x < 40; x++)
        {
            for (int y = 0; y < 40; y++)
            {
                if (KaamosFind.IsColdPodSquare("titan", x, y))
                {
                    hits++;
                }

                Assert.False(KaamosFind.IsColdPodSquare("mars", x, y));
            }
        }
        Assert.InRange(hits, 1, 1600 / 4);
    }
}
