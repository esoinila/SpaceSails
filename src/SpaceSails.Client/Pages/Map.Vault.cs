using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Vault — the personal vault: build, write, peek, resume, import and export, and the
// ApplyVault machinery that rehydrates a saved run. Split from Map.razor per #251.
public partial class Map
{

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // The personal vault (#225): the pirate's LIFE, durable across a server restart. localStorage
    // autosave + export/import a .json file; the resume is always a BERTH (owner's dock-resume law),
    // never a stored orbit. What is NOT saved (deliberate dev-kindness, documented in Vault.cs): NPC
    // positions, a hunter mid-chase (heat IS saved, but the pursuit resolves as escaped on reload),
    // and autopilot plans — all recomputed from the fresh docked state.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // #310 — the ten-vault bookshelf. localStorage is reached through the ISlotStore the SaveSlotBook
    // reads and writes; the vault payload rides its own per-slot key (lossless), a small manifest holds
    // the labels. One rolling AUTOSAVE slot follows the ship (Continue reads it); nine MANUAL banks the
    // captain fills deliberately and the autosave never touches.
    //
    // feat/game-threads (owner 2026-07-18): each GAME START is its own universe — a game-thread GUID,
    // minted client-side, that namespaces the WHOLE ten-slot book (SaveSlotBook's per-thread keyspace).
    // So a NEW voyage never reads another thread's slots ("the roadster already found in a new game"
    // leak), and Continue resumes the ACTIVE thread. The GameThreadRegistry is the thin index of which
    // universes exist and which is active. Guids are minted HERE (Core stays pure).
    private readonly RendererSlotStore _slotStore = new();

    // The active game thread (universe). Null until a game is started/continued/migrated; every autosave
    // path first EnsureGameThread()s so a durable write always lands in a real thread, never the default
    // (un-namespaced) shelf.
    private string? _activeThreadId;

    private GameThreadRegistry? _threads;
    private GameThreadRegistry Threads => _threads ??= new GameThreadRegistry(_slotStore);

    // The book for the active thread, rebuilt whenever the active thread changes. An empty/null active id
    // yields the DEFAULT (pre-thread) shelf — only ever seen transiently before the first thread is minted
    // or adopted; gameplay writes always run through EnsureGameThread first.
    private SaveSlotBook? _slots;
    private string _slotsBoundThreadId = "￿"; // sentinel: matches no real id/empty, forces first build
    private SaveSlotBook Slots
    {
        get
        {
            string tid = _activeThreadId ?? "";
            if (_slots is null || _slotsBoundThreadId != tid)
            {
                _slots = new SaveSlotBook(_slotStore, tid);
                _slotsBoundThreadId = tid;
            }

            return _slots;
        }
    }

    /// <summary>The ISlotStore the book and registry write through: the defensive localStorage interop (a
    /// private-mode throw or a full quota is swallowed JS-side, so a save that "didn't take" never breaks
    /// the sim).</summary>
    private sealed class RendererSlotStore : ISlotStore
    {
        public string? Read(string key) => RendererInterop.VaultRead(key);
        public void Write(string key, string value) => RendererInterop.VaultWrite(key, value);
        public void Clear(string key) => RendererInterop.VaultClear(key);
    }

    // Mint a brand-new game thread and make it active — the fresh universe every new voyage gets. The GUID
    // is client-only (Core takes a string). The thread is registered immediately (stamped active) so a
    // reload mid-new-game continues THIS thread, not the one it was started from.
    private void BeginNewGameThread()
    {
        _activeThreadId = Guid.NewGuid().ToString("N");
        long now = DateTimeOffset.UtcNow.UtcTicks;
        Threads.Touch(_activeThreadId, "unknown waters", 0, now);
        RefreshThreadList();
        RefreshSlotList();
    }

    // Lazily ensure SOME active thread exists before a durable write — covers the direct-start paths that
    // bypass the new-voyage buttons (the ?start=/?dock= dev cheats), so their autosave still lands in a
    // real, isolated thread rather than the default shelf.
    private void EnsureGameThread()
    {
        if (string.IsNullOrEmpty(_activeThreadId))
        {
            BeginNewGameThread();
        }
    }

    // The whole "begin a new voyage" gesture: wipe the live universe back to a clean slate, THEN mint the
    // fresh thread it will save under. The two together are the fix for the owner's leak — a new start
    // shares NOTHING with the run it was launched from, in memory (this reset) or on disk (the new thread).
    // Called by every new-voyage entry (the front-door New voyage, the scenario "other skies", the berth
    // starts). NOT called by Continue/Load/Import (those hydrate a saved universe instead).
    private void EnterNewGameThread()
    {
        ResetLiveStateForNewGame();
        BeginNewGameThread();
    }

