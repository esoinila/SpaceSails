using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: #708's black, laid down over everything the lamp missed (part of DeckView).
public sealed partial class DeckView
{
    // ── #708 · THE DARK, PAINTED ─────────────────────────────────────────────────────────────────────────
    //
    // The LampMask decides what gets DRAWN; this decides where it STOPS. A wall that starts under the
    // captain's boots and runs the width of the field passes the mask honestly — part of it is lit — and
    // without this it would be drawn all the way into the black. So once the world is down, everything
    // outside the light is painted over in Pitch.
    //
    // It is a fan of opaque wedges rather than one clever polygon-with-a-hole, because a canvas fill rule is
    // not a thing this renderer's command buffer carries and a hole punched with an even-odd winding is the
    // sort of drawing that works until somebody's browser disagrees. A wedge is four points and a fill.
    //
    // Each wedge runs from an INNER radius out past the farthest corner of the viewport. The inner radius is
    // the whole of the model, and it is two values: the lamp's reach where the cone points, the arm's-reach
    // ring everywhere else.

    /// <summary>How finely the fan is stepped. The inner arc's vertices are pushed out by 1/cos(step/2) so
    /// the chords lie ON the true radius at their midpoints and OUTSIDE it everywhere else — the black
    /// therefore never creeps inside the light, which would nibble the cone away from its own edge.</summary>
    private const double MaskStepRad = 4.0 * Math.PI / 180.0;

    private void PaintTheDark(int widthPx, int heightPx, in State state, float scale, float ox, float oy)
    {
        float ax = ox + ((float)state.AvatarX * scale);
        float ay = oy - ((float)state.AvatarY * scale);

        // Far enough to clear the corner of the canvas the captain is standing furthest from, whatever the
        // pan and whatever the follow-cam is doing.
        float far = 8f + Math.Max(
            Math.Max(Corner(0, 0), Corner(widthPx, 0)),
            Math.Max(Corner(0, heightPx), Corner(widthPx, heightPx)));

        float Corner(float cx, float cy) => MathF.Sqrt(((cx - ax) * (cx - ax)) + ((cy - ay) * (cy - ay)));

        // The captain's facing in SCREEN angle: deck +y is up, screen +y is down, so the sign flips.
        double axis = -state.HeadingRad;
        double half = SpaceSails.Core.SuitLamp.ConeHalfAngleDegrees * Math.PI / 180.0;
        float coneInner = (float)(SpaceSails.Core.SuitLamp.RangeDu * scale);
        float ringInner = (float)(LampRingDu * scale);

        // Two arcs, and they meet exactly on the cone's edges — so the edge itself is one straight radial
        // line, drawn to the pixel, which is the line the captain reads a wall appearing at.
        Fan(axis - half, axis + half, coneInner);                       // down the beam: black past the reach
        Fan(axis + half, axis - half + (2 * Math.PI), ringInner);       // everywhere else: black past the ring

        void Fan(double from, double to, float inner)
        {
            int steps = Math.Max(1, (int)Math.Ceiling((to - from) / MaskStepRad));
            double step = (to - from) / steps;
            float push = (float)(1.0 / Math.Cos(step / 2));
            float outer = far * push;

            for (int i = 0; i < steps; i++)
            {
                double a0 = from + (step * i);
                double a1 = a0 + step;
                float c0 = (float)Math.Cos(a0), s0 = (float)Math.Sin(a0);
                float c1 = (float)Math.Cos(a1), s1 = (float)Math.Sin(a1);
                float ri = inner * push;

                _scratch[0] = ax + (c0 * ri); _scratch[1] = ay + (s0 * ri);
                _scratch[2] = ax + (c1 * ri); _scratch[3] = ay + (s1 * ri);
                _scratch[4] = ax + (c1 * outer); _scratch[5] = ay + (s1 * outer);
                _scratch[6] = ax + (c0 * outer); _scratch[7] = ay + (s0 * outer);
                _renderer.DrawPolygon(_scratch.AsSpan(0, 8), Pitch, Pitch, 1f);
            }
        }
    }

