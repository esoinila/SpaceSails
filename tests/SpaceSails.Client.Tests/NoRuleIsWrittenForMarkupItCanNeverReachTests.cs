using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1110 · <b>A RULE THAT CAN NEVER MATCH IS THE DEAD-SELECTOR CLASS MADE WORSE.</b>
///
/// <para>#1109 split a 6,613-line stylesheet along its markup's seams and, in doing so, had to answer
/// "which surface renders this?" of every one of 780 rules. Twenty of them had no answer. Sixteen named
/// classes that appear in no source file at all — the ordinary dead selector, an editing residue that costs
/// bytes and reading time. <b>Four were worse:</b> <c>.captain-vault-actions</c>, <c>.captain-ident-row</c>,
/// <c>.captain-ident-label</c> and <c>.captain-ident-name</c> style markup that only
/// <c>Pages/Stations/Captain.razor</c> renders, but they were written in the MAP page's sheet without
/// <c>::deep</c> — so they compiled to <c>.captain-ident-row[b-4dqsdx4p75]</c> while Captain's own elements
/// wear <c>b-wa82vtva43</c>. The captain's identity line was unstyled on that desk, and everything about
/// the source looked right: a real class, a real rule, real values, in a file that really is loaded.</para>
///
/// <para><b>What this law says.</b> Blazor puts the scope attribute on the LAST compound of a selector — or,
/// where the selector uses <c>::deep</c>, on the last compound BEFORE it. So that compound is the one thing
/// in the rule that has to be markup the sheet's own scope can reach. Two clauses, and they catch the two
/// shapes above:</para>
/// <list type="number">
/// <item>every class the selector names is written by SOME client source (else the rule is wholly dead);</item>
/// <item>the SCOPED compound names at least one class written by a component in this sheet's own scope
/// (else the rule is aimed at somebody else's markup and the attribute will never line up).</item>
/// </list>
///
/// <para><b>The scope groups are read off the compiler, not assumed.</b> #251 item 1 pinned one scope across
/// <c>Pages/Map.razor</c> and its 76 surfaces under <c>Pages/Map/</c>, so "this sheet's scope" is a GROUP of
/// components and not one file. That mapping is taken from the generated <c>*.rz.scp.css</c> in
/// <c>obj/</c> — the same source of truth <c>TheBundleIsTheSameCascadeTests</c> checks its bundle order
/// against — so a change to the pin re-groups this law automatically instead of quietly invalidating it.</para>
///
/// <para><b>Reading the markup: loose for names, strict for stems.</b> A class counts as written if it is a
/// word on a <c>class=</c> line or inside any string literal of a component's own sources (its
/// <c>.razor</c>, plus every <c>partial class</c> file of the same component — this page is eighty files).
/// That over-approximates, deliberately: over-counting names can only make this law MISS a dead rule, never
/// invent one, and a guard that reddens on a live rule is deleted within a week rather than obeyed. But
/// half the state classes here are ASSEMBLED —
/// <c>class="map-plan-step map-plan-step-@_is.ToString().ToLowerInvariant()"</c> — so a word ending in a
/// hyphen is read as a STEM claiming everything built from it, and a stem is only harvested from inside a
/// <c>class</c> attribute's own quotes. Taking stems as loosely as names would let a page with
/// <c>src="art/captain-@(…).jpg"</c> beside a <c>class=</c> claim every <c>.captain-*</c> rule in the repo
/// — #1110 waved through by the guard written to catch it.</para>
///
/// <para><b>Found, on the first run:</b> fourteen more dead rules than #1109's list, in four sheets it never
/// looked at — and one entry of that list, <c>.map-plan-step-active</c>, is NOT dead: the stem reading finds
/// it on the step the autopilot is flying. It stayed.</para>
///
/// <para><b>Proven RED</b> both ways, quoted in the PR: a planted rule for a class nothing writes, and a
/// planted rule for a real class written only outside the sheet's scope — which is #1110 itself.</para>
/// </summary>
public sealed class NoRuleIsWrittenForMarkupItCanNeverReachTests
{
    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1110 · Every rule in every scoped sheet is aimed at markup its own scope can reach.</summary>
    [Fact]
    public void EveryScopedRuleNamesMarkupItsOwnScopeCanReach()
    {
        Written written = TheClassNamesTheMarkupWrites.Value;
        IReadOnlyDictionary<string, Written> byScope = TheClassesEachScopeWrites.Value;

        List<string> unreachable = [];
        foreach (Sheet sheet in Sheets())
        {
            Written inScope = byScope.TryGetValue(sheet.Scope, out Written? s)
                ? s : new Written(new HashSet<string>(StringComparer.Ordinal),
                                  new HashSet<string>(StringComparer.Ordinal));

            foreach (string selector in sheet.Selectors)
            {
                foreach (string one in SplitSelectorList(selector))
                {
                    string[] classes = ClassesIn(one);
                    if (classes.Length == 0)
                    {
                        continue;   // `img`, `:root`, `[hidden]` — nothing here claims a class
                    }

                    string[] missing = [.. classes.Where(c => !Writes(written, c))];
                    if (missing.Length > 0)
                    {
                        unreachable.Add(
                            $"  {one}\n    [{sheet.Path}]  — no client source writes " +
                            $"{string.Join(", ", missing.Select(c => "." + c))}");
                        continue;
                    }

                    string[] keyClasses = ClassesIn(ScopedCompoundOf(one));
                    if (keyClasses.Length > 0 && !keyClasses.Any(c => Writes(inScope, c)))
                    {
                        unreachable.Add(
                            $"  {one}\n    [{sheet.Path}]  — wears scope {sheet.Scope}, and " +
                            $"{string.Join(", ", keyClasses.Select(c => "." + c))} is written only OUTSIDE it");
                    }
                }
            }
        }

        Assert.True(unreachable.Count == 0,
            $"#1110 · {unreachable.Count} rule(s) are written for markup they can never land on:\n" +
            string.Join("\n", unreachable) + "\n\n" +
            "Blazor puts the scope attribute on the last compound of a selector (or the last one before\n" +
            "::deep), so a rule can only reach elements a component in ITS OWN scope renders. A rule whose\n" +
            "class nothing writes is an editing residue — delete it, and delete its line from\n" +
            "tests/SpaceSails.Client.Tests/MapCascade.baseline.txt if it is in there. A rule whose class is\n" +
            "written by another page's markup is #1110 itself: it looks right, it compiles, it loads, and the\n" +
            "desk it was written for is unstyled. Move it into that component's own sheet, or write it with\n" +
            "::deep if this page really is the host.");
    }

