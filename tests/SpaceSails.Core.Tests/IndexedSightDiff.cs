namespace SpaceSails.Core.Tests;

/// <summary>
/// #442 · Two findings from the owner's live reports, pinned so the refactor has to answer them.
///
/// <para><b>"See it fire through wall now"</b> — settled first by ruling OUT the #448 wall index: 76,000
/// sightlines across every real generated ground answer identically indexed and swept. The index is honest;
/// the bug was a third caller (the drawn zap beam) that never asked the walls at all.</para>
///
/// <para><b>"See the invisible wall here… can shoot through the invisible wall though"</b> — the mechanism,
/// below. A BODY collides with a capsule (the segment fattened by its radius); an EYE and a SHOT test the
/// bare segment; and the renderer draws the bare segment. So every wall carries a body-radius margin that
/// stops the captain, passes bullets, and is painted nowhere — worst at the ENDS, where the margin sticks
/// out past the visible stone into open ground.</para>
/// </summary>
public class IndexedSightDiff
{
    private static readonly SurfaceLayout.Field Field = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -96, LandingBandY: -27, AnchorX: -6, AnchorY: -21.5);

    private const double Radius = 0.7; // DeckPlan.AvatarRadius

    [Fact]
    public void IndexedSight_MatchesTheHandSweep_OnEveryRealGround()
    {
        // The #448 index must never change an ANSWER, only how fast it is reached — otherwise a missed wall
        // is a shot through stone. Swept across every landable body × every seeded landing site (#320).
        string[] bodies = ["miranda", "luna", "europa", "ganymede", "titan", "enceladus", "triton"];
        int checks = 0;

        foreach (string body in bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SurfaceLayout.Plan plan = SurfaceLayout.For(body, Field, site.LayoutSalt);
                SurfaceCollision.Segment[] plain =
                    [.. plan.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))];
                SurfaceCollision.WallIndex indexed = SurfaceCollision.WallIndex.Build(plain);

                var rng = new Random(20260727);
                for (int i = 0; i < 4000; i++)
                {
                    double ax = -46 + (rng.NextDouble() * 82);
                    double ay = -98 + (rng.NextDouble() * 80);
                    double bx = ax + (-18 + (rng.NextDouble() * 36));
                    double by = ay + (-18 + (rng.NextDouble() * 36));

                    checks++;
                    Assert.Equal(
                        SurfaceCollision.HasLineOfSight(ax, ay, bx, by, plain),      // the honest sweep
                        SurfaceCollision.HasLineOfSight(ax, ay, bx, by, indexed));   // the indexed walk
                }
            }
        }

        Assert.True(checks > 50_000, $"the sweep only checked {checks} sightlines");
    }

    [Fact]
    public void PastAWallsEND_TheCaptainIsStopped_ButAShotSailsThrough()
    {
        // THE INVISIBLE WALL, in one assertion. A slab from (0,-60) to (0,-50): its top end is at y = -50.
        // Stand just BEYOND that end — (0, -49.5), half a unit past where the stone visibly stops.
        SurfaceCollision.Segment[] slab = [new(0, -60, 0, -50)];

        // The body is blocked there: DistanceToSegment measures to the ENDPOINT, so the barrier is really a
        // capsule with a rounded cap sticking a full radius past the drawn line.
        Assert.True(SurfaceCollision.Blocked(0, -49.5, Radius, slab),
            "a body should be stopped by the wall's rounded cap");

        // …and a shot crossing that very spot passes clean through, because a sightline tests the BARE
        // segment and the segment simply is not there any more.
        Assert.True(SurfaceCollision.HasLineOfSight(-3, -49.5, 3, -49.5, slab),
            "a shot should sail through the same spot the body could not enter");

        // The renderer draws the bare segment too — so that half-unit of barrier is painted nowhere. Three
        // readers, three different answers about the same point of ground: exactly what #442 must reconcile.
    }

    [Fact]
    public void TheGhostMargin_SurroundsEveryWall_NotJustItsEnds()
    {
        // The same divergence hugs the whole length: a body is held a radius off the face, so there is a
        // band on both sides of every wall that stops a captain and passes a bullet. Along the face this
        // reads as "the wall is thick"; past the ends it reads as an invisible wall in open ground.
        SurfaceCollision.Segment[] slab = [new(0, -60, 0, -50)];

        Assert.True(SurfaceCollision.Blocked(-0.5, -55, Radius, slab), "held off the face");
        Assert.True(SurfaceCollision.HasLineOfSight(-0.5, -56, -0.5, -54, slab),
            "…while a shot runs freely up the same margin");
    }
}
