namespace SpaceSails.Core;

/// <summary>
/// #729 · POINT AT THE FLOOR AND WALK THERE. Owner, mid-playtest: <i>"Maybe for testing purposes an
/// automatic walk feature with point-at to walk-to using say A* might be useful. So our testing does not
/// hang on slow MCP speed to browser."</i> Minutes later, the framing changed: <i>"we could disable that
/// later in game or consider it as a feature also… like an alternative way to move to a spot even behind
/// automatic doors."</i>
///
/// <para><b>What this is NOT.</b> It is not a teleport, and it is not a faster walk. A cheat that skipped
/// the walk would un-test exactly the thing walking exists to test — #600's lift shipped broken for months
/// because the audit proved you can REACH the lift and never once that the lift is a way HOME, and a
/// movement shortcut that does not pay the walk breeds that same class by the dozen. So this hands out
/// nothing but a DIRECTION and a distance budget: the caller spends them through its own ordinary
/// per-frame movement, against its own collision, while its own air, nerve, tracker, doors and Old Ones
/// keep running. The world is never told it is being driven.</para>
///
/// <para><b>One source of truth for the walls.</b> The route comes from <see cref="DeckReachability.Path"/>
/// — the A* the reachability audits already walk every floor of every site with — whose lattice asks
/// <see cref="SurfaceCollision.Blocked"/>, the exact predicate the avatar's own stepper asks. There is no
/// second wall list here and no copied constant: a step this class offers is a step the collision has
/// already agreed to, by construction rather than by comment.</para>
///
/// <para><b>Keys always win.</b> <see cref="Cancel"/> kills the route outright, and the caller calls it on
/// any movement key before it spends a single sub-step, so a hand on WASD takes the captain back within
/// one step of the press.</para>
///
/// <para>Pure and deterministic: same walls + same click → same route, always. Nothing here allocates once
/// the route is planned, so a frame that is walking one costs no more garbage than a frame that is not.</para>
/// </summary>
public sealed class AutoWalk
{
    /// <summary>The dev-cheat flag that turns clicking the floor into a walk order, until the owner rules
    /// on always-on. Named once here so the URL parse, the docs table and the tests cannot drift.</summary>
    public const string QueryFlag = "autowalk";

    /// <summary>What the captain is told when the click lands somewhere no corridor connects to. It is the
    /// refusal the reachability audits make in prose — the honest answer, and never a silent no-op, because
    /// a button that does nothing reads as a broken button.</summary>
    public const string RefusalLine =
        "🦶 No way through from here — nothing you can walk connects to that spot.";

    /// <summary>What the captain is told when the route was good and the ground disagreed anyway: the
    /// stepper refused a move the plan expected to make. It stops rather than grinding against the wall,
    /// because a walk that jitters in place is a bug wearing a feature's clothes.</summary>
    public const string SnagLine =
        "🦶 The route snagged on something — the rest of that walk is yours.";

    /// <summary>Said when a walk is cut short by the captain's own hand. Not an error; a receipt.</summary>
    public const string CancelledLine = "🦶 Walk cancelled — you have the helm.";

    /// <summary>The largest single move this hands out, in deck units. It exists because the avatar's
    /// stepper resolves a diagonal axis-separately (X first, then Y from the X-resolved spot), so a long
    /// diagonal probes a CORNER that is not on the route at all. Kept well under the lattice spacing, that
    /// corner stays a hair off the line the A* proved clear. A frame with more budget than this simply
    /// takes several of them — the distance walked per second is untouched, which is what keeps the air
    /// bill identical to a hand-walked route of the same length.</summary>
    public const double MaxSubStepDu = 0.2;

    // Close enough to a waypoint to call it visited. A hair, not a tolerance: the sub-step below lands
    // exactly on the waypoint when the budget allows, so this only has to survive double rounding.
    private const double OnWaypointDu = 1e-6;

    /// <summary>The result of pointing at the floor: a live route, or a line explaining why not. Both null
    /// means the click was INERT — the cheat is off, and nothing happened at all (not even a message).</summary>
    public readonly record struct Attempt(AutoWalk? Route, string? Refusal);

    private readonly IReadOnlyList<DeckReachability.Point> _route;
    private int _cursor;
    private bool _cancelled;
    private bool _snagged;

    private AutoWalk(IReadOnlyList<DeckReachability.Point> route) => _route = route;

