using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// R1-F76 (#1400) · ONE RULE FOR EVERY PLATE LAID OVER THE GLASS: the world's words that touch it dim.
/// #1466 did it for the comms band. The live pass of R1-F76 still found the PULSE TOAST (a DOM box the pen cannot
/// see) sitting on "B24 · THE BERTH OFFICE" and its "-426 m", and the STANDING PROMPT ("CARRYING THE CHEST") printed
/// over "— MIRANDA SURFACE —". The frame is drawn onto a recording pen at a sweep of captain positions and no
/// full-alpha world word may touch the plate.
///
/// <para>RED PROOF: with <c>ReserveTheOtherPlates</c> not reserving, each sweep finds the surface's own labels at full
/// alpha under the toast / the prompt and the sweeps fail.</para>
/// </summary>
public sealed class TheToastAndThePromptHoldTheirBandTests
{
    private const int W = 1280, H = 720;

    private const string Toast =
        "🛗 The car does not decide anything about you. The doors close and it drops, and it keeps dropping, at the pace of "
        + "a building that has never once been asked to wait. Nobody dug this in an afternoon, and whoever paid for it did not notice the cost.";

    private const string LowToast = "🛗 The doors part on nothing. No pressure, no lights but yours.";

    private const string Prompt = "⛏ CARRYING THE CHEST — press E to BURY IT HERE";

    private sealed class Pen : IRenderer
    {
        public List<(float X, float Y, string Text, string Font, TextAlign Align, byte Alpha)> Words { get; } = [];

