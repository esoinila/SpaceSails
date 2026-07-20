namespace SpaceSails.Core;

/// <summary>
/// One thread's assembly of the NEBULA MUTUAL truth (issue #422): which lore fragments this universe's
/// captain has gathered about what their resurrections really are, and therefore how close they are to the
/// contract's true terms. The <see cref="KaamosProgress"/> idiom (which mirrors
/// <see cref="CacheLedger"/>/<see cref="ContactLedger"/>) — a plain mutable holder that drops into game
/// state, is wiped per game-thread (a new voyage is a new universe, so a shard learned in one run is unknown
/// in the next), and round-trips through the Vault as a flat list of assembled ids plus a one-time flag
/// (<see cref="NebulaSection"/>).
///
/// <para><b>Assembly, not a checklist.</b> A fragment is <i>assembled</i> the moment its system hands it
/// over; the order the player finds them in does not matter and is not stored — only WHICH are held. Ids the
/// pool no longer knows are dropped on load (tolerant), so a renamed shard never poisons a save. The fine-
/// print logic that reads this state is pure and lives in <see cref="NebulaLore"/>; the cross-arc
/// convergence lives in <see cref="ArcConvergence"/>.</para>
///
/// <para><b>The convergence flag.</b> This holder also remembers whether the one-time CONVERGENCE reveal has
/// already fired for this thread (<see cref="ConvergenceSeen"/>) — the analog of
/// <see cref="NerveSection.MonolithSeen"/> — so the biggest reveal in the game plays once in a captain's
/// universe and never re-fires on a reload. It rides in the same vault section as the fragments.</para>
/// </summary>
public sealed class NebulaProgress
{
    // A set: assembling the same shard twice is idempotent, and membership is all the fine-print logic asks.
    private readonly HashSet<string> _assembled = new(StringComparer.Ordinal);

    /// <summary>The assembled fragments, returned in the pool's authored (canonical) order — never in
    /// discovery or hash order — so the ledger renders stably and the vault serialises deterministically.
    /// Only ids the current pool still recognises appear (a stray saved id is held internally but not
    /// projected as a real fragment).</summary>
    public IReadOnlyList<NebulaFragment> Assembled =>
        [.. NebulaLore.Fragments.Where(f => _assembled.Contains(f.Id))];

    /// <summary>The assembled ids in canonical order — the exact, minimal shape the vault stores
    /// (<see cref="NebulaSection.AssembledFragmentIds"/>). Unknown-to-the-pool ids are dropped here so a save
    /// is always re-loadable against the pool that wrote it.</summary>
    public IReadOnlyList<string> AssembledIds =>
        [.. NebulaLore.Fragments.Where(f => _assembled.Contains(f.Id)).Select(f => f.Id)];

    /// <summary>How many recognised fragments are held (key included). For the plain "3 / 6" ledger read.</summary>
    public int Count => AssembledIds.Count;

    /// <summary>True once this shard is in hand. The one question the fine-print predicates ask.</summary>
    public bool Has(string fragmentId) => _assembled.Contains(fragmentId);

    /// <summary>True once the one-time CONVERGENCE reveal has fired for this thread (<see cref="ArcConvergence"/>).
    /// Persisted so the heaviest reveal in the game plays once in a captain's universe and never re-fires on a
    /// reload — the wiring lane sets this on the single edge it delivers the reveal. Cleared by
    /// <see cref="Clear"/> on a new voyage.</summary>
    public bool ConvergenceSeen { get; private set; }

    /// <summary>Assemble a fragment by id — idempotent, and it must be a REAL pool fragment (a typo'd or
    /// retired id is refused, so the set can never hold a phantom). Returns true only on the edge that first
    /// adds a recognised piece, so a caller can save-on-change and narrate a genuinely new find.</summary>
    public bool Assemble(string fragmentId)
    {
        if (NebulaLore.ById(fragmentId) is null)
        {
            return false;
        }

        return _assembled.Add(fragmentId);
    }

    /// <summary>Mark the one-time convergence reveal as delivered for this thread. Idempotent — returns true
    /// only on the edge that first sets it, so the wiring lane fires the reveal exactly once. Does nothing on
    /// a thread that has already seen it.</summary>
    public bool MarkConvergenceSeen()
    {
        if (ConvergenceSeen)
        {
            return false;
        }

        ConvergenceSeen = true;
        return true;
    }

    /// <summary>Rehydrate from a saved section (the vault load path), tolerant: unknown ids are skipped,
    /// duplicates collapse, order is irrelevant. Additive over whatever is already held; the convergence flag
    /// is OR-ed in so a save that recorded the reveal keeps it seen.</summary>
    public void Load(IEnumerable<string>? assembledIds, bool convergenceSeen = false)
    {
        if (convergenceSeen)
        {
            ConvergenceSeen = true;
        }

        if (assembledIds is null)
        {
            return;
        }

        foreach (string id in assembledIds)
        {
            if (NebulaLore.ById(id) is not null)
            {
                _assembled.Add(id);
            }
        }
    }

    /// <summary>Wipe back to nothing assembled and the reveal unseen — the per-game-thread reset (a new
    /// voyage is a new universe), matching <see cref="CacheLedger.Clear"/>.</summary>
    public void Clear()
    {
        _assembled.Clear();
        ConvergenceSeen = false;
    }

    // ── Convenience reads that defer to the pure fine-print logic, so callers touch one object. ──

    /// <summary>Intel shards assembled (the key never counts) — <see cref="NebulaLore.IntelAssembled"/>.</summary>
    public int IntelAssembled => NebulaLore.IntelAssembled(this);

    /// <summary>Enough intel assembled to earn the contract — <see cref="NebulaLore.HasEnoughIntelToEarnTheContract"/>.</summary>
    public bool HasEnoughIntelToEarnTheContract => NebulaLore.HasEnoughIntelToEarnTheContract(this);

    /// <summary>The whole gate for arc 2's own reveal: the truth is known — <see cref="NebulaLore.KnowsTheTruth"/>.</summary>
    public bool KnowsTheTruth => NebulaLore.KnowsTheTruth(this);
}
