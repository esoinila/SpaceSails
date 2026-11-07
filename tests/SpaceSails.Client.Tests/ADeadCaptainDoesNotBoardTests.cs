using System;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core.Tests;
using Xunit;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1401 R1-F24 / R1-F25 · <b>THE LIFTOFF AND THE KILL ARE ONE DECISION</b>, and the death line fits the sling.
///
/// <para>Proven red on revert: take the <c>_busted is not null</c> guard out of <c>LiftOffFromSurface</c> and the
/// first law goes red ("Back aboard" under the death card); take the <c>SeedFor</c> call out of the surface death
/// and the sling law goes red for the seeds that drew the chest line.</para>
/// </summary>
public sealed class ADeadCaptainDoesNotBoardTests
{
    private const string OnMirandasGround = "/map?dock=the-tilt&site=0&land=1";

    /// <summary>Bring the mothership alongside the ground, course-matched, so the boat can fly home.</summary>
    internal static void BringHerAlongside(DeskBench bench)
    {
        var ephemeris = (ICelestialEphemeris)bench.Peek("_ephemeris")!;
        object excursion = bench.Peek("_surface")!;
        object stop = excursion.GetType().GetProperty("Stop")!.GetValue(excursion)!;
        var body = (CelestialBody)stop.GetType().GetProperty("Body")!.GetValue(stop)!;
        double simTime = (double)bench.Peek("SimTime")!;
        Vector2d ground = ephemeris.Position(body.Id, simTime);
        Vector2d groundVelocity = TransferMath.BodyVelocity(ephemeris, body.Id, simTime);
        Vector2d outward = ground == Vector2d.Zero ? new Vector2d(1, 0) : ground.Normalized();
        bench.Poke("_ship", new ShipState(ground + (outward * (ShuttleRange.RangeMeters * 0.2)), groundVelocity, simTime));
        bench.Poke("_dockedHavenId", null);
        ((System.Collections.IDictionary)bench.Peek("_awayWindowScans")!).Clear();
    }

    [Fact]
    public async Task ALivingCaptainBoardsTheSameShuttleOnTheSamePress()
    {
        // The control for the law below: on this ground, with this boat, the press DOES take a living captain home.
        using DeskBench bench = await DeskBench.BootAsync(OnMirandasGround);
        await bench.RenderAsync();
        BringHerAlongside(bench);
        bench.CallOnTheDispatcher("LiftOffFromSurface");
        Assert.False(bench.OnSurface, $"the control press did not board. Pulse: {bench.Pulse}");
    }

    [Fact]
    public async Task ACaptainTheOldOnesHaveTakenCannotAlsoBoardTheShuttle()
    {
        using DeskBench bench = await DeskBench.BootAsync(OnMirandasGround);
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);

        bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", bench.Peek("_surface"), true, null, false);
        Assert.NotNull(bench.Peek("_busted"));

        BringHerAlongside(bench);
        bench.CallOnTheDispatcher("LiftOffFromSurface");

        Assert.True(bench.OnSurface, "a press on the shuttle took a captain who was already dead back aboard.");
        Assert.DoesNotContain("Back aboard", bench.Pulse, StringComparison.Ordinal);
        Assert.DoesNotContain("outran", bench.Pulse, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnEmptySlingIsNeverToldTheChestIsStillOutThere()
    {
        using DeskBench bench = await DeskBench.BootAsync(OnMirandasGround);
        await bench.RenderAsync();
        object ex = bench.Peek("_surface")!;
        Assert.False((bool)ex.GetType().GetProperty("Carrying")!.GetValue(ex)!, "the bench landed with a chest.");

        // The seed is drawn off the sim clock: sweep clocks, and insist the sweep includes draws the unfitted
        // pool would have answered with the chest line (else the law proves nothing).
        double t0 = (double)bench.Peek("SimTime")!;
        int wouldHaveBeenTheChest = 0;
        for (int i = 0; i < 40; i++)
        {
            double t = t0 + (i * 37.0);
            bench.Poke("SimTime", t);
            bench.Poke("_busted", null);
            if (DiceRule.Seed("overdraw", (long)t) % 3 == 0)
            {
                wouldHaveBeenTheChest++;
            }

            bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", ex, false, DeathCause.Reevers, false);
            object busted = bench.Peek("_busted")!;
            var seed = (ulong)busted.GetType().GetProperty("Seed")!.GetValue(busted)!;
            var place = (DeathPlace)busted.GetType().GetProperty("Place")!.GetValue(busted)!;
            Assert.DoesNotContain("chest", DeathNarration.Line(DeathCause.Reevers, place, seed, "Miranda"),
                StringComparison.OrdinalIgnoreCase);
        }
        Assert.True(wouldHaveBeenTheChest > 0, "the swept clocks never drew the chest line, so this proved nothing.");
    }
}

/// <summary>#1401 R1-F42 · Keys belong to Brem Kolt's decision card while it is up.</summary>
public sealed class TheKoltCardHoldsTheKeysTests
{
    [Fact]
    public async Task WalkingKeysAndEDoNothingUnderTheCard_AndWorkWithoutIt()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");
        await bench.RenderAsync();
        var held = (System.Collections.Generic.HashSet<string>)bench.Peek("_deckKeys")!;
        held.Clear();

