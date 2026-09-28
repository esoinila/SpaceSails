namespace SpaceSails.Core;

/// <summary>
/// #251 · THE LATTICE — the nested class every search in this file walks.
///
/// <para>Split out of <c>DeckReachability.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. The nested class travels whole; <c>Neighbours</c>, the class's one
/// initialised static, stays in the opening file (#1163).</para>
/// </summary>
public static partial class DeckReachability
{
    /// <summary>#589 · THE GRID, OWNED IN ONE PLACE. The walk and the flood ask the identical questions —
    /// where does this point land, is that square standable, may I take this diagonal — and the moment they
    /// each answer them for themselves they are two pathfinders that agree only by luck. That is exactly the
    /// drift the Hive keeps paying for (see <c>docs/features/the-landing-site.md</c>), so there is one
    /// lattice and both step over it.</summary>
    private sealed class Lattice
    {
        private readonly IReadOnlyList<SurfaceCollision.Segment> _walls;
        private readonly double _radius;
        private readonly (double MinX, double MinY, double MaxX, double MaxY) _bounds;
        private readonly double _step;
        private readonly int _width;
        private readonly int _height;

        internal Lattice(
            IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
            (double MinX, double MinY, double MaxX, double MaxY) bounds, double step)
        {
            _walls = walls;
            _radius = radius;
            _bounds = bounds;
            _step = step;
            _width = (int)Math.Ceiling((bounds.MaxX - bounds.MinX) / step) + 1;
            _height = (int)Math.Ceiling((bounds.MaxY - bounds.MinY) / step) + 1;
        }

        internal (int Cx, int Cy) Cell(Point p) =>
            ((int)Math.Round((p.X - _bounds.MinX) / _step), (int)Math.Round((p.Y - _bounds.MinY) / _step));

        internal Point World((int Cx, int Cy) c) =>
            new(_bounds.MinX + (c.Cx * _step), _bounds.MinY + (c.Cy * _step));

        internal bool Clear((int Cx, int Cy) c)
        {
            if (c.Cx < 0 || c.Cy < 0 || c.Cx >= _width || c.Cy >= _height)
            {
                return false;
            }
            Point p = World(c);
            return Standable(p.X, p.Y, _radius, _walls);
        }

        /// <summary>
        /// #1290 · <b>THE NODE A BODY STANDING HERE WALKS FROM</b> — and the reason it is not simply
        /// <see cref="Cell"/>.
        ///
        /// <para>Owner, four one-shot runs of the two-leg night: <i>"walk west along the gallery until the
        /// captain fetches up against the rail wall, and click-to-walk stops working entirely… From the same
        /// spot WASD still moves him, and one step off the wall clicks work again."</i></para>
        ///
        /// <para><b>The rounding was the whole bug.</b> <see cref="Cell"/> rounds a body's position to the
        /// nearest node, which is worth up to half a step in each axis — 0.35 du on the diagonal at the
        /// shipped 0.5 lattice. A held key walks the captain up to a wall until the very next sub-step is
        /// refused, which parks him between <c>AvatarRadius</c> and <c>AvatarRadius</c> + one sub-step off
        /// the stone. Those two numbers OVERLAP, so the node his square rounded to could sit inside the
        /// wall's collision skin while his feet were plainly on clear floor. The search then reported the
        /// caller error below and handed back no route at all — for a captain who could still walk out of
        /// that square with the arrow keys, because the keys are a collision-resolved slide and never touch
        /// this lattice. #875's ruling is that the two grips are ONE walk; a rounding is not allowed to be
        /// the difference between them.</para>
        ///
        /// <para><b>It nudges the NODE, never the body, and only for a body that is standing up.</b> The
        /// first question asked is whether the point itself is standable — if it is not, this is still the
        /// worse bug the constructor's note names (a spawn inside the stone) and it still fails loudly. What
        /// is repaired is only the discretisation: the nearest node this body could have been rounded to,
        /// out to two cells, preferring one the body could walk to in a straight line (the midpoint probe),
        /// because the route's first leg is spent from where the body actually IS.</para>
        /// </summary>
        internal bool TryStandOn(Point p, out (int Cx, int Cy) start)
        {
            start = Cell(p);
            if (Clear(start))
            {
                return true;
            }

            // Not a rounding artefact: the body is genuinely inside the stone. Loud, exactly as before.
            if (!Standable(p.X, p.Y, _radius, _walls))
            {
                return false;
            }

            bool found = false;
            double best = double.MaxValue;
            bool bestWalkable = false;
            for (int ring = 1; ring <= StartNudgeRings; ring++)
            {
                for (int dy = -ring; dy <= ring; dy++)
                {
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring)
                        {
                            continue;   // the rings are walked outward, so a nearer node always wins first
                        }
                        (int Cx, int Cy) candidate = (start.Cx + dx, start.Cy + dy);
                        if (!Clear(candidate))
                        {
                            continue;
                        }

                        Point at = World(candidate);
                        double away = ((at.X - p.X) * (at.X - p.X)) + ((at.Y - p.Y) * (at.Y - p.Y));
                        // Can the body plainly get there? A node past the corner of two walls is clear and
                        // still not somewhere this body can step, and the route's first leg is walked by the
                        // ordinary stepper rather than proved by the search.
                        bool walkable = Standable(
                            (at.X + p.X) / 2.0, (at.Y + p.Y) / 2.0, _radius, _walls);

                        // A node the body can step to beats a nearer one it cannot; otherwise, nearest wins.
                        bool better = !found
                            || (walkable && !bestWalkable)
                            || (walkable == bestWalkable && away < best);
                        if (!better)
                        {
                            continue;
                        }

                        found = true;
                        best = away;
                        bestWalkable = walkable;
                        start = candidate;
                    }
                }
                if (found && bestWalkable)
                {
                    return true;   // the nearest ring that offers a node the body can step to is the answer
                }
            }
            return found;
        }

        /// <summary>#1290 · How far the start node may be nudged, in lattice cells. TWO, which at the shipped
        /// 0.5 du step is one deck unit — enough to clear the half-step rounding that causes this and the
        /// corner case where the nearer node is itself in a jamb, and far too short to walk a body out of a
        /// sealed nook it is genuinely stuck in. It buys a NODE, never a place: the captain's own square is
        /// where the walk still starts from.</summary>
        private const int StartNudgeRings = 2;

        /// <summary>May the captain move off <paramref name="from"/> by this delta? No cutting corners: a
        /// diagonal needs both its orthogonal neighbours clear, or the walk could slip between two walls
        /// that touch — a route the real collision would never allow.</summary>
        internal bool CanStep((int Cx, int Cy) from, int dx, int dy)
        {
            if (!Clear((from.Cx + dx, from.Cy + dy)))
            {
                return false;
            }
            return dx == 0 || dy == 0
                || (Clear((from.Cx + dx, from.Cy)) && Clear((from.Cx, from.Cy + dy)));
        }
    }
}
