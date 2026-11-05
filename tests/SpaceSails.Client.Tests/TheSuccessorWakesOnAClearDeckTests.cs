using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1398 · A Hive death used to carry its excursion over onto the successor's rustbucket: the lift's card (DEAD AIR /
/// THE SHAFT) stayed open over the ship, closing it released the lift's line onto the deck, and the new captain
/// stood at the dead one's lift-lobby coordinates, on no deck at all (so no captain dot was drawn).
///
/// <para>Driven on the real page at the issue's URL (a berth is added: the descent needs one to ride from), pressing
/// the real "…wake up". <b>RED</b> by removing <c>ForgetTheExcursionsGlass()</c> (the card is still open) or the
/// avatar reset after <c>SetDeckForDock(null)</c> (the avatar is still at the lift lobby, y = −152.5).</para>
/// </summary>
public sealed class TheSuccessorWakesOnAClearDeckTests
{
    private const string Url = "/map?dock=selene-gate&death=suffocated&secretlab=1&land=1&floor=2";

    [Fact]
    public async Task ThePressOfWakeUpLeavesNoHiveCardAndPutsTheCaptainAtTheAirlock()
    {
        using var bench = await DeskBench.BootAsync(Url);
        for (int i = 0; i < 100 && bench.Peek("_busted") is null; i++)
        {
            await Task.Delay(100);
            await bench.RenderAsync();
        }

        Assert.NotNull(bench.Peek("_busted"));
        Assert.True(bench.OnSurface, "the bench did not stage a Hive death on a live excursion");
        Assert.NotNull(bench.Peek("_viewObject"));   // the control: the dead excursion DID leave a card up
        Assert.True((double)bench.Peek("_avatarY")! < -100, "the captain died on a Hive floor, far from any ship deck");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node wake = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("busted-close") && !n.Hidden && n.Handlers.ContainsKey("onclick"))
            ?? throw new Xunit.Sdk.XunitException("the death card has no way out to press");
        await bench.PressAsync(wake.Handlers["onclick"]);

        Assert.False(bench.OnSurface);
        Assert.Null(bench.Peek("_viewObject"));
        Assert.Empty((System.Collections.IEnumerable)bench.Peek("_viewObjectWaiting")!);
        Assert.Null(bench.Peek("_storyCard"));
        Assert.False(((SpaceSails.Core.PulseHold)bench.Peek("_held")!).Any);
        Assert.Equal(2.5, (double)bench.Peek("_avatarX")!);
        Assert.Equal(8.0, (double)bench.Peek("_avatarY")!);
    }
}
