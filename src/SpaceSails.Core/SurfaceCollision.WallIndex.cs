namespace SpaceSails.Core;

/// <summary>
/// #251 · A CEILING ON THE STONE (#448) — the coarse uniform grid over the walls, the nested
/// <c>WallIndex</c>, with its design record.
///
/// <para>Split out of <c>SurfaceCollision.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. The nested class travels whole with the banner that argues for it;
/// <c>HasLineOfSight</c> and the gait seam stay in the opening file, which two guards name by file.</para>
/// </summary>
public static partial class SurfaceCollision
{
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
