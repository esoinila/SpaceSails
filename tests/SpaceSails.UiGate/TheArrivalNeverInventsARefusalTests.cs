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
/// <para><b>RED PROOF.</b> Pointed at a publish of b71e1c5 via <c>SPACESAILS_PUBLISH_DIR</c>, the bench
/// itself cannot even be built: no path length leaves the row saying it has not judged anything, because on
/// that build a verdict is never withheld, and both facts fail at the bench's own <c>Assert.Fail</c>.</para>
///
/// <para><b>And a note on how this file first lied.</b> Its first draft pinned ONE path length and asserted
/// on whatever body the button then offered. It passed locally and failed on CI, which offered
/// <c>🛰 orbit Neptune · pass 30.64 AU</c> — a body whose closest approach on a five-day line sits at the
/// line's START, because the ship is simply receding from it. That is an honest ✗ and not the state under
/// test, and the difference between the two runs was the sky, not the code. (The local pass was worth
/// nothing on its own for a second reason: <c>ClientHost</c> shares one publish folder across worktrees, so
/// a bare local run can serve another lane's build. CI sets <c>SPACESAILS_PUBLISH_DIR</c> and is the honest
/// gate.) The bench now walks the control the row names until the premise is genuinely met, and says so out
/// loud when it cannot be.</para>
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
    /// AND PRESSING THE CONTROL THE ROW NAMES ACTUALLY LENGTHENS THE LINE. The sentence sends the captain to
    /// <c>auto</c>, so <c>auto</c> has to be an answer: it must reach for the plan's own ending rather than
    /// snapping back to last-burn + 90 d, which is what it did before this change.
    ///
    /// <para>The second half is deliberately stated as an OR, because which body the button offered is the
    /// sky's business (see the bench): either the longer line found the encounter and the row now carries a
    /// real ✓/✗ verdict, or the course genuinely never reaches that body and the line has run all the way to
    /// the projection cap — in which case "not judged" is still the honest reading and the row is entitled to
    /// keep saying it. What is NOT allowed, and is the whole point, is a short line and a confident number.</para>
    /// </summary>
    [Fact]
    public async Task Pressing_the_control_the_row_names_lengthens_the_line_toward_the_plans_own_ending()
    {
        await AnArrivalOnAStubOfALine();
        string lengthBefore = await PathLengthText();

        await _page.Locator(".map-plot button", new() { HasTextString = "auto" })
                   .DispatchEventAsync("click", null, new() { Timeout = ActionTimeoutMs });

        // Keyed on the panel's own words, never a sleep.
        await _page.WaitForFunctionAsync(
            "(was) => { const p = document.querySelector('.map-plot'); if (p === null) { return false; } "
            + "const m = /Path length: ([^\\n]*)/.exec(p.textContent); "
            + "return m !== null && m[1].trim() !== was; }",
            lengthBefore,
            new() { Timeout = ActionTimeoutMs });

        string lengthAfter = await PathLengthText();
        Assert.NotEqual(lengthBefore, lengthAfter);
        Assert.True(
            DaysIn(lengthAfter) > DaysIn(lengthBefore),
            $"auto must reach FOR the plan's ending, not away from it: {lengthBefore} → {lengthAfter}");

        ILocator row = _page.Locator(".map-plan-step").Last;
        string badge = await row.Locator(".badge").InnerTextAsync();
        string rowText = await row.InnerTextAsync();

        bool judged = badge.Contains("VALID", StringComparison.Ordinal);
        bool ranToTheCap = DaysIn(lengthAfter) >= 700;
        Assert.True(
            judged || ranToTheCap,
            $"after auto the line reads {lengthAfter} and the badge \"{badge}\" — a course this short with no "
            + $"verdict is neither of the two honest endings. Row: \"{rowText}\"");
    }

    /// <summary>
    /// #1042 · <b>THE SCRUB NEVER POINTS PAST THE END OF THE LINE IT SCRUBS.</b> The scrub slider's
    /// <c>max</c> IS the path length, but its bound value was only ever written by a hand on the control — so
    /// dragging Path length DOWN left the two disagreeing, with the scrub standing hundreds of days outside
    /// the drawn world and every "at scrub" readout and button quietly agreeing on an hour that is not on the
    /// picture.
    ///
    /// <para><b>Why the ELEMENT cannot be asked, and the PANEL must be.</b> The first draft of this fact read
    /// the scrub input's <c>value</c> against its own <c>max</c> — and passed on the broken build, because a
    /// range input re-sanitises its value the moment its <c>max</c> attribute shrinks. The browser quietly
    /// walks the thumb back to the new far end WITHOUT firing an <c>input</c> event, so the DOM looks tidy
    /// while the page's own scrub clock still stands at the old hour: the thumb and the clock, two readings
    /// of one control, disagreeing. So the fact is stated against the panel's own printed numbers — its
    /// <c>Scrub:</c> clock against its <c>Path length:</c> — which are what the captain actually reads and
    /// what every "at scrub" button actually acts on. (A green test that asserts nothing is this repo's own
    /// named bug class; the first draft was one, and this note is why it is not shipped.)</para>
    ///
    /// <para>Sky-independent on purpose — nothing here depends on where the planets are, which is the trap
    /// this file already records (see the class note).</para>
    /// </summary>
    [Fact]
    public async Task The_scrub_never_points_past_the_end_of_the_line_it_scrubs()
    {
        await AtThePlottingDesk();

        // The panel's clock at the scrub's origin — this is "now" in the only units the panel speaks.
        await SetRange(0, 0.0);
        string nowText = await ScrubClockText();
        double now = DaysInClock(nowText);

        // A long line, and the scrub dragged to the far end of it — the captain looking at the whole sail.
        await SetPathLength(0.9);
        await SetRange(0, 1.0);
        await _page.WaitForFunctionAsync(
            "(was) => { const p = document.querySelector('.map-plot'); if (p === null) { return false; } "
            + "const m = /Scrub: ([^\\n]*)/.exec(p.textContent); return m !== null && m[1].trim() !== was; }",
            nowText,
            new() { Timeout = ActionTimeoutMs });

        double longLine = DaysIn(await PathLengthText());
        double outAtTheEnd = DaysInClock(await ScrubClockText()) - now;
        Assert.True(
            outAtTheEnd > longLine / 2,
            $"the bench must start with the scrub genuinely out at the far end: {outAtTheEnd:F1} d out on a "
            + $"{longLine} d line.");

        // Now he drags Path length back down to look at the near term. Nothing touches the scrub.
        await SetPathLength(0.0);
        double shortLine = DaysIn(await PathLengthText());
        Assert.True(shortLine < longLine, $"the line must genuinely have shrunk: {longLine} d → {shortLine} d");

        // The reprojection that carries the clamp is on a 250 ms throttle, so wait on the panel's own words
        // for it rather than sleeping — and if it never comes, fall through and let the assertion speak.
        try
        {
            await _page.WaitForFunctionAsync(
                "(was) => { const p = document.querySelector('.map-plot'); if (p === null) { return false; } "
                + "const m = /Scrub: ([^\\n]*)/.exec(p.textContent); return m !== null && m[1].trim() !== was; }",
                await ScrubClockText(),
                new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            // deliberate: the assertion below is the one that should speak, with the numbers in it
        }

        string scrubText = await ScrubClockText();
        double outNow = DaysInClock(scrubText) - now;
        Assert.True(
            outNow <= shortLine + 1,
            $"the panel says Path length: {shortLine} d and Scrub: {scrubText} — {outNow:F1} d out from "
            + $"{nowText}. The scrub clock is standing past the end of the world it scrubs, while the "
            + "slider's own thumb sits at the new far end: one control, two answers.");
    }

    /// <summary>The panel's <c>Scrub:</c> clock, verbatim.</summary>
    private async Task<string> ScrubClockText()
    {
        string panel = await _page.Locator(".map-plot").InnerTextAsync();
        System.Text.RegularExpressions.Match m =
            System.Text.RegularExpressions.Regex.Match(panel, @"Scrub: ([^\n]*)");
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    /// <summary>That clock ("291d 04h 12m" — <c>FormatSimTime</c>'s own shape) as days.</summary>
    private static double DaysInClock(string clock)
    {
        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
            clock, @"(\d+)d\s+(\d+)h\s+(\d+)m");
        if (!m.Success)
        {
            throw new InvalidOperationException($"the scrub clock read \"{clock}\", which is not FormatSimTime's shape");
        }

        double Part(int group) =>
            double.Parse(m.Groups[group].Value, System.Globalization.CultureInfo.InvariantCulture);
        return Part(1) + (Part(2) / 24) + (Part(3) / 1440);
    }

    /// <summary>The panel's Path-length readout back as days ("2.0 yr" · "381 d" · "5 d"), so two lengths can
    /// be compared. Its own ladder (<c>FormatHorizon</c>) is the only thing being parsed.</summary>
    private static double DaysIn(string horizonText)
    {
        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
            horizonText, @"([0-9]+(?:\.[0-9]+)?)\s*(yr|d)");
        if (!m.Success)
        {
            throw new InvalidOperationException($"Path length read \"{horizonText}\", which is not on the panel's ladder");
        }

        double value = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        return m.Groups[2].Value == "yr" ? value * 365 : value;
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
        await AtThePlottingDesk();

        // A burn, so the plan is a plan and not an arrival standing on its own.
        await Compose("Add burn");
        await ExpectRows(1);

        // NOW SHORTEN THE LINE UNTIL IT DOES NOT REACH. Which body the button offers is decided by the
        // world — it takes the arrivable pass nearest the scrub — so pinning ONE path length and hoping the
        // sky cooperates is how this gate first went green locally and red on CI, offering Neptune (whose
        // closest approach on a five-day line is at the line's START: the ship is simply receding, which is
        // an honest ✗ and not the state under test). So the bench walks the very control the row names, from
        // the shortest line upward, scrubbing to the far END each time — where an end-edge pass wins the
        // nearest-in-time pick outright — and stops at the first length that genuinely cuts the course short
        // of whatever it is offering. Re-pressing the button REPLACES the terminal step (a plan ends once),
        // so no cleanup is needed between turns.
        foreach (double pathLength in TheLengthsToTry)
        {
            await SetPathLength(pathLength);
            await SetRange(0, 1.0);   // the scrub to the far END — read AFTER the length settled, since the
                                      // scrub slider's own max IS the path length
            await Compose("Add orbit at scrub");
            await ExpectRows(2);

            string row = await _page.Locator(".map-plan-step").Last.InnerTextAsync();
            if (row.Contains("not judged", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        Assert.Fail(
            "no path length between the slider's shortest and its midpoint left the plotted course short of "
            + "the body the button offers, so this gate never reached the state it is about. The sky at "
            + "?start=wreck has moved out from under the bench — pick a start whose course has a reachable "
            + "encounter beyond a short line and say so here (#952).");
    }

    /// <summary>Free-flying at <c>?start=wreck</c> with the Plotting panel open — the state both facts above
    /// start from, and nothing more.</summary>
    private async Task AtThePlottingDesk()
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
    }

    /// <summary>Fractions of the Path-length slider's travel, shortest first. Log-scaled, so these are ~5 d,
    /// ~11 d, ~24 d, ~54 d, ~121 d and ~270 d — a spread wide enough that some body the course is closing on
    /// has its pass at the far end, and short enough that it is genuinely cut off.</summary>
    private static readonly double[] TheLengthsToTry = [0.0, 0.1, 0.2, 0.3, 0.4, 0.5];

    /// <summary>Drag Path length to a fraction of its travel and WAIT until the panel says the new length —
    /// the panel's own words are the signal (never a sleep). This matters twice over: the reprojection is on
    /// a 250 ms throttle, and the scrub slider's <c>max</c> IS the path length, so reading the scrub before
    /// this settles moves it to the wrong hour.</summary>
    private async Task SetPathLength(double fraction)
    {
        string before = await PathLengthText();
        await SetRange(1, fraction);
        await _page.WaitForFunctionAsync(
            "(was) => { const p = document.querySelector('.map-plot'); if (p === null) { return false; } "
            + "const m = /Path length: ([^\\n]*)/.exec(p.textContent); "
            + "return m !== null && m[1].trim() !== was; }",
            before,
            new() { Timeout = ActionTimeoutMs });
    }

    private async Task<string> PathLengthText()
    {
        string panel = await _page.Locator(".map-plot").InnerTextAsync();
        System.Text.RegularExpressions.Match m =
            System.Text.RegularExpressions.Regex.Match(panel, @"Path length: ([^\n]*)");
        return m.Success ? m.Groups[1].Value.Trim() : "";
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
