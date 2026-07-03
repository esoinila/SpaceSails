using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// The ship-side telescope (worldbuilding notes §5): an inset optical view that auto-locks the
/// current target with adaptive magnification. Everything here is hand-drawn vector art from
/// the renderer's three primitives — circles, polylines, text — so the scope costs no assets
/// and no new interop. Pure function of its inputs; all animation phases derive from sim time.
/// </summary>
public sealed class ScopeView
{
    public enum TargetKind { None, Body, Freighter, Pod, Player }

    public readonly record struct Target(
        TargetKind Kind,
        string Name,
        string? Detail,          // cargo class, body class…
        Vector2d Position,
        Vector2d Velocity,
        double BodyRadius,       // bodies only
        RgbaColor Color,
        bool InPlasma);

    private static readonly RgbaColor ScopeBackground = new(3, 6, 10);
    private static readonly RgbaColor ScopeRim = new(90, 200, 190, 140);
    private static readonly RgbaColor BracketColor = new(120, 255, 190, 200);
    private static readonly RgbaColor HudText = new(150, 240, 210);
    private static readonly RgbaColor StarDim = new(150, 160, 180, 120);
    private static readonly RgbaColor StarBright = new(230, 235, 245, 200);
    private static readonly RgbaColor Plasma = new(80, 220, 220, 46);
    private static readonly RgbaColor PlasmaSpark = new(160, 250, 245, 160);
    private static readonly RgbaColor HullLine = new(200, 210, 225);
    private static readonly RgbaColor SailLine = new(140, 190, 235, 190);
    private static readonly RgbaColor EngineGlow = new(255, 170, 80, 200);
    private static readonly RgbaColor NightShade = new(0, 0, 0, 165);

    private readonly IRenderer _renderer;
    private readonly float[] _scratch = new float[64];

    /// <summary>Footer tag: "◆ AUTO" in auto mode, "◆ TRACK" when the pilot picked the target.</summary>
    public string LockLabel { get; set; } = "◆ AUTO";

