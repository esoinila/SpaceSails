using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1037 · THE NAVIGATION-TARGET PANEL IS NEVER PAINTED OVER BY THE PLOTTING PANEL.
///
/// <para><b>The sighting.</b> #992/#994/#997 fixed the Plotting panel's own overflow — it never again runs
/// past the bottom of the window (<see cref="PlotPanelFitsTheWindowTests"/>), and a story plate can no
/// longer paint over its list (<c>HudCollisionTests.The_story_plate_never_covers_the_plotting_panel</c>).
/// Neither guard, though, sets a navigation destination. Dock at a berth, plot a plan (cast off · two burns ·
/// the last one's editor open — <see cref="PlotPanelFitsTheWindowTests"/>'s own owner scenario), then search
/// for a body and set it as the destination, and M25's <c>.map-dest-panel</c> appears at the foot of the
/// glass. MEASURED on the untouched baseline with exactly that boot: <c>.map-plot</c>'s own bottom edge
/// landed <b>12 px</b> inside <c>.map-dest-panel</c>'s top edge at 1280×720 and <b>16 px</b> at 390×700, and
/// a real screen painted the Plotting panel's edge there at desktop and the HUD's spilled toolbar there on
/// the phone — the surface the captain was NOT reading hiding the top of the one that answers "does this
/// course actually get you there".
/// </para>
///
/// <para><b>Why bounding boxes cannot settle this, in either direction.</b> Two boxes can overlap while one
/// is entirely hidden behind the other, or while both stay fully legible. Worse for a guard: after the fix
/// the two LAYOUT rects still overlap at 390×700 — the HUD's own box ends above the panel and its content is
/// clipped and scrollable inside it, so the Plotting panel's unclipped rect reaches into the panel's rows
/// while not one of its pixels is painted there. Geometry says nothing about what a screen shows.
/// <c>document.elementFromPoint</c> is the DOM's own answer to that question, and it is the same answer a
/// screenshot would give.</para>
///
/// <para><b>What it asserts.</b> The panel's own top strip — the head row carrying the target's name and
/// <i>clear target</i>, which is exactly the band the Plotting panel's edge was landing in — is painted by
/// the panel, at five points across its width and two depths, at both viewports.</para>
///
/// <para><b>RED PROOF.</b> Revert <c>.map-dest-panel</c> to its M25 window-anchored box (
/// <c>position: absolute; bottom: 0.75rem; left: 50%; transform: translateX(-50%)</c>, outside
/// <c>.map-flowcolumn</c>) and this fails at both viewports, naming the class actually painted there.</para>
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
    /// THE NAVIGATION-TARGET PANEL OWNS ITS OWN PIXELS, at a normal desktop size and at TallCardTests' phone
    /// size — built once (the boot is the expensive part) and checked at both viewports by resizing in
    /// place, the same window a captain would resize.
    /// </summary>
    [Fact]
    public async Task The_destination_panel_is_never_painted_over_by_the_plotting_panel()
    {
        await BootWithAPlanAndADestinationSet();

        await AssertThePanelPaintsItsOwnHead($"{DesktopWidth}x{DesktopHeight} desktop");

        await _page.SetViewportSizeAsync(PhoneWidth, PhoneHeight);
        await _page.WaitForFunctionAsync(
            "() => window.innerWidth === " + PhoneWidth, null, new() { Timeout = ActionTimeoutMs });
        await AssertThePanelPaintsItsOwnHead($"{PhoneWidth}x{PhoneHeight} phone");
    }

    private async Task AssertThePanelPaintsItsOwnHead(string atSize)
    {
        ILocator dest = _page.Locator(".map-dest-panel");
        ILocator plot = _page.Locator(".map-plot");

        // Both premises, out loud (#735's honesty clause). Without the panel there is nothing to be painted
        // over, and without the Plotting panel there is nothing that could do the painting: either way this
        // guard has measured nothing and must say so rather than going green.
        Assert.True(
            await dest.CountAsync() > 0 && await dest.IsVisibleAsync(),
            $"the navigation-target panel is not showing at {atSize} — this guard has nothing to measure. "
            + "Re-check the destination-set flow (#1037).");
        Assert.True(
            await plot.CountAsync() > 0 && await plot.IsVisibleAsync(),
            $"the Plotting panel is not showing at {atSize} — nothing here could paint over the "
            + "navigation-target panel, so this guard proves nothing (#1037).");

        if (await dest.BoundingBoxAsync() is not { } destBox)
        {
            throw new InvalidOperationException($"the destination panel has no box at {atSize}");
        }

        // …and it is actually ON the glass: a panel pushed off the bottom of the window by the column above
        // it would pass a "nothing is painted over me" test by being nowhere at all.
        Assert.True(
            destBox.Y >= 0 && destBox.Y + destBox.Height <= _page.ViewportSize!.Height,
            $"the navigation-target panel runs {destBox.Y:0}…{destBox.Y + destBox.Height:0} px down a "
            + $"{_page.ViewportSize!.Height} px window at {atSize} — it is off the glass, not merely covered "
            + "(#1037).");

        // WHAT A REAL SCREEN SHOWS along the panel's own head row: five points across its width (clear of
        // the rounded corners) at two depths inside its top edge — the band the Plotting panel's bottom
        // edge was landing in. Read off the DOM's own answer to "what is painted here".
        string[] verdicts = await _page.EvaluateAsync<string[]>(
            """
            () => {
                const dest = document.querySelector('.map-dest-panel');
                const box = dest.getBoundingClientRect();
                const out = [];
                for (const fx of [0.1, 0.25, 0.5, 0.75, 0.9]) {
                    for (const dy of [3, 10]) {
                        const x = box.x + box.width * fx, y = box.y + dy;
                        const el = document.elementFromPoint(x, y);
                        const who = !el ? '(nothing)'
                            : el.closest('.map-dest-panel') ? 'dest'
                            : el.closest('.map-plot') ? '.map-plot'
                            : el.closest('.map-hud') ? '.map-hud'
                            : el.closest('.story-plate') ? '.story-plate'
                            : (el.className ? el.className.toString() : el.tagName);
                        out.push(who + ' at (' + x.toFixed(0) + ',' + y.toFixed(0) + ')');
                    }
                }
                return out;
            }
            """);

        string[] stolen = verdicts.Where(v => !v.StartsWith("dest ", StringComparison.Ordinal)).ToArray();
        Assert.True(
            stolen.Length == 0,
            $"at {atSize} the navigation-target panel's own head row is painted by something else at "
            + $"{stolen.Length} of {verdicts.Length} sampled points — {string.Join("; ", stolen)}. The panel "
            + "answers \"does this course get you there\"; nothing above it may spend its pixels (#1037).");
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
