using System.Text.Json;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1038 · <b>THE PEEK IS A TRAP WITH AN INVISIBLE LATCH — ON THE PIXELS.</b>
///
/// <para>Owner, at the Captain's desk with screenshots: pressing 👁 Peek cleared every panel <i>including the
/// 👁 button itself</i>, and nothing on the screen said how to get back. Verbatim: <i>"I thing esc-key should
/// end the peek. Also peek button should remain visible."</i> And then the question that turned out to be the
/// worse half: <i>"Are the other buttons also there even though they are invisible during the peek?"</i></para>
///
/// <h3>Why this gate and not a unit test</h3>
///
/// <para>#680's law — <b>in the DOM is not on the screen</b> — and its sharper sibling, which this issue is
/// about: <b>off the screen is not out of reach</b>. Both halves of the bug are invisible to every browser-free
/// assertion that could be made about them.</para>
/// <list type="bullet">
/// <item>The latch is an OPACITY GROUP. <c>.map-peek &gt; *:not(.desk-tab-bar)</c> exempted the bar by name and
/// #992 moved the bar two levels down inside <c>.map-flowcolumn</c>, so the column faded as one box and took
/// the exempt bar with it. Every element was still in the tree, correct, wired and enabled. Only a real layout
/// can tell you the button is gone.</item>
/// <item>The blind controls are a HIT-TESTING question. <c>pointer-events</c> inherits, so
/// <c>pointer-events: none !important</c> on the faded box lost to the <c>pointer-events: auto</c> that
/// <c>.map-hud</c>'s toolbar, readouts and frame chip each declare on themselves (by design — the canvas has to
/// stay grabbable through the gaps). Opacity 0 is not a hit-test barrier either. So the HUD's buttons went on
/// answering the mouse while nobody could see them, and the only instrument that can say so is
/// <c>document.elementFromPoint</c>.</item>
/// </list>
///
/// <h3>Two things this gate was nearly wrong about, written down so they stay fixed</h3>
///
/// <para><b>It does not ask Playwright whether the button is visible.</b> Playwright's visibility is a
/// bounding-box-and-<c>visibility</c> test, and it counts an element at <b>opacity 0 as visible</b> — so the
/// first cut of this guard called <c>IsVisibleAsync</c> on the 👁 button and PASSED on the broken build, with
/// its trial click passing too. That is the repository's fifth named bug class (a guard that cannot tell pass
/// from fail) caught in the act. What is measured instead is the EFFECTIVE opacity: the product of every
/// <c>opacity</c> from the button up to the document, which is the number the eye actually sees. On the broken
/// build it is 0.</para>
///
/// <para><b>It enters the peek with the ` HOTKEY, never by clicking the button.</b> The mode has to be
/// reachable on BOTH builds for the comparison to mean anything, and a fixture that got there by pressing the
/// very control under test would be resting on the thing it is trying to measure. The hotkey puts both builds
/// in the same place and lets the assertions do the talking.</para>
///
/// <para>Reverted against the old CSS, this gate reports all three failures at once: the way out at effective
/// opacity 0, the label with no key on it, and — the owner's answer — <c>&lt;button class="btn btn-warning
/// btn-sm map-key-action me-2"&gt; — "🗺 Plot"</c> sitting under the mouse at the middle of its own invisible
/// box.</para>
/// </summary>
public sealed class ThePeekLeavesAWayOutTests : IAsyncLifetime
{
    // Interpreted WASM under a plain local publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

    // A window big enough that the Nav HUD and the tab bar are both comfortably drawn. Nothing here is a
    // layout-height question (that is TallCardTests' and PlotPanelFitsTheWindowTests' job).
    private const int Width = 1280;
    private const int Height = 900;

    /// <summary>The way out: the 👁 chip on the desk tab bar. Matched on the eye rather than on a class,
    /// because the class list is precisely the thing under test.</summary>
    private const string TheWayOut = ".desk-tab-bar button:has-text(\"👁\")";

    /// <summary>A control the peek is SUPPOSED to take away, standing inside the block that re-enables its own
    /// pointer events — which is what made it pressable while invisible. The Plot button specifically: two
    /// other gates in this project click it for real, so a run where the mouse cannot reach it is this guard
    /// having gone wrong rather than the game.</summary>
    private const string AHudControl = ".map-hud button:has-text(\"Plot\")";

