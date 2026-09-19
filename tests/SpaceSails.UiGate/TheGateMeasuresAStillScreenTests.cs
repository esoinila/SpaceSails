using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1234 · <b>THE ROW IS THE SAME ROW WHENEVER THE GATE LOOKS AT IT.</b>
///
/// <para><b>The sighting.</b> <c>TheFollowDestButtonIsRealTests.Follow_dest_stands_beside_follow_ship…</c>
/// went red on 2026-09-18 on a commit that touched no markup — <i>"Follow dest sits on a different row from
/// Follow Ship (y 165 vs 125)"</i> — and green on a re-run of the identical commit. Both times the answer
/// was the re-run button, which is the #1109 class wearing a different coat: a guard that is red one run in
/// ten teaches nobody anything, and a guard that goes green because it was asked twice has asserted
/// nothing.</para>
///
/// <para><b>What was actually happening</b> (probed on this branch — one boot, sampled every 250 ms, logged
/// only when something moved):</para>
/// <code>
/// [ 13155 ms] Follow Ship visible — THIS IS WHERE THE GATE MEASURES
/// [ 13173 ms] ⏭ Long coast ahead (30 d) — @388,125 w243 … Follow Ship@880,125 | Follow dest@978,125
/// [ 13968 ms] ⏭ Long coast ahead (29 d 23 @388,125 w274 … Follow Ship@911,125 | Follow dest@20,165
/// </code>
/// <para>Eight hundred milliseconds after the boot door came down, the long-coast advert re-read its own
/// countdown — <c>30 d</c> to <c>29 d 23 h</c>, thirty-one pixels wider — and <c>.btn-toolbar</c>'s
/// <c>flex-wrap</c> (#123/#195) broke the row one button earlier. y 165, the exact number CI printed. The
/// commit had nothing to do with it; the gate simply read a row that was still being written, and which of
/// the two rows it got was decided by how many milliseconds the box took to get from the door to the
/// bounding box.</para>
///
/// <para><b>So this is the law, and it is about the GATE rather than the game:</b> boot the same screen
/// three times, wait a different length of time each time, and the row the gate reads must be the same row.
/// Not the same pixels to the byte — sim time passes and a countdown is allowed to spend a character — but
/// the same controls on the same LINES, which is the only thing the #956 and #1219 guards ever assert. What
/// makes it true is <see cref="GateReady.SettledAsync"/>: no box is read until the toolbar's own controls
/// have all held the same rectangle for three quarters of a second.</para>
///
/// <para><b>RED PROOF</b> (run on this branch before the PR): delete the <c>SettledAsync</c> call from
/// <see cref="ReadTheToolbarRows"/> and this fails on the very first pair — the 0 ms reading puts
/// <c>Follow dest</c> on line 0 and the 1200 ms reading on line 1, and the failure prints both rows.</para>
///
/// <para><b>Its own premise, out loud.</b> A toolbar that stopped wrapping, or stopped carrying the advert,
/// would make this pass while proving nothing — so it requires a crowded row that really does take more than
/// one line, and Follow Ship and Follow dest really on it, before it compares anything.</para>
/// </summary>
public sealed class TheGateMeasuresAStillScreenTests : IAsyncLifetime
{
    private const float BootTimeoutMs = 180_000;

    // The #1234 screen, at the width its own guard uses: the Nav toolbar wraps here, which is the whole
    // reason a button's WIDTH can decide which line its neighbour lands on.
    private const int Width = 1280;
    private const int Height = 900;
    private const string Url = "/map?scenario=sol&start=wreck&dest=saturn";

    // The row itself. Scoped to the Nav desk's own toolbar so the settle below asks exactly the boxes this
    // guard reads to hold still — and not, say, the flight readouts beside them, which re-read live numbers
    // every tick and would never hold still at all.
    private const string Toolbar = "[role='toolbar'][aria-label='Time warp controls']";

