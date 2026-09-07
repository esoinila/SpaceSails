using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1149 slice 2 · <b>PRESENTING AT THE FRONT, ON A LIVE PAGE.</b>
///
/// <para>Core owns the ladder and <c>TheSafetyInspectorsCardTests</c> holds it to its laws over two hundred
/// sites. This bench is the other half, and it is the half the laws are FOR: a shipping
/// <see cref="Pages.Map"/>, a real floor, a real round, and the captain carrying nothing but the
/// inspector's card. It provokes the challenge the way a player does — three wrong entries on the pad, and
/// the nearest man walks over — which is <c>TheCarStopsAndTheStairIsThePriceTests</c>' own bench, one
/// feature along.</para>
///
/// <list type="number">
/// <item><b>Present it where an inspection is honoured</b> → he says the authored line, the inspection is on
/// for this trip, and the SEALED row on the car panel opens. A fresh landing finds it shut again.</item>
/// <item><b>Present it where nobody is expecting anybody</b> → the same sentence, and then the round does
/// what a refused round has always done: he says the floor into his radio and the car goes away.</item>
/// </list>
/// </summary>
public sealed class TheInspectorPresentsAtTheFrontTests
{
    private const BindingFlags Hidden = TestTree.PrivateOnAnInstance;
    private const double Dt = 1.0 / 60.0;
    private const long Watch = 3L;

