namespace SpaceSails.Core.Tests;

/// <summary>
/// #442 slice 1 · The barrier invariant — one wall list, four consumers, randomized (owner, 2026-07-26:
/// "Maybe a test where you test that the walls as barrier truly match one to one with physical barrier as
/// part of CI testing… The wall there should be randomized and tested with all three, character movement,
/// reever movement and shot passing, etc, reever visibility." … "As part of CI suite tests that is.").
///
/// <para>Three defects in one session were all the same defect wearing different clothes — a system that
/// forgot to ask the walls: #324 (Reevers walked and saw through stone), #435 (a Reever pinned on the face
/// of a slab and the chase died there), #437 (the sentries shot through the maze). Each was found by the
/// owner, in play, one at a time. This file is the net that turns that whole family from "someone notices
/// in a playtest" into "the build goes red": a seeded random wall field, and every consumer of "is there a
/// wall here" interrogated against the SAME one list.</para>
///
/// <para><b>Every failure prints its seed.</b> A red line here is reproducible by hand from the number in
/// the message — that is the whole point of randomizing rather than hand-placing slabs.</para>
///
/// <para>Deliberately NOT covered here: the renderer. <c>DeckView</c>'s fog <c>continue</c> can drop a
/// barrier from the draw while collision still honours it, which is the divergence #442 names last and a
/// LATER slice fixes — it lives in the client and cannot be reached from a Core test. This net locks the
/// four Core readers together first, so the refactor that moves the walls has something holding it
/// honest.</para>
/// </summary>
public class BarrierInvariantTests
{
    /// <summary>The captain's body — <c>DeckPlan.AvatarRadius</c>, restated because it lives in the client.
    /// The Old Ones are given the same radius on the surface (<c>Map.Surface.StepReevers</c>), which is the
    /// point: one body size, one wall list.</summary>
    private const double Radius = 0.7;

    /// <summary>#724 · These invariants are stated about the CAPTAIN's gait, deliberately: it is the one
    /// with the extra move in it, so it is the harder claim. An Old One's step is the same primitive with
    /// the doorway sidestep withheld, and that it withholds it — and withholds nothing else — is pinned in
    /// SurfaceCollisionTests.</summary>
    private const SurfaceCollision.Gait Boots = SurfaceCollision.Gait.Person;

    /// <summary>The longest step a Reever can ever take in one frame: <c>ReeverSpeed</c> 5.6 du/s against
    /// the client's <c>Math.Min(dt, 0.1)</c> clamp. It matters that this is under the body's DIAMETER
    /// (1.4 du) — a mover that cannot travel its own width in a step cannot pass through a slab without
    /// standing inside it first, and standing inside it is what <c>Blocked</c> refuses.</summary>
    private const double MaxReeverStep = 0.56;

    /// <summary>Half-width of the generated ground, in deck units — roughly a landing field's spread.</summary>
    private const double FieldHalf = 20.0;

    // ── The seeded wall-field generator ──────────────────────────────────────────────────────────────