        public void BeginFrame(int w, int h, RgbaColor b) { }
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? f, RgbaColor s, float w = 1f) { }
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh, float x, float y, float w, float h, float a = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> p, RgbaColor? f, RgbaColor s, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> p, RgbaColor s, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Words.Add((x, y, text, font, align, c.A));
    }

    private const string BandLine = "⚓ docked — the station holds the ship, no fuel spent on keeping";

    private static Pen Draw(DeckPlan plan, double dx, double dy, string? toast, bool low, string? prompt, string? band = null)
    {
        var state = new DeckView.State(plan.SpawnX + dx, plan.SpawnY + dy, 0.9, 0, 0,
            ShuttleAway: false, ElectricUniverse: false,
            Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: false, HitsTaken: 0,
            PulseToast: toast, PulseToastLow: low);
        var hud = new DeckView.SurfaceHud(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [], Cadence: 2, Readout: "quiet",
            CacheMarks: [], Nerve: 58, NerveReadout: "FRAYED", Instruments: true,
            StandingPrompt: prompt, OrbitComms: band, OrbitSeverity: 0);
        var pen = new Pen();
        new DeckView(pen).Draw(plan, W, H, simTime: 0.0, in state, surface: hud);
        return pen;
    }

    private static (double L, double T, double R, double B) InkOf((float X, float Y, string Text, string Font, TextAlign Align, byte Alpha) w)
    {
        double px = double.Parse(Regex.Match(w.Font, @"(\d+(?:\.\d+)?)px").Groups[1].Value, CultureInfo.InvariantCulture);
        double width = CommsBand.WidthOf(w.Text, px);
        double left = w.Align == TextAlign.Center ? w.X - (width / 2) : w.Align == TextAlign.Right ? w.X - width : w.X;
        return (left, w.Y - MarkBand.AscentOf(px), left + width, w.Y + MarkBand.DescentOf(px));
    }

    /// <summary>Sweeps the captain over the surface; returns the full-alpha words touching the plate and how many dimmed ones did.</summary>
    private static (List<string> Bare, int Dimmed) Sweep(
        (double X, double Y, double W, double H) plate, string? toast, bool low, string? prompt, Func<string, bool> ownWords,
        string? band = null)
    {
        DeckPlan ground = Scenes.Build("surface:miranda:0");
        var bare = new List<string>();
        int dimmed = 0;
        for (double dy = -70; dy <= 40; dy += 1.0)
        {
            for (double dx = -24; dx <= 24; dx += 4.0)
            {
                Pen pen = Draw(ground, dx, dy, toast, low, prompt, band);
                foreach (var w in pen.Words)
                {
                    if (ownWords(w.Text))
                    {
                        continue;
                    }
                    (double l, double t, double r, double b) = InkOf(w);
                    if (l < plate.X + plate.W && r > plate.X && t < plate.Y + plate.H && b > plate.Y)
                    {
                        if (w.Alpha >= 128) { bare.Add($"{w.Text}@{w.X:0},{w.Y:0}"); } else { dimmed++; }
                    }
                }
            }
        }
        return (bare, dimmed);
    }

    private static bool NotWorld(string text) =>
        text.Contains("press E to", StringComparison.Ordinal)
        || text.Contains("steady hands", StringComparison.Ordinal);

    [Fact]
    public void NoWorldWordIsDrawnUnderThePulseToast()
    {
        var plate = PulseToastPlate.Of(Toast, low: false, W, H);
        (List<string> bare, int dimmed) = Sweep(plate, Toast, low: false, prompt: null, NotWorld);
        Assert.True(dimmed + bare.Count > 20, $"premise: the sweep put only {dimmed + bare.Count} words under the toast");
        Assert.True(bare.Count == 0, "R1-F76 · full-alpha world words under the toast: " + string.Join(" | ", new HashSet<string>(bare)));
    }

    [Fact]
    public void NoWorldWordIsDrawnUnderTheLowToast()
    {
        var plate = PulseToastPlate.Of(LowToast, low: true, W, H);
        (List<string> bare, int dimmed) = Sweep(plate, LowToast, low: true, prompt: null,
            t => t.Contains("WASD", StringComparison.Ordinal));
        Assert.True(dimmed + bare.Count > 0, "premise: nothing sits under the low toast");
        Assert.True(bare.Count == 0, "R1-F76 · full-alpha world words under the low toast: " + string.Join(" | ", new HashSet<string>(bare)));
    }

    [Fact]
    public void NoWorldWordIsDrawnUnderTheStandingPrompt()
    {
        const double px = 14.0;
        double w = CommsBand.WidthOf(Prompt, px);
        var plate = ((W - w) / 2.0, H - 30.0 - MarkBand.AscentOf(px), w, MarkBand.AscentOf(px) + MarkBand.DescentOf(px));
        (List<string> bare, int dimmed) = Sweep(plate, toast: null, low: false, Prompt,
            t => t == Prompt || t.Contains("WASD", StringComparison.Ordinal));
        Assert.True(dimmed + bare.Count > 0, "premise: nothing sits under the prompt at any captain position");
        Assert.True(bare.Count == 0, "R1-F76 · full-alpha world words under the standing prompt: " + string.Join(" | ", new HashSet<string>(bare)));
    }

    /// <summary>WITH THE COMMS BAND UP TOO. The band's plate and the toast / prompt are all reserved together: a band that
    /// re-armed (replacing the other plates) instead of reserving would leave the toast's words bare.</summary>
    [Fact]
    public void TheCommsBandDoesNotUnreserveTheToastOrThePrompt()
    {
        var toastPlate = PulseToastPlate.Of(Toast, low: false, W, H);
        (List<string> bareT, int dimT) = Sweep(toastPlate, Toast, low: false, prompt: null, t => NotWorld(t) || t == BandLine, BandLine);
        Assert.True(dimT > 20, "premise: words sit under the toast");
        Assert.True(bareT.Count == 0, "with the band up, bare under the toast: " + string.Join(" | ", new HashSet<string>(bareT)));

        const double px = 14.0;
        double pw = CommsBand.WidthOf(Prompt, px);
        var promptPlate = ((W - pw) / 2.0, H - 30.0 - MarkBand.AscentOf(px), pw, MarkBand.AscentOf(px) + MarkBand.DescentOf(px));
        (List<string> bareP, int dimP) = Sweep(promptPlate, toast: null, low: false, Prompt,
            t => t == Prompt || t == BandLine || t.Contains("WASD", StringComparison.Ordinal), BandLine);
        Assert.True(dimP > 0, "premise: words sit under the prompt");
        Assert.True(bareP.Count == 0, "with the band up, bare under the prompt: " + string.Join(" | ", new HashSet<string>(bareP)));
    }

    /// <summary>The LOW toast is nowrap, so its box grows to min(40rem, 90vw), not to half the glass.</summary>
    [Fact]
    public void TheLowToastGrowsToTheCssMaxWidthNotHalfTheGlass()
    {
        (double x, double _, double w, double _) = PulseToastPlate.Of(Toast, low: true, 1024, 700);
        Assert.True(w >= 640 - 0.5, $"a long nowrap line on 1024 wide is a 640 px box, estimated {w:0}");
        Assert.True(x <= 192 + 0.5, $"left edge {x:0} must reach 192");
        (double _, double _, double w2, double _) = PulseToastPlate.Of(Toast, low: true, 390, 700);
        Assert.True(w2 >= 351 - 0.5, $"on a 390 glass the cap is 90vw = 351, estimated {w2:0}");
    }

    /// <summary>The estimate covers the box the live site really drew (getBoundingClientRect on the deployed page,
    /// 2026-11-07, the four-row line above): the box is never wider than half the glass (left: 50% leaves it that much to
    /// shrink-to-fit in), so on a phone it is a tall narrow column.</summary>
    [Theory]
    [InlineData(1280, 720, 320, 96, 960, 210)]
    [InlineData(1600, 900, 480, 96, 1120, 210)]
    [InlineData(1024, 700, 256, 96, 768, 234)]
    [InlineData(390, 700, 98, 96, 292, 450)]
    public void TheEstimatedPlateCoversTheBoxTheBrowserDrew(int glassW, int glassH, double left, double top, double right, double bottom)
    {
        (double x, double y, double w, double h) = PulseToastPlate.Of(Toast, low: false, glassW, glassH);
        Assert.True(x <= left + 0.5 && x + w >= right - 0.5, $"width: {x:0}..{x + w:0} must cover {left}..{right}");
        Assert.True(y <= top && y + h >= bottom, $"height: {y:0}..{y + h:0} must cover {top}..{bottom}");
        Assert.True(h < (bottom - top) * 1.3 + 30, $"the estimate ({h:0}) is not absurdly taller than the {bottom - top:0} px box");
    }
}
