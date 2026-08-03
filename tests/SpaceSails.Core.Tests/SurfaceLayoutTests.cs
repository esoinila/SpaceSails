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
    // band, the deep commitment anchor).
    //
    // #606 · READ, never copied — and this was the LAST hand-made copy of it. #573 found the same duplicate
    // in SurfaceReachabilityTests and fixed it there, in a comment that names this file as the other one; it
    // was left alone, and it went on laying a 78 x 64 world that has not shipped since the field grew
    // sixteenfold. That is not a small difference in scale, it is a different question: on that envelope the
    // shelters eat the whole field, so EVERY body came out with ZERO buildings on it and four to thirty-seven
    // wall segments. The guard below — no two bodies share a ground — was passing on eight nearly empty
    // fields, and it went red the first time a keep-out moved by a metre. On the field that ships, the same
    // eight bodies carry 636–1008 segments and 4–12 buildings apiece, and they differ comfortably.
    private static readonly SurfaceLayout.Field Env = SurfaceLayout.DefaultField;

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
    public void Miranda_KeepsItsMaze_Canon()
    {
        SurfaceLayout.Plan miranda = SurfaceLayout.For("miranda", Env);
        // #649 · The maze is canon; the WORD is not its. It kept the geometry and gave back "monolith".
        Assert.Equal(FalseSlab.Scheme, miranda.Scheme);
        Assert.Contains(miranda.Landmarks, m => m.Label == FalseSlab.ConsoleLabel);
        Assert.DoesNotContain(miranda.Landmarks, m => m.Label.Contains("MONOLITH"));
        // The freestanding slab still sits exactly on the deep anchor (the #318 nerve hook keys off it).
        Assert.Contains(miranda.Landmarks, m =>
            System.Math.Abs(m.X - Env.AnchorX) < 4 && System.Math.Abs(m.Y - Env.AnchorY) < 6);
    }

    /// <summary>#649 · Phobos is the monolith's ground and it is AUTHORED — it used to fall through to the
    /// seeded rubble generator, which is why the moon every treasure map names had no monolith on it.</summary>
    [Fact]
    public void Phobos_IsTheMonolithsGround_AndIsNotSeededRubble()
    {
        SurfaceLayout.Plan phobos = SurfaceLayout.For(Monolith.BodyId, Env);
        Assert.Equal(SurfaceLayout.MonolithScheme, phobos.Scheme);
        Assert.Contains(phobos.Landmarks, m => m.Label == Monolith.ConsoleLabel);
        Assert.NotEqual("THE DEEP RUINS", phobos.Scheme);

        // And it is NOT A BOXED BACKYARD. The owner's ruling: "it must not sit in a fenced little plot with
        // the rest of the set dressing around it… open enough that the object IS the horizon, not a prop in a
        // room." Stated as the thing that can actually be checked: every segment on this ground belongs
        // either to the object itself or to a building out in the flanks. There is NOTHING in between — no
        // corridor row, no rubble span, nothing to walk round on the way in. Miranda's maze fails this by
        // construction, which is the point of the contrast.
        var stray = new List<string>();
        foreach (SurfaceLayout.Wall w in phobos.Walls)
        {
            double mx = (w.X1 + w.X2) / 2, my = (w.Y1 + w.Y2) / 2;
            bool onTheObject = System.Math.Sqrt(((mx - Env.AnchorX) * (mx - Env.AnchorX))
                                              + ((my - Env.AnchorY) * (my - Env.AnchorY))) <= Monolith.KeepOutRadius;
            bool inABuilding = (phobos.BuildingFootprints ?? []).Any(b =>
                System.Math.Sqrt(((mx - b.X) * (mx - b.X)) + ((my - b.Y) * (my - b.Y))) <= b.R + 2);
            if (!onTheObject && !inABuilding)
            {
                stray.Add($"({mx:0.0}, {my:0.0})");
            }
        }
        Assert.True(stray.Count == 0,
            "geometry on the monolith's ground that is neither the object nor a building in the flanks — "
            + "something is standing between the landing band and the thing you came to see: "
            + string.Join(", ", stray));
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
    [InlineData("triton")]        // the retrograde landable off The Deep (berth-matrix audit)
    [InlineData("the-clinker")]   // Cinder Roost's captured-cinder landing skerry off Venus
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

    // #606 · Walked with the INDEXED walker the rest of the audits use, not a hand-rolled lattice.
    //
    // The hand-rolled one asked SurfaceCollision.Blocked against a flat wall LIST for every cell, which is
    // fine on a 78 x 64 board and is a quarter of a billion segment tests on the field that ships. Pointing
    // the old loop at the real envelope would have meant an audit too slow to keep, which is how a stale
    // mirror survives in the first place. DeckReachability is the same predicate the live avatar moves by,
    // over the same spatial index the deck builds, so a reachable deep cell here is one in play.
    // The goal is a BAND, not a point — the deep edge anywhere along it. A named spot would mostly be
    // measuring whether this file's arithmetic happened to miss a boulder, which is the sampling mistake
    // SurfaceReachabilityTests writes up at length.
    private static bool DeepFieldReachableFromTube(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        IReadOnlyCollection<DeckReachability.Point> reached = DeckReachability.Reachable(
            // From the tube mouth column, just inside the landing band (open by law).
            new DeckReachability.Point(-7, Env.TopY - 2),
            SurfaceCollision.WallIndex.Build(walls), AvatarRadius,
            (Env.LeftX + 1, Env.BottomY + 1, Env.RightX - 1, Env.TopY - 1));

        return reached.Any(p => p.Y <= Env.BottomY + 4);
    }
}
