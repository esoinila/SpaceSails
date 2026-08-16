using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 7a · THE BOOT BUILDS THE SAME WORLD IT ALWAYS DID.
///
/// <para><b>Why this file exists.</b> <c>BootTheWorldAsync</c> was one method of 1,656 lines: the whole
/// <c>?query</c> cheat surface, the scenario load, the appended cheat bodies, the ephemeris, four traffic
/// planners, the berth roster, the start and the renderer wiring, in one straight pass. Splitting it is a
/// BEHAVIOUR-BEARING refactor — a method is being cut, not a file — and the only honest way to cut it is
/// to write down what it builds FIRST, from the old code, and then require the new code to build it byte
/// for byte.</para>
///
/// <para><b>What a fingerprint is here.</b> Two <see cref="Pages.Map"/>s: one that never booted, one that
/// booted at the URL under test. Every instance field whose rendered value DIFFERS between them is the
/// boot's own work — the ship, the ephemeris, the traffic, the purse and the hold, the camera, the start
/// picker, the scenario name, and every one of the four dozen <c>_…Cheat</c> flags the query sets. Those
/// differences are rendered to a stable text (invariant culture, round-trip doubles, sets sorted, long
/// primitive arrays folded to a digest) and hashed. Diffing against a virgin component rather than naming
/// fields by hand is deliberate: a field the boot starts writing tomorrow enters the fingerprint on its
/// own, and a field the boot never touches never enters it at all.</para>
///
/// <para><b>Where the fingerprint is taken, and why not further.</b> At the throw. <c>#737</c>'s own
/// comment calls <c>abandoned.ThrowIfCancellationRequested()</c> before <c>CanvasRenderer</c> "THE LAST
/// GATE BEFORE THE DOM", and one line above it the boot awaits <c>RendererInterop.EnsureModuleLoadedAsync</c>
/// — <c>JSHost.ImportAsync</c>, which off a browser cannot be reached at all. That is not a limitation of
/// this bench, it is the documented shape of the page: <c>TheBootStopsWhenYouLeaveTests</c> asserts the
/// same wall from the other side ("a boot NOBODY left still goes all the way to the BROWSER"), by proving
/// the unabandoned boot THROWS. So the fingerprint covers the boot up to that gate — the query parse, the
/// berth defaults, the scenario load, every appended cheat body, the ephemeris, the ship, the purse, the
/// hold, the four traffic planners and the camera. What the boot does AFTER the gate (the start point and
/// the cheats that need a live world) is unreachable off-browser in any harness, and travels as a verbatim
/// move instead — see the PR body's line-multiset proof.</para>
///
/// <para><b>The parse loop's own answer</b> — the locals the 1,150-line <c>?query</c> chain writes, which
/// are invisible in the fields above until something after the gate consumes them — is pinned separately
/// by <see cref="TheBootReadsTheSameQueryTests"/>.</para>
///
/// <para><b>Red proof, twice, both quoted verbatim in the PR body.</b> Swap two stages in the conductor
/// and the hashes move. Moving <c>PointTheCameraAtHer</c> above <c>LayTheShipDownWithHerHistory</c> —
/// the camera aimed at a ship that has not been laid down yet — reddens <b>75 of 75</b>. Moving
/// <c>RaiseTheFrontDoorWhileTheReactorWarms</c> above <c>DefaultABerthForTheCheatsThatNeedOne</c> —
/// so the front door is decided before the cheats that need a berth have invented theirs — reddens
/// exactly <b>4 of 75</b>, and they are exactly the four URLs where a cheat has to invent a berth
/// (<c>?bond=1</c> twice, <c>?ashore=1</c>, <c>?death=impact</c>). Both numbers matter: the first says
/// the sweep sees the whole boot, the second says it is not merely sensitive to everything.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBootBuildsTheSameWorldTests
{
    /// <summary>Set to a file path to DUMP every URL's rendered fingerprint text instead of asserting —
    /// how the hashes below were captured off the old code. Never set in CI.</summary>
    private const string DumpVariable = "SPACESAILS_BOOT_FINGERPRINT_DUMP";

    // ── The pinned world, one sha256 (first 32 hex) per URL, captured from the OLD one-method boot ───────
    private static readonly IReadOnlyDictionary<string, string> TheWorldEachUrlBuilds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["/map"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?archive=1&land=1&nerve=2"] = "7ff8b29328da14c24e323d429e2efd66",
            ["/map?ashore=1&kaamos=bounce"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?ashore=1&start=space-bar"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?badge=1"] = "7a2157520754fb99a69e66f20cf50b55",
            ["/map?bond=1"] = "997a1856f73c587a2263d7ab1ba39b12",
            ["/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all"] = "ed300e0dc20fdfc4b8a0df8889e4a6e1",
            ["/map?converge=1"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?counter=1"] = "3c29c8812bfd366dcda5b773d0892b46",
            ["/map?credits=1234&fuel=7&simhours=9"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?credits=50000"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?death=collector&dock=selene-gate"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?death=impact"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?death=suffocated&dock=the-tilt&land=1"] = "e5bc7054e7447037a65a7a61690f725c",
            ["/map?deflection=1"] = "a2827d9e09e61c0848ec403662b9bc2b",
            ["/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1"] = "b3c504178574a4790c333151937adb2e",
            ["/map?designate=1"] = "12d2240e907004bed6f6aeaa0dc22c5c",
            ["/map?dock=red-eye&body=ganymede&site=1&land=1"] = "a7cedb33278669b28975a0cdd998d6c5",
            ["/map?dock=ringside-exchange&body=titan&site=1&land=1"] = "047eb4aae53d5080e575b0697454eadf",
            ["/map?dock=selene-gate&body=luna&site=1&land=1"] = "e9877cf05401a0d0b2c19a258fdca43c",
            ["/map?dock=the-deep&body=triton&site=2&land=1"] = "c7a86304521adba809833ddfdc675e7f",
            ["/map?dock=the-space-bar"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1"] = "f3484917e3dba0d8164641215b25a9d2",
            ["/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1"] = "7f2cdc79adf0371e5a862832f15d313e",
            ["/map?dock=the-space-bar&body=phobos&site=1&land=1"] = "17d1e52416189ae65d394f7c5fa09caa",
            ["/map?dock=the-tilt&site=0"] = "063e12023157d59b4504ea447e5b350f",
            ["/map?dock=the-tilt&site=0&land=1"] = "ff629608ff8f72bde4813f450240ff51",
            ["/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low"] = "32598d06bfeb7a18f34b8baf38072bc3",
            ["/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1"] = "1bd82db3ef3ba27dffc5101535a4e4eb",
            ["/map?dock=the-tilt&site=0&land=1&reevers=4"] = "459db9e0f9bd9699977d96697ae73346",
            ["/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12"] = "56e7c1fb23b3f5c7b88272206d894da6",
            ["/map?dock=the-tilt&site=1"] = "db095bc44e0a82ac81d117a7b3e7c55f",
            ["/map?dock=the-tilt&start=space-bar"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?expedition=mining"] = "4c137b29f15f1fb91a50f7608872d1a6",
            ["/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?found=1&land=1"] = "f6bcc42d30790f36d26ead13ca0dde0d",
            ["/map?found=1&land=1&floor=17&card=all"] = "6646b79921214a4ee308aa1853c8978d",
            ["/map?freight=1"] = "499a06d4ca8bd0b6110c9f48355f792c",
            ["/map?frontdoor=1"] = "0dad1128b1f56f65b9ce878656c7217f",
            ["/map?goodscar=1"] = "60f91bdba32b1ea73a952af65d2f52d3",
            ["/map?kaamos=all"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?kaamos=hq&arrivalphase=2&land=1&floor=23"] = "09ac6eae644add9fb42c766181fd95b6",
            ["/map?kaamos=hq&land=1"] = "bbd102f43449c03bc34772c86c8cbf4a",
            ["/map?kaamos=pod&nebula=adjuster&arrivalphase=7"] = "26e34dd580be7562e6aaa36e59439665",
            ["/map?nebula=all"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?park=1"] = "afe9d0be28a8faee7cb69d8eb6948200",
            ["/map?park=1&spread=1"] = "68fe6f920bfe8fbfc203c6a95fff1dc3",
            ["/map?parkback=1"] = "06144ae7a23de85e29d2f46ef784e071",
            ["/map?parkwalk=1"] = "27eb67ed2a0d02e189a5244ddd6250a9",
            ["/map?patrol=2"] = "d7deed8e2802c54bedee5d817dc185af",
            ["/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1"] = "ea56d3959b11b65dd84440a734d46814",
            ["/map?ringoffice=1"] = "e6d6fc5e106f3d80869a8225b2a0755f",
            ["/map?rip=1"] = "d8483f6da18237798125984981222117",
            ["/map?scenario=..%2Foops"] = "c2affbb46741397274d14b3c871d041e",
            ["/map?scenario=sol-eu"] = "e8ebe7387d63f82e416ec6dff4319a1d",
            ["/map?secretlab=1"] = "b3851d6e76cc0875ea601d898aa58418",
            ["/map?secretlab=deep&land=1&card=next"] = "d30196963cb876eda13044d766ea966d",
            ["/map?secretlab=deep&land=1&floor=1"] = "e52b03754da5fa78aaef9506a85c86f2",
            ["/map?secretlab=deep&land=1&floor=1&card=next"] = "0c851248da43c45cd5ee07a6106857af",
            ["/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1"] = "f8b93a833fa86dee954f39702865ab44",
            ["/map?secretlab=deep&land=1&floor=21"] = "6b79058c7890a5498a8c0376ad7c3229",
            ["/map?skim=saturn"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?sling=jupiter"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?spread=1"] = "3e238df2da78b28c4b0c5be11cd2ff79",
            ["/map?start=&dock=&fuel=&nerve=&site=&land="] = "8fd72ea4996170f6407eee7573af119f",
            ["/map?start=wreck&fetch=active"] = "ae2959d39951e35b28fafcd6521266d0",
            ["/map?stool=1&neighbour=0"] = "c474cccb41251e1d4146224e848a1cf6",
            ["/map?stool=1&neighbour=1"] = "8250a85d35ac1549d89750a00ddd6298",
            ["/map?tablescene=free&approach=1"] = "d8f3dd890d85e07ec52e57edc30edb34",
            ["/map?tablescene=free&watch=5&approach=0"] = "72661dcc4a102d609059b7e2a61da730",
            ["/map?threads=1"] = "81e729c004db55c2786e257553375188",
            ["/map?threads=1&watch=5"] = "dc78f709a056d049514982a8175f6834",
            ["/map?wreck=drivefailure&land=1"] = "dfbe14f4f1e1a7e2c6900f3af3b8e404",
            ["/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4"] = "ecc6aa422011028051ca809f5620dfdf",
        };

    /// <summary>The bare front door, plus every dev URL the game itself offers, plus a set of hand-picked
    /// combinations that exercise the query keys no DevStart happens to use and the reading orders the
    /// boot's own comments call load-bearing (<c>?dock=</c> before <c>?start=</c>, <c>?found=</c> implying
    /// <c>?secretlab=</c>, <c>?threads=</c> implying <c>?spread=</c> implying <c>?tablescene=</c>).</summary>
    public static IEnumerable<string> EveryBootUrl() =>
        new[] { "/map" }
            .Concat(SpaceSails.Core.DevStarts.All.Select(e => e.Url))
            .Concat(HandPicked)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(u => u, StringComparer.Ordinal);

    private static readonly string[] HandPicked =
    [
        // the keys that write only a local — invisible in the fields, and the reason the query guard next
        // door exists — booted anyway, because they also steer which bodies get appended and where.
        "/map?credits=1234&fuel=7&simhours=9",
        "/map?fetch=intel&tip=route&hoard=both&crack=active&backroom=quest",
        "/map?reveal=derelict-roadster&reveal=nothing-at-all&ellipse=1",
        "/map?sling=jupiter",
        "/map?skim=saturn",
        "/map?start=wreck&fetch=active",
        // the orders the boot calls load-bearing
        "/map?dock=the-tilt&start=space-bar",
        "/map?ashore=1&start=space-bar",
        "/map?death=suffocated&dock=the-tilt&land=1",
        "/map?death=impact",
        "/map?found=1&land=1&floor=17&card=all",
        "/map?threads=1&watch=5",
        // the sanitizers, and a query of nothing this page has ever heard of
        "/map?scenario=sol-eu",
        "/map?scenario=..%2Foops",
        // …every key handed nothing. NOT `?scenario=`: an empty scenario name passes the slug check
        // (vacuously — "" is all-ASCII-letters-or-digits) and the boot then fetches `scenarios/.json`,
        // 404s and dies before it builds anything. That is the shipped behaviour on this base and this
        // lane does not change it; it is written up in the PR body instead of pinned here.
        "/map?start=&dock=&fuel=&nerve=&site=&land=",
        "/map?nonsense=1&start=there-is-no-such-start&dock=NOT+A+HAVEN&site=-3&floor=0",
        // the excursion dials nothing else sets
        "/map?dock=the-tilt&site=0&land=1&air=45&process=0&collectors=20&hurt=2&nerve=low",
        "/map?secretlab=deep&land=1&floor=2&book=9&dark=1&roll=lo&approach=0&neighbour=1",
        "/map?wreck=infested&land=1&sweep=3&mags=0&reevers=4",
        "/map?archive=1&land=1&nerve=2",
        "/map?deflection=s&expedition=science&watchers=1&outpost=1&kit=1",
        "/map?bond=1&oracle=1&converge=1&kaamos=all&nebula=all",
        "/map?kaamos=pod&nebula=adjuster&arrivalphase=7",
    ];

    // ── The guard ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EveryBootUrlBuildsTheWorldItAlwaysBuilt()
    {
        string? dump = Environment.GetEnvironmentVariable(DumpVariable);
        var dumped = new StringBuilder();
        var wrong = new List<string>();
        var seen = new List<string>();

        foreach (string url in EveryBootUrl())
        {
            string rendered = await TheWorldBuiltBy(url);
            string hash = Sha256(rendered);
            seen.Add(url);

            if (dump is not null)
            {
                dumped.Append("            [\"").Append(url).Append("\"] = \"").Append(hash).Append("\",\n");
                File.AppendAllText(dump + ".full", $"───── {url}\n{rendered}\n\n");
                continue;
            }

            Assert.True(TheWorldEachUrlBuilds.ContainsKey(url),
                $"{url} is not pinned. A new boot URL must be fingerprinted before it can be trusted.");
            if (!string.Equals(TheWorldEachUrlBuilds[url], hash, StringComparison.Ordinal))
            {
                wrong.Add($"{url}\n  pinned {TheWorldEachUrlBuilds[url]}\n  built  {hash}\n"
                    + Indent(rendered));
            }
        }

        if (dump is not null)
        {
            File.WriteAllText(dump, dumped.ToString());
            return;
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {seen.Count} boot URLs no longer build the world they built before the "
            + "split. Every line below is a field whose value the boot changed:\n\n"
            + string.Join("\n\n", wrong));
    }

    [Fact]
    public void ThePinnedListIsTheWHOLEDevStartCatalogue()
    {
        // The fifth bug class, applied to this file: a fingerprint sweep over an empty list is green and
        // says nothing. Every URL the game's own front door offers has to be in the pinned dictionary, and
        // the dictionary may not carry a URL nobody boots.
        string[] booted = [.. EveryBootUrl()];

        Assert.All(SpaceSails.Core.DevStarts.All.Select(e => e.Url).Distinct(StringComparer.Ordinal),
            url => Assert.Contains(url, booted, StringComparer.Ordinal));
        Assert.Equal(
            booted.OrderBy(u => u, StringComparer.Ordinal),
            TheWorldEachUrlBuilds.Keys.OrderBy(u => u, StringComparer.Ordinal));
        Assert.True(booted.Length >= 60, $"only {booted.Length} boot URLs — the sweep has shrunk.");
    }

    [Fact]
    public async Task TheFingerprintCanTellTwoWORLDSApart()
    {
        // …and the other half of the same law: a fingerprint that answered the same thing for every URL
        // would pin nothing at all. Two boots that build genuinely different worlds must differ, and the
        // SAME boot twice must not — which is also this sweep's determinism proof.
        string bare = await TheWorldBuiltBy("/map");
        string again = await TheWorldBuiltBy("/map");
        string elsewhere = await TheWorldBuiltBy("/map?dock=the-tilt&site=0&land=1");

        Assert.Equal(Sha256(bare), Sha256(again));
        Assert.NotEqual(Sha256(bare), Sha256(elsewhere));
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────

    internal const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>Boot the SHIPPING component at <paramref name="url"/> and render everything it changed.
    ///
    /// <para>The boot ends in a throw, always and by design: its last stage names DOM by id through
    /// <c>renderer.js</c> and there is no browser under a test runner. That throw is the fingerprint's
    /// horizon, not its failure — but a throw from anywhere EARLIER would be a boot that never built a
    /// world, so the world the bench renders is checked for a pulse before it is hashed.</para></summary>
    private static async Task<string> TheWorldBuiltBy(string url)
    {
        var booted = new Pages.Map();
        NeverRender(booted);
        Hand(booted, "Http", ScenariosFromDisk());
        Hand(booted, "Navigation", new Bench(url));

        MethodInfo boot = typeof(Pages.Map).GetMethod("BootTheWorldAsync", Hidden)
            ?? throw new InvalidOperationException("Map has no BootTheWorldAsync to fingerprint.");
        try
        {
            await (Task)boot.Invoke(booted, [CancellationToken.None])!;
        }
        catch (TargetInvocationException)
        {
            // the browser gate, reached synchronously
        }
        catch (Exception)
        {
            // the browser gate, reached from a continuation
        }

        Assert.True(Field(booted, "_ephemeris") is not null,
            $"{url}: the boot stopped before it built an ephemeris — this is not the browser gate, it is a "
            + "world that was never built, and hashing it would pin a boot that fell over.");

        return Rendered(booted);
    }

    /// <summary>The two services the bench hands the component. They are not the boot's work and one of
    /// them is the URL itself, so they are held out of the fingerprint by name.</summary>
    private static readonly HashSet<string> TheBenchsOwnDoing =
        new(StringComparer.Ordinal) { "<Http>k__BackingField", "<Navigation>k__BackingField" };

    /// <summary>Every instance field the boot CHANGED, in name order, one per line.</summary>
    private static string Rendered(Pages.Map booted)
    {
        var virgin = new Pages.Map();
        var lines = new List<string>();
        foreach (FieldInfo field in typeof(Pages.Map)
                     .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     .Where(f => !TheBenchsOwnDoing.Contains(f.Name))
                     .OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            string before = Render(Value(field, virgin));
            string after = Render(Value(field, booted));
            if (!string.Equals(before, after, StringComparison.Ordinal))
            {
                lines.Add($"{field.Name} = {after}");
            }
        }
        return string.Join("\n", lines);
    }

    private static object? Value(FieldInfo field, object on)
    {
        try
        {
            return field.GetValue(on);
        }
        catch (Exception ex)
        {
            return $"<unreadable: {ex.GetType().Name}>";
        }
    }

    private static object? Field(Pages.Map map, string name) =>
        typeof(Pages.Map).GetField(name, Hidden)?.GetValue(map);

    // ── The renderer: a value to a stable string, the same way every time ────────────────────────────

    internal static string Render(object? value) =>
        Render(value, depth: 0, new HashSet<object>(ReferenceEqualityComparer.Instance));

    private static string Render(object? value, int depth, HashSet<object> walking)
    {
        switch (value)
        {
            case null: return "∅";
            case string s: return $"\"{s}\"";
            case bool b: return b ? "true" : "false";
            case double d: return d.ToString("R", CultureInfo.InvariantCulture);
            case float f: return f.ToString("R", CultureInfo.InvariantCulture);
            case decimal m: return m.ToString(CultureInfo.InvariantCulture);
            case Enum e: return e.ToString();
            case DateTime dt: return dt.ToString("O", CultureInfo.InvariantCulture);
            case TimeSpan ts: return ts.ToString("c", CultureInfo.InvariantCulture);
            case IFormattable n when value.GetType().IsPrimitive:
                return n.ToString(null, CultureInfo.InvariantCulture);
        }

        Type type = value.GetType();
        if (depth >= 6)
        {
            return $"<{type.Name}…>";
        }
        if (!type.IsValueType && !walking.Add(value))
        {
            return $"<{type.Name} again>";
        }

        try
        {
            if (value is IDictionary map)
            {
                var pairs = new List<string>();
                foreach (DictionaryEntry entry in map)
                {
                    pairs.Add($"{Render(entry.Key, depth + 1, walking)}: {Render(entry.Value, depth + 1, walking)}");
                }
                pairs.Sort(StringComparer.Ordinal);
                return $"{{{string.Join(", ", pairs)}}}";
            }

            if (value is IEnumerable list)
            {
                var items = new List<string>();
                foreach (object? item in list)
                {
                    items.Add(Render(item, depth + 1, walking));
                }
                // a set has no order of its own, so give it one; a list's order IS its content.
                if (IsASet(type))
                {
                    items.Sort(StringComparer.Ordinal);
                }
                // a long run of numbers is folded to a digest so the text stays a text.
                return items.Count > 64
                    ? $"[{items.Count} × {Sha256(string.Join(",", items))}]"
                    : $"[{string.Join(", ", items)}]";
            }

            if (IsOurs(type))
            {
                var members = new List<string>();
                foreach (PropertyInfo property in type
                             .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                             .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                             .OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    members.Add($"{property.Name}={Read(() => property.GetValue(value), depth, walking)}");
                }
                foreach (FieldInfo field in type
                             .GetFields(BindingFlags.Instance | BindingFlags.Public)
                             .OrderBy(f => f.Name, StringComparer.Ordinal))
                {
                    members.Add($"{field.Name}={Read(() => field.GetValue(value), depth, walking)}");
                }
                return $"{type.Name}({string.Join(", ", members)})";
            }

            return $"<{type.FullName}>";
        }
        finally
        {
            if (!type.IsValueType)
            {
                walking.Remove(value);
            }
        }
    }

    private static string Read(Func<object?> get, int depth, HashSet<object> walking)
    {
        try
        {
            return Render(get(), depth + 1, walking);
        }
        catch (Exception ex)
        {
            return $"<threw {ex.GetType().Name}>";
        }
    }

    private static bool IsASet(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(ISet<>));

    /// <summary>Ours gets walked; everything else is named and left alone — a <c>CancellationTokenSource</c>
    /// or an <c>HttpClient</c> has no content a fingerprint wants and plenty it could not repeat.
    ///
    /// <para>The bench's OWN types are excluded by name, not by accident: this project's namespace also
    /// begins with <c>SpaceSails</c>, and walking the injected <c>NavigationManager</c> would put the URL
    /// under test INSIDE the fingerprint — which would make every URL's hash trivially unique and the
    /// whole sweep a test that cannot fail.</para>
    ///
    /// <para>Tuples are walked too. Two of the boot's fields (<c>_pendingExpeditionCheat</c>,
    /// <c>_pendingDeflectionCheat</c>) are tuples of twelve and four, and a rule that named them by type
    /// would answer the same thing for every rock the deflection cheat ever spawns.</para></summary>
    private static bool IsOurs(Type type) =>
        (type.Namespace?.StartsWith("SpaceSails", StringComparison.Ordinal) == true
            && type.Namespace?.StartsWith("SpaceSails.Client.Tests", StringComparison.Ordinal) != true)
        || type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true
        || type.FullName?.StartsWith("System.Tuple`", StringComparison.Ordinal) == true;

    // ── The services the page injects ────────────────────────────────────────────────────────────────

    internal static void NeverRender(Pages.Map map)
    {
        // A ComponentBase that was never attached to a renderer throws out of StateHasChanged, and the boot
        // calls it five times. Telling the component it already has a render queued is the framework's own
        // early-out; nothing else about it is faked.
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);
    }

    internal static void Hand(Pages.Map map, string property, object service) =>
        typeof(Pages.Map).GetProperty(property, Hidden)!.SetValue(map, service);

    internal static HttpClient ScenariosFromDisk() =>
        new(new FromDisk()) { BaseAddress = new Uri("http://localhost/") };

    internal sealed class Bench : NavigationManager
    {
        public Bench(string url) => Initialize("http://localhost/", "http://localhost" + url);
    }

    /// <summary>The real scenario files, off the real repo — the page fetches <c>scenarios/&lt;name&gt;.json</c>
    /// and a scenario nobody shipped must 404 here exactly as it would in the browser.</summary>
    private sealed class FromDisk : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string relative = request.RequestUri!.AbsolutePath.TrimStart('/');
            string path = Path.Combine(RepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
            return Task.FromResult(File.Exists(path)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(File.ReadAllText(path)) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    internal static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    internal static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32].ToLowerInvariant();

    private static string Indent(string text) =>
        string.Join("\n", text.Split('\n').Select(l => "    " + l));
}
