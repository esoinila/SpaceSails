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
