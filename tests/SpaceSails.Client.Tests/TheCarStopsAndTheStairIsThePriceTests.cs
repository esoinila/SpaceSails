using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #719 slice 2 · <b>THE MAINTENANCE BREAK, DRIVEN.</b>
///
/// <para>Owner, 2026-08-05: <i>"just stopping the elevator by remote of radio message would stop all escape
/// way too easy :-d"</i> / <i>"going up would use more air"</i>. Core holds the rule about WHERE a break may
/// land (<c>TheMaintenanceBreakIsAPriceTests</c>); this file holds the two things only a running page can
/// answer — <b>whether it actually fires on the outcome it is supposed to</b>, and <b>whether a captain it
/// has fired on can still get out of the building</b>.</para>
///
/// <para>Everything here is driven through the shipped verbs. The challenge is provoked the way a player
/// provokes it — three wrong codes on the keypad (#602/#1106), which fetches the nearest man, who walks
/// over and reads the wallet — and then the panel, the pad, the fan, the tank and the stair are asked what
/// they say about it. Nothing sets the flag by hand except the audit, which is measuring the ESCAPE and
/// deliberately does not care how the car came to be stopped.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCarStopsAndTheStairIsThePriceTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const double Dt = 1.0 / 60.0;
    private const long Watch = 7;

    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A site, and a floor of it, where the shipped panel really draws a keypad, the rota really covers the
    /// floor, AND Core really allows a break there. All three asked rather than typed — a hand-picked triple
    /// goes stale the first time a generator moves, and a bench standing on a floor with no pad, no man or
    /// no stair on it would pass forever about nothing.
    /// </summary>
    private static (string Body, int Floor, UndergroundComplex.LiftStop Row) APaddedPatrolledFloorWithAStair()
    {
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.LiftCode.PaperRoomFor(body) is null)
            {
                continue;
            }
            foreach (int floor in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, floor)
                    || !UndergroundComplex.ACallCanStopTheCarOn(body, floor))
                {
                    continue;
                }
                foreach (UndergroundComplex.LiftStop stop in UndergroundComplex.LiftPanel(body, floor, []))
                {
                    if (stop.HasPad)
                    {
                        return (body, floor, stop);
                    }
                }
            }
        }

        throw new InvalidOperationException(
            "no site in the sweep has a keypad on a patrolled floor that admits a break — this bench has "
            + "nothing to drive.");
    }

    /// <summary>A shipping page standing on one floor of one site, with a round on it. <c>AtThePad</c>'s own
    /// bench (<c>ThreeWrongEntriesFetchSomebodyTests</c>), one feature along.</summary>
    private static Pages.Map OnTheFloor(string body, int floor, int heads, double air, bool deck = true)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the page's verbs will throw instead of running.");
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
        exType.GetProperty("Floor")!.SetValue(ex, floor);
        exType.GetProperty("AirSeconds")!.SetValue(ex, air);
        exType.GetProperty("CanteenWatch")!.SetValue(ex, Watch);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        if (heads > 0)
        {
            Set(map, "_patrolCheat", (int?)heads);
        }

        if (deck)
        {
            Invoke(map, "RebuildSurfaceDeck");
            Invoke(map, "SpawnPatrolFor", Ex(map));
        }
        return map;
    }

    private static object Ex(Pages.Map map) => Get(map, "_surface")!;

    private static bool CarStopped(Pages.Map map) =>
        (bool)Ex(map).GetType().GetProperty("CarStopped")!.GetValue(Ex(map))!;

    private static void StopTheCar(Pages.Map map) =>
        Ex(map).GetType().GetProperty("CarStopped")!.SetValue(Ex(map), true);

    private static int FloorOf(Pages.Map map) =>
        (int)Ex(map).GetType().GetProperty("Floor")!.GetValue(Ex(map))!;

    private static double AirOf(Pages.Map map) =>
        (double)Ex(map).GetType().GetProperty("AirSeconds")!.GetValue(Ex(map))!;

    private static IReadOnlyList<UndergroundComplex.LiftStop> Rows(Pages.Map map) =>
        (IReadOnlyList<UndergroundComplex.LiftStop>)
        typeof(Pages.Map).GetMethod("LiftStops", Hidden)!.Invoke(map, [])!;

    private static bool PadIsDark(Pages.Map map) =>
        (bool)typeof(Pages.Map).GetProperty("LiftPadIsDark", Hidden)!.GetValue(map)!;

    private static bool PanelSaysStopped(Pages.Map map) =>
        (bool)typeof(Pages.Map).GetProperty("TheCarIsStopped", Hidden)!.GetValue(map)!;

    private static double WayHome(Pages.Map map) =>
        (double)typeof(Pages.Map).GetMethod("DistanceToTheTube", Hidden)!.Invoke(map, [])!;

    private static List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> Fan(Pages.Map map) =>
        (List<(double, double, bool, bool, bool)>)
        typeof(Pages.Map).GetMethod("BuildBeacons", Hidden)!.Invoke(map, [Ex(map)])!;

    /// <summary>Key four digits and press ↵, through the shipped verbs and nothing else.</summary>
    private static void Key(Pages.Map map, UndergroundComplex.LiftStop row, string code)
    {
        typeof(Pages.Map).GetMethod("LiftPadClear", Hidden)!.Invoke(map, []);
        foreach (char c in code)
        {
            typeof(Pages.Map).GetMethod("LiftPadPush", Hidden)!.Invoke(map, [c.ToString()]);
        }
        typeof(Pages.Map).GetMethod("LiftPadSubmit", Hidden)!.Invoke(map, [row]);
    }

    private static string AWrongCode(string body)
    {
        string right = UndergroundComplex.LiftCode.CodeFor(body);
        string wrong = right == "1111" ? "2222" : "1111";
        Assert.False(UndergroundComplex.LiftCode.Answers(body, wrong));
        return wrong;
    }

    /// <summary>A spot on the first guard's own next leg — ground his legs really publish a route across.
    /// <c>ThreeWrongEntriesFetchSomebodyTests</c>' helper, verbatim in shape.</summary>
    private static (double X, double Y) DownHisOwnCorridor(Pages.Map map, double du)
    {
        object g = Guards(map)[0];
        var beat = (List<PatrolBeat.Stop>)Get(map, "_patrolBeat")!;
        PatrolBeat.Stop at = beat[(int)Get(g, "Leg")!];
        double gx = (double)Get(g, "X")!, gy = (double)Get(g, "Y")!;
        double dx = at.X - gx, dy = at.Y - gy;
        double len = Math.Sqrt((dx * dx) + (dy * dy));

        Assert.True(len > 1.0, "the first guard's next stop is on top of him; this bench has nothing to walk.");
        double along = Math.Min(du, len - 0.5);
        return (gx + (dx / len * along), gy + (dy / len * along));
    }

    private static IReadOnlyList<object> Guards(Pages.Map map) =>
        ((System.Collections.IEnumerable)Get(Get(map, "_patrol")!, "Guards")!).Cast<object>().ToList();

    private static void Frames(Pages.Map map, int n)
    {
        MethodInfo step = typeof(Pages.Map).GetMethod("AdvancePatrol", Hidden)!;
        for (int i = 0; i < n; i++)
        {
            step.Invoke(map, [Dt]);
        }
    }

    /// <summary>Provoke the challenge the way a player does: stand at the pad, miss three times, and let the
    /// nearest man walk over and read the wallet. Returns once his card is up.</summary>
    private static void TheRoundReadsYourWallet(
        Pages.Map map, string body, UndergroundComplex.LiftStop row)
    {
        (double x, double y) = DownHisOwnCorridor(map, 14.0);
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);

        string wrong = AWrongCode(body);
        Key(map, row, wrong);
        Key(map, row, wrong);
        Key(map, row, wrong);

        Frames(map, 900);

        object? up = Get(map, "_viewObject");
        Assert.True(up is not null && (string)Get(up, "Label")! == PatrolBeat.ChallengeLabel,
            "nobody came over and read the wallet in 900 frames, so this case is about nothing.");
    }

    // ── (a) THE TRIGGER ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE WALLET THAT COULD NOT ANSWER COSTS YOU THE CAR — AND THE ONE THAT COULD DOES NOT.</b>
    ///
    /// <para>Both halves on the same floor of the same site, provoked the same way, differing in exactly one
    /// thing: what is in the captain's pocket. That is what makes the pair a guard instead of two
    /// observations — a world that cannot tell a pass from a refusal would go green on both.</para>
    ///
    /// <para>Read off the four instruments a player actually reads: the panel has no floors on it, the
    /// arithmetic behind the keypad says the keys are out, and the flag itself. On the passing arm every one
    /// of them says the opposite, and the panel still has its whole directory.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>TheCarIsStoppedForMaintenance(ex)</c> call from
    /// <c>TheRoundStopsAtYou</c>:</para>
    /// <code>
    /// Assert.True() Failure — the wallet could not answer and the car is still running.
    /// </code>
    /// </summary>
    [Fact]
    public void AWalletThatCannotAnswerStopsTheCarAndAPassDoesNot()
    {
        (string body, int floor, UndergroundComplex.LiftStop row) = APaddedPatrolledFloorWithAStair();

        // ── THE REFUSAL ──
        Pages.Map refused = OnTheFloor(body, floor, heads: 2, SuitAir.TankSeconds);
        Assert.False(CarStopped(refused), "the car was stopped before anything happened to this captain.");
        Assert.NotEmpty(Rows(refused));

        TheRoundReadsYourWallet(refused, body, row);

        Assert.True(CarStopped(refused), "the wallet could not answer and the car is still running.");
        Assert.True(PanelSaysStopped(refused), "the flag is set and the panel does not know it.");
        Assert.Empty(Rows(refused));
        Assert.True(PadIsDark(refused), "the car is stopped and the keypad is still live.");

        // ── THE PASS, WHICH IS THE CONTROL ──
        Pages.Map passed = OnTheFloor(body, floor, heads: 2, SuitAir.TankSeconds);
        Set(passed, "_satchel", new List<Satchel.Item> { PatrolBeat.Badge(body) });
        Assert.True(PatrolBeat.BadgeHeld(body, (IReadOnlyList<Satchel.Item>)Get(passed, "_satchel")!),
            "the control arm is not actually carrying this site's pass, so it proves nothing.");

        TheRoundReadsYourWallet(passed, body, row);

        Assert.False(CarStopped(passed),
            "the pass worked and the car stopped anyway — the break is not the challenge's outcome, it is "
            + "a thing that happens to anybody a guard looks at.");
        Assert.False(PanelSaysStopped(passed));
        Assert.NotEmpty(Rows(passed));
    }

    /// <summary>
    /// <b>AND IT IS NOT RESET BY WALKING AWAY — only by being back on the surface.</b> The owner's ruling in
    /// two assertions: a hundred and eighty seconds of the floor going on around a stopped car change
    /// nothing, and then the one thing that does change it is the arrival.
    ///
    /// <para>The arrival is taken by the STAIR, because that is the only way out a captain in this state
    /// has — so this also happens to be the shortest statement of the whole feature: the break is survivable
    /// and it ends when the building is behind you.</para>
    ///
    /// <para><b>Proven RED twice, once per half.</b> Clearing <c>CarStopped</c> from inside
    /// <c>AdvancePatrol</c> — a plausible "it decays like the pad's window" edit — reddens the first:</para>
    /// <code>
    /// three minutes on the floor and the car came back. Nothing but the surface may clear the break.
    /// </code>
    /// <para>…and dropping the <c>ex.CarStopped = false</c> out of <c>RideTheLiftTo</c>'s level-0 arm
    /// reddens the second:</para>
    /// <code>
    /// the captain is standing on the regolith and the building is still holding a maintenance ticket
    /// against them.
    /// </code>
    /// </summary>
    [Fact]
    public void TheBreakSurvivesTheFloorAndEndsOnTheSurface()
    {
        (string body, int floor, UndergroundComplex.LiftStop row) = APaddedPatrolledFloorWithAStair();

        Pages.Map map = OnTheFloor(body, floor, heads: 2, SuitAir.TankSeconds);
        TheRoundReadsYourWallet(map, body, row);
        Assert.True(CarStopped(map));

        Set(map, "_viewObject", null);
        Frames(map, 60 * 180);
        Assert.True(CarStopped(map),
            "three minutes on the floor and the car came back. Nothing but the surface may clear the break.");
        Assert.Empty(Rows(map));

        // …AND THEN OUT, THE ONLY WAY THERE IS.
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot stair = deck.Consoles.Single(c => c.Kind == DeckPlan.ConsoleKind.HiveStair);
        Set(map, "_avatarX", (double)stair.X);
        Set(map, "_avatarY", (double)stair.Y);

        double before = AirOf(map);
        double climb = UndergroundComplex.ClimbAirSeconds(Field, floor);
        Invoke(map, "ClimbTheStairOut");

        Assert.Equal(0, FloorOf(map));
        Assert.Equal(before - climb, AirOf(map), 3);
        Assert.False(CarStopped(map),
            "the captain is standing on the regolith and the building is still holding a maintenance ticket "
            + "against them.");
        Assert.False(PanelSaysStopped(map));
    }

    // ── (b) THE ESCAPE, AUDITED ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY FLOOR A BREAK CAN LAND ON STILL REACHES THE SURFACE WITH THE CAR STOPPED.</b> #1115's audit,
    /// re-driven in the one state it was shipped to survive — and this time the press is taken, so the walk
    /// and the way out are proved on the same floor in the same run.
    ///
    /// <para>Three things per floor, each separately reported: the panel really is empty (a break that left
    /// one live button would not be a break), the stair console is reachable from the CAGE's own doorstep and
    /// BACK (#600's whole lesson — an exit you can reach and cannot leave is the bug in a new coat), and the
    /// press really puts the captain on the regolith with the climb out of the tank.</para>
    ///
    /// <para>The flag is set by hand here, and deliberately: this leg is about the ESCAPE and has no opinion
    /// about how the car came to be stopped. How it comes to be stopped is case (a).</para>
    ///
    /// <para><b>Proven RED</b> by having <c>ClimbTheStairOut</c> return early while the car is stopped — the
    /// exact shape of the softlock #719 exists to prevent:</para>
    /// <code>
    /// 127 of 127 floor(s) break the law: a stopped car is a price and never a locked building
    ///   luna B1: the car is stopped and the stair did not take the captain out — this is the softlock.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryFloorThatCanBeStoppedStillWalksOutByTheStair()
    {
        var bad = new List<string>();
        int seen = 0;

        (double, double, double, double) whole =
            (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.ACallCanStopTheCarOn(body, level))
                {
                    continue;
                }
                seen++;

                Pages.Map map = OnTheFloor(body, level, heads: 0, SuitAir.TankSeconds);
                StopTheCar(map);

                if (Rows(map).Count != 0)
                {
                    bad.Add($"  {body} B{-level}: the car is stopped and the panel still offers "
                        + $"{Rows(map).Count} floor(s).");
                    continue;
                }

                var deck = (DeckPlan)Get(map, "_deckPlan")!;
                if (deck.Consoles.FirstOrDefault(c => c.Kind == DeckPlan.ConsoleKind.HiveStair) is not
                    { Kind: DeckPlan.ConsoleKind.HiveStair } console)
                {
                    bad.Add($"  {body} B{-level}: the car is stopped and there is no stair console on the "
                        + "floor — this captain is in a building with no way out of it.");
                    continue;
                }

                (double cageX, double cageY) =
                    HiveInterior.SpawnOn(Field, UndergroundComplex.ShaftKind.Cage);
                var fromCage = new DeckReachability.Point(cageX, cageY);
                var atStair = new DeckReachability.Point(console.X, console.Y);

                if (!DeckReachability.CanReach(
                        fromCage, atStair, deck.CollisionField, DeckPlan.AvatarRadius, whole))
                {
                    bad.Add($"  {body} B{-level}: with the car stopped the stair cannot be walked to from "
                        + "the spot every arrival puts you on.");
                    continue;
                }
                if (!DeckReachability.CanReach(
                        atStair, fromCage, deck.CollisionField, DeckPlan.AvatarRadius, whole))
                {
                    bad.Add($"  {body} B{-level}: the walk reaches the stair and does not come back — #600 "
                        + "in a new coat.");
                    continue;
                }

                Set(map, "_avatarX", (double)console.X);
                Set(map, "_avatarY", (double)console.Y);
                double before = AirOf(map);
                double climb = UndergroundComplex.ClimbAirSeconds(Field, level);
                Invoke(map, "ClimbTheStairOut");

                if (FloorOf(map) != 0)
                {
                    bad.Add($"  {body} B{-level}: the car is stopped and the stair did not take the captain "
                        + "out — this is the softlock.");
                    continue;
                }
                if (Math.Abs((before - climb) - AirOf(map)) > 1e-3)
                {
                    bad.Add($"  {body} B{-level}: the climb out cost {before - AirOf(map):F1} s and Core "
                        + $"prices it at {climb:F1} s.");
                }
            }
        }

        Report(bad, seen, "a stopped car is a price and never a locked building", 100);
    }

    // ── (c) THE INSTRUMENTS ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE HOME RING MOVES FROM THE CAGE TO THE STAIR DOOR, AND THE READOUT PRICES THE JOURNEY IT POINTS
    /// AT.</b>
    ///
    /// <para>This is #1115's one flagged judgement call being paid off, and both halves have to be asked
    /// together or neither means anything. With the car running the fan's first HOME ring is the CAGE and
    /// the readout is the tube's own distance — that is the shipped world, and it is the control. With the
    /// car stopped there is exactly ONE home ring, it is the stair door, and the readout is the walk to that
    /// door plus the climb.</para>
    ///
    /// <para>The ring is read back off the picture the way a player reads it — bearing and range projected
    /// from where the captain stands — rather than compared against a number typed here, so a ring that
    /// points somewhere plausible and wrong still reddens.</para>
    ///
    /// <para><b>Proven RED twice, once per instrument.</b> Leaving the cars on the fan (dropping the
    /// <c>if (!TheCarIsStopped)</c> guard in <c>BuildBeacons</c>):</para>
    /// <code>
    /// the car is stopped and the fan paints 3 way-home ring(s). A ring pointing at a car that will not
    /// come is the map lying in the one place it costs a life.
    /// </code>
    /// <para>…and dropping the stopped-car branch out of <c>DistanceToTheTube</c>, which leaves the suit
    /// quoting the tube's own straight line through several hundred metres of rock:</para>
    /// <code>
    /// Expected: 807.094 · Actual: 135.004
    /// </code>
    /// </summary>
    [Fact]
    public void TheFanAndTheTankBothMoveToTheStairWhenTheCarStops()
    {
        (string body, int floor, _) = APaddedPatrolledFloorWithAStair();

        // ── THE CONTROL: the car is running ──
        Pages.Map running = OnTheFloor(body, floor, heads: 0, SuitAir.TankSeconds);
        (double cageX, double cageY) = UndergroundComplex.ShaftAt(Field);
        Set(running, "_avatarX", cageX - 40.0);
        Set(running, "_avatarY", cageY - 3.0);

        List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> before = Fan(running);
        List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> homeBefore =
            before.FindAll(b => b.IsHome);
        Assert.True(homeBefore.Count >= 2,
            "with the car running the fan should be painting the cars AND the stair; it paints "
            + $"{homeBefore.Count} way-home ring(s), so the control arm is not the shipped world.");

        (double px, double py) = Project(running, homeBefore[0]);
        (double landX, double landY) =
            (cageX, cageY + UndergroundComplex.CorridorHalf + 1.5 + 1.0);
        Assert.Equal(landX, px, 2);
        Assert.Equal(landY, py, 2);

        double tube = Math.Sqrt(
            (((double)Get(running, "_avatarX")! - MoonSurface.SpawnX)
             * ((double)Get(running, "_avatarX")! - MoonSurface.SpawnX))
            + (((double)Get(running, "_avatarY")! - MoonSurface.SpawnY)
               * ((double)Get(running, "_avatarY")! - MoonSurface.SpawnY)));
        Assert.Equal(tube, WayHome(running), 3);

        // ── AND THE BREAK ──
        Pages.Map stopped = OnTheFloor(body, floor, heads: 0, SuitAir.TankSeconds);
        StopTheCar(stopped);
        Set(stopped, "_avatarX", cageX - 40.0);
        Set(stopped, "_avatarY", cageY - 3.0);

        List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> home =
            Fan(stopped).FindAll(b => b.IsHome);
        Assert.True(home.Count == 1,
            $"the car is stopped and the fan paints {home.Count} way-home ring(s). A ring pointing at a car "
            + "that will not come is the map lying in the one place it costs a life.");

        (double sx, double sy) = Project(stopped, home[0]);
        (double doorX, double doorY) = UndergroundComplex.StairRingAt(Field)!.Value;
        Assert.Equal(doorX, sx, 2);
        Assert.Equal(doorY, sy, 2);

        // …AND THE TANK PRICES THAT JOURNEY. Core's own number, not one retyped here.
        double owed = UndergroundComplex.WayOutByStairDu(
            Field, floor, (double)Get(stopped, "_avatarX")!, (double)Get(stopped, "_avatarY")!);
        Assert.Equal(owed, WayHome(stopped), 3);

        // …and it is a LONGER way home than the free ride was, which is the whole of "you pay".
        Assert.True(WayHome(stopped) > WayHome(running),
            $"the car has stopped and the suit thinks the way home got no worse: {WayHome(running):F1} du "
            + $"before, {WayHome(stopped):F1} du after.");
    }

    private static (double X, double Y) Project(
        Pages.Map map, (double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead) ring)
    {
        double x = (double)Get(map, "_avatarX")!, y = (double)Get(map, "_avatarY")!;
        return (x + (ring.Range * Math.Cos(ring.Bearing)), y + (ring.Range * Math.Sin(ring.Bearing)));
    }

    // ── (d) WHAT IT MAY NOT DO ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOTHING ABOUT THE BREAK IS WRITTEN DOWN.</b> The owner's ruling — <i>"the next excursion finds it
    /// running (nobody files a maintenance ticket against a man who left)"</i> — asked of the payload the
    /// game actually saves: stop the car, build the vault the autosave builds, serialize it, and there is no
    /// trace of the break anywhere in it.
    ///
    /// <para><b>And the same payload is checked for something that IS in it</b>, in the same breath. An
    /// absence-assertion over a blob is the emptiest guard there is — a serializer that returned "" would
    /// pass it forever — so the control names a fact this vault does carry, and only a payload that can tell
    /// pass from fail gets to make the negative claim.</para>
    ///
    /// <para><b>Proven RED</b> by adding a <c>CarStopped</c> to <c>ProgressSection</c> and filling it in
    /// <c>BuildVault</c>:</para>
    /// <code>
    /// Assert.DoesNotContain() Failure: Sub-string found (pos 926) · Found: "carStopped"
    /// </code>
    /// </summary>
    [Fact]
    public void TheBreakIsNotInTheSaveFile()
    {
        (string body, int floor, _) = APaddedPatrolledFloorWithAStair();
        Pages.Map map = OnTheFloor(body, floor, heads: 0, SuitAir.TankSeconds, deck: false);
        StopTheCar(map);
        Assert.True(CarStopped(map), "the bench did not manage to stop the car, so this proves nothing.");

        Set(map, "_worldReady", true);
        var vault = (Vault)typeof(Pages.Map)
            .GetMethod("BuildVault", Hidden)!.Invoke(map, ["", ""])!;
        string payload = VaultSerializer.Save(vault);

        // THE CONTROL, FIRST. A payload that cannot tell pass from fail may not be used to prove an absence.
        Assert.True(payload.Length > 200,
            $"the vault serialized to {payload.Length} characters — this world cannot tell pass from fail.");
        Assert.Contains("savedSimTime", payload, StringComparison.Ordinal);

        foreach (string trace in new[] { "CarStopped", "carStopped", "MAINTENANCE" })
        {
            Assert.DoesNotContain(trace, payload, StringComparison.Ordinal);
        }

        // …and a landing that has not had one starts with the car running, which is the other half of the
        // same ruling: the flag's default IS "nobody has called anybody".
        Pages.Map next = OnTheFloor(body, floor, heads: 0, SuitAir.TankSeconds, deck: false);
        Assert.False(CarStopped(next), "a fresh excursion begins with the car already stopped.");
    }

    /// <summary>
    /// <b>THE PLATE IS THE ONLY THING THIS SLICE SAYS OUT LOUD — asked of the SOURCE, not of a type.</b>
    ///
    /// <para>Reflection catches a new <c>const</c>; it does not catch a sentence typed straight into a
    /// method, which is how prose actually gets into this codebase. So both of the slice's own files are
    /// read and every string literal outside their comments is collected: there must be exactly one, and it
    /// must be the plate.</para>
    ///
    /// <para><b>Proven RED</b> by pulsing a line from <c>TheCarIsStoppedForMaintenance</c>:</para>
    /// <code>
    /// the break's own files carry 2 string literal(s): "CAR STOPPED · MAINTENANCE", "The car will not
    /// come." — the plate is meant to be the whole of what this slice says.
    /// </code>
    /// </summary>
    [Fact]
    public void TheOnlyStringInTheBreaksOwnFilesIsThePlate()
    {
        string root = RepoRoot();
        string[] files =
        [
            Path.Combine(root, "src", "SpaceSails.Core", "UndergroundComplex.Break.cs"),
            Path.Combine(root, "src", "SpaceSails.Client", "Pages", "Map.Surface.Break.cs"),
        ];

        var found = new List<string>();
        foreach (string file in files)
        {
            Assert.True(File.Exists(file), $"{file} is gone — this guard is watching a file that moved.");
            string code = WithoutComments(File.ReadAllText(file));
            foreach (Match m in Regex.Matches(code, "\"(?:[^\"\\\\\\n]|\\\\.)*\""))
            {
                found.Add(m.Value);
            }
        }

        Assert.True(found.Count == 1 && found[0] == $"\"{UndergroundComplex.CarStoppedPlate}\"",
            $"the break's own files carry {found.Count} string literal(s): {string.Join(", ", found)} — the "
            + "plate is meant to be the whole of what this slice says out loud, and a beat that wants a "
            + "sentence gets a // FABLE: marker rather than a line typed by this crew.");
    }

    // ── the bench's plumbing ──────────────────────────────────────────────────────────────────────────

    private static string WithoutComments(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, @"//[^\n]*", " ");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException($"no repo root above {AppContext.BaseDirectory}");
    }

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

    // The round's state and verbs moved off the page in #870 lane 6'b/6'c, so every raw name goes through
    // PatrolState — the one place the strings are written down. A guard spelling them itself is invisible to
    // the compiler and would go quietly green the day one of them moved.
    private static void Set(object o, string field, object? value)
    {
        if (!PatrolState.TrySet(o, field, value))
        {
            (o.GetType().GetField(field, Hidden)
             ?? throw new InvalidOperationException($"{o.GetType().Name} has no field {field}."))
            .SetValue(o, value);
        }
    }

    private static object? Get(object o, string name)
    {
        if (PatrolState.TryFollow(o, name, out object? onTheRound))
        {
            return onTheRound;
        }
        for (Type? t = o.GetType(); t is not null; t = t.BaseType)
        {
            if (t.GetField(name, Hidden | BindingFlags.Public) is { } f)
            {
                return f.GetValue(o);
            }
            if (t.GetProperty(name, Hidden | BindingFlags.Public) is { } p)
            {
                return p.GetValue(o);
            }
        }
        throw new InvalidOperationException($"{o.GetType().Name} has no {name}.");
    }

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        (object Target, MethodInfo Call)? found = PatrolState.Verb(map, method);
        Assert.True(found is not null,
            $"neither the page nor its round has `{method}` — this guard is reading a dead name.");
        return found!.Value.Call.Invoke(found.Value.Target, args);
    }
}
