using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #535 · <b>THE TWO SPENDS, DRIVEN ON THE SHIPPING PAGE.</b>
///
/// <para>Core proves the object; this proves the game. Every assertion below runs through the shipping
/// markup and the shipping handler: the fifth exit is FOUND on the rendered BUSTED card by the words a player
/// would read on it and PRESSED through the handler the markup wired, and the burn is found the same way on
/// the satchel row.</para>
///
/// <para><b>What makes these able to fail.</b> The trace guard is written as a before/after on four separate
/// registers — the law's heat, the wire's pushed events, the contacts book and the hunter roster — with the
/// world set up so each one WOULD have moved: there is a hunter on the roster to break off, heat on the
/// meter to be raised or cleared, and an empty wire that any <c>PushNewsEvent</c> would land in. A guard run
/// against a catch with no pursuer and no heat would be green against a method that did nothing at all.</para>
/// </summary>
[SlowGate] // #251 · 26 s over 5 test(s), measured 2026-09-05; see TheSlowGateRosterTests.
public sealed class TheKeyLeavesNoTraceTests
{
    private const string FreeFlying = "/map?start=wreck";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

    // ── The fifth exit ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE EXIT IS THERE WHEN THE KEY IS, AND IS NOT THERE WHEN IT IS NOT.
    ///
    /// <para>Absent rather than greyed, which is the judgement call this feature makes and the one worth
    /// pinning: a disabled control naming an object the captain has never met would teach them that a thing
    /// exists which would have saved them, and that is a quest marker in a button's clothes.</para>
    /// </summary>
    [Fact]
    public async Task TheFifthExitIsDrawnOnlyWhenAKeyIsCarried()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests.StageTheDemand(bench, "Demand");
        bench.Poke("_satchel", new List<Satchel.Item>());

        Assert.DoesNotContain(await OfferedOnTheCard(bench), n => Names(n, BlackOpsKey.PresentVerb));

        bench.Poke("_satchel", new List<Satchel.Item> { BlackOpsKey.FoundOn("lost-1") });