    private const string ThePage = ".map-page";

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
    /// THE OWNER'S TWO ASKS AND HIS QUESTION, IN ONE VOYAGE. Peek is entered with the hotkey; then the way out
    /// must be on the screen and pressable, the panels must be gone in the sense that MATTERS (nothing under
    /// the mouse), and Escape must end it.
    /// </summary>
    [Fact]
    public async Task The_peek_hides_the_panels_and_keeps_the_way_out()
    {
        await BootToTheFlightDeck();

        // The premise, checked out loud: the control we are about to lose is really there to begin with.
        // Without this, "it is not clickable while peeking" could be a sentence about a button that never
        // existed — the fifth named bug class in this repository (a guard that cannot tell pass from fail).
        Assert.True(
            await _page.Locator(AHudControl).First.IsVisibleAsync(),
            $"no `{AHudControl}` on the flight deck before the peek — this guard would be proving nothing "
            + "about a control that was never on the screen.");
        Box hudBefore = await BoxOf(AHudControl);

        // …and the mouse really reaches it BEFORE the peek. This is the anti-vacuum for assertion 3 below:
        // "nothing is under that point while peeking" would be perfectly true of a control that was never
        // hit-testable in the first place, and a guard that cannot tell those two apart proves nothing.
        Assert.True(
            (await WhatIsUnder(hudBefore)).Length > 0,
            $"`{AHudControl}` is drawn but the mouse does not reach it even before the peek — the top paint "
            + $"at the middle of it is {await TopPaintAt(hudBefore)}. Find a control the player can actually "
            + "press, or the 'no blind clicks while peeking' half of this gate is a sentence about nothing.");

        await EnterThePeekWithTheHotkey();

        // The three facts about the peeking screen are GATHERED and then judged together, so one run of this
        // gate reports the whole state of the mode instead of stopping at the first thing wrong with it.
        List<string> wrong = [];

        // 1 · THE WAY OUT IS ON THE SCREEN. Owner: "Also peek button should remain visible."
        //
        //     Measured as EFFECTIVE opacity down the ancestor chain, and deliberately not with Playwright's
        //     own visibility — that is a bounding-box-and-`visibility` test which counts an element at
        //     opacity 0 as visible, and this bug IS an opacity 0. The first cut of this guard used
        //     IsVisibleAsync and passed on the broken build; that is the fifth named bug class in this
        //     repository (a guard that cannot tell pass from fail) and it is written down here so nobody
        //     puts it back.
        Paint wayOut = await PaintOf(TheWayOut);
        if (wayOut.Hidden || wayOut.Opacity < 0.5)
        {
            wrong.Add(
                $"THE WAY OUT IS NOT ON THE SCREEN. The 👁 button paints at effective opacity "
                + $"{wayOut.Opacity:0.###}{(wayOut.Hidden ? " and is `visibility: hidden`" : "")}. It is the "
                + "ONLY labelled way out of the mode — the ` hotkey and Escape are unlabelled — so a peek "
                + "without it is a state the player cannot see his way out of, which the pop-up law (#992, "
                + "owner 2026-08-24) forbids. It fades because the exemption in app.css is written as a "
                + "DEPTH (`.map-peek > *:not(.desk-tab-bar)`) and #992 put .map-flowcolumn between the page "
                + "and the bar; opacity is a GROUP, so nothing inside that column can paint itself back.");
        }

        // 2 · AND IT SAYS WHAT PRESSING IT WILL DO, including the key that also works (#195's law, and the
        //     shell wave's "the way out is visible AND worded").
        string label = await _page.Locator(TheWayOut).InnerTextAsync();
        if (!label.Contains("Show panels", StringComparison.Ordinal)
            || !label.Contains("Esc", StringComparison.Ordinal))
        {
            wrong.Add(
                $"THE WAY OUT DOES NOT SAY WHAT IT IS. It reads \"{label.Trim()}\"; while peeking it has to "
                + "name the thing it will do AND the key that also does it, because the panels it would "
                + "bring back are the only other place that could have said so.");
        }

        // 3 · THE OWNER'S QUESTION, ANSWERED BY THE MOUSE. What is under the middle of a HUD button that is
        //     no longer drawn? On the build he reported: that same button. It was invisible and it took
        //     clicks — the #603 class inverted, a control that quietly does something.
        string blind = await WhatIsUnder(hudBefore);
        if (blind.Length > 0)
        {
            wrong.Add(
                "A CONTROL THE PEEK HAS HIDDEN IS STILL ANSWERING THE MOUSE: " + blind + ". "
                + "`pointer-events: none !important` on the faded box does NOT reach it — pointer-events "
                + "INHERITS, and .map-hud .btn-toolbar / .map-readouts / .map-frame / ::deep .map-plot each "
                + "declare `pointer-events: auto` on themselves so the canvas stays grabbable between them; "
                + "a descendant's own declaration beats an inherited one however important the ancestor's "
                + "was. Opacity 0 is not a hit-test barrier either. A button nobody can see and everybody "
                + "can press is worse than a missing one: the captain aims at the sky and pauses the clock. "
                + "Fade with `visibility: hidden`, which nothing in this client declares its way back out "
                + "of and which takes the subtree out of the tab order too.");
        }

        Assert.True(wrong.Count == 0, "#1038 — the peeking screen:\n\n • " + string.Join("\n\n • ", wrong));

        // …and the way out is genuinely PRESSABLE, not merely painted. Playwright's full actionability
        // battery (visible · stable · enabled · receives events), with the side effect withheld — the #293
        // question. It comes AFTER the judgement above because it throws rather than reports.
        await _page.Locator(TheWayOut).ClickAsync(new() { Trial = true, Timeout = ActionTimeoutMs });

        // 4 · AND THE MODE ENDS ON THE CANCEL KEY. Owner: "I thing esc-key should end the peek."
        await _page.Keyboard.PressAsync("Escape");
        await _page.Locator(".map-page.map-peek").WaitForAsync(
            new() { State = WaitForSelectorState.Detached, Timeout = ActionTimeoutMs });

        await _page.Locator(AHudControl).First.WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
        Assert.Contains("Peek", await _page.Locator(TheWayOut).InnerTextAsync(), StringComparison.Ordinal);

        // …and the HUD is a working control again, not merely a painted one. Measured fresh rather than at
        // the remembered point: the sim ran while the peek was up, so the toolbar's own width can have moved
        // under a changed warp label, and this half is about clickability, not about pixels holding still.
        Box hudAfter = await BoxOf(AHudControl);
        Assert.True(
            (await WhatIsUnder(hudAfter)).Length > 0,
            "the panels came back but the toolbar answers nothing under the mouse — a peek that ended by "
            + "leaving the HUD un-clickable would be a worse trap than the one this issue is about. The top "
            + $"paint at the middle of the control is {await TopPaintAt(hudAfter)}.");
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────────────────────────────

    private readonly record struct Box(double X, double Y);

    /// <summary>How a control actually paints: the product of every <c>opacity</c> on the way up to the
    /// document, and whether anything on that chain is <c>visibility: hidden</c> or <c>display: none</c>.
    /// Playwright's own visibility cannot be used for this — it counts an element at opacity 0 as visible,
    /// and opacity 0 is precisely what this issue is about.</summary>
    private readonly record struct Paint(double Opacity, bool Hidden);

    private async Task<Paint> PaintOf(string selector)
    {
        string json = await _page.Locator(selector).EvaluateAsync<string>(
            """
            el => {
                let opacity = 1;
                let hidden = false;
                for (let at = el; at && at !== document.documentElement; at = at.parentElement) {
                    const style = getComputedStyle(at);
                    const own = parseFloat(style.opacity);
                    opacity *= Number.isNaN(own) ? 1 : own;
                    if (style.visibility === 'hidden' || style.display === 'none') { hidden = true; }
                }
                return JSON.stringify({ opacity, hidden });
            }
            """);
        using JsonDocument doc = JsonDocument.Parse(json);
        return new Paint(
            doc.RootElement.GetProperty("opacity").GetDouble(),
            doc.RootElement.GetProperty("hidden").GetBoolean());
    }

    /// <summary>The middle of a control, in page coordinates, remembered so the same spot can be interrogated
    /// after the control stops being drawn.</summary>
    private async Task<Box> BoxOf(string selector)
    {
        LocatorBoundingBoxResult box = await _page.Locator(selector).First.BoundingBoxAsync()
            ?? throw new Xunit.Sdk.XunitException($"`{selector}` has no bounding box — it is not laid out.");
        return new Box(box.X + (box.Width / 2), box.Y + (box.Height / 2));
    }

    /// <summary>What the mouse would hit at a point, IF it would hit a control at all — an empty string when
    /// the top paint there is the canvas or nothing, and a description otherwise.</summary>
    private async Task<string> WhatIsUnder(Box at) =>
        await _page.EvaluateAsync<string>(
            """
            ([x, y]) => {
                const hit = document.elementFromPoint(x, y);
                if (!hit) { return ''; }
                const control = hit.closest('button, a, input, select, textarea, [role="button"]');
                if (!control) { return ''; }
                const text = (control.innerText || control.getAttribute('title') || '').trim().slice(0, 60);
                return `<${control.tagName.toLowerCase()} class="${control.getAttribute('class') ?? ''}">`
                       + (text ? ` — "${text}"` : '');
            }
            """,
            new[] { at.X, at.Y });

    /// <summary>The top paint at a point whatever it is, control or not — so a failure can say what WAS there
    /// instead of only what was not.</summary>
    private async Task<string> TopPaintAt(Box at) =>
        await _page.EvaluateAsync<string>(
            """
            ([x, y]) => {
                const hit = document.elementFromPoint(x, y);
                return hit
                    ? `<${hit.tagName.toLowerCase()} class="${hit.getAttribute('class') ?? ''}">`
                    : 'nothing at all';
            }
            """,
            new[] { at.X, at.Y });

    /// <summary>
    /// Into the mode by the player's own hotkey, at the page's own keyboard host. Never by clicking the 👁
    /// button: on the broken build that control is invisible and the click would fail the fixture instead of
    /// the assertion, which would leave the bad state unreachable and this guard unable to tell pass from fail.
    /// </summary>
    private async Task EnterThePeekWithTheHotkey()
    {
        await _page.Locator(ThePage).FocusAsync();
        await _page.Keyboard.PressAsync("Backquote");

        await _page.Locator(".map-page.map-peek").WaitForAsync(
            new() { State = WaitForSelectorState.Attached, Timeout = ActionTimeoutMs });

        // The fade is a 0.12s transition and the hit-testing switches with it, so give the paint a beat
        // before asking the layout anything. Generous, and still nothing next to the boot.
        await _page.WaitForTimeoutAsync(600);
    }

    /// <summary>Boot docked at The Red Eye — the same berth the Plotting-panel gates use, so the flight deck
    /// this drives and the one they drive are the same deck.</summary>
    private async Task BootToTheFlightDeck()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?dock=red-eye", new() { Timeout = BootTimeoutMs });
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        // The docked boot lands the captain on the ship's deck plan, where the flight HUD is not drawn at
        // all, so the Nav tab has to be pressed to reach it — the same step the Plotting-panel gates take.
        //
        // A REAL click, with this project's own action timeout rather than Playwright's 30 s default. Two
        // things were learned here the hard way and are worth the sentence: a dispatched `el.click()` does
        // NOT get the captain off the deck plan (the HUD never arrives), so this step has to be the real
        // press; and Playwright's post-click "waiting for scheduled navigations to finish" outruns 30 s on an
        // interpreted-WASM desk switch when the rest of this project's classes are booting beside it, which
        // is the same 30 s flake PlotPanelFitsTheWindow and TheArrivalNeverInventsARefusal show on a loaded
        // box. Sixty seconds, and it is the fixture's own timeout, not an assertion's.
        await _page.Locator("button.desk-tab", new() { HasTextString = "Nav" })
            .ClickAsync(new() { Timeout = ActionTimeoutMs });
        await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = "Nav" }).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await _page.Locator(".map-hud").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }
}
