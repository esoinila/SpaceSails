using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #251 · <b>THE PAGE'S SHEET, AS THE BROWSER RESOLVED IT, PINNED.</b> The computed-style half of the proof
/// the owner's 2026-09-03 ruling needs before <c>Map.razor.css</c> may be regrouped (<i>"CSS → flow column"</i>).
///
/// <para><c>TheBundleIsTheSameCascadeTests</c> holds the ORDER of the rules — which of two equal-specificity
/// rules comes later — and says in its own header it is not a snapshot of values. What a regroup can break is
/// what the browser DOES with that order: an element whose winning declaration changes because a rule moved
/// past its rival. That is a question only a real layout answers, so it lives here, in a real Chromium, on the
/// published artifact.</para>
///
/// <para><b>What is read.</b> Every selector the sheet itself writes — parsed off <c>Pages/Map.razor.css</c>,
/// never typed here — is asked of the live page in a fixed list of states: the docked ship at each of its six
/// desks, ashore in the bar, Down Below, the Hive park, a Hive floor, and a ground. For every element a
/// selector matches, the COMPUTED value of each property that selector's own rules declare (from a fixed set:
/// display, position, inset, size, z-index, overflow, colour, background, font, flex and grid) is read back,
/// with animations and transitions held still so a pulse is read at its declared value rather than mid-beat.
/// Per state, per selector, per property, the distinct values are written sorted — so an element that comes
/// and goes between two identical values cannot flake the pin, and a rule that loses its fight can.</para>
///
/// <para><b>How it is used.</b> Run with <c>SPACESAILS_CSS_PIN_DUMP=&lt;file&gt;</c> to write the reading;
/// without it the reading is compared with <c>MapSheetComputedStyle.baseline.txt</c> line by line. A pure move
/// of whole rule blocks must leave it unchanged; a deliberate style change updates it in the same commit.</para>
/// </summary>
public sealed class TheSheetComputesTheSameStyleTests : IAsyncLifetime
{
    private const float BootTimeoutMs = 240_000;
    private const float ActionTimeoutMs = 60_000;
    private const string BaselineFile = "MapSheetComputedStyle.baseline.txt";
    private const string DumpVariable = "SPACESAILS_CSS_PIN_DUMP";

    /// <summary>The longhands read, and the declared properties (shorthands included) that ask for each.</summary>
    private static readonly Dictionary<string, string[]> Asks = new(StringComparer.Ordinal)
    {
        ["display"] = ["display"],
        ["position"] = ["position"],
        ["inset"] = ["top", "right", "bottom", "left"],
        ["top"] = ["top"], ["right"] = ["right"], ["bottom"] = ["bottom"], ["left"] = ["left"],
        ["width"] = ["width"], ["height"] = ["height"],
        ["min-width"] = ["min-width"], ["max-width"] = ["max-width"],
        ["min-height"] = ["min-height"], ["max-height"] = ["max-height"],
        ["z-index"] = ["z-index"],
        ["overflow"] = ["overflow-x", "overflow-y"],
        ["overflow-x"] = ["overflow-x"], ["overflow-y"] = ["overflow-y"],
        ["color"] = ["color"],
        ["background"] = ["background-color", "background-image"],
        ["background-color"] = ["background-color"], ["background-image"] = ["background-image"],
        ["font"] = ["font-family", "font-size", "font-weight", "font-style"],
        ["font-family"] = ["font-family"], ["font-size"] = ["font-size"],
        ["font-weight"] = ["font-weight"], ["font-style"] = ["font-style"],
        ["flex"] = ["flex-grow", "flex-shrink", "flex-basis"],
        ["flex-direction"] = ["flex-direction"], ["flex-wrap"] = ["flex-wrap"],
        ["flex-flow"] = ["flex-direction", "flex-wrap"],
        ["justify-content"] = ["justify-content"], ["align-items"] = ["align-items"],
        ["gap"] = ["row-gap", "column-gap"],
        ["grid-template-columns"] = ["grid-template-columns"], ["grid-template-rows"] = ["grid-template-rows"],
    };

