using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1110 · <b>THE CAPTAIN'S IDENT ROW WAS UNSTYLED, AND NOTHING IN THE SOURCE LOOKED WRONG.</b>
///
/// <para>Four rules — <c>.captain-vault-actions</c>, <c>.captain-ident-row</c>, <c>.captain-ident-label</c>
/// and <c>.captain-ident-name</c> — styled markup only <c>Pages/Stations/Captain.razor</c> renders, and
/// were written in <c>Pages/Map.razor.css</c> <b>without <c>::deep</c></b>. Blazor puts the writing sheet's
/// scope on the last compound, so they compiled to <c>.captain-ident-row[b-4dqsdx4p75]</c> while the
/// captain's own elements wear Captain's scope. Real classes, real values, in a sheet that really loads,
/// and the row a captain reads their own name off had no styling on it at all.</para>
///
/// <para><b>Why this gate and not only the static law.</b>
/// <c>NoRuleIsWrittenForMarkupItCanNeverReachTests</c> reads the sources and says the rule <i>can</i> reach
/// the markup; it cannot say the browser <i>did</i>. This does: a real Chromium, the published artifact,
/// the actual desk, and the row's own COMPUTED style read back off the element. A rule filed in the wrong
/// sheet, a scope that stops being pinned, a selector renamed on one side only — any of them puts
/// <c>display</c> back to the browser's default here, whatever the source looks like.</para>
///
/// <para><b>Proven RED</b> by putting the three ident rules back in <c>Pages/Map.razor.css</c>: the row
/// comes back as <c>display: block</c> with no gap, which is exactly the desk the owner has been looking
/// at.</para>
/// </summary>
public sealed class TheCaptainsIdentRowIsStyledTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    /// <summary>#288's dev cheat: boot clamped on at a berth, so the desks are reachable.</summary>
    private const string BerthId = "selene-gate";

    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _host = await ClientHost.StartAsync(TextWriter.Null);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>Dock, open the Captain desk, and read the ident row's computed style off the element.</summary>
    [Fact]
    public async Task The_captains_ident_row_wears_the_rules_written_for_it()
    {
        await _page.GotoAsync($"{_host.BaseUrl}/map?dock={BerthId}", new() { Timeout = BootTimeoutMs });
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });

        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        ILocator captainTab = _page.Locator("button.desk-tab", new() { HasTextString = "Captain" });
        await captainTab.ClickAsync(new() { Timeout = ActionTimeoutMs });

        ILocator row = _page.Locator(".captain-ident-row");
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        // THE FACT. `display: flex` is the first declaration of .captain-ident-row and the one no browser
        // default supplies — a <div> is `block` until a rule that reaches it says otherwise. Read as the
        // COMPUTED value, so it is the cascade answering and not the stylesheet being re-read.
        string display = await row.EvaluateAsync<string>("e => getComputedStyle(e).display");
        Assert.True(display == "flex",
            $"#1110 · the captain's ident row computes `display: {display}`. The three rules written for it " +
            "are not reaching it — which is the whole bug: they were in Pages/Map.razor.css without ::deep, " +
            "so they wore the Map page's scope and this desk's elements wear Captain's. They belong in " +
            "Pages/Stations/Captain.razor.css.");

        // …and the rest of the block with it, so a stray `display: flex` from somewhere else cannot pass
        // for the rule having landed.
        Assert.Equal("0.5rem", ToRem(await row.EvaluateAsync<string>("e => getComputedStyle(e).columnGap")));
        Assert.Equal("wrap", await row.EvaluateAsync<string>("e => getComputedStyle(e).flexWrap"));

        // The label and the name are sized by the same block. 0.8rem and 1.05rem against the 16px root.
        string label = await _page.Locator(".captain-ident-label").First
            .EvaluateAsync<string>("e => getComputedStyle(e).fontSize");
        Assert.Equal("0.8rem", ToRem(label));

        ILocator name = _page.Locator(".captain-ident-name");
        if (await name.CountAsync() > 0)   // the name is behind the rename control's read state
        {
            Assert.Equal("1.05rem", ToRem(await name.First.EvaluateAsync<string>("e => getComputedStyle(e).fontSize")));
            Assert.Equal("600", await name.First.EvaluateAsync<string>("e => getComputedStyle(e).fontWeight"));
        }
    }

    /// <summary>A computed length in px, said back in rem against the 16 px root — so the assertion above
    /// quotes the stylesheet's own number instead of a pixel a reader has to convert.</summary>
    private static string ToRem(string px) =>
        double.TryParse(px.Replace("px", "", StringComparison.Ordinal),
            System.Globalization.CultureInfo.InvariantCulture, out double v)
            ? (v / 16.0).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "rem"
            : px;
}
