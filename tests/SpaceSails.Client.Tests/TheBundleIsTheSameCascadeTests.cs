using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 item 3 · THE STYLESHEET WAS SPLIT AND THE BROWSER CANNOT TELL.
///
/// <para><c>Map.razor.css</c> was 6,613 lines. Item 1 cut the page's markup into 76 components under
/// <c>Pages/Map/</c> and gave each of them a rules-free <c>.razor.css</c> carrying the PAGE's own CSS scope;
/// this lane moved the rules into those files. Because the scope is pinned and identical, every selector
/// compiles to the same string it compiled to before, so <b>nothing about matching changed. The only thing
/// a move can change is ORDER</b> — and for CSS, order is meaning: between two rules of equal specificity
/// that both match an element, the later one wins.</para>
///
/// <para><b>Where the order comes from.</b> The Razor SDK's <c>ConcatenateCssFiles</c> writes every
/// <c>*.rz.scp.css</c> into <c>SpaceSails.Client.styles.css</c> sorted by project-relative path, compared
/// WITHOUT REGARD TO CASE. So <c>Pages/Map.razor.css</c> comes first (<c>'.'</c> sorts under <c>'/'</c>) and
/// the 76 surfaces follow it alphabetically. A rule that moved out of the page's sheet therefore lands
/// LATER in the cascade than it was, and a rule that moved into an alphabetically earlier surface lands
/// earlier than a rule that moved into a later one. That is the entire risk surface of this refactor, and
/// it is invisible: no build error, no exception, just a colour that is now somebody else's.</para>
///
/// <para><b>What is asserted.</b> Two things, against a baseline of the cascade as it stood at
/// <c>bd6c5b4a</c> (<c>MapCascade.baseline.txt</c> — selectors and their order, nothing about values):</para>
/// <list type="number">
/// <item>every rule the stylesheet had is still in the cascade; and</item>
/// <item>for every PAIR of rules that could win or lose against each other — same specificity, at least one
/// declared property in common, and key compounds that some one element could carry at once — the pair is
/// still in the order it was in.</item>
/// </list>
///
/// <para><b>And two things the SDK could change under us,</b> both stated against the compiler rather than
/// assumed: the reader's file order is checked against the real generated bundle in <c>obj/</c>, and every
/// <c>animation:</c> is checked to be in the same file as the <c>@keyframes</c> it names.</para>
///
/// <para><b>The keyframes trap, found the hard way.</b> The scoped-CSS rewriter renames
/// <c>@keyframes pilot-banner-pulse</c> to <c>pilot-banner-pulse-b-4dqsdx4p75</c> and rewrites the
/// <c>animation:</c> shorthands that name it — <b>but only the ones in the same file</b>. The first cut of
/// this split moved <c>.ship-alert-red</c> into <c>TopBannerStack.razor.css</c> and left the keyframes
/// behind: the compiled rule came out saying <c>animation: pilot-banner-pulse …</c>, a name that resolves
/// to nothing. The alarm banner simply stopped pulsing. It built clean, and not one of the 6,000-odd tests
/// in this repo would have said a word — it was caught by diffing the generated bundle. Three keyframes
/// families (<c>pilot-banner-pulse</c>, <c>save-warming-turn</c>) had users on both sides of the cut and
/// stayed in the page's sheet with them; <see cref="EveryAnimationIsInTheFileThatDefinesItsKeyframes"/> is
/// that discovery made permanent.</para>
///
/// <para><b>Proven RED</b> three ways before it was trusted: moving <c>.oracle-backstory</c> into
/// <c>OracleCard.razor.css</c> (it must stay behind <c>.deck-offer-blurb</c>), moving <c>.ship-alert-red</c>
/// away from its keyframes, and deleting a rule. All three quoted in the PR body.</para>
/// </summary>
public sealed class TheBundleIsTheSameCascadeTests
{
    // ── THE CASCADE, PARSED ───────────────────────────────────────────────────────────────────────────

    /// <summary>One top-level rule (or at-rule) of the cascade: what it selects, what it declares, and
    /// which sheet it is written in.</summary>
    private sealed record Rule(string Selector, string Declarations, string Sheet, int Position);

