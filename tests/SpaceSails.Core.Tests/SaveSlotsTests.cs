namespace SpaceSails.Core.Tests;

/// <summary>
/// The ten-vault bookshelf (#310): rolling autosave + manual banks over the lossless Vault round-trip,
/// the "Continue = newest" rule, migration of the pre-#310 single slot, and the guarantee that a manual
/// bank is never clobbered by the rolling autosave (the very bug that pulled the owner back to Mars).
/// </summary>
public class SaveSlotsTests
{
    // A dictionary-backed store — the whole book is browserless and deterministic to test.
    private sealed class MemStore : ISlotStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
        public string? Read(string key) => _map.TryGetValue(key, out string? v) ? v : null;
        public void Write(string key, string value) => _map[key] = value;
        public void Clear(string key) => _map.Remove(key);
        public bool Has(string key) => _map.ContainsKey(key);
    }

    private static Vault VaultAt(string havenId, string havenName, bool docked, double simTime) => new()
    {
        SavedSimTime = simTime,
        Purse = new PurseSection(1234),
        Resume = new ResumeSection { HavenId = havenId, HavenName = havenName, WasDocked = docked },
    };

    private static SaveSlotMeta MetaFor(Vault v, SaveSlotKind kind, long ticks) => new()
    {
        Kind = kind,
        Where = SaveSlotLabels.Where(v),
        WasDocked = v.Resume?.WasDocked ?? false,
        SavedSimTime = v.SavedSimTime,
        SimDay = (int)(v.SavedSimTime / 86400),
        RealTimeLabel = "2026-07-18 21:40",
        SavedRealTicks = ticks,
        BuildStamp = "build test",
        Tampered = v.Tampered,
    };

    [Fact]
    public void SlotRoundTrip_IsLossless_ThroughTheVaultSerializer()
    {
        var book = new SaveSlotBook(new MemStore());
        Vault original = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        string json = VaultSerializer.Save(original);

        book.Save(SaveSlotBook.ManualSlotId(3), json, MetaFor(original, SaveSlotKind.Manual, ticks: 100));

        string? read = book.ReadPayload(SaveSlotBook.ManualSlotId(3));
        Assert.NotNull(read);
        Vault restored = VaultSerializer.Load(read!);
        Assert.Equal("the-tilt", restored.Resume!.HavenId);
        Assert.Equal("The Tilt", restored.Resume!.HavenName);
        Assert.True(restored.Resume!.WasDocked);
        Assert.Equal(2_600_000, restored.SavedSimTime);
        Assert.Equal(1234, restored.Purse!.Credits);
    }

    [Fact]
    public void WhereLabel_NamesTheBerth_AdriftOrUnknown()
    {
        Assert.Equal("The Tilt", SaveSlotLabels.Where(VaultAt("the-tilt", "The Tilt", docked: true, 0)));
        Assert.Equal("adrift near The Tilt", SaveSlotLabels.Where(VaultAt("the-tilt", "The Tilt", docked: false, 0)));
        Assert.Equal("unknown waters", SaveSlotLabels.Where(new Vault()));
    }

    [Fact]
    public void Autosave_UpdatesTheLabelAndPlace_AsTheShipMoves()
    {
        var book = new SaveSlotBook(new MemStore());

        Vault atMars = VaultAt("the-space-bar", "The Rusty Roadstead", docked: true, simTime: 0);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(atMars), MetaFor(atMars, SaveSlotKind.Autosave, 10));
        Assert.Equal("The Rusty Roadstead", book.Get(SaveSlotBook.AutoSlotId)!.Where);

        // The ship sails on to Uranus — the rolling autosave rewrites the SAME slot with the new place.
        Vault atTilt = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(atTilt), MetaFor(atTilt, SaveSlotKind.Autosave, 20));

        SaveSlotMeta auto = book.Get(SaveSlotBook.AutoSlotId)!;
        Assert.Equal("The Tilt", auto.Where);
        Assert.Equal((int)(2_600_000 / 86400), auto.SimDay);
        Assert.Single(book.List(), s => s.Kind == SaveSlotKind.Autosave); // still ONE autosave, updated in place
    }

    [Fact]
    public void Continue_PicksTheAutosave_AsTheNewest()
    {
        var book = new SaveSlotBook(new MemStore());

        // An old manual bank at Mars, then the rolling autosave carries on to Uranus (newer tick).
        Vault mars = VaultAt("the-space-bar", "The Rusty Roadstead", docked: true, simTime: 0);
        book.Save(SaveSlotBook.ManualSlotId(1), VaultSerializer.Save(mars), MetaFor(mars, SaveSlotKind.Manual, ticks: 100));

        Vault tilt = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(tilt), MetaFor(tilt, SaveSlotKind.Autosave, ticks: 200));

        SaveSlotMeta? cont = book.Newest();
        Assert.NotNull(cont);
        Assert.Equal(SaveSlotBook.AutoSlotId, cont!.Id);
        Assert.Equal("The Tilt", cont.Where); // Continue = where I actually am, not the Rusty Nail
    }

    [Fact]
    public void ManualBank_IsNeverTouchedByTheAutosave()
    {
        var book = new SaveSlotBook(new MemStore());

        // Bank a deliberate pre-haul save at The Tilt.
        Vault banked = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        string bankedJson = VaultSerializer.Save(banked);
        book.Save(SaveSlotBook.ManualSlotId(2), bankedJson, MetaFor(banked, SaveSlotKind.Manual, ticks: 50));

        // The rolling autosave then wanders off and rewrites its own slot many times.
        for (int i = 0; i < 5; i++)
        {
            Vault drifting = VaultAt("the-space-bar", "The Rusty Roadstead", docked: false, simTime: i * 1000);
            book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(drifting), MetaFor(drifting, SaveSlotKind.Autosave, 60 + i));
        }

        // The manual slot is byte-for-byte untouched.
        Assert.Equal(bankedJson, book.ReadPayload(SaveSlotBook.ManualSlotId(2)));
        Assert.Equal("The Tilt", book.Get(SaveSlotBook.ManualSlotId(2))!.Where);
    }

    [Fact]
    public void Migration_ImportsThePre310SingleSlot_NothingLost()
    {
        var store = new MemStore();
        Vault legacy = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        store.Write(SaveSlotBook.LegacyKey, VaultSerializer.Save(legacy));

        var book = new SaveSlotBook(store);
        Assert.True(book.NeedsMigration());

        // The client migration: the legacy vault becomes the rolling autosave AND manual slot 1.
        string legacyJson = book.LegacyPayload()!;
        Vault parsed = VaultSerializer.Load(legacyJson);
        book.Save(SaveSlotBook.AutoSlotId, legacyJson, MetaFor(parsed, SaveSlotKind.Autosave, 1));
        book.Save(SaveSlotBook.ManualSlotId(1), legacyJson, MetaFor(parsed, SaveSlotKind.Manual, 1));

        Assert.False(book.NeedsMigration()); // a manifest now exists — migration is one-time
        Assert.Equal("The Tilt", book.Newest()!.Where); // Continue immediately resumes the migrated life
        Assert.Equal("The Tilt", book.Get(SaveSlotBook.ManualSlotId(1))!.Where);
        Vault restored = VaultSerializer.Load(book.ReadPayload(SaveSlotBook.AutoSlotId)!);
        Assert.Equal(2_600_000, restored.SavedSimTime);
    }

    [Fact]
    public void Delete_ForgetsPayloadAndLabel()
    {
        var book = new SaveSlotBook(new MemStore());
        Vault v = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 0);
        book.Save(SaveSlotBook.ManualSlotId(4), VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Manual, 5));
        Assert.NotNull(book.Get(SaveSlotBook.ManualSlotId(4)));

        book.Delete(SaveSlotBook.ManualSlotId(4));
        Assert.Null(book.Get(SaveSlotBook.ManualSlotId(4)));
        Assert.Null(book.ReadPayload(SaveSlotBook.ManualSlotId(4)));
    }

    [Fact]
    public void Export_SerializesTheLiveState_ByteStable()
    {
        // Export writes VaultSerializer.Save(BuildVault()) — the LIVE moment, not a stored slot. The
        // load-bearing guarantee is that those bytes ARE the state: re-loading and re-saving them is a
        // fixed point (so an exported file, re-imported, is byte-for-byte the same voyage).
        var live = new Vault
        {
            SavedSimTime = 2_600_000,
            Purse = new PurseSection(1234),
            Ship = new ShipSection { ReactionMassPulses = 42, SlugAmmo = 3, MissileAmmo = 1 },
            Resume = new ResumeSection { HavenId = "the-tilt", HavenName = "The Tilt", WasDocked = true },
        };
        string exported = VaultSerializer.Save(live);
        string reExported = VaultSerializer.Save(VaultSerializer.Load(exported));
        Assert.Equal(exported, reExported);
    }

    [Fact]
    public void ImportThenReload_ContinuesTheImportedState_NoExtraSavePress()
    {
        // The client import writes the imported bytes into the autosave slot (RequestVaultSave adopts it).
        // Model that: after import, Continue (Newest) resumes the imported state with no further press.
        var book = new SaveSlotBook(new MemStore());

        // The player is currently at Mars (autosave).
        Vault current = VaultAt("the-space-bar", "The Rusty Roadstead", docked: true, simTime: 0);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(current), MetaFor(current, SaveSlotKind.Autosave, 10));

        // They import a file captured at The Tilt — it becomes the autosave at once.
        Vault imported = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        string importedJson = VaultSerializer.Save(imported);
        book.Save(SaveSlotBook.AutoSlotId, importedJson, MetaFor(imported, SaveSlotKind.Autosave, 20));

        SaveSlotMeta? cont = book.Newest();
        Assert.Equal(SaveSlotBook.AutoSlotId, cont!.Id);
        Assert.Equal("The Tilt", cont.Where); // Continue matches what the player just imported
        Assert.Equal(importedJson, book.ReadPayload(SaveSlotBook.AutoSlotId)); // exactly the imported bytes
    }

    [Fact]
    public void BankCurrentFirst_ThenImport_KeepsBothTheOldAndNew()
    {
        // The import consent's "bank current first" escape: bank the running voyage to a free manual slot,
        // THEN let the imported file become the autosave. Both survive — the old is recoverable.
        var book = new SaveSlotBook(new MemStore());

        Vault current = VaultAt("the-space-bar", "The Rusty Roadstead", docked: true, simTime: 5_000);
        string currentJson = VaultSerializer.Save(current);
        book.Save(SaveSlotBook.AutoSlotId, currentJson, MetaFor(current, SaveSlotKind.Autosave, 10));

        // Escape: bank the current autosave state into manual slot 1.
        book.Save(SaveSlotBook.ManualSlotId(1), currentJson, MetaFor(current, SaveSlotKind.Manual, 11));

        // Then import replaces the autosave with the new voyage.
        Vault imported = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(imported), MetaFor(imported, SaveSlotKind.Autosave, 12));

        // The banked old voyage is intact and loadable; the autosave is the imported one.
        Assert.Equal(currentJson, book.ReadPayload(SaveSlotBook.ManualSlotId(1)));
        Assert.Equal("The Rusty Roadstead", book.Get(SaveSlotBook.ManualSlotId(1))!.Where);
        Assert.Equal("The Tilt", book.Newest()!.Where);
    }

    [Fact]
    public void List_OnATickTie_OrdersAutosaveFirst_ThenManualById()
    {
        var book = new SaveSlotBook(new MemStore());
        Vault v = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 0);
        book.Save(SaveSlotBook.ManualSlotId(2), VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Manual, 1));
        book.Save(SaveSlotBook.ManualSlotId(1), VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Manual, 1));
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Autosave, 1));

        // On an EXACT tick tie the tie-break is autosave-first, then id — the same tie-break Newest uses.
        var ids = book.List().Select(s => s.Id).ToList();
        Assert.Equal([SaveSlotBook.AutoSlotId, "1", "2"], ids);
    }

    // ── #312: the ordering law. The owner's Tilt autosave was NOT the top row because #311's List()
    //    ordered by KIND (autosave always first) then id — a fixed order that ignored the monotonic tick,
    //    so it disagreed with Newest() (tick-based) whenever the newest save wasn't the autosave. List()
    //    now sorts NEWEST FIRST by tick, so row 1 is always exactly what Continue resumes. ──

    [Fact]
    public void List_SortsNewestFirst_ByTick_EvenAManualBankAboveAnOlderAutosave()
    {
        var book = new SaveSlotBook(new MemStore());

        // An autosave at Mars (older), then a deliberate manual bank at The Tilt (newer tick).
        Vault mars = VaultAt("the-space-bar", "The Rusty Roadstead", docked: true, simTime: 0);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(mars), MetaFor(mars, SaveSlotKind.Autosave, ticks: 10));
        Vault tilt = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        book.Save(SaveSlotBook.ManualSlotId(3), VaultSerializer.Save(tilt), MetaFor(tilt, SaveSlotKind.Manual, ticks: 20));

        var rows = book.List();
        Assert.Equal("3", rows[0].Id);              // the newer manual bank sits ABOVE the older autosave
        Assert.Equal("The Tilt", rows[0].Where);
        Assert.Equal(SaveSlotBook.AutoSlotId, rows[1].Id);
    }

    [Fact]
    public void RowOne_AlwaysEqualsContinueTarget_AfterEverySaveImportOrAutosaveEvent()
    {
        var book = new SaveSlotBook(new MemStore());

        void AssertRowOneIsContinue()
        {
            SaveSlotMeta? cont = book.Newest();
            Assert.NotNull(cont);
            Assert.Equal(cont!.Id, book.List()[0].Id); // the Continue headline and list row 1 are one save
        }

        // Autosave rolls in.
        Vault a = VaultAt("the-space-bar", "The Rusty Roadstead", docked: true, simTime: 0);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(a), MetaFor(a, SaveSlotKind.Autosave, 10));
        AssertRowOneIsContinue();

        // A manual bank (newer) — row 1 must move to it.
        Vault b = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        book.Save(SaveSlotBook.ManualSlotId(1), VaultSerializer.Save(b), MetaFor(b, SaveSlotKind.Manual, 20));
        AssertRowOneIsContinue();
        Assert.Equal("1", book.List()[0].Id);

        // The autosave rolls forward again (newest) — row 1 returns to the autosave.
        Vault c = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_700_000);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(c), MetaFor(c, SaveSlotKind.Autosave, 30));
        AssertRowOneIsContinue();
        Assert.Equal(SaveSlotBook.AutoSlotId, book.List()[0].Id);

        // An import banked into a berth (newest by the moment it was filed) — row 1 tracks it too.
        Vault d = VaultAt("cinder-roost", "Cinder Roost", docked: true, simTime: 500_000);
        book.Save(SaveSlotBook.ManualSlotId(5), VaultSerializer.Save(d), MetaFor(d, SaveSlotKind.Manual, 40));
        AssertRowOneIsContinue();
    }

    // ── #312: named exports. The filename carries the harbor, spun from the same label the slots show. ──

    [Fact]
    public void FileName_ForMeta_NamesThePlaceDayAndStamp()
    {
        var savedAt = new DateTimeOffset(2026, 7, 18, 10, 22, 0, TimeSpan.Zero);
        var meta = new SaveSlotMeta
        {
            Where = "The Tilt",
            SimDay = 34,
            SavedRealTicks = savedAt.UtcTicks,
        };

        Assert.Equal("spacesails-the-tilt-day34-2026-07-18-1022.json", SaveFileNames.ForMeta(meta));
    }

    [Fact]
    public void FileName_Slug_IsFilesystemSafe_AndCollapsesPunctuation()
    {
        Assert.Equal("the-tilt", SaveFileNames.Slug("The Tilt"));
        Assert.Equal("the-rusty-roadstead", SaveFileNames.Slug("The Rusty Roadstead"));
        Assert.Equal("adrift-near-the-tilt", SaveFileNames.Slug("adrift near The Tilt"));
        Assert.Equal("unknown-waters", SaveFileNames.Slug("unknown waters"));
        Assert.Equal("cinder-roost", SaveFileNames.Slug("  Cinder — Roost!!  ")); // trims + collapses
        Assert.Equal("voyage", SaveFileNames.Slug(""));                            // empty → a safe fallback
        Assert.Equal("voyage", SaveFileNames.Slug("＊＊＊"));                        // all-exotic → fallback
    }

    [Fact]
    public void FileName_FromASlotsOwnLabel_NamesThatSlotsState_NotTheLiveMoment()
    {
        // Per-slot export names the SLOT's harbor: a slot banked adrift near Uranus on day 12 names itself.
        var savedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero);
        Vault v = VaultAt("the-tilt", "The Tilt", docked: false, simTime: 12 * 86400.0);
        SaveSlotMeta meta = MetaFor(v, SaveSlotKind.Manual, savedAt.UtcTicks);

        Assert.Equal("spacesails-adrift-near-the-tilt-day12-2026-01-02-0304.json", SaveFileNames.ForMeta(meta));
    }

    // ── #312: the import preview label is read FROM THE FILE'S CONTENTS, never the filename. ──

    [Fact]
    public void ImportPreview_LabelComesFromFileContent_NotTheFilename()
    {
        // A vault captured docked at The Tilt on sim day 30 — however the file on disk is (re)named.
        Vault onDisk = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        string bytes = VaultSerializer.Save(onDisk);

        // The preview parses the bytes and reads the label straight from them.
        SaveSlotMeta preview = SaveSlotLabels.PreviewMeta(VaultSerializer.Load(bytes));

        Assert.Equal("The Tilt", preview.Where);
        Assert.True(preview.WasDocked);
        Assert.Equal((int)(2_600_000 / 86400), preview.SimDay);
        // The portable file carries no real-time/build stamp — the preview honestly leaves those blank.
        Assert.Equal("", preview.RealTimeLabel);
        Assert.Equal("", preview.BuildStamp);
    }

    [Fact]
    public void ImportPreview_AdriftOrUnknown_AndCarriesTheTamperedMark()
    {
        Assert.Equal("adrift near The Tilt",
            SaveSlotLabels.PreviewMeta(VaultAt("the-tilt", "The Tilt", docked: false, 0)).Where);
        Assert.Equal("unknown waters", SaveSlotLabels.PreviewMeta(new Vault()).Where);
        Assert.False(SaveSlotLabels.PreviewMeta(new Vault()).Tampered);
    }

    // ── #312: import a file INTO a berth banks it there WITHOUT boarding — the live state is untouched. ──

    [Fact]
    public void ImportIntoBerth_BanksTheFile_WithoutTouchingTheLiveAutosave()
    {
        var book = new SaveSlotBook(new MemStore());

        // The live game is at Mars (the rolling autosave).
        Vault live = VaultAt("the-space-bar", "The Rusty Roadstead", docked: true, simTime: 5_000);
        string liveJson = VaultSerializer.Save(live);
        book.Save(SaveSlotBook.AutoSlotId, liveJson, MetaFor(live, SaveSlotKind.Autosave, 10));

        // A rescued Downloads file is banked INTO manual berth 5 — modelled as a Save to that slot.
        Vault rescued = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        string rescuedJson = VaultSerializer.Save(rescued);
        book.Save(SaveSlotBook.ManualSlotId(5), rescuedJson, MetaFor(rescued, SaveSlotKind.Manual, 11));

        // The live autosave is byte-for-byte unchanged — the import did NOT board.
        Assert.Equal(liveJson, book.ReadPayload(SaveSlotBook.AutoSlotId));
        Assert.Equal("The Rusty Roadstead", book.Get(SaveSlotBook.AutoSlotId)!.Where);
        // And the berth now holds exactly the rescued file's bytes.
        Assert.Equal(rescuedJson, book.ReadPayload(SaveSlotBook.ManualSlotId(5)));
        Assert.Equal("The Tilt", book.Get(SaveSlotBook.ManualSlotId(5))!.Where);
    }

    // ── #312: the rack renders all ten slots (autosave + nine berths), empty ones as empty berths. ──

    [Fact]
    public void Rack_IsAllTenSlots_WithTheRightOccupancy()
    {
        // The whole rack the UI renders: the autosave id plus manual ids "1".."9".
        string[] allSlotIds =
            [SaveSlotBook.AutoSlotId, .. Enumerable.Range(1, SaveSlotBook.ManualSlotCount).Select(SaveSlotBook.ManualSlotId)];
        Assert.Equal(10, allSlotIds.Length);

        var book = new SaveSlotBook(new MemStore());
        Vault v = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 0);
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Autosave, 30));
        book.Save(SaveSlotBook.ManualSlotId(2), VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Manual, 20));

        var occupied = book.List().Select(s => s.Id).ToHashSet();
        var empty = allSlotIds.Where(id => !occupied.Contains(id)).ToList();

        Assert.Equal(2, occupied.Count);                       // autosave + berth 2 occupied
        Assert.Equal(8, empty.Count);                          // the other eight berths are empty
        Assert.Equal(10, occupied.Count + empty.Count);        // every slot is accounted for, none twice
        Assert.Contains(SaveSlotBook.ManualSlotId(9), empty);  // berth 9 renders as an empty berth
    }
}
