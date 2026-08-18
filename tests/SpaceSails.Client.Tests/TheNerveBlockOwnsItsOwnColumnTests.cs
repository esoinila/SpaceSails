using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #561 · THE NERVE BLOCK AND THE MOTION TRACKER ARE TWO INSTRUMENTS.
///
/// <para>Owner, playtesting Miranda on the regolith (<c>/map?dock=the-tilt&amp;site=0</c>): <i>"the UI could
/// use a little more space between the health and the motion tracker."</i> Two defects were stacked in that
/// one sentence and they had one cause — a typed <c>SanityColumnTop = 82.0</c> that was measured against a
/// nerve plate which afterwards grew a row of condition pips (#453) under it:</para>
/// <list type="number">
///   <item>the plate still ended at y=70 while the pips ran 70 → 80, so five red pips hung off their own
///   backing plate onto bare regolith;</item>
///   <item>the tracker's caption began at cap-top ≈ 81, one pixel below a pip bottom of 80, so nerve and
///   fan read as one block.</item>
/// </list>
///
/// <para><b>Why these guards are measured off a PEN and not off the constants.</b> A test that asserted
/// <c>HudColumn.Surface.ColumnTop == PlateBottom + GapPx</c> would restate the formula and pass on any
/// world — this repo's fifth named bug class, a green test that asserts nothing. So the frame is DRAWN by
/// the real <see cref="DeckView"/> onto a measuring pen, and the plate, the pips and the caption are read
/// back out of the ink. Core supplies only the law the ink is judged against.</para>
/// </summary>
public sealed class TheNerveBlockOwnsItsOwnColumnTests
{
    private const int WidthPx = 1280, HeightPx = 800;

    /// <summary>Cap-height as a fraction of the declared font size. Monospace faces sit near 0.72 and the
    /// issue's own measurement used it; it is only ever used to push the caption's top DOWN toward the
    /// pips, so a face with a shorter cap makes the gap larger, never smaller.</summary>
    private const double CapHeightRatio = 0.72;

    // ── THE PEN THAT MEASURES ─────────────────────────────────────────────────────────────────────────
    //
    // It keeps only what this lane is about: axis-aligned filled rects (FillRect draws a 4-point polygon),
    // axis-aligned stroked rects (DrawRectOutline draws a 5-point closed polyline) and text with its font.

    private readonly record struct Mark(float X, float Y, float W, float H, RgbaColor Ink)
    {
        public float Bottom => Y + H;

        public bool Contains(in Mark inner) =>
            inner.X >= X - 1e-3f && inner.Y >= Y - 1e-3f &&
            inner.X + inner.W <= X + W + 1e-3f && inner.Bottom <= Bottom + 1e-3f;

        public override string ToString() =>
            string.Create(CultureInfo.InvariantCulture, $"({X:0.##},{Y:0.##}) {W:0.##}×{H:0.##}");
    }

    private readonly record struct Word(float X, float Y, string Text, string Font);

    private sealed class MeasuringPen : IRenderer
    {
        public List<Mark> Fills { get; } = [];

        public List<Mark> Outlines { get; } = [];

        public List<Word> Words { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background)
        {
        }

