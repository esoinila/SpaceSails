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
/// #602 · <b>THREE WRONG ENTRIES, AND SOMEBODY LEAVES HIS ROUND — driven on a live page.</b>
///
/// <para>Owner's ruling, 2026-08-02: <i>"it gives you 3 tries before calling security"</i>, and the counter is
/// a decay window: <i>"the alert resets if you just walk away… but if you repeat the try soon after before the
/// reset then the security patrol comes"</i>. The arithmetic is pinned one project along
/// (<c>ThePadOnTheCarPanelTests</c>); what is here is the thing itself — a real <see cref="Pages.Map"/>, on a
/// real generated Hive floor of a real site whose panel really carries a keypad, with a real round on it, and
/// the shipped ↵ pressed the way a captain presses it.</para>
///
/// <para><b>The bench is #906's</b>, by way of <c>AShotOnAPatrolledFloorTests</c>, deliberately and not by
/// copy-paste convenience: it is the harness in this repository that drives the SHIPPED round rather than a
/// replica of it, and this feature's whole risk is that a rule which reads correctly never reaches a man's
/// legs. #602's summons is #618's walk to a place pointed at a keypad, so it is proved in the same bench
/// that proved the walk.</para>
///
/// <h3>Proven able to fail, each by breaking the shipped code and watching it go red</h3>
/// <list type="bullet">
/// <item>Not calling <c>SecurityWasCalledToThePad</c> on the third miss reddens
/// <see cref="TheThirdWrongEntryFetchesTheNearestManAndHeChallengesYouAtThePad"/>.</item>
/// <item>Dropping the window (misses never forgotten) reddens
/// <see cref="TwoTriesOutsideTheWindowFetchNobody"/>.</item>
/// <item>Saying anything at all about it reddens <see cref="NothingOnThePageExplainsThePatrol"/>.</item>
/// <item>Writing the opened band anywhere but the excursion reddens
/// <see cref="ARightCodeOpensTheGateForThisTripOnly"/>.</item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ThreeWrongEntriesFetchSomebodyTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>#906's own watch number, so this file and the round's other benches stand a floor up
    /// identically.</summary>
    private const long Watch = 7;

    /// <summary>A live rAF frame.</summary>
    private const double Dt = 1.0 / 60.0;

    // ── FINDING A REAL PAD ON A REAL PATROLLED FLOOR ──────────────────────────────────────────────────

    /// <summary>
    /// A site, and a floor of it, where the shipped panel really draws a keypad AND the rota really covers
    /// the floor. Both asked of Core rather than typed: a hand-picked pair is a pair that goes stale the
    /// first time a generator moves, and a guard standing on a floor with no pad on it would pass forever.
    /// </summary>
    private static (string Body, int Floor, UndergroundComplex.LiftStop Row) APaddedPatrolledFloor()
    {
        foreach (string body in new[]
                 {
                     "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "miranda",
                     "triton", "the-clinker",
                 })
        {
            if (UndergroundComplex.LiftCode.PaperRoomFor(body) is null)
            {
                continue;
            }
            foreach (int floor in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, floor))
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
            "no site in the sweep has a keypad on a floor the rota covers — this bench has nothing to drive.");
    }

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    private static (Pages.Map Map, object Ex, UndergroundComplex.LiftStop Row) AtThePad(int heads)
    {
        (string body, int floor, UndergroundComplex.LiftStop row) = APaddedPatrolledFloor();
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
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);
        exType.GetProperty("CanteenWatch")!.SetValue(ex, Watch);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_patrolCheat", (int?)heads);

        Invoke(map, "RebuildSurfaceDeck");
        Invoke(map, "SpawnPatrolFor", ex);

        // THE CAPTAIN STANDS AT THE PAD, and the pad is wherever the captain is: you cannot open a car panel
        // from across the floor. Put him on the first guard's own next leg so the floor really publishes a
        // route between them — a coordinate measured into a wall would prove nothing about a walk.
        (double X, double Y) at = DownHisOwnCorridor(map, 14.0);
        Set(map, "_avatarX", at.X);
        Set(map, "_avatarY", at.Y);
        return (map, ex, row);
    }

    /// <summary>A spot on the first guard's own next leg, <paramref name="du"/> along it — a place his own
    /// legs could take him, on a route the floor really publishes. <c>AShotOnAPatrolledFloorTests</c>'
    /// helper, one feature along.</summary>
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

    // ── PRESSING ↵ ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Key four digits and press ↵, through the shipped verbs and nothing else — the same two calls
    /// the razor's buttons make.</summary>
    private static void Key(Pages.Map map, UndergroundComplex.LiftStop row, string code)
    {
        MethodInfo push = typeof(Pages.Map).GetMethod("LiftPadPush", Hidden)!;
        MethodInfo submit = typeof(Pages.Map).GetMethod("LiftPadSubmit", Hidden)!;
        MethodInfo clear = typeof(Pages.Map).GetMethod("LiftPadClear", Hidden)!;

        clear.Invoke(map, []);
        foreach (char c in code)
        {
            push.Invoke(map, [c.ToString()]);
        }
        submit.Invoke(map, [row]);
    }

    /// <summary>A code this site's pad does not answer to, derived off the right one rather than typed: a
    /// literal "0000" is a literal that is one day the real code, and this guard would then quietly test a
    /// pad opening.</summary>
    private static string AWrongCode(string body)
    {
        string right = UndergroundComplex.LiftCode.CodeFor(body);
        string wrong = right == "1111" ? "2222" : "1111";
        Assert.False(UndergroundComplex.LiftCode.Answers(body, wrong));
        return wrong;
    }

    private static string BodyOf(object ex) =>
        ((CelestialBody)ex.GetType().GetProperty("Stop")!.GetValue(ex)!
            .GetType().GetProperty("Body")!.GetValue(
                ex.GetType().GetProperty("Stop")!.GetValue(ex)!)!).Id;

    private static string? Said(object ex) =>
        (string?)ex.GetType().GetProperty("LiftPadSaid", Hidden)!.GetValue(ex);

    private static void SecondsOnTheGround(object ex, double t) =>
        ex.GetType().GetProperty("SecondsOnTheGround", Hidden)!.SetValue(ex, t);

    // ── (a) THE THIRD ONE FETCHES SOMEBODY ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>TWO MISSES COST NOTHING BUT A PLATE. THE THIRD FETCHES THE NEAREST MAN, AND HE CHALLENGES YOU AT
    /// THE PAD.</b>
    ///
    /// <para>Five things are asserted and the last two are the ones that matter. The pad counts aloud
    /// (<c>WRONG · 1</c>, <c>WRONG · 2</c>) and nobody moves; the third says <c>SECURITY CALLED</c> and the
    /// pad goes dark; somebody leaves his round on that frame; <b>the distance from him to the pad goes
    /// DOWN</b>, because a man who was set walking and then walked somewhere else is exactly the
    /// sim-and-the-sentence disagreement this repo has paid for; and <b>what finally goes up is the GENERAL
    /// HANDS challenge this ground already had</b> — <c>PatrolBeat.ChallengeLabel</c>, off the shipped card,
    /// with no new kind of security anywhere in it.</para>
    ///
    /// <para><b>RED</b> by deleting the <c>SecurityWasCalledToThePad()</c> call from <c>LiftPadSubmit</c>:
    /// <i>"nobody left his round in 480 frames."</i></para>
    /// </summary>
    [Fact]
    public void TheThirdWrongEntryFetchesTheNearestManAndHeChallengesYouAtThePad()
    {
        (Pages.Map map, object ex, UndergroundComplex.LiftStop row) = AtThePad(2);
        string body = BodyOf(ex);
        string wrong = AWrongCode(body);

        Assert.Equal(0, Walking(map));

        Key(map, row, wrong);
        Assert.Equal(UndergroundComplex.LiftCode.WrongOnePlate, Said(ex));
        Assert.Equal(0, Walking(map));
        Assert.Null(LookingIntoIt(map));

        Key(map, row, wrong);
        Assert.Equal(UndergroundComplex.LiftCode.WrongTwoPlate, Said(ex));
        Assert.Equal(0, Walking(map));
        Assert.Null(LookingIntoIt(map));

        // …AND THE THIRD ONE.
        (double X, double Y) pad = ((double)Get(map, "_avatarX")!, (double)Get(map, "_avatarY")!);
        object him = FirstGuard(map);
        double was = DistanceFrom(him, pad);

        Key(map, row, wrong);
        Assert.Equal(UndergroundComplex.LiftCode.SecurityCalledPlate, Said(ex));
        Assert.True(IsDark(ex), "the pad called security and stayed lit.");

        Assert.NotNull(LookingIntoIt(map));
        Assert.Equal(1, Walking(map));   // one man, one errand — never a floor-wide hunt

        Frames(map, 240);
        double now = DistanceFrom(him, pad);
        Assert.True(now < was - 1.0,
            $"he was sent for and then did not walk to the pad: {was:0.##} du before, {now:0.##} du after "
            + "240 frames.");

        // …AND WHAT ARRIVES IS THE CHALLENGE THIS GROUND ALREADY HAD.
        Frames(map, 480);
        object? up = Get(map, "_viewObject");
        Assert.NotNull(up);
        Assert.Equal(PatrolBeat.ChallengeLabel, (string)Get(up!, "Label")!);

        // …and nobody ever said a floor number into a radio. A keypad may not buy a run.
        foreach (object g in Guards(map))
        {
            Assert.False((bool)Get(g, "AfterYou")!, "a wrong code started a chase.");
            Assert.Equal(PatrolBeat.Provocation.None, (PatrolBeat.Provocation)Get(g, "Why")!);
        }
    }

    // ── (b) THE WINDOW ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>TWO TRIES A HUNDRED SECONDS APART ARE TWO BORED TECHNICIANS.</b> The owner's own reason for a decay
    /// window, driven end to end on the page: three misses in total, the third one outside the window opened
    /// by the first, and nobody is sent for.
    ///
    /// <para>It is the same three presses as case (a) with nothing between them but the clock, which is what
    /// makes the pair worth having: the difference between a patrol and no patrol is ninety seconds and
    /// nothing else.</para>
    ///
    /// <para><b>RED</b> by dropping the window (<c>LiftCode.Forgotten</c> returning false): the third press
    /// then fetches a man on a floor where nothing has happened for two minutes.</para>
    /// </summary>
    [Fact]
    public void TwoTriesOutsideTheWindowFetchNobody()
    {
        (Pages.Map map, object ex, UndergroundComplex.LiftStop row) = AtThePad(2);
        string wrong = AWrongCode(BodyOf(ex));

        SecondsOnTheGround(ex, 0);
        Key(map, row, wrong);
        Assert.Equal(UndergroundComplex.LiftCode.WrongOnePlate, Said(ex));

        SecondsOnTheGround(ex, 100);
        Key(map, row, wrong);
        Assert.Equal(UndergroundComplex.LiftCode.WrongOnePlate, Said(ex));   // its own window, its own first

        SecondsOnTheGround(ex, 250);
        Key(map, row, wrong);
        Assert.Equal(UndergroundComplex.LiftCode.WrongOnePlate, Said(ex));

        Assert.Null(LookingIntoIt(map));
        Assert.Equal(0, Walking(map));
        Assert.False(IsDark(ex));

        Frames(map, 480);
        Assert.Equal(0, Walking(map));
        Assert.Null(Get(map, "_viewObject"));
    }

    // ── (c) A RIGHT CODE ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>A RIGHT CODE OPENS THE GATE FOR THIS TRIP, AND THE PANEL SAYS SO.</b> The paper in the room is
    /// carried up to the car and typed in: the row that was SEALED a frame ago has no refusal on it, the pad
    /// comes off it, and nobody is fetched.
    ///
    /// <para>And it is written on the EXCURSION and nowhere else. The vault is asserted untouched by asking
    /// the panel again with a fresh excursion's empty set — which is what the next landing is.</para>
    ///
    /// <para><b>RED</b> by writing the opened band to <c>HiveShaftsOpened</c> or the vault instead: the fresh
    /// panel then opens too.</para>
    /// </summary>
    [Fact]
    public void ARightCodeOpensTheGateForThisTripOnly()
    {
        (Pages.Map map, object ex, UndergroundComplex.LiftStop row) = AtThePad(1);
        string body = BodyOf(ex);

        // The number the room's own paper says, read the way a captain reads it.
        (int Level, int RoomIndex) kept = UndergroundComplex.LiftCode.PaperRoomFor(body)!.Value;
        string onThePaper = UndergroundComplex.LiftCode.PaperLine(body);
        string code = UndergroundComplex.LiftCode.CodeFor(body);
        Assert.Contains(code, onThePaper, StringComparison.Ordinal);
        Assert.Equal(
            UndergroundComplex.Haul.Records,
            UndergroundComplex.InRoom(body, kept.Level, kept.RoomIndex));

        Key(map, row, code);

        Assert.Equal(UndergroundComplex.LiftCode.OpenPlate, Said(ex));
        Assert.Null(LookingIntoIt(map));
        Assert.Equal(0, Walking(map));

        // The PANEL agrees, asked of the page's own verb rather than of this file's opinion.
        var panel = (IReadOnlyList<UndergroundComplex.LiftStop>)
            typeof(Pages.Map).GetMethod("LiftStops", Hidden)!.Invoke(map, [])!;
        UndergroundComplex.LiftStop opened = panel.Single(s => s.Level == row.Level);
        Assert.Null(opened.Refusal);
        Assert.False(opened.HasPad);

        // …and the next landing is shut again. A code buys the afternoon; the card is the durable way in.
        UndergroundComplex.LiftStop nextTime = UndergroundComplex
            .LiftPanel(body, (int)Get(ex, "Floor")!, [], null, 0, new HashSet<int>())
            .Single(s => s.Level == row.Level);
        Assert.NotNull(nextTime.Refusal);
        Assert.True(nextTime.HasPad);
    }

    // ── (d) AND NOTHING EXPLAINS IT ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NO LINE ON THE PAGE EXPLAINS THE PATROL.</b> The sticker said what it costs before the first press
    /// and the pad has said <c>SECURITY CALLED</c>; a third sentence would be the building explaining its own
    /// consequence to the person it is happening to (§13.8, #603's inference horror).
    ///
    /// <para>Asked of the page rather than of a catalogue: the pulse slot the captain reads and the autopilot
    /// log the field book is written from are both exactly what they were before the third press, through the
    /// frame he leaves his round and every frame of the walk over.</para>
    ///
    /// <para><b>It stops at the moment he sees you</b>, and that boundary is the ruling rather than a
    /// convenience. From there it is #833's ladder — the hail, the walk-up, the card — and that ladder has
    /// had its own sentences since #804 and is meant to. What #602 may not add is a word about WHY he came:
    /// the whole register is that the captain infers it, having read the sticker.</para>
    ///
    /// <para><b>RED by saying anything</b> — one <c>ShowPulseMessage</c> in <c>LiftPadSubmit</c> or in
    /// <c>SecurityWasCalledTo</c> reddens it on the first frame.</para>
    /// </summary>
    [Fact]
    public void NothingOnThePageExplainsThePatrol()
    {
        (Pages.Map map, object ex, UndergroundComplex.LiftStop row) = AtThePad(2);
        string wrong = AWrongCode(BodyOf(ex));

        Frames(map, 30);   // let the floor settle, so what is measured is the PAD's doing
        object pulseWas = Get(map, "_pulse")!;
        int logWas = ((ICollection)Get(map, "_autopilotEvents")!).Count;

        Key(map, row, wrong);
        Key(map, row, wrong);
        Key(map, row, wrong);

        Assert.Equal(pulseWas, Get(map, "_pulse"));
        Assert.Equal(logWas, ((ICollection)Get(map, "_autopilotEvents")!).Count);

        // Every frame of the errand, so a line said once anywhere along it is caught on the frame it is said.
        // It ends when he stops being on an errand and starts being a man who has seen somebody — from there
        // the round is doing its own thing and #833's ladder is allowed to speak.
        Assert.NotNull(LookingIntoIt(map));
        int walked = 0;
        for (; walked < 900; walked++)
        {
            Frames(map, 1);
            if (LookingIntoIt(map) is null)
            {
                break;   // he has seen you; from here the round's own ladder is speaking, and may
            }
            Assert.Equal(pulseWas, Get(map, "_pulse"));
            Assert.Equal(logWas, ((ICollection)Get(map, "_autopilotEvents")!).Count);
        }

        // …and the walk really happened, and really took frames, so the silence is a silence ABOUT something.
        // Anti-vacuous twice over, because a summons that never fired would pass this fact perfectly.
        Assert.Equal(UndergroundComplex.LiftCode.SecurityCalledPlate, Said(ex));
        Assert.True(walked > 10,
            $"the errand was over in {walked} frames — nothing was walked, so nothing was kept quiet.");
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static bool IsDark(object ex) =>
        UndergroundComplex.LiftCode.IsDark(
            (UndergroundComplex.LiftCode.Pad)ex.GetType().GetProperty("LiftPad", Hidden)!.GetValue(ex)!,
            (double)ex.GetType().GetProperty("SecondsOnTheGround", Hidden)!.GetValue(ex)!);

    private static object FirstGuard(Pages.Map map) => Guards(map)[0];

    private static IReadOnlyList<object> Guards(Pages.Map map) =>
        [.. ((IEnumerable)Get(map, "_guards")!).Cast<object>()];

    private static int Walking(Pages.Map map) =>
        Guards(map).Count(g => (bool)Get(g, "WalkingUp")!);

    private static object? LookingIntoIt(Pages.Map map)
    {
        object round = PatrolState.Round(map)!;
        return round.GetType().GetProperty("LookingIntoIt", Hidden)!.GetValue(round);
    }

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
        Assert.True(found is not null,
            $"neither the page nor its round has `{method}` — this guard is reading a dead name.");
        return found!.Value.Call.Invoke(found.Value.Target, args);
    }
}
