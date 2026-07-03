using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// The view from inside the ship (M13): a column raycaster over <see cref="DeckPlan"/> built
/// entirely from the renderer's primitives — every wall strip is a two-point polyline. Window
/// walls open onto REAL space: the sun, planets and stars are computed from the actual
/// ephemeris relative to the ship's position, so the sun blazes bigger the closer you fly,
/// and the sky slews as your ship's heading (velocity) changes. Droid pirate infantry render
/// as distance-scaled vector sprites with per-column occlusion.
/// </summary>
public sealed class FirstPersonView
{
    /// <summary>A light in the sky: world-angle direction, angular radius, color, sun flag.</summary>
    public readonly record struct SkyBody(double WorldAngle, double AngularRadius, RgbaColor Color, bool IsSun);

    private const int Columns = 220;
    private const double HalfFov = 0.62; // ~71° total
    private const float WallHeightK = 1.35f;
    private const float WindowBandTop = 0.30f, WindowBandBottom = 0.72f; // fraction of wall strip

    private static readonly RgbaColor CeilingColor = new(26, 32, 44);
    private static readonly RgbaColor FloorColor = new(18, 22, 32);
    private static readonly RgbaColor SpaceBlack = new(2, 4, 9);
    private static readonly RgbaColor WallBase = new(96, 110, 132);
    private static readonly RgbaColor HullBase = new(120, 130, 150);
    private static readonly RgbaColor FrameColor = new(70, 200, 190);
    private static readonly RgbaColor StarColor = new(225, 230, 245);
    private static readonly RgbaColor DroidBody = new(150, 160, 180);
    private static readonly RgbaColor DroidEye = new(255, 70, 70);
    private static readonly RgbaColor HudText = new(150, 240, 210, 190);

    private readonly IRenderer _renderer;
    private readonly DeckPlan.Droid[] _droids = new DeckPlan.Droid[DeckPlan.DroidCount];
    private readonly float[] _scratch = new float[32];
    private readonly double[] _depth = new double[Columns];

