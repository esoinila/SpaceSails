using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #832 · WHY THE FAN NEVER ONCE RETURNED A WALKING GUARD — and it was never the fan.
///
/// <para>Owner, evening playtest 2026-08-11: the tracker read <i>"no movement — for now"</i> for a whole
/// session on B2 while PATROL 2 was plainly walking the corridor. Everything downstream checked out — guards
/// are yielded by the one accessor, the hive HUD sweeps it, the sweep feeds the drawn fan — so the issue
/// asked which end was broken. Neither. <b>The guard genuinely was not moving.</b></para>
///
/// <para>The sub-stepper below the round is the captain's own (<c>Map.Deck.cs</c>), copied, and the copy
/// dropped one epsilon: the captain spends his frame budget while <c>budget &gt; 1e-9</c>, the round spent it
/// while <c>budget &gt; 0</c>. A float budget is never consumed to exactly zero, so nearly every frame took
/// one more sub-step of about 1e-17 du, the slide moved the body by less than the snag threshold, and the
/// route was declared refused by the ground — whereupon the arrival clause, which read an inactive route as
/// an ARRIVAL, charged five seconds of standing and skipped the stop. Measured on the shipped code: a guard
/// on luna B2 was moving <b>4.0%</b> of a simulated minute and covered <b>7.7 du</b>. A motion-only fan is
/// perfectly correct about a man like that, and perfectly useless.</para>
///
/// <para><b>What this file can and cannot prove.</b> The stepper lives in a partial class on a razor page
/// that no test can build, so the walk below is a faithful REPLICA of it over the real deck, the real Core
/// constants and the real collision field — it proves the geometry and the numbers let a guard walk. The
/// two lines that were actually wrong are then pinned in the page itself, in the source-shape idiom
/// <see cref="TheRoundIsWalkableTests"/> uses, because a replica that drifts from the page is exactly the
/// green test that asserts nothing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRoundIsNotStandingStillTests
{
    private static readonly string[] Bodies = ["luna", "miranda", "titan", "europa"];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The frame the walk is spent in — a live rAF frame, which is the case the epsilon fails on.</summary>
    private const double Dt = 1.0 / 60.0;

    /// <summary>How long a round is watched. A minute is several legs and several stands, so a guard who is
    /// standing because he is AT A STOP and one who is standing because his route keeps being refused cannot
    /// be confused.</summary>
    private const int Frames = 60 * 60;

    /// <summary>One guard, exactly the fields <c>Map.Patrol</c>'s own carries.</summary>
    private sealed class Walker
    {
        public double X, Y, Vx, Vy, Standing;
        public int Leg, Retries;
        public AutoWalk? Route;
    }

    /// <summary>
    /// A GUARD ON A REAL ROUND SPENDS MOST OF A MINUTE MOVING — and the fan holds him while he does.
    ///
    /// <para><b>Proven RED</b> against the shipped stepper (restore <c>budget &gt; 0</c> and the
    /// <c>there || route inactive</c> arrival clause): every floor fails the moving-fraction assertion, at
    /// 1.7–17.4% moving and 3.2–33.3 du travelled per minute — see the PR body for the run.</para>
    /// </summary>
    [Fact]
    public void AGuardWalksHisRoundInsteadOfStandingInACorridor()
    {
        var bad = new List<string>();
        int floors = 0, walked = 0, stood = 0;

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
                floors++;

                IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;
                var g = new Walker
                {
                    X = beat[0].X,
                    Y = beat[0].Y,
                    Leg = 1,
                    Standing = PatrolBeat.StandSeconds,
                };

                // The captain stands where the lift put them — the one square on the floor everybody has to
                // pass through, and the place the owner was standing when he filed this.
                (double cx, double cy) = HiveInterior.SpawnOn(Field);
                double fan = MotionTracker.UndergroundRange(MotionTracker.DetectionRange(32.0), level);

                int moving = 0, crisp = 0, blob = 0;
                double travelled = 0;
                for (int i = 0; i < Frames; i++)
                {
                    double x0 = g.X, y0 = g.Y;
                    Step(g, walls, beat);
                    travelled += Math.Sqrt(((g.X - x0) * (g.X - x0)) + ((g.Y - y0) * (g.Y - y0)));

                    if (MotionTracker.IsMoving(g.Vx, g.Vy))
                    {
                        moving++;
                    }

                    // …and what the fan makes of him from there, off the entity the client's one accessor
                    // builds — the register included, because that is what a guard IS to this instrument.
                    //
                    // Asked only while he is INSIDE the reach, because the reach is real and shortens with
                    // depth (#591, and a deliberate third cost of going down): a guard the fan cannot hear
                    // from B7 is the instrument telling the truth, not this test finding a bug.
                    IReadOnlyList<MotionTracker.Blip> sweep = MotionTracker.Sweep(
                        cx, cy, [new MotionTracker.Entity(g.X, g.Y, g.Vx, g.Vy, PatrolBeat.FanRegister)], fan);
                    double range = Math.Sqrt(((g.X - cx) * (g.X - cx)) + ((g.Y - cy) * (g.Y - cy)));

                    if (MotionTracker.IsMoving(g.Vx, g.Vy) && range <= fan)
                    {
                        crisp++;
                        if (sweep.Count != 1 || sweep[0].Kind != MotionTracker.BlipKind.Crisp)
                        {
                            bad.Add($"  {body} B{-level} frame {i}: a guard walking {range:F0} du out, " +
                                    $"inside a {fan:F0} du fan, returned {Describe(sweep)}.");
                        }
                    }
                    else if (!MotionTracker.IsMoving(g.Vx, g.Vy)
                             && range <= fan * MotionTracker.StillReachFraction)
                    {
                        blob++;
                        if (sweep.Count != 1 || sweep[0].Kind != MotionTracker.BlipKind.Blob)
                        {
                            bad.Add($"  {body} B{-level} frame {i}: a guard STANDING {range:F0} du out " +
                                    $"returned {Describe(sweep)} — a standing man is not silence (#830).");
                        }
                    }
                }

                double movingShare = moving / (double)Frames;
                if (movingShare < 0.5)
                {
                    bad.Add($"  {body} B{-level}: a guard was moving {movingShare:P1} of the minute and " +
                            $"covered {travelled:F1} du — the round is standing in a corridor (#832).");
                }
                walked += crisp;
                stood += blob;
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} finding(s) about rounds that do not walk:");
            foreach (string line in bad)
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        // A flood over nothing is a green test that asserts nothing (§13). Pin the size of what was actually
        // measured, and pin BOTH halves of it: a run in which nobody ever walked inside the fan, or in which
        // nobody ever stood inside it, would have proved only one of the two laws.
        Assert.True(floors > 8, $"only {floors} restricted floors were walked — the sweep is too small.");
        Assert.True(walked > 5_000, $"only {walked} frames had a guard WALKING inside the fan.");
        Assert.True(stood > 1_000, $"only {stood} frames had a guard STANDING inside the fan.");
    }

    /// <summary>What a sweep held, for a red run's own printout.</summary>
    private static string Describe(IReadOnlyList<MotionTracker.Blip> sweep) =>
        sweep.Count == 0 ? "NOTHING AT ALL" : $"{sweep.Count} × {sweep[0].Kind}";

    /// <summary>
    /// A STAND IS EARNED BY ARRIVING. The other half of the same failure: with the shipped clause a body
    /// grazing a jamb halfway down a corridor stood for five seconds and then walked to a DIFFERENT stop, so
    /// the round stopped in places that are not stops — which also makes it unlearnable, and the round being
    /// learnable is the entire feature (#804).
    /// </summary>
    [Fact]
    public void AGuardOnlyEverStandsWhereTheBeatSaysToStand()
    {
        DeckPlan deck = HiveInterior.FloorDeck("luna", -2, Field, 0, (_, _) => { }, []);
        UndergroundComplex.FloorPlan plan = UndergroundComplex.Build("luna", -2, Field);
        IReadOnlyList<PatrolBeat.Stop> beat = PatrolBeat.BeatFor("luna", -2, 0, plan, Field);
        Assert.True(beat.Count >= 2, "luna B2 has no round on it — this test is measuring nothing.");

        var g = new Walker { X = beat[0].X, Y = beat[0].Y, Leg = 1, Standing = 0 };
        var strandedAt = new List<string>();

        for (int i = 0; i < Frames; i++)
        {
            bool wasWalking = g.Standing <= 0;
            Step(g, deck.CollisionField, beat);
            if (!wasWalking || g.Standing <= 0)
            {
                continue;   // not the frame a stand began
            }

            double nearest = double.MaxValue;
            foreach (PatrolBeat.Stop s in beat)
            {
                nearest = Math.Min(nearest, Math.Sqrt(((s.X - g.X) * (s.X - g.X)) + ((s.Y - g.Y) * (s.Y - g.Y))));
            }
            if (nearest > PatrolBeat.AtTheStopDu + 0.001)
            {
                strandedAt.Add($"  frame {i}: stood at ({g.X:F1}, {g.Y:F1}), {nearest:F1} du from any stop.");
            }
        }

        Assert.True(strandedAt.Count == 0,
            $"the round stood {strandedAt.Count} time(s) somewhere that is not a stop:\n" +
            string.Join("\n", strandedAt.GetRange(0, Math.Min(6, strandedAt.Count))));
    }

    /// <summary>One frame of the round, replicating <c>Map.Patrol.WalkTheRound</c> line for line. The
    /// source-shape guards below are what keep it honest.</summary>
    private static void Step(Walker g, IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<PatrolBeat.Stop> beat)
    {
        if (g.Standing > 0)
        {
            g.Standing -= Dt;
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        PatrolBeat.Stop target = beat[g.Leg % beat.Count];
        if (g.Route is not { Active: true })
        {
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, new DeckReachability.Point(g.X, g.Y), new DeckReachability.Point(target.X, target.Y),
                walls, DeckPlan.AvatarRadius,
                PatrolBeat.LatticeFor(new PatrolBeat.Stop(g.X, g.Y, "here"), target, Field));
            if (planned.Route is null)
            {
                g.Retries = 0;
                g.Leg = (g.Leg + 1) % beat.Count;
                return;
            }
            g.Route = planned.Route;
        }

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

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    /// <summary>
    /// THE TWO LINES THAT WERE ACTUALLY WRONG, pinned in the page. The replica above cannot see them; this
    /// is the only thing that can.
    /// </summary>
    [Fact]
    public void ThePageSpendsItsBudgetLikeTheCaptainDoesAndStandsOnlyOnArrival()
    {
        string walk = Between(Pages("Map.Patrol.cs"), "private void WalkTheRound(", "── THE CHALLENGE");

        // The epsilon. Without it the last sub-step of nearly every frame is a phantom snag.
        Assert.Contains("budget > 1e-9", walk, StringComparison.Ordinal);
        Assert.DoesNotContain("budget > 0;", walk, StringComparison.Ordinal);

        // …and it is the CAPTAIN'S epsilon, so the two steppers cannot drift apart again unnoticed.
        Assert.Contains("budget > 1e-9", Pages("Map.Deck.cs"), StringComparison.Ordinal);

        // A snag is not an arrival: the stand and the leg advance are behind the "there" test alone.
        Assert.DoesNotContain("if (there || g.Route is not { Active: true })", walk, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.RePlansPerLeg", walk, StringComparison.Ordinal);
    }

    /// <summary>#830 · The guard's REGISTER goes on the fan from the one accessor, and it is Core's answer
    /// rather than a flag typed into a page — so the day something else walks these floors it declares its
    /// own beside its own rules.</summary>
    [Fact]
    public void TheFanIsToldWhatKindOfContactAGuardIs()
    {
        string accessor = Between(
            Pages("Map.SweepTeam.cs"), "private IEnumerable<MotionTracker.Entity> EverythingThatMoves()", "\n}");
        Assert.Contains("g.Vx, g.Vy, PatrolBeat.FanRegister", accessor, StringComparison.Ordinal);
        Assert.Equal(MotionTracker.AtRest.Restless, PatrolBeat.FanRegister);
    }

    /// <summary>#830 law 4 · The HUD's sentence, its pulse and its dots are built from ONE sweep. The
    /// shipped code asked the sweep for a number and then asked the number for a sentence, which is how
    /// "no movement — for now" came to be printed over a fan that was holding something.</summary>
    [Fact]
    public void TheSentenceAndTheSweepReadTheSameList()
    {
        string hud = Pages("Map.Surface.cs");

        Assert.Contains("MotionTracker.ReadoutOf(blips, closing)", hud, StringComparison.Ordinal);
        Assert.Contains("MotionTracker.CadenceOf(blips)", hud, StringComparison.Ordinal);
        Assert.Contains("MotionTracker.ReadoutOf(aboardBlips, aboardClosing)", hud, StringComparison.Ordinal);
        Assert.Contains("MotionTracker.CadenceOf(aboardBlips)", hud, StringComparison.Ordinal);

        // The old shape: a scalar taken off the sweep and then asked, separately, for a sentence.
        Assert.DoesNotContain("MotionTracker.Readout(nearest, closing)", hud, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionTracker.CadenceFor(nearest)", hud, StringComparison.Ordinal);
    }

    /// <summary>#832 · The eye's three rungs are the SIM's answer, handed down on the droid — the pen is
    /// never left to work out how far a person is visible.</summary>
    [Fact]
    public void TheDistantFigureTierComesDownFromTheSim()
    {
        string step = Between(Pages("Map.Patrol.cs"), "private void AdvancePatrol(", "private void WalkTheRound(");
        Assert.Contains("PatrolBeat.SightingFor(", step, StringComparison.Ordinal);
        Assert.Contains("SightBlockers()", step, StringComparison.Ordinal);

        string filler = Between(Pages("Map.Patrol.cs"), "private void FillPatrolDroids(", "\n}");
        Assert.Contains("PatrolBeat.Sighting.None", filler, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.Sighting.Smear", filler, StringComparison.Ordinal);
        Assert.Contains("-9999", filler, StringComparison.Ordinal);
    }
}