    /// <summary>Build one randomized ground from a seed: slabs at dead-horizontal, dead-vertical, the 45°
    /// diagonal and arbitrary angles, some hinged into CORNERS (a shared endpoint — the classic place a
    /// "solid" and a "drawn" disagree by half a radius), some doubled into narrow PARALLEL corridors (where
    /// #435's axis-separated stall lived), and one slab worn down to near-nothing. Seeded so any failure is
    /// reproducible from the number alone.</summary>
    private static SurfaceCollision.Segment[] WallField(int seed)
    {
        var rng = new Random(seed);
        int slabs = 4 + rng.Next(9);
        var walls = new List<SurfaceCollision.Segment>(slabs * 2);
        for (int i = 0; i < slabs; i++)
        {
            double x = Span(rng), y = Span(rng);
            double length = 0.4 + (rng.NextDouble() * 14.0);
            double angle = rng.Next(4) switch
            {
                0 => 0,                                 // dead horizontal
                1 => Math.PI / 2,                       // dead vertical
                2 => Math.PI / 4,                       // the 45° diagonal — where axis-separation is worst
                _ => rng.NextDouble() * Math.PI,        // any angle at all
            };
            double ex = x + (Math.Cos(angle) * length), ey = y + (Math.Sin(angle) * length);
            walls.Add(new SurfaceCollision.Segment(x, y, ex, ey));

            if (rng.Next(4) == 0)
            {
                // Hinge a second slab on the first one's far end: a genuine corner, so the sweep samples
                // the inside and outside of a right angle and not just open faces.
                double turn = angle + (rng.Next(2) == 0 ? Math.PI / 2 : -Math.PI / 2);
                double armLength = 0.4 + (rng.NextDouble() * 8.0);
                walls.Add(new SurfaceCollision.Segment(
                    ex, ey, ex + (Math.Cos(turn) * armLength), ey + (Math.Sin(turn) * armLength)));
            }
            if (rng.Next(5) == 0)
            {
                // A parallel twin a hair off the first — a corridor, sometimes narrower than a body, which
                // is exactly the geometry that ground a Reever's whole step away on the blocked axis.
                double offset = 0.05 + (rng.NextDouble() * 2.5);
                double ox = -Math.Sin(angle) * offset, oy = Math.Cos(angle) * offset;
                walls.Add(new SurfaceCollision.Segment(x + ox, y + oy, ex + ox, ey + oy));
            }
        }

        // One near-degenerate slab per field. It is NOT zero-length — see the dedicated test below for why
        // exactly-zero is its own case — but it is short enough that it acts as a bare point of stone, and
        // every consumer must still handle it without dividing by its own length.
        double px = Span(rng), py = Span(rng);
        walls.Add(new SurfaceCollision.Segment(px, py, px + 1e-7, py - 1e-7));
        return [.. walls];
    }

    private static double Span(Random rng) => ((rng.NextDouble() * 2) - 1) * FieldHalf;

    /// <summary>Pick a point to interrogate. A quarter of the time it is a segment ENDPOINT, a quarter a
    /// midpoint, a quarter a hair off an endpoint — because uniform noise almost never lands on the corners
    /// and endpoints where the consumers are most likely to part company.</summary>
    private static (double X, double Y) Sample(Random rng, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SurfaceCollision.Segment w = walls[rng.Next(walls.Count)];
        return rng.Next(4) switch
        {
            0 => rng.Next(2) == 0 ? (w.X1, w.Y1) : (w.X2, w.Y2),
            1 => ((w.X1 + w.X2) / 2, (w.Y1 + w.Y2) / 2),
            2 => (w.X1 + (Span(rng) * 1e-3), w.Y1 + (Span(rng) * 1e-3)),
            _ => (Span(rng), Span(rng)),
        };
    }

    // ── The one shared assertion the whole sweep calls ───────────────────────────────────────────────

