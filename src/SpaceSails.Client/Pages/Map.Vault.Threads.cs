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

// Map.Vault.Threads — THE GAME THREAD AND ITS SHELF: which universe this run belongs to, the ten-slot
// book that universe saves into, and the boot peek that adopts, binds and reads it.
//
// #251 · MOVED HERE BY PURE MOTION. `Map.Vault.cs` stood at 1,425 lines and three lanes in one week
// (#1092, #1117, #1133) each had to shove a block out of it to fit their own row under the 1,500-line law
// (NoSourceFileIsTooLongTests). A file that has to be split to be added to is a file that is telling you
// what its seams are; this lane cuts along all of them at once so the next row is just a row.
//
// Every line below is the line that was in `Map.Vault.cs`, character for character, in the order it was in.
// Nothing was renamed, no signature changed, and no statement moved inside a method. A partial is the same
// class: the page's field roster, its three ledgers and every source guard that reads this family are
// unmoved by construction.
public partial class Map
{
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
}
