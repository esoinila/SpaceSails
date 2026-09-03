using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 item 3 · THE MAP PAGE'S CASCADE, AS THE BROWSER RECEIVES IT.
///
/// <para>Thirty-odd test classes in this suite read <c>Pages/Map.razor.css</c> as TEXT — the z-band sweep,
/// the capped-card family, the readable-contrast pass, the "does this class have a rule at all" guards.
/// They are the reason the stylesheet could be trusted while it all lived in one file, and they were the
/// same wall in front of splitting it that the fifty-eight markup guards were in front of splitting
/// <c>Map.razor</c>: the moment a rule moves into <c>Pages/Map/&lt;Surface&gt;.razor.css</c>, a guard that
/// opens one file on disk stops being able to see it and goes red for a reason that has nothing to do with
/// the law it states.</para>
///
/// <para><b>So the guards stop reading a FILE and start reading the CASCADE.</b> <see cref="Text"/> is
/// <c>Map.razor.css</c> followed by every surface sheet, concatenated <b>in the order the build bundles
/// them</b> — which is exactly the order the browser applies them in. That is not a convenience: for CSS,
/// order IS meaning, and a reader that concatenated the surfaces in any other order would be showing these
/// guards a cascade the browser never sees.</para>
///
/// <para><b>The order, established against the compiler rather than assumed.</b> The Razor SDK's
/// <c>ConcatenateCssFiles</c> emits every <c>*.rz.scp.css</c> sorted by its project-relative path, compared
/// WITHOUT REGARD TO CASE — so <c>Pages/Map.razor.css</c> lands before <c>Pages/Map/…</c> (<c>'.'</c> sorts
/// under <c>'/'</c>) and the surfaces follow one another alphabetically. <c>TheBundleIsTheSameCascadeTests</c>
/// reads the real generated bundle out of <c>obj/</c> and holds this reader to it, so if a future SDK ever
/// sorts differently the reader is corrected by a red test instead of quietly lying.</para>
///
/// <para><b>Why concatenation is right here where splicing was right for the markup.</b> A surface's markup
/// belongs INSIDE the page at its invocation site, so <see cref="MapMarkup"/> splices. A surface's rules
/// belong at the END of the sheet before it, because that is where the bundle puts them and where the
/// cascade resolves them. Both readers are the same idea: hand the guard the artefact that ships, not the
/// file that happens to hold part of it.</para>
/// </summary>
internal static class MapStylesheet
{
    private const string PageSheetName = "Map.razor.css";

    private static readonly Lazy<string> TheCascade = new(Compose, isThreadSafe: true);

    /// <summary>The whole Map cascade: the page's sheet then every surface's, in bundle order.</summary>
    internal static string Text => TheCascade.Value;

    /// <summary>
    /// <see cref="File.ReadAllText(string)"/> for every path but one. Handed <c>Pages/Map.razor.css</c> it
    /// returns the whole cascade — the page's sheet plus every surface sheet the bundle appends to it.
    /// </summary>
    internal static string Read(string path) =>
        IsThePageSheet(path) ? TheCascade.Value : File.ReadAllText(path);

    /// <summary>Where the page's own sheet lives, so a guard can name it in a message.</summary>
    internal static string PageSheetPath => Path.Combine(PagesDirectory(), PageSheetName);

    /// <summary>Where the surface sheets live.</summary>
    internal static string SurfacesDirectory() => Path.Combine(PagesDirectory(), "Map");

    /// <summary>
    /// Every sheet in the Map cascade, in bundle order, as (project-relative path, text). The page's own
    /// sheet is first; the surfaces follow, ordered by path without regard to case.
    /// </summary>
    internal static IReadOnlyList<(string RelativePath, string Text)> Sheets()
    {
        List<(string RelativePath, string Text)> sheets =
        [
            ("Pages/Map.razor.css", File.ReadAllText(PageSheetPath))
        ];

        sheets.AddRange(Directory
            .EnumerateFiles(SurfacesDirectory(), "*.razor.css", SearchOption.TopDirectoryOnly)
            .Select(p => (RelativePath: "Pages/Map/" + Path.GetFileName(p), FullPath: p))
            .OrderBy(e => e.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(e => (e.RelativePath, File.ReadAllText(e.FullPath))));

        return sheets;
    }

    private static string Compose()
    {
        IReadOnlyList<(string RelativePath, string Text)> sheets = Sheets();
        if (sheets.Count < 20)
        {
            throw new InvalidOperationException(
                $"#251 item 3 · only {sheets.Count} sheet(s) found for the Map cascade. Every surface under " +
                "Pages/Map/ carries a .razor.css (it is what stamps the page's CSS scope on its markup), so a " +
                "count this small means the reader is looking in the wrong place and every guard reading it " +
                "is asserting nothing.");
        }

        return string.Join("\n", sheets.Select(s => s.Text));
    }

    /// <summary>True for <c>Pages/Map.razor.css</c> and nothing else — the one path that means "the whole
    /// cascade" rather than "one file".</summary>
    internal static bool IsThePageSheet(string path) =>
        Path.GetFileName(path).Equals(PageSheetName, StringComparison.Ordinal)
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
}
