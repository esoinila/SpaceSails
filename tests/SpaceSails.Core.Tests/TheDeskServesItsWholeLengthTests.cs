using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #791 · THE DESK SERVES ITS WHOLE LENGTH — the Core half.
///
/// <para>Owner, live playtest 2026-08-08: <i>"The Bar desk is really long now, but there is only one spot to
/// get service on it… we should probably have service on the whole length indicated somehow, so we would
/// need an E-bus of the bar desk length instead of one bar keep cashier at a single spot. Now the counter is
/// in front of the desk, where the bar keep physically would be behind the bar desk instead of being in
/// front of it."</i></para>
///
/// <h3>The two facts that made this a bug and not a preference</h3>
///
/// <para><b>Eighty-two deck units of desk, six of them pressable.</b> The counter has been a POINT since
/// #707 and the hall grew to Mos Eisley size in #751; the photograph, the collidable wall and the row of
/// stools all ran the length of it and the one thing that answered [E] did not. Nothing on the floor said
/// which six du were the live ones.</para>
///
/// <para><b>And the row of stools started at the wrong end.</b> #775 gave the counter's sealed band over to
/// a goods hoist — the first twelve du of it — and #792 laid the eight tall seats from the band's own start,
/// so on every hall in the game the first stool (and half of the second) stood in front of a freight
/// shutter, twelve du before the bar's own photograph begins. That is the drawn desk and the seated row
/// disagreeing about where the bar is, and it shipped green.</para>
///
/// <para>Every guard below walks the REAL generator over the REAL field and measures the run against the
/// desk's own PUBLISHED photograph — never against a number typed in a comment.</para>
/// </summary>
public sealed class TheDeskServesItsWholeLengthTests
{
    /// <summary>The same spread <c>TheCantinaHallTests</c> sweeps: shallow annexes, deep clinics, both cheat
    /// rocks and the head office (whose dining room serves nobody and must publish no run).</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 20).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every hall the generator lays, on every floor that has one, with the use it was carved
    /// for — so nothing below is measured on one lucky room.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Comfort Use,
        UndergroundComplex.Amenity Amenity, UndergroundComplex.Hall Hall)> Halls()
    {
        foreach (string body in Sweep())
        {
            for (int level = -1; level >= -8; level--)
            {
                foreach (UndergroundComplex.Amenity a in
                    UndergroundComplex.Build(body, level, Field).Amenities)
                {
                    if (a.Hall is { } hall)
                    {
                        yield return (body, level, a.Use, a, hall);
                    }
                }
            }
        }
    }

    /// <summary>How far along a run a point falls, and how far off it — the run's own two axes, so every
    /// law below can be stated without knowing which way this hall's rib points. A generator that one day
    /// lays a hall on a diagonal changes nothing here.</summary>
    private static (double Along, double Across) Frame(in UndergroundComplex.ServiceRun run,
        double x, double y)
    {
        double dx = run.X1 - run.X0, dy = run.Y1 - run.Y0;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        double ux = dx / len, uy = dy / len;
        double px = x - run.X0, py = y - run.Y0;
        return (((px * ux) + (py * uy)), ((px * -uy) + (py * ux)));
    }

    /// <summary>The desk's own photograph — the box #780 stretches <c>art/b1-bar-desk.jpg</c> over, which is
    /// the only PUBLISHED statement in the game about where the bar is. Every "the whole length" assertion
    /// below is measured against it rather than against the counter arithmetic, because measuring the run
    /// against the numbers it was built from would prove nothing at all.</summary>
    private static UndergroundComplex.SpotArt? Desk(in UndergroundComplex.Hall hall)
    {
        foreach (UndergroundComplex.SpotArt s in hall.Painted)
        {
            if (s.Url == UndergroundComplex.CounterArtUrl)
            {
                return s;
            }
        }
        return null;
    }

    /// <summary>The photograph's four corners, in the run's own two axes.</summary>
    private static (double Lo, double Hi, double AcrossLo, double AcrossHi) DeskInRunAxes(
        in UndergroundComplex.ServiceRun run, in UndergroundComplex.SpotArt desk)
    {
        double lo = double.MaxValue, hi = double.MinValue, alo = double.MaxValue, ahi = double.MinValue;
        foreach ((double cx, double cy) in new[]
        {
            (desk.X0, desk.Y0), (desk.X1, desk.Y0), (desk.X0, desk.Y1), (desk.X1, desk.Y1),
        })
        {
            (double along, double across) = Frame(run, cx, cy);
            lo = Math.Min(lo, along);
            hi = Math.Max(hi, along);
            alo = Math.Min(alo, across);
            ahi = Math.Max(ahi, across);
        }
        return (lo, hi, alo, ahi);
    }

    // ── (a) THE RUN IS THE DESK, AND THE WHOLE DESK ───────────────────────────────────────────────────

    /// <summary>
    /// #791 · THE SERVICE RUN COVERS EXACTLY THE DESK THE PHOTOGRAPH SHOWS — end to end, no further, and
    /// nowhere short of it.
    ///
    /// <para><b>Proven RED both ways.</b> With the fixture as it shipped — one spot, no run — every hall
    /// reports <c>no service run at all</c>. With the run laid from the band's own start (the tempting
    /// version, and the very arithmetic the stools had) it goes red the other way:</para>
    /// <code>
    /// 41 hall(s) serve over a length that is not the desk:
    ///   europa B1: the run starts 12.00 du BEFORE the desk's photograph does (over the goods hoist).
    ///   titan B1: the run starts 12.00 du BEFORE the desk's photograph does (over the goods hoist).
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void THE_RUN_IsTheWholeServingDeskAndNotOneSpotOnIt()
    {
        var wrong = new List<string>();
        int served = 0;
        double shortest = double.MaxValue, longest = 0;

        foreach ((string body, int level, UndergroundComplex.Comfort use,
            UndergroundComplex.Amenity _, UndergroundComplex.Hall hall) in Halls())
        {
            if (Desk(hall) is not { } desk)
            {
                continue;   // a hall with no bar photograph is not what this law is about
            }

            Assert.NotNull(CounterService.For(body, use));

            if (hall.Service is not { } run)
            {
                wrong.Add($"  {body} B{-level}: no service run at all — the desk is a point again.");
                continue;
            }

            served++;
            (double lo, double hi, _, _) = DeskInRunAxes(run, desk);

            if (Math.Abs(lo) > 0.01)
            {
                // lo is where the desk's photograph begins in the RUN's own axis, so a positive lo means the
                // run got going first — out over the goods hoist, which is the tempting version of this.
                wrong.Add(lo > 0
                    ? $"  {body} B{-level}: the run starts {lo:F2} du BEFORE the desk's photograph does "
                        + "(over the goods hoist)."
                    : $"  {body} B{-level}: the run starts {-lo:F2} du AFTER the desk's photograph does.");
            }
            if (Math.Abs(hi - run.LengthDu) > 0.01)
            {
                wrong.Add(
                    $"  {body} B{-level}: the run stops {hi - run.LengthDu:F2} du from the desk's far end.");
            }

            shortest = Math.Min(shortest, run.LengthDu);
            longest = Math.Max(longest, run.LengthDu);
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} hall(s) serve over a length that is not the desk:\n"
            + string.Join("\n", wrong.Take(12)));

        // ANTI-VACUITY. A guard that only ever met halls with no photograph would pass on an empty sweep,
        // and one that met halls whose desks were three du long would prove nothing about a BUS. The rooms
        // the game ships are long: the shortest of them is tens of du, which is why one press point in the
        // middle of it was a bug worth an issue.
        Assert.True(served >= 20, $"only {served} serving desk(s) in the whole sweep — nothing was measured.");

        // A point console reaches DeckPlan.InteractRadius (3) either way of itself: six du of desk, which is
        // what the owner walked into. The SMALLEST bar the generator lays is three times that, and the ones
        // the game actually ships are more than thirteen times it — so "a run" is a real claim here and not
        // a point with a wide radius, which is the shape this guard exists to refuse.
        Assert.True(shortest >= 18.0,
            $"the shortest serving desk in the game is {shortest:F1} du — barely more than the six a point "
            + "console already covered, so nothing above would notice the fixture going back to a dot.");
        Assert.True(longest >= 75.0, $"the longest serving desk is only {longest:F1} du.");
    }

    // ── (b) THE KEEP'S SIDE IS BEHIND, AND THE WALL IS BETWEEN ────────────────────────────────────────

    /// <summary>
    /// #791 · THE CAPTAIN IS IN FRONT OF THE DESK AND THE SERVICE COMES FROM BEHIND IT.
    ///
    /// <para>Owner: <i>"the bar keep physically would be behind the bar desk instead of being in front of
    /// it."</i> Three things are asserted about one axis, and they are an ORDER: the run (where a customer
    /// stands) is clear of the desk's photograph on one side, the keep's spot is INSIDE that photograph's
    /// band on the other, and the fixture's own plate spot — the square <c>?counter=1</c> stands a tester on
    /// — is on the captain's side with the run.</para>
    ///
    /// <para><b>Proven RED</b> by putting the keep on the customer's side (<c>counterV − band/2</c>, the
    /// sign slip a reader makes about a room whose v axis runs away from the spine):</para>
    /// <code>
    /// 41 hall(s) read the wrong way round:
    ///   europa B1: the keep stands 2.50 du IN FRONT of the desk, on the captain's side of it.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_KEEPS_SideIsBehindTheDeskAndTheCaptainsIsInFrontOfIt()
    {
        var wrong = new List<string>();
        int checkedHalls = 0;

        foreach ((string body, int level, UndergroundComplex.Comfort _,
            UndergroundComplex.Amenity amenity, UndergroundComplex.Hall hall) in Halls())
        {
            if (hall.Service is not { } run || Desk(hall) is not { } desk)
            {
                continue;
            }

            checkedHalls++;
            (_, _, double deskLo, double deskHi) = DeskInRunAxes(run, desk);

            (double _, double keepAcross) = Frame(run, run.KeepX, run.KeepY);
            (double _, double plateAcross) = Frame(run, amenity.X, amenity.Y);

            // The desk's band is entirely on ONE side of the run — that is what "the run is in front of the
            // desk" means, and it is the first thing that would stop being true if the standoff flipped.
            if (deskLo * deskHi <= 0)
            {
                wrong.Add($"  {body} B{-level}: the run lies INSIDE the desk's own band.");
                continue;
            }

            double behind = Math.Sign(deskLo + deskHi);   // which way "behind the desk" is, off the run
            double face = Math.Min(Math.Abs(deskLo), Math.Abs(deskHi));   // the desk's near edge
            double back = Math.Max(Math.Abs(deskLo), Math.Abs(deskHi));   // and the far wall behind it

            if (Math.Sign(keepAcross) != behind || Math.Abs(keepAcross) < face - 0.01)
            {
                wrong.Add(
                    $"  {body} B{-level}: the keep stands {Math.Abs(keepAcross):F2} du "
                    + (Math.Sign(keepAcross) == behind ? "SHORT OF" : "IN FRONT of")
                    + " the desk, on the captain's side of it.");
            }

            // …and inside the sealed band rather than out the far side of it, which is where a "behind the
            // desk" written as `counterV + band` would put him — in the park, through the glass.
            if (Math.Abs(keepAcross) > back + 0.01)
            {
                wrong.Add($"  {body} B{-level}: the keep stands beyond the far wall of his own bar.");
            }

            // The fixture's plate — and the square a tester is set down on — is on the CAPTAIN's side.
            if (Math.Abs(plateAcross) > 0.01)
            {
                wrong.Add($"  {body} B{-level}: the fixture's plate is {plateAcross:F2} du off its own run.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} hall(s) read the wrong way round:\n" + string.Join("\n", wrong.Take(12)));
        Assert.True(checkedHalls >= 20, $"only {checkedHalls} hall(s) measured.");
    }

    /// <summary>
    /// #791 · AND THE COUNTER'S OWN WALL IS STILL BETWEEN THE TWO. The service side is reachable by an eye
    /// and never by a body: a straight line from where a customer stands to where the keep stands crosses a
    /// collidable wall the carve laid, everywhere along the desk.
    ///
    /// <para>This is the clause the whole geometry rests on and it is asked of the WALL LIST — the very
    /// segments <c>SurfaceCollision</c> stops a captain on — rather than of the arithmetic that produced
    /// them. <b>Proven RED</b> by moving the run to the desk's own side of the counter (i.e. by serving from
    /// inside the bar): every sample then reaches the keep without crossing anything.</para>
    /// </summary>
    [Fact]
    public void THE_COUNTERS_WallStandsBetweenTheCustomerAndTheKeep()
    {
        var wrong = new List<string>();
        int samples = 0;

        foreach ((string body, int level, UndergroundComplex.Comfort _,
            UndergroundComplex.Amenity _, UndergroundComplex.Hall hall) in Halls())
        {
            if (hall.Service is not { } run)
            {
                continue;
            }

            IReadOnlyList<SurfaceLayout.Wall> walls = UndergroundComplex.Build(body, level, Field).Walls;

            // Walked end to end at a metre and a bit, because a wall with a gap in it is exactly the defect
            // this is watching for — the counter is three segments and #775 cut a shutter into one of them.
            int steps = Math.Max(8, (int)(run.LengthDu / 1.5));
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double sx = run.X0 + ((run.X1 - run.X0) * t);
                double sy = run.Y0 + ((run.Y1 - run.Y0) * t);

                // Straight at the keep, which is the shortest way behind the bar and the one a body would
                // take. The keep is at the run's middle, so the far ends test the line obliquely too.
                samples++;
                if (!Crosses(walls, sx, sy, run.KeepX, run.KeepY))
                {
                    wrong.Add($"  {body} B{-level}: at {sx:F1},{sy:F1} the keep's side is open floor.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} place(s) where a body could walk behind the bar:\n"
            + string.Join("\n", wrong.Take(12)));
        Assert.True(samples >= 400, $"only {samples} sample(s) — the desk was barely walked.");
    }

    /// <summary>Does the segment a..b cross any wall the carve laid? Plain segment intersection: this is a
    /// question about the PUBLISHED walls, so it is asked of them directly rather than of a walker.</summary>
    private static bool Crosses(IReadOnlyList<SurfaceLayout.Wall> walls,
        double ax, double ay, double bx, double by)
    {
        foreach (SurfaceLayout.Wall w in walls)
        {
            if (SegmentsCross(ax, ay, bx, by, w.X1, w.Y1, w.X2, w.Y2))
            {
                return true;
            }
        }
        return false;
    }

    private static bool SegmentsCross(
        double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
    {
        double d1 = Cross(cx, cy, dx, dy, ax, ay);
        double d2 = Cross(cx, cy, dx, dy, bx, by);
        double d3 = Cross(ax, ay, bx, by, cx, cy);
        double d4 = Cross(ax, ay, bx, by, dx, dy);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
            && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static double Cross(double ax, double ay, double bx, double by, double px, double py) =>
        ((bx - ax) * (py - ay)) - ((by - ay) * (px - ax));

    // ── (c) THE STOOLS STAND AT THE BAR, NOT AT THE FREIGHT SHUTTER ───────────────────────────────────

    /// <summary>
    /// #792/#791 · EVERY TALL SEAT IS BOLTED DOWN ALONG THE DESK THAT SERVES.
    ///
    /// <para>#775 gave the first twelve du of the counter's sealed band to a goods hoist and #792 laid the
    /// eight stools from the band's own start, so the row began twelve du before the bar did. <b>This is
    /// the guard that goes red on the code as it shipped</b>, and its output is the bug:</para>
    /// <code>
    /// 82 stool(s) are not at the bar:
    ///   europa B1 stool 1: 6.13 du before the desk's photograph begins — in front of the goods shutter.
    ///   europa B1 stool 2: at the very start of the desk, 5.61 du inside the hoist's own twelve.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void NO_STOOL_StandsInFrontOfTheGoodsShutter()
    {
        var wrong = new List<string>();
        int seats = 0;

        foreach ((string body, int level, UndergroundComplex.Comfort _,
            UndergroundComplex.Amenity _, UndergroundComplex.Hall hall) in Halls())
        {
            if (hall.Service is not { } run || Desk(hall) is not { } desk)
            {
                continue;
            }

            Assert.Equal(TheStools.Count, hall.StoolRow.Count);
            (double lo, double hi, double deskLo, double deskHi) = DeskInRunAxes(run, desk);
            double behind = Math.Sign(deskLo + deskHi);
            double deskFace = Math.Min(Math.Abs(deskLo), Math.Abs(deskHi));

            for (int s = 0; s < hall.StoolRow.Count; s++)
            {
                seats++;
                (double sx, double sy) = hall.StoolRow[s];
                (double along, double across) = Frame(run, sx, sy);

                if (along < lo - 0.01 || along > hi + 0.01)
                {
                    wrong.Add($"  {body} B{-level} stool {s + 1}: {(along < lo ? lo - along : along - hi):F2} "
                        + $"du {(along < lo ? "before the desk's photograph begins — in front of the goods "
                            + "shutter" : "past the end of the desk")}.");
                }

                // …and it is a SEAT AT the desk: between the standing line and the bar's own face, never
                // out in the room behind the customers and never through the counter.
                if (Math.Sign(across) != behind || Math.Abs(across) >= deskFace - 0.01)
                {
                    wrong.Add($"  {body} B{-level} stool {s + 1}: {across:F2} du off the run — a tall seat "
                        + "belongs between where a customer stands and the desk they lean on.");
                }
            }

            // The row spreads across the desk rather than huddling: the first and last seat are near the
            // two ends of it. A row that collapsed to the middle would satisfy every clause above.
            (double first, _) = Frame(run, hall.StoolRow[0].X, hall.StoolRow[0].Y);
            (double last, _) = Frame(run, hall.StoolRow[^1].X, hall.StoolRow[^1].Y);
            Assert.True(last - first > 0.7 * (hi - lo),
                $"{body} B{-level}: the eight stools span {last - first:F1} du of a {hi - lo:F1} du desk.");
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} stool(s) are not at the bar:\n" + string.Join("\n", wrong.Take(12)));
        Assert.True(seats >= 160, $"only {seats} stool(s) measured.");
    }

    // ── (d) A RUN ONLY WHERE A COUNTER SERVES ────────────────────────────────────────────────────────

    /// <summary>
    /// #791 · THE RUN AND THE CARD ARE GATED ON ONE ANSWER. A desk that takes orders publishes a run; a
    /// counter nobody has ever been served over (the staff mess, the head office's sideboard) publishes
    /// none — and that is a true statement about those rooms rather than a missing feature.
    ///
    /// <para><b>Proven RED</b> by publishing a run for every hall: the mess then advertises an eighty-du
    /// service bus to a card that does not exist.</para>
    /// </summary>
    [Fact]
    public void ONESOURCE_ARunExistsExactlyWhereTheCounterTakesOrders()
    {
        int withRun = 0, without = 0;

        foreach ((string body, int level, UndergroundComplex.Comfort use,
            UndergroundComplex.Amenity _, UndergroundComplex.Hall hall) in Halls())
        {
            bool serves = CounterService.For(body, use) is not null;
            Assert.True(serves == hall.Service.HasValue,
                $"{body} B{-level} ({use}): serves={serves} but Service={(hall.Service.HasValue ? "a run" : "null")}.");

            // The stools are gated on the very same answer, which is the whole point of asking it once.
            Assert.Equal(serves, hall.StoolRow.Count > 0);

            if (serves)
            {
                withRun++;
            }
            else
            {
                without++;
            }
        }

        // ANTI-VACUITY, both ways: a sweep of nothing but serving halls would make the second half of this
        // law untested, and one of nothing but messes would make the first.
        Assert.True(withRun >= 20, $"only {withRun} serving hall(s) in the sweep.");
        Assert.True(without >= 5, $"only {without} non-serving hall(s) in the sweep.");
    }

    // ── (e) ONE SOURCE FOR THE DESK'S LENGTH ─────────────────────────────────────────────────────────

    /// <summary>
    /// #791 · THE PHOTOGRAPH, THE ROW AND THE RUN ARE THREE VIEWS OF ONE NUMBER. Nothing outside the carve
    /// may measure this desk — the mistake the module's own header keeps a table of, and the one that put
    /// the stools at the freight shutter.
    /// </summary>
    [Fact]
    public void ONESOURCE_NobodyOutsideTheCarveMeasuresTheDesk()
    {
        string core = System.IO.File.ReadAllText(System.IO.Path.Combine(
            RepoRoot(), "src", "SpaceSails.Core", "UndergroundComplex.cs"));

        // One place computes where the serving desk starts, and the picture, the row and the run all read
        // it. Three spellings of "after the hoist" is how two of them come to disagree.
        Assert.Equal(1, Occurrences(core, "double serveU0 = hoisted ? hoistU1 : counterU0;"));
        Assert.Equal(1, Occurrences(core, "ServiceRun? service = serves"));

        // …and the client asks rather than measures.
        string hive = System.IO.File.ReadAllText(System.IO.Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Rendering", "HiveInterior.cs"));
        Assert.Contains("a.Hall?.Service", hive, StringComparison.Ordinal);
        Assert.DoesNotContain("HallCounterBandDu", hive, StringComparison.Ordinal);
        Assert.DoesNotContain("HallServiceStandoffDu", hive, StringComparison.Ordinal);
    }

    private static int Occurrences(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }

    private static string RepoRoot()
    {
        System.IO.DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (System.IO.Directory.Exists(System.IO.Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new System.IO.DirectoryNotFoundException(
            $"could not find the repo root above {AppContext.BaseDirectory}");
    }
}
