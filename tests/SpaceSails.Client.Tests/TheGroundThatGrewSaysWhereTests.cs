using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #584 · <b>"NEW GROUND ON THE PLAN" NEVER SAID WHERE.</b>
///
/// <para>Owner, mid-tour of the rebuilt grounds: <i>"I got like one 'you expanded the map' notification in
/// one map but I was left totally un-aware about what that did and where?"</i></para>
///
/// <para>Three paths append real ground to the live plan — a forced expedition door, an outpost hatch,
/// Vantar's concealed lab door — and every one of them raised the same #563 card, which explained the
/// MECHANIC at length and named no place at all. The chamber is laid at a seeded spot that is routinely off
/// the current view, so a captain read the card, closed it, turned round and had nothing to walk toward.
/// A notification that cannot be acted on is worse than silence: the player now knows they have missed
/// something.</para>
///
/// <para><b>The fix is two halves and this file holds both to the same fact.</b> The card gains a plate —
/// <see cref="GroundGrows.Where"/>, composed out of the floor's own stencil, the SDR kit's compass and the
/// fan's own "N du" (pinned token by token in Core's <c>GroundGrowsTests</c>) — and the motion tracker gains
/// a RING on the new ground for the rest of the excursion, because a card is gone in four seconds and the
/// walk is not.</para>
///
/// <para><b>What is actually driven here.</b> Not a helper and not a re-derivation: the shipping
/// <c>ForceOpenDoor</c> and <c>ForceSecretLabDoor</c> are called on a shipping page, and what they wrote is
/// read back and compared against <see cref="ExpeditionRegions.DoorPosition"/> — the same question the
/// console-removal one line away asks. That is the failure this must catch: a plate that names a real place
/// which is not the place that just appeared. It has happened on this exact ground before (#625: a beacon
/// seeded per body, a hut clamped per site, and up to 235 du between them on 21 of 34 pairs).</para>
///
/// <para><b>Proven RED</b> three ways, quoted in the PR: handing the writer the captain's own position
/// instead of the door's; taking the fan's ring back out of <c>BuildBeacons</c>; and marking the ground
/// before the door is forced instead of after.</para>
/// </summary>
public class TheGroundThatGrewSaysWhereTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly ExpeditionSiteKind[] Kinds =
    [
        ExpeditionSiteKind.MysticalRuins, ExpeditionSiteKind.CrashedHull, ExpeditionSiteKind.SealedTunnel,
    ];

    // ── LAW 1 · THE CARD NAMES THE GROUND THAT ACTUALLY APPENDED ──────────────────────────────────────

    /// <summary>
    /// #584 · Force every door of every expedition kind, from several standing spots, and hold the plate the
    /// card would show against the door the site itself resolves.
    ///
    /// <para>Swept over standing spots as well as doors because the plate is RELATIVE — bearing and range
    /// are from the captain — so a writer that quietly used a fixed origin, or the region's centre, or the
    /// captain's own square, passes at one spot and lies at every other.</para>
    /// </summary>
    [Fact]
    public void TheCard_NamesTheDoorThatJustGave_AndNotSomeOtherPlace()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        int forced = 0;
        var platesSeen = new HashSet<string>(StringComparer.Ordinal);

        foreach (ExpeditionSiteKind kind in Kinds)
        {
            string body = BodyIdOf(kind);
            foreach (ExpeditionRegions.SealedDoor door in ExpeditionRegions.AllDoors(kind, field))
            {
                foreach ((double ax, double ay) in StandingSpots(field))
                {
                    Map map = OnAnExpedition(body, ax, ay);
                    Call(map, "ForceOpenDoor", Surface(map), door.Id);

                    (double X, double Y) mouth = ExpeditionRegions.DoorPosition(kind, door.Id, field)
                        ?? throw new InvalidOperationException(
                            $"{kind}/{door.Id} has no door position — the world this law is stated in is gone.");

                    string plate = (string)Read(map, "_groundGrewWhere")!;
                    AssertPlateNames(plate, body, level: 0, mouth.X - ax, mouth.Y - ay,
                        $"{kind} · {door.Id} · standing at ({ax:F0},{ay:F0})");

                    platesSeen.Add(plate);
                    forced++;
                }
            }
        }

        Assert.True(forced > 20, $"only {forced} doors were forced — that is not a sweep of this mechanic.");
        Assert.True(platesSeen.Count > 8,
            $"the sweep produced only {platesSeen.Count} distinct plates out of {forced} forcings. A plate " +
            "that says the same thing wherever the captain stands is not a location, and this law would " +
            "pass over a writer that had stopped reading the ground.");
    }

    /// <summary>#584 · …and the concealed lab, which shares the card, names ITS own door. Same law, the other
    /// reveal — the one the owner was standing in front of when he filed this.</summary>
    [Fact]
    public void TheSecretLab_NamesTheDoorItGrewFrom()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        int opened = 0;

        foreach (string body in new[] { "luna", "titan", "miranda", "phobos" })
        {
            SecretLab.Placement placement = SecretLab.For(body, field, forcePresent: true);
            foreach ((double ax, double ay) in StandingSpots(field))
            {
                Map map = OnAnExpedition(body, ax, ay);
                object ex = Surface(map);
                ex.GetType().GetProperty("Lab")!.SetValue(ex, placement);
                ex.GetType().GetProperty("SecretLabDoorRevealed")!.SetValue(ex, true);

                Call(map, "ForceSecretLabDoor", ex);

                AssertPlateNames((string)Read(map, "_groundGrewWhere")!, body, level: 0,
                    placement.DoorX - ax, placement.DoorY - ay, $"{body} · the lab door");
                opened++;
            }
        }

        Assert.True(opened > 8, $"only {opened} lab doors were forced.");
    }

    // ── LAW 2 · THE FAN CARRIES THE RING AFTER THE REVEAL, AND NOT BEFORE ─────────────────────────────

    /// <summary>
    /// #584 · The half that makes the notification ACTIONABLE. The card is gone in four seconds; the walk to
    /// the chamber is not, so the instrument the captain is already watching has to go on pointing.
    ///
    /// <para>Both halves of "and not before" are load-bearing. A ring that was always there would be an
    /// instrument that knows about a room nobody has opened — the fog-of-war leak this game has paid for
    /// twice — and it would also make the "after" half unfalsifiable.</para>
    /// </summary>
    [Fact]
    public void TheFan_RingsTheNewGroundAfterTheDoorGives_AndNotBefore()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        int checkedDoors = 0;

        foreach (ExpeditionSiteKind kind in Kinds)
        {
            string body = BodyIdOf(kind);
            foreach (ExpeditionRegions.SealedDoor door in ExpeditionRegions.AllDoors(kind, field))
            {
                (double X, double Y) mouth = ExpeditionRegions.DoorPosition(kind, door.Id, field)!.Value;
                (double ax, double ay) = (MoonSurface.SpawnX, MoonSurface.SpawnY);

                Map map = OnAnExpedition(body, ax, ay);

                Assert.False(RingsOn(map).Any(r => PointsAt(r, ax, ay, mouth.X, mouth.Y)),
                    $"{kind} · {door.Id}: the fan is already ringing a chamber nobody has opened. The " +
                    "instrument may not know about ground the captain has not made.");

                Call(map, "ForceOpenDoor", Surface(map), door.Id);

                Assert.True(RingsOn(map).Any(r => PointsAt(r, ax, ay, mouth.X, mouth.Y)),
                    $"{kind} · {door.Id}: the door gave, the plan grew, and the fan says nothing about it. " +
                    "The card names the place once and is then gone — this ring is the whole of what makes " +
                    "that notification something a captain can act on.");
                checkedDoors++;
            }
        }

        Assert.True(checkedDoors > 6, $"only {checkedDoors} doors were swept.");
    }

    /// <summary>#584 · …and it is not the WAY HOME. The home ring is the one a captain reads when the air
    /// gets short (#563 slice 2 guards exactly that), so a new chamber wearing the home flag would be the
    /// map lying in the one place it costs a life.</summary>
    [Fact]
    public void TheNewGroundRing_IsNotTheWayHome()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        const ExpeditionSiteKind kind = ExpeditionSiteKind.SealedTunnel;
        string body = BodyIdOf(kind);
        ExpeditionRegions.SealedDoor door = ExpeditionRegions.AllDoors(kind, field)[0];

        Map map = OnAnExpedition(body, MoonSurface.SpawnX + 40, MoonSurface.SpawnY + 40);
        int homesBefore = RingsOn(map).Count(r => r.IsHome);
        Call(map, "ForceOpenDoor", Surface(map), door.Id);

        List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> after = RingsOn(map);
        Assert.Equal(homesBefore, after.Count(r => r.IsHome));
        Assert.True(homesBefore == 1, $"{homesBefore} way-home rings before the door was even forced.");
    }

    // ── LAW 3 · AND THE CARD ON SCREEN ACTUALLY SHOWS IT ──────────────────────────────────────────────

    /// <summary>#584 · The plate is composed, handed to the surface and PAINTED. Two of those three were
    /// already true of a page that renders nothing — the value is a field on <c>Map</c> and the card is its
    /// own component since #251 item 1 — so the only proof that a captain sees it is a render.</summary>
    [Fact]
    public async Task TheCardOnScreen_CarriesThePlate()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?dock=the-tilt&site=0&land=1");
        const string plate = "B4 · deepward — 61 du";

        foreach (string other in new[]
                 { "_convergenceRevealOpen", "_groundLessonOpen", "_tubeRearmOpen", "_airCardOpen" })
        {
            bench.Poke(other, false);
        }

        bench.Poke("_faceScene", null);
        bench.Poke("_groundGrewWhere", plate);
        bench.Poke("_groundGrewOpen", true);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("ground-grows-card") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "#584 · the map-just-grew card did not draw at all — this law is testing nothing.");

        Assert.True(
            card.Descendants().Any(n => string.Equals(n.Name, plate, StringComparison.Ordinal)),
            "#584 · the card is on screen and the place is not on it. The whole of this issue is a card " +
            "that told a captain the world got bigger and would not say where.");
    }

    // ── THE WORLD THESE LAWS ARE STATED IN ────────────────────────────────────────────────────────────

    /// <summary>#584 · The fifth bug class, pre-empted. Every law above is stated over a bench, and a bench
    /// that quietly built no doors, no rings and no distances would pass all of them. So: the doors are
    /// real and several, the standing spots are genuinely apart, the plate that comes out is not empty, and
    /// the ring-matching predicate can say NO — a predicate that matched everything would make "and not
    /// before" unfalsifiable, which is the exact shape of a green test that asserts nothing.</summary>
    [Fact]
    public void THE_LAW_CanTellPassFromFail()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();

        int doors = Kinds.Sum(k => ExpeditionRegions.AllDoors(k, field).Count);
        Assert.True(doors >= 6, $"only {doors} sealed doors across all three site kinds.");

        (double X, double Y)[] spots = [.. StandingSpots(field)];
        Assert.True(spots.Length >= 3, "the sweep stands in fewer than three places.");
        Assert.True(spots.Distinct().Count() == spots.Length, "two standing spots are the same square.");

        // The ring predicate is not vacuous in either direction: it says yes to the spot itself and no to a
        // place fifty du away, at the same bearing from the captain.
        var ring = (Bearing: 0.4, Range: 30.0, IsHome: false, IsLab: true, IsDead: false);
        (double px, double py) = (10 + (30 * Math.Cos(0.4)), 20 + (30 * Math.Sin(0.4)));
        Assert.True(PointsAt(ring, 10, 20, px, py));
        Assert.False(PointsAt(ring, 10, 20, px + 50, py));

        // And a forcing really does produce a plate with all three tokens in it.
        string body = BodyIdOf(ExpeditionSiteKind.MysticalRuins);
        Map map = OnAnExpedition(body, MoonSurface.SpawnX, MoonSurface.SpawnY);
        Assert.Equal("", (string)Read(map, "_groundGrewWhere")!);
        Call(map, "ForceOpenDoor", Surface(map), ExpeditionRegions.AllDoors(
            ExpeditionSiteKind.MysticalRuins, field)[0].Id);
        string plate = (string)Read(map, "_groundGrewWhere")!;
        Assert.Contains(" · ", plate, StringComparison.Ordinal);
        Assert.Contains(" — ", plate, StringComparison.Ordinal);
        Assert.EndsWith(" du", plate, StringComparison.Ordinal);
    }

    // ── the bench ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The plate says the floor, the bearing and the range of ONE spot — held against that spot's
    /// own delta, token by token, so a writer handed the wrong place reddens on the piece that moved.</summary>
    private static void AssertPlateNames(string plate, string body, int level, double dx, double dy, string what)
    {
        Assert.False(string.IsNullOrEmpty(plate), $"#584 · {what}: the ground grew and the card names no place.");
        Assert.Equal(GroundGrows.Where(body, level, dx, dy), plate);

        // …and, because the line above would also pass if BOTH the page and Core were wrong in the same
        // direction, the range is read straight back off the plate and measured.
        string tail = plate[(plate.LastIndexOf(" — ", StringComparison.Ordinal) + 3)..];
        double quoted = double.Parse(tail[..^3].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(Math.Sqrt((dx * dx) + (dy * dy)), quoted, 0);
    }

    /// <summary>Does this ring point at (<paramref name="tx"/>, <paramref name="ty"/>) from where the captain
    /// stands? Read off the picture — bearing and range resolved back to a spot — the way a player reads it,
    /// so a ring that points somewhere plausible and wrong still fails.</summary>
    private static bool PointsAt(
        (double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead) ring,
        double ax, double ay, double tx, double ty)
    {
        double px = ax + (ring.Range * Math.Cos(ring.Bearing));
        double py = ay + (ring.Range * Math.Sin(ring.Bearing));
        return Math.Abs(px - tx) < 0.05 && Math.Abs(py - ty) < 0.05;
    }

    private static List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> RingsOn(Map map) =>
        (List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)>)
        Call(map, "BuildBeacons", Surface(map));

    /// <summary>Spots a captain could be standing when a door gives — the tube mouth, mid-field, and deep
    /// beside the anchor. Three genuinely different origins, because the plate is relative to all of them.
    /// </summary>
    private static IEnumerable<(double X, double Y)> StandingSpots(SurfaceLayout.Field field)
    {
        yield return (MoonSurface.SpawnX, MoonSurface.SpawnY);
        yield return ((field.LeftX + field.RightX) / 2.0, (field.LandingBandY + field.BottomY) / 2.0);
        yield return (field.AnchorX + 18.0, field.AnchorY - 9.0);
    }

    private static string BodyIdOf(ExpeditionSiteKind kind) => kind switch
    {
        ExpeditionSiteKind.CrashedHull => $"{ExpeditionSite.BodyId}-wreck",
        ExpeditionSiteKind.SealedTunnel => $"{ExpeditionSite.BodyId}-tunnel",
        _ => $"{ExpeditionSite.BodyId}-ruins",
    };

    /// <summary>A shipping page standing on an expedition site, at one spot on its ground. The same bench
    /// <c>TheWayBackIsAlwaysOnTheFanTests</c> uses, with <c>Expedition</c> set — the door channel and the
    /// region append are expedition-only.</summary>
    private static Map OnAnExpedition(string body, double x, double y)
    {
        var map = new Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Type exType = typeof(Map).GetNestedType(
            "SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Map).GetNestedType(
            "ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("Site")!.SetValue(ex, LandingSites.For("luna")[0]);
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        exType.GetProperty("Expedition")!.SetValue(ex, true);

        // ── A PLAN OF ITS OWN, AND THIS LINE IS NOT OPTIONAL ────────────────────────────────────────
        //
        // A fresh Map starts on `DeckPlan.Ship`, which is a `{ get; } = BuildShip(null)` SINGLETON, and
        // `AppendRegion` MUTATES the plan it is called on. So a bench that drove ForceOpenDoor on a page
        // still holding the default welded an expedition chamber — and then Vantar's whole lab — onto the
        // process-wide ship deck, for every other class in the assembly.
        //
        // It was not subtle once it landed, and it was invisible until CI: 49 tests red on a Linux runner
        // and 11 on this box, reporting the ship drawing 462 marks where 364 were pinned, two identical
        // '🖥 VANTAR — FIELD LOG' consoles 0.00 du apart on `surface:luna:1`, and thirty landings that
        // could no longer be walked to a shelter. Nothing was wrong with the code under test; the bench
        // was editing the world the other suites read.
        //
        // ShipWith([]) is BuildShip run again — the same deck, a new object. One line, and this suite's
        // appends stay inside this suite.
        Set(map, "_deckPlan", DeckPlan.ShipWith([]));

        Set(map, "_surface", ex);
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        return map;
    }

    private static object Surface(Map map) => typeof(Map).GetField("_surface", Hidden)!.GetValue(map)!;

    private static void Set(object target, string field, object? value) =>
        (typeof(Map).GetField(field, Hidden)
         ?? throw new InvalidOperationException($"Map has no field {field}."))
        .SetValue(target, value);

    private static object? Read(Map map, string field) =>
        (typeof(Map).GetField(field, Hidden)
         ?? throw new InvalidOperationException($"Map has no field {field}."))
        .GetValue(map);

    private static object Call(Map map, string method, params object?[] args) =>
        (typeof(Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no {method} — the paint site has moved."))
        .Invoke(map, args)!;
}
