namespace SpaceSails.Core;

/// <summary>
/// #933 · WHICH WAY SHE IS GOING. The little arrowhead the map hangs off the ship marker.
///
/// <para>Owner, 2026-08-17 (playing the flight side): <i>"our ship shape on the map could indicate little
/// better about where it is going if it was depicted as arrow like triangle … having a small triangle
/// indicator would also serve to show the pointed at direction when planning a burn. So not necessarily ship
/// shape change... more like add shape that points to the direction the ship is going even when its motion is
/// stopped during the burn parameter selection."</i> — and, to the shape below: <i>"nice love it … let's do
/// it."</i></para>
///
/// <h3>Two shapes, and they answer two different questions</h3>
/// <para>This is GOING. The node's nose vector (#916/#838's <see cref="NodeFrame"/>) is PUSHING. Both are on
/// screen while a burn is being planned, and neither is allowed to stand in for the other: an arrowhead that
/// swung to the burn's aim would tell the captain he was already flying the course he has not burned yet.</para>
///
/// <h3>The frame is the frame the plan is being READ in</h3>
/// <para>The direction handed in here is the ship's velocity <b>relative to the plot frame</b> (#135/#926 —
/// the same <c>v helio</c> / <c>v rel {body}</c> the plotting panel's speed readout names, computed once and
/// read twice). That is the whole teaching value of the shape: switch the frame and the arrow swings, or
/// collapses to a ring, because "moving" was never a property of the ship alone. Drawing the heliocentric
/// velocity under Earth's frame would be this repo's third named bug class — the drawn shape reporting one
/// thing while the sim and the label next to it say another.</para>
///
/// <h3>Fixed pixels, never metres</h3>
/// <para>Every number here is SCREEN PIXELS and every point that comes out is a screen point. The map spans
/// thirteen orders of magnitude of zoom (<c>Camera.MetersPerPixel</c> 1e-2 → 1e11); an arrowhead sized in
/// world metres would be a hair at one end of that and would swallow the solar system at the other. It is a
/// glyph, like the ship dot and like her mast (Lab 43): the same size at every zoom.</para>
///
/// <h3>Angles are SCREEN angles</h3>
/// <para><c>dirRad</c> is measured on the canvas — 0 = +X (right), increasing CLOCKWISE on screen, because
/// canvas Y points down while world Y points up (<c>Camera.WorldToScreen</c> flips it). A caller holding a
/// world-space velocity converts once, with <c>Math.Atan2(-v.Y, v.X)</c>, and the flip lives at that one
/// call. This differs on purpose from <see cref="NodeFrame"/>'s WORLD-space degrees: that is an angle the
/// integrator burns along, this is an angle a pen draws at.</para>
/// </summary>
public static class VelocityArrow
{
    /// <summary>The full angle at the arrowhead's tip, in degrees. Thirty is a dart: narrow enough to read as
    /// a DIRECTION at a glance rather than as a blob, wide enough that a couple of pixels of it survive on a
    /// crowded map. One number, so the two base corners can never be cut from different triangles.</summary>
    public const double ApexDegrees = 30.0;

    /// <summary>How far the tip stands ahead of the ship's centre, in screen pixels. Comfortably clear of the
    /// 4 px ship dot so the point is outside her hull, and short enough that it never reads as the barrel
    /// line (which is 12 px and carries the hull's facing, not her course).</summary>
    public const float LengthPx = 13f;

    /// <summary>How far the BASE sits behind the ship's centre, in screen pixels — "base just behind the ship
    /// dot" (the dot's radius is 4 px). The triangle therefore straddles the marker and reads as a flight
    /// through it, not as a separate object floating off her bow.</summary>
    public const float BaseBehindPx = 5f;

    /// <summary>
    /// Below this relative speed the arrow is not drawn at all — a ring is (see <see cref="ShowsRing"/>).
    ///
    /// <para>One metre per second, and the number is chosen against what the map is FOR. Orbital work on this
    /// chart is kilometres per second; a ship clamped to a dock, or one co-moving with the body whose frame
    /// the plan is being read in, sits at metres per second or less, and at that point the DIRECTION of the
    /// residual is numerical noise off a finite-differenced ephemeris — an arrow drawn from it would spin
    /// like a compass on a magnet and would be a lie about the one thing the shape exists to say. A ring says
    /// the honest thing instead: in THIS frame, she is not going anywhere.</para>
    /// </summary>
    public const double RingBelowMps = 1.0;

    /// <summary>The ring's radius in screen pixels when the arrow collapses. Sits just outside the 4 px ship
    /// dot: a collar, not a halo — the halo shape belongs to nothing on this map (Lab 43 took the discharge
    /// off it for good reasons of its own).</summary>
    public const float RingRadiusPx = 7f;

    /// <summary>Whether this relative speed gets the ring instead of the arrowhead. The one place the
    /// threshold is applied, so the picture and any sentence about it can never disagree.</summary>
    public static bool ShowsRing(double relativeSpeedMps) =>
        !(relativeSpeedMps >= RingBelowMps);   // written so NaN falls to the ring, never to a spinning dart

    /// <summary>
    /// The three screen points of the arrowhead — apex first, then the two base corners — written into
    /// <paramref name="into"/> as x,y,x,y,x,y.
    ///
    /// <para>Pure: same six floats for the same three arguments, no state, no camera. <paramref name="x"/> /
    /// <paramref name="y"/> are the ship marker's own screen pixel; <paramref name="dirRad"/> is the SCREEN
    /// angle of her frame-relative velocity (see the class note on the Y flip).</para>
    ///
    /// <para>The base corners are taken at the triangle's full height — <see cref="LengthPx"/> +
    /// <see cref="BaseBehindPx"/> back from the tip — so the angle AT THE TIP really is
    /// <see cref="ApexDegrees"/>, whatever the two lengths are set to. Half of it each side of the axis:
    /// half-width = height × tan(apex/2).</para>
    /// </summary>
    /// <param name="into">A span of at least six floats. Shorter and this throws — a half-written triangle
    /// would draw as a shape nobody designed.</param>
    public static void Points(double dirRad, float x, float y, Span<float> into)
    {
        if (into.Length < 6)
        {
            throw new ArgumentException("an arrowhead is three points — six floats.", nameof(into));
        }

        double ax = Math.Cos(dirRad);
        double ay = Math.Sin(dirRad);
        double height = LengthPx + BaseBehindPx;
        double halfWidth = height * Math.Tan(ApexDegrees * 0.5 * Math.PI / 180.0);

        // The base's midpoint, BaseBehindPx behind the marker, and the unit perpendicular to the axis.
        double bx = x - (ax * BaseBehindPx);
        double by = y - (ay * BaseBehindPx);

        into[0] = (float)(x + (ax * LengthPx));          // the tip
        into[1] = (float)(y + (ay * LengthPx));
        into[2] = (float)(bx - (ay * halfWidth));        // one base corner
        into[3] = (float)(by + (ax * halfWidth));
        into[4] = (float)(bx + (ay * halfWidth));        // …and the other
        into[5] = (float)(by - (ax * halfWidth));
    }
}
