namespace SpaceSails.Core;

/// <summary>
/// COMMS-LOSS · The long silence (owner, cruise 2026-07-19, when the session was throttled: <b>"loss of
/// comms.. that also is great horror element."</b>). On an away excursion the mothership's telemetry
/// downlink can DEGRADE or DROP for a while — the away-mission HUD stops getting fresh confirmation and
/// FREEZES at "last known", while the captain's HANDHELD instruments (the motion tracker, the nerve
/// gauge, the local away/dig clock) keep running honestly on the suit. <b>Stale information IS the
/// fear</b>: is the ship still up there holding the orbit? You can't hear it. Pairs with the fog-of-war
/// (#393) and the long ear (#338).
///
/// <para><b>THE HONESTY LAW (the difference between fair dread and a feels-bad bug).</b> Comms-loss must
/// NEVER strand the player: it withholds the DISPLAYED confirmation only — the ship's real state keeps
/// advancing correctly underneath, nothing on the surface self-resolves into an unavoidable death
/// (liftoff is always player-initiated), and when the link recovers the TRUE current state is shown at
/// once (with a catch-up rush). This module is therefore a pure <i>display gate</i> and a
/// <i>presentation vocabulary</i> — it owns no game state that a consequence rides on. What it degrades
/// is the mothership's own telemetry (the orbit-hold ladder, <see cref="OrbitHold"/>, which quotes the
/// ship's tank). What it must NEVER freeze is anything the captain reckons locally on the suit — the
/// away-window / doom clock is the suit's own count, a hard deadline whose closing costs crew, so it
/// stays live and merely gets flagged "unconfirmed by the ship".</para>
///
/// <para>Pure and fully deterministic from a threat seed and a monotonic onset index — the same idiom
/// as <see cref="ReeverTide"/>, salted off the ONE shared <see cref="DiceRule"/> engine (never
/// <see cref="System.Random"/> or the clock — determinism is law in Core). Given a seed and the next
/// index it answers "how long until the next comms episode", "how long it lasts", and "does it deepen
/// to a full blackout"; given an episode's start/shape and the on-site clock it answers the live
/// <see cref="Phase"/>. The live accumulator, the last-known snapshot and the pulse copy are the
/// client's thin real-time layer.</para>
/// </summary>
public static class CommsLink
{
    /// <summary>The state of the downlink from the mothership to the away team.</summary>
    public enum Phase
    {
        /// <summary>Full telemetry — the HUD shows the live, ship-confirmed value.</summary>
        Nominal = 0,
        /// <summary>The feed is breaking up — the readout is stale/uncertain but still trickling.</summary>
        Degraded = 1,
        /// <summary>Dead air — no downlink at all; the readout is frozen at its last-known value.</summary>
        Blackout = 2,
    }

    // ── Onset cadence (rare-ish): how long the link stays clean between episodes. ────────────────────

    /// <summary>The mean quiet gap between comms episodes (on-site seconds). Long enough that most short
    /// hops never see a drop — the dread is that it CAN happen, deep in a stay, not that it always does.</summary>
    public const double MeanGapSeconds = 85.0;

    /// <summary>How far a single gap jitters off the mean, as a fraction — each gap lands in
    /// <c>Mean × [1 − Jitter, 1 + Jitter]</c>, deterministic per (seed, index). Never
    /// <see cref="System.Random"/>, so a test replays the exact schedule.</summary>
    public const double GapJitterFraction = 0.55;

    /// <summary>A hard floor on any gap (seconds) so the jitter's low tail can never chain two episodes
    /// back-to-back into a permanent outage.</summary>
    public const double MinGapSeconds = 30.0;

    // ── Episode shape: a degraded ramp, maybe a blackout core, a degraded tail, then recovery. ───────

    /// <summary>The mean length of one comms episode (seconds) — a short storm, not a marooning.</summary>
    public const double MeanEpisodeSeconds = 16.0;

    /// <summary>How far an episode's length jitters off the mean, as a fraction.</summary>
    public const double EpisodeJitterFraction = 0.5;

