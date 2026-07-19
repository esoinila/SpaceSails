namespace SpaceSails.Core.Tests;

/// <summary>
/// Reever thermal motion (owner, cruise 2026-07-19: "the reevers could be more active, like little thermal
/// motion so they don't just stay still"). These pin the three laws that keep the shiver honest: it is
/// bounded (a shuffle, not a drift), it has zero mean over time (the anchor never creeps — the sentry pin
/// is law), and its peak speed stays BELOW <see cref="MotionTracker.StillSpeed"/> so option (a) holds — a
/// still contact could feed this velocity to the fan and still read quiet.
/// </summary>
public class ReeverIdleTests
{
    // ── Deterministic-from-seed (no Random, no clock) ──

    [Fact]
    public void JitterAt_IsDeterministic_PerSeedAndTime()
    {
        for (ulong seed = 1; seed < 40; seed++)
        {
            for (int k = 0; k < 20; k++)
            {
                double t = k * 0.37;
                Assert.Equal(ReeverIdle.JitterAt(seed, t), ReeverIdle.JitterAt(seed, t));
                Assert.Equal(ReeverIdle.FacingTwitchAt(seed, t), ReeverIdle.FacingTwitchAt(seed, t));
            }
        }
    }

    [Fact]
    public void DifferentSeeds_ShuffleDifferently()
    {
        // Two Old Ones don't breathe in lockstep — distinct seeds give distinct phase, so at a shared time
        // their offsets differ. (A cheap proxy: across a batch of seeds the offsets must not all coincide.)
        var seen = new HashSet<(double, double)>();
        for (ulong seed = 1; seed < 60; seed++)
        {
            (double dx, double dy) = ReeverIdle.JitterAt(seed, 2.5);
            seen.Add((Math.Round(dx, 6), Math.Round(dy, 6)));
        }
        Assert.True(seen.Count > 40, "distinct seeds must shuffle distinctly");
    }

    // ── Bounded amplitude: a shuffle around the anchor, never a drift ──

    [Fact]
    public void JitterAt_StaysWithinTheAmplitudeBound()
    {
        double radialCap = ReeverIdle.WanderAmplitudeDu * Math.Sqrt(2.0);
        for (ulong seed = 1; seed < 30; seed++)
        {
            for (int k = 0; k < 4000; k++)
            {
                double t = k * 0.05;
                (double dx, double dy) = ReeverIdle.JitterAt(seed, t);
                Assert.True(Math.Abs(dx) <= ReeverIdle.WanderAmplitudeDu + 1e-9, $"|dx|={Math.Abs(dx)}");
                Assert.True(Math.Abs(dy) <= ReeverIdle.WanderAmplitudeDu + 1e-9, $"|dy|={Math.Abs(dy)}");
                double radius = Math.Sqrt((dx * dx) + (dy * dy));
                Assert.True(radius <= radialCap + 1e-9, $"radius={radius} exceeds cap {radialCap}");
                // The owner's shuffle scale: the radial wander never exceeds ~0.1 du.
                Assert.True(radius <= 0.1 + 1e-9);
            }
        }
    }

    [Fact]
    public void FacingTwitchAt_StaysWithinTheBound()
    {
        for (ulong seed = 1; seed < 30; seed++)
        {
            for (int k = 0; k < 4000; k++)
            {
                double a = ReeverIdle.FacingTwitchAt(seed, k * 0.05);
                Assert.True(Math.Abs(a) <= ReeverIdle.FacingTwitchRad + 1e-9, $"|a|={Math.Abs(a)}");
            }
        }
    }

    // ── Zero mean over time: the anchor never creeps (the pin is law) ──

    [Fact]
    public void JitterAt_HasZeroMeanOverTime_SoTheAnchorNeverCreeps()
    {
        // Average the offset over a long, dense window (many periods of both components). A sinusoid sum has
        // an exact zero mean, so the sampled mean must sit within a whisker of the origin — the shiver
        // returns everything it borrows, and a pinned Reever's resting spot does not wander.
        for (ulong seed = 1; seed < 20; seed++)
        {
            double sx = 0, sy = 0;
            const int n = 200_000;
            for (int k = 0; k < n; k++)
            {
                double t = k * 0.01;
                (double dx, double dy) = ReeverIdle.JitterAt(seed, t);
                sx += dx;
                sy += dy;
            }
            Assert.True(Math.Abs(sx / n) < 1e-3, $"mean dx={sx / n}");
            Assert.True(Math.Abs(sy / n) < 1e-3, $"mean dy={sy / n}");
        }
    }

    // ── Below StillSpeed: option (a) holds — the fan stays quiet even if fed this velocity ──

    [Fact]
    public void JitterVelocity_StaysBelowStillSpeed()
    {
        // Finite-difference the offset to get the shuffle's instantaneous speed, and confirm the radial
        // speed never reaches the tracker's motion floor. This is the whole point of option (a): the
        // thermal shuffle is honestly still to a motion-gated tracker.
        const double h = 1e-4;
        double worst = 0;
        for (ulong seed = 1; seed < 40; seed++)
        {
            for (int k = 0; k < 8000; k++)
            {
                double t = k * 0.02;
                (double x0, double y0) = ReeverIdle.JitterAt(seed, t);
                (double x1, double y1) = ReeverIdle.JitterAt(seed, t + h);
                double vx = (x1 - x0) / h;
                double vy = (y1 - y0) / h;
                double speed = Math.Sqrt((vx * vx) + (vy * vy));
                worst = Math.Max(worst, speed);
            }
        }
        Assert.True(worst < MotionTracker.StillSpeed,
            $"peak shuffle speed {worst} du/s must stay below StillSpeed {MotionTracker.StillSpeed}");
        // And it genuinely MOVES — a below-floor speed, not a dead zero (the dread is real).
        Assert.True(worst > 0.02, "the shuffle must actually shuffle");
    }

    [Fact]
    public void IsMoving_ReadsTheShuffleAsStill()
    {
        // Straight against the tracker's own gate: sample the shuffle velocity across a run and confirm
        // MotionTracker never counts it as a mover — the pinned-sentry honesty, composed from pure pieces.
        const double h = 1e-4;
        for (ulong seed = 1; seed < 25; seed++)
        {
            for (int k = 0; k < 3000; k++)
            {
                double t = k * 0.03;
                (double x0, double y0) = ReeverIdle.JitterAt(seed, t);
                (double x1, double y1) = ReeverIdle.JitterAt(seed, t + h);
                Assert.False(MotionTracker.IsMoving((x1 - x0) / h, (y1 - y0) / h));
            }
        }
    }
}
