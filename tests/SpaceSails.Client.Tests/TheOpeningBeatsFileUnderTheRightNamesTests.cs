using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Main-arcs beats 1 and 2, the ledger and the keybar: a story card is filed under a story heading and not the
/// autopilot's (R1-F63), the overheard bounce line files under the person who said it (R1-F64), a keep's quoted
/// nickname keeps its case (R1-F66), and the keybar stops pointing at an airlock the captain has walked through
/// (R1-F75).
/// </summary>
public sealed class TheOpeningBeatsFileUnderTheRightNamesTests
{
    private static (string Title, string Line)[] Rows(DeskBench bench) =>
        ((IEnumerable)bench.Call("LedgerTips")!).Cast<object>()
        .Select(t =>
        {
            Type ty = t.GetType();
            string title = (string)ty.GetProperty("Title")!.GetValue(t)!;
            string line = string.Join("|", (IEnumerable<string>)ty.GetProperty("Lines")!.GetValue(t)!);
            return (title, line);
        }).ToArray();

    [Fact]
    public async Task AStoryCardIsFiledUnderTheStoryHeadingAndAHandbackStaysUnderTheAutopilot()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?ashore=1&kaamos=bounce");
        await bench.RenderAsync();
        bench.CallOnTheDispatcher("RaiseStoryBeat", StoryBeats.Beat.KaamosFilingBounced, null, null);
        bench.CallOnTheDispatcher("LogAutopilotEvent", "A real handback line.");

        var rows = Rows(bench);
        string title = StoryBeats.Title(StoryBeats.Beat.KaamosFilingBounced);
        Assert.Contains(rows, r => r.Title == StoryBeats.LedgerTitle && r.Line.Contains(title, StringComparison.Ordinal));
        Assert.DoesNotContain(rows, r => r.Title == "🛰 Autopilot" && r.Line.Contains(title, StringComparison.Ordinal));
        Assert.Contains(rows, r => r.Title == "🛰 Autopilot" && r.Line == "A real handback line.");
    }

    [Fact]
    public async Task TheOverheardBounceLineFilesUnderTheOneWhoGaveTheJob()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?ashore=1&kaamos=bounce");
        await bench.RenderAsync();
        object offer = bench.Call("MakeKaamosBounceOffer", "madam coil")
            ?? throw new Xunit.Sdk.XunitException("no bounce offer minted at the cheat bar");
        bench.CallOnTheDispatcher("TakeKaamosBounceFiling", offer);

        var book = (IList)bench.Peek("_overheard")!;
        object last = book[^1]!;
        string source = (string)last.GetType().GetProperty("Source")!.GetValue(last)!;
        Assert.Equal("madam coil", source);
        Assert.NotEqual("kaamos-bounce", source);
    }

    [Fact]
    public void AKeepsQuotedNicknameKeepsItsCaseAndAnIdStillTitleCases()
    {
        MethodInfo m = typeof(SpaceSails.Client.Pages.Map).GetMethod("GiverDisplay", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.Equal("Cassini “Cass” Roe", (string)m.Invoke(null, ["Cassini “Cass” Roe"])!);
        Assert.Equal("Madam Coil", (string)m.Invoke(null, ["madam coil"])!);
    }

    [Fact]
    public async Task TheKeybarDoesNotSendAnAshoreCaptainUpThroughTheAirlock()
    {
        DeskBench bench = await DeskBench.BootAsync("/map?ashore=1&kaamos=bounce");
        await bench.RenderAsync();
        await bench.RenderAsync();
        Assert.True((bool)bench.Peek("_ashore")!);
        string? hints = (string?)bench.Call("BuildDeckKeyHints");
        Assert.NotNull(hints);
        Assert.DoesNotContain("walk up through the airlock", hints, StringComparison.Ordinal);
        Assert.StartsWith(DeckView.AshoreKeyHints, hints, StringComparison.Ordinal);
    }
}
