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
/// #986 F2 · THE SHIP'S OWN LINE IS NOT BURIED — AND NEITHER IS THE SENTRY'S.
///
/// <para>The boot sweep on #986 walked <c>/map?dock=the-tilt&amp;site=0&amp;land=1</c> in a real browser and
/// found the mothership's orbit line struck through: the sentry standing at the airlock projects to the top
/// centre of the frame, and its alarm-red magazine readout painted over the words <i>"holds the ship,"</i>.
/// The comment three lines above the draw said <i>"Never buried (the #324 visibility law)"</i> — the third
/// named bug class in this repository, a SENTENCE that reports one thing while the pen does another.</para>
///
/// <para><b>Why the guard is measured off a pen and not off the constants.</b> A test that asserted
/// <c>CommsBand.ReservedBottom &gt; CommsBand.PlateBottom(2)</c> would restate the formula and pass on any
/// build — the fifth named bug class, a green test that asserts nothing. So a real
/// <see cref="DeckView"/> DRAWS the frame onto a measuring pen and the two text rows are read back out of
/// the ink.</para>
///
/// <para><b>And the sentry is placed by measurement, never by a typed coordinate.</b> Where a deck unit lands
/// on the glass depends on the plan, the viewport and where the captain is standing; a hand-picked world
/// position that put the counter on the line today would drift off it the next time the placement changed,
/// and the guard would go quietly green on a still-broken frame. So the projection is SOLVED first — two
/// probe frames with the bot at two known deck positions, the counter read back from each — and the sentry is
/// then stood exactly where its digits land on the ship's line. On the unfixed renderer that placement is
/// exact, which is what makes the red proof below a real one.</para>
///
/// <para>RED PROOF (verbatim runs in the pull request): remove the
/// <c>plateTop &lt; CommsBand.ReservedBottom</c> flip from <c>DeckView.DrawTheSentries</c> and
/// <see cref="TheSentrysMagazineNeverSharesPixelsWithTheShipsLine"/> fails with the two rows and their
/// overlap in pixels; remove the <c>CommsBand.PlateFor</c> fill from <c>DeckView.DrawTheInstruments</c> and
/// <see cref="TheShipsLineIsDrawnOnItsOwnBackingPlate"/> fails saying the line is bare ink.</para>
/// </summary>
public sealed class TheShipsLineIsNeverBuriedTests
{
    private const int WidthPx = 1280, HeightPx = 720;

    /// <summary>The Tilt's ground: the world the sweep was walking when it found this.</summary>
    private const string Ground = "surface:miranda:0";

    /// <summary>The orbit line the owner had on screen, word for word.</summary>
    private const string OrbitLine = "⚓ docked — the station holds the ship, no fuel spent on keeping";

    /// <summary>A full drum, the way a sentry that has just been set reads.</summary>
    private const string Magazine = "99";

    // ── THE PEN THAT MEASURES ─────────────────────────────────────────────────────────────────────────

    private readonly record struct Word(float X, float Y, string Text, string Font, TextAlign Align);

    private readonly record struct Mark(float X, float Y, float W, float H, RgbaColor Ink);

