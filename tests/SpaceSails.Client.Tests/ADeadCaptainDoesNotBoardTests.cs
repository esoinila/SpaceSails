using System;
using System.Threading.Tasks;
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
[SlowGate]
public sealed class ADeadCaptainDoesNotBoardTests
{
    private const string OnMirandasGround = "/map?dock=the-tilt&site=0&land=1";

    /// <summary>Bring the mothership alongside the ground, course-matched, so the boat can fly home.</summary>
    private static void BringHerAlongside(DeskBench bench)
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

        bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", bench.Peek("_surface"), true, null);
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

            bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", ex, false, DeathCause.Reevers);
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
[SlowGate]
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
[SlowGate]
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
