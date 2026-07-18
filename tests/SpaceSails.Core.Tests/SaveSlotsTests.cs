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
    public void List_OrdersAutosaveFirst_ThenManualById()
    {
        var book = new SaveSlotBook(new MemStore());
        Vault v = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 0);
        book.Save(SaveSlotBook.ManualSlotId(2), VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Manual, 1));
        book.Save(SaveSlotBook.ManualSlotId(1), VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Manual, 1));
        book.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Autosave, 1));

        var ids = book.List().Select(s => s.Id).ToList();
        Assert.Equal([SaveSlotBook.AutoSlotId, "1", "2"], ids);
    }
}
