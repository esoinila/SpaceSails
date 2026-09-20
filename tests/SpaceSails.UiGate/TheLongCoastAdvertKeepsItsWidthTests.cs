using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1247 · <b>THE CLOCK DOES NOT GET TO MOVE THE TOOLBAR.</b>
///
/// <para><b>The sighting</b> (#1245's ui-gate, 2026-09-19). #1239's own guard,
/// <c>The_nav_toolbars_rows_do_not_depend_on_when_the_gate_looked</c>, failed honestly:</para>
/// <code>
/// ── at 0 ms ──      line 0: ⏭ Long coast ahead (# d) — skip it      line 0: Follow Ship / Follow dest
/// ── at 1200 ms ──   line 0: ⏭ Long coast ahead (# d # h) — skip it  line 2: Follow Ship / Follow dest
/// </code>
/// <para>The advert grew a second number group when its countdown crossed the month boundary — #989's
/// ladder coarsens to <c>FormatHorizon</c>'s whole days past thirty, so <c>(30 d)</c> became
/// <c>(29 d 23 h)</c>, thirty-one pixels dearer — and <c>.btn-toolbar</c>'s <c>flex-wrap</c> (#123/#195)
/// broke the row one button earlier. The guard was right to fail: it normalises DIGITS, not digit GROUPS,
/// and the group count is what changes the width. <b>The instability is the screen.</b> A toolbar whose row
/// layout depends on a live countdown's text width is a toolbar that jumps under the captain's hand, and
/// #1234's flake was that jump seen from the gate's side.</para>
///
/// <para><b>Two questions, because neither answers the other.</b></para>
/// <list type="number">
///   <item><b>Every shape, on the real element.</b> The countdown is written into the live slot, one shape
///   at a time, and the advert measured after each. This reaches shapes the clock will not show a gate
///   inside a run — <c>(364 d)</c>, <c>(12.3 yr)</c>, the whole of <c>FormatHorizon</c>'s far end — and it
///   is a measurement of the shipped button with the shipped stylesheet on it, not a model of one.</item>
///   <item><b>The boundary the advert's own clock really crosses</b>, sampled live across it — with the
///   shipping warp slider pushed to its stop, because waiting on the clock alone is not a guard. #1234
///   measured its crossing at ~800 ms after the boot door on a quiet box; on a loaded runner the boot
///   goes past it and the countdown then holds for as long as anyone watches (seen on this branch). At
///   the stop the advert sheds an hour every ~0.36 s, and the guard requires to have seen readings of
///   two different LENGTHS — a digit changing is what #1239 already normalises away; a character count
///   changing is what broke the row — before it asserts anything, and it stops the moment it has one.</item>
/// </list>
///
/// <para>Neither test loosens #1239's normalisation, which stays exactly as it was: this is the product
/// being fixed so that guard can be true, rather than the guard being taught to look away.</para>
///
/// <para><b>RED PROOF</b> (run on this branch): delete the <c>.map-coast-countdown</c> rule from
/// <c>NavHud.razor.css</c>. The numbers are in the PR body.</para>
/// </summary>
public sealed class TheLongCoastAdvertKeepsItsWidthTests(Xunit.Abstractions.ITestOutputHelper output)
    : IAsyncLifetime
{
    private const float BootTimeoutMs = 600_000;

    // #1234's own screen and width: the Nav toolbar wraps at 1280 and the advert is on it, which is the
    // whole reason one button's width can decide which line its neighbour lands on.
    private const int Width = 1280;
    private const int Height = 900;
    private const string Url = "/map?scenario=sol&start=wreck&dest=saturn";

    private const string Toolbar = "[role='toolbar'][aria-label='Time warp controls']";

    /// <summary>Every shape #989's ladder can hand the advert. The advert opens at 24 h, so the seconds and
    /// minutes bands below it are unreachable and are not claimed here; everything from the hours band to
    /// <c>FormatHorizon</c>'s years is.</summary>
    private static readonly string[] EveryShapeTheLadderCanGive =
    [
        "(25 h)", "(47 h)", "(2 d 0 h)", "(11 d 11 h)", "(29 d 23 h)",
        "(30 d)", "(364 d)", "(1.0 yr)", "(12.3 yr)",
    ];

    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _host = await ClientHost.StartAsync(TextWriter.Null);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    private async Task<IPage> ABootedTabAsync()
    {
        IBrowserContext context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = Width, Height = Height },
        });
        IPage page = await context.NewPageAsync();
        await page.GotoAsync(_host.BaseUrl + Url, new() { Timeout = BootTimeoutMs });
        await page.BootDoorClosedAsync(BootTimeoutMs);
        return page;
    }

    [Fact]
    public async Task Every_shape_the_countdown_can_take_leaves_the_toolbar_the_same_row()
    {
        IPage page = await ABootedTabAsync();
        await page.SettledAsync(Toolbar + " button, " + Toolbar + " a.btn");

        string verdict = await page.EvaluateAsync<string>(ShapeSweepScript, new
        {
            toolbar = Toolbar,
            shapes = EveryShapeTheLadderCanGive,
        });
        output.WriteLine("[#1247 shapes]\n" + verdict);
        await page.Context.CloseAsync();

        Assert.False(verdict.StartsWith("premise:", StringComparison.Ordinal),
            "#1247 — this guard could not get to its question:\n" + verdict);
        Assert.True(verdict.StartsWith("steady", StringComparison.Ordinal),
            "#1247 — the long-coast advert changes size with what its countdown happens to say, so the "
            + "Nav toolbar's wrap point is a property of the clock rather than of the toolbar. That is the "
            + "row that jumped under #1234's gate and under the captain's hand:\n" + verdict);
    }

    [Fact]
    public async Task The_advert_keeps_its_width_across_the_boundary_its_own_clock_crosses()
    {
        IPage page = await ABootedTabAsync();

        // THE BENCH THAT SETS THE COUNTDOWN IS THE SHIPPING WARP SLIDER, pushed to its stop. Waiting on
        // the clock alone is not a guard: the crossing #1234 caught (30 d → 29 d 23 h) lands ~800 ms after
        // the boot door on a quiet box and BEFORE the door on a slow one, and a run that boots past it
        // sits on one reading for as long as anyone watches — seen on this branch, three times running.
        // At the stop the slider reads 10,000× and UpdateEffectiveWarp clamps it to ~1,000× here, so the
        // advert sheds an hour of coast every ~3.6 s of real time, and the crossing that changes the text
        // LENGTH (29 d 10 h → 29 d 9 h) is ~50 s out. The sampler below stops the moment it has one, so
        // that is what this usually costs; the ceiling is there for a runner where the clock is pulled
        // back rather than to be waited out. Machine-independent: warp multiplies REAL time, so a slow
        // runner serves fewer frames of the same size rather than a slower clock.
        await page.Locator(".map-warp-control input[type=range]").FillAsync("100");

        // NOT settled, deliberately: this measures a screen that is deliberately being made to move, and a
        // settle would stand and wait for the far side of the very thing being measured.
        string verdict = await page.EvaluateAsync<string>(LiveCrossingScript, new
        {
            toolbar = Toolbar,
            sampleForMs = 90_000,
            everyMs = 100,
        });
        output.WriteLine("[#1247 live crossing]\n" + verdict);
        await page.Context.CloseAsync();

        Assert.False(verdict.StartsWith("premise:", StringComparison.Ordinal),
            "#1247 — this guard drove the warp slider to its stop and watched the advert, and it never "
            + "crossed a boundary that changes the countdown's LENGTH — so it has asserted nothing. Either "
            + "the warp control did not take, or something pulled the clock back to 1×:\n" + verdict);
        Assert.True(verdict.StartsWith("steady", StringComparison.Ordinal),
            "#1247 — the Nav toolbar re-laid itself out when the advert's countdown crossed a boundary, "
            + "which is #1234's sighting exactly:\n" + verdict);
    }

    /// <summary>Writes each shape into the live slot and measures the shipped button after each. Every
    /// write and every read happen inside this one evaluate, so no Blazor re-render can land between them
    /// and hand a measurement back for text that is no longer there. The slot's own text is put back at the
    /// end, which matters because this page goes on living until the context closes.</summary>
    private const string ShapeSweepScript =
        "({ toolbar, shapes }) => {\n" + ToolbarRows.Declarations + "\n" + """
            const bar = document.querySelector(toolbar);
            if (!bar) { return 'premise: there is no Nav toolbar on this screen.'; }

            const controls = () => controlsOf(bar);

            const advert = controls().find(b => (b.innerText || '').includes('Long coast ahead'));
            if (!advert) {
                return 'premise: the long-coast advert is not on this toolbar, so its width decides nothing.';
            }
            const slot = advert.querySelector('.map-coast-countdown');
            if (!slot) {
                return 'premise: the advert has no .map-coast-countdown slot to write into — the markup '
                     + 'this guard was written against is gone.';
            }

            // WHICH LINE EACH CONTROL IS ON, by its place in the toolbar — and NOT by its label, which is
            // exactly why #1239's label comparison could not tell this bug from a legitimate reading: the
            // advert's own label changes SHAPE ("(# h)" against "(# d # h)") precisely when the bug fires.
            // #1265 moved that decision into ToolbarRows, where all three guards now read it.
            const rows = () => rowsOf(bar);

            const firstRows = rows();
            if (!firstRows.key.split(',').includes('1')) {
                return 'premise: the Nav toolbar fits on ONE line at this width, so nothing here could wrap '
                     + 'and a button growing would cost nothing.\n' + firstRows.shown;
            }

            const was = slot.textContent;
            const slotWidth = Math.round(slot.getBoundingClientRect().width * 100) / 100;
            const readings = [];
            try {
                for (const shape of shapes) {
                    slot.textContent = shape;
                    // The shape's OWN width, with the slot's floor lifted for the length of the read: the
                    // headroom this rule has is then a measured number rather than a hope, and a shape that
                    // has outgrown the slot is named rather than merely making two widths differ.
                    slot.style.minWidth = '0';
                    const natural = Math.round(slot.getBoundingClientRect().width * 100) / 100;
                    slot.style.minWidth = '';
                    readings.push({
                        shape,
                        natural,
                        width: Math.round(advert.getBoundingClientRect().width * 100) / 100,
                        rows: rows(),
                    });
                }
            } finally {
                slot.textContent = was;
                slot.style.minWidth = '';
            }

            const widest = readings.reduce((a, b) => (b.natural > a.natural ? b : a));
            const table = readings.map(r =>
                ('  ' + r.shape).padEnd(16) + ' natural w=' + String(r.natural).padEnd(7)
                + ' advert w=' + r.width).join('\n');
            const widths = [...new Set(readings.map(r => r.width))];
            const layouts = [...new Set(readings.map(r => r.rows.key))];

            if (widths.length > 1 || layouts.length > 1) {
                const a = readings[0];
                const b = readings.find(r => r.width !== a.width || r.rows.key !== a.rows.key);
                return 'MOVED — ' + widths.length + ' distinct advert width(s) and ' + layouts.length
                     + ' distinct row layout(s) across ' + readings.length + ' shape(s):\n' + table
                     + '\n── with (' + a.shape + ') ──\n' + a.rows.shown
                     + '\n── with (' + b.shape + ') ──\n' + b.rows.shown;
            }
            return 'steady — all ' + readings.length + ' shape(s) leave the advert ' + widths[0]
                 + ' px wide on the same rows. The slot is ' + slotWidth + ' px and its widest tenant '
                 + widest.shape + ' is ' + widest.natural + ' px, so the headroom is '
                 + (Math.round((slotWidth - widest.natural) * 100) / 100) + ' px:\n'
                 + table + '\n' + readings[0].rows.shown;
        }
        """;

    /// <summary>Samples the advert as its own clock runs, and keeps one reading per distinct countdown
    /// text. The sampler runs in the page on a timer, so what it records is what the captain would have
    /// been looking at.</summary>
    private const string LiveCrossingScript =
        "async ({ toolbar, sampleForMs, everyMs }) => {\n" + ToolbarRows.Declarations + "\n" + """
            const limit = Number(sampleForMs) > 0 ? Number(sampleForMs) : 5000;
            const step = Number(everyMs) > 0 ? Number(everyMs) : 100;
            const bar = document.querySelector(toolbar);
            if (!bar) { return 'premise: there is no Nav toolbar on this screen.'; }

            const controls = () => controlsOf(bar);

            // Keyed on the line numbers in toolbar order rather than on the labels — see the sweep above.
            const rows = () => rowsOf(bar);

            const byText = new Map();
            const started = performance.now();
            const lengthsSeen = () => new Set([...byText.keys()].map(t => t.length)).size;
            while (performance.now() - started < limit && lengthsSeen() < 2) {
                const advert = controls().find(b => (b.innerText || '').includes('Long coast ahead'));
                if (advert) {
                    const slot = advert.querySelector('.map-coast-countdown');
                    const text = (slot ? slot.textContent : advert.innerText || '').trim();
                    if (!byText.has(text)) {
                        byText.set(text, {
                            at: Math.round(performance.now() - started),
                            width: Math.round(advert.getBoundingClientRect().width * 100) / 100,
                            rows: rows(),
                        });
                    }
                }
                await new Promise(r => setTimeout(r, step));
            }

            const seen = [...byText.entries()];
            if (seen.length === 0) { return 'premise: the long-coast advert never appeared on this toolbar.'; }
            const table = seen.map(([t, r]) => '  [' + r.at + ' ms] ' + t + ' advert w=' + r.width).join('\n');
            if (seen.length < 2) {
                return 'premise: the advert read the same thing for the whole ' + limit + ' ms.\n' + table;
            }
            // …and it has to have crossed a boundary that changes the text's LENGTH. "29 d 23 h" becoming
            // "29 d 22 h" is a digit, and a digit is what #1239 already normalises away; what broke the row
            // was a CHARACTER COUNT changing. A window that only ever saw the former has watched the clock
            // tick, not a boundary get crossed.
            const lengths = [...new Set(seen.map(([t]) => t.length))];
            if (lengths.length < 2) {
                return 'premise: every reading in this window was ' + lengths[0] + ' characters long, so no '
                     + 'boundary that could change the advert\'s WIDTH was crossed.\n' + table;
            }

            const widths = [...new Set(seen.map(([, r]) => r.width))];
            const layouts = [...new Set(seen.map(([, r]) => r.rows.key))];
            if (widths.length > 1 || layouts.length > 1) {
                const [ta, a] = seen[0];
                const [tb, b] = seen.find(([, r]) => r.width !== a.width || r.rows.key !== a.rows.key);
                return 'MOVED across the crossing:\n' + table
                     + '\n── at ' + ta + ' ──\n' + a.rows.shown
                     + '\n── at ' + tb + ' ──\n' + b.rows.shown;
            }
            return 'steady — ' + seen.length + ' distinct countdown reading(s), one advert width ('
                 + widths[0] + ' px) and one row layout:\n' + table + '\n' + seen[0][1].rows.shown;
        }
        """;
}
