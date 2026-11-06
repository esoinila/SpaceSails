using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1463 item 3 · THE DRILL HUD FITS A 390 PX GLASS. The live-site pass found the rock's one centred canvas line
/// clipped at both edges on a phone, "· bore, then FIRE" lost. The frame is drawn onto a recording pen and each
/// text row is measured by the band's own metric (<see cref="CommsBand.WidthOf(string,double)"/>) at the size
/// the pen was handed, so the guard reads the ink and not the constants.
///
/// <para>RED PROOF: on the old renderer (one row at a fixed 13px) the line measures about 540 px against 390 and
/// <see cref="EveryRowOfTheDrillLineLiesInsideTheGlass"/> fails.</para>
/// </summary>
public sealed class TheDrillHudFitsAPhoneGlassTests
{
    private const int PhoneW = 390, PhoneH = 700;

    private const string DrillLine =
        "⏱ IMPACT — RINGSIDE EXCHANGE — T-5:58 · DRILL POINT deepward — 115 du · bore, then FIRE";

    private sealed class Pen : IRenderer
    {
        public List<(float X, float Y, string Text, string Font, TextAlign Align)> Words { get; } = [];

        public void BeginFrame(int w, int h, RgbaColor b) { }
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? f, RgbaColor s, float w = 1f) { }
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh, float x, float y, float w, float h, float a = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> p, RgbaColor? f, RgbaColor s, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> p, RgbaColor s, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Words.Add((x, y, text, font, align));
    }

    private static Pen Draw(string line)
    {
        DeckPlan ground = Scenes.Build("surface:miranda:0");
        var state = new DeckView.State(ground.SpawnX, ground.SpawnY, 0.9, 0, 0,
            ShuttleAway: false, ElectricUniverse: false,
            Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: false, HitsTaken: 0);
        var hud = new DeckView.SurfaceHud(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [], Cadence: 2, Readout: "quiet",
            CacheMarks: [], Nerve: 58, NerveReadout: "FRAYED", Instruments: true,
            OrbitComms: line, OrbitSeverity: 1);
        var pen = new Pen();
        new DeckView(pen).Draw(ground, PhoneW, PhoneH, simTime: 0.0, in state, surface: hud);
        return pen;
    }

    [Fact]
    public void EveryRowOfTheDrillLineLiesInsideTheGlass()
    {
        Pen pen = Draw(DrillLine);
        var rows = pen.Words.Where(w => w.Y < 60 && w.Align == TextAlign.Center && w.Text.Length > 8
            && (DrillLine.Contains(w.Text, StringComparison.Ordinal))).ToList();
        Assert.NotEmpty(rows);

        foreach (var w in rows)
        {
            double px = double.Parse(Regex.Match(w.Font, @"(\d+(?:\.\d+)?)px").Groups[1].Value, CultureInfo.InvariantCulture);
            double width = CommsBand.WidthOf(w.Text, px);
            Assert.True(w.X - (width / 2) >= 0 && w.X + (width / 2) <= PhoneW,
                $"#1463 · the row \"{w.Text}\" at {px}px measures {width:0.#}px centred at {w.X:0.#}: it spills past the 0..{PhoneW} glass.");
            Assert.True(px >= 6.0, $"the row is shrunk to an unreadable {px}px");
        }

        string joined = string.Join(" · ", rows.Select(r => r.Text));
        Assert.Contains("T-5:58", joined);
        Assert.Contains("deepward", joined);
        Assert.Contains("bore, then FIRE", joined);
    }

    [Fact]
    public void ALineThatAlreadyFitsIsLeftAtThirteenOnOneRow()
    {
        (string[] rows, double px) = CommsBand.Fit("⚓ docked — the station holds the ship", 1264);
        Assert.Single(rows);
        Assert.Equal(CommsBand.LinePx, px);
    }
}