    /// <summary>The reusable barrier assertion for a watcher/shooter at (ax, ay) and a target at (bx, by)
    /// against one wall list. It hunts for DISAGREEMENT: a pair where one consumer says "barrier" and
    /// another says "clear".
    ///
    /// <para>Sight and shot are the same primitive and must never drift apart again — #437 happened because
    /// <c>SentryBot</c> had its own idea of reach (pure distance) while the Old Ones already asked
    /// <c>HasLineOfSight</c>. So the volley's actual behaviour is pinned against sight-and-range EXACTLY,
    /// in both directions, including the no-shot/no-drain half: a gun that cannot see does not spend.</para></summary>
    private static void AssertBarrierAgreement(
        int seed, string field, IReadOnlyList<SurfaceCollision.Segment> walls,
        double ax, double ay, double bx, double by)
    {
        string where = $"seed {seed} [{field}] a=({ax:R},{ay:R}) b=({bx:R},{by:R})";

        bool sight = SurfaceCollision.HasLineOfSight(ax, ay, bx, by, walls);

        // A look is a look from either end: the same stone must break it both ways, or a Reever and the
        // captain watching each other would disagree about who is hidden. (The orientation predicate is
        // exact arithmetic, so this holds outright except in the one configuration where a wall's endpoint
        // sits ON the sightline to within a whisker of double precision — there the sign of a determinant of
        // size ~1e-16 is evaluation-order dependent. No body can ever be there: Blocked keeps every mover a
        // full 0.7 du off the stone, and the sweep does not manufacture that collinearity on purpose.)
        Assert.True(sight == SurfaceCollision.HasLineOfSight(bx, by, ax, ay, walls),
            $"sight is not symmetric — {where}");

        bool inRange = SentryBot.InRange(ax, ay, bx, by);
        Assert.True(SentryBot.CanEngage(ax, ay, bx, by, walls) == (inRange && sight),
            $"CanEngage disagrees with range-and-sight — {where}");

        // The volley itself, not just the predicate: a full bot alone against one target must fire exactly
        // when it can engage, and must burn NOTHING when it cannot (the #437 law).
        SentryBot.Volley v = SentryBot.Step(
            [new SentryBot.Deployed("K-77", ax, ay, SentryBot.MaxMagazine)],
            [new SentryBot.Target(bx, by, 0)],
            walls);
        bool fired = v.Shots == 1;
        Assert.True(fired == (inRange && sight),
            $"the gun and the eye disagree (fired={fired}, sight={sight}, inRange={inRange}) — {where}");
        Assert.True(v.Bots[0].Rounds == SentryBot.MaxMagazine - (fired ? 1 : 0),
            $"a round was spent without a shot (or a shot without a round) — {where}");
        Assert.True(v.Reevers[0].HitsTaken == (fired ? 1 : 0),
            $"the target's hits disagree with the shot — {where}");
    }

    // ── The sweep ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SightAndShot_NeverDisagree_AcrossSeededWallFields()
    {
        // The #437 regression net, generalized off the single hand-placed slab in SentryBotTests: 250
        // randomized grounds, 60 interrogated pairs each, endpoints and corners over-sampled. If anyone ever
        // gives the guns their own geometry again, this goes red with the seed to reproduce it.
        for (int seed = 1; seed <= 250; seed++)
        {
            SurfaceCollision.Segment[] walls = WallField(seed);
            var rng = new Random(seed + 500_000);
            for (int i = 0; i < 60; i++)
            {
                (double ax, double ay) = Sample(rng, walls);
                (double bx, double by) = Sample(rng, walls);
                AssertBarrierAgreement(seed, "random pair", walls, ax, ay, bx, by);
            }
        }
    }

    [Fact]
    public void SightAndShot_Agree_WhenBothStandOnASegmentEndpointOrCorner()
    {
        // Endpoints and hinged corners are where a "solid" and a "drawn" classically part company by half a
        // radius (#442). Here the pair is placed exactly ON the stone — both ends of every segment, and the
        // shared corner points — so the intersection test is asked its hardest question: a sightline whose
        // own endpoint lies on the wall.
        for (int seed = 1001; seed <= 1100; seed++)
        {
            SurfaceCollision.Segment[] walls = WallField(seed);
            var rng = new Random(seed);
            for (int i = 0; i < walls.Length; i++)
            {
                SurfaceCollision.Segment u = walls[i];
                SurfaceCollision.Segment w = walls[(i + 1) % walls.Length];
                AssertBarrierAgreement(seed, "endpoint↔endpoint", walls, u.X1, u.Y1, w.X2, w.Y2);
                AssertBarrierAgreement(seed, "endpoint↔far endpoint", walls, u.X2, u.Y2, w.X1, w.Y1);
                // …and a hair off it, since "on the line" and "just beside it" must not be two different
                // worlds. The nudges are drawn independently per coordinate: a SHARED offset vector would
                // put both ends of the sightline and the wall's endpoint on one line by construction, which
                // is the one arrangement where the exact predicate has nothing left to be exact about.
                AssertBarrierAgreement(seed, "beside the endpoint", walls,
                    u.X1 + (Span(rng) * 1e-3), u.Y1 + (Span(rng) * 1e-3),
                    w.X2 + (Span(rng) * 1e-3), w.Y2 + (Span(rng) * 1e-3));
            }
        }
    }

