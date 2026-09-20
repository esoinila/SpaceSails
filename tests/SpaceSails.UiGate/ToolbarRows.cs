namespace SpaceSails.UiGate;

/// <summary>
/// #1265 · <b>A ROW IS A GEOMETRY FACT. THE LABEL ON IT IS ONLY A NAME.</b>
///
/// <para><b>The sighting</b> (2026-09-20, PR #1263 — a doc-only change).
/// <c>TheGateMeasuresAStillScreenTests.The_nav_toolbars_rows_do_not_depend_on_when_the_gate_looked</c> went
/// red on two readings whose every control sat on the same line:</para>
/// <code>
/// ── at 0 ms ──      line 0: 🗺 Plot …  line 2: Follow Ship  line 2: Follow dest
/// ── at 1200 ms ──   line 0: 🗺 Plot …  line 2: Follow Ship  line 2: Follow dest
/// </code>
/// <para>Every line number matched. The only difference in the whole listing was the long-coast advert's
/// text — <c>(# d)</c> against <c>(# d # h)</c> — because #1239 built the thing it compared by gluing the
/// line number to the <i>digit-normalised label</i>. #1252 had already made that text harmless to the
/// layout (a fixed-width slot with tabular figures), so the screen genuinely did not move; the guard went
/// red about a sentence. It was then the guard, not the toolbar, that depended on when the gate looked.</para>
///
/// <para><b>So the key is the geometry and nothing else</b>: each control's LINE, keyed by its index in
/// toolbar order. A control's label rides along to be READ in a failure and is never part of the
/// comparison — not raw, and not normalised either, because normalising is a guess about which parts of a
/// sentence are allowed to change and #1239's guess (digits, but not digit GROUPS) was wrong twice in two
/// days. #1252's own two guards were written this way from the start, for exactly this reason; this file is
/// that decision spelled once, for all three of them.</para>
///
/// <para><b>What it still catches.</b> Everything geometric: a control moving line, a control arriving or
/// leaving (the key's length changes), the whole row re-wrapping. Deliberately NOT a control being renamed,
/// which is <c>NavHudMarkup.baseline.txt</c>'s question and not a row's.</para>
/// </summary>
internal static class ToolbarRows
{
    /// <summary>
    /// JavaScript declarations to paste at the top of an evaluate's arrow body, before anything else:
    /// <c>controlsOf(bar)</c> — every laid-out control in the toolbar, in document order — and
    /// <c>rowsOf(bar)</c>, which returns <c>{ key, shown }</c>.
    ///
    /// <para><c>key</c> is the comparable: the line index of each control, in toolbar order, joined by
    /// commas. <c>shown</c> is the human listing, labels and all, and exists only to be printed in a
    /// failure message.</para>
    ///
    /// <para>Both take <c>bar</c> as an argument rather than closing over it, so the block can sit at the
    /// very top of a body that has not looked the toolbar up yet.</para>
    /// </summary>
    public const string Declarations = """
        // #1265 · WHICH LINE EACH CONTROL IS ON, BY ITS PLACE IN THE TOOLBAR — AND NEVER BY ITS LABEL.
        // A zero-sized control is not on any line, so it is not a row fact and is left out.
        const controlsOf = (bar) => [...bar.querySelectorAll('button, a.btn')]
            .filter(el => { const r = el.getBoundingClientRect(); return r.width > 0 && r.height > 0; });

        const rowsOf = (bar) => {
            const seen = controlsOf(bar).map((el, i) => ({
                i,
                y: Math.round(el.getBoundingClientRect().y),
                label: (el.innerText || '').replace(/\s+/g, ' ').trim(),
            }));
            // The distinct tops, top-down: "line 0" is the first row on the screen rather than a pixel, so
            // a toolbar that sits ten pixels lower than it used to reads as the same rows.
            const tops = [...new Set(seen.map(s => s.y))].sort((a, b) => a - b);
            return {
                key: seen.map(s => tops.indexOf(s.y)).join(','),
                shown: seen.map(s => 'line ' + tops.indexOf(s.y) + ': ' + s.label).join('\n'),
            };
        };
        """;
}
