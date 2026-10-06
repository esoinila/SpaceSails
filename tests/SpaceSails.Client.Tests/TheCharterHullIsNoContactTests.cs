using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1074 beat 5 · THE CHARTER HULL IS A FIXTURE AND NOTHING ELSE — no verb that acts on a contact may act on
/// her. She sits on the traffic board as a depot-style ship with a cargo class and 0 units, which is exactly
/// the shape the trade list, the hunt booth, the boarding window, the war room, the ordnance loop and the
/// finder all walk; unguarded, a captain could sell to her, be sent to hunt her, board her for heat and a
/// hunter, or shoot the one hull the beat leaves alone.
///
/// <para><b>It boots the SHIPPING component</b> (the bench <see cref="TheBootBuildsTheSameWorldTests"/> uses),
/// parks the hull on the real board, makes her and a control ship BOTH eligible for every verb (active, not
/// arrived, observed, every other ship stood down), and asks each verb. The control ship is what makes the
/// world able to tell pass from fail: a verb that returned nothing at all would pass an "absent" assertion for
/// the wrong reason, so each verb must also still offer the control.</para>
///
/// <para>The ordnance loop and the finder are not reachable as a pure answer from outside (a step of the
/// simulation; a case that is null in most worlds), so those two are pinned by <b>source law</b> at the end.
/// FLAGGED in the PR.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[Collection(StopRegisterCollection.Name)]
public sealed class TheCharterHullIsNoContactTests
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const string Rock = "luna";

    private sealed class Rig
    {
        public required Pages.Map Map;
        public required object Hull;
        public required object Control;
        public required string HullId;
        public required string ControlId;
    }

    private static object? Get(object on, string name) =>
        on.GetType().GetField(name, Any)?.GetValue(on) ?? on.GetType().GetProperty(name, Any)?.GetValue(on);

    private static void Set(object on, string name, object? value)
    {
        FieldInfo? f = on.GetType().GetField(name, Any);
        if (f is not null) { f.SetValue(on, value); return; }
        on.GetType().GetProperty(name, Any)!.SetValue(on, value);
    }

    private static object Call(Pages.Map map, string method, params object?[] args) =>
        typeof(Pages.Map).GetMethod(method, Any)!.Invoke(map, args)!;

    private static async Task<Rig> Rigged()
    {
        var map = new Pages.Map();
        TheBootBuildsTheSameWorldTests.NeverRender(map);
        TheBootBuildsTheSameWorldTests.Hand(map, "Http", TheBootBuildsTheSameWorldTests.ScenariosFromDisk());
        TheBootBuildsTheSameWorldTests.Hand(map, "Navigation", new TheBootBuildsTheSameWorldTests.Bench("/map"));
        try
        {
            await (Task)typeof(Pages.Map).GetMethod("BootTheWorldAsync", Any)!.Invoke(map, [CancellationToken.None])!;
        }
        catch (Exception)
        {
            // the browser gate, reached after the world was built
        }
        Assert.NotNull(Get(map, "_ephemeris"));

        // fire the beat on the real board
        Set(map, "_shuttle", new ReturningShuttle.Row(Rock, 0));
        Call(map, "TheCharterHullIsOnTheBoard");

        var states = ((IEnumerable)Get(map, "_npcStates")!).Cast<object>().ToList();
        string idOf(object n) => ((NpcShip)Get(n, "Ship")!).Id;
        object hull = states.Single(n => ReturningShuttle.IsTheHull(idOf(n)));
        object? control = states.FirstOrDefault(n =>
            !ReturningShuttle.IsTheHull(idOf(n)) && !((NpcShip)Get(n, "Ship")!).IsPod
            && ((NpcShip)Get(n, "Ship")!).DepotBodyId is null);
        Assert.NotNull(control);

        // every other ship stands down; the hull and the control are both live, observed and un-arrived
        foreach (object n in states)
        {
            bool live = ReferenceEquals(n, hull) || ReferenceEquals(n, control);
            Set(n, "Active", live);
            Set(n, "Arrived", false);
            Set(n, "Boarded", false);
            Set(n, "CurrentlyObserved", live);
        }

        return new Rig { Map = map, Hull = hull, Control = control!, HullId = idOf(hull), ControlId = idOf(control!) };
    }

    /// <summary>TRADE: the commerce list never carries her, and still carries the control.</summary>
    [Fact]
    public async Task TheTradeListNeverContainsTheHull()
    {
        Rig r = await Rigged();
        var ids = ((IEnumerable)Call(r.Map, "LocalShips")).Cast<CommerceRule.LocalShip>().Select(s => s.Id).ToArray();
        Assert.Contains(r.ControlId, ids);
        Assert.DoesNotContain(r.HullId, ids);
    }

    /// <summary>WAR ROOM: the contacts never carry her, and still carry the control.</summary>
    [Fact]
    public async Task TheWarRoomNeverContainsTheHull()
    {
        Rig r = await Rigged();
        var ids = ((IEnumerable)Call(r.Map, "WarRoomContacts")).Cast<object>()
            .Select(c => ((NpcShip)Get(c, "Ship")!).Id).ToArray();
        Assert.Contains(r.ControlId, ids);
        Assert.DoesNotContain(r.HullId, ids);
    }

    /// <summary>HUNT: with only the hull and the control eligible, every offer across sixty sim-times targets
    /// the control — never her.</summary>
    [Fact]
    public async Task TheHuntBoothNeverOffersTheHull()
    {
        Rig r = await Rigged();
        Assert.True(string.CompareOrdinal(r.HullId, r.ControlId) < 0,
            "the hull's id must sort before the control's, or this guard could not tell pass from fail");
        // The booth's pick is a weighted draw seeded on sim-time, so ONE offer proves little: with the guard
        // gone it picks the hull about half the time. Sixty draws across sixty sim-times leave no room for
        // luck, and every one must be the control.
        for (int i = 0; i < 60; i++)
        {
            Set(r.Map, "SimTime", i * 977.0);
            object? offer = Call(r.Map, "MakeHuntOffer", "a stranger");
            Assert.NotNull(offer);
            Assert.Equal(r.ControlId, (string)Get(offer!, "TargetShipId")!);
        }
    }

    /// <summary>BOARDING: a selected, observed hull is not a capture target; the same selection of the control
    /// is.</summary>
    [Fact]
    public async Task TheHullIsNeverABoardingCandidate()
    {
        Rig r = await Rigged();
        Set(r.Map, "_selectedTargetId", r.ControlId);
        Assert.NotNull(Call(r.Map, "SelectedCaptureTarget"));

        Set(r.Map, "_selectedTargetId", r.HullId);
        Assert.Null(typeof(Pages.Map).GetMethod("SelectedCaptureTarget", Any)!.Invoke(r.Map, null));
    }

    /// <summary>INTEREST: she cannot be made the target of interest; the control can.</summary>
    [Fact]
    public async Task TheHullIsNeverATargetOfInterest()
    {
        Rig r = await Rigged();
        Set(r.Map, "_interestTargetId", null);
        try { Call(r.Map, "SetInterestTarget", r.ControlId); } catch (TargetInvocationException) { }
        Assert.Equal(r.ControlId, Get(r.Map, "_interestTargetId"));

        Set(r.Map, "_interestTargetId", null);
        try { Call(r.Map, "SetInterestTarget", r.HullId); } catch (TargetInvocationException) { }
        Assert.Null(Get(r.Map, "_interestTargetId"));
    }

    // The ordnance loop and the finder pool source-law pins live in EveryStoryHullIsNoContactTests (#1357).
}