    /// <summary>A floor on any episode's length (seconds).</summary>
    public const double MinEpisodeSeconds = 7.0;

    /// <summary>The fraction of a DEEPENING episode at EACH edge that reads <see cref="Phase.Degraded"/>
    /// (the feed breaking up on the way down and clawing back on the way up); the middle span is the full
    /// <see cref="Phase.Blackout"/>. Two edges, so twice this must stay under 1.</summary>
    public const double DegradedEdgeFraction = 0.3;

    /// <summary>A non-deepening episode is a shallow wobble — <see cref="Phase.Degraded"/> throughout,
    /// never a full blackout — chosen this often. The rest deepen to dead air in the middle.</summary>
    public const double BlackoutChance = 0.55;

    // The fraction resolution: a large-faced die off the shared rule gives a smooth [0,1) sample while
    // staying every bit as platform-stable and replayable as the dice engine itself.
    private const int Resolution = 4096;

    /// <summary>Seconds of clean link until the <paramref name="onsetIndex"/>-th episode begins (0-based),
    /// jittered around <see cref="MeanGapSeconds"/> and floored at <see cref="MinGapSeconds"/>. The
    /// <paramref name="onsetBias"/> multiplies the ODDS of a near-term drop (deep in a site, during solar
    /// interference): a bias &gt; 1 divides the gap down so drops come sooner; 1 is the baseline; it can
    /// never push a gap below the floor. Pure and deterministic in <paramref name="seed"/>.</summary>
    public static double NextGap(ulong seed, int onsetIndex, double onsetBias = 1.0)
    {
        double u = Fraction(seed, $"comms-gap:{onsetIndex}");                 // [0,1)
        double gap = MeanGapSeconds * ((1.0 - GapJitterFraction) + (2.0 * GapJitterFraction * u));
        double biased = gap / System.Math.Max(0.01, onsetBias);
        return System.Math.Max(MinGapSeconds, biased);
    }

    /// <summary>How long the <paramref name="onsetIndex"/>-th episode lasts (seconds), jittered around
    /// <see cref="MeanEpisodeSeconds"/> and floored at <see cref="MinEpisodeSeconds"/>. Salted apart from
    /// the gap stream so length and timing never correlate on the shared seed.</summary>
    public static double EpisodeDuration(ulong seed, int onsetIndex)
    {
        double u = Fraction(seed, $"comms-dur:{onsetIndex}");
        double dur = MeanEpisodeSeconds * ((1.0 - EpisodeJitterFraction) + (2.0 * EpisodeJitterFraction * u));
        return System.Math.Max(MinEpisodeSeconds, dur);
    }

    /// <summary>Does the <paramref name="onsetIndex"/>-th episode deepen to a full
    /// <see cref="Phase.Blackout"/> (true) or stay a shallow <see cref="Phase.Degraded"/> wobble (false)?
    /// Deterministic per (seed, index), salted apart from the gap and duration streams.</summary>
    public static bool EpisodeDeepens(ulong seed, int onsetIndex) =>
        Fraction(seed, $"comms-deep:{onsetIndex}") < BlackoutChance;

    /// <summary>The live <see cref="Phase"/> inside an episode that began at
    /// <paramref name="episodeStart"/> (on-site seconds), of length <paramref name="duration"/>, at the
    /// current clock <paramref name="now"/>. Before the start or after the end it is
    /// <see cref="Phase.Nominal"/>. A non-<paramref name="deepens"/> episode is
    /// <see cref="Phase.Degraded"/> for its whole span; a deepening one ramps Degraded → Blackout →
    /// Degraded, the blackout owning the middle <c>1 − 2×<see cref="DegradedEdgeFraction"/></c>. Pure, so
    /// the whole arc pins in a test.</summary>
    public static Phase PhaseAt(double episodeStart, double duration, bool deepens, double now)
    {
        if (duration <= 0 || now < episodeStart || now >= episodeStart + duration)
        {
            return Phase.Nominal;
        }
        if (!deepens)
        {
            return Phase.Degraded;
        }
        double into = (now - episodeStart) / duration;               // 0..1 through the episode
        double edge = System.Math.Clamp(DegradedEdgeFraction, 0.0, 0.5);
        return into < edge || into >= 1.0 - edge ? Phase.Degraded : Phase.Blackout;
    }

