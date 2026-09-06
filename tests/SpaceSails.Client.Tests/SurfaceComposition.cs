using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 item 1 · THE COMPOSITION ITSELF, SAID ONCE.
///
/// <para><see cref="MapMarkup"/> was written for <c>Map.razor</c> and one surfaces directory, because at the
/// time there was only one decomposed page in the repo. There are two now — <c>Pages/Map.razor</c> and
/// <c>Pages/Stations/TrackingPost.razor</c> — and the mechanics are identical in both: read the surface files
/// of a directory, take the block each one fences, and splice it back into the host AT ITS INVOCATION SITE so
/// the guards that read the host as TEXT go on reading the same regions, in the same order, at the same
/// indentation.</para>
///
/// <para>So the mechanics live here and the two readers are bindings of them. That is not tidiness for its own
/// sake: the three ways a composition can lie — a surface nobody invokes, a surface invoked twice, a surface
/// with no fence — are proven able to throw in <c>TheComposedPageIsThePageTests</c>, and a second copy of this
/// code would be a second chance for one of those three to be quietly missing.</para>
/// </summary>
internal static class SurfaceComposition
{
    /// <summary>Opens the sliceable region of an extracted surface. Everything between this line and
    /// <see cref="MarkupEnds"/> is the block as it stood in the host page, character for character.</summary>
    internal const string MarkupBegins = "MARKUP BEGINS";

    /// <summary>Closes the sliceable region of an extracted surface.</summary>
    internal const string MarkupEnds = "MARKUP ENDS";

    /// <summary>
    /// #1107 · A COMPONENT IS TWO FILES NOW, AND A GUARD THAT OPENS ONE OF THEM IS HALF BLIND.
    ///
    /// <para>Every <c>@code { … }</c> block in this project moved into a <c>&lt;Name&gt;.razor.cs</c> partial
    /// beside its component, because the razor generator's output is not analysed and a finding inside an
    /// <c>@code</c> block is a finding nobody is ever shown. That move is invisible to the compiler and to a
    /// player — and it is NOT invisible to the guards in this suite that read a component as TEXT and look
    /// for a method, a <c>[Parameter]</c>, a field. Four of them went red the moment the code moved, and
    /// they went red saying "0 of these exist", which is the fifth bug class one step from being shipped: a
    /// guard whose world can no longer tell pass from fail.</para>
    ///
    /// <para>So a guard stops opening a FILE and starts reading the COMPONENT: the razor, plus the
    /// code-behind beside it if there is one. That is the same text those guards were reading before the
    /// move, in the same order — the markup first, then the members — so not one of them had to change what
    /// it asserts.</para>
    /// </summary>
    internal static string ComponentText(string razorPath)
    {
        string razor = File.ReadAllText(razorPath);
        string behind = razorPath + ".cs";
        return File.Exists(behind) ? razor + "\n" + File.ReadAllText(behind) : razor;
    }

    internal static string RepoRoot()
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

    /// <summary>Every extracted surface of a directory, by component name, with its sliceable markup.</summary>
    internal static IReadOnlyList<(string Name, string Path, string Markup)> SurfacesIn(
        string surfacesDirectory, string surfacesLabel) =>
    [
        .. Directory
            .EnumerateFiles(surfacesDirectory, "*.razor", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => (Path.GetFileNameWithoutExtension(p), p, MarkupOf(p, surfacesLabel)))
    ];

