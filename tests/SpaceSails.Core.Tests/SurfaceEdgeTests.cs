using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 · The field's outer bound. Owner, after the fence had been made invisible but not reshaped:
/// <i>"But the limit to movement is still a box here?"</i> It was. These hold the two things that make the
/// answer no — the shape genuinely wanders, and it is still a closed loop nobody can walk out of.
/// </summary>
public class SurfaceEdgeTests
{
    private static readonly SurfaceLayout.Field Env = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -84, LandingBandY: -27, AnchorX: -6, AnchorY: -70);

    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    [Fact]
    public void TheBoundIsAClosedChain_WithNoGapToWalkOutOf()
    {
        // The one that would be catastrophic and silent. Three independently wandering edges do not meet
        // at the corners by themselves; the sin(pi*t) taper is what forces each one to arrive exactly where
        // its neighbour leaves. A gap here is not a scenic detail — it is a captain walking out of the
        // world, and no other test in the suite would notice.
        foreach ((string body, LandingSite site) in EverySite())
        {
            var chain = SurfaceEdge.Bound(body, site.LayoutSalt, Env);
            Assert.NotEmpty(chain);

            for (int i = 1; i < chain.Count; i++)
            {
                (double px, double py) = (chain[i - 1].X2, chain[i - 1].Y2);
                (double cx, double cy) = (chain[i].X1, chain[i].Y1);
                Assert.True(Math.Abs(px - cx) < 1e-6 && Math.Abs(py - cy) < 1e-6,
                    $"{body}/{site.Name}: the bound breaks between segment {i - 1} and {i}.");
            }
        }
    }

    [Fact]
    public void TheChainStartsAndEndsOnTheShipsUnderside()
    {
        // The top of the field is closed by the ship's own hull, which is drawn and straight. The wandering
        // chain has to hand off to it exactly, at both top corners, or the loop is open at the ends.
        foreach ((string body, LandingSite site) in EverySite())
        {
            var chain = SurfaceEdge.Bound(body, site.LayoutSalt, Env);

            Assert.Equal(Env.LeftX, chain[0].X1, 6);
            Assert.Equal(Env.TopY, chain[0].Y1, 6);
            Assert.Equal(Env.RightX, chain[^1].X2, 6);
            Assert.Equal(Env.TopY, chain[^1].Y2, 6);
        }
    }

    [Fact]
    public void TheBoundOnlyEverBulgesOUTWARD_SoNothingGeneratedCanBeEaten()
    {
        // The rule that makes this safe to drop under a game already generating near the edge: the outpost
        // huts (#563) are built INTO the far edge lane, and a bound free to wander inward would eventually
        // swallow one. Outward can only add bare regolith.
        foreach ((string body, LandingSite site) in EverySite())
        {
            foreach ((double x1, double y1, double x2, double y2) in SurfaceEdge.Bound(body, site.LayoutSalt, Env))
            {
                foreach ((double x, double y) in new[] { (x1, y1), (x2, y2) })
                {
                    // The invariant, stated once and correctly: NO point of the bound lies strictly inside
                    // the nominal rectangle. On it or outside it is fine — a flank at x = LeftX - bulge and
                    // a rim at y = BottomY - bulge are both outside, and the tapered corners sit exactly on
                    // it. Anything strictly inside would mean the field had SHRUNK somewhere, which is what
                    // could swallow a hut.
                    bool strictlyInside =
                        x > Env.LeftX + 1e-6 && x < Env.RightX - 1e-6 &&
                        y > Env.BottomY + 1e-6 && y < Env.TopY - 1e-6;

                    Assert.False(strictlyInside,
                        $"{body}/{site.Name}: the bound cuts INTO the field at ({x:F2}, {y:F2}) — it may only bulge out.");

                    // ...and it never wanders further out than it promised.
                    Assert.InRange(x, Env.LeftX - SurfaceEdge.MaxBulgeDu - 1e-6, Env.RightX + SurfaceEdge.MaxBulgeDu + 1e-6);
                    Assert.InRange(y, Env.BottomY - SurfaceEdge.MaxBulgeDu - 1e-6, Env.TopY + 1e-6);
                }
            }
        }
    }

    [Fact]
    public void TheNominalRectangleIsAlwaysFullyInsideTheBound()
    {
        // The stronger form of the same promise, and the one the rest of the game leans on: every point of
        // the old rectangle is still walkable ground. Sampled along each flank and the deep rim.
        foreach ((string body, LandingSite site) in EverySite())
        {
            for (int i = 0; i <= 20; i++)
            {
                double t = i / 20.0;
                Assert.True(SurfaceEdge.Bulge(body, site.LayoutSalt, SurfaceEdge.Side.Port, t) >= -1e-9);
                Assert.True(SurfaceEdge.Bulge(body, site.LayoutSalt, SurfaceEdge.Side.Starboard, t) >= -1e-9);
                Assert.True(SurfaceEdge.Bulge(body, site.LayoutSalt, SurfaceEdge.Side.Deep, t) >= -1e-9);
            }
        }
    }

    [Fact]
    public void ItActuallyWANDERS_SoTheLimitToMovementIsNotABox()
    {
        // The owner's actual question. A bound that technically supports a bulge but always computes ~0 is
        // still a rectangle, and would pass every other test in this file. So: somewhere along each site's
        // flanks and rim, the bound must stand meaningfully clear of the nominal line.
        foreach ((string body, LandingSite site) in EverySite())
        {
            foreach (SurfaceEdge.Side side in Enum.GetValues<SurfaceEdge.Side>())
            {
                double most = 0;
                for (int i = 0; i <= 40; i++)
                {
                    most = Math.Max(most, SurfaceEdge.Bulge(body, site.LayoutSalt, side, i / 40.0));
                }
                Assert.True(most > 2.0,
                    $"{body}/{site.Name}: the {side} edge is straight to within {most:F2} du — still a box.");
            }
        }
    }

    [Fact]
    public void TwoSitesOnAMoon_HaveDifferentlyShapedEdges()
    {
        // Seeded per SITE, not per body — otherwise every ground on a moon shares one silhouette and the
        // wander stops being a fact about a place.
        var shapes = LandingSites.For("miranda")
            .Select(s => string.Join(",", Enumerable.Range(0, 12)
                .Select(i => SurfaceEdge.Bulge("miranda", s.LayoutSalt, SurfaceEdge.Side.Deep, i / 11.0).ToString("F2"))))
            .Distinct()
            .Count();

        Assert.True(shapes > 1, "every Miranda site has the same edge — the salt is not reaching the seed.");
    }

    [Fact]
    public void NoSegmentIsALongStraightRun()
    {
        // A ruled line reads as generated no matter how the shape wanders around it. Every piece of the
        // bound is short enough that the eye follows a curve rather than an edge.
        foreach ((string body, LandingSite site) in EverySite())
        {
            foreach ((double x1, double y1, double x2, double y2) in SurfaceEdge.Bound(body, site.LayoutSalt, Env))
            {
                double len = Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
                Assert.True(len <= SurfaceEdge.SegmentDu * 2.0,
                    $"{body}/{site.Name}: a {len:F1} du straight run in the bound.");
            }
        }
    }
}
