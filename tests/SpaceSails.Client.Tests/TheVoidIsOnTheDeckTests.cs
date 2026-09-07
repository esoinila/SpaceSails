using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #537 slice 3 · THE VOID, ON A DECK SOMEBODY CAN ACTUALLY STAND IN.
///
/// <para>Core can prove the rules are right about a world nobody built — it cannot prove the world was built.
/// This lane has already lost a feature to exactly that gap: the found plate was <c>AppendRegion</c>-ed onto
/// the live plan, so the one find of a twenty-two-racket search was deleted by the next rebuild (a dogged
/// hatch, a pump run, a purge), and nothing in Core could see it because Core does not rebuild decks.</para>
///
/// <para>So these walk the SHIPPING <see cref="DeckPlan"/> the renderer draws and <c>[E]</c> dispatches
/// through, and ask the three story-QA questions: is it on screen, can it be reached, and does the sentence
/// match the sim.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheVoidIsOnTheDeckTests
{
    private const double PlateX = -8.0;
    private const double InsideY = (WreckLayout.TopY + WreckLayout.OuterTopY) / 2.0;
    private const double CorridorY = -6.5;

    private static Derelict.Wreck Hull =>
        Derelict.SeededWithCause(Derelict.WreckCause.InsuranceJob) ?? Derelict.Seeded("kestrel-3");

    private static HullSounding.HiddenVoid InTheBand() =>
        new("DEEP HOLD", Outboard: true, PlateX - 3, PlateX + 3, Top: true, PlateX, WreckLayout.TopY,
            HullSounding.VoidFrames * WreckLayout.ShieldingDepth, "A rack of code keys.");

    private static DeckPlan Deck(HullSounding.HiddenVoid? plate, bool open = false, bool shut = false) =>
        WreckInterior.WreckDeck(
            Hull, new HashSet<string>(), salvaged: false, droidCount: 0,
            fillDroids: (_, _) => { }, heldDoors: null, blockedDoors: null,
            archiveAboard: false, archivePurged: false,
            plate: plate, voidOpen: open, plateShut: shut);

    private static DeckPlan.ConsoleSpot[] Plates(DeckPlan deck) =>
        [.. deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.SecretDoor)];

    private static bool CanWalkInto(DeckPlan deck) =>
        DeckReachability.Path(
            new DeckReachability.Point(PlateX, CorridorY),
            new DeckReachability.Point(PlateX, InsideY),
            deck.CollisionSegments, DeckPlan.AvatarRadius,
            (PlateX - 12, WreckLayout.OuterTopY - 1, PlateX + 12, -2.0), step: 0.25).Reached;

    // ── 1 · It exists on screen — and only once somebody has knocked ─────────────────────────────────

    /// <summary>
    /// THE ANTI-TELL, AND THE BUG THAT ATE THE FIND, IN ONE PAIR. A plate console standing on the deck from
    /// boarding hands the whole search to anybody who walks past; a plate console that only ever lived on
    /// the live plan is gone the next time anything rebuilds it. It is state now, so it is drawn from state
    /// — every time.
    /// </summary>
    [Fact]
    public void ThePlateAppearsOnlyWhenFoundAndThenOnEveryRebuild()
    {
        Assert.Empty(Plates(Deck(null)));

        foreach (DeckPlan rebuilt in new[] { Deck(InTheBand()), Deck(InTheBand()), Deck(InTheBand()) })
        {
            DeckPlan.ConsoleSpot spot = Assert.Single(Plates(rebuilt));
            Assert.Equal((float)PlateX, spot.X);
            Assert.Equal((float)WreckLayout.TopY, spot.Y);
        }
    }

    /// <summary>One console, three faces, and the face says which of the three verbs the press will spend.
    /// A fixture that offers "get in" and "get out" behind the same words is a control the player has to
    /// learn by pressing.</summary>
    [Fact]
    public void ThePlateSaysWhichVerbItIsOfferingRightNow()
    {
        string found = Assert.Single(Plates(Deck(InTheBand()))).Label;
        string open = Assert.Single(Plates(Deck(InTheBand(), open: true))).Label;
        string inside = Assert.Single(Plates(Deck(InTheBand(), open: true, shut: true))).Label;

        Assert.Equal(3, new HashSet<string> { found, open, inside }.Count);
        Assert.Equal(HullStowage.PlateLabel(false, false), found);
        Assert.Equal(HullStowage.PlateLabel(true, false), open);
        Assert.Equal(HullStowage.PlateLabel(true, true), inside);
    }

    // ── 2 · It can be walked into ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WHOLE SLICE, ON THE SHIPPED DECK. A captain gets into the shielding band once the plate is cut
    /// and not before, and a plate pulled to behind him is a wall again on the very collision field the
    /// renderer draws and the sweep team's eye reads.
    /// </summary>
    [Fact]
    public void TheBandIsWalkableOnlyThroughAPlateThatIsActuallyOut()
    {
        Assert.False(CanWalkInto(Deck(InTheBand())), "a found plate is still a plate");
        Assert.True(CanWalkInto(Deck(InTheBand(), open: true)), "a cut plate is a way in");
        Assert.False(CanWalkInto(Deck(InTheBand(), open: true, shut: true)), "…and it goes back");
    }

    /// <summary>A HAND'S WIDTH OF PIPEWORK NEVER BECOMES A ROOM. Opening a bulkhead-run void yields its
    /// contents and changes no geometry at all — there was never anywhere in there to stand, and a deck that
    /// quietly widened one would be the map lying about the ship.</summary>
    [Fact]
    public void ABulkheadRunOpensAndTheDeckDoesNotMove()
    {
        HullSounding.HiddenVoid inAWall = InTheBand() with { Outboard = false };

        Assert.Equal(Deck(inAWall).CollisionSegments.Length,
                     Deck(inAWall, open: true).CollisionSegments.Length);
        Assert.Equal(Deck(inAWall).Structures.Length, Deck(inAWall, open: true).Structures.Length);
    }

    // ── 3 · The sentence matches the sim ────────────────────────────────────────────────────────────

    /// <summary>The captain draws what he knows. The band is one solid run until he has been inside it, and
    /// then only the stretch he has been inside stops being drawn solid — the rest of it is still a guess,
    /// and a hidden space drawn as a space is not hidden.</summary>
    [Fact]
    public void OnlyTheStretchHeHasBeenInsideIsDrawnAsSpace()
    {
        DeckPlan closed = Deck(InTheBand());
        DeckPlan cut = Deck(InTheBand(), open: true);

        Assert.Equal(closed.Structures.Length + 1, cut.Structures.Length);
        Assert.Contains(closed.Structures, s => s.X0 == WreckLayout.TransomX
                                                && s.X1 == WreckLayout.ShieldingForwardEnd
                                                && s.Y1 == WreckLayout.TopY);
        Assert.DoesNotContain(cut.Structures, s => s.X0 == WreckLayout.TransomX
                                                   && s.X1 == WreckLayout.ShieldingForwardEnd
                                                   && s.Y1 == WreckLayout.TopY);
    }

    /// <summary>And the header line says where he is. Standing in a pocket labelled THE HULL would be the
    /// repo's third named bug class in a header: the sim doing one thing while a sentence reports another.</summary>
    [Fact]
    public void TheHeaderNamesThePocketOnceItIsOneAndNotBefore()
    {
        Assert.Equal(HullStowage.PocketName, Deck(InTheBand(), open: true).Location(PlateX, InsideY));
        Assert.NotEqual(HullStowage.PocketName, Deck(InTheBand()).Location(PlateX, InsideY));

        // …and the room the plate is on the wall of is still itself, from one step inboard of it.
        Assert.Equal("DEEP HOLD", Deck(InTheBand(), open: true).Location(PlateX, CorridorY));
    }

    /// <summary>The pocket is named on the map too, once it is known — a place with a wall round it and no
    /// name on it reads as a drawing error rather than as a find.</summary>
    [Fact]
    public void ThePocketGetsALabelOnceItIsKnown()
    {
        Assert.DoesNotContain(Deck(InTheBand()).RoomLabels, l => l.Text == HullStowage.PocketName);
        Assert.Contains(Deck(InTheBand(), open: true).RoomLabels, l => l.Text == HullStowage.PocketName);
    }
}
