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
    /// Walks OUR samples, linearly interpolating THEIR path at each of our sample times over
    /// the overlapping time window. Null when the windows don't overlap at all.
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
        foreach (TrajectorySample sample in ours)
        {
            if (sample.SimTime < start || sample.SimTime > end)
            {
                continue;
            }

            while (j < theirs.Count - 2 && theirs[j + 1].SimTime < sample.SimTime)
            {
                j++;
            }

            TrajectorySample a = theirs[j];
            TrajectorySample b = theirs[j + 1];
            double span = b.SimTime - a.SimTime;
            double f = span > 0 ? Math.Clamp((sample.SimTime - a.SimTime) / span, 0, 1) : 0;
            Vector2d theirPos = a.Position + (b.Position - a.Position) * f;
            double d = (sample.Position - theirPos).Length;

            if (d < minDistance)
            {
                (minDistance, minTime) = (d, sample.SimTime);
            }

            if (firstWithin is null && d <= thresholdMeters)
            {
                firstWithin = sample.SimTime;
            }
        }

        return minDistance == double.MaxValue ? null : new Result(minDistance, minTime, firstWithin);
    }
}
