namespace SpaceSails.Core.Tests;

/// <summary>
/// NEBULA MUTUAL — the second story arc spine (#422). Pins the invariants the sibling/wiring lanes build
/// against: the fragment pool is well-formed and deterministic, assembly progresses monotonically per
/// thread, the intel threshold gates earning the contract, the <see cref="NebulaLore.KnowsTheTruth"/>
/// predicate needs BOTH the key and the intel (a pasted contract alone never yields the truth), the vault
/// round-trips the assembly (and the convergence bit) losslessly and tolerantly, and no authored lore leaks
/// a stray brace.
/// </summary>
public class NebulaLoreTests
{
    private const string KeyId = "policy-terms";

    // ── The pool: well-formed, deterministic, authored. ──

    [Fact]
    public void Pool_IsWellFormed_UniqueIds_ExactlyOneKey()
    {
        Assert.True(NebulaLore.PoolIsWellFormed);
        Assert.Equal(NebulaLore.Fragments.Count, NebulaLore.Fragments.Select(f => f.Id).Distinct().Count());
        Assert.Single(NebulaLore.Fragments, f => f.IsKey);
        Assert.Equal(KeyId, NebulaLore.KeyFragment.Id);
    }

    [Fact]
    public void Pool_IsDeterministic_SameOrderEveryRead()
    {
        var a = NebulaLore.Fragments.Select(f => f.Id).ToList();
        var b = NebulaLore.Fragments.Select(f => f.Id).ToList();
        Assert.Equal(a, b); // no RNG, no wall clock — the same shards, same order, every universe
    }

