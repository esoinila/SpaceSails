using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · THE HUD THAT WAS A SURFACE AND IS NOW A HOST, HELD TO THE SAME LAW AS THE PAGE ABOVE IT.
///
/// <para><c>Pages/Map/NavHud.razor</c> came out of <c>Map.razor</c> in #1107 and was the largest thing that
/// move left behind — 1,197 lines, a page's worth of HUD inside one of Map's own surfaces. Its markup is now
/// cut into <c>Pages/Map/NavHud/&lt;Surface&gt;.razor</c>, which makes this the first surface in the repo
/// that is itself decomposed, and the first time the composition has had to follow a cut DOWN a level.</para>
///
/// <para><b>Why that is worth a law of its own.</b> Fifty-eight source guards read Map through
/// <see cref="MapMarkup"/>, and what <see cref="MapMarkup"/> splices into the page at
/// <c>&lt;NavHud …/&gt;</c> is whatever <c>NavHud.razor</c>'s fence contains. Read flat, that fence would now
/// hold nineteen invocation tags where a thousand lines of toolbar, readouts and flight plan used to be, and
/// every one of those guards would go on passing over a page with a hole in it — the fifth bug class arriving
/// through a door the composition already had open.
/// <see cref="SurfaceComposition.RazorText"/> is the answer: a surface with a directory beside it wearing its
/// own name is composed from that directory before its block is cut out. So Map's composed page does not
/// change by a byte, and the proof of that is <see cref="TheComposedNavHudIsTheNavHudAtTheBase"/> below —
/// because if the HUD composes back to what it was, so does the page that hosts it.</para>
///
/// <para><b>What is proved here.</b> Every sub-surface is fenced and findable; the HUD hosts each of them
/// exactly once; nothing that looks like a sub-surface is left un-spliced; the splice is doing real work; the
/// composed HUD is the HUD it was cut from, byte for byte, at this lane's base (75b3ce39); and the three ways
/// a composition can lie each throw, demonstrated on a doctored HUD rather than asserted in prose.</para>
/// </summary>
public sealed class TheComposedNavHudIsTheNavHudTests
{
    /// <summary>Where the HUD's own surfaces live.</summary>
    private static string SurfacesDirectory() =>
        Path.Combine(MapMarkup.SurfacesDirectory(), "NavHud");

    /// <summary>How that directory names itself in the composer's errors.</summary>
    private const string SurfacesLabel = "Pages/Map/NavHud/";

    private static string HudPath() => Path.Combine(MapMarkup.SurfacesDirectory(), "NavHud.razor");

    private static IReadOnlyList<(string Name, string Path, string Markup)> Surfaces() =>
        SurfaceComposition.SurfacesIn(SurfacesDirectory(), SurfacesLabel);

    /// <summary>The HUD as the composition hands it on: <c>NavHud.razor</c> with every sub-surface spliced
    /// back in at its invocation site. This is the exact text <see cref="MapMarkup"/> cuts the HUD's fenced
    /// block out of.</summary>
    private static string ComposedHud() => SurfaceComposition.RazorText(HudPath());

    /// <summary>The composed HUD's fenced markup — what Map's page gets at <c>&lt;NavHud …/&gt;</c>.</summary>
    private static string ComposedMarkup() => SurfaceComposition.MarkupOf(HudPath(), "Pages/Map/");

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

    /// <summary>The HUD hosts every sub-surface, exactly once, and the composed text has no sub-surface tag
    /// left in it — the splice ran to completion rather than half of it.</summary>
    [Fact]
    public void TheHudHostsEverySubSurfaceExactlyOnceAndTheCompositionLeavesNoTagBehind()
    {
        string composed = ComposedHud();

        foreach ((string name, _, string markup) in Surfaces())
        {
            Assert.DoesNotContain($"<{name} ", composed, StringComparison.Ordinal);
            Assert.DoesNotContain($"<{name}/>", composed, StringComparison.Ordinal);
            Assert.Contains(markup.Split('\n')[0], composed, StringComparison.Ordinal);
        }
    }

