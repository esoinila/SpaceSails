using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #955 NAV-2 · THE WINDOW THAT READS THE ROUTE. The owner's payoff for the unified nav list (2026-08-23):
/// <b>"with a pre-plotted, armed route you can LEAVE the ship by shuttle mid-voyage (meteorites, small
/// sites) with a return-window clock on the captain's remote — without a planned route you risk leaving
/// the ship adrift"</b>, and <b>"a sensitive visit could have separate ingress and egress shuttle
/// windows"</b>.
///
/// <para>The ship is never adrift, because the ship keeps flying the armed plan (#969's plan-time promise
/// finishes the trip with no input at all). YOU are the one who can be left behind — so the only number
/// that matters is <b>RETURN BY</b>: the moment the boat must lift off the rock to still catch a ship that
/// is not waiting.</para>
///
/// <para>This is the pure spine. It invents no physics and no second projection: the caller hands in the
/// separations it ALREADY sampled — the same plotted-path samples <c>ArrivePassFor</c> reads — and this
/// slices them into the spans that lie inside one shuttle hop. Two disjoint spans is not an error: that is
/// exactly the sensitive visit's INGRESS and EGRESS windows, a periodic pass that comes round twice.</para>
/// </summary>
public static class RouteShuttleWindow
{
    /// <summary>One sample of the plotted path against one site: when the ship is there, and how far the
    /// site is from her at that instant. Straight off the projection the planner already runs.</summary>
    public readonly record struct RouteSample(double SimTime, double SeparationMeters);

    /// <summary>
    /// One shuttle window on the route: the ship's path is inside a hop of the site from
    /// <see cref="OpensSimTime"/> (INGRESS — the earliest the boat can cross) to <see cref="ClosesSimTime"/>
    /// (EGRESS — the last instant the gap is crossable at all), and the captain must be off the ground by
    /// <see cref="ReturnBySimTime"/> to make it.
    /// </summary>
    public readonly record struct Window(double OpensSimTime, double ClosesSimTime, double ReturnBySimTime)
    {
        /// <summary>How long the path stays inside a hop (seconds) — the window's own width, before the ride
        /// home is taken out of it.</summary>
        public double SpanSeconds => System.Math.Max(0.0, ClosesSimTime - OpensSimTime);

        /// <summary>How long the captain actually gets on the rock: the span minus the ride home. Zero when
        /// the crossing eats the whole window.</summary>
        public double AshoreSeconds => System.Math.Max(0.0, ReturnBySimTime - OpensSimTime);

        /// <summary>Whether this window is worth offering at all — there is time to land, stand up, and lift
        /// off again. A window whose whole width is swallowed by the return crossing is a window you can
        /// only miss, and the board must not offer it.</summary>
        public bool IsUsable => AshoreSeconds > 0.0;
    }

    /// <summary>
    /// The shuttle windows the plotted path opens on one site, in path order. A span runs while the sampled
    /// separation is within <paramref name="rangeMeters"/>; its edges are linearly interpolated between the
    /// bracketing samples, so the numbers do not jitter with the projection's sample stride.
    ///
    /// <para>RETURN BY is the egress minus the ride home, and the ride home is priced at the separation the
    /// boat will actually face at that egress (<see cref="ShuttleRange.TravelSeconds"/>). Where a span ends
    /// because the ship left reach, that is a full-reach crossing — the widest gap the boat can cross, and
    /// the reason a window shuts a long time before it looks like it should. Where a span ends because the
    /// SAMPLES ran out (the plan's own horizon), the last sample's real separation is used instead, so a
    /// short plan is not charged for a crossing it never has to make.</para>
    /// </summary>
    public static IReadOnlyList<Window> Along(
        IReadOnlyList<RouteSample> samples, double rangeMeters = ShuttleRange.RangeMeters)
    {
        System.ArgumentNullException.ThrowIfNull(samples);
        var windows = new List<Window>();
        if (samples.Count == 0 || rangeMeters <= 0.0)
        {
            return windows;
        }

        bool inside = false;
        double opens = 0.0;
        for (int i = 0; i < samples.Count; i++)
        {
            RouteSample s = samples[i];
            bool here = s.SeparationMeters <= rangeMeters;
            if (here && !inside)
            {
                inside = true;
                opens = i == 0 ? s.SimTime : Crossing(samples[i - 1], s, rangeMeters);
            }
            else if (!here && inside)
            {
                inside = false;
                double closes = Crossing(samples[i - 1], s, rangeMeters);
                windows.Add(Close(opens, closes, rangeMeters));
            }
        }

        if (inside)
        {
            // The samples ran out while still in reach — the plan's horizon closes the window, not the
            // geometry, so the ride home is priced at the gap actually standing there.
            RouteSample last = samples[^1];
            windows.Add(Close(opens, last.SimTime, last.SeparationMeters));
        }

        return windows;
    }

