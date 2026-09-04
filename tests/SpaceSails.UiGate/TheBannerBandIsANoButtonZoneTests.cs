using System.Text.Json;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #236 · THE BANNER'S BAND IS A NO-BUTTON ZONE ON EVERY SCREEN.
///
/// <para>Owner ruling, mid-car-run (2026-07-17): <i>"generally we should try to keep the ship status real
/// estate (under them) button free in all screens to avoid unpressable buttons."</i> The band is the box
/// <c>.map-topstack</c> lays out — desk tabs, the who-flies banner, the #166 alert strip — at whatever height
/// the banner really grew to this frame. The banner may grow into its band; nothing else may sit in it.</para>
///
/// <para><b>Why this is a browser gate and not a stylesheet one.</b> The rule was kept for two years by a
/// hand-tuned number (<c>--desk-top-clearance</c>: 5.75rem, then 8.25rem) that stood for "how tall can the
/// masthead get?" — an estimate, re-typed each time the banner grew, and green in every unit test in the
/// repository on the day it was wrong. Only a real layout can answer where a control actually IS, so the law
/// asks the browser: it reads the band off the topstack's own laid-out box and then asks every pressable
/// control on the screen whether it is standing in it.</para>
///
/// <para><b>The three ways a control is allowed to be in the band</b>, and the third is the one this lane
/// built:</para>
/// <list type="number">
/// <item>It is the band's OWN — a descendant of <c>.map-topstack</c>. The banner's ▲▼ pager and the alert
/// strip's ✕ / Board her / Stand down chips are the banner growing into its own real estate.</item>
/// <item>It is <b>named furniture</b> that shares the band deliberately — <see cref="TheFurnitureThatSharesTheBand"/>
/// — and it is proven to be REACHABLE there: the gate hit-tests its own centre and requires the browser to
/// hand back that control. An excuse written in a table is not a fact; an <c>elementFromPoint</c> is.</item>
/// <item>It is a <b>modal that owns the screen</b> — <see cref="TheModalsThatOwnTheScreen"/> — covering
/// everything on purpose (the berth picker gate, the BUSTED interrupt). #236 exempts these by name.</item>
/// </list>
///
/// <para>Anything else is an offence, named with its pixels. <b>A new overlay family with no row goes
/// red</b> — and so does a row nobody meets any more: every row in both tables must be SEEN at least once
/// across the sweep, so the list cannot rot into a set of excuses for surfaces that no longer exist.</para>
///
/// <para><b>The structural half.</b> Clearing the band today is not the same as clearing it tomorrow, so the
/// gate also asks HOW each desk clears it: <see cref="Every_desk_flows_under_the_band_rather_than_clearing_it_by_a_number"/>
/// walks every ancestor from <c>.desk-content</c> up to <c>.map-flowcolumn</c> and requires every one of them
/// to be in flow. A desk in flow cannot be covered by a growing banner — it is PUSHED. That is what makes the
/// rule keep itself: there is no number left to be wrong.</para>
///
/// <para><b>Proven RED.</b> Restoring the hand-tuned clearance this lane deleted, at the value it carried
/// before the #190 voice grew the banner — <c>.desk-layer { position: absolute; inset: 0; padding-top:
/// 5.75rem; }</c> — fails with <b>27 offences across the sweep</b>, and the first five of them are the exact
/// row the owner caught in 2026-07-17:</para>
/// <code>
/// #236 · 27 pressable control(s) are laid out inside the banner's band …
///   Selene Gate · Captain · '⚓ Orders' (.btn.btn-info) at (349,100) 84×31 — 11 px of it is inside
///     the banner's band, and it is not the band's own, not named furniture and not a modal. …
///   Selene Gate · Captain · '🧭 Ship status' … at (432,100) 110×31 — 11 px …
///   Selene Gate · Captain · '🎓 Tutorials' … · '📜 Ledger' … · '🌡 Crew' …
///   Selene Gate · Sensors · '🔍−' (.btn.btn-outline-light.py-0) at (937,96) 45×23 — 15 px …
///   390×700 · Captain · '⚓ Orders' (.btn.btn-info) at (-25,100) 61×73 — 73 px …  (the whole row)
/// </code>
/// </summary>
public sealed class TheBannerBandIsANoButtonZoneTests : IAsyncLifetime
{
    private static readonly float BootTimeoutMs = 180_000;

