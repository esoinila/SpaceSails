namespace SpaceSails.Client.Tests;

/// <summary>
/// AN UNCLOSED CSS RULE EATS EVERY RULE AFTER IT — INCLUDING THE ONES IN OTHER FILES.
///
/// <para><b>The bug this exists for (#994).</b> <c>.old-crew-reply</c> went into
/// <c>Map.razor.css</c> in #975 without its closing brace. CSS nesting is legal now, so no parser
/// complained and no build failed: the browser simply read the next 210 rules as CHILDREN of
/// <c>.old-crew-reply</c>. Blazor concatenates every scoped stylesheet into one
/// <c>SpaceSails.Client.styles.css</c>, so the damage did not stop at the file — the tail of
/// <c>Map.razor.css</c> AND the entire scoped CSS of Captain, DarkWeb, DeskChips, Galley, LocalSpace,
/// TrackingPost and WarRoom (every file that sorts after <c>Pages/Map.razor.css</c> in the bundle) went
/// dark with it. <c>.desk-chip-strip</c> computed <c>position: static</c> and the desk-chip strip was
/// invisible on every desk for weeks; the rep's pitch card rendered with the browser's default buttons.</para>
///
/// <para><b>Why nobody's brace counter caught it.</b> <c>Map.razor.css</c> had 786 <c>{</c> and 786
/// <c>}</c>. A stray <c>}</c> left after <c>@keyframes busted-flash</c> since #231 — harmless on its own,
/// because a CSS parser drops a stray close at top level — cancelled the missing one EXACTLY. A guard
/// that counts braces is green on this file. So this one does not count: it walks the file keeping a
/// STACK of open rules, and reports both halves separately, each at its own line.</para>
///
/// <para><b>The second witness.</b> Balance is not the only shape this failure takes: a nested rule can
/// be perfectly balanced and still be wrong, because none of these stylesheets nests on purpose. So the
/// second law is that a plain rule never contains another rule — which names <c>.old-crew-reply</c>
/// carrying <c>.rep-portrait</c> even in a file whose braces add up. <c>@media</c>, <c>@supports</c>,
/// <c>@container</c>, <c>@layer</c> and <c>@keyframes</c> are the at-rules that legitimately hold blocks,
/// and they are exempt by name.</para>
///
/// <para>The scanner is the quote-aware idiom of <c>TheRazorCommentIsNotAnAttributeTests</c>, moved to
/// the other language: comments, quoted strings (a <c>content: "}"</c> is not a closing brace) and
/// <c>url(…)</c> tokens are stepped over rather than read.</para>
/// </summary>
public class EveryCssRuleIsClosedTests
{
    /// <summary>
    /// THE LAW. Every rule a shipped stylesheet opens, it closes — and it closes nothing it did not open.
    /// </summary>
    [Fact]
    public void Every_scoped_stylesheet_closes_every_rule_it_opens()
    {
        string[] files = ShippedStylesheets();

        // A guard that scanned nothing would be green forever; say so out loud.
        Assert.True(files.Length >= 8,
                    $"only {files.Length} scoped stylesheet(s) found under {ClientRoot} — the scan proved nothing");

        var offences = new List<string>();
        foreach (string path in files)
        {
            string css = File.ReadAllText(path);
            CssBlocks scan = CssBlocks.Read(css);
            string name = Path.GetRelativePath(RepoRoot, path);

            foreach ((int line, string head) in scan.Unclosed)
            {
                offences.Add(
                    $"{name}:{line}: `{head}` is never closed — every rule after it in the bundle is parsed "
                    + "as one of its children and does not apply");
            }
            foreach (int line in scan.Strays)
            {
                offences.Add(
                    $"{name}:{line}: a `}}` that closes nothing — on its own the browser drops it, but it "
                    + "hides a missing brace from anything that only counts them");
            }
        }

        Assert.True(offences.Count == 0,
                    "A rule that is never closed does not fail a build and does not fail a render — it "
                    + "silently swallows every rule that follows it, in this file and in every scoped "
                    + "stylesheet after it in SpaceSails.Client.styles.css (#994):\n  "
                    + string.Join("\n  ", offences));
    }

    /// <summary>
    /// THE SECOND WITNESS. None of these stylesheets nests on purpose, so a rule inside a rule is the
    /// same accident wearing balanced braces.
    /// </summary>
    [Fact]
    public void No_plain_rule_holds_another_rule_inside_it()
    {
        var offences = new List<string>();
        foreach (string path in ShippedStylesheets())
        {
            string css = File.ReadAllText(path);
            string name = Path.GetRelativePath(RepoRoot, path);
            foreach ((int line, string head, int parentLine, string parentHead) in CssBlocks.Read(css).Nested)
            {
                offences.Add(
                    $"{name}:{line}: `{head}` is nested inside `{parentHead}` (opened at line {parentLine}) — "
                    + "nothing in this client nests on purpose, so the parent is missing its closing brace");
            }
        }

        Assert.True(offences.Count == 0,
                    "A rule is holding another rule. CSS nesting is legal, so the browser applies the child "
                    + "only to descendants of the parent — which is how a whole stylesheet goes quietly dead "
                    + "(#994):\n  " + string.Join("\n  ", offences));
    }

