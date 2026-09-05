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

    /// <summary>How the page names itself in the composer's own error messages.</summary>
    private const string PageLabel = "Pages/Map.razor";

    /// <summary>Opens the sliceable region of an extracted surface. Everything between this line and
    /// <see cref="MarkupEnds"/> is the block as it stood in <c>Map.razor</c>, character for character.
    /// The mechanics live in <see cref="SurfaceComposition"/>, said once for both decomposed pages.</summary>
    internal const string MarkupBegins = SurfaceComposition.MarkupBegins;

    /// <summary>Closes the sliceable region of an extracted surface.</summary>
    internal const string MarkupEnds = SurfaceComposition.MarkupEnds;

    private static readonly Lazy<string> TheComposedPage = new(Compose, isThreadSafe: true);

    /// <summary>
    /// <see cref="File.ReadAllText(string)"/> for every path but two. Handed <c>Pages/Map.razor</c> it returns
    /// the COMPOSED page — the file plus every surface it hosts, spliced in where it is invoked.
    ///
    /// <para>#251 item 3 · and handed <c>Pages/Map.razor.css</c> it returns the whole CASCADE, through
    /// <see cref="MapStylesheet"/>: the page's sheet plus every surface sheet, concatenated in the order the
    /// build bundles them. The stylesheet was split along the same seams as the markup, for the same reason,
    /// and the guards that read it are the same guards — so this stays their one entry point and neither half
    /// of the split can quietly blind them.</para>
    /// </summary>
    internal static string Read(string path) =>
        IsThePage(path) ? TheComposedPage.Value
        : MapStylesheet.IsThePageSheet(path) ? MapStylesheet.Text
        : File.ReadAllText(path);

    /// <summary><see cref="File.ReadAllLines(string)"/>'s twin of <see cref="Read"/>, with the same
    /// line-splitting semantics (either ending, no trailing empty).</summary>
    internal static string[] ReadLines(string path)
    {
        if (MapStylesheet.IsThePageSheet(path))
        {
            return SurfaceComposition.AsLines(MapStylesheet.Text);
        }

        return IsThePage(path) ? SurfaceComposition.AsLines(TheComposedPage.Value) : File.ReadAllLines(path);
    }

    /// <summary>The composed page, for guards that want it without a path.</summary>
    internal static string Text => TheComposedPage.Value;

    /// <summary>Where the page lives, so a guard can name it in a message.</summary>
    internal static string PagePath => Path.Combine(PagesDirectory(), PageFileName);

    /// <summary>Where the extracted surfaces live.</summary>
    internal static string SurfacesDirectory() => Path.Combine(PagesDirectory(), "Map");

    /// <summary>Every extracted surface, by component name, with its sliceable markup.</summary>
    internal static IReadOnlyList<(string Name, string Path, string Markup)> Surfaces() =>
        SurfaceComposition.SurfacesIn(SurfacesDirectory(), "Pages/Map/");

    private static bool IsThePage(string path) =>
        Path.GetFileName(path).Equals(PageFileName, StringComparison.Ordinal)
        && Path.GetFileName(Path.GetDirectoryName(path) ?? "").Equals("Pages", StringComparison.Ordinal);

    private static string PagesDirectory() =>
        Path.Combine(SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client", "Pages");

    /// <summary>Map.razor with every surface spliced in where the page invokes it.</summary>
    private static string Compose() => ComposeFrom(File.ReadAllText(PagePath), Surfaces());

    /// <summary>The composition itself, stated over a page text and a set of surfaces rather than over the
    /// disk — so the guard can hand it a doctored page and watch it go red.</summary>
    internal static string ComposeFrom(string page, IReadOnlyList<(string Name, string Path, string Markup)> surfaces)
        => SurfaceComposition.ComposeFrom(page, surfaces, PageLabel);
}
