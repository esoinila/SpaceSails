using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · THE SECOND SURFACE THAT IS ALSO A HOST, HELD TO THE SAME LAW AS THE PAGE ABOVE IT.
///
/// <para><c>Pages/Map/SatchelPanel.razor</c> came out of <c>Map.razor</c> in #1107 and grew back to 983
/// lines — six pages behind one tab bar, and by some way the largest surface left under
/// <c>Pages/Map/</c>. Its markup is now cut into <c>Pages/Map/SatchelPanel/&lt;Surface&gt;.razor</c>,
/// which makes it the second surface in the repo that is itself decomposed after <c>NavHud</c>, and the
/// first one where a sub-surface hosts sub-surfaces of its own (<c>CarriedPockets</c> holds the load
/// chooser and the rows).</para>
///
/// <para><b>Why that is worth a law of its own.</b> Fifty-eight source guards read Map through
/// <see cref="MapMarkup"/>, and what <see cref="MapMarkup"/> splices into the page at
/// <c>&lt;SatchelPanel …/&gt;</c> is whatever <c>SatchelPanel.razor</c>'s fence contains. Read flat, that
/// fence would now hold eight invocation tags where nine hundred lines of pocket, book, threads, spread,
/// bin and compass used to be, and every one of those guards would go on passing over a page with a hole
/// in it. <see cref="SurfaceComposition.RazorText"/> is the answer, and it already knew how: a surface
/// with a directory beside it wearing its own name is composed from that directory before its block is
/// cut out, and the fixpoint splice #1172 put in place lets a sub-surface host a sub-surface without the
/// alphabet getting a say. So Map's composed page does not change by a byte, and the proof of that is
/// <see cref="TheComposedSatchelIsTheSatchelAtTheBase"/> below — because if the satchel composes back to
/// what it was, so does the page that hosts it.</para>
///
/// <para><b>What is proved here.</b> Every sub-surface is fenced and findable; the panel (or one of its
/// own sub-surfaces) hosts each of them exactly once; nothing that looks like a sub-surface is left
/// un-spliced; the splice is doing real work; the composed panel is the panel it was cut from, byte for
/// byte, at this lane's base (911e91aa); and the three ways a composition can lie each throw, demonstrated
/// on a doctored panel rather than asserted in prose.</para>
/// </summary>
public sealed class TheComposedSatchelIsTheSatchelTests
{
    /// <summary>Where the panel's own surfaces live.</summary>
    private static string SurfacesDirectory() =>
        Path.Combine(MapMarkup.SurfacesDirectory(), "SatchelPanel");

    /// <summary>How that directory names itself in the composer's errors.</summary>
    private const string SurfacesLabel = "Pages/Map/SatchelPanel/";

    private static string PanelPath() => Path.Combine(MapMarkup.SurfacesDirectory(), "SatchelPanel.razor");

    private static IReadOnlyList<(string Name, string Path, string Markup)> Surfaces() =>
        SurfaceComposition.SurfacesIn(SurfacesDirectory(), SurfacesLabel);

    /// <summary>The panel as the composition hands it on: <c>SatchelPanel.razor</c> with every sub-surface
    /// spliced back in at its invocation site. This is the exact text <see cref="MapMarkup"/> cuts the
    /// panel's fenced block out of.</summary>
    private static string ComposedPanel() => SurfaceComposition.RazorText(PanelPath());

    /// <summary>The composed panel's fenced markup — what Map's page gets at
    /// <c>&lt;SatchelPanel …/&gt;</c>.</summary>
    private static string ComposedMarkup() => SurfaceComposition.MarkupOf(PanelPath(), "Pages/Map/");

    /// <summary>Every sub-surface fences a markup region, or the composer throws naming the file. A surface
    /// whose fence is missing is a surface no source guard can see.</summary>
    [Fact]
    public void EverySubSurfaceFencesTheBlockItWasCutFrom()
    {
        foreach ((string name, string path, string markup) in Surfaces())
        {
            Assert.False(string.IsNullOrWhiteSpace(markup),
                $"#251 · {Path.GetFileName(path)} fences an EMPTY markup region. A surface with nothing " +
                "between its two sentinels is a component that renders nothing and a guard that reads nothing.");
            Assert.Equal(name, Path.GetFileNameWithoutExtension(path));
        }
    }

