using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #775 · CAN YOU ACTUALLY WALK IN THROUGH THE NEW DOORS? — the A* flood, pointed at the circulation this
/// issue added rather than at the room it added them to.
///
/// <para>#585's law on new ground, and #488's idiom: a door that exists on screen and cannot be walked
/// through is the same bug as a room that cannot be reached. The geometry guards in Core prove the wall is
/// not poured across the gap; these prove a BODY gets through it, on the very deck the captain's boots
/// collide with — which is a different question, because a gap can be open in the segments and still be
/// narrower than the avatar, or open onto furniture that was laid afterwards.</para>
///
/// <para><b>The flood is BOUNDED to the door's own neighbourhood on purpose.</b> A whole-field flood would
/// find the way round through the rib corridor and report success about a door that is bricked up — which
/// is precisely #600's lesson (the audit proved you could REACH the lift, never that it was a way HOME).
/// A box a few du either side of the jamb can only be crossed by going through.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCirculationIsWalkableTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, [], 0);

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Hall Hall, DeckPlan Deck)>
        EveryHall()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.Amenity a in
                    UndergroundComplex.Build(body, level, Field).Amenities)
                {
                    if (a.Hall is { } hall)
                    {
                        yield return (body, level, hall, DeckFor(body, level));
                    }
                }
            }
        }
    }

    /// <summary>Two points, one either side of a doorway, and the little box that contains both — the whole
    /// question "is this door a way through" in three numbers.</summary>
    private static (DeckReachability.Point A, DeckReachability.Point B,
        (double, double, double, double) Bounds) AcrossTheJamb(SurfaceLayout.Doorway d, double step)
    {
        double cx = (d.X1 + d.X2) / 2.0, cy = (d.Y1 + d.Y2) / 2.0;
        bool horizontal = Math.Abs(d.Y1 - d.Y2) < 0.05;
        double ox = horizontal ? 0 : step, oy = horizontal ? step : 0;
        return (
            new DeckReachability.Point(cx - ox, cy - oy),
            new DeckReachability.Point(cx + ox, cy + oy),
            (cx - ox - 2.0, cy - oy - 2.0, cx + ox + 2.0, cy + oy + 2.0));
    }

    // ── (a) EVERY DOOR IS A WAY THROUGH ──────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY ONE OF THE HALL'S DOORS CAN BE WALKED THROUGH — both jambs standable, and the crossing made
    /// inside a box that leaves no way round.
    ///
    /// <para><b>Proven RED</b> by publishing the front doors without cutting them (the carve's
    /// <c>spineCuts.Add(...)</c> commented out — the plan draws a violet leaf, the wall stays poured):</para>
    /// <code>
    /// 34 door(s) in a hall cannot be walked through:
    ///   luna B1: the door at (34.0,-157.0) is a picture — the two sides of it are not connected.
    ///   luna B1: the door at (93.2,-157.0) is a picture — the two sides of it are not connected.
    ///   luna B13: the door at (-13.7,-150.0) is a picture — the two sides of it are not connected.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void EveryHallDoorIsAWayThrough()
    {
        var bad = new List<string>();
        int doors = 0, halls = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, DeckPlan deck) in EveryHall())
        {
            halls++;
            foreach (SurfaceLayout.Doorway d in hall.Openings)
            {
                doors++;
                (DeckReachability.Point a, DeckReachability.Point b, var bounds) =
                    AcrossTheJamb(d, DeckPlan.AvatarRadius + 1.5);

                if (!DeckReachability.Standable(a.X, a.Y, DeckPlan.AvatarRadius, deck.CollisionField)
                    || !DeckReachability.Standable(b.X, b.Y, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    bad.Add($"  {body} B{-level}: one side of the door at ({d.X1:F1},{d.Y1:F1}) is solid — "
                        + "the verdict below would mean nothing.");
                    continue;
                }
                if (!DeckReachability.CanReach(a, b, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    bad.Add($"  {body} B{-level}: the door at ({(d.X1 + d.X2) / 2:F1},"
                        + $"{(d.Y1 + d.Y2) / 2:F1}) is a picture — the two sides of it are not connected.");
                }
            }
        }

        Assert.True(halls > 15, $"only {halls} halls were flooded — this proved little.");
        Assert.True(doors > 60, $"only {doors} hall doors were walked — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} door(s) in a hall cannot be walked through:\n" + string.Join("\n", bad.Take(20)));
    }

    // ── (b) AND THE WALK THE OWNER COULD NOT MAKE ────────────────────────────────────────────────────

    /// <summary>
    /// OFF THE LIFT, ALONG THE MAIN CORRIDOR, AND IN THROUGH THE FRONT DOOR — the walk the whole issue is
    /// about, flooded end to end from the car.
    ///
    /// <para>Three legs, each asserted separately so a red run says WHICH one broke: the corridor side of
    /// the entrance is reachable from the lift; the hall side of it is; and the counter is reachable from
    /// the corridor side, which is the sentence "you can get from the main corridor into the bar".</para>
    /// </summary>
    [Fact]
    public void TheFrontDoorIsWalkedFromTheLift()
    {
        var bad = new List<string>();
        int halls = 0;

        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        var spawn = new DeckReachability.Point(sx, sy);
        (double, double, double, double) whole = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);
        (_, double shaftY) = UndergroundComplex.ShaftAt(Field);

        foreach ((string body, int level, UndergroundComplex.Hall hall, DeckPlan deck) in EveryHall())
        {
            SurfaceLayout.Doorway? front = hall.Openings
                .Cast<SurfaceLayout.Doorway?>()
                .FirstOrDefault(d => d is { } o && Math.Abs(o.Y1 - o.Y2) < 0.05);
            if (front is not { } entrance)
            {
                bad.Add($"  {body} B{-level}: no door on the main corridor at all.");
                continue;
            }
            halls++;

            double cx = (entrance.X1 + entrance.X2) / 2.0;
            double step = DeckPlan.AvatarRadius + 1.5;
            var outside = new DeckReachability.Point(
                cx, entrance.Y1 + (entrance.Y1 < shaftY ? step : -step));
            var inside = new DeckReachability.Point(
                cx, entrance.Y1 + (entrance.Y1 < shaftY ? -step : step));

            if (!DeckReachability.Standable(spawn.X, spawn.Y, DeckPlan.AvatarRadius, deck.CollisionField))
            {
                bad.Add($"  {body} B{-level}: the walk starts inside a wall.");
                continue;
            }
            if (!DeckReachability.CanReach(
                spawn, outside, deck.CollisionField, DeckPlan.AvatarRadius, whole))
            {
                bad.Add($"  {body} B{-level}: the front door cannot be reached from the lift at all.");
                continue;
            }
            if (!DeckReachability.CanReach(
                spawn, inside, deck.CollisionField, DeckPlan.AvatarRadius, whole))
            {
                bad.Add($"  {body} B{-level}: you can stand at the front door and not get in.");
            }
            if (!DeckReachability.CanReach(
                outside, new DeckReachability.Point(hall.BoardX, hall.BoardY),
                deck.CollisionField, DeckPlan.AvatarRadius, whole))
            {
                bad.Add($"  {body} B{-level}: the cork board cannot be reached from the corridor.");
            }
        }

        Assert.True(halls > 15, $"only {halls} halls were walked into — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} hall(s) cannot be entered off the main corridor:\n"
            + string.Join("\n", bad.Take(20)));
    }

    // ── (c) THE FREIGHT FIXTURE: DRAWN AND COLLIDABLE, AND THE TWO AGREE ─────────────────────────────

    /// <summary>
    /// THE GOODS HOIST IS SOLID AND ITS PLATE IS NOT — the drawn-versus-simulated guard for a fixture whose
    /// whole content is that it is there and you may not use it.
    ///
    /// <para>Both halves are asserted because either alone is the bug: a car the captain can walk INTO is a
    /// freight lift drawn as a box and simulated as air, and a plate the captain cannot walk UP TO is a
    /// refusal nobody is ever told (#757's absence, which is the one refusal a player cannot read).</para>
    ///
    /// <para><b>Proven RED</b> by publishing the car and hanging no shutter in it — the one
    /// <c>locked.Add(...)</c> in <c>Build</c> removed, so the counter's line simply has a twelve-du hole in
    /// it and the freight car becomes part of the room:</para>
    /// <code>
    /// 44 goods hoist(s) do not collide the way they are drawn:
    ///   luna B1: the car at (8.5,-219.0) is thin air — the captain walks into the freight lift.
    ///   luna B1: nothing on the deck carries the hoist's plate.
    ///   luna B13: the car at (-18.5,-88.0) is thin air — the captain walks into the freight lift.
    ///   …
    /// </code>
    ///
    /// <para>The car's other three walls are proved by Core's own hoist guard rather than here, and
    /// deliberately: knock the divider out and this stays GREEN, because the servery band behind the
    /// counter is sealed from the hall anyway. A reachability flood cannot tell a two-compartment sealed
    /// band from a one-compartment one — <c>TheGoodsHoistIsBuiltAndItRefusesInWords</c> can, because it
    /// asks whether the segment the record names was ever poured.</para>
    /// </summary>
    [Fact]
    public void TheGoodsHoistIsSolidAndItsPlateIsNot()
    {
        var bad = new List<string>();
        int hoists = 0;

        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        var spawn = new DeckReachability.Point(sx, sy);
        (double, double, double, double) whole = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

        foreach ((string body, int level, UndergroundComplex.Hall hall, DeckPlan deck) in EveryHall())
        {
            if (hall.Freight is not { } hoist)
            {
                bad.Add($"  {body} B{-level}: no goods hoist to check.");
                continue;
            }
            hoists++;

            double cx = (hoist.X0 + hoist.X1) / 2.0, cy = (hoist.Y0 + hoist.Y1) / 2.0;
            if (DeckReachability.CanReach(
                spawn, new DeckReachability.Point(cx, cy), deck.CollisionField,
                DeckPlan.AvatarRadius, whole))
            {
                bad.Add($"  {body} B{-level}: the car at ({cx:F1},{cy:F1}) is thin air — the captain walks "
                    + "into the freight lift.");
            }

            if (!DeckReachability.Standable(
                hoist.PlateX, hoist.PlateY, DeckPlan.AvatarRadius, deck.CollisionField))
            {
                bad.Add($"  {body} B{-level}: the plate spot at ({hoist.PlateX:F1},{hoist.PlateY:F1}) is "
                    + "inside a wall.");
            }
            else if (!DeckReachability.CanReach(
                spawn, new DeckReachability.Point(hoist.PlateX, hoist.PlateY),
                deck.CollisionField, DeckPlan.AvatarRadius, whole))
            {
                bad.Add($"  {body} B{-level}: the refusal cannot be walked up to and therefore is never "
                    + "told.");
            }

            // …and the deck actually says the words. Drawn, labelled, collidable — all three, in one place.
            if (!deck.Consoles.Any(c => c.Label.Contains(
                    UndergroundComplex.FreightPlate, StringComparison.Ordinal)))
            {
                bad.Add($"  {body} B{-level}: nothing on the deck carries the hoist's plate.");
            }
            if (!deck.RoomLabels.Any(l => l.Text == UndergroundComplex.FreightSign))
            {
                bad.Add($"  {body} B{-level}: the hoist is not labelled on the deck.");
            }
        }

        Assert.True(hoists > 15, $"only {hoists} goods hoists were flooded — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} goods hoist(s) do not collide the way they are drawn:\n"
            + string.Join("\n", bad.Take(20)));
    }

    // ---- (d) THE PARK IS A WAY THROUGH -------------------------------------------------------------

    /// <summary>
    /// EVERY GATE INTO THE PARK IS A WAY THROUGH, AND THE GREEN IS ON A ROUTE BETWEEN TWO PLACES.
    ///
    /// <para>Owner, 2026-08-09: <i>"let's have multiple doors to the park... it is a kind of place people
    /// like to walk through on their way."</i> Core's own guard says the gates exist, are published and
    /// have nothing poured across them; this says a BODY gets through each of them, and then walks the one
    /// sentence the feature is actually about: <b>in one gate and out of another</b>, with the whole flood
    /// bounded to the park and its two doorways so the route cannot go back up the corridor and round.</para>
    ///
    /// <para><b>Proven RED</b> by cutting the park's near wall for the hall's own rib ONLY while still
    /// publishing every gate — corridors that run all the way to the green and stop at poured concrete,
    /// with a violet leaf drawn across the wall:</para>
    /// <code>
    /// 37 park gate(s) are not a way through:
    ///   luna B1: the gate at (-103.4,-221.5) is a picture - the two sides of it are not connected.
    ///   luna B1: the gate at (-54.2,-221.5) is a picture - the two sides of it are not connected.
    ///   luna B1: the gate at (-138.5,-221.5) is a picture - the two sides of it are not connected.
    ///   luna B1: you cannot cross the park - in at -5.0 and out at -138.5 is not a route.
    ///   ...
    /// </code>
    /// </summary>
    [Fact]
    public void EveryParkGateIsAWayThroughAndTheGreenIsOnTheRoute()
    {
        var bad = new List<string>();
        int parks = 0, gates = 0, crossings = 0;

        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        var spawn = new DeckReachability.Point(sx, sy);
        (double, double, double, double) whole =
            (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.Build(body, level, Field).Park is not { } park)
                {
                    continue;
                }
                parks++;
                DeckPlan deck = DeckFor(body, level);

                foreach (SurfaceLayout.Doorway g in park.Ways)
                {
                    gates++;
                    (DeckReachability.Point a, DeckReachability.Point b, var box) =
                        AcrossTheJamb(g, DeckPlan.AvatarRadius + 1.5);

                    if (!DeckReachability.Standable(a.X, a.Y, DeckPlan.AvatarRadius, deck.CollisionField)
                        || !DeckReachability.Standable(b.X, b.Y, DeckPlan.AvatarRadius, deck.CollisionField))
                    {
                        bad.Add($"  {body} B{-level}: one side of the gate at ({g.X1:F1},{g.Y1:F1}) is "
                            + "solid - the verdict below would mean nothing.");
                        continue;
                    }
                    if (!DeckReachability.CanReach(a, b, deck.CollisionField, DeckPlan.AvatarRadius, box))
                    {
                        bad.Add($"  {body} B{-level}: the gate at ({(g.X1 + g.X2) / 2:F1},{g.Y1:F1}) is a "
                            + "picture - the two sides of it are not connected.");
                        continue;
                    }
                    if (!DeckReachability.CanReach(
                        spawn, b, deck.CollisionField, DeckPlan.AvatarRadius, whole))
                    {
                        bad.Add($"  {body} B{-level}: the gate at ({(g.X1 + g.X2) / 2:F1},{g.Y1:F1}) cannot "
                            + "be reached from the lift.");
                    }
                }

                // ...AND THE CROSSING ITSELF. Corridor side of the first gate to corridor side of the last,
                // inside a box that contains the park and its own two doorways and NOTHING of the spine -
                // so the only route between them is the gravel. That is the owner's sentence, walked.
                //
                // #813 · WHICH WAY "OUTSIDE" IS, ASKED OF THE GATE. This used to push both points along y
                // by a fixed sign, which was the whole truth while every gate was a gap in the park's near
                // wall. The block is crossed from all four sides now and its west and east gates are
                // VERTICAL: a y-offset on one of those lands ON the gate line rather than outside it, and
                // the crossing would be measured from a point in the doorway to a point in the doorway.
                // It happened to pass. A guard that passes for the wrong reason is one bad day from
                // passing when it should not.
                if (park.Ways.Count > 1)
                {
                    SurfaceLayout.Doorway first = park.Ways[0], last = park.Ways[^1];
                    double midX = (park.X0 + park.X1) / 2.0, midY = (park.Y0 + park.Y1) / 2.0;
                    DeckReachability.Point Outside(SurfaceLayout.Doorway g)
                    {
                        double gx = (g.X1 + g.X2) / 2.0, gy = (g.Y1 + g.Y2) / 2.0;
                        return Math.Abs(g.Y1 - g.Y2) < 0.05
                            ? new DeckReachability.Point(gx, gy + (Math.Sign(gy - midY) * 2.2))
                            : new DeckReachability.Point(gx + (Math.Sign(gx - midX) * 2.2), gy);
                    }

                    DeckReachability.Point from = Outside(first), to = Outside(last);
                    (double, double, double, double) green = (
                        Math.Min(park.X0, Math.Min(from.X, to.X)) - 4,
                        Math.Min(park.Y0, Math.Min(from.Y, to.Y)) - 4,
                        Math.Max(park.X1, Math.Max(from.X, to.X)) + 4,
                        Math.Max(park.Y1, Math.Max(from.Y, to.Y)) + 4);

                    crossings++;
                    if (!DeckReachability.CanReach(
                        from, to, deck.CollisionField, DeckPlan.AvatarRadius, green))
                    {
                        bad.Add($"  {body} B{-level}: you cannot cross the park - in at "
                            + $"{(first.X1 + first.X2) / 2:F1} and out at {(last.X1 + last.X2) / 2:F1} is "
                            + "not a route.");
                    }
                }
            }
        }

        Assert.True(bad.Count == 0,
            $"{bad.Count} park gate(s) are not a way through:\n" + string.Join("\n", bad.Take(20)));
        Assert.True(parks > 8, $"only {parks} parks were flooded - this proved little.");
        Assert.True(gates > 20, $"only {gates} park gates were walked - this proved little.");
        Assert.True(crossings > 8, $"only {crossings} crossings were walked - this proved little.");
    }
}
