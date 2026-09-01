using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #804 · WHERE A MAN PUTS SOMEBODY HE IS WALKING OUT. The canon pass gave the guard a line that moved the
/// sim — <i>"No? Then you walk ahead of me to the lift, and we don't make it a thing."</i> — and #833's
/// escort walked the captain in his WAKE, which is the opposite of what he now says out loud.
///
/// <para><b>Why the point comes off his ROUTE and not off his facing.</b> #833 measured the hazard from the
/// other side and wrote the number down: a target half a pace to the SIDE landed in stone at every doorway,
/// the captain slid along the jamb, and the escort ran at 34% moving without ever reaching the car. Walking
/// where he had already walked was walkable by construction. In FRONT of him there is no such ground — the
/// naive ray points squarely into shotcrete at every corner he has not turned yet — so
/// <see cref="PatrolBeat.AheadOnHisRoute"/> takes the point off the A* polyline itself, every leg of which
/// the planner proved clear at the avatar's radius.</para>
///
/// <para><b>Proven RED.</b> <see cref="TheTargetTurnsTheCornerBeforeHeDoes"/> is the one that fails on the
/// obvious wrong implementation: a ray off the facing puts the captain at (6, 0) — a metre inside the wall
/// past the corner — where the route puts him at (5, 1), round it. <see cref="ThePointIsAlwaysOnTheRoute"/>
/// fails on the same ray everywhere the route is not straight.</para>
/// </summary>
public sealed class TheCaptainIsWalkedOutInFrontTests
{
    /// <summary>A corridor that turns: five deck units east, then five north. The corner at (5, 0) is where a
    /// facing ray and a route part company.</summary>
    private static IReadOnlyList<DeckReachability.Point> TheBend =>
        [new(0, 0), new(5, 0), new(5, 5)];

    /// <summary>Down a straight corridor there is nothing to get wrong, and the point is simply a pace along
    /// it. Here so the interesting cases below are not the only thing holding the shape up.</summary>
    [Fact]
    public void DownAStraightCorridorHeIsSimplyAPaceAhead()
    {
        (double x, double y) = PatrolBeat.AheadOnHisRoute(
            2.0, 0.0, 0.0, [new DeckReachability.Point(0, 0), new DeckReachability.Point(10, 0)], 1.3);

        Assert.Equal(3.3, x, 6);
        Assert.Equal(0.0, y, 6);
    }

    /// <summary>
    /// THE CORNER, WHICH IS THE WHOLE POINT. He is a pace short of the bend and still facing east; the man he
    /// is walking out is a pace and a bit further on, which is ROUND the corner and not through the wall.
    /// </summary>
    [Fact]
    public void TheTargetTurnsTheCornerBeforeHeDoes()
    {
        (double x, double y) = PatrolBeat.AheadOnHisRoute(4.0, 0.0, 0.0, TheBend, 2.0);

        // One unit of the two spends the last of the eastward leg; the other goes north.
        Assert.Equal(5.0, x, 6);
        Assert.Equal(1.0, y, 6);

        // …and the naive answer, which is what this exists to refuse.
        Assert.NotEqual(6.0, x, 6);
    }

    /// <summary>The route runs out before the pace does, at the car — so the captain arrives at the doors
    /// rather than walking through them.</summary>
    [Fact]
    public void ThePaceIsClampedAtTheEndOfTheRoute()
    {
        (double x, double y) = PatrolBeat.AheadOnHisRoute(5.0, 4.0, Math.PI / 2, TheBend, 10.0);

        Assert.Equal(5.0, x, 6);
        Assert.Equal(5.0, y, 6);
    }

