using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #874 · A GROWING BED IS DRAWN AS A BOX. IT HAD BETTER BE ONE.
///
/// <para>#873 fixed the CLICK on a raised bed and said, in its own body, what it was deliberately leaving
/// alone: <i>"a raised bed is drawn as a solid box and simulated as a FENCE: four rails round a
/// 12.6 × 5.6 du pocket that is perfectly standable and that nobody on this floor can ever get into."</i>
/// That is this repo's oldest bug class — the sim doing one thing while the drawn shape reports another —
/// at furniture scale, nine times per park and eleven parks in the game.</para>
///
/// <para>Nobody could ever SEE those pockets, which is exactly why they need a test. A sealed island of
/// standable floor costs nothing a player can point at: it just sits in the reachability lattice being
/// walkable-and-unwalkable-to, quietly making every audit that floods this floor slower and every refusal
/// the planner hands back mean two different things at once.</para>
///
/// <para>So this file asks the park three questions the pictures cannot answer: is there anywhere in it a
/// body could stand and never get to; is the solid the sim carries exactly the box the art draws; and does
/// a bed still stop the eye, which is a fact about guards and must not change by accident.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBedsAreSolidTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan FloorOf(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park,
        UndergroundComplex.FloorPlan Floor)> EveryPark()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Park is { } park)
                {
                    yield return (body, level, park, floor);
                }
            }
        }
    }

    /// <summary>Everywhere the captain can get to from a stand square, as a lookup — ONE flood per deck.
    /// The same helper <c>TheParkIsWalkableTests</c> keeps, for the same reason: the alternative is one A*
    /// per sample over a field this size, and there are tens of thousands of samples below.</summary>
    private sealed class Flood
    {
        private const double Step = DeckReachability.DefaultStep;
        private readonly HashSet<(int, int)> _cells = [];

        public int Count => _cells.Count;

        public Flood(DeckPlan deck, DeckReachability.Point from,
            (double, double, double, double) bounds)
        {
            foreach (DeckReachability.Point p in
                DeckReachability.Reachable(from, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
            {
                _cells.Add(Key(p.X, p.Y));
            }
        }

        private static (int, int) Key(double x, double y) =>
            ((int)Math.Round(x / Step), (int)Math.Round(y / Step));

        /// <summary>Is this LATTICE POINT on the flood? Exact, not within-a-step: this guard is counting
        /// squares that the flood did not reach, so a tolerance would forgive the very thing it looks
        /// for.</summary>
        public bool Holds(double x, double y) => _cells.Contains(Key(x, y));
    }

    // ── (a) NOWHERE IN A PARK IS THERE FLOOR NOBODY CAN GET TO ────────────────────────────────────────

    /// <summary>
    /// EVERY SQUARE OF A PARK A BODY COULD STAND ON CAN BE WALKED TO FROM THE GRAVEL.
    ///
    /// <para>The lattice is the whole of the claim. <c>TheParkIsWalkableTests</c> already walks every metre
    /// of the published gravel and finds it all reachable — it did on the afternoon the owner could not
    /// cross the room — because the gravel is a centreline and the pockets are not on it. This asks the
    /// question of the FLOOR: sweep every lattice point in the park's own box, keep the ones a body could
    /// stand on, and every single one of them must be on the flood from the park's stand square.</para>
    ///
    /// <para><b>Proven RED</b> by script-reverting <c>UndergroundComplex.cs</c> to its state at 11356fe —
    /// the beds back to four rails, the bins back to four sides:</para>
    /// <code>
    /// 27720 standable square(s) of park floor cannot be walked to from the gravel — the room is drawn one
    /// way and simulated another, and no picture in the game can show it:
    ///   luna B1: 2520 standable square(s) in 10 sealed pocket(s) — bed 1: 275 from (-100.5, -219.5),
    ///     bed 2: 275 from (-80.5, -219.5), bed 3: 286 from (38.5, -219.5), bed 4: 286 from (38.5, -229.0),
    ///     bed 5: 286 from (-61.0, -238.0), bed 6: 286 from (-61.0, -247.5), bed 7: 275 from (-1.0, -247.5),
    ///     bed 8: 275 from (58.5, -247.5), bed 9: 275 from (78.5, -247.5),
    ///     outside every bed: 1 from (16.5, -214.0)
    ///   …the same 2520, park for park, on all eleven sites.
    /// </code>
    ///
    /// <para>The last entry on that line is why this guard sweeps the ROOM and not the beds: one square,
    /// in the middle of a 1.8 du waste bin, which is the identical fault at one tenth the size and which
    /// nobody would ever have gone looking for.</para>
    /// </summary>
    [Fact]
    public void NoSquareOfAParkIsSealedOffFromTheGravel()
    {
        var bad = new List<string>();
        int parks = 0, squares = 0, sealedOff = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryPark())
        {
            DeckPlan deck = FloorOf(body, level);
            parks++;

            var from = new DeckReachability.Point(park.X, park.Y);
            Assert.True(
                DeckReachability.Standable(from.X, from.Y, DeckPlan.AvatarRadius, deck.CollisionField),
                $"{body} B{-level}: the captain cannot stand on the park's own published spot at "
                + $"({from.X:F1}, {from.Y:F1}) — every verdict below would be about a body inside a wall.");

            var flood = new Flood(deck, from,
                (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY));

            // Which pocket a stranded square is in, named by the bed it is inside — a count of squares says
            // there is a hole, and the bed number says WHICH hole, which is the difference between a red
            // test and a diagnosable one.
            var islands = new Dictionary<string, (int Count, double X, double Y)>();
            const double step = DeckReachability.DefaultStep;
            for (double x = park.X0; x <= park.X1 + 1e-9; x += step)
            {
                for (double y = park.Y0; y <= park.Y1 + 1e-9; y += step)
                {
                    if (!DeckReachability.Standable(x, y, DeckPlan.AvatarRadius, deck.CollisionField))
                    {
                        continue;
                    }
                    squares++;
                    if (flood.Holds(x, y))
                    {
                        continue;
                    }
                    sealedOff++;

                    string where = "outside every bed";
                    foreach (UndergroundComplex.GrowingBed bed in park.Beds)
                    {
                        if (bed.Contains(x, y))
                        {
                            where = $"bed {bed.Number}";
                            break;
                        }
                    }
                    islands[where] = islands.TryGetValue(where, out var had)
                        ? (had.Count + 1, had.X, had.Y)
                        : (1, x, y);
                }
            }

            if (islands.Count > 0)
            {
                bad.Add($"  {body} B{-level}: {islands.Values.Sum(v => v.Count)} standable square(s) in "
                    + $"{islands.Count} sealed pocket(s) — "
                    + string.Join(", ", islands.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .Select(kv => $"{kv.Key}: {kv.Value.Count} from "
                            + $"({kv.Value.X:F1}, {kv.Value.Y:F1})")));
            }
        }

        Assert.True(parks >= 10, $"only {parks} parks were swept — this proved little.");
        Assert.True(squares > 20000,
            $"only {squares} standable squares were found in the parks of this game — this proved little.");
        Assert.True(bad.Count == 0,
            $"{sealedOff} standable square(s) of park floor cannot be walked to from the gravel — the room "
            + "is drawn one way and simulated another, and no picture in the game can show it:\n"
            + string.Join("\n", bad.Take(12)));
    }

    // ── (b) THE SOLID THE SIM CARRIES IS THE BOX THE ART DRAWS ────────────────────────────────────────

    /// <summary>
    /// A BED'S DRAWN BOX AND ITS SIMULATED MASS ARE ONE SOURCE — the #827 counter class, at furniture scale.
    ///
    /// <para>Three claims, and they close the shape from both sides. <b>The rails are the box</b>: the four
    /// segments the renderer draws are at exactly the coordinates <see cref="UndergroundComplex.GrowingBed"/>
    /// publishes, so nothing can quietly draw a bed one size and collide it at another. <b>The box is
    /// full</b>: no lattice point inside it is standable, which is the half that was false. <b>And the box
    /// is no bigger than it is drawn</b>: every segment that lives inside a bed stays inside it, so the
    /// mass that fills the pocket cannot leak out into the gravel beside it.</para>
    ///
    /// <para><b>Proven RED</b> by script-reverting <c>UndergroundComplex.cs</c> to 11356fe:</para>
    /// <code>
    /// 99 thing(s) about a growing bed are drawn one way and simulated another:
    ///   luna B1 bed 1 at (-94.5, -217.0): 275 square(s) inside the drawn box are standable
    ///     (e.g. (-88.5, -214.5)) — the box is a fence round a hollow.
    ///   luna B1 bed 2 at (-74.6, -217.0): 275 square(s) inside the drawn box are standable
    ///     (e.g. (-68.6, -214.5)) — the box is a fence round a hollow.
    ///   …ninety-nine of them: every bed on every site.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryBedIsAsSolidAsItIsDrawnAndNoBigger()
    {
        var wrong = new List<string>();
        int beds = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryPark())
        {
            DeckPlan deck = FloorOf(body, level);

            foreach (UndergroundComplex.GrowingBed bed in park.Beds)
            {
                beds++;
                double x0 = bed.X - bed.HalfW, x1 = bed.X + bed.HalfW;
                double y0 = bed.Y - bed.HalfH, y1 = bed.Y + bed.HalfH;

                // ── THE RAILS ARE THE BOX. Four segments, at the published coordinates, in the floor's own
                //    wall list — which is the list the renderer draws and the list the collision indexes.
                (double AX, double AY, double BX, double BY)[] rails =
                [
                    (x0, y0, x1, y0), (x0, y1, x1, y1), (x0, y0, x0, y1), (x1, y0, x1, y1),
                ];
                foreach ((double ax, double ay, double bx, double by) in rails)
                {
                    if (!floor.Walls.Any(w => Same(w, ax, ay, bx, by)))
                    {
                        wrong.Add($"  {body} B{-level} bed {bed.Number}: the plan has no wall along "
                            + $"({ax:F1}, {ay:F1})–({bx:F1}, {by:F1}) — the box the art draws is not a box "
                            + "the sim has.");
                    }
                }

                // ── THE BOX IS FULL. Every lattice point inside the drawn outline, at the step the walk
                //    itself uses. A body that can stand in there is a body in the middle of the soil.
                int standable = 0;
                (double X, double Y) worst = (bed.X, bed.Y);
                for (double x = x0; x <= x1 + 1e-9; x += DeckReachability.DefaultStep)
                {
                    for (double y = y0; y <= y1 + 1e-9; y += DeckReachability.DefaultStep)
                    {
                        if (DeckReachability.Standable(x, y, DeckPlan.AvatarRadius, deck.CollisionField))
                        {
                            standable++;
                            worst = (x, y);
                        }
                    }
                }
                if (standable > 0)
                {
                    wrong.Add($"  {body} B{-level} bed {bed.Number} at ({bed.X:F1}, {bed.Y:F1}): "
                        + $"{standable} square(s) inside the drawn box are standable (e.g. "
                        + $"({worst.X:F1}, {worst.Y:F1})) — the box is a fence round a hollow.");
                }

                // ── AND NO BIGGER. Whatever fills it stays in it: a segment whose middle is inside the box
                //    must have both its ends inside the box too.
                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    if (!Inside((w.X1 + w.X2) / 2.0, (w.Y1 + w.Y2) / 2.0)
                        || (Inside(w.X1, w.Y1) && Inside(w.X2, w.Y2)))
                    {
                        continue;
                    }
                    wrong.Add($"  {body} B{-level} bed {bed.Number}: a wall from ({w.X1:F1}, {w.Y1:F1}) to "
                        + $"({w.X2:F1}, {w.Y2:F1}) starts inside the bed and ends outside it — the bed's "
                        + "mass has leaked onto the gravel.");
                }

                bool Inside(double px, double py) =>
                    px >= x0 - 0.001 && px <= x1 + 0.001 && py >= y0 - 0.001 && py <= y1 + 0.001;
            }
        }

        Assert.True(beds >= 90, $"only {beds} growing beds were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} thing(s) about a growing bed are drawn one way and simulated another:\n"
            + string.Join("\n", wrong.Take(12)));
    }

    private static bool Same(SurfaceLayout.Wall w, double ax, double ay, double bx, double by) =>
        (Near(w.X1, ax) && Near(w.Y1, ay) && Near(w.X2, bx) && Near(w.Y2, by))
        || (Near(w.X1, bx) && Near(w.Y1, by) && Near(w.X2, ax) && Near(w.Y2, ay));

    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.001;

    // ── (d) AND A BED STILL STOPS THE EYE ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A SIGHTLINE ACROSS A BED IS BLOCKED — which is what it has always been, and is now written down.
    ///
    /// <para>This is the one behaviour #874 had a choice about. A raised bed is waist-high, so an argument
    /// exists for letting a guard see over it; but it has been four wall segments since #759, and
    /// <see cref="SurfaceCollision.HasLineOfSight"/> — the ONE eye the captain, the guards and the Old Ones
    /// all share — has been stopping at those rails ever since. Filling the box in keeps that exactly as it
    /// was, and this guard is what makes "exactly as it was" a thing a build can check rather than a
    /// sentence in a PR: the day somebody decides a patrol should see over the planting, they change it
    /// here, on purpose, with the parks that go green as the evidence.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the bed's mass from <c>CarvePark</c> outright and leaving only
    /// the plate — a bed that is a picture and nothing else:</para>
    /// <code>
    /// 198 sightline(s) cross a growing bed unblocked. A bed has stopped the eye since #759 and #874 kept
    /// it that way on purpose…
    ///   luna B1 bed 1: the eye crosses it from (-94.5, -221.7) to (-94.5, -212.3).
    ///   luna B1 bed 1: the eye crosses it from (-102.7, -217.0) to (-86.3, -217.0).
    ///   luna B1 bed 2: the eye crosses it from (-74.6, -221.7) to (-74.6, -212.3).
    /// </code>
    /// </summary>
    [Fact]
    public void ABedStopsTheEyeThatCrossesIt()
    {
        var seen = new List<string>();
        int looks = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryPark())
        {
            DeckPlan deck = FloorOf(body, level);

            foreach (UndergroundComplex.GrowingBed bed in park.Beds)
            {
                // A clear body's width off each rail, so the eye starts and ends on ground somebody could
                // be standing on and the whole of the bed is between them.
                const double off = DeckPlan.AvatarRadius + 0.5;
                (double AX, double AY, double BX, double BY)[] across =
                [
                    (bed.X, bed.Y - bed.HalfH - off, bed.X, bed.Y + bed.HalfH + off),
                    (bed.X - bed.HalfW - off, bed.Y, bed.X + bed.HalfW + off, bed.Y),
                ];
                foreach ((double ax, double ay, double bx, double by) in across)
                {
                    looks++;
                    if (SurfaceCollision.HasLineOfSight(ax, ay, bx, by, deck.CollisionField))
                    {
                        seen.Add($"  {body} B{-level} bed {bed.Number}: the eye crosses it from "
                            + $"({ax:F1}, {ay:F1}) to ({bx:F1}, {by:F1}).");
                    }
                }
            }
        }

        Assert.True(looks >= 180, $"only {looks} sightlines were cast across a bed — this proved little.");
        Assert.True(seen.Count == 0,
            $"{seen.Count} sightline(s) cross a growing bed unblocked. A bed has stopped the eye since #759 "
            + "and #874 kept it that way on purpose; if a patrol is meant to see over the planting now, that "
            + "is a decision and this guard is where it is written down:\n"
            + string.Join("\n", seen.Take(12)));
    }
}
