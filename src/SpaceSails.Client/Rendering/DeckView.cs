using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Walk your own ship (M12): a top-down vector deck plan of the pirate craft with a movable
/// avatar and interactable consoles. Same three-primitive art style as <see cref="ScopeView"/>;
/// pure function of its inputs. Deck coordinates are "deck units" (du), ship nose +X;
/// the plan is ~46×18 du and scales to fit the viewport.
///
/// Layout, bow to stern: bridge (helm) → scope alcove (port) / cargo hold (starboard) →
/// midship corridor → shuttle bay (port, with the boarding shuttle) / engine room (vent panel).
/// </summary>
public sealed class DeckView
{
    public enum Console { None, Helm, Scope, Vent, Cargo, Shuttle }

    public readonly record struct State(
        double AvatarX, double AvatarY, double HeadingRad,
        int CargoUnits, double Charge, bool ShuttleAway, bool ElectricUniverse);

    public const double InteractRadius = 3.0;
    private const double AvatarRadius = 0.75;

    // Walls as segments (x1,y1,x2,y2) in deck units. Origin midships; +X bow, +Y port.
    private static readonly float[][] Walls =
    [
        // Outer hull: pointed bow, boxy stern.
        [23, 0, 16, 8], [16, 8, -14, 8], [-14, 8, -20, 6], [-20, 6, -23, 6], [-23, 6, -23, -6],
        [-23, -6, -20, -6], [-20, -6, -14, -8], [-14, -8, 16, -8], [16, -8, 23, 0],
        // Bridge bulkhead (door gap on centerline y in [-1.5, 1.5]).
        [12, 8, 12, 1.5f], [12, -1.5f, 12, -8],
        // Scope alcove (port forward): wall with a door gap.
        [12, 4, 4, 4], // alcove floor wall piece? (runs fore-aft) — inner wall
        [4, 8, 4, 5.5f],
        // Cargo hold (starboard forward): inner wall with door.
        [12, -4, 4, -4],
        [4, -8, 4, -5.5f],
        // Aft section split: shuttle bay (port) and engine room (starboard), door gaps midship.
        [-6, 8, -6, 1.5f], [-6, -1.5f, -6, -8],
        // Spine wall splitting bay from engine room. Recessed to -8 so the aft door has a
        // vestibule — flush with the bulkhead it dead-ended anyone walking the centerline.
        [-8, 0, -14, 0],
    ];

    private static readonly (Console Kind, float X, float Y, string Label)[] Consoles =
    [
        (Console.Helm, 19, 0, "HELM"),
        (Console.Scope, 8, 6.2f, "SCOPE"),
        (Console.Cargo, 8, -6.2f, "CARGO"),
        (Console.Shuttle, -15, 4.5f, "SHUTTLE BAY"),
        (Console.Vent, -20, -4, "VENT PANEL"),
    ];

    private static readonly RgbaColor Floor = new(10, 14, 22);
    private static readonly RgbaColor HullLine = new(170, 185, 205);
    private static readonly RgbaColor InnerLine = new(110, 125, 145, 200);
    private static readonly RgbaColor ConsoleGlow = new(120, 220, 200);
    private static readonly RgbaColor ConsoleNear = new(190, 255, 220);
    private static readonly RgbaColor AvatarColor = new(255, 210, 80);
    private static readonly RgbaColor CrateColor = new(200, 160, 90, 220);
    private static readonly RgbaColor ShuttleColor = new(150, 210, 255, 220);
    private static readonly RgbaColor TextDim = new(140, 160, 180, 170);

    private readonly IRenderer _renderer;
    private readonly float[] _scratch = new float[64];