    /// <summary>#251 · And the same is true one level up: Map's composed page carries the HUD's markup, not
    /// the HUD's invocation tags. This is the clause that would go red if the nesting were ever dropped — the
    /// one that stands between fifty-eight source guards and a page with a hole in it.</summary>
    [Fact]
    public void MapsComposedPageStillCarriesTheWholeHud()
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
    /// <para>The composed HUD equals <c>NavHud.razor</c>'s fenced markup at this lane's base commit, byte for
    /// byte — every region, every comment, every space of indentation. Nothing was rewritten on its way out:
    /// every member a sub-surface reads is a <c>[Parameter]</c> with the SAME NAME, and every comment that
    /// travelled travelled INSIDE its surface's own fence, so there is no arithmetic to check and nothing to
    /// explain away.</para>
    ///
    /// <para>It is stated against a written-down text rather than left in the PR body for the same reason
    /// <c>TheComposedDeskIsTheDeskTests</c> gives: the equality is what makes it safe for the next lane to
    /// edit one sub-surface, and it stops being true the first time somebody adds a button. A deliberate
    /// change to the HUD updates <c>NavHudMarkup.baseline.txt</c> in the same commit, and the diff on that
    /// file is then the honest description of what changed.</para></summary>
    [Fact]
    public void TheComposedNavHudIsTheNavHudAtTheBase()
    {
        string baseline = File.ReadAllText(Path.Combine(
            SurfaceComposition.RepoRoot(), "tests", "SpaceSails.Client.Tests", "NavHudMarkup.baseline.txt"));

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
            "#251 · the composed NavHud is NOT the HUD it was cut from.\n\n"
            + (firstDifference >= 0
                ? $"  first difference at line {firstDifference + 1}:\n"
                  + $"    baseline: {want[firstDifference]}\n"
                  + $"    composed: {got[firstDifference]}\n\n"
                : $"  the two texts agree for {Math.Min(want.Length, got.Length)} lines and then one ends: "
                  + $"baseline {want.Length} lines, composed {got.Length}.\n\n")
            + "NavHudMarkup.baseline.txt is NavHud.razor's markup as it stood at 75b3ce39, the commit this\n"
            + "decomposition branched from. The whole claim of that refactor is that the markup MOVED and was\n"
            + "not rewritten, and this is where the claim is checked — and, because Map's composed page is\n"
            + "this text spliced in at <NavHud/>, it is also what keeps the page's own fifty-eight source\n"
            + "guards reading the page they have always read. If you meant to change the HUD, change the\n"
            + "baseline in the same commit: the diff on that file is then the honest description of what you\n"
            + "changed.");
    }

    /// <summary>#251 · The world this reader is stated against can tell pass from fail. The nested splice must
    /// be doing real work, and each of the three ways a composition can lie must throw rather than shrug.
    /// </summary>
    [Fact]
    public void THE_NESTED_READER_CanTellPassFromFail()
    {
        string onDisk = File.ReadAllText(HudPath());
        IReadOnlyList<(string Name, string Path, string Markup)> surfaces = Surfaces();

        Assert.True(surfaces.Count > 8,
            $"#251 · only {surfaces.Count} surface(s) live under src/SpaceSails.Client/Pages/Map/NavHud/. " +
            "Either the decomposition has been reverted, or this reader is composing a HUD out of one file " +
            "and proving nothing.");
        Assert.True(ComposedHud().Length > onDisk.Length,
            $"#251 · the composed HUD ({ComposedHud().Length} chars) is not longer than NavHud.razor on disk " +
            $"({onDisk.Length}) even though {surfaces.Count} surface(s) moved out of it — the nested splice " +
            "is not putting them back, and Map's composed page has a hole in it where the HUD should be.");

        (string Name, string Path, string Markup) one = surfaces[0];

        // A surface nobody invokes.
        InvalidOperationException never = Assert.Throws<InvalidOperationException>(
            () => SurfaceComposition.ComposeFrom(
                RemoveTheInvocation(onDisk, one.Name), [one], "Pages/Map/NavHud.razor"));
        Assert.Contains("0 time(s)", never.Message, StringComparison.Ordinal);

        // A surface invoked twice.
        string line = TheInvocationLine(onDisk, one.Name)
            ?? throw new InvalidOperationException($"the HUD does not invoke <{one.Name}> on a line of its own");
        InvalidOperationException twice = Assert.Throws<InvalidOperationException>(
            () => SurfaceComposition.ComposeFrom(
                onDisk.Replace(line, line + "\n" + line, StringComparison.Ordinal), [one],
                "Pages/Map/NavHud.razor"));
        Assert.Contains("2 time(s)", twice.Message, StringComparison.Ordinal);

        // A surface with no fence.
        string unfenced = Path.Combine(Path.GetTempPath(), $"spacesails-hud-unfenced-{Guid.NewGuid():N}.razor");
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
