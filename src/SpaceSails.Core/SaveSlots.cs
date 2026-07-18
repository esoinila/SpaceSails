using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// Ten vaults and an honest door (#310). The personal vault (#225) round-trips ONE life; this is the
// bookshelf that holds many. The design law:
//   * The vault payload itself is UNTOUCHED — each slot stores a byte-for-byte VaultSerializer string,
//     so the determinism/lossless law (Vault.cs) still holds per slot. This file only manages WHICH
//     slot, and the human-readable LABEL beside each (where/when/build stamp).
//   * ONE rolling AUTOSAVE slot follows the ship (rewritten on every durable event, exactly as the
//     single-slot autosave did) so Continue is always current — "where I actually am" (owner's law).
//   * NINE MANUAL slots the captain banks deliberately (pre-haul, pre-bury, pre-aerobrake — the vault
//     moments) — never touched by the autosave, so a deliberate bank is safe from the rolling save.
//   * A tiny MANIFEST (this file's JSON) carries only the labels; the heavy vault JSON lives under a
//     per-slot key, so a rolling autosave rewrites one slot, not the whole book.
//
// THE MARS-PULL ROOT CAUSE this replaces (issue #310): the old system had exactly ONE key
// (spacesails.vault.v1). The rolling autosave, a fresh scenario start, and Continue ALL read/wrote
// that single key. So once ANY non-Continue path wrote it — an accidental "Rusty Roadstead — docked"
// scenario pick at the (unresponsive) boot picker, or any durable event after such a pick — the
// Uranus/The-Tilt autosave was overwritten and GONE. With one slot, Continue can only ever offer "the
// last thing written", which is why it kept waking at Mars. Separating the rolling autosave from
// deliberate scenario starts, and giving Continue its own always-current slot, is the fix.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>Which kind of slot: the one rolling autosave that follows the ship, or a manual bank.</summary>
public enum SaveSlotKind
{
    /// <summary>The single rolling autosave — rewritten on every durable event; Continue reads it.</summary>
    Autosave,

    /// <summary>A deliberately-banked slot; never overwritten by the autosave.</summary>
    Manual,
}

/// <summary>
/// One slot's label — everything the picker shows beside it. NOT the vault payload (that rides its own
/// per-slot key, byte-for-byte a <see cref="VaultSerializer"/> string). WHERE + WHEN + build stamp, plus
/// a monotonic <see cref="SavedRealTicks"/> so "Continue = newest" is an exact, tie-free comparison.
/// </summary>
public sealed record SaveSlotMeta
{
    public string Id { get; init; } = "";
    public SaveSlotKind Kind { get; init; } = SaveSlotKind.Manual;

    /// <summary>Human WHERE: the berth name when docked, or "adrift near &lt;body&gt;" when saved in flight
    /// (VaultResume's nearest-haven), or "unknown waters" when the vault carried no resume section.</summary>
    public string Where { get; init; } = "";

    /// <summary>True when the save happened clamped on (WHERE is a real berth); false = adrift/nearest.</summary>
    public bool WasDocked { get; init; }

    /// <summary>Sim clock (seconds) at save — the same value the resume ephemeris keys off.</summary>
    public double SavedSimTime { get; init; }

    /// <summary>Sim day (0-based), the game's own clock unit (see Map's "day N" convention).</summary>
    public int SimDay { get; init; }

    /// <summary>Real wall-clock label at save, e.g. "2026-07-18 21:40" — the "when in MY life" stamp.</summary>
    public string RealTimeLabel { get; init; } = "";

    /// <summary>Monotonic real-time tick at save (<c>DateTimeOffset.UtcNow.UtcTicks</c>). The tie-free key
    /// for "newest" — Continue resumes the slot with the greatest value (autosave, as it plays).</summary>
    public long SavedRealTicks { get; init; }

    /// <summary>The build stamp (#254) that wrote the slot — "am I even looking at the same build?".</summary>
    public string BuildStamp { get; init; } = "";

    /// <summary>Mirror of <see cref="Vault.Tampered"/> at save time, so the picker can mark it.</summary>
    public bool Tampered { get; init; }
}