    /// <summary>
    /// #1110 · The fifth bug class, pre-empted. This law is a sweep over files, and a sweep that read no
    /// sheets, found no classes or grouped every component under one scope would pass over the whole repo.
    /// So the world is measured: many sheets, several distinct scopes, a Map scope that really is shared by
    /// dozens of surfaces, and — by name — the two facts #1110 turns on.
    /// </summary>
    [Fact]
    public void THE_LAW_CanTellPassFromFail()
    {
        Sheet[] sheets = [.. Sheets()];
        Assert.True(sheets.Length > 60, $"only {sheets.Length} scoped sheet(s) with rules in them were read.");
        Assert.True(sheets.Sum(s => s.Selectors.Count) > 700,
            $"only {sheets.Sum(s => s.Selectors.Count)} selectors parsed out of the client's stylesheets.");

        string[] scopes = [.. sheets.Select(s => s.Scope).Distinct(StringComparer.Ordinal)];
        Assert.True(scopes.Length > 5,
            $"every sheet came back under {scopes.Length} scope(s) — the scope reader is not reading.");

        // The pinned Map scope really is one scope over many components (#251 item 1), which is the whole
        // reason "this sheet's scope" had to be a GROUP.
        Sheet page = sheets.Single(s => s.Path == "Pages/Map.razor.css");
        Assert.True(sheets.Count(s => s.Scope == page.Scope) > 40,
            "the Map page's scope is not shared by its surfaces — the pin has gone, and this law would then " +
            "call every surface rule a scope mismatch.");
        Assert.NotEqual(page.Scope, sheets.Single(s => s.Path == "Pages/Stations/Captain.razor.css").Scope);

        // The two readings this law is built on, asserted by name rather than by count.
        IReadOnlyDictionary<string, Written> byScope = TheClassesEachScopeWrites.Value;
        Assert.True(Writes(byScope[page.Scope], "map-page"));            // the page's own root
        Assert.True(Writes(byScope[page.Scope], "ground-grows-beats"));  // a SURFACE's class, same scope
        Assert.True(Writes(byScope[page.Scope], "map-plan-step-active"), // …and one only a STEM can claim
            "the stem reading is gone: .map-plan-step-active is assembled at render time and would now be " +
            "called dead, which is the false red this law cannot survive.");

        // #251 · the same question of the SECOND decomposition, where every surface's sheet is a rules-free
        // carrier. Building the scope groups out of Sheets() alone left these components out of their own
        // scope and called four of the desk's rules unreachable; EveryScopedSheet() is what fixed it, and
        // this is the assertion that stops it silently coming back.
        Sheet desk = sheets.Single(s => s.Path == "Pages/Stations/TrackingPost.razor.css");
        Assert.True(Writes(byScope[desk.Scope], "tracking-post-card"),          // the desk's own root
            "the sensors desk's scope does not know the class its own markup writes.");
        Assert.True(Writes(byScope[desk.Scope], "sensors-opportunity-box"),     // a SURFACE's class
            "the sensors desk's scope does not include its surfaces. Their .razor.css files carry no rules, "
            + "so the compiler stamps no scope on them, and a scope group built out of sheets-with-rules "
            + "leaves every one of them out — which reads exactly like #1110 and is not.");
        Assert.False(Writes(byScope[page.Scope], "sensors-opportunity-box"),
            "the map's scope now claims a class only the sensors desk writes — the pin has drifted, or the "
            + "grouping has stopped telling two scopes apart.");

        // #1110 itself, stated as a fact about the two readings: the class IS written, and NOT in the scope
        // whose sheet used to carry the rule for it.
        Assert.True(Writes(TheClassNamesTheMarkupWrites.Value, "captain-ident-row"));
        Assert.False(Writes(byScope[page.Scope], "captain-ident-row"));

        // …and the compound reader knows where the compiler puts the attribute.
        Assert.Equal(".b", ScopedCompoundOf(".a .b").Trim());
        Assert.Equal(".a", ScopedCompoundOf(".a ::deep .b").Trim());
        Assert.Equal("", ScopedCompoundOf("::deep .b").Trim());
    }

