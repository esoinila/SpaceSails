using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1074 beat 2 · THE FENCE SHUTS NOBODY IN AND NOBODY OUT — the preservation zone walked, on the REAL deck
/// the game builds, with A* and a flood.
///
/// <para>The rail goes round the shed the lift car comes up in. That is the whole reason this file exists:
/// the captain is set down INSIDE the ring when he rides home from the halls, so a ring with no gap, or a
/// ring whose gap another building happens to stand across, is a captain fenced away from his own boat with
/// a tank running down. #602's report was one man in a wall; this would be one man in a paddock.</para>
///
/// <para><b>It drives <see cref="MoonSurface.SurfaceDeck"/> and not a hand-built field.</b> The shed is
/// seeded — size, thickness, angle and where on the ground it stands — and every other wall on a landing
/// site is seeded too, so an audit over geometry the test laid itself would be auditing a world the game
/// does not ship. That is #587's lesson and #573's: a test that MIRRORS the ground is not testing the ground.
/// The lift head is FORCED on every site (<c>hasSecretSite: true</c>), which is strictly harder than the
/// seeded truth and is the same move <c>SurfaceReachabilityTests</c> makes with the outpost huts.</para>
///
/// <para>The predicate is the one the live avatar moves by (<see cref="DeckReachability.Standable"/> wraps
/// <see cref="SurfaceCollision.Blocked"/>), so the audit and the game agree by construction rather than by
/// comment.</para>
/// </summary>
public sealed class ThePreservedSiteIsStillWalkableTests
{
    private static readonly SurfaceLayout.Field Env = SurfaceLayout.DefaultField;

    private const double AvatarRadius = 0.7;   // DeckPlan.AvatarRadius — the captain's own body

    /// <summary>The flood's and the walk's step. Coarser than <see cref="DeckReachability.DefaultStep"/> for
    /// <c>SurfaceReachabilityTests</c>' reason, and SOUND for its reason too: two cells this far apart cannot
    /// have a wall between them when each is <see cref="AvatarRadius"/> clear of every wall, because a
    /// segment lying between them would be within 0.5 du of one of them.</summary>
    private const double Step = 1.0;

