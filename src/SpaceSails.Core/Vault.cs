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

    /// <summary>#587 · The captain's FIELD BOOK — what they found on the ground. Its own independently
    /// optional section; a pre-#587 file simply lacks it and defaults to an empty book.</summary>
    public FieldNotesSection? FieldNotes { get; init; }

    /// <summary>#741 · The RED LINES the captain has drawn between entries in that book. Its own
    /// independently optional section; a pre-#741 file simply lacks it and comes back with a book full of
    /// loose ends, which is exactly what it was.</summary>
    public CaseThreadsSection? CaseThreads { get; init; }

    /// <summary>#836 · WHICH NAME THE CAPTAIN GAVE, AND WHERE. Its own independently optional section; a
    /// pre-#836 file simply lacks it and the wallet's every row reads <i>never shown</i> — which is the
    /// truth about a captain this build has no record of.</summary>
    public PapersShownSection? PapersShown { get; init; }

    /// <summary>#973 L1 · THE FILING LINE'S MARKS ON THE BOOK — which pages of the Captain's ledger this
    /// captain does not remember writing, which of them have been read at already, and the hidden originals of
    /// the ones that came back wrong. Its own independently optional section; a pre-#973 file simply lacks it
    /// and loads with nothing marked, which is the truth about a captain nobody ever filed a claim for.</summary>
    public FilingSection? Filing { get; init; }

    /// <summary>#973 L5a · THE OLD CREW — which four shipmates this thread cast, the history rolled between
    /// them, and where each ended up working. Its own independently optional section; a pre-#973 file simply
    /// lacks it and the crew are seeded from the thread id on first load, which is what a deterministic
    /// seeding is for.</summary>
    public OldCrewSection? OldCrew { get; init; }

    /// <summary>#973 L5b · THE WALK-INS THE SPREAD HAS FOUND OUT — which of the two women's jobs the captain
    /// has laid beside a money-tagged slip and read the same hand off. Its own independently optional section;
    /// a pre-#973-L5b file simply lacks it and loads with every setup card quiet, which is the truth about a
    /// captain who never worked it out — and exactly what the card says before the SPREAD fires.</summary>
    public WalkInSection? WalkIn { get; init; }

    /// <summary>#973 L5a · THE CAPTAIN'S CROSSINGS (the-captains-character.md §3). Its own independently
    /// optional section; a pre-#973 file lacks it and loads with an empty book, which is the honest state of
    /// a captain nobody ever asked about his face.</summary>
    public CrossingsSection? Crossings { get; init; }

    /// <summary>#973 · THE VOID'S WEATHER — how many times this thread has heard each of the eight lines
    /// about the walking insurance men, and, per station, how many visits it has had and which of them a line
    /// was last in the air on. Its own independently optional section; a file written before the weather
    /// simply lacks it and wakes with all eight lines unheard, which is the truth about a room nobody has
    /// stood in yet.</summary>
    public InsuranceWeatherSection? InsuranceWeather { get; init; }

    /// <summary>#973 · THE HELD MEMORIES — the sheets in the black book that are not documents. Its own
    /// independently optional section; a pre-#973 file lacks it and the seeded pages come back on first
    /// load.</summary>
    public HeldMemoriesSection? HeldMemories { get; init; }

    /// <summary>#590 · The authority cards the captain is carrying. Its own independently optional section;
    /// a pre-#590 file simply lacks it and defaults to an empty wallet.</summary>
    public AuthoritiesSection? Authorities { get; init; }

    /// <summary>#603 · Everything the captain is carrying on foot. Supersedes <see cref="Authorities"/>,
    /// which is still read on load so an older save's cards are not lost — a captain who earned a card
    /// eleven floors down does not lose it to a refactor.</summary>
    public SatchelSection? Satchel { get; init; }

    /// <summary>#1016 · Which sheets this captain has already dug out at a table. Its own independently
    /// optional section; a pre-#1016 file simply lacks it and wakes with an empty register, which is the
    /// truth about a case nobody has worked yet — and about every save written while the register still
    /// lived on the excursion and died with the shuttle.</summary>
    public WorkedUpSection? WorkedUp { get; init; }
    public KaamosSection? Kaamos { get; init; }
    public NebulaSection? Nebula { get; init; }
    public ResumeSection? Resume { get; init; }

    /// <summary>#948 · WHOSE MOMENT THIS IS, AND WHY THEY KEPT IT. The captain's (renameable) name, and — on
    /// a deliberately banked or exported save — the title and the note they wrote on it. Its own
    /// independently optional section: a pre-#948 file simply lacks it, loads with blanks, and the game falls
    /// back to the seeded captain and a derived title, which is the honest truth about a save nobody named.</summary>
    public LogbookSection? Logbook { get; init; }

    /// <summary>#638 · THE COUNTDOWN THE VOID IS RUNNING, if one is running at all.
    ///
    /// <para><b>Written only while the clock is live</b> — the client hands over <c>null</c> whenever nothing
    /// is counting down, which is every voyage that has never gone dry with nowhere to go. That is deliberate
    /// and it is what makes the save-compat proof cheap: the section is simply absent from the file, so a
    /// vault written before #638 re-loaded and re-saved by this build comes back BYTE FOR BYTE — same keys,
    /// same order, same checksum (the digest is taken over the payload, so a written-but-idle section would
    /// change the hash of every save ever made and hang the 📛 tampered marker on an honest voyage). The
    /// #1057 precedent, one level up: there it was a nullable FIELD carrying
    /// <c>JsonIgnore(WhenWritingNull)</c>; here the whole section is the thing that stays unwritten.
    /// <c>ALegacyVaultRoundTripsByteForByteAcrossTheVoid</c> in the Core suite holds that line.</para></summary>
    public VoidSection? Void { get; init; }

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

    /// <summary>#409 — the body ids where this thread has FOUND one of Dr. Vantar's secret labs (revealed its
    /// hidden door). Persisted per-universe (the vault/thread idiom) so a revisit to a known body shows the
    /// door already revealed — you remember where it is. Defaults empty (a pre-#409 file simply lacks the
    /// field), a lossless round-trip.</summary>
    public IReadOnlyList<string> SecretLabsFound { get; init; } = [];

    /// <summary>#440 — true once this captain has been shown <see cref="GroundLesson"/>, the one card that
    /// explains the surface before their first excursion. Persisted so it greets the truly new and NEVER
    /// again (#292's ruling), even across reloads. Defaults false, so a pre-#440 file — a captain who has
    /// already walked a dozen moons — gets it once on their next trip down and then never more.</summary>
    public bool GroundLessonSeen { get; init; }

    /// <summary>#563 — true once this captain has been shown <see cref="GroundGrows"/>, the card that fires
    /// the first time forcing something open makes the map itself bigger. Same law as
    /// <see cref="GroundLessonSeen"/>: it greets the truly new and never again, because after one showing
    /// the toast says everything a card would. Defaults false, so a captain who has already forced a door
    /// before this existed gets the explanation once on their next one.</summary>
    public bool GroundGrewSeen { get; init; }

    /// <summary>#562 — true once this captain has been shown <see cref="TubeRearm"/>, the card that fires
    /// the first time the ship racks a sentry magazine in her down-tube. Same law again: it teaches the
    /// shape of an excursion (the tube is the one place your sentries get fed, so every trip is a loop with
    /// one anchor) and then never speaks again, because the receipt line says the rest.</summary>
    public bool TubeRearmSeen { get; init; }

    /// <summary>#573 — true once this captain has been shown <see cref="AirCard"/>, the card that fires the
    /// first time their tank passes the low mark on a surface. Same law as its siblings: it teaches the
    /// clock once and then the pulse line carries it.</summary>
    public bool AirCardSeen { get; init; }

    /// <summary>#701 — the <see cref="OddBooks.Entry.Id"/>s this thread has already filed a gist for.
    /// Persisted per-universe (the same idiom as <see cref="SecretLabsFound"/>) because the one-shot law is
    /// about KNOWLEDGE: looking at a book again is free and always will be, but the casebook learns a thing
    /// once. Defaults empty — a pre-#701 file simply lacks the field, and a captain who has read a shelf
    /// they cannot remember reading files it again, which is the harmless direction to be wrong in.</summary>
    public IReadOnlyList<string> OddBooksRead { get; init; } = [];

    /// <summary>
    /// #1066 · CONSECUTIVE WORKING STOPS SINCE THE CREW WERE LAST ASHORE — the whole of the shore-leave
    /// mechanic, as one integer. A clamp at a great port (<see cref="ArrivalTube.Tier.GreatPort"/>) puts it
    /// back to nothing; every other berth adds one, and <see cref="CrewTemp.ShoreLeavePromisesBroken"/>
    /// turns the tally into the broken promise the crew's report reads.
    ///
    /// <para><b>Written only when somebody is counting</b>, which is the #1057/#1072 pattern and not a
    /// stylistic choice: the checksum is taken over the payload, so an extra
    /// <c>"workingStopsSinceShoreLeave": 0</c> on every save would change the digest of every vault ever
    /// written and hang the 📛 tampered marker on an honest voyage. Null means "this file never counted" —
    /// the truth about every save written before shore leave was a number, and about every ship that has
    /// just walked off a great port's gangway.
    /// <c>ShoreLeaveIsALedgerLineTests.ALegacyVaultRoundTripsByteForByteAcrossTheShoreLeaveLine</c> holds
    /// that line against a real pre-#1066 file.</para>
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? WorkingStopsSinceShoreLeave { get; init; }
    /// <summary>#677 — THE DISCLOSURE CLOCK'S REGISTER: the grounds whose halls this thread has been past the
    /// seam of, and the world-side window each was opened in (<see cref="DisclosureClock"/>).
    ///
    /// <para>Persisted per-universe, the same idiom as <see cref="SecretLabsFound"/> and for a harder version
    /// of its reason. That one remembers WHERE a door is; this one remembers WHEN a ground was opened, and a
    /// clock that forgot across a reload would not be a clock — every threshold written against it would
    /// silently reset to zero the moment a captain closed the tab, and nothing on screen would ever say so.
    /// The first crossing is the one kept (<see cref="DisclosureClock.Note"/>), so a revisit can never move
    /// it.</para>
    ///
    /// <para>Null until somebody has crossed a seam — the #1057/#1072/#1066 pattern, and here for their
    /// exact reason: the checksum is taken over the payload, so an eager <c>"hallsOpened": []</c> on every
    /// save would change the digest of every vault ever written and hang the 📛 tampered marker on honest
    /// voyages (the #1078 byte guard caught precisely that when this section met the shore-leave line).
    /// A pre-#677 file simply lacks the field, and a captain who walked a gallery before this existed has
    /// their clock start on their next crossing, which is the harmless direction to be wrong in.</para></summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<HallOpeningRecord>? HallsOpened { get; init; }
}

