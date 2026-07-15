namespace SpaceSails.Core;

/// <summary>
/// Orbital insertion around a planet (M20). Prograde-only pulses can scale the velocity vector
/// but never rotate it, so entering a planetary orbit by hand is physically impossible — this
/// is the ship system that does the turn. The window: inside the planet's Hill sphere and under
/// the relative-speed limit. The cost: honest — pulses proportional to the Δv the insertion
/// burn actually performs.
/// </summary>
public static class OrbitRule
{
    /// <summary>Above this relative speed the insertion burn would shred the sail. Same limit as boarding.</summary>
    public const double MaxRelativeSpeed = 5000;

    /// <summary>The indicator becomes visible within this many Hill radii — approach guidance.</summary>
    public const double IndicatorRangeHillRadii = 5;

    /// <summary>One insertion pulse buys this fraction of current heliocentric speed as Δv.</summary>
    public const double DeltaVPerPulseFraction = 0.01;

    /// <summary>
    /// M25: the armed autopilot works from this many Hill radii out. Threading the bare Hill
    /// sphere by hand is a needle at map scale (owner: prograde-only steering can't do close
    /// maneuvers — "point at it and throttle would really be used instead", so the ship's
    /// systems do exactly that, priced in pulses like every assisted burn).
    /// </summary>
    public const double CaptureRangeHillRadii = 5;

    /// <summary>Capture-range floor: Mercury's Hill sphere is ~2e8 m — invisible at plot zoom.</summary>
    public const double CaptureRangeFloorMeters = 3e9;

    /// <summary>Auto-approach closing speed as a fraction of the window's speed limit —
    /// arrives under the limit with margin for tidal drift along the fall.</summary>
    public const double ApproachSpeedFraction = 0.8;

    /// <summary>The approach aims to clear the TARGET's own surface by this factor of its body
    /// radius. A naive fall aimed at the center gravity-focuses straight into the planet — the
    /// owner's Saturn playtest flew right through it — so the aim is offset off-center by the
    /// impact parameter that puts the ballistic periapsis at this safe radius.</summary>
    public const double ApproachSafeBodyRadii = 2.0;

    /// <summary>When auto-orbiting a MOON, the approach chord is bent around its PARENT planet,
    /// kept this many parent-radii clear — so aiming at Enceladus never threads Saturn.</summary>
    public const double ParentSafeBodyRadii = 2.0;

    /// <summary>The autopilot inserts only this deep inside the Hill sphere. A manual O-press
    /// at the Hill edge is the player's choice; the autopilot parks where the sun's tide
    /// cannot strip the orbit (prograde orbits are long-term stable to roughly half Hill).</summary>
    public const double AutopilotInsertHillFraction = 0.5;

    /// <summary>Hill-sphere radius: where the body's gravity owns a satellite against its parent's tide.</summary>
    public static double HillRadius(CelestialBody body, double parentMu) =>
        body.OrbitRadius * Math.Pow(body.Mu / (3 * parentMu), 1.0 / 3.0);

    /// <summary>Circular-orbit speed around the body at the given distance.</summary>
    public static double CircularSpeed(CelestialBody body, double distance) =>
        Math.Sqrt(body.Mu / distance);

    /// <summary>The Δv the insertion burn must perform from the current state.</summary>
    public static double InsertionDeltaV(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body)
    {
        Vector2d target = CircularVelocity(ship, bodyPosition, bodyVelocity, body);
        return (target - ship.Velocity).Length;
    }

    /// <summary>Mass-pulse cost of the insertion from the current state (at least 1).</summary>
    public static int PulseCost(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body) =>
        PulsesFor(InsertionDeltaV(ship, bodyPosition, bodyVelocity, body), ship.Velocity.Length);

    private static int PulsesFor(double deltaV, double currentSpeed)
    {
        double unit = Math.Max(1.0, currentSpeed * DeltaVPerPulseFraction);
        return Math.Max(1, (int)Math.Ceiling(deltaV / unit));
    }

    /// <summary>The distance from which the armed autopilot can take over.</summary>
    public static double CaptureRange(double hillRadius) =>
        Math.Max(CaptureRangeHillRadii * hillRadius, CaptureRangeFloorMeters);

    /// <summary>Rate at which the distance to the body is shrinking. Negative = receding.</summary>
    public static double ClosingSpeed(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity)
    {
        Vector2d toBody = bodyPosition - ship.Position;
        double distance = toBody.Length;
        return distance <= 0 ? 0 : (ship.Velocity - bodyVelocity).Dot(toBody) / distance;
    }

