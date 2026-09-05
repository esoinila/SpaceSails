using System;
using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #436 · <b>THE OBSERVATION ROLL, ON THE SHIPPING PAGE.</b>
///
/// <para>Core pins the rule; only the page knows what it is HANDED. These drive real frames of the real
/// surface tick with real Old Ones on a real deck, and ask the two questions the rule's own tests cannot:
/// does the world the odds read actually reach them (a captain who stands still is genuinely covered where a
/// captain who walks is not, at the same spot, against the same contact), and does the one authored sentence
/// arrive at the moment of the fix and at no other moment.</para>
///
/// <para>The 2026-08-02 story-QA finding is the whole reason the second one is a law: <i>"A player cannot
/// tell the frame it happened from the frame before it."</i> A guard that only checked the line EXISTS would
/// pass on a build that said it on the landing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheMomentItLooksAtYouTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private const string Body = "luna";

    // Far enough that a captain standing still is not seen at all and a captain walking is — the one range
    // band where the motion term decides the outcome by itself. Read off Core rather than typed, so a
    // re-tuning of the odds moves this bench instead of silently making it prove nothing.
    private static readonly double DecidingRangeDu = FindTheRangeWhereMotionDecides();

    private static double FindTheRangeWhereMotionDecides()
    {
        for (double r = 1.0; r <= ReeverObservation.LongLookDu; r += 0.25)
        {
            bool stillIsSafe = ReeverObservation.ChanceIn20(
                new ReeverObservation.View(r, 0.0, ReeverObservation.Doing.Nothing)) == 0;
            bool walkingIsNot = ReeverObservation.ChanceIn20(
                new ReeverObservation.View(r, SuitAir.WalkSpeedDu, ReeverObservation.Doing.Nothing)) > 0;
            if (stillIsSafe && walkingIsNot)
            {
                return r;
            }
        }
        throw new InvalidOperationException(
            "no range exists where standing still is cover and walking is not — the motion term has stopped "
            + "deciding anything, and every guard in this file would pass on a rule that read no motion.");
    }

    /// <summary>
    /// STANDING STILL IS COVER, AND WALKING IS NOT — the same contact, the same spot, the same sightline, and
    /// only the captain's own feet different. The owner's motion-only law pointed the other way: you hide
    /// from them the way they hide from you.
    ///
    /// <para><b>Proven RED</b> by returning 0 from <c>ReeverObservation.MotionModifier</c> for a still
    /// captain (i.e. deleting the <see cref="ReeverObservation.StillPenalty"/> arm): the frozen captain is
    /// then fixed on inside the first few looks and the first assertion fails —
    /// <c>a captain who never moved a boot was fixed on after 40 looks.</c></para>
    /// </summary>
    [Fact]
    public void AStillCaptainIsNotSeenWhereAWalkingOneIs()
    {
        Pages.Map frozen = OnTheRegolith();
        object one = PutAnUnawareOneAtRange(frozen, DecidingRangeDu);
        RunLooks(frozen, 40, walking: false);
        Assert.False(EverSeen(one),
            $"a captain who never moved a boot was fixed on after 40 looks at {DecidingRangeDu:F2} du.");

        Pages.Map walking = OnTheRegolith();
        object other = PutAnUnawareOneAtRange(walking, DecidingRangeDu);
        RunLooks(walking, 40, walking: true);
        Assert.True(EverSeen(other),
            $"a captain walking in the open at {DecidingRangeDu:F2} du was never once seen in 40 looks — "
            + "either the motion the page measures is not reaching the odds, or the odds are not reaching "
            + "the latch.");
    }

    /// <summary>
    /// AND THE HEAD COMES UP FIRST. A contact with a clear sightline that has not yet fixed is STIRRED on the
    /// very first frame — the pose change that is the whole of the drawn beat — and it does NOT latch:
    /// take the sightline away and the head goes back down, which is the fear window this issue is about.
    ///
    /// <para><b>Proven RED</b> by having <c>TakeALook</c> set <c>r.Stirred</c> only on a successful roll:
    /// <c>Assert.True() Failure — the head never came up.</c></para>
    /// </summary>
    [Fact]
    public void TheHeadComesUpOnSight_AndGoesBackDownWhenStoneReturns()
    {
        Pages.Map map = OnTheRegolith();
        object one = PutAnUnawareOneAtRange(map, DecidingRangeDu);

        RunLooks(map, 1, walking: false);
        Assert.True(Stirred(one), "the head never came up on a clear sightline.");
        Assert.False(EverSeen(one), "one frame of standing still fixed it, which is not a roll at all.");

        // Now put real stone between them — a spot on this ground the captain genuinely cannot see, found
        // by asking the deck rather than by assuming a far one is a hidden one (it is not: an open field
        // has a clear line all the way to the rim, which is exactly why the maze is play).
        // Its ANCHOR moves with it: a still contact's drawn position is re-derived from the anchor by the
        // idle shiver every frame, so a body moved without its anchor is dragged straight back into view on
        // the next tick and the case would test nothing but the shiver.
        (double hx, double hy) = BehindStoneFrom(map);
        foreach (string where in new[] { "X", "AnchorX" })
        {
            one.GetType().GetField(where, Hidden)!.SetValue(one, hx);
        }
        foreach (string where in new[] { "Y", "AnchorY" })
        {
            one.GetType().GetField(where, Hidden)!.SetValue(one, hy);
        }
        RunLooks(map, 1, walking: false);
        Assert.False(Stirred(one), "the head stayed up with stone standing between them.");
    }

    /// <summary>A spot on this ground the captain has no line to, found by sweeping the deck's own collision
    /// field. Asserted to exist, because a bench that silently fell back to an OPEN spot would prove the
    /// opposite of what it claims.</summary>
    private static (double X, double Y) BehindStoneFrom(Pages.Map map)
    {
        double ax = (double)Get(map, "_avatarX")!, ay = (double)Get(map, "_avatarY")!;
        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        for (double range = 4.0; range <= 60.0; range += 0.5)
        {
            for (int step = 0; step < 72; step++)
            {
                double a = step * Math.PI / 36.0;
                double x = ax + (range * Math.Cos(a)), y = ay + (range * Math.Sin(a));
                if (!SurfaceCollision.HasLineOfSight(x, y, ax, ay, deck.CollisionField))
                {
                    return (x, y);
                }
            }
        }
        throw new InvalidOperationException(
            "there is no spot within sixty deck units of the captain that stone hides on this ground — "
            + "the maze has stopped being a maze, and the whole fear window rests on it.");
    }

    /// <summary>
    /// THE ONE AUTHORED SENTENCE ARRIVES AT THE FIX, AND AT NO OTHER MOMENT. Checked on every frame of a
    /// whole approach: while nothing has fixed, the line is not on the pulse; the frame the first contact
    /// fixes, it is; and a second contact fixing later never says it again.
    ///
    /// <para><b>Proven RED</b>, twice. Saying it on the <c>Stirred</c> transition instead of the fix:
    /// <c>the line was said on a frame when nothing had fixed yet.</c> And dropping the
    /// <c>ex.FixedOnYouSaid</c> latch: <c>the line was said 2 times.</c></para>
    /// </summary>
    [Fact]
    public void TheLineIsSaidExactlyOnceAndOnlyAtTheFix()
    {
        Pages.Map map = OnTheRegolith();
        object first = PutAnUnawareOneAtRange(map, DecidingRangeDu);
        object second = PutAnUnawareOneAtRange(map, DecidingRangeDu + 1.5);

        int said = 0;
        bool anyFixedBefore = false;
        for (int look = 0; look < 60; look++)
        {
            string? before = PulseLine(map);
            RunLooks(map, 1, walking: true);
            string? after = PulseLine(map);

            bool anyFixedNow = EverSeen(first) || EverSeen(second);
            if (after == ReeverObservation.FixedOnYouLine && before != after)
            {
                said++;
                Assert.True(anyFixedNow, "the line was said on a frame when nothing had fixed yet.");
            }
            if (!anyFixedNow)
            {
                Assert.NotEqual(ReeverObservation.FixedOnYouLine, after);
            }
            anyFixedBefore |= anyFixedNow;
        }

        Assert.True(anyFixedBefore, "nothing ever fixed, so this case proved nothing about the line.");
        Assert.True(EverSeen(first) && EverSeen(second),
            "only one of the two ever fixed — the 'never said twice' half of this guard is untested.");
        Assert.Equal(1, said);
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A captain standing out on the open regolith of a real landing site, past the arrival grace,
    /// with the surface clock running.</summary>
    private static Pages.Map OnTheRegolith()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        exType.GetProperty("LandedAtMs")!.SetValue(ex, 0.0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        // Well past SurfaceArrival.SpotGraceSeconds: the ground is live, which is the posture every one of
        // these cases is about. A bench inside the grace would prove that nothing sees you, for the wrong
        // reason, and pass.
        Set(map, "_lastTimestampMs", (double?)(SurfaceArrival.SpotGraceSeconds * 1000.0 * 3));
        Set(map, "_avatarX", (double)MoonSurface.SpawnX);
        Set(map, "_avatarY", MoonSurface.SpawnY);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Put one awake, unaware Old One on the ground at <paramref name="rangeDu"/> from the captain,
    /// with the sightline between them ASSERTED clear — a case whose geometry quietly did not cooperate
    /// would pass every "it was not seen" assertion in this file for a reason that is not the rule.</summary>
    private static object PutAnUnawareOneAtRange(Pages.Map map, double rangeDu)
    {
        double ax = (double)Get(map, "_avatarX")!, ay = (double)Get(map, "_avatarY")!;
        double x = ax + rangeDu, y = ay;

        var deck = (DeckPlan)Get(map, "_deckPlan")!;
        Assert.True(SurfaceCollision.HasLineOfSight(x, y, ax, ay, deck.CollisionField),
            $"stone stands between the captain and {rangeDu:F2} du to starboard on this ground — this bench "
            + "would prove nothing about a roll that never gets permission to happen.");

        Type reever = typeof(Pages.Map).GetNestedType("Reever", Hidden | BindingFlags.Public)!;
        object one = Activator.CreateInstance(reever, nonPublic: true)!;
        reever.GetField("X", Hidden)!.SetValue(one, x);
        reever.GetField("Y", Hidden)!.SetValue(one, y);
        // Its own stable seed, the way the tide gives one out — without it every contact on the bench would
        // share a stream and "two of them" would be one of them twice.
        reever.GetField("JitterSeed", Hidden)!.SetValue(one,
            (ulong)(0xD1B54A32D192ED03UL * (ulong)(((IList)Get(map, "_reevers")!).Count + 1)));
        ((IList)Get(map, "_reevers")!).Add(one);
        return one;
    }

    /// <summary>Run whole LOOKS of surface frames. A captain who is "walking" is stepped back and forth by a
    /// full walk's worth of travel each frame, so the speed the page measures is a real walk while the RANGE
    /// the odds read stays where the bench put it — the one variable under test moves and nothing else
    /// does.</summary>
    private static void RunLooks(Pages.Map map, int looks, bool walking)
    {
        const double dt = 1.0 / 60.0;
        double step = SuitAir.WalkSpeedDu * dt;
        int frames = (int)Math.Ceiling(looks * ReeverObservation.LookIntervalSeconds / dt);
        for (int i = 0; i < frames; i++)
        {
            if (walking)
            {
                Set(map, "_avatarX", (double)Get(map, "_avatarX")! + (i % 2 == 0 ? step : -step));
            }
            Set(map, "_lastTimestampMs", (double?)((double)(Get(map, "_lastTimestampMs") as double? ?? 0) + (dt * 1000.0)));
            Invoke(map, "MeasureTheCaptainsMotion", dt);
            Invoke(map, "StepReevers", dt);
        }
    }

    private static bool EverSeen(object r) => (bool)r.GetType().GetField("EverSeen", Hidden)!.GetValue(r)!;

    private static bool Stirred(object r) => (bool)r.GetType().GetField("Stirred", Hidden)!.GetValue(r)!;

    private static string? PulseLine(Pages.Map map) => ((PulseSlot)Get(map, "_pulse")!).Message;

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);
}
