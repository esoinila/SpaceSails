using System.Text.Json;
using System.Text.Json.Nodes;

namespace SpaceSails.Core.Tests;

/// <summary>
/// The personal vault (#225): versioned, field-tolerant JSON + a salted checksum. These tests pin the
/// two forever-promises — tolerance BOTH directions, and a checksum that is an honesty speed-bump (a
/// mismatch marks the ledger but never refuses the load) — plus a lossless round-trip of every section.
/// </summary>
public class VaultSerializerTests
{
    private static Vault FullVault() => new()
    {
        Version = Vault.CurrentVersion,
        SavedSimTime = 123456.75,
        Purse = new PurseSection(4200),
        Ship = new ShipSection { ReactionMassPulses = 180.5, SlugAmmo = 12, MissileAmmo = 2 },
        Cargo = new CargoSection(
            [new CargoLine("He3", 6), new CargoLine("Ice", 4)],
            [new HotCargoLine("He3", 3)]),
        Heat = new HeatSection(2, 100000.0),
        Contacts = new ContactsSection(
        [
            new ContactRecord
            {
                ContactId = "madam-coil",
                DisplayName = "Madam Coil",
                MissionsCompleted = 3,
                TotalPaidCredits = 900,
                LastCompletedSimTime = 90000.0,
                Hostile = false,
                CreditBalance = 500,
                Transactions =
                [
                    new CreditTxnRecord((int)CreditKind.Deposit, 700, 80000, "parked a stake"),
                    new CreditTxnRecord((int)CreditKind.Withdrawal, -200, 90000, "drew some back"),
                ],
            },
        ]),
        Caches = new CachesSection
        {
            NextMintIndex = 5,
            Caches =
            [
                new CacheRecord
                {
                    Id = "cache-you-4",
                    BodyId = "phobos",
                    LandmarkName = "the monolith",
                    Bearing = "anti-spinward",
                    Paces = 40,
                    Coin = 1200,
                    Cargo = [new CacheCargoRecord("He3", 3, true)],
                    BuriedSimTime = 70000.0,
                    Owner = "you",
                    PlayerOwned = true,
                },
            ],
        },
        Quests = new QuestsSection
        {
            Quests =
            [
                new QuestRecord
                {
                    Id = "q-1",
                    Kind = "FetchCache",
                    Status = "PickedUp",
                    Title = "Fetch the Ghost's hoard",
                    Detail = "A map to someone else's chest.",
                    GiverContactId = "madam-coil",
                    RewardCredits = 800,
                    AcceptedSimTime = 60000,
                    Fields = new Dictionary<string, string> { ["cacheId"] = "cache-npc-2", ["paces"] = "40" },
                },
            ],
            Obligations =
            [
                new ObligationRecord("madam-coil", "Madam Coil", 300, 50000, "you owe her one quiet delivery"),
            ],
        },
        Insurance = new InsuranceSection((int)InsuranceTier.Premium, 200000.0),
        Upgrades = new UpgradesSection { MassLevel = 2, SensorLevel = 1, HoldLevel = 3, TelescopeLevel = 1 },
        DiceItems = new DiceItemsSection([new DiceItemRecord("boarding-nets", "Boarding nets", 2)]),
        Progress = new ProgressSection { TutorialPlayed = true },
        Resume = new ResumeSection { HavenId = "ringside", HavenName = "Ringside", WasDocked = true },
    };