/// <summary>#677 — one row of <see cref="ProgressSection.HallsOpened"/>: a ground, and the world-side window
/// its halls were first entered in.
///
/// <para>A wire record of its own rather than <see cref="DisclosureClock.Opening"/> serialized directly, for
/// the reason <see cref="DiceItemRecord"/> is one: this file is a FORMAT, and a format that is spelled as a
/// domain type moves every time the domain type is renamed — with old saves silently losing the field, which
/// is the one failure mode this serializer's two promises exist to prevent.</para></summary>
public sealed record HallOpeningRecord(string BodyId, long Window);

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

// ── #638 · The void's countdown, which is a clock and therefore has to survive a save. ──

/// <summary>
/// #638 · The adrift countdown, as it stands. Present in the file ONLY while a clock is actually running —
/// see <see cref="Vault.Void"/> for why that is the whole save-compat story.
///
/// <para>Two numbers and nothing else, because everything else about the void is derived: the day the run
/// began, and the last day the captain was told anything on. <see cref="VoidRule"/> turns them back into a
/// countdown, and a resumed voyage that slept through a telling gets it on the next tick
/// (<see cref="VoidRule.TellingsBetween"/>), because a beat you can miss by loading is not a beat.</para>
/// </summary>
public sealed record VoidSection
{
    /// <summary>The whole sim-day (<see cref="VoidRule.DayIndex"/>) the ship was declared ADRIFT on.
    /// Defaults to <see cref="VoidRule.ClockNotRunning"/> so a file that carries the section but not this
    /// field reads as "no clock", which is the safe direction: the worst a wrong default can do here is kill
    /// a captain who was fine.</summary>
    public long DeclaredDay { get; init; } = VoidRule.ClockNotRunning;

