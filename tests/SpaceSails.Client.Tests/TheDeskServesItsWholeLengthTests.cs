using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #791 · THE DESK SERVES ITS WHOLE LENGTH — the client half: the [E] bus, and the mark that advertises it.
///
/// <para>Owner, live playtest 2026-08-08: <i>"The Bar desk is really long now, but there is only one spot to
/// get service on it… we would need an E-bus of the bar desk length instead of one bar keep cashier at a
/// single spot… we should probably have service on the whole length indicated somehow."</i></para>
///
/// <h3>What was wrong, measured</h3>
///
/// <para>The B1 bar's serving desk is <b>81.9 deck units</b> long. The console that answered [E] was a
/// POINT, and <see cref="DeckPlan.InteractRadius"/> is 3 — so <b>six du of it</b> took an order, in the
/// middle, with nothing on the floor saying which six. Walking the desk end to end, the press answered on
/// 7% of it and refused on the rest, at a fixture whose photograph, whose collidable wall and whose eight
/// stools all run the full length.</para>
///
/// <para>Everything below is measured on the REAL deck the REAL renderer draws, and every claim about what
/// is drawn is checked against the very segment the key measures — the split this deck has paid for more
/// often than any other.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDeskServesItsWholeLengthTests
{
    private const int WidthPx = 1200, HeightPx = 700;
    private const string Body = "luna";
    private const int Level = -1;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan Deck(long watch = 0) =>
        HiveInterior.FloorDeck(Body, Level, Field, 0, (_, _) => { }, [], watch);

    private static UndergroundComplex.Amenity Cantina()
    {
        foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(Body, Level, Field).Amenities)
        {
            if (a.Use == UndergroundComplex.Comfort.UpperCanteen && a.Hall is { Service: not null })
            {
                return a;
            }
        }
        throw new InvalidOperationException($"{Body} B{-Level} has no serving counter to measure.");
    }

    private static UndergroundComplex.ServiceRun Run() => Cantina().Hall!.Value.Service!.Value;

    /// <summary>The one console the counter is. Found by KIND and by the box it stands in, never by index:
    /// a guard that trusted array order would pass a deck that had grown a second bar.</summary>
    private static DeckPlan.ConsoleSpot Bus(DeckPlan plan)
    {
        UndergroundComplex.Hall hall = Cantina().Hall!.Value;
        List<DeckPlan.ConsoleSpot> found = plan.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.HiveAmenity && hall.Contains(c.X, c.Y))
            .ToList();
        Assert.Single(found);
        return found[0];
    }

    // ── (a) THE BUS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #791 · [E] ANSWERS FROM EVERY POINT ALONG THE DESK.
    ///
    /// <para>The whole run is walked at half-metre steps, standing where a customer stands, and at every one
    /// of them the nearest console within reach must be THE COUNTER. Then the same walk is made a du, two du
    /// and three du out into the room, because "the desk-front is the interaction zone" is a claim about a
    /// BAND and not about a line.</para>
    ///
    /// <para><b>Proven RED</b> by the fixture exactly as it shipped — one point console, no span:</para>
    /// <code>
    /// the counter answers on 13 of 165 places along its own desk (7.9%):
    ///   at 0.0 du along the desk: nothing in reach.
    ///   at 0.5 du along the desk: nothing in reach.
    ///   …
    ///   at 41.0 du along the desk: THE COUNTER.   ← the six du in the middle
    /// </code>
    /// </summary>
    [Fact]
    public void E_FiresFromEveryPointAlongTheDesk()
    {
        DeckPlan plan = Deck();
        UndergroundComplex.ServiceRun run = Run();
        DeckPlan.ConsoleSpot bus = Bus(plan);

        double dx = run.X1 - run.X0, dy = run.Y1 - run.Y0;
        double len = run.LengthDu;
        double ux = dx / len, uy = dy / len;
        double nx = -uy, ny = ux;

        // Which way the room is: the hall's own middle, off the run. The desk is on the other side, so a
        // step "out" is a step toward the tables.
        UndergroundComplex.Hall hall = Cantina().Hall!.Value;
        double hallMidY = (hall.Y0 + hall.Y1) / 2.0, hallMidX = (hall.X0 + hall.X1) / 2.0;
        double toRoom = Math.Sign(((hallMidX - run.MidX) * nx) + ((hallMidY - run.MidY) * ny));

        var missed = new List<string>();
        int answered = 0, places = 0;

        foreach (double out_ in new[] { 0.0, 1.0, 2.0, 2.8 })
        {
            for (double s = 0; s <= len + 1e-9; s += 0.5)
            {
                double px = run.X0 + (ux * s) + (nx * toRoom * out_);
                double py = run.Y0 + (uy * s) + (ny * toRoom * out_);
                places++;

                DeckPlan.ConsoleSpot? at = plan.NearestConsoleSpot(px, py);
                if (at is { Kind: DeckPlan.ConsoleKind.HiveAmenity } spot && spot.X == bus.X && spot.Y == bus.Y)
                {
                    answered++;
                    continue;
                }
                missed.Add($"  at {s:F1} du along the desk, {out_:F1} du out: "
                    + (at is null ? "nothing in reach." : $"{at.Value.Label}."));
            }
        }

        Assert.True(missed.Count == 0,
            $"the counter answers on {answered} of {places} places along its own desk "
            + $"({100.0 * answered / places:F1}%):\n" + string.Join("\n", missed.Take(12)));

        // ANTI-VACUITY: this must be a BUS and not a wide dot. A desk that were six du long would pass
        // every line above while being the very fixture the owner filed the issue against.
        Assert.True(len >= 40.0, $"the desk under test is only {len:F1} du long.");
        Assert.True(places >= 300, $"only {places} place(s) walked.");
    }

    /// <summary>
    /// #791 · …AND IT IS STILL ONE FIXTURE. One console, one plate, one card — the alternative (a row of
    /// consoles bolted along the bar) would put a dozen [E] targets in a room already dotted with table
    /// consoles, eleven of which would open the same card.
    ///
    /// <para>The anchor is also asserted to be the amenity's own published spot, because
    /// <c>Map.Surface.HiveAmenityInteract</c> identifies WHICH room was pressed by matching the spot against
    /// <c>Amenity.X/Y</c> within half a du. A bus whose anchor drifted would be a card that never opens.</para>
    /// </summary>
    [Fact]
    public void ONE_FixtureOneCard_AndTheAnchorIsTheAmenitysOwnSpot()
    {
        DeckPlan plan = Deck();
        UndergroundComplex.Amenity a = Cantina();
        DeckPlan.ConsoleSpot bus = Bus(plan);

        Assert.True(Math.Abs(bus.X - a.X) < 0.5 && Math.Abs(bus.Y - a.Y) < 0.5,
            $"the bus is anchored at ({bus.X:F2},{bus.Y:F2}) and the amenity at ({a.X:F2},{a.Y:F2}) — "
            + "HiveAmenityInteract matches those two within half a du, so the card would never open.");

        Assert.True(bus.IsRun, "the counter is a point again.");
        Assert.Equal(a.Fixture, bus.Label);

        // And the run it carries IS the run Core carved — both ends, to the deck unit.
        UndergroundComplex.ServiceRun run = Run();
        Assert.Equal(run.X0, bus.End0.X, 3);
        Assert.Equal(run.Y0, bus.End0.Y, 3);
        Assert.Equal(run.X1, bus.End1.X, 3);
        Assert.Equal(run.Y1, bus.End1.Y, 3);
    }

    /// <summary>
    /// #791 · EVERY OTHER CONSOLE IN THE GAME IS STILL A POINT, and reaches exactly as far as it did.
    ///
    /// <para>A helm is a chair and a valve is a wheel. The span defaults to zero, and a segment of zero
    /// length is its own endpoint — so <see cref="DeckPlan.ConsoleSpot.DistanceFrom"/> must return the plain
    /// euclidean distance for all of them, which is the number the old code computed. This is the guard that
    /// says the shared primitive was widened rather than changed.</para>
    /// </summary>
    [Fact]
    public void EVERY_OtherConsoleIsStillAPointAndReachesExactlyAsFar()
    {
        var decks = new List<DeckPlan> { Deck(), DeckPlan.Ship };
        int points = 0, runs = 0;

        foreach (DeckPlan plan in decks)
        {
            foreach (DeckPlan.ConsoleSpot c in plan.Consoles)
            {
                if (c.IsRun)
                {
                    runs++;

                    // #821 · A SECOND FIXTURE THAT IS A LENGTH RATHER THAN A DOT. The public washroom's
                    // basin run borrows this very primitive for the bar desk's own reason: four taps in a
                    // row all opening one beat would be three pieces of furniture pretending to be a choice.
                    // The list is the whole point of the assertion — a run is a deliberate, named thing and
                    // never something a console grows by accident.
                    //
                    // #1040 · A THIRD, and it is the ship's own. Her cantina grew the counter the haven bars
                    // have had since #247, and a counter is one fixture you walk up to anywhere along: three
                    // stool consoles a stool apart would fail the deck audit's own label law and would read
                    // as three pieces of furniture. The list is still the whole point of the assertion —
                    // this entry is a deliberate addition made in the same commit as the run itself.
                    Assert.Contains(c.Kind,
                        new[]
                        {
                            DeckPlan.ConsoleKind.HiveAmenity, DeckPlan.ConsoleKind.HiveBasin,
                            DeckPlan.ConsoleKind.ShipStool,
                        });
                    continue;
                }

                points++;
                foreach ((double px, double py) in new[]
                {
                    (c.X + 1.0, c.Y), (c.X - 2.5, c.Y + 4.0), (c.X, c.Y - 7.0), (c.X + 30.0, c.Y + 30.0),
                })
                {
                    double euclid = Math.Sqrt(((px - c.X) * (px - c.X)) + ((py - c.Y) * (py - c.Y)));
                    Assert.Equal(euclid, c.DistanceFrom(px, py), 6);
                    Assert.Equal((c.X, c.Y), c.NearestPointTo(px, py));
                }
            }
        }

        Assert.True(points >= 40, $"only {points} point console(s) checked.");
        Assert.True(runs >= 1, "no run console anywhere — the fixture under test is missing.");
    }

    // ── (b) AND NOT FROM BEHIND IT ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #791 · THE SERVICE SIDE IS NOT THE CAPTAIN'S SIDE. The collidable counter walls stay the law: the
    /// band behind the desk — where the keep stands, and where the goods hoist is parked — cannot be walked
    /// into from anywhere in the room, so the bus cannot be pressed from inside the bar.
    ///
    /// <para>A flood fill over the hall's own box on a half-du grid, started where <c>?counter=1</c> stands a
    /// tester and stepped only where <see cref="DeckPlan.Collides"/> allows a body. Not one reached cell may
    /// lie inside the desk's own photograph — which is the published box of the sealed band.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the counter's long wall segment from the carve: the fill pours
    /// straight through the bar and reports <c>1 8 4 2 cell(s) reached behind the desk</c>.</para>
    /// </summary>
    [Fact]
    public void E_DoesNotFireFromBehindOrInsideTheCounter()
    {
        DeckPlan plan = Deck();
        UndergroundComplex.Amenity a = Cantina();
        UndergroundComplex.Hall hall = a.Hall!.Value;
        UndergroundComplex.SpotArt desk = hall.Painted
            .Single(s => s.Url == UndergroundComplex.CounterArtUrl);

        const double Step = 0.5;
        int nx = (int)((hall.X1 - hall.X0) / Step) + 1;
        int ny = (int)((hall.Y1 - hall.Y0) / Step) + 1;
        var seen = new bool[nx, ny];

        (int Cx, int Cy) Cell(double x, double y) =>
            ((int)Math.Round((x - hall.X0) / Step), (int)Math.Round((y - hall.Y0) / Step));

        // #827 · Started on the counter's own STANDING SQUARE. The amenity's spot is the desk's face now —
        // the console dot is drawn on the counter, which is the whole of that issue — so a flood started on
        // it would start inside the very wall this guard is about.
        UndergroundComplex.ServiceRun start = hall.Service!.Value;
        (int sx, int sy) = Cell(start.StandX, start.StandY);
        Assert.False(plan.Collides(start.StandX, start.StandY),
            "the square a customer stands on is inside a wall.");

        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((sx, sy));
        seen[sx, sy] = true;                     // seen == REACHED here: nothing blocked is ever enqueued

        int reached = 0, behind = 0;
        var behindWhere = new List<string>();
        while (queue.Count > 0)
        {
            (int cx, int cy) = queue.Dequeue();
            reached++;
            double wx = hall.X0 + (cx * Step), wy = hall.Y0 + (cy * Step);
            if (wx >= desk.X0 && wx <= desk.X1 && wy >= desk.Y0 && wy <= desk.Y1)
            {
                behind++;
                if (behindWhere.Count < 6)
                {
                    behindWhere.Add($"  ({wx:F1},{wy:F1}) is behind the desk and a body got to it.");
                }
            }

            foreach ((int ox, int oy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int tx = cx + ox, ty = cy + oy;
                if (tx < 0 || ty < 0 || tx >= nx || ty >= ny || seen[tx, ty])
                {
                    continue;
                }
                if (plan.Collides(hall.X0 + (tx * Step), hall.Y0 + (ty * Step)))
                {
                    continue;   // a wall. Left UNSEEN so no later approach mistakes it for reached ground.
                }
                seen[tx, ty] = true;
                queue.Enqueue((tx, ty));
            }
        }

        Assert.True(behind == 0,
            $"{behind} cell(s) reached behind the desk — the counter's wall has a hole in it, and the [E] "
            + $"bus can be pressed from the keep's side:\n{string.Join("\n", behindWhere)}");

        // ANTI-VACUITY, and it is the whole reason this fill is worth running: the room in FRONT of the desk
        // must be wide open, or a guard that found nothing behind the bar found nothing anywhere.
        Assert.True(reached > 2000,
            $"only {reached} cell(s) of the hall are walkable at all — the fill never left the counter.");

        // …and the keep's own spot is inside that band, and is one of the cells nothing could reach.
        UndergroundComplex.ServiceRun run = Run();
        (int kx, int ky) = Cell(run.KeepX, run.KeepY);
        Assert.False(seen[kx, ky], "a body walked to where the keep stands.");
        Assert.True(run.KeepX >= desk.X0 && run.KeepX <= desk.X1
            && run.KeepY >= desk.Y0 && run.KeepY <= desk.Y1,
            "the keep does not stand behind his own desk.");
    }

    // ── (c) WHAT IS DRAWN IS WHAT ANSWERS ────────────────────────────────────────────────────────────

    private sealed record Mark(string Kind, float[] Points, RgbaColor Ink, float Width);

    private sealed class RecordingRenderer : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark(fill is null ? "ring" : "disc", [x, y, r], fill ?? stroke, w));

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polyline", pts.ToArray(), stroke, w));

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polygon", pts.ToArray(), fill ?? stroke, w));

        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) =>
            Marks.Add(new Mark($"text:{text}", [x, y], c, 0f));

        public void DrawImage(int id, float x, float y, float w, float h, float alpha = 1f) =>
            Marks.Add(new Mark("image", [x, y], new RgbaColor(0, 0, 0), 0f));

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float alpha = 1f) =>
            Marks.Add(new Mark("slice", [x, y], new RgbaColor(0, 0, 0), 0f));
    }

    /// <summary>The console inks, transcribed rather than borrowed — a guard that asked the renderer for its
    /// own constant could not notice one turning into another (#792's note).</summary>
    private static readonly RgbaColor ConsoleGlow = new(120, 220, 200);
    private static readonly RgbaColor ConsoleNear = new(190, 255, 220);

    private static bool Is(RgbaColor a, RgbaColor b) => a.R == b.R && a.G == b.G && a.B == b.B;

    private static (List<Mark> Marks, DeckView.Placement Place) Frame(double atX, double atY)
    {
        DeckPlan plan = Deck();
        var pen = new RecordingRenderer();
        new DeckView(pen).Draw(plan, WidthPx, HeightPx, 0,
            new DeckView.State(atX, atY, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false),
            0, 0, null);
        return (pen.Marks, DeckView.PlacementFor(plan, WidthPx, HeightPx, atX, atY, 0, 0));
    }

    private static (float X, float Y) At(DeckView.Placement p, double x, double y) =>
        (p.Ox + ((float)x * p.Scale), p.Oy - ((float)y * p.Scale));

    /// <summary>
    /// #791 · THE SERVICE THAT IS DRAWN IS THE SERVICE THAT ANSWERS — end to end, in pixels.
    ///
    /// <para>Owner: <i>"we should probably have service on the whole length indicated somehow."</i> The pen
    /// must lay a rail whose two ends are the two ends of the run the key measures, and it must strike a
    /// serving tick across it often enough that no stretch of the desk reads as unattended.</para>
    ///
    /// <para><b>Proven RED both ways.</b> Without the rail (the fixture as it shipped) there is no mark on
    /// the desk at all; with the rail drawn over the counter's whole BAND instead of its serving run it goes
    /// red the other way — <c>the drawn rail starts 12.0 du before the zone that answers</c> — which is the
    /// drawn-versus-simulated split this issue is one instance of.</para>
    /// </summary>
    [Fact]
    public void THE_DRAWN_ServiceIndicationIsExactlyTheZoneThatAnswers()
    {
        UndergroundComplex.ServiceRun run = Run();
        UndergroundComplex.Amenity a = Cantina();

        // Standing at the FAR END of the desk, not at the plate — so the lit state is measured from a place
        // that used to be out of reach entirely.
        (List<Mark> marks, DeckView.Placement place) = Frame(run.X1, run.Y1);

        (float e0x, float e0y) = At(place, run.X0, run.Y0);
        (float e1x, float e1y) = At(place, run.X1, run.Y1);

        static bool Ends(Mark m, (float X, float Y) p, (float X, float Y) q, float tol) =>
            m.Kind == "polyline" && m.Points.Length == 4
            && ((Near(m.Points[0], m.Points[1], p, tol) && Near(m.Points[2], m.Points[3], q, tol))
                || (Near(m.Points[0], m.Points[1], q, tol) && Near(m.Points[2], m.Points[3], p, tol)));

        // …IN THE CONSOLE'S OWN INK. #827 moved the run ONTO the desk's front face, which is also where the
        // counter's collidable wall is drawn — so "a line between these two points" now matches the wall as
        // well, and the thing this guard is about is the one drawn in the ink that means YOU MAY PRESS THIS.
        List<Mark> rail = marks
            .Where(m => Ends(m, (e0x, e0y), (e1x, e1y), 1.5f)
                && (Is(m.Ink, ConsoleNear) || Is(m.Ink, ConsoleGlow)))
            .ToList();
        Assert.True(rail.Count == 1,
            $"{rail.Count} rail(s) drawn down the desk's service run — expected exactly one, from "
            + $"({run.X0:F1},{run.Y0:F1}) to ({run.X1:F1},{run.Y1:F1}).");

        // It is LIT, because [E] answers where the captain is standing.
        Assert.True(Is(rail[0].Ink, ConsoleNear),
            "the desk is in reach and its rail is drawn in the dim ink.");

        // …and the serving ticks: struck across the rail, spread over its whole length rather than bunched
        // at the plate. Measured as marks near the rail's line, at least one in each fifth of it.
        var perFifth = new int[5];
        foreach (Mark m in marks)
        {
            if (m.Kind != "polyline" || m.Points.Length != 4 || ReferenceEquals(m, rail[0]))
            {
                continue;
            }
            float mx = (m.Points[0] + m.Points[2]) / 2f, my = (m.Points[1] + m.Points[3]) / 2f;
            double along = Along(mx, my, (e0x, e0y), (e1x, e1y), out double off);
            if (off > 2.0 * place.Scale || along < 0 || along > 1 || !Is(m.Ink, ConsoleNear))
            {
                continue;
            }
            perFifth[Math.Min(4, (int)(along * 5))]++;
        }

        for (int i = 0; i < 5; i++)
        {
            Assert.True(perFifth[i] >= 1,
                $"nothing marks the desk over its {i + 1}/5 stretch — a captain standing there is told "
                + "nothing about ordering, which is the whole of the report.");
        }

        // The plate is said ONCE (#782: a label repeated every metre is a wall of noise).
        Assert.Single(marks, m => m.Kind == $"text:{a.Fixture}");
    }

    private static bool Near(float x, float y, (float X, float Y) p, float tol) =>
        Math.Abs(x - p.X) <= tol && Math.Abs(y - p.Y) <= tol;

    private static double Along(float x, float y, (float X, float Y) a, (float X, float Y) b,
        out double off)
    {
        double ex = b.X - a.X, ey = b.Y - a.Y;
        double len2 = (ex * ex) + (ey * ey);
        double t = (((x - a.X) * ex) + ((y - a.Y) * ey)) / len2;
        double px = a.X + (t * ex), py = a.Y + (t * ey);
        off = Math.Sqrt(((x - px) * (x - px)) + ((y - py) * (y - py)));
        return t;
    }

    /// <summary>
    /// #791 · AND THE [E] IS DRAWN WHERE THE CAPTAIN IS, not forty du away at the plate. Standing at one end
    /// of the desk and then at the other must move the prompt with you.
    /// </summary>
    [Fact]
    public void THE_OFFER_IsDrawnWhereYouAreStandingAlongTheDesk()
    {
        UndergroundComplex.ServiceRun run = Run();

        (List<Mark> nearEnd, DeckView.Placement p0) = Frame(run.X0, run.Y0);
        (List<Mark> farEnd, DeckView.Placement p1) = Frame(run.X1, run.Y1);

        static (float X, float Y) Prompt(List<Mark> marks) =>
            marks.Where(m => m.Kind == "text:[E]")
                 .Select(m => (m.Points[0], m.Points[1]))
                 .Single();

        (float ax, float ay) = Prompt(nearEnd);
        (float bx, float by) = Prompt(farEnd);

        (float wantA_x, float wantA_y) = At(p0, run.X0, run.Y0);
        (float wantB_x, float wantB_y) = At(p1, run.X1, run.Y1);

        // Within a few pixels of the end you are standing at, plus the 20px drop the prompt has always had.
        Assert.True(Math.Abs(ax - wantA_x) < 6f && Math.Abs(ay - 20f - wantA_y) < 6f,
            $"standing at one end of the desk, the [E] is drawn at ({ax:F0},{ay:F0}) and the desk's end is "
            + $"at ({wantA_x:F0},{wantA_y:F0}).");
        Assert.True(Math.Abs(bx - wantB_x) < 6f && Math.Abs(by - 20f - wantB_y) < 6f,
            $"standing at the other end, the [E] is drawn at ({bx:F0},{by:F0}) and that end is at "
            + $"({wantB_x:F0},{wantB_y:F0}).");
    }

    // ── (d) THE GUIDE SAYS SO ────────────────────────────────────────────────────────────────────────

    /// <summary>#693 · A demo route nobody can follow is not a demo. The two rows a tester walks this
    /// fixture with say what changed, so the next playtest is aimed at it.</summary>
    [Fact]
    public void THE_GUIDE_RowsTellATesterToWalkTheWholeDesk()
    {
        string guide = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "testing-guide.md"));
        Assert.Contains("#791", guide, StringComparison.Ordinal);
        Assert.Contains("E-bus", guide, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }
}