        // Control: with no card up, a walking key is taken.
        Assert.True((bool)bench.CallOnTheDispatcher("HandleDeckKey", "d", false)!);
        Assert.Contains("d", held);
        held.Clear();

        bench.Poke("_hardcaseCard", HardcaseRep.OffersFor(InsuranceTier.Basic));
        string before = bench.Pulse;
        foreach (string key in new[] { "w", "a", "s", "d", "ArrowUp", "E" })
        {
            Assert.True((bool)bench.CallOnTheDispatcher("HandleDeckKey", key, false)!);
        }

        Assert.Empty(held);
        Assert.Equal(before, bench.Pulse);
    }
}

/// <summary>#1401 R1-F41 · An Old One the tracker cannot show is not danger.</summary>
public sealed class AnOldOneOffTheDialIsNotDangerTests
{
    [Fact]
    public async Task OnlyAnOldOneWithinTheTrackersReachHoldsTheStoryCards()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");
        await bench.RenderAsync();
        var avatarX = (double)bench.Peek("_avatarX")!;
        var avatarY = (double)bench.Peek("_avatarY")!;
        double reach = (double)bench.Call("FanReach")!;
        Assert.True(reach > 0);

        Type reeverType = typeof(SpaceSails.Client.Pages.Map).GetNestedType("Reever",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!;
        object Reever(double dx)
        {
            object r = Activator.CreateInstance(reeverType)!;
            reeverType.GetField("X")!.SetValue(r, avatarX + dx);
            reeverType.GetField("Y")!.SetValue(r, avatarY);
            return r;
        }

        object list = bench.Peek("_reevers")!;
        System.Reflection.MethodInfo add = list.GetType().GetMethod("Add")!;
        System.Reflection.MethodInfo clear = list.GetType().GetMethod("Clear")!;
        clear.Invoke(list, null);
        Assert.False((bool)bench.Call("CaptainIsInDanger")!);

        add.Invoke(list, [Reever(reach * 3.0)]);
        Assert.False((bool)bench.Call("CaptainIsInDanger")!, "an Old One far off the dial held the ground's calm.");

        clear.Invoke(list, null);
        add.Invoke(list, [Reever(reach * 0.5)]);
        Assert.True((bool)bench.Call("CaptainIsInDanger")!, "an Old One on the dial no longer counts as danger.");
    }
}

/// <summary>#1401 R1-F21 · A load from the ground (or from under a death card) arrives in the loaded life, not under
/// the ground it left, and the ledger forgets the lines the load undid.</summary>
[Collection(StopRegisterCollection.Name)]
public sealed class ALoadLeavesTheGroundBehindTests
{
    [Fact]
    public async Task LoadingFromTheGroundFoldsTheExcursionAwayAndForgetsTheLinesAfterTheSave()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);

        // The walk that is about to be undone: a death staged, its lines written, a nerve ledger on the books.
        bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", bench.Peek("_surface"), true, null, false);
        Assert.NotNull(bench.Peek("_busted"));
        var events = (System.Collections.Generic.List<(double SimTime, string Text, bool Story)>)bench.Peek("_autopilotEvents")!;
        events.Insert(0, (5.0e8, "🧠 Nerves shot past empty on Titan — the line the load undoes.", false));
        events.Insert(0, (-1.0, "a line from before the loaded moment.", false));

        bench.CallOnTheDispatcher("ApplyVault", new Vault { SavedSimTime = 1000 });

        Assert.False(bench.OnSurface, "the load left the captain standing in the excursion it was loading away from.");
        Assert.Null(bench.Peek("_busted"));
        Assert.DoesNotContain(events, e => e.Text.Contains("the line the load undoes", StringComparison.Ordinal));
        Assert.Contains(events, e => e.Text.StartsWith("a line from before", StringComparison.Ordinal));
    }
}

