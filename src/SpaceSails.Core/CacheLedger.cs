namespace SpaceSails.Core;

/// <summary>
/// The captain's hoards (#223) — every buried chest we know of, ours and any rival's whose map we
/// hold. The <see cref="ContactLedger"/> pattern: a plain mutable holder that drops into game state
/// and a future save layer just serializes <see cref="Caches"/>. Bury adds; dig removes and hands
/// the contents back.
///
/// <para><b>The confiscation seam (read this, BUSTED lane).</b> This ledger is the whole reason a
/// hoard is salvation: buried loot lives HERE, never in the ship's carried coin or hold. A boarding
/// confiscation takes what it can SEE aboard — so it reads the ship's purse and manifest and MUST
/// NOT consult this ledger. Buried coin and cargo are invisible by construction; there is no flag to
/// set. The only clean read the heat/evidence lane wants is <see cref="BuriedHotUnits"/> — the count
/// of stolen-flagged units currently underground and therefore off the books.</para>
/// </summary>
public sealed class CacheLedger
{
    private readonly List<TreasureCache> _caches = [];
    private int _seq;

    /// <summary>Every known cache, newest first (the ledger's render order and what a save serializes).</summary>
    public IReadOnlyList<TreasureCache> Caches => _caches;

    /// <summary>A fresh, owner-scoped id + mint index for the next burial (monotonic, so two burials
    /// on the same body at the same instant never collide their map seed).</summary>
    public int NextMintIndex() => _seq;

    /// <summary>Bury a freshly minted chest: it goes to the front of the ledger and its map is now
    /// ours to view any time. Returns the stored cache (with its final id and map text).</summary>
    public TreasureCache Bury(string bodyId, int coin, IReadOnlyList<CacheCargo> cargo, double simTime, string owner, bool playerOwned, int reeverLevel = 0, double? digX = null, double? digY = null)
    {
        int mint = _seq++;
        string id = $"cache-{(playerOwned ? "you" : "npc")}-{mint}";
        TreasureCache cache = CacheMint.Bury(id, bodyId, mint, coin, cargo, simTime, owner, playerOwned, reeverLevel, digX, digY);
        _caches.Insert(0, cache);
        return cache;
    }

    /// <summary>Register a cache we learned of from a map we DON'T own the burial of — a rumour map or
    /// a fetch-a-cache job. Same dig path; it just wasn't us that put it there. Idempotent on id.</summary>
    public TreasureCache Learn(TreasureCache cache)
    {
        if (!_caches.Any(c => c.Id == cache.Id))
        {
            _caches.Insert(0, cache);
        }
        return cache;
    }

    /// <summary>Rehydrate one cache verbatim in saved order (the personal-vault load path, #225).
    /// Unlike <see cref="Learn"/> it appends to the END, so replaying the saved <see cref="Caches"/>
    /// list front-to-back reproduces the exact newest-first order. Idempotent on id.</summary>
    public void Load(TreasureCache cache)
    {
        if (!_caches.Any(c => c.Id == cache.Id))
        {
            _caches.Add(cache);
        }
    }

    /// <summary>Restore the mint counter after a load so a freshly-buried chest cannot collide its id
    /// with a loaded one (#225). Clamped so a garbled value can never rewind the counter.</summary>
    public void RestoreMintIndex(int nextMintIndex) => _seq = Math.Max(_seq, Math.Max(0, nextMintIndex));

    /// <summary>The known caches buried at a body (the dig-here list for the shuttle pop-up).</summary>
    public IEnumerable<TreasureCache> CachesAt(string bodyId) =>
        _caches.Where(c => c.BodyId == bodyId);

    /// <summary>True when there is a chest to dig at a body.</summary>
    public bool HasCacheAt(string bodyId) => _caches.Any(c => c.BodyId == bodyId);

    /// <summary>Dig up a cache by id: remove it from the ledger and hand it back so the caller can
    /// return its contents to the ship. Null if it isn't known (already dug, or found by a rival).</summary>
    public TreasureCache? Dig(string cacheId)
    {
        int i = _caches.FindIndex(c => c.Id == cacheId);
        if (i < 0)
        {
            return null;
        }
        TreasureCache cache = _caches[i];
        _caches.RemoveAt(i);
        return cache;
    }

    /// <summary>Drop a cache a rival found before we got back to it (discovery risk, ruling 4).
    /// Returns the lost cache so the caller can file a ledger line and squawk.</summary>
    public TreasureCache? Remove(string cacheId) => Dig(cacheId);

    /// <summary>The clean read for the heat/evidence lane: stolen-flagged units currently buried
    /// across all OUR caches — evidence off the books while it stays underground. The confiscation
    /// itself never calls this (buried is invisible); the heat-decay assist does.</summary>
    public int BuriedHotUnits => _caches.Where(c => c.PlayerOwned).Sum(c => c.HotCargoUnits);

    /// <summary>Total coin buried across our own caches (for the ledger's hoard summary).</summary>
    public int BuriedCoin => _caches.Where(c => c.PlayerOwned).Sum(c => c.Coin);
}
