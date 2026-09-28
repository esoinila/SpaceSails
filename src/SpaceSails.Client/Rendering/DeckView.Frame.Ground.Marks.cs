using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · NAMING THE ROOMS AND MARKING THE GROUND — the room names, and every mark left on the ground:
/// the pits, the husks, the chalk and the rest.
///
/// <para>Split out of <c>DeckView.Frame.Ground.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field. <c>PaintTheGround</c>, the one reader of
/// <c>ParkDay</c> that TheParkKeepsItsOwnDayTests allows by this file's path, stays home.</para>
/// </summary>
public sealed partial class DeckView
{
    /// <summary>#870 lane 7b · What the structure is CALLED — #600/#612's signage on its plate first and
    /// in a dimmer ink, then #348's room labels on theirs. One pass, because the order between the two is
    /// the whole of the rule: paint on a wall must not compete with the caption over a console.</summary>
    private void NameTheRooms(
        DeckPlan plan, Func<double, double, (float X, float Y)> project, Func<double, double, int> darkState)
    {
        // #348: each room label on its own dark backing plate for contrast over the art panels, with
        // MED BAY drawn as the clean-room exception (see the RoomLabel* colours above).
        // #600 · SIGNAGE, painted on the structure at the size a facility actually paints it. Owner, riding
        // between floors cut from the same bones: "something different in every floor so we visually spot
        // some difference when we go to different floors."
        //
        // Drawn before the room labels and in a dimmer ink than them ON PURPOSE: this is paint on a wall the
        // captain glances at, not a caption competing with the consoles. It is big enough to read without
        // looking for it and quiet enough to ignore while doing something else.
        //
        // #612 · ON A PLATE, not merely in a louder colour. The dim-paint idea above was right about the
        // FICTION and wrong about the screen: paint over a lit corridor is hard to read, and the owner hit
        // that twice ("they are kind of hidden now", then again after the ink was brightened). A dark panel
        // behind the letters is what makes signage legible in the real world too, and it is the same trick
        // #348 already uses one size down for the room labels — so the Hive's plate and the ship's cabin
        // labels are now the same instrument at two scales, which is one thing to learn instead of two.
        foreach ((float bx, float by, string text, float px, int tone) in plan.BigLabels)
        {
            if (darkState(bx, by) == 0)
            {
                continue;
            }
            (float bxp, float byp) = project(bx, by);
            // #612 · Owner: "The meters and the floor name could be yellow here... they are kind of hidden
            // now.... it should say if the floor is pressurized also." Tone chooses the ink and nothing
            // else: tone 1 is the relief of somewhere you can breathe, tone 2 is the one that costs you, and
            // everything else is paint on a wall. A state gets the colour that state wears everywhere else
            // in the game — the same green and the same amber as the chip on the suit gauge.
            RgbaColor ink = tone switch
            {
                1 => StencilAir,
                2 => StencilDead,
                _ => StencilPaint,
            };

            // Monospace, so the width is arithmetic rather than a measurement the renderer cannot do — the
            // same 0.6-em-per-glyph estimate DrawRoomLabel has used since #348, with the baseline sitting
            // roughly three quarters down the panel (canvas draws text from its alphabetic baseline).
            float w = (text.Length * px * 0.62f) + (px * 0.9f);
            float h = px * 1.32f;
            float x0 = bxp - (w / 2f);
            // #1218 · Signage goes in the band book on its PLATE, which is the shape that occupies the wall;
            // the baseline keeps its own seat inside it.
            float y0 = (float)SeatTheBand(x0, x0 + w, byp - (h * 0.77f), byp - (h * 0.77f) + h);
            FillRect(x0, y0, w, h, StencilPlate);
            DrawRectOutline(x0, y0, w, h, new RgbaColor(ink.R, ink.G, ink.B, 90));
            _renderer.DrawText(bxp, y0 + (h * 0.77f), text, ink, $"bold {px:0}px monospace", TextAlign.Center);
        }

        foreach ((float lx, float ly, string text) in plan.RoomLabels)
        {
            int ls = darkState(lx, ly); // #371 Phase 3 fog: hide an unseen chamber's label, dim an explored one
            if (ls == 0)
            {
                continue;
            }
            (float lxp, float lyp) = project(lx, ly);
            if (ls == 1)
            {
                // #1218 · An explored room's name wears no plate, so its own ink is its band.
                _renderer.DrawText(lxp, SeatTheCaption(lxp, lyp, text, 10.0, TextAlign.Center), text,
                    ExploredText, "10px monospace", TextAlign.Center);
            }
            else
            {
                DrawRoomLabel(lxp, lyp, text, medBay: text == "MED BAY");
            }
        }
    }

