using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #1234 · <b>WHEN MAY A GATE READ A BOX?</b> — the one answer, for every gate in this project.
///
/// <para><b>The sighting.</b> <c>TheFollowDestButtonIsRealTests.Follow_dest_stands_beside_follow_ship…</c>
/// went red on 2026-09-18 on a commit that touched no markup — <i>"Follow dest sits on a different row from
/// Follow Ship (y 165 vs 125)"</i> — and green on a re-run of the identical commit. Nobody could say why, so
/// the re-run button was the fix.</para>
///
/// <para><b>What the page actually said</b> (probed on this branch: one boot, sampled every 250 ms, logged
/// only when something moved):</para>
/// <code>
/// [ 13155 ms] Follow Ship visible — THIS IS WHERE THE GATE MEASURES
/// [ 13173 ms] ⏭ Long coast ahead (30 d) — @388,125 w243 … Follow Ship@880,125 | Follow dest@978,125
/// [ 13968 ms] ⏭ Long coast ahead (29 d 23 @388,125 w274 … Follow Ship@911,125 | Follow dest@20,165
/// </code>
/// <para>Eight hundred milliseconds after the boot door came down, the long-coast advert re-read its own
/// countdown — <c>30 d</c> to <c>29 d 23 h</c>, <b>thirty-one pixels wider</b>. That was more than the row
/// had left, so <c>.btn-toolbar</c>'s <c>flex-wrap</c> (#123/#195) broke one button earlier and Follow dest
/// moved to the next line: y 165, the exact number CI printed. Nothing to do with the commit, the markup or
/// the fonts — the gate measured a row that was still being written, and which of the two rows it got was
/// decided by how many milliseconds passed between the door detaching and the bounding-box query. Measured
/// here: ~20 ms on a quiet dev box (20 runs, all green), and a deliberate 1.2 s standing in for a loaded CI
/// runner (10 runs, all red, with that same sentence and those same two numbers).</para>
///
/// <para><b>It is not the fonts.</b> #1234 guessed a web font arriving late. This client has no web font at
/// all — no <c>@font-face</c>, no font file in <c>wwwroot</c>, a system stack in <c>app.css</c> — and the
/// probe read <c>document.fonts.status = loaded</c> on its very first sample. <see cref="SettledAsync"/>
/// still awaits <c>document.fonts.ready</c>, because it costs nothing and the day somebody adds a face is
/// the day this would otherwise become the cause. But today's cause is the settle, not the font.</para>
///
/// <para><b>The seam, and why it is narrow.</b> A gate may not read a box until <i>that box</i> has stopped
/// moving — so <see cref="SettledAsync"/> takes the selector list of exactly what is about to be measured.
/// "The whole screen holds still" is a thing this game never does and must never be asked for: the flight
/// readouts re-read live numbers every tick and would hold a gate up forever while telling it nothing about
/// the row it came to measure. The boxes the assertion stands on are the boxes that have to be still, and
/// the caller names them.</para>
///
/// <para>If they never stop, this <b>throws</b> and names the box that kept moving with both of its
/// readings. It is not a retry and it never re-reads until it likes the answer: a gate that cannot get a
/// still screen has not measured anything.</para>
/// </summary>
internal static class GateReady
{
    /// <summary>Every control the #236 band sweep asks about — the widest honest scope in this file, and
    /// still only the boxes that gate actually reads.</summary>
    public const string EveryPressable = ".map-page button, .map-page input, .map-page a[href]";

    /// <summary>How long the named boxes must all hold still. Two frames of stillness is luck; the flake
    /// above moved 800 ms after the door and the reprojection cadence behind it runs every 250 ms, so the
    /// window is set well clear of both.</summary>
    public const int DefaultStableMs = 750;

    /// <summary>The ceiling on waiting for stillness. Interpreted WASM under a plain local publish is ~100×
    /// slower than the AOT build CI runs, so this is generous; it is a FAILURE bound, not a budget.</summary>
    public const int DefaultSettleTimeoutMs = 30_000;

