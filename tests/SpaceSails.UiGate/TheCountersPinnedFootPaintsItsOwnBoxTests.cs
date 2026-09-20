using System.Text.Json;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1271 · <b>THE PINNED FOOT PAINTS ITS OWN BOX.</b>
///
/// <para><b>The sighting.</b> Owner's boot of §8 of <c>docs/testing-links-2026-09-17.md</c>, 2026-09-20,
/// 1440 × 1100: <c>/map?ashore=1&amp;dock=ringside-exchange</c> → the counter → <c>[E]</c> → <b>See the
/// menu</b>, and at the card's <i>default</i> scroll the house-pour row is drawn straight THROUGH the five
/// pinned buttons — <c>🥃 RINGSIDE · 8 cr</c> and its ingredient line superimposed on <c>📜 Hide the
/// menu</c>, <c>🍻 Round for the room · 50 cr</c>, <c>👂 Hear a rumor</c> and <c>Done</c>. On a fuller room
/// the card is taller and it is <b>THE BOARD</b> in the band instead. Scrolled to the end, both render
/// perfectly: the content and the ordering were never the fault.</para>
///
/// <para><b>What the band actually was.</b> <c>background-color: rgba(0, 0, 0, 0)</c>. #780 pins the row
/// <c>position: sticky</c> and hangs a scrim off it as <c>box-shadow: 0 12rem 0 12rem &lt;dark&gt;</c> —
/// offset EQUALS spread, deliberately, so the scrim starts at the row's own top edge and all of it falls
/// BELOW (reach higher and it swallows the footnote the first-ground cards print above their button). A
/// box-shadow never fills the border box it is cast from, so the row's own rectangle painted nothing at
/// all. Invisible while a foot is one line of buttons, because a button fills its own box; the haven
/// counter wraps five of them onto two and three lines and the gaps are where the card's prose showed
/// through.</para>
///
/// <para><b>Why this gate and not a unit test, and why not the usual instrument.</b> Nothing was clickable
/// through it — the issue's own sweep put <c>elementFromPoint</c> over 110 points inside the band and got
/// <c>DIV.deck-offer-actions</c> at every one. So the question
/// <see cref="TheDestinationPanelIsNeverPaintedOverTests"/> asks, which is the right question for a
/// STACKING fault, answers GREEN on the broken build: hit-testing does not care what a box is filled with.
/// Geometry answers nothing either, in either direction — the rows are SUPPOSED to lie under the foot,
/// that is what a sticky foot over a scrolling body is for. The only witness that can tell this bug from
/// its fix is the pixels, so the pixels are what is asked.</para>
///
/// <para><b>The instrument.</b> The band's rectangle is photographed twice at one scroll position: once as
/// the captain sees it, and once with the card's own prose hidden (<c>visibility: hidden</c>, which keeps
/// every box exactly where it was, so the band cannot move between the two frames — and the guard checks
/// that it did not). If the band fills its own box the two frames are identical. If anything at all reads
/// through it they are not. The deck canvas is hidden for BOTH frames and restored afterwards: it is a live
/// animation behind a card that is only 92% opaque, and a frame that photographs a moving starfield would
/// be measuring the sky rather than the foot.</para>
///
/// <para><b>RED PROOF.</b> Delete the <c>background-color: var(--card-foot-fill, …)</c> rule from
/// <c>Map.razor.css</c> — the shipped CSS this issue was filed against — and every world fails, naming the
/// rows that were standing in the band and how many bytes the two frames differ by. Measured: world one
/// reds on <c>.bar-menu-item</c> <i>"🥃 RINGSIDE · 8 cr"</i> 60 px inside the band, world two on
/// <c>.contact-offer-row</c> <i>"🥃 Offer Ilse Marrow a drink · 6 cr"</i> 12 px inside it.</para>
/// </summary>
public sealed class TheCountersPinnedFootPaintsItsOwnBoxTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    // The window the walk is driven in. HudCollisionTests' own numbers, and they are not a preference: the
    // bar is a CANVAS, so reaching the counter is a pixel click at a measured spot on this frame
    // (A_room_full_of_bar_contacts_never_covers_the_counters_own_foot walks to the same console). Every
    // haven bar is laid out from the same HavenInterior constants and `?ashore=1` stands the captain on the
    // same threshold at each, so the spot is the berth-independent one.
    private const int WalkWidth = 1280;
    private const int WalkHeight = 900;
    private const int CounterClickX = 438;
    private const int CounterClickY = 297;

    // The owner's own window in #1271…
    private const int WideWidth = 1440;
    private const int WideHeight = 1100;

    // …and TallCardTests' phone, where #780 wraps the foot onto three lines and the band is ~180 px tall.
    private const int PhoneWidth = 390;
    private const int PhoneHeight = 700;

    // HudCollisionTests' own short window — #1013's, where a room full of faces pushes the card past its
    // cap at a desktop width and the Roadstead's four-line foot owns 148 px of a 244 px scrollport.
    private const int ShortWidth = 1280;
    private const int ShortHeight = 420;

    private const string Card = ".deck-offer-card";
    private const string Band = ".deck-offer-card > .deck-offer-actions";

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
            ViewportSize = new() { Width = WalkWidth, Height = WalkHeight },
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// THE LAW, AT BOTH OF THE ISSUE'S VIEWPORTS AND IN BOTH OF THE ROOMS THAT MAKE THE CARD TALL. Built
    /// once per world — the boot and the walk are the expensive part — and checked at each viewport by
    /// resizing in place, the same window a captain would resize.
    ///
    /// <para><b>World one, the menu.</b> #1271's own repro: the Ringside with <b>See the menu</b> pressed,
    /// where seven priced rows and the board push the card past its cap, at the card's DEFAULT scroll.</para>
    ///
    /// <para><b>World two, the room.</b> The Roadstead with its old shipmates in it (#973 L5a's cheat) and
    /// the menu SHUT — the card is tall because of WHO IS STANDING AT THE COUNTER, and the contact rows are
    /// typed ABOVE the menu, so what stands in the band here is a face's own <i>Offer &lt;name&gt; a drink</i>
    /// rather than a drink. This is the shape #1013 was filed about and the shape <c>HudCollisionTests</c>
    /// measures the flow of; here the same rectangle is asked the one question a box cannot answer, which is
    /// what it is FILLED with.</para>
    ///
    /// <para><b>And world two is driven at 390×700 and at 1280×420, not at 1440×1100.</b> Measured: with the
    /// menu shut, a full room is <b>752 px of content against a 924 px cap</b> on the tall window — the card
    /// does not overflow at all, nothing is under the foot, and a guard asserted there would be proving
    /// nothing while going green. 1280×420 is <c>HudCollisionTests</c>' own window, the one #1013 was
    /// measured in; its foot wraps onto four lines and takes 148 px of a 244 px scrollport.</para>
    /// </summary>
    [Fact]
    public async Task The_counters_pinned_foot_paints_over_the_menu_that_scrolls_under_it()
    {
        await BootIntoTheCounterCardWithTheMenuOpen();

        await Resize(WideWidth, WideHeight);
        await AssertTheFootFillsItsOwnBox($"the Ringside's menu at {WideWidth}x{WideHeight}");

        await Resize(PhoneWidth, PhoneHeight);
        await AssertTheFootFillsItsOwnBox($"the Ringside's menu at {PhoneWidth}x{PhoneHeight}");

        await BootIntoACounterWithAContactAtIt();

        await Resize(PhoneWidth, PhoneHeight);
        await ScrollUntilTheBandStandsOverSomething();
        await AssertTheFootFillsItsOwnBox($"a contact at the counter at {PhoneWidth}x{PhoneHeight}");

        await Resize(ShortWidth, ShortHeight);
        await ScrollUntilTheBandStandsOverSomething();
        await AssertTheFootFillsItsOwnBox($"a contact at the counter at {ShortWidth}x{ShortHeight}");
    }

    /// <summary>
    /// WORLD TWO'S ONE CONCESSION, AND IT IS THE CAPTAIN'S OWN THUMB. #1271 is a claim about the card's
    /// DEFAULT scroll and world one is held to exactly that. In a room full of faces the contact rows are
    /// typed near the TOP of the card, so which of them stands in the band depends on how many the room
    /// seeded — measured at 1280×420, a full Roadstead is 752 px of content in a 242 px scrollport and at
    /// scrollTop 0 the band lands on the card's intro sentence rather than on a face.
    ///
    /// <para>So the card is scrolled, half a band-height at a time, until the band really is standing over a
    /// row: the position a captain reaches by reading down the card. The law is not about one offset — a
    /// foot that fills its own box fills it at every offset there is, and a foot that does not is
    /// see-through at all of them. Bounded, and it never re-reads until it likes the answer: if no offset
    /// puts a row under the foot, the caller's own premise assertion says the guard measured nothing.</para>
    /// </summary>
    private async Task ScrollUntilTheBandStandsOverSomething()
    {
        await _page.SettledAsync($"{Card}, {Band}");
        for (int step = 0; step < 12; step++)
        {
            if ((await Read("the scroll search")).Rows.Length > 0)
            {
                return;
            }

            bool moved = await _page.EvaluateAsync<bool>(
                """
                () => {
                    const card = [...document.querySelectorAll('.deck-offer-card')]
                        .find(c => c.getClientRects().length > 0);
                    const band = card.querySelector(':scope > .deck-offer-actions');
                    const was = card.scrollTop;
                    card.scrollTop = Math.min(was + Math.max(40, band.getBoundingClientRect().height / 2),
                                              card.scrollHeight - card.clientHeight);
                    return Math.round(card.scrollTop) !== Math.round(was);
                }
                """);
            if (!moved)
            {
                return;
            }

            await _page.SettledAsync($"{Card}, {Band}");
        }
    }

    // ── THE LAW ─────────────────────────────────────────────────────────────────────────────────────────

    private async Task AssertTheFootFillsItsOwnBox(string atSize)
    {
        // #1234 · nothing may have moved for three quarters of a second before a box is read.
        await _page.SettledAsync($"{Card}, {Band}");

        Reading before = await Read(atSize);

        // THE PREMISE, OUT LOUD (#735's honesty clause). The band must really be standing over the card's
        // own prose at the scroll the card opens at — otherwise the frames below would be identical on the
        // broken build too, and this guard would be proving nothing while going green.
        Assert.True(
            before.Rows.Length > 0,
            $"at {atSize} not one row of the counter's menu lies inside the pinned band's box at the card's "
            + $"default scroll (card {before.ClientHeight} px tall over {before.ScrollHeight} px of content, "
            + $"scrollTop {before.ScrollTop}; band {before.Band.Width:0} × {before.Band.Height:0}). This "
            + "guard has measured nothing — there was nothing under the foot to be painted over. Find a "
            + "boot where the card overflows before trusting it again (#1271).");

        // Both frames are taken with the deck canvas hidden: it is a live animation, and the card over it
        // is 92% opaque, so a starfield that moved between the two shots would redden this on the sky.
        await HideTheDeckCanvas(true);
        try
        {
            byte[] asPlayed = await PhotographTheBand(before.Band);

            // The prose goes invisible — which keeps every box exactly where it is, so the band cannot
            // move — and the same rectangle is photographed again. This frame is the band over the card's
            // own fill and nothing else: what the captain is supposed to be looking at.
            int hidden = await HideTheCardsProse(true);
            Assert.True(hidden > 0, $"at {atSize} the counter card has no children beside its own foot — "
                                    + "this guard could not have hidden anything (#1271).");
            try
            {
                await _page.SettledAsync(Band);
                Reading after = await Read(atSize);

                Assert.True(
                    SameBox(before.Band, after.Band),
                    $"at {atSize} the pinned band MOVED between the two frames — "
                    + $"{Describe(before.Band)} then {Describe(after.Band)} — so the comparison below would "
                    + "be about two different rectangles. `visibility: hidden` is supposed to keep every box "
                    + "where it is; something in the card is laying itself out off its siblings' visibility.");

                byte[] overNothing = await PhotographTheBand(before.Band);

                if (!asPlayed.AsSpan().SequenceEqual(overNothing))
                {
                    string saved = SaveTheTwoFrames(atSize, asPlayed, overNothing);
                    Assert.Fail(
                        $"#1271 · at {atSize} the counter card's PINNED BAND does not fill its own box: the "
                        + "same rectangle photographed over the card's prose and over the card's bare fill "
                        + $"is not the same picture ({asPlayed.Length} vs {overNothing.Length} bytes of "
                        + "PNG).\n\n"
                        + "What was standing in the band:\n  "
                        + string.Join("\n  ", before.Rows)
                        + "\n\nThe band is `position: sticky` with a 12rem box-shadow scrim (#735/#780), and "
                        + "a box-shadow never fills the border box it is cast from — the row's own rectangle "
                        + "needs a background of its own (`--card-foot-fill` in Map.razor.css). Without one, "
                        + "the menu is read THROUGH the buttons and the buttons through the menu, and which "
                        + "line lands there moves with every contact in the room.\n\n"
                        + saved);
                }
            }
            finally
            {
                await HideTheCardsProse(false);
            }
        }
        finally
        {
            await HideTheDeckCanvas(false);
        }
    }

    // ── THE INSTRUMENTS ─────────────────────────────────────────────────────────────────────────────────

    private readonly record struct Box(double X, double Y, double Width, double Height);

    private readonly record struct Reading(Box Band, int ScrollTop, int ScrollHeight, int ClientHeight, string[] Rows);

    private static string Describe(Box b) => $"{b.Width:0}×{b.Height:0} at {b.X:0},{b.Y:0}";

    /// <summary>Two rectangles are the same when nothing has moved by as much as half a device pixel —
    /// the band is laid out in fractional CSS pixels and a re-read of an unchanged layout can differ in
    /// the last bit of a double.</summary>
    private static bool SameBox(Box a, Box b) =>
        Math.Abs(a.X - b.X) < 0.5 && Math.Abs(a.Y - b.Y) < 0.5
        && Math.Abs(a.Width - b.Width) < 0.5 && Math.Abs(a.Height - b.Height) < 0.5;

    /// <summary>The band's box, the card's scroll state, and every row of the card's own prose whose box
    /// lies inside the band's — named and quoted, so a failure says what was being read through the
    /// buttons rather than just that something was.</summary>
    private async Task<Reading> Read(string atSize)
    {
        string json = await _page.EvaluateAsync<string>(
            """
            () => {
                const card = [...document.querySelectorAll('.deck-offer-card')]
                    .find(c => c.getClientRects().length > 0);
                if (!card) { return JSON.stringify({ found: false }); }
                const band = card.querySelector(':scope > .deck-offer-actions');
                if (!band) { return JSON.stringify({ found: false }); }
                const b = band.getBoundingClientRect();
                const rows = [];
                const prose = '.bar-menu-head, .bar-menu-item, .bar-board-head, .bar-board-item, '
                            + '.bar-overheard-line, .deck-offer-flavor, .bar-menu-flavor, '
                            + '.bar-board-line, .contact-offer-row';
                for (const el of card.querySelectorAll(prose)) {
                    const r = el.getBoundingClientRect();
                    const over = Math.min(r.bottom, b.bottom) - Math.max(r.top, b.top);
                    if (over > 0.5 && r.width > 0 && r.height > 0) {
                        const words = (el.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 70);
                        rows.push(`.${el.className.split(' ')[0]} — ${Math.round(over)} px inside the band`
                                  + (words ? `: "${words}"` : ''));
                    }
                }
                return JSON.stringify({
                    found: true,
                    x: b.x, y: b.y, w: b.width, h: b.height,
                    scrollTop: Math.round(card.scrollTop),
                    scrollHeight: Math.round(card.scrollHeight),
                    clientHeight: Math.round(card.clientHeight),
                    rows,
                });
            }
            """);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        Assert.True(root.GetProperty("found").GetBoolean(),
                    $"at {atSize} there is no visible counter card with a pinned foot on the screen — this "
                    + "guard has nothing to measure (#1271).");

        return new Reading(
            new Box(root.GetProperty("x").GetDouble(), root.GetProperty("y").GetDouble(),
                    root.GetProperty("w").GetDouble(), root.GetProperty("h").GetDouble()),
            root.GetProperty("scrollTop").GetInt32(),
            root.GetProperty("scrollHeight").GetInt32(),
            root.GetProperty("clientHeight").GetInt32(),
            [.. root.GetProperty("rows").EnumerateArray().Select(e => e.GetString() ?? "")]);
    }

    /// <summary>One frame of exactly the band's rectangle. Clipped in PAGE coordinates, which is what
    /// Playwright takes, so the page's own scroll offset is added to the viewport box read above.</summary>
    private async Task<byte[]> PhotographTheBand(Box band)
    {
        double[] origin = await _page.EvaluateAsync<double[]>("() => [window.scrollX, window.scrollY]");
        return await _page.ScreenshotAsync(new()
        {
            Type = ScreenshotType.Png,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
            Clip = new()
            {
                X = (float)(band.X + origin[0]),
                Y = (float)(band.Y + origin[1]),
                Width = (float)band.Width,
                Height = (float)band.Height,
            },
        });
    }

    /// <summary>Everything in the card EXCEPT its own foot, made invisible — boxes and all layout intact,
    /// which is the whole reason it is `visibility` and not `display`.</summary>
    private Task<int> HideTheCardsProse(bool hide) => _page.EvaluateAsync<int>(
        """
        (hide) => {
            const card = [...document.querySelectorAll('.deck-offer-card')]
                .find(c => c.getClientRects().length > 0);
            if (!card) { return 0; }
            const band = card.querySelector(':scope > .deck-offer-actions');
            let n = 0;
            for (const kid of card.children) {
                if (kid === band) { continue; }
                kid.style.visibility = hide ? 'hidden' : '';
                n++;
            }
            return n;
        }
        """, hide);

    /// <summary>The deck, hidden for both frames. It is a live canvas under a 92%-opaque card, so leaving
    /// it running would put a moving starfield in the comparison — a flake that says nothing about the
    /// foot. Hidden, not stopped: the game goes on doing exactly what it was doing.</summary>
    private Task HideTheDeckCanvas(bool hide) => _page.EvaluateAsync(
        """
        (hide) => {
            for (const c of document.querySelectorAll('canvas')) {
                c.style.visibility = hide ? 'hidden' : '';
            }
        }
        """, hide);

    private static string SaveTheTwoFrames(string atSize, byte[] asPlayed, byte[] overNothing)
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetEnvironmentVariable("SPACESAILS_UIGATE_ARTIFACTS") is { Length: > 0 } fromEnv
                    ? fromEnv
                    : Path.Combine(AppContext.BaseDirectory, "ui-gate-artifacts"));
            Directory.CreateDirectory(dir);
            string stem = "foot-1271-" + string.Concat(
                atSize.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
            string a = Path.Combine(dir, stem + "-as-played.png");
            string b = Path.Combine(dir, stem + "-over-the-bare-card.png");
            File.WriteAllBytes(a, asPlayed);
            File.WriteAllBytes(b, overNothing);
            return $"Both frames were written for a reader:\n  {a}\n  {b}";
        }
        catch (IOException ex)
        {
            return $"(the two frames could not be written out: {ex.Message})";
        }
    }

    // ── GETTING THERE ───────────────────────────────────────────────────────────────────────────────────

    private async Task Resize(int width, int height)
    {
        await _page.SetViewportSizeAsync(width, height);
        await _page.WaitForFunctionAsync(
            $"() => window.innerWidth === {width} && window.innerHeight === {height}",
            null, new() { Timeout = ActionTimeoutMs });
    }

    /// <summary>
    /// The issue's own repro: ashore at the Ringside Exchange, walk to the counter, <c>[E]</c>, and press
    /// <b>See the menu</b>. The walk is a pixel click because the room is a canvas, and it is retried for
    /// the reason <c>HudCollisionTests</c> gives: a single click-to-walk pass can land a few pixels short
    /// of the console's own reach radius, and a gate that flakes on the WALK rather than on the law it
    /// exists to check is a gate nobody would trust.
    /// </summary>
    private async Task BootIntoTheCounterCardWithTheMenuOpen()
    {
        ILocator card = await WalkToACounter("ringside-exchange");

        // …and open the drinks menu, which is the one press between the counter card and #1271's screen.
        ILocator seeTheMenu = card.Locator("button", new() { HasTextString = "See the menu" });
        Assert.True(await seeTheMenu.CountAsync() > 0,
                    "the counter card came up without a `See the menu` button — the walk landed somewhere "
                    + "that is not the Ringside's counter, so this guard would be measuring the wrong card.");
        await seeTheMenu.First.ClickAsync();

        await _page.Locator(".bar-menu").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        // The card opens at the top and STAYS there — #1271 is a claim about the DEFAULT scroll, and a
        // fixture that scrolled it would be photographing a screen the captain never sees.
        Assert.Equal(0, await card.EvaluateAsync<int>("el => Math.round(el.scrollTop)"));
    }

    /// <summary>
    /// #1013's own room, and the second half of #1271: the Roadstead with its old shipmates in it, the
    /// drinks menu SHUT. What makes the card taller than its cap here is WHO IS AT THE COUNTER, so what
    /// stands in the pinned band is a contact's offer row — the same rectangle <c>HudCollisionTests</c> has
    /// been measuring the flow of since #1013, asked what it is filled with instead.
    ///
    /// <para>Each present face opens with an "is looking at you" gate (#973 L5a); they are answered here so
    /// the real <c>Offer &lt;name&gt; a drink</c> rows draw, exactly as that gate does it.</para>
    /// </summary>
    private async Task BootIntoACounterWithAContactAtIt()
    {
        await Resize(WalkWidth, WalkHeight);
        ILocator card = await WalkToACounter(null, "&oldcrew=1");

        for (int i = 0; i < 8; i++)
        {
            ILocator faceBtn = card.Locator("button", new() { HasTextString = "is looking at you" });
            if (await faceBtn.CountAsync() == 0)
            {
                break;
            }
            await faceBtn.First.ClickAsync();
            await _page.Locator("button.old-crew-answer").First.ClickAsync();
            await _page.Locator("button", new() { HasTextString = "Leave it there" }).ClickAsync();
        }

        Assert.True(await card.Locator(".contact-offer-row").CountAsync() > 0,
                    "nobody the captain knows is drinking at this counter, so no contact row was drawn — "
                    + "this half of the guard would be measuring the same screen as the first (#1013/#1271).");
    }

    /// <summary>
    /// Ashore at a berth, walk to the counter, <c>[E]</c>. The walk is a pixel click because the room is a
    /// canvas, and it is retried for the reason <c>HudCollisionTests</c> gives: a single click-to-walk pass
    /// can land a few pixels short of the console's own reach radius, and a gate that flakes on the WALK
    /// rather than on the law it exists to check is a gate nobody would trust.
    /// </summary>
    private async Task<ILocator> WalkToACounter(string? dock, string extra = "")
    {
        // `&holdbeats=1` — this fixture presses its way through a card, and a story beat whose cadence
        // lands mid-drive paints its own backdrop over the button about to be pressed (#1148).
        await _page.GotoAsync(
            _host.BaseUrl + "/map?scenario=sol&ashore=1&holdbeats=1"
            + (dock is null ? "" : $"&dock={dock}") + extra,
            new() { Timeout = BootTimeoutMs });

        await _page.BootDoorClosedAsync(BootTimeoutMs);
        await _page.Locator(".map-page").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        // The ashore boot raises the arrival-tube story plate — take it down if it is up so it cannot eat
        // the click-to-walk.
        ILocator plateClose = _page.Locator(".story-plate-close");
        if (await plateClose.CountAsync() > 0 && await plateClose.IsVisibleAsync())
        {
            await plateClose.ClickAsync();
        }

        ILocator card = _page.Locator(Card);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await _page.Locator(".map-page").FocusAsync();
            await _page.Mouse.ClickAsync(CounterClickX, CounterClickY);
            await _page.WaitForTimeoutAsync(2500);
            await _page.Locator(".map-page").FocusAsync();
            await _page.Keyboard.PressAsync("e");
            await _page.WaitForTimeoutAsync(500);
            if (await card.CountAsync() > 0 && await card.IsVisibleAsync())
            {
                break;
            }
        }

        await card.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        return card;
    }
}