    [Fact]
    public void SightAndShot_Agree_ForAShooterRingedAllTheWayRoundASlab()
    {
        // The #437 pair, generalized: the same shooter/target pair swung through every angle, once with the
        // slab standing between them and once with it shifted aside. The slab spans the origin, and every
        // pair is diametrically opposed through the origin, so the answer is knowable by hand — which is the
        // point of pinning it: this is the one case where we assert what the truth IS, not merely that the
        // four readers agree on it.
        SurfaceCollision.Segment[] between = [new SurfaceCollision.Segment(0, -6, 0, 6)];
        SurfaceCollision.Segment[] aside = [new SurfaceCollision.Segment(60, -6, 60, 6)];

        for (int i = 0; i < 48; i++)
        {
            double theta = i * Math.PI * 2 / 48;
            double ax = Math.Cos(theta) * 8, ay = Math.Sin(theta) * 8;
            double bx = -ax, by = -ay;

            AssertBarrierAgreement(i, "ring, slab between", between, ax, ay, bx, by);
            AssertBarrierAgreement(i, "ring, slab aside", aside, ax, ay, bx, by);

            // Every pair is opposed through the origin and the slab spans the origin, so the stone is
            // genuinely between them at every angle on the ring — the look must die at all 48 of them.
            Assert.False(SurfaceCollision.HasLineOfSight(ax, ay, bx, by, between),
                $"the slab between failed to break the look at angle index {i}");
            Assert.True(SurfaceCollision.HasLineOfSight(ax, ay, bx, by, aside),
                $"a slab 60 du away broke a look it is nowhere near, at angle index {i}");
        }

        // …and out past the arc, a clear line is still no shot: range and sight are AND, never OR.
        Assert.False(SentryBot.CanEngage(0, 0, SentryBot.RangeDeckUnits + 1, 0, aside),
            "a bot engaged past its own arc on a clear line");

        // Exactly parallel is its own case (#442 lists it), and it needs EXACT coordinates — a ring angle of
        // π/2 is only parallel to within 1e-16, and the intersection test is exact, so it still crosses. Laid
        // out by hand: a pair standing on the slab's own line, and a pair on a line beside it. In neither does
        // the stone stand BETWEEN them, so in neither is the look broken — and the guns agree, as ever.
        AssertBarrierAgreement(-1, "collinear with the slab", between, 0, 8, 0, -8);
        AssertBarrierAgreement(-2, "parallel beside the slab", between, 2, 8, 2, -8);
        Assert.True(SurfaceCollision.HasLineOfSight(0, 8, 0, -8, between),
            "a look straight down the slab's own line is not a look through it");
        Assert.True(SurfaceCollision.HasLineOfSight(2, 8, 2, -8, between),
            "a look parallel to the slab never crosses it");
    }

    [Fact]
    public void Sight_BrokenByStone_MeansTheBootCannotWalkThatLine()
    {
        // The cross-primitive claim, and the one that would actually catch a wall list read by one consumer
        // and not another: if a look between two points is broken, then somewhere on that straight line a
        // body of the captain's radius stands INSIDE stone. Sight is an exact ray and collision is a disc,
        // so they can never be equal — but a broken look must always be a blocked walk, or the eye and the
        // boot are reading different walls. Sampled finer than the radius, so a crossing cannot be stepped
        // over.
        for (int seed = 2001; seed <= 2150; seed++)
        {
            SurfaceCollision.Segment[] walls = WallField(seed);
            var rng = new Random(seed + 500_000);
            for (int i = 0; i < 30; i++)
            {
                (double ax, double ay) = Sample(rng, walls);
                (double bx, double by) = Sample(rng, walls);
                if (SurfaceCollision.HasLineOfSight(ax, ay, bx, by, walls))
                {
                    continue;
                }
                double length = Math.Sqrt(((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay)));
                int steps = Math.Max(2, (int)Math.Ceiling(length / (Radius * 0.5)));
                bool stoodInStone = false;
                for (int k = 0; k <= steps && !stoodInStone; k++)
                {
                    double t = (double)k / steps;
                    stoodInStone = SurfaceCollision.Blocked(
                        ax + ((bx - ax) * t), ay + ((by - ay) * t), Radius, walls);
                }
                Assert.True(stoodInStone,
                    $"seed {seed}: a wall broke the look from ({ax:R},{ay:R}) to ({bx:R},{by:R}) " +
                    "but no point on that line was blocked — the eye and the boot are reading different walls");
            }
        }
    }

