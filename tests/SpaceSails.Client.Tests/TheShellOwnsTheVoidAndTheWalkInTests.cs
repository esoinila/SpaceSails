using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 2 · <b>THE TWO SURFACES THIS WAVE MOVED, ASKED THE QUESTIONS THE MOVE COULD HAVE BROKEN.</b>
///
/// <para>#997 gave this client one minimise mechanism and one way-out mechanism. Wave 2 puts the last
/// hand-rolled minimise on it — <c>.jump-overlay</c>, which #992 implemented for the THIRD time — and moves
/// the walk-in card next to the rep's card it shares its markup with. Neither surface may move a pixel or a
/// word, and one of them is a woman standing at your table whose lines are canon.</para>
///
/// <para><b>What is asked here that the dismissibility law does not ask.</b> #992's law asks whether a
/// surface can be got rid of. These two rows can be, and were before this wave. What can break in a
/// MIGRATION is different and more specific: whether the tile comes BACK, whether the thing that was in the
/// window is still in it (the M26 rule: a recreated subtree is a dead canvas), whether the crossing went on
/// running while the captain was looking at his map, and whether the way out of her card is still a DIRECT
/// child of the card — which is not a nicety but the relation
/// <c>::deep .view-object &gt; .view-object-close</c> pins the family's sticky foot with.</para>
/// </summary>
[SlowGate] // #251 · 15 s over 4 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheShellOwnsTheVoidAndTheWalkInTests
{
    private const string FreeFlying = "/map?start=wreck";
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";

    // ── The void sheet ────────────────────────────────────────────────────────────────────────────────

    private static void ACrossingIsUnderWay(DeskBench bench)
    {
        bench.Poke("_voidCardTucked", false);
        bench.Poke("_jumpInProgress", true);
        bench.Poke("_jumpTotalYears", 6);
        bench.Poke("_jumpYear", 2);
        bench.Poke("_jumpDestName", "Barnard's Reach");
        bench.Poke("_jumpFlavor", "the bus does not stop out here");
        bench.Poke("_jumpActive", true);
    }

    private static DeskBench.Painted.Node? Wearing(DeskBench.Painted painted, string css) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(css));

    private static DeskBench.Painted.Node? Control(DeskBench.Painted painted, string named) =>
        painted.Root.Descendants().FirstOrDefault(
            n => !n.Hidden && n.Handlers.ContainsKey("onclick") && n.Name == named);

    /// <summary>
    /// MINIMISE, TILE, RESTORE — MID-CROSSING, AND THE CROSSING NEVER NOTICED.
    ///
    /// <para>The round trip through the SHELL's mechanism, on the surface that used to own three lines of
    /// its own to do it. Two halves, and both of them are things the old shape could get wrong: the tile
    /// must bring the sheet back (#992's tile was a separate element gated on the same flag, so a flag that
    /// stopped agreeing with either gate left the captain with neither), and the tuck must be a decision
    /// about the SCREEN — every field the crossing runs on is read back afterwards and must be untouched.
    /// </para>
    /// </summary>
    [Fact]
    public async Task TheVoidSheetTucksIntoItsTileAndComesBackWithTheCrossingUntouched()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        ACrossingIsUnderWay(bench);

        DeskBench.Painted open = await bench.RenderAsync();
        DeskBench.Painted.Node? sheet = Wearing(open, "jump-overlay");
        Assert.True(sheet is not null && !sheet.Hidden,
            "the crossing is under way and no .jump-overlay reached the screen. The register's driver and "
            + "the markup's gate have come apart.");
        Assert.Contains(LongHaul.VoidYearLine(2, 6), sheet!.Spoken);
        Assert.Null(Wearing(open, "jump-overlay-tile"));

        DeskBench.Painted.Node? minimise = Control(open, "–");
        Assert.True(minimise is not null,
            "the void sheet drew no – at all. It is the owner's ruling of 2026-08-24 failing on the surface "
            + "#992 called the worst offender in the client.");

        await bench.PressAsync(minimise!.Handlers["onclick"]);
        DeskBench.Painted tucked = await bench.RenderAsync();

        // The sheet is off the screen and the tile is up — and it is the SAME element wearing the other
        // name, which is why nothing below it was recreated.
        Assert.Null(Wearing(tucked, "jump-overlay"));
        DeskBench.Painted.Node? tile = Wearing(tucked, "jump-overlay-tile");
        Assert.True(tile is not null && !tile.Hidden,
            "pressing – took the sheet away and put nothing in its place. A crossing with no tile is a "
            + "crossing the captain cannot get back to.");

        // THE TILE IS AN INSTRUMENT, NOT A MYSTERY BUTTON: it names what is still running, off the same
        // field the sheet reads, so the two cannot disagree about which year it is.
        Assert.Contains($"🚀 {LongHaul.VoidYearLine(2, 6)}", tile!.Spoken);

        // …and what was in the window is STILL in the window, drawn and hidden rather than destroyed. This
        // is the M26 rule the scope paid for in a dark eyepiece, asked of the void sheet.
        DeskBench.Painted.Node? words = Wearing(tucked, "jump-overlay-inner");
        Assert.True(words is not null && words.Hidden,
            "the sheet's body left the tree when it tucked. The shell tucks by swapping a class list "
            + "precisely so nothing below it is ever recreated.");
        Assert.Contains(LongHaul.VoidYearLine(2, 6), words!.Spoken);

        // THE WORLD DID NOT MOVE. Tucking a card is a decision about the screen; cancelling a crossing is a
        // decision about the world, and this surface still offers no way to make it.
        Assert.True((bool)bench.Peek("_jumpActive")!, "tucking the sheet ended the crossing.");
        Assert.True((bool)bench.Peek("_jumpInProgress")!, "tucking the sheet unfroze the tick mid-void.");
        Assert.Equal(2, (int)bench.Peek("_jumpYear")!);
        Assert.Equal(6, (int)bench.Peek("_jumpTotalYears")!);
        Assert.True((bool)bench.Peek("_voidCardTucked")!,
            "the page's own memory of the tuck was not written. A captain who puts the void away expects it "
            + "to stay away.");

        // …and the tile brings it back.
        DeskBench.Painted.Node? back = tile.Descendants().Append(tile)
            .FirstOrDefault(n => n.Handlers.ContainsKey("onclick") && !n.Hidden);
        Assert.True(back is not null, "the tile is drawn and nothing in it can be pressed.");

        await bench.PressAsync(back!.Handlers["onclick"]);
        DeskBench.Painted again = await bench.RenderAsync();

        DeskBench.Painted.Node? reopened = Wearing(again, "jump-overlay");
        Assert.True(reopened is not null && !reopened.Hidden,
            "the tile did not bring the crossing back. A minimise whose tile does not restore is a close "
            + "wearing a minimise's clothes.");
        Assert.Contains(LongHaul.VoidYearLine(2, 6), reopened!.Spoken);
        Assert.Contains(LongHaul.VoidBound("Barnard's Reach"), reopened.Spoken);
        Assert.Null(Wearing(again, "jump-overlay-tile"));
        Assert.False((bool)bench.Peek("_voidCardTucked")!);
        Assert.True((bool)bench.Peek("_jumpActive")!, "the round trip ended the crossing.");
    }

    /// <summary>
    /// ONE MECHANISM, AND THE COAST SKIP IS ON IT TOO. The computed skip wears the same class as the long
    /// haul and inherited the same fault; #992 gave it the same hand-rolled tile off the same switch. It is
    /// the same shell now, which is the whole claim of the wave: two surfaces, one gesture, no second
    /// implementation to drift.
    /// </summary>
    [Fact]
    public async Task TheCoastBeatTucksThroughTheSameMechanismIntoTheSameTile()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        bench.Poke("_voidCardTucked", false);
        bench.Poke("_jumpActive", false);
        bench.Poke("_coastSkipDays", 40);
        bench.Poke("_coastSkipLabel", "the long coast");
        bench.Poke("_coastSkipActive", true);

        DeskBench.Painted open = await bench.RenderAsync();
        DeskBench.Painted.Node? sheet = Wearing(open, "jump-overlay");
        Assert.True(sheet is not null && !sheet.Hidden, "the coast beat drew nothing.");
        Assert.Contains(WarpSkip.CoastConsumedTitle, sheet!.Spoken);

        await bench.PressAsync(Control(open, "–")!.Handlers["onclick"]);
        DeskBench.Painted tucked = await bench.RenderAsync();

        DeskBench.Painted.Node? tile = Wearing(tucked, "jump-overlay-tile");
        Assert.True(tile is not null, "the coast beat has no tile — its – took the screen back and gave "
                                     + "nothing to return to.");
        Assert.Contains($"⏭ {WarpSkip.CoastConsumedBound("the long coast")}", tile!.Spoken);
        Assert.True((bool)bench.Peek("_coastSkipActive")!, "tucking the beat cancelled the coast.");
        Assert.Equal(40, (int)bench.Peek("_coastSkipDays")!);
    }

    // ── The walk-in card ──────────────────────────────────────────────────────────────────────────────

    private static async Task<DeskBench> SheIsAtYourTable(WalkIn.Who who)
    {
        DeskBench bench = await DeskBench.BootAsync(Docked);
        bench.Poke("_walkInCard", (WalkIn.Who?)who);
        return bench;
    }

    /// <summary>
    /// HER CARD IS THE SHELL'S NOW, AND NOT A CHARACTER OF HER MOVED.
    ///
    /// <para>Every word on this card comes out of <see cref="WalkIn"/> — her name, the line she says at the
    /// table, her story and both of her answers — and this asserts them against that file rather than
    /// against a string typed here, so a chrome refactor cannot quietly reword a scene whose text is canon.
    /// </para>
    ///
    /// <para>The structural half is the one a Frame choice can break. The card is drawn INSIDE the scrim,
    /// which owns the geometry; the shell adds no box between them; and the way out is a DIRECT child of
    /// <c>.view-object</c>, because that is the relation
    /// <c>::deep .view-object &gt; .view-object-close</c> pins the family's sticky foot with. A shell that
    /// wrapped the children would unstick this card's foot and read as correct in every other way.</para>
    /// </summary>
    [Fact]
    public async Task TheWalkInCardIsHostedInItsScrimAndHerWordsAreUntouched()
    {
        using DeskBench bench = await SheIsAtYourTable(WalkIn.Who.Ilse);
        DeskBench.Painted painted = await bench.RenderAsync();

        DeskBench.Painted.Node? scrim = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("view-object-backdrop") && n.HasClass("rep-backdrop"));
        Assert.True(scrim is not null, "she is at the table and her scrim never reached the screen.");

        DeskBench.Painted.Node? card = scrim!.Children.FirstOrDefault(n => n.HasClass("rep-card"));
        Assert.True(card is not null,
            "her card is not a direct child of its own scrim any more — the shell put a box between the two "
            + "and the host no longer owns the geometry.");
        Assert.True(card!.HasClass("view-object"),
            "the card lost its family name. #992's completeness guard reads class attributes as typed, and a "
            + "surface that renames itself through a parameter vanishes from the law's sight.");

        // Her words, read off Core rather than typed here again.
        Assert.Contains(WalkIn.Name(WalkIn.Who.Ilse), card.Spoken);
        Assert.Contains(WalkIn.AtTheTable(WalkIn.Who.Ilse), card.Spoken);
        Assert.Contains(WalkIn.TheStory(WalkIn.Who.Ilse), card.Spoken);

        var answers = card.Descendants().Where(n => n.HasClass("rep-offer")).Select(n => n.Name).ToList();
        Assert.Equal([WalkIn.Yes, WalkIn.No], answers);

        // …and the way out, where the family's own sticky foot can find it.
        DeskBench.Painted.Node? wayOut = card.Children.FirstOrDefault(n => n.HasClass("view-object-close"));
        Assert.True(wayOut is not null,
            "her card has no way out that is not one of her two answers — and a control that is only reachable "
            + "by deciding is the owner's ruling of 2026-08-24 leaning on the exception it was given.");
        Assert.True(wayOut!.Handlers.ContainsKey("onclick"),
            "the way out is drawn and wired to nothing: a control that LOOKS like a way out and is not one.");
    }

    /// <summary>
    /// SHE LEAVES POLITELY, AND THE ROOM IS STILL THERE.
    ///
    /// <para>The dismiss runs her own no-path, entire — #979's exit and not a new one. So her line is said,
    /// the card goes, and the answered flag is set, which is what stops the evening trying to walk her over
    /// again. A ✕ that only took the panel away would leave a woman standing at your elbow with nothing left
    /// to answer and no way to answer it, which is the inverse of the empty-chair card #973 already refuses.
    /// </para>
    ///
    /// <para>And the room underneath survives it: closing her card gives the deck back rather than taking it
    /// with her. The count of pressable controls on the deck is the same before she arrived and after she
    /// has gone.</para>
    /// </summary>
    [Fact]
    public async Task PressingHerWayOutRunsHerOwnNoPathAndGivesTheRoomBack()
    {
        using DeskBench bench = await DeskBench.BootAsync(Docked);
        DeskBench.Painted before = await bench.RenderAsync();
        int roomBefore = Pressable(before);

        bench.Poke("_walkInCard", (WalkIn.Who?)WalkIn.Who.Nadia);
        DeskBench.Painted raised = await bench.RenderAsync();
        DeskBench.Painted.Node card = raised.Root.Descendants().First(n => n.HasClass("rep-card"));
        DeskBench.Painted.Node wayOut = card.Children.First(n => n.HasClass("view-object-close"));

        await bench.PressAsync(wayOut.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.Null(after.Root.Descendants().FirstOrDefault(n => n.HasClass("rep-card")));
        Assert.Null(bench.Peek("_walkInCard"));
        Assert.True((bool)bench.Peek("_walkInAnswered")!,
            "her card went and the evening was never told she had answered — so the room is free to walk her "
            + "over again, at a table she has already left.");
        Assert.Contains(WalkIn.IfNo(WalkIn.Who.Nadia), bench.Pulse);

        Assert.Equal(roomBefore, Pressable(after));
    }

    private static int Pressable(DeskBench.Painted painted) =>
        painted.Root.Descendants().Count(n => n.Handlers.ContainsKey("onclick") && !n.Hidden);
}
