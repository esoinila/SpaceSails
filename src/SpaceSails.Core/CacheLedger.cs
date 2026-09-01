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

    /// <summary>The sentinel <see cref="LastCheckedPeriod"/> carries while no watch is running — nothing
    /// of ours is in the ground yet, or a save from before the watch was persisted has not been re-seeded.</summary>
    public const long WatchNotStarted = -1;

    /// <summary>The last discovery period (<c>DiscoveryRule.PeriodIndex</c>) this hoard was rolled through
    /// — the watch's own bookmark, so a warp that skips days cannot skip a roll.
    ///
    /// <para>It lives HERE, on the ledger it governs, rather than beside it in the client: the caches and
    /// the clock that threatens them are one fact, and the vault must carry both or the promise made at
    /// bury time ("rivals may dig it up over the coming days") quietly stops being true after a resume.
    /// <see cref="WatchNotStarted"/> means no watch is running.</para></summary>
    public long LastCheckedPeriod { get; set; } = WatchNotStarted;

    /// <summary>Wipe the ledger back to a fresh, empty hoard — the per-game-thread reset (feat/game-threads,
    /// owner 2026-07-18): a NEW voyage is a NEW universe, so a chest we know of in one run must NOT be known
    /// in the next. Also rewinds the mint counter so the fresh run's first burial mints from zero, and stops
    /// the discovery watch — a universe with no hoards has nothing to roll for.</summary>
    public void Clear()
    {
        _caches.Clear();
        _seq = 0;
        LastCheckedPeriod = WatchNotStarted;
    }

    /// <summary>A fresh, owner-scoped id + mint index for the next burial (monotonic, so two burials
    /// on the same body at the same instant never collide their map seed).</summary>
    public int NextMintIndex() => _seq;

    /// <summary>Bury a freshly minted chest: it goes to the front of the ledger and its map is now
    /// ours to view any time. Returns the stored cache (with its final id and map text).</summary>
    public TreasureCache Bury(string bodyId, int coin, IReadOnlyList<CacheCargo> cargo, double simTime, string owner, bool playerOwned, int reeverLevel = 0, double? digX = null, double? digY = null, int? siteIndex = null)
    {
        int mint = _seq++;
        string id = $"cache-{(playerOwned ? "you" : "npc")}-{mint}";
        TreasureCache cache = CacheMint.Bury(id, bodyId, mint, coin, cargo, simTime, owner, playerOwned, reeverLevel, digX, digY, siteIndex);
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

    /// <summary>The known caches buried at a body — every ground on it. This is the BOOK's read (the ledger's
    /// 🗺 section, the destination board's "there's a chest down there" hint): a captain in orbit is owed every
    /// chest on the moon. The GROUND's read, which must not show a ✗ on a site the shovel never touched, is
    /// <see cref="CachesAt(string, int)"/> (#650).</summary>
    public IEnumerable<TreasureCache> CachesAt(string bodyId) =>
        _caches.Where(c => c.BodyId == bodyId);

    /// <summary>#650 · The known caches diggable from ONE ground: the caches at a body whose site matches the
    /// one being walked. Since #320 a body's 2–4 landing sites all rebuild the same local coordinate frame, so
    /// this — not <see cref="CachesAt(string)"/> — is what the surface deck, the ✗ marks and the dig may read.
    /// A null-site (legacy/rumour) cache still answers for every site of its body, so no saved chest is ever
    /// stranded by the new field.</summary>
    public IEnumerable<TreasureCache> CachesAt(string bodyId, int siteIndex) =>
        _caches.Where(c => c.BodyId == bodyId && c.IsOnSite(siteIndex));

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

    /// <summary>How many chests of OURS are in the ground (a rival's map we hold is not a holding).</summary>
    public int OwnCacheCount => _caches.Count(c => c.PlayerOwned);

    /// <summary>
    /// The ledger's one-line read of the hoard: what is ours, underground, and therefore off the books —
    /// "🪙 2,400 cr + 7 units (3 hot) in the ground across 2 caches". Empty when we have buried nothing,
    /// so the section simply shows its maps.
    ///
    /// <para>The numbers behind it (<see cref="BuriedCoin"/>, <see cref="BuriedHotUnits"/>) have existed
    /// since the hoard shipped and nothing ever read them: the one figure a captain wants from this page —
    /// how much of the fortune is out of a boarding party's reach, and how much of it is evidence — lived
    /// only in Core. A truth that lives only in Core is not being told.</para>
    /// </summary>
    public string HoardLine()
    {
        int chests = OwnCacheCount;
        if (chests == 0)
        {
            return "";
        }
        int coin = BuriedCoin;
        int units = _caches.Where(c => c.PlayerOwned).Sum(c => c.TotalCargoUnits);
        int hot = BuriedHotUnits;

        var parts = new List<string>();
        if (coin > 0)
        {
            parts.Add($"{coin:N0} cr");
        }
        if (units > 0)
        {
            parts.Add(hot > 0 ? $"{units} units ({hot} hot)" : $"{units} units");
        }
        string what = parts.Count == 0 ? "nothing left worth digging" : string.Join(" + ", parts);
        string where = chests == 1 ? "one cache" : $"{chests} caches";
        return $"🪙 {what} in the ground across {where} — off the books while it stays there.";
    }
}
