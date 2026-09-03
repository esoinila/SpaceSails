using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 item 1 · THE PAGE'S MARKUP, AS THE PAGE COMPOSES IT.
///
/// <para>Fifty-eight test classes in this suite read <c>Pages/Map.razor</c> as TEXT — the census of scrims,
/// the seated region, the satchel block, the docking panel, the art slots, the toolbar. They are the reason
/// the markup could be trusted while it all lived in one file, and they were also the thing standing in the
/// way of ever splitting it: the moment a surface moves into <c>Pages/Map/&lt;Surface&gt;.razor</c>, a guard
/// that reads one file on disk stops being able to see it, and goes red for a reason that has nothing to do
/// with the law it states.</para>
///
/// <para><b>So the guards stop reading a FILE and start reading the PAGE.</b> <see cref="Read"/> hands back
/// <c>Map.razor</c> with every extracted surface spliced back in AT ITS OWN INVOCATION SITE — the same
/// regions, in the same order, at the same indentation. Because the extraction moves markup verbatim (the
/// members a surface reads become <c>[Parameter]</c>s with the SAME NAMES, so not one character of the moved
/// block changes), the composed text is byte-for-byte the text that was in <c>Map.razor</c> before the cut.
/// That equality is the whole proof of the refactor: it was measured on the base of this lane
/// (<c>57a5804</c>) and quoted in the PR body.</para>
///
/// <para><b>Why a splice and not a concatenation.</b> Half of these guards are about ORDER — "the sky menu is
/// written last, so it paints on top", "the satchel's block is typed earlier than the arrival card's" — and
/// half slice a region out with <c>Between(text, startOfA, startOfB)</c>. Appending the surfaces to the end
/// of the file would keep every <c>Assert.Contains</c> green and silently destroy every one of those, which
/// is the fifth bug class exactly: a guard whose world can no longer tell pass from fail. Splicing at the
/// invocation site keeps paint order, adjacency and indentation all true.</para>
///
/// <para><b>It cannot go quietly blind.</b> A surface that is never invoked, or invoked twice, throws here
/// rather than being dropped from the text — see <c>TheComposedPageIsThePageTests</c>, which also proves the
/// reader red by hiding a surface from the composition.</para>
/// </summary>
internal static class MapMarkup
{
    /// <summary>The one file every guard used to open.</summary>
    private const string PageFileName = "Map.razor";

    /// <summary>Opens the sliceable region of an extracted surface. Everything between this line and
    /// <see cref="MarkupEnds"/> is the block as it stood in <c>Map.razor</c>, character for character.</summary>
    internal const string MarkupBegins = "MARKUP BEGINS";

    /// <summary>Closes the sliceable region of an extracted surface.</summary>
    internal const string MarkupEnds = "MARKUP ENDS";

    private static readonly Lazy<string> TheComposedPage = new(Compose, isThreadSafe: true);

    /// <summary>
    /// <see cref="File.ReadAllText(string)"/> for every path but one. Handed <c>Pages/Map.razor</c> it returns
    /// the COMPOSED page — the file plus every surface it hosts, spliced in where it is invoked.
    /// </summary>
    internal static string Read(string path) =>
        IsThePage(path) ? TheComposedPage.Value : File.ReadAllText(path);

    /// <summary><see cref="File.ReadAllLines(string)"/>'s twin of <see cref="Read"/>, with the same
    /// line-splitting semantics (either ending, no trailing empty).</summary>
    internal static string[] ReadLines(string path)
    {
        if (!IsThePage(path))
        {
            return File.ReadAllLines(path);
        }

        string[] split = TheComposedPage.Value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return split.Length > 0 && split[^1].Length == 0 ? split[..^1] : split;
    }

    /// <summary>The composed page, for guards that want it without a path.</summary>
    internal static string Text => TheComposedPage.Value;

    /// <summary>Where the page lives, so a guard can name it in a message.</summary>
    internal static string PagePath => Path.Combine(PagesDirectory(), PageFileName);

    /// <summary>Where the extracted surfaces live.</summary>
    internal static string SurfacesDirectory() => Path.Combine(PagesDirectory(), "Map");

