using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 · BREADCRUMBS — asked, and found already answered.
///
/// <para>Owner, opening the issue: a captain who walks out into an unbounded world needs <i>a way to find
/// the way back</i>. It is the obvious thing to build a dropped trail for, and the trail is the wrong
/// answer: the ground is not a maze and the captain is not lost in the topological sense — there is nothing
/// between them and the tube but distance. What they need is <b>which way, and how far</b>, from anywhere,
/// including from further out than any instrument can see.</para>
///
/// <para><b>The game has said that since #573 and it says it in three registers at once.</b> The motion
/// tracker paints the tube mouth as the HOME ring on every frame of every excursion on the regolith
/// (<c>Map.BuildBeacons</c>: <c>Add(MoonSurface.SpawnX, MoonSurface.SpawnY, home: true)</c>, unconditional);
/// a place beyond the fan's reach CLAMPS TO THE RIM rather than dropping off it
/// (<c>DeckView.Hud</c>: <i>"you always know which way it is, never how far once it is far"</i>); and the
/// suit's readout prices the same distance in the only currency that matters — how much FURTHER you may go
/// before the walk home stops being affordable (<see cref="SuitAir.Readout"/> against
/// <c>Map.DistanceToTheTube</c>), with a one-time line on the step where that changes and, since slice 2, a
/// refusal at the backstop.</para>
///
/// <para>So this file is not a new mechanic. It is the guard that was missing under the old one: nothing
/// stated anywhere that the way home is on the instrument <b>at every distance</b>, which is precisely the
/// clause an unbounded world puts under strain and precisely the clause a range gate would quietly remove.
/// A captain nine thousand du out with no ring on the fan is lost in a way no amount of ground being
/// generated correctly can fix.</para>
/// </summary>
public class TheWayBackIsAlwaysOnTheFanTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly string[] Bodies = ["luna", "phobos", "titan", "miranda"];

    /// <summary>THE WAY HOME IS ON THE FAN FROM ANYWHERE A CAPTAIN CAN STAND — one ring, pointing at the
    /// tube, at every distance out to the backstop itself.
    ///
    /// <para>Swept in RANGE rather than tested at one spot, because the failure this guards is a range gate
    /// (an instrument that helpfully stops painting what it cannot resolve), and a gate is invisible to any
    /// test taken inside it. The bearing is checked against the tube mouth's own coordinates rather than a
    /// number typed here, so a ring that points somewhere plausible and wrong still reddens.</para></summary>
    [Fact]
    public void TheWayHome_IsOnTheFanAtEveryDistanceOutToTheBackstop()
    {
        // The spot the fan and the tank both measure to: the square outside the tube's surface door, where
        // the boots land. NOT SurfaceTiles.TubeMouth() — that is the landing band, 5.5 du down-field, and it
        // is what the BACKSTOP measures from. The two are the two ends of one tube; the guard below holds
        // them within a tube's length of each other so neither can wander off alone.
        (double tubeX, double tubeY) = (MoonSurface.SpawnX, MoonSurface.SpawnY);
        int readings = 0;

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                foreach (double range in new[] { 5.0, 60.0, 400.0, 2_500.0, 9_000.0, SurfaceTiles.BackstopRadiusDu })
                {
                    for (int i = 0; i < 6; i++)
                    {
                        double bearing = i * Math.Tau / 6.0;
                        double x = tubeX + (Math.Cos(bearing) * range);
                        double y = tubeY + (Math.Sin(bearing) * range);

                        Map map = StandingOutOn(body, site, x, y);
                        var beacons =
                            (List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)>)
                            Call(map, "BuildBeacons");

                        var home = beacons.FindAll(b => b.IsHome);
                        Assert.True(home.Count == 1,
                            $"{body} · {site.Name}: {home.Count} way-home rings at {range:F0} du out. A " +
                            "captain out here has no way of knowing which way the tube is.");

                        // The ring points AT THE TUBE — read back off the picture, the way a player reads it.
                        double px = x + (home[0].Range * Math.Cos(home[0].Bearing));
                        double py = y + (home[0].Range * Math.Sin(home[0].Bearing));
                        Assert.Equal(tubeX, px, 3);
                        Assert.Equal(tubeY, py, 3);
                        readings++;
                    }
                }
            }
        }

        Assert.True(readings > 100, $"only {readings} readings were taken — that is not a sweep.");
    }

    /// <summary>AND THE SUIT PRICES THE SAME DISTANCE IN AIR. The ring says which way; the readout says
    /// whether you can afford it, and it is the same distance both times — the tube mouth's, measured from
    /// where the captain stands. Two instruments disagreeing about how far home is would be the worst
    /// version of this project's oldest bug, on the one number an excursion is decided by.</summary>
    [Fact]
    public void TheReadout_MeasuresTheSameWayHomeTheFanPointsAt()
    {
        (double tubeX, double tubeY) = (MoonSurface.SpawnX, MoonSurface.SpawnY);

        // The backstop's own idea of the way home is the other end of the same tube. Held here, because a
        // day when one of them moves and the other does not is a day the suit and the boundary are measuring
        // two different journeys.
        (double bx, double by) = SurfaceTiles.TubeMouth();
        Assert.True(Math.Sqrt(((bx - tubeX) * (bx - tubeX)) + ((by - tubeY) * (by - tubeY))) < 12.0,
            "the suit's way home and the backstop's way home have drifted more than a tube apart.");

        foreach (string body in Bodies)
        {
            LandingSite site = LandingSites.For(body)[0];
            foreach (double range in new[] { 12.0, 300.0, 4_000.0 })
            {
                Map map = StandingOutOn(body, site, tubeX + range, tubeY);
                double measured = (double)(typeof(Map)
                    .GetMethod("DistanceToTheTube", Hidden)!.Invoke(map, [])!);

                Assert.Equal(range, measured, 3);

                var beacons =
                    (List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)>)
                    Call(map, "BuildBeacons");
                Assert.Equal(measured, beacons.Find(b => b.IsHome).Range, 3);
            }
        }
    }

    // ── the bench ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A shipping page standing on the open regolith of one site, at one spot on the ground.</summary>
    private static Map StandingOutOn(string body, LandingSite site, double x, double y)
    {
        var map = new Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Type exType = typeof(Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("Site")!.SetValue(ex, site);
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_surface", ex);
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        return map;
    }

    private static void Set(object target, string field, object? value) =>
        (typeof(Map).GetField(field, Hidden)
         ?? throw new InvalidOperationException($"Map has no field {field}."))
        .SetValue(target, value);

    private static object Call(Map map, string method) =>
        (typeof(Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no {method} — the paint site has moved."))
        .Invoke(map, [typeof(Map).GetField("_surface", Hidden)!.GetValue(map)])!;
}