    /// <summary>The last day-count of THIS run the captain has already been told about. Defaults to
    /// <see cref="VoidRule.NothingToldYet"/> — below day 0, because day 0 is itself a telling.</summary>
    public int LastToldDay { get; init; } = VoidRule.NothingToldYet;
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

/// <summary>#587 · The captain's field book: everything found out on a surface, kept so it can be re-read.
/// Owner: <i>"we should maybe collect the tips to ledger if we don't show them again?"</i> — the same ruling
/// he made for bar intel in #347, pointed at the ground. Its own independently-optional section.</summary>
public sealed record FieldNotesSection
{
    /// <summary>The notes, oldest first. Capped by the writer (see <c>FieldNotes</c>).</summary>
    public IReadOnlyList<FieldNote> Notes { get; init; } = [];
}

/// <summary>#741 · THE RED LINES. Owner: <i>"I dream of drawing those conspiracy board connecting red
/// lines… I guess it could be a red pen only used to connect the things."</i>
///
/// <para>Stored as opaque pair strings (<c>CaseThreads.Thread.Stored</c>), which is the same shape
/// <see cref="SatchelSection"/> uses and for the same reason: the file carries the FACT — these two entries
/// were connected by a hand — and never the words. Both ends are
/// <c>CaseThreads.IdentityOf</c> handles derived from the note's own place, moment and text, so a book that
/// round-trips brings its lines back with it. A pair this build cannot parse is dropped on load rather than
/// thrown over.</para></summary>
public sealed record CaseThreadsSection
{
    /// <summary>The threads, in the order they were drawn. Capped by the writer (see <c>CaseThreads</c>).</summary>
    public IReadOnlyList<string> Threads { get; init; } = [];
}

/// <summary>#836 · THE CAPTAIN'S OWN PAPER TRAIL: which identity was shown, where, on what floor, and how it
/// went — one row per guard's read.
///
/// <para>Stored as opaque row strings (<c>WalletChoice.Shown.Stored</c>), the same shape
/// <see cref="SatchelSection"/> and <see cref="CaseThreadsSection"/> use and for the same reason: the file
/// carries the FACT and never the words, because every sentence built out of these rows is a seeded property
/// of the world. A row this build cannot parse is dropped on load rather than thrown over.</para>
///
/// <para>It is durable because the wallet's hint is: <i>worked here, twice</i> is worth nothing if it forgets
/// between excursions, and the paper you handed a man last night is exactly what a gumshoe would remember
/// about him.</para></summary>
public sealed record PapersShownSection
{
    /// <summary>The reads, oldest first. Capped by the writer (see <c>WalletChoice.BookKeeps</c>).</summary>
    public IReadOnlyList<string> Shown { get; init; } = [];
}

/// <summary>#590 · WHAT THE CAPTAIN IS CARRYING THAT OPENS SOMETHING. Owner: <i>"could there be like a
/// keycode etc that allows us access to the lab"</i>.
///
/// <para>Stored as the flat list of card ids (<c>UndergroundComplex.AuthorityCard.Id</c>) and nothing else,
/// so the save carries the FACT and the prose is rebuilt from the generator at read time — the same shape
/// KAAMOS uses, and for the same reason: a card's title is a seeded property of the world, and a file that
/// carried the words would go stale the day the words changed. Ids the current build cannot parse are
/// dropped on load rather than thrown over.</para>
///
/// <para>These are durable on purpose. A card is found eleven floors under a moon and must still be in the
/// captain's pocket a month and a world later, or it is not a possession — it is a mood.</para></summary>
/// <summary>#603 · THE POCKET. Stored as opaque item strings (<c>Satchel.Item.Stored</c>) so the save carries
/// the FACT and never the words — every label, every clue's certainty and every card's title is a seeded
/// property of the world, rebuilt at read time. A file that carried the prose would go stale the day the
/// prose changed, which is the shape of half the bugs in this repository's history.
///
/// <para>Supersedes <see cref="AuthoritiesSection"/>, which is still READ on load so an older save's cards
/// migrate in rather than being lost.</para></summary>
public sealed record SatchelSection
{
    /// <summary>Items, in whatever order they were written.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];
}

/// <summary>
/// #1016 · <b>THE CASE'S OWN REGISTER: which sheets have been dug out at a table.</b>
///
/// <para>Owner, 2026-08-30, on finding "Work the case" dead at a bar top in a docked haven: <i>"Maybe it
/// might be good idea to refactor the working the case etc table options to not be tied to any location?
/// Kind of clean separation from the arriving random encounters that are more place tied events."</i></para>
///
/// <para>It lived on the <c>SurfaceExcursion</c> until that ruling, which made it a fact about a WALK: dig a
/// pay sheet on B1, fly home, and the book still held the entry while the register that knew the sheet was
/// worked had been thrown away with the shuttle. Worse, a captain sitting in a station bar had no excursion
/// at all, so every reader of it answered "no" and every writer of it dropped the write on the floor. A
/// register that belongs to the case belongs to the captain, and a captain's things ride the vault.</para>
///
/// <para>Stored as the opaque keys the register is keyed on (<c>Kind:Id</c>, built by the one key builder,
/// <c>Map.WrittenUpKey</c>) — the same shape <see cref="SatchelSection"/> and <see cref="FilingSection"/> use
/// and for the same reason: the file carries the FACT (this sheet is in the book in the captain's own hand)
/// and never a sentence, because every word the book prints about a sheet is rebuilt from the paper at read
/// time. A key this build cannot recognise costs nothing to carry, so nothing is dropped.</para></summary>
public sealed record WorkedUpSection
{
    /// <summary>One key per sheet already dug out. A set on the page, so the order this list happens to be
    /// in is not a fact about anything.</summary>
    public IReadOnlyList<string> Sheets { get; init; } = [];
}

/// <summary>#973 L1 · The filing line's marks. Stored as opaque row strings
/// (<c>FilingLine.Page.Stored</c>) — the same shape <see cref="SatchelSection"/> and
/// <see cref="CaseThreadsSection"/> use and for the same reason: the file carries the FACT (this row is grey;
/// this one came back wrong and really said <i>this</i>) and never the sentences, which are rebuilt from the
/// live ledger every render. A row this build cannot parse is dropped rather than thrown over.</summary>
public sealed record FilingSection
{
    /// <summary>One row per marked ledger entry, in ledger order.</summary>
    public IReadOnlyList<string> Pages { get; init; } = [];
}

/// <summary>#973 L5b · What the SPREAD has found out about a walk-in. Job ids and nothing else: the file
/// carries the FACT (this job is a setup and the captain knows it) and never the grey line, which
/// <see cref="WalkIn.SetupCardLine"/> rebuilds every render — the same shape <see cref="FilingSection"/> and
/// <see cref="SatchelSection"/> use, for the same reason. A knowing is never un-known, so this list only ever
/// grows.</summary>
public sealed record WalkInSection
{
    /// <summary>One job id per walk-in the SPREAD has read the same hand off. A set on the page, so the order
    /// this list happens to be in is not a fact about anything.</summary>
    public IReadOnlyList<string> SetupsRevealed { get; init; } = [];
}

/// <summary>#973 L5a · THE OLD CREW's seeding. Stored as opaque row strings so the file carries the FACT
/// (these four, bound like this, posted there) and never the words — every sentence the black book prints
/// about a shipmate is rebuilt from the pool at read time. A row this build cannot parse is dropped rather
/// than thrown over; a seeding that comes back short is re-rolled from the thread id.</summary>
public sealed record OldCrewSection
{
    /// <summary>One row per seeded shipmate, in pool order.</summary>
    public IReadOnlyList<string> Shipmates { get; init; } = [];