    // Reset every scrap of durable + discovered state to a brand-new-game slate (owner 2026-07-18: "different
    // game starts don't share state", and the follow-up: "mission statuses reset from the new game starting").
    // This is the exact inverse of BuildVault (so a new game equals a blank vault) PLUS the two SESSION-scoped
    // discovery sets the vault deliberately never carries — _revealedBodyIds (where "the roadster is found"
    // actually lives) and the scope-intel cards. The sim clock itself returns to the beginning date via the
    // ApplyStart/StartDockedAtHaven that runs right after (it rebuilds the ship at epoch 0).
    private void ResetLiveStateForNewGame()
    {
        // Purse + hold: the same opening stake a fresh boot lays down (Map.Trade constants), so a New voyage
        // is byte-for-byte the standard Earth opening.
        _credits = StartingCredits;
        _cargoByClass.Clear();
        foreach ((string cargoClass, int units) in StartingManifest)
        {
            _cargoByClass[cargoClass] = _cargoByClass.GetValueOrDefault(cargoClass) + units;
        }

        _hotCargo.Launder();
        RecomputeCargoTotals();

        // Ship consumables + the sentry roster, fresh.
        _slugAmmo = 12;
        _missileAmmo = 4;
        _shipBots.Clear();
        foreach (string unit in SentryBot.RosterUnits)
        {
            _shipBots.Add(new ShipBot(unit, SentryBot.MaxMagazine));
        }

        // Upgrades back to base (and the tank to the base capacity that implies), sensor rebuilt.
        _massLevel = 0;
        _sensorLevel = 0;
        _holdLevel = 0;
        _telescopeLevel = 0;
        _reactionMassPulses = ReactionMassCapacity; // = 500 at mass level 0
        _hasNetJammer = false;
        RebuildSensor();

        // Heat, nerve, insurance — the mood of the run, all calm again.
        _heat = HeatState.None;
        _nerve = NerveModel.Steady;
        _monolithSeen = false;
        _insurance = PirateInsurance.Uninsured;

        // The mission/contract slate and every relationship, wiped: a new universe owes nobody and knows
        // nobody (owner: mission statuses reset with the new game). New quest ids mint from zero again.
        _quests.Clear();
        _questSeq = 0;
        _favorObligations.Clear();
        _contacts.Clear();

        // The hoard and the bar-intel book — knowledge of a previous run's world, gone.
        _caches.Clear();
        _overheard = [];

        // #394: a new universe has not saved the Ringside Exchange — its dedication plaque reads its
        // original bronze until this run's crew earns the gratitude line. Any live gig is dropped too.
        _ringsideSaved = false;
        _deflection = null;
        _deflectionResolved = null;
        _deflectionRaiseMeters = 0;
        _deflectionLeftPort = false;

        // THE leak's home: the session-scoped "found it" sets. Clearing _revealedBodyIds re-hides every
        // scenario-hidden body (the derelict roadster among them) so a new game must re-discover it; the
        // scope-intel cards (scan fixes) go with it. _hiddenBodyIds is scenario data — left untouched.
        _revealedBodyIds.Clear();
        _scopeIntel.Clear();

        // #292 note: _tutorialPlayed is deliberately NOT reset here. Whether a fresh universe re-runs the
        // tutorial (and its date-triggered target rush) is the docked-starts lane's rework, not this one;
        // this lane owns that the persistence model resets and isolates the mission slate above.
    }

    // Peeked at boot so the front-door load view can lead with "Continue — <where>".
    private bool _resumeAvailable;
    private string? _resumeHavenName;
    private bool _resumeTampered;
    private Vault? _pendingResumeVault;

    // The labels of every occupied slot, projected for the front-door and the captain's-desk drawer.
    private IReadOnlyList<SaveSlotMeta> _slotList = [];

    // The in-game save/load drawer (#310, #292 quiet-drawer law): the SAME surface as the boot front
    // door, opened from the captain's desk. One surface, two doors.
    private bool _showSaveDrawer;

    private void OpenSaveDrawer()
    {
        RefreshSlotList();
        _resumeAvailable = Slots.Newest() is not null;
        _resumeHavenName = Slots.Newest()?.Where;
        _showSaveDrawer = true;
    }

    private void CloseSaveDrawer() => _showSaveDrawer = false;

    // The nine manual slot ids (1..9), for the drawer/front-door to render a bank-to row per slot.
    private static readonly string[] ManualSlotIds =
        [.. Enumerable.Range(1, SaveSlotBook.ManualSlotCount).Select(SaveSlotBook.ManualSlotId)];

    // The WHOLE rack (#312), in fixed slot order: the rolling autosave first, then the nine manual berths.
    // The front door and the captain's-desk drawer both render all ten — occupied ones (newest-first from
    // the label list) then empty berths — so the logbook is a shelf, never just the occupied handful.
    private static readonly string[] AllSlotIds =
        [SaveSlotBook.AutoSlotId, .. ManualSlotIds];

    // Every dockable station haven (id + name), inner → outer (scenario body order), straight from the one
    // registry (#297/#288) — the front door's primary "pick a berth to begin" list AND the ?dock menu.
    private IReadOnlyList<(string Id, string Name)> BerthStarts()
        => _ephemeris is null
            ? []
            : [.. DockableHavens.All(_ephemeris).Select(b => (b.Id, b.Name))];

    // Boot a brand-new voyage already clamped on at a chosen berth — the front door's primary start action
    // (docked-starts rework, 2026-07-18). Dismisses the front door and hands to the shared docked-start
    // path, then lands on the deck (walkable haven) or the Nav map (pumps-only berth).
    private async Task ChooseBerthStart(string havenId)
    {
        _showStartPicker = false;
        EnterNewGameThread(); // a berth start is a NEW voyage — fresh universe, fresh thread (feat/game-threads)
        StartDockedAtHaven(havenId);
        MaybeGreetTutorialHome(havenId); // a fresh new captain picking Selene Gate gets the soft-catch lesson, seeded here
        if (!_deckMode && _activeDesk != ShipDesk.Nav)
        {
            SwitchDesk(ShipDesk.Nav);
        }

        StateHasChanged();
        await _focusableDiv.FocusAsync();
    }

    // One debounced write path: every save-worthy event just raises this flag; the tick flushes a
    // single write, so a burst (dock → payment → deck) costs one serialize, not three.
    private bool _autosaveDirty;

    /// <summary>Request an autosave (debounced). Wired to every durable event: dock, undock, payment,
    /// boarding resolution, bury/dig, and every bank transaction.</summary>
    private void RequestVaultSave() => _autosaveDirty = true;

