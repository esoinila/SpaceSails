namespace SpaceSails.Core;

/// <summary>Maps the plotting panel's burn-angle input between the captain's ship-relative
/// convention and the world-space heading the physics burns along (#201). The owner types the
/// angle he uses mid-chase — 0 = straight ahead (the ship's current heading), +90 = starboard
/// (turn to the right), −90 = port (left) — the lateral-closing move. The stored/flown heading
/// stays world-space (0° = +X, CCW), so only the input and its echo are translated here.
///
/// Starboard is a clockwise quarter-turn from the nose. In the world's CCW math frame a clockwise
/// turn is NEGATIVE, hence world = shipHeading − relative: with the nose along +X (heading 0),
/// +90 relative maps to world −90 (pointing to −Y, the ship's right on a screen whose +Y is down).
/// Pure arithmetic — cheap to unit-test.</summary>
public static class BurnHeadingConvention
{
    /// <summary>World-space heading (deg, 0°=+X, CCW) for a ship-relative angle: 0 ahead, +90
    /// starboard, −90 port. Result is wrapped to [0, 360).</summary>
    public static double RelativeToWorld(double shipHeadingDeg, double relativeDeg) =>
        Wrap360(shipHeadingDeg - relativeDeg);

    /// <summary>The ship-relative angle (0 ahead, +90 starboard, −90 port) that a world heading
    /// reads as, given the ship's heading. Result is wrapped to (−180, 180] so the input echoes a
    /// signed "type −90/+90" number rather than a 0–360 bearing.</summary>
    public static double WorldToRelative(double shipHeadingDeg, double worldDeg) =>
        Wrap180(shipHeadingDeg - worldDeg);

    private static double Wrap360(double deg)
    {
        deg %= 360;
        return deg < 0 ? deg + 360 : deg;
    }

    private static double Wrap180(double deg)
    {
        deg = Wrap360(deg);
        return deg > 180 ? deg - 360 : deg;
    }
}