    // #563 · How deep the dark reaches in from an unseen bound, in deck units, and how far that depth
    // wanders along it. A straight falloff is just the rectangle again; these two numbers are what stop the
    // eye finding a corner.
    private const double FalloffBaseDu = 7.0;
    private const double FalloffWanderDu = 5.0;

    /// <summary>Darken the ground approaching every <see cref="DeckPlan.Wall.Unseen"/> bound, with an
    /// irregular inner edge. No-op on any plan without unseen walls — i.e. every ship and station.</summary>
    private void DrawUnseenFalloff(DeckPlan plan, float scale, float ox, float oy)
    {
        // The bounds of the unseen set tell us which side of the field each one is, and therefore which way
        // "inward" points — a vertical wall at the smallest x faces right, and so on.
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        int unseen = 0;
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            if (!w.Unseen) { continue; }
            unseen++;
            minX = Math.Min(minX, Math.Min(w.X1, w.X2)); maxX = Math.Max(maxX, Math.Max(w.X1, w.X2));
            minY = Math.Min(minY, Math.Min(w.Y1, w.Y2)); maxY = Math.Max(maxY, Math.Max(w.Y1, w.Y2));
        }
        if (unseen == 0)
        {
            return;
        }

        const int Bands = 4;
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            if (!w.Unseen) { continue; }

            bool vertical = Math.Abs(w.X1 - w.X2) < 0.001f;
            double inward = vertical
                ? (Math.Abs(w.X1 - minX) < Math.Abs(w.X1 - maxX) ? 1.0 : -1.0)
                : (Math.Abs(w.Y1 - minY) < Math.Abs(w.Y1 - maxY) ? 1.0 : -1.0);

            double a0 = vertical ? Math.Min(w.Y1, w.Y2) : Math.Min(w.X1, w.X2);
            double a1 = vertical ? Math.Max(w.Y1, w.Y2) : Math.Max(w.X1, w.X2);
            const double step = 2.0;

            for (double a = a0; a < a1; a += step)
            {
                double span = Math.Min(step, a1 - a);
                double depth = FalloffBaseDu + (FalloffWanderDu * Wander(a, vertical ? w.X1 : w.Y1));

                for (int k = 0; k < Bands; k++)
                {
                    // Darkest against the bound, thinning inward. Near-black keyed to the floor's own blue
                    // so the dark reads as unlit ground rather than as a painted shape.
                    var ink = new RgbaColor(4, 6, 10, (byte)(205 - (k * 48)));
                    double d0 = depth * k / Bands, d1 = depth * (k + 1) / Bands;
                    double c0 = vertical ? w.X1 + (inward * d0) : w.Y1 + (inward * d0);
                    double c1 = vertical ? w.X1 + (inward * d1) : w.Y1 + (inward * d1);

                    (double bx0, double by0, double bx1, double by1) = vertical
                        ? (Math.Min(c0, c1), a, Math.Max(c0, c1), a + span)
                        : (a, Math.Min(c0, c1), a + span, Math.Max(c0, c1));

                    float sx0 = ox + ((float)bx0 * scale), sy0 = oy - ((float)by1 * scale);
                    float sx1 = ox + ((float)bx1 * scale), sy1 = oy - ((float)by0 * scale);
                    FillRect(sx0, sy0, sx1 - sx0, sy1 - sy0, ink);
                }
            }
        }
    }

    /// <summary>A stable 0..1 wander keyed to a world position — deterministic, so the dark edge is a fact
    /// about the place and does not shimmer as the camera moves or the frame ticks.</summary>
    private static double Wander(double along, double which)
    {
        // Low frequency ON PURPOSE. A high-frequency hash makes adjacent steps uncorrelated and the
        // dark edge reads as a jagged comb — obviously generated. This undulates over tens of deck
        // units, so the boundary wanders the way a shadow line does and no straight run is legible.
        double s = Math.Sin((along * 0.11) + (which * 0.037)) * 0.5;
        return s + 0.5;   // 0..1
    }
}
