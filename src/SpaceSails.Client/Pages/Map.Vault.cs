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
    private const string VaultStorageKey = "spacesails.vault.v1";

    // Peeked at boot so the start picker can lead with "Continue — docked at <haven>".
    private bool _resumeAvailable;
    private string? _resumeHavenName;
    private bool _resumeTampered;
    private Vault? _pendingResumeVault;

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

    private void TryWriteVault()
    {
        if (!_worldReady || _ephemeris is null)
        {
            return;
        }

        try
        {
            RendererInterop.VaultWrite(VaultStorageKey, VaultSerializer.Save(BuildVault()));
        }
        catch
        {
            // An autosave must NEVER break the sim — a full storage or a serialize hiccup is silent;
            // the owner still has the export button and the previous good autosave.
        }
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

    // Boot peek: is there a saved run, and where does it resume? Caches the loaded vault for Continue.
    private void PeekSavedVault()
    {
        try
        {
            string? raw = RendererInterop.VaultRead(VaultStorageKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            Vault vault = VaultSerializer.Load(raw);
            _pendingResumeVault = vault;
            _resumeAvailable = true;
            _resumeTampered = vault.Tampered;
            _resumeHavenName = vault.Resume?.HavenName;
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

    // The picker's "Continue — docked at <haven>": restore the saved life instead of a fresh start.
    private async Task ContinueFromSave()
    {
        _showStartPicker = false;
        if (_pendingResumeVault is { } vault)
        {
            ApplyVault(vault);
            if (vault.Tampered)
            {
                ShowPulseMessage("📛 Vault loaded — the file was edited outside the game; the ledger is marked tampered.");
            }
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

    // Captain-desk actions (#225): a manual save, an export download, and an import file-picker.
    private void SaveVaultManually()
    {
        TryWriteVault();
        ShowPulseMessage("💾 Saved to the vault.");
    }

    private void ExportVault()
    {
        try
        {
            RendererInterop.VaultDownload("spacesails-vault.json", VaultSerializer.Save(BuildVault()));
            ShowPulseMessage("⬇ Vault exported as spacesails-vault.json.");
        }
        catch
        {
            ShowPulseMessage("Export failed — the browser blocked the download.");
        }
    }

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

        Vault vault = VaultSerializer.Load(text);
        ApplyVault(vault);
        RequestVaultSave(); // adopt the imported vault as the new autosave
        ShowPulseMessage(vault.Tampered
            ? "📛 Vault imported — the file was edited outside the game; the ledger is marked tampered."
            : "⬆ Vault imported.");
        if (!_deckMode && _activeDesk != ShipDesk.Nav)
        {
            SwitchDesk(ShipDesk.Nav);
        }

        StateHasChanged();
        await _focusableDiv.FocusAsync();
    }
}
