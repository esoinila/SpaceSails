using System.IO;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>Fable's item 38 (#1387 / #1465 d): the long-coast advert's face, title and once-per-leg log line, pinned
/// byte-for-byte. The PlanEnd "coast this orbit" face of the OTHER skip button (#1465) must keep its wording.</summary>
public sealed class TheLongCoastAdvertSaysDeadCoastTests
{
    private static string Src(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", .. parts]));

    [Fact]
    public void TheAdvertFaceAndTitleAreFablesWordsWithTheCountdownSlotKept()
    {
        string razor = Src("Map", "NavHud", "NavToolbar.razor");
        Assert.Contains("⏭ Dead coast ahead <span class=\"map-coast-countdown\">(@LongCoastAheadReadout)</span> — sleep through it", razor);
        Assert.Contains("title=\"Nothing to fly for the whole of it. The coast is free and the skip spends only time; a burn, an arrival or an alert still pulls you back to 1×.\"", razor);
        Assert.DoesNotContain("Long coast ahead", razor);
        Assert.Contains("\"⏭ coast this orbit\"", razor); // #1465 PlanEnd face untouched
    }

    [Fact]
    public void TheOncePerLegLogLineIsFablesWords()
    {
        string cs = Src("Map.LongHaul.Skip.cs");
        Assert.Contains("of nothing to {SkipEventLabel(_skipNext.Kind)}. \" +", cs);
        Assert.Contains("\"The coast costs nothing but the time — ⏭ sleeps through it.\"", cs);
        Assert.Contains("$\"⏭ {FormatDuration(_skipNext.Epoch - SimTime)} of nothing to", cs);
        Assert.DoesNotContain("the coast is free, only time passes", cs);
    }
}
