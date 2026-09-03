using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #677 · THE WALLS PAST THE SEAM ARE A THIRD MATERIAL, and the absence of texture IS the style.
///
/// <para>Owner's ruling on the halls' senses, 2026-08-05: <i>"the pre-existing tunnels would be scary as dark
/// ones and totally different style … it is just built into the smooth monolith style walls."</i></para>
///
/// <para>The game has exactly two wall materials and both of them say who built the thing. <c>IsHull</c> is
/// poured, welded, bolted and paid for, and it takes the DEPARTMENT LIVERY as its ink (#605); <c>IsStone</c> is
/// the body's own rock, and it takes the MOON's colour (#589). Either one applied to a gallery would quietly
/// answer the question the whole feature exists to leave open — so the halls are drawn in neither, in one flat
/// constant that belongs to no palette at all.</para>
///
/// <para>Two halves, and both are needed: the PLAN has to flag it (Core decides which side of the seam a floor
/// is on; the renderer only asks), and the RENDERER has to honour the flag with an ink that is not the hull's
/// and not the stone's. This drives the real <see cref="DeckView"/> through a recording pen for the second
/// half, because a flag nobody draws is #708's own opening failure: <c>222 of 226 primitive(s) drawn in the
/// dark</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHallsAreDrawnInAThirdIdiomTests
{
    /// <summary>The rock the <c>?found=1</c> cheat parks — the one site in the system with galleries under it.
    /// Read from Core so this file and the cheat cannot come to disagree about which rock that is.</summary>
    private const string Halls = UndergroundComplex.FoundBandCheatSiteId;

    private const int WidthPx = 1280;
    private const int HeightPx = 560;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static IEnumerable<int> GalleriesOf(string body) =>
        UndergroundComplex.FloorsOf(body).Where(l => UndergroundComplex.IsFound(body, l));

    // ── The plan ─────────────────────────────────────────────────────────────────────────────────────────

    [Xunit.Fact]
    public void EVERYWallPastTheSeamIsSeamlessAndNoneAboveItIs()
    {
        var sb = new StringBuilder();
        int galleries = 0, facilityFloors = 0;

        foreach (string body in new[] { Halls, "luna", "miranda", "secret-lab-site-unlisted" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                DeckPlan deck = DeckFor(body, level);
                bool pastTheSeam = UndergroundComplex.IsFound(body, level);
                if (pastTheSeam) { galleries++; } else { facilityFloors++; }

                int wrong = deck.Walls.Count(w => w.IsSeamless != pastTheSeam);
                if (wrong > 0)
                {
                    sb.AppendLine($"  {body} B{-level}: {wrong} of {deck.Walls.Length} wall(s) drawn in the "
                        + (pastTheSeam ? "facility's material." : "halls' material."));
                }

                // …and the two channels are exclusive. A seamless wall is not ALSO hull or stone, because
                // both of those read a palette and the whole point is that this one reads none.
                int doubled = deck.Walls.Count(w => w.IsSeamless && (w.IsHull || w.IsStone || w.IsWindow));
                if (doubled > 0)
                {
                    sb.AppendLine($"  {body} B{-level}: {doubled} seamless wall(s) also claim a palette.");
                }
            }
        }

        Xunit.Assert.True(galleries >= 4, $"only {galleries} gallery floor(s) in the sweep.");
        Xunit.Assert.True(facilityFloors > 20, $"only {facilityFloors} facility floor(s) in the sweep.");
        Xunit.Assert.True(sb.Length == 0, $"the seam is drawn in the wrong place:{Environment.NewLine}{sb}");
    }

    [Xunit.Fact]
    public void AGalleryCarriesNoDOORLEAFAndNoSEALEDPLATE()
    {
        // Every door in this building is drawn IMPORTED violet — the one channel that means somebody flew a
        // thing here and fitted it (#592's material language) — and every sealed rib mouth carries a
        // stencilled distance, which is a survey and a department and a decision about where somebody's
        // authority stopped. A gallery has none of that: the wall simply stops, and the passage simply ends.
        var sb = new StringBuilder();
        foreach (int level in GalleriesOf(Halls))
        {
            DeckPlan deck = DeckFor(Halls, level);
            if (deck.Doors.Length > 0)
            {
                sb.AppendLine($"  B{-level}: {deck.Doors.Length} door leaf(s) in a gallery.");
            }
            int plates = deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.HiveSign);
            if (plates > 0)
            {
                sb.AppendLine($"  B{-level}: {plates} stencilled plate(s) in a gallery.");
            }
            if (deck.HullInk is not null)
            {
                sb.AppendLine($"  B{-level}: a department painted its livery on a gallery.");
            }

            // And there IS something to walk into — the emptiness is load-bearing, not literal.
            int rooms = deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.HiveHaul);
            if (rooms < 4)
            {
                sb.AppendLine($"  B{-level}: only {rooms} chamber(s) to search.");
            }
        }

        Xunit.Assert.True(sb.Length == 0, $"a gallery was furnished:{Environment.NewLine}{sb}");
    }

    // ── The renderer ─────────────────────────────────────────────────────────────────────────────────────

    private sealed record Mark(string Kind, float[] Points, RgbaColor Ink, float Width);

    private sealed class RecordingRenderer : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("circle", [x, y], fill ?? stroke, w));

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polyline", pts.ToArray(), stroke, w));

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polygon", pts.ToArray(), fill ?? stroke, w));

        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) =>
            Marks.Add(new Mark("text", [x, y], c, 0f));

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new Mark("image", [x, y], new RgbaColor(0, 0, 0), 0f));

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new Mark("slice", [x, y], new RgbaColor(0, 0, 0), 0f));
    }

    /// <summary>THE WHOLE FLOOR, drawn by the real <see cref="DeckView"/> with the lights ON so the dark is
    /// not what is being measured — walked past in overlapping frames rather than seen from one standpoint.
    ///
    /// <para>#563 · It used to be a single frame from where the car puts you, and the assertion below counted
    /// on the renderer drawing every wall it was handed whether or not that wall was on the glass. It culls
    /// now, so one frame of a gallery hall drew 11 strokes for 161 walls and this test was quietly measuring
    /// the VIEWPORT instead of the ink.</para>
    ///
    /// <para>The fix is not to widen the canvas — the view is a fixed 64 deck units across at any canvas size
    /// (<c>DeckView.PlacementFor</c>) — and it is certainly not to copy the cull's arithmetic in here, which
    /// would be a second opinion about what is visible. It is to LOOK AT ALL OF IT: stand at a grid of spots
    /// across the plan's own extent, keep every mark, and ask the union. The claim is stronger than it was —
    /// no wall anywhere down here is inked hull or stone, seen from anywhere — and it is answered by the
    /// renderer that ships.</para></summary>
    private static List<Mark> Frame(DeckPlan plan)
    {
        double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            minX = Math.Min(minX, Math.Min(w.X1, w.X2)); maxX = Math.Max(maxX, Math.Max(w.X1, w.X2));
            minY = Math.Min(minY, Math.Min(w.Y1, w.Y2)); maxY = Math.Max(maxY, Math.Max(w.Y1, w.Y2));
        }
        if (plan.Walls.Length == 0)
        {
            (minX, maxX, minY, maxY) = (0, 0, 0, 0);
        }

        // The view is 64 x 28 du; step well inside that so consecutive standpoints overlap and no wall can
        // fall between two frames.
        const double StepX = 30.0, StepY = 14.0;
        var marks = new List<Mark>();
        for (double ax = minX - StepX; ax <= maxX + StepX; ax += StepX)
        {
            for (double ay = minY - StepY; ay <= maxY + StepY; ay += StepY)
            {
                var pen = new RecordingRenderer();
                new DeckView(pen).Draw(plan, WidthPx, HeightPx, 0,
                    new DeckView.State(ax, ay, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false, Dark: false),
                    0, 0, null);
                marks.AddRange(pen.Marks);
            }
        }
        return marks;
    }

    /// <summary>The three wall inks, transcribed from <c>DeckView</c> — deliberately, because a test that
    /// asked the renderer for its own constants could not notice one of them changing into another.</summary>
    private static readonly RgbaColor Seamless = new(150, 150, 150);
    private static readonly RgbaColor Hull = new(170, 185, 205);
    private static readonly RgbaColor Stone = new(166, 150, 130);

    private static int Inked(IEnumerable<Mark> marks, RgbaColor ink) =>
        marks.Count(m => m.Kind == "polyline"
            && m.Ink.R == ink.R && m.Ink.G == ink.G && m.Ink.B == ink.B);

    [Xunit.Fact]
    public void THERENDERERHonoursItWithAnInkThatIsNeitherHULLNorSTONE()
    {
        int gallery = GalleriesOf(Halls).First();
        DeckPlan hallDeck = DeckFor(Halls, gallery);
        List<Mark> hall = Frame(hallDeck);

        // Past the seam the walls are all one flat constant, and not one of them is inked hull or stone.
        int smooth = Inked(hall, Seamless);
        Xunit.Assert.True(smooth >= hallDeck.Walls.Length,
            $"{smooth} seamless stroke(s) drawn for {hallDeck.Walls.Length} seamless wall(s).");
        Xunit.Assert.Equal(0, Inked(hall, Hull));
        Xunit.Assert.Equal(0, Inked(hall, Stone));

        // And ABOVE the seam nothing uses it — the ordinary floors are untouched, which is not a promise but
        // a guard. B1 of the same rock, which is a lobby full of poured concrete with a department's paint on
        // it: its walls take the LIVERY, which is the ink the halls must never be able to reach.
        DeckPlan lobby = DeckFor(Halls, -1);
        List<Mark> facility = Frame(lobby);
        Xunit.Assert.Equal(0, Inked(facility, Seamless));

        BodyPalette.Ink livery = lobby.HullInk ?? new BodyPalette.Ink(Hull.R, Hull.G, Hull.B);
        Xunit.Assert.True(
            Inked(facility, new RgbaColor(livery.R, livery.G, livery.B)) > 20,
            "the facility stopped being drawn as a made thing.");
    }

    [Xunit.Fact]
    public void AndTheLIVERYCannotReachThem()
    {
        // The load-bearing half of the third idiom: a palette is an ANSWER. The hull branch reads
        // DeckPlan.HullInk — the department that painted the corridor — so a gallery that took the hull
        // branch would have its walls quietly saying whose they were, in a channel #605 built for exactly
        // that purpose. Proved on the DECK rather than by reading the source: the plan carries no livery at
        // all down there, and the strokes are the constant even so.
        foreach (int level in GalleriesOf(Halls))
        {
            DeckPlan deck = DeckFor(Halls, level);
            Xunit.Assert.Null(deck.HullInk);
            Xunit.Assert.Null(deck.StoneInk);
            Xunit.Assert.Equal(0, Inked(Frame(deck), Hull));
            Xunit.Assert.True(Inked(Frame(deck), Seamless) > 20);
        }
    }
}
