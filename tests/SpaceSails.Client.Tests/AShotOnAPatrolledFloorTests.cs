using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #618 · <b>A SHOT IS HEARD — the floor half, driven on a live page.</b>
///
/// <para>Owner's ruling, 2026-08-05: <i>"they come if we make a big noise like start to use the special ammo
/// to open a locked door."</i> The rules are pinned one project along (<c>AShotIsHeardTests</c>); what is
/// here is the thing itself — a real <see cref="Pages.Map"/> on a real generated Hive floor with a real round
/// on it, a shot published into the excursion's own ledger exactly the way <c>Map.Combat.Remote.cs</c>
/// publishes one, and then <c>AdvancePatrol</c> called a frame at a time until somebody walks.</para>
///
/// <para><b>The bench is #906's</b>, deliberately and not by copy-paste convenience: it is the one harness in
/// the repository that drives the shipped round rather than a replica of it, and this feature's whole risk is
/// that a rule which reads correctly never reaches a man's legs. It is reproduced rather than shared because
/// <c>EveryRoundFingerprintsTheSameTests</c>'s bench is private to a file whose subject is a set of pinned
/// digests; the two agree line for line on how a floor is stood up, and the fourteenth case in THAT file is
/// the same shot in the same place, hashed.</para>
///
/// <h3>Proven able to fail, each by breaking the shipped code and watching it go red</h3>
///
/// <list type="bullet">
/// <item>Not publishing (never filing the shot) reddens <see cref="TheNearestManWalksToWhereItCameFrom"/>.</item>
/// <item>A floor-wide ear (dropping <c>WithinEarshot</c> from <c>GunfireHeard.NearestEar</c>) reddens
/// <see cref="AShotNobodyCouldHearBuysNothingAtAll"/>.</item>
/// <item>Banking against the BODY instead of the operator, and banking every frame instead of once, both
/// redden <see cref="TheCrossingIsBankedOnceAtWeightThreeToTheOutfit"/>.</item>
/// <item>Saying anything at all on either end of the walk reddens
/// <see cref="NothingIsSaidAboutItOnEitherEndOfTheWalk"/>.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AShotOnAPatrolledFloorTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    /// <summary>The watch every case is walked in — fixed, because the beat, the plates and the head count are
    /// all functions of it. #906's own number, so the two files stand a floor up identically.</summary>
    private const long Watch = 7;

    /// <summary>A live rAF frame.</summary>
    private const double Dt = 1.0 / 60.0;

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

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
        Nowhere(map);
        return (map, ex);
    }

    private static int ThePatrolledFloor => Enumerable.Range(1, 14)
        .Select(i => -i)
        .First(level => PatrolBeat.IsPatrolled(Body, level));

    /// <summary>…and one with nobody on it at all, which is where a shot buys nothing because there is no
    /// register to cover it. Asked of Core rather than typed in.</summary>
    private static int AnUnpatrolledFloor => Enumerable.Range(1, 14)
        .Select(i => -i)
        .First(level => !PatrolBeat.IsPatrolled(Body, level));

    /// <summary>Nine hundred metres from anywhere: out of the eye, out of earshot, out of the round's
    /// business. The captain is never in the picture in this file — the whole point of the feature is that
    /// the round comes for the NOISE, and a captain standing in shot would buy a hail and prove nothing.
    /// </summary>
    private static void Nowhere(Pages.Map map)
    {
        Set(map, "_avatarX", -9999.0);
        Set(map, "_avatarY", -9999.0);
    }

    /// <summary>
    /// FIRE ONE, exactly the way the shipped trigger does: a <c>GunfireHeard.Shot</c> appended to the
    /// excursion's own ledger through Core's own <c>File</c> (<c>Map.Combat.Remote.cs</c>'s one line). Nothing
    /// else about pressing SAY WHEN is simulated, because nothing else about it is what the round reads —
    /// which is the claim this file is here to make good.
    /// </summary>
    private static void FireAt(object ex, double x, double y)
    {
        PropertyInfo shots = ex.GetType().GetProperty("ShotsHeard", Hidden)!;
        var log = (IReadOnlyList<GunfireHeard.Shot>)shots.GetValue(ex)!;
        shots.SetValue(ex, GunfireHeard.File(log, new GunfireHeard.Shot(
            "K-77", "LONG STORAGE", x, y, 100, 6)).ToList());
    }

    /// <summary>A spot on the first guard's own next leg, <paramref name="du"/> along it — so the place the
    /// shot came from is somewhere his own legs could take him, on a route the floor really publishes, rather
    /// than a coordinate this file measured into a wall. #906's <c>StandInHisWay</c>, one errand along.</summary>
    private static (double X, double Y) DownHisOwnCorridor(Pages.Map map, double du)
    {
        object g = FirstGuard(map);
        var beat = (List<PatrolBeat.Stop>)Get(map, "_patrolBeat")!;
        PatrolBeat.Stop at = beat[(int)Get(g, "Leg")!];
        double gx = (double)Get(g, "X")!, gy = (double)Get(g, "Y")!;
        double dx = at.X - gx, dy = at.Y - gy;
        double len = Math.Sqrt((dx * dx) + (dy * dy));

        Assert.True(len > 1.0, "the first guard's next stop is on top of him; this bench has nothing to walk.");
        double along = Math.Min(du, len - 0.5);
        return (gx + (dx / len * along), gy + (dy / len * along));
    }

    private static object FirstGuard(Pages.Map map) => Guards(map)[0];

    private static IReadOnlyList<object> Guards(Pages.Map map) =>
        [.. ((IEnumerable)Get(map, "_guards")!).Cast<object>()];

    private static double DistanceFrom(object g, (double X, double Y) to)
    {
        double dx = (double)Get(g, "X")! - to.X, dy = (double)Get(g, "Y")! - to.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static void Frames(Pages.Map map, int n)
    {
        MethodInfo step = typeof(Pages.Map).GetMethod("AdvancePatrol", Hidden)
            ?? throw new InvalidOperationException("the page has no AdvancePatrol — this guard is dead.");
        for (int i = 0; i < n; i++)
        {
            step.Invoke(map, [Dt]);
        }
    }

    private static int HeatHere(Pages.Map map) =>
        IllegalHeat.HeatAtSite((ContactLedger)Get(map, "_contacts")!, Body);

    private static int Walking(Pages.Map map) =>
        Guards(map).Count(g => (bool)Get(g, "WalkingUp")!);

    // ── (a) A SHOT WITHIN RANGE ON A PATROLLED FLOOR ──────────────────────────────────────────────────

    /// <summary>
    /// <b>THE NEAREST MAN WALKS TO WHERE IT CAME FROM</b>, and he does it off nothing but the shot being
    /// filed — no new press, no new call in <c>FireOnTheLock</c>, no host member.
    ///
    /// <para>Three things are asserted and the third is the one that matters. He is <c>WalkingUp</c> within a
    /// frame; the conductor really walks him through that arm (<c>Guard.Doing.WalkingUp</c>, asked of the
    /// shipped type rather than of this file's opinion); and <b>the distance from him to the place the shot
    /// came from goes DOWN</b> — because a man who was set walking and then walked somewhere else is exactly
    /// the sim-and-the-sentence disagreement this repo has paid for three times in one afternoon.</para>
    ///
    /// <para>He is also the only one who goes. A second man leaving his round would be the floor-wide hunt
    /// #835 deliberately did not build, and it is asserted on a floor rostered with two.</para>
    ///
    /// <para><b>RED by not publishing</b> — comment out the <c>FireAt</c> below and the round never hears
    /// anything: <i>"nobody left his round in 240 frames."</i></para>
    /// </summary>
    [Fact]
    public void TheNearestManWalksToWhereItCameFrom()
    {
        (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 2);

        Assert.Equal(0, Walking(map));
        (double X, double Y) noise = DownHisOwnCorridor(map, 12.0);
        object him = FirstGuard(map);
        double was = DistanceFrom(him, noise);

        FireAt(ex, noise.X, noise.Y);
        Frames(map, 1);

        Assert.True(
            (bool)Get(him, "WalkingUp")!,
            $"the man {was:0.#} du from a gun going off did not leave his round. Walking: {Walking(map)}.");

        // …and the CONDUCTOR agrees, asked of the shipped type's own answer rather than of this file's
        // opinion: he is walked through the WALK-UP arm and not through some eighth thing this feature
        // invented. (Not the escort, no cubicle — which is what the two bits are.)
        Assert.Equal("WalkingUp", TheArmHeIsIn(him));

        // …and nobody else moved. One bang, one errand.
        Assert.Equal(1, Walking(map));

        Frames(map, 240);
        double now = DistanceFrom(him, noise);
        Assert.True(
            now < was - 1.0,
            $"he was set walking and then did not walk to it: {was:0.##} du before, {now:0.##} du after " +
            "240 frames.");

        // …and he never says a floor number into a radio on this road. A noise may not buy a run.
        foreach (object g in Guards(map))
        {
            Assert.False((bool)Get(g, "AfterYou")!, "a bang started a run.");
            Assert.Equal(PatrolBeat.Provocation.None, (PatrolBeat.Provocation)Get(g, "Why")!);
        }
    }

    /// <summary>Which of the seven arms this man is in, off <c>Guard.DoingThisFrame</c> itself — the shipped
    /// conductor's own answer, with its two bits handed in as this bench's own facts: nobody here is the
    /// escort, and nobody is behind a cubicle door.</summary>
    private static string TheArmHeIsIn(object g) =>
        g.GetType().GetMethod("DoingThisFrame", Hidden)!.Invoke(g, [false, false])!.ToString()!;

    /// <summary>The man the round has sent to look at a bang, or null — the one piece of #618's state a fact
    /// in this file reads directly.</summary>
    private static object? LookingIntoIt(Pages.Map map)
    {
        object round = PatrolState.Round(map)!;
        return round.GetType().GetProperty("LookingIntoIt", Hidden)!.GetValue(round);
    }

    /// <summary>
    /// HE GETS THERE, FINDS NOBODY, AND GOES BACK TO WORK. The other end of the errand: the round is
    /// SUSPENDED by it and never ended, so a floor that heard one bang is walking its beat again a minute
    /// later rather than carrying a man who stopped forever at a door.
    /// </summary>
    [Fact]
    public void HeLooksAtItAndThenTheRoundCarriesOn()
    {
        (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 1);
        (double X, double Y) noise = DownHisOwnCorridor(map, 8.0);
        object him = FirstGuard(map);

        FireAt(ex, noise.X, noise.Y);
        Frames(map, 1);
        Assert.True((bool)Get(him, "WalkingUp")!);

        // Well past the clock's own bound, so both roads out of the walk are covered whichever one this floor
        // happens to take: he arrives, or the backstop ends it.
        Frames(map, (int)(PatrolBeat.WalkUpSeconds / Dt) + 120);

        Assert.False((bool)Get(him, "WalkingUp")!, "he is still walking to a bang a minute and a half old.");
        Assert.False((bool)Get(him, "AfterYou")!);

        // …and the errand is off the round with him. A reference kept past a walk-up is an errand pointing at
        // a man who is signing a watchclock station.
        Assert.Null(LookingIntoIt(map));

        // …and it never came back. One bang is answered once, however long the floor is walked afterwards.
        Frames(map, 600);
        Assert.False((bool)Get(him, "WalkingUp")!, "the same shot was answered a second time.");
    }

    // ── (b) OUT OF RANGE, ANOTHER FLOOR, AN EMPTY ONE ─────────────────────────────────────────────────

    /// <summary>
    /// <b>A SHOT NOBODY COULD HEAR BUYS NOTHING AT ALL</b> — no walk and no heat, which is the same sentence
    /// twice because they come off the same answer.
    ///
    /// <para>Three floors of it: out past <c>GunfireHeard.EarshotDu</c> with men standing on the floor; a
    /// floor the rota does not cover, where the shot lands in a corridor with nobody in it; and a shot fired
    /// before the captain took this floor, which is what a shot on ANOTHER floor looks like from here.</para>
    ///
    /// <para><b>RED by a floor-wide ear</b> — drop <c>WithinEarshot</c> from <c>GunfireHeard.NearestEar</c> and
    /// the first case reddens: <i>"a shot 44.0 du away took a man off his round."</i></para>
    /// </summary>
    [Fact]
    public void AShotNobodyCouldHearBuysNothingAtAll()
    {
        // ── out of range, on a floor with men on it ──
        (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 2);
        object him = FirstGuard(map);
        double far = GunfireHeard.EarshotDu + 10.0;
        FireAt(ex, (double)Get(him, "X")! + far, (double)Get(him, "Y")!);
        Frames(map, 240);

        Assert.Equal(0, Walking(map));
        Assert.Equal(0, HeatHere(map));

        // …and it is genuinely a shot on the ledger rather than a case that fired nothing.
        Assert.Equal(1, GunfireHeard.Count(
            (IReadOnlyList<GunfireHeard.Shot>)ex.GetType().GetProperty("ShotsHeard", Hidden)!.GetValue(ex)!));

        // ── a floor the rota does not cover ──
        (Pages.Map empty, object emptyEx) = OnAPatrolledFloor(AnUnpatrolledFloor, 2);
        Assert.Empty(Guards(empty));
        FireAt(emptyEx, 0, 0);
        Frames(empty, 240);
        Assert.Equal(0, HeatHere(empty));

        // ── and a shot that was already on the ledger when this floor was entered ──
        (Pages.Map rode, object rodeEx) = OnAPatrolledFloor(ThePatrolledFloor, 1);
        (double X, double Y) noise = DownHisOwnCorridor(rode, 10.0);
        FireAt(rodeEx, noise.X, noise.Y);

        // The lift ride: the one place a floor changes, and the one place the cursor is brought up.
        Invoke(rode, "SpawnPatrolFor", rodeEx);
        Frames(rode, 240);

        Assert.Equal(0, Walking(rode));
        Assert.Equal(0, HeatHere(rode));
    }

    // ── (c) WHAT IT COSTS, AND WHO IS OWED IT ─────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONE CROSSING, AT WEIGHT 3, TO THE OUTFIT THAT RUNS THE SITE.</b>
    ///
    /// <para>Banked on the frame somebody hears it and never again, whatever the floor does for the next four
    /// hundred frames — and banked against <c>SiteOperator.Of(body)</c> rather than against the rock, which is
    /// #715's whole law and is asserted by reading the outfit's row and the body's row separately.</para>
    ///
    /// <para><b>RED two ways.</b> Banking to the body (<c>LedgerId(bodyId)</c>) reddens the operator half:
    /// <i>"the outfit remembers 0."</i> Moving the bank below the cursor so it runs every frame reddens the
    /// once half: <i>"the outfit remembers 12 after 400 frames"</i> — the ceiling, which is what a per-frame
    /// charge looks like.</para>
    /// </summary>
    [Fact]
    public void TheCrossingIsBankedOnceAtWeightThreeToTheOutfit()
    {
        (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 2);
        var book = (ContactLedger)Get(map, "_contacts")!;
        Assert.Equal(0, HeatHere(map));

        (double X, double Y) noise = DownHisOwnCorridor(map, 10.0);
        FireAt(ex, noise.X, noise.Y);
        Frames(map, 1);

        Assert.Equal(IllegalHeat.WeightOf(IllegalHeat.Crossing.ShotOnTheirFloor), HeatHere(map));
        Assert.Equal(3, HeatHere(map));

        // …and it is the OUTFIT's book, not the rock's.
        string outfit = SiteOperator.Of(Body).Id;
        Assert.Equal(3, IllegalHeat.HeatAt(book, outfit));
        Assert.Equal(0, book.For(Body).HeatOwed);

        // …ONCE. Four hundred frames of a man walking over, arriving, and going back to his round.
        Frames(map, 400);
        Assert.Equal(3, HeatHere(map));

        // …and a SECOND bang is a second crossing, because it is a second thing that happened to them.
        (double X, double Y) again = DownHisOwnCorridor(map, 6.0);
        FireAt(ex, again.X, again.Y);
        Frames(map, 1);
        Assert.Equal(6, HeatHere(map));
    }

    // ── (d) AND NOTHING EXPLAINS IT ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOTHING IS SAID ABOUT IT, ON EITHER END OF THE WALK.</b> #603's inference horror, asked of the page
    /// rather than of a catalogue: the pulse slot the captain reads and the autopilot log the field book is
    /// written from are both exactly what they were before the gun went off — through the frame he leaves his
    /// round, the whole walk, and the frame he gives up on it.
    ///
    /// <para>This is the guard against the shape the brief named as wrong: a <c>SECURITY ALERTED</c> banner,
    /// or a man narrating what he heard. The guard simply comes.</para>
    ///
    /// <para><b>RED by saying anything</b> — one <c>_host.ShowPulseMessage</c> in <c>TheRoundHearsAShot</c>
    /// reddens it on the first frame.</para>
    /// </summary>
    [Fact]
    public void NothingIsSaidAboutItOnEitherEndOfTheWalk()
    {
        (Pages.Map map, object ex) = OnAPatrolledFloor(ThePatrolledFloor, 2);

        // Let the floor settle first, so what is measured is the SHOT's doing and not the frame the round was
        // stood up on.
        Frames(map, 30);
        object pulseWas = Get(map, "_pulse")!;
        int logWas = ((ICollection)Get(map, "_autopilotEvents")!).Count;

        (double X, double Y) noise = DownHisOwnCorridor(map, 10.0);
        FireAt(ex, noise.X, noise.Y);

        // Every frame of it, so a line said once anywhere along the walk is caught on the frame it is said.
        for (int i = 0; i < (int)(PatrolBeat.WalkUpSeconds / Dt) + 200; i++)
        {
            Frames(map, 1);
            Assert.Equal(pulseWas, Get(map, "_pulse"));
            Assert.Equal(logWas, ((ICollection)Get(map, "_autopilotEvents")!).Count);
        }

        // …and the walk really happened, so the silence is a silence about something. Anti-vacuous, because a
        // feature that never fired would pass this fact perfectly.
        Assert.Equal(3, HeatHere(map));
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static object? Get(object o, string name)
    {
        if (PatrolState.TryFollow(o, name, out object? onTheRound))
        {
            return onTheRound;
        }
        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        return prop?.GetValue(o);
    }

    private static void Set(object o, string field, object? value)
    {
        if (!PatrolState.TrySet(o, field, value))
        {
            o.GetType().GetField(field, Hidden)!.SetValue(o, value);
        }
    }

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        (object Target, MethodInfo Call)? found = PatrolState.Verb(map, method);
        Assert.True(
            found is not null,
            $"neither the page nor its round has `{method}` — this guard is reading a dead name.");
        return found!.Value.Call.Invoke(found.Value.Target, args);
    }
}
