namespace SpaceSails.Core;

/// <summary>
/// #488 · CAN THE CAPTAIN ACTUALLY GET THERE? An A* walk over a deck's own collision geometry, so a room
/// that is sealed by accident fails a test instead of failing a player.
///
/// <para>Owner, aboard the LONG SHRIFT: <i>"THERE IS NO WAY TO GO TO THE BACK OF THE SHIP … we need some
/// kind of CI test to spot similar problems. Maybe do a lab with A-star algorithm and rig it up to our CI
/// tests."</i> He was right on both counts. Two mutiny barricades spanned the full width of the spine and
/// cut the wreck in half — the cargo manifest, half the compartments and the whole aft end were
/// unreachable — and every build was green the entire time, because <b>a wall you cannot pass has no
/// test that fails.</b> Geometry bugs are invisible to type checks and unit tests of the pieces; the only
/// thing that catches them is walking the room.</para>
///
/// <para>So: walk it. This is the same question the renderer answers implicitly every frame — is this
/// point clear of the walls, given how wide I am — asked deliberately, over a grid, from the spawn to
/// every place the player is expected to reach.</para>
///
/// <para><b>Why A* and not a plain flood fill.</b> A flood fill answers "is it connected"; A* answers that
/// AND hands back the route, which is what makes a failure diagnosable — the lab prints the path it found
/// to the places that ARE reachable, so when a console is not, the shape of what the captain can reach
/// tells you which wall did it. Same reason the labs exist at all: the number has to explain itself.</para>
///
/// <para>Pure and deterministic: same walls + same points + same step → same verdict, always.</para>
/// </summary>
public static class DeckReachability
{
    /// <summary>How finely the walk samples the deck, in deck units. Small enough to find a doorway a
    /// captain could squeeze through, coarse enough that auditing a whole ship is instant. Doorways in this
    /// game are metres wide; anything this misses was never a passage.</summary>
    public const double DefaultStep = 0.5;

    /// <summary>A point on the deck, in deck units.</summary>
    public readonly record struct Point(double X, double Y);

    /// <summary>What a walk found: whether it got there, how many steps the route took, and the route
    /// itself (empty when unreachable). The path is what makes a red test readable.</summary>
    public readonly record struct Walk(bool Reached, int Steps, IReadOnlyList<Point> Path);