    /// <summary>
    /// #251 · A SURFACE CAN BE DECOMPOSED IN ITS TURN, AND THE COMPOSITION HAS TO FOLLOW IT DOWN.
    ///
    /// <para><c>NavHud.razor</c> was the largest surface #1107 left — 1,197 lines, a page's worth of HUD
    /// living inside one of Map's own surfaces — and its markup is now cut into
    /// <c>Pages/Map/NavHud/&lt;Surface&gt;.razor</c>. Read flat, <c>NavHud.razor</c>'s fenced block would be
    /// nineteen invocation tags where a thousand lines of toolbar, readouts and flight plan used to be, and
    /// Map's composed page — which fifty-eight source guards read — would quietly lose all of it. That is the
    /// fifth bug class arriving through a door the composition already had open.</para>
    ///
    /// <para>So a surface's text is itself composed first: where a directory sits beside a surface WEARING
    /// ITS NAME (<c>Pages/Map/NavHud.razor</c> ↔ <c>Pages/Map/NavHud/</c>), that directory's surfaces are
    /// spliced into it before its own block is cut out. The recursion terminates because each step goes one
    /// directory deeper, and every way it can lie is the same three the flat splice already throws on. The
    /// consequence is the point: <b>Map's composed page does not change by a byte</b> when one of its
    /// surfaces is decomposed further, because what gets spliced in is what was always there.</para>
    /// </summary>
    internal static string RazorText(string razorPath)
    {
        string text = File.ReadAllText(razorPath);
        string nested = Path.Combine(
            Path.GetDirectoryName(razorPath)!, Path.GetFileNameWithoutExtension(razorPath));
        return Directory.Exists(nested)
            ? ComposeFrom(text, SurfacesIn(nested, LabelOf(nested) + "/"), LabelOf(razorPath))
            : text;
    }

    /// <summary>How a file under the client project names itself in an error message: its project-relative
    /// path with forward slashes, so the message reads the way the repo's own prose writes it.</summary>
    internal static string LabelOf(string path)
    {
        string client = Path.Combine(RepoRoot(), "src", "SpaceSails.Client") + Path.DirectorySeparatorChar;
        return path.StartsWith(client, StringComparison.Ordinal)
            ? path[client.Length..].Replace('\\', '/')
            : Path.GetFileName(path);
    }

    /// <summary>The block between the two sentinels of a surface file — the markup, and nothing else: not the
    /// directives above it, not the <c>@code</c> plumbing below it. Read through <see cref="RazorText"/>, so
    /// a surface decomposed in its turn hands back the whole block it was cut from rather than the
    /// invocations that replaced it.</summary>
    internal static string MarkupOf(string surfacePath, string surfacesLabel)
    {
        string[] lines = RazorText(surfacePath).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        int begins = Array.FindIndex(lines, l => l.Contains(MarkupBegins, StringComparison.Ordinal));
        int ends = Array.FindIndex(lines, l => l.Contains(MarkupEnds, StringComparison.Ordinal));
        if (begins < 0 || ends < 0 || ends <= begins)
        {
            throw new InvalidOperationException(
                $"#251 · {Path.GetFileName(surfacePath)} has no sliceable markup region. Every surface under " +
                $"{surfacesLabel} must fence its moved block with `@* ── {MarkupBegins} … *@` and " +
                $"`@* ── {MarkupEnds} ── *@`, or the guards that read the page cannot see it.");
        }

        return string.Join("\n", lines[(begins + 1)..ends]);
    }

    /// <summary>The composition itself, stated over a page text and a set of surfaces rather than over the
    /// disk — so a guard can hand it a doctored page and watch it go red.</summary>
    internal static string ComposeFrom(
        string page, IReadOnlyList<(string Name, string Path, string Markup)> surfaces, string pageLabel)
    {
        bool crlf = page.Contains("\r\n", StringComparison.Ordinal);
        List<string> lines = [.. page.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')];

        foreach ((string name, string path, string markup) in surfaces)
        {
            Splice(lines, name, path, markup, pageLabel);
        }

        string composed = string.Join("\n", lines);
        return crlf ? composed.Replace("\n", "\r\n", StringComparison.Ordinal) : composed;
    }

    /// <summary><see cref="File.ReadAllLines(string)"/>'s line-splitting semantics (either ending, no trailing
    /// empty) applied to an already-composed text.</summary>
    internal static string[] AsLines(string text)
    {
        string[] split = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return split.Length > 0 && split[^1].Length == 0 ? split[..^1] : split;
    }

    /// <summary>Replaces the one <c>&lt;Surface … /&gt;</c> element in the page with the surface's own markup.
    /// Exactly one: none and the page has lost a surface, two and the composed text would double a region and
    /// every order guard reading it would be reading a fiction.</summary>
    private static void Splice(List<string> lines, string name, string path, string markup, string pageLabel)
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
                $"#251 · {pageLabel} invokes <{name}> {opens.Count} time(s); a surface is hosted exactly " +
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
                $"#251 · the <{name}> element opened on line {start + 1} of {pageLabel} never closes.");
        }

        lines.RemoveRange(start, end - start + 1);
        lines.InsertRange(start, markup.Split('\n'));
    }
}
