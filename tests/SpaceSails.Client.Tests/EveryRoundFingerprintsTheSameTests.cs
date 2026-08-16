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
/// no chance of pinning what the new code happens to do.</para>
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
public sealed class EveryRoundFingerprintsTheSameTests
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

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
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
        Set(map, "_fpMode", false);
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
    ];

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

    // ── THE PINS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What each case's transcript hashed to on the 175-line <c>AdvancePatrol</c> of
    /// <c>d064085</c>, before a line of it was moved. A change here is either a behaviour change in the round
    /// or a change to the floor the round is walked on; both are things a human should have to look at.</summary>
    private static readonly Dictionary<string, string> Pinned = new(StringComparer.Ordinal)
    {
        ["an empty floor still fades the plate"] = "148ff6f3378feede544d018d3771364edfe52e86ecdf3b316d39696757114ab3",
        ["up on the surface there is no round at all"] = "343943d96209111af869ab6dbe8a2fb427427d89b9ca1eb09e9de0fa1934d8ae",
        ["two men walk the round and nobody is watching"] = "07cd8ad1a5538d1e77e4c603c96aeadb8d5c25f4b180d9cb7f4c2de9ffafc34d",
        ["one man walks the round, all the way round it"] = "11c4a45bcf8bc5264099ba24343ce57433152cba227fd2714cd37ce05af3dc43",
        ["he hails you and you walk away from it"] = "11047c537b2e1ecba26719c794e327c7d3ad52e311cf78d257c894777b0ae9e4",
        ["he crosses the floor, reads your papers, and walks you to the car"] = "e28da120432d3a69b47871db86e9e6b90769ea195a72ad04b7f4ef3bfa186708",
        ["…and this time he does not press the button for your floor"] = "a5e4b2a86ccc31b32346959aedd538af55b9b05a38cf2afbf20bf2448a8d4140",
        ["he calls it in, comes at a run, and he has you"] = "6b5da25dd0810d31fb57a276925f43b3292795059265c08f33456636a64f583d",
        ["he calls it in, and by the time he moves you are gone"] = "23bd65d4552be534fcbfc6b59b8ecdc2a4d3209a700982b2ba0869f217aa01e1",
        ["you duck into a cubicle and he watched the catch turn"] = "79fa953a17e87de5e336ddb9fd76e1fa4e6618ba8c5520bc9a220dde16ec9fc1",
        ["you duck into a cubicle and nobody saw a thing"] = "4599ddbf4a51a9bf29c26029e818d4ca861f46c3bc933f92da882459b82dbe8e",
        ["he called it in, and then you shut a door in his face"] = "23cb583bbfd91ae873af33f5eef7fef73a71a3615086d87d791a5d4ff6ca5890",
        ["you duck in while he is already walking over"] = "653e6acf180cdf9cebe314f4cd9da594faccac2c588998fb93518b60836a9ab0",
    };



    // ── THE FACTS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>EVERY ROUND FINGERPRINTS THE SAME. The whole of the guard: drive each case through the fixed
    /// dt sequence and hash what the floor did.</summary>
    [Fact]
    public void EveryCaseHashesToWhatItHashedToOnTheOldCode()
    {
        var wrong = new List<string>();
        foreach (Case c in EveryCase())
        {
            (string text, int frames, _) = Walk(c);
            string got = Sha256(text);
            if (!Pinned.TryGetValue(c.Name, out string? want) || !string.Equals(want, got, StringComparison.Ordinal))
            {
                wrong.Add($"  {c.Name} — {frames} frame(s), sha256 {got}\n      pinned {want ?? "(nothing)"}");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} case(s) walk a different round than the one pinned on the old code:\n"
            + string.Join("\n", wrong));
    }

    /// <summary>…and it hashes the same TWICE, in one process, from two fresh floors. The clause that catches
    /// a plan built off an unordered set, a cache with memory in it, or a round that reads a clock.</summary>
    [Fact]
    public void TheSameCaseWalkedTwiceIsTheSameRound()
    {
        foreach (Case c in EveryCase())
        {
            Assert.Equal(Sha256(Walk(c).Text), Sha256(Walk(c).Text));
        }
    }

    /// <summary>THE CASE SET ENTERS EVERY ARM IT CAN. Six of the seven arms of the chain, each named, each
    /// entered by at least one case — otherwise the pins above would be a guard that cannot tell pass from
    /// fail on the arms nobody walked.
    ///
    /// <para>The census is taken by this file, from the page's own private answers (the escort, the hide, the
    /// hold law) and a replica of the chain's own tests. It is deliberately not used for anything but
    /// coverage: it is one frame behind on the arms armed inside a frame, which does not matter for counting
    /// and would matter for pinning.</para></summary>
    [Fact]
    public void EveryArmOfTheChainIsWalkedBySomeCase()
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Case c in EveryCase())
        {
            foreach ((string arm, int n) in Walk(c).Arms)
            {
                seen[arm] = seen.TryGetValue(arm, out int had) ? had + n : n;
            }
        }

        foreach (string arm in new[]
        {
            "the escort", "the knock at the door", "the run", "lost to a door",
            "the walk-up", "the round's own leg",
        })
        {
            Assert.True(seen.GetValueOrDefault(arm) > 0,
                $"no case in this file ever takes the `{arm}` arm — the pins say nothing about it.\n"
                + "walked: " + string.Join(", ", seen.Select(kv => $"{kv.Key}×{kv.Value}")));
        }

        // …and the one that is not in that list is not in it for a reason, which the next fact states.
        Assert.Equal(0, seen.GetValueOrDefault("the cover act"));
    }

    /// <summary>#793 · THE HOLD ARM CANNOT BE ENTERED FROM THIS METHOD'S OWN INPUTS, and that is the shipped
    /// law rather than a gap in the case set: a guard is always <c>OnAPublishedRound</c>, a published round
    /// can never be <c>IsTailing</c>, so <c>MustHold</c> is false for every guard on every floor however the
    /// captain sits. The day something down here does tail the captain, this fact goes red and the case set
    /// owes the cover act a case.</summary>
    [Fact]
    public void TheHoldArmIsUnreachableAndTheGuardSaysSo()
    {
        for (int i = 0; i < PatrolBeat.MostOnAFloor; i++)
        {
            FootTail.Mover afoot = PatrolBeat.OnTheRound(i, 10.0, 10.0);
            Assert.True(afoot.OnAPublishedRound, "a guard is no longer stamped as a published round.");
            Assert.False(FootTail.IsTailing(in afoot));
            Assert.False(FootTail.MustHold(true, 10.2, 10.2, in afoot, null),
                "a guard CAN be held now — the cover-act arm is reachable and this file owes it a case.");
        }
    }

    // ── WALKING ONE CASE ──────────────────────────────────────────────────────────────────────────────

    private static (string Text, int Frames, IReadOnlyDictionary<string, int> Arms) Walk(Case c)
    {
        (Pages.Map map, object ex) = c.Stage();
        var arms = new Dictionary<string, int>(StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.Append("case ").Append(c.Name).Append('\n');

        MethodInfo step = typeof(Pages.Map).GetMethod("AdvancePatrol", Hidden)
            ?? throw new InvalidOperationException("the page has no AdvancePatrol — this guard is dead.");

        Dictionary<string, string> before = EveryScalarOnThePage(map);

        for (int frame = 0; frame < c.Frames; frame++)
        {
            c.EachFrame?.Invoke(map, ex, frame);
            Census(map, ex, arms);

            double dt = Dts[frame % Dts.Length];
            step.Invoke(map, [dt]);

            sb.Append("f").Append(frame).Append(" dt=").Append(R(dt)).Append('\n');
            sb.Append(TheFloorAfterThatFrame(map, ex));
        }

        sb.Append("── what this case moved on the page ──\n")
          .Append(WhatMoved(before, EveryScalarOnThePage(map)));
        return (Normalize(sb.ToString()), c.Frames, arms);
    }

    /// <summary>Which arm each guard would take on the frame about to be spent. A replica of the chain's own
    /// tests over the page's own private answers — for the coverage fact and for nothing else.</summary>
    private static void Census(Pages.Map map, object ex, Dictionary<string, int> arms)
    {
        var guards = Guards(map);
        if (guards.Count == 0
            || (int)ex.GetType().GetProperty("Floor")!.GetValue(ex)! >= 0)
        {
            return;
        }

        object? escort = Get(map, "_escort");
        object? hide = Invoke(map, "TheCubicleTheCaptainIsShutIn", ex);
        bool sitting = (bool)Get(map, "SeatedOnABenchInTheOpen")!;
        var sight = (IReadOnlyList<SurfaceCollision.Segment>)Invoke(map, "SightBlockers")!;
        double ax = (double)Get(map, "_avatarX")!, ay = (double)Get(map, "_avatarY")!;

        for (int i = 0; i < guards.Count; i++)
        {
            object g = guards[i];
            FootTail.Mover afoot = PatrolBeat.OnTheRound(
                i, (double)Get(g, "X")!, (double)Get(g, "Y")!);
            bool held = FootTail.MustHold(sitting, ax, ay, in afoot, sight);

            string arm =
                ReferenceEquals(g, escort) ? "the escort"
                : hide is not null && CubicleLock.WaitsAtTheDoor((bool)Get(g, "SawYouShutIt")!)
                    ? "the knock at the door"
                : (bool)Get(g, "AfterYou")! ? "the run"
                : hide is not null && (bool)Get(g, "WalkingUp")! ? "lost to a door"
                : held ? "the cover act"
                : (bool)Get(g, "WalkingUp")! ? "the walk-up"
                : "the round's own leg";
            arms[arm] = arms.GetValueOrDefault(arm) + 1;
        }
    }

    // ── WHAT A FRAME LEAVES BEHIND ────────────────────────────────────────────────────────────────────

    private static string TheFloorAfterThatFrame(Pages.Map map, object ex)
    {
        var sb = new StringBuilder();
        var guards = Guards(map);
        sb.Append("  captain ").Append(R(Get(map, "_avatarX"))).Append(' ')
          .Append(R(Get(map, "_avatarY"))).Append('\n');
        sb.Append("  guards ").Append(guards.Count).Append('\n');

        for (int i = 0; i < guards.Count; i++)
        {
            object g = guards[i];
            sb.Append("  [").Append(i).Append(']');
            foreach (MemberInfo m in GuardMembers(g.GetType()))
            {
                sb.Append(' ').Append(m.Name).Append('=').Append(R(ValueOf(g, m)));
            }
            sb.Append('\n');
        }

        object? escort = Get(map, "_escort");
        object? due = Get(map, "_escortDue");
        sb.Append("  escort=").Append(IndexOf(guards, escort))
          .Append(" due=").Append(IndexOf(guards, due))
          .Append(" car=").Append(R(Get(map, "_escortCar")))
          .Append(" seconds=").Append(R(Get(map, "_escortSeconds")))
          .Append(" pumps=").Append(R(Get(map, "_escortSaidPumps")))
          .Append('\n');
        sb.Append("  kickOut=").Append(R(Get(map, "_kickOutDue")))
          .Append(" ride=").Append(R(Get(map, "_kickOutRideDue")))
          .Append(" plate=").Append(R(Get(map, "_kickedOutPlateFor")))
          .Append(" escorts=").Append(R(Get(map, "_escortsThisWatch")))
          .Append(" walkedAway=").Append(R(Get(map, "_walkedAwayThisWatch")))
          .Append('\n');
        sb.Append("  floorSeconds=").Append(R(Get(map, "_patrolFloorSeconds")))
          .Append(" heardAgo=").Append(R(Get(map, "_patrolHeardAgo")))
          .Append(" fan=").Append(R(Get(map, "_walletFanOpen")))
          .Append(" paper=").Append(R(Get(map, "_paperInHand")))
          .Append(" walkedPast=").Append(R(Get(map, "_walkedPastSaid")))
          .Append(" shownBook=").Append(((ICollection)Get(map, "_shownBook")!).Count)
          .Append('\n');
        sb.Append("  card=").Append(R(Get(map, "_viewObject"))).Append('\n');
        sb.Append("  pulse=").Append(R(Get(map, "_pulse"))).Append('\n');

        var events = (IEnumerable)Get(map, "_autopilotEvents")!;
        foreach (object row in events)
        {
            sb.Append("  log ").Append(R(row)).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Every scalar on the whole page, by name — read once before a case and once after.</summary>
    private static Dictionary<string, string> EveryScalarOnThePage(Pages.Map map)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (FieldInfo f in typeof(Pages.Map)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            object? v = f.GetValue(map);
            if (v is null)
            {
                seen[f.Name] = "-";
            }
            else if (v is bool or int or long or double or float or string or Enum or decimal)
            {
                seen[f.Name] = R(v);
            }
            else if (v is ICollection col)
            {
                seen[f.Name + ".Count"] = col.Count.ToString(Inv);
            }
        }
        return seen;
    }

    /// <summary>
    /// WHAT THIS CASE MOVED ON THE PAGE — the whole scalar surface of <see cref="Pages.Map"/>, before against
    /// after, with only the differences written down.
    ///
    /// <para>The DIFFERENCE rather than the census is the point. A census of every field would catch the same
    /// side effects, and it would also change every hash in this file the day a lane somewhere else on the
    /// page adds, renames or retires a field the round has never heard of — which is what happened the first
    /// time this guard met CI, on the morning #870's seat lane landed. A field this method never writes
    /// contributes nothing here whether it exists or not; a field it DOES write is named, with both values,
    /// and moves the hash.</para>
    /// </summary>
    private static string WhatMoved(
        IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after)
    {
        var sb = new StringBuilder();
        foreach (string name in before.Keys.Concat(after.Keys).Distinct().OrderBy(n => n, StringComparer.Ordinal))
        {
            string was = before.GetValueOrDefault(name, "(not there)");
            string now = after.GetValueOrDefault(name, "(not there)");
            if (!string.Equals(was, now, StringComparison.Ordinal))
            {
                sb.Append("  ").Append(name).Append(": ").Append(was).Append(" → ").Append(now).Append('\n');
            }
        }
        return sb.ToString();
    }

    private static IEnumerable<MemberInfo> GuardMembers(Type guard) =>
        guard.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.Name.Contains('<', StringComparison.Ordinal))
            .Cast<MemberInfo>()
            .Concat(guard.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetIndexParameters().Length == 0))
            .OrderBy(m => m.Name, StringComparer.Ordinal);

    private static object? ValueOf(object o, MemberInfo m) =>
        m is FieldInfo f ? f.GetValue(o) : ((PropertyInfo)m).GetValue(o);

    private static string IndexOf(IReadOnlyList<object> guards, object? one)
    {
        if (one is null)
        {
            return "-";
        }
        for (int i = 0; i < guards.Count; i++)
        {
            if (ReferenceEquals(guards[i], one))
            {
                return i.ToString(Inv);
            }
        }
        return "gone";
    }

    // ── WRITING A VALUE DOWN ──────────────────────────────────────────────────────────────────────────

    private static string R(object? v) => v switch
    {
        null => "-",
        double d => d.ToString("R", Inv),
        float fl => fl.ToString("R", Inv),
        bool b => b ? "yes" : "no",
        AutoWalk route =>
            $"route[{route.Route.Count} pts, cursor {R(Get(route, "_cursor"))}, "
            + $"active {R(route.Active)}, arrived {R(route.Arrived)}, cancelled {R(route.Cancelled)}, "
            + $"snagged {R(Get(route, "_snagged"))}, "
            + $"{string.Join(" ", route.Route.Select(p => p.ToString()))}]",
        AutoWalk.Planner plan => $"plan[{plan.From} → {plan.To}, done {R(plan.Done)}]",
        IFormattable other => other.ToString(null, Inv),
        _ => v.ToString() ?? "-",
    };

    /// <summary>The one normalisation, and the reason is in the class docblock: six decimals, so a platform's
    /// libm last bit cannot redden a guard about a code move.</summary>
    private static readonly Regex Numbers = new(
        @"-?\d+\.\d+(?:[eE][-+]?\d+)?|-?\d+[eE][-+]?\d+", RegexOptions.Compiled);

    private static string Normalize(string raw) => Numbers.Replace(raw, m =>
    {
        double v = double.Parse(m.Value, NumberStyles.Float, Inv);
        double r = Math.Round(v, 6, MidpointRounding.AwayFromZero);
        return (r == 0 ? 0 : r).ToString("0.######", Inv);
    });

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static object? Get(object o, string name)
    {
        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"`{name}` is not on {o.GetType().Name} — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the page has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }
}