    [Fact]
    public void Slide_RefusesAnAxis_ExactlyWhenBlockedCallsItStone()
    {
        // The captain's boots. Bump-and-slide is axis-separated, and its refusal must be nothing more nor
        // less than what Blocked says about the candidate spot: X is tried from where you stand, then Y is
        // tried from the X-resolved spot. Anything else — a refusal with no wall, or a wall with no refusal
        // — is the movement reader drifting away from the wall list.
        //
        // #724 · WITH ONE STATED EXCEPTION, and it is stated here rather than quietly excluded. When the
        // split carries NEITHER axis the step is not thrown away any more: the body shuffles along the face
        // toward a doorway if one is within reach, which is the whole of the fix for a captain pinned on a
        // jamb. That case is the ONLY licence — the axis answers are still exactly Blocked's answers
        // whenever an axis moved at all — and the shuffle is held to its own three laws below.
        for (int seed = 3001; seed <= 3150; seed++)
        {
            SurfaceCollision.Segment[] walls = WallField(seed);
            var rng = new Random(seed + 500_000);
            for (int i = 0; i < 40; i++)
            {
                (double x, double y) = Sample(rng, walls);
                double dx = Span(rng) / FieldHalf * Radius;   // a per-frame nudge, never longer than a body
                double dy = Span(rng) / FieldHalf * Radius;

                (double nx, double ny) = SurfaceCollision.Slide(x, y, dx, dy, Radius, walls, Boots);

                bool xIsStone = SurfaceCollision.Blocked(x + dx, y, Radius, walls);
                double splitX = xIsStone ? x : x + dx;
                bool yIsStone = SurfaceCollision.Blocked(splitX, y + dy, Radius, walls);
                double splitY = yIsStone ? y : y + dy;

                if (splitX != x || splitY != y)
                {
                    Assert.True(nx == splitX,
                        $"seed {seed}: the X axis was {(xIsStone ? "not " : "")}refused with Blocked saying " +
                        $"{xIsStone} at ({x + dx:R},{y:R})");
                    Assert.True(ny == splitY,
                        $"seed {seed}: the Y axis was {(yIsStone ? "not " : "")}refused with Blocked saying " +
                        $"{yIsStone} at ({splitX:R},{y + dy:R})");
                    continue;
                }

                // Both axes refused. Either the body held, or it shuffled — and a shuffle must be
                // PERPENDICULAR to the press (it is the free hand, never a shortcut along the way you were
                // already going), NO LONGER than the press (redirected, never faster), and OUT OF THE STONE
                // like every other destination this primitive ever returns.
                if (nx == x && ny == y)
                {
                    continue;
                }
                double mx = nx - x, my = ny - y;
                Assert.False(SurfaceCollision.Blocked(nx, ny, Radius, walls),
                    $"seed {seed}: the sidestep from ({x:R},{y:R}) put the body inside stone at ({nx:R},{ny:R})");
                Assert.True(Math.Sqrt((mx * mx) + (my * my)) <= Math.Sqrt((dx * dx) + (dy * dy)) + 1e-9,
                    $"seed {seed}: the sidestep from ({x:R},{y:R}) covered more ground than the press asked for");
                Assert.True(Math.Abs((mx * dx) + (my * dy)) < 1e-9,
                    $"seed {seed}: the sidestep from ({x:R},{y:R}) was not perpendicular to the press " +
                    $"({dx:R},{dy:R}) — it moved ({mx:R},{my:R})");
            }
        }
    }

