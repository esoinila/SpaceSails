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
                float pw = 3.0f * scale, ph = 2.0f * scale;
                float plateBottom = sy - 0.8f * scale;      // clears the bot box (half 0.55·scale) with a gap
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
            float top = by - 2.4f * scale;

            FillRect(bx - pw / 2, top, pw, ph, new RgbaColor(20, 6, 6, 235));
            // A hard border so it reads as a fitted instrument rather than a floating label.
            DrawSeg((bx - pw / 2, top), (bx + pw / 2, top), SegAlarm, 1.2f);
            DrawSeg((bx - pw / 2, top + ph), (bx + pw / 2, top + ph), SegAlarm, 1.2f);

            float px = MagBasePx * 1.5f;
            _renderer.DrawText(bx, top + ph / 2 + px * 0.35f, burn.Text, SegAlarm,
                $"bold {px:0.#}px monospace", TextAlign.Center);
        }
    }

    /// <summary>#870 lane 7b · The consoles, and the ONE prompt that is the true one — the offer is drawn
    /// only where <see cref="DeckPlan.NearestConsoleSpot"/> would actually answer, and #791's service run
    /// is marked down the whole length that answers.
    ///
    /// <para>#708 · This pass puts the lamp BACK on the pen and takes it off again. Consoles are drawn late
    /// — after the fan's smudges, so a contact heard through a wall is not painted over by a plate — which
    /// puts them on the far side of the blackout; they are WORLD all the same, so the world is drawn in two
    /// passes and both of them are behind the headlights.</para></summary>
    private void DrawTheConsoles(
        DeckPlan plan, in State state, Func<double, double, (float X, float Y)> project, Func<double, double, int> darkState)
    {
        // Consoles.
        //
        // ONE PROMPT, AND IT IS THE TRUE ONE. Owner, twice, on two different decks: "there two e's are too
        // close to each others now" and then "see the two crowded consoles at the back of our ship". Both
        // times I moved a console — and both times the real fault was here: this drew an [E] over EVERY
        // console inside the interact radius, while the key itself only ever answers the NEAREST one
        // (InteractAtConsole → NearestConsoleSpot). So a captain standing between two fittings saw two
        // offers, and one of them was a lie.
        //
        // Geometry could never fix that. A bridge is dense on purpose — helm, nav post, scope and three
        // desks inside a few du — so "keep every pair 6 du apart" is not a ship anyone would want to walk.
        // Asking the same function the key asks is the fix, it is one line, and it is right on every deck in
        // the game at once: her own, a derelict's, a station's, the regolith.
        //
        // #708 · AND THE LAMP GOES BACK ON THE PEN FOR THEM. Consoles are drawn late — after the fan's
        // smudges, so a contact heard through a wall is not painted over by a plate — which puts them on the
        // wrong side of the blackout. They are WORLD, though: a fitting bolted to a wall in an unlit hall is
        // not visible because it is important. So the world is drawn in two passes and both of them are
        // behind the headlights, rather than moving the blackout and quietly hiding the instrument.
        if (state.Dark)
        {
            _renderer = _mask;
        }

        DeckPlan.ConsoleSpot? answering = plan.NearestConsoleSpot(state.AvatarX, state.AvatarY);
        // Which console the key would actually reach, asked as a name rather than as a comparison, because
        // two passes below ask it: the cull (which may never drop the one you are standing at) and the ink.
        bool IsTheOneAnswering(DeckPlan.ConsoleSpot spot) => answering == spot;

        foreach (DeckPlan.ConsoleSpot console in plan.Consoles)
        {
            // #371 Phase 3 fog: a console inside an unseen chamber is unknown (hidden); an explored one is
            // dimmed. A still-sealed door's console sits OUTSIDE any chamber rect, so it always shows.
            if (darkState(console.X, console.Y) == 0)
            {
                continue;
            }
            (float sx, float sy) = project(console.X, console.Y);

            // #563 slice 2 · …and the same reject for a plate, at ITS OWN reach. A console is a dot with a
            // line of text centred over it, so the thing on screen is as wide as the label — tested as a
            // zero-length segment with a margin that covers half that width (the plates are drawn at ~6px a
            // character) plus the ring. Anything narrower would clip words off the edge of the screen, and a
            // cull that changes the picture is not a cull.
            //
            // The ANSWERING console is never skipped, whatever the arithmetic says: #212's law is that an
            // affordance the game will let you use may not be invisible. Neither is a service RUN, which is
            // eighty deck units of counter and not a point at all.
            //
            // What this drops is the several dozen plates a nine-tile chunk now carries, standing in ruins
            // several hundred deck units off the side of the screen.
            if (!IsTheOneAnswering(console) && !console.IsRun
                && OffTheGlass(sx, sy, sx, sy, 24f + (console.Label.Length * 3.5f)))
            {
                continue;
            }

            // Lit only when [E] would actually reach THIS console. The radius check is still the gate —
            // NearestConsoleSpot applies it — so nothing lights up across the ship; what changed is that a
            // second console in range no longer claims a key it will not get.
            bool near = IsTheOneAnswering(console);
            RgbaColor c = near ? ConsoleNear : ConsoleGlow;

            // ── #791 · A FIXTURE THAT IS A RUN IS DRAWN AS ONE ────────────────────────────────────────
            //
            // Owner, at the B1 bar: "we should probably have service on the whole length indicated somehow."
            // The desk's front is now the press zone, so the desk's front is now MARKED — a service rail
            // down the whole of it with a serving tick struck across it every few du, in the same console
            // ink the dot has always used. No text is repeated along it (#782: a plate you cannot read is
            // worse than none, and one you read forty times is a wall of noise); the plate is said once, at
            // the fixture's own middle, exactly as it always was.
            //
            // IT IS THE VERY SEGMENT THE KEY MEASURES. Both come off ConsoleSpot's own span, which came off
            // Core's Hall.Service — so the length that is lit and the length that answers cannot disagree,
            // which is the split this deck has paid for more than any other.
            if (console.IsRun)
            {
                DrawServiceRun(console, c, near, project);
            }

            _renderer.DrawCircle(sx, sy, near ? 5f : 3.5f, c, c);
            _renderer.DrawText(sx, sy - 10, console.Label, near ? ConsoleNear : TextDim,
                near ? "bold 10px monospace" : "9px monospace", TextAlign.Center);
            if (near)
            {
                // …and the offer is drawn WHERE YOU ARE STANDING. On a point console that is the console;
                // on an eighty-du desk it is the stretch of counter under your elbow, because an [E] forty
                // du away at the plate would be the game answering a press it looks like it is refusing.
                (float ex, float ey) = console.NearestPointTo(state.AvatarX, state.AvatarY);
                (float px, float py) = project(ex, ey);
                _renderer.DrawText(px, py + 20, "[E]", ConsoleNear, "bold 11px monospace", TextAlign.Center);
            }
        }

        _renderer = _canvas;    // #708 · and off again — everything below is the captain, or an instrument
    }

    /// <summary>#870 lane 7b · The captain, and the things that happen to THEM — #453's blood on the ground
    /// and #467's wash round the edges of the screen, #784's seated figure or the standing one and its
    /// spoke, and #313's channel bar with #562's glyph saying which slow thing this is.</summary>
    private void DrawTheCaptain(
        in State state, SurfaceHud? surface, int widthPx, int heightPx, float scale,
        Func<double, double, (float X, float Y)> project)
    {
        // The captain.
        (float ax, float ay) = project(state.AvatarX, state.AvatarY);

        // #453 · BLOOD, when a blow gets past the block (owner: "Maybe a splash of blood when reever hit
        // goes through players attempt to block it. :-D"). Seeded spatter around the captain, thrown on the
        // regolith UNDER them so it reads as coming off the body. Brief — it is punctuation, not a decal.
        // #467 · THE SCREEN REACTS. Owner: "I had no sound to alert that I was taking damage… I should know
        // when I'm hurt." A small spatter under the boots was too easy to miss mid-fight, so a blow also
        // washes the EDGES of the screen red on the same fade. Peripheral, never over the grid — the deck
        // stays readable while you decide whether to run.
        if (surface is { BloodSplash: > 0 } hurt)
        {
            double f = Math.Clamp(hurt.BloodSplash, 0, 1);
            byte a = (byte)Math.Clamp(150 * f, 0, 255);
            var edge = new RgbaColor(150, 12, 12, a);
            float band = Math.Max(10f, heightPx * 0.055f);
            FillRect(0, 0, widthPx, band, edge);
            FillRect(0, heightPx - band, widthPx, band, edge);
            FillRect(0, 0, band, heightPx, edge);
            FillRect(widthPx - band, 0, band, heightPx, edge);
        }

        if (surface is { BloodSplash: > 0 })
        {
            double fade = Math.Clamp(surface.Value.BloodSplash, 0, 1);
            for (int i = 0; i < 9; i++)
            {
                // A fixed fan, so the spatter is stable for the moment it is up rather than crawling.
                double a = i * 2.399963229728653;             // the golden angle again
                double reach = scale * (0.5 + (0.16 * (i % 4)));
                float bx = ax + (float)(Math.Cos(a) * reach);
                float by = ay + (float)(Math.Sin(a) * reach);
                var blood = new RgbaColor(190, 30, 30, (byte)Math.Clamp(235 * fade, 0, 255));
                _renderer.DrawCircle(bx, by, Math.Max(1.5f, 0.16f * scale), blood, blood);
            }
        }

        if (state.Seated)
        {
            // ── #784 · SITTING DOWN, DRAWN ──
            //
            // Owner: "Let's make the graphics say I am sitting down at the avatar level." A standing captain
            // is a body and a long spoke pointing where they are going. A seated one is going nowhere, so
            // the spoke is gone entirely — in its place a CHAIR BACK behind the shoulders and a short bar of
            // ARMS on the table in front, and a body that takes a little less floor because it is folded
            // into a chair. Same ink, same anchor: it is the same captain, in a different posture, and the
            // three marks read as one figure rather than as furniture that has appeared beside them.
            DrawSeated(ax, ay, state.HeadingRad, scale);
        }
        else
        {
            // #473: the captain's mark already happened to equal AvatarRadius — say so, so the two can never
            // drift apart again the way the Old Ones' mark had.
            _renderer.DrawCircle(ax, ay, (float)DeckPlan.AvatarRadius * scale, AvatarColor, AvatarColor);
            float hx = ax + (float)Math.Cos(state.HeadingRad) * scale * 1.1f;
            float hy = ay - (float)Math.Sin(state.HeadingRad) * scale * 1.1f;
            DrawSeg((ax, ay), (hx, hy), AvatarColor, 2f);
        }

        // #313 the dig channel: a shovel glyph over the captain and a crude progress bar — the
        // vulnerability window, drawn ON the grid so the player watches the tracker while it fills.
        if (surface is { DigProgress: >= 0 } dig)
        {
            // #562: the glyph and the tint say WHICH slow thing this is. A shovel over a magazine being
            // racked would be the same class of lie this project keeps paying for.
            RgbaColor glyphInk = dig.ChannelIsAid
                ? new RgbaColor(150, 235, 200, 245)
                : new RgbaColor(255, 230, 140, 240);
            RgbaColor fillInk = dig.ChannelIsAid
                ? new RgbaColor(120, 215, 175, 240)
                : new RgbaColor(255, 200, 90, 240);
            _renderer.DrawText(ax, ay - 1.6f * scale, dig.ChannelGlyph, glyphInk, "bold 15px monospace", TextAlign.Center);
            float bw = 3.2f * scale, bh = 0.45f * scale;
            float bx0 = ax - bw / 2, by0 = ay + 1.1f * scale;
            FillRect(bx0, by0, bw, bh, new RgbaColor(20, 24, 30, 220));
            FillRect(bx0, by0, bw * (float)Math.Clamp(dig.DigProgress, 0, 1), bh, fillInk);
        }
    }

    /// <summary>#870 lane 7b · The corner and edge chrome, which is not the world and never was: #313's
    /// motion fan, #317/#330's nerve gauge and its ledger, #327's orbit line, #825's stall banner on its
    /// own line under it, the keybar, and #440's standing prompt above it.</summary>
    private void DrawTheInstruments(
        in State state, SurfaceHud? surface, double simTime, int widthPx, int heightPx, float ox)
    {
        // #313 the motion tracker: a crude corner fan of MOVING blips (bearing/range), including
        // contacts beyond the grid edge — the early warning. Cadence pulses the blips as they close.
        if (surface is { Instruments: true } tHud)
        {
            DrawMotionTracker(widthPx, heightPx, simTime, tHud);
        }

        // #317/#330 the nerve gauge: a crude deck-plan bar in the TOP-LEFT column. On the surface it is the
        // full-size head of the instrument column (the tracker seats beneath it); aboard the ship and in a
        // haven it whispers (compact, tucked below the deck chrome). Shown in every walk mode, never flight.
        if (state.ShowNerve)
        {
            DrawNerveLedger(state, heightPx);
            DrawNerveGauge(simTime, state.Nerve, state.NerveReadout, state.NerveCompact, state.HitsTaken, surface?.BloodSplash ?? 0);
        }

        // #986 F2 · …AND THE PLATE THAT MAKES "NEVER BURIED" TRUE. The two lines below were drawn as bare
        // ink on bare pixels while every other line the #324 law protects — the nerve gauge, the #612 air bar
        // and its source chip — sits on a dark plate. On the Tilt's ground the sentry at the airlock projects
        // to the top centre and its alarm-red magazine readout struck straight through "holds the ship,".
        // One plate, under both lines, in the same ink the other plates use; the band it is measured to
        // (SpaceSails.Core.CommsBand) is the SAME band DrawTheSentries keeps its counters out of, so this
        // does not simply trade the ship's buried line for the sentry's.
        string? orbitText = surface is { OrbitComms: { Length: > 0 } ol } ? ol : null;
        string? stallText = state.StallBanner is { Length: > 0 } sb ? sb : null;
        if (orbitText is not null || stallText is not null)
        {
            (double px, double py, double pw, double ph) = CommsBand.PlateFor(widthPx / 2.0, orbitText, stallText);
            FillRect((float)px, (float)py, (float)pw, (float)ph, new RgbaColor(6, 11, 10, 205));
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
            _renderer.DrawText(widthPx / 2f, (float)CommsBand.BaselineY(0), orbitLine, color,
                $"{CommsBand.LinePx:0}px monospace", TextAlign.Center);
        }

        // #825 · THE MACHINE'S OWN BANNER, on its own line, under the ship's. The owner had "SIGNAL BREAKING
        // UP" across the top while the thing that was actually broken was the frame rate — and a sentence
        // about a downlink is a worse answer than no sentence at all when the question is "why will my legs
        // not move". So it is drawn SEPARATELY (never appended to the orbit line), in the amber of a
        // machine-level warning rather than the comms grey, and it steps down a line when the ship is
        // already talking so neither fact ever paints over the other.
        if (state.StallBanner is { Length: > 0 } stall)
        {
            float y = (float)CommsBand.BaselineY(orbitText is null ? 0 : 1);
            _renderer.DrawText(widthPx / 2f, y, stall,
                new RgbaColor(255, 190, 100, 235), $"{CommsBand.LinePx:0}px monospace", TextAlign.Center);
        }

        // Blind-UI audit finding: with the tube off-camera, nothing said the ship was docked or
        // how to go ashore — the tester could only guess "airlock" by genre convention. On the surface
        // the keybar turns contextual (#324): the deploy/drop keys spell themselves out while they matter.
        string bottomHint = surface is { KeyHints: { Length: > 0 } hints }
            ? hints
            : state.Docked
                ? "docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ Q — helm"
                : "WASD / arrows — move ∙ E — interact ∙ Q — back to the helm";
        _renderer.DrawText(ox, heightPx - 10, bottomHint, TextDim, "11px monospace", TextAlign.Center);

        // #440: the standing prompt rides just ABOVE the keybar, bright and a size up — the same eyeline the
        // player already checks for keys, but unmistakably not chrome. Gently breathing so it reads as a
        // thing still owed rather than furniture.
        if (surface is { StandingPrompt: { Length: > 0 } standing })
        {
            double breathe = 0.78 + (0.22 * Math.Sin(simTime * 0.001 * 2.2));
            var promptColor = new RgbaColor(255, 205, 90, (byte)Math.Clamp(255 * breathe, 60, 255));
            _renderer.DrawText(ox, heightPx - 30, standing, promptColor, "bold 14px monospace", TextAlign.Center);
        }
    }
    // #314: brighten a colour toward white by t (0..1) — the one-frame decrement flash on the magazine
    // digits. Alpha is preserved; only the RGB warms up.
    private static RgbaColor LerpToWhite(RgbaColor c, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        static byte L(byte v, float t) => (byte)(v + (255 - v) * t);
        return new RgbaColor(L(c.R, t), L(c.G, t), L(c.B, t), c.A);
    }
}
