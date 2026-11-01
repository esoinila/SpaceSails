using System;
using System.IO;
using System.Linq;
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

    private static void SetField(Pages.Map map, string name, object? value) =>
        (typeof(Pages.Map).GetField(name, Hidden) ?? throw new InvalidOperationException(name)).SetValue(map, value);

    private static StoryBeats.Beat[] Beats(StoryBeats.Presentation p) =>
        Enum.GetValues<StoryBeats.Beat>().Where(b => StoryBeats.PresentationOf(b) == p
            && StoryBeats.CadenceOf(b) != StoryBeats.Cadence.Cooled).ToArray();

    private static StoryBeats.Beat? Plate(Pages.Map map) =>
        Field(map, "_storyPlate") is { } p ? (StoryBeats.Beat)p.GetType().GetField("Item1")!.GetValue(p)! : null;

    /// <summary>FOUR cards raised on one press come out in the order raised, one per close.</summary>
    [Fact]
    public void ManyCardsRaisedTogetherAllComeOutInOrder()
    {
        StoryBeats.Beat[] cards = Beats(StoryBeats.Presentation.Card).Take(4).ToArray();
        Assert.Equal(4, cards.Length);
        Pages.Map map = ABench();
        foreach (StoryBeats.Beat b in cards)
        {
            Call(map, "RaiseStoryBeat", b, null, null);
        }

        foreach (StoryBeats.Beat b in cards)
        {
            Assert.Equal(b, Showing(map));
            Call(map, "CloseStoryCard");
        }

        Assert.Null(Showing(map));
    }

    /// <summary>Two plates raised under ONE scrim are both seen, in order, and neither replaces a plate that
    /// is still on its clock. RED without the `_storyPlate is null` guard in ServeTheStoryQueue.</summary>
    [Fact]
    public void TwoPlatesRaisedUnderOneScrimAreBothSeenInOrderAndNeverReplaceALivePlate()
    {
        StoryBeats.Beat[] plates = Beats(StoryBeats.Presentation.Plate).Take(2).ToArray();
        Assert.Equal(2, plates.Length);
        Pages.Map map = ABench();
        SetField(map, "_navHelpOpen", true);   // a scrim is up
        Call(map, "RaiseStoryBeat", plates[0], "a", null);
        Call(map, "RaiseStoryBeat", plates[1], "b", null);
        Assert.Null(Plate(map));

        SetField(map, "_navHelpOpen", false);
        Call(map, "AdvanceStoryCards");
        Assert.Equal(plates[0], Plate(map));

        Call(map, "AdvanceStoryCards");
        Call(map, "AdvanceStoryCards");
        Assert.Equal(plates[0], Plate(map));   // the second did not flash past the first

        SetField(map, "_storyPlate", null);    // the first runs its clock out
        Call(map, "AdvanceStoryCards");
        Assert.Equal(plates[1], Plate(map));
    }

    /// <summary>#1396's frame path: the plate is raised under a scrim, is not shown, and is drawn on the first
    /// frame after the scrim goes. RED without the per-frame serve in AdvanceStoryCards.</summary>
    [Fact]
    public void APlateRaisedUnderAScrimIsDrawnOnTheFrameAfterTheScrimGoes()
    {
        Pages.Map map = ABench();
        SetField(map, "_navHelpOpen", true);
        Call(map, "RaiseStoryBeat", APlate, "x", null);
        Assert.Null(Plate(map));
        Call(map, "AdvanceStoryCards");
        Assert.Null(Plate(map));              // still under the scrim: still waiting

        SetField(map, "_navHelpOpen", false);
        Call(map, "AdvanceStoryCards");
        Assert.Equal(APlate, Plate(map));
    }

    /// <summary>#1379/#1395's family: a new life starts with no card on the glass and none waiting.</summary>
    [Fact]
    public void ANewLifeStartsWithAnEmptyLine()
    {
        Pages.Map map = ABench();
        Call(map, "RaiseStoryBeat", First, null, null);
        Call(map, "RaiseStoryBeat", Second, null, null);
        Call(map, "ForgetThePageSessionRiders");
        Assert.Null(Showing(map));
        Assert.Empty((System.Collections.IEnumerable)Field(map, "_storyWaiting")!);
    }

    private static SpaceSails.Client.Rendering.DeckPlan.ConsoleSpot Spot(string label) =>
        new(SpaceSails.Client.Rendering.DeckPlan.ConsoleKind.ViewObject, 0f, 0f, label, "", "caption");

    /// <summary>#1395 · THE SHAFT, then the gate's card. The ride raises both on one arrival through
    /// RaiseTheRideCard; the first-descent card is on the glass first and the second appears when it is closed.
    /// RED with the slot written directly (the second overwrites the first).</summary>
    [Fact]
    public void TheRidesCardsFollowEachOtherInsteadOfOverwriting()
    {
        Pages.Map map = ABench();
        Call(map, "RaiseTheRideCard", Spot("THE SHAFT"));
        Call(map, "RaiseTheRideCard", Spot("THE PANEL READS YOUR WALLET"));
        Assert.Equal("THE SHAFT", ((SpaceSails.Client.Rendering.DeckPlan.ConsoleSpot)Field(map, "_viewObject")!).Label);

        Call(map, "CloseViewObject");
        Assert.Equal("THE PANEL READS YOUR WALLET", ((SpaceSails.Client.Rendering.DeckPlan.ConsoleSpot)Field(map, "_viewObject")!).Label);

        Call(map, "CloseViewObject");
        Assert.Null(Field(map, "_viewObject"));
    }

    /// <summary>The ride's cards all go through the queue: no raw write to the slot is left in the ride files
    /// for the arrival beats (the door they used to overwrite each other through).</summary>
    [Fact]
    public void TheRideFilesRaiseTheirCardsThroughTheQueue()
    {
        string root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "src", "SpaceSails.Client"))) { root = Path.GetDirectoryName(root)!; }
        foreach (string f in new[] { "Map.Surface.Hive.Ride.cs", "Map.Surface.Hive.cs" })
        {
            string text = File.ReadAllText(Path.Combine(root, "src", "SpaceSails.Client", "Pages", f));
            Assert.DoesNotContain("_viewObject = new DeckPlan.ConsoleSpot(", text, StringComparison.Ordinal);
        }
    }
}
