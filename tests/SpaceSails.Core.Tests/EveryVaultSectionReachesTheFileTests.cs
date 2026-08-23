using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SpaceSails.Core.Tests;

/// <summary>
/// THE LAW: every optional section declared on <see cref="Vault"/> must reach the file.
///
/// <para>It was broken three times before anyone noticed. <c>OldCrew</c>, <c>Crossings</c> and
/// <c>HeldMemories</c> were declared on the vault, built by the client's <c>BuildVault</c> and read back by
/// its <c>ApplyVault</c> — and <see cref="VaultSerializer"/> never wrote a byte of them, because adding a
/// section means editing THREE places (the key, the <c>AddSection</c>, the <c>Harvest</c>) and nothing on
/// earth checked that the three agreed. Every save silently dropped the old crew's <i>Explained</i> latch,
/// the captain's ⚖ crossings and the black book's held-memory sheets.</para>
///
/// <para>So this test does not read the serializer's source, and does not enumerate a hand-written list of
/// section names that a 28th section could be left off of. It REFLECTS the sections off
/// <see cref="Vault"/> itself — the declaration is the only list — fills each one with non-default data
/// built generically from its own shape, and pushes it through REAL JSON: <see cref="VaultSerializer.Save"/>
/// then <see cref="VaultSerializer.Load"/>, comparing what came back against what went in. A section the
/// serializer forgets comes back null, and this goes red the day it is declared, not a month after a player
/// loses it.</para>
/// </summary>
public class EveryVaultSectionReachesTheFileTests
{
    /// <summary>Every optional section property on <see cref="Vault"/>: a nullable class-typed slot that is
    /// not a string and not a collection (which excludes <see cref="Vault.Warnings"/>) and not a value type
    /// (which excludes Version, SavedSimTime and <see cref="Vault.Tampered"/>). Deliberately shape-based
    /// rather than name-based, so a future section that is not called "…Section" is still governed.</summary>
    private static IReadOnlyList<PropertyInfo> SectionProperties() =>
    [
        .. typeof(Vault)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsClass
                        && p.PropertyType != typeof(string)
                        && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                        && p.CanRead
                        && p.SetMethod is { IsPublic: true })
            .OrderBy(p => p.Name, StringComparer.Ordinal),
    ];

    [Fact]
    public void EveryVaultSection_ReachesTheFile_AndComesBackWhole()
    {
        IReadOnlyList<PropertyInfo> sections = SectionProperties();

        // A sanity floor, so a filter that quietly stopped matching anything cannot make this test pass by
        // governing nothing. There were 27 sections the day this law was written.
        Assert.True(sections.Count >= 27, $"only {sections.Count} vault sections were discovered by reflection");

        var dropped = new List<string>();
        var mangled = new List<string>();

        foreach (PropertyInfo section in sections)
        {
            // A vault carrying ONLY this section, filled from its own shape, so the file names it or nothing.
            var one = new Vault { Version = Vault.CurrentVersion, SavedSimTime = 4242.5 };
            object filled = Filler.Build(section.PropertyType);
            section.SetValue(one, filled);

            string json = VaultSerializer.Save(one);

            // 1. It reached the bytes at all.
            JsonObject written = ((JsonNode.Parse(json) as JsonObject)?["sections"] as JsonObject)!;
            Assert.NotNull(written);
            if (written.Count == 0)
            {
                dropped.Add(section.Name);
                continue;
            }

            // 2. It came back, and came back as the same thing.
            Vault loaded = VaultSerializer.Load(json);
            Assert.False(loaded.Tampered, $"{section.Name}: a vault carrying only this section loaded tampered");
            Assert.Empty(loaded.Warnings);

            object? back = section.GetValue(loaded);
            if (back is null)
            {
                dropped.Add(section.Name);
                continue;
            }

            if (!string.Equals(Canonical(filled), Canonical(back), StringComparison.Ordinal))
            {
                mangled.Add($"{section.Name}\n  wrote: {Canonical(filled)}\n  read : {Canonical(back)}");
            }
        }

        Assert.True(
            dropped.Count == 0,
            "these Vault sections NEVER REACH THE FILE — VaultSerializer has no AddSection/Harvest for them, "
            + "so every save silently drops them: " + string.Join(", ", dropped));
        Assert.True(mangled.Count == 0, "these Vault sections changed across the file:\n" + string.Join("\n", mangled));
    }

    /// <summary>The same law with every section aboard at once: one file, all of them, byte-stable. This is
    /// the shape a real save has, and it also proves the sections do not collide over a key.</summary>
    [Fact]
    public void AVaultWithEverySection_RoundTripsWholeAndReSavesIdentically()
    {
        IReadOnlyList<PropertyInfo> sections = SectionProperties();

        var full = new Vault { Version = Vault.CurrentVersion, SavedSimTime = 987654.25 };
        foreach (PropertyInfo section in sections)
        {
            section.SetValue(full, Filler.Build(section.PropertyType));
        }

        string json = VaultSerializer.Save(full);
        var written = ((JsonNode.Parse(json) as JsonObject)?["sections"] as JsonObject)!;

        // One key per declared section — no more (a stray key) and no fewer (a forgotten one).
        Assert.Equal(sections.Count, written.Count);

        Vault loaded = VaultSerializer.Load(json);
        Assert.False(loaded.Tampered);
        Assert.Empty(loaded.Warnings);

        foreach (PropertyInfo section in sections)
        {
            object? back = section.GetValue(loaded);
            Assert.True(back is not null, $"{section.Name} did not survive a full-vault round-trip");
            Assert.Equal(Canonical(section.GetValue(full)!), Canonical(back!));
        }

        // Re-saving what was loaded reproduces the exact same file, checksum and all.
        Assert.Equal(json, VaultSerializer.Save(loaded));
    }

    /// <summary>#973 · The three that were lost, named, so the next reader of this file knows what it cost:
    /// the old crew's <i>Explained</i> latch, the captain's crossings and the held-memory sheets.</summary>
    [Fact]
    public void TheThreeThatWereLost_SurviveTheFile()
    {
        var vault = new Vault
        {
            OldCrew = new OldCrewSection { Shipmates = ["row-a", "row-b"], Explained = ["fess", "maren"] },
            Crossings = new CrossingsSection { Crossings = ["crossing-one", "crossing-two"] },
            HeldMemories = new HeldMemoriesSection { Sheets = ["sheet-photograph", "sheet-her-note"] },
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));

        Assert.False(loaded.Tampered);
        Assert.Empty(loaded.Warnings);
        Assert.Equal(["row-a", "row-b"], loaded.OldCrew!.Shipmates);
        Assert.Equal(["fess", "maren"], loaded.OldCrew.Explained);
        Assert.Equal(["crossing-one", "crossing-two"], loaded.Crossings!.Crossings);
        Assert.Equal(["sheet-photograph", "sheet-her-note"], loaded.HeldMemories!.Sheets);
    }

    /// <summary>…and the other promise: a file written before they existed still loads clean, with nothing
    /// marked, rather than warning or failing.</summary>
    [Fact]
    public void AFileWithoutThem_LoadsCleanWithNothingMarked()
    {
        string json = VaultSerializer.Save(new Vault { Purse = new PurseSection(7), SavedSimTime = 10.0 });
        Vault loaded = VaultSerializer.Load(json);

        Assert.False(loaded.Tampered);
        Assert.Empty(loaded.Warnings);
        Assert.Equal(7, loaded.Purse!.Credits);
        Assert.Null(loaded.OldCrew);
        Assert.Null(loaded.Crossings);
        Assert.Null(loaded.HeldMemories);
    }

    private static readonly JsonSerializerOptions CompareOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = false,
    };

    private static string Canonical(object value) => JsonSerializer.Serialize(value, value.GetType(), CompareOptions);

    /// <summary>Builds a non-default instance of ANY section shape from its own type — primitives, strings,
    /// enums, lists, dictionaries and nested records — so the law needs no per-section fixture to maintain
    /// (an unmaintained fixture is exactly how a section gets forgotten in the first place).</summary>
    private sealed class Filler
    {
        private int _n;

        public static object Build(Type type) => new Filler().Make(type, 0)!;

        private object? Make(Type type, int depth)
        {
            _n++;

            if (depth > 6)
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            if (Nullable.GetUnderlyingType(type) is { } inner)
            {
                return Make(inner, depth);
            }

            if (type == typeof(string))
            {
                return $"kept-{_n}";
            }

            if (type == typeof(bool))
            {
                return true;
            }

            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                return values.Length > 1 ? values.GetValue(1)! : values.GetValue(0)!;
            }

            if (type == typeof(int) || type == typeof(short) || type == typeof(byte) || type == typeof(long))
            {
                return Convert.ChangeType(_n + 3, type);
            }

            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                return Convert.ChangeType(_n + 0.5, type);
            }

            if (type.IsGenericType)
            {
                Type def = type.GetGenericTypeDefinition();
                Type[] args = type.GetGenericArguments();

                if (def == typeof(IReadOnlyDictionary<,>) || def == typeof(IDictionary<,>) || def == typeof(Dictionary<,>))
                {
                    Type dict = typeof(Dictionary<,>).MakeGenericType(args);
                    object made = Activator.CreateInstance(dict)!;
                    dict.GetMethod("Add")!.Invoke(made, [Make(args[0], depth + 1), Make(args[1], depth + 1)]);
                    return made;
                }

                if (def == typeof(IReadOnlyList<>) || def == typeof(IList<>) || def == typeof(List<>)
                    || def == typeof(IEnumerable<>) || def == typeof(ICollection<>))
                {
                    Type list = typeof(List<>).MakeGenericType(args[0]);
                    object made = Activator.CreateInstance(list)!;
                    MethodInfo add = list.GetMethod("Add")!;
                    add.Invoke(made, [Make(args[0], depth + 1)]);
                    add.Invoke(made, [Make(args[0], depth + 1)]);
                    return made;
                }
            }

            if (type.IsArray)
            {
                Type element = type.GetElementType()!;
                var made = Array.CreateInstance(element, 1);
                made.SetValue(Make(element, depth + 1), 0);
                return made;
            }

            // A record (or any object): build through its widest constructor, then fill every settable
            // property — including init-only ones, which reflection can set — so nothing is left default.
            ConstructorInfo? ctor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            object instance = ctor is null
                ? Activator.CreateInstance(type)!
                : ctor.Invoke([.. ctor.GetParameters().Select(p => Make(p.ParameterType, depth + 1))]);

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.SetMethod is not { IsPublic: true } || prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                prop.SetValue(instance, Make(prop.PropertyType, depth + 1));
            }

            return instance;
        }
    }
}
