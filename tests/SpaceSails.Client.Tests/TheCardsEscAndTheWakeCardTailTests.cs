using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Encores follow-up (#1412 / #1413). Esc reaches the bank prompt and Ilse's card (R1-F67 follow-up), and the
/// scuttle wake card ends with the death card's tail (R1-F68, on the rendered cards).
/// Proven RED by removing, in turn: the <c>_bankPrompt</c> rung, the <c>_finderCard</c> rung (both in
/// <c>Map.Sim.Cancel.cs</c>), and the <c>WakeTail</c> call on the wake card in BustedCard.razor.
/// </summary>
public sealed class TheCardsEscAndTheWakeCardTailTests
{
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";

    [Fact]
    public async Task EscapeCancelsTheBankPrompt()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        bench.Poke("_audioArmed", true);
        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);
        bench.Poke("_bankPrompt", (Pages.Map.BankPromptKind?)Pages.Map.BankPromptKind.Bank);
        await bench.RenderAsync();
        await bench.TypeAsync(keyboard, "Escape");
        Assert.Null(bench.Field("_bankPrompt"));
    }

    [Fact]
    public async Task EscapeClosesTheHavenCarPanelAndLeavesTheDeckWhereItIs()
    {
        // U10-F04 · Esc over the CAR PANEL used to fall through to the helm: the desk switched to Nav and
        // the panel stayed up behind it.
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&ashore=1&havenfloor=-1");
        bench.Poke("_audioArmed", true);
        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);
        bench.Poke("_showLiftPanel", true);
        await bench.RenderAsync();
        object desk = bench.Field("_activeDesk")!;
        await bench.TypeAsync(keyboard, "Escape");
        Assert.Equal(false, bench.Field("_showLiftPanel"));
        Assert.Equal(desk, bench.Field("_activeDesk"));
    }

    [Fact]
    public async Task EscapeIsIlseVargasPass()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        bench.Poke("_audioArmed", true);
        DeskBench.Painted painted = await bench.RenderAsync();
        ulong keyboard = DeskBench.TheKeyboard(painted);
        bench.Poke("_finderCard", (default(FinderCase.Case), false));
        painted = await bench.RenderAsync();
        Assert.Contains(painted.Root.Descendants(), n => n.HasClass("rep-card"));
        await bench.TypeAsync(keyboard, "Escape");
        painted = await bench.RenderAsync();
        Assert.Null(bench.Field("_finderCard"));
        Assert.DoesNotContain(painted.Root.Descendants(), n => n.HasClass("rep-card"));
    }

    [Fact]
    public async Task TheScuttleWakeCardEndsWithTheDeathCardsTail()
    {
        // #1450 · The REAL derelict path: land on a wreck, and die there of her scuttle through the surface
        // death trigger (the idiom of ADeadCaptainDoesNotBoardTests). Place is derived from the wreck, never poked.
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&wreck=drivefailure&land=1");
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);
        bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", bench.Peek("_surface"), false, DeathCause.Scuttled, false);
        object busted = bench.Field("_busted")!;
        Assert.Equal(DeathPlace.Derelict, busted.GetType().GetProperty("Place")!.GetValue(busted));
        DeskBench.Painted painted = await bench.RenderAsync();
        string quote = painted.Root.Descendants().First(n => n.HasClass("busted-quote")).Spoken;
        const string tail = "Her log will not mention it.";
        Assert.EndsWith(tail, quote);

        DeskBench.Painted.Node wake = painted.Root.Descendants()
            .First(n => n.Element == "button" && n.HasClass("busted-close") && n.Handlers.ContainsKey("onclick"));
        await bench.PressAsync(wake.Handlers["onclick"]);
        painted = await bench.RenderAsync();
        string line = painted.Root.Descendants().First(n => n.HasClass("busted-whathappened-line")).Spoken;
        Assert.EndsWith(tail, line);
    }
}
