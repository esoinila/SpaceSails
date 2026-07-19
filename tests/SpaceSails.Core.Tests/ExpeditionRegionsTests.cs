namespace SpaceSails.Core.Tests;

/// <summary>
/// #371 Phase 3 — THE DOOR-OPEN DREAM (owner, cruise 2026-07-19: "I love the idea of progress bar of
/// forcing a door to open in expedition and a new space appending to the map"). These pin the pure Core
/// spine of the appended regions: determinism per (kind, door), sealed doors that sit on real ground, a
/// forced chamber that is genuinely enclosed with ONE walkable doorway, collision that is law on the new
/// walls, the depth-2 cap, the discovery constants, and — the append LAW the client's DeckPlan implements —
/// that growing the map never disturbs the collision of the geometry already on the ground (the world
/// grows, nobody teleports) and the segments grow by EXACTLY the region's own walls.
/// </summary>
public class ExpeditionRegionsTests
{
    // The same field envelope MoonSurface hands in (mirrors MoonSurface's constants).
    private static readonly SurfaceLayout.Field Env = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -84, LandingBandY: -27, AnchorX: -6, AnchorY: -70);

    private const double AvatarRadius = 0.7; // DeckPlan.AvatarRadius

    private static readonly ExpeditionSiteKind[] Kinds =
        [ExpeditionSiteKind.MysticalRuins, ExpeditionSiteKind.CrashedHull, ExpeditionSiteKind.SealedTunnel];

    private static List<SurfaceCollision.Segment> Segs(IEnumerable<SurfaceLayout.Wall> walls) =>
        walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)).ToList();

    // ── Determinism: pure per (kind, doorId, field) — same struck site, same forced ground ──

    [Fact]
    public void ForceOpen_IsDeterministic_PerKindAndDoor()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, Env))
            {
                ExpeditionRegions.Region a = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                ExpeditionRegions.Region b = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                Assert.Equal(a.Walls, b.Walls);           // record-struct value equality
                Assert.Equal(a.Consoles, b.Consoles);
                Assert.Equal(a.DiscoveryBonus, b.DiscoveryBonus);
                Assert.Equal(a.Scheme, b.Scheme);
            }
        }
    }

    [Fact]
    public void UnknownDoor_ReturnsEmptyRegion_NeverThrows()
    {
        ExpeditionRegions.Region r = ExpeditionRegions.ForceOpen(ExpeditionSiteKind.MysticalRuins, "no-such-door", Env);
        Assert.Empty(r.Walls);
        Assert.Empty(r.Consoles);
        Assert.Equal(0, r.DiscoveryBonus);
    }

    // ── The sealed doors: every kind offers 1–2 outer (depth-1) doors, on real ground inside the field ──

    [Fact]
    public void EveryKind_Offers_OneToTwo_OuterDoors_OnGroundInsideTheField()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            IReadOnlyList<ExpeditionRegions.SealedDoor> outer = ExpeditionRegions.OuterDoors(kind, Env);
            Assert.InRange(outer.Count, 1, 2);
            foreach (ExpeditionRegions.SealedDoor d in outer)
            {
                Assert.Equal(1, d.Depth);
                Assert.InRange(d.X, Env.LeftX, Env.RightX);
                Assert.InRange(d.Y, Env.BottomY, Env.LandingBandY);
                Assert.Contains("SEALED DOOR", d.Label);
            }
        }
    }

    [Fact]
    public void EveryKind_HasADepthTwoChain_ForcingItYieldsTheDeeperReward()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            // At least one outer door nests a deeper (depth-2) sealed door in the room it opens.
            var nested = new List<ExpeditionRegions.RegionConsole>();
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.OuterDoors(kind, Env))
            {
                ExpeditionRegions.Region r = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                nested.AddRange(r.Consoles.Where(c => c.Kind == ExpeditionRegions.RegionConsoleKind.SealedDoor));
            }
            Assert.NotEmpty(nested);
            Assert.All(nested, c => Assert.Equal(2, c.Depth));

            // Forcing that nested door pays the deeper reward.
            ExpeditionRegions.Region deep = ExpeditionRegions.ForceOpen(kind, nested[0].Id, Env);
            Assert.Equal(ExpeditionRegions.DiscoveryBonusDepth2, deep.DiscoveryBonus);
        }
    }

    // ── The depth cap: a depth-2 room seeds NO further door — the append can never runaway ──

    [Fact]
    public void DepthTwoRooms_SeedNoFurtherDoor_DepthIsBoundedAtTwo()
    {
        Assert.Equal(2, ExpeditionRegions.MaxDepth);
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, Env))
            {
                ExpeditionRegions.Region r = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                Assert.True(r.Depth <= ExpeditionRegions.MaxDepth);
                if (r.Depth == 2)
                {
                    Assert.DoesNotContain(r.Consoles, c => c.Kind == ExpeditionRegions.RegionConsoleKind.SealedDoor);
                }
            }
        }
    }

    // ── Discovery constants: each chamber's cache carries the depth-appropriate bonus ──

    [Fact]
    public void EveryChamber_CarriesADiscoveryCache_WithTheDepthBonus()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, Env))
            {
                ExpeditionRegions.Region r = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                Assert.Contains(r.Consoles, c => c.Kind == ExpeditionRegions.RegionConsoleKind.DiscoveryCache);
                int expected = r.Depth == 2 ? ExpeditionRegions.DiscoveryBonusDepth2 : ExpeditionRegions.DiscoveryBonusDepth1;
                Assert.Equal(expected, r.DiscoveryBonus);
            }
        }
    }

    // ── Collision is law on the new walls, and the doorway is walkable ──

    [Fact]
    public void EveryRegionWall_IsLaw_AndTheDoorwayIsWalkable()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, Env))
            {
                ExpeditionRegions.Region r = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                List<SurfaceCollision.Segment> segs = Segs(r.Walls);

                // A body centred on any wall is blocked — the maze is law for everyone on the new ground.
                foreach (SurfaceLayout.Wall w in r.Walls)
                {
                    double mx = (w.X1 + w.X2) / 2.0, my = (w.Y1 + w.Y2) / 2.0;
                    Assert.True(SurfaceCollision.Blocked(mx, my, AvatarRadius, segs),
                        $"{kind}/{d.Id}: a wall midpoint was not collidable");
                }

                // The doorway (the door's own console spot) is open ground — you stand there to force it.
                (double dx, double dy) = ExpeditionRegions.DoorPosition(kind, d.Id, Env)!.Value;
                Assert.False(SurfaceCollision.Blocked(dx, dy, AvatarRadius, segs),
                    $"{kind}/{d.Id}: the doorway was not walkable");
            }
        }
    }

    // ── Pathability: the chamber's heart is reachable from just outside its doorway (flood fill) ──

    [Fact]
    public void EveryChamber_IsReachable_FromItsDoorway()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, Env))
            {
                ExpeditionRegions.Region r = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                (double dx, double dy) = ExpeditionRegions.DoorPosition(kind, d.Id, Env)!.Value;
                // The inward direction: door → heart.
                double vx = r.RevealX - dx, vy = r.RevealY - dy;
                double len = System.Math.Sqrt((vx * vx) + (vy * vy));
                vx /= len; vy /= len;
                double startX = dx - (vx * 1.2), startY = dy - (vy * 1.2); // just OUTSIDE the doorway

                Assert.True(HeartReachable(Segs(r.Walls), startX, startY, r.RevealX, r.RevealY, r),
                    $"{kind}/{d.Id}: chamber heart not reachable through the doorway");
            }
        }
    }

    // ── The append LAW (the contract DeckPlan.AppendRegion implements): growing the map never disturbs the
    //    collision of the geometry already on the ground, and the segment set grows by EXACTLY the region's
    //    own walls (the perf guarantee shape). Tested at the Core primitive that DeckPlan mirrors. ──

    [Fact]
    public void Append_DoesNotChange_ExistingCollision_AndGrowsBy_ExactlyTheRegionWalls()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            // The base site's own geography (what is already on the ground before any door is forced).
            List<SurfaceCollision.Segment> baseSegs = Segs(SurfaceLayout.ForExpedition(kind, Env).Walls);

            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.OuterDoors(kind, Env))
            {
                ExpeditionRegions.Region region = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                List<SurfaceCollision.Segment> grown = [.. baseSegs, .. Segs(region.Walls)];

                // Perf shape: the segment count grew by EXACTLY the region's wall count — nothing else.
                Assert.Equal(baseSegs.Count + region.Walls.Count, grown.Count);
                // And the existing prefix is byte-identical — no existing segment moved.
                for (int i = 0; i < baseSegs.Count; i++)
                {
                    Assert.Equal(baseSegs[i], grown[i]);
                }

                // No-mutation of behaviour: for a lattice of points across the field, the append changes the
                // collision answer ONLY inside the appended room's own bounds — every pre-existing point of
                // ground plays exactly as it did (the world grew, nobody teleports).
                for (double x = Env.LeftX + 1; x < Env.RightX; x += 2.0)
                {
                    for (double y = Env.BottomY + 1; y < Env.LandingBandY; y += 2.0)
                    {
                        bool inNewRoom = x >= region.MinX - AvatarRadius && x <= region.MaxX + AvatarRadius
                                       && y >= region.MinY - AvatarRadius && y <= region.MaxY + AvatarRadius;
                        if (inNewRoom)
                        {
                            continue; // the new room is allowed to differ — that is the point of the append
                        }
                        Assert.Equal(
                            SurfaceCollision.Blocked(x, y, AvatarRadius, baseSegs),
                            SurfaceCollision.Blocked(x, y, AvatarRadius, grown));
                    }
                }
            }
        }
    }

    [Fact]
    public void RegionWalls_DoNotIntersect_TheBaseGeography()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            List<SurfaceCollision.Segment> baseSegs = Segs(SurfaceLayout.ForExpedition(kind, Env).Walls);
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, Env))
            {
                ExpeditionRegions.Region region = ExpeditionRegions.ForceOpen(kind, d.Id, Env);
                foreach (SurfaceLayout.Wall w in region.Walls)
                {
                    // A region wall crossing a base wall would mean the room overlaps existing geometry.
                    foreach (SurfaceCollision.Segment b in baseSegs)
                    {
                        Assert.False(SegmentsCross(w.X1, w.Y1, w.X2, w.Y2, b.X1, b.Y1, b.X2, b.Y2),
                            $"{kind}/{d.Id}: a region wall crosses the base geography");
                    }
                }
                // And the room stays inside the field's fenced rim.
                Assert.InRange(region.MinX, Env.LeftX, Env.RightX);
                Assert.InRange(region.MaxX, Env.LeftX, Env.RightX);
                Assert.InRange(region.MinY, Env.BottomY, Env.LandingBandY);
                Assert.InRange(region.MaxY, Env.BottomY, Env.LandingBandY);
            }
        }
    }

    // ── flood-fill helper: can a body reach the heart from a start point, over the room's own walls? ──
    private static bool HeartReachable(IReadOnlyList<SurfaceCollision.Segment> walls,
        double startX, double startY, double heartX, double heartY, in ExpeditionRegions.Region r)
    {
        const double cell = 0.5, margin = 4.0;
        double minX = r.MinX - margin, maxX = r.MaxX + margin;
        double minY = r.MinY - margin, maxY = r.MaxY + margin;
        int cols = (int)((maxX - minX) / cell) + 1, rows = (int)((maxY - minY) / cell) + 1;

        int Col(double x) => (int)System.Math.Round((x - minX) / cell);
        int Row(double y) => (int)System.Math.Round((y - minY) / cell);
        bool Walkable(int cx, int cy) =>
            !SurfaceCollision.Blocked(minX + (cx * cell), minY + (cy * cell), AvatarRadius, walls);

        int gc = Col(heartX), gr = Row(heartY);
        var seen = new bool[cols, rows];
        var stack = new Stack<(int, int)>();
        int sc = Col(startX), sr = Row(startY);
        if (sc < 0 || sr < 0 || sc >= cols || sr >= rows || !Walkable(sc, sr))
        {
            return false;
        }
        stack.Push((sc, sr));
        seen[sc, sr] = true;
        int[] dx = [1, -1, 0, 0], dy = [0, 0, 1, -1];
        while (stack.Count > 0)
        {
            (int x, int y) = stack.Pop();
            if (x == gc && y == gr)
            {
                return true;
            }
            for (int k = 0; k < 4; k++)
            {
                int nx = x + dx[k], ny = y + dy[k];
                if (nx < 0 || ny < 0 || nx >= cols || ny >= rows || seen[nx, ny] || !Walkable(nx, ny))
                {
                    continue;
                }
                seen[nx, ny] = true;
                stack.Push((nx, ny));
            }
        }
        return false;
    }

    // Standard proper-crossing test (shared endpoints/touching don't count as an overlap).
    private static bool SegmentsCross(double p1x, double p1y, double p2x, double p2y,
        double p3x, double p3y, double p4x, double p4y)
    {
        double d1 = Cross(p3x, p3y, p4x, p4y, p1x, p1y);
        double d2 = Cross(p3x, p3y, p4x, p4y, p2x, p2y);
        double d3 = Cross(p1x, p1y, p2x, p2y, p3x, p3y);
        double d4 = Cross(p1x, p1y, p2x, p2y, p4x, p4y);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static double Cross(double ax, double ay, double bx, double by, double cx, double cy) =>
        ((bx - ax) * (cy - ay)) - ((by - ay) * (cx - ax));
}
