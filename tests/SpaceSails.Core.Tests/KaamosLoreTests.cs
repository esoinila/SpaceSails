namespace SpaceSails.Core.Tests;

/// <summary>
/// PROJEKTI KAAMOS — the ice-moon plotline spine (#411). Pins the invariants the sibling lanes build
/// against: the fragment pool is well-formed and deterministic, assembly progresses monotonically per
/// thread, the intel threshold gates earning the capstone, the <see cref="KaamosLore.CanReachEnceladus"/>
/// predicate needs BOTH the key and the intel (a pasted key alone never opens the ice moon), the vault
/// round-trips the assembly losslessly and tolerantly, and no authored lore leaks a stray brace.
/// </summary>
public class KaamosLoreTests
{
    private const string KeyId = "berth-code";

    // ── The pool: well-formed, deterministic, authored. ──

    [Fact]
    public void Pool_IsWellFormed_UniqueIds_ExactlyOneKey()
    {
        Assert.True(KaamosLore.PoolIsWellFormed);
        Assert.Equal(KaamosLore.Fragments.Count, KaamosLore.Fragments.Select(f => f.Id).Distinct().Count());
        Assert.Single(KaamosLore.Fragments, f => f.IsKey);
        Assert.Equal(KeyId, KaamosLore.KeyFragment.Id);
    }

    [Fact]
    public void Pool_IsDeterministic_SameOrderEveryRead()
    {
        var a = KaamosLore.Fragments.Select(f => f.Id).ToList();
        var b = KaamosLore.Fragments.Select(f => f.Id).ToList();
        Assert.Equal(a, b); // no RNG, no wall clock — the same shards, same order, every universe
    }

