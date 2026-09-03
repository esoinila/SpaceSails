using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #563 · THE GROUND IS WALKABLE — every body, every landing site, proved by A* rather than by eye.
///
/// <para>The wrecks have had this since #497 (<c>WreckLayoutTests</c>): a hull is not allowed to generate
/// geometry that seals a compartment off, and <see cref="DeckReachability"/> walks every one of them in CI.
/// Landing sites — which are OLDER than the wrecks and are seeded rather than authored — had no such audit
/// at all. A seeded site could quietly wall the deep off from the tube and nothing would fail; you would only
/// find out by flying there and pressing W.</para>
///
/// <para><b>#563 · THE AUDIT CHANGES SHAPE WITH THE WORLD.</b> It used to be a flood of one fenced rectangle,
/// and it could be, because the rectangle was the world. The ground is an unbounded lattice of addressed
/// tiles now (<see cref="SurfaceTiles"/>), so "flood the whole field" is not a question that has an answer.
/// The law it becomes is the one the decision comment named: <b>no sealed pocket within N tiles of the
/// tube</b> — still provable, still a flood, different bounds.</para>
///
/// <para>Two things follow from the change and both are deliberate. Ground that touches the EDGE of the
/// audited region is exempt, because a captain can walk round it through ground this audit did not lay; and
/// the audited region is the tube's own tile plus every tile touching it (<see cref="SurfaceTiles.Chunk"/>),
/// which is exactly the ground a captain can be standing on at once.</para>
///
/// <para>The predicate is the same one the live avatar moves by (<see cref="DeckReachability.Standable"/>
/// wraps <see cref="SurfaceCollision.Blocked"/>), so the audit and the game agree by construction — and it is
/// handed a <see cref="SurfaceCollision.WallIndex"/>, which is the same list answering the same question and
/// is the only reason a region six times the old one costs less than the old one did.</para>
/// </summary>
// #251 · NO LONGER SLOW-GATED. It held the tag at 142 s, and 142 s was the cost of asking every one of
// four million collision questions against an unindexed List<Segment>. #563 hands the flood a
// SurfaceCollision.WallIndex — the same walls, the same answers, filed into a grid — and audits SIX tiles
// instead of one for 2 s in Release. A gate is a budget, and this one no longer spends anything.
public class SurfaceReachabilityTests
{
    // The shared field envelope MoonSurface hands in — mirrors its constants, the same way
    // SurfaceLayoutTests.Env does, so the test lays exactly the ground the client lays.
    // #573 · READ, never copied. This was a hand-made duplicate of the client's constants, so when the
    // field grew sixteenfold the game shipped a 310 x 260 du world while this audit went on flooding the old
    // 78 x 64 one — and passing. A sealed building with consoles in it shipped straight past it.
    private static readonly SurfaceLayout.Field Env = SurfaceLayout.DefaultField;

    private const double AvatarRadius = 0.7;   // DeckPlan.AvatarRadius — the captain's own body
    private const double TubeCenterX = -7.0;   // MoonSurface.TubeCenterX / SpawnX
    private const double TubeLeft = -9.0;      // MoonSurface.TubeLeft
    private const double TubeRight = -5.0;     // MoonSurface.TubeRight

