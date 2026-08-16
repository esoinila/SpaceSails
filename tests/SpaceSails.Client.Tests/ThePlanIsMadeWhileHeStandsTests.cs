using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #858 · THE ONLY THING IN LAB 45 THAT COULD EAT A FRAME — and the guard walks the identical round without
/// it.
///
/// <para>Lab 45 (#852) §C timed one <c>AutoWalk.Plan</c> over a guard's own leg at a median 1.6–2.2 ms and a
/// worst <b>6.4 ms — 38.6% of a 60 fps frame</b>, spent whole on the single frame he leaves a stop, about
/// twice a minute per guard. That is NATIVE, on a desk machine, in a game that ships to WASM, and it lands
/// on a frame the player is often watching him on. He is standing at that stop for five seconds either way,
/// so the search now runs during the stand, <c>PatrolBeat.PlanCellsAFrame</c> cells at a time.</para>
///
/// <para><b>The whole of the risk is that he comes out different.</b> A perf fix that changed a route, or a
/// stand, or left a man waiting on a plan that never finished (#843's class: a body that is waiting is a
/// body that never walks again) would be a worse bug than the hitch it cured. So this file walks a real
/// round on real floors TWICE — once planning on departure exactly as the shipped code did, once planning
/// through the stand — and demands the two men be in the same place on every frame of a simulated minute.
/// Then it demands the spread actually do the work, because a slice budget that never finished anything
/// would pass the sameness test perfectly.</para>
///
/// <para>The replica below is <see cref="TheRoundIsNotStandingStillTests"/>'s, for its reason: the stepper
/// is a private method of a razor page no test can build, so the page itself is pinned in the source-shape
/// idiom at the bottom.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ThePlanIsMadeWhileHeStandsTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private const double Dt = 1.0 / 60.0;

    /// <summary>A simulated minute — several legs and several stands on every floor walked.</summary>
    private const int Frames = 60 * 60;

    private static readonly (string Body, int Level)[] Floors =
        [("luna", -2), ("luna", -3), ("miranda", -2)];

    /// <summary>One guard, exactly the fields <c>Map.Patrol</c>'s own carries — plus the tally this file is
    /// about, which the page has no need of.</summary>
    private sealed class Walker
    {
        public double X, Y, Vx, Vy, Standing;
        public int Leg, Retries;
        public AutoWalk? Route;
        public AutoWalk.Planner? Planning;

        public bool StoodLastFrame;
        public int LeftAStop;          // plans asked for on the frame a stand ended
        public int ReadyOnDeparture;   // …of which the stand had already finished
    }

    /// <summary>One frame of the round, replicating <c>Map.Patrol.WalkTheRound</c> line for line.
    /// <paramref name="slice"/> = 0 is the shipped-before-#858 shape: nothing is planned during the stand and
    /// the whole A* is paid on the frame he leaves.</summary>
    private static void Step(
        Walker g, IReadOnlyList<SurfaceCollision.Segment> walls, IReadOnlyList<PatrolBeat.Stop> beat, int slice)
    {
        if (g.Standing > 0)
        {
            g.Standing -= Dt;
            g.Vx = 0;
            g.Vy = 0;
            g.StoodLastFrame = true;
            if (slice > 0)
            {
                PatrolBeat.Stop next = beat[g.Leg % beat.Count];
                if (g.Planning is not { } ahead || !ahead.PlannedFor(At(g), At(next)))
                {
                    ahead = Begin(g, next, walls);
                    g.Planning = ahead;
                }
                ahead.Advance(PatrolBeat.PlanCellsAFrame);
            }
            return;
        }

        PatrolBeat.Stop target = beat[g.Leg % beat.Count];
        if (g.Route is not { Active: true })
        {
            AutoWalk.Planner plan = g.Planning is { } ahead && ahead.PlannedFor(At(g), At(target))
                ? ahead
                : Begin(g, target, walls);
            g.Planning = null;

            if (g.StoodLastFrame)
            {
                g.LeftAStop++;
                if (plan.Done)
                {
                    g.ReadyOnDeparture++;
                }
            }
            g.StoodLastFrame = false;

            AutoWalk.Attempt planned = plan.Finish();
            if (planned.Route is null)
            {
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % beat.Count;
                return;
            }
            g.Route = planned.Route;
        }
        g.StoodLastFrame = false;

        double budget = PatrolBeat.WalkSpeed * Dt;
        double startX = g.X, startY = g.Y;
        for (int step = 0; step < 8 && budget > 1e-9; step++)
        {
            if (!g.Route.TryStep(g.X, g.Y, budget, out double dx, out double dy))
            {
                break;
            }
            (double nx, double ny) = SurfaceCollision.Slide(
                g.X, g.Y, dx, dy, DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);
            double mx = nx - g.X, my = ny - g.Y;
            if ((mx * mx) + (my * my) < 1e-18)
            {
                g.Route.Snag();
                break;
            }
            budget -= Math.Sqrt((mx * mx) + (my * my));
            g.X = nx;
            g.Y = ny;
        }

        g.Vx = (g.X - startX) / Dt;
        g.Vy = (g.Y - startY) / Dt;

        double gx = target.X - g.X, gy = target.Y - g.Y;
        if ((gx * gx) + (gy * gy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu)
        {
            g.Route = null;
            g.Retries = 0;
            g.Standing = PatrolBeat.StandSeconds;
            g.Leg = (g.Leg + 1) % beat.Count;
            return;
        }

        if (g.Route is not { Active: true })
        {
            g.Route = null;
            if (++g.Retries > PatrolBeat.RePlansPerLeg)
            {
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % beat.Count;
            }
        }
    }

    private static DeckReachability.Point At(Walker g) => new(g.X, g.Y);

    private static DeckReachability.Point At(in PatrolBeat.Stop s) => new(s.X, s.Y);

    private static AutoWalk.Planner Begin(
        Walker g, in PatrolBeat.Stop target, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        AutoWalk.Planner.Begin(
            At(g), At(target), walls, DeckPlan.AvatarRadius,
            PatrolBeat.LatticeFor(new PatrolBeat.Stop(g.X, g.Y, "here"), target, Field));

    private static (DeckPlan Deck, IReadOnlyList<PatrolBeat.Stop> Beat) FloorOf(string body, int level)
    {
        DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
        UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
        return (deck, PatrolBeat.BeatFor(body, level, 0, plan, Field));
    }

    /// <summary>
    /// THE SAME MAN, IN THE SAME PLACE, ON EVERY FRAME OF A MINUTE. Two guards on one floor: one plans on
    /// the frame he leaves a stop (the shipped shape), one plans through the stand. They must not differ by
    /// so much as a bit — same route, same five seconds, same stops, same order.
    ///
    /// <para><b>Proven RED</b> by making the stand plan over a COARSER lattice — the lab's third candidate
    /// fix, the one it warned "changes routes". At <c>DefaultStep * 2</c> the two men part company on luna
    /// B2 at frame 528, by well under a tenth of a deck unit, which is exactly the size of drift a sameness
    /// test has to be able to see.</para>
    /// </summary>
    [Fact]
    public void PlanningThroughTheStandChangesNothingTheGuardDoes()
    {
        var bad = new List<string>();
        int floors = 0, stands = 0, ready = 0;

        foreach ((string body, int level) in Floors)
        {
            (DeckPlan deck, IReadOnlyList<PatrolBeat.Stop> beat) = FloorOf(body, level);
            if (beat.Count < 2)
            {
                continue;
            }
            floors++;
            IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;

            var asShipped = new Walker { X = beat[0].X, Y = beat[0].Y, Leg = 1, Standing = PatrolBeat.StandSeconds };
            var spread = new Walker { X = beat[0].X, Y = beat[0].Y, Leg = 1, Standing = PatrolBeat.StandSeconds };

            for (int i = 0; i < Frames && bad.Count < 6; i++)
            {
                Step(asShipped, walls, beat, slice: 0);
                Step(spread, walls, beat, PatrolBeat.PlanCellsAFrame);

                if (asShipped.X != spread.X || asShipped.Y != spread.Y)
                {
                    bad.Add($"  {body} B{-level} frame {i}: the round that planned while it stood is at " +
                            $"({spread.X:F9}, {spread.Y:F9}), the shipped one at " +
                            $"({asShipped.X:F9}, {asShipped.Y:F9}).");
                }
                else if (asShipped.Leg != spread.Leg || Math.Abs(asShipped.Standing - spread.Standing) > 1e-12)
                {
                    bad.Add($"  {body} B{-level} frame {i}: leg {spread.Leg} standing {spread.Standing:F9} " +
                            $"against leg {asShipped.Leg} standing {asShipped.Standing:F9}.");
                }
            }

            // THE POINT OF THE EXERCISE, and the half that a sameness test cannot see. The shipped shape
            // must NEVER arrive at a departure with the plan in hand; the spread one must nearly always.
            Assert.Equal(0, asShipped.ReadyOnDeparture);
            stands += spread.LeftAStop;
            ready += spread.ReadyOnDeparture;
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} frame(s) where the two rounds parted company:");
            foreach (string line in bad)
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        Assert.True(floors >= 2, $"only {floors} floors were walked — the comparison is too small.");
        // Lab 45 §C measured the round asking for about 2 plans per minute per guard, so a minute on three
        // floors is a handful of departures and not a hundred. Pinned all the same: a run in which nobody
        // ever left a stop would prove nothing about the frame he leaves it on.
        Assert.True(stands >= 6, $"only {stands} stands ended in a minute — nobody walked a round.");
        Assert.True(ready >= stands,
            $"only {ready} of {stands} departures had the next leg already planned — the stand is not " +
            "doing the work, so the 6.4 ms is still being paid on the frame he leaves (#858).");
    }

    /// <summary>
    /// #843's CLASS: A MAN WAITING ON A PLAN IS A MAN WHO NEVER WALKS AGAIN. The slicing must never be able
    /// to withhold a route — so a planner given NO frames at all, and one given a single cell a frame, both
    /// hand back the identical route <c>AutoWalk.Plan</c> hands back in one go.
    /// </summary>
    [Fact]
    public void AGuardAlwaysLeavesHisStopWithARouteAndItIsTheSameRoute()
    {
        int legs = 0, waypoints = 0;

        foreach ((string body, int level) in Floors)
        {
            (DeckPlan deck, IReadOnlyList<PatrolBeat.Stop> beat) = FloorOf(body, level);
            IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;

            for (int i = 0; i < beat.Count; i++)
            {
                PatrolBeat.Stop from = beat[i], to = beat[(i + 1) % beat.Count];
                (double MinX, double MinY, double MaxX, double MaxY) box = PatrolBeat.LatticeFor(from, to, Field);
                var a = new DeckReachability.Point(from.X, from.Y);
                var b = new DeckReachability.Point(to.X, to.Y);

                AutoWalk.Attempt whole = AutoWalk.Plan(true, a, b, walls, DeckPlan.AvatarRadius, box);
                Assert.NotNull(whole.Route);

                // Never advanced at all — the frame he leaves is the first the search ever hears of.
                AutoWalk.Attempt cold =
                    AutoWalk.Planner.Begin(a, b, walls, DeckPlan.AvatarRadius, box).Finish();

                // …and the meanest possible slicing: one lattice cell a frame, for as many frames as the
                // stand has, then whatever is left on the departure frame.
                AutoWalk.Planner crawl = AutoWalk.Planner.Begin(a, b, walls, DeckPlan.AvatarRadius, box);
                for (int frame = 0; frame < 300 && !crawl.Advance(1); frame++)
                {
                    // one cell a frame, and it may well not be finished when the stand ends
                }
                AutoWalk.Attempt sliced = crawl.Finish();

                Assert.NotNull(cold.Route);
                Assert.NotNull(sliced.Route);
                Assert.Equal(whole.Route!.Route, cold.Route!.Route);
                Assert.Equal(whole.Route.Route, sliced.Route!.Route);

                legs++;
                waypoints += whole.Route.Route.Count;
            }
        }

        Assert.True(legs > 15, $"only {legs} legs were planned three ways — the sweep is too small.");
        Assert.True(waypoints > 500, $"the {legs} legs held only {waypoints} waypoints — these are not routes.");
    }

    /// <summary>The slice budget is <b>derived</b>, not tuned: the stand has to be long enough to walk the
    /// biggest lattice a leg can pose (Lab 45 measured 29,002 cells), or the spread is a comment.</summary>
    [Fact]
    public void TheStandIsLongEnoughToWalkTheBiggestLatticeALegCanPose()
    {
        const int framesInAStand = (int)(PatrolBeat.StandSeconds * 60);
        const int biggestLatticeLab45Measured = 29_002;

        Assert.True(framesInAStand * PatrolBeat.PlanCellsAFrame > biggestLatticeLab45Measured,
            $"{framesInAStand} frames × {PatrolBeat.PlanCellsAFrame} cells is " +
            $"{framesInAStand * PatrolBeat.PlanCellsAFrame} — less than the {biggestLatticeLab45Measured}-cell " +
            "lattice Lab 45 measured, so a worst-case leg would still be planned on the departure frame.");
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

    /// <summary>#870 · The round is six partials by subject now, so the page this guard reads is all six —
    /// concatenated in the order the one file laid them out, which is exactly the text it read before the
    /// split. The count is asserted, so a seventh part can never go unread.</summary>
    private static string Patrol()
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages");
        string[] order =
        [
            "Map.Patrol.cs", "Map.Patrol.Hide.cs", "Map.Patrol.Round.cs",
            "Map.Patrol.Challenge.cs", "Map.Patrol.Escort.cs", "Map.Patrol.Run.cs",
        ];
        Assert.Equal(order.Length, Directory.GetFiles(dir, "Map.Patrol*.cs").Length);

        var parts = new string[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            parts[i] = File.ReadAllText(Path.Combine(dir, order[i]));
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

    /// <summary>The replica above cannot see whether the PAGE plans during the stand. Pinned here: the stand
    /// branch spends the slice, and the departure finishes whatever is left rather than starting again.
    /// </summary>
    [Fact]
    public void ThePageSpendsTheStandOnTheNextLegAndFinishesItOnDeparture()
    {
        string walk = Between(Patrol(), "private void WalkTheRound(", "── THE CHALLENGE");

        Assert.Contains("PlanTheNextLegWhileHeStands(g, walls);", walk, StringComparison.Ordinal);
        Assert.Contains("ahead.PlannedFor(from, to)", walk, StringComparison.Ordinal);
        Assert.Contains("plan.Finish()", walk, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.PlanCellsAFrame", walk, StringComparison.Ordinal);

        // The leg's own box, still — a floor-sized lattice on arrival is what #804 took out.
        Assert.Contains("PatrolBeat.LatticeFor(", walk, StringComparison.Ordinal);
    }
}
