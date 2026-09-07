using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #956 · <b>FOLLOW DEST, PRESSED FOR REAL.</b>
///
/// <para>Owner, screenshot of the Nav toolbar, 2026-08-22: <i>"Let's have a follow nav destination option
/// here in addition to to follow ship."</i> #971 answered it — <b>Follow dest</b> stands beside <b>Follow
/// Ship</b>, mutually exclusive with it, greyed with its reason when there is no destination.</para>
///
/// <h3>Why this gate and not the guards that already exist</h3>
/// <para><c>TheKeysOfNavigationAreTheLoudOnesTests</c> drives a real <c>Map</c> and is good at what it does:
/// it proves the toggle flips the field, that the two follows cannot both be live, that the tooltip names the
/// live body, and — since this branch — that the camera rides <i>wherever the plan says we are going now</i>
/// rather than a copy taken when the button went down. Every one of those assertions is made against fields
/// through reflection. What none of them can answer is #680's law — <i>in the DOM is not on the screen</i> —
/// nor the #603 class, a control that quietly does nothing. This button is <c>disabled</c> off a computed
/// property, lives in a crowded wrapping toolbar, and flips its own dress on click; each is a way for it to be
/// perfect in the source and dead under a finger.</para>
///
/// <para>It could not be gated before this branch for the reason <c>?target=</c> had to be invented in #1010:
/// a nav destination had exactly one road, a click on a body's menu drawn on the <b>canvas</b>, and canvas has
/// no DOM for Playwright to reach. <c>?dest=&lt;body-id&gt;</c> (added here, beside <c>?target=</c>, and laid
/// down through the page's own <c>SetDestination</c>) is the URL that buys these pixels.</para>
/// </summary>
public sealed class TheFollowDestButtonIsRealTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    // A desktop window close to the owner's screenshot. Width matters here: the nav toolbar WRAPS, and a
    // narrow viewport that pushed Follow dest onto a hidden row would be exactly the bug.
    private const int Width = 1280;
    private const int Height = 900;

    // The Nav desk's own toolbar row — scoped to it so these locators can never drift onto some other
    // button that happens to carry the same words.
    private const string Toolbar = "[role='toolbar'][aria-label='Time warp controls']";
    private const string FollowDest = Toolbar + " button:has-text('Follow dest')";
    private const string FollowShip = Toolbar + " button:has-text('Follow Ship')";

    // The dress the page gives an engaged follow. Reading the CLASS is reading the pixels' cause: it is the
    // one thing on the screen that tells a captain which follow currently owns the camera.
    private const string Engaged = "btn-info";

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
            ViewportSize = new() { Width = Width, Height = Height },
        });

        _page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                _errors.Add($"console: {msg.Text}");
            }
        };
        _page.PageError += (_, err) => _errors.Add($"uncaught: {err}");
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// THE ASK, AS PIXELS. With a destination set, the button the owner asked for is beside the one he asked
    /// for it beside, on the screen, and a real press hands it the camera — taking it off Follow Ship, which
    /// is the whole reason there is one camera and two buttons.
    /// </summary>
    [Fact]
    public async Task Follow_dest_stands_beside_follow_ship_and_a_real_press_takes_the_camera()
    {
        await BootWithADestination("saturn");

        ILocator dest = _page.Locator(FollowDest);
        ILocator ship = _page.Locator(FollowShip);
        Assert.Equal(1, await dest.CountAsync());

        if (await dest.BoundingBoxAsync() is not { } destBox ||
            await ship.BoundingBoxAsync() is not { } shipBox)
        {
            throw new InvalidOperationException(
                "#956 — a follow button is in the DOM with no box at all: it is not laid out, so there is "
                + "nothing to press.");
        }

        Assert.True(
            destBox.Y >= 0 && destBox.Y + destBox.Height <= Height,
            $"#956 — Follow dest's hit-box runs {destBox.Y:0}…{destBox.Y + destBox.Height:0} px down a "
            + $"{Height} px viewport. A button off the screen is not an option the captain has.");

        // "HERE", which is what the owner actually pointed at: the same toolbar row as Follow Ship, immediately
        // after it. A button that answered the ask but landed in some other corner would pass every other
        // assertion in this file.
        Assert.True(
            Math.Abs(destBox.Y - shipBox.Y) < shipBox.Height,
            $"#956 — Follow dest sits on a different row from Follow Ship (y {destBox.Y:0} vs {shipBox.Y:0}). "
            + "The owner asked for it HERE, beside the follow it is the alternative to.");
        Assert.True(
            destBox.X > shipBox.X,
            "#956 — Follow dest is drawn before Follow Ship; it is the addition, and reads as one only after.");

        // It says whose camera it is offering before it is pressed…
        Assert.Contains("Saturn", await dest.GetAttributeAsync("title") ?? "");

        // …and the press is REAL — Playwright's actionability battery (visible · stable · enabled · not
        // covered by another element) has to agree first, which is the assertion no source-reading or
        // reflection guard can make.
        await dest.ClickAsync(new() { Timeout = ActionTimeoutMs });

        await _page.Locator($"{FollowDest}.{Engaged}").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        // ONE CAMERA. Follow Ship gave it up on the screen, not merely in a field.
        Assert.DoesNotContain(Engaged, await ship.GetAttributeAsync("class") ?? "");

        // …and the frame does not take it back. The follow is read on every tick; a press that the next tick
        // undid would look identical for one paint and be useless.
        await _page.WaitForTimeoutAsync(1_500);
        Assert.Contains(Engaged, await dest.GetAttributeAsync("class") ?? "");
        Assert.DoesNotContain(Engaged, await ship.GetAttributeAsync("class") ?? "");

        Assert.Empty(RealErrors());
    }

    /// <summary>
    /// WITH NOWHERE TO GO, IT IS STILL THERE AND STILL SAYS WHY (#212 — a control that vanishes teaches
    /// nothing). The greying has to be real greying: Playwright refuses to click a disabled button, so the
    /// press below failing IS the assertion.
    /// </summary>
    [Fact]
    public async Task With_no_destination_the_button_is_visibly_offered_and_visibly_refused()
    {
        await BootWithADestination(null);

        ILocator dest = _page.Locator(FollowDest);
        await dest.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        Assert.False(await dest.IsEnabledAsync());

        string tip = await dest.GetAttributeAsync("title") ?? "";
        Assert.Contains("No navigation target", tip);

        // The refusal is the browser's, not ours: a click cannot land on it at all.
        await Assert.ThrowsAnyAsync<Exception>(
            () => dest.ClickAsync(new() { Timeout = 3_000 }));

        Assert.Empty(RealErrors());
    }

    /// <summary>Boot the Nav screen FLYING, with or without a navigation target — the #956 cheat lays the
    /// destination down through the page's own door rather than poking the field.
    ///
    /// <para><b>Why <c>?start=wreck</c> and not one of the berths.</b> Every named berth start routes through
    /// <c>StartDockedAtHaven</c>, and where the haven has a walkable interior that start steps the captain
    /// ASHORE — <c>_deckMode = true</c>, <c>_activeDesk = Deck</c> — so the Nav HUD is not on the screen at
    /// all. The first cut of this gate booted <c>?start=jupiter</c> and it cost an hour: the toolbar is
    /// briefly there during the boot, before <c>ApplyTheStartPoint</c> runs, and then the whole
    /// <c>@if (_worldReady &amp;&amp; !_deckMode)</c> block goes away underneath you. A count taken in that
    /// window says 7 buttons; the very next call finds nothing to measure. The two <c>Test: true</c> starts
    /// (the derelict roadster and the Enceladus band) are the only ones that leave her flying, which is the
    /// state this button belongs to. Worth knowing beyond this file: a boot-window read of the Nav toolbar at
    /// a berth start is reading a screen the player never sits on.</para></summary>
    private async Task BootWithADestination(string? bodyId)
    {
        string dest = bodyId is null ? "" : $"&dest={bodyId}";
        await _page.GotoAsync($"{_host.BaseUrl}/map?scenario=sol&start=wreck{dest}",
            new() { Timeout = BootTimeoutMs });

        // …and it is the SETTLED screen, not the boot window. The start point is applied after the first
        // paint, so a gate that measured immediately could be measuring a HUD that is about to be replaced —
        // which is exactly the trap described above.
        //
        // #161 · THIS USED TO BE "TWO SECONDS OF FRAMES", AND THE TWO SECONDS WERE A FICTION. The Nav HUD
        // is drawn on `_activeDesk == ShipDesk.Nav` alone, so the toolbar is in the DOM from the boot's very
        // first render — measured here at 0.9 s, twelve seconds before `?dest=` is laid down. What made the
        // old wait "work" was the thing #161 exists to kill: the boot pegged the main thread in one
        // unbroken block, so the CDP query behind `WaitForTimeoutAsync` could not be ANSWERED until the
        // world was finished, and the gate read a settled screen by accident. Stage the boot so the browser
        // breathes between ships and the same two seconds land squarely inside the boot window — this gate
        // was the only one of the twenty-eight that noticed, and it noticed by going red on a page that is
        // now strictly more responsive than it was.
        //
        // So it waits on the BOOT instead of on a clock: the "Rigging the sails…" door detaches when the
        // world is ready, and the start point, the cheats and `?dest=` are laid down in the same
        // synchronous run before the next render — so a DOM without that door is a DOM with the
        // destination already in it. No sleep, and nothing left to be lucky about.
        // (Attached first, then detached — `GotoAsync` returns while the page is still an empty shell, and
        // a bare "wait until it is gone" is satisfied instantly by a door that has not been hung yet.)
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Attached, Timeout = BootTimeoutMs });
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
        await _page.Locator(FollowShip).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
    }

    /// <summary>Uncaught JS and console errors seen since the page opened — a follow that engaged by throwing
    /// its way past a null ephemeris would still flip the class, so the dress alone is not the whole
    /// question. The noise the boot gate already calls benign is filtered the same way here, rather than a
    /// second opinion about what a clean console is.</summary>
    private readonly List<string> _errors = new();

    private static readonly string[] Benign =
    [
        "favicon", "Failed to load resource", "net::ERR_", "sourcemap", "source map", "DevTools",
    ];

    private string[] RealErrors() =>
        [.. _errors.Where(e => !Benign.Any(b => e.Contains(b, StringComparison.OrdinalIgnoreCase)))];
}
