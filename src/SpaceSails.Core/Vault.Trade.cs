namespace SpaceSails.Core;

// ─── VAULT SECTIONS: WHO THE CAPTAIN DEALS WITH (#225) ───
//
// Contacts and the credit ledger under each of them, the caches buried and what is in them, the quests
// taken and the obligations they leave behind. The relationship half of the vault.
// 
// Split out of Vault.cs under #251 — whole record types moved, nothing inside one re-ordered.

public sealed record ContactsSection(IReadOnlyList<ContactRecord> Contacts)
{
    public ContactsSection() : this([]) { }
}

/// <summary>One contact's history and signed bank balance — a faithful mirror of
/// <see cref="ContactHistory"/> so <see cref="VaultMapper"/> can round-trip it without loss.</summary>
public sealed record ContactRecord
{
    public string ContactId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public int MissionsCompleted { get; init; }
    public int TotalPaidCredits { get; init; }
    public double LastCompletedSimTime { get; init; }
    public bool Hostile { get; init; }
    /// <summary>Signed running balance (+ they hold our coin, − we owe them). Invariant: == Σ txn.</summary>
    public long CreditBalance { get; init; }
    /// <summary>#247 — goodwill stood at the bar (a round for the room). Non-transactional; defaults 0.</summary>
    public int Goodwill { get; init; }
    /// <summary>#306 — tells this contact now knows about us (slipped over a drink). Defaults empty.</summary>
    public IReadOnlyList<string> KnownTells { get; init; } = [];
    /// <summary>#5 SundayMorningWind — the favourite drink id we've learned for this contact. Empty until known.</summary>
    public string KnownFavorite { get; init; } = "";
    /// <summary>#715 — illegal heat this entity holds against us (an outfit we crossed). Defaults 0, so a
    /// vault written before the meter existed loads as a captain nobody remembers.</summary>
    public int HeatOwed { get; init; }
    /// <summary>#715 — when that heat was last charged or cooled. Only meaningful while
    /// <see cref="HeatOwed"/> is above zero, which is why a defaulted 0 costs an old file nothing.</summary>
    public double HeatStampSimTime { get; init; }
    /// <summary>#973 L5a — this contact knew the captain's old face. Defaults false, so a vault written
    /// before the old crew existed loads a cast of people who never served with him.</summary>
    public bool KnewTheOldFace { get; init; }
    /// <summary>#973 L5a — the captain told this contact the reactor-seal story. <i>The book marks the
    /// lie.</i> Defaults false.</summary>
    public bool WasLiedTo { get; init; }
    public IReadOnlyList<CreditTxnRecord> Transactions { get; init; } = [];
}

/// <summary>A single line of a contact's passbook. <see cref="Kind"/> is stored as the int value of
/// <see cref="CreditKind"/> so an unknown future kind survives as a number rather than failing.</summary>
public sealed record CreditTxnRecord(int Kind, long Amount, double SimTime, string Note);

// ── The hoard: buried caches and the maps we hold (ours and rivals'). ──

public sealed record CachesSection
{
    /// <summary>The mint counter, preserved so freshly-buried caches after a load can't collide with
    /// loaded ids.</summary>
    public int NextMintIndex { get; init; }

    /// <summary>The discovery watch's bookmark — the last whole day this hoard was rolled through
    /// (<see cref="CacheLedger.LastCheckedPeriod"/>). Defaults to <see cref="CacheLedger.WatchNotStarted"/>
    /// so a vault written before this field existed loads as "no watch yet" and the client re-seeds it at
    /// the load clock; without the default a legacy save would read period 0 and resolve every day since
    /// the epoch in one go — a hoard massacre on load.</summary>
    public long LastCheckedPeriod { get; init; } = CacheLedger.WatchNotStarted;

    public IReadOnlyList<CacheRecord> Caches { get; init; } = [];
}

/// <summary>A buried chest (mirror of <see cref="TreasureCache"/>). A rival's cache we merely hold a
/// map to has <see cref="PlayerOwned"/> = false but is still ours to remember.</summary>
public sealed record CacheRecord
{
    public string Id { get; init; } = "";
    public string BodyId { get; init; } = "";
    public string LandmarkName { get; init; } = "";
    public string Bearing { get; init; } = "";
    public int Paces { get; init; }
    public int Coin { get; init; }
    public IReadOnlyList<CacheCargoRecord> Cargo { get; init; } = [];
    public double BuriedSimTime { get; init; }
    public string Owner { get; init; } = "";
    public bool PlayerOwned { get; init; }

