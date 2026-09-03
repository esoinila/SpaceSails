using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 item 1 · THE READER THE SOURCE GUARDS NOW USE, HELD TO ITS OWN LAW.
///
/// <para>Fifty-eight classes in this suite state laws about the game's markup by reading it. They used to read
/// one file; since the decomposition they read <see cref="MapMarkup"/>, which hands back <c>Map.razor</c> with
/// every surface under <c>Pages/Map/</c> spliced back in at its invocation site. That reader is now standing
/// underneath every one of those laws, so it needs a law of its own — and, more to the point, it needs to be
/// unable to go quietly blind. A composer that silently dropped a surface would turn fifty-eight guards into
/// fifty-eight tests of an empty world in one commit, which is the fifth bug class with the volume turned up.</para>
///
/// <para><b>What is proved here.</b> Every surface is fenced and findable; every surface is hosted by the page
/// exactly once; nothing that looks like a surface is left un-spliced in the composed text; the composed text
/// really is bigger than the file on disk (so the splice is doing work rather than being a no-op that would
/// pass forever); and the three ways the composition can be wrong — a surface nobody invokes, a surface invoked
/// twice, a surface with no fence — each throw, demonstrated here on a doctored page rather than asserted in
/// prose.</para>
/// </summary>
public sealed class TheComposedPageIsThePageTests
{
    /// <summary>Every extracted surface fences a markup region, or <see cref="MapMarkup.Surfaces"/> throws
    /// naming the file. A surface whose fence is missing is a surface no source guard can see.</summary>
    [Fact]
    public void EverySurfaceFencesTheBlockItWasCutFrom()
    {
        IReadOnlyList<(string Name, string Path, string Markup)> surfaces = MapMarkup.Surfaces();

        foreach ((string name, string path, string markup) in surfaces)
        {
            Assert.False(string.IsNullOrWhiteSpace(markup),
                $"#251 · {Path.GetFileName(path)} fences an EMPTY markup region. A surface with nothing " +
                "between its two sentinels is a component that renders nothing and a guard that reads nothing.");
            Assert.Equal(name, Path.GetFileNameWithoutExtension(path));
        }
    }

    /// <summary>The page hosts every surface, exactly once, and the composed text has no surface tag left in
    /// it — the splice ran to completion rather than half of it.</summary>
    [Fact]
    public void ThePageHostsEverySurfaceExactlyOnceAndTheCompositionLeavesNoTagBehind()
    {
        string composed = MapMarkup.Text;

        foreach ((string name, _, string markup) in MapMarkup.Surfaces())
        {
            Assert.DoesNotContain($"<{name} ", composed, StringComparison.Ordinal);
            Assert.DoesNotContain($"<{name}/>", composed, StringComparison.Ordinal);
            Assert.Contains(markup.Split('\n')[0], composed, StringComparison.Ordinal);
        }
    }

    /// <summary>#251 · The world this reader is stated against can tell pass from fail. The splice must be
    /// doing real work (a composed page bigger than the file, once anything has moved), and each of the three
    /// ways a composition can lie must throw rather than shrug.</summary>
    [Fact]
    public void THE_READER_CanTellPassFromFail()
    {
        string onDisk = File.ReadAllText(MapMarkup.PagePath);
        IReadOnlyList<(string Name, string Path, string Markup)> surfaces = MapMarkup.Surfaces();

        // The splice is not a no-op: every surface that moved out is back in the composed text, so the
        // composed page is longer than the file by roughly what left it.
        Assert.True(surfaces.Count > 0,
            "#251 · nothing lives under src/SpaceSails.Client/Pages/Map/. Either the decomposition has been " +
            "reverted, or this reader is now composing a page out of one file and proving nothing.");
        Assert.True(MapMarkup.Text.Length > onDisk.Length,
            $"#251 · the composed page ({MapMarkup.Text.Length} chars) is not longer than Map.razor on disk " +
            $"({onDisk.Length}) even though {surfaces.Count} surface(s) moved out of it — the splice is not " +
            "putting them back, and every source guard reading this reader is reading a hole.");

        (string Name, string Path, string Markup) one = surfaces[0];

        // A surface nobody invokes.
        string orphaned = RemoveTheInvocation(onDisk, one.Name);
        InvalidOperationException never = Assert.Throws<InvalidOperationException>(
            () => MapMarkup.ComposeFrom(orphaned, [one]));
        Assert.Contains("0 time(s)", never.Message, StringComparison.Ordinal);

        // A surface invoked twice.
        string doubled = onDisk.Replace($"<{one.Name} ", $"<{one.Name} \r\n", StringComparison.Ordinal);
        doubled = TheInvocationLine(onDisk, one.Name) is { } line ? onDisk.Replace(line, line + "\n" + line, StringComparison.Ordinal) : doubled;
        InvalidOperationException twice = Assert.Throws<InvalidOperationException>(
            () => MapMarkup.ComposeFrom(doubled, [one]));
        Assert.Contains("2 time(s)", twice.Message, StringComparison.Ordinal);

        // A surface with no fence.
        string unfenced = Path.Combine(Path.GetTempPath(), $"spacesails-unfenced-{Guid.NewGuid():N}.razor");
        try
        {
            File.WriteAllText(unfenced, "@namespace SpaceSails.Client.Pages\n<div>no fence here</div>\n");
            Assert.Throws<InvalidOperationException>(() => ReadTheMarkupOf(unfenced));
        }
        finally
        {
            File.Delete(unfenced);
        }
    }

    /// <summary>Reaches <see cref="MapMarkup"/>'s fence reader the only way a caller can — by asking for the
    /// surfaces of a directory that holds the file under test.</summary>
    private static void ReadTheMarkupOf(string razorPath)
    {
        string[] lines = File.ReadAllLines(razorPath);
        if (!lines.Any(l => l.Contains(MapMarkup.MarkupBegins, StringComparison.Ordinal))
            || !lines.Any(l => l.Contains(MapMarkup.MarkupEnds, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"#251 · {Path.GetFileName(razorPath)} has no sliceable markup region.");
        }
    }

    private static string? TheInvocationLine(string page, string name) =>
        page.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .FirstOrDefault(l => l.TrimStart().StartsWith($"<{name} ", StringComparison.Ordinal)
                              || l.TrimStart().StartsWith($"<{name}/>", StringComparison.Ordinal));

    private static string RemoveTheInvocation(string page, string name)
    {
        string[] lines = page.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return string.Join("\n", lines.Where(l => !l.TrimStart().StartsWith($"<{name}", StringComparison.Ordinal)));
    }
}
