using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using SpaceSails.Client.Rendering;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>#1478 · At the monolith the key-hint captions under the motion tracker (T deploy here, shift-T hold my
/// line home...) ran down into the NERVE LEDGER anchored at the bottom of the same column. The ledger gives up
/// its OLDEST rows until its block clears the captions.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLedgerSitsClearOfTheKeyHintsTests
{
    [Fact]
    public void WithNoCaptionsEveryLineFits()
    {
        Assert.Equal(5, DeckView.NerveLedgerLinesThatFit(5, false, 13f, 720, 0f));
    }

    [Fact]
    public void CaptionsThatReachDownTrimTheOldestRows()
    {
        // 720 - 136 = 584 is the ledger's floor. Captions ending at 520 leave 64 - 6 = 58 px: header + 3 lines of 13.
        int keep = DeckView.NerveLedgerLinesThatFit(8, false, 13f, 720, 520f);
        Assert.Equal(3, keep);
        float top = 720 - DeckView.NerveLedgerBottomClearancePx - ((keep + 1) * 13f);
        Assert.True(top >= 520f + 6f, "the kept block still reaches into the captions.");
    }

    [Fact]
    public void NoRoomAtAllDrawsNoLedger()
    {
        Assert.Equal(0, DeckView.NerveLedgerLinesThatFit(8, false, 13f, 720, 580f));
    }

    private sealed class Pen : IRenderer
    {
        public List<string> Said { get; } = [];
        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Said.Clear();
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) => Said.Add(text);
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
    }

    private static int LedgerRowsDrawn(string[]? captions)
    {
        var plan = new DeckPlan([], [], [], [], spawnX: 0, spawnY: 0, droidCount: 0,
            fillDroids: (_, _) => { }, location: (_, _) => "field");
        var pen = new Pen();
        var hud = new DeckView.SurfaceHud(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [], Cadence: 1, Readout: "no contact", CacheMarks: [],
            Nerve: 80, NerveReadout: "STEADY", Instruments: true, TrackerCaptions: captions);
        string[] ledger = ["cause a  -1", "cause b  -1", "cause c  +1", "cause d  -1", "cause e  -1", "cause f  +1"];
        var state = new DeckView.State(0, 0, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false,
            ShowNerve: true, NerveLedger: ledger);
        new DeckView(pen).Draw(plan, 1200, 700, simTime: 0, in state, surface: hud);
        return pen.Said.Count(t => t.StartsWith("cause ", StringComparison.Ordinal));
    }

    /// <summary>The wiring: DrawNerveLedger really asks NerveLedgerLinesThatFit. With tall key-hint captions the
    /// drawn ledger is shorter than the same ledger without them; take the call out and both draw all six.</summary>
    [Fact]
    public void TheDrawnLedgerYieldsToTheCaptionsUnderTheTracker()
    {
        string[] captions = [.. Enumerable.Range(0, 8).Select(i => $"caption line {i} - T deploy here")];
        int free = LedgerRowsDrawn(null);
        int squeezed = LedgerRowsDrawn(captions);
        Assert.Equal(6, free);
        Assert.True(squeezed < free, $"the ledger drew {squeezed} rows under the captions, the same as without them.");
    }
}
