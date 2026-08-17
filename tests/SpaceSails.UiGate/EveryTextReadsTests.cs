using System.Text.Json;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

/// <summary>
/// #782 · EVERY TEXT READS — the live half, at the size the owner reads it.
///
/// <para>Owner ruling, 2026-08-08, live over the counter menu that had gone dark (#780): <i>"All text needs
/// to have good contrast from the background as a general ruling… also BIG ENOUGH FONTS — we can scroll the
/// menu."</i></para>
///
/// <para><b>Why this one has to be a browser.</b> #780's fault was not a colour anybody had typed. The menu
/// was legible in the stylesheet and dark on the screen, because a sticky sibling's box-shadow scrim was
/// painting over it. No amount of reading CSS finds that; only compositing does. And the ground that decides
/// the counter card's contrast is not in any stylesheet either — the B1 cantina hall wears a gen-AI painting
/// (<c>UndergroundComplex.CantinaHallArtUrl</c> at <c>HallArtAlpha</c>) drawn onto the deck CANVAS, behind
/// the card.</para>
///
/// <para><b>The probe is honest about where its ground came from, and says so per element.</b> For every
/// visible text node in the surface it composites the real background-colours up the ancestor chain; when
/// that stack never reaches opacity — which is exactly the case that matters — it reads the DECK CANVAS'S
/// OWN PIXELS under the element's box with <c>getImageData</c> and composites the stack over those. That is
/// a measurement of what is actually painted, not a guess: the hall photograph is same-origin, so the canvas
/// is untainted and the bytes are readable. Contrast is then WCAG 2.1 relative luminance, and the bar is AA
/// for body text (4.5:1).</para>
///
/// <para><b>Where a ratio is genuinely not computable</b> — a text run whose ground never resolves, canvas
/// or not — the guard does not invent one. It demands the shipped idiom instead: ink armour, a dark
/// <c>text-shadow</c> halo. The source-shape twin (<c>SpaceSails.Client.Tests.EveryTextReadsTests</c>) is
/// what sweeps every art slot in the game for that; this is what proves the arithmetic on the one surface
/// the owner was actually standing in front of when he filed the rule.</para>
/// </summary>
public sealed class EveryTextReadsTests : IAsyncLifetime
{
    private const float BootTimeoutMs = 180_000;

    // The same phone the tall-card gate stands on (#735/#754): a small portrait screen, chrome removed.
    private const int PhoneWidth = 390;
    private const int PhoneHeight = 700;

    /// <summary>WCAG 2.1 AA for body text. The owner's word for it is "good contrast".</summary>
    private const double MinContrast = 4.5;

