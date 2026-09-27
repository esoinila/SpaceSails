using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceSails.Core;

// #251 · ISlotStore and SaveSlotBook, moved whole out of SaveSlots.cs as a pure move: two top-level types,
// every line character for character and in order. SaveSlots.cs keeps the slot kind, the meta record, the
// labels and the file names; this file is the bookshelf that stores them. A whole type carries its own static
// initialiser (JsonOptions) with it, so no initialiser changes order relative to its class.

/// <summary>The keyed store a <see cref="SaveSlotBook"/> reads and writes — one method per localStorage
/// primitive. The client backs it with <c>RendererInterop.Vault*</c>; tests back it with a dictionary,
/// so the whole book is deterministic and browserless to test.</summary>
public interface ISlotStore
{
    /// <summary>The stored value for a key, or null if absent (or storage is unavailable).</summary>
    string? Read(string key);

    /// <summary>Write a value under a key.</summary>
    void Write(string key, string value);

    /// <summary>Forget a key.</summary>
    void Clear(string key);
}

/// <summary>
/// The bookshelf of vaults (#310): one rolling autosave plus nine manual banks, over any
/// <see cref="ISlotStore"/>. Manages the label manifest and per-slot payload keys; the vault JSON itself
/// passes through untouched (lossless per slot). Pure and deterministic — no clock, no browser: the
/// caller supplies each slot's <see cref="SaveSlotMeta"/> (including the real-time tick), so tests pin
/// "newest" exactly.
/// </summary>
public sealed class SaveSlotBook
{
    /// <summary>The manifest key of the UN-namespaced (default) book — the pre-thread shelf. The small JSON
    /// of labels (not the vaults themselves). A per-thread book (feat/game-threads) derives its own key.</summary>
    public const string ManifestKey = "spacesails.slots.v1";

    /// <summary>Per-slot payload key prefix of the default book; the full key is <c>PayloadPrefix + slotId</c>.
    /// A per-thread book folds its thread id into the prefix so two universes never collide a slot.</summary>
    public const string PayloadPrefix = "spacesails.slot.v1.";

    /// <summary>The pre-#310 single-slot key. Migrated into the autosave (and manual slot 1) on first run.
    /// Global (never namespaced): it predates both threads and the shelf.</summary>
    public const string LegacyKey = "spacesails.vault.v1";

    /// <summary>The one rolling autosave's slot id.</summary>
    public const string AutoSlotId = "auto";

    /// <summary>How many manual banks the shelf holds (ids "1".."9"); with the autosave that is ten.</summary>
    public const int ManualSlotCount = 9;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ISlotStore _store;

    // The per-thread key namespace (feat/game-threads). Empty for the default/legacy shelf — then the keys
    // are exactly the pre-thread constants, so old saves are read back byte-for-byte and every existing test
    // (which builds an un-namespaced book) still holds. Non-empty (a game-thread GUID) folds into the keys:
    //   manifest  → spacesails.thread.<id>.slots.v1
    //   payload   → spacesails.thread.<id>.slot.v1.<slotId>
    // so two universes each keep their own ten-slot shelf, sharing nothing (owner 2026-07-18, the "roadster
    // already found in a new game" leak: different game starts must not share state).
    private readonly string _manifestKey;
    private readonly string _payloadPrefix;

    /// <summary>The game-thread this book is namespaced under, or "" for the default (pre-thread) shelf.</summary>
    public string ThreadId { get; }

    public SaveSlotBook(ISlotStore store) : this(store, "")
    {
    }

