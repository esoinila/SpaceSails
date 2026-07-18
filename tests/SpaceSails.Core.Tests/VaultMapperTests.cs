namespace SpaceSails.Core.Tests;

/// <summary>
/// The personal-vault mapper (#225): live Core records ↔ flat vault sections, lossless both ways, and
/// the pure resume-berth selector (docked vs in-flight nearest — the owner's law that a load always
/// wakes the pirate at a berth).
/// </summary>
public class VaultMapperTests
{
    [Fact]
    public void Contacts_RoundTrip_PreservesHistoryBalanceAndPassbook()
    {
        var ledger = new ContactLedger();
        ledger.RecordCompletion("madam-coil", "Madam Coil", 300, 10000);
        ledger.RecordCompletion("madam-coil", "Madam Coil", 600, 20000);
        ledger.ApplyCredit("madam-coil", "Madam Coil", new CreditTransaction(CreditKind.Deposit, 700, 21000, "stake"));
        ledger.ApplyCredit("madam-coil", "Madam Coil", new CreditTransaction(CreditKind.Withdrawal, -200, 22000, "drew"));
        ledger.RecordPlunder("rival", "The Rival", 30000);

        ContactsSection section = VaultMapper.ToSection(ledger);
        var restored = new ContactLedger();
        VaultMapper.Apply(section, restored);

        ContactHistory coil = restored.For("madam-coil");
        Assert.Equal(2, coil.MissionsCompleted);
        Assert.Equal(900, coil.TotalPaidCredits);
        Assert.Equal(20000, coil.LastCompletedSimTime);
        Assert.Equal(500, coil.CreditBalance); // 700 - 200
        Assert.Equal(2, coil.Transactions.Length);
        Assert.Equal(CreditKind.Deposit, coil.Transactions[0].Kind);
        Assert.Equal(CreditKind.Withdrawal, coil.Transactions[1].Kind);

        ContactHistory rival = restored.For("rival");
        Assert.True(rival.Hostile);

        // The book still foots after a round-trip: balance == Σ transactions.
        Assert.Equal(coil.Transactions.Sum(t => t.Amount), coil.CreditBalance);
    }

    [Fact]
    public void Caches_RoundTrip_PreservesCargoHotFlagsAndMintIndex()
    {
        var ledger = new CacheLedger();
        ledger.Bury("phobos", 1200, [new CacheCargo("He3", 3, true)], 70000, "you", playerOwned: true);
        ledger.Bury("deimos", 400, [], 71000, "you", playerOwned: true);
        ledger.Learn(new TreasureCache("cache-npc-9", "phobos", "the monolith", "sunward", 22, 500,
            [new CacheCargo("Ice", 2, false)], 60000, "the Ghost", false));
        int mintBefore = ledger.NextMintIndex();

        CachesSection section = VaultMapper.ToSection(ledger);
        var restored = new CacheLedger();
        VaultMapper.Apply(section, restored);

        Assert.Equal(ledger.Caches.Count, restored.Caches.Count);
        Assert.Equal(mintBefore, restored.NextMintIndex());
        Assert.Equal(ledger.Caches.Select(c => c.Id), restored.Caches.Select(c => c.Id)); // order preserved

        TreasureCache phobos = restored.Caches.Single(c => c.Id == "cache-you-0");
        Assert.Equal(1200, phobos.Coin);
        Assert.Single(phobos.Cargo);
        Assert.True(phobos.Cargo[0].Hot);

        // A freshly-buried chest after load must not collide ids with a loaded one: the restored mint
        // index picks up where the save left off, so the new id is unique in the ledger.
        var loadedIds = restored.Caches.Select(c => c.Id).ToHashSet();
        TreasureCache fresh = restored.Bury("io", 100, [], 72000, "you", true);
        Assert.DoesNotContain(fresh.Id, loadedIds);
        Assert.Equal(1, restored.Caches.Count(c => c.Id == fresh.Id));
    }

