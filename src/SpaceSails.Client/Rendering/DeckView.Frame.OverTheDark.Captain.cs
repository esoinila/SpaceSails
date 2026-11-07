using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · WHAT HAPPENS TO YOU RATHER THAN TO THE ROOM — the captain's mark and the corner instruments,
/// drawn over the black.
///
/// <para>Split out of <c>DeckView.Frame.OverTheDark.cs</c> under #251 as a pure move: one contiguous run,
/// no member renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public sealed partial class DeckView
{
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
            // #1218 · The fourth literal, `ay - 1.6f * scale`, at THE lift over the captain's own body. It is
            // a single glyph, so the band book hands it straight back where it asked to be (MarkBand.IsAMark:
            // a mark stands FOR a thing rather than naming one, and the ground may not be shoved about) —
            // what changed is that the clearance over the captain is now the same decision as over everybody
            // else, instead of a number typed on this line.
            _renderer.DrawText(
                ax,
                SeatAboveAMark(ax, ay, (float)DeckPlan.AvatarRadius * scale, dig.ChannelGlyph, 15.0,
                    TextAlign.Center),
                dig.ChannelGlyph, glyphInk, "bold 15px monospace", TextAlign.Center);
            float bw = 3.2f * scale, bh = 0.45f * scale;
            float bx0 = ax - bw / 2, by0 = ay + 1.1f * scale;
            FillRect(bx0, by0, bw, bh, new RgbaColor(20, 24, 30, 220));
            FillRect(bx0, by0, bw * (float)Math.Clamp(dig.DigProgress, 0, 1), bh, fillInk);
        }
    }

    // #1463 · THE BAND AS IT IS REALLY DRAWN. Each line is fitted to the glass (CommsBand.Fit), so on a phone the
    // drill HUD is two rows (three with a stall banner) and the plate is taller and wider than the two single rows
    // CommsBand.ReservedBottom was measured for. Everything that must stay out of the band reads these, so none of
    // them can disagree with the pen: the sentries' readouts (the floor), and the surface nerve block and the
    // tracker under it, which step DOWN below the plate wherever the plate reaches over their column (#324).
    private string[] _orbitRows = [], _stallRows = [];
    private double _orbitPx = CommsBand.LinePx, _stallPx = CommsBand.LinePx;
    private double _bandBottom = CommsBand.ReservedBottom;
    private double _nerveShift;

    /// <summary>Where the nerve block's plate would end if nothing shifted it, plus the air under the band.</summary>
    private const double BandAirPx = 4.0;

    private void LayOutTheBand(in State state, SurfaceHud? surface, int widthPx)
    {
        string? orbitText = surface is { OrbitComms: { Length: > 0 } ol } ? ol : null;
        string? stallText = state.StallBanner is { Length: > 0 } sb ? sb : null;
        double glassRoom = widthPx - (2 * CommsBand.EdgeMarginPx);
        (string[] oRows, double oPx) = orbitText is null ? ([], CommsBand.LinePx) : CommsBand.Fit(orbitText, glassRoom);
        (string[] sRows, double sPx) = stallText is null ? ([], CommsBand.LinePx) : CommsBand.Fit(stallText, glassRoom);
        (_orbitRows, _orbitPx, _stallRows, _stallPx) = (oRows, oPx, sRows, sPx);
        _bandBottom = CommsBand.ReservedBottom;
        _nerveShift = 0.0;
        _yield.Disarm();
        if (oRows.Length + sRows.Length == 0)
        {
            SeatTheNerveBlockUnderTheMasthead(in state);
            return;
        }

        var drawn = new List<(string Text, double Px)>();
        drawn.AddRange(oRows.Select(r => (r, oPx)));
        drawn.AddRange(sRows.Select(r => (r, sPx)));
        (double bx, double by, double bw, double bh) = CommsBand.PlateForRows(widthPx / 2.0, drawn);
        // The stall banner comes and goes with the frame rate, so everything seated by the band is measured as if
        // ONE stall row were always up (CommsBand's own "measured for MaxLines on purpose" rule): the gauge, the
        // tracker and the sentries' seats must not hop a row each time the banner toggles.
        _yield.Arm(bx, by, bw, bh);     // #1466 · world words that would touch the plate are not drawn
        double plannedBottom = CommsBand.PlateBottom(oRows.Length + Math.Max(1, sRows.Length));
        _bandBottom = Math.Max(CommsBand.ReservedBottom, plannedBottom);
        if (state.ShowNerve && !state.NerveCompact)
        {
            // The nerve plate's right edge, with the gauge's tremor (up to 3px) allowed for.
            double nerveRight = 18.0 + 210.0 + HudColumn.PlateInsetPx + 3.0;
            if (bx < nerveRight)
            {
                _nerveShift = Math.Max(0.0, plannedBottom + BandAirPx - HudColumn.Surface.PlateTop);
            }
        }

        SeatTheNerveBlockUnderTheMasthead(in state);
    }

    /// <summary>#1466 item 3 · On a phone the wrapped desk tab bar lies over the top of the canvas, and the ashore
    /// (compact) nerve block's plate begins at y 92 - under a three-row bar whose bottom is y 117. The block is seated
    /// no higher than the bar's real bottom (the same number the page publishes as <c>--map-control-top</c>), read off
    /// the page at most twice a second; zero off a phone, so a desktop's gauge stays where it was.</summary>
    private void SeatTheNerveBlockUnderTheMasthead(in State state)
    {
        if (!state.ShowNerve)
        {
            return;
        }
        long now = Environment.TickCount64;
        if (now - _mastheadSampledAtMs >= 500)
        {
            _mastheadSampledAtMs = now;
            _mastheadBottomPx = MastheadBottom();
        }
        double plateTop = HudColumn.For(state.NerveCompact).PlateTop;
        _nerveShift = Math.Max(_nerveShift, (_mastheadBottomPx > 0 ? Math.Max(0.0, _mastheadBottomPx + 2.0 - plateTop) : 0.0));
    }

    private long _mastheadSampledAtMs = long.MinValue / 2;
    private double _mastheadBottomPx;

    /// <summary>#1466 · Where the page's masthead ends, in canvas pixels. The browser reads the live tab bar; a
    /// bench sets it to the number it wants to prove the seating against.</summary>
    internal Func<double> MastheadBottom { get; set; } = RendererInterop.MastheadBottom;

    /// <summary>#870 lane 7b · The corner and edge chrome, which is not the world and never was: #313's
    /// motion fan, #317/#330's nerve gauge and its ledger, #327's orbit line, #825's stall banner on its
    /// own line under it, the keybar, and #440's standing prompt above it.</summary>
    private void DrawTheInstruments(
        in State state, SurfaceHud? surface, double simTime, int widthPx, int heightPx)
    {
        // #313 the motion tracker: a crude corner fan of MOVING blips (bearing/range), including
        // contacts beyond the grid edge — the early warning. Cadence pulses the blips as they close.
        _trackerCaptionsBottom = 0f;
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
        // (#1463) The rows were laid out at the top of the frame by LayOutTheBand, because the sentries' seat and the
        // nerve block's seat both depend on how many rows the band really needs.
        (string[] Rows, double Px) orbitFit = (_orbitRows, _orbitPx);
        (string[] Rows, double Px) stallFit = (_stallRows, _stallPx);
        if (orbitFit.Rows.Length + stallFit.Rows.Length > 0)
        {
            var drawn = new List<(string Text, double Px)>();
            drawn.AddRange(orbitFit.Rows.Select(r => (r, orbitFit.Px)));
            drawn.AddRange(stallFit.Rows.Select(r => (r, stallFit.Px)));
            (double px, double py, double pw, double ph) = CommsBand.PlateForRows(widthPx / 2.0, drawn);
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
            for (int r = 0; r < orbitFit.Rows.Length; r++)
            {
                _renderer.DrawText(widthPx / 2f, (float)CommsBand.BaselineY(r), orbitFit.Rows[r], color,
                    string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{orbitFit.Px:0.#}px monospace"),
                    TextAlign.Center);
            }
        }

        // #825 · THE MACHINE'S OWN BANNER, on its own line, under the ship's. The owner had "SIGNAL BREAKING
        // UP" across the top while the thing that was actually broken was the frame rate — and a sentence
        // about a downlink is a worse answer than no sentence at all when the question is "why will my legs
        // not move". So it is drawn SEPARATELY (never appended to the orbit line), in the amber of a
        // machine-level warning rather than the comms grey, and it steps down a line when the ship is
        // already talking so neither fact ever paints over the other.
        if (state.StallBanner is { Length: > 0 } stall)
        {
            for (int r = 0; r < stallFit.Rows.Length; r++)
            {
                float y = (float)CommsBand.BaselineY(orbitFit.Rows.Length + r);
                _renderer.DrawText(widthPx / 2f, y, stallFit.Rows[r],
                    new RgbaColor(255, 190, 100, 235),
                    string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{stallFit.Px:0.#}px monospace"),
                    TextAlign.Center);
            }
        }

        // Blind-UI audit finding: with the tube off-camera, nothing said the ship was docked or
        // how to go ashore — the tester could only guess "airlock" by genre convention. On the surface
        // the keybar turns contextual (#324): the deploy/drop keys spell themselves out while they matter.
        // #440 · …and OFF the surface it turns contextual too. The ladder is the same shape it always was
        // with one rung added in the middle: the excursion's bar wins where there is an excursion, then the
        // deck's own composed bar (the bank's B at a contact's table, the mute), then the fixed sentence for
        // every caller that hands neither down. The two fixed sentences are DeckView's own consts so the
        // page composing the rung above them quotes them rather than re-typing them.
        string bottomHint = surface is { KeyHints: { Length: > 0 } hints }
            ? hints
            : state.KeyHints is { Length: > 0 } deckHints
                ? deckHints
                : state.Docked ? DockedKeyHints : DeckKeyHints;
        // R1-F75 · the keybar owns its band: a backing strip so a plate's label drawn underneath it (NEBULA
        // MUTUAL, GARDEN · GROWERS ONLY…) cannot print through the hint and make it unreadable.
        FillRect(0, heightPx - 22, widthPx, 22, new RgbaColor(8, 10, 14, 225));
        _renderer.DrawText(widthPx / 2f, heightPx - 10, bottomHint, TextDim, "11px monospace", TextAlign.Center);

        // #440: the standing prompt rides just ABOVE the keybar, bright and a size up — the same eyeline the
        // player already checks for keys, but unmistakably not chrome. Gently breathing so it reads as a
        // thing still owed rather than furniture.
        if (surface is { StandingPrompt: { Length: > 0 } standing })
        {
            double breathe = 0.78 + (0.22 * Math.Sin(simTime * 0.001 * 2.2));
            var promptColor = new RgbaColor(255, 205, 90, (byte)Math.Clamp(255 * breathe, 60, 255));
            _renderer.DrawText(widthPx / 2f, heightPx - 30, standing, promptColor, "bold 14px monospace", TextAlign.Center);
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
