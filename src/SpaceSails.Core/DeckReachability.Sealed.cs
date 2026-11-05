namespace SpaceSails.Core;

/// <summary>
/// #1384 · THE CHEAP PROOF THAT A WALK IS IMPOSSIBLE, so a click on a sealed-off place can be refused without
/// the search that would otherwise flood the whole field to say so.
///
/// <para>An A* looking for a goal with no road to it can only stop when it has expanded every cell the start
/// can reach. On a hive floor that is a few thousand cells. On the open regolith it is the whole ground - every
/// standable half-du square of a 310 x 260 field, a second and a half on the desk and, in the browser that runs
/// the same loop thirty times slower, a tab that never comes back (playtest R1-F06: click the DORMANT SYNTHETIC
/// across the lift-head shed's wall, 3 of 3). But the goals that have no road are almost always in a SMALL
/// pocket - a lab behind a wall, a room behind a shut door, the inside of a ring - and a pocket is cheap to
/// finish: flood outward from the goal's side with a small budget, and if the flood runs OUT of cells without
/// ever touching the walker, there is no road, proved in a few thousand cells instead of a few hundred thousand.
/// The same flood from the walker's side answers the mirror case (a captain sealed in).</para>
///
/// <para>It only ever says <b>true</b> when it has PROVED the two ends are in different, fully explored
/// components; running out of budget is "do not know", and the caller then does exactly what it always did. It
/// walks the SAME <c>Lattice</c> (the same standability, the same no-corner-cutting rule) as the search it
/// stands in front of, so it cannot refuse a walk the search would have found.</para>
/// </summary>
public static partial class DeckReachability
{
    /// <summary>Whether no route can exist from <paramref name="from"/> to anywhere that counts as being at
    /// <paramref name="to"/> (within one lattice step, or within <paramref name="goalReachDu"/>), proved by
    /// exhausting a component of at most <paramref name="cellBudget"/> cells. False means "possibly reachable
    /// or unknown" - never "reachable".</summary>
    public static bool ProvablySealedOff(
        Point from,
        Point to,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double radius,
        (double MinX, double MinY, double MaxX, double MaxY) bounds,
        double step,
        double goalReachDu,
        int cellBudget)
    {
        ArgumentNullException.ThrowIfNull(walls);
        var grid = new Lattice(walls, radius, bounds, step);
        if (!grid.TryStandOn(from, out (int Cx, int Cy) start))
        {
            return false;   // an unwalkable start is its own, louder bug: the search reports it
        }

        // The cells that count as THERE: the one-step arrival the search always allows, plus the reach.
        (int Cx, int Cy) goal = grid.Cell(to);
        int ring = (int)Math.Ceiling(goalReachDu / step) + 1;
        var arrivals = new HashSet<(int, int)>();
        for (int dy = -ring; dy <= ring; dy++)
        {
            for (int dx = -ring; dx <= ring; dx++)
            {
                (int Cx, int Cy) c = (goal.Cx + dx, goal.Cy + dy);
                bool oneStep = Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1;
                bool inReach = false;
                if (goalReachDu > 0)
                {
                    Point p = grid.World(c);
                    inReach = ((p.X - to.X) * (p.X - to.X)) + ((p.Y - to.Y) * (p.Y - to.Y)) <= goalReachDu * goalReachDu;
                }
                if ((oneStep || inReach) && grid.Clear(c))
                {
                    arrivals.Add(c);
                }
            }
        }

        if (arrivals.Count == 0)
        {
            return true;   // nowhere that counts as there is a square a body can stand on: no walk can end there
        }
        if (arrivals.Contains(start))
        {
            return false;
        }

        // Flood each side in turn. A side that EXHAUSTS without touching the other has proved it.
        return Exhausts(grid, arrivals.ToList(), start, cellBudget, touches: arrivals.Contains, targetIsStart: false)
            || Exhausts(grid, [start], start, cellBudget, touches: arrivals.Contains, targetIsStart: true);
    }

    /// <summary>Breadth-first from <paramref name="seeds"/>; true only when the frontier ran dry inside the
    /// budget and no visited cell was a hit (the walker's own cell, or - flooding from the walker - any
    /// arrival cell).</summary>
    private static bool Exhausts(
        Lattice grid, List<(int Cx, int Cy)> seeds, (int Cx, int Cy) start, int cellBudget,
        Func<(int, int), bool> touches, bool targetIsStart)
    {
        var seen = new HashSet<(int, int)>(seeds.Select(s => (s.Cx, s.Cy)));
        var queue = new Queue<(int Cx, int Cy)>(seeds);
        while (queue.Count > 0)
        {
            if (seen.Count > cellBudget)
            {
                return false;   // not a pocket - or not one this cheap: do not know
            }
            (int Cx, int Cy) cur = queue.Dequeue();
            foreach ((int dx, int dy) in Neighbours)
            {
                (int Cx, int Cy) next = (cur.Cx + dx, cur.Cy + dy);
                if (seen.Contains((next.Cx, next.Cy)) || !grid.CanStep(cur, dx, dy))
                {
                    continue;
                }
                bool hit = targetIsStart ? touches((next.Cx, next.Cy)) : next == start;
                if (hit)
                {
                    return false;   // the two ends meet: a road exists
                }
                seen.Add((next.Cx, next.Cy));
                queue.Enqueue(next);
            }
        }
        return true;
    }
}
