using System;
using System.Collections.Generic;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · THE SENSORS DESK'S MARKUP, AS THE DESK COMPOSES IT.
///
/// <para><see cref="MapMarkup"/>'s twin for <c>Pages/Stations/TrackingPost.razor</c>. The desk's markup has
/// been cut into surfaces under <c>Pages/Stations/TrackingPost/</c>, and a guard that opens the file on disk
/// can no longer see the regions it states laws about. This hands back the desk with every surface spliced in
/// AT ITS OWN INVOCATION SITE — the same regions, in the same order, at the same indentation — so the source
/// guards go on reading the desk they always read.</para>
///
/// <para><b>A splice and not a concatenation</b>, for the reason <see cref="MapMarkup"/> gives at length:
/// <c>TheDeskReadsTheStateNotThePercentTests</c> slices the sensor-task box out of this text with
/// <c>IndexOf("sensor-tasks-box") … IndexOf("sensor-lost-box")</c>, and appending the surfaces to the end of
/// the file would keep every <c>Contains</c> green while destroying that slice silently.</para>
///
/// <para>The extraction moved the markup verbatim — every desk member a surface reads is a
/// <c>[Parameter]</c> with the SAME NAME — so the composed text is byte-for-byte the text that was in
/// <c>TrackingPost.razor</c> before the cut. That equality is the whole proof of the refactor and is quoted in
/// the PR body; <c>TheComposedDeskIsTheDeskTests</c> holds this reader to its own law.</para>
/// </summary>
internal static class TrackingPostMarkup
{
    private const string PageFileName = "TrackingPost.razor";

    /// <summary>How the desk names itself in the composer's own error messages.</summary>
    private const string PageLabel = "Pages/Stations/TrackingPost.razor";

    /// <summary>How the surfaces directory names itself in the fence error.</summary>
    internal const string SurfacesLabel = "Pages/Stations/TrackingPost/";

    private static readonly Lazy<string> TheComposedDesk = new(Compose, isThreadSafe: true);

    /// <summary><see cref="File.ReadAllText(string)"/> for every path but one. Handed
    /// <c>Pages/Stations/TrackingPost.razor</c> it returns the COMPOSED desk.</summary>
    internal static string Read(string path) => IsTheDesk(path) ? TheComposedDesk.Value : File.ReadAllText(path);

    /// <summary>The composed desk, for guards that want it without a path.</summary>
    internal static string Text => TheComposedDesk.Value;

    /// <summary>Where the desk lives, so a guard can name it in a message.</summary>
    internal static string PagePath => Path.Combine(StationsDirectory(), PageFileName);

    /// <summary>Where the extracted surfaces live.</summary>
    internal static string SurfacesDirectory() => Path.Combine(StationsDirectory(), "TrackingPost");

    /// <summary>Every extracted surface, by component name, with its sliceable markup.</summary>
    internal static IReadOnlyList<(string Name, string Path, string Markup)> Surfaces() =>
        SurfaceComposition.SurfacesIn(SurfacesDirectory(), SurfacesLabel);

    private static bool IsTheDesk(string path) =>
        Path.GetFileName(path).Equals(PageFileName, StringComparison.Ordinal)
        && Path.GetFileName(Path.GetDirectoryName(path) ?? "").Equals("Stations", StringComparison.Ordinal);

    private static string StationsDirectory() =>
        Path.Combine(SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Stations");

    private static string Compose() => ComposeFrom(File.ReadAllText(PagePath), Surfaces());

    /// <summary>The composition, stated over a desk text and a set of surfaces rather than over the disk — so
    /// the guard can hand it a doctored desk and watch it go red.</summary>
    internal static string ComposeFrom(string page, IReadOnlyList<(string Name, string Path, string Markup)> surfaces)
        => SurfaceComposition.ComposeFrom(page, surfaces, PageLabel);
}
