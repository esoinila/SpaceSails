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
    private const string SecResume = "resume";

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
        AddSection(sections, SecResume, vault.Resume);

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
            Resume = Harvest<ResumeSection>(sections, SecResume, warnings),
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