    /// <summary>Open the shelf for one game thread. An empty <paramref name="threadId"/> is the default
    /// (un-namespaced) shelf — the pre-thread keys, for reading legacy saves and for the migration source.</summary>
    public SaveSlotBook(ISlotStore store, string threadId)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(threadId);
        _store = store;
        ThreadId = threadId;
        if (threadId.Length == 0)
        {
            _manifestKey = ManifestKey;
            _payloadPrefix = PayloadPrefix;
        }
        else
        {
            _manifestKey = $"spacesails.thread.{threadId}.slots.v1";
            _payloadPrefix = $"spacesails.thread.{threadId}.slot.v1.";
        }
    }

    /// <summary>The stable id of manual slot N (1..9).</summary>
    public static string ManualSlotId(int n) => n.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Copy every occupied slot (payload bytes + label) from another book into this one — the
    /// migration primitive (feat/game-threads): the pre-thread shelf is adopted wholesale into a freshly
    /// minted thread, losslessly, so nothing on the old shelf is lost when threads arrive.</summary>
    public void CopyFrom(SaveSlotBook source)
    {
        ArgumentNullException.ThrowIfNull(source);
        foreach (SaveSlotMeta meta in source.List())
        {
            if (source.ReadPayload(meta.Id) is { } payload)
            {
                Save(meta.Id, payload, meta);
            }
        }
    }

    /// <summary>Every occupied slot's label, NEWEST FIRST by the monotonic <see cref="SaveSlotMeta.SavedRealTicks"/>
    /// (autosave included) — so the top row is always the same save <see cref="Newest"/> returns and the Continue
    /// headline points at (#312 ordering law: the owner's Tilt autosave must be row 1, not sunk below older entries).
    /// Ties go to the autosave, then by id — the exact tie-break <see cref="Newest"/> uses, so "row 1 == Continue"
    /// holds after every save/import/autosave event. Slot numbers are row LABELS, never positions.</summary>
    public IReadOnlyList<SaveSlotMeta> List()
    {
        Manifest m = ReadManifest();
        return [.. m.Slots
            .OrderByDescending(s => s.SavedRealTicks)
            .ThenBy(s => s.Kind == SaveSlotKind.Autosave ? 0 : 1)
            .ThenBy(s => s.Id, StringComparer.Ordinal)];
    }

    /// <summary>The label for one slot id, or null if that slot is empty.</summary>
    public SaveSlotMeta? Get(string slotId)
        => ReadManifest().Slots.FirstOrDefault(s => s.Id == slotId);

    /// <summary>The slot Continue resumes: the most-recently-saved one (greatest real tick; ties go to the
    /// autosave). Null when the shelf is empty. This is "where I actually am" — the autosave, as it plays.</summary>
    public SaveSlotMeta? Newest()
    {
        SaveSlotMeta? best = null;
        foreach (SaveSlotMeta s in ReadManifest().Slots)
        {
            if (best is null
                || s.SavedRealTicks > best.SavedRealTicks
                || (s.SavedRealTicks == best.SavedRealTicks && s.Kind == SaveSlotKind.Autosave))
            {
                best = s;
            }
        }

        return best;
    }

    /// <summary>The vault JSON stored in a slot, or null if the slot is empty.</summary>
    public string? ReadPayload(string slotId) => _store.Read(_payloadPrefix + slotId);

    /// <summary>Bank a vault into a slot: write its payload byte-for-byte and upsert its label. The
    /// autosave uses <see cref="AutoSlotId"/>; a manual bank uses <see cref="ManualSlotId"/>.</summary>
    public void Save(string slotId, string vaultJson, SaveSlotMeta meta)
    {
        ArgumentException.ThrowIfNullOrEmpty(slotId);
        ArgumentNullException.ThrowIfNull(vaultJson);
        ArgumentNullException.ThrowIfNull(meta);

        _store.Write(_payloadPrefix + slotId, vaultJson);

        Manifest m = ReadManifest();
        m.Slots.RemoveAll(s => s.Id == slotId);
        m.Slots.Add(meta with { Id = slotId });
        WriteManifest(m);
    }

    /// <summary>
    /// #948 · RE-TITLE A SLOT IN PLACE — the label AND the file it labels, in one act.
    ///
    /// <para>The captain writes the page after the fact ("that was the run before the dive"), so a title and
    /// a note must be editable on a slot that is already banked. Two things carry them and both must move
    /// together, or the row and the exported file start disagreeing: the manifest label (what the drawer
    /// prints) and the payload's own <see cref="LogbookSection"/> (what a .json carries to another machine).
    /// So this writes both — the payload through <see cref="VaultSerializer.StampLogbook"/>, which is JSON
    /// surgery and leaves every other section byte-identical, checksum included.</para>
    ///
    /// <para>NOT a save: the voyage in the slot is not re-serialized and its clocks are untouched, so a
    /// retitled bank does not jump to the top of the newest-first list and does not become what Continue
    /// resumes. A no-op on an empty slot.</para>
    /// </summary>
    public void Retitle(string slotId, string? title, string? note, string? captainName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(slotId);
        Manifest m = ReadManifest();
        SaveSlotMeta? meta = m.Slots.FirstOrDefault(s => s.Id == slotId);
        if (meta is null)
        {
            return; // nothing banked here to write a page about
        }

        var page = new LogbookSection
        {
            // A rename is a separate act; passing null here means "keep whoever this slot says sailed it".
            CaptainName = SaveSlotLabels.CleanName(captainName ?? meta.CaptainName),
            Title = SaveSlotLabels.CleanTitle(title),
            Note = SaveSlotLabels.CleanNote(note),
        };

        if (ReadPayload(slotId) is { } raw && !string.IsNullOrWhiteSpace(raw))
        {
            _store.Write(_payloadPrefix + slotId, VaultSerializer.StampLogbook(raw, page));
        }

        m.Slots.RemoveAll(s => s.Id == slotId);
        m.Slots.Add(meta with { CaptainName = page.CaptainName, Title = page.Title, Note = page.Note });
        WriteManifest(m);
    }

    /// <summary>Empty a slot: forget its payload and drop its label. A no-op on an already-empty slot.</summary>
    public void Delete(string slotId)
    {
        _store.Clear(_payloadPrefix + slotId);
        Manifest m = ReadManifest();
        if (m.Slots.RemoveAll(s => s.Id == slotId) > 0)
        {
            WriteManifest(m);
        }
    }

    /// <summary>True when there is a pre-#310 single-slot save to import and no shelf yet — the one-time
    /// migration condition (a manifest already present means we've migrated, so this is false).</summary>
    public bool NeedsMigration()
        => _store.Read(ManifestKey) is null && !string.IsNullOrWhiteSpace(_store.Read(LegacyKey));

    /// <summary>The legacy single-slot vault JSON, or null if none.</summary>
    public string? LegacyPayload() => _store.Read(LegacyKey);

    // ── Manifest (de)serialization. Tolerant: an unreadable manifest reads as an empty shelf rather
    //    than throwing, so a corrupt label file never bricks the load path (the vaults themselves survive
    //    under their own keys and can still be read directly). ──

    private Manifest ReadManifest()
    {
        string? raw = _store.Read(_manifestKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Manifest();
        }

        try
        {
            return JsonSerializer.Deserialize<Manifest>(raw, JsonOptions) ?? new Manifest();
        }
        catch
        {
            return new Manifest();
        }
    }

    private void WriteManifest(Manifest m)
        => _store.Write(_manifestKey, JsonSerializer.Serialize(m, JsonOptions));

    private sealed class Manifest
    {
        public int Version { get; set; } = 1;
        public List<SaveSlotMeta> Slots { get; set; } = [];
    }
}
