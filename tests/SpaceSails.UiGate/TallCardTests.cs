using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// THE TALL-CARD GATE (issue #735). The 2026-08-06 smoke run found the Enceladus restore card — the death
/// card carrying the extra 📄 THE CLINIC'S LEDGER section — rendering its one button, <i>"Board the
/// rustbucket"</i>, BELOW THE FOLD on a 1049 px-tall window. The modal did not scroll, the backdrop did not
/// dismiss, and Enter did nothing: the player was softlocked on a story card until they resized the browser.
///
/// <para><b>Why a browser gate and not a unit test.</b> This is #680's law one level up — <i>in the DOM is
/// not on the screen</i>. Every assertion the client suite can make about that card passed while it was
/// happening: the button was rendered, enabled, wired to the right handler and reachable by every code path
/// that looked. Only a real layout at a real viewport height can answer the only question that mattered,
/// which is whether the pixels are inside the screen.</para>
///
/// <para><b>Why 700 px.</b> The owner's second screen is a PHONE in portrait (mobile RC testing), where every
/// viewport is shorter than the desktop window this failed on — so the gate stands where the player stands.
/// 390 × 700 is a small phone with its browser chrome taken off the top.</para>
///
/// <para><b>What it does NOT assume.</b> The guard does not hunt for the ledger variant. The card family
/// keeps growing sections (the ledger, the nerve ledger, the succession block, and whatever #725's arrival
/// cards add), so the law being pinned is the one that holds for all of them: a card that outgrows the
/// screen must SCROLL, and its action row must be ON the screen at first paint. The second test asserts the
/// card really is taller than the screen it was rendered into, so a future card that quietly got short can
/// never leave these tests passing while proving nothing.</para>
/// </summary>
public sealed class TallCardTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    // A small phone in portrait, browser chrome removed. The number in the issue.
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
            ViewportSize = new() { Width = PhoneWidth, Height = PhoneHeight },
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// THE BUTTON IS ON THE SCREEN. The bug, stated as pixels: the restore card's only way on lay below the
    /// bottom edge of a viewport that could not be scrolled.
    /// </summary>
    [Fact]
    public async Task The_restore_cards_primary_action_lies_inside_a_phone_screen()
    {
        await BootIntoTheRestoreCard();

        ILocator button = _page.Locator(".busted-card .busted-close");
        if (await button.BoundingBoxAsync() is not { } box)
        {
            throw new InvalidOperationException("the restore card's button has no box at all");
        }

        Assert.True(
            box.Y >= 0 && box.Y + box.Height <= PhoneHeight,
            $"the restore card's primary action is off the screen: its hit-box runs "
            + $"{box.Y:0}…{box.Y + box.Height:0} px down a {PhoneHeight} px viewport. A button in the DOM "
            + "that is not on the screen is a locked door — the player cannot go on without resizing the "
            + "browser (#735).");

        // …and Playwright's own actionability battery (visible · stable · enabled · not covered) agrees a
        // real press would land — asked as a trial click, so the card is not actually dismissed here.
        await button.ClickAsync(new() { Trial = true, Timeout = ActionTimeoutMs });
    }

    /// <summary>
    /// THE PROSE IS STILL REACHABLE. Pinning the button would be a cheat if the fix were simply to clip the
    /// card: the words the card exists to say have to be readable too, which means the card scrolls INSIDE
    /// itself. The first assertion is the guard's own honesty check — if the card fits the screen, this test
    /// is proving nothing and says so rather than passing quietly.
    /// </summary>
    [Fact]
    public async Task The_card_that_outgrows_the_screen_scrolls_inside_itself()
    {
        await BootIntoTheRestoreCard();

        ILocator card = _page.Locator(".busted-card");
        int contentHeight = await card.EvaluateAsync<int>("el => el.scrollHeight");
        int boxHeight = await card.EvaluateAsync<int>("el => el.clientHeight");

        // The premise, checked out loud: this card really is taller than the screen it was rendered into.
        // If a future edit makes it short, this guard is proving nothing and must say so rather than pass.
        Assert.True(
            contentHeight > PhoneHeight,
            $"the restore card's content is only {contentHeight} px on a {PhoneHeight} px screen — this "
            + "guard proved nothing, because there was nothing to overflow. Find a taller variant before "
            + "trusting it again.");

        // The law: the card is capped at the screen…
        Assert.True(
            boxHeight <= PhoneHeight,
            $"the card is laid out {boxHeight} px tall on a {PhoneHeight} px screen — it is not capped at "
            + "all, so everything past the fold (including the way on) is simply off the screen (#735).");

        // …and it scrolls, and the scrolling reaches the end of the card.
        int reached = await card.EvaluateAsync<int>(
            "el => { el.scrollTop = el.scrollHeight; return el.scrollTop; }");
        Assert.True(
            reached > 0,
            "the card is taller than the screen and will not scroll — the words below the fold cannot be "
            + "read at all (#735).");

        // …and the button is STILL on the screen with the card scrolled back to the top, which is the whole
        // point of pinning it: the way on is visible before the player has discovered the scroll.
        await card.EvaluateAsync("el => { el.scrollTop = 0; }");
        if (await _page.Locator(".busted-card .busted-close").BoundingBoxAsync() is not { } box)
        {
            throw new InvalidOperationException("the restore card's button has no box at all");
        }

        Assert.True(
            box.Y + box.Height <= PhoneHeight,
            $"scrolled back to the top, the action row is off the screen again (bottom edge {box.Y + box.Height:0} "
            + $"px on a {PhoneHeight} px viewport) — it is riding the prose instead of being pinned (#735).");
    }

    /// <summary>
    /// THE KEYBOARD'S WAY ON. Enter presses the visible primary action of a card that has exactly one — the
    /// second road to the same button, and the one automation can take.
    ///
    /// <para>The keys here are HONEST: nothing is focused by the test and no handler is called directly.
    /// Playwright's keyboard types into whatever the app itself focused (the map div takes focus at the end
    /// of boot), which is exactly the path a player's keystroke takes. The first Enter walks the freeze beat
    /// to the wake; the second acknowledges the restore card and the card is gone.</para>
    /// </summary>
    [Fact]
    public async Task Enter_presses_the_single_visible_action_of_an_open_card()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?scenario=sol&death=impact",
            new() { Timeout = BootTimeoutMs });
        await _page.Locator(".busted-card").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        // The freeze beat is up ("…wake up" is its only button). Enter walks it on.
        await _page.Keyboard.PressAsync("Enter");
        await _page.Locator(".busted-clinic-ledger, .busted-receipt").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        // The restore card is up ("Board the rustbucket" is its only button). Enter acknowledges it.
        await _page.Keyboard.PressAsync("Enter");
        await _page.Locator(".busted-card").WaitForAsync(
            new() { State = WaitForSelectorState.Detached, Timeout = ActionTimeoutMs });
    }

    /// <summary>
    /// Boot straight into the death the cheat stages through the REAL pipeline (<c>?death=impact</c>, #621),
    /// then step the freeze beat on to the restore card — the tall one.
    ///
    /// <para>The step uses a DISPATCHED click rather than a real one on purpose: on the layout this gate was
    /// written to fail, the freeze card's own button can be off the screen too, and a real click would fail
    /// on actionability before the guard ever reached the card it is about. Getting into the state is not
    /// the thing under test; where the pixels land once we are there is.</para>
    /// </summary>
    private async Task BootIntoTheRestoreCard()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?scenario=sol&death=impact",
            new() { Timeout = BootTimeoutMs });

        ILocator wake = _page.Locator(".busted-card .busted-close");
        await wake.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await wake.DispatchEventAsync("click");

        // The wake receipt is the restore card's own fingerprint — the freeze beat has no receipt.
        await _page.Locator(".busted-card .busted-receipt").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
    }
}
