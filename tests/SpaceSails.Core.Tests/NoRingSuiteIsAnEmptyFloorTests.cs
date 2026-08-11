using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #817 · AN OFFICE WITHOUT FURNITURE IS NOT AN OFFICE — and a landscape office with one door is not a
/// landscape office either.
///
/// <para>Owner, live in a park-view suite on the evening of 2026-08-11, standing on bare deck a few paces
/// off the glass:</para>
///
/// <list type="bullet">
/// <item><i>"It really needs tables … the cubicles etc chairs maybe tables etc. It is way too empty"</i></item>
/// <item><i>"I mean in office people sit down … what on the floor there?"</i></item>
/// <item><i>"Oh just one door in a landscape office?"</i> → <i>"bigger spaces must have much more doors"</i></item>
/// <item><i>"Such a big premium office would have little kitchen and couple toilets also"</i> and
/// <i>"maybe some privacy cabinets also"</i></item>
/// </list>
///
/// <para>Every guard below walks the REAL generator over the REAL field and is stated against a PUBLISHED
/// list — the ring, its doors, its fittings, its seats — never against a coordinate typed into the test. A
/// law about a list nobody keeps is a law nobody can fail, and a law measured off a number typed here is the
/// house's fifth bug class.</para>
///
/// <para>The clearance guards are the sharp ones. This project's most expensive bugs have all been a room
/// that was correct on the plan and wrong to walk, so what is measured is not "is the desk inside the room"
/// but "is the desk out of the doorway, off the walls and off the square the audit stands on".</para>
/// </summary>
public sealed class NoRingSuiteIsAnEmptyFloorTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every floor that has a block on it, with the park the ring was carved round.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park,
        UndergroundComplex.FloorPlan Floor)> EveryBlock()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || !UndergroundComplex.HasParkBlock(body, level))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            Assert.True(floor.Park is not null, $"{body} B{-level}: the block has no park in the middle.");
            yield return (body, level, floor.Park!.Value, floor);
        }
    }

    /// <summary>Is this one of the rooms the owner was standing in — a suite that looks at the green?</summary>
    private static bool IsViewSuite(in UndergroundComplex.RingRoom room) =>
        room.HasView && room.Side != UndergroundComplex.RingSide.Far;

    /// <summary>How far a point is from a segment. The one measurement every clearance below is stated in,
    /// written once so a door, a wall and a desk are all measured the same way.</summary>
    private static double ToSegment(
        double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double len2 = (dx * dx) + (dy * dy);
        double t = len2 < 1e-9 ? 0.0 : Math.Clamp((((px - x1) * dx) + ((py - y1) * dy)) / len2, 0.0, 1.0);
        double cx = x1 + (t * dx), cy = y1 + (t * dy);
        return Math.Sqrt(((px - cx) * (px - cx)) + ((py - cy) * (py - cy)));
    }

    /// <summary>…and how far a BOX is from a segment, sampled along the segment. A rectangle-to-segment
    /// distance in closed form is three cases and a sign error; this walks the segment at a pitch far finer
    /// than any clearance being asserted, which is honest about what it proves.</summary>
    private static double BoxToSegment(
        in RingOffice.Fixture box, double x1, double y1, double x2, double y2)
    {
        double worst = double.MaxValue;
        const int Samples = 48;
        for (int i = 0; i <= Samples; i++)
        {
            double t = (double)i / Samples;
            double px = x1 + ((x2 - x1) * t), py = y1 + ((y2 - y1) * t);
            double dx = Math.Max(Math.Max(box.X0 - px, px - box.X1), 0.0);
            double dy = Math.Max(Math.Max(box.Y0 - py, py - box.Y1), 0.0);
            worst = Math.Min(worst, Math.Sqrt((dx * dx) + (dy * dy)));
        }
        return worst;
    }

    // ── (a) NO FLOOR IS BARE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY VIEW SUITE HAS SOMETHING TO WORK AT AND SOMEWHERE TO SIT, and every OTHER ring room has at
    /// least one piece of furniture in it.
    ///
    /// <para>The owner's complaint, as arithmetic. A suite that rents on a garden aspect and shows a captain
    /// bare deck is the finding; a corner office or a potting shed with a bench and some shelving in it is
    /// the plain version he explicitly allowed (<i>cheap is fine, empty is not</i>).</para>
    ///
    /// <para><b>Proven RED</b> by returning <c>RingOffice.Furnishing.Empty</c> from the top of
    /// <c>RingOffice.Fit</c>:</para>
    /// <code>
    /// 728 ring room(s) are bare deck:
    ///   luna B1: ring room 1 (NEAR) — CONSENT FILES — has nothing on its floor at all.
    ///   luna B1: ring room 2 (NEAR) — SENIOR ROTA · GREEN SIDE — has nothing on its floor at all.
    ///   luna B1: ring room 3 (NEAR) — PRIVILEGED RECORDS · READING ROOM — has nothing on its floor at all.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryRingRoomIsFurnishedAndEveryViewSuiteSeatsMoreThanOne()
    {
        var bare = new List<string>();
        int blocks = 0, suites = 0, rooms = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryBlock())
        {
            blocks++;
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                rooms++;
                string what = $"{body} B{-level}: ring room {room.Number} "
                    + $"({room.Side.ToString().ToUpperInvariant()}) — {room.Plate}";

                // Counted BEFORE the emptiness check, so that stripping the furniture out makes this guard
                // report the FURNITURE rather than reporting that it saw no suites — an anti-vacuity clause
                // that fires first turns a red into a shrug.
                bool suite = IsViewSuite(in room);
                if (suite)
                {
                    suites++;
                }

                if (room.Furniture.Count == 0)
                {
                    bare.Add($"  {what} — has nothing on its floor at all.");
                    continue;
                }

                if (!suite)
                {
                    continue;
                }

                // A desk, a table or a counter — the thing somebody works AT. Off the published kind and
                // never off the plate's wording, so a room renamed tomorrow still has to have one.
                int worktops = room.Furniture.Count(f => f.Kind is RingOffice.Fitting.DeskBank
                    or RingOffice.Fitting.Table or RingOffice.Fitting.Counter);

                // …and #817's second ruling: you can sit down in MORE THAN ONE PLACE in an office. One
                // chair in a landscape suite is a prop.
                if (worktops == 0 || room.Seats.Count < RingOffice.SeatsPerViewSuite)
                {
                    bare.Add($"  {what} — has {worktops} desk(s) and {room.Seats.Count} chair(s).");
                }
            }
        }

        Assert.True(blocks > 40, $"only {blocks} blocks were measured — this proved little.");
        Assert.True(rooms > 500, $"only {rooms} ring rooms were checked — this proved little.");
        Assert.True(suites > 150,
            $"only {suites} of them were suites with the view — the rooms the owner was standing in are "
            + "barely being read at all, and a guard that only ever sees the back of house is the one that "
            + "would miss them.");
        Assert.True(bare.Count == 0,
            $"{bare.Count} ring room(s) are bare deck:\n" + string.Join("\n", bare.Take(20)));
    }

    /// <summary>
    /// THE DRESSING MATCHES THE PLATE. A NEGOTIATION ROOM reads as one long table, a READING ROOM as
    /// shelving, a RECEPTION as a counter — because the plate beside the door is the only thing that says
    /// what a room is for, and a room whose plate and whose floor disagree is the sim and the sentence
    /// coming apart.
    ///
    /// <para><b>Proven RED</b> by returning <c>Dressing.Cubicles</c> from every branch of
    /// <c>RingOffice.DressingFor</c>:</para>
    /// <code>
    /// 143 room(s) are furnished as something else:
    ///   luna B1: PRIVILEGED RECORDS · READING ROOM has no Shelving in it.
    ///   luna B1: NEGOTIATION ROOM · BOOK AT THE COUNTER has no Table in it.
    ///   phobos B1: RECEPTION · APPOINTMENTS HELD has no Counter in it.
    /// </code>
    /// </summary>
    [Fact]
    public void ThePlateAndTheFloorAgreeAboutWhatTheRoomIsFor()
    {
        var wrong = new List<string>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (!IsViewSuite(in room))
                {
                    continue;
                }

                RingOffice.Fitting? wanted = room.Plate switch
                {
                    var p when p.Contains("NEGOTIATION", StringComparison.Ordinal) =>
                        RingOffice.Fitting.Table,
                    var p when p.Contains("READING ROOM", StringComparison.Ordinal) =>
                        RingOffice.Fitting.Shelving,
                    var p when p.Contains("RECEPTION", StringComparison.Ordinal) =>
                        RingOffice.Fitting.Counter,
                    _ => null,
                };
                if (wanted is not { } kind)
                {
                    continue;
                }

                seen[room.Plate] = seen.GetValueOrDefault(room.Plate) + 1;
                if (!room.Furniture.Any(f => f.Kind == kind))
                {
                    wrong.Add($"  {body} B{-level}: {room.Plate} has no {kind} in it.");
                }
            }
        }

        Assert.True(seen.Count == 3,
            $"only {seen.Count} of the three plated dressings were met at all ({string.Join(", ", seen.Keys)})"
            + " — the branches this guard exists to check were never taken.");
        foreach ((string plate, int count) in seen)
        {
            Assert.True(count >= 20, $"only {count} rooms plated {plate} — this proved little about it.");
        }
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} room(s) are furnished as something else:\n" + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>
    /// THE BIG SUITES GET THE SERVICE TIER — a kitchenette, two WC cubicles and the privacy booths — and the
    /// small ones do not. Owner: <i>"Such a big premium office would have little kitchen and couple toilets
    /// also"</i> · <i>"maybe some privacy cabinets also"</i>, and the operative word in the first is
    /// <b>big</b>.
    ///
    /// <para>#821 · The cubicles' doors are REAL published doorways rather than decorative gaps, because the
    /// lock-from-inside is coming and it has to land on a door the building already knows about.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>IsBigSuite</c> branch out of <c>RingOffice.Fit</c>:</para>
    /// <code>
    /// 312 suite(s) are missing their service tier:
    ///   luna B1: ring room 2 (48.8 du of frontage) has no kitchenette.
    ///   luna B1: ring room 2 has 0 WC cubicle(s) and the owner asked for 2.
    ///   luna B1: ring room 2 has no privacy booth.
    ///   luna B1: ring room 3 (42.2 du of frontage) has no kitchenette.
    /// </code>
    /// </summary>
    [Fact]
    public void ABigSuiteHasAKitchenetteTwoWCsAndItsBooths()
    {
        var wrong = new List<string>();
        int big = 0, small = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (!IsViewSuite(in room))
                {
                    continue;
                }

                int kitchens = room.Furniture.Count(f => f.Kind == RingOffice.Fitting.Kitchenette);
                int cubicles = room.Furniture.Count(f => f.Kind == RingOffice.Fitting.Cubicle);
                int booths = room.Furniture.Count(f => f.Kind == RingOffice.Fitting.Booth);

                if (!RingOffice.IsBigSuite(in room))
                {
                    small++;
                    if (kitchens + cubicles + booths > 0)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number} is "
                            + $"{RingOffice.FrontageOf(room):F1} du wide — under the "
                            + $"{RingOffice.BigSuiteDu:F0} du gate — and has the service tier anyway.");
                    }
                    continue;
                }

                big++;
                if (kitchens == 0)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} "
                        + $"({RingOffice.FrontageOf(room):F1} du of frontage) has no kitchenette.");
                }
                if (cubicles != RingOffice.Cubicles)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} has {cubicles} WC cubicle(s) "
                        + $"and the owner asked for {RingOffice.Cubicles}.");
                }
                if (booths == 0)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} has no privacy booth.");
                }

                // #821 · One published door per cubicle, in the floor's own list.
                int leaves = 0;
                foreach (RingOffice.Fixture cell in room.Furniture)
                {
                    if (cell.Kind != RingOffice.Fitting.Cubicle)
                    {
                        continue;
                    }
                    foreach (SurfaceLayout.Doorway d in floor.Doorways)
                    {
                        double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                        if (mx >= cell.X0 - 0.01 && mx <= cell.X1 + 0.01
                            && my >= cell.Y0 - 0.01 && my <= cell.Y1 + 0.01)
                        {
                            leaves++;
                        }
                    }
                }
                if (leaves != cubicles)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} has {cubicles} cubicle(s) and "
                        + $"{leaves} of them are doors the floor published. A gap is not a door and #821's "
                        + "lock has nothing to land on.");
                }

                // …and a booth is a seat. A phone box with nowhere to sit is a cupboard.
                foreach (RingOffice.Fixture booth in room.Furniture)
                {
                    if (booth.Kind != RingOffice.Fitting.Booth)
                    {
                        continue;
                    }
                    if (!room.Seats.Any(c => c.X >= booth.X0 && c.X <= booth.X1
                        && c.Y >= booth.Y0 && c.Y <= booth.Y1))
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number}'s privacy booth has no "
                            + "chair in it.");
                    }
                }
            }
        }

        Assert.True(big >= 80, $"only {big} big suites were met — the tier under test barely ran.");
        Assert.True(small >= 150,
            $"only {small} suites were under the size gate — the half of this guard that proves the gate "
            + "BITES never ran.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} suite(s) are missing their service tier:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (b) AND IT IS ALL OUT OF THE WAY ──────────────────────────────────────────────────────────────

    /// <summary>
    /// NOTHING IS LAID IN A DOORWAY, IN A WALL, OR ON THE SQUARE THE AUDIT STANDS ON.
    ///
    /// <para>Three clearances, one sweep, and every one of them measured against a published segment rather
    /// than against a number this file believes about the geometry: every fitting and every chair keeps
    /// <see cref="RingOffice.DoorClearDu"/> off every doorway of its own room (street doors, the gate onto
    /// the green, and the cubicles' own leaves excepted — a cubicle's door is IN its own box), stays inside
    /// the room that owns it, and stays <see cref="RingOffice.RoomCentreClearDu"/> off the room's centre,
    /// which is the square <c>TheRingIsWalkableTests</c> stands a body on to say the room was reached.</para>
    ///
    /// <para><b>Proven RED</b> by dropping <c>RingOffice.StreetClearDu</c> and <c>GlassClearDu</c> to 0
    /// (<c>double vA = 0, vB = frame.Depth;</c> in <c>Fit</c>):</para>
    /// <code>
    /// 3433 fitting(s) are in somebody's way:
    ///   luna B1: room 1's Shelving is 0.0 du from a doorway and the law wants 4.0.
    ///   luna B1: room 1's Bench is 0.0 du from a doorway and the law wants 4.0.
    ///   luna B1: room 1's chair 0 is 2.4 du from a doorway and the law wants 4.0.
    ///   luna B1: room 2's Kitchenette is 0.0 du from a doorway and the law wants 4.0.
    ///   luna B1: room 2's DeskBank is 3.0 du from a doorway and the law wants 4.0.
    /// </code>
    /// </summary>
    [Fact]
    public void NoFittingStandsInADoorwayOrOnTheRoomsOwnCentre()
    {
        var wrong = new List<string>();
        int fittings = 0, chairs = 0, doors = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                // Every way in and out of THIS room, off its own published lists.
                var ways = new List<SurfaceLayout.Doorway>(room.Doors);
                if (room.Gate is { } onto)
                {
                    ways.Add(onto);
                }
                doors += ways.Count;

                foreach (RingOffice.Fixture f in room.Furniture)
                {
                    fittings++;

                    // A cubicle and a booth are ROOMS with a doorway in their own wall — the clearance law
                    // is about furniture standing in front of a door, and a cell's door is part of it.
                    bool cell = f.Kind is RingOffice.Fitting.Cubicle or RingOffice.Fitting.Booth;

                    if (f.X0 < room.X0 - 0.01 || f.X1 > room.X1 + 0.01
                        || f.Y0 < room.Y0 - 0.01 || f.Y1 > room.Y1 + 0.01)
                    {
                        wrong.Add($"  {body} B{-level}: room {room.Number}'s {f.Kind} "
                            + $"({f.X0:F1},{f.Y0:F1})-({f.X1:F1},{f.Y1:F1}) is not inside the room it "
                            + $"furnishes ({room.X0:F1},{room.Y0:F1})-({room.X1:F1},{room.Y1:F1}).");
                    }

                    if (!cell)
                    {
                        foreach (SurfaceLayout.Doorway d in ways)
                        {
                            double clear = BoxToSegment(f, d.X1, d.Y1, d.X2, d.Y2);
                            if (clear < RingOffice.DoorClearDu - 0.01)
                            {
                                wrong.Add($"  {body} B{-level}: room {room.Number}'s {f.Kind} is "
                                    + $"{clear:F1} du from a doorway and the law wants "
                                    + $"{RingOffice.DoorClearDu:F1}.");
                            }
                        }
                    }

                    double centreDu = BoxToSegment(f, room.X, room.Y, room.X, room.Y);
                    if (centreDu < RingOffice.RoomCentreClearDu - 0.01)
                    {
                        wrong.Add($"  {body} B{-level}: room {room.Number}'s {f.Kind} is {centreDu:F1} du "
                            + "from the room's own centre — the square the ring's A* audit stands a body "
                            + "on to say the room was reached at all.");
                    }
                }

                foreach (RingOffice.Chair c in room.Seats)
                {
                    chairs++;
                    if (c.X < room.X0 || c.X > room.X1 || c.Y < room.Y0 || c.Y > room.Y1)
                    {
                        wrong.Add($"  {body} B{-level}: room {room.Number}'s chair {c.Index} at "
                            + $"({c.X:F1},{c.Y:F1}) is outside the room it is in.");
                    }

                    // Off the room's own four walls. A chair drawn on top of a wall is a chair you sit in
                    // by standing in the concrete.
                    foreach ((double x1, double y1, double x2, double y2) in
                        (ValueTuple<double, double, double, double>[])
                        [
                            (room.X0, room.Y0, room.X1, room.Y0),
                            (room.X0, room.Y1, room.X1, room.Y1),
                            (room.X0, room.Y0, room.X0, room.Y1),
                            (room.X1, room.Y0, room.X1, room.Y1),
                        ])
                    {
                        double off = ToSegment(c.X, c.Y, x1, y1, x2, y2);
                        if (off < RingOffice.PierClearDu - 0.01)
                        {
                            wrong.Add($"  {body} B{-level}: room {room.Number}'s chair {c.Index} is "
                                + $"{off:F1} du off a wall of its own room.");
                        }
                    }

                    foreach (SurfaceLayout.Doorway d in ways)
                    {
                        double clear = ToSegment(c.X, c.Y, d.X1, d.Y1, d.X2, d.Y2);
                        if (clear < RingOffice.DoorClearDu - 0.01)
                        {
                            wrong.Add($"  {body} B{-level}: room {room.Number}'s chair {c.Index} is "
                                + $"{clear:F1} du from a doorway and the law wants "
                                + $"{RingOffice.DoorClearDu:F1}.");
                        }
                    }

                    // …and it is a unit facing rather than a pair of numbers that happen to be nonzero.
                    double mag = Math.Sqrt((c.FaceX * c.FaceX) + (c.FaceY * c.FaceY));
                    if (Math.Abs(mag - 1.0) > 0.001)
                    {
                        wrong.Add($"  {body} B{-level}: room {room.Number}'s chair {c.Index} faces "
                            + $"({c.FaceX:F2},{c.FaceY:F2}), which is not a direction.");
                    }
                }
            }
        }

        Assert.True(fittings > 600, $"only {fittings} fittings were measured — this proved little.");
        Assert.True(chairs > 600, $"only {chairs} chairs were measured — this proved little.");
        Assert.True(doors > 1000, $"only {doors} doorways were measured against — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} fitting(s) are in somebody's way:\n" + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>
    /// #820 · SITTING SNAPS THE CAPTAIN ONTO THE SEAT, and the seat is somewhere a body can be.
    ///
    /// <para>The client's press puts the avatar on <c>chair.X, chair.Y</c> and its stand-up puts them on
    /// <see cref="RingOffice.Chair.StandAt"/>. So both have to be real floor: the same published pair, and
    /// clear of every solid segment the room's own furniture laid. A snap onto a coordinate inside a desk
    /// would trap the dot, which is the exact failure the ruling was issued about.</para>
    ///
    /// <para><b>Proven RED</b> by seating the desk chairs ON the worktop
    /// (<c>double chairV = vRow;</c> in <c>RingOffice.Banks</c>):</para>
    /// <code>
    /// 945 seat(s) cannot be sat on:
    ///   luna B1: room 2's chair 2 is 0.0 du inside its own room's DeskBank — the snap would trap the dot.
    ///   luna B1: room 2's chair 3 is 0.0 du inside its own room's DeskBank — the snap would trap the dot.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryPublishedSeatIsFloorAndStandingUpPutsYouBackOnIt()
    {
        var wrong = new List<string>();
        int seats = 0;

        // A body's radius, as the client knows it (DeckPlan.AvatarRadius). Core carries no client geometry,
        // so this is stated as what it is FOR rather than imported: the half-width of the thing being set
        // down on the seat.
        const double BodyRadiusDu = 0.7;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                foreach (RingOffice.Chair c in room.Seats)
                {
                    seats++;

                    (double sx, double sy) = c.StandAt;
                    if (Math.Abs(sx - c.X) > 0.001 || Math.Abs(sy - c.Y) > 0.001)
                    {
                        wrong.Add($"  {body} B{-level}: room {room.Number}'s chair {c.Index} seats you at "
                            + $"({c.X:F1},{c.Y:F1}) and stands you up at ({sx:F1},{sy:F1}) — two answers to "
                            + "one question.");
                    }

                    foreach (RingOffice.Fixture f in room.Furniture)
                    {
                        // A booth's chair is INSIDE its booth, which is the whole point of a booth.
                        if (f.Kind == RingOffice.Fitting.Booth)
                        {
                            continue;
                        }

                        double dx = Math.Max(Math.Max(f.X0 - c.X, c.X - f.X1), 0.0);
                        double dy = Math.Max(Math.Max(f.Y0 - c.Y, c.Y - f.Y1), 0.0);
                        double gap = Math.Sqrt((dx * dx) + (dy * dy));
                        if (gap < BodyRadiusDu)
                        {
                            wrong.Add($"  {body} B{-level}: room {room.Number}'s chair {c.Index} is "
                                + $"{gap:F1} du inside its own room's {f.Kind} — the snap would trap the "
                                + "dot.");
                        }
                    }
                }
            }
        }

        Assert.True(seats > 600, $"only {seats} seats were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} seat(s) cannot be sat on:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (c) THE DOORS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE DOOR COUNT SCALES WITH THE FRONTAGE, AND NOTHING HAS FEWER THAN TWO WAYS OUT.
    ///
    /// <para>Owner: <i>"Oh just one door in a landscape office?"</i> → <i>"bigger spaces must have much more
    /// doors"</i>, and then #822's standing law: <i>"no space may have only one door except bedroom-small
    /// rooms."</i> Both are measured off the room's own published lists, and the ratio is asked of Core
    /// (<see cref="UndergroundComplex.DoorsForFrontage"/>) rather than restated here — a test that mirrors a
    /// constant is not testing the thing that ships.</para>
    ///
    /// <para><b>Proven RED</b> by putting the single door back (<c>int leaves = 1;</c> in
    /// <c>RingBox</c>):</para>
    /// <code>
    /// 1040 ring room(s) are under-doored:
    ///   luna B1: ring room 1 (20.0 du) has 1 street door(s) and the law wants 2.
    ///   luna B1: ring room 1 has 1 way(s) out altogether, and the fire code wants 2.
    ///   luna B1: ring room 2 (48.8 du) has 1 street door(s) and the law wants 3.
    ///   luna B1: ring room 2 is 48.8 du wide and has 1 door.
    /// </code>
    /// </summary>
    [Fact]
    public void DoorCountScalesWithFrontageAndTheFireCodeHolds()
    {
        var wrong = new List<string>();
        int rooms = 0, wide = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                rooms++;
                double frontage = RingOffice.FrontageOf(room);
                int want = UndergroundComplex.DoorsForFrontage(frontage, room.Gate is not null);

                if (room.Doors.Count != want)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} ({frontage:F1} du) has "
                        + $"{room.Doors.Count} street door(s) and the law wants {want}.");
                }

                // The owner's own case, said in the shape the issue is written in: a room wide enough to be
                // called a landscape office is never served by one leaf. Stated on the FRONTAGE alone, so
                // the gate a suite has onto the green cannot buy its way out of this one — a 48 du office
                // with one street door is what he was standing in when he filed it.
                if (frontage >= 2 * UndergroundComplex.RingStreetFaceDuPerDoor)
                {
                    wide++;
                    if (room.Doors.Count < 2)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number} is {frontage:F1} du wide "
                            + $"and has {room.Doors.Count} door.");
                    }
                }

                // #822 · …and every one of them has a second way out, counting the gate onto the green.
                if (room.Exits < UndergroundComplex.FireCodeMinExits)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} has {room.Exits} way(s) out "
                        + $"altogether, and the fire code wants {UndergroundComplex.FireCodeMinExits}.");
                }

                // The first door is still Door, so every caller written against the old record still means
                // the same leaf.
                Assert.Equal(room.Door, room.Doors[0]);

                // Every one of them is a door the FLOOR published, so the deck hangs a leaf on it and an
                // audit can find it without knowing anything about rings. Past the seam nothing is doored
                // at all, which is #592's own tell and not an omission.
                if (UndergroundComplex.IsFound(body, level))
                {
                    continue;
                }
                foreach (SurfaceLayout.Doorway d in room.Doors)
                {
                    if (!floor.Doorways.Contains(d))
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number} has a street door at "
                            + $"({(d.X1 + d.X2) / 2:F1},{(d.Y1 + d.Y2) / 2:F1}) that the floor's own list "
                            + "does not carry — nothing would be drawn there.");
                    }
                }

                // …and they are all on the room's own street wall, evenly enough spaced that no two of them
                // are the same hole.
                var along = room.Doors
                    .Select(d => room.Side is UndergroundComplex.RingSide.Near
                        or UndergroundComplex.RingSide.Far
                        ? (d.X1 + d.X2) / 2.0
                        : (d.Y1 + d.Y2) / 2.0)
                    .OrderBy(v => v)
                    .ToList();
                for (int i = 1; i < along.Count; i++)
                {
                    if (along[i] - along[i - 1] < 2 * UndergroundComplex.DoorHalf)
                    {
                        wrong.Add($"  {body} B{-level}: ring room {room.Number} has two doors "
                            + $"{along[i] - along[i - 1]:F1} du apart — one hole wearing two names.");
                    }
                }
            }
        }

        Assert.True(rooms > 500, $"only {rooms} ring rooms were checked — this proved little.");
        Assert.True(wide > 150,
            $"only {wide} of them were wide enough to earn a second leaf — the ruling this guard carries "
            + "was barely exercised.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} ring room(s) are under-doored:\n" + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>
    /// EVERY DOOR STILL OPENS ONTO ITS STREET. The count changed; where they are did not.
    ///
    /// <para>On the near band the street is the SPINE and its face is poured by the spine builder from one
    /// sorted list of spans, so this is the half that could break silently: a door handed over as a span and
    /// never cut would be a leaf drawn on a solid wall. Measured as an actual GAP in the poured wall on the
    /// spine's own line, which is the only statement that can tell a cut door from a promised one.</para>
    ///
    /// <para>#817 · …and every VIEW SUITE has its gate onto the green, which is the owner's other ruling
    /// from the same playtest.</para>
    ///
    /// <para><b>Proven RED</b> twice. Handing the spine only the first cut (<c>d &lt; 1</c> on the
    /// <c>spineDoors.Add</c> loop in <c>RingBox</c>):</para>
    /// <code>
    /// 260 door(s) open onto a wall:
    ///   luna B1: ring room 1's door at (-111.5,-157.0) is drawn on a poured wall — the spine was never
    ///   cut for it.
    ///   luna B1: ring room 2's door at (-82.1,-157.0) is drawn on a poured wall — the spine was never
    ///   cut for it.
    /// </code>
    /// <para>…and putting the suites back to <c>gate: false</c>, which is what they were before the owner's
    /// ruling:</para>
    /// <code>
    /// 312 door(s) open onto a wall:
    ///   luna B1: ring room 2 — SENIOR ROTA · GREEN SIDE — looks at the green through glass with no door
    ///   in it. The premium suites get a way OUT onto the garden they are sold on.
    ///   luna B1: ring room 11 — REGISTERED OFFICE · GARDEN ASPECT — looks at the green through glass
    ///   with no door in it. …
    /// </code>
    /// </summary>
    [Fact]
    public void EveryStreetDoorIsAGapInTheStreetAndEveryViewSuiteHasItsGate()
    {
        var wrong = new List<string>();
        int near = 0, gates = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryBlock())
        {
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (room.Side == UndergroundComplex.RingSide.Near)
                {
                    double line = room.Y1;
                    foreach (SurfaceLayout.Doorway d in room.Doors)
                    {
                        near++;
                        double lo = Math.Min(d.X1, d.X2), hi = Math.Max(d.X1, d.X2);

                        foreach (SurfaceLayout.Wall w in floor.Walls)
                        {
                            if (Math.Abs(w.Y1 - line) > 0.001 || Math.Abs(w.Y2 - line) > 0.001)
                            {
                                continue;
                            }
                            double wLo = Math.Min(w.X1, w.X2), wHi = Math.Max(w.X1, w.X2);
                            if (wLo < hi - 0.05 && wHi > lo + 0.05)
                            {
                                wrong.Add($"  {body} B{-level}: ring room {room.Number}'s door at "
                                    + $"({(lo + hi) / 2:F1},{line:F1}) is drawn on a poured wall — the "
                                    + "spine was never cut for it.");
                                break;
                            }
                        }
                    }
                }

                // #817 · EVERY suite with a view — the near band's and the block's two ends — has a way OUT
                // onto the garden it is sold on. The far band has had one since #801 and is checked by its
                // own guards; a corner room has no park in front of it and correctly has none.
                if (!IsViewSuite(in room))
                {
                    continue;
                }

                // Counted on the rooms that OWE a gate rather than on the ones that have one, so that
                // taking the gates away makes this guard report the gates instead of reporting that it saw
                // none — an anti-vacuity clause that fires first turns a red into a shrug.
                gates++;

                if (room.Gate is not { } gate)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number} — {room.Plate} — looks at the "
                        + "green through glass with no door in it. The premium suites get a way OUT onto "
                        + "the garden they are sold on.");
                    continue;
                }

                // …and it is cut in the park's OWN wall, whichever of the four this room stands on.
                double gx = (gate.X1 + gate.X2) / 2.0, gy = (gate.Y1 + gate.Y2) / 2.0;
                bool onThePark = room.Side switch
                {
                    UndergroundComplex.RingSide.Near => Math.Abs(gy - park.Y1) < 0.001,
                    UndergroundComplex.RingSide.Far => Math.Abs(gy - park.Y0) < 0.001,
                    UndergroundComplex.RingSide.West => Math.Abs(gx - park.X0) < 0.001,
                    _ => Math.Abs(gx - park.X1) < 0.001,
                };
                if (!onThePark)
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number}'s gate at ({gx:F1},{gy:F1}) is "
                        + "not in the park's own wall.");
                }
                if (!UndergroundComplex.IsFound(body, level) && !floor.Doorways.Contains(gate))
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number}'s gate onto the green is not "
                        + "in the floor's own list of doorways.");
                }
                if (!park.BackDoors.Contains(gate))
                {
                    wrong.Add($"  {body} B{-level}: ring room {room.Number}'s gate is not in the park's "
                        + "own list of doors off the gravel — the sealing experiment would miss it.");
                }
            }
        }

        Assert.True(near > 100, $"only {near} near-band doors were checked — this proved little.");
        Assert.True(gates > 80, $"only {gates} premium gates were checked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} door(s) open onto a wall:\n" + string.Join("\n", wrong.Take(20)));
    }
}