    public DeckView(IRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>Clamped move with wall collision; returns the new avatar position.</summary>
    public static (double X, double Y) Move(double x, double y, double dx, double dy)
    {
        // Axis-separated so the avatar slides along walls instead of sticking.
        double nx = Collides(x + dx, y) ? x : x + dx;
        double ny = Collides(nx, y + dy) ? y : y + dy;
        return (nx, ny);
    }

    private static bool Collides(double x, double y)
    {
        foreach (float[] w in Walls)
        {
            if (DistanceToSegment(x, y, w[0], w[1], w[2], w[3]) < AvatarRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double lengthSq = dx * dx + dy * dy;
        double t = lengthSq > 0 ? Math.Clamp(((px - x1) * dx + (py - y1) * dy) / lengthSq, 0, 1) : 0;
        double cx = x1 + t * dx, cy = y1 + t * dy;
        return Math.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
    }

    /// <summary>The console within interaction range of the avatar, or None.</summary>
    public static Console NearestConsole(double x, double y)
    {
        foreach ((Console kind, float cx, float cy, _) in Consoles)
        {
            double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d <= InteractRadius)
            {
                return kind;
            }
        }

        return Console.None;
    }

    public void Draw(int widthPx, int heightPx, double simTime, in State state)
    {
        _renderer.BeginFrame(widthPx, heightPx, Floor);

        // Fit the ~46×18 du plan into the viewport with margins.
        float scale = Math.Min(widthPx / 52f, heightPx / 26f);
        float ox = widthPx / 2f, oy = heightPx / 2f;
        (float X, float Y) P(double dx, double dy) => (ox + (float)dx * scale, oy - (float)dy * scale);

        // Deck grid for depth.
        for (int gx = -22; gx <= 22; gx += 4)
        {
            DrawSeg(P(gx, -7.6), P(gx, 7.6), new RgbaColor(255, 255, 255, 10), 1f);
        }

        // Walls.
        foreach (float[] w in Walls)
        {
            bool outer = Array.IndexOf(Walls, w) < 9;
            DrawSeg(P(w[0], w[1]), P(w[2], w[3]), outer ? HullLine : InnerLine, outer ? 2.5f : 1.5f);
        }

        // Room labels.
        Label(P(17, -5.2), "BRIDGE");
        Label(P(8, 2.2), "SCOPE ALCOVE");
        Label(P(8, -2.2), "CARGO HOLD");
        Label(P(-10, 5.8), "SHUTTLE BAY");
        Label(P(-10, -5.8), "ENGINE ROOM");

        // Cargo crates: one per unit aboard, racked in the hold.
        for (int i = 0; i < Math.Min(state.CargoUnits, 12); i++)
        {
            (float cx, float cy) = P(5.5 + (i % 4) * 1.8, -4.8 - (i / 4) * 1.4);
            DrawBox(cx, cy, 0.65f * scale, CrateColor);
        }

        // The boarding shuttle: parked in the bay, or away doing piracy.
        if (!state.ShuttleAway)
        {
            DrawShuttle(P(-10.5, 4.2), scale, simTime);
        }
        else
        {
            (float bx, float by) = P(-10.5, 4.2);
            _renderer.DrawText(bx, by, "— AWAY —", new RgbaColor(255, 170, 80, 200), "bold 11px monospace", TextAlign.Center);
            // Open bay doors: blinking edge strips.
            if (Math.Sin(simTime * 0.005) > 0)
            {
                DrawSeg(P(-14, 7.9), P(-7, 7.9), new RgbaColor(255, 120, 80, 220), 3f);
            }
        }

        // Engine room dressing: reactor ring + charge conduit that glows with hull charge.
        (float rx, float ry) = P(-19, -2.5);
        _renderer.DrawCircle(rx, ry, 1.6f * scale, null, InnerLine, 2f);
        double throb = 0.5 + 0.5 * Math.Sin(simTime * 0.002);
        var reactor = new RgbaColor(120, 200, 255, (byte)(90 + 70 * throb));
        _renderer.DrawCircle(rx, ry, 0.9f * scale, reactor, reactor);
        if (state.ElectricUniverse)
        {
            var conduit = new RgbaColor(255, 240, 120, (byte)(40 + 180 * state.Charge));
            DrawSeg(P(-19, -4), P(-20, -4), conduit, 3f);
            DrawSeg(P(-19, -1), P(-19, -4), conduit, 3f);
        }

        // Consoles: dot + label; glow brighter when the avatar is in range.
        foreach ((Console kind, float cx, float cy, string label) in Consoles)
        {
            (float sx, float sy) = P(cx, cy);
            bool near = Math.Sqrt((state.AvatarX - cx) * (state.AvatarX - cx) + (state.AvatarY - cy) * (state.AvatarY - cy)) <= InteractRadius;
            RgbaColor c = near ? ConsoleNear : ConsoleGlow;
            _renderer.DrawCircle(sx, sy, near ? 5f : 3.5f, c, c);
            _renderer.DrawText(sx, sy - 10, label, near ? ConsoleNear : TextDim, near ? "bold 10px monospace" : "9px monospace", TextAlign.Center);
            if (near)
            {
                _renderer.DrawText(sx, sy + 20, "[E]", ConsoleNear, "bold 11px monospace", TextAlign.Center);
            }
        }

        // The captain.
        (float ax, float ay) = P(state.AvatarX, state.AvatarY);
        _renderer.DrawCircle(ax, ay, 0.75f * scale, AvatarColor, AvatarColor);
        float hx = ax + (float)Math.Cos(state.HeadingRad) * scale * 1.1f;
        float hy = ay - (float)Math.Sin(state.HeadingRad) * scale * 1.1f;
        DrawSeg((ax, ay), (hx, hy), AvatarColor, 2f);

        _renderer.DrawText(ox, heightPx - 10, "WASD / arrows — move ∙ E — interact ∙ Q — back to the helm",
            TextDim, "11px monospace", TextAlign.Center);

        _renderer.EndFrame();
    }

    private void DrawSeg((float X, float Y) a, (float X, float Y) b, RgbaColor color, float width)
    {
        Span<float> s = _scratch.AsSpan(0, 4);
        s[0] = a.X; s[1] = a.Y; s[2] = b.X; s[3] = b.Y;
        _renderer.DrawPolyline(s, color, width);
    }

    private void DrawBox(float cx, float cy, float half, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 10);
        s[0] = cx - half; s[1] = cy - half;
        s[2] = cx + half; s[3] = cy - half;
        s[4] = cx + half; s[5] = cy + half;
        s[6] = cx - half; s[7] = cy + half;
        s[8] = cx - half; s[9] = cy - half;
        _renderer.DrawPolyline(s, color, 1.5f);
    }

    private void DrawShuttle((float X, float Y) at, float scale, double simTime)
    {
        // A stubby dart with a canopy — the little craft that does the boarding.
        Span<float> s = _scratch.AsSpan(0, 12);
        float u = scale * 0.9f;
        (float x, float y) = at;
        s[0] = x + 2.2f * u; s[1] = y;
        s[2] = x - 1.4f * u; s[3] = y + 1.1f * u;
        s[4] = x - 0.8f * u; s[5] = y;
        s[6] = x - 1.4f * u; s[7] = y - 1.1f * u;
        s[8] = x + 2.2f * u; s[9] = y;
        _renderer.DrawPolyline(s[..10], ShuttleColor, 2f);
        _renderer.DrawCircle(x + 0.8f * u, y, 0.35f * u, ShuttleColor, ShuttleColor);
        // Standby beacon.
        if (Math.Sin(simTime * 0.003) > 0.6)
        {
            var beacon = new RgbaColor(120, 255, 160, 200);
            _renderer.DrawCircle(x - 1.2f * u, y, 2.5f, beacon, beacon);
        }
    }

    private void Label((float X, float Y) at, string text) =>
        _renderer.DrawText(at.X, at.Y, text, TextDim, "10px monospace", TextAlign.Center);
}
