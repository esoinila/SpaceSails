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
            LastCheckedPeriod = 3, // #223: the discovery watch's bookmark rides with the hoard
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
        Progress = new ProgressSection
        {
            TutorialPlayed = true,
            SecretLabsFound = ["phobos", "the-hermits-rock"],
            OddBooksRead = ["the-travels", "the-fat-paperback"],
            // #677 — the disclosure clock's register. Two grounds, opened in two different world-side
            // windows, because a register that only ever held one row would round-trip a shape the game
            // cannot produce and would say nothing about the window travelling with the ground.
            HallsOpened = [new HallOpeningRecord("phobos", 0), new HallOpeningRecord("miranda", 17)],
            // #1063 — …and which of those grounds the neighbours have since filled in. ONE of the two, so
            // the round trip carries a register that is a SUBSET rather than a copy of the one above it: a
            // file where every opened ground is also a buried one would round-trip a shape the game reaches
            // only at the very end and would say nothing about the two lists being independent.
            HallsBuried = ["miranda"],
            // #1068 — …and which of them the world has since declined on, with the window each declined
            // in. The OTHER of the two, so the three registers here are three different subsets rather
            // than one list said three times: a file where the buried ground and the declined ground were
            // the same row would round-trip nothing about them being independent facts.
            HallsDeclined = [new HallDeclineRecord("phobos", 4)],
            // #1068 — …and which of them the harbour has done its paperwork about, with the window and with
            // the berth already handed over. BerthGiven is TRUE on purpose: false is what a dropped field
            // decays to, so a round trip that only ever carried false would prove nothing about the one flag
            // standing between "the berth moved once" and "the berth moves every time you reload".
            HallsHandled = [new QuietHandRecord("miranda", 9, BerthGiven: true)],
            // #1074 — …and which of them the Authority has since closed the deep working of. The SAME row
            // as the declined one, deliberately: a stop and a decline are two different things that can be
            // true of one ground at once (they are different channels), and a file where the three lists
            // never overlapped would round-trip a shape that says nothing about them being independent.
            // What may never share a row is a stop and a BURIAL, and that is a law about the world rather
            // than about this format — TheStopOrderAtTheDigTests holds it.
            HallsStopped = ["phobos"],
            // #1074 beat 2 — and which of THOSE closed workings have since been fenced, signed and put under
            // study. The same id as the stopped one, deliberately and unavoidably: a zone stands on a closed
            // working and nowhere else, so a file whose two lists did not overlap would round-trip a shape
            // the world cannot produce.
            HallsPreserved = ["phobos"],
        },
        Nerve = new NerveSection { Nerve = 42.5, MonolithSeen = true },
        Overheard = new OverheardSection
        {
            Lines =
            [
                new OverheardLine("“Prices run soft on ice at the next berth.”", 12345.0, "GILT-EYE", "THE RINGSIDE BAR"),
                new OverheardLine("“A ghost runs dark past the rings this watch.”", 12360.5, "THE MAGPIE", "THE RINGSIDE BAR"),
            ],
        },
        Authorities = new AuthoritiesSection { Cards = ["luna#1", "titan#3"] },   // #590
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
        Assert.Equal(3, loaded.Caches.LastCheckedPeriod); // #223 — the watch survives the file, not just the session
        Assert.True(loaded.Caches.Caches[0].Cargo[0].Hot);
        Assert.Equal("cacheId", loaded.Quests!.Quests[0].Fields.Keys.First());
        Assert.Single(loaded.Quests.Obligations);
        Assert.Equal(3, loaded.Cargo!.Hot[0].HotUnits);
        Assert.True(loaded.Progress!.TutorialPlayed); // #292 — the onboarding bit rides the vault losslessly
        Assert.Equal(["phobos", "the-hermits-rock"], loaded.Progress.SecretLabsFound); // #409 — found labs persist per thread
        // #701 — and the shelves whose gist the casebook already carries, so a reload never re-files one
        Assert.Equal(["the-travels", "the-fat-paperback"], loaded.Progress.OddBooksRead);
        // #677 — and the disclosure clock's register, WITH the window each ground was opened in. A clock
        // that forgot across a reload would reset every threshold written against it, silently.
        Assert.Equal(
            [new HallOpeningRecord("phobos", 0), new HallOpeningRecord("miranda", 17)],
            loaded.Progress.HallsOpened);
        // #1063 — and which of them were filled in. A burial that forgot across a reload would put a set of
        // galleries back under a site the captain's own field book says are gone, and the book is the only
        // witness there is.
        Assert.Equal(["miranda"], loaded.Progress.HallsBuried);
        // #1068 — and which of them the world declined on, WITH the window. The window is what the door is
        // chosen against, so a save that dropped it would re-open the shut leaf and shut a different one:
        // a lock that moved by itself, which is the one reading a declined door may never have.
        Assert.Equal([new HallDeclineRecord("phobos", 4)], loaded.Progress.HallsDeclined);
        // #1068 — and which of them the harbour filed, with the window and the spent berth. The spent flag is
        // the load-bearing half: a reassignment that came back on every reload would be the one farmable
        // shape #672's channel is written to avoid, and nothing on screen would ever say so.
        Assert.Equal([new QuietHandRecord("miranda", 9, true)], loaded.Progress.HallsHandled);
        // #1074 — and which of them an office closed the working of. A closure that forgot across a reload
        // would re-open a shaft an office had sealed, and would let the neighbours fill in a ground the
        // Authority had already taken.
        Assert.Equal(["phobos"], loaded.Progress.HallsStopped);
        // #1074 beat 2 — and which of those have since passed into official care. Nothing ever takes a site
        // back OUT of care, so a reload that dropped this list would be the one mechanical fact of the beat
        // going missing: the study would end, which is the thing it does not do.
        Assert.Equal(["phobos"], loaded.Progress.HallsPreserved);
        Assert.Equal(42.5, loaded.Nerve!.Nerve, 6);   // #317 — a captain who fled shaking is still shaking
        Assert.True(loaded.Nerve.MonolithSeen);        //        and the monolith's first-sight hit stays spent
        Assert.Equal(["luna#1", "titan#3"], loaded.Authorities!.Cards);   // #590 — the wallet
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
    [InlineData("nerve")]
    [InlineData("overheard")]
    [InlineData("authorities")]
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
            "authorities" => new Vault { Authorities = full.Authorities },
            "upgrades" => new Vault { Upgrades = full.Upgrades },
            "diceItems" => new Vault { DiceItems = full.DiceItems },
            "progress" => new Vault { Progress = full.Progress },
            "nerve" => new Vault { Nerve = full.Nerve },
            "overheard" => new Vault { Overheard = full.Overheard },
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
    public void Nerve_SurvivesTheVaultRoundTrip_StillShakingAfterReload()
    {
        // #317 — the nerve gauge persists losslessly: a captain who fled a moon shaking must still be
        // shaking on reload (the ease-off is time aboard, never the load), and the monolith's first-sight
        // hit stays spent so a revisit never re-fires the big Lovecraftian shock.
        var vault = new Vault { Nerve = new NerveSection { Nerve = 17.25, MonolithSeen = true } };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));

        Assert.NotNull(loaded.Nerve);
        Assert.Equal(17.25, loaded.Nerve!.Nerve, 6);
        Assert.True(loaded.Nerve.MonolithSeen);
        Assert.False(loaded.Tampered);
    }

    [Fact]
    public void Overheard_SurvivesTheVaultRoundTrip_TheWordsYouPaidForDoNotVanish()
    {
        // Owner 2026-07-18: bar intel "may not hide" and must not "autodisappear" — so an overheard tip is
        // a durable record that round-trips. A received tip → an entry that is still there after a reload.
        IReadOnlyList<OverheardLine> log = OverheardLog.Append(
            [], new OverheardLine("“The collectors swept Ringside yesterday.”", 999.0, "CASS", "THE RINGSIDE BAR"));
        var vault = new Vault { Overheard = new OverheardSection { Lines = log } };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.NotNull(loaded.Overheard);
        Assert.Single(loaded.Overheard!.Lines);
        Assert.Equal("“The collectors swept Ringside yesterday.”", loaded.Overheard.Lines[0].Text);
        Assert.Equal("CASS", loaded.Overheard.Lines[0].Source);
        Assert.Equal("THE RINGSIDE BAR", loaded.Overheard.Lines[0].BarName);
        Assert.Equal(999.0, loaded.Overheard.Lines[0].SimTime, 6);
    }

    [Fact]
    public void Authorities_SurviveTheVaultRoundTrip_ACardIsAPossessionNotAMood()
    {
        // #590 · A card is found eleven floors under a moon. It has to still be in the pocket a month and a
        // world later, or the gate that reads it is a mood rather than a mechanic. The save carries the ID
        // and nothing else, so this round trip IS the persistence contract.
        var wallet = new AuthoritiesSection
        {
            Cards =
            [
                new UndergroundComplex.AuthorityCard("luna", 1).Id,
                new UndergroundComplex.AuthorityCard("titan", 3).Id,
            ],
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(new Vault { Authorities = wallet }));
        Assert.NotNull(loaded.Authorities);
        Assert.Equal(["luna#1", "titan#3"], loaded.Authorities!.Cards);

        // And it reads back as the thing it authorises, not as a string somebody has to interpret.
        Assert.True(UndergroundComplex.AuthorityCard.TryParse(
            loaded.Authorities.Cards[1], out UndergroundComplex.AuthorityCard back));
        Assert.Equal(new UndergroundComplex.AuthorityCard("titan", 3), back);
    }

    [Fact]
    public void Authorities_MissingSection_DefaultsToAnEmptyWallet()
    {
        // A pre-#590 save simply lacks the section, and a captain who has never been down a shaft is a
        // captain carrying nothing. It must never be a load failure.
        Assert.Empty(new AuthoritiesSection().Cards);
        Assert.Null(VaultSerializer.Load(VaultSerializer.Save(new Vault())).Authorities);
    }

    [Fact]
    public void Overheard_MissingSection_DefaultsToAnEmptyBook()
    {
        Assert.Empty(new OverheardSection().Lines);
    }

    [Fact]
    public void Nerve_MissingSection_DefaultsToACalmGauge()
    {
        // A pre-#317 file carries no nerve section: the reader returns null and the game defaults a full,
        // calm gauge. (Proven here at the contract layer: a section that IS present but omits the field
        // defaults its Nerve to Max, not to a bare-double 0 = "nerves shot".)
        var section = new NerveSection();
        Assert.Equal(NerveModel.Max, section.Nerve, 6);
        Assert.False(section.MonolithSeen);
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

    /// <summary>#223 · A voyage saved BEFORE the discovery watch rode the vault has a caches section with
    /// no bookmark field at all. It must read back as WATCH NOT STARTED (−1) so the client re-seeds it at
    /// the load clock — never as day 0, which would resolve every day since the epoch on the first frame
    /// and empty the captain's hoard the instant they resumed.</summary>
    [Fact]
    public void Caches_LegacyFileWithNoWatchField_ReadsAsWatchNotStarted()
    {
        string json = VaultSerializer.Save(FullVault());
        JsonObject root = (JsonObject)JsonNode.Parse(json)!;
        JsonObject caches = (JsonObject)((JsonObject)root["sections"]!)["caches"]!;
        Assert.True(caches.Remove("lastCheckedPeriod")); // the old shape: the field never existed
        RestampChecksum(root);

        Vault loaded = VaultSerializer.Load(root.ToJsonString());

        Assert.False(loaded.Tampered);
        Assert.Equal(CacheLedger.WatchNotStarted, loaded.Caches!.LastCheckedPeriod);
    }

    /// <summary>#715 · A voyage saved BEFORE the illegal-heat meter existed has contact rows with no heat
    /// fields at all. They must read back as <b>nobody remembers you</b> — zero owed, no clock running — and
    /// not as a captain who is already burned with every outfit in the game on the frame they resume.
    ///
    /// <para><b>Proven RED</b> by breaking the fallback — giving <c>ContactRecord.HeatOwed</c> a non-zero
    /// initialiser, which is what any "sensible default" on a save-shape field does to every file already
    /// written:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 0
    /// Actual:   3
    /// </code></summary>
    [Fact]
    public void Contacts_LegacyFileWithNoHeatFields_ReadsAsNobodyRemembersYou()
    {
        string json = VaultSerializer.Save(FullVault());
        JsonObject root = (JsonObject)JsonNode.Parse(json)!;
        JsonObject contacts = (JsonObject)((JsonObject)root["sections"]!)["contacts"]!;
        JsonArray rows = (JsonArray)contacts["contacts"]!;
        Assert.NotEmpty(rows);
        foreach (JsonNode? row in rows)
        {
            JsonObject r = (JsonObject)row!;
            r.Remove("heatOwed");            // the old shape: neither field ever existed
            r.Remove("heatStampSimTime");
        }
        RestampChecksum(root);

        Vault loaded = VaultSerializer.Load(root.ToJsonString());

        Assert.False(loaded.Tampered);
        var ledger = new ContactLedger();
        VaultMapper.Apply(loaded.Contacts, ledger);
        Assert.NotEmpty(ledger.Entries);
        foreach (ContactHistory h in ledger.Entries.Values)
        {
            Assert.Equal(0, h.HeatOwed);
            Assert.Equal(0.0, h.HeatStampSimTime);
        }
        foreach (SiteOperator.Operator op in SiteOperator.All)
        {
            Assert.Equal(0, IllegalHeat.HeatAt(ledger, op.Id));
        }
    }

    /// <summary>#715 · …and a heat that WAS banked survives the round trip, for the reason the ship's does
    /// (this file's own header): a restart that cleansed it would be a heat-cleanse exploit with a company on
    /// the other end of it.</summary>
    [Fact]
    public void Contacts_IllegalHeat_RoundTrips()
    {
        var written = new ContactLedger();
        written.ApplyHeat(IllegalHeat.LedgerId("meridian"), "MERIDIAN WORKS COMPANY", 5, 1234.5);

        var read = new ContactLedger();
        VaultMapper.Apply(
            VaultSerializer.Load(
                VaultSerializer.Save(new Vault { Contacts = VaultMapper.ToSection(written) })).Contacts,
            read);

        Assert.Equal(5, IllegalHeat.HeatAt(read, "meridian"));
        Assert.Equal(1234.5, read.For(IllegalHeat.LedgerId("meridian")).HeatStampSimTime);
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
