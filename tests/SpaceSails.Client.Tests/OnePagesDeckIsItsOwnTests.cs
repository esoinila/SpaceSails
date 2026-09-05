using System.Reflection;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1119 item 1 · <b>THE SHIP DECK WAS A SINGLETON AND <c>AppendRegion</c> WRITES INTO IT.</b>
///
/// <para><c>DeckPlan.Ship</c> is a <c>{ get; } = BuildShip(null)</c> static, and <c>AppendRegion</c> mutates
/// the plan it is called on rather than returning a new one. <c>Map._deckPlan</c> started life holding that
/// static, so every path that grows the live deck — a forced expedition door, an outpost hatch, Vantar's lab
/// — was one call away from welding that ground onto the process-wide ship plan that the docked complex, the
/// moon surface and the boot warm-up all go on reading.</para>
///
/// <para>The #584 bench found it the loud way: a page still holding the default appended an expedition
/// chamber, and 49 tests went red on a Linux runner reporting the ship drawing 462 marks where 364 were
/// pinned and two identical Vantar consoles 0.00 du apart. Nothing was wrong with the code under test — the
/// world the other suites read had been edited underneath them.</para>
///
/// <para><b>What is driven here.</b> Two shipping <see cref="Map"/> pages, no mocks: a region is appended to
/// one page's live deck exactly the way <c>ComposeSecretLabSite</c> does it, and the OTHER page's deck — and
/// the shared <c>DeckPlan.Ship</c> every read-only asker uses — is asked whether anything moved.</para>
///
/// <para><b>Proven RED</b> on today's code: with <c>_deckPlan = DeckPlan.Ship</c> both pages hold one object,
/// so the append lands on the second page's deck and on the singleton, and all three laws below fail.</para>
/// </summary>
public class OnePagesDeckIsItsOwnTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static DeckPlan DeckOf(Map page) =>
        (DeckPlan)typeof(Map).GetField("_deckPlan", Hidden)!.GetValue(page)!;

    /// <summary>One wall, one console, one label — enough ground that any sharing shows up as a count.</summary>
    private static DeckPlan.DeckRegion ALittleGround() =>
        new(
            [new DeckPlan.Wall(900, 900, 900, 910, false, false)],
            [new DeckPlan.ConsoleSpot(DeckPlan.ConsoleKind.LabConsole, 902, 905, "⚙ TEST BENCH")],
            [(902f, 902f, "TEST BENCH")],
            []);

    /// <summary>LAW 1 · Two pages, two decks. Reference identity is the whole point: <c>AppendRegion</c> is a
    /// mutation, so a shared object is a shared edit however careful either page is.</summary>
    [Fact]
    public void TwoPages_DoNotShareOneDeckObject()
    {
        var a = new Map();
        var b = new Map();

        Assert.False(ReferenceEquals(DeckOf(a), DeckOf(b)),
            "two Map pages booted holding ONE DeckPlan object. AppendRegion mutates the plan it is called " +
            "on, so whatever one page grows, the other page is standing in.");
        Assert.False(ReferenceEquals(DeckOf(a), DeckPlan.Ship),
            "a page's live deck IS the process-wide DeckPlan.Ship singleton — the plan the docked complex, " +
            "the moon surface and the boot warm-up all read as the unchanging ship.");
    }

    /// <summary>LAW 2 · Growing one page's ground leaves the other page's ground exactly as it was. Counted
    /// on walls, collision segments, consoles and labels, because <c>AppendRegion</c> grows all four and a
    /// law that watched only one of them would pass over three quarters of the bug.</summary>
    [Fact]
    public void AppendingToOnePagesDeck_LeavesAnotherPagesDeckAlone()
    {
        var a = new Map();
        var b = new Map();

        DeckPlan other = DeckOf(b);
        int walls = other.Walls.Length;
        int segments = other.CollisionSegments.Length;
        int consoles = other.Consoles.Length;
        int labels = other.RoomLabels.Length;
        int regions = other.AppendedRegionCount;

        DeckOf(a).AppendRegion(ALittleGround());

        Assert.Equal(walls, other.Walls.Length);
        Assert.Equal(segments, other.CollisionSegments.Length);
        Assert.Equal(consoles, other.Consoles.Length);
        Assert.Equal(labels, other.RoomLabels.Length);
        Assert.Equal(regions, other.AppendedRegionCount);

        Assert.DoesNotContain(other.Consoles, c => c.Label == "⚙ TEST BENCH");
    }

    /// <summary>LAW 3 · …and the ship deck every OTHER reader asks is untouched too. The singleton is fine as
    /// a read-only fact; what it may never be is the object a page appends into.</summary>
    [Fact]
    public void AppendingToAPagesDeck_LeavesTheSharedShipPlanAlone()
    {
        var a = new Map();

        int walls = DeckPlan.Ship.Walls.Length;
        int consoles = DeckPlan.Ship.Consoles.Length;
        int regions = DeckPlan.Ship.AppendedRegionCount;

        DeckOf(a).AppendRegion(ALittleGround());

        Assert.Equal(walls, DeckPlan.Ship.Walls.Length);
        Assert.Equal(consoles, DeckPlan.Ship.Consoles.Length);
        Assert.Equal(regions, DeckPlan.Ship.AppendedRegionCount);
    }

    /// <summary>LAW 4 · The other door into the same hazard. <c>ShipDeckNow()</c> is what the page falls back
    /// to on undock and after every hatch move, and it used to hand back the singleton whenever no door
    /// happened to be shut — which put the shared plan under the captain's feet again on the commonest case
    /// of all.</summary>
    [Fact]
    public void HerDeckRightNow_IsNeverTheSharedShipPlan()
    {
        var page = new Map();
        var now = (DeckPlan)typeof(Map).GetMethod("ShipDeckNow", Hidden)!.Invoke(page, null)!;

        Assert.False(ReferenceEquals(now, DeckPlan.Ship),
            "ShipDeckNow() handed back DeckPlan.Ship. That answer becomes _deckPlan, and _deckPlan is what " +
            "AppendRegion writes into.");
        Assert.Equal(DeckPlan.Ship.Walls.Length, now.Walls.Length);   // same deck, own object
        Assert.Equal(DeckPlan.Ship.Consoles.Length, now.Consoles.Length);
    }
}
