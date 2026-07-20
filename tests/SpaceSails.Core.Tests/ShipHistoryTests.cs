using System.Text.RegularExpressions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// NPC hull service histories (owner ruling, 2026-07-19, aboard the ex-premium ferry Victoria I:
/// every scanned hull carries a Victoria-I story — former names, owners deep, and the wear that proves
/// the glory). Covers seeded determinism, former-name count bounds, distinctness, no brace leaks, the
/// house-yard / name-pool integrity, the FALSE COLORS "latest name" idiom, and the one-line teaser.
/// </summary>
public class ShipHistoryTests
{
    // A spread of ship ids in the shapes the traffic generator mints: founding traffic, later waves,
    // pods, hunters, the named starters.
    private static readonly string[] SampleIds =
    [
        "npc-0", "npc-1", "npc-2", "npc-7", "npc-w2-0", "npc-w2-3", "npc-w3-1",
        "pod-0", "pod-3", "pod-w2-1", "hunter-0", "hunter-1",
        "starter-pod", "starter-freighter",
    ];

    [Fact]
    public void For_IsSeededDeterministic_SameIdSameStory()
    {
        foreach (string id in SampleIds)
        {
            ShipHistory a = ShipHistories.For(id);
            ShipHistory b = ShipHistories.For(id);

            Assert.Equal(a.LaidDown, b.LaidDown);
            Assert.Equal(a.OwnersDeep, b.OwnersDeep);
            Assert.Equal(a.Condition, b.Condition);
            Assert.Equal(a.FormerNames, b.FormerNames); // order and content stable
            Assert.Equal(a.Teaser, b.Teaser);
        }
    }

    [Fact]
    public void For_DifferentIds_GiveDifferentStories()
    {
        // Not every field differs per id, but the whole story should vary — no single canned history.
        var stories = SampleIds
            .Select(id => ShipHistories.For(id))
            .Select(h => $"{h.LaidDown}|{h.FormerNamesLine}|{h.OwnersDeep}|{h.Condition}")
            .ToList();

        Assert.True(stories.Distinct().Count() >= SampleIds.Length - 2,
            "seeded histories should be well spread across ids, not one canned story");
    }

    [Fact]
    public void FormerNames_AreWithinBounds_ZeroToThree()
    {
        foreach (string id in SampleIds)
        {
            ShipHistory h = ShipHistories.For(id);
            Assert.InRange(h.FormerNames.Count, 0, 3);
        }
    }

    [Fact]
    public void FormerNames_AreDistinctWithinAHull()
    {
        // Scan a wide id space so the multi-former-name hulls are exercised.
        foreach (int i in Enumerable.Range(0, 400))
        {
            ShipHistory h = ShipHistories.For($"npc-{i}");
            var bare = h.FormerNames
                .Select(n => n.Split(" (", StringSplitOptions.None)[0])
                .ToList();
            Assert.Equal(bare.Count, bare.Distinct().Count()); // no hull lists the same former name twice
        }
    }

    [Fact]
    public void OwnersDeep_IsAtLeastTheRenameCount()
    {
        // A rename is a re-registration, so a hull is always at least as many owners deep as it has
        // former names (owner canon: "ex-Aurora Queen … three owners deep").
        foreach (int i in Enumerable.Range(0, 400))
        {
            ShipHistory h = ShipHistories.For($"npc-{i}");
            Assert.True(h.OwnersDeep >= h.FormerNames.Count);
        }
    }

    [Fact]
    public void NoBraceLeaks_InAnyRenderedString()
    {
        // Guards against a formatting bug leaving a "{0}" / "{name}" in the copy.
        foreach (int i in Enumerable.Range(0, 400))
        {
            ShipHistory h = ShipHistories.For($"npc-{i}");
            AssertNoBraces(h.LaidDown);
            AssertNoBraces(h.Condition);
            AssertNoBraces(h.Teaser);
            AssertNoBraces(h.FormerNamesLine);
            foreach (string fn in h.FormerNames)
            {
                AssertNoBraces(fn);
            }

            AssertNoBraces(h.CurrentNameLine("MERIDIAN", flyingFalseColors: false) ?? "");
            AssertNoBraces(h.CurrentNameLine("MERIDIAN", flyingFalseColors: true) ?? "");
        }
    }

