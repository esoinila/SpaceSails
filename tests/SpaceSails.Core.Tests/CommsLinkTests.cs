namespace SpaceSails.Core.Tests;

/// <summary>
/// COMMS-LOSS · the mothership downlink (owner, cruise 2026-07-19: "loss of comms.. that also is great
/// horror element"). These pin the honest-to-the-trope laws: the onset/duration/deepen cadence is
/// deterministic and replayable (never <see cref="System.Random"/>), the live phase ramps
/// Nominal → Degraded → Blackout → Degraded → Nominal in strict order, the TRUE state a HUD reports keeps
/// advancing under a blackout (the display is withheld, not the truth), and recovery speaks the true
/// current state — including a hold that went bad while the captain was dark.
/// </summary>
public class CommsLinkTests
{
    private const ulong Seed = 0xC0FFEE1234UL;

    // ── Determinism: the whole schedule replays from the seed. ───────────────────────────────────────

    [Fact]
    public void Onset_Duration_Deepen_AreDeterministic_ForSameSeedAndIndex()
    {
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(CommsLink.NextGap(Seed, i), CommsLink.NextGap(Seed, i));
            Assert.Equal(CommsLink.EpisodeDuration(Seed, i), CommsLink.EpisodeDuration(Seed, i));
            Assert.Equal(CommsLink.EpisodeDeepens(Seed, i), CommsLink.EpisodeDeepens(Seed, i));
        }
    }

    [Fact]
    public void Gap_StaysWithinJitterBand_AndAboveTheFloor()
    {
        double lo = CommsLink.MeanGapSeconds * (1.0 - CommsLink.GapJitterFraction);
        double hi = CommsLink.MeanGapSeconds * (1.0 + CommsLink.GapJitterFraction);
        for (int i = 0; i < 200; i++)
        {
            double g = CommsLink.NextGap(Seed, i);
            Assert.True(g >= CommsLink.MinGapSeconds, $"gap {g} under floor");
            // The floor only ever raises a gap, so the band's upper edge always holds.
            Assert.True(g <= hi + 1e-9, $"gap {g} over band {hi}");
            Assert.True(g >= Math.Min(lo, CommsLink.MinGapSeconds) - 1e-9);
        }
    }

    [Fact]
    public void Duration_StaysWithinJitterBand_AndAboveTheFloor()
    {
        double hi = CommsLink.MeanEpisodeSeconds * (1.0 + CommsLink.EpisodeJitterFraction);
        for (int i = 0; i < 200; i++)
        {
            double d = CommsLink.EpisodeDuration(Seed, i);
            Assert.True(d >= CommsLink.MinEpisodeSeconds, $"dur {d} under floor");
            Assert.True(d <= hi + 1e-9, $"dur {d} over band {hi}");
        }
    }

    [Fact]
    public void OnsetBias_BringsTheDropSooner_NeverBelowTheFloor()
    {
        // Deep in the site (bias 2×) the gap is at most half the baseline — drops come sooner.
        for (int i = 0; i < 50; i++)
        {
            double baseGap = CommsLink.NextGap(Seed, i, onsetBias: 1.0);
            double deepGap = CommsLink.NextGap(Seed, i, onsetBias: 2.0);
            Assert.True(deepGap <= baseGap + 1e-9, $"deep {deepGap} not <= base {baseGap}");
            Assert.True(deepGap >= CommsLink.MinGapSeconds, "bias must never breach the floor");
        }
    }

    // ── The live phase arc. ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PhaseAt_IsNominal_OutsideTheEpisode()
    {
        Assert.Equal(CommsLink.Phase.Nominal, CommsLink.PhaseAt(100, 20, deepens: true, now: 99.9));
        Assert.Equal(CommsLink.Phase.Nominal, CommsLink.PhaseAt(100, 20, deepens: true, now: 120.0)); // end is exclusive
        Assert.Equal(CommsLink.Phase.Nominal, CommsLink.PhaseAt(100, 20, deepens: true, now: 200.0));
    }

    [Fact]
    public void DeepeningEpisode_Ramps_Degraded_Blackout_Degraded_InOrder()
    {
        const double start = 100, dur = 20;
        double edge = CommsLink.DegradedEdgeFraction; // 0.3 → first & last 30%

        // Just after onset: the feed is breaking up (degraded edge).
        Assert.Equal(CommsLink.Phase.Degraded, CommsLink.PhaseAt(start, dur, true, start + 0.1));
        Assert.Equal(CommsLink.Phase.Degraded, CommsLink.PhaseAt(start, dur, true, start + (edge * dur) - 0.1));
        // The middle span is dead air.
        Assert.Equal(CommsLink.Phase.Blackout, CommsLink.PhaseAt(start, dur, true, start + (dur / 2.0)));
        Assert.Equal(CommsLink.Phase.Blackout, CommsLink.PhaseAt(start, dur, true, start + (edge * dur) + 0.1));
        // The tail claws back to degraded before recovery.
        Assert.Equal(CommsLink.Phase.Degraded, CommsLink.PhaseAt(start, dur, true, start + dur - 0.1));

        // The order is strict as the clock walks the whole episode: no Blackout before the first Degraded,
        // none after the last, and Nominal only at the ends.
        var seen = new List<CommsLink.Phase>();
        for (double t = start - 1; t <= start + dur + 1; t += 0.25)
        {
            seen.Add(CommsLink.PhaseAt(start, dur, true, t));
        }
        int firstBlackout = seen.IndexOf(CommsLink.Phase.Blackout);
        int lastBlackout = seen.LastIndexOf(CommsLink.Phase.Blackout);
        Assert.True(firstBlackout > 0 && seen[firstBlackout - 1] == CommsLink.Phase.Degraded);
        Assert.True(lastBlackout < seen.Count - 1 && seen[lastBlackout + 1] == CommsLink.Phase.Degraded);
    }

    [Fact]
    public void ShallowEpisode_IsDegradedThroughout_NeverBlackout()
    {
        const double start = 50, dur = 12;
        for (double t = start; t < start + dur; t += 0.5)
        {
            Assert.Equal(CommsLink.Phase.Degraded, CommsLink.PhaseAt(start, dur, deepens: false, now: t));
        }
    }

    // ── THE HONESTY INVARIANT: the true state advances under a blackout; the DISPLAY is what's frozen. ──

    [Fact]
    public void TrueState_KeepsAdvancing_UnderBlackout_WhileDisplayFreezes()
    {
        // Model the away HUD: a TRUE orbit-hold value that keeps eroding every tick, and the DISPLAY, which
        // snapshots the truth while nominal and freezes it while the link is down. The invariant the owner
        // demands: comms-loss withholds the DISPLAY only — the truth never stops moving underneath.
        const double start = 100, dur = 20;

        double TrueValueAt(double now) => 1000.0 - now; // the tank draining, second by second — always live

        double displayed = double.NaN;
        double lastKnown = double.NaN;
        double lastContact = 0;

        double blackoutTruth = double.NaN, blackoutDisplay = double.NaN, blackoutContact = double.NaN;
        for (double now = 90; now <= 130; now += 1.0)
        {
            CommsLink.Phase phase = CommsLink.PhaseAt(start, dur, deepens: true, now);
            double truth = TrueValueAt(now); // the real state ALWAYS advances, comms or not
            if (phase == CommsLink.Phase.Nominal)
            {
                lastKnown = truth;   // snapshot the honest current value
                lastContact = now;
                displayed = truth;   // live
            }
            else
            {
                displayed = lastKnown; // FROZEN at last-known — the withheld confirmation
            }

            if (phase == CommsLink.Phase.Blackout && double.IsNaN(blackoutTruth))
            {
                blackoutTruth = truth;
                blackoutDisplay = displayed;
                blackoutContact = lastContact;
            }
        }

        // Deep in the blackout the display was stale (frozen) while the truth had moved on.
        Assert.False(double.IsNaN(blackoutTruth));
        Assert.NotEqual(blackoutTruth, blackoutDisplay, precision: 6);
        Assert.True(blackoutTruth < blackoutDisplay, "the truth kept draining under the freeze");
        // The frozen value was an honestly-recent reading (from the last contact), not invented.
        Assert.Equal(TrueValueAt(blackoutContact), blackoutDisplay, precision: 6);
        // After recovery the display equals the true current value again — the catch-up.
        Assert.Equal(TrueValueAt(130), displayed, precision: 6);
    }

    [Fact]
    public void StaleBanner_NamesTheContactAge_OnlyWhileDown()
    {
        Assert.Equal(string.Empty, CommsLink.StaleBanner(CommsLink.Phase.Nominal, 42));
        Assert.Contains("SIGNAL LOST", CommsLink.StaleBanner(CommsLink.Phase.Blackout, 20));
        Assert.Contains("BREAKING UP", CommsLink.StaleBanner(CommsLink.Phase.Degraded, 20));
        Assert.Contains("20s", CommsLink.StaleBanner(CommsLink.Phase.Blackout, 20));
        Assert.Contains("2 min", CommsLink.StaleBanner(CommsLink.Phase.Blackout, 120));
    }

    [Fact]
    public void UnconfirmedTag_FlagsButNeverAltersTheSuitClock()
    {
        // The away/doom clock keeps its live number; the tag only says the ship can't confirm it.
        Assert.Equal(string.Empty, CommsLink.UnconfirmedTag(CommsLink.Phase.Nominal));
        Assert.Contains("unconfirmed", CommsLink.UnconfirmedTag(CommsLink.Phase.Blackout));
        Assert.NotEqual(string.Empty, CommsLink.UnconfirmedTag(CommsLink.Phase.Degraded));
    }

    [Fact]
    public void RecoveryPulse_TellsTheTrueState_IncludingBadNews()
    {
        // Calm recovery: the ship was there all along.
        Assert.Contains("still there", CommsLink.RecoveryPulse(recoveredSeverity: 0));
        // Honest recovery: if the hold went bad while dark, recovery owns it — never hidden.
        Assert.Contains("bad news", CommsLink.RecoveryPulse(recoveredSeverity: 2));
    }

    [Fact]
    public void Humanize_ReadsSecondsThenMinutes()
    {
        Assert.Equal("moments", CommsLink.Humanize(0.4));
        Assert.Equal("5s", CommsLink.Humanize(5));
        Assert.Equal("1 min", CommsLink.Humanize(60));
        Assert.Equal("2 min", CommsLink.Humanize(115));
    }
}
