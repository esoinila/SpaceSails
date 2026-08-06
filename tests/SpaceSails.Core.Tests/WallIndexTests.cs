namespace SpaceSails.Core.Tests;

/// <summary>
/// #448 · THE CEILING ON THE STONE, and the proof it changed nothing. Owner, live 2026-07-26:
/// <i>"Now the shuttle ride is timing out twice at least."</i> The cure is a coarse grid over the walls
/// (<see cref="SurfaceCollision.WallIndex"/>) so a step, a sightline or a shot only measures the stone
/// near it instead of the whole ground — and the ONE thing such a cure must never do is answer
/// differently from the honest full sweep.
///
/// <para>So these tests are not about speed at all. They are a differential: for grounds ranging from a
/// single slab to a seeded maze, and for thousands of queries fired across them, the indexed answer and
/// the swept answer must agree EXACTLY — the same blocked/clear verdicts, and the same slid positions to
/// the bit. A grid that misses a wall would let a Reever walk through stone; a grid that misses a
/// sightline would let one see through it. Both are caught here.</para>
/// </summary>
public class WallIndexTests
{
    private const double Radius = 0.7; // DeckPlan.AvatarRadius — the captain's own body

    // A slab, a box, a long rim and a diagonal — the shapes real grounds are actually made of, including
    // the degenerate zero-length segment #442 pins elsewhere.
    private static SurfaceCollision.Segment[] MixedGround() =>
    [
        new(0, -5, 0, 5),          // the canon vertical slab
        new(-20, -20, 20, -20),    // a long rim, spanning many cells
        new(-6, 8, 6, 8),
        new(-6, 8, -6, 2),
        new(6, 8, 6, 2),
        new(-14, -3, -8, 4),       // a diagonal
        new(9, -9, 13, -13),       // another, the other way
        new(3, 3, 3, 3),           // a point of stone (zero length)
        new(-18, 12, 18, 12),
        new(11, 0, 11, 11),
    ];

    // A seeded scatter, so the shapes are not only the ones a human happened to think of.
    private static SurfaceCollision.Segment[] SeededGround(int seed, int count)
    {
        var rng = new System.Random(seed);
        var walls = new SurfaceCollision.Segment[count];
        for (int i = 0; i < count; i++)
        {
            double x = (rng.NextDouble() * 80) - 40;
            double y = (rng.NextDouble() * 60) - 45;
            double len = 1 + (rng.NextDouble() * 14);
            walls[i] = rng.Next(3) switch
            {
                0 => new SurfaceCollision.Segment(x, y, x + len, y),
                1 => new SurfaceCollision.Segment(x, y, x, y + len),
                _ => new SurfaceCollision.Segment(x, y, x + len, y + (len * 0.6)),
            };
        }
        return walls;
    }

