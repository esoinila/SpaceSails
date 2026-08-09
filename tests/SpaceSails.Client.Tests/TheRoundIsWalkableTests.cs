using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #804 · CAN A GUARD ACTUALLY WALK THE ROUND? Owner: <i>"the guards using the A* to have a beat are
/// missing"</i> — and a beat is only a beat if every leg of it is a walk somebody can make.
///
/// <para>This is <see cref="YouCanWalkTheHiveTests"/>'s question pointed at a new target list. Core decides
/// WHERE the stops are; only the client can build the walls a body actually collides with
/// (<see cref="HiveInterior.FloorDeck"/>), so the walk has to be audited here. It floods every restricted
/// floor of every clandestine site on several watches and asks two things: can the car reach every stop,
/// and can each stop reach the NEXT one — because a round is a loop, and a loop with one impassable leg is
/// a guard standing in a corridor forever.</para>
///
/// <para>The second half of the file is the WIRING, in the source-shape idiom
/// <see cref="TheChitRidesTheCageTests"/> uses: the sightline gate, the fan's one accessor and the deck's
/// droid count all live in a partial class on a razor page, and the thing that must never come back is a
/// guard drawn through a wall.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRoundIsWalkableTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>
    /// The floor as it is built with NOTHING shot open (#809's <c>locksShotOpen</c> left empty), which is
    /// deliberately the HARDEST case and therefore the only one worth flooding: a lock the captain has shot
    /// off takes a wall OUT of the collision field, so it can only ever make a leg easier to walk. A round
    /// that connects on the un-shot building connects on every shot-open version of it.
    /// </summary>
    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    /// <summary>
    /// EVERY LEG OF EVERY ROUND, ON EVERY WATCH, WALKED FOR REAL.
    ///
    /// <para><b>Proven RED</b> by pointing <c>PatrolBeat.Circuit</c>'s room stop at the room's own centre
    /// plus twenty deck units in y — a spot that is inside a wall on most floors — which produces the
    /// stranded-leg table this audit exists to print.</para>
    /// </summary>
    [Fact]
    public void EveryStopOnEveryRoundIsWalkableFromTheCarAndFromTheStopBeforeIt()
    {
        var bad = new List<string>();
        int floors = 0, legs = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                DeckPlan deck = DeckFor(body, level);
                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                (double sx, double sy) = HiveInterior.SpawnOn(Field);
                var spawn = new DeckReachability.Point(sx, sy);
                var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);
                floors++;

                // Three watches, because the ROTATION reorders the stops and a leg that is fine walked one
                // way is a leg walked the other way — and because the stop list itself must not change with
                // the watch, only its order. A watch-dependent stop would be a round that walks somewhere
                // the audit never checked.
                for (long watch = 0; watch < 3; watch++)
                {
                    IReadOnlyList<PatrolBeat.Stop> beat =
                        PatrolBeat.BeatFor(body, level, watch, plan, Field);

                    if (beat.Count < 2)
                    {
                        bad.Add($"  {body} B{-level} w{watch}: a round with {beat.Count} stop(s) on it.");
                        continue;
                    }

                    for (int i = 0; i < beat.Count; i++)
                    {
                        PatrolBeat.Stop here = beat[i];
                        PatrolBeat.Stop next = beat[(i + 1) % beat.Count];
                        var at = new DeckReachability.Point(here.X, here.Y);
                        legs++;

                        if (!DeckReachability.Standable(
                                here.X, here.Y, DeckPlan.AvatarRadius, deck.CollisionField))
                        {
                            bad.Add($"  {body} B{-level} w{watch}: {here.What} at ({here.X:F0}, {here.Y:F0}) " +
                                    "is inside a wall — nobody can stand on it.");
                            continue;
                        }

                        if (!DeckReachability.CanReach(
                                spawn, at, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                        {
                            bad.Add($"  {body} B{-level} w{watch}: {here.What} at ({here.X:F0}, {here.Y:F0}) " +
                                    "cannot be reached from the car.");
                            continue;
                        }

                        // …and the leg is walked over the box the GAME will plan it in, not over the whole
                        // floor. A sweep that used a floor-sized lattice would prove a route nothing ever
                        // asks for — two pathfinders agreeing by luck, which is the exact drift Core's one
                        // LatticeFor exists to stop.
                        if (!DeckReachability.CanReach(
                                at, new DeckReachability.Point(next.X, next.Y),
                                deck.CollisionField, DeckPlan.AvatarRadius,
                                PatrolBeat.LatticeFor(here, next, Field)))
                        {
                            bad.Add($"  {body} B{-level} w{watch}: the leg from {here.What} to {next.What} " +
                                    "does not connect — the round stops there forever.");
                        }
                    }
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} leg(s) of a round are not walkable:");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }

        // A flood over nothing is a green test that asserts nothing. Pin the size of the sweep.
        Assert.True(floors > 40, $"only {floors} restricted floors were walked.");
        Assert.True(legs > 500, $"only {legs} legs were walked.");
    }

    /// <summary>
    /// The round's stops are the SAME PLACES on every watch — only their order turns over. A watch that
    /// moved the stops would put a guard somewhere the audit above never checked, and the audit would still
    /// be green because it walks whatever it is handed.
    /// </summary>
    [Fact]
    public void TheShiftReordersTheRoundAndNeverMovesIt()
    {
        int floors = 0;
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                HashSet<(double, double)> canonical = Places(PatrolBeat.Circuit(plan, Field));
                floors++;

                for (long watch = 0; watch < 6; watch++)
                {
                    Assert.Equal(
                        canonical,
                        Places(PatrolBeat.BeatFor(body, level, watch, plan, Field)));
                }
            }
        }
        Assert.True(floors > 40, $"only {floors} floors were measured.");

        static HashSet<(double, double)> Places(IReadOnlyList<PatrolBeat.Stop> stops) =>
            [.. stops.Select(s => (Math.Round(s.X, 3), Math.Round(s.Y, 3)))];
    }

    /// <summary>Nobody is placed on top of a fixture the [E] key answers for — a guard standing ON the
    /// SEARCH THE ROOM console would make the room unpressable while they stood there.</summary>
    [Fact]
    public void TheCarStopIsOffTheLiftConsole()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                PatrolBeat.Stop car = PatrolBeat.Circuit(plan, Field)[0];
                (double sx, double sy) = HiveInterior.SpawnOn(Field);

                // The car stop IS the square the captain arrives on, deliberately: it is the one place on
                // the floor everybody has to pass through, and a round that never checked the lift would be
                // a round nobody could time from safety.
                Assert.Equal(sx, car.X, 6);
                Assert.Equal(sy, car.Y, 6);
            }
        }
    }

    // ── THE WIRING ────────────────────────────────────────────────────────────────────────────────────
    //
    // The gating, the fan and the buffer live in a partial class on a razor page, which no test can build.
    // Source-shape guards, exactly as #752's own file argues for them: what must never come back is a guard
    // drawn through a wall, a fan that cannot hear one, and a band that falls off the end of a deck.

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
    /// A GUARD THE CAPTAIN CANNOT SEE IS NOT ON THE DECK. The droid filler asks Core's one predicate and
    /// parks everybody else at the off-map coordinates an empty slot uses (#371's idiom).
    /// </summary>
    [Fact]
    public void TheDroidFillerDrawsOnlyWhatTheCaptainCanSee()
    {
        string filler = Between(
            Pages("Map.Patrol.cs"), "private void FillPatrolDroids(", "\n}");

        Assert.Contains("Drawn", filler, StringComparison.Ordinal);
        Assert.Contains("-9999", filler, StringComparison.Ordinal);

        // …and the flag it reads is written from Core's predicate, once a frame, in the step.
        string step = Between(Pages("Map.Patrol.cs"), "private void AdvancePatrol(", "private void WalkTheRound(");
        Assert.Contains("PatrolBeat.DrawnFor(", step, StringComparison.Ordinal);
        Assert.Contains("SightBlockers()", step, StringComparison.Ordinal);

        // The gate must be the SIGHT set, not the bare collision field: a shut door is a wall to an eye.
        Assert.DoesNotContain("DrawnFor(_avatarX, _avatarY, g.X, g.Y, walls)", step, StringComparison.Ordinal);
    }

    /// <summary>The fan hears them, and it hears them from the ONE accessor — the comment in that method
    /// says every kind of figure that moves has to be listed there and nowhere else.</summary>
    [Fact]
    public void TheRoundsAreOnTheMotionFan()
    {
        string accessor = Between(
            Pages("Map.SweepTeam.cs"), "private IEnumerable<MotionTracker.Entity> EverythingThatMoves()", "\n}");
        Assert.Contains("_guards", accessor, StringComparison.Ordinal);
        Assert.Contains("g.Vx, g.Vy", accessor, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE HIVE DECK COUNTS THE WHOLE BUFFER. It used to stop at the repo crew, which parked the sweep team
    /// by accident and would have parked the rounds the same way — a band that falls off the end of a deck
    /// does not throw, it silently draws nobody (#633's own lesson).
    /// </summary>
    [Fact]
    public void TheHiveDeckHasRoomForARound()
    {
        string rebuild = Between(
            Pages("Map.Surface.cs"), "private void RebuildSurfaceDeck()", "if (Derelict.TryParseWreckId(");
        Assert.Contains("HiveInterior.FloorDeck(", rebuild, StringComparison.Ordinal);
        Assert.Contains("SurfaceDroidCount, FillSurfaceDroids", rebuild, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "3 + ReeverEngineCeiling + MaxCollectors, FillSurfaceDroids", rebuild, StringComparison.Ordinal);

        // …and the buffer the renderer sizes to is big enough for the sum of every band.
        Assert.True(
            DeckPlan.MaxDroids >= 3 + 24 + 4 + InspectionTeam.TeamSize + PatrolBeat.MostOnAFloor,
            $"MaxDroids is {DeckPlan.MaxDroids}, which is short of the five bands.");
    }

    /// <summary>The round is built where a FLOOR changes and nowhere else. Building it in the deck rebuild
    /// would restart it every time a room is searched, under a captain who was timing it.</summary>
    [Fact]
    public void TheRoundIsBuiltWhenTheFloorChangesAndNotWhenTheDeckIsRebuilt()
    {
        string ride = Between(
            Pages("Map.Surface.cs"), "private void RideTheLiftTo(", "private (double X, double Y) SecretLabHeadSpot(");
        Assert.Contains("SpawnPatrolFor(ex)", ride, StringComparison.Ordinal);

        string rebuild = Between(
            Pages("Map.Surface.cs"), "private void RebuildSurfaceDeck()", "private void ComposeWhatYouLeft(");
        Assert.DoesNotContain("SpawnPatrolFor", rebuild, StringComparison.Ordinal);
    }

    /// <summary>
    /// A SCENE NOBODY CAN REACH ON DEMAND IS A SCENE THAT SHIPS BROKEN (#693's rule, the owner's twice-over
    /// <i>"testing is a feature"</i>). Both halves need a lever: how many walk a floor is a seeded roll on
    /// the shift, and whether the captain has anything to show is the whole of the challenge.
    /// </summary>
    [Fact]
    public void BothHalvesOfTheChallengeAreOneUrlFromTheFrontDoor()
    {
        Assert.Contains(DevStarts.All, e => e.Url == "/map?patrol=2");
        Assert.Contains(DevStarts.All, e => e.Url == "/map?badge=1");

        // …and the guide is the prose twin of that catalogue, so a button without a row is half a feature.
        string root = RepoRoot();
        string guide = File.ReadAllText(Path.Combine(root, "docs", "testing-guide.md"));
        Assert.Contains("?patrol=", guide, StringComparison.Ordinal);
        Assert.Contains("?badge=1", guide, StringComparison.Ordinal);

        string hive = File.ReadAllText(Path.Combine(root, "docs", "testing-links-the-hive.md"));
        Assert.Contains("patrol=", hive, StringComparison.Ordinal);
        Assert.Contains("badge=1", hive, StringComparison.Ordinal);

        // And the cheat the docs promise is a cheat the parser actually reads.
        string boot = Between(Pages("Map.Sim.cs"), "private async Task BootTheWorldAsync(", "\n    }");
        Assert.Contains("\"patrol=\"", boot, StringComparison.Ordinal);
        Assert.Contains("\"badge=\"", boot, StringComparison.Ordinal);
    }

    /// <summary>The pass is issued by the SHIFT, not by the offer — hung on the arrival the day-labour chit
    /// opened, which is the moment the gig stops being a promise.</summary>
    [Fact]
    public void ThePassIsIssuedWhenTheCageTakesYouDown()
    {
        string ride = Between(
            Pages("Map.Surface.cs"), "private void RideTheLiftTo(", "private (double X, double Y) SecretLabHeadSpot(");
        Assert.Contains("chitGateThisRide = true", ride, StringComparison.Ordinal);
        Assert.Contains("IssueTheSitePass(ex)", ride, StringComparison.Ordinal);

        // …and it is said AFTER the arrival has finished composing, or the pass's own sentence is a pulse
        // the arrival's release takes straight back off it (#693/#768).
        Assert.True(
            ride.IndexOf("ReleaseHeldSayingsUnlessACardStopsTheWorld", StringComparison.Ordinal)
            < ride.IndexOf("IssueTheSitePass(ex)", StringComparison.Ordinal),
            "the pass is granted inside the arrival's own composition, where its line loses the slot.");

        string issue = Between(
            Pages("Map.Patrol.cs"), "private void IssueTheSitePass(", "── DRAWING THEM");
        Assert.Contains("PatrolBeat.BadgeHeld(", issue, StringComparison.Ordinal);
        Assert.Contains("Satchel.CanTake(", issue, StringComparison.Ordinal);
        Assert.Contains("Satchel.Add(", issue, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.BadgeIssuedLine", issue, StringComparison.Ordinal);
    }
}
