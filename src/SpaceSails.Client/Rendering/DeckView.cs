using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Top-down deck plan view (M12, rebuilt on <see cref="DeckPlan"/> in M13): the tactical map
/// of your own ship. Windows draw teal, droids patrol, cargo racks mirror the hold, and the
/// shuttle sits in its cradle unless it's away boarding. First-person is the immersive twin
/// (<see cref="FirstPersonView"/>); both render the same plan.
/// </summary>
public sealed class DeckView
{
    public readonly record struct State(
        double AvatarX, double AvatarY, double HeadingRad,
        int CargoUnits, double Charge, bool ShuttleAway, bool ElectricUniverse);

    private static readonly RgbaColor Floor = new(10, 14, 22);
    private static readonly RgbaColor HullLine = new(170, 185, 205);
    private static readonly RgbaColor InnerLine = new(110, 125, 145, 200);
    private static readonly RgbaColor WindowLine = new(80, 220, 210, 220);
    private static readonly RgbaColor ConsoleGlow = new(120, 220, 200);
    private static readonly RgbaColor ConsoleNear = new(190, 255, 220);
    private static readonly RgbaColor AvatarColor = new(255, 210, 80);
    private static readonly RgbaColor CrateColor = new(200, 160, 90, 220);
    private static readonly RgbaColor ShuttleColor = new(150, 210, 255, 220);
    private static readonly RgbaColor DroidColor = new(150, 160, 180);
    private static readonly RgbaColor TextDim = new(140, 160, 180, 170);

    private readonly IRenderer _renderer;
    private readonly DeckPlan.Droid[] _droids = new DeckPlan.Droid[DeckPlan.DroidCount];
    private readonly float[] _scratch = new float[32];