    private void FlushVaultSaveIfDirty()
    {
        if (!_autosaveDirty)
        {
            return;
        }

        _autosaveDirty = false;
        TryWriteVault();
    }

    // The rolling autosave: rewrite the ACTIVE THREAD's autosave slot with the live state. This is the fix
    // for the Mars pull — a fresh scenario start writes NOTHING here (it is not a durable in-play event),
    // so an accidental "Rusty Roadstead — docked" pick can no longer overwrite where you actually are — and
    // the fix for the cross-game leak: the write lands in THIS universe's thread (feat/game-threads), never
    // another's. The registry is touched alongside so "the newest thread" stays true and Continue current.
    private void TryWriteVault()
    {
        if (!_worldReady || _ephemeris is null)
        {
            return;
        }

        try
        {
            EnsureGameThread(); // a durable event always saves into a real, isolated thread
            Vault live = BuildVault();
            SaveSlotMeta meta = BuildSlotMeta(live, SaveSlotKind.Autosave);
            Slots.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(live), meta);
            Threads.Touch(_activeThreadId!, meta.Where, meta.SimDay, meta.SavedRealTicks);
            RefreshThreadList();
            RefreshSlotList();
        }
        catch
        {
            // An autosave must NEVER break the sim — a full storage or a serialize hiccup is silent;
            // the owner still has the export button and the previous good autosave.
        }
    }

    // The label beside a slot: WHERE (berth / adrift / unknown), WHEN (sim day + real wall-clock), and
    // the build stamp (#254). DateTimeOffset.UtcNow is the browser's clock in WASM — no JS interop needed.
    private SaveSlotMeta BuildSlotMeta(Vault vault, SaveSlotKind kind)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new SaveSlotMeta
        {
            Kind = kind,
            Where = SaveSlotLabels.Where(vault),
            WasDocked = vault.Resume?.WasDocked ?? false,
            SavedSimTime = vault.SavedSimTime,
            SimDay = (int)(vault.SavedSimTime / 86400),
            RealTimeLabel = now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            SavedRealTicks = now.UtcTicks,
            BuildStamp = BuildStamp.Display,
            Tampered = vault.Tampered,
        };
    }

    private void RefreshSlotList()
    {
        _slotList = Slots.List();
        RefreshVoyageGroups();
    }

    /// <summary>Gather the whole durable life into a vault envelope. Physics (orbit/trajectory) is
    /// deliberately absent — the resume section names the berth to wake at instead.</summary>
    private Vault BuildVault()
    {
        List<CargoLine> hold = _cargoByClass
            .Where(kv => kv.Value > 0)
            .Select(kv => new CargoLine(kv.Key, kv.Value))
            .ToList();

        return new Vault
        {
            SavedSimTime = _ship.SimTime,
            Purse = new PurseSection(_credits),
            Ship = new ShipSection
            {
                ReactionMassPulses = _reactionMassPulses,
                SlugAmmo = _slugAmmo,
                MissileAmmo = _missileAmmo,
                SentryMagazines = _shipBots.Select(b => b.Rounds).ToList(), // #314
            },
            Cargo = new CargoSection(hold, VaultMapper.ToHotLines(_hotCargo)),
            Heat = new HeatSection(_heat.Level, _heat.RaisedAtSimTime),
            Contacts = VaultMapper.ToSection(_contacts),
            Caches = VaultMapper.ToSection(_caches),
            Quests = BuildQuestsSection(),
            Insurance = VaultMapper.ToSection(_insurance),
            Upgrades = new UpgradesSection
            {
                MassLevel = _massLevel,
                SensorLevel = _sensorLevel,
                HoldLevel = _holdLevel,
                TelescopeLevel = _telescopeLevel,
            },
            DiceItems = BuildDiceItemsSection(),
            Progress = new ProgressSection { TutorialPlayed = _tutorialPlayed, RingsideSaved = _ringsideSaved }, // #292 / #394
            Nerve = new NerveSection { Nerve = _nerve, MonolithSeen = _monolithSeen }, // #317
            Overheard = _overheard.Count > 0 ? new OverheardSection { Lines = _overheard } : null, // bar intel, durable
            Resume = BuildResumeSection(),
        };
    }

    private QuestsSection BuildQuestsSection()
    {
        var quests = _quests.Select(q =>
        {
            var fields = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(q.TargetShipId)) fields["targetShipId"] = q.TargetShipId;
            if (!string.IsNullOrEmpty(q.TargetCallsign)) fields["targetCallsign"] = q.TargetCallsign;
            if (q.DestBodyId is { } dest) fields["destBodyId"] = dest;
            if (q.SourceBodyId is { } src) fields["sourceBodyId"] = src;
            if (q.Pin is { } pin) fields["pin"] = pin;

            return new QuestRecord
            {
                Id = q.Id,
                Kind = q.Kind.ToString(),
                Status = q.State.ToString(),
                Title = q.Title,
                Detail = q.Blurb,
                GiverContactId = q.Giver,
                RewardCredits = q.Reward,
                Fields = fields,
            };
        }).ToList();

        return new QuestsSection { Quests = quests, Obligations = VaultMapper.ToRecords(_favorObligations) };
    }

    // The persistent dice items (TTRPG helpers). Today only the boarding-nets jammer exists (the
    // dice-helper seam, #222); it saves as a labelled +2 modifier so the section is future-proof.
    private const string NetJammerItemId = "boarding-nets-jammer";

    private DiceItemsSection BuildDiceItemsSection()
    {
        var items = new List<DiceItemRecord>();
        if (_hasNetJammer)
        {
            items.Add(new DiceItemRecord(NetJammerItemId, "Boarding-nets jammer", 2));
        }

        return new DiceItemsSection(items);
    }

    // The resume berth: docked haven if clamped, else the nearest dockable haven at save time (never a
    // trajectory). Positions are read at the current sim time so a load rebuilds the ship clamped at
    // the load-time ephemeris.
    private ResumeSection? BuildResumeSection()
    {
        if (_ephemeris is null)
        {
            return null;
        }

        var havens = _ephemeris.Bodies
            .Where(IsDockableHaven)
            .Select(b => new VaultResume.HavenLocus(b.Id, b.Name, _ephemeris.Position(b.Id, _ship.SimTime)))
            .ToList();

        return VaultResume.Select(_dockedHavenId, _ship.Position, havens);
    }

    // Boot peek: adopt any pre-thread saves into a game thread, bind to the ACTIVE thread, then read its
    // newest slot so the front-door load view can lead with "Continue — <where>". Caches that vault for
    // Continue. Also exposes the OTHER threads (the registry) for the front door's parallel-voyage list.
    private void PeekSavedVault()
    {
        try
        {
            MigrateToThreadsIfNeeded();

            // Bind to the universe the game should resume (explicit-active, else newest). Null => a true
            // first run: no threads yet, one is minted when the captain picks a New voyage.
            _activeThreadId = Threads.Active()?.Id;
            RefreshThreadList();
            RefreshSlotList();

            SaveSlotMeta? newest = Slots.Newest();
            if (_activeThreadId is null || newest is null
                || Slots.ReadPayload(newest.Id) is not { } raw || string.IsNullOrWhiteSpace(raw))
            {
                _resumeAvailable = false;
                _pendingResumeVault = null;
                return;
            }

            Vault vault = VaultSerializer.Load(raw);
            _pendingResumeVault = vault;
            _resumeAvailable = true;
            _resumeTampered = vault.Tampered;
            _resumeHavenName = newest.Where;
            // #292: honor "tutorial played" even for a fresh Earth start this session — a returning
            // captain who finished the lessons last run should not be re-greeted just because they
            // pick a fresh Earth start over Continue. (Continue/Import overwrite this via ApplyVault.)
            _tutorialPlayed = vault.Progress?.TutorialPlayed ?? false;
        }
        catch
        {
            _resumeAvailable = false;
            _pendingResumeVault = null;
        }
    }

    // ── feat/game-threads migration: fold every pre-thread save into a freshly minted game thread, so a
    //    returning captain loses nothing and their one universe becomes thread #1 (the owner's migration
    //    law: "existing single-vault saves appear as slot 1, nothing lost"). Two shapes are adopted:
    //      (a) the #310 ten-slot DEFAULT shelf → copied wholesale into a new thread (all ten berths kept);
    //      (b) the pre-#310 single vault key → seeded as the thread's autosave AND manual slot 1.
    //    Runs once: the moment any thread exists (registry non-empty), migration is done forever. The old
    //    keys are left in place (harmless, never re-read) so a rollback still finds the original saves. ──
    private void MigrateToThreadsIfNeeded()
    {
        if (!Threads.IsEmpty)
        {
            return; // already on threads — nothing to adopt
        }

        try
        {
            var defaultShelf = new SaveSlotBook(_slotStore); // the un-namespaced, pre-thread book
            IReadOnlyList<SaveSlotMeta> existing = defaultShelf.List();

            if (existing.Count > 0)
            {
                // (a) Adopt the whole #310 shelf into a new thread, byte-for-byte.
                string threadId = Guid.NewGuid().ToString("N");
                var threadBook = new SaveSlotBook(_slotStore, threadId);
                threadBook.CopyFrom(defaultShelf);
                SaveSlotMeta newest = existing[0]; // List() is newest-first
                Threads.Touch(threadId, newest.Where, newest.SimDay, newest.SavedRealTicks);
                return;
            }

            if (defaultShelf.NeedsMigration()
                && defaultShelf.LegacyPayload() is { } legacyJson && !string.IsNullOrWhiteSpace(legacyJson))
            {
                // (b) Adopt the ancient single-slot vault as a thread's autosave + manual slot 1.
                Vault legacy = VaultSerializer.Load(legacyJson);
                string threadId = Guid.NewGuid().ToString("N");
                var threadBook = new SaveSlotBook(_slotStore, threadId);
                SaveSlotMeta auto = BuildSlotMeta(legacy, SaveSlotKind.Autosave);
                threadBook.Save(SaveSlotBook.AutoSlotId, legacyJson, auto);
                threadBook.Save(SaveSlotBook.ManualSlotId(1), legacyJson, auto with { Kind = SaveSlotKind.Manual });
                Threads.Touch(threadId, auto.Where, auto.SimDay, auto.SavedRealTicks);
            }
        }
        catch
        {
            // A corrupt legacy file simply doesn't migrate — the shelf starts empty; nothing crashes.
        }
    }

    // The registry's threads, newest-first, for the front-door "other voyages" list (a minimal load-a-game
    // door — #310's full picker builds on this keying). The active thread is the one Continue leads with.
    private IReadOnlyList<GameThreadInfo> _threadList = [];
    private void RefreshThreadList()
    {
        _threadList = Threads.List();
        RefreshVoyageGroups();
    }

    // The captains' roster (owner 2026-07-19): the front-door saved-voyages list grouped by universe —
    // one captain card per game thread, its save slots beneath it, the active captain first ("at the helm").
    // Built from the registry + each thread's own SaveSlotBook (the active one reuses the bound instance).
    private IReadOnlyList<GameThreadGroup> _voyageGroups = [];
    private void RefreshVoyageGroups()
        => _voyageGroups = GameThreads.GroupSlots(
            _threadList, _activeThreadId,
            tid => (tid == (_activeThreadId ?? "") ? Slots : new SaveSlotBook(_slotStore, tid)).List());

    // ── Captain-card display helpers (presentation over the Core-seeded identity). ──

    // The active universe's registry row — its captain identity — for the in-play captain chip (owner
    // 2026-07-19: "the current captain profile pic could also be at some corner of the screen while
    // playing"). Prefers the cached list; falls back to a direct registry read so the chip is correct even
    // before the first RefreshThreadList of a session. The name is EDITABLE stored data (a later lane's
    // rename UI writes GameThreadInfo.CaptainName); the chip just reads whatever is stored (or seeded).
    private GameThreadInfo? ActiveThreadInfo
        => string.IsNullOrEmpty(_activeThreadId)
            ? null
            : _threadList.FirstOrDefault(t => t.Id == _activeThreadId) ?? Threads.Get(_activeThreadId);

    // The book for a given thread id: the bound active book when it IS the active universe (so the drawer
    // and live writes stay on one instance), else a fresh book over the same store for that other universe.
    private SaveSlotBook BookFor(string threadId)
        => string.IsNullOrEmpty(threadId) || threadId == (_activeThreadId ?? "")
            ? Slots
            : new SaveSlotBook(_slotStore, threadId);

    // A captain's card subtitle: where the voyage sits and when it was last touched — the "which universe is
    // this" line under the name. A thin (pre-#354) thread honestly reads "unknown waters" here.
    private static string CaptainWhen(GameThreadInfo t)
    {
        string place = string.IsNullOrWhiteSpace(t.Where) ? "unknown waters" : t.Where;
        string last = t.LastActiveTicks > 0
            ? new DateTimeOffset(t.LastActiveTicks, TimeSpan.Zero).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "—";
        return $"{place} · day {Math.Max(0, t.SimDay)} · last active {last}";
    }

    // The captain card's "retired" line (Evening wind #20): who held this license before the piracy
    // insurance replaced them, most recent first. Compact — the newest one or two retirees, then a "+N
    // more" tail so a long-lived, oft-killed universe doesn't run the card off the door. Each entry reads
    // "under Capt. <name> until day <N>" (Core CaptainSuccession.RetiredLine).
    private static string CaptainRetiredSummary(GameThreadInfo t)
    {
        IReadOnlyList<RetiredCaptain> retired = t.Retired;
        if (retired.Count == 0)
        {
            return "";
        }

        const int show = 2;
        IEnumerable<RetiredCaptain> newestFirst = retired.Reverse();
        string head = string.Join(" · ", newestFirst.Take(show).Select(CaptainSuccession.RetiredLine));
        int more = retired.Count - show;
        return more > 0 ? $"{head} · +{more} more" : head;
    }

    // The monogram initial for the fallback avatar disc (first letter of the captain's given name).
    private static string CaptainInitial(string captainName)
    {
        string n = captainName.StartsWith("Captain ", StringComparison.Ordinal)
            ? captainName["Captain ".Length..]
            : captainName;
        return n.Length > 0 ? n[..1].ToUpperInvariant() : "?";
    }

    // A stable seeded hue for the fallback disc, so a captain whose portrait fails to load still gets a
    // consistent colour (and two captains rarely share one). Derived from the thread id, deterministic.
    private static string CaptainMonoColor(string id)
    {
        uint h = 2166136261u;
        foreach (char c in id)
        {
            h = (h ^ c) * 16777619u;
        }

        return $"hsl({h % 360} 42% 36%)";
    }

    // The front door's "Continue — <where>": restore the ACTIVE thread's newest slot instead of a fresh start.
    private Task ContinueFromSave() => LoadSlot(Slots.Newest()?.Id);

    // ── The captains' roster row actions (owner 2026-07-19): every save row now belongs to a captain
    //    (universe), so Load / Export / Import-into / Clear must target THAT captain's book, not the active
    //    one. In-game the drawer only ever renders the active thread, so these fall through to it. ──

    // Board a slot from any captain's card: switch the active universe to that captain (a deliberate
    // SetActive — it doesn't bump the thread newest), then load the chosen slot from its (now active) book.
    private Task LoadThreadSlot(string threadId, string slotId)
    {
        if (!string.IsNullOrEmpty(threadId) && threadId != _activeThreadId)
        {
            Threads.SetActive(threadId);
            _activeThreadId = threadId;
            RefreshSlotList();
        }

        return LoadSlot(slotId);
    }

    // Export a specific captain's slot to a .json (named for that slot's harbor · day · when-saved).
    private void ExportThreadSlot(string threadId, string slotId)
    {
        try
        {
            SaveSlotBook book = BookFor(threadId);
            if (book.ReadPayload(slotId) is not { } raw || string.IsNullOrWhiteSpace(raw) || book.Get(slotId) is not { } meta)
            {
                return;
            }

            string name = SaveFileNames.ForMeta(meta);
            RendererInterop.VaultDownload(name, raw);
            ShowPulseMessage($"⬇ Exported {meta.Where} as {name}.");
        }
        catch
        {
            ShowPulseMessage("Export failed — the browser blocked the download.");
        }
    }

    // Clear a specific captain's slot. If that empties an OTHER captain's shelf entirely, retire that thread
    // from the roster (front-door housekeeping — there is no live game to strand). The active universe is
    // never auto-retired out from under a running voyage.
    private void DeleteThreadSlot(string threadId, string slotId)
    {
        BookFor(threadId).Delete(slotId);
        if (!string.IsNullOrEmpty(threadId) && threadId != _activeThreadId
            && new SaveSlotBook(_slotStore, threadId).List().Count == 0)
        {
            Threads.Remove(threadId);
        }

        RefreshThreadList();
        RefreshSlotList();
        ShowPulseMessage($"🗑 Cleared slot {slotId}.");
    }

    // Import a file into a specific captain's berth WITHOUT boarding it (shelve a rescued Downloads save).
    private async Task ImportIntoThreadSlot(string threadId, string slotId)
    {
        string text;
        try
        {
            text = await RendererInterop.VaultImport();
        }
        catch
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            Vault vault = VaultSerializer.Load(text);
            SaveSlotKind kind = slotId == SaveSlotBook.AutoSlotId ? SaveSlotKind.Autosave : SaveSlotKind.Manual;
            BookFor(threadId).Save(slotId, text, BuildSlotMeta(vault, kind));
            RefreshThreadList();
            RefreshSlotList();
            ShowPulseMessage(vault.Tampered
                ? $"🗄 Banked into slot {slotId} — {SaveSlotLabels.Where(vault)} 📛 (edited outside the game). Not boarded; Load it when you mean to."
                : $"🗄 Banked into slot {slotId} — {SaveSlotLabels.Where(vault)}. Not boarded; Load it when you mean to.");
        }
        catch
        {
            ShowPulseMessage("Import refused — that file wasn't a readable save.");
        }

        StateHasChanged();
    }

    // Load a specific slot by id (the front-door list, the captain's-desk drawer, or Continue-newest).
    private async Task LoadSlot(string? slotId)
    {
        _showStartPicker = false;
        _showSaveDrawer = false;

        if (slotId is not null && Slots.ReadPayload(slotId) is { } raw && !string.IsNullOrWhiteSpace(raw))
        {
            Vault vault = VaultSerializer.Load(raw);
            ApplyVault(vault);
            RequestVaultSave(); // #310: the rolling autosave now follows THIS life, so Continue matches it
            ShowPulseMessage(vault.Tampered
                ? "📛 Vault loaded — the file was edited outside the game; the ledger is marked tampered."
                : $"💾 Loaded — {SaveSlotLabels.Where(vault)}.");
        }

        if (!_deckMode && _activeDesk != ShipDesk.Nav)
        {
            SwitchDesk(ShipDesk.Nav);
        }

        StateHasChanged();
        await _focusableDiv.FocusAsync();
    }

    /// <summary>Restore a vault into live state. Economy first (order-independent), then dock at the
    /// resume berth LAST so the ship is built fresh alongside the haven at load-time ephemeris — the
    /// owner's law that a resume is a berth, never a stored orbit.</summary>
    private void ApplyVault(Vault vault)
    {
        // Clear the ADDITIVE ledgers first (feat/game-threads). VaultMapper.Apply / ApplyHot LOAD into a
        // ledger without clearing, and _revealedBodyIds is never vaulted — so without this, loading a save
        // (especially switching to another universe via the other-voyages door) would MERGE the old run's
        // contacts, hoards, hot flags and discoveries into the loaded one. A load must be the loaded life,
        // whole and alone. (The other sections below already replace-on-apply, so they need no pre-clear.)
        _contacts.Clear();
        _caches.Clear();
        _hotCargo.Launder();
        _revealedBodyIds.Clear();
        _scopeIntel.Clear();

        if (vault.Purse is { } purse)
        {
            _credits = (int)Math.Clamp(purse.Credits, int.MinValue, int.MaxValue);
        }

        if (vault.Ship is { } ship)
        {
            _reactionMassPulses = (int)Math.Max(0, Math.Round(ship.ReactionMassPulses));
            _slugAmmo = Math.Max(0, ship.SlugAmmo);
            _missileAmmo = Math.Max(0, ship.MissileAmmo);

            // #314/#324: rebuild the full sentry roster (K-77, R-3B) from the saved magazines, padding any
            // missing entry to a full mag — a load never permanently shrinks the roster (the pinned Core
            // law SentryBot.RosterFromSave). A pre-#322 vault with no SentryMagazines loads as full 99s.
            _shipBots.Clear();
            IReadOnlyList<int> mags = SentryBot.RosterFromSave(ship.SentryMagazines);
            for (int i = 0; i < SentryBot.RosterUnits.Count; i++)
            {
                _shipBots.Add(new ShipBot(SentryBot.RosterUnits[i], mags[i]));
            }
        }

        if (vault.Upgrades is { } up)
        {
            _massLevel = Math.Max(0, up.MassLevel);
            _sensorLevel = Math.Max(0, up.SensorLevel);
            _holdLevel = Math.Max(0, up.HoldLevel);
            _telescopeLevel = Math.Max(0, up.TelescopeLevel);
            RebuildSensor();
        }

        ApplyCargo(vault.Cargo);

        if (vault.Heat is { } heat)
        {
            _heat = new HeatState(heat.Level, heat.RaisedAtSimTime);
        }

        VaultMapper.Apply(vault.Contacts, _contacts);
        VaultMapper.Apply(vault.Caches, _caches);
        _insurance = VaultMapper.ToInsurance(vault.Insurance);
        ApplyObligationsAndQuests(vault.Quests);
        ApplyDiceItems(vault.DiceItems);

        // #292: a saved life is never a fresh captain. Restore the "played" flag (a missing section —
        // an old save from before this flag — defaults to false, which is harmless: the greeting is
        // still suppressed below because a LOAD is not a fresh Earth start), then keep the nav clear.
        _tutorialPlayed = vault.Progress?.TutorialPlayed ?? _tutorialPlayed;
        // #394: restore whether this universe's crew turned the rock aside from Ringside — so its plaque
        // keeps the appended gratitude line across a reload (a pre-#394 save defaults false, harmless).
        _ringsideSaved = vault.Progress?.RingsideSaved ?? _ringsideSaved;

        // #317 — the nerve gauge rides the vault losslessly: a captain who fled shaking is still shaking
        // after a reload, and the monolith's first-sight hit stays spent. A missing section defaults calm.
        if (vault.Nerve is { } nerve)
        {
            _nerve = NerveModel.Clamp(nerve.Nerve);
            _monolithSeen = nerve.MonolithSeen;
        }

        // The "overheard at the bar" book (owner 2026-07-18): the tips/rumors a player was handed are
        // durable and revisitable — they survive the reload rather than living-and-vanishing in a toast.
        _overheard = vault.Overheard is { } book ? [.. book.Lines] : [];

        ApplyResumeBerth(vault.Resume, vault.SavedSimTime);

        // Loading a saved game shows NONE of the tutorial promotions (owner, 2026-07-18) — set last so
        // even the no-berth ApplyStart("earth") fallback above can't leave the greeting raised.
        _showTutorial = false;
    }

    private void ApplyCargo(CargoSection? cargo)
    {
        if (cargo is null)
        {
            return;
        }

        _cargoByClass.Clear();
        foreach (CargoLine line in cargo.Hold)
        {
            if (line.Units > 0)
            {
                _cargoByClass[line.CargoClass] = line.Units;
            }
        }

        VaultMapper.ApplyHot(cargo.Hot, _hotCargo);
        RecomputeCargoTotals();
    }

    private void RecomputeCargoTotals()
    {
        _cargoUnits = _cargoByClass.Values.Sum();
        _cargoValue = _cargoByClass.Sum(kv => kv.Value * CargoMarket.UnitValue(kv.Key));
    }

    private void ApplyObligationsAndQuests(QuestsSection? quests)
    {
        _favorObligations.Clear();
        _quests.Clear();
        if (quests is null)
        {
            return;
        }

        foreach (FavorObligation obligation in VaultMapper.ToObligations(quests.Obligations))
        {
            _favorObligations.Add(obligation);
        }

        int maxSeq = _questSeq;
        foreach (QuestRecord r in quests.Quests)
        {
            if (!Enum.TryParse(r.Kind, out QuestKind kind))
            {
                continue; // an unknown future quest kind — skip it, keep the rest (tolerant by design)
            }

            Enum.TryParse(r.Status, out QuestState state);
            var quest = new Quest(
                r.Id, kind, r.GiverContactId,
                r.Fields.GetValueOrDefault("targetShipId", ""),
                r.Fields.GetValueOrDefault("targetCallsign", ""),
                r.Title, r.Detail, r.RewardCredits,
                r.Fields.GetValueOrDefault("destBodyId"),
                r.Fields.GetValueOrDefault("sourceBodyId"),
                r.Fields.GetValueOrDefault("pin"))
            {
                State = state,
            };
            _quests.Add(quest);
            maxSeq = Math.Max(maxSeq, TrailingInt(r.Id));
        }

        _questSeq = maxSeq; // new quest ids mint beyond every restored one
    }

    private static int TrailingInt(string id)
    {
        int i = id.Length;
        while (i > 0 && char.IsDigit(id[i - 1]))
        {
            i--;
        }

        return i < id.Length && int.TryParse(id.AsSpan(i), out int n) ? n : 0;
    }

    private void ApplyDiceItems(DiceItemsSection? dice)
    {
        _hasNetJammer = dice?.Items.Any(i => i.ItemId == NetJammerItemId) ?? false;
    }

    // Dock the ship at the resume berth: built fresh alongside the haven at the SAVED sim time (bodies
    // are deterministic from time, so this reconstructs the world exactly), zero relative velocity,
    // clamped. No stored orbit ever crosses the save boundary.
    private void ApplyResumeBerth(ResumeSection? resume, double savedSimTime)
    {
        string? havenId = resume?.HavenId;
        if (_ephemeris is null || havenId is null || _ephemeris.Bodies.All(b => b.Id != havenId))
        {
            ApplyStart("earth"); // no berth to resume at — fall back to the docked tutorial home (Selene Gate)
            return;
        }

        // #256: the id is the truth we resume at; the name is a convenience the picker showed. If the
        // vault's two fields disagree (a real export had HavenId 'the-space-bar' with HavenName 'The
        // Rusty Roadstead' — a nearest-haven computation and a display lookup that resolved DIFFERENT
        // bars), prefer the id and mark the ledger rather than wake the captain at the wrong bar. New
        // saves can't disagree — VaultResume.Select derives both from one HavenLocus — but old files can.
        if (resume?.HavenName is { } savedName && BodyName(havenId) is { } trueName && savedName != trueName)
        {
            LogAutopilotEvent($"the vault's memory of where we were is smudged — it named '{savedName}', but the berth is {trueName}; resuming by id");
        }

        _dockedHavenId = null;
        SetDeckForDock(null);

        Vector2d dockPos = _ephemeris.Position(havenId, savedSimTime);
        _ship = BerthState.CoMoving(_ephemeris, havenId, savedSimTime, BerthState.BerthOffsetMeters); // the shared berth build (#269)

        SimTime = savedSimTime;

        // #255 — the freeze class: the world was seeded with movers at boot epoch ~0 BEFORE this vault
        // restore runs (traffic is generated once during OnAfterRender, then the start picker offers
        // "Continue"). Jumping the clock to a far savedSimTime (the owner's 8.3-year "the-tilt" save) would
        // leave every scheduled mover a decade behind — StepNpcs would try to integrate the whole gap at the
        // 60 s NpcTimeStep on the first frame and hard-freeze the tab. Re-seed exactly as a long-haul jump
        // does: keep the pure-rails depots, drop the epoch-0 movers, and let RefillTraffic repopulate fresh
        // AT the resume epoch. A vault resume IS a void crossing — the world we left is a decade gone.
        ReseedWorldForJump(savedSimTime);

        _dockedHavenId = havenId;
        _dockOffset = _ship.Position - dockPos;
        SetDeckForDock(havenId);
        (_avatarX, _avatarY, _avatarHeading) = (2.5, 6, Math.PI / 2);
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;

        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
    }

    // ── The save/load surface (#310): one set of actions, two doors (the boot front-door and the
    //    captain's-desk drawer). Manual bank to a slot, delete a slot, export the live moment, import a
    //    file straight into play. Every control's razor tip says exactly what it reads and writes. ──

    /// <summary>Bank the LIVE state into a specific manual slot (pre-haul, pre-bury — the vault moments).
    /// The autosave never touches these, so a deliberate bank is safe from the rolling save.</summary>
    private void SaveToSlot(string slotId)
    {
        try
        {
            Vault live = BuildVault();
            Slots.Save(slotId, VaultSerializer.Save(live), BuildSlotMeta(live, SaveSlotKind.Manual));
            RefreshSlotList();
            ShowPulseMessage($"💾 Banked to slot {slotId} — {SaveSlotLabels.Where(live)}.");
        }
        catch
        {
            ShowPulseMessage("The bank refused — storage is full or unavailable.");
        }
    }

    // Legacy single-button "Save to vault": now banks the live moment into the first free manual slot
    // (or slot 1). Kept so the captain's-desk quick-save button still works without opening the drawer.
    private void SaveVaultManually()
    {
        string target = FirstFreeManualSlotId() ?? SaveSlotBook.ManualSlotId(1);
        SaveToSlot(target);
    }

    private string? FirstFreeManualSlotId()
    {
        for (int n = 1; n <= SaveSlotBook.ManualSlotCount; n++)
        {
            string id = SaveSlotBook.ManualSlotId(n);
            if (Slots.Get(id) is null)
            {
                return id;
            }
        }

        return null;
    }

    // EXPORT = the LIVE state at press time, never a stale slot (owner, #310). Reads current game state,
    // serializes it fresh, downloads it — NAMED for the harbor it was saved at (#312): the file names the
    // place, so six files in Downloads no longer play "which one is the Uranus save?".
    private void ExportVault()
    {
        try
        {
            Vault live = BuildVault();
            string name = SaveFileNames.ForMeta(BuildSlotMeta(live, SaveSlotKind.Autosave));
            RendererInterop.VaultDownload(name, VaultSerializer.Save(live));
            ShowPulseMessage($"⬇ Exported this moment as {name}.");
        }
        catch
        {
            ShowPulseMessage("Export failed — the browser blocked the download.");
        }
    }

    // IMPORT = immediately the live game (owner, #310). Asks ONCE before replacing the current voyage,
    // offering a one-click "bank current to a slot first" escape. After import the rolling autosave
    // updates to the imported state, so Continue matches what the player sees.
    private bool _importConfirming;
    private string? _importPendingText;

    // The label the consent screen shows, read FROM THE FILE'S CONTENTS (#312) — never the filename, so a
    // renamed spacesails-vault (5).json still says "The Tilt · day 34" before the captain commits to it.
    private SaveSlotMeta? _importPreview;

    private async Task ImportVault()
    {
        string text;
        try
        {
            text = await RendererInterop.VaultImport();
        }
        catch
        {
            return; // a cancelled or failed picker is a no-op
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Read the label from the file itself so the ask can say WHAT it is about to board.
        try
        {
            _importPreview = SaveSlotLabels.PreviewMeta(VaultSerializer.Load(text));
        }
        catch
        {
            _importPreview = null; // an unreadable file: the ask still warns, just without a label
        }

        // Hold the picked file and ask before it replaces the running voyage.
        _importPendingText = text;
        _importConfirming = true;
        StateHasChanged();
    }

    // "Bank current first, then import": the safety escape — the running voyage is banked to a free
    // manual slot before the imported file becomes live, so nothing in-flight is lost.
    private async Task ConfirmImportBankingFirst()
    {
        SaveVaultManually();
        await ApplyPendingImport();
    }

    // "Replace now": the imported file becomes the live game at once (autosave adopts it).
    private Task ConfirmImportReplace() => ApplyPendingImport();

    private void CancelImport()
    {
        _importConfirming = false;
        _importPendingText = null;
        _importPreview = null;
    }

    private async Task ApplyPendingImport()
    {
        string? text = _importPendingText;
        _importConfirming = false;
        _importPendingText = null;
        _importPreview = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Vault vault = VaultSerializer.Load(text);
        // An imported file is a whole universe arriving — give it its OWN thread (feat/game-threads) so it
        // never overwrites the autosave of the run that was live; that run stays intact under its own thread
        // and remains resumable. ApplyVault clears the slate first, so the imported life boards clean.
        BeginNewGameThread();
        ApplyVault(vault);
        RequestVaultSave(); // the new thread's autosave adopts the imported state → Continue matches the screen
        RefreshThreadList();
        RefreshSlotList();
        ShowPulseMessage(vault.Tampered
            ? "📛 Imported — the file was edited outside the game; the ledger is marked tampered."
            : $"⬆ Imported — now flying {SaveSlotLabels.Where(vault)}.");
        if (!_deckMode && _activeDesk != ShipDesk.Nav)
        {
            SwitchDesk(ShipDesk.Nav);
        }

        StateHasChanged();
        await _focusableDiv.FocusAsync();
    }
}
