using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// The boarding run (M14): pilot the shuttle from the mothership's bay across the gap to the
/// prey, soft-dock, and the droid infantry does the rest. A compressed local-frame minigame,
/// but its difficulty is the REAL capture geometry: the prey's screen drift comes from the
/// live relative velocity and the crossing from the live distance — a sloppy pass by the
/// mothership makes for a hard run in the shuttle. Docking completes the boarding instantly;
/// the alternative is waiting out the passive shuttle-timer like a deckhand.
/// </summary>
public sealed class ShuttleFlightView
{
    public enum RunState { Flying, Docked, WindowLost }

    /// <summary>Local minigame state, real-time (dtReal), independent of sim warp.</summary>
    public sealed class Run
    {
        public double ShuttleX, ShuttleY;      // px, scene space (0,0 = center)
        public double VelocityX, VelocityY;    // px/s
        public double PreyY;                   // prey drifts vertically
        public double PreyDriftPxPerSec;
        public double GapPx;                   // horizontal distance bay → prey airlock
        public string PreyCallsign = "";
        public bool PreyIsPod;
        public RunState State = RunState.Flying;
        public double StateTime;               // seconds in current state
    }

    private const double Accel = 150;          // px/s²
    private const double MaxSpeed = 170;       // px/s
    private const double DockSpeedLimit = 55;  // px/s — soft dock or bounce
    private const double DockZonePx = 26;

    private static readonly RgbaColor Space = new(3, 5, 10);
    private static readonly RgbaColor HullLine = new(200, 210, 225);
    private static readonly RgbaColor SailLine = new(140, 190, 235, 190);
    private static readonly RgbaColor ShuttleColor = new(150, 210, 255);
    private static readonly RgbaColor ThrustColor = new(255, 180, 90, 220);
    private static readonly RgbaColor HudText = new(150, 240, 210, 210);
    private static readonly RgbaColor DockGlow = new(120, 255, 160);

    private readonly IRenderer _renderer;
    private readonly float[] _scratch = new float[32];

    public ShuttleFlightView(IRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>Start a run from live capture geometry.</summary>
    public static Run Launch(double distanceMeters, double relSpeedMeters, string callsign, bool isPod)
    {
        return new Run
        {
            ShuttleX = -280,
            ShuttleY = 40,
            GapPx = 220 + 200 * Math.Min(1, distanceMeters / CaptureRule.CaptureRadiusMeters),
            PreyDriftPxPerSec = 55 * Math.Min(1, relSpeedMeters / CaptureRule.MaxRelativeSpeed),
            PreyCallsign = callsign,
            PreyIsPod = isPod,
        };
    }

    /// <summary>Advance physics; returns true while the run is still in progress.</summary>
    public static void Update(Run run, double dtReal, bool up, bool down, bool left, bool right, bool windowStillOpen)
    {
        double dt = Math.Min(dtReal, 0.05);
        run.StateTime += dt;

        if (run.State != RunState.Flying)
        {
            return;
        }

        if (!windowStillOpen)
        {
            run.State = RunState.WindowLost;
            run.StateTime = 0;
            return;
        }

        double ax = (right ? Accel : 0) - (left ? Accel : 0);
        // Screen y grows downward: W must push the shuttle UP the screen.
        double ay = (down ? Accel : 0) - (up ? Accel : 0);
        run.VelocityX += ax * dt;
        run.VelocityY += ay * dt;
        double speed = Math.Sqrt(run.VelocityX * run.VelocityX + run.VelocityY * run.VelocityY);
        if (speed > MaxSpeed)
        {
            run.VelocityX *= MaxSpeed / speed;
            run.VelocityY *= MaxSpeed / speed;
        }

        run.ShuttleX += run.VelocityX * dt;
        run.ShuttleY += run.VelocityY * dt;

        // The prey slides — the mothership's sloppy pass, made visible.
        run.PreyY += run.PreyDriftPxPerSec * dt * Math.Sin(run.StateTime * 0.35);

        // Dock check against the prey's airlock.
        double dx = run.ShuttleX - run.GapPx / 2;
        double dy = run.ShuttleY - run.PreyY;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < DockZonePx)
        {
            if (speed <= DockSpeedLimit)
            {
                run.State = RunState.Docked;
                run.StateTime = 0;
            }
            else
            {
                // Too hot: bounce off the hull.
                run.VelocityX = -run.VelocityX * 0.5;
                run.VelocityY = -run.VelocityY * 0.5;
                run.ShuttleX += run.VelocityX * 0.06;
                run.ShuttleY += run.VelocityY * 0.06;
            }
        }
    }