    public ScopeView(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Draw(int sizePx, double simTime, Vector2d shipPosition, Vector2d shipVelocity, Target target)
    {
        _renderer.BeginFrame(sizePx, sizePx, ScopeBackground);

        float s = sizePx;
        float cx = s / 2, cy = s / 2;

        DrawStarfield(s, simTime, target.Kind == TargetKind.None ? 0 : HashDirection(target.Position - shipPosition));

        if (target.Kind == TargetKind.None)
        {
            DrawStatic(s, simTime);
            _renderer.DrawText(cx, cy, "NO TARGET", new RgbaColor(220, 120, 120, 200), "bold 14px monospace", TextAlign.Center);
            DrawRim(s);
            _renderer.EndFrame();
            return;
        }

        if (target.InPlasma)
        {
            DrawPlasmaWisps(s, simTime);
        }

        double distance = (target.Position - shipPosition).Length;
        double relSpeed = (target.Velocity - shipVelocity).Length;
        // The sprite noses along its velocity; screen y is down, world y is up.
        double heading = Math.Atan2(-(target.Velocity.Y), target.Velocity.X);

        switch (target.Kind)
        {
            case TargetKind.Body:
                DrawBody(cx, cy, s, simTime, target, shipPosition);
                break;
            case TargetKind.Freighter:
                DrawFreighter(cx, cy, heading, simTime);
                break;
            case TargetKind.Pod:
                DrawPod(cx, cy, heading, simTime);
                break;
            case TargetKind.Player:
                DrawPlayerDart(cx, cy, heading);
                break;
        }

        DrawLockBrackets(cx, cy, s * 0.36f, simTime);

        // Instrument-screen readouts, corner-anchored (the display is rectangular now).
        _renderer.DrawText(8, 16, target.Name.ToUpperInvariant(), HudText, "bold 12px monospace");
        _renderer.DrawText(s - 8, 16, KindLabel(target), HudText, "11px monospace", TextAlign.Right);
        if (target.Detail is { Length: > 0 } detail)
        {
            _renderer.DrawText(8, 30, detail, new RgbaColor(150, 240, 210, 160), "10px monospace");
        }
        _renderer.DrawText(8, s - 10, FormatDistance(distance), HudText, "bold 12px monospace");
        _renderer.DrawText(s - 8, s - 10, $"{relSpeed / 1000:F1} km/s rel", HudText, "11px monospace", TextAlign.Right);
        _renderer.DrawText(cx, s - 10, LockLabel, new RgbaColor(120, 255, 190, 170), "9px monospace", TextAlign.Center);

        DrawRim(s);
        _renderer.EndFrame();
    }

    private static string KindLabel(in Target t) => t.Kind switch
    {
        TargetKind.Body => t.BodyRadius > 1e8 ? "STAR" : "PLANET",
        TargetKind.Freighter => "FREIGHTER",
        TargetKind.Pod => "CARGO POD",
        TargetKind.Player => "SHIP ∙ CREW",
        _ => "",
    };

    // ---- Backdrop ----

    private static int HashDirection(Vector2d look)
    {
        // Quantize the look direction so the starfield is stable per target but shifts
        // (parallax!) when the scope swings to a new one.
        int a = (int)(Math.Atan2(look.Y, look.X) * 8 / Math.Tau + 8);
        return unchecked(a * (int)2654435761u);
    }

    private void DrawStarfield(float s, double simTime, int seed)
    {
        uint h = (uint)seed | 1;
        for (int i = 0; i < 46; i++)
        {
            h = h * 1664525u + 1013904223u;
            float x = (h >> 8) % 1000 / 1000f * s;
            h = h * 1664525u + 1013904223u;
            float y = (h >> 8) % 1000 / 1000f * s;
            h = h * 1664525u + 1013904223u;
            bool bright = (h & 7) == 0;

            // Slow drift sells "we are moving"; wraps around the frame.
            x = (x + (float)(simTime * 0.002 % s) + s) % s;
            _renderer.DrawCircle(x, y, bright ? 1.3f : 0.7f, bright ? StarBright : StarDim, bright ? StarBright : StarDim);
        }
    }

    private void DrawStatic(float s, double simTime)
    {
        uint h = (uint)(simTime * 997) | 1;
        for (int i = 0; i < 30; i++)
        {
            h = h * 1664525u + 1013904223u;
            float x = (h >> 7) % 1000 / 1000f * s;
            h = h * 1664525u + 1013904223u;
            float y = (h >> 7) % 1000 / 1000f * s;
            _renderer.DrawCircle(x, y, 0.6f, new RgbaColor(120, 130, 140, 90), new RgbaColor(120, 130, 140, 90));
        }
    }

    private void DrawPlasmaWisps(float s, double simTime)
    {
        Span<float> pts = _scratch.AsSpan(0, 34);
        for (int ribbon = 0; ribbon < 3; ribbon++)
        {
            double phase = simTime * 0.00025 + ribbon * 2.1;
            float baseY = s * (0.3f + 0.2f * ribbon);
            for (int i = 0; i <= 16; i++)
            {
                float x = s * i / 16f;
                pts[i * 2] = x;
                pts[i * 2 + 1] = baseY + (float)(Math.Sin(x / s * Math.Tau * 1.5 + phase) * s * 0.06);
            }
            _renderer.DrawPolyline(pts, Plasma, 7f);
        }

        uint h = (uint)(simTime / 400) | 1;
        for (int i = 0; i < 8; i++)
        {
            h = h * 1664525u + 1013904223u;
            float x = (float)((h >> 8) % 1000 / 1000f * s + simTime * 0.01) % s;
            h = h * 1664525u + 1013904223u;
            float y = (h >> 8) % 1000 / 1000f * s;
            _renderer.DrawCircle(x, y, 1.2f, PlasmaSpark, PlasmaSpark);
        }
    }

    // ---- Targets ----

    private void DrawBody(float cx, float cy, float s, double simTime, in Target target, Vector2d shipPosition)
    {
        bool isStar = target.BodyRadius > 1e8;
        float r = s * (isStar ? 0.30f : 0.33f);

        if (isStar)
        {
            for (int i = 4; i >= 1; i--)
            {
                var corona = new RgbaColor(255, 210, 90, (byte)(18 * i));
                _renderer.DrawCircle(cx, cy, r * (1 + 0.22f * (5 - i)), corona, corona);
            }
            _renderer.DrawCircle(cx, cy, r, target.Color, target.Color);
            for (int i = 0; i < 8; i++)
            {
                double a = Math.Tau * i / 8 + simTime * 0.00002;
                float f1 = r * 1.05f, f2 = r * (1.35f + 0.1f * (i % 3));
                Span<float> flare = _scratch.AsSpan(0, 4);
                flare[0] = cx + f1 * (float)Math.Cos(a); flare[1] = cy + f1 * (float)Math.Sin(a);
                flare[2] = cx + f2 * (float)Math.Cos(a); flare[3] = cy + f2 * (float)Math.Sin(a);
                _renderer.DrawPolyline(flare, new RgbaColor(255, 220, 120, 120), 2f);
            }
            return;
        }

        _renderer.DrawCircle(cx, cy, r, target.Color, target.Color);

        // Night side: a dark disc pushed anti-sunward. It spills onto black space, so only the
        // in-disc part reads — a cheap terminator from circles alone.
        Vector2d toSun = (Vector2d.Zero - target.Position).Normalized();
        float sx = (float)toSun.X, sy = (float)(-toSun.Y);
        _renderer.DrawCircle(cx - sx * r * 0.55f, cy - sy * r * 0.55f, r * 0.98f, NightShade, NightShade);

        // A whisper of limb light on the day side.
        _renderer.DrawCircle(cx + sx * r * 0.08f, cy + sy * r * 0.08f, r * 0.99f, null, new RgbaColor(255, 255, 255, 40), 1.5f);

        if (target.Name is "Saturn")
        {
            _renderer.DrawCircle(cx, cy, r * 1.45f, null, new RgbaColor(220, 200, 150, 110), 4f);
            _renderer.DrawCircle(cx, cy, r * 1.62f, null, new RgbaColor(220, 200, 150, 60), 2.5f);
        }
    }

    private void DrawFreighter(float cx, float cy, double heading, double simTime)
    {
        float u = 9f; // one hull unit in pixels; the freighter is ~14 units long

        // Solar sail behind the hull: a big braced quad.
        DrawRotated(cx, cy, heading, u, SailLine, 1.5f,
            [-6, 5.5f, -2, 5.5f, -2, -5.5f, -6, -5.5f, -6, 5.5f]);
        DrawRotated(cx, cy, heading, u, SailLine, 1f, [-6, 5.5f, -2, -5.5f]);
        DrawRotated(cx, cy, heading, u, SailLine, 1f, [-6, -5.5f, -2, 5.5f]);
        DrawRotated(cx, cy, heading, u, SailLine, 1f, [-4, 5.5f, -4, -5.5f]);

        // Hull: long hexagonal barge with a spine.
        DrawRotated(cx, cy, heading, u, HullLine, 2f,
            [7, 0, 5, 1.6f, -4, 1.6f, -5.5f, 0.8f, -5.5f, -0.8f, -4, -1.6f, 5, -1.6f, 7, 0]);
        DrawRotated(cx, cy, heading, u, HullLine, 1f, [-5.5f, 0, 7, 0]);
        // Cargo frames.
        DrawRotated(cx, cy, heading, u, HullLine, 1f, [2, 1.6f, 2, -1.6f]);
        DrawRotated(cx, cy, heading, u, HullLine, 1f, [0, 1.6f, 0, -1.6f]);
        DrawRotated(cx, cy, heading, u, HullLine, 1f, [-2, 1.6f, -2, -1.6f]);

        // Bridge and engines.
        (float bx, float by) = Rotate(5.6f * u, 0, heading);
        _renderer.DrawCircle(cx + bx, cy + by, 3.2f, new RgbaColor(160, 220, 255, 220), HullLine);
        double throb = 0.6 + 0.4 * Math.Sin(simTime * 0.003);
        (float e1x, float e1y) = Rotate(-5.9f * u, 0.7f * u, heading);
        (float e2x, float e2y) = Rotate(-5.9f * u, -0.7f * u, heading);
        var glow = new RgbaColor(255, 170, 80, (byte)(120 + 80 * throb));
        _renderer.DrawCircle(cx + e1x, cy + e1y, 2.6f, glow, EngineGlow);
        _renderer.DrawCircle(cx + e2x, cy + e2y, 2.6f, glow, EngineGlow);
    }

    private void DrawPod(float cx, float cy, double heading, double simTime)
    {
        float u = 8f;

        // Capsule: two end circles joined by side lines.
        (float n1x, float n1y) = Rotate(3.2f * u, 0, heading);
        (float n2x, float n2y) = Rotate(-3.2f * u, 0, heading);
        _renderer.DrawCircle(cx + n1x, cy + n1y, 1.7f * u, null, HullLine, 2f);
        _renderer.DrawCircle(cx + n2x, cy + n2y, 1.7f * u, null, HullLine, 2f);
        DrawRotated(cx, cy, heading, u, HullLine, 2f, [3.2f, 1.7f, -3.2f, 1.7f]);
        DrawRotated(cx, cy, heading, u, HullLine, 2f, [3.2f, -1.7f, -3.2f, -1.7f]);
        // Mass-driver cradle fins.
        DrawRotated(cx, cy, heading, u, HullLine, 1.5f, [-3.2f, 1.7f, -4.4f, 2.8f]);
        DrawRotated(cx, cy, heading, u, HullLine, 1.5f, [-3.2f, -1.7f, -4.4f, -2.8f]);
        // Compute-core stripes.
        DrawRotated(cx, cy, heading, u, new RgbaColor(120, 200, 255, 170), 1f, [1.5f, 1.7f, 1.5f, -1.7f]);
        DrawRotated(cx, cy, heading, u, new RgbaColor(120, 200, 255, 170), 1f, [-1.5f, 1.7f, -1.5f, -1.7f]);

        // Beacon: pods are honest — they blink.
        if (Math.Sin(simTime * 0.004) > 0.4)
        {
            (float bx, float by) = Rotate(0, -2.4f * u, heading);
            var beacon = new RgbaColor(255, 90, 90, 220);
            _renderer.DrawCircle(cx + bx, cy + by, 2.2f, beacon, beacon);
        }
    }

    private void DrawPlayerDart(float cx, float cy, double heading)
    {
        float u = 10f;
        var green = new RgbaColor(120, 255, 140, 220);
        DrawRotated(cx, cy, heading, u, green, 2f, [6, 0, -4, 2.6f, -2.5f, 0, -4, -2.6f, 6, 0]);
        (float px, float py) = Rotate(2.2f * u, 0, heading);
        _renderer.DrawCircle(cx + px, cy + py, 2.4f, new RgbaColor(190, 255, 200, 230), green);
        (float ex, float ey) = Rotate(-3.2f * u, 0, heading);
        _renderer.DrawCircle(cx + ex, cy + ey, 2.2f, EngineGlow, EngineGlow);
    }

    // ---- Instrument chrome ----

    private void DrawLockBrackets(float cx, float cy, float half, double simTime)
    {
        // Brackets breathe slightly — a live lock, not a decal.
        half += (float)(2 * Math.Sin(simTime * 0.002));
        float arm = half * 0.3f;
        Span<float> b = _scratch.AsSpan(0, 6);
        ReadOnlySpan<float> signs = [1, 1, 1, -1, -1, 1, -1, -1];
        for (int i = 0; i < 8; i += 2)
        {
            float sx = signs[i], sy = signs[i + 1];
            b[0] = cx + sx * half - sx * arm; b[1] = cy + sy * half;
            b[2] = cx + sx * half; b[3] = cy + sy * half;
            b[4] = cx + sx * half; b[5] = cy + sy * half - sy * arm;
            _renderer.DrawPolyline(b, BracketColor, 2f);
        }
    }

    private void DrawRim(float s)
    {
        // Screen bezel: frame + corner ticks + faint scanlines. An instrument, not a porthole.
        Span<float> f = _scratch.AsSpan(0, 10);
        f[0] = 1; f[1] = 1; f[2] = s - 1; f[3] = 1; f[4] = s - 1; f[5] = s - 1; f[6] = 1; f[7] = s - 1; f[8] = 1; f[9] = 1;
        _renderer.DrawPolyline(f, ScopeRim, 2f);
        for (int y = 8; y < s; y += 14)
        {
            Span<float> line = _scratch.AsSpan(0, 4);
            line[0] = 2; line[1] = y; line[2] = s - 2; line[3] = y;
            _renderer.DrawPolyline(line, new RgbaColor(120, 220, 210, 8), 1f);
        }
    }

    // ---- Small vector helpers ----

    private static (float X, float Y) Rotate(float x, float y, double angle)
    {
        float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
        return (x * c - y * s, x * s + y * c);
    }

    /// <summary>Rotate a polyline given in hull units around the origin, scale, translate, draw.</summary>
    private void DrawRotated(float cx, float cy, double angle, float unit, RgbaColor color, float width, ReadOnlySpan<float> unitPoints)
    {
        Span<float> pts = _scratch.AsSpan(0, unitPoints.Length);
        for (int i = 0; i < unitPoints.Length; i += 2)
        {
            (float x, float y) = Rotate(unitPoints[i] * unit, unitPoints[i + 1] * unit, angle);
            pts[i] = cx + x;
            pts[i + 1] = cy + y;
        }
        _renderer.DrawPolyline(pts, color, width);
    }

    private static string FormatDistance(double meters)
    {
        const double au = 1.495978707e11;
        if (meters >= au / 10) return $"{meters / au:F3} AU";
        if (meters >= 1e9) return $"{meters / 1e9:F2} M km";
        return $"{meters / 1000:N0} km";
    }
}