    /// <summary>
    /// PROVE THE SCANNER CAN FAIL, AND PROVE IT DOES NOT CRY WOLF.
    ///
    /// <para>Both halves matter. A scanner that never fires is a green number never asked of the world; a
    /// scanner that fires on <c>content: "{"</c> gets weakened until it catches nothing. The bad shapes
    /// here are #994's own two, written out; the innocent ones are shapes that actually appear in these
    /// stylesheets — a media query, a keyframes block, a brace inside a string, a brace inside a comment.</para>
    /// </summary>
    [Fact]
    public void The_scanner_names_the_two_bad_shapes_and_leaves_the_awkward_good_ones_alone()
    {
        // #994's first half: the rule that never closes. Reported at the line it was OPENED on.
        CssBlocks missing = CssBlocks.Read(".a {\n  color: red;\n\n.b {\n  color: blue;\n}\n");
        (int line, string head) = Assert.Single(missing.Unclosed);
        Assert.Equal(1, line);
        Assert.Equal(".a", head);
        // …and the same file trips the second witness too, from the child's side.
        Assert.Equal((4, ".b", 1, ".a"), Assert.Single(missing.Nested));

        // #994's second half: the stray close that made the braces add up.
        CssBlocks stray = CssBlocks.Read("@keyframes k {\n  0% { opacity: 0; }\n}\n}\n.c { color: red; }\n");
        Assert.Equal(4, Assert.Single(stray.Strays));
        Assert.Empty(stray.Unclosed);
        Assert.Empty(stray.Nested);

        // The two together, which is the file as it shipped: balanced braces, both faults still named.
        CssBlocks both = CssBlocks.Read("@keyframes k {\n  0% { opacity: 0; }\n}\n}\n.a {\n  color: red;\n\n.b { color: blue; }\n");
        Assert.Equal(5, Assert.Single(both.Unclosed).Line);
        Assert.Equal(4, Assert.Single(both.Strays));

        string[] innocent =
        [
            // A media query holding rules — the at-rules that nest on purpose.
            "@media (max-width: 640.98px) {\n  .a { color: red; }\n  .b { color: blue; }\n}\n",
            // Keyframes, whose children are percentages rather than selectors.
            "@keyframes k {\n  0% { opacity: 0.55; transform: scale(0.9); }\n  100% { opacity: 1; }\n}\n",
            // A brace inside a string is not a brace.
            ".a::before { content: \"}\"; }\n.b::after { content: '{'; }\n",
            // …including one escaped inside a string.
            ".a::before { content: \"\\\"}\"; }\n",
            // A brace inside a comment is not a brace either.
            "/* .dead { color: red; */\n.a { color: red; }\n",
            // A data URI with braces in it, unquoted, inside url().
            ".a { background: url(data:image/svg+xml,%3Csvg%3E{}%3C/svg%3E) no-repeat; }\n",
            // Nested at-rules: a media query inside a supports block.
            "@supports (display: grid) {\n  @media (min-width: 641px) {\n    .a { color: red; }\n  }\n}\n",
            // The real file's shape: a long comment block between two rules.
            ".a { color: red; }\n\n/* ══ a heading comment with { and } in it ══ */\n.b { color: blue; }\n",
        ];
        foreach (string sample in innocent)
        {
            CssBlocks scan = CssBlocks.Read(sample);
            Assert.Empty(scan.Unclosed);
            Assert.Empty(scan.Strays);
            Assert.Empty(scan.Nested);
        }
    }

