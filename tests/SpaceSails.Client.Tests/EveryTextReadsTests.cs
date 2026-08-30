using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #782 · EVERY TEXT READS — the source-shape sweep.
///
/// <para>Owner ruling, 2026-08-08 evening, live over the unreadable counter menu (#780): <i>"All text needs
/// to have good contrast from the background as a general ruling… text that cannot be read is horrible…
/// also BIG ENOUGH FONTS — we can scroll the menu."</i> Three laws in one sentence: CONTRAST against what
/// the words actually sit on, SIZE on the owner's real screens (the phone is first-class, #735/#754), and
/// the pressure valve is SCROLL — never shrink.</para>
///
/// <para><b>Why a source sweep and not only a browser probe.</b> The live half lives in
/// <c>tests/SpaceSails.UiGate/EveryTextReadsTests</c>, where a real Chromium at 390 × 700 reads the counter
/// card's computed styles and samples the deck canvas's own pixels behind them. That gate can only stand in
/// front of the surfaces it can boot. This one reads every stylesheet the client ships and every art slot
/// Map.razor opens, so a NEW picture with a NEW caption on it is caught the day it is written rather than
/// the day somebody thinks to drive to it.</para>
///
/// <para><b>What an "art surface" is, and it is three different things.</b> The sweep does not guess — it
/// enumerates each kind from the source:</para>
/// <list type="number">
///   <item><b>A CSS art ground</b> — a rule whose <c>background</c> paints <c>url('art/…')</c>. The shipped
///   idiom (<c>.galley-desk</c>, <c>.busted-collector-hail</c>) layers a darkening gradient ABOVE the
///   picture in the same shorthand, so the words never depend on which photograph loaded.</item>
///   <item><b>A razor-set art ground</b> — an element whose <c>background-image</c> is written by Map.razor
///   at render time (the treasure map, the brief, the reveal). Nothing in CSS knows what that picture looks
///   like and no ratio can be computed against it, so text laid ON one wears INK ARMOUR: a dark
///   <c>text-shadow</c> halo, which is what <c>.tm-x</c>, <c>.eb-stamp</c> and <c>.er-stamp</c> already
///   do.</item>
///   <item><b>A canvas art ground</b> — the deck canvas itself, which wears a gen-AI hall painting at
///   <c>UndergroundComplex.HallArtAlpha</c>. Every HTML panel pinned over it that paints text has to bring
///   its own ground (or its own armour), because the thing behind it is a photograph.</item>
/// </list>
///
/// <para>And the fourth law, the one #754 shipped: a panel that does not fit SCROLLS. The subtle way to
/// lose that is not to delete it — it is to declare <c>overflow: hidden</c> on a card that the #735 family
/// block later hands <c>overflow-y: auto</c>, and then move one of the two. Three cards are in exactly that
/// position today and nothing said so until now.</para>
/// </summary>
public sealed class EveryTextReadsTests
{
    /// <summary>The readable floor, in rem: 14 px at the root size, which is the smallest the owner asked
    /// to be able to read on a phone. Sizes are compared as rem because that is what the source declares.</summary>
    private const double FloorRem = 0.875;

    /// <summary>How opaque a panel's own ground has to be before we call the contrast its own business
    /// rather than the photograph's. <c>.seated-dock</c> ships at 0.86 and is the reason it is not 0.9.</summary>
    private const double GroundAlpha = 0.80;

    /// <summary>How dark an ink-armour halo has to be to count as armour.</summary>
    private const double ArmourAlpha = 0.7;

    // ── LAW 1 · A PICTURE IN CSS CARRIES ITS OWN SCRIM ──────────────────────────────────────────────────

