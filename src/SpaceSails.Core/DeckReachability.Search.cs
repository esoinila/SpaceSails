namespace SpaceSails.Core;

/// <summary>
/// #251 · THE INCREMENTAL SEARCH — the nested <c>Search</c> class, a path planned a slice at a time.
///
/// <para>Split out of <c>DeckReachability.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. The nested class travels whole.</para>
/// </summary>
public static partial class DeckReachability
{
    /// <summary>
    /// #858 · THE SAME WALK, PAUSED HALFWAY DOWN IT.
    ///
    /// <para>Lab 45 timed one <see cref="Path"/> over a guard's own leg at a median 1.6–2.2 ms and a worst
    /// case of <b>6.4 ms — 38.6% of a 60 fps frame, spent inside the single frame he arrives at a stop</b>,
    /// about twice a minute per guard, and that is NATIVE on a desk machine while the game ships to WASM. It
    /// is the only measurement in the whole lab that can miss a frame. Nothing about the search was wrong;
    /// what was wrong is that it is asked for all at once, on the one frame the player is most likely to be
    /// looking straight at the man.</para>
    ///
    /// <para>So the loop is the same loop, with a handle on it: <see cref="Advance"/> expands a bounded
    /// number of lattice cells and comes back, and the caller may spend as many frames on it as it has.
    /// <b><see cref="Path"/> IS this class run to the end</b> — same lattice, same neighbours, same queue,
    /// the identical sequence of enqueues and dequeues — so a sliced search and a whole one cannot return
    /// different routes. There is no second pathfinder here, which is the same rule
    /// <see cref="Reachable"/> and <see cref="Path"/> already share a <c>Lattice</c> for.</para>
    ///
    /// <para><b>It never traps the caller.</b> <see cref="Finish"/> completes whatever is left, however
    /// little time it was given — so a body waiting on a route gets one on the frame it asks, at worst
    /// paying exactly the bill it would have paid before this class existed.</para>
    /// </summary>
    public sealed class Search
    {
        private readonly Lattice _grid;
        private readonly (int Cx, int Cy) _goal;
        private readonly Point _at;
        private readonly double _reachDu;
        private readonly PriorityQueue<(int Cx, int Cy), double> _open = new();
        private readonly Dictionary<(int, int), (int, int)> _cameFrom = [];
        private readonly Dictionary<(int, int), double> _best = [];

        private Search(
            Point from, Point to, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
            (double MinX, double MinY, double MaxX, double MaxY) bounds, double step, double reachDu)
        {
            _grid = new Lattice(walls, radius, bounds, step);
            _goal = _grid.Cell(to);
            _at = to;
            _reachDu = reachDu;

            // A start the captain could not stand on is a caller error worth surfacing loudly rather than
            // reporting as "unreachable" — an unwalkable SPAWN is its own, worse bug.
            //
            // #1290 · …but the question is asked of the BODY and not of the rounding. A captain pressed flat
            // against a wall by a held key stands on clear floor whose nearest lattice node is inside the
            // stone, and until this seam moved he was a captain the arrow keys would walk and the mouse
            // would not — see Lattice.TryStandOn for the measurement and the bound.
            if (!_grid.TryStandOn(from, out (int Cx, int Cy) start))
            {
                Done = true;
                return;
            }
            _best[start] = 0;
            _open.Enqueue(start, H(start));
        }

        /// <summary>
        /// Open a search without walking any of it. Validates exactly what <see cref="Path"/>
        /// validated, in the same place, so a bad call fails the same way whichever door it came in.
        ///
        /// <para>#866 · <paramref name="goalReachDu"/> is HOW NEAR COUNTS AS THERE, and it defaults to
        /// zero — which is to say, to the one-lattice-step arrival every audit in this repo has always used,
        /// byte for byte. It exists because a POINTED-AT place is not the same question as a walked-to one:
        /// a finger lands on a thing, and the thing may be a raised bed fourteen deck units across whose
        /// every square is either solid or sealed inside its own fence. Asked to reach the middle of that,
        /// the one-step rule can only answer "no route" — and it spends the whole floor's lattice finding
        /// that out. Given a reach, the same search stops at the first square it can stand on within that
        /// far of where the finger went, which is the answer the finger meant.</para>
        ///
        /// <para>It is a parameter and not a new default because the audits must not move. A guard walking
        /// a beat stands ON his stop; #488's reachability audit asks whether a console can be REACHED, not
        /// whether the captain can get within a couple of paces of it. Both keep the zero.</para>
        /// </summary>
        public static Search Begin(
            Point from,
            Point to,
            IReadOnlyList<SurfaceCollision.Segment> walls,
            double radius,
            (double MinX, double MinY, double MaxX, double MaxY) bounds,
            double step = DefaultStep,
            double goalReachDu = 0)
        {
            ArgumentNullException.ThrowIfNull(walls);
            if (step <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }
            return new Search(from, to, walls, radius, bounds, step, goalReachDu);
        }

