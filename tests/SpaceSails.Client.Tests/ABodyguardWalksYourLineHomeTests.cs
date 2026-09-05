using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #326 · <b>A BODYGUARD WALKS YOUR LINE HOME — driven on a live page.</b>
///
/// <para>Owner, live 2026-07-18: <i>"I think the bot at surface should act a bit like a body guard. Protect
/// the path to the ship at about half way so there is always a way to retreat back to safety (until bullets
/// run out) :-D"</i></para>
///
/// <para>The doctrine's arithmetic is pinned one project along
/// (<c>NothingComesBetweenTheCaptainAndTheShuttleTests</c>). What is here is the thing itself: a real
/// <see cref="Pages.Map"/> on real regolith, the shipping key handler pressed the way a captain presses it,
/// and then the shipping frame stepped until somebody walks. This project exists because a rule that reads
/// correctly and never reaches a machine's legs is the failure mode this repository keeps paying for — the
/// lift that only went down survived three PRs of correct reasoning.</para>
///
/// <h3>Proven able to fail, each by breaking the shipping code and watching it go red</h3>
/// <list type="bullet">
/// <item>Dropping the <c>StepEscorts</c> call from the surface frame reddens
/// <see cref="TheEscortWalksToTheMiddleOfTheLineAndFollowsTheCaptainAlongIt"/>.</item>
/// <item>Ignoring the modifier — deploying every bot in the posted stance — reddens
/// <see cref="ShiftTIsTheOtherStanceAndPlainTIsStillThePost"/>.</item>
/// <item>Letting a dry escort keep walking reddens <see cref="ADryEscortStopsWhereItStands"/>.</item>
/// <item>Skipping escorts in the volley's live list reddens
/// <see cref="BothStancesAreInTheSameVolleyAndDrainTheSameDrum"/>.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ABodyguardWalksYourLineHomeTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The Stickney rim — the one authored ground with no maze between the landing band and the
    /// object, which is what a walking guard wants under it (worldbuilding-notes §8, the-landing-site §10).
    /// The guards below never assume open ground anyway: they measure against the shipping post, walls and
    /// all.</summary>
    private const string Body = "phobos";

    /// <summary>A live rAF frame.</summary>
    private const double Dt = 1.0 / 60.0;

    /// <summary>Where the captain is standing when he sets a bot down. Deliberately NOT on any of the
    /// midpoints the walks below ask for — off to one side and up-field — so a bot that never took a step
    /// cannot pass a single case in this file by standing still in the right place.</summary>
    private static readonly (double X, double Y) DeployedAt =
        (MoonSurface.SpawnX + 20.0, MoonSurface.SpawnY - 20.0);

    /// <summary>A second square, a few paces off <see cref="DeployedAt"/>. The retrieve arm of the verb wins
    /// while the captain is standing on a set-down bot (that is #314's law and this lane does not touch it),
    /// so setting a SECOND one down means walking off the first — exactly as a captain has to.</summary>
    private static readonly (double X, double Y) ThenAStepOver =
        (DeployedAt.X + (DeckPlan.InteractRadius * 2), DeployedAt.Y);

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    private static Pages.Map OnTheRegolith(int botsInTheSling)
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
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Type botType = typeof(Pages.Map).GetNestedType("SurfaceBot", Hidden | BindingFlags.Static)!;
        var sling = (IList)exType.GetProperty("Bots")!.GetValue(ex)!;
        for (int i = 0; i < botsInTheSling; i++)
        {
            object bot = Activator.CreateInstance(botType, nonPublic: true)!;
            Set(bot, "Unit", SentryBot.RosterUnits[i]);
            Set(bot, "Rounds", SentryBot.MaxMagazine);
            Set(bot, "Deployed", false);
            sling.Add(bot);
        }

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        StandAt(map, DeployedAt);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Press the key a captain presses, through the shipping handler — with or without the
    /// modifier. Driving the KEY and not the verb is the point: the plate names ⇧T, and a bar that names a
    /// press nothing is listening for is the affordance-that-is-not-there this repo has shipped before.</summary>
    private static void PressT(Pages.Map map, bool shift) =>
        Assert.True(
            (bool)typeof(Pages.Map).GetMethod("HandleDeckKey", Hidden)!
                .Invoke(map, [shift ? "T" : "t", shift])!,
            "the deck did not consume the sentry key at all.");

    private static void StandAt(Pages.Map map, (double X, double Y) at)
    {
        Set(map, "_avatarX", at.X);
        Set(map, "_avatarY", at.Y);
    }

    private static void Frames(Pages.Map map, string step, int n)
    {
        MethodInfo walk = typeof(Pages.Map).GetMethod(step, Hidden)
            ?? throw new InvalidOperationException($"the page has no {step} — this guard is dead.");
        for (int i = 0; i < n; i++)
        {
            walk.Invoke(map, [Dt]);
        }
    }

    private static IReadOnlyList<object> Bots(Pages.Map map) =>
        [.. ((IEnumerable)typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!
            .GetProperty("Bots")!.GetValue(Get(map, "_surface")!)!).Cast<object>()];

    private static (double X, double Y) Where(object bot) =>
        ((double)Get(bot, "X")!, (double)Get(bot, "Y")!);

    /// <summary>The line the page itself believes in, asked of the page rather than rebuilt here — the one
    /// projection the escort walks against (#729's discipline: never restate the arithmetic under test).</summary>
    private static SentryDoctrine.RetreatLine TheLine(Pages.Map map) =>
        (SentryDoctrine.RetreatLine)typeof(Pages.Map)
            .GetProperty("TheRetreatLine", Hidden)!.GetValue(map)!;

    /// <summary>…and the post it is walking to, likewise: the shipping function over the shipping walls, so
    /// a corridor with stone in it is measured honestly instead of assumed away.</summary>
    private static (double X, double Y) ThePost(Pages.Map map) =>
        SentryDoctrine.HoldingPoint(
            TheLine(map), DeckPlan.AvatarRadius, ((DeckPlan)Get(map, "_deckPlan")!).CollisionField);

    private static double Gap((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>How far along the captain→home line a spot sits: 0 at the captain's boots, 1 at the door.
    /// "Between" is a number, and this is it.</summary>
    private static double Along(SentryDoctrine.RetreatLine line, (double X, double Y) at)
    {
        double dx = line.HomeX - line.CaptainX, dy = line.HomeY - line.CaptainY;
        double len2 = (dx * dx) + (dy * dy);
        return len2 <= 0 ? 0 : (((at.X - line.CaptainX) * dx) + ((at.Y - line.CaptainY) * dy)) / len2;
    }

    // ── (a) THE WALK ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #326 clause 2 · <b>SET IT DOWN, WALK, AND IT IS BETWEEN YOU AND THE TUBE</b> — the whole ticket,
    /// driven end to end on a page.
    ///
    /// <para>The captain presses ⇧T at his own boots (so the bot starts at <c>t = 0</c> on the line, as far
    /// from a bodyguard's post as it is possible to be), then walks two legs of a real excursion. After each
    /// leg the bot has closed on the shipping post, sits inside the corridor it is guarding, and is
    /// genuinely BETWEEN — its position along the live line is in the middle of it, not at either end.</para>
    ///
    /// <para><b>And it recomputes.</b> The second leg is sideways, so a bot that had merely walked once to a
    /// remembered spot would still be standing on the first leg's middle. Two legs, two different posts, and
    /// the bot on both.</para>
    /// </summary>
    [Fact]
    public void TheEscortWalksToTheMiddleOfTheLineAndFollowsTheCaptainAlongIt()
    {
        Pages.Map map = OnTheRegolith(1);
        PressT(map, shift: true);

        object guard = Assert.Single(Bots(map));
        Assert.True((bool)Get(guard, "Deployed")!, "⇧T did not set the bot down at all.");
        Assert.True((bool)Get(guard, "HoldsTheLine")!, "⇧T set it down in the posted stance.");
        Assert.Equal(DeployedAt.X, Where(guard).X, 6);

        var legs = new (double X, double Y)[]
        {
            (MoonSurface.SpawnX, MoonSurface.SpawnY - 140.0),   // deeper
            (MoonSurface.SpawnX + 90.0, MoonSurface.SpawnY - 140.0),   // …and sideways
        };

        var posts = new List<(double X, double Y)>();
        foreach ((double X, double Y) leg in legs)
        {
            StandAt(map, leg);
            (double X, double Y) post = ThePost(map);
            posts.Add(post);

            Frames(map, "StepSurface", 1200);   // twenty seconds of the WHOLE surface frame

            (double X, double Y) at = Where(guard);
            // Off its mark by less than the corridor is half-wide. Loose on purpose and not by accident:
            // the ground has stone in it, and a bodyguard grinding on a slab a few units short of its post
            // is #324's law working rather than this feature failing. The claims that carry the ticket are
            // the two below — it is IN the corridor, and it is BETWEEN.
            Assert.True(
                Gap(at, post) <= SentryDoctrine.CorridorHalfWidthDu,
                $"the bodyguard is {Gap(at, post):0.##} du off its post at ({post.X:0.#},{post.Y:0.#}); it "
                + $"is standing at ({at.X:0.#},{at.Y:0.#}).");

            SentryDoctrine.RetreatLine line = TheLine(map);
            Assert.True(
                SentryDoctrine.ThreatensTheLine(line, at.X, at.Y),
                "the bodyguard is not even standing in the corridor it is supposed to be holding.");

            double t = Along(line, at);
            Assert.InRange(t, 0.25, 0.75);
        }

        // The two legs really did ask for two different places to stand — the guard against a "recomputed"
        // post that is recomputed to the same answer every time.
        Assert.True(
            Gap(posts[0], posts[1]) > 20.0,
            "both legs wanted the same post; this bench cannot tell a following guard from a walked-once one.");
    }

    /// <summary>
    /// #326 clause 2 · <b>TWO STANCES, AND THE MODIFIER IS THE CHOICE.</b> Two bots, the same ground, the
    /// same walk: the one set down with plain T has not moved a millimetre, and the one set down with ⇧T is
    /// out on the line. A build that ignored the modifier would have both standing where they were put, and
    /// one that ignored the plain press would have both out on the road.
    /// </summary>
    [Fact]
    public void ShiftTIsTheOtherStanceAndPlainTIsStillThePost()
    {
        Pages.Map map = OnTheRegolith(2);

        PressT(map, shift: false);   // the post, at his boots
        StandAt(map, ThenAStepOver);
        PressT(map, shift: true);    // …and the bodyguard, a step further on

        object post = Bots(map)[0], bodyguard = Bots(map)[1];
        Assert.False((bool)Get(post, "HoldsTheLine")!);
        Assert.True((bool)Get(bodyguard, "HoldsTheLine")!);

        (double X, double Y) postWas = Where(post);

        StandAt(map, (MoonSurface.SpawnX + 60.0, MoonSurface.SpawnY - 150.0));
        Frames(map, "StepSurface", 1200);

        Assert.Equal(postWas, Where(post));
        Assert.True(
            Gap(Where(bodyguard), postWas) > 20.0,
            "the bodyguard never left the spot it was set down on.");
    }

    /// <summary>
    /// #326 clause 3 · <b>UNTIL THE BULLETS RUN OUT.</b> Owner's own parenthesis, and it is the expiry of the
    /// whole guarantee: at 00 the escort stops where it stands, and what is left on the retreat line is a bot
    /// with a frozen counter — the mark #316 already reads and the write-off the ledger already prints. No
    /// new husk was minted for it.
    /// </summary>
    [Fact]
    public void ADryEscortStopsWhereItStands()
    {
        Pages.Map map = OnTheRegolith(1);
        PressT(map, shift: true);
        object guard = Assert.Single(Bots(map));

        // Walk it out to its post first, so what is asserted afterwards is a bot that COULD have kept going.
        StandAt(map, (MoonSurface.SpawnX, MoonSurface.SpawnY - 140.0));
        Frames(map, "StepSurface", 1200);
        (double X, double Y) held = Where(guard);
        Assert.True(Gap(held, DeployedAt) > 20.0, "it never set off, so there is nothing here to stop.");

        Set(guard, "Rounds", 0);
        StandAt(map, (MoonSurface.SpawnX + 120.0, MoonSurface.SpawnY - 200.0));
        Frames(map, "StepSurface", 1200);

        Assert.Equal(held, Where(guard));

        // …and it is the write-off that was already there, in the words that were already there.
        string ledger = SentryBot.AbandonLedgerLine(
            (string)Get(guard, "Unit")!, (int)Get(guard, "Rounds")!);
        Assert.Contains("00", ledger, StringComparison.Ordinal);
        Assert.True(SentryBot.IsDry((int)Get(guard, "Rounds")!));
    }

    /// <summary>
    /// #326 clause 5 · <b>BOTH STANCES ARE IN THE SAME VOLLEY AND DRAIN THE SAME DRUM.</b> A stance that
    /// quietly fell out of the firing list would be a bodyguard that walks your line home and never shoots
    /// anything on it — and it would look exactly right on screen the whole time.
    ///
    /// <para>Both bots are set down on the same square with one Old One inside both arcs, and one fire-tick
    /// is resolved through the shipping <c>StepSentries</c>. Two drums, one round each.</para>
    /// </summary>
    [Fact]
    public void BothStancesAreInTheSameVolleyAndDrainTheSameDrum()
    {
        Pages.Map map = OnTheRegolith(2);
        PressT(map, shift: false);
        StandAt(map, ThenAStepOver);
        PressT(map, shift: true);

        object post = Bots(map)[0], bodyguard = Bots(map)[1];
        Assert.True((bool)Get(post, "Deployed")! && (bool)Get(bodyguard, "Deployed")!,
            "both bots did not go down; the second press shouldered the first instead.");
        Assert.Equal(SentryBot.MaxMagazine, (int)Get(post, "Rounds")!);
        Assert.Equal(SentryBot.MaxMagazine, (int)Get(bodyguard, "Rounds")!);

        // One Old One, four units off both guns — well inside the arc, with nothing between.
        AnOldOneAt(map, ((DeployedAt.X + ThenAStepOver.X) / 2.0, DeployedAt.Y));

        // One tick of the cadence, resolved by the shipping volley.
        Frames(map, "StepSentries", (int)Math.Ceiling(SentryBot.FireIntervalSeconds / Dt) + 1);

        Assert.Equal(SentryBot.MaxMagazine - 1, (int)Get(post, "Rounds")!);
        Assert.Equal(SentryBot.MaxMagazine - 1, (int)Get(bodyguard, "Rounds")!);
    }

    private static void AnOldOneAt(Pages.Map map, (double X, double Y) at)
    {
        Type reeverType = typeof(Pages.Map).GetNestedType("Reever", Hidden | BindingFlags.Static)!;
        object r = Activator.CreateInstance(reeverType, nonPublic: true)!;
        reeverType.GetField("X")!.SetValue(r, at.X);
        reeverType.GetField("Y")!.SetValue(r, at.Y);
        ((IList)Get(map, "_reevers")!).Add(r);
    }

    // ── REFLECTION PLUMBING ───────────────────────────────────────────────────────────────────────────

    private static object? Get(object on, string name) =>
        on.GetType().GetField(name, Hidden)?.GetValue(on)
        ?? on.GetType().GetProperty(name, Hidden)?.GetValue(on);

    private static void Set(object on, string name, object? value)
    {
        FieldInfo? f = on.GetType().GetField(name, Hidden);
        if (f is not null)
        {
            f.SetValue(on, value);
            return;
        }
        PropertyInfo p = on.GetType().GetProperty(name, Hidden)
            ?? throw new InvalidOperationException($"{on.GetType().Name} has no {name}.");
        p.SetValue(on, value);
    }

    private static void Invoke(object on, string name) =>
        (on.GetType().GetMethod(name, Hidden)
         ?? throw new InvalidOperationException($"{on.GetType().Name} has no {name}.")).Invoke(on, null);
}
