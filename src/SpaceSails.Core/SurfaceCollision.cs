namespace SpaceSails.Core;

/// <summary>
/// PR-324 · The one wall law for everyone on the walked ground (owner, live 2026-07-18: "let's not let
/// the Reevers move through walls here :-D … Now the reevers see through wall and move through them").
/// The maze must be law for the many the same way it is for the captain: this is the SINGLE collision +
/// line-of-sight primitive the player's avatar and the Old Ones both obey, so a wall stops a shamble
/// exactly as it stops a boot, and a slab of stone between hunter and quarry breaks the chase.
///
/// <para>Crude by design — the deck-plan's own idiom, no pathfinding library and no raycasting engine.
/// <see cref="Slide"/> is axis-separated bump-and-slide (the very move <c>DeckPlan.Move</c> makes for the
/// captain); <see cref="HasLineOfSight"/> is an exact segment-vs-segment test of the sightline against
/// the wall segments. Both take the SAME <see cref="Segment"/> list the renderer draws, so there is one
/// source of truth and a Core test can pin it.</para>
/// </summary>
public static class SurfaceCollision
{
    /// <summary>A wall segment in deck units — the collidable/opaque line the captain and the Reevers
    /// alike must respect. Mirrors a <c>DeckPlan.Wall</c>'s endpoints (dropping the render-only flags).</summary>
    public readonly record struct Segment(double X1, double Y1, double X2, double Y2);

    /// <summary>Perpendicular distance from a point to a wall segment — the exact check
    /// <c>DeckPlan.DistanceToSegment</c> runs for the avatar, lifted here so both movers share it.</summary>
    public static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double lengthSq = (dx * dx) + (dy * dy);
        double t = lengthSq > 0 ? System.Math.Clamp((((px - x1) * dx) + ((py - y1) * dy)) / lengthSq, 0, 1) : 0;
        double cx = x1 + (t * dx), cy = y1 + (t * dy);
        return System.Math.Sqrt(((px - cx) * (px - cx)) + ((py - cy) * (py - cy)));
    }

    /// <summary>True when a body of the given <paramref name="radius"/> at (<paramref name="x"/>,
    /// <paramref name="y"/>) is touching any wall — the captain's own collision test.</summary>
    public static bool Blocked(double x, double y, double radius, IReadOnlyList<Segment>? walls)
    {
        if (walls is null)
        {
            return false;
        }
        foreach (Segment w in walls)
        {
            if (DistanceToSegment(x, y, w.X1, w.Y1, w.X2, w.Y2) < radius)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Bump-and-slide a body from (<paramref name="x"/>, <paramref name="y"/>) by
    /// (<paramref name="dx"/>, <paramref name="dy"/>), stopping at walls but sliding along them — the
    /// exact axis-separated move <c>DeckPlan.Move</c> makes for the avatar: try X, then try Y from the
    /// X-resolved spot, so a diagonal into a wall grazes along it instead of stopping dead.</summary>
    public static (double X, double Y) Slide(double x, double y, double dx, double dy, double radius, IReadOnlyList<Segment>? walls)
    {
        if (walls is null || walls.Count == 0)
        {
            return (x + dx, y + dy);
        }
        double nx = Blocked(x + dx, y, radius, walls) ? x : x + dx;
        double ny = Blocked(nx, y + dy, radius, walls) ? y : y + dy;
        return (nx, ny);
    }

    /// <summary>Crude grid line-of-sight: can a watcher at (<paramref name="ax"/>, <paramref name="ay"/>)
    /// see a target at (<paramref name="bx"/>, <paramref name="by"/>), or does a wall stand between them?
    /// An exact segment-vs-segment intersection of the sightline against every wall — a slab of stone
    /// breaks the look. No wall crossed → clear sight. (Windows/hull flags don't matter: a Reever can't
    /// see through any solid line the captain can't walk through.)</summary>
    public static bool HasLineOfSight(double ax, double ay, double bx, double by, IReadOnlyList<Segment>? walls)
    {
        if (walls is null || walls.Count == 0)
        {
            return true;
        }
        foreach (Segment w in walls)
        {
            if (SegmentsIntersect(ax, ay, bx, by, w.X1, w.Y1, w.X2, w.Y2))
            {
                return false;
            }
        }
        return true;
    }

    // Standard orientation-based segment intersection (p1p2 vs p3p4). Crude, allocation-free, exact.
    private static bool SegmentsIntersect(
        double p1x, double p1y, double p2x, double p2y,
        double p3x, double p3y, double p4x, double p4y)
    {
        double d1 = Cross(p3x, p3y, p4x, p4y, p1x, p1y);
        double d2 = Cross(p3x, p3y, p4x, p4y, p2x, p2y);
        double d3 = Cross(p1x, p1y, p2x, p2y, p3x, p3y);
        double d4 = Cross(p1x, p1y, p2x, p2y, p4x, p4y);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }
        return false;
    }

    // Cross product of (b-a) × (c-a): the side of line ab that point c falls on.
    private static double Cross(double ax, double ay, double bx, double by, double cx, double cy) =>
        ((bx - ax) * (cy - ay)) - ((by - ay) * (cx - ax));
}