    /// <summary>The states, in order. Each is a boot URL and, optionally, a desk tab pressed after it.</summary>
    private static readonly (string Name, string Url, string? Desk)[] States =
    [
        ("docked · Nav", "/map?dock=selene-gate&holdbeats=1", null),
        ("docked · Captain", "/map?dock=selene-gate&holdbeats=1", "Captain"),
        ("docked · Sensors", "/map?dock=selene-gate&holdbeats=1", "Sensors"),
        ("docked · War room", "/map?dock=selene-gate&holdbeats=1", "War room"),
        ("docked · Trade", "/map?dock=selene-gate&holdbeats=1", "Trade"),
        ("docked · Comms", "/map?dock=selene-gate&holdbeats=1", "Comms"),
        ("ashore · the bar", "/map?dock=selene-gate&ashore=1&holdbeats=1", null),
        ("ashore · Down Below", "/map?dock=selene-gate&ashore=1&havenfloor=-1&holdbeats=1", null),
        ("the Hive park", "/map?park=1&holdbeats=1", null),
        ("a Hive floor", "/map?secretlab=deep&land=1&floor=1&holdbeats=1", null),
        ("a ground", "/map?dock=the-tilt&site=0&land=1&holdbeats=1", null),
    ];

    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _host = await ClientHost.StartAsync(TextWriter.Null);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    [Fact]
    public async Task Every_rule_the_page_sheet_writes_computes_what_it_computed()
    {
        IReadOnlyList<(string Selector, string[] Props)> asked = WhatTheSheetAsks();
        Assert.True(asked.Count > 50, $"only {asked.Count} selectors were read off Map.razor.css — the parser is blind.");

        var lines = new List<string>();
        int matchedAnywhere = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        IPage? page = null;
        string? bootedUrl = null;

        foreach ((string name, string url, string? desk) in States)
        {
            if (page is null || bootedUrl != url)
            {
                if (page is not null)
                {
                    await page.CloseAsync();
                }
                page = await _browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
                await page.GotoAsync(_host.BaseUrl + url, new() { Timeout = BootTimeoutMs });
                await page.BootDoorClosedAsync(BootTimeoutMs);
                await page.Locator(".map-page").WaitForAsync(
                    new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
                ILocator plate = page.Locator(".story-plate-close");
                if (await plate.CountAsync() > 0 && await plate.First.IsVisibleAsync())
                {
                    await plate.First.ClickAsync(new() { Timeout = ActionTimeoutMs });
                }
                bootedUrl = url;
            }

            if (desk is not null)
            {
                await page.Locator("button.desk-tab", new() { HasTextString = desk }).First
                    .ClickAsync(new() { Timeout = ActionTimeoutMs });
            }

            await page.SettledAsync(".map-page");
            await page.WaitForTimeoutAsync(500);

            string json = await page.EvaluateAsync<string>(ReadScript,
                asked.Select(a => new { sel = a.Selector, props = a.Props }).ToArray());
            using JsonDocument doc = JsonDocument.Parse(json);
            foreach (JsonElement row in doc.RootElement.EnumerateArray())
            {
                string sel = row.GetProperty("sel").GetString()!;
                string prop = row.GetProperty("prop").GetString()!;
                string values = string.Join(" ‖ ", row.GetProperty("values").EnumerateArray().Select(v => v.GetString()));
                lines.Add($"{name}\t{sel}\t{prop}\t{values}");
                if (seen.Add(sel))
                {
                    matchedAnywhere++;
                }
            }
        }

        if (page is not null)
        {
            await page.CloseAsync();
        }

        Assert.True(matchedAnywhere > 20,
            $"only {matchedAnywhere} of the sheet's selectors matched anything in {States.Length} states — "
            + "the pin would be a pin of nothing.");

        string reading = string.Join("\n", lines) + "\n";
        string baselinePath = Path.Combine(RepoRoot(), "tests", "SpaceSails.UiGate", BaselineFile);

        if (Environment.GetEnvironmentVariable(DumpVariable) is { Length: > 0 } dump)
        {
            await File.WriteAllTextAsync(dump, reading);
            return;
        }

        string[] want = File.ReadAllText(baselinePath).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        string[] got = reading.TrimEnd('\n').Split('\n');
        int first = -1;
        for (int i = 0; i < Math.Min(want.Length, got.Length); i++)
        {
            if (!string.Equals(want[i], got[i], StringComparison.Ordinal))
            {
                first = i;
                break;
            }
        }

        Assert.True(first < 0 && want.Length == got.Length,
            "#251 · the page's sheet no longer computes what it computed.\n\n"
            + (first >= 0
                ? $"  first difference at line {first + 1}:\n    baseline: {want[first]}\n    computed: {got[first]}\n\n"
                : $"  the readings agree for {Math.Min(want.Length, got.Length)} lines and then one ends: "
                  + $"baseline {want.Length}, computed {got.Length}.\n\n")
            + $"{BaselineFile} is every Map.razor.css selector's computed style across {States.Length} live states. "
            + "A regroup of whole rule blocks must leave it byte for byte; a deliberate style change updates it "
            + $"in the same commit (run with {DumpVariable}=<file> to write the new reading).");
    }

    // ── THE SHEET, READ ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Every selector <c>Map.razor.css</c> writes, with the longhands its own declarations ask for. Comments
    /// are stripped; <c>@keyframes</c> blocks are skipped; a comma list is split at the top level;
    /// <c>::deep</c> is dropped (it is Blazor's scope combinator, not a selector); interaction pseudo-classes
    /// (<c>:hover</c>, <c>:focus…</c>, <c>:active</c>) are dropped so the element is read in its resting state;
    /// a selector with a pseudo-ELEMENT is skipped, because it has no element to ask.
    /// </summary>
    internal static IReadOnlyList<(string Selector, string[] Props)> WhatTheSheetAsks()
    {
        string css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));
        css = Regex.Replace(css, @"/\*.*?\*/", " ", RegexOptions.Singleline);