        public void EndFrame()
        {
        }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f)
        {
        }

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f)
        {
        }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f)
        {
        }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f)
        {
            if (fill is { } ink && Rect(pointsXY, 4) is { } m)
            {
                Fills.Add(m with { Ink = ink });
            }
        }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f)
        {
            if (Rect(pointsXY, 5) is { } m)
            {
                Outlines.Add(m with { Ink = stroke });
            }
        }

        public void DrawText(float x, float y, string text, RgbaColor color,
                             string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Words.Add(new Word(x, y, text, font));

        /// <summary>An axis-aligned rectangle, or null if these points are not one.</summary>
        private static Mark? Rect(ReadOnlySpan<float> xy, int corners)
        {
            if (xy.Length != corners * 2)
            {
                return null;
            }
            float x0 = xy[0], y0 = xy[1], x1 = xy[4], y1 = xy[5];
            bool square =
                Math.Abs(xy[2] - x1) < 1e-3f && Math.Abs(xy[3] - y0) < 1e-3f &&
                Math.Abs(xy[6] - x0) < 1e-3f && Math.Abs(xy[7] - y1) < 1e-3f;
            return square ? new Mark(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0), Math.Abs(y1 - y0), default) : null;
        }
    }

    // ── THE TWO FRAMES THE GAUGE IS EVER DRAWN IN ─────────────────────────────────────────────────────

    /// <summary>The regolith: the gauge is the full-size head of the column and the fan seats under it.</summary>
    private static MeasuringPen OnTheRegolith()
    {
        DeckPlan ground = Scenes.Build("surface:luna:0");
        var state = new DeckView.State(ground.SpawnX, ground.SpawnY, 0.9, 0, 0,
            ShuttleAway: false, ElectricUniverse: false,
            Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: false, HitsTaken: 1);
        var hud = new DeckView.SurfaceHud(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [(0.3, 12.0, false)], Cadence: 2, Readout: "CONTACT · 12 m",
            CacheMarks: [], Nerve: 58, NerveReadout: "FRAYED", Instruments: true);

        var pen = new MeasuringPen();
        new DeckView(pen).Draw(ground, WidthPx, HeightPx, simTime: 0.0, in state, surface: hud);
        return pen;
    }

    /// <summary>Aboard: the gauge whispers, compact, tucked below the deck chrome. No fan up here — but the
    /// plate still has to back its own pips, and the column top it publishes still has to be honest.</summary>
    private static MeasuringPen Aboard()
    {
        DeckPlan ship = Scenes.Build("ship");
        var state = new DeckView.State(-2.5, 1.5, 0.7, CargoUnits: 7, Charge: 0.62,
            ShuttleAway: false, ElectricUniverse: false,
            ShowNerve: true, NerveCompact: true, Nerve: 71, NerveReadout: "STEADY", HitsTaken: 2);

        var pen = new MeasuringPen();
        new DeckView(pen).Draw(ship, WidthPx, HeightPx, simTime: 0.0, in state);
        return pen;
    }

    /// <summary>The nerve gauge's ink, picked out of a whole frame without a single typed coordinate: the
    /// five SQUARE frames in the gauge's own ash-blue ink are #453's condition pips, the sixth rectangle in
    /// that ink is the nerve bar, and the plate is the dark fill that contains the bar.</summary>
    private static (Mark Plate, List<Mark> Pips) NerveInk(MeasuringPen pen)
    {
        var frame = new RgbaColor(150, 170, 190, 175);
        List<Mark> inGaugeInk = pen.Outlines.Where(m => m.Ink == frame).ToList();
        List<Mark> pips = inGaugeInk.Where(m => Math.Abs(m.W - m.H) < 1e-3f).ToList();
        List<Mark> bars = inGaugeInk.Where(m => Math.Abs(m.W - m.H) >= 1e-3f).ToList();

        Assert.Equal(CaptainCondition.MaxHits, pips.Count);
        Mark bar = Assert.Single(bars);

        List<Mark> plates = pen.Fills
            .Where(m => m.Ink == new RgbaColor(6, 11, 10, 205) && m.Contains(bar))
            .ToList();
        Mark plate = Assert.Single(plates);
        return (plate, pips.OrderBy(p => p.X).ToList());
    }

    // ── GUARD (a) · THE PLATE BACKS ITS OWN PIPS ──────────────────────────────────────────────────────

    /// <summary>
    /// Every condition pip lies INSIDE the nerve gauge's backing plate — on the regolith, where the owner
    /// saw them on bare ground, and aboard, where the same arithmetic hung them 7px past the plate's edge.
    ///
    /// <para>RED PROOF: restore the old typed plate height (<c>h + 42f</c> in place of
    /// <c>block.PlateHeight</c>) and this fails naming both frames and the overhang in pixels.</para>
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheConditionPipsLieInsideTheNervePlate(bool compact)
    {
        MeasuringPen pen = compact ? Aboard() : OnTheRegolith();
        (Mark plate, List<Mark> pips) = NerveInk(pen);

        List<string> hanging = pips
            .Where(p => !plate.Contains(p))
            .Select(p => $"  pip {p} hangs {p.Bottom - plate.Bottom:0.##}px past the plate {plate}")
            .ToList();

        Assert.True(hanging.Count == 0,
            $"#561 · {(compact ? "aboard" : "on the regolith")} the condition pips are drawn outside their own "
            + $"backing plate — the plate must be measured to what it backs:\n{string.Join("\n", hanging)}");
    }

    // ── GUARD (b) · THE CAPTION KEEPS ITS AIR ─────────────────────────────────────────────────────────

    /// <summary>Where the tracker's caption's TOP lands for a given block, using the game's own anchor
    /// arithmetic (<see cref="MotionTracker"/>) and the game's own label-size rule.</summary>
    private static double CaptionCapTop(HudColumn.NerveBlock block)
    {
        double r = MotionTracker.TrackerRadius(WidthPx, HeightPx, block.ColumnTop, desired: 116.0);
        (double _, double cy) = MotionTracker.TrackerAnchor(WidthPx, HeightPx, r, block.ColumnTop);
        double labelPx = Math.Round(Math.Clamp(r * 0.13, 10, 15));   // DeckView's own label rule, "{labelPx:0}px"
        return cy - r - 8 - (labelPx * CapHeightRatio);              // baseline, less the caps above it
    }

    /// <summary>
    /// The MOTION TRACKER caption starts at least <see cref="HudColumn.GapPx"/> below the bottom of the
    /// scar pips — on the surface layout, where the fan is really drawn, and on the compact one, whose
    /// published column top is measured against the compact pips the frame really draws.
    ///
    /// <para>The surface leg first proves the arithmetic is not a fiction: the cap-top computed from
    /// <see cref="HudColumn.Surface"/> is the cap-top of the caption the pen actually recorded.</para>
    ///
    /// <para>RED PROOF: put the old <c>SanityColumnTop = 82.0</c> back (surface leg: the gap collapses to
    /// ~1px), or set <c>HudColumn.GapPx</c> to 0 (both legs).</para>
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheTrackersCaptionKeepsItsAirBelowTheScarPips(bool compact)
    {
        MeasuringPen pen = compact ? Aboard() : OnTheRegolith();
        (Mark _, List<Mark> pips) = NerveInk(pen);
        double pipsBottom = pips.Max(p => p.Bottom);

        HudColumn.NerveBlock block = HudColumn.For(compact);
        double capTop = CaptionCapTop(block);

        if (!compact)
        {
            // The fan is drawn here, so the computed caption must BE the drawn one.
            Word caption = Assert.Single(pen.Words, w => w.Text == "MOTION TRACKER");
            double drawnPx = double.Parse(
                Regex.Match(caption.Font, @"(\d+(?:\.\d+)?)px").Groups[1].Value, CultureInfo.InvariantCulture);
            double drawnCapTop = caption.Y - (drawnPx * CapHeightRatio);
            Assert.Equal(drawnCapTop, capTop, 3);
        }

        double air = capTop - pipsBottom;
        Assert.True(air >= HudColumn.GapPx - 1e-6,
            $"#561 · {(compact ? "compact" : "surface")}: the scar pips end at y={pipsBottom:0.##} and the "
            + $"MOTION TRACKER caption starts at y={capTop:0.##} — {air:0.##}px of air, and two instruments "
            + $"need at least HudColumn.GapPx = {HudColumn.GapPx:0.##}px between them.");
    }

    // ── GUARD (c) · THE COLUMN IS ASKED FOR, NEVER TYPED ──────────────────────────────────────────────

    /// <summary>
    /// No column top anywhere in the shipping source or the tests is a NUMBER. It was one for two releases
    /// after the thing it measured grew, which is exactly how a constant governing one thing while another
    /// grew under it survives — nothing in the build could tell that 82 had stopped being true.
    ///
    /// <para>RED PROOF: put <c>private const double SanityColumnTop = 82.0;</c> back in
    /// <c>DeckView.Hud.cs</c> (or the mirrored <c>ColumnTop = 82.0</c> back in
    /// <c>MotionTrackerTests</c>) and this fails naming the file, the line and the literal.</para>
    /// </summary>
    [Fact]
    public void TheTrackersColumnTopIsDerivedFromTheNerveBlockAndNeverTyped()
    {
        // No word boundary in front: the old offender was called SanityColumnTop, and the next one will be
        // called something else again. What is being caught is the SHAPE — any column top set to a number.
        var typed = new Regex(@"ColumnTop\s*(?:=>|=)\s*-?\d", RegexOptions.None);
        var offenders = new List<string>();

        foreach (string dir in new[] { "src", "tests", "labs" })
        {
            string root = Path.Combine(RepoRoot(), dir);
            if (!Directory.Exists(root))
            {
                continue;
            }
            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                         .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    // Prose may say "82.0" — the whole story of this lane is written in comments that quote
                    // it. Only CODE is swept.
                    string line = lines[i].TrimStart();
                    if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*'))
                    {
                        continue;
                    }
                    if (typed.IsMatch(lines[i]))
                    {
                        offenders.Add($"  {Path.GetRelativePath(RepoRoot(), file)}:{i + 1} — {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "#561 · a column top is a MEASUREMENT of the block above it, not a number somebody typed once:\n"
            + string.Join("\n", offenders));

        // …and the one place that seats the fan asks the nerve block for it.
        string hud = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Rendering", "DeckView.Hud.cs"));
        Assert.Contains("HudColumn.Surface.ColumnTop", hud, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "SpaceSails.slnx")))
            {
                return d.FullName;
            }
        }
        throw new DirectoryNotFoundException("Could not find SpaceSails.slnx above the test assembly.");
    }
}
