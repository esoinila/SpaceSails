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
        ".deck-view-toggle",
        ".captains-remote-btn",
        ".map-layers",
        ".wreck-clocks",
        ".desk-tab-bar",
    ];

    /// <summary>
    /// THE CORNER THE GAUGES OWN — and it MOVES, which the first run of this gate taught me by failing.
    ///
    /// <para>It reported <c>.deck-view-toggle at (12,51) sits over the NERVE gauge</c>, and that was a false
    /// positive that turned out to be worth more than a true one: aboard the ship the gauge is drawn COMPACT at
    /// <c>baseY = 112</c>, deliberately BELOW the first-person toggle ("tucked below the deck chrome … so it
    /// whispers without colliding"), while on a surface excursion it takes the corner outright and the TOGGLE is
    /// the one that steps down to <c>bottom: 2.4rem</c>. Two controls that swap places.</para>
    ///
    /// <para>So a single reserved rectangle is wrong, and asserting one would have forced somebody to "fix" a
    /// layout that was already correct. The gate reads which arrangement it is in from the toggle's own
    /// <c>on-surface</c> class — the same flag the CSS switches on — and reserves accordingly. That is the
    /// arrangement written down in a place that fails when it stops being true.</para>
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

        // Which arrangement are we in? The toggle carries the same flag the CSS switches on.
        bool onSurface = await _page.Locator(".deck-view-toggle.on-surface").CountAsync() > 0;
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

        Assert.True(boxes.Count >= 2, "the deck showed fewer than two HUD controls — the gate proved nothing");

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

        // The Deck tab is where the gauges and the walking HUD live.
        await _page.Locator("button.desk-tab", new() { HasTextString = "Deck" }).ClickAsync();
        await _page.Locator(".deck-view-toggle").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }
}
