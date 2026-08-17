namespace SpaceSails.Core;

/// <summary>
/// The four directions a planned burn is ever pointed in, named in the words the owner flies by
/// (#838): forward, back, up, down — with respect to the TRAJECTORY at the node, not the compass
/// and not the ship's live nose.
/// </summary>
public enum NodeDirection
{
    /// <summary>Prograde — along the course the ghost is already flying at that instant.</summary>
    Forward,

    /// <summary>Retrograde — straight back against it.</summary>
    Back,

    /// <summary>Radial out — the quarter turn off the course that points AWAY from the primary.</summary>
    Up,

    /// <summary>Radial in — the quarter turn that points TOWARD the primary.</summary>
    Down,
}

/// <summary>
/// #838 · THE GHOST'S FRAME AT THE NODE. The trajectory-relative frame a maneuver node is planned in,
/// and the plain words the four quick selects wear.
///
/// <para>Owner, at the end of a playtest (2026-08-11): <i>"The actual scrub flying should probably only
/// use the vector view but have like quick selects for forward, backward, up and down directions. I think
/// the plus- and minus flying is something that works for reflex flying but our planning sailship flying
/// almost always use the vector view. I almost always use those four directions in respect to our
/// trajectory... the intermediate angles are the exception. Also that panel could be bigger to fit all the
/// buttons properly."</i></para>
///
/// <para>OWNER RULING (2026-08-17), option A: the vector view is the ONLY planning surface at the scrub.
/// The plus/minus idiom leaves the maneuver-node planner and stays in reflex flying (the live
/// <c>+</c>/<c>−</c>/arrow keys that scale the ship's velocity right now). Four trajectory-relative quick
/// selects replace it, computed against the trajectory AT THE SCRUBBED NODE — the ghost's frame — never
/// against the ship's live heading. Free aim in the vector view remains, as the exception.</para>
///
/// <h3>Why the frame has to be the ghost's</h3>
/// <para>The planner plans the FUTURE. Forty hours down a curved course the ship is pointed somewhere else
/// entirely, and "forward" read off the live nose would aim the burn at a direction the ghost has not flown
/// since. The two frames differ exactly when planning matters most — a burn at apoapsis, an insertion, a
/// brake at a pass — so every function here takes the ghost's own position and velocity AT THE NODE'S
/// EPOCH and nothing else. There is no cache: re-time the node and the answer must be re-solved from the
/// re-projected ribbon.</para>
///
/// <h3>The angle convention</h3>
/// <para>Every heading returned here is WORLD-SPACE degrees in <c>[0, 360)</c> — 0° = +X, increasing
/// counter-clockwise — which is exactly what <see cref="ManeuverNode.HeadingDegrees"/> stores and what the
/// integrator burns along. #201's <see cref="BurnHeadingConvention"/> translates only the panel's typed
/// input (ship-relative: 0 ahead, +90 starboard) and its echo; it is a display layer over this same
/// world angle, so a quick select and a typed angle land in the same units.</para>
///
/// <h3>Up and down are quarter turns, and the sign is chosen by geometry</h3>
/// <para><see cref="NodeDirection.Up"/> is defined as the quarter turn off prograde — never the raw
/// geometric outward vector — so the four directions are always exactly 90° apart and the panel can never
/// offer two buttons that mean nearly the same thing. Which quarter turn is "up" is then decided by the
/// primary: the one whose dot product with (ghost − primary) is positive, i.e. the one that climbs away
/// from the body. On a circular orbit that IS the outward radial; on an eccentric arc it is the standard
/// radial-out (perpendicular to velocity, on the far side from the body) rather than the line to the body,
/// which is what a pilot means by "lift the orbit".</para>
/// </summary>
public static class NodeFrame
{
    /// <summary>The four buttons the planner offers, in the owner's reading order.</summary>
    public static readonly IReadOnlyList<NodeDirection> QuickSelects =
        [NodeDirection.Forward, NodeDirection.Back, NodeDirection.Up, NodeDirection.Down];

    /// <summary>Prograde: the world heading of <paramref name="velocity"/>. A dead-stop ghost has no
    /// forward — the frame degenerates to the world axes and this returns 0.</summary>
    public static double Prograde(Vector2d velocity) =>
        velocity.LengthSquared == 0 ? 0 : Wrap360(Math.Atan2(velocity.Y, velocity.X) * 180.0 / Math.PI);

    /// <summary>Retrograde: prograde reversed, exactly half a turn.</summary>
    public static double Retrograde(Vector2d velocity) => Wrap360(Prograde(velocity) + 180);

    /// <summary>Radial out ("up"): the quarter turn off prograde that climbs away from
    /// <paramref name="primaryPosition"/>. When the course points straight at or straight away from the
    /// primary (or the ghost sits on it) neither quarter turn is more outward than the other — the
    /// counter-clockwise one is chosen, deterministically.</summary>
    public static double RadialOut(Vector2d velocity, Vector2d position, Vector2d primaryPosition)
    {
        double ccw = Wrap360(Prograde(velocity) + 90);
        Vector2d out_ = position - primaryPosition;
        double rad = ccw * Math.PI / 180.0;
        var quarter = new Vector2d(Math.Cos(rad), Math.Sin(rad));
        return quarter.Dot(out_) >= 0 ? ccw : Wrap360(ccw + 180);
    }