        /// <summary>Whether the walk has settled — a route found, or the whole reachable lattice exhausted
        /// without one. Until it is true, <see cref="Result"/> is the honest "not yet".</summary>
        public bool Done { get; private set; }

        /// <summary>What the walk found. Unreached until <see cref="Done"/>, so a caller that reads it early
        /// is told nothing rather than told a half-answer.</summary>
        public Walk Result { get; private set; } = new(false, 0, []);

        /// <summary>Expand at most <paramref name="cells"/> more lattice cells. Returns
        /// <see cref="Done"/> — true when there is nothing left to do.</summary>
        public bool Advance(int cells)
        {
            while (!Done && cells-- > 0 && _open.TryDequeue(out (int Cx, int Cy) current, out _))
            {
                Expand(current);
            }
            if (!Done && _open.Count == 0)
            {
                Done = true;   // the reachable lattice ran out and the goal was not in it
            }
            return Done;
        }

        /// <summary>Walk the rest of it, whatever is left, and hand back the route. This is what
        /// <see cref="Path"/> is.</summary>
        public Walk Finish()
        {
            while (!Advance(int.MaxValue))
            {
                // Advance only returns early on a budget, and this one cannot be spent.
            }
            return Result;
        }

        /// <summary>Is standing here standing THERE? Within one lattice step of the goal always is — see the
        /// note above about consoles — and, when the caller asked for a reach, so is any square it can stand
        /// on within that far of the point itself. Measured off the POINT rather than off the goal's cell,
        /// because the reach is a statement about where a finger landed and the cell is a rounding of it.</summary>
        private bool Arrived((int Cx, int Cy) c)
        {
            if (Math.Abs(c.Cx - _goal.Cx) <= 1 && Math.Abs(c.Cy - _goal.Cy) <= 1)
            {
                return true;
            }
            if (_reachDu <= 0)
            {
                return false;
            }
            Point p = _grid.World(c);
            double dx = p.X - _at.X, dy = p.Y - _at.Y;
            return (dx * dx) + (dy * dy) <= _reachDu * _reachDu;
        }

        private void Expand((int Cx, int Cy) current)
        {
            if (Arrived(current))
            {
                var path = new List<Point>();
                (int, int) walk = current;
                path.Add(_grid.World(current));
                while (_cameFrom.TryGetValue(walk, out (int, int) prev))
                {
                    walk = prev;
                    path.Add(_grid.World(walk));
                }
                path.Reverse();
                Result = new Walk(true, path.Count, path);
                Done = true;
                return;
            }

            double soFar = _best[current];
            foreach ((int dx, int dy) in Neighbours)
            {
                (int Cx, int Cy) next = (current.Cx + dx, current.Cy + dy);
                if (!_grid.CanStep(current, dx, dy))
                {
                    continue;
                }

                double cost = soFar + (dx != 0 && dy != 0 ? 1.41421356 : 1.0);
                if (_best.TryGetValue(next, out double had) && had <= cost)
                {
                    continue;
                }

                _best[next] = cost;
                _cameFrom[next] = current;
                _open.Enqueue(next, cost + H(next));
            }
        }

        private double H((int Cx, int Cy) c) =>
            Math.Sqrt(((c.Cx - _goal.Cx) * (double)(c.Cx - _goal.Cx))
                + ((c.Cy - _goal.Cy) * (double)(c.Cy - _goal.Cy)));
    }
}