    /// <summary>Every extracted surface, by component name, with its sliceable markup.</summary>
    internal static IReadOnlyList<(string Name, string Path, string Markup)> Surfaces() =>
    [
        .. Directory
            .EnumerateFiles(SurfacesDirectory(), "*.razor", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => (Path.GetFileNameWithoutExtension(p), p, MarkupOf(p)))
    ];

    private static bool IsThePage(string path) =>
        Path.GetFileName(path).Equals(PageFileName, StringComparison.Ordinal)
        && Path.GetFileName(Path.GetDirectoryName(path) ?? "").Equals("Pages", StringComparison.Ordinal);

    private static string PagesDirectory() =>
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages");

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

    /// <summary>The block between the two sentinels of a surface file — the markup, and nothing else: not the
    /// directives above it, not the <c>@code</c> plumbing below it.</summary>
    private static string MarkupOf(string surfacePath)
    {
        string[] lines = File.ReadAllText(surfacePath).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        int begins = Array.FindIndex(lines, l => l.Contains(MarkupBegins, StringComparison.Ordinal));
        int ends = Array.FindIndex(lines, l => l.Contains(MarkupEnds, StringComparison.Ordinal));
        if (begins < 0 || ends < 0 || ends <= begins)
        {
            throw new InvalidOperationException(
                $"#251 · {Path.GetFileName(surfacePath)} has no sliceable markup region. Every surface under " +
                $"Pages/Map/ must fence its moved block with `@* ── {MarkupBegins} … *@` and " +
                $"`@* ── {MarkupEnds} ── *@`, or the guards that read the page cannot see it.");
        }

        return string.Join("\n", lines[(begins + 1)..ends]);
    }

    /// <summary>Map.razor with every surface spliced in where the page invokes it.</summary>
    private static string Compose() => ComposeFrom(File.ReadAllText(PagePath), Surfaces());

    /// <summary>The composition itself, stated over a page text and a set of surfaces rather than over the
    /// disk — so the guard can hand it a doctored page and watch it go red.</summary>
    internal static string ComposeFrom(string page, IReadOnlyList<(string Name, string Path, string Markup)> surfaces)
    {
        bool crlf = page.Contains("\r\n", StringComparison.Ordinal);
        List<string> lines = [.. page.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')];

        foreach ((string name, string path, string markup) in surfaces)
        {
            Splice(lines, name, path, markup);
        }

        string composed = string.Join("\n", lines);
        return crlf ? composed.Replace("\n", "\r\n", StringComparison.Ordinal) : composed;
    }

    /// <summary>Replaces the one <c>&lt;Surface … /&gt;</c> element in the page with the surface's own markup.
    /// Exactly one: none and the page has lost a surface, two and the composed text would double a region and
    /// every order guard reading it would be reading a fiction.</summary>
    private static void Splice(List<string> lines, string name, string path, string markup)
    {
        List<int> opens = [];
        for (int i = 0; i < lines.Count; i++)
        {
            string t = lines[i].TrimStart();
            if (t.StartsWith('<') && t.Length > name.Length + 1
                && t.AsSpan(1).StartsWith(name, StringComparison.Ordinal)
                && !char.IsLetterOrDigit(t[name.Length + 1]) && t[name.Length + 1] != '_')
            {
                opens.Add(i);
            }
        }

        if (opens.Count != 1)
        {
            throw new InvalidOperationException(
                $"#251 · Pages/Map.razor invokes <{name}> {opens.Count} time(s); a surface is hosted exactly " +
                "once. Composing the page's markup out of the file plus its surfaces only means anything while " +
                $"that is true — see {Path.GetFileName(path)} and MapMarkup.");
        }

        int start = opens[0];
        int end = start;
        while (end < lines.Count && !lines[end].TrimEnd().EndsWith("/>", StringComparison.Ordinal)
                                 && !lines[end].TrimEnd().EndsWith($"</{name}>", StringComparison.Ordinal))
        {
            end++;
        }
        if (end >= lines.Count)
        {
            throw new InvalidOperationException(
                $"#251 · the <{name}> element opened on line {start + 1} of Pages/Map.razor never closes.");
        }

        lines.RemoveRange(start, end - start + 1);
        lines.InsertRange(start, markup.Split('\n'));
    }
}