/// <summary>Pure label helpers — the WHERE line, derived from a loaded <see cref="Vault"/>. Kept here (not
/// in the client) so the slot label is a tested truth, not a razor accident.</summary>
public static class SaveSlotLabels
{
    /// <summary>The WHERE string for a vault: a berth name when docked, "adrift near X" when the save
    /// happened in flight (nearest-haven resume), or "unknown waters" when there is no resume section.</summary>
    public static string Where(Vault? vault)
    {
        ResumeSection? resume = vault?.Resume;
        if (resume is null || string.IsNullOrWhiteSpace(resume.HavenName))
        {
            return "unknown waters";
        }

        return resume.WasDocked ? resume.HavenName : $"adrift near {resume.HavenName}";
    }

    /// <summary>The label an import screen shows, read STRAIGHT FROM the file's contents (never the
    /// filename — a renamed file still identifies itself). WHERE + sim day + the tampered mark, all from
    /// the vault payload. Real-time/build-stamp are left blank: they live in the slot manifest, not the
    /// portable file, so a file preview honestly shows only what the file itself carries.</summary>
    public static SaveSlotMeta PreviewMeta(Vault? vault) => new()
    {
        Kind = SaveSlotKind.Manual,
        Where = Where(vault),
        WasDocked = vault?.Resume?.WasDocked ?? false,
        SavedSimTime = vault?.SavedSimTime ?? 0,
        SimDay = (int)((vault?.SavedSimTime ?? 0) / 86400),
        Tampered = vault?.Tampered ?? false,
    };
}

/// <summary>
/// Filesystem-safe export filenames, spun from the SAME label machinery the slots show (#312). One
/// truth: the harbor in the filename IS the <see cref="SaveSlotLabels.Where"/> line, slugged. Ends the
/// browser's <c>spacesails-vault (5).json</c> roulette — the file names the place it was saved at.
/// Shape: <c>spacesails-&lt;place-slug&gt;-day&lt;N&gt;-&lt;yyyy-MM-dd-HHmm&gt;.json</c>,
/// e.g. <c>spacesails-the-tilt-day34-2026-07-18-1022.json</c>.
/// </summary>
public static class SaveFileNames
{
    /// <summary>Slug a WHERE line into a filesystem-safe token: lower-case ASCII letters/digits, every
    /// run of anything else collapsed to a single dash, ends trimmed. "adrift near The Tilt" →
    /// "adrift-near-the-tilt"; "The Tilt" → "the-tilt"; empty/exotic → "voyage".</summary>
    public static string Slug(string? where)
    {
        if (string.IsNullOrWhiteSpace(where))
        {
            return "voyage";
        }

        var sb = new System.Text.StringBuilder(where.Length);
        bool pendingDash = false;
        foreach (char c in where.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingDash && sb.Length > 0)
                {
                    sb.Append('-');
                }

                sb.Append(c);
                pendingDash = false;
            }
            else
            {
                pendingDash = true;
            }
        }

        return sb.Length == 0 ? "voyage" : sb.ToString();
    }

    /// <summary>The export filename for a slot's (or the live moment's) label — the one true filename
    /// builder. Reads WHERE, the sim day, and the real-time stamp reconstructed from the label's
    /// monotonic tick, so a per-slot export names the SLOT's state and a live export names THIS moment.</summary>
    public static string ForMeta(SaveSlotMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        DateTimeOffset realTime = meta.SavedRealTicks > 0
            ? new DateTimeOffset(meta.SavedRealTicks, TimeSpan.Zero)
            : DateTimeOffset.UtcNow;
        return For(meta.Where, meta.SimDay, realTime);
    }

    /// <summary>Build the filename from its parts: <c>spacesails-&lt;place&gt;-day&lt;N&gt;-&lt;stamp&gt;.json</c>.</summary>
    public static string For(string? where, int simDay, DateTimeOffset realTime)
    {
        string place = Slug(where);
        int day = Math.Max(0, simDay);
        string stamp = realTime.ToString("yyyy-MM-dd-HHmm", System.Globalization.CultureInfo.InvariantCulture);
        return $"spacesails-{place}-day{day}-{stamp}.json";
    }
}

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
