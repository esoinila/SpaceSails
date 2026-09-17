namespace SpaceSails.Core.Tests;

/// <summary>
/// THE CONVERGENCE (#422) — the cross-arc Expanse beat. Pins the joint predicate the wiring lane builds
/// against: it needs BOTH arcs far enough along (one alone never converges), it is deterministic, it reads
/// the KAAMOS arc strictly read-only, the reveal is one-time (the pending edge closes once seen), and the
/// convergence shock is the heaviest #391 throw in the game.
/// </summary>
public class ArcConvergenceTests
{
    // Assemble n KAAMOS intel shards into a fresh progress — the one the convergence card QUOTES first,
    // then canonical order. #422: the joint bar is still a count of three, but it names which one of the
    // three (ArcConvergence.KaamosSideShard), because the card's closing line claims the captain has been
    // carrying that sentence. A helper that took a blind Take(n) would build a captain the card lies to,
    // which is the world-that-cannot-tell-pass-from-fail trap; KaamosSideDoesNotConvergeWithoutTheTell
    // below is the guard that pins the difference.
    private static KaamosProgress KaamosWithIntel(int n)
    {
        var p = new KaamosProgress();
        foreach (string id in Named(KaamosLore.IntelFragments.Select(f => f.Id), ArcConvergence.KaamosSideShard).Take(n))
        {
            p.Assemble(id);
        }
        return p;
    }

    // Assemble n NEBULA intel shards into a fresh progress — same ordering rule, same reason.
    private static NebulaProgress NebulaWithIntel(int n)
    {
        var p = new NebulaProgress();
        foreach (string id in Named(NebulaLore.IntelFragments.Select(f => f.Id), ArcConvergence.NebulaSideShard).Take(n))
        {
            p.Assemble(id);
        }
        return p;
    }

    // Canonical order with the card's own shard pulled to the front.
    private static IEnumerable<string> Named(IEnumerable<string> ids, string first)
    {
        var all = ids.ToList();
        Assert.Contains(first, all); // the bar may only name a shard that exists and is intel
        return all.OrderByDescending(id => id == first);
    }

    [Fact]
    public void NeitherArc_Empty_DoesNotConverge()
    {
        Assert.False(ArcConvergence.HasConverged(new KaamosProgress(), new NebulaProgress()));
    }

    [Fact]
    public void KaamosAlone_HoweverDeep_DoesNotConverge()
    {
        // The whole ice-moon dig solved, but never a question asked about their own deaths: no convergence.
        var kaamos = KaamosWithIntel(KaamosLore.IntelFragments.Count());
        kaamos.Assemble(KaamosLore.KeyFragment.Id);
        Assert.True(kaamos.CanReachEnceladus);

        Assert.False(ArcConvergence.HasConverged(kaamos, new NebulaProgress()));
    }

    [Fact]
    public void NebulaAlone_HoweverDeep_DoesNotConverge()
    {
        // The whole resurrection truth known, but the ice moon never touched: still no convergence.
        var nebula = NebulaWithIntel(NebulaLore.IntelFragments.Count());
        nebula.Assemble(NebulaLore.KeyFragment.Id);
        Assert.True(nebula.KnowsTheTruth);

        Assert.False(ArcConvergence.HasConverged(new KaamosProgress(), nebula));
    }

    [Fact]
    public void BothSidesAtThreshold_Converges()
    {
        var kaamos = KaamosWithIntel(ArcConvergence.KaamosSideThreshold);
        var nebula = NebulaWithIntel(ArcConvergence.NebulaSideThreshold);

        Assert.True(ArcConvergence.KaamosSideReady(kaamos));
        Assert.True(ArcConvergence.NebulaSideReady(nebula));
        Assert.True(ArcConvergence.HasConverged(kaamos, nebula));
    }

    [Fact]
    public void OneShortOnEitherSide_DoesNotConverge()
    {
        // KAAMOS one short, Nebula ready.
        Assert.False(ArcConvergence.HasConverged(
            KaamosWithIntel(ArcConvergence.KaamosSideThreshold - 1),
            NebulaWithIntel(ArcConvergence.NebulaSideThreshold)));

        // Nebula one short, KAAMOS ready.
        Assert.False(ArcConvergence.HasConverged(
            KaamosWithIntel(ArcConvergence.KaamosSideThreshold),
            NebulaWithIntel(ArcConvergence.NebulaSideThreshold - 1)));
    }

