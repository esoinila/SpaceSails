namespace SpaceSails.Core;

/// <summary>
/// Reachability-as-geometry for the HUD's stacked overlays (#293) — the #253 (menu viewport clamp),
/// #195/#236 (layout laws) and #212 (affordances never hide) family, generalized from "one menu on
/// screen" to "is this critical control actually pressable?".
///
/// <para>The owner's report: "the rescue-me button was barely clickable when we ran out of power…
/// this problem keeps biting us." It keeps biting because reachability of a control buried in a
/// ~15-layer overlay stack is emergent — it depends on which panels are raised, their z-order, their
/// footprints and the viewport size — and no one can eyeball that. So we compute it.</para>
///
/// <para>Pure geometry, kept out of the razor so it unit-tests without a browser: a control is a
/// rectangle with a z-index and a pointer-events flag; the concurrently-raised overlays are the same.
/// The law measures how much of the control's hit-rect survives — on-screen and painted over by no
/// higher pointer-events layer. Zero survives → <see cref="ReachVerdict.Occluded"/>. A sliver survives
/// → <see cref="ReachVerdict.BarelyClickable"/> (the owner's exact complaint, named). A full-screen
/// gate that legitimately owns the screen (a modal that supplies its own resolution) is recorded as a
/// supersede, not counted as an occluder — only PARTIAL overlays burying a lifeline are the bug.</para>
/// </summary>
public static class OverlayLayout
{
    /// <summary>Breathing room kept from each viewport edge; a control's hit-rect outside this inset
    /// is treated as off-screen (matches the #253 menu-clamp margin).</summary>
    public const double DefaultMarginPx = 6;

    /// <summary>The smallest square, per side in CSS px, a hit target must expose to count as pressable.
    /// 24 px is the WCAG 2.5.8 (AA) minimum target size; below it a control is "barely clickable".</summary>
    public const double MinTapPx = 24;

    /// <summary>An overlay covering at least this fraction of the viewport is a full-screen gate — it
    /// legitimately owns the screen (its own modal resolves the state), so it supersedes rather than
    /// occludes the lifeline. Anchored/partial panels below this are real occluders.</summary>
    public const double FullScreenFraction = 0.95;

    /// <summary>An axis-aligned rectangle in CSS pixels, top-left origin (y grows downward, as on a
    /// web page). Immutable value type so fixtures read like coordinates.</summary>
    public readonly record struct Rect(double X, double Y, double W, double H)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
        public double Area => Math.Max(0, W) * Math.Max(0, H);

        /// <summary>True when the two rectangles share any interior area (edge-touch is not overlap).</summary>
        public bool Overlaps(Rect other) =>
            X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