    /// <summary>The "point at it and throttle" velocity: fall straight at the body at a safe closing speed.</summary>
    public static Vector2d ApproachVelocity(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity)
    {
        Vector2d toBody = bodyPosition - ship.Position;
        double distance = toBody.Length;
        return distance <= 0
            ? bodyVelocity
            : bodyVelocity + toBody / distance * (MaxRelativeSpeed * ApproachSpeedFraction);
    }

    /// <summary>Mass-pulse cost of the approach burn from the current state (at least 1).</summary>
    public static int ApproachPulseCost(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity) =>
        PulsesFor((ApproachVelocity(ship, bodyPosition, bodyVelocity) - ship.Velocity).Length, ship.Velocity.Length);

    /// <summary>Perform the approach burn — impulsive, like every other pulse in the game.</summary>
    public static ShipState Approach(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity) =>
        ship with { Velocity = ApproachVelocity(ship, bodyPosition, bodyVelocity) };

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
        ApproachObstacle? parent = null)
    {
        Vector2d aim = SafeAimPoint(ship, bodyPosition, bodyVelocity, body, parent);
        Vector2d toAim = aim - ship.Position;
        double distance = toAim.Length;
        return distance <= 0
            ? bodyVelocity
            : bodyVelocity + toAim / distance * (MaxRelativeSpeed * ApproachSpeedFraction);
    }

    /// <summary>Mass-pulse cost of the safe approach burn from the current state (at least 1).</summary>
    public static int ApproachPulseCost(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, ApproachObstacle? parent) =>
        PulsesFor((SafeApproachVelocity(ship, bodyPosition, bodyVelocity, body, parent) - ship.Velocity).Length, ship.Velocity.Length);

    /// <summary>Perform the safe approach burn — impulsive, like every other pulse in the game.</summary>
    public static ShipState Approach(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, ApproachObstacle? parent) =>
        ship with { Velocity = SafeApproachVelocity(ship, bodyPosition, bodyVelocity, body, parent) };

    /// <summary>
    /// Where the safe approach aims this instant. Rounds an obstructing parent first (a tangent
    /// point on its safe circle, taken the short way toward the target); otherwise falls toward
    /// the target offset off-center by the periapsis-preserving impact parameter.
    /// </summary>
    public static Vector2d SafeAimPoint(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, ApproachObstacle? parent)
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
        // the center by the safe radius is NOT enough — gravity focuses the fall inward.
        double safeRadius = body.BodyRadius * ApproachSafeBodyRadii;
        double offset = ImpactParameterFor(safeRadius, body.Mu, MaxRelativeSpeed * ApproachSpeedFraction);

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

    public enum AutopilotAction { None, Approach, Insert }

    /// <summary>
    /// What the armed autopilot should do this instant. Insert once the window is open AND
    /// the ship is deep enough for a tide-proof parking orbit; otherwise, inside capture
    /// range, burn onto an approach fall when the ship is too fast for the window to ever
    /// open or the sun's tide has bent the fall off (closing speed under half the approach
    /// speed). Coasting nicely toward the window costs nothing.
    /// </summary>
    public static AutopilotAction AutopilotDecision(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, double hillRadius)
    {
        double distance = (ship.Position - bodyPosition).Length;
        if (WindowOpen(ship, bodyPosition, bodyVelocity, body, hillRadius)
            && distance < hillRadius * AutopilotInsertHillFraction)
        {
            return AutopilotAction.Insert;
        }

        if (distance > CaptureRange(hillRadius) || distance < body.BodyRadius * 4)
        {
            return AutopilotAction.None; // out of reach, or so low the swing must play out
        }

        double relSpeed = (ship.Velocity - bodyVelocity).Length;
        double approachSpeed = MaxRelativeSpeed * ApproachSpeedFraction;
        bool needsBurn = relSpeed >= MaxRelativeSpeed
            || ClosingSpeed(ship, bodyPosition, bodyVelocity) < approachSpeed * 0.5;
        return needsBurn ? AutopilotAction.Approach : AutopilotAction.None;
    }

    /// <summary>Window: inside the Hill sphere, under the speed limit, above the surface.</summary>
    public static bool WindowOpen(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, double hillRadius)
    {
        double distance = (ship.Position - bodyPosition).Length;
        double relSpeed = (ship.Velocity - bodyVelocity).Length;
        return distance < hillRadius && distance > body.BodyRadius * 2 && relSpeed < MaxRelativeSpeed;
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
