namespace SpaceSails.Core;

// The personal vault (#225): the things of personal value — relationships, balances, caches, maps,
// dice items, insurance, the ship's fit — persisted as versioned, field-tolerant JSON + checksum.
//
// DESIGN LAW (owner, 2026-07-17):
//  * NOT a physics snapshot. No orbit/trajectory/NPC positions ever. The resume state is a BERTH —
//    the last-docked haven (or the nearest dockable haven if the save happened in flight). Loading
//    reconstructs the ship DOCKED there at load-time ephemeris (zero relative velocity, clamped).
//  * Field-tolerant BOTH directions, forever: a reader ignores unknown fields and defaults missing
//    ones, and every section is INDEPENDENTLY optional and self-described — a partly-understood old
//    (or partly-corrupt) file still yields its understood parts. See <see cref="VaultSerializer"/>.
//  * The checksum is an honesty speed-bump, not DRM. A failed checksum does NOT refuse the load — it
//    loads anyway and marks the vault <see cref="Vault.Tampered"/> so the game can say so plainly.
//
// WHAT IS NOT SAVED (deliberately — dev-kindness, documented here as the contract):
//  * NPC positions and any hunter mid-chase. Heat IS saved, so a restart is never a heat-cleanse
//    exploit; but an in-progress pursuit simply resolves as ESCAPED on reload (the wolves lose the
//    scent when the world is rebuilt at a berth). Deliberate leniency, not an oversight.
//  * Autopilot plans / maneuver rehearsals — recomputed from the fresh docked state.
//  * Exact orbit/trajectory of the ship — replaced by the docked resume (see the resume section).

/// <summary>
/// The in-memory personal vault: a versioned envelope of independently-optional sections. Build one
/// from live game state, hand it to <see cref="VaultSerializer.Save"/>; load one back with
/// <see cref="VaultSerializer.Load"/>. Every section is nullable — absent means "this file did not
/// carry (or could not read) that section", and the game defaults it.
/// </summary>
public sealed class Vault
{
    /// <summary>The envelope schema version. Bumped only on a breaking shape change; readers of an
    /// older or newer version still harvest every section they understand.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    /// <summary>Sim time (seconds) at the moment of save. Interest, decay, and the resume ephemeris
    /// all key off this, so it rides in the checksummed payload.</summary>
    public double SavedSimTime { get; init; }

    // Every section is independently optional and self-described. A null section is simply "not
    // present in this file" — the game defaults it. Adding a section here is backward-compatible:
    // old files just carry a null for it, new files carry it, and old readers ignore what they lack.
    public PurseSection? Purse { get; init; }
    public ShipSection? Ship { get; init; }
    public CargoSection? Cargo { get; init; }
    public HeatSection? Heat { get; init; }
    public ContactsSection? Contacts { get; init; }
    public CachesSection? Caches { get; init; }
    public QuestsSection? Quests { get; init; }
    public InsuranceSection? Insurance { get; init; }
    public UpgradesSection? Upgrades { get; init; }
    public DiceItemsSection? DiceItems { get; init; }
    public ProgressSection? Progress { get; init; }
    public NerveSection? Nerve { get; init; }
    public OverheardSection? Overheard { get; init; }
    public ResumeSection? Resume { get; init; }

    /// <summary>Set true by <see cref="VaultSerializer.Load"/> when the stored checksum did not match
    /// the payload — the file was edited outside the game. The vault still loads (honesty speed-bump,
    /// not DRM); the game surfaces a permanent 📛 marker line in the Captain's ledger. Never persisted
    /// (it is a property of THIS load, not of the data). </summary>
    public bool Tampered { get; set; }

    /// <summary>Non-fatal notes gathered during a tolerant load (e.g. "contacts section unreadable —
    /// skipped"). Empty on a clean load. Never persisted.</summary>
    public IReadOnlyList<string> Warnings { get; set; } = [];
}

// ─── Sections. All plain data (records): self-described, tolerant, trivially round-tripped. ───

/// <summary>Carried coin (the purse). Banked coin lives on the contacts ledger, not here.</summary>
public sealed record PurseSection(long Credits);

/// <summary>The ship's durable fit that survives a berth-to-berth save: reaction mass in the tank
/// and the magazines. Position/velocity are NOT here — the resume section docks the ship fresh.</summary>
public sealed record ShipSection
{
    /// <summary>Reaction mass remaining, in pulses (the game's fuel unit). Double so a partial pulse
    /// survives; the mercy law keeps a restart from ever stranding you below a pump's reach.</summary>
    public double ReactionMassPulses { get; init; }
    public int SlugAmmo { get; init; }
    public int MissileAmmo { get; init; }

    /// <summary>#314 — the sentry roster's magazines, one entry per ship bot (K-77, R-3B), each 0..99
    /// rounds. Durable ship state like the other ammo above; a missing section (old file) defaults to a
    /// full-loaded roster on the client. Order matches <see cref="SentryBot.RosterUnits"/>.</summary>
    public IReadOnlyList<int> SentryMagazines { get; init; } = [];
}

/// <summary>The hold. Each line is a cargo class and its unit count; the hot (stolen-while-heated)
/// flags ride separately in <see cref="Vault.Cargo"/>'s hot list so laundering is independent.</summary>
public sealed record CargoSection(IReadOnlyList<CargoLine> Hold, IReadOnlyList<HotCargoLine> Hot)
{
    public CargoSection() : this([], []) { }
}

public sealed record CargoLine(string CargoClass, int Units);

/// <summary>A class of stolen-while-heated cargo (the <see cref="HotCargoLedger"/> stamp). When heat
/// fully cools the class launders; until then it is evidence the collectors can take.</summary>
public sealed record HotCargoLine(string CargoClass, int HotUnits);

