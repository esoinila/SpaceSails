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

        public List<(float X, float Y, float W, float H, RgbaColor Ink)> Fills { get; } = [];

        public void BeginFrame(int w, int h, RgbaColor b) { }
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? f, RgbaColor s, float w = 1f) { }
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh, float x, float y, float w, float h, float a = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> p, RgbaColor? f, RgbaColor s, float w = 1f)
        {
            if (f is { } ink && p.Length == 8)
            {
                Fills.Add((Math.Min(p[0], p[4]), Math.Min(p[1], p[5]), Math.Abs(p[4] - p[0]), Math.Abs(p[5] - p[1]), ink));
            }
        }
        public void DrawPolyline(ReadOnlySpan<float> p, RgbaColor s, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Words.Add((x, y, text, font, align));
    }

    private static Pen Draw(string line, string? stall = null)
    {
        DeckPlan ground = Scenes.Build("surface:miranda:0");
        var state = new DeckView.State(ground.SpawnX, ground.SpawnY, 0.9, 0, 0,
            ShuttleAway: false, ElectricUniverse: false,
            Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: false, HitsTaken: 0,
            StallBanner: stall);
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

    private static (float X, float Y, float W, float H) TheBandPlate(Pen pen)
    {
        var plateInk = new RgbaColor(6, 11, 10, 205);
        var p = Assert.Single(pen.Fills, f => f.Ink == plateInk && Math.Abs(f.Y - CommsBand.PlateTop) < 0.01);
        return (p.X, p.Y, p.W, p.H);
    }

    private static bool Meet((float X, float Y, float W, float H) a, (float X, float Y, float W, float H) b) =>
        a.X < b.X + b.W && a.X + a.W > b.X && a.Y < b.Y + b.H && a.Y + a.H > b.Y;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheDrillRowsNeverBuryTheNerveGaugeOnAPhone(bool withStallBanner)
    {
        Pen pen = Draw(DrillLine, withStallBanner ? "SIGNAL BREAKING UP - the frame rate is falling behind the sim" : null);
        var plate = TheBandPlate(pen);
        Assert.True(plate.H > CommsBand.PlateHeight(1), "premise: the phone band is more than one row tall");

        // The nerve gauge's bar (the empty channel), its backing plate and the pips all sit below the band.
        var channel = pen.Fills.Where(f => f.Ink == new RgbaColor(14, 18, 24, 220)).ToList();
        Assert.NotEmpty(channel);
        foreach (var c in channel)
        {
            Assert.False(Meet(plate, (c.X, c.Y, c.W, c.H)),
                $"#1463 · the nerve bar ({c.X:0},{c.Y:0} {c.W:0}x{c.H:0}) lies under the band plate ({plate.X:0},{plate.Y:0} {plate.W:0}x{plate.H:0}) - the #324 never-buried law");
        }

        var nervePlate = pen.Fills.Where(f => f.Ink == new RgbaColor(6, 11, 10, 205) && f.Y > plate.Y + 1 && f.X < 40 && f.W > 200).ToList();
        Assert.NotEmpty(nervePlate);
        foreach (var n in nervePlate)
        {
            Assert.False(Meet(plate, (n.X, n.Y, n.W, n.H)), "the nerve block's own plate is under the band plate");
        }

        // …and every row of ink the band wrote lies inside its plate (the sentries' floor follows the same rows).
        foreach (var w in pen.Words.Where(w => w.Y < plate.Y + plate.H + 1 && w.Align == TextAlign.Center && w.Y > 0 && w.Y < 80))
        {
            Assert.True(w.Y <= plate.Y + plate.H, $"a band row is drawn at y {w.Y} below its plate's bottom {plate.Y + plate.H}");
        }
    }
}