    /// <summary>
    /// THE FURNITURE THAT SHARES THE BAND. Two neighbours stand in the masthead's band on purpose and have
    /// their own laws saying so; each row is an exemption that must be EARNED every run — the gate hit-tests
    /// the control's centre and fails if the browser hands back anything else, because a control that is
    /// excused from the band and buried in it is exactly the bug #236 exists to stop.
    /// </summary>
    private static readonly (string Class, string Why)[] TheFurnitureThatSharesTheBand =
    [
        (".map-layers",
         "#986 F1 · the 🗺 Layers toggle is the top-LEFT floating map-control strip, anchored at "
         + "--map-control-top. It is why the band has a floor at all (.map-topstack's min-height is this "
         + "strip's own top plus its own height), and it paints at chrome+30 against the masthead's "
         + "chrome+14 — the banner can never bury it."),
        (".nav-search",
         "#406 · the find-a-target box, the strip's other half, at chrome+40. Same band, same reason, and "
         + "the same proof: it is on top where it stands."),
        (".desk-chip-strip",
         "#994/#997 · the right-edge desk status-chip column, top 4.75rem, z-index 30. It is the one piece "
         + "of furniture all seven desks share and it is deliberately in the masthead's row — above it in "
         + "paint order, so a banner that grows wide passes UNDER the chips rather than over them."),
    ];

