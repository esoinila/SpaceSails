namespace SpaceSails.Core.Tests;

/// <summary>
/// Per-game state isolation (feat/game-threads, owner 2026-07-18: "different game starts don't share
/// state ... like have the roadster already found in a new game"). Covers the two Core seams the client
/// composes: the per-thread <see cref="SaveSlotBook"/> namespace (two universes never collide a slot, and
/// a whole shelf can be adopted losslessly into a thread) and the <see cref="GameThreadRegistry"/> index
/// (which threads exist, which is active, newest-first ordering). Plus the ledger wipes a new game needs.
/// </summary>
public class GameThreadsTests
{
    private sealed class MemStore : ISlotStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
        public string? Read(string key) => _map.TryGetValue(key, out string? v) ? v : null;
        public void Write(string key, string value) => _map[key] = value;
        public void Clear(string key) => _map.Remove(key);
        public int Count => _map.Count;
        public IEnumerable<string> Keys => _map.Keys;
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
        SavedRealTicks = ticks,
        Tampered = v.Tampered,
    };

    // ── The per-thread namespace: two universes, one store, no shared state ──

    [Fact]
    public void TwoThreads_ShareNoSlots_EvenUnderTheSameSlotId()
    {
        var store = new MemStore();
        var alpha = new SaveSlotBook(store, "alpha");
        var beta = new SaveSlotBook(store, "beta");

        Vault mars = VaultAt("space-bar", "The Rusty Roadstead", docked: true, simTime: 0);
        Vault tilt = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);

        alpha.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(mars), MetaFor(mars, SaveSlotKind.Autosave, 10));
        beta.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(tilt), MetaFor(tilt, SaveSlotKind.Autosave, 20));

        // Same slot id, different universes — each keeps its own truth.
        Assert.Equal("The Rusty Roadstead", alpha.Get(SaveSlotBook.AutoSlotId)!.Where);
        Assert.Equal("The Tilt", beta.Get(SaveSlotBook.AutoSlotId)!.Where);

        // Beta is a brand-new universe as far as ITS other slots go — nothing from alpha bleeds across.
        Assert.Null(beta.Get(SaveSlotBook.ManualSlotId(1)));
        Assert.Empty(new SaveSlotBook(store, "gamma").List()); // an untouched thread is empty
    }

    [Fact]
    public void DefaultBook_KeepsThePreThreadKeys_ForBackCompat()
    {
        var store = new MemStore();
        var legacyShelf = new SaveSlotBook(store); // empty keyspace == the pre-thread shelf
        Vault v = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 0);
        legacyShelf.Save(SaveSlotBook.AutoSlotId, VaultSerializer.Save(v), MetaFor(v, SaveSlotKind.Autosave, 1));

        Assert.Equal("", legacyShelf.ThreadId);
        Assert.Contains(SaveSlotBook.ManifestKey, store.Keys);                 // the exact old manifest key
        Assert.Contains(SaveSlotBook.PayloadPrefix + SaveSlotBook.AutoSlotId, store.Keys); // the exact old payload key
    }

    [Fact]
    public void CopyFrom_AdoptsAWholeShelf_Losslessly_IntoAThread()
    {
        var store = new MemStore();
        var shelf = new SaveSlotBook(store); // the pre-thread shelf a migration adopts
        Vault auto = VaultAt("the-tilt", "The Tilt", docked: true, simTime: 2_600_000);
        Vault bank = VaultAt("space-bar", "The Rusty Roadstead", docked: true, simTime: 0);
        string autoJson = VaultSerializer.Save(auto);
        string bankJson = VaultSerializer.Save(bank);
        shelf.Save(SaveSlotBook.AutoSlotId, autoJson, MetaFor(auto, SaveSlotKind.Autosave, 20));
        shelf.Save(SaveSlotBook.ManualSlotId(1), bankJson, MetaFor(bank, SaveSlotKind.Manual, 10));

        var thread = new SaveSlotBook(store, "adopted");
        thread.CopyFrom(shelf);

        // Byte-for-byte, both berths landed in the thread, labels intact.
        Assert.Equal(autoJson, thread.ReadPayload(SaveSlotBook.AutoSlotId));
        Assert.Equal(bankJson, thread.ReadPayload(SaveSlotBook.ManualSlotId(1)));
        Assert.Equal("The Tilt", thread.Get(SaveSlotBook.AutoSlotId)!.Where);
        Assert.Equal("The Rusty Roadstead", thread.Get(SaveSlotBook.ManualSlotId(1))!.Where);
        // The source shelf is untouched by the copy (non-destructive migration).
        Assert.Equal(autoJson, shelf.ReadPayload(SaveSlotBook.AutoSlotId));
    }

    // ── The registry: which universes exist, which is active, newest-first ──

    [Fact]
    public void Registry_IsEmpty_UntilAThreadIsRecorded()
    {
        var reg = new GameThreadRegistry(new MemStore());
        Assert.True(reg.IsEmpty);
        Assert.Null(reg.Newest());
        Assert.Null(reg.Active());
        Assert.Null(reg.ActiveId);
    }

    [Fact]
    public void Touch_UpsertsAndSetsActive_PreservingCreatedTick()
    {
        var reg = new GameThreadRegistry(new MemStore());
        reg.Touch("t1", "unknown waters", 0, ticks: 100);   // minted
        reg.Touch("t1", "The Tilt", 30, ticks: 250);        // first autosave

        GameThreadInfo? t = reg.Get("t1");
        Assert.NotNull(t);
        Assert.Equal("The Tilt", t!.Where);
        Assert.Equal(30, t.SimDay);
        Assert.Equal(250, t.LastActiveTicks);
        Assert.Equal(100, t.CreatedTicks); // born-on stamp survives the update
        Assert.Equal("t1", reg.ActiveId);
        Assert.False(reg.IsEmpty);
    }

    [Fact]
    public void Newest_And_List_OrderByLastActive_NewestFirst()
    {
        var reg = new GameThreadRegistry(new MemStore());
        reg.Touch("old", "The Rusty Roadstead", 0, ticks: 10);
        reg.Touch("mid", "Ringside Exchange", 5, ticks: 20);
        reg.Touch("new", "The Tilt", 40, ticks: 30);

        Assert.Equal("new", reg.Newest()!.Id);
        Assert.Equal(["new", "mid", "old"], reg.List().Select(t => t.Id));
    }

    [Fact]
    public void Active_PrefersExplicitActive_ButFallsBackToNewestIfDeleted()
    {
        var reg = new GameThreadRegistry(new MemStore());
        reg.Touch("a", "A", 0, ticks: 30); // newest by tick
        reg.Touch("b", "B", 0, ticks: 20);
        reg.SetActive("b");                 // deliberately continue the older one

        Assert.Equal("b", reg.Active()!.Id); // explicit active wins over newest-by-tick
        Assert.Equal("a", reg.Newest()!.Id); // ...without bumping b's clock

        reg.Remove("b");
        Assert.Equal("a", reg.Active()!.Id); // active gone → newest survives as the resume target
        Assert.Null(reg.Get("b"));
    }

    [Fact]
    public void SetActive_IgnoresUnknownThread()
    {
        var reg = new GameThreadRegistry(new MemStore());
        reg.Touch("real", "Somewhere", 0, ticks: 10);
        reg.SetActive("ghost"); // not recorded — must not become active
        Assert.Equal("real", reg.ActiveId);
    }

    [Fact]
    public void CorruptRegistry_ReadsAsEmpty_RatherThanThrowing()
    {
        var store = new MemStore();
        store.Write(GameThreadRegistry.RegistryKey, "{ this is not json ]");
        var reg = new GameThreadRegistry(store);
        Assert.True(reg.IsEmpty); // tolerant: the universes survive under their own keys; the index just resets
    }

    [Fact]
    public void Touch_MintsAStableCaptainIdentity_AndPreservesItAcrossTouches()
    {
        var reg = new GameThreadRegistry(new MemStore());
        reg.Touch("thread-guid-xyz", "unknown waters", 0, ticks: 100); // minted

        GameThreadInfo minted = reg.Get("thread-guid-xyz")!;
        Assert.False(string.IsNullOrWhiteSpace(minted.CaptainName));
        Assert.InRange(minted.AvatarIndex, 1, Captains.AvatarCount);
        // Seeded off the id — matches the pure generator exactly.
        Assert.Equal(Captains.Name("thread-guid-xyz"), minted.CaptainName);
        Assert.Equal(Captains.AvatarIndex("thread-guid-xyz"), minted.AvatarIndex);

        reg.Touch("thread-guid-xyz", "The Tilt", 30, ticks: 250); // first autosave — identity must survive
        GameThreadInfo played = reg.Get("thread-guid-xyz")!;
        Assert.Equal(minted.CaptainName, played.CaptainName);
        Assert.Equal(minted.AvatarIndex, played.AvatarIndex);
    }

    // ── The captains' roster: seeded identity + the grouping the front-door renders ──

    [Fact]
    public void Captains_AreDeterministic_AndAvatarIsInRange()
    {
        Assert.Equal(Captains.Name("abc"), Captains.Name("abc"));       // stable across calls
        Assert.NotEqual(Captains.Name("abc"), Captains.Name("xyz-999")); // different seeds → (very likely) different
        for (int i = 0; i < 50; i++)
        {
            int a = Captains.AvatarIndex($"seed-{i}");
            Assert.InRange(a, 1, Captains.AvatarCount);
        }

        Assert.StartsWith("Captain ", Captains.Name("anything"));
        Assert.Equal("Captain Nemo", Captains.Name("")); // blank seed → a stable fallback captain
    }

    [Fact]
    public void Captains_For_PrefersStoredIdentity_ElseDerivesFromId()
    {
        var stored = new GameThreadInfo { Id = "t", CaptainName = "Captain Ada Vex", AvatarIndex = 5 };
        Assert.Equal(("Captain Ada Vex", 5), Captains.For(stored));

        var thin = new GameThreadInfo { Id = "legacy-thread" }; // pre-roster: no stored identity
        (string Name, int AvatarIndex) resolved = Captains.For(thin);
        Assert.Equal(Captains.Name("legacy-thread"), resolved.Name);
        Assert.Equal(Captains.AvatarIndex("legacy-thread"), resolved.AvatarIndex);
    }

    [Fact]
    public void GroupSlots_ActiveFirst_ThenNewestThread_SlotsNewestWithinGroup()
    {
        var threads = new List<GameThreadInfo>
        {
            new() { Id = "old", Where = "The Rusty Roadstead", SimDay = 2, LastActiveTicks = 10 },
            new() { Id = "new", Where = "The Tilt", SimDay = 40, LastActiveTicks = 30 },
            new() { Id = "mid", Where = "Ringside Exchange", SimDay = 5, LastActiveTicks = 20 },
            new() { Id = "empty", Where = "nowhere", SimDay = 0, LastActiveTicks = 25 }, // no slots → dropped
        };

        var slots = new Dictionary<string, IReadOnlyList<SaveSlotMeta>>
        {
            ["old"] = [Meta("old", SaveSlotBook.ManualSlotId(1), SaveSlotKind.Manual, 5)],
            ["new"] = [Meta("new", SaveSlotBook.ManualSlotId(1), SaveSlotKind.Manual, 100)],
            ["mid"] =
            [
                Meta("mid", SaveSlotBook.ManualSlotId(2), SaveSlotKind.Manual, 40),
                Meta("mid", SaveSlotBook.AutoSlotId, SaveSlotKind.Autosave, 90), // newer → must lead its group
            ],
            ["empty"] = [],
        };

        IReadOnlyList<GameThreadGroup> groups =
            GameThreads.GroupSlots(threads, activeId: "old", tid => slots.GetValueOrDefault(tid, []));

        // The empty universe is dropped; the active captain ("old") heads the roster despite the lowest tick,
        // then the rest newest-thread-first (new @30 before mid @20).
        Assert.Equal(["old", "new", "mid"], groups.Select(g => g.Thread.Id));
        Assert.True(groups[0].IsActive);
        Assert.False(groups[1].IsActive);

        // Within the "mid" group the autosave (tick 90) sorts before the manual (tick 40).
        GameThreadGroup mid = groups.Single(g => g.Thread.Id == "mid");
        Assert.Equal([SaveSlotBook.AutoSlotId, SaveSlotBook.ManualSlotId(2)], mid.Slots.Select(s => s.Id));
    }

    [Fact]
    public void GroupSlots_NoActive_IsPureNewestThreadFirst()
    {
        var threads = new List<GameThreadInfo>
        {
            new() { Id = "a", LastActiveTicks = 10 },
            new() { Id = "b", LastActiveTicks = 30 },
        };
        var slots = new Dictionary<string, IReadOnlyList<SaveSlotMeta>>
        {
            ["a"] = [Meta("a", SaveSlotBook.AutoSlotId, SaveSlotKind.Autosave, 1)],
            ["b"] = [Meta("b", SaveSlotBook.AutoSlotId, SaveSlotKind.Autosave, 1)],
        };

        IReadOnlyList<GameThreadGroup> groups =
            GameThreads.GroupSlots(threads, activeId: null, tid => slots[tid]);

        Assert.Equal(["b", "a"], groups.Select(g => g.Thread.Id)); // newest thread first, none marked active
        Assert.DoesNotContain(groups, g => g.IsActive);
    }

    private static SaveSlotMeta Meta(string threadHint, string slotId, SaveSlotKind kind, long ticks) => new()
    {
        Id = slotId,
        Kind = kind,
        Where = threadHint,
        SavedRealTicks = ticks,
    };

    // ── The ledger wipes a new game needs (the roadster's actual home is the client's reveal set, but the
    //    hoard and the contact book are durable and must also reset per universe) ──

    [Fact]
    public void CacheLedger_Clear_EmptiesTheHoard_AndRewindsTheMint()
    {
        var ledger = new CacheLedger();
        ledger.Bury("mars", 500, [], simTime: 0, owner: "you", playerOwned: true);
        ledger.Bury("luna", 300, [], simTime: 0, owner: "you", playerOwned: true);
        Assert.Equal(2, ledger.Caches.Count);

        ledger.Clear();

        Assert.Empty(ledger.Caches);
        Assert.Equal(0, ledger.NextMintIndex()); // a fresh universe's first burial mints from zero
    }

    [Fact]
    public void ContactLedger_Clear_ForgetsEveryContact()
    {
        var ledger = new ContactLedger();
        ledger.RecordCompletion("fixer", "The Fixer", 1000, simTime: 0);
        Assert.NotEmpty(ledger.Entries);

        ledger.Clear();

        Assert.Empty(ledger.Entries);
        Assert.Equal(0, ledger.For("fixer").MissionsCompleted); // a blank slate, as if never met
    }
}
