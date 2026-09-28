namespace SpaceSails.Core;

/// <summary>
/// #251 · A FALL THAT NEVER PUNCHES A BODY (#127/#128) — the obstacle record, the safe approach velocity
/// and aim point, and the geometry under them.
///
/// <para>Split out of <c>OrbitRule.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class OrbitRule
{
    // ---- Body-avoidance approach (issues #127/#128): a fall that never punches a body ----

    /// <summary>A body the approach chord must NOT cross — the target's parent planet when
    /// auto-orbiting a moon. Position is in world coordinates at the current instant.</summary>
    public readonly record struct ApproachObstacle(Vector2d Position, double SafeRadius);

    /// <summary>
    /// The safe "point at it and throttle" velocity — the naive <see cref="ApproachVelocity"/>
    /// with two guards. (1) The aim is offset off the TARGET center by the impact parameter that
    /// puts the gravity-focused ballistic periapsis at <see cref="ApproachSafeBodyRadii"/>·R, so
    /// the fall arcs into insertion instead of center-punching. (2) When a PARENT obstacle sits
    /// across the chord, the aim becomes a tangent point that rounds it at its safe radius. Same
    /// closing speed and body-relative framing as the naive version; deterministic and cheap.
    /// With no obstacle this differs from <see cref="ApproachVelocity"/> only by the small
    /// off-center b-plane offset (a few body radii — a needle at Hill-sphere scale).
    /// </summary>
    public static Vector2d SafeApproachVelocity(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body,
        ApproachObstacle? parent = null, double hillRadius = 0)
    {
        Vector2d aim = SafeAimPoint(ship, bodyPosition, bodyVelocity, body, parent, hillRadius);
        Vector2d toAim = aim - ship.Position;
        double distance = toAim.Length;
        return distance <= 0
            ? bodyVelocity
            : bodyVelocity + toAim / distance * ApproachClosingSpeed(body, hillRadius);
    }

    /// <summary>Mass-pulse cost of the safe approach burn from the current state (at least 1).</summary>
    public static int ApproachPulseCost(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, ApproachObstacle? parent, double hillRadius = 0) =>
        PulsesFor((SafeApproachVelocity(ship, bodyPosition, bodyVelocity, body, parent, hillRadius) - ship.Velocity).Length, ship.Velocity.Length);

    /// <summary>Perform the safe approach burn — impulsive, like every other pulse in the game.</summary>
    public static ShipState Approach(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, ApproachObstacle? parent, double hillRadius = 0) =>
        ship with { Velocity = SafeApproachVelocity(ship, bodyPosition, bodyVelocity, body, parent, hillRadius) };

    /// <summary>
    /// Where the safe approach aims this instant. Rounds an obstructing parent first (a tangent
    /// point on its safe circle, taken the short way toward the target); otherwise falls toward
    /// the target offset off-center by the periapsis-preserving impact parameter.
    /// </summary>
    public static Vector2d SafeAimPoint(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, ApproachObstacle? parent, double hillRadius = 0)
    {
        // While a parent lies across the chord, head for the tangent that rounds it — not the
        // target hiding behind it. Once the ship has rounded past, the chord clears and the aim
        // snaps back to the target (the tick loop re-solves this every step).
        if (parent is { } p
            && ChordEntersCircle(ship.Position, bodyPosition, p.Position, p.SafeRadius)
            && TangentAimPoint(ship.Position, bodyPosition, p.Position, p.SafeRadius) is { } tangent)
        {
            return tangent;
        }

        Vector2d toBody = bodyPosition - ship.Position;
        double distance = toBody.Length;
        if (distance <= 0)
        {
            return bodyPosition;
        }

        // Aim off-center by the impact parameter whose two-body hyperbola (at the closing speed
        // we're about to set) reaches periapsis exactly at the safe radius. Aiming to merely miss
        // the center by the safe radius is NOT enough — gravity focuses the fall inward. The safe
        // periapsis is Hill-aware (issue #136): for a deep-well moon it is bent below the insertion
        // gate so the fall actually reaches capture instead of grazing forever above it.
        double safeRadius = ApproachPeriapsis(body, hillRadius);
        double offset = ImpactParameterFor(safeRadius, body.Mu, ApproachClosingSpeed(body, hillRadius));

        // Offset to the side that continues the ship's existing swing about the body (matching
        // Insert's sense choice), defaulting to +perp at dead-zero. Either side clears the surface.
        Vector2d perp = new Vector2d(-toBody.Y, toBody.X) / distance;
        Vector2d relVel = ship.Velocity - bodyVelocity;
        if (toBody.X * relVel.Y - toBody.Y * relVel.X < 0)
        {
            perp = -perp;
        }

        return bodyPosition + perp * offset;
    }

    /// <summary>Impact parameter whose two-body hyperbola at <paramref name="speed"/> has periapsis
    /// = <paramref name="periapsis"/>: b = rp·√(1 + 2μ/(rp·v²)). The √ term is the gravitational
    /// focusing that a straight-line miss ignores.</summary>
    private static double ImpactParameterFor(double periapsis, double mu, double speed) =>
        speed <= 0 || periapsis <= 0 ? periapsis : periapsis * Math.Sqrt(1 + 2 * mu / (periapsis * speed * speed));

    /// <summary>True when the segment from <paramref name="a"/> to <paramref name="b"/> passes
    /// within <paramref name="radius"/> of <paramref name="center"/> (closest point on-segment).</summary>
    private static bool ChordEntersCircle(Vector2d a, Vector2d b, Vector2d center, double radius)
    {
        Vector2d ab = b - a;
        double len2 = ab.LengthSquared;
        double t = len2 <= 0 ? 0 : Math.Clamp((center - a).Dot(ab) / len2, 0, 1);
        Vector2d closest = a + ab * t;
        return (closest - center).LengthSquared < radius * radius;
    }

    /// <summary>The tangent point on the safe circle of radius <paramref name="radius"/> about
    /// <paramref name="center"/>, from <paramref name="from"/>, taken on the side that heads
    /// toward <paramref name="toward"/> (the short way round). Null when already inside the circle.</summary>
    private static Vector2d? TangentAimPoint(Vector2d from, Vector2d toward, Vector2d center, double radius)
    {
        Vector2d toCenter = center - from;
        double length = toCenter.Length;
        if (length <= radius)
        {
            return null; // inside the safe circle — no tangent; caller falls back to the target
        }

        double tangentLength = Math.Sqrt(length * length - radius * radius);
        double alpha = Math.Asin(radius / length); // half-angle from ship→center to ship→tangent
        Vector2d dirToCenter = toCenter / length;
        Vector2d toTarget = toward - from;
        Vector2d plus = Rotate(dirToCenter, alpha);
        Vector2d minus = Rotate(dirToCenter, -alpha);
        Vector2d dir = plus.Dot(toTarget) >= minus.Dot(toTarget) ? plus : minus;
        return from + dir * tangentLength;
    }

    private static Vector2d Rotate(Vector2d v, double angle)
    {
        double c = Math.Cos(angle), s = Math.Sin(angle);
        return new Vector2d(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}
