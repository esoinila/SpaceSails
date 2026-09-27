namespace SpaceSails.Core;

/// <summary>
/// #251 · THE FORCES — the dynamical time near a body, gravity, atmospheric drag and its stable form, and
/// a body's velocity.
///
/// <para>Split out of <c>Simulator.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field.</para>
/// </summary>
public sealed partial class Simulator
{
    /// <summary>
    /// Shortest orbital timescale <c>sqrt(d³/μ)</c> over all attracting bodies at this position —
    /// small next to a heavy body, huge in deep space. Governs the adaptive projection step.
    /// </summary>
    public double DynamicalTime(Vector2d position, double simTime)
    {
        double shortest = double.MaxValue;

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.Mu == 0)
            {
                continue;
            }

            double distance = (_ephemeris.Position(body.Id, simTime) - position).Length;
            double tau = Math.Sqrt(distance * distance * distance / body.Mu);
            if (tau < shortest)
            {
                shortest = tau;
            }
        }

        return shortest;
    }

    /// <summary>Sum of point-mass gravity from every body with nonzero Mu.</summary>
    public Vector2d GravitationalAcceleration(Vector2d position, double simTime)
    {
        Vector2d acceleration = Vector2d.Zero;

        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.Mu == 0)
            {
                continue;
            }

            Vector2d toBody = _ephemeris.Position(body.Id, simTime) - position;
            double distanceSquared = toBody.LengthSquared;

            // Inside the physical body the point-mass model is meaningless (and singular);
            // collision handling is a later milestone, so just clamp the force off.
            if (distanceSquared < body.BodyRadius * body.BodyRadius || distanceSquared == 0)
            {
                continue;
            }

            double distance = Math.Sqrt(distanceSquared);
            acceleration += toBody * (body.Mu / (distanceSquared * distance));
        }

        return acceleration;
    }

    /// <summary>
    /// Total atmospheric drag acceleration at a ship state (m/s^2), summed over every body whose
    /// shell the ship is currently inside. <c>a = −0.5·ρ(h)·|v_rel|·v_rel / BC</c>, where ρ(h) is the
    /// body's exponential density at the ship's altitude, v_rel is the ship's velocity minus the
    /// body's rail velocity (the shell translates with the body; its spin is ignored), and BC is
    /// <see cref="BallisticCoefficient"/>. Returns <see cref="Vector2d.Zero"/> outside every shell.
    /// </summary>
    public Vector2d DragAcceleration(Vector2d position, Vector2d velocity, double simTime)
    {
        Vector2d drag = Vector2d.Zero;

        foreach (CelestialBody body in _atmosphereBodies)
        {
            Vector2d bodyPosition = _ephemeris.Position(body.Id, simTime);
            double altitude = (position - bodyPosition).Length - body.BodyRadius;
            double density = body.Atmosphere!.DensityAt(altitude);
            if (density <= 0.0)
            {
                continue;
            }

            Vector2d vRel = velocity - BodyVelocity(body.Id, simTime);
            double speed = vRel.Length;
            if (speed == 0.0)
            {
                continue;
            }

            drag += vRel * (-0.5 * density * speed / BallisticCoefficient);
        }

        return drag;
    }

    /// <summary>
    /// Semi-implicit quadratic-drag update (issue #153): return the ship's velocity after one step of
    /// atmospheric drag applied stably, summed over every shell the ship is inside. For each body the
    /// relative velocity relaxes by <c>v_rel ← v_rel / (1 + c·|v_rel|·dt)</c> with <c>c = 0.5·ρ(h)/BC</c>
    /// — the exact integral of <c>dv/dt = −c·|v_rel|·v_rel</c> holding the speed factor over the step.
    /// It is unconditionally stable (the factor is in (0, 1], so drag never adds energy or reverses the
    /// flow at any dt) and matches the old explicit <c>v += a_drag·dt</c> to first order in dt. Returns
    /// <paramref name="velocity"/> untouched outside every shell, keeping the vacuum path exact.
    /// </summary>
    public Vector2d ApplyStableDrag(Vector2d position, Vector2d velocity, double simTime, double dt)
    {
        foreach (CelestialBody body in _atmosphereBodies)
        {
            Vector2d bodyPosition = _ephemeris.Position(body.Id, simTime);
            double altitude = (position - bodyPosition).Length - body.BodyRadius;
            double density = body.Atmosphere!.DensityAt(altitude);
            if (density <= 0.0)
            {
                continue;
            }

            Vector2d vRel = velocity - BodyVelocity(body.Id, simTime);
            double speed = vRel.Length;
            if (speed == 0.0)
            {
                continue;
            }

            double c = 0.5 * density / BallisticCoefficient;
            Vector2d vRelAfter = vRel / (1.0 + c * speed * dt);
            velocity += vRelAfter - vRel;
        }

        return velocity;
    }

    // Rail velocity of a body by central finite difference of its analytic position — deterministic
    // (same pure Position function on both sides) and only ever evaluated for a body whose shell the
    // ship is actually inside, so it never touches the vacuum hot path.
    private Vector2d BodyVelocity(string bodyId, double simTime) =>
        (_ephemeris.Position(bodyId, simTime + 1.0) - _ephemeris.Position(bodyId, simTime - 1.0)) / 2.0;
}