    public void Draw(int widthPx, int heightPx, double simTime, Run run,
        bool up, bool down, bool left, bool right, double windowSecondsLeftHint)
    {
        _renderer.BeginFrame(widthPx, heightPx, Space);
        float cx = widthPx / 2f, cy = heightPx / 2f;

        // Starfield.
        uint h = 12345;
        for (int i = 0; i < 70; i++)
        {
            h = h * 1664525u + 1013904223u;
            float sx = (h >> 8) % 1000 / 1000f * widthPx;
            h = h * 1664525u + 1013904223u;
            float sy = (h >> 8) % 1000 / 1000f * heightPx;
            var star = ((h >> 4) & 7) == 0 ? new RgbaColor(230, 235, 245, 200) : new RgbaColor(160, 170, 190, 110);
            _renderer.DrawCircle(sx, sy, ((h >> 6) & 3) == 0 ? 1.3f : 0.7f, star, star);
        }

        // The mothership: bay edge on the left.
        DrawMothership(cx + (float)(-run.GapPx / 2 - 90), cy + 20, simTime);

        // The prey, drifting.
        float preyX = cx + (float)(run.GapPx / 2);
        float preyY = cy + (float)run.PreyY;
        if (run.PreyIsPod)
        {
            DrawPodPrey(preyX + 55, preyY, simTime);
        }
        else
        {
            DrawFreighterPrey(preyX + 95, preyY, simTime);
        }

        // Airlock dock zone.
        var zone = new RgbaColor(120, 255, 160, run.State == RunState.Docked ? (byte)220 : (byte)90);
        _renderer.DrawCircle(preyX, preyY, (float)DockZonePx, null, zone, 1.5f);
        _renderer.DrawText(preyX, preyY - (float)DockZonePx - 6, "AIRLOCK", zone, "9px monospace", TextAlign.Center);

        // The shuttle + RCS puffs.
        float shX = cx + (float)run.ShuttleX, shY = cy + (float)run.ShuttleY;
        DrawShuttle(shX, shY, run.VelocityX, run.VelocityY);
        if (run.State == RunState.Flying)
        {
            if (left) Puff(shX + 14, shY, 1, 0);
            if (right) Puff(shX - 14, shY, -1, 0);
            if (up) Puff(shX, shY + 12, 0, 1);
            if (down) Puff(shX, shY - 12, 0, -1);
        }

        // HUD.
        double gap = Math.Sqrt(Math.Pow(run.ShuttleX - run.GapPx / 2, 2) + Math.Pow(run.ShuttleY - run.PreyY, 2));
        double speed = Math.Sqrt(run.VelocityX * run.VelocityX + run.VelocityY * run.VelocityY);
        var speedColor = speed <= DockSpeedLimit ? DockGlow : new RgbaColor(255, 170, 80);
        _renderer.DrawText(12, 22, $"BOARDING RUN — {run.PreyCallsign.ToUpperInvariant()}", HudText, "bold 13px monospace");
        _renderer.DrawText(12, 40, $"range {gap:F0}  ∙  closing {speed:F0} (dock ≤ {DockSpeedLimit})", speedColor, "11px monospace");
        _renderer.DrawText(widthPx - 12, 22, windowSecondsLeftHint > 0 ? $"window steady" : "window at risk",
            HudText, "11px monospace", TextAlign.Right);

        string footer = run.State switch
        {
            RunState.Docked => "SOFT DOCK — the droids swarm aboard 🏴‍☠️",
            RunState.WindowLost => "WINDOW LOST — auto-return to the bay",
            _ => "WASD / arrows — thrust ∙ dock slow at the airlock ∙ Q — abort",
        };
        _renderer.DrawText(cx, heightPx - 14, footer,
            run.State == RunState.Docked ? DockGlow : HudText, "bold 12px monospace", TextAlign.Center);

        _renderer.EndFrame();
    }

