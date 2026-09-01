using System.Text.Json;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1027 · <b>THE POCKET IS THE THING YOU SEE WHEN YOU PRESS I.</b>
///
/// <para>Found by the boot-every-scene sweep's maiden run (2026-08-30) and confirmed by a skeptic re-boot:
/// boot <c>?rip=1</c>, dismiss the first-ground briefing, and 🛗 THE SHAFT stands over the scene. Press
/// <b>I</b> and NOTHING VISIBLY HAPPENS. The satchel is in the DOM — the heading, CARRIED (3), all three
/// rows with their verbs — but it wore the same <c>.view-object-backdrop</c> at the same z-index 1320 as
/// the arrival card, and its block is typed EARLIER in <c>Map.razor</c> (~2480) than the card's (~3550), so
/// the later paint won and the pocket opened behind the picture. With a second card queued behind the first
/// the key looks completely dead. That is the #603 class: a control that quietly does nothing.</para>
///
/// <para><b>Why this gate and not a unit test.</b> #680's law — <i>in the DOM is not on the screen</i>. Every
/// browser-free assertion that could be made about this passed while it was happening: the satchel rendered,
/// its rows were right, its ✕ was wired, <c>_showSatchel</c> was true. The only witness that could tell the
/// bug from the fix is a real stacking context, and the sharpest question to ask it is the one the issue
/// asked: <c>document.elementFromPoint</c> at the middle of the pocket — which returned the arrival card's
/// image. So that is what is asserted here, on the pixels, through the player's own keystroke.</para>
///
/// <para><b>What it also refuses to accept.</b> The fix could have been made by having the pocket DISMISS the
/// cards it was buried under, and that would be a worse game: the arrival beats are told-once and latch when
/// they are raised, so a satchel that waved them away would silently spend a beat nobody read. The last two
/// assertions here pin the alternative that was chosen — the card is untouched while the pocket is over it,
/// and it is still standing when the pocket shuts.</para>
/// </summary>
public sealed class TheSatchelPaintsOverTheCardTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    // The desktop window the sweep ran in. Nothing here is a layout-height question (that is TallCardTests'),
    // so this is simply a window big enough that both surfaces are comfortably drawn.
    private const int Width = 1280;
    private const int Height = 900;

    // The card the cheat's own descent raises, and the pocket. Both are named in the issue.
    private const string ArrivalCard = ".view-object-backdrop:not(.satchel-backdrop)";
    private const string Satchel = ".satchel";

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
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// THE BUG, AS PIXELS. With an arrival card standing over the scene, the captain presses I — and what is
    /// under the middle of the screen must be the pocket he just opened, not the picture he was already
    /// looking at.
    /// </summary>
    [Fact]
    public async Task Pressing_I_under_an_arrival_card_puts_the_satchel_on_top()
    {
        await BootIntoTheArrivalCard();

        // The premise, checked out loud before anything is proved: a card really is standing over the scene.
        // Without one this test is pressing I into an empty room and proving nothing.
        Assert.True(
            await _page.Locator(ArrivalCard).IsVisibleAsync(),
            "no arrival card is up after the ?rip=1 boot — this guard proved nothing, because there was "
            + "nothing for the satchel to be buried under. Find a boot that raises one before trusting it.");

        await PressTheSatchelKey();

        // 1 · IT IS ON THE SCREEN AT ALL. (It always was — this is the assertion the old build also passed,
        //     and it is here so a failure below cannot be misread as "the satchel did not open".)
        await _page.Locator(Satchel).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        // 2 · AND IT IS WHAT THE MOUSE WOULD HIT. The issue's own instrument: elementFromPoint at the middle
        //     of the pocket. On the broken build this returns the arrival card's <img>.
        string hitReport = await _page.EvaluateAsync<string>(
            """
            () => {
                const card = document.querySelector('.satchel');
                const box = card.getBoundingClientRect();
                const hit = document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2);
                if (!hit) { return 'nothing at all is under the middle of the satchel'; }
                return card.contains(hit)
                    ? ''
                    : `the top paint at the middle of the satchel is <${hit.tagName.toLowerCase()} `
                      + `class="${hit.getAttribute('class') ?? ''}">, which is NOT inside the satchel`;
            }
            """);

        Assert.True(
            hitReport.Length == 0,
            "the satchel opened UNDERNEATH the card the captain was looking at (#1027): " + hitReport
            + ". Pressing I is supposed to hand the captain his own pocket; a key that opens a surface "
            + "nobody can see is a dead key, which is the #603 class this repository has been burned by "
            + "before.");

        // 3 · AND IT IS ON TOP BECAUSE THE BAND SAYS SO, not because of the order two blocks happen to be
        //     typed in. Read off the live computed styles — the whole bug was two surfaces agreeing on 1320
        //     and letting document order break the tie.
        Layers z = await ReadTheLayers();
        Assert.True(
            z.Satchel > z.Card,
            $"the satchel paints at z-index {z.Satchel} and the card at {z.Card} — the pocket must OUT-RANK "
            + "the cards rather than tie with them, or which one the player sees goes back to being decided "
            + "by which block was typed first in Map.razor (#1027).");
    }

    /// <summary>
    /// NOTHING WAS SPENT TO MAKE ROOM. The rejected fix was to have the satchel dismiss the cards under it;
    /// the arrival beats latch when they are raised, so that would burn a told-once beat the player never
    /// read. The card is still there under the open pocket, and still there when the pocket shuts.
    /// </summary>
    [Fact]
    public async Task The_card_underneath_is_neither_spent_nor_lost()
    {
        await BootIntoTheArrivalCard();
        string titleBefore = await _page.Locator(ArrivalCard + " .view-object-title").InnerTextAsync();

        await PressTheSatchelKey();
        await _page.Locator(Satchel).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        Assert.True(
            await _page.Locator(ArrivalCard).CountAsync() > 0,
            $"the card '{titleBefore}' was taken off the screen by the satchel opening. The arrival beats "
            + "are told ONCE and latch at the moment they are raised, so a pocket that dismissed them would "
            + "spend a beat the captain never read — the pocket goes OVER them instead (#1027).");

        // Esc peels the top-most surface, and the top-most surface is now the pocket. On the old chain the
        // satchel was not in TryDismissTopOverlay AT ALL, so this key fell straight through it and closed
        // the card underneath — the bug this issue names, running backwards.
        await _page.Keyboard.PressAsync("Escape");
        await _page.WaitForTimeoutAsync(750);

        bool pocketStillUp = await _page.Locator(Satchel).CountAsync() > 0;
        int cardsLeft = await _page.Locator(ArrivalCard).CountAsync();

        Assert.False(
            pocketStillUp,
            "Escape did not close the satchel — the cancel key must peel the TOP-MOST surface, and the "
            + $"pocket is now the top-most one. (The card underneath is {(cardsLeft > 0 ? "still" : "no "
            + "longer")} there, which is the tell: on the old chain Escape reached past the satchel and "
            + "shut the card the captain could not even see.) Add _showSatchel to TryDismissTopOverlay "
            + "(#1027).");

        Assert.True(cardsLeft > 0, $"Escape closed the card '{titleBefore}' instead of the satchel (#1027).");
        Assert.Equal(titleBefore, await _page.Locator(ArrivalCard + " .view-object-title").InnerTextAsync());
    }

    private readonly record struct Layers(int Card, int Satchel);

    private async Task<Layers> ReadTheLayers()
    {
        string json = await _page.EvaluateAsync<string>(
            """
            () => {
                const z = sel => {
                    const el = document.querySelector(sel);
                    return el ? parseInt(getComputedStyle(el).zIndex, 10) || 0 : -1;
                };
                return JSON.stringify({
                    card: z('.view-object-backdrop:not(.satchel-backdrop)'),
                    satchel: z('.satchel-backdrop'),
                });
            }
            """);
        using JsonDocument doc = JsonDocument.Parse(json);
        return new Layers(
            doc.RootElement.GetProperty("card").GetInt32(),
            doc.RootElement.GetProperty("satchel").GetInt32());
    }

    /// <summary>
    /// The player's own keystroke, typed at whatever the app itself focused — never a handler called by name.
    /// The path under test is the one the issue reports: a hand on the I key.
    /// </summary>
    private Task PressTheSatchelKey() => _page.Keyboard.PressAsync("i");

    /// <summary>
    /// Boot <c>?rip=1</c> — the #798 dev row that lands a captain on B1 beside the canteen's slop bin — and
    /// walk the boot's own cards down to the arrival card, exactly as the issue's repro does.
    ///
    /// <para>The first-ground family (the briefing, the map-just-grew card) is drawn on
    /// <c>.convergence-backdrop</c>, a different band well ABOVE both surfaces this test is about, so it is
    /// dismissed first and its dismissal is not the thing under test. Each is stepped with a DISPATCHED click
    /// on its own way out rather than a real one: getting into the state is not what is being measured.</para>
    /// </summary>
    private async Task BootIntoTheArrivalCard()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?scenario=sol&rip=1", new() { Timeout = BootTimeoutMs });

        // The boot is over when the deck is drawn, and the descent's own card is up shortly after — waited
        // for FIRST, because the ground family below is raised on the same boot and peeling it before the
        // card exists would leave this method dismissing an empty screen.
        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await _page.Locator(ArrivalCard).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        // Peel the told-once ground family off the top. Bounded: five of them exist and a loop that could
        // spin forever is worse than a failed assertion. The look-up and the press are ONE evaluation on
        // purpose — these cards arrive in a small cascade, and a locator resolved on one render and pressed
        // on the next is a flake in the fixture rather than a finding about the game.
        for (int i = 0; i < 10 && await _page.Locator(".convergence-backdrop").CountAsync() > 0; i++)
        {
            await _page.EvaluateAsync(
                """
                () => {
                    const b = document.querySelector('.convergence-backdrop .overlay-shell-dismiss');
                    if (b) { b.click(); }
                }
                """);
            await _page.WaitForTimeoutAsync(400);
        }

        Assert.Equal(0, await _page.Locator(".convergence-backdrop").CountAsync());
        await _page.Locator(ArrivalCard).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
    }
}
