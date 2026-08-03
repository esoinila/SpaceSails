namespace SpaceSails.Core;

/// <summary>
/// One thread's assembly of PROJEKTI KAAMOS (issue #411): which lore fragments this universe's captain
/// has gathered, and therefore how close they are to earning the way to the ice moon. The
/// <see cref="CacheLedger"/>/<see cref="ContactLedger"/> pattern — a plain mutable holder that drops
/// into game state, is wiped per game-thread (a new voyage is a new universe, so a shard learned in one
/// run is unknown in the next), and round-trips through the Vault as a flat list of assembled ids
/// (<see cref="KaamosSection"/>).
///
/// <para><b>Assembly, not a checklist.</b> A fragment is <i>assembled</i> the moment its system hands it
/// over; the order the player finds them in does not matter and is not stored — only WHICH are held.
/// Ids the pool no longer knows are dropped on load (tolerant), so a renamed shard never poisons a save.
/// The reach logic that reads this state is pure and lives in <see cref="KaamosLore"/>.</para>
/// </summary>
public sealed class KaamosProgress
{
    // A set: assembling the same shard twice is idempotent, and membership is all the reach logic asks.
    private readonly HashSet<string> _assembled = new(StringComparer.Ordinal);

    /// <summary>#635 · THE FRONT DOOR. True once this captain has put their own hull's number on a
    /// consignment for the sealed ice-moon berth and watched the board send it back.
    ///
    /// <para>It is deliberately NOT a fragment. The bounce states nothing and credits nothing toward the
    /// gate — <see cref="KaamosLore.IntelAssembled"/> is unchanged by it, and so is
    /// <see cref="KaamosLore.CanReachEnceladus"/>. What it changes is that the arc <i>exists</i> for this
    /// captain: before it, PROJEKTI KAAMOS was invisible until somebody happened to read the whole of one
    /// plate among seven, and the ledger card that would have said so only appeared once a shard was
    /// already in hand. A returned filing is the world being consistent — a berth that is HELD rather than
    /// closed will refuse a filing and say so on the docket — rather than the fiction arriving early
    /// (#380's law).</para></summary>
    public bool BerthFilingBounced { get; private set; }

    /// <summary>The assembled fragments, returned in the pool's authored (canonical) order — never in
    /// discovery or hash order — so the ledger renders stably and the vault serialises deterministically.
    /// Only ids the current pool still recognises appear (a stray saved id is held internally but not
    /// projected as a real fragment).</summary>
    public IReadOnlyList<KaamosFragment> Assembled =>
        [.. KaamosLore.Fragments.Where(f => _assembled.Contains(f.Id))];

    /// <summary>The assembled ids in canonical order — the exact, minimal shape the vault stores
    /// (<see cref="KaamosSection.AssembledFragmentIds"/>). Unknown-to-the-pool ids are dropped here so a
    /// save is always re-loadable against the pool that wrote it.</summary>
    public IReadOnlyList<string> AssembledIds =>
        [.. KaamosLore.Fragments.Where(f => _assembled.Contains(f.Id)).Select(f => f.Id)];

    /// <summary>How many recognised fragments are held (key included). For the plain "3 / 6" ledger read.</summary>
    public int Count => AssembledIds.Count;

    /// <summary>True once this shard is in hand. The one question the reach predicates ask.</summary>
    public bool Has(string fragmentId) => _assembled.Contains(fragmentId);

    /// <summary>Assemble a fragment by id — idempotent, and it must be a REAL pool fragment (a typo'd or
    /// retired id is refused, so the set can never hold a phantom). Returns true only on the edge that
    /// first adds a recognised piece, so a caller can save-on-change and narrate a genuinely new find.</summary>
    public bool Assemble(string fragmentId)
    {
        if (KaamosLore.ById(fragmentId) is null)
        {
            return false;
        }

        return _assembled.Add(fragmentId);
    }

    /// <summary>#635 · Record that the board sent a filing back. Returns true only on the single edge that
    /// first sets it, so the caller narrates a genuinely new bounce, saves on the change, and a second
    /// attempt at another counter stays quiet. Idempotent — the same shape as
    /// <see cref="NebulaProgress.MarkConvergenceSeen"/>, and for the same reason: a one-time beat that a
    /// reload must not re-fire.</summary>
    public bool MarkBerthFilingBounced()
    {
        if (BerthFilingBounced)
        {
            return false;
        }

        BerthFilingBounced = true;
        return true;
    }

    /// <summary>Rehydrate from a saved list of ids (the vault load path), tolerant: unknown ids are
    /// skipped, duplicates collapse, order is irrelevant. Additive over whatever is already held — and so
    /// is the bounce bit, which can only ever go from false to true within a thread.</summary>
    public void Load(IEnumerable<string>? assembledIds, bool berthFilingBounced = false)
    {
        BerthFilingBounced |= berthFilingBounced;

        if (assembledIds is null)
        {
            return;
        }

        foreach (string id in assembledIds)
        {
            if (KaamosLore.ById(id) is not null)
            {
                _assembled.Add(id);
            }
        }
    }

    /// <summary>Wipe back to nothing assembled — the per-game-thread reset (a new voyage is a new
    /// universe), matching <see cref="CacheLedger.Clear"/>. The front door closes with it: a captain in a
    /// fresh universe has never had a filing come back.</summary>
    public void Clear()
    {
        _assembled.Clear();
        BerthFilingBounced = false;
    }

    // ── Convenience reads that defer to the pure reach logic, so callers touch one object. ──

    /// <summary>Intel shards assembled (the key never counts) — <see cref="KaamosLore.IntelAssembled"/>.</summary>
    public int IntelAssembled => KaamosLore.IntelAssembled(this);

    /// <summary>Enough intel assembled to earn the capstone — <see cref="KaamosLore.HasEnoughIntelToEarnTheKey"/>.</summary>
    public bool HasEnoughIntelToEarnTheKey => KaamosLore.HasEnoughIntelToEarnTheKey(this);

    /// <summary>The whole gate: the reach is earned — <see cref="KaamosLore.CanReachEnceladus"/>.</summary>
    public bool CanReachEnceladus => KaamosLore.CanReachEnceladus(this);
}
