using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #951 · <b>THE DOOR OFF A DEATH, PRESSED.</b>
///
/// <para>Owner, 2026-08-21, screenshot of the Impact freeze-frame after flying Selene Gate's periapsis under
/// its own surface: <i>"I undocked from station and died. I want an option to load previous game here like in
/// the beginning."</i> #970 answered it — <b>📖 Load a saved voyage</b> stands beside <i>…wake up</i> on all
/// four death panels, and #997 wave 7 moved that row onto <c>OverlayShell</c>'s <c>OnBeside</c>.</para>
///
/// <h3>Why this gate and not the guards that already exist</h3>
/// <para><c>TheDeathCardOffersTheShelfTests</c> and <c>TheShellOwnsTheDeathRowsAndTheShuttleHatchTests</c>
/// read the SHIPPING razor and the SHIPPING methods, and they are good at what they do: they prove the four
/// panels name the handler, that the handler exists and really opens the drawer, and that the card yields
/// while the drawer is up. What no source-reading guard can answer is #680's law — <i>in the DOM is not on
/// the screen</i>. The door is drawn by a component (<c>OnBeside</c> renders only when something is wired to
/// it), it lands in a row shared with the canon dismiss, and it opens a surface that sits BELOW the death
/// card's own backdrop in the z bands. Every one of those is a way for this to be perfect in the source and
/// dead under a player's finger — the #603 class, a control that quietly does nothing.</para>
///
/// <para>So this gate boots the owner's own death through the real pipeline
/// (<c>?scenario=sol&amp;death=impact</c>, the cheat #621 stages), finds the button, and <b>presses it for
/// real</b> — no dispatched event, no handler called directly — then insists the logbook he asked for is the
/// thing on the screen afterwards, and that the death he came from is still waiting when he shuts it.</para>
/// </summary>
public sealed class TheDeathCardsShelfDoorTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    // A desktop window close to the owner's screenshot. Height matters here: the death panels are tall and
    // the shelf door sits in the foot row, so a viewport that hid it would be exactly the bug.
    private const int Width = 1280;
    private const int Height = 900;

    private const string DeathCard = ".busted-card";
    private const string Shelf = ".busted-logbook";
    private const string Logbook = ".start-picker.save-surface";

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
    /// THE ASK, AS PIXELS. Die the owner's death; the card carries the shelf door, on the screen, pressable —
    /// and pressing it puts the same logbook the boot door opens over the card.
    /// </summary>
    [Fact]
    public async Task The_impact_death_card_offers_a_shelf_door_that_really_opens_the_logbook()
    {
        await BootIntoTheImpactDeath();

        // The premise, said out loud: the canon beat is still the card's other way out. If "…wake up" ever
        // vanishes this gate is testing a card the owner never saw and should say so rather than pass.
        await _page.Locator($"{DeathCard} .busted-close").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        ILocator shelf = _page.Locator($"{DeathCard} {Shelf}");
        Assert.Equal(1, await shelf.CountAsync());

        if (await shelf.BoundingBoxAsync() is not { } box)
        {
            throw new InvalidOperationException(
                "#951 — the shelf door is in the DOM with no box at all: it is not laid out, so there is "
                + "nothing to press.");
        }

        Assert.True(
            box.Y >= 0 && box.Y + box.Height <= Height,
            $"#951 — the shelf door's hit-box runs {box.Y:0}…{box.Y + box.Height:0} px down a {Height} px "
            + "viewport. A door off a death that is not on the screen is not a door.");

        // The press is REAL — Playwright's actionability battery (visible · stable · enabled · not covered by
        // another element) has to agree first, which is the assertion that a source-reading guard cannot make.
        await shelf.ClickAsync(new() { Timeout = ActionTimeoutMs });

        // …and what stands afterwards is the logbook: the same surface the front door opens, with a shelf of
        // berths on it. Not a second loader built for this card — there is one save surface in this game.
        await _page.Locator(Logbook).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        Assert.True(
            await _page.Locator($"{Logbook} .slot-row").CountAsync() > 0,
            "#951 — the logbook opened onto nothing: the shelf has no rows, so the player who came here to "
            + "board a moment he banked has still not been shown one.");

        // The card yields rather than painting over the drawer it just opened (the busted backdrop outranks
        // the picker's gate in the z bands, so an ungated card would bury this).
        Assert.Equal(0, await _page.Locator($"{DeathCard}:visible").CountAsync());
    }

    /// <summary>
    /// NOBODY IS TRAPPED, AND THE BEAT IS NOT SKIPPED BY LOOKING. Closing the logbook brings the death card
    /// back exactly as it was, so a player who opens the shelf, finds nothing worth boarding and shuts it can
    /// still take the wake — the general UI law (no pop-up that cannot be closed) held at both layers.
    /// </summary>
    [Fact]
    public async Task Shutting_the_logbook_hands_the_death_card_back()
    {
        await BootIntoTheImpactDeath();
        await _page.Locator($"{DeathCard} {Shelf}").ClickAsync(new() { Timeout = ActionTimeoutMs });
        await _page.Locator(Logbook).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });

        // The logbook's own way out, the third button of its foot — the one #997 wave 8 handed the shell.
        await _page.Locator($"{Logbook} .save-surface-foot button", new() { HasTextString = "Close" })
                   .First.ClickAsync(new() { Timeout = ActionTimeoutMs });

        await _page.Locator(DeathCard).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        await _page.Locator($"{DeathCard} .busted-close").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        Assert.Equal(1, await _page.Locator($"{DeathCard} {Shelf}").CountAsync());
    }

    /// <summary>The owner's own death, staged through the real pipeline rather than by poking state.</summary>
    private async Task BootIntoTheImpactDeath()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?scenario=sol&death=impact",
            new() { Timeout = BootTimeoutMs });
        await _page.Locator(DeathCard).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }
}
