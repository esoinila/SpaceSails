using System.Linq;
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

    private static readonly float ActionTimeoutMs = 30_000;

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

    /// <summary>
    /// #994 · THE DESK-CHIP STRIP IS ON THE SCREEN, ON EVERY DESK.
    ///
    /// <para>An unclosed <c>.old-crew-reply</c> rule in <c>Map.razor.css</c> (#975) made the browser read the
    /// whole of <c>DeskChips.razor.css</c> as nested children of it, so <c>.desk-chip-strip</c> fell back to
    /// <c>position: static</c> and laid out as a full-width in-flow block BELOW the fold — measured at
    /// 1280×720, a 1280×384 box at y 720. It was drawn, it was visible, it was enabled, and no player could
    /// see it on any desk.</para>
    ///
    /// <para><b>Why the two gates above could not see it.</b> They measure named controls against each other;
    /// a control that walks off the bottom of the world collides with nothing at all and passes every overlap
    /// test ever written. So this asks the other question — is it WHERE IT WAS PUT — and asks it on every
    /// desk, because the strip is the one piece of furniture all eight share.</para>
    ///
    /// <para>RED PROOF: take the closing brace off <c>.old-crew-reply</c> again and every desk fails, naming
    /// a strip 1280 px wide at y 720.</para>
    ///
    /// <para><b>#1021 · SEVEN DESKS, NOT EIGHT.</b> The Galley chip is still on the bar and is no longer a
    /// desk: pressing it raises a pop-up card over whatever desk you are already at and never lights, so the
    /// wait below — "the tab I clicked is now the lit one" — is a wait that would never come back. It is off
    /// this list because there is no eighth desk to sit down at, not because the strip stopped mattering
    /// there: the strip is drawn by the desk UNDER the card and is measured on all seven of those.</para>
    /// </summary>
    [Fact]
    public async Task The_desk_chip_strip_is_positioned_and_on_the_screen_on_every_desk()
    {
        await _page.SetViewportSizeAsync(1280, 720);
        await BootIntoTheDeck();

        var offences = new List<string>();
        int measured = 0;

        foreach (string tab in new[] { "Captain", "Nav", "Sensors", "War room", "Trade", "Comms", "Deck" })
        {
            await _page.Locator("button.desk-tab", new() { HasTextString = tab }).First.ClickAsync();
            await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = tab }).First.WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

            ILocator strip = _page.Locator(".desk-chip-strip").First;
            await strip.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

            string position = await strip.EvaluateAsync<string>("el => getComputedStyle(el).position");
            if (position != "absolute")
            {
                offences.Add($"{tab}: the strip computes `position: {position}` — its own stylesheet is not "
                             + "applying, so it is an in-flow block and not an edge strip");
            }

            var box = await strip.BoundingBoxAsync();
            Assert.True(box is { Width: > 0, Height: > 0 },
                        $"{tab}: the desk-chip strip has no box at all — this gate measured nothing");
            measured++;

            if (box!.Y + box.Height <= 0 || box.Y >= 720 || box.X + box.Width <= 0 || box.X >= 1280)
            {
                offences.Add($"{tab}: the strip is laid out at ({box.X:0},{box.Y:0}) {box.Width:0}×{box.Height:0}"
                             + " — entirely off a 1280×720 screen, which is where a strip goes when its own "
                             + "rules are dead (#994)");
            }
        }

        Assert.Equal(7, measured);   // #1021: the Galley is a card now, not the eighth desk
        Assert.True(offences.Count == 0,
                    "the desk-chip strip is not where DeskChips.razor.css puts it (#994):\n  "
                    + string.Join("\n  ", offences));
    }

    /// <summary>
    /// #994 item 2 · THE STORY PLATE AND THE PLOTTING PANEL NEVER SHARE A PIXEL.
    ///
    /// <para><b>The sighting.</b> Measured on the real page at 1280×720: <c>.story-plate</c> ran y 465…633 and
    /// <c>.map-plot</c> y 520…720, so 480×113 px of the Plotting panel — the step list and whatever editor was
    /// open in it — sat behind an opaque card (<c>rgba(8,11,17,.92)</c>) for the whole of
    /// <c>StoryBeats.PlateSeconds</c>. The plate cannot eat a click (<c>pointer-events: none</c>) and never
    /// did; what it ate was the pixels, and #212's law does not care which.</para>
    ///
    /// <para><b>Why here.</b> The same family as #482/#199/#559/#986: two surfaces, each correct, in one
    /// place. The plate's height is its caption's — the great port's own runs to three lines — so there is no
    /// constant to check, only a laid-out box.</para>
    ///
    /// <para><b>Staging.</b> The plate the Sol boot raises at the berth is real and lasts 7 SIM seconds, so
    /// the drive PAUSES the sim before opening the panel — the captain's own control, and the plate then
    /// cannot expire in the middle of a measurement. Both premises are asserted out loud, so a boot that
    /// stops raising a plate fails this gate instead of passing it while proving nothing.</para>
    ///
    /// <para>RED PROOF: publish the parent commit — the plate absolutely positioned at bottom 5.5rem — and
    /// this fails, naming the overlap in pixels.</para>
    /// </summary>
    [Fact]
    public async Task The_story_plate_never_covers_the_plotting_panel()
    {
        await _page.SetViewportSizeAsync(1280, 720);
        await BootIntoTheDeck();

        await _page.Locator("button.desk-tab", new() { HasTextString = "Nav" }).First.ClickAsync();
        await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = "Nav" }).First.WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        // Stop the clock first: the plate expires on SIM time, and a measurement that races a countdown is a
        // flake waiting to happen. Pause is a control the captain has anyway.
        await _page.Locator("button", new() { HasTextString = "Pause" }).First.ClickAsync();
        await _page.Locator("button", new() { HasTextString = "Plot" }).First.ClickAsync();

        ILocator plate = _page.Locator(".story-plate");
        ILocator panel = _page.Locator(".map-plot");

        // Both premises, out loud. If the berth stops telling its story, or Plot stops opening a panel, this
        // gate has measured nothing and must say so rather than going green.
        await panel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        Assert.True(await plate.CountAsync() > 0 && await plate.IsVisibleAsync(),
                    "no story plate is on the screen at the berth — the Sol boot used to raise one, and "
                    + "without it this gate has nothing to measure the Plotting panel against (#994).");

        if (await plate.BoundingBoxAsync() is not { } a || await panel.BoundingBoxAsync() is not { } b)
        {
            throw new InvalidOperationException("the plate or the panel has no box — nothing was measured");
        }

        float ox = Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X);
        float oy = Math.Min(a.Y + a.Height, b.Y + b.Height) - Math.Max(a.Y, b.Y);

        Assert.True(
            ox <= 0 || oy <= 0,
            $"the story plate (x {a.X:0}…{a.X + a.Width:0}, y {a.Y:0}…{a.Y + a.Height:0}) is lying on the "
            + $"Plotting panel (x {b.X:0}…{b.X + b.Width:0}, y {b.Y:0}…{b.Y + b.Height:0}) — {ox:0}×{oy:0} px "
            + "of the plan is behind an opaque card. The plate is a story, the panel is the work; neither may "
            + "be spent on the other's pixels (#994 item 2).");
    }

    /// <summary>
    /// #1013 · A ROOM FULL OF CONTACTS DOES NOT COVER THE COUNTER'S OWN FOOT.
    ///
    /// <para><b>The sighting.</b> The counter card draws one <c>ContactDrinkOffer</c> block per present bar
    /// contact (<c>PresentBarContacts</c>), each wrapped in its own <c>.deck-offer-actions</c> — the SAME
    /// class the card's real foot (Buy the special / Round for the room / …) uses, and #735/#780 pin that
    /// class <c>position: sticky; bottom: 0</c> with a 12rem box-shadow scrim. With more than one bar
    /// contact in the room, every one of those rows raced the real foot for the identical pinned rectangle:
    /// measured live (four old shipmates at the Roadstead, #973 L5a's own dev cheat), the last contact's row
    /// touched the foot with a 0px gap where every other pair in the card runs ~20px, and the foot's scrim
    /// painted straight over it — "Round for the room" read struck through by "Offer &lt;name&gt; a drink"
    /// exactly as the owner's screenshot showed.</para>
    ///
    /// <para><b>The fix.</b> <c>ContactDrinkOffer</c>'s own wrapper is <c>.contact-offer-row</c> now — the
    /// same flex/wrap/gap/centre rule, none of the sticky/scrim rule — so only the card's one true foot is
    /// still pinned.</para>
    ///
    /// <para>RED PROOF: put <c>.contact-offer-row</c> back to <c>.deck-offer-actions</c> in
    /// <c>ContactDrinkOffer</c> (Map.razor) and this fails, naming the exact pixel gap that collapsed to
    /// zero.</para>
    /// </summary>
    [Fact]
    public async Task A_room_full_of_bar_contacts_never_covers_the_counters_own_foot()
    {
        await _page.GotoAsync(
            _host.BaseUrl + "/map?scenario=sol&oldcrew=1", new() { Timeout = BootTimeoutMs });

        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
        await _page.Locator(".map-page").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        // The ashore boot raises the arrival-tube story plate (ArrivalTube "ONE TUBE, NO CEREMONY") — take
        // it down if it is up so it cannot eat the click-to-walk below.
        ILocator plateClose = _page.Locator(".story-plate-close");
        if (await plateClose.CountAsync() > 0 && await plateClose.IsVisibleAsync())
        {
            await plateClose.ClickAsync();
        }

        // Click-to-walk (#875) straight onto the BARKEEP console — the whole scene is canvas-drawn, so
        // this is a pixel click rather than a DOM locator, at the console's measured spot on the
        // Roadstead's welcome frame (1280x900, oldcrew's default dock). [E] confirms reach. Retried: a
        // single click-to-walk pass can land a few pixels short of the console's own reach radius, and a
        // gate that flakes on the WALK rather than the law it exists to check is a gate nobody would trust.
        ILocator card = _page.Locator(".deck-offer-card");
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await _page.Locator(".map-page").FocusAsync();
            await _page.Mouse.ClickAsync(438, 297);
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

        // Answer every present contact's "is looking at you" face-reveal (#973 L5a) so the real "Offer
        // <name> a drink" row draws instead of the face gate — PresentBarContacts seeds several old
        // shipmates at once, which is exactly the "room full of contacts" shape #1013 needs.
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

        Assert.True(await card.Locator("button", new() { HasTextString = "Round for the room" }).CountAsync() > 0,
                    "the counter never raised its own foot (Round for the room) — this gate proved nothing");

        // The card's own cap is `max-height: calc(100vh - 11rem)` — how many old shipmates PresentBarContacts
        // seeds this run (and how their names wrap) decides whether that cap is ever reached, so a full-size
        // window can pass by accident. Shrinking the viewport forces the card to overflow regardless, which
        // is the one condition every sticky `.deck-offer-actions` sibling needs to race the real foot for the
        // same pinned rectangle (#1013).
        await _page.SetViewportSizeAsync(1280, 420);

        // Every row in the card's body — each present contact's offer row, plus the one true foot — read
        // off the DOM in source order, which is paint/scroll order here.
        var rows = new List<(string Name, float X, float Y, float W, float H)>();
        ILocator rowLocator = card.Locator(".contact-offer-row, .deck-offer-actions");
        int rowCount = await rowLocator.CountAsync();
        Assert.True(rowCount >= 2,
                    $"only {rowCount} action row(s) drew on the counter card — this gate needs a contact row "
                    + "AND the card's own foot to prove they do not collide");
        for (int i = 0; i < rowCount; i++)
        {
            ILocator row = rowLocator.Nth(i);
            string text = (await row.InnerTextAsync()).Replace('\n', ' ').Trim();
            if (await row.BoundingBoxAsync() is { } box && box.Width > 0 && box.Height > 0)
            {
                rows.Add((text.Length > 40 ? text[..40] : text, box.X, box.Y, box.Width, box.Height));
            }
        }

        var collisions = new List<string>();
        for (int i = 0; i < rows.Count; i++)
        {
            for (int j = i + 1; j < rows.Count; j++)
            {
                if (Overlaps(rows[i], (rows[j].X, rows[j].Y, rows[j].W, rows[j].H)))
                {
                    collisions.Add($"'{rows[i].Name}' at ({rows[i].X:0},{rows[i].Y:0}) {rows[i].W:0}×{rows[i].H:0} "
                                   + $"overlaps '{rows[j].Name}' at ({rows[j].X:0},{rows[j].Y:0}) "
                                   + $"{rows[j].W:0}×{rows[j].H:0}");
                }
            }
        }

        Assert.True(collisions.Count == 0,
                    "the counter card's own rows are covering each other (#1013):\n  "
                    + string.Join("\n  ", collisions));
    }

    /// <summary>
    /// #997 · THE DESK-CHIP STRIP DOES NOT PAINT OVER THE SCOPE'S OWN CONTROLS.
    ///
    /// <para><b>The sighting.</b> Walking every desk in a real browser for #994 found two literals that had
    /// never been told about each other: <c>.desk-chip-strip</c> (DeskChips.razor.css) docks a 9.5rem column
    /// 0.5rem off the right edge, and <c>.map-scope</c>/<c>.map-scope-tile</c>/<c>.parrot-perch</c>
    /// (Map.razor.css) each anchored themselves 0.75–0.9rem off the SAME edge, with no idea the strip's
    /// column existed. Measured at 1280×720: the strip ran x 1120…1272, and the scope's card at its old
    /// offset ran x 978…1268 — a 148×32 px overlap where the last chip (Galley) painted over the Scope
    /// card's own header row (<c>◀ AUTO ▶</c> and the <c>–</c> minimise), so a click aimed at the scope's
    /// controls switched desk instead. The parrot's perch (33×36 px) sat behind a chip the same way.</para>
    ///
    /// <para><b>The fix.</b> A single <c>--desk-chip-strip-clearance: 11rem</c> on <c>.map-page</c>, read by
    /// <c>.desk-layer</c>'s right padding AND by <c>.map-scope</c>/<c>.map-scope-tile</c>/<c>.parrot-perch</c>
    /// as their own <c>right</c> — the same move #986 F1 made for the top edge, one number instead of four
    /// that agree by accident.</para>
    ///
    /// <para><b>The other collision this could have traded for.</b> The scope moving to <c>right: 11rem</c>
    /// sits it further under <c>.map-readouts</c>' own horizontal span (x 12…1117 at 1280×720) than before —
    /// but the two are stacked, not side by side: the readouts end at y≈389 and the scope begins at y≈386,
    /// a ≤4 px hairline shared before AND after this change. This gate does not assert that hairline away
    /// (it is not the bug #997 is about and was never zero), but it does assert the strip/scope/perch
    /// collision that #997 IS about.</para>
    ///
    /// <para>RED PROOF: put the literal <c>right: 0.75rem</c> back on <c>.map-scope</c> (or
    /// <c>.map-scope-tile</c>, or <c>right: 0.9rem</c> on <c>.parrot-perch</c>) in place of
    /// <c>var(--desk-chip-strip-clearance)</c> and this fails, naming the exact pixels the strip steals.</para>
    /// </summary>
    [Fact]
    public async Task The_desk_chip_strip_never_covers_the_scopes_own_controls()
    {
        await _page.SetViewportSizeAsync(1280, 720);
        await BootIntoNav();

        ILocator strip = _page.Locator(".desk-chip-strip").First;
        await strip.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        ILocator scope = _page.Locator(".map-scope").First;
        await scope.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        var boxes = new List<(string Name, float X, float Y, float W, float H)>();
        foreach ((string name, ILocator locator) in new (string, ILocator)[]
                 {
                     (".desk-chip-strip", strip),
                     (".map-scope", scope),
                     (".parrot-perch", _page.Locator(".parrot-perch").First),
                 })
        {
            if (await locator.CountAsync() == 0 || !await locator.IsVisibleAsync())
            {
                continue;   // the parrot is a flourish, not a guarantee — absence is not this gate's concern
            }
            if (await locator.BoundingBoxAsync() is { Width: > 0, Height: > 0 } box)
            {
                boxes.Add((name, box.X, box.Y, box.Width, box.Height));
            }
        }

        Assert.True(boxes.Any(b => b.Name == ".desk-chip-strip") && boxes.Any(b => b.Name == ".map-scope"),
                    "the strip or the scope had no box at all on Nav — this gate measured nothing");

        var collisions = new List<string>();
        (string Name, float X, float Y, float W, float H) stripBox = boxes.First(b => b.Name == ".desk-chip-strip");
        foreach ((string Name, float X, float Y, float W, float H) other in boxes.Where(b => b.Name != ".desk-chip-strip"))
        {
            if (Overlaps(other, (stripBox.X, stripBox.Y, stripBox.W, stripBox.H)))
            {
                collisions.Add($"{other.Name} at ({other.X:0},{other.Y:0}) {other.W:0}×{other.H:0} sits under "
                                + $"the desk-chip strip at ({stripBox.X:0},{stripBox.Y:0}) {stripBox.W:0}×{stripBox.H:0}");
            }
        }

        Assert.True(collisions.Count == 0,
                    "the desk-chip strip is painting over the scope's own controls (#997):\n  "
                    + string.Join("\n  ", collisions));
    }

    /// <summary>
    /// #997 · THE SCOPE STAYS ON THE GLASS AT A PHONE WIDTH.
    ///
    /// <para>Verifying the strip/scope fix above at 390×700 (the instructions' own second checkpoint) found
    /// a second trade-off past the one the issue named: <c>.map-scope</c> is a fixed ~290px card (an
    /// unresponsive 280px canvas underneath it, unrelated to this fix), so pushing its <c>right</c> out to a
    /// flat <c>--desk-chip-strip-clearance</c> (11rem) walked its LEFT edge off the left of a 390px screen by
    /// 76px — not overlapping anything, just not there. <c>min(var(...), calc(100% - 18.5rem))</c> on
    /// <c>.map-scope</c>/<c>.map-scope-tile</c> is the guard against that: it only ever bites below a
    /// ~29.5rem-wide viewport, so 1280×720 reads the flat 11rem exactly as #997 specifies (the test above
    /// proves that), and a phone gets a smaller offset instead of losing part of the card off-screen.</para>
    ///
    /// <para>This does not assert zero overlap at 390px — there is not 11rem of clearance plus 18.1rem of
    /// card in a 390px screen to give, and the strip does still touch the scope's header there. It asserts
    /// the narrower, load-bearing fact: the card's own left edge is never negative, i.e. never off the
    /// glass (#212's law, the other direction from the gate above).</para>
    ///
    /// <para>RED PROOF: drop the <c>min(…)</c> back to a bare <c>var(--desk-chip-strip-clearance)</c> on
    /// <c>.map-scope</c> and this fails, naming a negative X.</para>
    /// </summary>
    [Fact]
    public async Task The_scope_stays_on_screen_at_a_phone_width()
    {
        await _page.SetViewportSizeAsync(390, 700);
        await BootIntoNav();

        ILocator scope = _page.Locator(".map-scope").First;
        await scope.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        var box = await scope.BoundingBoxAsync();
        Assert.True(box is { Width: > 0, Height: > 0 }, "the scope had no box at all on Nav at 390×700");

        Assert.True(box!.X >= 0,
                    $"the scope's card sits at x {box.X:0} on a 390px-wide screen — part of it is off the "
                    + "left edge of the glass, not merely covered by something else (#997).");
    }

    private static bool Overlaps(
        (string Name, float X, float Y, float W, float H) a, (float X, float Y, float W, float H) b) =>
        a.X < b.X + b.W && a.X + a.W > b.X && a.Y < b.Y + b.H && a.Y + a.H > b.Y;

    /// <summary>Boot the published artifact and get onto the Nav desk, where the scope lives.</summary>
    private async Task BootIntoNav()
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

        await _page.Locator("button.desk-tab", new() { HasTextString = "Nav" }).First.ClickAsync();
        await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = "Nav" }).First.WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }

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
