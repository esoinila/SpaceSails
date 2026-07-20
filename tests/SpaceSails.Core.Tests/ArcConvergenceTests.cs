namespace SpaceSails.Core.Tests;

/// <summary>
/// THE CONVERGENCE (#422) — the cross-arc Expanse beat. Pins the joint predicate the wiring lane builds
/// against: it needs BOTH arcs far enough along (one alone never converges), it is deterministic, it reads
/// the KAAMOS arc strictly read-only, the reveal is one-time (the pending edge closes once seen), and the
/// convergence shock is the heaviest #391 throw in the game.
/// </summary>
public class ArcConvergenceTests
{
    // Assemble the first n KAAMOS intel shards into a fresh progress.
    private static KaamosProgress KaamosWithIntel(int n)
    {
        var p = new KaamosProgress();
        foreach (string id in KaamosLore.IntelFragments.Select(f => f.Id).Take(n))
        {
            p.Assemble(id);
        }
        return p;
    }

    // Assemble the first n NEBULA intel shards into a fresh progress.
    private static NebulaProgress NebulaWithIntel(int n)
    {
        var p = new NebulaProgress();
        foreach (string id in NebulaLore.IntelFragments.Select(f => f.Id).Take(n))
        {
            p.Assemble(id);
        }
        return p;
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
    public void ConvergenceReveal_IsAuthored_AndNamesBothArcs()
    {
        Assert.False(string.IsNullOrWhiteSpace(ArcConvergence.ConvergenceReveal));
        Assert.True(ArcConvergence.ConvergenceReveal.Length > 120);
        Assert.DoesNotContain("{", ArcConvergence.ConvergenceReveal);
        Assert.DoesNotContain("}", ArcConvergence.ConvergenceReveal);
        // The reveal binds the two arcs: Vantar (KAAMOS) and Nebula (arc 2) both named.
        Assert.Contains("Vantar", ArcConvergence.ConvergenceReveal);
        Assert.Contains("Nebula", ArcConvergence.ConvergenceReveal);
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
