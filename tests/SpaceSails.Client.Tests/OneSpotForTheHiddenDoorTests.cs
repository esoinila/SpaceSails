using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1119 item 2 · <b>THE CONSOLE AND THE BEACON DISAGREED ABOUT WHERE THE LAB IS.</b>
///
/// <para>#625 moved the tracker's ring and its rumour wash off <see cref="SecretLab.For"/>'s raw seeded spot
/// and onto <see cref="SecretLab.HeadSpot"/> — the spot after this SITE's shelters, outpost and monolith have
/// had their say, which is where <c>MoonSurface.LiftHead</c> actually draws the shed and where
/// <c>SecretLab.ChamberFootprint</c> keeps the ground clear. It did not move the <b>hidden-door console</b>,
/// which <c>ComposeSecretLabSite</c> went on planting at the raw spot.</para>
///
/// <para>So the instrument said one thing and the ground held another, up to 235 du apart: a captain who
/// walked the ring arrived at the shed with nothing to press, and the [E] that opens Vantar's lab stood in
/// open regolith a two-minute walk away with no ring on it. The #573 family — the instrument disagreeing with
/// the ground — and #584 — the map lying — in one fixture.</para>
///
/// <h3>What is driven</h3>
///
/// <para>The shipping <c>ResolveSecretLab</c> on a shipping page, then the shipping
/// <c>ComposeSecretLabSite</c>, <c>BuildBeacons</c> and <c>BuildRumours</c>, swept over every body × site pair
/// the world lays a lift head on. Nothing is re-derived here: the console is read off the composed
/// <c>_deckPlan</c> the captain would walk on, and the beacon and the wash are read back out of the fan's own
/// polar arithmetic the way a player reads them.</para>
///
/// <h3>The tolerance, and why it can select</h3>
///
/// <para><see cref="DeckPlan.InteractRadius"/> — the shipping number, not one typed in here — because the
/// question the bug asks is exactly "if you stand where the instrument points, can you press the button?"
/// A tolerance nothing can fail is a green test that asserts nothing, so
/// <see cref="TheWorldStillHoldsASiteWhereTheRawSeedWouldFail"/> proves the world CONTAINS a site where the
/// raw seed is further from the built hut than that radius. If the placement clamp is ever neutered, that law
/// goes red for the honest reason rather than this file passing on a world that cannot tell pass from
/// fail.</para>
///
/// <para><b>Proven RED</b> on today's code: with <c>ex.Lab</c> holding <c>SecretLab.For</c>'s raw placement,
/// the console lands at the seed while the ring and the wash are on the hut, and the sweep reports the
/// disagreeing pairs by name and by distance.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class OneSpotForTheHiddenDoorTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The shipped sweep — the same list #625's guard walks, so the two cannot drift apart about
    /// which grounds count.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>Where the captain stands while the fan is read. Fixed across every site, because bearing and
    /// range are measured from here: if the painted point moves, only the thing painted could have moved it.
    /// Well clear of the deep field the lab is seeded into, so no console lands on top of the reader.</summary>
    private const double StandingX = 0.0;
    private const double StandingY = -60.0;

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A shipping page out on <paramref name="body"/>'s regolith at <paramref name="site"/>, whose
    /// lab placement was resolved by the SHIPPING <c>ResolveSecretLab</c> under the <c>?secretlab=1</c> cheat
    /// — so the sweep is about WHERE the door is and not about which bodies the seed happens to favour.</summary>
    private static Pages.Map Standing(string body, LandingSite site)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("Site")!.SetValue(ex, site);
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_surface", ex);
        Set(map, "_avatarX", StandingX);
        Set(map, "_avatarY", StandingY);
        Set(map, "_secretLabForceBodyId", body);   // the cheat: every body on the list hides one
        Get<HashSet<string>>(map, "_labLeads").Add(body);   // …and the captain has been tipped about it

        // #1119 item 1 · her own copy. This bench APPENDS to the live plan, and the singleton is the world
        // every other suite in this assembly reads.
        Set(map, "_deckPlan", DeckPlan.ShipWith([]));

        Call(map, "ResolveSecretLab", ex);
        return map;
    }

    private static void Set(object target, string field, object? value) =>
        (typeof(Pages.Map).GetField(field, Hidden)
         ?? throw new InvalidOperationException($"Map has no field {field}."))
        .SetValue(target, value);

    private static T Get<T>(object target, string field) =>
        (T)(typeof(Pages.Map).GetField(field, Hidden)
            ?? throw new InvalidOperationException($"Map has no field {field}."))
        .GetValue(target)!;

    private static object? Call(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no {method} — the publisher has moved."))
        .Invoke(map, args);

    private static object? OnTheSurface(Pages.Map map, string method) =>
        Call(map, method, Get<object>(map, "_surface"));

    private static void Reveal(Pages.Map map, bool revealed) =>
        Get<object>(map, "_surface").GetType()
            .GetProperty("SecretLabDoorRevealed")!.SetValue(Get<object>(map, "_surface"), revealed);

    /// <summary>Undo the fan's polar arithmetic and get the GROUND POINT the instrument claims — read back the
    /// way a player reads it, a bearing and a range from where they stand.</summary>
    private static (double X, double Y) Painted(double bearing, double range) =>
        (StandingX + (range * Math.Cos(bearing)), StandingY + (range * Math.Sin(bearing)));

    private static double Apart((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>The hidden-door console the composed deck actually carries — off <c>_deckPlan</c>, the plan
    /// the captain walks on, and not off any list this test built.</summary>
    private static (double X, double Y) TheConsoleOnTheGround(Pages.Map map, string where)
    {
        Get<DeckPlan>(map, "_deckPlan");
        OnTheSurface(map, "ComposeSecretLabSite");

        DeckPlan plan = Get<DeckPlan>(map, "_deckPlan");
        var found = new List<DeckPlan.ConsoleSpot>();
        foreach (DeckPlan.ConsoleSpot c in plan.Consoles)
        {
            if (c.Kind == DeckPlan.ConsoleKind.SecretDoor)
            {
                found.Add(c);
            }
        }

        Assert.True(found.Count == 1,
            $"{where}: a revealed, unforced lab put {found.Count} hidden-door consoles on the deck. This "
            + "sweep is not reading the fixture it thinks it is.");
        return (found[0].X, found[0].Y);
    }

    // ── The laws ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>LAW 1 · On every site, the button, the ring and the wash name one patch of ground — close
    /// enough that a captain standing on any of them can press the other.</summary>
    [Fact]
    public void TheConsole_TheBeacon_AndTheWash_AreOnOnePatchOfGround()
    {
        var wrong = new List<string>();
        int pairs = 0;

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                pairs++;
                string where = $"{body} · {site.Name}";
                Pages.Map map = Standing(body, site);

                // The cheat pre-reveals, which is the state the console and the ring both live in.
                Reveal(map, true);
                (double X, double Y) console = TheConsoleOnTheGround(map, where);

                var beacons =
                    (List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)>)
                    OnTheSurface(map, "BuildBeacons")!;
                var lab = beacons.FindAll(b => b.IsLab);
                Assert.True(lab.Count == 1,
                    $"{where}: a revealed door drew {lab.Count} lab rings, so this sweep is not reading the "
                    + "instrument it thinks it is.");
                (double X, double Y) ring = Painted(lab[0].Bearing, lab[0].Range);

                // The wash is the OTHER half of the same fact, and it only speaks while the door is unfound.
                Reveal(map, false);
                var wash = (List<(double Bearing, double Range, double Spread)>)
                    OnTheSurface(map, "BuildRumours")!;
                Assert.True(wash.Count == 1,
                    $"{where}: a tipped captain with an unfound lab got {wash.Count} washes.");
                (double X, double Y) tip = Painted(wash[0].Bearing, wash[0].Range);

                foreach ((string a, (double X, double Y) pa, string b, (double X, double Y) pb) in
                         new[]
                         {
                             ("the ⚙ HIDDEN DOOR console", console, "the tracker's ring", ring),
                             ("the ⚙ HIDDEN DOOR console", console, "the rumour wash", tip),
                             ("the tracker's ring", ring, "the rumour wash", tip),
                         })
                {
                    double off = Apart(pa, pb);
                    if (off > DeckPlan.InteractRadius)
                    {
                        wrong.Add(
                            $"  {where}: {a} at ({pa.X:F1}, {pa.Y:F1}) and {b} at ({pb.X:F1}, {pb.Y:F1}) — "
                            + $"{off:F1} du apart, and [E] reaches {DeckPlan.InteractRadius:F1}.");
                    }
                }
            }
        }

        Assert.True(wrong.Count == 0,
            "the lab's own fixtures do not agree where the lab is:\n" + string.Join("\n", wrong));
        Assert.True(pairs >= Bodies.Length,
            $"only {pairs} body × site pairs were swept for {Bodies.Length} bodies — this sweep has stopped "
            + "enumerating the world.");
    }

    /// <summary>LAW 2 · …and the ground they agree on is the ground the BUILDER built on. Pairwise agreement
    /// alone would be satisfied by three fixtures all pointing at the same wrong place; this asks the shed.
    /// </summary>
    [Fact]
    public void AndThatPatchOfGround_IsWhereTheShedStands()
    {
        var wrong = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                Pages.Map map = Standing(body, site);
                Reveal(map, true);
                (double X, double Y) console = TheConsoleOnTheGround(map, $"{body} · {site.Name}");

                // Asked of the BUILDER: MoonSurface.LiftHead is what draws the shed, and nothing in this test
                // is allowed to compute where it stands.
                MoonSurface.LiftHeadBox built = MoonSurface.LiftHead(body, site.LayoutSalt, Field);
                double off = Apart(console, (built.CentreX, built.CentreY));
                if (off > DeckPlan.InteractRadius)
                {
                    wrong.Add(
                        $"  {body} · {site.Name}: the hidden-door console is at ({console.X:F1}, "
                        + $"{console.Y:F1}) and the shed stands at ({built.CentreX:F1}, {built.CentreY:F1}) — "
                        + $"{off:F1} du apart.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            "the button that opens the lab is not on the building that hides it:\n" + string.Join("\n", wrong));
    }

    /// <summary>ANTI-VACUOUS. The radius above can only tell pass from fail if the world still contains a site
    /// where the RAW seed is further from the built shed than that radius. Measured off the shipped seeds; if
    /// the placement clamp ever stops relocating anything, this goes red for having stopped covering the bug
    /// rather than the laws above passing on a world that cannot fail them.</summary>
    [Fact]
    public void TheWorldStillHoldsASiteWhereTheRawSeedWouldFail()
    {
        double worst = 0;
        string where = "nowhere";

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SecretLab.Placement raw = SecretLab.For(body, Field, forcePresent: true);
                MoonSurface.LiftHeadBox built = MoonSurface.LiftHead(body, site.LayoutSalt, Field);
                double moved = Apart((raw.DoorX, raw.DoorY), (built.CentreX, built.CentreY));
                if (moved > worst)
                {
                    worst = moved;
                    where = $"{body} · {site.Name}";
                }
            }
        }

        Assert.True(worst > DeckPlan.InteractRadius,
            $"the raw seed never sat further than {worst:F1} du from the shed (worst: {where}) while [E] "
            + $"reaches {DeckPlan.InteractRadius:F1} du — so the laws in this file would pass on the raw spot "
            + "too, and they prove nothing. Either the placement clamp has stopped working, or this sweep no "
            + "longer covers #1119 item 2.");
    }
}
