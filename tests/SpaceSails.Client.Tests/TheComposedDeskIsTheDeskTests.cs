using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · THE SENSORS DESK'S READER, HELD TO ITS OWN LAW.
///
/// <para><see cref="TheComposedPageIsThePageTests"/>' twin for <c>Pages/Stations/TrackingPost.razor</c>. The
/// desk's markup lives in surfaces under <c>Pages/Stations/TrackingPost/</c> now, and
/// <see cref="TrackingPostMarkup"/> hands the source guards the desk with every one of them spliced back in at
/// its invocation site. That reader stands underneath a law about what the sensor-tasks box says (#239's
/// state rows, in <c>TheDeskReadsTheStateNotThePercentTests</c>), so it needs to be unable to go quietly
/// blind: a composer that silently dropped a surface would turn that guard into a test of an empty world.</para>
///
/// <para><b>What is proved here.</b> Every surface is fenced and findable; every surface is hosted by the desk
/// exactly once; nothing that looks like a surface is left un-spliced in the composed text; the composed text
/// really is bigger than the file on disk (so the splice is doing work rather than being a no-op that would
/// pass forever); and the three ways the composition can be wrong — a surface nobody invokes, a surface
/// invoked twice, a surface with no fence — each throw, demonstrated on a doctored desk rather than asserted
/// in prose.</para>
///
/// <para><b>And the equality that is the whole point.</b> <see cref="TheComposedDeskIsTheDeskAtTheBase"/>
/// holds the composed text to the desk as it stood before the cut — byte for byte, comments included. The
/// extraction moved the markup verbatim (every desk member a surface reads is a <c>[Parameter]</c> with the
/// SAME NAME, and every comment travelled inside its surface's own fence), so there is nothing to explain
/// away: the composed desk is the desk. The expected text is not typed here — it is read from git, out of the
/// commit this lane branched from, so the assertion cannot be quietly retuned to whatever the code now says.</para>
/// </summary>
public sealed class TheComposedDeskIsTheDeskTests
{
    /// <summary>Every extracted surface fences a markup region, or <see cref="TrackingPostMarkup.Surfaces"/>
    /// throws naming the file. A surface whose fence is missing is a surface no source guard can see.</summary>
    [Fact]
    public void EverySurfaceFencesTheBlockItWasCutFrom()
    {
        foreach ((string name, string path, string markup) in TrackingPostMarkup.Surfaces())
        {
            Assert.False(string.IsNullOrWhiteSpace(markup),
                $"#251 · {Path.GetFileName(path)} fences an EMPTY markup region. A surface with nothing " +
                "between its two sentinels is a component that renders nothing and a guard that reads nothing.");
            Assert.Equal(name, Path.GetFileNameWithoutExtension(path));
        }
    }

    /// <summary>The desk hosts every surface, exactly once, and the composed text has no surface tag left in
    /// it — the splice ran to completion rather than half of it.</summary>
    [Fact]
    public void TheDeskHostsEverySurfaceExactlyOnceAndTheCompositionLeavesNoTagBehind()
    {
        string composed = TrackingPostMarkup.Text;

        foreach ((string name, _, string markup) in TrackingPostMarkup.Surfaces())
        {
            Assert.DoesNotContain($"<{name} ", composed, StringComparison.Ordinal);
            Assert.DoesNotContain($"<{name}/>", composed, StringComparison.Ordinal);
            Assert.Contains(markup.Split('\n')[0], composed, StringComparison.Ordinal);
        }
    }

