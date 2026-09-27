namespace SpaceSails.Core;

/// <summary>
/// #251 · WHEN IT COMES AND WHAT IT FEELS LIKE — the cadence, the context-to-setting map, the escalation
/// gate, the decaying shake and the held pause, and the seeded dice under all of them.
///
/// <para>Split out of <c>HullShudder.cs</c> under #251 as a pure move: two runs of the base file, no member
/// renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class HullShudder
{
    // ── The cadence. ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Seconds of calm until the <paramref name="shudderIndex"/>-th shudder (0-based), jittered
    /// around <see cref="MeanGapSeconds"/> and floored at <see cref="MinGapSeconds"/>. Pure and
    /// deterministic in <paramref name="seed"/> — the same deck replays the same rhythm.</summary>
    public static double NextGap(ulong seed, int shudderIndex)
    {
        double u = Fraction(seed, $"shudder-gap:{shudderIndex}");            // [0,1)
        double gap = MeanGapSeconds * ((1.0 - GapJitterFraction) + (2.0 * GapJitterFraction * u));
        return System.Math.Max(MinGapSeconds, gap);
    }

    // ── Context → setting. ────────────────────────────────────────────────────────────────────────────

    /// <summary>Which house voice speaks a shudder given the captain's context. A <paramref name="deepSite"/>
    /// (a surface/lab/secret-lab excursion) reads coldest and WINS — even at a docked haven, a landing is a
    /// landing; otherwise a docked haven speaks the reassuring clamps/settling voice; failing both, it's the
    /// ship's own deck. Pure, so the selection pins in a test.</summary>
    public static Setting SettingFor(bool deepSite, bool haven) =>
        deepSite ? Setting.DeepSite : haven ? Setting.Haven : Setting.Ship;

    /// <summary>#590 · The four-way version, which exists because the three-way one was quietly wrong.
    ///
    /// <para>Owner, standing on a moon: <i>"the hull flexing feeling buildings should not come when on
    /// planet / moon ... we should have place specific ones for the sites, not generic ones of the ship
    /// playing on site."</i></para>
    ///
    /// <para>The flag was right — a surface excursion already selected <see cref="Setting.DeepSite"/> — and
    /// the WORDS were wrong, which is a harder bug to see. Every deep-site line names a hull, a room, and
    /// other people: <i>"the steel laid over it"</i>, <i>"the whole room freezes"</i>, <i>"every head in the
    /// place comes up"</i>. Those were written for a sealed site with a crew in it, and read as the ship's
    /// voice because they ARE an interior's voice.</para>
    ///
    /// <para>On open regolith there is no hull, no room, and — the part that matters — <b>nobody else to
    /// look up with you</b>. The unison beat, which is the whole shape of this mechanic, has no one to be in
    /// unison with. That is not a reason to drop the beat out here; it is the best thing that could happen
    /// to it.</para></summary>
    /// <summary>#867 · …AND THE GROUND ANSWERS FIRST, which exists because the four-way one was quietly
    /// wrong in exactly the way the three-way one had been.
    ///
    /// <para>Owner, 2026-08-13, strolling the B1 park: <i>"Our mood texts still assume the vacuum of the
    /// surface btw :-D ... talk of nothing carrying the sound here as we are literally taking a walk in a
    /// park :-D"</i></para>
    ///
    /// <para>A floor of the Hive is laid inside the SURFACE'S OWN coordinate envelope, so
    /// <c>onRegolith</c> — "below the regolith's top rim and away from your own tube" — is TRUE two hundred
    /// deck units down in a lit, planted, pressurised gallery. Every line of the Regolith pool was therefore
    /// reachable on a lawn under grow-lights, and the pool's whole tell is that there is no air out there to
    /// carry a sound.</para>
    ///
    /// <para><b>The law (the #802 row law, extended to mood).</b> A mood sentence is a claim about the room,
    /// so it asks the same fact the lift panel's rows ask — <see cref="UndergroundComplex.HoldsPressure"/> —
    /// and speaks the register the ground earns. <paramref name="groundHoldsPressure"/> WINS OUTRIGHT over
    /// every other flag here: whatever else is true of a landing, a floor that holds its own air is not a
    /// place where nothing carries the sound. Vacuum ground keeps every existing line, byte for byte.</para>
    /// </summary>
    public static Setting SettingOutside(bool groundHoldsPressure, bool onRegolith, bool deepSite, bool haven) =>
        groundHoldsPressure ? Setting.Pressurised
        : onRegolith ? Setting.Regolith
        : SettingFor(deepSite, haven);

    // ── The escalation gate. ──────────────────────────────────────────────────────────────────────────

    /// <summary>Does the <paramref name="shudderIndex"/>-th shudder carry a CHILL? Deterministic per (seed,
    /// index), salted apart from the gap and line streams. The caller only consults this at an eligible site
    /// (a secret lab, or an arc gone deep); a haven/ship shudder never chills. Fires
    /// <see cref="ChillChance"/> of the time — mostly even a deep-site shudder is still nothing.</summary>
    public static bool CarriesChill(ulong seed, int shudderIndex) =>
        Fraction(seed, $"shudder-chill:{shudderIndex}") < ChillChance;

    // ── The shudder's shape. ──────────────────────────────────────────────────────────────────────────

    /// <summary>The decay envelope of the deck-shake at <paramref name="tSinceOnset"/> seconds after the
    /// tremor began: 1 at the first instant, easing to exactly 0 at <see cref="ShakeDurationSeconds"/>.
    /// An exponential decay times a linear taper — sharp at the start, fully settled by the end, so the
    /// shake can never linger into a sustained (nauseating) rumble. In [0, 1].</summary>
    public static double ShakeEnvelope(double tSinceOnset)
    {
        if (tSinceOnset <= 0.0)
        {
            return tSinceOnset < 0.0 ? 0.0 : 1.0;
        }
        if (tSinceOnset >= ShakeDurationSeconds)
        {
            return 0.0;
        }
        double taper = 1.0 - (tSinceOnset / ShakeDurationSeconds);   // hits 0 at the end
        return System.Math.Exp(-tSinceOnset / ShakeDecayTau) * taper;
    }

    /// <summary>The seeded deck-shake OFFSET at <paramref name="tSinceOnset"/> seconds after onset — a
    /// bounded, decaying 2-D jitter (the <see cref="ReeverIdle"/> incommensurate-sinusoid idiom under the
    /// <see cref="ShakeEnvelope"/>). The two axes carry independent seeded phases so the shake is a shiver,
    /// not a diagonal slide. Each axis is bounded by <see cref="ShakeAmplitude"/> × the envelope, so it is
    /// always in [−1, 1] and is exactly (0, 0) at and beyond <see cref="ShakeDurationSeconds"/> (and before
    /// onset). The client multiplies this by a small pixel amplitude and adds it to the render pan — a pure
    /// visual offset that never moves an entity anchor.</summary>
    public static (double Dx, double Dy) ShakeOffset(ulong seed, double tSinceOnset)
    {
        double env = ShakeEnvelope(tSinceOnset);
        if (env <= 0.0)
        {
            return (0.0, 0.0);
        }
        double dx = env * Axis(seed, tSinceOnset, 0x51, 0x52);
        double dy = env * Axis(seed, tSinceOnset, 0x61, 0x62);
        return (dx, dy);
    }

    /// <summary>Is the unison pause still held at <paramref name="tSinceOnset"/> seconds after onset? The
    /// window all present NPCs freeze (heads up, idle jitter stopped) on the shared onset timestamp — the
    /// synchronized held breath — before resuming as one.</summary>
    public static bool Pausing(double tSinceOnset) =>
        tSinceOnset >= 0.0 && tSinceOnset < PauseDurationSeconds;

    // One shake axis: a weighted sum of two incommensurate sinusoids at seeded phases, scaled to the unit
    // amplitude. The weights sum to 1, so |value| ≤ ShakeAmplitude before the envelope multiplies it.
    private static double Axis(ulong seed, double t, ulong saltA, ulong saltB)
    {
        double pa = Phase(seed, saltA);
        double pb = Phase(seed, saltB);
        double s = (ShakeWeight1 * System.Math.Sin((ShakeRate1RadPerSec * t) + pa))
                 + (ShakeWeight2 * System.Math.Sin((ShakeRate2RadPerSec * t) + pb));
        return ShakeAmplitude * s;
    }

    // A deterministic phase in [0, 2π) from a seed and a salt — a splitmix64 finalizer, pure and platform
    // stable (no System.Random, no clock). Distinct salts give the independent per-axis phases.
    private static double Phase(ulong seed, ulong salt)
    {
        ulong z = seed + (salt * 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return z / (double)ulong.MaxValue * System.Math.Tau;
    }

    // A deterministic index into a pool of the given size, salted by the purpose tag off the shared rule.
    private static int Index(ulong seed, string tag, int count) =>
        count <= 1 ? 0 : (int)(Fraction(seed, tag) * count) % count;
}
