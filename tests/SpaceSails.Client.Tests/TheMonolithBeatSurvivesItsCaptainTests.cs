using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1411 · main arcs beat 14 (R1-F27, F28, F29, F30). The monolith's ground and the death card.
///
/// <para>Proven red on revert: (F27) drop the <c>packHeld</c> pin from <c>StepReevers</c>; (F28) drop the
/// <c>TheDeathCardFreezesTheGround</c> gate from <c>StepTheWalkedGround</c>; (F29/F30) drop the three clears from
/// <c>ForgetTheExcursionsGlass</c>.</para>
/// </summary>
public sealed class TheMonolithBeatSurvivesItsCaptainTests
{
    private const string Phobos = "/map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1&kolt=0&reevers=3";

    private static async Task<DeskBench> OnTheMonolithsGroundAsync(bool atTheLanding = false)
    {
        DeskBench bench = await DeskBench.BootAsync(Phobos);
        await bench.RenderAsync();
        Assert.True(bench.OnSurface);
        if (atTheLanding)
        {
            return bench;
        }

        double ax = SpaceSails.Client.Rendering.MoonSurface.AnchorX, ay = SpaceSails.Client.Rendering.MoonSurface.AnchorY;
        bench.Poke("_avatarX", ax);
        bench.Poke("_avatarY", ay + Monolith.HalfHeight + 30.0);   // inside its sight, on the approach side
        return bench;
    }

    private static List<(double X, double Y)> Positions(DeskBench bench) =>
        ((IEnumerable)bench.Peek("_reevers")!).Cast<object>()
            .Select(r => (Convert.ToDouble(Member(r, "X")), Convert.ToDouble(Member(r, "Y"))))
            .ToList();

    private static object Member(object o, string name) =>
        (o.GetType().GetField(name)?.GetValue(o) ?? o.GetType().GetProperty(name)?.GetValue(o))!;

    private static void Steps(DeskBench bench, string method, int frames, double dt)
    {
        for (int i = 0; i < frames; i++)
        {
            bench.CallOnTheDispatcher(method, dt);
        }
    }

    /// <summary>R1-F27 · Put the clock on an epoch whose roll IS the pack variant (the roll is seeded per epoch,
    /// so the sweep is over epochs, and it must find one or this proves nothing).</summary>
    private static void SetTheClockToAPackEpoch(DeskBench bench)
    {
        const string bodyId = Monolith.BodyId;
        const string salt = "";
        for (long epoch = 1; epoch < 400; epoch++)
        {
            if (MonolithWatch.Which(bodyId, salt, epoch, packOnTheField: true) == MonolithWatch.What.ThePackGoesStill)
            {
                bench.Poke("SimTime", (epoch * Monolith.EpochSeconds) + 1.0);
                return;
            }
        }

        Assert.Fail("no epoch drew the pack variant; the sweep proves nothing.");
    }

    [Fact]
    public async Task WhenTheLineSaysEveryOldOneHasStopped_NoOldOneMoves()
    {
        using DeskBench bench = await OnTheMonolithsGroundAsync();
        Assert.NotEmpty(Positions(bench));

        // Control: with nothing held, the pack on this ground does move (else the stillness below proves nothing).
        List<(double X, double Y)> before = Positions(bench);
        Steps(bench, "StepReevers", 100, 0.1);
        Assert.NotEqual(before, Positions(bench));

        SetTheClockToAPackEpoch(bench);
        bench.CallOnTheDispatcher("StepMonolithWatch", 1000.0);
        object ex = bench.Peek("_surface")!;
        Assert.True((double)ex.GetType().GetProperty("PackStillSeconds")!.GetValue(ex)! > 0,
            "the pack line was told and nothing was held.");

        List<(double X, double Y)> held = Positions(bench);
        Steps(bench, "StepReevers", 50, 0.1);   // five of the ~eight seconds
        Assert.Equal(held.Count, Positions(bench).Count);
        for (int i = 0; i < held.Count; i++)
        {
            Assert.True(Math.Abs(held[i].X - Positions(bench)[i].X) < 0.5 && Math.Abs(held[i].Y - Positions(bench)[i].Y) < 0.5,
                $"Old One {i} walked while the line said every one of them had stopped.");
        }
    }

