using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #863 · B1 GETS ITS ROUND — the furnished floor finally meets the man who walks it.
///
/// <para><b>Owner ruling, 2026-08-13,</b> watching a guard on the top pressurised floor with nothing to do:
/// <i>"let's try to have round because the guard looks kind of silly just standing in the middle of an
/// aisle."</i></para>
///
/// <para>Every other floor in the building has had a rota since #804. B1 — the one floor with the park, the
/// canteen, the offices, the washrooms and (post-#862) full furnishing — was excluded by a single
/// <c>&gt;=</c> in <see cref="PatrolBeat.IsPatrolled"/>. Lab 45 (§C) is where the irony was finally written
/// down: <i>the furniture and the rounds have never yet stood on the same floor</i>, so every heavy-floor
/// number in that lab was a what-if about a round nobody was ever going to walk there.</para>
///
/// <para>This file is the reversal, stated as three things a build can fail on:</para>
///
/// <list type="number">
/// <item><b>The bar floor is walked</b> — on every site that has one, and still never on the head office.</item>
/// <item><b>Nobody in a uniform steps on the gravel.</b> The park's watcher is #790's bench figure, and a
/// man on a rota crossing the green would take that figure's whole job off it. The round stands at the head
/// of both streets that lead down to the gates and turns round — which makes the hole a CHOICE rather than a
/// gap in the geometry, exactly as #824's freight side is.</item>
/// <item><b>…and it does walk the canteen's frontage</b>, which is the point of putting it there: the
/// spine's south face IS the canteen's front wall, and every lap takes the round past every door in it,
/// under a plate that says <c>NO PASS REQUIRED</c>, past a room the owner filled with people in #709.</item>
/// </list>
///
/// <para><b>Where the other halves live.</b> That every stop is standable and every leg connects on the real
/// collision field is the client's <c>TheRoundIsWalkableTests</c> — B1 walked into that audit the moment the
/// clause flipped, and it is green, which is the honest finding: the densest floor in the game needed no
/// waypoint moved. That no leg enters the goods car's alcove is #824's own guard, whose sweep is
/// <see cref="PatrolBeat.IsPatrolled"/>'s answer and which therefore grew this floor for free.</para>
/// </summary>
public sealed class B1GetsItsRoundTests
{
    /// <summary>A du of slack a computed coordinate is allowed to miss a poured one by. Both sides of every
    /// comparison here come off the same published geometry in exact arithmetic, so this is a float's
    /// epsilon and not a tolerance for a near miss.</summary>
    private const double Eps = 0.001;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The shipped sites, both cheat rocks, the head office, and forty rolled ones — #824's own
    /// sweep, because a law about the bar floor is a law about every building's bar floor and a rule that
    /// only holds on luna has not met the generator.</summary>
    private static IEnumerable<string> Sweep() =>
        new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
            "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
        }.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    // ── (a) THE CLAUSE THAT WAS REVERSED ──────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TOP PRESSURISED FLOOR IS WALKED, on every site that has one, and the roll that puts heads on it
    /// answers for it like any other floor.
    ///
    /// <para><b>Proven RED</b> by putting the old <c>level &gt;= bar</c> back into
    /// <see cref="PatrolBeat.IsPatrolled"/> — fifty-two sites, every one of them naming the floor the owner
    /// watched a man stand still on. The verbatim run is in the PR body.</para>
    ///
    /// <para>The head office keeps its exclusion, in this same sweep and on purpose: #411's building has no
    /// gate and a fiction of its own, and a reversal that quietly took the wintering hall with it would be
    /// this lane deciding something about a place it does not own.</para>
    /// </summary>
    [Fact]
    public void TheBarFloorIsWalkedNowOnEverySiteThatHasOne()
    {
        var statues = new List<string>();
        int walked = 0, asked = 0;
        var heads = new HashSet<int>();

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } bar)
            {
                continue;   // a hole in the ground with nothing that breathes has no floor to argue about
            }

            if (UndergroundComplex.IsHeadOffice(body))
            {
                Assert.False(
                    PatrolBeat.IsPatrolled(body, bar),
                    $"{body} B{-bar}: a contract guard in the head office — #411's building is not this " +
                    "lane's to roster.");
                continue;
            }

            if (!PatrolBeat.IsPatrolled(body, bar))
            {
                statues.Add($"  {body} B{-bar}: the furnished floor still has nobody walking it — the guard " +
                            "is standing in the middle of an aisle.");
                continue;
            }

            walked++;
            for (long watch = 0; watch < 6; watch++)
            {
                int on = PatrolBeat.GuardsOn(body, bar, watch);
                asked++;
                Assert.InRange(on, 1, PatrolBeat.MostOnAFloor);
                heads.Add(on);
            }
        }

        Assert.True(statues.Count == 0,
            $"{statues.Count} site(s) still keep the bar floor off the rota:\n"
            + string.Join("\n", statues.Take(20)));

        // …and the sweep is not about nothing. A predicate that answered true everywhere would satisfy the
        // clause above while saying nothing at all about the floor this issue is named after.
        Assert.True(walked > 40, $"only {walked} site(s) have a round on the bar floor.");
        Assert.True(asked > 200, $"only {asked} (site, watch) pairs were asked.");
        Assert.Equal(new HashSet<int> { 1, 2 }, heads);
    }

    // ── (b) …AND STILL NOBODY WALKS ON THE GRASS ──────────────────────────────────────────────────────

    /// <summary>
    /// NO STOP AND NO LEG OF ANY ROUND ENTERS THE PARK, on the circuit and on four watches of it, on every
    /// bar floor in the sweep that has a park block — and the round comes to the HEAD OF THE STREET that
    /// leads to the gravel, so the hole is a choice.
    ///
    /// <para>Two claims, and the second exists so the first is not a statement about a patch of ground the
    /// round could never have reached anyway (the fifth bug class, exactly — an absence is what a box
    /// pointed at the wrong place reports too):</para>
    ///
    /// <list type="bullet">
    /// <item><b>The green is untouched.</b> The park's own published box, grown by
    /// <see cref="UndergroundComplex.CorridorHalf"/> so a beat that came to stand in a gate's mouth is red
    /// before it steps out onto the gravel. The crossing test is
    /// <see cref="SurfaceCollision.HasLineOfSight"/> over the box's four sides — the same wall law the
    /// guard's own eye obeys, rather than a second copy of somebody's segment arithmetic.</item>
    /// <item><b>The streets to it are walked.</b> Every gate in the park's north face has a rib running
    /// straight up from it to the spine, and the round stops at the mouth of every one — so a guard turns
    /// away from the gravel twice a lap, at a spot from which the gate is a straight walk down an empty
    /// corridor.</item>
    /// </list>
    ///
    /// <para><b>Proven RED</b> by having <see cref="PatrolBeat.Circuit"/> add a stop at the park's own
    /// centre — the exact shape "the round should take in the green too" would have had as a patch. The
    /// verbatim run is in the PR body.</para>
    /// </summary>
    [Fact]
    public void NobodyInAUniformStepsOnTheGravel()
    {
        var trespass = new List<string>();
        int floors = 0, gates = 0, legs = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } bar
                || !PatrolBeat.IsPatrolled(body, bar))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, bar, Field);
            if (floor.Park is not { } park)
            {
                continue;
            }

            (double _, double spineY) = UndergroundComplex.ShaftAt(Field);
            floors++;

            // The green, plus the corridor's own half-width on every side. Derived from the building rather
            // than typed here, so a generator that widens its corridors widens this with them.
            double m = UndergroundComplex.CorridorHalf;
            (double X0, double Y0, double X1, double Y1) green =
                (park.X0 - m, park.Y0 - m, park.X1 + m, park.Y1 + m);
            IReadOnlyList<SurfaceCollision.Segment> sides = Sides(green);

            IReadOnlyList<PatrolBeat.Stop>[] rounds =
            [
                PatrolBeat.Circuit(floor, Field),
                .. Enumerable.Range(0, 4).Select(w => PatrolBeat.BeatFor(body, bar, w, floor, Field)),
            ];

            double closest = double.PositiveInfinity;
            foreach (IReadOnlyList<PatrolBeat.Stop> round in rounds)
            {
                for (int i = 0; i < round.Count; i++)
                {
                    PatrolBeat.Stop here = round[i], next = round[(i + 1) % round.Count];
                    legs++;

                    if (Inside(here.X, here.Y, green))
                    {
                        trespass.Add(
                            $"  {body} B{-bar}: the stop '{here.What}' stands at ({here.X:0.0}, {here.Y:0.0}), " +
                            "on the park's own ground — the bench figure is not the only one watching it now.");
                    }

                    // A leg that neither starts nor ends inside the box enters it exactly when it crosses
                    // the boundary, which is the one question SurfaceCollision already answers.
                    if (!SurfaceCollision.HasLineOfSight(here.X, here.Y, next.X, next.Y, sides))
                    {
                        trespass.Add(
                            $"  {body} B{-bar}: the leg ({here.X:0.0}, {here.Y:0.0}) → ({next.X:0.0}, " +
                            $"{next.Y:0.0}) crosses the park — a uniform on the grass.");
                    }

                    closest = Math.Min(closest, ToBox(here.X, here.Y, next.X, next.Y, park));
                }
            }

            // THE STREETS. Every gate in the park's north face stands at the foot of a rib, and the round
            // stops at that rib's mouth on the spine: the turn away from the green happens at a spot from
            // which the gate is a straight walk down an empty corridor.
            foreach (SurfaceLayout.Doorway gate in park.Gates ?? [park.Gate])
            {
                bool northFace = Math.Abs(gate.Y1 - gate.Y2) < Eps && Math.Abs(gate.Y1 - park.Y1) < Eps;
                if (!northFace)
                {
                    continue;
                }

                gates++;
                double gx = (gate.X1 + gate.X2) / 2;
                bool stoodAtTheHead = PatrolBeat.Circuit(floor, Field)
                    .Any(s => Math.Abs(s.X - gx) < Eps && Math.Abs(s.Y - spineY) < Eps);
                if (!stoodAtTheHead)
                {
                    trespass.Add(
                        $"  {body} B{-bar}: the gate at x{gx:0.0} has no stop at the head of its street — " +
                        "nobody turns away from the green there, so nothing here is a choice.");
                }
            }

            // …and the round really does come as near as the head of that street. A round that had retreated
            // to the far end of the floor would keep off the grass for a reason nobody meant. Collected
            // rather than asserted here, so a red run prints the whole building instead of its first floor.
            double street = spineY - park.Y1;
            if (closest > street + Eps)
            {
                trespass.Add(
                    $"  {body} B{-bar}: the round never comes nearer the park than {closest:0.000} du, and " +
                    $"the street from the spine to the gate is only {street:0.000} du long — the green is " +
                    "being avoided by geography rather than by anybody's decision.");
            }
            if (closest <= 0)
            {
                trespass.Add($"  {body} B{-bar}: the round is standing on the green ({closest:0.000} du).");
            }
        }

        Assert.True(trespass.Count == 0,
            $"{trespass.Count} thing(s) put a uniform on the park's own ground:\n"
            + string.Join("\n", trespass.Take(20)));

        Assert.True(floors > 40, $"only {floors} bar floor(s) have a park block on them.");
        Assert.True(gates >= floors * 2, $"only {gates} north gates over {floors} floors — this proved little.");
        Assert.True(legs > floors * 30, $"only {legs} legs walked over {floors} floors.");
    }

    // ── (c) …AND IT GOES PAST THE DINERS, WHICH IS THE WHOLE POINT ────────────────────────────────────

    /// <summary>
    /// EVERY DOOR IN THE CANTEEN'S FRONT WALL IS PASSED, every lap.
    ///
    /// <para>The spine's south face IS the canteen's front wall — the hall is carved right up against the
    /// corridor — so the legs that run the length of the spine are a man walking the length of a room full
    /// of contractors eating. That is the texture the owner filed the issue for, and it is asserted here as
    /// geometry rather than left as a hope about a shape: for every opening in the hall that stands on the
    /// spine's own face, some leg of the round passes within a corridor's half-width plus the tolerance a
    /// guard counts as standing AT a stop (<see cref="PatrolBeat.AtTheStopDu"/>) — that is, he goes by in
    /// the doorway's mouth.</para>
    ///
    /// <para>And the room has people in it: <see cref="CanteenRegulars.PeopleSitHere"/> is the owner's own
    /// B1 ruling, asked rather than assumed, so a floor whose crowd was ever moved would take this guard
    /// with it.</para>
    ///
    /// <para><b>Proven RED</b> by lifting <see cref="PatrolBeat.Circuit"/>'s rib mouths twenty du off the
    /// spine — the drift a mouth stop computed from the wrong face would have. The verbatim run is in the
    /// PR body.</para>
    /// </summary>
    [Fact]
    public void TheRoundGoesPastEveryDoorInTheCanteensFrontWall()
    {
        var missed = new List<string>();
        int floors = 0, doors = 0;

        // In the doorway's mouth: the corridor's own half-width (the door is in the wall the corridor runs
        // along) plus the slack a guard is allowed to stand off a stop by. Both read, neither typed.
        double inTheMouth = UndergroundComplex.CorridorHalf + PatrolBeat.AtTheStopDu;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } bar
                || !PatrolBeat.IsPatrolled(body, bar))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, bar, Field);
            (double _, double spineY) = UndergroundComplex.ShaftAt(Field);
            double face = spineY - UndergroundComplex.CorridorHalf;
            IReadOnlyList<PatrolBeat.Stop> round = PatrolBeat.Circuit(floor, Field);

            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (!CanteenRegulars.PeopleSitHere(body, bar, a) || a.Hall is not { } hall)
                {
                    continue;
                }

                Assert.True(a.Tables.Count > 0,
                    $"{body} B{-bar}: the room the round walks past has no tops in it — there is nobody to " +
                    "walk past.");
                floors++;

                foreach (SurfaceLayout.Doorway door in hall.Openings)
                {
                    // Only the doors in the front wall. The hall's other openings give onto the park block's
                    // own street, which is a different corridor and nobody's round.
                    if (Math.Abs(door.Y1 - door.Y2) >= Eps || Math.Abs(door.Y1 - face) >= Eps)
                    {
                        continue;
                    }

                    doors++;
                    double dx = (door.X1 + door.X2) / 2, dy = door.Y1;
                    double nearest = double.PositiveInfinity;
                    for (int i = 0; i < round.Count; i++)
                    {
                        PatrolBeat.Stop here = round[i], next = round[(i + 1) % round.Count];
                        nearest = Math.Min(
                            nearest,
                            SurfaceCollision.DistanceToSegment(dx, dy, here.X, here.Y, next.X, next.Y));
                    }

                    if (nearest > inTheMouth)
                    {
                        missed.Add(
                            $"  {body} B{-bar}: the canteen door at ({dx:0.0}, {dy:0.0}) is never passed — " +
                            $"the nearest the round comes is {nearest:0.000} du, and the doorway's mouth is " +
                            $"{inTheMouth:0.000}.");
                    }
                }
            }
        }

        Assert.True(missed.Count == 0,
            $"{missed.Count} canteen door(s) nobody on the rota ever goes past:\n"
            + string.Join("\n", missed.Take(20)));

        Assert.True(floors > 40, $"only {floors} bar floor(s) carry a canteen with people in it.");
        Assert.True(doors >= floors * 2, $"only {doors} front doors over {floors} floors — this proved little.");
    }

    // ── THE ARITHMETIC, all of it off geometry Core already publishes ─────────────────────────────────

    private static bool Inside(double px, double py, (double X0, double Y0, double X1, double Y1) box) =>
        px >= box.X0 && px <= box.X1 && py >= box.Y0 && py <= box.Y1;

    /// <summary>A box as four wall segments, so a crossing question can be asked of
    /// <see cref="SurfaceCollision.HasLineOfSight"/> rather than of a second copy of somebody's segment
    /// intersection.</summary>
    private static IReadOnlyList<SurfaceCollision.Segment> Sides(
        (double X0, double Y0, double X1, double Y1) box) =>
        [
            new(box.X0, box.Y0, box.X1, box.Y0),
            new(box.X1, box.Y0, box.X1, box.Y1),
            new(box.X1, box.Y1, box.X0, box.Y1),
            new(box.X0, box.Y1, box.X0, box.Y0),
        ];

    /// <summary>How far a leg passes from the park's own box, zero when it touches or enters it. The corners
    /// and the endpoints are enough: between two convex shapes that do not meet, the closest pair always has
    /// a vertex of one of them in it.</summary>
    private static double ToBox(
        double ax, double ay, double bx, double by, in UndergroundComplex.Park park)
    {
        (double X0, double Y0, double X1, double Y1) box = (park.X0, park.Y0, park.X1, park.Y1);
        if (Inside(ax, ay, box) || Inside(bx, by, box)
            || !SurfaceCollision.HasLineOfSight(ax, ay, bx, by, Sides(box)))
        {
            return 0;
        }

        double best = Math.Min(ToPoint(ax, ay, box), ToPoint(bx, by, box));
        foreach ((double cx, double cy) in new[]
                 {
                     (box.X0, box.Y0), (box.X1, box.Y0), (box.X1, box.Y1), (box.X0, box.Y1),
                 })
        {
            best = Math.Min(best, SurfaceCollision.DistanceToSegment(cx, cy, ax, ay, bx, by));
        }
        return best;
    }

    private static double ToPoint(double px, double py, (double X0, double Y0, double X1, double Y1) box)
    {
        double dx = Math.Max(Math.Max(box.X0 - px, px - box.X1), 0);
        double dy = Math.Max(Math.Max(box.Y0 - py, py - box.Y1), 0);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