    [Fact]
    public void Caches_RoundTrip_PreservesFreeFormDigSpot()
    {
        // Beach-comber kit (playtest bug #5): a free-form bury records the REAL dug coords on the cache.
        // They must survive the vault round-trip so the ✗ reloads exactly where the shovel dug.
        var ledger = new CacheLedger();
        ledger.Bury("miranda", 500, [], 80000, "you", playerOwned: true, reeverLevel: 2, digX: -6.5, digY: -71.25);

        var restored = new CacheLedger();
        VaultMapper.Apply(VaultMapper.ToSection(ledger), restored);

        TreasureCache c = restored.Caches.Single();
        Assert.True(c.HasDigSpot);
        Assert.Equal(-6.5, c.DigX);
        Assert.Equal(-71.25, c.DigY);
    }

    [Fact]
    public void Caches_OldVaultWithoutDigSpot_FallsBackToHashPosition()
    {
        // Backward compat: a pre-beach-comber cache record has no DigX/DigY (both null). It must load with
        // HasDigSpot == false so the client falls back to the deterministic hash-scatter — a lossless
        // round-trip for every legacy save (no coords in, no coords out).
        var section = new CachesSection
        {
            NextMintIndex = 1,
            Caches =
            [
                new CacheRecord
                {
                    Id = "cache-you-0",
                    BodyId = "phobos",
                    LandmarkName = "the monolith",
                    Bearing = "sunward",
                    Paces = 40,
                    Coin = 900,
                    Owner = "you",
                    PlayerOwned = true,
                    ReeverLevel = 1,
                    // DigX / DigY deliberately unset — the old shape.
                },
            ],
        };

        var restored = new CacheLedger();
        VaultMapper.Apply(section, restored);

        TreasureCache c = restored.Caches.Single();
        Assert.Null(c.DigX);
        Assert.Null(c.DigY);
        Assert.False(c.HasDigSpot); // the client reads this and scatters the ✗ by hash instead
    }

    [Fact]
    public void HotCargo_RoundTrip_ViaLines()
    {
        var hot = new HotCargoLedger();
        hot.Stamp("He3", 6, heatAtTheft: 2);
        hot.Stamp("Ice", 3, heatAtTheft: 1);

        IReadOnlyList<HotCargoLine> lines = VaultMapper.ToHotLines(hot);
        var restored = new HotCargoLedger();
        VaultMapper.ApplyHot(lines, restored);

        Assert.Equal(6, restored.HotUnits("He3"));
        Assert.Equal(3, restored.HotUnits("Ice"));
        Assert.Equal(9, restored.TotalHotUnits);
    }

    [Fact]
    public void Insurance_RoundTrips_AndUninsuredDefaultsOnNull()
    {
        var policy = new PirateInsurance(InsuranceTier.Premium, 200000);
        InsuranceSection section = VaultMapper.ToSection(policy);
        Assert.Equal(policy, VaultMapper.ToInsurance(section));

        // A vault with no insurance section defaults to the uninsured rustbucket.
        Assert.Equal(PirateInsurance.Uninsured, VaultMapper.ToInsurance(null));
    }

    [Fact]
    public void Obligations_RoundTrip()
    {
        var obligations = new List<FavorObligation>
        {
            new("madam-coil", "Madam Coil", 300, 50000, "one quiet delivery"),
            new("fixer", "The Fixer", 800, 51000, "a package, no questions"),
        };

        IReadOnlyList<ObligationRecord> records = VaultMapper.ToRecords(obligations);
        IReadOnlyList<FavorObligation> restored = VaultMapper.ToObligations(records);

        Assert.Equal(obligations, restored);
        Assert.Empty(VaultMapper.ToObligations(null));
    }

    [Fact]
    public void Resume_WhenDocked_PicksThatBerth()
    {
        var havens = new List<VaultResume.HavenLocus>
        {
            new("ringside", "Ringside", new Vector2d(1000, 0)),
            new("space-bar", "The Space Bar", new Vector2d(0, 0)),
        };

        // Ship happens to be far from Ringside, but we're DOCKED there — the berth wins regardless.
        ResumeSection? resume = VaultResume.Select("ringside", new Vector2d(5000, 5000), havens);

        Assert.NotNull(resume);
        Assert.Equal("ringside", resume!.HavenId);
        Assert.Equal("Ringside", resume.HavenName);
        Assert.True(resume.WasDocked);
    }

