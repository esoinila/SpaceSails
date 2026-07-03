namespace SpaceSails.Core;

/// <summary>
/// Scans a projected trajectory for its closest pass by each celestial body, so the planner
/// can warn the captain before the ribbon threads a planet (owner, M18: "It would be
/// embarrassing space captaining to plot through a planet"). Severity is measured in body
/// radii — a 1.2 R sun-grazer outranks a 300 R Earth flyby.
/// </summary>
public static class ClosestApproach
{
    public readonly record struct Pass(
        string BodyId, string BodyName, double BodyRadius,
        double Distance, double SimTime, Vector2d ShipPosition)
    {
        public double Severity => Distance / BodyRadius;
        public bool Impact => Distance < BodyRadius;
    }

    /// <summary>
    /// The single most severe pass along the sampled path, or null when there are no samples.
    /// Coarse-strides the samples (≤ maxEvaluationsPerBody per body), refines at stride 1
    /// around the coarse minimum, then interpolates between samples with a parabola on d² —
    /// a flyby's closest moment rarely lands exactly on a sample.
    /// </summary>
    public static Pass? MostSevere(
        IReadOnlyList<TrajectorySample> samples,
        ICelestialEphemeris ephemeris,
        int maxEvaluationsPerBody = 400)
    {
        Pass? best = null;
        foreach (Pass pass in Passes(samples, ephemeris, maxEvaluationsPerBody))
        {
            if (best is null || pass.Severity < best.Value.Severity)
            {
                best = pass;
            }
        }

        return best;
    }

    /// <summary>The closest pass for every body along the path (M22: the arm-insertion button
    /// wants the tightest PLANET pass even when the fat sun is the most severe overall).</summary>
    public static IReadOnlyList<Pass> Passes(
        IReadOnlyList<TrajectorySample> samples,
        ICelestialEphemeris ephemeris,
        int maxEvaluationsPerBody = 400)
    {
        if (samples.Count < 2)
        {
            return [];
        }

        var passes = new List<Pass>();
        int stride = Math.Max(1, samples.Count / maxEvaluationsPerBody);
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            int coarse = 0;
            double coarseDist = double.MaxValue;
            for (int i = 0; i < samples.Count; i += stride)
            {
                double d = Distance(samples[i], body.Id, ephemeris);
                if (d < coarseDist)
                {
                    (coarseDist, coarse) = (d, i);
                }
            }

            int lo = Math.Max(0, coarse - stride);
            int hi = Math.Min(samples.Count - 1, coarse + stride);
            int min = coarse;
            double minDist = coarseDist;
            for (int i = lo; i <= hi; i++)
            {
                double d = Distance(samples[i], body.Id, ephemeris);
                if (d < minDist)
                {
                    (minDist, min) = (d, i);
                }
            }

            (double dist, double t, Vector2d pos) = RefineBetweenSamples(samples, min, body.Id, ephemeris, minDist);
            passes.Add(new Pass(body.Id, body.Name, body.BodyRadius, dist, t, pos));
        }

        return passes;
    }

    private static double Distance(TrajectorySample sample, string bodyId, ICelestialEphemeris ephemeris) =>
        (sample.Position - ephemeris.Position(bodyId, sample.SimTime)).Length;

    private static (double Dist, double SimTime, Vector2d Pos) RefineBetweenSamples(
        IReadOnlyList<TrajectorySample> samples, int min, string bodyId,
        ICelestialEphemeris ephemeris, double dMin)
    {
        if (min <= 0 || min >= samples.Count - 1)
        {
            return (dMin, samples[min].SimTime, samples[min].Position);
        }

        // Parabola vertex on d² over the three bracketing samples (in sample-index space).
        double d0 = Distance(samples[min - 1], bodyId, ephemeris);
        double d2 = Distance(samples[min + 1], bodyId, ephemeris);
        double a = d0 * d0, b = dMin * dMin, c = d2 * d2;
        double denom = a - 2 * b + c;
        if (denom <= 0)
        {
            return (dMin, samples[min].SimTime, samples[min].Position);
        }

        double offset = Math.Clamp(0.5 * (a - c) / denom, -1, 1);
        TrajectorySample from = offset < 0 ? samples[min - 1] : samples[min];
        TrajectorySample to = offset < 0 ? samples[min] : samples[min + 1];
        double f = offset < 0 ? offset + 1 : offset;
        double t = from.SimTime + (to.SimTime - from.SimTime) * f;
        Vector2d pos = from.Position + (to.Position - from.Position) * f;
        double d = (pos - ephemeris.Position(bodyId, t)).Length;
        return d < dMin ? (d, t, pos) : (dMin, samples[min].SimTime, samples[min].Position);
    }
}
