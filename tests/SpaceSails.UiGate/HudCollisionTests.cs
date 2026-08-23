using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// THE HUD COLLISION GATE. Owner, after finding the captain's remote sitting on top of two meters:
/// <i>"Let's make a new PR about checking all the new UIs after changes in browser"</i>, and on why it is worth
/// the wall-clock: <i>"yeah it is kind of slow but it does catch the unexpected ones we do not yet check with
/// CI."</i>
///
/// <para><b>This is that check, automated.</b> The bug it exists for has now happened three times in this
/// repository, and no unit test could have caught any of them, because they are all the same shape: a control
/// that is correct, visible, enabled and pressable — sitting on top of something else that is also correct,
/// visible and enabled.</para>
/// <list type="bullet">
/// <item>#482 — the nerve LEDGER drew on top of the motion tracker.</item>
/// <item>#199 — the pilot banner and the Layers control both claimed top-centre.</item>
/// <item>#559 — the captain's remote, at top 3.2rem, buried the nerve readout AND all five condition pips.</item>
/// </list>
///
/// <para><b>Why it belongs here and not in the client tests.</b> Overlap is a question about LAID-OUT PIXELS.
/// The client suite can prove a console is reachable on a deck plan, and does; it cannot know that a CSS rule
/// forty lines from an unrelated one puts a button over a canvas gauge. Only a real browser with a real layout
/// can answer that, which is exactly the trade the owner named — slower, and it catches the ones nothing else
/// does.</para>
/// </summary>
public sealed class HudCollisionTests : IAsyncLifetime
{
    private static readonly float BootTimeoutMs = 180_000;

    /// <summary>
    /// The HUD controls that share the deck with the canvas gauges. Named rather than globbed so a NEW control
    /// has to be added here deliberately — which is the moment somebody asks where it goes, which is the moment
    /// this class exists to create.
    /// </summary>
    private static readonly string[] HudControls =
    [
        ".captains-remote-btn",
        ".map-layers",
        ".wreck-clocks",
        ".desk-tab-bar",
    ];

    /// <summary>
    /// THE CORNER THE GAUGES OWN — and it MOVES, which the first run of this gate taught me by failing.
    ///
    /// <para>It reported a deck control at (12,51) sitting over the NERVE gauge, and that was a false
    /// positive that turned out to be worth more than a true one: aboard the ship the gauge is drawn COMPACT at
    /// <c>baseY = 112</c>, deliberately BELOW the top-left deck chrome ("tucked below the deck chrome … so it
    /// whispers without colliding"), while on a surface excursion it takes the corner outright and the deck
    /// controls step down to the bottom of the column. Two things that swap places.</para>
    ///
    /// <para>So a single reserved rectangle is wrong, and asserting one would have forced somebody to "fix" a
    /// layout that was already correct. The gate reads which arrangement it is in from the DESK TAB BAR, which
    /// the page hides outright on an excursion (#330) — the same fact the CSS and the gauge both switch on —
    /// and reserves accordingly. (It used to read the deck-view toggle's own <c>on-surface</c> class; #958
    /// removed that button with the mode it opened.) That is the arrangement written down in a place that
    /// fails when it stops being true.</para>
    ///
    /// <para>Literals because the gate talks to a PUBLISHED artifact over HTTP and cannot reference the client
    /// assembly; they mirror <c>DeckView.DrawNerveGauge</c>, and the comment above <c>.captains-remote-btn</c> in
    /// <c>Map.razor.css</c> says the same thing from the other side.</para>
    /// </summary>
    private static (float X, float Y, float W, float H) GaugeBand(bool onSurface) => onSurface
        // Surface: plate from (10,10), the readout under it, five condition pips under that.
        ? (0f, 0f, 250f, 95f)
        // Aboard: compact, baseY 112 — the whole stack sits below the deck chrome.
        : (0f, 88f, 210f, 92f);

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

    /// <summary>
    /// NOTHING ON THE DECK COVERS ANYTHING ELSE ON THE DECK — including the two canvas gauges, which have no DOM
    /// node to defend themselves with and so have been buried twice.
    /// </summary>
    [Fact]
    public async Task Deck_hud_controls_do_not_cover_each_other_or_the_gauges()
    {
        await BootIntoTheDeck();

        // Which arrangement are we in? The desk tab bar is hidden outright on an excursion (#330), which is
        // the same fact the gauge's own compact/full switch is made on.
        bool onSurface = await _page.Locator(".desk-tab-bar").CountAsync() == 0;
        (float gx, float gy, float gw, float gh) = GaugeBand(onSurface);

        var boxes = new List<(string Name, float X, float Y, float W, float H)>();
        foreach (string selector in HudControls)
        {
            ILocator control = _page.Locator(selector).First;
            if (await control.CountAsync() == 0 || !await control.IsVisibleAsync())
            {
                continue;   // not every control is up in every state, and that is fine
            }

            if (await control.BoundingBoxAsync() is { } box && box.Width > 0 && box.Height > 0)
            {
                boxes.Add((selector, box.X, box.Y, box.Width, box.Height));
            }
        }

        // #958 · This asked for two. It could, while the deck-view toggle stood in the same corner as the desk
        // tabs; with that button gone the ship's deck shows exactly ONE named control, and the load-bearing half
        // of this gate has always been the one the canvas gauges cannot make for themselves — a DOM control
        // sitting on a painted meter. One control is enough to make that check, and zero is not.
        Assert.True(boxes.Count >= 1, "the deck showed no HUD control at all — the gate proved nothing");

        var collisions = new List<string>();

        for (int i = 0; i < boxes.Count; i++)
        {
            // …against the canvas gauges, which cannot be queried and cannot complain.
            if (Overlaps(boxes[i], (gx, gy, gw, gh)))
            {
                collisions.Add(
                    $"{boxes[i].Name} at ({boxes[i].X:0},{boxes[i].Y:0}) {boxes[i].W:0}×{boxes[i].H:0} " +
                    "sits over the NERVE gauge and the condition pips");
            }

            // …and against each other.
            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (Overlaps(boxes[i], (boxes[j].X, boxes[j].Y, boxes[j].W, boxes[j].H)))
                {
                    collisions.Add($"{boxes[i].Name} overlaps {boxes[j].Name}");
                }
            }
        }

