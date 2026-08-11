using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #819 · A LIFT MEETS ITS OWN WALL.
///
/// <para>Owner, in the evening playtest of 2026-08-11, standing on B1 at GOODS CAR 2: <i>"the elevator here
/// has little gaps to the wall."</i> It was not that car and not that floor. The corridor face was swept
/// from one sorted list of spans (#587/#775) and every entry that arrived by way of the rib list was cut
/// corridor-wide, at ± CorridorHalf = 3.5 du — but an alcove's box stands at ± ShaftHalf = 3.0 du. Both
/// cars were appended into that rib list, so the face stopped half a du outboard of the wall it was meant
/// to meet: an open slit either side of either car, on both faces, on every floor of every site.</para>
///
/// <para>The law under test is the seam itself: <b>the face's wall ends exactly where the alcove's side
/// wall begins</b>, at all four seams of every car — the two x's where the mouth is cut and the two ends of
/// the box that stand on them — and the mouth is the width of the box and not of a corridor.</para>
///
/// <para><b>Proven RED</b> by putting the alcoves back through the rib list's one-width cut (the game as it
/// shipped before this PR):</para>
/// <code>
/// 3564 seam(s) stand open beside a car:
///   luna B1 cage: the face's left end is at x 30.500, the alcove's left wall at 31.000 — a 0.500 du slit.
///   luna B1 cage: the face's right end is at x 37.500, the alcove's right wall at 37.000 — a 0.500 du slit.
///   luna B1 cage: the mouth is 7.000 du wide and the box is 6.000 — one constant is governing the other's wall.
///   luna B1 service: the face's left end is at x -141.500, the alcove's left wall at -141.000 — a 0.500 du slit.
///   …
/// </code>
/// </summary>
public sealed class TheLiftAlcoveSeamTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>A du of slack a drawn wall is allowed to miss its neighbour by. The generator works in exact
    /// arithmetic off shared constants, so this is a float's epsilon and not a tolerance for a near miss —
    /// the bug it was written for was five hundred times this wide.</summary>
    private const double Eps = 0.001;

    /// <summary>The shipped sites, both cheat rocks, and forty rolled ones — the same sweep the second car's
    /// own laws walk (TheOtherCarTests), because a seam is a fact about every floor of every building.</summary>
    private static IEnumerable<string> Sweep() =>
        new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
            "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
        }.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    /// <summary>
    /// EVERY ALCOVE ON EVERY FLOOR IS CLOSED AT BOTH SHOULDERS — the corridor's face runs up to the box and
    /// stops on it, and the box's own wall starts there and runs out.
    /// </summary>
    [Fact]
    public void NoCarStandsBesideAnOpenSlit()
    {
        IReadOnlyList<UndergroundComplex.Shaft> cars = UndergroundComplex.ShaftsOn(Field);
        Assert.True(cars.Count >= 2,
            "the shipped field takes only one car — half of this law would be about nothing.");

        var open = new List<string>();
        int seams = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.Shaft car in cars)
                {
                    seams += 4;
                    Seams(floor, car, $"{body} B{-level} {car.Kind.ToString().ToLowerInvariant()}", open);
                }
            }
        }

        Assert.True(seams > 400, $"only {seams} seams walked — this proved little.");
        Assert.True(open.Count == 0,
            $"{open.Count} seam(s) stand open beside a car:\n" + string.Join("\n", open.Take(20)));
    }

    /// <summary>The four seams of one car on one floor, read off the walls the generator actually laid.
    ///
    /// <para>Which face the car opens off is derived from its own doorstep and never typed: the cage hangs
    /// off the spine's upper face and the goods car off the lower one, so a y written here would be right
    /// for one of them and silently vacuous for the other.</para></summary>
    private static void Seams(
        in UndergroundComplex.FloorPlan floor, in UndergroundComplex.Shaft car, string where,
        List<string> open)
    {
        bool up = car.Landing.Y > car.Y;
        double faceY = car.Y + ((up ? 1 : -1) * UndergroundComplex.CorridorHalf);
        double lo = car.X - UndergroundComplex.ShaftHalf, hi = car.X + UndergroundComplex.ShaftHalf;

        // ── SEAMS 1 AND 2 · WHERE THE FACE STOPS. The face is built in segments; the mouth is the gap
        //    between the segment that arrives from the left and the one that leaves to the right.
        double left = double.NegativeInfinity, right = double.PositiveInfinity;
        foreach (SurfaceLayout.Wall w in floor.Walls)
        {
            if (Math.Abs(w.Y1 - w.Y2) > Eps || Math.Abs(w.Y1 - faceY) > Eps)
            {
                continue;   // not a segment of this face
            }
            double x0 = Math.Min(w.X1, w.X2), x1 = Math.Max(w.X1, w.X2);
            if (x1 > lo + Eps && x0 < hi - Eps)
            {
                open.Add($"  {where}: a face segment [{x0:0.000}, {x1:0.000}] lies ACROSS the mouth "
                    + $"[{lo:0.000}, {hi:0.000}] — the alcove is sealed, not slit.");
            }
            if (x1 <= car.X)
            {
                left = Math.Max(left, x1);
            }
            if (x0 >= car.X)
            {
                right = Math.Min(right, x0);
            }
        }

        if (double.IsInfinity(left) || double.IsInfinity(right))
        {
            open.Add($"  {where}: the face has no segment on one side of the car at all.");
            return;
        }
        if (Math.Abs(left - lo) > Eps)
        {
            open.Add($"  {where}: the face's left end is at x {left:0.000}, the alcove's left wall at "
                + $"{lo:0.000} — a {Math.Abs(left - lo):0.000} du slit.");
        }
        if (Math.Abs(right - hi) > Eps)
        {
            open.Add($"  {where}: the face's right end is at x {right:0.000}, the alcove's right wall at "
                + $"{hi:0.000} — a {Math.Abs(right - hi):0.000} du slit.");
        }
        if (Math.Abs((right - left) - (2.0 * UndergroundComplex.ShaftHalf)) > Eps)
        {
            open.Add($"  {where}: the mouth is {right - left:0.000} du wide and the box is "
                + $"{2.0 * UndergroundComplex.ShaftHalf:0.000} — one constant is governing the other's wall.");
        }

        // ── SEAMS 3 AND 4 · WHERE THE BOX STARTS. The two verticals either side of the car must each stand
        //    ON the face line, which is what makes the two halves of a seam one joint rather than two walls
        //    that happen to share an x.
        foreach (double side in new[] { lo, hi })
        {
            bool met = false;
            foreach (SurfaceLayout.Wall w in floor.Walls)
            {
                if (Math.Abs(w.X1 - w.X2) > Eps || Math.Abs(w.X1 - side) > Eps)
                {
                    continue;   // not one of this alcove's side walls
                }
                double near = up ? Math.Min(w.Y1, w.Y2) : Math.Max(w.Y1, w.Y2);
                met |= Math.Abs(near - faceY) <= Eps;
            }
            if (!met)
            {
                open.Add($"  {where}: nothing vertical at x {side:0.000} starts on the face at y "
                    + $"{faceY:0.000} — the box does not stand on the corridor.");
            }
        }
    }

    /// <summary>
    /// …AND THE MOUTH IS THE ALCOVE'S OWN NUMBER, said once. A companion to the sweep above, at the level of
    /// the constants: the two that met on this seam are the corridor's and the shaft's, and the fix is only
    /// a fix while they are still allowed to differ.
    ///
    /// <para>Were they ever made equal, the sweep above would pass on a generator that had gone back to
    /// cutting an alcove corridor-wide — the #587 lesson, which is that a guard has to be able to tell the
    /// two worlds apart.</para>
    /// </summary>
    [Fact]
    public void ACorridorAndAShaftAreStillTwoDifferentWidths()
    {
        Assert.NotEqual(UndergroundComplex.ShaftHalf, UndergroundComplex.CorridorHalf, 6);
    }
}
