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

    /// <summary>The captains this universe has BURIED — the ones the piracy insurance replaced (Evening wind
    /// #20: on a death-resurrection the license changes hands, but the roster keeps the memory). Oldest
    /// first; appended by <see cref="CaptainSuccession.Succeed"/>. Additive registry data — a pre-succession
    /// thread reads it back as the empty default, and it survives every <see cref="GameThreadRegistry.Touch"/>
    /// like the born-on stamp.</summary>
    public IReadOnlyList<RetiredCaptain> Retired { get; init; } = [];

    /// <summary>The captain's SELFIES — the legend ledger, the "proof I was there" they'll show everyone
    /// (issue #400). Additive registry data (a pre-selfie thread reads it back empty) preserved across every
    /// <see cref="GameThreadRegistry.Touch"/> like the born-on stamp — but, unlike <see cref="Retired"/>,
    /// RESET on succession (<see cref="CaptainSuccession.Succeed"/>): a NEW captain inherits NONE, so the
    /// wall of fame is per-life (owner #398, a quiet Fail Forward beat). Appended by
    /// <see cref="GameThreadRegistry.AddSelfie"/>, deduped by <see cref="CapturedSelfie.SpotId"/>.</summary>
    public IReadOnlyList<CapturedSelfie> Selfies { get; init; } = [];

    // ── #640 · AND WHEN IT IS OVER ───────────────────────────────────────────────────────────────────
    //
    // Owner ruling, 2026-09-17: a captain who purges their OWN pattern out of a cold-archive node has no
    // rebirth left, and the next death is the last one. That is the first permadeath this game has ever
    // had, and a thread it happens to is not a run any more — so the front door must stop offering to
    // Continue into it. It still LISTS: the captain, the retirees, the selfies and every banked moment
    // stay exactly where they are, because a run ending is not a record being deleted.
    //
    // WRITTEN ONLY WHEN IT IS TRUE, like #563's grave: `false` is not emitted at all, so a registry
    // written by this build for a shelf with no ended thread on it is BYTE-IDENTICAL to the one its
    // predecessor wrote. The registry is the index every universe is found through; a silent rewrite of
    // it is not a small thing.

    /// <summary>#640 · This thread's run is OVER — the captain closed their own policy and then died, so
    /// there is no successor and never will be. <see cref="GameThreadRegistry.Active"/> and
    /// <see cref="GameThreadRegistry.Newest"/> skip it (Continue may not resume a dead run);
    /// <see cref="GameThreadRegistry.List"/> still returns it, because the logbook has to be able to show
    /// the player what became of that captain. Set once, by <see cref="GameThreadRegistry.Close"/>, and
    /// never cleared.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Ended { get; init; }
}

/// <summary>One former captain kept in a thread's history (Evening wind #20): the name that held the
/// license, and the sim day the insurance wrote them off. Plain, JSON-friendly data.</summary>
public sealed record RetiredCaptain(string Name, int SimDay)
{
    /// <summary>Parameterless default for tolerant JSON round-trips (a garbled entry reads as blanks
    /// rather than throwing the whole index).</summary>
    public RetiredCaptain() : this("", 0) { }

    // ── #563 · AND WHERE HE FELL ─────────────────────────────────────────────────────────────────────
    //
    // Owner ruling, 2026-09-13: "I love the own lineage. If not enough material, fill in with strangers,
    // preferably NPCs we know something about." A breadcrumb out of your own lineage needs a PLACE, and a
    // name-and-a-day is not one. Written once, by the succession, off the death record the client is
    // already holding; read back by every later excursion that walks that ground.
    //
    // WRITTEN ONLY WHEN IT IS WRITEABLE. Null for every death that did not happen standing on a landing
    // party's regolith (CaptainGrave.CanRecord), and null for every retiree recorded before this existed —
    // and in that case the JSON says nothing at all rather than `"Grave":null`, so a registry written by
    // this build for a save with no graves in it is BYTE-IDENTICAL to the one its predecessor wrote. The
    // registry is the index every universe is found through; a silent rewrite of it is not a small thing.

    /// <summary>#563 · The ground this captain died on, or null — either the death had no ground, or the
    /// row predates the record. See <see cref="CaptainGrave"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CaptainGrave? Grave { get; init; }
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