    /// <summary>Every landable body the game can put a captain on — the same list the surface reachability
    /// audit walks, and for the same reason: Miranda and Luna are authored geography and the rest are
    /// seeded, so a sample of one kind proves nothing about the other.</summary>
    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];

    /// <summary>Where the captain actually arrives: just below the tube mouth, on the landing band.</summary>
    private static DeckReachability.Point Spawn => new(Env.HomeX, Env.TopY - 2.0);

    /// <summary>The audited region: the field the home tile lays, with a little air round it. Ground that
    /// touches this rim is exempt from the pocket count — the world carries on into tiles this deck did not
    /// lay (<see cref="SurfaceTiles"/>), so counting it as sealed would be the audit reporting its own
    /// bounds.</summary>
    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds =>
        (Env.LeftX - 2, Env.BottomY - 2, Env.RightX + 2, Env.TopY);

    /// <summary>Build the ground the game ships for this site, with the lift head forced on, either fenced
    /// or not. The register is installed around the build and put back afterwards whatever happens — and the
    /// deck cache is keyed on it (<see cref="SurfaceDeckKey.Preserved"/>), so the two builds are two grounds
    /// rather than one served twice.</summary>
    private static SurfaceCollision.WallIndex GroundFor(
        string body, LandingSite site, bool fenced, out (float X, float Y, string Text)[] labels)
    {
        PreservationZone.Install(fenced ? [body] : []);
        try
        {
            DeckPlan deck = MoonSurface.SurfaceDeck(
                body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name,
                monolithEpoch: 0, hasSecretSite: true);
            labels = deck.RoomLabels;
            return SurfaceCollision.WallIndex.Build(
                [.. deck.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))]);
        }
        finally
        {
            PreservationZone.Install([]);
        }
    }

    /// <summary>
    /// #1074 · A CAPTAIN CAN ALWAYS WALK BETWEEN THE TUBE AND THE SHED INSIDE THE RING — both directions, on
    /// every body, on every landing site, with the fence up.
    ///
    /// <para>Two targets, and each says a different half of the law: the DOORSTEP outside the shed, which is
    /// where a landing sets him down and where he stands after walking out of the hut, and the CAR FLOOR
    /// inside it, which is where the lift puts him when he comes home from the halls. Both are the shed's
    /// own published spots (<c>MoonSurface.LiftHead</c>), never numbers this test made up — a caller doing
    /// its own geometry about a building it does not own is the #602/#681 bug, and a TEST doing it is that
    /// bug wearing a lab coat.</para>
    ///
    /// <para>Both directions, because "the shed can be reached from the tube" and "the tube can be reached
    /// from the shed" are two sentences and only the second one is about being fenced in. A* on a symmetric
    /// lattice is symmetric and asking twice is cheap; what it buys is that the guard has said both.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the gap side drawn like the other fifteen (the rail loop started
    /// at <c>k = 0</c>), which is a ring with no way out — <i>"the fence shut somebody in: miranda/The Ridge
    /// Camp: the car floor cannot be walked to from the tube / miranda/The Ridge Camp: the tube cannot be
    /// walked to from the car floor"</i>.</para>
    /// </summary>
    [Fact]
    public void TheFenceNeverStandsBetweenTheCaptainAndHisBoat()
    {
        var wrong = new List<string>();
        int walked = 0;

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SurfaceCollision.WallIndex walls = GroundFor(body, site, fenced: true, out _);
                MoonSurface.LiftHeadBox head =
                    MoonSurface.LiftHead(body, site.LayoutSalt, MoonSurface.ExpeditionField());

                (double sx, double sy) = head.DoorStep;
                (double cx, double cy) = head.CarFloor;
                var targets = new List<(string What, DeckReachability.Point Where)>
                {
                    ("the shed's doorstep", new DeckReachability.Point(sx, sy)),
                    ("the car floor", new DeckReachability.Point(cx, cy)),
                };

                Assert.True(DeckReachability.Standable(Spawn.X, Spawn.Y, AvatarRadius, walls),
                    $"{body}/{site.Name}: the tube mouth is not standable — the walk has nowhere to start.");

                foreach ((string what, DeckReachability.Point where) in targets)
                {
                    if (!DeckReachability.Standable(where.X, where.Y, AvatarRadius, walls))
                    {
                        // A spot inside the shed's own wall is somebody else's guard
                        // (TheLiftPutsYouSomewhereYouCanSTANDTests); this one is about the rail.
                        continue;
                    }
                    walked++;
                    if (!DeckReachability.Path(Spawn, where, walls, AvatarRadius, Bounds, Step).Reached)
                    {
                        wrong.Add($"  {body}/{site.Name}: {what} cannot be walked to from the tube");
                    }
                    if (!DeckReachability.Path(where, Spawn, walls, AvatarRadius, Bounds, Step).Reached)
                    {
                        wrong.Add($"  {body}/{site.Name}: the tube cannot be walked to from {what}");
                    }
                }
            }
        }

        Assert.True(walked >= 20, $"only {walked} spot(s) were walked to — this proves little.");
        Assert.True(wrong.Count == 0, "the fence shut somebody in:\n" + string.Join("\n", wrong));
    }

    /// <summary>
    /// #1074 · AND THE RAIL SEALS NO GROUND OFF THAT WAS OPEN BEFORE IT WENT UP.
    ///
    /// <para>The walk above names two spots; this is the honest form of the same question, and the reason it
    /// is a flood rather than more probe points: sampling named places mostly measures whether the tester's
    /// arithmetic happened to miss a wall, and says nothing at all about the ground between the samples.
    /// A flood asks the thing that actually matters — <b>is there anywhere on this site a captain could stand
    /// and never walk back to the tube</b> — and it names the pocket when there is one.</para>
    ///
    /// <para><b>It is a DELTA and not an absolute</b>, and that is deliberate. Every landing site in this
    /// game already has a handful of small sealed insides — the crude grid draws a solid object as an
    /// outline, so the space inside a plinth reads as standable to a collision test while being, in the
    /// fiction, the inside of a rock — and this guard is not about those. It builds the same site twice, once
    /// plain and once fenced, and asks whether the RAIL made anything worse. A fence is allowed to enclose
    /// ground; it is not allowed to seal any.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the same one — the gap side drawn like the others —
    /// <i>"the rail sealed ground off: miranda/The Ridge Camp: the biggest pocket went 12.0 du² → 743.0 du²
    /// around (-118.0, -207.0)"</i>, which is the whole inside of the ring with the shed in it.</para>
    /// </summary>
    [Fact]
    public void TheRailSealsNoGroundOffThatWasOpenBeforeIt()
    {
        var wrong = new List<string>();
        int compared = 0;

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                (double plain, _) = LargestSealedPocket(GroundFor(body, site, false, out _));
                (double fenced, (double X, double Y)? at) =
                    LargestSealedPocket(GroundFor(body, site, true, out _));
                compared++;

                // A hair of slack for the cells the rail itself takes out of an existing fixture inside;
                // anything the rail actually SEALS is a clearing, and a clearing is orders of magnitude
                // bigger than this.
                if (fenced > plain + MaxFixtureInteriorArea)
                {
                    string where = at is { } s ? $" around ({s.X:F1}, {s.Y:F1})" : "";
                    wrong.Add(
                        $"  {body}/{site.Name}: the biggest pocket went {plain:F1} du² → {fenced:F1} du²" +
                        where);
                }
            }
        }

        Assert.True(compared >= 9, $"only {compared} site(s) were flooded — this proves little.");
        Assert.True(wrong.Count == 0, "the rail sealed ground off:\n" + string.Join("\n", wrong));
    }

    /// <summary>
    /// #1074 · THE NOTICE IS POSTED ON A PRESERVED SITE, ONCE, AND ON NO OTHER SITE AT ALL.
    ///
    /// <para>Read off the REAL deck's own labels, both ways round — the same body and the same site built
    /// plain and built fenced — so this is a guard about what the game draws rather than about what a
    /// constant says. One sign and not two, because the sign is posted at THE gate and a ring has one.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the <c>preserved</c> arm dropped from <c>BuildLayout</c>, so
    /// the notice went up on every ground with a shed on it — <i>"Assert.Equal() Failure: Values differ.
    /// Expected: 0. Actual: 1"</i> on the plain build; and <c>preserved</c> left out of
    /// <see cref="SurfaceDeckKey"/>, which served the plain deck back for the fenced build out of the memo —
    /// <i>"Expected: 1. Actual: 0"</i>.</para>
    /// </summary>
    [Fact]
    public void TheNoticeIsPostedOnAPreservedSiteAndOnNoOther()
    {
        int posted = 0;
        foreach (string body in Bodies)
        {
            LandingSite site = LandingSites.For(body)[0];

            GroundFor(body, site, fenced: false, out (float X, float Y, string Text)[] plain);
            Assert.Equal(0, plain.Count(l => PreservationZone.IsNotice(l.Text)));

            GroundFor(body, site, fenced: true, out (float X, float Y, string Text)[] fenced);
            Assert.Equal(1, fenced.Count(l => PreservationZone.IsNotice(l.Text)));
            posted++;

            // …and it stands OUTSIDE the rail, on the approach — a sign you read on the way in rather than
            // one you find after walking through the gate.
            (float sx, float sy, _) = fenced.Single(l => PreservationZone.IsNotice(l.Text));
            PreservationZone.Fence fence = PreservationZone.FenceAround(
                SecretLab.HeadHut(body, site.LayoutSalt, Env), Env);
            double reach = Math.Sqrt(
                ((sx - fence.CentreX) * (sx - fence.CentreX))
                + ((sy - fence.CentreY) * (sy - fence.CentreY)));
            Assert.True(reach > fence.Radius,
                $"{body}/{site.Name}: the notice stands inside its own fence");
        }

        Assert.True(posted >= 9, $"only {posted} site(s) were checked — this proves little.");
    }

    // ── the flood, and the bar it is measured against ────────────────────────────────────────────────────

    private const double CellArea = Step * Step;

    /// <summary>How big a sealed pocket may be before it stops being a fixture's hollow inside and starts
    /// being a place. The same number and the same argument as <c>SurfaceReachabilityTests</c>' own bar: it
    /// sits above every fixture interior on every site in the game and below the smallest room worth calling
    /// one. Stated as an AREA rather than as a cell count, because a threshold in cells silently changes
    /// meaning the moment somebody re-tunes the grid.</summary>
    private const double MaxFixtureInteriorArea = 20.0;

    /// <summary>Flood the field from the tube mouth, then measure what the flood could not touch: the largest
    /// connected pocket of standable-but-unreachable ground, in du², and a point inside it.</summary>
    private static (double Area, (double X, double Y)? At) LargestSealedPocket(
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

        int sc = (int)Math.Round((Spawn.X - bMinX) / Step);
        int sr = (int)Math.Round((Spawn.Y - bMinY) / Step);
        Assert.True(sc >= 0 && sc < cols && sr >= 0 && sr < rows && standable[sc, sr],
            "The tube mouth is not standable — the flood has nowhere to start.");

        var seen = new bool[cols, rows];
        Flood(sc, sr, standable, seen, cols, rows);

        // Everything that can reach the region's rim is out of scope: the world carries on out there into
        // tiles this deck did not lay.
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
        return (biggest * CellArea, at);
    }

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

    // Four-way only. A captain can move diagonally, but a diagonal that squeezes between two wall corners is
    // not a passage — flooding orthogonally keeps the audit strictly more conservative than the game.
    private static readonly (int Dx, int Dy)[] Steps = [(1, 0), (-1, 0), (0, 1), (0, -1)];
}
