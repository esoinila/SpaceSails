using System.Text;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

// Lab 34 / issue #293 — "the rescue button must always be pressable."
//
// The owner kept getting bitten by controls that were PRESENT but not PRESSABLE (the out-of-power
// rescue pill buried under the masthead), and — cruising 2026-07-19, approving PRs from his phone —
// asked for the boot itself to be under test: "there would have to be more testing done here."
//
// This is that gate. One headless Chromium boots the published game the whole way a player does —
// front page → Launch Sol → wait out the (slow, interpreted-WASM) boot → start a new voyage — and
// then walks a SMALL, STABLE canary of critical controls, asking Playwright's own actionability
// engine the exact question the owner cares about: would a real click land on this control, or is
// something covering it? Trial clicks answer that without firing the control's side effect.
//
// Kept deliberately narrow (a canary, not an E2E suite): boot + desk switching + the captain's
// "Set course" + the pilot banner. The rescue pill's z-band itself is proven browser-free by the
// companion law (SpaceSails.Core.RescueLifeline, Lab 34 approach (b)); this proves the live boot
// path those laws assume actually stands up.
public sealed class BootAndReachabilityTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish (no AOT) is CPU-heavy while the sim loop spins, so
    // CDP round-trips can legitimately run long. Generous, signal-keyed waits — never sleeps.
    private static readonly float BootTimeoutMs = 180_000;
    private static readonly float ActionTimeoutMs = 60_000;

    // Console noise that is NOT a boot failure. Anything else on the error channel fails the gate.
    private static readonly string[] BenignConsole =
    [
        "favicon", "Failed to load resource", "net::ERR_", "sourcemap", "source map", "DevTools",
    ];

    private readonly StringBuilder _log = new();
    private readonly List<string> _consoleErrors = new();
    private readonly List<string> _pageErrors = new();

    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        EnsureBrowsersInstalled();
        _host = await ClientHost.StartAsync(new StringWriter(_log));

        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
        IBrowserContext ctx = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
        });
        _page = await ctx.NewPageAsync();
        _page.SetDefaultTimeout(ActionTimeoutMs);
        _page.SetDefaultNavigationTimeout(BootTimeoutMs);

        _page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                _consoleErrors.Add(msg.Text);
            }
        };
        _page.PageError += (_, err) => _pageErrors.Add(err);
    }

    [Fact]
    public async Task Boots_and_the_critical_controls_are_reachable()
    {
        try
        {
            // --- 1. Front page → Launch the Sol scenario (the maiden voyage's front door). ------
            await GotoWithRetry(_host.BaseUrl + "/");
            ILocator launch = _page.Locator("a.btn-primary[href*='scenario=sol']");
            await launch.ClickAsync(); // canary #1: the Launch button lands
            Record("front page: Launch (Sol) is clickable");

            // --- 2. Wait out the boot: the "Rigging the sails…" spinner detaches on world-ready. -
            await _page.WaitForSelectorAsync(".map-loading",
                new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
            Record("world-ready: the boot spinner cleared");

            // --- 3. The start-picker front door, then start a new voyage (a docked berth). -------
            await _page.Locator(".start-picker-backdrop").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
            ILocator newVoyage = _page.Locator(".start-picker-newvoyage");
            await newVoyage.ClickAsync(); // canary #2: the new-voyage berth lands
            Record("start picker: New voyage berth is clickable");

            // --- 4. The desk tab bar renders — the spine every desk hangs off. ------------------
            ILocator tabBar = _page.Locator(".desk-tab-bar");
            await tabBar.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
            Assert.True(await tabBar.IsVisibleAsync(), "desk tab bar did not render after boot");
            // We booted INTO the game, not back onto the picker.
            Assert.False(await _page.Locator(".start-picker-backdrop").IsVisibleAsync(),
                "start picker is still up — the new voyage never started");
            Record("desk tab bar renders");

            // --- 5. Switching to a desk works: click the Captain tab, its room comes up. --------
            await _page.Locator("button.desk-tab", new() { HasTextString = "Captain" }).ClickAsync(); // canary #3
            await _page.Locator(".captain-desk").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            Record("desk switch: Captain tab opens the captain's desk");

            // --- 6. The captain's "Set course to a start point…" — REACHABLE (trial, no jump). --
            // A trial click runs Playwright's full actionability battery (visible, stable, enabled,
            // receives-events / not covered) but does NOT fire the handler, so it never reopens the
            // picker. This is the direct "would a real click land, or is it buried?" question #293
            // is about.
            await _page.Locator(".captain-desk button", new() { HasTextString = "Set course to a start point" })
                .ClickAsync(new() { Trial = true }); // canary #4
            Record("captain desk: 'Set course to a start point' is reachable");

            // --- 7. Back to Nav — the flight HUD returns (desk switching both ways). ------------
            await _page.Locator("button.desk-tab", new() { HasTextString = "Nav" }).ClickAsync(); // canary #5
            await _page.Locator(".map-hud").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            Record("desk switch: Nav tab restores the flight HUD");

            // --- 8. The #127 pilot banner — the "who is flying the ship" affordance, on every ----
            //        desk, must never be covered. Trial-click it as a reachability canary.
            await _page.Locator(".pilot-banner").First.ClickAsync(new() { Trial = true }); // canary #6
            Record("nav: the pilot banner (who has the ship) is reachable");

            // --- The console must be clean: no uncaught JS, no unexplained error logs. ----------
            Assert.True(_pageErrors.Count == 0,
                "Uncaught JS errors during boot:\n  " + string.Join("\n  ", _pageErrors));
            string[] realConsoleErrors = _consoleErrors
                .Where(e => !BenignConsole.Any(b => e.Contains(b, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            Assert.True(realConsoleErrors.Length == 0,
                "Console errors during boot:\n  " + string.Join("\n  ", realConsoleErrors));
            Record("console clean: no uncaught JS or unexplained console errors");
        }
        catch
        {
            await CaptureFailureArtifactsAsync();
            throw;
        }
        finally
        {
            DumpLog();
        }
    }

    // --- helpers -------------------------------------------------------------------------------

    // Load the page, retrying once — a cold CI runner can drop the very first navigation while the
    // just-started Kestrel/WASM assets warm up. One retry, then let the failure stand.
    private async Task GotoWithRetry(string url)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await _page.GotoAsync(url, new()
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = BootTimeoutMs,
                });
                return;
            }
            catch (Exception ex) when (attempt == 1)
            {
                _log.AppendLine($"[retry] first page load failed ({ex.Message}); retrying once.");
            }
        }
    }

    private void Record(string name) => _log.AppendLine($"PASS — {name}");

    private async Task CaptureFailureArtifactsAsync()
    {
        string dir = ArtifactsDir();
        Directory.CreateDirectory(dir);
        try
        {
            await _page.ScreenshotAsync(new()
            {
                Path = Path.Combine(dir, "ui-gate-failure.png"),
                FullPage = true,
            });
            _log.AppendLine($"[artifacts] screenshot → {Path.Combine(dir, "ui-gate-failure.png")}");
        }
        catch (Exception ex)
        {
            _log.AppendLine($"[artifacts] screenshot failed: {ex.Message}");
        }

        var console = new StringBuilder();
        console.AppendLine("=== page errors (uncaught JS) ===");
        console.AppendLine(_pageErrors.Count == 0 ? "(none)" : string.Join("\n", _pageErrors));
        console.AppendLine();
        console.AppendLine("=== console errors ===");
        console.AppendLine(_consoleErrors.Count == 0 ? "(none)" : string.Join("\n", _consoleErrors));
        await File.WriteAllTextAsync(Path.Combine(dir, "ui-gate-console.log"), console.ToString());
        _log.AppendLine($"[artifacts] console log → {Path.Combine(dir, "ui-gate-console.log")}");
    }

    // Write the step log where CI can upload it too (not only to xUnit output, which a red mobile
    // check can't expand).
    private void DumpLog()
    {
        Console.WriteLine(_log.ToString());
        try
        {
            string dir = ArtifactsDir();
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ui-gate-steps.log"), _log.ToString());
        }
        catch
        {
            // Best-effort — the log already went to stdout.
        }
    }

    private static string ArtifactsDir()
    {
        string? fromEnv = Environment.GetEnvironmentVariable("SPACESAILS_UIGATE_ARTIFACTS");
        return string.IsNullOrWhiteSpace(fromEnv)
            ? Path.Combine(AppContext.BaseDirectory, "ui-gate-artifacts")
            : fromEnv;
    }

    // Ensure a Chromium is present so a bare `dotnet test` works with no separate install step.
    // CI installs browsers as an explicit warm step; this is the local-dev safety net.
    private static void EnsureBrowsersInstalled()
    {
        int exit = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exit != 0)
        {
            throw new InvalidOperationException(
                $"`playwright install chromium` exited {exit} — cannot run the UI gate without a browser.");
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }
        _pw?.Dispose();
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }
}