    [Fact]
    public async Task TheDeathCardFreezesTheGround_HeldKeysDoNotWalkTheDeadCaptain()
    {
        using DeskBench bench = await OnTheMonolithsGroundAsync(atTheLanding: true);
        bench.Poke("_deckMode", true);
        var keys = (HashSet<string>)bench.Peek("_deckKeys")!;

        // Control: a held key walks the living captain.
        keys.Clear();
        keys.Add("s");
        double y0 = (double)bench.Peek("_avatarY")!;
        for (int i = 0; i < 10; i++)
        {
            bench.CallOnTheDispatcher("StepTheWalkedGround", 0.1);
        }
        Assert.NotEqual(y0, (double)bench.Peek("_avatarY")!);

        bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", bench.Peek("_surface"), true, null, false);
        Assert.NotNull(bench.Peek("_busted"));
        keys.Clear();
        keys.Add("s");
        double yDead = (double)bench.Peek("_avatarY")!;
        for (int i = 0; i < 30; i++)
        {
            bench.CallOnTheDispatcher("StepTheWalkedGround", 0.1);
        }

        Assert.Equal(yDead, (double)bench.Peek("_avatarY")!);
    }

    [Fact]
    public async Task TheSelfieOfferAndKoltsPitchDieWithTheCaptain()
    {
        using DeskBench bench = await OnTheMonolithsGroundAsync();
        Type offer = typeof(SpaceSails.Client.Pages.Map).GetNestedType("SelfieOffer", BindingFlags.Public | BindingFlags.NonPublic)!;
        bench.Poke("_selfieOffer", Activator.CreateInstance(offer, "first-monolith", "FIRST CONTACT", ""));
        bench.Poke("_hardcaseCard", HardcaseRep.OffersFor(InsuranceTier.Basic));
        bench.Poke("_scrimQueued", ((Action)(() => { }), (Func<bool>)(() => true)));
        Assert.NotNull(bench.Peek("_scrimQueued"));   // the queue is really full, so the clear below is a clear

        bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", bench.Peek("_surface"), true, null, false);
        bench.CallOnTheDispatcher("BustedResurrect");

        Assert.Null(bench.Peek("_selfieOffer"));
        Assert.Null(bench.Peek("_hardcaseCard"));
        Assert.Null(bench.Peek("_scrimQueued"));
    }

    [Fact]
    public async Task KoltsCardBehindTheDeathCardCannotSellAPolicy()
    {
        using DeskBench bench = await OnTheMonolithsGroundAsync();
        bench.Poke("_credits", 100000);
        var before = bench.Peek("_insurance");
        bench.Poke("_hardcaseCard", HardcaseRep.OffersFor(InsuranceTier.Basic));
        bench.CallOnTheDispatcher("TriggerSurfaceOverdrawDeath", bench.Peek("_surface"), true, null, false);
        Assert.NotNull(bench.Peek("_busted"));

        bench.CallOnTheDispatcher("AnswerTheHardcase", NebulaRep.RepMove.BuyPremium);
        bench.CallOnTheDispatcher("AnswerTheHardcase", NebulaRep.RepMove.BuyBasic);

        Assert.Equal(before, bench.Peek("_insurance"));
    }

    [Fact]
    public async Task LeftoverHoldSecondsDoNotPinAFreshPack()
    {
        using DeskBench bench = await OnTheMonolithsGroundAsync();
        object ex = bench.Peek("_surface")!;
        ex.GetType().GetProperty("PackStillSeconds")!.SetValue(ex, 5.0);
        ((IList)bench.Peek("_reevers")!).Clear();
        Steps(bench, "StepReevers", 60, 0.1);
        Assert.Equal(0.0, (double)ex.GetType().GetProperty("PackStillSeconds")!.GetValue(ex)!);
    }
}