    [Fact]
    public void FullEnvelope_RoundTrips_EverySection()
    {
        Vault original = FullVault();
        string json = VaultSerializer.Save(original);
        Vault loaded = VaultSerializer.Load(json);

        Assert.False(loaded.Tampered);
        Assert.Empty(loaded.Warnings);
        Assert.Equal(original.Version, loaded.Version);
        Assert.Equal(original.SavedSimTime, loaded.SavedSimTime);

        // The tightest fidelity check: re-saving the loaded vault reproduces the exact same file
        // (content AND checksum). Records with IReadOnlyList members don't compare element-wise, so
        // this canonical re-serialization is the honest round-trip assertion.
        Assert.Equal(json, VaultSerializer.Save(loaded));

        // Spot-check a few load-bearing values across nested collections all the same.
        Assert.Equal(4200, loaded.Purse!.Credits);
        Assert.Equal(2, loaded.Heat!.Level);
        Assert.Equal("madam-coil", loaded.Contacts!.Contacts[0].ContactId);
        Assert.Equal(500, loaded.Contacts.Contacts[0].CreditBalance);
        Assert.Equal(2, loaded.Contacts.Contacts[0].Transactions.Count);
        Assert.Equal(5, loaded.Caches!.NextMintIndex);
        Assert.True(loaded.Caches.Caches[0].Cargo[0].Hot);
        Assert.Equal("cacheId", loaded.Quests!.Quests[0].Fields.Keys.First());
        Assert.Single(loaded.Quests.Obligations);
        Assert.Equal(3, loaded.Cargo!.Hot[0].HotUnits);
        Assert.True(loaded.Progress!.TutorialPlayed); // #292 — the onboarding bit rides the vault losslessly
        Assert.True(loaded.Resume!.WasDocked);
    }

    // #255 — the vault round-trips a long-haul jump: the crossing writes the vault (personal life) with
    // SavedSimTime at the ARRIVAL epoch, a decade past when the heat was raised. The whole life survives the
    // round-trip, and because heat keys off an absolute checkpoint, reading it at the jumped clock decays a
    // decade's worth in one closed-form step — no per-tick replay, exactly as the live restore does.
    [Fact]
    public void JumpVault_RoundTrips_AndAppliesElapsedDecayOnRestore()
    {
        double raisedAt = 100.0 * 86400.0;
        double arrivalEpoch = raisedAt + 3655.0 * 86400.0; // a Mars->Uranus decade after the heat was raised

        var preJump = new Vault
        {
            Version = Vault.CurrentVersion,
            SavedSimTime = arrivalEpoch,
            Purse = new PurseSection(4200),
            Heat = new HeatSection(4, raisedAt),
            Contacts = new ContactsSection(
            [
                new ContactRecord { ContactId = "madam-coil", DisplayName = "Madam Coil", CreditBalance = 500 },
            ]),
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(preJump));

        // The personal life crossed the void intact.
        Assert.False(loaded.Tampered);
        Assert.Equal(arrivalEpoch, loaded.SavedSimTime);
        Assert.Equal(4200, loaded.Purse!.Credits);
        Assert.Equal("madam-coil", loaded.Contacts!.Contacts[0].ContactId);
        Assert.Equal(4, loaded.Heat!.Level);                 // stored level is untouched — the RULE decays it
        Assert.Equal(raisedAt, loaded.Heat.RaisedAtSimTime); // the absolute checkpoint survived the jump

        // Restore-time closed-form decay: read at the jumped clock and a decade of heat is gone at once.
        HeatState restored = EncounterRule.DecayHeat(
            new HeatState(loaded.Heat.Level, loaded.Heat.RaisedAtSimTime), loaded.SavedSimTime, atHavenOrbit: false);
        Assert.Equal(0, restored.Level);
    }

    [Theory]
    [InlineData("purse")]
    [InlineData("ship")]
    [InlineData("cargo")]
    [InlineData("heat")]
    [InlineData("contacts")]
    [InlineData("caches")]
    [InlineData("quests")]
    [InlineData("insurance")]
    [InlineData("upgrades")]
    [InlineData("diceItems")]
    [InlineData("progress")]
    [InlineData("resume")]
    public void EachSection_RoundTrips_Independently(string section)
    {
        // Build a vault carrying ONLY the one section, so the round-trip proves that section alone.
        Vault full = FullVault();
        Vault one = section switch
        {
            "purse" => new Vault { Purse = full.Purse },
            "ship" => new Vault { Ship = full.Ship },
            "cargo" => new Vault { Cargo = full.Cargo },
            "heat" => new Vault { Heat = full.Heat },
            "contacts" => new Vault { Contacts = full.Contacts },
            "caches" => new Vault { Caches = full.Caches },
            "quests" => new Vault { Quests = full.Quests },
            "insurance" => new Vault { Insurance = full.Insurance },
            "upgrades" => new Vault { Upgrades = full.Upgrades },
            "diceItems" => new Vault { DiceItems = full.DiceItems },
            "progress" => new Vault { Progress = full.Progress },
            "resume" => new Vault { Resume = full.Resume },
            _ => throw new ArgumentOutOfRangeException(nameof(section)),
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(one));
        Assert.False(loaded.Tampered);
        Assert.Empty(loaded.Warnings);
    }