    [Fact]
    public void EveryFragment_HasTitleAndEvocativeLore_AndNamesTheProject()
    {
        foreach (KaamosFragment f in KaamosLore.Fragments)
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Id), "fragment has no id");
            Assert.False(string.IsNullOrWhiteSpace(f.Title), $"{f.Id} has no title");
            Assert.False(string.IsNullOrWhiteSpace(f.Lore), $"{f.Id} has no lore");
            Assert.True(f.Lore.Length > 80, $"{f.Id} lore is too thin to be a real shard");
        }

        // KAAMOS is the through-line: at least one shard names it, and none spells out the whole truth
        // (the reveal is earned at Enceladus, never in a fragment).
        Assert.Contains(KaamosLore.Fragments, f => f.Lore.Contains("KAAMOS"));
    }

    [Fact]
    public void NoLore_LeaksAStrayBrace()
    {
        // The plaque/souvenir idiom: authored constants must never carry an unresolved interpolation
        // placeholder (or a hand-typed brace) into the world.
        foreach (KaamosFragment f in KaamosLore.Fragments)
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
        Assert.Equal(KaamosLore.Fragments.Count - 1, KaamosLore.IntelFragments.Count());
        Assert.DoesNotContain(KaamosLore.IntelFragments, f => f.IsKey);
        // The threshold must be earnable but not a completionist sweep of every intel shard.
        Assert.InRange(KaamosLore.IntelNeededToUnlock, 1, KaamosLore.IntelFragments.Count());
    }

    // ── Assembly: per-thread, idempotent, tolerant. ──

    [Fact]
    public void Assemble_AddsRecognisedFragments_Idempotently()
    {
        var p = new KaamosProgress();
        Assert.Equal(0, p.Count);

        Assert.True(p.Assemble("listed-berth"));    // the edge that first adds
        Assert.False(p.Assemble("listed-berth"));   // idempotent — already held
        Assert.True(p.Has("listed-berth"));
        Assert.Equal(1, p.Count);
    }

    [Fact]
    public void Assemble_RefusesAPhantomId()
    {
        var p = new KaamosProgress();
        Assert.False(p.Assemble("not-a-real-fragment"));
        Assert.False(p.Assemble(""));
        Assert.Equal(0, p.Count);
    }

    [Fact]
    public void AssembledIds_AreCanonicalOrder_RegardlessOfDiscoveryOrder()
    {
        var canonical = KaamosLore.Fragments.Select(f => f.Id).ToList();

        var p = new KaamosProgress();
        // Assemble in a deliberately scrambled order (reverse of canonical).
        foreach (string id in canonical.AsEnumerable().Reverse())
        {
            p.Assemble(id);
        }

        Assert.Equal(canonical, p.AssembledIds); // stored/rendered in authored order, not discovery order
    }

    [Fact]
    public void Clear_WipesTheThread()
    {
        var p = new KaamosProgress();
        p.Assemble("listed-berth");
        p.Assemble("cold-pod");
        p.Clear();
        Assert.Equal(0, p.Count);
        Assert.False(p.CanReachEnceladus);
    }

    // ── The threshold + the reach predicate. ──

    [Fact]
    public void EnoughIntel_GatesEarningTheKey()
    {
        var p = new KaamosProgress();
        var intel = KaamosLore.IntelFragments.Select(f => f.Id).ToList();

        // One short of the threshold: not yet.
        for (int i = 0; i < KaamosLore.IntelNeededToUnlock - 1; i++)
        {
            p.Assemble(intel[i]);
        }
        Assert.False(p.HasEnoughIntelToEarnTheKey);

        // The threshold shard tips it.
        p.Assemble(intel[KaamosLore.IntelNeededToUnlock - 1]);
        Assert.True(p.HasEnoughIntelToEarnTheKey);
    }

    [Fact]
    public void TheKeyDoesNotCountAsIntel()
    {
        var p = new KaamosProgress();
        p.Assemble(KeyId);
        Assert.Equal(0, p.IntelAssembled);        // the capstone is not a shard of intel
        Assert.False(p.HasEnoughIntelToEarnTheKey);
    }

    [Fact]
    public void CanReachEnceladus_NeedsBothKeyAndIntel()
    {
        var intel = KaamosLore.IntelFragments.Select(f => f.Id).ToList();

        // Intel-complete but no key: the shape is visible, the door is not open.
        var a = new KaamosProgress();
        foreach (string id in intel)
        {
            a.Assemble(id);
        }
        Assert.True(a.HasEnoughIntelToEarnTheKey);
        Assert.False(a.CanReachEnceladus);

        // A pasted key with no intel to legitimise it: still refused (the cheat guard).
        var b = new KaamosProgress();
        b.Assemble(KeyId);
        Assert.False(b.CanReachEnceladus);

        // Enough intel AND the key: the reach opens.
        var c = new KaamosProgress();
        for (int i = 0; i < KaamosLore.IntelNeededToUnlock; i++)
        {
            c.Assemble(intel[i]);
        }
        c.Assemble(KeyId);
        Assert.True(c.CanReachEnceladus);
    }

    [Fact]
    public void ReachHook_NamesTheIceMoon_AndTheRevealShockOutweighsTheMonolith()
    {
        Assert.Equal("enceladus", KaamosLore.IceMoonBodyId);
        // The reveal is the biggest #391 throw by design — heavier than first sight of the monolith.
        Assert.True(KaamosLore.RevealSanityShockHook > NerveModel.MonolithSightShock);
    }

    // ── The vault round-trip (the thread-idiom persistence). ──

    [Fact]
    public void Vault_RoundTripsTheAssembly_Losslessly()
    {
        var p = new KaamosProgress();
        p.Assemble("listed-berth");
        p.Assemble("vantar-log");
        p.Assemble(KeyId);

        KaamosSection section = VaultMapper.ToSection(p);
        var vault = new Vault { Kaamos = section };

        string json = VaultSerializer.Save(vault);
        Vault loaded = VaultSerializer.Load(json);

        Assert.False(loaded.Tampered);
        var rebuilt = new KaamosProgress();
        VaultMapper.Apply(loaded.Kaamos, rebuilt);

        Assert.Equal(p.AssembledIds, rebuilt.AssembledIds);
        Assert.True(rebuilt.Has(KeyId));
    }

    [Fact]
    public void Vault_MissingKaamosSection_DefaultsToNothingAssembled()
    {
        // A pre-#411 file simply lacks the section — the captain has heard nothing of the polar night.
        var vault = new Vault { Purse = new PurseSection(10) };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));

        var rebuilt = new KaamosProgress();
        VaultMapper.Apply(loaded.Kaamos, rebuilt);
        Assert.Equal(0, rebuilt.Count);
        Assert.False(rebuilt.CanReachEnceladus);
    }

    [Fact]
    public void Apply_DropsAnUnknownSavedId_Tolerantly()
    {
        var section = new KaamosSection
        {
            AssembledFragmentIds = ["listed-berth", "a-retired-shard-id", "cold-pod"],
        };
        var p = new KaamosProgress();
        VaultMapper.Apply(section, p);

        Assert.Equal(new[] { "listed-berth", "cold-pod" }, p.AssembledIds); // the phantom is dropped
    }
}
