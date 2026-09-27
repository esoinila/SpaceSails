namespace SpaceSails.Core;

/// <summary>
/// #251 · THE AUTOPILOT'S DECISION — approach or insert, the window, the insertion itself, and whether an
/// orbit is bound.
///
/// <para>Split out of <c>OrbitRule.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class OrbitRule
{
    public enum AutopilotAction { None, Approach, Insert }

    /// <summary>
    /// What the armed autopilot should do this instant. Insert once the window is open AND
    /// the ship is deep enough for a tide-proof parking orbit; otherwise, inside capture
    /// range, burn onto an approach fall when the ship is too fast for the window to ever
    /// open or the sun's tide has bent the fall off (closing speed under half the approach
    /// speed). Coasting nicely toward the window costs nothing.
    /// </summary>
    /// <param name="keptRadiusCap">#286 — an upper bound on the kept-orbit radius so the swept park
    /// clears the moon's PARENT planet (<see cref="MaxKeptRadiusUnderParent"/>). The insert fires only
    /// once the ship is deeper than <c>min(ParkingRadius, keptRadiusCap)</c>, so the circularized orbit
    /// is guaranteed flyable. Defaults to +∞ — no cap, the pre-#286 behaviour for planets and for every
    /// caller that passes no parent context (all existing tests unchanged).</param>
    public static AutopilotAction AutopilotDecision(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, double hillRadius,
        double keptRadiusCap = double.PositiveInfinity)
    {
        double distance = (ship.Position - bodyPosition).Length;
        if (WindowOpen(ship, bodyPosition, bodyVelocity, body, hillRadius)
            && distance < Math.Min(ParkingRadius(body, hillRadius), keptRadiusCap))
        {
            return AutopilotAction.Insert;
        }

        // Out of reach, or already inside where the approach was aiming (its own safe periapsis) —
        // let the ballistic swing play out rather than re-burning. Scaling this too-close guard off
        // the safe periapsis (not a fixed 4·R) is what lets a deep-well moon whose Hill sits inside
        // 4·R hand the approach over to insertion at all (issue #136).
        if (distance > CaptureRange(hillRadius) || distance < ApproachPeriapsis(body, hillRadius))
        {
            return AutopilotAction.None;
        }

        double relSpeed = (ship.Velocity - bodyVelocity).Length;
        double approachSpeed = ApproachClosingSpeed(body, hillRadius);
        bool needsBurn = relSpeed >= MaxRelativeSpeed
            || ClosingSpeed(ship, bodyPosition, bodyVelocity) < approachSpeed * 0.5;
        return needsBurn ? AutopilotAction.Approach : AutopilotAction.None;
    }

    /// <summary>Window: inside the Hill sphere, under the speed limit, above the surface.</summary>
    public static bool WindowOpen(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, double hillRadius)
    {
        double distance = (ship.Position - bodyPosition).Length;
        double relSpeed = (ship.Velocity - bodyVelocity).Length;
        return distance < hillRadius && distance > body.BodyRadius * SurfaceParkRadii && relSpeed < MaxRelativeSpeed;
    }

    /// <summary>
    /// Perform the insertion: velocity becomes the body's velocity plus the local circular
    /// velocity, keeping the ship's current swing direction around the body (or the positive
    /// sense when there is none to keep). Position and time are untouched — the burn is
    /// modeled as impulsive, like every other pulse in the game.
    /// </summary>
    public static ShipState Insert(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body) =>
        ship with { Velocity = CircularVelocity(ship, bodyPosition, bodyVelocity, body) };

    /// <summary>Bound: negative two-body energy relative to the body AND inside its Hill sphere.</summary>
    public static bool IsBound(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, double hillRadius)
    {
        Vector2d r = ship.Position - bodyPosition;
        double distance = r.Length;
        if (distance >= hillRadius || distance <= 0)
        {
            return false;
        }

        double relSpeedSq = (ship.Velocity - bodyVelocity).LengthSquared;
        return relSpeedSq / 2 - body.Mu / distance < 0;
    }

    private static Vector2d CircularVelocity(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body)
    {
        Vector2d radial = ship.Position - bodyPosition;
        double distance = radial.Length;
        Vector2d tangent = new Vector2d(-radial.Y, radial.X) / distance;

        // Keep the current swing direction; default to the positive sense at dead-zero.
        Vector2d relVel = ship.Velocity - bodyVelocity;
        double sense = radial.X * relVel.Y - radial.Y * relVel.X;
        if (sense < 0)
        {
            tangent = -tangent;
        }

        return bodyVelocity + tangent * CircularSpeed(body, distance);
    }
}