    /// <summary>
    /// Every rule that paints <c>art/…</c> into a background layers a darkening gradient ABOVE the picture,
    /// so the copy on it is lit by the rule and not by whichever photograph the manifest last delivered.
    ///
    /// <para>RED PROOF: delete the <c>linear-gradient(…)</c> layer from <c>.galley-desk</c> (or move it
    /// after the <c>url()</c>, which is the same thing — CSS paints the FIRST layer on top) and this fails
    /// naming the rule and the file.</para>
    /// </summary>
    [Fact]
    public void EveryCssPictureIsUnderAScrimBeforeAnyWordsGoOnIt()
    {
        // The one rule that legitimately paints a picture with nothing over it, and why.
        var allowed = new Dictionary<string, string>(StringComparer.Ordinal);

        List<CssRule> art = Stylesheets()
            .SelectMany(Rules)
            .Where(r => Background(r) is { } bg && bg.Contains("url('art/", StringComparison.Ordinal))
            .ToList();

        // The anti-vacuous half: the sweep has to have SEEN the pictures it is clearing.
        //
        // #1021 · TWO BECAME ONE, and by a picture LEAVING rather than by the law weakening. `.galley-desk`
        // painted the-space-bar-desk.jpg under a 0.82/0.88 scrim — a photograph of a bar, blacked out,
        // standing in front of the actual bar — and the owner's ruling on that desk was "this UI MUST GO!…
        // it has no gen AI or visibility to the bar surroundings." The galley is a pop-up card now and the
        // room BEHIND it is what the captain sees, so the rule went out with the desk and its picture with
        // it. Lowering a floor is a deliberate edit to a written-down count, made here by the change that
        // removes the ground it counted; the law itself is untouched and still holds `.busted-collector-hail`
        // — and the moment a second CSS art ground is written, it holds that one too.
        Assert.True(art.Count >= 1,
            $"the sweep found {art.Count} CSS art ground(s) — the client ships at least one "
            + "(.busted-collector-hail), so this guard is reading the wrong files and "
            + "would pass over a picture with dark words on it.");

        var bare = new List<string>();
        foreach (CssRule rule in art)
        {
            if (allowed.ContainsKey(rule.Selector))
            {
                continue;
            }

            string bg = Background(rule)!;
            int picture = bg.IndexOf("url('art/", StringComparison.Ordinal);
            int scrim = bg.IndexOf("gradient(", StringComparison.Ordinal);
            if (scrim < 0 || scrim > picture)
            {
                bare.Add($"  {rule.File} :: {rule.Selector} — background: {Squash(bg)}");
            }
        }

        Assert.True(bare.Count == 0,
            "a picture is painted with no scrim above it, so whatever text lands on it is lit by the "
            + "photograph rather than by the rule (#782). CSS paints the FIRST background layer on top, so "
            + "the darkening gradient goes BEFORE the url():" + Environment.NewLine
            + string.Join(Environment.NewLine, bare));
    }

    // ── LAW 2 · WORDS LAID ON A RUNTIME PICTURE WEAR INK ARMOUR ─────────────────────────────────────────