        Assert.Contains(await OfferedOnTheCard(bench), n => Names(n, BlackOpsKey.PresentVerb));
    }

    /// <summary>
    /// PRESS IT ON A REAL CATCH: NOTHING IS FILED ANYWHERE, AND THE PLATE IS ON THE SCREEN.
    ///
    /// <para>Four registers, read before and after. Heat is unchanged — not raised and not cleared, because a
    /// catch that never happened cannot settle a debt either. The wire's pushed list is still empty, which is
    /// the guard the issue asks for by name: the entry never FORMS, so this reads what the client pushes
    /// rather than what the ticker happens to render. The contacts book has no new row. And the hunter is off
    /// the roster, because the pursuer breaks off.</para>
    /// </summary>
    [Fact]
    public async Task PressingItFilesNothingAnywhereAndSaysOnlyThePlate()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests.StageTheDemand(bench, "Demand");

        // A world with something to lose in every register the scrub is supposed to leave alone.
        var heatBefore = new HeatState(2, 0);
        bench.Poke("_heat", heatBefore);
        bench.Poke("_satchel", new List<Satchel.Item> { BlackOpsKey.FoundOn("lost-1") });
        PutACollectorOnTheRoster(bench, "collector-1");

        Assert.Single(TheRoster(bench));
        Assert.Empty(TheWire(bench));

        DeskBench.Painted.Node exit = Assert.Single(
            await OfferedOnTheCard(bench), n => Names(n, BlackOpsKey.PresentVerb));
        await bench.PressAsync(exit.Handlers["onclick"]);
        DeskBench.Painted painted = await bench.RenderAsync();

        // …the encounter never happened.
        Assert.Equal(heatBefore, (HeatState)bench.Field("_heat")!);
        Assert.Empty(TheWire(bench));
        Assert.Empty(((ContactLedger)bench.Field("_contacts")!).Entries);
        Assert.Empty(TheRoster(bench));

        // …the key is spent.
        Assert.Equal(0, BlackOpsKey.CountIn((List<Satchel.Item>)bench.Field("_satchel")!));

        // …and the plate is the whole of what is said.
        DeskBench.Painted.Node? card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("busted-card") && !n.Hidden);
        Assert.True(card is not null, "the BUSTED card is gone from the screen after the key was presented — "
            + "the encounter unhappens, the panel does not.");
        Assert.Contains(BlackOpsKey.NoContactLoggedPlate, card!.Spoken, StringComparison.Ordinal);
    }

    /// <summary>The stage really is the fifth exit's own, and it is APPENDED — every arm that was on this
    /// enum before it keeps the ordinal several guards in this suite name by hand.</summary>
    [Fact]
    public void TheStageIsAppendedAndNotSlippedIntoTheMiddle()
    {
        Type stage = typeof(Map).GetNestedType("BustedEncounter", BindingFlags.Public | BindingFlags.NonPublic)!
            .GetNestedType("Stage")!;
        string[] arms = Enum.GetNames(stage);

        Assert.Equal("NoContactLogged", arms[^1]);
        Assert.Equal("Demand", arms[0]);
    }

    // ── The burn ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// BURN IT COLD ON THEIR GROUND: ONE BAND OFF THE BOOK, THE KEY GONE, AND THE LINE SAID ONCE.
    ///
    /// <para>Driven from the satchel row itself — found by the canon verb, pressed through the markup's own
    /// handler — so a control that stopped being drawn, or stopped being wired, fails here.</para>
    /// </summary>
    [Fact]
    public async Task BurningItOnTheirGroundTakesABandAndTheKey()
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);

        string bodyId = TheGroundUnderfoot(bench);
        string outfit = SiteOperator.Of(bodyId).Id;
        var contacts = (ContactLedger)bench.Field("_contacts")!;
        bench.CallOnTheDispatcher("BankTheCrossing",
            IllegalHeat.Charge(bodyId, IllegalHeat.Crossing.TheKickOut));
        bench.CallOnTheDispatcher("BankTheCrossing",
            IllegalHeat.Charge(bodyId, IllegalHeat.Crossing.TheKickOut));
        bench.CallOnTheDispatcher("BankTheCrossing",
            IllegalHeat.Charge(bodyId, IllegalHeat.Crossing.TheKickOut));

        int before = IllegalHeat.HeatAt(contacts, outfit);
        Assert.True(before > IllegalHeat.ABand,
            "the world this guard is asking the question of has less than a band on the book, so a scrub that "
            + "erased everything and a scrub that erased one band would look the same.");

        bench.Poke("_satchel", new List<Satchel.Item> { BlackOpsKey.FoundOn("lost-1") });
        OpenTheSatchel(bench);

        DeskBench.Painted.Node burn = Assert.Single(
            (await bench.RenderAsync()).Root.Descendants().ToList(), IsTheBurnControl);

        await bench.PressAsync(burn.Handlers["onclick"]);
        await bench.RenderAsync();

        Assert.Equal(before - IllegalHeat.ABand, IllegalHeat.HeatAt(contacts, outfit));
        Assert.Equal(0, BlackOpsKey.CountIn((List<Satchel.Item>)bench.Field("_satchel")!));
    }

    /// <summary>…and it is not offered where there is no file for it to close. The verb does not apply on a
    /// ground whose outfit has never heard of this captain — the same grammar the loader keeps on a keycard —
    /// so the control is not drawn rather than drawn and refusing.</summary>
    [Fact]
    public async Task TheBurnIsNotOfferedWhereNothingIsFiled()
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);
        bench.Poke("_satchel", new List<Satchel.Item> { BlackOpsKey.FoundOn("lost-1") });
        OpenTheSatchel(bench);

        Assert.Empty(((ContactLedger)bench.Field("_contacts")!).Entries);
        Assert.DoesNotContain((await bench.RenderAsync()).Root.Descendants(), IsTheBurnControl);
    }

    // ── Plumbing ────────────────────────────────────────────────────────────────────────────────────────

    private static bool Names(DeskBench.Painted.Node node, string verb) =>
        node.Name.Contains(verb, StringComparison.OrdinalIgnoreCase);

    /// <summary>The burn's own control, and it has to say BUTTON: a node's <c>Name</c> is the text of its
    /// whole subtree, so every pressable ancestor of the row — the satchel's backdrop included — "names" the
    /// verb as well. A guard that took the first match would have been pressing the backdrop.</summary>
    private static bool IsTheBurnControl(DeskBench.Painted.Node node) =>
        string.Equals(node.Element, "button", StringComparison.Ordinal)
        && !node.Hidden && node.Handlers.ContainsKey("onclick")
        && Names(node, BlackOpsKey.BurnVerb);

    private static async Task<IReadOnlyList<DeskBench.Painted.Node>> OfferedOnTheCard(DeskBench bench)
    {
        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node? card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("busted-card") && !n.Hidden);

        Assert.True(card is not null, "the BUSTED card is not on the screen at all, so this guard is asking "
            + "its question of a world that cannot answer it.");

        return [.. card!.Descendants().Where(n => !n.Hidden && n.Handlers.ContainsKey("onclick"))];
    }

    private static void OpenTheSatchel(DeskBench bench)
    {
        Type page = typeof(Map).GetNestedType("SatchelPage", BindingFlags.Public | BindingFlags.NonPublic)!;
        bench.Poke("_satchelPage", Enum.Parse(page, "Carried"));
        bench.Poke("_showSatchel", true);
    }

    /// <summary>The body the captain is standing on, read off the live excursion — so the guard asks about
    /// whoever actually runs this ground rather than an outfit typed into a test.</summary>
    private static string TheGroundUnderfoot(DeskBench bench)
    {
        object excursion = bench.Field("_surface")
            ?? throw new InvalidOperationException(
                "no excursion — this guard needs the captain standing on somebody's ground, which is the only "
                + "place a key has a file to close.");

        object stop = excursion.GetType().GetProperty("Stop")!.GetValue(excursion)!;
        object body = stop.GetType().GetProperty("Body")!.GetValue(stop)!;
        return (string)body.GetType().GetProperty("Id")!.GetValue(body)!;
    }

    private static List<HunterState> TheRoster(DeskBench bench) =>
        (List<HunterState>)bench.Field("_hunters")!;

    private static List<NewsWire.NewsEvent> TheWire(DeskBench bench) =>
        (List<NewsWire.NewsEvent>)bench.Field("_newsEvents")!;

    /// <summary>Put a real collector on the roster under the callsign the staged demand names, so "the
    /// pursuer breaks off" is a question about a pursuer that was there.</summary>
    private static void PutACollectorOnTheRoster(DeskBench bench, string hunterId) =>
        TheRoster(bench).Add(new HunterState(
            hunterId, "VULTURE ACTUAL", "luna", 0, 0, (ShipState)bench.Field("_ship")!,
            CaughtPlayer: true, BrokenOff: false));
}