    /// <summary>#870 lane 7b · #313's surface ground marks — the swept grid first, then own caches, a
    /// panic-dropped chest, #314's husks and #371's movement echoes. All of it under the movers, so a
    /// figure can stand on any of it.</summary>
    private void MarkTheGround(
        SurfaceHud? surface, float scale, Func<double, double, (float X, float Y)> project)
    {
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
                    (float sx, float sy) = project(swx, swy);
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
            // #316 law 1, second half · THE HOLE WHERE A ✗ USED TO BE. Under the live marks, because a hole
            // is older than anything standing on it, and in the ground vocabulary already here: the divot
            // ring a probed square wears, with the treasure glyph gone grey in the middle of it. Nothing new
            // is drawn for this — the ✗ that is no longer yours IS the mark.
            if (hud.Pits is { } pits)
            {
                foreach ((double px, double py) in pits)
                {
                    (float sx, float sy) = project(px, py);
                    _renderer.DrawCircle(sx, sy, 0.5f * scale, null, PitRing, 1f);
                    _renderer.DrawText(sx, sy + 4, "✗", PitInk, "bold 16px monospace", TextAlign.Center);
                }
            }
            foreach ((double mx, double my, bool haunted) in hud.CacheMarks)
            {
                (float sx, float sy) = project(mx, my);
                var xcol = haunted ? new RgbaColor(230, 120, 90, 230) : new RgbaColor(230, 210, 120, 230);
                _renderer.DrawText(sx, sy + 4, "✗", xcol, "bold 16px monospace", TextAlign.Center);
                if (haunted)
                {
                    // #1218 · …AND THE FORENSIC LINE IS A ROW OF THE CACHE'S OWN PLATE. It used to be typed at
                    // sy - 12 while MoonSurface.Layout seeds a DigSite console at the SAME (x, y) by
                    // construction, and that console's plate was typed at sy - 10: two authors, two literals,
                    // two pixels apart, and the owner read one printed through the other at his own ✗. Both
                    // ask for the mark's row now (the ✗ is bold 16px, so its ink reaches about half that
                    // above its anchor) and whichever is drawn second takes the row above it.
                    const double exMarkHalfPx = 8.0, linePx = 8.0;
                    const string line = "yours · something walks near it";
                    _renderer.DrawText(
                        sx, SeatAboveAMark(sx, sy, exMarkHalfPx, line, linePx, TextAlign.Center),
                        line, new RgbaColor(230, 120, 90, 170), "8px monospace", TextAlign.Center);
                }
            }
            if (hud.HasDroppedChest)
            {
                (float dx2, float dy2) = project(hud.DropX, hud.DropY);
                _renderer.DrawText(dx2, dy2 + 5, "🧰", new RgbaColor(200, 160, 90, 240), "15px monospace", TextAlign.Center);
                // #1218 · A FIFTH upward literal the issue's own audit missed — `dy2 - 11`, and it is the
                // same lift over the same kind of mark as the ✗ two blocks up. The chest is 15px, so its ink
                // reaches about seven and a half above the anchor it is drawn a little below.
                const double chestHalfPx = 7.5, chestLinePx = 8.0;
                const string chestLine = "dropped chest";
                _renderer.DrawText(
                    dx2, SeatAboveAMark(dx2, dy2, chestHalfPx, chestLine, chestLinePx, TextAlign.Center),
                    chestLine, new RgbaColor(200, 160, 90, 180), "8px monospace", TextAlign.Center);
            }
            // #314: husks of downed Old Ones — dim marks left where they fell (the forensic seed, #316).
            if (hud.Husks is { } husks)
            {
                foreach ((double hkx, double hky) in husks)
                {
                    (float sx, float sy) = project(hkx, hky);
                    _renderer.DrawCircle(sx, sy, 0.55f * scale, HuskColor, HuskColor);
                    _renderer.DrawText(sx, sy + 3, "×", new RgbaColor(90, 60, 60, 220), "bold 11px monospace", TextAlign.Center);
                }
            }
            // #794 · A CHALK CROSS ON THE SHOTCRETE, beside the park's notice, while the mark is up. Two strokes
            // in chalk-white, a hand wide and no wider — a mark on a wall rather than a glyph on the map, and
            // nothing points at it: the notice is right there and the captain has to look.
            if (hud.Chalk is { } chalk)
            {
                (float cx, float cy) = project(chalk.X, chalk.Y);
                float arm = 0.45f * scale;
                ReadOnlySpan<float> rising = [cx - arm, cy + arm, cx + arm, cy - arm];
                ReadOnlySpan<float> falling = [cx - arm, cy - arm, cx + arm, cy + arm];
                _renderer.DrawPolyline(rising, ChalkInk, 1.4f);
                _renderer.DrawPolyline(falling, ChalkInk, 1.4f);
            }
            // #371 Phase 3: movement echoes — where a contact was last seen before it slipped behind cover.
            // A dim tracker-green ripple that fades over its life; "here was movement before" (owner's ask),
            // making the motion tracker's through-wall blips all the more exciting to chase.
            if (hud.Echoes is { } echoes)
            {
                foreach ((double ex2, double ey2, double alpha) in echoes)
                {
                    (float sx, float sy) = project(ex2, ey2);
                    byte a = (byte)Math.Clamp(alpha * 180.0, 0, 180);
                    var ring = new RgbaColor(EchoColor.R, EchoColor.G, EchoColor.B, a);
                    _renderer.DrawCircle(sx, sy, (0.35f + 0.5f * (float)alpha) * scale, null, ring, 1.2f);
                    _renderer.DrawText(sx, sy + 3, "·", ring, "10px monospace", TextAlign.Center);
                }
            }
        }
    }
}
