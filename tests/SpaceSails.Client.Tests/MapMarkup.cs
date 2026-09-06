using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    ///
    /// <para>#1107 · and handed ANY OTHER <c>.razor</c> it returns the whole COMPONENT — the razor plus the
    /// <c>.razor.cs</c> code-behind its <c>@code</c> block moved into. Same reason a third time, said in
    /// <see cref="SurfaceComposition.ComponentText"/>: a guard that opens one of a component's two files is
    /// half blind, and it fails by finding nothing rather than by finding something wrong.</para>
    /// </summary>
    internal static string Read(string path) =>
        IsThePage(path) ? TheComposedPage.Value
        : MapStylesheet.IsThePageSheet(path) ? MapStylesheet.Text
        : IsAComponent(path) ? SurfaceComposition.ComponentText(path)
        : IsTheHiveSurface(path) ? TheHiveSurface.Value
        : File.ReadAllText(path);

    /// <summary><see cref="File.ReadAllLines(string)"/>'s twin of <see cref="Read"/>, with the same
    /// line-splitting semantics (either ending, no trailing empty).</summary>
    internal static string[] ReadLines(string path)
    {
        if (MapStylesheet.IsThePageSheet(path))
        {
            return SurfaceComposition.AsLines(MapStylesheet.Text);
        }

        if (IsThePage(path))
        {
            return SurfaceComposition.AsLines(TheComposedPage.Value);
        }

        if (IsTheHiveSurface(path))
        {
            return SurfaceComposition.AsLines(TheHiveSurface.Value);
        }

        return IsAComponent(path)
            ? SurfaceComposition.AsLines(SurfaceComposition.ComponentText(path))
            : File.ReadAllLines(path);
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

    /// <summary>
    /// #251 · A SPLIT FAMILY, READ AS ONE SUBJECT — every file under <c>Pages/</c> matching
    /// <paramref name="pattern"/>, joined in ORDINAL FILENAME ORDER so the read is the same on every machine
    /// and in CI.
    ///
    /// <para>This is the other half of <see cref="Read"/>'s promise. <c>Read</c> keeps a guard from being
    /// half-blind when one SUBJECT is two files by construction (a page and its surfaces, a component and
    /// its code-behind); this keeps a guard from going half-blind when the refactor phase turns one file
    /// into six. #1163's crew wrote the warning down after the fifth time: a guard that sweeps a whole file
    /// with <c>DoesNotContain</c> and is then re-pathed at ONE partial does not go red — <b>it quietly stops
    /// looking at four fifths of the room</b>, which is the fifth bug class wearing a tidy diff.</para>
    ///
    /// <para>So: a guard whose claim is about a SUBJECT (<i>"nothing in the docking code names the
    /// autopilot's economy"</i>) reads the family; a guard whose claim is about ONE MEMBER may name the one
    /// partial that member lives in, and will fail loudly if it moves. The pattern is a glob, so an exact
    /// filename is a family of one and this is safe to use for both.</para>
    ///
    /// <para><b>Proven RED</b> in the PR that added it, by appending a canary line to a partial that did not
    /// exist before the split and watching the sweeps that read the family fail on it.</para>
    /// </summary>
    internal static string PagesFamily(string pattern) => ReadFamily(PagesDirectory(), pattern);

    /// <summary><see cref="PagesFamily"/> for a family that does not live under <c>Pages/</c>.</summary>
    internal static string ReadFamily(string directory, string pattern) =>
        string.Join("\n", Directory
            .EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(Read));

    private static bool IsThePage(string path) =>
        Path.GetFileName(path).Equals(PageFileName, StringComparison.Ordinal)
        && Path.GetFileName(Path.GetDirectoryName(path) ?? "").Equals("Pages", StringComparison.Ordinal);

    /// <summary>#1107 · a razor file that is not the page — read it with its code-behind.</summary>
    private static bool IsAComponent(string path) =>
        path.EndsWith(".razor", StringComparison.Ordinal);

    // ── #251 · THE HIVE SURFACE IS FOUR PARTIALS, AND FOURTEEN GUARDS READ IT AS ONE FILE ────────────────
    //
    // Map.Surface.Hive.cs was 1,122 lines and the size gate's next case. It is now the shaft and its panel,
    // plus Ride (arriving on a floor), Search (turning a room over, reading a sign) and SecretLab (the
    // assembled person, the detector, the monolith's foot). Fourteen test classes in this suite open it as
    // TEXT — six of them slice a region out with Between(from, to), three count occurrences, and several
    // assert DoesNotContain over the WHOLE subject, which is the claim that goes quietly blind rather than
    // red when the code it is about moves into a file nobody opened.
    //
    // So they stop reading a FILE and start reading the SURFACE, exactly as they stopped reading Map.razor
    // and started reading the composed page. ORDER IS PART OF THE ANSWER: three of these guards assert on
    // the ORDER of two indices in the text (the book is asked for before the pocket, the decision is
    // offered before the pocket), so the parts are concatenated in the order the ONE FILE laid them out —
    // which Ordinal filename order does NOT give, since "Map.Surface.Hive.Ride.cs" sorts before
    // "Map.Surface.Hive.cs". The order is spelled out, and a part missing from disk throws rather than
    // being silently left out of the text.

    /// <summary>The Hive surface's partials, in the order the one file declared them.</summary>
    private static readonly string[] HiveSurfaceParts =
    [
        "Map.Surface.Hive.cs",
        "Map.Surface.Hive.Ride.cs",
        "Map.Surface.Hive.Search.cs",
        "Map.Surface.Hive.SecretLab.cs",
    ];

    private static readonly Lazy<string> TheHiveSurface = new(ComposeHiveSurface, isThreadSafe: true);

    /// <summary>The first of <see cref="HiveSurfaceParts"/> — the path every guard still names.</summary>
    private static bool IsTheHiveSurface(string path) =>
        Path.GetFileName(path).Equals(HiveSurfaceParts[0], StringComparison.Ordinal)
        && Path.GetFileName(Path.GetDirectoryName(path) ?? "").Equals("Pages", StringComparison.Ordinal);

    private static string ComposeHiveSurface()
    {
        var text = new StringBuilder();
        foreach (string part in HiveSurfaceParts)
        {
            string path = Path.Combine(PagesDirectory(), part);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Pages/{part} is not on disk — the Hive surface has been re-cut and every guard that "
                    + "reads it is about to go blind rather than red. Fix this list.");
            }

            text.Append(File.ReadAllText(path));
        }

        return text.ToString();
    }

    private static string PagesDirectory() =>
        Path.Combine(SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client", "Pages");

    /// <summary>Map.razor with every surface spliced in where the page invokes it — and, since #1107, with
    /// the page's own code-behind on the end, because two of its <c>@code</c> members live there now.</summary>
    private static string Compose() =>
        ComposeFrom(SurfaceComposition.ComponentText(PagePath), Surfaces());

    /// <summary>The composition itself, stated over a page text and a set of surfaces rather than over the
    /// disk — so the guard can hand it a doctored page and watch it go red.</summary>
    internal static string ComposeFrom(string page, IReadOnlyList<(string Name, string Path, string Markup)> surfaces)
        => SurfaceComposition.ComposeFrom(page, surfaces, PageLabel);
}