/// <summary>The heat (wanted level) — a faithful mirror of <see cref="HeatState"/>. MUST be saved:
/// otherwise a server restart would cleanse heat, turning the vault into an exploit.
/// <see cref="RaisedAtSimTime"/> is the decay checkpoint and may be <c>double.NegativeInfinity</c>
/// (the "None" sentinel), so the serializer enables named floating-point literals.</summary>
public sealed record HeatSection(int Level, double RaisedAtSimTime);

// ── Relationships: the whole social network, mirrored from the ContactLedger. ──

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

/// <summary>The pirate-insurance policy (mirror of <see cref="PirateInsurance"/>). Tier stored as the
/// int value of <see cref="InsuranceTier"/> for forward-tolerance.</summary>
public sealed record InsuranceSection(int Tier, double PremiumPaidThroughSimTime);

/// <summary>The bought-and-kept ship upgrade levels. Separate from <see cref="ShipSection"/> because
/// they are permanent purchases, not consumables — and the owner's spec lists them as their own
/// section.</summary>
public sealed record UpgradesSection
{
    public int MassLevel { get; init; }
    public int SensorLevel { get; init; }
    public int HoldLevel { get; init; }
    public int TelescopeLevel { get; init; }
}

/// <summary>Persistent TTRPG dice items — the purchasable style modifiers ("boarding-nets jammer",
/// etc.) that tilt a roll. Each is a <see cref="DiceModifier"/> (label + value) plus a stable item
/// id so duplicates and stacking survive.</summary>
public sealed record DiceItemsSection(IReadOnlyList<DiceItemRecord> Items)
{
    public DiceItemsSection() : this([]) { }
}

public sealed record DiceItemRecord(string ItemId, string Label, int Value);

// ── Player progression flags (#292): onboarding state that gates the nav-screen greeting. ──

/// <summary>Durable player-progression flags. Today it carries just one bit — whether the
/// new-player tutorial has been engaged or completed (#292) — so a loaded save never re-greets a
/// captain who is no longer truly new. Its own section (independently optional, self-described) so
/// future progression flags can join without touching any other part of the vault.</summary>
public sealed record ProgressSection
{
    /// <summary>True once the captain has started or finished a tutorial lesson. Gates the fresh-start
    /// nav-screen promotion: an Earth start greets only a captain for whom this is still false.</summary>
    public bool TutorialPlayed { get; init; }

    /// <summary>#394 — true once this universe's crew turned an inbound rock aside from the Ringside
    /// Exchange (a full or grazing deflection). Persisted per-universe so Ringside's dedication plaque
    /// carries the appended line of gratitude forever after, on this thread and no other. Defaults false
    /// (a pre-#394 file simply lacks the field), so an unsaved port shows only its original dedication.</summary>
    public bool RingsideSaved { get; init; }
}

// ── The captain's nerve (#317, first slice of #226): the sanity gauge that debuts on the regolith. ──

/// <summary>The captain's nerve (#317 / #226): the sanity gauge that debuts on surface excursions.
/// Persisted so a captain who fled a moon shaking is still shaking after a reload — the ease-off is time
/// spent aboard, never the load itself. Its own independently-optional section: a pre-#317 file simply
/// lacks it and defaults to a full, calm gauge with an unseen monolith.</summary>
public sealed record NerveSection
{
    /// <summary>Current nerve, 0..<see cref="NerveModel.Max"/> (full = steady hands, 0 = nerves shot).
    /// Defaults full so a file that carries the section but not this field — or no section at all — loads
    /// calm rather than at the "nerves shot" floor a bare <c>default(double)</c> would imply.</summary>
    public double Nerve { get; init; } = NerveModel.Max;

    /// <summary>True once the captain has laid eyes on the monolith. Persisted so the big first-sight hit
    /// (<see cref="NerveModel.MonolithSightShock"/>) fires once in a life and never again on a revisit.</summary>
    public bool MonolithSeen { get; init; }
}

// ── Overheard at the bar (#308/#283 → owner 2026-07-18): the words the player paid a round to hear. ──

/// <summary>One durable line of bar intel the captain has been handed (#308 OpensUp intel, a barkeep
/// rumor firmed into a tip, a round's volunteered whisper). Owner's law: "the words the player paid a
/// round to hear may not hide" and "it autodisappears which is not convenient" — so every such line is
/// WRITTEN here, revisitable, rather than lived and lost in a transient toast. A <c>readonly record
/// struct</c> — flat, tolerant, trivially round-tripped.</summary>
public readonly record struct OverheardLine(string Text, double SimTime, string Source, string BarName);

/// <summary>The captain's "overheard at the bar" book (the #119 receipt/ledger idiom, for intel): a
/// capped, time-ordered log of the tips and rumors handed across the counter. Its own
/// independently-optional section — a pre-existing file simply lacks it and defaults to an empty book.</summary>
public sealed record OverheardSection
{
    /// <summary>The overheard lines, oldest first. Capped by the writer (see <c>OverheardLog</c>).</summary>
    public IReadOnlyList<OverheardLine> Lines { get; init; } = [];
}

// ── The resume berth (owner's law): where the pirate wakes, always docked. ──

/// <summary>The berth to resume at: the last-docked haven, or the nearest dockable haven computed at
/// save time if the save happened in flight. Loading rebuilds the ship DOCKED here at load-time
/// ephemeris — no stored orbit, ever. <see cref="WasDocked"/> records which case produced it (for the
/// picker's honesty and for tests).</summary>
public sealed record ResumeSection
{
    public string HavenId { get; init; } = "";
    public string HavenName { get; init; } = "";
    /// <summary>True if the save happened while docked/clamped at this berth; false if the ship was in
    /// flight and this is the nearest dockable haven chosen at save time.</summary>
    public bool WasDocked { get; init; }
}
