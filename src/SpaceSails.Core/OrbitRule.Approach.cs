namespace SpaceSails.Core;

/// <summary>
/// #251 · PARKING AND APPROACHING — the park-stability verdict, the approach periapsis and closing speed,
/// the insertion burn and what it costs in pulses.
///
/// <para>Split out of <c>OrbitRule.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class OrbitRule
{
    /// <summary>Verdict on a bound orbit's long-term survival — the moon-grade stability check the
    /// manual Enter-orbit button lacked (#179/#180).</summary>
    public enum ParkStabilityVerdict
    {
        /// <summary>Bound, whole orbit inside the tide-stable band (floor … <see cref="StableParkCeiling"/>).</summary>
        Stable,

        /// <summary>Bound, but the apoapsis reaches into the tide-chaotic zone above the stable band —
        /// the sun's tide will pump and strip it over hours (Lab 16). The owner's ≈ 0.53-Hill park.</summary>
        TideRisk,

        /// <summary>Bound, but the periapsis dips below the surface floor — the orbit intersects the
        /// body; impact is coming.</summary>
        Subsurface,

        /// <summary>Not gravitationally captured (non-negative two-body energy about the body, or
        /// outside its Hill sphere).</summary>
        NotBound,
    }

    /// <summary>
    /// Classify a ship's orbit about <paramref name="body"/> from its two-body elements — the moon-grade
    /// stability verdict the manual orbit press and the degradation alert both read (#180). Energy and
    /// specific angular momentum give the semi-major axis and eccentricity, hence periapsis and apoapsis
    /// (same conic math as <see cref="TransferPlanner"/>'s Periapsis helper); the apses are then judged
    /// against the tide-stable band whose ceiling the Enceladus 10-day / ≈36-orbit drift sweep (Lab 16)
    /// mapped: robust to ≈ 0.33 Hill, chaotic near half-Hill.
    /// <list type="bullet">
    /// <item><see cref="ParkStabilityVerdict.NotBound"/> — energy ≥ 0 (hyperbolic/parabolic) or outside the Hill sphere.</item>
    /// <item><see cref="ParkStabilityVerdict.Subsurface"/> — periapsis below <see cref="SurfaceParkRadii"/>·R (checked first: impact is the most urgent failure).</item>
    /// <item><see cref="ParkStabilityVerdict.TideRisk"/> — apoapsis above <see cref="StableParkCeiling"/> (into the chaotic outer band).</item>
    /// <item><see cref="ParkStabilityVerdict.Stable"/> — the whole orbit sits inside the stable band.</item>
    /// </list>
    /// </summary>
    public static ParkStabilityVerdict ParkStability(
        ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body, double hillRadius)
    {
        Vector2d r = ship.Position - bodyPosition;
        double radius = r.Length;
        if (!(radius > 0) || !(hillRadius > 0) || !(body.Mu > 0) || radius >= hillRadius)
        {
            return ParkStabilityVerdict.NotBound;
        }

        Vector2d v = ship.Velocity - bodyVelocity;
        double mu = body.Mu;
        double energy = v.LengthSquared / 2 - mu / radius;
        if (energy >= 0)
        {
            return ParkStabilityVerdict.NotBound; // unbound: no periapsis/apoapsis to speak of
        }

        double h = r.X * v.Y - r.Y * v.X;                       // specific angular momentum
        double a = -mu / (2 * energy);                          // semi-major axis (>0 when bound)
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (mu * mu)));
        double periapsis = a * (1 - e);
        double apoapsis = a * (1 + e);

        // Impact first — the most urgent failure. Then the tide-chaotic outer band.
        if (periapsis < SurfaceParkRadii * body.BodyRadius)
        {
            return ParkStabilityVerdict.Subsurface;
        }

        if (apoapsis > StableParkCeiling(body, hillRadius))
        {
            return ParkStabilityVerdict.TideRisk;
        }

        return ParkStabilityVerdict.Stable;
    }

    /// <summary>The periapsis the safe approach aims for — the closest the ballistic fall comes to
    /// the target's centre. The established big-body aim is 2·R (<see cref="ApproachSafeBodyRadii"/>);
    /// for a deep well it is bent DOWN to the middle of the insert band (surface floor …
    /// <see cref="ParkingRadius"/>) so the fall dwells inside the window and the tick loop catches
    /// the insertion. Never below the surface margin. With no Hill context (hillRadius ≤ 0) it is
    /// the plain 2·R aim, preserving pre-#136 behaviour for callers that pass no Hill radius (the
    /// big-body unit tests and Titan e2e).</summary>
    public static double ApproachPeriapsis(CelestialBody body, double hillRadius)
    {
        double byBody = ApproachSafeBodyRadii * body.BodyRadius;
        if (hillRadius <= 0)
        {
            return byBody;
        }
        double floor = SurfaceParkRadii * body.BodyRadius;
        double bandMid = floor + InsertBandMidpoint * (ParkingRadius(body, hillRadius) - floor);
        return Math.Clamp(byBody, Math.Min(floor, bandMid), bandMid);
    }

    /// <summary>The closing speed the safe approach flies. Global <see cref="MaxRelativeSpeed"/>·
    /// <see cref="ApproachSpeedFraction"/> (4 km/s) for a roomy moon or planet, but capped at
    /// <see cref="ApproachCircularSpeedFactor"/>× the parking circular speed for a deep well — a
    /// tiny moon can't be captured at 4 km/s, and slowing the terminal fall keeps the ship inside
    /// the thin stable shell between ticks. With no Hill context (hillRadius ≤ 0) it is the flat
    /// global speed, preserving pre-#136 behaviour for callers that pass no Hill radius.</summary>
    public static double ApproachClosingSpeed(CelestialBody body, double hillRadius)
    {
        double global = MaxRelativeSpeed * ApproachSpeedFraction;
        if (hillRadius <= 0)
        {
            return global;
        }
        double capped = ApproachCircularSpeedFactor * CircularSpeed(body, ParkingRadius(body, hillRadius));
        return Math.Min(global, capped);
    }

    /// <summary>The Δv the insertion burn must perform from the current state.</summary>
    public static double InsertionDeltaV(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body)
    {
        Vector2d target = CircularVelocity(ship, bodyPosition, bodyVelocity, body);
        return (target - ship.Velocity).Length;
    }

    /// <summary>Mass-pulse cost of the insertion from the current state (at least 1).</summary>
    public static int PulseCost(ShipState ship, Vector2d bodyPosition, Vector2d bodyVelocity, CelestialBody body) =>
        PulsesFor(InsertionDeltaV(ship, bodyPosition, bodyVelocity, body), ship.Velocity.Length);

    /// <summary>Mass-pulse price of an arbitrary assisted burn: one pulse buys
    /// <see cref="DeltaVPerPulseFraction"/> of the current heliocentric speed as Δv (floor 1 m/s),
    /// rounded up, at least 1. Public since #146 so the transfer planner quotes with the SAME
    /// kernel the live approach/insertion burns spend with — one pricing source, no drift.</summary>
    public static int PulsesFor(double deltaV, double currentSpeed)
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
}
