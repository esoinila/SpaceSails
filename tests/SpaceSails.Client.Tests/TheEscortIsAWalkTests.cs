using System;
using System.Collections.Generic;
using System.IO;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #833 · THE TWO SENTENCES THAT WERE WRITTEN OVER PLACEMENTS.
///
/// <para>Owner, evening playtest 2026-08-11, four challenges on B2: <i>"how did I jump to elevator there?"</i>
/// … <i>"So the guard walk me back to the car"</i> — and, on the challenge itself, <i>"I think the guard
/// should approach us when it does the inspection."</i> Both halves of the feature said one thing and did
/// another: the card went up the frame he NOTICED you (at up to nine deck units, with the man standing
/// wherever the round had left him), and the escort was <c>StandCaptainAt</c> with <c>EscortLine</c>'s prose
/// over it.</para>
///
/// <para><b>What this file can and cannot prove.</b> The walk lives in a partial class on a razor page no
/// test can build, so the two sims below are faithful REPLICAS of <c>Map.Patrol</c>'s own methods over the
/// real deck, the real Core constants and the real collision field — they prove the floor, the numbers and
/// the routes let both walks happen, and that the captain is never once displaced by more than a stride. The
/// clauses that decide WHEN the card raises and WHETHER a placement is used are then pinned in the page
/// itself, in the source-shape idiom <see cref="TheRoundIsNotStandingStillTests"/> uses, because a replica
/// that drifts from the page is exactly the green test that asserts nothing.</para>
///
/// <para><b>Proven able to fail.</b> Every pin below is written against the SHIPPED page of 2026-08-11:
/// <c>StopTheRoundIfAnybodySeesYou</c> called <c>TheRoundStopsAtYou(ex, g)</c> directly, and
/// <c>TheRoundStopsAtYou</c> ended in <c>StandCaptainAt(sx, sy, ...)</c>. Both are asserted absent.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheEscortIsAWalkTests
{
    private static readonly string[] Bodies = ["luna", "miranda", "titan", "europa"];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>A live rAF frame — the case #832's epsilon failed on, and the case both walks run in.</summary>
    private const double Dt = 1.0 / 60.0;

    /// <summary>One guard, exactly the fields <c>Map.Patrol</c>'s own carries.</summary>
    private sealed class Walker
    {
        public double X, Y, Vx, Vy, Facing, RePlanIn, WalkUpFor;
        public AutoWalk? Route;
    }

    // ── THE APPROACH ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HE CROSSES THE FLOOR, AND IT TAKES HIM A WHILE. On every patrolled floor of every site: a guard who
    /// has hailed a captain standing at his own notice reach closes to card reach, on his round's own gait
    /// and A*, without the ground refusing him — and he is a moving contact on the fan while he does it.
    ///
    /// <para>The count of FRAMES is the point of the assertion, not the arrival: the whole complaint was that
    /// the read happened on frame zero. A walk-up that took one frame would pass an arrival test and would be
    /// the bug.</para>
    /// </summary>
    [Fact]
    public void TheWalkUpIsAWalkAndTakesTheTimeAWalkTakes()
    {
        var bad = new List<string>();
        int floors = 0, quickest = int.MaxValue, seenOnTheFan = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                IReadOnlyList<PatrolBeat.Stop> beat = PatrolBeat.BeatFor(body, level, 0, plan, Field);
                if (beat.Count < 2)
                {
                    continue;
                }

                IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;

                // The captain stands on the spine, a few paces off the car — the corridor everybody has to
                // walk — and the man who has just hailed him is along it at his own notice reach.
                (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);
                (double cx, double cy) = Clear((shaftX + 4.0, shaftY), walls);
                (double gx, double gy) = Clear((cx + (PatrolBeat.NoticeDu - 0.5), cy), walls);
                if (!PatrolBeat.Notices(gx, gy, cx, cy, walls))
                {
                    continue;   // no line of sight from there on this floor: he would never have hailed
                }
                floors++;

                var g = new Walker { X = gx, Y = gy };
                double fan = MotionTracker.UndergroundRange(MotionTracker.DetectionRange(32.0), level);

                int frames = 0, moving = 0;
                bool arrived = false;
                while (frames < (int)(PatrolBeat.WalkUpSeconds / Dt))
                {
                    frames++;
                    if (PatrolBeat.AtCardReach(g.X, g.Y, cx, cy))
                    {
                        arrived = true;
                        break;
                    }
                    if (!PatrolBeat.StillComing(g.WalkUpFor, g.X, g.Y, cx, cy))
                    {
                        break;   // he gave up on a captain who never moved: that is a finding
                    }
                    WalkUp(g, cx, cy, walls);

                    if (MotionTracker.IsMoving(g.Vx, g.Vy))
                    {
                        moving++;
                        IReadOnlyList<MotionTracker.Blip> sweep = MotionTracker.Sweep(
                            cx, cy,
                            [new MotionTracker.Entity(g.X, g.Y, g.Vx, g.Vy, PatrolBeat.FanRegister)], fan);
                        double range = Math.Sqrt(((g.X - cx) * (g.X - cx)) + ((g.Y - cy) * (g.Y - cy)));
                        if (range <= fan)
                        {
                            seenOnTheFan++;
                            if (sweep.Count != 1 || sweep[0].Kind != MotionTracker.BlipKind.Crisp)
                            {
                                bad.Add($"  {body} B{-level}: a guard walking up {range:F0} du out, inside a " +
                                        $"{fan:F0} du fan, returned {sweep.Count} return(s) — the approach " +
                                        "has to be live on the instruments the whole way (#833).");
                            }
                        }
                    }
                }

                if (!arrived)
                {
                    bad.Add($"  {body} B{-level}: the walk-up never reached card reach — he stopped " +
                            $"{Math.Sqrt(((g.X - cx) * (g.X - cx)) + ((g.Y - cy) * (g.Y - cy))):F1} du out " +
                            $"after {frames} frames.");
                    continue;
                }

                if (moving < frames / 2)
                {
                    bad.Add($"  {body} B{-level}: he was moving on {moving} of {frames} frames of the walk-up.");
                }
                quickest = Math.Min(quickest, frames);
            }
        }

        Assert.True(bad.Count == 0, $"{bad.Count} finding(s) about the approach:\n{string.Join("\n", bad)}");

        // A sweep over nothing is a green test that asserts nothing.
        Assert.True(floors > 8, $"only {floors} restricted floors were walked up on.");
        Assert.True(seenOnTheFan > 500, $"only {seenOnTheFan} frames had the approach inside the fan.");

        // AND THE WHOLE POINT: the read is never instant. Closing (NoticeDu − CardReachDu) at WalkSpeed is
        // over two seconds of watching a man come at you, and the shipped code spent none of it.
        Assert.True(quickest > 100,
            $"the quickest walk-up took {quickest} frames — a read that arrives in under two seconds of " +
            "walking is the telepathy this issue is about (#833).");
    }

    /// <summary>
    /// WALKING AWAY IS ALLOWED, AND HE DOES NOT FOLLOW. The captain's controls are never taken during the
    /// approach, and a captain who uses them is out of it: the man stops coming inside
    /// <see cref="PatrolBeat.WalkUpSeconds"/> and no card is ever earned.
    /// </summary>
    [Fact]
    public void ACaptainWhoWalksAwayIsNeverRead()
    {
        DeckPlan deck = HiveInterior.FloorDeck("luna", -2, Field, 0, (_, _) => { }, []);
        IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;

        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);
        (double cx, double cy) = Clear((shaftX + 4.0, shaftY), walls);

        // Which way down the spine there is room to walk away — asked of the floor rather than assumed, so a
        // captain cornered against the shaft wall is never mistaken for a captain who stood still.
        double away = OpenFor(deck, cx, cy, +1) ? +1 : -1;
        Assert.True(OpenFor(deck, cx, cy, away), "luna B2 has no open spine to walk away down.");

        // …and the man who hailed him is on the other side of him.
        (double gx, double gy) = Clear((cx - (away * (PatrolBeat.NoticeDu - 0.5)), cy), walls);

        var g = new Walker { X = gx, Y = gy };
        bool everRead = false;
        int frames = 0;

        while (PatrolBeat.StillComing(g.WalkUpFor, g.X, g.Y, cx, cy) && frames < 60 * 60)
        {
            frames++;
            everRead |= PatrolBeat.AtCardReach(g.X, g.Y, cx, cy);
            WalkUp(g, cx, cy, walls);

            // …and the captain walks, at his own speed, away down the spine. 9.0 du/s against the round's
            // 3.2: out-walking a guard is never in doubt, which is exactly why it is allowed.
            (cx, cy) = deck.Move(cx, cy, away * 9.0 * Dt, 0);
        }

        Assert.False(everRead, "he read the wallet of a captain who was walking away from him.");
        Assert.True(frames < PatrolBeat.WalkUpSeconds / Dt,
            $"he was still coming after {frames} frames — nothing in this file may follow anybody (#833).");
    }

    // ── THE ESCORT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CAPTAIN WALKS TO THE LIFT — every frame of it, from the far end of the floor.
    ///
    /// <para><b>Proven RED</b> against the shipped page: <c>StandCaptainAt(sx, sy, …)</c> moved the captain
    /// the whole way in ONE frame, which is a single-frame displacement of thirty-odd deck units against the
    /// stride of 0.09 asserted below, and left the guard at his pre-challenge coordinates.</para>
    /// </summary>
    [Fact]
    public void TheWalkBackIsWalkedAndEndsAtTheCarWithHimBesideYou()
    {
        var bad = new List<string>();
        var tally = new List<string>();
        int floors = 0, escorted = 0;
        double slowest = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                IReadOnlyList<PatrolBeat.Stop> beat = PatrolBeat.BeatFor(body, level, 0, plan, Field);
                if (beat.Count < 2)
                {
                    continue;
                }

                IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;
                (double carX, double carY) = HiveInterior.SpawnOn(Field);

                // The worst case, which is the honest one: challenged at the stop that stands furthest from
                // the car, so the walk back is the longest walk the floor can ask for.
                PatrolBeat.Stop far = beat[0];
                double worst = -1;
                foreach (PatrolBeat.Stop s in beat)
                {
                    double d = ((s.X - carX) * (s.X - carX)) + ((s.Y - carY) * (s.Y - carY));
                    if (d > worst)
                    {
                        worst = d;
                        far = s;
                    }
                }

                (double gx, double gy) = Clear((far.X, far.Y), walls);
                (double cx, double cy) = Clear((gx + 1.2, gy), walls);
                double startOut = Math.Sqrt(((cx - carX) * (cx - carX)) + ((cy - carY) * (cy - carY)));
                if (startOut < 10)
                {
                    continue;   // a floor whose far stop is next to the car proves nothing about a walk
                }
                floors++;

                AutoWalk.Attempt planned = AutoWalk.Plan(
                    true, new DeckReachability.Point(gx, gy), new DeckReachability.Point(carX, carY),
                    walls, DeckPlan.AvatarRadius,
                    PatrolBeat.LatticeFor(
                        new PatrolBeat.Stop(gx, gy, "here"), new PatrolBeat.Stop(carX, carY, "the car"),
                        Field));
                if (planned.Route is null)
                {
                    // The one case the page keeps a placement for — and it is a finding here, because §13.1
                    // says this floor connects. If it ever fires, the caption is what has to be true.
                    bad.Add($"  {body} B{-level}: no route from the far stop to the car at all.");
                    continue;
                }

                var g = new Walker { X = gx, Y = gy, Route = planned.Route };
                double escortSeconds = 0, longestStep = 0, walked = 0;
                int frames = 0, moving = 0, onTheFan = 0;
                bool home = false;
                double fan = MotionTracker.UndergroundRange(MotionTracker.DetectionRange(32.0), level);

                while (escortSeconds <= PatrolBeat.EscortSecondsCap)
                {
                    frames++;
                    escortSeconds += Dt;
                    g.RePlanIn -= Dt;

                    double hx = carX - g.X, hy = carY - g.Y;
                    bool heIsThere = (hx * hx) + (hy * hy)
                        <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu;

                    double lx = cx - g.X, ly = cy - g.Y;
                    bool waitingForYou = (lx * lx) + (ly * ly) > PatrolBeat.TetherDu * PatrolBeat.TetherDu;

                    if (heIsThere || waitingForYou)
                    {
                        g.Vx = 0;
                        g.Vy = 0;
                        if (heIsThere)
                        {
                            g.Facing = Math.Atan2(ly, lx);
                        }
                    }
                    else
                    {
                        if (g.Route is not { Active: true } || g.RePlanIn <= 0)
                        {
                            g.RePlanIn = PatrolBeat.RePlanEverySeconds;
                            AutoWalk.Attempt again = AutoWalk.Plan(
                                true, new DeckReachability.Point(g.X, g.Y),
                                new DeckReachability.Point(carX, carY), walls, DeckPlan.AvatarRadius,
                                PatrolBeat.LatticeFor(
                                    new PatrolBeat.Stop(g.X, g.Y, "here"),
                                    new PatrolBeat.Stop(carX, carY, "the car"), Field));
                            if (again.Route is null)
                            {
                                break;
                            }
                            g.Route = again.Route;
                        }
                        Stride(g, walls);
                    }

                    // …and the captain, walked at his shoulder through the ONE primitive his body is ever
                    // stepped by. This is DeckPlan.Move, called on the real deck — not a copy of it.
                    (double tx, double ty) = heIsThere ? (carX, carY) : ShoulderOf(g);
                    double cdx = tx - cx, cdy = ty - cy;
                    double want = Math.Sqrt((cdx * cdx) + (cdy * cdy));
                    (double wasX, double wasY) = (cx, cy);
                    if (want > 1e-6)
                    {
                        double pace = Math.Min(
                            want, PatrolBeat.WalkSpeed * PatrolBeat.CatchUpFactor * Dt);
                        (cx, cy) = deck.Move(cx, cy, cdx / want * pace, cdy / want * pace);
                    }

                    double stepped = Math.Sqrt(
                        ((cx - wasX) * (cx - wasX)) + ((cy - wasY) * (cy - wasY)));
                    longestStep = Math.Max(longestStep, stepped);
                    walked += stepped;

                    if (MotionTracker.IsMoving(g.Vx, g.Vy))
                    {
                        moving++;
                        IReadOnlyList<MotionTracker.Blip> sweep = MotionTracker.Sweep(
                            cx, cy,
                            [new MotionTracker.Entity(g.X, g.Y, g.Vx, g.Vy, PatrolBeat.FanRegister)], fan);
                        if (sweep.Count == 1 && sweep[0].Kind == MotionTracker.BlipKind.Crisp)
                        {
                            onTheFan++;
                        }
                    }

                    double adx = carX - cx, ady = carY - cy;
                    if (heIsThere && (adx * adx) + (ady * ady) <= PatrolBeat.AtTheCarDu * PatrolBeat.AtTheCarDu)
                    {
                        home = true;
                        break;
                    }
                }

                if (!home)
                {
                    bad.Add($"  {body} B{-level}: the escort never arrived — the captain was " +
                            $"{Math.Sqrt(((carX - cx) * (carX - cx)) + ((carY - cy) * (carY - cy))):F1} du " +
                            $"from the car after {frames} frames; the guard was " +
                            $"{Math.Sqrt(((carX - g.X) * (carX - g.X)) + ((carY - g.Y) * (carY - g.Y))):F1} " +
                            $"du out, moving on {moving} of them, having started {startOut:F1} du out at " +
                            $"({gx:F1}, {gy:F1}) with the car at ({carX:F1}, {carY:F1}).");
                    continue;
                }
                escorted++;
                slowest = Math.Max(slowest, escortSeconds);
                tally.Add($"{body} B{-level}: {startOut:F0} du in {escortSeconds:F0} s, moving " +
                          $"{moving * 100.0 / frames:F0}%, step {longestStep:F3}");

                // NO SINGLE-FRAME JUMP. The captain's own frame budget is the whole of what he may be moved
                // by, and the shipped placement was thirty-odd deck units of it in one frame.
                double stride = PatrolBeat.WalkSpeed * PatrolBeat.CatchUpFactor * Dt;
                if (longestStep > stride + 1e-9)
                {
                    bad.Add($"  {body} B{-level}: the captain moved {longestStep:F2} du in one frame — a " +
                            $"stride is {stride:F2} du (#833: this is what a teleport looks like).");
                }

                // …and he covered the ground, rather than being nudged along a short cut.
                if (walked < startOut * 0.75)
                {
                    bad.Add($"  {body} B{-level}: the captain walked {walked:F1} du to cover {startOut:F1} du " +
                            "of floor — that is not the route the guard took.");
                }

                // HE IS AT THE DOORS WITH YOU, not back where he stopped you.
                double hisGap = Math.Sqrt(((g.X - carX) * (g.X - carX)) + ((g.Y - carY) * (g.Y - carY)));
                if (hisGap > PatrolBeat.AtTheStopDu + 1e-9)
                {
                    bad.Add($"  {body} B{-level}: the escort ended with the guard {hisGap:F1} du from the " +
                            "car — he is supposed to see you into it.");
                }

                // …and both of them were contacts the whole way (owner: "they should definitely show on the
                // motion tracker").
                if (moving < frames / 2 || onTheFan < moving / 2)
                {
                    bad.Add($"  {body} B{-level}: the escorting guard moved on {moving} of {frames} frames " +
                            $"and returned a crisp blip on {onTheFan} of them.");
                }
            }
        }

        Assert.True(bad.Count == 0, $"{bad.Count} finding(s) about the walk back:\n{string.Join("\n", bad)}");
        Assert.True(floors > 6, $"only {floors} floors were long enough to be an escort worth measuring.");
        Assert.Equal(floors, escorted);

        // …and the BOUND is a backstop rather than the thing that ends the walk. Measured over the 22 floors
        // this sweep walks, the worst escort — the far room of the far rib, 152 du of corridor — arrives in
        // 56 seconds with the guard moving on 99% of the frames. If that ever creeps up on the cap, the cut
        // is about to start firing in ordinary play and somebody has to look at why.
        Assert.True(slowest < PatrolBeat.EscortSecondsCap * 0.75,
            $"the slowest escort took {slowest:F0} s against a {PatrolBeat.EscortSecondsCap:F0} s bound:\n" +
            string.Join("\n", tally));
    }

    // ── THE REPLICAS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>One frame of <c>Map.Patrol.SpendTheStride</c>, line for line.</summary>
    private static void Stride(Walker g, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        double budget = PatrolBeat.WalkSpeed * Dt;
        double startX = g.X, startY = g.Y;

        for (int step = 0; step < 8 && budget > 1e-9; step++)
        {
            if (g.Route is not { Active: true } route
                || !route.TryStep(g.X, g.Y, budget, out double dx, out double dy))
            {
                break;
            }
            (double nx, double ny) = SurfaceCollision.Slide(
                g.X, g.Y, dx, dy, DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);
            double mx = nx - g.X, my = ny - g.Y;
            if ((mx * mx) + (my * my) < 1e-18)
            {
                route.Snag();
                break;
            }
            budget -= Math.Sqrt((mx * mx) + (my * my));
            g.X = nx;
            g.Y = ny;
        }

        double tx = g.X - startX, ty = g.Y - startY;
        g.Vx = tx / Dt;
        g.Vy = ty / Dt;
        if ((tx * tx) + (ty * ty) > 1e-8)
        {
            g.Facing = Math.Atan2(ty, tx);
        }
    }

    /// <summary>One frame of <c>Map.Patrol.WalkUpToTheCaptain</c>, after its two exits.</summary>
    private static void WalkUp(
        Walker g, double cx, double cy, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        g.WalkUpFor += Dt;
        g.RePlanIn -= Dt;

        if (g.Route is not { Active: true } || g.RePlanIn <= 0)
        {
            g.RePlanIn = PatrolBeat.RePlanEverySeconds;
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(cx, cy),
                walls, DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(
                    new PatrolBeat.Stop(g.X, g.Y, "here"), new PatrolBeat.Stop(cx, cy, "you"), Field));
            if (planned.Route is null)
            {
                g.WalkUpFor = PatrolBeat.WalkUpSeconds * 2;   // the page gives up; so does this
                return;
            }
            g.Route = planned.Route;
        }

        Stride(g, walls);
    }

    /// <summary><c>Map.Patrol.ShoulderOf</c>, line for line.</summary>
    private static (double X, double Y) ShoulderOf(Walker g)
    {
        double back = PatrolBeat.ShoulderDu, side = PatrolBeat.ShoulderDu * 0.25;
        return (g.X - (Math.Cos(g.Facing) * back) - (Math.Sin(g.Facing) * side),
                g.Y - (Math.Sin(g.Facing) * back) + (Math.Cos(g.Facing) * side));
    }

    /// <summary>Is there ten deck units of walkable spine that way? Walked with the captain's own legs rather
    /// than reasoned about, because the walls are what decide it.</summary>
    private static bool OpenFor(DeckPlan deck, double x, double y, double sign)
    {
        double went = 0;
        for (int i = 0; i < 120 && went < 10.0; i++)
        {
            (double nx, double ny) = deck.Move(x, y, sign * 9.0 * Dt, 0);
            went += Math.Abs(nx - x);
            (x, y) = (nx, ny);
        }
        return went >= 10.0;
    }

    /// <summary>A standable square, through the sim's own net (#681) — so a test never starts a body inside
    /// stone and then blames the walk for it.</summary>
    private static (double X, double Y) Clear(
        (double X, double Y) at, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SpawnNudge.Result spot = SpawnNudge.Clear(at.X, at.Y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? at : (spot.X, spot.Y);
    }

    // ── THE PAGE ITSELF ───────────────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>#870 · The deck page is seven partials by subject now, so "the deck" a guard reads over is
    /// all of them — exactly the text it read out of one file before the split. Concatenated rather than
    /// narrowed to one part on purpose: the claims below count and refuse over the WHOLE page, and pointing
    /// them at a single partial would be a silent weakening.</summary>
    private static string Deck() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Deck*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    /// <summary>#870 · The round is six partials by subject now, so the page this guard reads is all six —
    /// concatenated in the order the one file laid them out, which is exactly the text it read before the
    /// split. The count is asserted, so a seventh part can never go unread.</summary>
    private static string Patrol()
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages");
        string[] order =
        [
            // #870 lane 6′c · RE-PATHED. The verbs moved onto Patrol's own partials, so the page's half
            // is four files: Map.Patrol.Round.cs and Map.Patrol.Escort.cs had no caller outside the family
            // to forward to and are gone. The count is still asserted, so a fifth part cannot go unread.
            "Map.Patrol.cs", "Map.Patrol.Hide.cs", "Map.Patrol.Challenge.cs", "Map.Patrol.Run.cs",
            "Map.PatrolHost.cs",
        ];
        Assert.Equal(order.Length, Directory.GetFiles(dir, "Map.Patrol*.cs").Length);

        // #870 lane 6′b · RE-PATHED, never re-asserted. The round's twenty-two fields and the Guard they
        // are made of moved into Pages/Patrol/, so the page this guard reads is EIGHT files now — the six
        // verbs first, in the order the one file laid them out, then the state. Both directories are
        // counted, so a ninth part can never go unread.
        string own = Path.Combine(dir, "Patrol");
        string[] state =
        [
            "Patrol.cs", "Guard.cs", "IPatrolHost.cs",
            "Patrol.Floor.cs", "Patrol.Hide.cs", "Patrol.Round.cs",
            "Patrol.Challenge.cs", "Patrol.Escort.cs", "Patrol.Run.cs",
        ];
        Assert.Equal(state.Length, Directory.GetFiles(own, "*.cs").Length);

        var parts = new string[order.Length + state.Length];
        for (int i = 0; i < order.Length; i++)
        {
            parts[i] = File.ReadAllText(Path.Combine(dir, order[i]));
        }
        for (int i = 0; i < state.Length; i++)
        {
            parts[order.Length + i] = File.ReadAllText(Path.Combine(own, state[i]));
        }
        return string.Concat(parts);
    }

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    /// <summary>
    /// A NOTICE BUYS THE HAIL, AND NOTHING ELSE. The clause the shipped page got wrong, pinned where the
    /// replicas cannot see it: the sighting loop raises no card, and the ONE call that does is behind
    /// <see cref="PatrolBeat.AtCardReach"/> in the walk-up.
    /// </summary>
    [Fact]
    public void ThePageOnlyReadsAWalletAtCardReach()
    {
        string patrol = Patrol();

        string sighting = Between(
            patrol, "private void StopTheRoundIfAnybodySeesYou(", "public void TheHail(");
        Assert.Contains("PatrolBeat.Notices(", sighting, StringComparison.Ordinal);
        Assert.Contains("TheHail(g);", sighting, StringComparison.Ordinal);
        // The shipped shape: the card, raised at up to NoticeDu.
        Assert.DoesNotContain("TheRoundStopsAtYou(", sighting, StringComparison.Ordinal);

        string walkUp = Between(patrol, "private void WalkUpToTheCaptain(", "private void GiveUpTheHail(");

        // #618 · THE DESTINATION IS A NAMED PAIR NOW, and the clause is the same clause. A man may be crossing
        // the floor to a PLACE — where a gun went off — instead of to a person, so the reach is asked of a
        // destination; and that destination is the captain on every road but that one, spelled out here so a
        // lane which quietly widened it to something else has to come through this line.
        Assert.Contains("bool toTheNoise = ReferenceEquals(g, LookingIntoIt);", walkUp, StringComparison.Ordinal);
        Assert.Contains("double toX = toTheNoise ? TheNoise.X : _host.AvatarX;", walkUp, StringComparison.Ordinal);
        Assert.Contains("double toY = toTheNoise ? TheNoise.Y : _host.AvatarY;", walkUp, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.AtCardReach(g.X, g.Y, toX, toY)", walkUp, StringComparison.Ordinal);
        Assert.Contains("TheRoundStopsAtYou(ex, g);", walkUp, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.StillComing(", walkUp, StringComparison.Ordinal);

        // …and arriving at a NOISE may never raise the card: that arm returns ABOVE the read, so a man who
        // walked over to a hole in a hasp does not inspect the papers of whoever is not standing beside it.
        int atTheNoise = walkUp.IndexOf("TheNoiseWasNothing(g);", StringComparison.Ordinal);
        int theCard = walkUp.IndexOf("TheRoundStopsAtYou(ex, g);", StringComparison.Ordinal);
        Assert.True(
            atTheNoise >= 0 && atTheNoise < theCard,
            "the walk to a bang no longer returns above the read — a man who arrived at a PLACE would raise "
            + "a challenge at whatever happened to be the destination.");

        // …and there is exactly ONE road to the card in the whole file, so it cannot be raised from anywhere
        // that has not asked how far away he is.
        Assert.Equal(2, Count(patrol, "TheRoundStopsAtYou("));   // the call, and its own declaration

        // The reach itself is a real number and a short one: a card at NoticeDu is the bug.
        Assert.True(PatrolBeat.CardReachDu < PatrolBeat.NoticeDu / 3);
    }

    /// <summary>
    /// THE ESCORT IS A WALK, AND THE ONE PLACEMENT LEFT ADMITS IT IS A CUT. <c>TheRoundStopsAtYou</c> ends by
    /// ARMING the walk; the only <c>StandCaptainAt</c> left in the file sits in the fallback, next to the
    /// caption that says the walk did not happen.
    /// </summary>
    [Fact]
    public void ThePageWalksTheCaptainBackAndNeverPlacesHimWithoutSayingSo()
    {
        string patrol = Patrol();

        string read = Between(patrol, "private void TheRoundStopsAtYou(", "── #833 · THE WALKED ESCORT");
        Assert.Contains("EscortDue = g;", read, StringComparison.Ordinal);
        // The shipped shape, verbatim.
        Assert.DoesNotContain("StandCaptainAt(", read, StringComparison.Ordinal);

        string escort = Between(patrol, "private void WalkTheEscort(", "private static (double X, double Y) ShoulderOf(");
        Assert.Contains("SpendTheStride(g, dt, walls);", escort, StringComparison.Ordinal);
        Assert.Contains("_host.DeckPlan.Move(_host.AvatarX, _host.AvatarY", escort, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.TetherDu", escort, StringComparison.Ordinal);
        Assert.DoesNotContain("StandCaptainAt(", escort, StringComparison.Ordinal);

        // The whole file has exactly one placement left, and it is the admitted cut.
        // #870 lane 6c - RE-SPELLED: it counts CALLS by the round, through the one door, rather than
        // every mention of the name (the interface declares it and the page answers it).
        Assert.Equal(1, Count(patrol, "_host.StandCaptainAt("));
        string cut = Between(patrol, "private void TheCutToTheLift(", "── WHERE THE PASS COMES FROM");
        Assert.Contains("StandCaptainAt(", cut, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.EscortCutLine", cut, StringComparison.Ordinal);
        Assert.Contains("Next thing you know, you are at the lift", PatrolBeat.EscortCutLine,
            StringComparison.Ordinal);
    }

    /// <summary>#833 · The controls are held for the escort and for nothing else — and they are held in the
    /// two places #784's chair already taught this codebase to hold them: the press, and the held key.
    ///
    /// <para>#875 · The press half is asked through ONE predicate that BOTH grips consult now, so the shape
    /// this counts changed while the law did not: <c>CaptainIsUnderEscort</c> is read in exactly three places
    /// — the predicate, the sentence it prints, and the frame's own legs — and the two grips ask the
    /// predicate rather than the field. An inline re-check in either handler is a second author on one law,
    /// which is precisely what #875 was filed on, and it puts the counts below out.</para></summary>
    [Fact]
    public void TheControlsAreHeldOnlyWhileSomebodyIsWalkingYouOut()
    {
        string deck = Deck();

        // The one predicate, the one sentence, and the frame's own refusal of a held key / a live route.
        Assert.Contains("TheCaptainsLegsAreTheirOwn => _deckMode && !CaptainIsUnderEscort", deck,
            StringComparison.Ordinal);
        Assert.Contains("TheHoldOnTheLegs => CaptainIsUnderEscort ? Core.PatrolBeat.EscortHeldLine : null",
            deck, StringComparison.Ordinal);
        Assert.Equal(1, Count(deck, "if (CaptainIsUnderEscort)"));   // MoveAvatar's own, and nowhere else

        // …and BOTH grips are refused by that predicate: the key press, and the clicked route.
        Assert.Equal(2, Count(deck, "if (!TheCaptainsLegsAreTheirOwn)"));
        Assert.Contains("Core.PatrolBeat.EscortHeldLine", deck, StringComparison.Ordinal);

        // …and the approach is NOT one of them. The captain may always walk away from a hail.
        string patrol = Patrol();
        string walkUp = Between(patrol, "private void WalkUpToTheCaptain(", "private void GiveUpTheHail(");
        Assert.DoesNotContain("_host.AvatarX =", walkUp, StringComparison.Ordinal);
        Assert.DoesNotContain("_host.AvatarY =", walkUp, StringComparison.Ordinal);
    }

    private static int Count(string text, string needle)
    {
        int n = 0;
        for (int i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }
}