    /// <summary>
    /// Map.razor writes some art grounds at render time (<c>style="background-image: …"</c> on the treasure
    /// map, the brief, the reveal). No stylesheet knows what those pictures look like, so no contrast RATIO
    /// can be computed against them — the honest law is the one the code already follows: text laid on one
    /// carries a dark <c>text-shadow</c> halo, and is at least <see cref="FloorRem"/> big.
    ///
    /// <para>RED PROOF: drop the <c>text-shadow</c> from <c>.tm-x</c>, or put <c>.eb-stamp span</c> back to
    /// <c>0.6rem</c>, and this fails naming the class and the number.</para>
    /// </summary>
    [Fact]
    public void EveryWordLaidOnARuntimePictureCarriesItsOwnHaloAndIsBigEnoughToRead()
    {
        string razor = Client("Pages/Map.razor");
        var css = Rules(Client("Pages/Map.razor.css")).ToList();

        List<(string Class, string Inner)> slots = ArtSlots(razor);
        Assert.True(slots.Count >= 5,
            $"the sweep found {slots.Count} runtime art slot(s) in Map.razor — there were five when this "
            + "guard was written (two .tm-art, two .eb-art, one .er-art), so it is reading the wrong markup.");

        var faults = new List<string>();
        int checkedWords = 0;

        foreach ((string slotClass, string inner) in slots)
        {
            foreach (string child in TextBearingChildren(inner))
            {
                checkedWords++;
                CssRule? rule = css.FirstOrDefault(r => r.Selector == "." + child);
                if (rule is null)
                {
                    faults.Add($"  .{child} (on .{slotClass}) — no rule in Map.razor.css at all, so it is "
                        + "wearing whatever it inherits over a photograph.");
                    continue;
                }

                string? halo = Declaration(rule, "text-shadow");
                if (halo is null || DarkestLayer(halo) < ArmourAlpha)
                {
                    faults.Add($"  .{child} (on .{slotClass}) — no dark text-shadow. The picture behind it "
                        + "is chosen at render time; a halo is the only thing that can make the words hold "
                        + $"against every one of them. (text-shadow: {Squash(halo ?? "—")})");
                }

                // …and every rule that sizes those words, including a descendant like `.eb-stamp span`.
                foreach (CssRule sized in css.Where(r =>
                    r.Selector == "." + child || r.Selector.StartsWith("." + child + " ", StringComparison.Ordinal)))
                {
                    double rem = Rem(sized, "font-size");
                    if (rem > 0 && rem < FloorRem)
                    {
                        faults.Add($"  {sized.Selector} (on .{slotClass}) — font-size {rem.ToString(CultureInfo.InvariantCulture)}rem "
                            + $"= {(rem * 16).ToString("0.#", CultureInfo.InvariantCulture)} px, under the "
                            + $"{FloorRem.ToString(CultureInfo.InvariantCulture)}rem (14 px) the owner asked to be able to read.");
                    }
                }
            }
        }

        Assert.True(checkedWords >= 3,
            $"the sweep found {checkedWords} text element(s) laid on those {slots.Count} pictures — there "
            + "were three (.tm-x, .eb-stamp, .er-stamp), so it is finding the slots and missing what is "
            + "written on them.");

        Assert.True(faults.Count == 0,
            "words are laid on a picture chosen at render time with nothing to hold them up (#782):"
            + Environment.NewLine + string.Join(Environment.NewLine, faults));
    }

    // ── LAW 3 · A PANEL OVER THE DECK BRINGS ITS OWN GROUND ─────────────────────────────────────────────

    /// <summary>
    /// The deck canvas wears a gen-AI hall painting (<c>UndergroundComplex.HallArtAlpha</c>), so an HTML
    /// panel pinned over it has a PHOTOGRAPH behind it, not a colour. Every positioned rule that paints
    /// text therefore declares its own ground at <see cref="GroundAlpha"/> or better — or wears ink armour,
    /// or is on the allow-list below with the reason written out.
    ///
    /// <para>RED PROOF: drop <c>.seated-dock</c>'s <c>background</c> to <c>rgba(6, 9, 15, 0.4)</c> and this
    /// fails naming the rule and the alpha.</para>
    /// </summary>
    [Fact]
    public void EveryPositionedPanelThatPaintsTextBringsItsOwnGround()
    {
        // The positioned text that legitimately has no ground and no halo of its own, and WHY.
        var allowed = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [".busted-freeze-art .bf-flash"] =
                "not words — the 6rem flash glyph of the collector's freeze-frame, a decoration inside "
                + ".busted-freeze-art's own opaque plate. There is nothing on it to read.",
            [".captain-mono"] =
                "the seeded monogram letter on the captain-card avatar disc, which is `background: "
                + "var(--mono-color, #35506a)` — an OPAQUE disc, and the monogram only shows at all when "
                + "the portrait failed to load. The deck is never behind it.",
            [".pd-scale"] =
                "the pressure dial's tick labels, inside .pd-face — whose ground is rgba(10, 14, 20, 0.95). "
                + "The dial is the panel; the scale is printed on its face and never over the deck.",
        };

        List<CssRule> positioned = Stylesheets()
            .SelectMany(Rules)
            .Where(r => Regex.IsMatch(r.Body, @"position:\s*(absolute|fixed)")
                        && Declaration(r, "color") is not null)
            .ToList();

        Assert.True(positioned.Count >= 10,
            $"the sweep found {positioned.Count} positioned rule(s) that paint text — there were ten when "
            + "this guard was written, so it is reading the wrong stylesheets.");