    /// <summary>The most-recently-active thread the game can still be PLAYED in (greatest tick), or null
    /// when no such thread exists. #640: an <see cref="GameThreadInfo.Ended"/> thread is skipped — its
    /// captain has no pattern on file and no successor, so resuming it is resuming a dead run. It is still
    /// on the shelf (<see cref="List"/>); it is simply not a candidate for Continue.</summary>
    public GameThreadInfo? Newest()
    {
        GameThreadInfo? best = null;
        foreach (GameThreadInfo t in ReadIndex().Threads)
        {
            if (t.Ended)
            {
                continue; // #640 · the run is over; Continue does not lead here
            }

            if (best is null || t.LastActiveTicks > best.LastActiveTicks
                || (t.LastActiveTicks == best.LastActiveTicks && string.CompareOrdinal(t.Id, best.Id) < 0))
            {
                best = t;
            }
        }

        return best;
    }

    /// <summary>The thread the game should resume: the explicitly-active one if it still exists and is
    /// still playable, else the newest that is. This is "the run I was last in" (owner's Continue law),
    /// robust to an active id that was since deleted — and, since #640, to one whose captain died with no
    /// pattern on file. A shelf on which EVERY thread has ended answers null, exactly as an empty one
    /// does: there is nothing to continue, and the door offers a new voyage.</summary>
    public GameThreadInfo? Active()
    {
        Index idx = ReadIndex();
        if (idx.ActiveId is { } id
            && idx.Threads.FirstOrDefault(t => t.Id == id) is { Ended: false } active)
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
        // The retired-captain history is minted-once data like the born-on stamp: an autosave must never
        // wipe the memory of who held the license before (Evening wind #20). The current captain's selfie
        // album (#400) is carried the same way — an autosave never clears the legend ledger; only a
        // succession does (CaptainSuccession.Succeed).
        IReadOnlyList<RetiredCaptain> retired = existing?.Retired ?? [];
        IReadOnlyList<CapturedSelfie> selfies = existing?.Selfies ?? [];
        // #640 · …and so is the END. A touch is a stamp, not a resurrection: if this thread's captain died
        // with no pattern on file, no later write of any kind puts the run back on its feet. Carried like
        // the born-on stamp for exactly that reason — a stray autosave landing after the last death must
        // not quietly hand Continue a dead run back.
        bool ended = existing?.Ended ?? false;
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
            Retired = retired,
            Selfies = selfies,
            Ended = ended,
        });
        idx.ActiveId = id;
        WriteIndex(idx);
    }

    /// <summary>
    /// #640 · CLOSE THE THREAD — the run is over, and this is the only thing in the game that says so.
    ///
    /// <para>Owner ruling 2026-09-17, option A: a captain who pulled the purge handle on a node holding
    /// their OWN pattern has nothing on file, so their next death is the last one. No successor is issued,
    /// the clinic never plays, and this marks the universe they leave behind.</para>
    ///
    /// <para><b>What it does NOT do.</b> It deletes nothing: the ten-slot book, the retirees, the graves
    /// and the selfies are all exactly where they were, and <see cref="List"/> still returns the row so
    /// the logbook can show the player what became of that captain. It does not touch any other thread,
    /// and it does not bump a clock — dying is not activity. What it does is make the row unreachable by
    /// <see cref="Active"/> and <see cref="Newest"/>, which is what "Continue does not lead here" means in
    /// this codebase, and clear the stored active id if it pointed here so the front door is not holding a
    /// dead run by the hand.</para>
    ///
    /// <para>Idempotent: returns the row (already ended or now ended), or null if the thread is unknown —
    /// a legacy, unindexed run, which the client guards, and which simply has no row to close.</para>
    /// </summary>
    public GameThreadInfo? Close(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        Index idx = ReadIndex();
        GameThreadInfo? existing = idx.Threads.FirstOrDefault(t => t.Id == id);
        if (existing is null)
        {
            return null;
        }

        if (existing.Ended)
        {
            return existing; // already closed — pulling a handle twice does not end a run twice
        }

        GameThreadInfo ended = existing with { Ended = true };
        idx.Threads.RemoveAll(t => t.Id == id);
        idx.Threads.Add(ended);
        if (idx.ActiveId == id)
        {
            idx.ActiveId = null; // the door falls back to the newest thread that is still a run
        }

        WriteIndex(idx);
        return ended;
    }

    /// <summary>Issue a NEW CAPTAIN onto a thread after a death-resurrection (Evening wind #20): roll a
    /// fresh seeded name + a differing face and append the retiree to the thread's history, persisting both
    /// onto the row (the identity is editable data). Returns the updated row so the caller can narrate the
    /// hand-over, or null if the thread is unknown (e.g. a legacy/unindexed run). Keeps the thread active —
    /// it is still the run you are in — without bumping its clocks.</summary>
    /// <param name="grave">#563 · The ground the retiring captain died on, when there was one — handed
    /// straight down to <see cref="CaptainSuccession.Succeed"/>, which is the rule that decides whether it
    /// is keepable. Null on every death that happened anywhere but a landing party's regolith.</param>
    public GameThreadInfo? IssueSuccessor(string id, int retiredSimDay, CaptainGrave? grave = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        Index idx = ReadIndex();
        GameThreadInfo? existing = idx.Threads.FirstOrDefault(t => t.Id == id);
        if (existing is null)
        {
            return null; // no row to succeed — a legacy run narrates generically, the client guards this
        }

        GameThreadInfo successor = CaptainSuccession.Succeed(existing, retiredSimDay, grave);
        idx.Threads.RemoveAll(t => t.Id == id);
        idx.Threads.Add(successor);
        idx.ActiveId = id;
        WriteIndex(idx);
        return successor;
    }

    /// <summary>File a captured selfie into a thread's legend ledger (issue #400): append it to the active
    /// captain's album and persist onto the row (like the retired history — but reset on succession). DEDUPED
    /// by <see cref="CapturedSelfie.SpotId"/>: the same spot/beat is one shot per life, so re-viewing a spot
    /// never spams the ledger. Returns the updated row (with the selfie present), or null if the thread is
    /// unknown (a legacy/unindexed run — the client guards this and just shows the shot). Keeps the thread
    /// active without bumping its clocks (a photo is not a durable game event).</summary>
    public GameThreadInfo? AddSelfie(string id, CapturedSelfie selfie)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(selfie);
        Index idx = ReadIndex();
        GameThreadInfo? existing = idx.Threads.FirstOrDefault(t => t.Id == id);
        if (existing is null)
        {
            return null; // no row to file into — a legacy run just shows the shot, the client guards this
        }

        // Already in the ledger (same spot/beat) — return the row unchanged, so a re-view is idempotent.
        if (existing.Selfies.Any(s => s.SpotId == selfie.SpotId))
        {
            return existing;
        }

        var album = new List<CapturedSelfie>(existing.Selfies) { selfie };
        GameThreadInfo updated = existing with { Selfies = album };
        idx.Threads.RemoveAll(t => t.Id == id);
        idx.Threads.Add(updated);
        idx.ActiveId = id;
        WriteIndex(idx);
        return updated;
    }

    /// <summary>
    /// #948 · GIVE THE CAPTAIN A NAME OF YOUR OWN. Owner: <i>"Let's have an option to change the name of our
    /// avatar. It solves the issue of forgetting who you are amongst the autogenerated names. Making it
    /// personal makes it easy to remember, and having it updatable."</i>
    ///
    /// <para>The identity was always DATA on the row (that is why <see cref="Touch"/> preserves it across
    /// every autosave, like the born-on stamp) — it simply had no door. This is the door. Writes the cleaned
    /// name (<see cref="Captains.CleanName"/>) and NOTHING else: no clock is bumped, so renaming an old
    /// captain does not falsely promote their universe to "newest", and the active thread is not switched —
    /// you may name a captain you are not currently sailing.</para>
    ///
    /// <para>A blank name CLEARS the override, and the row falls back to the seeded roster name
    /// (<see cref="Captains.Name"/>) — so "updatable" includes updating back to whatever the generator said.
    /// Returns the updated row, or null when the thread is unknown.</para>
    /// </summary>
    public GameThreadInfo? Rename(string id, string? name)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        Index idx = ReadIndex();
        GameThreadInfo? existing = idx.Threads.FirstOrDefault(t => t.Id == id);
        if (existing is null)
        {
            return null;
        }

        string cleaned = Captains.CleanName(name);
        GameThreadInfo renamed = existing with
        {
            // Blank = "back to the generator". Storing the seeded name rather than "" keeps every reader
            // (Captains.For, the vault's logbook page, an exported file) reading ONE field.
            CaptainName = cleaned.Length > 0 ? cleaned : Captains.Name(id),
        };
        idx.Threads.RemoveAll(t => t.Id == id);
        idx.Threads.Add(renamed);
        WriteIndex(idx);
        return renamed;
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
