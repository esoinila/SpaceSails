namespace SpaceSails.Client.Tests;

/// <summary>
/// A RAZOR COMMENT INSIDE A START TAG IS NOT A COMMENT — IT IS AN ATTRIBUTE NAME.
///
/// <para>The bug this exists for shipped in <c>Map.razor</c> and took the Trade desk out entirely. Somebody
/// explained an affordability gate the way you explain anything in this repository — a <c>@* … *@</c> note
/// next to the line it is about — except the line it was about was an ATTRIBUTE, so the note landed BETWEEN
/// <c>&lt;button</c> and its <c>&gt;</c>:</para>
/// <code>
/// &lt;button type="button" class="btn btn-outline-warning"
///         @* #562: the affordability gate reads the CONSTANT, not a literal. *@
///         disabled="@(...)"&gt;
/// </code>
///
/// <para>Razor's parser is in ATTRIBUTE state there, not markup state. It does not strip the run; it compiles
/// it into a rendered attribute whose NAME is the comment's text, spaces, hashes, em dashes and all. At runtime
/// the renderer calls <c>setAttribute</c> with that, the browser answers
/// <c>InvalidCharacterError: Failed to execute 'setAttribute' on 'Element'</c>, and because the exception comes
/// out of the render tree it does not spoil one button — it takes down the whole page, which then shows
/// "An unhandled error has occurred. Reload." and nothing else. The owner hit exactly that twice on 2026-08-21
/// (#962, #948) and nobody could reproduce it, because reproducing it needs a captain docked at a haven with
/// bots still aboard, on the Trade desk.</para>
///
/// <para><b>Why a static law and not only a render test.</b> The compiler is happy — this is valid Razor. Every
/// unit test in the repository is happy — none of them render markup. The only witness is a real browser
/// touching that exact panel, and there are hundreds of panels. So the law is written where the mistake is
/// made: a comment goes ABOVE a tag or AFTER its <c>&gt;</c>, never between its attributes, in every
/// <c>.razor</c> file the client ships. The Trade desk's own render is proved separately by the UI gate
/// (<c>SpaceSails.UiGate.TheTradeDeskRendersTests</c>), which boots the published artifact at a berth.</para>
/// </summary>
public class TheRazorCommentIsNotAnAttributeTests
{
    /// <summary>THE LAW. No <c>@* … *@</c> anywhere between a start tag's <c>&lt;</c> and its <c>&gt;</c>.</summary>
    [Fact]
    public void No_razor_comment_sits_inside_an_element_start_tag()
    {
        string razorRoot = Path.Combine(RepoRoot, "src", "SpaceSails.Client");
        Assert.True(Directory.Exists(razorRoot), $"{razorRoot} is gone — this guard cannot see what it guards.");

        string[] files = Directory.GetFiles(razorRoot, "*.razor", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        // A guard that scanned nothing would be green forever; say so out loud.
        Assert.True(files.Length >= 2, $"only {files.Length} .razor file(s) found under {razorRoot} — the scan proved nothing");

        var offences = new List<string>();
        foreach (string path in files)
        {
            string text = File.ReadAllText(path);
            foreach ((int tagOffset, string tag, int commentOffset) in FindCommentsInStartTags(text))
            {
                offences.Add(
                    $"{Path.GetRelativePath(RepoRoot, path)}:{LineOf(text, commentOffset)}: " +
                    $"a Razor comment sits inside the <{tag}> start tag opened at line {LineOf(text, tagOffset)}");
            }
        }

        Assert.True(offences.Count == 0,
                    "A `@* … *@` between a tag's attributes is compiled into an ATTRIBUTE NAME, and setAttribute " +
                    "throws InvalidCharacterError at runtime — which kills the whole render, not just that " +
                    "control. Move the comment above the tag (or after its `>`):\n  " +
                    string.Join("\n  ", offences));
    }

    /// <summary>
    /// PROVE THE SCANNER CAN FAIL, AND PROVE IT DOES NOT CRY WOLF.
    ///
    /// <para>The first draft of this scanner reported a second, innocent button in <c>Map.razor</c>. The cause
    /// was <c>title="@($"…")"</c> — a Razor expression holding an INTERPOLATED C# STRING inside an attribute
    /// value. Read naively, the <c>$"</c> closes the attribute's own quote, the scanner falls out of the tag,
    /// misses the <c>&gt;</c>, and runs on until it meets the next honest markup comment. A guard that
    /// hallucinates offences gets weakened until it catches nothing, so the shapes that fooled it are pinned
    /// here as fixtures alongside the shape it must catch.</para>
    /// </summary>
    [Fact]
    public void The_scanner_catches_the_bad_shape_and_leaves_the_awkward_good_ones_alone()
    {
        const string bad =
            "<button type=\"button\" class=\"btn\"\n" +
            "        @* the note that became an attribute name *@\n" +
            "        disabled=\"@(x <= 0)\">Rearm</button>";
        Assert.Single(FindCommentsInStartTags(bad));

        // The good shapes, each one a thing that actually appears in this client's markup.
        string[] innocent =
        [
            // The fix: the comment above the tag it explains.
            "@* the note, where a note belongs *@\n<button type=\"button\" disabled=\"@(x <= 0)\">Rearm</button>",
            // An interpolated C# string inside an attribute value, then a real comment further down the file.
            "<button title=\"@($\"Rack {n} rounds at {Price} cr each\")\" @onclick=\"Go\">go</button>\n" +
            "@* a markup comment that is nobody's attribute *@",
            // A comment between two elements, and C# comparisons inside an attribute expression.
            "<div class=\"a\">x</div>\n@* between siblings *@\n<div class=\"@(a < b ? \"lo\" : \"hi\")\">y</div>",
            // A comment inside a razor code block, next to a generic type.
            "@code {\n    // List<string> is not a start tag\n    private List<string> _x = new();\n}\n@* tail *@",
        ];
        foreach (string sample in innocent)
        {
            Assert.Empty(FindCommentsInStartTags(sample));
        }
    }

    /// <summary>
    /// Walk the file the way the Razor parser does, in two states that matter: markup, and inside a start tag.
    /// Returns (tag offset, tag name, comment offset) for every <c>@*</c> found while inside a start tag.
    ///
    /// <para>Inside a tag, three things must be skipped rather than read: a quoted attribute value (which may
    /// itself contain <c>@(</c> holding C# strings — see the fixture test), a bare <c>@(…)</c> expression, and
    /// an already-reported comment. Outside a tag, a top-level <c>@* … *@</c> is skipped whole, so a comment
    /// that quotes markup at somebody cannot be mistaken for markup.</para>
    /// </summary>
    private static List<(int TagOffset, string Tag, int CommentOffset)> FindCommentsInStartTags(string text)
    {
        var hits = new List<(int, string, int)>();
        int i = 0;
        while (i < text.Length)
        {
            if (Is(text, i, "@*"))
            {
                i = SkipComment(text, i);
                continue;
            }
            if (text[i] != '<' || !TryReadTagName(text, i, out string tag, out int afterName))
            {
                i++;
                continue;
            }

            int tagOffset = i;
            int j = afterName;
            while (j < text.Length)
            {
                char c = text[j];
                if (c is '"' or '\'')
                {
                    j = SkipAttributeValue(text, j);
                    continue;
                }
                if (Is(text, j, "@*"))
                {
                    hits.Add((tagOffset, tag, j));
                    j = SkipComment(text, j);
                    continue;
                }
                if (c == '@' && j + 1 < text.Length && text[j + 1] == '(')
                {
                    j = SkipParenthesised(text, j + 1);
                    continue;
                }
                if (c == '>')
                {
                    j++;
                    break;
                }
                if (c == '<')
                {
                    break;      // not a well-formed start tag after all; resume the markup walk
                }
                j++;
            }
            i = Math.Max(j, afterName);
        }
        return hits;
    }

    private static bool Is(string text, int i, string what) =>
        i + what.Length <= text.Length && string.CompareOrdinal(text, i, what, 0, what.Length) == 0;

    /// <summary>A start tag is <c>&lt;</c>, a name, then whitespace, <c>/</c> or <c>&gt;</c> — never <c>a &lt; b</c>.</summary>
    private static bool TryReadTagName(string text, int i, out string tag, out int afterName)
    {
        tag = "";
        afterName = i + 1;
        int k = i + 1;
        if (k >= text.Length || !char.IsAsciiLetter(text[k]))
        {
            return false;
        }
        while (k < text.Length && (char.IsAsciiLetterOrDigit(text[k]) || text[k] is '.' or '_' or '-'))
        {
            k++;
        }
        if (k >= text.Length || !(char.IsWhiteSpace(text[k]) || text[k] is '/' or '>'))
        {
            return false;
        }
        tag = text[(i + 1)..k];
        afterName = k;
        return true;
    }

    private static int SkipComment(string text, int i)
    {
        int end = text.IndexOf("*@", i + 2, StringComparison.Ordinal);
        return end < 0 ? text.Length : end + 2;
    }

    private static int SkipAttributeValue(string text, int i)
    {
        char quote = text[i];
        int j = i + 1;
        while (j < text.Length)
        {
            if (text[j] == '@' && j + 1 < text.Length && text[j + 1] == '(')
            {
                j = SkipParenthesised(text, j + 1);
                continue;
            }
            if (text[j] == quote)
            {
                return j + 1;
            }
            j++;
        }
        return text.Length;
    }

    /// <summary>Skip a balanced <c>(…)</c>, stepping over any C# string literals inside it.</summary>
    private static int SkipParenthesised(string text, int i)
    {
        int depth = 0;
        int j = i;
        while (j < text.Length)
        {
            char c = text[j];
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return j + 1;
                }
            }
            else if (c is '"' or '\'')
            {
                j = SkipCSharpString(text, j);
                continue;
            }
            j++;
        }
        return text.Length;
    }

    private static int SkipCSharpString(string text, int i)
    {
        char quote = text[i];
        int j = i + 1;
        while (j < text.Length)
        {
            if (text[j] == '\\')
            {
                j += 2;
                continue;
            }
            if (text[j] == quote)
            {
                return j + 1;
            }
            j++;
        }
        return text.Length;
    }

    private static int LineOf(string text, int offset)
    {
        int line = 1;
        for (int i = 0; i < offset && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }
        return line;
    }

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