    [Fact]
    public void Resume_WhenInFlight_PicksNearestDockableHaven()
    {
        var havens = new List<VaultResume.HavenLocus>
        {
            new("ringside", "Ringside", new Vector2d(1000, 0)),
            new("space-bar", "The Space Bar", new Vector2d(0, 0)),
            new("cinder-roost", "Cinder Roost", new Vector2d(10000, 0)),
        };

        // Not docked (null); ship sits nearest to the Space Bar.
        ResumeSection? resume = VaultResume.Select(dockedHavenId: null, new Vector2d(100, 50), havens);

        Assert.NotNull(resume);
        Assert.Equal("space-bar", resume!.HavenId);
        Assert.False(resume.WasDocked);
    }

    [Fact]
    public void Resume_WhenDockedHavenNoLongerExists_FallsBackToNearest()
    {
        var havens = new List<VaultResume.HavenLocus>
        {
            new("ringside", "Ringside", new Vector2d(1000, 0)),
        };

        ResumeSection? resume = VaultResume.Select("a-haven-from-another-scenario", new Vector2d(900, 0), havens);

        Assert.NotNull(resume);
        Assert.Equal("ringside", resume!.HavenId);
        Assert.False(resume.WasDocked);
    }

    [Fact]
    public void Resume_WithNoHavens_ReturnsNull()
    {
        Assert.Null(VaultResume.Select(null, Vector2d.Zero, []));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Resume_IdAndName_AlwaysCoRefer_AgainstRealSol(bool docked)
    {
        // #256: a real export had HavenId/HavenName resolved from two different lookups and could name
        // one bar while pointing at another. Select derives BOTH from one HavenLocus, so for every
        // dockable haven in the shipped scenario the resume's name must be the id's true name — no
        // resume can ever wake the captain at the wrong bar.
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        var havens = eph.Bodies
            .Where(b => b.IsHaven && b.Mu <= 0)
            .Select(b => new VaultResume.HavenLocus(b.Id, b.Name, eph.Position(b.Id, 0)))
            .ToList();
        Assert.NotEmpty(havens);

        foreach (VaultResume.HavenLocus h in havens)
        {
            ResumeSection? resume = VaultResume.Select(docked ? h.Id : null, h.Position, havens);
            Assert.NotNull(resume);
            string trueName = eph.Bodies.Single(b => b.Id == resume!.HavenId).Name;
            Assert.Equal(trueName, resume!.HavenName); // id and name co-refer, always
        }
    }

    [Fact]
    public void FullVaultBuiltFromLiveLedgers_SurvivesSerialization()
    {
        // End-to-end: live ledgers → sections → JSON → sections → live ledgers, all lossless.
        var contacts = new ContactLedger();
        contacts.ApplyCredit("coil", "Coil", new CreditTransaction(CreditKind.Deposit, 500, 1, "stake"));
        var caches = new CacheLedger();
        caches.Bury("phobos", 900, [new CacheCargo("He3", 2, true)], 2, "you", true);
        var hot = new HotCargoLedger();
        hot.Stamp("He3", 4, 2);

        var vault = new Vault
        {
            SavedSimTime = 999,
            Purse = new PurseSection(1500),
            Contacts = VaultMapper.ToSection(contacts),
            Caches = VaultMapper.ToSection(caches),
            Cargo = new CargoSection([new CargoLine("He3", 4)], VaultMapper.ToHotLines(hot)),
            Insurance = VaultMapper.ToSection(new PirateInsurance(InsuranceTier.Basic, 500)),
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.False(loaded.Tampered);

        var rebuiltContacts = new ContactLedger();
        VaultMapper.Apply(loaded.Contacts, rebuiltContacts);
        Assert.Equal(500, rebuiltContacts.For("coil").CreditBalance);

        var rebuiltCaches = new CacheLedger();
        VaultMapper.Apply(loaded.Caches, rebuiltCaches);
        Assert.Single(rebuiltCaches.Caches);
        Assert.True(rebuiltCaches.Caches[0].Cargo[0].Hot);

        var rebuiltHot = new HotCargoLedger();
        VaultMapper.ApplyHot(loaded.Cargo!.Hot, rebuiltHot);
        Assert.Equal(4, rebuiltHot.HotUnits("He3"));

        Assert.Equal(InsuranceTier.Basic, VaultMapper.ToInsurance(loaded.Insurance).Tier);
    }
}
