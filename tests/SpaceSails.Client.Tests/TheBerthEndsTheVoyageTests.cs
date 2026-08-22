using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #962 · A BERTH IS AN ARRIVAL, AND AN ARRIVAL ENDS THE VOYAGE.
///
/// <para>Owner, a leg later and long gone from the place: <i>"Now why does this rustys roadshed show as any
/// kind of navigation target here still? I left that place already. Why can it not be dismissed / deleted
/// from the screen now. It is just on the way here?"</i></para>
///
/// <para><c>ArrivedAt</c> is the page's "the voyage is over, the orders complete" hook, and it was wired to
/// ORBITAL INSERTION only — the armed auto-insert and the manual EnterOrbit. A dock haven is mass-less: you
/// clamp onto it, you never insert. So no arrival at any station in the game had ever completed its own
/// voyage. The DEST lock survived the clamp, survived the undock, and rode out of the berth with the ship,
/// for the rest of the session — and while docked there is no affordance on screen that can clear it, which
/// is the "why can it not be dismissed" half of the report.</para>
///
/// <para><b>Red proof (run before shipping).</b> Delete <c>ArrivedAt(dock.Id);</c> from
/// <c>Map.Docking.ClampOntoHaven</c> and the first test goes red with the berth still named as the
/// destination; delete <c>ArrivedAt(_dockedHavenId);</c> from <c>Map.Docking.Undock</c> and the second does.
/// Both were watched red before this file shipped.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBerthEndsTheVoyageTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private const string Berth = "rustys-roadstead";

    /// <summary>CLAMPING ON IS ARRIVING. The destination the captain plotted to this berth is retired by
    /// reaching it — the same statement an orbital insertion has always made.</summary>
    [Fact]
    public void CLAMPING_ON_RetiresTheDestinationThatNamedThisBerth()
    {
        Pages.Map map = AShipBoundForTheRoadstead();

        Clamp(map);

        Assert.Null(Get(map, "_destinationBodyId"));
    }

    /// <summary>…AND CASTING OFF CANNOT LEAVE THE BERTH YOU ARE LEAVING AS YOUR TARGET. The same statement
    /// said at the other end — and the line that heals a session which clamped on before this fix existed,
    /// which is the state the owner's save was actually in.</summary>
    [Fact]
    public void CASTING_OFF_RetiresTheDestinationThatNamedTheBerthBeingLeft()
    {
        Pages.Map map = AShipBoundForTheRoadstead();
        Set(map, "_dockedHavenId", Berth);

        typeof(Pages.Map).GetMethod("Undock", Hidden)!.Invoke(map, null);

        Assert.Null(Get(map, "_destinationBodyId"));
    }

    /// <summary>AND IT IS THE BERTH THAT IS RETIRED, NOT WHATEVER WAS PLOTTED. A clamp that cleared any
    /// destination at all would be a worse bug than the one being fixed: put in at a haven on the way and
    /// your actual voyage would quietly forget where it was going. <c>ArrivedAt</c> already carries that
    /// guard; this pins that the call site did not reach past it.</summary>
    [Fact]
    public void CLAMPING_ON_SomewhereElse_LeavesTheRealVoyageAlone()
    {
        Pages.Map map = AShipBoundForTheRoadstead();
        Set(map, "_destinationBodyId", "mercury");

        Clamp(map);

        Assert.Equal("mercury", Get(map, "_destinationBodyId"));
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    private static void Clamp(Pages.Map map)
    {
        var ephemeris = (ICelestialEphemeris)Get(map, "_ephemeris")!;
        CelestialBody dock = ephemeris.Bodies[^1];
        typeof(Pages.Map).GetMethod("ClampOntoHaven", Hidden)!
            .Invoke(map, [dock, ephemeris.Position(dock.Id, 0), null]);
    }

    private static Pages.Map AShipBoundForTheRoadstead()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        var ephemeris = new CircularOrbitEphemeris(
        [
            new CelestialBody("sol", "Sol", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("mercury", "Mercury", "sol", 2.2e13, 2.44e6, 5.79e10, 7.6e6, 0),
            new CelestialBody(Berth, "Rusty's Roadstead", "mercury", 0, 0, 4e7, 4e4, 0,
                BodyKind.Station, IsHaven: true),
        ]);

        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_ship", new ShipState(ephemeris.Position(Berth, 0), Vector2d.Zero, 0));
        Set(map, "_destinationBodyId", Berth);
        return map;
    }

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);
}