    [Fact]
    public void HasConverged_IsDeterministic()
    {
        var kaamos = KaamosWithIntel(ArcConvergence.KaamosSideThreshold);
        var nebula = NebulaWithIntel(ArcConvergence.NebulaSideThreshold);

        bool a = ArcConvergence.HasConverged(kaamos, nebula);
        bool b = ArcConvergence.HasConverged(kaamos, nebula);
        Assert.Equal(a, b);
        Assert.True(a); // no clock, no RNG — the same two progresses, the same answer
    }

    [Fact]
    public void Convergence_ReadsKaamos_ReadOnly()
    {
        var kaamos = KaamosWithIntel(ArcConvergence.KaamosSideThreshold);
        var nebula = NebulaWithIntel(ArcConvergence.NebulaSideThreshold);
        var before = kaamos.AssembledIds.ToList();

        ArcConvergence.HasConverged(kaamos, nebula);
        ArcConvergence.ConvergenceRevealPending(kaamos, nebula);

        Assert.Equal(before, kaamos.AssembledIds); // the KAAMOS arc is never mutated by a convergence read
    }

    [Fact]
    public void RevealPending_IsTheOneTimeEdge_ClosedByMarkSeen()
    {
        var kaamos = KaamosWithIntel(ArcConvergence.KaamosSideThreshold);
        var nebula = NebulaWithIntel(ArcConvergence.NebulaSideThreshold);

        // Ready and not yet fired: the wiring lane's edge is open.
        Assert.True(ArcConvergence.ConvergenceRevealPending(kaamos, nebula));

        // Fire it once.
        Assert.True(nebula.MarkConvergenceSeen());

        // Still converged, but the reveal no longer pends — it plays once in a universe.
        Assert.True(ArcConvergence.HasConverged(kaamos, nebula));
        Assert.False(ArcConvergence.ConvergenceRevealPending(kaamos, nebula));
    }

    [Fact]
    public void RevealPending_IsFalse_BeforeConvergence_EvenIfUnseen()
    {
        var kaamos = KaamosWithIntel(ArcConvergence.KaamosSideThreshold - 1);
        var nebula = NebulaWithIntel(ArcConvergence.NebulaSideThreshold);
        Assert.False(nebula.ConvergenceSeen);
        Assert.False(ArcConvergence.ConvergenceRevealPending(kaamos, nebula)); // not converged yet, so nothing pends
    }

