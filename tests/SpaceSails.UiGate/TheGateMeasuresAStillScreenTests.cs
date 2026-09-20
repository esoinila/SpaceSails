using System.Text.Json;
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
/// <para><b>#1265 — what "the same LINES" is compared as, and what it is not.</b> The first cut of this
/// guard compared a listing that glued each line number to the control's <i>digit-normalised label</i>, and
/// on PR #1263 (doc-only) it went red on two readings whose every control sat on the same line: the whole of
/// the difference was <c>(# d)</c> against <c>(# d # h)</c> in the long-coast advert. #1252 had already made
/// that text harmless to the layout, so the screen had not moved — the guard had. The comparable is
/// <see cref="ToolbarRows"/>'s <c>key</c> now: each control's line, keyed by its index in toolbar order, no
/// text in it at all. Labels are printed in the failure and never compared, not even normalised — the
/// normalisation was a guess about which parts of a sentence may change, and it was wrong twice in two days
/// (it knocked out digits but not digit GROUPS, and the group count is what moved the row).</para>
///
/// <para><b>RED PROOF</b> (both run on this branch before the PR):</para>
/// <list type="number">
///   <item>delete the <c>SettledAsync</c> call from <see cref="ReadTheToolbarRows"/> — the gate is back to
///   reading a row that is still being written;</item>
///   <item>plant a real re-wrap: widen the advert's slot (<c>.map-coast-countdown { min-width: 40ch }</c>,
///   the geometry #1252 pinned) on the later readings only. The key changes, and the failure prints both
///   listings. A text-only difference of the #1263 kind — two readings whose advert reads different
///   LENGTHS inside #1252's fixed slot — leaves it green, which is the whole of this fix.</item>
/// </list>
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
        var readings = new List<(int Delay, Rows Rows)>();

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
        // Every premise below is read off the KEY (geometry) except the last, which asks whether two named
        // controls are on this toolbar at all. That one is identity — is the thing #956 is about even here —
        // and not a comparison of one reading against another, which is the line #1265 draws.
        Rows first = readings[0].Rows;
        string[] lines = first.Key.Split(',');
        Assert.True(lines.Length >= 8,
            $"the Nav toolbar drew only {lines.Length} control(s) on this boot. #1234 is about a row CROWDED "
            + "enough that one button's width decides which line its neighbour lands on; a short row would "
            + "make this guard green about nothing.\n" + first.Shown);
        Assert.True(lines.Contains("1"),
            "the Nav toolbar fitted on a single line at 1280×900, so nothing here could wrap and this guard "
            + "is proving nothing. Find a width (or a state) where it takes two.\n" + first.Shown);
        Assert.True(
            first.Shown.Contains("Follow Ship", StringComparison.Ordinal)
            && first.Shown.Contains("Follow dest", StringComparison.Ordinal),
            "the two follows #956 pairs are not both on this row, so the very placement #1234 went red about "
            + "is not being measured.\n" + first.Shown);

        // ── The law ─────────────────────────────────────────────────────────────────────────────────────
        // KEY against KEY: the line each control is on, in toolbar order. The listings below it carry the
        // labels so a failure can be read, and they are not what decides it (#1265).
        foreach ((int delay, Rows rows) in readings.Skip(1))
        {
            Assert.True(string.Equals(rows.Key, first.Key, StringComparison.Ordinal),
                $"#1234 — the Nav toolbar reads as a DIFFERENT row depending on when the gate looked at it "
                + $"({readings[0].Delay} ms after the boot door vs {delay} ms). A geometry guard standing on "
                + "this is a coin toss, and a re-run is not a fix.\n"
                + $"── at {readings[0].Delay} ms ── lines {first.Key}\n{first.Shown}\n"
                + $"── at {delay} ms ── lines {rows.Key}\n{rows.Shown}");
        }
    }

    /// <summary>The toolbar as lines: <c>Key</c> is what is compared (see <see cref="ToolbarRows"/>),
    /// <c>Shown</c> is the listing a failure prints.</summary>
    private sealed record Rows(string Key, string Shown);

    /// <summary>
    /// The toolbar as LINES: which line each control is on, keyed by its index in toolbar order. Lines
    /// rather than pixels because the toolbar is allowed to sit at a different height; positions rather
    /// than labels because sim time is allowed to pass between two boots and the countdown may legitimately
    /// read <c>(30 d)</c> on one and <c>(29 d 23 h)</c> on the next (#1265). What may NOT change is which
    /// line each control is on, because that is the whole of what #956 and #1219 assert.
    /// </summary>
    private async Task<Rows> ReadTheToolbarRows()
    {
        // #1234's fix, and the one line this guard exists to protect: nothing is read until nothing has
        // moved. Delete it and this test goes red — that is the red proof in the class comment.
        await _page.SettledAsync(Toolbar + " button, " + Toolbar + " a.btn");

        // One evaluate, not two: the key and the listing that explains it are read off the SAME layout, so
        // a re-render landing between them cannot print a listing that does not belong to the key.
        string json = await _page.EvaluateAsync<string>(RowsScript, Toolbar);
        return JsonSerializer.Deserialize<Rows>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? new Rows("(unreadable)", "(the toolbar probe returned nothing)");
    }

    private const string RowsScript =
        ProbeHead + "\n" + ToolbarRows.Declarations + "\n" + ProbeTail;

    private const string ProbeHead = """
        (toolbar) => {
            const bar = document.querySelector(toolbar);
            if (!bar) {
                return JSON.stringify({ key: '(none)', shown: '(no Nav toolbar on the screen)' });
            }
        """;

    private const string ProbeTail = """
            return JSON.stringify(rowsOf(bar));
        }
        """;
}