    // ── The presentation vocabulary: how a withheld/stale readout reads, and the recovery rush. ──────

    /// <summary>The prefix that fronts a FROZEN mothership readout — the honest "you're looking at old
    /// news" banner, naming how long since the ship last got through. <see cref="Phase.Blackout"/> is
    /// dead air (a hard freeze); <see cref="Phase.Degraded"/> is a breaking-up feed (stale, not dead).
    /// The last-known line itself is appended by the caller; <see cref="Phase.Nominal"/> returns empty
    /// (nothing to prefix).</summary>
    public static string StaleBanner(Phase phase, double sinceContactSeconds) => phase switch
    {
        Phase.Blackout => $"⚠ SIGNAL LOST — last contact {Humanize(sinceContactSeconds)} ago · ",
        Phase.Degraded => $"⚠ SIGNAL BREAKING UP — last update {Humanize(sinceContactSeconds)} ago · ",
        _ => string.Empty,
    };

    /// <summary>The tag appended to a reading the captain reckons LOCALLY on the suit (the away/doom
    /// clock) while comms are down — the number stays live and honest (it is the suit's own count, never
    /// the ship's downlink), but the ship can no longer confirm it. Honest by construction: it flags the
    /// loss of confirmation, it does NOT alter the figure. Empty while <see cref="Phase.Nominal"/>.</summary>
    public static string UnconfirmedTag(Phase phase) => phase switch
    {
        Phase.Blackout => "  ·  ⚠ carrier lost — suit's own count, unconfirmed",
        Phase.Degraded => "  ·  ⚠ ship's confirm breaking up",
        _ => string.Empty,
    };

    /// <summary>The catch-up rush when the link recovers (the blackout/degraded → nominal edge): the true
    /// current state, shown at once. Honest — if the hold went bad while you were dark, recovery says so
    /// (the maroon is never hidden, only its live countdown was withheld). <paramref name="recoveredSeverity"/>
    /// is the TRUE line's severity at the moment of recovery (0 calm, 1 amber, 2 red).</summary>
    public static string RecoveryPulse(int recoveredSeverity) => recoveredSeverity >= 2
        ? "📡 SIGNAL — and it's bad news: the ship slipped its hold while you were dark. That's real. Move."
        : "📡 SIGNAL — the ship's still there. Orbit held. You didn't know that a minute ago.";

    /// <summary>The one-time notice the FIRST time an away excursion loses the downlink — teaches that the
    /// frozen readout is stale, not the truth, and that the suit instruments (tracker, nerve, clock) still
    /// tell it straight.</summary>
    public const string FirstLossPulse =
        "📡… static. The ship's telemetry drops out — the orbit readout freezes on its last word. Your suit's tracker and clock still run true; the ship you'll just have to trust. Get moving.";

    // ── Copies of the humaniser kept local so Core has no cross-module coupling for a string. ─────────

    /// <summary>A compact "time since last contact" for the stale banner: seconds under a minute, then
    /// minutes. Short and honest — a blackout is a storm measured in seconds-to-minutes, never hours.</summary>
    public static string Humanize(double seconds)
    {
        if (seconds < 1)
        {
            return "moments";
        }
        if (seconds < 60)
        {
            return $"{(int)System.Math.Round(seconds)}s";
        }
        int mins = (int)System.Math.Round(seconds / 60.0);
        return $"{System.Math.Max(1, mins)} min";
    }

    // A uniform [0,1) sample: one large-faced die off the shared rule, salted by the purpose tag so the
    // gap, duration and deepen streams are independent.
    private static double Fraction(ulong seed, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed(seed, tag), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }
}
