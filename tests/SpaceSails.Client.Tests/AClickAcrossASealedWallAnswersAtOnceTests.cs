using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;
using Xunit.Abstractions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1384 · A CLICK ACROSS A WALL THAT HAS NO WAY ROUND MUST ANSWER, NOT HANG.
///
/// <para>Playtest, three runs of three: click the DORMANT SYNTHETIC dot across the lift-head shed's wall and
/// the tab never came back. The goal was on the far side of a wall ring with no road to it, so the A* did the
/// one thing a search for an unreachable goal can do - it flooded EVERY standable square of the lattice the
/// bounds allowed (the bounding box of every wall on the whole ground, on a field several hundred du across at
/// a half-du step) to find that out, twice (the pointing fallback is a second look), in a browser that runs
/// the same loop thirty times slower than the desk. The refusal the house already has ("no way through") was
/// right; it simply arrived after the tab had given up.</para>
///
/// <para>So a search that has expanded more cells than any walk the game asks for could possibly need ends as a
/// refusal. This drives the REAL surface deck (<see cref="MoonSurface.SurfaceDeck"/>) with a goal walled into a
/// sealed cell, through <see cref="AutoWalk.Plan"/> exactly as <c>Map.ClickToWalkAt</c> calls it, and asserts
/// the click comes back refused, quickly, having expanded a bounded number of cells; the control walks a
/// long legitimate route across the same ground and must still be planned.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class AClickAcrossASealedWallAnswersAtOnceTests
{
    private readonly ITestOutputHelper _out;
    public AClickAcrossASealedWallAnswersAtOnceTests(ITestOutputHelper o) => _out = o;

    private static readonly SurfaceLayout.Field Env = MoonSurface.ExpeditionField();
    private const double AvatarRadius = 0.7;

    private static List<SurfaceCollision.Segment> GroundOf(string body, LandingSite site)
    {
        DeckPlan deck = MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name,
            monolithEpoch: 0, hasSecretSite: true);
        return [.. deck.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))];
    }

    /// <summary>A closed ring round a point: four walls, no gap.</summary>
    private static void Seal(List<SurfaceCollision.Segment> walls, double x, double y, double half)
    {
        walls.Add(new(x - half, y - half, x + half, y - half));
        walls.Add(new(x - half, y + half, x + half, y + half));
        walls.Add(new(x - half, y - half, x - half, y + half));
        walls.Add(new(x + half, y - half, x + half, y + half));
    }

    [Fact]
    public void AGoalSealedInsideAWallIsRefusedAtOnce()
    {
        var slow = new List<string>();
        int clicks = 0;
        foreach (string body in new[] { "secret-lab-site", "miranda", "luna", "triton" })
        {
            foreach (LandingSite site in LandingSites.For(body).Take(2))
            {
                List<SurfaceCollision.Segment> walls = GroundOf(body, site);
                var from = new DeckReachability.Point(Env.HomeX, Env.TopY - 2.0);
                var to = new DeckReachability.Point(Env.HomeX + 40, Env.AnchorY + 4);
                Seal(walls, to.X, to.Y, 8.0);   // wider than the pointing reach (5 du), so no square outside counts as there
                SurfaceCollision.WallIndex field = SurfaceCollision.WallIndex.Build([.. walls]);

                var sw = Stopwatch.StartNew();
                AutoWalk.Attempt attempt = AutoWalk.Plan(
                    enabled: true, from, to, field, AvatarRadius,
                    AutoWalk.BoundsFor(walls, from, to), DeckReachability.DefaultStep, AutoWalk.PointingReachDu);
                sw.Stop();
                // The deterministic half: the proof itself says sealed (the clock below is only a loose backstop).
                if (!DeckReachability.ProvablySealedOff(from, to, field, AvatarRadius,
                        AutoWalk.BoundsFor(walls, from, to), DeckReachability.DefaultStep, AutoWalk.PointingFallbackDu, AutoWalk.SealedProbeCells))
                {
                    slow.Add($"{body}/{site.Name}: the pocket proof did not call a sealed ring sealed");
                }
                clicks++;
                _out.WriteLine($"{body}/{site.Name}: {sw.ElapsedMilliseconds} ms, route={(attempt.Route is null ? "refused" : "planned")}");
                if (attempt.Route is not null)
                {
                    slow.Add($"{body}/{site.Name}: a goal inside a sealed ring was PLANNED");
                }
                else if (sw.ElapsedMilliseconds > 1000)
                {
                    slow.Add($"{body}/{site.Name}: refusal took {sw.ElapsedMilliseconds} ms");
                }
            }
        }

        Assert.True(clicks >= 6, $"only {clicks} click(s) were asked.");
        Assert.True(slow.Count == 0, "a click on an unreachable goal must be refused at once:\n  " + string.Join("\n  ", slow));
    }

    [Fact]
    public void ALongLegitimateWalkAcrossTheSameGroundStillPlans()
    {
        int planned = 0;
        var wrong = new List<string>();
        foreach (string body in new[] { "secret-lab-site", "miranda", "luna", "triton" })
        {
            foreach (LandingSite site in LandingSites.For(body).Take(2))
            {
                List<SurfaceCollision.Segment> walls = GroundOf(body, site);
                SurfaceCollision.WallIndex field = SurfaceCollision.WallIndex.Build([.. walls]);
                var from = new DeckReachability.Point(Env.HomeX, Env.TopY - 2.0);
                var to = new DeckReachability.Point(Env.HomeX + 40, Env.AnchorY + 4);
                if (!DeckReachability.Standable(to.X, to.Y, AvatarRadius, field))
                {
                    continue;   // the control must be a place a captain could stand
                }
                AutoWalk.Attempt attempt = AutoWalk.Plan(
                    enabled: true, from, to, field, AvatarRadius,
                    AutoWalk.BoundsFor(walls, from, to), DeckReachability.DefaultStep, AutoWalk.PointingReachDu);
                if (attempt.Route is null)
                {
                    wrong.Add($"{body}/{site.Name}: a reachable goal {Math.Abs(to.Y - from.Y):F0} du away was refused");
                }
                else
                {
                    planned++;
                }
            }
        }

        Assert.True(planned >= 3, $"only {planned} control walk(s) were planned - this proves little.");
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    /// <summary>
    /// #1384 · A REACHABLE WALK NEVER PAYS FOR THE PROOF. The pocket proof is a flood on the lattice; run before
    /// every plan it costs more than the search it guards on a short click (measured: 10 ms against 0.3 ms at
    /// 30 du), and the browser is thirty times slower. So the search goes first with a budget and the proof runs
    /// only for a search that has spent it. Counted, not timed: the proof's own invocation counter.
    /// </summary>
    [Fact]
    public void AReachableWalkNeverRunsThePocketProof()
    {
        int walks = 0;
        foreach (string body in new[] { "secret-lab-site", "miranda", "luna", "triton" })
        {
            foreach (LandingSite site in LandingSites.For(body).Take(2))
            {
                List<SurfaceCollision.Segment> walls = GroundOf(body, site);
                SurfaceCollision.WallIndex field = SurfaceCollision.WallIndex.Build([.. walls]);
                var from = new DeckReachability.Point(Env.HomeX, Env.TopY - 2.0);
                foreach (double dy in new[] { 12.0, 30.0, 55.0 })
                {
                    var to = new DeckReachability.Point(Env.HomeX, Env.TopY - 2.0 - dy);
                    if (!DeckReachability.Standable(to.X, to.Y, AvatarRadius, field))
                    {
                        continue;
                    }
                    int before = DeckReachability.SealedProofsRun;
                    AutoWalk.Attempt attempt = AutoWalk.Plan(
                        enabled: true, from, to, field, AvatarRadius,
                        AutoWalk.BoundsFor(walls, from, to), DeckReachability.DefaultStep, AutoWalk.PointingReachDu);
                    if (attempt.Route is null)
                    {
                        continue;   // not a reachable walk; the sealed test owns that case
                    }
                    walks++;
                    Assert.Equal(before, DeckReachability.SealedProofsRun);
                }
            }
        }

        Assert.True(walks >= 6, $"only {walks} reachable walk(s) were asked - this proves little.");

        // …and the counter is live: a sealed goal DOES run it (so zero above is not a dead counter).
        List<SurfaceCollision.Segment> sealedWalls = GroundOf("miranda", LandingSites.For("miranda")[0]);
        var a = new DeckReachability.Point(Env.HomeX, Env.TopY - 2.0);
        var b = new DeckReachability.Point(Env.HomeX + 40, Env.AnchorY + 4);
        Seal(sealedWalls, b.X, b.Y, 8.0);
        int ran = DeckReachability.SealedProofsRun;
        AutoWalk.Plan(true, a, b, SurfaceCollision.WallIndex.Build([.. sealedWalls]), AvatarRadius,
            AutoWalk.BoundsFor(sealedWalls, a, b), DeckReachability.DefaultStep, AutoWalk.PointingReachDu);
        Assert.True(DeckReachability.SealedProofsRun > ran, "a sealed goal never ran the proof - the counter is dead.");
    }
}