        var bySelector = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var order = new List<string>();
        int i = 0;
        while (i < css.Length)
        {
            int open = css.IndexOf('{', i);
            if (open < 0)
            {
                break;
            }
            string prelude = css[i..open].Trim();
            int depth = 1, j = open + 1;
            while (j < css.Length && depth > 0)
            {
                depth += css[j] == '{' ? 1 : css[j] == '}' ? -1 : 0;
                j++;
            }
            string body = css[(open + 1)..(j - 1)];
            i = j;

            if (prelude.StartsWith('@'))
            {
                continue;   // @keyframes — no element to ask
            }

            var props = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string decl in body.Split(';'))
            {
                int colon = decl.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }
                string p = decl[..colon].Trim().ToLowerInvariant();
                if (Asks.TryGetValue(p, out string[]? longhands))
                {
                    foreach (string l in longhands)
                    {
                        props.Add(l);
                    }
                }
            }
            if (props.Count == 0)
            {
                continue;
            }

            foreach (string raw in SplitTopLevel(prelude))
            {
                if (raw.Contains("::", StringComparison.Ordinal) && !raw.Contains("::deep", StringComparison.Ordinal)
                    || Regex.IsMatch(raw, @"::(before|after|placeholder|selection|-webkit-)"))
                {
                    continue;
                }
                string sel = Regex.Replace(raw, @"::deep", " ");
                sel = Regex.Replace(sel, @":(hover|focus-visible|focus-within|focus|active)\b", "");
                sel = Regex.Replace(sel, @"\s+", " ").Trim();
                if (sel.Length == 0)
                {
                    continue;
                }
                if (!bySelector.TryGetValue(sel, out SortedSet<string>? set))
                {
                    bySelector[sel] = set = new SortedSet<string>(StringComparer.Ordinal);
                    order.Add(sel);
                }
                set.UnionWith(props);
            }
        }

        return [.. order.Select(s => (s, bySelector[s].ToArray()))];
    }

    private static IEnumerable<string> SplitTopLevel(string prelude)
    {
        int depth = 0, start = 0;
        for (int k = 0; k < prelude.Length; k++)
        {
            depth += prelude[k] == '(' ? 1 : prelude[k] == ')' ? -1 : 0;
            if (prelude[k] == ',' && depth == 0)
            {
                yield return prelude[start..k].Trim();
                start = k + 1;
            }
        }
        yield return prelude[start..].Trim();
    }

    private static string RepoRoot()
    {
        for (DirectoryInfo? at = new(AppContext.BaseDirectory); at is not null; at = at.Parent)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    // Runs in the page. Holds every animation and transition still first, so a pulse is read at its declared
    // value; then asks each selector for its elements and each element for its computed longhands. Returns
    // one row per (selector, property) that matched anything, with the distinct values sorted.
    private const string ReadScript = @"
        (asked) => {
            if (!document.getElementById('css-pin-still')) {
                const still = document.createElement('style');
                still.id = 'css-pin-still';
                still.textContent = '*, *::before, *::after { animation: none !important; transition: none !important; }';
                document.head.appendChild(still);
            }
            const rows = [];
            for (const a of asked) {
                let els;
                try { els = document.querySelectorAll(a.sel); } catch { continue; }
                if (els.length === 0) { continue; }
                for (const p of a.props) {
                    const vals = new Set();
                    for (const el of els) { vals.add(getComputedStyle(el).getPropertyValue(p)); }
                    rows.push({ sel: a.sel, prop: p, values: [...vals].sort() });
                }
            }
            return JSON.stringify(rows);
        }";
}