    /// <summary>Radial in ("down"): the other quarter turn — the one that drops toward the primary.</summary>
    public static double RadialIn(Vector2d velocity, Vector2d position, Vector2d primaryPosition) =>
        Wrap360(RadialOut(velocity, position, primaryPosition) + 180);

    /// <summary>The world heading for one quick select, from the ghost's own state at the node.</summary>
    public static double Heading(NodeDirection direction, Vector2d velocity, Vector2d position, Vector2d primaryPosition) =>
        direction switch
        {
            NodeDirection.Forward => Prograde(velocity),
            NodeDirection.Back => Retrograde(velocity),
            NodeDirection.Up => RadialOut(velocity, position, primaryPosition),
            _ => RadialIn(velocity, position, primaryPosition),
        };

    /// <summary>
    /// THE ONE FUNCTION THE PLANNER CALLS. The heading a quick select means at <paramref name="simTime"/>,
    /// read off the projected ribbon <paramref name="samples"/> — the ghost's interpolated position and its
    /// finite-differenced velocity at that epoch — against the primary's position at that same epoch.
    /// Outside the projected horizon the fallbacks (the live ship) stand in, which is the only case where
    /// the live state is ever consulted.
    /// </summary>
    public static double HeadingAt(
        NodeDirection direction,
        IReadOnlyList<TrajectorySample> samples,
        double simTime,
        Vector2d primaryPosition,
        Vector2d fallbackPosition,
        Vector2d fallbackVelocity) =>
        Heading(direction,
                VelocityAt(samples, simTime, fallbackVelocity),
                PositionAt(samples, simTime, fallbackPosition),
                primaryPosition);

    /// <summary>Position on a projected ribbon at a sim time, linearly interpolated between the two
    /// bracketing samples and clamped to the endpoints outside the horizon. Empty ribbon → the
    /// fallback.</summary>
    public static Vector2d PositionAt(IReadOnlyList<TrajectorySample> samples, double simTime, Vector2d fallback)
    {
        if (samples.Count == 0) return fallback;
        if (simTime <= samples[0].SimTime) return samples[0].Position;
        if (simTime >= samples[^1].SimTime) return samples[^1].Position;

        int lo = 0, hi = samples.Count - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) >> 1;
            if (samples[mid].SimTime <= simTime) lo = mid; else hi = mid;
        }

        TrajectorySample a = samples[lo];
        TrajectorySample b = samples[hi];
        double span = b.SimTime - a.SimTime;
        double f = span > 0 ? (simTime - a.SimTime) / span : 0;
        return a.Position + (b.Position - a.Position) * f;
    }

    /// <summary>Velocity on a projected ribbon at a sim time: the secant of the bracketing sample pair.
    /// Past the end of the ribbon (or on a ribbon too short to have a pair) the fallback stands in.</summary>
    public static Vector2d VelocityAt(IReadOnlyList<TrajectorySample> samples, double simTime, Vector2d fallback)
    {
        for (int i = 1; i < samples.Count; i++)
        {
            if (samples[i].SimTime >= simTime)
            {
                double dt = samples[i].SimTime - samples[i - 1].SimTime;
                return dt <= 0 ? fallback : (samples[i].Position - samples[i - 1].Position) / dt;
            }
        }
        return fallback;
    }

    /// <summary>The button's face. Glyph plus one plain word — the four the owner names, no ± anywhere:
    /// that idiom belongs to reflex flying now.</summary>
    public static string Label(NodeDirection direction) => direction switch
    {
        NodeDirection.Forward => "▶ FORWARD",
        NodeDirection.Back => "◀ BACK",
        NodeDirection.Up => "▲ UP",
        _ => "▼ DOWN",
    };

    /// <summary>The button's tooltip: what the direction DOES to the orbit, with the body the radials are
    /// measured against named out loud so "up" is never ambiguous.</summary>
    public static string Hint(NodeDirection direction, string primaryName) => direction switch
    {
        NodeDirection.Forward => "With the trajectory at this node — push the orbit along",
        NodeDirection.Back => "Against the trajectory at this node — hold the orbit back",
        NodeDirection.Up => $"Away from {primaryName} — lift the orbit",
        _ => $"Toward {primaryName} — drop the orbit",
    };

    /// <summary>The line under the four buttons: whose frame this is. States the law on the panel — the
    /// aim is taken at the NODE, and up/down are measured from a named body.</summary>
    public static string FrameNote(string primaryName) =>
        $"Aimed off the course at this node · up/down from {primaryName}";

    /// <summary>Whether a stored heading still points along a quick select's direction, within
    /// <paramref name="toleranceDegrees"/>. Used to light the button that matches — and to let it go dark
    /// the moment the node is re-timed and the ghost's frame has moved under it.</summary>
    public static bool PointsAlong(double headingDegrees, double directionDegrees, double toleranceDegrees = 0.5) =>
        Math.Abs(Wrap180(headingDegrees - directionDegrees)) <= toleranceDegrees;

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
