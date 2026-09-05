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
    /// captain (i.e. deleting the <see cref="ReeverObservation.StillPenalty"/> arm). The bench itself then
    /// refuses to exist, which is the strongest failure this file can produce: <c>no range exists where
    /// standing still is cover and walking is not — the motion term has stopped deciding anything, and
    /// every guard in this file would pass on a rule that read no motion.</c></para>
    ///
    /// <para>…and the silence half proved RED by saying the line on the STIRRED transition instead of at
    /// the fix: <c>Assert.NotEqual() Failure — Actual: "One of them has stopped. It is looking at
    /// you."</c>, forty looks before anything had fixed.</para>
    /// </summary>
    [Fact]
    public void AStillCaptainIsNotSeenWhereAWalkingOneIs()
    {
        Pages.Map frozen = OnTheRegolith();
        object one = PutAnUnawareOneAtRange(frozen, DecidingRangeDu);
        for (int look = 0; look < 40; look++)
        {
            Set(frozen, "_pulse", PulseSlot.Empty);
            RunLooks(frozen, 1, walking: false);
            // AND NOT ONE WORD IN FORTY LOOKS. This contact is STIRRED the whole way through — a clear
            // sightline it can never make anything of — which is the one world that tells "said at the fix"
            // apart from "said when the head came up". The pose is the beat here, and it is silent.
            Assert.NotEqual(ReeverObservation.FixedOnYouLine, PulseLine(frozen));
        }
        Assert.True(Stirred(one), "the contact was never stirred, so its silence proved nothing.");
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
    /// <para><b>Proven RED</b> by dropping the <c>ex.FixedOnYouSaid</c> latch:
    /// <c>Assert.Equal() Failure — Expected: 1, Actual: 2.</c> (The "never before a fix" half is proved red
    /// in <see cref="AStillCaptainIsNotSeenWhereAWalkingOneIs"/>, which owns a world where a contact is
    /// stirred for forty looks and fixes on none of them.)</para>
    ///
    /// <para><b>An earlier version of this case was a green test that asserted nothing</b> on exactly the
    /// half it exists for: it watched the pulse for a CHANGE of text, and the second contact says the
    /// identical sentence, so a build that said it twice counted one write and passed. Hence the empty-slot
    /// bench below — a write has to be observable as a write.</para>
    /// </summary>
    [Fact]
    public void TheLineIsSaidExactlyOnceAndOnlyAtTheFix()
    {
        Pages.Map map = OnTheRegolith();
        object first = PutAnUnawareOneAtRange(map, DecidingRangeDu);
        object second = PutAnUnawareOneAtRange(map, DecidingRangeDu + 1.5);

        // The slot is EMPTIED before every look, so a WRITE is observable rather than a change of text.
        // This matters: the second contact says the identical sentence, so a guard that watched the pulse
        // for a different string would count one write and pass on a build that said it twice — the green
        // number that asserts nothing, on the exact half of the law this case exists for.
        int said = 0;
        int fixings = 0;
        bool hadFirst = false, hadSecond = false;
        for (int look = 0; look < 60; look++)
        {
            Set(map, "_pulse", PulseSlot.Empty);
            RunLooks(map, 1, walking: true);

            int newlyFixed = 0;
            if (EverSeen(first) && !hadFirst)
            {
                hadFirst = true;
                newlyFixed++;
            }
            if (EverSeen(second) && !hadSecond)
            {
                hadSecond = true;
                newlyFixed++;
            }
            fixings += newlyFixed;

            if (PulseLine(map) == ReeverObservation.FixedOnYouLine)
            {
                said++;
                Assert.True(newlyFixed > 0, "the line was said on a look when nothing newly fixed.");
            }
        }

        Assert.True(hadFirst && hadSecond,
            "only one of the two ever fixed — the 'never said twice' half of this guard is untested.");
        Assert.Equal(2, fixings);
        Assert.Equal(1, said);
    }

    /// <summary>
    /// #324 SURVIVES THE ROLL. The oldest law on this ground: <i>duck behind stone and the hunter loses your
    /// live position — the maze becomes a real instrument.</i> The latch is one-way, and it is a latch on
    /// HAVING SEEN YOU, not on knowing where you are: a contact that already has the captain must still stop
    /// updating its memory the frame stone comes between them.
    ///
    /// <para>This case exists because the lane's first draft repealed that law by accident — the rule
    /// reports Fixed for an already-fixed contact whether or not it can see anything, and the client wrote
    /// the captain's live position on the strength of the STATE rather than of the LOOK. No existing test
    /// caught it: every guard about the maze watches a contact that has not yet seen you.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>!clearLine</c> half of that gate:
    /// <c>Assert.Equal() Failure — the hunter learned where the captain moved to through a slab.</c></para>
    /// </summary>
    [Fact]
    public void AFixedOneStillLosesYouBehindStone()
    {
        Pages.Map map = OnTheRegolith();
        object one = PutAnUnawareOneAtRange(map, DecidingRangeDu);
        one.GetType().GetField("EverSeen", Hidden)!.SetValue(one, true);   // it already has him

        // In the open: it tracks him, which is the behaviour that must SURVIVE.
        RunLooks(map, 1, walking: true);
        Assert.Equal((double)Get(map, "_avatarX")!, LastSeenX(one), 3);

        // Now stone. Whatever he does after this, its memory is of where he WAS.
        (double hx, double hy) = BehindStoneFrom(map);
        foreach (string where in new[] { "X", "AnchorX" })
        {
            one.GetType().GetField(where, Hidden)!.SetValue(one, hx);
        }
        foreach (string where in new[] { "Y", "AnchorY" })
        {
            one.GetType().GetField(where, Hidden)!.SetValue(one, hy);
        }
        double remembered = LastSeenX(one);

        Set(map, "_avatarX", (double)Get(map, "_avatarX")! + 6.0);
        RunLooks(map, 4, walking: true);

        Assert.Equal(remembered, LastSeenX(one), 3);
        Assert.NotEqual((double)Get(map, "_avatarX")!, LastSeenX(one), 3);
    }

    private static double LastSeenX(object r) =>
        (double)r.GetType().GetField("LastSeenX", Hidden)!.GetValue(r)!;

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
