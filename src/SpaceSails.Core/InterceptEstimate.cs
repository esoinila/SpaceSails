namespace SpaceSails.Core;

/// <summary>
/// M27: the war room's intercept clock for a piracy run. Given OUR projected course and a
/// target's predicted path, find where they come closest, when, and — the initiative roll —
/// when the range first drops inside a threshold (the boarding envelope, weapon range…).
/// Pure function of the two sample sets; the caller decides how honest the target's
/// projection is (gravity-only coast is the standard estimate for a freighter between burns).
/// </summary>
public static class InterceptEstimate
{
    public readonly record struct Result(
        double MinDistance, double MinSimTime,
        double? FirstWithinThresholdSimTime);

    /// <summary>
    /// Walks the overlapping time window segment by segment, treating BOTH paths as piecewise
    /// linear and taking each segment's minimum in CLOSED FORM — the relative motion across a
    /// segment is linear, so the minimum of |Δr(t)| and any threshold crossing are exact
    /// quadratic algebra, never a missed sample. This is Lab 06's fast-graze fix: the old
    /// per-sample check let a fast crossing tunnel clean between two samples. (What remains
    /// approximate is the piecewise-linear model itself; live hit resolution re-checks every
    /// integrator step with the same closed form.) Null when the windows don't overlap.
    /// </summary>
    public static Result? Against(
        IReadOnlyList<TrajectorySample> ours,
        IReadOnlyList<TrajectorySample> theirs,
        double thresholdMeters)
    {
        if (ours.Count < 2 || theirs.Count < 2)
        {
            return null;
        }

        double start = Math.Max(ours[0].SimTime, theirs[0].SimTime);
        double end = Math.Min(ours[^1].SimTime, theirs[^1].SimTime);
        if (end <= start)
        {
            return null;
        }

        double minDistance = double.MaxValue;
        double minTime = start;
        double? firstWithin = null;
        int j = 0;
        for (int i = 0; i < ours.Count - 1; i++)
        {
            double t0 = Math.Max(ours[i].SimTime, start);
            double t1 = Math.Min(ours[i + 1].SimTime, end);
            if (t1 <= t0)
            {
                continue;
            }

            while (j < theirs.Count - 2 && theirs[j + 1].SimTime < t1)
            {
                // Their sampling can be finer than ours: split our segment at their knots so
                // every evaluated stretch is truly linear on both sides.
                double knot = Math.Clamp(theirs[j + 1].SimTime, t0, t1);
                ScanLinearStretch(ours, theirs, i, j, t0, knot, thresholdMeters,
                    ref minDistance, ref minTime, ref firstWithin);
                t0 = knot;
                j++;
            }

            ScanLinearStretch(ours, theirs, i, j, t0, t1, thresholdMeters,
                ref minDistance, ref minTime, ref firstWithin);
        }

        return minDistance == double.MaxValue ? null : new Result(minDistance, minTime, firstWithin);
    }

    /// <summary>M28: the closed-form segment check reused by live hit resolution — the minimum
    /// separation of two linearly-moving points over one time step, and the earliest moment
    /// the separation drops inside a threshold. No step size can tunnel through it.</summary>
    public static (double MinDistance, double MinTimeFraction, double? WithinFraction) SegmentMin(
        Vector2d relStart, Vector2d relEnd, double thresholdMeters)
    {
        Vector2d dr = relEnd - relStart;
        double drSq = dr.LengthSquared;
        double s = drSq > 0 ? Math.Clamp(-relStart.Dot(dr) / drSq, 0, 1) : 0;
        double min = (relStart + dr * s).Length;

        double? within = null;
        if (relStart.Length <= thresholdMeters)
        {
            within = 0;
        }
        else if (drSq > 0)
        {
            double b = 2 * relStart.Dot(dr);
            double c = relStart.LengthSquared - thresholdMeters * thresholdMeters;
            double disc = b * b - 4 * drSq * c;
            if (disc >= 0)
            {
                double sIn = (-b - Math.Sqrt(disc)) / (2 * drSq);
                if (sIn >= 0 && sIn <= 1)
                {
                    within = sIn;
                }
            }
        }

        return (min, s, within);
    }

    private static void ScanLinearStretch(
        IReadOnlyList<TrajectorySample> ours, IReadOnlyList<TrajectorySample> theirs,
        int i, int j, double t0, double t1, double thresholdMeters,
        ref double minDistance, ref double minTime, ref double? firstWithin)
    {
        if (t1 <= t0)
        {
            return;
        }

        Vector2d r0 = PositionAt(ours, i, t0) - PositionAt(theirs, j, t0);
        Vector2d r1 = PositionAt(ours, i, t1) - PositionAt(theirs, j, t1);
        (double min, double sMin, double? within) = SegmentMin(r0, r1, thresholdMeters);

        if (min < minDistance)
        {
            (minDistance, minTime) = (min, t0 + (t1 - t0) * sMin);
        }

        if (firstWithin is null && within is { } sIn)
        {
            firstWithin = t0 + (t1 - t0) * sIn;
        }
    }

    private static Vector2d PositionAt(IReadOnlyList<TrajectorySample> samples, int i, double t)
    {
        TrajectorySample a = samples[i];
        TrajectorySample b = samples[i + 1];
        double span = b.SimTime - a.SimTime;
        double f = span > 0 ? Math.Clamp((t - a.SimTime) / span, 0, 1) : 0;
        return a.Position + (b.Position - a.Position) * f;
    }
}