    /// <summary>#973 L5a · Which of them THIS captain has already explained his face to. Saved rather than
    /// recomputed for the reason the filing line's marks are: a scene is a fact about a life, and a reload
    /// that replayed it would let a player re-answer a question they have already answered — and write a
    /// second crossing for it. Emptied by a rebirth, because the next face has its own explaining to do.</summary>
    public IReadOnlyList<string> Explained { get; init; } = [];
}

/// <summary>#973 L5a · The captain's crossings, oldest first. Opaque rows (<c>CaptainCrossings.Crossing
/// .Stored</c>) for the reason every other book here uses them: the row carries which line, which situation
/// and who saw, and the sentence the desk prints is rebuilt from those three.</summary>
public sealed record CrossingsSection
{
    /// <summary>The crossings, oldest first.</summary>
    public IReadOnlyList<string> Crossings { get; init; } = [];
}

/// <summary>#973 · The held-memory sheets (<c>HeldMemory.Sheet.Stored</c>). These rows DO carry their text,
/// unlike most of the opaque books here, and deliberately: a held memory is authored prose or a person's own
/// words, not a sentence assembled out of a seeded world, and a sheet whose text was rebuilt could come back
/// saying something the captain never read.</summary>
public sealed record HeldMemoriesSection
{
    /// <summary>The sheets, in the order they entered the book.</summary>
    public IReadOnlyList<string> Sheets { get; init; } = [];
}

/// <summary>#973 · The void's weather (<see cref="InsuranceWeather"/>). Opaque pipe-separated rows, the house
/// idiom: the file carries how OFTEN a line was heard and never the sentence, so an edited save can never make
/// the room say something nobody wrote. A row this build cannot parse is dropped rather than thrown over.</summary>
public sealed record InsuranceWeatherSection
{
    /// <summary>One row per line the captain has heard at all: <c>lineId|times</c>.</summary>
    public IReadOnlyList<string> Heard { get; init; } = [];

