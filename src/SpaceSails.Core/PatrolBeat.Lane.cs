using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: #831's lane, the circuit walked on it, and the timing every leg is spent at (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── #831 · THE LANE ───────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "they should respect right side traffic, and not walk in the middle of the corridor."
    //
    // Three things it buys, and the owner named all three: it reads as PROCEDURE, in the same register as
    // the checkpoints; two guards passing each other pass cleanly on opposite sides instead of meeting in
    // the middle; and a walker who holds the centre of a corridor is — visibly, learnably — somebody who does
    // not know the house rules. (The captain's own lane-keeping as a badge tell is design headroom and is
    // deliberately not built here.)
    //
    // IT OFFSETS THE WAYPOINT LINE AND NEVER THE COLLISION. The A* is the same A* over the same field; what
    // changes is where along the width of a corridor the walked line runs. At a corner, a doorway or a rib
    // mouth the offset is simply given up and the round takes the middle — a lane is a preference, and a
    // preference that could wedge a man against a jamb would be a wall.

    /// <summary>#831 · Over how many waypoints the lane is EASED IN and out. A route is a lattice path at
    /// half a deck unit a cell, and an offset that appeared all at once would move a body two du sideways in
    /// one step — a diagonal lunge across a corridor that the slide refuses at every jamb it meets, which is
    /// a snag cycle wearing a feature's clothes. Four cells taper the two du into steps no longer than the
    /// walk's own, so a man drifts onto his side of the corridor the way a person does.</summary>
    public const int LaneEaseCells = 4;

    /// <summary>
    /// #831 · THE ROUND, PUT ON ITS OWN SIDE OF THE CORRIDOR.
    ///
    /// <para>Every waypoint in the middle of a STRAIGHT run is shifted to the right hand of travel by
    /// <see cref="LaneOffsetDu"/>; every waypoint at a turn keeps the centre line, and so do both ends. The
    /// shift is EASED in over <see cref="LaneEaseCells"/> — see that constant — and a shift a body would not
    /// fit on is stepped back down until it does, so the offset is never allowed to put a man inside
    /// anything and never asks him to lunge.</para>
    ///
    /// <para><b>Right of travel, in this game's own frame.</b> +y is up on the deck (the renderer flips it
    /// for the screen), so the right hand of a heading (ax, ay) is (ay, −ax): a man walking EAST walks on the
    /// SOUTH side of the corridor, and the man coming back walks on the north side. That is the sentence a
    /// test asserts, rather than this formula copied into it.</para>
    ///
    /// <para>Plan-time and not per-frame: it runs once on the route the leg was planned with, over a list
    /// that is already in hand. Lab 45's frame budget is untouched.</para>
    ///
    /// <para>#870 · <b>The conductor.</b> Four banners used to run down the inside of this method; they are
    /// four named steps now, called IN ORDER, and the order is the whole content of the lane — a shift eased
    /// before it is known which waypoints are on a straight run at all lays a different line down the same
    /// corridor. <c>EveryLaneItLaysHashesTheSameTests</c> pins that order as twenty-three digests taken on
    /// the code as it stood before the split.</para>
    /// </summary>
    public static IReadOnlyList<DeckReachability.Point> KeepRight(
        IReadOnlyList<DeckReachability.Point>? route,
        IReadOnlyList<SurfaceCollision.Segment>? walls,
        double radius)
    {
        int n = route?.Count ?? 0;
        if (route is null || n < 3)
        {
            return route ?? [];
        }

        var rightX = new double[n];
        var rightY = new double[n];
        var straight = new bool[n];

        WhichWaypointsAreOnAStraightRun(route, n, straight, rightX, rightY);
        int[] ease = HowFarFromTheNearestTurn(straight, n);
        double[] scale = HowFarTheOffsetIsTaken(straight, ease, n);
        AndTheGroundMayRefuseAnyOfIt(route, rightX, rightY, scale, n, radius, walls);
        return TheLineAsItIsWalked(route, rightX, rightY, scale, n);
    }

    /// <summary>#831 · WHICH WAYPOINTS ARE ON A STRAIGHT RUN AT ALL, and which way is right there. The
    /// right hand of a heading (ax, ay) is (ay, −ax) — see <see cref="KeepRight"/> for why that is south of
    /// an eastbound walk on this deck. A waypoint that is not on a straight run keeps its zero normal and is
    /// never offset by anything downstream.</summary>
    private static void WhichWaypointsAreOnAStraightRun(
        IReadOnlyList<DeckReachability.Point> route, int n,
        bool[] straight, double[] rightX, double[] rightY)
    {
        for (int i = 1; i < n - 1; i++)
        {
            double bx = route[i].X - route[i - 1].X, by = route[i].Y - route[i - 1].Y;
            double ax = route[i + 1].X - route[i].X, ay = route[i + 1].Y - route[i].Y;
            double bl = Math.Sqrt((bx * bx) + (by * by)), al = Math.Sqrt((ax * ax) + (ay * ay));
            if (bl < 1e-9 || al < 1e-9)
            {
                continue;
            }
            bx /= bl; by /= bl; ax /= al; ay /= al;

            if (ACornerGivesTheLaneUp(bx, by, ax, ay))
            {
                continue;
            }
            straight[i] = true;
            rightX[i] = ay;
            rightY[i] = -ax;
        }
    }

    /// <summary>#831 · A CORNER GIVES THE LANE UP. This is the doorway clause and the mouth clause both —
    /// the route turns at exactly those places — and it is what keeps the offset from ever being asked to
    /// hold through a 6.4 du leaf.</summary>
    private static bool ACornerGivesTheLaneUp(double bx, double by, double ax, double ay) =>
        (bx * ax) + (by * ay) < LaneStraightEnough;

    /// <summary>#831 · HOW FAR EACH ONE IS FROM THE NEAREST TURN, in cells, so the shift can be eased. Two
    /// passes, forward and back, and the smaller of the two runs wins — which is what makes a straight
    /// stretch shorter than twice the ramp taper from BOTH ends rather than reaching full offset in the
    /// middle of it.</summary>
    private static int[] HowFarFromTheNearestTurn(bool[] straight, int n)
    {
        var ease = new int[n];
        int run = 0;
        for (int i = 0; i < n; i++)
        {
            run = straight[i] ? run + 1 : 0;
            ease[i] = run;
        }
        run = 0;
        for (int i = n - 1; i >= 0; i--)
        {
            run = straight[i] ? run + 1 : 0;
            ease[i] = Math.Min(ease[i], run);
        }
        return ease;
    }

    /// <summary>#831 · HOW FAR THE OFFSET IS TAKEN AT EACH ONE, before the ground has been asked at all.
    /// The fraction of <see cref="LaneOffsetDu"/> this waypoint would like, if nothing were in the
    /// way.</summary>
    private static double[] HowFarTheOffsetIsTaken(bool[] straight, int[] ease, int n)
    {
        var scale = new double[n];
        for (int i = 1; i < n - 1; i++)
        {
            scale[i] = straight[i] ? Math.Min(1.0, ease[i] / (double)LaneEaseCells) : 0.0;
        }
        return scale;
    }

    /// <summary>
    /// #831 · …AND THE GROUND MAY REFUSE ANY OF IT, ASKED OF THE WHOLE HOP AND NOT ONLY OF THE WAYPOINT.
    ///
    /// <para>Takes the wanted offsets down until every hop on the line is one a body walks. Nothing is added
    /// here — the only thing this step can do to <paramref name="scale"/> is make it smaller.</para>
    /// </summary>
    private static void AndTheGroundMayRefuseAnyOfIt(
        IReadOnlyList<DeckReachability.Point> route,
        double[] rightX, double[] rightY, double[] scale, int n, double radius,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        // This is the clause the first cut of this lane did not have, and three of this generator's
        // twenty-five patrolled floors said so: an offset moves a man sideways BETWEEN two points the A*
        // proved, and it is the ground between them that a wedged guard ends up standing in. Checking the
        // waypoints alone let a laned line clip a corner the search had cleared, and a man who arrives there
        // is somewhere the planner cannot plan FROM — every subsequent A*, to every stop on that floor, came
        // back null and the round never went anywhere again. <see cref="AutoWalk.Along"/> says out loud that
        // whoever hands it a line owns the claim that consecutive points are walkable; this is that claim
        // being earned rather than assumed.
        //
        // Stepped back down a notch at a time rather than dropped, so a waypoint beside something bulky
        // rejoins its neighbours instead of jumping to the middle and back. The point BEFORE gives way when
        // the later one has nothing left to give, and the fuel bounds the whole thing: every turn of the
        // inner loop spends a notch off a sum that starts below n, and at zero the line is the A*'s own.
        int fuel = n * (LaneEaseCells + 1);
        for (bool again = true; again && fuel > 0;)
        {
            again = false;
            for (int i = 1; i < n; i++)
            {
                while (fuel > 0)
                {
                    (double ax, double ay) = At(route, rightX, rightY, scale, i - 1);
                    (double bx, double by) = At(route, rightX, rightY, scale, i);
                    if (!SurfaceCollision.Blocked(bx, by, radius, walls)
                        && HopIsWalkable(ax, ay, bx, by, radius, walls))
                    {
                        break;
                    }

                    if (scale[i] > 0)
                    {
                        scale[i] = Math.Max(0, scale[i] - (1.0 / LaneEaseCells));
                    }
                    else if (scale[i - 1] > 0)
                    {
                        scale[i - 1] = Math.Max(0, scale[i - 1] - (1.0 / LaneEaseCells));
                    }
                    else
                    {
                        // Both ends are back on the centre line and it is STILL refused: this is the A*'s
                        // own segment, and a lane is not the thing that gets to have an opinion about it.
                        break;
                    }
                    fuel--;
                    again = true;
                }
            }
        }
    }

    /// <summary>#831 · THE LINE AS IT IS ACTUALLY WALKED — every waypoint moved by however much of the
    /// offset survived the ground, both ends of the leg included (their scale is zero, so a lane never moves
    /// a STOP).</summary>
    private static List<DeckReachability.Point> TheLineAsItIsWalked(
        IReadOnlyList<DeckReachability.Point> route,
        double[] rightX, double[] rightY, double[] scale, int n)
    {
        var laned = new List<DeckReachability.Point>(n);
        for (int i = 0; i < n; i++)
        {
            (double x, double y) = At(route, rightX, rightY, scale, i);
            laned.Add(new DeckReachability.Point(x, y));
        }
        return laned;
    }

    /// <summary>#831 · Where waypoint <paramref name="i"/> sits once the lane has had its say.</summary>
    private static (double X, double Y) At(
        IReadOnlyList<DeckReachability.Point> route,
        double[] rightX, double[] rightY, double[] scale, int i) =>
        (route[i].X + (rightX[i] * LaneOffsetDu * scale[i]),
         route[i].Y + (rightY[i] * LaneOffsetDu * scale[i]));

    /// <summary>
    /// #831 · Is the GROUND BETWEEN two laned waypoints ground a body walks on?
    ///
    /// <para>Probed at half a body's own width, and each probe asks about a body <b>fattened by half the
    /// spacing it was probed at</b> — which is what turns a sample into a proof rather than a hope. Every
    /// point on the hop is within half a spacing of some probe, so if the fat disc at every probe is clear
    /// then the real disc everywhere between them is clear too. A sampled test without the fattening would
    /// answer "no wall at these few spots", which is the kind of green that says nothing.</para>
    ///
    /// <para>A hop is about a lattice cell long, so this is a handful of indexed queries per waypoint, once
    /// per leg, on a frame the man is standing still on anyway (§ Lab 45).</para>
    /// </summary>
    private static bool HopIsWalkable(
        double ax, double ay, double bx, double by, double radius,
        IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double dx = bx - ax, dy = by - ay;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        int probes = Math.Max(1, (int)Math.Ceiling(len / Math.Max(1e-6, radius / 2.0)));
        double fat = radius + (len / probes / 2.0);
        for (int k = 0; k <= probes; k++)
        {
            double t = k / (double)probes;
            if (SurfaceCollision.Blocked(ax + (dx * t), ay + (dy * t), fat, walls))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// THE CIRCUIT, TAKEN OFF THE FLOOR PLAN AND NEVER OFF A CONSTANT.
    ///
    /// <para>The lift, then every rib mouth in ASCENDING X, and after each mouth the room on that rib that
    /// stands furthest from the spine. Three things about that are load-bearing:</para>
    ///
    /// <list type="bullet">
    /// <item><b>Every stop is published geometry.</b> <see cref="UndergroundComplex.ShaftAt"/>,
    /// <c>FloorPlan.Ribs</c> (published by #587 precisely so nothing outside the generator has to do
    /// arithmetic that copies the placement) and <c>FloorPlan.RoomCentres</c>. This file computes no
    /// position of its own, which is §13.15 — a seam that re-derived a fact about a building it does not own
    /// would be the mirrored-constant bug this ground keeps paying for.</item>
    /// <item><b>Sorted before it is walked.</b> #587's own lesson, one floor along: a list built by
    /// appending is not a list in order, and a round that jumped back and forth along the spine would be
    /// unlearnable — which is the whole feature.</item>
    /// <item><b>Every stop is a place the A* audit already walks.</b> Room centres are what
    /// <c>HiveInterior</c> hangs its SEARCH THE ROOM consoles on, and §13.1's sweep proves every one of them
    /// is reachable from the car on every floor of every site. A beat built out of them is walkable by
    /// construction rather than by hope — and there is a guard that walks it anyway.</item>
    /// </list>
    ///
    /// <h3>#831 · AND EVERY STOP SNAPS TO ITS CHECKPOINT</h3>
    ///
    /// <para>Owner: <i>"why would it just stand there if there is no inspection point etc"</i> → <i>"they
    /// actually in real life like have these check points they electronically sign on rounds to prove they
    /// did their round."</i> So the coordinates above are where the round WANTS to be, and the coordinates it
    /// walks to are the square the nearest watchclock station is signed from (<see cref="PointFor"/>) — a
    /// pace and a half off a wall, facing a plate. A stop that finds no wall within
    /// <see cref="CheckpointReachDu"/> keeps its own place and says so by carrying a null
    /// <see cref="Stop.Point"/>; the audit counts those, because a stop nobody can explain is the thing this
    /// whole issue is about.</para>
    /// </summary>
    /// <param name="floor">The floor, as Core built it.</param>
    /// <param name="field">The site's envelope — the shaft's position comes from it.</param>
    public static IReadOnlyList<Stop> Circuit(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        IReadOnlyList<Stop> wanted = WantedCircuit(floor, field);
        List<SurfaceCollision.Segment> blockers = Blockers(in floor);

        var snapped = new List<Stop>(wanted.Count);
        for (int i = 0; i < wanted.Count; i++)
        {
            Stop stop = wanted[i];
            snapped.Add(PointFor(in stop, i + 1, in floor, blockers) is { } at
                ? stop with { X = at.StandX, Y = at.StandY, Point = at }
                : stop);
        }
        return snapped;
    }

    /// <summary>#831 · The circuit as the floor plan alone decides it, BEFORE the stations move it. Split out
    /// so the numbering of the stations is a fact about the PLACE — station 3 is station 3 on every watch —
    /// and so the snap has exactly one caller.</summary>
    private static IReadOnlyList<Stop> WantedCircuit(
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        var stops = new List<Stop>();

        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(field);

        // The car. It is the first stop on every round because it is the first thing anybody checks and the
        // one place on the floor a captain has to come back to.
        stops.Add(new Stop(shaftX, shaftY + 1.0, "the car"));

        // The ribs, in x order — SORTED, not as the plan happens to list them.
        var ribs = new List<UndergroundComplex.Rib>(floor.Ribs ?? []);
        ribs.Sort((a, b) => a.X.CompareTo(b.X));

        var taken = new HashSet<int>();
        foreach (UndergroundComplex.Rib rib in ribs)
        {
            // The mouth: where the cross corridor opens off the spine. This is the square a captain watches
            // from, so it has to be on the round.
            stops.Add(new Stop(rib.X, shaftY, $"the mouth at x{rib.X:F0}"));

            // …and the far room down it. Which room "belongs" to this rib is decided by nearness in x and
            // by which side of the spine the rib runs — both facts the plan publishes.
            (int Index, double X, double Y)? room = FurthestRoomOn(floor, rib, shaftY, taken);
            if (room is { } far)
            {
                taken.Add(far.Index);
                stops.Add(new Stop(far.X, far.Y, $"the far room off x{rib.X:F0}"));
            }
        }

        return stops;
    }

    /// <summary>The room furthest down one rib: nearest to the rib in x, on the rib's own side of the spine,
    /// and the deepest of those. The ordinal comes back with it so the caller can CLAIM it — two ribs
    /// sharing a stop would be a round that doubles back on a room it has just left.</summary>
    private static (int Index, double X, double Y)? FurthestRoomOn(
        in UndergroundComplex.FloorPlan floor, UndergroundComplex.Rib rib, double spineY, HashSet<int> taken)
    {
        (int Index, double X, double Y)? best = null;
        double bestDepth = -1;

        IReadOnlyList<(double X, double Y)> rooms = floor.RoomCentres ?? [];
        for (int i = 0; i < rooms.Count; i++)
        {
            if (taken.Contains(i))
            {
                continue;
            }

            (double rx, double ry) = rooms[i];

            // The rib's own side of the spine. Down means toward the deep field, away from the landing band,
            // which is the flag the plan carries for exactly this question.
            bool below = ry < spineY;
            if (below != rib.Down)
            {
                continue;
            }

            // Rooms hang off the rib they were placed along, so a room more than a room's width away in x is
            // somebody else's. RoomWidthDu is the generator's own number, read rather than guessed.
            if (Math.Abs(rx - rib.X) > UndergroundComplex.RoomWidthDu)
            {
                continue;
            }

            double depth = Math.Abs(ry - spineY);
            if (depth > bestDepth)
            {
                bestDepth = depth;
                best = (i, rx, ry);
            }
        }

        return best;
    }

    /// <summary>
    /// THE ROUND AS IT IS ACTUALLY WALKED THIS WATCH — the circuit, turned over by the shift.
    ///
    /// <para>Two things rotate, and both are seeded on (site, floor, watch): which DIRECTION the round runs,
    /// and which stop it starts from. That is the whole of "rotating guards": the same floor, the same
    /// stops, a different round — so a captain who learned one shift's round has learned the floor and not
    /// the answer, and has to watch again.</para>
    ///
    /// <para>Every guard on the floor shares this ONE list and differs only by which leg they are on
    /// (<see cref="StartLeg"/>) — the sweep team's own idiom, and for its reason: <i>being hidden from has
    /// to be legible.</i> Two rounds with two different routes would be two things to learn at once.</para>
    /// </summary>
    public static IReadOnlyList<Stop> BeatFor(
        string bodyId, int level, long watch,
        in UndergroundComplex.FloorPlan floor, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        IReadOnlyList<Stop> circuit = Circuit(floor, field);
        if (circuit.Count == 0)
        {
            return circuit;
        }

        var walked = new List<Stop>(circuit);
        if (DiceRule.Roll(DiceRule.Seed($"hive:patrol:dir:{bodyId}:{level}", watch), 2).Face == 1)
        {
            walked.Reverse();
        }

        int offset = DiceRule.Roll(
            DiceRule.Seed($"hive:patrol:from:{bodyId}:{level}", watch), walked.Count).Face - 1;
        if (offset == 0)
        {
            return walked;
        }

        var rotated = new List<Stop>(walked.Count);
        for (int i = 0; i < walked.Count; i++)
        {
            rotated.Add(walked[(offset + i) % walked.Count]);
        }
        return rotated;
    }

    /// <summary>
    /// #804 · HOW BIG A LATTICE ONE LEG IS WALKED OVER, and why it is not the whole floor.
    ///
    /// <para><see cref="AutoWalk.BoundsFor"/> spans every wall it is handed, which is right for a captain
    /// clicking an arbitrary spot and wrong for a guard walking forty du down a rib: a floor-sized lattice
    /// at <see cref="DeckReachability.DefaultStep"/> is a third of a million cells, and this repo runs in
    /// WASM where a Debug build is a hundred times slower than the machine this is written on. A round that
    /// planned one of those every time it reached a stop would hitch the frame it arrived on.</para>
    ///
    /// <para><b>The margin is the whole of the safety.</b> A leg may bulge around a room to reach the next
    /// stop, so the box is the two stops plus a room's own depth on every side — derived from
    /// <c>RoomHeightDu</c> and the corridor's width rather than typed, so a generator that grows its rooms
    /// grows this with them. Clamped to the field, because nothing is built outside it.</para>
    ///
    /// <para><b>And it lives here so the audit and the game ask ONE question.</b> If the client shrank the
    /// box and the sweep walked the whole floor, the sweep would be proving a route the game never plans —
    /// two pathfinders agreeing only by luck, which is the drift <c>DeckReachability</c>'s own lattice was
    /// consolidated to stop.</para>
    /// </summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) LatticeFor(
        in Stop from, in Stop to, in SurfaceLayout.Field field)
    {
        double margin = UndergroundComplex.RoomHeightDu + (UndergroundComplex.CorridorHalf * 2);
        double minX = Math.Max(field.LeftX, Math.Min(from.X, to.X) - margin);
        double maxX = Math.Min(field.RightX, Math.Max(from.X, to.X) + margin);
        double minY = Math.Max(field.BottomY, Math.Min(from.Y, to.Y) - margin);
        double maxY = Math.Min(field.LandingBandY, Math.Max(from.Y, to.Y) + margin);
        return (minX, minY, maxX, maxY);
    }

    /// <summary>Where the <paramref name="index"/>th guard starts on a beat of <paramref name="legs"/> stops
    /// — spread evenly around it, so two of them cover the floor instead of walking in a queue. The sweep
    /// team's arithmetic, because it is the same problem.</summary>
    public static int StartLeg(int legs, int index, int count) =>
        legs <= 0 ? 0 : index * legs / Math.Max(1, count) % legs;

    /// <summary>
    /// How fast a round walks, deck units per second. SLOWER than the captain's walk on purpose: a captain
    /// can always out-walk a guard and it never helps, because the thing they are bad at is not speed.
    /// FLAGGED for the owner's tuning — this and <see cref="StandSeconds"/> are the whole timing window.
    /// </summary>
    public const double WalkSpeed = 3.2;

    /// <summary>How long they stand at a stop before walking on. THE NUMBER THE FEATURE IS ABOUT: it is the
    /// gap a captain watches for and steps into. Long enough to see from the far end of a corridor, short
    /// enough that waiting is a decision rather than a wait. FLAGGED.</summary>
    public const double StandSeconds = 5.0;

    /// <summary>Close enough to a stop to be standing at it. The A* leg lands the guard adjacent to the
    /// stop rather than on it (a room centre has a console on it), so this is the same "arrived" tolerance
    /// the sweep team's route uses.</summary>
    public const double AtTheStopDu = 1.5;

    /// <summary>#832 · How many times a leg may be re-planned after the ground refuses a step before the
    /// round gives up on that stop and walks to the next one. A snag is not an arrival and must not buy a
    /// stand — but a stop nothing can reach must not be ground at forever either, and an A* plan per frame
    /// is not free in WASM. Small on purpose: the audit (§13.1) says every leg of every round on every floor
    /// this generator builds connects, so this is the belt on top of the braces.</summary>
    public const int RePlansPerLeg = 6;

    /// <summary>#858 · How much of the NEXT leg's A* a standing guard walks per frame
    /// (<see cref="AutoWalk.Planner"/>), in lattice cells.
    ///
    /// <para>Lab 45 measured the plan the round asks for at a median 1.6–2.2 ms and a worst 6.4 ms — 38.6%
    /// of a 60 fps frame — spent whole on the frame he leaves a stop, which is the one number in that lab
    /// that can miss a frame. He is standing for <see cref="StandSeconds"/> either way; this is the rate
    /// that gets the same work done during it.</para>
    ///
    /// <para><b>Derived, not tuned.</b> The stand is 5 s ≈ 300 frames, and the biggest lattice
    /// <see cref="LatticeFor"/> poses on the floors this generator builds is 29,002 cells (Lab 45 §C). At
    /// 128 a frame the whole of that lattice is walked in ~227 frames — inside the stand with a quarter of
    /// it to spare — while one frame's share of that worst 6.4 ms plan is about a fiftieth of it. Being a
    /// CELL budget rather than a millisecond one is the point: it means the same thing in WASM, where the
    /// clock this was measured on does not.</para></summary>
    public const int PlanCellsAFrame = 128;
}