    // The eight ways off a grid square. Diagonals are allowed because the captain moves freely — but a
    // diagonal is only taken when BOTH its orthogonal neighbours are clear, so the walk can never squeeze
    // through the corner of two walls that the real collision would stop.
    private static readonly (int Dx, int Dy)[] Neighbours =
        [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    /// <summary>Is this point somewhere the captain could stand — clear of every wall by their own
    /// radius? The SAME predicate the live movement uses, so the audit and the game agree by construction
    /// rather than by comment.</summary>
    public static bool Standable(
        double x, double y, double radius, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        !SurfaceCollision.Blocked(x, y, radius, walls);

    /// <summary>
    /// Walk from <paramref name="from"/> to <paramref name="to"/> with A*, over a grid of
    /// <paramref name="step"/> deck units, treating any point within <paramref name="radius"/> of a wall as
    /// solid. Returns the route if one exists.
    ///
    /// <para>The goal counts as reached once the walk is within one step of it, so a console standing a
    /// hair inside a wall's clearance (they are interaction points, not standing room) does not read as
    /// unreachable when the captain can plainly walk up and press E.</para>
    /// </summary>
    public static Walk Path(
        Point from,
        Point to,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double radius,
        (double MinX, double MinY, double MaxX, double MaxY) bounds,
        double step = DefaultStep)
    {
        ArgumentNullException.ThrowIfNull(walls);
        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        (int Cx, int Cy) Cell(Point p) =>
            ((int)Math.Round((p.X - bounds.MinX) / step), (int)Math.Round((p.Y - bounds.MinY) / step));
        Point World((int Cx, int Cy) c) =>
            new(bounds.MinX + (c.Cx * step), bounds.MinY + (c.Cy * step));

        int width = (int)Math.Ceiling((bounds.MaxX - bounds.MinX) / step) + 1;
        int height = (int)Math.Ceiling((bounds.MaxY - bounds.MinY) / step) + 1;

        bool InBounds((int Cx, int Cy) c) => c.Cx >= 0 && c.Cy >= 0 && c.Cx < width && c.Cy < height;
        bool Clear((int Cx, int Cy) c)
        {
            Point p = World(c);
            return InBounds(c) && Standable(p.X, p.Y, radius, walls);
        }

        (int Cx, int Cy) start = Cell(from);
        (int Cx, int Cy) goal = Cell(to);

        // A start the captain could not stand on is a caller error worth surfacing loudly rather than
        // reporting as "unreachable" — an unwalkable SPAWN is its own, worse bug.
        if (!Clear(start))
        {
            return new Walk(false, 0, []);
        }

        double H((int Cx, int Cy) c) =>
            Math.Sqrt(((c.Cx - goal.Cx) * (double)(c.Cx - goal.Cx)) + ((c.Cy - goal.Cy) * (double)(c.Cy - goal.Cy)));

        var open = new PriorityQueue<(int Cx, int Cy), double>();
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var best = new Dictionary<(int, int), double> { [start] = 0 };
        open.Enqueue(start, H(start));

        while (open.TryDequeue(out (int Cx, int Cy) current, out _))
        {
            // Within one step of the goal is arrived — see the note above about consoles.
            if (Math.Abs(current.Cx - goal.Cx) <= 1 && Math.Abs(current.Cy - goal.Cy) <= 1)
            {
                var path = new List<Point>();
                (int, int) walk = current;
                path.Add(World(current));
                while (cameFrom.TryGetValue(walk, out (int, int) prev))
                {
                    walk = prev;
                    path.Add(World(walk));
                }
                path.Reverse();
                return new Walk(true, path.Count, path);
            }

            double soFar = best[current];
            foreach ((int dx, int dy) in Neighbours)
            {
                (int Cx, int Cy) next = (current.Cx + dx, current.Cy + dy);
                if (!Clear(next))
                {
                    continue;
                }

                // No cutting corners: a diagonal needs both its orthogonal neighbours clear, or the walk
                // could slip between two walls that touch — a route the real collision would never allow.
                if (dx != 0 && dy != 0
                    && (!Clear((current.Cx + dx, current.Cy)) || !Clear((current.Cx, current.Cy + dy))))
                {
                    continue;
                }

                double cost = soFar + (dx != 0 && dy != 0 ? 1.41421356 : 1.0);
                if (best.TryGetValue(next, out double had) && had <= cost)
                {
                    continue;
                }

                best[next] = cost;
                cameFrom[next] = current;
                open.Enqueue(next, cost + H(next));
            }
        }

        return new Walk(false, 0, []);
    }

    /// <summary>Can the captain get from here to there at all? The one-line form for a test.</summary>
    public static bool CanReach(
        Point from, Point to, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
        (double MinX, double MinY, double MaxX, double MaxY) bounds, double step = DefaultStep) =>
        Path(from, to, walls, radius, bounds, step).Reached;

    /// <summary>
    /// THE AUDIT: which of <paramref name="targets"/> the captain CANNOT reach from <paramref name="spawn"/>.
    /// An empty result is a deck that hangs together; anything in it is a room, a console or a way home
    /// that exists on screen and cannot be walked to.
    ///
    /// <para>This is the shape a CI test wants — it names every unreachable thing at once instead of
    /// failing on the first, so one red run tells you the whole story of what the geometry sealed off.</para>
    /// </summary>
    public static IReadOnlyList<string> Unreachable(
        Point spawn,
        IReadOnlyList<(string Name, Point At)> targets,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double radius,
        (double MinX, double MinY, double MaxX, double MaxY) bounds,
        double step = DefaultStep)
    {
        ArgumentNullException.ThrowIfNull(targets);

        var missed = new List<string>();
        foreach ((string name, Point at) in targets)
        {
            if (!CanReach(spawn, at, walls, radius, bounds, step))
            {
                missed.Add(name);
            }
        }
        return missed;
    }
}
