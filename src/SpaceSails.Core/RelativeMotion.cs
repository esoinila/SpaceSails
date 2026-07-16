using System;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>ONE voice for "are we getting closer or further" (#210). Every rel-speed readout that
/// answers approach-vs-escape — the Scope corner, the Nav contact line, the war-room tracks — signs
/// the range-rate through here, matching the tracked-target card's long-standing convention:
/// POSITIVE closing speed = the gap is shrinking ("closing"), negative = it is growing ("opening").
/// Pure geometry — cheap to unit-test, no ship type needed.</summary>
public static class RelativeMotion
{
    /// <summary>Closing speed along the line of sight: how fast the range is shrinking. Positive =
    /// closing (distance reducing), negative = opening (receding). This is −d|range|/dt; a purely
    /// lateral pass reads ≈0. Same maths as the tracked-target card's "closing N km/s".</summary>
    public static double ClosingSpeed(Vector2d selfPos, Vector2d selfVel, Vector2d otherPos, Vector2d otherVel)
    {
        Vector2d toOther = otherPos - selfPos;
        double distance = toOther.Length;
        return distance <= 0 ? 0 : (selfVel - otherVel).Dot(toOther) / distance;
    }

    /// <summary>Words a closing speed with the sign AFTER the number — the Scope corner readout
    /// "12.7 km/s closing" / "12.7 km/s opening" (#210).</summary>
    public static string WordedAfter(double closingSpeedMps) =>
        $"{Math.Abs(closingSpeedMps) / 1000:F1} km/s {ClosingWord(closingSpeedMps)}";

    /// <summary>Words a closing speed with the sign BEFORE the number — the tracked-target /
    /// contact-line form "closing 12.7 km/s" / "opening 12.7 km/s" (#210).</summary>
    public static string WordedBefore(double closingSpeedMps, string format = "F1") =>
        $"{ClosingWord(closingSpeedMps)} {(Math.Abs(closingSpeedMps) / 1000).ToString(format, CultureInfo.InvariantCulture)} km/s";

    /// <summary>"closing" when the gap is shrinking (or steady), "opening" when it grows.</summary>
    public static string ClosingWord(double closingSpeedMps) =>
        closingSpeedMps >= 0 ? "closing" : "opening";
}
