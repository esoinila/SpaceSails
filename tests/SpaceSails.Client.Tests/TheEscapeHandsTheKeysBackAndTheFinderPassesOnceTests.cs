using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Playtest encore 1 (#1412) · two card-chrome findings.
///
/// <para><b>R1-F35.</b> A card button clicked with the mouse holds DOM focus; Esc closes the card, the button
/// leaves the DOM, focus falls to &lt;body&gt; and E / WASD go deaf until the deck is clicked. Esc must do
/// what the Done path does and hand focus home to <c>.map-page</c>. <b>Proven RED</b> by removing the
/// <c>RefocusMap</c> call from the Escape rung: the focus log stays empty.</para>
///
/// <para><b>R1-F67.</b> Ilse Varga's card drew a full-width "Pass" and the shell's own small "Pass" way out,
/// two controls for one answer. One remains. <b>Proven RED</b> by putting the body button back.</para>
/// </summary>
public sealed class TheEscapeHandsTheKeysBackAndTheFinderPassesOnceTests
{
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";

    [Fact]
    public async Task EscapeClosingACardHandsTheKeyboardBackToTheDeck()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        bench.Poke("_audioArmed", true);
        bench.WatchTheFocus();
        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);
        Assert.True(keyboard != 0, "the page drew no keyboard host.");

        bench.Poke("_oracleStation", bench.Field("_dockedHavenId"));
        bench.Poke("_oracleOpen", true);
        painted = await bench.RenderAsync();
        Assert.True((bool)bench.Field("_oracleOpen")!);

        bench.ForgetTheFocus();
        await bench.TypeAsync(keyboard, "Escape");
        painted = await bench.RenderAsync();
        Assert.False((bool)bench.Field("_oracleOpen")!, "Esc did not close her card.");

        var host = (ElementReference)bench.Field("_focusableDiv")!;
        Assert.True(bench.Focused.Count > 0,
            "Esc closed the card and nothing handed focus back to .map-page - E and WASD are dead until a click.");
        Assert.Equal(host.Id, bench.Focused[^1]);
    }

    [Fact]
    public async Task IlseVargasCardHasOneWayToPass()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        bench.Poke("_finderCard", (default(FinderCase.Case), false));
        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants().First(n => n.HasClass("rep-card"));

        int passes = card.Descendants().Count(n => n.Element == "button" && n.Name.Trim() == "Pass");
        Assert.True(passes <= 1, $"her card draws {passes} 'Pass' controls - one answer, one control.");
        // The way out is still there (the shell's), and 'Take the job' still stands.
        Assert.Contains(card.Descendants(), n => n.HasClass("view-object-close"));
        Assert.Contains(card.Descendants(), n => n.Name.Contains("Take the job"));
    }
}
