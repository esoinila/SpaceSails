using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// THE TRADE DESK RENDERS. The bug: a Razor comment written between two of a <c>&lt;button&gt;</c>'s
/// attributes in the SENTRY ARMORY panel. Razor compiles a <c>@* … *@</c> run found inside a START TAG into
/// an ATTRIBUTE NAME rather than dropping it, so at runtime the renderer called
/// <c>setAttribute("#562: the affordability gate reads the CONSTANT, not a …", …)</c>, Chrome answered
/// <c>InvalidCharacterError</c>, and the exception came out of the render tree — which does not spoil one
/// button, it takes the page down to "An unhandled error has occurred. Reload." and nothing else.
///
/// <para><b>Why nothing caught it.</b> It compiles: it is valid Razor. Every unit test in the repository
/// stayed green: none of them render markup. And it only fires in one state — docked at a haven, on the
/// Trade desk, with sentries still aboard — which is why the owner met it twice on 2026-08-21 (#962, #948)
/// and no one could reproduce it from the report.</para>
///
/// <para><b>What this gate adds over the static law.</b> <c>SpaceSails.Client.Tests</c> holds the rule that
/// no comment may sit inside a start tag, and that rule is the cheap one that will catch the next typist.
/// This is the expensive one that cannot be fooled: a real Chromium, the PUBLISHED artifact, the actual
/// panel — the page either paints the rearm button or it does not. Any future way of poisoning that
/// render (an attribute name, a bad style, a throwing expression) fails here without anybody having
/// predicted the mechanism.</para>
/// </summary>
public sealed class TheTradeDeskRendersTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    // #288's dev cheat: boot already CLAMPED ON at a berth, which is the only state where the armory
    // panel — and therefore the button that crashed — is rendered at all.
    private const string BerthId = "selene-gate";

    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    /// <summary>Everything the page threw or logged as an error while this test drove it.</summary>
    private readonly List<string> _pageFaults = [];

    public async Task InitializeAsync()
    {
        _host = await ClientHost.StartAsync(TextWriter.Null);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });

        _page.PageError += (_, e) => _pageFaults.Add("pageerror: " + e);
        _page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                _pageFaults.Add("console.error: " + msg.Text);
            }
        };
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// Dock, open Trade, and find the armory's rearm button standing there — with Blazor's crash banner
    /// down and nothing thrown on the way.
    /// </summary>
    [Fact]
    public async Task The_trade_desk_at_a_berth_paints_the_sentry_rearm_button_without_crashing()
    {
        await _page.GotoAsync($"{_host.BaseUrl}/map?dock={BerthId}", new() { Timeout = BootTimeoutMs });
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });

        // The desk tab bar is the page saying the world is up and the captain is aboard, not on a surface.
        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        ILocator tradeTab = _page.Locator("button.desk-tab", new() { HasTextString = "Trade" });
        await tradeTab.ClickAsync(new() { Timeout = ActionTimeoutMs });
        await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = "Trade" }).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        // THE CONTROL THAT CRASHED. A fresh ship carries the full sentry roster, so docked at a berth this
        // button is rendered — unless the render died, in which case nothing on the page is.
        ILocator rearm = _page.Locator(".sentry-rearm-btn");
        await rearm.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        Assert.Equal(1, await rearm.CountAsync());

        // THE CRASH BANNER. index.html keeps #blazor-error-ui hidden until Blazor unhandles something; the
        // whole symptom the owner reported is that div becoming the only thing on the screen.
        ILocator crashBanner = _page.Locator("#blazor-error-ui");
        Assert.False(await crashBanner.IsVisibleAsync(),
                     "Blazor's \"An unhandled error has occurred\" banner is up on the Trade desk — the render "
                     + "died. That is the #562 shape: something in the markup is not what the DOM will accept.");

        // …and the armory panel itself is still whole around it, so a page that painted a lone orphaned
        // button could not pass either.
        Assert.Equal(1, await _page.Locator(".fuel-desk-title", new() { HasTextString = "SENTRY ARMORY" }).CountAsync());

        Assert.True(_pageFaults.Count == 0,
                    "the Trade desk threw on the way up:\n  " + string.Join("\n  ", _pageFaults));
    }
}