    [Fact]
    public void Slide_FromClearGround_NeverEndsInsideStone_NorCrossesASpan()
    {
        // The invariant the captain actually cares about: start out of the stone and you finish out of the
        // stone, and the ground you covered getting there was never on the far side of a wall. The second
        // half reuses HasLineOfSight on the mover's OWN chord — the tidiest statement of #442 there is, since
        // it makes the movement reader and the sight reader answer about the same segments in the same call.
        for (int seed = 4001; seed <= 4150; seed++)
        {
            SurfaceCollision.Segment[] walls = WallField(seed);
            var rng = new Random(seed + 500_000);
            for (int i = 0; i < 40; i++)
            {
                (double x, double y) = Sample(rng, walls);
                if (SurfaceCollision.Blocked(x, y, Radius, walls))
                {
                    continue;   // already wedged in stone (an endpoint sample) — nothing is promised from there
                }
                double dx = Span(rng) / FieldHalf * Radius, dy = Span(rng) / FieldHalf * Radius;

                (double nx, double ny) = SurfaceCollision.Slide(x, y, dx, dy, Radius, walls, Boots);

                Assert.False(SurfaceCollision.Blocked(nx, ny, Radius, walls),
                    $"seed {seed}: a boot slid from clear ground ({x:R},{y:R}) into stone at ({nx:R},{ny:R})");
                Assert.True(SurfaceCollision.HasLineOfSight(x, y, nx, ny, walls),
                    $"seed {seed}: a boot's own step from ({x:R},{y:R}) to ({nx:R},{ny:R}) crossed a wall");
            }
        }
    }

    [Fact]
    public void Reever_WalksTheSameStone_AsTheCaptainsBoots_OverSeededFields()
    {
        // The Old Ones, on the SAME segments and the SAME radius (#324: "let's not let the Reevers move
        // through walls here"). Run whole chases — both handrail hands, since #435's fix gave the hand a
        // fixed side per contact and a pack splits on it — and assert every single frame lands outside the
        // stone. The crew-only barrier is parked far above the field so this test is about walls and only
        // walls; the door's own law is pinned in SurfaceCollisionTests.
        const double barrierY = 1e6;
        for (int seed = 5001; seed <= 5100; seed++)
        {
            SurfaceCollision.Segment[] walls = WallField(seed);
            var rng = new Random(seed + 500_000);
            foreach (int hand in new[] { 1, -1 })
            {
                for (int run = 0; run < 3; run++)
                {
                    (double rx, double ry) = (Span(rng), Span(rng));
                    if (SurfaceCollision.Blocked(rx, ry, Radius, walls))
                    {
                        continue;
                    }
                    double avatarX = Span(rng), avatarY = Span(rng);
                    double step = 0.05 + (rng.NextDouble() * (MaxReeverStep - 0.05));
                    for (int frame = 0; frame < 120; frame++)
                    {
                        (double nx, double ny) = ReeverChase.Step(
                            rx, ry, avatarX, avatarY, step, barrierY, walls, Radius, hand);
                        Assert.False(SurfaceCollision.Blocked(nx, ny, Radius, walls),
                            $"seed {seed} hand {hand}: an Old One stepped INTO stone at ({nx:R},{ny:R}) " +
                            $"from ({rx:R},{ry:R}) on frame {frame} (step {step:R})");
                        (rx, ry) = (nx, ny);
                    }
                }
            }
        }
    }

