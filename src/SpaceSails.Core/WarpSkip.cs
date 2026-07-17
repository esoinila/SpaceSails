using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #172 — "⏭ skip to next event". The pure arithmetic behind the warp-skip control, pulled out of
/// <c>Map.razor</c> so the next-event selection, the eased warp ramp, and the long-coast advert edge
/// can all be unit-tested without a browser.
///
/// <para>The feature is acceleration-with-a-destination, not teleportation: the live loop still
/// integrates every tick, burns still fire and fuel still spends — this class only decides WHICH
/// event is next, HOW FAST to warp toward it (respecting the neighborhood caps the caller applies on
/// top), and WHEN a long coast is worth advertising. It invents nothing: the caller feeds it the same
/// epochs the pilot banner's NOW/NEXT rows read (the one-truth rule).</para>
/// </summary>
public static class WarpSkip
{
    /// <summary>A coast longer than this (seconds to the next event) is worth advertising the cheap
    /// skip ride for — the owner's "we need the cheap autopilot ride to skip the long waits between
    /// outer and inner planets" (one sim-day).</summary>
    public const double LongCoastThresholdSeconds = 86_400.0;

    /// <summary>The final approach window over which the skip eases warp down toward 1×, so an
    /// UN-guarded event epoch (a plan end, a keeping trim) isn't overshot by more than about one
    /// integrator quantum. Guarded events (scheduled burns, arrival insertions) land exactly on their
    /// own split-advance / arrival guard regardless; this only softens the landing for the rest.</summary>
    public const double EaseWindowSeconds = 1_800.0;

    /// <summary>Within this of the target epoch the skip has ARRIVED — it drops to 1× and announces.</summary>
    public const double ArriveToleranceSeconds = 1.0;

    /// <summary>Two next-event epochs within this of each other are the SAME leg — a generous slack so
    /// the long-coast advert doesn't re-fire when the plan re-projection jitters the arrival epoch by a
    /// sample step (mirrors <see cref="PlotHorizon"/>'s hour of tolerance).</summary>
    public const double LegEpochToleranceSeconds = 3_600.0;

    /// <summary>What kind of event the skip is aimed at — drives the readout / announcement wording.</summary>
    public enum EventKind
    {
        /// <summary>No event ahead — the control is disabled.</summary>
        None,

        /// <summary>The next scheduled / plotted burn.</summary>
        Burn,

        /// <summary>The orbit-insert / dock arrival window.</summary>
        Arrival,

        /// <summary>The next station-keeping trim (while the autopilot holds a kept orbit).</summary>
        KeepTrim,

        /// <summary>A queued sensor / intercept task's completion (extension point).</summary>
        SensorPass,

        /// <summary>The plan's furthest encounter — the fallback when nothing sooner is queued.</summary>
        PlanEnd,
    }

    /// <summary>The chosen next event: whether one exists, its sim-time epoch, and its kind.</summary>
    public readonly record struct NextEvent(bool Found, double Epoch, EventKind Kind)
    {
        public static readonly NextEvent None = new(false, 0, EventKind.None);
    }

    /// <summary>One candidate epoch offered to <see cref="Resolve"/>. A null epoch is "not applicable"
    /// (e.g. no burn pending) and is skipped.</summary>
    public readonly record struct Candidate(double? Epoch, EventKind Kind);

    /// <summary>
    /// The soonest strictly-future candidate — the event the skip aims at. Pure: it reads only the
    /// epochs the caller supplies, which are the same truths the banner's NEXT row reads. Candidates
    /// already at or before now (within <see cref="ArriveToleranceSeconds"/>) are ignored; the earliest
    /// remaining wins. Returns <see cref="NextEvent.None"/> when nothing is queued — the caller's cue to
    /// DISABLE the control.
    /// </summary>
    public static NextEvent Resolve(double nowSeconds, IEnumerable<Candidate> candidates)
    {
        NextEvent best = NextEvent.None;
        foreach (Candidate c in candidates)
        {
            if (c.Epoch is not { } epoch)
            {
                continue;
            }

            if (epoch <= nowSeconds + ArriveToleranceSeconds)
            {
                continue; // already here or in the past — not a future event to skip to.
            }

            if (!best.Found || epoch < best.Epoch)
            {
                best = new NextEvent(true, epoch, c.Kind);
            }
        }

        return best;
    }

