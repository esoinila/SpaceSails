using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 7d · THE SNAPSHOT, TAKEN ON THE OLD CODE.
///
/// <para><c>AdvancePatrol</c> is a six-armed <c>if / else if</c> chain whose ORDER is the feature and is
/// named nowhere. Splitting it into named arms cannot be proved pure by counting members — the whole content
/// of the method is WHICH arm each guard takes, in which order, with which side effects — so the guard is a
/// fingerprint: a real <see cref="Pages.Map"/> on a real generated Hive floor, a fixed <c>dt</c> sequence,
/// and every guard's whole state written down after every frame, sha256'd per case and PINNED.</para>
///
/// <para><b>It went in as its own commit on the untouched method</b>, before a line of it moved, so there was
/// no chance of pinning what the new code happens to do. The digests below are the SECOND set: the first
/// thirteen were taken the same way and reproduced across the split, and then the base moved under this lane
/// and the transcript's last section had to change shape (see <see cref="WhatMoved"/>). Every digest here was
/// re-measured at the merged base and checked against the <b>untouched</b> <c>AdvancePatrol</c> as well as
/// against the split — the same thirteen either way — and on a linux runtime as well as on this desktop,
/// because the first time this file met CI was a lesson in what a snapshot is allowed to depend on.</para>
///
/// <para><b>Thirteen cases, 7,100 frames</b>, and between them they walk every arm of the chain the shipped
/// game can reach: the round's own leg (leaving a stop, spending a route, arriving, standing, planning the
/// next leg while he stands), the hail and the walk-up, walking off on him, the read and the card it raises,
/// the walk back to the car, the ride to the sky, the radio call and the run — caught, and lost — the knock
/// at a shut cubicle, a round that walks past an OCCUPIED plate it never saw turn, and a man crossing the
/// floor to somebody who has just closed a door on him. Plus both sides of the gate itself: an empty floor,
/// and the surface, where the plate is the only clock still running.</para>
///
/// <h3>What is written down</h3>
///
/// <para>After each frame: every field and property of a <c>Guard</c> (position, facing, velocity, leg,
/// standing clock, route and its cursor, the plan being made while he stands, held, walking up, after you,
/// why, knocking, signed point, cover), the page's own patrol state (the escort, the escort due, the kick-out
/// and its ride, the watch's two counters, the floor clock, the ear's cooldown, the wallet fan, the hide's
/// one line), the card that is up, the pulse slot and every autopilot log line raised. At the end of a case,
/// the whole scalar surface of the page is compared against what it was before the case started, and every
/// field that MOVED is written down with both values — so a side effect landing somewhere this file never
/// thought about still moves the hash, while a field the round has never heard of contributes nothing
/// whether some other lane invents it, renames it or retires it.</para>
///
/// <h3>Determinism</h3>
///
/// <para>The watch is fixed, the floor is the generator's own, the head count is forced through the
/// <c>?patrol=</c> cheat, and the <c>dt</c> sequence is a fixed cycle (two rAF frames, a 30 Hz one, one just
/// under the surface clamp, and one far over it so the clamp itself is walked). Nothing here reads a clock.
/// The one normalisation: every number in the transcript is rounded to six decimals before hashing, because
/// <c>Math.Atan2</c>/<c>Sin</c>/<c>Cos</c> are the platform's libm and a last-bit difference between this
/// desktop and the CI runner must not redden a guard about a code move. Six decimals is a millionth of a deck
/// unit; no arm of this chain can be taken by the wrong man and land inside that.</para>
///
/// <h3>The one arm no case can enter, and why that is said out loud</h3>
///
/// <para>The <c>g.Held</c> arm (the made tail's cover act) is UNREACHABLE from this method's own inputs
/// today, and not by accident: <c>g.Held</c> is written every frame from <see cref="FootTail.MustHold"/>,
/// which requires <c>IsTailing</c>, which is <c>Tailing &amp;&amp; !OnAPublishedRound</c> — and
/// <see cref="PatrolBeat.OnTheRound"/> stamps every guard as a published round. #793 says so in prose and
/// <see cref="TheHoldArmIsUnreachableAndTheGuardSaysSo"/> says so as a fact, so this file's silence about
/// that arm is a written-down gap rather than a case somebody forgot. Its body
/// (<c>TheCoverAct</c>) is pinned by <c>AStandingGuardIsStandingAtSomethingTests</c> and its PLACE in the
/// chain by <c>TheRoundIsNotStandingStillTests</c>, which both read the step's own source.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 29 s over 5 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed partial class EveryRoundFingerprintsTheSameTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>The watch every case is walked in. Fixed, because the beat, the plates and the head count are
    /// all functions of it — a guard that let the shift roll would be pinning a different floor every run.</summary>
    private const long Watch = 7;

    /// <summary>The frame lengths, cycled. Two live rAF frames, a 30 Hz one, one just under the surface
    /// clamp and one far over it — so <c>MaxSurfaceStepSeconds</c> is walked rather than assumed.</summary>
    private static readonly double[] Dts = [1.0 / 60.0, 1.0 / 60.0, 1.0 / 30.0, 0.09, 0.5];

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor with a round on it. The one piece of theatre is the
    /// render handle, the same one <c>MustStandUpBeforeWalkingTests.OnTheFloor</c> uses and for the same
    /// reason; everything else is the shipping page holding the shipping excursion over the shipping
    /// floor.</summary>
    private static (Pages.Map Map, object Ex) OnAPatrolledFloor(int level, int heads)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved, and the page's verbs will throw instead of running.");
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
        exType.GetProperty("Floor")!.SetValue(ex, level);
        exType.GetProperty("CanteenWatch")!.SetValue(ex, Watch);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_patrolCheat", (int?)heads);

        Invoke(map, "RebuildSurfaceDeck");
        Invoke(map, "SpawnPatrolFor", ex);
        return (map, ex);
    }

    /// <summary>The floors this file walks: every level of the site that has a round on it, lowest number of
    /// stops first — asked of Core rather than typed in, so a generator that moves the rota moves this file
    /// with it instead of leaving it asserting about an empty corridor.</summary>
    private static int ThePatrolledFloor => Enumerable.Range(1, 14)
        .Select(i => -i)
        .First(level => PatrolBeat.IsPatrolled(Body, level));

    /// <summary>…and the one this file hides on: patrolled AND carrying cubicles, because #821's three arms
    /// need a door to be behind.</summary>
    private static int TheFloorWithCubicles => Enumerable.Range(1, 14)
        .Select(i => -i)
        .First(level =>
        {
            if (!PatrolBeat.IsPatrolled(Body, level))
            {
                return false;
            }
            (Pages.Map map, object ex) = OnAPatrolledFloor(level, 2);
            return Cubicles(map, ex).Count > 0;
        });

    private static List<(UndergroundComplex.RingRoom Room, RingOffice.Stall Cell)> Cubicles(
        Pages.Map map, object ex) =>
        (List<(UndergroundComplex.RingRoom Room, RingOffice.Stall Cell)>)Invoke(map, "CubiclesOn", ex)!;

    // ── THE CASES ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One thing that can happen to a round: how the floor is staged, what the captain does on each
    /// frame, and how many frames are spent.</summary>
    private sealed record Case(
        string Name, Func<(Pages.Map Map, object Ex)> Stage, Action<Pages.Map, object, int>? EachFrame,
        int Frames);

    private static IReadOnlyList<Case> EveryCase() =>
    [
        // ── THE GATE, and the one clause above it ──────────────────────────────────────────────────
        new("an empty floor still fades the plate", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 0);
            Set(map, "_kickedOutPlateFor", PatrolBeat.KickedOutPlateSeconds);
            Nowhere(map);
            return (map, ex);
        }, null, 60),

        new("up on the surface there is no round at all", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 2);
            ex.GetType().GetProperty("Floor")!.SetValue(ex, 0);
            Set(map, "_kickedOutPlateFor", 3.0);
            Nowhere(map);
            return (map, ex);
        }, null, 40),

        // ── THE ROUND'S OWN LEG: walking, arriving, standing, planning the next one while he stands ──
        new("two men walk the round and nobody is watching", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 2);
            Nowhere(map);
            return (map, ex);
        }, null, 400),

        new("one man walks the round, all the way round it", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 1);
            Nowhere(map);
            return (map, ex);
        }, null, 400),

        // ── THE HAIL, THE APPROACH, AND WALKING OFF ────────────────────────────────────────────────
        new("he hails you and you walk away from it", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 1);
            StandInHisWay(map);
            return (map, ex);
        }, (map, ex, frame) =>
        {
            // …and once he is crossing the floor the captain simply leaves. Past GivesUpBeyondDu the walk-up
            // books it as walking off, which is the first of #835's three doors into a run.
            if (frame == 120)
            {
                Nowhere(map);
            }
        }, 400),

        // ── THE READ, AND THE WALK BACK ────────────────────────────────────────────────────────────
        new("he crosses the floor, reads your papers, and walks you to the car", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 1);
            StandInHisWay(map);
            return (map, ex);
        }, (map, ex, frame) =>
        {
            // The card comes down on the frame after it goes up — the captain pressing Esc, which is one of
            // the roads BeginTheWalkBack is armed behind.
            Set(map, "_viewObject", null);
        }, 1800),

        new("…and this time he does not press the button for your floor", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 1);
            StandInHisWay(map);
            Set(map, "_escortsThisWatch", PatrolBeat.EscortsAWatchAllows);
            return (map, ex);
        }, (map, ex, frame) => Set(map, "_viewObject", null), 1800),

        // ── THE RUN ────────────────────────────────────────────────────────────────────────────────
        new("he calls it in, comes at a run, and he has you", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 1);
            StandInHisWay(map);
            Invoke(map, "TheRadioCall", FirstGuard(map), PatrolBeat.Provocation.WalkedAwayTwice, 0);
            return (map, ex);
        }, (map, ex, frame) => Set(map, "_viewObject", null), 400),

        new("he calls it in, and by the time he moves you are gone", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 1);
            StandInHisWay(map);
            Invoke(map, "TheRadioCall", FirstGuard(map), PatrolBeat.Provocation.BookedTooManyTimes, 0);
            return (map, ex);
        }, (map, ex, frame) =>
        {
            if (frame == 40)
            {
                Nowhere(map);
            }
        }, 400),

        // ── THE HIDE, all three of its arms ────────────────────────────────────────────────────────
        new("you duck into a cubicle and he watched the catch turn", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(TheFloorWithCubicles, 1);
            ShutYourselfIn(map, ex, seen: true);
            return (map, ex);
        }, null, 400),

        new("you duck into a cubicle and nobody saw a thing", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(TheFloorWithCubicles, 2);
            ShutYourselfIn(map, ex, seen: false);
            return (map, ex);
        }, null, 400),

        // …and THE CASE THE ORDER IS FOR. A man who has called it in AND watched the catch turn satisfies two
        // arms at once, and which of them takes him is the whole of what a locked door is worth: he arrives,
        // and then he is a man standing outside a door. Without this case the two arms are disjoint in every
        // other case here and swapping them in the conductor would change nothing at all — a guard that could
        // not tell pass from fail about the one ordering #821 was filed to fix.
        new("he called it in, and then you shut a door in his face", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(TheFloorWithCubicles, 1);
            Invoke(map, "TheRadioCall", FirstGuard(map), PatrolBeat.Provocation.SeenAtTheHasp, 0);
            ShutYourselfIn(map, ex, seen: true);
            return (map, ex);
        }, null, 400),

        new("you duck in while he is already walking over", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(TheFloorWithCubicles, 1);
            object g = FirstGuard(map);
            ShutYourselfIn(map, ex, seen: false);

            // He hailed you from where he could not see the leaf — the walk-up is on, the door is over, and
            // the man crossing the floor has lost you.
            Invoke(map, "TheHail", g);
            return (map, ex);
        }, null, 200),

        // ── #618 · A GUN GOES OFF, AND SOMEBODY WALKS OVER TO LOOK ─────────────────────────────────
        //
        // THE FOURTEENTH, and it is a NEW case rather than a re-pin: the thirteen above are byte-identical on
        // this lane (no field was added to Guard, which is why #618's three live on the round), and this row
        // is the only place a shot is ever fired in this file. Its digest was taken on the NEW code and could
        // not have been taken anywhere else — there was nothing to walk to before it.
        //
        // The captain is NOWHERE for the whole four hundred frames, which is the whole point: this walks the
        // arm nobody could reach before, from a man leaving his round for a place, through the walk, to the
        // frame he looks at it and goes back to work — with no person in it anywhere. If the walk-up ever
        // starts reading the avatar on this road, this hash moves and the man at -9999 is the reason.
        new("a gun goes off down the corridor and somebody walks over", () =>
        {
            (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 2);
            Nowhere(map);
            FireDownHisOwnCorridor(map, ex);
            return (map, ex);
        }, null, 400),
    ];

    /// <summary>#618 · FIRE ONE, exactly the way the shipped trigger does — a <c>GunfireHeard.Shot</c>
    /// appended to the excursion's own ledger through Core's own <c>File</c>, which is the single line
    /// <c>Map.Combat.Remote.cs</c> publishes with — at a spot on the first guard's own next leg, so the place
    /// it came from is somewhere his legs can take him on a route the floor really publishes rather than a
    /// coordinate this file measured into a wall. <see cref="StandInHisWay"/>'s geometry, one errand
    /// along.</summary>
    private static void FireDownHisOwnCorridor(Pages.Map map, object ex)
    {
        object g = FirstGuard(map);
        var beat = (List<PatrolBeat.Stop>)Get(map, "_patrolBeat")!;
        PatrolBeat.Stop at = beat[(int)Get(g, "Leg")!];
        double gx = (double)Get(g, "X")!, gy = (double)Get(g, "Y")!;
        double dx = at.X - gx, dy = at.Y - gy;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.True(len > InTheWayDu, "the next stop is closer than the reach this file fires the shot at.");

        PropertyInfo shots = ex.GetType().GetProperty("ShotsHeard", Hidden)!;
        var log = (IReadOnlyList<GunfireHeard.Shot>)shots.GetValue(ex)!;
        shots.SetValue(ex, GunfireHeard.File(log, new GunfireHeard.Shot(
            "K-77", "LONG STORAGE",
            gx + (dx / len * InTheWayDu), gy + (dy / len * InTheWayDu), 100, 6)).ToList());
    }

    /// <summary>Nine hundred metres from anywhere: out of the eye, out of earshot, out of the round's
    /// business entirely. The droid filler's own off-deck coordinate.</summary>
    private static void Nowhere(Pages.Map map)
    {
        Set(map, "_avatarX", -9999.0);
        Set(map, "_avatarY", -9999.0);
    }

    /// <summary>Stand the captain in the round's way: on the line between the man and the stop he is walking
    /// to, inside his notice reach but well outside card reach. The direction is the beat's, so the spot is a
    /// place his own legs take him past rather than a coordinate this file measured — and the walk-up that
    /// follows is a WALK of several seconds, which is the whole of what #833 is about.</summary>
    private static void StandInHisWay(Pages.Map map)
    {
        object g = FirstGuard(map);
        var beat = (List<PatrolBeat.Stop>)Get(map, "_patrolBeat")!;
        PatrolBeat.Stop at = beat[(int)Get(g, "Leg")!];
        double gx = (double)Get(g, "X")!, gy = (double)Get(g, "Y")!;
        double dx = at.X - gx, dy = at.Y - gy;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        Assert.True(len > InTheWayDu, "the next stop is closer than the reach this file stands the captain at.");
        Set(map, "_avatarX", gx + (dx / len * InTheWayDu));
        Set(map, "_avatarY", gy + (dy / len * InTheWayDu));
    }

    /// <summary>How far down the corridor the captain plants himself: inside <c>NoticeDu</c> so the hail is
    /// the round's own doing, outside <c>CardReachDu</c> so the read cannot happen on frame zero — which is
    /// the exact bug #833 was filed for.</summary>
    private const double InTheWayDu = PatrolBeat.NoticeDu - 2.0;

    /// <summary>Put the captain inside a cubicle and turn the catch, with the shipping verb. Whether the man
    /// on the floor SAW it is the one bit the whole hide turns on, so it is set here by standing him at the
    /// leaf or leaving him where he was — and then read back off the guard, never assumed.</summary>
    private static void ShutYourselfIn(Pages.Map map, object ex, bool seen)
    {
        (UndergroundComplex.RingRoom _, RingOffice.Stall cell) = Cubicles(map, ex)[0];
        Set(map, "_avatarX", cell.X);
        Set(map, "_avatarY", cell.Y);

        object g = FirstGuard(map);
        if (seen)
        {
            // He is standing at the leaf when the catch turns, which is the one bit the hide is decided by.
            Set(g, "X", cell.StepX);
            Set(g, "Y", cell.StepY);
        }

        Invoke(map, "ShutTheCubicle", ex, HiveInterior.CubicleKey(
            (int)ex.GetType().GetProperty("Floor")!.GetValue(ex)!, in cell));

        if (!seen)
        {
            // …and the man who did NOT watch it turn comes round the corner AFTERWARDS, into a room with an
            // OCCUPIED plate on a door and boots he can hear through it. That is the ordinary case, and the
            // one the feature is for.
            Set(g, "X", cell.StepX);
            Set(g, "Y", cell.StepY);
        }

        Assert.Equal(seen, (bool)Get(g, "SawYouShutIt")!);
    }

    private static object FirstGuard(Pages.Map map) => Guards(map)[0];

    private static IReadOnlyList<object> Guards(Pages.Map map) =>
        [.. ((IEnumerable)Get(map, "_guards")!).Cast<object>()];
}
