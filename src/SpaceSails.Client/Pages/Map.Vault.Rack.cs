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

// Map.Vault.Rack — THE VERBS ON A SAVE ROW: load it, export it, delete it, import over it, bank the live
// moment into it. One set of actions, two doors (the boot front door and the captain's-desk drawer).
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Vault.cs` — see the note at the head of
// `Map.Vault.Threads.cs` for why the file was cut and what "pure motion" is holding here.
public partial class Map
{
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
        await TheRestOfTheBootAsync(); // #161: Continue is clickable while the sky is still being plotted

        if (slotId is not null && Slots.ReadPayload(slotId) is { } raw && !string.IsNullOrWhiteSpace(raw))
        {
            Vault vault = VaultSerializer.Load(raw);
            // #951 — a load from the death card is the answer to the death: the catch/impact/regolith card
            // that was open belongs to a timeline this captain just walked out of, so it goes with it. (On
            // every other load path there is no card open and this is a no-op.)
            _busted = null;
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

    // ── The save/load surface (#310): one set of actions, two doors (the boot front-door and the
    //    captain's-desk drawer). Manual bank to a slot, delete a slot, export the live moment, import a
    //    file straight into play. Every control's razor tip says exactly what it reads and writes. ──

    /// <summary>Bank the LIVE state into a specific manual slot (pre-haul, pre-bury — the vault moments).
    /// The autosave never touches these, so a deliberate bank is safe from the rolling save.</summary>
    private void SaveToSlot(string slotId, string title = "", string note = "")
    {
        try
        {
            Vault live = BuildVault(title, note);
            SaveSlotMeta meta = BuildSlotMeta(live, SaveSlotKind.Manual);
            Slots.Save(slotId, VaultSerializer.Save(live), meta);
            RefreshSlotList();
            ShowPulseMessage($"💾 Banked to slot {slotId} — {SaveSlotLabels.TitleOf(meta)}.");
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
    private void ExportVault(string title = "", string note = "")
    {
        try
        {
            Vault live = BuildVault(title, note);
            string name = SaveFileNames.ForMeta(BuildSlotMeta(live, SaveSlotKind.Autosave));
            RendererInterop.VaultDownload(name, VaultSerializer.Save(live));
            ShowPulseMessage($"⬇ Exported this moment as {name}.");
        }
        catch
        {
            ShowPulseMessage("Export failed — the browser blocked the download.");
        }
    }
}