    // ── WHAT A SHEET IS ───────────────────────────────────────────────────────────────────────────────

    private sealed record Sheet(string Path, string Scope, IReadOnlyList<string> Selectors);

    /// <summary>Every scoped stylesheet in the client that has a rule in it, with the scope the COMPILER
    /// gave it and the selectors it writes (at-rule wrappers unwrapped, keyframes left alone).</summary>
    private static IEnumerable<Sheet> Sheets() =>
        EveryScopedSheet().Select(s =>
        {
            List<string> selectors = [];
            Collect(File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", s.Path)), selectors);
            return new Sheet(s.Path, s.Scope, selectors);
        }).Where(s => s.Selectors.Count > 0);

    /// <summary>
    /// #251 · EVERY SHEET WITH A SCOPE, RULES OR NO RULES — which is a different set from
    /// <see cref="Sheets"/>, and the difference is a bug this law shipped for two days.
    ///
    /// <para>A scope is a GROUP of components, and the group is what clause 2 asks about ("is this class
    /// written by anything wearing my scope?"). Building that group out of <see cref="Sheets"/> silently
    /// leaves out every component whose sheet is a RULES-FREE CARRIER — a file that exists only to give the
    /// component its page's scope. Map's surfaces all carry rules (#1109 moved them), so the omission never
    /// showed; the tracking post's eleven surfaces carry none, and the first run of this law over them called
    /// four of the desk's own rules unreachable. The rules were fine. The law could not see the markup.</para>
    ///
    /// <para>So the scope is read from the compiler's stamp where there IS one, and from the csproj pin that
    /// PUT it there where the sheet had no rule to stamp it on. Both are the build's own answer rather than
    /// this file's — and <c>EverySurfaceWearsThePagesCssScopeTests</c> holds the pin to the page's sheet, so
    /// the two cannot drift apart without something going red.</para></summary>
    private static IEnumerable<(string Path, string Scope)> EveryScopedSheet()
    {
        string root = Path.Combine(RepoRoot(), "src", "SpaceSails.Client");
        foreach (string path in Directory.EnumerateFiles(root, "*.razor.css", SearchOption.AllDirectories)
                     .Where(p => !Loose(p).Contains("/obj/", StringComparison.Ordinal)
                              && !Loose(p).Contains("/bin/", StringComparison.Ordinal))
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            string relative = Loose(path)[(Loose(root).Length + 1)..];
            if ((ScopeOf(relative) ?? PinnedScopeOf(relative)) is { } scope)
            {
                yield return (relative, scope);
            }
        }
    }

    /// <summary>The scope the csproj pins onto a sheet, for a rules-free carrier the compiler stamped
    /// nothing on. Reads the same <c>&lt;None Update="…" CssScope="…" /&gt;</c> entries
    /// <c>EverySurfaceWearsThePagesCssScopeTests</c> holds to the page's own identifier, and understands the
    /// one wildcard shape they use (<c>Pages\Map\*.razor.css</c>).</summary>
    private static string? PinnedScopeOf(string relativeSheetPath)
    {
        foreach (Match pin in Regex.Matches(
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "SpaceSails.Client.csproj")),
            @"<None\s+Update=""([^""]+)""\s+CssScope=""([^""]+)""\s*/>"))
        {
            string pattern = pin.Groups[1].Value.Replace('\\', '/');
            string asRegex = "^" + string.Join("[^/]*", pattern.Split('*').Select(Regex.Escape)) + "$";
            if (Regex.IsMatch(relativeSheetPath, asRegex))
            {
                return pin.Groups[2].Value;
            }
        }
        return null;
    }

    /// <summary>The scope attribute the SDK actually stamped on this sheet, read out of the generated
    /// <c>*.rz.scp.css</c> beside the build. Not derived and not assumed: #251 item 1 pins one scope across
    /// a whole folder from the csproj, and a law that worked the id out for itself would be a second opinion
    /// about the one thing this file is about.</summary>
    private static string? ScopeOf(string relativeSheetPath)
    {
        string generated = relativeSheetPath[..^4] + ".rz.scp.css";   // "X.razor.css" → "X.razor.rz.scp.css"
        string obj = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "obj");
        string? newest = Directory.Exists(obj)
            ? Directory.EnumerateFiles(obj, Path.GetFileName(generated), SearchOption.AllDirectories)
                .Where(p => Loose(p).Contains("/scopedcss/", StringComparison.Ordinal)
                         && Loose(p).EndsWith("/" + generated, StringComparison.Ordinal))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        Assert.True(newest is not null,
            $"no generated scoped css for {relativeSheetPath} under src/SpaceSails.Client/obj. This test " +
            "project references the client, so a run has built it — if the SDK has moved these files this " +
            "law needs re-pointing rather than deleting.");

        Match m = Regex.Match(File.ReadAllText(newest!), @"\[(b-[a-z0-9]+)\]");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>A brace-and-comment scanner over one sheet, collecting the selector of every rule. Wrapper
    /// at-rules (<c>@media</c> and friends) are unwrapped so the rules inside them are read too;
    /// <c>@keyframes</c>, <c>@font-face</c> and <c>@property</c> are not selectors and are skipped whole.
    /// Deliberately not a regex — this repo's stylesheets carry prose comments containing braces and whole
    /// rules, and a regex reading those as CSS is a guard that cannot tell pass from fail.</summary>
    private static void Collect(string text, List<string> into)
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

            string head = Normalise(text[start..i]);
            string body = text[(i + 1)..Math.Max(i + 1, j - 1)];

            if (head.StartsWith("@media", StringComparison.Ordinal)
                || head.StartsWith("@supports", StringComparison.Ordinal)
                || head.StartsWith("@container", StringComparison.Ordinal)
                || head.StartsWith("@layer", StringComparison.Ordinal))
            {
                Collect(body, into);
            }
            else if (!head.StartsWith('@'))
            {
                into.Add(head);
            }

            i = j;
            start = j;
        }
    }

    private static string Normalise(string s) =>
        string.Join(' ', Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // ── WHERE THE COMPILER PUTS THE ATTRIBUTE ─────────────────────────────────────────────────────────

    /// <summary>The compound the scope attribute lands on: the last one before <c>::deep</c>, or the last
    /// one of the selector when there is no <c>::deep</c>. That compound — and only that compound — has to
    /// be markup this sheet's own scope renders.</summary>
    private static string ScopedCompoundOf(string selector)
    {
        int deep = selector.IndexOf("::deep", StringComparison.Ordinal);
        string upTo = deep >= 0 ? selector[..deep] : selector;
        string[] compounds = upTo.Split([' ', '\t', '>', '+', '~'], StringSplitOptions.RemoveEmptyEntries);
        return compounds.Length == 0 ? "" : compounds[^1];
    }

    private static string[] ClassesIn(string selector) =>
        [.. Regex.Matches(selector, @"\.([A-Za-z][-\w]*)").Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)];

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

    // ── WHAT THE MARKUP WRITES, AND UNDER WHICH SCOPE ─────────────────────────────────────────────────

    private static readonly Lazy<IReadOnlyDictionary<string, Written>> TheClassesEachScopeWrites =
        new(BuildByScope, isThreadSafe: true);

    /// <summary>Every class name ANY client source writes, whatever scope it belongs to — clause 1's world.
    /// A class written only in <c>index.html</c>, in a <c>.razor</c> with no sheet beside it, or in the
    /// renderer's own JS is still a class the browser puts on an element.</summary>
    private static readonly Lazy<Written> TheClassNamesTheMarkupWrites =
        new(() => ClassesWrittenBy(
                ClientFiles(".razor").Concat(ClientFiles(".cs"))
                    .Concat(ClientFiles(".html")).Concat(ClientFiles(".js"))),
            isThreadSafe: true);

    /// <summary>Scope id → every class name the components wearing that scope write. A component's sources
    /// are its own <c>.razor</c> and every <c>partial class</c> file of the same type — the Map page is one
    /// component and eighty files, and a law that read only the <c>.razor</c> would call most of its own
    /// classes unwritten.</summary>
    private static IReadOnlyDictionary<string, Written> BuildByScope()
    {
        Dictionary<string, (HashSet<string> Names, HashSet<string> Stems)> byScope = new(StringComparer.Ordinal);
        foreach ((string path, string scope) in EveryScopedSheet())
        {
            string razor = Path.Combine(
                RepoRoot(), "src", "SpaceSails.Client", path[..^4]);   // "X.razor.css" → "X.razor"
            if (!File.Exists(razor))
            {
                continue;
            }

            if (!byScope.TryGetValue(scope, out (HashSet<string> Names, HashSet<string> Stems) set))
            {
                byScope[scope] = set =
                    (new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
            }

            Written mine = ClassesWrittenBy(SourcesOfTheComponent(razor));
            set.Names.UnionWith(mine.Names);
            set.Stems.UnionWith(mine.Stems);
        }
        return byScope.ToDictionary(
            e => e.Key, e => new Written(e.Value.Names, e.Value.Stems), StringComparer.Ordinal);
    }

    /// <summary>A component's own files: the markup, plus every C# file that continues its class.</summary>
    private static IEnumerable<string> SourcesOfTheComponent(string razorPath)
    {
        yield return razorPath;

        string name = Path.GetFileNameWithoutExtension(razorPath);   // "Map.razor" → "Map"
        var partial = new Regex($@"\bpartial class {Regex.Escape(name)}\b");
        foreach (string cs in ClientFiles(".cs"))
        {
            if (partial.IsMatch(ReadLoose(cs)))
            {
                yield return cs;
            }
        }
    }

    private static IEnumerable<string> ClientFiles(string extension) =>
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*" + extension, SearchOption.AllDirectories)
            .Where(p => !Loose(p).Contains("/obj/", StringComparison.Ordinal)
                     && !Loose(p).Contains("/bin/", StringComparison.Ordinal));

    /// <summary>
    /// What a set of sources writes: the whole names, and the STEMS that finish into names at render time.
    /// </summary>
    private sealed record Written(IReadOnlySet<string> Names, IReadOnlySet<string> Stems);

    /// <summary>
    /// Every class name these sources write. Two readings, and the difference between them is the whole
    /// reason this law can tell a live rule from a dead one.
    ///
    /// <para><b>NAMES are read loosely</b> — every word on a line that carries a <c>class=</c>, and every
    /// word of every string literal, which is the same reading #1109's co-occurrence index uses. A class
    /// can be assembled anywhere (a switch in a partial, a ternary inside the attribute, a constant), and
    /// over-counting names can only make this law MISS a dead rule, never invent one.</para>
    ///
    /// <para><b>STEMS are read strictly</b>, from inside a <c>class</c> attribute's own quotes and nowhere
    /// else. A stem claims everything that starts with it, so a loose one is a false GREEN with a blast
    /// radius: a page writing <c>src="art/captain-@(…).jpg"</c> next to a <c>class=</c> would hand its own
    /// scope the stem <c>captain-</c> and go on to claim every <c>.captain-*</c> rule in the repo — which is
    /// precisely the mismatch #1110 is about, waved through by the guard written to catch it.</para></summary>
    private static Written ClassesWrittenBy(IEnumerable<string> paths)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        HashSet<string> stems = new(StringComparer.Ordinal);

        foreach (string path in paths)
        {
            string text = ReadLoose(path);

            // Line by line, never over the whole file: one unbalanced quote anywhere would otherwise
            // re-pair every literal below it and the sweep would be reading the gaps between strings.
            foreach (string line in text.Split('\n'))
            {
                if (Regex.IsMatch(line, @"[Cc]lass\s*="))
                {
                    foreach (Match m in Regex.Matches(line, @"[A-Za-z][A-Za-z0-9_\-]*"))
                    {
                        names.Add(m.Value);
                    }
                }

                foreach (Match lit in Regex.Matches(line, "\"([^\"]*)\""))
                {
                    foreach (Match m in Regex.Matches(lit.Groups[1].Value, @"[A-Za-z][A-Za-z0-9_\-]*"))
                    {
                        names.Add(m.Value);
                    }
                }

                foreach (Match attribute in Regex.Matches(line, @"[Cc]lass\s*=\s*(""[^""]*""|'[^']*')"))
                {
                    foreach (Match m in Regex.Matches(attribute.Groups[1].Value, @"[A-Za-z][A-Za-z0-9_\-]*"))
                    {
                        if (m.Value[^1] == '-')
                        {
                            stems.Add(m.Value);
                        }
                    }
                }
            }
        }
        return new Written(names, stems);
    }

    /// <summary>
    /// Does this markup claim <paramref name="cls"/>?
    ///
    /// <para>By name, or <b>by stem</b>. Half the state classes in this app are assembled at render time —
    /// <c>class="crew-band crew-band-@CrewTemp.BandOf(r.Score)…"</c>,
    /// <c>class="captain-card captain-quest-@q.StatusKind"</c>,
    /// <c>class="map-plan-step map-plan-step-@_is.ToString().ToLowerInvariant()"</c> — and what the source
    /// contains is the stem <c>crew-band-</c>, never the five names it becomes. A reader that demanded the
    /// whole name called eleven live rules dead on the first run of this law, and one of them,
    /// <c>.map-plan-step-active</c>, was on #1109's own found-not-fixed list as dead. It is not: the
    /// autopilot wears it while it flies an approach.</para></summary>
    private static bool Writes(Written written, string cls)
    {
        if (written.Names.Contains(cls))
        {
            return true;
        }
        for (int i = 1; i < cls.Length; i++)
        {
            if (cls[i - 1] == '-' && written.Stems.Contains(cls[..i]))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Not every file in this tree is UTF-8; this sweep is about token shapes, so read the bytes
    /// and take the ASCII rather than let a decoder take out a law about stylesheets.</summary>
    private static string ReadLoose(string path) =>
        System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Loose(string path) => path.Replace('\\', '/');

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
}