        Assert.True(collisions.Count == 0,
                    "HUD controls are covering each other or the gauges — an affordance you cannot see is one " +
                    "you do not have (#212):\n  " + string.Join("\n  ", collisions));
    }

    /// <summary>
    /// #986 F1 · A DESK'S OWN TITLE IS NOT UNDER THE MAP CONTROLS.
    ///
    /// <para>The boot sweep on #986 walked every desk in a real browser and found "Tracking post 📡" half
    /// struck through: <c>.map-layers</c> at (12,51) 82×31 and <c>.nav-search</c> at (120,51) 240×31 are
    /// absolutely positioned in the top-left corner, and <c>.tracking-post-desk</c>'s card header was laid
    /// out at (69,64) 1440×31 — straight underneath them. The same shape as #482/#199/#559: two things, each
    /// correct, visible and enabled, in one place.</para>
    ///
    /// <para><b>Why the gate above could not see it.</b> That one boots into the DECK and measures the deck's
    /// own chrome. Nothing in this repository had ever measured a DESK's laid-out pixels — which is exactly
    /// how a desk header spent releases sitting under a button.</para>
    ///
    /// <para>Measured at 1280×720, the narrowest viewport the game is laid out for: both controls are anchored
    /// to the top-LEFT and the header stretches the desk's width, so a wider window only moves them further
    /// apart. The War Room is walked in the same pass because it is the OTHER passthrough desk — the two that
    /// shared the clearance exemption which caused this.</para>
    ///
    /// <para>RED PROOF: put <c>:not(.desk-layer-passthrough)</c> back on the two <c>.desk-layer</c> clearance
    /// rules in <c>Map.razor.css</c> and this fails, naming both controls and the pixels they steal.</para>
    /// </summary>
    [Fact]
    public async Task Desk_headers_are_not_covered_by_the_floating_map_controls()
    {
        await _page.SetViewportSizeAsync(1280, 720);
        await BootIntoTheDeck();

        var collisions = new List<string>();

        foreach ((string tab, string header, string desk) in new[]
                 {
                     ("Sensors", ".tracking-post-desk:not(.d-none) > .card-header", "the tracking post"),
                     ("War room", ".war-room-desk:not(.d-none) > .card-header", "the gun deck"),
                 })
        {
            await _page.Locator("button.desk-tab", new() { HasTextString = tab }).ClickAsync();
            await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = tab }).WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

            ILocator title = _page.Locator(header).First;
            await title.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

            // The desk's own title row, as the browser laid it out. If it is not there the gate proves
            // nothing, and says so rather than passing quietly.
            var box = await title.BoundingBoxAsync();
            Assert.True(box is { Width: > 0, Height: > 0 },
                        $"{desk} desk showed no card header — this gate measured nothing");
            (float, float, float, float) headerBox =
                (box!.X, box.Y, box.Width, box.Height);

            foreach (string selector in new[] { ".map-layers", ".nav-search" })
            {
                ILocator control = _page.Locator(selector).First;
                if (await control.CountAsync() == 0 || !await control.IsVisibleAsync())
                {
                    // Both controls are raised on Nav/Sensors/WarRoom (Map.razor); a desk that does not raise
                    // them cannot collide with them, and that is a pass rather than a skip in disguise.
                    continue;
                }
                if (await control.BoundingBoxAsync() is not { } c || c.Width <= 0 || c.Height <= 0)
                {
                    continue;
                }
                if (Overlaps((selector, (float)c.X, (float)c.Y, (float)c.Width, (float)c.Height), headerBox))
                {
                    collisions.Add(
                        $"{selector} at ({c.X:0},{c.Y:0}) {c.Width:0}×{c.Height:0} sits on {desk}'s header at "
                        + $"({headerBox.Item1:0},{headerBox.Item2:0}) {headerBox.Item3:0}×{headerBox.Item4:0} "
                        + "— the desk's own title is struck through");
                }
            }
        }

        Assert.True(collisions.Count == 0,
                    "#986 F1 · the floating map controls are covering a desk's own title — a desk that cannot "
                    + "say its own name is one you have to guess at (#212):\n  "
                    + string.Join("\n  ", collisions));
    }

    private static bool Overlaps(
        (string Name, float X, float Y, float W, float H) a, (float X, float Y, float W, float H) b) =>
        a.X < b.X + b.W && a.X + a.W > b.X && a.Y < b.Y + b.H && a.Y + a.H > b.Y;

    /// <summary>Boot the published artifact and get onto the deck, where the HUD lives.</summary>
    private async Task BootIntoTheDeck()
    {
        await _page.GotoAsync(_host.BaseUrl + "/", new() { Timeout = BootTimeoutMs });
        await _page.Locator("a.btn-primary[href*='scenario=sol']").ClickAsync();

        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });

        await _page.Locator(".start-picker-backdrop").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await _page.Locator(".start-picker-newvoyage").ClickAsync();

        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        // The Deck tab is where the gauges and the walking HUD live. The tab going info-blue is the page
        // saying the desk switched (#958: this used to wait on the deck-view toggle, which no longer exists).
        await _page.Locator("button.desk-tab", new() { HasTextString = "Deck" }).ClickAsync();
        await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = "Deck" }).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }
}
