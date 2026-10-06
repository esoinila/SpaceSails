using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1363 finding 2 · A SEAT-CHEAT BOOTS THE ROOM, NOT THE TUTORIAL. On a fresh profile <c>?spread=1</c> and
/// <c>?counter=1</c> put the 🛗 THE SHAFT story card and the FIRST TIME ON THE GROUND lesson over the seated
/// scene, so the guide's "no backdrop, no card" was only true of a profile that had already ridden the lift.
/// The seat-cheats now spend both latches (the SAME ones the real flow writes) before the scene is built.
///
/// <para>The other half is the scope law: a real first landing and a landing cheat that stands the captain at
/// a gate keep their cards exactly as shipped.</para>
/// </summary>
public sealed class TheSeatCheatsBootCleanTests
{
    /// <summary>Every ashore seat-cheat that rides the hive, and each of the implied routes.</summary>
    public static TheoryData<string> SeatCheats =>
    [
        "/map?dock=selene-gate&tablescene=1",
        "/map?dock=selene-gate&tablescene=free",
        "/map?dock=selene-gate&spread=1",
        "/map?dock=selene-gate&rip=1",
        "/map?dock=selene-gate&threads=1",
        "/map?dock=selene-gate&counter=1",
        "/map?dock=selene-gate&stool=1",
        "/map?dock=selene-gate&park=1",
        "/map?dock=selene-gate&park=1&spread=1",
        // The berth seat-cheat never lands and never rides the hive: it must stay card-free on its own.
        "/map?barcase=1",
    ];

    /// <summary><b>NO CARD ON THE FIRST FRAME.</b> Neither the lesson flag, nor the establishing card, nor
    /// the drawn lesson card is up. <b>RED PROOF:</b> make <c>ASeatCheatIsBooting</c> return false and every
    /// hive row fails on the open lesson.</summary>
    [Theory]
    [MemberData(nameof(SeatCheats))]
    public async Task NoFirstRunCardOverlaysTheSeatedScene(string url)
    {
        using DeskBench bench = await DeskBench.BootAsync(url);

        Assert.False((bool)bench.Peek("_groundLessonOpen")!, $"{url}: the FIRST TIME ON THE GROUND lesson is up");
        Assert.Null(bench.Peek("_viewObject"));   // the 🛗 THE SHAFT card rides this slot

        DeskBench.Painted painted = await bench.RenderAsync();
        Assert.DoesNotContain(painted.ClassLists, c => c.Contains("ground-lesson-card"));
    }

    /// <summary><b>A REAL FIRST LANDING KEEPS ITS LESSON.</b> No cheat seat, a fresh captain, the shuttle sets
    /// them down: the card is up and the latch is written. <b>RED PROOF:</b> mark the lesson seen
    /// unconditionally (drop the <c>ASeatCheatIsBooting</c> test in the Spend method, and call it from the
    /// plain landing) and this fails on the missing card.</summary>
    [Fact]
    public async Task ARealFirstLandingStillRaisesTheGroundLesson()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");

        Assert.True(bench.OnSurface, "premise: the shuttle set the captain down");
        Assert.True((bool)bench.Peek("_groundLessonOpen")!, "a first landing must still raise the lesson");
        Assert.True((bool)bench.Peek("_groundLessonSeen")!, "…and write the latch it always wrote");
    }

    /// <summary><b>A STANDING CHEAT KEEPS BOTH CARDS.</b> <c>?frontdoor=1</c> stands the captain on the corridor
    /// outside the hall, not in a seat, and rides down in the cage: the ground lesson is up and the
    /// first-descent card is the one in the slot — the first lift ride as shipped.</summary>
    [Fact]
    public async Task AStandingCheatThatRidesTheLiftKeepsTheShaftCard()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=selene-gate&frontdoor=1");

        Assert.True(bench.OnSurface, "premise: landed");
        Assert.True((bool)bench.Peek("_groundLessonOpen")!, "the standing cheat keeps the first-ground lesson");
        object? card = bench.Peek("_viewObject");
        Assert.NotNull(card);
        Assert.Contains(UndergroundComplex.DescentCardLabel,
            card!.ToString() ?? "", System.StringComparison.Ordinal);
    }
}
