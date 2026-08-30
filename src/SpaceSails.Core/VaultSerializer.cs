using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SpaceSails.Core;

/// <summary>
/// Reads and writes the personal <see cref="Vault"/> as versioned JSON with a salted SHA-256
/// checksum. Two promises that must hold FOREVER:
///
/// <list type="number">
///   <item><b>Field-tolerant both directions.</b> Unknown fields are ignored, missing fields default,
///   and every section is harvested INDEPENDENTLY (per-section try/catch): a file whose <c>contacts</c>
///   section is corrupt still yields its <c>caches</c>, <c>purse</c>, etc. An old file missing whole
///   sections loads the sections it has; a newer file with extra junk loads its readable remainder.</item>
///   <item><b>The checksum is an honesty speed-bump, not DRM.</b> A mismatch NEVER refuses the load —
///   it loads anyway and sets <see cref="Vault.Tampered"/> so the game can say so plainly (the 📛
///   marker in the Captain's ledger). It exists only to make "give-self-money" file edits non-trivial;
///   burying/banking is the real economy, not this hash.</item>
/// </list>
///
/// The checksum is computed over the CANONICALIZED payload (every object key sorted, arrays left in
/// order, unknown fields preserved) so it is stable across property-order changes and so a
/// forward-compatible writer and this reader agree on the same bytes.
/// </summary>
public static class VaultSerializer
{
    // A fixed salt folded into the digest. Public repo, so this is not a secret and is not pretending
    // to be one — it just means a casual editor cannot recompute a valid checksum by pasting the file
    // into a plain sha256 box. Honesty speed-bump, by design (see the class summary).
    private const string ChecksumSalt = "SpaceSails::personal-vault::v1::salt";

