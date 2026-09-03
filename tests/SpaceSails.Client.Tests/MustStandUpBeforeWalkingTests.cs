using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #847 · MUST STAND UP BEFORE WALKING.
///
/// <para>Owner's ruling, 2026-08-13, with the receipt attached: <i>"Must stand up before walking (not
/// standing up before doing that was my Pyramid climbing exercise in Giza last January … legs were sore
/// :-D)"</i> — and the shape of it: <i>"WASD (and click-to-walk) while seated at ANY seat — stool, bench,
/// table, office chair, cubicle — first routes through the seat's own stand-up path (the #846 step-off,
/// scene teardown and all), THEN walks. No refusal line needed — the keys simply cost you the stand first,
/// which is how chairs work."</i></para>
///
/// <h3>What was wrong, and why nothing caught it</h3>
///
/// <para>#784 answered a movement key from a chair with a POP-UP, and it only ever asked the TABLE flag
/// (<c>CaptainIsSeated</c>, which is <c>_table is not null</c>). A counter stool has never been in that flag
/// — it is a posture of the bar card, not a table — so at the counter WASD did the shipped thing: the dot
/// walked off along the service run with the card still saying <i>you are sitting on a stool</i>. It was
/// invisible for as long as sitting down left the captain standing wherever they pressed [E]; #820's snap
/// put the dot ON the seat, and from that evening on the bug was a body sliding out of its own chair.</para>
///
/// <h3>Why these guards are driven and not read</h3>
///
/// <para>Every fact below is asked of a REAL <see cref="Pages.Map"/> on a REAL generated floor: the seats
/// are taken with the shipping verbs (<c>TakeAStool</c>, <c>TryTakeBench</c>, <c>TryTakeOfficeChair</c>,
/// <c>TryTakeTable</c>), the key is pressed through <c>HandleDeckKey</c>, the click goes through
/// <c>ClickToWalkAt</c>, and the frame is spent in <c>MoveAvatar</c>. A guard that read the source for the
/// word <c>StandUpBeforeWalking</c> would pass on a build where the call had been moved below the plan, or
/// where the stool's own teardown had quietly been copied out — which is this repo's first named bug class
/// and its fifth in one paragraph.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class MustStandUpBeforeWalkingTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The floors this file drives. One site is enough to prove a law about a key handler, and the
    /// seats themselves are swept for existence by <c>EverySeatIsSomewhereYouCanSitTests</c> next door — but
    /// the FLOOR is the generator's, never a hand-typed room, because a guard handed a world it built itself
    /// cannot tell pass from fail.</summary>
    private const string Body = "luna";

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to sit down on.");

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A live component on a real Hive floor, with nothing running but the deck.
    ///
    /// <para>The one piece of theatre is the render handle. A <see cref="ComponentBase"/> that was never
    /// attached to a renderer throws out of <c>StateHasChanged</c>, and every seat verb in the game ends
    /// with one — so the component is told it already has a render queued, which is the framework's own
    /// early-out and makes the call a silent no-op. Nothing else about the component is faked: this is the
    /// shipping <see cref="Pages.Map"/> holding the shipping excursion over the shipping floor.</para>
    /// </summary>
    private static Pages.Map OnTheFloor()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on " +
                "has moved, and the seat verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, TheFloor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        // #875 · NOTHING LEFT TO SWITCH ON. This bench used to set #729's ?autowalk=1 flag right here,
        // because click-to-walk did not exist off it. The owner ruled the click always-on ("the two should
        // be linked as alternative UI methods for walking"), the flag retired to a no-op alias, and a bench
        // that has to enable a control before it can test it is testing a build nobody boots.

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    // ── THE FOUR SEATS, TAKEN WITH THE GAME'S OWN VERBS ───────────────────────────────────────────────

    /// <summary>What a seat is, for a test that has to sit on four different ones and then ask the same
    /// question of each: how you get on it, and where the game says standing up puts you.</summary>
    private sealed record Seat(string Name, Action<Pages.Map> SitDown);

    private static IReadOnlyList<Seat> EverySeatKind() =>
    [
        new("a stool at the counter", TakeAStool),
        new("a park bench", TakeABench),
        new("an office chair", TakeAnOfficeChair),
        new("a canteen table", TakeATable),
    ];

    private static void TakeAStool(Pages.Map map)
    {
        Invoke(map, "TakeAStool");
        Assert.True(Get(map, "_stool") is not null,
            "nobody got onto a stool at all — this bench cannot tell a stand-up from a no-op.");
    }

    private static void TakeABench(Pages.Map map)
    {
        StandAtAConsole(map, DeckPlan.ConsoleKind.HiveBench);
        Assert.True((bool)Invoke(map, "TryTakeBench")!, "the press at the bench was not taken.");
        Assert.True(Get(map, "_table") is not null, "nobody sat on the bench.");
    }

    private static void TakeAnOfficeChair(Pages.Map map)
    {
        StandAtAConsole(map, DeckPlan.ConsoleKind.HiveOfficeChair);
        Assert.True((bool)Invoke(map, "TryTakeOfficeChair")!, "the press at the chair was not taken.");
        Assert.True(Get(map, "_table") is not null, "nobody sat in the office chair.");
    }

    private static void TakeATable(Pages.Map map)
    {
        StandAtAConsole(map, DeckPlan.ConsoleKind.HiveTable);
        Assert.True((bool)Invoke(map, "TryTakeTable")!, "the press at the free table was not taken.");
        Assert.True(Get(map, "_table") is not null, "nobody sat at the table.");
    }

    /// <summary>Walk the captain up to the first fixture of a kind — the console the renderer draws and the
    /// [E] key dispatches through, off the deck the game just built, so the press lands on the real thing.</summary>
    private static void StandAtAConsole(Pages.Map map, DeckPlan.ConsoleKind kind)
    {
        DeckPlan.ConsoleSpot[] spots = [.. ThePlan(map).Consoles.Where(c => c.Kind == kind)];
        Assert.True(spots.Length > 0,
            $"this floor carves no {kind} — the guard below would be asserting about a seat that is not there.");
        Set(map, "_avatarX", (double)spots[0].X);
        Set(map, "_avatarY", (double)spots[0].Y);
    }

    // ── (a) THE STOOL · THE GAP #847 WAS FILED ON ─────────────────────────────────────────────────────

    /// <summary>
    /// #847 · A MOVEMENT KEY AT THE COUNTER GETS YOU OFF THE STOOL, and the legs work again afterwards.
    ///
    /// <para>RED on the base commit for the plainest possible reason: <c>_stool</c> was in no flag any
    /// movement path asked about, so the press did nothing to the seat at all and the very next frame walked
    /// the captain along the bar.</para>
    ///
    /// <para>And it gets you off it through the COUNTER'S OWN VERB — the notice the keep leaves on the card
    /// is <see cref="TheStools.GotDownLine"/>, the one <c>GetDownFromStool</c> writes. A hand-written copy of
    /// getting down would pass a test that only looked at <c>_stool</c> going null, and the day getting off a
    /// stool costs a beat or a watch, the copy is the one that would not pay it.</para>
    /// </summary>
    [Fact]
    public void WASD_OnAStoolGetsYouDownOffIt()
    {
        Pages.Map map = OnTheFloor();
        TakeAStool(map);

        (double x, double y) = Where(map);

        Assert.True((bool)Invoke(map, "HandleDeckKey", "d")!, "the deck did not take the movement key.");

        Assert.True(Get(map, "_stool") is null,
            "W/A/S/D at the counter left the captain ON the stool — the press cost nothing, and the frame " +
            "after it walks a seated body out of its own seat.");
        Assert.Equal(TheStools.GotDownLine, (string?)Get(map, "_barNotice"));

        // A stool is bolted a standoff off the counter on floor a captain could have walked to anyway, so
        // its step-off IS the seat: getting down leaves you exactly where you were sitting.
        Assert.Equal((x, y), Where(map));

        // …and now the legs are the captain's again. This is the "THEN walks" half of the ruling: a stand
        // that left the body frozen would satisfy every line above and be a worse bug than the one fixed.
        Assert.True(TheLegsWork(map),
            "the captain got off the stool and then could not walk in any direction — the stand-up took the " +
            "seat away and gave nothing back.");
    }

    // ── (b) THE TABLE FAMILY · THROUGH THE STAND-UP PATH, ONTO THE STEP-OFF ───────────────────────────

    /// <summary>
    /// #847 · A MOVEMENT KEY ON A PARK BENCH RUNS THE WHOLE STAND-UP, teardown and step-off and line.
    ///
    /// <para>The bench is the seat this is asked of because it is the one seat in the game where standing up
    /// has to MOVE THE BODY: a plank is a solid segment in the collision field, so #820's snap puts the
    /// captain inside furniture, and <c>TableTalk.StepOff</c> is the walk side of it. A press that tore the
    /// sitting down without going through <c>StandUpFromTable</c> → <c>CloseTable</c> would leave the dot
    /// standing in the steel — which is exactly the trap the whole seated law is built around.</para>
    ///
    /// <para>Three separate facts, and each one can fail on its own: the scene is GONE, the body is on the
    /// seat's own published square, and the one line the stand says is the one #784 wrote for it.</para>
    /// </summary>
    [Fact]
    public void WASD_OnABenchStandsYouUpAtTheStepOffSquare()
    {
        Pages.Map map = OnTheFloor();
        TakeABench(map);

        (double sx, double sy) = Where(map);
        (double ox, double oy) = TheStepOff(map);
        Assert.True(Math.Abs(ox - sx) + Math.Abs(oy - sy) > 1e-6,
            "this bench steps off onto the seat itself, so the guard below could not tell a captain who " +
            "stood up from one who never moved. The bench carve has changed under this test.");

        Assert.True((bool)Invoke(map, "HandleDeckKey", "w")!, "the deck did not take the movement key.");

        Assert.True(Get(map, "_table") is null,
            "W/A/S/D on a bench left the sitting open — the scene is still up and the captain is still in it.");
        Assert.Equal((ox, oy), Where(map));
        Assert.Equal(SeatedPosture.StoodUpToWalkLine, ThePulse(map));
        Assert.True(TheLegsWork(map), "off the bench and still unable to walk.");
    }

    // ── (c) THE CLICK PAYS THE SAME PRICE, AND ROUTES FROM WHERE IT LEFT YOU ──────────────────────────

    /// <summary>
    /// #847 · …AND THE MOUSE DOES NOT WALK AROUND THE LAW. The ruling names both grips, and a law the
    /// keyboard obeys and the pointer ignores is half a law.
    ///
    /// <para>The second half of this guard is the ORDER, which is the only thing about the click that is a
    /// judgement call. The route is planned AFTER the stand, so its first waypoint is off the step-off square
    /// and not off the seat — plan it first and the captain, having been stood up onto the walk side of the
    /// plank, would set off by walking BACK into the bench they just left. The two squares are metres apart
    /// on a bench, which is why the bench is the seat this is asked of.</para>
    ///
    /// <para>What must NOT move is the target: the pixel is resolved through the projection that was on the
    /// glass when the finger went down, because the camera follows the captain and standing up moves it.</para>
    /// </summary>
    [Fact]
    public void AClickedRouteAlsoCostsTheStandAndSetsOffFromTheStepOff()
    {
        Pages.Map map = OnTheFloor();
        TakeABench(map);

        (double sx, double sy) = Where(map);
        (double ox, double oy) = TheStepOff(map);

        // Somewhere to walk TO: the nearest of the fixtures this floor INVITES you to walk to — the same
        // list #585's reachability audit floods toward, so the target is a promise the game already makes
        // rather than a coordinate this test picked out of a room.
        DeckPlan plan = ThePlan(map);
        DeckPlan.ConsoleSpot target = plan.Consoles
            .Where(c => c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveLift
                or DeckPlan.ConsoleKind.HiveAmenity or DeckPlan.ConsoleKind.HiveRefuge)
            .OrderBy(c => ((c.X - sx) * (c.X - sx)) + ((c.Y - sy) * (c.Y - sy)))
            .First();

        // The pixel a captain would have pressed to point at it, through the projection the renderer is
        // drawing with RIGHT NOW — that is, with the captain still on the bench.
        DeckView.Placement glass = DeckView.PlacementFor(
            plan, (int)Get(map, "_viewportWidth")!, (int)Get(map, "_viewportHeight")!,
            sx, sy, (double)Get(map, "_deckPanX")!, (double)Get(map, "_deckPanY")!);
        double px = glass.Ox + (target.X * glass.Scale);
        double py = glass.Oy - (target.Y * glass.Scale);

        Invoke(map, "ClickToWalkAt", px, py);

        Assert.True(Get(map, "_table") is null,
            "a clicked route left the captain sitting on the bench — the pointer is walking around a law " +
            "the keyboard pays.");
        Assert.Equal((ox, oy), Where(map));

        var route = (AutoWalk?)Get(map, "_autoWalk");
        Assert.True(route is { Active: true },
            "the click planned no route at all. This target is reachable from the step-off square, so the " +
            "one thing it is not reachable from is the BENCH END the captain was sitting on — which is a " +
            "route planned before the stand, over a start inside solid steel.");

        DeckReachability.Point first = route!.Route[0];
        double toStepOff = ((first.X - ox) * (first.X - ox)) + ((first.Y - oy) * (first.Y - oy));
        double toSeat = ((first.X - sx) * (first.X - sx)) + ((first.Y - sy) * (first.Y - sy));
        Assert.True(toStepOff < toSeat,
            $"the route starts at ({first.X:0.00},{first.Y:0.00}), which is nearer the BENCH END " +
            $"({sx:0.00},{sy:0.00}) than the step-off ({ox:0.00},{oy:0.00}) — it was planned before the " +
            "captain stood up, so the walk sets off by stepping back into the seat.");

        // …and THEN it walks. Frames only, no keys: the route the click laid is spent by the real stepper
        // until it is done, and the captain is standing at the thing they pointed at.
        for (int frame = 0; frame < 2000 && route.Active; frame++)
        {
            Invoke(map, "MoveAvatar", 0.1);
        }
        (double ax, double ay) = Where(map);
        Assert.True(target.DistanceFrom(ax, ay) <= DeckPlan.InteractRadius,
            $"the walk stopped at ({ax:0.00},{ay:0.00}), which is not within reach of the fixture the " +
            $"captain clicked at ({target.X:0.00},{target.Y:0.00}).");
    }

    // ── (d) WHILE ANY SITTING IS OPEN, A FRAME MOVES NOBODY ───────────────────────────────────────────

    /// <summary>
    /// #847 · THE SECOND HALF OF THE LAW, asked of EVERY SEAT KIND: while a sitting is open, no frame moves
    /// the body, whatever is in the held-key set.
    ///
    /// <para>The key handler pays for the stand on the press, and that is not enough on its own — a key HELD
    /// BEFORE the captain sat down is still held, and a route the auto-walk is mid-way through is still a
    /// route. So the frame refuses too, and it refuses on the ANY-SEAT question rather than the table's.</para>
    ///
    /// <para>This is the guard the #847 revert lands on. Put <c>MoveAvatar</c>'s stop back on
    /// <c>CaptainIsSeated</c> — the table flag, which is what shipped — and the three table-shaped seats go
    /// on passing while the STOOL goes red, walking a captain along the counter out of a card that still has
    /// them sitting on it. Four seats are driven for exactly that reason: a guard that only sat at a table
    /// could not have told the two flags apart, and that is why nothing caught this for a fortnight.</para>
    /// </summary>
    [Fact]
    public void NoSeatLetsAHeldKeyWalkTheCaptainOut()
    {
        foreach (Seat seat in EverySeatKind())
        {
            Pages.Map map = OnTheFloor();
            seat.SitDown(map);

            (double x, double y) = Where(map);

            // Straight into the held set, bypassing the press — this is the key that was already down when
            // the captain sat, which no key handler will ever see again.
            var held = (HashSet<string>)Get(map, "_deckKeys")!;
            held.Clear();
            held.Add("d");

            for (int frame = 0; frame < 30; frame++)
            {
                Invoke(map, "MoveAvatar", 0.1);
            }

            (double nx, double ny) = Where(map);
            Assert.True(Math.Abs(nx - x) < 1e-9 && Math.Abs(ny - y) < 1e-9,
                $"a held key walked the captain off {seat.Name}: they sat at ({x:0.00},{y:0.00}) and the " +
                $"frame put them at ({nx:0.00},{ny:0.00}) with the sitting still open. The seat is still " +
                "drawn where it was, so the sim and the picture now disagree about where the captain is.");
        }
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Can the captain walk at all right now? Tried in all four directions, because a floor is a
    /// room and any one heading may be a wall. One frame each, through the shipping stepper.</summary>
    private static bool TheLegsWork(Pages.Map map)
    {
        var held = (HashSet<string>)Get(map, "_deckKeys")!;
        foreach (string key in new[] { "w", "a", "s", "d" })
        {
            (double x, double y) = Where(map);
            held.Clear();
            held.Add(key);
            Invoke(map, "MoveAvatar", 0.1);
            (double nx, double ny) = Where(map);
            if (Math.Abs(nx - x) > 1e-9 || Math.Abs(ny - y) > 1e-9)
            {
                return true;
            }
        }
        return false;
    }

    private static (double X, double Y) Where(Pages.Map map) =>
        ((double)Get(map, "_avatarX")!, (double)Get(map, "_avatarY")!);

    /// <summary>Where the open sitting says standing up will put the body — Core's own square, carried on
    /// the sitting, read here rather than measured.</summary>
    private static (double X, double Y) TheStepOff(Pages.Map map)
    {
        object sitting = Get(map, "_table")
            ?? throw new InvalidOperationException("nobody is sitting at anything.");
        object step = sitting.GetType().GetProperty("StepOff")!.GetValue(sitting)
            ?? throw new InvalidOperationException("this sitting publishes no step-off square.");
        return ((double)step.GetType().GetField("Item1")!.GetValue(step)!,
                (double)step.GetType().GetField("Item2")!.GetValue(step)!);
    }

    private static string? ThePulse(Pages.Map map) => ((PulseSlot)Get(map, "_pulse")!).Message;

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    /// <summary>#870 lane 6b - The five seat fields live on the page's <c>_seating</c> object now, so this
    /// follows them there (<see cref="SeatState"/>); every assertion below still asks for the state by the
    /// name it was written with.</summary>
    private static object? Get(object o, string field) =>
        SeatState.TryFollow(o, field, out object? seated)
            ? seated
            : o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }
}