    private void DrawShuttle(float x, float y, double vx, double vy)
    {
        double heading = Math.Abs(vx) + Math.Abs(vy) > 5 ? Math.Atan2(vy, vx) : 0;
        float c = (float)Math.Cos(heading), s = (float)Math.Sin(heading);
        (float X, float Y) R(float px, float py) => (x + px * c - py * s, y + px * s + py * c);

        Span<float> p = _scratch.AsSpan(0, 10);
        (p[0], p[1]) = R(14, 0);
        (p[2], p[3]) = R(-9, 7);
        (p[4], p[5]) = R(-5, 0);
        (p[6], p[7]) = R(-9, -7);
        (p[8], p[9]) = R(14, 0);
        _renderer.DrawPolyline(p, ShuttleColor, 2f);
        (float cxp, float cyp) = R(5, 0);
        _renderer.DrawCircle(cxp, cyp, 2.5f, ShuttleColor, ShuttleColor);
    }

    private void Puff(float x, float y, int dx, int dy)
    {
        Span<float> p = _scratch.AsSpan(0, 4);
        p[0] = x; p[1] = y; p[2] = x + dx * 10; p[3] = y + dy * 10;
        _renderer.DrawPolyline(p, ThrustColor, 3f);
    }

    private void DrawMothership(float x, float y, double simTime)
    {
        // Stern quarter of the pirate ship with the open bay.
        Span<float> p = _scratch.AsSpan(0, 12);
        p[0] = x - 120; p[1] = y - 70; p[2] = x + 60; p[3] = y - 70;
        p[4] = x + 60; p[5] = y - 18; // bay top lip
        _renderer.DrawPolyline(p[..6], HullLine, 2.5f);
        p[0] = x + 60; p[1] = y + 18; p[2] = x + 60; p[3] = y + 70; p[4] = x - 120; p[5] = y + 70;
        _renderer.DrawPolyline(p[..6], HullLine, 2.5f);
        // Bay mouth glow.
        var bay = new RgbaColor(120, 255, 160, (byte)(90 + 60 * Math.Sin(simTime * 0.003)));
        p[0] = x + 60; p[1] = y - 18; p[2] = x + 60; p[3] = y + 18;
        _renderer.DrawPolyline(p[..4], bay, 3f);
        // Sail hint.
        p[0] = x - 118; p[1] = y - 66; p[2] = x - 80; p[3] = y - 30;
        _renderer.DrawPolyline(p[..4], SailLine, 1.5f);
        _renderer.DrawText(x - 30, y - 78, "YOUR SHIP", HudText, "9px monospace", TextAlign.Center);
    }

    private void DrawFreighterPrey(float x, float y, double simTime)
    {
        Span<float> p = _scratch.AsSpan(0, 16);
        p[0] = x - 60; p[1] = y; p[2] = x - 40; p[3] = y - 16;
        p[4] = x + 50; p[5] = y - 16; p[6] = x + 64; p[7] = y;
        p[8] = x + 50; p[9] = y + 16; p[10] = x - 40; p[11] = y + 16; p[12] = x - 60; p[13] = y;
        _renderer.DrawPolyline(p[..14], HullLine, 2f);
        double throb = 0.6 + 0.4 * Math.Sin(simTime * 0.003);
        var glow = new RgbaColor(255, 170, 80, (byte)(120 + 80 * throb));
        _renderer.DrawCircle(x + 58, y - 8, 3f, glow, glow);
        _renderer.DrawCircle(x + 58, y + 8, 3f, glow, glow);
    }

    private void DrawPodPrey(float x, float y, double simTime)
    {
        _renderer.DrawCircle(x - 18, y, 14, null, HullLine, 2f);
        _renderer.DrawCircle(x + 18, y, 14, null, HullLine, 2f);
        Span<float> p = _scratch.AsSpan(0, 4);
        p[0] = x - 18; p[1] = y - 14; p[2] = x + 18; p[3] = y - 14;
        _renderer.DrawPolyline(p, HullLine, 2f);
        p[0] = x - 18; p[1] = y + 14; p[2] = x + 18; p[3] = y + 14;
        _renderer.DrawPolyline(p, HullLine, 2f);
        if (Math.Sin(simTime * 0.004) > 0.4)
        {
            var beacon = new RgbaColor(255, 90, 90, 220);
            _renderer.DrawCircle(x, y - 20, 2.5f, beacon, beacon);
        }
    }
}