    private static readonly JsonSerializerOptions WireOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // HeatState.RaisedAtSimTime can be double.NegativeInfinity (the "None" sentinel); allow the
        // named literals so it survives a round-trip instead of throwing.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = true,
    };

    // Compact, deterministic form used only for hashing (never written to disk).
    private static readonly JsonSerializerOptions HashOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = false,
    };

    // The canonical section order and names. Adding a name here is backward-compatible: old files
    // simply lack the key (harvested as null), old readers ignore a key they don't list.
    private const string SecPurse = "purse";
    private const string SecShip = "ship";
    private const string SecCargo = "cargo";
    private const string SecHeat = "heat";
    private const string SecContacts = "contacts";
    private const string SecCaches = "caches";
    private const string SecQuests = "quests";
    private const string SecInsurance = "insurance";
    private const string SecUpgrades = "upgrades";
    private const string SecDiceItems = "diceItems";
    private const string SecProgress = "progress";
    private const string SecNerve = "nerve";
    private const string SecOverheard = "overheard";
    private const string SecFieldNotes = "fieldnotes";   // #587 · the field book
    private const string SecCaseThreads = "casethreads"; // #741 · the red lines between entries
    private const string SecPapersShown = "papersshown"; // #836 · which name was given, and where
    private const string SecAuthorities = "authorities";  // #590 · the cards that run a shaft
    private const string SecFiling = "filing";            // #973 · the pages you don't remember writing
    private const string SecWalkIn = "walkin";            // #973 L5b · the walk-ins the SPREAD found out
    private const string SecOldCrew = "oldcrew";           // #973 L5a · the four who knew the old face
    private const string SecCrossings = "crossings";       // #973 L5a · ⚖ what he said of it, and who heard
    private const string SecHeldMemories = "heldmemories"; // #978 · the sheets that are not documents
    private const string SecWeather = "insuranceweather"; // #973 · what the bars say about the insurance men
    private const string SecSatchel = "satchel";          // #603 · everything carried on foot
    private const string SecWorkedUp = "workedup";        // #1016 · the sheets already dug out at a table
    private const string SecKaamos = "kaamos";
    private const string SecNebula = "nebula";
    private const string SecResume = "resume";
    private const string SecLogbook = "logbook";        // #948 · the captain's name, the title, the note

    /// <summary>Serialize a vault to its on-disk JSON string (envelope + checksum). Only non-null
    /// sections are written, so the file is exactly as large as the pirate's life is rich.</summary>
    public static string Save(Vault vault)
    {
        ArgumentNullException.ThrowIfNull(vault);

        var sections = new JsonObject();
        AddSection(sections, SecPurse, vault.Purse);
        AddSection(sections, SecShip, vault.Ship);
        AddSection(sections, SecCargo, vault.Cargo);
        AddSection(sections, SecHeat, vault.Heat);
        AddSection(sections, SecContacts, vault.Contacts);
        AddSection(sections, SecCaches, vault.Caches);
        AddSection(sections, SecQuests, vault.Quests);
        AddSection(sections, SecInsurance, vault.Insurance);
        AddSection(sections, SecUpgrades, vault.Upgrades);
        AddSection(sections, SecDiceItems, vault.DiceItems);
        AddSection(sections, SecProgress, vault.Progress);
        AddSection(sections, SecNerve, vault.Nerve);
        AddSection(sections, SecOverheard, vault.Overheard);
        AddSection(sections, SecFieldNotes, vault.FieldNotes);
        AddSection(sections, SecCaseThreads, vault.CaseThreads);
        AddSection(sections, SecPapersShown, vault.PapersShown);
        AddSection(sections, SecAuthorities, vault.Authorities);
        AddSection(sections, SecFiling, vault.Filing);
        AddSection(sections, SecWalkIn, vault.WalkIn);
        AddSection(sections, SecOldCrew, vault.OldCrew);
        AddSection(sections, SecCrossings, vault.Crossings);
        AddSection(sections, SecHeldMemories, vault.HeldMemories);
        AddSection(sections, SecWeather, vault.InsuranceWeather);
        AddSection(sections, SecSatchel, vault.Satchel);
        AddSection(sections, SecWorkedUp, vault.WorkedUp);
        AddSection(sections, SecKaamos, vault.Kaamos);
        AddSection(sections, SecNebula, vault.Nebula);
        AddSection(sections, SecResume, vault.Resume);
        AddSection(sections, SecLogbook, vault.Logbook);

        // Build the payload (everything the checksum protects), hash it, THEN stamp the checksum in.
        var envelope = new JsonObject
        {
            ["version"] = vault.Version,
            ["savedSimTime"] = vault.SavedSimTime,
            ["sections"] = sections,
        };

        string checksum = Checksum(Canonicalize(envelope));
        envelope["checksum"] = checksum;

        return envelope.ToJsonString(WireOptions);
    }

    /// <summary>
    /// #948 · WRITE THE PAGE ONTO A SAVE THAT IS ALREADY BANKED — and touch nothing else.
    ///
    /// <para>Editing a slot's title or note must not re-serialize the voyage. A <see cref="Load"/> then
    /// <see cref="Save"/> round-trip would silently DROP any section this build does not know about (the
    /// harvest is per-section and by name), so a forward-compatible file edited by an older build would come
    /// back poorer than it went in. This is JSON surgery instead: parse the stored bytes, replace exactly the
    /// <c>logbook</c> section, recompute the checksum over the payload as it now stands, and re-emit. Every
    /// other section — known, unknown or unreadable — passes through untouched, and the file stays honest
    /// (its checksum still validates, so a retitled save is not marked tampered).</para>
    ///
    /// <para>Returns the stored text UNCHANGED if it cannot be parsed as a save envelope — an unreadable
    /// file is not made worse by a rename.</para>
    /// </summary>
    public static string StampLogbook(string json, LogbookSection logbook)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        ArgumentNullException.ThrowIfNull(logbook);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return json; // not JSON at all — leave the bytes exactly as they were
        }

        if (root is not JsonObject rootObj)
        {
            return json;
        }

        JsonObject? sections = FindObject(rootObj, "sections");
        if (sections is null)
        {
            sections = new JsonObject();
            rootObj["sections"] = sections;
        }

        // Replace under whatever casing the file already used, so we never leave two logbook keys behind.
        foreach (string key in sections.Select(kv => kv.Key)
                     .Where(k => string.Equals(k, SecLogbook, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            sections.Remove(key);
        }

        sections[SecLogbook] = JsonSerializer.SerializeToNode(logbook, WireOptions);

        var payload = new JsonObject
        {
            ["version"] = rootObj["version"]?.DeepClone(),
            ["savedSimTime"] = rootObj["savedSimTime"]?.DeepClone(),
            ["sections"] = sections.DeepClone(),
        };
        rootObj["checksum"] = Checksum(Canonicalize(payload));

        return rootObj.ToJsonString(WireOptions);
    }

    /// <summary>Load a vault from JSON, harvesting every section it can and flagging tampering. Never
    /// throws for a merely-unreadable-or-edited file — the worst case is an near-empty vault with a
    /// warning. (It can still throw for input that is not JSON at all; callers guard the boot path.)</summary>
    public static Vault Load(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        var warnings = new List<string>();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            // Not even JSON. Return an empty, tampered vault rather than crashing the boot.
            return new Vault
            {
                Tampered = true,
                Warnings = [$"vault file is not valid JSON — nothing could be read ({ex.Message})"],
            };
        }

        if (root is not JsonObject rootObj)
        {
            return new Vault
            {
                Tampered = true,
                Warnings = ["vault file's top level is not an object — nothing could be read"],
            };
        }

        int version = ReadInt(rootObj, "version", Vault.CurrentVersion);
        double savedSimTime = ReadDouble(rootObj, "savedSimTime", 0.0);
        string? storedChecksum = TryString(rootObj, "checksum");

        JsonObject? sections = FindObject(rootObj, "sections");

        var vault = new Vault
        {
            Version = version,
            SavedSimTime = savedSimTime,
            Purse = Harvest<PurseSection>(sections, SecPurse, warnings),
            Ship = Harvest<ShipSection>(sections, SecShip, warnings),
            Cargo = Harvest<CargoSection>(sections, SecCargo, warnings),
            Heat = Harvest<HeatSection>(sections, SecHeat, warnings),
            Contacts = Harvest<ContactsSection>(sections, SecContacts, warnings),
            Caches = Harvest<CachesSection>(sections, SecCaches, warnings),
            Quests = Harvest<QuestsSection>(sections, SecQuests, warnings),
            Insurance = Harvest<InsuranceSection>(sections, SecInsurance, warnings),
            Upgrades = Harvest<UpgradesSection>(sections, SecUpgrades, warnings),
            DiceItems = Harvest<DiceItemsSection>(sections, SecDiceItems, warnings),
            Progress = Harvest<ProgressSection>(sections, SecProgress, warnings),
            Nerve = Harvest<NerveSection>(sections, SecNerve, warnings),
            Overheard = Harvest<OverheardSection>(sections, SecOverheard, warnings),
            FieldNotes = Harvest<FieldNotesSection>(sections, SecFieldNotes, warnings),
            CaseThreads = Harvest<CaseThreadsSection>(sections, SecCaseThreads, warnings),
            PapersShown = Harvest<PapersShownSection>(sections, SecPapersShown, warnings),
            Authorities = Harvest<AuthoritiesSection>(sections, SecAuthorities, warnings),
            Filing = Harvest<FilingSection>(sections, SecFiling, warnings),
            WalkIn = Harvest<WalkInSection>(sections, SecWalkIn, warnings),
            OldCrew = Harvest<OldCrewSection>(sections, SecOldCrew, warnings),
            Crossings = Harvest<CrossingsSection>(sections, SecCrossings, warnings),
            HeldMemories = Harvest<HeldMemoriesSection>(sections, SecHeldMemories, warnings),
            InsuranceWeather = Harvest<InsuranceWeatherSection>(sections, SecWeather, warnings),
            Satchel = Harvest<SatchelSection>(sections, SecSatchel, warnings),
            WorkedUp = Harvest<WorkedUpSection>(sections, SecWorkedUp, warnings),
            Kaamos = Harvest<KaamosSection>(sections, SecKaamos, warnings),
            Nebula = Harvest<NebulaSection>(sections, SecNebula, warnings),
            Resume = Harvest<ResumeSection>(sections, SecResume, warnings),
            Logbook = Harvest<LogbookSection>(sections, SecLogbook, warnings),
        };

        // Recompute the checksum over the payload exactly as written (raw node, unknown fields and
        // all), so a forward-compatible file still validates. Compare in fixed time out of habit.
        var payload = new JsonObject
        {
            ["version"] = rootObj["version"]?.DeepClone(),
            ["savedSimTime"] = rootObj["savedSimTime"]?.DeepClone(),
            ["sections"] = sections?.DeepClone(),
        };
        string recomputed = Checksum(Canonicalize(payload));

        bool tampered;
        if (storedChecksum is null)
        {
            tampered = true;
            warnings.Add("vault file carries no checksum — ledger marked tampered");
        }
        else if (!FixedTimeEquals(storedChecksum, recomputed))
        {
            tampered = true;
            warnings.Add("vault checksum did not match — the file was edited outside the game (ledger marked tampered)");
        }
        else
        {
            tampered = false;
        }

        vault.Tampered = tampered;
        vault.Warnings = warnings;
        return vault;
    }

    // ─── section plumbing ───

    private static void AddSection<T>(JsonObject sections, string name, T? value) where T : class
    {
        if (value is null)
        {
            return;
        }

        sections[name] = JsonSerializer.SerializeToNode(value, WireOptions);
    }

    /// <summary>Independently deserialize one named section. On ANY failure, records a warning and
    /// returns null so the rest of the vault still loads — this is the per-section harvest that makes
    /// a partly-unreadable file still useful.</summary>
    private static T? Harvest<T>(JsonObject? sections, string name, List<string> warnings) where T : class
    {
        if (sections is null)
        {
            return null;
        }

        JsonNode? node = FindProperty(sections, name);
        if (node is null)
        {
            return null; // simply absent — a normal, silent case (old file, or nothing to save).
        }

        try
        {
            return node.Deserialize<T>(WireOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            warnings.Add($"vault section '{name}' was unreadable and skipped ({ex.Message})");
            return null;
        }
    }

    // ─── checksum ───

    private static string Checksum(string canonicalPayload)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(ChecksumSalt + "\n" + canonicalPayload);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Emit a JSON string with every object's keys sorted (recursively); arrays keep order.
    /// This is the "canonicalized payload (stable key order)" the checksum is taken over, so property
    /// or dictionary ordering never changes the digest.</summary>
    private static string Canonicalize(JsonNode? node)
    {
        JsonNode? sorted = SortKeys(node);
        return sorted?.ToJsonString(HashOptions) ?? "null";
    }

    private static JsonNode? SortKeys(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var ordered = new JsonObject();
                foreach (KeyValuePair<string, JsonNode?> kv in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    ordered[kv.Key] = SortKeys(kv.Value);
                }

                return ordered;
            case JsonArray arr:
                var copy = new JsonArray();
                foreach (JsonNode? item in arr)
                {
                    copy.Add(SortKeys(item));
                }

                return copy;
            default:
                return node?.DeepClone();
        }
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        byte[] ba = Encoding.UTF8.GetBytes(a);
        byte[] bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    // ─── tolerant node readers (case-insensitive property lookup) ───

    private static JsonObject? FindObject(JsonObject parent, string name) => FindProperty(parent, name) as JsonObject;

    private static JsonNode? FindProperty(JsonObject parent, string name)
    {
        if (parent.TryGetPropertyValue(name, out JsonNode? exact))
        {
            return exact;
        }

        foreach (KeyValuePair<string, JsonNode?> kv in parent)
        {
            if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return kv.Value;
            }
        }

        return null;
    }

    private static int ReadInt(JsonObject obj, string name, int fallback)
    {
        JsonNode? node = FindProperty(obj, name);
        if (node is null)
        {
            return fallback;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch
        {
            return fallback;
        }
    }

    private static double ReadDouble(JsonObject obj, string name, double fallback)
    {
        JsonNode? node = FindProperty(obj, name);
        if (node is null)
        {
            return fallback;
        }

        try
        {
            return node.GetValue<double>();
        }
        catch
        {
            return fallback;
        }
    }

    private static string? TryString(JsonObject obj, string name)
    {
        JsonNode? node = FindProperty(obj, name);
        try
        {
            return node?.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }
}