    // How long after the boot door each reading waits before it asks for a settled screen. The flake's own
    // window is ~800 ms, so the three straddle it: one before, one after, one well clear.
    private static readonly int[] Delays = [0, 1_200, 3_000];

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
    }

    /// <summary>A brand-new context per reading — the three boots have to differ in NOTHING but how long the
    /// gate waited, and a shared origin would carry the last voyage's <c>localStorage</c> (the crash note,
    /// the autosave) into the next one.</summary>
    private async Task<IPage> AFreshTab()
    {
        IBrowserContext context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = Width, Height = Height },
        });
        return await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    [Fact]
    public async Task The_nav_toolbars_rows_do_not_depend_on_when_the_gate_looked()
    {
        var readings = new List<(int Delay, string Rows)>();

        foreach (int delay in Delays)
        {
            // A FRESH WORLD each time, not a second look at the same one: the row settles once, early, so a
            // gate that booted once and read twice would be asking the same settled screen the same question.
            _page = await AFreshTab();
            await _page.GotoAsync(_host.BaseUrl + Url, new() { Timeout = BootTimeoutMs });
            await _page.BootDoorClosedAsync(BootTimeoutMs);
            if (delay > 0)
            {
                await _page.WaitForTimeoutAsync(delay);
            }

            readings.Add((delay, await ReadTheToolbarRows()));
            await _page.Context.CloseAsync();
        }

        // ── The premise, out loud ───────────────────────────────────────────────────────────────────────
        string first = readings[0].Rows;
        string[] lines = first.Split('\n');
        Assert.True(lines.Length >= 8,
            $"the Nav toolbar drew only {lines.Length} control(s) on this boot. #1234 is about a row CROWDED "
            + "enough that one button's width decides which line its neighbour lands on; a short row would "
            + "make this guard green about nothing.\n" + first);
        Assert.True(lines.Any(l => l.StartsWith("line 1", StringComparison.Ordinal)),
            "the Nav toolbar fitted on a single line at 1280×900, so nothing here could wrap and this guard "
            + "is proving nothing. Find a width (or a state) where it takes two.\n" + first);
        Assert.True(
            first.Contains("Follow Ship", StringComparison.Ordinal)
            && first.Contains("Follow dest", StringComparison.Ordinal),
            "the two follows #956 pairs are not both on this row, so the very placement #1234 went red about "
            + "is not being measured.\n" + first);

        // ── The law ─────────────────────────────────────────────────────────────────────────────────────
        foreach ((int delay, string rows) in readings.Skip(1))
        {
            Assert.True(string.Equals(rows, first, StringComparison.Ordinal),
                $"#1234 — the Nav toolbar reads as a DIFFERENT row depending on when the gate looked at it "
                + $"({readings[0].Delay} ms after the boot door vs {delay} ms). A geometry guard standing on "
                + "this is a coin toss, and a re-run is not a fix.\n"
                + $"── at {readings[0].Delay} ms ──\n{first}\n── at {delay} ms ──\n{rows}");
        }
    }

    /// <summary>
    /// The toolbar as LINES: every control's line number and its label with the digits knocked out. Lines
    /// rather than pixels, and no digits, because sim time is allowed to pass between two boots — the
    /// countdown may legitimately read <c>29 d 23 h</c> on one and <c>29 d 22 h</c> on the next. What may
    /// NOT change is which line each control is on, because that is the whole of what #956 and #1219 assert.
    /// </summary>
    private async Task<string> ReadTheToolbarRows()
    {
        // #1234's fix, and the one line this guard exists to protect: nothing is read until nothing has
        // moved. Delete it and this test goes red — that is the red proof in the class comment.
        await _page.SettledAsync(Toolbar + " button, " + Toolbar + " a.btn");

        return await _page.EvaluateAsync<string>(
            """
            () => {
                const bar = document.querySelector("[role='toolbar'][aria-label='Time warp controls']");
                if (!bar) { return '(no Nav toolbar on the screen)'; }
                const seen = [];
                for (const el of bar.querySelectorAll('button, a.btn')) {
                    const r = el.getBoundingClientRect();
                    if (r.width <= 0 || r.height <= 0) { continue; }
                    seen.push({ y: Math.round(r.y), label: (el.innerText || '').replace(/\s+/g, ' ').trim() });
                }
                const tops = [...new Set(seen.map(s => s.y))].sort((a, b) => a - b);
                return seen
                    .map(s => 'line ' + tops.indexOf(s.y) + ': ' + s.label.replace(/\d+/g, '#'))
                    .join('\n');
            }
            """);
    }
}
