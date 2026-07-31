using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 · The structure generator. Owner: <i>"1 pixel wide variously rotated letter U-shapes are just not
/// buildings enough"</i> — so these hold the two things that make one a building rather than a drawing: the
/// wall is solid mass you cannot stand inside, and the inside is somewhere you can actually get to.
/// </summary>
public class SurfaceStructureTests
{
    private const double AvatarRadius = 0.7;

    private static IEnumerable<SurfaceStructure.Spec> EveryShape()
    {
        foreach (SurfaceStructure.Footprint shape in Enum.GetValues<SurfaceStructure.Footprint>())
        {
            foreach (double angle in new[] { 0.0, 0.4, 1.1, 2.7 })
            {
                foreach (int doors in new[] { 1, 2, 3 })
                {
                    yield return new SurfaceStructure.Spec(
                        CentreX: 0, CentreY: 0, Width: 14, Height: 11,
                        AngleRad: angle, Doors: doors, WallThickness: 1.6, Shape: shape);
                }
            }
        }
    }

    private static (double, double, double, double) Bounds => (-40, -40, 40, 40);

    private static List<SurfaceCollision.Segment> Segments(SurfaceStructure.Built b) =>
        [.. b.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))];

    [Fact]
    public void TheWallIsSOLID_ThereIsNoSlotInsideItToStandIn()
    {
        // The whole point of hatching the thickness. Two parallel faces with a gap between them is not a
        // thick wall, it is a corridor nobody can reach — the sealed-pocket failure the surface flood
        // exists to catch, hidden inside a wall where nobody would look.
        //
        // Flooded rather than sampled on a ring: an earlier version of this test walked a circle through
        // where it thought the wall was, which misses a polygon's faces entirely and reported 73 of 126
        // points "standable" purely because they were out in the open. A flood needs to know nothing about
        // the shape. With a doorway, inside and outside are ONE connected region — so any second region is
        // a cavity, by definition.
        foreach (SurfaceStructure.Spec spec in EveryShape())
        {
            List<SurfaceCollision.Segment> segs = Segments(SurfaceStructure.Build(spec));
            Assert.Equal(1, StandableRegions(segs, spec));
        }
    }

    /// <summary>How many separate patches of standable ground exist around a structure. One means the
    /// inside and the outside are joined by a doorway and there is nothing else. Two or more means either
    /// the building is sealed or its wall has a hollow slot in it.</summary>
    private static int StandableRegions(List<SurfaceCollision.Segment> segs, SurfaceStructure.Spec spec)
    {
        const double step = 0.35, reach = 16;
        int n = (int)(reach * 2 / step);
        double X(int i) => spec.CentreX - reach + (i * step);
        double Y(int j) => spec.CentreY - reach + (j * step);

        var open = new bool[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                open[i, j] = !SurfaceCollision.Blocked(X(i), Y(j), AvatarRadius, segs);
            }
        }

        var seen = new bool[n, n];
        int regions = 0;
        (int, int)[] steps = [(1, 0), (-1, 0), (0, 1), (0, -1)];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (!open[i, j] || seen[i, j])
                {
                    continue;
                }
                regions++;
                var q = new Queue<(int, int)>();
                seen[i, j] = true;
                q.Enqueue((i, j));
                while (q.Count > 0)
                {
                    (int ci, int cj) = q.Dequeue();
                    foreach ((int di, int dj) in steps)
                    {
                        int ni = ci + di, nj = cj + dj;
                        if (ni < 0 || nj < 0 || ni >= n || nj >= n || seen[ni, nj] || !open[ni, nj])
                        {
                            continue;
                        }
                        seen[ni, nj] = true;
                        q.Enqueue((ni, nj));
                    }
                }
            }
        }
        return regions;
    }

    [Fact]
    public void YouCanWalkIn_ThroughEveryShapeAtEveryAngle()
    {
        // A building you cannot enter is the U-shape's opposite failure and just as useless. Walk from well
        // outside to the middle.
        foreach (SurfaceStructure.Spec spec in EveryShape())
        {
            SurfaceStructure.Built b = SurfaceStructure.Build(spec);
            Assert.NotEmpty(b.Doorways);

            bool reached = DeckReachability.CanReach(
                new(spec.CentreX - 30, spec.CentreY - 30), new(spec.CentreX, spec.CentreY),
                Segments(b), AvatarRadius, Bounds);

            Assert.True(reached,
                $"{spec.Shape} at {spec.AngleRad:F1} rad with {spec.Doors} door(s): sealed shut.");
        }
    }

    [Fact]
    public void AndYouCanGetOutAgain()
    {
        // Not redundant with the above: A* is run over a symmetric graph here, but a one-way structure is
        // exactly the kind of thing a doorway offset bug produces, and being trapped inside a ruin on a
        // suit-air clock (#564) is a death the player cannot see coming.
        foreach (SurfaceStructure.Spec spec in EveryShape())
        {
            SurfaceStructure.Built b = SurfaceStructure.Build(spec);
            Assert.True(DeckReachability.CanReach(
                new(spec.CentreX, spec.CentreY), new(spec.CentreX - 30, spec.CentreY - 30),
                Segments(b), AvatarRadius, Bounds));
        }
    }

    [Fact]
    public void NoDegenerateStubs()
    {
        // DegenerateWallScan's law, held at the source: a wall shorter than the body it blocks reads as an
        // invisible wall. The rotation maths is exactly where those get created.
        foreach (SurfaceStructure.Spec spec in EveryShape())
        {
            foreach (SurfaceLayout.Wall w in SurfaceStructure.Build(spec).Walls)
            {
                double len = Math.Sqrt(((w.X2 - w.X1) * (w.X2 - w.X1)) + ((w.Y2 - w.Y1) * (w.Y2 - w.Y1)));
                Assert.True(len >= 0.5, $"{spec.Shape}: a {len:F2} du wall — shorter than the captain.");
            }
        }
    }

    [Fact]
    public void ItIsPure_TheSameSpecAlwaysBuildsTheSameWalls()
    {
        foreach (SurfaceStructure.Spec spec in EveryShape())
        {
            SurfaceStructure.Built a = SurfaceStructure.Build(spec);
            SurfaceStructure.Built b = SurfaceStructure.Build(spec);
            Assert.Equal(a.Walls.Count, b.Walls.Count);
            for (int i = 0; i < a.Walls.Count; i++)
            {
                Assert.Equal(a.Walls[i], b.Walls[i]);
            }
        }
    }

    [Fact]
    public void ARotatedBuildingIsActuallyRotated()
    {
        // Every wall on a landing site has been axis-aligned since the game began, which is most of the
        // reason the ground reads as graph paper. If the angle silently did nothing this would all still
        // pass, so: an angled building must have walls that are neither horizontal nor vertical.
        var spec = new SurfaceStructure.Spec(0, 0, 14, 11, 0.6, 2, 1.6, SurfaceStructure.Footprint.Rectangular);
        bool anyOblique = SurfaceStructure.Build(spec).Walls.Any(w =>
            Math.Abs(w.X2 - w.X1) > 0.3 && Math.Abs(w.Y2 - w.Y1) > 0.3);

        Assert.True(anyOblique, "the angle did nothing — every wall is still square to the grid.");
    }

    [Fact]
    public void AThinRequestIsStillBuiltThick()
    {
        // "not 1 pixel line" is a floor, not a suggestion. A caller asking for a hairline gets masonry.
        var spec = new SurfaceStructure.Spec(0, 0, 14, 11, 0, 1, 0.05, SurfaceStructure.Footprint.Rectangular);
        SurfaceStructure.Built b = SurfaceStructure.Build(spec);

        // With a real thickness there are hatch segments; with a hairline there would be almost none.
        Assert.True(b.Walls.Count > 30,
            $"only {b.Walls.Count} segments — the wall came out as lines, not mass.");
    }
}