    public DeckView(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Draw(int widthPx, int heightPx, double simTime, in State state, double panX = 0, double panY = 0)
    {
        _renderer.BeginFrame(widthPx, heightPx, Floor);

        float scale = Math.Min(widthPx / 64f, heightPx / 28f);
        float ox = widthPx / 2f + (float)panX, oy = heightPx / 2f + (float)panY;
        (float X, float Y) P(double dx, double dy) => (ox + (float)dx * scale, oy - (float)dy * scale);

        // Room backdrops sit UNDER every vector overlay (walls, consoles, avatar, labels stay on top
        // for legibility — the hybrid look). The cantina wears The Space Bar; the zone is x∈[4,18],
        // y∈[3,10] in deck units. Registration is idempotent, so calling it per frame is cheap.
        int barArt = _renderer.RegisterImage("art/the-space-bar.jpg");
        (float barX, float barY) = P(4, 10); // top-left corner of the cantina zone on screen
        _renderer.DrawImage(barArt, barX, barY, 14f * scale, 7f * scale, 0.9f);

        // Starboard berths (3D-reno Phase 3): the three cabins wear a cramped bunk; the HEAD wears a
        // grimy space-toilet. Zones partition x∈[4,18], y∈[-10,-3]; top edge is y=-3 (P is y-up).
        int bunkArt = _renderer.RegisterImage("art/cabin-bunk.jpg");
        int headArt = _renderer.RegisterImage("art/space-head.jpg");
        float berthH = 7f * scale;
        foreach ((double x0, double x1) in stackalloc (double, double)[] { (4, 7.5), (7.5, 11), (11, 14.5) })
        {
            (float bx, float by) = P(x0, -3);
            _renderer.DrawImage(bunkArt, bx, by, (float)(x1 - x0) * scale, berthH, 0.9f);
        }
        (float headX, float headY) = P(14.5, -3);
        _renderer.DrawImage(headArt, headX, headY, 3.5f * scale, berthH, 0.9f);

        for (int gx = -22; gx <= 28; gx += 4)
        {
            DrawSeg(P(gx, -9.6), P(gx, 9.6), new RgbaColor(255, 255, 255, 10), 1f);
        }

        foreach (DeckPlan.Wall w in DeckPlan.Walls)
        {
            RgbaColor color = w.IsWindow ? WindowLine : w.IsHull ? HullLine : InnerLine;
            DrawSeg(P(w.X1, w.Y1), P(w.X2, w.Y2), color, w.IsHull ? 2.5f : 1.5f);
        }

        foreach ((float lx, float ly, string text) in DeckPlan.RoomLabels)
        {
            _renderer.DrawText(P(lx, ly).X, P(lx, ly).Y, text, TextDim, "10px monospace", TextAlign.Center);
        }

        // Cargo crates: one per unit aboard.
        for (int i = 0; i < Math.Min(state.CargoUnits, 12); i++)
        {
            (float cx, float cy) = P(-10 + (i % 4) * 1.9, -5 - (i / 4) * 1.6);
            DrawBox(cx, cy, 0.65f * scale, CrateColor);
        }

        // Shuttle in its cradle — or away doing piracy.
        if (!state.ShuttleAway)
        {
            DrawShuttle(P(-6.5, 6.5), scale, simTime);
        }
        else
        {
            (float bx, float by) = P(-6.5, 6.5);
            _renderer.DrawText(bx, by, "— AWAY —", new RgbaColor(255, 170, 80, 200), "bold 11px monospace", TextAlign.Center);
            if (Math.Sin(simTime * 0.005) > 0)
            {
                DrawSeg(P(-11, 9.9), P(-2, 9.9), new RgbaColor(255, 120, 80, 220), 3f);
            }
        }

        // Reactor + charge conduit (engine room).
        (float rx, float ry) = P(-19, 2.5);
        _renderer.DrawCircle(rx, ry, 1.6f * scale, null, InnerLine, 2f);
        double throb = 0.5 + 0.5 * Math.Sin(simTime * 0.002);
        var reactor = new RgbaColor(120, 200, 255, (byte)(90 + 70 * throb));
        _renderer.DrawCircle(rx, ry, 0.9f * scale, reactor, reactor);
        if (state.ElectricUniverse)
        {
            var conduit = new RgbaColor(255, 240, 120, (byte)(40 + 180 * state.Charge));
            DrawSeg(P(-19, 1), P(-20, -4), conduit, 3f);
        }

        // Cantina dressing: tables with a view.
        foreach ((double tx, double ty) in stackalloc (double, double)[] { (8, 7.5), (11, 6), (14, 7.5) })
        {
            (float cx2, float cy2) = P(tx, ty);
            _renderer.DrawCircle(cx2, cy2, 0.9f * scale, null, InnerLine, 1.5f);
        }

        // Droid pirate infantry.
        DeckPlan.GetDroids(simTime, _droids);
        foreach (DeckPlan.Droid droid in _droids)
        {
            (float dx, float dy) = P(droid.X, droid.Y);
            _renderer.DrawCircle(dx, dy, 0.5f * scale, DroidColor, DroidColor);
            float fx = dx + (float)Math.Cos(droid.FacingRad) * scale * 0.8f;
            float fy = dy - (float)Math.Sin(droid.FacingRad) * scale * 0.8f;
            DrawSeg((dx, dy), (fx, fy), DroidColor, 1.5f);
            _renderer.DrawText(dx, dy - 0.9f * scale, droid.Name, TextDim, "8px monospace", TextAlign.Center);
        }

        // Consoles.
        foreach (DeckPlan.ConsoleSpot console in DeckPlan.Consoles)
        {
            (float sx, float sy) = P(console.X, console.Y);
            bool near = Math.Sqrt((state.AvatarX - console.X) * (state.AvatarX - console.X)
                                + (state.AvatarY - console.Y) * (state.AvatarY - console.Y)) <= DeckPlan.InteractRadius;
            RgbaColor c = near ? ConsoleNear : ConsoleGlow;
            _renderer.DrawCircle(sx, sy, near ? 5f : 3.5f, c, c);
            _renderer.DrawText(sx, sy - 10, console.Label, near ? ConsoleNear : TextDim,
                near ? "bold 10px monospace" : "9px monospace", TextAlign.Center);
            if (near)
            {
                _renderer.DrawText(sx, sy + 20, "[E]", ConsoleNear, "bold 11px monospace", TextAlign.Center);
            }
        }

        // The captain.
        (float ax, float ay) = P(state.AvatarX, state.AvatarY);
        _renderer.DrawCircle(ax, ay, 0.7f * scale, AvatarColor, AvatarColor);
        float hx = ax + (float)Math.Cos(state.HeadingRad) * scale * 1.1f;
        float hy = ay - (float)Math.Sin(state.HeadingRad) * scale * 1.1f;
        DrawSeg((ax, ay), (hx, hy), AvatarColor, 2f);

        _renderer.DrawText(ox, heightPx - 10,
            "WASD / arrows — move ∙ E — interact ∙ F — first person ∙ Q — back to the helm",
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
        Span<float> s = _scratch.AsSpan(0, 10);
        float u = scale * 0.9f;
        (float x, float y) = at;
        s[0] = x + 2.2f * u; s[1] = y;
        s[2] = x - 1.4f * u; s[3] = y + 1.1f * u;
        s[4] = x - 0.8f * u; s[5] = y;
        s[6] = x - 1.4f * u; s[7] = y - 1.1f * u;
        s[8] = x + 2.2f * u; s[9] = y;
        _renderer.DrawPolyline(s, ShuttleColor, 2f);
        _renderer.DrawCircle(x + 0.8f * u, y, 0.35f * u, ShuttleColor, ShuttleColor);
        if (Math.Sin(simTime * 0.003) > 0.6)
        {
            var beacon = new RgbaColor(120, 255, 160, 200);
            _renderer.DrawCircle(x - 1.2f * u, y, 2.5f, beacon, beacon);
        }
    }
}
