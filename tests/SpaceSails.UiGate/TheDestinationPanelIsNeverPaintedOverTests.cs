using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #950 (REOPENED) · THE NAVIGATION-TARGET PANEL MUST NOT BE PAINTED OVER BY THE PLOTTING PANEL'S EDGE.
///
/// <para><b>The sighting.</b> #992/#994/#997 fixed the Plotting panel's OWN overflow — it never again runs
/// past the bottom of the window (<see cref="PlotPanelFitsTheWindowTests"/>), and a story plate can no
/// longer paint over its list (<see cref="HudCollisionTests.The_story_plate_never_covers_the_plotting_panel"/>).
/// Neither guard, though, sets a navigation destination: dock at a berth, plot a plan (cast off · two burns ·
/// the last one's editor open — <see cref="PlotPanelFitsTheWindowTests"/>'s own owner scenario), then search
/// for a body and set it as the destination. M25's <c>.map-dest-panel</c> — "the whole point of navigating
/// there", by its own doc comment — appears bottom-centre, ABSOLUTELY positioned against the window,
/// independent of <c>.map-flowcolumn</c>. MEASURED at both 1280×720 and 390×700 with exactly that boot:
/// <c>.map-plot</c>'s own bottom edge lands 12 px inside <c>.map-dest-panel</c>'s top edge at BOTH sizes —
/// the two panels were never made to coordinate (one lives in #992's flex column, the other is pinned to the
/// window's own corner), and whichever painted on top before this fix was whichever the browser's default
/// DOM-order stacking happened to favour: <c>.map-hud</c> (<c>chrome + 12</c>) above <c>.map-dest-panel</c>
/// (<c>chrome + 2</c>) — the edge of the panel the captain was NOT reading at that moment hid a sliver of the
/// one panel that answers "did the course actually get you there".</para>
///
/// <para><b>The fix.</b> <c>.map-dest-panel</c>'s z-index moves to <c>chrome + 13</c> — one above
/// <c>.map-hud</c>'s <c>chrome + 12</c>, one below <c>.map-topstack</c>'s <c>chrome + 14</c> (the banner
/// still wins over everything). The two panels still geometrically overlap by the same handful of pixels —
/// nothing here changes either panel's position or size, and #950's own broader "give the flow column real
/// arithmetic" fix is #992/#997's, already shipped — but on the frame they touch, the navigation target's
/// own words are the ones actually painted, not a stray edge of a different panel.</para>
///
/// <para><b>Why bounding boxes alone cannot prove this.</b> Two elements' <c>getBoundingClientRect()</c>s can
/// overlap while one is fully hidden behind the other, or while both remain fully legible (transparent
/// backgrounds, a gap in content) — geometry says nothing about which pixel a real screen actually shows.
/// This guard reads the DOM's own answer to that question, <c>document.elementFromPoint</c>, at the centre of
/// the two boxes' own overlap — the same test a real screenshot would settle.</para>
///
/// <para><b>RED PROOF.</b> Reverting <c>.map-dest-panel</c>'s z-index to <c>chrome + 2</c> fails at both
/// viewports: the point in the overlap resolves to an element inside <c>.map-plot</c>, not
/// <c>.map-dest-panel</c>.</para>
/// </summary>
public sealed class TheDestinationPanelIsNeverPaintedOverTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    private const int DesktopWidth = 1280;
    private const int DesktopHeight = 720;

    // TallCardTests' own number: a small phone in portrait, browser chrome removed.
    private const int PhoneWidth = 390;
    private const int PhoneHeight = 700;

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
        _page = await _browser.NewPageAsync(new()
        {
            ViewportSize = new() { Width = DesktopWidth, Height = DesktopHeight },
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// THE NAVIGATION-TARGET PANEL WINS THE OVERLAP, at a normal desktop size and at TallCardTests' phone
    /// size — built once (boot is the expensive part) and checked at both viewports by resizing in place,
    /// the same window a captain would resize.
    /// </summary>
    [Fact]
    public async Task The_destination_panel_is_never_painted_over_by_the_plotting_panel()
    {
        await BootWithAPlanAndADestinationSet();

        await AssertDestinationPanelWinsTheOverlap("1280x720 desktop");

        await _page.SetViewportSizeAsync(PhoneWidth, PhoneHeight);
        await _page.WaitForTimeoutAsync(300);
        await AssertDestinationPanelWinsTheOverlap($"{PhoneWidth}x{PhoneHeight} phone");
    }

    private async Task AssertDestinationPanelWinsTheOverlap(string atSize)
    {
        ILocator plot = _page.Locator(".map-plot");
        ILocator dest = _page.Locator(".map-dest-panel");

        Assert.True(
            await dest.CountAsync() > 0 && await dest.IsVisibleAsync(),
            $"the navigation-target panel is not showing at {atSize} — this guard has nothing to measure "
            + "the Plotting panel's edge against. Re-check the destination-set flow (#950).");

        if (await plot.BoundingBoxAsync() is not { } plotBox || await dest.BoundingBoxAsync() is not { } destBox)
        {
            throw new InvalidOperationException($"the plot panel or the destination panel has no box at {atSize}");
        }

        double ox1 = Math.Max(plotBox.X, destBox.X);
        double oy1 = Math.Max(plotBox.Y, destBox.Y);
        double ox2 = Math.Min(plotBox.X + plotBox.Width, destBox.X + destBox.Width);
        double oy2 = Math.Min(plotBox.Y + plotBox.Height, destBox.Y + destBox.Height);

        // The premise, checked out loud (#735's honesty clause): the two panels really do share pixels at
        // this size. If a future layout change separates them cleanly, this guard has nothing left to prove
        // and must say so rather than passing on an empty overlap.
        Assert.True(
            ox2 > ox1 && oy2 > oy1,
            $"the Plotting panel (x {plotBox.X:0}…{plotBox.X + plotBox.Width:0}, y {plotBox.Y:0}…"
            + $"{plotBox.Y + plotBox.Height:0}) and the destination panel (x {destBox.X:0}…"
            + $"{destBox.X + destBox.Width:0}, y {destBox.Y:0}…{destBox.Y + destBox.Height:0}) do not "
            + $"overlap at {atSize} — nothing here for this guard to prove a winner over (#950).");

        // THE DESTINATION PANEL IS WHAT A REAL SCREEN ACTUALLY SHOWS at the centre of that shared patch —
        // read off the DOM's own answer to "what is painted here", not the two panels' geometry (which say
        // nothing about which one is on top, or whether either is actually opaque there).
        double cx = (ox1 + ox2) / 2, cy = (oy1 + oy2) / 2;
        string atPoint = await _page.EvaluateAsync<string>(
            """
            ([x, y]) => {
                const el = document.elementFromPoint(x, y);
                if (!el) return '(nothing)';
                const dest = el.closest('.map-dest-panel');
                const plot = el.closest('.map-plot');
                return dest ? 'dest' : plot ? 'plot' : (el.className ? el.className.toString() : el.tagName);
            }
            """,
            new object[] { cx, cy });

        Assert.True(
            atPoint == "dest",
            $"at {atSize}, the pixel where the Plotting panel and the destination panel overlap "
            + $"({cx:0},{cy:0}) is painted by \"{atPoint}\", not the destination panel — the navigation "
            + "target's own words are hidden behind the Plotting panel's edge (#950 reopened).");
    }

    /// <summary>
    /// <see cref="PlotPanelFitsTheWindowTests"/>'s own owner scenario (docked, cast off, two burns, the last
    /// one's editor open) plus a destination: search for Earth (which centres the camera on it — a pointer
    /// down at the canvas centre then hits it directly, #253's forgiving pick radius), open its body menu
    /// (a knot of bodies near Earth answers with a chooser first — pick Earth from it), and set it as the
    /// destination. That raises <c>.map-dest-panel</c> — M25's own panel, independent of the flight plan.
    /// </summary>
    private async Task BootWithAPlanAndADestinationSet()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?dock=red-eye", new() { Timeout = BootTimeoutMs });
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        await _page.Locator("button.desk-tab", new() { HasTextString = "Nav" }).ClickAsync();
        await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = "Nav" }).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        await _page.Locator(".map-hud button", new() { HasTextString = "Plot" }).ClickAsync();
        await _page.Locator(".map-plot-compose").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        await ComposeAsync("Cast off");
        await Expect(2);
        await ScrubTo(0.25);
        await ComposeAsync("Add burn");
        await Expect(3);
        await ScrubTo(0.8);
        await ComposeAsync("Add burn");
        await Expect(4);
        await _page.Locator(".map-plan-step-open .map-plan-step-edit").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        await _page.Locator(".nav-search-input").ClickAsync();
        await _page.Locator(".nav-search-input").FillAsync("Earth");
        await _page.Locator(".nav-search-row", new() { HasTextString = "Earth" }).First.WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        await _page.Locator(".nav-search-row", new() { HasTextString = "Earth" }).First.ClickAsync();

        if (await _page.Locator("canvas.map-canvas").BoundingBoxAsync() is { } canvasBox)
        {
            double cx = canvasBox.X + canvasBox.Width / 2, cy = canvasBox.Y + canvasBox.Height / 2;
            await _page.Mouse.MoveAsync((float)cx, (float)cy);
            await _page.Mouse.DownAsync();
            await _page.Mouse.UpAsync();
        }

        await _page.Locator(".map-body-menu").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        string menuText = await _page.Locator(".map-body-menu").First.InnerTextAsync();
        if (menuText.Contains("Which one?"))
        {
            await _page.Locator(".map-body-menu button", new() { HasTextString = "Earth" }).First.ClickAsync();
            await _page.Locator(".map-body-menu button", new() { HasTextString = "Set destination" }).WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        }

        await _page.Locator(".map-body-menu button", new() { HasTextString = "Set destination" }).ClickAsync();
        await _page.Locator(".map-dest-panel").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
    }

    /// <summary>Run the plot clock out to a fraction of the drawn path — the scrub handle, moved from
    /// outside, the same idiom <see cref="PlotPanelFitsTheWindowTests"/> uses.</summary>
    private async Task ScrubTo(double fraction)
    {
        await _page.Locator(".map-plot input.form-range").First.EvaluateAsync(
            """
            (el, f) => {
                const set = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
                set.call(el, String(Math.round(Number(el.max) * f)));
                el.dispatchEvent(new Event('input', { bubbles: true }));
            }
            """,
            fraction);
        await _page.WaitForFunctionAsync(
            "() => { const p = document.querySelector('.map-plot'); "
            + "return p !== null && !/Scrub: 0d 00h 00m/.test(p.textContent); }",
            null,
            new() { Timeout = ActionTimeoutMs });
    }

    private async Task ComposeAsync(string label) =>
        await _page.Locator(".map-plot-compose button", new() { HasTextString = label })
                   .DispatchEventAsync("click", null, new() { Timeout = ActionTimeoutMs });

    private async Task Expect(int rows) =>
        await _page.Locator(".map-plan-step").Nth(rows - 1).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
}
