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
        int CargoUnits, double Charge, bool ShuttleAway, bool ElectricUniverse,
        bool Docked = false);

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
    private static readonly RgbaColor DoorShut = new(255, 180, 90, 220);   // amber airlock door, closed
    private static readonly RgbaColor DoorOpen = new(255, 180, 90, 90);    // retracted leaves, faded
    private static readonly RgbaColor DoorLocked = new(120, 140, 170, 210);// another berth's sealed hatch
    private const double DoorOpenRadius = 4.0;

    private readonly IRenderer _renderer;
    private readonly DeckPlan.Droid[] _droids = new DeckPlan.Droid[DeckPlan.MaxDroids];
    private readonly float[] _scratch = new float[32];

    public DeckView(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Draw(DeckPlan plan, int widthPx, int heightPx, double simTime, in State state, double panX = 0, double panY = 0)
    {
        _renderer.BeginFrame(widthPx, heightPx, Floor);

        float scale = Math.Min(widthPx / 64f, heightPx / 28f);

        // A whole-plan tactical frame (bare ship / lone room) centres on the plan origin; a docked
        // complex is far too long for the fixed frame, so it scrolls to keep the avatar centred
        // (FollowCam). Manual pan still nudges either.
        float ox = plan.FollowCam ? widthPx / 2f - (float)state.AvatarX * scale + (float)panX : widthPx / 2f + (float)panX;
        float oy = plan.FollowCam ? heightPx / 2f + (float)state.AvatarY * scale + (float)panY : heightPx / 2f + (float)panY;
        (float X, float Y) P(double dx, double dy) => (ox + (float)dx * scale, oy - (float)dy * scale);

        // Ship-only dressing (cargo crates, shuttle cradle, reactor, cantina tables) is hardcoded to
        // the ship's geometry — a bare haven room has none of it, but a docked complex still contains
        // the ship. Everything else (backdrops, walls, doors, labels, consoles, droids, the avatar) is
        // plan-driven and general.
        bool isShip = plan.ShipFixtures;

        // Room backdrops sit UNDER every vector overlay (walls, consoles, avatar, labels stay on top
        // for legibility — the hybrid look). Each is top-left at (X, Y) deck-units, W×H deck-units.
        // Registration is idempotent, so calling it per frame is cheap.
        foreach (DeckPlan.Backdrop bd in plan.Backdrops)
        {
            (float bx, float by) = P(bd.X, bd.Y);
            _renderer.DrawImage(_renderer.RegisterImage(bd.Url), bx, by, bd.W * scale, bd.H * scale, bd.Alpha);
        }

        for (int gx = -22; gx <= 28; gx += 4)
        {
            DrawSeg(P(gx, -9.6), P(gx, 9.6), new RgbaColor(255, 255, 255, 10), 1f);
        }

        foreach (DeckPlan.Wall w in plan.Walls)
        {
            RgbaColor color = w.IsWindow ? WindowLine : w.IsHull ? HullLine : InnerLine;
            DrawSeg(P(w.X1, w.Y1), P(w.X2, w.Y2), color, w.IsHull ? 2.5f : 1.5f);
        }

        // Automatic airlock doors (the docking tube): shut across the passage until you near them,
        // then they retract to a stub at each jamb. Purely visual — the passage is always walkable.
        foreach (DeckPlan.Door d in plan.Doors)
        {
            if (d.Locked)
            {
                // Another berth's sealed hatch — always shut, drawn cold (steel-blue), a real wall behind.
                DrawSeg(P(d.X1, d.Y1), P(d.X2, d.Y2), DoorLocked, 3.5f);
                continue;
            }
            double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
            bool open = Math.Sqrt((state.AvatarX - mx) * (state.AvatarX - mx)
                                + (state.AvatarY - my) * (state.AvatarY - my)) <= DoorOpenRadius;
            if (open)
            {
                // Retracted: a short leaf at each jamb (25% in from each end).
                DrawSeg(P(d.X1, d.Y1), P(d.X1 + (d.X2 - d.X1) * 0.25f, d.Y1 + (d.Y2 - d.Y1) * 0.25f), DoorOpen, 3f);
                DrawSeg(P(d.X2, d.Y2), P(d.X2 - (d.X2 - d.X1) * 0.25f, d.Y2 - (d.Y2 - d.Y1) * 0.25f), DoorOpen, 3f);
            }
            else
            {
                DrawSeg(P(d.X1, d.Y1), P(d.X2, d.Y2), DoorShut, 3.5f);
            }
        }

        foreach ((float lx, float ly, string text) in plan.RoomLabels)
        {
            _renderer.DrawText(P(lx, ly).X, P(lx, ly).Y, text, TextDim, "10px monospace", TextAlign.Center);
        }

        if (isShip)
        {
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

        }

        // Round tables (plan-driven: the ship's cantina, a haven bar) — a ring on the floor.
        foreach ((float tx, float ty) in plan.Tables)
        {
            (float cx2, float cy2) = P(tx, ty);
            _renderer.DrawCircle(cx2, cy2, 0.9f * scale, null, InnerLine, 1.5f);
        }

        // Droid pirate infantry (the ship's; a haven has none — DroidCount 0).
        plan.FillDroids(simTime, _droids);
        for (int di = 0; di < plan.DroidCount; di++)
        {
            DeckPlan.Droid droid = _droids[di];
            (float dx, float dy) = P(droid.X, droid.Y);
            _renderer.DrawCircle(dx, dy, 0.5f * scale, DroidColor, DroidColor);
            float fx = dx + (float)Math.Cos(droid.FacingRad) * scale * 0.8f;
            float fy = dy - (float)Math.Sin(droid.FacingRad) * scale * 0.8f;
            DrawSeg((dx, dy), (fx, fy), DroidColor, 1.5f);
            _renderer.DrawText(dx, dy - 0.9f * scale, droid.Name, TextDim, "8px monospace", TextAlign.Center);
        }

        // Consoles.
        foreach (DeckPlan.ConsoleSpot console in plan.Consoles)
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

        // Blind-UI audit finding: with the tube off-camera, nothing said the ship was docked or
        // how to go ashore — the tester could only guess "airlock" by genre convention.
        _renderer.DrawText(ox, heightPx - 10,
            state.Docked
                ? "docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ F — first person ∙ Q — helm"
                : "WASD / arrows — move ∙ E — interact ∙ F — first person ∙ Q — back to the helm",
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