    // Every landable body the game can put a captain on. Miranda and Luna are authored; the rest are seeded.
    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];

    // Where the captain actually arrives: just below the tube mouth, on the landing band.
    private static DeckReachability.Point Spawn => new(TubeCenterX, Env.TopY - 2.0);

    /// <summary>The tiles audited: the tube's own, and every tile touching it. See the class note.</summary>
    private static IReadOnlyList<SurfaceTiles.Address> Ring => SurfaceTiles.Chunk(SurfaceTiles.Home);

    /// <summary>The flood's step. Coarser than <see cref="DeckReachability.DefaultStep"/> because the audited
    /// region is six times the ground it used to be — and still SOUND, which is the only thing that matters:
    /// two cells this far apart cannot have a wall between them when each is <see cref="AvatarRadius"/> clear
    /// of every wall, because a segment lying between them would be within 0.5 du of one of them. Anything up
    /// to twice the radius is safe by that argument; this is comfortably under it.</summary>
    private const double Step = 1.0;

    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds
    {
        get
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (SurfaceTiles.Address a in Ring)
            {
                (double l, double r, double b, double t) = SurfaceTiles.Rect(a);
                minX = System.Math.Min(minX, l);
                maxX = System.Math.Max(maxX, r);
                minY = System.Math.Min(minY, b);
                maxY = System.Math.Max(maxY, t);
            }
            return (minX, minY, maxX, maxY);
        }
    }

    /// <summary>The collidable ground for one site across the audited ring: the down-tube's two walls, the
    /// ship's own underside, everything every tile's geography generated, the northern rim of each top-row
    /// tile, and every hut FORCED OPEN.
    ///
    /// <para>The field's outer bound is not here any more, and its absence is the point of the whole lane:
    /// it was three unseen-but-solid runs that made the ground finite, and it is a far backstop
    /// (<see cref="SurfaceEdge.BeyondBackstop"/>, ten thousand du out) that this audit could not reach if it
    /// tried.</para></summary>
    private static SurfaceCollision.WallIndex WallsFor(string body, string salt)
    {
        var walls = new List<SurfaceCollision.Segment>
        {
            // The tube walls — the captain squeezes out between them.
            new(TubeLeft, -14, TubeLeft, Env.TopY),
            new(TubeRight, -14, TubeRight, Env.TopY),
            // The top rim, port and starboard of the tube mouth (the ship's own underside).
            new(Env.LeftX, Env.TopY, TubeLeft, Env.TopY),
            new(TubeRight, Env.TopY, Env.RightX, Env.TopY),
        };

        foreach (SurfaceTiles.Address a in Ring)
        {
            walls.AddRange(SurfaceTiles.Ground(body, salt, a).Walls
                .Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)));

            if (SurfaceTiles.NorthRim(a) is { } rim)
            {
                walls.Add(new SurfaceCollision.Segment(rim.X1, rim.Y1, rim.X2, rim.Y2));
            }

            // #563 · The hut on this tile, FORCED — the worst case for connectivity, because a hut that is
            // still shut is only a console and has no walls at all. `forcePresent` puts one on EVERY tile
            // rather than the seeded three-in-four: the audit should hold for the tiles that have one, and
            // asking it of all of them is strictly harder.
            SurfaceOutpost.Placement hut = SurfaceOutpost.ForTile(body, salt, a, forcePresent: true);
            if (!hut.HasOutpost)
            {
                continue;
            }
            walls.AddRange(SurfaceOutpost.Build(body, salt, hut).Walls
                .Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)));
        }

        // #448 · the SAME walls, filed into a grid. The audit asks millions of collision questions and every
        // one of them used to measure every wall on the ground.
        return SurfaceCollision.WallIndex.Build(walls);
    }

    /// <summary>NO STANDABLE GROUND IS SEALED OFF, anywhere within a tile of the tube. Flood from the tube
    /// mouth and compare what the walk reaches against every cell the captain could physically stand on.
    ///
    /// <para>This is the honest form of the question, and the reason it is a flood rather than a list of
    /// probe points: sampling named spots (a landmark, the deep anchor, a corner) mostly measures whether
    /// the tester's arithmetic happened to miss a wall. It red-flagged Miranda's maze on the first run
    /// purely because a sample sat exactly on a gapped row, and it says nothing at all about the ground
    /// between the samples. A flood asks the thing that actually matters — is there anywhere a captain
    /// could stand and never walk to — and it names the pocket when there is.</para></summary>
    [Fact]
    public void EveryLandingSite_LeavesNoStandableGroundSealedOff()
    {
        var pockets = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SurfaceCollision.WallIndex walls = WallsFor(body, site.LayoutSalt);
                (int biggest, (double X, double Y)? at) = LargestSealedPocket(walls);

                if (biggest * CellArea > MaxFixtureInteriorArea)
                {
                    string where = at is { } s ? $" around ({s.X:F1}, {s.Y:F1})" : "";
                    pockets.Add(
                        $"{body}/{site.Name}: a sealed pocket of {biggest} cells " +
                        $"({biggest * CellArea:F1} du²){where} cannot be walked to from the tube");
                }
            }
        }

        Assert.True(pockets.Count == 0,
            "A landing site sealed ground off from the tube:\n  " + string.Join("\n  ", pockets));
    }

    private const double CellArea = Step * Step;

    /// <summary>How big a sealed pocket may be before it stops being a fixture's hollow inside and starts
    /// being a place. The crude-grid aesthetic draws every solid object as an OUTLINE — Miranda's monolith
    /// slab is a 2.4 × 5.0 box of four wall segments — so the space enclosed by one reads as "standable"
    /// to the collision test while being, in the fiction, the inside of a rock. Every landing site in the
    /// game today has a handful of these, roughly 6–13 du² apiece, and they are correct.
    ///
    /// <para>TIGHTENED when buildings arrived: sized against fixture interiors alone this had been 50 du²,
    /// but a small building room is about 25, so a genuinely sealed room would have slipped under the old bar
    /// and the audit would have shrugged at exactly the new failure mode. 20 du² sits above every fixture and
    /// below the smallest room worth calling one.</para>
    ///
    /// <para>#563 · Stated as an AREA rather than as a cell count, because the flood's step is no longer the
    /// same number it was. A threshold in cells is a threshold that silently changes meaning the moment
    /// somebody re-tunes the grid — this project's fifth named bug class (a guard handed a world that cannot
    /// tell pass from fail) waiting to happen.</para></summary>
    private const double MaxFixtureInteriorArea = 20.0;

    /// <summary>Flood the audited ring from the spawn, then measure what the flood could not touch: the
    /// largest connected pocket of standable-but-unreachable ground, and a point inside it. Connected
    /// components, not a raw count, because the question is "how big is the biggest thing sealed off" —
    /// twenty scattered fixture insides are fine, one sealed clearing is not.
    ///
    /// <para>#563 · Ground that touches the region's own EDGE is exempt. The world does not stop there any
    /// more; it goes on into tiles this audit did not lay, so a captain can walk round through ground that is
    /// simply not in the picture. Counting that as sealed would be the audit reporting its own bounds.</para></summary>
    private static (int Cells, (double X, double Y)? At) LargestSealedPocket(
        SurfaceCollision.WallIndex walls)
    {
        (double bMinX, double bMinY, double bMaxX, double bMaxY) = Bounds;
        int cols = (int)((bMaxX - bMinX) / Step) + 1;
        int rows = (int)((bMaxY - bMinY) / Step) + 1;

        double X(int c) => bMinX + (c * Step);
        double Y(int r) => bMinY + (r * Step);

        var standable = new bool[cols, rows];
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                standable[c, r] = DeckReachability.Standable(X(c), Y(r), AvatarRadius, walls);
            }
        }

        // Start from the standable cell nearest the spawn — the mouth itself may sit a hair inside the
        // tube walls' clearance, and that is not what this test is about.
        int sc = (int)System.Math.Round((Spawn.X - bMinX) / Step);
        int sr = (int)System.Math.Round((Spawn.Y - bMinY) / Step);
        Assert.True(
            sc >= 0 && sc < cols && sr >= 0 && sr < rows && standable[sc, sr],
            "The tube mouth is not standable — the flood has nowhere to start.");

        var seen = new bool[cols, rows];
        Flood(sc, sr, standable, seen, cols, rows);

        // Everything that can reach the region's rim is out of scope: the world carries on out there.
        var escapes = new bool[cols, rows];
        void Edge(int c, int r)
        {
            if (standable[c, r] && !escapes[c, r])
            {
                Flood(c, r, standable, escapes, cols, rows);
            }
        }
        for (int c = 0; c < cols; c++)
        {
            Edge(c, 0);
            Edge(c, rows - 1);
        }
        for (int r = 0; r < rows; r++)
        {
            Edge(0, r);
            Edge(cols - 1, r);
        }

        // Whatever NEITHER flood reached forms its own islands. Measure the biggest.
        var counted = new bool[cols, rows];
        int biggest = 0;
        (double X, double Y)? at = null;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                if (!standable[c, r] || seen[c, r] || escapes[c, r] || counted[c, r])
                {
                    continue;
                }
                int size = Flood(c, r, standable, counted, cols, rows);
                if (size > biggest) { biggest = size; at = (X(c), Y(r)); }
            }
        }
        return (biggest, at);
    }

    /// <summary>Orthogonal flood from one cell across standable ground, marking <paramref name="seen"/>.
    /// Returns how many cells it covered.</summary>
    private static int Flood(int c0, int r0, bool[,] standable, bool[,] seen, int cols, int rows)
    {
        var queue = new Queue<(int C, int R)>();
        seen[c0, r0] = true;
        queue.Enqueue((c0, r0));
        int n = 0;

        while (queue.Count > 0)
        {
            (int c, int r) = queue.Dequeue();
            n++;
            foreach ((int dx, int dy) in Steps)
            {
                int nc = c + dx, nr = r + dy;
                if (nc < 0 || nc >= cols || nr < 0 || nr >= rows || seen[nc, nr] || !standable[nc, nr])
                {
                    continue;
                }
                seen[nc, nr] = true;
                queue.Enqueue((nc, nr));
            }
        }
        return n;
    }

    // Four-way only. A captain can move diagonally, but a diagonal that squeezes between two wall corners
    // is not a passage — flooding orthogonally keeps the audit strictly more conservative than the game.
    private static readonly (int Dx, int Dy)[] Steps = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    /// <summary>The spawn point itself is somewhere a captain can stand. Cheap, and it would have caught a
    /// generated feature seeded on top of the tube mouth — the one place the game puts you without asking.</summary>
    [Fact]
    public void EveryLandingSite_LeavesTheTubeMouthStandable()
    {
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SurfaceCollision.WallIndex walls = WallsFor(body, site.LayoutSalt);

                Assert.True(
                    DeckReachability.Standable(Spawn.X, Spawn.Y, AvatarRadius, walls),
                    $"{body}/{site.Name}: something is seeded on top of the tube mouth.");
            }
        }
    }
}