    private static string[] ShippedStylesheets() =>
        Directory.GetFiles(ClientRoot, "*.razor.css", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// A CSS file read as a tree of BLOCKS rather than as a bag of braces: what it left open, what it
    /// closed without opening, and what it put inside something that does not hold rules.
    /// </summary>
    internal sealed class CssBlocks
    {
        /// <summary>Rules still open at the end of the file, each at the line it was opened on.</summary>
        public List<(int Line, string Head)> Unclosed { get; } = [];

        /// <summary>Lines carrying a `}` with nothing open to close.</summary>
        public List<int> Strays { get; } = [];

        /// <summary>Blocks opened inside a block whose head is not a nesting at-rule.</summary>
        public List<(int Line, string Head, int ParentLine, string ParentHead)> Nested { get; } = [];

        /// <summary>The at-rules whose whole job is to hold other rules.</summary>
        private static readonly string[] NestingAtRules =
            ["@media", "@supports", "@container", "@layer", "@keyframes", "@scope", "@document"];

        public static CssBlocks Read(string css)
        {
            var result = new CssBlocks();
            var open = new Stack<(int Line, string Head)>();
            int i = 0;
            int line = 1;
            int headStart = 0;

            while (i < css.Length)
            {
                char c = css[i];

                if (c == '\n')
                {
                    line++;
                    i++;
                    continue;
                }

                // A comment is not CSS. Its braces, selectors and quotes are somebody's prose.
                if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
                {
                    int end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        i = css.Length;
                        break;
                    }
                    line += CountNewlines(css, i, end + 2);
                    i = end + 2;
                    headStart = i;
                    continue;
                }

                // A quoted string is not CSS either — `content: "}"` closes nothing.
                if (c is '"' or '\'')
                {
                    int after = SkipString(css, i);
                    line += CountNewlines(css, i, after);
                    i = after;
                    continue;
                }

                // url(…) may be unquoted and may carry anything short of an unescaped ')'.
                if ((c is 'u' or 'U') && StartsWithIgnoreCase(css, i, "url("))
                {
                    int after = SkipUrl(css, i + 4);
                    line += CountNewlines(css, i, after);
                    i = after;
                    continue;
                }

                if (c == '{')
                {
                    string head = Head(css, headStart, i);
                    if (open.Count > 0)
                    {
                        (int parentLine, string parentHead) = open.Peek();
                        if (!IsNestingAtRule(parentHead))
                        {
                            result.Nested.Add((line, head, parentLine, parentHead));
                        }
                    }
                    open.Push((line, head));
                    i++;
                    headStart = i;
                    continue;
                }

                if (c == '}')
                {
                    if (open.Count == 0)
                    {
                        result.Strays.Add(line);
                    }
                    else
                    {
                        open.Pop();
                    }
                    i++;
                    headStart = i;
                    continue;
                }

                if (c == ';')
                {
                    // A declaration ends; the next rule's selector starts after it.
                    i++;
                    headStart = i;
                    continue;
                }

                i++;
            }

            // Deepest-first is how a reader would meet them; report outermost-first instead, because the
            // outermost one is the fault and the rest are its consequences.
            foreach ((int Line, string Head) block in open.Reverse())
            {
                result.Unclosed.Add(block);
            }
            return result;
        }

        private static bool IsNestingAtRule(string head) =>
            NestingAtRules.Any(at => head.StartsWith(at, StringComparison.OrdinalIgnoreCase))
            // Inside @keyframes the children are `0%`/`from`/`to`, whose own bodies are declarations.
            || head.EndsWith('%')
            || head is "from" or "to";

        /// <summary>The selector (or at-rule prelude) as one line, trimmed to something readable.</summary>
        private static string Head(string css, int start, int end)
        {
            string raw = css[Math.Min(start, end)..end];
            string flat = string.Join(' ', raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return flat.Length <= 90 ? flat : flat[..90] + "…";
        }

        private static int SkipString(string css, int i)
        {
            char quote = css[i];
            int j = i + 1;
            while (j < css.Length)
            {
                if (css[j] == '\\')
                {
                    j += 2;        // covers \" and the line continuation \<newline>
                    continue;
                }
                if (css[j] == quote)
                {
                    return j + 1;
                }
                if (css[j] == '\n')
                {
                    return j;      // an unterminated string ends at the newline; keep walking the file
                }
                j++;
            }
            return css.Length;
        }

        private static int SkipUrl(string css, int i)
        {
            int j = i;
            while (j < css.Length && char.IsWhiteSpace(css[j]))
            {
                j++;
            }
            if (j < css.Length && css[j] is '"' or '\'')
            {
                j = SkipString(css, j);     // url("…") — the quoted form; the ')' after it is plain text
            }
            while (j < css.Length)
            {
                if (css[j] == '\\')
                {
                    j += 2;
                    continue;
                }
                if (css[j] == ')')
                {
                    return j + 1;
                }
                if (css[j] == '\n')
                {
                    return j;
                }
                j++;
            }
            return css.Length;
        }

        private static bool StartsWithIgnoreCase(string css, int i, string what) =>
            i + what.Length <= css.Length
            && string.Compare(css, i, what, 0, what.Length, StringComparison.OrdinalIgnoreCase) == 0;

        private static int CountNewlines(string css, int from, int to)
        {
            int n = 0;
            for (int i = from; i < to && i < css.Length; i++)
            {
                if (css[i] == '\n')
                {
                    n++;
                }
            }
            return n;
        }
    }

    private static string ClientRoot => Path.Combine(RepoRoot, "src", "SpaceSails.Client");

    private static string RepoRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir, "src", "SpaceSails.Client")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("Could not find the repository root above the test assembly.");
        }
    }
}
