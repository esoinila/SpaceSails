namespace SpaceSails.Core.Tests;

/// <summary>
/// HULL-SHUDDER · the ambient-dread mood-setter (owner, at sea in rough weather 2026-07-20: "The ship
/// sometimes shakes… as if joint, in unison, people then decide.. probably just a wave"). These pin the
/// pure spine: a seed-locked, jittered, rare-ish onset cadence; a context-flavored house-voice line pool
/// (non-blank, unique, the unison-decide beat) selected deterministically; the bounded deck-shake curve
/// (deterministic, gently bounded, decays to exactly zero — never a sustained rumble); the synchronized
/// unison pause window; and the bounded story-site chill escalation.
/// </summary>
public class HullShudderTests
{
    private const ulong Seed = 0x5EA5_1DE0_0424UL;

    // ── The onset cadence: positive, jittered, floored, and deterministic-from-seed. ──

    [Fact]
    public void NextGap_IsDeterministic_PerSeedAndIndex()
    {
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(HullShudder.NextGap(Seed, i), HullShudder.NextGap(Seed, i));
        }
    }

    [Fact]
    public void NextGap_StaysWithinTheJitterBand_AndAboveTheFloor()
    {
        double lo = HullShudder.MeanGapSeconds * (1.0 - HullShudder.GapJitterFraction);
        double hi = HullShudder.MeanGapSeconds * (1.0 + HullShudder.GapJitterFraction);
        for (ulong s = 0; s < 60; s++)
        {
            for (int i = 0; i < 40; i++)
            {
                double gap = HullShudder.NextGap(s, i);
                Assert.True(gap >= HullShudder.MinGapSeconds, $"gap {gap} breached the floor");
                Assert.True(gap <= hi + 1e-9, $"gap {gap} over band {hi}");
                // The floor only ever raises a gap, so the lower edge holds too.
                Assert.True(gap >= System.Math.Min(lo, HullShudder.MinGapSeconds) - 1e-9);
            }
        }
    }

    [Fact]
    public void NextGap_ActuallyJitters_NotAFixedPulse()
    {
        var seen = new HashSet<double>();
        double sum = 0;
        const int n = 500;
        for (int i = 0; i < n; i++)
        {
            double gap = HullShudder.NextGap(31337, i);
            seen.Add(System.Math.Round(gap, 3));
            sum += gap;
        }
        Assert.True(seen.Count > 100, "the gaps must spread, not repeat one value");
        Assert.InRange(sum / n, HullShudder.MeanGapSeconds * 0.85, HullShudder.MeanGapSeconds * 1.15);
    }

    [Fact]
    public void Cadence_IsRareIsh_MeanGapIsGenerous()
    {
        // A mood-setter, not a drumbeat: the mean gap is well over half a minute.
        Assert.True(HullShudder.MeanGapSeconds >= 60.0);
        Assert.True(HullShudder.MinGapSeconds >= 20.0, "two shudders must never stutter back-to-back");
    }

    // ── The line pools: non-blank, unique, and context-flavored. ──

    [Theory]
    [InlineData(HullShudder.Setting.Haven)]
    [InlineData(HullShudder.Setting.Ship)]
    [InlineData(HullShudder.Setting.DeepSite)]
    public void LinePool_IsNonBlank_AndUnique(HullShudder.Setting setting)
    {
        IReadOnlyList<string> pool = HullShudder.LinesFor(setting);
        Assert.NotEmpty(pool);
        Assert.All(pool, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.Equal(pool.Count, pool.Distinct().Count()); // no duplicates
    }

    [Fact]
    public void ChillPool_IsNonBlank_AndUnique()
    {
        IReadOnlyList<string> pool = HullShudder.ChillLines_;
        Assert.NotEmpty(pool);
        Assert.All(pool, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.Equal(pool.Count, pool.Distinct().Count());
    }

    [Fact]
    public void EveryLine_CarriesTheUnisonBeat()
    {
        // The owner's shape: heads coming up "as one" / "in unison" — the shared, held beat.
        var all = HullShudder.LinesFor(HullShudder.Setting.Haven)
            .Concat(HullShudder.LinesFor(HullShudder.Setting.Ship))
            .Concat(HullShudder.LinesFor(HullShudder.Setting.DeepSite))
            .Concat(HullShudder.ChillLines_);
        foreach (string line in all)
        {
            bool unison = line.Contains("as one", System.StringComparison.OrdinalIgnoreCase)
                       || line.Contains("in unison", System.StringComparison.OrdinalIgnoreCase)
                       || line.Contains("all around", System.StringComparison.OrdinalIgnoreCase)
                       || line.Contains("at once", System.StringComparison.OrdinalIgnoreCase);
            Assert.True(unison, $"a shudder line must carry the unison beat: {line}");
        }
    }

    [Fact]
    public void HavenAndDeepSite_SpeakDistinctVoices()
    {
        // Haven blames the clamps / the station settling; a deep site can't name what settles this far
        // down — the pools must not overlap, so the flavor actually changes with the context.
        var haven = HullShudder.LinesFor(HullShudder.Setting.Haven);
        var deep = HullShudder.LinesFor(HullShudder.Setting.DeepSite);
        Assert.Empty(haven.Intersect(deep));
        Assert.Contains(haven, l => l.Contains("clamps", System.StringComparison.OrdinalIgnoreCase)
                                 || l.Contains("station", System.StringComparison.OrdinalIgnoreCase)
                                 || l.Contains("moorings", System.StringComparison.OrdinalIgnoreCase));
    }

    // ── Line selection: deterministic, in-pool, and it rotates rather than repeating. ──

    [Fact]
    public void Line_IsDeterministic_AndFromTheSettingsPool()
    {
        foreach (HullShudder.Setting setting in System.Enum.GetValues<HullShudder.Setting>())
        {
            IReadOnlyList<string> pool = HullShudder.LinesFor(setting);
            for (int i = 0; i < 40; i++)
            {
                string line = HullShudder.Line(setting, Seed, i);
                Assert.Equal(line, HullShudder.Line(setting, Seed, i));
                Assert.Contains(line, pool);
            }
        }
    }

    [Fact]
    public void Line_RotatesThePool_OverARun()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < 40; i++)
        {
            seen.Add(HullShudder.Line(HullShudder.Setting.DeepSite, Seed, i));
        }
        // A run of shudders should exercise more than one line of the pool — not a stuck single value.
        Assert.True(seen.Count >= 2, "the line pool must rotate across a run of shudders");
    }

    [Fact]
    public void ChillLine_IsDeterministic_AndFromTheChillPool()
    {
        for (int i = 0; i < 40; i++)
        {
            string line = HullShudder.ChillLine(Seed, i);
            Assert.Equal(line, HullShudder.ChillLine(Seed, i));
            Assert.Contains(line, HullShudder.ChillLines_);
        }
    }

    // ── Context → setting selection. ──

    [Fact]
    public void SettingFor_PicksTheColdestContext()
    {
        // A deep site (a landing) wins even at a docked haven — a landing is a landing.
        Assert.Equal(HullShudder.Setting.DeepSite, HullShudder.SettingFor(deepSite: true, haven: true));
        Assert.Equal(HullShudder.Setting.DeepSite, HullShudder.SettingFor(deepSite: true, haven: false));
        // Otherwise a docked haven speaks the reassuring voice…
        Assert.Equal(HullShudder.Setting.Haven, HullShudder.SettingFor(deepSite: false, haven: true));
        // …and failing both, it's the ship's own deck.
        Assert.Equal(HullShudder.Setting.Ship, HullShudder.SettingFor(deepSite: false, haven: false));
    }

    // ── The deck-shake curve: bounded, gentle, deterministic, and decays to EXACTLY zero. ──

    [Fact]
    public void ShakeEnvelope_IsOneAtOnset_ZeroAtTheEnd_AndInRangeThroughout()
    {
        Assert.Equal(1.0, HullShudder.ShakeEnvelope(0.0), precision: 9);
        Assert.Equal(0.0, HullShudder.ShakeEnvelope(HullShudder.ShakeDurationSeconds), precision: 9);
        Assert.Equal(0.0, HullShudder.ShakeEnvelope(HullShudder.ShakeDurationSeconds + 1.0), precision: 9);
        Assert.Equal(0.0, HullShudder.ShakeEnvelope(-0.5), precision: 9);
        for (double t = 0; t <= HullShudder.ShakeDurationSeconds; t += 0.01)
        {
            Assert.InRange(HullShudder.ShakeEnvelope(t), 0.0, 1.0 + 1e-9);
        }
    }

    [Fact]
    public void ShakeEnvelope_Decays_SharpAtStartFadedByEnd()
    {
        // The tremor is sharpest at the first instant and settled by the end (never a sustained rumble).
        double early = HullShudder.ShakeEnvelope(0.05);
        double late = HullShudder.ShakeEnvelope(HullShudder.ShakeDurationSeconds * 0.8);
        Assert.True(late < early, "the shake must fade, not sustain");
        Assert.True(late < 0.15, "by the tail the shake is nearly gone");
    }

    [Fact]
    public void ShakeOffset_IsDeterministic_BoundedAndGentle()
    {
        for (ulong s = 0; s < 40; s++)
        {
            for (double t = 0; t <= HullShudder.ShakeDurationSeconds; t += 0.017)
            {
                (double dx, double dy) = HullShudder.ShakeOffset(s, t);
                Assert.Equal((dx, dy), HullShudder.ShakeOffset(s, t)); // deterministic
                // Bounded to the unit amplitude at every instant (the client scales to a few px) — subtle,
                // never nauseating (owner's constraint): |offset| ≤ amplitude × envelope ≤ amplitude.
                double bound = HullShudder.ShakeAmplitude + 1e-9;
                Assert.InRange(dx, -bound, bound);
                Assert.InRange(dy, -bound, bound);
            }
        }
    }

    [Fact]
    public void ShakeOffset_IsExactlyZero_OutsideTheWindow()
    {
        for (ulong s = 0; s < 20; s++)
        {
            Assert.Equal((0.0, 0.0), HullShudder.ShakeOffset(s, HullShudder.ShakeDurationSeconds));
            Assert.Equal((0.0, 0.0), HullShudder.ShakeOffset(s, HullShudder.ShakeDurationSeconds + 0.5));
            Assert.Equal((0.0, 0.0), HullShudder.ShakeOffset(s, -0.1));
        }
    }

    [Fact]
    public void ShakeAxes_DoNotSlideDiagonally()
    {
        // Independent per-axis phases: the shake is a shiver, not a straight diagonal slide — dx and dy
        // must not move in perfect lockstep across the window.
        int locked = 0, n = 0;
        double prevDx = 0, prevDy = 0;
        bool first = true;
        for (double t = 0.01; t < HullShudder.ShakeDurationSeconds * 0.6; t += 0.01)
        {
            (double dx, double dy) = HullShudder.ShakeOffset(12345, t);
            if (!first)
            {
                if ((dx > prevDx) == (dy > prevDy)) { locked++; }
                n++;
            }
            (prevDx, prevDy, first) = (dx, dy, false);
        }
        Assert.InRange(locked, 1, n - 1); // neither perfectly locked nor perfectly anti-locked
    }

    // ── The unison pause window. ──

    [Fact]
    public void Pausing_HoldsTheBreath_ThenReleases()
    {
        Assert.True(HullShudder.Pausing(0.0));
        Assert.True(HullShudder.Pausing(HullShudder.PauseDurationSeconds - 0.01));
        Assert.False(HullShudder.Pausing(HullShudder.PauseDurationSeconds));
        Assert.False(HullShudder.Pausing(HullShudder.PauseDurationSeconds + 1.0));
        Assert.False(HullShudder.Pausing(-0.01));
    }

    [Fact]
    public void Pause_OutlastsTheShake_TheStillnessIsTheFeature()
    {
        // The held breath is the point — it lasts at least as long as the tremor that announced it.
        Assert.True(HullShudder.PauseDurationSeconds >= HullShudder.ShakeDurationSeconds);
    }

    // ── The bounded chill escalation. ──

    [Fact]
    public void CarriesChill_IsDeterministic_AndBounded()
    {
        // Deterministic per (seed, index), and mostly it is STILL nothing — the chill is the minority.
        int chills = 0;
        const int n = 400;
        for (int i = 0; i < n; i++)
        {
            bool c = HullShudder.CarriesChill(Seed, i);
            Assert.Equal(c, HullShudder.CarriesChill(Seed, i));
            if (c) { chills++; }
        }
        double rate = chills / (double)n;
        Assert.InRange(rate, 0.2, 0.6); // near ChillChance (0.4), and never "always"
        Assert.True(HullShudder.ChillChance < 0.5, "mostly a deep-site shudder is still nothing");
    }

    [Fact]
    public void ChillNerveTick_IsSmall_APrickleNotAShock()
    {
        // Far smaller than a Reever's touch or the monolith — the dread is the pause, not damage.
        Assert.True(HullShudder.ChillNerveTick > 0);
        Assert.True(HullShudder.ChillNerveTick < NerveModel.TouchShock);
        Assert.True(HullShudder.ChillNerveTick < NerveModel.MonolithSightShock);
    }

    // ══ THE UNEXPLAINED SIGNAL · the shudder's colder sibling. ══

    [Fact]
    public void SignalGap_IsDeterministic_Jittered_Floored_AndRarerThanTheShudder()
    {
        // Rarer than a shudder (a longer mean gap and a higher floor) — the once-a-long-while off-deck note.
        Assert.True(HullShudder.SignalMeanGapSeconds > HullShudder.MeanGapSeconds);
        Assert.True(HullShudder.SignalMinGapSeconds > HullShudder.MinGapSeconds);

        double hi = HullShudder.SignalMeanGapSeconds * (1.0 + HullShudder.SignalGapJitterFraction);
        for (ulong s = 0; s < 40; s++)
        {
            for (int i = 0; i < 40; i++)
            {
                double gap = HullShudder.SignalNextGap(s, i);
                Assert.Equal(gap, HullShudder.SignalNextGap(s, i));
                Assert.True(gap >= HullShudder.SignalMinGapSeconds, $"signal gap {gap} under floor");
                Assert.True(gap <= hi + 1e-9, $"signal gap {gap} over band {hi}");
            }
        }
    }

    [Fact]
    public void SignalGap_DoesNotLockToTheShudderGap()
    {
        // Salted apart on the shared seed: the two ambient rhythms must not move in lockstep across indices.
        int locked = 0;
        for (int i = 1; i < 200; i++)
        {
            bool shudderUp = HullShudder.NextGap(2024, i) > HullShudder.NextGap(2024, i - 1);
            bool signalUp = HullShudder.SignalNextGap(2024, i) > HullShudder.SignalNextGap(2024, i - 1);
            if (shudderUp == signalUp) { locked++; }
        }
        Assert.InRange(locked, 60, 140); // ~half by chance; neither perfectly (anti)locked
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SignalPool_IsNonBlank_Unique_AndCarriesTheGlanceBeat(bool cold)
    {
        IReadOnlyList<string> pool = HullShudder.SignalLinesFor(cold);
        Assert.NotEmpty(pool);
        Assert.All(pool, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.Equal(pool.Count, pool.Distinct().Count());
        // The owner's shape: the STAFF/crew catch each other's eye — a shared glance, and no one explains it.
        foreach (string line in pool)
        {
            bool glance = line.Contains("eye", System.StringComparison.OrdinalIgnoreCase)
                       || line.Contains("glance", System.StringComparison.OrdinalIgnoreCase)
                       || line.Contains("look", System.StringComparison.OrdinalIgnoreCase)
                       || line.Contains("eyes", System.StringComparison.OrdinalIgnoreCase);
            Assert.True(glance, $"a signal line must carry the crew-glance beat: {line}");
            bool crew = line.Contains("barkeep", System.StringComparison.OrdinalIgnoreCase)
                     || line.Contains("staff", System.StringComparison.OrdinalIgnoreCase)
                     || line.Contains("crew", System.StringComparison.OrdinalIgnoreCase)
                     || line.Contains("dock-hand", System.StringComparison.OrdinalIgnoreCase);
            Assert.True(crew, $"a signal line reacts through the STAFF, not the patrons: {line}");
        }
    }

    [Fact]
    public void WarmAndColdSignalPools_AreDistinct()
    {
        var warm = HullShudder.SignalLinesFor(cold: false);
        var cold = HullShudder.SignalLinesFor(cold: true);
        Assert.Empty(warm.Intersect(cold));
    }

    [Fact]
    public void SignalLine_IsDeterministic_AndFromTheRightPool()
    {
        foreach (bool cold in new[] { false, true })
        {
            IReadOnlyList<string> pool = HullShudder.SignalLinesFor(cold);
            for (int i = 0; i < 40; i++)
            {
                string line = HullShudder.SignalLine(cold, Seed, i);
                Assert.Equal(line, HullShudder.SignalLine(cold, Seed, i));
                Assert.Contains(line, pool);
            }
        }
    }

    [Fact]
    public void ColdGlance_LingersLongerThanTheOrdinaryGlance()
    {
        // The story-deep escalation: the eyes lock a beat too long.
        Assert.True(HullShudder.ColdGlanceDurationSeconds > HullShudder.GlanceDurationSeconds);

        double justAfterWarm = HullShudder.GlanceDurationSeconds + 0.01;
        // Past the ordinary glance the warm look has let go — but a cold one is still held.
        Assert.False(HullShudder.Glancing(justAfterWarm, cold: false));
        Assert.True(HullShudder.Glancing(justAfterWarm, cold: true));

        Assert.True(HullShudder.Glancing(0.0, cold: false));
        Assert.False(HullShudder.Glancing(HullShudder.ColdGlanceDurationSeconds, cold: true));
        Assert.False(HullShudder.Glancing(-0.01, cold: false));
    }

    [Fact]
    public void Signal_IsColderThanTheShudder_NoShakeInvolved()
    {
        // The signal is the colder sibling: it reacts through the staff and never shakes the deck — there is
        // deliberately no shake curve on this path (only the shudder throws the frame). The glance is the
        // whole visual, and it is synchronized (a single window), same as the shudder's pause.
        Assert.True(HullShudder.GlanceDurationSeconds > 0);
        // A signal glance is on the order of the shudder's held breath (a beat), not a lingering minute.
        Assert.InRange(HullShudder.GlanceDurationSeconds, 0.5, 3.0);
    }

    // ══ THE CAUTION ANNOUNCEMENT · the rough-passage PA (third sibling). ══

    [Fact]
    public void RoughPatch_IsARunOfShudders_NotASingleBeat()
    {
        // Two shudders in a row make a rough patch the PA answers; one is just an ambient beat.
        Assert.True(HullShudder.RoughPatchShudderRun >= 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CautionPool_IsNonBlank_Unique_AndCarriesTheUndercut(bool cold)
    {
        IReadOnlyList<string> pool = HullShudder.CautionLinesFor(cold);
        Assert.NotEmpty(pool);
        Assert.All(pool, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.Equal(pool.Count, pool.Distinct().Count());
        foreach (string line in pool)
        {
            // The all-hands PA voice: move carefully / deliberately, one hand for the ship.
            bool caution = line.Contains("carefully", System.StringComparison.OrdinalIgnoreCase)
                        || line.Contains("deliberately", System.StringComparison.OrdinalIgnoreCase)
                        || line.Contains("footing", System.StringComparison.OrdinalIgnoreCase)
                        || line.Contains("slow", System.StringComparison.OrdinalIgnoreCase)
                        || line.Contains("don't hurry", System.StringComparison.OrdinalIgnoreCase);
            Assert.True(caution, $"a caution PA must ask for care: {line}");
            // The parenthetical undercut IS the mood — every line carries one.
            Assert.True(line.Contains('(') && line.Contains(')'), $"a caution PA must carry the undercut: {line}");
        }
    }

    [Fact]
    public void WarmAndColdCautionPools_AreDistinct_AndColdNamesTheDread()
    {
        var warm = HullShudder.CautionLinesFor(cold: false);
        var cold = HullShudder.CautionLinesFor(cold: true);
        Assert.Empty(warm.Intersect(cold));
        // The owner's cold example: "keep one hand for the ship, and the other for yourself".
        Assert.Contains(cold, l => l.Contains("the other for yourself", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CautionLine_IsDeterministic_AndFromTheRightPool()
    {
        foreach (bool cold in new[] { false, true })
        {
            IReadOnlyList<string> pool = HullShudder.CautionLinesFor(cold);
            for (int i = 0; i < 40; i++)
            {
                string line = HullShudder.CautionLine(cold, Seed, i);
                Assert.Equal(line, HullShudder.CautionLine(cold, Seed, i));
                Assert.Contains(line, pool);
            }
        }
    }

    [Fact]
    public void CautionNerveNudge_IsTiny_SteadyOrFray()
    {
        // A hair of nuance, either way — smaller than a shudder's chill, never a real hit.
        Assert.True(HullShudder.CautionSteadyTick > 0 && HullShudder.CautionSteadyTick <= HullShudder.ChillNerveTick);
        Assert.True(HullShudder.CautionColdTick > 0 && HullShudder.CautionColdTick <= HullShudder.ChillNerveTick);
    }
}
