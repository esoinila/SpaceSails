using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// #1074 beat 5 · THE HULL ON THE GROUND — where she is parked and where the three fixtures stand in her.
//
// Core places it, because the client's habit of laying fixtures by eye is the bug class this repo has paid
// for more than once: the geometry is a pure function of the fence round the shed, the shared field and an
// OBSTRUCTION QUESTION the caller answers about the ground it has actually built, so a test can walk every
// ground in the game and ask whether the hull fits.
public static partial class ReturningShuttle
{
    /// <summary>The hull's outer length along the ground, in deck units. FLAGGED tuning: long enough to read
    /// as a ship beside the shed, short enough to fit between a fence and a field edge.</summary>
    public const double HullLength = 14.0;

    /// <summary>The hull's outer breadth.</summary>
    public const double HullBreadth = 6.0;

    /// <summary>The width of the airlock gap in her long wall — wide enough to walk through with the
    /// captain's own collision radius to spare.</summary>
    public const double GapWidth = 3.0;

    /// <summary>The clearance the hull keeps from every wall and fixture already on the ground.</summary>
    public const double Berth = 2.5;

    /// <summary>The clearance the hull keeps from the way home.</summary>
    public const double HomeBerth = 14.0;

    /// <summary>The hull on the ground. <see cref="Walls"/> close the rectangle but for one gap in the long
    /// wall nearest the way home; the three stations stand inside.</summary>
    public readonly record struct Hull(
        double CentreX, double CentreY,
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        (double X, double Y) Airlock, (double X, double Y) Rack, (double X, double Y) Log,
        double PlateX, double PlateY);

    /// <summary>
    /// Park her. Tries a deterministic ladder of bearings and distances round the fence — beside the sign's
    /// approach first, then the far side — and answers the first spot where the whole rectangle (and its
    /// berth) is inside the field, clear of the way home and not blocked.
    /// </summary>
    /// <param name="fenceX">The fence's centre — the shed's.</param>
    /// <param name="fenceY">…</param>
    /// <param name="fenceRadius">The ring's radius.</param>
    /// <param name="field">The shared field envelope.</param>
    /// <param name="blocked">Does the rectangle (x0, y0, x1, y1), already inflated by the berth, hit anything
    /// the ground has — a wall, a fixture, a label? The caller answers for the ground it built.</param>
    /// <returns>The hull, or null when the ground has nowhere to put her (the board row then stands alone).</returns>
    public static Hull? Park(
        double fenceX, double fenceY, double fenceRadius, in SurfaceLayout.Field field,
        Func<double, double, double, double, bool> blocked)
    {
        ArgumentNullException.ThrowIfNull(blocked);

        double home = Math.Atan2(field.TopY - fenceY, field.HomeX - fenceX);
        double[] bearings = [Math.PI / 2, -Math.PI / 2, 3 * Math.PI / 4, -3 * Math.PI / 4, Math.PI, Math.PI / 4, -Math.PI / 4];
        double[] reaches = [fenceRadius + 12, fenceRadius + 18, fenceRadius + 24, fenceRadius + 32];

        foreach (double reach in reaches)
        {
            foreach (double bearing in bearings)
            {
                double cx = fenceX + (reach * Math.Cos(home + bearing));
                double cy = fenceY + (reach * Math.Sin(home + bearing));
                if (Fits(cx, cy, field, blocked))
                {
                    return Build(cx, cy, field);
                }
            }
        }
        return null;
    }

    private static bool Fits(
        double cx, double cy, in SurfaceLayout.Field field, Func<double, double, double, double, bool> blocked)
    {
        double hx = (HullLength / 2) + Berth, hy = (HullBreadth / 2) + Berth;
        double x0 = cx - hx, x1 = cx + hx, y0 = cy - hy, y1 = cy + hy;

        // inside the field, with a pace to spare. The ground runs DOWN from the tube: TopY (-20) is the
        // surface line and BottomY (-280) the far edge, so "inside" is BottomY below and the landing band above.
        if (x0 < field.LeftX + 1 || x1 > field.RightX - 1
            || y0 < field.BottomY + 1 || y1 > field.LandingBandY - 1)
        {
            return false;
        }

        // …and clear of the way home
        double dx = cx - field.HomeX, dy = cy - field.TopY;
        if ((dx * dx) + (dy * dy) < HomeBerth * HomeBerth)
        {
            return false;
        }

        return !blocked(x0, y0, x1, y1);
    }

    private static Hull Build(double cx, double cy, in SurfaceLayout.Field field)
    {
        double hl = HullLength / 2, hb = HullBreadth / 2, g = GapWidth / 2;

        // The long wall nearest the way home carries the gap — a captain steps in from the side he came.
        bool gapOnTop = Math.Abs((cy - hb) - field.TopY) <= Math.Abs((cy + hb) - field.TopY);
        double gy = gapOnTop ? cy - hb : cy + hb;
        double ky = gapOnTop ? cy + hb : cy - hb;

        var walls = new List<SurfaceLayout.Wall>
        {
            new(cx - hl, gy, cx - g, gy, IsHull: true),
            new(cx + g, gy, cx + hl, gy, IsHull: true),
            new(cx - hl, ky, cx + hl, ky, IsHull: true),
            new(cx - hl, cy - hb, cx - hl, cy + hb, IsHull: true),
            new(cx + hl, cy - hb, cx + hl, cy + hb, IsHull: true),
        };

        // The airlock stands just inside the gap; the rack and the log desk at the two ends of her.
        double inward = gapOnTop ? 1.4 : -1.4;
        return new Hull(
            cx, cy, walls,
            Airlock: (cx, gy + inward),
            Rack: (cx - hl + 2.0, cy),
            Log: (cx + hl - 2.0, cy),
            PlateX: cx, PlateY: gapOnTop ? gy - 1.6 : gy + 1.6);
    }

    /// <summary>Do two segments, or a segment and a rectangle, touch? The one geometry question every
    /// caller's <c>blocked</c> needs when it holds the ground's walls as segments.</summary>
    public static bool SegmentHitsRect(
        double ax, double ay, double bx, double by, double x0, double y0, double x1, double y1)
    {
        // Liang–Barsky against the rectangle.
        double t0 = 0, t1 = 1, dx = bx - ax, dy = by - ay;
        double[] p = [-dx, dx, -dy, dy];
        double[] q = [ax - x0, x1 - ax, ay - y0, y1 - ay];
        for (int i = 0; i < 4; i++)
        {
            if (p[i] == 0)
            {
                if (q[i] < 0)
                {
                    return false;
                }
                continue;
            }
            double t = q[i] / p[i];
            if (p[i] < 0)
            {
                if (t > t1) { return false; }
                if (t > t0) { t0 = t; }
            }
            else
            {
                if (t < t0) { return false; }
                if (t < t1) { t1 = t; }
            }
        }
        return true;
    }
}