    /// <summary>
    /// THE BOOT DOOR, BOTH HALVES. <c>GotoAsync</c> returns while the page is still an empty shell, so a bare
    /// "wait until <c>.map-loading</c> is gone" is satisfied instantly by a door that has not been hung yet —
    /// and the gate then measures the shell. Waiting for it to be ATTACHED first and only then DETACHED is
    /// the difference between "the world is ready" and "the page has not started". Thirteen call sites across
    /// nine gates in this project had only the second half; they have both now, through here.
    /// </summary>
    public static async Task BootDoorClosedAsync(this IPage page, float timeoutMs)
    {
        await page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
        await page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = timeoutMs });
    }

    /// <summary>
    /// EVERYTHING THAT MUST BE TRUE BEFORE A BOX IS READ: the fonts are in, two animation frames have been
    /// served, and every element matching <paramref name="boxes"/> has had the same laid-out rectangle across
    /// consecutive frame-pairs for <paramref name="stableMs"/> — the same count of them too, so a control
    /// that has not arrived yet is caught as surely as one that has moved.
    /// </summary>
    /// <param name="boxes">A CSS selector list naming exactly what the caller is about to measure. Narrow is
    /// right: this asks the boxes the assertion stands on to hold still, not the screen around them.</param>
    /// <exception cref="InvalidOperationException">they never held still inside
    /// <paramref name="timeoutMs"/> — the message names the box that kept moving and both of its readings.
    /// </exception>
    public static async Task SettledAsync(
        this IPage page,
        string boxes,
        int stableMs = DefaultStableMs,
        int timeoutMs = DefaultSettleTimeoutMs)
    {
        // #1234 · THE TWO NUMBERS TRAVEL AS `int`, AND THAT IS LOAD-BEARING. Written as `float`, Playwright's
        // .NET serialiser hands the page `{}` for each of them — proven with a diagnostic evaluate that
        // printed `stableMs={} timeoutMs={}` — and `now - started > {}` is false forever, so the settle loop
        // below spun until the whole gate was killed by hand. A silent wrong type, not a wrong idea: the
        // shape of the FIFTH named bug class, where the world cannot tell pass from fail. The script coerces
        // with Number() as well, so a future caller cannot reintroduce it from the other side.
        string verdict = await page.EvaluateAsync<string>(SettleScript, new
        {
            boxes,
            stableMs,
            timeoutMs,
        });

        if (!verdict.StartsWith("settled", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"#1234 — the gate asked '{boxes}' to hold still before measuring it, and it never did: "
                + $"{verdict}. A geometry assertion made on a moving layout is a coin toss, so this is a "
                + "failure rather than a measurement.");
        }
    }

    /// <summary>The boot door and the settle, in the one order that is ever right — what a gate calls after
    /// <c>GotoAsync</c> when it is going to measure something.</summary>
    public static async Task BootedAndSettledAsync(
        this IPage page,
        float bootTimeoutMs,
        string boxes,
        int stableMs = DefaultStableMs,
        int settleTimeoutMs = DefaultSettleTimeoutMs)
    {
        await page.BootDoorClosedAsync(bootTimeoutMs);
        await page.SettledAsync(boxes, stableMs, settleTimeoutMs);
    }

    // Runs in the page, on the page's own frame clock, so "two frames apart" means two frames and not two
    // CDP round-trips. It returns a SENTENCE either way — the caller turns anything but 'settled' into a
    // failure — so the reason a box would not hold still travels back with the failure instead of being lost
    // in a timeout. The frame wait is raced against a 250 ms timer: a page that stops serving animation
    // frames must still reach the deadline and report, never hang an evaluate that has no timeout of its own.
    private const string SettleScript = @"
        async ({ boxes, stableMs, timeoutMs }) => {
            // Belt and braces after the `{}` bug above: a number that did not survive the trip is a settle
            // that never ends, so neither of these may be taken on trust.
            const hold = Number(stableMs) > 0 ? Number(stableMs) : 750;
            const limit = Number(timeoutMs) > 0 ? Number(timeoutMs) : 30000;

            if (document.fonts && document.fonts.ready) { await document.fonts.ready; }

            const tick = () => new Promise(done => {
                let already = false;
                const finish = () => { if (!already) { already = true; done(); } };
                requestAnimationFrame(() => requestAnimationFrame(finish));
                setTimeout(finish, 250);
            });

            const restless = (el) => {
                try { return (getComputedStyle(el).animationIterationCount || '').includes('infinite'); }
                catch (ignored) { return false; }
            };

            const snap = () => {
                const found = Array.from(document.querySelectorAll(boxes));
                const keys = [];
                const kept = [];
                for (const el of found) {
                    // An element the page deliberately never lets rest — a spinner, a pulse — is not evidence
                    // that the LAYOUT is moving, and waiting for one to stop is waiting forever.
                    if (restless(el)) { continue; }
                    const r = el.getBoundingClientRect();
                    keys.push(el.tagName + '.' + (el.getAttribute('class') || '') + '@'
                        + Math.round(r.x) + ',' + Math.round(r.y)
                        + ' ' + Math.round(r.width) + 'x' + Math.round(r.height));
                    kept.push(el);
                }
                return { keys, kept };
            };

            const describe = (before, after) => {
                const n = Math.max(before.keys.length, after.keys.length);
                for (let i = 0; i < n; i++) {
                    if (before.keys[i] === after.keys[i]) { continue; }
                    const el = after.kept[i] || before.kept[i];
                    const words = el ? (el.innerText || el.textContent || '') : '';
                    const label = words.replace(/\s+/g, ' ').trim().slice(0, 40);
                    return '[' + (before.keys[i] || '(absent)') + '] became [' + (after.keys[i] || '(absent)')
                        + ']' + (label ? '  text: ' + label : '');
                }
                return 'the box count changed (' + before.keys.length + ' -> ' + after.keys.length + ')';
            };

            const started = performance.now();
            let previous = null;
            let lastChangeAt = started;
            let lastMover = '(it never held still for a single pair of frames)';

            for (;;) {
                const now = performance.now();
                const current = snap();
                if (previous === null || current.keys.join('|') !== previous.keys.join('|')) {
                    if (previous !== null) { lastMover = describe(previous, current); }
                    previous = current;
                    lastChangeAt = now;
                } else if (now - lastChangeAt >= hold) {
                    return 'settled after ' + Math.round(now - started) + ' ms ('
                        + current.keys.length + ' box(es) matching ' + boxes + ')';
                }
                if (now - started > limit) {
                    return 'gave up after ' + Math.round(now - started) + ' ms — last movement: ' + lastMover;
                }
                await tick();
            }
        }";
}