        var faults = new List<string>();
        foreach (CssRule rule in positioned)
        {
            if (allowed.ContainsKey(rule.Selector))
            {
                continue;
            }

            string? ground = Background(rule);
            bool hasGround = ground is not null && AlphaOf(ground) >= GroundAlpha;
            bool hasArmour = Declaration(rule, "text-shadow") is { } halo && DarkestLayer(halo) >= ArmourAlpha;
            if (!hasGround && !hasArmour)
            {
                faults.Add($"  {rule.File} :: {rule.Selector} — background: {Squash(ground ?? "none")}"
                    + $" (alpha {(ground is null ? 0 : AlphaOf(ground)).ToString("0.##", CultureInfo.InvariantCulture)}), "
                    + "no text-shadow. Pinned over the deck, its contrast is decided by whichever hall "
                    + "painting is behind it.");
            }
        }

        Assert.True(faults.Count == 0,
            "a positioned panel paints text with neither its own ground nor a halo, so what it reads like "
            + "is the canvas's business (#782):" + Environment.NewLine
            + string.Join(Environment.NewLine, faults)
            + Environment.NewLine
            + "If it genuinely sits inside another panel's ground, add it to the allow-list above WITH THE "
            + "REASON — that list is the record of every exception anybody has ever made.");
    }

    // ── LAW 4 · THE PRESSURE VALVE IS SCROLL, NEVER SHRINK ──────────────────────────────────────────────

    /// <summary>
    /// #754's law, and the quiet way to lose it. Three cards in the #735 capped-and-scrolling family
    /// (<c>.treasure-map-card</c>, <c>.expedition-brief-card</c>, <c>.expedition-reveal-card</c>) declare
    /// <c>overflow: hidden</c> for themselves, to crop the hero picture into their rounded corner. They
    /// scroll only because the family block that hands them <c>overflow-y: auto</c> comes LATER in the
    /// file: same specificity, last one wins. Move the family block up — or add an <c>overflow: hidden</c>
    /// below it — and three story cards silently stop scrolling on a phone, with nothing red anywhere.
    ///
    /// <para>RED PROOF: append <c>.treasure-map-card { overflow: hidden; }</c> to the foot of
    /// Map.razor.css and this fails naming the card and both line numbers.</para>
    /// </summary>
    [Fact]
    public void NoCardInTheScrollingFamilyIsCroppedShutAfterItWasToldToScroll()
    {
        string css = Client("Pages/Map.razor.css");

        int family = css.IndexOf(".busted-card,", StringComparison.Ordinal);
        Assert.True(family >= 0,
            "Map.razor.css no longer opens the #735 capped-and-scrolling family with `.busted-card,`, so "
            + "this guard cannot find the block that makes every modal card scroll.");

        int scrollAt = css.IndexOf("overflow-y: auto", family, StringComparison.Ordinal);
        Assert.True(scrollAt > 0,
            "the #735 family block no longer declares `overflow-y: auto` — the cards are capped with no "
            + "way to reach what is past the cap, which is #735's original softlock (#754, #782).");

        // Every member of the family, read off the block itself rather than typed in here again.
        string head = css[family..css.IndexOf('{', family)];
        // `::deep` is a legal way to open a selector in this file, and has been since M27 (Map.razor.css
        // reaches into the TrackingPost child component that way). #997 put the rep's pitch inside an
        // OverlayShell, so `.view-object` is drawn by a component now and its membership of this family
        // has to be written `::deep .view-object` or it matches nothing at all. A guard that counted only
        // the dot-first members would have quietly stopped watching that card — and gone on passing.
        string[] members = head.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.StartsWith('.') || s.StartsWith("::deep ", StringComparison.Ordinal))
            .ToArray();
        Assert.True(members.Length >= 10,
            $"the #735 family lists {members.Length} card(s) — there were ten, so this guard is reading the "
            + "wrong block.");

        var cropped = new List<string>();
        var shadowed = new List<string>();
        foreach (string member in members)
        {
            foreach (CssRule rule in Rules(css).Where(r => r.Selector == member))
            {
                if (!Regex.IsMatch(rule.Body, @"overflow(-y)?:\s*hidden"))
                {
                    continue;
                }

                cropped.Add(member);
                if (rule.At > scrollAt)
                {
                    shadowed.Add($"  {member} — declares `overflow: hidden` at char {rule.At}, AFTER the "
                        + $"family's `overflow-y: auto` at char {scrollAt}. Last one wins, so this card is "
                        + "capped shut: everything past the fold, including its way on, is unreachable on a "
                        + "short screen.");
                }
            }
        }

        // Anti-vacuous: this law is about a real collision, and there really are cards standing in it.
        Assert.True(cropped.Count >= 3,
            $"only {cropped.Count} card(s) in the family crop themselves with `overflow: hidden` — there "
            + "were three (the treasure map, the brief, the reveal), so this guard is watching a collision "
            + "that no longer happens and proves nothing. Re-read the family before trusting it.");

        Assert.True(shadowed.Count == 0,
            "a card in the #735 family is cropped shut AFTER it was told to scroll (#754/#782):"
            + Environment.NewLine + string.Join(Environment.NewLine, shadowed));

        // …and the seated strip, which is deliberately NOT in the card family (#784), keeps the same valve
        // for itself: a cap, and a scroll inside it.
        CssRule dock = Rules(css).First(r => r.Selector == ".seated-dock");
        Assert.Contains("max-height:", dock.Body, StringComparison.Ordinal);
        Assert.Contains("overflow-y: auto", dock.Body, StringComparison.Ordinal);
    }

    // ── The readers ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One CSS rule as the sweep reads it: where it came from, what it selects, what it says.</summary>
    private sealed record CssRule(string File, string Selector, string Body, int At);

    /// <summary>Every stylesheet the client itself ships — the scoped ones and the app sheet. Bootstrap and
    /// the rest of <c>wwwroot/lib</c> are not ours to style and are deliberately out.</summary>
    private static IEnumerable<string> Stylesheets()
    {
        string root = Path.Combine(RepoRoot(), "src", "SpaceSails.Client");
        foreach (string f in Directory.EnumerateFiles(root, "*.razor.css", SearchOption.AllDirectories)
                     .OrderBy(f => f, StringComparer.Ordinal))
        {
            yield return File.ReadAllText(f);
        }
        yield return File.ReadAllText(Path.Combine(root, "wwwroot", "css", "app.css"));
    }

    /// <summary>Every top-level rule in a stylesheet, comments stripped. Good enough by construction: the
    /// client's sheets are flat (no nesting), and an at-rule's inner braces simply read as more rules.</summary>
    private static IEnumerable<CssRule> Rules(string css)
    {
        string file = FileOf(css);
        string stripped = Regex.Replace(css, @"/\*.*?\*/", m => new string(' ', m.Length), RegexOptions.Singleline);
        foreach (Match m in Regex.Matches(stripped, @"([^{}]+)\{([^{}]*)\}"))
        {
            string selector = m.Groups[1].Value.Trim();
            if (selector.Length == 0 || selector.StartsWith('@'))
            {
                continue;
            }
            yield return new CssRule(file, selector, m.Groups[2].Value, m.Index);
        }
    }

    /// <summary>Which sheet a blob of CSS came from — matched on a fingerprint so the rules can name their
    /// own file in a failure without threading a path through every call.</summary>
    private static string FileOf(string css) =>
        css.Contains(".map-canvas", StringComparison.Ordinal) ? "Map.razor.css"
        // #1021 · `.galley-desk` / Galley.razor.css was here and is gone with the desk. The wire it drew
        // lives in NewsWirePanel.razor.css now, fingerprinted on the class only that sheet declares.
        : css.Contains(".news-wire-headline", StringComparison.Ordinal) ? "NewsWirePanel.razor.css"
        : css.Contains(".boot-screen", StringComparison.Ordinal) ? "app.css"
        : "a client stylesheet";

    /// <summary>A rule's <c>background</c> / <c>background-color</c> value, or null when it declares none.</summary>
    private static string? Background(CssRule rule) =>
        Declaration(rule, "background") ?? Declaration(rule, "background-color");

    /// <summary>One declaration's value, matched on a whole property name so `background` never picks up
    /// `background-position` and `color` never picks up `border-color`.</summary>
    private static string? Declaration(CssRule rule, string property)
    {
        Match m = Regex.Match(rule.Body, @"(?:^|[;{\s])" + Regex.Escape(property) + @"\s*:\s*([^;]+)");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    /// <summary>The LOWEST alpha any <c>rgba()</c> in a value declares — the worst case, which is the one
    /// that decides whether the words are the rule's business. A value with no rgba() is opaque.</summary>
    private static double AlphaOf(string value)
    {
        double worst = 1.0;
        foreach (Match m in Regex.Matches(value, @"rgba\(\s*[^)]*?,\s*([0-9.]+)\s*\)"))
        {
            if (double.TryParse(m.Groups[1].Value, CultureInfo.InvariantCulture, out double a))
            {
                worst = Math.Min(worst, a);
            }
        }
        return worst;
    }

    /// <summary>The HEAVIEST alpha any <c>rgba()</c> in a value declares. A <c>text-shadow</c> is a STACK of
    /// halos and a rule is armoured if any ONE of them is dark: <c>.tm-x</c>'s first layer is black at 0.9
    /// and its second is a red 0.6 glow, and reading the glow as the armour would condemn the strongest
    /// shadow in the file.</summary>
    private static double DarkestLayer(string value)
    {
        double best = 0;
        foreach (Match m in Regex.Matches(value, @"rgba\(\s*[^)]*?,\s*([0-9.]+)\s*\)"))
        {
            if (double.TryParse(m.Groups[1].Value, CultureInfo.InvariantCulture, out double a))
            {
                best = Math.Max(best, a);
            }
        }
        return best;
    }

    /// <summary>A rule's declared size in rem, or 0 when it declares none in rem.</summary>
    private static double Rem(CssRule rule, string property)
    {
        if (Declaration(rule, property) is not { } value)
        {
            return 0;
        }
        Match m = Regex.Match(value, @"([0-9.]+)rem");
        return m.Success && double.TryParse(m.Groups[1].Value, CultureInfo.InvariantCulture, out double v) ? v : 0;
    }

    /// <summary>Every element in Map.razor whose background picture is written at render time: its class,
    /// and the markup between its open tag and its matching close.</summary>
    private static List<(string Class, string Inner)> ArtSlots(string razor)
    {
        var slots = new List<(string, string)>();
        foreach (Match m in Regex.Matches(razor, @"<div class=""([a-z-]+)""\s+style=""background-image:"))
        {
            int open = m.Index;
            int cursor = razor.IndexOf('>', open) + 1;
            int depth = 1;
            int body = cursor;
            while (depth > 0 && cursor < razor.Length)
            {
                int next = razor.IndexOf("<div", cursor, StringComparison.Ordinal);
                int close = razor.IndexOf("</div>", cursor, StringComparison.Ordinal);
                if (close < 0)
                {
                    break;
                }
                if (next >= 0 && next < close)
                {
                    depth++;
                    cursor = next + 4;
                    continue;
                }
                depth--;
                cursor = close + 6;
            }
            slots.Add((m.Groups[1].Value, razor[body..Math.Max(body, cursor - 6)]));
        }
        return slots;
    }

    /// <summary>The classes inside an art slot that actually have words in them — a child element with a
    /// class whose content is not empty and not another element. A picture with an empty overlay div on it
    /// has nothing to read and nothing to prove.</summary>
    private static IEnumerable<string> TextBearingChildren(string inner)
    {
        foreach (Match m in Regex.Matches(inner, @"<div class=""([a-z-]+)""[^>]*>(.*?)</div>", RegexOptions.Singleline))
        {
            string content = Regex.Replace(m.Groups[2].Value, @"<[^>]*>", " ").Trim();
            if (content.Length > 0)
            {
                yield return m.Groups[1].Value;
            }
        }
    }

    /// <summary>A shipped client file, read from the repo the tests were built out of.</summary>
    private static string Client(string relative) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>A long value on one line, for a failure message that has to stay readable itself.</summary>
    private static string Squash(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim() is { Length: > 120 } s ? s[..120] + "…" : Regex.Replace(value, @"\s+", " ").Trim();

    private static string RepoRoot()
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "SpaceSails.slnx")))
            {
                return d.FullName;
            }
        }
        throw new DirectoryNotFoundException("Could not find SpaceSails.slnx above the test assembly.");
    }
}
