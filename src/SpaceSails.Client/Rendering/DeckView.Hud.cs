using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: the corner instruments: the motion tracker, the nerve gauge and its ledger (part of DeckView).
public sealed partial class DeckView
{
    // The crude motion-tracker fan (top-right corner, screen-space): a graph-paper radar showing MOVING
    // contacts by bearing + range, clamped to the ring when beyond it. Blips pulse faster as they close.
    private static readonly RgbaColor TrackerRing = new(120, 200, 150, 150);

    // #330/#561 · Where the left-edge instrument column begins under the nerve block. ASKED, never typed:
    // the block publishes its own plate bottom and HudColumn.GapPx is the air after it, so the day something
    // else is hung under the pips the fan steps down with them. It was a literal 82.0 for two releases after
    // #453's condition pips pushed the gauge to y=80, and the owner read the result as one instrument.
    // The fan is only ever drawn on an excursion, where the gauge takes the corner full-size — so the
    // SURFACE block is the one this column sits under.
    private static double NerveColumnTop => HudColumn.Surface.ColumnTop;
    private const double TrackerDesiredRadius = 116.0;   // owner: "make the motion meter bigger" — the ~115 class

    private void DrawMotionTracker(int widthPx, int heightPx, double simTime, in SurfaceHud hud)
    {
        // #324/#330 (owner: "make the motion meter bigger and visible… put the motion under the sanity
        // meter"): the excursion's star instrument, big enough to read at a glance, seated in the top-left
        // column directly beneath the SANITY plate on an opaque disc. It SHRINKS proportionally on a small
        // viewport rather than clipping, and the ship-desk chrome is hidden on-surface so nothing buries it.
        float r = (float)MotionTracker.TrackerRadius(widthPx, heightPx, NerveColumnTop, TrackerDesiredRadius);
        (double acx, double acy) = MotionTracker.TrackerAnchor(widthPx, heightPx, r, NerveColumnTop);
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

        // #591 · WHERE YOU ARE, ON THE INSTRUMENT. Owner: "the motion tracker should be in underground
        // visibility mode when we are deeeeeeeep under surface" — and depth is the single most important
        // fact about a captain's situation down there, because it is the number that decides whether they
        // get back up on the air they have. It was only ever readable as a label lying on the floor plan
        // behind them, which is the wrong place: you read the plan when you are thinking and the instrument
        // when you are worried.
        //
        // Drawn in the fan's own ink, above the ring beside the title, so a glance says "you are inside
        // something" before a single word is read.
        if (hud.TrackerPlace is { Length: > 0 } place)
        {
            _renderer.DrawText(cx, cy - r + 4, place, TrackerRing,
                $"{Math.Clamp(labelPx * 0.82, 8, 11):0}px monospace", TextAlign.Center);
        }

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
        // #591: the reach is HANDED DOWN by the sim now, because the sim and this draw were deriving it
        // separately and could disagree — and underground the sim shortens it with depth, which would have
        // made that disagreement the difference between a blip you can hear and a blip you can only see.
        // The viewport derivation stays as the fallback for callers from before the field existed.
        float surfScale = Math.Min(widthPx / 64f, heightPx / 28f);
        double visualHalfWidthDu = (widthPx / Math.Max(surfScale, 0.001f)) / 2.0;
        double detectionRange = hud.FanReach > 0 ? hud.FanReach : MotionTracker.DetectionRange(visualHalfWidthDu);
        foreach ((double bearing, double range, bool blob) in hud.Blips)
        {
            double rr = Math.Min(range / detectionRange, 1.0) * (r - 6);
            // World bearing: +x = right, +y = port (up on screen) → screen y flips.
            float bx = cx + (float)(Math.Cos(bearing) * rr);
            float by = cy - (float)(Math.Sin(bearing) * rr);
            double firm = MotionTracker.BlipIntensity(range, detectionRange); // 1 near … FaintFloor on the rim

            // #830 · THE UNSURE BLOB. Something alive is standing out there and the fan cannot tell you much
            // more than that, so it is drawn as the nest's own register at person scale: a soft wash a few
            // rings deep, WIDER than a body and wider still the further out it is (MotionTracker.BlobSpreadDu
            // — the on-grid smudge's law, so the two pictures agree about how vague a thing is), and never
            // the tight certain dot a mover gets. Same watchdog red: it is the same instrument reporting the
            // same kind of trouble, with less confidence.
            if (blob)
            {
                float wide = (float)Math.Max(
                    6.0, MotionTracker.BlobSpreadDu(range) / detectionRange * r * 4.0);
                byte soft = (byte)Math.Clamp(
                    beatAlpha * MotionTracker.BlobFaintness * (0.35 + (0.65 * firm)), 22, 190);
                for (int ring = 3; ring >= 1; ring--)
                {
                    var wash = new RgbaColor(235, 70, 60, (byte)(soft / (ring + 1)));
                    _renderer.DrawCircle(bx, by, wide * ring / 3f, wash, wash);
                }
                continue;
            }

            float sz = (float)(blipFar + ((blipNear - blipFar) * firm)) * beatScale;
            byte alpha = (byte)Math.Clamp(beatAlpha * (0.35 + (0.65 * firm)), 30, 255);
            var col = new RgbaColor(235, 70, 60, alpha); // watchdog red, pulsing — dimmer the farther out
            _renderer.DrawCircle(bx, by, sz, col, col);
        }

        // #573 · A RUMOUR: a soft, wide, low-contrast wash. It is under everything else on purpose — it is
        // the least certain thing on the instrument and must never compete with a contact.
        if (hud.Rumours is { Count: > 0 } rumours)
        {
            foreach ((double bearing, double range, double spread) in rumours)
            {
                double rr = Math.Min(range / detectionRange, 1.0) * (r - 5);
                float bx = cx + (float)(Math.Cos(bearing) * rr);
                float by = cy - (float)(Math.Sin(bearing) * rr);
                float wide = (float)Math.Max(9.0, spread / detectionRange * r);
                for (int ring = 3; ring >= 1; ring--)
                {
                    _renderer.DrawCircle(bx, by, wide * ring / 3f, null,
                        new RgbaColor(120, 170, 210, (byte)(30 + (10 * (3 - ring)))), 1f);
                }
            }
        }

        // #573 · Own caches, once they are close enough for the fan to have any business knowing.
        if (hud.CacheBeacons is { Count: > 0 } caches)
        {
            foreach ((double bearing, double range) in caches)
            {
                double rr = Math.Min(range / detectionRange, 1.0) * (r - 5);
                float bx = cx + (float)(Math.Cos(bearing) * rr);
                float by = cy - (float)(Math.Sin(bearing) * rr);
                var gold = new RgbaColor(235, 205, 120, 220);
                DrawSeg((bx - 3.5f, by - 3.5f), (bx + 3.5f, by + 3.5f), gold, 1.6f);
                DrawSeg((bx + 3.5f, by - 3.5f), (bx - 3.5f, by + 3.5f), gold, 1.6f);
            }
        }

        // #573 · The beacons, drawn UNDER nothing and OVER the rings: hollow, calm, and slowly breathing,
        // so they never read as contacts. A place clamps to the rim when it is beyond the fan's reach, the
        // same way a distant mover does — you always know which way it is, never how far once it is far.
        if (hud.Beacons is { Count: > 0 } beacons)
        {
            double breathe = 0.85 + (0.15 * Math.Sin(simTime * 0.0016));
            foreach ((double bearing, double range, bool isHome, bool isLab) in beacons)
            {
                double rr = Math.Min(range / detectionRange, 1.0) * (r - 5);
                float bx = cx + (float)(Math.Cos(bearing) * rr);
                float by = cy - (float)(Math.Sin(bearing) * rr);

                // The way home is warmer than a shelter, because they are not the same promise: one is your
                // ship, the other is somebody else's roof.
                //
                // #585 · And a LIFT HEAD is neither, so it gets the imported violet the door itself wears
                // (#592). With nine shelter rings on the fan, one more ring in the same ink is not a signal —
                // the owner had a tracker full of identical circles and no way to tell which one was the way
                // down. A beacon that cannot be told apart from its neighbours is decoration.
                var ink = isLab
                    ? new RgbaColor(
                        SpaceSails.Core.BodyPalette.Imported.R,
                        SpaceSails.Core.BodyPalette.Imported.G,
                        SpaceSails.Core.BodyPalette.Imported.B, (byte)(235 * breathe))
                    : isHome
                        ? new RgbaColor(150, 215, 255, (byte)(210 * breathe))
                        : new RgbaColor(130, 235, 215, (byte)(195 * breathe));

                // The lab ring is drawn a size larger and doubled, so it reads at a glance on a busy fan.
                _renderer.DrawCircle(bx, by, (float)((isLab ? 8.0 : 5.5) * breathe), null, ink, isLab ? 2.4f : 1.8f);
                _renderer.DrawCircle(bx, by, isLab ? 2.4f : 1.6f, ink, ink);
            }
        }

        _renderer.DrawText(cx, cy + r + 14, hud.Readout, TrackerRing, $"{readoutPx:0}px monospace", TextAlign.Center);

        // #564 · THE AIR METER, directly under the tracker where the owner looked for it. It gets a BAR
        // rather than a line of text for the same reason NERVE does: it is one of the two things on a
        // surface that can kill you without anything touching you, and a number buried among key hints is
        // not something a captain glances at while a pack closes.
        float airBottom = cy + r + 14 + readoutPx + 6f;
        if (hud.AirSeconds >= 0)
        {
            float aw = r * 1.75f, ah = Math.Max(7f, r * 0.085f);
            float ax0 = Math.Max(8f, cx - (aw / 2)), ay0 = airBottom;
            double frac = Math.Clamp(hud.AirSeconds / SuitAir.TankSeconds, 0, 1);

            // Colour is the BAND, not the fraction — because the question is never "how full is it" but
            // "can I still get home from here", and those two part company the moment you walk anywhere.
            SuitAir.Band band = SuitAir.BandFor(hud.AirSeconds, hud.AirDistanceHome);
            RgbaColor fill = band switch
            {
                SuitAir.Band.Easy => new RgbaColor(120, 200, 235, 235),
                SuitAir.Band.Thinking => new RgbaColor(225, 200, 95, 240),
                SuitAir.Band.PastTheLine => new RgbaColor(240, 120, 60, 245),
                SuitAir.Band.Critical => new RgbaColor(255, 60, 45, 250),
                _ => new RgbaColor(90, 40, 38, 230),
            };

            // #612 · AND WHERE IT IS COMING FROM. Owner, on a pressurised floor with no way to tell:
            // "Maybe we should have on our hud a AIR: Tanks / External symbol... it is vital info."
            //
            // Drawn as a SOLID CHIP — dark letters on a block of colour — rather than as coloured text,
            // because a filled block is read pre-attentively and a word is not. At a glance the captain gets
            // green-or-amber; a beat later a triangle pointing down or a stopped square; only then the word.
            // That is three chances to learn the most consequential fact on the surface without reading.
            //
            // The chip is a SEPARATE colour from the bar on purpose. The bar answers "can I still get home
            // on this tank", which stays a real question in a shelter — you have to leave eventually. The
            // chip answers "is it going down right now". Both are true at once and they are not the same,
            // and the old gauge could only show one of them.
            bool drawing = SuitAir.Drawing(hud.AirSupply);
            RgbaColor chipInk = hud.AirSupply switch
            {
                SuitAir.Supply.Room => StencilAir,
                SuitAir.Supply.Ship => new RgbaColor(150, 215, 255, 245),
                _ => StencilDead,
            };

            FillRect(ax0 - 6f, ay0 - 15f, aw + 12f, ah + 32f, new RgbaColor(6, 11, 10, 205));
            _renderer.DrawText(ax0, ay0 - 4f, "AIR", fill, "bold 10px monospace", TextAlign.Left);

            string chip = $"{SuitAir.SourceGlyph(hud.AirSupply)} {SuitAir.SourceLabel(hud.AirSupply)}";
            float chipW = (chip.Length * 6.2f) + 10f;
            float chipX = Math.Max(ax0 + 26f, ax0 + aw - chipW);
            FillRect(chipX, ay0 - 14f, chipW, 13f, chipInk);
            _renderer.DrawText(chipX + 5f, ay0 - 4f, chip, new RgbaColor(8, 12, 16, 255),
                "bold 10px monospace", TextAlign.Left);

            FillRect(ax0, ay0, aw, ah, new RgbaColor(14, 18, 24, 220));
            FillRect(ax0, ay0, aw * (float)frac, ah, fill);
            // A held tank is ringed in its source's colour, so the "is it running" answer is on the bar
            // itself and not only on the chip above it — the one place a captain's eye is already resting.
            DrawRectOutline(ax0, ay0, aw, ah, drawing ? TrackerRing : chipInk);
            _renderer.DrawText(ax0, ay0 + ah + 11f,
                SuitAir.Readout(hud.AirSeconds, hud.AirDistanceHome, hud.AirSupply),
                drawing ? fill : chipInk, "10px monospace", TextAlign.Left);
            airBottom = ay0 + ah + 20f;
        }

        // Lane-1: the dig/sentry captions seated beneath the readout (owner: "advertise the dig and bot
        // options in text under the motion detector"). Column chrome only — and drawn only while each line
        // clears the viewport bottom, so a short screen never buries the keybar under them.
        if (hud.TrackerCaptions is { Count: > 0 } captions)
        {
            // #440: these were CENTRED on the tracker's centre-x. The tracker sits in the left instrument
            // gutter, so every caption longer than about twice that inset ran off the left edge of the
            // canvas and was sliced — the leading glyph gone, the line starting mid-word at x=0. The one
            // place the ground explains itself was unreadable (owner: "not advertised clearly enough").
            // Left-align them in the gutter instead, so a caption grows RIGHTWARDS into open screen and
            // every word survives however long the line gets.
            float capPx = (float)Math.Clamp(r * 0.095, 9, 12);
            float capY = airBottom + 6f;
            float capX = Math.Max(8f, cx - r);
            foreach (string caption in captions)
            {
                if (string.IsNullOrEmpty(caption) || capY > heightPx - 16)
                {
                    break;
                }
                _renderer.DrawText(capX, capY, caption, TextDim, $"{capPx:0}px monospace", TextAlign.Left);
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
    private void DrawNerveGauge(double simTime, double nerve, string readout, bool compact, int hitsTaken, double bloodFlash)
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
        // top-left gauge on its own dark plate. Full-size on the regolith, where it owns the corner outright;
        // COMPACT aboard/ashore, tucked below the deck chrome so it whispers without colliding.
        // #380 item 2: the plate NAMES the meter — "NERVE", the diegetic name every flavor rung, band-drop,
        // and shock pulse already speaks (the #226 sanity system's on-screen face). No name, no cause, no
        // remedy was the mystery; the name lands here, the cause+remedy in the band-drop pulse (Map.Surface).
        // #561 · The stack down the column — baseY, the bar's height, the pip size, the plate's height and
        // where the next instrument may start — is ONE measurement, held in Core (HudColumn) and read by
        // both the drawing here and the tracker's anchor. Only the widths are the renderer's own business.
        HudColumn.NerveBlock block = HudColumn.For(compact);
        float w = compact ? 150f : 210f;
        float h = (float)block.BarHeight;
        float labelPx = compact ? 9f : 11f;
        float baseY = (float)block.BaseY;   // aboard: clear below the top-left deck chrome; surface: column head
        float x0 = 18f + jx, y0 = baseY + jy;
        float inset = (float)HudColumn.PlateInsetPx;

        // #561 · THE PLATE BACKS EVERYTHING IT WAS DRAWN TO BACK. Its height is the block's own, measured
        // to the bottom of the condition pips plus the same inset it keeps at its left and right edges.
        // Typed as h + 42 it ended at y=70 while #453's pips ran 70 → 80, so on the regolith five red pips
        // sat on bare ground with no plate under them.
        FillRect(x0 - inset, y0 - (float)HudColumn.PlateHeadPx, w + (2f * inset),
            (float)block.PlateHeight, new RgbaColor(6, 11, 10, 205));                  // the backing plate
        _renderer.DrawText(x0, y0 - 6, "NERVE", NerveFrame, $"bold {labelPx:0}px monospace", TextAlign.Left);

        // #480 · TEN WHOLE PIPS, not a bar. Owner: "the sanity events should be quantized … not this float
        // stuff we have now." A sliding fill is exactly what made a loss unreadable — you cannot tell a
        // slide from a stop, or a big cause from a small one. Discrete pips can only ever change by a whole
        // unit, so the eye sees COUNT, and the flash line beside them says which cause spent it. Deliberately
        // the same pip idiom as the condition marker below, because the two meters are now comparable (#469).
        FillRect(x0, y0, w, h, new RgbaColor(14, 18, 24, 220));           // the empty channel
        int pipsLeft = NervePips.PipsOf(nerve);
        float npGap = w * 0.012f;
        float npW = (w - (npGap * (NervePips.MaxPips - 1))) / NervePips.MaxPips;
        for (int i = 0; i < NervePips.MaxPips; i++)
        {
            float px = x0 + (i * (npW + npGap));
            FillRect(px, y0, npW, h, i < pipsLeft ? fill : new RgbaColor(22, 28, 34, 200));
        }
        DrawRectOutline(x0, y0, w, h, NerveFrame);                        // the frame
        _renderer.DrawText(x0, y0 + h + 13, readout, fill, $"{labelPx:0}px monospace", TextAlign.Left);

        // #453 · THE CONDITION MARKER, under the nerve bar exactly where the owner asked for it ("Some kind
        // of hit condition marker below the nerves bar"). Five pips: how many blows are left in you. It is
        // NOT a second bar — nerve is a slope you slide down, skin is a countdown you can read at a glance,
        // and the two must never be mistaken for each other while you decide whether to run.
        if (hitsTaken >= 0)
        {
            float py = y0 + h + (float)HudColumn.PipDropPx;
            float pip = (float)block.PipSize;
            float gap = pip * 0.55f;
            int left = Math.Max(0, CaptainCondition.MaxHits - hitsTaken);
            var spent = new RgbaColor(70, 26, 24, 220);
            var intact = left switch
            {
                >= 4 => new RgbaColor(200, 90, 85, 240),
                3 => new RgbaColor(225, 120, 70, 245),
                2 => new RgbaColor(235, 90, 60, 250),
                _ => new RgbaColor(255, 45, 35, 255),   // one left: the loudest thing in the corner
            };
            for (int i = 0; i < CaptainCondition.MaxHits; i++)
            {
                float px = x0 + (i * (pip + gap));
                RgbaColor fillPip = i < left ? intact : spent;
                // #467: the pip that just went out FLASHES white-hot for a beat, so the eye is pulled to the
                // corner exactly when it changed rather than discovering the loss later.
                if (i == left && bloodFlash > 0)
                {
                    byte hot = (byte)Math.Clamp(255 * bloodFlash, 0, 255);
                    fillPip = new RgbaColor(255, (byte)(230 * bloodFlash), (byte)(210 * bloodFlash), hot);
                }
                FillRect(px, py, pip, pip, fillPip);
                DrawRectOutline(px, py, pip, pip, NerveFrame);
            }
            _renderer.DrawText(x0 + (CaptainCondition.MaxHits * (pip + gap)) + 6f, py + pip - 1f,
                CaptainCondition.Readout(hitsTaken), intact, $"{labelPx:0}px monospace", TextAlign.Left);
        }
    }

    // #480 · THE CAUSE, said twice. The FLASH is the line for the pip that just moved, sat right under the
    // gauge where the eye already is; the LEDGER keeps the last few so "what broke me?" has an answer after
    // the fact (the death card reads the same list). Owner: "what caused the sanity loss and what we did to
    // regain it. Now it is vague and wishy-washy." Losses read red, gains green — a recovery must be as
    // legible as a loss, or only half the ruling is honoured.
    private void DrawNerveLedger(in State state, int heightPx)
    {
        var ledger = state.NerveLedger;
        bool hasFlash = !string.IsNullOrEmpty(state.NerveFlash);
        if (!hasFlash && (ledger is null || ledger.Count == 0))
        {
            return;
        }

        float px = state.NerveCompact ? 9f : 11f;
        float x = 18f;

        // Anchored to the BOTTOM of the left column, growing upward. The first cut sat it directly under
        // the gauge and it landed straight on top of the motion tracker's fan — unreadable, and it buried
        // the one instrument you actually steer by. Down here it shares the column with nothing, and the
        // reading order still runs newest-nearest-the-eye.
        // The bottom margin clears the keybar AND whatever deck control sits in this same corner (the
        // captain's remote today) — at 46 the last ledger line printed straight through the button.
        const float BottomClearance = 78f;
        float lineH = px + 2f;
        int rows = (ledger?.Count ?? 0) + (ledger is { Count: > 0 } ? 1 : 0) + (hasFlash ? 1 : 0);
        float y = heightPx - BottomClearance - (rows * lineH);

        if (hasFlash)
        {
            _renderer.DrawText(x, y, state.NerveFlash!, new RgbaColor(255, 225, 210, 250),
                $"bold {px:0}px monospace", TextAlign.Left);
            y += lineH + 4f;
        }

        if (ledger is null || ledger.Count == 0)
        {
            return;
        }

        _renderer.DrawText(x, y, "NERVE LEDGER", NerveFrame, $"bold {px - 1:0}px monospace", TextAlign.Left);
        y += lineH;
        for (int i = 0; i < ledger.Count; i++)
        {
            // Older lines fade — the newest cause is the one that matters while you are deciding to run.
            byte a = (byte)Math.Clamp(215 - (i * 22), 70, 255);
            bool gain = ledger[i].Contains('+');
            var c = gain ? new RgbaColor(120, 220, 170, a) : new RgbaColor(235, 140, 130, a);
            _renderer.DrawText(x, y, ledger[i], c, $"{px - 1:0}px monospace", TextAlign.Left);
            y += lineH;
        }
    }
}