        /// <summary>The overlapping rectangle, or a zero-area rect when they do not overlap.</summary>
        public Rect Intersect(Rect other)
        {
            double x = Math.Max(X, other.X);
            double y = Math.Max(Y, other.Y);
            double r = Math.Min(Right, other.Right);
            double b = Math.Min(Bottom, other.Bottom);
            return new Rect(x, y, Math.Max(0, r - x), Math.Max(0, b - y));
        }
    }

    /// <summary>One layer of the HUD: a name (for diagnostics), its footprint, its stacking order, and
    /// whether it actually swallows clicks (<c>pointer-events: auto</c>). A layer with
    /// <c>PointerEvents = false</c> paints but never occludes — the #195 gaps-pass-clicks pattern.</summary>
    public readonly record struct Overlay(string Name, Rect Bounds, int ZIndex, bool PointerEvents = true);

    /// <summary>Why a control is (not) reachable. <see cref="Reachable"/> is the only passing verdict.</summary>
    public enum ReachVerdict
    {
        /// <summary>On-screen, enabled, and enough of its hit-rect is clear of every higher layer.</summary>
        Reachable,

        /// <summary>The control is flagged disabled — present but inert (a distress state must never
        /// disable the rescue affordance).</summary>
        Disabled,

        /// <summary>Its hit-rect falls (almost) entirely outside the viewport's safe inset.</summary>
        OffViewport,

        /// <summary>A higher pointer-events layer paints over the whole hit-rect — no click lands.</summary>
        Occluded,

        /// <summary>A sliver survives, but smaller than <see cref="MinTapPx"/> a side — the owner's
        /// "barely clickable". Present, technically hittable, practically not.</summary>
        BarelyClickable,
    }

    /// <summary>The measured outcome. Every field is a real number a probe can print — the free
    /// fraction, the surviving box, and which layers ate the rest.</summary>
    public readonly record struct ReachResult(
        ReachVerdict Verdict,
        double ControlArea,
        double FreeArea,
        double FreeWidth,
        double FreeHeight,
        IReadOnlyList<string> OccludedBy,
        IReadOnlyList<string> SupersededBy)
    {
        /// <summary>True only for <see cref="ReachVerdict.Reachable"/>.</summary>
        public bool Ok => Verdict == ReachVerdict.Reachable;

        /// <summary>Fraction of the control's own area that stays clear and on-screen (0..1).</summary>
        public double FreeFraction => ControlArea <= 0 ? 0 : FreeArea / ControlArea;
    }

    /// <summary>
    /// Measure whether <paramref name="control"/> is pressable given the <paramref name="others"/>
    /// raised at the same time. A layer occludes the control when it (a) swallows clicks, (b) stacks
    /// strictly above it, (c) overlaps its hit-rect, and (d) is not a full-screen gate. The surviving
    /// (clear, on-screen) region of the control is measured exactly by coordinate compression.
    /// </summary>
    /// <param name="control">The critical control being audited (e.g. the rescue affordance).</param>
    /// <param name="viewport">The visible canvas, top-left origin.</param>
    /// <param name="others">Every overlay that can be raised concurrently with the control.</param>
    /// <param name="enabled">False models a distress state that greyed the control out.</param>
    /// <param name="minTapPx">Smallest square side that still counts as pressable.</param>
    /// <param name="margin">Safe inset kept from each viewport edge.</param>
    public static ReachResult Evaluate(
        Overlay control,
        Rect viewport,
        IEnumerable<Overlay> others,
        bool enabled = true,
        double minTapPx = MinTapPx,
        double margin = DefaultMarginPx)
    {
        ArgumentNullException.ThrowIfNull(others);

        double controlArea = control.Bounds.Area;

        if (!enabled)
        {
            return new ReachResult(ReachVerdict.Disabled, controlArea, 0, 0, 0, [], []);
        }

        // Clip the control's hit-rect to the viewport's safe inset — anything past the edge cannot be
        // pressed (the #253 off-screen failure), so it does not count toward the free area.
        Rect safe = new(
            viewport.X + margin,
            viewport.Y + margin,
            Math.Max(0, viewport.W - 2 * margin),
            Math.Max(0, viewport.H - 2 * margin));
        Rect onScreen = control.Bounds.Intersect(safe);

        if (onScreen.Area <= 0)
        {
            return new ReachResult(ReachVerdict.OffViewport, controlArea, 0, 0, 0, [], []);
        }

        // Split the raised layers: higher pointer-events overlays that overlap the control are the
        // candidate occluders; those big enough to be a full-screen gate supersede instead.
        double viewportArea = viewport.Area;
        List<Rect> occluderRects = [];
        List<string> occludedBy = [];
        List<string> supersededBy = [];
        foreach (Overlay o in others)
        {
            if (!o.PointerEvents || o.ZIndex <= control.ZIndex || !o.Bounds.Overlaps(onScreen))
            {
                continue;
            }

            bool fullScreenGate = viewportArea > 0
                && o.Bounds.Intersect(viewport).Area >= FullScreenFraction * viewportArea;
            if (fullScreenGate)
            {
                supersededBy.Add(o.Name);
            }
            else
            {
                occluderRects.Add(o.Bounds.Intersect(onScreen));
                occludedBy.Add(o.Name);
            }
        }

        (double freeArea, double freeW, double freeH) = FreeRegion(onScreen, occluderRects);

        ReachVerdict verdict;
        if (freeArea <= 0)
        {
            verdict = ReachVerdict.Occluded;
        }
        else if (freeW < minTapPx || freeH < minTapPx || freeArea < minTapPx * minTapPx)
        {
            verdict = ReachVerdict.BarelyClickable;
        }
        else
        {
            verdict = ReachVerdict.Reachable;
        }

        return new ReachResult(verdict, controlArea, freeArea, freeW, freeH, occludedBy, supersededBy);
    }

    /// <summary>
    /// Exact area (and bounding box) of <paramref name="region"/> not covered by any of
    /// <paramref name="occluders"/>, via coordinate compression: the union of every rectangle's edges
    /// carves the region into cells; a cell is free when its centre sits in no occluder. Exact for
    /// axis-aligned rectangles and deterministic — no sampling.
    /// </summary>
    private static (double Area, double Width, double Height) FreeRegion(Rect region, List<Rect> occluders)
    {
        if (occluders.Count == 0)
        {
            return (region.Area, region.W, region.H);
        }

        SortedSet<double> xsSet = [region.X, region.Right];
        SortedSet<double> ysSet = [region.Y, region.Bottom];
        foreach (Rect o in occluders)
        {
            if (o.X > region.X && o.X < region.Right) { xsSet.Add(o.X); }
            if (o.Right > region.X && o.Right < region.Right) { xsSet.Add(o.Right); }
            if (o.Y > region.Y && o.Y < region.Bottom) { ysSet.Add(o.Y); }
            if (o.Bottom > region.Y && o.Bottom < region.Bottom) { ysSet.Add(o.Bottom); }
        }

        double[] xs = [.. xsSet];
        double[] ys = [.. ysSet];

        double freeArea = 0;
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;

        for (int i = 0; i < xs.Length - 1; i++)
        {
            double cx = (xs[i] + xs[i + 1]) / 2;
            double cw = xs[i + 1] - xs[i];
            for (int j = 0; j < ys.Length - 1; j++)
            {
                double cy = (ys[j] + ys[j + 1]) / 2;
                double ch = ys[j + 1] - ys[j];

                bool covered = false;
                foreach (Rect o in occluders)
                {
                    if (cx > o.X && cx < o.Right && cy > o.Y && cy < o.Bottom)
                    {
                        covered = true;
                        break;
                    }
                }

                if (!covered)
                {
                    freeArea += cw * ch;
                    minX = Math.Min(minX, xs[i]);
                    maxX = Math.Max(maxX, xs[i + 1]);
                    minY = Math.Min(minY, ys[j]);
                    maxY = Math.Max(maxY, ys[j + 1]);
                }
            }
        }

        if (freeArea <= 0)
        {
            return (0, 0, 0);
        }

        return (freeArea, maxX - minX, maxY - minY);
    }
}
