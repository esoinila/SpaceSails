using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;
using Xunit.Abstractions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1478 items 2-4 · THE STAMPS ARE WHOLE. The live pass found the fissure's crack drawn past its walls, the
/// shelf's lip missing a long stretch, and the apron's top edge running through the captain's spawn.
/// </summary>
public class TheStampsAreWholeTests(ITestOutputHelper output)
{
    private static readonly SurfaceLayout.Field Env = SurfaceLayout.DefaultField;

    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];

    private static double DistToWall(double x, double y, SurfaceLayout.Wall w)
    {
        double dx = w.X2 - w.X1, dy = w.Y2 - w.Y1, l2 = (dx * dx) + (dy * dy);
        double t = l2 < 1e-12 ? 0 : Math.Clamp((((x - w.X1) * dx) + ((y - w.Y1) * dy)) / l2, 0, 1);
        return Math.Sqrt(Math.Pow(x - (w.X1 + (t * dx)), 2) + Math.Pow(y - (w.Y1 + (t * dy)), 2));
    }

    /// <summary>Length of each maximal uncovered run along one mark, sampled at a quarter du.</summary>
    private static List<double> UnwalledRuns(SurfaceScenery.Mark m, IReadOnlyList<SurfaceLayout.Wall> walls)
    {
        double len = Math.Sqrt(Math.Pow(m.X2 - m.X1, 2) + Math.Pow(m.Y2 - m.Y1, 2));
        var runs = new List<double>();
        double run = 0;
        for (double s = 0; s <= len; s += 0.25)
        {
            double x = m.X1 + ((m.X2 - m.X1) * s / len), y = m.Y1 + ((m.Y2 - m.Y1) * s / len);
            if (walls.Any(w => DistToWall(x, y, w) <= 0.75))
            {
                if (run > 0) { runs.Add(run); }
                run = 0;
            }
            else
            {
                run += 0.25;
            }
        }
        if (run > 0) { runs.Add(run); }
        return runs;
    }

    [Fact]
    public void EveryCrackMarkHasWallsExceptTheOneBridge()
    {
        foreach (string body in Bodies)
        {
            SurfaceLayout.Stamp s = SurfaceLayout.StampFor(body, Env, nameof(LandingSiteKind.IceFissure));
            var gaps = new List<double>();
            foreach (SurfaceScenery.Mark m in s.Marks)
            {
                gaps.AddRange(UnwalledRuns(m, s.Walls));
            }
            output.WriteLine($"{body}: {s.Marks.Count} marks, {s.Walls.Count} walls, gaps [{string.Join(", ", gaps.Select(g => g.ToString("F2")))}]");
            Assert.True(s.Marks.Count > 0, $"{body}: no crack at all.");
            Assert.True(gaps.Count(g => g >= 2.0) == 1,
                $"{body}: expected exactly one designed bridge (~4 du), found gaps [{string.Join(", ", gaps.Select(g => g.ToString("F2")))}].");
            Assert.All(gaps.Where(g => g < 2.0), g => Assert.True(g <= 1.0, $"{body}: an unwalled crack stretch of {g:F2} du."));
            Assert.True(gaps.Where(g => g >= 2.0).Single() <= 6.0, $"{body}: the bridge is too wide.");
        }
    }

    [Fact]
    public void TheShelfLipCoversMostOfTheFieldWidthOnEveryBody()
    {
        double width = (Env.RightX - SurfaceLayout.EdgeMargin) - (Env.LeftX + SurfaceLayout.EdgeMargin);
        foreach (string body in Bodies)
        {
            SurfaceLayout.Stamp s = SurfaceLayout.StampFor(body, Env, nameof(LandingSiteKind.CraterShelf));
            double covered = s.Walls.Sum(w => Math.Abs(w.X2 - w.X1));
            output.WriteLine($"{body}: lip {covered:F1}/{width:F1} = {covered / width:P0}; missing: " + string.Join(" ", s.Walls.OrderBy(w => w.X1).Zip(s.Walls.OrderBy(w => w.X1).Skip(1), (a, b) => (a, b)).Where(p => p.b.X1 - p.a.X2 > 0.1).Select(p => $"[{p.a.X2:F0}..{p.b.X1:F0}]")));
            Assert.True(covered >= 0.8 * width, $"{body}: the lip covers {covered / width:P0} of the field width.");
        }
    }

    [Fact]
    public void TheApronLiesFullyBelowTheLandingSquare()
    {
        (double x, double y, double r) = SurfaceLayout.LandingApproach(Env);
        foreach (string body in Bodies)
        {
            SurfaceLayout.Stamp s = SurfaceLayout.StampFor(body, Env, nameof(LandingSiteKind.DepotApron));
            Assert.NotEmpty(s.Marks);
            double top = s.Marks.Max(m => Math.Max(m.Y1, m.Y2));
            Assert.True(top < y - r - 1.0, $"{body}: the apron's top edge y={top:F1} reaches the landing square (spawn y={y:F1}, r={r}).");
            foreach (SurfaceLayout.Wall w in s.Walls)
            {
                Assert.True(Math.Max(w.Y1, w.Y2) < y - r, $"{body}: an apron post stands in the landing square.");
            }
        }
    }
}
