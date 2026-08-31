using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #952 · <b>THE ARRIVE ROW NEVER INVENTS A REFUSAL — READ OFF THE PIXELS.</b>
///
/// <para>The Core law and the client's own reflection bench both prove the arithmetic; this gate proves the
/// only thing neither of them can, which is #680's and #735's standing law here: <i>in the DOM is not on the
/// screen, and compiled is not rendered</i>. The change touches a Razor region whose sibling shipped a
/// green-building, page-killing bug three weeks ago (#985 — a comment between an element's attributes became
/// an attribute NAME and <c>setAttribute</c> took the Trade desk down). So the two words that replaced the
/// fabricated refusal are asked for out loud, in a real browser, on the real published artefact.</para>
///
/// <para><b>The sighting behind it.</b> <c>ClosestApproach.Passes</c> answers for every body in the system
/// whether or not the plotted course goes near it, so for a body the ribbon stops short of it returns the
/// ribbon's LAST SAMPLE. The arrive row judged that artefact and printed a confident ✗ with numbers — over,
/// in the bench case, a course that genuinely arrives 383,764 km from Mars at 2.3 km/s. Here the row must
/// instead say it has not judged anything, and name the control that ends the wait.</para>
///
/// <para><b>RED PROOF (watched, 2026-08-31).</b> Pointed at a Release publish of b71e1c5 via
/// <c>SPACESAILS_PUBLISH_DIR</c>, the first fact below fails at its own premise —
/// <c>Assert.Contains() Failure: Not found: "not judged"</c> — because on that build the row simply carries
/// a fabricated verdict instead. The SECOND fact passes there too, and is meant to: it is the anti-vacuity
/// half (a verdict comes back when the control the row names is pressed), and on the old build a verdict was
/// never withheld in the first place. It earns its keep on THIS build, where the first fact proves the same
/// bench starts out unjudged.</para>
/// </summary>
public sealed class TheArrivalNeverInventsARefusalTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish is CPU-heavy on the boot; signal-keyed waits, never sleeps.
    private const float BootTimeoutMs = 180_000;
    private const float ActionTimeoutMs = 60_000;

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
    /// A COURSE TOO SHORT TO REACH THE BODY SAYS SO, IN THE ROW, AND NAMES THE CONTROL. Path length is dragged
    /// to its shortest, a burn is dropped on the plan and the arrival is added at the far end of that stub of
    /// a line — the state in which the old code manufactured a refusal.
    /// </summary>
    [Fact]
    public async Task A_course_too_short_to_reach_the_body_is_not_judged_and_says_which_control_fixes_it()
    {
        await AnArrivalOnAStubOfALine();

        ILocator row = _page.Locator(".map-plan-step").Last;
        string rowText = await row.InnerTextAsync();

        // The premise, out loud (#735's honesty clause): this really is the unjudgeable state, or the gate
        // is standing somewhere else and proves nothing.
        Assert.Contains("not judged", rowText, StringComparison.OrdinalIgnoreCase);

        // THE TWO WORDS. On b71e1c5 this badge reads "✗ INVALID".
        string badge = await row.Locator(".badge").InnerTextAsync();
        Assert.Contains("NOT JUDGED", badge, StringComparison.Ordinal);
        Assert.DoesNotContain("INVALID", badge, StringComparison.Ordinal);
        Assert.DoesNotContain("✗", badge, StringComparison.Ordinal);

        // …and the sentence in its place names the control the captain already has his hand on, so the fix
        // is one press away rather than a puzzle.
        string why = await row.Locator(".map-arrive-why").InnerTextAsync();
        Assert.Contains("Path length", why, StringComparison.Ordinal);
        Assert.Contains("auto", why, StringComparison.Ordinal);

        // No fabricated numbers anywhere on the row: no threshold, no shortfall, no ✗.
        Assert.DoesNotContain("too far", rowText, StringComparison.Ordinal);
        Assert.DoesNotContain("too fast", rowText, StringComparison.Ordinal);
        Assert.DoesNotContain("need ≤", rowText, StringComparison.Ordinal);

        // The row is not painted as a broken plan either — "invalid" is a claim, and none was made.
        Assert.False(
            await row.EvaluateAsync<bool>("el => el.classList.contains('map-plan-step-invalid')"),
            "an arrival nobody has judged must not wear the ruined-plan pip.");

        // …and above all the sleeping-captain alarm did NOT fire: no "NO LONGER ENDS SAFELY" on the screen.
        string page = await _page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("NO LONGER ENDS SAFELY", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// AND THE WAIT ENDS WHERE THE ROW SAYS IT DOES. Press <c>auto</c> — the control the sentence names — and
    /// the arrival stops being unjudged: the row carries a real verdict with the real gates in it. This is the
    /// half that keeps the first fact from being a way to make every arrival silent.
    /// </summary>
    [Fact]
    public async Task Pressing_the_control_the_row_names_gets_a_real_verdict_back()
    {
        await AnArrivalOnAStubOfALine();

        await _page.Locator(".map-plot button", new() { HasTextString = "auto" })
                   .DispatchEventAsync("click", null, new() { Timeout = ActionTimeoutMs });

        // Keyed on the words themselves, never a sleep: the row stops saying it cannot judge.
        await _page.WaitForFunctionAsync(
            "() => { const rows = document.querySelectorAll('.map-plan-step'); "
            + "if (rows.length === 0) { return false; } "
            + "return !/not judged/i.test(rows[rows.length - 1].textContent); }",
            null,
            new() { Timeout = ActionTimeoutMs });

        ILocator row = _page.Locator(".map-plan-step").Last;
        string badge = await row.Locator(".badge").InnerTextAsync();
        Assert.DoesNotContain("NOT JUDGED", badge, StringComparison.Ordinal);

        // A verdict is a verdict either way round — what matters is that it is now a claim about the COURSE,
        // with the real thresholds in it, rather than a confession about the picture.
        string rowText = await row.InnerTextAsync();
        Assert.True(
            badge.Contains("VALID", StringComparison.Ordinal),
            $"the row should now carry a ✓/✗ verdict; its badge reads \"{badge}\" and the row \"{rowText}\"");
    }

    // ── The bench: an arrival added at the end of a deliberately short line ────────────────────────────

    /// <summary>
    /// Free-flying on the shipping sol scenario, Path length dragged to its SHORTEST, one burn on the plan
    /// and the arrival added at the far end of that stub. Everything is pressed the way a captain presses it,
    /// through the panel's own controls, so what the gate reads is what he would read.
    ///
    /// <para><c>?start=wreck</c> is the dev free-flying jump (co-moving beside the derelict roadster, sunward
    /// of Mars): it skips the boot picker — whose backdrop otherwise swallows every click — and leaves the
    /// ship OFF a berth, which is where the plan this issue is about is drawn.</para>
    /// </summary>
    private async Task AnArrivalOnAStubOfALine()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?scenario=sol&start=wreck", new() { Timeout = BootTimeoutMs });
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

        // Path length to its shortest — the SECOND range control in the panel (the first is the scrub).
        await SetRange(1, 0.0);
        await _page.WaitForFunctionAsync(
            "() => { const p = document.querySelector('.map-plot'); "
            + "return p !== null && /Path length: 5 d/.test(p.textContent); }",
            null,
            new() { Timeout = ActionTimeoutMs });

        // A burn, so the plan is a plan and not an arrival standing on its own.
        await Compose("Add burn");
        await ExpectRows(1);

        // The scrub run out to the far end of that stub — where a captain looking for a distant encounter
        // puts it — and then the cherry on top.
        await SetRange(0, 1.0);
        await Compose("Add orbit at scrub");
        await ExpectRows(2);
    }

    /// <summary>Move one of the panel's range controls to a fraction of its travel, the way a hand does.
    /// The native value setter plus an <c>input</c> event is what Blazor's <c>@@bind:event="oninput"</c> and
    /// <c>@@oninput</c> both hear.</summary>
    private async Task SetRange(int index, double fraction) =>
        await _page.Locator(".map-plot input.form-range").Nth(index).EvaluateAsync(
            """
            (el, f) => {
                const set = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
                const lo = Number(el.min), hi = Number(el.max);
                set.call(el, String(Math.round(lo + ((hi - lo) * f))));
                el.dispatchEvent(new Event('input', { bubbles: true }));
            }
            """,
            fraction);

    /// <summary>Press a compose button at the head of the Plotting panel by its own words — dispatched for
    /// the same reason the #992 gate dispatches: getting into the state is not what is under test.</summary>
    private async Task Compose(string label) =>
        await _page.Locator(".map-plot-compose button", new() { HasTextString = label })
                   .DispatchEventAsync("click", null, new() { Timeout = ActionTimeoutMs });

    private async Task ExpectRows(int rows) =>
        await _page.Locator(".map-plan-step").Nth(rows - 1).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
}
