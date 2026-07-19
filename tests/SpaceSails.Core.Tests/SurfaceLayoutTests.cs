namespace SpaceSails.Core.Tests;

/// <summary>
/// Sunday-morning wind · #1–#2 (owner, 2026-07-19, verbatim): "Earth Moon and Miranda out-doors were
/// extremely similar maps. For Moon we should come up with something different… at least the walls of
/// buildings should not be the same layout." These pin the per-body ground-truth: Luna's layout is NOT
/// Miranda's, the seeded bodies are deterministic and distinct, and — the one law the geography must
/// never break — every scheme leaves a walkable corridor from the tube mouth down to the deep field.
/// </summary>
public class SurfaceLayoutTests
{
    // The shared field envelope MoonSurface hands in — the LAWS (Left/Right/Top/Bottom, the landing
    // band, the deep commitment anchor). Mirrors MoonSurface's constants so the test lays the same ground.
    private static readonly SurfaceLayout.Field Env = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -84, LandingBandY: -27, AnchorX: -6, AnchorY: -70);

    private const double AvatarRadius = 0.7; // DeckPlan.AvatarRadius — the captain's own body

    private static readonly string[] SeededBodies =
        ["phobos", "europa", "ganymede", "callisto", "titan", "enceladus"];

    // ── Luna ≠ Miranda: the owner's literal ask, as a hash of the wall sets ──

    [Fact]
    public void Luna_And_Miranda_HaveDifferentWalls()
    {
        SurfaceLayout.Plan luna = SurfaceLayout.For("luna", Env);
        SurfaceLayout.Plan miranda = SurfaceLayout.For("miranda", Env);

        Assert.NotEqual(SurfaceLayout.WallHash(miranda), SurfaceLayout.WallHash(luna));
        Assert.NotEqual(miranda.Scheme, luna.Scheme);
    }

    [Fact]
    public void Miranda_KeepsTheMonolith_Canon()
    {
        SurfaceLayout.Plan miranda = SurfaceLayout.For("miranda", Env);
        Assert.Equal("THE MONOLITH MAZE", miranda.Scheme);
        Assert.Contains(miranda.Landmarks, m => m.Label.Contains("MONOLITH"));
        // The freestanding slab still sits exactly on the deep anchor (the #318 nerve hook keys off it).
        Assert.Contains(miranda.Landmarks, m =>
            System.Math.Abs(m.X - Env.AnchorX) < 4 && System.Math.Abs(m.Y - Env.AnchorY) < 6);
    }

    [Fact]
    public void Luna_IsTheMassDriverRuins_NotAMaze()
    {
        SurfaceLayout.Plan luna = SurfaceLayout.For("luna", Env);
        Assert.Equal("THE MASS-DRIVER RUINS", luna.Scheme);
        Assert.Contains(luna.Landmarks, m => m.Label.Contains("MASS-DRIVER"));
    }

    // ── Determinism: pure, per body id, off the shared dice engine (never Random or the clock) ──

    [Fact]
    public void For_IsDeterministic_PerBodyId()
    {
        foreach (string id in SeededBodies.Concat(["luna", "miranda"]))
        {
            SurfaceLayout.Plan a = SurfaceLayout.For(id, Env);
            SurfaceLayout.Plan b = SurfaceLayout.For(id, Env);
            Assert.Equal(SurfaceLayout.WallHash(a), SurfaceLayout.WallHash(b));
            Assert.Equal(a.Scheme, b.Scheme);
            Assert.Equal(a.Walls.Count, b.Walls.Count);
        }
    }

    [Fact]
    public void SeededBodies_AllDifferFromEachOther_AndFromTheAuthoredPair()
    {
        var hashes = new Dictionary<string, long>();
        foreach (string id in SeededBodies.Concat(["luna", "miranda"]))
        {
            hashes[id] = SurfaceLayout.WallHash(SurfaceLayout.For(id, Env));
        }

        // Every body's ground is its own — no two share a wall-set hash.
        Assert.Equal(hashes.Count, hashes.Values.Distinct().Count());
    }

    [Fact]
    public void EveryScheme_ProducesWalls_AndAtLeastOneLandmark()
    {
        foreach (string id in SeededBodies.Concat(["luna", "miranda"]))
        {
            SurfaceLayout.Plan p = SurfaceLayout.For(id, Env);
            Assert.NotEmpty(p.Walls);
            Assert.NotEmpty(p.Landmarks);
        }
    }

    // ── Pathability: the one law the geography must never break — a way down from the tube mouth to the
    //    deep field exists on every ground (the walkable tube→deep path the whole loop needs). A flood
    //    fill over the field, blocking a cell that touches any wall, must reach the deep edge. ──

    [Theory]
    [InlineData("miranda")]
    [InlineData("luna")]
    [InlineData("phobos")]
    [InlineData("europa")]
    [InlineData("ganymede")]
    [InlineData("callisto")]
    [InlineData("titan")]
    [InlineData("enceladus")]
    [InlineData("some-future-moon")]
    public void EveryScheme_HasAClearCorridor_TubeMouthToDeepField(string bodyId)
    {
        SurfaceLayout.Plan plan = SurfaceLayout.For(bodyId, Env);
        var walls = plan.Walls
            .Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))
            .ToList();

        Assert.True(DeepFieldReachableFromTube(walls),
            $"{bodyId}: no walkable corridor from the tube mouth down to the deep field");
    }

    // A crude grid flood fill in deck units: start just below the tube mouth (the landing area, always
    // open), step on a 1-du lattice through cells the avatar's body clears, and succeed when we touch the
    // deep edge (the tide's spawn rim, where caches lie). Uses the SAME SurfaceCollision the live avatar
    // and Reevers obey, so a reachable deep cell here is a reachable deep cell in play.
    private static bool DeepFieldReachableFromTube(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        const double cell = 1.0;
        double minX = Env.LeftX + 1, maxX = Env.RightX - 1;
        double minY = Env.BottomY + 1, maxY = Env.TopY - 1;
        int cols = (int)((maxX - minX) / cell) + 1;
        int rows = (int)((maxY - minY) / cell) + 1;

        bool Walkable(int cx, int cy)
        {
            double x = minX + (cx * cell), y = minY + (cy * cell);
            return !SurfaceCollision.Blocked(x, y, AvatarRadius, walls);
        }
        int Col(double x) => (int)System.Math.Round((x - minX) / cell);
        int Row(double y) => (int)System.Math.Round((y - minY) / cell);

        // Start: the tube mouth column, just inside the landing band (open by law).
        int startCol = Col(-7);          // TubeCenterX
        int startRow = Row(Env.TopY - 2);
        // Goal band: the deep edge — the bottom rows where the tide claws out and chests are buried.
        int goalRow = Row(Env.BottomY + 4);

        var seen = new bool[cols, rows];
        var stack = new Stack<(int, int)>();
        if (Walkable(startCol, startRow))
        {
            stack.Push((startCol, startRow));
            seen[startCol, startRow] = true;
        }

        int[] dx = [1, -1, 0, 0];
        int[] dy = [0, 0, 1, -1];
        while (stack.Count > 0)
        {
            (int x, int y) = stack.Pop();
            if (y <= goalRow)
            {
                return true; // touched the deep edge
            }
            for (int k = 0; k < 4; k++)
            {
                int nx = x + dx[k], ny = y + dy[k];
                if (nx < 0 || ny < 0 || nx >= cols || ny >= rows || seen[nx, ny])
                {
                    continue;
                }
                if (Walkable(nx, ny))
                {
                    seen[nx, ny] = true;
                    stack.Push((nx, ny));
                }
            }
        }
        return false;
    }
}
