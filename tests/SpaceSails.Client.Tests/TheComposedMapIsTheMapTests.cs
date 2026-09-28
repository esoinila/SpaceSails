using System;
using System.IO;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · THE WHOLE PAGE, AS AN ASSERTION: the composed <c>Map.razor</c> (the page with every surface spliced
/// back in at its invocation site, <see cref="MapMarkup"/>) equals a written-down text, byte for byte.
///
/// <para>The owner's ruling of 2026-09-03 on #251 was <i>"REFACTOR AS MUCH AS FIT: Map.razor → components"</i>.
/// Every cut this decomposition has made kept one promise: the markup MOVED and was not rewritten.
/// <c>TheComposedNavHudIsTheNavHudTests</c>, <c>TheComposedSatchelIsTheSatchelTests</c> and
/// <c>TheComposedDeskIsTheDeskTests</c> hold that promise for the three sub-hosts; nothing held it for the host
/// itself, so a lane cutting the page's own last blocks into surfaces had nothing to prove itself against.
/// This is that proof, written first and on its own commit, before anything moved.</para>
///
/// <para><b>What it means to the next lane.</b> Any deliberate change to the game's markup updates
/// <c>MapMarkup.baseline.txt</c> in the same commit, exactly as the satchel's and the nav HUD's own baselines
/// already ask, and the diff on that file is the honest description of what changed. A pure move leaves it
/// alone, and that is the whole claim a move makes.</para>
/// </summary>
public sealed class TheComposedMapIsTheMapTests
{
    /// <summary>The page, composed, at the commit the razor half of #251 branched from (a560fc43).</summary>
    private const string BaselineFile = "MapMarkup.baseline.txt";

    [Fact]
    public void TheComposedPageIsThePageAtTheBase()
    {
        string baseline = File.ReadAllText(Path.Combine(
            SurfaceComposition.RepoRoot(), "tests", "SpaceSails.Client.Tests", BaselineFile));

        string[] want = SurfaceComposition.AsLines(baseline);
        string[] got = SurfaceComposition.AsLines(MapMarkup.Text);

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
            "#251 · the composed Map.razor is NOT the page it was.\n\n"
            + (firstDifference >= 0
                ? $"  first difference at line {firstDifference + 1}:\n"
                  + $"    baseline: {want[firstDifference]}\n"
                  + $"    composed: {got[firstDifference]}\n\n"
                : $"  the two texts agree for {Math.Min(want.Length, got.Length)} lines and then one ends: "
                  + $"baseline {want.Length} lines, composed {got.Length}.\n\n")
            + $"{BaselineFile} is the page with every surface spliced back in. A move out of Map.razor into a\n"
            + "surface must leave it byte for byte; if you meant to change the game's markup, change the\n"
            + "baseline in the same commit — the diff on that file is then the honest description of it.");
    }

    /// <summary>The pin can tell pass from fail: the baseline is the size of a real page, and it is not the
    /// file on disk (so the splice is part of what is being pinned).</summary>
    [Fact]
    public void THE_PIN_CanTellPassFromFail()
    {
        string[] pinned = SurfaceComposition.AsLines(File.ReadAllText(Path.Combine(
            SurfaceComposition.RepoRoot(), "tests", "SpaceSails.Client.Tests", BaselineFile)));
        string[] onDisk = SurfaceComposition.AsLines(File.ReadAllText(MapMarkup.PagePath));

        Assert.True(pinned.Length > 5 * onDisk.Length,
            $"#251 · the pinned page is {pinned.Length} lines and Map.razor on disk is {onDisk.Length} — the "
            + "baseline is not a composed page, and a pin of the host alone proves nothing about its surfaces.");
    }
}
