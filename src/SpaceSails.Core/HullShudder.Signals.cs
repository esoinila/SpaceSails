namespace SpaceSails.Core;

/// <summary>
/// #251 · THE TWO SIBLING EVENTS, AS BEHAVIOUR — the unexplained signal's cadence, line and glance, the
/// caution PA's line, and the shared uniform <c>Fraction</c>.
///
/// <para>Split out of <c>HullShudder.cs</c> under #251 as a pure move: two runs of the base file, no member
/// renamed, re-scoped or re-ordered. The two events' banners, constants and line pools stay in the opening
/// file with the rest of the class's static fields, in their original order (#1163).</para>
/// </summary>
public static partial class HullShudder
{
    /// <summary>The unexplained-signal line pool — <paramref name="cold"/> for the story-deep escalation
    /// (the lingering glance), else the ordinary "back to work, saying nothing" voice. Exposed so a test can
    /// pin every line non-blank and the pool free of duplicates.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> SignalLinesFor(bool cold) =>
        cold ? SignalColdLines : SignalLines;

    /// <summary>Seconds of calm until the <paramref name="signalIndex"/>-th unexplained signal (0-based),
    /// jittered around <see cref="SignalMeanGapSeconds"/> and floored at <see cref="SignalMinGapSeconds"/>.
    /// Pure and deterministic in <paramref name="seed"/>, salted apart from the shudder's own gap stream so
    /// the two ambient rhythms never lock together.</summary>
    public static double SignalNextGap(ulong seed, int signalIndex)
    {
        double u = Fraction(seed, $"signal-gap:{signalIndex}");             // [0,1)
        double gap = SignalMeanGapSeconds * ((1.0 - SignalGapJitterFraction) + (2.0 * SignalGapJitterFraction * u));
        return System.Math.Max(SignalMinGapSeconds, gap);
    }

    /// <summary>The house-voice line for an unexplained signal — deterministically drawn from the ordinary
    /// or <paramref name="cold"/> pool per (seed, index), salted apart from every shudder stream.</summary>
    public static string SignalLine(bool cold, ulong seed, int signalIndex)
    {
        System.Collections.Generic.IReadOnlyList<string> pool = SignalLinesFor(cold);
        return pool[Index(seed, $"signal-line:{(cold ? "cold" : "warm")}:{signalIndex}", pool.Count)];
    }

    /// <summary>Is the staff glance still held at <paramref name="tSinceOnset"/> seconds after the buzzer?
    /// The <paramref name="cold"/> glance lingers a beat too long (<see cref="ColdGlanceDurationSeconds"/>)
    /// where the ordinary one has already let go (<see cref="GlanceDurationSeconds"/>).</summary>
    public static bool Glancing(double tSinceOnset, bool cold) =>
        tSinceOnset >= 0.0 && tSinceOnset < (cold ? ColdGlanceDurationSeconds : GlanceDurationSeconds);

    /// <summary>The caution-PA line pool — <paramref name="cold"/> for the deep/lab/story escalation, else
    /// the ordinary rough-passage advisory. Exposed so a test can pin every line non-blank, the pool free of
    /// duplicates, and the parenthetical undercut present (the mood).</summary>
    public static System.Collections.Generic.IReadOnlyList<string> CautionLinesFor(bool cold) =>
        cold ? CautionColdLines : CautionLines;

    /// <summary>The house-voice caution PA for a rough patch — deterministically drawn from the ordinary or
    /// <paramref name="cold"/> pool per (seed, index), salted apart from every shudder/signal stream.</summary>
    public static string CautionLine(bool cold, ulong seed, int cautionIndex)
    {
        System.Collections.Generic.IReadOnlyList<string> pool = CautionLinesFor(cold);
        return pool[Index(seed, $"caution-line:{(cold ? "cold" : "warm")}:{cautionIndex}", pool.Count)];
    }

    // A uniform [0,1) sample: one large-faced die off the shared rule, salted by the purpose tag so the
    // gap, line and chill streams are independent.
    private static double Fraction(ulong seed, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed(seed, tag), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }
}
