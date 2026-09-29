using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1318 · <c>?nerve=N&amp;land=1</c> SETS THE BOOTS DOWN ON N PIPS.
///
/// <para>Booted headless on 2026-09-29: <c>/map?nerve=1&amp;dock=the-tilt&amp;site=0&amp;land=1&amp;reevers=1</c>
/// touched down on nine pips with <i>the airlock closes behind you +1</i> eight times in the ledger, and
/// <c>?nerve=3</c> onto Phobos did the same. The seed is laid before the shuttle goes and the captain is
/// safe for every frame of the ride, so the ease handed the gauge back before the row's beat could start.
/// This bench runs no frames, so the ride's eases are put on the page by hand, exactly as the frames left
/// them — and the landing's own continuation, <c>AutoLandThenStageDeathAsync</c>, has to take them off.</para>
/// </summary>
public sealed class TheNerveSeedHoldsThroughTheDescentTests
{
    [Theory]
    [InlineData("/map?nerve=1&dock=the-tilt&site=0&land=1&reevers=1", 1)]
    [InlineData("/map?nerve=3&dock=the-space-bar&body=phobos&site=0&land=1", 3)]
    [InlineData("/map?nerve=0&dock=the-tilt&site=0&land=1", 0)]
    public async Task TheGround_ReadsTheSeed_NotTheRidesEases(string url, int seed)
    {
        using DeskBench bench = await DeskBench.BootAsync(url);
        Assert.True(bench.OnSurface, $"{url}: the bench never landed, so nothing here can be asked.");

        // What the ride's frames left on a real page: the gauge eased back to nine, one line per pip.
        bench.Poke("_nerve", NervePips.FromPips(9));
        var eased = Enumerable.Repeat(
            new NervePips.Event(NervePips.Cause.Airlock, -1, NervePips.AirlockEaseName), 9 - seed).ToList();
        bench.Poke("_nerveLedger", (IReadOnlyList<NervePips.Event>)eased);

        // The landing's continuation, run again on a landed page: the descent itself early-outs (the boots
        // are down), and what follows it is what this issue is about.
        await (Task)bench.CallOnTheDispatcher("AutoLandThenStageDeathAsync", [null])!;

        Assert.Equal(seed, NervePips.PipsOf((double)bench.Peek("_nerve")!));
        Assert.DoesNotContain(
            (IReadOnlyList<NervePips.Event>)bench.Peek("_nerveLedger")!,
            e => e.Cause == NervePips.Cause.Airlock);
    }

    [Fact]
    public async Task WithoutASeed_TheLandingLeavesTheGaugeAlone()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");
        bench.Poke("_nerve", NervePips.FromPips(4));

        await (Task)bench.CallOnTheDispatcher("AutoLandThenStageDeathAsync", [null])!;

        Assert.Equal(4, NervePips.PipsOf((double)bench.Peek("_nerve")!));
    }
}
