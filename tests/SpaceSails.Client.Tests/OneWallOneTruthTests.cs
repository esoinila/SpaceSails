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
public sealed class OneWallOneTruthTests
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

    // ══ THE NET ══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #442 · THE CONTROL: on open ground, every wall the body meets is a wall the eye is shown.
    ///
    /// <para>This one should be green on today's code and is here so the rest of the file cannot pass by
    /// examining nothing — if the pen, the projection or the field generator ever stops producing walls on
    /// the glass, this goes red before the interesting guards below get a chance to pass vacuously. It is
    /// the positive control the fog test is measured against.</para>
    /// </summary>
    [Fact]
    public void OnOpenGround_EveryWallTheBodyMeets_IsAWallTheEyeIsShown()
    {
        var bad = new List<string>();
        int checked_ = 0;

        for (int seed = 1; seed <= Seeds; seed++)
        {
            DeckPlan.Wall[] walls = WallField(seed);
            DeckPlan plan = PlanOf(walls);
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, 0, 0);

            foreach (DeckPlan.Wall w in plan.Walls)
            {
                if (!OnTheGlass(place, w))
                {
                    continue;
                }
                checked_++;
                (bool boots, bool shamble, bool round, bool eye) = TheBodyAndTheGun(plan, w);
                bool drawn = Drawn(strokes, place, w);
                if (boots && shamble && round && eye && drawn)
                {
                    continue;
                }
                bad.Add($"  seed {seed} · ({w.X1:0.##}, {w.Y1:0.##})→({w.X2:0.##}, {w.Y2:0.##}): "
                        + Dissent(boots, shamble, round, eye, drawn));
            }
        }

        Assert.True(checked_ > 200, $"the sweep only interrogated {checked_} walls — the field went quiet");
        Fail(bad, "wall(s) on open ground where the five consumers do not agree");
    }

    /// <summary>
    /// #442 · <b>THE FOG MAY CHANGE THE PAINT OR THE PHYSICS — IT MAY NOT CHANGE ONLY ONE.</b>
    ///
    /// <para>Owner, live: <i>"See the invisible wall there now?"</i> This is that wall, generated rather
    /// than hunted for. The #371 chamber overlay is laid over the middle of a seeded field in state 0
    /// (unseen); every wall inside it is then asked of all five consumers. The four Core readers say
    /// STONE — the collision list has never heard of the fog — and the pen is asked whether it drew
    /// anything.</para>
    ///
    /// <para><b>Both halves are asserted deliberately.</b> "The pen drew it" on its own would pass on a
    /// world with no stone in the region at all, which is the known local bug class — a guard handed a
    /// world that cannot tell pass from fail. So the test first insists the four bodies really are stopped
    /// there, and only then that the eye was shown what stopped them.</para>
    ///
    /// <para><b>Proven RED on today's code</b> — see the class summary and the PR body for the table it
    /// printed before the fog rule was made structural.</para>
    /// </summary>
    [Fact]
    public void UnderTheFog_AWallThatStillStopsYou_IsStillDrawn()
    {
        var bad = new List<string>();
        int inside = 0;

        for (int seed = 1; seed <= Seeds; seed++)
        {
            DeckPlan.Wall[] walls = WallField(seed);
            DeckPlan plan = PlanOf(walls);

            // One still-unseen chamber over the heart of the field (state 0 — "nobody has looked in here").
            var fog = new List<(double X0, double Y0, double X1, double Y1, int State)>
            {
                (-FieldHalf / 2, -FieldHalf / 2, FieldHalf / 2, FieldHalf / 2, 0),
            };
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, 0, 0, Hud(fog));

            foreach (DeckPlan.Wall w in plan.Walls)
            {
                double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;
                bool inFog = mx >= -FieldHalf / 2 && mx <= FieldHalf / 2
                          && my >= -FieldHalf / 2 && my <= FieldHalf / 2;
                if (!inFog || !OnTheGlass(place, w))
                {
                    continue;
                }
                inside++;

                (bool boots, bool shamble, bool round, bool eye) = TheBodyAndTheGun(plan, w);
                bool drawn = Drawn(strokes, place, w);
                if (boots && shamble && round && eye && drawn)
                {
                    continue;
                }
                bad.Add($"  seed {seed} · in the unseen chamber, ({w.X1:0.##}, {w.Y1:0.##})→"
                        + $"({w.X2:0.##}, {w.Y2:0.##}): " + Dissent(boots, shamble, round, eye, drawn));
            }
        }

        Assert.True(inside > 40,
            $"only {inside} wall(s) fell inside the unseen chamber — the fog test is not being handed a "
            + "world that can tell pass from fail");
        Fail(bad, "wall(s) that the fog hid from the eye and not from the body");
    }

    /// <summary>#442 · …and the same for a chamber that HAS been walked but is out of sight (state 1). It
    /// draws dim rather than not at all today, so this is a second control: the dim path already gets the
    /// rule right, which is what makes the state-0 path's <c>continue</c> a bug rather than a design.
    /// </summary>
    [Fact]
    public void InAnExploredChamber_TheWallsAreStillDrawn()
    {
        var bad = new List<string>();
        int inside = 0;
        for (int seed = 1; seed <= Seeds; seed++)
        {
            DeckPlan plan = PlanOf(WallField(seed));
            var fog = new List<(double X0, double Y0, double X1, double Y1, int State)>
            {
                (-FieldHalf / 2, -FieldHalf / 2, FieldHalf / 2, FieldHalf / 2, 1),
            };
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, 0, 0, Hud(fog));
            foreach (DeckPlan.Wall w in plan.Walls)
            {
                double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;
                if (mx < -FieldHalf / 2 || mx > FieldHalf / 2 || my < -FieldHalf / 2 || my > FieldHalf / 2
                    || !OnTheGlass(place, w))
                {
                    continue;
                }
                inside++;
                if (!Drawn(strokes, place, w))
                {
                    bad.Add($"  seed {seed} · ({w.X1:0.##}, {w.Y1:0.##})→({w.X2:0.##}, {w.Y2:0.##}) "
                            + "is solid in an explored chamber and the pen laid nothing.");
                }
            }
        }
        Assert.True(inside > 40, $"only {inside} wall(s) fell inside the explored chamber");
        Fail(bad, "wall(s) an explored chamber made solid and did not draw");
    }

    private static DeckView.SurfaceHud Hud(
        IReadOnlyList<(double X0, double Y0, double X1, double Y1, int State)> fog) =>
        new(DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            Blips: [], Cadence: 0, Readout: "", CacheMarks: [], Nerve: 100, NerveReadout: "",
            DarkRegions: fog);

    // ══ THE SWEEP — EVERY SITE OF EVERY BODY ═════════════════════════════════════════════════════════

    /// <summary>#585/#320 · Every landable body in the scenario. Mirrored from
    /// <see cref="EverySiteMeetsTheSpecTests"/>, which explains why it is a hand-kept list (the scenario is
    /// a wwwroot JSON the test host cannot reliably locate) and why a new moon must be added to it.
    /// </summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    private static DeckPlan DeckFor(string body, LandingSite site) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name);

    /// <summary>
    /// #442 · <b>THE TWO LISTS ARE ONE LIST, ON EVERY GROUND IN THE GAME.</b>
    ///
    /// <para><c>DeckPlan</c> derives <c>CollisionSegments</c> from <c>Walls</c> in its constructor and
    /// grows both together in <c>AppendRegion</c>, so the identity is structural — and this is the guard
    /// that keeps it structural, swept across every seeded site of every body rather than asserted once on
    /// a synthetic plan. Index-parallel and coordinate-identical: not "the same count", which a shuffle
    /// would pass.</para>
    /// </summary>
    [Fact]
    public void EverySiteOfEveryBody_CollidesWithExactlyTheWallsItLists()
    {
        var bad = new List<string>();
        int sites = 0, walls = 0;

        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan plan = DeckFor(body, site);
            sites++;
            if (plan.CollisionSegments.Length != plan.Walls.Length)
            {
                bad.Add($"  {body}/{site.Name}: {plan.Walls.Length} wall(s) and "
                        + $"{plan.CollisionSegments.Length} collision segment(s).");
                continue;
            }
            for (int i = 0; i < plan.Walls.Length; i++)
            {
                DeckPlan.Wall w = plan.Walls[i];
                SurfaceCollision.Segment s = plan.CollisionSegments[i];
                walls++;
                if (Math.Abs(s.X1 - w.X1) > 1e-9 || Math.Abs(s.Y1 - w.Y1) > 1e-9
                    || Math.Abs(s.X2 - w.X2) > 1e-9 || Math.Abs(s.Y2 - w.Y2) > 1e-9)
                {
                    bad.Add($"  {body}/{site.Name} wall {i}: drawn ({w.X1:0.##}, {w.Y1:0.##})→"
                            + $"({w.X2:0.##}, {w.Y2:0.##}) but collided ({s.X1:0.##}, {s.Y1:0.##})→"
                            + $"({s.X2:0.##}, {s.Y2:0.##}).");
                }
            }
        }

        Assert.True(sites >= 20, $"the sweep only saw {sites} site(s) — the site list went quiet");
        Assert.True(walls > 5000, $"the sweep only saw {walls} wall(s) — the grounds stopped generating");
        Fail(bad, "site(s) whose collision list is not its wall list");
    }

    /// <summary>
    /// #442 · <b>THE SWEEP: every site of every body draws every wall it makes solid.</b>
    ///
    /// <para>Owner: <i>"And test those on multiple landing site so they really match the, the graphics and
    /// barrier."</i> — with the site sweep raised to a hard criterion once #320 gave a body 2–4 grounds:
    /// <i>"Sweep every site of every landable body … Include site 0 explicitly."</i> Site 0 is the canon
    /// ground (Miranda's maze, Luna's mass-driver ruins) and is the first entry
    /// <see cref="LandingSites.For"/> hands back, so it is in here by construction.</para>
    ///
    /// <para>The one exemption is <see cref="DeckPlan.Wall.Unseen"/>, and it is exempt because it is
    /// <b>declared on the wall itself, in the same list</b> — the field's own envelope, which the owner
    /// asked never be advertised (<i>"if our space has limits for some technical reasons then let's not
    /// advertise it"</i>), and the interior hatching of a solid whose outline IS drawn (#649). That is the
    /// whole distinction #442 is about: a barrier may be invisible, but only by saying so in the one list —
    /// never because a second system decided not to paint it. <see cref="OnlyTheOneList_GetsToDeclareAWallInvisible"/>
    /// holds the exemption itself honest.</para>
    /// </summary>
    [Fact]
    public void EverySiteOfEveryBody_DrawsEveryWallItMakesSolid()
    {
        var bad = new List<string>();
        int sites = 0, onGlass = 0;

        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan plan = DeckFor(body, site);
            (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, plan.SpawnX, plan.SpawnY);
            sites++;
            int here = 0;

            for (int i = 0; i < plan.Walls.Length; i++)
            {
                DeckPlan.Wall w = plan.Walls[i];
                if (w.Unseen || !OnTheGlass(place, w))
                {
                    continue;
                }
                onGlass++;
                here++;
                if (Drawn(strokes, place, w))
                {
                    continue;
                }
                bad.Add($"  {body}/{site.Name} wall {i}: solid from ({w.X1:0.##}, {w.Y1:0.##}) to "
                        + $"({w.X2:0.##}, {w.Y2:0.##}), on the glass, and the pen laid nothing.");
            }

            if (here == 0)
            {
                bad.Add($"  {body}/{site.Name}: not one wall was on the glass — this ground was not audited.");
            }
        }

        Assert.True(sites >= 20, $"the sweep only saw {sites} site(s) — the site list went quiet");
        Assert.True(onGlass > 300, $"only {onGlass} wall(s) were on the glass across the whole sweep");
        Fail(bad, "wall(s) a landing site makes solid and never draws");
    }

    /// <summary>
    /// #442 · <b>THE EXEMPTION, HELD HONEST.</b> A wall may be invisible only by declaring it in the one
    /// list — and every such declaration on every real ground must be one of the two the game means:
    ///
    /// <list type="bullet">
    /// <item>the FIELD'S OWN ENVELOPE, which stops you at the rim of the world and is deliberately never
    /// painted (owner: <i>"let's not advertise it, more like hide that fact"</i>); or</item>
    /// <item>the INTERIOR HATCHING OF A SOLID — strokes inside a mass whose outline the eye is shown, so
    /// the picture already says "you cannot be here" before the hatch ever stops anybody (#649).</item>
    /// </list>
    ///
    /// <para>A third kind would be an invisible wall in open ground, which is the bug. The test states it
    /// positively: an unseen wall's midpoint must lie either outside the field's walkable envelope or
    /// inside a drawn <see cref="DeckPlan.Structure"/> — the filled mass the pen paints.</para>
    /// </summary>
    [Fact]
    public void OnlyTheOneList_GetsToDeclareAWallInvisible()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        var bad = new List<string>();
        int unseen = 0;

        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan plan = DeckFor(body, site);
            foreach (DeckPlan.Wall w in plan.Walls)
            {
                if (!w.Unseen)
                {
                    continue;
                }
                unseen++;
                double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;

                // The rim of the world: at or beyond the field's own bound, in any direction.
                bool atTheRim = mx <= field.LeftX + 1 || mx >= field.RightX - 1
                             || my >= field.TopY - 1 || my <= field.BottomY + 1;

                // …or inside something the eye is shown as a filled mass.
                bool insideADrawnSolid = plan.Structures.Any(s =>
                    mx >= Math.Min(s.X0, s.X1) - 0.5 && mx <= Math.Max(s.X0, s.X1) + 0.5
                    && my >= Math.Min(s.Y0, s.Y1) - 0.5 && my <= Math.Max(s.Y0, s.Y1) + 0.5);

                if (!atTheRim && !insideADrawnSolid)
                {
                    bad.Add($"  {body}/{site.Name}: an UNSEEN wall at ({mx:0.##}, {my:0.##}) that is "
                            + "neither the field's rim nor inside a drawn solid — an invisible wall in the "
                            + "open.");
                }
            }
        }

        Assert.True(unseen > 0,
            "no ground declared a single invisible wall — either the sweep is empty or the idiom is gone "
            + "and this guard is now asserting nothing");
        Fail(bad, "invisible wall(s) that no drawn thing accounts for");
    }

    // ══ THE DOOR ═════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #442 · <b>A LOCKED DOOR IS A WALL WITH A DOOR'S LOOK.</b>
    ///
    /// <para>The issue names the idiom by its own comment: a locked door is <i>"decoration only, and is
    /// backed by a real wall so you can't pass"</i> — two records for one barrier, kept in step by hand,
    /// which is precisely the parallel construct that drifts. This pins the pairing so a drift goes red:
    /// every locked leaf on every deck that has one must lie ON a collision segment out of the SAME list
    /// the pen draws from. A locked door whose backing wall moved, or was never laid, fails here.</para>
    ///
    /// <para>The plan's own answer about the doorway (<see cref="DeckPlan.DoorwayIsWalledUp"/>) is asserted
    /// against the walls themselves rather than trusted, so the derivation cannot quietly start answering a
    /// different question than the one the pen is now asking it.</para>
    /// </summary>
    [Fact]
    public void ALockedDoorIsBackedByTheSameListThePenDrawsFrom()
    {
        var bad = new List<string>();
        int locked = 0, open = 0;

        foreach (string scene in Scenes.Names())
        {
            DeckPlan plan;
            try
            {
                plan = Scenes.Build(scene);
            }
            catch (Exception)
            {
                continue; // a scene this harness cannot stand up is not this guard's business
            }

            foreach (DeckPlan.Door d in plan.Doors)
            {
                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                bool stone = plan.Walls.Any(w => !w.Unseen && OnTheSameLine(w, d));
                _ = d.Locked ? locked++ : open++;

                if (d.Locked && !stone)
                {
                    bad.Add($"  {scene}: a LOCKED door at ({mx:0.##}, {my:0.##}) with no wall behind it — "
                            + "it reads as a barrier and is not one.");
                }

                // …and the plan's own derived answer is the walls' answer, on every door in the game.
                if (plan.DoorwayIsWalledUp(d) != stone)
                {
                    bad.Add($"  {scene}: the door at ({mx:0.##}, {my:0.##}) is "
                            + (stone ? "walled up" : "an open doorway")
                            + $" and the plan says {(plan.DoorwayIsWalledUp(d) ? "walled up" : "open")} — "
                            + "the derivation the pen reads has parted company with the list.");
                }
            }
        }

        Assert.True(locked > 0, "no scene carries a locked door — this guard is asserting nothing");
        Assert.True(open > 0, "no scene carries an ordinary door — this guard is asserting nothing");
        Fail(bad, "door(s) whose look and whose stone disagree");
    }

    /// <summary>
    /// #442 · <b>A LEAF WITH A WALL ACROSS IT IS NEVER DRAWN SLIDING OPEN.</b>
    ///
    /// <para>The second direction of the owner's report, and the one the net found rather than the one it
    /// was written for: three constructs in the game are a wall PLUS an unlocked door laid over it — the
    /// ship's own shuttle hatch while she is docked ("the hatch itself — sealed here"), every dogged
    /// compartment hatch (<c>ShipWith</c>: <i>"a dogged hatch is a WALL, and the walls are what everything
    /// else asks"</i>), and a haven's sealed berth hatch. All three drew as ORDINARY automatic doors, which
    /// means they retracted as the captain walked up — the player watched the opening open and then walked
    /// into stone.</para>
    ///
    /// <para>The captain is stood <b>right at</b> each doorway, inside <c>DoorOpenRadius</c>, which is the
    /// only place the bug exists: further off, every leaf is drawn shut anyway and the guard would pass on a
    /// world that cannot tell pass from fail.</para>
    ///
    /// <para><b>What the pen is asked is the RETRACTED STUB, and the first cut of this guard asked the
    /// wrong thing.</b> A retracted leaf is two short strokes reaching a quarter of the way in from each
    /// jamb; a shut one is a single full-span stroke. Asking for the full span passed on the broken
    /// renderer, because the WALL behind a sealed hatch is drawn at exactly the same two endpoints — the
    /// guard was reading the stone and calling it the door. The stub belongs to nothing else on the deck,
    /// so that is what is looked for, and taking the fix back out now puts 18 doorways on the report.</para>
    /// </summary>
    [Fact]
    public void ASealedDoorwayIsDrawnShut_EvenWithTheCaptainStandingAtIt()
    {
        var bad = new List<string>();
        int sealedLeaves = 0, ordinary = 0;

        foreach (string scene in Scenes.Names())
        {
            DeckPlan plan;
            try
            {
                plan = Scenes.Build(scene);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (DeckPlan.Door d in plan.Doors)
            {
                if (d.Locked)
                {
                    continue; // always drawn shut and cold; the guard above owns that one
                }
                double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
                bool walled = plan.DoorwayIsWalledUp(d);

                // Standing ON the leaf: as open as the interlock will ever let this one be.
                (List<Stroke> strokes, DeckView.Placement place) = Frame(plan, mx, my);
                bool retracted = Stub(strokes, place, d);

                if (walled)
                {
                    sealedLeaves++;
                    if (retracted)
                    {
                        bad.Add($"  {scene}: the doorway at ({mx:0.##}, {my:0.##}) has a wall across it and "
                                + "the pen slid the leaf ASIDE — an opening you cannot walk through.");
                    }
                }
                else if (d.Interlock == 0)
                {
                    // No partner to take turns with (#462), so standing in it is the whole of the rule and
                    // this one MUST be open. The interlocked pairs are left out rather than guessed at: the
                    // far end of a tube is drawn shut on purpose and is not this guard's business.
                    ordinary++;
                    if (!retracted)
                    {
                        bad.Add($"  {scene}: the ordinary doorway at ({mx:0.##}, {my:0.##}) is walkable and "
                                + "the pen drew the leaf SHUT with the captain standing in it.");
                    }
                }
            }
        }

        Assert.True(sealedLeaves > 0,
            "not one sealed doorway in the whole scene list — this guard is being handed a world that "
            + "cannot tell pass from fail");
        Assert.True(ordinary > 0, "not one ordinary doorway — the retract path is untested");
        Fail(bad, "doorway(s) the pen drew as the opposite of what the walls say");
    }

    /// <summary>Did the pen draw this door RETRACTED — the short stub reaching a quarter of the way in from
    /// a jamb that <c>DeckView.DrawTheDoors</c> lays for an open leaf, and that nothing else on a deck
    /// draws? Asked at both jambs, either of which is proof the leaf slid aside.</summary>
    private static bool Stub(IEnumerable<Stroke> strokes, DeckView.Placement p, in DeckPlan.Door d)
    {
        (float ax, float ay) = On(p, d.X1, d.Y1);
        (float bx, float by) = On(p, d.X2, d.Y2);
        (float qax, float qay) = On(p, d.X1 + ((d.X2 - d.X1) * 0.25f), d.Y1 + ((d.Y2 - d.Y1) * 0.25f));
        (float qbx, float qby) = On(p, d.X2 - ((d.X2 - d.X1) * 0.25f), d.Y2 - ((d.Y2 - d.Y1) * 0.25f));
        return strokes.Any(s =>
            (Near(s.X1, ax) && Near(s.Y1, ay) && Near(s.X2, qax) && Near(s.Y2, qay))
            || (Near(s.X1, bx) && Near(s.Y1, by) && Near(s.X2, qbx) && Near(s.Y2, qby)));
    }

    /// <summary>Does this wall lie along this door — same line, covering its span? A locked door is drawn
    /// as the middle stretch of the wall behind it, so "backed" means the wall covers the door's two ends
    /// AND its MIDDLE.
    ///
    /// <para>The middle is the load-bearing third of the test. A CARVED doorway leaves two stubs whose inner
    /// ends touch the door's ends exactly (<c>DeckExpansions.CarveDoorway</c>), so an end-only test calls
    /// every ordinary auto-door on the ship "walled up" — the gap between the stubs is the whole point of a
    /// doorway and it is precisely what a stub does not cover.</para></summary>
    private static bool OnTheSameLine(in DeckPlan.Wall w, in DeckPlan.Door d) =>
        SurfaceCollision.DistanceToSegment(d.X1, d.Y1, w.X1, w.Y1, w.X2, w.Y2) < 0.15
        && SurfaceCollision.DistanceToSegment(d.X2, d.Y2, w.X1, w.Y1, w.X2, w.Y2) < 0.15
        && SurfaceCollision.DistanceToSegment(
            (d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0, w.X1, w.Y1, w.X2, w.Y2) < 0.15;

    // ══ THE GUN ══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #442 / #437 · <b>THE STONE A SHOT IS MEASURED AGAINST AND THE STONE ITS BEAM IS DRAWN AGAINST ARE
    /// ONE LIST.</b>
    ///
    /// <para>The surface page keeps two wall lists on purpose and they are not interchangeable:
    /// <c>_deckPlan.CollisionField</c> is the stone (what stops a boot) and <c>SightBlockers()</c> is the
    /// stone PLUS whatever doors are shut this instant (what stops an eye and a round — #465, because
    /// <i>"the gun would be behind one door and not shooting through it"</i>). Every reader that decides
    /// what a sentry can HIT must be handed the second one; the page's own comment beside the beam says so
    /// (<i>"Same CanEngage gate as the volley, so the beam can only ever be drawn at the target the volley
    /// could actually have spent its round on"</i>).</para>
    ///
    /// <para>This reads the page and insists every <c>SentryBot</c> sight call is handed
    /// <c>SightBlockers()</c>. Source-level on purpose: the alternative is standing a whole Blazor page up
    /// to catch a one-argument slip, and the slip is exactly the kind a reader can see and a runtime test
    /// only reaches on the one frame a Reever happens to be behind a shut door.</para>
    ///
    /// <para><b>Proven RED on today's code:</b> <c>NearestReeverInArc</c> — the method that decides where
    /// the firing beam is PAINTED — passed <c>_deckPlan.CollisionField</c> while the volley beside it
    /// passed <c>SightBlockers()</c>, so the picture and the round disagreed about a shut door.</para>
    /// </summary>
    [Fact]
    public void EverySentrySightCall_IsHandedTheStoneThatIncludesShutDoors()
    {
        string page = ClientSource("Pages", "Map.Surface.Reevers.cs");
        var bad = new List<string>();
        int calls = 0;

        foreach (string method in new[] { "SentryBot.CanEngage(", "SentryBot.Step(" })
        {
            int at = 0;
            while ((at = page.IndexOf(method, at, StringComparison.Ordinal)) >= 0)
            {
                int close = MatchingParen(page, at + method.Length - 1);
                string args = close < 0 ? page[at..] : page[(at + method.Length)..close];
                calls++;
                if (!args.Contains("SightBlockers()", StringComparison.Ordinal))
                {
                    int line = page.Take(at).Count(c => c == '\n') + 1;
                    bad.Add($"  Map.Surface.Reevers.cs:{line} · {method}…) is handed "
                            + $"`{args.Split(',').Last().Trim()}` — not SightBlockers(). A shut door stops "
                            + "the round and this reader has never heard of it.");
                }
                at = close < 0 ? at + method.Length : close;
            }
        }

        Assert.True(calls >= 3,
            $"only {calls} sentry sight call(s) found — the page was renamed or the guard is reading the "
            + "wrong text");
        Fail(bad, "sentry sight call(s) handed the wrong wall list");
    }

    /// <summary>One of the client's own source files, read off disk — the same trick
    /// <c>SeatsAreDrawnTests</c> uses to hold a claim about the pen that no runtime call can reach.</summary>
    private static string ClientSource(params string[] parts)
    {
        System.IO.DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string src = System.IO.Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (System.IO.Directory.Exists(src))
            {
                return System.IO.File.ReadAllText(System.IO.Path.Combine([src, .. parts]));
            }
            at = at.Parent;
        }
        throw new System.IO.DirectoryNotFoundException(
            $"could not find the repo root above {AppContext.BaseDirectory}");
    }

    /// <summary>Index of the ')' that closes the '(' at <paramref name="open"/>, or -1.</summary>
    private static int MatchingParen(string s, int open)
    {
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            if (s[i] == '(')
            {
                depth++;
            }
            else if (s[i] == ')' && --depth == 0)
            {
                return i;
            }
        }
        return -1;
    }

    // ── REPORTING ─────────────────────────────────────────────────────────────────────────────────────

    private static string Dissent(bool boots, bool shamble, bool round, bool eye, bool drawn)
    {
        var said = new List<string>();
        said.Add(boots ? "the boot is stopped" : "THE BOOT WALKS THROUGH");
        said.Add(shamble ? "the shamble is stopped" : "THE OLD ONE WALKS THROUGH");
        said.Add(round ? "the round is stopped" : "THE ROUND PASSES");
        said.Add(eye ? "the eye is broken" : "THE EYE SEES THROUGH");
        said.Add(drawn ? "the pen drew it" : "THE PEN DREW NOTHING");
        return string.Join(", ", said) + ".";
    }

    private static void Fail(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.Take(40))
        {
            sb.AppendLine(line);
        }
        if (bad.Count > 40)
        {
            sb.AppendLine($"  …and {bad.Count - 40} more.");
        }
        Assert.Fail(sb.ToString());
    }
}