    public FirstPersonView(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Draw(
        int widthPx, int heightPx, double simTime,
        double avatarX, double avatarY, double heading,
        double deckWorldAngle,              // world angle the bow (+X deck) points at
        IReadOnlyList<SkyBody> sky,
        string locationHint)
    {
        _renderer.BeginFrame(widthPx, heightPx, SpaceBlack);

        float cy = heightPx / 2f;
        float colW = (float)widthPx / Columns;

        // Ceiling and floor: banded gradients, brighter away from the horizon.
        for (int band = 0; band < 6; band++)
        {
            float t = band / 6f;
            float bandH = cy / 6f;
            var ceil = new RgbaColor(CeilingColor.R, CeilingColor.G, CeilingColor.B, (byte)(255 * (1 - t * 0.7)));
            var floor = new RgbaColor(FloorColor.R, FloorColor.G, FloorColor.B, (byte)(255 * (1 - t * 0.7)));
            DrawHBand(widthPx, band * bandH + bandH / 2, bandH, ceil);
            DrawHBand(widthPx, heightPx - band * bandH - bandH / 2, bandH, floor);
        }

        // Wall columns.
        for (int c = 0; c < Columns; c++)
        {
            double screenX = (c + 0.5) / Columns - 0.5;            // -0.5 … 0.5
            double rayOffset = Math.Atan(2 * screenX * Math.Tan(HalfFov));
            double rayAngle = heading + rayOffset;
            double dirX = Math.Cos(rayAngle), dirY = Math.Sin(rayAngle);

            if (!DeckPlan.CastRay(avatarX, avatarY, dirX, dirY, out double dist, out bool isWindow, out bool isHull, out double along))
            {
                _depth[c] = double.MaxValue;
                continue;
            }

            double perp = dist * Math.Cos(rayOffset);
            _depth[c] = perp;
            float h = (float)(heightPx * WallHeightK / Math.Max(perp, 0.4));
            float top = cy - h / 2, bottom = cy + h / 2;
            float x = c * colW + colW / 2;

            double shade = 1.0 / (1.0 + perp * 0.055);
            // Panel banding: alternate tone every 2 du along the wall.
            double band = ((int)(along / 2) & 1) == 0 ? 1.0 : 0.86;
            RgbaColor baseColor = isHull ? HullBase : WallBase;
            var wall = new RgbaColor(
                (byte)(baseColor.R * shade * band),
                (byte)(baseColor.G * shade * band),
                (byte)(baseColor.B * shade * band));

            if (!isWindow)
            {
                DrawVStrip(x, top, bottom, colW + 1, wall);
                continue;
            }

            // Window column: wall above and below the glass, space in between.
            float winTop = top + h * WindowBandTop;
            float winBottom = top + h * WindowBandBottom;
            DrawVStrip(x, top, winTop, colW + 1, wall);
            DrawVStrip(x, winBottom, bottom, colW + 1, wall);

            // --- Space through the glass ---
            double worldAngle = deckWorldAngle + rayAngle;
            float pxPerRad = (float)(widthPx / (2 * HalfFov)); // small-angle vertical scale

            // Stars: cheap deterministic glints per direction bucket.
            uint hash = (uint)((int)(WrapAngle(worldAngle) * 400) * 2654435761L) | 1;
            hash = hash * 1664525u + 1013904223u;
            if ((hash & 15) < 3)
            {
                float sy = winTop + (hash >> 8) % 1000 / 1000f * (winBottom - winTop);
                _renderer.DrawCircle(x, sy, ((hash >> 4) & 3) == 0 ? 1.4f : 0.8f, StarColor, StarColor);
            }

            foreach (SkyBody body in sky)
            {
                double diff = WrapAngle(worldAngle - body.WorldAngle);
                double drawRadius = Math.Max(body.AngularRadius * (body.IsSun ? 6 : 40), body.IsSun ? 0.035 : 0.006);
                if (Math.Abs(diff) > drawRadius)
                {
                    continue;
                }

                // Vertical chord of the disc at this horizontal offset.
                double chord = Math.Sqrt(Math.Max(0, drawRadius * drawRadius - diff * diff));
                float half = (float)(chord * pxPerRad);
                float discTop = Math.Max(winTop, cy - half);
                float discBottom = Math.Min(winBottom, cy + half);
                if (discBottom <= discTop)
                {
                    continue;
                }

                if (body.IsSun)
                {
                    // Blazing: a glow wider than the core.
                    var glow = new RgbaColor(255, 200, 90, 70);
                    float glowTop = Math.Max(winTop, cy - half * 2.2f);
                    float glowBottom = Math.Min(winBottom, cy + half * 2.2f);
                    DrawVStrip(x, glowTop, glowBottom, colW + 1, glow);
                }

                DrawVStrip(x, discTop, discBottom, colW + 1, body.Color);
            }

            // Glass frame ticks at the window edges.
            _renderer.DrawCircle(x, winTop, 0.8f, FrameColor, FrameColor);
            _renderer.DrawCircle(x, winBottom, 0.8f, FrameColor, FrameColor);
        }

        DrawDroids(widthPx, heightPx, simTime, avatarX, avatarY, heading, colW, cy);

        _renderer.DrawText(widthPx / 2f, heightPx - 12,
            $"{locationHint}  ∙  W/S walk ∙ A/D turn ∙ E interact ∙ F deck plan ∙ Q helm",
            HudText, "11px monospace", TextAlign.Center);

        _renderer.EndFrame();
    }

    private void DrawDroids(int widthPx, int heightPx, double simTime,
        double avatarX, double avatarY, double heading, float colW, float cy)
    {
        DeckPlan.GetDroids(simTime, _droids);

        foreach (DeckPlan.Droid droid in _droids)
        {
            double dx = droid.X - avatarX, dy = droid.Y - avatarY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.6)
            {
                continue;
            }

            double angle = WrapAngle(Math.Atan2(dy, dx) - heading);
            if (Math.Abs(angle) > HalfFov + 0.2)
            {
                continue;
            }

            double perp = dist * Math.Cos(angle);
            double screenX = Math.Tan(angle) / (2 * Math.Tan(HalfFov)) + 0.5;
            int column = (int)(screenX * Columns);
            if (column < 0 || column >= Columns || _depth[column] < perp - 0.3)
            {
                continue; // behind a wall
            }

            float x = (float)(screenX * widthPx);
            float h = (float)(heightPx * WallHeightK / Math.Max(perp, 0.4)) * 0.62f; // droid ~62% of wall height
            float baseY = cy + (float)(heightPx * WallHeightK / Math.Max(perp, 0.4)) / 2 * 0.96f;
            float u = h / 10f;
            double shade = 1.0 / (1.0 + perp * 0.05);
            var body = new RgbaColor((byte)(DroidBody.R * shade), (byte)(DroidBody.G * shade), (byte)(DroidBody.B * shade));

            // Legs.
            DrawSeg(x - 1.2f * u, baseY, x - 0.8f * u, baseY - 3f * u, body, 1.6f);
            DrawSeg(x + 1.2f * u, baseY, x + 0.8f * u, baseY - 3f * u, body, 1.6f);
            // Torso trapezoid.
            Span<float> t = _scratch.AsSpan(0, 10);
            t[0] = x - 1.6f * u; t[1] = baseY - 3f * u;
            t[2] = x + 1.6f * u; t[3] = baseY - 3f * u;
            t[4] = x + 1.1f * u; t[5] = baseY - 6.5f * u;
            t[6] = x - 1.1f * u; t[7] = baseY - 6.5f * u;
            t[8] = x - 1.6f * u; t[9] = baseY - 3f * u;
            _renderer.DrawPolyline(t, body, 1.8f);
            // Blaster arm 🏴‍☠️.
            DrawSeg(x + 1.4f * u, baseY - 5.5f * u, x + 3.2f * u, baseY - 5.8f * u, body, 2f);
            // Dome head + eye light (blinks).
            _renderer.DrawCircle(x, baseY - 7.6f * u, 1.3f * u, null, body, 1.8f);
            if (Math.Sin(simTime * 0.004 + droid.X) > -0.5)
            {
                _renderer.DrawCircle(x + 0.4f * u, baseY - 7.6f * u, 0.35f * u, DroidEye, DroidEye);
            }
            // Nameplate when close.
            if (perp < 8)
            {
                _renderer.DrawText(x, baseY - 9.5f * u, droid.Name, HudText, "9px monospace", TextAlign.Center);
            }
        }
    }

    private void DrawHBand(int widthPx, float centerY, float height, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 4);
        s[0] = 0; s[1] = centerY; s[2] = widthPx; s[3] = centerY;
        _renderer.DrawPolyline(s, color, height + 1);
    }

    private void DrawVStrip(float x, float top, float bottom, float width, RgbaColor color)
    {
        if (bottom <= top)
        {
            return;
        }

        Span<float> s = _scratch.AsSpan(0, 4);
        s[0] = x; s[1] = top; s[2] = x; s[3] = bottom;
        _renderer.DrawPolyline(s, color, width);
    }

    private void DrawSeg(float x1, float y1, float x2, float y2, RgbaColor color, float width)
    {
        Span<float> s = _scratch.AsSpan(0, 4);
        s[0] = x1; s[1] = y1; s[2] = x2; s[3] = y2;
        _renderer.DrawPolyline(s, color, width);
    }

    private static double WrapAngle(double a)
    {
        while (a > Math.PI) a -= Math.Tau;
        while (a < -Math.PI) a += Math.Tau;
        return a;
    }
}
