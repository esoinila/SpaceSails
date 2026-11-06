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
}