    /// <summary>The stash's standing Reever presence (#295), 0..3. Defaults to 0 so a pre-#295 vault
    /// file (no field) loads as an unhaunted chest — a lossless round-trip.</summary>
    public int ReeverLevel { get; init; }

    /// <summary>The REAL dug spot (beach-comber kit, owner 2026-07-18 "bury anywhere" / playtest bug #5).
    /// Nullable so an older vault file (no fields) loads with both null — the client then falls back to the
    /// hash-scatter position (<c>MoonSurface.CachePosition</c>), a lossless round-trip for every legacy
    /// cache. A free-form bury persists the actual coords, so the ✗ reloads where the shovel dug.</summary>
    public double? DigX { get; init; }
    public double? DigY { get; init; }

    /// <summary>#650 · WHICH GROUND the chest is under — the landing-site ordinal (<see cref="LandingSite.Index"/>)
    /// the shovel went in at. Null means body-wide, which is what every chest saved before this field existed is.
    ///
    /// <para><b>Written only when it has a value</b>, unlike <see cref="DigX"/>/<see cref="DigY"/> which have
    /// always emitted explicit nulls. That is the whole point: a vault written before #650, re-loaded and
    /// re-saved by this build, must come back BYTE-FOR-BYTE — same keys, same order, same checksum (the digest
    /// is taken over the payload, so an extra <c>"siteIndex": null</c> would change the file's hash and mark
    /// every legacy save as edited). <c>CachesRoundTripByteForByte</c> in the Core suite holds that line.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? SiteIndex { get; init; }

    /// <summary>#455 · Whether the SHOVEL went in (true) or the chest was left lying where it was dropped
    /// (false). Null = the chest never recorded it, which is every hoard saved before #455 and every rumour
    /// map; those keep the exact discovery odds they were buried under.
    ///
    /// <para>Omitted when null for the same reason <see cref="SiteIndex"/> is, and it is worth restating
    /// because #650's guard caught it at byte 564 of a real legacy file: the checksum is taken over the
    /// PAYLOAD, so one extra <c>"buried": null</c> per chest changes the digest of every hoard ever saved
    /// and the game opens each one flying the 📛 tampered flag on an honest captain's voyage.
    /// <c>ALegacyVaultRoundTripsByteForByte</c> (#650) and <c>TheDeepBuriedChestSurvivesTheVaultByteForByte</c>
    /// (#455) both hold that line.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? Buried { get; init; }

    /// <summary>#455 · How far from the landing pad the chest was carried, in deck units, measured when it
    /// went down. The carried courage that the return-trip roll pays out on. Written only when it has a
    /// value — see <see cref="Buried"/> for why that matters to the byte.</summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double? PadDistance { get; init; }
}

public sealed record CacheCargoRecord(string CargoClass, int Units, bool Hot);

// ── Quests in hand + the favor-debt (obligation) queue. ──

public sealed record QuestsSection
{
    public IReadOnlyList<QuestRecord> Quests { get; init; } = [];
    public IReadOnlyList<ObligationRecord> Obligations { get; init; } = [];
}

/// <summary>One contract in hand. Kept deliberately loose (id + kind + a free-form state bag) so the
/// quest system can evolve its own shapes without ever breaking an old save — the reader keeps what
/// it understands. Numbers/targets a quest needs to resume live in <see cref="Fields"/>.</summary>
public sealed record QuestRecord
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Status { get; init; } = "";
    public string Title { get; init; } = "";
    public string Detail { get; init; } = "";
    public string GiverContactId { get; init; } = "";
    public int RewardCredits { get; init; }
    public double AcceptedSimTime { get; init; }
    /// <summary>Free-form extra state (target ids, paces walked, cache id, stage index…). String→string
    /// so any quest kind can stash what it needs and a reader that doesn't know a key just carries it.</summary>
    public IReadOnlyDictionary<string, string> Fields { get; init; } = new Dictionary<string, string>();
}

/// <summary>A favor-debt owed to a contact (mirror of <see cref="FavorObligation"/>): "you owe Madam
/// Coil one quiet delivery."</summary>
public sealed record ObligationRecord(
    string ContactId, string DisplayName, long PrincipalCredits, double IncurredSimTime, string VoiceLine);

// ── Insurance, upgrades, dice items. ──
