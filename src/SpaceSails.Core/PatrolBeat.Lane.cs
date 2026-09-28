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
}
