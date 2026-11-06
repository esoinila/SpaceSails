using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// R1-F65 · A TOAST MAY NOT SIT ON AN OPEN CARD'S TITLE. With a card up the pulse toast takes the
/// <c>deck-pulse-toast-low</c> modifier (foot of the glass); with none it stays where it always was. Driven on
/// the shipping page, rendered, and read off the painted tree — the class IS the layout switch.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AToastClearsTheOpenCardTests
{
    private const string Ashore = "/map?oracle=1&ashore=1";

    private static async Task<DeskBench.Painted.Node> ToastAsync(DeskBench b)
    {
        b.Poke("_pulse", PulseSlot.Empty.Write("a line the captain should read", PulseRank.Status, 0.0));
        DeskBench.Painted painted = await b.RenderAsync();
        return Assert.Single(painted.Root.Descendants(), n => n.HasClass("deck-pulse-toast"));
    }

    [Fact]
    public async Task WithACardOpenTheToastDropsClearOfTheTitle()
    {
        DeskBench b = await DeskBench.BootAsync(Ashore);
        b.CallOnTheDispatcher("TalkToOracle");
        Assert.True((bool)b.Peek("_oracleOpen")!, "premise: her corner card is up");
        Assert.NotNull(b.Peek("_oracleLine"));   // premise: the oracle card is really on the glass
        Assert.True((await ToastAsync(b)).HasClass("deck-pulse-toast-low"));
    }

    [Fact]
    public async Task WithNoCardOpenTheToastStaysAtTheTop()
    {
        DeskBench b = await DeskBench.BootAsync(Ashore);
        Assert.False((await ToastAsync(b)).HasClass("deck-pulse-toast-low"));
    }

    /// <summary>R1-F65 · The issue's own repro: Barkeep, then a purchase. The receipt is said on the counter card's own
    /// notice (with its cost) and NOT also as a toast that lands on the card's title and action row.</summary>
    [Fact]
    public async Task APurchaseWithTheBarCardOpenLandsInTheCardsOwnNoticeNotInATitleToast()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1");
        b.Poke("_credits", 5000);
        b.Poke("_barMenu", Core.Interior.Barkeeps.For((string)b.Peek("_dockedHavenId")!));
        Assert.NotNull(b.Peek("_barMenu"));   // premise: the counter card is up
        b.Poke("_pulse", PulseSlot.Empty);
        b.CallOnTheDispatcher("BuyHouseSpecial");
        Assert.Null(((PulseSlot)b.Peek("_pulse")!).Message);
        string notice = (string)b.Peek("_barNotice")!;
        Assert.Contains(" cr)", notice);
    }

    /// <summary>R1-F65 · Static Marsh's true-line answer rides her own card (the oracle notice), not a toast.</summary>
    [Fact]
    public async Task AnAnswerSaidWhileTheOraclesCardIsOpenLandsOnHerCard()
    {
        DeskBench b = await DeskBench.BootAsync(Ashore);
        b.CallOnTheDispatcher("TalkToOracle");
        b.Poke("_pulse", PulseSlot.Empty);
        b.CallOnTheDispatcher("SayItWhereTheyAreLooking", "a true line, said where she is looking", PulseRank.Status);
        Assert.Null(((PulseSlot)b.Peek("_pulse")!).Message);
        Assert.Equal("a true line, said where she is looking", (string)b.Peek("_oracleNotice")!);
    }
}
