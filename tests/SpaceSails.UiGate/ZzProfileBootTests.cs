using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

// TEMPORARY profiling harness for #161 — NOT shipped. Boots the published client the same way the
// gate does and dumps every `[SpaceSails] boot ·` console line, so the longest synchronous block is
// read off the boot's own clock rather than guessed.
public sealed class ZzProfileBootTests : IAsyncLifetime
{
    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;
    private readonly StringBuilder _log = new();

    public async Task InitializeAsync()
    {
        _host = await ClientHost.StartAsync(new StringWriter(_log));
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
    public async Task Profile()
    {
        var all = new StringBuilder();
        all.AppendLine(_log.ToString());
        for (int run = 1; run <= 3; run++)
        {
            IBrowserContext ctx = await _browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1280, Height = 800 },
            });
            IPage page = await ctx.NewPageAsync();
            var lines = new List<string>();
            page.Console += (_, m) =>
            {
                if (m.Text.Contains("[SpaceSails] boot", StringComparison.Ordinal)
                    || m.Text.Contains("[profile]", StringComparison.Ordinal))
                {
                    lines.Add(m.Text);
                }
            };
            page.SetDefaultTimeout(120_000);
            page.SetDefaultNavigationTimeout(180_000);

            var clock = Stopwatch.StartNew();
            await page.GotoAsync(_host.BaseUrl + "/", new() { Timeout = 180_000 });
            ILocator launch = page.Locator("a.btn-primary[href*='scenario=sol']");
            await launch.ClickAsync();
            long frontPage = clock.ElapsedMilliseconds;
            long bootStart = clock.ElapsedMilliseconds;

            ILocator newVoyage = page.Locator(".start-picker-newvoyage");
            await newVoyage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 180_000 });
            long picker = clock.ElapsedMilliseconds - bootStart;
            await newVoyage.ClickAsync();
            await page.WaitForSelectorAsync(".map-loading",
                new() { State = WaitForSelectorState.Detached, Timeout = 180_000 });
            ILocator tabBar = page.Locator(".desk-tab-bar");
            await tabBar.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 180_000 });
            long boot = clock.ElapsedMilliseconds - bootStart;

            // Let any deferred work finish and report itself.
            await page.WaitForTimeoutAsync(20_000);

            all.AppendLine($"=== RUN {run} ===  frontPage={frontPage}ms  frontDoor={picker}ms  bootComplete={boot}ms");
            foreach (string l in lines)
            {
                all.AppendLine("   " + l);
            }
            await ctx.CloseAsync();
        }

        string outPath = Environment.GetEnvironmentVariable("SPACESAILS_PROFILE_OUT")
            ?? Path.Combine(Path.GetTempPath(), "spacesails-boot-profile.txt");
        File.WriteAllText(outPath, all.ToString());
        Assert.Fail(all.ToString());
    }
}
