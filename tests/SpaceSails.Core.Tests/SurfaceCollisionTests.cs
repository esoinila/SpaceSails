namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-324 · The maze must be law for everyone. These pin the SINGLE wall primitive the captain and the
/// Old Ones share (owner, live 2026-07-18: "let's not let the Reevers move through walls here… Now the
/// reevers see through wall and move through them"): a Reever bump-and-slides on a wall exactly as a boot
/// does, and cannot see (nor therefore track) a captain hidden behind stone.
/// </summary>
public class SurfaceCollisionTests
{
    // A single vertical wall at x=0 from y=-5 to y=+5 — a slab standing between left and right.
    private static readonly SurfaceCollision.Segment[] VerticalWall =
        [new SurfaceCollision.Segment(0, -5, 0, 5)];

    private const double Radius = 0.7; // DeckPlan.AvatarRadius — the captain's own body

    [Fact]
    public void Slide_StopsAtAWall_DoesNotClipThrough()
    {
        // Just left of the wall's collision band, a per-frame step straight at it is refused: the body
        // holds where it stands rather than clipping into (or through) the stone.
        (double x, double y) = SurfaceCollision.Slide(-0.9, 0, dx: 0.5, dy: 0, Radius, VerticalWall);
        Assert.Equal(-0.9, x, 6);   // the into-wall X move was blocked
        Assert.Equal(0, y, 6);
    }

    [Fact]
    public void Slide_GrazesAlongAWall_TheBumpAndSlideIdiom()
    {
        // Pushing diagonally into the wall (right + up): the into-wall X is refused but the along-wall Y
        // still carries — the captain's own grazing move, not a dead stop.
        (double x, double y) = SurfaceCollision.Slide(-0.85, 0, dx: 0.3, dy: 0.5, Radius, VerticalWall);
        Assert.Equal(-0.85, x, 6);  // the into-wall axis is blocked
        Assert.Equal(0.5, y, 6);    // the along-wall axis still slides
    }

    [Fact]
    public void HasLineOfSight_IsBrokenByAWallBetween_ClearWhenNoWallStands()
    {
        // Watcher and target on opposite sides of the slab: no sight.
        Assert.False(SurfaceCollision.HasLineOfSight(-3, 0, 3, 0, VerticalWall));
        // Both on the SAME side, the wall off to the side: clear sight.
        Assert.True(SurfaceCollision.HasLineOfSight(-3, 0, -1, 0, VerticalWall));
        // No walls at all: always clear.
        Assert.True(SurfaceCollision.HasLineOfSight(-3, 0, 3, 0, []));
    }

    [Fact]
    public void Reever_AcrossAWall_CannotCloseThroughIt_ButSlidesAlong()
    {
        // The captain is on the far (right) side of the slab; a Reever starts on the near (left) side,
        // level with the captain so a straight run would drive it into the wall. Step it many times: it
        // must never end up on the captain's side of the slab — the wall stops the pursuit dead.
        const double barrierY = 100; // high above, so the crew-only clamp never fires in this horizontal test
        double rx = -2, ry = 0;
        for (int i = 0; i < 500; i++)
        {
            (rx, ry) = ReeverChase.Step(rx, ry, avatarX: 3, avatarY: 0, stepDistance: 0.2, barrierY,
                VerticalWall, Radius);
            // It may pile up against the slab's near face, but must never reach the captain's side (x>0).
            Assert.True(rx < 0, $"a Reever clipped through the wall to x={rx} on step {i}");
        }
    }

    [Fact]
    public void Reever_WithNoWall_StillClosesNormally()
    {
        // With the wall out of the way (target and Reever both left of it), the chase closes as before —
        // the collision-aware Step is a strict superset of the plain one.
        const double barrierY = 100; // clamp parked high; no wall in this path anyway
        double rx = -8, ry = 0;
        double before = System.Math.Abs(-3 - rx);
        (rx, ry) = ReeverChase.Step(rx, ry, avatarX: -3, avatarY: 0, stepDistance: 1.0, barrierY,
            VerticalWall, Radius);
        Assert.True(System.Math.Abs(-3 - rx) < before, "the Reever should still close when no wall blocks");
    }

    [Fact]
    public void Reever_StoppedFlatAgainstWall_IsStationary_SoDropsOffTheMotionTracker()
    {
        // A Reever jammed against the slab makes no progress two steps running — its frame-to-frame
        // velocity is ~0, which the motion tracker reads as "not moving" (the motion-only law): the
        // wall-stopped hunter vanishes from the fan for free.
        const double barrierY = 100; // high, so the Reever truly pins on the wall (not the crew barrier)
        double rx = -1, ry = 0;
        (double nx, double ny) = ReeverChase.Step(rx, ry, avatarX: 5, avatarY: 0, stepDistance: 0.2, barrierY,
            VerticalWall, Radius);
        (double nx2, double ny2) = ReeverChase.Step(nx, ny, avatarX: 5, avatarY: 0, stepDistance: 0.2, barrierY,
            VerticalWall, Radius);
        double vx = nx2 - nx, vy = ny2 - ny;
        Assert.False(MotionTracker.IsMoving(vx, vy), "a wall-pinned Reever must read as stationary");
    }

    [Fact]
    public void CollisionAwareStep_StillHonorsTheCrewOnlyBarrier()
    {
        // The #295 law survives the new overload: chase toward a target up the tube, the Reever is still
        // penned at the barrier and never crosses it.
        const double barrierY = -10;
        double rx = -6, ry = -12;
        for (int i = 0; i < 300; i++)
        {
            (rx, ry) = ReeverChase.Step(rx, ry, avatarX: -6, avatarY: 8, stepDistance: 0.9, barrierY,
                walls: [], radius: Radius);
            Assert.True(ry <= barrierY, $"a Reever reached y={ry}, past the crew-only door at {barrierY}");
        }
    }
}
