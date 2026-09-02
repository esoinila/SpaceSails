using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 · THE TREADMILL'S LAWS, each one measured.
///
/// <para>Owner, on what the metaphor is for and where it stops: <i>"it is a bit like this Virtual reality
/// Carpet ... you can walk in any direction on it but actually you can stand still in the room you play
/// in"</i> — but on a real treadmill the floor is a belt and where you have been is gone, and here it must
/// not be. Every guard below defends one clause of that, and every one of them was watched go RED with the
/// fix reverted before it was allowed to stay.</para>
/// </summary>
public class TheTreadmillTests
{
    private static readonly string[] Bodies = ["miranda", "luna", "phobos", "europa", "titan", "ganymede"];

    private static IEnumerable<(string Body, string Salt)> Sites()
    {
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                yield return (body, site.LayoutSalt);
            }
        }
    }

    // ── LAW 1 · ADDRESSED, NEVER RECYCLED ────────────────────────────────────────────────────────────────

    /// <summary>THE GROUND YOU WALKED AWAY FROM IS STILL THERE. Generate a tile, throw every trace of it
    /// away, generate it again from its address alone, and compare byte for byte.
    ///
    /// <para>This is the property the whole issue turns on and the one whose loss would be invisible: nothing
    /// crashes when a tile comes back different, the world just quietly becomes wallpaper and the
    /// buried-treasure loop breaks in a way that reads as a save bug. So it is measured on the SERIALISED
    /// SHAPE of the tile — every wall endpoint, every flag, every doorway, every terrain stroke — rather than
    /// on a count or a hash of a subset, because a guard that compares wall COUNTS passes happily on a tile
    /// whose walls have all moved.</para></summary>
    [Fact]
    public void ATileGeneratedTwice_ComesBackByteIdentical()
    {
        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                string first = Fingerprint(body, salt, a);
                // Nothing is held between the two calls: the second regeneration knows only the address.
                string second = Fingerprint(body, salt, a);
                Assert.True(first == second,
                    $"{body}/{salt} tile ({a.X}, {a.Y}) regenerated differently.\n" +
                    $"  first : {Head(first)}\n  second: {Head(second)}");
            }
        }
    }

    /// <summary>…AND IT IS THE ADDRESS THAT DECIDES, not the visit. Two different tiles must not hand back
    /// the same ground — otherwise "unbounded" is one field wallpapered outward, which is exactly the
    /// treadmill-belt failure wearing the fix's clothes.
    ///
    /// <para><b>Measured on the tile's LOCAL shape</b>, with its own corner subtracted off, and that is the
    /// whole point of the guard rather than a detail of it. Compared in world coordinates it passes on a
    /// generator that seeds every tile identically and merely translates the result — which is precisely the
    /// wallpaper it exists to catch. It was written that way first and stayed GREEN with the seed
    /// deliberately broken; a guard handed a world that cannot tell pass from fail is a known bug class
    /// here.</para></summary>
    [Fact]
    public void TwoDifferentTiles_AreDifferentGround()
    {
        foreach ((string body, string salt) in Sites())
        {
            var seen = new Dictionary<string, SurfaceTiles.Address>();
            foreach (SurfaceTiles.Address a in Spread())
            {
                string print = LocalFingerprint(body, salt, a);
                Assert.False(seen.TryGetValue(print, out SurfaceTiles.Address other),
                    $"{body}/{salt}: tiles ({a.X}, {a.Y}) and ({other.X}, {other.Y}) are the same ground " +
                    "laid twice — the lattice is wallpaper.");
                seen[print] = a;
            }
        }
    }

    /// <summary>THE GROUND UNDER THE TUBE DID NOT MOVE. Tile (0, 0) must be exactly what
    /// <see cref="SurfaceLayout.For"/> has always laid — same walls, same doorways, same terrain — because
    /// every pinned picture in the client's ledger was taken standing on it. If this reddens, the canon
    /// ground changed, and that is a bug rather than a re-pin.</summary>
    [Fact]
    public void TheHomeTile_IsTheGroundTheGameAlwaysLaid()
    {
        foreach ((string body, string salt) in Sites())
        {
            Assert.Equal(
                Shape(SurfaceLayout.For(body, SurfaceLayout.DefaultField, salt)),
                Shape(SurfaceTiles.Ground(body, salt, SurfaceTiles.Home)));
            Assert.Equal(
                Strokes(SurfaceScenery.For(body, salt, SurfaceLayout.DefaultField)),
                Strokes(SurfaceTiles.Terrain(body, salt, SurfaceTiles.Home)));
        }
    }

    /// <summary>NO TILE'S GROUND REACHES INTO A NEIGHBOUR'S. Two tiles are generated with no knowledge of
    /// each other, so a feature that crossed a seam could be laid straight through the neighbour's — the
    /// merged-structures failure the owner has already caught once (<i>"check this structure out... it
    /// functions but is kind of funny"</i>), except invisible until somebody walked the boundary.
    ///
    /// <para>The invariant is NOT "nothing crosses the line" — a rotated building's corner has always been
    /// allowed to poke a couple of du past the span its centre was placed in, on the home tile as much as
    /// anywhere. It is that <b>nothing crosses far enough to reach the band the neighbour keeps clear</b>: the
    /// generator lays features inside <see cref="SurfaceLayout.EdgeMargin"/> of each flank and four du of the
    /// bottom, so an overhang smaller than that can only ever land on bare regolith. The tolerances below are
    /// those two numbers and nothing else — which is why this reddens if either is ever tightened.</para></summary>
    [Fact]
    public void NoTilesGroundReachesIntoItsNeighbours()
    {
        // The narrowest clear band a neighbour keeps against a shared seam. Sideways it is the edge margin;
        // vertically it is the four du the seeded generator keeps above a tile's own bottom edge.
        const double SideSlack = SurfaceLayout.EdgeMargin;
        const double EndSlack = 4.0;

        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                (double left, double right, double bottom, double top) = SurfaceTiles.Rect(a);
                foreach (SurfaceLayout.Wall w in SurfaceTiles.Ground(body, salt, a).Walls)
                {
                    bool inside =
                        w.X1 >= left - SideSlack && w.X1 <= right + SideSlack &&
                        w.X2 >= left - SideSlack && w.X2 <= right + SideSlack &&
                        w.Y1 >= bottom - EndSlack && w.Y1 <= top + EndSlack &&
                        w.Y2 >= bottom - EndSlack && w.Y2 <= top + EndSlack;
                    Assert.True(inside,
                        $"{body}/{salt} tile ({a.X}, {a.Y}) x[{left:F1},{right:F1}] y[{bottom:F1},{top:F1}]: " +
                        $"wall ({w.X1:F1},{w.Y1:F1})-({w.X2:F1},{w.Y2:F1}) reaches into the next tile.");
                }
            }
        }
    }

    // ── LAW 2 · WHAT THE CAPTAIN CHANGED IS KEYED BY TILE ────────────────────────────────────────────────

    /// <summary>ONE LOCKER, ONE ID. The hut interactables' claim keys carry the tile address, so emptying the
    /// locker in one hut cannot empty every locker on the moon — and the HOME tile keeps the exact id it has
    /// always had, so nothing a save already remembers is forgotten.</summary>
    [Fact]
    public void AHutsConsoleIds_AreKeyedByTile()
    {
        var ids = new HashSet<string>();
        foreach (SurfaceTiles.Address a in Spread())
        {
            foreach (string what in new[] { "ammo", "effects" })
            {
                Assert.True(ids.Add(SurfaceOutpost.ConsoleId("miranda", "", a, what)),
                    $"two tiles share the '{what}' id — spending one would spend both.");
            }
        }

        Assert.Equal("outpost:miranda::ammo",
            SurfaceOutpost.ConsoleId("miranda", "", SurfaceTiles.Home, "ammo"));
        Assert.Equal("outpost:miranda::effects",
            SurfaceOutpost.ConsoleId("miranda", "", SurfaceTiles.Home, "effects"));
    }

    // ── LAW 3 · THE TIDE RISES AROUND THE CAPTAIN ────────────────────────────────────────────────────────

    /// <summary>THE TIDE FOLLOWS THE CAPTAIN AND HAS NO OPINION ABOUT DEPTH.
    ///
    /// <para>Two halves, and the second is the one that matters. The spawns must sit on a ring around
    /// WHEREVER THE CAPTAIN IS (there is no rim to rise from any more) — and that ring must be ISOTROPIC, or
    /// #453's deleted y-graded danger has crept back in through the spawner. Owner: <i>"Let's not have any
    /// don't venture too far set-up by y-coordinate."</i></para>
    ///
    /// <para>The world can tell pass from fail here: a spawner that still rose from the bottom rim would put
    /// every contact in the two southern quadrants and fail the second assertion by a mile, and one that
    /// ignored the captain would fail the first for any captain not standing at the origin.</para></summary>
    [Fact]
    public void TheTide_RisesOnAnIsotropicRingAroundTheCaptain()
    {
        double ring = ReeverTide.SpawnRingDu(SurfaceLayout.DefaultField);
        Assert.InRange(ring, 100.0, 220.0);   // the distance the old bottom rim actually gave

        foreach ((double cx, double cy) in new[] { (-7.0, -27.0), (120.0, -250.0), (-410.0, -430.0) })
        {
            var quadrant = new int[4];
            for (int i = 0; i < 800; i++)
            {
                (double x, double y) = ReeverTide.SpawnAround(0xC0FFEEUL, i, cx, cy, ring);

                double dx = x - cx, dy = y - cy;
                Assert.Equal(ring, Math.Sqrt((dx * dx) + (dy * dy)), 6);

                quadrant[(dx >= 0 ? 0 : 2) + (dy >= 0 ? 0 : 1)]++;
            }

            // Even over the whole circle. A quarter of 800 is 200; a spawner with any depth bias in it lands
            // nowhere near this band, and one that rose from a rim would put 800 of them in two quadrants.
            foreach (int n in quadrant)
            {
                Assert.InRange(n, 140, 260);
            }
        }
    }

    // ── LAW 4 · A HUT PER TILE, SOMEWHERE OUT THERE ──────────────────────────────────────────────────────

    /// <summary>HUTS ARE AS COMMON AS THEY EVER WERE, PER UNIT OF GROUND. A tile is exactly the old field, so
    /// the old three-in-four per SITE is three-in-four per TILE and nothing about how often a captain meets
    /// one has changed. Measured over many tiles rather than asserted from the constant, because the
    /// placement can now refuse a tile that is too full and that refusal is part of the real rate.</summary>
    [Fact]
    public void HutsKeepTheirRarity_PerAreaOfGround()
    {
        int tiles = 0, huts = 0;
        foreach ((string body, string salt) in Sites())
        {
            foreach (SurfaceTiles.Address a in Spread())
            {
                tiles++;
                if (SurfaceOutpost.ForTile(body, salt, a).HasOutpost)
                {
                    huts++;
                }
            }
        }

        double rate = huts / (double)tiles;
        Assert.InRange(rate, 0.55, 0.85);   // nominal 0.75, less whatever the ground refuses
    }

    /// <summary>AND THEY ARE NOT IN A LANE. The hut used to be pinned against the field's own boundary at a
    /// fixed depth into the edge margin; there is no boundary now, so a placement that still hugged one would
    /// be a hut queueing along an invisible line. Measured as SPREAD: the x of the hatches across a body's
    /// tiles has to cover most of the width of the ground, not two columns of it.</summary>
    [Fact]
    public void HutsAreNotLinedUpAgainstAnEdge()
    {
        foreach ((string body, string salt) in Sites())
        {
            var offsets = new List<double>();
            foreach (SurfaceTiles.Address a in Spread())
            {
                SurfaceOutpost.Placement p = SurfaceOutpost.ForTile(body, salt, a, forcePresent: true);
                if (!p.HasOutpost)
                {
                    continue;
                }
                (double left, double right, double _, double _) = SurfaceTiles.Rect(a);
                offsets.Add((p.DoorX - left) / (right - left));   // 0 at the port edge, 1 at starboard
            }

            Assert.True(offsets.Count >= 8, $"{body}/{salt}: too few hatches to measure a spread.");

            // NOT min-to-max: a placer that alternates between the two edge lanes MAXIMISES that, and this
            // guard duly stayed green with the old edge-lane placement pasted back in. What separates "spread
            // across the ground" from "queued along two lines" is how many land in the MIDDLE of a tile,
            // which an edge lane can never do — by construction rather than by luck.
            int middle = offsets.Count(o => o > 0.25 && o < 0.75);
            Assert.True(middle * 4 >= offsets.Count,
                $"{body}/{salt}: only {middle} of {offsets.Count} hatches stand anywhere but the flanks — " +
                "they are in a lane.");
        }
    }

    /// <summary>AND NONE OF THEM IS ON THE DOORSTEP. A hut is something a captain plans a route to; one
    /// standing in the landing lights is furniture. Home tile only, because it is the only tile with a tube
    /// on it.</summary>
    [Fact]
    public void NoHutStandsWithinSightOfTheTube()
    {
        (double hx, double hy) = SurfaceTiles.TubeMouth();

        // TWO HUNDRED SEEDS, not the two dozen sites the game happens to offer. The ground a hut may stand on
        // is some 66,000 du² and the doorstep this forbids is under a tenth of it, so across the real sites
        // the standoff catches roughly two placements — and "roughly two" is a coin toss, not a measurement.
        // With the check deliberately removed the guard stayed GREEN on the real sites; it reddens on this
        // sweep. That is the difference between a world that can tell pass from fail and one that cannot.
        int nearMisses = 0;
        for (int i = 0; i < 200; i++)
        {
            string salt = $"treadmill-seed-{i}";
            SurfaceOutpost.Placement p =
                SurfaceOutpost.ForTile("miranda", salt, SurfaceTiles.Home, forcePresent: true);
            if (!p.HasOutpost)
            {
                continue;
            }
            double dx = p.DoorX - hx, dy = p.DoorY - hy;
            double d = Math.Sqrt((dx * dx) + (dy * dy));
            Assert.True(d >= SurfaceOutpost.TubeStandoffDu,
                $"seed {salt}: a hut stands {d:F1} du from the tube, " +
                $"inside the {SurfaceOutpost.TubeStandoffDu:F0} du standoff.");
            if (d < SurfaceOutpost.TubeStandoffDu * 2.0)
            {
                nearMisses++;
            }
        }

        // …and the sweep really does put huts near the tube, or the assertion above never had anything to
        // judge. The "prove the world can fail" half, stated rather than assumed.
        Assert.True(nearMisses > 0,
            "no seed in the sweep placed a hut anywhere near the tube — the standoff was never tested.");
    }

    // ── LAW 6 · CHUNKED STREAMING ────────────────────────────────────────────────────────────────────────

    /// <summary>THE WORLD IS RE-WELDED ON A TILE CROSSING, NOT ON A STEP.
    ///
    /// <para>This is the guard with a real performance trap behind it. <c>DeckPlan</c> rebuilds
    /// <see cref="SurfaceCollision.WallIndex"/> whenever its walls change, so a stream that appended per step
    /// would rebuild a spatial index over a thousand segments on every frame — and it would look and play
    /// exactly the same until somebody's shuttle ride timed out, which is a thing that has happened to this
    /// game's owner already.</para>
    ///
    /// <para>So the number is asserted rather than the feeling: walking three thousand deck units in
    /// one-du steps costs one rebuild per boundary crossed, and the crossings are counted independently from
    /// the geometry rather than read off the same counter.</para></summary>
    [Fact]
    public void ALongStraightWalk_CostsOneRebuildPerTileCrossing()
    {
        var stream = new SurfaceStream();
        (double x, double y) = SurfaceTiles.TubeMouth();
        y -= 30.0;

        SurfaceTiles.Address was = SurfaceTiles.At(x, y);
        int crossings = 0, steps = 0;

        stream.Step(x, y);   // the first weld: the excursion has to start on ground
        for (int i = 0; i < 3000; i++)
        {
            x -= 1.0;
            steps++;
            SurfaceTiles.Address now = SurfaceTiles.At(x, y);
            if (now != was)
            {
                crossings++;
                was = now;
            }
            stream.Step(x, y);
        }

        Assert.Equal(crossings, stream.Crossings);
        Assert.Equal(crossings + 1, stream.Rebuilds);   // +1 for the initial weld
        Assert.True(crossings >= 9, $"a 3,000 du walk crossed only {crossings} tiles — the lattice is wrong.");
        Assert.True(stream.Rebuilds * 100 < steps,
            $"{stream.Rebuilds} rebuilds over {steps} steps is per-step streaming in disguise.");
    }

    /// <summary>WALK AWAY, WALK BACK, FIND THE SAME GROUND. The chunk evicts what the captain left and the
    /// tile comes back from its address — this is law 1 exercised through the thing that actually does the
    /// forgetting.</summary>
    [Fact]
    public void GroundEvictedByTheChunk_ComesBackTheSame()
    {
        var stream = new SurfaceStream();
        (double x, double y) = SurfaceTiles.TubeMouth();
        y -= 30.0;

        stream.Step(x, y);
        string before = Fingerprint("miranda", "", SurfaceTiles.Home);

        var evicted = new List<SurfaceTiles.Address>();
        var added = new List<SurfaceTiles.Address>();
        bool everEvictedHome = false;
        for (int i = 0; i < 1200; i++)
        {
            x -= 1.0;
            stream.Step(x, y, added, evicted);
            everEvictedHome |= evicted.Contains(SurfaceTiles.Home);
        }
        Assert.True(everEvictedHome, "the walk never got far enough to forget the ground it started on.");
        Assert.DoesNotContain(SurfaceTiles.Home, stream.Loaded);

        while (x < SurfaceTiles.TubeMouth().X)
        {
            x += 1.0;
            stream.Step(x, y);
        }
        Assert.Contains(SurfaceTiles.Home, stream.Loaded);
        Assert.Equal(before, Fingerprint("miranda", "", SurfaceTiles.Home));
    }

    // ── LAW 7 · THE BACKSTOP ─────────────────────────────────────────────────────────────────────────────

    /// <summary>THE BOUND IS PAST WHERE THE AIR RUNS OUT, TWICE OVER.
    ///
    /// <para>The wandering bound used to be the edge of the world; it is a backstop now, and the whole
    /// requirement is that no ordinary excursion ever meets it. The number is not a taste — it is a full tank
    /// at a walking pace — so this asserts the RELATION rather than the value: a captain who leaves the tube
    /// with a full suit crosses the point of no return (<see cref="SuitAir.PastPointOfNoReturn"/>) at less
    /// than half the distance, and dies of the walk home long before geometry gets a say.</para></summary>
    [Fact]
    public void TheBackstop_SitsFarPastThePointOfNoReturn()
    {
        double noReturn = 0.0;
        for (double d = 0.0; d < SurfaceTiles.BackstopRadiusDu; d += 10.0)
        {
            // What is left in the tank after walking d out, and whether the walk home is still affordable.
            double left = SuitAir.TankSeconds - (d / SuitAir.WalkSpeedDu);
            if (SuitAir.PastPointOfNoReturn(left, d))
            {
                noReturn = d;
                break;
            }
        }

        Assert.True(noReturn > 0.0, "a captain can walk to the backstop and still get home — the tank is a lie.");
        Assert.True(SurfaceTiles.BackstopRadiusDu > noReturn * 2.0,
            $"the backstop is at {SurfaceTiles.BackstopRadiusDu:F0} du and the walk home stops being " +
            $"affordable at {noReturn:F0} du — that is close enough to meet.");
    }

    /// <summary>THE BACKSTOP IS CLOSED, AND IT IS NOT A CIRCLE. Closed because a gap in it is a captain
    /// walking out of the world; not a circle because the owner's ruling on the old bound —
    /// <i>"But the limit to movement is still a box here?"</i> — is a ruling about shapes the eye can name,
    /// and a circle is the most nameable of all.</summary>
    [Fact]
    public void TheBackstop_IsClosedAndIsNotACircle()
    {
        foreach ((string body, string salt) in Sites())
        {
            double first = SurfaceEdge.BackstopRadiusAt(body, salt, 0.0);
            Assert.Equal(first, SurfaceEdge.BackstopRadiusAt(body, salt, Math.Tau), 6);

            var radii = new List<double>();
            for (int i = 0; i < 180; i++)
            {
                double r = SurfaceEdge.BackstopRadiusAt(body, salt, i * Math.Tau / 180.0);
                Assert.True(r > 0.0);
                radii.Add(r);
            }
            double wander = (radii.Max() - radii.Min()) / SurfaceTiles.BackstopRadiusDu;
            Assert.True(wander > 0.02, $"{body}/{salt}: the backstop wanders {wander:P1} — that is a circle.");
        }
    }

    /// <summary>AND IT STOPS YOU. A point well past the wander is beyond it from every bearing; a point well
    /// inside is beyond it from none. The cheap early-out inside <see cref="SurfaceEdge.BeyondBackstop"/> is
    /// exactly the kind of optimisation that answers the wrong question when the constants drift.</summary>
    [Fact]
    public void TheBackstop_StopsYouFromEveryBearing()
    {
        double r = SurfaceTiles.BackstopRadiusDu;
        (double cx, double cy) = SurfaceTiles.TubeMouth();

        foreach ((string body, string salt) in Sites())
        {
            for (int i = 0; i < 72; i++)
            {
                double b = i * Math.Tau / 72.0;
                double outside = r * (1.0 + SurfaceEdge.BackstopWanderFraction + 0.01);
                double inside = r * (1.0 - SurfaceEdge.BackstopWanderFraction - 0.01);

                Assert.True(SurfaceEdge.BeyondBackstop(
                    body, salt, cx + (outside * Math.Cos(b)), cy + (outside * Math.Sin(b))));
                Assert.False(SurfaceEdge.BeyondBackstop(
                    body, salt, cx + (inside * Math.Cos(b)), cy + (inside * Math.Sin(b))));
            }
        }
    }

    // ── the shared measuring tools ───────────────────────────────────────────────────────────────────────

    /// <summary>The tiles every guard sweeps: the home tile and a spread of neighbours in every direction the
    /// world actually has (the lattice does not run up through the ship).</summary>
    private static IEnumerable<SurfaceTiles.Address> Spread()
    {
        for (int dx = -3; dx <= 3; dx++)
        {
            for (int dy = -3; dy <= 0; dy++)
            {
                yield return new SurfaceTiles.Address(dx, dy);
            }
        }
    }

    /// <summary>One tile's whole ground, written out — geometry AND weather. Every number that could move.</summary>
    private static string Fingerprint(string body, string salt, SurfaceTiles.Address a) =>
        Shape(SurfaceTiles.Ground(body, salt, a)) + "\n" + Strokes(SurfaceTiles.Terrain(body, salt, a));

    /// <summary>The same, with the tile's own corner subtracted off — the SHAPE of the ground rather than
    /// where it happens to sit. Two tiles seeded from one key have identical local shapes and different world
    /// ones, so this is the only form of the question that can catch wallpaper.</summary>
    private static string LocalFingerprint(string body, string salt, SurfaceTiles.Address a)
    {
        (double left, double _, double bottom, double _) = SurfaceTiles.Rect(a);
        SurfaceLayout.Plan p = SurfaceTiles.Ground(body, salt, a);

        string walls = string.Join(";", p.Walls.Select(w =>
            $"{w.X1 - left:F6},{w.Y1 - bottom:F6},{w.X2 - left:F6},{w.Y2 - bottom:F6}," +
            $"{(w.IsHull ? 1 : 0)},{(w.Unseen ? 1 : 0)}"));
        string doors = string.Join(";", (p.Doorways ?? []).Select(d =>
            $"{d.X1 - left:F6},{d.Y1 - bottom:F6},{d.X2 - left:F6},{d.Y2 - bottom:F6}"));
        string marks = string.Join(";", p.Landmarks.Select(m =>
            $"{m.X - left:F6},{m.Y - bottom:F6},{m.Label}"));
        string weather = string.Join(";", SurfaceTiles.Terrain(body, salt, a).Select(m =>
            $"{m.X1 - left:F6},{m.Y1 - bottom:F6},{m.X2 - left:F6},{m.Y2 - bottom:F6},{(int)m.Of}"));

        return $"{p.Scheme}|{walls}|{doors}|{marks}|{weather}";
    }

    private static string Shape(SurfaceLayout.Plan p) =>
        p.Scheme + "|" +
        string.Join(";", p.Walls.Select(w =>
            $"{w.X1:F6},{w.Y1:F6},{w.X2:F6},{w.Y2:F6},{(w.IsHull ? 1 : 0)},{(w.Unseen ? 1 : 0)}")) + "|" +
        string.Join(";", (p.Doorways ?? []).Select(d => $"{d.X1:F6},{d.Y1:F6},{d.X2:F6},{d.Y2:F6}")) + "|" +
        string.Join(";", (p.BuildingFootprints ?? []).Select(f => $"{f.X:F6},{f.Y:F6},{f.R:F6}")) + "|" +
        string.Join(";", p.Landmarks.Select(m => $"{m.X:F6},{m.Y:F6},{m.Label}"));

    private static string Strokes(IReadOnlyList<SurfaceScenery.Mark> marks) =>
        string.Join(";", marks.Select(m => $"{m.X1:F6},{m.Y1:F6},{m.X2:F6},{m.Y2:F6},{(int)m.Of}"));

    private static string Head(string s) => s.Length <= 120 ? s : s[..120] + "…";
}
