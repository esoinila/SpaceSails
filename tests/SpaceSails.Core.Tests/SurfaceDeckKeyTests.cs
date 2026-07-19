namespace SpaceSails.Core.Tests;

using System.Collections.Generic;

/// <summary>
/// #371 Phase 1 · pins the surface-deck memoization key. The client's <c>MoonSurface.SurfaceDeck</c>
/// caches the built layout by this key, so the key's equality IS the invalidation contract: identical
/// inputs must reuse the cached deck, and any change to the own-cache set (a bury, a lift, a drop — even
/// a moved ✗) must be a different key so the ground is honestly rebuilt. A stale ✗ after a bury would be
/// exactly the nasty cache bug this guards against.
/// </summary>
public class SurfaceDeckKeyTests
{
    private static List<(string Id, double X, double Y, int ReeverLevel)> Caches(
        params (string Id, double X, double Y, int ReeverLevel)[] cs) => new(cs);

    // ── Cache HIT: same inputs → equal keys (and equal hash) → a revisit reuses the built deck ──

    [Fact]
    public void SameInputs_ProduceEqualKeys()
    {
        var a = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)));
        var b = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void EmptyCaches_AreEqual_RegardlessOfNullOrEmptyList()
    {
        var fromNull = SurfaceDeckKey.For("luna", "Luna", null);
        var fromEmpty = SurfaceDeckKey.For("luna", "Luna", Caches());

        Assert.Equal(fromNull, fromEmpty);
        Assert.Equal(fromNull.GetHashCode(), fromEmpty.GetHashCode());
    }

    [Fact]
    public void ReusableListMutation_DoesNotMutateAnAlreadyBuiltKey()
    {
        // The client hands a mutable list; the key must copy it defensively so a later mutation (the next
        // rebuild reusing the buffer) can't retroactively change an existing cache entry's key.
        var live = Caches(("c1", 3, -50, 2));
        var key = SurfaceDeckKey.For("miranda", "Miranda", live);
        var snapshot = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)));

        live.Add(("c2", 9, -60, 0)); // caller mutates the very list it passed in
        Assert.Equal(snapshot, key);
    }

    // ── Cache MISS: any input change → different key → an honest rebuild ──

    [Fact]
    public void DifferentBody_IsADifferentKey()
    {
        var luna = SurfaceDeckKey.For("luna", "Luna", null);
        var miranda = SurfaceDeckKey.For("miranda", "Miranda", null);
        Assert.NotEqual(luna, miranda);
    }

    [Fact]
    public void DifferentDisplayName_IsADifferentKey()
    {
        var a = SurfaceDeckKey.For("x", "Alpha", null);
        var b = SurfaceDeckKey.For("x", "Beta", null);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Bury_AddingACache_IsADifferentKey()
    {
        var before = SurfaceDeckKey.For("miranda", "Miranda", Caches());
        var after = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)));
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Lift_RemovingACache_IsADifferentKey()
    {
        var before = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)));
        var after = SurfaceDeckKey.For("miranda", "Miranda", Caches());
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void MovedX_IsADifferentKey()
    {
        // A ✗ that plants at a different spot (the free-form bury records the real dug coords) must rebuild
        // so the dig console lands where the shovel did.
        var here = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)));
        var there = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3.5, -50, 2)));
        Assert.NotEqual(here, there);
    }

    [Fact]
    public void DifferentReeverLevel_IsADifferentKey()
    {
        var quiet = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 0)));
        var haunted = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 5)));
        Assert.NotEqual(quiet, haunted);
    }

    [Fact]
    public void DifferentCacheId_IsADifferentKey()
    {
        var a = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)));
        var b = SurfaceDeckKey.For("miranda", "Miranda", Caches(("c2", 3, -50, 2)));
        Assert.NotEqual(a, b);
    }

    // ── Value-equality plumbing (Dictionary-key correctness) ──

    [Fact]
    public void WorksAsADictionaryKey()
    {
        var dict = new Dictionary<SurfaceDeckKey, int>
        {
            [SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)))] = 1,
        };

        // A fresh, value-equal key finds the same slot (the revisit cache hit) …
        Assert.True(dict.ContainsKey(SurfaceDeckKey.For("miranda", "Miranda", Caches(("c1", 3, -50, 2)))));
        // … while a changed set misses (the honest rebuild).
        Assert.False(dict.ContainsKey(SurfaceDeckKey.For("miranda", "Miranda", Caches())));
    }
}
