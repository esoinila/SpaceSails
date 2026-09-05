using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #719 · <b>THE AUDIT GROWS A SECOND COLUMN.</b>
///
/// <para>#600 is the scar this file exists to respect: <i>an A* audit proves you can REACH the lift, never
/// that it is a way HOME.</i> So the law here is stated per floor and per EXIT, and both halves are asked of
/// each one — the exit is reachable from the car's own standable spot AND the walk comes back, and then the
/// stair is pressed and the captain is actually on the surface with the air gone out of the tank.</para>
///
/// <para><b>Why a client test.</b> Core knows where the stair is cut; only the deck knows whether a BODY
/// gets to it — a gap can be open in the segments and still be narrower than the avatar, or opened onto
/// furniture laid afterwards — and only the page knows what the press does.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
// #251 · NOT slow-gated: measured at ~4 s for the four tests, well under the roster's ten-second cut, and
// a law about whether the second way out can be WALKED to is one the fast loop should be running.
public sealed class TheStairIsAWayHomeTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0);

    private static void Report(List<string> bad, int seen, string law, int atLeast)
    {
        Assert.True(seen >= atLeast, $"only {seen} floor(s) were walked — this proved nothing about {law}.");
        if (bad.Count == 0)
        {
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} of {seen} floor(s) break the law: {law}");
        foreach (string line in bad.Take(20))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }

    // ── (a) THE SECOND COLUMN ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY LISTED FLOOR REACHES THE SURFACE BY THE STAIR AS WELL AS BY THE CAGE, AND THE WALK GOES BOTH
    /// WAYS.
    ///
    /// <para>Three questions per floor, each one separately reported so a red run says which broke:</para>
    /// <list type="bullet">
    /// <item>the stair's own doorstep is somewhere a captain can stand;</item>
    /// <item>it is reachable from the CAGE's doorstep — the spot every arrival puts them on;</item>
    /// <item>and the walk comes BACK, which is #600's whole lesson said about a second exit: an exit you can
    /// reach and cannot leave is the bug wearing a different hat.</item>
    /// </list>
    ///
    /// <para>The [E] press that turns standing there into being on the surface is asserted below; this leg
    /// is about the legs.</para>
    ///
    /// <para><b>Proven RED</b> by not cutting the mouth — <c>CarveStair</c>'s <c>alcoveMouths.Add(...)</c>
    /// commented out, so the plan draws a leaf and the wall stays poured. <b>The first cut of this guard did
    /// NOT go red on that break</b>, because it walked to <c>Shaft.Landing</c>, which is in the corridor on
    /// the near side of the leaf: a walk that never crosses the door cannot tell a door from a wall. It walks
    /// to the console now — see the comment in the loop.</para>
    /// <code>
    /// 127 of 127 floor(s) break the law: every listed floor reaches the stair, and gets back
    ///   luna B1: the stair cannot be walked to from the car everybody arrives in — a second way out
    ///   nobody can get to is not one.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryListedFloorWalksToTheStairAndBackFromTheCage()
    {
        var bad = new List<string>();
        int seen = 0;

        (double, double, double, double) whole =
            (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.HasStairOn(body, level))
                {
                    continue;
                }
                seen++;

                if (UndergroundComplex.StairOn(Field) is not { } stair)
                {
                    bad.Add($"  {body} B{-level}: there is no stair on this ground at all.");
                    continue;
                }

                DeckPlan deck = DeckFor(body, level);

                // WHERE THE WALK STARTS: the cage's own doorstep, asked of the shaft rather than re-typed
                // (#801 put a captain in a wall that way once already).
                (double cageX, double cageY) = HiveInterior.SpawnOn(Field, UndergroundComplex.ShaftKind.Cage);

                // …AND WHERE IT ENDS: the CONSOLE, which is inside the pocket, on the far side of the leaf.
                //
                // Not Shaft.Landing, and this is the whole difference between a guard and a green tick. Every
                // shaft's landing is "a pace out of the car, ON THE SPINE" — it is in the CORRIDOR, the same
                // side of the face the walk starts on — so a walk that ended there never crossed the door and
                // passed just as happily with the mouth left uncut. Watched happen: commenting out
                // CarveStair's alcoveMouths.Add and re-running gave "Passed! 1". The press is what a captain
                // has come here for, so the press is what the walk has to reach.
                if (deck.Consoles.FirstOrDefault(c => c.Kind == DeckPlan.ConsoleKind.HiveStair) is not
                    { Kind: DeckPlan.ConsoleKind.HiveStair } console)
                {
                    bad.Add($"  {body} B{-level}: no HiveStair console to walk to.");
                    continue;
                }
                double stairX = console.X, stairY = console.Y;

                var fromCage = new DeckReachability.Point(cageX, cageY);
                var atStair = new DeckReachability.Point(stairX, stairY);

                if (!DeckReachability.Standable(stairX, stairY, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    bad.Add($"  {body} B{-level}: the stair's doorstep at ({stairX:F1}, {stairY:F1}) is solid.");
                    continue;
                }
                if (!DeckReachability.Standable(cageX, cageY, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    bad.Add($"  {body} B{-level}: the CAGE's doorstep is solid — the walk has nowhere to "
                        + "start, and this floor has a bigger problem than a stair.");
                    continue;
                }

                if (!DeckReachability.CanReach(
                        fromCage, atStair, deck.CollisionField, DeckPlan.AvatarRadius, whole))
                {
                    bad.Add($"  {body} B{-level}: the stair cannot be walked to from the car everybody "
                        + "arrives in — a second way out nobody can get to is not one.");
                    continue;
                }

                if (!DeckReachability.CanReach(
                        atStair, fromCage, deck.CollisionField, DeckPlan.AvatarRadius, whole))
                {
                    bad.Add($"  {body} B{-level}: the walk reaches the stair and does not come back — #600 "
                        + "in a new coat.");
                }
            }
        }

        Report(bad, seen, "every listed floor reaches the stair, and gets back", 100);
    }

    /// <summary>
    /// AND THE PLATE IS ON THE DECK, at the door, on every one of those floors — the one thing this feature
    /// says out loud, drawn where a captain reads it. A console the renderer never places is a feature wired
    /// to a key nobody can press, which is the bug class <c>SceneInventoryTests</c> keeps a list for.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>HasStairOn</c> arm out of <c>HiveInterior</c>:</para>
    /// <code>
    /// 127 of 127 floor(s) break the law: the stair is a console on the deck with its plate on it
    ///   luna B1: 0 HiveStair console(s) on the deck — the door is a picture, or the building grew a spare.
    /// </code>
    /// </summary>
    [Fact]
    public void TheStairIsAConsoleOnEveryListedFloorWithThePlateOnIt()
    {
        var bad = new List<string>();
        int seen = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.HasStairOn(body, level))
                {
                    continue;
                }
                seen++;

                List<DeckPlan.ConsoleSpot> stairs = DeckFor(body, level).Consoles
                    .Where(c => c.Kind == DeckPlan.ConsoleKind.HiveStair)
                    .ToList();

                if (stairs.Count != 1)
                {
                    bad.Add($"  {body} B{-level}: {stairs.Count} HiveStair console(s) on the deck — the "
                        + "door is a picture, or the building grew a spare.");
                    continue;
                }
                if (!string.Equals(stairs[0].Label, UndergroundComplex.StairSign, StringComparison.Ordinal))
                {
                    bad.Add($"  {body} B{-level}: the console is labelled \"{stairs[0].Label}\" and Core "
                        + $"paints \"{UndergroundComplex.StairSign}\".");
                }
            }
        }

        Report(bad, seen, "the stair is a console on the deck with its plate on it", 100);
    }

    // ── (b) THE PRESS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A shipping page standing on one floor of one site, through the same bench every other Hive
    /// verb in this suite is driven from.</summary>
    private static Pages.Map OnTheFloor(string body, int level, double air)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType(
            "SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType(
            "ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, level);
        exType.GetProperty("AirSeconds")!.SetValue(ex, air);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);

    private static double AirOf(Pages.Map map) =>
        (double)Get(map, "_surface")!.GetType().GetProperty("AirSeconds")!.GetValue(Get(map, "_surface"))!;

    private static int FloorOf(Pages.Map map) =>
        (int)Get(map, "_surface")!.GetType().GetProperty("Floor")!.GetValue(Get(map, "_surface"))!;

    /// <summary>
    /// RIDE DOWN, TAKE THE STAIR UP, AND ARRIVE ON THE SURFACE WITH THE AIR DEDUCTED — the whole loop, in one
    /// press, on the deck the boots actually collide with.
    ///
    /// <para>The tank is checked against <c>UndergroundComplex.ClimbAirSeconds</c> rather than against a
    /// number typed here, for this file's usual reason: a test that retyped the price would pass on a client
    /// that had quietly stopped asking Core for it.</para>
    ///
    /// <para><b>Proven RED</b> by having <c>ClimbTheStairOut</c> ride without draining:</para>
    /// <code>
    /// Assert.Equal() Failure: Values are not within 3 decimal places
    /// Expected: 1074.771 (rounded from 1074.7711111111112)
    /// Actual:   1200 (rounded from 1200)
    /// </code>
    /// </summary>
    [Fact]
    public void TheStairPutsTheCaptainOnTheSurfaceAndTakesTheClimbOutOfTheTank()
    {
        const string Body = "luna";
        int level = UndergroundComplex.DepthOf(Body);
        Assert.True(UndergroundComplex.HasStairOn(Body, level),
            $"{Body}'s listed bottom carries no stair, so this whole case is about nothing.");

        Pages.Map map = OnTheFloor(Body, level, SuitAir.TankSeconds);

        DeckPlan deck = (DeckPlan)Get(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot at = deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.HiveStair);
        Set(map, "_avatarX", (double)at.X);
        Set(map, "_avatarY", (double)at.Y);

        double before = AirOf(map);
        double climb = UndergroundComplex.ClimbAirSeconds(Field, level);
        Assert.True(climb > 0, "the climb is free, which is what the CAR is.");

        Invoke(map, "ClimbTheStairOut");

        Assert.Equal(0, FloorOf(map));
        Assert.Equal(before - climb, AirOf(map), 3);

        // …and standing in the shed the car comes up into, on ground a captain can walk off (#602/#681).
        (double shedX, double shedY) = MoonSurface.LiftHead(Body, "", Field).CarFloor;
        Assert.Equal(shedX, (double)Get(map, "_avatarX")!, 1);
        Assert.Equal(shedY, (double)Get(map, "_avatarY")!, 1);
    }

    /// <summary>
    /// AND IT IS AN ESCAPE ROUTE, NOT AN ENTRANCE. The press taken from anywhere that is not the stair does
    /// nothing at all — #801's lesson, said about a third machine: the SPOT decides. Standing at the cage and
    /// invoking the climb must not teleport a captain out of a building they have not walked across.
    ///
    /// <para>And there is no way back IN: the surface deck carries the head's own panel and no stair console,
    /// so nothing anywhere can be pressed to go DOWN a flight of steps. That is what keeps the stair off
    /// every gate the cage runs — the SEALED row, the ID CHECK band, a stop order's seal — and §13.5's one
    /// earned thing earned.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>NearestConsoleSpot</c> arm from
    /// <c>ClimbTheStairOut</c>:</para>
    /// <code>
    /// Assert.Equal() Failure: Expected: -13  Actual: 0
    /// </code>
    /// </summary>
    [Fact]
    public void TheClimbIsRefusedAnywhereButTheStairAndTheSurfaceOffersNoWayDownIt()
    {
        const string Body = "luna";
        int level = UndergroundComplex.DepthOf(Body);

        Pages.Map map = OnTheFloor(Body, level, SuitAir.TankSeconds);
        (double cageX, double cageY) = HiveInterior.SpawnOn(Field, UndergroundComplex.ShaftKind.Cage);
        Set(map, "_avatarX", cageX);
        Set(map, "_avatarY", cageY);

        Invoke(map, "ClimbTheStairOut");

        Assert.Equal(level, FloorOf(map));
        Assert.Equal(SuitAir.TankSeconds, AirOf(map), 3);

        // The regolith has one console over this building and it is the head's panel. A stair console up
        // here would be a way to walk back DOWN, which is the one thing this shaft may never be.
        DeckPlan surface = MoonSurface.SurfaceDeck(
            Body, Body, [], 0, (_, _) => { }, siteSalt: "", siteName: "The Wild Plain", hasSecretSite: true);
        Assert.DoesNotContain(surface.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveStair);
        Assert.Contains(surface.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveHead);
    }

    // ── (c) THE ARRIVAL SAYS WHICH ROAD IT WAS ───────────────────────────────────────────────────────────

    /// <summary>
    /// THE CLIMB HAS ITS OWN SENTENCE, AND IT IS SAID ONCE. Fable canon 2026-09-04 — the line the FABLE
    /// marker in <c>Map.Surface.Hive</c> was left standing for. Three laws in one case, and the middle one is
    /// the reason the marker existed at all: the CAR's line names a machine, and saying it for a trip made on
    /// the captain's own legs would be a sentence reporting a thing the sim did not do.
    ///
    /// <list type="number">
    /// <item>a stair arrival says the stair's line, verbatim, off Core;</item>
    /// <item>it never says the car's;</item>
    /// <item>and a second climb in the same excursion says it again to nobody — the captain has been told
    /// what a climb costs, and the tank is the thing still saying it.</item>
    /// </list>
    ///
    /// <para><b>Proven RED</b> by spending the car's line on the stair (deleting the <c>byStair</c> arm):
    /// <c>Assert.Equal() Failure — Expected: Up the long way… / Actual: 🛃 The car climbs for a long
    /// time…</c>; and by dropping the <c>ex.StairArrivalSaid</c> latch, on the third assertion:
    /// <c>the climb said its line a second time in one excursion.</c></para>
    /// </summary>
    [Fact]
    public void TheClimbSaysItsOwnLineOnce_AndNeverTheCars()
    {
        const string Body = "luna";
        int level = UndergroundComplex.DepthOf(Body);
        Pages.Map map = OnTheFloor(Body, level, SuitAir.TankSeconds);

        StandAtTheStair(map);
        Invoke(map, "ClimbTheStairOut");
        Assert.Equal(0, FloorOf(map));

        string? said = ((PulseSlot)Get(map, "_pulse")!).Message;
        Assert.Equal(UndergroundComplex.StairArrivalLine, said);
        Assert.DoesNotContain("car climbs", said!, StringComparison.OrdinalIgnoreCase);

        // Back down the way the captain came, and up the long way a second time. Nothing new is said: the
        // beat belongs to the excursion, not to the flight of steps.
        Get(map, "_surface")!.GetType().GetProperty("Floor")!.SetValue(Get(map, "_surface"), level);
        Invoke(map, "RebuildSurfaceDeck");
        Set(map, "_pulse", PulseSlot.Empty);
        StandAtTheStair(map);
        Invoke(map, "ClimbTheStairOut");

        Assert.Equal(0, FloorOf(map));
        Assert.NotEqual(UndergroundComplex.StairArrivalLine, ((PulseSlot)Get(map, "_pulse")!).Message);
    }

    /// <summary>Put the captain on the stair's own console spot, wherever this floor's plan cut it.</summary>
    private static void StandAtTheStair(Pages.Map map)
    {
        DeckPlan deck = (DeckPlan)Get(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot at = deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.HiveStair);
        Set(map, "_avatarX", (double)at.X);
        Set(map, "_avatarY", (double)at.Y);
    }
}
