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

    private static StoryBeats.Beat[] DeferrableCards(int n) =>
        Beats(StoryBeats.Presentation.Card).Where(StoryBeats.DeferrableWhileInDanger).Take(n).ToArray();

    /// <summary>A queued card obeys the danger hold on PROMOTION too: two calm cards are up and waiting, a fight
    /// starts while the first is read, the first is closed, and the second does not take the screen mid-fight; it
    /// is held, and shows when the fight is over. RED without the hold check in ServeTheStoryQueue.</summary>
    [Fact]
    public void AQueuedCardWaitsOutAFightThatStartedWhileTheFirstWasRead()
    {
        StoryBeats.Beat[] held = DeferrableCards(2);
        Assert.Equal(2, held.Length);
        Pages.Map map = ABench();
        Call(map, "RaiseStoryBeat", held[0], null, null);
        Call(map, "RaiseStoryBeat", held[1], null, null);
        Assert.Equal(held[0], Showing(map));

        SetField(map, "_shipChargesSeconds", 30.0);   // a fight starts while the first is read
        Assert.True((bool)typeof(Pages.Map).GetMethod("CaptainIsInDanger", Hidden)!.Invoke(map, null)!);
        Call(map, "CloseStoryCard");
        Assert.Null(Showing(map));

        SetField(map, "_shipChargesSeconds", null);   // the hold lifts
        Call(map, "AdvanceStoryCards");
        Assert.Equal(held[1], Showing(map));
    }

    /// <summary>Several cooled cards held in danger are ONE moment, and a held card is asked its cadence again
    /// when it drains: they do not show back to back past their cooldown.</summary>
    [Fact]
    public void CooledCardsHeldInDangerDoNotStackPastTheirCooldown()
    {
        StoryBeats.Beat cooled = StoryBeats.Beat.StrangerStandsADrink;
        Assert.Equal(StoryBeats.Cadence.Cooled, StoryBeats.CadenceOf(cooled));
        Assert.True(StoryBeats.DeferrableWhileInDanger(cooled));
        Pages.Map map = ABench();
        SetField(map, "_shipChargesSeconds", 30.0);
        Call(map, "RaiseStoryBeat", cooled, "a", null);
        Call(map, "RaiseStoryBeat", cooled, "b", null);
        Call(map, "RaiseStoryBeat", cooled, "c", null);
        Assert.Single((System.Collections.IList)Field(map, "_deferredBeats")!);

        SetField(map, "_shipChargesSeconds", null);
        Call(map, "AdvanceStoryCards");
        Assert.Equal(cooled, Showing(map));
        Call(map, "CloseStoryCard");
        Call(map, "AdvanceStoryCards");
        Assert.Null(Showing(map));
    }

    private static string? OutcomeOf(object? card) => (string?)card!.GetType().GetField("Item3")!.GetValue(card);

    /// <summary>A found line said right after raising lands on the card JUST raised, not on the one already
    /// showing; a line said with nothing just raised still edits the showing card (the satchel path).</summary>
    [Fact]
    public void AFoundLineLandsOnTheCardJustRaisedEvenWhenAnotherIsAlreadyUp()
    {
        Pages.Map map = ABench();
        Call(map, "RaiseStoryBeat", First, null, null);
        Call(map, "RaiseStoryBeat", Second, null, null);
        Call(map, "SayItWhereTheyAreLooking", "the found line", SpaceSails.Core.PulseRank.Status);

        Assert.Null(OutcomeOf(Field(map, "_storyCard")));
        var waiting = (System.Collections.IList)Field(map, "_storyWaiting")!;
        Assert.Equal("the found line", OutcomeOf(waiting[0]));

        // Nothing just raised: the line edits the card on the glass, as the satchel path always did.
        Call(map, "SayItWhereTheyAreLooking", "the satchel line", SpaceSails.Core.PulseRank.Status);
        Assert.Equal("the satchel line", OutcomeOf(Field(map, "_storyCard")));
    }

    /// <summary>A held cooled card is asked its cadence again at the drain: another of the same moment spoke
    /// while it was held, so it does not follow it past the cooldown.</summary>
    [Fact]
    public void AHeldCooledCardIsAskedItsCooldownAgainWhenItDrains()
    {
        StoryBeats.Beat cooled = StoryBeats.Beat.StrangerStandsADrink;
        Pages.Map map = ABench();
        SetField(map, "_shipChargesSeconds", 30.0);
        Call(map, "RaiseStoryBeat", cooled, "a", null);      // held
        SetField(map, "_shipChargesSeconds", null);
        Call(map, "RaiseStoryBeat", cooled, "b", null);      // speaks now and spends the cooldown
        Assert.Equal(cooled, Showing(map));
        Call(map, "CloseStoryCard");
        Call(map, "AdvanceStoryCards");
        Assert.Null(Showing(map));
    }
}
