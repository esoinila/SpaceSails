using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// Game threads (feat/game-threads, owner 2026-07-18): "game sessions should have a thread based on
// guid so different game starts don't share state with each other, like have the roadster already
// found in a new game." A THREAD is one universe — one whole ten-slot SaveSlotBook, keyed under the
// thread's GUID (SaveSlotBook's per-thread namespace). This registry is the thin index over them: which
// threads exist, which one is ACTIVE (the run the autosave writes and Continue resumes), and a tiny
// label per thread (WHERE + day + when-last-played) so a front door can name each without opening its
// whole book. The GUID itself is minted CLIENT-side (no Guid in Core — determinism law); this registry
// only records the string id it is handed. Lays the keying #310's ten-savegame picker will build on.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>One row of the thread index: a game universe's id, a human label, and the clocks that order
/// it. NOT the save payload (that lives in the thread's own <see cref="SaveSlotBook"/>) — just enough to
/// point Continue at the right universe and let a picker name the rest.</summary>
public sealed record GameThreadInfo
{
    /// <summary>The thread's GUID (minted client-side), the namespace of its <see cref="SaveSlotBook"/>.</summary>
    public string Id { get; init; } = "";

    /// <summary>The newest-slot WHERE line, mirrored here so the front door names the thread cheaply.</summary>
    public string Where { get; init; } = "";

    /// <summary>Sim day at last activity — the "day N" a picker shows beside the place.</summary>
    public int SimDay { get; init; }

    /// <summary>Monotonic real-time tick of the LAST durable event in this thread. The tie-free key for
    /// "newest thread" — Continue (with no explicit pick) resumes the greatest.</summary>
    public long LastActiveTicks { get; init; }

    /// <summary>Monotonic real-time tick when the thread was minted (its "born on" stamp).</summary>
    public long CreatedTicks { get; init; }

    // ── The captain's roster (owner 2026-07-19: "a list of captains ... then under those are their slots").
    //    Each thread (universe) IS a captain — a seeded NAME and an AVATAR picked from a fixed roster of
    //    profile images. Both are DATA (not hardcoded), seeded off the thread GUID so they are stable across
    //    reloads, yet re-assignable later (Evening wind #20: insurance issues a new captain). Added additively
    //    (registry JSON is versioned/tolerant): a thread saved before this field reads them back at their
    //    defaults and the client re-derives a stable identity from the id via <see cref="Captains"/>. ──

    /// <summary>The captain's display name for this universe, seeded from <see cref="Id"/> at creation. Empty
    /// on a pre-roster thread — the client re-derives a stable name from the id (<see cref="Captains.For"/>).</summary>
    public string CaptainName { get; init; } = "";

    /// <summary>Which avatar (1..<see cref="Captains.AvatarCount"/>) fronts this captain — the <c>art/captain-N.jpg</c>
    /// index, seeded from <see cref="Id"/>. Zero means unset (a pre-roster thread); the client derives one.</summary>
    public int AvatarIndex { get; init; }
}

/// <summary>
/// The index of game threads over an <see cref="ISlotStore"/> — pure and deterministic (no clock, no
/// browser, no GUID minting: the caller supplies each id and tick). Tolerant of a corrupt index (reads
/// as empty rather than throwing, so a garbled registry never bricks the load path — the universes
/// themselves survive under their own <see cref="SaveSlotBook"/> keys).
/// </summary>
public sealed class GameThreadRegistry
{
    /// <summary>The one registry key — the small JSON index of threads (not the vaults themselves).</summary>
    public const string RegistryKey = "spacesails.threads.v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ISlotStore _store;

