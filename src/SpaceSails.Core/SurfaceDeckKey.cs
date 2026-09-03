using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #371 Phase 1 · The memoization key for the built surface deck (the client's <c>MoonSurface.SurfaceDeck</c>).
/// The perf study cites <see cref="SurfaceLayout.For"/> as pure/deterministic, so the WHOLE surface layout
/// (the fence, tube, per-body ruin/maze, kiosk, the way home, every ✗ dig console and label) is a pure
/// function of a small set of inputs:
/// <list type="bullet">
/// <item>the body id — drives the geography and the deep-area location line,</item>
/// <item>the body display name — drives the surface labels and the location line,</item>
/// <item>the captain's OWN caches in this ground — each ✗ plants a 🗺 dig console at its recorded spot.</item>
/// </list>
///
/// <para>Deliberately absent: the droid buffer size and the fill-droids delegate. Those are NOT layout —
/// they are re-bound fresh on every build (the delegate is bound to the live game component and would go
/// stale across sessions), so they must never key the layout cache.</para>
///
/// <para>The invariant the client's cache leans on, and the one this key is tested for: identical inputs
/// produce value-EQUAL keys (a revisit to a moon with the same caches reuses the built deck), and any
/// bury / lift / drop that changes the own-cache set produces a DIFFERENT key — an honest rebuild, never a
/// stale ✗.</para>
/// </summary>
public sealed class SurfaceDeckKey : IEquatable<SurfaceDeckKey>
{
    /// <summary>One own cache as it feeds the layout: its id, its ✗ spot, and the standing watchdog level
    /// (all of it is honestly part of the key so no input change can slip past the cache).</summary>
    public readonly record struct Cache(string Id, double X, double Y, int ReeverLevel);

    public string BodyId { get; }
    public string BodyDisplayName { get; }

    /// <summary>#320 · The chosen landing site's layout salt (empty = the body's canon site 0). Part of the
    /// key so two sites on the same body cache — and rebuild — as distinct grounds, never colliding.</summary>
    public string SiteSalt { get; }

    /// <summary>#586 · Which of the monolith's slow visit-windows this deck was built for
    /// (<see cref="Monolith.EpochAt"/>). Part of the key because what lies at the foot of the landmark
    /// CHANGES between windows — owner: <i>"some items appearing there now and then"</i> — and a cache that
    /// ignored it would keep serving a deck whose console says something is there long after it is not.
    /// That is the map lying, which this project has paid for more than once.</summary>
    public long MonolithEpoch { get; }

    /// <summary>#585 · Whether this body hides a clandestine site, as the GAME decided it (the seed, or the
    /// ?secretlab= cheat). Part of the key because a cached deck built without the lift head would keep being
    /// served to a captain who is standing on a body that has one.</summary>
    public bool HasSecretSite { get; }

    /// <summary>#1074 · Whether this ground is fenced, signed and under study
    /// (<see cref="PreservationZone.On"/>). Part of the key for <see cref="HasSecretSite"/>'s exact reason,
    /// one beat along: a preserved site grows a rail and a notice on the surface, and a cached deck built
    /// before the office took the site into care would go on being served to a captain standing in a zone
    /// the world says is there. The register is written on a descent, so this flips at most once in a
    /// voyage and costs one honest rebuild when it does.</summary>
    public bool Preserved { get; }

    private readonly Cache[] _caches;
    private readonly int _hash;

    private SurfaceDeckKey(
        string bodyId, string bodyDisplayName, string siteSalt, Cache[] caches, long monolithEpoch,
        bool hasSecretSite, bool preserved)
    {
        BodyId = bodyId;
        BodyDisplayName = bodyDisplayName;
        SiteSalt = siteSalt;
        MonolithEpoch = monolithEpoch;
        HasSecretSite = hasSecretSite;
        Preserved = preserved;
        _caches = caches;

        var hc = new HashCode();
        hc.Add(bodyId, StringComparer.Ordinal);
        hc.Add(bodyDisplayName, StringComparer.Ordinal);
        hc.Add(siteSalt, StringComparer.Ordinal);
        hc.Add(monolithEpoch);
        hc.Add(hasSecretSite);
        hc.Add(preserved);
        hc.Add(caches.Length);
        foreach (Cache c in caches)
        {
            hc.Add(c);
        }
        _hash = hc.ToHashCode();
    }

    /// <summary>Build a key from the <c>SurfaceDeck</c> inputs. The caches are copied defensively (the
    /// caller's list is mutable) and kept in the caller's order — the ledger is walked the same way on
    /// every build, so the order is stable per body and no sort is needed for equal-input → equal-key.</summary>
    public static SurfaceDeckKey For(
        string? bodyId, string? bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)>? ownCaches,
        string? siteSalt = null, long monolithEpoch = 0, bool hasSecretSite = false,
        bool preserved = false)
    {
        bodyId ??= "";
        bodyDisplayName ??= "";
        siteSalt ??= "";

        Cache[] caches;
        if (ownCaches is null || ownCaches.Count == 0)
        {
            caches = Array.Empty<Cache>();
        }
        else
        {
            caches = new Cache[ownCaches.Count];
            for (int i = 0; i < ownCaches.Count; i++)
            {
                (string id, double x, double y, int level) = ownCaches[i];
                caches[i] = new Cache(id ?? "", x, y, level);
            }
        }

        return new SurfaceDeckKey(
            bodyId, bodyDisplayName, siteSalt, caches, monolithEpoch, hasSecretSite, preserved);
    }

    public bool Equals(SurfaceDeckKey? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (_hash != other._hash || _caches.Length != other._caches.Length)
        {
            return false;
        }
        if (!string.Equals(BodyId, other.BodyId, StringComparison.Ordinal)
            || !string.Equals(BodyDisplayName, other.BodyDisplayName, StringComparison.Ordinal)
            || !string.Equals(SiteSalt, other.SiteSalt, StringComparison.Ordinal)
            || MonolithEpoch != other.MonolithEpoch
            || HasSecretSite != other.HasSecretSite
            || Preserved != other.Preserved)
        {
            return false;
        }
        for (int i = 0; i < _caches.Length; i++)
        {
            if (!_caches[i].Equals(other._caches[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as SurfaceDeckKey);

    public override int GetHashCode() => _hash;
}
