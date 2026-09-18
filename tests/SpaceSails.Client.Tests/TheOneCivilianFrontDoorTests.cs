using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #323 · <b>ONE CIVILIAN FRONT DOOR.</b>
///
/// <para>Owner, live (2026-07-18): <i>"The scenario urls still use the old boot… I had to empty the url to
/// get the new one."</i> A captain coming back to a bookmarked <c>/map?scenario=sol</c> — which is also
/// exactly what the home page's Launch button writes — was dropped into a brand-new life without ever being
/// shown the saves they had. The old door rule asked whether four particular CHEAT keys were absent
/// (<c>?dock=</c>, <c>?start=</c>, <c>?sling=</c>, <c>?skim=</c>), and <c>?scenario=</c> is not one of them,
/// so the door went up — and then the world booted into it and nothing was ever chosen. The URL had to be
/// emptied by hand to reach the logbook.</para>
///
/// <para><b>The law, and it is one sentence:</b> a query is <b>civilian</b> — it goes through the logbook —
/// <b>iff it carries nothing but <c>scenario</c></b>. Everything else is a bench incantation out of
/// <c>docs/testing-guide.md</c> Appendix A and keeps the direct boot the smoke sweeps are written against.
/// The sentence lives once, in <c>Map.TheQueryIsCivilian</c>, and this file asks it of the shipping page,
/// of every world the game ships, and of every <c>/map?…</c> link written down anywhere in the product.</para>
///
/// <para><b>What this file does NOT claim.</b> It does not say a civilian boot can never end anywhere but
/// the picker — #1221's refusals put the picker back up for a <c>?start=</c> no sky can anchor, which is a
/// bench URL ending at the door on purpose, and <see cref="ABootUrlRefusesRatherThanCrashesTests"/> owns
/// that. The law here is about which URLs are OFFERED the door in the first place.</para>
///
/// <para><b>Proven red on revert</b>, and the reverts are quoted in the PR body. Putting the old
/// four-key condition back (<c>q.DockCheat is null &amp;&amp; q.StartId is null &amp;&amp; …</c>) reddens
/// <see cref="EveryCheatUrlTheGameOffersIsRefusedTheDoor"/> on 54 URLs and
/// <see cref="TheRuleCanSayBench"/> on 8 of its 13 rows, while every civilian assertion here stays green —
/// which is the shape of the bug: the old rule could not tell a bench URL from a bookmark. Making the rule
/// answer TRUE always reddens the bench half and leaves the civilian half green; making it answer FALSE
/// always reddens <see cref="EveryShippedScenarioDeepLinkOpensTheLogbook"/> and the bare <c>/map</c>.</para>
/// </summary>
[SlowGate]
public sealed class TheOneCivilianFrontDoorTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // ── 1 · THE RULE ITSELF, AS A TABLE ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]                              // the bare front door — /map, nothing after it
    [InlineData("?")]                             // …and the same thing with the question mark typed
    [InlineData("?scenario=sol")]                 // the bookmark the owner had to empty by hand
    [InlineData("?scenario=wheel")]
    [InlineData("?scenario=electric")]            // a sky that does not exist, asked for civilly (#1216 → Sol)
    [InlineData("?SCENARIO=sol")]                 // the browser's autocomplete does not respect our casing
    [InlineData("?scenario=sol&")]                // a trailing ampersand is not a second ask
    [InlineData("?scenario=sol&scenario=wheel")]  // two skies is still only a sky
    public void TheRuleCanSayCivilian(string query) =>
        Assert.True(Map.TheQueryIsCivilian(query),
            $"'{query}' carries nothing but a scenario, so it is a captain arriving at the front door.");

    [Theory]
    [InlineData("?dock=the-tilt")]
    [InlineData("?start=wreck")]
    [InlineData("?fuel=7")]
    [InlineData("?credits=50000")]
    [InlineData("?simhours=9")]
    [InlineData("?backroom=quest")]
    [InlineData("?ellipse=1")]
    [InlineData("?land=1")]
    [InlineData("?sling=jupiter")]
    [InlineData("?skim=saturn")]
    [InlineData("?scenario=sol&dock=the-tilt")]   // a scenario BESIDE a cheat is the cheat's URL
    [InlineData("?scenario=sol&land=1")]
    [InlineData("?scenario=sol&fuel=7&credits=50000")]
    public void TheRuleCanSayBench(string query) =>
        Assert.False(Map.TheQueryIsCivilian(query),
            $"'{query}' asks for a set-up as well as a sky, so it is Appendix A and not a bookmark.");

    // ── 2 · A BARE SCENARIO DEEP-LINK OPENS THE LOGBOOK ─────────────────────────────────────────────────

    [Fact]
    public async Task EveryShippedScenarioDeepLinkOpensTheLogbook()
    {
        // Booted for real, through the shipping page, one per world the game ships — so a scenario added
        // tomorrow joins this sweep by existing rather than by being remembered.
        var wrong = new List<string>();
        foreach (string url in new[] { "/map" }.Concat(EveryShippedScenario().Select(s => $"/map?scenario={s}")))
        {
            PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(url);

            if (booted.Threw is { } threw)
            {
                wrong.Add($"{url}\n  threw {threw.GetType().Name}: {threw.Message}");
                continue;
            }

            if (!booted.Read<bool>("_showStartPicker"))
            {
                wrong.Add($"{url}\n  booted a fresh voyage without offering the logbook — this is #323's bug.");
            }

            // …and it is a DOOR and not merely a flag: nothing has been started behind it. A boot that
            // raised the picker and clamped onto a berth anyway would satisfy a flag check and would still
            // have dropped the captain into a new life.
            if (booted.Field("_dockedHavenId") is not null)
            {
                wrong.Add($"{url}\n  the picker is up but the ship is already clamped on — the door is a picture.");
            }
        }

        Assert.True(wrong.Count == 0,
            "a bare scenario deep-link must route through the logbook door:\n\n" + string.Join("\n\n", wrong));
    }

    [Fact]
    public async Task AndTheScenarioMerelyPRESELECTSWhatNewVoyageWouldLaunch()
    {
        // The other half of the ask: naming a sky must still decide the sky. The berth list the door offers
        // is the loaded scenario's own, so "New voyage" launches into the world the link named — the param
        // preselects, it does not decide that a new voyage is what the captain wanted.
        PastTheGateBench.Boot sol = await PastTheGateBench.BootAsync("/map?scenario=sol");
        PastTheGateBench.Boot wheel = await PastTheGateBench.BootAsync("/map?scenario=wheel");

        Assert.True(sol.Read<bool>("_showStartPicker") && wheel.Read<bool>("_showStartPicker"));
        Assert.NotEqual(TheBerthsOfferedBy(sol), TheBerthsOfferedBy(wheel));
        Assert.Contains("selene-gate", TheBerthsOfferedBy(sol));
    }

    // ── 3 · …AND A BENCH URL IS NEVER OFFERED THE DOOR ──────────────────────────────────────────────────

    [Fact]
    public void EveryCheatUrlTheGameOffersIsRefusedTheDoor()
    {
        // The whole DEV START catalogue — the sixty-odd buttons the front door itself offers under
        // ⚙ DEV START SITES — plus the pinned boot sweep's hand-picked combinations, put through the
        // shipping page's own raise stage. This is the cheap, broad half: no world is built, so all of them
        // can be swept, and what is measured is precisely the door's decision.
        var wrong = new List<string>();
        int swept = 0;

        foreach (string url in TheBenchUrlsTheProductShips())
        {
            swept++;
            if (TheDoorIsRaisedFor(url))
            {
                wrong.Add($"{url} — a bench URL was offered the logbook; the smoke sweeps boot straight in.");
            }
        }

        Assert.True(swept >= 60, $"only {swept} bench URLs were swept — the catalogue has shrunk.");
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public async Task AndTheWholeBootAgreesWithThatDecision()
    {
        // The expensive, narrow half: ten bench URLs booted all the way past the browser gate, because the
        // raise is only the FIRST thing that touches this flag and a later stage could put the menu back up
        // over the situation the cheat just built. Deliberately spread across the four shapes a bench URL
        // has: one that names a berth, two seeded approaches, and six that name no berth at all — the last
        // group being exactly the ones that used to end on the front door.
        string[] benchUrls =
        [
            "/map?dock=the-tilt&site=0&land=1",
            "/map?sling=jupiter",
            "/map?skim=saturn",
            "/map?ellipse=1",
            "/map?credits=50000",
            "/map?credits=1234&fuel=7&simhours=9",
            "/map?kaamos=all",
            "/map?converge=1",
            "/map?secretlab=1",
            "/map?expedition=mining",
        ];

        var wrong = new List<string>();
        foreach (string url in benchUrls)
        {
            PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(url);

            if (booted.Threw is { } threw)
            {
                wrong.Add($"{url}\n  threw {threw.GetType().Name}: {threw.Message}");
            }
            else if (booted.Read<bool>("_showStartPicker"))
            {
                wrong.Add($"{url}\n  ended on the front door — a bench URL must boot into the situation it names.");
            }
        }

        Assert.True(wrong.Count == 0,
            "a bench URL keeps its direct boot all the way through:\n\n" + string.Join("\n\n", wrong));
    }

    // ── 4 · THE SOURCE SWEEP: EXACTLY ONE CIVILIAN FRONT DOOR IN THE WHOLE PRODUCT ──────────────────────

    [Fact]
    public void NoShippedLinkBootsFreshExceptThroughAListedCheat()
    {
        // #323 item 3. Every `/map?…` the product writes down — the pages the game renders, the dev-start
        // catalogue, every document under docs/, the README — is one of exactly two things: a CIVILIAN link,
        // which opens the logbook, or a link whose every key is a cheat this boot can actually read. A third
        // kind (a link that boots fresh through a key nobody parses) is a pre-#311 leftover pointing at
        // nothing, and is what this sweep exists to find.
        IReadOnlySet<string> readable = TheKeysTheBootCanRead();
        var unreadable = new List<string>();
        var undocumented = new List<string>();
        int civilian = 0, bench = 0;

        foreach ((string file, string link) in EveryMapLinkTheProductShips())
        {
            string query = link[link.IndexOf('?')..];
            if (Map.TheQueryIsCivilian(query))
            {
                civilian++;
                continue;
            }

            bench++;
            foreach (string key in TheKeysIn(query))
            {
                if (!readable.Contains(key))
                {
                    unreadable.Add($"{file}: {link}\n  ?{key}= is a key no reader in Map.Sim.World.Query* claims.");
                }
                else if (!TheTestingGuideNames(key))
                {
                    undocumented.Add($"{file}: {link}\n  ?{key}= is real but is not in docs/testing-guide.md.");
                }
            }
        }

        // Anti-vacuous, both ways. A sweep that found no links, or only one kind of link, would be green and
        // would say nothing at all about the door.
        Assert.True(civilian >= 3, $"only {civilian} civilian links found — the sweep is not reading the pages.");
        Assert.True(bench >= 100, $"only {bench} bench links found — the sweep is not reading the docs.");

        Assert.True(unreadable.Count == 0,
            $"{unreadable.Count} shipped links boot fresh through a key the game cannot read:\n\n"
            + string.Join("\n", unreadable.Distinct(StringComparer.Ordinal)));
        Assert.True(undocumented.Count == 0,
            $"{undocumented.Count} shipped links use a cheat that Appendix A does not list. The table and the "
            + "readers are one register; add the row:\n\n"
            + string.Join("\n", undocumented.Distinct(StringComparer.Ordinal)));
    }

    [Fact]
    public void TheHomePagesLaunchButtonAndTheNavMenuAreBothCivilian()
    {
        // Named on their own because they are THE front door in the product's own words — the button a
        // stranger presses and the link in the frame around every page — and because #323's report was
        // written about that button's target specifically. A razor expression stands where the slug goes, so
        // this reads the page's own markup rather than re-typing the URL.
        string home = File.ReadAllText(Path.Combine(
            TheBootBuildsTheSameWorldTests.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Home.razor"));

        string[] launches = [.. Regex.Matches(home, @"href=""(map\?[^""]*)""").Select(m => m.Groups[1].Value)];

        Assert.NotEmpty(launches);
        Assert.All(launches, url => Assert.True(Map.TheQueryIsCivilian(url[url.IndexOf('?')..]),
            $"the home page's Launch button points at {url}, which is a bench URL, not a front door."));

        string nav = File.ReadAllText(Path.Combine(
            TheBootBuildsTheSameWorldTests.RepoRoot(), "src", "SpaceSails.Client", "Layout", "NavMenu.razor"));
        Assert.Contains(@"href=""map""", nav, StringComparison.Ordinal);
    }

    // ── The readings this file makes ────────────────────────────────────────────────────────────────────

    /// <summary>Put one URL through the shipping page's own door stage and answer what the door did. No
    /// world is built: the raise is the only thing being asked, which is why the whole catalogue fits.</summary>
    private static bool TheDoorIsRaisedFor(string url)
    {
        var page = new Map();
        TheBootBuildsTheSameWorldTests.NeverRender(page);
        typeof(Map).GetMethod("RaiseTheFrontDoorWhileTheReactorWarms", Hidden)!
            .Invoke(page, [new Uri("http://localhost" + url)]);
        return (bool)typeof(Map).GetField("_showStartPicker", Hidden)!.GetValue(page)!;
    }

    private static IReadOnlyList<string> TheBerthsOfferedBy(PastTheGateBench.Boot booted)
    {
        object berths = typeof(Map).GetMethod("BerthStarts", Hidden)!.Invoke(booted.Page, [])!;
        return [.. ((System.Collections.IEnumerable)berths)
            .Cast<object>()
            .Select(b => (string)b.GetType().GetField("Item1")!.GetValue(b)!)];
    }

    /// <summary>Every world the game actually ships, off the folder the page fetches from.</summary>
    private static IReadOnlyList<string> EveryShippedScenario() =>
        [.. Directory.EnumerateFiles(
                Path.Combine(TheBootBuildsTheSameWorldTests.RepoRoot(), "scenarios"), "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .OrderBy(name => name, StringComparer.Ordinal)];

    private static IEnumerable<string> TheBenchUrlsTheProductShips() =>
        DevStarts.All.Select(e => e.Url)
            .Concat(TheBootBuildsTheSameWorldTests.EveryBootUrl())
            .Distinct(StringComparer.Ordinal)
            .Where(u => u.Contains('?') && !Map.TheQueryIsCivilian(u[u.IndexOf('?')..]));

    /// <summary>The key of every pair in a query, lower-cased, empties dropped.</summary>
    private static IEnumerable<string> TheKeysIn(string query) =>
        query.TrimStart('?').Split('&')
            .Select(pair => pair.Split('=')[0].ToLowerInvariant())
            .Where(key => key.Length > 0);

    /// <summary>
    /// THE REGISTER OF CHEATS, taken off the boot's own readers rather than typed here — the eighty-odd
    /// <c>StartsWith("key=")</c> literals in <c>Map.Sim.World.Query*.cs</c>, plus the three keys that are
    /// read somewhere other than that chain and would otherwise read as unparsed: the retired
    /// <c>?autowalk=</c> alias (<see cref="AutoWalk.QueryFlag"/>, #875), the beat-hold
    /// (<see cref="StoryBeats.HoldQueryFlag"/>) and <c>?perf=</c>, which
    /// <c>Map.Sim.World.Build</c> reads straight off the Uri.
    /// </summary>
    private static IReadOnlySet<string> TheKeysTheBootCanRead()
    {
        string pages = Path.Combine(TheBootBuildsTheSameWorldTests.RepoRoot(), "src", "SpaceSails.Client", "Pages");
        var keys = new HashSet<string>(StringComparer.Ordinal)
        {
            AutoWalk.QueryFlag, StoryBeats.HoldQueryFlag, "perf",
        };

        foreach (string file in Directory.EnumerateFiles(pages, "Map.Sim.World.Query*.cs"))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"StartsWith\(""([a-z]+)="""))
            {
                keys.Add(m.Groups[1].Value);
            }
        }

        Assert.True(keys.Count >= 80, $"only {keys.Count} cheat keys were read off the boot — the register is empty.");
        return keys;
    }

    private static readonly string TheTestingGuide = File.ReadAllText(
        Path.Combine(TheBootBuildsTheSameWorldTests.RepoRoot(), "docs", "testing-guide.md"));

    private static bool TheTestingGuideNames(string key) =>
        Regex.IsMatch(TheTestingGuide, @"[?&`]" + Regex.Escape(key) + "=");

    /// <summary>Every <c>/map?…</c> link the PRODUCT writes down: the pages the game renders, the dev-start
    /// catalogue, the docs and the README. Tests are deliberately not swept — a fingerprint sweep boots
    /// deliberate nonsense, which is its job and not a link anybody follows.</summary>
    private static IEnumerable<(string File, string Link)> EveryMapLinkTheProductShips()
    {
        string root = TheBootBuildsTheSameWorldTests.RepoRoot();
        IEnumerable<string> files =
            Directory.EnumerateFiles(Path.Combine(root, "src"), "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".razor", StringComparison.Ordinal)
                         || f.EndsWith(".cs", StringComparison.Ordinal))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                                        StringComparison.Ordinal))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories))
            .Append(Path.Combine(root, "README.md"));

        foreach (string file in files)
        {
            // The docs and the C# docblocks both escape their ampersands; a link read through the escaping
            // would show a key called "amp" that nobody parses, which is the sweep inventing its own bug.
            string text = File.ReadAllText(file)
                .Replace("&amp;", "&", StringComparison.Ordinal)
                .Replace("&lt;", "<", StringComparison.Ordinal)
                .Replace("&gt;", ">", StringComparison.Ordinal);

            foreach (Match m in Regex.Matches(text, @"(?<=^|[\s""'(\[`])/?map\?[A-Za-z0-9=&_.%+@-]+", RegexOptions.Multiline))
            {
                yield return (Path.GetRelativePath(root, file).Replace('\\', '/'), m.Value);
            }
        }
    }
}
