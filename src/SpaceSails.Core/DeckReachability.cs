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
/// <para>#589 · <b>And the flood as well, in the end.</b> <see cref="Reachable"/> answers the question a
/// PICTURE wants — everywhere you could get to, as a wash over the floor, so a sealed room is an island
/// nobody coloured in. #587 was found by the A* audit and could not be EXPLAINED by it: the audit named
/// three coordinates and an evening went into guessing which wall put them there. Both now step over one
/// <c>Lattice</c>, so a point the flood washes is a point the walk can reach, by construction.</para>
///
/// <para>Pure and deterministic: same walls + same points + same step → same verdict, always.</para>
/// </summary>
public static partial class DeckReachability
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
    /// #589 · EVERYWHERE THE CAPTAIN COULD GET TO, as a set of grid points — the shape of what is reachable
    /// rather than a yes/no about one place.
    ///
    /// <para><see cref="Path"/> answers "can I get there" and hands back a route, which is what a red test
    /// wants. This answers the question a PICTURE wants: wash the floor with everywhere you can stand, and a
    /// sealed room shows up as an island nobody coloured in. That is the whole of Lab 48, and it is the
    /// difference between knowing a room is unreachable and seeing which wall did it.</para>
    ///
    /// <para>Same lattice, same standability predicate and the same no-corner-cutting rule as the walk, so a
    /// point in this set is reachable by <see cref="CanReach"/> and a point outside it is not.</para>
    /// </summary>
    public static IReadOnlyCollection<Point> Reachable(
        Point from,
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

        var grid = new Lattice(walls, radius, bounds, step);

        // #1290 · The same start the WALK takes, for the reason this class already keeps one Lattice: the
        // flood washes everywhere the walk can reach, and a flood that began at a different node than the
        // walk would be a picture of a different floor.
        var found = new List<Point>();
        if (!grid.TryStandOn(from, out (int Cx, int Cy) start))
        {
            return found;
        }

        var seen = new HashSet<(int, int)> { start };
        var queue = new Queue<(int Cx, int Cy)>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            (int Cx, int Cy) current = queue.Dequeue();
            found.Add(grid.World(current));

            foreach ((int dx, int dy) in Neighbours)
            {
                (int Cx, int Cy) next = (current.Cx + dx, current.Cy + dy);
                if (seen.Contains(next) || !grid.CanStep(current, dx, dy))
                {
                    continue;
                }
                seen.Add(next);
                queue.Enqueue(next);
            }
        }
        return found;
    }

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
        double step = DefaultStep) =>
        Search.Begin(from, to, walls, radius, bounds, step).Finish();

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