    [Fact]
    public void Heat_Survives_IncludingTheNoneSentinel()
    {
        // HeatState.None carries double.NegativeInfinity — it must ride through the JSON unscathed so a
        // restart is never a heat-cleanse exploit and a "None" heat is still exactly None.
        var vault = new Vault { Heat = new HeatSection(0, double.NegativeInfinity) };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));

        Assert.NotNull(loaded.Heat);
        Assert.Equal(0, loaded.Heat!.Level);
        Assert.True(double.IsNegativeInfinity(loaded.Heat.RaisedAtSimTime));
        Assert.False(loaded.Tampered);
    }

    [Fact]
    public void ForwardCompat_UnknownJunkFields_SurviveAndAreIgnored()
    {
        // A file written by a NEWER game: extra unknown fields at the envelope, section, and record
        // levels. An old reader must load its readable parts and NOT flag tampering (the junk is part
        // of what the newer writer checksummed, and canonicalization preserves it on both ends).
        string json = VaultSerializer.Save(FullVault());
        JsonObject root = (JsonObject)JsonNode.Parse(json)!;

        // Inject junk at three levels, THEN recompute the checksum the way the newer writer would, so
        // the file is a legitimate forward-compatible save rather than an edit.
        root["futureTopLevelFlag"] = "someday";
        ((JsonObject)root["sections"]!)["someFutureSection"] = new JsonObject { ["x"] = 1 };
        ((JsonObject)((JsonObject)root["sections"]!)["purse"]!)["futureField"] = 999;
        RestampChecksum(root);

        Vault loaded = VaultSerializer.Load(root.ToJsonString());

        Assert.False(loaded.Tampered); // forward-compatible, not tampered
        Assert.Equal(4200, loaded.Purse!.Credits); // known field still read
        Assert.NotNull(loaded.Contacts); // other sections intact
    }

    [Fact]
    public void BackwardCompat_EnvelopeMissingWholeSections_LoadsRemainder()
    {
        // An OLD file that only ever knew about purse + contacts. The reader loads those two and
        // defaults everything else to absent — no throw, no tamper.
        var old = new Vault
        {
            SavedSimTime = 42,
            Purse = new PurseSection(1500),
            Contacts = new ContactsSection([new ContactRecord { ContactId = "fixer", DisplayName = "Fixer" }]),
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(old));

        Assert.False(loaded.Tampered);
        Assert.NotNull(loaded.Purse);
        Assert.NotNull(loaded.Contacts);
        Assert.Null(loaded.Ship);
        Assert.Null(loaded.Caches);
        Assert.Null(loaded.Resume);
    }

    [Fact]
    public void Checksum_DetectsAOneCharacterEdit_AndStillLoads()
    {
        // The give-self-money edit: bump the purse by one digit without re-hashing.
        string json = VaultSerializer.Save(new Vault { Purse = new PurseSection(1000) });
        JsonObject root = (JsonObject)JsonNode.Parse(json)!;
        ((JsonObject)((JsonObject)root["sections"]!)["purse"]!)["credits"] = 9000; // one-char class of edit
        // NOTE: deliberately do NOT restamp the checksum — that is the whole point.

        Vault loaded = VaultSerializer.Load(root.ToJsonString());

        Assert.True(loaded.Tampered); // caught
        Assert.Equal(9000, loaded.Purse!.Credits); // but loaded anyway — honesty speed-bump, not DRM
        Assert.Contains(loaded.Warnings, w => w.Contains("checksum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MissingChecksum_MarksTampered_ButLoads()
    {
        string json = VaultSerializer.Save(FullVault());
        JsonObject root = (JsonObject)JsonNode.Parse(json)!;
        root.Remove("checksum");

        Vault loaded = VaultSerializer.Load(root.ToJsonString());

        Assert.True(loaded.Tampered);
        Assert.Equal(4200, loaded.Purse!.Credits);
    }

    [Fact]
    public void CorruptSection_IsSkippedWithAWarning_OthersStillHarvested()
    {
        // One section is mangled into the wrong shape (a string where an object belongs). The harvest
        // must drop ONLY that section (with a warning) and still yield every other section.
        string json = VaultSerializer.Save(FullVault());
        JsonObject root = (JsonObject)JsonNode.Parse(json)!;
        ((JsonObject)root["sections"]!)["contacts"] = "totally not a contacts object";
        RestampChecksum(root); // it's a legitimate (if broken) file, not an edit — isolate the harvest

        Vault loaded = VaultSerializer.Load(root.ToJsonString());

        Assert.False(loaded.Tampered);
        Assert.Null(loaded.Contacts); // the broken one dropped
        Assert.Contains(loaded.Warnings, w => w.Contains("contacts", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(loaded.Purse); // the readable ones survived
        Assert.NotNull(loaded.Caches);
        Assert.Equal(4200, loaded.Purse!.Credits);
    }

    [Fact]
    public void Load_NonJson_ReturnsEmptyTamperedVault_NeverThrows()
    {
        Vault loaded = VaultSerializer.Load("this is not json {{{");
        Assert.True(loaded.Tampered);
        Assert.NotEmpty(loaded.Warnings);
        Assert.Null(loaded.Purse);
    }

    [Fact]
    public void UnknownEnumValue_SurvivesAsANumber_RatherThanFailing()
    {
        // A future CreditKind (int 99) written by a newer game must not break the contacts harvest.
        var vault = new Vault
        {
            Contacts = new ContactsSection(
            [
                new ContactRecord
                {
                    ContactId = "future",
                    DisplayName = "Future",
                    Transactions = [new CreditTxnRecord(99, 10, 1.0, "unknown kind")],
                },
            ]),
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.False(loaded.Tampered);
        Assert.Equal(99, loaded.Contacts!.Contacts[0].Transactions[0].Kind);
    }

    // Recompute + restamp the checksum so a deliberately-modified node reads as a legitimate save
    // (used to isolate tolerance behavior from tamper detection).
    private static void RestampChecksum(JsonObject root)
    {
        var payload = new JsonObject
        {
            ["version"] = root["version"]?.DeepClone(),
            ["savedSimTime"] = root["savedSimTime"]?.DeepClone(),
            ["sections"] = root["sections"]?.DeepClone(),
        };
        root["checksum"] = ChecksumFor(payload);
    }

    // Mirror of VaultSerializer's private checksum (salt + canonical sorted-key payload) so a test can
    // forge a valid forward-compatible file. If the production algorithm changes, this must too — which
    // is exactly the tripwire we want.
    private static string ChecksumFor(JsonNode payload)
    {
        JsonNode sorted = Sort(payload)!;
        string canonical = sorted.ToJsonString(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
        });
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("SpaceSails::personal-vault::v1::salt" + "\n" + canonical);
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    private static JsonNode? Sort(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var ordered = new JsonObject();
                foreach (var kv in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    ordered[kv.Key] = Sort(kv.Value);
                }

                return ordered;
            case JsonArray arr:
                var copy = new JsonArray();
                foreach (JsonNode? item in arr)
                {
                    copy.Add(Sort(item));
                }

                return copy;
            default:
                return node?.DeepClone();
        }
    }
}