    private static IEnumerable<string> Bodies()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto", "titan", "miranda", "triton",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 60; i++)
        {
            yield return $"generated-moon-{i}";
        }
    }

    /// <summary>A real floor of a real site with all three things this bench needs at once: a round on it, a
    /// SEALED gate with #602's keypad (which is how the challenge is provoked AND the row whose opening is
    /// the point), and an inspection that is <paramref name="honoured"/> there on this watch.</summary>
    private static (string Body, int Floor, UndergroundComplex.LiftStop Row) AFloorWhere(bool honoured)
    {
        foreach (string body in Bodies())
        {
            foreach (int floor in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, floor)
                    || Inspectorate.HonouredAt(body, floor, Watch) != honoured)
                {
                    continue;
                }
                foreach (UndergroundComplex.LiftStop stop in
                         UndergroundComplex.LiftPanel(body, floor, [], [Inspectorate.Card]))
                {
                    if (stop.HasPad)
                    {
                        return (body, floor, stop);
                    }
                }
            }
        }

        throw new InvalidOperationException(
            $"no site in the sweep has a patrolled floor with a keypad where an inspection is "
            + $"{(honoured ? "honoured" : "not due")} — this bench has nothing to drive, and a bench with "
            + "nothing to drive cannot tell pass from fail.");
    }

    // ── (a) THE ONE THAT IS HONOURED ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HE SAYS THE LINE, THE INSPECTION IS ON, AND THE SEALED ROW OPENS — for this trip and no
    /// further.</b> Four instruments, all of them ones a player actually reads: the card in front of him,
    /// the row on the car panel, the keypad that comes off it, and a fresh landing that finds the gate shut
    /// again.
    ///
    /// <para>The wallet holds exactly one paper and it is the card, so nothing here can be the sim quietly
    /// answering with something better.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>ex.InspectionRunning = true</c> out of
    /// <c>TheRoundStopsAtYou</c>:</para>
    /// <code>
    /// the card was honoured and the gate is still ↓ THE OTHER SHAFT — SEALED.
    /// </code>
    /// </summary>
    [Fact]
    public void PresentedWhereItIsHonouredTheLineIsSaidAndTheGateOpensForThisExcursionOnly()
    {
        (string body, int floor, UndergroundComplex.LiftStop row) = AFloorWhere(honoured: true);
        Pages.Map map = OnTheFloor(body, floor);

        // BEFORE: the gate refuses, and it is the SEALED refusal with the pad on it.
        UndergroundComplex.LiftStop shut = TheGateRow(map)!.Value;
        Assert.NotNull(shut.Refusal);
        Assert.True(shut.HasPad, "this is not the SEALED row, so the guard is watching the wrong door.");
        Assert.False(Running(map), "the excursion began with an inspection already on it.");

        TheRoundReadsYourWallet(map, row);

        // THE CARD IN FRONT OF HIM SAYS THE AUTHORED SENTENCE, and it is the shipped constant.
        string told = (string)Get(Get(map, "_viewObject")!, "Outcome")!;
        Assert.Contains(Inspectorate.HonouredLine, told, StringComparison.Ordinal);

        // THE INSPECTION IS ON, AND THE GATE IS OPEN.
        Assert.True(Running(map), "he read it and walked on, and no inspection is running.");
        UndergroundComplex.LiftStop open = TheGateRow(map)!.Value;
        Assert.True(open.Refusal is null,
            $"the card was honoured and the gate is still {open.Name}.");
        Assert.True(open.OpenedByInspection, "the row does not know what opened it.");
        Assert.Contains(Inspectorate.Plate, open.OpenedBy ?? "", StringComparison.Ordinal);
        Assert.False(open.HasPad, "the gate is open and the keypad is still bolted to it.");

        // …AND NOT THE NEXT TRIP. A fresh landing on the same floor with the same card in the same wallet
        // finds the same shut gate, which is the whole of "for one excursion".
        Pages.Map next = OnTheFloor(body, floor);
        Assert.False(Running(next), "an inspection survived the shuttle.");
        Assert.NotNull(TheGateRow(next)!.Value.Refusal);
    }

    // ── (b) THE ONE THAT IS NOT ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOBODY INSPECTS UNANNOUNCED.</b> The same card, the same wallet, the same provocation, on a site
    /// with nothing on its roster for this watch: he says the same sentence — the captain cannot tell the
    /// two reads apart from it — and then the round calls it in, which on these floors is the escalation
    /// that has been here since #804 and #719: a walk back to the car, and the car taken away.
    ///
    /// <para>No new security kind is minted, and no inspection is left running.</para>
    ///
    /// <para><b>Proven RED</b> by making <c>Inspectorate.HonouredAt</c> answer true unconditionally:</para>
    /// <code>
    /// nothing is due at this site and the card was waved through anyway.
    /// </code>
    /// </summary>
    [Fact]
    public void PresentedWhereNobodyIsExpectingAnybodyTheRoundCallsItIn()
    {
        (string body, int floor, UndergroundComplex.LiftStop row) = AFloorWhere(honoured: false);
        Pages.Map map = OnTheFloor(body, floor);
        Assert.False(CarStopped(map), "the car was stopped before anything happened to this captain.");

        TheRoundReadsYourWallet(map, row);

        string told = (string)Get(Get(map, "_viewObject")!, "Outcome")!;
        Assert.Contains(Inspectorate.HonouredLine, told, StringComparison.Ordinal);
        Assert.Contains(PatrolBeat.EscortLine, told, StringComparison.Ordinal);

        Assert.False(Running(map), "nothing is due at this site and the card was waved through anyway.");
        Assert.True(CarStopped(map),
            "the bet was lost and nobody called anything in — the car is still running.");
        Assert.NotNull(Get(map, "_patrol") is { } p ? Get(p, "EscortDue") : null);
    }

    // ── the bench's plumbing (TheCarStopsAndTheStairIsThePriceTests', one feature along) ─────────────────

    private static Pages.Map OnTheFloor(string body, int floor)
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
        exType.GetProperty("AirSeconds")!.SetValue(ex, SuitAir.TankSeconds);
        exType.GetProperty("CanteenWatch")!.SetValue(ex, Watch);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_patrolCheat", (int?)2);

        // THE WALLET HOLDS THE CARD AND NOTHING ELSE, so no arm of this bench can be the sim quietly
        // answering the guard with a better paper.
        Set(map, "_satchel", new List<Satchel.Item> { Inspectorate.Card });

        Invoke(map, "RebuildSurfaceDeck");
        Invoke(map, "SpawnPatrolFor", Ex(map));
        return map;
    }

    private static object Ex(Pages.Map map) => Get(map, "_surface")!;

    private static bool Running(Pages.Map map) =>
        (bool)Ex(map).GetType().GetProperty("InspectionRunning")!.GetValue(Ex(map))!;

    private static bool CarStopped(Pages.Map map) =>
        (bool)Ex(map).GetType().GetProperty("CarStopped")!.GetValue(Ex(map))!;

    private static IReadOnlyList<UndergroundComplex.LiftStop> Rows(Pages.Map map) =>
        (IReadOnlyList<UndergroundComplex.LiftStop>)
        typeof(Pages.Map).GetMethod("LiftStops", Hidden)!.Invoke(map, [])!;

    /// <summary>The panel's gate row, read off the shipped panel — or, once the car has been taken away, the
    /// panel has no rows at all and there is nothing to read.</summary>
    private static UndergroundComplex.LiftStop? TheGateRow(Pages.Map map)
    {
        foreach (UndergroundComplex.LiftStop stop in Rows(map))
        {
            if (stop.Name.StartsWith("↓ THE OTHER SHAFT", StringComparison.Ordinal))
            {
                return stop;
            }
        }
        return null;
    }

    /// <summary>Provoke the challenge the way a player does: stand on the first guard's own next leg, miss
    /// three times on the pad, and let the nearest man walk over and read the wallet.</summary>
    private static void TheRoundReadsYourWallet(Pages.Map map, UndergroundComplex.LiftStop row)
    {
        string body = ((CelestialBody)Get(Get(Ex(map), "Stop")!, "Body")!).Id;

        (double x, double y) = DownHisOwnCorridor(map, 14.0);
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);

        string right = UndergroundComplex.LiftCode.CodeFor(body);
        string wrong = right == "1111" ? "2222" : "1111";
        for (int i = 0; i < 3; i++)
        {
            typeof(Pages.Map).GetMethod("LiftPadClear", Hidden)!.Invoke(map, []);
            foreach (char c in wrong)
            {
                typeof(Pages.Map).GetMethod("LiftPadPush", Hidden)!.Invoke(map, [c.ToString()]);
            }
            typeof(Pages.Map).GetMethod("LiftPadSubmit", Hidden)!.Invoke(map, [row]);
        }

        MethodInfo step = typeof(Pages.Map).GetMethod("AdvancePatrol", Hidden)!;
        for (int i = 0; i < 900; i++)
        {
            step.Invoke(map, [Dt]);
        }

        object? up = Get(map, "_viewObject");
        Assert.True(up is not null && (string)Get(up, "Label")! == PatrolBeat.ChallengeLabel,
            "nobody came over and read the wallet in 900 frames, so this case is about nothing.");
    }

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
