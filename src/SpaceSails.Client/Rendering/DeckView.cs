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
        bool Docked = false,
        // #330 (owner: "we could have the sanity meter visible when we walk around"): the nerve gauge
        // rides EVERY walk mode — surface, haven/bar ashore, and aboard the ship (compact, a whisper) —
        // but never flight (the map view has its own instruments and never draws a DeckView). ShowNerve
        // gates it; NerveCompact draws the subtler aboard size that must clear the deck chrome.
        double Nerve = 0, string NerveReadout = "", bool ShowNerve = false, bool NerveCompact = false);

    /// <summary>#313 · Everything the surface excursion overlays on the grid: the timed dig channel
    /// (shovel + bar), a panic-dropped chest, own caches' ✗ marks, and the crude motion-tracker fan
    /// (moving blips by bearing/range, cadence-pulsed). Null off-surface — the ship draws none of it.</summary>
    public readonly record struct SurfaceHud(
        double DigProgress,          // <0 = not channeling
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
        string? KeyHints = null,
        // #327 the ship calls home: the mothership's in-voice orbit line, painted plainly across the top
        // (the #324 HUD-visibility law). Null when the excursion carries no orbit risk. Severity colours
        // it: 0 calm teal (steady), 1 amber (slipping), 2 red (failing / lost) — the maroon, never silent.
        string? OrbitComms = null,
        int OrbitSeverity = 0,
        // COMMS-LOSS (owner, cruise 2026-07-19): the mothership downlink phase colouring the orbit line —
        // 0 nominal (paint live), 1 degraded (greyed, faint static), 2 blackout (dim, flickering static,
        // the frozen last-known value). The OrbitComms string already carries the honest stale banner; this
        // just drives the visual static/grey so the readout LOOKS lost, not merely worded so.
        int CommsState = 0,
        // Lane-1 (owner, 2026-07-18: "advertise the dig and bot options in text under the motion
        // detector"): short contextual lines seated BENEATH the tracker readout in the left instrument
        // column — the dig-site and sentry affordances spelled out. Column chrome only, never over the
        // grid (the OverlayBands / dig-channel-watch law). Optional so earlier callers still compile.
        System.Collections.Generic.IReadOnlyList<string>? TrackerCaptions = null,
        // Beach-comber kit (owner, 2026-07-18: "some kind of grid system onto planet Miranda for marking
        // the checked squares on that visit"): the per-visit swept grid — each probed square at its centre,
        // Hard = the shovel rang off bedrock. Drawn as a subtle dug/checked glyph ON the regolith, under
        // the movers. Optional so earlier callers still compile.
        System.Collections.Generic.IReadOnlyList<(double X, double Y, bool Hard)>? SweptSquares = null,
        // #371 Phase 3 · EXPEDITION FOG OF WAR. DarkRegions = each forced chamber's axis-aligned bounds and
        // its visibility state (0 unseen — a hatched void, walls/consoles hidden; 1 explored — drawn dim;
        // 2 visible — drawn lit). Echoes = fading "movement was here" ripples a contact left when it slipped
        // behind cover. Both empty/absent off an expedition site (open terrain draws exactly as before).
        System.Collections.Generic.IReadOnlyList<(double X0, double Y0, double X1, double Y1, int State)>? DarkRegions = null,
        System.Collections.Generic.IReadOnlyList<(double X, double Y, double Alpha)>? Echoes = null);

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
    private static readonly RgbaColor SegWarn = new(255, 185, 70);       // #314: magazine under 25 — warming amber
    private static readonly RgbaColor SegAlarm = new(255, 45, 35);       // #314: magazine under 10 — hot alarm red
    private static readonly RgbaColor ZapColor = new(180, 255, 210, 235);// #314: the sentry's zap line
    private static readonly RgbaColor TextDim = new(140, 160, 180, 170);

    // #348 (owner, 2026-07-18: "make these room texts have better contrast … the Med Bay should stand out
    // from the cabins more … make it the shiny clean room that stands out from the bunk rooms. Like the
    // exception that makes the role.. it can look old and used but clean."). The room labels used to draw
    // in the dim grey TextDim, which the cabin art JPGs swallowed. Now every room label rides a subtle
    // dark backing plate (the house sentry-counter / SANITY-plate idiom) under a brighter fill, so the
    // schematic reads over the panels. MED BAY is the deliberate exception — the one clean room among the
    // grubby bunks: a whiter, cooler label on a cleaner plate with a thin cyan-white keyline.
    private static readonly RgbaColor RoomLabelText = new(214, 228, 242, 245);    // brighter than the old TextDim
    private static readonly RgbaColor RoomLabelPlate = new(8, 12, 18, 170);       // subtle dark backing, reads over art
    private static readonly RgbaColor MedBayText = new(240, 250, 255, 252);       // clean-room white, faint cool cast
    private static readonly RgbaColor MedBayPlate = new(16, 26, 32, 165);         // a cleaner, cooler plate than the bunks
    private static readonly RgbaColor MedBayKeyline = new(150, 222, 236, 155);    // the tidy edge — a thin cyan-white keyline
    // #371 Phase 3 · expedition fog-of-war palette. An UNSEEN forced chamber is a dark hatched void (unknown
    // ground behind a freshly-forced door); an EXPLORED one (seen, now out of sight) draws in a cold dim
    // slate; a VISIBLE one draws normally. Echoes ripple in the tracker's own green — "movement was here".
    private static readonly RgbaColor VoidFill = new(4, 7, 12, 214);
    private static readonly RgbaColor VoidHatch = new(34, 46, 62, 90);
    private static readonly RgbaColor VoidText = new(90, 110, 135, 150);
    private static readonly RgbaColor ExploredWall = new(74, 90, 112, 140);
    private static readonly RgbaColor ExploredText = new(120, 140, 162, 120);
    private static readonly RgbaColor EchoColor = new(120, 200, 150, 255);

    private static readonly RgbaColor DoorShut = new(255, 180, 90, 220);   // amber airlock door, closed
    private static readonly RgbaColor DoorOpen = new(255, 180, 90, 90);    // retracted leaves, faded
    private static readonly RgbaColor DoorLocked = new(120, 140, 170, 210);// another berth's sealed hatch
    private const double DoorOpenRadius = 4.0;

    private readonly IRenderer _renderer;
    private readonly DeckPlan.Droid[] _droids = new DeckPlan.Droid[DeckPlan.MaxDroids];
    private readonly float[] _scratch = new float[32];

    // #314 magazine-counter change-emphasis (owner, live playtest 2026-07-19: "make the round-count
    // numbers even bigger … I love to see those numbers move"). The DeckView draw is immediate-mode
    // and stateless, so a brief pop on decrement needs somewhere to remember each bot's last counter
    // and when it last changed. Keyed by the sentry's index in the per-frame Bots list (stable order —
    // a spent bot stays in place, dimmed). Pure rendering; never touches gameplay.
    private const float MagBasePx = 28f;    // the scoreboard digits — ~2× the old 15px label
    private const double MagFlash = 0.16;   // seconds a change stays lit + swollen
    private string[] _botCounters = System.Array.Empty<string>();
    private double[] _botCounterChanged = System.Array.Empty<double>();

    public DeckView(IRenderer renderer)
    {
        _renderer = renderer;
    }

    // #424 HULL-SHUDDER · the unison pause. When a shudder fires on a populated interior deck (the ship,
    // a haven bar/hall) the client hands a FROZEN npc-hold time here for the held-breath beat: every present
    // NPC/patron is filled at that ONE shared timestamp — so their idle thermal jitter and patrol/pace all
    // stop together (the synchronized freeze IS the feature) — and their heads turn up as one. Null the rest
    // of the time, when the deck fills live at simTime. The deck-shake itself rides the render pan (panX/panY),
    // a pure transient offset that never moves an entity anchor.
    // #424 THE UNEXPLAINED SIGNAL · the crew glance. A companion ambient event: when a faint distant buzzer
    // sounds off-deck the STAFF (not the drinking patrons) briefly catch each other's eye — <paramref
    // name="crewGlance"/> turns every working crew member (barkeep, customs, the ship's own droids) to face
    // the nearest other crew member for the beat, a synchronized look. The patrons keep animating, oblivious.
    public void Draw(DeckPlan plan, int widthPx, int heightPx, double simTime, in State state,
        double panX = 0, double panY = 0, SurfaceHud? surface = null, double? npcHoldTime = null,
        bool crewGlance = false)
    {
        _renderer.BeginFrame(widthPx, heightPx, Floor);

        float scale = Math.Min(widthPx / 64f, heightPx / 28f);

        // A whole-plan tactical frame (bare ship / lone room) centres on the plan origin; a docked
        // complex is far too long for the fixed frame, so it scrolls to keep the avatar centred
        // (FollowCam). Manual pan still nudges either.
        float ox = plan.FollowCam ? widthPx / 2f - (float)state.AvatarX * scale + (float)panX : widthPx / 2f + (float)panX;
        float oy = plan.FollowCam ? heightPx / 2f + (float)state.AvatarY * scale + (float)panY : heightPx / 2f + (float)panY;
        (float X, float Y) P(double dx, double dy) => (ox + (float)dx * scale, oy - (float)dy * scale);

        // #371 Phase 3 fog: the visibility state of a point against the forced-chamber overlay — -1 = not in
        // any chamber (draw as normal), 0 = unseen (hidden under the void), 1 = explored (dim), 2 = visible.
        var darkRegions = surface?.DarkRegions;
        int DarkState(double x, double y)
        {
            if (darkRegions is null)
            {
                return -1;
            }
            int best = -1;
            foreach ((double x0, double y0, double x1, double y1, int st) in darkRegions)
            {
                if (x >= x0 && x <= x1 && y >= y0 && y <= y1 && st > best)
                {
                    best = st; // a point in overlapping rects takes the most-revealed state
                }
            }
            return best;
        }

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

        // #371 Phase 3 fog: paint the still-UNSEEN forced chambers as dark hatched voids — unknown ground
        // behind a freshly-forced door — over the floor/grid, under everything that follows (the walls and
        // consoles inside are skipped, so nothing pokes through). Explored/visible chambers get no void.
        if (darkRegions is { Count: > 0 })
        {
            foreach ((double x0, double y0, double x1, double y1, int st) in darkRegions)
            {
                if (st != 0)
                {
                    continue;
                }
                (float vx0, float vy0) = P(x0, y1); // deck +y is up on screen → y1 is the top edge
                float vw = (float)(x1 - x0) * scale, vh = (float)(y1 - y0) * scale;
                FillRect(vx0, vy0, vw, vh, VoidFill);
                for (float vhy = vy0 + 6f; vhy < vy0 + vh; vhy += 7f) // crude hatch
                {
                    DrawSeg((vx0, vhy), (vx0 + vw, vhy), VoidHatch, 1f);
                }
                _renderer.DrawText(vx0 + vw / 2f, vy0 + vh / 2f, "· ? ·", VoidText, "10px monospace", TextAlign.Center);
            }
        }

        foreach (DeckPlan.Wall w in plan.Walls)
        {
            // #371 Phase 3 fog: a wall inside a still-unseen forced chamber is hidden (the room is unknown
            // until the captain looks in); one in an explored-but-out-of-sight chamber draws dim.
            int ws = DarkState((w.X1 + w.X2) / 2.0, (w.Y1 + w.Y2) / 2.0);
            if (ws == 0)
            {
                continue;
            }
            RgbaColor color = ws == 1 ? ExploredWall : w.IsWindow ? WindowLine : w.IsHull ? HullLine : InnerLine;
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

        // #348: each room label on its own dark backing plate for contrast over the art panels, with
        // MED BAY drawn as the clean-room exception (see the RoomLabel* colours above).
        foreach ((float lx, float ly, string text) in plan.RoomLabels)
        {
            int ls = DarkState(lx, ly); // #371 Phase 3 fog: hide an unseen chamber's label, dim an explored one
            if (ls == 0)
            {
                continue;
            }
            (float lxp, float lyp) = P(lx, ly);
            if (ls == 1)
            {
                _renderer.DrawText(lxp, lyp, text, ExploredText, "10px monospace", TextAlign.Center);
            }
            else
            {
                DrawRoomLabel(lxp, lyp, text, medBay: text == "MED BAY");
            }
        }

        // #313 surface ground overlays: own caches' ✗ marks and a panic-dropped chest (drawn under the
        // avatar/droids so a mover can stand on them).
        if (surface is { } hud)
        {
            // Beach-comber kit: the per-visit swept grid, drawn FIRST so every other ground mark sits on
            // top. A checked square is a faint dug divot (a small ring + tick); a bedrock square rings off
            // with a dim ✕ — the sweep at a glance, in the deck-plan NetHack idiom (subtle, never loud).
            if (hud.SweptSquares is { } swept)
            {
                foreach ((double swx, double swy, bool hard) in swept)
                {
                    (float sx, float sy) = P(swx, swy);
                    if (hard)
                    {
                        _renderer.DrawText(sx, sy + 3, "✕", new RgbaColor(120, 110, 95, 150), "10px monospace", TextAlign.Center);
                    }
                    else
                    {
                        _renderer.DrawCircle(sx, sy, 0.35f * scale, null, new RgbaColor(110, 130, 120, 130), 1f);
                        _renderer.DrawText(sx, sy + 3, "·", new RgbaColor(120, 150, 135, 160), "10px monospace", TextAlign.Center);
                    }
                }
            }
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
            // #371 Phase 3: movement echoes — where a contact was last seen before it slipped behind cover.
            // A dim tracker-green ripple that fades over its life; "here was movement before" (owner's ask),
            // making the motion tracker's through-wall blips all the more exciting to chase.
            if (hud.Echoes is { } echoes)
            {
                foreach ((double ex2, double ey2, double alpha) in echoes)
                {
                    (float sx, float sy) = P(ex2, ey2);
                    byte a = (byte)Math.Clamp(alpha * 180.0, 0, 180);
                    var ring = new RgbaColor(EchoColor.R, EchoColor.G, EchoColor.B, a);
                    _renderer.DrawCircle(sx, sy, (0.35f + 0.5f * (float)alpha) * scale, null, ring, 1.2f);
                    _renderer.DrawText(sx, sy + 3, "·", ring, "10px monospace", TextAlign.Center);
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
        // #424 HULL-SHUDDER: during the unison pause the NPCs are filled at the FROZEN onset time (all their
        // simTime-driven idle jitter / patrol / pace stop together — the synchronized held breath), and their
        // heads turn up as one (facing snapped screen-up). A Reever is never a patron, so it keeps its facing.
        bool headsUp = npcHoldTime.HasValue;
        plan.FillDroids(npcHoldTime ?? simTime, _droids);
        // #424 THE UNEXPLAINED SIGNAL: pre-compute each working crew member's glance — the facing toward the
        // NEAREST other crew member — so the barkeep and the dock-hand catch each other's eye as one. Only
        // built when a signal is glancing; a Reever or a drinking patron is never crew (StaffFacing skips them).
        double?[]? glance = crewGlance ? BuildCrewGlance(plan.DroidCount) : null;
        for (int di = 0; di < plan.DroidCount; di++)
        {
            DeckPlan.Droid droid = _droids[di];
            (float dx, float dy) = P(droid.X, droid.Y);
            // #295: the Reevers read hostile — a red mark, not the crew's grey.
            bool reever = droid.Name == "Reever";
            RgbaColor mark = reever ? ReeverColor : DroidColor;
            _renderer.DrawCircle(dx, dy, (reever ? 0.6f : 0.5f) * scale, mark, mark);
            // Heads up as one (hull-shudder pause), or the crew catch each other's eye (unexplained signal),
            // else the droid's own facing. The shudder pause wins if both somehow overlap.
            double facing = headsUp && !reever ? Math.PI / 2
                : glance?[di] ?? droid.FacingRad;
            float fx = dx + (float)Math.Cos(facing) * scale * 0.8f;
            float fy = dy - (float)Math.Sin(facing) * scale * 0.8f;
            DrawSeg((dx, dy), (fx, fy), mark, 1.5f);
            _renderer.DrawText(dx, dy - 0.9f * scale, droid.Name, reever ? ReeverColor : TextDim, "8px monospace", TextAlign.Center);
        }

        // #314: deployed sentries — a gun-green mark (dim once dry), a zap line to the Old One it's
        // dropping, and its crude two-digit magazine readout riding above (seven-segment red, dim at 00).
        // Drawn ON the grid, not a corner widget — the counter is meant to be read from across the map.
        if (surface is { Bots: { } sentries })
        {
            // Keep the per-bot change-tracking arrays as long as the deployed list (grows only).
            if (_botCounters.Length < sentries.Count)
            {
                System.Array.Resize(ref _botCounters, sentries.Count);
                System.Array.Resize(ref _botCounterChanged, sentries.Count);
            }
            for (int i = 0; i < sentries.Count; i++)
            {
                (double bxr, double byr, string counter, bool dry, bool firing, double aimX, double aimY) = sentries[i];
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

                // The number changed this frame? Stamp the moment so the pop below can key off it. (First
                // sight of a bot counts as a change — a one-off blip as it deploys, which reads as intent.)
                if (_botCounters[i] != counter)
                {
                    _botCounters[i] = counter;
                    _botCounterChanged[i] = simTime;
                }
                double since = simTime - _botCounterChanged[i];
                float pop = since >= 0 && since < MagFlash ? (float)(1.0 - since / MagFlash) : 0f;

                // #314 low-ammo warning (owner, 2026-07-19): the magazine's house red is the identity down
                // the top of the belt; it warms to amber under 25 and snaps to a hot alarm red under 10 —
                // the small honest touch the counter never had. Non-numeric readouts keep the house red.
                RgbaColor digit = dry ? SegDim : SegLit;
                if (!dry && int.TryParse(counter, out int rounds))
                {
                    if (rounds < 10) digit = SegAlarm;
                    else if (rounds < 25) digit = SegWarn;
                }
                // On a decrement the digits flash brighter and swell for a frame or two — the owner loves
                // to watch them move, so the change gets a subtle brighten-toward-white + size pop.
                if (!dry && pop > 0f) digit = LerpToWhite(digit, 0.7f * pop);
                float fontPx = MagBasePx * (1f + 0.16f * pop);

                // The readout: a dark scoreboard panel with the two big digits, anchored above the bot so
                // it never covers the mark or its neighbours. Plate stays a steady size; only the number pops.
                float pw = 3.0f * scale, ph = 2.0f * scale;
                float plateBottom = sy - 0.8f * scale;      // clears the bot box (half 0.55·scale) with a gap
                float plateTop = plateBottom - ph;
                FillRect(sx - pw / 2, plateTop, pw, ph, new RgbaColor(16, 10, 10, 225));
                float baseY = (plateTop + plateBottom) / 2f + fontPx * 0.35f; // optical centre for the fixed-px glyphs
                _renderer.DrawText(sx, baseY, counter, digit,
                    $"bold {fontPx:0.#}px monospace", TextAlign.Center);
            }
        }

        // Consoles.
        foreach (DeckPlan.ConsoleSpot console in plan.Consoles)
        {
            // #371 Phase 3 fog: a console inside an unseen chamber is unknown (hidden); an explored one is
            // dimmed. A still-sealed door's console sits OUTSIDE any chamber rect, so it always shows.
            if (DarkState(console.X, console.Y) == 0)
            {
                continue;
            }
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

        // #317/#330 the nerve gauge: a crude deck-plan bar in the TOP-LEFT column. On the surface it is the
        // full-size head of the instrument column (the tracker seats beneath it); aboard the ship and in a
        // haven it whispers (compact, tucked below the deck chrome). Shown in every walk mode, never flight.
        if (state.ShowNerve)
        {
            DrawNerveGauge(simTime, state.Nerve, state.NerveReadout, state.NerveCompact);
        }

        // #327 the ship calls home: the mothership's orbit line, painted plainly across the TOP-CENTRE —
        // the one channel the owner's Miranda maroon never had. Never buried (the #324 visibility law):
        // calm teal while it holds, amber as it slips, a pulsing red for the last call and the maroon.
        if (surface is { OrbitComms: { Length: > 0 } orbitLine } oHud)
        {
            RgbaColor color = oHud.OrbitSeverity switch
            {
                >= 2 => new RgbaColor(255, 90, 70, (byte)(170 + 85 * (0.5 + 0.5 * Math.Sin(simTime * 4.0)))),
                1 => new RgbaColor(255, 190, 100, 235),
                _ => new RgbaColor(130, 225, 205, 220),
            };
            // COMMS-LOSS: when the downlink is degraded/blacked out the orbit line is a STALE readout — drop
            // it to a cold signal-grey and flicker its alpha like breaking static (faster + deeper on a full
            // blackout), so the frozen last-known value LOOKS lost, not just worded so. The honesty is in the
            // banner text (SurfaceComms); this is the matching visual.
            if (oHud.CommsState > 0)
            {
                double flickerHz = oHud.CommsState >= 2 ? 11.0 : 6.0;
                double floor = oHud.CommsState >= 2 ? 0.28 : 0.55; // blackout drops darker between flickers
                double f = floor + (1.0 - floor) * (0.5 + 0.5 * Math.Sin(simTime * flickerHz));
                color = new RgbaColor(170, 180, 190, (byte)(255 * Math.Clamp(f, 0.0, 1.0)));
            }
            _renderer.DrawText(widthPx / 2f, 20, orbitLine, color, "13px monospace", TextAlign.Center);
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

    // #424 THE UNEXPLAINED SIGNAL · the crew glance. From the freshly-filled _droids, work out each WORKING
    // crew member's facing toward the nearest OTHER crew member — so the barkeep and the dock-hand (and, on
    // the bare ship, the ship's own droids) catch each other's eye as one. A drinking patron (a seated
    // regular, the Magpie) and a Reever are never crew, so their entry stays null (they keep their own
    // facing, oblivious to the buzzer). Returns a per-droid facing override, or null where there's no glance.
    private double?[] BuildCrewGlance(int count)
    {
        var facing = new double?[count];
        // The crew indices + their world positions this frame.
        Span<int> crew = stackalloc int[count];
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            if (IsCrew(_droids[i].Name))
            {
                crew[n++] = i;
            }
        }
        if (n < 2)
        {
            return facing; // a lone crew member has no one to catch eyes with — no glance
        }
        for (int a = 0; a < n; a++)
        {
            DeckPlan.Droid da = _droids[crew[a]];
            double bestSq = double.MaxValue;
            int nearest = -1;
            for (int b = 0; b < n; b++)
            {
                if (b == a)
                {
                    continue;
                }
                DeckPlan.Droid db = _droids[crew[b]];
                double d = (db.X - da.X) * (db.X - da.X) + (db.Y - da.Y) * (db.Y - da.Y);
                if (d < bestSq)
                {
                    (bestSq, nearest) = (d, crew[b]);
                }
            }
            DeckPlan.Droid dn = _droids[nearest];
            facing[crew[a]] = Math.Atan2(dn.Y - da.Y, dn.X - da.X); // world radians toward the caught eye
        }
        return facing;
    }

    // A WORKING crew member (the people who work the deck): the barkeep, the customs officer, the ship's own
    // droids — anyone who is neither a Reever nor a drinking PATRON (a seated bar regular, or the Magpie).
    private static bool IsCrew(string name) => name != "Reever" && !IsPatron(name);

    // The drinking patrons — the regulars' short names (HavenInterior.ShortNameFor) + the roaming Magpie +
    // the station Oracle (a ranting-drunk bar fixture, #425, not working staff) + the empty-chair fallback.
    // They never react to the off-deck buzzer; only the staff do.
    private static bool IsPatron(string name) => name switch
    {
        "Silas" or "Coil" or "Gilt-Eye" or "The Fixer" or "Regular" or "Magpie" or "Oracle" => true,
        _ => false,
    };

    // #314: brighten a colour toward white by t (0..1) — the one-frame decrement flash on the magazine
    // digits. Alpha is preserved; only the RGB warms up.
    private static RgbaColor LerpToWhite(RgbaColor c, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        static byte L(byte v, float t) => (byte)(v + (255 - v) * t);
        return new RgbaColor(L(c.R, t), L(c.G, t), L(c.B, t), c.A);
    }

    private void FillRect(float x, float y, float w, float h, RgbaColor color)
    {
        Span<float> s = _scratch.AsSpan(0, 8);
        s[0] = x; s[1] = y; s[2] = x + w; s[3] = y; s[4] = x + w; s[5] = y + h; s[6] = x; s[7] = y + h;
        _renderer.DrawPolygon(s, color, color, 1f);
    }

    // #348: a room label with a subtle dark backing plate (raised contrast over the cabin art), and the
    // MED BAY exception — a whiter, cooler label on a cleaner plate ringed by a thin cyan-white keyline,
    // "the shiny clean room that stands out from the bunk rooms" (owner, 2026-07-18). The plate sits in
    // the float command buffer (flushed under all text), so it always backs the glyphs and never covers
    // them. Text draws on the alphabetic baseline at (cx, cy); the plate is sized to the monospace run
    // (~6px/char at 10px) and seated around that baseline.
    private void DrawRoomLabel(float cx, float cy, string text, bool medBay)
    {
        float w = text.Length * 6.0f + 9f;
        const float h = 13f;
        float x0 = cx - w / 2f, y0 = cy - 10f;
        FillRect(x0, y0, w, h, medBay ? MedBayPlate : RoomLabelPlate);
        if (medBay)
        {
            DrawRectOutline(x0, y0, w, h, MedBayKeyline); // the clean room's tidy edge — the exception's keyline
        }
        _renderer.DrawText(cx, cy, text, medBay ? MedBayText : RoomLabelText,
            medBay ? "bold 10px monospace" : "10px monospace", TextAlign.Center);
    }

    // The crude motion-tracker fan (top-right corner, screen-space): a graph-paper radar showing MOVING
    // contacts by bearing + range, clamped to the ring when beyond it. Blips pulse faster as they close.
    private static readonly RgbaColor TrackerRing = new(120, 200, 150, 150);

    // #330 · Where the left-edge instrument column begins under the SANITY plate (its base bottom ≈ 70px
    // + a small consistent gap). The tracker's centre sits directly below this — one honest column.
    private const double SanityColumnTop = 82.0;
    private const double TrackerDesiredRadius = 116.0;   // owner: "make the motion meter bigger" — the ~115 class

    private void DrawMotionTracker(int widthPx, int heightPx, double simTime, in SurfaceHud hud)
    {
        // #324/#330 (owner: "make the motion meter bigger and visible… put the motion under the sanity
        // meter"): the excursion's star instrument, big enough to read at a glance, seated in the top-left
        // column directly beneath the SANITY plate on an opaque disc. It SHRINKS proportionally on a small
        // viewport rather than clipping, and the ship-desk chrome is hidden on-surface so nothing buries it.
        float r = (float)MotionTracker.TrackerRadius(widthPx, heightPx, SanityColumnTop, TrackerDesiredRadius);
        (double acx, double acy) = MotionTracker.TrackerAnchor(widthPx, heightPx, r, SanityColumnTop);
        float cx = (float)acx, cy = (float)acy;

        // Sizes scale with the disc so a shrunk tracker stays legible and a big one reads across the map.
        float labelPx = (float)Math.Clamp(r * 0.13, 10, 15);
        float readoutPx = (float)Math.Clamp(r * 0.11, 9, 13);
        // Lane-1 (owner, 2026-07-18): the Reever blips read SMALLER than the old contacts — a tight,
        // insistent dot, not a fat one, so a crowding tide is a rash of pinpricks rather than a smear.
        float blipNear = (float)Math.Max(2.6, r * 0.042);
        float blipFar = (float)Math.Max(2.0, r * 0.032);

        // The graph-paper fan: an opaque backing disc, three rings + crosshair.
        _renderer.DrawCircle(cx, cy, r + 6f, new RgbaColor(6, 11, 10, 238), TrackerRing, 1f);
        _renderer.DrawCircle(cx, cy, r, null, TrackerRing, 1.75f);
        _renderer.DrawCircle(cx, cy, r * 0.66f, null, new RgbaColor(120, 200, 150, 85), 1f);
        _renderer.DrawCircle(cx, cy, r * 0.33f, null, new RgbaColor(120, 200, 150, 70), 1f);
        DrawSeg((cx - r, cy), (cx + r, cy), new RgbaColor(120, 200, 150, 70), 1f);
        DrawSeg((cx, cy - r), (cx, cy + r), new RgbaColor(120, 200, 150, 70), 1f);
        _renderer.DrawText(cx, cy - r - 8, "MOTION TRACKER", TrackerRing, $"bold {labelPx:0}px monospace", TextAlign.Center);

        // Lane-1 (owner, 2026-07-18): the Reever blips are red and "pulsing like a heartbeat" — the
        // creatures' pulse on the sweep. A lub-dub envelope drives the blips' size and glow, quickening
        // with the tracker cadence as the nearest closes; even a far-off tide keeps a slow, live beat.
        double beatHz = hud.Cadence switch { 3 => 2.4, 2 => 1.6, 1 => 1.0, _ => 0.75 };
        double beat = Heartbeat((simTime * 0.001 * beatHz) % 1.0); // 0..1 lub-dub envelope
        byte beatAlpha = (byte)(120 + (135 * beat));
        float beatScale = 0.72f + (0.5f * (float)beat);

        // #338 "The long ear": the range that reaches the ring edge is no longer a magic 60 — it is a
        // MULTIPLE of what the eye actually sees here (the surface camera shows 64×28 du, so this viewport's
        // visible half-width in du is widthPx/scale/2), so the fan hears several times farther than the grid
        // shows. A blip's DISTANCE is read straight off the fan: faint + small on the rim, firming to an
        // insistent near dot as it closes (MotionTracker.BlipIntensity) — the dread-gap made visible.
        float surfScale = Math.Min(widthPx / 64f, heightPx / 28f);
        double visualHalfWidthDu = (widthPx / Math.Max(surfScale, 0.001f)) / 2.0;
        double detectionRange = MotionTracker.DetectionRange(visualHalfWidthDu);
        foreach ((double bearing, double range) in hud.Blips)
        {
            double rr = Math.Min(range / detectionRange, 1.0) * (r - 6);
            // World bearing: +x = right, +y = port (up on screen) → screen y flips.
            float bx = cx + (float)(Math.Cos(bearing) * rr);
            float by = cy - (float)(Math.Sin(bearing) * rr);
            double firm = MotionTracker.BlipIntensity(range, detectionRange); // 1 near … FaintFloor on the rim
            float sz = (float)(blipFar + ((blipNear - blipFar) * firm)) * beatScale;
            byte alpha = (byte)Math.Clamp(beatAlpha * (0.35 + (0.65 * firm)), 30, 255);
            var col = new RgbaColor(235, 70, 60, alpha); // watchdog red, pulsing — dimmer the farther out
            _renderer.DrawCircle(bx, by, sz, col, col);
        }

        _renderer.DrawText(cx, cy + r + 14, hud.Readout, TrackerRing, $"{readoutPx:0}px monospace", TextAlign.Center);

        // Lane-1: the dig/sentry captions seated beneath the readout (owner: "advertise the dig and bot
        // options in text under the motion detector"). Column chrome only — and drawn only while each line
        // clears the viewport bottom, so a short screen never buries the keybar under them.
        if (hud.TrackerCaptions is { Count: > 0 } captions)
        {
            float capPx = (float)Math.Clamp(r * 0.095, 9, 12);
            float capY = cy + r + 14 + readoutPx + 8f;
            foreach (string caption in captions)
            {
                if (string.IsNullOrEmpty(caption) || capY > heightPx - 16)
                {
                    break;
                }
                _renderer.DrawText(cx, capY, caption, TextDim, $"{capPx:0}px monospace", TextAlign.Center);
                capY += capPx + 5f;
            }
        }
    }

    // A lub-dub heartbeat envelope over a [0,1) beat phase: two quick gaussian thumps near the start of
    // the cycle, then a rest — the shape the Reever blips pulse to (owner: "pulsing like a heartbeat").
    private static double Heartbeat(double phase)
    {
        double lub = Math.Exp(-Math.Pow((phase - 0.06) / 0.05, 2));
        double dub = 0.7 * Math.Exp(-Math.Pow((phase - 0.20) / 0.055, 2));
        return Math.Min(1.0, lub + dub);
    }

    // #317 the nerve gauge (top-left, screen-space): a crude deck-plan bar — full teal = steady hands,
    // draining through amber to blood as the regolith's stressors fray the captain. The whole gauge trembles
    // harder the lower the nerve falls (the "tremor in the glyph" the flavor ladder names), and a house-voice
    // line reads out beneath it. Display-only — this slice never rolls, exits, or ends a run (#226 owns that).
    private static readonly RgbaColor NerveFrame = new(150, 170, 190, 175);
    private void DrawNerveGauge(double simTime, double nerve, string readout, bool compact)
    {
        double frac = NerveModel.Fraction(nerve);
        NerveModel.NerveBand band = NerveModel.BandFor(nerve);
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

        // #324/#330 (owner: "let's make sanity visible :-D … even on the ship bar also"): a plainly-labelled
        // top-left gauge on its own dark plate. Full-size on the regolith where the FP toggle steps aside;
        // COMPACT aboard/ashore, tucked below the deck chrome (the top-left first-person toggle) so it
        // whispers without colliding.
        // #380 item 2: the plate NAMES the meter — "NERVE", the diegetic name every flavor rung, band-drop,
        // and shock pulse already speaks (the #226 sanity system's on-screen face). No name, no cause, no
        // remedy was the mystery; the name lands here, the cause+remedy in the band-drop pulse (Map.Surface).
        float w = compact ? 150f : 210f;
        float h = compact ? 13f : 18f;
        float labelPx = compact ? 9f : 11f;
        float baseY = compact ? 112f : 30f;   // aboard: clear below the top-left FP toggle; surface: column head
        float x0 = 18f + jx, y0 = baseY + jy;

        FillRect(x0 - 8f, y0 - 20f, w + 16f, h + 42f, new RgbaColor(6, 11, 10, 205));  // the backing plate
        _renderer.DrawText(x0, y0 - 6, "NERVE", NerveFrame, $"bold {labelPx:0}px monospace", TextAlign.Left);
        FillRect(x0, y0, w, h, new RgbaColor(14, 18, 24, 220));           // the empty channel
        FillRect(x0, y0, w * (float)frac, h, fill);                       // the fill
        for (int i = 1; i < 5; i++)                                       // crude deck-plan segments
        {
            float tx = x0 + w * i / 5f;
            DrawSeg((tx, y0), (tx, y0 + h), new RgbaColor(10, 14, 20, 160), 1f);
        }
        DrawRectOutline(x0, y0, w, h, NerveFrame);                        // the frame
        _renderer.DrawText(x0, y0 + h + 13, readout, fill, $"{labelPx:0}px monospace", TextAlign.Left);
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
