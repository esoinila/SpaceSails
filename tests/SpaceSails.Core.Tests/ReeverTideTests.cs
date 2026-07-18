namespace SpaceSails.Core.Tests;

/// <summary>
/// Lane-1 · The tide (owner, Saturday-evening playtest 2026-07-18): "even with bots there is only so
/// long time to stay there." These pin the ambient pressure the deep hands up — a pure, seed-locked
/// cadence of claw-outs "at random intervals" with "no fixed total number", and the home range that
/// keeps the tide holding the deep instead of chasing to the landing.
/// </summary>
public class ReeverTideTests
{
    // ── The cadence: positive, jittered, and deterministic-from-seed ──

    [Fact]
    public void NextGap_IsDeterministic_PerSeedAndIndex()
    {
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(ReeverTide.NextGap(4242, i), ReeverTide.NextGap(4242, i));
        }
    }

    [Fact]
    public void NextGap_IsAlwaysPositive_AndFlooredAtTheMinimum()
    {
        for (ulong seed = 0; seed < 100; seed++)
        {
            for (int i = 0; i < 40; i++)
            {
                double gap = ReeverTide.NextGap(seed, i);
                Assert.True(gap > 0, "a tide gap is a real wait");
                Assert.True(gap >= ReeverTide.MinGapSeconds, "the floor holds off a same-frame flood");
            }
        }
    }

    [Fact]
    public void NextGap_StaysWithinTheJitterBand_AroundTheMean()
    {
        // Every gap lands in Mean × [1 − Jitter, 1 + Jitter] (or the floor, whichever is larger).
        double lo = Math.Max(ReeverTide.MinGapSeconds, ReeverTide.MeanGapSeconds * (1.0 - ReeverTide.JitterFraction));
        double hi = ReeverTide.MeanGapSeconds * (1.0 + ReeverTide.JitterFraction);
        for (ulong seed = 0; seed < 60; seed++)
        {
            for (int i = 0; i < 40; i++)
            {
                double gap = ReeverTide.NextGap(seed, i);
                Assert.InRange(gap, lo, hi + 1e-9);
            }
        }
    }

    [Fact]
    public void NextGap_ActuallyJitters_NotAFixedDrumbeat()
    {
        // Across a run of indices on one seed the gaps must genuinely vary — "at random intervals",
        // never a metronome. The mean over a long run should sit near MeanGapSeconds.
        var seen = new HashSet<double>();
        double sum = 0;
        const int n = 600;
        for (int i = 0; i < n; i++)
        {
            double gap = ReeverTide.NextGap(31337, i);
            seen.Add(Math.Round(gap, 3));
            sum += gap;
        }

        Assert.True(seen.Count > 100, "the gaps must spread, not repeat a single value");
        Assert.InRange(sum / n, ReeverTide.MeanGapSeconds * 0.85, ReeverTide.MeanGapSeconds * 1.15);
    }

    [Fact]
    public void NextGap_HasNoFixedTotal_TheTideKeepsComing()
    {
        // The point of the tide: index arbitrarily far and it still answers a finite, positive gap —
        // there is no last Reever, no built-in cap on how many the deep hands up.
        foreach (int index in new[] { 0, 1, 10, 100, 1000, 100_000, 5_000_000 })
        {
            double gap = ReeverTide.NextGap(7, index);
            Assert.True(gap > 0 && double.IsFinite(gap));
        }
    }

    // ── The spawn edge: deterministic, spread across the deep rim, independent of the gap stream ──

    [Fact]
    public void SpawnX_IsDeterministic_AndInsideTheEdge()
    {
        for (int i = 0; i < 60; i++)
        {
            double x = ReeverTide.SpawnX(99, i, -40, 30);
            Assert.Equal(x, ReeverTide.SpawnX(99, i, -40, 30));
            Assert.InRange(x, -40, 30);
        }
    }

    [Fact]
    public void SpawnX_SpreadsAcrossTheEdge_NotSingleFile()
    {
        // A tide over many indices should fan out across the whole rim, not stack on one column.
        double min = double.MaxValue, max = double.MinValue;
        for (int i = 0; i < 200; i++)
        {
            double x = ReeverTide.SpawnX(555, i, -40, 30);
            min = Math.Min(min, x);
            max = Math.Max(max, x);
        }

        Assert.True(max - min > 0.6 * (30 - (-40)), "the tide must spread across most of the deep edge");
    }

    [Fact]
    public void SpawnX_ToleratesSwappedBounds()
    {
        // Bounds handed in either order still yield a point inside the range (no NaN, no escape).
        double x = ReeverTide.SpawnX(1, 3, rightX: -40, leftX: 30);
        Assert.InRange(x, -40, 30);
    }

    [Fact]
    public void GapAndPositionStreams_DoNotCorrelate()
    {
        // Salted apart on the shared seed: the gap and x streams are independent (a shared draw would
        // lock the wait to the position). Cheap proxy — they should not move in lockstep across indices.
        int locked = 0;
        for (int i = 1; i < 200; i++)
        {
            bool gapUp = ReeverTide.NextGap(2024, i) > ReeverTide.NextGap(2024, i - 1);
            bool xUp = ReeverTide.SpawnX(2024, i, -40, 30) > ReeverTide.SpawnX(2024, i - 1, -40, 30);
            if (gapUp == xUp)
            {
                locked++;
            }
        }

        Assert.InRange(locked, 60, 140); // ~half by chance; neither near 0 nor near 199 (perfect (anti)lock)
    }

    // ── The home range: the tide holds the deep and stops venturing toward the landing ──

    [Fact]
    public void HomeRangeY_SitsInTheDeep_WellSouthOfTheDoor()
    {
        // Field geometry as MoonSurface uses it: top (tube mouth / crew-only door) = -20, deep edge = -84.
        const double top = -20, bottom = -84;
        double home = ReeverTide.HomeRangeY(top, bottom);

        // Strictly inside the field…
        Assert.InRange(home, bottom, top);
        // …but well SOUTH of the door (more negative), so the landing band is never in the tide's reach.
        Assert.True(home < top - 20, "the tide must hold well short of the landing");
        // …and north of the spawn edge, so a clawed-out Reever has field to walk before it turns back.
        Assert.True(home > bottom, "the home range must leave room north of the deep edge");
    }

    [Fact]
    public void HomeRange_LeashesATideReever_ItTurnsBackAndNeverReachesTheLanding()
    {
        // Feed the home-range y to ReeverChase as the barrier (exactly how the client leashes a tide
        // Reever) and chase a captain standing up at the landing: the Reever may pile at the line but can
        // never cross north of it — the "will stop venturing too far" law, composed from pure pieces.
        const double top = -20, bottom = -84;
        double home = ReeverTide.HomeRangeY(top, bottom);

        double rx = 0, ry = bottom + 2; // clawed out at the deep edge
        for (int i = 0; i < 2000; i++)
        {
            (rx, ry) = ReeverChase.Step(rx, ry, avatarX: -6, avatarY: top - 2, stepDistance: 5.6 * 0.1, barrierY: home);
            Assert.True(ry <= home + 1e-9, $"a tide Reever reached y={ry}, north of its home range at {home}");
        }
    }
}