    [Fact]
    public void Blocked_AnswersExactlyWhatTheFullSweepAnswers_OverAGridOfProbes()
    {
        // A dense lattice of standing spots across (and well outside) the ground: for every one of them the
        // indexed verdict must equal the swept verdict. This is the law a Reever's body depends on.
        foreach (SurfaceCollision.Segment[] ground in Grounds())
        {
            SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);
            for (int i = -50; i <= 50; i++)
            {
                for (int j = -50; j <= 50; j++)
                {
                    double x = i * 0.93, y = j * 0.71;
                    Assert.Equal(
                        SurfaceCollision.Blocked(x, y, Radius, (IReadOnlyList<SurfaceCollision.Segment>)ground),
                        SurfaceCollision.Blocked(x, y, Radius, index));
                }
            }
        }
    }

    [Fact]
    public void Blocked_AgreesOnTheHairsBreadthCasesRightAgainstEveryWall()
    {
        // The verdicts that matter are the ones a hair either side of "touching". Walk each wall's own line
        // and stand at exactly a radius, and a whisker inside and outside it, on both flanks.
        foreach (SurfaceCollision.Segment[] ground in Grounds())
        {
            SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);
            foreach (SurfaceCollision.Segment w in ground)
            {
                double dx = w.X2 - w.X1, dy = w.Y2 - w.Y1;
                double len = System.Math.Sqrt((dx * dx) + (dy * dy));
                double nx = len > 0 ? -dy / len : 1, ny = len > 0 ? dx / len : 0;
                for (int t = 0; t <= 8; t++)
                {
                    double px = w.X1 + (dx * t / 8.0), py = w.Y1 + (dy * t / 8.0);
                    foreach (double d in new[] { -Radius - 1e-6, -Radius, -Radius + 1e-6, 0, Radius - 1e-6, Radius, Radius + 1e-6 })
                    {
                        double qx = px + (nx * d), qy = py + (ny * d);
                        Assert.Equal(
                            SurfaceCollision.Blocked(qx, qy, Radius, (IReadOnlyList<SurfaceCollision.Segment>)ground),
                            SurfaceCollision.Blocked(qx, qy, Radius, index));
                    }
                }
            }
        }
    }

    [Fact]
    public void HasLineOfSight_AnswersExactlyWhatTheFullSweepAnswers_AcrossThousandsOfLooks()
    {
        // Sightlines of every length and angle, including ones that start or end inside the stone. A grid
        // that skipped a cell would quietly hand a Reever sight through a slab.
        var rng = new System.Random(20260726);
        foreach (SurfaceCollision.Segment[] ground in Grounds())
        {
            SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);
            for (int i = 0; i < 4000; i++)
            {
                double ax = (rng.NextDouble() * 100) - 50, ay = (rng.NextDouble() * 100) - 50;
                double bx = (rng.NextDouble() * 100) - 50, by = (rng.NextDouble() * 100) - 50;
                Assert.Equal(
                    SurfaceCollision.HasLineOfSight(ax, ay, bx, by, (IReadOnlyList<SurfaceCollision.Segment>)ground),
                    SurfaceCollision.HasLineOfSight(ax, ay, bx, by, index));
            }
        }
    }

    [Fact]
    public void HasLineOfSight_AgreesOnAxisAlignedAndZeroLengthLooks()
    {
        // The degenerate looks: a watcher staring at its own feet, and perfectly horizontal/vertical
        // sightlines — the cases a cell-walk is most likely to get wrong at a boundary.
        foreach (SurfaceCollision.Segment[] ground in Grounds())
        {
            SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);
            for (int i = -30; i <= 30; i++)
            {
                double v = i * 1.1;
                foreach ((double ax, double ay, double bx, double by) look in new[]
                {
                    (-45.0, v, 45.0, v),   // straight across
                    (v, -45.0, v, 45.0),   // straight up
                    (v, v, v, v),          // no look at all
                    (v, v, v + 0.001, v),  // a hair of a look
                })
                {
                    Assert.Equal(
                        SurfaceCollision.HasLineOfSight(look.ax, look.ay, look.bx, look.by, (IReadOnlyList<SurfaceCollision.Segment>)ground),
                        SurfaceCollision.HasLineOfSight(look.ax, look.ay, look.bx, look.by, index));
                }
            }
        }
    }

    [Fact]
    public void Slide_LandsOnTheVerySAMESpot_SoAWholeChaseIsUnchanged()
    {
        // Positions, not just verdicts: run a pack of Old Ones through a long chase on the indexed ground
        // and on the swept ground and require the two runs to stay bit-identical. Determinism is law here —
        // a fast road to a different position would be a different game.
        foreach (SurfaceCollision.Segment[] ground in Grounds())
        {
            SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);
            for (int contact = 0; contact < 24; contact++)
            {
                double sweptX = -30 + (contact * 2.3), sweptY = -34 + (contact * 1.7);
                double fastX = sweptX, fastY = sweptY;
                int hand = (contact % 2 == 0) ? 1 : -1;
                for (int step = 0; step < 200; step++)
                {
                    double targetX = 12 * System.Math.Cos(step * 0.05);
                    double targetY = 9 * System.Math.Sin(step * 0.05);
                    (sweptX, sweptY) = ReeverChase.Step(sweptX, sweptY, targetX, targetY, 0.31, 1e6,
                        (IReadOnlyList<SurfaceCollision.Segment>)ground, Radius, hand);
                    (fastX, fastY) = ReeverChase.Step(fastX, fastY, targetX, targetY, 0.31, 1e6,
                        index, Radius, hand);
                    Assert.Equal(sweptX, fastX);
                    Assert.Equal(sweptY, fastY);
                }
            }
        }
    }

    [Fact]
    public void KeepApartAndTheGuns_ReadTheIndexedGroundExactlyAsTheySweptIt()
    {
        // The other two readers of the wall law: the pack's elbows (#441) and the sentries' shot (#437).
        SurfaceCollision.Segment[] ground = MixedGround();
        SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);

        var swept = new (double X, double Y)[8];
        var fast = new (double X, double Y)[8];
        for (int i = 0; i < swept.Length; i++)
        {
            swept[i] = fast[i] = (0.2 * i, 0.1 * i); // a deliberate blob, right on the slab
        }
        ReeverPack.KeepApart(swept, (IReadOnlyList<SurfaceCollision.Segment>)ground, Radius);
        ReeverPack.KeepApart(fast, index, Radius);
        for (int i = 0; i < swept.Length; i++)
        {
            Assert.Equal(swept[i].X, fast[i].X);
            Assert.Equal(swept[i].Y, fast[i].Y);
        }

        for (int i = -25; i <= 25; i++)
        {
            for (int j = -25; j <= 25; j++)
            {
                Assert.Equal(
                    SentryBot.CanEngage(-9, -6, i * 1.3, j * 1.1, (IReadOnlyList<SurfaceCollision.Segment>)ground),
                    SentryBot.CanEngage(-9, -6, i * 1.3, j * 1.1, index));
            }
        }
    }

    [Fact]
    public void AnIndexIsJustAWallListWithAMap_SoEveryOtherReaderSeesTheSameStone()
    {
        // The index is handed to callers that simply ITERATE the walls (the renderer's own readers). It must
        // present exactly the segments it was built from, in order — it is the same list, only better filed.
        SurfaceCollision.Segment[] ground = MixedGround();
        SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);

        Assert.Equal(ground.Length, index.Count);
        for (int i = 0; i < ground.Length; i++)
        {
            Assert.Equal(ground[i], index[i]);
        }
        Assert.Equal(ground, index.ToList());
    }

    [Fact]
    public void OpenGroundIndexesToOpenGround()
    {
        // No walls at all is the commonest ground there is (the ship before a tube, a bare haven room), and
        // it must stay the cheap nothing every caller relies on.
        foreach (SurfaceCollision.WallIndex empty in new[]
                 { SurfaceCollision.WallIndex.Build(null), SurfaceCollision.WallIndex.Build([]) })
        {
            Assert.Empty(empty);
            Assert.False(SurfaceCollision.Blocked(3, 4, Radius, empty));
            Assert.True(SurfaceCollision.HasLineOfSight(-9, -9, 9, 9, empty));
            Assert.True(SentryBot.CanEngage(0, 0, 5, 5, empty));
            // #724 · both gaits, because an empty index must be the same nothing to a person and to an
            // Old One — the gait decides what happens at a wall, and here there is none.
            foreach (SurfaceCollision.Gait gait in
                     new[] { SurfaceCollision.Gait.Person, SurfaceCollision.Gait.Stagger })
            {
                (double x, double y) = SurfaceCollision.Slide(1, 2, 0.5, -0.5, Radius, empty, gait);
                Assert.Equal(1.5, x, 12);
                Assert.Equal(1.5, y, 12);
            }
        }
    }

    [Fact]
    public void AGroundFarLargerThanTheGridStillAnswersTruthfully()
    {
        // A site is free to seed whatever geometry it likes (#320), including one spread far wider than the
        // grid's own cell budget. The index then takes coarser cells — never more of them — and the answers
        // must survive that entirely.
        var huge = new SurfaceCollision.Segment[]
        {
            new(-5000, -5000, 5000, -5000),
            new(-5000, 5000, 5000, 5000),
            new(0, -20, 0, 20),
            new(-3000, 100, -2990, 100),
        };
        SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(huge);
        var rng = new System.Random(448);
        for (int i = 0; i < 3000; i++)
        {
            double ax = (rng.NextDouble() * 12000) - 6000, ay = (rng.NextDouble() * 12000) - 6000;
            double bx = (rng.NextDouble() * 12000) - 6000, by = (rng.NextDouble() * 12000) - 6000;
            Assert.Equal(
                SurfaceCollision.HasLineOfSight(ax, ay, bx, by, (IReadOnlyList<SurfaceCollision.Segment>)huge),
                SurfaceCollision.HasLineOfSight(ax, ay, bx, by, index));
            Assert.Equal(
                SurfaceCollision.Blocked(ax, ay, Radius, (IReadOnlyList<SurfaceCollision.Segment>)huge),
                SurfaceCollision.Blocked(ax, ay, Radius, index));
        }
    }

    [Fact]
    public void TheRealSeededGrounds_AnswerIdentically_WallForWall()
    {
        // Not a made-up ground: the actual per-body geographies a landing site lays down (#320), read
        // through the same field envelope the surface uses. If any real site's stone disagreed, this is
        // where it would show.
        var field = new SurfaceLayout.Field(-44, 34, -20, -84, -27, -6, -70);
        var rng = new System.Random(3130320);
        foreach (string body in new[] { "miranda", "luna", "phobos", "europa", "titan", "enceladus" })
        {
            foreach (string salt in new[] { "", "site-1", "site-2" })
            {
                SurfaceLayout.Plan plan = SurfaceLayout.For(body, field, salt);
                var segs = new SurfaceCollision.Segment[plan.Walls.Count];
                for (int i = 0; i < segs.Length; i++)
                {
                    SurfaceLayout.Wall w = plan.Walls[i];
                    segs[i] = new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2);
                }
                SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(segs);
                for (int i = 0; i < 2000; i++)
                {
                    double ax = (rng.NextDouble() * 90) - 48, ay = (rng.NextDouble() * 70) - 88;
                    double bx = (rng.NextDouble() * 90) - 48, by = (rng.NextDouble() * 70) - 88;
                    Assert.Equal(
                        SurfaceCollision.Blocked(ax, ay, Radius, (IReadOnlyList<SurfaceCollision.Segment>)segs),
                        SurfaceCollision.Blocked(ax, ay, Radius, index));
                    Assert.Equal(
                        SurfaceCollision.HasLineOfSight(ax, ay, bx, by, (IReadOnlyList<SurfaceCollision.Segment>)segs),
                        SurfaceCollision.HasLineOfSight(ax, ay, bx, by, index));
                }
            }
        }
    }

    [Fact]
    public void TheIndexIsBOUNDED_NotMerelyFaster_AsTheGroundGrowsStonier()
    {
        // The point of the whole exercise (#448): "a surface step whose cost scales with walls × Reevers
        // will drift back into this." So pin the CEILING itself rather than a millisecond figure that would
        // rot on the next machine — how many walls a step actually measures, as the ground gets 16× stonier.
        // Sweeping, a step measures every wall there is; indexed, it measures only its own neighbourhood.
        foreach ((int walls, int ceiling) in new[] { (32, 8), (128, 16), (512, 48) })
        {
            SurfaceCollision.Segment[] ground = SeededGround(seed: 1, count: walls);
            SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(ground);

            long total = 0;
            int probes = 0, worst = 0;
            for (int i = -20; i <= 20; i++)
            {
                for (int j = -20; j <= 20; j++)
                {
                    int n = index.CandidatesNear(i * 1.9, j * 1.4, Radius);
                    total += n;
                    worst = System.Math.Max(worst, n);
                    probes++;
                }
            }
            double average = (double)total / probes;
            Assert.True(average <= ceiling,
                $"a step on a {walls}-wall ground measured {average:F1} walls on average — the ceiling is {ceiling}");
            Assert.True(worst < walls,
                $"a step on a {walls}-wall ground still measured all {worst} of them somewhere");
        }
    }

    [Fact]
    public void AStepOnlyEverMeasuresItsOwnCornerOfTheGround_HoweverMuchStoneThereIs()
    {
        // The ceiling said as the FRACTION the issue actually cares about. Sweeping, every step measures
        // 100% of the ground — so doubling a site's stone doubles the frame, for every Old One on it.
        // Indexed, a step measures only the walls in its own neighbourhood, and that stays a couple of
        // percent of the ground at every size: the frame stops caring how much a landing site seeded.
        foreach (int walls in new[] { 32, 64, 128, 256, 512 })
        {
            SurfaceCollision.WallIndex index = SurfaceCollision.WallIndex.Build(SeededGround(seed: 1, count: walls));
            long total = 0;
            int probes = 0;
            for (int i = -20; i <= 20; i++)
            {
                for (int j = -20; j <= 20; j++)
                {
                    total += index.CandidatesNear(i * 1.9, j * 1.4, Radius);
                    probes++;
                }
            }
            double fraction = (double)total / probes / walls;
            Assert.True(fraction < 0.05,
                $"a step on a {walls}-wall ground measured {fraction:P1} of it — the sweep it replaces measured 100%");
        }
    }

    private static IEnumerable<SurfaceCollision.Segment[]> Grounds()
    {
        yield return [new SurfaceCollision.Segment(0, -5, 0, 5)];
        yield return MixedGround();
        yield return SeededGround(seed: 448, count: 40);
        yield return SeededGround(seed: 1313, count: 200);
    }
}