    /// <summary>The panel hosts every sub-surface, exactly once — itself, or through one of its own
    /// sub-surfaces — and the composed text has no sub-surface tag left in it, so the splice ran to
    /// completion rather than half of it.</summary>
    [Fact]
    public void ThePanelHostsEverySubSurfaceExactlyOnceAndTheCompositionLeavesNoTagBehind()
    {
        string composed = ComposedPanel();

        foreach ((string name, _, string markup) in Surfaces())
        {
            Assert.DoesNotContain($"<{name} ", composed, StringComparison.Ordinal);
            Assert.DoesNotContain($"<{name}/>", composed, StringComparison.Ordinal);
            Assert.Contains(markup.Split('\n')[0], composed, StringComparison.Ordinal);
        }
    }

    /// <summary>#251 · And the same is true one level up: Map's composed page carries the satchel's markup,
    /// not the satchel's invocation tags. This is the clause that would go red if the nesting were ever
    /// dropped — the one that stands between fifty-eight source guards and a page with a hole in it.</summary>
    [Fact]
    public void MapsComposedPageStillCarriesTheWholeSatchel()
    {
        string page = MapMarkup.Text;

        foreach ((string name, _, string markup) in Surfaces())
        {
            Assert.DoesNotContain($"<{name} ", page, StringComparison.Ordinal);
            Assert.Contains(markup.Split('\n')[0], page, StringComparison.Ordinal);
        }
    }

    /// <summary>#251 · THE PROOF OF THE REFACTOR, AS AN ASSERTION AND NOT A PARAGRAPH.
    ///
    /// <para>The composed panel equals <c>SatchelPanel.razor</c>'s fenced markup at this lane's base commit,
    /// byte for byte — every region, every comment, every space of indentation. Nothing was rewritten on its
    /// way out: every member a sub-surface reads is a <c>[Parameter]</c> with the SAME NAME, every page gate
    /// stayed on the panel with its invocation (the six of them are one <c>if / else if</c> chain and half a
    /// chain is not a sentence), and every comment that travelled travelled INSIDE its surface's own fence.
    /// So there is no arithmetic to check and nothing to explain away.</para>
    ///
    /// <para>It is stated against a written-down text rather than left in the PR body for the same reason
    /// <c>TheComposedNavHudIsTheNavHudTests</c> gives: the equality is what makes it safe for the next lane
    /// to edit one sub-surface, and it stops being true the first time somebody adds a button. A deliberate
    /// change to the satchel updates <c>SatchelPanelMarkup.baseline.txt</c> in the same commit, and the diff
    /// on that file is then the honest description of what changed.</para></summary>
    [Fact]
    public void TheComposedSatchelIsTheSatchelAtTheBase()
    {
        string baseline = File.ReadAllText(Path.Combine(
            SurfaceComposition.RepoRoot(), "tests", "SpaceSails.Client.Tests",
            "SatchelPanelMarkup.baseline.txt"));

        string[] want = SurfaceComposition.AsLines(baseline);
        string[] got = SurfaceComposition.AsLines(ComposedMarkup());

        int firstDifference = -1;
        for (int i = 0; i < Math.Min(want.Length, got.Length); i++)
        {
            if (!string.Equals(want[i], got[i], StringComparison.Ordinal))
            {
                firstDifference = i;
                break;
            }
        }

        Assert.True(firstDifference < 0 && want.Length == got.Length,
            "#251 · the composed satchel is NOT the panel it was cut from.\n\n"
            + (firstDifference >= 0
                ? $"  first difference at line {firstDifference + 1}:\n"
                  + $"    baseline: {want[firstDifference]}\n"
                  + $"    composed: {got[firstDifference]}\n\n"
                : $"  the two texts agree for {Math.Min(want.Length, got.Length)} lines and then one ends: "
                  + $"baseline {want.Length} lines, composed {got.Length}.\n\n")
            + "SatchelPanelMarkup.baseline.txt is SatchelPanel.razor's markup as it stood at 911e91aa, the\n"
            + "commit this decomposition branched from. The whole claim of that refactor is that the markup\n"
            + "MOVED and was not rewritten, and this is where the claim is checked — and, because Map's\n"
            + "composed page is this text spliced in at <SatchelPanel/>, it is also what keeps the page's own\n"
            + "fifty-eight source guards reading the page they have always read. If you meant to change the\n"
            + "satchel, change the baseline in the same commit: the diff on that file is then the honest\n"
            + "description of what you changed.");
    }