    public GameThreadRegistry(ISlotStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <summary>Every known thread, NEWEST FIRST by <see cref="GameThreadInfo.LastActiveTicks"/> (ties by
    /// id, ordinal) — the order a "load a game" picker (#310) renders, freshest universe on top.</summary>
    public IReadOnlyList<GameThreadInfo> List()
    {
        Index idx = ReadIndex();
        return [.. idx.Threads
            .OrderByDescending(t => t.LastActiveTicks)
            .ThenBy(t => t.Id, StringComparer.Ordinal)];
    }

    /// <summary>The most-recently-active thread (greatest tick), or null when no thread exists yet.</summary>
    public GameThreadInfo? Newest()
    {
        GameThreadInfo? best = null;
        foreach (GameThreadInfo t in ReadIndex().Threads)
        {
            if (best is null || t.LastActiveTicks > best.LastActiveTicks
                || (t.LastActiveTicks == best.LastActiveTicks && string.CompareOrdinal(t.Id, best.Id) < 0))
            {
                best = t;
            }
        }

        return best;
    }

    /// <summary>The thread the game should resume: the explicitly-active one if it still exists, else the
    /// newest. This is "the run I was last in" (owner's Continue law), robust to an active id that was
    /// since deleted.</summary>
    public GameThreadInfo? Active()
    {
        Index idx = ReadIndex();
        if (idx.ActiveId is { } id && idx.Threads.FirstOrDefault(t => t.Id == id) is { } active)
        {
            return active;
        }

        return Newest();
    }

    /// <summary>The active thread's id, or null. Convenience over <see cref="Active"/>.</summary>
    public string? ActiveId => Active()?.Id;

    /// <summary>One thread's row, or null if unknown.</summary>
    public GameThreadInfo? Get(string id) => ReadIndex().Threads.FirstOrDefault(t => t.Id == id);

    /// <summary>No threads recorded yet — the true-first-run signal (before any migration or new game).</summary>
    public bool IsEmpty => ReadIndex().Threads.Count == 0;

    /// <summary>Upsert a thread and make it ACTIVE: stamp its label (WHERE + day) and last-active tick.
    /// Called when a new game is minted (first stamp) and on every durable autosave (keeps "newest" true
    /// and Continue current). Preserves the thread's original <see cref="GameThreadInfo.CreatedTicks"/>.</summary>
    public void Touch(string id, string where, int simDay, long ticks)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        Index idx = ReadIndex();
        GameThreadInfo? existing = idx.Threads.FirstOrDefault(t => t.Id == id);
        long created = existing?.CreatedTicks ?? ticks;
        // The captain identity is minted once and preserved across every touch (like CreatedTicks) — a
        // re-assignable pick (Evening wind #20) survives autosaves. A fresh thread seeds both from its GUID.
        string captain = existing?.CaptainName is { Length: > 0 } name ? name : Captains.Name(id);
        int avatar = existing is { AvatarIndex: > 0 } ? existing.AvatarIndex : Captains.AvatarIndex(id);
        idx.Threads.RemoveAll(t => t.Id == id);
        idx.Threads.Add(new GameThreadInfo
        {
            Id = id,
            Where = where ?? "",
            SimDay = simDay,
            LastActiveTicks = ticks,
            CreatedTicks = created,
            CaptainName = captain,
            AvatarIndex = avatar,
        });
        idx.ActiveId = id;
        WriteIndex(idx);
    }

    /// <summary>Point Continue at a thread WITHOUT touching its clocks — the picker's "load THIS universe"
    /// (a deliberate switch to an older thread must not falsely bump it to "newest").</summary>
    public void SetActive(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        Index idx = ReadIndex();
        if (idx.Threads.Any(t => t.Id == id))
        {
            idx.ActiveId = id;
            WriteIndex(idx);
        }
    }

    /// <summary>Forget a thread from the index (its <see cref="SaveSlotBook"/> payloads are cleared
    /// separately). If it was the active one, active falls back to newest on the next read.</summary>
    public void Remove(string id)
    {
        Index idx = ReadIndex();
        if (idx.Threads.RemoveAll(t => t.Id == id) > 0)
        {
            if (idx.ActiveId == id)
            {
                idx.ActiveId = null;
            }

            WriteIndex(idx);
        }
    }

    private Index ReadIndex()
    {
        string? raw = _store.Read(RegistryKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Index();
        }

        try
        {
            return JsonSerializer.Deserialize<Index>(raw, JsonOptions) ?? new Index();
        }
        catch
        {
            return new Index();
        }
    }

    private void WriteIndex(Index idx)
        => _store.Write(RegistryKey, JsonSerializer.Serialize(idx, JsonOptions));

    private sealed class Index
    {
        public int Version { get; set; } = 1;
        public string? ActiveId { get; set; }
        public List<GameThreadInfo> Threads { get; set; } = [];
    }
}

/// <summary>One captain's card in the roster: the thread (universe) they helm, whether they are the active
/// captain ("at the helm"), and their save slots newest-first. The unit a per-captain header groups (owner
/// 2026-07-19: "a list of captains ... then under those are their slots").</summary>
public sealed record GameThreadGroup
{
    public GameThreadInfo Thread { get; init; } = new();
    public bool IsActive { get; init; }
    public IReadOnlyList<SaveSlotMeta> Slots { get; init; } = [];
}

/// <summary>Pure grouping over the thread index — the roster the front-door picker renders (no clock, no
/// browser: the caller hands each thread's slots).</summary>
public static class GameThreads
{
    /// <summary>Group saved slots by their game thread (captain) for the roster: the ACTIVE captain first
    /// (marked), then the rest NEWEST-THREAD-FIRST (by <see cref="GameThreadInfo.LastActiveTicks"/> desc, ties
    /// by id ordinal); within each captain the slots stay NEWEST-FIRST (<see cref="SaveSlotMeta.SavedRealTicks"/>
    /// desc, the autosave winning ties — the same order <see cref="SaveSlotBook.List"/> and Continue use).
    /// Threads with no saved slot are dropped: an empty captain has nothing to show (and this is exactly how a
    /// truly-unmigrated pre-thread key stays out of the roster). Pure: <paramref name="slotsFor"/> supplies the
    /// slots per thread id.</summary>
    public static IReadOnlyList<GameThreadGroup> GroupSlots(
        IReadOnlyList<GameThreadInfo> threads,
        string? activeId,
        Func<string, IReadOnlyList<SaveSlotMeta>> slotsFor)
    {
        ArgumentNullException.ThrowIfNull(threads);
        ArgumentNullException.ThrowIfNull(slotsFor);

        var groups = new List<GameThreadGroup>();
        foreach (GameThreadInfo t in threads)
        {
            IReadOnlyList<SaveSlotMeta> slots = slotsFor(t.Id) ?? [];
            if (slots.Count == 0)
            {
                continue; // an empty universe has no card to draw
            }

            List<SaveSlotMeta> ordered = [.. slots
                .OrderByDescending(s => s.SavedRealTicks)
                .ThenBy(s => s.Kind == SaveSlotKind.Autosave ? 0 : 1)
                .ThenBy(s => s.Id, StringComparer.Ordinal)];

            groups.Add(new GameThreadGroup
            {
                Thread = t,
                IsActive = !string.IsNullOrEmpty(activeId) && t.Id == activeId,
                Slots = ordered,
            });
        }

        return [.. groups
            .OrderByDescending(g => g.IsActive)               // the active captain heads the roster
            .ThenByDescending(g => g.Thread.LastActiveTicks)  // then newest universe first
            .ThenBy(g => g.Thread.Id, StringComparer.Ordinal)];
    }
}
