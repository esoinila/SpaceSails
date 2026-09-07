using System;
using System.Collections.Concurrent;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1112 · The shared numbers for <see cref="BoundedMemo{TKey, TValue}"/>. A separate non-generic class
/// because a constant on a generic type is a constant per instantiation, and there is only one cap here.
/// </summary>
internal static class BoundedMemo
{
    /// <summary>
    /// The generous cap both world-builder memos have. It comes from <c>MoonSurface</c>'s
    /// <c>LayoutCacheCap</c> (#371 Phase 1): high enough that it never trips in play — a session walks a
    /// handful of grounds and docks at a handful of berths — and low enough that a long voyage's worth of
    /// keys nobody will ask for again cannot sit in memory for the rest of the process.
    /// </summary>
    internal const int DefaultCap = 64;
}

/// <summary>
/// #1112 · ONE CACHE POLICY FOR BOTH WORLD BUILDERS — a small bounded concurrent memo.
///
/// <para><b>Why this type exists at all.</b> The two generators that memoise (<c>MoonSurface</c>'s layout
/// cache and <c>HavenInterior</c>'s deck cache) were written as twins and then drifted. The moon's memo
/// grew a cap and a flush; the haven's did not, and because its key carries the docking watch — which
/// advances for ever — a long voyage left one deck in memory per watch, permanently. Two dictionaries with
/// one job and one of them bounded is a bug waiting to be re-filed against the other, so the policy now
/// lives in one place that both hold. Nobody has to remember to fix a cache twice.</para>
///
/// <para><b>The eviction rule is the moon's, unchanged: on overflow, start fresh.</b> Not an LRU — a
/// flush. Every entry is a pure function of its key, so the only cost of throwing one away is rebuilding
/// it, and a flush that fires once every sixty-odd misses (which in play is never) is cheaper to hold in
/// the head than a recency list that has to be right. The oldest entry leaves because they all do.</para>
///
/// <para><b>Concurrent, because the browser is not the only caller (#585, #649, #1108).</b> In WASM the
/// game is single-threaded and a plain <c>Dictionary</c> was safe; xUnit runs test classes in parallel and
/// it was not — that cost two afternoons, once as a shelter list that did not match the ground and once as
/// an oracle test failing one run in three. Reads are lock-free on the
/// <see cref="ConcurrentDictionary{TKey, TValue}"/>. The check-clear-insert on the miss path is taken under
/// a lock, which the moon's inline version did not do: without it two racing misses can both read a count
/// one short of the cap and both insert, so the cap is only nearly kept — and a cap that is only nearly
/// kept cannot be guarded, because the guard flakes. Building happens OUTSIDE the lock, so the critical
/// section is three dictionary operations and never a deck.</para>
///
/// <para><b>A racing double-build is waste, never a wrong answer.</b> Two callers that miss the same key at
/// once both build; the second insert wins the cache and the first caller walks off with an equal value
/// built from the same inputs. That is exactly what both memos did before this type and is safe for the
/// same reason: the builders are deterministic.</para>
/// </summary>
internal sealed class BoundedMemo<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _entries = new();

    /// <summary>Guards the check-clear-insert triple, so <see cref="Count"/> can never exceed
    /// <see cref="Cap"/> — not even for the instant between two racing misses.</summary>
    private readonly object _gate = new();

    internal BoundedMemo(int cap)
    {
        if (cap < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cap), cap, "A memo that can hold nothing is not a memo.");
        }
        Cap = cap;
    }

    /// <summary>The most entries this memo will ever hold.</summary>
    internal int Cap { get; }

    /// <summary>How many entries it holds right now. Never more than <see cref="Cap"/>.</summary>
    internal int Count => _entries.Count;

    /// <summary>Whether this key's value is in the memo — the eviction guard's only window in.</summary>
    internal bool Holds(TKey key) => _entries.ContainsKey(key);

    /// <summary>
    /// The cached value for <paramref name="key"/>, built by <paramref name="build"/> if it is not held.
    /// A hit and a miss hand back the same value: the builder is a pure function of the key, and nothing
    /// here touches what it returns.
    /// </summary>
    internal TValue GetOrBuild(TKey key, Func<TValue> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        if (_entries.TryGetValue(key, out TValue? hit))
        {
            return hit;
        }

        TValue built = build();
        lock (_gate)
        {
            if (_entries.Count >= Cap)
            {
                _entries.Clear();
            }
            _entries[key] = built;
        }
        return built;
    }
}
