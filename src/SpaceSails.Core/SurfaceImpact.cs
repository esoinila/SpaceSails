namespace SpaceSails.Core;

/// <summary>
/// #264 — the impact enforcer. Lab 16 warns "periapsis under the surface — impact coming", but nothing
/// ever made the impact ARRIVE: the point-mass integrator flies the ship straight through a planet's
/// interior (inside <see cref="CelestialBody.BodyRadius"/> the force is clamped off in
/// <see cref="Simulator.GravitationalAcceleration"/>, so the ship coasts a straight chord and exits with
/// the wrong velocity direction — the "eight-petaled flower" of precessing apsides the owner drew at
/// Uranus was that numerical artifact bleeding energy, not aerobraking). This finds the moment a
/// LIVE-FLOWN step first touches a body's surface radius so the caller can end the flight THERE — never
/// integrating the interior at all, which is what kills both the free capture and the fake energy loss.
///
/// <para>Step-size-robust by construction. Across one step the ship moves from <c>from</c> to <c>to</c>
/// and the body moves along its rails; both are treated as linear in the step fraction, so the RELATIVE
/// position is linear (<c>rel(s) = a + s·b</c>) and the closest approach is the vertex of a quadratic.
/// A whole small moon can pass between a coarse step's endpoints — a 60 s NPC step at km/s tunnels one —
/// and the min-distance root test still catches it, where an endpoint-only "is it inside now?" check
/// would sail clean through. #263's aerocapture lab is the FUTURE sanctioned exception: a body with a
/// modelled atmosphere corridor will one day be survivable below the cloud tops; see the seam comment
/// at the crossing test.</para>
/// </summary>
public static class SurfaceImpact
{
    /// <summary>The body struck, the step fraction [0,1] at contact, and the sim-time and position there.</summary>
    public readonly record struct Crossing(
        string BodyId, string BodyName, double Fraction, double SimTime, Vector2d Position);

    /// <summary>
    /// The earliest surface crossing of the segment <paramref name="from"/>→<paramref name="to"/> (over
    /// sim-times <paramref name="fromTime"/>→<paramref name="toTime"/>) against any body's
    /// <see cref="CelestialBody.BodyRadius"/>, or null when the step stays clear of every body. Bodies on
    /// rails move across the step too; their position is interpolated linearly between the step's ends —
    /// exact enough for a collision test and it keeps the crossing a closed-form quadratic root. A body
    /// with zero radius (a mass-less station haven) can't be struck and is skipped, so havens on rails
    /// are exempt (#264); the caller likewise skips a docked ship (it never integrates).
    /// </summary>
    public static Crossing? FirstCrossing(
        Vector2d from, double fromTime, Vector2d to, double toTime, ICelestialEphemeris ephemeris)
    {
        double dt = toTime - fromTime;
        Crossing? earliest = null;

        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.BodyRadius <= 0.0)
            {
                continue; // a mass-less/point station haven has no surface to strike (#264 exemption)
            }

            // #263 seam: once the aerocapture lab lands, an atmosphere-bearing body gets a modelled
            // survivable corridor here — a deep-but-above-the-mantle pass would be handed to the drag
            // model instead of counted as a strike. Today every surface is fatal; nothing to add yet.
            Vector2d bodyFrom = ephemeris.Position(body.Id, fromTime);
            Vector2d bodyTo = ephemeris.Position(body.Id, toTime);

            // Relative offset across the step is linear in s ∈ [0,1]: rel(s) = a + s·b.
            Vector2d a = from - bodyFrom;
            Vector2d b = (to - from) - (bodyTo - bodyFrom);

            if (FirstInsideFraction(a, b, body.BodyRadius) is not { } frac)
            {
                continue;
            }

            if (earliest is null || frac < earliest.Value.Fraction)
            {
                double t = fromTime + frac * dt;
                Vector2d pos = from + (to - from) * frac;
                earliest = new Crossing(body.Id, body.Name, frac, t, pos);
            }
        }

        return earliest;
    }

    /// <summary>
    /// The smallest step fraction s ∈ [0,1] at which the moving point <c>a + s·b</c> first enters the
    /// disc of radius <paramref name="r"/> about the origin, or null when the segment never reaches it.
    /// Solves <c>|a|² + 2(a·b)s + |b|²s² = r²</c> for the entering (smaller) root. A step that begins
    /// already inside returns 0 — immediate contact.
    /// </summary>
    public static double? FirstInsideFraction(Vector2d a, Vector2d b, double r)
    {
        double rr = r * r;
        double aa = a.LengthSquared;
        if (aa <= rr)
        {
            return 0.0; // the step starts on/under the surface — contact is immediate
        }

        double bb = b.LengthSquared;
        if (bb == 0.0)
        {
            return null; // no motion relative to the body, and it started clear
        }

        double ab = a.Dot(b);
        double disc = ab * ab - bb * (aa - rr);
        if (disc < 0.0)
        {
            return null; // the line never reaches the surface
        }

        // Entering root is the smaller of the two (bb > 0). If it lands in the step it is the crossing;
        // otherwise the whole in-disc arc lies outside [0,1] (the pass happens before or after this step).
        double entering = (-ab - Math.Sqrt(disc)) / bb;
        return entering is >= 0.0 and <= 1.0 ? entering : null;
    }
}
