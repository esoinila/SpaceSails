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

    // #453 · THE HOME RANGE IS GONE. Owner, live 2026-07-27: "Let's not have any don't venture too far
    // set-up by y-coordinate. If you can get away with it with the help of the sentries then do it but you
    // might get killed by the reevers (or end up joining them)." HomeRangeFraction / HomeRangeY lived here
    // and turned the tide back at a fixed y — an invisible horizontal line the owner watched a charge halt
    // on ("as if their path was blocked by static distance from the airlock"), where the Reever then stood
    // still and was shot. Every Old One now runs to the one barrier that is real fiction: the crew-only
    // door at the tube mouth. How deep you dare go is priced by sentries and nerve, not by geometry.

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

    /// <summary>Where along a straight edge the <paramref name="spawnIndex"/>-th tide Reever claws out — a
    /// deterministic x in [<paramref name="leftX"/>, <paramref name="rightX"/>], salted apart from the gap
    /// stream so the two never correlate.
    ///
    /// <para>#563 · SUPERSEDED FOR THE OPEN GROUND. It spread the tide along the field's bottom rim, and an
    /// unbounded world has no rim. Kept because an edge is still the right shape for a tide that rises along
    /// one — a corridor, a shaft — and because deleting a pure function to prove a point is how a pinned
    /// cadence gets lost. The moon's tide uses <see cref="SpawnAround"/>.</para></summary>
    public static double SpawnX(ulong seed, int spawnIndex, double leftX, double rightX)
    {
        if (rightX < leftX)
        {
            (leftX, rightX) = (rightX, leftX);
        }
        double u = Fraction(seed, $"tide-x:{spawnIndex}");
        return leftX + (u * (rightX - leftX));
    }

    /// <summary>#563 law 3 · HOW FAR OUT THE TIDE CLAWS UP, measured from the captain rather than from a rim.
    ///
    /// <para>The tide used to rise at <c>SurfaceBottomY + 1.5</c> — just inside the field's deep edge — and an
    /// unbounded ground has no deep edge to rise at. It rises around the CAPTAIN now, which is more honest
    /// anyway: what the deep is answering is somebody standing on it, not a coordinate.</para>
    ///
    /// <para><b>The distance is the one the rim actually gave</b>, computed rather than chosen. A captain
    /// working a landing site was somewhere between the landing band and the deep anchor; the rim stood at
    /// the field's bottom; so this is the rim's distance from the middle of the ground people walk. Read off
    /// the field rather than typed, so a field that changes shape takes the tide with it.</para>
    ///
    /// <para><b>And it must not sneak y-graded danger back in</b> (#453, owner: <i>"Let's not have any don't
    /// venture too far set-up by y-coordinate"</i>). The ring is ISOTROPIC — the bearing is a uniform sample
    /// over the whole circle, with no term in it that knows which way is deep — so the tide is exactly as
    /// thick a hundred du north of the tube as a hundred du south of it. <c>TheTreadmillTests</c> measures
    /// that: the spawns around a captain must be evenly spread over every quadrant.</para></summary>
    public static double SpawnRingDu(in SurfaceLayout.Field field) =>
        ((field.LandingBandY + field.AnchorY) / 2.0) - field.BottomY;

    /// <summary>Where the <paramref name="spawnIndex"/>-th tide Reever claws out around a captain standing at
    /// (<paramref name="captainX"/>, <paramref name="captainY"/>): a point on the ring of radius
    /// <paramref name="ringDu"/>, at a deterministic bearing salted apart from the gap stream.</summary>
    public static (double X, double Y) SpawnAround(
        ulong seed, int spawnIndex, double captainX, double captainY, double ringDu)
    {
        double bearing = Fraction(seed, $"tide-bearing:{spawnIndex}") * System.Math.Tau;
        return (captainX + (ringDu * System.Math.Cos(bearing)),
                captainY + (ringDu * System.Math.Sin(bearing)));
    }

    // A uniform [0,1) sample: one large-faced die off the shared rule, salted by the purpose tag so the
    // gap and position streams are independent.
    private static double Fraction(ulong seed, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed(seed, tag), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }
}
