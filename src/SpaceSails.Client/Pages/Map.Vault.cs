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
    private SaveSlotBook? _slots;
    private SaveSlotBook Slots => _slots ??= new SaveSlotBook(new RendererSlotStore());

    /// <summary>The ISlotStore the book writes through: the defensive localStorage interop (a private-mode
    /// throw or a full quota is swallowed JS-side, so a save that "didn't take" never breaks the sim).</summary>
    private sealed class RendererSlotStore : ISlotStore
    {
        public string? Read(string key) => RendererInterop.VaultRead(key);
        public void Write(string key, string value) => RendererInterop.VaultWrite(key, value);
        public void Clear(string key) => RendererInterop.VaultClear(key);
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

    // #310: the experimental non-Sol skies are demoted below a footer toggle on the front door — a new
    // player never trips over the barely-playtested scenarios.
    private bool _showScenarioStarts;

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

    // #312: the New-voyage "start at a berth" expander — a modest shelf for the bench to boot a fresh
    // campaign already docked at any dockable haven, routed through the SAME StartDockedAtHaven the
    // ?dock=<id> cheat uses (no parallel boot code). The truly-new player still sees one big New-voyage.
    private bool _showBerthStarts;

    // Every dockable station haven (id + name) for the expander, straight from the one registry (#297).
    private IReadOnlyList<(string Id, string Name)> BerthStarts()
        => _ephemeris is null
            ? []
            : [.. DockableHavens.All(_ephemeris).Select(b => (b.Id, b.Name))];

    // Boot a brand-new voyage already clamped on at a chosen berth — the expander's action. Dismisses the
    // front door and hands to the shared docked-start path, then lands on the deck/Nav like ChooseStart.
    private async Task ChooseBerthStart(string havenId)
    {
        _showStartPicker = false;
        _showBerthStarts = false;
        StartDockedAtHaven(havenId);
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

    // The rolling autosave: rewrite the ONE autosave slot with the live state. This is the fix for the
    // Mars pull — a fresh scenario start writes NOTHING here (it is not a durable in-play event), so an
    // accidental "Rusty Roadstead — docked" pick can no longer overwrite where you actually are.
    private void TryWriteVault()
    {
        if (!_worldReady || _ephemeris is null)
        {
            return;
        }

        try
        {
            Vault live = BuildVault();
            Slots.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(live), BuildSlotMeta(live, SaveSlotKind.Autosave));
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

    private void RefreshSlotList() => _slotList = Slots.List();

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
            Progress = new ProgressSection { TutorialPlayed = _tutorialPlayed }, // #292
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

    // Boot peek: migrate any pre-#310 single save into the shelf, then read the newest slot so the
    // front-door load view can lead with "Continue — <where>". Caches that newest vault for Continue.
    private void PeekSavedVault()
    {
        try
        {
            MigrateLegacyVaultIfNeeded();
            RefreshSlotList();

            SaveSlotMeta? newest = Slots.Newest();
            if (newest is null || Slots.ReadPayload(newest.Id) is not { } raw || string.IsNullOrWhiteSpace(raw))
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

    // #310 migration: a player who saved before the shelf existed has one vault under the legacy key.
    // Seed BOTH the rolling autosave (so Continue immediately continues it — "where I actually am") AND
    // manual slot 1 (the deliberate bank the issue names), so nothing is lost and the old save is a slot.
    private void MigrateLegacyVaultIfNeeded()
    {
        if (!Slots.NeedsMigration() || Slots.LegacyPayload() is not { } legacyJson || string.IsNullOrWhiteSpace(legacyJson))
        {
            return;
        }

        try
        {
            Vault legacy = VaultSerializer.Load(legacyJson);
            SaveSlotMeta auto = BuildSlotMeta(legacy, SaveSlotKind.Autosave);
            Slots.Save(SaveSlotBook.AutoSlotId, legacyJson, auto);
            Slots.Save(SaveSlotBook.ManualSlotId(1), legacyJson, auto with { Kind = SaveSlotKind.Manual });
        }
        catch
        {
            // A corrupt legacy file simply doesn't migrate — the new shelf starts empty; nothing crashes.
        }
    }

    // The front door's "Continue — <where>": restore the newest slot instead of a fresh start.
    private Task ContinueFromSave() => LoadSlot(Slots.Newest()?.Id);

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
        if (vault.Purse is { } purse)
        {
            _credits = (int)Math.Clamp(purse.Credits, int.MinValue, int.MaxValue);
        }

        if (vault.Ship is { } ship)
        {
            _reactionMassPulses = (int)Math.Max(0, Math.Round(ship.ReactionMassPulses));
            _slugAmmo = Math.Max(0, ship.SlugAmmo);
            _missileAmmo = Math.Max(0, ship.MissileAmmo);
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
            ApplyStart("earth"); // no berth to resume at — fall back to a fresh Earth spawn
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

    /// <summary>Empty a manual slot (the autosave slot is never offered for deletion — it re-fills itself).</summary>
    private void DeleteSlot(string slotId)
    {
        Slots.Delete(slotId);
        RefreshSlotList();
        ShowPulseMessage($"🗑 Cleared slot {slotId}.");
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

    // Per-slot EXPORT (#312): download a stored slot's bytes byte-for-byte, named for THAT slot's state
    // (its label — place · day · when-saved), not the live moment. So exporting slot 3 gives you exactly
    // the voyage banked in slot 3, in a file that names where slot 3 was.
    private void ExportSlot(string slotId)
    {
        try
        {
            if (Slots.ReadPayload(slotId) is not { } raw || string.IsNullOrWhiteSpace(raw) || Slots.Get(slotId) is not { } meta)
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

    // IMPORT INTO A BERTH (#312): bank a picked file straight into a chosen slot WITHOUT boarding it — the
    // live game keeps flying. This is the shelf: file the rescued Downloads voyages into named berths, then
    // Load the one you mean. Distinct from the top-level Import (which becomes live after the ask): two
    // buttons, two labelled truths (#310 semantics law). At boot there is no live state to protect; in-game
    // this leaves the rolling autosave and the running voyage entirely untouched.
    private async Task ImportIntoSlot(string slotId)
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
            // Store the file's bytes byte-for-byte; the label is read from the file's own content.
            Slots.Save(slotId, text, BuildSlotMeta(vault, kind));
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
        ApplyVault(vault);
        RequestVaultSave(); // the rolling autosave adopts the imported state → Continue matches the screen
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
