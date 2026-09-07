using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #442 slice 1 · ONE WALL, ONE TRUTH — and the fifth consumer is the PEN.
///
/// <para>Owner, live 2026-07-26: <i>"See the invisible wall there now?"</i> … <i>"There should be a
/// refactor to make sure the visible and physics wall ALWAYS are 1 to 1 the same. Now they seem to be very
/// hacky."</i> And, on the shape of the guard: <i>"Maybe a test where you test that the walls as barrier
/// truly match one to one with physical barrier as part of CI testing… The wall there should be randomized
/// and tested with all three, character movement, reever movement and shot passing, etc, reever
/// visibility."</i> … <i>"And test those on multiple landing site so they really match the, the graphics
/// and barrier."</i></para>
///
/// <h3>What this file adds that <c>BarrierInvariantTests</c> could not</h3>
///
/// <para><see cref="SpaceSails.Core"/>'s own net (Core.Tests · <c>BarrierInvariantTests</c>) locks FOUR
/// readers together — the captain's boots, an Old One's shamble, a sentry's round and everybody's eye —
/// over seeded random fields. It says so at the top of itself that it deliberately leaves out the fifth:
/// <i>"Deliberately NOT covered here: the renderer … it lives in the client and cannot be reached from a
/// Core test."</i> That fifth reader is the one the owner actually walked into, and it is reachable from
/// HERE: the client test project can hand the real <see cref="DeckView"/> a recording pen and read back
/// every stroke it laid.</para>
///
/// <para>So this is the same property with the pen in the room:</para>
/// <list type="number">
/// <item><b>The net.</b> A seeded random wall field, drawn by the real renderer, with every wall
/// interrogated by all five consumers — and, crucially, the same field again under the #371 fog, which is
/// where the divergence lives.</item>
/// <item><b>The sweep.</b> Every seeded landing site of every landable body (#320's
/// <see cref="LandingSites"/> × <see cref="MoonSurface.SurfaceDeck"/>), site 0 included, asked the same
/// question — because a body is no longer one ground and a hand-check of the one he was standing on is
/// what let this ship in the first place.</item>
/// <item><b>The door.</b> A locked door's solidity has to come out of the same list the pen draws from,
/// not out of a second record kept in step by hand.</item>
/// <item><b>The gun.</b> The stone a shot is measured against and the stone the BEAM is drawn against must
/// be the same list, which is #437's defect stated as a law rather than as a fix.</item>
/// </list>
///
/// <para><b>Every failure names its seed, its body, its site and its segment</b>, and reports the whole
/// table rather than dying on the first one — "which grounds disagree" is the actual question.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed partial class OneWallOneTruthTests
{
    private const int WidthPx = 1200, HeightPx = 700;

    /// <summary>The captain's own body, and the Old Ones' (<c>Map.Surface.StepReevers</c> hands them the
    /// same radius) — one body size, one wall list.</summary>
    private const double Radius = DeckPlan.AvatarRadius;

    /// <summary>Half-width of a generated ground, in deck units. Small enough that the whole field lands on
    /// the glass under the tactical (non-FollowCam) frame, so "the pen did not draw it" can never be an
    /// artefact of the camera.</summary>
    private const double FieldHalf = 14.0;

    // ── THE PEN THAT REMEMBERS ────────────────────────────────────────────────────────────────────────

    /// <summary>One two-point stroke the renderer laid — the shape <c>DeckView.DrawSeg</c> emits for a
    /// wall. Anything with more points (a polygon, a longer polyline) is not a wall stroke and is not
    /// collected, so a filled rectangle whose edge happens to run along a wall cannot be mistaken for one.
    /// </summary>
    private sealed record Stroke(float X1, float Y1, float X2, float Y2, RgbaColor Ink, float Width);

    private sealed class WallPen : IRenderer
    {
        public List<Stroke> Strokes { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Strokes.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f)
        {
            if (pts.Length == 4)
            {
                Strokes.Add(new Stroke(pts[0], pts[1], pts[2], pts[3], stroke, w));
            }
        }

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) { }

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
    }

    /// <summary>Draw one real frame of a plan and hand back every two-point stroke on it, with the
    /// projection it was drawn under so a wall's world coordinates can be put on the same glass.</summary>
    private static (List<Stroke> Strokes, DeckView.Placement Place) Frame(
        DeckPlan plan, double ax, double ay, DeckView.SurfaceHud? hud = null)
    {
        var pen = new WallPen();
        var state = new DeckView.State(ax, ay, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false);
        new DeckView(pen).Draw(plan, WidthPx, HeightPx, simTime: 0, in state, surface: hud);
        return (pen.Strokes, DeckView.PlacementFor(plan, WidthPx, HeightPx, ax, ay, 0, 0));
    }

    private static (float X, float Y) On(DeckView.Placement p, double x, double y) =>
        (p.Ox + ((float)x * p.Scale), p.Oy - ((float)y * p.Scale));

    /// <summary>Did the pen lay a stroke for THIS wall? Matched on the projected endpoints, either way
    /// round, to within a pixel — the pen is handed <c>project(w.X1, w.Y1)</c> and <c>project(w.X2,
    /// w.Y2)</c> verbatim, so an exact-to-rounding match is the honest test and a near-miss is not a
    /// wall.</summary>
    private static bool Drawn(IEnumerable<Stroke> strokes, DeckView.Placement p, in DeckPlan.Wall w)
    {
        (float ax, float ay) = On(p, w.X1, w.Y1);
        (float bx, float by) = On(p, w.X2, w.Y2);
        foreach (Stroke s in strokes)
        {
            if ((Near(s.X1, ax) && Near(s.Y1, ay) && Near(s.X2, bx) && Near(s.Y2, by))
                || (Near(s.X1, bx) && Near(s.Y1, by) && Near(s.X2, ax) && Near(s.Y2, ay)))
            {
                return true;
            }
        }
        return false;
    }

    private static bool Near(float a, float b) => Math.Abs(a - b) <= 1.0f;

    /// <summary>Is any part of this wall on the glass? The same reject <c>DeckView.DrawTheWalls</c> makes
    /// (#563's off-the-glass <c>continue</c>), restated here so a wall the camera legitimately never
    /// reached is never counted as one the pen dropped.</summary>
    private static bool OnTheGlass(DeckView.Placement p, in DeckPlan.Wall w, float margin = 12f)
    {
        (float x1, float y1) = On(p, w.X1, w.Y1);
        (float x2, float y2) = On(p, w.X2, w.Y2);
        return !((x1 < -margin && x2 < -margin)
              || (x1 > WidthPx + margin && x2 > WidthPx + margin)
              || (y1 < -margin && y2 < -margin)
              || (y1 > HeightPx + margin && y2 > HeightPx + margin));
    }

    // ── THE OTHER FOUR CONSUMERS, ASKED ABOUT ONE WALL ────────────────────────────────────────────────

    /// <summary>Two points straddling a wall, a body's width out either side of its midpoint along its own
    /// normal — the pair every "is there a barrier here" reader is asked about.</summary>
    private static (double AX, double AY, double BX, double BY) Across(in DeckPlan.Wall w, double reach)
    {
        double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;
        double dx = w.X2 - w.X1, dy = w.Y2 - w.Y1;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 1e-9)
        {
            return (mx - reach, my, mx + reach, my);
        }
        double nx = -dy / len, ny = dx / len;
        return (mx + (nx * reach), my + (ny * reach), mx - (nx * reach), my - (ny * reach));
    }

    /// <summary>What the four Core readers say about the stone at this wall, asked the way each of them is
    /// actually asked in play. All four read ONE list — the plan's own <c>CollisionField</c>, which is
    /// <c>CollisionSegments</c> filed into a grid — which is the half of #442 that is already structural.
    /// </summary>
    private static (bool Boots, bool Shamble, bool Round, bool Eye) TheBodyAndTheGun(
        DeckPlan plan, in DeckPlan.Wall w)
    {
        IReadOnlyList<SurfaceCollision.Segment> stone = plan.CollisionField;
        double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;
        (double ax, double ay, double bx, double by) = Across(w, Radius * 1.6);

        // The CAPTAIN: standing on the wall is standing in stone, and it refuses.
        bool boots = SurfaceCollision.Blocked(mx, my, Radius, stone);

        // An OLD ONE: the same primitive with the doorway sidestep withheld (#724). Stood a hair off the
        // face and walked STRAIGHT AT it, one real frame's travel (MaxReeverStep — ReeverSpeed against the
        // client's dt clamp), it must still be on the side it started. Deliberately a short step from close
        // range rather than a long one from far off: a long step is allowed to slide ALONG the face and
        // round the END of a short slab, which is correct behaviour and would have this guard reporting the
        // sim's own law as a disagreement.
        double side = Radius + 0.05;
        (double fx, double fy) = Offset(w, mx, my, side);
        (double sx, double sy) = SurfaceCollision.Slide(
            fx, fy, (mx - fx) / side * MaxReeverStep, (my - fy) / side * MaxReeverStep,
            Radius, stone, SurfaceCollision.Gait.Stagger);
        bool shamble = Dot(w, sx - mx, sy - my) > 0;

        // A SENTRY'S ROUND and EVERYBODY'S EYE — the same primitive, which is why they must never drift.
        bool round = !SentryBot.CanEngage(ax, ay, bx, by, stone);
        bool eye = !SurfaceCollision.HasLineOfSight(ax, ay, bx, by, stone);
        return (boots, shamble, round, eye);
    }

    /// <summary>The longest step an Old One can take in one frame: <c>ReeverSpeed</c> 5.6 du/s against the
    /// client's <c>Math.Min(dt, 0.1)</c> clamp. Restated from <c>BarrierInvariantTests</c>, which explains
    /// why it matters that this is under the body's own diameter.</summary>
    private const double MaxReeverStep = 0.56;

    /// <summary>A point <paramref name="d"/> deck units off (<paramref name="mx"/>, <paramref name="my"/>)
    /// along the wall's own normal — the near side of the face.</summary>
    private static (double X, double Y) Offset(in DeckPlan.Wall w, double mx, double my, double d)
    {
        double dx = w.X2 - w.X1, dy = w.Y2 - w.Y1;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        return len < 1e-9 ? (mx + d, my) : (mx + (-dy / len * d), my + (dx / len * d));
    }

    /// <summary>How far a point is off a wall along the wall's own normal — the signed side it is on.</summary>
    private static double Dot(in DeckPlan.Wall w, double px, double py)
    {
        double dx = w.X2 - w.X1, dy = w.Y2 - w.Y1;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        return len < 1e-9 ? 0 : (px * (-dy / len)) + (py * (dx / len));
    }

    // ── (a) THE SEEDED RANDOM FIELD ───────────────────────────────────────────────────────────────────

    /// <summary>Build one randomized ground from a seed: slabs at dead-horizontal, dead-vertical, the 45°
    /// diagonal and arbitrary angles, hinged into corners and doubled into narrow corridors — the geometry
    /// <c>BarrierInvariantTests</c> generates for the Core readers, laid here as real
    /// <see cref="DeckPlan.Wall"/>s so the PEN can be asked about the same stone.
    ///
    /// <para>Long enough to be unmistakable (≥ 3 du — the game's own generators never seed shorter, which
    /// <c>DegenerateWallScan</c> pins) and inside <see cref="FieldHalf"/>, so every one of them lands on
    /// the tactical frame's glass and "not drawn" can only ever mean not drawn.</para></summary>
    private static DeckPlan.Wall[] WallField(int seed)
    {
        var rng = new Random(seed);
        var walls = new List<DeckPlan.Wall>();
        int slabs = 6 + rng.Next(7);
        for (int i = 0; i < slabs; i++)
        {
            double x = Span(rng), y = Span(rng);
            double length = 3.0 + (rng.NextDouble() * 8.0);
            double angle = rng.Next(4) switch
            {
                0 => 0,
                1 => Math.PI / 2,
                2 => Math.PI / 4,
                _ => rng.NextDouble() * Math.PI,
            };
            double ex = x + (Math.Cos(angle) * length), ey = y + (Math.Sin(angle) * length);
            bool hull = rng.Next(3) == 0;
            walls.Add(new DeckPlan.Wall((float)x, (float)y, (float)ex, (float)ey, false, hull));

            if (rng.Next(3) == 0)
            {
                // A hinged corner — the classic place a "solid" and a "drawn" part company by half a radius.
                double turn = angle + (rng.Next(2) == 0 ? Math.PI / 2 : -Math.PI / 2);
                double arm = 3.0 + (rng.NextDouble() * 5.0);
                walls.Add(new DeckPlan.Wall((float)ex, (float)ey,
                    (float)(ex + (Math.Cos(turn) * arm)), (float)(ey + (Math.Sin(turn) * arm)), false, hull));
            }
            if (rng.Next(4) == 0)
            {
                // A parallel twin a hair off — a corridor, sometimes narrower than a body (#435's stall).
                double off = 0.3 + (rng.NextDouble() * 2.2);
                double ox = -Math.Sin(angle) * off, oy = Math.Cos(angle) * off;
                walls.Add(new DeckPlan.Wall((float)(x + ox), (float)(y + oy),
                    (float)(ex + ox), (float)(ey + oy), false, false));
            }
        }
        return [.. walls];
    }

    private static double Span(Random rng) => ((rng.NextDouble() * 2) - 1) * FieldHalf;

    private static DeckPlan PlanOf(DeckPlan.Wall[] walls) =>
        new(walls, [], [], [], spawnX: 0, spawnY: 0, droidCount: 0,
            fillDroids: (_, _) => { }, location: (_, _) => "field");

    /// <summary>How many seeds a sweep runs. Forty fields is a few hundred slabs at every angle and is
    /// under a second — the whole point of the net being cheap is that it can live in the inner loop.
    /// </summary>
    private const int Seeds = 40;
}