    [Fact]
    public void Reever_StepShorterThanItsOwnBody_NeverCrossesASlabsSpan()
    {
        // The no-tunnelling law, restated as a property over random ground: #435's honest invariant was "the
        // slab's span is never crossed — the far side is only ever reached around its ends". A step is
        // checked against the wall list at its ENDPOINT, so the guarantee rests on a mover never being able
        // to travel its own diameter in one frame; the client's ReeverSpeed (5.6 du/s) against its
        // Math.Min(dt, 0.1) clamp tops out at 0.56 du against a 1.4 du body, a 2.5× margin. Generated steps
        // stay inside that real ceiling, and the chord of every frame must read as a clear sightline through
        // the same walls — the mover and the eye agreeing, once more, about one list.
        const double barrierY = 1e6;
        for (int seed = 6001; seed <= 6100; seed++)
        {
            SurfaceCollision.Segment[] walls = WallField(seed);
            var rng = new Random(seed + 500_000);
            for (int run = 0; run < 6; run++)
            {
                (double rx, double ry) = (Span(rng), Span(rng));
                if (SurfaceCollision.Blocked(rx, ry, Radius, walls))
                {
                    continue;
                }
                // Aim straight through the field so plenty of these runs are driven HEAD-ON into a face —
                // the approach that stalled the chase in #435 and the one most likely to clip.
                double avatarX = -rx, avatarY = -ry;
                double step = 0.05 + (rng.NextDouble() * (MaxReeverStep - 0.05));
                Assert.True(step < Radius * 2, "the generated step must stay under a body's diameter");
                for (int frame = 0; frame < 150; frame++)
                {
                    (double nx, double ny) = ReeverChase.Step(
                        rx, ry, avatarX, avatarY, step, barrierY, walls, Radius, (run % 2 == 0) ? 1 : -1);
                    Assert.True(SurfaceCollision.HasLineOfSight(rx, ry, nx, ny, walls),
                        $"seed {seed}: an Old One crossed a slab's span in one step, ({rx:R},{ry:R}) → " +
                        $"({nx:R},{ny:R}) on frame {frame} (step {step:R})");
                    (rx, ry) = (nx, ny);
                }
            }
        }
    }

    [Fact]
    public void HeadOn_Glancing_AndParallel_Approaches_AllEndOutsideTheStone()
    {
        // #442 names the three approaches by hand because they fail differently: head-on spends the whole
        // step on the blocked axis (#435's stall), a glancing run has to keep the along-wall component, and
        // an exactly-parallel run must not be refused at all. One slab, both movers, every angle around the
        // circle with a seeded jitter on the start so the geometry is never accidentally perfect.
        SurfaceCollision.Segment[] slab = [new SurfaceCollision.Segment(0, -8, 0, 8)];
        const double barrierY = 1e6;
        var rng = new Random(4423);

        for (int i = 0; i < 144; i++)
        {
            double theta = i * Math.PI * 2 / 144;
            double startX = -4 + (rng.NextDouble() * 0.5), startY = ((rng.NextDouble() * 16) - 8);
            double targetX = startX + (Math.Cos(theta) * 12), targetY = startY + (Math.Sin(theta) * 12);

            // The captain, hand on the stick, driving at the face at this angle.
            (double cx, double cy) = (startX, startY);
            for (int frame = 0; frame < 60; frame++)
            {
                double len = Math.Sqrt(((targetX - cx) * (targetX - cx)) + ((targetY - cy) * (targetY - cy)));
                if (len < 1e-9)
                {
                    break;
                }
                (double px, double py) = (cx, cy);
                (cx, cy) = SurfaceCollision.Slide(
                    cx, cy, (targetX - cx) / len * 0.35, (targetY - cy) / len * 0.35, Radius, slab, Boots);
                Assert.False(SurfaceCollision.Blocked(cx, cy, Radius, slab),
                    $"angle {i}: a captain approaching at {theta:R} rad ended inside the slab at ({cx:R},{cy:R})");
                // #435's honest law, stated per frame: the span is never crossed. Reaching the far side is
                // allowed — that is the maze costing the long way round — but only ever AROUND an end, which
                // is exactly "no step's own chord crosses the stone".
                Assert.True(SurfaceCollision.HasLineOfSight(px, py, cx, cy, slab),
                    $"angle {i}: a captain crossed the slab's span, ({px:R},{py:R}) → ({cx:R},{cy:R})");
            }

            // …and the Old One at the same angle, which additionally gets to grab the wall as a handrail.
            (double rx, double ry) = (startX, startY);
            for (int frame = 0; frame < 60; frame++)
            {
                (double px, double py) = (rx, ry);
                (rx, ry) = ReeverChase.Step(rx, ry, targetX, targetY, 0.35, barrierY, slab, Radius,
                    (i % 2 == 0) ? 1 : -1);
                Assert.False(SurfaceCollision.Blocked(rx, ry, Radius, slab),
                    $"angle {i}: an Old One approaching at {theta:R} rad ended inside the slab at ({rx:R},{ry:R})");
                Assert.True(SurfaceCollision.HasLineOfSight(px, py, rx, ry, slab),
                    $"angle {i}: an Old One crossed the slab's span, ({px:R},{py:R}) → ({rx:R},{ry:R})");
            }
        }
    }