    /// <summary>One row per station the captain has drunk at: <c>stationId|visits|lastSaidAtVisit</c>, where
    /// the last field is −1 for a station the weather has never blown through.</summary>
    public IReadOnlyList<string> Stations { get; init; } = [];
}

public sealed record AuthoritiesSection
{
    /// <summary>Card ids, in whatever order they were written.</summary>
    public IReadOnlyList<string> Cards { get; init; } = [];
}

// ── PROJEKTI KAAMOS (#411): the ice-moon mystery, assembled per-thread. ──

/// <summary>The KAAMOS lore-fragments this universe's captain has assembled (#411) — stored as the flat
/// list of fragment ids, the minimal shape (<see cref="KaamosProgress.AssembledIds"/>), so the reach
/// logic and the text both rebuild from the pool at load rather than the save carrying prose. Its own
/// independently-optional section: a pre-#411 file simply lacks it and defaults to nothing assembled (a
/// captain who has heard nothing of the polar night). Unknown-to-the-pool ids are dropped on load, so
/// the round-trip is always re-readable against the pool that wrote it.</summary>
public sealed record KaamosSection
{
    /// <summary>The assembled fragment ids (see <see cref="KaamosLore.Fragments"/>). Order is not
    /// meaningful — assembly is a set — but the writer emits canonical order for a stable file.</summary>
    public IReadOnlyList<string> AssembledFragmentIds { get; init; } = [];

