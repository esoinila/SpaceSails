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

        public List<byte> Alphas { get; } = [];

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
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif", TextAlign align = TextAlign.Left)
        {
            Words.Add((x, y, text, font, align));
            Alphas.Add(c.A);
        }
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

    [Fact]
    public void TheNerveGaugeDoesNotHopWhenTheStallBannerComesAndGoes()
    {
        Pen without = Draw(DrillLine);
        Pen with = Draw(DrillLine, "SIGNAL BREAKING UP");
        float Y(Pen pen) => Assert.Single(pen.Fills, f => f.Ink == new RgbaColor(14, 18, 24, 220) && f.W > 100).Y;
        Assert.Equal(Y(without), Y(with));
    }

    /// <summary>#1466 · THE WORLD'S WORDS NEVER SIT UNDER THE BAND. The live pass of #1464 found the moored ship's
    /// "ENGINE ROOM", "SCUTTLING CHARGES", the ⚓ and the boxed AIRLOCK showing through the drill line at 390x700:
    /// the band is the last thing drawn but its plate is translucent. The moored ship on the rock's ground is drawn at a sweep of
    /// captain positions (the follow-cam moves every label across the top of the glass) and no FULL-alpha text other than the
    /// band's own rows may touch the plate rectangle.</summary>
    [Fact]
    public void NoWorldLabelIsDrawnUnderTheBandPlate()
    {
        DeckPlan ship = Scenes.Build("surface:miranda:0");
        int underneath = 0, drawn = 0; var offenders = new List<string>();
        for (double dy = -26; dy <= 2; dy += 1.0)
        {
            for (double dx = -20; dx <= 20; dx += 4.0)
            {
                var state = new DeckView.State(ship.SpawnX + dx, ship.SpawnY + dy, 0.9, 0, 0,
                    ShuttleAway: false, ElectricUniverse: false,
                    Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: false, HitsTaken: 0);
                var hud = new DeckView.SurfaceHud(
                    DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
                    Blips: [], Cadence: 2, Readout: "quiet",
                    CacheMarks: [], Nerve: 58, NerveReadout: "FRAYED", Instruments: true,
                    OrbitComms: DrillLine, OrbitSeverity: 1);
                var pen = new Pen();
                new DeckView(pen).Draw(ship, PhoneW, PhoneH, simTime: 0.0, in state, surface: hud);
                var plate = TheBandPlate(pen);
                for (int wi = 0; wi < pen.Words.Count; wi++)
                {
                    var w = pen.Words[wi];
                    if (pen.Alphas[wi] < 128) { continue; }   // a dimmed label is a yielded one
                    if (DrillLine.Contains(w.Text, StringComparison.Ordinal) && w.Align == TextAlign.Center && w.Y < plate.Y + plate.H + 1)
                    {
                        continue; // the band's own rows
                    }
                    drawn++;
                    double px = double.Parse(Regex.Match(w.Font, @"(\d+(?:\.\d+)?)px").Groups[1].Value, CultureInfo.InvariantCulture);
                    double width = CommsBand.WidthOf(w.Text, px);
                    double left = w.Align == TextAlign.Center ? w.X - (width / 2) : w.Align == TextAlign.Right ? w.X - width : w.X;
                    if (Meet(plate, ((float)left, (float)(w.Y - MarkBand.AscentOf(px)), (float)width, (float)(MarkBand.AscentOf(px) + MarkBand.DescentOf(px)))))
                    {
                        underneath++; offenders.Add($"{w.Text}@{w.X:0},{w.Y:0} {w.Font}");
                    }
                }
            }
        }
        Assert.True(drawn > 100, $"premise: the sweep drew only {drawn} labels");
        Assert.True(underneath == 0, string.Join(" | ", offenders.Distinct()));
    }

    /// <summary>#1466 item 3 · THE ASHORE NERVE BLOCK CLEARS A WRAPPED TAB BAR. At 390x700 the three-row desk bar's
    /// bottom is y 117 while the compact block's plate began at y 92, so "NERVE" (y 95-110) was under the bar.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(117.0)]
    public void TheCompactNerveBlockStartsBelowTheMasthead(double mastheadBottom)
    {
        DeckPlan ship = Scenes.Build("ship");
        var state = new DeckView.State(ship.SpawnX, ship.SpawnY, 0.9, 0, 0,
            ShuttleAway: false, ElectricUniverse: false,
            Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: true, HitsTaken: 0);
        var pen = new Pen();
        new DeckView(pen) { MastheadBottom = () => mastheadBottom }.Draw(ship, PhoneW, PhoneH, simTime: 0.0, in state);
        var label = Assert.Single(pen.Words, w => w.Text == "NERVE");
        var plate = pen.Fills.Where(f => f.Ink == new RgbaColor(6, 11, 10, 205) && f.X < 40 && f.W > 100 && f.W < 200).OrderBy(f => f.Y).First();
        Assert.True(plate.Y >= mastheadBottom - 0.01,
            $"#1466 · the nerve plate begins at y {plate.Y:0.#}, under the masthead that ends at y {mastheadBottom:0.#}");
        Assert.True(label.Y - 9 >= mastheadBottom, $"the NERVE label (baseline y {label.Y:0.#}) rides under the masthead ({mastheadBottom:0.#})");
        if (mastheadBottom == 0.0)
        {
            Assert.InRange(plate.Y, 89.9, 94.1);   // a desktop's gauge stays exactly where it was
        }
    }
}