    /// <summary>
    /// Point at (<paramref name="to"/>) from (<paramref name="from"/>) and see whether the floor connects.
    ///
    /// <para><paramref name="enabled"/> is the whole of the gate. When it is false this returns a wholly
    /// empty <see cref="Attempt"/> — no route, no line, no sound — so a build without the cheat behaves
    /// exactly as it did before the feature existed. It is a parameter rather than a check inside the
    /// caller because that is the seam a touch UI adopts unmodified: the day tap-to-move ships, the only
    /// thing that changes is what is passed here.</para>
    ///
    /// <para>The goal need not be standable. A* counts the goal reached once the walk is within one
    /// lattice step of it, so pointing at a console, a door or the wall behind them walks you up to the
    /// thing and stops adjacent — which is exactly what makes [E] live on arrival.</para>
    /// </summary>
    public static Attempt Plan(
        bool enabled,
        DeckReachability.Point from,
        DeckReachability.Point to,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double radius,
        (double MinX, double MinY, double MaxX, double MaxY) bounds,
        double step = DeckReachability.DefaultStep)
    {
        if (!enabled)
        {
            return default;
        }

        DeckReachability.Walk walk = DeckReachability.Path(from, to, walls, radius, bounds, step);
        return walk is { Reached: true, Path.Count: > 0 }
            ? new Attempt(new AutoWalk(walk.Path), null)
            : new Attempt(null, RefusalLine);
    }

    /// <summary>A lattice box big enough to hold the deck's stone and both ends of the walk, with a margin
    /// so a route may bulge around the outside of an obstacle that sits on the rim. Derived from the wall
    /// list the caller is about to path over rather than from any scene's own numbers, so it is right on a
    /// hive floor, a surface field and anything welded on later without knowing which it is looking at.</summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) BoundsFor(
        IReadOnlyList<SurfaceCollision.Segment> walls,
        DeckReachability.Point from,
        DeckReachability.Point to,
        double margin = 4.0)
    {
        ArgumentNullException.ThrowIfNull(walls);

        double minX = Math.Min(from.X, to.X), maxX = Math.Max(from.X, to.X);
        double minY = Math.Min(from.Y, to.Y), maxY = Math.Max(from.Y, to.Y);
        foreach (SurfaceCollision.Segment w in walls)
        {
            minX = Math.Min(minX, Math.Min(w.X1, w.X2));
            maxX = Math.Max(maxX, Math.Max(w.X1, w.X2));
            minY = Math.Min(minY, Math.Min(w.Y1, w.Y2));
            maxY = Math.Max(maxY, Math.Max(w.Y1, w.Y2));
        }
        return (minX - margin, minY - margin, maxX + margin, maxY + margin);
    }

    /// <summary>The route as planned, in deck units — what a red test prints, and what a debug overlay
    /// would draw. Never mutated; the walk's progress is the cursor, not the list.</summary>
    public IReadOnlyList<DeckReachability.Point> Route => _route;

    /// <summary>Is there still walking to do? False once the last waypoint is reached, once a key cancels
    /// it, or once the ground refused a step.</summary>
    public bool Active => !_cancelled && !_snagged && _cursor < _route.Count;

    /// <summary>True when the walk ran all the way to its last waypoint.</summary>
    public bool Arrived => !_cancelled && !_snagged && _cursor >= _route.Count;

    /// <summary>True when a key took the helm back.</summary>
    public bool Cancelled => _cancelled;

    /// <summary>THE KEYS WIN. Called before the caller spends any of this frame's budget, so a WASD press
    /// ends the route within one step of the press — no coasting, no "finishing the current leg".</summary>
    public void Cancel() => _cancelled = true;

    /// <summary>The ground refused a move the plan expected to make. Ends the walk honestly instead of
    /// letting it grind against whatever is in the way.</summary>
    public void Snag() => _snagged = true;

    /// <summary>
    /// The next move to hand the caller's ordinary stepper: a delta in deck units toward the current
    /// waypoint, no longer than what is left of <paramref name="budgetDu"/> and never longer than
    /// <see cref="MaxSubStepDu"/>. False when there is nothing left to walk.
    ///
    /// <para>It never overshoots a waypoint. That is the load-bearing rule: the A* proved the LINE between
    /// consecutive waypoints is clear, and a step that sails past one is a step across ground nobody
    /// checked — the corner-cutting that would let a fast frame walk through the corner of two walls.</para>
    ///
    /// <para>Allocation-free by design (out params on a live object, no tuple, no list), because this is
    /// called inside the frame loop and this repo pays for garbage in WASM.</para>
    /// </summary>
    public bool TryStep(double x, double y, double budgetDu, out double dx, out double dy)
    {
        dx = 0;
        dy = 0;
        if (!Active || budgetDu <= 0)
        {
            return false;
        }

        while (_cursor < _route.Count)
        {
            DeckReachability.Point target = _route[_cursor];
            double ax = target.X - x, ay = target.Y - y;
            double remaining = Math.Sqrt((ax * ax) + (ay * ay));
            if (remaining <= OnWaypointDu)
            {
                _cursor++;
                continue;
            }

            double take = Math.Min(Math.Min(budgetDu, remaining), MaxSubStepDu);
            dx = ax / remaining * take;
            dy = ay / remaining * take;
            return true;
        }
        return false;
    }

    /// <summary>#729 · Does this query string carry the cheat? Parsed here, once, in the same shape the
    /// client's other dev flags are parsed, so the URL the testing guide documents and the flag the gate
    /// reads are the same fact rather than two spellings of it.</summary>
    public static bool EnabledIn(string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return false;
        }
        foreach (string pair in query.TrimStart('?').Split('&'))
        {
            if (pair.Equals(QueryFlag + "=1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