    /// <summary>#635 · Whether this captain has had a filing for the sealed ice-moon berth returned by the
    /// board — the arc's front door, and not a fragment: it credits nothing toward the gate. Its own flag
    /// rather than a pool id, because the pool is the thing the reach logic counts and a phantom id in it
    /// would be exactly the drift <see cref="KaamosProgress.Assemble"/> refuses. A save written before
    /// #635 simply lacks the property and defaults to false.</summary>
    public bool BerthFilingBounced { get; init; }
}

// ── NEBULA MUTUAL (#422): the second story arc, the truth behind the player's own deaths, per-thread. ──

/// <summary>The NEBULA lore-fragments this universe's captain has assembled about what their resurrections
/// really are (#422) — stored as the flat list of fragment ids (<see cref="NebulaProgress.AssembledIds"/>)
/// plus the one-time convergence-reveal bit, so the fine-print logic and the text both rebuild from the pool
/// at load rather than the save carrying prose. Its own independently-optional section: a pre-#422 file
/// simply lacks it and defaults to nothing assembled and the convergence unseen (a captain who has died and
/// come back and never once wondered why). Unknown-to-the-pool ids are dropped on load, so the round-trip is
/// always re-readable against the pool that wrote it.</summary>
public sealed record NebulaSection
{
    /// <summary>The assembled fragment ids (see <see cref="NebulaLore.Fragments"/>). Order is not meaningful —
    /// assembly is a set — but the writer emits canonical order for a stable file.</summary>
    public IReadOnlyList<string> AssembledFragmentIds { get; init; } = [];

