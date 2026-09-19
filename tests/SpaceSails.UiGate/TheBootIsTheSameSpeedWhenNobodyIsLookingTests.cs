using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1244 · <b>A BOOT BEHIND ANOTHER TAB IS THE SAME BOOT.</b>
///
/// <para><b>The sighting</b> (owner, live build, 2026-09-19, a Chrome window that was not in front): every
/// slice of the staged boot cost about a second —</para>
/// <code>
/// [SpaceSails] boot · the traffic lanes — freighter 1 of 8 — 1086 ms (t+…)
/// [SpaceSails] boot · the traffic lanes — freighter 2 of 8 — 989 ms
/// [SpaceSails] boot · the traffic lanes — freighter 3 of 8 — 860 ms
/// </code>
/// <para>— eighty-two slices in two and a half minutes and still going, against ~26 ms each (#1203's
/// numbers) for the same URL in a tab the captain was looking at. The slices were never slow. The YIELD
/// between them was: Chrome rations a hidden document's timers to roughly one a second, and the boot's one
/// hand-back parks on a browser timer by design (#318 chose <c>Task.Delay(1)</c> over <c>Task.Yield</c>
/// precisely because it does). The finer #1114/#1203 sliced the boot, the worse that got.</para>
///
/// <para><b>WHAT ACTUALLY MAKES A HEADLESS PAGE HIDDEN — the answer is nothing, and it is worth writing
/// down.</b> Probed on this branch, Chromium 1.5x headless, both with Playwright's default arguments and
/// with the three that suppress throttling handed back
/// (<c>--disable-background-timer-throttling</c>, <c>--disable-backgrounding-occluded-windows</c>,
/// <c>--disable-renderer-backgrounding</c>):</para>
/// <code>
/// alone:                 visibility=visible hidden=false  5 timers=10ms  5 ticks=0ms
/// behind a 2nd page:     visibility=visible hidden=false  5 timers= 6ms  5 ticks=0ms
/// after the CDP calls:   visibility=visible hidden=false  5 timers= 6ms  5 ticks=0ms
/// </code>
/// <list type="bullet">
///   <item><c>page.BringToFrontAsync()</c> on a second page does NOT hide the first — headless has no tab
///   strip, so every page stays <c>visible</c>.</item>
///   <item><c>Emulation.setFocusEmulationEnabled</c> is accepted and changes nothing here: it forces focus
///   ON, which is the opposite of this lane's question.</item>
///   <item><c>Page.setWebLifecycleState</c> rejects <c>"hidden"</c> outright — <i>"Unidentified lifecycle
///   state"</i>; it takes only <c>frozen</c> and <c>active</c>, and <c>frozen</c> stops the world rather
///   than rationing it.</item>
///   <item>A headless renderer never throttles a timer at all, with or without those flags.</item>
/// </list>
///
/// <para><b>So the gate does to the page exactly what Chrome does to a background document, in an init
/// script, and says so out loud.</b> Two things, no more: <c>document.visibilityState</c> reads
/// <c>hidden</c>, and <c>setTimeout</c>/<c>setInterval</c> are rationed to one call a second. Both are
/// Chrome's documented background-tab behaviour and both are what the owner measured. It is an emulation,
/// and an emulation can lie — so this guard <b>asserts its own premise from inside the page after the
/// boot</b>: the document really did read hidden, a five-deep timer chain really did cost five seconds, and
/// the boot really did stage itself into a couple of dozen slices. A run where any of those collapsed would
/// be a green test about nothing.</para>
///
/// <para><b>The law is a comparison, not a stopwatch reading</b>, because this project's two payloads differ
/// by ~100× (interpreted local vs the AOT build CI ships) and a single absolute number can only be honest
/// for one of them. The same URL is booted twice in the same run — once ordinarily, once hidden — and the
/// hidden boot may cost no more than <see cref="HiddenBootRatio"/>× the visible one plus
/// <see cref="HiddenBootSlackMs"/>. The absolute ceiling #1244 asks for (the payload's own boot budget,
/// doubled) is asserted as well, as a backstop.</para>
///
/// <para><b>RED PROOF</b>, run on this branch: <c>git checkout HEAD~1 -- src/</c>, republish, and point
/// <c>SPACESAILS_PUBLISH_DIR</c> at it — the numbers are in the PR body.</para>
/// </summary>
public sealed class TheBootIsTheSameSpeedWhenNobodyIsLookingTests(Xunit.Abstractions.ITestOutputHelper output)
    : IAsyncLifetime
{
    private const float BootTimeoutMs = 600_000;

    /// <summary>A berth-naming boot: it skips the front door, so the whole of it is world-building, and it
    /// is the family that used to reach the planners before the renderer module had even landed.</summary>
    private const string Url = "/map?scenario=sol&start=wreck&dest=saturn";

    /// <summary>How much dearer a boot nobody is looking at may be. The whole cost of being hidden ought to
    /// be nothing; two-fold leaves room for a contended runner sharing four Chromiums, and is nowhere near
    /// the forty-fold the sighting measured.</summary>
    private const double HiddenBootRatio = 2.0;

    /// <summary>…plus a flat allowance, because the hidden boot still pays ONE rationed timer before it
    /// knows it is being rationed, and because a ratio alone is a flake-trap on a fast payload.</summary>
    private const long HiddenBootSlackMs = 6_000;

    /// <summary>The absolute backstop #1244 names, as a multiple of the payload's own boot budget from
    /// <c>BootAndReachabilityTests</c> (AOT 20 s · interpreted 150 s).</summary>
    private const double BudgetHeadroom = 2.0;

    private const long AotBootBudgetMs = 20_000;
    private const long InterpretedBootBudgetMs = 150_000;
    private const long AotNativeWasmThresholdBytes = 6_000_000;

    /// <summary>
    /// CHROME'S BACKGROUND TAB, IN AN INIT SCRIPT — because headless will not give us one (see the class
    /// comment). Installed before a single page script runs, so the .NET WASM runtime's own timer queue
    /// picks up the rationed <c>setTimeout</c> along with everyone else's.
    ///
    /// <para><c>MessageChannel</c> is deliberately left ALONE: a message is a task and not a timer, and no
    /// browser rations it. That is the whole of what this lane changed, so a fix that did not reach for one
    /// cannot pass this.</para>
    /// </summary>
    private const string BackgroundTabScript = """
        (() => {
            const RATION_MS = 1000;
            const timeout = globalThis.setTimeout.bind(globalThis);
            const interval = globalThis.setInterval.bind(globalThis);
            globalThis.setTimeout = (fn, ms, ...rest) =>
                timeout(fn, Math.max(Number(ms) || 0, RATION_MS), ...rest);
            globalThis.setInterval = (fn, ms, ...rest) =>
                interval(fn, Math.max(Number(ms) || 0, RATION_MS), ...rest);
            Object.defineProperty(document, 'visibilityState', { get: () => 'hidden', configurable: true });
            Object.defineProperty(document, 'hidden', { get: () => true, configurable: true });
        })();
        """;

    /// <summary>Read back INSIDE the page once the boot is over: was the tab really hidden, and were the
    /// timers really rationed? The premise of the whole guard, measured rather than assumed.</summary>
    private const string PremiseProbe = """
        async () => {
            const start = performance.now();
            for (let i = 0; i < 5; i++) { await new Promise(r => setTimeout(r, 1)); }
            return JSON.stringify({
                hidden: document.hidden === true && document.visibilityState === 'hidden',
                fiveTimersMs: Math.round(performance.now() - start),
            });
        }
        """;

    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;
    private readonly StringBuilder _log = new();

    public async Task InitializeAsync()
    {
        _host = await ClientHost.StartAsync(new StringWriter(_log));
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _pw = await Playwright.CreateAsync();

        // Playwright suppresses background throttling by default; handing those three arguments back costs
        // nothing here (headless never throttles anyway, see the class comment) and means this browser is
        // not quietly configured against the very thing the lane is about.
        _browser = await _pw.Chromium.LaunchAsync(new()
        {
            Headless = true,
            IgnoreDefaultArgs =
            [
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-renderer-backgrounding",
            ],
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    [Fact]
    public async Task A_boot_nobody_is_looking_at_costs_what_a_boot_someone_is_looking_at_costs()
    {
        (long visibleMs, int visibleSlices, string _) = await BootOnceAsync(hidden: false);
        (long hiddenMs, int hiddenSlices, string premise) = await BootOnceAsync(hidden: true);

        string story =
            $"\nvisible: {visibleMs} ms over {visibleSlices} staged slice(s)"
            + $"\nhidden:  {hiddenMs} ms over {hiddenSlices} staged slice(s)"
            + $"\npremise inside the hidden page: {premise}\n";

        // Logged on EVERY run, pass or fail — the same free perf time-series the load-speed budget keeps
        // (see the README), and the only place a CI log will say what being hidden actually costs.
        output.WriteLine("[#1244 hidden-boot]" + story);

        // ── The premises, out loud. Any of these collapsing makes the law below green about nothing. ──
        Assert.True(premise.Contains("\"hidden\":true", StringComparison.Ordinal),
            "the hidden boot's page did not actually read as hidden, so this guard drove an ordinary tab "
            + "and proved nothing about #1244." + story);

        int fiveTimersMs = int.Parse(
            premise.Split("\"fiveTimersMs\":")[1].TrimEnd('}'),
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(fiveTimersMs >= 4_000,
            $"five chained timers cost {fiveTimersMs} ms inside the 'hidden' page — they were NOT being "
            + "rationed the way Chrome rations a background tab, so the condition #1244 is about was never "
            + "reproduced." + story);

        Assert.True(hiddenSlices >= 20,
            $"the boot printed only {hiddenSlices} staged slice line(s). #1244 is a ration paid ONCE PER "
            + "SLICE — a boot that stopped staging itself would pass this while the bug it is about had "
            + "simply moved." + story);

        // ── The law ───────────────────────────────────────────────────────────────────────────────────
        long allowed = (long)(visibleMs * HiddenBootRatio) + HiddenBootSlackMs;
        Assert.True(hiddenMs <= allowed,
            $"#1244 — the same URL booted in {hiddenMs} ms with nobody looking against {visibleMs} ms in "
            + $"front of a captain (allowed: {allowed} ms). The boot is paying a rationed browser timer at "
            + "every one of its staged hand-backs instead of a message tick." + story);

        long ceiling = (long)(BootBudgetForThisPayload() * BudgetHeadroom);
        Assert.True(hiddenMs <= ceiling,
            $"#1244 — a hidden boot took {hiddenMs} ms, past this payload's doubled boot budget "
            + $"({ceiling} ms)." + story);
    }

    /// <summary>One boot in a brand-new context (a shared origin would carry the last voyage's
    /// <c>localStorage</c> into the next one), timed from the navigation to the boot door coming down, with
    /// the boot's own staged-slice lines counted off the console as they arrive.</summary>
    private async Task<(long Ms, int Slices, string Premise)> BootOnceAsync(bool hidden)
    {
        IBrowserContext context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
        });
        try
        {
            if (hidden)
            {
                await context.AddInitScriptAsync(BackgroundTabScript);
            }

            IPage page = await context.NewPageAsync();
            int slices = 0;
            page.Console += (_, msg) =>
            {
                if (msg.Text.Contains("[SpaceSails] boot ·", StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref slices);
                }
            };

            var clock = Stopwatch.StartNew();
            await page.GotoAsync(_host.BaseUrl + Url, new() { Timeout = BootTimeoutMs });
            await page.BootDoorClosedAsync(BootTimeoutMs);
            clock.Stop();

            string premise = hidden ? await page.EvaluateAsync<string>(PremiseProbe) : "(not probed)";
            return (clock.ElapsedMilliseconds, slices, premise);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>The same payload sniff <c>BootAndReachabilityTests</c> uses — AOT compiles managed IL to
    /// native wasm, ballooning <c>dotnet.native.*.wasm</c> from ~1.5 MB to ~18 MB — so the two builds get
    /// the two budgets that were measured for them instead of one number that fits neither.</summary>
    private long BootBudgetForThisPayload()
    {
        try
        {
            var framework = new DirectoryInfo(Path.Combine(_host.WwwrootPath, "_framework"));
            bool aot = framework.Exists && framework
                .GetFiles("dotnet.native*.wasm")
                .Any(f => f.Length >= AotNativeWasmThresholdBytes);
            return aot ? AotBootBudgetMs : InterpretedBootBudgetMs;
        }
        catch (IOException)
        {
            return InterpretedBootBudgetMs;
        }
    }
}
