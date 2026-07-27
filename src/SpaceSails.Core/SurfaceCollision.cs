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
        // #448: when the caller hands us an INDEXED wall list, only the stone plausibly near the body is
        // ever measured — same answer, bounded work. Any other list is the honest full sweep below.
        if (walls is WallIndex index)
        {
            return index.AnyBlocking(x, y, radius);
        }
        foreach (Segment w in walls)
        {
            if (NearSegment(x, y, radius, w))
            {
                return true;
            }
        }
        return false;
    }

    // #448 · the exact test, fronted by a free bounding-box reject. A body can only touch a segment when
    // it stands inside that segment's box grown by its own radius, and that is four comparisons instead of
    // a projection and a square root. The DECISION is untouched — the same "distance &lt; radius" law on the
    // same numbers, only reached sooner for the stone that was never a candidate. The box is grown by a
    // further hair (BoxSlack) so floating-point rounding in the box arithmetic can never reject a body the
    // exact test would have stopped: the reject is strictly more permissive than the law it fronts.
    private static bool NearSegment(double px, double py, double radius, in Segment w)
    {
        double reach = radius + BoxSlack;
        double minX = w.X1 < w.X2 ? w.X1 : w.X2, maxX = w.X1 < w.X2 ? w.X2 : w.X1;
        double minY = w.Y1 < w.Y2 ? w.Y1 : w.Y2, maxY = w.Y1 < w.Y2 ? w.Y2 : w.Y1;
        if (px < minX - reach || px > maxX + reach || py < minY - reach || py > maxY + reach)
        {
            return false;
        }
        return DistanceToSegment(px, py, w.X1, w.Y1, w.X2, w.Y2) < radius;
    }

    /// <summary>The hair every cheap box test is grown by, so rounding in the box arithmetic can only ever
    /// let a candidate THROUGH to the exact test, never drop one. A nanometre on a field measured in tens
    /// of metres — invisible to play, enormous next to double's error at these coordinates.</summary>
    private const double BoxSlack = 1e-9;

    // True when two segments' bounding boxes overlap (grown by BoxSlack). Segments that cross must have
    // overlapping boxes, so a miss here is proof they never meet — the free reject in front of the exact
    // orientation test, and never able to hide a crossing.
    private static bool BoxesOverlap(
        double p1x, double p1y, double p2x, double p2y, in Segment w)
    {
        double aMinX = p1x < p2x ? p1x : p2x, aMaxX = p1x < p2x ? p2x : p1x;
        double aMinY = p1y < p2y ? p1y : p2y, aMaxY = p1y < p2y ? p2y : p1y;
        double bMinX = w.X1 < w.X2 ? w.X1 : w.X2, bMaxX = w.X1 < w.X2 ? w.X2 : w.X1;
        double bMinY = w.Y1 < w.Y2 ? w.Y1 : w.Y2, bMaxY = w.Y1 < w.Y2 ? w.Y2 : w.Y1;
        return aMaxX >= bMinX - BoxSlack && bMaxX >= aMinX - BoxSlack
            && aMaxY >= bMinY - BoxSlack && bMaxY >= aMinY - BoxSlack;
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
        // #448: an indexed list only walks the stone the sightline actually passes among.
        if (walls is WallIndex index)
        {
            return !index.AnyCrossing(ax, ay, bx, by);
        }
        foreach (Segment w in walls)
        {
            if (BoxesOverlap(ax, ay, bx, by, w)
                && SegmentsIntersect(ax, ay, bx, by, w.X1, w.Y1, w.X2, w.Y2))
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

    // =====================================================================================================
    //  #448 · A CEILING ON THE STONE. Owner, live 2026-07-26: "Now the shuttle ride is timing out twice."
    //
    //  Every wall query above sweeps EVERY wall. That was honest while a ground carried thirty walls and a
    //  pack was three — but the surface step's cost is walls × movers, and both ends have grown: #435 gave a
    //  blocked Old One up to three Slide calls a frame (each two Blocked sweeps), #438 gave every sentry a
    //  line-of-sight test per candidate, #441 added a pairwise shove, and #320 lets a landing site seed
    //  whatever geometry it likes. Shaving the constant only postpones the next round of this fight; the
    //  budget has to stop depending on how much stone a site happens to lay down.
    //
    //  So: a coarse uniform GRID over the walls — the deck-plan's own crude idiom, no BSP, no physics
    //  engine. Built once when the plan is welded, it answers "which walls are plausibly near here" in
    //  constant time, and the exact segment math above then runs on a handful of candidates instead of the
    //  whole field. It is a WallIndex, an IReadOnlyList<Segment> like any other wall list, so every caller
    //  and every test that hands the primitives a plain list keeps its exact behaviour — the index is only
    //  a faster road to the same answer, never a different one.
    //
    //  The law it must never break: the grid may hand back EXTRA candidates (harmless — the exact test
    //  rejects them), but never miss one. That holds because a wall is filed in every cell its bounding box
    //  touches, and a query scans every cell its own box touches: if the two boxes meet at all, they meet
    //  inside a cell that is both filed and scanned. Pure, allocation-free to query, deterministic.
    // =====================================================================================================

    /// <summary>A wall list that also knows WHERE its walls are — a coarse uniform grid over the segments,
    /// so <see cref="Blocked"/> and <see cref="HasLineOfSight"/> only measure the stone near the query
    /// instead of the whole ground. Reads exactly like the plain segment list it wraps (it IS one), and
    /// answers exactly what the full sweep answers. Build it once per welded deck; querying allocates
    /// nothing.</summary>
    public sealed class WallIndex : IReadOnlyList<Segment>
    {
        // ~4 deck units: a couple of body-widths, so a collision query touches one or two cells while a
        // wall of any ordinary length files into a handful. Grown (never shrunk) if a ground is so large
        // that the grid would exceed MaxCells, so the index can never itself become the expensive thing.
        private const double BaseCell = 4.0;
        private const int MaxCells = 4096;

        private readonly Segment[] _walls;
        private readonly double _originX, _originY, _cell;
        private readonly int _cols, _rows;
        private readonly int[] _cellStart; // CSR offsets, length _cols * _rows + 1
        private readonly int[] _cellWalls; // wall indices, grouped by cell

        private WallIndex(Segment[] walls, double originX, double originY, double cell, int cols, int rows,
            int[] cellStart, int[] cellWalls)
        {
            _walls = walls;
            _originX = originX;
            _originY = originY;
            _cell = cell;
            _cols = cols;
            _rows = rows;
            _cellStart = cellStart;
            _cellWalls = cellWalls;
        }

        /// <summary>File a set of walls into a fresh index. Pure: same segments in, same index out, and the
        /// same answers as sweeping the list by hand. A null or empty set indexes to open ground.</summary>
        public static WallIndex Build(IReadOnlyList<Segment>? walls)
        {
            Segment[] segments = walls is null ? [] : [.. walls];
            if (segments.Length == 0)
            {
                return new WallIndex(segments, 0, 0, BaseCell, 1, 1, [0, 0], []);
            }

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (Segment w in segments)
            {
                minX = System.Math.Min(minX, System.Math.Min(w.X1, w.X2));
                maxX = System.Math.Max(maxX, System.Math.Max(w.X1, w.X2));
                minY = System.Math.Min(minY, System.Math.Min(w.Y1, w.Y2));
                maxY = System.Math.Max(maxY, System.Math.Max(w.Y1, w.Y2));
            }

            double cell = BaseCell;
            int cols, rows;
            while (true)
            {
                cols = (int)System.Math.Floor((maxX - minX) / cell) + 1;
                rows = (int)System.Math.Floor((maxY - minY) / cell) + 1;
                if (cols < 1) { cols = 1; }
                if (rows < 1) { rows = 1; }
                if ((long)cols * rows <= MaxCells)
                {
                    break;
                }
                cell *= 2; // a ground too big for the grid gets coarser cells, never more of them
            }

            // Two passes over the walls: count per cell, then fill. CSR keeps the whole index in two flat
            // arrays — no per-cell lists, nothing to allocate at query time.
            int cellCount = cols * rows;
            var counts = new int[cellCount + 1];
            var index = new WallIndex(segments, minX, minY, cell, cols, rows, counts, []);
            int total = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                index.CellRange(segments[i], out int c0, out int c1, out int r0, out int r1);
                for (int r = r0; r <= r1; r++)
                {
                    for (int c = c0; c <= c1; c++)
                    {
                        counts[(r * cols) + c + 1]++;
                        total++;
                    }
                }
            }
            for (int i = 1; i <= cellCount; i++)
            {
                counts[i] += counts[i - 1];
            }
            var cursor = new int[cellCount];
            var items = new int[total];
            for (int i = 0; i < segments.Length; i++)
            {
                index.CellRange(segments[i], out int c0, out int c1, out int r0, out int r1);
                for (int r = r0; r <= r1; r++)
                {
                    for (int c = c0; c <= c1; c++)
                    {
                        int cellId = (r * cols) + c;
                        items[counts[cellId] + cursor[cellId]] = i;
                        cursor[cellId]++;
                    }
                }
            }
            return new WallIndex(segments, minX, minY, cell, cols, rows, counts, items);
        }

        /// <inheritdoc/>
        public int Count => _walls.Length;

        /// <inheritdoc/>
        public Segment this[int i] => _walls[i];

        /// <inheritdoc/>
        public IEnumerator<Segment> GetEnumerator() => ((IEnumerable<Segment>)_walls).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _walls.GetEnumerator();

        // The cells a segment's bounding box touches. Clamped into the grid, so a wall on the rim files
        // into the rim cell rather than falling off the edge.
        private void CellRange(in Segment w, out int c0, out int c1, out int r0, out int r1)
        {
            double loX = w.X1 < w.X2 ? w.X1 : w.X2, hiX = w.X1 < w.X2 ? w.X2 : w.X1;
            double loY = w.Y1 < w.Y2 ? w.Y1 : w.Y2, hiY = w.Y1 < w.Y2 ? w.Y2 : w.Y1;
            c0 = Col(loX);
            c1 = Col(hiX);
            r0 = Row(loY);
            r1 = Row(hiY);
        }

        private int Col(double x) =>
            System.Math.Clamp((int)System.Math.Floor((x - _originX) / _cell), 0, _cols - 1);

        private int Row(double y) =>
            System.Math.Clamp((int)System.Math.Floor((y - _originY) / _cell), 0, _rows - 1);

        /// <summary>How many walls a collision query at this spot would actually measure. The CEILING, made
        /// visible: this is the number the whole index exists to keep small, and the number a test can pin
        /// so "bounded" is a property the suite checks rather than a claim a comment makes. Answers nothing
        /// about the game — it is the cost of the answer, not the answer.</summary>
        public int CandidatesNear(double x, double y, double radius)
        {
            if (_walls.Length == 0)
            {
                return 0;
            }
            double reach = radius + BoxSlack;
            int c0 = Col(x - reach), c1 = Col(x + reach);
            int r0 = Row(y - reach), r1 = Row(y + reach);
            int n = 0;
            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    int cellId = (r * _cols) + c;
                    n += _cellStart[cellId + 1] - _cellStart[cellId];
                }
            }
            return n;
        }

        /// <summary>The bounded twin of <see cref="Blocked"/>: is any wall in the cells this body's own box
        /// touches close enough to stop it? Same exact test, on a handful of candidates.</summary>
        internal bool AnyBlocking(double x, double y, double radius)
        {
            if (_walls.Length == 0)
            {
                return false;
            }
            double reach = radius + BoxSlack;
            int c0 = Col(x - reach), c1 = Col(x + reach);
            int r0 = Row(y - reach), r1 = Row(y + reach);
            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    int cellId = (r * _cols) + c;
                    for (int k = _cellStart[cellId]; k < _cellStart[cellId + 1]; k++)
                    {
                        if (NearSegment(x, y, radius, _walls[_cellWalls[k]]))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>The bounded twin of <see cref="HasLineOfSight"/> (inverted): does any wall in the cells
        /// the sightline passes through cross it? Walks the line's cells along its dominant axis, so the
        /// work is the LENGTH of the look, never the size of the ground's wall list. A wall may be tested
        /// twice where the line clips two cells of its box — harmless, the answer is an OR.</summary>
        internal bool AnyCrossing(double ax, double ay, double bx, double by)
        {
            if (_walls.Length == 0)
            {
                return false;
            }
            double dx = bx - ax, dy = by - ay;
            if (System.Math.Abs(dx) >= System.Math.Abs(dy))
            {
                int c0 = Col(System.Math.Min(ax, bx)), c1 = Col(System.Math.Max(ax, bx));
                for (int c = c0; c <= c1; c++)
                {
                    // The stretch of the look that lies in this column, and the rows it spans there.
                    double x0 = _originX + (c * _cell), x1 = x0 + _cell;
                    SpanAt(ax, ay, by, dx, x0, x1, out double lo, out double hi);
                    int r0 = Row(lo), r1 = Row(hi);
                    if (ScanCells(ax, ay, bx, by, c, c, r0, r1))
                    {
                        return true;
                    }
                }
                return false;
            }
            int rr0 = Row(System.Math.Min(ay, by)), rr1 = Row(System.Math.Max(ay, by));
            for (int r = rr0; r <= rr1; r++)
            {
                double y0 = _originY + (r * _cell), y1 = y0 + _cell;
                SpanAt(ay, ax, bx, dy, y0, y1, out double lo, out double hi);
                int c0 = Col(lo), c1 = Col(hi);
                if (ScanCells(ax, ay, bx, by, c0, c1, r, r))
                {
                    return true;
                }
            }
            return false;
        }

        // Where the look is, on the OTHER axis, while it is inside [sliceLo, sliceHi] on the driving axis.
        // Grown by a cell on either side of the exact answer, so the slice arithmetic can never shave a cell
        // the line actually clips — the index errs toward extra candidates, never toward missing one.
        private void SpanAt(double driveA, double otherA, double otherB, double driveDelta,
            double sliceLo, double sliceHi, out double lo, out double hi)
        {
            if (System.Math.Abs(driveDelta) < 1e-12)
            {
                lo = System.Math.Min(otherA, otherB);
                hi = System.Math.Max(otherA, otherB);
                return;
            }
            double t0 = (sliceLo - driveA) / driveDelta;
            double t1 = (sliceHi - driveA) / driveDelta;
            double tLo = System.Math.Clamp(System.Math.Min(t0, t1), 0, 1);
            double tHi = System.Math.Clamp(System.Math.Max(t0, t1), 0, 1);
            double vLo = otherA + (tLo * (otherB - otherA));
            double vHi = otherA + (tHi * (otherB - otherA));
            lo = System.Math.Min(vLo, vHi) - _cell;
            hi = System.Math.Max(vLo, vHi) + _cell;
        }

        private bool ScanCells(double ax, double ay, double bx, double by, int c0, int c1, int r0, int r1)
        {
            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    int cellId = (r * _cols) + c;
                    for (int k = _cellStart[cellId]; k < _cellStart[cellId + 1]; k++)
                    {
                        Segment w = _walls[_cellWalls[k]];
                        if (BoxesOverlap(ax, ay, bx, by, w)
                            && SegmentsIntersect(ax, ay, bx, by, w.X1, w.Y1, w.X2, w.Y2))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