    /// <summary>14 px — the readable floor at the phone's root size, and the same number the source sweep
    /// enforces in rem.</summary>
    private const double MinFontPx = 14.0;

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
            ViewportSize = new() { Width = PhoneWidth, Height = PhoneHeight },
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
        await _host.DisposeAsync();
    }

    /// <summary>
    /// THE WHOLE PHONE SCREEN AT THE COUNTER, OVER THE HALL PAINTING — the exact surface #780 was filed
    /// from. <c>?stool=1</c> puts a captain on a stool with the priced menu open, the first-ground lesson
    /// card still up and the deck's own HUD around it: a screenful of every kind of text this game has,
    /// standing on the one hall that wears a gen-AI painting.
    ///
    /// <para>RED PROOF: set <c>.bar-menu-flavor</c>'s colour to the card's own ground (or drop a font-size
    /// under 14 px) and this fails naming the row, the two colours, the ratio, and where the ground was
    /// read from.</para>
    /// </summary>
    [Fact]
    public async Task Every_word_on_the_counter_card_reads_against_what_it_actually_sits_on()
    {
        await _page.GotoAsync(_host.BaseUrl + "/map?stool=1&neighbour=1", new() { Timeout = BootTimeoutMs });
        await _page.Locator(".deck-offer-card").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
        await _page.Locator(".bar-menu").WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });

        Reading[] words = await Probe(".map-page");

        // ── The anti-vacuous half, said out loud ────────────────────────────────────────────────────────
        // A probe that sampled nothing passes every law ever written. So it reports what it saw, and both
        // premises it stands on: there is a screenful of words here, and some of them really are grounded
        // on the hall PAINTING rather than on a stylesheet. The run this was written from read 55 text
        // runs, 15 of them off the canvas's own pixels.
        Assert.True(words.Length >= 25,
            $"the probe read {words.Length} text run(s) on the phone screen — this boot puts a counter card, "
            + "a priced menu, the first-ground lesson and the deck's own HUD up at once, and it read 55 when "
            + "the guard was written. Under 25 means it is looking at the wrong element and would clear a "
            + "dark screen by seeing almost none of it.");

        int overArt = words.Count(w => w.GroundSource == "canvas");
        Assert.True(overArt >= 3,
            $"only {overArt} of the {words.Length} text runs resolved its ground against the DECK CANVAS — "
            + "every other one found an opaque CSS ancestor first. That may be true, but this gate exists to "
            + "measure words over the B1 hall PAINTING, and fifteen of them were over it when the guard was "
            + "written. Find out what changed before trusting a green run.");

        var faults = new List<string>();
        foreach (Reading w in words)
        {
            if (w.FontPx < MinFontPx)
            {
                faults.Add($"  SIZE  {w.Where} — {w.FontPx:0.#} px on a {PhoneWidth}×{PhoneHeight} phone "
                    + $"(floor {MinFontPx:0} px): \"{w.Text}\"");
            }

            if (w.GroundSource == "offscreen")
            {
                // Past the fold of a card that scrolls. Whether that is allowed is #735's question, asked
                // next door by TallCardTests; there is no pixel behind it, so there is no ratio to state.
                continue;
            }

            if (w.GroundSource == "undetermined")
            {
                if (!w.Armoured)
                {
                    faults.Add($"  GROUND {w.Where} — nothing behind it ever reaches opacity and it wears no "
                        + $"text-shadow, so what it reads like is whatever loaded: \"{w.Text}\"");
                }
                continue;
            }

            if (w.Contrast < MinContrast)
            {
                faults.Add($"  CONTRAST {w.Where} — {w.Contrast:0.00}:1 (AA needs {MinContrast:0.0}), ink "
                    + $"{w.Color} on {w.Ground} [ground read from the {w.GroundSource}]: \"{w.Text}\"");
            }
        }

        Assert.True(faults.Count == 0,
            $"the probe read {words.Length} text run(s) on the phone screen at {PhoneWidth}×{PhoneHeight} "
            + $"({overArt} of them grounded on the deck canvas's own pixels) and {faults.Count} of them "
            + "cannot be read where they render (#782):" + Environment.NewLine
            + string.Join(Environment.NewLine, faults));
    }

    /// <summary>One text run as the probe measured it.</summary>
    private sealed record Reading(
        string Where, string Text, double FontPx, string Color, string Ground,
        string GroundSource, double Contrast, bool Armoured);

    /// <summary>
    /// Read every visible text run under <paramref name="selector"/>: its size, its ink, the ground that is
    /// actually behind it, and the contrast between them.
    ///
    /// <para>The ground is built the way the browser builds it — background-colours composited up the
    /// ancestor chain, source-over — and when that stack never reaches opacity the deck canvas is sampled
    /// under the element's own box. Ancestor <c>opacity</c> is folded in as an alpha on the ink, because a
    /// half-faded panel is a half-faded panel however it got that way.</para>
    /// </summary>
    private async Task<Reading[]> Probe(string selector)
    {
        string json = await _page.EvaluateAsync<string>(ProbeScript, selector);
        return JsonSerializer.Deserialize<Reading[]>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? [];
    }

    private const string ProbeScript = """
        (selector) => {
          const root = document.querySelector(selector);
          if (!root) { return "[]"; }

          const parse = (c) => {
            const m = /rgba?\(([^)]+)\)/.exec(c || "");
            if (!m) { return null; }
            const p = m[1].split(",").map(s => parseFloat(s));
            return { r: p[0], g: p[1], b: p[2], a: p.length > 3 ? p[3] : 1 };
          };
          const over = (fg, bg) => ({
            r: fg.r * fg.a + bg.r * (1 - fg.a),
            g: fg.g * fg.a + bg.g * (1 - fg.a),
            b: fg.b * fg.a + bg.b * (1 - fg.a),
            a: 1,
          });
          const lum = (c) => {
            const ch = (v) => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };
            return 0.2126 * ch(c.r) + 0.7152 * ch(c.g) + 0.0722 * ch(c.b);
          };
          const ratio = (a, b) => {
            const la = lum(a), lb = lum(b);
            return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
          };
          const show = (c) => `rgb(${Math.round(c.r)}, ${Math.round(c.g)}, ${Math.round(c.b)})`;

          // The deck canvas, and the average of its own pixels under a box. Same-origin art, so the bytes
          // are readable; a tainted or missing canvas simply reports no sample rather than a made-up one.
          const canvas = document.querySelector("canvas.map-canvas");
          let ctx = null, box = null;
          if (canvas) {
            try { ctx = canvas.getContext("2d", { willReadFrequently: true }); } catch (e) { ctx = null; }
            box = canvas.getBoundingClientRect();
          }
          const sampleCanvas = (rect) => {
            if (!ctx || !box || box.width === 0 || box.height === 0) { return null; }
            const sx = canvas.width / box.width, sy = canvas.height / box.height;
            // Clamped to the canvas rather than abandoned at its edge: a run that hangs off the bottom of
            // the screen still has pixels behind the part you can see, and those are the ones being read.
            const x0 = Math.max(0, Math.round((rect.left - box.left) * sx));
            const y0 = Math.max(0, Math.round((rect.top - box.top) * sy));
            const x1 = Math.min(canvas.width, Math.round((rect.right - box.left) * sx));
            const y1 = Math.min(canvas.height, Math.round((rect.bottom - box.top) * sy));
            const x = x0, y = y0, w = x1 - x0, h = y1 - y0;
            if (w <= 0 || h <= 0) { return null; }
            let d;
            try { d = ctx.getImageData(x, y, w, h).data; } catch (e) { return null; }
            let r = 0, g = 0, b = 0, n = 0;
            for (let i = 0; i < d.length; i += 4) { r += d[i]; g += d[i + 1]; b += d[i + 2]; n++; }
            if (n === 0) { return null; }
            return { r: r / n, g: g / n, b: b / n, a: 1 };
          };

          const out = [];
          const walk = (el) => {
            const cs = getComputedStyle(el);
            const rect = el.getBoundingClientRect();
            // Only WORDS. A run of pure emoji (the parrot on the deck, a lone glyph on a button) is drawn
            // by the font in its own colours, so `color` says nothing about it and a ratio computed from
            // one would be arithmetic about nothing.
            const words = Array.from(el.childNodes)
              .filter(n => n.nodeType === 3 && /[\p{L}\p{N}]/u.test(n.textContent))
              .map(n => n.textContent.trim()).join(" ");
            const drawn = cs.visibility !== "hidden" && cs.display !== "none"
              && rect.width > 0 && rect.height > 0;

            if (words.length > 0 && drawn) {
              // The ink, faded by every ancestor opacity between here and the page.
              let ink = parse(cs.color) || { r: 255, g: 255, b: 255, a: 1 };
              let fade = 1;
              for (let n = el; n && n !== document.documentElement; n = n.parentElement) {
                fade *= parseFloat(getComputedStyle(n).opacity || "1");
              }
              ink = { r: ink.r, g: ink.g, b: ink.b, a: ink.a * fade };

              // The ground: background-colours composited up the chain until one of them is opaque — and
              // the chain STOPS at the deck page, because the deck canvas is a SIBLING painted between the
              // page's own fill and every overlay above it. Walking past it would ground a translucent HUD
              // strip on a colour the player never sees through it.
              const deck = document.querySelector(".map-page");
              const inkLum = lum(ink);
              // What one node paints, top layer first: a gradient sits ON its background-color, and half
              // the cards in this game are filled with a gradient — reading only background-color would
              // call an opaque card transparent and then blame the picture behind it for the contrast.
              // A gradient is many colours, so the WORST of them is used: the stop nearest the ink's own
              // luminance, which is the stop the words are hardest to read on.
              const layersOf = (n) => {
                const cs2 = getComputedStyle(n);
                const layers = [];
                const image = cs2.backgroundImage || "none";
                if (image.indexOf("gradient(") >= 0) {
                  let worst = null, worstGap = Infinity;
                  for (const m of image.matchAll(/rgba?\([^)]*\)/g)) {
                    const stop = parse(m[0]);
                    if (!stop || stop.a === 0) { continue; }
                    const gap = Math.abs(lum(stop) - inkLum);
                    if (gap < worstGap) { worstGap = gap; worst = stop; }
                  }
                  if (worst) { layers.push(worst); }
                }
                const flat = parse(cs2.backgroundColor);
                if (flat && flat.a > 0) { layers.push(flat); }
                return layers;
              };

              let acc = { r: 0, g: 0, b: 0 }, accA = 0, source = "undetermined";
              for (let n = el; n && n !== deck; n = n.parentElement) {
                for (const bg of layersOf(n)) {
                  const w = (1 - accA) * bg.a;
                  acc = { r: acc.r + bg.r * w, g: acc.g + bg.g * w, b: acc.b + bg.b * w };
                  accA += w;
                  if (accA >= 0.995) { break; }
                }
                if (accA >= 0.995) { source = "stylesheet"; break; }
              }
              if (source === "undetermined") {
                const base = sampleCanvas(rect);
                if (base) {
                  acc = {
                    r: acc.r + base.r * (1 - accA),
                    g: acc.g + base.g * (1 - accA),
                    b: acc.b + base.b * (1 - accA),
                  };
                  accA = 1;
                  source = "canvas";
                } else if (rect.bottom <= 0 || rect.top >= window.innerHeight
                           || rect.right <= 0 || rect.left >= window.innerWidth) {
                  // Past the fold of a card that scrolls (#735 owns whether that is allowed). There is no
                  // ground behind it because there is no pixel: its contrast is unmeasurable and saying so
                  // is the honest answer. Its SIZE is still a fact and is still checked.
                  source = "offscreen";
                }
              }

              const ground = { r: acc.r, g: acc.g, b: acc.b, a: 1 };
              const painted = over(ink, ground);
              out.push({
                where: (el.tagName.toLowerCase() + (el.className && typeof el.className === "string"
                  ? "." + el.className.trim().split(/\s+/).join(".") : "")).slice(0, 90),
                text: words.slice(0, 60),
                fontPx: parseFloat(cs.fontSize),
                color: show(ink) + (ink.a < 1 ? ` @${ink.a.toFixed(2)}` : ""),
                ground: source === "undetermined" ? "—" : show(ground),
                groundSource: source,
                contrast: source === "undetermined" ? 0 : Math.round(ratio(painted, ground) * 100) / 100,
                armoured: cs.textShadow !== "none" && cs.textShadow !== "",
              });
            }
            for (const kid of el.children) { walk(kid); }
          };
          walk(root);
          return JSON.stringify(out);
        }
        """;
}
