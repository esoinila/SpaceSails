using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// DeckView.Frame.OverTheDark — THE PASSES THAT ARE DRAWN OVER THE BLACK, and each of them is over it for
// its own stated reason: a deployed sentry carries a lamp (you can see a light in an unlit hall even when
// you cannot see what it lights), the motion fan's smudges and ghosts are an instrument whose whole worth
// is hearing what you cannot see, the overload countdown is a lit display, the consoles read their own
// panels, and the captain's mark and the corner gauges happen to YOU rather than to the room. `LerpToWhite`
// lives here because the magazine digits are the only thing that flashes. Pure motion out of the 1,393-line
// DeckView.Frame.cs: not one mark moved, and EveryFrameHashesTheSameTests says so per frame.

public sealed partial class DeckView
{
    /// <summary>#870 lane 7b · #314's deployed sentries and their scoreboard magazines — drawn ON the grid
    /// and OVER the dark, because a sentry carries a lamp and you can see a light in an unlit hall even
    /// when you cannot see what it lights.</summary>
    private void DrawTheSentries(
        SurfaceHud? surface, double simTime, int widthPx, int heightPx, float scale,
        Func<double, double, (float X, float Y)> project)
    {
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
                (float sx, float sy) = project(bxr, byr);
                if (firing && !dry)
                {
                    (float zx, float zy) = project(aimX, aimY);
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

                // #1039 · A COUNTER IS ITS SENTRY'S INSTRUMENT, AND IT MAY NOT OUTLIVE THE MARK IT BELONGS TO.
                //
                // The frame is FollowCam-centred on the captain, so a plate seated at a FIXED screen row is a
                // plate glued to the walker. That is exactly what the band-avoidance below did to the shuttle's
                // own door sentry: GATE-1 stands at the tube mouth (MoonSurface.SurfaceTopY + 2) and the captain
                // walks DOWN the field away from it, so the mark slides off the top of the glass — and the
                // Math.Max(ReservedBottom, …) then parked its "99" just under the ship's line and left it there
                // for the rest of the excursion, sliding sideways as he walked. The owner read it exactly as it
                // was drawn: "the magazine count follows the walker." A number riding a body that does not own
                // it is this repository's third named bug class — the pen reporting something the sim never said.
                //
                // So the plate is ANCHORED, not merely nudged: it is drawn only where its own sentry's mark is
                // drawn. Off the glass there is no mark to anchor to, and a counter with nothing under it is not
                // an instrument, it is a lie about whoever happens to be standing there. Nothing is lost by the
                // silence — the sling's own magazines are already spelled out in the HUD's MAGAZINES line and
                // the deployed roster in the key hints, and every sentry you can SEE still wears its drum.
                //
                // The mark itself (and the zap line to what it is dropping) keeps drawing: a beam arriving from
                // off-frame is real information, and the canvas clips it for free.
                bool markIsOnTheGlass =
                    sx >= -0.55f * scale && sx <= widthPx + (0.55f * scale) &&
                    sy >= -0.55f * scale && sy <= heightPx + (0.55f * scale);
                if (!markIsOnTheGlass)
                {
                    continue;
                }

                // The readout: a dark scoreboard panel with the two big digits, anchored above the bot so
                // it never covers the mark or its neighbours. Plate stays a steady size; only the number pops.
                // #1218 · …and it clears the bot box at THE lift, not at a literal of its own: the drum is a
                // plate like any other and joins the same rule (the head coder's ruling, in as many words).
                float pw = 3.0f * scale, ph = 2.0f * scale;
                float plateBottom = sy - (float)MarkBand.LiftAbove(0.55f * scale);
                float plateTop = plateBottom - ph;

                // #986 F2 · …EXCEPT WHERE THE SHIP IS TALKING. The top-centre band belongs to the mothership's
                // orbit line and the machine's stall banner (SpaceSails.Core.CommsBand, the #324 law). A bot
                // parked near the top of the frame — the sentry at the Tilt's airlock does exactly this — put
                // its alarm-red digits straight through those words. So a readout that would reach into the
                // band is seated just BELOW the band instead: never above its bot's mark, never over the
                // ship's line, and still the same plate read from across the map. The band is measured for
                // both lines whether or not the second is up, so a counter does not hop as the machine
                // complains — and a bot anywhere but the very top of the frame is untouched by this.
                //
                // #1039 · This floor is only ever reached now by a bot whose MARK is on the glass (the gate
                // above), so the seat it picks is always within a plate-height of its own sentry. Unguarded it
                // was a fixed screen row that any off-frame bot could be parked on, which is how a door sentry
                // twenty deck units behind the captain ended up wearing its drum over his head.
                if (plateTop < CommsBand.ReservedBottom)
                {
                    plateTop = (float)Math.Max(CommsBand.ReservedBottom, sy + (0.8f * scale));
                    plateBottom = plateTop + ph;
                }

                // #1218 · AND THEN IT TAKES ITS ROW IN THE BAND BOOK, as the sixth caption of the owner's
                // DEEP HOLD pile. A drum is a BLOCK and not a line — sixty pixels of opaque scoreboard — so
                // the whole plate is what is seated, and the digits keep their optical centre inside it.
                float rise = plateTop - (float)SeatTheBand(sx - (pw / 2), sx + (pw / 2), plateTop, plateBottom);
                plateTop -= rise;
                plateBottom -= rise;

                FillRect(sx - pw / 2, plateTop, pw, ph, new RgbaColor(16, 10, 10, 225));
                float baseY = (plateTop + plateBottom) / 2f + fontPx * 0.35f; // optical centre for the fixed-px glyphs
                _renderer.DrawText(sx, baseY, counter, digit,
                    $"bold {fontPx:0.#}px monospace", TextAlign.Center);
            }
        }
    }

    /// <summary>#870 lane 7b · #488's instrument half: the edgeless smudge over roughly where a return came
    /// from, and the colder broken ring where the fan last had something. Both are painted as AREAS on
    /// purpose — a dot would claim a precision a crude fan does not have.</summary>
    private void DrawWhatTheFanHeard(
        SurfaceHud? surface, double simTime, float scale, Func<double, double, (float X, float Y)> project)
    {
        // #488 · WHAT THE FAN HEARS THROUGH STEEL. A soft, edgeless bloom over roughly where the return
        // came from — big enough that it names a REGION and not a spot. Drawn under everything else so a
        // contact you can actually see is always the sharper mark on the deck.
        if (surface is { Smudges: { } heard })
        {
            foreach ((double smx, double smy, double smr) in heard)
            {
                (float ssx, float ssy) = project(smx, smy);
                float rPx = (float)(smr * scale);
                // Three widening rings, each fainter: no hard edge anywhere, so the eye reads "somewhere
                // about here" rather than a position.
                // Owner: "let's show them much better on motion detector still." The first pass was so
                // faint it read as a rendering artefact; a return you have to hunt for is not a warning.
                // Loud enough to catch the eye, still edgeless enough that it can never be mistaken for a
                // position — and it BREATHES, so a live return is obviously live.
                float pulse = 0.82f + 0.18f * (float)Math.Sin(simTime * 0.004);
                for (int ring = 4; ring >= 1; ring--)
                {
                    float f = ring / 4f;
                    byte alpha = (byte)Math.Clamp(96 * (1.05f - f) * pulse, 0f, 255f);
                    _renderer.DrawCircle(ssx, ssy, rPx * f * pulse, new RgbaColor(226, 96, 84, alpha), default);
                }
            }
        }

        // #488 · GHOSTS: where the fan last had something. Dimmer and colder than a live return, and drawn
        // with a broken ring so it never reads as a contact — this is a memory, not a target.
        if (surface is { Ghosts: { } ghosts })
        {
            foreach ((double gx, double gy, double fade) in ghosts)
            {
                (float gsx, float gsy) = project(gx, gy);
                byte a = (byte)Math.Clamp(70 * fade, 0f, 255f);
                float gr = (float)(2.4 * scale);
                _renderer.DrawCircle(gsx, gsy, gr, new RgbaColor(150, 120, 160, (byte)(a / 3)), default);
                // Four short arcs of a ring, so the eye reads "was here" rather than "is here".
                for (int seg = 0; seg < 4; seg++)
                {
                    double a0 = (seg * Math.PI / 2) + 0.35;
                    DrawSeg(
                        (gsx + (float)(Math.Cos(a0) * gr), gsy + (float)(Math.Sin(a0) * gr)),
                        (gsx + (float)(Math.Cos(a0 + 0.75) * gr), gsy + (float)(Math.Sin(a0 + 0.75) * gr)),
                        new RgbaColor(170, 140, 180, a), 1.1f);
                }
            }
        }
    }

    /// <summary>#870 lane 7b · #488's overload countdown, anchored to the thing that is about to fail so it
    /// recedes behind the captain as they run — the one number that decides whether they live, kept out of
    /// the message channel where the PA calls are.</summary>
    private void CountDownTheOverload(
        SurfaceHud? surface, float scale, Func<double, double, (float X, float Y)> project)
    {
        // #488 · THE OVERLOAD, ON THE GRID. Same scoreboard as a magazine, bigger and always alarm-red,
        // anchored to the thing that is about to fail — so it recedes behind the captain as they run, and
        // the one number that decides whether they live is never in the message channel with the PA calls.
        if (surface is { Countdown: { } burn })
        {
            (float bx, float by) = project(burn.X, burn.Y);
            float pw = 5.4f * scale, ph = 3.2f * scale;
            // #1218 · The same scoreboard, seated the same way: a block in the band book, and the digits keep
            // their place inside it.
            float top = (float)SeatTheBand(bx - (pw / 2), bx + (pw / 2), by - (2.4f * scale),
                by - (2.4f * scale) + ph);

            FillRect(bx - pw / 2, top, pw, ph, new RgbaColor(20, 6, 6, 235));
            // A hard border so it reads as a fitted instrument rather than a floating label.
            DrawSeg((bx - pw / 2, top), (bx + pw / 2, top), SegAlarm, 1.2f);
            DrawSeg((bx - pw / 2, top + ph), (bx + pw / 2, top + ph), SegAlarm, 1.2f);

            float px = MagBasePx * 1.5f;
            _renderer.DrawText(bx, top + ph / 2 + px * 0.35f, burn.Text, SegAlarm,
                $"bold {px:0.#}px monospace", TextAlign.Center);
        }
    }
}
