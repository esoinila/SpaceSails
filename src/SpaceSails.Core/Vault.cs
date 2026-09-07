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

    /// <summary>#417 · THE FINDER'S CASE — the graph Ilse Varga handed over and how far down it the captain
    /// has got. Its own independently optional section; a file written before the finder existed simply lacks
    /// it and loads with no case at all, which is the truth about a captain nobody ever asked to find
    /// anything.</summary>
    public FinderSection? Finder { get; init; }

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

    /// <summary>#615/#573 · Which rooms under which moons this captain has already turned over. Its own
    /// independently optional section; a pre-#615 file lacks it and wakes with an empty register, which is
    /// the honest truth about a facility nobody has walked yet.</summary>
    public TurnedOverSection? TurnedOver { get; init; }

    public KaamosSection? Kaamos { get; init; }
    public NebulaSection? Nebula { get; init; }
    public ResumeSection? Resume { get; init; }

    /// <summary>#948 · WHOSE MOMENT THIS IS, AND WHY THEY KEPT IT. The captain's (renameable) name, and — on
    /// a deliberately banked or exported save — the title and the note they wrote on it. Its own
    /// independently optional section: a pre-#948 file simply lacks it, loads with blanks, and the game falls
    /// back to the seeded captain and a derived title, which is the honest truth about a save nobody named.</summary>
    public LogbookSection? Logbook { get; init; }

    /// <summary>#563 slice 2 · WHAT THE CAPTAIN CHANGED ON THE GROUND — the huts forced, the lockers
    /// lifted and the effects read, keyed on (body, site, tile). Its own independently optional section; a
    /// file written before the treadmill's second slice simply lacks it and wakes with every hatch dogged
    /// again, which is what every save has done until now and what this section exists to stop.</summary>
    public GroundSection? Ground { get; init; }

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