    /// <summary>Every rule of the Map cascade, in bundle order — the page's sheet, then the surfaces.</summary>
    private static IReadOnlyList<Rule> Cascade()
    {
        List<Rule> all = [];
        foreach ((string sheet, string text) in MapStylesheet.Sheets())
        {
            foreach ((string selector, string declarations) in RulesOf(text))
            {
                all.Add(new Rule(selector, declarations, sheet, all.Count));
            }
        }
        return all;
    }

    /// <summary>A brace-and-comment scanner. Deliberately not a regex: this file's prose comments contain
    /// braces, selectors and whole rules, and a regex reading them as CSS is a guard that cannot tell pass
    /// from fail.</summary>
    private static IEnumerable<(string Selector, string Declarations)> RulesOf(string text)
    {
        int i = 0, start = 0;
        while (i < text.Length)
        {
            if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? text.Length : close + 2;
                continue;
            }

            if (text[i] != '{')
            {
                i++;
                continue;
            }

            int depth = 1, j = i + 1;
            while (j < text.Length && depth > 0)
            {
                if (text[j] == '/' && j + 1 < text.Length && text[j + 1] == '*')
                {
                    int close = text.IndexOf("*/", j + 2, StringComparison.Ordinal);
                    j = close < 0 ? text.Length : close + 2;
                    continue;
                }
                if (text[j] == '{') { depth++; }
                else if (text[j] == '}') { depth--; }
                j++;
            }

            yield return (Normalise(text[start..i]), Normalise(text[(i + 1)..Math.Max(i + 1, j - 1)]));
            i = j;
            start = j;
        }
    }

    /// <summary>Comments out, whitespace collapsed — the form the baseline is written in.</summary>
    private static string Normalise(string s) =>
        string.Join(' ', Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // ── THE BASELINE ──────────────────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<string> Baseline() =>
    [
        .. File.ReadAllLines(Path.Combine(RepoRoot(), "tests", "SpaceSails.Client.Tests", "MapCascade.baseline.txt"))
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
    ];

    private static string RepoRoot()
    {
        for (DirectoryInfo? at = new(AppContext.BaseDirectory); at is not null; at = at.Parent)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    // ── LAW 1 · NOTHING WAS LOST ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 item 3 · Every rule the one-file stylesheet had is still somewhere in the cascade. A move that
    /// dropped a block on the floor would leave a card unstyled and nothing else would notice.
    /// </summary>
    [Fact]
    public void EveryRuleTheStylesheetHadIsStillInTheCascade()
    {
        Dictionary<string, int> have = Cascade()
            .GroupBy(r => r.Selector, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        List<string> lost = Baseline()
            .GroupBy(s => s, StringComparer.Ordinal)
            .Where(g => !have.TryGetValue(g.Key, out int n) || n < g.Count())
            .Select(g => $"  {g.Key}  — the baseline has {g.Count()}, the cascade has " +
                         $"{(have.TryGetValue(g.Key, out int n) ? n : 0)}")
            .ToList();

        Assert.True(lost.Count == 0,
            $"#251 item 3 · {lost.Count} rule(s) the stylesheet had before the split are no longer in the\n" +
            "cascade:\n" + string.Join("\n", lost) + "\n\n" +
            "If a block was meant to move, it went to the wrong place or was dropped — put it back. If a rule\n" +
            "was DELETED on purpose, delete its line from tests/SpaceSails.Client.Tests/MapCascade.baseline.txt\n" +
            "in the same PR, so a reviewer sees the removal rather than a stylesheet quietly getting shorter.");
    }

    // ── LAW 2 · NOTHING CHANGED WHICH RULE WINS ───────────────────────────────────────────────────────

    /// <summary>
    /// #251 item 3 · THE LAW THIS LANE EXISTS FOR. Two rules only interact when they can both match one
    /// element AND declare the same property AND carry the same specificity; then, and only then, the one
    /// written later wins. So for every such pair, the pair must still be in the order the baseline had it.
    ///
    /// <para>Pairs that CANNOT interact are free to reorder, and most of them did: 126,244 of the whole
    /// bundle's 588,070 rule pairs swapped places in this split, of which the ones that could actually
    /// decide something number 230. That is not a warning — it is the measurement that says why the
    /// interacting pairs had to be counted rather than eyeballed.</para>
    /// </summary>
    [Fact]
    public void NoPairThatCouldWinOrLoseAgainstAnotherChangedPlaces()
    {
        IReadOnlyList<Rule> cascade = Cascade();
        IReadOnlyList<string> baseline = Baseline();

        // Baseline position of each rule, matching duplicate selectors up in order.
        Dictionary<string, Queue<int>> byName = new(StringComparer.Ordinal);
        for (int i = 0; i < baseline.Count; i++)
        {
            if (!byName.TryGetValue(baseline[i], out Queue<int>? q))
            {
                byName[baseline[i]] = q = new Queue<int>();
            }
            q.Enqueue(i);
        }

        List<(int Was, Rule Rule, Shape Shape)> known = [];
        foreach (Rule r in cascade)
        {
            if (byName.TryGetValue(r.Selector, out Queue<int>? q) && q.Count > 0)
            {
                known.Add((q.Dequeue(), r, ShapeOf(r)));
            }
        }

        List<string> broken = [];
        for (int a = 0; a < known.Count; a++)
        {
            for (int b = a + 1; b < known.Count; b++)
            {
                bool swapped = (known[a].Was < known[b].Was) != (known[a].Rule.Position < known[b].Rule.Position);
                if (swapped && CanFight(known[a].Shape, known[b].Shape))
                {
                    broken.Add(
                        $"  {known[a].Rule.Selector}  [{known[a].Rule.Sheet}]\n" +
                        $"  {known[b].Rule.Selector}  [{known[b].Rule.Sheet}]\n" +
                        $"    shared: {string.Join(", ", known[a].Shape.Properties.Intersect(known[b].Shape.Properties).Order(StringComparer.Ordinal))}");
                }
            }
        }

        Assert.True(broken.Count == 0,
            $"#251 item 3 · {broken.Count} pair(s) of rules that can win or lose against each other are no\n" +
            "longer in the order the cascade had them, so one of them is now painting over the other:\n" +
            string.Join("\n\n", broken.Take(20)) + "\n\n" +
            "The bundle concatenates Pages/Map.razor.css first, then Pages/Map/* alphabetically, so a rule\n" +
            "that leaves the page's sheet moves LATER and a rule that lands in an earlier-named surface moves\n" +
            "in front of one in a later-named surface. Put the earlier of each pair back in Map.razor.css, or\n" +
            "file both in the same sheet where a reader can see them together.");
    }

    // ── LAW 3 · AN ANIMATION AND ITS KEYFRAMES SHARE A FILE ───────────────────────────────────────────

    /// <summary>
    /// #251 item 3 · The scoped-CSS rewriter suffixes a <c>@keyframes</c> name with the file's scope and
    /// rewrites the <c>animation:</c> declarations that name it — <b>only within the same file</b>. Split the
    /// two apart and the reference resolves to nothing: the build stays green and the animation stops. This
    /// is the one thing about a scoped stylesheet that a pure move of rules can silently break.
    /// </summary>
    [Fact]
    public void EveryAnimationIsInTheFileThatDefinesItsKeyframes()
    {
        IReadOnlyList<Rule> cascade = Cascade();

        Dictionary<string, string> definedIn = cascade
            .Where(r => r.Selector.StartsWith("@keyframes ", StringComparison.Ordinal))
            .ToDictionary(r => r.Selector["@keyframes ".Length..].Trim(), r => r.Sheet, StringComparer.Ordinal);

        Assert.True(definedIn.Count > 5,
            $"only {definedIn.Count} @keyframes found in the Map cascade — the reader is not seeing the sheets.");

        List<string> orphaned = [];
        foreach (Rule r in cascade.Where(r => !r.Selector.StartsWith('@')))
        {
            foreach (Match m in Regex.Matches(r.Declarations, @"animation(?:-name)?\s*:([^;]*)"))
            {
                foreach (string token in Regex.Matches(m.Groups[1].Value, @"[A-Za-z][-\w]*").Select(t => t.Value))
                {
                    if (definedIn.TryGetValue(token, out string? sheet) && sheet != r.Sheet)
                    {
                        orphaned.Add($"  {r.Selector} in {r.Sheet} animates `{token}`, whose @keyframes is in {sheet}.");
                    }
                }
            }
        }

        Assert.True(orphaned.Count == 0,
            $"#251 item 3 · {orphaned.Count} animation(s) name a @keyframes that lives in another file:\n" +
            string.Join("\n", orphaned) + "\n\n" +
            "Blazor scopes keyframes per FILE: `@keyframes x` becomes `x-b-<scope>` and only the animation\n" +
            "declarations in that same file are rewritten to match. Across files the name resolves to nothing\n" +
            "and the animation never runs — with no build error and nothing else to notice it. Move the rule\n" +
            "back beside its keyframes, or move the keyframes with every rule that names it.");
    }

    // ── LAW 4 · THE READER'S ORDER IS THE BUILD'S ORDER ───────────────────────────────────────────────

    /// <summary>
    /// #251 item 3 · Every law above is stated over <see cref="MapStylesheet"/>'s idea of the cascade, which
    /// is a claim about how the SDK bundles: project-relative path, case-insensitive. That claim is not
    /// asserted here — it is CHECKED, against the bundle the compiler actually wrote. If a future SDK sorts
    /// differently, this reddens instead of the whole file quietly reasoning about a cascade nobody ships.
    /// </summary>
    [Fact]
    public void TheReaderPutsTheSheetsInTheOrderTheBuildDoes()
    {
        string bundle = TheGeneratedBundle();

        // The generated file is named for the COMPONENT — `Pages/NavHud.razor.rz.scp.css` — so the sheet it
        // was rewritten from is that name plus `.css`.
        List<string> inBundle = Regex.Matches(bundle, @"^/\* /(?<p>[^*]+?)\.rz\.scp\.css \*/$", RegexOptions.Multiline)
            .Select(m => m.Groups["p"].Value + ".css")
            .Where(p => p.StartsWith("Pages/Map.razor", StringComparison.Ordinal)
                     || p.StartsWith("Pages/Map/", StringComparison.Ordinal))
            .ToList();

        List<string> inReader = MapStylesheet.Sheets().Select(s => s.RelativePath).ToList();

        Assert.True(inBundle.Count > 60,
            $"only {inBundle.Count} Map sheet(s) found in the generated bundle — this law is reading the " +
            "wrong file, and every law in this class is then reasoning about an order nobody checked.");

        Assert.Equal(inReader, inBundle);
    }

    /// <summary>The newest <c>SpaceSails.Client.styles.css</c> the build has written. There is always one:
    /// this test project references the client, so running it has built it.</summary>
    private static string TheGeneratedBundle()
    {
        string obj = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "obj");
        string? newest = Directory.Exists(obj)
            ? Directory.EnumerateFiles(obj, "SpaceSails.Client.styles.css", SearchOption.AllDirectories)
                .Where(p => p.Replace('\\', '/').Contains("/scopedcss/bundle/", StringComparison.Ordinal))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        Assert.True(newest is not null,
            "no generated scoped-css bundle under src/SpaceSails.Client/obj. This test project references the " +
            "client, so a test run has built it and the bundle is there — if it is not, the SDK has moved it " +
            "and this law needs re-pointing rather than deleting.");

        return File.ReadAllText(newest!).Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    // ── THE WORLD THESE LAWS ARE STATED IN ────────────────────────────────────────────────────────────

    /// <summary>
    /// #251 item 3 · The fifth bug class, pre-empted: a guard whose world cannot tell pass from fail. The
    /// cascade must be a real cascade of hundreds of rules across dozens of sheets; the baseline must be the
    /// stylesheet that was there; and the "could these two fight" relation must actually find pairs — a
    /// relation that answered `false` to everything would make Law 2 pass over any partition at all.
    /// </summary>
    [Fact]
    public void THE_LAW_CanTellPassFromFail()
    {
        IReadOnlyList<Rule> cascade = Cascade();
        Assert.True(cascade.Count > 700, $"only {cascade.Count} rule(s) parsed out of the Map cascade.");
        Assert.True(cascade.Select(r => r.Sheet).Distinct(StringComparer.Ordinal).Count() > 40,
            "the cascade came out of too few sheets — the split is not being read.");
        Assert.Equal(780, Baseline().Count);

        // Named rules, not counts: a parser that silently stopped at the first prose comment containing a
        // brace would still count high.
        Assert.Contains(cascade, r => r.Selector == ".map-page" && r.Sheet == "Pages/Map.razor.css");
        Assert.Contains(cascade, r => r.Selector == ".vent-actions" && r.Sheet == "Pages/Map/VentPanel.razor.css");
        Assert.Contains(cascade, r => r.Selector == "@keyframes busted-flash" && r.Sheet == "Pages/Map/BustedCard.razor.css");

        // The relation is not vacuous in either direction.
        List<Shape> shapes = cascade.Select(ShapeOf).ToList();
        int fights = 0, cannot = 0;
        for (int a = 0; a < shapes.Count && fights < 50; a++)
        {
            for (int b = a + 1; b < shapes.Count && fights < 50; b++)
            {
                if (CanFight(shapes[a], shapes[b])) { fights++; } else { cannot++; }
            }
        }
        Assert.True(fights > 0, "no two rules in the whole cascade can fight — the relation selects nothing.");
        Assert.True(cannot > 0, "every pair of rules can fight — the relation selects everything.");

        // …and it knows a shorthand from its longhand. This exact pair is the one the #735 guard caught on
        // this lane: `overflow: hidden` on a card and `overflow-y: auto` on the family it belongs to are two
        // property NAMES and one setting, and a relation that compared names would have let them swap.
        Shape overflowShorthand = ShapeOf(new Rule(".x", "overflow: hidden;", "a", 0));
        Shape overflowLonghand = ShapeOf(new Rule(".x", "overflow-y: auto;", "a", 1));
        Assert.True(CanFight(overflowShorthand, overflowLonghand),
            "`overflow` and `overflow-y` are read as unrelated properties — the relation cannot see the bug " +
            "that made this law necessary.");
        Assert.False(CanFight(overflowShorthand, ShapeOf(new Rule(".x", "color: red;", "a", 2))),
            "`overflow` and `color` are read as the same property — the relation selects everything.");

        // The co-occurrence index is a real reading of the markup, not an empty dictionary that would make
        // every pair of distinct classes look disjoint.
        Assert.True(TheClassesAnElementCanCarry.Value.Count > 500,
            "the co-occurrence index is nearly empty — Law 2 would then call almost every pair harmless.");
        Assert.Contains("view-object", TheClassesAnElementCanCarry.Value["archive-vision"]);
    }

    // ── CAN THESE TWO RULES FIGHT? ────────────────────────────────────────────────────────────────────

    /// <summary>What a rule needs for the question "could these two ever disagree about one element".</summary>
    private sealed record Key(
        (int A, int B, int C) Specificity,
        IReadOnlySet<string> Classes,
        IReadOnlySet<string> PseudoElements,
        string? Type);

    private sealed record Shape(IReadOnlyList<Key> Selectors, IReadOnlySet<string> Properties);

    private static Shape ShapeOf(Rule r)
    {
        if (r.Selector.StartsWith('@'))
        {
            return new Shape([], new HashSet<string>(StringComparer.Ordinal));
        }

        List<Key> selectors = [];
        foreach (string one in SplitSelectorList(r.Selector))
        {
            string s = one.Replace("::deep", " ", StringComparison.Ordinal);
            string[] compounds = s.Split([' ', '\t', '>', '+', '~'], StringSplitOptions.RemoveEmptyEntries);
            string key = compounds.Length == 0 ? "" : compounds[^1];
            Match type = Regex.Match(key, @"^([a-zA-Z][-\w]*)");
            selectors.Add(new Key(
                Specificity(s),
                Names(key, @"\.([-\w]+)"),
                Names(key, @"::([-\w]+)"),
                type.Success ? type.Groups[1].Value : null));
        }

        return new Shape(selectors, DeclaredProperties(r.Declarations));
    }

    private static IReadOnlySet<string> Names(string s, string pattern) =>
        new HashSet<string>(Regex.Matches(s, pattern).Select(m => m.Groups[1].Value), StringComparer.Ordinal);

    private static (int, int, int) Specificity(string s)
    {
        int ids = Regex.Matches(s, @"#[-\w]+").Count;
        int classes = Regex.Matches(s, @"\.[-\w]+").Count
                    + Regex.Matches(s, @"\[[^\]]*\]").Count
                    + Regex.Matches(s, @"(?<!:):[-\w]+(?!\()").Count
                    + Regex.Matches(s, @"(?<!:):(?:not|is|where|has|nth-child|nth-of-type)\(").Count;
        int elements = Regex.Matches(s, @"(?:^|[\s>+~])[a-zA-Z][-\w]*").Count
                     + Regex.Matches(s, @"::[-\w]+").Count;
        return (ids, classes, elements);
    }

    private static IEnumerable<string> SplitSelectorList(string selector)
    {
        int depth = 0, start = 0;
        for (int i = 0; i < selector.Length; i++)
        {
            if (selector[i] == '(') { depth++; }
            else if (selector[i] == ')') { depth--; }
            else if (selector[i] == ',' && depth == 0)
            {
                yield return selector[start..i].Trim();
                start = i + 1;
            }
        }
        if (start < selector.Length) { yield return selector[start..].Trim(); }
    }

    /// <summary>
    /// The property FAMILIES a rule writes, not its property names. <c>overflow: hidden</c> and
    /// <c>overflow-y: auto</c> are different names and the same setting — the #735 guard is what noticed:
    /// the first cut of this split moved <c>::deep .treasure-map-card</c> (which sets <c>overflow</c>) past
    /// the family law (which sets <c>overflow-y</c>) and capped three cards shut. A shorthand and its
    /// longhands share a first segment in nearly every case, so the family is the segment before the first
    /// hyphen, plus a short table for the shorthands whose longhands are named differently.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> ShorthandFamilies =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["inset"] = ["inset", "top", "right", "bottom", "left"],
            ["inset-block"] = ["inset", "top", "bottom"],
            ["inset-inline"] = ["inset", "left", "right"],
            ["row-gap"] = ["gap"],
            ["column-gap"] = ["gap"],
            ["place-items"] = ["place", "align", "justify"],
            ["place-content"] = ["place", "align", "justify"],
            ["place-self"] = ["place", "align", "justify"],
            ["font"] = ["font", "line"],
        };

    private static IReadOnlySet<string> DeclaredProperties(string declarations)
    {
        HashSet<string> families = new(StringComparer.Ordinal);
        int depth = 0, start = 0;
        for (int i = 0; i <= declarations.Length; i++)
        {
            if (i < declarations.Length && declarations[i] == '(') { depth++; continue; }
            if (i < declarations.Length && declarations[i] == ')') { depth--; continue; }
            if (i < declarations.Length && (declarations[i] != ';' || depth != 0)) { continue; }

            string chunk = declarations[start..Math.Min(i, declarations.Length)];
            start = i + 1;

            int colon = chunk.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0) { continue; }

            string property = chunk[..colon].Trim().ToLowerInvariant();
            if (property.Length == 0) { continue; }

            if (property.StartsWith("--", StringComparison.Ordinal)) { families.Add(property); }
            else if (property == "all") { families.Add("*"); }
            else if (ShorthandFamilies.TryGetValue(property, out string[]? spread)) { families.UnionWith(spread); }
            else { families.Add(property.Split('-')[0]); }
        }
        return families;
    }

    /// <summary>
    /// Two rules can fight when some selector of each has the SAME specificity and their key compounds could
    /// both land on one element, and they declare a property in common. Anything else — different
    /// specificity, different pseudo-element, no property in common, class sets no element ever carries
    /// together — is a pair the cascade order cannot decide between, and is free to move.
    /// </summary>
    private static bool CanFight(Shape x, Shape y)
    {
        bool sharesAProperty = x.Properties.Contains("*") || y.Properties.Contains("*")
            ? x.Properties.Count > 0 && y.Properties.Count > 0     // `all:` writes every family there is
            : x.Properties.Any(y.Properties.Contains);

        if (!sharesAProperty)
        {
            return false;
        }

        foreach (Key a in x.Selectors)
        {
            foreach (Key b in y.Selectors)
            {
                if (a.Specificity == b.Specificity && CouldLandOnOneElement(a, b))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool CouldLandOnOneElement(Key a, Key b)
    {
        // ::before is not the box ::after is, and neither is the element itself.
        if (!a.PseudoElements.SetEquals(b.PseudoElements))
        {
            return false;
        }

        // An <img> is not a <span>. Two key compounds that both name a type, and different ones, are two
        // different elements whatever else they say.
        if (a.Type is not null && b.Type is not null && a.Type != b.Type)
        {
            return false;
        }

        // A key compound with no class of its own (`img`, `.a > span`) is not something we can rule out.
        if (a.Classes.Count == 0 || b.Classes.Count == 0 || a.Classes.Overlaps(b.Classes))
        {
            return true;
        }

        IReadOnlyDictionary<string, IReadOnlySet<string>> together = TheClassesAnElementCanCarry.Value;
        IReadOnlySet<string> written = TheClassNamesTheSourcesUse.Value;

        // A class no source names at all matches nothing — a dead selector cannot win or lose anything.
        if (a.Classes.Concat(b.Classes).Any(c => !written.Contains(c)))
        {
            return false;
        }

        // Otherwise: could one element carry every class of both keys at once? The markup says.
        foreach (string ca in a.Classes)
        {
            foreach (string cb in b.Classes)
            {
                if (!together.TryGetValue(ca, out IReadOnlySet<string>? with) || !with.Contains(cb))
                {
                    return false;
                }
            }
        }
        return true;
    }

    // ── WHAT THE MARKUP SAYS ABOUT WHICH CLASSES SHARE AN ELEMENT ─────────────────────────────────────

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlySet<string>>> TheClassesAnElementCanCarry =
        new(BuildCoOccurrence, isThreadSafe: true);

    private static readonly Lazy<IReadOnlySet<string>> TheClassNamesTheSourcesUse =
        new(BuildWrittenNames, isThreadSafe: true);

    private static IEnumerable<string> ClientSources() =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".razor", StringComparison.Ordinal)
                     || p.EndsWith(".cs", StringComparison.Ordinal)
                     || p.EndsWith(".html", StringComparison.Ordinal)
                     || p.EndsWith(".js", StringComparison.Ordinal))
            .Where(p => !p.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal)
                     && !p.Replace('\\', '/').Contains("/bin/", StringComparison.Ordinal));

    /// <summary>
    /// Every class name any client source writes. Read as one token sweep and used ONLY to tell a live class
    /// from a dead one: a selector whose class nothing names cannot be on an element, so it cannot fight.
    /// </summary>
    private static IReadOnlySet<string> BuildWrittenNames()
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (string path in ClientSources())
        {
            foreach (Match m in Regex.Matches(ReadLoose(path), @"[A-Za-z][A-Za-z0-9_\-]*"))
            {
                names.Add(m.Value);
            }
        }
        return names;
    }

    /// <summary>
    /// Which class names can end up on one element together. Every class on an element comes from ONE class
    /// attribute in ONE place in the source, so reading each `class=` LINE (and every string literal that
    /// lists more than one name, for the ones a method builds) as a group over-approximates the truth —
    /// which is the safe direction: a pair we wrongly believe can share is a pair Law 2 refuses to let move.
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlySet<string>> BuildCoOccurrence()
    {
        Dictionary<string, HashSet<string>> with = new(StringComparer.Ordinal);

        void Group(IEnumerable<string> tokens)
        {
            string[] group = tokens.Distinct(StringComparer.Ordinal).ToArray();
            if (group.Length < 2) { return; }
            foreach (string t in group)
            {
                if (!with.TryGetValue(t, out HashSet<string>? set))
                {
                    with[t] = set = new HashSet<string>(StringComparer.Ordinal);
                }
                set.UnionWith(group);
            }
        }

        foreach (string path in ClientSources())
        {
            string text = ReadLoose(path);
            foreach (string line in text.Split('\n'))
            {
                if (Regex.IsMatch(line, @"[Cc]lass\s*="))
                {
                    Group(Regex.Matches(line, @"[A-Za-z][A-Za-z0-9_\-]*").Select(m => m.Value));
                }
            }
            foreach (Match m in Regex.Matches(text, "\"([^\"\n]*)\""))
            {
                Group(Regex.Matches(m.Groups[1].Value, @"[A-Za-z][A-Za-z0-9_\-]*").Select(t => t.Value));
            }
        }

        return with.ToDictionary(e => e.Key, e => (IReadOnlySet<string>)e.Value, StringComparer.Ordinal);
    }

    /// <summary>Not every file in this tree is UTF-8; this sweep is about token shapes, so read it as bytes
    /// and take the ASCII. A decoder exception here would take out a law that has nothing to do with text
    /// encoding.</summary>
    private static string ReadLoose(string path) =>
        System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n", StringComparison.Ordinal);
}
