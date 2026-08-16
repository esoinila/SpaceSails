using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #883 · THE OTHER HOLLOW BOXES — every fixture the renderer FILLS, swept for a fence round a hollow.
///
/// <para>#874 filled in the two the park's own guard happened to trip over: nine growing beds (2,520 sealed
/// squares per park) and, the tenth pocket, one standable square in the middle of every waste bin. That
/// crew's closing line is this file's brief: <i>"I did not sweep the other ~1,500 rooms for further hollow
/// boxes — only the two the park's own guard found."</i></para>
///
/// <para>So this sweeps the rest of the building, and it sweeps it at the only place the question is
/// well-posed: <see cref="DeckPlan.FurnitureSpot"/>, the filled rectangle. A box the pen fills is a box the
/// player reads as MASS — that is the whole of why #868 filled them — and mass you can stand in the middle
/// of is the sim doing one thing while the drawn shape reports another, which is this repo's oldest fault.
/// Two sources feed that pen and they are both walked here: the ring suites' fittings
/// (<c>RingOffice</c>'s placer) and the chambers' #862 kits (<c>ChamberFitting</c>).</para>
///
/// <para><b>The law is STANDABLE, not reachable</b>, and the difference is the finding. A ring fitting is
/// four rails, so its pocket is standable and SEALED — an island in the lattice nobody can enter, which is
/// exactly the beds' fault. A chamber fitting was ONE segment against the wall, so its pocket is standable
/// and WIDE OPEN — the captain walks into the drawn desk and stands inside it. Opposite symptoms, one
/// disagreement, and one predicate catches both: nowhere strictly inside a filled box may a body stand.</para>
///
/// <para>The little rooms are exempt because they are rooms: a WC cubicle, a privacy booth and a screen are
/// the three kinds <see cref="DeckPlan.FurnitureSpot.IsFurniture"/> refuses to fill, so a captain hiding
/// from a round is hiding somewhere the pen never drew as solid. This file does not have to know that —
/// it asks the pen's own predicate, so the exemption cannot drift.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFilledBoxIsFullTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>THE WHOLE BUILDING, and not the dozen sites a hand-written list remembers.
    /// <c>RipItAndBinItTests.ManySites</c>'s own net, for its own reason: this generator is procedural per
    /// body, so a fault that only shows on a room module the authored moons happen not to roll is a fault a
    /// twelve-site sweep reports as clean. 1,154 floors, which is the number #874 measured its bins
    /// over.</summary>
    private static IEnumerable<string> Bodies()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 90; i++)
        {
            yield return $"generated-moon-{i}";
        }
        yield return KaamosLore.IceMoonBodyId;
    }

    /// <summary>One filled rectangle, with enough about it to name in a red line.</summary>
    private readonly record struct Filled(
        RingOffice.Fitting Kind, double X0, double Y0, double X1, double Y1, string Where);

    /// <summary>
    /// EVERY BOX THE PEN FILLS ON THIS FLOOR — asked of Core's published fittings through the PEN'S OWN
    /// predicate, so the swept set and the drawn set are one set by construction rather than by comment.
    ///
    /// <para>Two passes, mirroring <c>HiveInterior</c>'s two: the park block's frontage (the ring suites) and
    /// every carved CHAMBER. A hall, a booth and a cell are drawn by their own idioms and are not filled.</para>
    /// </summary>
    private static List<Filled> FilledBoxesOn(in UndergroundComplex.FloorPlan floor)
    {
        var boxes = new List<Filled>();

        if (floor.Park is { } park)
        {
            foreach (UndergroundComplex.RingRoom suite in park.Frontage)
            {
                Take(suite.Furniture, suite.Plate);
            }
        }

        foreach (UndergroundComplex.Room room in floor.TheRooms)
        {
            if (room.Kind == UndergroundComplex.RoomKind.Chamber)
            {
                Take(room.Furniture, room.Plate);
            }
        }

        return boxes;

        void Take(IReadOnlyList<RingOffice.Fixture> fittings, string plate)
        {
            foreach (RingOffice.Fixture f in fittings)
            {
                // The PEN'S OWN THREE TESTS, in the pen's own order (HiveInterior.Furnish): is it furniture
                // at all, and is it a rectangle rather than a line.
                if (!DeckPlan.FurnitureSpot.IsFurniture(f.Kind)
                    || Math.Abs(f.X1 - f.X0) < 0.001
                    || Math.Abs(f.Y1 - f.Y0) < 0.001)
                {
                    continue;
                }
                boxes.Add(new Filled(f.Kind, f.X0, f.Y0, f.X1, f.Y1, plate));
            }
        }
    }

    /// <summary>Every lattice point STRICTLY INSIDE a box, on a grid anchored at its centre so the middle of
    /// the box is always one of the samples. That anchoring is not a detail: the whole of the bin's fault in
    /// #874 was one square in the exact middle of a 1.8 du box, and a grid anchored at an edge can step over
    /// it.</summary>
    private static IEnumerable<(double X, double Y)> Inside(in Filled b)
    {
        const double step = DeckReachability.DefaultStep;
        double cx = (b.X0 + b.X1) / 2.0, cy = (b.Y0 + b.Y1) / 2.0;
        int nx = (int)Math.Floor(((b.X1 - b.X0) / 2.0) / step);
        int ny = (int)Math.Floor(((b.Y1 - b.Y0) / 2.0) / step);
        var pts = new List<(double, double)>();
        for (int i = -nx; i <= nx; i++)
        {
            for (int j = -ny; j <= ny; j++)
            {
                double x = cx + (i * step), y = cy + (j * step);
                if (x > b.X0 + 1e-9 && x < b.X1 - 1e-9 && y > b.Y0 + 1e-9 && y < b.Y1 - 1e-9)
                {
                    pts.Add((x, y));
                }
            }
        }
        return pts;
    }

    // ── (a) THE SWEEP ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NOWHERE INSIDE A FILLED BOX, ON ANY FLOOR OF THIS BUILDING, CAN A BODY STAND.
    ///
    /// <para>Every floor of every site, every fitting the pen fills, every lattice square strictly inside it.
    /// The count is reported BY KIND because the fix is by kind — a desk bank and a lab bench are laid by
    /// two different placers and the same sentence is false about both of them.</para>
    ///
    /// <para><b>Proven RED</b> on the tree of 2bc28af — the ring's placer laying four rails and the chamber
    /// kit laying one segment. This is the finding, and it is the whole of the finding:</para>
    /// <code>
    /// 104806 square(s) of standable floor lie inside 5045 box(es) the pen fills in as solid, on 410
    /// floor(s). A filled box a body can stand in the middle of is the drawn shape and the sim disagreeing,
    /// and no picture in the game can show it:
    ///   DeskBank:    2151 box(es), 34441 square(s) — e.g. luna B1 (SENIOR ROTA · GREEN SIDE) — its DeskBank
    ///     at (-105.0,-167.0)-(-76.2,-165.0) holds 55 standable square(s), e.g. (-77.1, -166.0)
    ///   LabBench:    1109 box(es), 19957 square(s) — e.g. luna B2 (RECOVERY 2) — its LabBench at
    ///     (-120.4,-170.0)-(-110.9,-168.7) holds 18 standable square(s), e.g. (-111.2, -169.8)
    ///   Kitchenette:  198 box(es), 16038 square(s) — e.g. luna B1 (SENIOR ROTA · GREEN SIDE) — its
    ///     Kitchenette at (-65.2,-168.0)-(-59.2,-162.0) holds 81 standable square(s), e.g. (-60.2, -163.0)
    ///   Table:         93 box(es), 15130 square(s) — e.g. luna B1 (NEGOTIATION ROOM · BOOK AT THE COUNTER)
    ///     — its Table at (-121.5,-223.6)-(-111.5,-219.6) holds 85 standable square(s), e.g. (-112.5, -220.6)
    ///   Shelving:     802 box(es), 13218 square(s) — e.g. luna B1 (PRIVILEGED RECORDS · READING ROOM) — its
    ///     Shelving at (-49.2,-203.5)-(-47.2,-162.0) holds 81 standable square(s), e.g. (-48.2, -162.8)
    ///   Counter:      197 box(es),  4735 square(s) — e.g. luna B1 (🚻 PUBLIC WASHROOMS · NO PASS REQUIRED)
    ///     — its Counter at (-125.0,-172.4)-(-123.0,-162.0) holds 17 standable square(s), e.g. (-124.0, -163.2)
    ///   Bench:        495 box(es),  1287 square(s) — e.g. luna B1 (🚻 PUBLIC WASHROOMS · NO PASS REQUIRED)
    ///     — its Bench at (-125.0,-183.4)-(-123.0,-177.4) holds 9 standable square(s), e.g. (-124.0, -178.4)
    /// </code>
    ///
    /// <para>And the two halves of it, told apart by an A* from the room's own centre to every one of those
    /// squares — the measurement that says which fault each kind has:</para>
    /// <code>
    /// luna B1: ring:Bench sealed=13 open=0,  ring:Counter    sealed=17  open=0,
    ///          ring:DeskBank sealed=303 open=0, ring:Kitchenette sealed=162 open=0,
    ///          ring:Shelving sealed=168 open=0, ring:Table       sealed=85  open=0,
    ///          chamber:DeskBank sealed=0 open=30
    /// luna B2: chamber:LabBench sealed=0 open=126
    /// </code>
    ///
    /// <para>EVERY ring square sealed, EVERY chamber square open. Six kinds of ring furniture are the beds'
    /// exact fault at office scale — 748 squares of floor on ONE luna B1 that no route can end in. The
    /// chambers are the same disagreement inside out: 1.3 du of drawn desk with a walkable strip down the
    /// front of it, on every furnished floor in the game.</para>
    /// </summary>
    [Fact]
    public void NoBodyCanStandInsideABoxThePenFillsIn()
    {
        var pockets = new Dictionary<RingOffice.Fitting, (int Boxes, int Squares, string First)>();
        var floorsWithPockets = new SortedSet<string>();
        int floors = 0, boxes = 0, drawn = 0;

        foreach (string body in Bodies())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                List<Filled> filled = FilledBoxesOn(in floor);
                floors++;
                boxes += filled.Count;
                if (filled.Count == 0)
                {
                    continue;
                }

                DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
                drawn += deck.Furniture.Length;

                foreach (Filled b in filled)
                {
                    int standable = 0;
                    (double X, double Y) worst = (0, 0);
                    foreach ((double x, double y) in Inside(in b))
                    {
                        if (DeckReachability.Standable(
                            x, y, DeckPlan.AvatarRadius, deck.CollisionField))
                        {
                            standable++;
                            worst = (x, y);
                        }
                    }
                    if (standable == 0)
                    {
                        continue;
                    }

                    floorsWithPockets.Add($"{body} B{-level}");
                    string first = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} B{1} ({2}) — its {3} at ({4:F1},{5:F1})-({6:F1},{7:F1}) holds {8} standable "
                        + "square(s), e.g. ({9:F1}, {10:F1})",
                        body, -level, b.Where, b.Kind, b.X0, b.Y0, b.X1, b.Y1, standable, worst.X, worst.Y);
                    pockets[b.Kind] = pockets.TryGetValue(b.Kind, out var had)
                        ? (had.Boxes + 1, had.Squares + standable, had.First)
                        : (1, standable, first);
                }
            }
        }

        // ── ANTI-VACUITY. A sweep that found no furniture would pass this test in silence, which is the
        //    fifth bug class in this house's own list. So: the building was walked, the boxes were found,
        //    and the set this file swept is exactly the set the pen filled.
        Assert.True(floors >= 1000, $"only {floors} floors were swept — this proved little.");
        Assert.True(boxes >= 3000, $"only {boxes} filled boxes were found — this proved little.");
        Assert.True(drawn >= boxes,
            $"the pen filled {drawn} rectangle(s) and this sweep only knew about {boxes} — a source of "
            + "furniture has been added that this guard does not walk.");

        int totalBoxes = pockets.Values.Sum(v => v.Boxes), totalSquares = pockets.Values.Sum(v => v.Squares);
        Assert.True(pockets.Count == 0,
            $"{totalSquares} square(s) of standable floor lie inside {totalBoxes} box(es) the pen fills in "
            + $"as solid, on {floorsWithPockets.Count} floor(s). A filled box a body can stand in the middle "
            + "of is the drawn shape and the sim disagreeing, and no picture in the game can show it:\n"
            + string.Join("\n", pockets
                .OrderByDescending(kv => kv.Value.Squares)
                .Select(kv => $"  {kv.Key}: {kv.Value.Boxes} box(es), {kv.Value.Squares} square(s) — "
                    + $"e.g. {kv.Value.First}")));
    }

    // ── (b) THE SEGMENT BUDGET ────────────────────────────────────────────────────────────────────────

    /// <summary>What the building's walls counted the day before #883, over the same 1,154 floors this file
    /// sweeps. Not an estimate: the number a run printed on the tree at 2bc28af.</summary>
    private const int SegmentsBefore = 236756;

    /// <summary>…and the heaviest single floor in it — generated-moon-16 B1. A total can hide a floor that
    /// doubled, so the worst one is pinned separately and it is the number that matters: the patrol eye is
    /// O(walls) on the floor it is standing on, never on the building (Lab 45).</summary>
    private const int WorstFloorBefore = 636;

    /// <summary>HOW MUCH MORE STONE #883 IS ALLOWED TO COST, and the figure the PR body quotes. Three per
    /// cent, against a measured +2.4% total and +0.3% on the worst floor — so the guard has a real margin
    /// and is nowhere near a threshold that would select everything.
    ///
    /// <para>It is 3% and not 10% because the first cut of this change was 10%: <c>AddSolidMass</c> hatched
    /// upright whatever the box's shape, a 28.8 × 2.0 du desk bank cost twenty-five strokes to say what one
    /// flat stroke says, and the building came out at 260,130 segments with a worst floor of 853. Both
    /// numbers were run before the allowance was chosen. Raising this constant to fit a future regression is
    /// the whole thing it exists to stop.</para>
    ///
    /// <para>#828 · <b>What has been spent since, and by whom.</b> The disposal ladder's third rung stands a
    /// secure disposal in every premium suite — 198 machines, 6.0 × 3.0 du, four rails and two flat hatch
    /// strokes apiece, through the same <c>Lay.Box</c> as every other fitting. That is <b>1,188 segments</b>,
    /// taking the building from 242,367 to <b>243,555</b> (worst floor 638 → <b>650</b>): <b>+2.87%</b> of a
    /// 3% allowance, and +2.20% on the worst floor. It fits, and it is recorded here rather than left for
    /// somebody to rediscover — the next lane that stands furniture in the ring has about three hundred
    /// segments of room, and should re-measure this baseline rather than widen the number below it.</para>
    /// </summary>
    private const double Allowance = 0.03;

    /// <summary>
    /// FILLING IN EVERY BOX IN THE BUILDING COST 3% MORE WALL, AND NO FLOOR PAID MORE THAN THAT.
    ///
    /// <para>Lab 45 measured the sightline at O(walls) and #862 chose segments over rectangles for that
    /// reason, so a sweep that makes fifteen hundred rooms honest has to say what it spent. It spends it
    /// twice — three rails apiece on the chamber kit, and a hatch on the ring's bigger boxes — and both are
    /// bounded here, per floor and in total, against numbers measured before one line was changed.</para>
    ///
    /// <para><b>Where the 5,611 went</b>, measured in three runs over the same 1,154 floors rather than
    /// argued: the tree at 2bc28af carries <b>236,756</b> segments (worst floor 636). Teaching
    /// <c>AddSolidMass</c> to hatch across the SHORT side, with no lay site touched at all, takes the
    /// building to <b>231,410</b> (worst floor 582) — the growing beds alone go from eleven upright strokes
    /// to four flat ones, and nothing about what they seal changes. Filling in the ring and the chambers on
    /// top of that lands at <b>242,367</b> (worst floor 638). So the honesty cost about eleven thousand
    /// segments and the arithmetic gave five thousand of them back.</para>
    ///
    /// <para><b>Proven RED</b> by reverting <c>AddSolidMass</c>'s <c>Hatch.Fewest</c> arm — upright courses
    /// on every box, which is how the method has hatched since #586:</para>
    /// <code>
    /// the heaviest floor in the building (generated-moon-54 B1) now carries 853 wall segment(s). It
    /// carried 636 before #883 and the stated allowance is 3 % (655) — somebody has spent 34.1 % more of the
    /// patrol eye's frame than this PR said it would (Lab 45: the sightline is O(walls)).
    /// </code>
    /// </summary>
    [Fact]
    public void TheHonestBoxesCostNoMoreWallThanTheBodySaid()
    {
        long total = 0;
        int floors = 0, worst = 0;
        string worstAt = "";

        foreach (string body in Bodies())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                int n = UndergroundComplex.Build(body, level, Field).Walls.Count;
                floors++;
                total += n;
                if (n > worst)
                {
                    (worst, worstAt) = (n, $"{body} B{-level}");
                }
            }
        }

        // ANTI-VACUITY, and the clause that stops the allowance from being quietly widened: the number in
        // the PR body is 3%, so the number here is 3%, and a run that measured a different building is a run
        // whose baseline no longer means anything.
        Assert.Equal(0.03, Allowance);
        Assert.True(floors >= 1000, $"only {floors} floors were counted — this proved little.");
        Assert.True(total > SegmentsBefore * 0.9,
            $"the building has {total} wall segment(s) against {SegmentsBefore} before #883 — that is not a "
            + "budget overrun, it is a different building, and the baseline below means nothing about it.");

        int floorCap = (int)(WorstFloorBefore * (1 + Allowance));
        Assert.True(worst <= floorCap,
            $"the heaviest floor in the building ({worstAt}) now carries {worst} wall segment(s). It carried "
            + $"{WorstFloorBefore} before #883 and the stated allowance is {Allowance:P0} ({floorCap}) — "
            + $"somebody has spent {(worst - WorstFloorBefore) / (double)WorstFloorBefore:P1} more of the "
            + "patrol eye's frame than this PR said it would (Lab 45: the sightline is O(walls)).");

        long cap = (long)(SegmentsBefore * (1 + Allowance));
        Assert.True(total <= cap,
            $"the building carries {total} wall segment(s) against {SegmentsBefore} before #883 — "
            + $"{(total - SegmentsBefore) / (double)SegmentsBefore:P1} more, and the stated allowance is "
            + $"{Allowance:P0} ({cap}).");
    }
}
