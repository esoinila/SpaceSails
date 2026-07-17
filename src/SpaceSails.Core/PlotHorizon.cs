namespace SpaceSails.Core;

/// <summary>
/// The plotting ribbon's horizon arithmetic, pulled out of <c>Map.razor</c> so it can be unit-tested
/// without a browser (#209). Two pure decisions live here:
///
/// <list type="number">
/// <item><b>The auto PROJECTION length</b> (<see cref="AutoProjectionSeconds"/>) — how many seconds of
/// trajectory to integrate when the captain leaves Path-length on "auto". With a plan it reaches the
/// plan's furthest encounter (plus a margin); with no plan it holds a short local default. Always
/// clamped to the projection cap so a runaway plan can't ask for an un-affordable re-projection.</item>
///
/// <item><b>The DRAWN ribbon length in a co-moving frame</b> (<see cref="DrawnWindow"/>) — #145 scales
/// the drawn ribbon to ~1.25 local orbital periods of the frame body so a moon-system view doesn't coil
/// into a spirograph. #209's bug: that same crop silently shrank a Venus→Mars departure ribbon down to
/// a Venus loop when the captain had picked the Venus frame. The fix here makes the crop PLAN-AWARE —
/// the drawn window is floored up to the plan's furthest encounter so the ribbon always reaches the
/// plan — and, whenever the ribbon still ends short of either the full projection or the plan, it
/// reports WHY with a <see cref="RibbonNote"/> so the panel can say the state out loud rather than
/// showing a silently short picture.</item>
/// </list>
///
/// Both take the plan's furthest-encounter epoch as an INPUT (seconds ahead of "now"); the caller reads
/// it from the one true source — the autopilot rehearsal's arrival, the plotted destination pass, the
/// live burn nodes — so there is never a second, disagreeing estimator (the one-truth rule).
/// </summary>
public static class PlotHorizon
{
    // Comparisons are slack by an hour so a ribbon that reaches the plan to within a sample step is not
    // flagged as "short", and the note doesn't flicker as the sim ticks.
    private const double ToleranceSeconds = 3600.0;

    /// <summary>
    /// The auto projection length (seconds). <paramref name="planFurthestSeconds"/> is the plan's
    /// furthest encounter ahead of now (≤ 0 when there is no plan). With a plan the horizon reaches it
    /// plus <paramref name="marginSeconds"/>; with none it is <paramref name="minSeconds"/>. Always
    /// clamped to [<paramref name="minSeconds"/>, <paramref name="capSeconds"/>].
    /// </summary>
    public static double AutoProjectionSeconds(
        double planFurthestSeconds, double minSeconds, double marginSeconds, double capSeconds)
    {
        double horizon = planFurthestSeconds > 0 ? planFurthestSeconds + marginSeconds : minSeconds;
        return Math.Clamp(horizon, minSeconds, capSeconds);
    }

    /// <summary>
    /// #265 — cap the projection/ribbon horizon for a CAPTURED ship. A bound orbit projected for the
    /// full solar-leg horizon draws many precessing revolutions (the owner's Uranus "eight-petal flower"),
    /// and the petal-to-petal drift is the projection integrator wandering through the deep-periapsis
    /// passes, not an honest future. So once the achieved orbit is bound — <paramref name="boundPeriodSeconds"/>
    /// is <see cref="OrbitRule.BoundOrbitPeriod"/>, null on an unbound leg — the horizon is capped to about
    /// one revolution (<paramref name="revolutions"/> × period; a touch over 1 so the ribbon closes its
    /// loop and reads "and so on", not a hard chop). Transfer/hyperbolic legs (null period) and a bound
    /// ship with a plotted departure that genuinely extends the future (<paramref name="planFurthestSeconds"/>
    /// &gt; 0) keep the full length.
    /// </summary>
    public static double BoundOrbitHorizon(
        double fullHorizonSeconds, double? boundPeriodSeconds, double planFurthestSeconds, double revolutions = 1.15)
    {
        // Cap only a genuinely bound orbit (finite, positive period) that no plotted departure extends;
        // an unbound leg (null period) or a plan reaching past the park keeps the full-length ribbon.
        if (boundPeriodSeconds is { } period && period > 0 && !(planFurthestSeconds > 0))
        {
            return Math.Min(fullHorizonSeconds, revolutions * period);
        }
        return fullHorizonSeconds;
    }

    /// <summary>Why the drawn ribbon is the length it is — the honesty note the panel prints.</summary>
    public enum RibbonNote
    {
        /// <summary>The ribbon reaches the full projection (and the plan): nothing to explain.</summary>
        None,

        /// <summary>A co-moving frame cropped the ribbon to its ~1.25 local periods and no plan pushed
        /// it further — the picture is frame-scaled, not the whole story. ("ribbon: 1.25 Venus periods")</summary>
        FrameLocalPeriods,

        /// <summary>The ribbon ends before the plan does — the projection cap (a performance limit) can't
        /// reach the furthest encounter. The panel must say how short, and where the plan runs to.</summary>
        CappedShortOfPlan,
    }

    /// <summary>The drawn ribbon length + the note explaining it.</summary>
    public readonly record struct RibbonResult(double DrawnSeconds, RibbonNote Note);

    /// <summary>
    /// Resolve the drawn ribbon length in a (maybe) co-moving frame.
    ///
    /// <para><paramref name="localWindowSeconds"/> is ~1.25 local orbital periods of the frame body, or
    /// ≤ 0 when there is nothing to scale to (the Sun / inertial frame, or a mass-less dock) — in which
    /// case the ribbon is the full projection, uncropped.</para>
    ///
    /// <para><paramref name="planFurthestSeconds"/> is the plan's furthest encounter ahead of now
    /// (≤ 0 when there is no plan); it FLOORS the crop so the ribbon reaches the plan.
    /// <paramref name="baseFloorSeconds"/> is the frame-independent floor (a few hours + the next
    /// imminent node margin) that keeps the near-term course readable. The window never exceeds
    /// <paramref name="fullHorizonSeconds"/>, the length actually projected.</para>
    /// </summary>
    public static RibbonResult DrawnWindow(
        double fullHorizonSeconds, double localWindowSeconds, double planFurthestSeconds, double baseFloorSeconds)
    {
        double plan = Math.Max(0, planFurthestSeconds);

        // No scalable frame (Sun / dock): draw the full projection. The only dishonesty possible here is
        // the projection cap itself falling short of the plan.
        if (!(localWindowSeconds > 0))
        {
            RibbonNote capNote = plan > fullHorizonSeconds + ToleranceSeconds
                ? RibbonNote.CappedShortOfPlan
                : RibbonNote.None;
            return new RibbonResult(fullHorizonSeconds, capNote);
        }

        double floor = Math.Max(baseFloorSeconds, plan);
        double window = Math.Min(Math.Max(localWindowSeconds, floor), fullHorizonSeconds);

        RibbonNote note =
            plan > window + ToleranceSeconds ? RibbonNote.CappedShortOfPlan          // can't reach the plan (cap)
            : plan <= 0 && window < fullHorizonSeconds - ToleranceSeconds ? RibbonNote.FrameLocalPeriods // local-period crop, no plan
            : RibbonNote.None;                                                        // reaches the plan / the full ribbon

        return new RibbonResult(window, note);
    }
}
