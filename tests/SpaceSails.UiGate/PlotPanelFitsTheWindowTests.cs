using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #992 · THE PLOTTING PANEL IS INSIDE THE WINDOW, AND THE STEP LIST IS WHAT SCROLLS.
///
/// <para><b>The sighting.</b> The owner, docked at The Red Eye with a four-step plan — cast off · clear the
/// harbour · a burn in 3 d 18 h · a burn in 54 d — opened the LAST row's editor on a 1490×1308 window. The
/// panel ran off the bottom of the screen and the lower half of that editor (the ±p row, the ±d/±h row, the
/// way to take the step off the plan) was simply not on the screen, with no scroll anywhere that reached
/// it: <i>"Let's make the step list start scroll when it gets high enough."</i></para>
///
/// <para><b>Why it was not already covered.</b> #950 capped <c>.map-plot</c> at <c>calc(100vh - 14rem)</c>.
/// That is a CONSTANT standing in for the chrome above the panel, and on the Nav desk the chrome is roughly
/// twice it: measured in a real browser at the owner's own window size, the tab bar and pilot banner take
/// 128 px, then the toolbar 48, the readouts block 264 and the frame chip 69 — the panel's own top is
/// 540 px, i.e. 34 rem. The cap resolved to 881 px against a 701 px panel, so it never engaged and the
/// panel grew straight past the bottom edge. Nothing regressed it; it could not fire on this desk from the
/// day it was written, and #983/#990's cast-off rows are simply what finally made the plan tall enough for
/// the owner to see it.</para>
///
/// <para><b>Why a browser gate.</b> #680's law and #735's: <i>in the DOM is not on the screen</i>. Every
/// control in that editor is rendered, enabled and wired; the only question that matters is whether its
/// pixels are inside the glass, and only a real layout at a real viewport can answer it.</para>
///
/// <para><b>RED PROOF.</b> Point <c>SPACESAILS_PUBLISH_DIR</c> at a publish of the parent commit and both
/// facts fail: the panel's box bottom lands ~136 px below the viewport, and the step list has no scroll to
/// bring the editor's remove control back onto the screen.</para>
/// </summary>
public sealed class PlotPanelFitsTheWindowTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    /// <summary>The narrowest and shortest window the game is laid out for (the number #986 F1 stands at).
    /// The owner's own window is far taller, so a law that holds here holds there — and a control that is
    /// off the screen at 720 is off the screen for anybody on a laptop.</summary>
    private const int ViewportWidth = 1280;
    private const int ViewportHeight = 720;

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
            ViewportSize = new() { Width = ViewportWidth, Height = ViewportHeight },
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// NOTHING IN THE PLOTTING PANEL IS BELOW THE BOTTOM EDGE. The bug, stated as pixels: the panel's own
    /// box ran past the foot of the window, and everything it carried down there went with it.
    /// </summary>
    [Fact]
    public async Task The_plotting_panel_is_never_taller_than_the_window_it_is_drawn_in()
    {
        await BuildTheOwnersPlan();

        ILocator panel = _page.Locator(".map-plot");
        if (await panel.BoundingBoxAsync() is not { } box)
        {
            throw new InvalidOperationException("the Plotting panel has no box at all — nothing was measured");
        }

        // The premise, checked out loud (#735's honesty clause): this plan really is taller than the space
        // the panel has. If a future edit makes it short, the cap is proving nothing and must say so.
        int overflowing = await panel.EvaluateAsync<int>("el => el.scrollHeight - el.clientHeight")
            + await _page.Locator(".map-plot-nodes").EvaluateAsync<int>("el => el.scrollHeight - el.clientHeight");
        Assert.True(
            overflowing > 0,
            $"the owner's four-step plan with its last editor open fits the panel with {overflowing} px to "
            + "spare on a "
            + $"{ViewportWidth}×{ViewportHeight} window — nothing overflowed, so this guard proved nothing. "
            + "Build a taller plan before trusting it again (#992).");

        Assert.True(
            box.Y >= 0 && box.Y + box.Height <= ViewportHeight,
            $"the Plotting panel runs {box.Y:0}…{box.Y + box.Height:0} px down a {ViewportHeight} px window — "
            + $"{box.Y + box.Height - ViewportHeight:0} px of it, and every control down there, is off the "
            + "bottom of the screen. #950's cap is a constant guess at the chrome above the panel; the panel "
            + "has to be bound to what the window actually has left (#992).");
    }

    /// <summary>
    /// THE WAY TO TAKE A STEP OFF THE PLAN IS REACHABLE. Pinning the panel's box would be a cheat if the fix
    /// were to clip it: the editor the captain opened has to be reachable, which means the step list scrolls
    /// and the scrolling gets you all the way to the end of the open editor.
    /// </summary>
    [Fact]
    public async Task The_open_editors_remove_control_can_be_scrolled_onto_the_screen()
    {
        await BuildTheOwnersPlan();

        ILocator list = _page.Locator(".map-plot-nodes");

        // The list is the scrolling element — the owner's own words for the fix.
        int reached = await list.EvaluateAsync<int>(
            "el => { el.scrollTop = el.scrollHeight; return el.scrollTop; }");
        Assert.True(
            reached > 0,
            "the flight-plan step list will not scroll at all, so the half of the open editor below its fold "
            + "cannot be reached by any means — which is the sighting on #992 exactly.");

        // The burn editor's own remove control (the × beside the @ re-time), which on the owner's screenshot
        // was among the things below the bottom edge.
        ILocator remove = _page.Locator(".map-plan-step-open .map-plan-step-edit .btn-outline-danger").Last;
        if (await remove.BoundingBoxAsync() is not { } box)
        {
            throw new InvalidOperationException("the open step editor shows no remove control at all");
        }

        Assert.True(
            box.Y >= 0 && box.Y + box.Height <= ViewportHeight,
            $"with the step list scrolled to its end, the open editor's remove control still sits "
            + $"{box.Y:0}…{box.Y + box.Height:0} px down a {ViewportHeight} px window — the captain cannot "
            + "take the step off the plan (#992, and #950's sighting before it).");

        // …and Playwright's own actionability battery (visible · stable · enabled · not covered) agrees a
        // real press would land. Trial, so the step is not actually deleted here.
        await remove.ClickAsync(new() { Trial = true, Timeout = ActionTimeoutMs });

        // The last row of the editor — the ±d/±h time steps — is the true bottom of what the owner lost.
        ILocator lastRow = _page.Locator(".map-plan-step-open .map-burn-steps").Last;
        if (await lastRow.BoundingBoxAsync() is not { } rowBox)
        {
            throw new InvalidOperationException("the open step editor shows no ± rows at all");
        }

        Assert.True(
            rowBox.Y + rowBox.Height <= ViewportHeight,
            $"the open editor's last ± row ends {rowBox.Y + rowBox.Height:0} px down a {ViewportHeight} px "
            + "window even with the list scrolled to its end — the bottom of the editor is still off the "
            + "screen (#992).");
    }

    /// <summary>
    /// Build the plan from the owner's screenshot, through the buttons a captain presses: docked at The Red
    /// Eye, ⚓ cast off (which lays the clamp release AND the clearance, #955 NAV-1), a burn at the scrub,
    /// then the scrub run far down the course and a second burn there. Adding a burn opens its editor, so
    /// the plan ends exactly as the sighting did — four rows with the LAST one's editor unfolded.
    /// </summary>
    private async Task BuildTheOwnersPlan()
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

        // ⚓ lays BOTH departure rows (#955 NAV-1): the clamp release and the clearance behind it.
        await ComposeAsync("Cast off");
        await Expect(2);

        // Both burns are run out along the course FIRST. A burn dropped at the berth's own hour would land
        // between the clamp and the clearance, which #989's shape check refuses out loud — a different
        // sighting, and not the plan the owner had on the board.
        await ScrubTo(0.25);
        await ComposeAsync("Add burn");
        await Expect(3);

        await ScrubTo(0.8);
        await ComposeAsync("Add burn");
        await Expect(4);

        // Adding a burn opens its editor (PR-D2's accordion). Say so out loud rather than assume it: this
        // whole gate is about what an OPEN editor does to the panel's height.
        await _page.Locator(".map-plan-step-open .map-plan-step-edit").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        Assert.True(
            await _page.Locator(".map-plan-step").Last.EvaluateAsync<bool>(
                "el => el.classList.contains('map-plan-step-open')"),
            "the last step's editor is not the open one — the gate is not standing where the owner stood");
    }

    /// <summary>Run the plot clock out to a fraction of the drawn path — the scrub handle, moved from
    /// outside. The native value setter plus an <c>input</c> event is what a range control understands, and
    /// Blazor's <c>@@bind:event="oninput"</c> hears exactly that.</summary>
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

        // …and the panel has actually re-rendered at the new hour before the next press aims at it. The
        // scrub reads "0d 00h 00m" until it moves, so its own words are the signal (never a sleep).
        await _page.WaitForFunctionAsync(
            "() => { const p = document.querySelector('.map-plot'); "
            + "return p !== null && !/Scrub: 0d 00h 00m/.test(p.textContent); }",
            null,
            new() { Timeout = ActionTimeoutMs });
    }

    /// <summary>Press one of the compose buttons at the head of the Plotting panel, by its own words.
    ///
    /// <para>A DISPATCHED click, the same reason <see cref="TallCardTests"/> uses one to get into its card:
    /// on the layout this gate was written to fail, the compose row can itself be off the screen or under a
    /// story plate, and a real click would fail on actionability before the guard reached the thing it is
    /// about. Getting into the state is not what is under test; where the pixels land once we are there is,
    /// and THAT is asked with a real trial click.</para></summary>
    private async Task ComposeAsync(string label) =>
        await _page.Locator(".map-plot-compose button", new() { HasTextString = label })
                   .DispatchEventAsync("click", null, new() { Timeout = ActionTimeoutMs });

    /// <summary>Wait until the plan really has <paramref name="rows"/> rows — the render is what the next
    /// press aims at, so a count is the honest signal to key on (never a sleep).</summary>
    private async Task Expect(int rows) =>
        await _page.Locator(".map-plan-step").Nth(rows - 1).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
}
