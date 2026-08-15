using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #831 · A STANDING GUARD IS ALWAYS STANDING <b>AT</b> SOMETHING, DOING SOMETHING READABLE.
///
/// <para>Owner, evening playtest 2026-08-11 on <c>/map?patrol=2</c>: <i>"I kind of liked the random encounter
/// but the guard was in the middle of the corridor which was strange"</i> — <i>"why would it just stand there
/// if there is no inspection point etc"</i>. And then the real-world anchor that turned it into a system:
/// <i>"they actually in real life like have these check points they electronically sign on rounds to prove
/// they did their round."</i></para>
///
/// <para>Core owns WHERE the stations are (<see cref="TheRoundSignsInTests"/> sweeps that); this file owns
/// what a man DOES about them, over the real collision field, and it answers the three questions the issue
/// put:</para>
///
/// <list type="number">
/// <item>A guard standing at a stop is <b>looking at the plate</b> he is standing at.</item>
/// <item>A MADE TAIL performs a cover act instead of freezing bare — he reads something on a wall, and
/// drifts the few du to one when nothing is at hand.</item>
/// <item>THE THIRD SUSPECT: a snag → stand → replan cycle pinning a guard mid-leg. Walled for real, and the
/// round has to reach a DIFFERENT stop.</item>
/// </list>
///
/// <para>The stepper lives in a partial class on a razor page that no test can build, so the walk below is a
/// faithful REPLICA of it over the real deck, the real Core constants and the real collision field — the
/// idiom <see cref="TheRoundIsNotStandingStillTests"/> established, with its source-shape pins underneath so
/// the replica cannot quietly drift from the page.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AStandingGuardIsStandingAtSomethingTests
{
    private static readonly string[] Bodies = ["luna", "miranda", "titan", "europa"];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>A live rAF frame — the one the epsilon fails on.</summary>
    private const double Dt = 1.0 / 60.0;

    /// <summary>One guard, the fields <c>Map.Patrol</c>'s own carries.</summary>
    private sealed class Walker
    {
        public double X, Y, Vx, Vy, Facing, Standing, CoverFor;
        public int Leg, Retries, SignedPoint, CoverPoint;
        public PatrolBeat.WallThing? CoverAt;
        public AutoWalk? Route;
    }

    // ── (b) HE IS LOOKING AT THE THING HE IS STANDING AT ──────────────────────────────────────────────

    /// <summary>
    /// EVERY STAND ON EVERY ROUND IS A MAN FACING A PLATE.
    ///
    /// <para>The stand was five seconds of a body with a facing left over from the last stride — which is
    /// exactly the picture the owner filed: a man in the middle of a corridor, pointed nowhere, for no
    /// reason. It is now the sign-in, and the facing comes off Core's own fixture.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>target.Point is { } station</c> clause from
    /// <c>WalkTheRound</c>'s arrival — see the PR body for the verbatim run.</para>
    /// </summary>
    [Fact]
    public void AGuardAtAStopIsLookingAtThePlateHeIsStandingAt()
    {
        var bad = new List<string>();
        int floors = 0, signIns = 0;

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

                var g = new Walker { X = beat[0].X, Y = beat[0].Y, Leg = 1, Standing = 0 };
                for (int i = 0; i < 60 * 90; i++)
                {
                    bool walking = g.Standing <= 0;
                    Step(g, deck.CollisionField, beat);
                    if (!walking || g.Standing <= 0)
                    {
                        continue;   // not the frame a stand began
                    }

                    signIns++;

                    // Which station is he standing at? The nearest one on the floor — asked of the FLOOR
                    // rather than of the stop he thinks he reached, because "he is facing the plate he is
                    // standing at" has to be true of the man on the deck and not of a bookkeeping field.
                    PatrolBeat.Checkpoint? at = null;
                    double best = double.MaxValue;
                    foreach (PatrolBeat.Checkpoint p in PatrolBeat.CheckpointsOn(plan, Field))
                    {
                        double gap = p.GapFrom(g.X, g.Y);
                        if (gap < best)
                        {
                            best = gap;
                            at = p;
                        }
                    }

                    if (at is not { } station)
                    {
                        bad.Add($"  {body} B{-level}: a stand began at ({g.X:F1}, {g.Y:F1}) on a floor with " +
                                "no stations at all.");
                        continue;
                    }

                    if (best > PatrolBeat.AtTheStopDu + 0.001)
                    {
                        bad.Add($"  {body} B{-level}: a stand began {best:F1} du from the nearest station — " +
                                "a man standing in a corridor at nothing (#831).");
                        continue;
                    }

                    double want = Math.Atan2(station.Y - g.Y, station.X - g.X);
                    double off = Math.Abs(Math.Atan2(Math.Sin(want - g.Facing), Math.Cos(want - g.Facing)));
                    if (off > 0.05)
                    {
                        bad.Add($"  {body} B{-level}: he stood at ROUND POINT {station.Number} facing " +
                                $"{off:F2} rad away from it — the stand has no body.");
                    }

                    if (g.SignedPoint != station.Number)
                    {
                        bad.Add($"  {body} B{-level}: he signed ROUND POINT {g.SignedPoint} while standing " +
                                $"at {station.Number}.");
                    }
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} stand(s) are not a man signing a plate:");
            foreach (string line in bad.GetRange(0, Math.Min(20, bad.Count)))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        Assert.True(floors > 8, $"only {floors} floors were walked.");
        Assert.True(signIns > 30, $"only {signIns} sign-ins were watched — this proved very little.");
    }

    // ── (c) THE COVER ACT ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A HELD TAIL IS READING SOMETHING, NOT STARING DOWN A CORRIDOR AT NOTHING.
    ///
    /// <para>Owner: <i>"A MADE tail performs a COVER ACT instead of freezing bare: turns to the nearest wall
    /// fixture, checks a plate, reads a docket — same hold, same honest Vx=0."</i> And: <i>"Mid-corridor with
    /// nothing near: he does not hold there — he drifts the few du to the nearest fixture first, then
    /// holds."</i></para>
    ///
    /// <para>The hold is taken at every square the round genuinely walks over (<see cref="HoldSpots"/>) and in
    /// every one of them the man must end up within <see cref="PatrolBeat.CoverStandDu"/> of something on a
    /// wall and looking straight at it, with the fan told the truth (Vx = Vy = 0) once he is there.</para>
    ///
    /// <para><b>And it is pinned at ALL of them.</b> This guard was first written pinning bare holds at a
    /// tenth and it came up RED at a fifth: measured over every leg of every round on all four sites, the
    /// nearest thing anybody has BOLTED to a wall is a median 19 du off and as much as 31, because the spine
    /// between two rib mouths is long and blank. That measurement is what put
    /// <see cref="PatrolBeat.TheNearestFace"/> under the fixtures — a man with nothing to read steps out of
    /// the middle to a pace off the stone and turns to it — and it is why the pin below is 0 and not 10%.</para>
    ///
    /// <para><b>Proven RED</b> by putting the bare <c>g.Vx = 0; g.Vy = 0;</c> hold back in place of
    /// <c>TheCoverAct</c> — see the PR body.</para>
    /// </summary>
    [Fact]
    public void AHeldTailReadsAFixtureInsteadOfFreezingBare()
    {
        var bad = new List<string>();
        int floors = 0, holds = 0, drifted = 0, bare = 0, covered = 0, onStone = 0;

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

                IReadOnlyList<PatrolBeat.WallThing> readables = PatrolBeat.ReadablesOn(plan, Field);

                foreach ((double hx, double hy) in HoldSpots(beat, deck.CollisionField))
                {
                    holds++;
                    var g = new Walker { X = hx, Y = hy, Facing = 0 };
                    double startX = g.X, startY = g.Y;

                    // Ten seconds of being held. The drift is a few du and is over long before that.
                    for (int i = 0; i < 60 * 10; i++)
                    {
                        CoverAct(g, deck.CollisionField, readables);
                    }

                    if (g.CoverAt is not { } thing)
                    {
                        // NOT SO MUCH AS A WALL within a few du. Counted rather than papered over, and pinned
                        // at zero below: on these floors there is nowhere a man can stand that has no stone
                        // within <see cref="PatrolBeat.CoverDriftDu"/>, and a floor plan that grew one would
                        // be a finding rather than a statue.
                        bare++;
                        continue;
                    }

                    covered++;
                    if (thing.Point == PatrolBeat.TheStoneItself)
                    {
                        onStone++;
                    }

                    // He is STOPPED — the hold law is dressed, never broken.
                    if (Math.Abs(g.Vx) > 1e-6 || Math.Abs(g.Vy) > 1e-6)
                    {
                        bad.Add($"  {body} B{-level}: a held tail is still moving at ({g.Vx:F3}, {g.Vy:F3}) " +
                                "— the hold law is broken, not dressed.");
                    }

                    // …and he is LOOKING AT THE THING. This is the whole of guard (c): the old hold left the
                    // facing wherever the last stride had put it, which is down the corridor at nothing.
                    double want = Math.Atan2(thing.Y - g.Y, thing.X - g.X);
                    double off = Math.Abs(Math.Atan2(Math.Sin(want - g.Facing), Math.Cos(want - g.Facing)));
                    if (off > 0.05)
                    {
                        bad.Add($"  {body} B{-level}: a held tail at ({g.X:F1}, {g.Y:F1}) is facing " +
                                $"{off:F2} rad away from the only thing near him.");
                    }

                    // …and he never walked AWAY from it. The drift closes or it does nothing; a hold that
                    // wandered would be a tail walking on past a seated captain, which is the one thing the
                    // law exists to stop.
                    double began = Math.Sqrt(
                        ((thing.X - startX) * (thing.X - startX)) + ((thing.Y - startY) * (thing.Y - startY)));
                    double ended = Math.Sqrt(
                        ((thing.X - g.X) * (thing.X - g.X)) + ((thing.Y - g.Y) * (thing.Y - g.Y)));
                    if (ended > began + 0.001)
                    {
                        bad.Add($"  {body} B{-level}: a held tail drifted AWAY from what he was reading " +
                                $"({began:F1} du → {ended:F1} du).");
                    }

                    if (Math.Abs(g.X - startX) + Math.Abs(g.Y - startY) > 0.01)
                    {
                        drifted++;
                    }
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} of {holds} held tails are statues:");
            foreach (string line in bad.GetRange(0, Math.Min(20, bad.Count)))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        Assert.True(floors > 8, $"only {floors} floors were held on.");
        Assert.True(holds > 100, $"only {holds} holds were taken.");
        Assert.True(drifted > 10,
            $"only {drifted} of {holds} holds involved a DRIFT — a run in which nobody ever had to walk the " +
            "few du to a fixture has not tested the clause the owner asked for.");

        // AND THE COVERAGE IS PINNED AT ALL OF IT. Not a tenth, not a fifth: EVERY hold on every round on
        // every patrolled floor of all four sites ends with a man standing at something and looking at it.
        // That is only true because the last resort is the wall itself — the published fixtures alone leave a
        // fifth of this building's rounds with nothing within a few du, which is #831's own picture and is
        // exactly what would be shipped if this line read "a tenth".
        Assert.Equal(0, bare);
        Assert.True(covered > 100, $"only {covered} holds were actually covered.");

        // …and the split is worth stating, because it is the measurement that decided the design: the bare
        // spine between two rib mouths is a real and large part of a round, and a fallback that never fired
        // would be a fallback nothing had tested.
        Assert.True(onStone > 100,
            $"only {onStone} of {covered} covered holds fell back to a bare face of wall — the fixtures alone " +
            "were measured to leave a fifth of these rounds with nothing in reach, so a run this small means " +
            "the last resort is not being exercised and this guard is asserting less than it claims.");
        Assert.True(onStone * 2 < covered,
            $"{onStone} of {covered} covered holds are a man reading blank shotcrete — over half, which is a " +
            "building that has stopped bolting things to its walls rather than a fallback.");
    }

    /// <summary>#831 · THE TELL, stated where a test can see it: a man signing the station he signed a minute
    /// ago is the gumshoe's confirmation. It is deliberately never said out loud — no card, no line, no
    /// marker — so this pins the predicate the sim reads and the fact that the nearest fixture to a man who
    /// has just left a stop IS the one he just signed.</summary>
    [Fact]
    public void TheDoubleSignInIsWhatAMadeTailLooksLike()
    {
        Assert.True(PatrolBeat.DoubleSignIn(3, 3));
        Assert.False(PatrolBeat.DoubleSignIn(3, 4));
        Assert.False(PatrolBeat.DoubleSignIn(0, 0), "a man who has signed nothing has not signed it twice.");

        DeckPlan deck = HiveInterior.FloorDeck("luna", -2, Field, 0, (_, _) => { }, []);
        UndergroundComplex.FloorPlan plan = UndergroundComplex.Build("luna", -2, Field);
        IReadOnlyList<PatrolBeat.Stop> beat = PatrolBeat.BeatFor("luna", -2, 0, plan, Field);
        Assert.True(beat.Count >= 2, "luna B2 has no round on it — this test is measuring nothing.");
        IReadOnlyList<PatrolBeat.WallThing> readables = PatrolBeat.ReadablesOn(plan, Field);

        // He signs a stop, walks a few strides off it, and is made. What he covers with is the plate he has
        // just signed — and a player who has been watching him has now seen the same act twice.
        var g = new Walker { X = beat[0].X, Y = beat[0].Y, Leg = 1, Standing = 0 };
        while (g.SignedPoint == 0)
        {
            Step(g, deck.CollisionField, beat);
        }
        int signed = g.SignedPoint;
        for (int i = 0; i < 45; i++)
        {
            Step(g, deck.CollisionField, beat);
        }

        for (int i = 0; i < 60 * 10; i++)
        {
            CoverAct(g, deck.CollisionField, readables);
        }

        Assert.True(PatrolBeat.DoubleSignIn(g.CoverPoint, signed),
            $"he covered at station {g.CoverPoint} having signed {signed} — the double sign-in is the whole " +
            "of the tell and there was nothing to see.");
    }

    // ── (d) THE THIRD SUSPECT: A SNAGGED ROUND MUST NOT CYCLE IN PLACE ────────────────────────────────

    /// <summary>
    /// A ROUND WHOSE NEXT STOP HAS BEEN WALLED OFF REACHES A DIFFERENT STOP, AND DOES IT SOON.
    ///
    /// <para>The issue's third suspect, ruled out for real rather than by reading: <i>"a snag → stand →
    /// replan cycle pinning a guard mid-leg — verify a snagged round actually progresses to its next stop
    /// rather than cycling in place."</i></para>
    ///
    /// <para>The stop the round is walking to is SEALED — a closed box of wall round it, on the real
    /// collision field, planned and walked over the same walled world the game would hand him. He has to be
    /// standing at a DIFFERENT stop inside <see cref="PinnedSeconds"/>.</para>
    ///
    /// <para><b>Proven RED</b> by making the two escapes in <c>WalkTheRound</c> return instead of advancing
    /// the leg (the <c>planned.Route is null</c> arm and the <c>Retries &gt; RePlansPerLeg</c> arm): the man
    /// grinds against the box for the whole minute and never stands anywhere. See the PR body.</para>
    /// </summary>
    [Fact]
    public void ASnaggedRoundReachesADifferentStop()
    {
        var bad = new List<string>();
        int walled = 0;

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
                if (beat.Count < 3)
                {
                    continue;
                }
                walled++;

                // WHICH LEG GETS WALLED, AND WHY IT IS A ROOM AND NOT A MOUTH. The seal is a FAR ROOM stop
                // — a dead end at the bottom of a rib — and the man starts at the CAR, out on the spine.
                //
                // That pairing is the whole of the honesty here. Walling a rib MOUTH seals the rib, and a
                // guard who happened to be standing down that rib is then a man in a sealed room: he would
                // fail this test, correctly, and prove nothing at all about a snag cycle. A dead end can be
                // taken away from a round without taking anything else away with it, which is exactly the
                // case the issue asked about — a stop the ground has stopped offering.
                int seal = -1, from = -1;
                for (int s = 0; s < beat.Count; s++)
                {
                    if (seal < 0 && beat[s].What.StartsWith("the far room", StringComparison.Ordinal))
                    {
                        seal = s;
                    }
                    if (from < 0 && beat[s].What == "the car")
                    {
                        from = s;
                    }
                }
                if (seal < 0 || from < 0 || seal == from)
                {
                    continue;
                }

                double h = 1.2;
                var walls = new List<SurfaceCollision.Segment>(deck.CollisionField);
                double bx = beat[seal].X, by = beat[seal].Y;
                walls.Add(new SurfaceCollision.Segment(bx - h, by - h, bx + h, by - h));
                walls.Add(new SurfaceCollision.Segment(bx - h, by + h, bx + h, by + h));
                walls.Add(new SurfaceCollision.Segment(bx - h, by - h, bx - h, by + h));
                walls.Add(new SurfaceCollision.Segment(bx + h, by - h, bx + h, by + h));

                var g = new Walker { X = beat[from].X, Y = beat[from].Y, Leg = seal, Standing = 0 };
                int reached = -1;
                for (int i = 0; i < (int)(PinnedSeconds * 60) && reached < 0; i++)
                {
                    bool walking = g.Standing <= 0;
                    Step(g, walls, beat);
                    if (!walking || g.Standing <= 0)
                    {
                        continue;
                    }
                    for (int s = 0; s < beat.Count; s++)
                    {
                        double dx = beat[s].X - g.X, dy = beat[s].Y - g.Y;
                        if ((dx * dx) + (dy * dy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu)
                        {
                            reached = s;
                        }
                    }
                }

                if (reached < 0)
                {
                    bad.Add($"  {body} B{-level}: with stop {seal} walled off, the round stood at NOTHING " +
                            $"for {PinnedSeconds:F0} s — it ended at ({g.X:F1}, {g.Y:F1}) on leg {g.Leg}, " +
                            "pinned mid-leg (#831's third suspect).");
                }
                else if (reached == seal)
                {
                    bad.Add($"  {body} B{-level}: the round stood at the stop this test had sealed — the " +
                            "wall did not take.");
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} walled round(s) never got anywhere:");
            foreach (string line in bad.GetRange(0, Math.Min(20, bad.Count)))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        Assert.True(walled > 8, $"only {walled} rounds were walled — this proved little.");
    }

    /// <summary>How long a walled round has to get somewhere else. Generous: the point is that it moves ON,
    /// not that it is quick about it.</summary>
    private const double PinnedSeconds = 90.0;

    // ── (e) THE LANE, ON THE REAL FLOOR ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A ROUND WALKS ON ITS OWN SIDE OF THE CORRIDOR, AND NEVER INSIDE THE WALL IT KEEPS TO.
    ///
    /// <para>Owner: <i>"they should respect right side traffic, and not walk in the middle of the
    /// corridor."</i> <see cref="TheRoundSignsInTests"/> states the compass law on a bare corridor; this
    /// takes the real legs of real rounds on the real collision field and asks two things of them: the
    /// straight runs are actually SHIFTED off the planned centre line, and not one shifted waypoint is
    /// somewhere a body does not fit.</para>
    ///
    /// <para><b>Proven RED</b> by handing the planned route straight to <c>AutoWalk.Along</c> without
    /// <c>KeepRight</c> — every floor reports zero shifted waypoints. See the PR body.</para>
    /// </summary>
    [Fact]
    public void TheRoundKeepsRightOnTheRealFloorAndNeverInsideAWall()
    {
        var bad = new List<string>();
        int floors = 0, legs = 0, shifted = 0;

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

                for (int i = 0; i < beat.Count; i++)
                {
                    PatrolBeat.Stop here = beat[i], next = beat[(i + 1) % beat.Count];
                    AutoWalk.Attempt planned = AutoWalk.Plan(
                        true, new DeckReachability.Point(here.X, here.Y),
                        new DeckReachability.Point(next.X, next.Y),
                        deck.CollisionField, DeckPlan.AvatarRadius,
                        PatrolBeat.LatticeFor(here, next, Field));
                    if (planned.Route is null)
                    {
                        continue;
                    }
                    legs++;

                    IReadOnlyList<DeckReachability.Point> centre = planned.Route.Route;
                    IReadOnlyList<DeckReachability.Point> laned =
                        PatrolBeat.KeepRight(centre, deck.CollisionField, DeckPlan.AvatarRadius);

                    Assert.Equal(centre.Count, laned.Count);

                    for (int w = 0; w < laned.Count; w++)
                    {
                        if (SurfaceCollision.Blocked(
                                laned[w].X, laned[w].Y, DeckPlan.AvatarRadius, deck.CollisionField))
                        {
                            bad.Add($"  {body} B{-level}: the lane put a waypoint at ({laned[w].X:F1}, " +
                                    $"{laned[w].Y:F1}), which is inside the shotcrete.");
                        }
                        if (Math.Abs(laned[w].X - centre[w].X) + Math.Abs(laned[w].Y - centre[w].Y) > 1e-9)
                        {
                            shifted++;
                        }
                    }

                    // …and the ends of a leg are never moved: a lane may not move a STOP.
                    Assert.Equal(centre[0], laned[0]);
                    Assert.Equal(centre[^1], laned[^1]);
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} laned waypoint(s) are in a wall:");
            foreach (string line in bad.GetRange(0, Math.Min(20, bad.Count)))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        Assert.True(floors > 8, $"only {floors} floors were laned.");
        Assert.True(legs > 80, $"only {legs} legs were laned.");
        Assert.True(shifted > 1_000,
            $"only {shifted} waypoints over {legs} legs were actually moved off the centre line — a lane " +
            "nobody walks is a green test that asserts nothing.");
    }

    // ── THE REPLICA ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>One frame of the round, replicating <c>Map.Patrol.WalkTheRound</c> line for line — the lane
    /// and the sign-in included. The source-shape guards at the bottom are what keep it honest.</summary>
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
            g.Route = AutoWalk.Along(
                PatrolBeat.KeepRight(planned.Route.Route, walls, DeckPlan.AvatarRadius));
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

        double tx = g.X - startX, ty = g.Y - startY;
        g.Vx = tx / Dt;
        g.Vy = ty / Dt;
        if ((tx * tx) + (ty * ty) > 1e-8)
        {
            g.Facing = Math.Atan2(ty, tx);
        }

        double gx = target.X - g.X, gy = target.Y - g.Y;
        if ((gx * gx) + (gy * gy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu)
        {
            g.Route = null;
            g.Retries = 0;
            g.Standing = PatrolBeat.StandSeconds;
            if (target.Point is { } station)
            {
                g.Facing = Math.Atan2(station.Y - g.Y, station.X - g.X);
                g.SignedPoint = station.Number;
            }
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

    /// <summary>One frame of the hold, replicating <c>Map.Patrol.TheCoverAct</c> line for line.</summary>
    private static void CoverAct(
        Walker g, IReadOnlyList<SurfaceCollision.Segment> walls,
        IReadOnlyList<PatrolBeat.WallThing> readables)
    {
        g.Route = null;
        g.CoverPoint = 0;
        g.CoverFor += Dt;
        g.CoverAt ??= PatrolBeat.CoverFor(g.X, g.Y, readables, walls, walls);

        if (g.CoverAt is not { } thing)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        g.Facing = Math.Atan2(thing.Y - g.Y, thing.X - g.X);
        g.CoverPoint = thing.Point;

        if (PatrolBeat.AtTheCover(g.X, g.Y, in thing) || g.CoverFor > PatrolBeat.CoverDriftSeconds)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        double dx = thing.X - g.X, dy = thing.Y - g.Y;
        double gap = Math.Sqrt((dx * dx) + (dy * dy));
        double take = Math.Min(PatrolBeat.WalkSpeed * Dt, Math.Max(0, gap - PatrolBeat.CoverStandDu));
        if (take < 1e-9)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        double startX = g.X, startY = g.Y;

        (double nx, double ny) = SurfaceCollision.Slide(
            g.X, g.Y, dx / gap * take, dy / gap * take,
            DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);

        double mx = nx - g.X, my = ny - g.Y;
        if ((mx * mx) + (my * my) < 1e-8)
        {
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        g.X = nx;
        g.Y = ny;
        g.Vx = (g.X - startX) / Dt;
        g.Vy = (g.Y - startY) / Dt;
    }

    /// <summary>
    /// WHERE A TAIL WOULD ACTUALLY BE STANDING WHEN THE CAPTAIN SITS DOWN.
    ///
    /// <para>Not a grid and not the midpoints between stops — <b>the round itself, walked, sampled every two
    /// seconds</b>. That distinction is the difference between a test and a green test that asserts nothing:
    /// the straight line between two stops runs through rooms and dead ground no leg ever crosses, and a
    /// coverage figure measured over places nobody stands is a figure about nothing. These are the squares a
    /// man on this floor is genuinely on, in the proportions he is genuinely on them.</para>
    /// </summary>
    private static List<(double X, double Y)> HoldSpots(
        IReadOnlyList<PatrolBeat.Stop> beat, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        var spots = new List<(double X, double Y)>();
        var g = new Walker { X = beat[0].X, Y = beat[0].Y, Leg = 1, Standing = 0 };
        for (int i = 0; i < 60 * 120; i++)
        {
            Step(g, walls, beat);
            if (i % 120 == 0 && !SurfaceCollision.Blocked(g.X, g.Y, DeckPlan.AvatarRadius, walls))
            {
                spots.Add((g.X, g.Y));
            }
        }
        return spots;
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

    /// <summary>The three lines the replica above cannot see: the leg is LANED before it is walked, the
    /// arrival turns him to his station, and the hold performs a cover act instead of freezing bare.</summary>
    [Fact]
    public void ThePageLanesItsLegsSignsItsStopsAndCoversItsHolds()
    {
        string walk = Between(Pages("Map.Patrol.cs"), "private void WalkTheRound(", "── #833 · THE APPROACH");

        Assert.Contains("PatrolBeat.KeepRight(planned.Route.Route", walk, StringComparison.Ordinal);
        Assert.Contains("AutoWalk.Along(", walk, StringComparison.Ordinal);
        Assert.Contains("System.Math.Atan2(station.Y - g.Y, station.X - g.X)", walk, StringComparison.Ordinal);
        Assert.Contains("g.SignedPoint = station.Number", walk, StringComparison.Ordinal);

        // The cover act, and the honest hold inside it.
        Assert.Contains(
            "PatrolBeat.CoverFor(g.X, g.Y, _patrolReadables, sight, walls)", walk, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.AtTheCover(", walk, StringComparison.Ordinal);

        // …and it is what the HELD branch of the step actually calls.
        string step = Between(Pages("Map.Patrol.cs"), "private void AdvancePatrol(", "private void TheRoundWalkedPast(");
        Assert.Contains("TheCoverAct(g, dt, walls, sight)", step, StringComparison.Ordinal);

        // FootTail.MustHold is the law and it is untouched: the picture changed, the rule did not.
        Assert.Contains("FootTail.MustHold(sitting", step, StringComparison.Ordinal);
    }

    /// <summary>The plate is drawn from Core's own list, on the floors that have a round, and it is a LABEL —
    /// no console, no verb, no card. Asking a checkpoint a question with [E] would turn the answer to "why is
    /// he standing there" into an errand.</summary>
    [Fact]
    public void TheStationsAreDrawnFromCoreAndCarryNoVerb()
    {
        string hive = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Rendering", "HiveInterior.cs"));
        string block = Between(hive, "#831 · AND THE WATCHCLOCK STATIONS", "// The cars, on every floor");

        Assert.Contains("PatrolBeat.IsPatrolled(bodyId, level)", block, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.CheckpointsOn(floor, field)", block, StringComparison.Ordinal);
        Assert.Contains("labels.Add(", block, StringComparison.Ordinal);
        Assert.Contains("point.Plate", block, StringComparison.Ordinal);
        Assert.DoesNotContain("consoles.Add(", block, StringComparison.Ordinal);

        // …and a floor with a round on it really does end up with the plates on its deck.
        DeckPlan deck = HiveInterior.FloorDeck("luna", -2, Field, 0, (_, _) => { }, []);
        int plates = deck.RoomLabels.Count(l => PatrolBeat.IsCheckpointPlate(l.Text));
        Assert.True(plates >= 4,
            $"luna B2's deck carries {plates} watchclock plates — the round has more stops than that.");
    }
}