    [Fact]
    public void EveryFragment_HasTitleAndEvocativeLore_AndNamesTheUnderwriter()
    {
        foreach (NebulaFragment f in NebulaLore.Fragments)
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Id), "fragment has no id");
            Assert.False(string.IsNullOrWhiteSpace(f.Title), $"{f.Id} has no title");
            Assert.False(string.IsNullOrWhiteSpace(f.Lore), $"{f.Id} has no lore");
            Assert.True(f.Lore.Length > 80, $"{f.Id} lore is too thin to be a real shard");
        }

        // Nebula Mutual is the through-line: at least one shard names the underwriter, and none spells out
        // the whole truth (the reveal is earned; the deepest one is the convergence, never in a fragment).
        Assert.Contains(NebulaLore.Fragments, f => f.Lore.Contains("NEBULA") || f.Lore.Contains("Nebula"));
    }

    [Fact]
    public void NoLore_LeaksAStrayBrace()
    {
        // The plaque/souvenir idiom: authored constants must never carry an unresolved interpolation
        // placeholder (or a hand-typed brace) into the world.
        foreach (NebulaFragment f in NebulaLore.Fragments)
        {
            Assert.DoesNotContain("{", f.Lore);
            Assert.DoesNotContain("}", f.Lore);
            Assert.DoesNotContain("{", f.Title);
            Assert.DoesNotContain("}", f.Title);
        }
    }

    [Fact]
    public void IntelFragments_AreEveryNonKeyFragment_AndThresholdFitsWithinThem()
    {
        Assert.Equal(NebulaLore.Fragments.Count - 1, NebulaLore.IntelFragments.Count());
        Assert.DoesNotContain(NebulaLore.IntelFragments, f => f.IsKey);
        // The threshold must be earnable but not a completionist sweep of every intel shard.
        Assert.InRange(NebulaLore.IntelNeededToUnlock, 1, NebulaLore.IntelFragments.Count());
    }

    // ── Assembly: per-thread, idempotent, tolerant. ──

    [Fact]
    public void Assemble_AddsRecognisedFragments_Idempotently()
    {
        var p = new NebulaProgress();
        Assert.Equal(0, p.Count);

        Assert.True(p.Assemble("fine-print"));    // the edge that first adds
        Assert.False(p.Assemble("fine-print"));   // idempotent — already held
        Assert.True(p.Has("fine-print"));
        Assert.Equal(1, p.Count);
    }

    [Fact]
    public void Assemble_RefusesAPhantomId()
    {
        var p = new NebulaProgress();
        Assert.False(p.Assemble("not-a-real-fragment"));
        Assert.False(p.Assemble(""));
        Assert.Equal(0, p.Count);
    }

    [Fact]
    public void AssembledIds_AreCanonicalOrder_RegardlessOfDiscoveryOrder()
    {
        var canonical = NebulaLore.Fragments.Select(f => f.Id).ToList();

        var p = new NebulaProgress();
        // Assemble in a deliberately scrambled order (reverse of canonical).
        foreach (string id in canonical.AsEnumerable().Reverse())
        {
            p.Assemble(id);
        }

        Assert.Equal(canonical, p.AssembledIds); // stored/rendered in authored order, not discovery order
    }

    [Fact]
    public void Clear_WipesTheThread_IncludingTheConvergenceBit()
    {
        var p = new NebulaProgress();
        p.Assemble("fine-print");
        p.Assemble("adjuster-tell");
        p.MarkConvergenceSeen();
        p.Clear();
        Assert.Equal(0, p.Count);
        Assert.False(p.KnowsTheTruth);
        Assert.False(p.ConvergenceSeen);
    }

    // ── The threshold + the truth predicate. ──

    [Fact]
    public void EnoughIntel_GatesEarningTheContract()
    {
        var p = new NebulaProgress();
        var intel = NebulaLore.IntelFragments.Select(f => f.Id).ToList();

        // One short of the threshold: not yet.
        for (int i = 0; i < NebulaLore.IntelNeededToUnlock - 1; i++)
        {
            p.Assemble(intel[i]);
        }
        Assert.False(p.HasEnoughIntelToEarnTheContract);
        Assert.False(NebulaLore.KnowsTheFinePrint(p));

        // The threshold shard tips it.
        p.Assemble(intel[NebulaLore.IntelNeededToUnlock - 1]);
        Assert.True(p.HasEnoughIntelToEarnTheContract);
        Assert.True(NebulaLore.KnowsTheFinePrint(p));
    }

    [Fact]
    public void TheKeyDoesNotCountAsIntel()
    {
        var p = new NebulaProgress();
        p.Assemble(KeyId);
        Assert.Equal(0, p.IntelAssembled);        // the capstone is not a shard of intel
        Assert.False(p.HasEnoughIntelToEarnTheContract);
    }

    [Fact]
    public void KnowsTheTruth_NeedsBothKeyAndIntel()
    {
        var intel = NebulaLore.IntelFragments.Select(f => f.Id).ToList();

        // Intel-complete but no contract: the shape is visible, the terms are not in hand.
        var a = new NebulaProgress();
        foreach (string id in intel)
        {
            a.Assemble(id);
        }
        Assert.True(a.HasEnoughIntelToEarnTheContract);
        Assert.False(a.KnowsTheTruth);

        // A pasted contract with no intel to legitimise it: still refused (the cheat guard).
        var b = new NebulaProgress();
        b.Assemble(KeyId);
        Assert.False(b.KnowsTheTruth);

        // Enough intel AND the contract: the truth is known.
        var c = new NebulaProgress();
        for (int i = 0; i < NebulaLore.IntelNeededToUnlock; i++)
        {
            c.Assemble(intel[i]);
        }
        c.Assemble(KeyId);
        Assert.True(c.KnowsTheTruth);
    }

    [Fact]
    public void TruthHook_NamesTheUnderwriter_AndTheRevealShockOutweighsTheMonolith()
    {
        Assert.Equal("Nebula Mutual", NebulaLore.Underwriter);
        // Arc 2's own reveal is a heavy #391 throw — heavier than first sight of the monolith.
        Assert.True(NebulaLore.TruthSanityShockHook > NerveModel.MonolithSightShock);
    }

    // ── The convergence bit is one-time and per-thread. ──

    [Fact]
    public void MarkConvergenceSeen_IsOneTimeEdge()
    {
        var p = new NebulaProgress();
        Assert.False(p.ConvergenceSeen);
        Assert.True(p.MarkConvergenceSeen());   // the edge that first sets it
        Assert.True(p.ConvergenceSeen);
        Assert.False(p.MarkConvergenceSeen());  // idempotent — already seen
    }

    // ── The vault round-trip (the thread-idiom persistence). ──

    [Fact]
    public void Vault_RoundTripsTheAssemblyAndConvergenceBit_Losslessly()
    {
        var p = new NebulaProgress();
        p.Assemble("rebirth-glitch");
        p.Assemble("clinic-ledger");
        p.Assemble(KeyId);
        p.MarkConvergenceSeen();

        NebulaSection section = VaultMapper.ToSection(p);
        var vault = new Vault { Nebula = section };

        string json = VaultSerializer.Save(vault);
        Vault loaded = VaultSerializer.Load(json);

        Assert.False(loaded.Tampered);
        var rebuilt = new NebulaProgress();
        VaultMapper.Apply(loaded.Nebula, rebuilt);

        Assert.Equal(p.AssembledIds, rebuilt.AssembledIds);
        Assert.True(rebuilt.Has(KeyId));
        Assert.True(rebuilt.ConvergenceSeen);
    }

    [Fact]
    public void Vault_MissingNebulaSection_DefaultsToNothingAssembledAndUnseen()
    {
        // A pre-#422 file simply lacks the section — the captain has never wondered why they keep waking up.
        var vault = new Vault { Purse = new PurseSection(10) };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));

        var rebuilt = new NebulaProgress();
        VaultMapper.Apply(loaded.Nebula, rebuilt);
        Assert.Equal(0, rebuilt.Count);
        Assert.False(rebuilt.KnowsTheTruth);
        Assert.False(rebuilt.ConvergenceSeen);
    }

    [Fact]
    public void Apply_DropsAnUnknownSavedId_Tolerantly()
    {
        var section = new NebulaSection
        {
            AssembledFragmentIds = ["fine-print", "a-retired-shard-id", "rebirth-glitch"],
        };
        var p = new NebulaProgress();
        VaultMapper.Apply(section, p);

        Assert.Equal(new[] { "rebirth-glitch", "fine-print" }, p.AssembledIds); // phantom dropped, canonical order
    }
}
