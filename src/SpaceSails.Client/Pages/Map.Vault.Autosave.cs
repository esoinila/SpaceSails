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

// Map.Vault.Autosave — THE ROLLING WRITE: one debounced flag, one serialize per tick, and the label the
// row it lands on wears.
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Vault.cs` — see the note at the head of
// `Map.Vault.Threads.cs` for why the file was cut and what "pure motion" is holding here.
public partial class Map
{
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
            // #948 — mirrored FROM THE PAYLOAD, never gathered a second time here: the row and the file it
            // labels then cannot disagree about whose moment this is or what was written on it.
            CaptainName = SaveSlotLabels.CleanName(vault.Logbook?.CaptainName),
            Title = SaveSlotLabels.CleanTitle(vault.Logbook?.Title),
            Note = SaveSlotLabels.CleanNote(vault.Logbook?.Note),
        };
    }

    private void RefreshSlotList()
    {
        _slotList = Slots.List();
        RefreshVoyageGroups();
    }
}
