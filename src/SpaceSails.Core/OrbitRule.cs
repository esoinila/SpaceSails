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

    /// <summary>Surface-margin floor for any parked/inserted orbit: never circularize below this
    /// many body radii. Enceladus is airless and its Hill sphere is only ≈ 3.8 R, so its
    /// robustly-stable park depth (≈ 0.33 Hill ≈ 1.24 R) sits below the old 1.5 R guideline — the
    /// tidal-stability constraint wins for a deep well, and 1.1 R still clears the surface with
    /// margin. Roomy moons and planets park far above this and never feel it.</summary>
    public const double SurfaceParkRadii = 1.1;

    /// <summary>Where the autopilot circularizes — the insertion gate — as a fraction of the Hill
    /// sphere. Empirically (the Enceladus 10-day / ≈36-orbit drift sweep) prograde orbits hold
    /// robustly to ≈ 0.33 Hill; nearer half-Hill they are chaotic and strip over many orbits. The
    /// old 0.5-Hill "stable to roughly half Hill" was only ever tested over Earth's ⅛-orbit — this
    /// is the depth that actually holds. Big bodies complete few orbits in 10 days, so this deeper,
    /// safer park is inert for them beyond a slightly deeper insertion.</summary>
    public const double ParkStableHillFraction = 0.33;

    /// <summary>The safe-approach periapsis for a deep well is aimed at the MIDDLE of the insert
    /// band (surface floor … park radius), so the ballistic fall dwells inside the window near
    /// periapsis and the tick loop reliably catches the insertion instead of blasting through a
    /// thin shell. Inert for big bodies, whose 2 R aim already sits far below the band.</summary>
    public const double InsertBandMidpoint = 0.5;

    /// <summary>A deep well is captured no faster than this many times its parking circular speed:
    /// screaming in at the global 4 km/s would need a monster insertion burn and skip the thin
    /// stable shell between ticks. For a roomy moon or planet the global approach speed is already
    /// far slower than this cap, so it is inert (issue #136).</summary>
    public const double ApproachCircularSpeedFactor = 8.0;

    /// <summary>Parked orbits never sit outside this fraction of the Hill sphere — the sun's tide
    /// strips the outer Hill. The park target is capped here.</summary>
    public const double ParkCeilingHillFraction = 0.9;

    /// <summary>Hill-sphere radius: where the body's gravity owns a satellite against its parent's tide.
    /// Uses the body's <see cref="CelestialBody.OrbitRadius"/> (semi-major axis) — the stable, mean
    /// value. For an eccentric body whose Hill sphere breathes with distance, prefer the
    /// instantaneous overload fed by <see cref="ICelestialEphemeris.InstantaneousOrbitRadius"/>.</summary>
    public static double HillRadius(CelestialBody body, double parentMu) =>
        HillRadius(body.OrbitRadius, body.Mu, parentMu);

    /// <summary>Hill-sphere radius from an explicit parent distance — pass the instantaneous
    /// distance (PR-B, Kepler rails) so an elliptical body's capture window tracks its real, changing
    /// separation instead of a fixed circle. Identical to the mean overload for a circular body.</summary>
    public static double HillRadius(double orbitRadius, double bodyMu, double parentMu) =>
        orbitRadius * Math.Pow(bodyMu / (3 * parentMu), 1.0 / 3.0);

    /// <summary>Circular-orbit speed around the body at the given distance.</summary>
    public static double CircularSpeed(CelestialBody body, double distance) =>
        Math.Sqrt(body.Mu / distance);

    /// <summary>The radius the autopilot parks/circularizes at — the insertion gate (issue #136).
    /// Robustly tide-stable (≈ 0.33 Hill, <see cref="ParkStableHillFraction"/>), a clear margin above
    /// the surface (≥ <see cref="SurfaceParkRadii"/>·R), never in the tide-stripped outer Hill
    /// (≤ 0.9 Hill). For a roomy moon or planet this is the flat 0.33·Hill; for a deep-well moon
    /// whose 0.33·Hill would collide with the surface it is clamped up off the body.</summary>
    public static double ParkingRadius(CelestialBody body, double hillRadius)
    {
        double floor = SurfaceParkRadii * body.BodyRadius;
        double ceiling = ParkCeilingHillFraction * hillRadius;
        double target = Math.Max(ParkStableHillFraction * hillRadius, floor);
        return ceiling <= floor ? floor : Math.Min(target, ceiling);
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
            && distance < ParkingRadius(body, hillRadius))
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