    [Fact]
    public void ConvergenceReveal_IsOneClosingLine_ThatExplainsNothing()
    {
        // #422 option B (owner 2026-09-17): the card's own voice is ONE sentence, and it is the last thing
        // on the card, not the first. It may not name either arc — naming them is the join, and the join is
        // the player's to make. The two things that ARE on the card are quoted from the world.
        Assert.False(string.IsNullOrWhiteSpace(ArcConvergence.ConvergenceReveal));
        Assert.DoesNotContain("{", ArcConvergence.ConvergenceReveal);
        Assert.DoesNotContain("}", ArcConvergence.ConvergenceReveal);

        Assert.Single(ArcConvergence.ConvergenceReveal.Split('.', StringSplitOptions.RemoveEmptyEntries));
        Assert.DoesNotContain("Vantar", ArcConvergence.ConvergenceReveal, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nebula", ArcConvergence.ConvergenceReveal, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("KAAMOS", ArcConvergence.ConvergenceReveal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KaamosSideDoesNotConverge_WithoutTheTellTheCardQuotes()
    {
        // THE AUDIT (#422, 2026-09-17). The card closes on "You have been carrying both of these for a
        // while" over two quoted sentences. The KAAMOS shards come from five different systems — the
        // Ringside plaque, a derelict pod, a sealed lab log, a bar, a bought tip — so a captain can reach
        // three intel having never sat down with the berth-holder, and a bar that counted only to three
        // would open a card quoting a line they had never heard, while telling them they had been carrying
        // it. The bar names the shard instead. Still three: the tell is one OF the three, not a fourth.
        var withoutTheTell = new KaamosProgress();
        foreach (string id in KaamosLore.IntelFragments
                     .Select(f => f.Id)
                     .Where(id => id != ArcConvergence.KaamosSideShard)
                     .Take(ArcConvergence.KaamosSideThreshold))
        {
            withoutTheTell.Assemble(id);
        }

        Assert.Equal(ArcConvergence.KaamosSideThreshold, withoutTheTell.IntelAssembled); // the count is met
        Assert.False(withoutTheTell.Has(ArcConvergence.KaamosSideShard));               // the voice is not
        Assert.False(ArcConvergence.KaamosSideReady(withoutTheTell));
        Assert.False(ArcConvergence.HasConverged(withoutTheTell, NebulaWithIntel(ArcConvergence.NebulaSideThreshold)));

        // …and the moment the holder finally talks, at the same count, it converges.
        withoutTheTell.Assemble(ArcConvergence.KaamosSideShard);
        Assert.True(ArcConvergence.KaamosSideReady(withoutTheTell));
    }

    [Fact]
    public void NebulaSideDoesNotConverge_WithoutTheTellTheCardQuotes()
    {
        // Symmetric, and on this side the adjuster is the rarer of the two: they drink at a given bar
        // roughly one watch in five, which is exactly why the card may not assume the captain met them.
        var withoutTheTell = new NebulaProgress();
        foreach (string id in NebulaLore.IntelFragments
                     .Select(f => f.Id)
                     .Where(id => id != ArcConvergence.NebulaSideShard)
                     .Take(ArcConvergence.NebulaSideThreshold))
        {
            withoutTheTell.Assemble(id);
        }

        Assert.Equal(ArcConvergence.NebulaSideThreshold, withoutTheTell.IntelAssembled);
        Assert.False(ArcConvergence.NebulaSideReady(withoutTheTell));
        Assert.False(ArcConvergence.HasConverged(KaamosWithIntel(ArcConvergence.KaamosSideThreshold), withoutTheTell));

        withoutTheTell.Assemble(ArcConvergence.NebulaSideShard);
        Assert.True(ArcConvergence.NebulaSideReady(withoutTheTell));
    }

    [Fact]
    public void TheBarIsStillThreeAndThree()
    {
        // Owner ruling 2026-09-17 kept option B's bar and rejected option A's (converge only at both
        // capstones). Naming a shard must never become raising the number: three intel, each side, and
        // both below the arcs' own IntelNeededToUnlock so the convergence stays a MID-dig sensation.
        Assert.Equal(3, ArcConvergence.KaamosSideThreshold);
        Assert.Equal(3, ArcConvergence.NebulaSideThreshold);
        Assert.True(ArcConvergence.KaamosSideThreshold < KaamosLore.IntelNeededToUnlock);
        Assert.True(ArcConvergence.NebulaSideThreshold < NebulaLore.IntelNeededToUnlock);

        // Exactly the threshold count converges — the named shard is one of the three, never an extra.
        var kaamos = KaamosWithIntel(ArcConvergence.KaamosSideThreshold);
        var nebula = NebulaWithIntel(ArcConvergence.NebulaSideThreshold);
        Assert.Equal(ArcConvergence.KaamosSideThreshold, kaamos.IntelAssembled);
        Assert.Equal(ArcConvergence.NebulaSideThreshold, nebula.IntelAssembled);
        Assert.True(ArcConvergence.HasConverged(kaamos, nebula));
    }

    [Fact]
    public void ConvergenceShock_IsTheHeaviestThrowInTheGame()
    {
        // Bigger than either arc's own reveal — the two landing as one is the heaviest #391 throw.
        Assert.True(ArcConvergence.ConvergenceSanityShockHook >= KaamosLore.RevealSanityShockHook);
        Assert.True(ArcConvergence.ConvergenceSanityShockHook > NebulaLore.TruthSanityShockHook);
        Assert.True(ArcConvergence.ConvergenceSanityShockHook > NerveModel.MonolithSightShock);
    }
}
