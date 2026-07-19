namespace SpaceSails.Core;

/// <summary>
/// Reever thermal motion (owner, cruise 2026-07-19: <b>"The reevers could be more active, like little
/// thermal motion so they don't just stay still."</b>). An Old One that is momentarily STILL — pinned
/// under a sentry's guns, holding at its tide home-range leash, or idling on a stalled chase — is not a
/// statue. It shivers: a tiny, seeded Brownian shuffle around a fixed anchor, plus a slow look-around
/// twitch of its facing. Pure dread, not a phantom on the fan.
///
/// <para><b>The motion-tracker honesty (option a, the default).</b> The <see cref="MotionTracker"/> is
/// motion-gated (<see cref="MotionTracker.StillSpeed"/>) — a still contact is invisible, and the
/// sentry-pin is honest: a pinned Reever reads still ("a quiet tracker is not a safe moon, only a patient
/// one"). So the shuffle is designed to stay STRICTLY below that floor: its peak speed is well under
/// <see cref="MotionTracker.StillSpeed"/>, so if the client fed the jitter velocity to the tracker the fan
/// would still stay quiet. The client goes one honest step further and keeps the tracker-facing velocity a
/// hard zero while a contact is still, so the pinned-sentry read is preserved exactly — the shiver is pure
/// visual. (Owner-tunable escalation to option (b) — letting an idle Reever occasionally shuffle a single
/// heartbeat ABOVE the floor for a rare "still contact blipped" fright — would only need the client to
/// stop zeroing that velocity and pass the derivative through; the amplitude/rate knobs below are the seam.
/// It is deliberately NOT wired: it changes the pinned-sentry honesty, which is the owner's to spend.)</para>
///
/// <para>Deterministic-from-seed and pure (no <see cref="System.Random"/>, no clock read — determinism is
/// law in Core): a per-Reever seed fixes the phase of a small sum of incommensurate sinusoids, sampled at
/// a time <c>t</c> the client supplies (sim-seconds). Sinusoids give an exact zero mean over time (the
/// anchor never creeps), a hard amplitude bound, and a hard velocity bound — all three pinned in tests.</para>
/// </summary>
public static class ReeverIdle
{
    /// <summary>The peak per-axis wander off the anchor (deck units). Small — a weight-shift, not a drift.
    /// The two per-axis sinusoid weights sum to 1, so <c>|dx|</c> and <c>|dy|</c> are each bounded by this,
    /// and the radial wander is bounded by <c>this × √2 ≈ 0.099 du</c> (the owner's "0.05–0.1 du" shuffle).
    /// Owner-tunable amplitude knob.</summary>
    public const double WanderAmplitudeDu = 0.07;

    /// <summary>The slow component's angular rate (rad/s) — a ~7 s breath. The primary sway.</summary>
    public const double PrimaryRateRadPerSec = 0.9;

    /// <summary>The quicker component's angular rate (rad/s) — a ~3.7 s tremor layered on the sway, so the
    /// shuffle never reads as a clean metronome. Incommensurate with the primary on purpose.</summary>
    public const double SecondaryRateRadPerSec = 1.7;

    // The two components' weights (sum = 1, so the amplitude bound above holds). The slow sway dominates;
    // the quicker tremor roughens it. Chosen with the rates so the peak speed clears StillSpeed with room:
    //   peak per-axis speed = A·(w1·R1 + w2·R2) = 0.07·(0.6·0.9 + 0.4·1.7) = 0.0854 du/s,
    //   peak radial speed   = that·√2           ≈ 0.121 du/s  <  StillSpeed (0.15) — option (a) holds.
    private const double PrimaryWeight = 0.6;
    private const double SecondaryWeight = 0.4;

    /// <summary>The peak facing twitch (radians, ≈10°) — the Old One "looks around" as it shivers. Cheap
    /// and unsettling; facing is cosmetic, so this rides its own slow sinusoid pair and never touches the
    /// tracker. Bounded by this value.</summary>
    public const double FacingTwitchRad = 0.18;

    /// <summary>The seeded thermal-shuffle offset from the anchor at time <paramref name="t"/> (sim-seconds)
    /// for a Reever with the given <paramref name="seed"/> — a bounded, zero-mean 2-D wander. The two axes
    /// carry independent seeded phases so the shuffle is a shiver, not a diagonal slide. Add this to the
    /// (fixed) anchor to get the shivering body position; the anchor itself must be held stable by the
    /// caller so the mean-zero wander never creeps the resting spot.</summary>
    public static (double Dx, double Dy) JitterAt(ulong seed, double t)
    {
        double dx = Axis(seed, t, 0x11, 0x12);
        double dy = Axis(seed, t, 0x21, 0x22);
        return (dx, dy);
    }

    /// <summary>The seeded facing twitch (radians) at time <paramref name="t"/> to ADD to the Reever's base
    /// facing — a slow, bounded look-around. Pure and zero-mean, on its own phases so it doesn't lock to the
    /// positional shuffle.</summary>
    public static double FacingTwitchAt(ulong seed, double t)
    {
        double p1 = Phase(seed, 0x31);
        double p2 = Phase(seed, 0x32);
        double s = (PrimaryWeight * System.Math.Sin((PrimaryRateRadPerSec * t) + p1))
                 + (SecondaryWeight * System.Math.Sin((SecondaryRateRadPerSec * t) + p2));
        return FacingTwitchRad * s;
    }

    // One axis: a weighted sum of two incommensurate sinusoids at seeded phases, scaled to the amplitude.
    private static double Axis(ulong seed, double t, ulong saltA, ulong saltB)
    {
        double pa = Phase(seed, saltA);
        double pb = Phase(seed, saltB);
        double s = (PrimaryWeight * System.Math.Sin((PrimaryRateRadPerSec * t) + pa))
                 + (SecondaryWeight * System.Math.Sin((SecondaryRateRadPerSec * t) + pb));
        return WanderAmplitudeDu * s;
    }

    // A deterministic phase in [0, 2π) from a seed and a salt — a splitmix64 finalizer, pure and platform
    // stable (no System.Random, no clock). Distinct salts give the independent per-axis / facing phases.
    private static double Phase(ulong seed, ulong salt)
    {
        ulong z = seed + (salt * 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return z / (double)ulong.MaxValue * System.Math.Tau;
    }
}