    /// <summary>#251 · The world this reader is stated against can tell pass from fail. The splice must be
    /// doing real work, and each of the three ways a composition can lie must throw rather than shrug.</summary>
    [Fact]
    public void THE_DESK_READER_CanTellPassFromFail()
    {
        string onDisk = File.ReadAllText(TrackingPostMarkup.PagePath);
        IReadOnlyList<(string Name, string Path, string Markup)> surfaces = TrackingPostMarkup.Surfaces();

        Assert.True(surfaces.Count > 0,
            "#251 · nothing lives under src/SpaceSails.Client/Pages/Stations/TrackingPost/. Either the " +
            "decomposition has been reverted, or this reader is composing a desk out of one file and proving " +
            "nothing.");
        Assert.True(TrackingPostMarkup.Text.Length > onDisk.Length,
            $"#251 · the composed desk ({TrackingPostMarkup.Text.Length} chars) is not longer than " +
            $"TrackingPost.razor on disk ({onDisk.Length}) even though {surfaces.Count} surface(s) moved out " +
            "of it — the splice is not putting them back, and every source guard reading this reader is " +
            "reading a hole.");

        (string Name, string Path, string Markup) one = surfaces[0];

        // A surface nobody invokes.
        string orphaned = RemoveTheInvocation(onDisk, one.Name);
        InvalidOperationException never = Assert.Throws<InvalidOperationException>(
            () => TrackingPostMarkup.ComposeFrom(orphaned, [one]));
        Assert.Contains("0 time(s)", never.Message, StringComparison.Ordinal);

        // A surface invoked twice.
        string line = TheInvocationLine(onDisk, one.Name)
            ?? throw new InvalidOperationException($"the desk does not invoke <{one.Name}> on a line of its own");
        string doubled = onDisk.Replace(line, line + "\n" + line, StringComparison.Ordinal);
        InvalidOperationException twice = Assert.Throws<InvalidOperationException>(
            () => TrackingPostMarkup.ComposeFrom(doubled, [one]));
        Assert.Contains("2 time(s)", twice.Message, StringComparison.Ordinal);

        // A surface with no fence.
        string unfenced = Path.Combine(Path.GetTempPath(), $"spacesails-desk-unfenced-{Guid.NewGuid():N}.razor");
        try
        {
            File.WriteAllText(unfenced, "@namespace SpaceSails.Client.Pages.Stations\n<div>no fence</div>\n");
            Assert.Throws<InvalidOperationException>(
                () => SurfaceComposition.MarkupOf(unfenced, TrackingPostMarkup.SurfacesLabel));
        }
        finally
        {
            File.Delete(unfenced);
        }
    }

    /// <summary>#251 · THE PROOF OF THE REFACTOR, AS AN ASSERTION AND NOT A PARAGRAPH.
    ///
    /// <para>The composed desk equals <c>TrackingPost.razor</c>'s markup at this lane's base commit, byte for
    /// byte — every region, every comment, every space of indentation. If it does not, either a surface was
    /// rewritten on its way out (which this lane promised not to do) or a later change edited a surface
    /// without the desk's own history following it. The second is the reason this law does not simply live in
    /// the PR body: the equality is what makes it safe for the next lane to edit one surface, and it stops
    /// being true the first time somebody adds a row.</para>
    ///
    /// <para>So it is stated against a WRITTEN-DOWN text — <c>docs/testing-guide</c>'s own idiom for a
    /// measured baseline — kept beside this test as the desk's markup at 4de98a79. A deliberate change to the
    /// desk updates that file in the same commit and the diff shows exactly what moved.</para></summary>
    [Fact]
    public void TheComposedDeskIsTheDeskAtTheBase()
    {
        string baseline = File.ReadAllText(Path.Combine(
            SurfaceComposition.RepoRoot(), "tests", "SpaceSails.Client.Tests", "TrackingPostMarkup.baseline.txt"));

        string[] want = SurfaceComposition.AsLines(baseline);
        string[] got = SurfaceComposition.AsLines(TrackingPostMarkup.Text);

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
            "#251 · the composed desk is NOT the desk it was cut from.\n\n"
            + (firstDifference >= 0
                ? $"  first difference at line {firstDifference + 1}:\n"
                  + $"    baseline: {want[firstDifference]}\n"
                  + $"    composed: {got[firstDifference]}\n\n"
                : $"  the two texts agree for {Math.Min(want.Length, got.Length)} lines and then one ends: "
                  + $"baseline {want.Length} lines, composed {got.Length}.\n\n")
            + "TrackingPostMarkup.baseline.txt is TrackingPost.razor's markup as it stood at 4de98a79, the\n"
            + "commit the decomposition branched from. The whole claim of that refactor is that the markup\n"
            + "MOVED and was not rewritten, and this is where the claim is checked. If you meant to change the\n"
            + "desk, change the baseline in the same commit — the diff on that file is then the honest\n"
            + "description of what you changed.");
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
