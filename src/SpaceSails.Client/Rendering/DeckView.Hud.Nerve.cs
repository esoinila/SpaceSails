using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · THE NERVE GAUGE AND THE NERVE LEDGER (#317) — drawing the gauge top-left, and the ledger of what
/// broke you beneath it.
///
/// <para>Split out of <c>DeckView.Hud.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. <c>NerveFrame</c>, a <c>static readonly</c>, stays in the opening file
/// with the comment that introduces the gauge (#1163); the column top that
/// TheNerveBlockOwnsItsOwnColumnTests reads by this file's path stays there too.</para>
/// </summary>
public sealed partial class DeckView
{
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
