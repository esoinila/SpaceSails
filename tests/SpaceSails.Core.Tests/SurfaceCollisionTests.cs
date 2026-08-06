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

    // ── #724 · THE DOORWAY CASES, in a wall list you can read in one glance ──────────────────────────────
    //
    // The law is stated on real generated Hive floors, in the client's AJambIsNotASealedDoorTests — that is
    // the test that would catch a generator laying doorways nobody can use. These are its SYNTHETIC
    // companions: four walls of hand-typed geometry whose whole point is that the reader can see the shape
    // being argued about, and which pin the two halves the honest world cannot state as sharply — that the
    // funnel FIRES at an opening, and that it does NOT fire anywhere else.

    // The same slab, cut: a doorway from y=-1.6 to y=+1.6 in a wall standing at x=0. The captain's body is
    // 1.4 du across, so the mouth is more than twice their width and nothing here is a squeeze.
    private static readonly SurfaceCollision.Segment[] SlabWithADoor =
    [
        new SurfaceCollision.Segment(0, -8, 0, -1.6),
        new SurfaceCollision.Segment(0, 1.6, 0, 8),
    ];

    [Fact]
    public void Slide_ABodyWidthOffADoorwayCut_WalksThroughItInsteadOfPinningOnTheJamb()
    {
        // The owner's thirty-five presses. Standing a whole body-width past the jamb — no part of the body
        // over the mouth but its very edge — and pressing ONLY the axis through the door.
        const double press = 0.15;
        foreach (int hand in new[] { -1, 1 })
        {
            double x = -3, y = hand * (1.6 + Radius);   // the edge of the body is on the edge of the cut
            for (int i = 0; i < 200 && x < 0.5; i++)
            {
                (x, y) = SurfaceCollision.Slide(x, y, press, 0, Radius, SlabWithADoor);
            }
            Assert.True(x > 0, $"pinned on the jamb at ({x}, {y}) — the door is 3.2 du wide and the body is 1.4");
        }
    }

    [Fact]
    public void Slide_IntoOpenWall_StillGetsYouAbsolutelyNothing()
    {
        // The other half, and the one that keeps this from being "walk through walls". Head-on into the
        // UNCUT slab, nowhere near an end: the body walks up until its own width stops it and then never
        // moves again — and above all it never drifts SIDEWAYS. A wall that quietly shuffled you along its
        // face looking for a way round would be a facility with no walls in it.
        double x = -0.9, y = 0;
        for (int i = 0; i < 200; i++)
        {
            (x, y) = SurfaceCollision.Slide(x, y, 0.15, 0, Radius, VerticalWall);
            Assert.Equal(0, y, 9);       // not one hair of unasked-for sideways travel, ever
            Assert.True(x < -Radius, $"the body walked into the slab, reaching x={x}");
        }

        // …and once it is against the face it is DONE: the last hundred presses moved it nowhere at all.
        double settled = x;
        (x, y) = SurfaceCollision.Slide(x, y, 0.15, 0, Radius, VerticalWall);
        Assert.Equal(settled, x, 9);
        Assert.Equal(0, y, 9);
    }

    [Fact]
    public void Slide_StandingInTheDoorwayPressingOnTheJamb_HoldsInsteadOfJittering()
    {
        // Standing IN the mouth of the door and pressing along the wall, into the jamb above: open ground
        // lies equally to either hand (that is what being in a doorway means), so there is nothing to
        // prefer, and a mover that picked one would flicker between them for as long as the key was held.
        // It holds — and holding here is also the honest answer, because the thing ahead really is a jamb.
        (double x, double y) = SurfaceCollision.Slide(0, 0.85, dx: 0, dy: 0.15, Radius, SlabWithADoor);
        Assert.Equal(0, x, 6);
        Assert.Equal(0.85, y, 6);
    }

    [Fact]
    public void Slide_NeverCoversMoreGroundThanTheStepItWasGiven()
    {
        // The feel guarantee. Redirecting a refused step must never be a way to travel FASTER than walking —
        // the shuffle is the same length as the press, spent on the other axis.
        var rng = new Random(724);
        for (int i = 0; i < 20_000; i++)
        {
            double x = (rng.NextDouble() * 8) - 4, y = (rng.NextDouble() * 8) - 4;
            double dx = (rng.NextDouble() * 0.6) - 0.3, dy = (rng.NextDouble() * 0.6) - 0.3;
            (double nx, double ny) = SurfaceCollision.Slide(x, y, dx, dy, Radius, SlabWithADoor);

            double moved = System.Math.Sqrt(((nx - x) * (nx - x)) + ((ny - y) * (ny - y)));
            double asked = System.Math.Sqrt((dx * dx) + (dy * dy));
            Assert.True(moved <= asked + 1e-9,
                $"a blocked step covered {moved} du on a {asked} du press, from ({x},{y}) by ({dx},{dy})");
        }
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
    public void Reever_AcrossAWall_NeverClipsThroughIt_OnlyEverRoundsTheEnds()
    {
        // The captain is on the far (right) side of the slab; a Reever starts on the near (left) side,
        // level with the captain so a straight run would drive it into the wall. Step it many times: the
        // stone is never crossed. It may WORK ITS WAY AROUND (that is the point — the maze costs the long
        // way round, it doesn't end the chase), so the honest law is that any moment it stands on the
        // captain's side, it is past one of the slab's ends — never through its span.
        const double barrierY = 100; // high above, so the crew-only clamp never fires in this horizontal test
        double rx = -2, ry = 0;
        for (int i = 0; i < 500; i++)
        {
            (rx, ry) = ReeverChase.Step(rx, ry, avatarX: 3, avatarY: 0, stepDistance: 0.2, barrierY,
                VerticalWall, Radius);
            if (rx > 0)
            {
                Assert.True(System.Math.Abs(ry) > 5 - Radius,
                    $"a Reever clipped THROUGH the slab's span at ({rx}, {ry}) on step {i}");
            }
        }
    }

    [Fact]
    public void Reever_DrivenStraightAtAWall_WorksAlongIt_InsteadOfPinningForever()
    {
        // #324 follow-up (owner, live 2026-07-26): "the reevers stopped progressing towards player as if
        // there was a wall between… a clear bug". Head-on into the slab, the whole step lands on the
        // blocked axis and the old slide-only chase pinned the hunter there for good. Now it takes the
        // wall as a handrail: over a run of steps it must cover real ground along the face.
        const double barrierY = 100;
        double rx = -1, ry = 0;
        for (int i = 0; i < 40; i++)
        {
            (rx, ry) = ReeverChase.Step(rx, ry, avatarX: 5, avatarY: 0, stepDistance: 0.2, barrierY,
                VerticalWall, Radius);
        }
        Assert.True(System.Math.Abs(ry) > 1.0, $"a wall-blocked Reever barely moved (y={ry}) — it is still pinned");
        Assert.True(rx < 0, "…and it must not have crossed the stone to get there");
    }

    [Fact]
    public void Reever_WallFollowingHand_IsFixedPerContact_SoAPackSplitsBothWays()
    {
        // The hand is a stable input, not a per-step re-decision: one side works the slab up, the other
        // down. That is what keeps a blocked hunter from dithering at the face, and sends a mixed pack
        // around a slab from both ends.
        const double barrierY = 100;
        double leftY = 0, rightY = 0, lx = -1, rxx = -1;
        for (int i = 0; i < 20; i++)
        {
            (lx, leftY) = ReeverChase.Step(lx, leftY, 5, 0, 0.2, barrierY, VerticalWall, Radius, wallSide: 1);
            (rxx, rightY) = ReeverChase.Step(rxx, rightY, 5, 0, 0.2, barrierY, VerticalWall, Radius, wallSide: -1);
        }
        Assert.True(leftY * rightY < 0, $"both hands skirted the same way (left={leftY}, right={rightY})");
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
    public void Reever_TrulyBoxedIn_IsStationary_SoDropsOffTheMotionTracker()
    {
        // A hunter with stone ahead AND stone on both hands has nowhere to take its step — it holds, and
        // its frame-to-frame velocity is ~0, which the motion tracker reads as "not moving" (the
        // motion-only law): the boxed-in Old One vanishes from the fan for free. (Merely TOUCHING a wall
        // no longer does this — post-#324-follow-up it works its way along the face instead of pinning.)
        SurfaceCollision.Segment[] pocket =
        [
            new(0, -5, 0, 5),        // the slab it is pressed against
            new(-3, 0.8, 0, 0.8),    // ceiling of the pocket
            new(-3, -0.8, 0, -0.8),  // floor of the pocket
        ];
        const double barrierY = 100; // high, so the Reever truly pins on the walls (not the crew barrier)
        double rx = -0.7, ry = 0;    // flat against the slab, wedged in the pocket
        (double nx, double ny) = ReeverChase.Step(rx, ry, avatarX: 5, avatarY: 0, stepDistance: 0.2, barrierY,
            pocket, Radius);
        (double nx2, double ny2) = ReeverChase.Step(nx, ny, avatarX: 5, avatarY: 0, stepDistance: 0.2, barrierY,
            pocket, Radius);
        double vx = nx2 - nx, vy = ny2 - ny;
        Assert.False(MotionTracker.IsMoving(vx, vy), "a boxed-in Reever must read as stationary");
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