    /// <summary>True once the one-time CONVERGENCE reveal has fired for this thread
    /// (<see cref="ArcConvergence"/>). Persisted so the biggest #391 throw plays once in a universe and never
    /// re-fires on a reload — the analog of <see cref="NerveSection.MonolithSeen"/>. Defaults false (a pre-#422
    /// file, or one saved before the two arcs met).</summary>
    public bool ConvergenceSeen { get; init; }
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

// ── #948 · The logbook page: the human half of a save. ──

/// <summary>
/// #948 · THE PAGE THE CAPTAIN WROTE. Owner, at the logbook with eight identical berths open: <i>"Let's have
/// an option to change the name of our avatar. It solves the issue of forgetting who you are amongst the
/// autogenerated names"</i>, and <i>"I would like to save this point with a comment field and a captain's
/// name of my whim."</i>
///
/// <para>Three strings, and every one of them is the player's, not the generator's: the CAPTAIN'S NAME (the
/// seeded roster name until they change it — see <see cref="Captains.CleanName"/>), the TITLE they gave this
/// moment, and the NOTE about what they were doing when they kept it. Rides IN the vault payload, not merely
/// beside it in the slot manifest, so an exported .json carries the name and the note to whatever machine
/// opens it next (<see cref="VaultSerializer.StampLogbook"/> writes it onto an already-banked payload without
/// disturbing a single other byte of the save).</para>
/// </summary>
public sealed record LogbookSection
{
    /// <summary>The captain's name at the moment of saving — the renamed one if the player renamed them,
    /// else the seeded roster name. Blank on a pre-#948 file; the game then re-derives the seeded name.</summary>
    public string CaptainName { get; init; } = "";

    /// <summary>The captain's-whim title for THIS moment ("the run before the Kaamos dive"). Blank means
    /// nobody titled it, and the row falls back to <see cref="SaveSlotLabels.DefaultTitle"/>.</summary>
    public string Title { get; init; } = "";

    /// <summary>Free text: what they were doing, what they meant to do next, who they must not meet again.
    /// Blank means no note was written. Never parsed by the game — it is for the human alone.</summary>
    public string Note { get; init; } = "";
}