    /// <summary>#251 · The world this reader is stated against can tell pass from fail. The nested splice
    /// must be doing real work, one sub-surface must really be hosted by ANOTHER sub-surface rather than by
    /// the panel (which is what the fixpoint is for), and each of the three ways a composition can lie must
    /// throw rather than shrug.</summary>
    [Fact]
    public void THE_NESTED_READER_CanTellPassFromFail()
    {
        string onDisk = File.ReadAllText(PanelPath());
        IReadOnlyList<(string Name, string Path, string Markup)> surfaces = Surfaces();

        Assert.True(surfaces.Count > 6,
            $"#251 · only {surfaces.Count} surface(s) live under src/SpaceSails.Client/Pages/Map/SatchelPanel/. " +
            "Either the decomposition has been reverted, or this reader is composing a panel out of one file " +
            "and proving nothing.");
        Assert.True(ComposedPanel().Length > onDisk.Length,
            $"#251 · the composed panel ({ComposedPanel().Length} chars) is not longer than SatchelPanel.razor " +
            $"on disk ({onDisk.Length}) even though {surfaces.Count} surface(s) moved out of it — the nested " +
            "splice is not putting them back, and Map's composed page has a hole in it where the satchel " +
            "should be.");

        // #1172 · at least one sub-surface is hosted by a SIBLING and not by the panel. That is the case
        // the fixpoint splice exists for, and a decomposition that flattened it would take this reader's
        // most interesting clause with it silently.
        int nested = surfaces.Count(s => !onDisk.Contains($"<{s.Name} ", StringComparison.Ordinal));
        Assert.True(nested > 0,
            "#251 · every sub-surface is invoked straight from SatchelPanel.razor, so nothing here is "
            + "exercising the fixpoint splice a surface-hosting-a-surface needs.");

        (string Name, string Path, string Markup) one = surfaces.First(
            s => onDisk.Contains($"<{s.Name} ", StringComparison.Ordinal));

        // A surface nobody invokes.
        InvalidOperationException never = Assert.Throws<InvalidOperationException>(
            () => SurfaceComposition.ComposeFrom(
                RemoveTheInvocation(onDisk, one.Name), [one], "Pages/Map/SatchelPanel.razor"));
        Assert.Contains("0 time(s)", never.Message, StringComparison.Ordinal);

        // A surface invoked twice.
        string line = TheInvocationLine(onDisk, one.Name)
            ?? throw new InvalidOperationException($"the panel does not invoke <{one.Name}> on a line of its own");
        InvalidOperationException twice = Assert.Throws<InvalidOperationException>(
            () => SurfaceComposition.ComposeFrom(
                onDisk.Replace(line, line + "\n" + line, StringComparison.Ordinal), [one],
                "Pages/Map/SatchelPanel.razor"));
        Assert.Contains("2 time(s)", twice.Message, StringComparison.Ordinal);

        // A surface with no fence.
        string unfenced = Path.Combine(Path.GetTempPath(), $"spacesails-satchel-unfenced-{Guid.NewGuid():N}.razor");
        try
        {
            File.WriteAllText(unfenced, "@namespace SpaceSails.Client.Pages\n<div>no fence</div>\n");
            Assert.Throws<InvalidOperationException>(
                () => SurfaceComposition.MarkupOf(unfenced, SurfacesLabel));
        }
        finally
        {
            File.Delete(unfenced);
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
