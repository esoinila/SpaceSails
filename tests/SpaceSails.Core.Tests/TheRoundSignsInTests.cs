using System;
using System.Collections.Generic;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #831 · A STANDING GUARD IS STANDING <b>AT</b> SOMETHING — the geometry half, in Core.
///
/// <para>Owner, evening playtest 2026-08-11, watching a man motionless in a corridor: <i>"I kind of liked
/// the random encounter but the guard was in the middle of the corridor which was strange"</i> — <i>"why
/// would it just stand there if there is no inspection point etc"</i>. And then the anchor that turned it
/// into a system: <i>"they actually in real life like have these check points they electronically sign on
/// rounds to prove they did their round."</i></para>
///
/// <para>So every stop on every round is a watchclock station, and this file is the sweep that says so of
/// the whole building: the plate is on a wall the generator built, it is clear of every hole the generator
/// cut, the stop the guard walks to IS the square that plate is signed from, and there is room for a body to
/// stand on it.</para>
///
/// <para>The other half — <i>"they should respect right side traffic, and not walk in the middle of the
/// corridor"</i> — is the lane, and its geometry is asserted here in the one form that cannot be a formula
/// copied out of the source: a man walking EAST walks on the SOUTH side, and the man coming back walks on
/// the north side.</para>
/// </summary>
public sealed class TheRoundSignsInTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>
    /// EVERY STOP ON EVERY ROUND IS A STATION, AND EVERY STATION IS ON A WALL AND OFF THE DOORWAYS.
    ///
    /// <para><b>Proven RED</b> by reverting <c>Circuit</c> to its unsnapped stops (return
    /// <c>WantedCircuit</c> straight through) — see the PR body for the verbatim run and the count of stops
    /// standing at nothing.</para>
    /// </summary>
    [Fact]
    public void EveryBeatStopStandsAtAPublishedCheckpoint()
    {
        var bad = new List<string>();
        int floors = 0, stops = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                IReadOnlyList<PatrolBeat.Stop> circuit = PatrolBeat.Circuit(plan, Field);
                if (circuit.Count < 2)
                {
                    continue;
                }
                floors++;

                foreach (PatrolBeat.Stop stop in circuit)
                {
                    stops++;

                    if (stop.Point is not { } point)
                    {
                        bad.Add($"  {body} B{-level}: {stop.What} at ({stop.X:F0}, {stop.Y:F0}) has no " +
                                "checkpoint — a man would be standing there with nothing to sign.");
                        continue;
                    }

                    // THE STOP *IS* THE SQUARE THE PLATE IS SIGNED FROM. Not near it: it.
                    if (!point.Signing(stop.X, stop.Y))
                    {
                        bad.Add($"  {body} B{-level}: {stop.What} is {point.GapFrom(stop.X, stop.Y):F1} du " +
                                $"from ROUND POINT {point.Number} — the stop did not snap to it.");
                    }

                    // …the plate is ON a wall the generator built.
                    double onWall = double.MaxValue;
                    foreach (SurfaceLayout.Wall w in plan.Walls)
                    {
                        onWall = Math.Min(
                            onWall,
                            SurfaceCollision.DistanceToSegment(point.X, point.Y, w.X1, w.Y1, w.X2, w.Y2));
                    }
                    if (onWall > 0.001)
                    {
                        bad.Add($"  {body} B{-level}: ROUND POINT {point.Number} is bolted {onWall:F2} du " +
                                "off the nearest wall — a plate hanging in mid-air.");
                    }

                    // …and CLEAR OF EVERY PUBLISHED OPENING, which is the whole of the #862 measured idiom.
                    foreach (SurfaceLayout.Doorway d in plan.Doorways)
                    {
                        double gap = SurfaceCollision.DistanceToSegment(
                            point.X, point.Y, d.X1, d.Y1, d.X2, d.Y2);
                        if (gap < PatrolBeat.ClearOfOpeningDu)
                        {
                            bad.Add($"  {body} B{-level}: ROUND POINT {point.Number} is {gap:F1} du from a " +
                                    "doorway — a station across a door somebody has to walk through.");
                            break;
                        }
                    }

                    // …and the man signing it is LOOKING AT IT.
                    double look = Math.Atan2(point.Y - stop.Y, point.X - stop.X);
                    if (Math.Abs(Math.Atan2(Math.Sin(look - point.Facing), Math.Cos(look - point.Facing)))
                        > 1e-9)
                    {
                        bad.Add($"  {body} B{-level}: ROUND POINT {point.Number}'s facing does not point at " +
                                "its own plate.");
                    }
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} stop(s) of {stops} are not standing at anything:");
            foreach (string line in bad.GetRange(0, Math.Min(20, bad.Count)))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        // A flood over nothing is a green test that asserts nothing (§13). Pin the size of the sweep.
        Assert.True(floors > 40, $"only {floors} patrolled floors were swept.");
        Assert.True(stops > 300, $"only {stops} stops were measured.");
    }

    /// <summary>The station numbers are facts about the PLACE and not about the shift: the plate says ROUND
    /// POINT 3 whichever way round tonight's watch walks it. A renumbering plate would be a building that
    /// cannot count its own doors.</summary>
    [Fact]
    public void TheStationsDoNotRenumberWithTheWatch()
    {
        int floors = 0;
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                var canonical = new HashSet<string>(StringComparer.Ordinal);
                foreach (PatrolBeat.Checkpoint p in PatrolBeat.CheckpointsOn(plan, Field))
                {
                    canonical.Add($"{p.Number}@{p.X:F3},{p.Y:F3}");
                }
                if (canonical.Count == 0)
                {
                    continue;
                }
                floors++;

                for (long watch = 0; watch < 5; watch++)
                {
                    var walked = new HashSet<string>(StringComparer.Ordinal);
                    foreach (PatrolBeat.Stop s in PatrolBeat.BeatFor(body, level, watch, plan, Field))
                    {
                        if (s.Point is { } p)
                        {
                            walked.Add($"{p.Number}@{p.X:F3},{p.Y:F3}");
                        }
                    }
                    Assert.Equal(canonical, walked);
                }
            }
        }
        Assert.True(floors > 40, $"only {floors} floors were measured.");
    }

    /// <summary>
    /// THE PLATE IS IN THE BUILDING'S OWN STENCIL REGISTER (§13.8): it names the FIXTURE and says nothing at
    /// all about what the facility is for.
    ///
    /// <para>The sixteen forbidden words are <c>TheHiveAmenitiesTests.NoAmenityEXPLAINSWhatThisPlaceWasFor</c>'s
    /// own list, run again here rather than trusted: that sweep walks the AMENITY signs, and a new kind of
    /// stencil that nothing swept would be a way into this building's one canon rule with nobody watching it.
    /// A watchclock station is the most tempting plate in the place to over-explain — it exists to prove
    /// somebody walked past, and "prove it to whom, about what" is the question §13.8 forbids answering.</para>
    /// </summary>
    [Fact]
    public void ThePlateNamesTheFixtureAndNothingElse()
    {
        Assert.Equal("🔘 ROUND POINT 3", PatrolBeat.CheckpointPlate(3));
        Assert.True(PatrolBeat.IsCheckpointPlate(PatrolBeat.CheckpointPlate(1)));
        Assert.False(PatrolBeat.IsCheckpointPlate("🖼 HYDROGEN: FUEL OF THE COMING CENTURY"));

        string[] forbidden =
        [
            "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect", "clone",
            "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        // …and it is a NOUN on a wall, never an instruction to the man reading it. A plate that told a guard
        // to sign it would be this building giving orders — a voice it does not have — and it would read to a
        // player as a verb they were being denied.
        string[] orders = ["sign", "press", "scan", "swipe", "report", "you"];

        for (int n = 1; n <= 40; n++)
        {
            string plate = PatrolBeat.CheckpointPlate(n);
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, plate, StringComparison.OrdinalIgnoreCase);
            }
            foreach (string order in orders)
            {
                Assert.DoesNotContain(order, plate, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── THE LANE ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// RIGHT-SIDE TRAFFIC, ASSERTED AS A DIRECTION AND NOT AS A FORMULA.
    ///
    /// <para>Owner: <i>"they should respect right side traffic, and not walk in the middle of the
    /// corridor."</i> +y is up on this deck, so the right hand of a man walking EAST points SOUTH. A test
    /// that re-derived the normal from the source would be the fifth bug class with a corridor in it; this
    /// one states the compass answer a person can check by standing up and turning round.</para>
    ///
    /// <para><b>Proven RED</b> by returning the route unlaned from <c>KeepRight</c>, and again by flipping
    /// the normal's sign — see the PR body.</para>
    /// </summary>
    [Fact]
    public void AManWalkingEastWalksOnTheSouthSideAndTheManComingBackWalksOnTheNorth()
    {
        // A bare east–west corridor, one corridor-width across, and nothing else in the world.
        double half = UndergroundComplex.CorridorHalf;
        SurfaceCollision.Segment[] walls =
        [
            new(-40, -half, 40, -half),
            new(-40, +half, 40, +half),
        ];

        IReadOnlyList<DeckReachability.Point> east = Straight(-30, 0, +30, 0);
        IReadOnlyList<DeckReachability.Point> west = Straight(+30, 0, -30, 0);

        IReadOnlyList<DeckReachability.Point> goingEast = PatrolBeat.KeepRight(east, walls, 0.7);
        IReadOnlyList<DeckReachability.Point> goingWest = PatrolBeat.KeepRight(west, walls, 0.7);

        double midEast = goingEast[goingEast.Count / 2].Y;
        double midWest = goingWest[goingWest.Count / 2].Y;

        Assert.True(midEast < 0,
            $"a guard walking east held y={midEast:F2} — the right hand of east is SOUTH on this deck.");
        Assert.True(midWest > 0,
            $"a guard walking west held y={midWest:F2} — the right hand of west is NORTH on this deck.");

        // …and it is the PUBLISHED offset, not a nudge.
        Assert.Equal(-PatrolBeat.LaneOffsetDu, midEast, 6);
        Assert.Equal(+PatrolBeat.LaneOffsetDu, midWest, 6);

        // TWO GUARDS ON OPPOSITE LEGS PASS ON OPPOSITE SIDES — the whole of what the owner bought with it.
        Assert.True(midWest - midEast >= 2 * PatrolBeat.LaneOffsetDu - 1e-9,
            $"two guards passing were only {midWest - midEast:F2} du apart across the corridor.");

        // …and neither of them is inside the wall he is keeping to.
        foreach (DeckReachability.Point p in goingEast)
        {
            Assert.False(SurfaceCollision.Blocked(p.X, p.Y, 0.7, walls),
                $"the laned route puts a body at ({p.X:F1}, {p.Y:F1}), which is inside the shotcrete.");
        }
    }

    /// <summary>THE LANE IS A PREFERENCE AND NEVER A WALL. A route that turns — a corner, a doorway, a rib
    /// mouth — gives the offset up at the turn and takes the middle, which is what lets a laned leg walk
    /// through a leaf narrower than twice the offset.</summary>
    [Fact]
    public void TheLaneIsGivenUpAtACorner()
    {
        var route = new List<DeckReachability.Point>();
        for (double x = 0; x <= 10; x += 0.5)
        {
            route.Add(new DeckReachability.Point(x, 0));
        }
        for (double y = 0.5; y <= 10; y += 0.5)
        {
            route.Add(new DeckReachability.Point(10, y));
        }

        IReadOnlyList<DeckReachability.Point> laned = PatrolBeat.KeepRight(route, [], 0.7);
        Assert.Equal(route.Count, laned.Count);

        // The corner waypoint itself is untouched…
        int corner = route.FindIndex(p => Math.Abs(p.X - 10) < 1e-9 && Math.Abs(p.Y) < 1e-9);
        Assert.True(corner > 0, "the corner is no longer in this route.");
        Assert.Equal(route[corner].X, laned[corner].X, 9);
        Assert.Equal(route[corner].Y, laned[corner].Y, 9);

        // …and both ends of the whole leg are, because a lane may never move a stop.
        Assert.Equal(route[0], laned[0]);
        Assert.Equal(route[^1], laned[^1]);
    }

    /// <summary>A shifted waypoint that a body would not fit on keeps the centre line. The offset is a
    /// preference the ground is always allowed to refuse — the clause that stops a lane wedging a man
    /// against a jamb.</summary>
    [Fact]
    public void TheLaneGivesWayToTheGround()
    {
        // A corridor with the RIGHT-hand side of an eastbound walk blocked solid.
        SurfaceCollision.Segment[] walls = [new(-40, -0.2, 40, -0.2)];
        IReadOnlyList<DeckReachability.Point> laned =
            PatrolBeat.KeepRight(Straight(-10, 2, 10, 2), walls, 0.7);

        foreach (DeckReachability.Point p in laned)
        {
            Assert.False(SurfaceCollision.Blocked(p.X, p.Y, 0.7, walls),
                $"the lane put a body at ({p.X:F1}, {p.Y:F1}), inside a wall.");
        }
    }

    /// <summary>
    /// AND THE GROUND <b>BETWEEN</b> TWO WAYPOINTS IS GROUND A BODY WALKS ON.
    ///
    /// <para>The clause the first cut of this lane did not have. An offset moves a man sideways between two
    /// points the A* proved, and checking only the waypoints let a laned line clip a corner the search had
    /// cleared — with the body then standing somewhere the planner could not plan FROM, which pinned a round
    /// for good. <see cref="AutoWalk.Along"/> says out loud that whoever hands it a line owns the claim that
    /// consecutive points are walkable; this is that claim asked for.</para>
    ///
    /// <para>The field is a corridor with a row of piers down the middle of the eastbound lane — an offset
    /// that only ever asked about its own waypoints would thread between them and drag the walked line
    /// straight through one. <b>Proven RED</b> by disabling the probe loop in <c>HopIsWalkable</c>; see the
    /// PR body.</para>
    /// </summary>
    [Fact]
    public void TheLaneNeverWalksAManThroughGroundItDidNotAskAbout()
    {
        double half = UndergroundComplex.CorridorHalf;
        var walls = new List<SurfaceCollision.Segment>
        {
            new(-40, -half, 40, -half),
            new(-40, +half, 40, +half),
        };

        // Piers on the eastbound lane's own line, spaced so a waypoint can sit between two of them.
        double lane = -PatrolBeat.LaneOffsetDu;
        for (double x = -20; x <= 20; x += 3.1)
        {
            walls.Add(new SurfaceCollision.Segment(x, lane - 0.4, x, lane + 0.4));
        }

        IReadOnlyList<DeckReachability.Point> laned =
            PatrolBeat.KeepRight(Straight(-30, 0, 30, 0), walls, 0.7);

        for (int i = 1; i < laned.Count; i++)
        {
            double dx = laned[i].X - laned[i - 1].X, dy = laned[i].Y - laned[i - 1].Y;
            int probes = (int)Math.Ceiling(Math.Sqrt((dx * dx) + (dy * dy)) / 0.05);
            for (int k = 0; k <= probes; k++)
            {
                double t = k / (double)probes;
                double px = laned[i - 1].X + (dx * t), py = laned[i - 1].Y + (dy * t);
                Assert.False(SurfaceCollision.Blocked(px, py, 0.7, walls),
                    $"the walked line passes through ({px:F2}, {py:F2}), which is inside a pier — the lane " +
                    "moved a man across ground nobody asked about.");
            }
        }
    }

    /// <summary>A dense straight line of waypoints, the shape the lattice A* hands back.</summary>
    private static IReadOnlyList<DeckReachability.Point> Straight(
        double x0, double y0, double x1, double y1)
    {
        var pts = new List<DeckReachability.Point>();
        double dx = x1 - x0, dy = y1 - y0;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        int steps = (int)Math.Round(len / DeckReachability.DefaultStep);
        for (int i = 0; i <= steps; i++)
        {
            pts.Add(new DeckReachability.Point(
                x0 + (dx * i / steps), y0 + (dy * i / steps)));
        }
        return pts;
    }
}