    /// <summary>
    /// THE MODALS THAT OWN THE SCREEN — #236's own exemption, verbatim: <i>"Overlays that deliberately cover
    /// everything (celebration, BUSTED, map card, freeze-frame) are exempt — they are modal by intent."</i>
    /// A modal is not competing with the banner for the band; it has taken the whole window, banner included.
    /// </summary>
    private static readonly (string Class, string Why)[] TheModalsThatOwnTheScreen =
    [
        (".start-picker-backdrop", "the berth picker gate — the front door, before there is a voyage to fly."),
        (".busted-card", "the BUSTED interrupt (#621's death pipeline) — it owns the screen while it is up."),
    ];

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
        _page = await _browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 720 } });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    // ── What the browser is asked ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the band off <c>.map-topstack</c>'s own laid-out box, then every pressable control under
    /// <c>.map-page</c> with the three facts the law needs of it: is it the band's own, is it named
    /// furniture or a modal, and — the one no stylesheet can answer — is it actually on top where it stands.
    /// </summary>
    private const string TheProbe = """
        (arg) => {
          const families = arg.families, modals = arg.modals;
          const page = document.querySelector('.map-page');
          const stack = document.querySelector('.map-topstack');
          if (!page || !stack) return JSON.stringify({ band: null, modal: null, controls: [] });
          const sb = stack.getBoundingClientRect();

          // Is a modal that owns the screen up? Then the furniture standing in the band is UNDER it on
          // purpose, and the "still on top where it stands" question below is the wrong one to ask —
          // #236 exempts a modal precisely because it covers everything, the band's neighbours included.
          let modal = null;
          for (const m of modals) {
            const el = page.querySelector(m);
            if (!el) continue;
            const r = el.getBoundingClientRect();
            if (r.width > 0 && r.height > 0 && getComputedStyle(el).visibility !== 'hidden') modal = m;
          }
          const PRESSABLE = 'button,[role=button],input,select,textarea,a[href],summary';
          const out = [];
          for (const el of page.querySelectorAll(PRESSABLE)) {
            const r = el.getBoundingClientRect();
            if (r.width <= 0 || r.height <= 0) continue;
            const s = getComputedStyle(el);
            if (s.visibility === 'hidden' || s.pointerEvents === 'none') continue;

            const seen = [];
            let flows = s.position === 'static' || s.position === 'relative';
            let reachedColumn = false;
            for (let n = el; n && n !== document.body; n = n.parentElement) {
              for (const f of families) {
                if (n.classList && n.classList.contains(f.slice(1)) && !seen.includes(f)) seen.push(f);
              }
              if (n.classList && n.classList.contains('map-flowcolumn')) { reachedColumn = true; break; }
              if (n !== el) {
                const ps = getComputedStyle(n).position;
                if (ps !== 'static' && ps !== 'relative') flows = false;
              }
            }

            const cx = r.left + r.width / 2, cy = r.top + r.height / 2;
            const hit = document.elementFromPoint(cx, cy);
            const onTop = !!hit && (hit === el || el.contains(hit) || hit.contains(el));

            out.push({
              t: Math.round(r.top), l: Math.round(r.left),
              w: Math.round(r.width), h: Math.round(r.height),
              inBand: r.top < sb.bottom && r.bottom > sb.top,
              ownBand: stack.contains(el),
              inFlowColumn: reachedColumn && flows,
              onTop: onTop,
              families: seen,
              cls: '.' + String(el.className || '(no class)').trim().split(/\s+/).join('.'),
              // …trimmed of a trailing LONE HIGH SURROGATE: half the desks are titled with an emoji, and
              // a slice that lands between the two halves of one is not text any JSON reader will take.
              txt: (el.innerText || el.value || el.title || '')
                     .replace(/\s+/g, ' ').trim().slice(0, 30).replace(/[\uD800-\uDBFF]$/, '')
            });
          }
          return JSON.stringify({
            band: { t: Math.round(sb.top), b: Math.round(sb.bottom), h: Math.round(sb.height) },
            modal: modal,
            controls: out
          });
        }
        """;

    /// <summary>Walks .desk-content's ancestry up to .map-flowcolumn and reports every step's position.</summary>
    private const string TheFlowProbe = """
        () => {
          const content = document.querySelector('.desk-layer:not(.d-none) > .desk-content');
          if (!content) return JSON.stringify({ found: false });
          const chain = [];
          let reached = false;
          for (let n = content; n && n !== document.body; n = n.parentElement) {
            const cls = '.' + String(n.className || '?').trim().split(/\s+/).join('.');
            chain.push({ cls: cls, pos: getComputedStyle(n).position });
            if (n.classList && n.classList.contains('map-flowcolumn')) { reached = true; break; }
          }
          const stack = document.querySelector('.map-topstack');
          const r = content.getBoundingClientRect();
          return JSON.stringify({
            found: true, reachedColumn: reached, chain: chain,
            top: Math.round(r.top),
            bandBottom: stack ? Math.round(stack.getBoundingClientRect().bottom) : -1
          });
        }
        """;

    private sealed record Band(int T, int B, int H);

    private sealed record Control(
        int T, int L, int W, int H, bool InBand, bool OwnBand, bool InFlowColumn, bool OnTop,
        string[] Families, string Cls, string Txt);

    private sealed record Reading(Band? Band, string? Modal, Control[] Controls);

    private static readonly JsonSerializerOptions Loose = new() { PropertyNameCaseInsensitive = true };

    // ── The law ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY SCREEN, EVERY PRESSABLE CONTROL: in the band only if the band is its own, if it is named
    /// furniture proven reachable there, or if it is a modal that has taken the whole window.
    /// </summary>
    [Fact]
    public async Task No_pressable_control_is_laid_out_inside_the_banners_band_on_any_screen()
    {
        string[] families =
        [
            .. TheFurnitureThatSharesTheBand.Select(f => f.Class),
            .. TheModalsThatOwnTheScreen.Select(m => m.Class),
        ];

        var offences = new List<string>();
        var rowsSeen = new HashSet<string>(StringComparer.Ordinal);
        var bands = new List<string>();
        int screens = 0, controls = 0;

        async Task Sweep(string where)
        {
            screens++;
            Reading reading = JsonSerializer.Deserialize<Reading>(
                await _page.EvaluateAsync<string>(
                    TheProbe,
                    new { families, modals = TheModalsThatOwnTheScreen.Select(m => m.Class).ToArray() }),
                Loose)!;
            Assert.True(reading.Band is { H: > 0 },
                        $"{where}: .map-topstack laid out no band at all — this gate measured nothing.");
            bands.Add($"{where}: band y {reading.Band!.T}…{reading.Band.B} ({reading.Band.H} px), "
                      + $"{reading.Controls.Length} pressable control(s)"
                      + (reading.Modal is null ? "" : $", under {reading.Modal}"));
            controls += reading.Controls.Length;

            foreach (Control c in reading.Controls)
            {
                foreach (string f in c.Families)
                {
                    rowsSeen.Add(f);
                }

                if (!c.InBand || c.OwnBand)
                {
                    continue;   // below the band, or the banner's own real estate — both allowed
                }

                string? excuse = c.Families.FirstOrDefault(
                    f => families.Contains(f, StringComparer.Ordinal));
                string what = $"{where} · '{c.Txt}' ({c.Cls}) at ({c.L},{c.T}) {c.W}×{c.H}";

                if (excuse is null)
                {
                    offences.Add(
                        $"{what} — {Math.Min(c.T + c.H, reading.Band.B) - Math.Max(c.T, reading.Band.T)} px of "
                        + "it is inside the banner's band, and it is not the band's own, not named furniture "
                        + "and not a modal. Either lay it out below the band or give it a row in "
                        + "TheFurnitureThatSharesTheBand / TheModalsThatOwnTheScreen saying why it is there.");
                }
                else if (TheFurnitureThatSharesTheBand.Any(f => f.Class == excuse)
                         && reading.Modal is null && !c.OnTop)
                {
                    // …and only while nothing modal is up. A screen under the berth picker or the BUSTED
                    // interrupt has the furniture buried ON PURPOSE — that is what "owns the screen" means,
                    // and asking the furniture to be on top there would be asking the modal not to be.
                    offences.Add(
                        $"{what} — it is excused into the band as {excuse}, and the browser does not hand it "
                        + "back at its own centre: something is on top of it there. An exemption is a claim "
                        + "that the control is still pressable, and this one is not (#236).");
                }
            }
        }

        // ── The front door FIRST, because it is only up before there is a voyage: the berth picker owns
        // the whole window, band included, and #236 exempts exactly that. Swept where it stands rather
        // than described, so its row is a fact.
        await _page.GotoAsync(_host.BaseUrl + "/", new() { Timeout = BootTimeoutMs });
        await _page.Locator("a.btn-primary[href*='scenario=sol']").ClickAsync();
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
        await _page.Locator(".start-picker-newvoyage").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await Sweep("the berth picker gate");

        // ── World one: the Sol boot at the berth, every desk, at the narrowest desktop the game is laid
        // out for. The masthead here is tabs + a one-row banner; the desks flow under whatever it is.
        await _page.Locator(".start-picker-newvoyage").ClickAsync();
        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        foreach (string tab in TheDesks)
        {
            await SitAt(tab);
            await Sweep($"Selene Gate · {tab}");
        }

        // ── The same world in a phone's portrait. The tab bar WRAPS at 390 px, so the band is taller than
        // any desktop run ever sees — the case a typed clearance is worst at and a flow column cannot get
        // wrong. (#735's viewport, for the same reason: the owner's second screen is a phone.)
        await _page.SetViewportSizeAsync(390, 700);
        foreach (string tab in TheDesks)
        {
            await SitAt(tab, dispatch: true);   // see SitAt: the strip lies over the wrapped tab bar here
            await Sweep($"390×700 · {tab}");
        }
        await _page.SetViewportSizeAsync(1280, 720);

        // ── World two: ashore at The Tilt. A different berth, a different set of chips and a different
        // desk content — #986's own lesson that one world is not a sweep.
        await BootAt("/map?dock=the-tilt&start=space-bar");
        foreach (string tab in TheDesks)
        {
            await SitAt(tab);
            await Sweep($"The Tilt · {tab}");
        }

        // ── The other modal: the BUSTED interrupt (#621's death pipeline staged through the real thing).
        // It takes the screen off every desk while it is up — #236's own named exemption, driven.
        await BootAt("/map?scenario=sol&death=impact");
        await _page.Locator(".busted-card .busted-close").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await Sweep("the BUSTED interrupt");

        // ── The premises, out loud, so this can never pass while proving nothing.
        Assert.True(screens >= 16, $"only {screens} screen(s) were swept — the matrix collapsed.");
        Assert.True(controls > 200,
                    $"only {controls} pressable control(s) were measured across {screens} screens — the probe "
                    + "is finding nothing and would pass on any layout at all.");

        // ── The list must stay complete in BOTH directions. A row nobody meets is an excuse for a surface
        // that no longer exists, and the next reader would trust it.
        string[] unmet = [.. families.Where(f => !rowsSeen.Contains(f))];
        Assert.True(unmet.Length == 0,
                    "#236 · these rows were never met on any screen this gate drives, so nothing proves they "
                    + "are still true — drive the surface or delete the row:\n  " + string.Join("\n  ", unmet)
                    + "\n\nWhat was swept:\n  " + string.Join("\n  ", bands));

        Assert.True(offences.Count == 0,
                    $"#236 · {offences.Count} pressable control(s) are laid out inside the banner's band — the "
                    + "top-centre real estate the who-flies masthead owns, which the owner ruled button-free "
                    + "on every screen (2026-07-17) because a button under a status surface is a button you "
                    + "do not have (#212):\n  " + string.Join("\n  ", offences)
                    + "\n\nWhat was swept:\n  " + string.Join("\n  ", bands));
    }

    /// <summary>
    /// #236 item 1 · A DESK CLEARS THE BAND BY FLOWING UNDER IT, NEVER BY A NUMBER.
    ///
    /// <para>Clearing the band today is not clearing it tomorrow. The law above measures where a desk IS;
    /// this one measures WHY — every ancestor from the desk's own content up to <c>.map-flowcolumn</c> is in
    /// flow, so a masthead that grows PUSHES the desk instead of covering it. That is the difference between
    /// a rule that keeps itself and a rule kept by somebody remembering to re-tune 8.25rem.</para>
    ///
    /// <para>RED PROOF: put <c>position: absolute; inset: 0</c> back on <c>.desk-layer</c> and it fails,
    /// naming the layer as the step that broke the chain AND the pixels it bought with it:</para>
    /// <code>
    /// Captain: .desk-layer computes `position: absolute`, which takes the desk OUT of the flow
    ///   column — a taller banner would then paint over it instead of pushing it (#236 item 1).
    /// Captain: .desk-content's top edge is y 92, above the band's bottom at y 111 — it is in flow
    ///   and STILL in the band.
    /// Sensors: … Trade: … Comms: …
    /// </code>
    /// </summary>
    [Fact]
    public async Task Every_desk_flows_under_the_band_rather_than_clearing_it_by_a_number()
    {
        await BootSol();

        var offences = new List<string>();
        int measured = 0;

        foreach (string tab in TheDesks)
        {
            await SitAt(tab);
            using JsonDocument doc = JsonDocument.Parse(await _page.EvaluateAsync<string>(TheFlowProbe));
            JsonElement root = doc.RootElement;
            if (!root.GetProperty("found").GetBoolean())
            {
                // Two of the seven raise no .desk-content, and both for a reason the game states elsewhere:
                // the DECK is a CANVAS (EveryDeskBootsTests' own `OwnsNoDomRoot` row says the same from the
                // other side), and NAV is the flight HUD — .map-hud, already the flow column's own child
                // since #195/#992, which is the arrangement this law is asking every other desk to join.
                // Naming them here rather than skipping quietly: an eighth screen that raised no desk
                // content would be a desk that failed to render, and this would say so.
                Assert.True(tab is "Deck" or "Nav",
                            $"{tab} raised no .desk-content — a desk that does not render is not a desk that "
                            + "clears the band.");
                continue;
            }

            measured++;
            int top = root.GetProperty("top").GetInt32();
            int bandBottom = root.GetProperty("bandBottom").GetInt32();

            if (!root.GetProperty("reachedColumn").GetBoolean())
            {
                offences.Add($"{tab}: .desk-content is not inside .map-flowcolumn at all — it is anchored to "
                             + "the page, so nothing but a typed number keeps it out of the band.");
                continue;
            }

            foreach (JsonElement step in root.GetProperty("chain").EnumerateArray())
            {
                string pos = step.GetProperty("pos").GetString()!;
                if (pos is not ("static" or "relative"))
                {
                    offences.Add($"{tab}: {step.GetProperty("cls").GetString()} computes `position: {pos}`, "
                                 + "which takes the desk OUT of the flow column — a taller banner would then "
                                 + "paint over it instead of pushing it (#236 item 1).");
                }
            }

            if (top < bandBottom)
            {
                offences.Add($"{tab}: .desk-content's top edge is y {top}, above the band's bottom at "
                             + $"y {bandBottom} — it is in flow and STILL in the band.");
            }
        }

        // Seven tabs; the Deck is a canvas and Nav is the HUD, so five raise a .desk-content to walk.
        Assert.Equal(5, measured);
        Assert.True(offences.Count == 0,
                    "#236 item 1 · a desk is clearing the banner's band by something other than flowing "
                    + "under it:\n  " + string.Join("\n  ", offences));
    }

    // ── The drive ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1021: seven, not eight — the Galley is a card raised over a desk, not a desk.</summary>
    private static readonly string[] TheDesks =
        ["Captain", "Nav", "Sensors", "War room", "Trade", "Comms", "Deck"];

    /// <summary>
    /// Sit down at a desk. <paramref name="dispatch"/> sends the click straight to the tab instead of
    /// aiming a mouse at it — needed ONLY at a phone width, and for a reason worth writing down rather
    /// than working around silently.
    ///
    /// <para><b>Found, not fixed (reported on this lane's PR).</b> At 390×700 the desk tab bar WRAPS to
    /// three rows inside <c>.map-topstack</c>, and the top-left floating control strip — <c>.map-layers</c>
    /// at <c>top: --map-control-top</c> (3.2rem), <c>.nav-search</c> beside it — is absolutely positioned
    /// across the second and third of them. Playwright reports it in as many words: <i>"button … from div
    /// class='map-layers' subtree intercepts pointer events"</i> on the Trade tab. That is #236's own bug
    /// shape between two pieces of top chrome (nothing to do with the desks this lane moved, and present
    /// on the base commit), and fixing it means the strip joining the band's column — a change to every
    /// map desk's chrome that has no business riding in a refactor. So: named, measured, left alone, and
    /// the sweep goes on measuring the layout past it rather than pretending the screen is fine.</para>
    /// </summary>
    private async Task SitAt(string tab, bool dispatch = false)
    {
        ILocator button = _page.Locator("button.desk-tab", new() { HasTextString = tab }).First;
        if (dispatch)
        {
            await button.DispatchEventAsync("click");
        }
        else
        {
            await button.ClickAsync();
        }

        await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = tab }).First.WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }

    private async Task BootSol()
    {
        await _page.GotoAsync(_host.BaseUrl + "/", new() { Timeout = BootTimeoutMs });
        await _page.Locator("a.btn-primary[href*='scenario=sol']").ClickAsync();
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
        await _page.Locator(".start-picker-backdrop").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await _page.Locator(".start-picker-newvoyage").ClickAsync();
        await _page.Locator(".desk-tab-bar").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }

    /// <summary>A dev-start URL that lands straight in a world, past the berth picker.</summary>
    private async Task BootAt(string url)
    {
        await _page.GotoAsync(_host.BaseUrl + url, new() { Timeout = BootTimeoutMs });
        await _page.WaitForSelectorAsync(".map-loading",
            new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
        await _page.Locator(".map-page").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
    }
}
