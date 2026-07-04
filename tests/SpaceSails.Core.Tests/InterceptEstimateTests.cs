namespace SpaceSails.Core.Tests;

/// <summary>M27: the war room's intercept clock — closest approach between two sampled paths
/// and the "initiative roll" moment the range first drops inside a threshold.</summary>
public class InterceptEstimateTests
{
    private static List<TrajectorySample> Line(Vector2d start, Vector2d velocity, double t0, double t1, double dt)
    {
        var samples = new List<TrajectorySample>();
        for (double t = t0; t <= t1 + 1e-9; t += dt)
        {
            samples.Add(new TrajectorySample(t, start + velocity * (t - t0)));
        }

        return samples;
    }

    [Fact]
    public void Against_CrossingPaths_FindsTheCrossingMomentAndThreshold()
    {
        // We fly +X, they fly +Y; both reach the origin at t = 100 — a dead crossing.
        var ours = Line(new Vector2d(-1e6, 0), new Vector2d(1e4, 0), 0, 200, 1);
        var theirs = Line(new Vector2d(0, -1e6), new Vector2d(0, 1e4), 0, 200, 1);

        InterceptEstimate.Result? result = InterceptEstimate.Against(ours, theirs, thresholdMeters: 5e5);

        Assert.NotNull(result);
        Assert.True(result.Value.MinDistance < 2e4, $"Expected a near-zero pass, got {result.Value.MinDistance}.");
        Assert.Equal(100, result.Value.MinSimTime, tolerance: 2);
        // Separation is sqrt(2)·|t-100|·1e4 — inside 5e5 m from t ≈ 100 − 35.4 s.
        Assert.NotNull(result.Value.FirstWithinThresholdSimTime);
        Assert.Equal(100 - 5e5 / Math.Sqrt(2) / 1e4, result.Value.FirstWithinThresholdSimTime!.Value, tolerance: 2);
    }

    [Fact]
    public void Against_ParallelDistantPaths_NoThresholdCrossing()
    {
        var ours = Line(new Vector2d(0, 0), new Vector2d(1e4, 0), 0, 100, 1);
        var theirs = Line(new Vector2d(0, 1e9), new Vector2d(1e4, 0), 0, 100, 1);

        InterceptEstimate.Result? result = InterceptEstimate.Against(ours, theirs, thresholdMeters: 5e8);

        Assert.NotNull(result);
        Assert.Equal(1e9, result.Value.MinDistance, tolerance: 1);
        Assert.Null(result.Value.FirstWithinThresholdSimTime);
    }

    [Fact]
    public void Against_NonOverlappingTimeWindows_ReturnsNull()
    {
        var ours = Line(Vector2d.Zero, new Vector2d(1e4, 0), 0, 50, 1);
        var theirs = Line(Vector2d.Zero, new Vector2d(0, 1e4), 100, 150, 1);

        Assert.Null(InterceptEstimate.Against(ours, theirs, 1e6));
    }
}