/// <summary>#1401 R1-F47 · The Trade tile counts only partners the shuttle bay could actually offer: not the
/// berth she is clamped to.</summary>
public sealed class TheTradeTileAgreesWithTheShuttleBayTests
{
    [Fact]
    public async Task TheBerthSheIsClampedToIsNotAPartnerInShuttleReach()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=ringside-exchange&simhours=240");
        await bench.RenderAsync();

        var stops = (System.Collections.IList)bench.Call("ShuttleDestinationsInRange")!;
        Assert.Empty(stops);   // the bay has nothing to offer from this berth, as in the report
        string chip = bench.Call("TradeChip")!.ToString()!;
        Assert.DoesNotContain("shuttle reach", chip, StringComparison.Ordinal);
    }

    /// <summary>#1469 · The chip's contact set is the desk's, and the desk's reach is the shuttle corridor: a
    /// depot <c>ContactsAt</c> lists only because it belongs to the body (the player is far from it, orbiting
    /// nothing) is not "in reach". RED: count every Trade-capable contact in <c>TradeChip</c>.</summary>
    [Fact]
    public async Task AFarDepotTheBodyListsIsNotAPartnerInReach()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?start=earth");
        await bench.RenderAsync();
        ICelestialEphemeris eph = (ICelestialEphemeris)bench.Peek("_ephemeris")!;
        Vector2d earth = eph.Position("earth", (double)bench.Peek("SimTime")!);
        bench.Poke("_orbitedBodyId", null);
        bench.Poke("_nearestBody", eph.Bodies.First(b => b.Id == "earth"));
        bench.Poke("_dockedHavenId", null);
        bench.Poke("_ship", new ShipState(earth + new Vector2d(CommerceRule.ShuttleRangeMeters * 4, 0), Vector2d.Zero, 0.0));
        var contacts = (System.Collections.IList)bench.Call("LocalContacts")!;
        Assert.NotEmpty(contacts);   // the premise: the body lists its depots, near or far
        Assert.DoesNotContain("trade partner", bench.Call("TradeChip")!.ToString()!, StringComparison.Ordinal);
    }

    /// <summary>#1469 · The tile and the desk agree on a SameOrbit partner past the shuttle corridor (a Jupiter-sized
    /// Hill sphere): the desk deals with it, so the chip counts it. RED: drop the explicit SameOrbit clause.</summary>
    [Fact]
    public async Task ASameOrbitPartnerPastTheCorridorIsCountedBecauseTheDeskDealsWithIt()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?start=earth");
        await bench.RenderAsync();
        ICelestialEphemeris eph = (ICelestialEphemeris)bench.Peek("_ephemeris")!;
        Vector2d earth = eph.Position("earth", (double)bench.Peek("SimTime")!);
        bench.Poke("_orbitedBodyId", "earth");
        bench.Poke("_dockedHavenId", null);
        bench.Poke("_ship", new ShipState(earth + new Vector2d(CommerceRule.ShuttleRangeMeters * 4, 0), Vector2d.Zero, 0.0));

        int deskDeals = 0;
        foreach (CommerceRule.LocalContact c in (System.Collections.IList)bench.Call("LocalContacts")!)
        {
            var mode = (CommerceRule.TradeMode)bench.Call("TradeModeFor", c)!;
            Assert.True(mode == CommerceRule.TradeMode.SameOrbit || (c.Actions & CommerceRule.ActionKind.Trade) == 0
                || mode == CommerceRule.TradeMode.None);
            if (mode == CommerceRule.TradeMode.SameOrbit && (c.Actions & CommerceRule.ActionKind.Trade) != 0)
            {
                deskDeals++;
            }
        }
        Assert.True(deskDeals > 0, "the premise: earth has a depot the desk deals with from its orbit.");
        string chip = bench.Call("TradeChip")!.ToString()!;
        Assert.Contains($"{deskDeals} trade partner", chip, StringComparison.Ordinal);
    }
}