    [Fact]
    public void ZeroLengthSegment_IsAPointOfStoneToBodies_AndNothingAtAllToSight()
    {
        // #442 asks for zero-length segments explicitly: "they should either be excluded everywhere or block
        // everywhere — never one each way." Today they are one each way, and this pins the exact split so it
        // is a KNOWN shape rather than a surprise a refactor discovers: a zero-length segment is a point
        // obstacle to a body (DistanceToSegment falls back to the single endpoint, so it stops a boot within
        // a radius of it) and is invisible to a sightline (the orientation test needs strictly opposite
        // sides of the wall's line, and a wall with no length has no sides).
        //
        // It stays harmless only while no ground actually emits one — <c>DeckPlan.CollisionSegments</c> is a
        // one-for-one copy of the drawn walls, so a collapsed wall would reach both readers and be answered
        // differently by each. If a refactor ever produces one, this test already says which way each
        // consumer will jump, and #442's later slice is where the two get made to agree.
        SurfaceCollision.Segment[] dot = [new SurfaceCollision.Segment(0, 0, 0, 0)];

        Assert.True(SurfaceCollision.Blocked(0.5, 0, Radius, dot));      // the boot stops on it
        Assert.False(SurfaceCollision.Blocked(0.8, 0, Radius, dot));     // …but only within a body of it
        Assert.Equal(0.0, SurfaceCollision.DistanceToSegment(0, 0, 0, 0, 0, 0), 12);

        Assert.True(SurfaceCollision.HasLineOfSight(-3, 0, 3, 0, dot));  // the eye passes straight through
        Assert.True(SentryBot.CanEngage(-3, 0, 3, 0, dot));              // and so, therefore, does the round

        // The consequence worth naming: a Reever cannot be pushed onto the dot, but it can be SHOT across it.
        // Everywhere the two primitives disagree, they disagree in exactly this direction.
        (double nx, double ny) = ReeverChase.Step(-1.0, 0, 3, 0, 0.35, 1e6, dot, Radius);
        Assert.False(SurfaceCollision.Blocked(nx, ny, Radius, dot));
    }

    [Fact]
    public void EmptyAndNullWallLists_ReadTheSameToEveryConsumer()
    {
        // Open regolith is the default every caller relies on (the walls parameter was added as optional so
        // every pre-#324/#437 call site kept its behaviour). Null and empty must be the same nothing to all
        // four readers, or "no walls yet" would mean different things to the boots, the Old Ones and the guns.
        foreach (IReadOnlyList<SurfaceCollision.Segment>? none in
                 new IReadOnlyList<SurfaceCollision.Segment>?[] { null, Array.Empty<SurfaceCollision.Segment>() })
        {
            Assert.False(SurfaceCollision.Blocked(3, 4, Radius, none));
            Assert.True(SurfaceCollision.HasLineOfSight(-9, -9, 9, 9, none));
            Assert.True(SentryBot.CanEngage(0, 0, 5, 5, none));
            // #724 · open ground is open ground for the whole cast: the gait only ever decides what
            // happens at a wall, so with no walls at all both answers must be the plain step.
            Assert.Equal((4.0, 6.0),
                SurfaceCollision.Slide(3, 4, 1, 2, Radius, none, SurfaceCollision.Gait.Person));
            Assert.Equal((4.0, 6.0),
                SurfaceCollision.Slide(3, 4, 1, 2, Radius, none, SurfaceCollision.Gait.Stagger));
            Assert.Equal(
                ReeverChase.Step(-5, -5, 5, 5, 0.4, 1e6),
                ReeverChase.Step(-5, -5, 5, 5, 0.4, 1e6, none, Radius));
        }
    }
}
