using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #618 / #582 slice 1 · <b>A GUARD, BUT NOT A GOOD ONE — DRIVEN.</b> A shipping page standing on the top
/// pressurised floor of a real site, with the man at the door forced into each of his three windows, and every
/// one of the four ways past him taken through the shipped verbs: the card his reach raises, the moves on it,
/// the floor change that carries the captain up under the lid, and the frame that asks the tide.
///
/// <para>Nothing here writes his state by hand except the WINDOW he is in (<see cref="Pages.Map.ManAtTheDoor"/>
/// is built with a forced <see cref="GateGuard.Visit"/> — the dev cheat's own seam), so each fact is about what
/// the page does with a window, never about a flag a test typed in.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed partial class TheManAtTheDoorTests
{
    private const BindingFlags Hidden = TestTree.PrivateOnAnInstance;
    private const double Dt = 1.0 / 60.0;
    private const long Watch = 7;

    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    // ── THE BENCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A site whose building has a top pressurised floor with a counter on it (so his round has a
    /// canteen end), and a floor below it for the way down to be about. Asked, never typed.</summary>
    internal static (string Body, int Floor) ASiteWithADoor()
    {
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } top)
            {
                continue;
            }

            bool below = UndergroundComplex.FloorsOf(body).Any(f => f < top);
            bool counter = UndergroundComplex.Build(body, top, MoonSurface.ExpeditionField()).Amenities
                .Any(a => CounterService.For(body, a.Use) is not null);
            if (below && counter)
            {
                return (body, top);
            }
        }

        throw new InvalidOperationException("no site in the sweep has a top floor with a counter and a floor below it.");
    }

    /// <summary>(#653 slice 2 pin) A FRESH excursion on the same page, standing on the floor he keeps — what
    /// a second boarding of the same ground builds. The man is met through the page's own rebuild; only the window
    /// is forced.</summary>
    internal static void NewExcursionOnHisFloor(
        Pages.Map map, string body, int floor, GateGuard.Posting posting, bool talkWorks = false)
    {
        var ex = new Pages.Map.SurfaceExcursion
        {
            Stop = new Pages.Map.ShuttleStop(
                new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false),
            RestoreHavenId = null,
            Site = new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""),
        };
        ex.Floor = floor;
        ex.CanteenWatch = Watch;
        Set(map, "_surface", ex);
        Invoke(map, "RebuildSurfaceDeck");

        Pages.Map.ManAtTheDoor met = ex.Gate ?? throw new InvalidOperationException("the floor was drawn and nobody met the door.");
        ex.Gate = new Pages.Map.ManAtTheDoor
        {
            Ground = met.Ground,
            Window = met.Window,
            Visit = new GateGuard.Visit(posting, talkWorks),
            Floor = met.Floor,
            Post = met.Post,
            Canteen = met.Canteen,
        };
        Invoke(map, "RebuildSurfaceDeck");
    }

    /// <summary>A shipping page on the floor, with the door kept the way <paramref name="posting"/> says and
    /// the word working or not. The man is met through the page's own rebuild; only the window is forced.</summary>
    internal static Pages.Map OnHisFloor(
        string body, int floor, GateGuard.Posting posting, bool talkWorks = false)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Set(map, "_deckMode", true);
        NewExcursionOnHisFloor(map, body, floor, posting, talkWorks);
        return map;
    }

    internal static Pages.Map.SurfaceExcursion Ex(Pages.Map map) => (Pages.Map.SurfaceExcursion)Get(map, "_surface")!;

    internal static Pages.Map.ManAtTheDoor Man(Pages.Map map) => Ex(map).Gate!;

    internal static Pages.Map.Walker? Him(Pages.Map map) =>
        Ex(map).Walkers.FirstOrDefault(w => w.Walk.Plate == GateGuard.Plate);

    internal static void Frames(Pages.Map map, int n)
    {
        for (int i = 0; i < n; i++)
        {
            Invoke(map, "StepSurface", Dt);
        }
    }

    internal static void StandAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
    }

    internal static IReadOnlyList<UndergroundComplex.LiftStop> Rows(Pages.Map map) =>
        (IReadOnlyList<UndergroundComplex.LiftStop>)Invoke(map, "LiftStops")!;

    internal static DeckPlan.ConsoleSpot? Card(Pages.Map map) => (DeckPlan.ConsoleSpot?)Get(map, "_viewObject");

    internal static bool Waits(Pages.Map map) => (bool)Get(map, "TheManAtTheDoorWaits")!;

    internal static IReadOnlyList<Encounter.Move> Moves(Pages.Map map) =>
        (IReadOnlyList<Encounter.Move>)Invoke(map, "TheManAtTheDoorsMoves")!;

    internal static void Press(Pages.Map map, string move) => Invoke(map, "AnswerTheManAtTheDoor", move);

    internal static HashSet<string> Register(Pages.Map map) => (HashSet<string>)Get(map, "_roomsTurnedOver")!;

    internal static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden) ?? throw new InvalidOperationException($"no field {field}"))
        .SetValue(o, value);

    internal static object? Get(object o, string name)
    {
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

    internal static object? Invoke(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden) ?? throw new InvalidOperationException($"no method {method}"))
        .Invoke(map, args);

}
