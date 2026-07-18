namespace SpaceSails.Core;

/// <summary>
/// Lane-1 · The tide (owner, Saturday-evening playtest 2026-07-18). The one law: <b>"The idea is that
/// even with bots there is only so long time to stay there."</b> Bots rent minutes; the tide owns the
/// ground. Where the acute <see cref="ReeverRaid"/> pack turns out on a dig, the tide is the ambient
/// pressure on top of it — owner: "reevers coming from bottom of screen without any limited number …
/// at random intervals." No fixed total, no countdown: the deep just keeps handing up more, so a
/// surface stay is bounded no matter the loadout.
///
/// <para>Pure and fully deterministic from a threat seed and a monotonic spawn index — the same idiom
/// as <see cref="ReeverRaid"/> and <see cref="MotionTracker"/>, salted off the ONE shared
/// <see cref="DiceRule"/> engine (never <see cref="System.Random"/> or the clock — determinism is law
/// in Core). Given a seed and the next index, it answers "how long until the next claw-out" and "where
/// along the deep edge" so the whole cadence can be pinned in a test. The live Reever positions, the
/// spawn buffer and the engine ceiling on simultaneously ACTIVE contacts are the client's thin
/// real-time layer; the tide itself, as a rule, never stops.</para>
/// </summary>
public static class ReeverTide
{
    /// <summary>The mean gap between tide claw-outs (seconds). A brisk-but-not-frantic drip — long
    /// enough that the tracker paints each new contact well before it crests into view, short enough
    /// that lingering in the deep steadily thickens the net.</summary>
    public const double MeanGapSeconds = 6.0;

    /// <summary>How far a single gap jitters off the mean, as a fraction: each gap lands in
    /// <c>Mean × [1 − Jitter, 1 + Jitter]</c>, deterministic per (seed, index). "At random intervals"
    /// (owner) without a fixed drumbeat — but never <see cref="System.Random"/>, so a test replays it.</summary>
    public const double JitterFraction = 0.55;

    /// <summary>A hard floor on any gap (seconds) so the jitter's low tail can never collapse into a
    /// same-frame flood — the tide is relentless, not instantaneous.</summary>
    public const double MinGapSeconds = 2.0;

    /// <summary>The tide's northern limit as a fraction of the way UP from the deep edge toward the
    /// surface top (0 = the deep bottom rim, 1 = the tube mouth). North of this line the tide holds and
    /// turns back — owner: they "will stop venturing too far" toward the landing. Kept low so the home
    /// range covers the deep dig-ground but stops well short of the landing band: the consequence the
    /// owner wanted is that bots can pin a spot but can never protect the whole deep field, so time
    /// there is bounded regardless.</summary>
    public const double HomeRangeFraction = 0.45;

    // The fraction resolution: a large-faced die off the shared rule gives a smooth [0,1) sample while
    // staying every bit as platform-stable and replayable as the dice engine itself.
    private const int Resolution = 4096;

    /// <summary>Seconds until the tide hands up its <paramref name="spawnIndex"/>-th Reever (0-based),
    /// jittered around <see cref="MeanGapSeconds"/> and floored at <see cref="MinGapSeconds"/>. Pure and
    /// deterministic in <paramref name="seed"/> — the same excursion replays the same cadence.</summary>
    public static double NextGap(ulong seed, int spawnIndex)
    {
        double u = Fraction(seed, $"tide-gap:{spawnIndex}");                 // [0,1)
        double gap = MeanGapSeconds * ((1.0 - JitterFraction) + (2.0 * JitterFraction * u));
        return System.Math.Max(MinGapSeconds, gap);
    }

    /// <summary>Where along the deep edge the <paramref name="spawnIndex"/>-th tide Reever claws out — a
    /// deterministic x in [<paramref name="leftX"/>, <paramref name="rightX"/>], salted apart from the
    /// gap stream so the two never correlate. Spreads the tide across the whole bottom rim rather than a
    /// single file.</summary>
    public static double SpawnX(ulong seed, int spawnIndex, double leftX, double rightX)
    {
        if (rightX < leftX)
        {
            (leftX, rightX) = (rightX, leftX);
        }
        double u = Fraction(seed, $"tide-x:{spawnIndex}");
        return leftX + (u * (rightX - leftX));
    }

    /// <summary>The tide's home-range boundary in deck-units for a field that runs from
    /// <paramref name="surfaceTopY"/> (the tube mouth, the safe north) down to
    /// <paramref name="surfaceBottomY"/> (the deep edge). North of the returned y a tide Reever holds
    /// and turns back — the deep floods, the landing stays clear. Pure geometry so the "will stop
    /// venturing too far" law is pinned without any client dependency.</summary>
    public static double HomeRangeY(double surfaceTopY, double surfaceBottomY) =>
        surfaceBottomY + ((surfaceTopY - surfaceBottomY) * HomeRangeFraction);

    // A uniform [0,1) sample: one large-faced die off the shared rule, salted by the purpose tag so the
    // gap and position streams are independent.
    private static double Fraction(ulong seed, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed(seed, tag), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }
}