    [Fact]
    public void FormerNames_RenderAsExNameWithFate()
    {
        // Find a hull with at least one former name and check the dossier shape.
        ShipHistory h = Enumerable.Range(0, 400)
            .Select(i => ShipHistories.For($"npc-{i}"))
            .First(x => x.HasFormerNames);

        string first = h.FormerNames[0];
        Assert.StartsWith("ex-", first);
        Assert.Contains(" (", first);
        Assert.EndsWith(")", first);
        Assert.Contains(" · ", h.FormerNamesLine.Length > first.Length ? h.FormerNamesLine : $"{first} · x");
    }

    [Fact]
    public void NamePool_HasNoDuplicates()
    {
        // The authored pools are the ground truth for uniqueness — a dup would let a hull's distinct
        // draw still surface the "same" glory name under two indices.
        AssertPoolDistinct(FieldPool("FormerNamePool"));
        AssertPoolDistinct(FieldPool("Fates"));
        AssertPoolDistinct(FieldPool("Yards"));
        AssertPoolDistinct(FieldPool("Conditions"));
    }

    [Fact]
    public void LaidDown_NamesAHouseYard_AndAnAgedYear()
    {
        string[] yards = FieldPool("Yards");
        foreach (string id in SampleIds)
        {
            ShipHistory h = ShipHistories.For(id);
            Assert.StartsWith("Laid down at ", h.LaidDown);
            Assert.Contains(yards, y => h.LaidDown.Contains(y));

            // The year is comfortably old (2270..2319) — older than the player's own 2341 hull.
            Match m = Regex.Match(h.LaidDown, @"(\d{4})\.$");
            Assert.True(m.Success, $"no year on '{h.LaidDown}'");
            int year = int.Parse(m.Groups[1].Value);
            Assert.InRange(year, 2270, 2319);
        }
    }

    [Fact]
    public void KoskiYard_IsSharedWithThePlayersBuildersPlate()
    {
        // The NPC yard canon extends the same Koski & Daughters yard the player's plate (#392) names.
        string[] yards = FieldPool("Yards");
        Assert.Contains(yards, y => y.Contains("Koski & Daughters"));
    }

    [Fact]
    public void CurrentNameLine_FalseColors_FramesTheNameAsMerelyTheLatest()
    {
        ShipHistory anyHull = ShipHistories.For("npc-0");
        string? falseLine = anyHull.CurrentNameLine("GHOST", flyingFalseColors: true);
        Assert.NotNull(falseLine);
        Assert.Contains("GHOST", falseLine!);
        Assert.Contains("latest", falseLine, StringComparison.OrdinalIgnoreCase);

        // An honest hull with a past is "currently answering to" her latest name; a maiden hull has none.
        ShipHistory withPast = Enumerable.Range(0, 400)
            .Select(i => ShipHistories.For($"npc-{i}"))
            .First(x => x.HasFormerNames);
        string? honest = withPast.CurrentNameLine("MERIDIAN", flyingFalseColors: false);
        Assert.NotNull(honest);
        Assert.Contains("MERIDIAN", honest!);

        ShipHistory maiden = Enumerable.Range(0, 400)
            .Select(i => ShipHistories.For($"npc-{i}"))
            .First(x => !x.HasFormerNames);
        Assert.Null(maiden.CurrentNameLine("MERIDIAN", flyingFalseColors: false)); // no ledger, no line
    }

    [Fact]
    public void Teaser_IsAlwaysANonEmptyOneLiner()
    {
        foreach (string id in SampleIds)
        {
            ShipHistory h = ShipHistories.For(id);
            Assert.False(string.IsNullOrWhiteSpace(h.Teaser));
            Assert.DoesNotContain('\n', h.Teaser);

            if (h.HasFormerNames)
            {
                Assert.Contains("owner", h.Teaser); // headline: former name · N owner(s) deep
                Assert.Contains("deep", h.Teaser);
            }
        }
    }

    private static void AssertNoBraces(string s)
    {
        Assert.DoesNotContain('{', s);
        Assert.DoesNotContain('}', s);
    }

    private static void AssertPoolDistinct(string[] pool) =>
        Assert.Equal(pool.Length, pool.Distinct().Count());

    // Read a private static string[] pool off ShipHistories via reflection — the pools are the tested
    // ground truth without widening their visibility.
    private static string[] FieldPool(string name)
    {
        var field = typeof(ShipHistories).GetField(
            name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        return (string[])field!.GetValue(null)!;
    }
}
