using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #442 · <b>A SLEEPER IS BEHIND THE DOOR TOO.</b> Owner ruling, 2026-09-06:
/// <i>"the Reevers should not see through a closed door."</i>
///
/// <para>#1099 shipped the one-wall-one-truth net and left one reader named and unfixed:
/// <c>Map.Surface.Reevers.cs</c>'s DORMANT branch asked <c>_deckPlan.CollisionField</c> — the LEGS' list —
/// while every awake contact eighteen lines below it asks <c>SightBlockers()</c>, the eye's list of stone
/// PLUS whatever is shut this instant. Opacity is not solidity, which is the whole of #442, and a shut hatch
/// is opaque: aboard a wreck a sleeper folded down behind a dogged leaf was drawn straight through it.</para>
///
/// <para>And the lamp that DRAWS a sleeper is the same lamp that WAKES it (<c>if (now &gt;= r.WakeAtMs ||
/// inLamp)</c>), so this was never only a drawing bug — a captain who had shut a hatch roused whatever was on
/// the far side of it without ever laying eyes on the thing. That is the half the owner ruled on, and it is
/// what the second case here holds: the hatch is the wake.</para>
///
/// <para><b>Why a driven guard and not a geometry one.</b> Both lists are the same object graph and both
/// answer honestly; the defect is entirely in WHICH ONE the page hands the sleeper, and only the page knows
/// that. So this drives real <c>StepReevers</c> frames on the shipping wreck deck with a real dormant contact
/// on it, and reads the answer off the contact's own fields.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ASleeperIsBehindTheDoorTooTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;

    /// <summary>A sentinel no wreck deck can produce, written into the contact's memory before the drive so a
    /// WRITE is observable as a write. A guard that watched for a CHANGE would pass on a build that wrote the
    /// captain's real position when the captain happened to be standing on the default.</summary>
    private const double NeverSeenAnyone = -12345.0;

    // ── THE PREMISE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE BENCH IS THE CASE IT CLAIMS TO BE. Three separate things have to be true of the spot this file
    /// uses, and if any one of them quietly stopped holding, every assertion below would pass for a reason
    /// that is not the rule:
    ///
    /// <list type="number">
    /// <item>the captain has a line to the sleeper through <b>stone alone</b> — so nothing but the leaf is
    /// ever in the way, and "it was not seen" cannot be the bulkhead's doing;</item>
    /// <item>the hatch between them is <b>shut</b> with the captain standing where the bench puts them, and
    /// the sight list therefore <b>stops</b> that same line;</item>
    /// <item>the sleeper is <b>inside the lamp</b> — nearer than <c>DormantSightRange</c> — so range is not
    /// what is hiding it either.</item>
    /// </list>
    /// </summary>
    [Fact]
    public void ThePremise_OnlyTheLeafStandsBetweenTheCaptainAndTheSleeper()
    {
        Pages.Map map = OnAHull();
        Spot spot = TheHatchWithSomethingBehindIt(map);
        var deck = (DeckPlan)Get(map, "_deckPlan")!;

        StandTheCaptainAt(map, spot.ShutFromX, spot.ShutFromY);

        Assert.True(
            SurfaceCollision.HasLineOfSight(spot.ShutFromX, spot.ShutFromY, spot.SleeperX, spot.SleeperY,
                deck.CollisionField),
            "stone stands between the captain and the sleeper on this bench — the leaf is then not the thing "
            + "under test and every 'it was not seen' below would pass on a bulkhead.");

        Assert.False(
            SurfaceCollision.HasLineOfSight(spot.ShutFromX, spot.ShutFromY, spot.SleeperX, spot.SleeperY,
                SightBlockers(map)),
            "the eye's own list lets the look through — either the hatch is not shut with the captain here, "
            + "or the line does not cross it, and this file is measuring nothing.");

        double dx = spot.SleeperX - spot.ShutFromX, dy = spot.SleeperY - spot.ShutFromY;
        double range = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.True(range <= DormantSightRange(),
            $"the sleeper is {range:F2} du off and the lamp reaches {DormantSightRange():F2} du — RANGE would "
            + "be hiding it, not the door, and the guard would pass on the old code.");

        // …and the OPEN pose has to be a real pose too: near enough that the leaf retracts, still inside the
        // lamp, and still with nothing but the (now open) doorway between the two.
        StandTheCaptainAt(map, spot.OpenFromX, spot.OpenFromY);
        Assert.True(
            SurfaceCollision.HasLineOfSight(spot.OpenFromX, spot.OpenFromY, spot.SleeperX, spot.SleeperY,
                SightBlockers(map)),
            "the hatch does not open with the captain standing at it, so the second half of this file could "
            + "never distinguish a fixed build from a broken one.");
    }

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A SLEEPER BEHIND A SHUT HATCH IS NOT SEEN, AND IS NOT WOKEN. The captain stands in its line for a
    /// full minute of frames — inside the lamp, with only the leaf between them — and the hull stays a hull
    /// she was told was dead: nothing drawn, nothing roused, nothing written into anything's memory.
    ///
    /// <para><b>Proven RED</b> by reverting the fix (the dormant branch reading <c>walls</c> again). See the
    /// PR body for the verbatim failure.</para>
    /// </summary>
    [Fact]
    public void AShutHatchHidesASleeper_AndDoesNotWakeIt()
    {
        Pages.Map map = OnAHull();
        Spot spot = TheHatchWithSomethingBehindIt(map);
        object sleeper = LayOneDownAt(map, spot.SleeperX, spot.SleeperY);
        StandTheCaptainAt(map, spot.ShutFromX, spot.ShutFromY);

        RunFrames(map, seconds: 60);

        Assert.True(Flag(sleeper, "Dormant"),
            "a shut hatch woke the thing behind it: the captain's lamp reached through the leaf, which is "
            + "exactly the ruling of 2026-09-06 — the Reevers do not see through a closed door.");
        Assert.False(Flag(sleeper, "VisibleOnMap"),
            "the sleeper is drawn on the deck plan through a shut hatch — the map is the captain's eyes, and "
            + "the captain cannot see it.");
        Assert.False(Flag(sleeper, "EverSeen"),
            "it fixed on the captain from behind a shut door without ever having looked at anything.");
        Assert.Equal(NeverSeenAnyone, Memory(sleeper, "LastSeenX"), 6);
        Assert.Equal(NeverSeenAnyone, Memory(sleeper, "LastSeenY"), 6);
    }

    /// <summary>
    /// …AND THE HATCH IS THE WAKE. The same bench, the same contact, the same lamp — the captain simply walks
    /// up to the door, which is the only verb this game has for opening one (<c>Airlock.MayOpen</c> off the
    /// captain's own distance; the renderer and the sight list read that one answer). The leaf retracts, the
    /// lamp finds what is folded down on the other side of it, and the hull stops being empty.
    ///
    /// <para>This is the half that makes the guard above a law rather than an accident: a build in which
    /// nothing can ever wake a sleeper by eye would satisfy the first case perfectly.</para>
    /// </summary>
    [Fact]
    public void OpeningTheHatchIsWhatWakesIt()
    {
        Pages.Map map = OnAHull();
        Spot spot = TheHatchWithSomethingBehindIt(map);
        object sleeper = LayOneDownAt(map, spot.SleeperX, spot.SleeperY);

        StandTheCaptainAt(map, spot.ShutFromX, spot.ShutFromY);
        RunFrames(map, seconds: 5);
        Assert.True(Flag(sleeper, "Dormant"), "it woke before the captain had touched the door.");

        // The door verb: stand at it. Nothing else is asked of the player and nothing else is asked here.
        StandTheCaptainAt(map, spot.OpenFromX, spot.OpenFromY);
        RunFrames(map, seconds: 1);

        Assert.False(Flag(sleeper, "Dormant"),
            "the captain opened the hatch and stood in the doorway of a compartment with an Old One folded "
            + "down in it, inside the lamp, and nothing came round — the fix has closed the eye rather than "
            + "given it a door.");
        Assert.True(Flag(sleeper, "VisibleOnMap"), "it woke and is still not drawn.");
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One hatch on the shipping wreck deck, with a pose either side of it: where the captain stands
    /// to have it SHUT in their face, where they stand to have it open, and where the sleeper lies beyond
    /// it.</summary>
    private readonly record struct Spot(
        double ShutFromX, double ShutFromY, double OpenFromX, double OpenFromY, double SleeperX, double SleeperY);

    /// <summary>
    /// Find that hatch by ASKING THE DECK, never by typing four numbers off a dump: a hull whose compartments
    /// move (they do — the deck is rebuilt from evidence, salvage, venting and the #537 plate) would leave
    /// hand-typed coordinates standing in open space, and every case in this file would go quietly green.
    ///
    /// <para>Swept perpendicular to each unlocked leaf: the captain outside the door's own opening radius, the
    /// sleeper a short way beyond it, and the whole triple accepted only when stone alone lets the look
    /// through, the shut leaf stops it, the lamp reaches, and a step to the doorway opens it again.</para>
    /// </summary>
    private static Spot TheHatchWithSomethingBehindIt(Pages.Map map)
    {
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        double lamp = DormantSightRange();
        double keptX = (double)Get(map, "_avatarX")!, keptY = (double)Get(map, "_avatarY")!;

        try
        {
            foreach (DeckPlan.Door d in deck.Doors)
            {
                if (d.Locked)
                {
                    continue;   // a locked hatch is shut whatever the captain does — no OPEN half to prove
                }

                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                double dx = d.X2 - d.X1, dy = d.Y2 - d.Y1;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 1e-9)
                {
                    continue;
                }
                double px = -dy / len, py = dx / len;   // the leaf's own normal: across the doorway

                foreach (int side in new[] { 1, -1 })
                {
                    for (double back = DeckPlan.DoorOpenRadius + 0.5; back <= lamp - 1.0; back += 0.25)
                    {
                        double cx = mx + (px * back * side), cy = my + (py * back * side);
                        for (double beyond = 1.0; beyond <= lamp - back; beyond += 0.25)
                        {
                            double sx = mx - (px * beyond * side), sy = my - (py * beyond * side);

                            if (!SurfaceCollision.HasLineOfSight(cx, cy, sx, sy, deck.CollisionField))
                            {
                                continue;   // a bulkhead is in the way: proves nothing about a leaf
                            }

                            StandTheCaptainAt(map, cx, cy);
                            if (SurfaceCollision.HasLineOfSight(cx, cy, sx, sy, SightBlockers(map)))
                            {
                                continue;   // this hatch is not shut from here, or the line misses it
                            }

                            // …and a pose that OPENS it: half the radius in, on the captain's own side.
                            double ox = mx + (px * (DeckPlan.DoorOpenRadius / 2.0) * side);
                            double oy = my + (py * (DeckPlan.DoorOpenRadius / 2.0) * side);
                            StandTheCaptainAt(map, ox, oy);
                            if (!SurfaceCollision.HasLineOfSight(ox, oy, sx, sy, SightBlockers(map)))
                            {
                                continue;
                            }

                            return new Spot(cx, cy, ox, oy, sx, sy);
                        }
                    }
                }
            }
        }
        finally
        {
            StandTheCaptainAt(map, keptX, keptY);
        }

        throw new InvalidOperationException(
            $"no hatch on this hull ({deck.Doors.Length} door(s), {deck.CollisionSegments.Length} segment(s)) "
            + "has a pose either side of it where the leaf and nothing else stands between the captain and a "
            + "spot inside the lamp. Either the wreck deck has stopped having compartments, or a shut door "
            + "has stopped stopping the eye — and #442's whole family of guards rests on both.");
    }

    /// <summary>A live component aboard the shipping derelict, past the arrival grace, with the surface clock
    /// running and nothing else in the sim alive.</summary>
    private static Pages.Map OnAHull()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        var wreck = new Derelict.Wreck(
            "sleeper-bench", "Bench Hull", Derelict.WreckCause.Infested, 250_000, 40.0);
        string bodyId = Derelict.BodyIdFor(wreck.Id);

        Type exType = typeof(Pages.Map).GetNestedType(
            "SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType(
            "ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(bodyId, wreck.ShipName, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        exType.GetProperty("LandedAtMs")!.SetValue(ex, 0.0);

        Set(map, "_wreck", wreck);
        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_weaponsTight", true);   // so the spawn's one warning line is not owed
        // Well past SurfaceArrival.SpotGraceSeconds: a bench inside the grace would prove that nothing sees
        // the captain, for the wrong reason, and pass.
        Set(map, "_lastTimestampMs", (double?)(SurfaceArrival.SpotGraceSeconds * 1000.0 * 3));

        Invoke(map, "RebuildSurfaceDeck");
        Assert.True((bool)Get(map, "OnWreck")!, "the bench is not aboard a wreck.");
        Assert.True(((DeckPlan)Get(map, "_deckPlan")!).Doors.Length > 0,
            "the wreck deck has no doors at all, so this whole file is about nothing.");
        return map;
    }

    /// <summary>One hibernating Old One, folded down where the bench puts it, on a clock that will never come
    /// round on its own — so the ONLY thing that can wake it in these cases is the lamp, which is the thing
    /// under test. Its memory is pre-loaded with a sentinel so a write is observable as a write.</summary>
    private static object LayOneDownAt(Pages.Map map, double x, double y)
    {
        Type reever = typeof(Pages.Map).GetNestedType("Reever", Hidden | BindingFlags.Public)!;
        object one = Activator.CreateInstance(reever, nonPublic: true)!;
        Set(one, "X", x);
        Set(one, "Y", y);
        Set(one, "AnchorX", x);
        Set(one, "AnchorY", y);
        Set(one, "Facing", 0.0);
        Set(one, "Dormant", true);
        Set(one, "VisibleOnMap", false);
        Set(one, "EverSeen", false);
        Set(one, "WakeAtMs", double.MaxValue);
        Set(one, "LastSeenX", NeverSeenAnyone);
        Set(one, "LastSeenY", NeverSeenAnyone);
        Set(one, "JitterSeed", 0xD1B54A32D192ED03UL);
        ((IList)Get(map, "_reevers")!).Add(one);
        return one;
    }

    /// <summary>Put the captain's boots on a spot. Both the door's own open/shut answer and the sleeper's
    /// lamp are measured off this one pair of numbers, which is the point: one source of truth.</summary>
    private static void StandTheCaptainAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    /// <summary>Whole surface frames at the shipping rate, with the clock advancing — the real
    /// <c>StepReevers</c>, not a re-implementation of what it is thought to do.</summary>
    private static void RunFrames(Pages.Map map, double seconds)
    {
        const double dt = 1.0 / 60.0;
        for (int i = 0; i < (int)Math.Round(seconds / dt); i++)
        {
            Set(map, "_lastTimestampMs",
                (double?)((Get(map, "_lastTimestampMs") as double? ?? 0) + (dt * 1000.0)));
            Invoke(map, "StepReevers", dt);
        }
    }

    /// <summary>The page's OWN sight list — walls plus whatever is shut with the captain standing where they
    /// are standing this instant. Asked of the shipping method rather than rebuilt here, so a bench that
    /// disagreed with the game about what is opaque cannot exist.</summary>
    private static IReadOnlyList<SurfaceCollision.Segment> SightBlockers(Pages.Map map) =>
        (IReadOnlyList<SurfaceCollision.Segment>)Invoke(map, "SightBlockers")!;

    /// <summary>How far the lamp reaches for a hibernating contact — read off the page's own constant so a
    /// re-tuning moves this bench instead of silently making it prove nothing.</summary>
    private static double DormantSightRange() =>
        (double)typeof(Pages.Map).GetField("DormantSightRange", Hidden)!.GetValue(null)!;

    private static bool Flag(object r, string name) =>
        (bool)r.GetType().GetField(name, Hidden)!.GetValue(r)!;

    private static double Memory(object r, string name) =>
        (double)r.GetType().GetField(name, Hidden)!.GetValue(r)!;

    private static object? Get(object o, string name)
    {
        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"the component has no `{name}` — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);
}
