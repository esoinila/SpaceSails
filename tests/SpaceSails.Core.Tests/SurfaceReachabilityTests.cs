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
/// <para>That gap is exactly the class of bug this project keeps paying for: geometry nobody walked. The
/// audit is free, because a moon's walls are already <see cref="SurfaceCollision.Segment"/> — the very type
/// <see cref="DeckReachability"/> takes. Nothing needed generalizing; it just had never been pointed here.</para>
///
/// <para>The predicate is the same one the live avatar moves by (<see cref="DeckReachability.Standable"/>
/// wraps <see cref="SurfaceCollision.Blocked"/>), so the audit and the game agree by construction.</para>
/// </summary>
[SlowGate] // #251 · 142 s over 2 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
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

    // Wide enough to contain the bound's outward wander, so the flood actually SEES the ground the bulge
    // adds. Auditing only the nominal rectangle would silently skip every cell the new shape gained.
    // MaxBulge PLUS slack. The bound can reach exactly MaxBulge, so a grid sized to precisely that lets a
    // headland touch the grid's own edge and pinch the outside void into disconnected slivers — which the
    // audit then dutifully reported as sealed pockets on half the moons in the game. The slack keeps the
    // void one connected region so the "exactly two regions, in and out" argument holds.
    private const double GridSlackDu = 6.0;

    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds =>
        (Env.LeftX - SurfaceEdge.MaxBulgeDu - GridSlackDu, Env.BottomY - SurfaceEdge.MaxBulgeDu - GridSlackDu,
         Env.RightX + SurfaceEdge.MaxBulgeDu + GridSlackDu, Env.TopY);

    /// <summary>The collidable ground for one site: the field's own envelope (which is unseen but still
    /// solid — #563), the down-tube's two walls, and everything the body's geography generated.</summary>
    private static List<SurfaceCollision.Segment> WallsFor(string body, string salt)
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

        // The field's outer bound — invisible since #563, and since the owner's "But the limit to movement
        // is still a box here?" no longer a rectangle either. The REAL chain is audited, not the nominal
        // one: a wandering bound is the thing that actually ships, and auditing a tidier stand-in would be
        // the drawing-lies-about-the-sim failure wearing a lab coat.
        walls.AddRange(SurfaceEdge.Bound(body, salt, Env)
            .Select(e => new SurfaceCollision.Segment(e.X1, e.Y1, e.X2, e.Y2)));

        SurfaceLayout.Plan plan = SurfaceLayout.For(body, Env, salt);
        walls.AddRange(plan.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)));

        // #563 · The outpost hut, FORCED — the worst case for the field's connectivity, because a hut that
        // is still shut is only a console and has no walls at all. It is built into the far edge lane, the
        // one strip SurfaceLayout.EdgeMargin promises no generated feature intrudes on, so the question this
        // answers is whether what remains of that lane is still wide enough to walk. `forcePresent` puts one
        // on EVERY site rather than the seeded three-in-four: the audit should hold for the sites that have
        // one, and asking it of all of them is strictly harder.
        SurfaceOutpost.Placement hut = SurfaceOutpost.For(body, salt, Env, forcePresent: true);
        SurfaceOutpost.Region room = SurfaceOutpost.Build(body, salt, hut);
        walls.AddRange(room.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)));

        return walls;
    }

    /// <summary>NO STANDABLE GROUND IS SEALED OFF. Flood the whole field from the tube mouth and compare
    /// what the walk reaches against every cell the captain could physically stand on. They must match.
    ///
    /// <para>This is the honest form of the question, and the reason it is a flood rather than a list of
    /// probe points: sampling named spots (a landmark, the deep anchor, a corner) mostly measures whether
    /// the tester's arithmetic happened to miss a wall. It red-flagged Miranda's maze on the first run
    /// purely because a sample sat exactly on a gapped row, and it says nothing at all about the ground
    /// between the samples. A flood asks the thing that actually matters — is there anywhere a captain
    /// could stand and never walk to — and it names the pocket when there is.</para>
    ///
    /// <para>Today the answer must be "nowhere", because no landing site has an interior yet. When #563's
    /// huts land, a sealed hut is a legitimately unreachable pocket until it is forced — at that point this
    /// test grows an exemption for declared interiors, and that exemption is exactly the list of rooms the
    /// site claims to have. A pocket that is NOT on that list stays a failure.</para></summary>
    [Fact]
    public void EveryLandingSite_LeavesNoStandableGroundSealedOff()
    {
        var pockets = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                List<SurfaceCollision.Segment> walls = WallsFor(body, site.LayoutSalt);
                (int biggest, (double X, double Y)? at) = LargestSealedPocket(walls);

                if (biggest > MaxFixtureInteriorCells)
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

    private const double CellArea = DeckReachability.DefaultStep * DeckReachability.DefaultStep;

    /// <summary>How big a sealed pocket may be before it stops being a fixture's hollow inside and starts
    /// being a place. The crude-grid aesthetic draws every solid object as an OUTLINE — Miranda's monolith
    /// slab is a 2.4 × 5.0 box of four wall segments — so the space enclosed by one reads as "standable"
    /// to the collision test while being, in the fiction, the inside of a rock. Every landing site in the
    /// game today has a handful of these, 25–52 cells apiece (roughly 6–13 du²), and they are correct.
    ///
    /// <para>50 du² is comfortably above the largest fixture and far below anything anyone would call a
    /// room, so this catches the failure that matters — a seeded feature walling off a piece of the FIELD —
    /// without failing on every slab in the solar system.</para></summary>
    /// <para>TIGHTENED when buildings arrived. 200 was sized against fixture interiors alone, the largest
    /// of which is ~52 cells — but a small building room is around 100, so a genuinely sealed room would
    /// have slipped under the old bar and the audit would have shrugged at exactly the new failure mode.
    /// 80 sits above every fixture and below the smallest room worth calling one.</para>
    private const int MaxFixtureInteriorCells = 80;

    /// <summary>Flood the field from the spawn over the same grid the A* audit uses, then measure what the
    /// flood could not touch: the largest connected pocket of standable-but-unreachable ground, and a point
    /// inside it. Connected components, not a raw count, because the question is "how big is the biggest
    /// thing sealed off" — twenty scattered fixture insides are fine, one sealed clearing is not.</summary>
    private static (int Cells, (double X, double Y)? At) LargestSealedPocket(
        IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        const double step = DeckReachability.DefaultStep;
        (double bMinX, double bMinY, double bMaxX, double bMaxY) = Bounds;
        int cols = (int)((bMaxX - bMinX) / step) + 1;
        int rows = (int)((bMaxY - bMinY) / step) + 1;

        double X(int c) => bMinX + (c * step);
        double Y(int r) => bMinY + (r * step);

        var standable = new bool[cols, rows];
        int standableCount = 0;
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                if (DeckReachability.Standable(X(c), Y(r), AvatarRadius, walls))
                {
                    standable[c, r] = true;
                    standableCount++;
                }
            }
        }

        // Start from the standable cell nearest the spawn — the mouth itself may sit a hair inside the
        // tube walls' clearance, and that is not what this test is about.
        int sc = (int)System.Math.Round((Spawn.X - bMinX) / step);
        int sr = (int)System.Math.Round((Spawn.Y - bMinY) / step);
        Assert.True(
            sc >= 0 && sc < cols && sr >= 0 && sr < rows && standable[sc, sr],
            "The tube mouth is not standable — the flood has nowhere to start.");

        var seen = new bool[cols, rows];
        Flood(sc, sr, standable, seen, cols, rows);

        // #563 · AND flood from OUTSIDE the world. The grid is deliberately wider than the field so it
        // contains the bound's outward wander, which means its corners are open space BEYOND the bound —
        // standable, unreachable, and entirely correct. Without this the audit reported the void itself as
        // a 1,300 du² sealed pocket on half the moons in the game.
        //
        // Flooding it separately is better than shrinking the grid back to the nominal rectangle: it proves
        // the bound is a genuine SEPARATOR (exactly two regions, in and out) rather than merely testing a
        // subset of the ground and hoping.
        var outside = new bool[cols, rows];
        if (standable[0, 0])
        {
            Flood(0, 0, standable, outside, cols, rows);
        }

        // Whatever NEITHER flood reached forms its own islands. Measure the biggest.
        var counted = new bool[cols, rows];
        int biggest = 0;
        (double X, double Y)? at = null;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                if (!standable[c, r] || seen[c, r] || outside[c, r] || counted[c, r])
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
                List<SurfaceCollision.Segment> walls = WallsFor(body, site.LayoutSalt);

                Assert.True(
                    DeckReachability.Standable(Spawn.X, Spawn.Y, AvatarRadius, walls),
                    $"{body}/{site.Name}: something is seeded on top of the tube mouth.");
            }
        }
    }
}