    private sealed class MeasuringPen : IRenderer
    {
        public List<Mark> Fills { get; } = [];

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
            if (fill is { } ink && Rect(pointsXY) is { } m)
            {
                Fills.Add(m with { Ink = ink });
            }
        }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f)
        {
        }

        public void DrawText(float x, float y, string text, RgbaColor color,
                             string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Words.Add(new Word(x, y, text, font, align));

        /// <summary>An axis-aligned rectangle, or null if these four points are not one (FillRect draws a
        /// 4-point polygon).</summary>
        private static Mark? Rect(ReadOnlySpan<float> xy)
        {
            if (xy.Length != 8)
            {
                return null;
            }
            float x0 = xy[0], y0 = xy[1], x1 = xy[4], y1 = xy[5];
            bool square =
                Math.Abs(xy[2] - x1) < 1e-3f && Math.Abs(xy[3] - y0) < 1e-3f &&
                Math.Abs(xy[6] - x0) < 1e-3f && Math.Abs(xy[7] - y1) < 1e-3f;
            return square
                ? new Mark(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0), Math.Abs(y1 - y0), default)
                : null;
        }
    }

    // ── ONE FRAME OF THE TILT'S GROUND, WITH ONE SENTRY ON IT ─────────────────────────────────────────

    /// <summary>Draw the excursion with a single deployed sentry standing at <paramref name="botX"/>,
    /// <paramref name="botY"/> deck units. A FRESH <see cref="DeckView"/> each time, so the magazine's
    /// change-flash is in the same phase on every frame and the digits are the same size in all of them.</summary>
    private static MeasuringPen OnTheGroundWithASentryAt(double botX, double botY)
    {
        DeckPlan ground = Scenes.Build(Ground);
        var state = new DeckView.State(ground.SpawnX, ground.SpawnY, 0.9, 0, 0,
            ShuttleAway: false, ElectricUniverse: false,
            Nerve: 58, NerveReadout: "FRAYED", ShowNerve: true, NerveCompact: false, HitsTaken: 1);
        var hud = new DeckView.SurfaceHud(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [], Cadence: 2, Readout: "no movement — for now",
            CacheMarks: [], Nerve: 58, NerveReadout: "FRAYED", Instruments: true,
            Bots: [(botX, botY, Magazine, false, false, 0.0, 0.0)],
            OrbitComms: OrbitLine, OrbitSeverity: 0);

        var pen = new MeasuringPen();
        new DeckView(pen).Draw(ground, WidthPx, HeightPx, simTime: 0.0, in state, surface: hud);
        return pen;
    }

    /// <summary>The ink one drawn line of text puts on the glass: the band's own metric
    /// (<see cref="CommsBand"/>) applied at whatever size the pen recorded, so the ship's 13px line and the
    /// sentry's 32px digits are measured by one rule and not by two guesses.</summary>
    private static (double X, double Y, double W, double H) RowOf(Word w)
    {
        double px = double.Parse(
            Regex.Match(w.Font, @"(\d+(?:\.\d+)?)px").Groups[1].Value, CultureInfo.InvariantCulture);
        double width = CommsBand.WidthOf(w.Text, px);
        double x = w.Align switch
        {
            TextAlign.Center => w.X - (width / 2),
            TextAlign.Right => w.X - width,
            _ => w.X,
        };
        double k = px / CommsBand.LinePx;
        return (x, w.Y - (CommsBand.AscentPx * k), width, (CommsBand.AscentPx + CommsBand.DescentPx) * k);
    }

    private static Word TheShipsLine(MeasuringPen pen) => Assert.Single(pen.Words, w => w.Text == OrbitLine);

    private static Word TheSentrysMagazine(MeasuringPen pen) =>
        Assert.Single(pen.Words, w => w.Text == Magazine);

    private static bool Overlaps(
        (double X, double Y, double W, double H) a, (double X, double Y, double W, double H) b) =>
        a.X < b.X + b.W && a.X + a.W > b.X && a.Y < b.Y + b.H && a.Y + a.H > b.Y;

    /// <summary>
    /// THE SENTRY STOOD WHERE ITS DIGITS LAND ON THE SHIP'S LINE — solved off the pen, never typed.
    ///
    /// <para>The projection is affine (<c>DeckView.Draw</c>: <c>ox + dx·scale</c>, <c>oy − dy·scale</c>), so
    /// two probe frames a known distance apart give the scale, and the scale gives the deck position whose
    /// counter lands on any screen point asked for. The probes are stood in the middle of the frame, well
    /// clear of the band, so nothing the fix does can perturb the solve.</para>
    /// </summary>
    private static MeasuringPen ASentryUnderTheShipsLine()
    {
        const double ax = 0.0, ay = 0.0, d = 8.0;
        Word a = TheSentrysMagazine(OnTheGroundWithASentryAt(ax, ay));
        Word b = TheSentrysMagazine(OnTheGroundWithASentryAt(ax + d, ay + d));

        double scale = (b.X - a.X) / d;
        Assert.True(scale > 0.5,
            $"the probe frames moved the counter {b.X - a.X:0.###}px for {d} deck units — the projection this "
            + "guard solves for is not the one the frame is drawing");

        // The screen point the ship's line occupies, asked of the frame rather than typed.
        Word ship = TheShipsLine(OnTheGroundWithASentryAt(ax, ay));

        double wx = ax + ((ship.X - a.X) / scale);
        double wy = ay - ((ship.Y - a.Y) / scale);
        return OnTheGroundWithASentryAt(wx, wy);
    }

    // ── GUARD (a) · TWO ROWS, NEVER ONE ───────────────────────────────────────────────────────────────

    /// <summary>
    /// With a sentry standing exactly where its magazine readout would print on the mothership's orbit line,
    /// the two text rows do not share a single pixel.
    ///
    /// <para>RED PROOF: delete the <c>plateTop &lt; CommsBand.ReservedBottom</c> flip in
    /// <c>DeckView.DrawTheSentries</c> and this fails, printing both rows and their overlap.</para>
    /// </summary>
    [Fact]
    public void TheSentrysMagazineNeverSharesPixelsWithTheShipsLine()
    {
        MeasuringPen pen = ASentryUnderTheShipsLine();

        (double X, double Y, double W, double H) ship = RowOf(TheShipsLine(pen));
        (double X, double Y, double W, double H) mag = RowOf(TheSentrysMagazine(pen));

        Assert.False(Overlaps(ship, mag),
            $"#986 F2 · the ship's line at ({ship.X:0.#},{ship.Y:0.#}) {ship.W:0.#}×{ship.H:0.#} and the "
            + $"sentry's magazine at ({mag.X:0.#},{mag.Y:0.#}) {mag.W:0.#}×{mag.H:0.#} are drawn through each "
            + "other. \"Never buried (the #324 visibility law)\" is written three lines above the orbit line's "
            + "own draw call, and a comment is not a guard.");
    }

    // ── GUARD (b) · THE LAW'S OWN MECHANISM, NOT A PROMISE ────────────────────────────────────────────

    /// <summary>
    /// The ship's line is drawn on a backing plate, the way every other line the #324 law protects is — the
    /// nerve gauge's plate and the #612 air bar's are the same dark ink, and this one was the only never-
    /// buried line in the frame drawn on bare pixels.
    ///
    /// <para>RED PROOF: delete the <c>CommsBand.PlateFor</c> fill from <c>DeckView.DrawTheInstruments</c> and
    /// this fails saying the line stands on nothing.</para>
    /// </summary>
    [Fact]
    public void TheShipsLineIsDrawnOnItsOwnBackingPlate()
    {
        MeasuringPen pen = ASentryUnderTheShipsLine();
        (double X, double Y, double W, double H) ship = RowOf(TheShipsLine(pen));

        // The plate ink the law already uses for the nerve block and the air bar.
        var plateInk = new RgbaColor(6, 11, 10, 205);
        List<Mark> under = pen.Fills
            .Where(m => m.Ink == plateInk)
            .Where(m => m.X <= ship.X + 1e-3 && m.Y <= ship.Y + 1e-3 &&
                        m.X + m.W >= ship.X + ship.W - 1e-3 && m.Y + m.H >= ship.Y + ship.H - 1e-3)
            .ToList();

        Assert.True(under.Count > 0,
            $"#986 F2 · the ship's orbit line at ({ship.X:0.#},{ship.Y:0.#}) {ship.W:0.#}×{ship.H:0.#} is bare "
            + "ink on bare pixels. Every other line the #324 visibility law protects sits on a plate; this one "
            + "only said so in a comment.");
    }

    // ── GUARD (c) · THE BAND IS WIDE ENOUGH FOR BOTH LINES, ALWAYS ────────────────────────────────────

    /// <summary>
    /// The reserved band is measured for BOTH lines whether or not the machine is complaining, so a sentry
    /// counter does not hop up and down the frame as #825's stall banner comes and goes with the frame rate.
    ///
    /// <para>Asked of the drawn frame, not of the arithmetic: the band's own bottom must clear the lowest
    /// baseline the frame can print up there, which is the stall banner's when the ship is already talking.</para>
    /// </summary>
    [Fact]
    public void TheReservedBandClearsBothOfTheBandsOwnLines()
    {
        MeasuringPen pen = ASentryUnderTheShipsLine();
        Word ship = TheShipsLine(pen);

        Assert.Equal(CommsBand.BaselineY(0), ship.Y, 3);
        Assert.True(CommsBand.ReservedBottom > CommsBand.BaselineY(CommsBand.MaxLines - 1),
            "the band a sentry keeps out of must clear the LAST line the band can print, not the first");

        // The sentry's own scoreboard panel — the dark plate #314 draws its two digits on, picked out by its
        // ink rather than by a coordinate. It is the thing the band displaces, so it is the thing measured.
        Mark board = Assert.Single(pen.Fills, m => m.Ink == new RgbaColor(16, 10, 10, 225));
        Assert.True(board.Y >= CommsBand.ReservedBottom - 1e-3,
            $"#986 F2 · the sentry's scoreboard starts at y={board.Y:0.#}, inside the band the ship's lines "
            + $"own (reserved down to y={CommsBand.ReservedBottom:0.#})");
    }
}
