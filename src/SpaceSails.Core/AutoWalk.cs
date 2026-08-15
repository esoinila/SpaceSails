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

    /// <summary>
    /// #866 · HOW NEAR A POINTED-AT PLACE COUNTS AS THAT PLACE, in deck units.
    ///
    /// <para>Owner, standing on the B1 park gravel: <i>"Now the click to walk does not work here in park?"</i>
    /// He was pointing at what the park's own photograph draws as open gravel, and what the deck has there
    /// is a raised bed — fourteen deck units by seven, fenced on all four sides. The click was honest; the
    /// arrival test was not. A* counted the goal reached only within ONE LATTICE STEP of it (half a deck
    /// unit), so a finger that lands anywhere on a thing wider than half a metre asks for a square that is
    /// solid, or sealed inside the thing's own fence, and the only answer the search can give is <i>no route
    /// at all</i> — after flooding every walkable square on the floor to prove it (456 ms, natively, on a
    /// desk machine, for one refused click).</para>
    ///
    /// <para><b>Two deck units, and the number is argued rather than tuned.</b> Downward it has to clear the
    /// thickest thing this game asks a captain to point AT: the widest gap between a solid square inside the
    /// park and gravel it could stand on is 1.5 du, which is a body's own <c>AvatarRadius</c> clearance
    /// either side of a bed's rail. Upward it has to stay comfortably inside the interact ring (3.0 du),
    /// because the whole promise of pointing at a fixture is that <c>[E]</c> is live when the walk stops —
    /// a reach equal to the ring would put arrival exactly on the boundary where "in reach" is decided by
    /// the last bit of a double.</para>
    ///
    /// <para>It is spent by the CLICK and by nothing else. A guard walking his beat and #488's reachability
    /// audit both keep the old zero, because "he stood on his stop" and "that room can be reached" are
    /// claims about a place and not about a finger.</para>
    /// </summary>
    public const double PointingReachDu = 2.0;

    /// <summary>
    /// #866 · …AND HOW FAR "AS NEAR AS THE FLOOR ALLOWS" GOES, when the first look found nothing.
    ///
    /// <para><see cref="PointingReachDu"/> answers a finger that landed ON the rail of a thing. It does not
    /// answer a finger that landed IN one — and this game has nine of those per park: a raised bed is drawn
    /// as a solid box and simulated as a FENCE, four rails round a 12.6 × 5.6 du pocket that is perfectly
    /// standable and that nobody on this floor can ever get into. Point at the middle of one and the nearest
    /// gravel is 4.2 du away, which is further than any reach that also has to keep arrival inside the [E]
    /// ring.</para>
    ///
    /// <para>So a click that finds nothing gets ONE second look, wider: 3.5 du (half the deepest thing this
    /// building plants on a floor) + 0.7 (the clearance a body needs off its rail) + 0.5 (one lattice step,
    /// because what the walk settles for is a SQUARE of the grid and not a point — leave that out and the
    /// four beds whose middles happen to fall between two rows of it go on refusing), rounded up.
    /// <c>TheParkTakesAClickTests</c> measures that claim against the park's own published beds rather than
    /// trusting the arithmetic in this paragraph. It is spent ONLY after the first search has already come
    /// back empty — the walk that was going to be refused anyway — so no click that works today pays for
    /// it, and the one that does was already the slow path (#858's sliced <see cref="Planner"/> is the
    /// answer if it ever has to be paid over frames rather than in one).</para>
    ///
    /// <para>It is deliberately SHORT. "Get me as close as you can" with no bound at all is a walk order
    /// that can never be refused, and the refusal is load-bearing: it is the reachability audit's own
    /// verdict said out loud to a person, and a captain who clicks a sealed room across the base must be
    /// told there is no way through rather than marched to the nearest wall.</para>
    /// </summary>
    public const double PointingFallbackDu = 5.0;

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
    /// <para>The goal need not be standable. A* counts the goal reached once the walk is within
    /// <paramref name="goalReachDu"/> of it (or, at zero, within one lattice step), so pointing at a
    /// console, a door or the wall behind them walks you up to the thing and stops adjacent — which is
    /// exactly what makes [E] live on arrival. #866 · <b>A finger gets <see cref="PointingReachDu"/>; a body
    /// walking a beat gets the zero.</b> That sentence used to be written as though the one-step rule
    /// covered both, and it does not: half a deck unit is narrower than nearly every fixture this game draws
    /// on a floor, so a click that landed ON a thing could only ever be refused. The caller says which of
    /// the two it is, because only the caller knows.</para>
    /// </summary>
    public static Attempt Plan(
        bool enabled,
        DeckReachability.Point from,
        DeckReachability.Point to,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double radius,
        (double MinX, double MinY, double MaxX, double MaxY) bounds,
        double step = DeckReachability.DefaultStep,
        double goalReachDu = 0)
    {
        if (!enabled)
        {
            return default;
        }

        Attempt pointed = Planner.Begin(from, to, walls, radius, bounds, step, goalReachDu).Finish();
        if (pointed.Route is not null || goalReachDu is <= 0 or >= PointingFallbackDu)
        {
            return pointed;
        }

        // #866 · THE SECOND LOOK. Only a finger gets one (a beat passes zero and stops at the line above),
        // and only after the first came back with nothing — see PointingFallbackDu for what it is for, why
        // it is short, and why it costs no click that already worked.
        return Planner.Begin(from, to, walls, radius, bounds, step, PointingFallbackDu).Finish();
    }

    /// <summary>
    /// #858 · A ROUTE PLANNED WHILE THE MAN IS STANDING THERE ANYWAY.
    ///
    /// <para>Lab 45's one frame-eating number: a single <see cref="Plan"/> over a guard's leg is a median
    /// 1.6–2.2 ms and a worst 6.4 ms — 38.6% of a 60 fps frame — and it is spent whole, in the one frame he
    /// leaves a stop, roughly twice a minute per guard, natively, in a game that ships to WASM.</para>
    ///
    /// <para><b>Why the stand and not a smaller lattice.</b> Of the three fixes the lab put up, coarsening
    /// the lattice CHANGES THE ROUTE (and would put #804's §13.1 reachability audit and the game back on two
    /// different pathfinders), and caching a whole beat's circuit means planning seven legs on the frame a
    /// floor is entered — the same spike, larger, moved somewhere else. This one changes nothing at all: a
    /// guard at a stop stands for <c>PatrolBeat.StandSeconds</c> = 5 s, which is ~300 frames in which he
    /// does not move, cannot move, and already knows exactly where he is going next. The work was always
    /// going to be done from that spot to that stop; this only stops it being done all at once, afterwards.
    /// The state it costs is ONE field on the guard, and it is self-invalidating: the plan carries the two
    /// points it was made for (<see cref="PlannedFor"/>), so a man whose errand changed while he stood
    /// cannot be handed somebody else's route — he plans his real one on the spot, exactly as before.</para>
    ///
    /// <para><b>He can never stand forever.</b> <see cref="Finish"/> completes whatever slicing did not, on
    /// the frame it is asked — at worst the old bill, on the old frame. #843's lesson class is that a body
    /// waiting on a plan is a body that never walks again, so there is no path through this class in which
    /// the answer is "not yet".</para>
    /// </summary>
    public sealed class Planner
    {
        private readonly DeckReachability.Search _search;

        private Planner(DeckReachability.Search search, DeckReachability.Point from, DeckReachability.Point to)
        {
            _search = search;
            From = from;
            To = to;
        }

        /// <summary>Open the plan without walking any of it. <paramref name="goalReachDu"/> is
        /// <see cref="Plan"/>'s own — zero for a body walking to a place, <see cref="PointingReachDu"/> for
        /// a finger pointing at one.</summary>
        public static Planner Begin(
            DeckReachability.Point from,
            DeckReachability.Point to,
            IReadOnlyList<SurfaceCollision.Segment> walls,
            double radius,
            (double MinX, double MinY, double MaxX, double MaxY) bounds,
            double step = DeckReachability.DefaultStep,
            double goalReachDu = 0) =>
            new(DeckReachability.Search.Begin(from, to, walls, radius, bounds, step, goalReachDu), from, to);

        /// <summary>Where the route this is planning starts.</summary>
        public DeckReachability.Point From { get; }

        /// <summary>…and where it ends.</summary>
        public DeckReachability.Point To { get; }

        /// <summary>Is this the plan for THIS walk? The caller's whole invalidation, asked of the plan rather
        /// than remembered beside it — a route half-searched from a spot the body has since left is not a
        /// stale answer to be spotted later, it is simply not this walk's plan.</summary>
        public bool PlannedFor(DeckReachability.Point from, DeckReachability.Point to) =>
            From == from && To == to;

        /// <summary>Walk a slice of the search. True once there is nothing left to do.</summary>
        public bool Advance(int cells) => _search.Advance(cells);

        /// <summary>Is the route already worked out? Nobody has to ask — <see cref="Finish"/> answers
        /// whatever the state — but a test that could not tell a plan finished during the stand from one
        /// finished on the frame it was needed would be measuring the wrong thing entirely.</summary>
        public bool Done => _search.Done;

        /// <summary>The route, finishing whatever is left of the search first. The same
        /// <see cref="Attempt"/> <see cref="Plan"/> hands back, because it is the same search.</summary>
        public Attempt Finish()
        {
            DeckReachability.Walk walk = _search.Finish();
            return walk is { Reached: true, Path.Count: > 0 }
                ? new Attempt(new AutoWalk(walk.Path), null)
                : new Attempt(null, RefusalLine);
        }
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