    /// <summary>
    /// The warp level to command this frame while skipping toward an epoch <paramref name="remainingSeconds"/>
    /// away, never above <paramref name="maxWarp"/>. Cranks to the ceiling while far; eases proportionally
    /// toward 1× across the final <see cref="EaseWindowSeconds"/> so an un-guarded epoch lands soft. The
    /// caller's neighborhood caps (near-body warp clamps, deep-well insertion caps) apply ON TOP of this —
    /// this never bypasses them; it only ever asks for LESS as the target nears.
    /// </summary>
    public static int SkipWarp(double remainingSeconds, int maxWarp)
    {
        if (maxWarp < 1)
        {
            maxWarp = 1;
        }

        if (remainingSeconds <= ArriveToleranceSeconds)
        {
            return 1;
        }

        if (remainingSeconds >= EaseWindowSeconds)
        {
            return maxWarp;
        }

        double frac = remainingSeconds / EaseWindowSeconds;
        int eased = (int)Math.Round(maxWarp * frac);
        return Math.Clamp(eased, 1, maxWarp);
    }

    /// <summary>Has the skip reached (or just crossed) its target epoch?</summary>
    public static bool HasArrived(double nowSeconds, double targetEpoch) =>
        nowSeconds >= targetEpoch - ArriveToleranceSeconds;

    // ===== Long-coast advert edge detection (owner addition: advertise the cheap ride) =====

    /// <summary>The per-leg memory the long-coast advert carries between frames: whether the current
    /// long coast has already been announced, and for which target epoch (its leg identity).</summary>
    public readonly record struct LongCoastState(bool Offered, double OfferedEpoch)
    {
        public static readonly LongCoastState Idle = new(false, 0);
    }

    /// <summary><see cref="EvaluateLongCoast"/>'s result: whether THIS call is the rising edge that
    /// should fire the advert + squawk, and the updated per-leg state to carry forward.</summary>
    public readonly record struct LongCoastDecision(bool Fire, LongCoastState State);

    /// <summary>
    /// Edge-detect the long-coast advert: fire ONCE when a long coast to the next event begins, re-arm
    /// on the next leg. Pure, so "offers once per leg, re-arms" is unit-tested.
    ///
    /// <list type="bullet">
    /// <item>While a skip is already running the advert is frozen (no point offering the ride you're on).</item>
    /// <item>With no long coast — nothing armed, or the next event is within
    /// <paramref name="thresholdSeconds"/> — the state re-arms so the NEXT long leg fires afresh.</item>
    /// <item>A long coast whose target epoch matches the one already offered (within
    /// <see cref="LegEpochToleranceSeconds"/>) is the SAME leg — stay quiet.</item>
    /// <item>Otherwise it is a fresh long-coast leg: fire, and remember this leg's epoch.</item>
    /// </list>
    /// </summary>
    public static LongCoastDecision EvaluateLongCoast(
        LongCoastState prev, bool skipActive, NextEvent next, double nowSeconds, double thresholdSeconds)
    {
        if (skipActive)
        {
            return new LongCoastDecision(false, prev); // frozen while the ride is under way.
        }

        bool longCoast = next.Found && (next.Epoch - nowSeconds) > thresholdSeconds;
        if (!longCoast)
        {
            return new LongCoastDecision(false, LongCoastState.Idle); // re-arm for the next leg.
        }

        bool sameLeg = prev.Offered && Math.Abs(prev.OfferedEpoch - next.Epoch) <= LegEpochToleranceSeconds;
        if (sameLeg)
        {
            return new LongCoastDecision(false, prev);
        }

        return new LongCoastDecision(true, new LongCoastState(true, next.Epoch));
    }
}
