using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1379 / #1395 / #1396 · THE STORY-CARD SLOT IS A LINE. Two cards raised on the same press used to share one
/// slot, so the second overwrote the first (the arc capstones, the shaft under the wallet read). The slot is
/// the head of a FIFO now: cards show one at a time, in the order raised, and closing one shows the next.
/// A plate raised under a scrim waits for the glass to clear instead of being drawn beneath it.
///
/// <para><b>RED</b> by restoring <c>_storyCard = (beat, subject, outcome)</c> unconditionally in the Card arm
/// of <c>ShowStoryBeat</c>: the second assertion of the order test sees the second card where the first
/// should be, and the third finds nothing waiting.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheStoryCardsQueueAndNeverSwallowEachOtherTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const StoryBeats.Beat First = StoryBeats.Beat.CrewDeputation;
    private const StoryBeats.Beat Second = StoryBeats.Beat.ArchivePurged;
    private const StoryBeats.Beat APlate = StoryBeats.Beat.Flashback;

    private static Pages.Map ABench()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        return map;
    }

    private static void Call(Pages.Map map, string name, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(name, Hidden) ?? throw new InvalidOperationException(name)).Invoke(map, args);

    private static object? Field(Pages.Map map, string name) =>
        (typeof(Pages.Map).GetField(name, Hidden) ?? throw new InvalidOperationException(name)).GetValue(map);

    private static StoryBeats.Beat? Showing(Pages.Map map) =>
        Field(map, "_storyCard") is { } c ? (StoryBeats.Beat)c.GetType().GetField("Item1")!.GetValue(c)! : null;

    [Fact]
    public void TheBenchRaisesTwoDistinctCardsAndAPlate()
    {
        Assert.Equal(StoryBeats.Presentation.Card, StoryBeats.PresentationOf(First));
        Assert.Equal(StoryBeats.Presentation.Card, StoryBeats.PresentationOf(Second));
        Assert.Equal(StoryBeats.Presentation.Plate, StoryBeats.PresentationOf(APlate));
        Assert.NotEqual(First, Second);
    }

    [Fact]
    public void TwoCardsRaisedOnOnePressAreBothSeenInTheOrderRaised()
    {
        Pages.Map map = ABench();
        Call(map, "RaiseStoryBeat", First, null, null);
        Call(map, "RaiseStoryBeat", Second, null, "the receipt");

        Assert.Equal(First, Showing(map));

        Call(map, "CloseStoryCard");
        Assert.Equal(Second, Showing(map));
        Assert.Equal("the receipt",
            Field(map, "_storyCard")!.GetType().GetField("Item3")!.GetValue(Field(map, "_storyCard")));

        Call(map, "CloseStoryCard");
        Assert.Null(Showing(map));
    }

    [Fact]
    public void APlateRaisedUnderACardWaitsForTheGlassAndIsThenShown()
    {
        Pages.Map map = ABench();
        Call(map, "RaiseStoryBeat", First, null, null);
        Call(map, "RaiseStoryBeat", APlate, "x", null);

        Assert.Null(Field(map, "_storyPlate"));

        Call(map, "CloseStoryCard");
        Assert.NotNull(Field(map, "_storyPlate"));
    }
}
