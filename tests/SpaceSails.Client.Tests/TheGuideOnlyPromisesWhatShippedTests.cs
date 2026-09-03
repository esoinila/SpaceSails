using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE GUIDE IS DOCUMENTATION, AND DOCUMENTATION THAT DESCRIBES A FEATURE NOBODY BUILT IS A BUG (#938 D3b).
///
/// <para><b>What was wrong.</b> <c>Guide.razor</c>'s "Choosing a voyage" section offered a fourth voyage —
/// <i>"Join the crew — multiplayer: enter a callsign and share a live session. Warp runs at the slowest
/// crew member's request (the min-warp rule)"</i> — and a URL recipe to skip the home page,
/// <c>&amp;mp=1&amp;callsign=YourName</c>. <c>git grep -n "multiplayer\|min-warp" -- src</c> returned
/// those Guide lines and nothing else. The home page has never carried such a voyage, and the boot's query
/// reader parses no <c>mp=</c> and no <c>callsign=</c> among the eighty-odd keys it does read. A player who
/// followed the recipe got an ordinary single-captain Sol.</para>
///
/// <para><b>The law, in two halves.</b> First: a term the Guide uses to name a feature must exist in the
/// shipped code somewhere other than the Guide. Second, and stronger, because it catches the next recipe
/// rather than the last one: every URL query parameter the Guide teaches must be a key the boot actually
/// reads. Both read the page as a READER sees it — Razor comments are stripped first, so the note
/// explaining the removal cannot keep this bench red (or, worse, green by accident).</para>
///
/// <para>Both halves say their own counts out loud: a scan that found no parameters, or a code base with
/// no parsed keys, would be green forever.</para>
/// </summary>
public class TheGuideOnlyPromisesWhatShippedTests
{
    /// <summary>Words the Guide may only use if the game has the thing. Each is checked against the whole
    /// shipped tree with the Guide itself taken out, so building the feature retires the rule
    /// automatically — this bench never has to be edited to allow a real multiplayer.</summary>
    private static readonly string[] FeatureWords = ["multiplayer", "min-warp", "minwarp"];

    /// <summary>
    /// THE FIRST LAW. The Guide never names a feature the code does not have.
    /// </summary>
    [Fact]
    public void The_guide_names_no_feature_the_code_does_not_have()
    {
        string guide = RenderedGuideText();
        Assert.True(guide.Length > 2000, $"only {guide.Length} chars of Guide left after stripping comments — the scan proved nothing");

        string elsewhere = EverythingElseInSrc();
        Assert.True(elsewhere.Length > 100_000, "the rest of src read as almost empty — the scan proved nothing");

        var offences = new List<string>();
        foreach (string word in FeatureWords)
        {
            bool inGuide = guide.Contains(word, StringComparison.OrdinalIgnoreCase);
            bool inCode = elsewhere.Contains(word, StringComparison.OrdinalIgnoreCase);
            if (inGuide && !inCode)
            {
                offences.Add($"  \"{word}\" — the Guide describes it; nothing else in src mentions it at all");
            }
        }

        Assert.True(offences.Count == 0,
                    "the Captain's Guide advertises something that does not exist:\n" + string.Join("\n", offences));
    }

    /// <summary>
    /// THE SECOND LAW. Every URL recipe the Guide teaches names a query key the boot really reads.
    /// </summary>
    [Fact]
    public void Every_url_recipe_in_the_guide_names_a_key_the_boot_reads()
    {
        IReadOnlySet<string> parsed = KeysTheBootReads();
        Assert.True(parsed.Count >= 20,
                    $"only {parsed.Count} query key(s) found in the boot's readers — the scan proved nothing");

        // The Guide writes its recipes HTML-escaped; a reader sees the ampersands.
        string guide = RenderedGuideText().Replace("&amp;", "&", StringComparison.Ordinal);

        var taught = Regex.Matches(guide, @"[?&]([A-Za-z][A-Za-z0-9_]*)=")
                          .Select(m => m.Groups[1].Value.ToLowerInvariant())
                          .Distinct(StringComparer.Ordinal)
                          .OrderBy(k => k, StringComparer.Ordinal)
                          .ToList();

        Assert.True(taught.Count > 0,
                    "the Guide teaches no URL recipe at all — either it lost its links or this bench is "
                    + "reading the wrong file, and either way it proves nothing");

        var invented = taught.Where(k => !parsed.Contains(k)).ToList();

        Assert.True(invented.Count == 0,
                    $"{invented.Count} query parameter(s) the Guide tells the captain to append are read by "
                    + "nothing in the boot — following the recipe changes nothing: "
                    + string.Join(", ", invented));
    }

    // ── Reading the two sides ────────────────────────────────────────────────────────────────────────

    /// <summary>The Guide as a reader sees it: Razor comments (<c>@* … *@</c>) removed, because a note ABOUT
    /// a removed passage is not the passage.</summary>
    private static string RenderedGuideText()
    {
        string razor = File.ReadAllText(Path.Combine(ClientRoot, "Pages", "Guide.razor"));
        return Regex.Replace(razor, @"@\*.*?\*@", " ", RegexOptions.Singleline);
    }

    /// <summary>Every query key any boot reader parses — the <c>pair.StartsWith("name=")</c> idiom the
    /// query files are written in.</summary>
    private static IReadOnlySet<string> KeysTheBootReads()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(ClientRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match m in Regex.Matches(File.ReadAllText(path), @"StartsWith\(""([A-Za-z][A-Za-z0-9_]*)="""))
            {
                keys.Add(m.Groups[1].Value.ToLowerInvariant());
            }
        }
        return keys;
    }

    /// <summary>The whole shipped client and Core, with the Guide taken out — "does the game have this".</summary>
    private static string EverythingElseInSrc()
    {
        var sb = new System.Text.StringBuilder();
        foreach (string path in Directory.GetFiles(Path.Combine(RepoRoot, "src"), "*.*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(path);
            if (ext is not (".cs" or ".razor" or ".js" or ".json"))
            {
                continue;
            }
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.EndsWith("Guide.razor", StringComparison.Ordinal))
            {
                continue;
            }
            sb.Append(File.ReadAllText(path)).Append('\n');
        }
        return sb.ToString();
    }

    private static string ClientRoot => Path.Combine(RepoRoot, "src", "SpaceSails.Client");

    private static string RepoRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir, "src", "SpaceSails.Client")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("Could not find the repository root above the test assembly.");
        }
    }
}