    /// <summary>Compose one window from its two edges and the separation the return crossing must fly.</summary>
    private static Window Close(double opens, double closes, double crossingMeters) =>
        new(opens, closes, closes - ShuttleRange.TravelSeconds(crossingMeters));

    /// <summary>The sim time the separation crosses <paramref name="rangeMeters"/> between two samples,
    /// linearly interpolated. A pair that does not straddle the edge falls back to the later sample.</summary>
    private static double Crossing(RouteSample a, RouteSample b, double rangeMeters)
    {
        double span = b.SeparationMeters - a.SeparationMeters;
        if (System.Math.Abs(span) < double.Epsilon)
        {
            return b.SimTime;
        }

        double f = (rangeMeters - a.SeparationMeters) / span;
        if (f is < 0.0 or > 1.0)
        {
            return b.SimTime;
        }

        return a.SimTime + ((b.SimTime - a.SimTime) * f);
    }

    // ── The copy: ONE builder, so the remote in the captain's fist and the away-site HUD line can never
    // quote two different numbers for the same window (the house law against parallel surfaces). ──

    /// <summary>The absolute ship-clock stamp a RETURN BY reads: the sim clock's own day-and-time idiom
    /// ("3d 07:41"). The day is kept because a plotted route runs for months and an "07:41" with no day on
    /// it is a lie a captain would act on.</summary>
    public static string Stamp(double simTime)
    {
        var span = System.TimeSpan.FromSeconds(System.Math.Clamp(simTime, 0, System.TimeSpan.MaxValue.TotalSeconds - 1));
        return string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalDays}d {span.Hours:00}:{span.Minutes:00}");
    }

    /// <summary>A coarse "in X" for a wait: minutes, then hours, then days. The house voice's own ladder
    /// (LedgerClock's, read forwards).</summary>
    public static string In(double seconds)
    {
        double s = System.Math.Max(0.0, seconds);
        if (s < 60)
        {
            return "under a minute";
        }

        long minutes = (long)(s / 60);
        if (minutes < 60)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{minutes} m");
        }

        long hours = minutes / 60;
        if (hours < 24)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{hours} h {minutes % 60:00} m");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{hours / 24} d {hours % 24:00} h");
    }

    /// <summary>
    /// The captain's remote's own line, and the away-site HUD's — ONE builder, so a captain who checks the
    /// handset and then looks at the HUD can never be shown two different numbers for one window.
    ///
    /// <para>Three things a captain standing on a rock can need, in priority order: a ship that is flying on
    /// without him (<paramref name="returnBySimTime"/> — the hard deadline), a window that is merely running
    /// out (<paramref name="secondsLeftInRange"/>, finite), or nothing at all. Then, if the window is shut,
    /// how long the wait is (<paramref name="reopenSeconds"/>) — or that there is no wait, only the dark.</para>
    /// </summary>
    public static string RemoteLine(
        double nowSimTime, double? returnBySimTime, double? reopenSeconds,
        double secondsLeftInRange = double.PositiveInfinity)
    {
        string head = returnBySimTime is { } by
            ? $"RETURN BY {Stamp(by)} ({In(by - nowSimTime)})"
            : !double.IsPositiveInfinity(secondsLeftInRange)
            ? $"WINDOW CLOSES IN {In(secondsLeftInRange)}"
            : "NO RETURN WINDOW";

        // The tail says whether there is a SECOND chance. It earns its place when there is a wait to name,
        // and when the captain is under a hard deadline (a RETURN BY is worth much more when you also know
        // nothing comes round again). It is left off only where it would be chatter: a window that is merely
        // running out, with nothing flying on — there, "no next window" answers a question nobody asked.
        if (reopenSeconds is { } r)
        {
            return $"🛸 {head} · next window in {In(r)}";
        }

        return returnBySimTime is null && !double.IsPositiveInfinity(secondsLeftInRange)
            ? $"🛸 {head}"
            : $"🛸 {head} · no next window";
    }
}