    /// <summary>No route at all — he is standing at the car, or a re-plan has just failed. The fallback is
    /// the ray off his facing, and it is safe precisely because a man with no route is not about to turn a
    /// corner.</summary>
    [Fact]
    public void WithNoRouteAtAllItIsTheRayOffHisFacing()
    {
        (double x, double y) = PatrolBeat.AheadOnHisRoute(3.0, 7.0, Math.PI / 2, null, 1.3);
        Assert.Equal(3.0, x, 6);
        Assert.Equal(8.3, y, 6);

        (double ex, double ey) = PatrolBeat.AheadOnHisRoute(3.0, 7.0, 0.0, [], 1.3);
        Assert.Equal(4.3, ex, 6);
        Assert.Equal(7.0, ey, 6);
    }

    /// <summary>
    /// THE LOAD-BEARING PROPERTY, swept: wherever along the bend he is standing and however far ahead the
    /// captain is walked, the point that comes back is ON the polyline. That is the whole guarantee — a point
    /// on a leg the planner cleared is walkable, and a point off it is the thing that put #833's escort at
    /// 34% moving.
    /// </summary>
    [Fact]
    public void ThePointIsAlwaysOnTheRoute()
    {
        var bad = new List<string>();
        int asked = 0;

        for (double t = 0; t <= 10.0; t += 0.25)
        {
            // A guard somewhere along the bend, and a captain slightly off it — which is the real case, since
            // the two of them are stepped by collision and not by the plan.
            (double gx, double gy) = t <= 5.0 ? (t, 0.0) : (5.0, t - 5.0);
            for (double jitter = -0.2; jitter <= 0.2; jitter += 0.2)
            {
                for (double ahead = 0.25; ahead <= 4.0; ahead += 0.25)
                {
                    asked++;
                    (double px, double py) = PatrolBeat.AheadOnHisRoute(
                        gx + jitter, gy - jitter, 0.0, TheBend, ahead);
                    double off = DistanceToPolyline(px, py, TheBend);
                    if (off > 1e-9)
                    {
                        bad.Add($"  guard at ({gx + jitter:F2}, {gy - jitter:F2}) + {ahead:F2} du landed " +
                                $"({px:F2}, {py:F2}), {off:F3} du off the route he planned.");
                    }
                }
            }
        }

        Assert.True(bad.Count == 0, $"{bad.Count} finding(s):\n{string.Join("\n", bad)}");
        Assert.True(asked > 500, $"only {asked} placements were asked for — that is not a sweep.");
    }

    /// <summary>…and he does not walk BACKWARDS down the route. A pace ahead is a pace toward the car, at
    /// every station along it.</summary>
    [Fact]
    public void TheCaptainIsNeverPutBehindHim()
    {
        int asked = 0;
        for (double t = 0; t < 9.5; t += 0.25)
        {
            (double gx, double gy) = t <= 5.0 ? (t, 0.0) : (5.0, t - 5.0);
            (double px, double py) = PatrolBeat.AheadOnHisRoute(gx, gy, 0.0, TheBend, PatrolBeat.AheadDu);

            // Progress along the bend, measured as distance travelled from its start.
            double his = t, hers = px <= 5.0 && py <= 1e-9 ? px : 5.0 + py;
            Assert.True(hers > his - 1e-9,
                $"a guard {his:F2} du along his own route put the captain {hers:F2} du along it — behind him.");
            asked++;
        }
        Assert.True(asked > 30);
    }

    private static double DistanceToPolyline(
        double x, double y, IReadOnlyList<DeckReachability.Point> route)
    {
        double best = double.PositiveInfinity;
        for (int i = 0; i + 1 < route.Count; i++)
        {
            (double ax, double ay) = (route[i].X, route[i].Y);
            double ex = route[i + 1].X - ax, ey = route[i + 1].Y - ay;
            double len2 = (ex * ex) + (ey * ey);
            double s = len2 <= 1e-12 ? 0 : Math.Clamp((((x - ax) * ex) + ((y - ay) * ey)) / len2, 0, 1);
            double dx = x - (ax + (ex * s)), dy = y - (ay + (ey * s));
            best = Math.Min(best, Math.Sqrt((dx * dx) + (dy * dy)));
        }
        return best;
    }
}
