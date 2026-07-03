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
    public static int PulseCost(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body)
    {
        double unit = Math.Max(1.0, ship.Velocity.Length * DeltaVPerPulseFraction);
        return Math.Max(1, (int)Math.Ceiling(InsertionDeltaV(ship, bodyPosition, bodyVelocity, body) / unit));
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
