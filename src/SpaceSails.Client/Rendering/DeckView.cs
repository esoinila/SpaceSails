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

    /// <summary>#313 · Everything the surface excursion overlays on the grid: the timed dig channel
    /// (shovel + bar), a panic-dropped chest, own caches' ✗ marks, and the crude motion-tracker fan
    /// (moving blips by bearing/range, cadence-pulsed). Null off-surface — the ship draws none of it.</summary>
    public readonly record struct SurfaceHud(
        double DigProgress,          // <0 = not channeling
        double SiteX, double SiteY,
        bool HasDroppedChest, double DropX, double DropY,
        System.Collections.Generic.IReadOnlyList<(double Bearing, double Range)> Blips,
        int Cadence,                 // MotionTracker.Cadence as int
        string Readout,
        System.Collections.Generic.IReadOnlyList<(double X, double Y, bool Haunted)> CacheMarks,
        double Nerve,                // #317: 0..100 (NerveModel.Max = steady). Drawn in the OPPOSITE corner
        string NerveReadout,         // to the motion tracker — the channel-law corner gauge, never on the grid
        // #314: deployed sentries (with their 99-counter readout + a firing zap line) and the husks of
        // downed Old Ones — ON-grid marks, not corner widgets. Optional so #313 callers still compile.
        System.Collections.Generic.IReadOnlyList<(double X, double Y, string Counter, bool Dry, bool Firing, double AimX, double AimY)>? Bots = null,
        System.Collections.Generic.IReadOnlyList<(double X, double Y)>? Husks = null,
        // #324: the contextual surface keybar — the deploy/drop keys spelled out along the bottom while
        // they're live (a bot in the sling shows [T], a chest in hand shows [G]). #212 affordances-never-hide.
        string? KeyHints = null);

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
    private static readonly RgbaColor ReeverColor = new(230, 80, 70);   // #295: watchdog red
    private static readonly RgbaColor HuskColor = new(120, 70, 60, 150); // #314: a downed Old One's husk
    private static readonly RgbaColor BotColor = new(120, 210, 160);     // #314: a live sentry, gun-green
    private static readonly RgbaColor BotDim = new(90, 100, 110);        // #314: a dry sentry, gone quiet
    private static readonly RgbaColor SegLit = new(255, 90, 70);         // #314: the 99-counter, seven-segment red
    private static readonly RgbaColor SegDim = new(90, 50, 45, 200);     // #314: a frozen 00, dim glyph
    private static readonly RgbaColor ZapColor = new(180, 255, 210, 235);// #314: the sentry's zap line
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

    public void Draw(DeckPlan plan, int widthPx, int heightPx, double simTime, in State state,
        double panX = 0, double panY = 0, SurfaceHud? surface = null)
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

        // #313 surface ground overlays: own caches' ✗ marks and a panic-dropped chest (drawn under the
        // avatar/droids so a mover can stand on them).
        if (surface is { } hud)
        {
            foreach ((double mx, double my, bool haunted) in hud.CacheMarks)
            {
                (float sx, float sy) = P(mx, my);
                var xcol = haunted ? new RgbaColor(230, 120, 90, 230) : new RgbaColor(230, 210, 120, 230);
                _renderer.DrawText(sx, sy + 4, "✗", xcol, "bold 16px monospace", TextAlign.Center);
                if (haunted)
                {
                    _renderer.DrawText(sx, sy - 12, "yours · something walks near it", new RgbaColor(230, 120, 90, 170), "8px monospace", TextAlign.Center);
                }
            }
            if (hud.HasDroppedChest)
            {
                (float dx2, float dy2) = P(hud.DropX, hud.DropY);
                _renderer.DrawText(dx2, dy2 + 5, "🧰", new RgbaColor(200, 160, 90, 240), "15px monospace", TextAlign.Center);
                _renderer.DrawText(dx2, dy2 - 11, "dropped chest", new RgbaColor(200, 160, 90, 180), "8px monospace", TextAlign.Center);
            }
            // #314: husks of downed Old Ones — dim marks left where they fell (the forensic seed, #316).
            if (hud.Husks is { } husks)
            {
                foreach ((double hkx, double hky) in husks)
                {
                    (float sx, float sy) = P(hkx, hky);
                    _renderer.DrawCircle(sx, sy, 0.55f * scale, HuskColor, HuskColor);
                    _renderer.DrawText(sx, sy + 3, "×", new RgbaColor(90, 60, 60, 220), "bold 11px monospace", TextAlign.Center);
                }
            }
        }

        if (isShip)
        {
            // Cargo crates: one per unit aboard (in the top-port hold now — #295).
            for (int i = 0; i < Math.Min(state.CargoUnits, 12); i++)
            {
                (float cx, float cy) = P(-10 + (i % 4) * 1.9, 5 + (i / 4) * 1.6);
                DrawBox(cx, cy, 0.65f * scale, CrateColor);
            }

            // Shuttle in its cradle (bottom-port bay now — #295) — or away doing piracy.
            if (!state.ShuttleAway)
            {
                DrawShuttle(P(-6.5, -6.5), scale, simTime);
            }
            else
            {
                (float bx, float by) = P(-6.5, -6.5);
                _renderer.DrawText(bx, by, "— AWAY —", new RgbaColor(255, 170, 80, 200), "bold 11px monospace", TextAlign.Center);
                if (Math.Sin(simTime * 0.005) > 0)
                {
                    DrawSeg(P(-9, -9.9), P(-5, -9.9), new RgbaColor(255, 120, 80, 220), 3f);
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
            // #295: the Reevers read hostile — a red mark, not the crew's grey.
            bool reever = droid.Name == "Reever";
            RgbaColor mark = reever ? ReeverColor : DroidColor;
            _renderer.DrawCircle(dx, dy, (reever ? 0.6f : 0.5f) * scale, mark, mark);
            float fx = dx + (float)Math.Cos(droid.FacingRad) * scale * 0.8f;
            float fy = dy - (float)Math.Sin(droid.FacingRad) * scale * 0.8f;
            DrawSeg((dx, dy), (fx, fy), mark, 1.5f);
            _renderer.DrawText(dx, dy - 0.9f * scale, droid.Name, reever ? ReeverColor : TextDim, "8px monospace", TextAlign.Center);
        }

        // #314: deployed sentries — a gun-green mark (dim once dry), a zap line to the Old One it's
        // dropping, and its crude two-digit magazine readout riding above (seven-segment red, dim at 00).
        // Drawn ON the grid, not a corner widget — the counter is meant to be read from across the map.
        if (surface is { Bots: { } sentries })
        {
            foreach ((double bxr, double byr, string counter, bool dry, bool firing, double aimX, double aimY) in sentries)
            {
                (float sx, float sy) = P(bxr, byr);
                if (firing && !dry)
                {
                    (float zx, float zy) = P(aimX, aimY);
                    DrawSeg((sx, sy), (zx, zy), ZapColor, 1.6f);
                    _renderer.DrawCircle(zx, zy, 3f, ZapColor, ZapColor);
                }
                RgbaColor body = dry ? BotDim : BotColor;
                DrawBox(sx, sy, 0.55f * scale, body);
                _renderer.DrawCircle(sx, sy, 0.3f * scale, body, body);
                // The readout: a dark panel with the two big digits, so it reads like a magazine counter.
                float pw = 1.7f * scale, ph = 1.15f * scale;
                FillRect(sx - pw / 2, sy - 2.3f * scale, pw, ph, new RgbaColor(16, 10, 10, 225));
                _renderer.DrawText(sx, sy - 1.35f * scale, counter, dry ? SegDim : SegLit,
                    "bold 15px monospace", TextAlign.Center);
            }
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

        // #313 the dig channel: a shovel glyph over the captain and a crude progress bar — the
        // vulnerability window, drawn ON the grid so the player watches the tracker while it fills.
        if (surface is { DigProgress: >= 0 } dig)
        {
            _renderer.DrawText(ax, ay - 1.6f * scale, "⛏", new RgbaColor(255, 230, 140, 240), "bold 15px monospace", TextAlign.Center);
            float bw = 3.2f * scale, bh = 0.45f * scale;
            float bx0 = ax - bw / 2, by0 = ay + 1.1f * scale;
            FillRect(bx0, by0, bw, bh, new RgbaColor(20, 24, 30, 220));
            FillRect(bx0, by0, bw * (float)Math.Clamp(dig.DigProgress, 0, 1), bh, new RgbaColor(255, 200, 90, 240));
        }

        // #313 the motion tracker: a crude corner fan of MOVING blips (bearing/range), including
        // contacts beyond the grid edge — the early warning. Cadence pulses the blips as they close.
        if (surface is { } tHud)
        {
            DrawMotionTracker(widthPx, heightPx, simTime, tHud);
        }

        // #317 the nerve gauge: a crude deck-plan bar in the TOP-LEFT corner — the opposite corner from the
        // motion tracker (which owns top-right), never over the grid (the dig-channel channel law). On-planet
        // only: the SurfaceHud is null off-surface, so the ship draws none of it.
        if (surface is { } nHud)
        {
            DrawNerveGauge(simTime, nHud);
        }

        // Blind-UI audit finding: with the tube off-camera, nothing said the ship was docked or
        // how to go ashore — the tester could only guess "airlock" by genre convention. On the surface
        // the keybar turns contextual (#324): the deploy/drop keys spell themselves out while they matter.
        string bottomHint = surface is { KeyHints: { Length: > 0 } hints }
            ? hints
            : state.Docked
                ? "docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ F — first person ∙ Q — helm"
                : "WASD / arrows — move ∙ E — interact ∙ F — first person ∙ Q — back to the helm";
        _renderer.DrawText(ox, heightPx - 10, bottomHint, TextDim, "11px monospace", TextAlign.Center);

        _renderer.EndFrame();
    }

    private void FillRect(float x, float y, float w, float h, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 8);
        s[0] = x; s[1] = y; s[2] = x + w; s[3] = y; s[4] = x + w; s[5] = y + h; s[6] = x; s[7] = y + h;
        _renderer.DrawPolygon(s, color, color, 1f);
    }

    // The crude motion-tracker fan (top-right corner, screen-space): a graph-paper radar showing MOVING
    // contacts by bearing + range, clamped to the ring when beyond it. Blips pulse faster as they close.
    private static readonly RgbaColor TrackerRing = new(120, 200, 150, 150);
    private static readonly RgbaColor TrackerBlip = new(120, 255, 160, 230);

    private void DrawMotionTracker(int widthPx, int heightPx, double simTime, in SurfaceHud hud)
    {
        _ = heightPx;
        // #324 (owner: "the motion tracker is too small"): the star instrument of the excursion, sized to
        // read blips at a glance from mid-grid. Nearly doubled, inset from the top-right so no chrome
        // buries it, on an opaque dark disc so it never washes out over the regolith.
        float r = 78f;
        float cx = widthPx - r - 24f, cy = r + 34f;

        // The graph-paper fan: an opaque backing disc, two rings + crosshair.
        _renderer.DrawCircle(cx, cy, r + 6f, new RgbaColor(6, 11, 10, 235), TrackerRing, 1f);
        _renderer.DrawCircle(cx, cy, r, null, TrackerRing, 1.75f);
        _renderer.DrawCircle(cx, cy, r * 0.66f, null, new RgbaColor(120, 200, 150, 85), 1f);
        _renderer.DrawCircle(cx, cy, r * 0.33f, null, new RgbaColor(120, 200, 150, 70), 1f);
        DrawSeg((cx - r, cy), (cx + r, cy), new RgbaColor(120, 200, 150, 70), 1f);
        DrawSeg((cx, cy - r), (cx, cy + r), new RgbaColor(120, 200, 150, 70), 1f);
        _renderer.DrawText(cx, cy - r - 8, "MOTION TRACKER", TrackerRing, "bold 11px monospace", TextAlign.Center);

        // Cadence → blink phase. Silent(0) steady, up to Imminent(3) frantic.
        double hz = hud.Cadence switch { 3 => 6.0, 2 => 3.0, 1 => 1.3, _ => 0.0 };
        bool on = hz <= 0 || Math.Sin(simTime * 0.001 * hz * 2 * Math.PI) > -0.2;

        const double maxRange = 60.0; // du mapped to the ring's edge; farther clamps to the rim
        foreach ((double bearing, double range) in hud.Blips)
        {
            double rr = Math.Min(range / maxRange, 1.0) * (r - 6);
            // World bearing: +x = right, +y = port (up on screen) → screen y flips.
            float bx = cx + (float)(Math.Cos(bearing) * rr);
            float by = cy - (float)(Math.Sin(bearing) * rr);
            var col = on ? TrackerBlip : new RgbaColor(120, 255, 160, 90);
            _renderer.DrawCircle(bx, by, range > maxRange ? 3.4f : 4.6f, col, col);
        }

        _renderer.DrawText(cx, cy + r + 14, hud.Readout, TrackerRing, "10px monospace", TextAlign.Center);
    }

    // #317 the nerve gauge (top-left, screen-space): a crude deck-plan bar — full teal = steady hands,
    // draining through amber to blood as the regolith's stressors fray the captain. The whole gauge trembles
    // harder the lower the nerve falls (the "tremor in the glyph" the flavor ladder names), and a house-voice
    // line reads out beneath it. Display-only — this slice never rolls, exits, or ends a run (#226 owns that).
    private static readonly RgbaColor NerveFrame = new(150, 170, 190, 175);
    private void DrawNerveGauge(double simTime, in SurfaceHud hud)
    {
        double frac = NerveModel.Fraction(hud.Nerve);
        NerveModel.NerveBand band = NerveModel.BandFor(hud.Nerve);
        RgbaColor fill = band switch
        {
            NerveModel.NerveBand.Steady => new RgbaColor(120, 220, 170, 235),
            NerveModel.NerveBand.Rattled => new RgbaColor(185, 220, 130, 235),
            NerveModel.NerveBand.Shaken => new RgbaColor(230, 200, 90, 240),
            NerveModel.NerveBand.Fraying => new RgbaColor(235, 150, 80, 245),
            _ => new RgbaColor(230, 80, 70, 250),
        };

        // The trembling scales with how much nerve is GONE — steady hands are still, shot ones shake hard.
        double tremor = 1.0 - frac;
        float jx = (float)(Math.Sin(simTime * 0.02) * tremor * tremor * 3.0);
        float jy = (float)(Math.Cos(simTime * 0.017) * tremor * tremor * 2.0);

        // #324 (owner: "let's make sanity visible :-D"): a big, plainly-labelled corner gauge on its own
        // dark plate — sits below the first-person toggle (moved aside on-surface) so nothing buries it.
        float x0 = 18f + jx, y0 = 30f + jy;
        const float w = 210f, h = 18f;

        FillRect(x0 - 8f, y0 - 20f, w + 16f, h + 42f, new RgbaColor(6, 11, 10, 205));  // the backing plate
        _renderer.DrawText(x0, y0 - 6, "SANITY", NerveFrame, "bold 11px monospace", TextAlign.Left);
        FillRect(x0, y0, w, h, new RgbaColor(14, 18, 24, 220));           // the empty channel
        FillRect(x0, y0, w * (float)frac, h, fill);                       // the fill
        for (int i = 1; i < 5; i++)                                       // crude deck-plan segments
        {
            float tx = x0 + w * i / 5f;
            DrawSeg((tx, y0), (tx, y0 + h), new RgbaColor(10, 14, 20, 160), 1f);
        }
        DrawRectOutline(x0, y0, w, h, NerveFrame);                        // the frame
        _renderer.DrawText(x0, y0 + h + 13, hud.NerveReadout, fill, "11px monospace", TextAlign.Left);
    }

    private void DrawRectOutline(float x, float y, float w, float h, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 10);
        s[0] = x; s[1] = y; s[2] = x + w; s[3] = y; s[4] = x + w; s[5] = y + h;
        s[6] = x; s[7] = y + h; s[8] = x; s[9] = y;
        _renderer.DrawPolyline(s, color, 1.5f);
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
