namespace SpaceSails.Core;

/// <summary>
/// #402 — nav-map label de-collision. When bodies cluster at a given zoom (the deflection gig's
/// Saturn cluster is the worst case: Ringside Exchange + Ringside's Depot + Saturn Depot + the
/// inbound rock), their text labels render directly on top of one another into an unreadable smear.
///
/// This resolver is a cheap, honest, immediate-mode declutter: it works in SCREEN SPACE over the
/// visible label set, so it is zoom-agnostic (overlap is measured in pixels, whatever the zoom).
/// Each candidate carries a PRIORITY; the highest-priority labels are placed first and always win.
/// A lower-priority label that would overlap an already-placed one is first NUDGED down by its line
/// height (simple vertical stacking) if that clears the collision, and CULLED (not drawn) if it
/// still collides — never drawn atop. No force-directed solver, no leader lines.
///
/// The caller assigns priority so the labels that carry the money-moment always survive: the
/// deflection THREAT ROCK and the DOCKED station outrank depots and minor bodies. Pure and tiny by
/// design (screen rects + priorities in, draw/nudge decisions out), so the declutter rule is settled
/// in Core and pinned by tests rather than inferred from the canvas draw path.
/// </summary>
public static class LabelPlacement
{
    /// <summary>An axis-aligned screen-space rectangle (pixels, origin top-left, Y down).</summary>
    public readonly record struct Rect(double X, double Y, double Width, double Height)
    {
        public double Right => X + Width;
        public double Bottom => Y + Height;

        /// <summary>True when this rect shares any interior area with <paramref name="other"/>.
        /// Edge-touching (zero overlap area) does not count as a collision.</summary>
        public bool Overlaps(Rect other) =>
            X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;
    }

    /// <summary>A label the caller wants placed. <paramref name="Key"/> is an opaque caller id echoed
    /// back in the result; <paramref name="Priority"/> is HIGHER-WINS; <paramref name="LineHeight"/>
    /// is the vertical step used when nudging a colliding label clear (0 disables nudging for it).</summary>
    public readonly record struct Candidate(int Key, Rect Rect, int Priority, double LineHeight);

    /// <summary>The decision for one candidate: whether to draw it, and the (possibly nudged) rect to
    /// draw it at. Returned in the SAME order as the input candidates.</summary>
    public readonly record struct Placement(int Key, Rect Rect, bool Draw);

    /// <summary>
    /// Resolve which labels to draw and where. Highest priority first (ties keep input order); each
    /// survivor reserves its rect. A candidate that collides with a reserved rect is nudged down by
    /// up to <paramref name="maxNudgeSteps"/> line-heights to find clear space, else it is culled.
    /// </summary>
    public static IReadOnlyList<Placement> Resolve(
        IReadOnlyList<Candidate> candidates, int maxNudgeSteps = 2)
    {
        int n = candidates.Count;
        var result = new Placement[n];
        if (n == 0)
        {
            return result;
        }

        // Stable priority order: sort indices by priority DESC, original index ASC for ties.
        var order = new int[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
        }
        Array.Sort(order, (a, b) =>
        {
            int byPriority = candidates[b].Priority.CompareTo(candidates[a].Priority);
            return byPriority != 0 ? byPriority : a.CompareTo(b);
        });

        var reserved = new List<Rect>(n);
        foreach (int idx in order)
        {
            Candidate c = candidates[idx];
            Rect rect = c.Rect;

            bool placed = !CollidesWithAny(rect, reserved);
            // Nudge downward by whole line-heights to find clear space (simple stacking), if enabled.
            if (!placed && c.LineHeight > 0)
            {
                for (int step = 1; step <= maxNudgeSteps && !placed; step++)
                {
                    Rect nudged = rect with { Y = rect.Y + c.LineHeight * step };
                    if (!CollidesWithAny(nudged, reserved))
                    {
                        rect = nudged;
                        placed = true;
                    }
                }
            }

            if (placed)
            {
                reserved.Add(rect);
                result[idx] = new Placement(c.Key, rect, Draw: true);
            }
            else
            {
                // Culled — still echo the key and its natural rect so the caller can dim/skip it.
                result[idx] = new Placement(c.Key, c.Rect, Draw: false);
            }
        }

        return result;
    }

    private static bool CollidesWithAny(Rect rect, List<Rect> reserved)
    {
        foreach (Rect r in reserved)
        {
            if (rect.Overlaps(r))
            {
                return true;
            }
        }
        return false;
    }
}
